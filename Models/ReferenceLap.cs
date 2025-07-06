using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LeMansUltimateCoPilot.Models
{
    /// <summary>
    /// Represents a reference lap with complete telemetry data and lap metadata.
    /// Used for storing and analyzing optimal lap performance for AI coaching.
    /// </summary>
    public class ReferenceLap
    {
        /// <summary>
        /// Unique identifier for this reference lap
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Track name where this lap was recorded
        /// </summary>
        public string TrackName { get; set; } = "";

        /// <summary>
        /// Vehicle used for this lap
        /// </summary>
        public string VehicleName { get; set; } = "";

        /// <summary>
        /// Total lap time in seconds
        /// </summary>
        public double LapTime { get; set; }

        /// <summary>
        /// When this lap was recorded
        /// </summary>
        public DateTime RecordedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// Lap number within the session
        /// </summary>
        public int LapNumber { get; set; }

        /// <summary>
        /// Whether this lap is considered valid (no cuts, penalties, etc.)
        /// </summary>
        public bool IsValid { get; set; } = true;

        /// <summary>
        /// Complete telemetry data for the entire lap
        /// </summary>
        public List<EnhancedTelemetryData> TelemetryData { get; set; } = new();

        /// <summary>
        /// Sector times for this lap (if sectors are defined)
        /// </summary>
        public List<double> SectorTimes { get; set; } = new();

        /// <summary>
        /// Performance metrics for this lap
        /// </summary>
        public LapPerformanceMetrics Performance { get; set; } = new();

        /// <summary>
        /// Additional metadata about the lap
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new();

        /// <summary>
        /// Constructor for creating a new reference lap
        /// </summary>
        public ReferenceLap()
        {
            // Initialize with default values
        }

        /// <summary>
        /// Constructor for creating a reference lap from telemetry data
        /// </summary>
        /// <param name="telemetryData">Complete telemetry data for the lap</param>
        /// <param name="lapNumber">Lap number within the session</param>
        public ReferenceLap(List<EnhancedTelemetryData> telemetryData, int lapNumber)
        {
            if (telemetryData == null || telemetryData.Count == 0)
                throw new ArgumentException("Telemetry data cannot be null or empty", nameof(telemetryData));

            TelemetryData = telemetryData;
            LapNumber = lapNumber;
            
            // Extract basic information from telemetry data
            var firstRecord = telemetryData.First();
            var lastRecord = telemetryData.Last();
            
            TrackName = firstRecord.TrackName;
            VehicleName = firstRecord.VehicleName;
            LapTime = lastRecord.LapTime;
            RecordedAt = firstRecord.Timestamp;
            IsValid = telemetryData.All(t => t.IsValidLap);
            
            // Calculate performance metrics
            CalculatePerformanceMetrics();
        }

        /// <summary>
        /// Calculate performance metrics from telemetry data
        /// </summary>
        private void CalculatePerformanceMetrics()
        {
            if (TelemetryData == null || TelemetryData.Count == 0)
                return;

            Performance.MaxSpeed = TelemetryData.Max(t => t.Speed);
            Performance.MinSpeed = TelemetryData.Min(t => t.Speed);
            Performance.AverageSpeed = TelemetryData.Average(t => t.Speed);
            
            Performance.MaxLongitudinalG = TelemetryData.Max(t => t.LongitudinalG);
            Performance.MinLongitudinalG = TelemetryData.Min(t => t.LongitudinalG);
            Performance.MaxLateralG = TelemetryData.Max(t => Math.Abs(t.LateralG));
            
            Performance.MaxThrottle = TelemetryData.Max(t => t.ThrottleInput);
            Performance.MaxBrake = TelemetryData.Max(t => t.BrakeInput);
            Performance.MaxSteering = TelemetryData.Max(t => Math.Abs(t.SteeringInput));
            
            Performance.FuelUsed = TelemetryData.First().FuelLevel - TelemetryData.Last().FuelLevel;
            Performance.DistanceTraveled = TelemetryData.Last().DistanceTraveled;
            
            // Calculate average tire temperatures
            Performance.AvgTireTemperature = TelemetryData.Average(t => 
                (t.TireTemperatureFL + t.TireTemperatureFR + t.TireTemperatureRL + t.TireTemperatureRR) / 4.0);
        }

        /// <summary>
        /// Get sector times from telemetry data
        /// </summary>
        /// <param name="sectorPositions">Track positions that define sector boundaries</param>
        /// <returns>List of sector times</returns>
        public List<double> CalculateSectorTimes(List<double> sectorPositions)
        {
            if (sectorPositions == null || sectorPositions.Count == 0 || TelemetryData == null)
                return new List<double>();

            var sectorTimes = new List<double>();
            var lastTime = 0.0;

            foreach (var sectorPosition in sectorPositions)
            {
                // Find the telemetry point closest to the sector position
                var sectorRecord = TelemetryData
                    .Where(t => t.LapProgress >= sectorPosition)
                    .OrderBy(t => Math.Abs(t.LapProgress - sectorPosition))
                    .FirstOrDefault();

                if (sectorRecord != null)
                {
                    var sectorTime = sectorRecord.LapTime - lastTime;
                    sectorTimes.Add(sectorTime);
                    lastTime = sectorRecord.LapTime;
                }
            }

            // Add final sector time (to finish line)
            if (TelemetryData.Count > 0)
            {
                var finalTime = TelemetryData.Last().LapTime - lastTime;
                sectorTimes.Add(finalTime);
            }

            SectorTimes = sectorTimes;
            return sectorTimes;
        }

        /// <summary>
        /// Export reference lap to JSON format
        /// </summary>
        /// <returns>JSON string representation of the reference lap</returns>
        public string ToJson()
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            
            return JsonSerializer.Serialize(this, options);
        }

        /// <summary>
        /// Load reference lap from JSON format
        /// </summary>
        /// <param name="json">JSON string representation</param>
        /// <returns>ReferenceLap object</returns>
        public static ReferenceLap FromJson(string json)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };
                
                return JsonSerializer.Deserialize<ReferenceLap>(json, options) ?? new ReferenceLap();
            }
            catch (JsonException)
            {
                return new ReferenceLap();
            }
        }

        /// <summary>
        /// Get a summary of this reference lap
        /// </summary>
        /// <returns>Formatted summary string</returns>
        public string GetSummary()
        {
            return $"Reference Lap: {TrackName} - {VehicleName}\n" +
                   $"Lap Time: {LapTime.ToString("F3", System.Globalization.CultureInfo.InvariantCulture)}s (Lap #{LapNumber})\n" +
                   $"Max Speed: {Performance.MaxSpeed.ToString("F1", System.Globalization.CultureInfo.InvariantCulture)} km/h\n" +
                   $"Recorded: {RecordedAt:yyyy-MM-dd HH:mm:ss}\n" +
                   $"Valid: {IsValid}\n" +
                   $"Data Points: {TelemetryData.Count}";
        }

        /// <summary>
        /// Validate that this reference lap meets quality criteria
        /// </summary>
        /// <returns>True if the lap meets quality standards</returns>
        public bool ValidateQuality()
        {
            if (TelemetryData == null || TelemetryData.Count < 100) // Minimum data points
                return false;

            if (LapTime <= 0 || LapTime > 600) // Reasonable lap time range
                return false;

            if (!IsValid)
                return false;

            // Check for data consistency
            var speedRange = Performance.MaxSpeed - Performance.MinSpeed;
            if (speedRange < 50) // Should have significant speed variation
                return false;

            return true;
        }
    }

    /// <summary>
    /// Performance metrics calculated from a reference lap
    /// </summary>
    public class LapPerformanceMetrics
    {
        public double MaxSpeed { get; set; }
        public double MinSpeed { get; set; }
        public double AverageSpeed { get; set; }
        
        public double MaxLongitudinalG { get; set; }
        public double MinLongitudinalG { get; set; }
        public double MaxLateralG { get; set; }
        
        public double MaxThrottle { get; set; }
        public double MaxBrake { get; set; }
        public double MaxSteering { get; set; }
        
        public double FuelUsed { get; set; }
        public double DistanceTraveled { get; set; }
        public double AvgTireTemperature { get; set; }
    }
}
