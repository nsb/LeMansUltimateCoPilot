using System;
using System.Collections.Generic;
using NUnit.Framework;
using LeMansUltimateCoPilot.Analysis;
using LeMansUltimateCoPilot.Models;

namespace LeMansUltimateCoPilot.Tests.Analysis
{
    [TestFixture]
    public class CorneringAnalysisModelsTests
    {
        [Test]
        public void Corner_PerformanceScore_WithPerfectDriving_ReturnsHighScore()
        {
            // Arrange
            var corner = new Corner
            {
                EntrySpeed = 100f,
                ApexSpeed = 80f,
                ExitSpeed = 110f, // Good exit speed
                MaxLateralG = 0.8, // Reasonable lateral G
                EntryAnalysis = new CornerPhaseAnalysis
                {
                    MaxBrakeInput = 0.7, // Smooth braking
                    SteeringSmoothness = 0.03 // Very smooth steering
                },
                ExitAnalysis = new CornerPhaseAnalysis
                {
                    ThrottleSmoothness = 0.05, // Smooth throttle
                    SteeringSmoothness = 0.04 // Smooth steering
                }
            };

            // Act
            var score = corner.PerformanceScore;

            // Assert
            Assert.That(score >= 90, Is.True); // Should be a high score for good driving
        }

        [Test]
        public void Corner_PerformanceScore_WithPoorDriving_ReturnsLowScore()
        {
            // Arrange
            var corner = new Corner
            {
                EntrySpeed = 100f,
                ApexSpeed = 80f,
                ExitSpeed = 70f, // Poor exit speed
                MaxLateralG = 1.5, // Excessive lateral G
                EntryAnalysis = new CornerPhaseAnalysis
                {
                    MaxBrakeInput = 0.95, // Harsh braking
                    SteeringSmoothness = 0.15 // Jerky steering
                },
                ExitAnalysis = new CornerPhaseAnalysis
                {
                    ThrottleSmoothness = 0.2, // Harsh throttle
                    SteeringSmoothness = 0.12 // Jerky steering
                }
            };

            // Act
            var score = corner.PerformanceScore;

            // Assert
            Assert.That(score <= 50, Is.True); // Should be a low score for poor driving
        }

        [Test]
        public void Corner_PerformanceScore_WithNullAnalysis_HandlesGracefully()
        {
            // Arrange
            var corner = new Corner
            {
                EntrySpeed = 100f,
                ApexSpeed = 80f,
                ExitSpeed = 95f,
                MaxLateralG = 0.9,
                EntryAnalysis = null, // Null analysis
                ExitAnalysis = null // Null analysis
            };

            // Act
            var score = corner.PerformanceScore;

            // Assert
            Assert.That(score >= 0 && score <= 100, Is.True); // Should still return a valid score
        }

        [Test]
        public void CorneringAnalysisResult_DefaultConstructor_InitializesCorrectly()
        {
            // Act
            var result = new CorneringAnalysisResult();

            // Assert
            Assert.That(result.IsInCorner, Is.False);
            Assert.That(result.CornerPhase, Is.EqualTo(CornerPhase.Unknown));
            Assert.That(result.CornerDirection, Is.EqualTo(CornerDirection.Straight));
            Assert.That(result.CurrentLateralG, Is.EqualTo(0.0));
            Assert.That(result.CurrentSpeed, Is.EqualTo(0.0));
            Assert.That(result.CompletedCorner, Is.Null);
            Assert.That(result.CoachingFeedback, Is.Not.Null);
            Assert.That(result.CoachingFeedback.Count, Is.EqualTo(0));
        }

        [Test]
        public void CoachingFeedback_DefaultConstructor_InitializesCorrectly()
        {
            // Act
            var feedback = new CoachingFeedback();

            // Assert
            Assert.That(feedback.Priority, Is.EqualTo(FeedbackPriority.Low));
            Assert.That(feedback.Category, Is.EqualTo(FeedbackCategory.General));
            Assert.That(feedback.Message, Is.EqualTo(""));
            Assert.That(feedback.Type, Is.EqualTo(FeedbackType.Tip));
            Assert.That(feedback.Timestamp != DateTime.MinValue, Is.True);
        }

