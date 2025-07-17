using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeMansUltimateCoPilot.Models;
using LeMansUltimateCoPilot.Analysis;

namespace LeMansUltimateCoPilot.Logging
{
    /// <summary>
    /// Enhanced telemetry logger with cornering analysis support
    /// </summary>
    public class EnhancedTelemetryLogger
    {
        private readonly string _logDirectory;
        private readonly string _sessionLogPath;
        private readonly string _corneringLogPath;
        private readonly Queue<EnhancedTelemetryData> _telemetryBuffer = new Queue<EnhancedTelemetryData>();
        private readonly Queue<CorneringAnalysisResult> _corneringBuffer = new Queue<CorneringAnalysisResult>();
        private readonly object _lockObject = new object();
        private bool _isLogging = false;
        private DateTime _sessionStartTime;
        private string _currentTrack = "";
        private string _currentVehicle = "";

        public EnhancedTelemetryLogger(string logDirectory = "Logs")
        {
            _logDirectory = logDirectory;
            _sessionStartTime = DateTime.Now;
            
            // Create log directory if it doesn't exist
            if (!Directory.Exists(_logDirectory))
            {
                Directory.CreateDirectory(_logDirectory);
            }

            // Create session-specific log files
            string sessionId = _sessionStartTime.ToString("yyyy-MM-dd_HH-mm-ss");
            _sessionLogPath = Path.Combine(_logDirectory, $"telemetry_{sessionId}.csv");
            _corneringLogPath = Path.Combine(_logDirectory, $"cornering_{sessionId}.csv");

            // Don't initialize log files until logging starts
        }

        /// <summary>
        /// Initialize log files with headers
        /// </summary>
        private void InitializeLogFiles()
        {
            // Initialize telemetry log
            using (var writer = new StreamWriter(_sessionLogPath, false, Encoding.UTF8))
            {
                writer.WriteLine("Timestamp,SessionTime,Track,Vehicle,Lap,LapTime,Position,Speed,RPM,Gear," +
                               "Throttle,Brake,Clutch,Steering,TireTemp_FL,TireTemp_FR,TireTemp_RL,TireTemp_RR," +
                               "TirePressure_FL,TirePressure_FR,TirePressure_RL,TirePressure_RR,WaterTemp,OilTemp," +
                               "FuelLevel,FuelCapacity,EngineMaxRPM,LateralG,LongitudinalG,LocalVelX,LocalVelY,LocalVelZ," +
                               "IsInCorner,CornerPhase,CornerDirection,LapDistance,TrackLength");
            }

            // Initialize cornering analysis log
            using (var writer = new StreamWriter(_corneringLogPath, false, Encoding.UTF8))
            {
                writer.WriteLine("Timestamp,CornerStartTime,CornerEndTime,Direction,EntrySpeed,ApexSpeed,ExitSpeed," +
                               "MaxLateralG,Duration,PerformanceScore,EntryBrakeMax,ExitThrottleSmooth,SteeringSmoothness," +
                               "FeedbackCount,FeedbackHighPriority,CoachingMessage");
            }
        }

        /// <summary>
        /// Log telemetry data
        /// </summary>
        public void LogTelemetryData(EnhancedTelemetryData data)
        {
            if (!_isLogging) return;

            lock (_lockObject)
            {
                _telemetryBuffer.Enqueue(data);
                
                // Process buffer when it reaches a certain size
                if (_telemetryBuffer.Count >= 10)
                {
                    ProcessTelemetryBuffer();
                }
            }
        }

        /// <summary>
        /// Log cornering analysis result
        /// </summary>
        public void LogCorneringAnalysis(CorneringAnalysisResult result)
        {
            if (!_isLogging) return;

            lock (_lockObject)
            {
                _corneringBuffer.Enqueue(result);
                
                // Process cornering buffer immediately for completed corners
                if (result.CompletedCorner != null)
                {
                    ProcessCorneringBuffer();
                }
            }
        }

