using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LeMansUltimateCoPilot.Models;
using LeMansUltimateCoPilot.Analysis;

namespace LeMansUltimateCoPilot.AI
{
    /// <summary>
    /// Represents the current driving context for LLM-powered coaching
    /// </summary>
    public class DrivingContext
    {
        /// <summary>
        /// Current telemetry snapshot
        /// </summary>
        public required EnhancedTelemetryData CurrentTelemetry { get; set; }

        /// <summary>
        /// Reference lap comparison data
        /// </summary>
        public required ReferenceLapComparison ReferenceLapData { get; set; }

        /// <summary>
        /// Current corner analysis state
        /// </summary>
        public required CornerState CurrentCornerState { get; set; }

        /// <summary>
        /// Recent performance events (last 30 seconds)
        /// </summary>
        public List<PerformanceEvent> RecentEvents { get; set; }

        /// <summary>
        /// Track-specific context information
        /// </summary>
        public required TrackContext CurrentTrack { get; set; }

        /// <summary>
        /// Session information
        /// </summary>
        public required SessionContext SessionInfo { get; set; }

        /// <summary>
        /// Driver's current performance status
        /// </summary>
        public PerformanceStatus CurrentPerformance { get; set; }

        public DrivingContext()
        {
            RecentEvents = new List<PerformanceEvent>();
            CurrentPerformance = new PerformanceStatus();
        }

        /// <summary>
        /// Converts the driving context to natural language for LLM consumption
        /// </summary>
        public string ToNaturalLanguage()
        {
            var context = new StringBuilder();

            // Current situation
            context.AppendLine(BuildCurrentSituationContext());

            // Reference lap comparison
            if (ReferenceLapData != null)
            {
                context.AppendLine(BuildReferenceLapContext());
            }

            // Recent performance
            if (RecentEvents.Any())
            {
                context.AppendLine(BuildRecentEventsContext());
            }

            // Track context
            if (CurrentTrack != null)
            {
                context.AppendLine(BuildTrackContext());
            }

            // Session context
            if (SessionInfo != null)
            {
                context.AppendLine(BuildSessionContext());
            }

            return context.ToString();
        }

        private string BuildCurrentSituationContext()
        {
            var situation = new StringBuilder();
            situation.AppendLine("=== CURRENT SITUATION ===");

            if (CurrentTelemetry != null)
            {
                situation.AppendLine($"Speed: {CurrentTelemetry.Speed:F1} km/h");
                situation.AppendLine($"Lateral G: {CurrentTelemetry.LateralG:F2}g");
                situation.AppendLine($"Brake Input: {CurrentTelemetry.BrakeInput:F1}%");
                situation.AppendLine($"Throttle Input: {CurrentTelemetry.ThrottleInput:F1}%");
                situation.AppendLine($"Gear: {CurrentTelemetry.Gear}");
            }

            if (CurrentCornerState != null)
            {
                situation.AppendLine($"Corner State: {(CurrentCornerState.IsInCorner ? "In Corner" : "Straight")}");
                if (CurrentCornerState.IsInCorner)
                {
                    situation.AppendLine($"Corner Direction: {CurrentCornerState.Direction}");
                    situation.AppendLine($"Corner Phase: {CurrentCornerState.Phase}");
                }
            }

            return situation.ToString();
        }

        private string BuildReferenceLapContext()
        {
            var reference = new StringBuilder();
            reference.AppendLine("=== REFERENCE LAP COMPARISON ===");

            reference.AppendLine($"Speed vs Reference: {ReferenceLapData.SpeedDelta:+0.0;-0.0} km/h ({ReferenceLapData.SpeedDeltaPercent:+0.0;-0.0}%)");
            reference.AppendLine($"Braking Point Delta: {ReferenceLapData.BrakingPointDelta:+0.0;-0.0}m");
            reference.AppendLine($"Racing Line Deviation: {ReferenceLapData.RacingLineDeviation:F1}m");
            reference.AppendLine($"Lap Time Delta: {ReferenceLapData.LapTimeDelta.TotalSeconds:+0.00;-0.00}s");

            if (ReferenceLapData.PerformanceZone != PerformanceZone.Optimal)
            {
                reference.AppendLine($"Performance Zone: {ReferenceLapData.PerformanceZone}");
            }

            return reference.ToString();
        }

