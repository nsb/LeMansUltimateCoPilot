using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Net.Http;
using LeMansUltimateCoPilot.Models;

namespace LeMansUltimateCoPilot.Services
{
    /// <summary>
    /// Service for generating coaching messages using Large Language Models
    /// Provides contextual, intelligent coaching based on telemetry analysis
    /// </summary>
    public class LLMCoachingService
    {
        private readonly HttpClient _httpClient;
        private readonly LLMConfiguration _config;
        private TrackConfiguration? _currentTrack;
        private readonly StringBuilder _conversationHistory = new();

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="config">LLM configuration</param>
        public LLMCoachingService(LLMConfiguration config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_config.ApiKey}");
        }

        /// <summary>
        /// Set the current track context
        /// </summary>
        /// <param name="track">Track configuration</param>
        public virtual void SetTrackContext(TrackConfiguration track)
        {
            _currentTrack = track;
        }

        /// <summary>
        /// Generate a coaching message based on current context
        /// </summary>
        /// <param name="context">Current coaching context</param>
        /// <param name="recentContext">Recent coaching history</param>
        /// <param name="style">Coaching style preference</param>
        /// <returns>Generated coaching message</returns>
        public virtual async Task<CoachingMessage> GenerateCoachingMessageAsync(
            CoachingContext context, 
            List<CoachingContext> recentContext, 
            CoachingStyle style)
        {
            var prompt = BuildCoachingPrompt(context, recentContext, style);
            var response = await CallLLMAsync(prompt);
            
            return new CoachingMessage
            {
                Content = response,
                Type = DetermineMessageType(context),
                Priority = DeterminePriority(context),
                RelatedSegment = context.Segment
            };
        }

        /// <summary>
        /// Generate a lap summary message
        /// </summary>
        /// <param name="metrics">Current lap metrics</param>
        /// <param name="recentContext">Recent coaching context</param>
        /// <returns>Lap summary message</returns>
        public virtual async Task<string> GenerateLapSummaryAsync(
            RealTimeComparisonMetrics metrics, 
            List<CoachingContext> recentContext)
        {
            var prompt = BuildLapSummaryPrompt(metrics, recentContext);
            return await CallLLMAsync(prompt);
        }

        /// <summary>
        /// Generate a session summary message
        /// </summary>
        /// <param name="metrics">Session metrics</param>
        /// <param name="recentContext">Full session context</param>
        /// <returns>Session summary message</returns>
        public virtual async Task<string> GenerateSessionSummaryAsync(
            RealTimeComparisonMetrics metrics, 
            List<CoachingContext> recentContext)
        {
            var prompt = BuildSessionSummaryPrompt(metrics, recentContext);
            return await CallLLMAsync(prompt);
        }