        [Test]
        public void CoachingFeedback_WithCriticalPriority_SetsPropertiesCorrectly()
        {
            // Arrange
            var timestamp = DateTime.Now;
            var feedback = new CoachingFeedback
            {
                Priority = FeedbackPriority.Critical,
                Category = FeedbackCategory.Safety,
                Message = "Excessive speed in corner!",
                Type = FeedbackType.Warning,
                Timestamp = timestamp
            };

            // Act & Assert
            Assert.That(feedback.Priority, Is.EqualTo(FeedbackPriority.Critical));
            Assert.That(feedback.Category, Is.EqualTo(FeedbackCategory.Safety));
            Assert.That(feedback.Message, Is.EqualTo("Excessive speed in corner!"));
            Assert.That(feedback.Type, Is.EqualTo(FeedbackType.Warning));
            Assert.That(feedback.Timestamp, Is.EqualTo(timestamp));
        }

        [Test]
        public void CornerPhaseAnalysis_DefaultConstructor_InitializesCorrectly()
        {
            // Act
            var analysis = new CornerPhaseAnalysis();

            // Assert
            Assert.That(analysis.Phase, Is.EqualTo(CornerPhase.Unknown));
            Assert.That(analysis.Duration, Is.EqualTo(0.0));
            Assert.That(analysis.AverageSpeed, Is.EqualTo(0.0));
            Assert.That(analysis.MaxLateralG, Is.EqualTo(0.0));
            Assert.That(analysis.AverageLateralG, Is.EqualTo(0.0));
            Assert.That(analysis.MaxBrakeInput, Is.EqualTo(0.0));
            Assert.That(analysis.AverageBrakeInput, Is.EqualTo(0.0));
            Assert.That(analysis.MaxThrottleInput, Is.EqualTo(0.0));
            Assert.That(analysis.AverageThrottleInput, Is.EqualTo(0.0));
            Assert.That(analysis.SteeringSmoothness, Is.EqualTo(0.0));
            Assert.That(analysis.ThrottleSmoothness, Is.EqualTo(0.0));
            Assert.That(analysis.BrakeSmoothness, Is.EqualTo(0.0));
        }

        [Test]
        public void CornerPhaseAnalysis_WithDataValues_SetsPropertiesCorrectly()
        {
            // Arrange
            var analysis = new CornerPhaseAnalysis
            {
                Phase = CornerPhase.Entry,
                Duration = 2.5,
                AverageSpeed = 85.5,
                MaxLateralG = 0.85,
                AverageLateralG = 0.65,
                MaxBrakeInput = 0.8,
                AverageBrakeInput = 0.4,
                MaxThrottleInput = 0.3,
                AverageThrottleInput = 0.15,
                SteeringSmoothness = 0.08,
                ThrottleSmoothness = 0.05,
                BrakeSmoothness = 0.12
            };

            // Act & Assert
            Assert.That(analysis.Phase, Is.EqualTo(CornerPhase.Entry));
            Assert.That(analysis.Duration, Is.EqualTo(2.5));
            Assert.That(analysis.AverageSpeed, Is.EqualTo(85.5));
            Assert.That(analysis.MaxLateralG, Is.EqualTo(0.85));
            Assert.That(analysis.AverageLateralG, Is.EqualTo(0.65));
            Assert.That(analysis.MaxBrakeInput, Is.EqualTo(0.8));
            Assert.That(analysis.AverageBrakeInput, Is.EqualTo(0.4));
            Assert.That(analysis.MaxThrottleInput, Is.EqualTo(0.3));
            Assert.That(analysis.AverageThrottleInput, Is.EqualTo(0.15));
            Assert.That(analysis.SteeringSmoothness, Is.EqualTo(0.08));
            Assert.That(analysis.ThrottleSmoothness, Is.EqualTo(0.05));
            Assert.That(analysis.BrakeSmoothness, Is.EqualTo(0.12));
        }

        [Test]
        public void CornerState_DefaultConstructor_InitializesCorrectly()
        {
            // Act
            var state = new CornerState();

            // Assert
            Assert.That(state.IsInCorner, Is.False);
            Assert.That(state.Direction, Is.EqualTo(CornerDirection.Straight));
            Assert.That(state.Phase, Is.EqualTo(CornerPhase.Unknown));
            Assert.That(state.LateralG, Is.EqualTo(0.0));
            Assert.That(state.Speed, Is.EqualTo(0.0));
        }

