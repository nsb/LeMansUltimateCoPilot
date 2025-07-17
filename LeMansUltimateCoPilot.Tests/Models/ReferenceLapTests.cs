using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using LeMansUltimateCoPilot.Models;

namespace LeMansUltimateCoPilot.Tests.Models
{
    [TestFixture]
    public class ReferenceLapTests
    {
        private ReferenceLap _referenceLap;
        private List<EnhancedTelemetryData> _sampleTelemetryData;

        [SetUp]
        public void SetUp()
        {
            // Create sample telemetry data for a lap
            _sampleTelemetryData = new List<EnhancedTelemetryData>();
            
            var baseTime = DateTime.Now;
            for (int i = 0; i < 100; i++)
            {
                var telemetry = new EnhancedTelemetryData
                {
                    Timestamp = baseTime.AddSeconds(i * 0.1),
                    LapNumber = 1,
                    LapTime = i * 0.1,
                    TrackName = "Test Track",
                    VehicleName = "Test Vehicle",
                    Speed = 100 + (i % 60), // Varying speed 100-159 (60 km/h range)
                    ThrottleInput = 0.8f,
                    BrakeInput = i > 80 ? 0.6f : 0.0f, // Brake near end
                    SteeringInput = (float)Math.Sin(i * 0.1) * 0.5f,
                    LongitudinalG = 0.5f - (i > 80 ? 1.5f : 0.0f), // Braking G-force
                    LateralG = (float)Math.Sin(i * 0.1) * 2.0f,
                    FuelLevel = 50 - (i * 0.1f),
                    IsValidLap = true,
                    LapProgress = (float)(i / 100.0),
                    DistanceTraveled = i * 50f // Approximate distance
                };
                _sampleTelemetryData.Add(telemetry);
            }

            _referenceLap = new ReferenceLap(_sampleTelemetryData, 1);
        }

        [Test]
        public void Constructor_ShouldInitializeWithDefaults()
        {
            // Arrange & Act
            var lap = new ReferenceLap();

            // Assert
            Assert.That(lap.Id, Is.Not.Null.And.Not.Empty);
            Assert.That(lap.TrackName, Is.EqualTo(""));
            Assert.That(lap.VehicleName, Is.EqualTo(""));
            Assert.That(lap.LapTime, Is.EqualTo(0.0));
            Assert.That(lap.IsValid, Is.True);
            Assert.That(lap.TelemetryData, Is.Not.Null);
            Assert.That(lap.Performance, Is.Not.Null);
        }

        [Test]
        public void Constructor_WithTelemetryData_ShouldInitializeCorrectly()
        {
            // Assert
            Assert.That(_referenceLap.TrackName, Is.EqualTo("Test Track"));
            Assert.That(_referenceLap.VehicleName, Is.EqualTo("Test Vehicle"));
            Assert.That(_referenceLap.LapNumber, Is.EqualTo(1));
            Assert.That(_referenceLap.LapTime, Is.EqualTo(9.9).Within(0.01));
            Assert.That(_referenceLap.TelemetryData.Count, Is.EqualTo(100));
            Assert.That(_referenceLap.IsValid, Is.True);
        }

