using System;
using System.Text.Json.Serialization;

namespace LeMansUltimateCoPilot.Models
{
    /// <summary>
    /// Represents a micro-sector of a racing track with detailed characteristics
    /// Used for AI coaching analysis and performance comparison
    /// </summary>
    public class TrackSegment
    {
        /// <summary>
        /// Unique identifier for this track segment
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Sequential number of this segment on the track (0-based)
        /// </summary>
        [JsonPropertyName("segmentNumber")]
        public int SegmentNumber { get; set; }

        /// <summary>
        /// Distance from track start in meters
        /// </summary>
        [JsonPropertyName("distanceFromStart")]
        public double DistanceFromStart { get; set; }

        /// <summary>
        /// Length of this segment in meters
        /// </summary>
        [JsonPropertyName("segmentLength")]
        public double SegmentLength { get; set; }

        /// <summary>
        /// Type of track segment (straight, corner, braking_zone, etc.)
        /// </summary>
        [JsonPropertyName("segmentType")]
        public TrackSegmentType SegmentType { get; set; }

        /// <summary>
        /// Center position of the segment (X coordinate)
        /// </summary>
        [JsonPropertyName("centerX")]
        public double CenterX { get; set; }

        /// <summary>
        /// Center position of the segment (Y coordinate)
        /// </summary>
        [JsonPropertyName("centerY")]
        public double CenterY { get; set; }

        /// <summary>
        /// Center position of the segment (Z coordinate)
        /// </summary>
        [JsonPropertyName("centerZ")]
        public double CenterZ { get; set; }

        /// <summary>
        /// Track direction angle in radians at segment center
        /// </summary>
        [JsonPropertyName("trackHeading")]
        public double TrackHeading { get; set; }

        /// <summary>
        /// Curvature of the track at this segment (1/radius)
        /// Positive for right turns, negative for left turns, 0 for straight
        /// </summary>
        [JsonPropertyName("curvature")]
        public double Curvature { get; set; }

        /// <summary>
        /// Banking angle in radians (positive for banked turns)
        /// </summary>
        [JsonPropertyName("banking")]
        public double Banking { get; set; }

        /// <summary>
        /// Elevation change in meters (positive for uphill)
        /// </summary>
        [JsonPropertyName("elevationChange")]
        public double ElevationChange { get; set; }

        /// <summary>
        /// Optimal speed for this segment in km/h
        /// </summary>
        [JsonPropertyName("optimalSpeed")]
        public double OptimalSpeed { get; set; }

        /// <summary>
        /// Recommended gear for this segment
        /// </summary>
        [JsonPropertyName("recommendedGear")]
        public int RecommendedGear { get; set; }

        /// <summary>
        /// Braking point indicator (percentage of segment where braking should start)
        /// 0.0 = start of segment, 1.0 = end of segment
        /// </summary>
        [JsonPropertyName("brakingPoint")]
        public double BrakingPoint { get; set; }

        /// <summary>
        /// Turn-in point for corners (percentage of segment)
        /// </summary>
        [JsonPropertyName("turnInPoint")]
        public double TurnInPoint { get; set; }

        /// <summary>
        /// Apex point for corners (percentage of segment)
        /// </summary>
        [JsonPropertyName("apexPoint")]
        public double ApexPoint { get; set; }

        /// <summary>
        /// Exit point for corners (percentage of segment)
        /// </summary>
        [JsonPropertyName("exitPoint")]
        public double ExitPoint { get; set; }

        /// <summary>
        /// Difficulty rating of this segment (1-10, 10 being most difficult)
        /// </summary>
        [JsonPropertyName("difficultyRating")]
        public int DifficultyRating { get; set; }

        /// <summary>
        /// Importance rating for lap time (1-10, 10 being most important)
        /// </summary>
        [JsonPropertyName("importanceRating")]
        public int ImportanceRating { get; set; }

