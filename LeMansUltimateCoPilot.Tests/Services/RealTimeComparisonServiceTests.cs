using LeMansUltimateCoPilot.Models;
using LeMansUltimateCoPilot.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LeMansUltimateCoPilot.Tests.Services
{
    /// <summary>
    /// Unit tests for RealTimeComparisonService
    /// </summary>
    public class RealTimeComparisonServiceTests
    {
        /// <summary>
        /// Test RealTimeComparisonService initialization
        /// </summary>
        public static void TestRealTimeComparisonServiceInitialization()
        {
            var trackMapper = new TrackMapper();
            var service = new RealTimeComparisonService(trackMapper);

            if (service.DistanceTolerance != 10.0)
                throw new Exception($"Default distance tolerance should be 10.0, got {service.DistanceTolerance}");

            if (service.MinimumConfidence != 50.0)
                throw new Exception($"Default minimum confidence should be 50.0, got {service.MinimumConfidence}");

            var metrics = service.GetCurrentMetrics();
            if (metrics == null)
                throw new Exception("Should return valid metrics object");

            var comparisons = service.GetCurrentLapComparisons();
            if (comparisons == null)
                throw new Exception("Should return valid comparisons list");

            if (comparisons.Count != 0)
                throw new Exception("Should start with empty comparisons list");
        }

        /// <summary>
        /// Test RealTimeComparisonService with null track mapper
        /// </summary>
        public static void TestRealTimeComparisonServiceNullTrackMapper()
        {
            try
            {
                var service = new RealTimeComparisonService(null!);
                throw new Exception("Should throw exception for null track mapper");
            }
            catch (ArgumentNullException)
            {
                // Expected
            }
        }

        /// <summary>
        /// Test SetReferenceLap method
        /// </summary>
        public static void TestSetReferenceLap()
        {
            var trackMapper = new TrackMapper();
            var service = new RealTimeComparisonService(trackMapper);

            // Create reference lap with telemetry data
            var referenceLap = new ReferenceLap
            {
                Id = "test-ref-lap",
                LapTime = 90.0,
                TrackName = "Test Track",
                VehicleName = "Test Car"
            };

            // Add telemetry data
            for (int i = 0; i < 10; i++)
            {
                referenceLap.TelemetryData.Add(new EnhancedTelemetryData
                {
                    DistanceFromStart = i * 100.0,
                    Speed = 100.0f + i * 5.0f,
                    LapTime = i * 0.5f,
                    ThrottleInput = 80.0f,
                    BrakeInput = 0.0f
                });
            }

            service.SetReferenceLap(referenceLap);

            var metrics = service.GetCurrentMetrics();
            if (metrics.ReferenceLap == null)
                throw new Exception("Metrics should have reference lap set");

            if (metrics.ReferenceLap.Id != "test-ref-lap")
                throw new Exception($"Reference lap ID should be 'test-ref-lap', got '{metrics.ReferenceLap.Id}'");

            if (Math.Abs(metrics.ReferenceLap.LapTime - 90.0) > 0.001)
                throw new Exception($"Reference lap time should be 90.0, got {metrics.ReferenceLap.LapTime}");
        }

        /// <summary>
        /// Test SetReferenceLap with null reference lap
        /// </summary>
        public static void TestSetReferenceLapNull()
        {
            var trackMapper = new TrackMapper();
            var service = new RealTimeComparisonService(trackMapper);

            try
            {
                service.SetReferenceLap(null!);
                throw new Exception("Should throw exception for null reference lap");
            }
            catch (ArgumentNullException)
            {
                // Expected
            }
        }

        /// <summary>
        /// Test ProcessTelemetry without reference lap
        /// </summary>
        public static void TestProcessTelemetryWithoutReferenceLap()
        {
            var trackMapper = new TrackMapper();
            var service = new RealTimeComparisonService(trackMapper);

            var currentTelemetry = new EnhancedTelemetryData
            {
                DistanceFromStart = 100.0,
                Speed = 105.0f,
                LapTime = 1.0f
            };

            var result = service.ProcessTelemetry(currentTelemetry);
            if (result != null)
                throw new Exception("Should return null when no reference lap is set");
        }

        /// <summary>
        /// Test ProcessTelemetry with reference lap
        /// </summary>
        public static void TestProcessTelemetryWithReferenceLap()
        {
            var trackMapper = new TrackMapper();
            var service = new RealTimeComparisonService(trackMapper);

            // Set up reference lap
            var referenceLap = new ReferenceLap { LapTime = 90.0 };
            referenceLap.TelemetryData.Add(new EnhancedTelemetryData
            {
                DistanceFromStart = 100.0,
                Speed = 100.0f,
                LapTime = 1.0f,
                ThrottleInput = 80.0f,
                BrakeInput = 0.0f,
                SteeringInput = 0.0f
            });

            service.SetReferenceLap(referenceLap);

            // Process current telemetry
            var currentTelemetry = new EnhancedTelemetryData
            {
                DistanceFromStart = 105.0, // Close to reference distance
                Speed = 105.0f,
                LapTime = 1.2f,
                ThrottleInput = 85.0f,
                BrakeInput = 0.0f,
                SteeringInput = 0.0f
            };

            var result = service.ProcessTelemetry(currentTelemetry);
            if (result == null)
                throw new Exception("Should return comparison result");

            if (Math.Abs(result.TimeDelta - 0.2) > 0.001)
                throw new Exception($"Time delta should be 0.2, got {result.TimeDelta}");

            if (Math.Abs(result.SpeedDelta - 5.0) > 0.001)
                throw new Exception($"Speed delta should be 5.0, got {result.SpeedDelta}");

            if (Math.Abs(result.ThrottleDelta - 5.0) > 0.001)
                throw new Exception($"Throttle delta should be 5.0, got {result.ThrottleDelta}");

            if (result.ConfidenceLevel <= 0)
                throw new Exception($"Confidence level should be greater than 0, got {result.ConfidenceLevel}");
        }

        /// <summary>
        /// Test ProcessTelemetry with distance too far from reference
        /// </summary>
        public static void TestProcessTelemetryDistanceTooFar()
        {
            var trackMapper = new TrackMapper();
            var service = new RealTimeComparisonService(trackMapper);

            // Set up reference lap
            var referenceLap = new ReferenceLap { LapTime = 90.0 };
            referenceLap.TelemetryData.Add(new EnhancedTelemetryData
            {
                DistanceFromStart = 100.0,
                Speed = 100.0f,
                LapTime = 1.0f
            });

            service.SetReferenceLap(referenceLap);

            // Process current telemetry with distance too far
            var currentTelemetry = new EnhancedTelemetryData
            {
                DistanceFromStart = 150.0, // 50m away, beyond default 10m tolerance
                Speed = 105.0f,
                LapTime = 1.2f
            };

            var result = service.ProcessTelemetry(currentTelemetry);
            if (result != null)
                throw new Exception("Should return null when distance is too far from reference");
        }

        /// <summary>
        /// Test ProcessTelemetry with custom distance tolerance
        /// </summary>
        public static void TestProcessTelemetryCustomDistanceTolerance()
        {
            var trackMapper = new TrackMapper();
            var service = new RealTimeComparisonService(trackMapper)
            {
                DistanceTolerance = 60.0 // Increase tolerance
            };

            // Set up reference lap
            var referenceLap = new ReferenceLap { LapTime = 90.0 };
            referenceLap.TelemetryData.Add(new EnhancedTelemetryData
            {
                DistanceFromStart = 100.0,
                Speed = 100.0f,
                LapTime = 1.0f
            });

            service.SetReferenceLap(referenceLap);

            // Process current telemetry with distance within new tolerance
            var currentTelemetry = new EnhancedTelemetryData
            {
                DistanceFromStart = 150.0, // 50m away, within 60m tolerance
                Speed = 105.0f,
                LapTime = 1.2f
            };

            var result = service.ProcessTelemetry(currentTelemetry);
            if (result == null)
                throw new Exception("Should return comparison result with custom tolerance");

            if (Math.Abs(result.TimeDelta - 0.2) > 0.001)
                throw new Exception($"Time delta should be 0.2, got {result.TimeDelta}");
        }

        /// <summary>
        /// Test CompleteLap method
        /// </summary>
        public static void TestCompleteLap()
        {
            var trackMapper = new TrackMapper();
            var service = new RealTimeComparisonService(trackMapper);

            // Set up reference lap
            var referenceLap = new ReferenceLap { LapTime = 90.0 };
            referenceLap.TelemetryData.Add(new EnhancedTelemetryData
            {
                DistanceFromStart = 100.0,
                Speed = 100.0f,
                LapTime = 1.0f
            });

            service.SetReferenceLap(referenceLap);

            // Complete a lap
            var lapTime = 91.5; // 1.5 seconds slower
            service.CompleteLap(lapTime);

            var metrics = service.GetCurrentMetrics();
            if (Math.Abs(metrics.CurrentLapTimeDelta - 1.5) > 0.001)
                throw new Exception($"Current lap time delta should be 1.5, got {metrics.CurrentLapTimeDelta}");

            if (metrics.SessionStats.LapsCompleted != 1)
                throw new Exception($"Should have completed 1 lap, got {metrics.SessionStats.LapsCompleted}");

            if (Math.Abs(metrics.SessionStats.BestLapTimeDelta - 1.5) > 0.001)
                throw new Exception($"Best lap time delta should be 1.5, got {metrics.SessionStats.BestLapTimeDelta}");
        }

        /// <summary>
        /// Test CompleteLap without reference lap
        /// </summary>
        public static void TestCompleteLapWithoutReferenceLap()
        {
            var trackMapper = new TrackMapper();
            var service = new RealTimeComparisonService(trackMapper);

            // Complete a lap without setting reference lap (should not crash)
            service.CompleteLap(91.5);

            var metrics = service.GetCurrentMetrics();
            if (metrics.CurrentLapTimeDelta != 0)
                throw new Exception($"Current lap time delta should be 0 without reference, got {metrics.CurrentLapTimeDelta}");
        }

        /// <summary>
        /// Test events are raised during telemetry processing
        /// </summary>
        public static void TestEventsRaisedDuringProcessing()
        {
            var trackMapper = new TrackMapper();
            var service = new RealTimeComparisonService(trackMapper);

            ComparisonResult? receivedComparison = null;
            RealTimeComparisonMetrics? receivedMetrics = null;

            service.ComparisonUpdated += (sender, result) => receivedComparison = result;
            service.MetricsUpdated += (sender, metrics) => receivedMetrics = metrics;

            // Set up reference lap
            var referenceLap = new ReferenceLap { LapTime = 90.0 };
            referenceLap.TelemetryData.Add(new EnhancedTelemetryData
            {
                DistanceFromStart = 100.0,
                Speed = 100.0f,
                LapTime = 1.0f
            });

            service.SetReferenceLap(referenceLap);

            // Process telemetry
            var currentTelemetry = new EnhancedTelemetryData
            {
                DistanceFromStart = 105.0,
                Speed = 105.0f,
                LapTime = 1.2f
            };

            service.ProcessTelemetry(currentTelemetry);

            if (receivedComparison == null)
                throw new Exception("ComparisonUpdated event should have been raised");

            if (receivedMetrics == null)
                throw new Exception("MetricsUpdated event should have been raised");

            if (Math.Abs(receivedComparison.TimeDelta - 0.2) > 0.001)
                throw new Exception($"Received comparison time delta should be 0.2, got {receivedComparison.TimeDelta}");
        }

        /// <summary>
        /// Test confidence level calculation
        /// </summary>
        public static void TestConfidenceLevelCalculation()
        {
            var trackMapper = new TrackMapper();
            var service = new RealTimeComparisonService(trackMapper);

            // Set up reference lap
            var referenceLap = new ReferenceLap { LapTime = 90.0 };
            referenceLap.TelemetryData.Add(new EnhancedTelemetryData
            {
                DistanceFromStart = 100.0,
                Speed = 100.0f,
                LapTime = 1.0f,
                TrackCondition = "Dry"
            });

            service.SetReferenceLap(referenceLap);

            // Test with perfect match
            var perfectMatch = new EnhancedTelemetryData
            {
                DistanceFromStart = 100.0,
                Speed = 100.0f,
                LapTime = 1.0f,
                TrackCondition = "Dry"
            };

            var result = service.ProcessTelemetry(perfectMatch);
            if (result == null)
                throw new Exception("Should return comparison result");

            if (result.ConfidenceLevel < 90)
                throw new Exception($"Confidence should be high for perfect match, got {result.ConfidenceLevel}");

            // Test with different conditions
            var differentConditions = new EnhancedTelemetryData
            {
                DistanceFromStart = 100.0,
                Speed = 100.0f,
                LapTime = 1.0f,
                TrackCondition = "Wet"
            };

            result = service.ProcessTelemetry(differentConditions);
            if (result == null)
                throw new Exception("Should return comparison result");

            if (result.ConfidenceLevel >= 90)
                throw new Exception($"Confidence should be lower for different conditions, got {result.ConfidenceLevel}");
        }

        /// <summary>
        /// Test improvement area analysis
        /// </summary>
        public static void TestImprovementAreaAnalysis()
        {
            var trackMapper = new TrackMapper();
            var service = new RealTimeComparisonService(trackMapper);

            // Set up reference lap
            var referenceLap = new ReferenceLap { LapTime = 90.0 };
            referenceLap.TelemetryData.Add(new EnhancedTelemetryData
            {
                DistanceFromStart = 100.0,
                Speed = 120.0f,
                ThrottleInput = 90.0f,
                BrakeInput = 0.0f,
                SteeringInput = 0.0f
            });

            service.SetReferenceLap(referenceLap);

            // Process telemetry with suboptimal inputs
            var currentTelemetry = new EnhancedTelemetryData
            {
                DistanceFromStart = 105.0,
                Speed = 110.0f,       // 10 km/h slower
                ThrottleInput = 75.0f, // 15% less throttle
                BrakeInput = 15.0f,    // 15% more brake
                SteeringInput = 20.0f  // 20% more steering
            };

            var result = service.ProcessTelemetry(currentTelemetry);
            if (result == null)
                throw new Exception("Should return comparison result");

            if (result.ImprovementAreas.Count == 0)
                throw new Exception("Should identify improvement areas");

            // Check for speed improvement
            var speedImprovement = result.ImprovementAreas.FirstOrDefault(i => i.Type == ImprovementType.CornerSpeed);
            if (speedImprovement == null)
                throw new Exception("Should identify corner speed improvement");

            // Check for throttle improvement
            var throttleImprovement = result.ImprovementAreas.FirstOrDefault(i => i.Type == ImprovementType.ThrottleApplication);
            if (throttleImprovement == null)
                throw new Exception("Should identify throttle application improvement");

            // Check for braking improvement
            var brakeImprovement = result.ImprovementAreas.FirstOrDefault(i => i.Type == ImprovementType.BrakingPressure);
            if (brakeImprovement == null)
                throw new Exception("Should identify braking pressure improvement");

            // Check for steering improvement
            var steeringImprovement = result.ImprovementAreas.FirstOrDefault(i => i.Type == ImprovementType.SteeringSmoothing);
            if (steeringImprovement == null)
                throw new Exception("Should identify steering smoothing improvement");
        }

        /// <summary>
        /// Test segment assignment with track configuration
        /// </summary>
        public static void TestSegmentAssignmentWithTrackConfiguration()
        {
            var trackMapper = new TrackMapper();
            var service = new RealTimeComparisonService(trackMapper);

            // Create track configuration
            var trackConfig = new TrackConfiguration
            {
                TrackName = "Test Track",
                TrackLength = 2000.0
            };

            trackConfig.Segments.Add(new TrackSegment
            {
                Id = "segment-1",
                SegmentNumber = 1,
                DistanceFromStart = 0.0,
                SegmentLength = 200.0,
                SegmentType = TrackSegmentType.Straight
            });

            trackConfig.Segments.Add(new TrackSegment
            {
                Id = "segment-2",
                SegmentNumber = 2,
                DistanceFromStart = 200.0,
                SegmentLength = 150.0,
                SegmentType = TrackSegmentType.LeftTurn
            });

            // Set up reference lap
            var referenceLap = new ReferenceLap { LapTime = 90.0 };
            referenceLap.TelemetryData.Add(new EnhancedTelemetryData
            {
                DistanceFromStart = 250.0,
                Speed = 100.0f,
                LapTime = 1.0f
            });

            service.SetReferenceLap(referenceLap, trackConfig);

            // Process telemetry in second segment
            var currentTelemetry = new EnhancedTelemetryData
            {
                DistanceFromStart = 250.0,
                Speed = 105.0f,
                LapTime = 1.2f
            };

            var result = service.ProcessTelemetry(currentTelemetry);
            if (result == null)
                throw new Exception("Should return comparison result");

            if (result.Segment == null)
                throw new Exception("Should assign segment to result");

            if (result.Segment.Id != "segment-2")
                throw new Exception($"Should assign segment-2, got {result.Segment.Id}");

            if (result.Segment.SegmentType != TrackSegmentType.LeftTurn)
                throw new Exception($"Segment should be LeftTurn type, got {result.Segment.SegmentType}");
        }

        /// <summary>
        /// Test GetCurrentLapComparisons method
        /// </summary>
        public static void TestGetCurrentLapComparisons()
        {
            var trackMapper = new TrackMapper();
            var service = new RealTimeComparisonService(trackMapper);

            // Set up reference lap
            var referenceLap = new ReferenceLap { LapTime = 90.0 };
            referenceLap.TelemetryData.Add(new EnhancedTelemetryData
            {
                DistanceFromStart = 100.0,
                Speed = 100.0f,
                LapTime = 1.0f
            });

            referenceLap.TelemetryData.Add(new EnhancedTelemetryData
            {
                DistanceFromStart = 200.0,
                Speed = 110.0f,
                LapTime = 2.0f
            });

            service.SetReferenceLap(referenceLap);

            // Process multiple telemetry points
            service.ProcessTelemetry(new EnhancedTelemetryData
            {
                DistanceFromStart = 105.0,
                Speed = 105.0f,
                LapTime = 1.2f
            });

            service.ProcessTelemetry(new EnhancedTelemetryData
            {
                DistanceFromStart = 205.0,
                Speed = 115.0f,
                LapTime = 2.3f
            });

            var comparisons = service.GetCurrentLapComparisons();
            if (comparisons.Count != 2)
                throw new Exception($"Should have 2 comparisons, got {comparisons.Count}");

            if (Math.Abs(comparisons[0].TimeDelta - 0.2) > 0.001)
                throw new Exception($"First comparison time delta should be 0.2, got {comparisons[0].TimeDelta}");

            if (Math.Abs(comparisons[1].TimeDelta - 0.3) > 0.001)
                throw new Exception($"Second comparison time delta should be 0.3, got {comparisons[1].TimeDelta}");
        }

        /// <summary>
        /// Run all RealTimeComparisonService tests
        /// </summary>
        public static void RunAllTests()
        {
            TestRealTimeComparisonServiceInitialization();
            TestRealTimeComparisonServiceNullTrackMapper();
            TestSetReferenceLap();
            TestSetReferenceLapNull();
            TestProcessTelemetryWithoutReferenceLap();
            TestProcessTelemetryWithReferenceLap();
            TestProcessTelemetryDistanceTooFar();
            TestProcessTelemetryCustomDistanceTolerance();
            TestCompleteLap();
            TestCompleteLapWithoutReferenceLap();
            TestEventsRaisedDuringProcessing();
            TestConfidenceLevelCalculation();
            TestImprovementAreaAnalysis();
            TestSegmentAssignmentWithTrackConfiguration();
            TestGetCurrentLapComparisons();
        }
    }
}
