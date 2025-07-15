using System;
using System.Runtime.InteropServices;

namespace LeMansUltimateCoPilot.Models
{
    /// <summary>
    /// Enhanced telemetry data structure for AI driving coach analysis
    /// Contains all relevant data points for performance analysis and coaching
    /// </summary>
    public class EnhancedTelemetryData
    {
        // Timestamp and session info
        public DateTime Timestamp { get; set; }
        public double SessionTime { get; set; }
        public double LapTime { get; set; }
        public int LapNumber { get; set; }
        public float DeltaTime { get; set; }

        // Position and motion data
        public float PositionX { get; set; }
        public float PositionY { get; set; }
        public float PositionZ { get; set; }
        public float VelocityX { get; set; }
        public float VelocityY { get; set; }
        public float VelocityZ { get; set; }
        public float AccelerationX { get; set; }
        public float AccelerationY { get; set; }
        public float AccelerationZ { get; set; }

        // Vehicle dynamics
        public float Speed { get; set; } // km/h
        public float SpeedMPS { get; set; } // m/s for calculations
        public int Gear { get; set; }
        public float EngineRPM { get; set; }
        public float MaxRPM { get; set; }

        // Driver inputs (filtered and unfiltered)
        public float ThrottleInput { get; set; }
        public float BrakeInput { get; set; }
        public float SteeringInput { get; set; }
        public float ClutchInput { get; set; }
        public float UnfilteredThrottle { get; set; }
        public float UnfilteredBrake { get; set; }
        public float UnfilteredSteering { get; set; }
        public float UnfilteredClutch { get; set; }

        // Forces and physics
        public float LongitudinalG { get; set; }
        public float LateralG { get; set; }
        public float VerticalG { get; set; }
        public float SteeringTorque { get; set; }

        // Temperatures
        public float WaterTemperature { get; set; }
        public float OilTemperature { get; set; }
        public float TireTemperatureFL { get; set; }
        public float TireTemperatureFR { get; set; }
        public float TireTemperatureRL { get; set; }
        public float TireTemperatureRR { get; set; }

        // Tire data
        public float TirePressureFL { get; set; }
        public float TirePressureFR { get; set; }
        public float TirePressureRL { get; set; }
        public float TirePressureRR { get; set; }
        public float TireLoadFL { get; set; }
        public float TireLoadFR { get; set; }
        public float TireLoadRL { get; set; }
        public float TireLoadRR { get; set; }
        public float TireGripFL { get; set; }
        public float TireGripFR { get; set; }
        public float TireGripRL { get; set; }
        public float TireGripRR { get; set; }

        // Suspension
        public float SuspensionDeflectionFL { get; set; }
        public float SuspensionDeflectionFR { get; set; }
        public float SuspensionDeflectionRL { get; set; }
        public float SuspensionDeflectionRR { get; set; }
        public float SuspensionVelocityFL { get; set; }
        public float SuspensionVelocityFR { get; set; }
        public float SuspensionVelocityRL { get; set; }
        public float SuspensionVelocityRR { get; set; }

        // Aerodynamics and vehicle setup
        public float FrontDownforce { get; set; }
        public float RearDownforce { get; set; }
        public float Drag { get; set; }
        public float FrontRideHeight { get; set; }
        public float RearRideHeight { get; set; }

        // Fuel and pit data
        public float FuelLevel { get; set; }
        public bool PitLimiterActive { get; set; }
        public float PitSpeedLimit { get; set; }

        // Track and session information
        public string VehicleName { get; set; } = "";
        public string TrackName { get; set; } = "";
        public string TrackCondition { get; set; } = ""; // Dry, Wet, etc.
        public bool IsValidLap { get; set; }

        // Calculated fields for analysis
        public float DistanceTraveled { get; set; }
        public double DistanceFromStart { get; set; } // Distance from start of track in meters
        public float LapProgress { get; set; } // 0.0 to 1.0
        public float TimeDelta { get; set; } // Compared to reference
        public float BrakingForce { get; set; }
        public float TractionForce { get; set; }

