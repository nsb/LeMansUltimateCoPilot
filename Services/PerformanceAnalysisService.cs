using System;
using System.Collections.Generic;
using System.Linq;
using LeMansUltimateCoPilot.Models;

namespace LeMansUltimateCoPilot.Services
{
    /// <summary>
    /// Service for analyzing performance data and generating coaching insights
    /// Provides detailed analysis of telemetry data for improvement recommendations
    /// </summary>
    public class PerformanceAnalysisService
    {
        /// <summary>
        /// Analyze a series of comparison results to identify patterns and trends
        /// </summary>
        /// <param name="comparisons">List of comparison results</param>
        /// <returns>Performance analysis summary</returns>
        public PerformanceAnalysisSummary AnalyzePerformance(List<ComparisonResult> comparisons)
        {
            if (comparisons == null || !comparisons.Any())
                return new PerformanceAnalysisSummary();

            var summary = new PerformanceAnalysisSummary
            {
                TotalComparisons = comparisons.Count,
                AnalysisTimestamp = DateTime.Now
            };

            // Overall performance metrics
            summary.AverageTimeDelta = comparisons.Average(c => c.TimeDelta);
            summary.TotalTimeLost = comparisons.Where(c => c.TimeDelta > 0).Sum(c => c.TimeDelta);
            summary.TotalTimeGained = Math.Abs(comparisons.Where(c => c.TimeDelta < 0).Sum(c => c.TimeDelta));

            // Speed analysis
            summary.AverageSpeedDelta = comparisons.Average(c => c.SpeedDelta);
            summary.MaxSpeedDeficit = comparisons.Where(c => c.SpeedDelta < 0).DefaultIfEmpty().Min(c => c?.SpeedDelta ?? 0);
            summary.MaxSpeedAdvantage = comparisons.Where(c => c.SpeedDelta > 0).DefaultIfEmpty().Max(c => c?.SpeedDelta ?? 0);

            // Input analysis
            AnalyzeInputs(comparisons, summary);

            // Identify improvement areas
            IdentifyImprovementAreas(comparisons, summary);

            // Analyze consistency
            AnalyzeConsistency(comparisons, summary);

            // Generate coaching recommendations
            GenerateCoachingRecommendations(summary);

            return summary;
        }

        /// <summary>
        /// Analyze driver inputs (throttle, brake, steering)
        /// </summary>
        /// <param name="comparisons">Comparison results</param>
        /// <param name="summary">Summary to update</param>
        private void AnalyzeInputs(List<ComparisonResult> comparisons, PerformanceAnalysisSummary summary)
        {
            // Throttle analysis
            var throttleComparisons = comparisons.Where(c => Math.Abs(c.ThrottleDelta) > 5).ToList();
            if (throttleComparisons.Any())
            {
                summary.ThrottleAnalysis = new InputAnalysis
                {
                    AverageDelta = throttleComparisons.Average(c => c.ThrottleDelta),
                    MaxDeficit = throttleComparisons.Where(c => c.ThrottleDelta < 0).DefaultIfEmpty().Min(c => c?.ThrottleDelta ?? 0),
                    MaxExcess = throttleComparisons.Where(c => c.ThrottleDelta > 0).DefaultIfEmpty().Max(c => c?.ThrottleDelta ?? 0),
                    ProblematicSections = throttleComparisons.Count(c => Math.Abs(c.ThrottleDelta) > 15)
                };
            }

            // Brake analysis
            var brakeComparisons = comparisons.Where(c => Math.Abs(c.BrakeDelta) > 5).ToList();
            if (brakeComparisons.Any())
            {
                summary.BrakeAnalysis = new InputAnalysis
                {
                    AverageDelta = brakeComparisons.Average(c => c.BrakeDelta),
                    MaxDeficit = brakeComparisons.Where(c => c.BrakeDelta < 0).DefaultIfEmpty().Min(c => c?.BrakeDelta ?? 0),
                    MaxExcess = brakeComparisons.Where(c => c.BrakeDelta > 0).DefaultIfEmpty().Max(c => c?.BrakeDelta ?? 0),
                    ProblematicSections = brakeComparisons.Count(c => Math.Abs(c.BrakeDelta) > 20)
                };
            }

            // Steering analysis
            var steeringComparisons = comparisons.Where(c => Math.Abs(c.SteeringDelta) > 10).ToList();
            if (steeringComparisons.Any())
            {
                summary.SteeringAnalysis = new InputAnalysis
                {
                    AverageDelta = steeringComparisons.Average(c => c.SteeringDelta),
                    MaxDeficit = steeringComparisons.Where(c => c.SteeringDelta < 0).DefaultIfEmpty().Min(c => c?.SteeringDelta ?? 0),
                    MaxExcess = steeringComparisons.Where(c => c.SteeringDelta > 0).DefaultIfEmpty().Max(c => c?.SteeringDelta ?? 0),
                    ProblematicSections = steeringComparisons.Count(c => Math.Abs(c.SteeringDelta) > 25)
                };
            }
        }