        /// <summary>
        /// Build coaching prompt for LLM
        /// </summary>
        private string BuildCoachingPrompt(
            CoachingContext context, 
            List<CoachingContext> recentContext, 
            CoachingStyle style)
        {
            var sb = new StringBuilder();
            
            // System prompt
            sb.AppendLine("You are an expert racing driving coach providing real-time feedback during a racing session.");
            sb.AppendLine("Your job is to help the driver improve their lap times by analyzing telemetry data.");
            sb.AppendLine("Provide concise, actionable coaching that can be delivered via voice during driving.");
            sb.AppendLine();
            
            // Style instructions
            sb.AppendLine($"Coaching Style: {GetStyleInstructions(style)}");
            sb.AppendLine();
            
            // Track context
            if (_currentTrack != null)
            {
                sb.AppendLine($"Track: {_currentTrack.TrackName}");
                sb.AppendLine($"Track Length: {_currentTrack.TrackLength:F0}m");
                sb.AppendLine();
            }
            
            // Current segment context
            if (context.Segment != null)
            {
                sb.AppendLine($"Current Segment: {context.Segment.SegmentType} (Segment {context.Segment.SegmentNumber})");
                sb.AppendLine($"Segment Description: {context.Segment.Description}");
                sb.AppendLine($"Difficulty: {context.Segment.DifficultyRating}/10");
                sb.AppendLine();
            }
            
            // Current performance
            sb.AppendLine("Current Performance Analysis:");
            sb.AppendLine($"Time Delta: {context.TimeDelta:F3}s ({(context.TimeDelta > 0 ? "slower" : "faster")} than reference)");
            sb.AppendLine($"Speed Delta: {context.SpeedDelta:F1} km/h");
            sb.AppendLine($"Current Speed: {context.CurrentTelemetry.Speed:F1} km/h");
            sb.AppendLine($"Reference Speed: {context.ReferenceTelemetry.Speed:F1} km/h");
            sb.AppendLine($"Throttle: {context.CurrentTelemetry.ThrottleInput:F1}% (ref: {context.ReferenceTelemetry.ThrottleInput:F1}%)");
            sb.AppendLine($"Brake: {context.CurrentTelemetry.BrakeInput:F1}% (ref: {context.ReferenceTelemetry.BrakeInput:F1}%)");
            sb.AppendLine();
            
            // Improvement areas
            if (context.ImprovementAreas.Any())
            {
                sb.AppendLine("Identified Improvement Areas:");
                foreach (var area in context.ImprovementAreas.OrderByDescending(a => a.Severity))
                {
                    sb.AppendLine($"- {area.Type}: {area.Message} (Severity: {area.Severity:F0}%, Potential Gain: {area.PotentialGain:F3}s)");
                }
                sb.AppendLine();
            }
            
            // Recent context (last 3 significant events)
            var recentSignificant = recentContext
                .Where(c => c.ImprovementAreas.Any(a => a.Severity > 40))
                .TakeLast(3)
                .ToList();
            
            if (recentSignificant.Any())
            {
                sb.AppendLine("Recent Performance Trends:");
                foreach (var recent in recentSignificant)
                {
                    sb.AppendLine($"- {recent.Segment?.SegmentType ?? TrackSegmentType.Straight}: {recent.TimeDelta:F3}s delta");
                }
                sb.AppendLine();
            }
            
            // Instructions
            sb.AppendLine("Generate a concise coaching message (max 20 words) that:");
            sb.AppendLine("1. Addresses the most critical improvement area");
            sb.AppendLine("2. Provides specific, actionable advice");
            sb.AppendLine("3. Is appropriate for voice delivery while driving");
            sb.AppendLine("4. Maintains the specified coaching style");
            sb.AppendLine("5. Focuses on the most impactful improvement");
            sb.AppendLine();
            sb.AppendLine("Coaching Message:");
            
            return sb.ToString();
        }

        /// <summary>
        /// Build lap summary prompt
        /// </summary>
        private string BuildLapSummaryPrompt(
            RealTimeComparisonMetrics metrics, 
            List<CoachingContext> recentContext)
        {
            var sb = new StringBuilder();
            
            sb.AppendLine("Provide a brief lap summary for the driver:");
            sb.AppendLine($"Lap Time Delta: {metrics.CurrentLapTimeDelta:F3}s");
            sb.AppendLine($"Consistency Rating: {metrics.ConsistencyRating:F1}%");
            sb.AppendLine($"Performance Rating: {metrics.PerformanceRating:F1}%");
            
            if (metrics.ProblematicSegments.Any())
            {
                sb.AppendLine("Problem areas: " + string.Join(", ", metrics.ProblematicSegments.Select(s => s.SegmentType.ToString())));
            }
            
            if (metrics.StrongSegments.Any())
            {
                sb.AppendLine("Strong areas: " + string.Join(", ", metrics.StrongSegments.Select(s => s.SegmentType.ToString())));
            }
            
            sb.AppendLine("Generate a 15-word summary focusing on key improvements for the next lap.");
            
            return sb.ToString();
        }

        /// <summary>
        /// Build session summary prompt
        /// </summary>
        private string BuildSessionSummaryPrompt(
            RealTimeComparisonMetrics metrics, 
            List<CoachingContext> recentContext)
        {
            var sb = new StringBuilder();
            
            sb.AppendLine("Provide a session summary for the driver:");
            sb.AppendLine($"Completed Laps: {metrics.SessionStats.LapsCompleted}");
            sb.AppendLine($"Best Lap Delta: {metrics.SessionStats.BestLapTimeDelta:F3}s");
            sb.AppendLine($"Average Lap Delta: {metrics.SessionStats.AverageLapTimeDelta:F3}s");
            sb.AppendLine($"Consistency: {metrics.ConsistencyRating:F1}%");
            
            sb.AppendLine("Generate a 25-word session summary highlighting progress and key focus areas.");
            
            return sb.ToString();
        }

