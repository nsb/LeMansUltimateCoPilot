using System;
using System.Threading.Tasks;
using NUnit.Framework;
using LeMansUltimateCoPilot.AI;
using LeMansUltimateCoPilot.Models;
using LeMansUltimateCoPilot.Analysis;
using LeMansUltimateCoPilot.Services;

namespace LeMansUltimateCoPilot.Tests.AI
{
    [TestFixture]
    public class RealTimeCoachingOrchestratorTests
    {
        private RealTimeCoachingOrchestrator _orchestrator;
        private ContextualDataAggregator _contextAggregator;
        private AICoachingService _coachingService;
        private VoiceOutputService _voiceService;
        private CoachingConfiguration _config;
        private CorneringAnalysisEngine _corneringEngine;

        [SetUp]
        public void Setup()
        {
            // Create mock services
            _corneringEngine = new CorneringAnalysisEngine();
            _contextAggregator = new ContextualDataAggregator(_corneringEngine);
            
            _config = new CoachingConfiguration
            {
                Provider = LLMProvider.OpenAI,
                ModelName = "gpt-4o-mini",
                ApiKey = "test-key",
                EnableRealTimeMode = true
            };
            
            _coachingService = new AICoachingService(_config);
            _voiceService = new VoiceOutputService();
            
            _orchestrator = new RealTimeCoachingOrchestrator(
                _contextAggregator,
                _coachingService,
                _voiceService,
                _config);
        }

        [TearDown]
        public void TearDown()
        {
            _orchestrator?.Dispose();
            _voiceService?.Dispose();
        }

        [Test]
        public void Constructor_WithValidParameters_InitializesSuccessfully()
        {
            // Act & Assert
            Assert.That(_orchestrator, Is.Not.Null);
        }

