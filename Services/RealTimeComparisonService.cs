using System;
using System.Collections.Generic;
using System.Linq;
using LeMansUltimateCoPilot.Models;

namespace LeMansUltimateCoPilot.Services
{
    /// <summary>
    /// Service for real-time comparison of current telemetry with reference lap data
    /// Provides performance analysis, coaching recommendations, and improvement identification
    /// </summary>
    public class RealTimeComparisonService
    {
        private readonly TrackMapper _trackMapper;
        private ReferenceLap? _currentReferenceLap;
        private TrackConfiguration? _currentTrackConfiguration;
        private readonly List<ComparisonResult> _currentLapComparisons = new();
        private readonly RealTimeComparisonMetrics _metrics = new();
        private readonly Dictionary<double, EnhancedTelemetryData> _referenceTelemetryByDistance = new();
        private double _lastProcessedDistance = 0;

        /// <summary>
        /// Tolerance for distance matching in meters
        /// </summary>
        public double DistanceTolerance { get; set; } = 10.0;

        /// <summary>
        /// Minimum confidence level for valid comparisons
        /// </summary>
        public double MinimumConfidence { get; set; } = 50.0;

        /// <summary>
        /// Event raised when a new comparison result is available
        /// </summary>
        public event EventHandler<ComparisonResult>? ComparisonUpdated;

        /// <summary>
        /// Event raised when metrics are updated
        /// </summary>
        public event EventHandler<RealTimeComparisonMetrics>? MetricsUpdated;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="trackMapper">Track mapping service</param>
        public RealTimeComparisonService(TrackMapper trackMapper)
        {
            _trackMapper = trackMapper ?? throw new ArgumentNullException(nameof(trackMapper));
        }

        /// <summary>
        /// Set the reference lap for comparison
        /// </summary>
        /// <param name="referenceLap">Reference lap to compare against</param>
        /// <param name="trackConfiguration">Track configuration for segmentation</param>
        public void SetReferenceLap(ReferenceLap referenceLap, TrackConfiguration? trackConfiguration = null)
        {
            _currentReferenceLap = referenceLap ?? throw new ArgumentNullException(nameof(referenceLap));
            _currentTrackConfiguration = trackConfiguration;
            
            // Build distance-indexed reference telemetry for fast lookup
            _referenceTelemetryByDistance.Clear();
            foreach (var telemetry in referenceLap.TelemetryData)
            {
                var distance = telemetry.DistanceFromStart;
                _referenceTelemetryByDistance[distance] = telemetry;
            }

            // Initialize metrics
            _metrics.ReferenceLap = referenceLap;
            _metrics.SessionStats.SessionStarted = DateTime.Now;
            
            // Reset lap comparisons
            _currentLapComparisons.Clear();
            _lastProcessedDistance = 0;
        }

        /// <summary>
        /// Process current telemetry and generate comparison results
        /// </summary>
        /// <param name="currentTelemetry">Current telemetry data</param>
        /// <returns>Comparison result or null if no valid comparison available</returns>
        public ComparisonResult? ProcessTelemetry(EnhancedTelemetryData currentTelemetry)
        {
            if (_currentReferenceLap == null)
                return null;

            var referenceTelemetry = FindClosestReferenceTelemetry(currentTelemetry.DistanceFromStart);
            if (referenceTelemetry == null)
                return null;

            var comparisonResult = CreateComparisonResult(currentTelemetry, referenceTelemetry);
            
            // Only process if we've moved forward significantly
            if (Math.Abs(currentTelemetry.DistanceFromStart - _lastProcessedDistance) > DistanceTolerance)
            {
                _currentLapComparisons.Add(comparisonResult);
                _lastProcessedDistance = currentTelemetry.DistanceFromStart;
                
                // Update metrics
                UpdateMetrics(comparisonResult);
                
                // Raise events
                ComparisonUpdated?.Invoke(this, comparisonResult);
                MetricsUpdated?.Invoke(this, _metrics);
            }

            return comparisonResult;
        }

        /// <summary>
        /// Complete the current lap and calculate final metrics
        /// </summary>
        /// <param name="lapTime">Final lap time</param>
        public void CompleteLap(double lapTime)
        {
            if (_currentReferenceLap == null) return;

            var lapTimeDelta = lapTime - _currentReferenceLap.LapTime;
            
            // Update session statistics
            _metrics.SessionStats.UpdateWithLapData(lapTimeDelta);
            _metrics.CurrentLapTimeDelta = lapTimeDelta;
            
            // Calculate segment time deltas
            UpdateSegmentTimeDeltas();
            
            // Update consistency rating
            _metrics.ConsistencyRating = _metrics.CalculateConsistencyRating();
            
            // Update performance rating
            _metrics.PerformanceRating = _metrics.CalculateOverallPerformance();
            
            // Reset for next lap
            _currentLapComparisons.Clear();
            _lastProcessedDistance = 0;
            
            MetricsUpdated?.Invoke(this, _metrics);
        }

        /// <summary>
        /// Get current comparison metrics
        /// </summary>
        /// <returns>Current metrics</returns>
        public RealTimeComparisonMetrics GetCurrentMetrics()
        {
            return _metrics;
        }

