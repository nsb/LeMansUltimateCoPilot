using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using LeMansUltimateCoPilot.AI;
using LeMansUltimateCoPilot.Models;
using LeMansUltimateCoPilot.Analysis;

namespace LeMansUltimateCoPilot.Tests.AI
{
    [TestFixture]
    public class AICoachingServiceTests
    {
        private AICoachingService _coachingService;
        private CoachingConfiguration _mockConfig;
        private DrivingContext _testContext;

        [SetUp]
        public void Setup()
        {
            // Create mock configuration for testing
            _mockConfig = new CoachingConfiguration
            {
                Provider = LLMProvider.OpenAI,
                ModelName = "gpt-4o-mini",
                ApiKey = "test-key-for-unit-tests",
                Temperature = 0.3f,
                TopP = 0.9f,
                EnableRealTimeMode = true,
                MaxResponseLength = 20,
                Style = CoachingStyle.Encouraging
            };

            // Create test driving context
            _testContext = CreateTestDrivingContext();
        }

        [Test]
        public void AICoachingService_Constructor_InitializesSuccessfully()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => new AICoachingService(_mockConfig));
        }

        [Test]
        public void AICoachingService_Constructor_NullConfig_ThrowsException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new AICoachingService(null));
        }

        [Test]
        public void AICoachingService_UnsupportedProvider_ThrowsException()
        {
            // Arrange
            _mockConfig.Provider = (LLMProvider)999; // Invalid provider

            // Act & Assert
            Assert.Throws<ArgumentException>(() => new AICoachingService(_mockConfig));
        }

        [Test]
        public void AICoachingService_LocalProvider_ThrowsNotImplementedException()
        {
            // Arrange
            _mockConfig.Provider = LLMProvider.Local;

            // Act & Assert
            Assert.Throws<NotImplementedException>(() => new AICoachingService(_mockConfig));
        }

        [Test]
        public async Task GenerateCoachingAsync_WithValidContext_ReturnsResponse()
        {
            // Arrange
            _coachingService = new AICoachingService(_mockConfig);

            // Act
            var response = await _coachingService.GenerateCoachingAsync(_testContext);

            // Assert
            Assert.That(response, Is.Not.Null);
            Assert.That(response.Message, Is.Not.Null);
            Assert.That(response.Message, Is.Not.Empty);
            Assert.That(response.Timestamp, Is.Not.EqualTo(default(DateTime)));
            Assert.That(response.Priority, Is.TypeOf<CoachingPriority>());
            Assert.That(response.Category, Is.TypeOf<CoachingCategory>());
        }

        [Test]
        public void CoachingConfiguration_DefaultValues_AreSet()
        {
            // Arrange & Act
            var config = new CoachingConfiguration();

            // Assert
            Assert.That(config.Provider, Is.EqualTo(LLMProvider.OpenAI));
            Assert.That(config.ModelName, Is.EqualTo("gpt-4o-mini"));
            Assert.That(config.ApiKey, Is.EqualTo(""));
            Assert.That(config.Temperature, Is.EqualTo(0.3f));
            Assert.That(config.TopP, Is.EqualTo(0.9f));
            Assert.That(config.EnableRealTimeMode, Is.True);
            Assert.That(config.MaxResponseLength, Is.EqualTo(20));
            Assert.That(config.Style, Is.EqualTo(CoachingStyle.Encouraging));
        }

        [Test]
        public void CoachingResponse_DefaultValues_AreSet()
        {
            // Arrange & Act
            var response = new CoachingResponse();

            // Assert
            Assert.That(response.Message, Is.EqualTo(""));
            Assert.That(response.Priority, Is.EqualTo(CoachingPriority.Low));
            Assert.That(response.Category, Is.EqualTo(CoachingCategory.General));
            Assert.That(response.IsError, Is.False);
            Assert.That(response.ErrorDetails, Is.EqualTo(""));
        }

        /// <summary>
        /// Helper method to create test driving context
        /// </summary>
        private DrivingContext CreateTestDrivingContext()
        {
            var telemetry = new EnhancedTelemetryData
            {
                Timestamp = DateTime.Now,
                Speed = 120.0f,
                LateralG = 0.5f,
                LongitudinalG = -0.3f,
                ThrottleInput = 0.8f,
                BrakeInput = 0.0f,
                SteeringInput = 0.1f,
                Gear = 4,
                EngineRPM = 6500f,
                TrackName = "Test Track",
                VehicleName = "Test Car",
                IsValidLap = true
            };

            return new DrivingContext
            {
                CurrentTelemetry = telemetry,
                ReferenceLapData = new ReferenceLapComparison
                {
                    SpeedDelta = 5.0f,
                    SpeedDeltaPercent = 4.2f,
                    BrakingPointDelta = 10.0f,
                    RacingLineDeviation = 2.0f,
                    LapTimeDelta = TimeSpan.FromSeconds(0.5),
                    PerformanceZone = PerformanceZone.Good
                },
                CurrentCornerState = new CornerState
                {
                    IsInCorner = false,
                    Direction = CornerDirection.Left,
                    Phase = CornerPhase.Entry
                },
                CurrentTrack = new TrackContext
                {
                    Name = "Test Track",
                    CurrentSector = 1,
                    NextCorner = "Turn 1",
                    DistanceToNextCorner = 150.0f,
                    TrackConditions = "Dry"
                },
                SessionInfo = new SessionContext
                {
                    CurrentLap = 3,
                    SessionTime = TimeSpan.FromMinutes(5),
                    SessionType = "Practice",
                    BestLapTime = TimeSpan.FromSeconds(85.2)
                },
                RecentEvents = new List<PerformanceEvent>(),
                CurrentPerformance = new PerformanceStatus
                {
                    Level = PerformanceLevel.Good,
                    ConsistencyScore = 0.8f,
                    ImprovementRate = 0.6f,
                    CurrentStrengths = new List<string> { "Smooth braking" },
                    CurrentWeaknesses = new List<string> { "Corner exit" }
                }
            };
        }
    }
}
