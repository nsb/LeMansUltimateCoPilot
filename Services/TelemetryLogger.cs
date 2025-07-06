using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using LMUSharedMemoryTest.Models;

namespace LMUSharedMemoryTest.Services
{
    /// <summary>
    /// Enhanced telemetry logging service for AI driving coach
    /// Handles real-time data logging with session management and data validation
    /// </summary>
    public class TelemetryLogger : IDisposable
    {
        private StreamWriter? _csvWriter;
        private readonly string _logDirectory;
        private string? _currentSessionFile;
        private bool _isLogging;
        private readonly object _lockObject = new();
        private int _recordCount;
        private DateTime _sessionStartTime;
        private EnhancedTelemetryData? _previousData;

        // Session metadata
        public string CurrentTrack { get; private set; } = "";
        public string CurrentVehicle { get; private set; } = "";
        public DateTime SessionStartTime => _sessionStartTime;
        public int RecordCount => _recordCount;
        public bool IsLogging => _isLogging;

        // Events for monitoring
        public event EventHandler<string>? LogMessage;
        public event EventHandler<EnhancedTelemetryData>? DataLogged;
        public event EventHandler<string>? SessionStarted;
        public event EventHandler<string>? SessionStopped;

        public TelemetryLogger(string? logDirectory = null)
        {
            _logDirectory = logDirectory ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "LMU_Telemetry_Logs");

            // Ensure log directory exists
            Directory.CreateDirectory(_logDirectory);
            LogMessage?.Invoke(this, $"Telemetry logger initialized. Log directory: {_logDirectory}");
        }

