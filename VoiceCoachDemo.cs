using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LeMansUltimateCoPilot.Models;
using LeMansUltimateCoPilot.Services;

namespace LeMansUltimateCoPilot
{
    /// <summary>
    /// Comprehensive demo of the Voice Driving Coach system with simulated telemetry data
    /// </summary>
    public class VoiceCoachDemo
    {
        private VoiceDrivingCoach? _voiceCoach;
        private readonly CancellationTokenSource _cancellationTokenSource = new();
        private readonly List<EnhancedTelemetryData> _simulatedTelemetry = new();
        private ReferenceLap? _referenceLap;
        private TrackConfiguration? _trackConfig;

        public async Task RunDemoAsync()
        {
            Console.WriteLine("🏁 Le Mans Ultimate Voice Driving Coach Demo");
            Console.WriteLine("==========================================\n");

            try
            {
                // Initialize the demo
                InitializeDemo();

                // Show the different demo scenarios
                await ShowDemoMenuAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Demo failed: {ex.Message}");
            }
            finally
            {
                await CleanupAsync();
            }
        }

        private void InitializeDemo()
        {
            Console.WriteLine("🔧 Initializing Voice Coach Demo...");

            // Create LLM configuration
            var llmConfig = new LLMConfiguration
            {
                ApiKey = "demo-key",
                Model = "gpt-3.5-turbo",
                ApiEndpoint = "https://api.openai.com/v1/chat/completions",
                MaxTokens = 150,
                Temperature = 0.7
            };

            // Create services for the demo - use mock implementations
            var llmService = new MockLLMCoachingService();
            var voiceService = new MockVoiceOutputService();
            var comparisonService = new MockRealTimeComparisonService();

            // Configure the voice coach
            _voiceCoach = new VoiceDrivingCoach(llmService, voiceService, comparisonService);

            // Set up event handlers to show what's happening
            _voiceCoach.CoachingProvided += OnCoachingProvided;

            // Generate demo data
            GenerateDemoData();

            Console.WriteLine("✅ Voice Coach initialized successfully!\n");
        }

        private void GenerateDemoData()
        {
            Console.WriteLine("📊 Generating demo telemetry and reference data...");

            // Create a sample track configuration (Le Mans Circuit de la Sarthe)
            _trackConfig = new TrackConfiguration
            {
                TrackName = "Circuit de la Sarthe",
                TrackLength = 13626.0, // meters
                Segments = new List<TrackSegment>
                {
                    new TrackSegment { Id = "1", SegmentNumber = 1, DistanceFromStart = 0, SegmentLength = 1000, SegmentType = TrackSegmentType.Straight, Description = "Main Straight" },
                    new TrackSegment { Id = "2", SegmentNumber = 2, DistanceFromStart = 1000, SegmentLength = 500, SegmentType = TrackSegmentType.Chicane, Description = "Dunlop Chicane" },
                    new TrackSegment { Id = "3", SegmentNumber = 3, DistanceFromStart = 1500, SegmentLength = 1500, SegmentType = TrackSegmentType.Straight, Description = "Esses Section" },
                    new TrackSegment { Id = "4", SegmentNumber = 4, DistanceFromStart = 3000, SegmentLength = 500, SegmentType = TrackSegmentType.FastCorner, Description = "Tertre Rouge" },
                    new TrackSegment { Id = "5", SegmentNumber = 5, DistanceFromStart = 3500, SegmentLength = 2500, SegmentType = TrackSegmentType.Straight, Description = "Mulsanne Straight" }
                }
            };

            // Create a reference lap with optimal times
            _referenceLap = new ReferenceLap
            {
                TrackName = "Circuit de la Sarthe",
                LapTime = 198.5, // 3:18.5 in seconds
                TelemetryData = GenerateReferenceTelemetry(),
                RecordedAt = DateTime.Now.AddDays(-1),
                VehicleName = "Le Mans Prototype",
                LapNumber = 1
            };

            // Generate simulated live telemetry with various scenarios
            GenerateSimulatedTelemetry();

            Console.WriteLine($"✅ Generated {_simulatedTelemetry.Count} telemetry samples");
            Console.WriteLine($"✅ Reference lap: {TimeSpan.FromSeconds(_referenceLap.LapTime):mm\\:ss\\.fff}\n");
        }

