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
            Console.WriteLine("  't' - Test player detection logic");
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
                            
                            // Clear cached player vehicle ID to force re-detection
                            _cachedPlayerVehicleID = -1;
                            _lastPlayerLookup = DateTime.MinValue;
                            
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
                        else if (key.KeyChar == 't' || key.KeyChar == 'T')
                        {
                            // Test player detection
                            TestPlayerDetection();
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

        // Official rF2 Scoring structures based on rF2SharedMemoryMapPlugin
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct rF2ScoringInfo
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
            public byte[] mTrackName;            // current track name
            public int mSession;                 // current session (0=testday 1-4=practice 5-8=qual 9=warmup 10-13=race)
            public double mCurrentET;            // current time
            public double mEndET;                // ending time
            public int mMaxLaps;                 // maximum laps
            public double mLapDist;              // distance around track
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public byte[] pointer1;              // pointer placeholder
            public int mNumVehicles;             // current number of vehicles
            public byte mGamePhase;              // game phase
            public sbyte mYellowFlagState;       // yellow flag state
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public sbyte[] mSectorFlag;          // sector flags
            public byte mStartLight;             // start light frame
            public byte mNumRedLights;           // number of red lights
            public byte mInRealtime;             // in realtime
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public byte[] mPlayerName;           // player name
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
            public byte[] mPlrFileName;          // player file name
            public double mDarkCloud;            // cloud darkness
            public double mRaining;              // rain severity
            public double mAmbientTemp;          // ambient temperature
            public double mTrackTemp;            // track temperature
            public rF2Vec3 mWind;                // wind speed
            public double mMinPathWetness;       // minimum path wetness
            public double mMaxPathWetness;       // maximum path wetness
            public byte mGameMode;               // game mode
            public byte mIsPasswordProtected;    // password protected
            public ushort mServerPort;           // server port
            public uint mServerPublicIP;         // server public IP
            public int mMaxPlayers;              // maximum players
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public byte[] mServerName;           // server name
            public float mStartET;               // start time
            public double mAvgPathWetness;       // average path wetness
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 200)]
            public byte[] mExpansion;            // future expansion
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public byte[] pointer2;              // pointer placeholder
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct rF2VehicleScoring
        {
            public int mID;                      // slot ID
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public byte[] mDriverName;           // driver name
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
            public byte[] mVehicleName;          // vehicle name
            public short mTotalLaps;             // laps completed
            public sbyte mSector;                // current sector
            public sbyte mFinishStatus;          // finish status
            public double mLapDist;              // current lap distance
            public double mPathLateral;          // lateral position
            public double mTrackEdge;            // track edge
            public double mBestSector1;          // best sector 1
            public double mBestSector2;          // best sector 2
            public double mBestLapTime;          // best lap time
            public double mLastSector1;          // last sector 1
            public double mLastSector2;          // last sector 2
            public double mLastLapTime;          // last lap time
            public double mCurSector1;           // current sector 1
            public double mCurSector2;           // current sector 2
            public short mNumPitstops;           // number of pitstops
            public short mNumPenalties;          // number of penalties
            public byte mIsPlayer;               // is this the player's vehicle (0=no, 1=yes)
            public sbyte mControl;               // who's in control (-1=nobody, 0=local player, 1=local AI, 2=remote, 3=replay)
            public byte mInPits;                 // in pits flag
            public byte mPlace;                  // 1-based position
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public byte[] mVehicleClass;         // vehicle class
            public double mTimeBehindNext;       // time behind next vehicle
            public int mLapsBehindNext;          // laps behind next vehicle
            public double mTimeBehindLeader;     // time behind leader
            public int mLapsBehindLeader;        // laps behind leader
            public double mLapStartET;           // lap start time
            public rF2Vec3 mPos;                 // world position
            public rF2Vec3 mLocalVel;            // local velocity
            public rF2Vec3 mLocalAccel;          // local acceleration
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public rF2Vec3[] mOri;               // orientation matrix
            public rF2Vec3 mLocalRot;            // local rotation
            public rF2Vec3 mLocalRotAccel;       // local rotational acceleration
            public byte mHeadlights;             // headlights status
            public byte mPitState;               // pit state
            public byte mServerScored;           // server scored
            public byte mIndividualPhase;        // individual phase
            public int mQualification;           // qualification position
            public double mTimeIntoLap;          // time into lap
            public double mEstimatedLapTime;     // estimated lap time
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 24)]
            public byte[] mPitGroup;             // pit group
            public byte mFlag;                   // primary flag
            public byte mUnderYellow;            // under yellow flag
            public byte mCountLapFlag;           // count lap flag
            public byte mInGarageStall;          // in garage stall
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public byte[] mUpgradePack;          // upgrade pack
            public float mPitLapDist;            // pit lap distance
            public float mBestLapSector1;        // best lap sector 1
            public float mBestLapSector2;        // best lap sector 2
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 48)]
            public byte[] mExpansion;            // future expansion
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct rF2Scoring
        {
            public int mBytesUpdatedHint;        // bytes updated hint
            public rF2ScoringInfo mScoringInfo;  // scoring info
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)]
            public rF2VehicleScoring[] mVehicles; // vehicle scoring array
        }

        // Cache player vehicle ID to avoid repeated lookups
        private static int _cachedPlayerVehicleID = -1;
        private static DateTime _lastPlayerLookup = DateTime.MinValue;
        private static readonly TimeSpan PlayerLookupCacheTime = TimeSpan.FromSeconds(2);

        static int FindPlayerVehicleID()
        {
            // Use cached value if recent
            if (_cachedPlayerVehicleID != -1 && 
                DateTime.Now - _lastPlayerLookup < PlayerLookupCacheTime)
            {
                return _cachedPlayerVehicleID;
            }

            try
            {
                // Try to connect to scoring shared memory
                using var scoringMmf = MemoryMappedFile.OpenExisting("$rFactor2SMMP_Scoring$");
                using var scoringAccessor = scoringMmf.CreateViewAccessor();
                
                // Read version numbers to check data stability with retry logic
                var versionBegin = scoringAccessor.ReadUInt32(0);
                var versionEnd = scoringAccessor.ReadUInt32(4);
                
                if (versionBegin != versionEnd)
                {
                    // Retry once after a short delay
                    Thread.Sleep(10);
                    versionBegin = scoringAccessor.ReadUInt32(0);
                    versionEnd = scoringAccessor.ReadUInt32(4);
                    
                    if (versionBegin != versionEnd)
                    {
                        Console.WriteLine("⚠️  Scoring data version mismatch after retry");
                        return _cachedPlayerVehicleID != -1 ? _cachedPlayerVehicleID : -1;
                    }
                }
                
                // Read the scoring structure using proper marshalling
                var scoringSize = Marshal.SizeOf<rF2Scoring>();
                var scoringData = new byte[scoringSize];
                
                try
                {
                    scoringAccessor.ReadArray(8, scoringData, 0, scoringSize); // Skip version info (8 bytes)
                    
                    // Marshal the data to the structure
                    var handle = GCHandle.Alloc(scoringData, GCHandleType.Pinned);
                    rF2Scoring scoring;
                    try
                    {
                        scoring = Marshal.PtrToStructure<rF2Scoring>(handle.AddrOfPinnedObject());
                    }
                    finally
                    {
                        handle.Free();
                    }
                    
                    var numVehicles = scoring.mScoringInfo.mNumVehicles;
                    Console.WriteLine($"🔍 Scoring data - NumVehicles: {numVehicles}");
                    
                    if (numVehicles <= 0)
                    {
                        Console.WriteLine("⚠️  No vehicles in scoring data");
                        return _cachedPlayerVehicleID != -1 ? _cachedPlayerVehicleID : -1;
                    }
                    
                    // Get player name from scoring info for validation
                    var playerName = System.Text.Encoding.UTF8.GetString(scoring.mScoringInfo.mPlayerName).TrimEnd('\0');
                    Console.WriteLine($"🎯 Looking for player: '{playerName}'");
                    
                    // Track the best candidate if no exact match is found
                    int bestCandidateID = -1;
                    string bestCandidateReason = "";
                    
                    // Search through all vehicles to find the player's car
                    for (int i = 0; i < numVehicles && i < 128; i++)
                    {
                        var vehicle = scoring.mVehicles[i];
                        
                        var vehicleID = vehicle.mID;
                        var driverName = System.Text.Encoding.UTF8.GetString(vehicle.mDriverName).TrimEnd('\0');
                        var vehicleName = System.Text.Encoding.UTF8.GetString(vehicle.mVehicleName).TrimEnd('\0');
                        var isPlayer = vehicle.mIsPlayer == 1;
                        var control = vehicle.mControl;
                        var place = vehicle.mPlace;
                        
                        Console.WriteLine($"🚗 Vehicle {i}: ID={vehicleID}, Driver='{driverName}', Vehicle='{vehicleName}', IsPlayer={isPlayer}, Control={control}, Place={place}");
                        
                        // Check for player using multiple criteria with priority scoring
                        bool isPlayerVehicle = false;
                        string detectionMethod = "";
                        
                        // Priority 1: mIsPlayer flag (most reliable)
                        if (isPlayer)
                        {
                            isPlayerVehicle = true;
                            detectionMethod = "mIsPlayer flag";
                        }
                        
                        // Priority 2: mControl == 0 (local player control)
                        if (!isPlayerVehicle && control == 0)
                        {
                            isPlayerVehicle = true;
                            detectionMethod = "mControl == 0 (local player)";
                        }
                        
                        // Priority 3: driver name matches player name
                        if (!isPlayerVehicle && !string.IsNullOrEmpty(playerName) && 
                            !string.IsNullOrEmpty(driverName) && 
                            driverName.Equals(playerName, StringComparison.OrdinalIgnoreCase))
                        {
                            isPlayerVehicle = true;
                            detectionMethod = "driver name match";
                        }
                        
                        // Track candidates for fallback
                        if (!isPlayerVehicle)
                        {
                            // Prefer vehicles with control == 0 even if not marked as player
                            if (control == 0 && bestCandidateID == -1)
                            {
                                bestCandidateID = vehicleID;
                                bestCandidateReason = $"control == 0 (local control) - {vehicleName}";
                            }
                            // Or vehicles in first place as secondary fallback
                            else if (place == 1 && bestCandidateID == -1)
                            {
                                bestCandidateID = vehicleID;
                                bestCandidateReason = $"place == 1 (first place) - {vehicleName}";
                            }
                        }
                        
                        if (isPlayerVehicle)
                        {
                            Console.WriteLine($"   ✅ Found via {detectionMethod}");
                            Console.WriteLine($"🎯 Found player's vehicle: {vehicleName} (ID: {vehicleID})");
                            
                            // Cache the result
                            _cachedPlayerVehicleID = vehicleID;
                            _lastPlayerLookup = DateTime.Now;
                            return vehicleID;
                        }
                    }
                    
                    Console.WriteLine("⚠️  Player's vehicle not found via primary methods");
                    
                    // Use best candidate if available
                    if (bestCandidateID != -1)
                    {
                        Console.WriteLine($"🔄 Using best candidate: {bestCandidateReason} (ID: {bestCandidateID})");
                        _cachedPlayerVehicleID = bestCandidateID;
                        _lastPlayerLookup = DateTime.Now;
                        return bestCandidateID;
                    }
                    
                    // Final fallback: use first vehicle with valid data
                    if (numVehicles > 0)
                    {
                        var fallbackVehicle = scoring.mVehicles[0];
                        var fallbackID = fallbackVehicle.mID;
                        var fallbackName = System.Text.Encoding.UTF8.GetString(fallbackVehicle.mVehicleName).TrimEnd('\0');
                        Console.WriteLine($"🔄 Using fallback vehicle: {fallbackName} (ID: {fallbackID})");
                        _cachedPlayerVehicleID = fallbackID;
                        _lastPlayerLookup = DateTime.Now;
                        return fallbackID;
                    }
                    
                    return -1;
                }
                catch (Exception marshalEx)
                {
                    Console.WriteLine($"⚠️  Error marshalling scoring data: {marshalEx.Message}");
                    
                    // Fallback to manual parsing with corrected offsets
                    var fallbackResult = FindPlayerVehicleIDFallback(scoringAccessor);
                    if (fallbackResult != -1)
                    {
                        _cachedPlayerVehicleID = fallbackResult;
                        _lastPlayerLookup = DateTime.Now;
                    }
                    return fallbackResult;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️  Error reading scoring data: {ex.Message}");
                return _cachedPlayerVehicleID != -1 ? _cachedPlayerVehicleID : -1;
            }
        }

        static int FindPlayerVehicleIDFallback(MemoryMappedViewAccessor scoringAccessor)
        {
            try
            {
                // Manual parsing with corrected structure offsets
                var scoringInfoOffset = 8 + 4; // Skip version info (8 bytes) + mBytesUpdatedHint (4 bytes)
                
                // Read mNumVehicles from rF2ScoringInfo
                // mNumVehicles is at offset 8 + 4 + 64 + 4 + 8 + 8 + 4 + 8 + 8 = 116
                var numVehiclesOffset = scoringInfoOffset + 64 + 4 + 8 + 8 + 4 + 8 + 8; // After mTrackName + mSession + mCurrentET + mEndET + mMaxLaps + mLapDist + pointer1
                var numVehicles = scoringAccessor.ReadInt32(numVehiclesOffset);
                
                Console.WriteLine($"🔍 Fallback: Scoring data - NumVehicles: {numVehicles}");
                
                if (numVehicles <= 0)
                {
                    Console.WriteLine("⚠️  No vehicles in fallback scoring data");
                    return -1;
                }
                
                // Calculate the start of the vehicle array
                var scoringInfoSize = Marshal.SizeOf<rF2ScoringInfo>();
                var vehicleArrayOffset = 8 + 4 + scoringInfoSize; // Skip version info + mBytesUpdatedHint + rF2ScoringInfo
                var vehicleSize = Marshal.SizeOf<rF2VehicleScoring>();
                
                Console.WriteLine($"🔧 Fallback: Vehicle array offset: {vehicleArrayOffset}, vehicle size: {vehicleSize}");
                
                // Search through vehicles
                for (int i = 0; i < numVehicles && i < 128; i++)
                {
                    var vehicleOffset = vehicleArrayOffset + (i * vehicleSize);
                    
                    try
                    {
                        var vehicleID = scoringAccessor.ReadInt32(vehicleOffset);
                        
                        // Read driver name (32 bytes at offset 4)
                        var driverNameBytes = new byte[32];
                        scoringAccessor.ReadArray(vehicleOffset + 4, driverNameBytes, 0, 32);
                        var driverName = System.Text.Encoding.UTF8.GetString(driverNameBytes).TrimEnd('\0');
                        
                        // Read vehicle name (64 bytes at offset 36)
                        var vehicleNameBytes = new byte[64];
                        scoringAccessor.ReadArray(vehicleOffset + 36, vehicleNameBytes, 0, 64);
                        var vehicleName = System.Text.Encoding.UTF8.GetString(vehicleNameBytes).TrimEnd('\0');
                        
                        // Read mIsPlayer flag (1 byte at offset 100 + 104 = 204)
                        var isPlayerOffset = vehicleOffset + 100 + 104; // After mTotalLaps + mSector + mFinishStatus + mLapDist + ... + mNumPenalties
                        var isPlayer = scoringAccessor.ReadByte(isPlayerOffset) == 1;
                        
                        // Read mControl flag (1 byte at offset 205)
                        var control = scoringAccessor.ReadSByte(isPlayerOffset + 1);
                        
                        Console.WriteLine($"🚗 Fallback Vehicle {i}: ID={vehicleID}, Driver='{driverName}', Vehicle='{vehicleName}', IsPlayer={isPlayer}, Control={control}");
                        
                        // Check for player using multiple criteria
                        if (isPlayer || control == 0)
                        {
                            Console.WriteLine($"🎯 Fallback found player's vehicle: {vehicleName} (ID: {vehicleID})");
                            return vehicleID;
                        }
                    }
                    catch (Exception vehicleEx)
                    {
                        Console.WriteLine($"⚠️  Error reading fallback vehicle {i}: {vehicleEx.Message}");
                    }
                }
                
                Console.WriteLine("⚠️  Player's vehicle not found in fallback scoring data");
                return -1;
            }
            catch (Exception fallbackEx)
            {
                Console.WriteLine($"⚠️  Error in fallback player detection: {fallbackEx.Message}");
                return -1;
            }
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

                // Find the player's vehicle ID from scoring data first
                var playerVehicleID = FindPlayerVehicleID();
                var targetVehicleIndex = 0; // Default to first vehicle
                var foundPlayerVehicle = false;
                
                if (playerVehicleID != -1)
                {
                    Console.WriteLine($"🎯 Looking for player's vehicle ID: {playerVehicleID}");
                    
                    // Find the correct vehicle in the telemetry array
                    for (int i = 0; i < telemetry.mNumVehicles && i < 128; i++)
                    {
                        var checkOffset = 16 + (i * Marshal.SizeOf<rF2VehicleTelemetry>());
                        
                        try
                        {
                            var checkID = accessor.ReadInt32(checkOffset);
                            
                            if (checkID == playerVehicleID)
                            {
                                targetVehicleIndex = i;
                                foundPlayerVehicle = true;
                                Console.WriteLine($"🎯 Found player's vehicle at index {targetVehicleIndex}");
                                break;
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"⚠️  Error checking vehicle {i} ID: {ex.Message}");
                        }
                    }
                }
                
                if (!foundPlayerVehicle)
                {
                    Console.WriteLine("⚠️  Could not find player's vehicle in telemetry data");
                    
                    // Try to find any vehicle with reasonable activity (non-zero RPM, throttle, etc.)
                    for (int i = 0; i < telemetry.mNumVehicles && i < 128; i++)
                    {
                        var checkOffset = 16 + (i * Marshal.SizeOf<rF2VehicleTelemetry>());
                        
                        try
                        {
                            // Read some basic telemetry to check if this vehicle has activity
                            var checkRPM = accessor.ReadDouble(checkOffset + 352); // Rough offset for RPM
                            var checkThrottle = accessor.ReadDouble(checkOffset + 372); // Rough offset for throttle
                            var checkBrake = accessor.ReadDouble(checkOffset + 380); // Rough offset for brake
                            var checkGear = accessor.ReadInt32(checkOffset + 344);  // Rough offset for gear
                            
                            // Check for signs of activity (RPM > idle, inputs > 0, or gear engaged)
                            if (checkRPM > 800 || checkThrottle > 0.1 || checkBrake > 0.1 || Math.Abs(checkGear) > 0)
                            {
                                targetVehicleIndex = i;
                                Console.WriteLine($"🔄 Found active vehicle at index {i} (RPM: {checkRPM:F0}, Throttle: {checkThrottle:F2}, Brake: {checkBrake:F2}, Gear: {checkGear})");
                                break;
                            }
                        }
                        catch
                        {
                            // Continue to next vehicle if this one has issues
                        }
                    }
                    
                    if (targetVehicleIndex == 0)
                    {
                        Console.WriteLine("🔄 No active vehicle found, using first vehicle");
                    }
                }
                
                // Use struct marshalling to read the correct vehicle data
                // The vehicle data starts at offset 16 (after the header) + (vehicle index * vehicle size)
                int vehicleDataOffset = 16 + (targetVehicleIndex * Marshal.SizeOf<rF2VehicleTelemetry>());
                
                Console.WriteLine($"🔧 Reading vehicle data at offset {vehicleDataOffset} (vehicle index {targetVehicleIndex})");
                
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
                    
                    // Read wheel data with proper struct calculations
                    try
                    {
                        // Calculate the offset to the wheel data array
                        // The mWheels array is at the end of the rF2VehicleTelemetry struct
                        // We need to calculate the exact offset based on the struct layout
                        
                        // Size of all fields before mWheels in rF2VehicleTelemetry
                        int offsetToWheels = 0;
                        offsetToWheels += 4;  // mID
                        offsetToWheels += 8;  // mDeltaTime
                        offsetToWheels += 8;  // mElapsedTime
                        offsetToWheels += 4;  // mLapNumber
                        offsetToWheels += 8;  // mLapStartET
                        offsetToWheels += 64; // mVehicleName
                        offsetToWheels += 64; // mTrackName
                        offsetToWheels += 24; // mPos (3 doubles)
                        offsetToWheels += 24; // mLocalVel (3 doubles)
                        offsetToWheels += 24; // mLocalAccel (3 doubles)
                        offsetToWheels += 72; // mOri (3 * 3 doubles)
                        offsetToWheels += 24; // mLocalRot (3 doubles)
                        offsetToWheels += 24; // mLocalRotAccel (3 doubles)
                        offsetToWheels += 4;  // mGear
                        offsetToWheels += 8;  // mEngineRPM
                        offsetToWheels += 8;  // mEngineWaterTemp
                        offsetToWheels += 8;  // mEngineOilTemp
                        offsetToWheels += 8;  // mClutchRPM
                        offsetToWheels += 8;  // mUnfilteredThrottle
                        offsetToWheels += 8;  // mUnfilteredBrake
                        offsetToWheels += 8;  // mUnfilteredSteering
                        offsetToWheels += 8;  // mUnfilteredClutch
                        offsetToWheels += 8;  // mFilteredThrottle
                        offsetToWheels += 8;  // mFilteredBrake
                        offsetToWheels += 8;  // mFilteredSteering
                        offsetToWheels += 8;  // mFilteredClutch
                        offsetToWheels += 8;  // mSteeringShaftTorque
                        offsetToWheels += 8;  // mFront3rdDeflection
                        offsetToWheels += 8;  // mRear3rdDeflection
                        offsetToWheels += 8;  // mFrontWingHeight
                        offsetToWheels += 8;  // mFrontRideHeight
                        offsetToWheels += 8;  // mRearRideHeight
                        offsetToWheels += 8;  // mDrag
                        offsetToWheels += 8;  // mFrontDownforce
                        offsetToWheels += 8;  // mRearDownforce
                        offsetToWheels += 8;  // mFuel
                        offsetToWheels += 8;  // mEngineMaxRPM
                        offsetToWheels += 1;  // mScheduledStops
                        offsetToWheels += 1;  // mOverheating
                        offsetToWheels += 1;  // mDetached
                        offsetToWheels += 1;  // mHeadlights
                        offsetToWheels += 8;  // mDentSeverity
                        offsetToWheels += 8;  // mLastImpactET
                        offsetToWheels += 8;  // mLastImpactMagnitude
                        offsetToWheels += 24; // mLastImpactPos (3 doubles)
                        offsetToWheels += 8;  // mEngineTorque
                        offsetToWheels += 4;  // mCurrentSector
                        offsetToWheels += 1;  // mSpeedLimiter
                        offsetToWheels += 1;  // mMaxGears
                        offsetToWheels += 1;  // mFrontTireCompoundIndex
                        offsetToWheels += 1;  // mRearTireCompoundIndex
                        offsetToWheels += 8;  // mFuelCapacity
                        offsetToWheels += 1;  // mFrontFlapActivated
                        offsetToWheels += 1;  // mRearFlapActivated
                        offsetToWheels += 1;  // mRearFlapLegalStatus
                        offsetToWheels += 1;  // mIgnitionStarter
                        offsetToWheels += 18; // mFrontTireCompoundName
                        offsetToWheels += 18; // mRearTireCompoundName
                        offsetToWheels += 1;  // mSpeedLimiterAvailable
                        offsetToWheels += 1;  // mAntiStallActivated
                        offsetToWheels += 2;  // mUnused
                        offsetToWheels += 4;  // mVisualSteeringWheelRange
                        offsetToWheels += 8;  // mRearBrakeBias
                        offsetToWheels += 8;  // mTurboBoostPressure
                        offsetToWheels += 12; // mPhysicsToGraphicsOffset (3 floats)
                        offsetToWheels += 4;  // mPhysicalSteeringWheelRange
                        offsetToWheels += 8;  // mBatteryChargeFraction
                        offsetToWheels += 8;  // mElectricBoostMotorTorque
                        offsetToWheels += 8;  // mElectricBoostMotorRPM
                        offsetToWheels += 8;  // mElectricBoostMotorTemperature
                        offsetToWheels += 8;  // mElectricBoostWaterTemperature
                        offsetToWheels += 1;  // mElectricBoostMotorState
                        offsetToWheels += 111; // mExpansion
                        
                        var wheelDataOffset = vehicleDataOffset + offsetToWheels;
                        var wheelSize = Marshal.SizeOf<rF2Wheel>();
                        
                        Console.WriteLine($"🔧 Wheel data offset: {wheelDataOffset}, wheel size: {wheelSize}");
                        
                        for (int i = 0; i < 4; i++)
                        {
                            var wheelOffset = wheelDataOffset + i * wheelSize;
                            
                            // Read wheel data using proper struct offsets
                            vehicle.mWheels[i].mSuspensionDeflection = accessor.ReadDouble(wheelOffset + 0);
                            vehicle.mWheels[i].mRideHeight = accessor.ReadDouble(wheelOffset + 8);
                            vehicle.mWheels[i].mSuspForce = accessor.ReadDouble(wheelOffset + 16);
                            vehicle.mWheels[i].mBrakeTemp = accessor.ReadDouble(wheelOffset + 24);
                            vehicle.mWheels[i].mBrakePressure = accessor.ReadDouble(wheelOffset + 32);
                            vehicle.mWheels[i].mRotation = accessor.ReadDouble(wheelOffset + 40);
                            vehicle.mWheels[i].mLateralPatchVel = accessor.ReadDouble(wheelOffset + 48);
                            vehicle.mWheels[i].mLongitudinalPatchVel = accessor.ReadDouble(wheelOffset + 56);
                            vehicle.mWheels[i].mLateralGroundVel = accessor.ReadDouble(wheelOffset + 64);
                            vehicle.mWheels[i].mLongitudinalGroundVel = accessor.ReadDouble(wheelOffset + 72);
                            vehicle.mWheels[i].mCamber = accessor.ReadDouble(wheelOffset + 80);
                            vehicle.mWheels[i].mLateralForce = accessor.ReadDouble(wheelOffset + 88);
                            vehicle.mWheels[i].mLongitudinalForce = accessor.ReadDouble(wheelOffset + 96);
                            vehicle.mWheels[i].mTireLoad = accessor.ReadDouble(wheelOffset + 104);
                            vehicle.mWheels[i].mGripFract = accessor.ReadDouble(wheelOffset + 112);
                            vehicle.mWheels[i].mPressure = accessor.ReadDouble(wheelOffset + 120);
                            
                            // Read tire temperature (3 doubles at offset 128)
                            vehicle.mWheels[i].mTemperature[0] = accessor.ReadDouble(wheelOffset + 128);
                            vehicle.mWheels[i].mTemperature[1] = accessor.ReadDouble(wheelOffset + 136);
                            vehicle.mWheels[i].mTemperature[2] = accessor.ReadDouble(wheelOffset + 144);
                            
                            vehicle.mWheels[i].mWear = accessor.ReadDouble(wheelOffset + 152);
                            
                            // Read terrain name (16 bytes at offset 160)
                            accessor.ReadArray(wheelOffset + 160, vehicle.mWheels[i].mTerrainName, 0, 16);
                            
                            // Read surface info
                            vehicle.mWheels[i].mSurfaceType = accessor.ReadByte(wheelOffset + 176);
                            vehicle.mWheels[i].mFlat = accessor.ReadByte(wheelOffset + 177);
                            vehicle.mWheels[i].mDetached = accessor.ReadByte(wheelOffset + 178);
                            vehicle.mWheels[i].mStaticUndeflectedRadius = accessor.ReadByte(wheelOffset + 179);
                            
                            // Read additional wheel data
                            vehicle.mWheels[i].mVerticalTireDeflection = accessor.ReadDouble(wheelOffset + 180);
                            vehicle.mWheels[i].mWheelYLocation = accessor.ReadDouble(wheelOffset + 188);
                            vehicle.mWheels[i].mToe = accessor.ReadDouble(wheelOffset + 196);
                            vehicle.mWheels[i].mTireCarcassTemperature = accessor.ReadDouble(wheelOffset + 204);
                            
                            // Read tire inner layer temperature (3 doubles at offset 212)
                            vehicle.mWheels[i].mTireInnerLayerTemperature[0] = accessor.ReadDouble(wheelOffset + 212);
                            vehicle.mWheels[i].mTireInnerLayerTemperature[1] = accessor.ReadDouble(wheelOffset + 220);
                            vehicle.mWheels[i].mTireInnerLayerTemperature[2] = accessor.ReadDouble(wheelOffset + 228);
                            
                            // Read expansion data (24 bytes at offset 236)
                            accessor.ReadArray(wheelOffset + 236, vehicle.mWheels[i].mExpansion, 0, 24);
                        }
                        
                        // Debug output for first wheel
                        Console.WriteLine($"🔧 FL Wheel - Pressure: {vehicle.mWheels[0].mPressure:F1} kPa, Temp: {vehicle.mWheels[0].mTemperature[0]:F1}K, Load: {vehicle.mWheels[0].mTireLoad:F1}N");
                    }
                    catch (Exception wheelEx)
                    {
                        Console.WriteLine($"⚠️  Error reading wheel data: {wheelEx.Message}");
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
            Console.WriteLine("CONTROLS: 'q'=Quit | 'r'=Reconnect | 'l'=Toggle Logging | 's'=Statistics | 'p'=Reference Laps | 'v'=Validate Offsets | 't'=Test Player Detection");
            
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

        static void TestPlayerDetection()
        {
            Console.Clear();
            Console.WriteLine("🧪 TESTING PLAYER DETECTION LOGIC");
            Console.WriteLine("=================================");
            Console.WriteLine();
            
            try
            {
                // Clear cache to force fresh detection
                _cachedPlayerVehicleID = -1;
                _lastPlayerLookup = DateTime.MinValue;
                
                Console.WriteLine("1. Testing player detection...");
                var playerID = FindPlayerVehicleID();
                
                if (playerID != -1)
                {
                    Console.WriteLine($"✅ Player detection successful! Vehicle ID: {playerID}");
                    Console.WriteLine($"🔄 Cached for future lookups (expires in {PlayerLookupCacheTime.TotalSeconds}s)");
                }
                else
                {
                    Console.WriteLine("❌ Player detection failed!");
                }
                
                Console.WriteLine();
                Console.WriteLine("2. Testing cached lookup...");
                var cachedID = FindPlayerVehicleID();
                
                if (cachedID == playerID)
                {
                    Console.WriteLine("✅ Cache working correctly");
                }
                else
                {
                    Console.WriteLine("⚠️  Cache mismatch or detection changed");
                }
                
                Console.WriteLine();
                Console.WriteLine("3. Testing with telemetry data...");
                
                // Try to read telemetry with the detected player
                try
                {
                    using var mmf = MemoryMappedFile.OpenExisting("$rFactor2SMMP_Telemetry$");
                    using var accessor = mmf.CreateViewAccessor();
                    
                    var telemetry = ReadTelemetryData(accessor);
                    if (telemetry.HasValue)
                    {
                        var vehicleName = System.Text.Encoding.UTF8.GetString(telemetry.Value.mVehicles.mVehicleName).TrimEnd('\0');
                        var trackName = System.Text.Encoding.UTF8.GetString(telemetry.Value.mVehicles.mTrackName).TrimEnd('\0');
                        var rpm = telemetry.Value.mVehicles.mEngineRPM;
                        var gear = telemetry.Value.mVehicles.mGear;
                        
                        Console.WriteLine($"✅ Telemetry data read successfully!");
                        Console.WriteLine($"   Vehicle: {vehicleName}");
                        Console.WriteLine($"   Track: {trackName}");
                        Console.WriteLine($"   RPM: {rpm:F0}");
                        Console.WriteLine($"   Gear: {gear}");
                        Console.WriteLine($"   Vehicle ID: {telemetry.Value.mVehicles.mID}");
                        
                        if (telemetry.Value.mVehicles.mID == playerID)
                        {
                            Console.WriteLine("✅ Telemetry vehicle ID matches detected player ID!");
                        }
                        else
                        {
                            Console.WriteLine($"⚠️  Telemetry vehicle ID ({telemetry.Value.mVehicles.mID}) doesn't match detected player ID ({playerID})");
                        }
                    }
                    else
                    {
                        Console.WriteLine("❌ Could not read telemetry data");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error reading telemetry: {ex.Message}");
                }
                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Test failed: {ex.Message}");
            }
            
            Console.WriteLine();
            Console.WriteLine("Press any key to return to main view...");
            Console.ReadKey();
            Console.Clear();
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