        [Test]
        public void Constructor_WithNullTelemetryData_ShouldThrow()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => new ReferenceLap(null!, 1));
        }

        [Test]
        public void Constructor_WithEmptyTelemetryData_ShouldThrow()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => new ReferenceLap(new List<EnhancedTelemetryData>(), 1));
        }

        [Test]
        public void CalculatePerformanceMetrics_ShouldCalculateCorrectly()
        {
            // Assert
            var performance = _referenceLap.Performance;
            
            Assert.That(performance.MaxSpeed, Is.EqualTo(159)); // 100 + 59 (max in cycle)
            Assert.That(performance.MinSpeed, Is.EqualTo(100)); // Base speed
            Assert.That(performance.AverageSpeed, Is.GreaterThan(100));
            Assert.That(performance.MaxThrottle, Is.EqualTo(0.8f));
            Assert.That(performance.MaxBrake, Is.EqualTo(0.6f));
            Assert.That(performance.FuelUsed, Is.EqualTo(9.9f).Within(0.01));
            Assert.That(performance.DistanceTraveled, Is.EqualTo(4950)); // 99 * 50
        }

        [Test]
        public void CalculateSectorTimes_ShouldCalculateCorrectly()
        {
            // Arrange
            var sectorPositions = new List<double> { 0.33, 0.66 }; // 3 sectors

            // Act
            var sectorTimes = _referenceLap.CalculateSectorTimes(sectorPositions);

            // Assert
            Assert.That(sectorTimes.Count, Is.EqualTo(3)); // 3 sectors
            Assert.That(sectorTimes.All(t => t > 0), Is.True);
            Assert.That(sectorTimes.Sum(), Is.EqualTo(_referenceLap.LapTime).Within(0.1));
        }

        [Test]
        public void CalculateSectorTimes_WithNullPositions_ShouldReturnEmpty()
        {
            // Act
            var sectorTimes = _referenceLap.CalculateSectorTimes(null!);

            // Assert
            Assert.That(sectorTimes, Is.Empty);
        }

        [Test]
        public void ValidateQuality_WithValidData_ShouldReturnTrue()
        {
            // Act
            var isValid = _referenceLap.ValidateQuality();

            // Assert
            Assert.That(isValid, Is.True);
        }

        [Test]
        public void ValidateQuality_WithInsufficientData_ShouldReturnFalse()
        {
            // Arrange
            var shortData = _sampleTelemetryData.Take(50).ToList();
            var shortLap = new ReferenceLap(shortData, 1);

            // Act
            var isValid = shortLap.ValidateQuality();

            // Assert
            Assert.That(isValid, Is.False);
        }

        [Test]
        public void ValidateQuality_WithInvalidLapTime_ShouldReturnFalse()
        {
            // Arrange
            _referenceLap.LapTime = 0; // Invalid lap time

            // Act
            var isValid = _referenceLap.ValidateQuality();

            // Assert
            Assert.That(isValid, Is.False);
        }

        [Test]
        public void ValidateQuality_WithInvalidFlag_ShouldReturnFalse()
        {
            // Arrange
            _referenceLap.IsValid = false;

            // Act
            var isValid = _referenceLap.ValidateQuality();

            // Assert
            Assert.That(isValid, Is.False);
        }

        [Test]
        public void ToJson_ShouldSerializeCorrectly()
        {
            // Act
            var json = _referenceLap.ToJson();

            // Assert
            Assert.That(json, Is.Not.Null.And.Not.Empty);
            Assert.That(json, Does.Contain("trackName"));
            Assert.That(json, Does.Contain("Test Track"));
            Assert.That(json, Does.Contain("lapTime"));
        }

        [Test]
        public void FromJson_ShouldDeserializeCorrectly()
        {
            // Arrange
            var json = _referenceLap.ToJson();

            // Act
            var deserializedLap = ReferenceLap.FromJson(json);

            // Assert
            Assert.That(deserializedLap.TrackName, Is.EqualTo(_referenceLap.TrackName));
            Assert.That(deserializedLap.VehicleName, Is.EqualTo(_referenceLap.VehicleName));
            Assert.That(deserializedLap.LapTime, Is.EqualTo(_referenceLap.LapTime).Within(0.01));
            Assert.That(deserializedLap.TelemetryData.Count, Is.EqualTo(_referenceLap.TelemetryData.Count));
        }

        [Test]
        public void FromJson_WithInvalidJson_ShouldReturnDefault()
        {
            // Act
            var deserializedLap = ReferenceLap.FromJson("invalid json");

            // Assert
            Assert.That(deserializedLap, Is.Not.Null);
            Assert.That(deserializedLap.TrackName, Is.EqualTo(""));
        }

        [Test]
        public void GetSummary_ShouldReturnFormattedString()
        {
            // Act
            var summary = _referenceLap.GetSummary();

            // Assert
            Assert.That(summary, Does.Contain("Test Track"));
            Assert.That(summary, Does.Contain("Test Vehicle"));
            Assert.That(summary, Does.Contain("9.900s"));
            Assert.That(summary, Does.Contain("159.0 km/h"));
            Assert.That(summary, Does.Contain("100"));
        }

        [Test]
        public void Performance_ShouldTrackGForces()
        {
            // Assert
            var performance = _referenceLap.Performance;
            
            Assert.That(performance.MaxLongitudinalG, Is.EqualTo(0.5f));
            Assert.That(performance.MinLongitudinalG, Is.EqualTo(-1.0f)); // 0.5f - 1.5f = -1.0f
            Assert.That(performance.MaxLateralG, Is.GreaterThan(0));
        }

        [Test]
        public void Performance_ShouldTrackInputs()
        {
            // Assert
            var performance = _referenceLap.Performance;
            
            Assert.That(performance.MaxThrottle, Is.EqualTo(0.8f));
            Assert.That(performance.MaxBrake, Is.EqualTo(0.6f));
            Assert.That(performance.MaxSteering, Is.GreaterThan(0));
        }

        [Test]
        public void Performance_ShouldTrackTireTemperature()
        {
            // Assert
            var performance = _referenceLap.Performance;
            
            // Since our sample data has default tire temps (0), average should be 0
            Assert.That(performance.AvgTireTemperature, Is.EqualTo(0.0));
        }

        [Test]
        public void Id_ShouldBeUnique()
        {
            // Arrange
            var lap1 = new ReferenceLap();
            var lap2 = new ReferenceLap();

            // Assert
            Assert.That(lap1.Id, Is.Not.EqualTo(lap2.Id));
        }

        [Test]
        public void RecordedAt_ShouldBeSetFromTelemetry()
        {
            // Assert
            Assert.That(_referenceLap.RecordedAt, Is.EqualTo(_sampleTelemetryData.First().Timestamp));
        }
    }
}

