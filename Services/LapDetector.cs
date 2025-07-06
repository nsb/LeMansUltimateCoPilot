using System;
using System.Collections.Generic;
using System.Linq;
using LMUSharedMemoryTest.Models;

namespace LMUSharedMemoryTest.Services
{
    /// <summary>
    /// Service for detecting completed laps from telemetry data stream.
    /// Monitors lap progress and identifies when a lap is completed.
    /// </summary>
    public class LapDetector
    {
        private readonly List<EnhancedTelemetryData> _currentLapData = new();
        private int _currentLapNumber = 0;
        private bool _isLapInProgress = false;
        private double _lastLapProgress = 0.0;
        private double _lapStartTime = 0.0;

        /// <summary>
        /// Event fired when a lap is completed
        /// </summary>
        public event EventHandler<LapCompletedEventArgs>? LapCompleted;

        /// <summary>
        /// Event fired when a new lap starts
        /// </summary>
        public event EventHandler<LapStartedEventArgs>? LapStarted;

        /// <summary>
        /// Configuration for lap detection
        /// </summary>
        public LapDetectionConfig Config { get; set; } = new();

        /// <summary>
        /// Current lap number being tracked
        /// </summary>
        public int CurrentLapNumber => _currentLapNumber;

        /// <summary>
        /// Whether a lap is currently in progress
        /// </summary>
        public bool IsLapInProgress => _isLapInProgress;

        /// <summary>
        /// Number of telemetry points collected for the current lap
        /// </summary>
        public int CurrentLapDataPoints => _currentLapData.Count;

        /// <summary>
        /// Process new telemetry data and detect lap completion
        /// </summary>
        /// <param name="telemetryData">New telemetry data point</param>
        public void ProcessTelemetryData(EnhancedTelemetryData telemetryData)
        {
            if (telemetryData == null)
                return;

            // If lap is in progress, check for completion first
            if (_isLapInProgress && ShouldCompleteLap(telemetryData))
            {
                CompleteLap(telemetryData);
            }

            // Detect lap start
            if (!_isLapInProgress && ShouldStartLap(telemetryData))
            {
                StartNewLap(telemetryData);
            }
            // Add data to current lap if in progress (but not if we just started the lap)
            else if (_isLapInProgress)
            {
                _currentLapData.Add(telemetryData);
            }

            _lastLapProgress = telemetryData.LapProgress;
        }

        /// <summary>
        /// Determine if a new lap should start
        /// </summary>
        /// <param name="telemetryData">Current telemetry data</param>
        /// <returns>True if a new lap should start</returns>
        private bool ShouldStartLap(EnhancedTelemetryData telemetryData)
        {
            // Start first lap when progress is low and no lap is in progress
            if (_currentLapNumber == 0 && telemetryData.LapProgress < Config.LapStartThreshold)
                return true;

            // Start new lap when lap number changes
            if (telemetryData.LapNumber > _currentLapNumber)
                return true;

            // Start subsequent laps when crossing start/finish line (lap progress resets to near 0)
            return telemetryData.LapProgress < Config.LapStartThreshold && 
                   _lastLapProgress > Config.LapCompleteThreshold;
        }

        /// <summary>
        /// Determine if the current lap should be completed
        /// </summary>
        /// <param name="telemetryData">Current telemetry data</param>
        /// <returns>True if the lap should be completed</returns>
        private bool ShouldCompleteLap(EnhancedTelemetryData telemetryData)
        {
            // Complete lap when crossing finish line (lap progress goes from high to low)
            bool crossedFinishLine = telemetryData.LapProgress < Config.LapStartThreshold && 
                                   _lastLapProgress > Config.LapCompleteThreshold;

            // Also check if lap number changed
            bool lapNumberChanged = telemetryData.LapNumber > _currentLapNumber;

            // Minimum lap time to avoid false positives
            double currentLapTime = telemetryData.LapTime - _lapStartTime;
            bool minimumTimeElapsed = currentLapTime > Config.MinimumLapTime;

            return (crossedFinishLine || lapNumberChanged) && minimumTimeElapsed;
        }

        /// <summary>
        /// Start tracking a new lap
        /// </summary>
        /// <param name="telemetryData">Telemetry data at lap start</param>
        private void StartNewLap(EnhancedTelemetryData telemetryData)
        {
            _currentLapNumber = telemetryData.LapNumber;
            _isLapInProgress = true;
            _lapStartTime = telemetryData.LapTime;
            _currentLapData.Clear();
            _currentLapData.Add(telemetryData);

            // Fire lap started event
            LapStarted?.Invoke(this, new LapStartedEventArgs
            {
                LapNumber = _currentLapNumber,
                TrackName = telemetryData.TrackName,
                VehicleName = telemetryData.VehicleName,
                StartTime = telemetryData.Timestamp
            });
        }

