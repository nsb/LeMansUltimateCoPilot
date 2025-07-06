using System;
using System.Collections.Generic;
using System.Linq;

namespace LeMansUltimateCoPilot.Models
{
    /// <summary>
    /// Real-time metrics for comparing current performance with reference lap
    /// Tracks consistency, improvement areas, and performance trends
    /// </summary>
    public class RealTimeComparisonMetrics
    {
        /// <summary>
        /// Current lap time delta compared to reference
        /// </summary>
        public double CurrentLapTimeDelta { get; set; }

        /// <summary>
        /// Best possible lap time if all segments were optimal
        /// </summary>
        public double TheoreticalBestLapTime { get; set; }

        /// <summary>
        /// Consistency rating (0-100%, higher is better)
        /// </summary>
        public double ConsistencyRating { get; set; }

        /// <summary>
        /// Performance rating compared to reference (0-100%, higher is better)
        /// </summary>
        public double PerformanceRating { get; set; }

        /// <summary>
        /// Time deltas for each completed segment
        /// </summary>
        public Dictionary<int, double> SegmentTimeDeltas { get; set; } = new();

        /// <summary>
        /// Best segment times achieved in current session
        /// </summary>
        public Dictionary<int, double> BestSegmentTimes { get; set; } = new();

        /// <summary>
        /// Worst segment times in current session
        /// </summary>
        public Dictionary<int, double> WorstSegmentTimes { get; set; } = new();

        /// <summary>
        /// Current active improvement areas
        /// </summary>
        public List<ImprovementArea> ActiveImprovements { get; set; } = new();

        /// <summary>
        /// Historical improvement areas from previous laps
        /// </summary>
        public List<ImprovementArea> HistoricalImprovements { get; set; } = new();

        /// <summary>
        /// Sector time deltas (if sectors are defined)
        /// </summary>
        public Dictionary<int, double> SectorTimeDeltas { get; set; } = new();

        /// <summary>
        /// Track segments where most time is being lost
        /// </summary>
        public List<TrackSegment> ProblematicSegments { get; set; } = new();

        /// <summary>
        /// Track segments where time is being gained
        /// </summary>
        public List<TrackSegment> StrongSegments { get; set; } = new();

        /// <summary>
        /// Overall session statistics
        /// </summary>
        public SessionComparisonStats SessionStats { get; set; } = new();

        /// <summary>
        /// Last update timestamp
        /// </summary>
        public DateTime LastUpdated { get; set; } = DateTime.Now;

        /// <summary>
        /// Reference lap being compared against
        /// </summary>
        public ReferenceLap? ReferenceLap { get; set; }

        /// <summary>
        /// Calculate overall performance percentage
        /// </summary>
        /// <returns>Performance percentage (0-100%)</returns>
        public double CalculateOverallPerformance()
        {
            if (!SegmentTimeDeltas.Any()) return 0;

            var totalDelta = SegmentTimeDeltas.Values.Sum();
            var referenceTime = ReferenceLap?.LapTime ?? 0;
            
            if (referenceTime == 0) return 0;

            var performanceRatio = Math.Max(0, (referenceTime - totalDelta) / referenceTime);
            return Math.Min(100, performanceRatio * 100);
        }

        /// <summary>
        /// Get the top improvement areas by potential gain
        /// </summary>
        /// <param name="count">Number of top areas to return</param>
        /// <returns>List of improvement areas sorted by potential gain</returns>
        public List<ImprovementArea> GetTopImprovements(int count = 3)
        {
            return ActiveImprovements
                .OrderByDescending(i => i.PotentialGain)
                .Take(count)
                .ToList();
        }

        /// <summary>
        /// Calculate consistency rating based on segment time variations
        /// </summary>
        /// <returns>Consistency rating (0-100%)</returns>
        public double CalculateConsistencyRating()
        {
            if (SegmentTimeDeltas.Count < 2) return 100;

            var deltas = SegmentTimeDeltas.Values.ToList();
            var mean = deltas.Average();
            var variance = deltas.Select(d => Math.Pow(d - mean, 2)).Average();
            var standardDeviation = Math.Sqrt(variance);

            // Lower standard deviation = higher consistency
            // Scale to 0-100 range (assuming 2 seconds std dev = 0% consistency)
            var consistencyRatio = Math.Max(0, (2.0 - standardDeviation) / 2.0);
            return Math.Min(100, consistencyRatio * 100);
        }
    }

    /// <summary>
    /// Session-wide comparison statistics
    /// </summary>
    public class SessionComparisonStats
    {
        /// <summary>
        /// Total number of laps compared
        /// </summary>
        public int LapsCompleted { get; set; }

        /// <summary>
        /// Best lap time delta achieved this session
        /// </summary>
        public double BestLapTimeDelta { get; set; } = double.MaxValue;

        /// <summary>
        /// Worst lap time delta this session
        /// </summary>
        public double WorstLapTimeDelta { get; set; } = double.MinValue;

        /// <summary>
        /// Average lap time delta for the session
        /// </summary>
        public double AverageLapTimeDelta { get; set; }

        /// <summary>
        /// Improvement trend over time (positive = improving, negative = declining)
        /// </summary>
        public double ImprovementTrend { get; set; }

        /// <summary>
        /// Total potential time gain identified
        /// </summary>
        public double TotalPotentialGain { get; set; }

        /// <summary>
        /// Time when session comparison started
        /// </summary>
        public DateTime SessionStarted { get; set; } = DateTime.Now;

        /// <summary>
        /// Update statistics with new lap data
        /// </summary>
        /// <param name="lapTimeDelta">New lap time delta to include</param>
        public void UpdateWithLapData(double lapTimeDelta)
        {
            LapsCompleted++;
            
            if (lapTimeDelta < BestLapTimeDelta)
                BestLapTimeDelta = lapTimeDelta;
            
            if (lapTimeDelta > WorstLapTimeDelta)
                WorstLapTimeDelta = lapTimeDelta;
            
            // Update rolling average
            AverageLapTimeDelta = ((AverageLapTimeDelta * (LapsCompleted - 1)) + lapTimeDelta) / LapsCompleted;
        }
    }
}