        private string BuildRecentEventsContext()
        {
            var events = new StringBuilder();
            events.AppendLine("=== RECENT PERFORMANCE (Last 30 seconds) ===");

            var mistakes = RecentEvents.Where(e => e.Type == PerformanceEventType.Mistake).ToList();
            var improvements = RecentEvents.Where(e => e.Type == PerformanceEventType.Improvement).ToList();

            if (mistakes.Any())
            {
                events.AppendLine("Recent Mistakes:");
                foreach (var mistake in mistakes.Take(3))
                {
                    events.AppendLine($"  - {mistake.Description} ({mistake.TimeSinceEvent.TotalSeconds:F0}s ago)");
                }
            }

            if (improvements.Any())
            {
                events.AppendLine("Recent Improvements:");
                foreach (var improvement in improvements.Take(3))
                {
                    events.AppendLine($"  - {improvement.Description} ({improvement.TimeSinceEvent.TotalSeconds:F0}s ago)");
                }
            }

            return events.ToString();
        }

        private string BuildTrackContext()
        {
            var track = new StringBuilder();
            track.AppendLine("=== TRACK CONTEXT ===");

            track.AppendLine($"Track: {CurrentTrack.Name}");
            track.AppendLine($"Current Sector: {CurrentTrack.CurrentSector}");
            
            if (!string.IsNullOrEmpty(CurrentTrack.NextCorner))
            {
                track.AppendLine($"Next Corner: {CurrentTrack.NextCorner} ({CurrentTrack.DistanceToNextCorner:F0}m)");
            }

            if (!string.IsNullOrEmpty(CurrentTrack.TrackConditions))
            {
                track.AppendLine($"Track Conditions: {CurrentTrack.TrackConditions}");
            }

            return track.ToString();
        }

        private string BuildSessionContext()
        {
            var session = new StringBuilder();
            session.AppendLine("=== SESSION CONTEXT ===");

            session.AppendLine($"Current Lap: {SessionInfo.CurrentLap}");
            session.AppendLine($"Session Time: {SessionInfo.SessionTime:mm\\:ss}");
            session.AppendLine($"Session Type: {SessionInfo.SessionType}");
            
            if (SessionInfo.BestLapTime.HasValue)
            {
                session.AppendLine($"Best Lap Time: {SessionInfo.BestLapTime:mm\\:ss\\.fff}");
            }

            return session.ToString();
        }
    }

    /// <summary>
    /// Reference lap comparison data
    /// </summary>
    public class ReferenceLapComparison
    {
        public float SpeedDelta { get; set; }
        public float SpeedDeltaPercent { get; set; }
        public float BrakingPointDelta { get; set; }
        public float RacingLineDeviation { get; set; }
        public TimeSpan LapTimeDelta { get; set; }
        public PerformanceZone PerformanceZone { get; set; }
    }

    /// <summary>
    /// Performance event that occurred recently
    /// </summary>
    public class PerformanceEvent
    {
        public PerformanceEventType Type { get; set; }
        public required string Description { get; set; }
        public DateTime Timestamp { get; set; }
        public TimeSpan TimeSinceEvent => DateTime.Now - Timestamp;
        public float Severity { get; set; } // 0.0 to 1.0
    }

    /// <summary>
    /// Track-specific context information
    /// </summary>
    public class TrackContext
    {
        public required string Name { get; set; }
        public int CurrentSector { get; set; }
        public required string NextCorner { get; set; }
        public float DistanceToNextCorner { get; set; }
        public required string TrackConditions { get; set; }
    }

    /// <summary>
    /// Session information
    /// </summary>
    public class SessionContext
    {
        public int CurrentLap { get; set; }
        public TimeSpan SessionTime { get; set; }
        public required string SessionType { get; set; }
        public TimeSpan? BestLapTime { get; set; }
    }

    /// <summary>
    /// Current performance status
    /// </summary>
    public class PerformanceStatus
    {
        public PerformanceLevel Level { get; set; }
        public float ConsistencyScore { get; set; } // 0.0 to 1.0
        public float ImprovementRate { get; set; } // Recent improvement trend
        public List<string> CurrentStrengths { get; set; }
        public List<string> CurrentWeaknesses { get; set; }

        public PerformanceStatus()
        {
            CurrentStrengths = new List<string>();
            CurrentWeaknesses = new List<string>();
        }
    }

    /// <summary>
    /// Types of performance events
    /// </summary>
    public enum PerformanceEventType
    {
        Mistake,
        Improvement,
        Achievement,
        Warning
    }

    /// <summary>
    /// Performance zones relative to reference lap
    /// </summary>
    public enum PerformanceZone
    {
        Optimal,      // Within 2% of reference
        Good,         // Within 5% of reference
        Struggling,   // 5-10% off reference
        Poor          // More than 10% off reference
    }

    /// <summary>
    /// Overall performance level
    /// </summary>
    public enum PerformanceLevel
    {
        Excellent,
        Good,
        Average,
        Below,
        Struggling
    }
}
