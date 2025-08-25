<img width="2812" height="920" alt="FLAC Bechmark-H 1 5" src="https://github.com/user-attachments/assets/d0e04ac4-e620-4b3e-a36e-cd8b7dcfd37f" />



# FLAC Benchmark-H

A Windows desktop tool for benchmarking FLAC encoders and verifying audio library integrity.

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

### 4. **Automate large-scale testing**
Use the **Job List** to define, save, and execute complex sequences of encoding and decoding tasks.  
Ideal for stress-testing, regression analysis, or comparing dozens of parameter combinations across multiple files.  
Jobs are saved between sessions — set up once, run anytime.

## Features

- **Multi-pass testing**: Measure average speed, variance, and stability across multiple runs.
- **Stability metrics**: Tracks min/max speed and calculates a consistency score (p50/p90 ratio).
- **Data analysis**: Identifies fastest encoder and smallest output size.
- **Detailed logging**: Records bit depth, sampling rate, file sizes, compression ratio, speed, parameters, encoder version.
- **Export**: Save logs to Excel (.xlsx) or copy as BBCode for forums.
- **Process priority**: Set low CPU priority for background testing.

## Usage

1. **Add encoders**: Drag-and-drop `.exe` files (e.g., `flac.exe`) into the encoders list.
2. **Add audio files**: Add `.wav` or `.flac` files for testing.
3. **Run test**: Use "Start Encode", "Start Decode", or "Start Joblist" to run benchmarks.
4. **Analyze results**: Click "Analyze Log" to consolidate multi-pass results and highlight top performers.
5. **Export**: Use "Log to Excel" for further analysis.

## Technical

- Built with C# and Windows Forms (.NET 9).
- Temporary files are stored in the `temp` folder (configurable).
- Supports FLAC 1.5.0+ (for multi-threading; use 1 thread for older versions).



<img width="2763" height="284" alt="FLAC Bechmark-H 1 5Excel" src="https://github.com/user-attachments/assets/03c79b0a-f498-46cb-be2b-e107818b135c" />


Note:
You may test any builds of FLAC starting from 1.5.0.
If you want to use earlier FLAC versions then set 'Threads' option to 1.

![GitHub all releases](https://img.shields.io/github/downloads/hat3k/FLAC-Benchmark-H/total)
