using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using LeMansUltimateCoPilot.Models;
using LeMansUltimateCoPilot.Services;

namespace LeMansUltimateCoPilot.Tests.Services
{
    [TestFixture]
    public class LapDetectorTests
    {
        private LapDetector _lapDetector;
        private List<LapCompletedEventArgs> _completedLaps;
        private List<LapStartedEventArgs> _startedLaps;

        [SetUp]
        public void SetUp()
        {
            _lapDetector = new LapDetector();
            _completedLaps = new List<LapCompletedEventArgs>();
            _startedLaps = new List<LapStartedEventArgs>();

            // Subscribe to events
            _lapDetector.LapCompleted += (sender, args) => _completedLaps.Add(args);
            _lapDetector.LapStarted += (sender, args) => _startedLaps.Add(args);
        }

        [Test]
        public void Constructor_ShouldInitializeWithDefaults()
        {
            // Assert
            Assert.That(_lapDetector.CurrentLapNumber, Is.EqualTo(0));
            Assert.That(_lapDetector.IsLapInProgress, Is.False);
            Assert.That(_lapDetector.CurrentLapDataPoints, Is.EqualTo(0));
            Assert.That(_lapDetector.Config, Is.Not.Null);
        }

        [Test]
        public void ProcessTelemetryData_WithNullData_ShouldNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => _lapDetector.ProcessTelemetryData(null!));
        }

        [Test]
        public void ProcessTelemetryData_ShouldStartLap()
        {
            // Arrange
            var telemetry = CreateTelemetryData(1, 0.05, 0.0); // Start of lap

            // Act
            _lapDetector.ProcessTelemetryData(telemetry);

            // Assert
            Assert.That(_lapDetector.IsLapInProgress, Is.True);
            Assert.That(_lapDetector.CurrentLapNumber, Is.EqualTo(1));
            Assert.That(_lapDetector.CurrentLapDataPoints, Is.EqualTo(1));
            Assert.That(_startedLaps.Count, Is.EqualTo(1));
        }

        [Test]
        public void ProcessTelemetryData_ShouldDetectLapCompletion()
        {
            // Arrange - simulate a complete lap
            var telemetryData = CreateLapTelemetryData(1, 60.0); // 60 second lap

            // Act - process all telemetry data
            foreach (var telemetry in telemetryData)
            {
                _lapDetector.ProcessTelemetryData(telemetry);
            }

            // Assert
            Assert.That(_completedLaps.Count, Is.EqualTo(1));
            Assert.That(_completedLaps[0].LapNumber, Is.EqualTo(1));
            Assert.That(_completedLaps[0].LapTime, Is.EqualTo(59.5).Within(0.1));
            Assert.That(_completedLaps[0].IsValid, Is.True);
            Assert.That(_completedLaps[0].TelemetryData.Count, Is.GreaterThan(100));
        }

        [Test]
        public void ProcessTelemetryData_ShouldValidateLapQuality()
        {
            // Arrange - create a short, invalid lap
            var telemetry1 = CreateTelemetryData(1, 0.05, 0.0); // Start
            var telemetry2 = CreateTelemetryData(1, 0.05, 5.0); // End too soon

            // Act
            _lapDetector.ProcessTelemetryData(telemetry1);
            _lapDetector.ProcessTelemetryData(telemetry2);

            // Assert - lap should not be completed due to minimum time requirement
            Assert.That(_completedLaps.Count, Is.EqualTo(0));
        }

        [Test]
        public void Config_ShouldAllowCustomization()
        {
            // Arrange
            _lapDetector.Config.MinimumLapTime = 10.0;
            _lapDetector.Config.MaximumLapTime = 120.0;
            _lapDetector.Config.MinimumDataPoints = 50;

            // Act
            var config = _lapDetector.Config;

            // Assert
            Assert.That(config.MinimumLapTime, Is.EqualTo(10.0));
            Assert.That(config.MaximumLapTime, Is.EqualTo(120.0));
            Assert.That(config.MinimumDataPoints, Is.EqualTo(50));
        }

        [Test]
        public void Reset_ShouldClearState()
        {
            // Arrange - start a lap
            var telemetry = CreateTelemetryData(1, 0.05, 0.0);
            _lapDetector.ProcessTelemetryData(telemetry);

            // Act
            _lapDetector.Reset();

            // Assert
            Assert.That(_lapDetector.IsLapInProgress, Is.False);
            Assert.That(_lapDetector.CurrentLapNumber, Is.EqualTo(0));
            Assert.That(_lapDetector.CurrentLapDataPoints, Is.EqualTo(0));
        }

        [Test]
        public void GetCurrentLapProgress_ShouldReturnCorrectInfo()
        {
            // Arrange
            var telemetry1 = CreateTelemetryData(1, 0.05, 0.0);
            var telemetry2 = CreateTelemetryData(1, 0.25, 10.0);

            // Act
            _lapDetector.ProcessTelemetryData(telemetry1);
            _lapDetector.ProcessTelemetryData(telemetry2);
            var progress = _lapDetector.GetCurrentLapProgress();

            // Assert
            Assert.That(progress.LapNumber, Is.EqualTo(1));
            Assert.That(progress.IsInProgress, Is.True);
            Assert.That(progress.DataPoints, Is.EqualTo(2));
            Assert.That(progress.ElapsedTime, Is.EqualTo(10.0).Within(0.1));
            Assert.That(progress.LastLapProgress, Is.EqualTo(0.25));
        }

        [Test]
        public void LapValidation_ShouldRejectInvalidLaps()
        {
            // Arrange - create lap with invalid flag
            var telemetryData = CreateLapTelemetryData(1, 60.0);
            foreach (var telemetry in telemetryData)
            {
                telemetry.IsValidLap = false; // Mark as invalid
            }

            // Act
            foreach (var telemetry in telemetryData)
            {
                _lapDetector.ProcessTelemetryData(telemetry);
            }

            // Assert
            Assert.That(_completedLaps.Count, Is.EqualTo(1));
            Assert.That(_completedLaps[0].IsValid, Is.False);
        }

        [Test]
        public void LapValidation_ShouldRejectLapsWithInsufficientSpeedVariation()
        {
            // Arrange - create lap with constant speed
            var telemetryData = CreateLapTelemetryData(1, 60.0);
            foreach (var telemetry in telemetryData)
            {
                telemetry.Speed = 100.0f; // Constant speed
            }

            // Act
            foreach (var telemetry in telemetryData)
            {
                _lapDetector.ProcessTelemetryData(telemetry);
            }

            // Assert
            Assert.That(_completedLaps.Count, Is.EqualTo(1));
            Assert.That(_completedLaps[0].IsValid, Is.False);
        }

        [Test]
        public void LapStarted_EventShouldContainCorrectInfo()
        {
            // Arrange
            var telemetry = CreateTelemetryData(1, 0.05, 0.0);

            // Act
            _lapDetector.ProcessTelemetryData(telemetry);

            // Assert
            Assert.That(_startedLaps.Count, Is.EqualTo(1));
            Assert.That(_startedLaps[0].LapNumber, Is.EqualTo(1));
            Assert.That(_startedLaps[0].TrackName, Is.EqualTo("Test Track"));
            Assert.That(_startedLaps[0].VehicleName, Is.EqualTo("Test Vehicle"));
        }

        [Test]
        public void LapCompleted_EventShouldContainCorrectInfo()
        {
            // Arrange
            var telemetryData = CreateLapTelemetryData(1, 60.0);

            // Act
            foreach (var telemetry in telemetryData)
            {
                _lapDetector.ProcessTelemetryData(telemetry);
            }

            // Assert
            Assert.That(_completedLaps.Count, Is.EqualTo(1));
            var completedLap = _completedLaps[0];
            
            Assert.That(completedLap.LapNumber, Is.EqualTo(1));
            Assert.That(completedLap.LapTime, Is.EqualTo(59.5).Within(0.1));
            Assert.That(completedLap.TrackName, Is.EqualTo("Test Track"));
            Assert.That(completedLap.VehicleName, Is.EqualTo("Test Vehicle"));
            Assert.That(completedLap.TelemetryData, Is.Not.Null);
            Assert.That(completedLap.TelemetryData.Count, Is.GreaterThan(0));
        }

        [Test]
        public void LapDetection_ShouldHandleProgressReset()
        {
            // Arrange - simulate progress going from high to low (lap completion)
            var telemetry1 = CreateTelemetryData(1, 0.05, 0.5); // Start of lap
            var telemetry2 = CreateTelemetryData(1, 0.95, 58.0); // Near end of lap  
            var telemetry3 = CreateTelemetryData(1, 0.05, 60.0); // Just after start/finish

            // Act
            _lapDetector.ProcessTelemetryData(telemetry1); // Start lap
            _lapDetector.ProcessTelemetryData(telemetry2); // Continue lap
            _lapDetector.ProcessTelemetryData(telemetry3); // Complete lap

            // Assert - should detect lap completion
            Assert.That(_completedLaps.Count, Is.EqualTo(1));
            Assert.That(_completedLaps[0].LapTime, Is.EqualTo(59.5).Within(0.1));
        }

        // Helper methods
        private EnhancedTelemetryData CreateTelemetryData(int lapNumber, double lapProgress, double lapTime)
        {
            return new EnhancedTelemetryData
            {
                LapNumber = lapNumber,
                LapProgress = (float)lapProgress,
                LapTime = (float)lapTime,
                TrackName = "Test Track",
                VehicleName = "Test Vehicle",
                Speed = (float)(100 + (lapProgress * 50)), // Varying speed
                IsValidLap = true,
                Timestamp = DateTime.Now
            };
        }

        private List<EnhancedTelemetryData> CreateLapTelemetryData(int lapNumber, double lapTime)
        {
            var telemetryData = new List<EnhancedTelemetryData>();
            var dataPoints = 120; // 2 minutes at 10Hz
            
            for (int i = 0; i < dataPoints; i++)
            {
                var progress = (double)i / dataPoints;
                var currentLapTime = lapTime * progress;
                
                // For the last few points, simulate crossing the finish line
                float lapProgress;
                if (i >= dataPoints - 3) // Last 3 data points
                {
                    // Simulate crossing finish line: 0.95 -> 0.98 -> 0.05 (crossing the line)
                    lapProgress = i == dataPoints - 1 ? 0.05f : 0.95f + (i - (dataPoints - 3)) * 0.01f;
                }
                else
                {
                    lapProgress = (float)progress;
                }
                
                var telemetry = new EnhancedTelemetryData
                {
                    LapNumber = lapNumber,
                    LapProgress = lapProgress,
                    LapTime = (float)currentLapTime,
                    TrackName = "Test Track",
                    VehicleName = "Test Vehicle",
                    Speed = (float)(80 + (progress * 80) + (Math.Sin(progress * Math.PI * 4) * 20)), // Varying speed
                    IsValidLap = true,
                    Timestamp = DateTime.Now.AddSeconds(currentLapTime)
                };
                
                telemetryData.Add(telemetry);
            }
            
            return telemetryData;
        }
    }
}
