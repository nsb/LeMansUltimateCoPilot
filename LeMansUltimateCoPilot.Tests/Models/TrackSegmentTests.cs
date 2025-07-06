using System;
using NUnit.Framework;
using LeMansUltimateCoPilot.Models;

namespace LeMansUltimateCoPilot.Tests.Models
{
    /// <summary>
    /// Unit tests for TrackSegment model
    /// Tests data structure, calculations, and validation
    /// </summary>
    [TestFixture]
    public class TrackSegmentTests
    {
        private TrackSegment _segment;

        [SetUp]
        public void SetUp()
        {
            _segment = new TrackSegment();
        }

        [Test]
        public void Constructor_Default_SetsDefaultValues()
        {
            // Arrange & Act
            var segment = new TrackSegment();

            // Assert
            Assert.That(segment.Id, Is.Not.Empty);
            Assert.That(segment.SegmentNumber, Is.EqualTo(0));
            Assert.That(segment.SegmentType, Is.EqualTo(TrackSegmentType.Straight));
            Assert.That(segment.DifficultyRating, Is.EqualTo(1));
            Assert.That(segment.ImportanceRating, Is.EqualTo(1));
            Assert.That(segment.LastUpdated, Is.Not.EqualTo(DateTime.MinValue));
        }

        [Test]
        public void Constructor_WithParameters_SetsSpecifiedValues()
        {
            // Arrange & Act
            var segment = new TrackSegment(5, 250.5, 25.0, 100.0, 200.0, 10.0);

            // Assert
            Assert.That(segment.SegmentNumber, Is.EqualTo(5));
            Assert.That(segment.DistanceFromStart, Is.EqualTo(250.5));
            Assert.That(segment.SegmentLength, Is.EqualTo(25.0));
            Assert.That(segment.CenterX, Is.EqualTo(100.0));
            Assert.That(segment.CenterY, Is.EqualTo(200.0));
            Assert.That(segment.CenterZ, Is.EqualTo(10.0));
        }

        [Test]
        public void DistanceTo_ValidCoordinates_ReturnsCorrectDistance()
        {
            // Arrange
            _segment.CenterX = 0;
            _segment.CenterY = 0;
            _segment.CenterZ = 0;

            // Act
            double distance = _segment.DistanceTo(3, 4, 0);

            // Assert
            Assert.That(distance, Is.EqualTo(5.0).Within(0.01));
        }

        [Test]
        public void DistanceTo_3DCoordinates_ReturnsCorrectDistance()
        {
            // Arrange
            _segment.CenterX = 1;
            _segment.CenterY = 2;
            _segment.CenterZ = 3;

            // Act
            double distance = _segment.DistanceTo(4, 6, 7);

            // Assert
            double expected = Math.Sqrt(9 + 16 + 16); // sqrt(3² + 4² + 4²)
            Assert.That(distance, Is.EqualTo(expected).Within(0.01));
        }

        [Test]
        public void IsCorner_LeftTurn_ReturnsTrue()
        {
            // Arrange
            _segment.SegmentType = TrackSegmentType.LeftTurn;

            // Act & Assert
            Assert.That(_segment.IsCorner(), Is.True);
        }

        [Test]
        public void IsCorner_RightTurn_ReturnsTrue()
        {
            // Arrange
            _segment.SegmentType = TrackSegmentType.RightTurn;

            // Act & Assert
            Assert.That(_segment.IsCorner(), Is.True);
        }

        [Test]
        public void IsCorner_Chicane_ReturnsTrue()
        {
            // Arrange
            _segment.SegmentType = TrackSegmentType.Chicane;

            // Act & Assert
            Assert.That(_segment.IsCorner(), Is.True);
        }

        [Test]
        public void IsCorner_Straight_ReturnsFalse()
        {
            // Arrange
            _segment.SegmentType = TrackSegmentType.Straight;

            // Act & Assert
            Assert.That(_segment.IsCorner(), Is.False);
        }

        [Test]
        public void IsCorner_BrakingZone_ReturnsFalse()
        {
            // Arrange
            _segment.SegmentType = TrackSegmentType.BrakingZone;

            // Act & Assert
            Assert.That(_segment.IsCorner(), Is.False);
        }