        private List<EnhancedTelemetryData> GenerateReferenceTelemetry()
        {
            var reference = new List<EnhancedTelemetryData>();
            var random = new Random(42); // Fixed seed for consistent demo

            for (int i = 0; i < 100; i++)
            {
                var distance = i * 136.26; // Distribute over track length
                reference.Add(new EnhancedTelemetryData
                {
                    Timestamp = DateTime.Now.AddSeconds(-3600 + i * 2), // Reference from 1 hour ago
                    DistanceFromStart = distance,
                    Speed = 280 + random.Next(-20, 20), // km/h
                    ThrottleInput = (float)(0.85 + random.NextDouble() * 0.15),
                    BrakeInput = 0.0f,
                    SteeringInput = (float)(random.NextDouble() * 0.2 - 0.1),
                    Gear = Math.Max(1, Math.Min(6, 3 + random.Next(-1, 3))),
                    EngineRPM = 7000 + random.Next(-500, 1000),
                    LapTime = i * 2,
                    LapNumber = 1,
                    SessionTime = i * 2
                });
            }

            return reference;
        }

        private void GenerateSimulatedTelemetry()
        {
            var random = new Random();
            var scenarios = new[]
            {
                "optimal_sector", "late_braking", "early_throttle", "wide_line", 
                "understeer", "oversteer", "good_improvement", "time_loss"
            };

            foreach (var scenario in scenarios)
            {
                _simulatedTelemetry.AddRange(GenerateScenarioTelemetry(scenario, random));
            }
        }

        private List<EnhancedTelemetryData> GenerateScenarioTelemetry(string scenario, Random random)
        {
            var telemetry = new List<EnhancedTelemetryData>();
            var baseTime = DateTime.Now;

            for (int i = 0; i < 20; i++)
            {
                var data = new EnhancedTelemetryData
                {
                    Timestamp = baseTime.AddSeconds(i * 0.1),
                    DistanceFromStart = i * 100,
                    LapNumber = 2,
                    SessionTime = i * 0.1
                };

                // Modify telemetry based on scenario
                switch (scenario)
                {
                    case "optimal_sector":
                        data.Speed = 290 + random.Next(-5, 5);
                        data.ThrottleInput = 0.95f;
                        data.BrakeInput = 0.0f;
                        data.SteeringInput = 0.05f;
                        break;
                    case "late_braking":
                        data.Speed = 320; // Too fast into corner
                        data.ThrottleInput = 0.0f;
                        data.BrakeInput = 0.9f; // Heavy braking
                        data.SteeringInput = 0.3f; // Struggling to turn
                        break;
                    case "early_throttle":
                        data.Speed = 180;
                        data.ThrottleInput = 0.8f; // Getting on power early
                        data.BrakeInput = 0.0f;
                        data.SteeringInput = 0.1f;
                        break;
                    case "wide_line":
                        data.Speed = 250;
                        data.ThrottleInput = 0.6f;
                        data.BrakeInput = 0.2f;
                        data.SteeringInput = 0.4f; // Wide steering angle
                        break;
                    case "understeer":
                        data.Speed = 220;
                        data.ThrottleInput = 0.4f;
                        data.BrakeInput = 0.1f;
                        data.SteeringInput = 0.6f; // Lots of steering, not much response
                        break;
                    case "oversteer":
                        data.Speed = 200;
                        data.ThrottleInput = 0.2f;
                        data.BrakeInput = 0.0f;
                        data.SteeringInput = -0.3f; // Counter-steering
                        break;
                    default:
                        data.Speed = 260 + random.Next(-10, 10);
                        data.ThrottleInput = (float)(0.7 + random.NextDouble() * 0.3);
                        data.BrakeInput = (float)(random.NextDouble() * 0.3);
                        data.SteeringInput = (float)(random.NextDouble() * 0.4 - 0.2);
                        break;
                }

                data.Gear = Math.Max(1, Math.Min(6, (int)(data.Speed / 60)));
                data.EngineRPM = 6000 + (int)(data.ThrottleInput * 2000);
                data.LapTime = i * 0.5;

                telemetry.Add(data);
            }

            return telemetry;
        }

