using System;
using System.Collections.Generic;
using System.Linq;
using LeMansUltimateCoPilot.Models;

namespace LeMansUltimateCoPilot.Services
{
    /// <summary>
    /// Service for automatically mapping and segmenting racing tracks
    /// Analyzes telemetry data to create track configurations
    /// </summary>
    public class TrackMapper
    {
        private readonly List<TrackDataPoint> _dataPoints;
        private readonly double _segmentLength;
        private readonly double _curvatureThreshold;
        private readonly double _speedThreshold;

        /// <summary>
        /// Event raised when track mapping progress is updated
        /// </summary>
        public event EventHandler<TrackMappingProgressEventArgs>? MappingProgress;

        /// <summary>
        /// Event raised when track mapping is completed
        /// </summary>
        public event EventHandler<TrackMappingCompletedEventArgs>? MappingCompleted;

        /// <summary>
        /// Creates a new track mapper with default settings
        /// </summary>
        /// <param name="segmentLength">Default segment length in meters (default: 25m)</param>
        /// <param name="curvatureThreshold">Curvature threshold for corner detection (default: 0.01)</param>
        /// <param name="speedThreshold">Speed change threshold for zone detection (default: 10 km/h)</param>
        public TrackMapper(double segmentLength = 25.0, double curvatureThreshold = 0.01, double speedThreshold = 10.0)
        {
            _dataPoints = new List<TrackDataPoint>();
            _segmentLength = segmentLength;
            _curvatureThreshold = curvatureThreshold;
            _speedThreshold = speedThreshold;
        }

        /// <summary>
        /// Adds a telemetry data point for track mapping
        /// </summary>
        /// <param name="telemetryData">Enhanced telemetry data</param>
        public void AddDataPoint(EnhancedTelemetryData telemetryData)
        {
            if (telemetryData == null)
                throw new ArgumentNullException(nameof(telemetryData));

            var dataPoint = new TrackDataPoint
            {
                Timestamp = telemetryData.Timestamp,
                X = telemetryData.PositionX,
                Y = telemetryData.PositionY,
                Z = telemetryData.PositionZ,
                Speed = telemetryData.Speed,
                Heading = CalculateHeading(telemetryData.VelocityX, telemetryData.VelocityZ),
                Throttle = telemetryData.ThrottleInput,
                Brake = telemetryData.BrakeInput,
                Steering = telemetryData.SteeringInput,
                Gear = telemetryData.Gear,
                RPM = telemetryData.EngineRPM,
                LapDistance = telemetryData.DistanceTraveled
            };

            _dataPoints.Add(dataPoint);
        }

        /// <summary>
        /// Clears all collected data points
        /// </summary>
        public void ClearDataPoints()
        {
            _dataPoints.Clear();
        }

        /// <summary>
        /// Gets the number of collected data points
        /// </summary>
        /// <returns>Number of data points</returns>
        public int GetDataPointCount()
        {
            return _dataPoints.Count;
        }

        /// <summary>
        /// Creates a track configuration from collected telemetry data
        /// </summary>
        /// <param name="trackName">Name of the track</param>
        /// <param name="trackVariant">Track variant or layout</param>
        /// <returns>Generated track configuration</returns>
        public TrackConfiguration CreateTrackConfiguration(string trackName, string trackVariant = "")
        {
            if (string.IsNullOrEmpty(trackName))
                throw new ArgumentException("Track name cannot be empty", nameof(trackName));

            if (_dataPoints.Count < 100)
                throw new InvalidOperationException("Insufficient data points for track mapping. Need at least 100 points.");

            var config = new TrackConfiguration(trackName, trackVariant);
            
            // Calculate track statistics
            CalculateTrackStatistics(config);
            
            // Generate track segments
            GenerateTrackSegments(config);
            
            // Analyze segments for corner detection
            AnalyzeSegmentCharacteristics(config);
            
            // Calculate optimal speeds and coaching data
            CalculateOptimalData(config);

            MappingCompleted?.Invoke(this, new TrackMappingCompletedEventArgs(config));
            
            return config;
        }

        /// <summary>
        /// Calculates basic track statistics from telemetry data
        /// </summary>
        /// <param name="config">Track configuration to update</param>
        private void CalculateTrackStatistics(TrackConfiguration config)
        {
            if (!_dataPoints.Any())
                return;

            // Calculate track length
            config.TrackLength = _dataPoints.Max(p => p.LapDistance);

            // Find start/finish line (assume it's at lap distance 0)
            var startFinishPoint = _dataPoints.Where(p => Math.Abs(p.LapDistance) < 10).FirstOrDefault();
            if (startFinishPoint != null)
            {
                config.StartFinishX = startFinishPoint.X;
                config.StartFinishY = startFinishPoint.Y;
                config.StartFinishZ = startFinishPoint.Z;
            }

            // Calculate elevation statistics
            config.MinElevation = _dataPoints.Min(p => p.Z);
            config.MaxElevation = _dataPoints.Max(p => p.Z);
            config.TotalElevationChange = config.MaxElevation - config.MinElevation;

            // Estimate sector boundaries (divide track into thirds)
            config.Sector1End = config.TrackLength / 3.0;
            config.Sector2End = config.TrackLength * 2.0 / 3.0;

            // Detect track direction based on position changes
            DetectTrackDirection(config);
        }

