using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using LeMansUltimateCoPilot.Models;

namespace LeMansUltimateCoPilot.Tests.Models
{
    /// <summary>
    /// Unit tests for ComparisonResult and related classes
    /// </summary>
    [TestFixture]
    public class ComparisonResultTests
    {
        [Test]
        public void ComparisonResult_DefaultValues_AreSetCorrectly()
        {
            var result = new ComparisonResult();

            Assert.That(result.TimeDelta, Is.EqualTo(0));
            Assert.That(result.DistanceFromStart, Is.EqualTo(0));
            Assert.That(result.SpeedDelta, Is.EqualTo(0));
            Assert.That(result.ImprovementAreas.Count, Is.EqualTo(0));
            Assert.That(result.ConfidenceLevel, Is.EqualTo(0));
            Assert.That(result.CurrentTelemetry, Is.Not.Null);
            Assert.That(result.ReferenceTelemetry, Is.Not.Null);
        }

        [Test]
        public void ComparisonResult_PropertyAssignments_WorkCorrectly()
        {
            var result = new ComparisonResult
            {
                TimeDelta = 1.5,
                DistanceFromStart = 1000.0,
                SpeedDelta = 10.5,
                ThrottleDelta = 15.0,
                BrakeDelta = -5.0,
                SteeringDelta = 2.5,
                LongitudinalGDelta = 0.3,
                LateralGDelta = -0.2,
                ConfidenceLevel = 85.0
            };

            Assert.That(result.TimeDelta, Is.EqualTo(1.5).Within(0.001));
            Assert.That(result.DistanceFromStart, Is.EqualTo(1000.0).Within(0.001));
            Assert.That(result.SpeedDelta, Is.EqualTo(10.5).Within(0.001));
            Assert.That(result.ThrottleDelta, Is.EqualTo(15.0).Within(0.001));
            Assert.That(result.BrakeDelta, Is.EqualTo(-5.0).Within(0.001));
            Assert.That(result.ConfidenceLevel, Is.EqualTo(85.0).Within(0.001));
        }

        [Test]
        public void ImprovementArea_Initialization_WorksCorrectly()
        {
            var improvement = new ImprovementArea
            {
                Type = ImprovementType.BrakingPoint,
                Severity = 75.0,
                Message = "Brake 50m later",
                PotentialGain = 0.15,
                DistanceRange = (1000.0, 1100.0)
            };

            Assert.That(improvement.Type, Is.EqualTo(ImprovementType.BrakingPoint));
            Assert.That(improvement.Severity, Is.EqualTo(75.0).Within(0.001));
            Assert.That(improvement.Message, Is.EqualTo("Brake 50m later"));
            Assert.That(improvement.PotentialGain, Is.EqualTo(0.15).Within(0.001));
            Assert.That(improvement.DistanceRange.Start, Is.EqualTo(1000.0));
            Assert.That(improvement.DistanceRange.End, Is.EqualTo(1100.0));
        }

        [Test]
        public void ImprovementType_AllEnumValues_AreValid()
        {
            var types = new[]
            {
                ImprovementType.BrakingPoint,
                ImprovementType.BrakingPressure,
                ImprovementType.ThrottleApplication,
                ImprovementType.ThrottleModulation,
                ImprovementType.CorneringLine,
                ImprovementType.SteeringSmoothing,
                ImprovementType.GearTiming,
                ImprovementType.CornerSpeed,
                ImprovementType.CornerExit,
                ImprovementType.Consistency
            };

            foreach (var type in types)
            {
                var improvement = new ImprovementArea { Type = type };
                Assert.That(improvement.Type, Is.EqualTo(type));
            }
        }