        /// <summary>
        /// Get comparison results for the current lap
        /// </summary>
        /// <returns>List of comparison results</returns>
        public List<ComparisonResult> GetCurrentLapComparisons()
        {
            return _currentLapComparisons.ToList();
        }

        /// <summary>
        /// Find the closest reference telemetry point by distance
        /// </summary>
        /// <param name="distance">Current distance from start</param>
        /// <returns>Closest reference telemetry or null if none found</returns>
        private EnhancedTelemetryData? FindClosestReferenceTelemetry(double distance)
        {
            if (!_referenceTelemetryByDistance.Any())
                return null;

            // Find the closest distance key
            var closestDistance = _referenceTelemetryByDistance.Keys
                .OrderBy(d => Math.Abs(d - distance))
                .FirstOrDefault();

            // Check if within tolerance
            if (Math.Abs(closestDistance - distance) > DistanceTolerance)
                return null;

            return _referenceTelemetryByDistance[closestDistance];
        }

        /// <summary>
        /// Create a comparison result from current and reference telemetry
        /// </summary>
        /// <param name="current">Current telemetry data</param>
        /// <param name="reference">Reference telemetry data</param>
        /// <returns>Comparison result</returns>
        private ComparisonResult CreateComparisonResult(EnhancedTelemetryData current, EnhancedTelemetryData reference)
        {
            var result = new ComparisonResult
            {
                CurrentTelemetry = current,
                ReferenceTelemetry = reference,
                DistanceFromStart = current.DistanceFromStart,
                TimeDelta = current.LapTime - reference.LapTime,
                SpeedDelta = current.Speed - reference.Speed,
                ThrottleDelta = current.ThrottleInput - reference.ThrottleInput,
                BrakeDelta = current.BrakeInput - reference.BrakeInput,
                SteeringDelta = current.SteeringInput - reference.SteeringInput,
                LongitudinalGDelta = current.LongitudinalG - reference.LongitudinalG,
                LateralGDelta = current.LateralG - reference.LateralG,
                ConfidenceLevel = CalculateConfidenceLevel(current, reference)
            };

            // Find associated track segment
            if (_currentTrackConfiguration != null)
            {
                result.Segment = _currentTrackConfiguration.Segments
                    .FirstOrDefault(s => IsDistanceInSegment(current.DistanceFromStart, s));
            }

            // Analyze improvement areas
            result.ImprovementAreas = AnalyzeImprovementAreas(current, reference, result.Segment);

            return result;
        }

        /// <summary>
        /// Calculate confidence level for a comparison
        /// </summary>
        /// <param name="current">Current telemetry</param>
        /// <param name="reference">Reference telemetry</param>
        /// <returns>Confidence level (0-100)</returns>
        private double CalculateConfidenceLevel(EnhancedTelemetryData current, EnhancedTelemetryData reference)
        {
            double confidence = 100.0;

            // Reduce confidence based on distance difference
            var distanceDiff = Math.Abs(current.DistanceFromStart - reference.DistanceFromStart);
            confidence -= (distanceDiff / DistanceTolerance) * 20;

            // Reduce confidence based on speed difference (might indicate different conditions)
            var speedDiff = Math.Abs(current.Speed - reference.Speed);
            if (speedDiff > 20) confidence -= 10;

            // Reduce confidence based on track conditions differences
            if (!string.Equals(current.TrackCondition, reference.TrackCondition, StringComparison.OrdinalIgnoreCase))
                confidence -= 15;

            return Math.Max(0, Math.Min(100, confidence));
        }

        /// <summary>
        /// Analyze improvement areas based on telemetry comparison
        /// </summary>
        /// <param name="current">Current telemetry</param>
        /// <param name="reference">Reference telemetry</param>
        /// <param name="segment">Current track segment</param>
        /// <returns>List of improvement areas</returns>
        private List<ImprovementArea> AnalyzeImprovementAreas(EnhancedTelemetryData current, EnhancedTelemetryData reference, TrackSegment? segment)
        {
            var improvements = new List<ImprovementArea>();

            // Speed-based improvements
            if (current.Speed < reference.Speed - 5) // 5 km/h threshold
            {
                improvements.Add(new ImprovementArea
                {
                    Type = ImprovementType.CornerSpeed,
                    Severity = Math.Min(100, (reference.Speed - current.Speed) / reference.Speed * 100),
                    Message = $"Carry {reference.Speed - current.Speed:F1} km/h more speed through this section",
                    PotentialGain = EstimateTimeGain(reference.Speed - current.Speed, 100), // Rough estimate
                    DistanceRange = (current.DistanceFromStart - 50, current.DistanceFromStart + 50)
                });
            }

            // Braking analysis
            if (current.BrakeInput > reference.BrakeInput + 10) // 10% threshold
            {
                improvements.Add(new ImprovementArea
                {
                    Type = ImprovementType.BrakingPressure,
                    Severity = Math.Min(100, (current.BrakeInput - reference.BrakeInput)),
                    Message = $"Reduce braking pressure by {current.BrakeInput - reference.BrakeInput:F1}%",
                    PotentialGain = EstimateTimeGain(current.BrakeInput - reference.BrakeInput, 50),
                    DistanceRange = (current.DistanceFromStart - 30, current.DistanceFromStart + 30)
                });
            }

            // Throttle analysis
            if (current.ThrottleInput < reference.ThrottleInput - 10) // 10% threshold
            {
                improvements.Add(new ImprovementArea
                {
                    Type = ImprovementType.ThrottleApplication,
                    Severity = Math.Min(100, (reference.ThrottleInput - current.ThrottleInput)),
                    Message = $"Apply {reference.ThrottleInput - current.ThrottleInput:F1}% more throttle",
                    PotentialGain = EstimateTimeGain(reference.ThrottleInput - current.ThrottleInput, 75),
                    DistanceRange = (current.DistanceFromStart - 30, current.DistanceFromStart + 30)
                });
            }

            // Steering smoothness
            var steeringDiff = Math.Abs(current.SteeringInput - reference.SteeringInput);
            if (steeringDiff > 15) // 15% threshold
            {
                improvements.Add(new ImprovementArea
                {
                    Type = ImprovementType.SteeringSmoothing,
                    Severity = Math.Min(100, steeringDiff),
                    Message = "Smooth steering inputs for better stability",
                    PotentialGain = EstimateTimeGain(steeringDiff, 25),
                    DistanceRange = (current.DistanceFromStart - 20, current.DistanceFromStart + 20)
                });
            }

            return improvements;
        }

