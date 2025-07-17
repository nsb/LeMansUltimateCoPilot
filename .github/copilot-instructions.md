<!-- Use this file to provide workspace-specific custom instructions to Copilot. For more details, visit https://code.visualstudio.com/docs/copilot/copilot-customization#_use-a-githubcopilotinstructionsmd-file -->

# rFactor2 Shared Memory Reader Project - AI Driving Coach

This project reads real-time telemetry data from Le Mans Ultimate (rFactor2) using Windows shared memory and develops an AI-assisted driving coach.

## Project Context
- Reading rFactor2 shared memory segments: Telemetry, Scoring, Rules, etc.
- Using C# MemoryMappedFile for Windows shared memory access
- Parsing binary data structures with proper marshalling
- Real-time telemetry data processing
- Enhanced telemetry logging for AI driving coach analysis
- CSV data export for performance analysis and reference lap storage

## Key Technologies
- C# with unsafe code blocks for memory operations
- System.IO.MemoryMappedFiles for shared memory access
- Runtime.InteropServices for struct marshalling
- Windows shared memory objects named like $rFactor2SMMP_*$
- CSV logging for telemetry data storage
- High-precision timestamps for accurate data analysis

## Technical Stack
- **.NET Version**: .NET 10.0 (Preview) - `net10.0` target framework
- **Language**: C# with unsafe code enabled for direct memory operations
- **Unit Testing**: NUnit 3 framework with NUnit3TestAdapter 4.6.0.0
- **Test Coverage**: Comprehensive unit tests with 100% pass rate
- **Platform**: Windows-only (MemoryMappedFiles and System.Speech dependencies)
- **Architecture**: Real-time telemetry processing at 100Hz with circular buffers

## Core Components
- **CorneringAnalysisEngine**: AI-powered corner detection and coaching feedback
- **EnhancedTelemetryLogger**: Multi-format CSV logging with session management
- **VoiceOutputService**: Windows Speech API integration for audio coaching
- **PlayerDetectionService**: Shared memory vehicle identification with caching
- **Phase 2 AI Coach**: Advanced cornering analysis with real-time feedback

## Code Style Preferences
- Use proper error handling for shared memory access
- Include safety checks for memory operations
- Use descriptive variable names for telemetry data
- Comment complex memory operations and data structures
- Implement comprehensive logging with proper data validation
- Use consistent naming conventions for telemetry fields
- Include metadata in logged data (session info, track conditions, etc.)

## Testing Guidelines
- Write NUnit tests for all public methods and core functionality
- Use reflection-based testing for private fields when necessary
- Mock shared memory access for reliable unit testing
- Test edge cases like missing shared memory and invalid data
- Ensure 100% test coverage for critical AI coaching components
- Use Assert.DoesNotThrow for graceful error handling verification

## Memory Management Best Practices
- Always use `using` statements for MemoryMappedFile objects
- Implement proper bounds checking for unsafe memory operations
- Use circular buffers for high-frequency telemetry data (500 points ~5 seconds)
- Cache player detection results (2-second timeout) to reduce memory access
- Validate shared memory data before marshalling to prevent crashes
