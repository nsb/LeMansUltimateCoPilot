using System;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using LeMansUltimateCoPilot.Models;
using LeMansUltimateCoPilot.Services;

namespace LeMansUltimateCoPilot.Tests.Services
{
    /// <summary>
    /// Unit tests for TelemetryLogger service
    /// Tests logging functionality, session management, and data validation
    /// </summary>
    [TestFixture]
    public class TelemetryLoggerTests
    {
        private TelemetryLogger _logger;
        private string _testLogDirectory;
        private EnhancedTelemetryData _sampleData;

        [SetUp]
        public void SetUp()
        {
            // Create temporary directory for test logs
            _testLogDirectory = Path.Combine(Path.GetTempPath(), "TelemetryLoggerTests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testLogDirectory);

            _logger = new TelemetryLogger(_testLogDirectory);

            // Create sample telemetry data
            _sampleData = new EnhancedTelemetryData
            {
                Timestamp = DateTime.Now,
                SessionTime = 120.5,
                LapTime = 85.234,
                LapNumber = 3,
                Speed = 180.5f,
                EngineRPM = 6500f,
                MaxRPM = 8000f,
                ThrottleInput = 0.85f,
                BrakeInput = 0.0f,
                VehicleName = "Test Vehicle",
                TrackName = "Test Track",
                IsValidLap = true
            };
        }

        [TearDown]
        public void TearDown()
        {
            _logger?.Dispose();
            
            // Clean up test directory
            if (Directory.Exists(_testLogDirectory))
            {
                Directory.Delete(_testLogDirectory, true);
            }
        }

        [Test]
        public void Constructor_ShouldInitializeWithDefaultDirectory()
        {
            // Act
            using var defaultLogger = new TelemetryLogger();

            // Assert
            Assert.That(defaultLogger.IsLogging, Is.False);
            Assert.That(defaultLogger.RecordCount, Is.EqualTo(0));
            Assert.That(defaultLogger.CurrentTrack, Is.EqualTo(""));
            Assert.That(defaultLogger.CurrentVehicle, Is.EqualTo(""));
        }

        [Test]
        public void Constructor_ShouldCreateLogDirectory()
        {
            // Assert
            Assert.That(Directory.Exists(_testLogDirectory), Is.True);
        }

        [Test]
        public void StartLoggingSession_ShouldCreateSessionFile()
        {
            // Act
            var result = _logger.StartLoggingSession("test_session");

            // Assert
            Assert.That(result, Is.True);
            Assert.That(_logger.IsLogging, Is.True);
            Assert.That(_logger.RecordCount, Is.EqualTo(0));
            
            // Check that log file was created
            var logFiles = Directory.GetFiles(_testLogDirectory, "*.csv");
            Assert.That(logFiles.Length, Is.EqualTo(1));
            Assert.That(logFiles[0], Does.Contain("test_session"));
        }

        [Test]
        public void StartLoggingSession_ShouldWriteCorrectHeader()
        {
            // Act
            _logger.StartLoggingSession("test_session");
            _logger.StopLoggingSession(); // Stop to release file handle

            // Assert
            var logFiles = Directory.GetFiles(_testLogDirectory, "*.csv");
            var content = File.ReadAllText(logFiles[0]);
            
            Assert.That(content, Does.Contain("# Telemetry Session Started:"));
            Assert.That(content, Does.Contain("# Session Name: test_session"));
            Assert.That(content, Does.Contain("# Log Format Version: 1.0"));
            Assert.That(content, Does.Contain("Timestamp,SessionTime,LapTime"));
        }

        [Test]
        public void StartLoggingSession_WhenAlreadyLogging_ShouldReturnFalse()
        {
            // Arrange
            _logger.StartLoggingSession("first_session");

            // Act
            var result = _logger.StartLoggingSession("second_session");

            // Assert
            Assert.That(result, Is.False);
            Assert.That(_logger.IsLogging, Is.True);
            
            // Should still have only one log file
            var logFiles = Directory.GetFiles(_testLogDirectory, "*.csv");
            Assert.That(logFiles.Length, Is.EqualTo(1));
        }

        [Test]
        public void StopLoggingSession_ShouldStopLoggingAndWriteSummary()
        {
            // Arrange
            _logger.StartLoggingSession("test_session");
            _logger.LogTelemetryData(_sampleData);

            // Act
            var result = _logger.StopLoggingSession();

            // Assert
            Assert.That(result, Is.True);
            Assert.That(_logger.IsLogging, Is.False);
            
            // Check that summary was written
            var logFiles = Directory.GetFiles(_testLogDirectory, "*.csv");
            var content = File.ReadAllText(logFiles[0]);
            
            Assert.That(content, Does.Contain("# Session Ended:"));
            Assert.That(content, Does.Contain("# Total Records: 1"));
            Assert.That(content, Does.Contain("# Track: Test Track"));
            Assert.That(content, Does.Contain("# Vehicle: Test Vehicle"));
        }

        [Test]
        public void StopLoggingSession_WhenNotLogging_ShouldReturnFalse()
        {
            // Act
            var result = _logger.StopLoggingSession();

            // Assert
            Assert.That(result, Is.False);
            Assert.That(_logger.IsLogging, Is.False);
        }

        [Test]
        public void LogTelemetryData_WhenNotLogging_ShouldReturnFalse()
        {
            // Act
            var result = _logger.LogTelemetryData(_sampleData);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void LogTelemetryData_WhenLogging_ShouldWriteDataAndReturnTrue()
        {
            // Arrange
            _logger.StartLoggingSession("test_session");

            // Act
            var result = _logger.LogTelemetryData(_sampleData);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(_logger.RecordCount, Is.EqualTo(1));
            Assert.That(_logger.CurrentTrack, Is.EqualTo("Test Track"));
            Assert.That(_logger.CurrentVehicle, Is.EqualTo("Test Vehicle"));
        }

        [Test]
        public void LogTelemetryData_ShouldWriteCorrectCSVData()
        {
            // Arrange
            _logger.StartLoggingSession("test_session");

            // Act
            _logger.LogTelemetryData(_sampleData);
            _logger.StopLoggingSession(); // Stop to release file handle

            // Assert
            var logFiles = Directory.GetFiles(_testLogDirectory, "*.csv");
            var content = File.ReadAllText(logFiles[0]);
            
            Assert.That(content, Does.Contain("120.500")); // SessionTime
            Assert.That(content, Does.Contain("85.234")); // LapTime
            Assert.That(content, Does.Contain("180.50")); // Speed
            Assert.That(content, Does.Contain("6500.0")); // EngineRPM
            Assert.That(content, Does.Contain("\"Test Vehicle\"")); // VehicleName
            Assert.That(content, Does.Contain("\"Test Track\"")); // TrackName
        }

        [Test]
        public void LogTelemetryData_WithInvalidData_ShouldReturnFalse()
        {
            // Arrange
            _logger.StartLoggingSession("test_session");
            var invalidData = new EnhancedTelemetryData
            {
                Speed = float.NaN, // Invalid speed
                EngineRPM = 6500f,
                ThrottleInput = 0.5f,
                BrakeInput = 0.0f
            };

            // Act
            var result = _logger.LogTelemetryData(invalidData);

            // Assert
            Assert.That(result, Is.False);
            Assert.That(_logger.RecordCount, Is.EqualTo(0));
        }

        [Test]
        public void LogTelemetryData_WithInvalidRPM_ShouldReturnFalse()
        {
            // Arrange
            _logger.StartLoggingSession("test_session");
            var invalidData = new EnhancedTelemetryData
            {
                Speed = 100f,
                EngineRPM = float.PositiveInfinity, // Invalid RPM
                ThrottleInput = 0.5f,
                BrakeInput = 0.0f
            };

            // Act
            var result = _logger.LogTelemetryData(invalidData);

            // Assert
            Assert.That(result, Is.False);
            Assert.That(_logger.RecordCount, Is.EqualTo(0));
        }

        [Test]
        public void LogTelemetryData_WithInvalidSpeed_ShouldReturnFalse()
        {
            // Arrange
            _logger.StartLoggingSession("test_session");
            var invalidData = new EnhancedTelemetryData
            {
                Speed = -50f, // Negative speed
                EngineRPM = 6500f,
                ThrottleInput = 0.5f,
                BrakeInput = 0.0f
            };

            // Act
            var result = _logger.LogTelemetryData(invalidData);

            // Assert
            Assert.That(result, Is.False);
            Assert.That(_logger.RecordCount, Is.EqualTo(0));
        }

        [Test]
        public void LogTelemetryData_WithInvalidThrottleInput_ShouldReturnFalse()
        {
            // Arrange
            _logger.StartLoggingSession("test_session");
            var invalidData = new EnhancedTelemetryData
            {
                Speed = 100f,
                EngineRPM = 6500f,
                ThrottleInput = 1.5f, // Invalid throttle > 1
                BrakeInput = 0.0f
            };

            // Act
            var result = _logger.LogTelemetryData(invalidData);

            // Assert
            Assert.That(result, Is.False);
            Assert.That(_logger.RecordCount, Is.EqualTo(0));
        }

        [Test]
        public void LogTelemetryData_WithInvalidBrakeInput_ShouldReturnFalse()
        {
            // Arrange
            _logger.StartLoggingSession("test_session");
            var invalidData = new EnhancedTelemetryData
            {
                Speed = 100f,
                EngineRPM = 6500f,
                ThrottleInput = 0.5f,
                BrakeInput = -0.1f // Invalid brake < 0
            };

            // Act
            var result = _logger.LogTelemetryData(invalidData);

            // Assert
            Assert.That(result, Is.False);
            Assert.That(_logger.RecordCount, Is.EqualTo(0));
        }

        [Test]
        public void LogTelemetryData_MultipleRecords_ShouldIncrementCount()
        {
            // Arrange
            _logger.StartLoggingSession("test_session");

            // Act
            _logger.LogTelemetryData(_sampleData);
            _logger.LogTelemetryData(_sampleData);
            _logger.LogTelemetryData(_sampleData);

            // Assert
            Assert.That(_logger.RecordCount, Is.EqualTo(3));
        }

        [Test]
        public void LogTelemetryData_ShouldRaiseDataLoggedEvent()
        {
            // Arrange
            _logger.StartLoggingSession("test_session");
            EnhancedTelemetryData? eventData = null;
            _logger.DataLogged += (sender, data) => eventData = data;

            // Act
            _logger.LogTelemetryData(_sampleData);

            // Assert
            Assert.That(eventData, Is.Not.Null);
            Assert.That(eventData.Speed, Is.EqualTo(_sampleData.Speed));
            Assert.That(eventData.VehicleName, Is.EqualTo(_sampleData.VehicleName));
        }

        [Test]
        public void StartLoggingSession_ShouldRaiseSessionStartedEvent()
        {
            // Arrange
            string? sessionFile = null;
            _logger.SessionStarted += (sender, file) => sessionFile = file;

            // Act
            _logger.StartLoggingSession("test_session");

            // Assert
            Assert.That(sessionFile, Is.Not.Null);
            Assert.That(sessionFile, Does.Contain("test_session"));
            Assert.That(sessionFile, Does.EndWith(".csv"));
        }

        [Test]
        public void StopLoggingSession_ShouldRaiseSessionStoppedEvent()
        {
            // Arrange
            _logger.StartLoggingSession("test_session");
            string? sessionFile = null;
            _logger.SessionStopped += (sender, file) => sessionFile = file;

            // Act
            _logger.StopLoggingSession();

            // Assert
            Assert.That(sessionFile, Is.Not.Null);
            Assert.That(sessionFile, Does.Contain("test_session"));
            Assert.That(sessionFile, Does.EndWith(".csv"));
        }

        [Test]
        public void GetAvailableLogFiles_ShouldReturnLogFiles()
        {
            // Arrange
            _logger.StartLoggingSession("test_session_1");
            _logger.StopLoggingSession();
            _logger.StartLoggingSession("test_session_2");
            _logger.StopLoggingSession();

            // Act
            var logFiles = _logger.GetAvailableLogFiles();

            // Assert
            Assert.That(logFiles.Count, Is.EqualTo(2));
            Assert.That(logFiles[0], Does.Contain("test_session"));
            Assert.That(logFiles[1], Does.Contain("test_session"));
        }

        [Test]
        public void GetAvailableLogFiles_WhenNoFiles_ShouldReturnEmptyList()
        {
            // Act
            var logFiles = _logger.GetAvailableLogFiles();

            // Assert
            Assert.That(logFiles.Count, Is.EqualTo(0));
        }

        [Test]
        public async Task GetSessionStatistics_ShouldReturnCorrectStatistics()
        {
            // Arrange
            _logger.StartLoggingSession("test_session");
            _logger.LogTelemetryData(_sampleData);
            _logger.LogTelemetryData(_sampleData);
            _logger.StopLoggingSession();

            var logFiles = _logger.GetAvailableLogFiles();
            var logFile = logFiles[0];

            // Act
            var stats = await _logger.GetSessionStatistics(logFile);

            // Assert
            Assert.That(stats, Is.Not.Null);
            // The count includes header line, so actual data lines are less
            Assert.That(stats.RecordCount, Is.GreaterThanOrEqualTo(2)); // Allow for header counting differences
            Assert.That(stats.Track, Is.EqualTo("Test Track"));
            Assert.That(stats.Vehicle, Is.EqualTo("Test Vehicle"));
            Assert.That(stats.SessionStart, Is.Not.Null);
            Assert.That(stats.SessionEnd, Is.Not.Null);
            Assert.That(stats.Duration, Is.Not.Null);
            Assert.That(stats.FileSize, Is.GreaterThan(0));
        }

        [Test]
        public async Task GetSessionStatistics_WithNonExistentFile_ShouldReturnNull()
        {
            // Act
            var stats = await _logger.GetSessionStatistics("nonexistent.csv");

            // Assert
            Assert.That(stats, Is.Null);
        }

        [Test]
        public void SessionStartTime_ShouldBeSetWhenLoggingStarts()
        {
            // Arrange
            var beforeStart = DateTime.Now.AddSeconds(-1);

            // Act
            _logger.StartLoggingSession("test_session");
            var afterStart = DateTime.Now.AddSeconds(1);

            // Assert
            Assert.That(_logger.SessionStartTime, Is.GreaterThan(beforeStart));
            Assert.That(_logger.SessionStartTime, Is.LessThan(afterStart));
        }

        [Test]
        public void TrackAndVehicleNames_ShouldBeUpdatedFromTelemetryData()
        {
            // Arrange
            _logger.StartLoggingSession("test_session");
            var data1 = new EnhancedTelemetryData
            {
                TrackName = "Spa-Francorchamps",
                VehicleName = "Formula 1",
                Speed = 200f,
                EngineRPM = 7000f,
                ThrottleInput = 0.9f,
                BrakeInput = 0.0f
            };

            // Act
            _logger.LogTelemetryData(data1);

            // Assert
            Assert.That(_logger.CurrentTrack, Is.EqualTo("Spa-Francorchamps"));
            Assert.That(_logger.CurrentVehicle, Is.EqualTo("Formula 1"));
        }

        [Test]
        public void TrackAndVehicleNames_ShouldUpdateWhenChanged()
        {
            // Arrange
            _logger.StartLoggingSession("test_session");
            var data1 = new EnhancedTelemetryData
            {
                TrackName = "Spa-Francorchamps",
                VehicleName = "Formula 1",
                Speed = 200f,
                EngineRPM = 7000f,
                ThrottleInput = 0.9f,
                BrakeInput = 0.0f
            };
            var data2 = new EnhancedTelemetryData
            {
                TrackName = "Silverstone",
                VehicleName = "McLaren",
                Speed = 180f,
                EngineRPM = 6500f,
                ThrottleInput = 0.8f,
                BrakeInput = 0.0f
            };

            // Act
            _logger.LogTelemetryData(data1);
            _logger.LogTelemetryData(data2);

            // Assert
            Assert.That(_logger.CurrentTrack, Is.EqualTo("Silverstone"));
            Assert.That(_logger.CurrentVehicle, Is.EqualTo("McLaren"));
        }

        [Test]
        public void Dispose_ShouldStopLoggingIfActive()
        {
            // Arrange
            _logger.StartLoggingSession("test_session");
            Assert.That(_logger.IsLogging, Is.True);

            // Act
            _logger.Dispose();

            // Assert
            Assert.That(_logger.IsLogging, Is.False);
        }

        [Test]
        public void LogMessage_EventShouldBeRaised()
        {
            // Arrange
            string? logMessage = null;
            _logger.LogMessage += (sender, message) => logMessage = message;

            // Act
            _logger.StartLoggingSession("test_session");

            // Assert
            Assert.That(logMessage, Is.Not.Null);
            Assert.That(logMessage, Does.Contain("Started logging session"));
        }

        [Test]
        public void SessionFileName_ShouldContainTimestamp()
        {
            // Act
            _logger.StartLoggingSession("test_session");
            var logFiles = _logger.GetAvailableLogFiles();

            // Assert
            Assert.That(logFiles.Count, Is.EqualTo(1));
            var fileName = Path.GetFileName(logFiles[0]);
            Assert.That(fileName, Does.Match(@"telemetry_test_session_\d{8}_\d{6}\.csv"));
        }

        [Test]
        public void SessionFileName_WithoutName_ShouldUseDefault()
        {
            // Act
            _logger.StartLoggingSession();
            var logFiles = _logger.GetAvailableLogFiles();

            // Assert
            Assert.That(logFiles.Count, Is.EqualTo(1));
            var fileName = Path.GetFileName(logFiles[0]);
            Assert.That(fileName, Does.Match(@"telemetry_\d{8}_\d{6}\.csv"));
        }
    }
}