        /// <summary>
        /// Create enhanced telemetry data from raw rFactor2 telemetry
        /// </summary>
        public static EnhancedTelemetryData FromRaw(rF2Telemetry rawTelemetry, DateTime timestamp)
        {
            var vehicle = rawTelemetry.mVehicles;
            var data = new EnhancedTelemetryData
            {
                Timestamp = timestamp,
                SessionTime = (float)vehicle.mElapsedTime,
                LapTime = vehicle.mLapStartET > 0 ? (float)(vehicle.mElapsedTime - vehicle.mLapStartET) : 0,
                LapNumber = vehicle.mLapNumber,
                DeltaTime = (float)vehicle.mDeltaTime,

                // Position and motion
                PositionX = (float)vehicle.mPos.x,
                PositionY = (float)vehicle.mPos.y,
                PositionZ = (float)vehicle.mPos.z,
                VelocityX = (float)vehicle.mLocalVel.x,
                VelocityY = (float)vehicle.mLocalVel.y,
                VelocityZ = (float)vehicle.mLocalVel.z,
                AccelerationX = (float)vehicle.mLocalAccel.x,
                AccelerationY = (float)vehicle.mLocalAccel.y,
                AccelerationZ = (float)vehicle.mLocalAccel.z,

                // Vehicle dynamics
                Gear = vehicle.mGear,
                EngineRPM = (float)vehicle.mEngineRPM,
                MaxRPM = (float)vehicle.mEngineMaxRPM,

                // Driver inputs
                ThrottleInput = (float)vehicle.mFilteredThrottle,
                BrakeInput = (float)vehicle.mFilteredBrake,
                SteeringInput = (float)vehicle.mFilteredSteering,
                ClutchInput = (float)vehicle.mFilteredClutch,
                UnfilteredThrottle = (float)vehicle.mUnfilteredThrottle,
                UnfilteredBrake = (float)vehicle.mUnfilteredBrake,
                UnfilteredSteering = (float)vehicle.mUnfilteredSteering,
                UnfilteredClutch = (float)vehicle.mUnfilteredClutch,

                // Forces
                SteeringTorque = (float)vehicle.mSteeringShaftTorque,

                // Temperatures
                WaterTemperature = (float)vehicle.mEngineWaterTemp,
                OilTemperature = (float)vehicle.mEngineOilTemp,

                // Aerodynamics
                FrontDownforce = (float)vehicle.mFrontDownforce,
                RearDownforce = (float)vehicle.mRearDownforce,
                Drag = (float)vehicle.mDrag,
                FrontRideHeight = (float)vehicle.mFrontRideHeight,
                RearRideHeight = (float)vehicle.mRearRideHeight,

                // Fuel
                FuelLevel = (float)vehicle.mFuel,
                PitLimiterActive = vehicle.mSpeedLimiter > 0,
                PitSpeedLimit = 0, // Not directly available in current struct

                // Track info
                VehicleName = System.Text.Encoding.ASCII.GetString(vehicle.mVehicleName).TrimEnd('\0'),
                TrackName = System.Text.Encoding.ASCII.GetString(vehicle.mTrackName).TrimEnd('\0'),
                IsValidLap = vehicle.mLapNumber > 0 && vehicle.mLapStartET > 0
            };

            // Calculate speed in both units
            data.SpeedMPS = (float)Math.Sqrt(data.VelocityX * data.VelocityX + data.VelocityZ * data.VelocityZ);
            data.Speed = data.SpeedMPS * 3.6f; // Convert to km/h

            // Calculate G-forces (assuming acceleration is in m/s²)
            data.LongitudinalG = data.AccelerationZ / 9.81f; // Forward/backward
            data.LateralG = data.AccelerationX / 9.81f; // Left/right
            data.VerticalG = data.AccelerationY / 9.81f; // Up/down

            // Set tire data from wheel array
            if (vehicle.mWheels != null && vehicle.mWheels.Length >= 4)
            {
                // FL, FR, RL, RR (Front Left, Front Right, Rear Left, Rear Right)
                data.TireTemperatureFL = (float)(vehicle.mWheels[0].mTemperature?[1] ?? 0); // Center temperature
                data.TireTemperatureFR = (float)(vehicle.mWheels[1].mTemperature?[1] ?? 0);
                data.TireTemperatureRL = (float)(vehicle.mWheels[2].mTemperature?[1] ?? 0);
                data.TireTemperatureRR = (float)(vehicle.mWheels[3].mTemperature?[1] ?? 0);

                // Convert from Kelvin to Celsius
                data.TireTemperatureFL = data.TireTemperatureFL > 0 ? (float)(data.TireTemperatureFL - 273.15) : 0;
                data.TireTemperatureFR = data.TireTemperatureFR > 0 ? (float)(data.TireTemperatureFR - 273.15) : 0;
                data.TireTemperatureRL = data.TireTemperatureRL > 0 ? (float)(data.TireTemperatureRL - 273.15) : 0;
                data.TireTemperatureRR = data.TireTemperatureRR > 0 ? (float)(data.TireTemperatureRR - 273.15) : 0;

                // Tire pressure (already in kPa)
                data.TirePressureFL = (float)vehicle.mWheels[0].mPressure;
                data.TirePressureFR = (float)vehicle.mWheels[1].mPressure;
                data.TirePressureRL = (float)vehicle.mWheels[2].mPressure;
                data.TirePressureRR = (float)vehicle.mWheels[3].mPressure;

                // Tire load (Newtons)
                data.TireLoadFL = (float)vehicle.mWheels[0].mTireLoad;
                data.TireLoadFR = (float)vehicle.mWheels[1].mTireLoad;
                data.TireLoadRL = (float)vehicle.mWheels[2].mTireLoad;
                data.TireLoadRR = (float)vehicle.mWheels[3].mTireLoad;

                // Tire grip fraction
                data.TireGripFL = (float)vehicle.mWheels[0].mGripFract;
                data.TireGripFR = (float)vehicle.mWheels[1].mGripFract;
                data.TireGripRL = (float)vehicle.mWheels[2].mGripFract;
                data.TireGripRR = (float)vehicle.mWheels[3].mGripFract;

                // Suspension deflection (meters)
                data.SuspensionDeflectionFL = (float)vehicle.mWheels[0].mSuspensionDeflection;
                data.SuspensionDeflectionFR = (float)vehicle.mWheels[1].mSuspensionDeflection;
                data.SuspensionDeflectionRL = (float)vehicle.mWheels[2].mSuspensionDeflection;
                data.SuspensionDeflectionRR = (float)vehicle.mWheels[3].mSuspensionDeflection;

                // Suspension velocity (not directly available in rF2Wheel, set to 0)
                data.SuspensionVelocityFL = 0;
                data.SuspensionVelocityFR = 0;
                data.SuspensionVelocityRL = 0;
                data.SuspensionVelocityRR = 0;
            }

            // Calculate additional derived values
            data.BrakingForce = data.BrakeInput * data.LongitudinalG;
            data.TractionForce = data.ThrottleInput * data.LongitudinalG;

            return data;
        }

