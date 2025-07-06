using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace LeMansUltimateCoPilot.Models
{
    /// <summary>
    /// Complete track configuration including all segments and metadata
    /// Used for AI coaching analysis and performance comparison
    /// </summary>
    public class TrackConfiguration
    {
        /// <summary>
        /// Unique identifier for this track configuration
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Official name of the track
        /// </summary>
        [JsonPropertyName("trackName")]
        public string TrackName { get; set; } = string.Empty;

        /// <summary>
        /// Track variant or layout name (e.g., "GP", "National", "Club")
        /// </summary>
        [JsonPropertyName("trackVariant")]
        public string TrackVariant { get; set; } = string.Empty;

        /// <summary>
        /// Country where the track is located
        /// </summary>
        [JsonPropertyName("country")]
        public string Country { get; set; } = string.Empty;

        /// <summary>
        /// Total length of the track in meters
        /// </summary>
        [JsonPropertyName("trackLength")]
        public double TrackLength { get; set; }

        /// <summary>
        /// Width of the track in meters (average)
        /// </summary>
        [JsonPropertyName("trackWidth")]
        public double TrackWidth { get; set; }

        /// <summary>
        /// Number of turns on the track
        /// </summary>
        [JsonPropertyName("numberOfTurns")]
        public int NumberOfTurns { get; set; }

        /// <summary>
        /// Direction of the track (clockwise or counterclockwise)
        /// </summary>
        [JsonPropertyName("trackDirection")]
        public TrackDirection TrackDirection { get; set; }

        /// <summary>
        /// Start/finish line position (X coordinate)
        /// </summary>
        [JsonPropertyName("startFinishX")]
        public double StartFinishX { get; set; }

        /// <summary>
        /// Start/finish line position (Y coordinate)
        /// </summary>
        [JsonPropertyName("startFinishY")]
        public double StartFinishY { get; set; }

        /// <summary>
        /// Start/finish line position (Z coordinate)
        /// </summary>
        [JsonPropertyName("startFinishZ")]
        public double StartFinishZ { get; set; }

        /// <summary>
        /// Pit lane entry position (X coordinate)
        /// </summary>
        [JsonPropertyName("pitEntryX")]
        public double PitEntryX { get; set; }

        /// <summary>
        /// Pit lane entry position (Y coordinate)
        /// </summary>
        [JsonPropertyName("pitEntryY")]
        public double PitEntryY { get; set; }

        /// <summary>
        /// Pit lane exit position (X coordinate)
        /// </summary>
        [JsonPropertyName("pitExitX")]
        public double PitExitX { get; set; }

        /// <summary>
        /// Pit lane exit position (Y coordinate)
        /// </summary>
        [JsonPropertyName("pitExitY")]
        public double PitExitY { get; set; }

        /// <summary>
        /// Collection of all track segments
        /// </summary>
        [JsonPropertyName("segments")]
        public List<TrackSegment> Segments { get; set; } = new List<TrackSegment>();

        /// <summary>
        /// Sector 1 end distance in meters
        /// </summary>
        [JsonPropertyName("sector1End")]
        public double Sector1End { get; set; }

        /// <summary>
        /// Sector 2 end distance in meters
        /// </summary>
        [JsonPropertyName("sector2End")]
        public double Sector2End { get; set; }

        /// <summary>
        /// Minimum elevation on the track in meters
        /// </summary>
        [JsonPropertyName("minElevation")]
        public double MinElevation { get; set; }

        /// <summary>
        /// Maximum elevation on the track in meters
        /// </summary>
        [JsonPropertyName("maxElevation")]
        public double MaxElevation { get; set; }

        /// <summary>
        /// Total elevation change on the track in meters
        /// </summary>
        [JsonPropertyName("totalElevationChange")]
        public double TotalElevationChange { get; set; }

        /// <summary>
        /// Version of the track configuration
        /// </summary>
        [JsonPropertyName("version")]
        public string Version { get; set; } = "1.0";

        /// <summary>
        /// When this configuration was created
        /// </summary>
        [JsonPropertyName("createdDate")]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// When this configuration was last updated
        /// </summary>
        [JsonPropertyName("lastUpdated")]
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Additional notes about the track
        /// </summary>
        [JsonPropertyName("notes")]
        public string Notes { get; set; } = string.Empty;

        /// <summary>
        /// Creates a new track configuration with default values
        /// </summary>
        public TrackConfiguration()
        {
            Id = Guid.NewGuid().ToString();
            TrackDirection = TrackDirection.Clockwise;
        }

        /// <summary>
        /// Creates a new track configuration with specified name and variant
        /// </summary>
        /// <param name="trackName">Name of the track</param>
        /// <param name="trackVariant">Variant or layout name</param>
        public TrackConfiguration(string trackName, string trackVariant = "") : this()
        {
            TrackName = trackName;
            TrackVariant = trackVariant;
        }

        /// <summary>
        /// Adds a new segment to the track configuration
        /// </summary>
        /// <param name="segment">Track segment to add</param>
        public void AddSegment(TrackSegment segment)
        {
            if (segment == null)
                throw new ArgumentNullException(nameof(segment));

            Segments.Add(segment);
            LastUpdated = DateTime.UtcNow;
        }

        /// <summary>
        /// Gets the segment at the specified distance from track start
        /// </summary>
        /// <param name="distanceFromStart">Distance in meters</param>
        /// <returns>Track segment at the specified distance, or null if not found</returns>
        public TrackSegment? GetSegmentAtDistance(double distanceFromStart)
        {
            return Segments.FirstOrDefault(s => 
                distanceFromStart >= s.DistanceFromStart && 
                distanceFromStart < s.DistanceFromStart + s.SegmentLength);
        }

        /// <summary>
        /// Gets the segment closest to the specified position
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        /// <param name="z">Z coordinate</param>
        /// <returns>Closest track segment</returns>
        public TrackSegment? GetClosestSegment(double x, double y, double z)
        {
            if (!Segments.Any())
                return null;

            return Segments.OrderBy(s => s.DistanceTo(x, y, z)).First();
        }

        /// <summary>
        /// Gets all corner segments on the track
        /// </summary>
        /// <returns>List of corner segments</returns>
        public List<TrackSegment> GetCornerSegments()
        {
            return Segments.Where(s => s.IsCorner()).ToList();
        }

        /// <summary>
        /// Gets all braking zone segments on the track
        /// </summary>
        /// <returns>List of braking zone segments</returns>
        public List<TrackSegment> GetBrakingZoneSegments()
        {
            return Segments.Where(s => s.IsBrakingZone()).ToList();
        }

        /// <summary>
        /// Gets segments in the specified sector
        /// </summary>
        /// <param name="sector">Sector number (1, 2, or 3)</param>
        /// <returns>List of segments in the sector</returns>
        public List<TrackSegment> GetSectorSegments(int sector)
        {
            return sector switch
            {
                1 => Segments.Where(s => s.DistanceFromStart < Sector1End).ToList(),
                2 => Segments.Where(s => s.DistanceFromStart >= Sector1End && s.DistanceFromStart < Sector2End).ToList(),
                3 => Segments.Where(s => s.DistanceFromStart >= Sector2End).ToList(),
                _ => new List<TrackSegment>()
            };
        }

        /// <summary>
        /// Calculates the total number of segments
        /// </summary>
        /// <returns>Total segment count</returns>
        public int GetSegmentCount()
        {
            return Segments.Count;
        }

        /// <summary>
        /// Calculates the average segment length
        /// </summary>
        /// <returns>Average segment length in meters</returns>
        public double GetAverageSegmentLength()
        {
            if (!Segments.Any())
                return 0.0;

            return Segments.Average(s => s.SegmentLength);
        }

        /// <summary>
        /// Gets the track's difficulty rating based on segment ratings
        /// </summary>
        /// <returns>Average difficulty rating (1-10)</returns>
        public double GetTrackDifficultyRating()
        {
            if (!Segments.Any())
                return 1.0;

            return Segments.Average(s => s.DifficultyRating);
        }

        /// <summary>
        /// Validates the track configuration for completeness and consistency
        /// </summary>
        /// <returns>True if configuration is valid</returns>
        public bool IsValid()
        {
            if (string.IsNullOrEmpty(TrackName))
                return false;

            if (TrackLength <= 0)
                return false;

            if (!Segments.Any())
                return false;

            // Check if segments are properly ordered
            for (int i = 0; i < Segments.Count - 1; i++)
            {
                if (Segments[i].SegmentNumber >= Segments[i + 1].SegmentNumber)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Returns a string representation of the track configuration
        /// </summary>
        /// <returns>String describing the track</returns>
        public override string ToString()
        {
            var culture = System.Globalization.CultureInfo.InvariantCulture;
            string variant = string.IsNullOrEmpty(TrackVariant) ? "" : $" ({TrackVariant})";
            return $"{TrackName}{variant} - {TrackLength.ToString("F0", culture)}m, {Segments.Count} segments";
        }
    }

    /// <summary>
    /// Enumeration of track directions
    /// </summary>
    public enum TrackDirection
    {
        /// <summary>
        /// Track runs clockwise
        /// </summary>
        Clockwise,

        /// <summary>
        /// Track runs counterclockwise
        /// </summary>
        Counterclockwise
    }
}
