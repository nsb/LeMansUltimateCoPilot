using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using LeMansUltimateCoPilot.Models;
using LeMansUltimateCoPilot.Analysis;
using LeMansUltimateCoPilot.Services;
using Timer = System.Timers.Timer;

namespace LeMansUltimateCoPilot.AI
{
    /// <summary>
    /// Real-time coaching orchestrator that coordinates context building and LLM coaching
    /// </summary>
    public class RealTimeCoachingOrchestrator : IDisposable
    {
        private readonly ContextualDataAggregator _contextAggregator;
        private readonly AICoachingService _coachingService;
        private readonly VoiceOutputService _voiceService;
        private readonly Timer _coachingTimer;
        private readonly SemaphoreSlim _processingLock;
        
        private readonly List<CoachingResponse> _recentCoaching;
        private readonly CoachingConfiguration _config;
        private readonly OrchestratorStatistics _statistics;
        
        private bool _isActive;
        private bool _disposed;
        private DrivingContext? _lastContext;
        private DateTime _lastCoachingTime;
        
        private const int COACHING_INTERVAL_MS = 3000; // 3 seconds between coaching messages
        private const int MIN_COACHING_INTERVAL_MS = 1000; // Minimum 1 second between messages
        private const int MAX_RECENT_COACHING = 10;

        public event EventHandler<CoachingResponse>? CoachingGenerated;
        public event EventHandler<string>? ErrorOccurred;

        public RealTimeCoachingOrchestrator(
            ContextualDataAggregator contextAggregator,
            AICoachingService coachingService,
            VoiceOutputService voiceService,
            CoachingConfiguration config)
        {
            _contextAggregator = contextAggregator ?? throw new ArgumentNullException(nameof(contextAggregator));
            _coachingService = coachingService ?? throw new ArgumentNullException(nameof(coachingService));
            _voiceService = voiceService ?? throw new ArgumentNullException(nameof(voiceService));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            
            _recentCoaching = new List<CoachingResponse>();
            _processingLock = new SemaphoreSlim(1, 1);
            _statistics = new OrchestratorStatistics();
            
            // Initialize coaching timer
            _coachingTimer = new Timer(COACHING_INTERVAL_MS);
            _coachingTimer.Elapsed += OnCoachingTimerElapsed;
            _coachingTimer.AutoReset = true;
            
            _lastCoachingTime = DateTime.MinValue;
        }

        /// <summary>
        /// Start real-time coaching
        /// </summary>
        public void Start()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(RealTimeCoachingOrchestrator));
            
            _isActive = true;
            _coachingTimer.Start();
            _statistics.StartTime = DateTime.Now;
            