        /// <summary>
        /// Detects the track direction (clockwise or counterclockwise)
        /// </summary>
        /// <param name="config">Track configuration to update</param>
        private void DetectTrackDirection(TrackConfiguration config)
        {
            if (_dataPoints.Count < 50)
                return;

            double totalAngleChange = 0;
            int validSamples = 0;

            for (int i = 1; i < Math.Min(_dataPoints.Count, 1000); i++)
            {
                var prev = _dataPoints[i - 1];
                var curr = _dataPoints[i];
                
                double angleChange = curr.Heading - prev.Heading;
                
                // Normalize angle change to [-π, π]
                while (angleChange > Math.PI) angleChange -= 2 * Math.PI;
                while (angleChange < -Math.PI) angleChange += 2 * Math.PI;
                
                if (Math.Abs(angleChange) < 0.1) // Only consider significant changes
                {
                    totalAngleChange += angleChange;
                    validSamples++;
                }
            }

            if (validSamples > 0)
            {
                config.TrackDirection = totalAngleChange > 0 ? TrackDirection.Counterclockwise : TrackDirection.Clockwise;
            }
        }

        /// <summary>
        /// Generates track segments from telemetry data
        /// </summary>
        /// <param name="config">Track configuration to update</param>
        private void GenerateTrackSegments(TrackConfiguration config)
        {
            if (config.TrackLength <= 0)
                return;

            int segmentCount = (int)Math.Ceiling(config.TrackLength / _segmentLength);
            double actualSegmentLength = config.TrackLength / segmentCount;

            for (int i = 0; i < segmentCount; i++)
            {
                double segmentStart = i * actualSegmentLength;
                double segmentEnd = Math.Min((i + 1) * actualSegmentLength, config.TrackLength);
                
                var segmentData = GetDataPointsInRange(segmentStart, segmentEnd);
                
                if (segmentData.Any())
                {
                    var segment = CreateSegmentFromData(i, segmentStart, segmentEnd - segmentStart, segmentData);
                    config.AddSegment(segment);
                }

                // Report progress
                int progress = (int)((i + 1) * 100.0 / segmentCount);
                MappingProgress?.Invoke(this, new TrackMappingProgressEventArgs(progress, $"Generating segment {i + 1}/{segmentCount}"));
            }
        }

        /// <summary>
        /// Gets data points within a specific distance range
        /// </summary>
        /// <param name="startDistance">Start distance in meters</param>
        /// <param name="endDistance">End distance in meters</param>
        /// <returns>Data points in the specified range</returns>
        private List<TrackDataPoint> GetDataPointsInRange(double startDistance, double endDistance)
        {
            return _dataPoints.Where(p => p.LapDistance >= startDistance && p.LapDistance < endDistance).ToList();
        }

        /// <summary>
        /// Creates a track segment from telemetry data points
        /// </summary>
        /// <param name="segmentNumber">Segment number</param>
        /// <param name="distanceFromStart">Distance from track start</param>
        /// <param name="segmentLength">Length of the segment</param>
        /// <param name="dataPoints">Data points for this segment</param>
        /// <returns>Created track segment</returns>
        private TrackSegment CreateSegmentFromData(int segmentNumber, double distanceFromStart, double segmentLength, List<TrackDataPoint> dataPoints)
        {
            var segment = new TrackSegment(segmentNumber, distanceFromStart, segmentLength, 0, 0, 0);

            if (!dataPoints.Any())
                return segment;

            // Calculate segment center position
            segment.CenterX = dataPoints.Average(p => p.X);
            segment.CenterY = dataPoints.Average(p => p.Y);
            segment.CenterZ = dataPoints.Average(p => p.Z);

            // Calculate average heading
            segment.TrackHeading = CalculateAverageHeading(dataPoints);

            // Calculate curvature
            segment.Curvature = CalculateCurvature(dataPoints);

            // Calculate elevation change
            if (dataPoints.Count > 1)
            {
                segment.ElevationChange = dataPoints.Max(p => p.Z) - dataPoints.Min(p => p.Z);
            }

            // Calculate optimal speed (average of fastest quartile)
            var speeds = dataPoints.Select(p => p.Speed).OrderByDescending(s => s).ToList();
            int topCount = Math.Max(1, speeds.Count / 4);
            segment.OptimalSpeed = speeds.Take(topCount).Average();

            // Calculate recommended gear
            var gears = dataPoints.Where(p => p.Gear > 0).Select(p => p.Gear).ToList();
            if (gears.Any())
            {
                segment.RecommendedGear = (int)Math.Round(gears.Average());
            }

            return segment;
        }

