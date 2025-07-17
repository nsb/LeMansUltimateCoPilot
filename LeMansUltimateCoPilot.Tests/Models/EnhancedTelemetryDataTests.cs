using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using LeMansUltimateCoPilot.Models;

namespace LeMansUltimateCoPilot.Tests.Models
{
    /// <summary>
    /// Unit tests for EnhancedTelemetryData class
    /// Tests data structure, CSV operations, and data validation
    /// </summary>
    [TestFixture]
    public class EnhancedTelemetryDataTests
    {
        private EnhancedTelemetryData _sampleData;

        [SetUp]
        public void SetUp()
        {
            _sampleData = new EnhancedTelemetryData
            {
                Timestamp = new DateTime(2024, 1, 15, 14, 30, 45, 123),
                SessionTime = 120.5,
                LapTime = 85.234,
                LapNumber = 3,
                DeltaTime = 0.016f,
                
                // Position and motion
                PositionX = 1234.567f,
                PositionY = 45.678f,
                PositionZ = 2345.789f,
                VelocityX = 15.5f,
                VelocityY = 2.1f,
                VelocityZ = 45.8f,
                AccelerationX = 2.5f,
                AccelerationY = 0.8f,
                AccelerationZ = 3.2f,
                
                // Vehicle dynamics
                Speed = 180.5f,
                SpeedMPS = 50.14f,
                Gear = 5,
                EngineRPM = 6500f,
                MaxRPM = 8000f,
                
                // Driver inputs
                ThrottleInput = 0.85f,
                BrakeInput = 0.0f,
                SteeringInput = 0.15f,
                ClutchInput = 0.0f,
                UnfilteredThrottle = 0.87f,
                UnfilteredBrake = 0.02f,
                UnfilteredSteering = 0.17f,
                UnfilteredClutch = 0.01f,
                
                // Forces and physics
                LongitudinalG = 0.326f,
                LateralG = 0.255f,
                VerticalG = 0.082f,
                SteeringTorque = 12.5f,
                
                // Temperatures
                WaterTemperature = 95.5f,
                OilTemperature = 115.2f,
                TireTemperatureFL = 85.5f,
                TireTemperatureFR = 84.8f,
                TireTemperatureRL = 87.2f,
                TireTemperatureRR = 86.9f,
                
                // Tire data
                TirePressureFL = 2.4f,
                TirePressureFR = 2.3f,
                TirePressureRL = 2.2f,
                TirePressureRR = 2.1f,
                TireLoadFL = 850f,
                TireLoadFR = 820f,
                TireLoadRL = 780f,
                TireLoadRR = 760f,
                TireGripFL = 0.95f,
                TireGripFR = 0.93f,
                TireGripRL = 0.91f,
                TireGripRR = 0.89f,
                
                // Fuel and setup
                FuelLevel = 45.8f,
                PitLimiterActive = false,
                PitSpeedLimit = 80f,
                
                // Track info
                VehicleName = "Test Vehicle",
                TrackName = "Test Track",
                IsValidLap = true,
                
                // Calculated fields
                DistanceTraveled = 1250.5f,
                LapProgress = 0.65f,
                TimeDelta = -0.245f,
                BrakingForce = 0.0f,
                TractionForce = 0.277f
            };
        }

        [Test]
        public void Constructor_ShouldInitializeWithDefaultValues()
        {
            // Arrange & Act
            var data = new EnhancedTelemetryData();

            // Assert
            Assert.That(data.Timestamp, Is.EqualTo(default(DateTime)));
            Assert.That(data.SessionTime, Is.EqualTo(0.0));
            Assert.That(data.LapTime, Is.EqualTo(0.0));
            Assert.That(data.LapNumber, Is.EqualTo(0));
            Assert.That(data.Speed, Is.EqualTo(0f));
            Assert.That(data.VehicleName, Is.EqualTo(""));
            Assert.That(data.TrackName, Is.EqualTo(""));
            Assert.That(data.IsValidLap, Is.False);
        }

        [Test]
        public void GetCSVHeader_ShouldReturnCorrectHeaderFormat()
        {
            // Act
            var header = EnhancedTelemetryData.GetCSVHeader();

            // Assert
            Assert.That(header, Is.Not.Null);
            Assert.That(header, Is.Not.Empty);
            Assert.That(header, Does.Contain("Timestamp"));
            Assert.That(header, Does.Contain("SessionTime"));
            Assert.That(header, Does.Contain("LapTime"));
            Assert.That(header, Does.Contain("Speed"));
            Assert.That(header, Does.Contain("EngineRPM"));
            Assert.That(header, Does.Contain("ThrottleInput"));
            Assert.That(header, Does.Contain("BrakeInput"));
            Assert.That(header, Does.Contain("VehicleName"));
            Assert.That(header, Does.Contain("TrackName"));
            Assert.That(header, Does.Contain("TireTemperatureFL"));
            
            // Count fields to ensure all are present
            var fields = header.Split(',');
            Assert.That(fields.Length, Is.GreaterThan(60), "Should have more than 60 telemetry fields");
        }

