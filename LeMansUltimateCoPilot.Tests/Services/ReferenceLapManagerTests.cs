using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using LeMansUltimateCoPilot.Models;
using LeMansUltimateCoPilot.Services;

namespace LeMansUltimateCoPilot.Tests.Services
{
    [TestFixture]
    public class ReferenceLapManagerTests
    {
        private ReferenceLapManager _manager;
        private string _testDirectory;
        private List<ReferenceLapSavedEventArgs> _savedEvents;
        private List<ReferenceLapsLoadedEventArgs> _loadedEvents;

        [SetUp]
        public void SetUp()
        {
            // Create temporary directory for tests
            _testDirectory = Path.Combine(Path.GetTempPath(), "LMUTestReferenceLaps", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testDirectory);
            
            _manager = new ReferenceLapManager(_testDirectory);
            _savedEvents = new List<ReferenceLapSavedEventArgs>();
            _loadedEvents = new List<ReferenceLapsLoadedEventArgs>();

            // Subscribe to events
            _manager.ReferenceLapSaved += (sender, args) => _savedEvents.Add(args);
            _manager.ReferenceLapsLoaded += (sender, args) => _loadedEvents.Add(args);
        }

        [TearDown]
        public void TearDown()
        {
            // Clean up test directory
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, true);
            }
        }

        [Test]
        public void Constructor_ShouldCreateDirectory()
        {
            // Assert
            Assert.That(Directory.Exists(_testDirectory), Is.True);
            Assert.That(_manager.TotalReferenceLaps, Is.EqualTo(0));
            Assert.That(_manager.TracksWithReferenceLaps, Is.EqualTo(0));
        }

        [Test]
        public void Constructor_WithNullDirectory_ShouldUseDefaultDirectory()
        {
            // Act
            var manager = new ReferenceLapManager(null);

            // Assert
            Assert.That(manager, Is.Not.Null);
            Assert.That(manager.TotalReferenceLaps, Is.EqualTo(0));
        }

        [Test]
        public void SaveReferenceLap_WithValidLap_ShouldReturnTrue()
        {
            // Arrange
            var referenceLap = CreateValidReferenceLap();

            // Act
            var result = _manager.SaveReferenceLap(referenceLap);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(_manager.TotalReferenceLaps, Is.EqualTo(1));
            Assert.That(_savedEvents.Count, Is.EqualTo(1));
            Assert.That(_savedEvents[0].ReferenceLap, Is.EqualTo(referenceLap));
        }

        [Test]
        public void SaveReferenceLap_WithNullLap_ShouldReturnFalse()
        {
            // Act
            var result = _manager.SaveReferenceLap(null!);

            // Assert
            Assert.That(result, Is.False);
            Assert.That(_manager.TotalReferenceLaps, Is.EqualTo(0));
        }

        [Test]
        public void SaveReferenceLap_WithInvalidLap_ShouldReturnFalse()
        {
            // Arrange
            var referenceLap = CreateValidReferenceLap();
            referenceLap.LapTime = 0; // Make it invalid

            // Act
            var result = _manager.SaveReferenceLap(referenceLap);

            // Assert
            Assert.That(result, Is.False);
            Assert.That(_manager.TotalReferenceLaps, Is.EqualTo(0));
        }

        [Test]
        public void SaveReferenceLap_ShouldCreateFile()
        {
            // Arrange
            var referenceLap = CreateValidReferenceLap();

            // Act
            _manager.SaveReferenceLap(referenceLap);

            // Assert
            var files = Directory.GetFiles(_testDirectory, "*.json");
            Assert.That(files.Length, Is.EqualTo(1));
            Assert.That(File.ReadAllText(files[0]), Does.Contain("Test Track"));
        }

        [Test]
        public void LoadReferenceLaps_WithNoFiles_ShouldReturnZero()
        {
            // Act
            var count = _manager.LoadReferenceLaps();

            // Assert
            Assert.That(count, Is.EqualTo(0));
            Assert.That(_loadedEvents.Count, Is.EqualTo(1));
            Assert.That(_loadedEvents[0].LoadedCount, Is.EqualTo(0));
        }

        [Test]
        public void LoadReferenceLaps_WithValidFiles_ShouldLoadCorrectly()
        {
            // Arrange
            var referenceLap1 = CreateValidReferenceLap();
            var referenceLap2 = CreateValidReferenceLap();
            referenceLap2.TrackName = "Test Track 2";
            
            _manager.SaveReferenceLap(referenceLap1);
            _manager.SaveReferenceLap(referenceLap2);

            // Create new manager to test loading
            var newManager = new ReferenceLapManager(_testDirectory);
            newManager.ReferenceLapsLoaded += (sender, args) => _loadedEvents.Add(args);

            // Act
            var count = newManager.LoadReferenceLaps();

            // Assert
            Assert.That(count, Is.EqualTo(2));
            Assert.That(newManager.TotalReferenceLaps, Is.EqualTo(2));
            Assert.That(newManager.TracksWithReferenceLaps, Is.EqualTo(2));
        }

        [Test]
        public void GetReferenceLaps_WithExistingTrack_ShouldReturnLaps()
        {
            // Arrange
            var referenceLap = CreateValidReferenceLap();
            _manager.SaveReferenceLap(referenceLap);

            // Act
            var laps = _manager.GetReferenceLaps("Test Track");

            // Assert
            Assert.That(laps.Count, Is.EqualTo(1));
            Assert.That(laps[0].TrackName, Is.EqualTo("Test Track"));
        }

        [Test]
        public void GetReferenceLaps_WithNonExistingTrack_ShouldReturnEmpty()
        {
            // Act
            var laps = _manager.GetReferenceLaps("Non-existing Track");

            // Assert
            Assert.That(laps, Is.Empty);
        }

        [Test]
        public void GetBestReferenceLap_WithExistingTrack_ShouldReturnBestLap()
        {
            // Arrange
            var lap1 = CreateValidReferenceLap();
            lap1.LapTime = 60.0;
            
            var lap2 = CreateValidReferenceLap();
            lap2.LapTime = 58.0; // Better lap

            _manager.SaveReferenceLap(lap1);
            _manager.SaveReferenceLap(lap2);

            // Act
            var bestLap = _manager.GetBestReferenceLap("Test Track");

            // Assert
            Assert.That(bestLap, Is.Not.Null);
            Assert.That(bestLap.LapTime, Is.EqualTo(58.0));
        }

        [Test]
        public void GetBestReferenceLap_WithVehicleFilter_ShouldReturnBestForVehicle()
        {
            // Arrange
            var lap1 = CreateValidReferenceLap();
            lap1.LapTime = 60.0;
            lap1.VehicleName = "Car A";
            
            var lap2 = CreateValidReferenceLap();
            lap2.LapTime = 58.0;
            lap2.VehicleName = "Car B";

            _manager.SaveReferenceLap(lap1);
            _manager.SaveReferenceLap(lap2);

            // Act
            var bestLap = _manager.GetBestReferenceLap("Test Track", "Car A");

            // Assert
            Assert.That(bestLap, Is.Not.Null);
            Assert.That(bestLap.LapTime, Is.EqualTo(60.0));
            Assert.That(bestLap.VehicleName, Is.EqualTo("Car A"));
        }

        [Test]
        public void GetBestReferenceLap_WithNonExistingTrack_ShouldReturnNull()
        {
            // Act
            var bestLap = _manager.GetBestReferenceLap("Non-existing Track");

            // Assert
            Assert.That(bestLap, Is.Null);
        }

        [Test]
        public void GetAvailableTracks_ShouldReturnTrackNames()
        {
            // Arrange
            var lap1 = CreateValidReferenceLap();
            lap1.TrackName = "Track A";
            
            var lap2 = CreateValidReferenceLap();
            lap2.TrackName = "Track B";

            _manager.SaveReferenceLap(lap1);
            _manager.SaveReferenceLap(lap2);

            // Act
            var tracks = _manager.GetAvailableTracks();

            // Assert
            Assert.That(tracks.Count, Is.EqualTo(2));
            Assert.That(tracks, Does.Contain("Track A"));
            Assert.That(tracks, Does.Contain("Track B"));
        }

        [Test]
        public void DeleteReferenceLap_WithExistingLap_ShouldReturnTrue()
        {
            // Arrange
            var referenceLap = CreateValidReferenceLap();
            _manager.SaveReferenceLap(referenceLap);

            // Act
            var result = _manager.DeleteReferenceLap(referenceLap.Id);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(_manager.TotalReferenceLaps, Is.EqualTo(0));
        }

        [Test]
        public void DeleteReferenceLap_WithNonExistingLap_ShouldReturnFalse()
        {
            // Act
            var result = _manager.DeleteReferenceLap("non-existing-id");

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void GetStatistics_ShouldReturnCorrectStatistics()
        {
            // Arrange
            var lap1 = CreateValidReferenceLap();
            lap1.LapTime = 60.0;
            
            var lap2 = CreateValidReferenceLap();
            lap2.LapTime = 58.0;

            _manager.SaveReferenceLap(lap1);
            _manager.SaveReferenceLap(lap2);

            // Act
            var stats = _manager.GetStatistics();

            // Assert
            Assert.That(stats.TotalReferenceLaps, Is.EqualTo(2));
            Assert.That(stats.TracksWithReferenceLaps, Is.EqualTo(1));
            Assert.That(stats.TrackStatistics, Does.ContainKey("Test Track"));
            
            var trackStats = stats.TrackStatistics["Test Track"];
            Assert.That(trackStats.TotalLaps, Is.EqualTo(2));
            Assert.That(trackStats.BestLapTime, Is.EqualTo(58.0));
            Assert.That(trackStats.AverageLapTime, Is.EqualTo(59.0));
        }

        [Test]
        public void Config_ShouldAllowCustomization()
        {
            // Arrange
            _manager.Config.MaxReferenceLapsPerTrack = 5;
            _manager.Config.AutoSaveValidLaps = true;
            _manager.Config.MinimumImprovementSeconds = 0.5;

            // Act
            var config = _manager.Config;

            // Assert
            Assert.That(config.MaxReferenceLapsPerTrack, Is.EqualTo(5));
            Assert.That(config.AutoSaveValidLaps, Is.True);
            Assert.That(config.MinimumImprovementSeconds, Is.EqualTo(0.5));
        }

        [Test]
        public void SaveReferenceLap_ShouldCleanupOldLaps()
        {
            // Arrange
            _manager.Config.MaxReferenceLapsPerTrack = 2;
            
            var lap1 = CreateValidReferenceLap();
            lap1.LapTime = 62.0;
            
            var lap2 = CreateValidReferenceLap();
            lap2.LapTime = 60.0;
            
            var lap3 = CreateValidReferenceLap();
            lap3.LapTime = 58.0; // Best lap

            // Act
            _manager.SaveReferenceLap(lap1);
            _manager.SaveReferenceLap(lap2);
            _manager.SaveReferenceLap(lap3); // Should trigger cleanup

            // Assert
            Assert.That(_manager.TotalReferenceLaps, Is.EqualTo(2));
            var laps = _manager.GetReferenceLaps("Test Track");
            Assert.That(laps.All(l => l.LapTime <= 60.0), Is.True); // Only best 2 should remain
        }

        [Test]
        public void LoadReferenceLaps_ShouldIgnoreInvalidFiles()
        {
            // Arrange
            var validLap = CreateValidReferenceLap();
            _manager.SaveReferenceLap(validLap);
            
            // Create an invalid JSON file
            var invalidFile = Path.Combine(_testDirectory, "invalid.json");
            File.WriteAllText(invalidFile, "invalid json content");

            // Create new manager to test loading
            var newManager = new ReferenceLapManager(_testDirectory);
            newManager.ReferenceLapsLoaded += (sender, args) => _loadedEvents.Add(args);

            // Act
            var count = newManager.LoadReferenceLaps();

            // Assert
            Assert.That(count, Is.EqualTo(1)); // Only the valid lap should be loaded
            Assert.That(newManager.TotalReferenceLaps, Is.EqualTo(1));
        }

        // Helper method
        private ReferenceLap CreateValidReferenceLap()
        {
            var telemetryData = new List<EnhancedTelemetryData>();
            
            // Create enough telemetry data for a valid lap
            for (int i = 0; i < 120; i++)
            {
                telemetryData.Add(new EnhancedTelemetryData
                {
                    Timestamp = DateTime.Now.AddSeconds(i * 0.5),
                    LapTime = i * 0.5,
                    LapNumber = 1,
                    TrackName = "Test Track",
                    VehicleName = "Test Vehicle",
                    Speed = 80 + (i % 60), // Varying speed for validation (80-139, range of 59)
                    IsValidLap = true,
                    LapProgress = (float)((double)i / 120),
                    DistanceTraveled = i * 30f
                });
            }

            return new ReferenceLap(telemetryData, 1);
        }
    }
}
