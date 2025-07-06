using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LeMansUltimateCoPilot.Models;

namespace LeMansUltimateCoPilot.Services
{
    /// <summary>
    /// Voice driving coach that provides real-time coaching feedback using LLM analysis
    /// Processes comparison results and generates natural coaching messages
    /// </summary>
    public class VoiceDrivingCoach
    {
        private readonly LLMCoachingService _llmService;
        private readonly VoiceOutputService _voiceService;
        private readonly RealTimeComparisonService _comparisonService;
        private readonly List<CoachingContext> _recentContext = new();
        private readonly Dictionary<int, DateTime> _lastCoachingBySegment = new();
        private TrackConfiguration? _currentTrack;
        private DateTime _lastCoachingTime = DateTime.MinValue;

        /// <summary>
        /// Minimum time between coaching messages in seconds
        /// </summary>
        public double MinimumCoachingInterval { get; set; } = 3.0;

        /// <summary>
        /// Maximum coaching messages to queue
        /// </summary>
        public int MaxQueuedMessages { get; set; } = 3;

        /// <summary>
        /// Coaching style preference
        /// </summary>
        public CoachingStyle Style { get; set; } = CoachingStyle.Encouraging;

        /// <summary>
        /// Event raised when coaching is provided
        /// </summary>
        public event EventHandler<CoachingMessage>? CoachingProvided;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="llmService">LLM coaching service</param>
        /// <param name="voiceService">Voice output service</param>
        /// <param name="comparisonService">Real-time comparison service</param>
        public VoiceDrivingCoach(LLMCoachingService llmService, VoiceOutputService voiceService, RealTimeComparisonService comparisonService)
        {
            _llmService = llmService ?? throw new ArgumentNullException(nameof(llmService));
            _voiceService = voiceService ?? throw new ArgumentNullException(nameof(voiceService));
            _comparisonService = comparisonService ?? throw new ArgumentNullException(nameof(comparisonService));

            // Subscribe to comparison service events
            _comparisonService.ComparisonUpdated += OnComparisonUpdated;
            _comparisonService.MetricsUpdated += OnMetricsUpdated;
        }

        /// <summary>
        /// Set the current track configuration
        /// </summary>
        /// <param name="track">Track configuration</param>
        public void SetTrack(TrackConfiguration track)
        {
            _currentTrack = track;
            _lastCoachingBySegment.Clear();
            _llmService.SetTrackContext(track);
        }

        /// <summary>
        /// Start coaching for a session
        /// </summary>
        public async Task StartCoachingSession()
        {
            await _voiceService.SpeakAsync("Coaching session started. Drive safely and focus on the track.");
            _recentContext.Clear();
            _lastCoachingTime = DateTime.Now;
        }

        /// <summary>
        /// Stop coaching and provide session summary
        /// </summary>
        public async Task StopCoachingSession()
        {
            var metrics = _comparisonService.GetCurrentMetrics();
            var summary = await _llmService.GenerateSessionSummaryAsync(metrics, _recentContext);
            
            await _voiceService.SpeakAsync(summary);
            CoachingProvided?.Invoke(this, new CoachingMessage
            {
                Content = summary,
                Type = CoachingMessageType.SessionSummary,
                Priority = CoachingPriority.Low
            });
        }

        /// <summary>
        /// Handle comparison result updates
        /// </summary>
        private async void OnComparisonUpdated(object? sender, ComparisonResult result)
        {
            if (ShouldProvideCoaching(result))
            {
                var context = CreateCoachingContext(result);
                _recentContext.Add(context);
                
                // Keep only recent context (last 20 comparisons)
                if (_recentContext.Count > 20)
                {
                    _recentContext.RemoveAt(0);
                }

                await ProcessCoachingOpportunity(context);
            }
        }

        /// <summary>
        /// Handle metrics updates
        /// </summary>
        private async void OnMetricsUpdated(object? sender, RealTimeComparisonMetrics metrics)
        {
            // Check for lap completion coaching
            if (metrics.SessionStats.LapsCompleted > 0 && 
                DateTime.Now - _lastCoachingTime > TimeSpan.FromSeconds(10))
            {
                await ProvideLapSummaryCoaching(metrics);
            }
        }

        /// <summary>
        /// Determine if coaching should be provided for this comparison
        /// </summary>
        private bool ShouldProvideCoaching(ComparisonResult result)
        {
            // Don't coach if confidence is too low
            if (result.ConfidenceLevel < 70) return false;

            // Don't coach too frequently
            if (DateTime.Now - _lastCoachingTime < TimeSpan.FromSeconds(MinimumCoachingInterval))
                return false;

            // Don't coach in critical sections (high-speed corners, braking zones)
            if (IsInCriticalSection(result)) return false;

            // Only coach significant improvements
            var significantImprovements = result.ImprovementAreas
                .Where(i => i.Severity > 30 && i.PotentialGain > 0.05)
                .ToList();

            return significantImprovements.Any();
        }

