using System;
using System.Collections.Generic;
using System.Linq;
using LeMansUltimateCoPilot.Models;
using LeMansUltimateCoPilot.Analysis;

namespace LeMansUltimateCoPilot.AI
{
    /// <summary>
    /// Contextual data aggregator that transforms raw telemetry into structured context for LLM consumption
    /// </summary>
    public class ContextualDataAggregator
    {
        private readonly CorneringAnalysisEngine _corneringEngine;
        private readonly List<EnhancedTelemetryData> _telemetryHistory;
        private readonly List<PerformanceEvent> _performanceEvents;
        private readonly Dictionary<string, ReferenceLapData> _referenceLaps;
        
        private const int TELEMETRY_HISTORY_SIZE = 3000; // 30 seconds at 100Hz
        private const int PERFORMANCE_EVENT_RETENTION_SECONDS = 30;

        public ContextualDataAggregator(CorneringAnalysisEngine corneringEngine)
        {
            _corneringEngine = corneringEngine;
            _telemetryHistory = new List<EnhancedTelemetryData>();
            _performanceEvents = new List<PerformanceEvent>();
            _referenceLaps = new Dictionary<string, ReferenceLapData>();
        }

        /// <summary>
        /// Process new telemetry data and generate driving context
        /// </summary>
        public DrivingContext ProcessTelemetryData(EnhancedTelemetryData telemetry)
        {
            // Add to history
            _telemetryHistory.Add(telemetry);
            if (_telemetryHistory.Count > TELEMETRY_HISTORY_SIZE)
            {
                _telemetryHistory.RemoveAt(0);
            }

            // Clean old performance events
            CleanOldPerformanceEvents();

            // Get corner analysis
            var corneringResult = _corneringEngine.ProcessTelemetry(telemetry);
            
            // Analyze performance events
            AnalyzePerformanceEvents(telemetry, corneringResult);

            // Build driving context
            var context = new DrivingContext
            {
                CurrentTelemetry = telemetry,
                CurrentCornerState = new CornerState
                {
                    IsInCorner = corneringResult.IsInCorner,
                    Direction = corneringResult.CornerDirection,
                    Phase = corneringResult.CornerPhase
                },
                ReferenceLapData = GenerateReferenceLapComparison(telemetry),
                RecentEvents = GetRecentPerformanceEvents(),
                CurrentTrack = GenerateTrackContext(telemetry),
                SessionInfo = GenerateSessionContext(telemetry),
                CurrentPerformance = AnalyzeCurrentPerformance()
            };

            return context;
        }

        /// <summary>
        /// Add reference lap data for comparison
        /// </summary>
        public void AddReferenceLap(string trackName, ReferenceLapData referenceLap)
        {
            _referenceLaps[trackName] = referenceLap;
        }

        /// <summary>
        /// Generate reference lap comparison
        /// </summary>
        private ReferenceLapComparison GenerateReferenceLapComparison(EnhancedTelemetryData telemetry)
        {
            // For now, return a placeholder - will be enhanced when we have reference lap data
            var comparison = new ReferenceLapComparison
            {
                SpeedDelta = 0f,
                SpeedDeltaPercent = 0f,
                BrakingPointDelta = 0f,
                RacingLineDeviation = 0f,
                LapTimeDelta = TimeSpan.Zero,
                PerformanceZone = PerformanceZone.Optimal
            };

            // TODO: Implement actual reference lap comparison logic
            // This will compare current position/speed against reference lap data
            
            return comparison;
        }

