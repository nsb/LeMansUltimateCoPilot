using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using LeMansUltimateCoPilot.Models;
using LeMansUltimateCoPilot.Services;

namespace LeMansUltimateCoPilot.Tests.Services
{
    [TestFixture]
    public class VoiceDrivingCoachSimpleTests
    {
        private VoiceDrivingCoach? _voiceCoach;
        private TestLLMService? _mockLLMService;
        private TestVoiceService? _mockVoiceService;
        private RealTimeComparisonService? _comparisonService;
        private TrackMapper? _trackMapper;

        [SetUp]
        public void Setup()
        {
            _trackMapper = new TrackMapper();
            _comparisonService = new RealTimeComparisonService(_trackMapper);
            _mockLLMService = new TestLLMService();
            _mockVoiceService = new TestVoiceService();
            _voiceCoach = new VoiceDrivingCoach(_mockLLMService, _mockVoiceService, _comparisonService);
        }

        [TearDown]
        public void TearDown()
        {
            _mockVoiceService?.Dispose();
        }

        [Test]
        public void Constructor_WithValidParameters_InitializesCorrectly()
        {
            // Act & Assert
            Assert.That(_voiceCoach, Is.Not.Null);
            Assert.That(_voiceCoach.MinimumCoachingInterval, Is.EqualTo(3.0));
            Assert.That(_voiceCoach.MaxQueuedMessages, Is.EqualTo(3));
            Assert.That(_voiceCoach.Style, Is.EqualTo(CoachingStyle.Encouraging));
        }

        [Test]
        public void Constructor_WithNullLLMService_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new VoiceDrivingCoach(null!, _mockVoiceService!, _comparisonService!));
        }

        [Test]
        public void SetTrack_WithValidTrack_UpdatesConfiguration()
        {
            // Arrange
            var track = new TrackConfiguration
            {
                TrackName = "Test Track",
                TrackLength = 5000,
                Segments = new List<TrackSegment>()
            };

            // Act
            _voiceCoach!.SetTrack(track);

            // Assert
            Assert.That(_mockLLMService!.CurrentTrack, Is.EqualTo(track));
        }

        [Test]
        public async Task StartCoachingSession_CallsVoiceService()
        {
            // Act
            await _voiceCoach!.StartCoachingSession();

            // Assert
            Assert.That(_mockVoiceService!.SpokenMessages, Has.Count.EqualTo(1));
            Assert.That(_mockVoiceService.SpokenMessages[0], Does.Contain("Coaching session started"));
        }

        [Test]
        public void CoachingSettings_CanBeConfigured()
        {
            // Act
            _voiceCoach!.MinimumCoachingInterval = 5.0;
            _voiceCoach.MaxQueuedMessages = 10;
            _voiceCoach.Style = CoachingStyle.Technical;

            // Assert
            Assert.That(_voiceCoach.MinimumCoachingInterval, Is.EqualTo(5.0));
            Assert.That(_voiceCoach.MaxQueuedMessages, Is.EqualTo(10));
            Assert.That(_voiceCoach.Style, Is.EqualTo(CoachingStyle.Technical));
        }
    }

    /// <summary>
    /// Simple test implementation of LLM service
    /// </summary>
    public class TestLLMService : LLMCoachingService
    {
        public TrackConfiguration? CurrentTrack { get; private set; }

        public TestLLMService() : base(new LLMConfiguration { ApiKey = "test" })
        {
        }

        public override void SetTrackContext(TrackConfiguration track)
        {
            CurrentTrack = track;
        }

        public override Task<CoachingMessage> GenerateCoachingMessageAsync(
            CoachingContext context, 
            List<CoachingContext> recentContext, 
            CoachingStyle style)
        {
            return Task.FromResult(new CoachingMessage
            {
                Content = "Test coaching message",
                Type = CoachingMessageType.RealTimeCorrection,
                Priority = CoachingPriority.Medium
            });
        }

        public override Task<string> GenerateSessionSummaryAsync(
            RealTimeComparisonMetrics metrics, 
            List<CoachingContext> recentContext)
        {
            return Task.FromResult("Test session summary");
        }
    }

    /// <summary>
    /// Simple test implementation of voice service
    /// </summary>
    public class TestVoiceService : VoiceOutputService
    {
        public List<string> SpokenMessages { get; } = new();
        public List<CoachingMessage> QueuedMessages { get; } = new();

        public TestVoiceService() : base(new VoiceSettings())
        {
        }

        public override Task SpeakAsync(string text)
        {
            SpokenMessages.Add(text);
            return Task.CompletedTask;
        }

        public override Task QueueMessageAsync(CoachingMessage message)
        {
            QueuedMessages.Add(message);
            return Task.CompletedTask;
        }
    }
}