        [Test]
        public void IsBrakingZone_BrakingZoneType_ReturnsTrue()
        {
            // Arrange
            _segment.SegmentType = TrackSegmentType.BrakingZone;

            // Act & Assert
            Assert.That(_segment.IsBrakingZone(), Is.True);
        }

        [Test]
        public void IsBrakingZone_HasBrakingPoint_ReturnsTrue()
        {
            // Arrange
            _segment.SegmentType = TrackSegmentType.Straight;
            _segment.BrakingPoint = 0.5;

            // Act & Assert
            Assert.That(_segment.IsBrakingZone(), Is.True);
        }

        [Test]
        public void IsBrakingZone_NoBrakingIndicators_ReturnsFalse()
        {
            // Arrange
            _segment.SegmentType = TrackSegmentType.Straight;
            _segment.BrakingPoint = 0.0;

            // Act & Assert
            Assert.That(_segment.IsBrakingZone(), Is.False);
        }

        [Test]
        public void ToString_ValidSegment_ReturnsFormattedString()
        {
            // Arrange
            _segment.SegmentNumber = 5;
            _segment.SegmentType = TrackSegmentType.RightTurn;
            _segment.DistanceFromStart = 275.5;
            _segment.OptimalSpeed = 145.2;

            // Act
            string result = _segment.ToString();

            // Assert
            Assert.That(result, Is.EqualTo("Segment 5: RightTurn @ 275.5m (145.2 km/h)"));
        }

        [Test]
        public void TrackSegmentType_AllValues_AreValid()
        {
            // Test that all enum values are defined
            var values = Enum.GetValues<TrackSegmentType>();
            
            Assert.That(values.Length, Is.GreaterThan(0));
            Assert.That(values, Contains.Item(TrackSegmentType.Straight));
            Assert.That(values, Contains.Item(TrackSegmentType.LeftTurn));
            Assert.That(values, Contains.Item(TrackSegmentType.RightTurn));
            Assert.That(values, Contains.Item(TrackSegmentType.Chicane));
            Assert.That(values, Contains.Item(TrackSegmentType.BrakingZone));
            Assert.That(values, Contains.Item(TrackSegmentType.AccelerationZone));
            Assert.That(values, Contains.Item(TrackSegmentType.Hairpin));
            Assert.That(values, Contains.Item(TrackSegmentType.FastCorner));
            Assert.That(values, Contains.Item(TrackSegmentType.SlowCorner));
            Assert.That(values, Contains.Item(TrackSegmentType.ComplexCorner));
        }

