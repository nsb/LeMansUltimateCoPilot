using System;
using System.Collections.Generic;
using System.Linq;
using System.Speech.Synthesis;
using System.Threading.Tasks;
using System.Threading;
using LeMansUltimateCoPilot.Models;

namespace LeMansUltimateCoPilot.Services
{
    /// <summary>
    /// Service for managing voice output and text-to-speech functionality
    /// Handles message queuing, timing, and audio output for coaching
    /// </summary>
    public class VoiceOutputService : IDisposable
    {
        private readonly SpeechSynthesizer _synthesizer;
        private readonly Queue<CoachingMessage> _messageQueue = new();
        private readonly Timer _processingTimer;
        private readonly object _queueLock = new();
        private bool _isProcessing = false;
        private bool _isEnabled = true;
        private readonly VoiceSettings _settings;

        /// <summary>
        /// Event raised when a message is spoken
        /// </summary>
        public event EventHandler<CoachingMessage>? MessageSpoken;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="settings">Voice settings configuration</param>
        public VoiceOutputService(VoiceSettings? settings = null)
        {
            _settings = settings ?? new VoiceSettings();
            _synthesizer = new SpeechSynthesizer();
            
            ConfigureSynthesizer();
            
            // Start processing timer
            _processingTimer = new Timer(ProcessMessageQueue, null, 
                TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(100));
        }

        /// <summary>
        /// Configure the speech synthesizer
        /// </summary>
        private void ConfigureSynthesizer()
        {
            _synthesizer.Volume = _settings.Volume;
            _synthesizer.Rate = _settings.Rate;
            
            // Try to select preferred voice
            if (!string.IsNullOrEmpty(_settings.VoiceName))
            {
                try
                {
                    _synthesizer.SelectVoice(_settings.VoiceName);
                }
                catch
                {
                    // Fallback to default voice if preferred voice not available
                    var voices = _synthesizer.GetInstalledVoices();
                    if (voices.Any())
                    {
                        _synthesizer.SelectVoice(voices.First().VoiceInfo.Name);
                    }
                }
            }
        }

        /// <summary>
        /// Enable or disable voice output
        /// </summary>
        /// <param name="enabled">True to enable, false to disable</param>
        public void SetEnabled(bool enabled)
        {
            _isEnabled = enabled;
            
            if (!enabled)
            {
                // Clear queue when disabled
                lock (_queueLock)
                {
                    _messageQueue.Clear();
                }
            }
        }

