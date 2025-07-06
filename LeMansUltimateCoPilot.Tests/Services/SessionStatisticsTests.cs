using System;
using NUnit.Framework;
using LeMansUltimateCoPilot.Services;

namespace LeMansUltimateCoPilot.Tests.Services
{
    /// <summary>
    /// Unit tests for SessionStatistics class
    /// </summary>
    [TestFixture]
    public class SessionStatisticsTests
    {
        [Test]
        public void Constructor_ShouldInitializeWithDefaultValues()
        {
            // Act
            var stats = new SessionStatistics();

            // Assert
            Assert.That(stats.FilePath, Is.EqualTo(""));
            Assert.That(stats.FileName, Is.EqualTo(""));
            Assert.That(stats.FileSize, Is.EqualTo(0));
            Assert.That(stats.RecordCount, Is.EqualTo(0));
            Assert.That(stats.SessionStart, Is.Null);
            Assert.That(stats.SessionEnd, Is.Null);
            Assert.That(stats.Duration, Is.Null);
            Assert.That(stats.Track, Is.EqualTo(""));
            Assert.That(stats.Vehicle, Is.EqualTo(""));
        }

        [Test]
        public void Properties_ShouldAllowGetAndSet()
        {
            // Arrange
            var stats = new SessionStatistics();
            var testTime = DateTime.Now;
            var testDuration = TimeSpan.FromMinutes(30);

            // Act & Assert
            stats.FilePath = "test/path/session.csv";
            Assert.That(stats.FilePath, Is.EqualTo("test/path/session.csv"));

            stats.FileName = "session.csv";
            Assert.That(stats.FileName, Is.EqualTo("session.csv"));

            stats.FileSize = 1024;
            Assert.That(stats.FileSize, Is.EqualTo(1024));

            stats.RecordCount = 500;
            Assert.That(stats.RecordCount, Is.EqualTo(500));

            stats.SessionStart = testTime;
            Assert.That(stats.SessionStart, Is.EqualTo(testTime));

            stats.SessionEnd = testTime.AddMinutes(30);
            Assert.That(stats.SessionEnd, Is.EqualTo(testTime.AddMinutes(30)));

            stats.Duration = testDuration;
            Assert.That(stats.Duration, Is.EqualTo(testDuration));

            stats.Track = "Spa-Francorchamps";
            Assert.That(stats.Track, Is.EqualTo("Spa-Francorchamps"));

            stats.Vehicle = "Formula 1";
            Assert.That(stats.Vehicle, Is.EqualTo("Formula 1"));
        }

        [Test]
        public void FileSize_ShouldAcceptLargeValues()
        {
            // Arrange
            var stats = new SessionStatistics();
            var largeSize = 1024L * 1024L * 100L; // 100MB

            // Act
            stats.FileSize = largeSize;

            // Assert
            Assert.That(stats.FileSize, Is.EqualTo(largeSize));
        }

        [Test]
        public void Duration_ShouldCalculateCorrectly()
        {
            // Arrange
            var stats = new SessionStatistics();
            var startTime = new DateTime(2024, 1, 15, 14, 30, 0);
            var endTime = new DateTime(2024, 1, 15, 15, 0, 0);

            // Act
            stats.SessionStart = startTime;
            stats.SessionEnd = endTime;
            stats.Duration = endTime - startTime;

            // Assert
            Assert.That(stats.Duration, Is.EqualTo(TimeSpan.FromMinutes(30)));
        }

        [Test]
        public void NullableProperties_ShouldHandleNullValues()
        {
            // Arrange
            var stats = new SessionStatistics();

            // Act & Assert
            stats.SessionStart = null;
            Assert.That(stats.SessionStart, Is.Null);

            stats.SessionEnd = null;
            Assert.That(stats.SessionEnd, Is.Null);

            stats.Duration = null;
            Assert.That(stats.Duration, Is.Null);
        }
    }
}
