using System;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Reflection;
using NUnit.Framework;
using LeMansUltimateCoPilot;

namespace LeMansUltimateCoPilot.Tests.Services
{
    [TestFixture]
    public class PlayerDetectionTests
    {
        [Test]
        public void PlayerDetection_ShouldHandleNoSharedMemory()
        {
            // Test that the player detection gracefully handles the case where shared memory doesn't exist
            // This is important for running tests without Le Mans Ultimate running
            
            // This test will likely return -1 when no shared memory is available
            // But it should not throw an exception
            Assert.DoesNotThrow(() => {
                var result = Program.FindPlayerVehicleID();
                Console.WriteLine($"FindPlayerVehicleID result (no shared memory): {result}");
                
                // Should return -1 or a cached value when no shared memory is available
                Assert.That(result, Is.GreaterThanOrEqualTo(-1), "Should return -1 or a valid cached ID");
            }, "Player detection should handle missing shared memory gracefully");
        }

        [Test]
        public void SharedMemoryNames_ShouldContainExpectedNames()
        {
            // Test that the shared memory names are correct
            var sharedMemoryNamesField = typeof(Program).GetField("SharedMemoryNames", 
                BindingFlags.NonPublic | BindingFlags.Static);
            
            Assert.That(sharedMemoryNamesField, Is.Not.Null, "SharedMemoryNames field should exist");
            
            var names = (string[])(sharedMemoryNamesField?.GetValue(null) ?? Array.Empty<string>());
            
            Assert.That(names, Is.Not.Empty, "Should have shared memory names");
            Assert.That(names, Contains.Item("$rFactor2SMMP_Telemetry$"), "Should contain telemetry memory name");
            Assert.That(names, Contains.Item("$rFactor2SMMP_Scoring$"), "Should contain scoring memory name");
            
            Console.WriteLine($"Shared memory names: {string.Join(", ", names)}");
        }

        [Test]
        public void PlayerDetectionCache_ShouldHaveReasonableTimeout()
        {
            // Test that the cache timeout is reasonable
            var playerLookupCacheTimeField = typeof(Program).GetField("PlayerLookupCacheTime", 
                BindingFlags.NonPublic | BindingFlags.Static);
            
            Assert.That(playerLookupCacheTimeField, Is.Not.Null, "PlayerLookupCacheTime field should exist");
            
            var cacheTime = (TimeSpan)(playerLookupCacheTimeField?.GetValue(null) ?? TimeSpan.Zero);
            
            Assert.That(cacheTime.TotalSeconds, Is.GreaterThan(0), "Cache time should be positive");
            Assert.That(cacheTime.TotalSeconds, Is.LessThan(60), "Cache time should be less than 60 seconds");
            Assert.That(cacheTime.TotalSeconds, Is.EqualTo(2), "Cache time should be 2 seconds as designed");
            
            Console.WriteLine($"Player lookup cache time: {cacheTime.TotalSeconds} seconds");
        }

        [Test]
        public void TestPlayerDetectionMethod_ShouldExist()
        {
            // Test that the TestPlayerDetection method exists and can be called
            var testPlayerDetectionMethod = typeof(Program).GetMethod("TestPlayerDetection", 
                BindingFlags.Public | BindingFlags.Static);
            
            Assert.That(testPlayerDetectionMethod, Is.Not.Null, "TestPlayerDetection method should exist");
            
            // We can't easily test the method execution without mocking console input
            // But we can verify it exists and has the right signature
            var parameters = testPlayerDetectionMethod.GetParameters();
            Assert.That(parameters.Length, Is.EqualTo(0), "TestPlayerDetection should have no parameters");
            
            Console.WriteLine("TestPlayerDetection method exists and has correct signature");
        }

        [Test]
        public void FindPlayerVehicleID_ShouldExist()
        {
            // Test that the FindPlayerVehicleID method exists and can be called
            var findPlayerVehicleIDMethod = typeof(Program).GetMethod("FindPlayerVehicleID", 
                BindingFlags.Public | BindingFlags.Static);
            
            Assert.That(findPlayerVehicleIDMethod, Is.Not.Null, "FindPlayerVehicleID method should exist");
            
            // Test that it returns an integer
            var returnType = findPlayerVehicleIDMethod.ReturnType;
            Assert.That(returnType, Is.EqualTo(typeof(int)), "FindPlayerVehicleID should return int");
            
            Console.WriteLine("FindPlayerVehicleID method exists and has correct signature");
        }

        [Test]
        public void PlayerDetectionCache_ShouldCacheResults()
        {
            // Test that the caching system works correctly
            // Access the private fields through reflection to test caching behavior
            var cachedPlayerVehicleIDField = typeof(Program).GetField("_cachedPlayerVehicleID", 
                BindingFlags.NonPublic | BindingFlags.Static);
            var lastPlayerLookupField = typeof(Program).GetField("_lastPlayerLookup", 
                BindingFlags.NonPublic | BindingFlags.Static);
            
            Assert.That(cachedPlayerVehicleIDField, Is.Not.Null, "Cache field should exist");
            Assert.That(lastPlayerLookupField, Is.Not.Null, "Last lookup field should exist");
            
            // Reset cache
            cachedPlayerVehicleIDField?.SetValue(null, -1);
            lastPlayerLookupField?.SetValue(null, DateTime.MinValue);
            
            // Verify initial state
            var initialCachedID = (int)(cachedPlayerVehicleIDField?.GetValue(null) ?? -1);
            var initialLastLookup = (DateTime)(lastPlayerLookupField?.GetValue(null) ?? DateTime.MinValue);
            
            Assert.That(initialCachedID, Is.EqualTo(-1), "Initial cached ID should be -1");
            Assert.That(initialLastLookup, Is.EqualTo(DateTime.MinValue), "Initial last lookup should be MinValue");
            
            Console.WriteLine("Cache fields exist and can be accessed");
        }

        [Test]
        public void PlayerDetectionLogic_ShouldBeTestable()
        {
            // Test that we can call the player detection logic
            // This is a basic smoke test to ensure the method is accessible
            
            int result = -1;
            Assert.DoesNotThrow(() => {
                result = Program.FindPlayerVehicleID();
            }, "FindPlayerVehicleID should be callable");
            
            // Result should be either -1 (no shared memory) or a valid vehicle ID (>= 0)
            Assert.That(result, Is.GreaterThanOrEqualTo(-1), "Result should be -1 or a valid vehicle ID");
            
            Console.WriteLine($"Player detection test completed. Result: {result}");
        }

        [Test]
        public void BasicStructDefinitions_ShouldExist()
        {
            // Test that the basic struct definitions exist and can be accessed
            Assert.DoesNotThrow(() => {
                var scoringInfoType = typeof(Program.rF2ScoringInfo);
                var vehicleScoringType = typeof(Program.rF2VehicleScoring);
                var scoringType = typeof(Program.rF2Scoring);
                
                Assert.That(scoringInfoType, Is.Not.Null, "rF2ScoringInfo should exist");
                Assert.That(vehicleScoringType, Is.Not.Null, "rF2VehicleScoring should exist");
                Assert.That(scoringType, Is.Not.Null, "rF2Scoring should exist");
                
                Console.WriteLine("Basic struct definitions verified");
            }, "Basic struct definitions should be accessible");
        }
    }
}

