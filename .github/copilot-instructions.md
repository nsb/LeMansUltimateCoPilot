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

## Code Style Preferences
- Use proper error handling for shared memory access
- Include safety checks for memory operations
- Use descriptive variable names for telemetry data
- Comment complex memory operations and data structures
- Implement comprehensive logging with proper data validation
- Use consistent naming conventions for telemetry fields
- Include metadata in logged data (session info, track conditions, etc.)