        /// <summary>
        /// Process telemetry buffer
        /// </summary>
        private void ProcessTelemetryBuffer(bool forceFlush = false)
        {
            if (!_isLogging && !forceFlush) return; // Don't process if not logging unless forced
            
            try
            {
                using (var writer = new StreamWriter(_sessionLogPath, true, Encoding.UTF8))
                {
                    while (_telemetryBuffer.Count > 0)
                    {
                        var data = _telemetryBuffer.Dequeue();
                        writer.WriteLine(FormatTelemetryData(data));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error writing telemetry data: {ex.Message}");
            }
        }

        /// <summary>
        /// Process cornering buffer
        /// </summary>
        /// <summary>
        /// Process cornering buffer
        /// </summary>
        private void ProcessCorneringBuffer(bool forceFlush = false)
        {
            if (!_isLogging && !forceFlush) return; // Don't process if not logging unless forced
            
            try
            {
                using (var writer = new StreamWriter(_corneringLogPath, true, Encoding.UTF8))
                {
                    while (_corneringBuffer.Count > 0)
                    {
                        var result = _corneringBuffer.Dequeue();
                        if (result.CompletedCorner != null)
                        {
                            writer.WriteLine(FormatCorneringData(result));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error writing cornering data: {ex.Message}");
            }
        }

        /// <summary>
        /// Format telemetry data for CSV output
        /// </summary>
        private string FormatTelemetryData(EnhancedTelemetryData data)
        {
            return $"{data.Timestamp:yyyy-MM-dd HH:mm:ss.fff}," +
                   $"{data.SessionTime:F3}," +
                   $"\"{data.TrackName}\"," +
                   $"\"{data.VehicleName}\"," +
                   $"{data.LapNumber}," +
                   $"{data.LapTime:F3}," +
                   $"{0}," + // Position placeholder
                   $"{data.Speed:F2}," +
                   $"{data.EngineRPM}," +
                   $"{data.Gear}," +
                   $"{data.ThrottleInput:F3}," +
                   $"{data.BrakeInput:F3}," +
                   $"{data.ClutchInput:F3}," +
                   $"{data.SteeringInput:F3}," +
                   $"{data.TireTemperatureFL:F1}," +
                   $"{data.TireTemperatureFR:F1}," +
                   $"{data.TireTemperatureRL:F1}," +
                   $"{data.TireTemperatureRR:F1}," +
                   $"{data.TirePressureFL:F2}," +
                   $"{data.TirePressureFR:F2}," +
                   $"{data.TirePressureRL:F2}," +
                   $"{data.TirePressureRR:F2}," +
                   $"{data.WaterTemperature:F1}," +
                   $"{data.OilTemperature:F1}," +
                   $"{data.FuelLevel:F2}," +
                   $"{0:F2}," + // FuelCapacity placeholder
                   $"{data.MaxRPM}," +
                   $"{data.LateralG:F3}," +
                   $"{data.LongitudinalG:F3}," +
                   $"{data.VelocityX:F3}," +
                   $"{data.VelocityY:F3}," +
                   $"{data.VelocityZ:F3}," +
                   $"{false}," + // IsInCorner placeholder
                   $"{"Unknown"}," + // CornerPhase placeholder
                   $"{"Straight"}," + // CornerDirection placeholder
                   $"{data.DistanceFromStart:F3}," +
                   $"{0:F3}"; // TrackLength placeholder
        }

        /// <summary>
        /// Format cornering data for CSV output
        /// </summary>
        private string FormatCorneringData(CorneringAnalysisResult result)
        {
            var corner = result.CompletedCorner;
            var highPriorityFeedback = result.CoachingFeedback.Count(f => f.Priority == FeedbackPriority.High || f.Priority == FeedbackPriority.Critical);
            var coachingMessage = string.Join("; ", result.CoachingFeedback.Select(f => f.Message));

            return $"{result.Timestamp:yyyy-MM-dd HH:mm:ss.fff}," +
                   $"{corner.StartTime:yyyy-MM-dd HH:mm:ss.fff}," +
                   $"{corner.EndTime:yyyy-MM-dd HH:mm:ss.fff}," +
                   $"{corner.Direction}," +
                   $"{corner.EntrySpeed:F2}," +
                   $"{corner.ApexSpeed:F2}," +
                   $"{corner.ExitSpeed:F2}," +
                   $"{corner.MaxLateralG:F3}," +
                   $"{corner.Duration:F3}," +
                   $"{corner.PerformanceScore:F1}," +
                   $"{corner.EntryAnalysis?.MaxBrakeInput:F3}," +
                   $"{corner.ExitAnalysis?.ThrottleSmoothness:F3}," +
                   $"{corner.EntryAnalysis?.SteeringSmoothness:F3}," +
                   $"{result.CoachingFeedback.Count}," +
                   $"{highPriorityFeedback}," +
                   $"\"{coachingMessage}\"";
        }

        /// <summary>
        /// Start logging
        /// </summary>
        public void StartLogging()
        {
            if (!_isLogging)
            {
                InitializeLogFiles();
            }
            _isLogging = true;
            Console.WriteLine($"Enhanced telemetry logging started - Session: {_sessionStartTime:yyyy-MM-dd HH:mm:ss}");
        }

        /// <summary>
        /// Stop logging and flush buffers
        /// </summary>
        public void StopLogging()
        {
            _isLogging = false;
            
            lock (_lockObject)
            {
                // Flush remaining data
                if (_telemetryBuffer.Count > 0)
                {
                    ProcessTelemetryBuffer(forceFlush: true);
                }
                
                if (_corneringBuffer.Count > 0)
                {
                    ProcessCorneringBuffer(forceFlush: true);
                }
            }
            
            Console.WriteLine($"Enhanced telemetry logging stopped - Total session time: {DateTime.Now - _sessionStartTime:hh\\:mm\\:ss}");
        }

        /// <summary>
        /// Update session information
        /// </summary>
        public void UpdateSessionInfo(string trackName, string vehicleName)
        {
            _currentTrack = trackName;
            _currentVehicle = vehicleName;
        }

        /// <summary>
        /// Get current session statistics
        /// </summary>
        public void LogSessionSummary()
        {
            var sessionDuration = DateTime.Now - _sessionStartTime;
            var summaryPath = Path.Combine(_logDirectory, $"session_summary_{_sessionStartTime:yyyy-MM-dd_HH-mm-ss}.txt");
            
            try
            {
                using (var writer = new StreamWriter(summaryPath, false, Encoding.UTF8))
                {
                    writer.WriteLine($"Le Mans Ultimate AI Copilot - Session Summary");
                    writer.WriteLine($"=========================================");
                    writer.WriteLine($"Session Start: {_sessionStartTime:yyyy-MM-dd HH:mm:ss}");
                    writer.WriteLine($"Session Duration: {sessionDuration:hh\\:mm\\:ss}");
                    writer.WriteLine($"Track: {_currentTrack}");
                    writer.WriteLine($"Vehicle: {_currentVehicle}");
                    writer.WriteLine($"Telemetry Log: {Path.GetFileName(_sessionLogPath)}");
                    writer.WriteLine($"Cornering Log: {Path.GetFileName(_corneringLogPath)}");
                    writer.WriteLine();
                    writer.WriteLine("Log files contain comprehensive telemetry data including:");
                    writer.WriteLine("- Real-time vehicle telemetry");
                    writer.WriteLine("- Cornering analysis and performance metrics");
                    writer.WriteLine("- AI coaching feedback and suggestions");
                    writer.WriteLine("- Tire temperature and pressure data");
                    writer.WriteLine("- Engine and fuel system monitoring");
                    writer.WriteLine();
                    writer.WriteLine("Use this data for performance analysis and driving improvement.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error writing session summary: {ex.Message}");
            }
        }
    }
}
