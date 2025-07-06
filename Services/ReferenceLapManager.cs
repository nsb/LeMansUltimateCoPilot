using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using LMUSharedMemoryTest.Models;

namespace LMUSharedMemoryTest.Services
{
    /// <summary>
    /// Service for managing reference laps - saving, loading, and organizing reference lap data.
    /// Handles persistent storage and retrieval of reference laps for AI coaching analysis.
    /// </summary>
    public class ReferenceLapManager
    {
        private readonly string _referenceLapsDirectory;
        private readonly Dictionary<string, List<ReferenceLap>> _referenceLapsByTrack = new();
        private readonly object _lock = new object();

        /// <summary>
        /// Event fired when a new reference lap is saved
        /// </summary>
        public event EventHandler<ReferenceLapSavedEventArgs>? ReferenceLapSaved;

        /// <summary>
        /// Event fired when reference laps are loaded
        /// </summary>
        public event EventHandler<ReferenceLapsLoadedEventArgs>? ReferenceLapsLoaded;

        /// <summary>
        /// Configuration for reference lap management
        /// </summary>
        public ReferenceLapConfig Config { get; set; } = new();

        /// <summary>
        /// Total number of reference laps stored
        /// </summary>
        public int TotalReferenceLaps => _referenceLapsByTrack.Values.Sum(laps => laps.Count);

        /// <summary>
        /// Number of tracks with reference laps
        /// </summary>
        public int TracksWithReferenceLaps => _referenceLapsByTrack.Count;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="referenceLapsDirectory">Directory to store reference lap files</param>
        public ReferenceLapManager(string? referenceLapsDirectory = null)
        {
            _referenceLapsDirectory = referenceLapsDirectory ?? 
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), 
                            "LMU_Reference_Laps");
            
