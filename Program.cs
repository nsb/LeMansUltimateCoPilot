using System;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Threading;
using LMUSharedMemoryTest.Models;
using LMUSharedMemoryTest.Services;

namespace LMUSharedMemoryTest
{
    // rFactor2 Telemetry Data Structure (simplified version)
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct rF2VehicleTelemetry
    {
        public int mID;                      // slot ID (note that it can be re-used in multiplayer after someone leaves)
        public float mDeltaTime;             // time since last update (seconds)
        public float mElapsedTime;           // game session time
        public int mLapNumber;               // current lap number
        public float mLapStartET;            // time this lap was started
        public float mPos_x, mPos_y, mPos_z; // world position in meters
        public float mLocalVel_x, mLocalVel_y, mLocalVel_z; // velocity (m/s) in local vehicle coordinates
        public float mLocalAccel_x, mLocalAccel_y, mLocalAccel_z; // acceleration (m/s^2) in local vehicle coordinates
        
        // Engine data
        public float mUnfilteredThrottle;    // ranges  0.0-1.0
        public float mUnfilteredBrake;       // ranges  0.0-1.0
        public float mUnfilteredSteering;    // ranges -1.0-1.0 (left to right)
        public float mUnfilteredClutch;      // ranges  0.0-1.0
        
        public float mFilteredThrottle;      // ranges  0.0-1.0
        public float mFilteredBrake;         // ranges  0.0-1.0
        public float mFilteredSteering;      // ranges -1.0-1.0 (left to right)
        public float mFilteredClutch;        // ranges  0.0-1.0
        
        public float mSteeringShaftTorque;   // torque around steering shaft (used to be mSteeringArmForce)
        public float mFront3rdDeflection;    // deflection at front 3rd spring
        public float mRear3rdDeflection;     // deflection at rear 3rd spring
        
        public float mFrontWingHeight;       // front wing height
        public float mFrontRideHeight;       // front ride height
        public float mRearRideHeight;        // rear ride height
        public float mDrag;                  // drag
        public float mFrontDownforce;        // front downforce
        public float mRearDownforce;         // rear downforce
        
        public float mFuel;                  // amount of fuel (liters)
        public float mEngineMaxRPM;          // rev limit
        public float mScheduledStops;        // number of scheduled pitstops
        public float mOverheating;           // whether overheating icon is shown
        public float mDetached;              // whether any parts (besides wheels) have been detached
        public float mHeadlights;            // whether headlights are on
        public float mPitLimiter;            // whether pit limiter is on
        public float mPitSpeedLimit;         // pit speed limit
        
        public float mEngineRPM;             // engine RPM
        public float mEngineWaterTemp;       // Celsius
        public float mEngineOilTemp;         // Celsius
        public float mClutchRPM;             // clutch RPM
        
        // Transmission
        public float mUnfilteredRPM;         // engine RPM
        public float mFilteredRPM;           // engine RPM
        public int mGear;                    // -1=reverse, 0=neutral, 1+=forward gears
        public float mBoostPressure;         // boost pressure if available
        public float mTurboSpeedPercent;     // turbo speed in percent if available
        
        // Tires - FL, FR, RL, RR
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] mWheelRotation;       // radians/sec
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] mTireLoad;            // kilograms
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] mTireGripFract;       // an approximation of what fraction of the contact patch is sliding
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] mTirePressure;        // Pascals
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] mTireTemp;            // Celsius
        
        // Suspension - FL, FR, RL, RR
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] mWheelYLocation;      // wheel Y location relative to vehicle Y location
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] mSuspensionDeflection; // deflection at wheel
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] mSuspensionVelocity;  // velocity of wheel deflection
        
        public rF2VehicleTelemetry()
        {
            mWheelRotation = new float[4];
            mTireLoad = new float[4];
            mTireGripFract = new float[4];
            mTirePressure = new float[4];
            mTireTemp = new float[4];
            mWheelYLocation = new float[4];
            mSuspensionDeflection = new float[4];
            mSuspensionVelocity = new float[4];
        }
    }

    // Main telemetry structure
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct rF2Telemetry
    {
        public int mVersionUpdateBegin;      // incremented right before updating the rest of the data
        public int mVersionUpdateEnd;        // incremented after updating the rest of the data
        public int mBytesUpdatedHint;        // How many bytes of the structure were written during the last update
        public float mDeltaTime;             // time since last update (seconds)
        public float mElapsedTime;           // game session time
        public int mLapNumber;               // current lap number
        public float mLapStartET;            // time this lap was started
        
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)]
        public byte[] mVehicleName;          // driver name
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)]
        public byte[] mTrackName;            // track name
        
        public rF2VehicleTelemetry mVehicles; // vehicle telemetry data
        
        public rF2Telemetry()
        {
            mVehicleName = new byte[128];
            mTrackName = new byte[128];
            mVehicles = new rF2VehicleTelemetry();
        }
    }

    class Program
    {
        private static bool _running = true;
        private static TelemetryLogger? _telemetryLogger;
        private static LapDetector? _lapDetector;
        private static ReferenceLapManager? _referenceLapManager;
        private static readonly string[] SharedMemoryNames = {
            "$rFactor2SMMP_Telemetry$",
            "$rFactor2SMMP_Scoring$",
            "$rFactor2SMMP_Rules$"
        };

        static void Main(string[] args)
        {
            Console.WriteLine("rFactor2 / Le Mans Ultimate Shared Memory Reader & AI Driving Coach");
            Console.WriteLine("====================================================================");
            Console.WriteLine("Enhanced telemetry logging for AI-assisted driving analysis");
            Console.WriteLine("Make sure Le Mans Ultimate is running with an active session!");
            Console.WriteLine();
            Console.WriteLine("Controls:");
            Console.WriteLine("  'q' - Quit application");
            Console.WriteLine("  'r' - Reconnect to shared memory");
            Console.WriteLine("  'l' - Start/Stop telemetry logging");
            Console.WriteLine("  's' - Show session statistics");
            Console.WriteLine("  'p' - Show reference lap statistics");
            Console.WriteLine("  'b' - Save current best lap as reference");
            Console.WriteLine();

            // Initialize telemetry logger
            _telemetryLogger = new TelemetryLogger();
            _telemetryLogger.LogMessage += (sender, message) => Console.WriteLine($"[LOG] {message}");
            _telemetryLogger.SessionStarted += (sender, file) => Console.WriteLine($"[SESSION] Started logging to: {file}");
            _telemetryLogger.SessionStopped += (sender, file) => Console.WriteLine($"[SESSION] Stopped logging. File: {file}");

            // Initialize reference lap manager
            _referenceLapManager = new ReferenceLapManager();
            _referenceLapManager.ReferenceLapSaved += (sender, args) => 
                Console.WriteLine($"[REFERENCE] Saved lap: {args.ReferenceLap.TrackName} - {args.ReferenceLap.LapTime:F3}s");
            _referenceLapManager.ReferenceLapsLoaded += (sender, args) => 
                Console.WriteLine($"[REFERENCE] Loaded {args.LoadedCount} reference laps from {args.TotalTracks} tracks");

            // Initialize lap detector
            _lapDetector = new LapDetector();
            _lapDetector.LapStarted += (sender, args) => 
                Console.WriteLine($"[LAP] Started Lap #{args.LapNumber} - {args.TrackName}");
            _lapDetector.LapCompleted += (sender, args) => 
            {
                Console.WriteLine($"[LAP] Completed Lap #{args.LapNumber} - {args.LapTime:F3}s ({(args.IsValid ? "Valid" : "Invalid")})");
                
                // Auto-save valid laps as potential reference laps
                if (args.IsValid && args.LapTime > 0)
                {
                    var referenceLap = new ReferenceLap(args.TelemetryData, args.LapNumber);
                    var bestExisting = _referenceLapManager?.GetBestReferenceLap(args.TrackName, args.VehicleName);
                    
                    // Save if it's a new best lap or we have no reference for this track/vehicle
                    if (bestExisting == null || referenceLap.LapTime < bestExisting.LapTime - 0.1)
                    {
                        _referenceLapManager?.SaveReferenceLap(referenceLap);
                        Console.WriteLine($"[REFERENCE] ⭐ New best lap saved! Previous best: {bestExisting?.LapTime:F3}s");
                    }
                }
            };

            // Load existing reference laps
            var loadedLaps = _referenceLapManager.LoadReferenceLaps();
            if (loadedLaps > 0)
            {
                Console.WriteLine($"[REFERENCE] Loaded {loadedLaps} reference laps");
            }
            _telemetryLogger = new TelemetryLogger();
            _telemetryLogger.LogMessage += (sender, message) => Console.WriteLine($"[LOG] {message}");
            _telemetryLogger.SessionStarted += (sender, file) => Console.WriteLine($"[SESSION] Started logging to: {file}");
            _telemetryLogger.SessionStopped += (sender, file) => Console.WriteLine($"[SESSION] Stopped logging. File: {file}");

            // Set up console key handler
            Console.CancelKeyPress += (sender, e) => {
                e.Cancel = true;
                _running = false;
            };

            // Start reading telemetry
            ReadTelemetryLoop();

            // Cleanup
            _telemetryLogger?.Dispose();
        }

        static void ReadTelemetryLoop()
        {
            MemoryMappedFile? mmf = null;
            MemoryMappedViewAccessor? accessor = null;

            while (_running)
            {
                try
                {
                    // Try to connect to shared memory if not already connected
                    if (mmf == null)
                    {
                        mmf = ConnectToSharedMemory();
                        if (mmf != null)
                        {
                            accessor = mmf.CreateViewAccessor();
                            Console.WriteLine("✅ Connected to rFactor2 shared memory!");
                        }
                        else
                        {
                            Console.WriteLine("❌ Waiting for Le Mans Ultimate... (make sure you're in an active session)");
                            Thread.Sleep(2000);
                            continue;
                        }
                    }

                    if (accessor != null)
                    {
                        // Read telemetry data
                        var rawTelemetry = ReadTelemetryData(accessor);
                        if (rawTelemetry.HasValue)
                        {
                            // Convert to enhanced telemetry data
                            var enhancedData = EnhancedTelemetryData.FromRaw(rawTelemetry.Value, DateTime.Now);
                            
                            // Log data if logging is active
                            if (_telemetryLogger?.IsLogging == true)
                            {
                                _telemetryLogger.LogTelemetryData(enhancedData);
                            }

                            // Process lap detection
                            _lapDetector?.ProcessTelemetryData(enhancedData);

                            // Display telemetry data
                            DisplayTelemetryData(enhancedData);
                        }
                    }

                    // Check for user input
                    if (Console.KeyAvailable)
                    {
                        var key = Console.ReadKey(true);
                        if (key.KeyChar == 'q' || key.KeyChar == 'Q')
                        {
                            _running = false;
                        }
                        else if (key.KeyChar == 'r' || key.KeyChar == 'R')
                        {
                            // Reconnect
                            accessor?.Dispose();
                            mmf?.Dispose();
                            mmf = null;
                            accessor = null;
                            Console.Clear();
                            Console.WriteLine("Reconnecting...");
                        }
                        else if (key.KeyChar == 'l' || key.KeyChar == 'L')
                        {
                            // Toggle logging
                            ToggleLogging();
                        }
                        else if (key.KeyChar == 's' || key.KeyChar == 'S')
                        {
                            // Show statistics
                            ShowSessionStatistics();
                        }
                        else if (key.KeyChar == 'p' || key.KeyChar == 'P')
                        {
                            // Show reference lap statistics
                            ShowReferenceLapStatistics();
                        }
                        else if (key.KeyChar == 'b' || key.KeyChar == 'B')
                        {
                            // Save current best lap as reference (if available)
                            SaveBestLapAsReference();
                        }
                    }

                    Thread.Sleep(100); // Update 10 times per second
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error: {ex.Message}");
                    // Try to reconnect
                    accessor?.Dispose();
                    mmf?.Dispose();
                    mmf = null;
                    accessor = null;
                    Thread.Sleep(2000);
                }
            }

            // Cleanup
            accessor?.Dispose();
            mmf?.Dispose();
            Console.WriteLine("Disconnected. Press any key to exit...");
            Console.ReadKey();
        }

        static MemoryMappedFile? ConnectToSharedMemory()
        {
            foreach (var memoryName in SharedMemoryNames)
            {
                try
                {
                    var mmf = MemoryMappedFile.OpenExisting(memoryName);
                    Console.WriteLine($"Found shared memory: {memoryName}");
                    return mmf;
                }
                catch (FileNotFoundException)
                {
                    // This shared memory doesn't exist, try the next one
                    continue;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error accessing {memoryName}: {ex.Message}");
                    continue;
                }
            }
            return null;
        }

        static rF2Telemetry? ReadTelemetryData(MemoryMappedViewAccessor accessor)
        {
            try
            {
                // Read the structure from shared memory
                var telemetry = accessor.ReadStruct<rF2Telemetry>(0);
                
                // Check if data is valid (version numbers should be equal when data is stable)
                if (telemetry.mVersionUpdateBegin == telemetry.mVersionUpdateEnd)
                {
                    return telemetry;
                }
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading telemetry: {ex.Message}");
                return null;
            }
        }

        static void DisplayTelemetryData(EnhancedTelemetryData telemetry)
        {
            // Clear console and display current data
            Console.SetCursorPosition(0, 10);
            
            // Enhanced header with logging status
            var loggingStatus = _telemetryLogger?.IsLogging == true ? "[LOGGING]" : "[NOT LOGGING]";
            var recordCount = _telemetryLogger?.RecordCount ?? 0;
            Console.WriteLine($"TELEMETRY DATA {loggingStatus} Records: {recordCount}");
            Console.WriteLine(new string('=', 60));

            // Basic session info
            Console.WriteLine($"Session Time: {telemetry.SessionTime:F1}s");
            Console.WriteLine($"Lap Number:   {telemetry.LapNumber}");
            Console.WriteLine($"Lap Time:     {telemetry.LapTime:F3}s");
            Console.WriteLine($"Delta Time:   {telemetry.DeltaTime:F3}s");
            Console.WriteLine();

            // Vehicle info
            Console.WriteLine($"Vehicle: {telemetry.VehicleName}");
            Console.WriteLine($"Track:   {telemetry.TrackName}");
            Console.WriteLine();

            // Enhanced engine data
            Console.WriteLine("ENGINE & DYNAMICS:");
            Console.WriteLine($"  RPM:         {telemetry.EngineRPM:F0} / {telemetry.MaxRPM:F0}");
            Console.WriteLine($"  Gear:        {telemetry.Gear}");
            Console.WriteLine($"  Speed:       {telemetry.Speed:F1} km/h ({telemetry.SpeedMPS:F1} m/s)");
            Console.WriteLine($"  Throttle:    {telemetry.ThrottleInput * 100:F1}% (Raw: {telemetry.UnfilteredThrottle * 100:F1}%)");
            Console.WriteLine($"  Brake:       {telemetry.BrakeInput * 100:F1}% (Raw: {telemetry.UnfilteredBrake * 100:F1}%)");
            Console.WriteLine($"  Steering:    {telemetry.SteeringInput * 100:F1}% (Raw: {telemetry.UnfilteredSteering * 100:F1}%)");
            Console.WriteLine($"  Fuel:        {telemetry.FuelLevel:F1} L");
            Console.WriteLine();

            // G-Forces
            Console.WriteLine("G-FORCES:");
            Console.WriteLine($"  Longitudinal: {telemetry.LongitudinalG:F2}g");
            Console.WriteLine($"  Lateral:      {telemetry.LateralG:F2}g");
            Console.WriteLine($"  Vertical:     {telemetry.VerticalG:F2}g");
            Console.WriteLine();

            // Temperatures
            Console.WriteLine("TEMPERATURES:");
            Console.WriteLine($"  Water:       {telemetry.WaterTemperature:F1}°C");
            Console.WriteLine($"  Oil:         {telemetry.OilTemperature:F1}°C");
            Console.WriteLine($"  Tire FL/FR:  {telemetry.TireTemperatureFL:F1}°C / {telemetry.TireTemperatureFR:F1}°C");
            Console.WriteLine($"  Tire RL/RR:  {telemetry.TireTemperatureRL:F1}°C / {telemetry.TireTemperatureRR:F1}°C");
            Console.WriteLine();

            // Enhanced tire data
            Console.WriteLine("TIRE PRESSURE (kPa):");
            Console.WriteLine($"  FL/FR:  {telemetry.TirePressureFL / 1000:F1} / {telemetry.TirePressureFR / 1000:F1}");
            Console.WriteLine($"  RL/RR:  {telemetry.TirePressureRL / 1000:F1} / {telemetry.TirePressureRR / 1000:F1}");
            Console.WriteLine();

            // Lap progress and reference lap info
            if (_lapDetector != null)
            {
                var lapProgress = _lapDetector.GetCurrentLapProgress();
                Console.WriteLine("LAP PROGRESS:");
                Console.WriteLine($"  Current Lap: #{lapProgress.LapNumber}");
                Console.WriteLine($"  In Progress: {(lapProgress.IsInProgress ? "Yes" : "No")}");
                Console.WriteLine($"  Progress:    {lapProgress.LastLapProgress * 100:F1}%");
                Console.WriteLine($"  Data Points: {lapProgress.DataPoints}");
                
                if (lapProgress.IsInProgress)
                {
                    Console.WriteLine($"  Elapsed:     {lapProgress.ElapsedTime:F1}s");
                }
                Console.WriteLine();
            }

            // Reference lap info
            if (_referenceLapManager != null)
            {
                var bestLap = _referenceLapManager.GetBestReferenceLap(telemetry.TrackName, telemetry.VehicleName);
                if (bestLap != null)
                {
                    Console.WriteLine("REFERENCE LAP:");
                    Console.WriteLine($"  Best Time:   {bestLap.LapTime:F3}s");
                    Console.WriteLine($"  Recorded:    {bestLap.RecordedAt:MM/dd HH:mm}");
                    Console.WriteLine($"  Max Speed:   {bestLap.Performance.MaxSpeed:F1} km/h");
                    Console.WriteLine();
                }
            }

            // Controls reminder
            Console.WriteLine("CONTROLS: 'q'=Quit | 'r'=Reconnect | 'l'=Toggle Logging | 's'=Statistics | 'p'=Reference Laps");
            
            // Clear any remaining lines
            for (int i = 0; i < 5; i++)
            {
                Console.WriteLine(new string(' ', Console.WindowWidth - 1));
            }
        }

        static void ToggleLogging()
        {
            if (_telemetryLogger == null)
                return;

            if (_telemetryLogger.IsLogging)
            {
                _telemetryLogger.StopLoggingSession();
                Console.WriteLine("[ACTION] Stopped telemetry logging");
            }
            else
            {
                var sessionName = $"{_telemetryLogger.CurrentTrack}_{_telemetryLogger.CurrentVehicle}".Replace(" ", "_");
                if (_telemetryLogger.StartLoggingSession(sessionName))
                {
                    Console.WriteLine("[ACTION] Started telemetry logging");
                }
                else
                {
                    Console.WriteLine("[ERROR] Failed to start telemetry logging");
                }
            }
        }

        static void ShowSessionStatistics()
        {
            if (_telemetryLogger == null)
                return;

            Console.Clear();
            Console.WriteLine("SESSION STATISTICS");
            Console.WriteLine("==================");
            
            if (_telemetryLogger.IsLogging)
            {
                Console.WriteLine($"Current Session:");
                Console.WriteLine($"  Status:     ACTIVE");
                Console.WriteLine($"  Started:    {_telemetryLogger.SessionStartTime:yyyy-MM-dd HH:mm:ss}");
                Console.WriteLine($"  Duration:   {(DateTime.Now - _telemetryLogger.SessionStartTime).TotalMinutes:F1} minutes");
                Console.WriteLine($"  Records:    {_telemetryLogger.RecordCount:N0}");
                Console.WriteLine($"  Track:      {_telemetryLogger.CurrentTrack}");
                Console.WriteLine($"  Vehicle:    {_telemetryLogger.CurrentVehicle}");
            }
            else
            {
                Console.WriteLine("No active logging session");
            }

            Console.WriteLine();
            
            var logFiles = _telemetryLogger.GetAvailableLogFiles();
            Console.WriteLine($"Available Log Files: {logFiles.Count}");
            Console.WriteLine("Recent Sessions:");
            
            for (int i = 0; i < Math.Min(5, logFiles.Count); i++)
            {
                var file = logFiles[i];
                var fileInfo = new System.IO.FileInfo(file);
                Console.WriteLine($"  {i + 1}. {fileInfo.Name} ({fileInfo.Length / 1024:N0} KB) - {fileInfo.CreationTime:MM/dd HH:mm}");
            }

            Console.WriteLine();
            Console.WriteLine("Press any key to return to telemetry view...");
            Console.ReadKey();
            Console.Clear();
        }

        static void ShowReferenceLapStatistics()
        {
            Console.Clear();
            Console.WriteLine("=== Reference Lap Statistics ===");
            Console.WriteLine();

            if (_referenceLapManager == null)
            {
                Console.WriteLine("Reference lap manager not initialized.");
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
                return;
            }

            var stats = _referenceLapManager.GetStatistics();
            
            Console.WriteLine($"Total Reference Laps: {stats.TotalReferenceLaps}");
            Console.WriteLine($"Tracks with Reference Laps: {stats.TracksWithReferenceLaps}");
            Console.WriteLine();

            if (stats.TrackStatistics.Count > 0)
            {
                Console.WriteLine("Track Statistics:");
                foreach (var trackStat in stats.TrackStatistics.Values)
                {
                    Console.WriteLine($"  📍 {trackStat.TrackName}:");
                    Console.WriteLine($"     Total Laps: {trackStat.TotalLaps}");
                    Console.WriteLine($"     Best Time: {trackStat.BestLapTime:F3}s");
                    Console.WriteLine($"     Avg Time: {trackStat.AverageLapTime:F3}s");
                    Console.WriteLine($"     Vehicles: {trackStat.UniqueVehicles}");
                    Console.WriteLine();
                }
            }

            if (_lapDetector != null)
            {
                var lapProgress = _lapDetector.GetCurrentLapProgress();
                Console.WriteLine("Current Lap Status:");
                Console.WriteLine($"  Lap #{lapProgress.LapNumber}");
                Console.WriteLine($"  In Progress: {lapProgress.IsInProgress}");
                Console.WriteLine($"  Data Points: {lapProgress.DataPoints}");
                Console.WriteLine($"  Elapsed Time: {lapProgress.ElapsedTime:F1}s");
                Console.WriteLine($"  Progress: {lapProgress.LastLapProgress * 100:F1}%");
            }

            Console.WriteLine();
            Console.WriteLine("Press any key to return to telemetry view...");
            Console.ReadKey();
            Console.Clear();
        }

        static void SaveBestLapAsReference()
        {
            Console.WriteLine("[REFERENCE] Manual save requested...");
            
            if (_referenceLapManager == null)
            {
                Console.WriteLine("[REFERENCE] Reference lap manager not available.");
                return;
            }

            // For now, just show the current reference lap status
            // In a real implementation, you might want to save the current session's best lap
            var stats = _referenceLapManager.GetStatistics();
            Console.WriteLine($"[REFERENCE] Current status: {stats.TotalReferenceLaps} laps from {stats.TracksWithReferenceLaps} tracks");
            
            if (_lapDetector != null)
            {
                var lapProgress = _lapDetector.GetCurrentLapProgress();
                if (lapProgress.IsInProgress)
                {
                    Console.WriteLine($"[REFERENCE] Current lap in progress: Lap #{lapProgress.LapNumber} ({lapProgress.ElapsedTime:F1}s)");
                }
                else
                {
                    Console.WriteLine("[REFERENCE] No lap currently in progress. Complete a lap to save as reference.");
                }
            }
        }
    }

    // Extension method for reading structs from MemoryMappedViewAccessor
    public static class MemoryMappedViewAccessorExtensions
    {
        public static T ReadStruct<T>(this MemoryMappedViewAccessor accessor, long offset) where T : struct
        {
            var size = Marshal.SizeOf<T>();
            var buffer = new byte[size];
            accessor.ReadArray(offset, buffer, 0, size);
            
            var handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            try
            {
                return Marshal.PtrToStructure<T>(handle.AddrOfPinnedObject());
            }
            finally
            {
                handle.Free();
            }
        }
    }
}