        /// <summary>
        /// Start a new logging session
        /// </summary>
        public bool StartLoggingSession(string sessionName = "")
        {
            try
            {
                lock (_lockObject)
                {
                    if (_isLogging)
                    {
                        LogMessage?.Invoke(this, "Already logging. Stop current session first.");
                        return false;
                    }

                    _sessionStartTime = DateTime.Now;
                    _recordCount = 0;
                    _previousData = null;

                    // Generate session filename
                    var timestamp = _sessionStartTime.ToString("yyyyMMdd_HHmmss");
                    var fileName = string.IsNullOrEmpty(sessionName) 
                        ? $"telemetry_{timestamp}.csv"
                        : $"telemetry_{sessionName}_{timestamp}.csv";

                    _currentSessionFile = Path.Combine(_logDirectory, fileName);

                    // Create CSV file and write header
                    _csvWriter = new StreamWriter(_currentSessionFile, false);
                    _csvWriter.WriteLine($"# Telemetry Session Started: {_sessionStartTime:yyyy-MM-dd HH:mm:ss}");
                    _csvWriter.WriteLine($"# Session Name: {sessionName}");
                    _csvWriter.WriteLine($"# Log Format Version: 1.0");
                    _csvWriter.WriteLine("#");
                    _csvWriter.WriteLine(EnhancedTelemetryData.GetCSVHeader());

                    _isLogging = true;
                }

                LogMessage?.Invoke(this, $"Started logging session: {_currentSessionFile}");
                SessionStarted?.Invoke(this, _currentSessionFile);
                return true;
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke(this, $"Failed to start logging session: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Stop the current logging session
        /// </summary>
        public bool StopLoggingSession()
        {
            try
            {
                lock (_lockObject)
                {
                    if (!_isLogging || _csvWriter == null)
                    {
                        LogMessage?.Invoke(this, "No active logging session to stop.");
                        return false;
                    }

                    // Write session summary
                    var sessionDuration = DateTime.Now - _sessionStartTime;
                    _csvWriter.WriteLine("#");
                    _csvWriter.WriteLine($"# Session Ended: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                    _csvWriter.WriteLine($"# Session Duration: {sessionDuration.TotalMinutes:F1} minutes");
                    _csvWriter.WriteLine($"# Total Records: {_recordCount}");
                    _csvWriter.WriteLine($"# Track: {CurrentTrack}");
                    _csvWriter.WriteLine($"# Vehicle: {CurrentVehicle}");

                    _csvWriter.Dispose();
                    _csvWriter = null;
                    _isLogging = false;
                }

                LogMessage?.Invoke(this, $"Stopped logging session. Total records: {_recordCount}");
                SessionStopped?.Invoke(this, _currentSessionFile ?? "");
                return true;
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke(this, $"Error stopping logging session: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Log telemetry data point
        /// </summary>
        public bool LogTelemetryData(EnhancedTelemetryData data)
        {
            if (!_isLogging || _csvWriter == null)
                return false;

            try
            {
                // Validate data quality
                if (!IsDataValid(data))
                {
                    return false;
                }

                // Calculate additional derived values if we have previous data
                if (_previousData != null)
                {
                    CalculateDerivedValues(data, _previousData);
                }

                lock (_lockObject)
                {
                    // Update session metadata
                    if (!string.IsNullOrEmpty(data.TrackName) && CurrentTrack != data.TrackName)
                    {
                        CurrentTrack = data.TrackName;
                        LogMessage?.Invoke(this, $"Track detected: {CurrentTrack}");
                    }

                    if (!string.IsNullOrEmpty(data.VehicleName) && CurrentVehicle != data.VehicleName)
                    {
                        CurrentVehicle = data.VehicleName;
                        LogMessage?.Invoke(this, $"Vehicle detected: {CurrentVehicle}");
                    }

                    // Write data to CSV
                    _csvWriter.WriteLine(data.ToCSVRow());
                    _csvWriter.Flush(); // Ensure data is written immediately

                    _recordCount++;
                }

                // Store for next iteration
                _previousData = data;

                // Raise event
                DataLogged?.Invoke(this, data);

                return true;
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke(this, $"Error logging telemetry data: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Validate telemetry data quality
        /// </summary>
        private bool IsDataValid(EnhancedTelemetryData data)
        {
            // Check for obviously invalid data
            if (float.IsNaN(data.Speed) || float.IsInfinity(data.Speed))
                return false;

            if (float.IsNaN(data.EngineRPM) || float.IsInfinity(data.EngineRPM))
                return false;

            if (data.Speed < 0 || data.Speed > 500) // Sanity check for speed
                return false;

            if (data.EngineRPM < 0 || data.EngineRPM > 20000) // Sanity check for RPM
                return false;

            // Check for reasonable input values
            if (data.ThrottleInput < 0 || data.ThrottleInput > 1)
                return false;

            if (data.BrakeInput < 0 || data.BrakeInput > 1)
                return false;

            return true;
        }

        /// <summary>
        /// Calculate derived values that require previous data point
        /// </summary>
        private void CalculateDerivedValues(EnhancedTelemetryData current, EnhancedTelemetryData previous)
        {
            if (current.DeltaTime > 0 && current.DeltaTime < 1.0f) // Reasonable delta time
            {
                // Calculate distance traveled since last update
                var deltaDistance = current.SpeedMPS * current.DeltaTime;
                current.DistanceTraveled = previous.DistanceTraveled + deltaDistance;

                // Calculate lap progress (simplified - would need track-specific data for accuracy)
                if (current.LapNumber > previous.LapNumber)
                {
                    // New lap started
                    current.LapProgress = 0.0f;
                    current.DistanceTraveled = 0.0f;
                }
                else if (current.LapNumber == previous.LapNumber && current.LapTime > previous.LapTime)
                {
                    // Estimate progress based on typical lap time (this is rough estimation)
                    var estimatedLapTime = 120.0f; // Default 2 minutes - would be track specific
                    current.LapProgress = Math.Min(1.0f, (float)(current.LapTime / estimatedLapTime));
                }
            }
        }

        /// <summary>
        /// Get list of available log files
        /// </summary>
        public List<string> GetAvailableLogFiles()
        {
            var logFiles = new List<string>();
            try
            {
                var files = Directory.GetFiles(_logDirectory, "telemetry_*.csv");
                logFiles.AddRange(files);
                logFiles.Sort((a, b) => File.GetCreationTime(b).CompareTo(File.GetCreationTime(a))); // Newest first
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke(this, $"Error getting log files: {ex.Message}");
            }
            return logFiles;
        }

        /// <summary>
        /// Get session statistics for a log file
        /// </summary>
        public async Task<SessionStatistics?> GetSessionStatistics(string logFilePath)
        {
            try
            {
                if (!File.Exists(logFilePath))
                    return null;

                var stats = new SessionStatistics
                {
                    FilePath = logFilePath,
                    FileName = Path.GetFileName(logFilePath),
                    FileSize = new FileInfo(logFilePath).Length
                };

                // Read file to extract statistics
                using var reader = new StreamReader(logFilePath);
                string? line;
                int recordCount = 0;
                DateTime? sessionStart = null;
                DateTime? sessionEnd = null;
                string track = "";
                string vehicle = "";

                while ((line = await reader.ReadLineAsync()) != null)
                {
                    if (line.StartsWith("# Telemetry Session Started:"))
                    {
                        if (DateTime.TryParse(line.Substring(29), out var start))
                            sessionStart = start;
                    }
                    else if (line.StartsWith("# Session Ended:"))
                    {
                        if (DateTime.TryParse(line.Substring(17), out var end))
                            sessionEnd = end;
                    }
                    else if (line.StartsWith("# Track:"))
                    {
                        track = line.Substring(8).Trim();
                    }
                    else if (line.StartsWith("# Vehicle:"))
                    {
                        vehicle = line.Substring(10).Trim();
                    }
                    else if (!line.StartsWith("#") && !string.IsNullOrEmpty(line))
                    {
                        recordCount++;
                    }
                }

                stats.RecordCount = recordCount;
                stats.SessionStart = sessionStart;
                stats.SessionEnd = sessionEnd;
                stats.Track = track;
                stats.Vehicle = vehicle;

                if (sessionStart.HasValue && sessionEnd.HasValue)
                {
                    stats.Duration = sessionEnd.Value - sessionStart.Value;
                }

                return stats;
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke(this, $"Error reading session statistics: {ex.Message}");
                return null;
            }
        }

        public void Dispose()
        {
            if (_isLogging)
            {
                StopLoggingSession();
            }
            _csvWriter?.Dispose();
        }
    }

    /// <summary>
    /// Session statistics for log file analysis
    /// </summary>
    public class SessionStatistics
    {
        public string FilePath { get; set; } = "";
        public string FileName { get; set; } = "";
        public long FileSize { get; set; }
        public int RecordCount { get; set; }
        public DateTime? SessionStart { get; set; }
        public DateTime? SessionEnd { get; set; }
        public TimeSpan? Duration { get; set; }
        public string Track { get; set; } = "";
        public string Vehicle { get; set; } = "";
    }
}