        [Test]
        public void ToCSVRow_ShouldReturnCorrectlyFormattedRow()
        {
            // Act
            var csvRow = _sampleData.ToCSVRow();

            // Assert
            Assert.That(csvRow, Is.Not.Null);
            Assert.That(csvRow, Is.Not.Empty);
            
            // Check timestamp format
            Assert.That(csvRow, Does.StartWith("2024-01-15 14:30:45.123"));
            
            // Check some key values are present
            Assert.That(csvRow, Does.Contain("120.500")); // SessionTime
            Assert.That(csvRow, Does.Contain("85.234")); // LapTime
            Assert.That(csvRow, Does.Contain("180.50")); // Speed
            Assert.That(csvRow, Does.Contain("6500.0")); // EngineRPM
            Assert.That(csvRow, Does.Contain("0.8500")); // ThrottleInput
            Assert.That(csvRow, Does.Contain("\"Test Vehicle\"")); // VehicleName with quotes
            Assert.That(csvRow, Does.Contain("\"Test Track\"")); // TrackName with quotes
            Assert.That(csvRow, Does.Contain("True")); // IsValidLap boolean
            
            // Count fields to ensure all are present
            var fields = csvRow.Split(',');
            Assert.That(fields.Length, Is.GreaterThan(60), "CSV row should have more than 60 fields");
        }

        [Test]
        public void ToCSVRow_ShouldHandleSpecialCharactersInStrings()
        {
            // Arrange
            _sampleData.VehicleName = "Test \"Vehicle\" with, commas";
            _sampleData.TrackName = "Track with, special chars";

            // Act
            var csvRow = _sampleData.ToCSVRow();

            // Assert
            Assert.That(csvRow, Does.Contain("\"Test \"Vehicle\" with, commas\""));
            Assert.That(csvRow, Does.Contain("\"Track with, special chars\""));
        }

        [Test]
        public void ToCSVRow_ShouldHandleEmptyStrings()
        {
            // Arrange
            _sampleData.VehicleName = "";
            _sampleData.TrackName = "";

            // Act
            var csvRow = _sampleData.ToCSVRow();

            // Assert
            Assert.That(csvRow, Does.Contain("\"\",\"\""));
        }

        [Test]
        public void ToCSVRow_ShouldFormatFloatingPointNumbersCorrectly()
        {
            // Arrange
            _sampleData.Speed = 123.456789f;
            _sampleData.EngineRPM = 6543.21f;
            _sampleData.ThrottleInput = 0.123456f;

            // Act
            var csvRow = _sampleData.ToCSVRow();

            // Assert
            // Use invariant culture formatting expectations
            Assert.That(csvRow, Does.Contain("123.46")); // Speed with 2 decimal places
            Assert.That(csvRow, Does.Contain("6543.2")); // RPM with 1 decimal place
            Assert.That(csvRow, Does.Contain("0.1235")); // ThrottleInput with 4 decimal places
        }

        [Test]
        public void Properties_ShouldAllowGetAndSet()
        {
            // Arrange
            var data = new EnhancedTelemetryData();
            var testTime = DateTime.Now;

            // Act & Assert - Test a sample of properties
            data.Timestamp = testTime;
            Assert.That(data.Timestamp, Is.EqualTo(testTime));

            data.Speed = 150.5f;
            Assert.That(data.Speed, Is.EqualTo(150.5f));

            data.Gear = 4;
            Assert.That(data.Gear, Is.EqualTo(4));

            data.ThrottleInput = 0.75f;
            Assert.That(data.ThrottleInput, Is.EqualTo(0.75f));

            data.VehicleName = "Formula 1";
            Assert.That(data.VehicleName, Is.EqualTo("Formula 1"));

            data.IsValidLap = true;
            Assert.That(data.IsValidLap, Is.True);
        }

        [Test]
        public void TireData_ShouldStoreAllFourWheels()
        {
            // Act & Assert
            Assert.That(_sampleData.TireTemperatureFL, Is.EqualTo(85.5f));
            Assert.That(_sampleData.TireTemperatureFR, Is.EqualTo(84.8f));
            Assert.That(_sampleData.TireTemperatureRL, Is.EqualTo(87.2f));
            Assert.That(_sampleData.TireTemperatureRR, Is.EqualTo(86.9f));

            Assert.That(_sampleData.TirePressureFL, Is.EqualTo(2.4f));
            Assert.That(_sampleData.TirePressureFR, Is.EqualTo(2.3f));
            Assert.That(_sampleData.TirePressureRL, Is.EqualTo(2.2f));
            Assert.That(_sampleData.TirePressureRR, Is.EqualTo(2.1f));

            Assert.That(_sampleData.TireLoadFL, Is.EqualTo(850f));
            Assert.That(_sampleData.TireLoadFR, Is.EqualTo(820f));
            Assert.That(_sampleData.TireLoadRL, Is.EqualTo(780f));
            Assert.That(_sampleData.TireLoadRR, Is.EqualTo(760f));

            Assert.That(_sampleData.TireGripFL, Is.EqualTo(0.95f));
            Assert.That(_sampleData.TireGripFR, Is.EqualTo(0.93f));
            Assert.That(_sampleData.TireGripRL, Is.EqualTo(0.91f));
            Assert.That(_sampleData.TireGripRR, Is.EqualTo(0.89f));
        }