        /// <summary>
        /// Check if current position is in a critical section where coaching should be avoided
        /// </summary>
        private bool IsInCriticalSection(ComparisonResult result)
        {
            if (result.Segment == null) return false;

            // Avoid coaching in high-speed sections or heavy braking zones
            var speed = result.CurrentTelemetry.Speed;
            var braking = result.CurrentTelemetry.BrakeInput;

            return speed > 200 || braking > 80 || // High speed or heavy braking
                   result.Segment.SegmentType == TrackSegmentType.Straight && speed > 150; // Fast straights
        }

        /// <summary>
        /// Create coaching context from comparison result
        /// </summary>
        private CoachingContext CreateCoachingContext(ComparisonResult result)
        {
            return new CoachingContext
            {
                Timestamp = DateTime.Now,
                DistanceFromStart = result.DistanceFromStart,
                Segment = result.Segment,
                TimeDelta = result.TimeDelta,
                SpeedDelta = result.SpeedDelta,
                ImprovementAreas = result.ImprovementAreas.ToList(),
                ConfidenceLevel = result.ConfidenceLevel,
                CurrentTelemetry = result.CurrentTelemetry,
                ReferenceTelemetry = result.ReferenceTelemetry
            };
        }

        /// <summary>
        /// Process a coaching opportunity
        /// </summary>
        private async Task ProcessCoachingOpportunity(CoachingContext context)
        {
            try
            {
                // Generate coaching message using LLM
                var coachingMessage = await _llmService.GenerateCoachingMessageAsync(context, _recentContext, Style);

                if (!string.IsNullOrEmpty(coachingMessage.Content))
                {
                    // Queue for voice output
                    await _voiceService.QueueMessageAsync(coachingMessage);
                    
                    // Update timing
                    _lastCoachingTime = DateTime.Now;
                    if (context.Segment != null)
                    {
                        _lastCoachingBySegment[context.Segment.SegmentNumber] = DateTime.Now;
                    }

                    // Raise event
                    CoachingProvided?.Invoke(this, coachingMessage);
                }
            }
            catch (Exception ex)
            {
                // Log error but don't interrupt driving
                System.Diagnostics.Debug.WriteLine($"Coaching error: {ex.Message}");
            }
        }

        /// <summary>
        /// Provide lap summary coaching
        /// </summary>
        private async Task ProvideLapSummaryCoaching(RealTimeComparisonMetrics metrics)
        {
            var summary = await _llmService.GenerateLapSummaryAsync(metrics, _recentContext);
            
            var message = new CoachingMessage
            {
                Content = summary,
                Type = CoachingMessageType.LapSummary,
                Priority = CoachingPriority.Medium
            };

            await _voiceService.QueueMessageAsync(message);
            CoachingProvided?.Invoke(this, message);
            
            _lastCoachingTime = DateTime.Now;
        }
    }

    /// <summary>
    /// Coaching context for a specific point in time
    /// </summary>
    public class CoachingContext
    {
        public DateTime Timestamp { get; set; }
        public double DistanceFromStart { get; set; }
        public TrackSegment? Segment { get; set; }
        public double TimeDelta { get; set; }
        public double SpeedDelta { get; set; }
        public List<ImprovementArea> ImprovementAreas { get; set; } = new();
        public double ConfidenceLevel { get; set; }
        public EnhancedTelemetryData CurrentTelemetry { get; set; } = new();
        public EnhancedTelemetryData ReferenceTelemetry { get; set; } = new();
    }

    /// <summary>
    /// Coaching message with metadata
    /// </summary>
    public class CoachingMessage
    {
        public string Content { get; set; } = "";
        public CoachingMessageType Type { get; set; }
        public CoachingPriority Priority { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public TrackSegment? RelatedSegment { get; set; }
    }

    /// <summary>
    /// Types of coaching messages
    /// </summary>
    public enum CoachingMessageType
    {
        RealTimeCorrection,
        LapSummary,
        SessionSummary,
        Encouragement,
        Warning
    }

    /// <summary>
    /// Coaching message priority
    /// </summary>
    public enum CoachingPriority
    {
        Low,
        Medium,
        High,
        Critical
    }

    /// <summary>
    /// Coaching style preferences
    /// </summary>
    public enum CoachingStyle
    {
        Encouraging,
        Technical,
        Concise,
        Detailed
    }
}
