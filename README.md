# Le Mans Ultimate Shared Memory Reader - AI Driving Coach

A C# console application that reads real-time telemetry data from Le Mans Ultimate (rFactor2) using Windows shared memory and provides enhanced telemetry logging for AI-assisted driving analysis.

## Current Status: Phase 1.2 - Reference Lap Recording ✅

**COMPLETED**: Phase 1.1 - Enhanced telemetry logging system is fully implemented and tested (50/50 tests passing).  
**COMPLETED**: Phase 1.2 - Reference lap recording and management system (103/103 tests passing).

### Phase 1.1 Features ✅
- **Enhanced Telemetry Data Model**: 60+ comprehensive telemetry fields including position, velocity, acceleration, tire data, suspension, aerodynamics, and more
- **Robust CSV Logging**: Session-based logging with proper headers, metadata, and statistics
- **Data Validation**: Input validation and error handling for telemetry data
- **Session Management**: Start/stop logging sessions with automatic file management
- **Statistics Tracking**: Session duration, record counts, and performance metrics
- **Comprehensive Testing**: 50+ unit tests covering all functionality

### Phase 1.2 Features ✅
- **Reference Lap Detection**: ✅ Real-time lap detection with start/finish line crossing
- **Lap Data Storage**: ✅ JSON-based storage with error handling and validation
- **Lap Quality Validation**: ✅ Comprehensive validation for lap data quality
- **Sector Analysis**: ✅ Automatic sector time calculation and analysis
- **Lap Management**: ✅ Save, load, delete, and organize reference laps
- **Performance Metrics**: ✅ Automatic calculation of lap performance statistics

### Technical Implementation Status
**Phase 1.1 Complete** ✅ (50/50 tests passing)
- `EnhancedTelemetryData` class with 60+ telemetry fields
- `TelemetryLogger` service for CSV logging and session management
- `SessionStatistics` for tracking session metrics
- Proper error handling and data validation
- Thread-safe operations and file management

**Phase 1.2 Complete** ✅ (103/103 tests passing)
- ✅ `ReferenceLap` data model for storing lap information with JSON serialization
- ✅ `LapDetector` service for real-time lap detection and validation
- ✅ `ReferenceLapManager` for managing lap storage, retrieval, and organization
- ✅ Comprehensive error handling and edge case coverage
- ✅ Culture-independent formatting and robust data validation

## Features

- **Real-time telemetry display** - Shows live comprehensive data from the game
- **Enhanced telemetry logging** - CSV export with detailed data for analysis
- **AI driving coach foundation** - Data structure designed for performance analysis
- **Session management** - Start/stop logging with automatic file naming
- **Comprehensive data capture**:
  - Engine data (RPM, gear, speed, throttle, brake, steering)
  - G-forces (longitudinal, lateral, vertical)
  - Temperatures (water, oil, tire temps for all four wheels)
  - Tire data (pressure, temperature, grip, load for all four wheels)
  - Suspension data (deflection, velocity for all four wheels)
  - Position and motion data (3D coordinates, velocity, acceleration)
  - Vehicle setup data (downforce, ride height, fuel level)
  - Driver inputs (both filtered and raw values)

## New Enhanced Features

### **Telemetry Logging**
- **Automatic CSV logging** with comprehensive data fields
- **Session metadata** tracking (track, vehicle, duration, record count)
- **Data validation** to ensure quality telemetry data
- **Real-time logging status** display
- **High-precision timestamps** for accurate data analysis

### **Controls**
- **'l'** - Start/Stop telemetry logging
- **'s'** - Show session statistics and log files
- **'q'** - Quit application  
- **'r'** - Reconnect to shared memory
- **Ctrl+C** - Force quit

### **Data Export Format**
CSV files include 60+ telemetry fields with metadata:
- Position and motion vectors
- All driver inputs (filtered and unfiltered)
- Complete tire data (4 wheels × temperature, pressure, load, grip)
- Suspension data (4 wheels × deflection, velocity)
- G-force calculations
- Engine and vehicle dynamics
- Session and lap information

## Prerequisites

- **Windows operating system** (shared memory is Windows-specific)
- **.NET 10.0 or later** installed
- **Le Mans Ultimate** running with an active driving session
- **rFactor2SharedMemoryPlugin** loaded (comes with Le Mans Ultimate)

## How to Use

### **Basic Operation**
1. **Start Le Mans Ultimate** and begin a driving session (practice, race, etc.)
2. **Run this application**: `dotnet run`
3. **The app will automatically connect** to the game's shared memory
4. **Real-time telemetry data** will be displayed in the console

