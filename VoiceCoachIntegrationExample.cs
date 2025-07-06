using System;
using System.Threading.Tasks;
using LeMansUltimateCoPilot.Models;
using LeMansUltimateCoPilot.Services;

namespace LeMansUltimateCoPilot.Examples
{
    /// <summary>
    /// Example demonstrating how to integrate the Voice Driving Coach
    /// with the existing telemetry and comparison systems
    /// </summary>
    public class VoiceCoachIntegrationExample
    {
        private readonly VoiceDrivingCoach _voiceCoach;
        private readonly RealTimeComparisonService _comparisonService;
        private readonly ReferenceLapManager _referenceLapManager;
        private readonly TrackConfigurationManager _trackConfigManager;

        public VoiceCoachIntegrationExample()
        {
            // Initialize services
            var trackMapper = new TrackMapper();
            _comparisonService = new RealTimeComparisonService(trackMapper);
            _referenceLapManager = new ReferenceLapManager("reference_laps");
            _trackConfigManager = new TrackConfigurationManager("track_configs");

            // Initialize LLM and Voice services
            var llmConfig = new LLMConfiguration
            {
                ApiKey = "your-openai-api-key-here", // Replace with actual API key
                Model = "gpt-3.5-turbo",
                MaxTokens = 50,
                Temperature = 0.3
            };

            var voiceSettings = new VoiceSettings
            {
                Volume = 80,
                Rate = 0,
                MaxQueueSize = 5
            };

            var llmService = new LLMCoachingService(llmConfig);
            var voiceService = new VoiceOutputService(voiceSettings);

            // Initialize voice coach
            _voiceCoach = new VoiceDrivingCoach(llmService, voiceService, _comparisonService);

            // Subscribe to coaching events
            _voiceCoach.CoachingProvided += OnCoachingProvided;
        }

        /// <summary>
        /// Start a coaching session
        /// </summary>
        public async Task StartCoachingSessionAsync(string trackName)
        {
            try
            {
                // Load track configuration
                var trackConfig = _trackConfigManager.LoadTrackConfiguration(trackName);
                
                // Load reference lap
                var referenceLap = _referenceLapManager.GetReferenceLaps(trackName).FirstOrDefault();
                
                if (trackConfig != null && referenceLap != null)
                {
                    // Setup comparison service
                    _comparisonService.SetReferenceLap(referenceLap, trackConfig);
                    
                    // Setup voice coach
                    _voiceCoach.SetTrack(trackConfig);
                    
                    // Start coaching
                    await _voiceCoach.StartCoachingSession();
                    
                    Console.WriteLine($"Voice coaching started for {trackName} with reference lap {referenceLap.Id}");
                }
                else
                {
                    Console.WriteLine("Failed to load track configuration or reference lap");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error starting coaching session: {ex.Message}");
            }
        }

        /// <summary>
        /// Process telemetry data (call this from your main telemetry loop)
        /// </summary>
        public async Task ProcessTelemetryAsync(EnhancedTelemetryData telemetry)
        {
            try
            {
                // Process comparison (this will trigger coaching if needed)
                var comparison = _comparisonService.ProcessTelemetry(telemetry);
                
                // The voice coach automatically receives updates via events
                // No additional code needed here
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing telemetry: {ex.Message}");
            }
        }

        /// <summary>
        /// Complete a lap
        /// </summary>
        public async Task CompleteLapAsync(double lapTime)
        {
            try
            {
                _comparisonService.CompleteLap(lapTime);
                
                // Voice coach will automatically provide lap summary
                Console.WriteLine($"Lap completed: {lapTime:F3}s");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error completing lap: {ex.Message}");
            }
        }

        /// <summary>
        /// Stop coaching session
        /// </summary>
        public async Task StopCoachingSessionAsync()
        {
            try
            {
                await _voiceCoach.StopCoachingSession();
                Console.WriteLine("Voice coaching session ended");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error stopping coaching session: {ex.Message}");
            }
        }

        /// <summary>
        /// Handle coaching events
        /// </summary>
        private void OnCoachingProvided(object? sender, CoachingMessage message)
        {
            Console.WriteLine($"[{message.Type}] [{message.Priority}] {message.Content}");
            
            // You can also log coaching messages, display them in UI, etc.
        }

        /// <summary>
        /// Example of integration with main program loop
        /// </summary>
        public async Task RunCoachingSessionExample()
        {
            Console.WriteLine("=== Voice Driving Coach Integration Example ===");
            
            // Start coaching session
            await StartCoachingSessionAsync("Silverstone");
            
            // Simulate telemetry processing
            for (int i = 0; i < 100; i++)
            {
                // Create sample telemetry data
                var telemetry = new EnhancedTelemetryData
                {
                    LapTime = i * 0.1,
                    DistanceFromStart = i * 50,
                    Speed = 150 + (i % 50),
                    ThrottleInput = 75 + (i % 25),
                    BrakeInput = i % 30,
                    SteeringInput = (i % 40) - 20,
                    TrackCondition = "Dry"
                };
                
                await ProcessTelemetryAsync(telemetry);
                await Task.Delay(100); // Simulate real-time processing
            }
            
            // Complete lap
            await CompleteLapAsync(85.456);
            
            // Stop session
            await StopCoachingSessionAsync();
        }
    }
}

namespace LeMansUltimateCoPilot.Examples
{
    /// <summary>
    /// Example program demonstrating Voice Coach integration
    /// </summary>
    public class VoiceCoachExampleProgram
    {
        public static async Task RunExample()
        {
            Console.WriteLine("Le Mans Ultimate CoPilot - Voice Driving Coach");
            Console.WriteLine("===============================================");
            
            try
            {
                // Run the voice coaching example
                var example = new VoiceCoachIntegrationExample();
                await example.RunCoachingSessionExample();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            
            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
    }
}