        /// <summary>
        /// Analyze recent telemetry for performance events
        /// </summary>
        private void AnalyzePerformanceEvents(EnhancedTelemetryData telemetry, CorneringAnalysisResult corneringResult)
        {
            // Analyze harsh braking
            if (telemetry.BrakeInput > 0.9f)
            {
                AddPerformanceEvent(PerformanceEventType.Warning, "Harsh braking detected", 0.7f);
            }

            // Analyze lockup (rapid brake input change)
            if (_telemetryHistory.Count > 5)
            {
                var recent = _telemetryHistory.TakeLast(5).ToList();
                var brakeInputVariation = CalculateInputVariation(recent.Select(d => (double)d.BrakeInput).ToList());
                
                if (brakeInputVariation > 0.3)
                {
                    AddPerformanceEvent(PerformanceEventType.Mistake, "Brake input instability", 0.6f);
                }
            }

            // Analyze corner performance
            if (corneringResult.IsInCorner)
            {
                if (corneringResult.CornerPhase == CornerPhase.Entry && telemetry.Speed > 120f)
                {
                    AddPerformanceEvent(PerformanceEventType.Warning, "High corner entry speed", 0.5f);
                }
            }

            // Analyze coaching feedback
            if (corneringResult.CoachingFeedback != null && corneringResult.CoachingFeedback.Any())
            {
                foreach (var feedback in corneringResult.CoachingFeedback)
                {
                    var severity = feedback.Priority switch
                    {
                        FeedbackPriority.Critical => 1.0f,
                        FeedbackPriority.High => 0.8f,
                        FeedbackPriority.Medium => 0.5f,
                        FeedbackPriority.Low => 0.3f,
                        _ => 0.3f
                    };

                    AddPerformanceEvent(PerformanceEventType.Mistake, feedback.Message, severity);
                }
            }
        }

        /// <summary>
        /// Calculate input variation for stability analysis
        /// </summary>
        private double CalculateInputVariation(List<double> inputData)
        {
            if (inputData.Count < 2) return 0;

            double totalVariation = 0;
            for (int i = 1; i < inputData.Count; i++)
            {
                totalVariation += Math.Abs(inputData[i] - inputData[i - 1]);
            }

            return totalVariation / inputData.Count;
        }

        /// <summary>
        /// Add a performance event
        /// </summary>
        private void AddPerformanceEvent(PerformanceEventType type, string description, float severity)
        {
            // Avoid duplicate events
            var recentSimilar = _performanceEvents
                .Where(e => e.Description == description && e.TimeSinceEvent.TotalSeconds < 5)
                .Any();

            if (!recentSimilar)
            {
                _performanceEvents.Add(new PerformanceEvent
                {
                    Type = type,
                    Description = description,
                    Timestamp = DateTime.Now,
                    Severity = severity
                });
            }
        }

        /// <summary>
        /// Clean old performance events
        /// </summary>
        private void CleanOldPerformanceEvents()
        {
            var cutoff = DateTime.Now.AddSeconds(-PERFORMANCE_EVENT_RETENTION_SECONDS);
            _performanceEvents.RemoveAll(e => e.Timestamp < cutoff);
        }

        /// <summary>
        /// Get recent performance events
        /// </summary>
        private List<PerformanceEvent> GetRecentPerformanceEvents()
        {
            return _performanceEvents
                .OrderByDescending(e => e.Timestamp)
                .Take(10)
                .ToList();
        }

        /// <summary>
        /// Generate track context
        /// </summary>
        private TrackContext GenerateTrackContext(EnhancedTelemetryData telemetry)
        {
            return new TrackContext
            {
                Name = "Le Mans Circuit", // TODO: Detect from telemetry
                CurrentSector = 1, // TODO: Calculate from position
                NextCorner = "Indianapolis", // TODO: Implement corner detection
                DistanceToNextCorner = 200f, // TODO: Calculate from position
                TrackConditions = "Dry" // TODO: Detect from telemetry
            };
        }

        /// <summary>
        /// Generate session context
        /// </summary>
        private SessionContext GenerateSessionContext(EnhancedTelemetryData telemetry)
        {
            return new SessionContext
            {
                CurrentLap = 1, // TODO: Get from telemetry
                SessionTime = TimeSpan.FromSeconds(120), // TODO: Calculate from session start
                SessionType = "Practice", // TODO: Detect from game state
                BestLapTime = null // TODO: Track best lap
            };
        }