### **Telemetry Logging for AI Analysis**
1. **Press 'l'** to start logging telemetry data
2. **Drive your session** - all data is automatically captured
3. **Press 'l' again** to stop logging
4. **Press 's'** to view session statistics and available log files
5. **Log files are saved** in `Documents\LMU_Telemetry_Logs\`

### **CSV Data Analysis**
- **Import CSV files** into Excel, Python, or data analysis tools
- **60+ data fields** per record for comprehensive analysis
- **10Hz sampling rate** (10 records per second)
- **Session metadata** included in file headers
- **Ready for AI/ML analysis** and driving coach development

## File Structure

```
LeMansUltimateCoPilot/
├── Program.cs                 # Main application
├── Models/
│   └── EnhancedTelemetryData.cs   # Comprehensive telemetry data model
├── Services/
│   └── TelemetryLogger.cs         # Logging service with session management
└── README.md
```

## Building and Running

### With .NET SDK installed:
```bash
dotnet build
dotnet run
```

### With Visual Studio:
1. Open the `.csproj` file in Visual Studio
2. Build and run the project (F5)

## Log File Format

### **Header Information**
```csv
# Telemetry Session Started: 2025-07-06 15:30:00
# Session Name: Silverstone_McLaren720S
# Log Format Version: 1.0
# Track: Silverstone International Circuit
# Vehicle: McLaren 720S GT3
```

### **Data Fields** (60+ columns including):
- **Timestamps**: Precise timing for each data point
- **Position/Motion**: 3D coordinates, velocity, acceleration
- **Driver Inputs**: Throttle, brake, steering, clutch (filtered + raw)
- **Vehicle Dynamics**: Speed, RPM, gear, G-forces
- **Tire Data**: Temperature, pressure, load, grip (all 4 wheels)
- **Suspension**: Deflection, velocity (all 4 wheels)
- **Temperatures**: Engine water, oil, tire temps
- **Setup Data**: Downforce, ride height, fuel level

## AI Driving Coach Development

This enhanced telemetry logging provides the foundation for AI driving coach development:

### **Phase 1 Complete: Enhanced Telemetry Logging** ✅
- Comprehensive data capture (60+ fields)
- Real-time logging with session management
- CSV export for analysis tools
- Data validation and quality checks

### **Next Phases** (Future Development):
- **Reference lap recording and comparison**
- **Real-time performance analysis**
- **AI coaching logic and recommendations**
- **Visual analysis dashboard**

## Troubleshooting

### "Waiting for Le Mans Ultimate..." message
- Make sure Le Mans Ultimate is running
- Ensure you're in an active driving session (not just main menu)
- The shared memory plugin only creates data during active gameplay

### "Error accessing shared memory"
- Run the application as Administrator
- Verify that the rFactor2SharedMemoryPlugin is loaded (check Process Explorer)
- Make sure no antivirus is blocking shared memory access

### Logging Issues
- Check that you have write permissions to `Documents\LMU_Telemetry_Logs\`
- Ensure sufficient disk space for telemetry data
- Large sessions can generate 50-100MB+ log files

## Technical Details

- **Uses `System.IO.MemoryMappedFiles`** for Windows shared memory access
- **Reads binary data structures** using P/Invoke marshalling
- **Connects to shared memory objects** named `$rFactor2SMMP_*$`
- **Updates at 10Hz** (10 times per second) for smooth data capture
- **Thread-safe logging** with proper cleanup and error handling
- **Comprehensive data model** designed for AI analysis

## License

This project is for educational and personal use. Le Mans Ultimate and rFactor2 are trademarks of their respective owners.

- **'q'** - Quit the application
- **'r'** - Reconnect to shared memory
- **Ctrl+C** - Force quit

## Building and Running

### With .NET SDK installed:
```bash
dotnet build
dotnet run
```

### With Visual Studio:
1. Open the `.csproj` file in Visual Studio
2. Build and run the project (F5)

## Troubleshooting

### "Waiting for Le Mans Ultimate..." message
- Make sure Le Mans Ultimate is running
- Ensure you're in an active driving session (not just main menu)
- The shared memory plugin only creates data during active gameplay

### "Error accessing shared memory"
- Run the application as Administrator
- Verify that the rFactor2SharedMemoryPlugin is loaded (check Process Explorer)
- Make sure no antivirus is blocking shared memory access

## Technical Details

- **Uses `System.IO.MemoryMappedFiles`** for Windows shared memory access
- **Reads binary data structures** using P/Invoke marshalling
- **Connects to shared memory objects** named `$rFactor2SMMP_*$`
- **Updates at 10Hz** (10 times per second) for smooth data display
- **Thread-safe** with proper cleanup and error handling

## Data Structure

The application reads from rFactor2's telemetry shared memory segment, which includes:
- Vehicle telemetry (position, velocity, acceleration)
- Engine data (RPM, temperatures, fuel)
- Input data (throttle, brake, steering, clutch)
- Tire data (temperature, pressure, grip, load)
- Suspension data (deflection, velocity)
- Session information (lap times, track name)

## License

This project is for educational and personal use. Le Mans Ultimate and rFactor2 are trademarks of their respective owners.
