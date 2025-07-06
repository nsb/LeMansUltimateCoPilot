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
        public bool IsValidLap { get; set; }

        // Calculated fields for analysis
        public float DistanceTraveled { get; set; }
        public float LapProgress { get; set; } // 0.0 to 1.0
        public float TimeDelta { get; set; } // Compared to reference
        public float BrakingForce { get; set; }
        public float TractionForce { get; set; }

        /// <summary>
        /// Create enhanced telemetry data from raw rFactor2 telemetry
        /// </summary>
        public static EnhancedTelemetryData FromRaw(rF2Telemetry rawTelemetry, DateTime timestamp)
        {
            var data = new EnhancedTelemetryData
            {
                Timestamp = timestamp,
                SessionTime = rawTelemetry.mElapsedTime,
                LapTime = rawTelemetry.mLapStartET > 0 ? rawTelemetry.mElapsedTime - rawTelemetry.mLapStartET : 0,
                LapNumber = rawTelemetry.mLapNumber,
                DeltaTime = rawTelemetry.mDeltaTime,

                // Position and motion
                PositionX = rawTelemetry.mVehicles.mPos_x,
                PositionY = rawTelemetry.mVehicles.mPos_y,
                PositionZ = rawTelemetry.mVehicles.mPos_z,
                VelocityX = rawTelemetry.mVehicles.mLocalVel_x,
                VelocityY = rawTelemetry.mVehicles.mLocalVel_y,
                VelocityZ = rawTelemetry.mVehicles.mLocalVel_z,
                AccelerationX = rawTelemetry.mVehicles.mLocalAccel_x,
                AccelerationY = rawTelemetry.mVehicles.mLocalAccel_y,
                AccelerationZ = rawTelemetry.mVehicles.mLocalAccel_z,

                // Vehicle dynamics
                Gear = rawTelemetry.mVehicles.mGear,
                EngineRPM = rawTelemetry.mVehicles.mEngineRPM,
                MaxRPM = rawTelemetry.mVehicles.mEngineMaxRPM,

                // Driver inputs
                ThrottleInput = rawTelemetry.mVehicles.mFilteredThrottle,
                BrakeInput = rawTelemetry.mVehicles.mFilteredBrake,
                SteeringInput = rawTelemetry.mVehicles.mFilteredSteering,
                ClutchInput = rawTelemetry.mVehicles.mFilteredClutch,
                UnfilteredThrottle = rawTelemetry.mVehicles.mUnfilteredThrottle,
                UnfilteredBrake = rawTelemetry.mVehicles.mUnfilteredBrake,
                UnfilteredSteering = rawTelemetry.mVehicles.mUnfilteredSteering,
                UnfilteredClutch = rawTelemetry.mVehicles.mUnfilteredClutch,

                // Forces
                SteeringTorque = rawTelemetry.mVehicles.mSteeringShaftTorque,

                // Temperatures
                WaterTemperature = rawTelemetry.mVehicles.mEngineWaterTemp,
                OilTemperature = rawTelemetry.mVehicles.mEngineOilTemp,

                // Aerodynamics
                FrontDownforce = rawTelemetry.mVehicles.mFrontDownforce,
                RearDownforce = rawTelemetry.mVehicles.mRearDownforce,
                Drag = rawTelemetry.mVehicles.mDrag,
                FrontRideHeight = rawTelemetry.mVehicles.mFrontRideHeight,
                RearRideHeight = rawTelemetry.mVehicles.mRearRideHeight,

                // Fuel
                FuelLevel = rawTelemetry.mVehicles.mFuel,
                PitLimiterActive = rawTelemetry.mVehicles.mPitLimiter > 0,
                PitSpeedLimit = rawTelemetry.mVehicles.mPitSpeedLimit,

                // Track info
                VehicleName = System.Text.Encoding.ASCII.GetString(rawTelemetry.mVehicleName).TrimEnd('\0'),
                TrackName = System.Text.Encoding.ASCII.GetString(rawTelemetry.mTrackName).TrimEnd('\0'),
                IsValidLap = rawTelemetry.mLapNumber > 0 && rawTelemetry.mLapStartET > 0
            };

            // Calculate speed in both units
            data.SpeedMPS = (float)Math.Sqrt(data.VelocityX * data.VelocityX + data.VelocityZ * data.VelocityZ);
            data.Speed = data.SpeedMPS * 3.6f; // Convert to km/h

            // Calculate G-forces (assuming acceleration is in m/s²)
            data.LongitudinalG = data.AccelerationZ / 9.81f; // Forward/backward
            data.LateralG = data.AccelerationX / 9.81f; // Left/right
            data.VerticalG = data.AccelerationY / 9.81f; // Up/down

            // Set tire data if available
            if (rawTelemetry.mVehicles.mTireTemp != null && rawTelemetry.mVehicles.mTireTemp.Length >= 4)
            {
                data.TireTemperatureFL = rawTelemetry.mVehicles.mTireTemp[0];
                data.TireTemperatureFR = rawTelemetry.mVehicles.mTireTemp[1];
                data.TireTemperatureRL = rawTelemetry.mVehicles.mTireTemp[2];
                data.TireTemperatureRR = rawTelemetry.mVehicles.mTireTemp[3];
            }

            if (rawTelemetry.mVehicles.mTirePressure != null && rawTelemetry.mVehicles.mTirePressure.Length >= 4)
            {
                data.TirePressureFL = rawTelemetry.mVehicles.mTirePressure[0];
                data.TirePressureFR = rawTelemetry.mVehicles.mTirePressure[1];
                data.TirePressureRL = rawTelemetry.mVehicles.mTirePressure[2];
                data.TirePressureRR = rawTelemetry.mVehicles.mTirePressure[3];
            }

            if (rawTelemetry.mVehicles.mTireLoad != null && rawTelemetry.mVehicles.mTireLoad.Length >= 4)
            {
                data.TireLoadFL = rawTelemetry.mVehicles.mTireLoad[0];
                data.TireLoadFR = rawTelemetry.mVehicles.mTireLoad[1];
                data.TireLoadRL = rawTelemetry.mVehicles.mTireLoad[2];
                data.TireLoadRR = rawTelemetry.mVehicles.mTireLoad[3];
            }

            if (rawTelemetry.mVehicles.mTireGripFract != null && rawTelemetry.mVehicles.mTireGripFract.Length >= 4)
            {
                data.TireGripFL = rawTelemetry.mVehicles.mTireGripFract[0];
                data.TireGripFR = rawTelemetry.mVehicles.mTireGripFract[1];
                data.TireGripRL = rawTelemetry.mVehicles.mTireGripFract[2];
                data.TireGripRR = rawTelemetry.mVehicles.mTireGripFract[3];
            }

            if (rawTelemetry.mVehicles.mSuspensionDeflection != null && rawTelemetry.mVehicles.mSuspensionDeflection.Length >= 4)
            {
                data.SuspensionDeflectionFL = rawTelemetry.mVehicles.mSuspensionDeflection[0];
                data.SuspensionDeflectionFR = rawTelemetry.mVehicles.mSuspensionDeflection[1];
                data.SuspensionDeflectionRL = rawTelemetry.mVehicles.mSuspensionDeflection[2];
                data.SuspensionDeflectionRR = rawTelemetry.mVehicles.mSuspensionDeflection[3];
            }

            if (rawTelemetry.mVehicles.mSuspensionVelocity != null && rawTelemetry.mVehicles.mSuspensionVelocity.Length >= 4)
            {
                data.SuspensionVelocityFL = rawTelemetry.mVehicles.mSuspensionVelocity[0];
                data.SuspensionVelocityFR = rawTelemetry.mVehicles.mSuspensionVelocity[1];
                data.SuspensionVelocityRL = rawTelemetry.mVehicles.mSuspensionVelocity[2];
                data.SuspensionVelocityRR = rawTelemetry.mVehicles.mSuspensionVelocity[3];
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
                   "VehicleName,TrackName,IsValidLap," +
                   "DistanceTraveled,LapProgress,TimeDelta,BrakingForce,TractionForce";
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
                   $"\"{VehicleName}\",\"{TrackName}\",{IsValidLap}," +
                   $"{DistanceTraveled.ToString("F2", culture)},{LapProgress.ToString("F4", culture)},{TimeDelta.ToString("F3", culture)},{BrakingForce.ToString("F3", culture)},{TractionForce.ToString("F3", culture)}";
        }
    }
}