        private async Task ShowDemoMenuAsync()
        {
            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                Console.WriteLine("🎯 Voice Coach Demo Options:");
                Console.WriteLine("1. Real-time Coaching Simulation");
                Console.WriteLine("2. Different Coaching Scenarios");
                Console.WriteLine("3. Voice Settings Demo");
                Console.WriteLine("4. Performance Analysis Demo");
                Console.WriteLine("5. Exit Demo");
                Console.Write("\nSelect option (1-5): ");

                var input = Console.ReadLine();
                if (int.TryParse(input, out int choice))
                {
                    switch (choice)
                    {
                        case 1:
                            await RunRealTimeSimulationAsync();
                            break;
                        case 2:
                            await ShowCoachingScenariosAsync();
                            break;
                        case 3:
                            await ShowVoiceSettingsDemoAsync();
                            break;
                        case 4:
                            await ShowPerformanceAnalysisDemoAsync();
                            break;
                        case 5:
                            Console.WriteLine("👋 Ending demo...");
                            _cancellationTokenSource.Cancel();
                            return;
                        default:
                            Console.WriteLine("❌ Invalid option. Please try again.\n");
                            break;
                    }
                }
                else
                {
                    Console.WriteLine("❌ Invalid input. Please enter a number.\n");
                }
            }
        }

        private async Task RunRealTimeSimulationAsync()
        {
            Console.WriteLine("\n🏎️ Starting Real-time Coaching Simulation...");
            Console.WriteLine("Simulating live telemetry data with voice coaching feedback\n");

            if (_voiceCoach == null) return;

            // Start the voice coach
            await _voiceCoach.StartCoachingSession();

            // Simulate real-time telemetry feed
            for (int i = 0; i < Math.Min(50, _simulatedTelemetry.Count); i++)
            {
                var telemetryData = _simulatedTelemetry[i];
                
                // Show current telemetry
                Console.WriteLine($"📊 Lap {telemetryData.LapNumber}, Distance: {telemetryData.DistanceFromStart:F1}m, " +
                                $"Speed: {telemetryData.Speed:F1} km/h, Throttle: {telemetryData.ThrottleInput:P1}");

                // Simulate coaching based on telemetry (since VoiceDrivingCoach works through events)
                if (i % 5 == 0) // Provide coaching every 5 samples
                {
                    await SimulateCoachingAsync(telemetryData);
                }

                // Simulate real-time delay
                await Task.Delay(500, _cancellationTokenSource.Token);

                if (i % 10 == 0)
                {
                    Console.WriteLine("Press any key to continue or 'q' to return to menu...");
                    var key = Console.ReadKey(true);
                    if (key.KeyChar == 'q' || key.KeyChar == 'Q')
                        break;
                }
            }

            await _voiceCoach.StopCoachingSession();
            Console.WriteLine("\n✅ Real-time simulation completed!\n");
        }

        private async Task ShowCoachingScenariosAsync()
        {
            Console.WriteLine("\n🎭 Different Coaching Scenarios Demo");
            Console.WriteLine("Showing various coaching situations and feedback\n");

            var scenarios = new Dictionary<string, string>
            {
                { "Optimal Performance", "Great job! You're hitting your marks perfectly. Keep this rhythm going." },
                { "Late Braking", "You're braking too late into Turn 3. Try braking 50 meters earlier to carry more speed through the corner." },
                { "Throttle Application", "You can get on the throttle earlier here. The car has good grip - trust it and apply power progressively." },
                { "Racing Line", "You're running wide through the esses. Tighten your line to set up better for the next straight." },
                { "Sector Improvement", "Excellent! You just improved your sector time by 0.3 seconds. That's the racing line paying off." },
                { "Tire Management", "Your rear tires are getting warm. Ease off the throttle by 5% for the next few corners." },
                { "Weather Adaptation", "Track conditions are changing. You can brake later now - there's more grip available." },
                { "Fuel Strategy", "You're using too much fuel. Try short-shifting and coasting into corners more." }
            };

            foreach (var scenario in scenarios)
            {
                Console.WriteLine($"🎯 Scenario: {scenario.Key}");
                Console.WriteLine($"🗣️ Coach says: \"{scenario.Value}\"");
                Console.WriteLine("🔊 [Playing voice output...]");
                
                // Simulate voice output delay
                await Task.Delay(2000);
                Console.WriteLine("✅ Voice output completed\n");
                
                await Task.Delay(1000);
            }

            Console.WriteLine("All scenarios demonstrated! Press any key to continue...");
            Console.ReadKey(true);
        }