        [Test]
        public void ComparisonResult_WithImprovementAreas_ManagesCollectionCorrectly()
        {
            var result = new ComparisonResult();

            result.ImprovementAreas.Add(new ImprovementArea
            {
                Type = ImprovementType.BrakingPoint,
                Severity = 80.0,
                Message = "Brake later",
                PotentialGain = 0.2
            });

            result.ImprovementAreas.Add(new ImprovementArea
            {
                Type = ImprovementType.ThrottleApplication,
                Severity = 60.0,
                Message = "Apply throttle earlier",
                PotentialGain = 0.1
            });

            Assert.That(result.ImprovementAreas.Count, Is.EqualTo(2));

            var totalGain = result.ImprovementAreas.Sum(i => i.PotentialGain);
            Assert.That(totalGain, Is.EqualTo(0.3).Within(0.001));

            var brakingImprovement = result.ImprovementAreas.FirstOrDefault(i => i.Type == ImprovementType.BrakingPoint);
            Assert.That(brakingImprovement, Is.Not.Null);
            Assert.That(brakingImprovement.Severity, Is.EqualTo(80.0).Within(0.001));
        }

        [Test]
        public void ComparisonResult_WithSegment_AssignsCorrectly()
        {
            var segment = new TrackSegment
            {
                Id = "test-segment",
                SegmentNumber = 10,
                SegmentType = TrackSegmentType.LeftTurn,
                DistanceFromStart = 1000.0,
                SegmentLength = 50.0
            };

            var result = new ComparisonResult
            {
                Segment = segment,
                DistanceFromStart = 1025.0
            };

            Assert.That(result.Segment, Is.Not.Null);
            Assert.That(result.Segment.Id, Is.EqualTo("test-segment"));
            Assert.That(result.Segment.SegmentNumber, Is.EqualTo(10));
            Assert.That(result.Segment.SegmentType, Is.EqualTo(TrackSegmentType.LeftTurn));
        }

        [Test]
        public void ComparisonResult_ConfidenceLevel_AcceptsAllValues()
        {
            var result = new ComparisonResult();

            var validConfidences = new[] { 0.0, 25.0, 50.0, 75.0, 100.0 };
            foreach (var confidence in validConfidences)
            {
                result.ConfidenceLevel = confidence;
                Assert.That(result.ConfidenceLevel, Is.EqualTo(confidence).Within(0.001));
            }

            // Test extreme values (validation should be done elsewhere)
            result.ConfidenceLevel = -10.0;
            Assert.That(result.ConfidenceLevel, Is.EqualTo(-10.0).Within(0.001));

            result.ConfidenceLevel = 150.0;
            Assert.That(result.ConfidenceLevel, Is.EqualTo(150.0).Within(0.001));
        }

        [Test]
        public void ComparisonResult_Timestamp_IsSetCorrectly()
        {
            var beforeCreation = DateTime.Now;
            var result = new ComparisonResult();
            var afterCreation = DateTime.Now;

            Assert.That(result.CalculatedAt, Is.GreaterThanOrEqualTo(beforeCreation));
            Assert.That(result.CalculatedAt, Is.LessThanOrEqualTo(afterCreation));

            var customTime = new DateTime(2023, 1, 1, 12, 0, 0);
            result.CalculatedAt = customTime;
            Assert.That(result.CalculatedAt, Is.EqualTo(customTime));
        }

        [Test]
        public void ComparisonResult_WithTelemetryData_CalculatesCorrectDeltas()
        {
            var currentTelemetry = new EnhancedTelemetryData
            {
                Speed = 120.0f,
                ThrottleInput = 80.0f,
                BrakeInput = 0.0f,
                DistanceFromStart = 1000.0
            };

            var referenceTelemetry = new EnhancedTelemetryData
            {
                Speed = 125.0f,
                ThrottleInput = 85.0f,
                BrakeInput = 0.0f,
                DistanceFromStart = 1000.0
            };

            var result = new ComparisonResult
            {
                CurrentTelemetry = currentTelemetry,
                ReferenceTelemetry = referenceTelemetry,
                SpeedDelta = currentTelemetry.Speed - referenceTelemetry.Speed,
                ThrottleDelta = currentTelemetry.ThrottleInput - referenceTelemetry.ThrottleInput
            };

            Assert.That(result.SpeedDelta, Is.EqualTo(-5.0).Within(0.001));
            Assert.That(result.ThrottleDelta, Is.EqualTo(-5.0).Within(0.001));
            Assert.That(result.CurrentTelemetry.Speed, Is.EqualTo(120.0f).Within(0.001));
            Assert.That(result.ReferenceTelemetry.Speed, Is.EqualTo(125.0f).Within(0.001));
        }
    }
}