        /// <summary>
        /// Queue a coaching message for voice output
        /// </summary>
        /// <param name="message">Message to queue</param>
        public virtual async Task QueueMessageAsync(CoachingMessage message)
        {
            if (!_isEnabled || string.IsNullOrEmpty(message.Content))
                return;

            lock (_queueLock)
            {
                // Remove low-priority messages if queue is full
                while (_messageQueue.Count >= _settings.MaxQueueSize)
                {
                    var oldestLowPriority = _messageQueue
                        .Where(m => m.Priority == CoachingPriority.Low)
                        .FirstOrDefault();
                    
                    if (oldestLowPriority != null)
                    {
                        var tempQueue = _messageQueue.ToList();
                        _messageQueue.Clear();
                        tempQueue.Remove(oldestLowPriority);
                        tempQueue.ForEach(m => _messageQueue.Enqueue(m));
                    }
                    else
                    {
                        _messageQueue.Dequeue(); // Remove oldest if no low priority
                    }
                }

                // Add message to queue
                _messageQueue.Enqueue(message);
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// Speak a message immediately (bypasses queue)
        /// </summary>
        /// <param name="text">Text to speak</param>
        public virtual async Task SpeakAsync(string text)
        {
            if (!_isEnabled || string.IsNullOrEmpty(text))
                return;

            await Task.Run(() =>
            {
                try
                {
                    _synthesizer.Speak(text);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Speech error: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// Process the message queue
        /// </summary>
        private void ProcessMessageQueue(object? state)
        {
            if (_isProcessing || !_isEnabled)
                return;

            CoachingMessage? messageToSpeak = null;

            lock (_queueLock)
            {
                if (_messageQueue.Count > 0)
                {
                    messageToSpeak = _messageQueue.Dequeue();
                }
            }

            if (messageToSpeak != null)
            {
                _isProcessing = true;
                
                Task.Run(async () =>
                {
                    try
                    {
                        await SpeakMessageAsync(messageToSpeak);
                    }
                    finally
                    {
                        _isProcessing = false;
                    }
                });
            }
        }

        /// <summary>
        /// Speak a coaching message
        /// </summary>
        private async Task SpeakMessageAsync(CoachingMessage message)
        {
            try
            {
                // Add pauses and emphasis based on message type
                var textToSpeak = FormatMessageForSpeech(message);
                
                await Task.Run(() =>
                {
                    _synthesizer.Speak(textToSpeak);
                });

                MessageSpoken?.Invoke(this, message);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error speaking message: {ex.Message}");
            }
        }

        /// <summary>
        /// Format message for optimal speech synthesis
        /// </summary>
        private string FormatMessageForSpeech(CoachingMessage message)
        {
            var text = message.Content;

            // Add appropriate emphasis based on priority
            switch (message.Priority)
            {
                case CoachingPriority.Critical:
                    text = $"<emphasis level='strong'>{text}</emphasis>";
                    break;
                case CoachingPriority.High:
                    text = $"<emphasis level='moderate'>{text}</emphasis>";
                    break;
            }

            // Add pauses for better comprehension
            switch (message.Type)
            {
                case CoachingMessageType.LapSummary:
                case CoachingMessageType.SessionSummary:
                    text = $"<break time='500ms'/>{text}<break time='500ms'/>";
                    break;
                case CoachingMessageType.Warning:
                    text = $"<break time='200ms'/>{text}";
                    break;
            }

            return text;
        }

        /// <summary>
        /// Get available voices
        /// </summary>
        public List<string> GetAvailableVoices()
        {
            return _synthesizer.GetInstalledVoices()
                .Select(v => v.VoiceInfo.Name)
                .ToList();
        }

        /// <summary>
        /// Update voice settings
        /// </summary>
        public void UpdateSettings(VoiceSettings settings)
        {
            _settings.Volume = settings.Volume;
            _settings.Rate = settings.Rate;
            _settings.VoiceName = settings.VoiceName;
            _settings.MaxQueueSize = settings.MaxQueueSize;
            
            ConfigureSynthesizer();
        }

        /// <summary>
        /// Clear the message queue
        /// </summary>
        public void ClearQueue()
        {
            lock (_queueLock)
            {
                _messageQueue.Clear();
            }
        }

        /// <summary>
        /// Get current queue size
        /// </summary>
        public int GetQueueSize()
        {
            lock (_queueLock)
            {
                return _messageQueue.Count;
            }
        }

        /// <summary>
        /// Dispose resources
        /// </summary>
        public void Dispose()
        {
            _processingTimer?.Dispose();
            _synthesizer?.Dispose();
        }
    }

    /// <summary>
    /// Voice output settings
    /// </summary>
    public class VoiceSettings
    {
        /// <summary>
        /// Volume (0-100)
        /// </summary>
        public int Volume { get; set; } = 80;

        /// <summary>
        /// Speech rate (-10 to 10)
        /// </summary>
        public int Rate { get; set; } = 0;

        /// <summary>
        /// Voice name to use
        /// </summary>
        public string VoiceName { get; set; } = "";

        /// <summary>
        /// Maximum messages in queue
        /// </summary>
        public int MaxQueueSize { get; set; } = 5;

        /// <summary>
        /// Enable SSML markup
        /// </summary>
        public bool EnableSSML { get; set; } = true;
    }
}
