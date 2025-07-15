using System;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Threading;
using LeMansUltimateCoPilot.Models;
using LeMansUltimateCoPilot.Services;

namespace LeMansUltimateCoPilot
{
    // Official rF2 Telemetry Data Structure from rF2SharedMemoryMapPlugin
    // Source: https://github.com/TheIronWolfModding/rF2SharedMemoryMapPlugin/blob/master/Include/rF2State.h
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct rF2Vec3
    {
        public double x, y, z;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct rF2Wheel
    {
        public double mSuspensionDeflection;  // meters
        public double mRideHeight;           // meters
        public double mSuspForce;            // pushrod load in Newtons
        public double mBrakeTemp;            // Celsius
        public double mBrakePressure;        // currently 0.0-1.0, depending on driver input and brake balance
        public double mRotation;             // radians/sec
        public double mLateralPatchVel;      // lateral velocity at contact patch
        public double mLongitudinalPatchVel; // longitudinal velocity at contact patch
        public double mLateralGroundVel;     // lateral velocity at contact patch
        public double mLongitudinalGroundVel; // longitudinal velocity at contact patch
        public double mCamber;               // radians
        public double mLateralForce;         // Newtons
        public double mLongitudinalForce;    // Newtons
        public double mTireLoad;             // Newtons
        public double mGripFract;            // approximation of what fraction of the contact patch is sliding
        public double mPressure;             // kPa (tire pressure)
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public double[] mTemperature;        // Kelvin (subtract 273.15 to get Celsius), left/center/right
        public double mWear;                 // wear (0.0-1.0, fraction of maximum)
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] mTerrainName;          // the material prefixes from the TDF file
        public byte mSurfaceType;            // 0=dry, 1=wet, 2=grass, 3=dirt, 4=gravel, 5=rumblestrip, 6=special
        public byte mFlat;                   // whether tire is flat
        public byte mDetached;               // whether wheel is detached
        public byte mStaticUndeflectedRadius; // tire radius in centimeters
        public double mVerticalTireDeflection; // how much is tire deflected from its (speed-sensitive) radius
        public double mWheelYLocation;       // wheel's y location relative to vehicle y location
        public double mToe;                  // current toe angle w.r.t. the vehicle
        public double mTireCarcassTemperature; // rough average of temperature samples from carcass (Kelvin)
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public double[] mTireInnerLayerTemperature; // rough average of temperature samples from innermost layer of rubber (Kelvin)
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 24)]
        public byte[] mExpansion;            // for future use
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct rF2VehicleTelemetry
    {
        // Time
        public int mID;                      // slot ID (note that it can be re-used in multiplayer after someone leaves)
        public double mDeltaTime;            // time since last update (seconds)
        public double mElapsedTime;          // game session time
        public int mLapNumber;               // current lap number
        public double mLapStartET;           // time this lap was started
        
        // Vehicle and track names
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public byte[] mVehicleName;          // current vehicle name
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public byte[] mTrackName;            // current track name

        // Position and derivatives
        public rF2Vec3 mPos;                 // world position in meters
        public rF2Vec3 mLocalVel;            // velocity (meters/sec) in local vehicle coordinates
        public rF2Vec3 mLocalAccel;          // acceleration (meters/sec^2) in local vehicle coordinates

        // Orientation and derivatives
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public rF2Vec3[] mOri;               // rows of orientation matrix
        public rF2Vec3 mLocalRot;            // rotation (radians/sec) in local vehicle coordinates
        public rF2Vec3 mLocalRotAccel;       // rotational acceleration (radians/sec^2) in local vehicle coordinates

        // Vehicle status
        public int mGear;                    // -1=reverse, 0=neutral, 1+=forward gears
        public double mEngineRPM;            // engine RPM
        public double mEngineWaterTemp;      // Celsius
        public double mEngineOilTemp;        // Celsius
        public double mClutchRPM;            // clutch RPM

        // Driver input
        public double mUnfilteredThrottle;   // ranges 0.0-1.0
        public double mUnfilteredBrake;      // ranges 0.0-1.0
        public double mUnfilteredSteering;   // ranges -1.0-1.0 (left to right)
        public double mUnfilteredClutch;     // ranges 0.0-1.0

        // Filtered input
        public double mFilteredThrottle;     // ranges 0.0-1.0
        public double mFilteredBrake;        // ranges 0.0-1.0
        public double mFilteredSteering;     // ranges -1.0-1.0 (left to right)
        public double mFilteredClutch;       // ranges 0.0-1.0

        // Misc
        public double mSteeringShaftTorque;  // torque around steering shaft
        public double mFront3rdDeflection;   // deflection at front 3rd spring
        public double mRear3rdDeflection;    // deflection at rear 3rd spring

        // Aerodynamics
        public double mFrontWingHeight;      // front wing height
        public double mFrontRideHeight;      // front ride height
        public double mRearRideHeight;       // rear ride height
        public double mDrag;                 // drag
        public double mFrontDownforce;       // front downforce
        public double mRearDownforce;        // rear downforce

        // State/damage info
        public double mFuel;                 // amount of fuel (liters)
        public double mEngineMaxRPM;         // rev limit
        public byte mScheduledStops;         // number of scheduled pitstops
        public byte mOverheating;            // whether overheating icon is shown
        public byte mDetached;               // whether any parts (besides wheels) have been detached
        public byte mHeadlights;             // whether headlights are on
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public byte[] mDentSeverity;         // dent severity at 8 locations around the car
        public double mLastImpactET;         // time of last impact
        public double mLastImpactMagnitude;  // magnitude of last impact
        public rF2Vec3 mLastImpactPos;       // location of last impact

        // Expanded
        public double mEngineTorque;         // current engine torque
        public int mCurrentSector;           // the current sector (zero-based) with the pitlane stored in the sign bit
        public byte mSpeedLimiter;           // whether speed limiter is on
        public byte mMaxGears;               // maximum forward gears
        public byte mFrontTireCompoundIndex; // index within brand
        public byte mRearTireCompoundIndex;  // index within brand
        public double mFuelCapacity;         // capacity in liters
        public byte mFrontFlapActivated;     // whether front flap is activated
        public byte mRearFlapActivated;      // whether rear flap is activated
        public byte mRearFlapLegalStatus;    // 0=disallowed, 1=criteria detected but not allowed quite yet, 2=allowed
        public byte mIgnitionStarter;        // 0=off 1=ignition 2=ignition+starter

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 18)]
        public byte[] mFrontTireCompoundName; // name of front tire compound
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 18)]
        public byte[] mRearTireCompoundName;  // name of rear tire compound

        public byte mSpeedLimiterAvailable;  // whether speed limiter is available
        public byte mAntiStallActivated;     // whether (hard) anti-stall is activated
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public byte[] mUnused;               // unused

        public float mVisualSteeringWheelRange; // the *visual* steering wheel range
        public double mRearBrakeBias;        // fraction of brakes on rear
        public double mTurboBoostPressure;   // current turbo boost pressure if available
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public float[] mPhysicsToGraphicsOffset; // offset from static CG to graphical center
        public float mPhysicalSteeringWheelRange; // the *physical* steering wheel range

        public double mBatteryChargeFraction; // Battery charge as fraction [0.0-1.0]

        // electric boost motor
        public double mElectricBoostMotorTorque; // current torque of boost motor
        public double mElectricBoostMotorRPM;    // current rpm of boost motor
        public double mElectricBoostMotorTemperature; // current temperature of boost motor
        public double mElectricBoostWaterTemperature; // current water temperature of boost motor cooler
        public byte mElectricBoostMotorState; // 0=unavailable 1=inactive, 2=propulsion, 3=regeneration

        // Future use
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 111)]
        public byte[] mExpansion;            // for future use

        // Wheel info (front left, front right, rear left, rear right)
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public rF2Wheel[] mWheels;           // wheel info
    }

    // Buffer header structures
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct rF2MappedBufferVersionBlock
    {
        public uint mVersionUpdateBegin;     // Incremented right before buffer is written to
        public uint mVersionUpdateEnd;       // Incremented after buffer write is done
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct rF2MappedBufferHeader
    {
        public const int MAX_MAPPED_VEHICLES = 128;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct rF2MappedBufferHeaderWithSize
    {
        public int mBytesUpdatedHint;        // How many bytes of the structure were written during the last update
    }

    // Main telemetry structure (based on official rF2SharedMemoryMapPlugin)
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct rF2Telemetry
    {
        // Buffer versioning - IMPORTANT: These must be read first
        public uint mVersionUpdateBegin;      // Incremented right before buffer is written to
        public uint mVersionUpdateEnd;        // Incremented after buffer write is done
        public int mBytesUpdatedHint;         // How many bytes were updated
        
        public int mNumVehicles;              // current number of vehicles
        
        // Vehicle data array - we'll read the first vehicle directly
        // Note: The actual array is MAX_MAPPED_VEHICLES size, but we only need the first one
        public rF2VehicleTelemetry mVehicles; // vehicle telemetry data
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
            Console.WriteLine("  'v' - Validate telemetry offsets (debug mode)");
            Console.WriteLine("  'd' - Run Voice Driving Coach Demo");
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
                        else if (key.KeyChar == 'v' || key.KeyChar == 'V')
                        {
                            // Validate telemetry offsets
                            if (accessor != null)
                            {
                                ValidateTelemetryOffsets(accessor);
                            }
                        }
                        else if (key.KeyChar == 'd' || key.KeyChar == 'D')
                        {
                            // Run Voice Driving Coach Demo
                            RunVoiceCoachDemo();
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
                // Read version numbers first to check data stability
                var versionBegin = accessor.ReadUInt32(0);
                var versionEnd = accessor.ReadUInt32(4);
                
                // Check if data is valid (version numbers should be equal when data is stable)
                if (versionBegin != versionEnd)
                {
                    return null;
                }

                // Create telemetry structure and read header
                var telemetry = new rF2Telemetry();
                telemetry.mVersionUpdateBegin = versionBegin;
                telemetry.mVersionUpdateEnd = versionEnd;
                telemetry.mBytesUpdatedHint = accessor.ReadInt32(8);
                telemetry.mNumVehicles = accessor.ReadInt32(12);

                // If no vehicles, return null
                if (telemetry.mNumVehicles <= 0)
                {
                    return null;
                }

                // Create vehicle telemetry structure
                var vehicle = new rF2VehicleTelemetry();
                
                // Initialize arrays
                vehicle.mVehicleName = new byte[64];
                vehicle.mTrackName = new byte[64];
                vehicle.mOri = new rF2Vec3[3];
                vehicle.mDentSeverity = new byte[8];
                vehicle.mFrontTireCompoundName = new byte[18];
                vehicle.mRearTireCompoundName = new byte[18];
                vehicle.mUnused = new byte[2];
                vehicle.mPhysicsToGraphicsOffset = new float[3];
                vehicle.mExpansion = new byte[111];
                vehicle.mWheels = new rF2Wheel[4];

                // Initialize wheel data
                for (int i = 0; i < 4; i++)
                {
                    vehicle.mWheels[i].mTemperature = new double[3];
                    vehicle.mWheels[i].mTireInnerLayerTemperature = new double[3];
                    vehicle.mWheels[i].mTerrainName = new byte[16];
                    vehicle.mWheels[i].mExpansion = new byte[24];
                }

                // Use struct marshalling to read the entire first vehicle data
                // The vehicle data starts at offset 16 (after the header)
                int vehicleDataOffset = 16;
                
                // Read the vehicle data using marshalling
                var vehicleSize = Marshal.SizeOf<rF2VehicleTelemetry>();
                var vehicleData = new byte[vehicleSize];
                
                try
                {
                    accessor.ReadArray(vehicleDataOffset, vehicleData, 0, vehicleSize);
                    
                    // Pin the byte array and marshal it to the struct
                    var handle = GCHandle.Alloc(vehicleData, GCHandleType.Pinned);
                    try
                    {
                        vehicle = Marshal.PtrToStructure<rF2VehicleTelemetry>(handle.AddrOfPinnedObject());
                    }
                    finally
                    {
                        handle.Free();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Could not read full vehicle data using marshalling: {ex.Message}");
                    
                    // Fallback to manual reading of key fields
                    vehicle.mID = accessor.ReadInt32(vehicleDataOffset + 0);
                    vehicle.mDeltaTime = accessor.ReadDouble(vehicleDataOffset + 4);
                    vehicle.mElapsedTime = accessor.ReadDouble(vehicleDataOffset + 12);
                    vehicle.mLapNumber = accessor.ReadInt32(vehicleDataOffset + 20);
                    vehicle.mLapStartET = accessor.ReadDouble(vehicleDataOffset + 24);
                    
                    // Read vehicle and track names
                    accessor.ReadArray(vehicleDataOffset + 32, vehicle.mVehicleName, 0, 64);
                    accessor.ReadArray(vehicleDataOffset + 96, vehicle.mTrackName, 0, 64);
                    
                    // Read position data
                    var posOffset = vehicleDataOffset + 160;
                    vehicle.mPos.x = accessor.ReadDouble(posOffset);
                    vehicle.mPos.y = accessor.ReadDouble(posOffset + 8);
                    vehicle.mPos.z = accessor.ReadDouble(posOffset + 16);
                    
                    // Read velocity data
                    vehicle.mLocalVel.x = accessor.ReadDouble(posOffset + 24);
                    vehicle.mLocalVel.y = accessor.ReadDouble(posOffset + 32);
                    vehicle.mLocalVel.z = accessor.ReadDouble(posOffset + 40);
                    
                    // Read acceleration data
                    vehicle.mLocalAccel.x = accessor.ReadDouble(posOffset + 48);
                    vehicle.mLocalAccel.y = accessor.ReadDouble(posOffset + 56);
                    vehicle.mLocalAccel.z = accessor.ReadDouble(posOffset + 64);
                    
                    // Skip orientation matrix (9 doubles = 72 bytes) and local rotation data
                    var engineDataOffset = posOffset + 72 + 72 + 48;
                    
                    // Read engine data
                    vehicle.mGear = accessor.ReadInt32(engineDataOffset);
                    vehicle.mEngineRPM = accessor.ReadDouble(engineDataOffset + 4);
                    vehicle.mEngineWaterTemp = accessor.ReadDouble(engineDataOffset + 12);
                    vehicle.mEngineOilTemp = accessor.ReadDouble(engineDataOffset + 20);
                    vehicle.mClutchRPM = accessor.ReadDouble(engineDataOffset + 28);
                    
                    // Read input data
                    var inputDataOffset = engineDataOffset + 36;
                    vehicle.mUnfilteredThrottle = accessor.ReadDouble(inputDataOffset);
                    vehicle.mUnfilteredBrake = accessor.ReadDouble(inputDataOffset + 8);
                    vehicle.mUnfilteredSteering = accessor.ReadDouble(inputDataOffset + 16);
                    vehicle.mUnfilteredClutch = accessor.ReadDouble(inputDataOffset + 24);
                    
                    vehicle.mFilteredThrottle = accessor.ReadDouble(inputDataOffset + 32);
                    vehicle.mFilteredBrake = accessor.ReadDouble(inputDataOffset + 40);
                    vehicle.mFilteredSteering = accessor.ReadDouble(inputDataOffset + 48);
                    vehicle.mFilteredClutch = accessor.ReadDouble(inputDataOffset + 56);
                    
                    // Read additional fields
                    var miscDataOffset = inputDataOffset + 64;
                    vehicle.mSteeringShaftTorque = accessor.ReadDouble(miscDataOffset);
                    vehicle.mFront3rdDeflection = accessor.ReadDouble(miscDataOffset + 8);
                    vehicle.mRear3rdDeflection = accessor.ReadDouble(miscDataOffset + 16);
                    
                    // Read aerodynamics data
                    vehicle.mFrontWingHeight = accessor.ReadDouble(miscDataOffset + 24);
                    vehicle.mFrontRideHeight = accessor.ReadDouble(miscDataOffset + 32);
                    vehicle.mRearRideHeight = accessor.ReadDouble(miscDataOffset + 40);
                    vehicle.mDrag = accessor.ReadDouble(miscDataOffset + 48);
                    vehicle.mFrontDownforce = accessor.ReadDouble(miscDataOffset + 56);
                    vehicle.mRearDownforce = accessor.ReadDouble(miscDataOffset + 64);
                    
                    // Read fuel and engine data
                    vehicle.mFuel = accessor.ReadDouble(miscDataOffset + 72);
                    vehicle.mEngineMaxRPM = accessor.ReadDouble(miscDataOffset + 80);
                    
                    // Read some basic wheel data if possible
                    try
                    {
                        // The wheel data is at the end of the vehicle struct
                        // This is a rough estimate - wheel data starts very late in the struct
                        var wheelDataOffset = vehicleDataOffset + 1000; // Rough estimate
                        
                        for (int i = 0; i < 4; i++)
                        {
                            var wheelOffset = wheelDataOffset + i * Marshal.SizeOf<rF2Wheel>();
                            vehicle.mWheels[i].mRotation = accessor.ReadDouble(wheelOffset + 40); // Rotation is at offset 40 in rF2Wheel
                            vehicle.mWheels[i].mTireLoad = accessor.ReadDouble(wheelOffset + 104); // TireLoad is at offset 104
                            vehicle.mWheels[i].mPressure = accessor.ReadDouble(wheelOffset + 112); // Pressure is at offset 112
                            
                            // Read tire temperature (3 doubles at offset 120)
                            vehicle.mWheels[i].mTemperature[0] = accessor.ReadDouble(wheelOffset + 120);
                            vehicle.mWheels[i].mTemperature[1] = accessor.ReadDouble(wheelOffset + 128);
                            vehicle.mWheels[i].mTemperature[2] = accessor.ReadDouble(wheelOffset + 136);
                        }
                    }
                    catch
                    {
                        // If wheel data reading fails, initialize with defaults
                        for (int i = 0; i < 4; i++)
                        {
                            vehicle.mWheels[i].mRotation = 0;
                            vehicle.mWheels[i].mTireLoad = 0;
                            vehicle.mWheels[i].mPressure = 0;
                            vehicle.mWheels[i].mTemperature[0] = 0;
                            vehicle.mWheels[i].mTemperature[1] = 0;
                            vehicle.mWheels[i].mTemperature[2] = 0;
                        }
                    }
                }

                telemetry.mVehicles = vehicle;
                return telemetry;
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
            Console.WriteLine("CONTROLS: 'q'=Quit | 'r'=Reconnect | 'l'=Toggle Logging | 's'=Statistics | 'p'=Reference Laps | 'v'=Validate Offsets");
            
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

        static void RunVoiceCoachDemo()
        {
            Console.Clear();
            Console.WriteLine("🎯 Running Voice Driving Coach Demo...");
            Console.WriteLine("=====================================");
            Console.WriteLine();
            
            try
            {
                var demo = new VoiceCoachDemo();
                // Run the demo synchronously
                demo.RunDemoAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Demo failed: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
            
            Console.WriteLine();
            Console.WriteLine("Demo completed. Press any key to return to main menu...");
            Console.ReadKey();
            Console.Clear();
            
            // Redisplay the main menu
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
            Console.WriteLine("  'd' - Run Voice Driving Coach Demo");
            Console.WriteLine();
        }

        /// <summary>
        /// Helper method to validate telemetry data and test different offsets
        /// Use this method to verify the analyzer findings
        /// </summary>
        static void ValidateTelemetryOffsets(MemoryMappedViewAccessor accessor)
        {
            Console.WriteLine("🔍 VALIDATING TELEMETRY OFFSETS");
            Console.WriteLine("===============================");
            
            // Test different offset candidates for key fields
            var testOffsets = new Dictionary<string, int[]>
            {
                {"Engine RPM", new[] { 284, 300, 316, 332, 348, 364, 380 }},
                {"Gear", new[] { 280, 296, 312, 328, 344, 360, 376 }},
                {"Throttle", new[] { 308, 324, 340, 356, 372, 388, 404 }},
                {"Brake", new[] { 312, 328, 344, 360, 376, 392, 408 }},
                {"Steering", new[] { 316, 332, 348, 364, 380, 396, 412 }}
            };
            
            Console.WriteLine("Testing offset candidates for key telemetry fields:");
            Console.WriteLine();
            
            foreach (var field in testOffsets)
            {
                Console.WriteLine($"{field.Key}:");
                foreach (var offset in field.Value)
                {
                    try
                    {
                        if (field.Key == "Gear")
                        {
                            var value = accessor.ReadInt32(offset);
                            Console.WriteLine($"  Offset {offset:X3}h ({offset,4}): {value,8} (as int)");
                        }
                        else
                        {
                            var value = accessor.ReadSingle(offset);
                            Console.WriteLine($"  Offset {offset:X3}h ({offset,4}): {value,8:F2} (as float)");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"  Offset {offset:X3}h ({offset,4}): ERROR - {ex.Message}");
                    }
                }
                Console.WriteLine();
            }
            
            Console.WriteLine("💡 Compare these values with what you see in-game!");
            Console.WriteLine("💡 The correct offsets should show realistic values:");
            Console.WriteLine("   - RPM: 0-8000 for most cars");
            Console.WriteLine("   - Gear: -1 (reverse), 0 (neutral), 1-8 (forward)");
            Console.WriteLine("   - Throttle/Brake: 0.0-1.0");
            Console.WriteLine("   - Steering: -1.0 to 1.0");
            Console.WriteLine();
        }

        // ...existing code...
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