            // Create directory if it doesn't exist
            if (!Directory.Exists(_referenceLapsDirectory))
            {
                Directory.CreateDirectory(_referenceLapsDirectory);
            }
        }

        /// <summary>
        /// Save a reference lap to persistent storage
        /// </summary>
        /// <param name="referenceLap">Reference lap to save</param>
        /// <returns>True if saved successfully</returns>
        public bool SaveReferenceLap(ReferenceLap referenceLap)
        {
            if (referenceLap == null || !referenceLap.ValidateQuality())
                return false;

            lock (_lock)
            {
                try
                {
                    // Generate filename
                    var filename = GenerateReferenceLapFilename(referenceLap);
                    var filePath = Path.Combine(_referenceLapsDirectory, filename);

                    // Save to JSON file
                    var json = referenceLap.ToJson();
                    File.WriteAllText(filePath, json);

                    // Add to in-memory cache
                    if (!_referenceLapsByTrack.ContainsKey(referenceLap.TrackName))
                    {
                        _referenceLapsByTrack[referenceLap.TrackName] = new List<ReferenceLap>();
                    }

                    _referenceLapsByTrack[referenceLap.TrackName].Add(referenceLap);

                    // Clean up old reference laps if needed
                    CleanupOldReferenceLaps(referenceLap.TrackName);

                    // Fire event
                    ReferenceLapSaved?.Invoke(this, new ReferenceLapSavedEventArgs
                    {
                        ReferenceLap = referenceLap,
                        FilePath = filePath
                    });

                    return true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error saving reference lap: {ex.Message}");
                    return false;
                }
            }
        }

        /// <summary>
        /// Load all reference laps from persistent storage
        /// </summary>
        /// <returns>Number of reference laps loaded</returns>
        public int LoadReferenceLaps()
        {
            lock (_lock)
            {
                _referenceLapsByTrack.Clear();
                int loadedCount = 0;

                try
                {
                    if (!Directory.Exists(_referenceLapsDirectory))
                        return 0;

                    var jsonFiles = Directory.GetFiles(_referenceLapsDirectory, "*.json");

                    foreach (var filePath in jsonFiles)
                    {
                        try
                        {
                            var json = File.ReadAllText(filePath);
                            var referenceLap = ReferenceLap.FromJson(json);

                            if (referenceLap != null && referenceLap.ValidateQuality())
                            {
                                if (!_referenceLapsByTrack.ContainsKey(referenceLap.TrackName))
                                {
                                    _referenceLapsByTrack[referenceLap.TrackName] = new List<ReferenceLap>();
                                }

                                _referenceLapsByTrack[referenceLap.TrackName].Add(referenceLap);
                                loadedCount++;
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error loading reference lap from {filePath}: {ex.Message}");
                        }
                    }

                    // Fire event
                    ReferenceLapsLoaded?.Invoke(this, new ReferenceLapsLoadedEventArgs
                    {
                        LoadedCount = loadedCount,
                        TotalTracks = _referenceLapsByTrack.Count
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading reference laps: {ex.Message}");
                }

                return loadedCount;
            }
        }

        /// <summary>
        /// Get reference laps for a specific track
        /// </summary>
        /// <param name="trackName">Name of the track</param>
        /// <returns>List of reference laps for the track</returns>
        public List<ReferenceLap> GetReferenceLaps(string trackName)
        {
            lock (_lock)
            {
                if (_referenceLapsByTrack.TryGetValue(trackName, out var laps))
                {
                    return new List<ReferenceLap>(laps);
                }
                return new List<ReferenceLap>();
            }
        }

        /// <summary>
        /// Get the best reference lap for a specific track and vehicle
        /// </summary>
        /// <param name="trackName">Name of the track</param>
        /// <param name="vehicleName">Name of the vehicle (optional)</param>
        /// <returns>Best reference lap or null if none found</returns>
        public ReferenceLap? GetBestReferenceLap(string trackName, string? vehicleName = null)
        {
            lock (_lock)
            {
                var laps = GetReferenceLaps(trackName);
                
                if (vehicleName != null)
                {
                    laps = laps.Where(l => l.VehicleName == vehicleName).ToList();
                }

                return laps.Where(l => l.IsValid)
                          .OrderBy(l => l.LapTime)
                          .FirstOrDefault();
            }
        }

        /// <summary>
        /// Get all available track names with reference laps
        /// </summary>
        /// <returns>List of track names</returns>
        public List<string> GetAvailableTracks()
        {
            lock (_lock)
            {
                return new List<string>(_referenceLapsByTrack.Keys);
            }
        }

        /// <summary>
        /// Delete a reference lap
        /// </summary>
        /// <param name="referenceLapId">ID of the reference lap to delete</param>
        /// <returns>True if deleted successfully</returns>
        public bool DeleteReferenceLap(string referenceLapId)
        {
            lock (_lock)
            {
                try
                {
                    // Find and remove from memory
                    ReferenceLap? lapToDelete = null;
                    string? trackName = null;

                    foreach (var kvp in _referenceLapsByTrack)
                    {
                        var lap = kvp.Value.FirstOrDefault(l => l.Id == referenceLapId);
                        if (lap != null)
                        {
                            lapToDelete = lap;
                            trackName = kvp.Key;
                            break;
                        }
                    }

                    if (lapToDelete == null || trackName == null)
                        return false;

                    _referenceLapsByTrack[trackName].Remove(lapToDelete);

                    // Remove from file system
                    var filename = GenerateReferenceLapFilename(lapToDelete);
                    var filePath = Path.Combine(_referenceLapsDirectory, filename);
                    
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error deleting reference lap: {ex.Message}");
                    return false;
                }
            }
        }

        /// <summary>
        /// Get statistics about reference laps
        /// </summary>
        /// <returns>Reference lap statistics</returns>
        public ReferenceLapStatistics GetStatistics()
        {
            lock (_lock)
            {
                var stats = new ReferenceLapStatistics
                {
                    TotalReferenceLaps = TotalReferenceLaps,
                    TracksWithReferenceLaps = TracksWithReferenceLaps,
                    TrackStatistics = new Dictionary<string, TrackStatistics>()
                };

                foreach (var kvp in _referenceLapsByTrack)
                {
                    var trackName = kvp.Key;
                    var laps = kvp.Value;

                    if (laps.Count > 0)
                    {
                        stats.TrackStatistics[trackName] = new TrackStatistics
                        {
                            TrackName = trackName,
                            TotalLaps = laps.Count,
                            BestLapTime = laps.Where(l => l.IsValid).Min(l => l.LapTime),
                            AverageLapTime = laps.Where(l => l.IsValid).Average(l => l.LapTime),
                            UniqueVehicles = laps.Select(l => l.VehicleName).Distinct().Count()
                        };
                    }
                }

                return stats;
            }
        }

        /// <summary>
        /// Generate a filename for a reference lap
        /// </summary>
        /// <param name="referenceLap">Reference lap to generate filename for</param>
        /// <returns>Generated filename</returns>
        private string GenerateReferenceLapFilename(ReferenceLap referenceLap)
        {
            var safeName = string.Join("_", referenceLap.TrackName.Split(Path.GetInvalidFileNameChars()));
            var safeVehicle = string.Join("_", referenceLap.VehicleName.Split(Path.GetInvalidFileNameChars()));
            var timestamp = referenceLap.RecordedAt.ToString("yyyyMMdd_HHmmss");
            
            return $"{safeName}_{safeVehicle}_{timestamp}_{referenceLap.LapTime:F3}s.json";
        }

        /// <summary>
        /// Clean up old reference laps if we have too many for a track
        /// </summary>
        /// <param name="trackName">Track name to clean up</param>
        private void CleanupOldReferenceLaps(string trackName)
        {
            if (!_referenceLapsByTrack.TryGetValue(trackName, out var laps))
                return;

            if (laps.Count <= Config.MaxReferenceLapsPerTrack)
                return;

            // Sort by lap time (best first) and keep only the best laps
            var sortedLaps = laps.OrderBy(l => l.LapTime).ToList();
            var lapsToRemove = sortedLaps.Skip(Config.MaxReferenceLapsPerTrack).ToList();

            foreach (var lapToRemove in lapsToRemove)
            {
                laps.Remove(lapToRemove);
                
                // Also remove from file system
                try
                {
                    var filename = GenerateReferenceLapFilename(lapToRemove);
                    var filePath = Path.Combine(_referenceLapsDirectory, filename);
                    
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error cleaning up old reference lap: {ex.Message}");
                }
            }
        }
    }

    /// <summary>
    /// Configuration for reference lap management
    /// </summary>
    public class ReferenceLapConfig
    {
        /// <summary>
        /// Maximum number of reference laps to keep per track (default: 10)
        /// </summary>
        public int MaxReferenceLapsPerTrack { get; set; } = 10;

        /// <summary>
        /// Whether to automatically save valid laps as reference laps (default: false)
        /// </summary>
        public bool AutoSaveValidLaps { get; set; } = false;

        /// <summary>
        /// Minimum lap time improvement required to save a new reference lap (default: 0.1 seconds)
        /// </summary>
        public double MinimumImprovementSeconds { get; set; } = 0.1;
    }

    /// <summary>
    /// Event arguments for reference lap saved
    /// </summary>
    public class ReferenceLapSavedEventArgs : EventArgs
    {
        public ReferenceLap ReferenceLap { get; set; } = new();
        public string FilePath { get; set; } = "";
    }

    /// <summary>
    /// Event arguments for reference laps loaded
    /// </summary>
    public class ReferenceLapsLoadedEventArgs : EventArgs
    {
        public int LoadedCount { get; set; }
        public int TotalTracks { get; set; }
    }

    /// <summary>
    /// Statistics about reference laps
    /// </summary>
    public class ReferenceLapStatistics
    {
        public int TotalReferenceLaps { get; set; }
        public int TracksWithReferenceLaps { get; set; }
        public Dictionary<string, TrackStatistics> TrackStatistics { get; set; } = new();
    }

    /// <summary>
    /// Statistics for a specific track
    /// </summary>
    public class TrackStatistics
    {
        public string TrackName { get; set; } = "";
        public int TotalLaps { get; set; }
        public double BestLapTime { get; set; }
        public double AverageLapTime { get; set; }
        public int UniqueVehicles { get; set; }
    }
}