        /// <summary>
        /// Estimate time gain from an improvement
        /// </summary>
        /// <param name="improvementMagnitude">Magnitude of improvement</param>
        /// <param name="impactFactor">Impact factor (0-100)</param>
        /// <returns>Estimated time gain in seconds</returns>
        private double EstimateTimeGain(double improvementMagnitude, double impactFactor)
        {
            // Simple heuristic: larger improvements and higher impact = more time gain
            return (improvementMagnitude / 100.0) * (impactFactor / 100.0) * 0.1; // Max 0.1 seconds per improvement
        }

        /// <summary>
        /// Check if a distance falls within a track segment
        /// </summary>
        /// <param name="distance">Distance to check</param>
        /// <param name="segment">Track segment</param>
        /// <returns>True if distance is within segment</returns>
        private bool IsDistanceInSegment(double distance, TrackSegment segment)
        {
            var segmentStart = segment.DistanceFromStart;
            var segmentEnd = segmentStart + segment.SegmentLength;
            return distance >= segmentStart && distance <= segmentEnd;
        }

        /// <summary>
        /// Update metrics with new comparison result
        /// </summary>
        /// <param name="result">New comparison result</param>
        private void UpdateMetrics(ComparisonResult result)
        {
            _metrics.LastUpdated = DateTime.Now;
            
            // Update active improvements
            _metrics.ActiveImprovements.AddRange(result.ImprovementAreas);
            
            // Keep only the most recent improvements (last 1000 meters)
            var currentDistance = result.DistanceFromStart;
            _metrics.ActiveImprovements.RemoveAll(i => 
                i.DistanceRange.End < currentDistance - 1000);
            
            // Update problematic segments
            if (result.TimeDelta > 0.1 && result.Segment != null) // Losing more than 0.1 seconds
            {
                if (!_metrics.ProblematicSegments.Any(s => s.Id == result.Segment.Id))
                {
                    _metrics.ProblematicSegments.Add(result.Segment);
                }
            }
            
            // Update strong segments
            if (result.TimeDelta < -0.1 && result.Segment != null) // Gaining more than 0.1 seconds
            {
                if (!_metrics.StrongSegments.Any(s => s.Id == result.Segment.Id))
                {
                    _metrics.StrongSegments.Add(result.Segment);
                }
            }
        }

        /// <summary>
        /// Update segment time deltas based on current lap comparisons
        /// </summary>
        private void UpdateSegmentTimeDeltas()
        {
            if (_currentTrackConfiguration == null) return;

            foreach (var segment in _currentTrackConfiguration.Segments)
            {
                var segmentComparisons = _currentLapComparisons
                    .Where(c => c.Segment != null && c.Segment.Id == segment.Id)
                    .ToList();

                if (segmentComparisons.Any())
                {
                    var avgDelta = segmentComparisons.Average(c => c.TimeDelta);
                    _metrics.SegmentTimeDeltas[segment.SegmentNumber] = avgDelta;
                    
                    // Update best/worst segment times
                    if (!_metrics.BestSegmentTimes.ContainsKey(segment.SegmentNumber) || 
                        avgDelta < _metrics.BestSegmentTimes[segment.SegmentNumber])
                    {
                        _metrics.BestSegmentTimes[segment.SegmentNumber] = avgDelta;
                    }
                    
                    if (!_metrics.WorstSegmentTimes.ContainsKey(segment.SegmentNumber) || 
                        avgDelta > _metrics.WorstSegmentTimes[segment.SegmentNumber])
                    {
                        _metrics.WorstSegmentTimes[segment.SegmentNumber] = avgDelta;
                    }
                }
            }
        }
    }
}