        /// <summary>
        /// Analyzes segment characteristics to identify corner types, braking zones, etc.
        /// </summary>
        /// <param name="config">Track configuration to analyze</param>
        private void AnalyzeSegmentCharacteristics(TrackConfiguration config)
        {
            for (int i = 0; i < config.Segments.Count; i++)
            {
                var segment = config.Segments[i];
                
                // Classify segment type based on curvature
                ClassifySegmentType(segment);
                
                // Calculate difficulty and importance ratings
                CalculateSegmentRatings(segment);
                
                // Detect braking and acceleration zones
                DetectBrakingZones(config, i);
                
                // Calculate corner points for turns
                if (segment.IsCorner())
                {
                    CalculateCornerPoints(segment);
                }
            }
        }

        /// <summary>
        /// Classifies the segment type based on curvature and other characteristics
        /// </summary>
        /// <param name="segment">Segment to classify</param>
        private void ClassifySegmentType(TrackSegment segment)
        {
            double absCurvature = Math.Abs(segment.Curvature);
            
            if (absCurvature < _curvatureThreshold)
            {
                segment.SegmentType = TrackSegmentType.Straight;
            }
            else if (absCurvature > 0.1) // Very tight turn
            {
                segment.SegmentType = TrackSegmentType.Hairpin;
            }
            else if (segment.OptimalSpeed > 180) // High speed turn
            {
                segment.SegmentType = TrackSegmentType.FastCorner;
            }
            else if (segment.OptimalSpeed < 100) // Low speed turn
            {
                segment.SegmentType = TrackSegmentType.SlowCorner;
            }
            else
            {
                // Determine turn direction
                segment.SegmentType = segment.Curvature > 0 ? TrackSegmentType.RightTurn : TrackSegmentType.LeftTurn;
            }
        }

        /// <summary>
        /// Calculates difficulty and importance ratings for a segment
        /// </summary>
        /// <param name="segment">Segment to rate</param>
        private void CalculateSegmentRatings(TrackSegment segment)
        {
            // Difficulty rating based on curvature and speed
            double curvatureFactor = Math.Min(Math.Abs(segment.Curvature) * 100, 5);
            double speedFactor = Math.Max(0, (200 - segment.OptimalSpeed) / 40);
            segment.DifficultyRating = Math.Min(10, (int)(curvatureFactor + speedFactor + 1));
            
            // Importance rating based on difficulty and potential time gain
            segment.ImportanceRating = Math.Min(10, segment.DifficultyRating + 2);
        }

        /// <summary>
        /// Detects braking zones before corners
        /// </summary>
        /// <param name="config">Track configuration</param>
        /// <param name="segmentIndex">Index of current segment</param>
        private void DetectBrakingZones(TrackConfiguration config, int segmentIndex)
        {
            var segment = config.Segments[segmentIndex];
            
            // Check if next segment is a corner and current is straight
            if (segmentIndex < config.Segments.Count - 1)
            {
                var nextSegment = config.Segments[segmentIndex + 1];
                if (nextSegment.IsCorner() && segment.SegmentType == TrackSegmentType.Straight)
                {
                    // Check if there's a significant speed difference
                    double speedDiff = segment.OptimalSpeed - nextSegment.OptimalSpeed;
                    if (speedDiff > _speedThreshold)
                    {
                        segment.SegmentType = TrackSegmentType.BrakingZone;
                        segment.BrakingPoint = 0.7; // Brake at 70% of segment
                    }
                }
            }
        }

        /// <summary>
        /// Calculates corner points (turn-in, apex, exit) for corner segments
        /// </summary>
        /// <param name="segment">Corner segment to analyze</param>
        private void CalculateCornerPoints(TrackSegment segment)
        {
            if (!segment.IsCorner())
                return;

            // Default corner points based on segment type
            switch (segment.SegmentType)
            {
                case TrackSegmentType.Hairpin:
                    segment.TurnInPoint = 0.2;
                    segment.ApexPoint = 0.4;
                    segment.ExitPoint = 0.8;
                    break;
                case TrackSegmentType.FastCorner:
                    segment.TurnInPoint = 0.1;
                    segment.ApexPoint = 0.5;
                    segment.ExitPoint = 0.9;
                    break;
                case TrackSegmentType.SlowCorner:
                    segment.TurnInPoint = 0.3;
                    segment.ApexPoint = 0.5;
                    segment.ExitPoint = 0.7;
                    break;
                default:
                    segment.TurnInPoint = 0.25;
                    segment.ApexPoint = 0.5;
                    segment.ExitPoint = 0.75;
                    break;
            }
        }

