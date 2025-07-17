using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using LeMansUltimateCoPilot.Models;

namespace LeMansUltimateCoPilot.Tests.Models
{
    /// <summary>
    /// Unit tests for TrackConfiguration model
    /// Tests data structure, calculations, and validation
    /// </summary>
    [TestFixture]
    public class TrackConfigurationTests
    {
        private TrackConfiguration _trackConfig;

        [SetUp]
        public void SetUp()
        {
            _trackConfig = new TrackConfiguration();
        }

        [Test]
        public void Constructor_Default_SetsDefaultValues()
        {
            // Arrange & Act
            var config = new TrackConfiguration();

            // Assert
            Assert.That(config.Id, Is.Not.Empty);
            Assert.That(config.TrackName, Is.Empty);
            Assert.That(config.TrackVariant, Is.Empty);
            Assert.That(config.TrackDirection, Is.EqualTo(TrackDirection.Clockwise));
            Assert.That(config.Segments, Is.Not.Null);
            Assert.That(config.Segments.Count, Is.EqualTo(0));
            Assert.That(config.Version, Is.EqualTo("1.0"));
            Assert.That(config.CreatedDate, Is.Not.EqualTo(DateTime.MinValue));
            Assert.That(config.LastUpdated, Is.Not.EqualTo(DateTime.MinValue));
        }

        [Test]
        public void Constructor_WithParameters_SetsSpecifiedValues()
        {
            // Arrange & Act
            var config = new TrackConfiguration("Silverstone", "GP");

            // Assert
            Assert.That(config.TrackName, Is.EqualTo("Silverstone"));
            Assert.That(config.TrackVariant, Is.EqualTo("GP"));
        }

        [Test]
        public void AddSegment_ValidSegment_AddsToCollection()
        {
            // Arrange
            var segment = new TrackSegment(1, 25.0, 25.0, 100.0, 200.0, 10.0);
            var initialLastUpdated = _trackConfig.LastUpdated;

            // Act
            _trackConfig.AddSegment(segment);

            // Assert
            Assert.That(_trackConfig.Segments.Count, Is.EqualTo(1));
            Assert.That(_trackConfig.Segments[0], Is.EqualTo(segment));
            Assert.That(_trackConfig.LastUpdated, Is.GreaterThan(initialLastUpdated));
        }

        [Test]
        public void AddSegment_NullSegment_ThrowsArgumentNullException()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentNullException>(() => _trackConfig.AddSegment(null));
        }

        [Test]
        public void GetSegmentAtDistance_ValidDistance_ReturnsCorrectSegment()
        {
            // Arrange
            var segment1 = new TrackSegment(0, 0, 25.0, 0, 0, 0);
            var segment2 = new TrackSegment(1, 25.0, 25.0, 0, 0, 0);
            var segment3 = new TrackSegment(2, 50.0, 25.0, 0, 0, 0);
            
            _trackConfig.AddSegment(segment1);
            _trackConfig.AddSegment(segment2);
            _trackConfig.AddSegment(segment3);

            // Act
            var result1 = _trackConfig.GetSegmentAtDistance(10.0);
            var result2 = _trackConfig.GetSegmentAtDistance(30.0);
            var result3 = _trackConfig.GetSegmentAtDistance(60.0);

            // Assert
            Assert.That(result1, Is.EqualTo(segment1));
            Assert.That(result2, Is.EqualTo(segment2));
            Assert.That(result3, Is.EqualTo(segment3));
        }

        [Test]
        public void GetSegmentAtDistance_InvalidDistance_ReturnsNull()
        {
            // Arrange
            var segment = new TrackSegment(0, 0, 25.0, 0, 0, 0);
            _trackConfig.AddSegment(segment);

            // Act
            var result = _trackConfig.GetSegmentAtDistance(100.0);

            // Assert
            Assert.That(result, Is.Null);
        }

        [Test]
        public void GetClosestSegment_ValidPosition_ReturnsClosestSegment()
        {
            // Arrange
            var segment1 = new TrackSegment(0, 0, 25.0, 0, 0, 0);
            var segment2 = new TrackSegment(1, 25.0, 25.0, 10, 0, 0);
            var segment3 = new TrackSegment(2, 50.0, 25.0, 20, 0, 0);
            
            _trackConfig.AddSegment(segment1);
            _trackConfig.AddSegment(segment2);
            _trackConfig.AddSegment(segment3);

            // Act
            var result = _trackConfig.GetClosestSegment(5, 0, 0);

            // Assert
            Assert.That(result, Is.EqualTo(segment1));
        }

        [Test]
        public void GetClosestSegment_NoSegments_ReturnsNull()
        {
            // Act
            var result = _trackConfig.GetClosestSegment(0, 0, 0);

            // Assert
            Assert.That(result, Is.Null);
        }