        /// <summary>
        /// Get CSV header for telemetry logging
        /// </summary>
        public static string GetCSVHeader()
        {
            return "Timestamp,SessionTime,LapTime,LapNumber,DeltaTime," +
                   "PositionX,PositionY,PositionZ,VelocityX,VelocityY,VelocityZ," +
                   "AccelerationX,AccelerationY,AccelerationZ,Speed,SpeedMPS," +
                   "Gear,EngineRPM,MaxRPM," +
                   "ThrottleInput,BrakeInput,SteeringInput,ClutchInput," +
                   "UnfilteredThrottle,UnfilteredBrake,UnfilteredSteering,UnfilteredClutch," +
                   "LongitudinalG,LateralG,VerticalG,SteeringTorque," +
                   "WaterTemperature,OilTemperature," +
                   "TireTemperatureFL,TireTemperatureFR,TireTemperatureRL,TireTemperatureRR," +
                   "TirePressureFL,TirePressureFR,TirePressureRL,TirePressureRR," +
                   "TireLoadFL,TireLoadFR,TireLoadRL,TireLoadRR," +
                   "TireGripFL,TireGripFR,TireGripRL,TireGripRR," +
                   "SuspensionDeflectionFL,SuspensionDeflectionFR,SuspensionDeflectionRL,SuspensionDeflectionRR," +
                   "SuspensionVelocityFL,SuspensionVelocityFR,SuspensionVelocityRL,SuspensionVelocityRR," +
                   "FrontDownforce,RearDownforce,Drag,FrontRideHeight,RearRideHeight," +
                   "FuelLevel,PitLimiterActive,PitSpeedLimit," +
                   "VehicleName,TrackName,TrackCondition,IsValidLap," +
                   "DistanceTraveled,DistanceFromStart,LapProgress,TimeDelta,BrakingForce,TractionForce";
        }

