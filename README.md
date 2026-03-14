### Main window
<img width="2812" height="920" alt="FLAC Benchmark-H 1.5" src="https://github.com/user-attachments/assets/d0e04ac4-e620-4b3e-a36e-cd8b7dcfd37f" />

### Speed Distribution
<img width="3440" height="1400" alt="Speed distribution" src="https://github.com/user-attachments/assets/4c6ff47c-eebd-4b51-8531-105f2d72985e" />

### Summary Report
<img width="629" height="1140" alt="Summary" src="https://github.com/user-attachments/assets/f4f42020-7a93-414f-8bd8-d6af53d3d6a0" />

---

# FLAC Benchmark-H

A Windows desktop tool for benchmarking FLAC encoders, verifying audio library integrity, and generating detailed file statistics.

## Primary Use Cases

### 1. **Compare FLAC encoder builds for maximum speed**
Automate testing of multiple FLAC encoder versions and custom builds to identify the fastest one for your specific hardware.  
Run multi-pass benchmarks with consistent settings, measure average speed, stability (min/max, consistency score), and output size.  
Ideal for comparing official releases, optimized builds, or experimental forks.

### 2. **Test entire audio library for errors**
Verify the integrity of all your FLAC files in bulk.  
Detect corruption, decoding errors, or file transfer issues.  

### 3. **Find audio duplicates across formats**
Identify duplicate audio content regardless of file format, size, metadata, or extension.  
Compares `.wav` and `.flac` files by calculating MD5 hashes of the actual audio data.  

### 4. **Generate detailed library summary**
- Format statistics (FLAC/WAV count, size, duration with percentages)
- Audio properties distribution (sampling rate, bit depth, channels)
- Collapsible lists of problematic files (missing metadata, long paths, MD5 issues)

### 5. **Automate large-scale testing**
Use the **Job List** to define, save, and execute complex sequences of encoding and decoding tasks.  
Ideal for stress-testing, regression analysis, or comparing dozens of parameter combinations across multiple files.  
Jobs are saved between sessions — set up once, run anytime.

## Features

### Core Benchmarking
- **Multi-pass testing**: Measure average speed, variance, and stability across multiple runs
- **Stability metrics**: Tracks min/max speed and calculates a consistency score (p50/p90 ratio)
- **Data analysis**: Identifies fastest encoder and smallest output size
- **Detailed logging**: Records bit depth, sampling rate, file sizes, compression ratio, speed, parameters, encoder version

### Summary & Analysis Tools
- **Summary report**: Comprehensive library overview with fixed-width columns for clean copy/paste
- **Writing library column**: Shows FLAC encoder version/date for each file
- **Problem detection**: Collapsible sections for files with missing metadata or errors
- **Natural sorting**: Intuitive ordering (`Track 1` → `Track 2` → `Track 10`) in all file lists

### Export & Integration
- **Export to Excel**: Save logs and summary data to .xlsx for further analysis
- **BBCode export**: Copy results formatted for forums
- **Context menu actions**: Quick operations on files in Audio Files list

## Usage

### Quick Start
1. **Add encoders**: Drag-and-drop `.exe` files (e.g., `flac.exe`) into the encoders list
2. **Add audio files**: Add `.wav` or `.flac` files for testing
3. **Run test**: Use "Start Encode", "Start Decode", or "Start Joblist" to run benchmarks
4. **Analyze results**: Click "Analyze Log" to consolidate multi-pass results and highlight top performers
5. **Generate Summary**: Use the **Summary** context menu item for detailed library report

## Export Example

<img width="2763" height="284" alt="FLAC Benchmark-H 1.5 Excel" src="https://github.com/user-attachments/assets/03c79b0a-f498-46cb-be2b-e107818b135c" />

## Technical

- Built with C# and Windows Forms (.NET 10)
- Temporary files are stored in the `temp` folder (configurable)
- Supports FLAC 1.5.0+ (for multi-threading; use 1 thread for older versions)
- Natural string comparer for intuitive file sorting
- Double-buffered UI for smooth rendering

---

> 💡 **Note**: You may test any builds of FLAC starting from 1.5.0.  
> If you want to use earlier FLAC versions then set 'Threads' option to 1.

![GitHub all releases](https://img.shields.io/github/downloads/hat3k/FLAC-Benchmark-H/total)