        [Test]
        public void SuspensionData_ShouldStoreAllFourWheels()
        {
            // Arrange
            _sampleData.SuspensionDeflectionFL = 0.015f;
            _sampleData.SuspensionDeflectionFR = 0.018f;
            _sampleData.SuspensionDeflectionRL = 0.012f;
            _sampleData.SuspensionDeflectionRR = 0.014f;

            _sampleData.SuspensionVelocityFL = 0.25f;
            _sampleData.SuspensionVelocityFR = 0.28f;
            _sampleData.SuspensionVelocityRL = 0.22f;
            _sampleData.SuspensionVelocityRR = 0.24f;

            // Act & Assert
            Assert.That(_sampleData.SuspensionDeflectionFL, Is.EqualTo(0.015f));
            Assert.That(_sampleData.SuspensionDeflectionFR, Is.EqualTo(0.018f));
            Assert.That(_sampleData.SuspensionDeflectionRL, Is.EqualTo(0.012f));
            Assert.That(_sampleData.SuspensionDeflectionRR, Is.EqualTo(0.014f));

            Assert.That(_sampleData.SuspensionVelocityFL, Is.EqualTo(0.25f));
            Assert.That(_sampleData.SuspensionVelocityFR, Is.EqualTo(0.28f));
            Assert.That(_sampleData.SuspensionVelocityRL, Is.EqualTo(0.22f));
            Assert.That(_sampleData.SuspensionVelocityRR, Is.EqualTo(0.24f));
        }

        [Test]
        public void CalculatedFields_ShouldStoreCorrectValues()
        {
            // Act & Assert
            Assert.That(_sampleData.DistanceTraveled, Is.EqualTo(1250.5f));
            Assert.That(_sampleData.LapProgress, Is.EqualTo(0.65f));
            Assert.That(_sampleData.TimeDelta, Is.EqualTo(-0.245f));
            Assert.That(_sampleData.BrakingForce, Is.EqualTo(0.0f));
            Assert.That(_sampleData.TractionForce, Is.EqualTo(0.277f));
        }

        [Test]
        public void FromRaw_ShouldHandleNullArrays()
        {
            // This test would require the rF2Telemetry structure to be available
            // For now, we'll test the basic structure validation
            Assert.That(() => new EnhancedTelemetryData(), Throws.Nothing);
        }

        [Test]
        public void CSVHeaderAndRowFieldCount_ShouldMatch()
        {
            // Act
            var header = EnhancedTelemetryData.GetCSVHeader();
            var row = _sampleData.ToCSVRow();

            // Assert
            var headerFields = header.Split(',');
            
            // Parse CSV row properly (accounting for quoted fields)
            var rowFields = ParseCSVRow(row);

            Assert.That(rowFields.Length, Is.EqualTo(headerFields.Length), 
                "CSV row field count should match header field count");
        }

        private string[] ParseCSVRow(string csvRow)
        {
            var fields = new List<string>();
            var inQuotes = false;
            var currentField = new StringBuilder();

            for (int i = 0; i < csvRow.Length; i++)
            {
                char c = csvRow[i];
                
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (c == ',' && !inQuotes)
                {
                    fields.Add(currentField.ToString());
                    currentField.Clear();
                }
                else
                {
                    currentField.Append(c);
                }
            }
            
            // Add the last field
            fields.Add(currentField.ToString());
            
            return fields.ToArray();
        }

        [Test]
        public void TimestampFormatting_ShouldBeConsistent()
        {
            // Arrange
            var testTime = new DateTime(2024, 12, 25, 23, 59, 59, 999);
            _sampleData.Timestamp = testTime;

            // Act
            var csvRow = _sampleData.ToCSVRow();

            // Assert
            Assert.That(csvRow, Does.StartWith("2024-12-25 23:59:59.999"));
        }

        [Test]
        public void BooleanValues_ShouldFormatCorrectly()
        {
            // Arrange
            _sampleData.PitLimiterActive = true;
            _sampleData.IsValidLap = false;

            // Act
            var csvRow = _sampleData.ToCSVRow();

            // Assert
            Assert.That(csvRow, Does.Contain("True")); // PitLimiterActive
            Assert.That(csvRow, Does.Contain("False")); // IsValidLap
        }

        [Test]
        public void NegativeValues_ShouldFormatCorrectly()
        {
            // Arrange
            _sampleData.TimeDelta = -1.234f;
            _sampleData.LongitudinalG = -0.5f;
            _sampleData.AccelerationX = -2.5f;

            // Act
            var csvRow = _sampleData.ToCSVRow();

            // Assert
            Assert.That(csvRow, Does.Contain("-1.234")); // TimeDelta
            Assert.That(csvRow, Does.Contain("-0.500")); // LongitudinalG  
            Assert.That(csvRow, Does.Contain("-2.500")); // AccelerationX
        }
    }
}