        private async Task ShowVoiceSettingsDemoAsync()
        {
            Console.WriteLine("\n🎙️ Voice Settings Demo");
            Console.WriteLine("Demonstrating different voice configurations\n");

            var voiceSettings = new[]
            {
                new VoiceSettings { Rate = 0, Volume = 80, VoiceName = "Default" },
                new VoiceSettings { Rate = 2, Volume = 90, VoiceName = "Male" },
                new VoiceSettings { Rate = -1, Volume = 70, VoiceName = "Female" },
                new VoiceSettings { Rate = 1, Volume = 100, VoiceName = "Robotic" }
            };

            foreach (var settings in voiceSettings)
            {
                Console.WriteLine($"🎚️ Testing voice settings:");
                Console.WriteLine($"   Rate: {settings.Rate}, Volume: {settings.Volume}%, Voice: {settings.VoiceName}");
                Console.WriteLine($"🗣️ \"Turn in earlier for optimal line through this corner.\"");
                Console.WriteLine("🔊 [Playing with these settings...]");
                
                await Task.Delay(2500);
                Console.WriteLine("✅ Voice test completed\n");
            }

            Console.WriteLine("Voice settings demo completed! Press any key to continue...");
            Console.ReadKey(true);
        }

        private async Task ShowPerformanceAnalysisDemoAsync()
        {
            Console.WriteLine("\n📈 Performance Analysis Demo");
            Console.WriteLine("Analyzing telemetry data and providing insights\n");

            // Simulate performance analysis
            var analysisResults = new[]
            {
                "📊 Lap Analysis Complete:",
                "   • Best sector: Sector 2 (1:12.345)",
                "   • Time loss: 0.8s in final sector",
                "   • Improvement potential: 1.2s per lap",
                "",
                "🔍 Key Insights:",
                "   • You're losing time in high-speed corners",
                "   • Brake pressure is inconsistent",
                "   • Throttle application could be smoother",
                "",
                "💡 Coaching Recommendations:",
                "   • Focus on smooth brake release",
                "   • Trust the car's downforce in fast corners",
                "   • Work on throttle modulation out of slow corners"
            };

            foreach (var line in analysisResults)
            {
                Console.WriteLine(line);
                await Task.Delay(300);
            }

            Console.WriteLine("\n🎯 Generating personalized coaching messages...");
            await Task.Delay(1500);

            var coachingMessages = new[]
            {
                "🗣️ \"Your braking consistency has improved 15% since last session. Great progress!\"",
                "🗣️ \"Try carrying 5 more km/h through the fast left-hander. You have the downforce.\"",
                "🗣️ \"Your throttle application is getting smoother. Keep building that muscle memory.\"",
                "🗣️ \"Focus on hitting your apex markers. Small improvements add up to big lap time gains.\""
            };

            foreach (var message in coachingMessages)
            {
                Console.WriteLine(message);
                Console.WriteLine("🔊 [Playing voice coaching...]");
                await Task.Delay(2000);
                Console.WriteLine("✅ Message delivered\n");
            }

            Console.WriteLine("Performance analysis demo completed! Press any key to continue...");
            Console.ReadKey(true);
        }

        private void OnCoachingProvided(object? sender, CoachingMessage message)
        {
            Console.WriteLine($"💬 Coaching Message Generated:");
            Console.WriteLine($"   Priority: {message.Priority}");
            Console.WriteLine($"   Type: {message.Type}");
            Console.WriteLine($"   Message: \"{message.Content}\"");
            Console.WriteLine($"   Created: {message.CreatedAt:HH:mm:ss.fff}");
            Console.WriteLine();
        }

        private void OnCoachingMessageGenerated(object? sender, CoachingMessage message)
        {
            Console.WriteLine($"💬 Coaching Message Generated:");
            Console.WriteLine($"   Priority: {message.Priority}");
            Console.WriteLine($"   Type: {message.Type}");
            Console.WriteLine($"   Message: \"{message.Content}\"");
            Console.WriteLine($"   Created: {message.CreatedAt:HH:mm:ss.fff}");
            Console.WriteLine();
        }

        private void OnVoiceOutputStarted(object? sender, string message)
        {
            Console.WriteLine($"🔊 Voice Output Started: \"{message}\"");
        }

