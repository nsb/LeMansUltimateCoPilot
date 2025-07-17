using System;
using System.Collections.Generic;
using NUnit.Framework;
using LeMansUltimateCoPilot.AI;
using LeMansUltimateCoPilot.Models;
using LeMansUltimateCoPilot.Analysis;

namespace LeMansUltimateCoPilot.Tests.AI
{
    [TestFixture]
    public class ContextualDataAggregatorTests
    {
        private ContextualDataAggregator _aggregator;
        private CorneringAnalysisEngine _corneringEngine;

        [SetUp]
        public void SetUp()
        {
            _corneringEngine = new CorneringAnalysisEngine();
            _aggregator = new ContextualDataAggregator(_corneringEngine);
        }

        [Test]
        public void ProcessTelemetryData_WithBasicData_GeneratesContext()
        {
            // Arrange
            var telemetry = CreateTestTelemetryData();

            // Act
            var context = _aggregator.ProcessTelemetryData(telemetry);

            // Assert
            Assert.That(context, Is.Not.Null);
            Assert.That(context.CurrentTelemetry, Is.EqualTo(telemetry));
            Assert.That(context.CurrentCornerState, Is.Not.Null);
            Assert.That(context.ReferenceLapData, Is.Not.Null);
            Assert.That(context.RecentEvents, Is.Not.Null);
            Assert.That(context.CurrentTrack, Is.Not.Null);
            Assert.That(context.SessionInfo, Is.Not.Null);
            Assert.That(context.CurrentPerformance, Is.Not.Null);
        }

        [Test]
        public void ProcessTelemetryData_WithMultipleDataPoints_TracksPerformanceEvents()
        {
            // Arrange - Create sequence with harsh braking
            var telemetrySequence = new List<EnhancedTelemetryData>();
            
            for (int i = 0; i < 10; i++)
            {
                var telemetry = CreateTestTelemetryData();
                telemetry.BrakeInput = i < 5 ? 0.3f : 0.95f; // Harsh braking in second half
                telemetrySequence.Add(telemetry);
            }

            // Act
            DrivingContext context = null;
            foreach (var telemetry in telemetrySequence)
            {
                context = _aggregator.ProcessTelemetryData(telemetry);
            }

            // Assert
            Assert.That(context, Is.Not.Null);
            Assert.That(context.RecentEvents.Count, Is.GreaterThan(0));
            Assert.That(context.RecentEvents.Exists(e => e.Description.Contains("braking")), Is.True);
        }

        [Test]
        public void DrivingContext_ToNaturalLanguage_GeneratesReadableContext()
        {
            // Arrange
            var telemetry = CreateTestTelemetryData();
            var context = _aggregator.ProcessTelemetryData(telemetry);

            // Act
            var naturalLanguageContext = context.ToNaturalLanguage();

            // Assert
            Assert.That(naturalLanguageContext, Is.Not.Null);
            Assert.That(naturalLanguageContext, Is.Not.Empty);
            Assert.That(naturalLanguageContext, Contains.Substring("CURRENT SITUATION"));
            Assert.That(naturalLanguageContext, Contains.Substring("REFERENCE LAP COMPARISON"));
            Assert.That(naturalLanguageContext, Contains.Substring("TRACK CONTEXT"));
            Assert.That(naturalLanguageContext, Contains.Substring("SESSION CONTEXT"));
            
            // Should contain telemetry data
            Assert.That(naturalLanguageContext, Contains.Substring("Speed:"));
            Assert.That(naturalLanguageContext, Contains.Substring("Lateral G:"));
            Assert.That(naturalLanguageContext, Contains.Substring("Brake Input:"));
            
            Console.WriteLine("Generated Natural Language Context:");
            Console.WriteLine(naturalLanguageContext);
        }

        [Test]
        public void ContextualDataAggregator_WithReferenceLap_ComparesPerformance()
        {
            // Arrange
            var referenceLap = new ReferenceLapData
            {
                TrackName = "Le Mans Circuit",
                LapTime = TimeSpan.FromMinutes(3.5)
            };
            
            _aggregator.AddReferenceLap("Le Mans Circuit", referenceLap);
            var telemetry = CreateTestTelemetryData();

            // Act
            var context = _aggregator.ProcessTelemetryData(telemetry);

            // Assert
            Assert.That(context.ReferenceLapData, Is.Not.Null);
            Assert.That(context.ReferenceLapData.PerformanceZone, Is.EqualTo(PerformanceZone.Optimal));
        }

        [Test]
        public void PerformanceStatus_CalculatesConsistencyScore()
        {
            // Arrange - Create consistent telemetry data
            var telemetrySequence = new List<EnhancedTelemetryData>();
            
            for (int i = 0; i < 150; i++)
            {
                var telemetry = CreateTestTelemetryData();
                telemetry.Speed = 100f + (float)(Math.Sin(i * 0.1) * 2); // Small speed variation
                telemetry.BrakeInput = 0.5f + (float)(Math.Sin(i * 0.05) * 0.1); // Small brake variation
                telemetrySequence.Add(telemetry);
            }

            // Act
            DrivingContext context = null;
            foreach (var telemetry in telemetrySequence)
            {
                context = _aggregator.ProcessTelemetryData(telemetry);
            }

            // Assert
            Assert.That(context.CurrentPerformance.ConsistencyScore, Is.GreaterThan(0.0f));
            Assert.That(context.CurrentPerformance.ConsistencyScore, Is.LessThanOrEqualTo(1.0f));
        }

        /// <summary>
        /// Helper method to create test telemetry data
        /// </summary>
        private EnhancedTelemetryData CreateTestTelemetryData()
        {
            return new EnhancedTelemetryData
            {
                Timestamp = DateTime.Now,
                Speed = 100f,
                LateralG = 0.3f,
                LongitudinalG = -0.2f,
                SteeringInput = -0.1f,
                ThrottleInput = 0.7f,
                BrakeInput = 0.0f,
                Gear = 4,
                EngineRPM = 6000f,
                PositionX = 100f,
                PositionY = 0f,
                PositionZ = 200f,
                VelocityX = 25f,
                VelocityY = 0f,
                VelocityZ = 15f,
                AccelerationX = 0f,
                AccelerationY = 0f,
                AccelerationZ = -2f,
                TireTemperatureFL = 80f,
                TireTemperatureFR = 80f,
                TireTemperatureRL = 85f,
                TireTemperatureRR = 85f,
                FuelLevel = 50f,
                LapNumber = 1,
                SessionTime = 120.0,
                TrackName = "Test Track",
                VehicleName = "Test Car",
                IsValidLap = true
            };
        }
    }
}
