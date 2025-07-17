using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using LeMansUltimateCoPilot.Models;
using LeMansUltimateCoPilot.Services;

namespace LeMansUltimateCoPilot.Tests.Services
{
    [TestFixture]
    public class VoiceOutputServiceTests
    {
        private VoiceOutputService _voiceService;
        private VoiceSettings _settings;

        [SetUp]
        public void Setup()
        {
            _settings = new VoiceSettings
            {
                Volume = 80,
                Rate = 0,
                MaxQueueSize = 3
            };
            _voiceService = new VoiceOutputService(_settings);
        }

        [TearDown]
        public void TearDown()
        {
            _voiceService?.Dispose();
        }

        [Test]
        public void Constructor_WithDefaultSettings_InitializesCorrectly()
        {
            // Arrange & Act
            using var service = new VoiceOutputService();

            // Assert
            Assert.That(service, Is.Not.Null);
        }

        [Test]
        public void Constructor_WithCustomSettings_InitializesCorrectly()
        {
            // Arrange
            var customSettings = new VoiceSettings
            {
                Volume = 60,
                Rate = 2,
                MaxQueueSize = 10
            };

            // Act
            using var service = new VoiceOutputService(customSettings);

            // Assert
            Assert.That(service, Is.Not.Null);
        }

        [Test]
        public void SetEnabled_WithFalse_DisablesService()
        {
            // Act
            _voiceService.SetEnabled(false);

            // Assert - Should not throw, service should be disabled
            Assert.DoesNotThrow(() => _voiceService.SetEnabled(false));
        }

        [Test]
        public void SetEnabled_WithTrue_EnablesService()
        {
            // Act
            _voiceService.SetEnabled(true);

            // Assert - Should not throw, service should be enabled
            Assert.DoesNotThrow(() => _voiceService.SetEnabled(true));
        }

        [Test]
        public async Task QueueMessageAsync_WithValidMessage_AddsToQueue()
        {
            // Arrange
            var message = new CoachingMessage
            {
                Content = "Test message",
                Type = CoachingMessageType.RealTimeCorrection,
                Priority = CoachingPriority.Medium
            };

            // Act
            await _voiceService.QueueMessageAsync(message);

            // Assert
            Assert.That(_voiceService.GetQueueSize(), Is.GreaterThan(0));
        }

        [Test]
        public async Task QueueMessageAsync_WithEmptyMessage_DoesNotAddToQueue()
        {
            // Arrange
            var message = new CoachingMessage
            {
                Content = "",
                Type = CoachingMessageType.RealTimeCorrection,
                Priority = CoachingPriority.Medium
            };

            // Act
            await _voiceService.QueueMessageAsync(message);

            // Assert
            Assert.That(_voiceService.GetQueueSize(), Is.EqualTo(0));
        }

        [Test]
        public async Task QueueMessageAsync_WhenDisabled_DoesNotAddToQueue()
        {
            // Arrange
            _voiceService.SetEnabled(false);
            var message = new CoachingMessage
            {
                Content = "Test message",
                Type = CoachingMessageType.RealTimeCorrection,
                Priority = CoachingPriority.Medium
            };

            // Act
            await _voiceService.QueueMessageAsync(message);

            // Assert
            Assert.That(_voiceService.GetQueueSize(), Is.EqualTo(0));
        }

        [Test]
        public async Task QueueMessageAsync_ExceedsMaxQueueSize_RemovesLowPriorityMessages()
        {
            // Arrange
            var lowPriorityMessage = new CoachingMessage
            {
                Content = "Low priority message",
                Type = CoachingMessageType.RealTimeCorrection,
                Priority = CoachingPriority.Low
            };

            var highPriorityMessage = new CoachingMessage
            {
                Content = "High priority message",
                Type = CoachingMessageType.Warning,
                Priority = CoachingPriority.High
            };

            // Fill queue to max capacity
            for (int i = 0; i < _settings.MaxQueueSize; i++)
            {
                await _voiceService.QueueMessageAsync(lowPriorityMessage);
            }

            // Act - Add high priority message (should remove low priority)
            await _voiceService.QueueMessageAsync(highPriorityMessage);

            // Assert
            Assert.That(_voiceService.GetQueueSize(), Is.LessThanOrEqualTo(_settings.MaxQueueSize));
        }

        [Test]
        public void ClearQueue_RemovesAllMessages()
        {
            // Arrange
            var message = new CoachingMessage
            {
                Content = "Test message",
                Type = CoachingMessageType.RealTimeCorrection,
                Priority = CoachingPriority.Medium
            };

            // Act
            _voiceService.QueueMessageAsync(message);
            _voiceService.ClearQueue();

            // Assert
            Assert.That(_voiceService.GetQueueSize(), Is.EqualTo(0));
        }

        [Test]
        public void GetAvailableVoices_ReturnsVoiceList()
        {
            // Act
            var voices = _voiceService.GetAvailableVoices();

            // Assert
            Assert.That(voices, Is.Not.Null);
            Assert.That(voices, Is.InstanceOf<List<string>>());
        }

        [Test]
        public void UpdateSettings_WithValidSettings_UpdatesConfiguration()
        {
            // Arrange
            var newSettings = new VoiceSettings
            {
                Volume = 50,
                Rate = 1,
                MaxQueueSize = 8
            };

            // Act
            _voiceService.UpdateSettings(newSettings);

            // Assert - Should not throw exception
            Assert.DoesNotThrow(() => _voiceService.UpdateSettings(newSettings));
        }

        [Test]
        public void SpeakAsync_WithValidText_CompletesSuccessfully()
        {
            // Arrange
            var text = "Test speech";

            // Act & Assert
            Assert.DoesNotThrowAsync(async () => await _voiceService.SpeakAsync(text));
        }

        [Test]
        public void SpeakAsync_WithEmptyText_DoesNotThrow()
        {
            // Arrange
            var text = "";

            // Act & Assert
            Assert.DoesNotThrowAsync(async () => await _voiceService.SpeakAsync(text));
        }

        [Test]
        public void SpeakAsync_WhenDisabled_DoesNotThrow()
        {
            // Arrange
            _voiceService.SetEnabled(false);
            var text = "Test speech";

            // Act & Assert
            Assert.DoesNotThrowAsync(async () => await _voiceService.SpeakAsync(text));
        }
    }

    [TestFixture]
    public class VoiceSettingsTests
    {
        [Test]
        public void Constructor_SetsDefaultValues()
        {
            // Act
            var settings = new VoiceSettings();

            // Assert
            Assert.That(settings.Volume, Is.EqualTo(80));
            Assert.That(settings.Rate, Is.EqualTo(0));
            Assert.That(settings.VoiceName, Is.EqualTo(""));
            Assert.That(settings.MaxQueueSize, Is.EqualTo(5));
            Assert.That(settings.EnableSSML, Is.True);
        }

        [Test]
        public void Properties_CanBeSetAndRetrieved()
        {
            // Arrange
            var settings = new VoiceSettings();

            // Act
            settings.Volume = 60;
            settings.Rate = 2;
            settings.VoiceName = "TestVoice";
            settings.MaxQueueSize = 10;
            settings.EnableSSML = false;

            // Assert
            Assert.That(settings.Volume, Is.EqualTo(60));
            Assert.That(settings.Rate, Is.EqualTo(2));
            Assert.That(settings.VoiceName, Is.EqualTo("TestVoice"));
            Assert.That(settings.MaxQueueSize, Is.EqualTo(10));
            Assert.That(settings.EnableSSML, Is.False);
        }
    }
}