            Console.WriteLine("🏁 Real-time AI coaching started");
        }

        /// <summary>
        /// Stop real-time coaching
        /// </summary>
        public void Stop()
        {
            _isActive = false;
            _coachingTimer?.Stop();
            _statistics.EndTime = DateTime.Now;
            
            Console.WriteLine("⏹️ Real-time AI coaching stopped");
        }

        /// <summary>
        /// Process new telemetry data
        /// </summary>
        public async Task ProcessTelemetryAsync(EnhancedTelemetryData telemetry)
        {
            if (!_isActive || _disposed) return;
            
            try
            {
                // Update context with new telemetry
                _lastContext = _contextAggregator.ProcessTelemetryData(telemetry);
                _statistics.TelemetryProcessed++;
                
                // Check if immediate coaching is needed for critical situations
                await CheckForImmediateCoachingAsync();
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error processing telemetry: {ex.Message}");
            }
        }

        /// <summary>
        /// Timer event handler for periodic coaching
        /// </summary>
        private async void OnCoachingTimerElapsed(object? sender, ElapsedEventArgs e)
        {
            if (!_isActive || _disposed || _lastContext == null) return;
            
            await _processingLock.WaitAsync();
            try
            {
                // Check if enough time has passed since last coaching
                var timeSinceLastCoaching = DateTime.Now - _lastCoachingTime;
                if (timeSinceLastCoaching.TotalMilliseconds < MIN_COACHING_INTERVAL_MS)
                {
                    return;
                }
                
                // Generate coaching based on current context
                await GenerateCoachingAsync(_lastContext, CoachingTrigger.Periodic);
            }
            finally
            {
                _processingLock.Release();
            }
        }

        /// <summary>
        /// Check if immediate coaching is needed for critical situations
        /// </summary>
        private async Task CheckForImmediateCoachingAsync()
        {
            if (_lastContext?.RecentEvents == null) return;
            
            // Check for critical events that need immediate coaching
            var criticalEvents = _lastContext.RecentEvents
                .Where(e => e.Severity > 0.8f && e.TimeSinceEvent.TotalSeconds < 2)
                .ToList();
            
            if (criticalEvents.Any())
            {
                await _processingLock.WaitAsync();
                try
                {
                    await GenerateCoachingAsync(_lastContext, CoachingTrigger.Critical);
                }
                finally
                {
                    _processingLock.Release();
                }
            }
        }

        /// <summary>
        /// Generate coaching response and deliver it
        /// </summary>
        private async Task GenerateCoachingAsync(DrivingContext context, CoachingTrigger trigger)
        {
            try
            {
                var startTime = DateTime.Now;
                
                // Check if we should skip coaching based on recent messages
                if (ShouldSkipCoaching(context, trigger))
                {
                    return;
                }
                
                // Generate coaching response
                var coaching = await _coachingService.GenerateCoachingAsync(context);
                
                // Update statistics
                var processingTime = DateTime.Now - startTime;
                _statistics.TotalCoachingGenerated++;
                _statistics.AverageResponseTime = UpdateAverageResponseTime(processingTime);
                
                // Store coaching response
                _recentCoaching.Add(coaching);
                CleanOldCoachingMessages();
                
                // Deliver coaching
                await DeliverCoachingAsync(coaching);
                
                // Update last coaching time
                _lastCoachingTime = DateTime.Now;
                
                // Raise event
                CoachingGenerated?.Invoke(this, coaching);
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error generating coaching: {ex.Message}");
            }
        }

        /// <summary>
        /// Determine if coaching should be skipped
        /// </summary>
        private bool ShouldSkipCoaching(DrivingContext context, CoachingTrigger trigger)
        {
            // Don't skip critical coaching
            if (trigger == CoachingTrigger.Critical) return false;
            
            // Skip if too many recent messages
            if (_recentCoaching.Count >= 3) return true;
            
            // Skip if recent message is very similar
            var recentMessages = _recentCoaching.TakeLast(2).ToList();
            if (recentMessages.Any(m => IsSimilarCoaching(m, context)))
            {
                return true;
            }
            
            // Skip if performance is excellent and no recent events
            if (context.CurrentPerformance.Level == PerformanceLevel.Excellent && 
                !context.RecentEvents.Any(e => e.Severity > 0.3f))
            {
                return true;
            }
            
            return false;
        }

        /// <summary>
        /// Check if coaching would be similar to recent messages
        /// </summary>
        private bool IsSimilarCoaching(CoachingResponse recent, DrivingContext context)
        {
            // Simple similarity check based on category and recent events
            var recentEventTypes = context.RecentEvents.Select(e => e.Type).ToList();
            var timeSinceRecent = DateTime.Now - recent.Timestamp;
            
            // Consider similar if same category and within 10 seconds
            return timeSinceRecent.TotalSeconds < 10 && 
                   recentEventTypes.Any(t => t.ToString().Contains(recent.Category.ToString()));
        }

        /// <summary>
        /// Deliver coaching response via voice and events
        /// </summary>
        private async Task DeliverCoachingAsync(CoachingResponse coaching)
        {
            try
            {
                // Deliver via voice if enabled
                if (_config.EnableRealTimeMode && _voiceService != null)
                {
                    await _voiceService.SpeakAsync(coaching.Message, coaching.Priority);
                }
                
                // Log coaching message
                Console.WriteLine($"🎯 AI Coach: {coaching.Message} (Priority: {coaching.Priority}, Category: {coaching.Category})");
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error delivering coaching: {ex.Message}");
            }
        }

        /// <summary>
        /// Clean old coaching messages
        /// </summary>
        private void CleanOldCoachingMessages()
        {
            var cutoff = DateTime.Now.AddMinutes(-2);
            _recentCoaching.RemoveAll(c => c.Timestamp < cutoff);
            
            // Also maintain max count
            if (_recentCoaching.Count > MAX_RECENT_COACHING)
            {
                _recentCoaching.RemoveRange(0, _recentCoaching.Count - MAX_RECENT_COACHING);
            }
        }

        /// <summary>
        /// Update average response time
        /// </summary>
        private TimeSpan UpdateAverageResponseTime(TimeSpan newTime)
        {
            if (_statistics.TotalCoachingGenerated == 1)
            {
                return newTime;
            }
            
            var currentAverage = _statistics.AverageResponseTime.TotalMilliseconds;
            var newAverage = (currentAverage * (_statistics.TotalCoachingGenerated - 1) + newTime.TotalMilliseconds) 
                           / _statistics.TotalCoachingGenerated;
            
            return TimeSpan.FromMilliseconds(newAverage);
        }

        /// <summary>
        /// Get orchestrator statistics
        /// </summary>
        public OrchestratorStatistics GetStatistics()
        {
            _statistics.IsActive = _isActive;
            _statistics.RecentCoachingCount = _recentCoaching.Count;
            return _statistics;
        }

        /// <summary>
        /// Get recent coaching history
        /// </summary>
        public List<CoachingResponse> GetRecentCoaching()
        {
            return _recentCoaching.ToList();
        }

        /// <summary>
        /// Error event handler
        /// </summary>
        private void OnErrorOccurred(string error)
        {
            ErrorOccurred?.Invoke(this, error);
            Console.WriteLine($"❌ Coaching Error: {error}");
        }

        /// <summary>
        /// Dispose resources
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;
            
            Stop();
            _coachingTimer?.Dispose();
            _processingLock?.Dispose();
            
            _disposed = true;
        }
    }

    /// <summary>
    /// Coaching trigger types
    /// </summary>
    public enum CoachingTrigger
    {
        Periodic,
        Critical,
        Manual
    }

    /// <summary>
    /// Orchestrator statistics
    /// </summary>
    public class OrchestratorStatistics
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public bool IsActive { get; set; }
        public int TotalCoachingGenerated { get; set; }
        public int TelemetryProcessed { get; set; }
        public int RecentCoachingCount { get; set; }
        public TimeSpan AverageResponseTime { get; set; }
        public TimeSpan SessionDuration => EndTime == default ? DateTime.Now - StartTime : EndTime - StartTime;
    }

    /// <summary>
    /// Extension methods for VoiceOutputService to support coaching priorities
    /// </summary>
    public static class VoiceOutputServiceExtensions
    {
        public static async Task SpeakAsync(this VoiceOutputService voiceService, string message, CoachingPriority priority)
        {
            // Convert AI CoachingPriority to Services CoachingPriority
            var servicesPriority = priority switch
            {
                CoachingPriority.Low => Services.CoachingPriority.Low,
                CoachingPriority.Medium => Services.CoachingPriority.Medium,
                CoachingPriority.High => Services.CoachingPriority.High,
                CoachingPriority.Critical => Services.CoachingPriority.Critical,
                _ => Services.CoachingPriority.Low
            };
            
            // Create a coaching message and queue it
            var coachingMessage = new CoachingMessage
            {
                Content = message,
                Type = CoachingMessageType.RealTimeCorrection,
                Priority = servicesPriority,
                CreatedAt = DateTime.Now
            };
            
            await voiceService.QueueMessageAsync(coachingMessage);
        }
    }
}