        /// <summary>
        /// Get style-specific instructions
        /// </summary>
        private string GetStyleInstructions(CoachingStyle style)
        {
            return style switch
            {
                CoachingStyle.Encouraging => "Be positive and motivating. Focus on progress and potential. Use encouraging language.",
                CoachingStyle.Technical => "Provide precise technical feedback. Use racing terminology and specific data points.",
                CoachingStyle.Concise => "Keep messages very short and direct. Focus on immediate actions only.",
                CoachingStyle.Detailed => "Provide comprehensive feedback with explanations of why improvements matter.",
                _ => "Provide clear, actionable coaching feedback."
            };
        }

        /// <summary>
        /// Determine message type based on context
        /// </summary>
        private CoachingMessageType DetermineMessageType(CoachingContext context)
        {
            if (context.ImprovementAreas.Any(a => a.Severity > 70))
                return CoachingMessageType.RealTimeCorrection;
            
            if (context.TimeDelta < -0.2)
                return CoachingMessageType.Encouragement;
            
            return CoachingMessageType.RealTimeCorrection;
        }

        /// <summary>
        /// Determine message priority based on context
        /// </summary>
        private CoachingPriority DeterminePriority(CoachingContext context)
        {
            var maxSeverity = context.ImprovementAreas.Any() ? 
                context.ImprovementAreas.Max(a => a.Severity) : 0;
            
            return maxSeverity switch
            {
                > 80 => CoachingPriority.High,
                > 50 => CoachingPriority.Medium,
                _ => CoachingPriority.Low
            };
        }

        /// <summary>
        /// Call the LLM API
        /// </summary>
        private async Task<string> CallLLMAsync(string prompt)
        {
            try
            {
                var requestBody = new
                {
                    model = _config.Model,
                    messages = new[]
                    {
                        new { role = "user", content = prompt }
                    },
                    max_tokens = _config.MaxTokens,
                    temperature = _config.Temperature
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(_config.ApiEndpoint, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    return result.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "";
                }
                else
                {
                    // Fallback to basic coaching if LLM fails
                    return GenerateFallbackCoaching(prompt);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LLM API Error: {ex.Message}");
                return GenerateFallbackCoaching(prompt);
            }
        }

        /// <summary>
        /// Generate fallback coaching when LLM is unavailable
        /// </summary>
        private string GenerateFallbackCoaching(string prompt)
        {
            // Simple pattern-based fallback
            if (prompt.Contains("Speed Delta") && prompt.Contains("slower"))
                return "Carry more speed through this section";
            if (prompt.Contains("BrakingPressure"))
                return "Try lighter braking pressure";
            if (prompt.Contains("ThrottleApplication"))
                return "Apply throttle earlier";
            
            return "Focus on consistency and smooth inputs";
        }

        /// <summary>
        /// Dispose resources
        /// </summary>
        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }

    /// <summary>
    /// Configuration for LLM service
    /// </summary>
    public class LLMConfiguration
    {
        /// <summary>
        /// API endpoint URL
        /// </summary>
        public string ApiEndpoint { get; set; } = "https://api.openai.com/v1/chat/completions";

        /// <summary>
        /// API key for authentication
        /// </summary>
        public string ApiKey { get; set; } = "";

        /// <summary>
        /// Model to use (e.g., "gpt-4", "gpt-3.5-turbo")
        /// </summary>
        public string Model { get; set; } = "gpt-3.5-turbo";

        /// <summary>
        /// Maximum tokens in response
        /// </summary>
        public int MaxTokens { get; set; } = 50;

        /// <summary>
        /// Temperature for response creativity (0.0 - 1.0)
        /// </summary>
        public double Temperature { get; set; } = 0.3;
    }
}