        [Test]
        public void GetCornerSegments_MixedSegments_ReturnsOnlyCorners()
        {
            // Arrange
            var straight = new TrackSegment { SegmentType = TrackSegmentType.Straight };
            var leftTurn = new TrackSegment { SegmentType = TrackSegmentType.LeftTurn };
            var rightTurn = new TrackSegment { SegmentType = TrackSegmentType.RightTurn };
            var chicane = new TrackSegment { SegmentType = TrackSegmentType.Chicane };
            var braking = new TrackSegment { SegmentType = TrackSegmentType.BrakingZone };
            
            _trackConfig.AddSegment(straight);
            _trackConfig.AddSegment(leftTurn);
            _trackConfig.AddSegment(rightTurn);
            _trackConfig.AddSegment(chicane);
            _trackConfig.AddSegment(braking);

            // Act
            var corners = _trackConfig.GetCornerSegments();

            // Assert
            Assert.That(corners.Count, Is.EqualTo(3));
            Assert.That(corners, Contains.Item(leftTurn));
            Assert.That(corners, Contains.Item(rightTurn));
            Assert.That(corners, Contains.Item(chicane));
            Assert.That(corners, Does.Not.Contain(straight));
            Assert.That(corners, Does.Not.Contain(braking));
        }

        [Test]
        public void GetBrakingZoneSegments_MixedSegments_ReturnsOnlyBrakingZones()
        {
            // Arrange
            var straight = new TrackSegment { SegmentType = TrackSegmentType.Straight };
            var brakingZone = new TrackSegment { SegmentType = TrackSegmentType.BrakingZone };
            var straightWithBraking = new TrackSegment { SegmentType = TrackSegmentType.Straight, BrakingPoint = 0.5 };
            var corner = new TrackSegment { SegmentType = TrackSegmentType.LeftTurn };
            
            _trackConfig.AddSegment(straight);
            _trackConfig.AddSegment(brakingZone);
            _trackConfig.AddSegment(straightWithBraking);
            _trackConfig.AddSegment(corner);

            // Act
            var brakingZones = _trackConfig.GetBrakingZoneSegments();

            // Assert
            Assert.That(brakingZones.Count, Is.EqualTo(2));
            Assert.That(brakingZones, Contains.Item(brakingZone));
            Assert.That(brakingZones, Contains.Item(straightWithBraking));
            Assert.That(brakingZones, Does.Not.Contain(straight));
            Assert.That(brakingZones, Does.Not.Contain(corner));
        }

        [Test]
        public void GetSectorSegments_Sector1_ReturnsCorrectSegments()
        {
            // Arrange
            _trackConfig.Sector1End = 100.0;
            _trackConfig.Sector2End = 200.0;
            
            var segment1 = new TrackSegment { DistanceFromStart = 50.0 };
            var segment2 = new TrackSegment { DistanceFromStart = 150.0 };
            var segment3 = new TrackSegment { DistanceFromStart = 250.0 };
            
            _trackConfig.AddSegment(segment1);
            _trackConfig.AddSegment(segment2);
            _trackConfig.AddSegment(segment3);

            // Act
            var sector1Segments = _trackConfig.GetSectorSegments(1);

            // Assert
            Assert.That(sector1Segments.Count, Is.EqualTo(1));
            Assert.That(sector1Segments, Contains.Item(segment1));
        }

        [Test]
        public void GetSectorSegments_Sector2_ReturnsCorrectSegments()
        {
            // Arrange
            _trackConfig.Sector1End = 100.0;
            _trackConfig.Sector2End = 200.0;
            
            var segment1 = new TrackSegment { DistanceFromStart = 50.0 };
            var segment2 = new TrackSegment { DistanceFromStart = 150.0 };
            var segment3 = new TrackSegment { DistanceFromStart = 250.0 };
            
            _trackConfig.AddSegment(segment1);
            _trackConfig.AddSegment(segment2);
            _trackConfig.AddSegment(segment3);

            // Act
            var sector2Segments = _trackConfig.GetSectorSegments(2);

            // Assert
            Assert.That(sector2Segments.Count, Is.EqualTo(1));
            Assert.That(sector2Segments, Contains.Item(segment2));
        }

        [Test]
        public void GetSectorSegments_Sector3_ReturnsCorrectSegments()
        {
            // Arrange
            _trackConfig.Sector1End = 100.0;
            _trackConfig.Sector2End = 200.0;
            
            var segment1 = new TrackSegment { DistanceFromStart = 50.0 };
            var segment2 = new TrackSegment { DistanceFromStart = 150.0 };
            var segment3 = new TrackSegment { DistanceFromStart = 250.0 };
            
            _trackConfig.AddSegment(segment1);
            _trackConfig.AddSegment(segment2);
            _trackConfig.AddSegment(segment3);

            // Act
            var sector3Segments = _trackConfig.GetSectorSegments(3);

            // Assert
            Assert.That(sector3Segments.Count, Is.EqualTo(1));
            Assert.That(sector3Segments, Contains.Item(segment3));
        }