        /// <summary>
        /// Identify key improvement areas from comparison data
        /// </summary>
        /// <param name="comparisons">Comparison results</param>
        /// <param name="summary">Summary to update</param>
        private void IdentifyImprovementAreas(List<ComparisonResult> comparisons, PerformanceAnalysisSummary summary)
        {
            // Collect all improvement areas
            var allImprovements = comparisons.SelectMany(c => c.ImprovementAreas).ToList();
            
            // Group by type and calculate statistics
            var improvementGroups = allImprovements
                .GroupBy(i => i.Type)
                .Select(g => new ImprovementTypeAnalysis
                {
                    Type = g.Key,
                    Frequency = g.Count(),
                    AverageSeverity = g.Average(i => i.Severity),
                    TotalPotentialGain = g.Sum(i => i.PotentialGain),
                    MaxSeverity = g.Max(i => i.Severity),
                    AffectedSections = g.Select(i => i.DistanceRange).Distinct().Count()
                })
                .OrderByDescending(a => a.TotalPotentialGain)
                .ToList();

            summary.ImprovementAreas = improvementGroups;
        }

        /// <summary>
        /// Analyze consistency across different track sections
        /// </summary>
        /// <param name="comparisons">Comparison results</param>
        /// <param name="summary">Summary to update</param>
        private void AnalyzeConsistency(List<ComparisonResult> comparisons, PerformanceAnalysisSummary summary)
        {
            // Group comparisons by track segments
            var segmentGroups = comparisons
                .Where(c => c.Segment != null)
                .GroupBy(c => c.Segment!.Id)
                .ToList();

            var consistencyScores = new List<double>();

            foreach (var group in segmentGroups)
            {
                var timeDeltas = group.Select(c => c.TimeDelta).ToList();
                if (timeDeltas.Count > 1)
                {
                    var mean = timeDeltas.Average();
                    var variance = timeDeltas.Select(t => Math.Pow(t - mean, 2)).Average();
                    var stdDev = Math.Sqrt(variance);
                    
                    // Lower standard deviation = higher consistency
                    var consistencyScore = Math.Max(0, 1.0 - (stdDev / 2.0)); // Normalize to 0-1
                    consistencyScores.Add(consistencyScore);
                }
            }

            summary.ConsistencyScore = consistencyScores.Any() ? consistencyScores.Average() * 100 : 0;
            summary.ConsistentSections = consistencyScores.Count(s => s > 0.8);
            summary.InconsistentSections = consistencyScores.Count(s => s < 0.5);
        }

        /// <summary>
        /// Generate coaching recommendations based on analysis
        /// </summary>
        /// <param name="summary">Performance analysis summary</param>
        private void GenerateCoachingRecommendations(PerformanceAnalysisSummary summary)
        {
            var recommendations = new List<string>();

            // Time-based recommendations
            if (summary.AverageTimeDelta > 0.5)
            {
                recommendations.Add($"Focus on reducing lap time - currently {summary.AverageTimeDelta:F2}s slower than reference");
            }

            // Speed-based recommendations
            if (summary.AverageSpeedDelta < -5)
            {
                recommendations.Add($"Work on carrying more speed - average deficit of {Math.Abs(summary.AverageSpeedDelta):F1} km/h");
            }

            // Input-based recommendations
            if (summary.ThrottleAnalysis != null && summary.ThrottleAnalysis.ProblematicSections > 5)
            {
                recommendations.Add("Focus on throttle application - multiple sections with suboptimal throttle usage");
            }

            if (summary.BrakeAnalysis != null && summary.BrakeAnalysis.ProblematicSections > 5)
            {
                recommendations.Add("Work on braking technique - consider brake point optimization");
            }

            if (summary.SteeringAnalysis != null && summary.SteeringAnalysis.ProblematicSections > 5)
            {
                recommendations.Add("Improve steering smoothness - avoid excessive steering inputs");
            }

            // Improvement area recommendations
            var topImprovements = summary.ImprovementAreas.Take(3).ToList();
            foreach (var improvement in topImprovements)
            {
                recommendations.Add($"Priority improvement: {improvement.Type} - potential gain {improvement.TotalPotentialGain:F2}s");
            }

            // Consistency recommendations
            if (summary.ConsistencyScore < 70)
            {
                recommendations.Add($"Focus on consistency - current score {summary.ConsistencyScore:F1}%");
            }

            summary.CoachingRecommendations = recommendations;
        }
    }

