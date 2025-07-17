using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using LeMansUltimateCoPilot.Models;

namespace LeMansUltimateCoPilot.AI
{
    /// <summary>
    /// AI-powered coaching service that provides real-time driving feedback using LLM models
    /// </summary>
    public class AICoachingService
    {
        private readonly Kernel _kernel;
        private readonly IChatCompletionService _chatCompletion;
        private readonly List<ChatHistory> _conversationHistory;
        private readonly CoachingConfiguration _config;
        
        private const int MAX_CONVERSATION_HISTORY = 10;
        private const int MAX_RESPONSE_TOKENS = 150;

        public AICoachingService(CoachingConfiguration config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _conversationHistory = new List<ChatHistory>();
            
            // Initialize Semantic Kernel
            var builder = Kernel.CreateBuilder();
            
            // Configure the LLM provider based on configuration
            switch (_config.Provider)
            {
                case LLMProvider.OpenAI:
                    builder.AddOpenAIChatCompletion(_config.ModelName, _config.ApiKey);
                    break;
                case LLMProvider.AzureOpenAI:
                    builder.AddAzureOpenAIChatCompletion(_config.ModelName, _config.Endpoint, _config.ApiKey);
                    break;
                case LLMProvider.Local:
                    // For local models like Ollama, we'll need a custom connector
                    throw new NotImplementedException("Local LLM support coming soon");
                default:
                    throw new ArgumentException($"Unsupported LLM provider: {_config.Provider}");
            }

            _kernel = builder.Build();
            _chatCompletion = _kernel.GetRequiredService<IChatCompletionService>();
        }

        /// <summary>
        /// Generate real-time coaching feedback based on driving context
        /// </summary>
        public async Task<CoachingResponse> GenerateCoachingAsync(DrivingContext context)
        {
            try
            {
                // Build the system prompt with coaching personality and rules
                var systemPrompt = BuildSystemPrompt();
                
                // Create context-aware user prompt
                var userPrompt = BuildUserPrompt(context);
                
                // Create chat history for this interaction
                var chatHistory = new ChatHistory(systemPrompt);
                chatHistory.AddUserMessage(userPrompt);
                
                // Generate response with configured parameters
                var response = await _chatCompletion.GetChatMessageContentAsync(
                    chatHistory,
                    new OpenAIPromptExecutionSettings
                    {
                        MaxTokens = MAX_RESPONSE_TOKENS,
                        Temperature = _config.Temperature,
                        TopP = _config.TopP,
                        FrequencyPenalty = 0.1,
                        PresencePenalty = 0.1
                    });

                // Parse and structure the response
                var coachingResponse = ParseCoachingResponse(response.Content ?? string.Empty, context);
                
                // Store conversation history for context continuity
                StoreConversationHistory(chatHistory, response.Content);
                
                return coachingResponse;
            }
            catch (Exception ex)
            {
                // Return fallback response on error
                return new CoachingResponse
                {
                    Message = "Keep focusing on smooth inputs and consistency.",
                    Priority = CoachingPriority.Low,
                    Category = CoachingCategory.General,
                    Timestamp = DateTime.Now,
                    IsError = true,
                    ErrorDetails = ex.Message
                };
            }
        }