        private void OnVoiceOutputCompleted(object? sender, string message)
        {
            Console.WriteLine($"✅ Voice Output Completed: \"{message}\"");
            Console.WriteLine();
        }

        private async Task CleanupAsync()
        {
            Console.WriteLine("🧹 Cleaning up demo resources...");
            
            if (_voiceCoach != null)
            {
                await _voiceCoach.StopCoachingSession();
                // VoiceDrivingCoach doesn't implement IDisposable, so we don't dispose it
            }

            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
            
            Console.WriteLine("✅ Demo cleanup completed.");
        }

        private async Task SimulateCoachingAsync(EnhancedTelemetryData telemetryData)
        {
            // Create a mock coaching context
            var context = new CoachingContext
            {
                Timestamp = telemetryData.Timestamp,
                DistanceFromStart = telemetryData.DistanceFromStart,
                TimeDelta = -0.5 + new Random().NextDouble() * 1.0, // Random delta
                SpeedDelta = -10 + new Random().NextDouble() * 20, // Random speed delta
                ConfidenceLevel = 0.8,
                CurrentTelemetry = telemetryData,
                ReferenceTelemetry = _referenceLap?.TelemetryData.FirstOrDefault() ?? new EnhancedTelemetryData()
            };

            // Get coaching from the LLM service
            var llmService = new MockLLMCoachingService();
            var coachingMessage = await llmService.GenerateCoachingMessageAsync(context, new List<CoachingContext>(), CoachingStyle.Encouraging);

            // Simulate voice output
            var voiceService = new MockVoiceOutputService();
            await voiceService.SpeakAsync(coachingMessage.Content);

            // Trigger the coaching provided event
            OnCoachingProvided(this, coachingMessage);
        }

        // Mock services for demonstration purposes
        public class MockLLMCoachingService : LLMCoachingService
        {
            private readonly Random _random = new();
            
            public MockLLMCoachingService() : base(new LLMConfiguration { ApiKey = "demo-key" })
            {
            }
            
            public override async Task<CoachingMessage> GenerateCoachingMessageAsync(
                CoachingContext context, 
                List<CoachingContext> recentContext, 
                CoachingStyle style)
            {
                await Task.Delay(100); // Simulate API call delay

                var messages = new[]
                {
                    "Great job maintaining your racing line through that section!",
                    "Try braking 10 meters later into the next corner for better lap time.",
                    "Your throttle application is improving - keep it smooth.",
                    "You're carrying good speed through the esses. Well done!",
                    "Consider a slightly tighter line to set up better for the straight.",
                    "Your consistency is excellent. Focus on finding those extra tenths.",
                    "The car has more grip than you think. Trust it in fast corners.",
                    "Smooth inputs are paying off. Your sector time is improving."
                };

                return new CoachingMessage
                {
                    Content = messages[_random.Next(messages.Length)],
                    Priority = (CoachingPriority)_random.Next(0, 3),
                    Type = CoachingMessageType.RealTimeCorrection,
                    CreatedAt = DateTime.Now
                };
            }
        }

        public class MockVoiceOutputService : VoiceOutputService
        {
            public override async Task SpeakAsync(string text)
            {
                Console.WriteLine($"🔊 [Voice Output]: \"{text}\"");
                await Task.Delay(Math.Max(500, text.Length * 50)); // Simulate speech duration
            }
        }

        public class MockRealTimeComparisonService : RealTimeComparisonService
        {
            private readonly Random _random = new();
            
            public MockRealTimeComparisonService() : base(new TrackMapper())
            {
            }
            
            public new RealTimeComparisonMetrics GetCurrentMetrics()
            {
                return new RealTimeComparisonMetrics
                {
                    CurrentLapTimeDelta = -0.6 + _random.NextDouble() * 1.2,
                    TheoreticalBestLapTime = 198.5 + _random.NextDouble() * 2,
                    ConsistencyRating = 75.0 + _random.NextDouble() * 20,
                    PerformanceRating = 80.0 + _random.NextDouble() * 15,
                    LastUpdated = DateTime.Now,
                    SegmentTimeDeltas = new Dictionary<int, double>
                    {
                        { 1, -0.1 + _random.NextDouble() * 0.2 },
                        { 2, -0.2 + _random.NextDouble() * 0.4 },
                        { 3, 0.1 + _random.NextDouble() * 0.3 }
                    }
                };
            }
        }
    }
}