        [Test]
        public void Properties_SetAndGet_WorkCorrectly()
        {
            // Arrange
            var expectedId = "test-id";
            var expectedSegmentNumber = 10;
            var expectedDistance = 500.0;
            var expectedLength = 50.0;
            var expectedType = TrackSegmentType.Hairpin;
            var expectedX = 100.0;
            var expectedY = 200.0;
            var expectedZ = 15.0;
            var expectedHeading = 1.57;
            var expectedCurvature = 0.05;
            var expectedBanking = 0.1;
            var expectedElevation = 5.0;
            var expectedSpeed = 180.0;
            var expectedGear = 4;
            var expectedBrakingPoint = 0.7;
            var expectedTurnIn = 0.2;
            var expectedApex = 0.5;
            var expectedExit = 0.8;
            var expectedDifficulty = 8;
            var expectedImportance = 9;
            var expectedNotes = "Test notes";
            var expectedLastUpdated = DateTime.UtcNow;

            // Act
            _segment.Id = expectedId;
            _segment.SegmentNumber = expectedSegmentNumber;
            _segment.DistanceFromStart = expectedDistance;
            _segment.SegmentLength = expectedLength;
            _segment.SegmentType = expectedType;
            _segment.CenterX = expectedX;
            _segment.CenterY = expectedY;
            _segment.CenterZ = expectedZ;
            _segment.TrackHeading = expectedHeading;
            _segment.Curvature = expectedCurvature;
            _segment.Banking = expectedBanking;
            _segment.ElevationChange = expectedElevation;
            _segment.OptimalSpeed = expectedSpeed;
            _segment.RecommendedGear = expectedGear;
            _segment.BrakingPoint = expectedBrakingPoint;
            _segment.TurnInPoint = expectedTurnIn;
            _segment.ApexPoint = expectedApex;
            _segment.ExitPoint = expectedExit;
            _segment.DifficultyRating = expectedDifficulty;
            _segment.ImportanceRating = expectedImportance;
            _segment.Notes = expectedNotes;
            _segment.LastUpdated = expectedLastUpdated;

            // Assert
            Assert.That(_segment.Id, Is.EqualTo(expectedId));
            Assert.That(_segment.SegmentNumber, Is.EqualTo(expectedSegmentNumber));
            Assert.That(_segment.DistanceFromStart, Is.EqualTo(expectedDistance));
            Assert.That(_segment.SegmentLength, Is.EqualTo(expectedLength));
            Assert.That(_segment.SegmentType, Is.EqualTo(expectedType));
            Assert.That(_segment.CenterX, Is.EqualTo(expectedX));
            Assert.That(_segment.CenterY, Is.EqualTo(expectedY));
            Assert.That(_segment.CenterZ, Is.EqualTo(expectedZ));
            Assert.That(_segment.TrackHeading, Is.EqualTo(expectedHeading));
            Assert.That(_segment.Curvature, Is.EqualTo(expectedCurvature));
            Assert.That(_segment.Banking, Is.EqualTo(expectedBanking));
            Assert.That(_segment.ElevationChange, Is.EqualTo(expectedElevation));
            Assert.That(_segment.OptimalSpeed, Is.EqualTo(expectedSpeed));
            Assert.That(_segment.RecommendedGear, Is.EqualTo(expectedGear));
            Assert.That(_segment.BrakingPoint, Is.EqualTo(expectedBrakingPoint));
            Assert.That(_segment.TurnInPoint, Is.EqualTo(expectedTurnIn));
            Assert.That(_segment.ApexPoint, Is.EqualTo(expectedApex));
            Assert.That(_segment.ExitPoint, Is.EqualTo(expectedExit));
            Assert.That(_segment.DifficultyRating, Is.EqualTo(expectedDifficulty));
            Assert.That(_segment.ImportanceRating, Is.EqualTo(expectedImportance));
            Assert.That(_segment.Notes, Is.EqualTo(expectedNotes));
            Assert.That(_segment.LastUpdated, Is.EqualTo(expectedLastUpdated));
        }

        [Test]
        public void CornerPoints_ValidValues_AreWithinRange()
        {
            // Test that corner points are within valid range [0, 1]
            _segment.TurnInPoint = 0.25;
            _segment.ApexPoint = 0.5;
            _segment.ExitPoint = 0.75;

            Assert.That(_segment.TurnInPoint, Is.GreaterThanOrEqualTo(0.0));
            Assert.That(_segment.TurnInPoint, Is.LessThanOrEqualTo(1.0));
            Assert.That(_segment.ApexPoint, Is.GreaterThanOrEqualTo(0.0));
            Assert.That(_segment.ApexPoint, Is.LessThanOrEqualTo(1.0));
            Assert.That(_segment.ExitPoint, Is.GreaterThanOrEqualTo(0.0));
            Assert.That(_segment.ExitPoint, Is.LessThanOrEqualTo(1.0));
        }

        [Test]
        public void DifficultyRating_ValidRange_IsAccepted()
        {
            // Test valid difficulty ratings
            for (int i = 1; i <= 10; i++)
            {
                _segment.DifficultyRating = i;
                Assert.That(_segment.DifficultyRating, Is.EqualTo(i));
            }
        }

        [Test]
        public void ImportanceRating_ValidRange_IsAccepted()
        {
            // Test valid importance ratings
            for (int i = 1; i <= 10; i++)
            {
                _segment.ImportanceRating = i;
                Assert.That(_segment.ImportanceRating, Is.EqualTo(i));
            }
        }
    }
}