        /// <summary>
        /// Build system prompt that defines the AI coach personality and rules
        /// </summary>
        private string BuildSystemPrompt()
        {
            var prompt = new StringBuilder();
            
            prompt.AppendLine("You are an expert racing coach providing real-time feedback to a driver in Le Mans Ultimate (rFactor2).");
            prompt.AppendLine("Your role is to provide concise, actionable advice that helps improve lap times and driving technique.");
            prompt.AppendLine();
            prompt.AppendLine("COACHING GUIDELINES:");
            prompt.AppendLine("- Keep responses under 20 words for real-time delivery");
            prompt.AppendLine("- Focus on ONE specific improvement per message");
            prompt.AppendLine("- Use encouraging and professional tone");
            prompt.AppendLine("- Prioritize safety and car control over pure speed");
            prompt.AppendLine("- Consider the driver's current performance level");
            prompt.AppendLine();
            prompt.AppendLine("FEEDBACK CATEGORIES:");
            prompt.AppendLine("- BRAKING: Brake points, pressure, trail braking");
            prompt.AppendLine("- CORNERING: Entry speed, apex, exit technique");
            prompt.AppendLine("- THROTTLE: Application timing, progressive inputs");
            prompt.AppendLine("- CONSISTENCY: Smooth inputs, predictable lap times");
            prompt.AppendLine("- RACECRAFT: Positioning, awareness, raceline");
            prompt.AppendLine();
            prompt.AppendLine("RESPONSE FORMAT:");
            prompt.AppendLine("Provide direct coaching advice. Examples:");
            prompt.AppendLine("- 'Brake 50m earlier into Turn 1'");
            prompt.AppendLine("- 'More gradual throttle application on exit'");
            prompt.AppendLine("- 'Apex is 2 meters later in this corner'");
            prompt.AppendLine("- 'Excellent consistency, maintain this rhythm'");

            return prompt.ToString();
        }

        /// <summary>
        /// Build user prompt with current driving context
        /// </summary>
        private string BuildUserPrompt(DrivingContext context)
        {
            var prompt = new StringBuilder();
            
            // Add natural language context
            prompt.AppendLine("CURRENT DRIVING CONTEXT:");
            prompt.AppendLine(context.ToNaturalLanguage());
            prompt.AppendLine();
            
            // Add recent events for context
            if (context.RecentEvents.Any())
            {
                prompt.AppendLine("RECENT EVENTS:");
                foreach (var evt in context.RecentEvents.Take(3))
                {
                    prompt.AppendLine($"- {evt.Description} (Severity: {evt.Severity:F1})");
                }
                prompt.AppendLine();
            }
            
            // Add performance trends
            prompt.AppendLine("PERFORMANCE STATUS:");
            prompt.AppendLine($"- Level: {context.CurrentPerformance.Level}");
            prompt.AppendLine($"- Consistency: {context.CurrentPerformance.ConsistencyScore:F1}");
            
            if (context.CurrentPerformance.CurrentStrengths.Any())
            {
                prompt.AppendLine($"- Strengths: {string.Join(", ", context.CurrentPerformance.CurrentStrengths)}");
            }
            
            if (context.CurrentPerformance.CurrentWeaknesses.Any())
            {
                prompt.AppendLine($"- Areas for improvement: {string.Join(", ", context.CurrentPerformance.CurrentWeaknesses)}");
            }
            
            prompt.AppendLine();
            prompt.AppendLine("Provide specific, actionable coaching advice based on this context:");
            
            return prompt.ToString();
        }

        /// <summary>
        /// Parse LLM response into structured coaching response
        /// </summary>
        private CoachingResponse ParseCoachingResponse(string response, DrivingContext context)
        {
            var coaching = new CoachingResponse
            {
                Message = response?.Trim() ?? "Keep up the good work!",
                Timestamp = DateTime.Now,
                IsError = false
            };

            // Determine priority based on context
            if (context.RecentEvents.Any(e => e.Severity > 0.7f))
            {
                coaching.Priority = CoachingPriority.High;
            }
            else if (context.RecentEvents.Any(e => e.Severity > 0.4f))
            {
                coaching.Priority = CoachingPriority.Medium;
            }
            else
            {
                coaching.Priority = CoachingPriority.Low;
            }

            // Categorize response based on content
            coaching.Category = CategorizeResponse(response ?? string.Empty);

            return coaching;
        }

