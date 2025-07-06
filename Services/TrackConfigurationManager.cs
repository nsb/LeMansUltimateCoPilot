using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using LeMansUltimateCoPilot.Models;

namespace LeMansUltimateCoPilot.Services
{
    /// <summary>
    /// Service for managing track configurations (save, load, delete)
    /// Handles JSON serialization and file management
    /// </summary>
    public class TrackConfigurationManager
    {
        private readonly string _configurationDirectory;
        private readonly JsonSerializerOptions _jsonOptions;

        /// <summary>
        /// Event raised when a track configuration is saved
        /// </summary>
        public event EventHandler<TrackConfigurationSavedEventArgs>? ConfigurationSaved;

        /// <summary>
        /// Event raised when track configurations are loaded
        /// </summary>
        public event EventHandler<TrackConfigurationsLoadedEventArgs>? ConfigurationsLoaded;

        /// <summary>
        /// Creates a new track configuration manager
        /// </summary>
        /// <param name="configurationDirectory">Directory to store track configurations (optional)</param>
        public TrackConfigurationManager(string? configurationDirectory = null)
        {
            _configurationDirectory = configurationDirectory ?? GetDefaultConfigurationDirectory();
            
            // Ensure directory exists
            Directory.CreateDirectory(_configurationDirectory);

            // Configure JSON serialization options
            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                Converters = { new JsonStringEnumConverter() }
            };
        }

        /// <summary>
        /// Gets the default configuration directory
        /// </summary>
        /// <returns>Default directory path</returns>
        private string GetDefaultConfigurationDirectory()
        {
            string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            return Path.Combine(documentsPath, "LMU_TrackConfigurations");
        }

        /// <summary>
        /// Saves a track configuration to a JSON file
        /// </summary>
        /// <param name="configuration">Track configuration to save</param>
        /// <param name="overwrite">Whether to overwrite existing configuration</param>
        /// <returns>Path to the saved file</returns>
        public string SaveTrackConfiguration(TrackConfiguration configuration, bool overwrite = false)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            if (!configuration.IsValid())
                throw new ArgumentException("Track configuration is not valid", nameof(configuration));

            // Generate filename
            string filename = GenerateFilename(configuration);
            string filePath = Path.Combine(_configurationDirectory, filename);

            // Check if file exists and overwrite is not allowed
            if (File.Exists(filePath) && !overwrite)
            {
                throw new InvalidOperationException($"Track configuration file already exists: {filename}. Use overwrite=true to replace it.");
            }

