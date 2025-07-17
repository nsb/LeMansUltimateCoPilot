using System;
using System.Collections.Generic;
using LeMansUltimateCoPilot.Models;

namespace LeMansUltimateCoPilot.Analysis
{
    /// <summary>
    /// Represents the result of cornering analysis
    /// </summary>
    public class CorneringAnalysisResult
    {
        public bool IsInCorner { get; set; }
        public CornerPhase CornerPhase { get; set; }
        public CornerDirection CornerDirection { get; set; }
        public double CurrentLateralG { get; set; }
        public double CurrentSpeed { get; set; }
        public Corner? CompletedCorner { get; set; }
        public List<CoachingFeedback> CoachingFeedback { get; set; } = new List<CoachingFeedback>();
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Represents a detected corner with full analysis
    /// </summary>
    public class Corner
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public CornerDirection Direction { get; set; }
        public double EntrySpeed { get; set; }
        public double ApexSpeed { get; set; }
        public double ExitSpeed { get; set; }
        public double MaxLateralG { get; set; }
        public double Duration { get; set; }
        public CornerPhaseAnalysis? EntryAnalysis { get; set; }
        public CornerPhaseAnalysis? ExitAnalysis { get; set; }
        public List<EnhancedTelemetryData> TelemetryData { get; set; } = new List<EnhancedTelemetryData>();
        
        /// <summary>
        /// Calculate corner performance score (0-100)
        /// </summary>
        public double PerformanceScore
        {
            get
            {
                double score = 100;
                
                // Penalize for harsh braking
                if (EntryAnalysis?.MaxBrakeInput > 0.9) score -= 15;
                
                // Penalize for poor throttle smoothness
                if (ExitAnalysis?.ThrottleSmoothness > 0.1) score -= 10;
                
                // Penalize for low exit speed relative to entry
                if (ExitSpeed < EntrySpeed * 0.8) score -= 20;
                
                // Penalize for excessive lateral G
                if (MaxLateralG > 1.2) score -= 10;
                
                // Bonus for smooth steering
                if (EntryAnalysis?.SteeringSmoothness < 0.05) score += 5;
                if (ExitAnalysis?.SteeringSmoothness < 0.05) score += 5;
                
                return Math.Max(0, Math.Min(100, score));
            }
        }
    }

    /// <summary>
    /// Analysis of a specific corner phase
    /// </summary>
    public class CornerPhaseAnalysis
    {
        public CornerPhase Phase { get; set; }
        public double Duration { get; set; }
        public double AverageSpeed { get; set; }
        public double MaxLateralG { get; set; }
        public double AverageLateralG { get; set; }
        public double MaxBrakeInput { get; set; }
        public double AverageBrakeInput { get; set; }
        public double MaxThrottleInput { get; set; }
        public double AverageThrottleInput { get; set; }
        public double SteeringSmoothness { get; set; }
        public double ThrottleSmoothness { get; set; }
        public double BrakeSmoothness { get; set; }
    }

    /// <summary>
    /// Current state of cornering
    /// </summary>
    public class CornerState
    {
        public bool IsInCorner { get; set; }
        public CornerDirection Direction { get; set; }
        public CornerPhase Phase { get; set; }
        public double LateralG { get; set; }
        public double Speed { get; set; }
    }

    /// <summary>
    /// Coaching feedback for the driver
    /// </summary>
    public class CoachingFeedback
    {
        public FeedbackPriority Priority { get; set; } = FeedbackPriority.Low;
        public FeedbackCategory Category { get; set; } = FeedbackCategory.General;
        public string Message { get; set; } = "";
        public FeedbackType Type { get; set; } = FeedbackType.Tip;
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// Corner direction enumeration
    /// </summary>
    public enum CornerDirection
    {
        Straight,
        Left,
        Right
    }

    /// <summary>
    /// Corner phase enumeration
    /// </summary>
    public enum CornerPhase
    {
        Unknown,
        Entry,
        Apex,
        Exit
    }

    /// <summary>
    /// Trend analysis enumeration
    /// </summary>
    public enum Trend
    {
        Increasing,
        Decreasing,
        Stable
    }

    /// <summary>
    /// Feedback priority levels
    /// </summary>
    public enum FeedbackPriority
    {
        Low,
        Medium,
        High,
        Critical
    }

    /// <summary>
    /// Feedback categories
    /// </summary>
    public enum FeedbackCategory
    {
        General,
        Cornering,
        Braking,
        Throttle,
        Steering,
        Safety
    }

    /// <summary>
    /// Feedback types
    /// </summary>
    public enum FeedbackType
    {
        Tip,
        Suggestion,
        Warning,
        Error
    }
}
