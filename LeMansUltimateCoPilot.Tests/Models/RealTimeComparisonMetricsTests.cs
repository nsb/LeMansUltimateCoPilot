using LeMansUltimateCoPilot.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LeMansUltimateCoPilot.Tests.Models
{
    /// <summary>
    /// Unit tests for RealTimeComparisonMetrics and related classes
    /// </summary>
    public class RealTimeComparisonMetricsTests
    {
        /// <summary>
        /// Test RealTimeComparisonMetrics initialization
        /// </summary>
        public static void TestRealTimeComparisonMetricsInitialization()
        {
            var metrics = new RealTimeComparisonMetrics();

            if (metrics.CurrentLapTimeDelta != 0)
                throw new Exception("CurrentLapTimeDelta should be 0 by default");

            if (metrics.TheoreticalBestLapTime != 0)
                throw new Exception("TheoreticalBestLapTime should be 0 by default");

            if (metrics.ConsistencyRating != 0)
                throw new Exception("ConsistencyRating should be 0 by default");

            if (metrics.SegmentTimeDeltas.Count != 0)
                throw new Exception("SegmentTimeDeltas should be empty by default");

            if (metrics.ActiveImprovements.Count != 0)
                throw new Exception("ActiveImprovements should be empty by default");

            if (metrics.SessionStats == null)
                throw new Exception("SessionStats should be initialized");
        }

        /// <summary>
        /// Test RealTimeComparisonMetrics property assignments
        /// </summary>
        public static void TestRealTimeComparisonMetricsPropertyAssignments()
        {
            var metrics = new RealTimeComparisonMetrics
            {
                CurrentLapTimeDelta = 1.5,
                TheoreticalBestLapTime = 90.5,
                ConsistencyRating = 75.0,
                PerformanceRating = 80.0
            };

            if (Math.Abs(metrics.CurrentLapTimeDelta - 1.5) > 0.001)
                throw new Exception($"CurrentLapTimeDelta should be 1.5, got {metrics.CurrentLapTimeDelta}");

            if (Math.Abs(metrics.TheoreticalBestLapTime - 90.5) > 0.001)
                throw new Exception($"TheoreticalBestLapTime should be 90.5, got {metrics.TheoreticalBestLapTime}");

            if (Math.Abs(metrics.ConsistencyRating - 75.0) > 0.001)
                throw new Exception($"ConsistencyRating should be 75.0, got {metrics.ConsistencyRating}");

            if (Math.Abs(metrics.PerformanceRating - 80.0) > 0.001)
                throw new Exception($"PerformanceRating should be 80.0, got {metrics.PerformanceRating}");
        }

        /// <summary>
        /// Test segment time deltas management
        /// </summary>
        public static void TestSegmentTimeDeltas()
        {
            var metrics = new RealTimeComparisonMetrics();

            // Add segment time deltas
            metrics.SegmentTimeDeltas[1] = 0.5;
            metrics.SegmentTimeDeltas[2] = -0.3;
            metrics.SegmentTimeDeltas[3] = 0.8;

            if (metrics.SegmentTimeDeltas.Count != 3)
                throw new Exception($"Should have 3 segment time deltas, got {metrics.SegmentTimeDeltas.Count}");

            if (Math.Abs(metrics.SegmentTimeDeltas[1] - 0.5) > 0.001)
                throw new Exception($"Segment 1 delta should be 0.5, got {metrics.SegmentTimeDeltas[1]}");

            if (Math.Abs(metrics.SegmentTimeDeltas[2] - (-0.3)) > 0.001)
                throw new Exception($"Segment 2 delta should be -0.3, got {metrics.SegmentTimeDeltas[2]}");

            if (Math.Abs(metrics.SegmentTimeDeltas[3] - 0.8) > 0.001)
                throw new Exception($"Segment 3 delta should be 0.8, got {metrics.SegmentTimeDeltas[3]}");
        }

        /// <summary>
        /// Test best and worst segment times tracking
        /// </summary>
        public static void TestBestWorstSegmentTimes()
        {
            var metrics = new RealTimeComparisonMetrics();

            // Add best segment times
            metrics.BestSegmentTimes[1] = -0.2;
            metrics.BestSegmentTimes[2] = -0.5;
            metrics.BestSegmentTimes[3] = 0.1;

            // Add worst segment times
            metrics.WorstSegmentTimes[1] = 0.8;
            metrics.WorstSegmentTimes[2] = 0.3;
            metrics.WorstSegmentTimes[3] = 1.2;

            if (metrics.BestSegmentTimes.Count != 3)
                throw new Exception($"Should have 3 best segment times, got {metrics.BestSegmentTimes.Count}");

            if (metrics.WorstSegmentTimes.Count != 3)
                throw new Exception($"Should have 3 worst segment times, got {metrics.WorstSegmentTimes.Count}");

            if (Math.Abs(metrics.BestSegmentTimes[1] - (-0.2)) > 0.001)
                throw new Exception($"Best segment 1 time should be -0.2, got {metrics.BestSegmentTimes[1]}");

            if (Math.Abs(metrics.WorstSegmentTimes[1] - 0.8) > 0.001)
                throw new Exception($"Worst segment 1 time should be 0.8, got {metrics.WorstSegmentTimes[1]}");
        }

        /// <summary>
        /// Test improvement areas management
        /// </summary>
        public static void TestImprovementAreas()
        {
            var metrics = new RealTimeComparisonMetrics();

            var improvement1 = new ImprovementArea
            {
                Type = ImprovementType.BrakingPoint,
                Severity = 80.0,
                PotentialGain = 0.3
            };

            var improvement2 = new ImprovementArea
            {
                Type = ImprovementType.ThrottleApplication,
                Severity = 60.0,
                PotentialGain = 0.2
            };

            metrics.ActiveImprovements.Add(improvement1);
            metrics.ActiveImprovements.Add(improvement2);

            if (metrics.ActiveImprovements.Count != 2)
                throw new Exception($"Should have 2 active improvements, got {metrics.ActiveImprovements.Count}");

            var totalGain = metrics.ActiveImprovements.Sum(i => i.PotentialGain);
            if (Math.Abs(totalGain - 0.5) > 0.001)
                throw new Exception($"Total potential gain should be 0.5, got {totalGain}");
        }

        /// <summary>
        /// Test CalculateOverallPerformance method
        /// </summary>
        public static void TestCalculateOverallPerformance()
        {
            var metrics = new RealTimeComparisonMetrics();

            // Test with empty segment deltas
            var performance = metrics.CalculateOverallPerformance();
            if (Math.Abs(performance - 0) > 0.001)
                throw new Exception($"Performance should be 0 with no segment deltas, got {performance}");

            // Test with reference lap but no segment deltas
            metrics.ReferenceLap = new ReferenceLap { LapTime = 90.0 };
            performance = metrics.CalculateOverallPerformance();
            if (Math.Abs(performance - 0) > 0.001)
                throw new Exception($"Performance should be 0 with no segment deltas, got {performance}");

            // Test with segment deltas
            metrics.SegmentTimeDeltas[1] = 0.5;  // 0.5s slower
            metrics.SegmentTimeDeltas[2] = -0.3; // 0.3s faster
            metrics.SegmentTimeDeltas[3] = 0.2;  // 0.2s slower
            // Total delta: 0.4fs slower

            performance = metrics.CalculateOverallPerformance();
            var expectedPerformance = Math.Max(0, (90.0 - 0.4) / 90.0) * 100; // Should be about 99.56%
            if (Math.Abs(performance - expectedPerformance) > 0.1)
                throw new Exception($"Performance should be {expectedPerformance:F2}%, got {performance:F2}%");
        }

        /// <summary>
        /// Test GetTopImprovements method
        /// </summary>
        public static void TestGetTopImprovements()
        {
            var metrics = new RealTimeComparisonMetrics();

            // Add improvements with different potential gains
            metrics.ActiveImprovements.Add(new ImprovementArea
            {
                Type = ImprovementType.BrakingPoint,
                PotentialGain = 0.3
            });

            metrics.ActiveImprovements.Add(new ImprovementArea
            {
                Type = ImprovementType.ThrottleApplication,
                PotentialGain = 0.5
            });

            metrics.ActiveImprovements.Add(new ImprovementArea
            {
                Type = ImprovementType.CornerSpeed,
                PotentialGain = 0.2
            });

            metrics.ActiveImprovements.Add(new ImprovementArea
            {
                Type = ImprovementType.SteeringSmoothing,
                PotentialGain = 0.4
            });

            var topImprovements = metrics.GetTopImprovements(2);
            if (topImprovements.Count != 2)
                throw new Exception($"Should return 2 top improvements, got {topImprovements.Count}");

            // Should be sorted by potential gain descending
            if (topImprovements[0].Type != ImprovementType.ThrottleApplication)
                throw new Exception($"First improvement should be ThrottleApplication, got {topImprovements[0].Type}");

            if (topImprovements[1].Type != ImprovementType.SteeringSmoothing)
                throw new Exception($"Second improvement should be SteeringSmoothing, got {topImprovements[1].Type}");

            if (Math.Abs(topImprovements[0].PotentialGain - 0.5) > 0.001)
                throw new Exception($"First improvement gain should be 0.5, got {topImprovements[0].PotentialGain}");
        }

        /// <summary>
        /// Test CalculateConsistencyRating method
        /// </summary>
        public static void TestCalculateConsistencyRating()
        {
            var metrics = new RealTimeComparisonMetrics();

            // Test with less than 2 segment deltas
            var consistency = metrics.CalculateConsistencyRating();
            if (Math.Abs(consistency - 100) > 0.001)
                throw new Exception($"Consistency should be 100% with insufficient data, got {consistency}");

            // Test with identical segment deltas (perfect consistency)
            metrics.SegmentTimeDeltas[1] = 0.5;
            metrics.SegmentTimeDeltas[2] = 0.5;
            metrics.SegmentTimeDeltas[3] = 0.5;

            consistency = metrics.CalculateConsistencyRating();
            if (Math.Abs(consistency - 100) > 0.001)
                throw new Exception($"Consistency should be 100% with identical deltas, got {consistency}");

            // Test with varied segment deltas
            metrics.SegmentTimeDeltas[1] = 0.0;
            metrics.SegmentTimeDeltas[2] = 1.0;
            metrics.SegmentTimeDeltas[3] = 0.5;
            // Standard deviation should be about 0.408

            consistency = metrics.CalculateConsistencyRating();
            if (consistency < 70 || consistency > 90)
                throw new Exception($"Consistency should be between 70-90% with moderate variation, got {consistency}");
        }

        /// <summary>
        /// Test SessionComparisonStats initialization
        /// </summary>
        public static void TestSessionComparisonStatsInitialization()
        {
            var stats = new SessionComparisonStats();

            if (stats.LapsCompleted != 0)
                throw new Exception("LapsCompleted should be 0 by default");

            if (stats.BestLapTimeDelta != double.MaxValue)
                throw new Exception("BestLapTimeDelta should be MaxValue by default");

            if (stats.WorstLapTimeDelta != double.MinValue)
                throw new Exception("WorstLapTimeDelta should be MinValue by default");

            if (stats.AverageLapTimeDelta != 0)
                throw new Exception("AverageLapTimeDelta should be 0 by default");

            if (stats.ImprovementTrend != 0)
                throw new Exception("ImprovementTrend should be 0 by default");
        }

        /// <summary>
        /// Test SessionComparisonStats UpdateWithLapData method
        /// </summary>
        public static void TestUpdateWithLapData()
        {
            var stats = new SessionComparisonStats();

            // Add first lap
            stats.UpdateWithLapData(1.5);
            if (stats.LapsCompleted != 1)
                throw new Exception($"LapsCompleted should be 1, got {stats.LapsCompleted}");

            if (Math.Abs(stats.BestLapTimeDelta - 1.5) > 0.001)
                throw new Exception($"BestLapTimeDelta should be 1.5, got {stats.BestLapTimeDelta}");

            if (Math.Abs(stats.WorstLapTimeDelta - 1.5) > 0.001)
                throw new Exception($"WorstLapTimeDelta should be 1.5, got {stats.WorstLapTimeDelta}");

            if (Math.Abs(stats.AverageLapTimeDelta - 1.5) > 0.001)
                throw new Exception($"AverageLapTimeDelta should be 1.5, got {stats.AverageLapTimeDelta}");

            // Add second lap (better)
            stats.UpdateWithLapData(0.8);
            if (stats.LapsCompleted != 2)
                throw new Exception($"LapsCompleted should be 2, got {stats.LapsCompleted}");

            if (Math.Abs(stats.BestLapTimeDelta - 0.8) > 0.001)
                throw new Exception($"BestLapTimeDelta should be 0.8, got {stats.BestLapTimeDelta}");

            if (Math.Abs(stats.WorstLapTimeDelta - 1.5) > 0.001)
                throw new Exception($"WorstLapTimeDelta should be 1.5, got {stats.WorstLapTimeDelta}");

            var expectedAverage = (1.5 + 0.8) / 2;
            if (Math.Abs(stats.AverageLapTimeDelta - expectedAverage) > 0.001)
                throw new Exception($"AverageLapTimeDelta should be {expectedAverage}, got {stats.AverageLapTimeDelta}");

            // Add third lap (worse)
            stats.UpdateWithLapData(2.1);
            if (stats.LapsCompleted != 3)
                throw new Exception($"LapsCompleted should be 3, got {stats.LapsCompleted}");

            if (Math.Abs(stats.BestLapTimeDelta - 0.8) > 0.001)
                throw new Exception($"BestLapTimeDelta should be 0.8, got {stats.BestLapTimeDelta}");

            if (Math.Abs(stats.WorstLapTimeDelta - 2.1) > 0.001)
                throw new Exception($"WorstLapTimeDelta should be 2.1, got {stats.WorstLapTimeDelta}");

            expectedAverage = (1.5 + 0.8 + 2.1) / 3;
            if (Math.Abs(stats.AverageLapTimeDelta - expectedAverage) > 0.001)
                throw new Exception($"AverageLapTimeDelta should be {expectedAverage}, got {stats.AverageLapTimeDelta}");
        }

        /// <summary>
        /// Test problematic and strong segments management
        /// </summary>
        public static void TestProblematicAndStrongSegments()
        {
            var metrics = new RealTimeComparisonMetrics();

            var segment1 = new TrackSegment { Id = "seg1", SegmentNumber = 1 };
            var segment2 = new TrackSegment { Id = "seg2", SegmentNumber = 2 };
            var segment3 = new TrackSegment { Id = "seg3", SegmentNumber = 3 };

            metrics.ProblematicSegments.Add(segment1);
            metrics.ProblematicSegments.Add(segment2);
            metrics.StrongSegments.Add(segment3);

            if (metrics.ProblematicSegments.Count != 2)
                throw new Exception($"Should have 2 problematic segments, got {metrics.ProblematicSegments.Count}");

            if (metrics.StrongSegments.Count != 1)
                throw new Exception($"Should have 1 strong segment, got {metrics.StrongSegments.Count}");

            if (metrics.ProblematicSegments[0].Id != "seg1")
                throw new Exception($"First problematic segment should be 'seg1', got '{metrics.ProblematicSegments[0].Id}'");

            if (metrics.StrongSegments[0].Id != "seg3")
                throw new Exception($"First strong segment should be 'seg3', got '{metrics.StrongSegments[0].Id}'");
        }

        /// <summary>
        /// Test LastUpdated timestamp
        /// </summary>
        public static void TestLastUpdatedTimestamp()
        {
            var beforeCreation = DateTime.Now;
            var metrics = new RealTimeComparisonMetrics();
            var afterCreation = DateTime.Now;

            if (metrics.LastUpdated < beforeCreation || metrics.LastUpdated > afterCreation)
                throw new Exception("LastUpdated should be set to current time during creation");

            var customTime = new DateTime(2023, 1, 1, 12, 0, 0);
            metrics.LastUpdated = customTime;
            if (metrics.LastUpdated != customTime)
                throw new Exception("Should allow custom LastUpdated time");
        }

        /// <summary>
        /// Run all RealTimeComparisonMetrics tests
        /// </summary>
        public static void RunAllTests()
        {
            TestRealTimeComparisonMetricsInitialization();
            TestRealTimeComparisonMetricsPropertyAssignments();
            TestSegmentTimeDeltas();
            TestBestWorstSegmentTimes();
            TestImprovementAreas();
            TestCalculateOverallPerformance();
            TestGetTopImprovements();
            TestCalculateConsistencyRating();
            TestSessionComparisonStatsInitialization();
            TestUpdateWithLapData();
            TestProblematicAndStrongSegments();
            TestLastUpdatedTimestamp();
        }
    }
}