            try
            {
                // Update last modified timestamp
                configuration.LastUpdated = DateTime.UtcNow;

                // Serialize to JSON
                string json = JsonSerializer.Serialize(configuration, _jsonOptions);

                // Write to file
                File.WriteAllText(filePath, json);

                // Raise event
                ConfigurationSaved?.Invoke(this, new TrackConfigurationSavedEventArgs(configuration, filePath));

                return filePath;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to save track configuration: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Loads a track configuration from a JSON file
        /// </summary>
        /// <param name="filePath">Path to the configuration file</param>
        /// <returns>Loaded track configuration</returns>
        public TrackConfiguration LoadTrackConfiguration(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("File path cannot be empty", nameof(filePath));

            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Track configuration file not found: {filePath}");

            try
            {
                string json = File.ReadAllText(filePath);
                var configuration = JsonSerializer.Deserialize<TrackConfiguration>(json, _jsonOptions);

                if (configuration == null)
                    throw new InvalidOperationException("Failed to deserialize track configuration");

                if (!configuration.IsValid())
                    throw new InvalidOperationException("Loaded track configuration is not valid");

                return configuration;
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException($"Failed to parse track configuration file: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to load track configuration: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Loads a track configuration by track name and variant
        /// </summary>
        /// <param name="trackName">Name of the track</param>
        /// <param name="trackVariant">Track variant (optional)</param>
        /// <returns>Loaded track configuration, or null if not found</returns>
        public TrackConfiguration? LoadTrackConfiguration(string trackName, string trackVariant = "")
        {
            if (string.IsNullOrEmpty(trackName))
                throw new ArgumentException("Track name cannot be empty", nameof(trackName));

            string filename = GenerateFilename(trackName, trackVariant);
            string filePath = Path.Combine(_configurationDirectory, filename);

            if (!File.Exists(filePath))
                return null;

            return LoadTrackConfiguration(filePath);
        }

        /// <summary>
        /// Loads all available track configurations
        /// </summary>
        /// <returns>List of all track configurations</returns>
        public List<TrackConfiguration> LoadAllTrackConfigurations()
        {
            var configurations = new List<TrackConfiguration>();

            try
            {
                var jsonFiles = Directory.GetFiles(_configurationDirectory, "*.json");

                foreach (var file in jsonFiles)
                {
                    try
                    {
                        var configuration = LoadTrackConfiguration(file);
                        configurations.Add(configuration);
                    }
                    catch (Exception ex)
                    {
                        // Log error but continue loading other files
                        Console.WriteLine($"Warning: Failed to load track configuration from {file}: {ex.Message}");
                    }
                }

                // Sort by track name and variant
                configurations = configurations.OrderBy(c => c.TrackName).ThenBy(c => c.TrackVariant).ToList();

                // Raise event
                ConfigurationsLoaded?.Invoke(this, new TrackConfigurationsLoadedEventArgs(configurations));
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to load track configurations: {ex.Message}", ex);
            }

            return configurations;
        }

        /// <summary>
        /// Deletes a track configuration file
        /// </summary>
        /// <param name="trackName">Name of the track</param>
        /// <param name="trackVariant">Track variant (optional)</param>
        /// <returns>True if file was deleted, false if not found</returns>
        public bool DeleteTrackConfiguration(string trackName, string trackVariant = "")
        {
            if (string.IsNullOrEmpty(trackName))
                throw new ArgumentException("Track name cannot be empty", nameof(trackName));

            string filename = GenerateFilename(trackName, trackVariant);
            string filePath = Path.Combine(_configurationDirectory, filename);

            if (!File.Exists(filePath))
                return false;

            try
            {
                File.Delete(filePath);
                return true;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to delete track configuration: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Checks if a track configuration exists
        /// </summary>
        /// <param name="trackName">Name of the track</param>
        /// <param name="trackVariant">Track variant (optional)</param>
        /// <returns>True if configuration exists</returns>
        public bool ConfigurationExists(string trackName, string trackVariant = "")
        {
            if (string.IsNullOrEmpty(trackName))
                return false;

            string filename = GenerateFilename(trackName, trackVariant);
            string filePath = Path.Combine(_configurationDirectory, filename);

            return File.Exists(filePath);
        }

        /// <summary>
        /// Gets a list of all available track names
        /// </summary>
        /// <returns>List of track names</returns>
        public List<string> GetAvailableTrackNames()
        {
            var configurations = LoadAllTrackConfigurations();
            return configurations.Select(c => c.TrackName).Distinct().OrderBy(name => name).ToList();
        }

        /// <summary>
        /// Gets a list of variants for a specific track
        /// </summary>
        /// <param name="trackName">Name of the track</param>
        /// <returns>List of track variants</returns>
        public List<string> GetTrackVariants(string trackName)
        {
            if (string.IsNullOrEmpty(trackName))
                return new List<string>();

            var configurations = LoadAllTrackConfigurations();
            return configurations.Where(c => c.TrackName.Equals(trackName, StringComparison.OrdinalIgnoreCase))
                                .Select(c => c.TrackVariant)
                                .Distinct()
                                .OrderBy(variant => variant)
                                .ToList();
        }

        /// <summary>
        /// Gets configuration statistics
        /// </summary>
        /// <returns>Configuration statistics</returns>
        public TrackConfigurationStatistics GetConfigurationStatistics()
        {
            var configurations = LoadAllTrackConfigurations();
            
            return new TrackConfigurationStatistics
            {
                TotalConfigurations = configurations.Count,
                UniqueTrackNames = configurations.Select(c => c.TrackName).Distinct().Count(),
                TotalSegments = configurations.Sum(c => c.GetSegmentCount()),
                AverageSegmentsPerTrack = configurations.Any() ? configurations.Average(c => c.GetSegmentCount()) : 0,
                DirectoryPath = _configurationDirectory,
                DirectorySize = CalculateDirectorySize(_configurationDirectory)
            };
        }

        /// <summary>
        /// Generates a filename for a track configuration
        /// </summary>
        /// <param name="configuration">Track configuration</param>
        /// <returns>Generated filename</returns>
        private string GenerateFilename(TrackConfiguration configuration)
        {
            return GenerateFilename(configuration.TrackName, configuration.TrackVariant);
        }

        /// <summary>
        /// Generates a filename for a track configuration
        /// </summary>
        /// <param name="trackName">Name of the track</param>
        /// <param name="trackVariant">Track variant (optional)</param>
        /// <returns>Generated filename</returns>
        private string GenerateFilename(string trackName, string trackVariant = "")
        {
            // Sanitize track name for filename
            string sanitizedName = SanitizeFilename(trackName);
            
            if (!string.IsNullOrEmpty(trackVariant))
            {
                string sanitizedVariant = SanitizeFilename(trackVariant);
                return $"{sanitizedName}_{sanitizedVariant}.json";
            }
            
            return $"{sanitizedName}.json";
        }

        /// <summary>
        /// Sanitizes a string for use as a filename
        /// </summary>
        /// <param name="filename">String to sanitize</param>
        /// <returns>Sanitized filename</returns>
        private string SanitizeFilename(string filename)
        {
            if (string.IsNullOrEmpty(filename))
                return "unknown";

            // Remove invalid characters
            char[] invalidChars = Path.GetInvalidFileNameChars();
            string sanitized = new string(filename.Where(c => !invalidChars.Contains(c)).ToArray());
            
            // Replace spaces with underscores
            sanitized = sanitized.Replace(' ', '_');
            
            // Ensure it's not empty
            if (string.IsNullOrEmpty(sanitized))
                return "unknown";
            
            return sanitized;
        }

        /// <summary>
        /// Calculates the total size of a directory
        /// </summary>
        /// <param name="directoryPath">Directory path</param>
        /// <returns>Size in bytes</returns>
        private long CalculateDirectorySize(string directoryPath)
        {
            try
            {
                var files = Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories);
                return files.Sum(file => new FileInfo(file).Length);
            }
            catch
            {
                return 0;
            }
        }
    }

    /// <summary>
    /// Event arguments for track configuration saved event
    /// </summary>
    public class TrackConfigurationSavedEventArgs : EventArgs
    {
        public TrackConfiguration Configuration { get; }
        public string FilePath { get; }

        public TrackConfigurationSavedEventArgs(TrackConfiguration configuration, string filePath)
        {
            Configuration = configuration;
            FilePath = filePath;
        }
    }

    /// <summary>
    /// Event arguments for track configurations loaded event
    /// </summary>
    public class TrackConfigurationsLoadedEventArgs : EventArgs
    {
        public List<TrackConfiguration> Configurations { get; }

        public TrackConfigurationsLoadedEventArgs(List<TrackConfiguration> configurations)
        {
            Configurations = configurations;
        }
    }

    /// <summary>
    /// Statistics about track configurations
    /// </summary>
    public class TrackConfigurationStatistics
    {
        public int TotalConfigurations { get; set; }
        public int UniqueTrackNames { get; set; }
        public int TotalSegments { get; set; }
        public double AverageSegmentsPerTrack { get; set; }
        public string DirectoryPath { get; set; } = string.Empty;
        public long DirectorySize { get; set; }
    }
}