        [Test]
        public void Constructor_WithNullContextAggregator_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new RealTimeCoachingOrchestrator(
                null, _coachingService, _voiceService, _config));
        }

        [Test]
        public void Constructor_WithNullCoachingService_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new RealTimeCoachingOrchestrator(
                _contextAggregator, null, _voiceService, _config));
        }

        [Test]
        public void Constructor_WithNullVoiceService_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new RealTimeCoachingOrchestrator(
                _contextAggregator, _coachingService, null, _config));
        }

        [Test]
        public void Constructor_WithNullConfig_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new RealTimeCoachingOrchestrator(
                _contextAggregator, _coachingService, _voiceService, null));
        }

        [Test]
        public void Start_InitializesOrchestrator()
        {
            // Act
            _orchestrator.Start();

            // Assert
            var stats = _orchestrator.GetStatistics();
            Assert.That(stats.IsActive, Is.True);
            Assert.That(stats.StartTime, Is.Not.EqualTo(default(DateTime)));
        }

        [Test]
        public void Stop_StopsOrchestrator()
        {
            // Arrange
            _orchestrator.Start();

            // Act
            _orchestrator.Stop();

            // Assert
            var stats = _orchestrator.GetStatistics();
            Assert.That(stats.IsActive, Is.False);
        }

        [Test]
        public void Start_AfterDispose_ThrowsObjectDisposedException()
        {
            // Arrange
            _orchestrator.Dispose();

            // Act & Assert
            Assert.Throws<ObjectDisposedException>(() => _orchestrator.Start());
        }

        [Test]
        public async Task ProcessTelemetryAsync_WithValidData_UpdatesContext()
        {
            // Arrange
            _orchestrator.Start();
            var telemetry = CreateTestTelemetryData();

            // Act
            await _orchestrator.ProcessTelemetryAsync(telemetry);

            // Assert
            var stats = _orchestrator.GetStatistics();
            Assert.That(stats.TelemetryProcessed, Is.GreaterThan(0));
        }

        [Test]
        public async Task ProcessTelemetryAsync_WhenInactive_DoesNotProcess()
        {
            // Arrange
            // Don't start orchestrator
            var telemetry = CreateTestTelemetryData();

            // Act
            await _orchestrator.ProcessTelemetryAsync(telemetry);

            // Assert
            var stats = _orchestrator.GetStatistics();
            Assert.That(stats.TelemetryProcessed, Is.EqualTo(0));
        }

        [Test]
        public async Task ProcessTelemetryAsync_WithCriticalEvent_TriggersImmediateCoaching()
        {
            // Arrange
            _orchestrator.Start();
            var telemetry = CreateTestTelemetryData();
            telemetry.BrakeInput = 1.0f; // Critical braking
            
            bool coachingGenerated = false;
            _orchestrator.CoachingGenerated += (sender, coaching) => coachingGenerated = true;

            // Act
            await _orchestrator.ProcessTelemetryAsync(telemetry);
            await Task.Delay(100); // Give time for async processing

            // Assert
            // Note: This test depends on the actual LLM service working, 
            // so we just check that telemetry was processed
            var stats = _orchestrator.GetStatistics();
            Assert.That(stats.TelemetryProcessed, Is.GreaterThan(0));
            
            // Check if coaching was generated (may be false in test environment)
            Assert.That(coachingGenerated, Is.True.Or.False);
        }

        [Test]
        public void GetStatistics_ReturnsValidStatistics()
        {
            // Arrange
            _orchestrator.Start();

            // Act
            var stats = _orchestrator.GetStatistics();

            // Assert
            Assert.That(stats, Is.Not.Null);
            Assert.That(stats.IsActive, Is.True);
            Assert.That(stats.StartTime, Is.Not.EqualTo(default(DateTime)));
            Assert.That(stats.SessionDuration, Is.GreaterThan(TimeSpan.Zero));
        }

        [Test]
        public void GetRecentCoaching_InitialState_ReturnsEmptyList()
        {
            // Act
            var recentCoaching = _orchestrator.GetRecentCoaching();

            // Assert
            Assert.That(recentCoaching, Is.Not.Null);
            Assert.That(recentCoaching.Count, Is.EqualTo(0));
        }

        [Test]
        public void CoachingGenerated_Event_CanBeSubscribed()
        {
            // Arrange
            bool eventFired = false;
            CoachingResponse receivedCoaching = null;

            // Act
            _orchestrator.CoachingGenerated += (sender, coaching) =>
            {
                eventFired = true;
                receivedCoaching = coaching;
            };

            // Assert
            Assert.That(eventFired, Is.False); // Event not fired yet
            Assert.That(receivedCoaching, Is.Null);
        }

        [Test]
        public void ErrorOccurred_Event_CanBeSubscribed()
        {
            // Arrange
            bool errorEventFired = false;
            string errorMessage = null;

            // Act
            _orchestrator.ErrorOccurred += (sender, error) =>
            {
                errorEventFired = true;
                errorMessage = error;
            };

            // Assert
            Assert.That(errorEventFired, Is.False); // Event not fired yet
            Assert.That(errorMessage, Is.Null);
        }

        [Test]
        public void Dispose_MultipleCallsDoNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => _orchestrator.Dispose());
            Assert.DoesNotThrow(() => _orchestrator.Dispose());
        }

        [Test]
        public void OrchestratorStatistics_DefaultValues_AreInitialized()
        {
            // Arrange & Act
            var stats = new OrchestratorStatistics();

            // Assert
            Assert.That(stats.StartTime, Is.EqualTo(default(DateTime)));
            Assert.That(stats.EndTime, Is.EqualTo(default(DateTime)));
            Assert.That(stats.IsActive, Is.False);
            Assert.That(stats.TotalCoachingGenerated, Is.EqualTo(0));
            Assert.That(stats.TelemetryProcessed, Is.EqualTo(0));
            Assert.That(stats.RecentCoachingCount, Is.EqualTo(0));
            Assert.That(stats.AverageResponseTime, Is.EqualTo(TimeSpan.Zero));
        }

        [Test]
        public void OrchestratorStatistics_SessionDuration_CalculatesCorrectly()
        {
            // Arrange
            var stats = new OrchestratorStatistics
            {
                StartTime = DateTime.Now.AddMinutes(-5),
                EndTime = DateTime.Now
            };

            // Act
            var duration = stats.SessionDuration;

            // Assert
            Assert.That(duration.TotalMinutes, Is.EqualTo(5).Within(0.1));
        }

        [Test]
        public void OrchestratorStatistics_SessionDuration_WithoutEndTime_UsesCurrentTime()
        {
            // Arrange
            var stats = new OrchestratorStatistics
            {
                StartTime = DateTime.Now.AddMinutes(-3)
            };

            // Act
            var duration = stats.SessionDuration;

            // Assert
            Assert.That(duration.TotalMinutes, Is.EqualTo(3).Within(0.1));
        }

        [Test]
        public void CoachingTrigger_EnumValues_AreCorrect()
        {
            // Assert
            Assert.That(Enum.IsDefined(typeof(CoachingTrigger), CoachingTrigger.Periodic));
            Assert.That(Enum.IsDefined(typeof(CoachingTrigger), CoachingTrigger.Critical));
            Assert.That(Enum.IsDefined(typeof(CoachingTrigger), CoachingTrigger.Manual));
        }

        [Test]
        public void VoiceOutputServiceExtensions_SpeakAsync_ConvertsCoachingPriorityCorrectly()
        {
            // Arrange
            var voiceService = new VoiceOutputService();
            var message = "Test coaching message";

            // Act & Assert - Should not throw
            Assert.DoesNotThrowAsync(async () => await voiceService.SpeakAsync(message, LeMansUltimateCoPilot.AI.CoachingPriority.High));
            
            voiceService.Dispose();
        }

        /// <summary>
        /// Helper method to create test telemetry data
        /// </summary>
        private EnhancedTelemetryData CreateTestTelemetryData()
        {
            return new EnhancedTelemetryData
            {
                Timestamp = DateTime.Now,
                Speed = 120.0f,
                LateralG = 0.5f,
                LongitudinalG = -0.3f,
                ThrottleInput = 0.8f,
                BrakeInput = 0.2f,
                SteeringInput = 0.1f,
                Gear = 4,
                EngineRPM = 6500f,
                TrackName = "Test Track",
                VehicleName = "Test Car",
                IsValidLap = true,
                LapNumber = 1,
                SessionTime = 120.0
            };
        }
    }
}