        [Test]
        public void CornerState_WithCornerData_SetsPropertiesCorrectly()
        {
            // Arrange
            var state = new CornerState
            {
                IsInCorner = true,
                Direction = CornerDirection.Left,
                Phase = CornerPhase.Apex,
                LateralG = 0.75,
                Speed = 95.5
            };

            // Act & Assert
            Assert.That(state.IsInCorner, Is.True);
            Assert.That(state.Direction, Is.EqualTo(CornerDirection.Left));
            Assert.That(state.Phase, Is.EqualTo(CornerPhase.Apex));
            Assert.That(state.LateralG, Is.EqualTo(0.75));
            Assert.That(state.Speed, Is.EqualTo(95.5));
        }

        [Test]
        public void Corner_WithCompleteData_CalculatesCorrectDuration()
        {
            // Arrange
            var startTime = DateTime.Now;
            var endTime = startTime.AddSeconds(3.5);
            
            var corner = new Corner
            {
                StartTime = startTime,
                EndTime = endTime,
                Duration = (endTime - startTime).TotalSeconds
            };

            // Act & Assert
            Assert.That(corner.Duration, Is.EqualTo(3.5).Within(0.1));
        }

        [Test]
        public void Corner_WithTelemetryData_StoresDataCorrectly()
        {
            // Arrange
            var telemetryData = new List<EnhancedTelemetryData>
            {
                CreateTestTelemetryData(100f, 0.5f),
                CreateTestTelemetryData(95f, 0.7f),
                CreateTestTelemetryData(90f, 0.8f)
            };

            var corner = new Corner
            {
                Direction = CornerDirection.Right,
                TelemetryData = telemetryData
            };

            // Act & Assert
            Assert.That(corner.Direction, Is.EqualTo(CornerDirection.Right));
            Assert.That(corner.TelemetryData.Count, Is.EqualTo(3));
            Assert.That(corner.TelemetryData[0].Speed, Is.EqualTo(100f));
            Assert.That(corner.TelemetryData[2].LateralG, Is.EqualTo(0.8f));
        }

        [Test]
        public void FeedbackPriority_EnumValues_AreCorrect()
        {
            // Act & Assert
            Assert.That((int)FeedbackPriority.Low, Is.EqualTo(0));
            Assert.That((int)FeedbackPriority.Medium, Is.EqualTo(1));
            Assert.That((int)FeedbackPriority.High, Is.EqualTo(2));
            Assert.That((int)FeedbackPriority.Critical, Is.EqualTo(3));
        }

        [Test]
        public void CornerDirection_EnumValues_AreCorrect()
        {
            // Act & Assert
            Assert.That((int)CornerDirection.Straight, Is.EqualTo(0));
            Assert.That((int)CornerDirection.Left, Is.EqualTo(1));
            Assert.That((int)CornerDirection.Right, Is.EqualTo(2));
        }

        [Test]
        public void CornerPhase_EnumValues_AreCorrect()
        {
            // Act & Assert
            Assert.That((int)CornerPhase.Unknown, Is.EqualTo(0));
            Assert.That((int)CornerPhase.Entry, Is.EqualTo(1));
            Assert.That((int)CornerPhase.Apex, Is.EqualTo(2));
            Assert.That((int)CornerPhase.Exit, Is.EqualTo(3));
        }

        [Test]
        public void Trend_EnumValues_AreCorrect()
        {
            // Act & Assert
            Assert.That((int)Trend.Increasing, Is.EqualTo(0));
            Assert.That((int)Trend.Decreasing, Is.EqualTo(1));
            Assert.That((int)Trend.Stable, Is.EqualTo(2));
        }

        /// <summary>
        /// Helper method to create test telemetry data
        /// </summary>
        private EnhancedTelemetryData CreateTestTelemetryData(float speed, float lateralG)
        {
            return new EnhancedTelemetryData
            {
                Timestamp = DateTime.Now,
                Speed = speed,
                LateralG = lateralG,
                SteeringInput = lateralG * 0.5f,
                ThrottleInput = 0.5f,
                BrakeInput = 0.0f,
                VehicleName = "Test Vehicle",
                TrackName = "Test Track",
                SessionTime = 60f,
                LapNumber = 1,
                LapTime = 60f
            };
        }
    }
}