        /// <summary>
        /// Categorize coaching response based on content
        /// </summary>
        private CoachingCategory CategorizeResponse(string response)
        {
            if (string.IsNullOrEmpty(response)) return CoachingCategory.General;

            var lowerResponse = response.ToLower();
            
            if (lowerResponse.Contains("brake") || lowerResponse.Contains("braking"))
                return CoachingCategory.Braking;
            
            if (lowerResponse.Contains("corner") || lowerResponse.Contains("apex") || lowerResponse.Contains("turn"))
                return CoachingCategory.Cornering;
            
            if (lowerResponse.Contains("throttle") || lowerResponse.Contains("acceleration"))
                return CoachingCategory.Throttle;
            
            if (lowerResponse.Contains("consistent") || lowerResponse.Contains("smooth"))
                return CoachingCategory.Consistency;
            
            if (lowerResponse.Contains("line") || lowerResponse.Contains("racing"))
                return CoachingCategory.RaceCraft;
            
            return CoachingCategory.General;
        }

        /// <summary>
        /// Store conversation history for context continuity
        /// </summary>
        private void StoreConversationHistory(ChatHistory chatHistory, string response)
        {
            // Add response to chat history
            chatHistory.AddAssistantMessage(response);
            
            // Store in conversation history
            _conversationHistory.Add(chatHistory);
            
            // Maintain history size limit
            if (_conversationHistory.Count > MAX_CONVERSATION_HISTORY)
            {
                _conversationHistory.RemoveAt(0);
            }
        }

        /// <summary>
        /// Get coaching statistics for performance analysis
        /// </summary>
        public CoachingStatistics GetStatistics()
        {
            var stats = new CoachingStatistics();
            
            // Calculate statistics from conversation history
            var totalResponses = _conversationHistory.Count;
            if (totalResponses == 0) return stats;
            
            stats.TotalCoachingMessages = totalResponses;
            stats.AverageResponseTime = TimeSpan.FromMilliseconds(150); // Placeholder
            stats.LastActiveTime = DateTime.Now;
            
            return stats;
        }

        /// <summary>
        /// Update coaching configuration
        /// </summary>
        public void UpdateConfiguration(CoachingConfiguration newConfig)
        {
            // Would need to reinitialize kernel with new config
            // For now, just update the reference
            // _config = newConfig;
            throw new NotImplementedException("Configuration updates require service restart");
        }
    }

    /// <summary>
    /// Configuration for LLM coaching service
    /// </summary>
    public class CoachingConfiguration
    {
        public LLMProvider Provider { get; set; } = LLMProvider.OpenAI;
        public string ModelName { get; set; } = "gpt-4o-mini";
        public string ApiKey { get; set; } = "";
        public string Endpoint { get; set; } = "";
        public float Temperature { get; set; } = 0.3f;
        public float TopP { get; set; } = 0.9f;
        public bool EnableRealTimeMode { get; set; } = true;
        public int MaxResponseLength { get; set; } = 20; // words
        public CoachingStyle Style { get; set; } = CoachingStyle.Encouraging;
    }

    /// <summary>
    /// Supported LLM providers
    /// </summary>
    public enum LLMProvider
    {
        OpenAI,
        AzureOpenAI,
        Local
    }

    /// <summary>
    /// Coaching style preferences
    /// </summary>
    public enum CoachingStyle
    {
        Encouraging,
        Direct,
        Technical,
        Motivational
    }

    /// <summary>
    /// Coaching response from LLM
    /// </summary>
    public class CoachingResponse
    {
        public string Message { get; set; } = "";
        public CoachingPriority Priority { get; set; }
        public CoachingCategory Category { get; set; }
        public DateTime Timestamp { get; set; }
        public bool IsError { get; set; }
        public string ErrorDetails { get; set; } = "";
    }

    /// <summary>
    /// Coaching priority levels
    /// </summary>
    public enum CoachingPriority
    {
        Low,
        Medium,
        High,
        Critical
    }

    /// <summary>
    /// Coaching categories
    /// </summary>
    public enum CoachingCategory
    {
        General,
        Braking,
        Cornering,
        Throttle,
        Consistency,
        RaceCraft
    }

    /// <summary>
    /// Coaching statistics
    /// </summary>
    public class CoachingStatistics
    {
        public int TotalCoachingMessages { get; set; }
        public TimeSpan AverageResponseTime { get; set; }
        public DateTime LastActiveTime { get; set; }
        public Dictionary<CoachingCategory, int> CategoryCounts { get; set; } = new();
    }
}