        /// <summary>
        /// Calculates optimal data for coaching and analysis
        /// </summary>
        /// <param name="config">Track configuration to update</param>
        private void CalculateOptimalData(TrackConfiguration config)
        {
            // Calculate number of turns
            config.NumberOfTurns = config.Segments.Count(s => s.IsCorner());
            
            // Generate coaching notes for difficult segments
            foreach (var segment in config.Segments.Where(s => s.DifficultyRating >= 7))
            {
                GenerateCoachingNotes(segment);
            }
        }

        /// <summary>
        /// Generates coaching notes for a segment
        /// </summary>
        /// <param name="segment">Segment to generate notes for</param>
        private void GenerateCoachingNotes(TrackSegment segment)
        {
            switch (segment.SegmentType)
            {
                case TrackSegmentType.Hairpin:
                    segment.Notes = "Slow corner - brake early, trail brake to apex, early throttle application";
                    break;
                case TrackSegmentType.FastCorner:
                    segment.Notes = "Fast corner - maintain speed, smooth inputs, late apex";
                    break;
                case TrackSegmentType.BrakingZone:
                    segment.Notes = "Braking zone - maximum braking, downshift preparation";
                    break;
                case TrackSegmentType.SlowCorner:
                    segment.Notes = "Technical corner - precise line, patience on throttle";
                    break;
                default:
                    if (segment.DifficultyRating >= 8)
                    {
                        segment.Notes = "Challenging section - focus on consistency and smooth inputs";
                    }
                    break;
            }
        }

        /// <summary>
        /// Calculates heading angle from velocity components
        /// </summary>
        /// <param name="velX">X velocity component</param>
        /// <param name="velY">Y velocity component</param>
        /// <returns>Heading angle in radians</returns>
        private double CalculateHeading(double velX, double velY)
        {
            return Math.Atan2(velY, velX);
        }

        /// <summary>
        /// Calculates average heading from data points
        /// </summary>
        /// <param name="dataPoints">Data points to analyze</param>
        /// <returns>Average heading in radians</returns>
        private double CalculateAverageHeading(List<TrackDataPoint> dataPoints)
        {
            if (!dataPoints.Any())
                return 0;

            double sumX = 0, sumY = 0;
            foreach (var point in dataPoints)
            {
                sumX += Math.Cos(point.Heading);
                sumY += Math.Sin(point.Heading);
            }

            return Math.Atan2(sumY, sumX);
        }

        /// <summary>
        /// Calculates curvature from data points
        /// </summary>
        /// <param name="dataPoints">Data points to analyze</param>
        /// <returns>Curvature value</returns>
        private double CalculateCurvature(List<TrackDataPoint> dataPoints)
        {
            if (dataPoints.Count < 3)
                return 0;

            // Calculate curvature using heading changes
            double totalCurvature = 0;
            int validSamples = 0;

            for (int i = 1; i < dataPoints.Count - 1; i++)
            {
                double heading1 = dataPoints[i - 1].Heading;
                double heading2 = dataPoints[i + 1].Heading;
                
                double headingChange = heading2 - heading1;
                
                // Normalize heading change
                while (headingChange > Math.PI) headingChange -= 2 * Math.PI;
                while (headingChange < -Math.PI) headingChange += 2 * Math.PI;
                
                totalCurvature += headingChange;
                validSamples++;
            }

            return validSamples > 0 ? totalCurvature / validSamples : 0;
        }
    }

    /// <summary>
    /// Internal data structure for track mapping
    /// </summary>
    internal class TrackDataPoint
    {
        public DateTime Timestamp { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
        public double Speed { get; set; }
        public double Heading { get; set; }
        public double Throttle { get; set; }
        public double Brake { get; set; }
        public double Steering { get; set; }
        public int Gear { get; set; }
        public double RPM { get; set; }
        public double LapDistance { get; set; }
    }

    /// <summary>
    /// Event arguments for track mapping progress
    /// </summary>
    public class TrackMappingProgressEventArgs : EventArgs
    {
        public int ProgressPercentage { get; }
        public string Message { get; }

        public TrackMappingProgressEventArgs(int progressPercentage, string message)
        {
            ProgressPercentage = progressPercentage;
            Message = message;
        }
    }

    /// <summary>
    /// Event arguments for track mapping completion
    /// </summary>
    public class TrackMappingCompletedEventArgs : EventArgs
    {
        public TrackConfiguration TrackConfiguration { get; }

        public TrackMappingCompletedEventArgs(TrackConfiguration trackConfiguration)
        {
            TrackConfiguration = trackConfiguration;
        }
    }
}
