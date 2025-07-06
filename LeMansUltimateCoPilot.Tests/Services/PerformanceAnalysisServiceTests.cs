using LeMansUltimateCoPilot.Models;
using LeMansUltimateCoPilot.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LeMansUltimateCoPilot.Tests.Services
{
    /// <summary>
    /// Unit tests for PerformanceAnalysisService
    /// </summary>
    public class PerformanceAnalysisServiceTests
    {
        /// <summary>
        /// Test PerformanceAnalysisService initialization
        /// </summary>
        public static void TestPerformanceAnalysisServiceInitialization()
        {
            var service = new PerformanceAnalysisService();
            
            // Test with empty comparisons
            var summary = service.AnalyzePerformance(new List<ComparisonResult>());
            if (summary == null)
                throw new Exception("Should return valid summary for empty comparisons");

            if (summary.TotalComparisons != 0)
                throw new Exception($"Total comparisons should be 0, got {summary.TotalComparisons}");

            if (summary.CoachingRecommendations.Count != 0)
                throw new Exception($"Should have no coaching recommendations for empty data, got {summary.CoachingRecommendations.Count}");
        }

        /// <summary>
        /// Test PerformanceAnalysisService with null comparisons
        /// </summary>
        public static void TestPerformanceAnalysisServiceNullComparisons()
        {
            var service = new PerformanceAnalysisService();
            
            var summary = service.AnalyzePerformance(null);
            if (summary == null)
                throw new Exception("Should return valid summary for null comparisons");

            if (summary.TotalComparisons != 0)
                throw new Exception($"Total comparisons should be 0, got {summary.TotalComparisons}");
        }

        /// <summary>
        /// Test basic performance analysis
        /// </summary>
        public static void TestBasicPerformanceAnalysis()
        {
            var service = new PerformanceAnalysisService();
            
            var comparisons = new List<ComparisonResult>
            {
                new ComparisonResult
                {
                    TimeDelta = 0.5,
                    SpeedDelta = -5.0,
                    ThrottleDelta = -10.0,
                    BrakeDelta = 5.0,
                    SteeringDelta = 0.0
                },
                new ComparisonResult
                {
                    TimeDelta = -0.2,
                    SpeedDelta = 3.0,
                    ThrottleDelta = 5.0,
                    BrakeDelta = -2.0,
                    SteeringDelta = 0.0
                },
                new ComparisonResult
                {
                    TimeDelta = 0.8,
                    SpeedDelta = -8.0,
                    ThrottleDelta = -15.0,
                    BrakeDelta = 10.0,
                    SteeringDelta = 5.0
                }
            };

            var summary = service.AnalyzePerformance(comparisons);
            if (summary.TotalComparisons != 3)
                throw new Exception($"Total comparisons should be 3, got {summary.TotalComparisons}");

            var expectedAvgTimeDelta = (0.5 + (-0.2) + 0.8) / 3; // ≈ 0.367
            if (Math.Abs(summary.AverageTimeDelta - expectedAvgTimeDelta) > 0.001)
                throw new Exception($"Average time delta should be {expectedAvgTimeDelta:F3}, got {summary.AverageTimeDelta:F3}");

            var expectedTotalTimeLost = 0.5 + 0.8; // 1.3
            if (Math.Abs(summary.TotalTimeLost - expectedTotalTimeLost) > 0.001)
                throw new Exception($"Total time lost should be {expectedTotalTimeLost}, got {summary.TotalTimeLost}");

            var expectedTotalTimeGained = 0.2;
            if (Math.Abs(summary.TotalTimeGained - expectedTotalTimeGained) > 0.001)
                throw new Exception($"Total time gained should be {expectedTotalTimeGained}, got {summary.TotalTimeGained}");

            var expectedAvgSpeedDelta = (-5.0 + 3.0 + (-8.0)) / 3; // ≈ -3.333
            if (Math.Abs(summary.AverageSpeedDelta - expectedAvgSpeedDelta) > 0.01)
                throw new Exception($"Average speed delta should be {expectedAvgSpeedDelta:F3}, got {summary.AverageSpeedDelta:F3}");

            if (summary.MaxSpeedDeficit != -8.0)
                throw new Exception($"Max speed deficit should be -8.0, got {summary.MaxSpeedDeficit}");

            if (summary.MaxSpeedAdvantage != 3.0)
                throw new Exception($"Max speed advantage should be 3.0, got {summary.MaxSpeedAdvantage}");
        }

        /// <summary>
        /// Test input analysis
        /// </summary>
        public static void TestInputAnalysis()
        {
            var service = new PerformanceAnalysisService();
            
            var comparisons = new List<ComparisonResult>
            {
                new ComparisonResult
                {
                    ThrottleDelta = -10.0,
                    BrakeDelta = 15.0,
                    SteeringDelta = 20.0
                },
                new ComparisonResult
                {
                    ThrottleDelta = -20.0,
                    BrakeDelta = 25.0,
                    SteeringDelta = 30.0
                },
                new ComparisonResult
                {
                    ThrottleDelta = 5.0,
                    BrakeDelta = -5.0,
                    SteeringDelta = 0.0
                }
            };

            var summary = service.AnalyzePerformance(comparisons);

            // Check throttle analysis
            if (summary.ThrottleAnalysis == null)
                throw new Exception("Should have throttle analysis");

            var expectedThrottleAvg = (-10.0 + (-20.0) + 5.0) / 3; // ≈ -8.333
            if (Math.Abs(summary.ThrottleAnalysis.AverageDelta - expectedThrottleAvg) > 0.01)
                throw new Exception($"Throttle average delta should be {expectedThrottleAvg:F3}, got {summary.ThrottleAnalysis.AverageDelta:F3}");

            if (summary.ThrottleAnalysis.MaxDeficit != -20.0)
                throw new Exception($"Throttle max deficit should be -20.0, got {summary.ThrottleAnalysis.MaxDeficit}");

            if (summary.ThrottleAnalysis.MaxExcess != 5.0)
                throw new Exception($"Throttle max excess should be 5.0, got {summary.ThrottleAnalysis.MaxExcess}");

            if (summary.ThrottleAnalysis.ProblematicSections != 1) // Only -20.0 is > 15%
                throw new Exception($"Throttle problematic sections should be 1, got {summary.ThrottleAnalysis.ProblematicSections}");

            // Check brake analysis
            if (summary.BrakeAnalysis == null)
                throw new Exception("Should have brake analysis");

            var expectedBrakeAvg = (15.0 + 25.0 + (-5.0)) / 3; // ≈ 11.667
            if (Math.Abs(summary.BrakeAnalysis.AverageDelta - expectedBrakeAvg) > 0.01)
                throw new Exception($"Brake average delta should be {expectedBrakeAvg:F3}, got {summary.BrakeAnalysis.AverageDelta:F3}");

            if (summary.BrakeAnalysis.ProblematicSections != 1) // Only 25.0 is > 20%
                throw new Exception($"Brake problematic sections should be 1, got {summary.BrakeAnalysis.ProblematicSections}");

            // Check steering analysis
            if (summary.SteeringAnalysis == null)
                throw new Exception("Should have steering analysis");

            var expectedSteeringAvg = (20.0 + 30.0 + 0.0) / 3; // ≈ 16.667
            if (Math.Abs(summary.SteeringAnalysis.AverageDelta - expectedSteeringAvg) > 0.01)
                throw new Exception($"Steering average delta should be {expectedSteeringAvg:F3}, got {summary.SteeringAnalysis.AverageDelta:F3}");

            if (summary.SteeringAnalysis.ProblematicSections != 1) // Only 30.0 is > 25%
                throw new Exception($"Steering problematic sections should be 1, got {summary.SteeringAnalysis.ProblematicSections}");
        }

        /// <summary>
        /// Test improvement areas identification
        /// </summary>
        public static void TestImprovementAreasIdentification()
        {
            var service = new PerformanceAnalysisService();
            
            var comparisons = new List<ComparisonResult>
            {
                new ComparisonResult
                {
                    ImprovementAreas = new List<ImprovementArea>
                    {
                        new ImprovementArea
                        {
                            Type = ImprovementType.BrakingPoint,
                            Severity = 80.0,
                            PotentialGain = 0.3,
                            DistanceRange = (100.0, 200.0)
                        },
                        new ImprovementArea
                        {
                            Type = ImprovementType.ThrottleApplication,
                            Severity = 60.0,
                            PotentialGain = 0.2,
                            DistanceRange = (300.0, 400.0)
                        }
                    }
                },
                new ComparisonResult
                {
                    ImprovementAreas = new List<ImprovementArea>
                    {
                        new ImprovementArea
                        {
                            Type = ImprovementType.BrakingPoint,
                            Severity = 70.0,
                            PotentialGain = 0.25,
                            DistanceRange = (500.0, 600.0)
                        },
                        new ImprovementArea
                        {
                            Type = ImprovementType.CornerSpeed,
                            Severity = 90.0,
                            PotentialGain = 0.4,
                            DistanceRange = (700.0, 800.0)
                        }
                    }
                }
            };

            var summary = service.AnalyzePerformance(comparisons);

            if (summary.ImprovementAreas.Count != 3)
                throw new Exception($"Should have 3 improvement area types, got {summary.ImprovementAreas.Count}");

            // Should be sorted by total potential gain descending
            var sortedAreas = summary.ImprovementAreas.OrderByDescending(a => a.TotalPotentialGain).ToList();
            
            if (sortedAreas[0].Type != ImprovementType.BrakingPoint)
                throw new Exception($"First improvement should be BrakingPoint, got {sortedAreas[0].Type}");

            if (Math.Abs(sortedAreas[0].TotalPotentialGain - 0.55) > 0.001) // 0.3 + 0.25
                throw new Exception($"BrakingPoint total gain should be 0.55, got {sortedAreas[0].TotalPotentialGain}");

            if (sortedAreas[0].Frequency != 2)
                throw new Exception($"BrakingPoint frequency should be 2, got {sortedAreas[0].Frequency}");

            if (Math.Abs(sortedAreas[0].AverageSeverity - 75.0) > 0.001) // (80 + 70) / 2
                throw new Exception($"BrakingPoint average severity should be 75.0, got {sortedAreas[0].AverageSeverity}");

            if (sortedAreas[0].MaxSeverity != 80.0)
                throw new Exception($"BrakingPoint max severity should be 80.0, got {sortedAreas[0].MaxSeverity}");

            if (sortedAreas[0].AffectedSections != 2)
                throw new Exception($"BrakingPoint affected sections should be 2, got {sortedAreas[0].AffectedSections}");
        }

        /// <summary>
        /// Test consistency analysis
        /// </summary>
        public static void TestConsistencyAnalysis()
        {
            var service = new PerformanceAnalysisService();
            
            var segment1 = new TrackSegment { Id = "seg1" };
            var segment2 = new TrackSegment { Id = "seg2" };
            
            var comparisons = new List<ComparisonResult>
            {
                // Segment 1 - consistent times
                new ComparisonResult { TimeDelta = 0.5, Segment = segment1 },
                new ComparisonResult { TimeDelta = 0.52, Segment = segment1 },
                new ComparisonResult { TimeDelta = 0.48, Segment = segment1 },
                
                // Segment 2 - inconsistent times
                new ComparisonResult { TimeDelta = 0.2, Segment = segment2 },
                new ComparisonResult { TimeDelta = 0.8, Segment = segment2 },
                new ComparisonResult { TimeDelta = 0.1, Segment = segment2 }
            };

            var summary = service.AnalyzePerformance(comparisons);

            if (summary.ConsistencyScore <= 0)
                throw new Exception($"Consistency score should be greater than 0, got {summary.ConsistencyScore}");

            if (summary.ConsistentSections == 0)
                throw new Exception($"Should have at least 1 consistent section, got {summary.ConsistentSections}");

            if (summary.InconsistentSections == 0)
                throw new Exception($"Should have at least 1 inconsistent section, got {summary.InconsistentSections}");
        }

        /// <summary>
        /// Test coaching recommendations generation
        /// </summary>
        public static void TestCoachingRecommendationsGeneration()
        {
            var service = new PerformanceAnalysisService();
            
            var comparisons = new List<ComparisonResult>
            {
                new ComparisonResult
                {
                    TimeDelta = 2.0, // Significantly slower
                    SpeedDelta = -10.0, // Significantly slower speed
                    ThrottleDelta = -20.0,
                    BrakeDelta = 25.0,
                    SteeringDelta = 30.0,
                    ImprovementAreas = new List<ImprovementArea>
                    {
                        new ImprovementArea
                        {
                            Type = ImprovementType.BrakingPoint,
                            PotentialGain = 0.5
                        }
                    }
                }
            };

            // Add problematic sections for inputs
            for (int i = 0; i < 10; i++)
            {
                comparisons.Add(new ComparisonResult
                {
                    ThrottleDelta = -20.0,
                    BrakeDelta = 25.0,
                    SteeringDelta = 30.0
                });
            }

            var summary = service.AnalyzePerformance(comparisons);

            if (summary.CoachingRecommendations.Count == 0)
                throw new Exception("Should generate coaching recommendations");

            var recommendations = summary.CoachingRecommendations;
            
            // Check for time-based recommendation
            var timeRecommendation = recommendations.FirstOrDefault(r => r.Contains("lap time"));
            if (timeRecommendation == null)
                throw new Exception("Should have lap time recommendation");

            // Check for speed-based recommendation
            var speedRecommendation = recommendations.FirstOrDefault(r => r.Contains("speed"));
            if (speedRecommendation == null)
                throw new Exception("Should have speed recommendation");

            // Check for throttle recommendation
            var throttleRecommendation = recommendations.FirstOrDefault(r => r.Contains("throttle"));
            if (throttleRecommendation == null)
                throw new Exception("Should have throttle recommendation");

            // Check for braking recommendation
            var brakeRecommendation = recommendations.FirstOrDefault(r => r.Contains("brake") || r.Contains("braking"));
            if (brakeRecommendation == null)
                throw new Exception("Should have braking recommendation");

            // Check for steering recommendation
            var steeringRecommendation = recommendations.FirstOrDefault(r => r.Contains("steering"));
            if (steeringRecommendation == null)
                throw new Exception("Should have steering recommendation");

            // Check for improvement area recommendation
            var improvementRecommendation = recommendations.FirstOrDefault(r => r.Contains("Priority improvement"));
            if (improvementRecommendation == null)
                throw new Exception("Should have priority improvement recommendation");
        }

        /// <summary>
        /// Test performance analysis with mixed results
        /// </summary>
        public static void TestPerformanceAnalysisWithMixedResults()
        {
            var service = new PerformanceAnalysisService();
            
            var comparisons = new List<ComparisonResult>
            {
                // Some good results
                new ComparisonResult { TimeDelta = -0.1, SpeedDelta = 2.0 },
                new ComparisonResult { TimeDelta = -0.05, SpeedDelta = 1.5 },
                
                // Some bad results
                new ComparisonResult { TimeDelta = 0.8, SpeedDelta = -5.0 },
                new ComparisonResult { TimeDelta = 1.2, SpeedDelta = -8.0 },
                
                // Some average results
                new ComparisonResult { TimeDelta = 0.1, SpeedDelta = 0.5 },
                new ComparisonResult { TimeDelta = 0.0, SpeedDelta = 0.0 }
            };

            var summary = service.AnalyzePerformance(comparisons);

            if (summary.TotalComparisons != 6)
                throw new Exception($"Should have 6 total comparisons, got {summary.TotalComparisons}");

            if (summary.TotalTimeLost <= 0)
                throw new Exception($"Should have some time lost, got {summary.TotalTimeLost}");

            if (summary.TotalTimeGained <= 0)
                throw new Exception($"Should have some time gained, got {summary.TotalTimeGained}");

            if (summary.MaxSpeedDeficit >= 0)
                throw new Exception($"Should have negative max speed deficit, got {summary.MaxSpeedDeficit}");

            if (summary.MaxSpeedAdvantage <= 0)
                throw new Exception($"Should have positive max speed advantage, got {summary.MaxSpeedAdvantage}");
        }

        /// <summary>
        /// Test InputAnalysis class
        /// </summary>
        public static void TestInputAnalysisClass()
        {
            var inputAnalysis = new InputAnalysis
            {
                AverageDelta = 5.0,
                MaxDeficit = -10.0,
                MaxExcess = 15.0,
                ProblematicSections = 3
            };

            if (Math.Abs(inputAnalysis.AverageDelta - 5.0) > 0.001)
                throw new Exception($"AverageDelta should be 5.0, got {inputAnalysis.AverageDelta}");

            if (Math.Abs(inputAnalysis.MaxDeficit - (-10.0)) > 0.001)
                throw new Exception($"MaxDeficit should be -10.0, got {inputAnalysis.MaxDeficit}");

            if (Math.Abs(inputAnalysis.MaxExcess - 15.0) > 0.001)
                throw new Exception($"MaxExcess should be 15.0, got {inputAnalysis.MaxExcess}");

            if (inputAnalysis.ProblematicSections != 3)
                throw new Exception($"ProblematicSections should be 3, got {inputAnalysis.ProblematicSections}");
        }

        /// <summary>
        /// Test ImprovementTypeAnalysis class
        /// </summary>
        public static void TestImprovementTypeAnalysisClass()
        {
            var typeAnalysis = new ImprovementTypeAnalysis
            {
                Type = ImprovementType.BrakingPoint,
                Frequency = 5,
                AverageSeverity = 75.0,
                MaxSeverity = 90.0,
                TotalPotentialGain = 1.2,
                AffectedSections = 3
            };

            if (typeAnalysis.Type != ImprovementType.BrakingPoint)
                throw new Exception($"Type should be BrakingPoint, got {typeAnalysis.Type}");

            if (typeAnalysis.Frequency != 5)
                throw new Exception($"Frequency should be 5, got {typeAnalysis.Frequency}");

            if (Math.Abs(typeAnalysis.AverageSeverity - 75.0) > 0.001)
                throw new Exception($"AverageSeverity should be 75.0, got {typeAnalysis.AverageSeverity}");

            if (Math.Abs(typeAnalysis.MaxSeverity - 90.0) > 0.001)
                throw new Exception($"MaxSeverity should be 90.0, got {typeAnalysis.MaxSeverity}");

            if (Math.Abs(typeAnalysis.TotalPotentialGain - 1.2) > 0.001)
                throw new Exception($"TotalPotentialGain should be 1.2, got {typeAnalysis.TotalPotentialGain}");

            if (typeAnalysis.AffectedSections != 3)
                throw new Exception($"AffectedSections should be 3, got {typeAnalysis.AffectedSections}");
        }

        /// <summary>
        /// Test PerformanceAnalysisSummary timestamp
        /// </summary>
        public static void TestPerformanceAnalysisSummaryTimestamp()
        {
            var service = new PerformanceAnalysisService();
            
            var beforeAnalysis = DateTime.Now;
            var summary = service.AnalyzePerformance(new List<ComparisonResult>());
            var afterAnalysis = DateTime.Now;

            if (summary.AnalysisTimestamp < beforeAnalysis || summary.AnalysisTimestamp > afterAnalysis)
                throw new Exception("AnalysisTimestamp should be set during analysis");
        }

        /// <summary>
        /// Run all PerformanceAnalysisService tests
        /// </summary>
        public static void RunAllTests()
        {
            TestPerformanceAnalysisServiceInitialization();
            TestPerformanceAnalysisServiceNullComparisons();
            TestBasicPerformanceAnalysis();
            TestInputAnalysis();
            TestImprovementAreasIdentification();
            TestConsistencyAnalysis();
            TestCoachingRecommendationsGeneration();
            TestPerformanceAnalysisWithMixedResults();
            TestInputAnalysisClass();
            TestImprovementTypeAnalysisClass();
            TestPerformanceAnalysisSummaryTimestamp();
        }
    }
}