    /// <summary>
    /// Summary of performance analysis results
    /// </summary>
    public class PerformanceAnalysisSummary
    {
        /// <summary>
        /// Total number of comparisons analyzed
        /// </summary>
        public int TotalComparisons { get; set; }

        /// <summary>
        /// When this analysis was performed
        /// </summary>
        public DateTime AnalysisTimestamp { get; set; }

        /// <summary>
        /// Average time delta across all comparisons
        /// </summary>
        public double AverageTimeDelta { get; set; }

        /// <summary>
        /// Total time lost compared to reference
        /// </summary>
        public double TotalTimeLost { get; set; }

        /// <summary>
        /// Total time gained compared to reference
        /// </summary>
        public double TotalTimeGained { get; set; }

        /// <summary>
        /// Average speed delta
        /// </summary>
        public double AverageSpeedDelta { get; set; }

        /// <summary>
        /// Maximum speed deficit
        /// </summary>
        public double MaxSpeedDeficit { get; set; }

        /// <summary>
        /// Maximum speed advantage
        /// </summary>
        public double MaxSpeedAdvantage { get; set; }

        /// <summary>
        /// Throttle input analysis
        /// </summary>
        public InputAnalysis? ThrottleAnalysis { get; set; }

        /// <summary>
        /// Brake input analysis
        /// </summary>
        public InputAnalysis? BrakeAnalysis { get; set; }

        /// <summary>
        /// Steering input analysis
        /// </summary>
        public InputAnalysis? SteeringAnalysis { get; set; }

        /// <summary>
        /// Identified improvement areas
        /// </summary>
        public List<ImprovementTypeAnalysis> ImprovementAreas { get; set; } = new();

        /// <summary>
        /// Overall consistency score (0-100)
        /// </summary>
        public double ConsistencyScore { get; set; }

        /// <summary>
        /// Number of consistent sections
        /// </summary>
        public int ConsistentSections { get; set; }

        /// <summary>
        /// Number of inconsistent sections
        /// </summary>
        public int InconsistentSections { get; set; }

        /// <summary>
        /// Generated coaching recommendations
        /// </summary>
        public List<string> CoachingRecommendations { get; set; } = new();
    }

    /// <summary>
    /// Analysis of driver input patterns
    /// </summary>
    public class InputAnalysis
    {
        /// <summary>
        /// Average input delta
        /// </summary>
        public double AverageDelta { get; set; }

        /// <summary>
        /// Maximum input deficit
        /// </summary>
        public double MaxDeficit { get; set; }

        /// <summary>
        /// Maximum input excess
        /// </summary>
        public double MaxExcess { get; set; }

        /// <summary>
        /// Number of problematic sections
        /// </summary>
        public int ProblematicSections { get; set; }
    }

    /// <summary>
    /// Analysis of specific improvement type
    /// </summary>
    public class ImprovementTypeAnalysis
    {
        /// <summary>
        /// Type of improvement
        /// </summary>
        public ImprovementType Type { get; set; }

        /// <summary>
        /// How often this improvement appears
        /// </summary>
        public int Frequency { get; set; }

        /// <summary>
        /// Average severity of this improvement type
        /// </summary>
        public double AverageSeverity { get; set; }

        /// <summary>
        /// Maximum severity observed
        /// </summary>
        public double MaxSeverity { get; set; }

        /// <summary>
        /// Total potential time gain from this improvement type
        /// </summary>
        public double TotalPotentialGain { get; set; }

        /// <summary>
        /// Number of track sections affected
        /// </summary>
        public int AffectedSections { get; set; }
    }
}