        /// <summary>
        /// Additional notes or coaching tips for this segment
        /// </summary>
        [JsonPropertyName("notes")]
        public string Notes { get; set; } = string.Empty;

        /// <summary>
        /// Timestamp when this segment was created or last updated
        /// </summary>
        [JsonPropertyName("lastUpdated")]
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Creates a new track segment with default values
        /// </summary>
        public TrackSegment()
        {
            Id = Guid.NewGuid().ToString();
            SegmentType = TrackSegmentType.Straight;
            DifficultyRating = 1;
            ImportanceRating = 1;
        }

        /// <summary>
        /// Creates a new track segment with specified parameters
        /// </summary>
        /// <param name="segmentNumber">Sequential segment number</param>
        /// <param name="distanceFromStart">Distance from track start in meters</param>
        /// <param name="segmentLength">Length of segment in meters</param>
        /// <param name="centerX">X coordinate of segment center</param>
        /// <param name="centerY">Y coordinate of segment center</param>
        /// <param name="centerZ">Z coordinate of segment center</param>
        public TrackSegment(int segmentNumber, double distanceFromStart, double segmentLength, 
            double centerX, double centerY, double centerZ) : this()
        {
            SegmentNumber = segmentNumber;
            DistanceFromStart = distanceFromStart;
            SegmentLength = segmentLength;
            CenterX = centerX;
            CenterY = centerY;
            CenterZ = centerZ;
        }

        /// <summary>
        /// Calculates the distance between this segment and another point
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        /// <param name="z">Z coordinate</param>
        /// <returns>Distance in meters</returns>
        public double DistanceTo(double x, double y, double z)
        {
            double dx = CenterX - x;
            double dy = CenterY - y;
            double dz = CenterZ - z;
            return Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        /// <summary>
        /// Determines if this segment is a corner (left or right turn)
        /// </summary>
        /// <returns>True if segment is a corner</returns>
        public bool IsCorner()
        {
            return SegmentType == TrackSegmentType.LeftTurn || 
                   SegmentType == TrackSegmentType.RightTurn ||
                   SegmentType == TrackSegmentType.Chicane;
        }

        /// <summary>
        /// Determines if this segment requires braking
        /// </summary>
        /// <returns>True if segment is a braking zone</returns>
        public bool IsBrakingZone()
        {
            return SegmentType == TrackSegmentType.BrakingZone || 
                   BrakingPoint > 0.0;
        }

        /// <summary>
        /// Returns a string representation of the track segment
        /// </summary>
        /// <returns>String describing the segment</returns>
        public override string ToString()
        {
            var culture = System.Globalization.CultureInfo.InvariantCulture;
            return $"Segment {SegmentNumber}: {SegmentType} @ {DistanceFromStart.ToString("F1", culture)}m " +
                   $"({OptimalSpeed.ToString("F1", culture)} km/h)";
        }
    }

    /// <summary>
    /// Enumeration of different track segment types
    /// </summary>
    public enum TrackSegmentType
    {
        /// <summary>
        /// Straight section of track
        /// </summary>
        Straight,

        /// <summary>
        /// Left turn or corner
        /// </summary>
        LeftTurn,

        /// <summary>
        /// Right turn or corner
        /// </summary>
        RightTurn,

        /// <summary>
        /// Chicane (S-curve)
        /// </summary>
        Chicane,

        /// <summary>
        /// Braking zone before a corner
        /// </summary>
        BrakingZone,

        /// <summary>
        /// Acceleration zone after a corner
        /// </summary>
        AccelerationZone,

        /// <summary>
        /// Hairpin turn (tight turn)
        /// </summary>
        Hairpin,

        /// <summary>
        /// Fast corner (high-speed turn)
        /// </summary>
        FastCorner,

        /// <summary>
        /// Slow corner (low-speed turn)
        /// </summary>
        SlowCorner,

        /// <summary>
        /// Complex corner (multiple apex points)
        /// </summary>
        ComplexCorner
    }
}
