using System;
using System.Collections.Generic;

namespace LeMansUltimateCoPilot.Models
{
    /// <summary>
    /// Result of comparing current telemetry with reference lap data
    /// Contains time deltas, performance analysis, and coaching recommendations
    /// </summary>
    public class ComparisonResult
    {
        /// <summary>
        /// Time difference in seconds (positive = slower than reference, negative = faster)
        /// </summary>
        public double TimeDelta { get; set; }

        /// <summary>
        /// Distance from track start in meters
        /// </summary>
        public double DistanceFromStart { get; set; }

        /// <summary>
        /// Track segment this comparison belongs to
        /// </summary>
        public TrackSegment? Segment { get; set; }

        /// <summary>
        /// Speed difference in km/h (positive = faster than reference, negative = slower)
        /// </summary>
        public double SpeedDelta { get; set; }

        /// <summary>
        /// Throttle input difference (0-100%)
        /// </summary>
        public double ThrottleDelta { get; set; }

        /// <summary>
        /// Brake input difference (0-100%)
        /// </summary>
        public double BrakeDelta { get; set; }

        /// <summary>
        /// Steering input difference (-100% to +100%)
        /// </summary>
        public double SteeringDelta { get; set; }

        /// <summary>
        /// Longitudinal G-force difference
        /// </summary>
        public double LongitudinalGDelta { get; set; }

        /// <summary>
        /// Lateral G-force difference
        /// </summary>
        public double LateralGDelta { get; set; }

        /// <summary>
        /// Performance areas where improvement is possible
        /// </summary>
        public List<ImprovementArea> ImprovementAreas { get; set; } = new();

        /// <summary>
        /// Confidence level of the comparison (0-100%)
        /// </summary>
        public double ConfidenceLevel { get; set; }

        /// <summary>
        /// Timestamp when this comparison was calculated
        /// </summary>
        public DateTime CalculatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// Current telemetry data point
        /// </summary>
        public EnhancedTelemetryData CurrentTelemetry { get; set; } = new();

        /// <summary>
        /// Reference telemetry data point
        /// </summary>
        public EnhancedTelemetryData ReferenceTelemetry { get; set; } = new();
    }

    /// <summary>
    /// Represents an area where the driver can improve compared to the reference lap
    /// </summary>
    public class ImprovementArea
    {
        /// <summary>
        /// Type of improvement area
        /// </summary>
        public ImprovementType Type { get; set; }

        /// <summary>
        /// Severity of the improvement opportunity (0-100%)
        /// </summary>
        public double Severity { get; set; }

        /// <summary>
        /// Coaching message describing the improvement
        /// </summary>
        public string Message { get; set; } = "";

        /// <summary>
        /// Potential time gain in seconds if this area is improved
        /// </summary>
        public double PotentialGain { get; set; }

        /// <summary>
        /// Distance range where this improvement applies
        /// </summary>
        public (double Start, double End) DistanceRange { get; set; }
    }

    /// <summary>
    /// Types of improvement areas for coaching
    /// </summary>
    public enum ImprovementType
    {
        /// <summary>
        /// Braking point optimization
        /// </summary>
        BrakingPoint,

        /// <summary>
        /// Braking pressure optimization
        /// </summary>
        BrakingPressure,

        /// <summary>
        /// Throttle application timing
        /// </summary>
        ThrottleApplication,

        /// <summary>
        /// Throttle modulation
        /// </summary>
        ThrottleModulation,

        /// <summary>
        /// Cornering line optimization
        /// </summary>
        CorneringLine,

        /// <summary>
        /// Steering smoothness
        /// </summary>
        SteeringSmoothing,

        /// <summary>
        /// Gear change timing
        /// </summary>
        GearTiming,

        /// <summary>
        /// Speed carrying through corners
        /// </summary>
        CornerSpeed,

        /// <summary>
        /// Acceleration out of corners
        /// </summary>
        CornerExit,

        /// <summary>
        /// General consistency issues
        /// </summary>
        Consistency
    }
}