        /// <summary>
        /// Complete the current lap
        /// </summary>
        /// <param name="telemetryData">Telemetry data at lap completion</param>
        private void CompleteLap(EnhancedTelemetryData telemetryData)
        {
            if (!_isLapInProgress || _currentLapData.Count == 0)
                return;

            // Create a copy of the lap data
            var lapData = new List<EnhancedTelemetryData>(_currentLapData);
            var lapTime = telemetryData.LapTime - _lapStartTime;

            // Validate lap quality
            bool isValidLap = ValidateLap(lapData, lapTime);

            // Fire lap completed event
            LapCompleted?.Invoke(this, new LapCompletedEventArgs
            {
                LapNumber = _currentLapNumber,
                LapTime = lapTime,
                TelemetryData = lapData,
                IsValid = isValidLap,
                TrackName = telemetryData.TrackName,
                VehicleName = telemetryData.VehicleName,
                CompletedAt = telemetryData.Timestamp
            });

            // Reset for next lap
            _isLapInProgress = false;
            _currentLapData.Clear();
        }

        /// <summary>
        /// Validate lap quality based on various criteria
        /// </summary>
        /// <param name="lapData">Complete lap telemetry data</param>
        /// <param name="lapTime">Total lap time</param>
        /// <returns>True if lap meets quality standards</returns>
        private bool ValidateLap(List<EnhancedTelemetryData> lapData, double lapTime)
        {
            if (lapData == null || lapData.Count < Config.MinimumDataPoints)
                return false;

            // Check lap time is reasonable
            if (lapTime < Config.MinimumLapTime || lapTime > Config.MaximumLapTime)
                return false;

            // Check for consistent data
            var trackName = lapData.First().TrackName;
            var vehicleName = lapData.First().VehicleName;
            
            if (lapData.Any(d => d.TrackName != trackName || d.VehicleName != vehicleName))
                return false;

            // Check for reasonable speed variation
            var maxSpeed = lapData.Max(d => d.Speed);
            var minSpeed = lapData.Min(d => d.Speed);
            
            if (maxSpeed - minSpeed < Config.MinimumSpeedVariation)
                return false;

            // Check all data points are marked as valid
            if (Config.RequireValidLapFlag && lapData.Any(d => !d.IsValidLap))
                return false;

            return true;
        }

        /// <summary>
        /// Reset the lap detector state
        /// </summary>
        public void Reset()
        {
            _currentLapData.Clear();
            _currentLapNumber = 0;
            _isLapInProgress = false;
            _lastLapProgress = 0.0;
            _lapStartTime = 0.0;
        }

        /// <summary>
        /// Get current lap progress information
        /// </summary>
        /// <returns>Current lap progress info</returns>
        public LapProgressInfo GetCurrentLapProgress()
        {
            return new LapProgressInfo
            {
                LapNumber = _currentLapNumber,
                IsInProgress = _isLapInProgress,
                DataPoints = _currentLapData.Count,
                ElapsedTime = _currentLapData.Count > 0 ? 
                    _currentLapData.Last().LapTime - _lapStartTime : 0.0,
                LastLapProgress = _lastLapProgress
            };
        }
    }

    /// <summary>
    /// Configuration for lap detection behavior
    /// </summary>
    public class LapDetectionConfig
    {
        /// <summary>
        /// Lap progress threshold for detecting lap start (default: 0.1 = 10%)
        /// </summary>
        public double LapStartThreshold { get; set; } = 0.1;

        /// <summary>
        /// Lap progress threshold for detecting lap completion (default: 0.9 = 90%)
        /// </summary>
        public double LapCompleteThreshold { get; set; } = 0.9;

        /// <summary>
        /// Minimum lap time in seconds to avoid false positives (default: 30 seconds)
        /// </summary>
        public double MinimumLapTime { get; set; } = 30.0;

        /// <summary>
        /// Maximum lap time in seconds to filter out invalid laps (default: 600 seconds)
        /// </summary>
        public double MaximumLapTime { get; set; } = 600.0;

        /// <summary>
        /// Minimum number of telemetry data points for a valid lap (default: 100)
        /// </summary>
        public int MinimumDataPoints { get; set; } = 100;

        /// <summary>
        /// Minimum speed variation required for a valid lap (default: 50 km/h)
        /// </summary>
        public double MinimumSpeedVariation { get; set; } = 50.0;

        /// <summary>
        /// Whether to require the IsValidLap flag to be true (default: true)
        /// </summary>
        public bool RequireValidLapFlag { get; set; } = true;
    }

    /// <summary>
    /// Event arguments for lap completion
    /// </summary>
    public class LapCompletedEventArgs : EventArgs
    {
        public int LapNumber { get; set; }
        public double LapTime { get; set; }
        public List<EnhancedTelemetryData> TelemetryData { get; set; } = new();
        public bool IsValid { get; set; }
        public string TrackName { get; set; } = "";
        public string VehicleName { get; set; } = "";
        public DateTime CompletedAt { get; set; }
    }

    /// <summary>
    /// Event arguments for lap start
    /// </summary>
    public class LapStartedEventArgs : EventArgs
    {
        public int LapNumber { get; set; }
        public string TrackName { get; set; } = "";
        public string VehicleName { get; set; } = "";
        public DateTime StartTime { get; set; }
    }

    /// <summary>
    /// Current lap progress information
    /// </summary>
    public class LapProgressInfo
    {
        public int LapNumber { get; set; }
        public bool IsInProgress { get; set; }
        public int DataPoints { get; set; }
        public double ElapsedTime { get; set; }
        public double LastLapProgress { get; set; }
    }
}