        /// <summary>
        /// Convert telemetry data to CSV row
        /// </summary>
        public string ToCSVRow()
        {
            var culture = System.Globalization.CultureInfo.InvariantCulture;
            return $"{Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff", culture)}," +
                   $"{SessionTime.ToString("F3", culture)},{LapTime.ToString("F3", culture)},{LapNumber},{DeltaTime.ToString("F6", culture)}," +
                   $"{PositionX.ToString("F3", culture)},{PositionY.ToString("F3", culture)},{PositionZ.ToString("F3", culture)}," +
                   $"{VelocityX.ToString("F3", culture)},{VelocityY.ToString("F3", culture)},{VelocityZ.ToString("F3", culture)}," +
                   $"{AccelerationX.ToString("F3", culture)},{AccelerationY.ToString("F3", culture)},{AccelerationZ.ToString("F3", culture)}," +
                   $"{Speed.ToString("F2", culture)},{SpeedMPS.ToString("F3", culture)}," +
                   $"{Gear},{EngineRPM.ToString("F1", culture)},{MaxRPM.ToString("F1", culture)}," +
                   $"{ThrottleInput.ToString("F4", culture)},{BrakeInput.ToString("F4", culture)},{SteeringInput.ToString("F4", culture)},{ClutchInput.ToString("F4", culture)}," +
                   $"{UnfilteredThrottle.ToString("F4", culture)},{UnfilteredBrake.ToString("F4", culture)},{UnfilteredSteering.ToString("F4", culture)},{UnfilteredClutch.ToString("F4", culture)}," +
                   $"{LongitudinalG.ToString("F3", culture)},{LateralG.ToString("F3", culture)},{VerticalG.ToString("F3", culture)},{SteeringTorque.ToString("F3", culture)}," +
                   $"{WaterTemperature.ToString("F1", culture)},{OilTemperature.ToString("F1", culture)}," +
                   $"{TireTemperatureFL.ToString("F1", culture)},{TireTemperatureFR.ToString("F1", culture)},{TireTemperatureRL.ToString("F1", culture)},{TireTemperatureRR.ToString("F1", culture)}," +
                   $"{TirePressureFL.ToString("F1", culture)},{TirePressureFR.ToString("F1", culture)},{TirePressureRL.ToString("F1", culture)},{TirePressureRR.ToString("F1", culture)}," +
                   $"{TireLoadFL.ToString("F1", culture)},{TireLoadFR.ToString("F1", culture)},{TireLoadRL.ToString("F1", culture)},{TireLoadRR.ToString("F1", culture)}," +
                   $"{TireGripFL.ToString("F3", culture)},{TireGripFR.ToString("F3", culture)},{TireGripRL.ToString("F3", culture)},{TireGripRR.ToString("F3", culture)}," +
                   $"{SuspensionDeflectionFL.ToString("F4", culture)},{SuspensionDeflectionFR.ToString("F4", culture)},{SuspensionDeflectionRL.ToString("F4", culture)},{SuspensionDeflectionRR.ToString("F4", culture)}," +
                   $"{SuspensionVelocityFL.ToString("F4", culture)},{SuspensionVelocityFR.ToString("F4", culture)},{SuspensionVelocityRL.ToString("F4", culture)},{SuspensionVelocityRR.ToString("F4", culture)}," +
                   $"{FrontDownforce.ToString("F2", culture)},{RearDownforce.ToString("F2", culture)},{Drag.ToString("F3", culture)},{FrontRideHeight.ToString("F4", culture)},{RearRideHeight.ToString("F4", culture)}," +
                   $"{FuelLevel.ToString("F2", culture)},{PitLimiterActive},{PitSpeedLimit.ToString("F1", culture)}," +
                   $"\"{VehicleName}\",\"{TrackName}\",\"{TrackCondition}\",{IsValidLap}," +
                   $"{DistanceTraveled.ToString("F2", culture)},{DistanceFromStart.ToString("F2", culture)},{LapProgress.ToString("F4", culture)},{TimeDelta.ToString("F3", culture)},{BrakingForce.ToString("F3", culture)},{TractionForce.ToString("F3", culture)}";
        }
    }
}