        /// <summary>
        /// Analyze current performance level
        /// </summary>
        private PerformanceStatus AnalyzeCurrentPerformance()
        {
            var recentMistakes = _performanceEvents
                .Where(e => e.Type == PerformanceEventType.Mistake && e.TimeSinceEvent.TotalSeconds < 10)
                .Count();

            var recentImprovements = _performanceEvents
                .Where(e => e.Type == PerformanceEventType.Improvement && e.TimeSinceEvent.TotalSeconds < 10)
                .Count();

            var performanceLevel = recentMistakes switch
            {
                0 => PerformanceLevel.Excellent,
                1 => PerformanceLevel.Good,
                2 => PerformanceLevel.Average,
                3 => PerformanceLevel.Below,
                _ => PerformanceLevel.Struggling
            };

            var status = new PerformanceStatus
            {
                Level = performanceLevel,
                ConsistencyScore = CalculateConsistencyScore(),
                ImprovementRate = CalculateImprovementRate()
            };

            // Analyze strengths and weaknesses
            AnalyzeStrengthsAndWeaknesses(status);

            return status;
        }

        /// <summary>
        /// Calculate consistency score based on recent performance
        /// </summary>
        private float CalculateConsistencyScore()
        {
            if (_telemetryHistory.Count < 100) return 0.5f;

            var recent = _telemetryHistory.TakeLast(100).ToList();
            var speedVariation = CalculateInputVariation(recent.Select(d => (double)d.Speed).ToList());
            var brakeVariation = CalculateInputVariation(recent.Select(d => (double)d.BrakeInput).ToList());

            // Lower variation = higher consistency
            var consistencyScore = 1.0f - Math.Min(1.0f, (float)(speedVariation / 50.0 + brakeVariation));
            return Math.Max(0.0f, consistencyScore);
        }

        /// <summary>
        /// Calculate improvement rate based on recent trends
        /// </summary>
        private float CalculateImprovementRate()
        {
            var recentImprovements = _performanceEvents
                .Where(e => e.Type == PerformanceEventType.Improvement)
                .Count();

            var recentMistakes = _performanceEvents
                .Where(e => e.Type == PerformanceEventType.Mistake)
                .Count();

            if (recentMistakes == 0 && recentImprovements == 0) return 0.0f;

            return (float)recentImprovements / (recentImprovements + recentMistakes);
        }

        /// <summary>
        /// Analyze current strengths and weaknesses
        /// </summary>
        private void AnalyzeStrengthsAndWeaknesses(PerformanceStatus status)
        {
            // Analyze braking performance
            var recentBrakingEvents = _performanceEvents
                .Where(e => e.Description.Contains("brak", StringComparison.OrdinalIgnoreCase))
                .Count();

            if (recentBrakingEvents == 0)
            {
                status.CurrentStrengths.Add("Smooth braking");
            }
            else if (recentBrakingEvents > 2)
            {
                status.CurrentWeaknesses.Add("Braking technique");
            }

            // Analyze cornering performance
            var recentCorneringEvents = _performanceEvents
                .Where(e => e.Description.Contains("corner", StringComparison.OrdinalIgnoreCase))
                .Count();

            if (recentCorneringEvents == 0)
            {
                status.CurrentStrengths.Add("Corner execution");
            }
            else if (recentCorneringEvents > 2)
            {
                status.CurrentWeaknesses.Add("Corner technique");
            }

            // Analyze consistency
            if (status.ConsistencyScore > 0.8f)
            {
                status.CurrentStrengths.Add("Consistent driving");
            }
            else if (status.ConsistencyScore < 0.5f)
            {
                status.CurrentWeaknesses.Add("Consistency");
            }
        }
    }

    /// <summary>
    /// Reference lap data structure (placeholder for future implementation)
    /// </summary>
    public class ReferenceLapData
    {
        public required string TrackName { get; set; }
        public TimeSpan LapTime { get; set; }
        public List<ReferencePoint> ReferencePoints { get; set; }

        public ReferenceLapData()
        {
            ReferencePoints = new List<ReferencePoint>();
        }
    }

    /// <summary>
    /// Reference point data (placeholder for future implementation)
    /// </summary>
    public class ReferencePoint
    {
        public float Distance { get; set; }
        public float Speed { get; set; }
        public float BrakeInput { get; set; }
        public float ThrottleInput { get; set; }
        public float LateralG { get; set; }
    }
}