        [Test]
        public void GetSectorSegments_InvalidSector_ReturnsEmptyList()
        {
            // Act
            var result = _trackConfig.GetSectorSegments(4);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(0));
        }

        [Test]
        public void GetSegmentCount_MultipleSegments_ReturnsCorrectCount()
        {
            // Arrange
            _trackConfig.AddSegment(new TrackSegment());
            _trackConfig.AddSegment(new TrackSegment());
            _trackConfig.AddSegment(new TrackSegment());

            // Act
            int count = _trackConfig.GetSegmentCount();

            // Assert
            Assert.That(count, Is.EqualTo(3));
        }

        [Test]
        public void GetSegmentCount_NoSegments_ReturnsZero()
        {
            // Act
            int count = _trackConfig.GetSegmentCount();

            // Assert
            Assert.That(count, Is.EqualTo(0));
        }

        [Test]
        public void GetAverageSegmentLength_MultipleSegments_ReturnsCorrectAverage()
        {
            // Arrange
            _trackConfig.AddSegment(new TrackSegment { SegmentLength = 20.0 });
            _trackConfig.AddSegment(new TrackSegment { SegmentLength = 30.0 });
            _trackConfig.AddSegment(new TrackSegment { SegmentLength = 40.0 });

            // Act
            double average = _trackConfig.GetAverageSegmentLength();

            // Assert
            Assert.That(average, Is.EqualTo(30.0));
        }

        [Test]
        public void GetAverageSegmentLength_NoSegments_ReturnsZero()
        {
            // Act
            double average = _trackConfig.GetAverageSegmentLength();

            // Assert
            Assert.That(average, Is.EqualTo(0.0));
        }

        [Test]
        public void GetTrackDifficultyRating_MultipleSegments_ReturnsCorrectAverage()
        {
            // Arrange
            _trackConfig.AddSegment(new TrackSegment { DifficultyRating = 5 });
            _trackConfig.AddSegment(new TrackSegment { DifficultyRating = 7 });
            _trackConfig.AddSegment(new TrackSegment { DifficultyRating = 9 });

            // Act
            double rating = _trackConfig.GetTrackDifficultyRating();

            // Assert
            Assert.That(rating, Is.EqualTo(7.0));
        }

        [Test]
        public void GetTrackDifficultyRating_NoSegments_ReturnsOne()
        {
            // Act
            double rating = _trackConfig.GetTrackDifficultyRating();

            // Assert
            Assert.That(rating, Is.EqualTo(1.0));
        }

        [Test]
        public void IsValid_ValidConfiguration_ReturnsTrue()
        {
            // Arrange
            _trackConfig.TrackName = "Test Track";
            _trackConfig.TrackLength = 1000.0;
            _trackConfig.AddSegment(new TrackSegment { SegmentNumber = 0 });
            _trackConfig.AddSegment(new TrackSegment { SegmentNumber = 1 });

            // Act
            bool isValid = _trackConfig.IsValid();

            // Assert
            Assert.That(isValid, Is.True);
        }

        [Test]
        public void IsValid_EmptyTrackName_ReturnsFalse()
        {
            // Arrange
            _trackConfig.TrackName = "";
            _trackConfig.TrackLength = 1000.0;
            _trackConfig.AddSegment(new TrackSegment());

            // Act
            bool isValid = _trackConfig.IsValid();

            // Assert
            Assert.That(isValid, Is.False);
        }

        [Test]
        public void IsValid_ZeroTrackLength_ReturnsFalse()
        {
            // Arrange
            _trackConfig.TrackName = "Test Track";
            _trackConfig.TrackLength = 0.0;
            _trackConfig.AddSegment(new TrackSegment());

            // Act
            bool isValid = _trackConfig.IsValid();

            // Assert
            Assert.That(isValid, Is.False);
        }

        [Test]
        public void IsValid_NoSegments_ReturnsFalse()
        {
            // Arrange
            _trackConfig.TrackName = "Test Track";
            _trackConfig.TrackLength = 1000.0;

            // Act
            bool isValid = _trackConfig.IsValid();

            // Assert
            Assert.That(isValid, Is.False);
        }

        [Test]
        public void IsValid_IncorrectSegmentOrdering_ReturnsFalse()
        {
            // Arrange
            _trackConfig.TrackName = "Test Track";
            _trackConfig.TrackLength = 1000.0;
            _trackConfig.AddSegment(new TrackSegment { SegmentNumber = 1 });
            _trackConfig.AddSegment(new TrackSegment { SegmentNumber = 0 });

            // Act
            bool isValid = _trackConfig.IsValid();

            // Assert
            Assert.That(isValid, Is.False);
        }

        [Test]
        public void ToString_ValidConfiguration_ReturnsFormattedString()
        {
            // Arrange
            _trackConfig.TrackName = "Silverstone";
            _trackConfig.TrackVariant = "GP";
            _trackConfig.TrackLength = 5891.0;
            _trackConfig.AddSegment(new TrackSegment());
            _trackConfig.AddSegment(new TrackSegment());

            // Act
            string result = _trackConfig.ToString();

            // Assert
            Assert.That(result, Is.EqualTo("Silverstone (GP) - 5891m, 2 segments"));
        }

        [Test]
        public void ToString_NoVariant_ReturnsFormattedStringWithoutVariant()
        {
            // Arrange
            _trackConfig.TrackName = "Silverstone";
            _trackConfig.TrackLength = 5891.0;
            _trackConfig.AddSegment(new TrackSegment());

            // Act
            string result = _trackConfig.ToString();

            // Assert
            Assert.That(result, Is.EqualTo("Silverstone - 5891m, 1 segments"));
        }

        [Test]
        public void TrackDirection_BothValues_AreValid()
        {
            // Test that both enum values are defined
            var values = Enum.GetValues<TrackDirection>();
            
            Assert.That(values.Length, Is.EqualTo(2));
            Assert.That(values, Contains.Item(TrackDirection.Clockwise));
            Assert.That(values, Contains.Item(TrackDirection.Counterclockwise));
        }

        [Test]
        public void Properties_SetAndGet_WorkCorrectly()
        {
            // Arrange
            var expectedId = "test-id";
            var expectedName = "Test Track";
            var expectedVariant = "GP";
            var expectedCountry = "UK";
            var expectedLength = 5891.0;
            var expectedWidth = 15.0;
            var expectedTurns = 18;
            var expectedDirection = TrackDirection.Counterclockwise;
            var expectedSector1 = 1000.0;
            var expectedSector2 = 2000.0;
            var expectedMinElev = 10.0;
            var expectedMaxElev = 50.0;
            var expectedTotalElev = 40.0;
            var expectedVersion = "2.0";
            var expectedNotes = "Test notes";

            // Act
            _trackConfig.Id = expectedId;
            _trackConfig.TrackName = expectedName;
            _trackConfig.TrackVariant = expectedVariant;
            _trackConfig.Country = expectedCountry;
            _trackConfig.TrackLength = expectedLength;
            _trackConfig.TrackWidth = expectedWidth;
            _trackConfig.NumberOfTurns = expectedTurns;
            _trackConfig.TrackDirection = expectedDirection;
            _trackConfig.Sector1End = expectedSector1;
            _trackConfig.Sector2End = expectedSector2;
            _trackConfig.MinElevation = expectedMinElev;
            _trackConfig.MaxElevation = expectedMaxElev;
            _trackConfig.TotalElevationChange = expectedTotalElev;
            _trackConfig.Version = expectedVersion;
            _trackConfig.Notes = expectedNotes;

            // Assert
            Assert.That(_trackConfig.Id, Is.EqualTo(expectedId));
            Assert.That(_trackConfig.TrackName, Is.EqualTo(expectedName));
            Assert.That(_trackConfig.TrackVariant, Is.EqualTo(expectedVariant));
            Assert.That(_trackConfig.Country, Is.EqualTo(expectedCountry));
            Assert.That(_trackConfig.TrackLength, Is.EqualTo(expectedLength));
            Assert.That(_trackConfig.TrackWidth, Is.EqualTo(expectedWidth));
            Assert.That(_trackConfig.NumberOfTurns, Is.EqualTo(expectedTurns));
            Assert.That(_trackConfig.TrackDirection, Is.EqualTo(expectedDirection));
            Assert.That(_trackConfig.Sector1End, Is.EqualTo(expectedSector1));
            Assert.That(_trackConfig.Sector2End, Is.EqualTo(expectedSector2));
            Assert.That(_trackConfig.MinElevation, Is.EqualTo(expectedMinElev));
            Assert.That(_trackConfig.MaxElevation, Is.EqualTo(expectedMaxElev));
            Assert.That(_trackConfig.TotalElevationChange, Is.EqualTo(expectedTotalElev));
            Assert.That(_trackConfig.Version, Is.EqualTo(expectedVersion));
            Assert.That(_trackConfig.Notes, Is.EqualTo(expectedNotes));
        }
    }
}

