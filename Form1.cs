using ClosedXML.Excel;
using DocumentFormat.OpenXml;
using MediaInfoLib;
using ScottPlot;
using System.Buffers;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Management;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace FLAC_Benchmark_H
{
    public partial class Form1 : Form
    {
        // Application version
        public string programVersionCurrent = "1.8.2 build 20260309"; // Current app version
        public string? programVersionIgnored = null;                  // Previously ignored update

        // Hardware info
        private int physicalCores; // Number of physical CPU cores
        private int threadCount;   // Total logical threads

        // CPU monitoring
        private readonly PerformanceCounter? cpuLoadCounter = null;                 // CPU Load counter (whole system)
        private readonly bool performanceCountersAvailable = false;                 // True if counters initialized
        private readonly System.Windows.Forms.Timer cpuUsageTimer = new();          // Updates CPU usage label
        private readonly System.Windows.Forms.Timer temporaryMessageTimer = new();  // Updates temporary messages

        private readonly PerformanceCounter? _cpuClockCounter = null;               // CPU clock counter (as % of base frequency)
        private List<double> _cpuClockReadings = [];
        private int _baseClockMhz = 0;                                              // Base CPU frequency in MHz

        // UI state
        private bool isCpuInfoLoaded = false;
        private ScriptConstructorForm? _scriptForm = null;

        // Encoder loading queue and progress
        private readonly Queue<List<(string path, bool isChecked)>> _encodersLoadQueue = new();
        private bool _isProcessingEncodersQueue = false;
        private readonly Lock _encodersQueueLock = new();
        private int _totalEncodersInQueue = 0;
        private int _processedEncodersCount = 0;
        private bool _isRefreshingEncoders = false;

        // Cancellation Token
        private CancellationTokenSource? _encoderLoadCancellation;

        // Static NumberFormatInfo for number formatting with spaces
        private static readonly NumberFormatInfo NumberFormatWithSpaces;

        // Static constructor for initialization (executed once when application starts)
        static Form1()
        {
            NumberFormatWithSpaces = (NumberFormatInfo)CultureInfo.GetCultureInfo("en-US").NumberFormat.Clone();
            NumberFormatWithSpaces.NumberGroupSeparator = " ";
        }

        // MediaInfo object pool for efficient resource reuse
        private readonly ConcurrentQueue<MediaInfo> _mediaInfoPool = new();
        private const int MediaInfoPoolSize = 20;

        // Prevents the system from entering sleep or turning off the display
        [DllImport("kernel32.dll")]
        static extern uint SetThreadExecutionState(uint esFlags);

        // Flags for SetThreadExecutionState
        const uint ES_CONTINUOUS = 0x80000000;
        const uint ES_SYSTEM_REQUIRED = 0x00000001;

        // Paths and files
        private const string SettingsGeneralFilePath = "Settings_general.txt";              // General settings file
        private const string SettingsEncodersFilePath = "Settings_flac_executables.txt";    // Encoder list file
        private const string SettingsAudioFilesFilePath = "Settings_audio_files.txt";       // Audio files list file
        private const string SettingsJobsFilePath = "Settings_jobs.txt";                    // Job list file
        private string tempFolderPath;                                                      // Path to temporary working directory

        // Process execution
        private Process _process;                                   // Running process instance
        private bool isExecuting = false;                           // True if encoding/decoding is active
        private bool _isEncodingStopped = false;                    // Request to stop
        private bool _isPaused = false;                             // Pause state
        private readonly ManualResetEvent _pauseEvent = new(true);  // Controls pause/resume sync for encoding thread

        public Form1()
        {
            InitializeComponent();
            LoadCPUInfoAsync();
            InitializeDataGridViewLog();
            InitializeDataGridViewLogDetectDupes();
            InitializeDataGridViewLogTestForErrors();

            // Initialize CPU Usage counter (Current CPU Load in %)
            try
            {
                cpuLoadCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                performanceCountersAvailable = true;
            }
            catch (Exception ex) when (ex is InvalidOperationException || ex is System.ComponentModel.Win32Exception)
            {
                MessageBox.Show(
                "Performance counters are unavailable.\n" +
                "You may be running the program remotely or without administrator privileges.\n" +
                "Some features will be disabled.\n\n" +
                "You may fix it by running CMD as Administrator, typing:\n" +
                "cd C:\\Windows\\System32\n" +
                "then:\n" +
                "lodctr /R",
                "Performance Counters Initialization Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning
                );

                performanceCountersAvailable = false;
            }

            cpuUsageTimer.Interval = 250; // Every 250 ms
            cpuUsageTimer.Tick += async (sender, e) => await UpdateCpuUsageAsync();
            cpuUsageTimer.Start();

            temporaryMessageTimer.Tick += (s, e) =>
            {
                try
                {
                    if (!IsDisposed && !Disposing)
                    {
                        if (labelStopped != null)
                            labelStopped.Visible = false;
                        if (labelAudioFileRemoved != null)
                            labelAudioFileRemoved.Visible = false;
                    }
                    temporaryMessageTimer.Stop();
                }
                catch (ObjectDisposedException) { }
            };

            // Initialize CPU Clock counter (% of base frequency)
            try
            {
                _cpuClockCounter = new PerformanceCounter("Processor Information", "% Processor Performance", "_Total");
                Debug.WriteLine("CPU Clock counter (% Processor Performance) initialized successfully.");
            }
            catch (Exception ex1)
            {
                Debug.WriteLine($"Failed to initialize CPU Clock counter with 'Processor Information': {ex1.Message}");
                try
                {
                    _cpuClockCounter = new PerformanceCounter("Processor", "% Processor Performance", "_Total");
                    Debug.WriteLine("CPU Clock counter (Processor) initialized successfully.");
                }
                catch (Exception ex2)
                {
                    Debug.WriteLine($"Failed to initialize CPU Clock counter with 'Processor': {ex2.Message}");
                    _cpuClockCounter = null;
                }
            }

            tempFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp"); // Initialize the path to the temporary folder
            _process = new Process(); // Initialize _process to avoid nullability warning

            comboBoxCPUPriority.SelectedIndex = 3;
        }

        private static string NormalizeSpaces(string input)
        {
            return Regex.Replace(input.Trim(), @"\s+", " "); // Remove extra spaces inside the string
        }
        private sealed class NaturalStringComparer : IComparer<string?>
        {
            public int Compare(string? x, string? y)
            {
                if (ReferenceEquals(x, y)) return 0;
                if (x is null) return -1;
                if (y is null) return 1;

                return CompareNatural(x.AsSpan(), y.AsSpan());
            }

            private static int CompareNatural(ReadOnlySpan<char> strA, ReadOnlySpan<char> strB)
            {
                int i1 = 0, i2 = 0;

                while (i1 < strA.Length && i2 < strB.Length)
                {
                    if (strA[i1] == strB[i2])
                    {
                        i1++; i2++;
                        continue;
                    }

                    if (IsDigit(strA[i1]) && IsDigit(strB[i2]))
                    {
                        int numEnd1 = FindNumberEnd(strA, i1);
                        int numEnd2 = FindNumberEnd(strB, i2);

                        int numberComparison = CompareNumbers(strA[i1..numEnd1],
                                                              strB[i2..numEnd2]);
                        if (numberComparison != 0)
                            return numberComparison;

                        i1 = numEnd1;
                        i2 = numEnd2;
                    }
                    else
                    {
                        char c1 = strA[i1];
                        char c2 = strB[i2];

                        if (c1 == c2)
                        {
                            i1++; i2++;
                            continue;
                        }

                        int cmp = char.ToLowerInvariant(c1).CompareTo(char.ToLowerInvariant(c2));
                        if (cmp != 0)
                            return cmp;

                        i1++; i2++;
                    }
                }

                return (strA.Length - i1).CompareTo(strB.Length - i2);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static bool IsDigit(char c)
            {
                return (uint)(c - '0') <= 9;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static int FindNumberEnd(ReadOnlySpan<char> str, int start)
            {
                int i = start;
                while (i < str.Length && IsDigit(str[i]))
                    i++;
                return i;
            }

            private static int CompareNumbers(ReadOnlySpan<char> num1, ReadOnlySpan<char> num2)
            {
                if (num1.SequenceEqual(num2))
                    return 0;

                int start1 = 0, start2 = 0;

                while (start1 < num1.Length - 1 && num1[start1] == '0') start1++;
                while (start2 < num2.Length - 1 && num2[start2] == '0') start2++;

                int len1 = num1.Length - start1;
                int len2 = num2.Length - start2;

                if (len1 != len2)
                    return len1.CompareTo(len2);

                for (int i = 0; i < len1; i++)
                {
                    char c1 = num1[start1 + i];
                    char c2 = num2[start2 + i];

                    if (c1 != c2)
                        return c1.CompareTo(c2);
                }

                return 0;
            }
        }

        // Method to load CPU information
        private async void LoadCPUInfoAsync()
        {
            if (!isCpuInfoLoaded)
            {
                physicalCores = 0;
                threadCount = 0;
                _baseClockMhz = 0;

                await Task.Run(() =>
                {
                    using ManagementObjectSearcher searcher = new("select * from Win32_Processor");
                    foreach (ManagementObject obj in searcher.Get().Cast<ManagementObject>())
                    {
                        if (obj["NumberOfCores"] != null && obj["ThreadCount"] != null)
                        {
                            physicalCores += int.Parse(obj["NumberOfCores"].ToString()!);
                            threadCount += int.Parse(obj["ThreadCount"].ToString()!);
                        }

                        // Get base clock speed in MHz
                        if (_baseClockMhz == 0 && obj["MaxClockSpeed"] != null)
                        {
                            _baseClockMhz = Convert.ToInt32(obj["MaxClockSpeed"]);
                        }
                    }
                });

                // Update the CPU information label on the UI thread
                if (physicalCores > 0 && threadCount > 0)
                {
                    labelCpuInfo.Text = $"Your system has:\nCores: {physicalCores}, Threads: {threadCount}";
                }
                else
                {
                    labelCpuInfo.Text = "Unable to retrieve CPU information.";
                }
                isCpuInfoLoaded = true;
            }
        }
        private async Task UpdateCpuUsageAsync()
        {
            float cpuLoad = 0f;
            double clockMhz = 0f;

            // Get CPU Usage
            if (performanceCountersAvailable && cpuLoadCounter != null)
            {
                try
                {
                    cpuLoad = cpuLoadCounter.NextValue();
                }
                catch { }
            }

            // Get CPU Clock
            if (_cpuClockCounter != null && _baseClockMhz > 0)
            {
                try
                {
                    _cpuClockCounter.NextValue(); // Warm up
                    await Task.Delay(1);
                    double clockPercent = _cpuClockCounter.NextValue();
                    if (clockPercent > 0)
                    {
                        clockMhz = (clockPercent / 100.0) * _baseClockMhz;
                    }
                }
                catch { }
            }

            // Update UI
            if (labelCpuUsageTitle.InvokeRequired)
            {
                labelCpuUsageTitle.Invoke((MethodInvoker)delegate
                {
                    labelCpuUsageTitle.Text = "CPU Load:\nCPU Clock:";
                    labelCpuUsageValue.Text = $"{cpuLoad:F1} %\n{clockMhz:F0} MHz";
                });
            }
            else
            {
                labelCpuUsageTitle.Text = "CPU Load:\nCPU Clock:";
                labelCpuUsageValue.Text = $"{cpuLoad:F1} %\n{clockMhz:F0} MHz";
            }
        }

        // Gets a MediaInfo instance from the pool or creates a new one if pool is empty
        private MediaInfo GetMediaInfoFromPool()
        {
            if (_mediaInfoPool.TryDequeue(out var mediaInfo))
            {
                return mediaInfo;
            }

            // Create new instance if pool is empty
            return new MediaInfoLib.MediaInfo();
        }
        // Returns MediaInfo instance to the pool for reuse
        private void ReturnMediaInfoToPool(MediaInfo mediaInfo)
        {
            if (mediaInfo == null) return;

            if (_mediaInfoPool.Count < MediaInfoPoolSize)
            {
                _mediaInfoPool.Enqueue(mediaInfo);
            }
            else
            {
                // Dispose if pool is full
                mediaInfo.Close();
            }
        }
        // Initializes MediaInfo pool with pre-created instances
        private void InitializeMediaInfoPool()
        {
            for (int i = 0; i < MediaInfoPoolSize; i++)
            {
                _mediaInfoPool.Enqueue(new MediaInfoLib.MediaInfo());
            }
        }

        // Method to get process priority
        private ProcessPriorityClass GetSelectedProcessPriority()
        {
            string priorityText;
            if (comboBoxCPUPriority.InvokeRequired)
            {
                priorityText = (string)comboBoxCPUPriority.Invoke(new Func<string>(() => comboBoxCPUPriority.SelectedItem?.ToString() ?? "Normal"));
            }
            else
            {
                priorityText = comboBoxCPUPriority.SelectedItem?.ToString() ?? "Normal";
            }

            return priorityText switch
            {
                "Idle" => ProcessPriorityClass.Idle,
                "BelowNormal" => ProcessPriorityClass.BelowNormal,
                "Normal" => ProcessPriorityClass.Normal,
                "AboveNormal" => ProcessPriorityClass.AboveNormal,
                "High" => ProcessPriorityClass.High,
                "RealTime" => ProcessPriorityClass.RealTime,
                _ => ProcessPriorityClass.Normal,
            };
        }

        // Method to save settings to .txt files
        private void SaveSettings()
        {
            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                string tempPathToSave = tempFolderPath;

                if (!string.IsNullOrEmpty(tempFolderPath) && tempFolderPath.StartsWith(baseDir, StringComparison.OrdinalIgnoreCase))
                {
                    string relativePart = tempFolderPath.Substring(baseDir.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    tempPathToSave = $".\\{relativePart}";
                }

                var settings = new List<string>
                {
                    $"CompressionLevel={textBoxCompressionLevel.Text}",
                    $"Threads={textBoxThreads.Text}",
                    $"CommandLineOptionsEncoder={textBoxCommandLineOptionsEncoder.Text}",
                    $"CommandLineOptionsDecoder={textBoxCommandLineOptionsDecoder.Text}",

                    // Quick
                    $"RemoveMetadata={checkBoxRemoveMetadata.Checked}",
                    $"AddWarmupPass={checkBoxWarmupPass.Checked}",
                    $"PreventSleep={checkBoxPreventSleep.Checked}",
                    $"AutoAnalyzeLog={checkBoxAutoAnalyzeLog.Checked}",
                    $"CPUPriority={(comboBoxCPUPriority.SelectedItem?.ToString() ?? "Normal")}",                    
 
                    //Plots
                    $"DrawMultiplots={checkBoxDrawMultiplots.Checked}",
                    $"ShowIndividualFilesPlots={checkBoxShowIndividualFilesPlots.Checked}",
                    $"ShowAggregatedByEncoderPlots={checkBoxShowAggregatedByEncoderPlots.Checked}",
                    $"ShowIdealCPULoadLine={checkBoxShowIdealCPULoadLine.Checked}",
                    $"ShowTooltipsOnPlots={checkBoxShowTooltipsOnPlots.Checked}",
                    $"WrapLongPlotLabels={checkBoxWrapLongPlotLabels.Checked}",
                    $"WrapLongPlotLabelsLength={textBoxWrapLongPlotLabels.Text}",

                    // Misc
                    $"TempFolderPath={tempPathToSave}",
                    $"ClearTempFolderOnExit={checkBoxClearTempFolder.Checked}",
                    $"AddMD5OnLoadWav={checkBoxAddMD5OnLoadWav.Checked}",
                    $"CheckForUpdatesOnStartup={checkBoxCheckForUpdatesOnStartup.Checked}",
                    $"WarningsAsErrors={checkBoxWarningsAsErrors.Checked}",
                    $"IgnoredVersion={programVersionIgnored ?? ""}"
                };

                // Logs
                foreach (DataGridViewColumn col in dataGridViewLog.Columns)
                {
                    settings.Add($"LogColumnHeaders_{col.Name}={col.HeaderText}");
                }
                foreach (DataGridViewColumn col in dataGridViewLog.Columns)
                {
                    settings.Add($"LogColumnVisibility_{col.Name}={col.Visible}");
                }

                File.WriteAllLines(SettingsGeneralFilePath, settings, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving settings: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void SaveEncoders()
        {
            try
            {
                var encoders = listViewEncoders.Items
                .Cast<ListViewItem>()
                .Select(item =>
                {
                    string status = item.Checked ? "Checked" : "Unchecked";
                    string path = item.Tag?.ToString() ?? "";
                    return $"{status}|{path}";
                })
                .ToArray();

                File.WriteAllLines(SettingsEncodersFilePath, encoders, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving encoders: {ex.Message}", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void SaveAudioFiles()
        {
            try
            {
                var audioFiles = listViewAudioFiles.Items
                .Cast<ListViewItem>()
                .Select(item =>
                {
                    string status = item.Checked ? "Checked" : "Unchecked";
                    string path = item.Tag?.ToString() ?? "";
                    return $"{status}|{path}";
                })
                .ToArray();

                File.WriteAllLines(SettingsAudioFilesFilePath, audioFiles, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving audio files: {ex.Message}", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void SaveJobs()
        {
            try
            {
                // Create an array of formatted strings representing the rows in dataGridViewJobs
                // Exclude the new row if it exists (though we disabled it)
                var lines = dataGridViewJobs.Rows.Cast<DataGridViewRow>()
                .Where(row => !row.IsNewRow) // Filter out the new row
                .Select(row =>
                {
                    // Get values from the respective cells
                    bool isChecked = Convert.ToBoolean(row.Cells["Column1CheckBox"].Value);
                    string type = row.Cells["Column2JobType"].Value?.ToString() ?? "";
                    string passes = row.Cells["Column3Passes"].Value?.ToString() ?? "";
                    string parameters = row.Cells["Column4Parameters"].Value?.ToString() ?? "";

                    // Format the row data as a single line
                    string status = isChecked ? "Checked" : "Unchecked";
                    return $"{status}|{type}|{passes}|{parameters}";
                })
                .ToArray();

                File.WriteAllLines(SettingsJobsFilePath, lines, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving jobs to file: {ex.Message}", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Method to load settings from .txt files
        private void LoadSettings()
        {
            // Set default value - relative path
            tempFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp");

            try
            {
                if (!File.Exists(SettingsGeneralFilePath))
                    return;

                string[] lines = File.ReadAllLines(SettingsGeneralFilePath);
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;

                foreach (var line in lines)
                {
                    var parts = line.Split(['='], 2);
                    if (parts.Length != 2) continue;

                    var key = parts[0].Trim();
                    var value = parts[1].Trim();

                    switch (key)
                    {
                        case "CompressionLevel":
                            textBoxCompressionLevel.Text = value;
                            break;
                        case "Threads":
                            textBoxThreads.Text = value;
                            break;
                        case "CommandLineOptionsEncoder":
                            textBoxCommandLineOptionsEncoder.Text = value;
                            break;
                        case "CommandLineOptionsDecoder":
                            textBoxCommandLineOptionsDecoder.Text = value;
                            break;

                        // Quick
                        case "RemoveMetadata":
                            checkBoxRemoveMetadata.Checked = bool.TryParse(value, out bool remove) && remove;
                            break;
                        case "AddWarmupPass":
                            checkBoxWarmupPass.Checked = bool.TryParse(value, out bool warmup) && warmup;
                            break;
                        case "PreventSleep":
                            checkBoxPreventSleep.Checked = bool.TryParse(value, out bool prevent) && prevent;
                            break;
                        case "AutoAnalyzeLog":
                            checkBoxAutoAnalyzeLog.Checked = bool.TryParse(value, out bool analyze) && analyze;
                            break;
                        case "CPUPriority":
                            comboBoxCPUPriority.SelectedItem = value;
                            break;

                        // Plots
                        case "DrawMultiplots":
                            checkBoxDrawMultiplots.Checked = bool.TryParse(value, out bool drawMultiplots) && drawMultiplots;
                            break;
                        case "ShowIndividualFilesPlots":
                            checkBoxShowIndividualFilesPlots.Checked = bool.TryParse(value, out bool showIndividualFiles) && showIndividualFiles;
                            break;
                        case "ShowAggregatedByEncoderPlots":
                            checkBoxShowAggregatedByEncoderPlots.Checked = bool.TryParse(value, out bool showAggregatedByEncoder) && showAggregatedByEncoder;
                            break;
                        case "ShowIdealCPULoadLine":
                            checkBoxShowIdealCPULoadLine.Checked = bool.TryParse(value, out bool showIdealCPULoadLine) && showIdealCPULoadLine;
                            break;
                        case "ShowTooltipsOnPlots":
                            checkBoxShowTooltipsOnPlots.Checked = bool.TryParse(value, out bool showTooltipsOnPlots) && showTooltipsOnPlots;
                            break;
                        case "WrapLongPlotLabels":
                            checkBoxWrapLongPlotLabels.Checked = bool.TryParse(value, out bool wrap) && wrap;
                            break;
                        case "WrapLongPlotLabelsLength":
                            textBoxWrapLongPlotLabels.Text = value;
                            break;

                        // Misc
                        case "TempFolderPath":
                            // If path is relative - resolve it relative to application directory
                            if (value.StartsWith(".\\") || value.StartsWith("./"))
                            {
                                string relativePart = value.Substring(2); // Remove ".\"
                                tempFolderPath = Path.Combine(baseDir, relativePart);
                            }
                            else
                            {
                                // Otherwise - use as absolute path
                                tempFolderPath = value;
                            }
                            break;
                        case "ClearTempFolderOnExit":
                            checkBoxClearTempFolder.Checked = bool.TryParse(value, out bool clear) && clear;
                            break;
                        case "AddMD5OnLoadWav":
                            checkBoxAddMD5OnLoadWav.Checked = bool.TryParse(value, out bool addMd5) && addMd5;
                            break;
                        case "CheckForUpdatesOnStartup":
                            checkBoxCheckForUpdatesOnStartup.Checked = bool.TryParse(value, out bool check) && check;
                            break;
                        case "WarningsAsErrors":
                            checkBoxWarningsAsErrors.Checked = bool.TryParse(value, out bool warnings) && warnings;
                            break;
                        case "IgnoredVersion":
                            programVersionIgnored = string.IsNullOrEmpty(value) ? null : value;
                            break;

                        // Logs
                        case string s when s.StartsWith("LogColumnHeaders_"):
                            string columnNameHdr = s.Substring("LogColumnHeaders_".Length);
                            if (dataGridViewLog.Columns[columnNameHdr] is DataGridViewColumn colHdr)
                            {
                                colHdr.HeaderText = value;
                            }
                            break;
                        case string s when s.StartsWith("LogColumnVisibility_"):
                            string columnNameVis = s.Substring("LogColumnVisibility_".Length);
                            if (bool.TryParse(value, out bool visible) && dataGridViewLog.Columns[columnNameVis] is DataGridViewColumn colVis)
                            {
                                colVis.Visible = visible;
                            }
                            break;
                    }
                }

                // Validate and ensure temp folder is accessible
                // If folder can't be created - reset to default and notify user

                if (!Directory.Exists(tempFolderPath))
                {
                    try
                    {
                        Directory.CreateDirectory(tempFolderPath);
                    }
                    catch (Exception)
                    {
                        string failedPath = tempFolderPath;
                        tempFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp");

                        if (!Directory.Exists(tempFolderPath))
                        {
                            Directory.CreateDirectory(tempFolderPath);
                        }

                        MessageBox.Show(
                        $"Cannot use specified temp folder:\n\n{failedPath}\n\nUsing default location:\n\n{tempFolderPath}",
                        "Info",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading settings: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        private async void LoadEncoders()
        {
            if (!File.Exists(SettingsEncodersFilePath))
                return;

            try
            {
                string[] lines = await File.ReadAllLinesAsync(SettingsEncodersFilePath);

                if (lines.All(string.IsNullOrWhiteSpace))
                {
                    return;
                }

                // Collect all valid encoder paths with their checked state
                var encoderPaths = new List<(string path, bool isChecked)>();
                var missingFiles = new List<string>();

                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    if (line.StartsWith("Checked|") || line.StartsWith("Unchecked|"))
                    {
                        // New format: "Checked|C:\path\to\encoder.exe"
                        int separatorIndex = line.IndexOf('|');
                        if (separatorIndex > 0 && separatorIndex < line.Length - 1)
                        {
                            bool isChecked = line.StartsWith("Checked");
                            string encoderPath = line.Substring(separatorIndex + 1);
                            if (!string.IsNullOrEmpty(encoderPath) && File.Exists(encoderPath))
                                encoderPaths.Add((encoderPath, isChecked));
                            else
                                missingFiles.Add(encoderPath);
                        }
                    }
                }

                // Enqueue encoders for processing
                AddEncoderBatchToQueue(encoderPaths, missingFiles);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading encoders: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                UpdateGroupBoxEncodersHeader();
            }
        }
        private async void LoadAudioFiles()
        {
            if (!File.Exists(SettingsAudioFilesFilePath))
                return;

            try
            {
                groupBoxAudioFiles.Text = "Loading...";

                string[] lines = await File.ReadAllLinesAsync(SettingsAudioFilesFilePath);
                listViewAudioFiles.Items.Clear();

                var missingFiles = new List<string>();
                var tasks = lines.Select(async line =>
                {
                    if (string.IsNullOrWhiteSpace(line))
                        return null;

                    if (line.StartsWith("Checked|") || line.StartsWith("Unchecked|"))
                    {
                        int separatorIndex = line.IndexOf('|');
                        if (separatorIndex == -1 || separatorIndex == line.Length - 1)
                            return null;

                        bool isChecked = line.StartsWith("Checked");
                        string audioFilePath = line.Substring(separatorIndex + 1);

                        if (!string.IsNullOrEmpty(audioFilePath) && File.Exists(audioFilePath))
                        {
                            var item = await Task.Run(() => CreateListViewAudioFilesItemInternal(audioFilePath, isChecked));
                            if (item != null)
                            {
                                item.Checked = isChecked;
                                return item;
                            }
                        }
                        else
                        {
                            missingFiles.Add(audioFilePath);
                        }
                    }
                    else if (line.Contains('~'))
                    {
                        var parts = line.Split('~');
                        if (parts.Length >= 2 && bool.TryParse(parts[^1], out bool isChecked))
                        {
                            string audioFilePath = string.Join("~", parts.Take(parts.Length - 1));

                            if (!string.IsNullOrEmpty(audioFilePath) && File.Exists(audioFilePath))
                            {
                                var item = await Task.Run(() => CreateListViewAudioFilesItemInternal(audioFilePath, isChecked));
                                if (item != null)
                                {
                                    item.Checked = isChecked;
                                    return item;
                                }
                            }
                            else
                            {
                                missingFiles.Add(audioFilePath);
                            }
                        }
                    }
                    return null;
                });

                var items = await Task.WhenAll(tasks);

                listViewAudioFiles.Items.AddRange([.. items.OfType<ListViewItem>()]);

                SaveAudioFiles();

                if (missingFiles.Count > 0)
                {
                    string warningMessage = $"The following audio files were missing and not loaded:\n\n" +
                    string.Join("\n", missingFiles.Select(Path.GetFileName)) +
                    "\n\nCheck if they still exist on your system.";

                    MessageBox.Show(warningMessage, "Missing Audio Files",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading audio files: {ex.Message}", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            UpdateGroupBoxAudioFilesHeader();
        }
        private void LoadJobs()
        {
            BackupJobsFile();
            if (!File.Exists(SettingsJobsFilePath))
                return;

            try
            {
                string[] lines = File.ReadAllLines(SettingsJobsFilePath);
                // Clear existing rows in dataGridViewJobs before loading
                dataGridViewJobs.Invoke(new Action(() => dataGridViewJobs.Rows.Clear()));

                foreach (string line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    // Check for the primary format: "Status|Type|Passes|Parameters"
                    if (line.StartsWith("Checked|") || line.StartsWith("Unchecked|"))
                    {
                        // New format: Checked|Type|Passes|Parameters
                        int firstBar = line.IndexOf('|');
                        int secondBar = line.IndexOf('|', firstBar + 1);
                        int thirdBar = line.IndexOf('|', secondBar + 1);

                        if (firstBar != -1 && secondBar != -1 && thirdBar != -1)
                        {
                            bool isChecked = line.StartsWith("Checked");
                            string type = line.Substring(firstBar + 1, secondBar - firstBar - 1);
                            string passes = line.Substring(secondBar + 1, thirdBar - secondBar - 1);
                            string parameters = thirdBar + 1 < line.Length ? line.Substring(thirdBar + 1) : "";

                            // Add the parsed job data as a new row to dataGridViewJobs
                            dataGridViewJobs.Invoke(new Action(() => dataGridViewJobs.Rows.Add(isChecked, type, passes, parameters)));
                            continue;
                        }
                    }
                    // Check for the alternative format: "Type~IsChecked~Passes~Parameters"
                    else if (line.Contains('~'))
                    {
                        // Old format: Text~Checked~Passes~Parameters
                        var parts = line.Split('~');
                        if (parts.Length == 4 && bool.TryParse(parts[1], out bool isChecked))
                        {
                            // Add the parsed job data as a new row to dataGridViewJobs
                            dataGridViewJobs.Invoke(new Action(() => dataGridViewJobs.Rows.Add(isChecked, NormalizeSpaces(parts[0]), NormalizeSpaces(parts[2]), NormalizeSpaces(parts[3]))));
                            continue;
                        }
                    }

                    MessageBox.Show($"Invalid line format: {line}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading jobs: {ex.Message}", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            dataGridViewJobs.ClearSelection();
        }
        private void BackupJobsFile()
        {
            try
            {
                if (File.Exists(SettingsJobsFilePath))
                {
                    string backupPath = $"{SettingsJobsFilePath}.bak";
                    File.Copy(SettingsJobsFilePath, backupPath, true); // Copy the file, overwrite if it exists
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Error creating backup for jobs file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Encoders
        private void ListViewEncoders_DragEnter(object? sender, DragEventArgs e)
        {
            if (_isRefreshingEncoders)
            {
                e.Effect = DragDropEffects.None;
                return;
            }

            if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
            {
                string[] files = (string[]?)e.Data.GetData(DataFormats.FileDrop) ?? [];
                // Check if there's at least one valid .exe file (excluding metaflac.exe) or a directory
                bool hasValidExeFiles = files.Any(file =>
                    Directory.Exists(file) ||
                    (Path.GetExtension(file).Equals(".exe", StringComparison.OrdinalIgnoreCase) &&
                     !Path.GetFileName(file).Equals("metaflac.exe", StringComparison.OrdinalIgnoreCase)));
                e.Effect = hasValidExeFiles ? DragDropEffects.Copy : DragDropEffects.None;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }
        private async void ListViewEncoders_DragDrop(object? sender, DragEventArgs e)
        {
            string[] files = (string[]?)e.Data?.GetData(DataFormats.FileDrop) ?? [];
            if (files.Length == 0) return;

            var validPaths = new List<(string path, bool isChecked)>();

            foreach (var file in files)
            {
                if (Directory.Exists(file))
                {
                    try
                    {
                        // Recursively find all .exe files in the directory (excluding metaflac.exe)
                        var exeFiles = Directory.GetFiles(file, "*.exe", SearchOption.AllDirectories)
                            .Where(f => !Path.GetFileName(f).Equals("metaflac.exe", StringComparison.OrdinalIgnoreCase))
                            .OrderBy(f => Path.GetDirectoryName(f), new NaturalStringComparer())
                            .ThenBy(f => Path.GetFileName(f), new NaturalStringComparer())
                            .Select(f => (f, true));
                        validPaths.AddRange(exeFiles);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error accessing directory: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else if (Path.GetExtension(file).Equals(".exe", StringComparison.OrdinalIgnoreCase))
                {
                    // Skip metaflac.exe as it's not an encoder
                    if (!Path.GetFileName(file).Equals("metaflac.exe", StringComparison.OrdinalIgnoreCase))
                    {
                        validPaths.Add((file, true));
                    }
                }
            }

            validPaths = validPaths
                .OrderBy(x => Path.GetDirectoryName(x.path) ?? string.Empty, new NaturalStringComparer())
                .ThenBy(x => Path.GetFileName(x.path), new NaturalStringComparer())
                .ToList();

            if (validPaths.Count > 0)
            {
                AddEncoderBatchToQueue(validPaths, []);
            }
        }
        private async void ButtonAddEncoders_Click(object? sender, EventArgs e)
        {
            if (_isRefreshingEncoders)
            {
                MessageBox.Show("Please wait until Refresh completes.",
                               "Refresh in Progress",
                               MessageBoxButtons.OK,
                               MessageBoxIcon.Information);
                return;
            }
            using OpenFileDialog openFileDialog = new();

            openFileDialog.Title = "Select Executable Files";
            openFileDialog.Filter = "Executable Files (*.exe)|*.exe|All Files (*.*)|*.*";
            openFileDialog.Multiselect = true;

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                var validPaths = openFileDialog.FileNames
                    .Where(file => !Path.GetFileName(file).Equals("metaflac.exe", StringComparison.OrdinalIgnoreCase))
                    .OrderBy(f => Path.GetDirectoryName(f) ?? string.Empty, new NaturalStringComparer())
                    .ThenBy(f => Path.GetFileName(f), new NaturalStringComparer())
                    .Select(file => (file, true))
                    .ToList();

                if (validPaths.Count > 0)
                {
                    AddEncoderBatchToQueue(validPaths, []);
                }
            }
        }
        private void AddEncoderBatchToQueue(List<(string path, bool isChecked)> batch, List<string> missingFiles)
        {
            using (_encodersQueueLock.EnterScope())
            {
                _encodersLoadQueue.Enqueue(batch);
                _totalEncodersInQueue += batch.Count;
            }

            // Update progress immediately to reflect the new total
            UpdateEncoderProgress();

            // Start processing if not already running
            if (!_isProcessingEncodersQueue)
            {
                _ = ProcessEncoderQueueAsync(missingFiles);
            }
        }
        private async Task ProcessEncoderQueueAsync(List<string> initialMissingFiles)
        {
            _isProcessingEncodersQueue = true;
            _processedEncodersCount = 0;
            var allMissingFiles = new List<string>(initialMissingFiles);

            _encoderLoadCancellation = new CancellationTokenSource();

            try
            {
                // listViewEncoders.BeginUpdate(); // Suspend UI updates for performance

                while (true)
                {
                    if (_encoderLoadCancellation.Token.IsCancellationRequested)
                        break;

                    List<(string path, bool isChecked)> currentBatch;

                    using (_encodersQueueLock.EnterScope())
                    {
                        if (_encodersLoadQueue.Count == 0) break;
                        currentBatch = _encodersLoadQueue.Dequeue();
                    }

                    // Process each encoder in the current batch
                    foreach (var (path, isChecked) in currentBatch)
                    {
                        if (_encoderLoadCancellation.Token.IsCancellationRequested)
                            break;

                        var item = await CreateListViewEncodersItemInternal(path, isChecked, _encoderLoadCancellation.Token);
                        if (item != null && !_encoderLoadCancellation.Token.IsCancellationRequested)
                        {
                            listViewEncoders.Items.Add(item);
                        }

                        _processedEncodersCount++;

                        // Update progress after every "% N" file for smooth UI feedback
                        if (_processedEncodersCount % 10 == 0)
                        {
                            UpdateEncoderProgress();
                        }
                        if (_processedEncodersCount % 10 == 0)
                        {
                            Application.DoEvents();
                        }
                    }
                }
            }
            finally
            {
                try
                {
                    // listViewEncoders.EndUpdate(); // Resume UI updates
                }
                catch { }

                // Reset processing state
                _isProcessingEncodersQueue = false;
                _processedEncodersCount = 0;
                _totalEncodersInQueue = 0;

                if (_encoderLoadCancellation?.IsCancellationRequested ?? false)
                {
                    using (_encodersQueueLock.EnterScope())
                    {
                        _encodersLoadQueue.Clear();
                    }
                }

                // Final UI update and persistence
                UpdateGroupBoxEncodersHeader();

                if (!(_encoderLoadCancellation?.IsCancellationRequested ?? false))
                {
                    SaveEncoders();
                }

                if (allMissingFiles.Count > 0 && !(_encoderLoadCancellation?.IsCancellationRequested ?? false))
                {
                    var uniqueMissingFiles = new HashSet<string>(allMissingFiles)
                        .OrderBy(f => f, new NaturalStringComparer())
                        .ToList();

                    string warningMessage = "The following Encoders were missing and not loaded:" +
                        Environment.NewLine + Environment.NewLine +
                        string.Join(Environment.NewLine, uniqueMissingFiles) +
                        Environment.NewLine + Environment.NewLine +
                        "Check if they still exist on your system.";

                    var dialog = new Form
                    {
                        Text = "Missing Encoders",
                        Size = new Size(600, 400),
                        MinimumSize = new Size(300, 200),
                        TopMost = true,
                        MinimizeBox = true,
                        MaximizeBox = true,
                        StartPosition = FormStartPosition.Manual
                    };

                    var mainForm = this;
                    dialog.Location = new Point(
                        mainForm.Location.X + (mainForm.Width - dialog.Width) / 2,
                        mainForm.Location.Y + (mainForm.Height - dialog.Height) / 2
                    );

                    var textBoxMissingEncoders = new RichTextBox
                    {
                        BorderStyle = BorderStyle.Fixed3D,
                        Multiline = true,
                        ScrollBars = RichTextBoxScrollBars.Both,
                        ReadOnly = true,
                        WordWrap = false,
                        Font = new Font("Segoe UI", 9),
                        Text = warningMessage,
                        SelectionLength = 0,
                        Location = new Point(12, 12),
                        Size = new Size(dialog.ClientSize.Width - 24, dialog.ClientSize.Height - 50),
                        Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
                    };

                    var buttonCloseMissingEncoders = new Button
                    {
                        Text = "Close",
                        Width = 55,
                        Height = 23,
                        Location = new Point((dialog.ClientSize.Width - 75) / 2, dialog.ClientSize.Height - 35),
                        Anchor = AnchorStyles.Bottom
                    };
                    dialog.Shown += (s, e) => buttonCloseMissingEncoders.Focus();

                    dialog.Controls.Add(textBoxMissingEncoders);
                    dialog.Controls.Add(buttonCloseMissingEncoders);

                    dialog.Show(mainForm);
                    buttonCloseMissingEncoders.Click += (s, e) => dialog.Close();
                }
                _encoderLoadCancellation?.Dispose();
                _encoderLoadCancellation = null;
            }
        }
        private void UpdateEncoderProgress()
        {
            if (_isRefreshingEncoders)
                return;

            string progressText;

            lock (_encodersQueueLock)
            {
                if (_totalEncodersInQueue > 0)
                {
                    progressText = $"Choose Encoder (Drag'n'Drop of files and folders is available) - Loading... ({_processedEncodersCount}/{_totalEncodersInQueue})";
                }
                else
                {
                    progressText = "Choose Encoder (Drag'n'Drop of files and folders is available)";
                }
            }

            // Ensure UI thread safety
            if (groupBoxEncoders.InvokeRequired)
            {
                groupBoxEncoders.Invoke((MethodInvoker)(() => groupBoxEncoders.Text = progressText));
            }
            else
            {
                groupBoxEncoders.Text = progressText;
            }
        }
        private async Task<ListViewItem?> CreateListViewEncodersItemInternal(string encoderPath, bool isChecked, CancellationToken cancellationToken = default)
        {
            if (!File.Exists(encoderPath))
                return null;

            var fileInfo = new FileInfo(encoderPath);
            DateTime currentCreationTime = fileInfo.CreationTimeUtc;
            DateTime currentLastWriteTime = fileInfo.LastWriteTimeUtc;

            // Check the cache for existing info
            if (encoderInfoCache.TryGetValue(encoderPath, out var cachedInfo))
            {
                // If the file was modified - invalidate the cache
                if (cachedInfo.CreationTime != currentCreationTime ||
                    cachedInfo.LastWriteTime != currentLastWriteTime)
                {
                    encoderInfoCache.TryRemove(encoderPath, out _);
                    cachedInfo = null;
                }
            }

            if (cachedInfo == null)
            {
                // Get encoder version by executing "--version"
                string? version = null;
                try
                {
                    version = await Task.Run(() =>
                    {
                        if (cancellationToken.IsCancellationRequested)
                            return null;

                        using Process process = new();
                        process.StartInfo.FileName = encoderPath;
                        process.StartInfo.Arguments = "--version";
                        process.StartInfo.UseShellExecute = false;
                        process.StartInfo.RedirectStandardOutput = true;
                        process.StartInfo.CreateNoWindow = true;

                        process.Start();
                        string? result = process.StandardOutput.ReadLine();

                        while (!process.HasExited)
                        {
                            if (cancellationToken.IsCancellationRequested)
                            {
                                try { process.Kill(); } catch { }
                                return null;
                            }
                            process.WaitForExit(100);
                        }

                        return result?.Trim();
                    }, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    return null;
                }
                catch (Exception)
                {
                    // Ignore version retrieval errors; version will be "N/A"
                }

                if (cancellationToken.IsCancellationRequested)
                    return null;

                // Create and cache fresh Encoder metadata
                cachedInfo = new EncoderInfo
                {
                    FilePath = encoderPath,
                    DirectoryPath = fileInfo.DirectoryName!,
                    FileName = fileInfo.Name,
                    FileNameWithoutExtension = Path.GetFileNameWithoutExtension(encoderPath),
                    Extension = fileInfo.Extension.ToLowerInvariant(),
                    Version = version ?? "N/A",
                    FileSize = fileInfo.Length,
                    CreationTime = currentCreationTime,
                    LastWriteTime = currentLastWriteTime
                };

                encoderInfoCache[encoderPath] = cachedInfo;
            }

            if (cancellationToken.IsCancellationRequested)
                return null;

            // Create ListViewItem with subitems
            var item = new ListViewItem(cachedInfo.FileName)
            {
                Tag = encoderPath,
                Checked = isChecked
            };

            item.SubItems.Add(cachedInfo.Version);
            item.SubItems.Add(cachedInfo.DirectoryPath);
            item.SubItems.Add($"{cachedInfo.FileSize:n0} bytes");
            item.SubItems.Add(cachedInfo.LastWriteTime.ToString("yyyy.MM.dd HH:mm"));

            return item;
        }

        // Class to store Encoder information
        private class EncoderInfo
        {
            public required string FilePath { get; set; }
            public required string DirectoryPath { get; set; }
            public required string FileName { get; set; }
            public required string FileNameWithoutExtension { get; set; }
            public required string Extension { get; set; } = string.Empty;
            public string Version { get; set; } = string.Empty;
            public long FileSize { get; set; }
            public DateTime CreationTime { get; set; }
            public DateTime LastWriteTime { get; set; }
            public bool WasModifiedSinceLoad { get; set; } = false;
        }
        private readonly ConcurrentDictionary<string, EncoderInfo> encoderInfoCache = new();

        private void ButtonUpEncoder_Click(object? sender, EventArgs e)
        {
            MoveSelectedItemsForListview(listViewEncoders, -1); // Pass -1 to move up
        }
        private void ButtonDownEncoder_Click(object? sender, EventArgs e)
        {
            MoveSelectedItemsForListview(listViewEncoders, 1); // Pass 1 to move down
        }
        private void ButtonClearSelectedEncoder_Click(object? sender, EventArgs e)
        {
            if (ModifierKeys == Keys.Shift)
            {
                MoveListViewItemToRecycleBin(listViewEncoders, item => item.Selected, "selected encoders", UpdateGroupBoxEncodersHeader);
                return;
            }

            for (int i = listViewEncoders.Items.Count - 1; i >= 0; i--)
            {
                if (listViewEncoders.Items[i].Selected)
                {
                    listViewEncoders.Items.RemoveAt(i);
                }
            }
            UpdateGroupBoxEncodersHeader();
        }
        private void ButtonClearEncoders_Click(object? sender, EventArgs e)
        {
            _encoderLoadCancellation?.Cancel();

            listViewEncoders.Items.Clear();

            using (_encodersQueueLock.EnterScope())
            {
                _encodersLoadQueue.Clear();
                _totalEncodersInQueue = 0;
            }

            UpdateGroupBoxEncodersHeader();
        }
        private void UpdateGroupBoxEncodersHeader()
        {
            string baseText = "Choose Encoder (Drag'n'Drop of files and folders is available)";
            int fileCount = listViewEncoders.Items.Count;

            if (fileCount > 0)
            {
                string fileWord = fileCount == 1 ? "encoder" : "encoders";
                groupBoxEncoders.Text = $"{baseText} - {fileCount} {fileWord} loaded";
            }
            else
            {
                groupBoxEncoders.Text = baseText;
            }
        }

        // Encoders Context Menu
        private void ContextMenuStripEncoders_Opening(object sender, CancelEventArgs e)
        {
            var items = listViewEncoders.Items;
            int totalItemsCount = items.Count;
            int selectedItemsCount = listViewEncoders.SelectedItems.Count;

            bool hasItems = totalItemsCount > 0;
            bool hasSelectedItems = selectedItemsCount > 0;
            bool hasUnselectedItems = hasItems && selectedItemsCount < totalItemsCount;
            bool isBusy = _isProcessingEncodersQueue || _isRefreshingEncoders;

            bool hasCheckedItems = false;
            bool hasUncheckedItems = false;
            bool hasSelectedCheckedItems = false;
            bool hasSelectedUncheckedItems = false;

            if (hasItems)
            {
                for (int i = 0; i < totalItemsCount; i++)
                {
                    var item = (ListViewItem)items[i];
                    if (item.Checked)
                        hasCheckedItems = true;
                    else
                        hasUncheckedItems = true;

                    if (hasCheckedItems && hasUncheckedItems) break;
                }
            }

            if (hasSelectedItems)
            {
                for (int i = 0; i < listViewEncoders.SelectedItems.Count; i++)
                {
                    var item = listViewEncoders.SelectedItems[i];
                    if (item.Checked)
                        hasSelectedCheckedItems = true;
                    else
                        hasSelectedUncheckedItems = true;

                    if (hasSelectedCheckedItems && hasSelectedUncheckedItems) break;
                }
            }

            // Check/Uncheck operations
            checkAllToolStripMenuItemEncoders.Enabled = hasUncheckedItems && !isBusy;
            uncheckAllToolStripMenuItemEncoders.Enabled = hasCheckedItems && !isBusy;
            checkSelectedToolStripMenuItemEncoders.Enabled = hasSelectedUncheckedItems && !isBusy;
            uncheckSelectedToolStripMenuItemEncoders.Enabled = hasSelectedCheckedItems && !isBusy;
            invertCheckToolStripMenuItemEncoders.Enabled = hasItems && !isBusy;

            // Selection operations  
            selectAllToolStripMenuItemEncoders.Enabled = hasUnselectedItems && !isBusy;
            clearSelectionToolStripMenuItemEncoders.Enabled = hasSelectedItems;
            invertSelectionToolStripMenuItemEncoders.Enabled = hasItems && !isBusy;

            // Move operations
            moveUpToolStripMenuItemEncoders.Enabled = hasSelectedItems && !isBusy;
            moveDownToolStripMenuItemEncoders.Enabled = hasSelectedItems && !isBusy;

            // Other operations
            refreshAllToolStripMenuItemEncoders.Enabled = hasItems && !isBusy;
            openContainingFolderToolStripMenuItemEncoders.Enabled = hasSelectedItems;
            clearUncheckedToolStripMenuItemEncoders.Enabled = hasUncheckedItems;
            clearSelectedToolStripMenuItemEncoders.Enabled = hasSelectedItems;
            clearDuplicateEntriesToolStripMenuItemEncoders.Enabled = hasItems && !isBusy;
            clearAllToolStripMenuItemEncoders.Enabled = true;
        }
        private void CheckAllToolStripMenuItemEncoders_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem item in listViewEncoders.Items)
            {
                item.Checked = true;
            }
        }
        private void UncheckAllToolStripMenuItemEncoders_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem item in listViewEncoders.Items)
            {
                item.Checked = false;
            }
        }
        private void CheckSelectedToolStripMenuItemEncoders_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem item in listViewEncoders.SelectedItems)
            {
                item.Checked = true;
            }
        }
        private void UncheckSelectedToolStripMenuItemEncoders_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem item in listViewEncoders.SelectedItems)
            {
                item.Checked = false;
            }
        }
        private void InvertCheckToolStripMenuItemEncoders_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem item in listViewEncoders.Items)
            {
                item.Checked = !item.Checked;
            }
        }
        private void SelectAllToolStripMenuItemEncoders_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem item in listViewEncoders.Items)
            {
                item.Selected = true;
            }
        }
        private void ClearSelectionToolStripMenuItemEncoders_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem item in listViewEncoders.SelectedItems)
            {
                item.Selected = false;
            }
        }
        private void InvertSelectionToolStripMenuItemEncoders_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem item in listViewEncoders.Items)
            {
                item.Selected = !item.Selected;
            }
        }
        private void MoveUpToolStripMenuItemEncoders_Click(object sender, EventArgs e)
        {
            MoveSelectedItemsForListview(listViewEncoders, -1);
        }
        private void MoveDownToolStripMenuItemEncoders_Click(object sender, EventArgs e)
        {
            MoveSelectedItemsForListview(listViewEncoders, 1);
        }
        private async void RefreshAllToolStripMenuItemEncoders_Click(object? sender, EventArgs e)
        {
            if (_isProcessingEncodersQueue || _isRefreshingEncoders || listViewEncoders.Items.Count == 0)
                return;

            _isRefreshingEncoders = true;
            groupBoxEncoders.Text = "Choose Encoder (Drag'n'Drop of files and folders is available) - Refreshing...";
            Application.DoEvents();

            int topIndex = listViewEncoders.TopItem?.Index ?? 0;
            var selectedIndices = new List<int>();
            foreach (ListViewItem item in listViewEncoders.SelectedItems)
            {
                selectedIndices.Add(item.Index);
            }

            try
            {
                for (int i = listViewEncoders.Items.Count - 1; i >= 0; i--)
                {
                    ListViewItem item = listViewEncoders.Items[i];
                    string encoderPath = item.Tag!.ToString()!;

                    if (!File.Exists(encoderPath))
                    {
                        listViewEncoders.Items.RemoveAt(i);
                        continue;
                    }

                    bool currentChecked = item.Checked;

                    var fileInfo = new FileInfo(encoderPath);
                    DateTime currentCreationTime = fileInfo.CreationTimeUtc;
                    DateTime currentLastWriteTime = fileInfo.LastWriteTimeUtc;

                    var cachedInfo = encoderInfoCache[encoderPath];

                    bool wasModified = cachedInfo.CreationTime != currentCreationTime ||
                                       cachedInfo.LastWriteTime != currentLastWriteTime;

                    var newItem = await CreateListViewEncodersItemInternal(encoderPath, currentChecked);
                    if (newItem == null)
                    {
                        listViewEncoders.Items.RemoveAt(i);
                        continue;
                    }

                    encoderInfoCache[encoderPath].WasModifiedSinceLoad = wasModified;

                    item.Text = newItem.Text;
                    for (int j = 0; j < newItem.SubItems.Count - 1; j++)
                    {
                        if (j + 1 < item.SubItems.Count)
                            item.SubItems[j + 1].Text = newItem.SubItems[j + 1].Text;
                    }

                    item.ForeColor = wasModified ? Color.DarkOrange : SystemColors.WindowText;
                    item.ToolTipText = wasModified
                        ? "Encoder file was modified since it was loaded.\nRefresh again to decolorize."
                        : "";
                }

                //SaveEncoders();
            }
            finally
            {
                if (listViewEncoders.Items.Count > 0 && topIndex < listViewEncoders.Items.Count)
                {
                    try
                    {
                        listViewEncoders.TopItem = listViewEncoders.Items[topIndex];
                    }
                    catch
                    {
                    }
                }

                foreach (int index in selectedIndices)
                {
                    if (index < listViewEncoders.Items.Count)
                    {
                        listViewEncoders.Items[index].Selected = true;
                    }
                }

                _isRefreshingEncoders = false;
                UpdateGroupBoxEncodersHeader();
            }
        }
        private void OpenContainingFolderToolStripMenuItemEncoders_Click(object sender, EventArgs e)
        {
            if (listViewEncoders.SelectedItems.Count == 0)
                return;

            foreach (ListViewItem selectedItem in listViewEncoders.SelectedItems)
            {
                string? encoderPath = selectedItem.Tag?.ToString();

                if (string.IsNullOrEmpty(encoderPath))
                    continue;

                if (!File.Exists(encoderPath))
                {
                    MessageBox.Show($"The selected encoder file no longer exists on disk:\n\n{encoderPath}\n\n" +
                                   "You can remove it from the list using 'Clear Selected' or 'Refresh'.",
                                   "File Not Found",
                                   MessageBoxButtons.OK,
                                   MessageBoxIcon.Warning);
                    continue;
                }

                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "explorer.exe",
                        Arguments = $"/select,\"{encoderPath}\"",
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to open folder for {Path.GetFileName(encoderPath)}:\n{ex.Message}",
                                   "Error",
                                   MessageBoxButtons.OK,
                                   MessageBoxIcon.Error);
                }
            }
        }
        private void ClearUncheckedToolStripMenuItemEncoders_Click(object sender, EventArgs e)
        {
            if (ModifierKeys == Keys.Shift)
            {
                MoveListViewItemToRecycleBin(listViewEncoders, item => !item.Checked, "unchecked encoders", UpdateGroupBoxEncodersHeader);
                return;
            }

            for (int i = listViewEncoders.Items.Count - 1; i >= 0; i--)
            {
                if (!listViewEncoders.Items[i].Checked)
                {
                    listViewEncoders.Items.RemoveAt(i);
                }
            }
            UpdateGroupBoxEncodersHeader();
        }
        private void ClearSelectedToolStripMenuItemEncoders_Click(object sender, EventArgs e)
        {
            ButtonClearSelectedEncoder_Click(sender, e);
        }
        private void ClearDuplicateEntriesToolStripMenuItemEncoders_Click(object sender, EventArgs e)
        {
            if (_isProcessingEncodersQueue || _isRefreshingEncoders || listViewEncoders.Items.Count == 0)
                return;

            var seenPaths = new HashSet<string>();
            var itemsToRemove = new List<ListViewItem>();

            foreach (ListViewItem item in listViewEncoders.Items)
            {
                string? encoderPath = item.Tag?.ToString();
                if (string.IsNullOrEmpty(encoderPath))
                    continue;

                if (!seenPaths.Add(encoderPath))
                {
                    itemsToRemove.Add(item);
                }
            }

            if (itemsToRemove.Count > 0)
            {
                foreach (var item in itemsToRemove)
                {
                    listViewEncoders.Items.Remove(item);
                }
                UpdateGroupBoxEncodersHeader();

                MessageBox.Show(
                    $"Cleared {itemsToRemove.Count} duplicate entr{(itemsToRemove.Count == 1 ? "y" : "ies")}.",
                    "Duplicates cleared",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
        }
        private void ClearAllToolStripMenuItemEncoders_Click(object sender, EventArgs e)
        {
            ButtonClearEncoders_Click(sender, e);
        }

        //Audio files
        private void ListViewAudioFiles_DragEnter(object? sender, DragEventArgs e)
        {
            if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
            {
                string[] files = (string[]?)e.Data.GetData(DataFormats.FileDrop) ?? [];
                // Check if there's at least one audio file
                bool hasAudioFiles = files.Any(file =>
                Directory.Exists(file) ||
                IsAudioFile(file)); // Use the IsAudioFile function
                e.Effect = hasAudioFiles ? DragDropEffects.Copy : DragDropEffects.None;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }
        private async void ListViewAudioFiles_DragDrop(object? sender, DragEventArgs e)
        {
            string[] files = (string[]?)e.Data?.GetData(DataFormats.FileDrop) ?? [];
            if (files.Length > 0)
            {
                groupBoxAudioFiles.Text = "Choose Audio Files (Drag'n'Drop of files and folders is available) - loading...";

                var tasks = files.Select(async file =>
                {
                    if (Directory.Exists(file))
                    {
                        // Get all audio files in the directory
                        var directoryFiles = Directory.GetFiles(file, "*.wav", SearchOption.AllDirectories)
                            .Concat(Directory.GetFiles(file, "*.flac", SearchOption.AllDirectories))
                            .OrderBy(f => Path.GetDirectoryName(f), new NaturalStringComparer())
                            .ThenBy(f => Path.GetFileName(f), new NaturalStringComparer())
                            .ToList();

                        // Create a ListViewItem for each found audio file
                        var items = await Task.WhenAll(directoryFiles.Select(f => Task.Run(() => CreateListViewAudioFilesItemInternal(f, true))));
                        return items; // Return an array of ListViewItem
                    }
                    else if (IsAudioFile(file) && File.Exists(file))
                    {
                        var item = await Task.Run(() => CreateListViewAudioFilesItemInternal(file, true)); // Create a list item
                        return [item]; // Return an array with one item
                    }

                    return []; // Return an empty array if it's not an audio file
                });

                var itemsList = await Task.WhenAll(tasks); // Wait for all tasks to complete

                // Add items to the ListView
                foreach (var itemList in itemsList)
                {
                    if (itemList != null && itemList.Length > 0)
                    {
                        var nonNullItems = itemList
                            .Where(item => item != null)
                            .Select(item => item!)
                            .ToArray();

                        if (nonNullItems.Length > 0)
                        {
                            listViewAudioFiles.Items.AddRange(nonNullItems);
                        }
                    }
                }
            }
            UpdateGroupBoxAudioFilesHeader();
        }
        private async void ButtonAddAudioFiles_Click(object? sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new())
            {
                openFileDialog.Title = "Select Audio Files";
                openFileDialog.Filter = "Audio Files (*.flac;*.wav)|*.flac;*.wav|" +
                                        "FLAC Files (*.flac)|*.flac|" +
                                        "WAV Files (*.wav)|*.wav|" +
                                        "All Files (*.*)|*.*";
                openFileDialog.Multiselect = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    groupBoxAudioFiles.Text = "Choose Audio Files (Drag'n'Drop of files and folders is available) - loading...";

                    var sortedFileNames = openFileDialog.FileNames
                        .Where(IsAudioFile)
                        .OrderBy(f => Path.GetDirectoryName(f), new NaturalStringComparer())
                        .ThenBy(f => Path.GetFileName(f), new NaturalStringComparer())
                        .ToList();

                    var tasks = sortedFileNames.Select(async file =>
                    {
                        var item = await Task.Run(() => CreateListViewAudioFilesItemInternal(file, true)); // Create a list item
                        if (item != null)
                        {
                            item.Checked = true;
                        }
                        return item;
                    });

                    var items = await Task.WhenAll(tasks); // Wait for all tasks to complete

                    foreach (var item in items)
                    {
                        if (item != null)
                        {
                            listViewAudioFiles.Items.Add(item); // Add items to the ListView
                        }
                    }
                }
            }
            UpdateGroupBoxAudioFilesHeader();
        }
        private static bool IsAudioFile(string audioFilePath)
        {
            string extension = Path.GetExtension(audioFilePath);
            return extension.Equals(".wav", StringComparison.OrdinalIgnoreCase) || extension.Equals(".flac", StringComparison.OrdinalIgnoreCase);
        }
        private async Task<ListViewItem?> CreateListViewAudioFilesItemInternal(string audioFilePath, bool isChecked)
        {
            if (!File.Exists(audioFilePath))
                return null;

            var fileInfo = new FileInfo(audioFilePath);
            DateTime currentCreationTime = fileInfo.CreationTimeUtc;
            DateTime currentLastWriteTime = fileInfo.LastWriteTimeUtc;

            if (audioFileInfoCache.TryGetValue(audioFilePath, out var cachedInfo))
            {
                if (cachedInfo.CreationTime != currentCreationTime ||
                    cachedInfo.LastWriteTime != currentLastWriteTime)
                {
                    audioFileInfoCache.TryRemove(audioFilePath, out _);
                    cachedInfo = null;
                }
            }

            if (cachedInfo == null)
            {
                cachedInfo = new AudioFileInfo
                {
                    FilePath = audioFilePath,
                    DirectoryPath = fileInfo.DirectoryName!,
                    FileName = fileInfo.Name,
                    FileNameWithoutExtension = Path.GetFileNameWithoutExtension(audioFilePath),
                    Extension = Path.GetExtension(audioFilePath).ToLowerInvariant(),
                    Channels = string.Empty,
                    BitDepth = string.Empty,
                    BitDepthString = string.Empty,
                    SamplingRate = string.Empty,
                    SamplingRateString = string.Empty,
                    Duration = string.Empty,
                    FileSize = fileInfo.Length,
                    Md5Hash = string.Empty,
                    CreationTime = currentCreationTime,
                    LastWriteTime = currentLastWriteTime,
                    ErrorDetails = string.Empty,
                    WritingLibrary = string.Empty
                };

                audioFileInfoCache[audioFilePath] = cachedInfo;

                var mediaInfo = GetMediaInfoFromPool();
                try
                {
                    mediaInfo.Open(audioFilePath);

                    cachedInfo.Channels = mediaInfo.Get(StreamKind.Audio, 0, "Channel(s)") ?? "N/A";
                    cachedInfo.Duration = mediaInfo.Get(StreamKind.Audio, 0, "Duration") ?? "N/A";
                    cachedInfo.BitDepth = mediaInfo.Get(StreamKind.Audio, 0, "BitDepth") ?? "N/A";
                    cachedInfo.BitDepthString = mediaInfo.Get(StreamKind.Audio, 0, "BitDepth/String") ?? "N/A";
                    cachedInfo.SamplingRate = mediaInfo.Get(StreamKind.Audio, 0, "SamplingRate") ?? "N/A";
                    cachedInfo.SamplingRateString = mediaInfo.Get(StreamKind.Audio, 0, "SamplingRate/String") ?? "N/A";

                    if (cachedInfo.Extension == ".flac")
                    {
                        cachedInfo.WritingLibrary = mediaInfo.Get(StreamKind.Audio, 0, "Encoded_Library/String") ?? "N/A";
                        
                        cachedInfo.Md5Hash = mediaInfo.Get(StreamKind.Audio, 0, "MD5_Unencoded") ?? "N/A";
                        cachedInfo.Md5HashMissing = string.IsNullOrEmpty(cachedInfo.Md5Hash) ||
                                 cachedInfo.Md5Hash == "N/A" ||
                                 cachedInfo.Md5Hash == "00000000000000000000000000000000";
                    }
                    else if (cachedInfo.Extension == ".wav" && checkBoxAddMD5OnLoadWav.Checked)
                    {
                        cachedInfo.Md5Hash = await CalculateWavMD5Async(audioFilePath);
                    }
                }
                finally
                {
                    mediaInfo.Close();
                    ReturnMediaInfoToPool(mediaInfo);
                }
            }
            else
            {
                if (cachedInfo.Extension == ".wav" && checkBoxAddMD5OnLoadWav.Checked)
                {
                    if (string.IsNullOrEmpty(cachedInfo.Md5Hash) || cachedInfo.Md5Hash == "N/A")
                    {
                        cachedInfo.Md5Hash = await CalculateWavMD5Async(audioFilePath);
                    }
                }
            }

            var item = new ListViewItem(cachedInfo.FileName)
            {
                Tag = audioFilePath,
                Checked = isChecked
            };

            item.SubItems.Add(cachedInfo.Channels);
            item.SubItems.Add(cachedInfo.BitDepthString);
            item.SubItems.Add(cachedInfo.SamplingRateString);
            item.SubItems.Add($"{cachedInfo.Duration:n0} ms");
            item.SubItems.Add($"{cachedInfo.FileSize:n0} bytes");
            item.SubItems.Add(cachedInfo.Md5Hash);
            item.SubItems.Add(cachedInfo.DirectoryPath);
            item.SubItems.Add(cachedInfo.WritingLibrary);

            return item;
        }

        // Class to store Audio File information
        private class AudioFileInfo
        {
            public required string FilePath { get; set; }
            public required string DirectoryPath { get; set; }
            public required string FileName { get; set; }
            public required string FileNameWithoutExtension { get; set; }
            public required string Extension { get; set; } = string.Empty;
            public string Channels { get; set; } = string.Empty;
            public string BitDepth { get; set; } = string.Empty;
            public string BitDepthString { get; set; } = string.Empty;
            public string SamplingRate { get; set; } = string.Empty;
            public string SamplingRateString { get; set; } = string.Empty;
            public string Duration { get; set; } = string.Empty;
            public long FileSize { get; set; }
            public string Md5Hash { get; set; } = string.Empty;
            public bool Md5HashMissing { get; set; }
            public string ErrorDetails { get; set; } = string.Empty;
            public string WritingLibrary { get; set; } = string.Empty;
            public DateTime CreationTime { get; set; }
            public DateTime LastWriteTime { get; set; }
            public bool WasModifiedSinceLoad { get; set; } = false;
        }
        private readonly ConcurrentDictionary<string, AudioFileInfo> audioFileInfoCache = new();

        private async Task<string> CalculateWavMD5Async(string audioFilePath)
        {
            try
            {
                await using var stream = File.OpenRead(audioFilePath);
                using var md5 = MD5.Create();
                using var reader = new BinaryReader(stream);

                // Validate RIFF header
                if (reader.ReadUInt32() != 0x46464952) // "RIFF"
                {
                    UpdateCacheWithMD5Error(audioFilePath, "Invalid WAV file: Missing RIFF header.");
                    return "MD5 calculation failed";
                }

                reader.ReadUInt32(); // Skip file size

                // Validate WAVE header
                if (reader.ReadUInt32() != 0x45564157) // "WAVE"
                {
                    UpdateCacheWithMD5Error(audioFilePath, "Invalid WAV file: Missing WAVE header.");
                    return "MD5 calculation failed";
                }

                // Process chunks
                while (reader.BaseStream.Position < reader.BaseStream.Length)
                {
                    // Check if we can read chunk header
                    if (reader.BaseStream.Position + 8 > reader.BaseStream.Length)
                        break;

                    uint chunkId = reader.ReadUInt32();
                    uint chunkSize = reader.ReadUInt32();

                    // Validate chunk size
                    if (reader.BaseStream.Position + chunkSize > reader.BaseStream.Length)
                    {
                        UpdateCacheWithMD5Error(audioFilePath, "Invalid WAV file: Chunk size exceeds file bounds.");
                        return "MD5 calculation failed";
                    }

                    if (chunkId == 0x20746D66) // "fmt "
                    {
                        reader.BaseStream.Seek(chunkSize, SeekOrigin.Current);

                        // Align to 2-byte boundary after chunk
                        if (chunkSize % 2 == 1)
                            reader.BaseStream.Seek(1, SeekOrigin.Current);
                    }
                    else if (chunkId == 0x61746164) // "data"
                    {
                        // Validate data chunk bounds
                        if (reader.BaseStream.Position + chunkSize > reader.BaseStream.Length)
                        {
                            UpdateCacheWithMD5Error(audioFilePath, "Invalid WAV file: 'data' chunk size exceeds file bounds.");
                            return "MD5 calculation failed";
                        }

                        long bytesToRead = chunkSize;
                        byte[] buffer = new byte[8192];
                        long totalBytesRead = 0;

                        // Stream the data chunk and update MD5 incrementally
                        while (totalBytesRead < bytesToRead)
                        {
                            int bytesToReadThisIteration = (int)Math.Min(buffer.Length, bytesToRead - totalBytesRead);
                            int bytesRead = await reader.BaseStream.ReadAsync(buffer.AsMemory(0, bytesToReadThisIteration));

                            if (bytesRead == 0)
                            {
                                UpdateCacheWithMD5Error(audioFilePath, "Unexpected end of file while reading 'data' chunk.");
                                return "MD5 calculation failed";
                            }

                            md5.TransformBlock(buffer, 0, bytesRead, null, 0);
                            totalBytesRead += bytesRead;
                        }

                        md5.TransformFinalBlock([], 0, 0);
                        string md5Hash = Convert.ToHexString(md5.Hash!).ToUpperInvariant();

                        // Align to 2-byte boundary after data chunk
                        if (chunkSize % 2 == 1)
                        {
                            // Only seek if we're not at the end of the stream
                            if (reader.BaseStream.Position < reader.BaseStream.Length)
                                reader.BaseStream.Seek(1, SeekOrigin.Current);
                        }

                        // Success: Update cache directly
                        var cachedInfo = audioFileInfoCache[audioFilePath];
                        cachedInfo.Md5Hash = md5Hash;
                        cachedInfo.ErrorDetails = string.Empty;

                        return md5Hash;
                    }
                    else
                    {
                        // Skip unknown chunk
                        reader.BaseStream.Seek(chunkSize, SeekOrigin.Current);

                        // Align to 2-byte boundary after unknown chunk
                        if (chunkSize % 2 == 1)
                        {
                            // Only seek if we're not at the end of the stream
                            if (reader.BaseStream.Position < reader.BaseStream.Length)
                                reader.BaseStream.Seek(1, SeekOrigin.Current);
                        }
                    }
                }

                UpdateCacheWithMD5Error(audioFilePath, "Invalid WAV file: No data chunk found.");
                return "MD5 calculation failed";
            }
            catch (Exception ex)
            {
                UpdateCacheWithMD5Error(audioFilePath, $"Error calculating MD5 for WAV file: {ex.Message}");
                return "MD5 calculation failed";
            }
        }
        private async Task<string> CalculateFlacMD5Async(string flacFilePath)
        {
            try
            {
                string? encoderExePath = null;

                // Get encoder path from UI (thread-safe)
                await InvokeAsync(() =>
                {
                    var encoderItem = listViewEncoders.Items
                        .Cast<ListViewItem>()
                        .FirstOrDefault(item =>
                        {
                            if (item.Tag == null) return false;
                            string path = item.Tag.ToString()!;
                            return encoderInfoCache[path].Extension == ".exe";
                        });

                    encoderExePath = encoderItem?.Tag?.ToString();
                });

                // Validate encoder path
                if (string.IsNullOrEmpty(encoderExePath) || !File.Exists(encoderExePath))
                {
                    UpdateCacheWithMD5Error(flacFilePath, "No .exe encoder found in the list");
                    return "MD5 calculation failed";
                }

                // Ensure temp folder exists
                if (!Directory.Exists(tempFolderPath))
                {
                    try
                    {
                        Directory.CreateDirectory(tempFolderPath);
                    }
                    catch (Exception ex)
                    {
                        UpdateCacheWithMD5Error(flacFilePath, $"Failed to create temp folder: {ex.Message}");
                        return "MD5 calculation failed";
                    }
                }

                // Create unique temp WAV file path
                string tempWavFile = Path.Combine(tempFolderPath, $"temp_flac_md5_{Guid.NewGuid()}.wav");
                string arguments = $"\"{flacFilePath}\" -d --no-preserve-modtime --silent -f -o \"{tempWavFile}\"";

                // Configure and start the decoding process
                using var process = new Process();
                process.StartInfo.FileName = encoderExePath;
                process.StartInfo.Arguments = arguments;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.CreateNoWindow = true;

                process.Start();

                // Read error output and wait for completion
                string errorOutput = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                // Handle decoding failure
                if (process.ExitCode != 0)
                {
                    UpdateCacheWithMD5Error(flacFilePath, $"Decode failed: {errorOutput.Trim()}");
                    try { if (File.Exists(tempWavFile)) File.Delete(tempWavFile); } catch { }
                    return "MD5 calculation failed";
                }

                // Verify temp file was created
                if (!File.Exists(tempWavFile))
                {
                    UpdateCacheWithMD5Error(flacFilePath, "Temporary WAV file was not created");
                    return "MD5 calculation failed";
                }

                // Create a chache entry for temp WAV file
                var tempFileInfo = new FileInfo(tempWavFile);
                var tempCachedInfo = audioFileInfoCache.GetOrAdd(tempWavFile, _ => new AudioFileInfo
                {
                    FilePath = tempWavFile,
                    DirectoryPath = tempFileInfo.DirectoryName!,
                    FileName = tempFileInfo.Name,
                    FileNameWithoutExtension = Path.GetFileNameWithoutExtension(tempWavFile),
                    Extension = ".wav",
                    Channels = string.Empty,
                    BitDepth = string.Empty,
                    BitDepthString = string.Empty,
                    SamplingRate = string.Empty,
                    SamplingRateString = string.Empty,
                    Duration = string.Empty,
                    FileSize = tempFileInfo.Length,
                    Md5Hash = string.Empty,           // Will be computed
                    CreationTime = tempFileInfo.CreationTimeUtc,
                    LastWriteTime = tempFileInfo.LastWriteTimeUtc,
                    ErrorDetails = string.Empty,
                    WritingLibrary = string.Empty
                });

                string wavMd5Result = await CalculateWavMD5Async(tempWavFile);

                if (wavMd5Result == "MD5 calculation failed")
                {
                    // Retrieve detailed error from the temp file's cache entry
                    string tempWavErrorDetails = tempCachedInfo.ErrorDetails;

                    if (string.IsNullOrEmpty(tempWavErrorDetails))
                    {
                        tempWavErrorDetails = "Unknown error during MD5 calculation of decoded WAV";
                    }

                    // Propagate error to the original FLAC file's cache entry
                    UpdateCacheWithMD5Error(flacFilePath, tempWavErrorDetails);

                    // Remove temp file entry from cache
                    audioFileInfoCache.TryRemove(tempWavFile, out _);

                    return "MD5 calculation failed";
                }

                // Success: update original FLAC file's cache entry
                var cachedInfo = audioFileInfoCache[flacFilePath];
                cachedInfo.Md5Hash = wavMd5Result;
                cachedInfo.ErrorDetails = string.Empty;

                // Clean up temp file and its cache entry
                try { File.Delete(tempWavFile); } catch { }
                audioFileInfoCache.TryRemove(tempWavFile, out _);

                return wavMd5Result;
            }
            catch (Exception ex)
            {
                UpdateCacheWithMD5Error(flacFilePath, $"Error: {ex.Message}");
                return "MD5 calculation failed";
            }
        }
        private void UpdateCacheWithMD5Error(string filePath, string errorDetails)
        {
            var cachedInfo = audioFileInfoCache[filePath];
            cachedInfo.Md5Hash = "MD5 calculation failed";
            cachedInfo.ErrorDetails = errorDetails;
        }

        private void ButtonUpAudioFile_Click(object? sender, EventArgs e)
        {
            MoveSelectedItemsForListview(listViewAudioFiles, -1); // Pass -1 to move up
        }
        private void ButtonDownAudioFile_Click(object? sender, EventArgs e)
        {
            MoveSelectedItemsForListview(listViewAudioFiles, 1); // Pass 1 to move down
        }
        private void ButtonClearSelectedAudioFile_Click(object? sender, EventArgs e)
        {
            if (ModifierKeys == Keys.Shift)
            {
                MoveListViewItemToRecycleBin(listViewAudioFiles, item => item.Selected, "selected audio files", UpdateGroupBoxAudioFilesHeader);
                return;
            }

            for (int i = listViewAudioFiles.Items.Count - 1; i >= 0; i--)
            {
                if (listViewAudioFiles.Items[i].Selected)
                {
                    listViewAudioFiles.Items.RemoveAt(i);
                }
            }
            UpdateGroupBoxAudioFilesHeader();
        }
        private void ButtonClearUnchecked_Click(object? sender, EventArgs e)
        {
            if (ModifierKeys == Keys.Shift)
            {
                MoveListViewItemToRecycleBin(listViewAudioFiles, item => !item.Checked, "unchecked audio files", UpdateGroupBoxAudioFilesHeader);
                return;
            }

            for (int i = listViewAudioFiles.Items.Count - 1; i >= 0; i--)
            {
                if (!listViewAudioFiles.Items[i].Checked)
                {
                    listViewAudioFiles.Items.RemoveAt(i);
                }
            }
            UpdateGroupBoxAudioFilesHeader();
        }
        private void ButtonClearAudioFiles_Click(object? sender, EventArgs e)
        {
            listViewAudioFiles.Items.Clear();
            UpdateGroupBoxAudioFilesHeader();
        }
        private void UpdateGroupBoxAudioFilesHeader()
        {
            string baseText = "Choose Audio Files (Drag'n'Drop of files and folders is available)";
            int fileCount = listViewAudioFiles.Items.Count;

            if (fileCount > 0)
            {
                string fileWord = fileCount == 1 ? "file" : "files";
                groupBoxAudioFiles.Text = $"{baseText} - {fileCount} {fileWord} loaded";
            }
            else
            {
                groupBoxAudioFiles.Text = baseText;
            }
        }

        // Audio Files Context Menu
        private void ContextMenuStripAudioFiles_Opening(object sender, CancelEventArgs e)
        {
            UpdateMenuItemCheckAndSelectState();
        }
        private void UpdateMenuItemCheckAndSelectState()
        {
            var items = listViewAudioFiles.Items;
            int totalItemsCount = items.Count;
            int selectedItemsCount = listViewAudioFiles.SelectedItems.Count;

            bool hasItems = totalItemsCount > 0;
            bool hasSelectedItems = selectedItemsCount > 0;
            bool isBusy = false;

            bool hasCheckedItems = false;
            bool hasUncheckedItems = false;
            bool hasSelectedCheckedItems = false;
            bool hasSelectedUncheckedItems = false;
            bool hasFLAC = false;
            bool hasWAV = false;

            int flacTotal = 0, flacChecked = 0, flacSelected = 0;
            int wavTotal = 0, wavChecked = 0, wavSelected = 0;
            int allChecked = 0, allSelected = 0;

            foreach (ListViewItem item in items)
            {
                string filePath = item.Tag!.ToString()!;
                string extension = audioFileInfoCache[filePath].Extension;

                if (item.Checked) hasCheckedItems = true; else hasUncheckedItems = true;
                if (item.Checked) allChecked++;
                if (item.Selected) allSelected++;

                if (item.Selected)
                {
                    if (item.Checked) hasSelectedCheckedItems = true; else hasSelectedUncheckedItems = true;
                }

                if (extension == ".flac")
                {
                    hasFLAC = true;
                    flacTotal++;
                    if (item.Checked) flacChecked++;
                    if (item.Selected) flacSelected++;
                }
                else if (extension == ".wav")
                {
                    hasWAV = true;
                    wavTotal++;
                    if (item.Checked) wavChecked++;
                    if (item.Selected) wavSelected++;
                }

                if (hasCheckedItems && hasUncheckedItems && hasSelectedCheckedItems &&
                    hasSelectedUncheckedItems && hasFLAC && hasWAV &&
                    flacChecked > 0 && flacChecked < flacTotal &&
                    wavChecked > 0 && wavChecked < wavTotal)
                {
                    break;
                }
            }

            // Enabled
            checkAllToolStripMenuItemAudioFiles.Enabled = hasItems && !isBusy;
            checkAllAllAudioFilesToolStripMenuItemAudioFiles.Enabled = hasItems && !isBusy;
            checkAllFLACToolStripMenuItemAudioFiles.Enabled = hasFLAC && !isBusy;
            checkAllWAVToolStripMenuItemAudioFiles.Enabled = hasWAV && !isBusy;
            uncheckAllToolStripMenuItemAudioFiles.Enabled = hasCheckedItems && !isBusy;
            checkSelectedToolStripMenuItemAudioFiles.Enabled = hasSelectedUncheckedItems && !isBusy;
            uncheckSelectedToolStripMenuItemAudioFiles.Enabled = hasSelectedCheckedItems && !isBusy;
            invertCheckToolStripMenuItemAudioFiles.Enabled = hasItems && !isBusy;

            selectAllToolStripMenuItemAudioFiles.Enabled = hasItems && !isBusy;
            selectAllAllAudioFilesToolStripMenuItemAudioFiles.Enabled = hasItems && !isBusy;
            selectAllFLACToolStripMenuItemAudioFiles.Enabled = hasFLAC && !isBusy;
            selectAllWAVToolStripMenuItemAudioFiles.Enabled = hasWAV && !isBusy;
            clearSelectionToolStripMenuItemAudioFiles.Enabled = hasSelectedItems;
            invertSelectionToolStripMenuItemAudioFiles.Enabled = hasItems && !isBusy;

            moveUpToolStripMenuItemAudioFiles.Enabled = hasSelectedItems && !isBusy;
            moveDownToolStripMenuItemAudioFiles.Enabled = hasSelectedItems && !isBusy;

            refreshAllToolStripMenuItemAudioFiles.Enabled = hasItems && !isBusy;
            openContainingFolderToolStripMenuItemAudioFiles.Enabled = hasSelectedItems;

            toolsToolStripMenuItemAudioFiles.Enabled = true;
            detectDupesToolStripMenuItemAudioFiles.Enabled = hasItems && !isBusy;
            testForErrorsToolStripMenuItemAudioFIles.Enabled = hasFLAC && !isBusy;
            warningsAsErrorsToolStripMenuItemAudioFiles.Enabled = true;
            summaryToolStripMenuItemAudioFiles.Enabled = hasItems && !isBusy;

            clearUncheckedToolStripMenuItemAudioFiles.Enabled = hasUncheckedItems && !isBusy;
            clearSelectedToolStripMenuItemAudioFiles.Enabled = hasSelectedItems;
            clearDuplicateEntriesToolStripMenuItemAudioFiles.Enabled = hasItems && !isBusy;
            clearAllToolStripMenuItemAudioFiles.Enabled = true;

            // CheckState
            checkAllFLACToolStripMenuItemAudioFiles.CheckState = GetCheckStateForContextMenuItem(flacChecked, flacTotal);
            checkAllWAVToolStripMenuItemAudioFiles.CheckState = GetCheckStateForContextMenuItem(wavChecked, wavTotal);
            checkAllAllAudioFilesToolStripMenuItemAudioFiles.CheckState = GetCheckStateForContextMenuItem(allChecked, totalItemsCount);

            selectAllFLACToolStripMenuItemAudioFiles.CheckState = GetCheckStateForContextMenuItem(flacSelected, flacTotal);
            selectAllWAVToolStripMenuItemAudioFiles.CheckState = GetCheckStateForContextMenuItem(wavSelected, wavTotal);
            selectAllAllAudioFilesToolStripMenuItemAudioFiles.CheckState = GetCheckStateForContextMenuItem(allSelected, totalItemsCount);

            // Update menu items
            checkAllToolStripMenuItemAudioFiles.DropDown.Invalidate();
            selectAllToolStripMenuItemAudioFiles.DropDown.Invalidate();
            toolsToolStripMenuItemAudioFiles.DropDown.Invalidate();
        }
        private void CheckAllToolStripMenuItemAudioFiles_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem item in listViewAudioFiles.Items)
            {
                item.Checked = true;
            }
            UpdateMenuItemCheckAndSelectState();
        }
        private void CheckAllAllAudioFilesToolStripMenuItemAudioFiles_Click(object sender, EventArgs e)
        {
            if (listViewAudioFiles.Items.Count == 0) return;

            bool allChecked = listViewAudioFiles.Items.Cast<ListViewItem>().All(item => item.Checked);

            foreach (ListViewItem item in listViewAudioFiles.Items)
            {
                item.Checked = !allChecked;
            }

            UpdateMenuItemCheckAndSelectState();
        }
        private void CheckAllFLACToolStripMenuItemAudioFiles_Click(object sender, EventArgs e)
        {
            var flacItems = listViewAudioFiles.Items
                .Cast<ListViewItem>()
                .Where(item => audioFileInfoCache[item.Tag!.ToString()!].Extension == ".flac")
                .ToList();

            if (flacItems.Count == 0) return;

            bool allChecked = flacItems.All(item => item.Checked);

            foreach (var item in flacItems)
            {
                item.Checked = !allChecked;
            }

            UpdateMenuItemCheckAndSelectState();
        }
        private void CheckAllWAVToolStripMenuItemAudioFiles_Click(object sender, EventArgs e)
        {
            var wavItems = listViewAudioFiles.Items
                .Cast<ListViewItem>()
                .Where(item => audioFileInfoCache[item.Tag!.ToString()!].Extension == ".wav")
                .ToList();

            if (wavItems.Count == 0) return;

            bool allChecked = wavItems.All(item => item.Checked);

            foreach (var item in wavItems)
            {
                item.Checked = !allChecked;
            }

            UpdateMenuItemCheckAndSelectState();
        }
        private void UncheckAllToolStripMenuItemAudioFiles_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem item in listViewAudioFiles.Items)
            {
                item.Checked = false;
            }
        }
        private void CheckSelectedToolStripMenuItemAudioFiles_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem item in listViewAudioFiles.SelectedItems)
            {
                item.Checked = true;
            }
        }
        private void UncheckSelectedToolStripMenuItemAudioFiles_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem item in listViewAudioFiles.SelectedItems)
            {
                item.Checked = false;
            }
        }
        private void InvertCheckToolStripMenuItemAudioFiles_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem item in listViewAudioFiles.Items)
            {
                item.Checked = !item.Checked;
            }
        }
        private void SelectAllToolStripMenuItemAudioFiles_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem item in listViewAudioFiles.Items)
            {
                item.Selected = true;
            }
            UpdateMenuItemCheckAndSelectState();
        }
        private void SelectAllAllAudioFilesToolStripMenuItemAudioFiles_Click(object sender, EventArgs e)
        {
            if (listViewAudioFiles.Items.Count == 0) return;

            bool allSelected = listViewAudioFiles.Items.Cast<ListViewItem>().All(item => item.Selected);

            foreach (ListViewItem item in listViewAudioFiles.Items)
            {
                item.Selected = !allSelected;
            }
            UpdateMenuItemCheckAndSelectState();
        }
        private void SelectAllFLACToolStripMenuItemAudioFiles_Click(object sender, EventArgs e)
        {
            var flacItems = listViewAudioFiles.Items
                .Cast<ListViewItem>()
                .Where(item => audioFileInfoCache[item.Tag!.ToString()!].Extension == ".flac")
                .ToList();

            if (flacItems.Count == 0) return;

            bool allSelected = flacItems.All(item => item.Selected);

            foreach (var item in flacItems)
            {
                item.Selected = !allSelected;
            }
            UpdateMenuItemCheckAndSelectState();
        }
        private void SelectAllWAVToolStripMenuItemAudioFiles_Click(object sender, EventArgs e)
        {
            var wavItems = listViewAudioFiles.Items
                .Cast<ListViewItem>()
                .Where(item => audioFileInfoCache[item.Tag!.ToString()!].Extension == ".wav")
                .ToList();

            if (wavItems.Count == 0) return;

            bool allSelected = wavItems.All(item => item.Selected);

            foreach (var item in wavItems)
            {
                item.Selected = !allSelected;
            }
            UpdateMenuItemCheckAndSelectState();
        }
        private void ClearSelectionToolStripMenuItemAudioFiles_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem item in listViewAudioFiles.SelectedItems)
            {
                item.Selected = false;
            }
        }
        private void InvertSelectionToolStripMenuItemAudioFiles_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem item in listViewAudioFiles.Items)
            {
                item.Selected = !item.Selected;
            }
        }
        private void MoveUpToolStripMenuItemAudioFiles_Click(object sender, EventArgs e)
        {
            MoveSelectedItemsForListview(listViewAudioFiles, -1);
        }
        private void MoveDownToolStripMenuItemAudioFiles_Click(object sender, EventArgs e)
        {
            MoveSelectedItemsForListview(listViewAudioFiles, 1);
        }
        private async void RefreshAllToolStripMenuItemAudioFiles_Click(object? sender, EventArgs e)
        {
            if (/*_isProcessingAudioFilesQueue || _isRefreshingAudioFiles || */listViewAudioFiles.Items.Count == 0)
                return;

            //_isRefreshingAudioFiles = true;
            groupBoxAudioFiles.Text = "Choose Audio Files (Drag'n'Drop of files and folders is available) - Refreshing...";
            Application.DoEvents();

            int topIndex = listViewAudioFiles.TopItem?.Index ?? 0;
            var selectedIndices = new List<int>();
            foreach (ListViewItem item in listViewAudioFiles.SelectedItems)
            {
                selectedIndices.Add(item.Index);
            }

            try
            {
                for (int i = listViewAudioFiles.Items.Count - 1; i >= 0; i--)
                {
                    ListViewItem item = listViewAudioFiles.Items[i];
                    string audioFilePath = item.Tag!.ToString()!;

                    if (!File.Exists(audioFilePath))
                    {
                        listViewAudioFiles.Items.RemoveAt(i);
                        continue;
                    }

                    bool currentChecked = item.Checked;

                    var fileInfo = new FileInfo(audioFilePath);
                    DateTime currentCreationTime = fileInfo.CreationTimeUtc;
                    DateTime currentLastWriteTime = fileInfo.LastWriteTimeUtc;

                    var cachedInfo = audioFileInfoCache[audioFilePath];

                    bool wasModified = cachedInfo.CreationTime != currentCreationTime ||
                                       cachedInfo.LastWriteTime != currentLastWriteTime;

                    var newItem = await CreateListViewAudioFilesItemInternal(audioFilePath, currentChecked);
                    if (newItem == null)
                    {
                        listViewAudioFiles.Items.RemoveAt(i);
                        continue;
                    }

                    audioFileInfoCache[audioFilePath].WasModifiedSinceLoad = wasModified;


                    item.Text = newItem.Text;
                    for (int j = 0; j < newItem.SubItems.Count - 1; j++)
                    {
                        if (j + 1 < item.SubItems.Count)
                            item.SubItems[j + 1].Text = newItem.SubItems[j + 1].Text;
                    }

                    item.ForeColor = wasModified ? Color.DarkOrange : SystemColors.WindowText;
                    item.ToolTipText = wasModified
                        ? "Audio file was modified since it was loaded.\nRefresh again to decolorize."
                        : "";
                }

                //SaveAudioFiles();
            }
            finally
            {
                if (listViewAudioFiles.Items.Count > 0 && topIndex < listViewAudioFiles.Items.Count)
                {
                    try
                    {
                        listViewAudioFiles.TopItem = listViewAudioFiles.Items[topIndex];
                    }
                    catch
                    {
                        // Ignore if index is invalid
                    }
                }

                foreach (int index in selectedIndices)
                {
                    if (index < listViewAudioFiles.Items.Count)
                    {
                        listViewAudioFiles.Items[index].Selected = true;
                    }
                }

                //_isRefreshingAudioFiles = false;
                UpdateGroupBoxAudioFilesHeader();
            }
        }
        private void OpenContainingFolderToolStripMenuItemAudioFiles_Click(object sender, EventArgs e)
        {
            if (listViewAudioFiles.SelectedItems.Count == 0)
                return;

            foreach (ListViewItem selectedItem in listViewAudioFiles.SelectedItems)
            {
                string? audioFilePath = selectedItem.Tag?.ToString();

                if (string.IsNullOrEmpty(audioFilePath))
                    continue;

                if (!File.Exists(audioFilePath))
                {
                    MessageBox.Show($"The selected audio file no longer exists on disk:\n\n{audioFilePath}\n\n" +
                                   "You can remove it from the list using 'Clear Selected' or 'Refresh'.",
                                   "File Not Found",
                                   MessageBoxButtons.OK,
                                   MessageBoxIcon.Warning);
                    continue;
                }

                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "explorer.exe",
                        Arguments = $"/select,\"{audioFilePath}\"",
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to open folder for {Path.GetFileName(audioFilePath)}:\n{ex.Message}",
                                   "Error",
                                   MessageBoxButtons.OK,
                                   MessageBoxIcon.Error);
                }
            }
        }
        private void DetectDupesToolStripMenuItemAudioFiles_Click(object sender, EventArgs e)
        {
            ButtonDetectDupesAudioFiles_Click(sender, e);
        }
        private void TestForErrorsToolStripMenuItemAudioFIles_Click(object sender, EventArgs e)
        {
            ButtonTestForErrors_Click(sender, e);
        }
        private void WarningsAsErrorsToolStripMenuItemAudioFiles_Click(object sender, EventArgs e)
        {
            checkBoxWarningsAsErrors.Checked = !checkBoxWarningsAsErrors.Checked;
        }
        private void SummaryToolStripMenuItemAudioFiles_Click(object sender, EventArgs e)
        {
            ShowAudioFilesSummary();
        }
        private void ClearUncheckedToolStripMenuItemAudioFiles_Click(object sender, EventArgs e)
        {
            ButtonClearUnchecked_Click(sender, e);
        }
        private void ClearSelectedToolStripMenuItemAudioFiles_Click(object sender, EventArgs e)
        {
            ButtonClearSelectedAudioFile_Click(sender, e);
        }
        private void ClearDuplicateEntriesToolStripMenuItemAudioFiles_Click(object sender, EventArgs e)
        {
            if (listViewAudioFiles.Items.Count == 0)
                return;

            var seenPaths = new HashSet<string>();
            var itemsToRemove = new List<ListViewItem>();

            foreach (ListViewItem item in listViewAudioFiles.Items)
            {
                string? audioFilePath = item.Tag?.ToString();
                if (string.IsNullOrEmpty(audioFilePath))
                    continue;

                if (!seenPaths.Add(audioFilePath))
                {
                    itemsToRemove.Add(item);
                }
            }

            if (itemsToRemove.Count > 0)
            {
                foreach (var item in itemsToRemove)
                {
                    listViewAudioFiles.Items.Remove(item);
                }
                UpdateGroupBoxAudioFilesHeader();

                MessageBox.Show(
                    $"Cleared {itemsToRemove.Count} duplicate entr{(itemsToRemove.Count == 1 ? "y" : "ies")}.",
                    "Duplicates cleared",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
        }
        private void ClearAllToolStripMenuItemAudioFiles_Click(object sender, EventArgs e)
        {
            ButtonClearAudioFiles_Click(sender, e);
        }

        // Generates and displays the summary report for audio files in listViewAudioFiles.
        private void ShowAudioFilesSummary()
        {
            static string FormatDuration(long totalMilliseconds)
            {
                if (totalMilliseconds <= 0) return "0:00:00";
                var duration = TimeSpan.FromMilliseconds(totalMilliseconds);
                return duration.TotalDays < 1
                    ? duration.ToString(@"hh\:mm\:ss\.fff")
                    : $"{(int)duration.TotalDays} {(duration.TotalDays == 1 ? "day" : "days")} {duration:hh\\:mm\\:ss\\.fff}";
            }

            var items = listViewAudioFiles.Items.Cast<ListViewItem>().ToList();

            // === FILTER BY EXTENSION ===
            var flacItems = items.Where(i => audioFileInfoCache[i.Tag!.ToString()!].Extension == ".flac").ToList();
            var wavItems = items.Where(i => audioFileInfoCache[i.Tag!.ToString()!].Extension == ".wav").ToList();

            // === TOTAL STATISTICS ===
            int totalFiles = items.Count;
            long totalSize = items.Sum(i => audioFileInfoCache[i.Tag!.ToString()!].FileSize);
            long totalDurationMs = items.Sum(i => long.TryParse(audioFileInfoCache[i.Tag!.ToString()!].Duration, out var d) ? d : 0);
            string totalDurationFormatted = FormatDuration(totalDurationMs);

            // === FLAC STATISTICS ===
            int flacFiles = flacItems.Count;
            long flacSize = flacItems.Sum(i => audioFileInfoCache[i.Tag!.ToString()!].FileSize);
            long flacDurationMs = flacItems.Sum(i => long.TryParse(audioFileInfoCache[i.Tag!.ToString()!].Duration, out var d) ? d : 0);
            string flacDurationFormatted = FormatDuration(flacDurationMs);

            // === WAV STATISTICS ===
            int wavFiles = wavItems.Count;
            long wavSize = wavItems.Sum(i => audioFileInfoCache[i.Tag!.ToString()!].FileSize);
            long wavDurationMs = wavItems.Sum(i => long.TryParse(audioFileInfoCache[i.Tag!.ToString()!].Duration, out var d) ? d : 0);
            string wavDurationFormatted = FormatDuration(wavDurationMs);

            // === PERCENTAGES ===
            double flacFilesPercent = totalFiles > 0 ? (double)flacFiles / totalFiles * 100 : 0;
            double flacSizePercent = totalSize > 0 ? (double)flacSize / totalSize * 100 : 0;
            double flacDurationPercent = totalDurationMs > 0 ? (double)flacDurationMs / totalDurationMs * 100 : 0;

            double wavFilesPercent = totalFiles > 0 ? (double)wavFiles / totalFiles * 100 : 0;
            double wavSizePercent = totalSize > 0 ? (double)wavSize / totalSize * 100 : 0;
            double wavDurationPercent = totalDurationMs > 0 ? (double)wavDurationMs / totalDurationMs * 100 : 0;

            // === AUDIO PROPERTIES (filter N/A) ===
            var samplingRates = items
                .Select(i => audioFileInfoCache[i.Tag!.ToString()!].SamplingRateString)
                .Where(sr => !string.IsNullOrEmpty(sr) && sr != "N/A")
                .GroupBy(sr => sr)
                .OrderByDescending(g => g.Count())
                .Select(g => $"{g.Key} ({g.Count()})")
                .ToList();

            var bitDepths = items
                .Select(i => audioFileInfoCache[i.Tag!.ToString()!].BitDepthString)
                .Where(bd => !string.IsNullOrEmpty(bd) && bd != "N/A")
                .GroupBy(bd => bd)
                .OrderByDescending(g => g.Count())
                .Select(g => $"{g.Key} ({g.Count()})")
                .ToList();

            var channels = items
                .Select(i => audioFileInfoCache[i.Tag!.ToString()!].Channels)
                .Where(ch => !string.IsNullOrEmpty(ch) && ch != "N/A")
                .GroupBy(ch => ch)
                .OrderByDescending(g => g.Count())
                .Select(g => $"{g.Key} ({g.Count()})")
                .ToList();

            var writingLibraries = flacItems
                .Select(i => audioFileInfoCache[i.Tag!.ToString()!].WritingLibrary)
                .Where(wl => !string.IsNullOrEmpty(wl) && wl != "N/A")
                .GroupBy(wl => wl)
                .OrderByDescending(g => g.Count())
                .Select(g => $"{g.Key} ({g.Count()})")
                .ToList();

            // === PROBLEMATIC FILES ===
            var longPathItems = items
                .Where(i => audioFileInfoCache[i.Tag!.ToString()!].FilePath.Length >= 260)
                .Select(i => audioFileInfoCache[i.Tag!.ToString()!].FilePath)
                .OrderBy(path => path, new NaturalStringComparer()).ToList();

            var filesWithoutMd5List = items
                .Where(i => audioFileInfoCache[i.Tag!.ToString()!].Md5HashMissing)
                .Select(i => audioFileInfoCache[i.Tag!.ToString()!].FilePath)
                .OrderBy(path => path, new NaturalStringComparer()).ToList();

            var filesWithMd5Errors = items
                .Where(i => audioFileInfoCache[i.Tag!.ToString()!].Md5Hash == "MD5 calculation failed")
                .Select(i => audioFileInfoCache[i.Tag!.ToString()!].FilePath)
                .OrderBy(path => path, new NaturalStringComparer()).ToList();

            var filesWithoutChannels = items
                .Where(i =>
                {
                    var info = audioFileInfoCache[i.Tag!.ToString()!];
                    return string.IsNullOrEmpty(info.Channels) || info.Channels == "N/A";
                })
                .Select(i => audioFileInfoCache[i.Tag!.ToString()!].FilePath)
                .OrderBy(path => path, new NaturalStringComparer()).ToList();

            var filesWithoutSamplingRate = items
                .Where(i =>
                {
                    var info = audioFileInfoCache[i.Tag!.ToString()!];
                    return string.IsNullOrEmpty(info.SamplingRate) || info.SamplingRate == "N/A";
                })
                .Select(i => audioFileInfoCache[i.Tag!.ToString()!].FilePath)
                .OrderBy(path => path, new NaturalStringComparer()).ToList();

            var filesWithoutBitDepth = items
                .Where(i =>
                {
                    var info = audioFileInfoCache[i.Tag!.ToString()!];
                    return string.IsNullOrEmpty(info.BitDepth) || info.BitDepth == "N/A";
                })
                .Select(i => audioFileInfoCache[i.Tag!.ToString()!].FilePath)
                .OrderBy(path => path, new NaturalStringComparer()).ToList();

            var filesWithoutDuration = items
                .Where(i =>
                {
                    var info = audioFileInfoCache[i.Tag!.ToString()!];
                    return string.IsNullOrEmpty(info.Duration) || info.Duration == "N/A";
                })
                .Select(i => audioFileInfoCache[i.Tag!.ToString()!].FilePath)
                .OrderBy(path => path, new NaturalStringComparer()).ToList();

            // === PASS TO SUMMARY FORM ===
            var summaryForm = new SummaryForm();
            summaryForm.SetSummaryData(

                // === TOTAL ===
                totalFiles,
                totalSize,
                totalDurationFormatted,

                // === FLAC ===
                flacFiles,
                flacSize,
                flacFilesPercent,
                flacSizePercent,
                flacDurationFormatted,
                flacDurationPercent,

                // === WAV ===
                wavFiles,
                wavSize,
                wavFilesPercent,
                wavSizePercent,
                wavDurationFormatted,
                wavDurationPercent,

                // === METADATA ===
                samplingRates,
                bitDepths,
                channels,
                filesWithoutMd5List,
                filesWithMd5Errors,
                longPathItems,
                writingLibraries,
                filesWithoutChannels,
                filesWithoutSamplingRate,
                filesWithoutBitDepth,
                filesWithoutDuration
            );
            summaryForm.Show(this);
        }

        // Log Settings (now supports only "Benchmark" tab)
        private DataGridViewLogSettingsForm? _logSettingsForm = null;
        private void ButtonDataGridViewLogSettings_Click(object? sender, EventArgs e)
        {
            if (_logSettingsForm == null || _logSettingsForm.IsDisposed)
            {
                _logSettingsForm = new DataGridViewLogSettingsForm(dataGridViewLog);
                _logSettingsForm.FormClosed += (s, args) => _logSettingsForm = null;
                _logSettingsForm.Show(this);
            }
            else
            {
                _logSettingsForm.BringToFront();
                _logSettingsForm.Focus();
            }
        }

        // Log for "Benchmark"
        private void InitializeDataGridViewLog()
        {
            // Configure DataGridView
            dataGridViewLog.Columns.Add("Name", "Name");
            dataGridViewLog.Columns.Add("Channels", "Ch.");
            dataGridViewLog.Columns.Add("BitDepth", "Bit Depth");
            dataGridViewLog.Columns.Add("SamplingRate", "Samp. Rate");
            dataGridViewLog.Columns.Add("InputFileSize", "In. Size");
            dataGridViewLog.Columns.Add("OutputFileSize", "Out. Size");
            dataGridViewLog.Columns.Add("Compression", "Compr.");
            dataGridViewLog.Columns.Add("Time", "Time");
            dataGridViewLog.Columns.Add("Speed", "Speed");
            dataGridViewLog.Columns.Add("SpeedMin", "Speed Min.");
            dataGridViewLog.Columns.Add("SpeedMax", "Speed Max.");
            dataGridViewLog.Columns.Add("SpeedRange", "Range");
            dataGridViewLog.Columns.Add("SpeedConsistency", "Speed Consistency");
            dataGridViewLog.Columns.Add("CPULoadEncoder", "CPU Load");
            dataGridViewLog.Columns.Add("CPUClock", "CPU Clock");
            dataGridViewLog.Columns.Add("Passes", "Passes");
            dataGridViewLog.Columns.Add("Parameters", "Parameters");
            dataGridViewLog.Columns.Add("Encoder", "Encoder");
            dataGridViewLog.Columns.Add("Version", "Version");

            var encoderDirectoryColumn = new DataGridViewLinkColumn
            {
                Name = "EncoderDirectory",
                HeaderText = "Encoder Directory",
                DataPropertyName = "EncoderDirectory"
            };
            dataGridViewLog.Columns.Add(encoderDirectoryColumn);

            dataGridViewLog.Columns.Add("FastestEncoder", "Fastest Encoder");
            dataGridViewLog.Columns.Add("BestSize", "Best Size");
            dataGridViewLog.Columns.Add("SameSize", "Same Size");

            var audioFileDirectoryColumn = new DataGridViewLinkColumn
            {
                Name = "AudioFileDirectory",
                HeaderText = "Audio File Directory",
                DataPropertyName = "AudioFileDirectory"
            };
            dataGridViewLog.Columns.Add(audioFileDirectoryColumn);

            dataGridViewLog.Columns.Add("MD5", "MD5");
            dataGridViewLog.Columns.Add("Duplicates", "Duplicates");
            dataGridViewLog.Columns.Add("Errors", "Errors");

            // Set alignment for columns
            dataGridViewLog.Columns["Channels"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewLog.Columns["BitDepth"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewLog.Columns["SamplingRate"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewLog.Columns["InputFileSize"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewLog.Columns["OutputFileSize"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewLog.Columns["Compression"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewLog.Columns["Time"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewLog.Columns["Speed"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewLog.Columns["SpeedMin"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewLog.Columns["SpeedMax"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewLog.Columns["SpeedRange"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewLog.Columns["SpeedConsistency"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewLog.Columns["CPULoadEncoder"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewLog.Columns["CPUClock"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewLog.Columns["Passes"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

            // Hide or show columns by default
            dataGridViewLog.Columns["Name"]!.Visible = true;
            dataGridViewLog.Columns["Channels"]!.Visible = false;
            dataGridViewLog.Columns["BitDepth"]!.Visible = false;
            dataGridViewLog.Columns["SamplingRate"]!.Visible = false;
            dataGridViewLog.Columns["InputFileSize"]!.Visible = true;
            dataGridViewLog.Columns["OutputFileSize"]!.Visible = true;
            dataGridViewLog.Columns["Compression"]!.Visible = true;
            dataGridViewLog.Columns["Time"]!.Visible = false;
            dataGridViewLog.Columns["Speed"]!.Visible = true;
            dataGridViewLog.Columns["SpeedMin"]!.Visible = false;
            dataGridViewLog.Columns["SpeedMax"]!.Visible = false;
            dataGridViewLog.Columns["SpeedRange"]!.Visible = false;
            dataGridViewLog.Columns["SpeedConsistency"]!.Visible = false;
            dataGridViewLog.Columns["CPULoadEncoder"]!.Visible = true;
            dataGridViewLog.Columns["CPUClock"]!.Visible = true;
            dataGridViewLog.Columns["Passes"]!.Visible = true;
            dataGridViewLog.Columns["Parameters"]!.Visible = true;
            dataGridViewLog.Columns["Encoder"]!.Visible = true;
            dataGridViewLog.Columns["Version"]!.Visible = false;
            dataGridViewLog.Columns["EncoderDirectory"]!.Visible = false;
            dataGridViewLog.Columns["FastestEncoder"]!.Visible = true;
            dataGridViewLog.Columns["BestSize"]!.Visible = true;
            dataGridViewLog.Columns["SameSize"]!.Visible = true;
            dataGridViewLog.Columns["AudioFileDirectory"]!.Visible = true;
            dataGridViewLog.Columns["MD5"]!.Visible = false;
            dataGridViewLog.Columns["Duplicates"]!.Visible = false;
            dataGridViewLog.Columns["Errors"]!.Visible = false;

            foreach (DataGridViewColumn column in dataGridViewLog.Columns)
            {
                column.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }
        }
        private void DataGridViewLog_CellContentClick(object? sender, DataGridViewCellEventArgs e)
        {
            // Prevent interaction with the new row or invalid cell
            if (e.RowIndex < 0) return;

            string columnName = dataGridViewLog.Columns[e.ColumnIndex].Name;

            // 1. Handle click on "AudioFileDirectory" column - path to the audio file directory
            if (columnName == "AudioFileDirectory")
            {
                string directoryPath = dataGridViewLog.Rows[e.RowIndex].Cells["AudioFileDirectory"].Value?.ToString() ?? string.Empty;
                string fileName = dataGridViewLog.Rows[e.RowIndex].Cells["Name"].Value?.ToString() ?? string.Empty;

                if (!string.IsNullOrEmpty(directoryPath) && !string.IsNullOrEmpty(fileName))
                {
                    string fullPath = Path.Combine(directoryPath, fileName);
                    if (File.Exists(fullPath))
                    {
                        // Highlight the audio file in Explorer
                        Process.Start("explorer.exe", $"/select,\"{fullPath}\"");
                    }
                    else if (Directory.Exists(directoryPath))
                    {
                        // Open directory if file not found
                        Process.Start("explorer.exe", $"\"{directoryPath}\"");
                    }
                }
            }

            // 2. Handle click on "EncoderDirectory" column - path to the encoder's folder
            else if (columnName == "EncoderDirectory")
            {
                string directoryPath = dataGridViewLog.Rows[e.RowIndex].Cells["EncoderDirectory"].Value?.ToString() ?? string.Empty;
                string encoderFileName = dataGridViewLog.Rows[e.RowIndex].Cells["Encoder"].Value?.ToString() ?? string.Empty;

                if (!string.IsNullOrEmpty(directoryPath) &&
                !string.IsNullOrEmpty(encoderFileName))
                {
                    string encoderExePath = Path.Combine(directoryPath, encoderFileName);
                    if (File.Exists(encoderExePath))
                    {
                        // Highlight the encoder .exe file in Explorer
                        Process.Start("explorer.exe", $"/select,\"{encoderExePath}\"");
                    }
                    else if (Directory.Exists(directoryPath))
                    {
                        // Open directory if .exe not found
                        Process.Start("explorer.exe", $"\"{directoryPath}\"");
                    }
                }
            }
        }
        private void DataGridViewLog_MouseDown(object? sender, MouseEventArgs e)
        {
            var hitTest = dataGridViewLog.HitTest(e.X, e.Y);
            if (hitTest.RowIndex == -1 && hitTest.ColumnIndex == -1)
            {
                dataGridViewLog.ClearSelection();
            }
        }

        // Log for "Detect Dupes"
        private void InitializeDataGridViewLogDetectDupes()
        {
            // Configure DataGridView
            dataGridViewLogDetectDupes.Columns.Add("Name", "Name");
            dataGridViewLogDetectDupes.Columns.Add("Channels", "Ch.");
            dataGridViewLogDetectDupes.Columns.Add("BitDepth", "Bit Depth");
            dataGridViewLogDetectDupes.Columns.Add("SamplingRate", "Samp. Rate");
            dataGridViewLogDetectDupes.Columns.Add("InputFileSize", "In. Size");
            dataGridViewLogDetectDupes.Columns.Add("OutputFileSize", "Out. Size");
            dataGridViewLogDetectDupes.Columns.Add("Compression", "Compr.");
            dataGridViewLogDetectDupes.Columns.Add("Time", "Time");
            dataGridViewLogDetectDupes.Columns.Add("Speed", "Speed");
            dataGridViewLogDetectDupes.Columns.Add("SpeedMin", "Speed Min.");
            dataGridViewLogDetectDupes.Columns.Add("SpeedMax", "Speed Max.");
            dataGridViewLogDetectDupes.Columns.Add("SpeedRange", "Range");
            dataGridViewLogDetectDupes.Columns.Add("SpeedConsistency", "Speed Consistency");
            dataGridViewLogDetectDupes.Columns.Add("CPULoadEncoder", "CPU Load");
            dataGridViewLogDetectDupes.Columns.Add("CPUClock", "CPU Clock");
            dataGridViewLogDetectDupes.Columns.Add("Passes", "Passes");
            dataGridViewLogDetectDupes.Columns.Add("Parameters", "Parameters");
            dataGridViewLogDetectDupes.Columns.Add("Encoder", "Encoder");
            dataGridViewLogDetectDupes.Columns.Add("Version", "Version");

            var encoderDirectoryColumn = new DataGridViewLinkColumn
            {
                Name = "EncoderDirectory",
                HeaderText = "Encoder Directory",
                DataPropertyName = "EncoderDirectory"
            };
            dataGridViewLogDetectDupes.Columns.Add(encoderDirectoryColumn);

            dataGridViewLogDetectDupes.Columns.Add("FastestEncoder", "Fastest Encoder");
            dataGridViewLogDetectDupes.Columns.Add("BestSize", "Best Size");
            dataGridViewLogDetectDupes.Columns.Add("SameSize", "Same Size");

            var audioFileDirectoryColumn = new DataGridViewLinkColumn
            {
                Name = "AudioFileDirectory",
                HeaderText = "Audio File Directory",
                DataPropertyName = "AudioFileDirectory"
            };
            dataGridViewLogDetectDupes.Columns.Add(audioFileDirectoryColumn);

            dataGridViewLogDetectDupes.Columns.Add("MD5", "MD5");
            dataGridViewLogDetectDupes.Columns.Add("Duplicates", "Duplicates");
            dataGridViewLogDetectDupes.Columns.Add("Errors", "Errors");

            // Set alignment for columns
            dataGridViewLogDetectDupes.Columns["Channels"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewLogDetectDupes.Columns["BitDepth"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewLogDetectDupes.Columns["SamplingRate"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewLogDetectDupes.Columns["InputFileSize"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewLogDetectDupes.Columns["OutputFileSize"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewLogDetectDupes.Columns["Compression"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewLogDetectDupes.Columns["Time"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewLogDetectDupes.Columns["Speed"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewLogDetectDupes.Columns["SpeedMin"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewLogDetectDupes.Columns["SpeedMax"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewLogDetectDupes.Columns["SpeedRange"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewLogDetectDupes.Columns["SpeedConsistency"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewLogDetectDupes.Columns["CPULoadEncoder"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewLogDetectDupes.Columns["CPUClock"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewLogDetectDupes.Columns["Passes"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

            // Hide or show columns by default
            dataGridViewLogDetectDupes.Columns["Name"]!.Visible = true;
            dataGridViewLogDetectDupes.Columns["Channels"]!.Visible = false;
            dataGridViewLogDetectDupes.Columns["BitDepth"]!.Visible = false;
            dataGridViewLogDetectDupes.Columns["SamplingRate"]!.Visible = false;
            dataGridViewLogDetectDupes.Columns["InputFileSize"]!.Visible = false;
            dataGridViewLogDetectDupes.Columns["OutputFileSize"]!.Visible = false;
            dataGridViewLogDetectDupes.Columns["Compression"]!.Visible = false;
            dataGridViewLogDetectDupes.Columns["Time"]!.Visible = false;
            dataGridViewLogDetectDupes.Columns["Speed"]!.Visible = false;
            dataGridViewLogDetectDupes.Columns["SpeedMin"]!.Visible = false;
            dataGridViewLogDetectDupes.Columns["SpeedMax"]!.Visible = false;
            dataGridViewLogDetectDupes.Columns["SpeedRange"]!.Visible = false;
            dataGridViewLogDetectDupes.Columns["SpeedConsistency"]!.Visible = false;
            dataGridViewLogDetectDupes.Columns["CPULoadEncoder"]!.Visible = false;
            dataGridViewLogDetectDupes.Columns["CPUClock"]!.Visible = false;
            dataGridViewLogDetectDupes.Columns["Passes"]!.Visible = false;
            dataGridViewLogDetectDupes.Columns["Parameters"]!.Visible = false;
            dataGridViewLogDetectDupes.Columns["Encoder"]!.Visible = false;
            dataGridViewLogDetectDupes.Columns["Version"]!.Visible = false;
            dataGridViewLogDetectDupes.Columns["EncoderDirectory"]!.Visible = false;
            dataGridViewLogDetectDupes.Columns["FastestEncoder"]!.Visible = false;
            dataGridViewLogDetectDupes.Columns["BestSize"]!.Visible = false;
            dataGridViewLogDetectDupes.Columns["SameSize"]!.Visible = false;
            dataGridViewLogDetectDupes.Columns["AudioFileDirectory"]!.Visible = true;
            dataGridViewLogDetectDupes.Columns["MD5"]!.Visible = true;
            dataGridViewLogDetectDupes.Columns["Duplicates"]!.Visible = true;
            dataGridViewLogDetectDupes.Columns["Errors"]!.Visible = true;

            foreach (DataGridViewColumn column in dataGridViewLogDetectDupes.Columns)
            {
                column.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }
        }
        private void DataGridViewLogDetectDupes_CellContentClick(object? sender, DataGridViewCellEventArgs e)
        {
            // Prevent interaction with the new row or invalid cell
            if (e.RowIndex < 0) return;

            string columnName = dataGridViewLogDetectDupes.Columns[e.ColumnIndex].Name;

            // 1. Handle click on "AudioFileDirectory" column - path to the audio file directory
            if (columnName == "AudioFileDirectory")
            {
                string directoryPath = dataGridViewLogDetectDupes.Rows[e.RowIndex].Cells["AudioFileDirectory"].Value?.ToString() ?? string.Empty;
                string fileName = dataGridViewLogDetectDupes.Rows[e.RowIndex].Cells["Name"].Value?.ToString() ?? string.Empty;

                if (!string.IsNullOrEmpty(directoryPath) && !string.IsNullOrEmpty(fileName))
                {
                    string fullPath = Path.Combine(directoryPath, fileName);
                    if (File.Exists(fullPath))
                    {
                        // Highlight the audio file in Explorer
                        Process.Start("explorer.exe", $"/select,\"{fullPath}\"");
                    }
                    else if (Directory.Exists(directoryPath))
                    {
                        // Open directory if file not found
                        Process.Start("explorer.exe", $"\"{directoryPath}\"");
                    }
                }
            }

            // 2. Handle click on "EncoderDirectory" column - path to the encoder's folder
            else if (columnName == "EncoderDirectory")
            {
                string directoryPath = dataGridViewLogDetectDupes.Rows[e.RowIndex].Cells["EncoderDirectory"].Value?.ToString() ?? string.Empty;
                string encoderFileName = dataGridViewLogDetectDupes.Rows[e.RowIndex].Cells["Encoder"].Value?.ToString() ?? string.Empty;

                if (!string.IsNullOrEmpty(directoryPath) && !string.IsNullOrEmpty(encoderFileName))
                {
                    string encoderExePath = Path.Combine(directoryPath, encoderFileName);
                    if (File.Exists(encoderExePath))
                    {
                        // Highlight the encoder .exe file in Explorer
                        Process.Start("explorer.exe", $"/select,\"{encoderExePath}\"");
                    }
                    else if (Directory.Exists(directoryPath))
                    {
                        // Open directory if .exe not found
                        Process.Start("explorer.exe", $"\"{directoryPath}\"");
                    }
                }
            }
        }
        private void DataGridViewLogDetectDupes_MouseDown(object? sender, MouseEventArgs e)
        {
            var hitTest = dataGridViewLogDetectDupes.HitTest(e.X, e.Y);
            if (hitTest.RowIndex == -1 && hitTest.ColumnIndex == -1)
            {
                dataGridViewLogDetectDupes.ClearSelection();
            }
        }

        // Log for "Test for Errors"
        private void InitializeDataGridViewLogTestForErrors()
        {
            // Configure DataGridView
            dataGridViewLogTestForErrors.Columns.Add("Name", "Name");
            dataGridViewLogTestForErrors.Columns.Add("Channels", "Ch.");
            dataGridViewLogTestForErrors.Columns.Add("BitDepth", "Bit Depth");
            dataGridViewLogTestForErrors.Columns.Add("SamplingRate", "Samp. Rate");
            dataGridViewLogTestForErrors.Columns.Add("InputFileSize", "In. Size");
            dataGridViewLogTestForErrors.Columns.Add("OutputFileSize", "Out. Size");
            dataGridViewLogTestForErrors.Columns.Add("Compression", "Compr.");
            dataGridViewLogTestForErrors.Columns.Add("Time", "Time");
            dataGridViewLogTestForErrors.Columns.Add("Speed", "Speed");
            dataGridViewLogTestForErrors.Columns.Add("SpeedMin", "Speed Min.");
            dataGridViewLogTestForErrors.Columns.Add("SpeedMax", "Speed Max.");
            dataGridViewLogTestForErrors.Columns.Add("SpeedRange", "Range");
            dataGridViewLogTestForErrors.Columns.Add("SpeedConsistency", "Speed Consistency");
            dataGridViewLogTestForErrors.Columns.Add("CPULoadEncoder", "CPU Load");
            dataGridViewLogTestForErrors.Columns.Add("CPUClock", "CPU Clock");
            dataGridViewLogTestForErrors.Columns.Add("Passes", "Passes");
            dataGridViewLogTestForErrors.Columns.Add("Parameters", "Parameters");
            dataGridViewLogTestForErrors.Columns.Add("Encoder", "Encoder");
            dataGridViewLogTestForErrors.Columns.Add("Version", "Version");

            var encoderDirectoryColumn = new DataGridViewLinkColumn
            {
                Name = "EncoderDirectory",
                HeaderText = "Encoder Directory",
                DataPropertyName = "EncoderDirectory"
            };
            dataGridViewLogTestForErrors.Columns.Add(encoderDirectoryColumn);

            dataGridViewLogTestForErrors.Columns.Add("FastestEncoder", "Fastest Encoder");
            dataGridViewLogTestForErrors.Columns.Add("BestSize", "Best Size");
            dataGridViewLogTestForErrors.Columns.Add("SameSize", "Same Size");

            var audioFileDirectoryColumn = new DataGridViewLinkColumn
            {
                Name = "AudioFileDirectory",
                HeaderText = "Audio File Directory",
                DataPropertyName = "AudioFileDirectory"
            };
            dataGridViewLogTestForErrors.Columns.Add(audioFileDirectoryColumn);

            dataGridViewLogTestForErrors.Columns.Add("MD5", "MD5");
            dataGridViewLogTestForErrors.Columns.Add("Duplicates", "Duplicates");
            dataGridViewLogTestForErrors.Columns.Add("Errors", "Errors");

            // Set alignment for columns
            dataGridViewLogTestForErrors.Columns["Channels"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewLogTestForErrors.Columns["BitDepth"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewLogTestForErrors.Columns["SamplingRate"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewLogTestForErrors.Columns["InputFileSize"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewLogTestForErrors.Columns["OutputFileSize"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewLogTestForErrors.Columns["Compression"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewLogTestForErrors.Columns["Time"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewLogTestForErrors.Columns["Speed"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewLogTestForErrors.Columns["SpeedMin"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewLogTestForErrors.Columns["SpeedMax"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewLogTestForErrors.Columns["SpeedRange"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewLogTestForErrors.Columns["SpeedConsistency"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewLogTestForErrors.Columns["CPULoadEncoder"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewLogTestForErrors.Columns["CPUClock"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewLogTestForErrors.Columns["Passes"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

            // Hide or show columns by default
            dataGridViewLogTestForErrors.Columns["Name"]!.Visible = true;
            dataGridViewLogTestForErrors.Columns["Channels"]!.Visible = false;
            dataGridViewLogTestForErrors.Columns["BitDepth"]!.Visible = false;
            dataGridViewLogTestForErrors.Columns["SamplingRate"]!.Visible = false;
            dataGridViewLogTestForErrors.Columns["InputFileSize"]!.Visible = false;
            dataGridViewLogTestForErrors.Columns["OutputFileSize"]!.Visible = false;
            dataGridViewLogTestForErrors.Columns["Compression"]!.Visible = false;
            dataGridViewLogTestForErrors.Columns["Time"]!.Visible = false;
            dataGridViewLogTestForErrors.Columns["Speed"]!.Visible = false;
            dataGridViewLogTestForErrors.Columns["SpeedMin"]!.Visible = false;
            dataGridViewLogTestForErrors.Columns["SpeedMax"]!.Visible = false;
            dataGridViewLogTestForErrors.Columns["SpeedRange"]!.Visible = false;
            dataGridViewLogTestForErrors.Columns["SpeedConsistency"]!.Visible = false;
            dataGridViewLogTestForErrors.Columns["CPULoadEncoder"]!.Visible = false;
            dataGridViewLogTestForErrors.Columns["CPUClock"]!.Visible = false;
            dataGridViewLogTestForErrors.Columns["Passes"]!.Visible = false;
            dataGridViewLogTestForErrors.Columns["Parameters"]!.Visible = false;
            dataGridViewLogTestForErrors.Columns["Encoder"]!.Visible = false;
            dataGridViewLogTestForErrors.Columns["Version"]!.Visible = false;
            dataGridViewLogTestForErrors.Columns["EncoderDirectory"]!.Visible = false;
            dataGridViewLogTestForErrors.Columns["FastestEncoder"]!.Visible = false;
            dataGridViewLogTestForErrors.Columns["BestSize"]!.Visible = false;
            dataGridViewLogTestForErrors.Columns["SameSize"]!.Visible = false;
            dataGridViewLogTestForErrors.Columns["AudioFileDirectory"]!.Visible = true;
            dataGridViewLogTestForErrors.Columns["MD5"]!.Visible = true;
            dataGridViewLogTestForErrors.Columns["Duplicates"]!.Visible = false;
            dataGridViewLogTestForErrors.Columns["Errors"]!.Visible = true;

            foreach (DataGridViewColumn column in dataGridViewLogTestForErrors.Columns)
            {
                column.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }
        }
        private void DataGridViewLogTestForErrors_CellContentClick(object? sender, DataGridViewCellEventArgs e)
        {
            // Prevent interaction with the new row or invalid cell
            if (e.RowIndex < 0) return;

            string columnName = dataGridViewLogTestForErrors.Columns[e.ColumnIndex].Name;

            // 1. Handle click on "AudioFileDirectory" column - path to the audio file directory
            if (columnName == "AudioFileDirectory")
            {
                string directoryPath = dataGridViewLogTestForErrors.Rows[e.RowIndex].Cells["AudioFileDirectory"].Value?.ToString() ?? string.Empty;
                string fileName = dataGridViewLogTestForErrors.Rows[e.RowIndex].Cells["Name"].Value?.ToString() ?? string.Empty;

                if (!string.IsNullOrEmpty(directoryPath) && !string.IsNullOrEmpty(fileName))
                {
                    string fullPath = Path.Combine(directoryPath, fileName);
                    if (File.Exists(fullPath))
                    {
                        // Highlight the audio file in Explorer
                        Process.Start("explorer.exe", $"/select,\"{fullPath}\"");
                    }
                    else if (Directory.Exists(directoryPath))
                    {
                        // Open directory if file not found
                        Process.Start("explorer.exe", $"\"{directoryPath}\"");
                    }
                }
            }

            // 2. Handle click on "EncoderDirectory" column - path to the encoder's folder
            else if (columnName == "EncoderDirectory")
            {
                string directoryPath = dataGridViewLogTestForErrors.Rows[e.RowIndex].Cells["EncoderDirectory"].Value?.ToString() ?? string.Empty;
                string encoderFileName = dataGridViewLogTestForErrors.Rows[e.RowIndex].Cells["Encoder"].Value?.ToString() ?? string.Empty;

                if (!string.IsNullOrEmpty(directoryPath) && !string.IsNullOrEmpty(encoderFileName))
                {
                    string encoderExePath = Path.Combine(directoryPath, encoderFileName);
                    if (File.Exists(encoderExePath))
                    {
                        // Highlight the encoder .exe file in Explorer
                        Process.Start("explorer.exe", $"/select,\"{encoderExePath}\"");
                    }
                    else if (Directory.Exists(directoryPath))
                    {
                        // Open directory if .exe not found
                        Process.Start("explorer.exe", $"\"{directoryPath}\"");
                    }
                }
            }
        }
        private void DataGridViewLogTestForErrors_MouseDown(object? sender, MouseEventArgs e)
        {
            var hitTest = dataGridViewLogTestForErrors.HitTest(e.X, e.Y);
            if (hitTest.RowIndex == -1 && hitTest.ColumnIndex == -1)
            {
                dataGridViewLogTestForErrors.ClearSelection();
            }
        }

        //Log processing
        private async Task LogProcessResults(string outputFilePath, string audioFilePath, string parameters, string encoderPath, TimeSpan elapsedTime, TimeSpan userProcessorTime, TimeSpan privilegedProcessorTime, double avgClock, string errorOutput = "", int exitCode = 0)
        {
            // Get Encoder and input Audio File information from cache
            var encoderInfo = encoderInfoCache[encoderPath];
            var audioFileInfo = audioFileInfoCache[audioFilePath];

            if (exitCode == 0)
            {
                FileInfo outputFile = new(outputFilePath);
                if (!outputFile.Exists)
                    return;


                // Extract data from cache
                long samplingRate = long.TryParse(audioFileInfo.SamplingRate, out long sr) ? sr : 0;
                long inputSize = audioFileInfo.FileSize;
                long durationMs = long.TryParse(audioFileInfo.Duration, out long d) ? d : 0;

                // Get output audio file information
                long outputSize = outputFile.Length;
                double compressionPercentage = inputSize > 0 ? ((double)outputSize / inputSize) * 100 : 0;
                double encodingSpeed = elapsedTime.TotalMilliseconds > 0 ? (double)durationMs / elapsedTime.TotalMilliseconds : 0;



                // Calculate CPU Load
                double totalCpuTime = (userProcessorTime + privilegedProcessorTime).TotalMilliseconds;
                double cpuLoadEncoder = elapsedTime.TotalMilliseconds > 0 ? (totalCpuTime / elapsedTime.TotalMilliseconds) * 100 : 0;

                // Create benchmark pass object
                var benchmarkPass = new BenchmarkPass
                {
                    AudioFilePath = audioFilePath,
                    EncoderPath = encoderPath,
                    Parameters = parameters,
                    InputSize = inputSize,
                    OutputSize = outputSize,
                    Time = elapsedTime.TotalMilliseconds,
                    Speed = encodingSpeed,
                    CPULoadEncoder = cpuLoadEncoder,
                    CPUClock = avgClock,
                    Channels = audioFileInfo.Channels,
                    BitDepth = audioFileInfo.BitDepth,
                    SamplingRate = audioFileInfo.SamplingRate,
                    Timestamp = DateTime.Now
                };

                // Add raw data of the Pass to cache
                _benchmarkPasses.Add(benchmarkPass);

                // Add record to DataGridView log
                await InvokeAsync(() =>
                {
                    int rowIndex = dataGridViewLog.Rows.Add(
                    audioFileInfo.FileName,                                 //  0 "Name"
                    audioFileInfo.Channels,                                 //  1 "Channels"
                    audioFileInfo.BitDepth,                                 //  2 "BitDepth"
                    samplingRate.ToString("N0", NumberFormatWithSpaces),    //  3 "SamplingRate"
                    inputSize.ToString("N0", NumberFormatWithSpaces),       //  4 "InputFileSize"
                    outputSize.ToString("N0", NumberFormatWithSpaces),      //  5 "OutputFileSize"
                    $"{compressionPercentage:F3}%",                         //  6 "Compression"
                    $"{elapsedTime.TotalMilliseconds:F3}",                  //  7 "Time"
                    $"{encodingSpeed:F3}x",                                 //  8 "Speed"
                    string.Empty,                                           //  9 "SpeedMin"
                    string.Empty,                                           // 10 "SpeedMax"
                    string.Empty,                                           // 11 "SpeedRange"
                    string.Empty,                                           // 12 "SpeedConsistency"
                    $"{cpuLoadEncoder:F3}%",                                // 13 "CPULoadEncoder"
                    $"{avgClock:F0} MHz",                                   // 14 "CPUClock"
                    "1",                                                    // 15 "Passes"
                    parameters,                                             // 16 "Parameters"
                    encoderInfo.FileName,                                   // 17 "Encoder"
                    encoderInfo.Version,                                    // 18 "Version"
                    encoderInfo.DirectoryPath,                              // 19 "EncoderDirectory"
                    string.Empty,                                           // 20 "FastestEncoder"
                    string.Empty,                                           // 21 "BestSize"
                    string.Empty,                                           // 22 "SameSize"
                    audioFileInfo.DirectoryPath,                            // 23 "AudioFileDirectory"
                    audioFileInfo.Md5Hash,                                  // 24 "MD5"
                    string.Empty,                                           // 25 "Duplicates"
                    string.Empty                                            // 26 "Errors"
                    );

                    // Add a tag to Log raw
                    dataGridViewLog.Rows[rowIndex].Tag = benchmarkPass;

                    // Set text colors based on results
                    dataGridViewLog.Rows[rowIndex].Cells["OutputFileSize"].Style.ForeColor =
                    outputSize < inputSize ? System.Drawing.Color.Green :
                    outputSize > inputSize ? System.Drawing.Color.Red :
                    dataGridViewLog.Rows[rowIndex].Cells["OutputFileSize"].Style.ForeColor;

                    dataGridViewLog.Rows[rowIndex].Cells["Compression"].Style.ForeColor =
                    compressionPercentage < 100 ? System.Drawing.Color.Green :
                    compressionPercentage > 100 ? System.Drawing.Color.Red :
                    dataGridViewLog.Rows[rowIndex].Cells["Compression"].Style.ForeColor;

                    dataGridViewLog.Rows[rowIndex].Cells["Speed"].Style.ForeColor =
                    encodingSpeed > 1 ? System.Drawing.Color.Green :
                    encodingSpeed < 1 ? System.Drawing.Color.Red :
                    dataGridViewLog.Rows[rowIndex].Cells["Speed"].Style.ForeColor;

                    // Scroll to the last added row (optional)
                    // if (dataGridViewLog.Rows.Count > 0)
                    //dataGridViewLog.FirstDisplayedScrollingRowIndex = dataGridViewLog.Rows.Count - 1;
                });

                // Log to file
                await File.AppendAllTextAsync("log.txt",
                $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} " +
                $"{audioFilePath}\t" +
                $"Input size: {inputSize}\t" +
                $"Output size: {outputSize} bytes\t" +
                $"Compression: {compressionPercentage:F3}%\t" +
                $"Time: {elapsedTime.TotalMilliseconds:F3} ms\t" +
                $"Speed: {encodingSpeed:F3}x\t" +
                $"CPU Load: {cpuLoadEncoder:F3}%\t" +
                $"CPU Clock: {avgClock:F0} MHz\t" +
                $"Parameters: {parameters.Trim()}\t" +
                $"Encoder: {encoderInfo.FileName}\t" +
                $"Version: {encoderInfo.Version}\t" +
                $"Encoder Path: {encoderInfo.DirectoryPath}{Environment.NewLine}"
                );
            }

            if (exitCode != 0)
            {
                // === ERROR CASE: minimal logging with empty cells ===
                string finalError = exitCode == unchecked((int)0xC000001D)
                    ? "Process failed: Illegal instruction (e.g. AVX-512 not supported on this CPU)."
                    : string.IsNullOrWhiteSpace(errorOutput)
                        ? "Unknown error (non-zero exit code)."
                        : errorOutput.Trim();

                await InvokeAsync(() =>
                {
                    int rowIndex = dataGridViewLog.Rows.Add(
                    audioFileInfo.FileName,                 //  0 "Name"
                    string.Empty,                           //  1 "Channels"
                    string.Empty,                           //  2 "BitDepth"
                    string.Empty,                           //  3 "SamplingRate"
                    string.Empty,                           //  4 "InputFileSize"
                    string.Empty,                           //  5 "OutputFileSize"
                    string.Empty,                           //  6 "Compression"
                    string.Empty,                           //  7 "Time"
                    string.Empty,                           //  8 "Speed"
                    string.Empty,                           //  9 "SpeedMin"
                    string.Empty,                           // 10 "SpeedMax"
                    string.Empty,                           // 11 "SpeedRange"
                    string.Empty,                           // 12 "SpeedConsistency"
                    string.Empty,                           // 13 "CPULoadEncoder"
                    string.Empty,                           // 14 "CPUClock"
                    "1",                                    // 15 "Passes"
                    parameters,                             // 16 "Parameters"
                    encoderInfo.FileName,                   // 17 "Encoder"
                    encoderInfo.Version,                    // 18 "Version"
                    encoderInfo.DirectoryPath,              // 19 "EncoderDirectory"
                    string.Empty,                           // 20 "FastestEncoder"
                    string.Empty,                           // 21 "BestSize"
                    string.Empty,                           // 22 "SameSize"
                    audioFileInfo.DirectoryPath,            // 23 "AudioFileDirectory"
                    audioFileInfo.Md5Hash,                  // 24 "MD5"
                    string.Empty,                           // 25 "Duplicates"
                    finalError                              // 26 "Errors"
                    );

                    // Highlight entire row in red
                    foreach (DataGridViewCell cell in dataGridViewLog.Rows[rowIndex].Cells)
                    {
                        cell.Style.ForeColor = System.Drawing.Color.Red;
                    }

                    // Ensure Errors column is visible
                    dataGridViewLog.Columns["Errors"]!.Visible = true;
                });

                // Log to file
                await File.AppendAllTextAsync("log.txt",
                $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} " +
                $"{audioFilePath}\t" +
                $"Parameters: {parameters.Trim()}\t" +
                $"Encoder: {encoderInfo.FileName}\t" +
                $"Version: {encoderInfo.Version}\t" +
                $"Encoder Path: {encoderInfo.DirectoryPath}\t" +
                $"Errors: {finalError}{Environment.NewLine}"
                );
            }
        }
        private class BenchmarkPass
        {
            public string AudioFilePath { get; set; } = string.Empty;
            public string EncoderPath { get; set; } = string.Empty;
            public string Parameters { get; set; } = string.Empty;
            public long InputSize { get; set; }
            public long OutputSize { get; set; }
            public double Time { get; set; }
            public double Speed { get; set; }
            public double CPULoadEncoder { get; set; }
            public double CPUClock { get; set; }
            public string Channels { get; set; } = string.Empty;
            public string BitDepth { get; set; } = string.Empty;
            public string SamplingRate { get; set; } = string.Empty;
            public DateTime Timestamp { get; set; }
        }
        private readonly List<BenchmarkPass> _benchmarkPasses = [];

        private void ButtonLogColumnsAutoWidth_Click(object? sender, EventArgs e)
        {
            if (ModifierKeys.HasFlag(Keys.Shift))
            {
                // Auto-resize all tabs
                dataGridViewLog.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells);
                dataGridViewLogDetectDupes.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells);
                dataGridViewLogTestForErrors.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells);
            }
            else
            {
                // Auto-resize only the currently selected tab
                var selectedTab = tabControlLog.SelectedTab;
                DataGridView? activeGrid = selectedTab switch
                {
                    _ when selectedTab == Benchmark => dataGridViewLog,
                    _ when selectedTab == DetectDupes => dataGridViewLogDetectDupes,
                    _ when selectedTab == TestForErrors => dataGridViewLogTestForErrors,
                    _ => null
                };

                activeGrid?.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells);
            }
        }
        private async void ButtonAnalyzeLog_Click(object? sender, EventArgs e)
        {
            await AnalyzeLogAsync();
        }

        private async Task AnalyzeLogAsync()
        {
            // 1. Group all benchmark passes by unique test configuration
            var grouped = _benchmarkPasses
            .GroupBy(pass => new
            {
                pass.AudioFilePath,
                pass.EncoderPath,
                pass.Parameters
            })
            .Select(g => new
            {
                AudioFilePath = g.Key.AudioFilePath,
                EncoderPath = g.Key.EncoderPath,
                Parameters = g.Key.Parameters,
                PassesCount = g.Count(),
                AvgTimeMs = g.Average(p => p.Time),
                AvgSpeed = g.Average(p => p.Speed),
                AvgCPULoadEncoder = g.Average(p => p.CPULoadEncoder),
                AvgCPUClock = g.Where(p => p.CPUClock > 0).Any() ? g.Where(p => p.CPUClock > 0).Average(p => p.CPUClock) : 0,
                MinOutputSize = g.Min(p => p.OutputSize),
                MaxOutputSize = g.Max(p => p.OutputSize),
                InputSize = g.First().InputSize,
                Channels = g.First().Channels,
                BitDepth = g.First().BitDepth,
                SamplingRate = g.First().SamplingRate,
                Speeds = g.Where(p => p.Speed > 0).Select(p => p.Speed).OrderBy(s => s).ToList(), // Extract sorted speeds once
                LatestPass = g.OrderByDescending(p => p.Timestamp).First() // Latest pass for final size
            })
            .ToList();

            // Speed, CPU Load and CPU Clock by Threads (3 graphs)
            var speedByThreadsSeries = new Dictionary<string, (List<int> Threads, List<double> AvgSpeeds)>();
            var cpuLoadByThreadsSeries = new Dictionary<string, (List<int> Threads, List<double> AvgCPULoads)>();
            var cpuClockByThreadsSeries = new Dictionary<string, (List<int> Threads, List<double> AvgCPUClocks)>();

            // Aggregated metrics across all files for one "Encoder+Parameters" set
            var avgSpeedsForAllFilesByThreadsSeries = new Dictionary<string, (List<int> Threads, List<double> AvgSpeedsForAllFiles)>();
            var avgCPULoadsForAllFilesByThreadsSeries = new Dictionary<string, (List<int> Threads, List<double> AvgCPULoadsForAllFiles)>();
            var avgCPUClocksForAllFilesByThreadsSeries = new Dictionary<string, (List<int> Threads, List<double> AvgCPUClocksForAllFiles)>();

            foreach (var group in grouped)
            {
                int j = 1;
                var jMatch = Regex.Match(group.Parameters, @"-j(\d+)");
                if (jMatch.Success)
                {
                    j = int.Parse(jMatch.Groups[1].Value);
                    if (j <= 0 || j > 256) continue;
                }

                string fileName = Path.GetFileName(group.AudioFilePath);
                string encoderName = Path.GetFileName(group.EncoderPath);
                string parametersWithoutJ = GetParametersWithoutJ(group.Parameters);
                string seriesKey = $"{fileName}|{encoderName}|{parametersWithoutJ}".TrimEnd('|');

                // Speed by Threads series (Individual files)
                if (!speedByThreadsSeries.TryGetValue(seriesKey, out var speedValue))
                {
                    speedValue = (new List<int>(), new List<double>());
                    speedByThreadsSeries[seriesKey] = speedValue;
                }
                speedValue.Threads.Add(j);
                speedValue.AvgSpeeds.Add(group.AvgSpeed);

                // CPU Load  by Threads series
                if (!cpuLoadByThreadsSeries.TryGetValue(seriesKey, out var cpuLoadValue))
                {
                    cpuLoadValue = (new List<int>(), new List<double>());
                    cpuLoadByThreadsSeries[seriesKey] = cpuLoadValue;
                }
                cpuLoadValue.Threads.Add(j);
                cpuLoadValue.AvgCPULoads.Add(group.AvgCPULoadEncoder);

                // CPU Clock  by Threads series
                if (!cpuClockByThreadsSeries.TryGetValue(seriesKey, out var cpuClockValue))
                {
                    cpuClockValue = (new List<int>(), new List<double>());
                    cpuClockByThreadsSeries[seriesKey] = cpuClockValue;
                }
                cpuClockValue.Threads.Add(j);
                cpuClockValue.AvgCPUClocks.Add(group.AvgCPUClock);

                // Aggregated series for all files by "Encoder + parameters without -jN" set
                string avgKey = $"{encoderName}|{parametersWithoutJ}".TrimEnd('|');

                if (!avgSpeedsForAllFilesByThreadsSeries.TryGetValue(avgKey, out var avgSpeedValue))
                {
                    avgSpeedValue = (new List<int>(), new List<double>());
                    avgSpeedsForAllFilesByThreadsSeries[avgKey] = avgSpeedValue;
                }
                avgSpeedValue.Threads.Add(j);
                avgSpeedValue.AvgSpeedsForAllFiles.Add(group.AvgSpeed);

                // Aggregated CPU Load series for all files
                if (!avgCPULoadsForAllFilesByThreadsSeries.TryGetValue(avgKey, out var avgCPULoadValue))
                {
                    avgCPULoadValue = (new List<int>(), new List<double>());
                    avgCPULoadsForAllFilesByThreadsSeries[avgKey] = avgCPULoadValue;
                }
                avgCPULoadValue.Threads.Add(j);
                avgCPULoadValue.AvgCPULoadsForAllFiles.Add(group.AvgCPULoadEncoder);

                // Aggregated CPU Clock series for all files
                if (!avgCPUClocksForAllFilesByThreadsSeries.TryGetValue(avgKey, out var avgCPUClockValue))
                {
                    avgCPUClockValue = (new List<int>(), new List<double>());
                    avgCPUClocksForAllFilesByThreadsSeries[avgKey] = avgCPUClockValue;
                }
                avgCPUClockValue.Threads.Add(j);
                avgCPUClockValue.AvgCPUClocksForAllFiles.Add(group.AvgCPUClock);
            }

            // Speed and Compression by Parameters (2 graphs)
            var speedByParamsSeries = new Dictionary<string, (List<string> Params, List<double> AvgSpeeds)>();
            var compressionByParamsSeries = new Dictionary<string, (List<string> Params, List<double> Compressions)>();

            // Aggregated metrics across all files for one "Encoder" set
            var avgCompressionForAllFilesByEncoder = new Dictionary<string, List<(string Param, double Compression)>>();
            var avgSpeedForAllFilesByEncoder = new Dictionary<string, List<(string Param, double Speed)>>();

            foreach (var group in grouped)
            {
                double compression = ((double)group.MinOutputSize / group.InputSize) * 100;

                string fileName = Path.GetFileName(group.AudioFilePath);
                string encoderName = Path.GetFileName(group.EncoderPath);
                string seriesKey = $"{fileName}|{encoderName}".TrimEnd('|');

                // Speed by Parameters series
                if (!speedByParamsSeries.TryGetValue(seriesKey, out var speedValue))
                {
                    speedValue = (new List<string>(), new List<double>());
                    speedByParamsSeries[seriesKey] = speedValue;
                }
                speedValue.Params.Add(group.Parameters);
                speedValue.AvgSpeeds.Add(group.AvgSpeed);

                // Compression by Parameters series
                if (!compressionByParamsSeries.TryGetValue(seriesKey, out var compValue))
                {
                    compValue = (new List<string>(), new List<double>());
                    compressionByParamsSeries[seriesKey] = compValue;
                }
                compValue.Params.Add(group.Parameters);
                compValue.Compressions.Add(compression);

                // Aggregated compression series for all files by Encoder
                if (!avgCompressionForAllFilesByEncoder.TryGetValue(encoderName, out var compList))
                {
                    compList = [];
                    avgCompressionForAllFilesByEncoder[encoderName] = compList;
                }
                compList.Add((group.Parameters, compression));

                // Aggregated speed series for all files by Encoder
                if (!avgSpeedForAllFilesByEncoder.TryGetValue(encoderName, out var speedList))
                {
                    speedList = [];
                    avgSpeedForAllFilesByEncoder[encoderName] = speedList;
                }
                speedList.Add((group.Parameters, group.AvgSpeed));
            }

            var resultEntries = new List<LogEntry>();

            foreach (var group in grouped)
            {
                // Get Audio File and Encoder info for display
                var audioFileInfo = audioFileInfoCache[group.AudioFilePath];
                var encoderInfo = encoderInfoCache[group.EncoderPath];

                string inputSizeFormatted = group.InputSize.ToString("N0", NumberFormatWithSpaces);

                // Use the final OutputSize (after metaflac used or not) for analysis
                long outputSizeFinal = group.LatestPass.OutputSize;
                string outputSizeFormatted = outputSizeFinal.ToString("N0", NumberFormatWithSpaces);

                // Calculate Compression using the final output size
                double compressionPercentage = ((double)outputSizeFinal / group.InputSize) * 100;

                // Format SamplingRate for display (e.g. 44100 -> "44 100")
                string samplingRateFormatted = long.TryParse(group.SamplingRate, out long sr) ? sr.ToString("N0", NumberFormatWithSpaces) : "N/A";

                string audioFileDirectory = audioFileInfo.DirectoryPath;

                // --- Speed Stability Analysis ---
                var speeds = group.Speeds; // Use pre-extracted list

                string speedMin = "", speedMax = "", speedRange = "", speedConsistency = "";

                if (group.PassesCount > 1 && speeds.Count > 0)
                {
                    // Since 'speeds' is already sorted in ascending order:
                    double minSpeed = speeds.First();  // Equivalent to Min()
                    double maxSpeed = speeds.Last();   // Equivalent to Max()
                    double range = maxSpeed - minSpeed;

                    speedMin = $"{minSpeed:F3}x";
                    speedMax = $"{maxSpeed:F3}x";
                    speedRange = $"{range:F3}x";

                    // Calculate p50 (median)
                    int n = speeds.Count;
                    double p50 = n % 2 == 0
                    ? (speeds[n / 2 - 1] + speeds[n / 2]) / 2.0
                    : speeds[n / 2];

                    // Calculate p90 (90th percentile)
                    double p90Index = 0.9 * (n - 1);
                    int p90Low = (int)Math.Floor(p90Index);
                    int p90High = (int)Math.Ceiling(p90Index);
                    double p90 = speeds[p90Low] + (p90Index - p90Low) * (speeds[p90High] - speeds[p90Low]);

                    // Speed Consistency = ratio of p50 to p90, expressed as a percentage
                    double consistency = p90 > 0 ? (p50 / p90) * 100.0 : 0.0;
                    speedConsistency = $"{consistency:F3}%";
                }

                // Create the log entry with averaged values
                var logEntry = new LogEntry
                {
                    Name = audioFileInfo.FileName,
                    Channels = group.Channels,
                    BitDepth = group.BitDepth,
                    SamplingRate = samplingRateFormatted,
                    InputFileSize = inputSizeFormatted,
                    OutputFileSize = outputSizeFormatted,
                    Compression = $"{compressionPercentage:F3}%",
                    Time = group.AvgTimeMs.ToString("F3"),
                    Speed = $"{group.AvgSpeed:F3}x",
                    SpeedMin = speedMin,
                    SpeedMax = speedMax,
                    SpeedRange = speedRange,
                    SpeedConsistency = speedConsistency,
                    CPULoadEncoder = $"{group.AvgCPULoadEncoder:F3}%",
                    CPUClock = group.AvgCPUClock > 0 ? $"{group.AvgCPUClock:F0} MHz" : "N/A",
                    Passes = group.PassesCount.ToString(),
                    Parameters = group.Parameters,
                    Encoder = encoderInfo.FileName,
                    Version = encoderInfo.Version,
                    EncoderDirectory = encoderInfo.DirectoryPath,
                    AudioFileDirectory = audioFileDirectory,
                    MD5 = audioFileInfo.Md5Hash,

                    // Set text colors based on results
                    OutputForeColor = outputSizeFinal < group.InputSize ? Color.Green :
                                      outputSizeFinal > group.InputSize ? Color.Red : Color.Black,
                    CompressionForeColor = compressionPercentage < 100 ? Color.Green :
                                           compressionPercentage > 100 ? Color.Red : Color.Black,
                    SpeedForeColor = group.AvgSpeed > 1 ? Color.Green :
                                     group.AvgSpeed < 1 ? Color.Red : Color.Black
                };

                resultEntries.Add(logEntry);
            }

            // 2. Split into encoding and decoding groups
            var encodeGroups = resultEntries.Where(e => !e.Parameters.Split(' ').Any(p => p == "-d" || p == "--decode")).ToList();
            var decodeGroups = resultEntries.Where(e => e.Parameters.Split(' ').Any(p => p == "-d" || p == "--decode")).ToList();

            // 3. Analysis for encoding: find fastest encoder and best compression
            var finalEncodeEntries = new List<LogEntry>();
            var encodeParamGroups = encodeGroups.GroupBy(e => Path.Combine(e.AudioFileDirectory ?? "", e.Name ?? "") + "|" + e.Parameters).ToList();

            foreach (var group in encodeParamGroups)
            {
                var entries = group.ToList();
                if (entries.Count <= 1)
                {
                    finalEncodeEntries.AddRange(entries);
                    continue;
                }

                // Find fastest encoder
                double maxSpeed = entries.Max(e => double.TryParse(e.Speed.Replace("x", "").Trim(), out double s) ? s : 0.0);
                foreach (var entry in entries)
                {
                    if (double.TryParse(entry.Speed.Replace("x", "").Trim(), out double speed) && speed >= maxSpeed - 0.01)
                        entry.FastestEncoder = "fastest encoder";
                }

                // Analyze output file sizes
                var validEntries = entries
                .Where(e => long.TryParse(e.OutputFileSize.Replace(" ", "").Trim(), out long size) && size > 0)
                .Select(e => (e, long.Parse(e.OutputFileSize.Replace(" ", ""))))
                .ToList();

                if (validEntries.Count == 0)
                {
                    finalEncodeEntries.AddRange(entries);
                    continue;
                }

                long minSize = validEntries.Min(x => x.Item2);
                var sizeCountDict = validEntries.GroupBy(x => x.Item2).ToDictionary(g => g.Key, g => g.Count());

                foreach (var (entry, size) in validEntries)
                {
                    entry.BestSize = (size == minSize && sizeCountDict.Keys.Any(s => s > minSize)) ? "smallest size" : string.Empty;
                    entry.SameSize = (sizeCountDict[size] > 1) ? "has same size" : string.Empty;
                }

                finalEncodeEntries.AddRange(entries);
            }

            // 4. Analysis for decoding: find fastest decoder
            var decodeParamGroups = decodeGroups.GroupBy(e => Path.Combine(e.AudioFileDirectory ?? "", e.Name ?? "") + "|" + e.Parameters).ToList();
            foreach (var group in decodeParamGroups)
            {
                if (group.Count() <= 1) continue;

                double maxSpeed = group.Max(e => double.TryParse(e.Speed.Replace("x", "").Trim(), out double s) ? s : 0.0);
                foreach (var entry in group)
                {
                    if (double.TryParse(entry.Speed.Replace("x", "").Trim(), out double speed) && speed >= maxSpeed - 0.01)
                        entry.FastestEncoder = "fastest decoder";
                }
            }

            // 5. Merge successful results
            var finalSuccessEntries = finalEncodeEntries.Concat(decodeGroups).ToList();

            // Group error rows from current log
            var errorGroups = dataGridViewLog.Rows.Cast<DataGridViewRow>()
            .Where(row => !row.IsNewRow && !string.IsNullOrEmpty(row.Cells["Errors"].Value?.ToString()))
            .GroupBy(row => new
            {
                Audio = (row.Cells["AudioFileDirectory"].Value?.ToString() ?? "") + "\\" + (row.Cells["Name"].Value?.ToString() ?? ""),
                Encoder = (row.Cells["EncoderDirectory"].Value?.ToString() ?? "") + "\\" + (row.Cells["Encoder"].Value?.ToString() ?? ""),
                Params = row.Cells["Parameters"].Value?.ToString() ?? ""
            })
            .Select(g => new
            {
                First = g.First(),
                TotalPasses = g.Sum(row =>
                {
                    string passesStr = row.Cells["Passes"].Value?.ToString() ?? "1";
                    return int.TryParse(passesStr, out int p) ? p : 1;
                })
            })
            .ToList();

            var errorEntries = new List<LogEntry>();
            foreach (var eg in errorGroups)
            {
                var r = eg.First;
                errorEntries.Add(new LogEntry
                {
                    Name = r.Cells["Name"].Value?.ToString() ?? "",
                    Channels = "",
                    BitDepth = "",
                    SamplingRate = "",
                    InputFileSize = "",
                    OutputFileSize = "",
                    Compression = "",
                    Time = "",
                    Speed = "",
                    SpeedMin = "",
                    SpeedMax = "",
                    SpeedRange = "",
                    SpeedConsistency = "",
                    CPULoadEncoder = "",
                    CPUClock = "",
                    Passes = eg.TotalPasses.ToString(),
                    Parameters = r.Cells["Parameters"].Value?.ToString() ?? "",
                    Encoder = r.Cells["Encoder"].Value?.ToString() ?? "",
                    Version = r.Cells["Version"].Value?.ToString() ?? "",
                    EncoderDirectory = r.Cells["EncoderDirectory"].Value?.ToString() ?? "",
                    AudioFileDirectory = r.Cells["AudioFileDirectory"].Value?.ToString() ?? "",
                    MD5 = "",
                    Errors = r.Cells["Errors"].Value?.ToString() ?? "",
                });
            }

            // 6. Merge all results
            var finalEntries = finalSuccessEntries.Concat(errorEntries).ToList();

            // 7. Update UI
            await InvokeAsync(() =>
            {
                dataGridViewLog.Rows.Clear();
                foreach (var entry in finalEntries)
                {
                    int rowIndex = dataGridViewLog.Rows.Add(
                    entry.Name,
                    entry.Channels,
                    entry.BitDepth,
                    entry.SamplingRate,
                    entry.InputFileSize,
                    entry.OutputFileSize,
                    entry.Compression,
                    entry.Time,
                    entry.Speed,
                    entry.SpeedMin,
                    entry.SpeedMax,
                    entry.SpeedRange,
                    entry.SpeedConsistency,
                    entry.CPULoadEncoder,
                    entry.CPUClock,
                    entry.Passes,
                    entry.Parameters,
                    entry.Encoder,
                    entry.Version,
                    entry.EncoderDirectory,
                    entry.FastestEncoder,
                    entry.BestSize,
                    entry.SameSize,
                    entry.AudioFileDirectory,
                    entry.MD5,
                    entry.Duplicates,
                    entry.Errors
                    );

                    // Restore text colors
                    dataGridViewLog.Rows[rowIndex].Cells["OutputFileSize"].Style.ForeColor = entry.OutputForeColor;
                    dataGridViewLog.Rows[rowIndex].Cells["Compression"].Style.ForeColor = entry.CompressionForeColor;
                    dataGridViewLog.Rows[rowIndex].Cells["Speed"].Style.ForeColor = entry.SpeedForeColor;
                }

                // Resort after analysis
                SortDataGridViewLog();

                // Apply red color to all text in error rows
                foreach (DataGridViewRow row in dataGridViewLog.Rows)
                {
                    if (row.IsNewRow) continue;
                    if (!string.IsNullOrEmpty(row.Cells["Errors"].Value?.ToString()))
                    {
                        foreach (DataGridViewCell cell in row.Cells)
                        {
                            cell.Style.ForeColor = System.Drawing.Color.Red;
                        }
                    }
                }

                allIndividualSeries.Clear();
                allAggregatedSeries.Clear();

                RenderScalingGraphSpeedByThreads(
                    speedByThreadsSeries,
                    avgSpeedsForAllFilesByThreadsSeries);

                RenderScalingGraphCPULoadByThreads(
                    cpuLoadByThreadsSeries,
                    avgCPULoadsForAllFilesByThreadsSeries);

                RenderScalingGraphCPUClockByThreads(
                    cpuClockByThreadsSeries,
                    avgCPUClocksForAllFilesByThreadsSeries);

                RenderScalingGraphSpeedByParameters(
                    speedByParamsSeries,
                    avgSpeedForAllFilesByEncoder);

                RenderScalingGraphCompressionByParameters(
                    compressionByParamsSeries,
                    avgCompressionForAllFilesByEncoder);

                UpdateSeriesVisibility();

                //RenderCompressionHistogramsByParamEncoder(compressionByParamEncoder);
                //RenderScalingPlotMeanCompressionByParameters(meanCompressionByParamEncoder);

                // Ensure Benchmark tab is active after analysis
                // tabControlLog.SelectedTab = Benchmark;
            });

            // Show error summary
            if (errorEntries.Count > 0)
            {
                int totalRuns = errorEntries.Sum(e => int.Parse(e.Passes));
                string taskWord = errorEntries.Count == 1 ? "task" : "tasks";
                string runWord = totalRuns == 1 ? "run" : "runs";
                MessageBox.Show(
                $"WARNING: {totalRuns} failed {runWord} across {errorEntries.Count} unique {taskWord}.\n\n" +
                "Results are based only on successful runs.",
                "Analysis Summary",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning
                );
            }
        }
        private class LogEntry
        {
            public string Name { get; set; } = string.Empty;
            public string Channels { get; set; } = string.Empty;
            public string BitDepth { get; set; } = string.Empty;
            public string SamplingRate { get; set; } = string.Empty;
            public string InputFileSize { get; set; } = string.Empty;
            public string OutputFileSize { get; set; } = string.Empty;
            public string Compression { get; set; } = string.Empty;
            public string Time { get; set; } = string.Empty;
            public string Speed { get; set; } = string.Empty;
            public string SpeedMin { get; set; } = string.Empty;
            public string SpeedMax { get; set; } = string.Empty;
            public string SpeedRange { get; set; } = string.Empty;
            public double SpeedP50 { get; set; }
            public double SpeedP90 { get; set; }
            public string SpeedConsistency { get; set; } = string.Empty;
            public string CPULoadEncoder { get; set; } = string.Empty;
            public string CPUClock { get; set; } = string.Empty;
            public string Passes { get; set; } = string.Empty;
            public string Parameters { get; set; } = string.Empty;
            public string Encoder { get; set; } = string.Empty;
            public string Version { get; set; } = string.Empty;
            public string EncoderDirectory { get; set; } = string.Empty;
            public string FastestEncoder { get; set; } = string.Empty;
            public string BestSize { get; set; } = string.Empty;
            public string SameSize { get; set; } = string.Empty;
            public string AudioFileDirectory { get; set; } = string.Empty;
            public string MD5 { get; set; } = string.Empty;
            public string Duplicates { get; set; } = string.Empty;
            public string Errors { get; set; } = string.Empty;

            public Color OutputForeColor { get; set; } // Color for OutputFileSize
            public Color CompressionForeColor { get; set; } // Color for Compression
            public Color SpeedForeColor { get; set; } // Color for Speed
        }

        private void SortDataGridViewLog()
        {
            // Collect data from DataGridView into a list
            var dataToSort = new List<LogEntry>();
            foreach (DataGridViewRow row in dataGridViewLog.Rows)
            {
                if (row.IsNewRow) continue; // Skip new row

                var logEntry = new LogEntry
                {
                    Name = row.Cells["Name"].Value?.ToString() ?? string.Empty,
                    Channels = row.Cells["Channels"].Value?.ToString() ?? string.Empty,
                    BitDepth = row.Cells["BitDepth"].Value?.ToString() ?? string.Empty,
                    SamplingRate = row.Cells["SamplingRate"].Value?.ToString() ?? string.Empty,
                    InputFileSize = row.Cells["InputFileSize"].Value?.ToString() ?? string.Empty,
                    OutputFileSize = row.Cells["OutputFileSize"].Value?.ToString() ?? string.Empty,
                    Compression = row.Cells["Compression"].Value?.ToString() ?? string.Empty,
                    Time = row.Cells["Time"].Value?.ToString() ?? string.Empty,
                    Speed = row.Cells["Speed"].Value?.ToString() ?? string.Empty,
                    SpeedMin = row.Cells["SpeedMin"].Value?.ToString() ?? string.Empty,
                    SpeedMax = row.Cells["SpeedMax"].Value?.ToString() ?? string.Empty,
                    SpeedRange = row.Cells["SpeedRange"].Value?.ToString() ?? string.Empty,
                    SpeedConsistency = row.Cells["SpeedConsistency"].Value?.ToString() ?? string.Empty,
                    CPULoadEncoder = row.Cells["CPULoadEncoder"].Value?.ToString() ?? string.Empty,
                    CPUClock = row.Cells["CPUClock"].Value?.ToString() ?? string.Empty,
                    Passes = row.Cells["Passes"].Value?.ToString() ?? string.Empty,
                    Parameters = row.Cells["Parameters"].Value?.ToString() ?? string.Empty,
                    Encoder = row.Cells["Encoder"].Value?.ToString() ?? string.Empty,
                    Version = row.Cells["Version"].Value?.ToString() ?? string.Empty,
                    EncoderDirectory = row.Cells["EncoderDirectory"].Value?.ToString() ?? string.Empty,
                    FastestEncoder = row.Cells["FastestEncoder"].Value?.ToString() ?? string.Empty,
                    BestSize = row.Cells["BestSize"].Value?.ToString() ?? string.Empty,
                    SameSize = row.Cells["SameSize"].Value?.ToString() ?? string.Empty,
                    AudioFileDirectory = row.Cells["AudioFileDirectory"].Value?.ToString() ?? string.Empty,
                    MD5 = row.Cells["MD5"].Value?.ToString() ?? string.Empty,
                    Duplicates = row.Cells["Duplicates"].Value?.ToString() ?? string.Empty,
                    Errors = row.Cells["Errors"].Value?.ToString() ?? string.Empty,

                    OutputForeColor = row.Cells["OutputFileSize"].Style.ForeColor, // Color for OutputFileSize
                    CompressionForeColor = row.Cells["Compression"].Style.ForeColor, // Color for Compression
                    SpeedForeColor = row.Cells["Speed"].Style.ForeColor // Color for Speed
                };

                dataToSort.Add(logEntry);
            }

            // Perform multi-level sorting with natural sort for Parameters
            var sortedData = dataToSort
            .OrderBy(x => x.AudioFileDirectory, new NaturalStringComparer())
            .ThenBy(x => x.Name, new NaturalStringComparer())
            .ThenBy(x => x.Parameters, new NaturalStringComparer())
            .ThenBy(x => x.EncoderDirectory, new NaturalStringComparer())
            .ThenBy(x => x.Encoder, new NaturalStringComparer())
            .ToList();

            // Clear DataGridView and add sorted data
            dataGridViewLog.Rows.Clear();
            foreach (var data in sortedData)
            {
                int rowIndex = dataGridViewLog.Rows.Add(
                data.Name,
                data.Channels,
                data.BitDepth,
                data.SamplingRate,
                data.InputFileSize,
                data.OutputFileSize,
                data.Compression,
                data.Time,
                data.Speed,
                data.SpeedMin,
                data.SpeedMax,
                data.SpeedRange,
                data.SpeedConsistency,
                data.CPULoadEncoder,
                data.CPUClock,
                data.Passes,
                data.Parameters,
                data.Encoder,
                data.Version,
                data.EncoderDirectory,
                data.FastestEncoder,
                data.BestSize,
                data.SameSize,
                data.AudioFileDirectory,
                data.MD5,
                data.Duplicates,
                data.Errors);

                // Set text color
                dataGridViewLog.Rows[rowIndex].Cells["OutputFileSize"].Style.ForeColor = data.OutputForeColor; // OutputFileSize
                dataGridViewLog.Rows[rowIndex].Cells["Compression"].Style.ForeColor = data.CompressionForeColor; // Compression
                dataGridViewLog.Rows[rowIndex].Cells["Speed"].Style.ForeColor = data.SpeedForeColor; // Speed
            }

            dataGridViewLog.ClearSelection();
        }

        private static string GetParametersWithoutJ(string parameters)
        {
            if (string.IsNullOrWhiteSpace(parameters))
                return string.Empty;

            string result = Regex.Replace(parameters, @"\s*-j\d+\s*", " ");
            return Regex.Replace(result.Trim(), @"\s+", " ");
        }

        private void RenderScalingGraphSpeedByThreads(
            Dictionary<string, (List<int> Threads, List<double> AvgSpeeds)> series,
            Dictionary<string, (List<int> Threads, List<double> AvgSpeedsForAllFiles)>? aggregatedSeries = null)
        {
            {
                var plt = plotScalingPlotSpeedByThreads.Plot;
                plt.Clear();
                allScatterSeriesSpeedByThreads.Clear();

                if (series.Count == 0)
                {
                    plt.Title("No Speed scaling data found");
                }
                else
                {
                    int minThread = series.Values.SelectMany(v => v.Threads).Min();
                    int maxThread = series.Values.SelectMany(v => v.Threads).Max();
                    int threadCount = maxThread - minThread + 1;

                    foreach (var kvp in series)
                    {
                        string label = kvp.Key;
                        var threads = kvp.Value.Threads;
                        var avgSpeeds = kvp.Value.AvgSpeeds;

                        var sorted = threads.Zip(avgSpeeds, (t, s) => new { t, s })
                                            .OrderBy(x => x.t)
                                            .ToList();
                        double[] xs = sorted.Select(x => (double)x.t).ToArray();
                        double[] ys = sorted.Select(x => x.s).ToArray();

                        var scatter = plt.AddScatter(xs, ys, label: label);
                        allIndividualSeries.Add(scatter);
                        allScatterSeriesSpeedByThreads.Add((scatter, label));
                    }

                    if (aggregatedSeries != null)
                    {
                        foreach (var kvp in aggregatedSeries)
                        {
                            string label = $"Avg Speed: {kvp.Key}";
                            var threads = kvp.Value.Threads;
                            var avgSpeedsForAllFiles = kvp.Value.AvgSpeedsForAllFiles;

                            var groupedByThread = threads.Zip(avgSpeedsForAllFiles, (t, s) => new { t, s })
                                                        .GroupBy(x => x.t)
                                                        .Select(g => new
                                                        {
                                                            Thread = g.Key,
                                                            AvgSpeedsForAllFiles = g.Average(x => x.s)
                                                        })
                                                        .OrderBy(x => x.Thread)
                                                        .ToList();

                            double[] xs = groupedByThread.Select(x => (double)x.Thread).ToArray();
                            double[] ys = groupedByThread.Select(x => x.AvgSpeedsForAllFiles).ToArray();

                            var scatter = plt.AddScatter(xs, ys, label: label, lineWidth: 3);
                            allAggregatedSeries.Add(scatter);
                            allScatterSeriesSpeedByThreads.Add((scatter, label));
                        }
                    }

                    double[] tickPositions = Enumerable.Range(minThread, threadCount).Select(j => (double)j).ToArray();
                    string[] tickLabels = tickPositions.Select(j => j.ToString("F0")).ToArray();
                    plt.XTicks(tickPositions, tickLabels);

                    plt.XLabel("Threads (-jN)");
                    plt.YLabel("Speed (x real-time)");
                    plt.Title("Speed Scaling by Threads");
                    plt.Legend(true, location: ScottPlot.Alignment.LowerRight);
                    plt.AxisAuto();
                }
            }

            {
                var plt = plotScalingMultiPlotSpeedByThreads.Plot;
                plt.Clear();

                if (series.Count == 0)
                {
                    plt.Title("No Speed scaling data found");
                }
                else
                {
                    int minThread = series.Values.SelectMany(v => v.Threads).Min();
                    int maxThread = series.Values.SelectMany(v => v.Threads).Max();
                    int threadCount = maxThread - minThread + 1;

                    foreach (var kvp in series)
                    {
                        string label = kvp.Key;
                        var threads = kvp.Value.Threads;
                        var avgSpeeds = kvp.Value.AvgSpeeds;

                        var sorted = threads.Zip(avgSpeeds, (t, s) => new { t, s })
                                            .OrderBy(x => x.t)
                                            .ToList();
                        double[] xs = sorted.Select(x => (double)x.t).ToArray();
                        double[] ys = sorted.Select(x => x.s).ToArray();

                        var scatter = plt.AddScatter(xs, ys, label: label);
                        allIndividualSeries.Add(scatter);
                        allScatterSeriesSpeedByThreads.Add((scatter, label));
                    }

                    if (aggregatedSeries != null)
                    {
                        foreach (var kvp in aggregatedSeries)
                        {
                            string label = $"Avg Speed: {kvp.Key}";
                            var threads = kvp.Value.Threads;
                            var avgSpeedsForAllFiles = kvp.Value.AvgSpeedsForAllFiles;

                            var groupedByThread = threads.Zip(avgSpeedsForAllFiles, (t, s) => new { t, s })
                                                        .GroupBy(x => x.t)
                                                        .Select(g => new
                                                        {
                                                            Thread = g.Key,
                                                            AvgSpeedsForAllFiles = g.Average(x => x.s)
                                                        })
                                                        .OrderBy(x => x.Thread)
                                                        .ToList();

                            double[] xs = groupedByThread.Select(x => (double)x.Thread).ToArray();
                            double[] ys = groupedByThread.Select(x => x.AvgSpeedsForAllFiles).ToArray();

                            var scatter = plt.AddScatter(xs, ys, label: label, lineWidth: 3);
                            allAggregatedSeries.Add(scatter);
                            allScatterSeriesSpeedByThreads.Add((scatter, label));
                        }
                    }

                    double[] tickPositions = Enumerable.Range(minThread, threadCount).Select(j => (double)j).ToArray();
                    string[] tickLabels = tickPositions.Select(j => j.ToString("F0")).ToArray();
                    plt.XTicks(tickPositions, tickLabels);

                    // plt.XLabel("Threads (-jN)");
                    plt.YLabel("Speed (x real-time)");
                    plt.Title("Speed Scaling by Threads");
                    plt.Legend(true, location: ScottPlot.Alignment.LowerRight);
                    plt.AxisAuto();
                }
            }

            plotScalingMultiPlotSpeedByThreads.Configuration.AddLinkedControl(
                plotScalingMultiPlotCPULoadByThreads, horizontal: true, vertical: false);
            plotScalingMultiPlotSpeedByThreads.Configuration.AddLinkedControl(
                plotScalingMultiPlotCPUClockByThreads, horizontal: true, vertical: false);
        }

        private void RenderScalingGraphCPULoadByThreads(
            Dictionary<string, (List<int> Threads, List<double> AvgCPULoads)> series,
            Dictionary<string, (List<int> Threads, List<double> AvgCPULoadsForAllFiles)>? aggregatedSeries = null)
        {
            {
                var plt = plotScalingPlotCPULoadByThreads.Plot;
                plt.Clear();
                allScatterSeriesCPULoadByThreads.Clear();

                if (series.Count == 0)
                {
                    plt.Title("No CPU scaling data found");
                }
                else
                {
                    int minThread = series.Values.SelectMany(v => v.Threads).Min();
                    int maxThread = series.Values.SelectMany(v => v.Threads).Max();
                    int threadCount = maxThread - minThread + 1;

                    double[] idealX = Enumerable.Range(minThread, threadCount).Select(j => (double)j).ToArray();
                    double[] idealY = idealX.Select(j => j * 100.0).ToArray();

                    foreach (var kvp in series)
                    {
                        string label = kvp.Key;
                        var threads = kvp.Value.Threads;
                        var avgCPULoads = kvp.Value.AvgCPULoads;

                        var sorted = threads.Zip(avgCPULoads, (t, l) => new { t, l })
                                            .OrderBy(x => x.t)
                                            .ToList();
                        double[] xs = sorted.Select(x => (double)x.t).ToArray();
                        double[] ys = sorted.Select(x => x.l).ToArray();

                        var scatter = plt.AddScatter(xs, ys, label: label);
                        allIndividualSeries.Add(scatter);
                        allScatterSeriesCPULoadByThreads.Add((scatter, label));
                    }

                    if (aggregatedSeries != null)
                    {
                        foreach (var kvp in aggregatedSeries)
                        {
                            string label = $"Avg CPU Load: {kvp.Key}";
                            var threads = kvp.Value.Threads;
                            var avgCPULoadsForAllFiles = kvp.Value.AvgCPULoadsForAllFiles;

                            var groupedByThread = threads.Zip(avgCPULoadsForAllFiles, (t, l) => new { t, l })
                                                        .GroupBy(x => x.t)
                                                        .Select(g => new
                                                        {
                                                            Thread = g.Key,
                                                            AvgCPULoad = g.Average(x => x.l)
                                                        })
                                                        .OrderBy(x => x.Thread)
                                                        .ToList();

                            double[] xs = groupedByThread.Select(x => (double)x.Thread).ToArray();
                            double[] ys = groupedByThread.Select(x => x.AvgCPULoad).ToArray();

                            var scatter = plt.AddScatter(xs, ys, label: label, lineWidth: 3);
                            allAggregatedSeries.Add(scatter);
                            allScatterSeriesCPULoadByThreads.Add((scatter, label));
                        }
                    }

                    plt.XLabel("Threads (-jN)");
                    plt.YLabel("CPU Load (%)");
                    plt.Title("CPU Load Scaling by Threads");
                    plt.XTicks(idealX, idealX.Select(j => j.ToString("F0")).ToArray());
                    plt.Legend(true, location: ScottPlot.Alignment.LowerRight);
                    plt.AxisAuto();

                    idealCPULoadLineSingle = plt.AddScatter(
                        xs: idealX,
                        ys: idealY,
                        label: "Ideal (100% per thread)",
                        color: Color.Gray,
                        lineStyle: ScottPlot.LineStyle.Dash,
                        markerSize: 3
                    );
                }
            }

            {
                var plt = plotScalingMultiPlotCPULoadByThreads.Plot;
                plt.Clear();

                if (series.Count == 0)
                {
                    plt.Title("No CPU scaling data found");
                }
                else
                {
                    int minThread = series.Values.SelectMany(v => v.Threads).Min();
                    int maxThread = series.Values.SelectMany(v => v.Threads).Max();
                    int threadCount = maxThread - minThread + 1;

                    double[] idealX = Enumerable.Range(minThread, threadCount).Select(j => (double)j).ToArray();
                    double[] idealY = idealX.Select(j => j * 100.0).ToArray();

                    foreach (var kvp in series)
                    {
                        string label = kvp.Key;
                        var threads = kvp.Value.Threads;
                        var avgCPULoads = kvp.Value.AvgCPULoads;

                        var sorted = threads.Zip(avgCPULoads, (t, l) => new { t, l })
                                            .OrderBy(x => x.t)
                                            .ToList();
                        double[] xs = sorted.Select(x => (double)x.t).ToArray();
                        double[] ys = sorted.Select(x => x.l).ToArray();

                        var scatter = plt.AddScatter(xs, ys, label: label);
                        allIndividualSeries.Add(scatter);
                        allScatterSeriesCPULoadByThreads.Add((scatter, label));
                    }

                    if (aggregatedSeries != null)
                    {
                        foreach (var kvp in aggregatedSeries)
                        {
                            string label = $"Avg CPU Load: {kvp.Key}";
                            var threads = kvp.Value.Threads;
                            var avgCPULoadsForAllFiles = kvp.Value.AvgCPULoadsForAllFiles;

                            var groupedByThread = threads.Zip(avgCPULoadsForAllFiles, (t, l) => new { t, l })
                                                        .GroupBy(x => x.t)
                                                        .Select(g => new
                                                        {
                                                            Thread = g.Key,
                                                            AvgCPULoad = g.Average(x => x.l)
                                                        })
                                                        .OrderBy(x => x.Thread)
                                                        .ToList();

                            double[] xs = groupedByThread.Select(x => (double)x.Thread).ToArray();
                            double[] ys = groupedByThread.Select(x => x.AvgCPULoad).ToArray();

                            var scatter = plt.AddScatter(xs, ys, label: label, lineWidth: 3);
                            allAggregatedSeries.Add(scatter);
                            allScatterSeriesCPULoadByThreads.Add((scatter, label));
                        }
                    }

                    // plt.XLabel("Threads (-jN)");
                    plt.YLabel("CPU Load (%)");
                    plt.Title("CPU Load Scaling by Threads");
                    plt.XTicks(idealX, idealX.Select(j => j.ToString("F0")).ToArray());
                    plt.Legend(true, location: ScottPlot.Alignment.LowerRight);
                    plt.AxisAuto();

                    idealCPULoadLineMultiplot = plt.AddScatter(
                        xs: idealX,
                        ys: idealY,
                        label: "Ideal (100% per thread)",
                        color: Color.Gray,
                        lineStyle: ScottPlot.LineStyle.Dash,
                        markerSize: 3
                    );
                }
            }

            plotScalingMultiPlotCPULoadByThreads.Configuration.AddLinkedControl(
                plotScalingMultiPlotSpeedByThreads, horizontal: true, vertical: false);
            plotScalingMultiPlotCPULoadByThreads.Configuration.AddLinkedControl(
                plotScalingMultiPlotCPUClockByThreads, horizontal: true, vertical: false);
        }

        private void RenderScalingGraphCPUClockByThreads(
            Dictionary<string, (List<int> Threads, List<double> AvgCPUClocks)> series,
            Dictionary<string, (List<int> Threads, List<double> AvgCPUClocksForAllFiles)>? aggregatedSeries = null)
        {
            {
                var plt = plotScalingPlotCPUClockByThreads.Plot;
                plt.Clear();
                allScatterSeriesCPUClockByThreads.Clear();

                if (series.Count == 0)
                {
                    plt.Title("No CPU clock scaling data found");
                }
                else
                {
                    int minThread = series.Values.SelectMany(v => v.Threads).Min();
                    int maxThread = series.Values.SelectMany(v => v.Threads).Max();
                    int threadCount = maxThread - minThread + 1;

                    foreach (var kvp in series)
                    {
                        string label = kvp.Key;
                        var threads = kvp.Value.Threads;
                        var avgCPUClocks = kvp.Value.AvgCPUClocks;

                        var sorted = threads.Zip(avgCPUClocks, (t, c) => new { t, c })
                                            .OrderBy(x => x.t)
                                            .ToList();
                        double[] xs = sorted.Select(x => (double)x.t).ToArray();
                        double[] ys = sorted.Select(x => x.c).ToArray();

                        var scatter = plt.AddScatter(xs, ys, label: label);
                        allIndividualSeries.Add(scatter);
                        allScatterSeriesCPUClockByThreads.Add((scatter, label));
                    }

                    if (aggregatedSeries != null)
                    {
                        foreach (var kvp in aggregatedSeries)
                        {
                            string label = $"Avg CPU Clock: {kvp.Key}";
                            var threads = kvp.Value.Threads;
                            var avgCPUClocksForAllFiles = kvp.Value.AvgCPUClocksForAllFiles;

                            var groupedByThread = threads.Zip(avgCPUClocksForAllFiles, (t, c) => new { t, c })
                                                        .GroupBy(x => x.t)
                                                        .Select(g => new
                                                        {
                                                            Thread = g.Key,
                                                            AvgCPUClock = g.Average(x => x.c)
                                                        })
                                                        .OrderBy(x => x.Thread)
                                                        .ToList();

                            double[] xs = groupedByThread.Select(x => (double)x.Thread).ToArray();
                            double[] ys = groupedByThread.Select(x => x.AvgCPUClock).ToArray();

                            var scatter = plt.AddScatter(xs, ys, label: label, lineWidth: 3);
                            allAggregatedSeries.Add(scatter);
                            allScatterSeriesCPUClockByThreads.Add((scatter, label));
                        }
                    }

                    plt.XLabel("Threads (-jN)");
                    plt.YLabel("CPU Clock (MHz)");
                    plt.Title("CPU Clock Scaling by Threads");
                    double[] tickPositions = Enumerable.Range(minThread, threadCount).Select(j => (double)j).ToArray();
                    string[] tickLabels = tickPositions.Select(j => j.ToString("F0")).ToArray();
                    plt.XTicks(tickPositions, tickLabels);
                    plt.Legend(true, location: ScottPlot.Alignment.LowerRight);
                    plt.AxisAuto();
                }
            }

            {
                var plt = plotScalingMultiPlotCPUClockByThreads.Plot;
                plt.Clear();

                if (series.Count == 0)
                {
                    plt.Title("No CPU clock scaling data found");
                }
                else
                {
                    int minThread = series.Values.SelectMany(v => v.Threads).Min();
                    int maxThread = series.Values.SelectMany(v => v.Threads).Max();
                    int threadCount = maxThread - minThread + 1;

                    foreach (var kvp in series)
                    {
                        string label = kvp.Key;
                        var threads = kvp.Value.Threads;
                        var avgCPUClocks = kvp.Value.AvgCPUClocks;

                        var sorted = threads.Zip(avgCPUClocks, (t, c) => new { t, c })
                                            .OrderBy(x => x.t)
                                            .ToList();
                        double[] xs = sorted.Select(x => (double)x.t).ToArray();
                        double[] ys = sorted.Select(x => x.c).ToArray();

                        var scatter = plt.AddScatter(xs, ys, label: label);
                        allIndividualSeries.Add(scatter);
                        allScatterSeriesCPUClockByThreads.Add((scatter, label));
                    }

                    if (aggregatedSeries != null)
                    {
                        foreach (var kvp in aggregatedSeries)
                        {
                            string label = $"Avg CPU Clock: {kvp.Key}";
                            var threads = kvp.Value.Threads;
                            var avgCPUClocksForAllFiles = kvp.Value.AvgCPUClocksForAllFiles;

                            var groupedByThread = threads.Zip(avgCPUClocksForAllFiles, (t, c) => new { t, c })
                                                        .GroupBy(x => x.t)
                                                        .Select(g => new
                                                        {
                                                            Thread = g.Key,
                                                            AvgCPUClock = g.Average(x => x.c)
                                                        })
                                                        .OrderBy(x => x.Thread)
                                                        .ToList();

                            double[] xs = groupedByThread.Select(x => (double)x.Thread).ToArray();
                            double[] ys = groupedByThread.Select(x => x.AvgCPUClock).ToArray();

                            var scatter = plt.AddScatter(xs, ys, label: label, lineWidth: 3);
                            allAggregatedSeries.Add(scatter);
                            allScatterSeriesCPUClockByThreads.Add((scatter, label));
                        }
                    }

                    plt.XLabel("Threads (-jN)");
                    plt.YLabel("CPU Clock (MHz)");
                    plt.Title("CPU Clock Scaling by Threads");
                    double[] tickPositions = Enumerable.Range(minThread, threadCount).Select(j => (double)j).ToArray();
                    string[] tickLabels = tickPositions.Select(j => j.ToString("F0")).ToArray();
                    plt.XTicks(tickPositions, tickLabels);
                    plt.Legend(true, location: ScottPlot.Alignment.LowerRight);
                    plt.AxisAuto();
                }
            }

            plotScalingMultiPlotCPUClockByThreads.Configuration.AddLinkedControl(
                plotScalingMultiPlotSpeedByThreads, horizontal: true, vertical: false);
            plotScalingMultiPlotCPUClockByThreads.Configuration.AddLinkedControl(
                plotScalingMultiPlotCPULoadByThreads, horizontal: true, vertical: false);
        }

        private void RenderScalingGraphSpeedByParameters(
            Dictionary<string, (List<string> Params, List<double> AvgSpeeds)> series,
            Dictionary<string, List<(string Param, double Speed)>>? aggregatedSeries = null)
        {
            List<string> allParams = [];
            double[] xPositions = [];
            string[] xLabels = [];

            if (series.Count > 0)
            {
                allParams = series.Values
                    .Where(v => v.Params != null)
                    .SelectMany(v => v.Params)
                    .Select(p => string.IsNullOrEmpty(p) ? "[default]" : p)
                    .Distinct()
                    .OrderBy(p => p, new NaturalStringComparer())
                    .ToList();

                xPositions = Enumerable.Range(0, allParams.Count).Select(i => (double)i).ToArray();

                xLabels = allParams.Select(param =>
                {
                    if (checkBoxWrapLongPlotLabels.Checked &&
                        int.TryParse(textBoxWrapLongPlotLabels.Text, out int maxLength) &&
                        maxLength > 0)
                    {
                        return param.Length > maxLength ? WrapTextLabelsOnPlots(param, maxLength) : param;
                    }
                    return param;
                }).ToArray();
            }

            {
                var plt = plotScalingPlotSpeedByParameters.Plot;
                plt.Clear();
                allScatterSeriesSpeedByParameters.Clear();

                if (series.Count == 0)
                {
                    plt.Title("No parameters data found");
                }
                else
                {
                    foreach (var kvp in series)
                    {
                        string label = kvp.Key;
                        var paramsList = kvp.Value.Params;
                        var speeds = kvp.Value.AvgSpeeds;

                        var normalizedParams = paramsList.Select(p => string.IsNullOrEmpty(p) ? "[default]" : p);
                        var points = normalizedParams.Select(p => (double)allParams.IndexOf(p))
                                                   .Zip(speeds, (x, y) => new { x, y })
                                                   .OrderBy(p => p.x)
                                                   .ToArray();
                        double[] xs = points.Select(p => p.x).ToArray();
                        double[] ys = points.Select(p => p.y).ToArray();

                        var scatter = plt.AddScatter(xs, ys, label: label);
                        allIndividualSeries.Add(scatter);
                        allScatterSeriesSpeedByParameters.Add((scatter, label));
                    }

                    if (aggregatedSeries != null)
                    {
                        foreach (var kvp in aggregatedSeries)
                        {
                            string label = $"Avg Speed: {kvp.Key}";
                            var dataPoints = kvp.Value;

                            var normalizedData = dataPoints.Select(dp =>
                                new { Param = string.IsNullOrEmpty(dp.Param) ? "[default]" : dp.Param, dp.Speed });

                            var groupedByParam = normalizedData
                                .GroupBy(x => x.Param)
                                .Select(g => new
                                {
                                    Param = g.Key,
                                    AvgSpeed = g.Average(x => x.Speed)
                                })
                                .Where(x => allParams.Contains(x.Param))
                                .OrderBy(x => x.Param, new NaturalStringComparer())
                                .ToList();

                            if (groupedByParam.Count == 0) continue;

                            double[] xs = groupedByParam.Select(x => (double)allParams.IndexOf(x.Param)).ToArray();
                            double[] ys = groupedByParam.Select(x => x.AvgSpeed).ToArray();

                            var scatter = plt.AddScatter(xs, ys, label: label, lineWidth: 3);
                            allAggregatedSeries.Add(scatter);
                            allScatterSeriesSpeedByParameters.Add((scatter, label));
                        }
                    }

                    plt.XTicks(xPositions, xLabels);
                    plt.XLabel("Parameters");
                    plt.YLabel("Speed (x real-time)");
                    plt.Title("Speed by Parameters");
                    plt.Legend(true, location: ScottPlot.Alignment.UpperRight);
                    plt.AxisAuto();
                }
            }

            {
                var plt = plotScalingMultiPlotSpeedByParameters.Plot;
                plt.Clear();

                if (series.Count == 0)
                {
                    plt.Title("No parameter data found");
                }
                else
                {
                    foreach (var kvp in series)
                    {
                        string label = kvp.Key;
                        var paramsList = kvp.Value.Params;
                        var speeds = kvp.Value.AvgSpeeds;

                        var normalizedParams = paramsList.Select(p => string.IsNullOrEmpty(p) ? "[default]" : p);
                        var points = normalizedParams.Select(p => (double)allParams.IndexOf(p))
                                                   .Zip(speeds, (x, y) => new { x, y })
                                                   .OrderBy(p => p.x)
                                                   .ToArray();
                        double[] xs = points.Select(p => p.x).ToArray();
                        double[] ys = points.Select(p => p.y).ToArray();

                        var scatter = plt.AddScatter(xs, ys, label: label);
                        allIndividualSeries.Add(scatter);
                        allScatterSeriesSpeedByParameters.Add((scatter, label));
                    }

                    if (aggregatedSeries != null)
                    {
                        foreach (var kvp in aggregatedSeries)
                        {
                            string label = $"Avg Speed: {kvp.Key}";
                            var dataPoints = kvp.Value;

                            var normalizedData = dataPoints.Select(dp =>
                                new { Param = string.IsNullOrEmpty(dp.Param) ? "[default]" : dp.Param, dp.Speed });

                            var groupedByParam = normalizedData
                                .GroupBy(x => x.Param)
                                .Select(g => new
                                {
                                    Param = g.Key,
                                    AvgSpeed = g.Average(x => x.Speed)
                                })
                                .Where(x => allParams.Contains(x.Param))
                                .OrderBy(x => x.Param, new NaturalStringComparer())
                                .ToList();

                            if (groupedByParam.Count == 0) continue;

                            double[] xs = groupedByParam.Select(x => (double)allParams.IndexOf(x.Param)).ToArray();
                            double[] ys = groupedByParam.Select(x => x.AvgSpeed).ToArray();

                            var scatter = plt.AddScatter(xs, ys, label: label, lineWidth: 3);
                            allAggregatedSeries.Add(scatter);
                            allScatterSeriesSpeedByParameters.Add((scatter, label));
                        }
                    }

                    plt.XTicks(xPositions, xLabels);
                    // plt.XLabel("Parameters");
                    plt.YLabel("Speed (x real-time)");
                    plt.Title("Speed by Parameters");
                    plt.Legend(true, location: ScottPlot.Alignment.UpperRight);
                    plt.AxisAuto();
                }
            }

            allParamsSpeedByParameters = allParams ?? [];

            plotScalingMultiPlotSpeedByParameters.Configuration.AddLinkedControl(
                plotScalingMultiPlotCompressionByParameters, horizontal: true, vertical: false);
        }

        private void RenderScalingGraphCompressionByParameters(
            Dictionary<string, (List<string> Params, List<double> Compressions)> series,
            Dictionary<string, List<(string Param, double Compression)>>? aggregatedSeries = null)
        {
            List<string> allParams = [];
            double[] xPositions = [];
            string[] xLabels = [];

            if (series.Count > 0)
            {
                allParams = series.Values
                    .Where(v => v.Params != null)
                    .SelectMany(v => v.Params)
                    .Select(p => string.IsNullOrEmpty(p) ? "[default]" : p)
                    .Distinct()
                    .OrderBy(p => p, new NaturalStringComparer())
                    .ToList();

                xPositions = Enumerable.Range(0, allParams.Count).Select(i => (double)i).ToArray();

                xLabels = allParams.Select(param =>
                {
                    if (checkBoxWrapLongPlotLabels.Checked &&
                        int.TryParse(textBoxWrapLongPlotLabels.Text, out int maxLength) &&
                        maxLength > 0)
                    {
                        return param.Length > maxLength ? WrapTextLabelsOnPlots(param, maxLength) : param;
                    }
                    return param;
                }).ToArray();
            }

            {
                var plt = plotScalingPlotCompressionByParameters.Plot;
                plt.Clear();
                allScatterSeriesCompressionByParameters.Clear();

                if (series.Count == 0)
                {
                    plt.Title("No compression data found");
                }
                else
                {
                    foreach (var kvp in series)
                    {
                        string label = kvp.Key;
                        var paramsList = kvp.Value.Params;
                        var compressions = kvp.Value.Compressions;

                        var normalizedParams = paramsList.Select(p => string.IsNullOrEmpty(p) ? "[default]" : p);
                        var points = normalizedParams.Select(p => (double)allParams.IndexOf(p))
                                                  .Zip(compressions, (x, y) => new { x, y })
                                                  .OrderBy(p => p.x)
                                                  .ToArray();
                        double[] xs = points.Select(p => p.x).ToArray();
                        double[] ys = points.Select(p => p.y).ToArray();

                        var scatter = plt.AddScatter(xs, ys, label: label);
                        allIndividualSeries.Add(scatter);
                        allScatterSeriesCompressionByParameters.Add((scatter, label));
                    }

                    if (aggregatedSeries != null)
                    {
                        foreach (var kvp in aggregatedSeries)
                        {
                            string label = $"Avg Compression: {kvp.Key}";
                            var dataPoints = kvp.Value;

                            var normalizedData = dataPoints.Select(dp =>
                                new { Param = string.IsNullOrEmpty(dp.Param) ? "[default]" : dp.Param, dp.Compression });

                            var groupedByParam = normalizedData
                                .GroupBy(x => x.Param)
                                .Select(g => new
                                {
                                    Param = g.Key,
                                    AvgCompression = g.Average(x => x.Compression)
                                })
                                .Where(x => allParams.Contains(x.Param))
                                .OrderBy(x => x.Param, new NaturalStringComparer())
                                .ToList();

                            if (groupedByParam.Count == 0) continue;

                            double[] xs = groupedByParam.Select(x => (double)allParams.IndexOf(x.Param)).ToArray();
                            double[] ys = groupedByParam.Select(x => x.AvgCompression).ToArray();

                            var scatter = plt.AddScatter(xs, ys, label: label, lineWidth: 3);
                            allAggregatedSeries.Add(scatter);
                            allScatterSeriesCompressionByParameters.Add((scatter, label));
                        }
                    }

                    plt.XTicks(xPositions, xLabels);
                    plt.XLabel("Parameters");
                    plt.YLabel("Compression (%)");
                    plt.Title("Compression by Parameters");
                    plt.Legend(true, location: ScottPlot.Alignment.UpperRight);
                    plt.AxisAuto();
                }
            }

            {
                var plt = plotScalingMultiPlotCompressionByParameters.Plot;
                plt.Clear();

                if (series.Count == 0)
                {
                    plt.Title("No compression data found");
                }
                else
                {
                    foreach (var kvp in series)
                    {
                        string label = kvp.Key;
                        var paramsList = kvp.Value.Params;
                        var compressions = kvp.Value.Compressions;

                        var normalizedParams = paramsList.Select(p => string.IsNullOrEmpty(p) ? "[default]" : p);
                        var points = normalizedParams.Select(p => (double)allParams.IndexOf(p))
                                                  .Zip(compressions, (x, y) => new { x, y })
                                                  .OrderBy(p => p.x)
                                                  .ToArray();
                        double[] xs = points.Select(p => p.x).ToArray();
                        double[] ys = points.Select(p => p.y).ToArray();

                        var scatter = plt.AddScatter(xs, ys, label: label);
                        allIndividualSeries.Add(scatter);
                        allScatterSeriesCompressionByParameters.Add((scatter, label));
                    }

                    if (aggregatedSeries != null)
                    {
                        foreach (var kvp in aggregatedSeries)
                        {
                            string label = $"Avg Compression: {kvp.Key}";
                            var dataPoints = kvp.Value;

                            var normalizedData = dataPoints.Select(dp =>
                                new { Param = string.IsNullOrEmpty(dp.Param) ? "[default]" : dp.Param, dp.Compression });

                            var groupedByParam = normalizedData
                                .GroupBy(x => x.Param)
                                .Select(g => new
                                {
                                    Param = g.Key,
                                    AvgCompression = g.Average(x => x.Compression)
                                })
                                .Where(x => allParams.Contains(x.Param))
                                .OrderBy(x => x.Param, new NaturalStringComparer())
                                .ToList();

                            if (groupedByParam.Count == 0) continue;

                            double[] xs = groupedByParam.Select(x => (double)allParams.IndexOf(x.Param)).ToArray();
                            double[] ys = groupedByParam.Select(x => x.AvgCompression).ToArray();

                            var scatter = plt.AddScatter(xs, ys, label: label, lineWidth: 3);
                            allAggregatedSeries.Add(scatter);
                            allScatterSeriesCompressionByParameters.Add((scatter, label));
                        }
                    }

                    plt.XTicks(xPositions, xLabels);
                    plt.XLabel("Parameters");
                    plt.YLabel("Compression (%)");
                    plt.Title("Compression by Parameters");
                    plt.Legend(true, location: ScottPlot.Alignment.UpperRight);
                    plt.AxisAuto();
                }
            }

            allParamsCompressionByParameters = allParams;

            plotScalingMultiPlotCompressionByParameters.Configuration.AddLinkedControl(
                plotScalingMultiPlotSpeedByParameters, horizontal: true, vertical: false);
        }

        private static string WrapTextLabelsOnPlots(string text, int maxLineLength)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= maxLineLength)
                return text;

            var lines = new List<string>();
            int start = 0;

            while (start < text.Length)
            {
                int end = Math.Min(start + maxLineLength, text.Length);
                if (end < text.Length)
                {
                    int lastSpace = text.LastIndexOf(' ', end - 1, end - start);
                    if (lastSpace > start)
                        end = lastSpace;
                }
                lines.Add(text[start..end]);
                start = end;
                while (start < text.Length && text[start] == ' ')
                    start++;
            }

            return string.Join("\n", lines);
        }

        private void PlotScalingPlotSpeedByThreads_MouseMove(object? sender, MouseEventArgs e)
        {
            if (sender is not ScottPlot.FormsPlot formsPlot) return;

            var plt = formsPlot.Plot;
            var (label, x, y, found) = FindNearestPoint(allScatterSeriesSpeedByThreads, formsPlot);

            if (found && dynamicTooltipSpeedByThreads == null)
            {
                string tooltipText = $"{label}\nThreads: {x:F0}\nSpeed: {y:F3}x";
                dynamicTooltipSpeedByThreads = plt.AddTooltip(tooltipText, x, y);
                formsPlot.Refresh();
            }
            else if (!found && dynamicTooltipSpeedByThreads != null)
            {
                plt.Remove(dynamicTooltipSpeedByThreads);
                dynamicTooltipSpeedByThreads = null;
                formsPlot.Refresh();
            }
        }
        private void PlotScalingPlotSpeedByThreads_MouseLeave(object? sender, EventArgs e)
        {
            if (dynamicTooltipSpeedByThreads != null)
            {
                plotScalingPlotSpeedByThreads.Plot.Remove(dynamicTooltipSpeedByThreads);
                dynamicTooltipSpeedByThreads = null;
                plotScalingPlotSpeedByThreads.Refresh();
            }
        }
        private void PlotScalingMultiPlotSpeedByThreads_MouseMove(object? sender, MouseEventArgs e)
        {
            if (sender is not ScottPlot.FormsPlot formsPlot) return;

            var plt = formsPlot.Plot;
            var (label, x, y, found) = FindNearestPoint(allScatterSeriesSpeedByThreads, formsPlot);

            if (found && dynamicTooltipMultiplotSpeedByThreads == null)
            {
                string tooltipText = $"{label}\nThreads: {x:F0}\nSpeed: {y:F3}x";
                dynamicTooltipMultiplotSpeedByThreads = plt.AddTooltip(tooltipText, x, y);
                formsPlot.Refresh();
            }
            else if (!found && dynamicTooltipMultiplotSpeedByThreads != null)
            {
                plt.Remove(dynamicTooltipMultiplotSpeedByThreads);
                dynamicTooltipMultiplotSpeedByThreads = null;
                formsPlot.Refresh();
            }
        }
        private void PlotScalingMultiPlotSpeedByThreads_MouseLeave(object? sender, EventArgs e)
        {
            if (dynamicTooltipMultiplotSpeedByThreads != null)
            {
                plotScalingMultiPlotSpeedByThreads.Plot.Remove(dynamicTooltipMultiplotSpeedByThreads);
                dynamicTooltipMultiplotSpeedByThreads = null;
                plotScalingMultiPlotSpeedByThreads.Refresh();
            }
        }
        private void PlotScalingPlotCPULoadByThreads_MouseMove(object? sender, MouseEventArgs e)
        {
            if (sender is not ScottPlot.FormsPlot formsPlot) return;

            var plt = formsPlot.Plot;
            var (label, x, y, found) = FindNearestPoint(allScatterSeriesCPULoadByThreads, formsPlot);

            if (found && dynamicTooltipCPULoadByThreads == null)
            {
                string tooltipText = $"{label}\nThreads: {x:F0}\nCPU Load: {y:F1}%";
                dynamicTooltipCPULoadByThreads = plt.AddTooltip(tooltipText, x, y);
                formsPlot.Refresh();
            }
            else if (!found && dynamicTooltipCPULoadByThreads != null)
            {
                plt.Remove(dynamicTooltipCPULoadByThreads);
                dynamicTooltipCPULoadByThreads = null;
                formsPlot.Refresh();
            }
        }
        private void PlotScalingPlotCPULoadByThreads_MouseLeave(object? sender, EventArgs e)
        {
            if (dynamicTooltipCPULoadByThreads != null)
            {
                plotScalingPlotCPULoadByThreads.Plot.Remove(dynamicTooltipCPULoadByThreads);
                dynamicTooltipCPULoadByThreads = null;
                plotScalingPlotCPULoadByThreads.Refresh();
            }
        }
        private void PlotScalingMultiPlotCPULoadByThreads_MouseMove(object? sender, MouseEventArgs e)
        {
            if (sender is not ScottPlot.FormsPlot formsPlot) return;

            var plt = formsPlot.Plot;
            var (label, x, y, found) = FindNearestPoint(allScatterSeriesCPULoadByThreads, formsPlot);

            if (found && dynamicTooltipMultiplotCPULoadByThreads == null)
            {
                string tooltipText = $"{label}\nThreads: {x:F0}\nCPU Load: {y:F1}%";
                dynamicTooltipMultiplotCPULoadByThreads = plt.AddTooltip(tooltipText, x, y);
                formsPlot.Refresh();
            }
            else if (!found && dynamicTooltipMultiplotCPULoadByThreads != null)
            {
                plt.Remove(dynamicTooltipMultiplotCPULoadByThreads);
                dynamicTooltipMultiplotCPULoadByThreads = null;
                formsPlot.Refresh();
            }
        }
        private void PlotScalingMultiPlotCPULoadByThreads_MouseLeave(object? sender, EventArgs e)
        {
            if (dynamicTooltipMultiplotCPULoadByThreads != null)
            {
                plotScalingMultiPlotCPULoadByThreads.Plot.Remove(dynamicTooltipMultiplotCPULoadByThreads);
                dynamicTooltipMultiplotCPULoadByThreads = null;
                plotScalingMultiPlotCPULoadByThreads.Refresh();
            }
        }
        private void PlotScalingPlotCPUClockByThreads_MouseMove(object? sender, MouseEventArgs e)
        {
            if (sender is not ScottPlot.FormsPlot formsPlot) return;

            var plt = formsPlot.Plot;
            var (label, x, y, found) = FindNearestPoint(allScatterSeriesCPUClockByThreads, formsPlot);

            if (found && dynamicTooltipCPUClockByThreads == null)
            {
                string tooltipText = $"{label}\nThreads: {x:F0}\nCPU Clock: {y:F0} MHz";
                dynamicTooltipCPUClockByThreads = plt.AddTooltip(tooltipText, x, y);
                formsPlot.Refresh();
            }
            else if (!found && dynamicTooltipCPUClockByThreads != null)
            {
                plt.Remove(dynamicTooltipCPUClockByThreads);
                dynamicTooltipCPUClockByThreads = null;
                formsPlot.Refresh();
            }
        }
        private void PlotScalingPlotCPUClockByThreads_MouseLeave(object? sender, EventArgs e)
        {
            if (dynamicTooltipCPUClockByThreads != null)
            {
                plotScalingPlotCPUClockByThreads.Plot.Remove(dynamicTooltipCPUClockByThreads);
                dynamicTooltipCPUClockByThreads = null;
                plotScalingPlotCPUClockByThreads.Refresh();
            }
        }
        private void PlotScalingMultiPlotCPUClockByThreads_MouseMove(object? sender, MouseEventArgs e)
        {
            if (sender is not ScottPlot.FormsPlot formsPlot) return;

            var plt = formsPlot.Plot;
            var (label, x, y, found) = FindNearestPoint(allScatterSeriesCPUClockByThreads, formsPlot);

            if (found && dynamicTooltipMultiplotCPUClockByThreads == null)
            {
                string tooltipText = $"{label}\nThreads: {x:F0}\nCPU Clock: {y:F0} MHz";
                dynamicTooltipMultiplotCPUClockByThreads = plt.AddTooltip(tooltipText, x, y);
                formsPlot.Refresh();
            }
            else if (!found && dynamicTooltipMultiplotCPUClockByThreads != null)
            {
                plt.Remove(dynamicTooltipMultiplotCPUClockByThreads);
                dynamicTooltipMultiplotCPUClockByThreads = null;
                formsPlot.Refresh();
            }
        }
        private void PlotScalingMultiPlotCPUClockByThreads_MouseLeave(object? sender, EventArgs e)
        {
            if (dynamicTooltipMultiplotCPUClockByThreads != null)
            {
                plotScalingMultiPlotCPUClockByThreads.Plot.Remove(dynamicTooltipMultiplotCPUClockByThreads);
                dynamicTooltipMultiplotCPUClockByThreads = null;
                plotScalingMultiPlotCPUClockByThreads.Refresh();
            }
        }

        private void PlotScalingPlotSpeedByParameters_MouseMove(object? sender, MouseEventArgs e)
        {
            if (sender is not ScottPlot.FormsPlot formsPlot) return;

            var plt = formsPlot.Plot;
            var (label, x, y, found) = FindNearestPoint(allScatterSeriesSpeedByParameters, formsPlot);

            if (found && dynamicTooltipSpeedByParameters == null)
            {
                int paramIndex = (int)Math.Round(x);
                string paramStr = paramIndex >= 0 && paramIndex < allParamsSpeedByParameters.Count
                    ? allParamsSpeedByParameters[paramIndex]
                    : "N/A";
                string tooltipText = $"{label}\nParameters: {paramStr}\nSpeed: {y:F3}x";
                dynamicTooltipSpeedByParameters = plt.AddTooltip(tooltipText, x, y);
                formsPlot.Refresh();
            }
            else if (!found && dynamicTooltipSpeedByParameters != null)
            {
                plt.Remove(dynamicTooltipSpeedByParameters);
                dynamicTooltipSpeedByParameters = null;
                formsPlot.Refresh();
            }
        }
        private void PlotScalingPlotSpeedByParameters_MouseLeave(object? sender, EventArgs e)
        {
            if (dynamicTooltipSpeedByParameters != null)
            {
                plotScalingPlotSpeedByParameters.Plot.Remove(dynamicTooltipSpeedByParameters);
                dynamicTooltipSpeedByParameters = null;
                plotScalingPlotSpeedByParameters.Refresh();
            }
        }
        private void PlotScalingMultiPlotSpeedByParameters_MouseMove(object? sender, MouseEventArgs e)
        {
            if (sender is not ScottPlot.FormsPlot formsPlot) return;

            var plt = formsPlot.Plot;
            var (label, x, y, found) = FindNearestPoint(allScatterSeriesSpeedByParameters, formsPlot);

            if (found && dynamicTooltipMultiplotSpeedByParameters == null)
            {
                int paramIndex = (int)Math.Round(x);
                string paramStr = paramIndex >= 0 && paramIndex < allParamsSpeedByParameters.Count
                    ? allParamsSpeedByParameters[paramIndex]
                    : "N/A";
                string tooltipText = $"{label}\nParameters: {paramStr}\nSpeed: {y:F3}x";
                dynamicTooltipMultiplotSpeedByParameters = plt.AddTooltip(tooltipText, x, y);
                formsPlot.Refresh();
            }
            else if (!found && dynamicTooltipMultiplotSpeedByParameters != null)
            {
                plt.Remove(dynamicTooltipMultiplotSpeedByParameters);
                dynamicTooltipMultiplotSpeedByParameters = null;
                formsPlot.Refresh();
            }
        }
        private void PlotScalingMultiPlotSpeedByParameters_MouseLeave(object? sender, EventArgs e)
        {
            if (dynamicTooltipMultiplotSpeedByParameters != null)
            {
                plotScalingMultiPlotSpeedByParameters.Plot.Remove(dynamicTooltipMultiplotSpeedByParameters);
                dynamicTooltipMultiplotSpeedByParameters = null;
                plotScalingMultiPlotSpeedByParameters.Refresh();
            }
        }
        private void PlotScalingPlotCompressionByParameters_MouseMove(object? sender, MouseEventArgs e)
        {
            if (sender is not ScottPlot.FormsPlot formsPlot) return;

            var plt = formsPlot.Plot;
            var (label, x, y, found) = FindNearestPoint(allScatterSeriesCompressionByParameters, formsPlot);

            if (found && dynamicTooltipCompressionByParameters == null)
            {
                int paramIndex = (int)Math.Round(x);
                string paramStr = paramIndex >= 0 && paramIndex < allParamsCompressionByParameters.Count
                    ? allParamsCompressionByParameters[paramIndex]
                    : "N/A";
                string tooltipText = $"{label}\nParameters: {paramStr}\nCompression: {y:F3}%";
                dynamicTooltipCompressionByParameters = plt.AddTooltip(tooltipText, x, y);
                formsPlot.Refresh();
            }
            else if (!found && dynamicTooltipCompressionByParameters != null)
            {
                plt.Remove(dynamicTooltipCompressionByParameters);
                dynamicTooltipCompressionByParameters = null;
                formsPlot.Refresh();
            }
        }
        private void PlotScalingPlotCompressionByParameters_MouseLeave(object? sender, EventArgs e)
        {
            if (dynamicTooltipCompressionByParameters != null)
            {
                plotScalingPlotCompressionByParameters.Plot.Remove(dynamicTooltipCompressionByParameters);
                dynamicTooltipCompressionByParameters = null;
                plotScalingPlotCompressionByParameters.Refresh();
            }
        }
        private void PlotScalingMultiPlotCompressionByParameters_MouseMove(object? sender, MouseEventArgs e)
        {
            if (sender is not ScottPlot.FormsPlot formsPlot) return;

            var plt = formsPlot.Plot;
            var (label, x, y, found) = FindNearestPoint(allScatterSeriesCompressionByParameters, formsPlot);

            if (found && dynamicTooltipMultiplotCompressionByParameters == null)
            {
                int paramIndex = (int)Math.Round(x);
                string paramStr = paramIndex >= 0 && paramIndex < allParamsCompressionByParameters.Count
                    ? allParamsCompressionByParameters[paramIndex]
                    : "N/A";
                string tooltipText = $"{label}\nParameters: {paramStr}\nCompression: {y:F3}%";
                dynamicTooltipMultiplotCompressionByParameters = plt.AddTooltip(tooltipText, x, y);
                formsPlot.Refresh();
            }
            else if (!found && dynamicTooltipMultiplotCompressionByParameters != null)
            {
                plt.Remove(dynamicTooltipMultiplotCompressionByParameters);
                dynamicTooltipMultiplotCompressionByParameters = null;
                formsPlot.Refresh();
            }
        }
        private void PlotScalingMultiPlotCompressionByParameters_MouseLeave(object? sender, EventArgs e)
        {
            if (dynamicTooltipMultiplotCompressionByParameters != null)
            {
                plotScalingMultiPlotCompressionByParameters.Plot.Remove(dynamicTooltipMultiplotCompressionByParameters);
                dynamicTooltipMultiplotCompressionByParameters = null;
                plotScalingMultiPlotCompressionByParameters.Refresh();
            }
        }

        private static (string seriesLabel, double x, double y, bool found) FindNearestPoint(
            List<(ScottPlot.Plottable.ScatterPlot Series, string Label)> seriesList,
            ScottPlot.FormsPlot formsPlot)
        {
            if (seriesList.Count == 0)
                return (string.Empty, 0, 0, false);

            (double mouseX, double mouseY) = formsPlot.GetMouseCoordinates();

            var limits = formsPlot.Plot.GetAxisLimits();
            double xRange = limits.XSpan;
            double yRange = limits.YSpan;

            double bestDistance = double.MaxValue;
            string bestLabel = string.Empty;
            double bestX = 0, bestY = 0;
            bool found = false;

            foreach (var (scatter, label) in seriesList)
            {
                if (!scatter.IsVisible) continue;

                (double pointX, double pointY, int pointIndex) = scatter.GetPointNearest(mouseX, mouseY, 1.0);

                double normalizedDx = (pointX - mouseX) / xRange;
                double normalizedDy = (pointY - mouseY) / yRange;
                double distance = Math.Sqrt(normalizedDx * normalizedDx + normalizedDy * normalizedDy);

                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestLabel = label;
                    bestX = pointX;
                    bestY = pointY;
                    found = true;
                }
            }

            if (bestDistance > 0.01)
                return (string.Empty, 0, 0, false);

            return (bestLabel, bestX, bestY, found);
        }

        // Log to Excel, copy, clear
        private void ButtonLogToExcel_Click(object? sender, EventArgs e)
        {
            using var workbook = new XLWorkbook();
            // Export each tab as a separate worksheet
            ExportDataGridViewToWorksheet(workbook, dataGridViewLog, "Benchmark");
            ExportDataGridViewToWorksheet(workbook, dataGridViewLogDetectDupes, "Detect Dupes");
            ExportDataGridViewToWorksheet(workbook, dataGridViewLogTestForErrors, "Test for Errors");

            // Create filename
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss");
            string fileName = $"Log {timestamp}.xlsx";
            string fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);

            // Save
            workbook.SaveAs(fullPath);

            // Ask to open
            if (MessageBox.Show(
            $"Log exported to Excel successfully!\n\nSaved as:\n{fullPath}\n\nWould you like to open it?",
            "Success", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
            {
                Process.Start(new ProcessStartInfo { FileName = fullPath, UseShellExecute = true });
            }
        }
        private static void ExportDataGridViewToWorksheet(XLWorkbook workbook, DataGridView dgv, string sheetName)
        {
            if (dgv.Rows.Count == 0) return;

            var worksheet = workbook.Worksheets.Add(sheetName);

            // Get only VISIBLE columns in display order
            var visibleColumns = dgv.Columns.Cast<DataGridViewColumn>()
            .Where(col => col.Visible)
            .OrderBy(col => col.DisplayIndex)
            .ToList();

            if (visibleColumns.Count == 0) return;

            int colCount = visibleColumns.Count;

            // --- Write headers ---
            for (int j = 0; j < colCount; j++)
            {
                var col = visibleColumns[j];
                var cell = worksheet.Cell(1, j + 1);
                cell.Value = col.HeaderText;
                cell.Style.Font.Bold = true;
            }

            // --- Write data rows ---
            int rowIndexInSheet = 2;
            foreach (DataGridViewRow row in dgv.Rows)
            {
                if (row.IsNewRow) continue;

                for (int j = 0; j < colCount; j++)
                {
                    var col = visibleColumns[j];
                    var cellValue = row.Cells[col.Index].Value;
                    var sheetCell = worksheet.Cell(rowIndexInSheet, j + 1);

                    // Handle special formatting by column name
                    string colName = col.Name;
                    string stringValue = cellValue?.ToString() ?? string.Empty;

                    if (colName == "InputFileSize" || colName == "OutputFileSize")
                    {
                        if (!string.IsNullOrEmpty(stringValue) && long.TryParse(stringValue.Replace(" ", ""), out long val))
                            sheetCell.Value = val;
                        else
                            sheetCell.Value = stringValue;
                    }
                    else if (colName == "Channels")
                    {
                        if (!string.IsNullOrEmpty(stringValue) && long.TryParse(stringValue, out long val))
                            sheetCell.Value = val;
                        else
                            sheetCell.Value = stringValue;
                    }
                    else if (colName == "BitDepth" || colName == "SamplingRate")
                    {
                        if (!string.IsNullOrEmpty(stringValue) && long.TryParse(stringValue.Replace(" ", ""), out long val))
                            sheetCell.Value = val;
                        else
                            sheetCell.Value = stringValue;
                    }
                    else if (colName == "Compression" || colName == "CPULoadEncoder" || colName == "SpeedConsistency")
                    {
                        if (!string.IsNullOrEmpty(stringValue) && double.TryParse(stringValue.Replace("%", "").Trim(), out double val))
                            sheetCell.Value = val / 100.0;
                        else
                            sheetCell.Value = stringValue;
                    }
                    else if (colName == "Time" || colName == "Speed" || colName == "SpeedMin" || colName == "SpeedMax" || colName == "SpeedRange" || colName == "CPUClock")
                    {
                        if (!string.IsNullOrEmpty(stringValue))
                        {
                            string cleanValue = stringValue.Replace("x", "").Replace("MHz", "").Trim();
                            if (double.TryParse(cleanValue, out double val))
                                sheetCell.Value = val;
                            else
                                sheetCell.Value = stringValue;
                        }
                        else
                        {
                            sheetCell.Value = string.Empty;
                        }
                    }
                    else if (colName == "Passes")
                    {
                        if (!string.IsNullOrEmpty(stringValue) && int.TryParse(stringValue, out int val))
                            sheetCell.Value = val;
                        else
                            sheetCell.Value = stringValue;
                    }
                    else if (colName == "EncoderDirectory" || colName == "AudioFileDirectory")
                    {
                        sheetCell.Value = stringValue;
                        if (Directory.Exists(stringValue))
                            sheetCell.SetHyperlink(new XLHyperlink(stringValue));
                    }
                    else
                    {
                        sheetCell.Value = stringValue;
                    }

                    // Copy text color
                    if (row.Cells[col.Index].Style.ForeColor != Color.Empty)
                    {
                        var color = row.Cells[col.Index].Style.ForeColor;
                        sheetCell.Style.Font.FontColor = XLColor.FromArgb(color.A, color.R, color.G, color.B);
                    }
                }

                rowIndexInSheet++;
            }

            // --- Apply number formats ---
            for (int j = 0; j < colCount; j++)
            {
                var col = visibleColumns[j];
                var excelCol = worksheet.Column(j + 1);

                switch (col.Name)
                {
                    case "Channels":
                    case "BitDepth":
                    case "Passes":
                    case "CPUClock":
                        excelCol.Style.NumberFormat.Format = "0";
                        break;
                    case "SamplingRate":
                    case "InputFileSize":
                    case "OutputFileSize":
                        excelCol.Style.NumberFormat.Format = "#,##0";
                        break;
                    case "Compression":
                    case "CPULoadEncoder":
                    case "SpeedConsistency":
                        excelCol.Style.NumberFormat.Format = "0.000%";
                        break;
                    case "Time":
                    case "Speed":
                    case "SpeedMin":
                    case "SpeedMax":
                    case "SpeedRange":
                        excelCol.Style.NumberFormat.Format = "0.000";
                        break;
                    case "Parameters":
                        excelCol.Style.NumberFormat.Format = "@";
                        break;
                }
            }

            // Final touches
            worksheet.RangeUsed()?.SetAutoFilter();
            worksheet.SheetView.FreezeRows(1);
            worksheet.Columns().AdjustToContents();

            // Header style
            worksheet.Row(1).Style.Fill.SetBackgroundColor(XLColor.FromHtml("4F81BD"));
            worksheet.Row(1).Style.Font.FontColor = XLColor.White;
        }
        private void ButtonCopyLogAsBBCode_Click(object? sender, EventArgs e)
        {
            try
            {
                // Get only VISIBLE columns, sorted by their display order in the UI
                var visibleColumns = dataGridViewLog.Columns.Cast<DataGridViewColumn>()
                .Where(col => col.Visible)
                .OrderBy(col => col.DisplayIndex)
                .ToList();

                // Check if there are any visible columns to copy
                if (visibleColumns.Count == 0)
                {
                    MessageBox.Show("No visible columns to copy.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Check if there are any data rows (excluding the new row placeholder)
                bool hasData = dataGridViewLog.Rows.Cast<DataGridViewRow>()
                .Any(row => !row.IsNewRow);

                if (!hasData)
                {
                    MessageBox.Show("There is no log to copy.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Build BBCode using only visible columns
                var bbCodeText = new StringBuilder();
                bbCodeText.AppendLine("[table]");

                // Add header row
                bbCodeText.Append("[tr]");
                foreach (var col in visibleColumns)
                {
                    bbCodeText.Append($"[td][b]{col.HeaderText}[/b][/td]");
                }
                bbCodeText.AppendLine("[/tr]");

                // Iterate through data rows
                foreach (DataGridViewRow row in dataGridViewLog.Rows)
                {
                    if (row.IsNewRow) continue; // Skip the empty "new row" at the bottom

                    bbCodeText.Append("[tr]");
                    foreach (var col in visibleColumns)
                    {
                        string cellValue = row.Cells[col.Index]?.Value?.ToString() ?? "";
                        bbCodeText.Append($"[td]{cellValue}[/td]");
                    }
                    bbCodeText.AppendLine("[/tr]");
                }

                bbCodeText.Append("[/table]");

                // Copy to clipboard if there's content
                if (bbCodeText.Length > 0)
                {
                    Clipboard.SetText(bbCodeText.ToString());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error copying log as BBCode: {ex.Message}", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void ButtonOpenLogtxt_Click(object? sender, EventArgs e)
        {
            // Path to log file
            string logFilePath = "log.txt";
            // Check if file exists
            if (File.Exists(logFilePath))
            {
                try
                {
                    // Open log.txt with default text editor
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = logFilePath,
                        UseShellExecute = true // This will open the file with associated application
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error opening log file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show("Log file does not exist.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        private void ButtonCopyLog_Click(object? sender, EventArgs e)
        {
            // Determine which DataGridView corresponds to the currently selected tab
            DataGridView? activeGrid = tabControlLog.SelectedTab switch
            {
                _ when tabControlLog.SelectedTab == Benchmark => dataGridViewLog,
                _ when tabControlLog.SelectedTab == DetectDupes => dataGridViewLogDetectDupes,
                _ when tabControlLog.SelectedTab == TestForErrors => dataGridViewLogTestForErrors,
                _ => null
            };

            // If no valid grid is found, show error and exit
            if (activeGrid == null)
            {
                MessageBox.Show("Unable to determine active log tab.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Get only VISIBLE columns, sorted by their display order in the UI
            var visibleColumns = activeGrid.Columns.Cast<DataGridViewColumn>()
            .Where(col => col.Visible)
            .OrderBy(col => col.DisplayIndex)
            .ToList();

            // Check if there are any visible columns to copy
            if (visibleColumns.Count == 0)
            {
                MessageBox.Show("No visible columns to copy.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Check if there are any data rows (excluding the new row placeholder)
            bool hasData = activeGrid.Rows.Cast<DataGridViewRow>()
            .Any(row => !row.IsNewRow);

            if (!hasData)
            {
                MessageBox.Show("There is no log to copy.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Build tab-separated text using only visible columns
            var logText = new StringBuilder();

            // Add header row
            foreach (var col in visibleColumns)
            {
                logText.Append(col.HeaderText).Append('\t');
            }
            logText.AppendLine();


            // Iterate through data rows
            foreach (DataGridViewRow row in activeGrid.Rows)
            {
                if (row.IsNewRow) continue; // Skip the empty "new row" at the bottom

                foreach (var col in visibleColumns)
                {
                    string cellValue = row.Cells[col.Index]?.Value?.ToString() ?? string.Empty;
                    logText.Append(cellValue).Append('\t');
                }
                logText.AppendLine();
            }

            // Copy to clipboard if there's content
            if (logText.Length > 0)
            {
                Clipboard.SetText(logText.ToString());
                // Optional: MessageBox.Show("Log copied to clipboard!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        private void ButtonClearLog_Click(object? sender, EventArgs e)
        {
            bool clearAllTabs = ModifierKeys.HasFlag(Keys.Shift);

            if (clearAllTabs)
            {
                // Clear all tabs
                dataGridViewLog.Rows.Clear();
                dataGridViewLog.Columns["Errors"]!.Visible = false;
                dataGridViewLog.ClearSelection();

                dataGridViewLogDetectDupes.Rows.Clear();
                dataGridViewLogDetectDupes.ClearSelection();

                dataGridViewLogTestForErrors.Rows.Clear();
                dataGridViewLogTestForErrors.ClearSelection();

                _benchmarkPasses.Clear();
                ClearAllPlots();
                // tabControlLog.SelectedTab = Benchmark;
            }
            else
            {
                // Clear only selected tab
                if (tabControlLog.SelectedTab == Benchmark)
                {
                    dataGridViewLog.Rows.Clear();
                    dataGridViewLog.Columns["Errors"]!.Visible = false;
                    _benchmarkPasses.Clear();
                    dataGridViewLog.ClearSelection();

                    ClearAllPlots();
                }
                else if (tabControlLog.SelectedTab == ScalingPlots)
                {
                    ClearAllPlots();
                }
                else if (tabControlLog.SelectedTab == DetectDupes)
                {
                    dataGridViewLogDetectDupes.Rows.Clear();
                    dataGridViewLogDetectDupes.ClearSelection();
                }
                else if (tabControlLog.SelectedTab == TestForErrors)
                {
                    dataGridViewLogTestForErrors.Rows.Clear();
                    dataGridViewLogTestForErrors.ClearSelection();
                }
            }
        }
        private void ClearAllPlots()
        {
            allIndividualSeries.Clear();
            allAggregatedSeries.Clear();
            allScatterSeriesSpeedByThreads.Clear();
            allScatterSeriesCPULoadByThreads.Clear();
            allScatterSeriesCPUClockByThreads.Clear();
            allScatterSeriesSpeedByParameters.Clear();
            allScatterSeriesCompressionByParameters.Clear();

            dynamicTooltipSpeedByThreads = null;
            dynamicTooltipMultiplotSpeedByThreads = null;
            dynamicTooltipCPULoadByThreads = null;
            dynamicTooltipMultiplotCPULoadByThreads = null;
            dynamicTooltipCPUClockByThreads = null;
            dynamicTooltipMultiplotCPUClockByThreads = null;
            dynamicTooltipSpeedByParameters = null;
            dynamicTooltipMultiplotSpeedByParameters = null;
            dynamicTooltipCompressionByParameters = null;
            dynamicTooltipMultiplotCompressionByParameters = null;

            idealCPULoadLineSingle = null;
            idealCPULoadLineMultiplot = null;

            allParamsSpeedByParameters.Clear();
            allParamsCompressionByParameters.Clear();

            plotScalingPlotSpeedByThreads.Plot.Clear();
            plotScalingMultiPlotSpeedByThreads.Plot.Clear();
            plotScalingPlotCPULoadByThreads.Plot.Clear();
            plotScalingMultiPlotCPULoadByThreads.Plot.Clear();
            plotScalingPlotCPUClockByThreads.Plot.Clear();
            plotScalingMultiPlotCPUClockByThreads.Plot.Clear();
            plotScalingPlotSpeedByParameters.Plot.Clear();
            plotScalingMultiPlotSpeedByParameters.Plot.Clear();
            plotScalingPlotCompressionByParameters.Plot.Clear();
            plotScalingMultiPlotCompressionByParameters.Plot.Clear();

            plotScalingPlotSpeedByThreads.Refresh();
            plotScalingMultiPlotSpeedByThreads.Refresh();
            plotScalingPlotCPULoadByThreads.Refresh();
            plotScalingMultiPlotCPULoadByThreads.Refresh();
            plotScalingPlotCPUClockByThreads.Refresh();
            plotScalingMultiPlotCPUClockByThreads.Refresh();
            plotScalingPlotSpeedByParameters.Refresh();
            plotScalingMultiPlotSpeedByParameters.Refresh();
            plotScalingPlotCompressionByParameters.Refresh();
            plotScalingMultiPlotCompressionByParameters.Refresh();
        }

        // Key actions
        private void ListViewEncoders_KeyDown(object? sender, KeyEventArgs e)
        {
            // Check if Delete key is pressed
            if (e.KeyCode == Keys.Delete)
            {
                buttonClearSelectedEncoder.PerformClick();
            }

            // Check if Ctrl and A are pressed simultaneously
            if (e.Control && e.KeyCode == Keys.A)
            {
                e.Handled = true; // Cancel default behavior

                // Select all items
                foreach (ListViewItem item in listViewEncoders.Items)
                {
                    item.Selected = true; // Set selection for each item
                }
            }
        }
        private void ListViewAudioFiles_KeyDown(object? sender, KeyEventArgs e)
        {
            // Check if Delete key is pressed
            if (e.KeyCode == Keys.Delete)
            {
                buttonClearSelectedAudioFile.PerformClick();
            }

            // Check if Ctrl and A are pressed simultaneously
            if (e.Control && e.KeyCode == Keys.A)
            {
                e.Handled = true; // Cancel default behavior

                // Select all items
                foreach (ListViewItem item in listViewAudioFiles.Items)
                {
                    item.Selected = true; // Set selection for each item
                }
            }
        }
        private void DataGridViewJobs_KeyDown(object? sender, KeyEventArgs e)
        {
            // Check if Delete key is pressed
            if (e.KeyCode == Keys.Delete)
            {
                buttonRemoveJob.PerformClick(); // Reuse the existing remove logic
            }

            // Check if Ctrl and A are pressed simultaneously
            if (e.Control && e.KeyCode == Keys.A)
            {
                e.Handled = true; // Cancel default behavior
                e.SuppressKeyPress = true; // Also suppress the key press to prevent beep

                // Select all rows in dataGridViewJobs
                dataGridViewJobs.SelectAll(); // This is the standard way to select all rows
            }

            // Handle Ctrl+C (Copy in custom format)
            if (e.Control && e.KeyCode == Keys.C)
            {
                ButtonCopyJobs_Click(sender, EventArgs.Empty);
                e.Handled = true; // Cancel default behavior
                e.SuppressKeyPress = true; // Suppress beep
            }

            // Handle Ctrl+V (Paste)
            if (e.Control && e.KeyCode == Keys.V)
            {
                buttonPasteJobs.PerformClick(); // Reuse the existing paste logic
                e.Handled = true; // Cancel default behavior
                e.SuppressKeyPress = true; // Suppress beep
            }
        }
        private void DataGridViewLog_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                var passesToDelete = new List<BenchmarkPass>();

                foreach (DataGridViewRow row in dataGridViewLog.SelectedRows)
                {
                    if (row.IsNewRow) continue;

                    // Case 1: Raw log row (before AnalyzeLogAsync)
                    // In this case, row.Tag holds a direct reference to a BenchmarkPass object.
                    if (row.Tag is BenchmarkPass singlePass)
                    {
                        passesToDelete.Add(singlePass);
                    }
                    // Case 2: Analyzed log row (after AnalyzeLogAsync)
                    // These rows have no Tag, but contain aggregated data including "Passes" count.
                    // We reconstruct the test configuration and remove exactly N oldest matching BenchmarkPass objects,
                    // where N = the "Passes" value shown in the row.
                    else if (int.TryParse(row.Cells["Passes"]?.Value?.ToString(), out int passesCount))
                    {
                        string audioFileName = row.Cells["Name"]?.Value?.ToString() ?? string.Empty;
                        string audioDir = row.Cells["AudioFileDirectory"]?.Value?.ToString() ?? string.Empty;
                        string encoderFileName = row.Cells["Encoder"]?.Value?.ToString() ?? string.Empty;
                        string encoderDir = row.Cells["EncoderDirectory"]?.Value?.ToString() ?? string.Empty;
                        string parameters = row.Cells["Parameters"]?.Value?.ToString() ?? string.Empty;

                        string audioFilePath = Path.Combine(audioDir, audioFileName);
                        string encoderPath = Path.Combine(encoderDir, encoderFileName);

                        // Find all BenchmarkPass entries matching the test configuration (AudioFilePath, EncoderPath, Parameters)
                        // Sort them by Timestamp (oldest first) to match the order in which they were originally added
                        // Then take only the first 'passesCount' entries - this corresponds to the group that was analyzed
                        var matchingPasses = _benchmarkPasses
                        .Where(p =>
                        p.AudioFilePath.Equals(audioFilePath, StringComparison.OrdinalIgnoreCase) &&
                        p.EncoderPath.Equals(encoderPath, StringComparison.OrdinalIgnoreCase) &&
                        p.Parameters == parameters)
                        .OrderBy(p => p.Timestamp) // Oldest first
                        .Take(passesCount)         // Take exactly N = Passes
                        .ToList();

                        passesToDelete.AddRange(matchingPasses);
                    }
                }

                // Remove selected rows from DataGridView (reverse order to avoid index issues)
                var indexes = dataGridViewLog.SelectedRows.Cast<DataGridViewRow>()
                .Where(r => !r.IsNewRow)
                .Select(r => r.Index)
                .OrderByDescending(i => i)
                .ToList();

                foreach (int index in indexes)
                {
                    dataGridViewLog.Rows.RemoveAt(index);
                }

                // Remove the identified BenchmarkPass objects from the internal cache
                // This ensures they won't reappear after a subsequent AnalyzeLogAsync call
                foreach (var pass in passesToDelete)
                {
                    _benchmarkPasses.Remove(pass);
                }

                // Suppress default key press behavior (e.g., error beep)
                e.SuppressKeyPress = true;
            }
        }
        private void DataGridViewLogDetectDupes_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                // Remove selected rows from DataGridView (reverse order to avoid index issues)
                var indexes = dataGridViewLogDetectDupes.SelectedRows.Cast<DataGridViewRow>()
                .Where(r => !r.IsNewRow)
                .Select(r => r.Index)
                .OrderByDescending(i => i)
                .ToList();

                foreach (int index in indexes)
                {
                    dataGridViewLogDetectDupes.Rows.RemoveAt(index);
                }

                // Suppress default key press behavior (e.g., error beep)
                e.SuppressKeyPress = true;
            }
        }
        private void DataGridViewLogTestForErrors_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                // Remove selected rows from DataGridView (reverse order to avoid index issues)
                var indexes = dataGridViewLogTestForErrors.SelectedRows.Cast<DataGridViewRow>()
                .Where(r => !r.IsNewRow)
                .Select(r => r.Index)
                .OrderByDescending(i => i)
                .ToList();

                foreach (int index in indexes)
                {
                    dataGridViewLogTestForErrors.Rows.RemoveAt(index);
                }

                // Suppress default key press behavior (e.g., error beep)
                e.SuppressKeyPress = true;
            }
        }

        // Jobs
        private void DataGridViewJobs_DragEnter(object? sender, DragEventArgs e)
        {
            if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
            {
                string[] files = (string[]?)e.Data.GetData(DataFormats.FileDrop) ?? [];

                // Check for .txt or .bak files or directories
                e.Effect = files.Any(file => Directory.Exists(file) ||
                Path.GetExtension(file).Equals(".txt", StringComparison.OrdinalIgnoreCase) ||
                Path.GetExtension(file).Equals(".bak", StringComparison.OrdinalIgnoreCase))
                ? DragDropEffects.Copy : DragDropEffects.None;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }
        private void DataGridViewJobs_DragDrop(object? sender, DragEventArgs e)
        {
            string[] files = (string[]?)e.Data?.GetData(DataFormats.FileDrop) ?? [];
            foreach (var file in files)
            {
                if (Directory.Exists(file))
                {
                    AddJobsFromDirectory(file);
                }
                else if (Path.GetExtension(file).Equals(".txt", StringComparison.OrdinalIgnoreCase) || Path.GetExtension(file).Equals(".bak", StringComparison.OrdinalIgnoreCase))
                {
                    LoadJobsFromFile(file); // Load jobs from file
                }
            }
        }
        private async void AddJobsFromDirectory(string directory)
        {
            try
            {
                // Find all .txt and .bak files in current directory and subdirectories
                var txtFiles = await Task.Run(() => Directory.GetFiles(directory, "*.txt", SearchOption.AllDirectories));
                var bakFiles = await Task.Run(() => Directory.GetFiles(directory, "*.bak", SearchOption.AllDirectories));

                // Combine file arrays
                var allFiles = txtFiles.Concat(bakFiles);

                foreach (var file in allFiles)
                {
                    LoadJobsFromFile(file); // Load jobs from found file into dataGridViewJobs
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error accessing directory: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private async void LoadJobsFromFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                MessageBox.Show("The specified file does not exist.", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                string[] lines = await Task.Run(() => File.ReadAllLines(filePath));
                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    // Check for the primary format: "Status|Type|Passes|Parameters"
                    if (line.StartsWith("Checked|") || line.StartsWith("Unchecked|"))
                    {
                        int firstBar = line.IndexOf('|');
                        int secondBar = line.IndexOf('|', firstBar + 1);
                        int thirdBar = line.IndexOf('|', secondBar + 1);

                        if (firstBar != -1 && secondBar != -1 && thirdBar != -1 && thirdBar != line.Length - 1)
                        {
                            bool isChecked = line.StartsWith("Checked");
                            string type = line.Substring(firstBar + 1, secondBar - firstBar - 1);
                            string passes = line.Substring(secondBar + 1, thirdBar - secondBar - 1);
                            string parameters = line.Substring(thirdBar + 1);

                            // Add the parsed job data as a new row to dataGridViewJobs
                            dataGridViewJobs.Rows.Add(isChecked, type, passes, parameters);
                            continue;
                        }
                    }
                    // Check for the alternative format: "Type~IsChecked~Passes~Parameters"
                    else if (line.Contains('~'))
                    {
                        var parts = line.Split('~');
                        if (parts.Length == 4 && bool.TryParse(parts[1], out bool isChecked))
                        {
                            string jobName = NormalizeSpaces(parts[0]);
                            string passes = NormalizeSpaces(parts[2]);
                            string parameters = NormalizeSpaces(parts[3]);
                            // Add the parsed job data as a new row to dataGridViewJobs
                            dataGridViewJobs.Rows.Add(isChecked, jobName, passes, parameters);
                            continue;
                        }
                    }

                    MessageBox.Show($"Invalid line format: {line}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error reading file: {ex.Message}", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private async void ButtonImportJobList_Click(object? sender, EventArgs e)
        {
            using OpenFileDialog openFileDialog = new();
            openFileDialog.Filter = "Text and Backup files (*.txt;*.bak)|*.txt;*.bak|All files (*.*)|*.*";
            openFileDialog.Title = "Import Job Lists";
            openFileDialog.Multiselect = true;

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    foreach (string fileName in openFileDialog.FileNames)
                    {
                        string[] lines = await Task.Run(() => File.ReadAllLines(fileName));
                        foreach (var line in lines)
                        {
                            if (string.IsNullOrWhiteSpace(line))
                                continue;

                            // Check for the primary format: "Status|Type|Passes|Parameters"
                            if (line.StartsWith("Checked|") || line.StartsWith("Unchecked|"))
                            {
                                int firstBar = line.IndexOf('|');
                                int secondBar = line.IndexOf('|', firstBar + 1);
                                int thirdBar = line.IndexOf('|', secondBar + 1);

                                if (firstBar != -1 && secondBar != -1 && thirdBar != -1 && thirdBar != line.Length - 1)
                                {
                                    bool isChecked = line.StartsWith("Checked");
                                    string type = line.Substring(firstBar + 1, secondBar - firstBar - 1);
                                    string passes = line.Substring(secondBar + 1, thirdBar - secondBar - 1);
                                    string parameters = line.Substring(thirdBar + 1);

                                    // Add the parsed job data as a new row to dataGridViewJobs
                                    dataGridViewJobs.Rows.Add(isChecked, type, passes, parameters);
                                    continue;
                                }
                            }
                            // Check for the alternative format: "Type~IsChecked~Passes~Parameters"
                            else if (line.Contains('~'))
                            {
                                var parts = line.Split('~');
                                if (parts.Length == 4 && bool.TryParse(parts[1], out bool isChecked))
                                {
                                    string jobName = NormalizeSpaces(parts[0]);
                                    string passes = NormalizeSpaces(parts[2]);
                                    string parameters = NormalizeSpaces(parts[3]);
                                    // Add the parsed job data as a new row to dataGridViewJobs
                                    dataGridViewJobs.Rows.Add(isChecked, jobName, passes, parameters);
                                    continue;
                                }
                            }

                            MessageBox.Show($"Invalid line format: {line}", "Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error reading file: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
        private void ButtonExportJobList_Click(object? sender, EventArgs e)
        {
            using SaveFileDialog saveFileDialog = new();
            saveFileDialog.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";
            saveFileDialog.Title = "Save Job List";
            string fileName = $"Settings_joblist {DateTime.Now:yyyy-MM-dd}.txt";
            saveFileDialog.FileName = fileName;
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    // Create an array of formatted strings representing the rows in dataGridViewJobs
                    // Exclude the new row if it exists (though we disabled it)
                    var jobList = dataGridViewJobs.Rows.Cast<DataGridViewRow>()
                    .Where(row => !row.IsNewRow) // Filter out the new row
                    .Select(row =>
                    {
                        // Get values from the respective cells
                        bool isChecked = Convert.ToBoolean(row.Cells["Column1CheckBox"].Value);
                        string type = row.Cells["Column2JobType"].Value?.ToString() ?? "";
                        string passes = row.Cells["Column3Passes"].Value?.ToString() ?? "";
                        string parameters = row.Cells["Column4Parameters"].Value?.ToString() ?? "";

                        // Format the row data as a single line
                        string status = isChecked ? "Checked" : "Unchecked";
                        return $"{status}|{type}|{passes}|{parameters}";
                    })
                    .ToArray();

                    File.WriteAllLines(saveFileDialog.FileName, jobList, Encoding.UTF8);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error exporting job list: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
        private void ButtonUpJob_Click(object? sender, EventArgs e)
        {
            MoveSelectedItemsForDataGridViewEx(dataGridViewJobs, -1); // Pass -1 to move up
        }
        private void ButtonDownJob_Click(object? sender, EventArgs e)
        {
            MoveSelectedItemsForDataGridViewEx(dataGridViewJobs, 1); // Pass 1 to move down
        }
        private void ButtonRemoveJob_Click(object? sender, EventArgs e)
        {
            // Remove selected rows from dataGridViewJobs
            // Get selected row indices in descending order to avoid index shifting issues during removal
            var selectedIndices = dataGridViewJobs.SelectedRows.Cast<DataGridViewRow>()
            .Select(row => row.Index)
            .OrderByDescending(index => index)
            .ToList();

            foreach (int index in selectedIndices)
            {
                if (index >= 0 && index < dataGridViewJobs.Rows.Count)
                {
                    dataGridViewJobs.Rows.RemoveAt(index);
                }
            }
            // Clear selection and current cell after modification to prevent default highlighting
            dataGridViewJobs.ClearSelection();
            dataGridViewJobs.CurrentCell = null;
        }
        private void ButtonClearJobList_Click(object? sender, EventArgs e)
        {
            dataGridViewJobs.Rows.Clear();
        }
        private void ButtonAddJobToJobListEncoder_Click(object? sender, EventArgs e)
        {
            // Get values from text fields and form parameters
            string compressionLevel = NormalizeSpaces(textBoxCompressionLevel.Text);
            string threads = NormalizeSpaces(textBoxThreads.Text);
            string commandLine = NormalizeSpaces(textBoxCommandLineOptionsEncoder.Text);

            // Form parameter string
            string parameters = $"-{compressionLevel} {commandLine}".Trim();

            // Add thread count if greater than 1
            if (int.TryParse(threads, out int threadCount) && threadCount > 1)
            {
                parameters += $" -j{threads}"; // add -j{threads} flag
            }

            // Job type for this button
            string jobName = "Encode";

            // Check if job already exists in last row for dataGridViewJobs
            bool existingRowFoundForDGV = false;
            if (dataGridViewJobs.Rows.Count > 0)
            {
                var lastRow = dataGridViewJobs.Rows[^1];
                // Compare Job Type and Parameters columns
                if (lastRow.Cells["Column2JobType"].Value?.ToString() == jobName && lastRow.Cells["Column4Parameters"].Value?.ToString() == parameters)
                {
                    // Update the last row's pass count in dataGridViewJobs
                    int currentPasses = int.Parse(lastRow.Cells["Column3Passes"].Value?.ToString() ?? "1");
                    lastRow.Cells["Column3Passes"].Value = (currentPasses + 1).ToString();
                    existingRowFoundForDGV = true;
                }
            }

            // Add new row to dataGridViewJobs if no existing match was found
            if (!existingRowFoundForDGV)
            {
                // Add new row with checkbox checked, job type, default pass count, and parameters
                dataGridViewJobs.Rows.Add(true, jobName, "1", parameters);
            }

            // Clear selection and current cell after modification to prevent default highlighting
            dataGridViewJobs.ClearSelection();
            dataGridViewJobs.CurrentCell = null;
        }
        private void ButtonAddJobToJobListDecoder_Click(object? sender, EventArgs e)
        {
            // Get values from text fields and form parameters
            string commandLine = NormalizeSpaces(textBoxCommandLineOptionsDecoder.Text);
            // Form parameter string
            string parameters = $"-d {commandLine}".Trim();

            // Job type for this button
            string jobName = "Decode";

            // Check if job already exists in last row for dataGridViewJobs
            bool existingRowFoundForDGV = false;
            if (dataGridViewJobs.Rows.Count > 0)
            {
                var lastRow = dataGridViewJobs.Rows[^1];
                // Compare Job Type and Parameters columns
                if (lastRow.Cells["Column2JobType"].Value?.ToString() == jobName && lastRow.Cells["Column4Parameters"].Value?.ToString() == parameters)
                {
                    // Update the last row's pass count in dataGridViewJobs
                    int currentPasses = int.Parse(lastRow.Cells["Column3Passes"].Value?.ToString() ?? "1");
                    lastRow.Cells["Column3Passes"].Value = (currentPasses + 1).ToString();
                    existingRowFoundForDGV = true;
                }
            }

            // Add new row to dataGridViewJobs if no existing match was found
            if (!existingRowFoundForDGV)
            {
                // Add new row with checkbox checked, job type, default pass count, and parameters
                dataGridViewJobs.Rows.Add(true, jobName, "1", parameters);
            }

            // Clear selection and current cell after modification to prevent default highlighting
            dataGridViewJobs.ClearSelection();
            dataGridViewJobs.CurrentCell = null;
        }
        private void ButtonPlusPass_Click(object? sender, EventArgs e)
        {
            // Iterate through the selected rows in dataGridViewJobs
            foreach (DataGridViewRow row in dataGridViewJobs.SelectedRows)
            {
                // Ensure it's not the new row (which is always present when AllowUserToAddRows is true, but we set it to false)
                // and that the cell in the "Passes" column (index 2 or by name) contains a valid integer.
                if (!row.IsNewRow)
                {
                    // Get the current passes value from the "Passes" column (assuming Column3Passes)
                    if (int.TryParse(row.Cells["Column3Passes"].Value?.ToString(), out int currentPasses))
                    {
                        // Increment the passes count
                        currentPasses++;
                        // Update the cell value in dataGridViewJobs
                        row.Cells["Column3Passes"].Value = currentPasses.ToString();
                    }
                }
            }
        }
        private void ButtonMinusPass_Click(object? sender, EventArgs e)
        {
            // Iterate through the selected rows in dataGridViewJobs
            foreach (DataGridViewRow row in dataGridViewJobs.SelectedRows)
            {
                // Ensure it's not the new row (which is always present when AllowUserToAddRows is true, but we set it to false)
                // and that the cell in the "Passes" column (index 2 or by name) contains a valid integer.
                if (!row.IsNewRow)
                {
                    // Get the current passes value from the "Passes" column (assuming Column3Passes)
                    if (int.TryParse(row.Cells["Column3Passes"].Value?.ToString(), out int currentPasses))
                    {
                        // Ensure the value is greater than 1 before decrementing
                        if (currentPasses > 1)
                        {
                            // Decrement the passes count
                            currentPasses--;
                            // Update the cell value in dataGridViewJobs
                            row.Cells["Column3Passes"].Value = currentPasses.ToString();
                        }
                    }
                }
            }
        }
        private void ButtonCopyJobs_Click(object? sender, EventArgs e)
        {
            StringBuilder jobsText = new();

            if (dataGridViewJobs.SelectedRows.Count > 0)
            {
                // --- LOGIC FOR SELECTED ROWS ---
                // Get the indices of the selected rows
                var selectedIndices = dataGridViewJobs.SelectedRows.Cast<DataGridViewRow>()
                .Select(row => row.Index)
                .OrderBy(index => index) // Sort indices in ascending order (top -> down)
                .ToList();

                // Iterate through rows in the order of their ascending index
                foreach (int index in selectedIndices)
                {
                    // Verify the index is valid (just in case)
                    if (index >= 0 && index < dataGridViewJobs.Rows.Count)
                    {
                        var row = dataGridViewJobs.Rows[index];

                        // Get values from the respective cells
                        bool isChecked = Convert.ToBoolean(row.Cells["Column1CheckBox"].Value);
                        string type = row.Cells["Column2JobType"].Value?.ToString() ?? "";
                        string passes = row.Cells["Column3Passes"].Value?.ToString() ?? "";
                        string parameters = row.Cells["Column4Parameters"].Value?.ToString() ?? "";

                        // Format the row data as a single line
                        string status = isChecked ? "Checked" : "Unchecked";
                        jobsText.AppendLine($"{status}|{type}|{passes}|{parameters}");
                    }
                }
            }
            else
            {
                // --- LOGIC FOR ALL ROWS (when nothing is selected) ---
                // Iterate through all rows (excluding the potential new row)
                foreach (DataGridViewRow row in dataGridViewJobs.Rows)
                {
                    if (row.IsNewRow) continue; // Skip the new row

                    // Get values from the respective cells
                    bool isChecked = Convert.ToBoolean(row.Cells["Column1CheckBox"].Value);
                    string type = row.Cells["Column2JobType"].Value?.ToString() ?? "";
                    string passes = row.Cells["Column3Passes"].Value?.ToString() ?? "";
                    string parameters = row.Cells["Column4Parameters"].Value?.ToString() ?? "";

                    // Format the row data as a single line
                    string status = isChecked ? "Checked" : "Unchecked";
                    jobsText.AppendLine($"{status}|{type}|{passes}|{parameters}");
                }
            }

            if (jobsText.Length > 0)
            {
                // Set the formatted text to the clipboard
                Clipboard.SetText(jobsText.ToString());
            }
            else
            {
                // Show a message if no rows were found to copy
                MessageBox.Show("No jobs to copy.", "Information",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        private void ButtonPasteJobs_Click(object? sender, EventArgs e)
        {
            try
            {
                string clipboardText = Clipboard.GetText();
                if (!string.IsNullOrEmpty(clipboardText))
                {
                    // Split the clipboard text into lines
                    string[] lines = clipboardText.Split([Environment.NewLine], StringSplitOptions.RemoveEmptyEntries);
                    foreach (var line in lines)
                    {
                        if (string.IsNullOrWhiteSpace(line))
                            continue;

                        // Check for the primary format: "Status|Type|Passes|Parameters"
                        if (line.StartsWith("Checked|") || line.StartsWith("Unchecked|"))
                        {
                            int firstBar = line.IndexOf('|');
                            int secondBar = line.IndexOf('|', firstBar + 1);
                            int thirdBar = line.IndexOf('|', secondBar + 1);

                            if (firstBar != -1 && secondBar != -1 && thirdBar != -1 && thirdBar != line.Length - 1)
                            {
                                bool isChecked = line.StartsWith("Checked");
                                string type = line.Substring(firstBar + 1, secondBar - firstBar - 1);
                                string passes = line.Substring(secondBar + 1, thirdBar - secondBar - 1);
                                string parameters = line.Substring(thirdBar + 1);

                                // Add the parsed job data as a new row to dataGridViewJobs
                                dataGridViewJobs.Rows.Add(isChecked, type, passes, parameters);
                                continue;
                            }
                        }
                        // Check for the alternative format: "Type~IsChecked~Passes~Parameters"
                        else if (line.Contains('~'))
                        {
                            var parts = line.Split('~');
                            if (parts.Length == 4 && bool.TryParse(parts[1], out bool isChecked))
                            {
                                string jobName = parts[0];
                                string passes = parts[2];
                                string parameters = parts[3];
                                // Add the parsed job data as a new row to dataGridViewJobs
                                dataGridViewJobs.Rows.Add(isChecked, jobName, passes, parameters);
                                continue;
                            }
                        }

                        // Show a warning if a line doesn't match expected formats
                        MessageBox.Show($"Invalid line format: {line}", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
                else
                {
                    // Show a message if the clipboard is empty
                    MessageBox.Show("Clipboard is empty.", "Information",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                // Show an error message if an exception occurs during pasting
                MessageBox.Show($"Error pasting jobs: {ex.Message}", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void DataGridViewJobs_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            // Check if it's the 'Job Type' column (assuming it's the second column, index 1 or name "Column2JobType")
            if (e.ColumnIndex == 1 && e.Value is string cellValue)
            {
                if (cellValue.Equals("Encode", StringComparison.OrdinalIgnoreCase))
                {
                    e.CellStyle.ForeColor = Color.Green;
                    e.FormattingApplied = true;
                }
                else if (cellValue.Equals("Decode", StringComparison.OrdinalIgnoreCase))
                {
                    e.CellStyle.ForeColor = Color.Red;
                    e.FormattingApplied = true;
                }
            }
        }
        private void DataGridViewJobs_MouseDown(object? sender, MouseEventArgs e)
        {
            var hitTest = dataGridViewJobs.HitTest(e.X, e.Y);

            // Handle click on checkbox column header (column 0)
            if (hitTest.RowIndex == -1 && hitTest.ColumnIndex == 0)
            {
                // Get only data rows (exclude new row and any other special rows)
                var dataRows = dataGridViewJobs.Rows
                .Cast<DataGridViewRow>()
                .Where(row => !row.IsNewRow && row.Index >= 0);

                // Check if all data rows are selected
                bool allChecked = true;
                bool hasDataRows = false;

                foreach (DataGridViewRow row in dataRows)
                {
                    hasDataRows = true;
                    object? value = row.Cells["Column1CheckBox"].Value;
                    if (value == null || !Convert.ToBoolean(value))
                    {
                        allChecked = false;
                        break;
                    }
                }

                // If no data rows exist, default to checked state
                bool newState = !(hasDataRows && allChecked);

                // Set new state for all data rows
                foreach (DataGridViewRow row in dataRows)
                {
                    row.Cells["Column1CheckBox"].Value = newState;
                }

                // Force immediate refresh
                dataGridViewJobs.EndEdit();
                dataGridViewJobs.Refresh();
                return;
            }

            // Clear selection on click out of cell
            if (hitTest.RowIndex == -1 && hitTest.ColumnIndex == -1)
            {
                dataGridViewJobs.ClearSelection();
            }
        }

        // Script Constructor
        private void ButtonScriptConstructor_Click(object? sender, EventArgs e)
        {
            // Build initial script parameters from UI controls
            string compressionLevel = NormalizeSpaces(textBoxCompressionLevel.Text);
            string threads = NormalizeSpaces(textBoxThreads.Text);
            string commandLine = NormalizeSpaces(textBoxCommandLineOptionsEncoder.Text);
            string parameters = $"-{compressionLevel} {commandLine}".Trim();
            if (int.TryParse(threads, out int threadCount) && threadCount > 1)
            {
                parameters += $" -j{threads}";
            }
            parameters = Regex.Replace(parameters, @"\s+", " ").Trim();

            if (_scriptForm == null || _scriptForm.IsDisposed)
            {
                _scriptForm = new ScriptConstructorForm
                {
                    InitialScriptText = parameters
                };

                _scriptForm.OnJobsAdded += (jobs) =>
                {
                    // Iterate through the list of jobs received from the ScriptConstructorForm
                    // Each job is now a ScriptJobData struct containing the script definition
                    foreach (var job in jobs) // job is ScriptJobData
                    {
                        // Extract data directly from the ScriptJobData struct
                        bool isChecked = job.IsChecked; // Get the checked state
                        string jobType = job.JobType; // Get the job type (e.g., "Encode")
                        string passes = job.Passes; // Get the number of passes
                        string jobParameters = job.Parameters; // Get the script parameters (the script itself)

                        // Add a new row to dataGridViewJobs with the extracted data
                        dataGridViewJobs.Rows.Add(isChecked, jobType, passes, jobParameters);
                    }
                    // Optionally clear selection after adding new jobs to maintain a clean UI state
                    dataGridViewJobs.ClearSelection();
                    dataGridViewJobs.CurrentCell = null;
                };

                _scriptForm.FormClosed += (s, args) => _scriptForm = null;
                _scriptForm.Show(this);
            }
            else
            {
                _scriptForm.InitialScriptText = parameters;
                _scriptForm.BringToFront();
                _scriptForm.Focus();
            }
        }

        // Actions (Buttons)
        private async void ButtonStartEncode_Click(object? sender, EventArgs e)
        {
            // Create a temporary directory for the output file
            Directory.CreateDirectory(tempFolderPath);

            if (isExecuting) return; // Check if process is already running
            isExecuting = true; // Set execution flag
            _isEncodingStopped = false;
            _isPaused = false;
            _pauseEvent.Set();

            Invoke((MethodInvoker)(() =>
            {
                buttonPauseResume.Text = "Pause";
                buttonPauseResume.Enabled = true;
                progressBarEncoder.Value = 0;
                progressBarDecoder.Value = 0;
                progressBarEncoder.ManualText = string.Empty;
                progressBarDecoder.ManualText = string.Empty;
            }));

            try
            {
                // Get selected encoders
                var selectedEncoders = listViewEncoders.Items.Cast<ListViewItem>()
                    .Where(item => item.Checked)
                    .Select(item => item.Tag!.ToString()!) // Get full path from Tag
                    .ToList();

                // Get all selected .wav and .flac audio files using cache
                var selectedAudioFiles = listViewAudioFiles.Items.Cast<ListViewItem>()
                    .Where(item => item.Checked)
                    .Select(item => item.Tag!.ToString()!)
                    .Where(filePath =>
                    {
                        string extension = audioFileInfoCache[filePath].Extension;
                        return extension == ".wav" || extension == ".flac";
                    })
                    .ToList();

                // 1. Check if there is at least one encoder
                if (selectedEncoders.Count == 0)
                {
                    MessageBox.Show("Select at least one Encoder.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    isExecuting = false;
                    return;
                }

                // 2. Check if there is at least one audio file
                if (selectedAudioFiles.Count == 0)
                {
                    MessageBox.Show("Select at least one Audio File (WAV or FLAC).", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    isExecuting = false;
                    return;
                }

                // === WARM-UP PASS (before main loop) ===
                if (checkBoxWarmupPass.Checked)
                {
                    var firstAudioFile = selectedAudioFiles.FirstOrDefault();
                    var firstEncoder = selectedEncoders.FirstOrDefault();

                    if (!string.IsNullOrEmpty(firstAudioFile) && !string.IsNullOrEmpty(firstEncoder))
                    {
                        // Form parameters using current UI settings
                        string compressionLevel = NormalizeSpaces(textBoxCompressionLevel.Text);
                        string threads = NormalizeSpaces(textBoxThreads.Text);
                        string commandLine = NormalizeSpaces(textBoxCommandLineOptionsEncoder.Text);
                        string parameters = $"-{compressionLevel} {commandLine}".Trim();

                        if (int.TryParse(threads, out int threadCount) && threadCount > 1)
                        {
                            parameters += $" -j{threads}";
                        }

                        string warmupOutputFilePath = Path.Combine(tempFolderPath, $"temp_warmup_{Guid.NewGuid()}.flac");
                        string arguments = $"\"{firstAudioFile}\" {parameters} --no-preserve-modtime -f -o \"{warmupOutputFilePath}\"";

                        Invoke((MethodInvoker)(() =>
                        {
                            progressBarEncoder.ManualText = "Warming up...";
                        }));

                        // Run warm-up asynchronously
                        await Task.Run(() =>
                        {
                            var warmupStopwatch = Stopwatch.StartNew();
                            int iteration = 0;

                            try
                            {
                                while (warmupStopwatch.Elapsed < TimeSpan.FromSeconds(5) && !_isEncodingStopped)
                                {
                                    iteration++;
                                    DeleteFileIfExists(warmupOutputFilePath);

                                    using var warmupProcess = new Process();
                                    warmupProcess.StartInfo = new ProcessStartInfo
                                    {
                                        FileName = firstEncoder,
                                        Arguments = arguments,
                                        UseShellExecute = false,
                                        CreateNoWindow = true,
                                    };

                                    warmupProcess.Start();

                                    try
                                    {
                                        warmupProcess.PriorityClass = GetSelectedProcessPriority();
                                    }
                                    catch (InvalidOperationException) { }

                                    // Wait for process to exit OR until 5 seconds total elapsed
                                    bool exited = warmupProcess.WaitForExit(5000);

                                    if (!exited)
                                    {
                                        try
                                        {
                                            warmupProcess.Kill();
                                            Debug.WriteLine($"Warm-up iteration {iteration}: terminated by timeout.");
                                        }
                                        catch (Exception killEx)
                                        {
                                            Debug.WriteLine($"Failed to kill warm-up process: {killEx.Message}");
                                        }
                                    }
                                    else
                                    {
                                        Debug.WriteLine($"Warm-up iteration {iteration}: completed in {warmupProcess.ExitTime - warmupProcess.StartTime}.");
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"Warm-up pass failed: {ex.Message}");
                            }
                            finally
                            {
                                warmupStopwatch.Stop();

                                // Delete warm-up file silently (fire-and-forget, no MessageBox)
                                _ = Task.Run(() =>
                                {
                                    try
                                    {
                                        if (File.Exists(warmupOutputFilePath))
                                        {
                                            File.SetAttributes(warmupOutputFilePath, FileAttributes.Normal);
                                            File.Delete(warmupOutputFilePath);
                                        }
                                    }
                                    catch
                                    {
                                        // Silent ignore: temp file deletion errors don't affect main process
                                        Debug.WriteLine($"Warning: Could not delete warm-up file: {warmupOutputFilePath}");
                                    }
                                });

                                Invoke((MethodInvoker)(() =>
                                {
                                    progressBarEncoder.ManualText = "";
                                }));

                                Debug.WriteLine($"Warm-up completed in {warmupStopwatch.Elapsed.TotalSeconds:F2}s after {iteration} iterations.");
                            }
                        });
                    }
                }
                // === END OF WARM-UP PASS ===

                // Set maximum values for the progress bar
                progressBarEncoder.Maximum = selectedEncoders.Count * selectedAudioFiles.Count;
                progressBarEncoder.ManualText = $"{progressBarEncoder.Value}/{progressBarEncoder.Maximum}";

                foreach (var audioFilePath in selectedAudioFiles)
                {
                    foreach (var encoderPath in selectedEncoders)
                    {
                        if (_isEncodingStopped)
                        {
                            isExecuting = false;
                            return;
                        }

                        // Form the parameter string
                        string compressionLevel = NormalizeSpaces(textBoxCompressionLevel.Text);
                        string threads = NormalizeSpaces(textBoxThreads.Text);
                        string commandLine = NormalizeSpaces(textBoxCommandLineOptionsEncoder.Text);
                        string parameters = $"-{compressionLevel} {commandLine}".Trim();

                        // Add thread count if it's greater than 1
                        if (int.TryParse(threads, out int threadCount) && threadCount > 1)
                        {
                            parameters += $" -j{threads}"; // add the -j{threads} flag
                        }

                        // Form the arguments for execution
                        string outputFilePath = Path.Combine(tempFolderPath, "temp_encoded.flac"); // Output file name
                        DeleteFileIfExists(outputFilePath); // Delete the old file
                        string arguments = $"\"{audioFilePath}\" {parameters} --no-preserve-modtime -f -o \"{outputFilePath}\"";

                        // Prepare for CPU clock monitoring
                        _cpuClockReadings = [];
                        var clockTimer = new System.Timers.Timer(20); // Read every 20ms
                        bool isFirstValue = true;
                        clockTimer.Elapsed += (s, e) =>
                        {
                            if (_cpuClockCounter != null && !_isEncodingStopped)
                            {
                                try
                                {
                                    double clock = _cpuClockCounter.NextValue();
                                    if (!isFirstValue && clock >= 1)
                                    {
                                        _cpuClockReadings.Add(clock);
                                    }
                                    isFirstValue = false;
                                }
                                catch (Exception ex)
                                {
                                    Debug.WriteLine($"Error reading CPU clock: {ex.Message}");
                                }
                            }
                        };

                        TimeSpan elapsedTime = TimeSpan.Zero;
                        TimeSpan userProcessorTime = TimeSpan.Zero;
                        TimeSpan privilegedProcessorTime = TimeSpan.Zero;
                        string errorOutput = string.Empty;
                        int exitCode = 0;

                        // Start the process and wait for completion
                        try
                        {
                            clockTimer.Start(); // Start clock monitoring

                            await Task.Run(() =>
                            {
                                if (_isPaused)
                                {
                                    _pauseEvent.WaitOne(); // Wait for pause in the background thread
                                }

                                using (_process = new Process())
                                {
                                    _process.StartInfo = new ProcessStartInfo
                                    {
                                        FileName = encoderPath,
                                        Arguments = arguments,
                                        UseShellExecute = false,
                                        CreateNoWindow = true,
                                        RedirectStandardError = true,
                                        StandardErrorEncoding = Encoding.UTF8,
                                    };

                                    var stopwatch = Stopwatch.StartNew();

                                    if (!_isEncodingStopped)
                                    {
                                        try
                                        {
                                            _process.Start();

                                            // Set process priority
                                            try
                                            {
                                                _process.PriorityClass = GetSelectedProcessPriority();
                                            }
                                            catch (InvalidOperationException)
                                            {
                                                // Process completed too early
                                            }

                                            errorOutput = _process.StandardError.ReadToEnd();
                                            _process.WaitForExit();
                                            exitCode = _process.ExitCode;
                                        }
                                        catch (Exception ex)
                                        {
                                            Debug.WriteLine($"Process start error: {ex.Message}");
                                        }
                                    }

                                    stopwatch.Stop();
                                    elapsedTime = stopwatch.Elapsed;

                                    // Get CPU times after process completion
                                    try
                                    {
                                        userProcessorTime = _process.UserProcessorTime;
                                        privilegedProcessorTime = _process.PrivilegedProcessorTime;
                                    }
                                    catch (InvalidOperationException)
                                    {
                                        // Process already exited
                                    }
                                }
                            });
                            clockTimer.Stop();

                            double avgClock = 0;
                            if (_cpuClockReadings.Count > 0 && _baseClockMhz > 0)
                            {
                                double avgPercent = _cpuClockReadings.Average();
                                avgClock = (avgPercent / 100.0) * _baseClockMhz;
                            }
                            if (!_isEncodingStopped)
                            {
                                // Check checkbox state
                                if (checkBoxRemoveMetadata.Checked)
                                {
                                    // Run metaflac.exe --remove-all if checkbox is checked
                                    try
                                    {
                                        using var metaflacProcess = new Process();
                                        metaflacProcess.StartInfo = new ProcessStartInfo
                                        {
                                            FileName = "metaflac.exe",
                                            Arguments = $"--remove-all --dont-use-padding \"{outputFilePath}\"",
                                            UseShellExecute = false,
                                            CreateNoWindow = true,
                                        };

                                        await Task.Run(() =>
                                        {
                                            metaflacProcess.Start();
                                            metaflacProcess.WaitForExit();
                                        });
                                    }
                                    catch (Exception ex)
                                    {
                                        MessageBox.Show($"Error removing metadata: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                    }
                                }
                            }

                            if (!_isEncodingStopped)
                            {
                                await LogProcessResults(outputFilePath, audioFilePath, parameters, encoderPath, elapsedTime, userProcessorTime, privilegedProcessorTime, avgClock, errorOutput, exitCode);
                            }
                        }
                        catch (Win32Exception winEx) when (unchecked((uint)winEx.NativeErrorCode) == 0xC000001D)
                        {
                            clockTimer.Stop();
                            string specificError = "Process failed: Illegal instruction (e.g. AVX-512 not supported on this CPU).";
                            if (!_isEncodingStopped)
                            {
                                await LogProcessResults(
                                    outputFilePath: "",
                                    audioFilePath: audioFilePath,
                                    parameters: parameters,
                                    encoderPath: encoderPath,
                                    elapsedTime: TimeSpan.Zero,
                                    userProcessorTime: TimeSpan.Zero,
                                    privilegedProcessorTime: TimeSpan.Zero,
                                    avgClock: 0,
                                    errorOutput: specificError,
                                    exitCode: -1
                                );
                            }
                            isExecuting = false;
                            return;
                        }
                        catch (Exception ex)
                        {
                            clockTimer.Stop();
                            if (!_isEncodingStopped)
                            {
                                await LogProcessResults(
                                    outputFilePath: "",
                                    audioFilePath: audioFilePath,
                                    parameters: parameters,
                                    encoderPath: encoderPath,
                                    elapsedTime: TimeSpan.Zero,
                                    userProcessorTime: TimeSpan.Zero,
                                    privilegedProcessorTime: TimeSpan.Zero,
                                    avgClock: 0,
                                    errorOutput: ex.Message,
                                    exitCode: -1
                                );
                            }
                            isExecuting = false;
                            return;
                        }
                        finally
                        {
                            progressBarEncoder.Invoke((MethodInvoker)(() =>
                            {
                                progressBarEncoder.Value++;
                                progressBarEncoder.ManualText = $"{progressBarEncoder.Value}/{progressBarEncoder.Maximum}";
                            }));
                        }
                    }
                }

                if (checkBoxAutoAnalyzeLog.Checked)
                {
                    await AnalyzeLogAsync();
                }
            }
            finally
            {
                isExecuting = false;
                _isPaused = false;
                _pauseEvent.Set();

                Invoke((MethodInvoker)(() =>
                {
                    buttonPauseResume.Text = "Pause";
                    buttonPauseResume.Enabled = false;
                    progressBarEncoder.Value = 0;
                    progressBarDecoder.Value = 0;
                    progressBarEncoder.ManualText = string.Empty;
                    progressBarDecoder.ManualText = string.Empty;
                }));
            }
        }
        private async void ButtonStartDecode_Click(object? sender, EventArgs e)
        {
            // Create a temporary directory for the output file
            Directory.CreateDirectory(tempFolderPath);

            if (isExecuting) return; // Check if process is already running
            isExecuting = true; // Set execution flag
            _isEncodingStopped = false;
            _isPaused = false;
            _pauseEvent.Set();

            Invoke((MethodInvoker)(() =>
            {
                buttonPauseResume.Text = "Pause";
                buttonPauseResume.Enabled = true;
                progressBarEncoder.Value = 0;
                progressBarDecoder.Value = 0;
                progressBarEncoder.ManualText = string.Empty;
                progressBarDecoder.ManualText = string.Empty;
            }));

            try
            {
                // Get selected encoders
                var selectedEncoders = listViewEncoders.Items.Cast<ListViewItem>()
                    .Where(item => item.Checked)
                    .Select(item => item.Tag!.ToString()!) // Get full path from Tag
                    .ToList();

                // Get all selected .flac audio files using cache
                var selectedFlacAudioFiles = listViewAudioFiles.Items.Cast<ListViewItem>()
                    .Where(item => item.Checked)
                    .Select(item => item.Tag!.ToString()!)
                    .Where(filePath => audioFileInfoCache[filePath].Extension == ".flac")
                    .ToList();

                // 1. Check if there is at least one encoder
                if (selectedEncoders.Count == 0)
                {
                    MessageBox.Show("Select at least one encoder.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    isExecuting = false;
                    return;
                }

                // 2. Check if there is at least one .flac audio file
                if (selectedFlacAudioFiles.Count == 0)
                {
                    MessageBox.Show("Select at least one FLAC audio file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    isExecuting = false;
                    return;
                }

                // === WARM-UP PASS (before main loop) ===
                if (checkBoxWarmupPass.Checked)
                {
                    var firstAudioFile = selectedFlacAudioFiles.FirstOrDefault();
                    var firstEncoder = selectedEncoders.FirstOrDefault();

                    if (firstAudioFile is not null && firstEncoder is not null)
                    {
                        // Use current UI settings to form parameters
                        string commandLine = NormalizeSpaces(textBoxCommandLineOptionsDecoder.Text);
                        string parameters = $"-d {commandLine}".Trim();

                        // Use unique filename for warm-up to avoid conflicts (interpolated string with $)
                        string warmupOutputFilePath = Path.Combine(tempFolderPath, $"temp_warmup_{Guid.NewGuid()}.wav");
                        string arguments = $"\"{firstAudioFile}\" {parameters} --no-preserve-modtime -f -o \"{warmupOutputFilePath}\"";

                        Invoke((MethodInvoker)(() =>
                        {
                            progressBarDecoder.ManualText = "Warming up...";
                        }));

                        // Run warm-up asynchronously
                        await Task.Run(() =>
                        {
                            var warmupStopwatch = Stopwatch.StartNew();
                            int iteration = 0;

                            try
                            {
                                while (warmupStopwatch.Elapsed < TimeSpan.FromSeconds(5) && !_isEncodingStopped)
                                {
                                    iteration++;
                                    DeleteFileIfExists(warmupOutputFilePath);

                                    using var warmupProcess = new Process();
                                    warmupProcess.StartInfo = new ProcessStartInfo
                                    {
                                        FileName = firstEncoder,
                                        Arguments = arguments,
                                        UseShellExecute = false,
                                        CreateNoWindow = true,
                                    };

                                    warmupProcess.Start();

                                    try
                                    {
                                        warmupProcess.PriorityClass = GetSelectedProcessPriority();
                                    }
                                    catch (InvalidOperationException) { }

                                    // Wait for process to exit OR until 5 seconds total elapsed
                                    bool exited = warmupProcess.WaitForExit(5000);

                                    if (!exited)
                                    {
                                        try
                                        {
                                            warmupProcess.Kill();
                                            Debug.WriteLine($"Warm-up iteration {iteration}: terminated by timeout.");
                                        }
                                        catch (Exception killEx)
                                        {
                                            Debug.WriteLine($"Failed to kill warm-up process: {killEx.Message}");
                                        }
                                    }
                                    else
                                    {
                                        Debug.WriteLine($"Warm-up iteration {iteration}: completed in {warmupProcess.ExitTime - warmupProcess.StartTime}.");
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"Warm-up pass failed: {ex.Message}");
                            }
                            finally
                            {
                                warmupStopwatch.Stop();

                                // Delete warm-up file silently (fire-and-forget, no MessageBox)
                                _ = Task.Run(() =>
                                {
                                    try
                                    {
                                        if (File.Exists(warmupOutputFilePath))
                                        {
                                            File.SetAttributes(warmupOutputFilePath, FileAttributes.Normal);
                                            File.Delete(warmupOutputFilePath);
                                        }
                                    }
                                    catch
                                    {
                                        // Silent ignore: temp file deletion errors don't affect main process
                                        Debug.WriteLine($"Warning: Could not delete warm-up file: {warmupOutputFilePath}");
                                    }
                                });

                                Invoke((MethodInvoker)(() =>
                                {
                                    progressBarDecoder.ManualText = "";
                                }));

                                Debug.WriteLine($"Warm-up completed in {warmupStopwatch.Elapsed.TotalSeconds:F2}s after {iteration} iterations.");
                            }
                        });
                    }
                }
                // === END OF WARM-UP PASS ===

                // Set maximum values for the progress bar
                progressBarDecoder.Maximum = selectedEncoders.Count * selectedFlacAudioFiles.Count;
                progressBarDecoder.ManualText = $"{progressBarDecoder.Value}/{progressBarDecoder.Maximum}";

                foreach (var audioFilePath in selectedFlacAudioFiles)
                {
                    foreach (var encoderPath in selectedEncoders)
                    {
                        if (_isEncodingStopped)
                        {
                            isExecuting = false;
                            return;
                        }

                        // Form the parameter string
                        string commandLine = NormalizeSpaces(textBoxCommandLineOptionsDecoder.Text);
                        string parameters = $"-d {commandLine}".Trim();

                        // Form the arguments for execution
                        string outputFilePath = Path.Combine(tempFolderPath, "temp_decoded.wav"); // Output file name
                        DeleteFileIfExists(outputFilePath); // Delete the old file
                        string arguments = $"\"{audioFilePath}\" {parameters} --no-preserve-modtime -f -o \"{outputFilePath}\"";

                        // Prepare for CPU clock monitoring
                        _cpuClockReadings = [];
                        var clockTimer = new System.Timers.Timer(20); // Read every 20ms
                        bool isFirstValue = true;
                        clockTimer.Elapsed += (s, e) =>
                        {
                            if (_cpuClockCounter != null && !_isEncodingStopped)
                            {
                                try
                                {
                                    double clock = _cpuClockCounter.NextValue();
                                    if (!isFirstValue && clock >= 1)
                                    {
                                        _cpuClockReadings.Add(clock);
                                    }
                                    isFirstValue = false;
                                }
                                catch (Exception ex)
                                {
                                    Debug.WriteLine($"Error reading CPU clock: {ex.Message}");
                                }
                            }
                        };

                        TimeSpan elapsedTime = TimeSpan.Zero;
                        TimeSpan userProcessorTime = TimeSpan.Zero;
                        TimeSpan privilegedProcessorTime = TimeSpan.Zero;
                        string errorOutput = string.Empty;
                        int exitCode = 0;

                        // Start the process and wait for completion
                        try
                        {
                            clockTimer.Start(); // Start clock monitoring

                            await Task.Run(() =>
                            {
                                if (_isPaused)
                                {
                                    _pauseEvent.WaitOne(); // Wait for pause in the background thread
                                }

                                using (_process = new Process())
                                {
                                    _process.StartInfo = new ProcessStartInfo
                                    {
                                        FileName = encoderPath,
                                        Arguments = arguments,
                                        UseShellExecute = false,
                                        CreateNoWindow = true,
                                        RedirectStandardError = true,
                                        StandardErrorEncoding = Encoding.UTF8,
                                    };

                                    var stopwatch = Stopwatch.StartNew();

                                    if (!_isEncodingStopped)
                                    {
                                        try
                                        {
                                            _process.Start();

                                            // Set process priority
                                            try
                                            {
                                                _process.PriorityClass = GetSelectedProcessPriority();
                                            }
                                            catch (InvalidOperationException)
                                            {
                                                // Process completed too early
                                            }

                                            errorOutput = _process.StandardError.ReadToEnd();
                                            _process.WaitForExit();
                                            exitCode = _process.ExitCode;
                                        }
                                        catch (Exception ex)
                                        {
                                            Debug.WriteLine($"Process start error: {ex.Message}");
                                        }
                                    }

                                    stopwatch.Stop();
                                    elapsedTime = stopwatch.Elapsed;

                                    // Get CPU times after process completion
                                    try
                                    {
                                        userProcessorTime = _process.UserProcessorTime;
                                        privilegedProcessorTime = _process.PrivilegedProcessorTime;
                                    }
                                    catch (InvalidOperationException)
                                    {
                                        // Process already exited
                                    }
                                }
                            });
                            clockTimer.Stop();

                            double avgClock = 0;
                            if (_cpuClockReadings.Count > 0 && _baseClockMhz > 0)
                            {
                                double avgPercent = _cpuClockReadings.Average();
                                avgClock = (avgPercent / 100.0) * _baseClockMhz;
                            }

                            if (!_isEncodingStopped)
                            {
                                await LogProcessResults(outputFilePath, audioFilePath, parameters, encoderPath, elapsedTime, userProcessorTime, privilegedProcessorTime, avgClock, errorOutput, exitCode);
                            }
                        }
                        catch (Win32Exception winEx) when (unchecked((uint)winEx.NativeErrorCode) == 0xC000001D)
                        {
                            clockTimer.Stop();
                            string specificError = "Process failed: Illegal instruction (e.g. AVX-512 not supported on this CPU).";
                            if (!_isEncodingStopped)
                            {
                                await LogProcessResults(
                                    outputFilePath: "",
                                    audioFilePath: audioFilePath,
                                    parameters: parameters,
                                    encoderPath: encoderPath,
                                    elapsedTime: TimeSpan.Zero,
                                    userProcessorTime: TimeSpan.Zero,
                                    privilegedProcessorTime: TimeSpan.Zero,
                                    avgClock: 0,
                                    errorOutput: specificError,
                                    exitCode: -1
                                );
                            }
                            isExecuting = false;
                            return;
                        }
                        catch (Exception ex)
                        {
                            clockTimer.Stop();
                            if (!_isEncodingStopped)
                            {
                                await LogProcessResults(
                                    outputFilePath: "",
                                    audioFilePath: audioFilePath,
                                    parameters: parameters,
                                    encoderPath: encoderPath,
                                    elapsedTime: TimeSpan.Zero,
                                    userProcessorTime: TimeSpan.Zero,
                                    privilegedProcessorTime: TimeSpan.Zero,
                                    avgClock: 0,
                                    errorOutput: ex.Message,
                                    exitCode: -1
                                );
                            }
                            isExecuting = false;
                            return;
                        }
                        finally
                        {
                            progressBarDecoder.Invoke((MethodInvoker)(() =>
                            {
                                progressBarDecoder.Value++;
                                progressBarDecoder.ManualText = $"{progressBarDecoder.Value}/{progressBarDecoder.Maximum}";
                            }));
                        }
                    }
                }

                if (checkBoxAutoAnalyzeLog.Checked)
                {
                    await AnalyzeLogAsync();
                }
            }
            finally
            {
                isExecuting = false;
                _isPaused = false;
                _pauseEvent.Set();

                Invoke((MethodInvoker)(() =>
                {
                    buttonPauseResume.Text = "Pause";
                    buttonPauseResume.Enabled = false;
                    progressBarEncoder.Value = 0;
                    progressBarDecoder.Value = 0;
                    progressBarEncoder.ManualText = string.Empty;
                    progressBarDecoder.ManualText = string.Empty;
                }));
            }
        }
        private async void ButtonStartJobList_Click(object? sender, EventArgs e)
        {
            // Create a temporary directory for the output file
            Directory.CreateDirectory(tempFolderPath);

            if (isExecuting) return; // Check if process is already running
            isExecuting = true; // Set execution flag
            _isEncodingStopped = false;
            _isPaused = false;
            _pauseEvent.Set();

            Invoke((MethodInvoker)(() =>
            {
                buttonPauseResume.Text = "Pause";
                buttonPauseResume.Enabled = true;
                progressBarEncoder.Value = 0;
                progressBarDecoder.Value = 0;
                progressBarEncoder.ManualText = string.Empty;
                progressBarDecoder.ManualText = string.Empty;
            }));

            try
            {
                // Get selected encoders
                var selectedEncoders = listViewEncoders.Items.Cast<ListViewItem>()
                    .Where(item => item.Checked)
                    .Select(item => item.Tag!.ToString()!) // Get full path from Tag
                    .ToList();

                // Get all selected .wav and .flac audio files using cache
                var selectedAudioFiles = listViewAudioFiles.Items.Cast<ListViewItem>()
                    .Where(item => item.Checked)
                    .Select(item => item.Tag!.ToString()!)
                    .Where(filePath =>
                    {
                        string extension = audioFileInfoCache[filePath].Extension;
                        return extension == ".wav" || extension == ".flac";
                    })
                    .ToList();

                // Get all selected .flac audio files using cache
                var selectedFlacAudioFiles = selectedAudioFiles
                    .Where(filePath => audioFileInfoCache[filePath].Extension == ".flac")
                    .ToList();

                // Create expanded Job List (Virtual Job List)
                var dataGridViewJobsExpanded = new List<DataGridViewRow>();

                foreach (DataGridViewRow row in dataGridViewJobs.Rows)
                {
                    if (row.IsNewRow) continue;

                    var checkBoxCell = row.Cells[0] as DataGridViewCheckBoxCell;
                    if (checkBoxCell?.Value is bool isChecked && isChecked)
                    {
                        string jobType = row.Cells[1].Value?.ToString() ?? "";
                        string passes = row.Cells[2].Value?.ToString() ?? "";
                        string parameters = NormalizeSpaces((row.Cells[3].Value?.ToString() ?? "").Trim());

                        // Check if parameters contain script patterns (like [0..8] or [1,2,3])
                        if (parameters.Contains('[') && parameters.Contains(']'))
                        {
                            // Expand script using ScriptParser
                            var expandedParameters = ScriptParser.ExpandScriptLine(parameters);

                            foreach (string expandedParam in expandedParameters)
                            {
                                // Create new job row for each expanded parameter set
                                DataGridViewRow newRow = (DataGridViewRow)row.Clone();
                                newRow.Cells[0].Value = true;
                                newRow.Cells[1].Value = jobType;
                                newRow.Cells[2].Value = passes;
                                newRow.Cells[3].Value = expandedParam;
                                dataGridViewJobsExpanded.Add(newRow);
                            }
                        }
                        else
                        {
                            // Regular job without script expansion
                            dataGridViewJobsExpanded.Add(row);
                        }
                    }
                }

                // Count the number of tasks and passes for Encode
                int totalEncodeTasks = dataGridViewJobsExpanded
                    .Where(row => string.Equals(NormalizeSpaces(row.Cells[1].Value?.ToString() ?? ""), "Encode", StringComparison.OrdinalIgnoreCase))
                    .Sum(row => int.Parse((row.Cells[2].Value?.ToString() ?? "").Trim()));

                // Count the number of tasks and passes for Decode
                int totalDecodeTasks = dataGridViewJobsExpanded
                    .Where(row => string.Equals(NormalizeSpaces(row.Cells[1].Value?.ToString() ?? ""), "Decode", StringComparison.OrdinalIgnoreCase))
                    .Sum(row => int.Parse((row.Cells[2].Value?.ToString() ?? "").Trim()));

                // 1. Check if there is at least one job
                if (totalEncodeTasks == 0 && totalDecodeTasks == 0)
                {
                    MessageBox.Show("Select at least one job.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    isExecuting = false;
                    return;
                }

                // 2. Check if there is at least one Encoder
                if (selectedEncoders.Count == 0)
                {
                    MessageBox.Show("Select at least one Encoder.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    isExecuting = false;
                    return;
                }

                // 3. Check if there are audio files for the tasks
                if (totalEncodeTasks > 0)
                {
                    // For encoding: any WAV/FLAC files
                    if (selectedAudioFiles.Count == 0)
                    {
                        MessageBox.Show("Select at least one Audio File (WAV or FLAC).", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        isExecuting = false;
                        return;
                    }
                }
                else if (totalDecodeTasks > 0)
                {
                    // For decoding: only FLAC files
                    if (selectedFlacAudioFiles.Count == 0)
                    {
                        MessageBox.Show("Select at least one FLAC file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        isExecuting = false;
                        return;
                    }
                }

                // === WARM-UP PASS (before main loop) ===
                if (checkBoxWarmupPass.Checked)
                {
                    // Find the first job in dataGridViewJobsExpanded that will actually be executed
                    DataGridViewRow? firstExecutableJobRow = null;
                    string? jobType = null;
                    string? audioFilePath = null;
                    string? warmupOutputFilePath = null;

                    foreach (var jobRow in dataGridViewJobsExpanded)
                    {
                        string type = NormalizeSpaces(jobRow.Cells[1].Value?.ToString() ?? "");
                        if (string.Equals(type, "Encode", StringComparison.OrdinalIgnoreCase) && selectedAudioFiles.Count > 0)
                        {
                            firstExecutableJobRow = jobRow;
                            jobType = type;
                            audioFilePath = selectedAudioFiles.First();
                            // Use unique filename for warm-up with interpolated string ($)
                            warmupOutputFilePath = Path.Combine(tempFolderPath, $"temp_warmup_{Guid.NewGuid()}.flac");
                            break;
                        }
                        else if (string.Equals(type, "Decode", StringComparison.OrdinalIgnoreCase) && selectedFlacAudioFiles.Count > 0)
                        {
                            firstExecutableJobRow = jobRow;
                            jobType = type;
                            audioFilePath = selectedFlacAudioFiles.First();
                            // Use unique filename for warm-up with interpolated string ($)
                            warmupOutputFilePath = Path.Combine(tempFolderPath, $"temp_warmup_{Guid.NewGuid()}.wav");
                            break;
                        }
                    }

                    if (firstExecutableJobRow == null || audioFilePath == null || warmupOutputFilePath == null)
                        return;

                    string parameters = NormalizeSpaces((firstExecutableJobRow.Cells[3].Value?.ToString() ?? "").Trim());
                    var firstEncoder = selectedEncoders.FirstOrDefault();
                    if (string.IsNullOrEmpty(firstEncoder)) return;

                    string arguments = $"\"{audioFilePath}\" {parameters} --no-preserve-modtime -f -o \"{warmupOutputFilePath}\"";

                    Invoke((MethodInvoker)(() =>
                    {
                        progressBarEncoder.ManualText = "Warming up...";
                        progressBarDecoder.ManualText = "Warming up...";
                    }));

                    await Task.Run(() =>
                    {
                        var warmupStopwatch = Stopwatch.StartNew();
                        int iteration = 0;

                        try
                        {
                            while (warmupStopwatch.Elapsed < TimeSpan.FromSeconds(5) && !_isEncodingStopped)
                            {
                                iteration++;
                                DeleteFileIfExists(warmupOutputFilePath);

                                using var warmupProcess = new Process();
                                warmupProcess.StartInfo = new ProcessStartInfo
                                {
                                    FileName = firstEncoder,
                                    Arguments = arguments,
                                    UseShellExecute = false,
                                    CreateNoWindow = true,
                                };

                                warmupProcess.Start();

                                try
                                {
                                    warmupProcess.PriorityClass = GetSelectedProcessPriority();
                                }
                                catch (InvalidOperationException) { }

                                // Wait for process to exit OR until 5 seconds total elapsed
                                bool exited = warmupProcess.WaitForExit(5000);

                                if (!exited)
                                {
                                    try
                                    {
                                        warmupProcess.Kill();
                                        Debug.WriteLine($"Warm-up iteration {iteration}: terminated by timeout.");
                                    }
                                    catch (Exception killEx)
                                    {
                                        Debug.WriteLine($"Failed to kill warm-up process: {killEx.Message}");
                                    }
                                }
                                else
                                {
                                    Debug.WriteLine($"Warm-up iteration {iteration}: completed in {warmupProcess.ExitTime - warmupProcess.StartTime}.");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Warm-up pass failed: {ex.Message}");
                        }
                        finally
                        {
                            warmupStopwatch.Stop();

                            // Delete warm-up file silently (fire-and-forget, no MessageBox)
                            _ = Task.Run(() =>
                            {
                                try
                                {
                                    if (File.Exists(warmupOutputFilePath))
                                    {
                                        File.SetAttributes(warmupOutputFilePath, FileAttributes.Normal);
                                        File.Delete(warmupOutputFilePath);
                                    }
                                }
                                catch
                                {
                                    // Silent ignore: temp file deletion errors don't affect main process
                                    Debug.WriteLine($"Warning: Could not delete warm-up file: {warmupOutputFilePath}");
                                }
                            });

                            Invoke((MethodInvoker)(() =>
                            {
                                progressBarEncoder.ManualText = "";
                                progressBarDecoder.ManualText = "";
                            }));

                            Debug.WriteLine($"Warm-up completed in {warmupStopwatch.Elapsed.TotalSeconds:F2}s after {iteration} iterations.");
                        }
                    });
                }
                // === END OF WARM-UP PASS ===

                // Set maximum values for progress bars
                progressBarEncoder.Maximum = selectedEncoders.Count * selectedAudioFiles.Count * totalEncodeTasks;
                progressBarDecoder.Maximum = selectedEncoders.Count * selectedFlacAudioFiles.Count * totalDecodeTasks;
                progressBarEncoder.ManualText = $"{progressBarEncoder.Value}/{progressBarEncoder.Maximum}";
                progressBarDecoder.ManualText = $"{progressBarDecoder.Value}/{progressBarDecoder.Maximum}";

                foreach (DataGridViewRow row in dataGridViewJobsExpanded)
                {
                    string jobType = NormalizeSpaces(row.Cells[1].Value?.ToString() ?? "");
                    int passes = int.TryParse(row.Cells[2].Value?.ToString(), out int p) ? p : 1;

                    for (int i = 0; i < passes; i++) // Loop for the number of passes
                    {
                        if (_isEncodingStopped)
                        {
                            isExecuting = false;
                            return;
                        }

                        if (string.Equals(jobType, "Encode", StringComparison.OrdinalIgnoreCase))
                        {
                            foreach (var audioFilePath in selectedAudioFiles)
                            {
                                foreach (var encoderPath in selectedEncoders)
                                {
                                    if (_isEncodingStopped)
                                    {
                                        isExecuting = false;
                                        return;
                                    }

                                    // Form the parameter string
                                    string parameters = NormalizeSpaces((row.Cells[3].Value?.ToString() ?? "").Trim());

                                    // Form the arguments for execution
                                    string outputFilePath = Path.Combine(tempFolderPath, "temp_encoded.flac"); // Output file name
                                    DeleteFileIfExists(outputFilePath); // Delete the old file
                                    string arguments = $"\"{audioFilePath}\" {parameters} --no-preserve-modtime -f -o \"{outputFilePath}\"";

                                    // Prepare for CPU clock monitoring
                                    _cpuClockReadings = [];
                                    var clockTimer = new System.Timers.Timer(20); // Read every 20ms
                                    bool isFirstValue = true;
                                    clockTimer.Elapsed += (s, e) =>
                                    {
                                        if (_cpuClockCounter != null && !_isEncodingStopped)
                                        {
                                            try
                                            {
                                                double clock = _cpuClockCounter.NextValue();
                                                if (!isFirstValue && clock >= 1)
                                                {
                                                    _cpuClockReadings.Add(clock);
                                                }
                                                isFirstValue = false;
                                            }
                                            catch (Exception ex)
                                            {
                                                Debug.WriteLine($"Error reading CPU clock: {ex.Message}");
                                            }
                                        }
                                    };

                                    TimeSpan elapsedTime = TimeSpan.Zero;
                                    TimeSpan userProcessorTime = TimeSpan.Zero;
                                    TimeSpan privilegedProcessorTime = TimeSpan.Zero;
                                    string errorOutput = string.Empty;
                                    int exitCode = 0;

                                    // Start the process and wait for completion
                                    try
                                    {
                                        clockTimer.Start(); // Start clock monitoring

                                        await Task.Run(() =>
                                        {
                                            if (_isPaused)
                                            {
                                                _pauseEvent.WaitOne(); // Wait for pause in the background thread
                                            }

                                            using (_process = new Process())
                                            {
                                                _process.StartInfo = new ProcessStartInfo
                                                {
                                                    FileName = encoderPath,
                                                    Arguments = arguments,
                                                    UseShellExecute = false,
                                                    CreateNoWindow = true,
                                                    RedirectStandardError = true,
                                                    StandardErrorEncoding = Encoding.UTF8,
                                                };

                                                var stopwatch = Stopwatch.StartNew();

                                                if (!_isEncodingStopped)
                                                {
                                                    try
                                                    {
                                                        _process.Start();

                                                        // Set process priority
                                                        try
                                                        {
                                                            _process.PriorityClass = GetSelectedProcessPriority();
                                                        }
                                                        catch (InvalidOperationException)
                                                        {
                                                            // Process completed too early
                                                        }

                                                        errorOutput = _process.StandardError.ReadToEnd();
                                                        _process.WaitForExit();
                                                        exitCode = _process.ExitCode;
                                                    }
                                                    catch (Exception ex)
                                                    {
                                                        Debug.WriteLine($"Process start error: {ex.Message}");
                                                    }
                                                }

                                                stopwatch.Stop();
                                                elapsedTime = stopwatch.Elapsed;

                                                // Get CPU times after process completion
                                                try
                                                {
                                                    userProcessorTime = _process.UserProcessorTime;
                                                    privilegedProcessorTime = _process.PrivilegedProcessorTime;
                                                }
                                                catch (InvalidOperationException)
                                                {
                                                    // Process already exited
                                                }
                                            }
                                        });
                                        clockTimer.Stop();

                                        double avgClock = 0;
                                        if (_cpuClockReadings.Count > 0 && _baseClockMhz > 0)
                                        {
                                            double avgPercent = _cpuClockReadings.Average();
                                            avgClock = (avgPercent / 100.0) * _baseClockMhz;
                                        }
                                        if (!_isEncodingStopped)
                                        {
                                            // Check checkbox state
                                            if (checkBoxRemoveMetadata.Checked)
                                            {
                                                // Run metaflac.exe --remove-all if checkbox is checked
                                                try
                                                {
                                                    using var metaflacProcess = new Process();
                                                    metaflacProcess.StartInfo = new ProcessStartInfo
                                                    {
                                                        FileName = "metaflac.exe",
                                                        Arguments = $"--remove-all --dont-use-padding \"{outputFilePath}\"",
                                                        UseShellExecute = false,
                                                        CreateNoWindow = true,
                                                    };

                                                    await Task.Run(() =>
                                                    {
                                                        metaflacProcess.Start();
                                                        metaflacProcess.WaitForExit();
                                                    });
                                                }
                                                catch (Exception ex)
                                                {
                                                    MessageBox.Show($"Error removing metadata: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                                }
                                            }
                                        }

                                        if (!_isEncodingStopped)
                                        {
                                            await LogProcessResults(outputFilePath, audioFilePath, parameters, encoderPath, elapsedTime, userProcessorTime, privilegedProcessorTime, avgClock, errorOutput, exitCode);
                                        }
                                    }
                                    catch (Win32Exception winEx) when (unchecked((uint)winEx.NativeErrorCode) == 0xC000001D)
                                    {
                                        clockTimer.Stop();
                                        string specificError = "Process failed: Illegal instruction (e.g. AVX-512 not supported on this CPU).";
                                        if (!_isEncodingStopped)
                                        {
                                            await LogProcessResults(
                                                outputFilePath: "",
                                                audioFilePath: audioFilePath,
                                                parameters: parameters,
                                                encoderPath: encoderPath,
                                                elapsedTime: TimeSpan.Zero,
                                                userProcessorTime: TimeSpan.Zero,
                                                privilegedProcessorTime: TimeSpan.Zero,
                                                avgClock: 0,
                                                errorOutput: specificError,
                                                exitCode: -1
                                            );
                                        }
                                        isExecuting = false;
                                        return;
                                    }
                                    catch (Exception ex)
                                    {
                                        clockTimer.Stop();
                                        if (!_isEncodingStopped)
                                        {
                                            await LogProcessResults(
                                                outputFilePath: "",
                                                audioFilePath: audioFilePath,
                                                parameters: parameters,
                                                encoderPath: encoderPath,
                                                elapsedTime: TimeSpan.Zero,
                                                userProcessorTime: TimeSpan.Zero,
                                                privilegedProcessorTime: TimeSpan.Zero,
                                                avgClock: 0,
                                                errorOutput: ex.Message,
                                                exitCode: -1
                                            );
                                        }
                                        isExecuting = false;
                                        return;
                                    }
                                    finally
                                    {
                                        progressBarEncoder.Invoke((MethodInvoker)(() =>
                                        {
                                            progressBarEncoder.Value++;
                                            progressBarEncoder.ManualText = $"{progressBarEncoder.Value}/{progressBarEncoder.Maximum}";
                                        }));
                                    }
                                }
                            }
                        }
                        else if (string.Equals(jobType, "Decode", StringComparison.OrdinalIgnoreCase))
                        {
                            foreach (var audioFilePath in selectedFlacAudioFiles)
                            {
                                foreach (var encoderPath in selectedEncoders)
                                {
                                    if (_isEncodingStopped)
                                    {
                                        isExecuting = false;
                                        return;
                                    }

                                    // Form the parameter string
                                    string parameters = NormalizeSpaces((row.Cells[3].Value?.ToString() ?? "").Trim());

                                    // Form the arguments for execution
                                    string outputFilePath = Path.Combine(tempFolderPath, "temp_decoded.wav"); // Output file name
                                    DeleteFileIfExists(outputFilePath); // Delete the old file
                                    string arguments = $"\"{audioFilePath}\" {parameters} --no-preserve-modtime -f -o \"{outputFilePath}\"";

                                    // Prepare for CPU clock monitoring
                                    _cpuClockReadings = [];
                                    var clockTimer = new System.Timers.Timer(20); // Read every 20ms
                                    bool isFirstValue = true;
                                    clockTimer.Elapsed += (s, e) =>
                                    {
                                        if (_cpuClockCounter != null && !_isEncodingStopped)
                                        {
                                            try
                                            {
                                                double clock = _cpuClockCounter.NextValue();
                                                if (!isFirstValue && clock >= 1)
                                                {
                                                    _cpuClockReadings.Add(clock);
                                                }
                                                isFirstValue = false;
                                            }
                                            catch (Exception ex)
                                            {
                                                Debug.WriteLine($"Error reading CPU clock: {ex.Message}");
                                            }
                                        }
                                    };

                                    TimeSpan elapsedTime = TimeSpan.Zero;
                                    TimeSpan userProcessorTime = TimeSpan.Zero;
                                    TimeSpan privilegedProcessorTime = TimeSpan.Zero;
                                    string errorOutput = string.Empty;
                                    int exitCode = 0;

                                    // Start the process and wait for completion
                                    try
                                    {
                                        clockTimer.Start(); // Start clock monitoring

                                        await Task.Run(() =>
                                        {
                                            if (_isPaused)
                                            {
                                                _pauseEvent.WaitOne(); // Wait for pause in the background thread
                                            }

                                            using (_process = new Process())
                                            {
                                                _process.StartInfo = new ProcessStartInfo
                                                {
                                                    FileName = encoderPath,
                                                    Arguments = arguments,
                                                    UseShellExecute = false,
                                                    CreateNoWindow = true,
                                                    RedirectStandardError = true,
                                                    StandardErrorEncoding = Encoding.UTF8,
                                                };

                                                var stopwatch = Stopwatch.StartNew();

                                                if (!_isEncodingStopped)
                                                {
                                                    try
                                                    {
                                                        _process.Start();

                                                        // Set process priority
                                                        try
                                                        {
                                                            _process.PriorityClass = GetSelectedProcessPriority();
                                                        }
                                                        catch (InvalidOperationException)
                                                        {
                                                            // Process completed too early
                                                        }

                                                        errorOutput = _process.StandardError.ReadToEnd();
                                                        _process.WaitForExit();
                                                        exitCode = _process.ExitCode;
                                                    }
                                                    catch (Exception ex)
                                                    {
                                                        Debug.WriteLine($"Process start error: {ex.Message}");
                                                    }
                                                }

                                                stopwatch.Stop();
                                                elapsedTime = stopwatch.Elapsed;

                                                // Get CPU times after process completion
                                                try
                                                {
                                                    userProcessorTime = _process.UserProcessorTime;
                                                    privilegedProcessorTime = _process.PrivilegedProcessorTime;
                                                }
                                                catch (InvalidOperationException)
                                                {
                                                    // Process already exited
                                                }
                                            }
                                        });
                                        clockTimer.Stop();

                                        double avgClock = 0;
                                        if (_cpuClockReadings.Count > 0 && _baseClockMhz > 0)
                                        {
                                            double avgPercent = _cpuClockReadings.Average();
                                            avgClock = (avgPercent / 100.0) * _baseClockMhz;
                                        }

                                        if (!_isEncodingStopped)
                                        {
                                            await LogProcessResults(outputFilePath, audioFilePath, parameters, encoderPath, elapsedTime, userProcessorTime, privilegedProcessorTime, avgClock, errorOutput, exitCode);
                                        }
                                    }
                                    catch (Win32Exception winEx) when (unchecked((uint)winEx.NativeErrorCode) == 0xC000001D)
                                    {
                                        clockTimer.Stop();
                                        string specificError = "Process failed: Illegal instruction (e.g. AVX-512 not supported on this CPU).";
                                        if (!_isEncodingStopped)
                                        {
                                            await LogProcessResults(
                                                outputFilePath: "",
                                                audioFilePath: audioFilePath,
                                                parameters: parameters,
                                                encoderPath: encoderPath,
                                                elapsedTime: TimeSpan.Zero,
                                                userProcessorTime: TimeSpan.Zero,
                                                privilegedProcessorTime: TimeSpan.Zero,
                                                avgClock: 0,
                                                errorOutput: specificError,
                                                exitCode: -1
                                            );
                                        }
                                        isExecuting = false;
                                        return;
                                    }
                                    catch (Exception ex)
                                    {
                                        clockTimer.Stop();
                                        if (!_isEncodingStopped)
                                        {
                                            await LogProcessResults(
                                                outputFilePath: "",
                                                audioFilePath: audioFilePath,
                                                parameters: parameters,
                                                encoderPath: encoderPath,
                                                elapsedTime: TimeSpan.Zero,
                                                userProcessorTime: TimeSpan.Zero,
                                                privilegedProcessorTime: TimeSpan.Zero,
                                                avgClock: 0,
                                                errorOutput: ex.Message,
                                                exitCode: -1
                                            );
                                        }
                                        isExecuting = false;
                                        return;
                                    }
                                    finally
                                    {
                                        progressBarDecoder.Invoke((MethodInvoker)(() =>
                                        {
                                            progressBarDecoder.Value++;
                                            progressBarDecoder.ManualText = $"{progressBarDecoder.Value}/{progressBarDecoder.Maximum}";
                                        }));
                                    }
                                }
                            }
                        }
                    }
                }

                if (checkBoxAutoAnalyzeLog.Checked)
                {
                    await AnalyzeLogAsync();
                }
            }
            finally
            {
                isExecuting = false;
                _isPaused = false;
                _pauseEvent.Set();

                Invoke((MethodInvoker)(() =>
                {
                    buttonPauseResume.Text = "Pause";
                    buttonPauseResume.Enabled = false;
                    progressBarEncoder.Value = 0;
                    progressBarDecoder.Value = 0;
                    progressBarEncoder.ManualText = string.Empty;
                    progressBarDecoder.ManualText = string.Empty;
                }));
            }
        }

        private async void ButtonDetectDupesAudioFiles_Click(object? sender, EventArgs e)
        {
            var button = (Button)sender!;
            var originalText = button.Text;
            var cts = new CancellationTokenSource();

            // Declare variables for summary message
            Dictionary<string, List<string>> hashDict = null!;
            List<string> filesWithMD5Errors = null!;

            try
            {
                // --- STAGE 0: PREPARE USER INTERFACE ---
                button.Invoke((MethodInvoker)(() =>
                {
                    button.Text = "In progress...";
                    button.Enabled = false;
                }));

                // --- STAGE 0.1: CHECK FILE EXISTENCE AND CLEAN UP LISTVIEW ---
                var itemsToRemove = new List<ListViewItem>();
                Invoke((MethodInvoker)delegate
                {
                    foreach (ListViewItem item in listViewAudioFiles.Items)
                    {
                        string filePath = item.Tag!.ToString()!;
                        if (!File.Exists(filePath))
                        {
                            itemsToRemove.Add(item);
                        }
                    }

                    foreach (var item in itemsToRemove)
                    {
                        listViewAudioFiles.Items.Remove(item);
                    }

                    UpdateGroupBoxAudioFilesHeader();
                    if (itemsToRemove.Count > 0)
                    {
                        var count = itemsToRemove.Count;

                        ShowTemporaryAudioFileRemovedMessage(
                            $"{count} {(count == 1 ? "file was" : "files were")} not found on disk and {(count == 1 ? "has" : "have")} been removed from the list."
                        );
                    }
                });

                if (listViewAudioFiles.Items.Count == 0)
                {
                    MessageBox.Show("No audio files to process.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // --- STAGE 0.2: COLLECT FILE PATHS (on UI thread) ---
                var filePaths = new List<string>();
                Invoke((MethodInvoker)delegate
                {
                    filePaths.AddRange(listViewAudioFiles.Items.Cast<ListViewItem>().Select(item => item.Tag!.ToString()!));
                });

                // --- STAGE 1: PERFORM DUPLICATE DETECTION IN BACKGROUND THREAD ---
                await Task.Run(async () =>
                {
                    hashDict = []; // Group files by MD5 hash.
                    filesWithMD5Errors = []; // Track paths of files with MD5 errors.
                    var itemsToCheck = new List<string>();   // Paths of files to mark as checked (primary).
                    var itemsToUncheck = new List<string>(); // Paths of files to mark as unchecked (non-primary duplicates).

                    // --- STAGE 1.1: CALCULATE OR RETRIEVE MD5 HASHES ---
                    foreach (string filePath in filePaths)
                    {
                        if (cts.Token.IsCancellationRequested)
                            return;

                        string md5Hash = audioFileInfoCache[filePath].Md5Hash;

                        if (string.IsNullOrEmpty(md5Hash) ||
                        md5Hash == "MD5 calculation failed" ||
                        md5Hash == "00000000000000000000000000000000" ||
                        md5Hash == "N/A")
                        {
                            string fileExtension = audioFileInfoCache[filePath].Extension;
                            if (fileExtension == ".wav")
                            {
                                md5Hash = await CalculateWavMD5Async(filePath);
                            }
                            else if (fileExtension == ".flac")
                            {
                                md5Hash = await CalculateFlacMD5Async(filePath);
                            }
                            else
                            {
                                md5Hash = "MD5 calculation failed";
                            }

                            // Update the cache with the new MD5 hash
                            audioFileInfoCache[filePath].Md5Hash = md5Hash;

                        }

                        if (!string.IsNullOrEmpty(md5Hash) && md5Hash != "MD5 calculation failed")
                        {
                            if (!hashDict.ContainsKey(md5Hash))
                                hashDict[md5Hash] = [];
                            hashDict[md5Hash].Add(filePath);
                        }
                        else
                        {
                            filesWithMD5Errors.Add(filePath);
                        }
                    }

                    if (cts.Token.IsCancellationRequested)
                        return;

                    // --- STAGE 1.2: DETERMINE PRIMARY DUPLICATE IN EACH GROUP ---
                    foreach (var kvp in hashDict.Where(g => g.Value.Count > 1))
                    {
                        var sortedPaths = kvp.Value
                    .Select(path =>
                    {
                        var info = audioFileInfoCache[path];
                        return new { Path = path, Info = info };
                    })
                        .OrderBy(x => x.Info.Extension == ".flac" ? 0 : 1)  // FLAC > WAV
                        .ThenBy(x => x.Info.DirectoryPath.Length)           // Shorter path first
                        .ThenByDescending(x => x.Info.LastWriteTime)        // Newer first
                        .ThenBy(x => x.Path)                                // Then by path
                        .ToList();

                        if (sortedPaths.Count > 0)
                        {
                            itemsToCheck.Add(sortedPaths[0].Path); // Primary file
                            itemsToUncheck.AddRange(sortedPaths.Skip(1).Select(x => x.Path)); // Others
                        }
                    }

                    // --- STAGE 2: UPDATE USER INTERFACE (on UI thread) ---
                    Invoke((MethodInvoker)delegate
                    {
                        // --- STAGE 2.1: CLEAR PREVIOUS RESULTS FROM LOG ---
                        for (int i = dataGridViewLogDetectDupes.Rows.Count - 1; i >= 0; i--)
                        {
                            DataGridViewRow row = dataGridViewLogDetectDupes.Rows[i];
                            if (row.Cells["MD5"].Value?.ToString() == "MD5 calculation failed" || !string.IsNullOrEmpty(row.Cells["Duplicates"].Value?.ToString()))
                            {
                                dataGridViewLogDetectDupes.Rows.RemoveAt(i);
                            }
                        }

                        // --- STAGE 2.2: UPDATE CHECKBOX STATES IN LISTVIEW ---
                        foreach (ListViewItem item in listViewAudioFiles.Items)
                        {
                            string path = item.Tag!.ToString()!;
                            item.Checked = !itemsToUncheck.Contains(path); // Uncheck non-primary duplicates
                        }

                        // --- STAGE 2.3: UPDATE MD5 DISPLAY IN LISTVIEW ---
                        foreach (ListViewItem item in listViewAudioFiles.Items)
                        {
                            string path = item.Tag!.ToString()!;
                            var info = audioFileInfoCache[path];
                            if (item.SubItems.Count > 6 && item.SubItems[6].Text != info.Md5Hash)
                            {
                                item.SubItems[6].Text = info.Md5Hash;
                            }
                        }

                        // --- STAGE 2.4: LOG MD5 CALCULATION ERROR RESULTS ---
                        foreach (string filePath in filesWithMD5Errors)
                        {
                            var info = audioFileInfoCache[filePath];

                            int rowIndex = dataGridViewLogDetectDupes.Rows.Add(
                            info.FileName, // 0 Name
                            "", // 1 Channels
                            "", // 2 BitDepth
                            "", // 3 SamplingRate
                            "", // 4 InputFileSize
                            "", // 5 OutputFileSize
                            "", // 6 Compression
                            "", // 7 Time
                            "", // 8 Speed
                            "", // 9 SpeedMin
                            "", // 10 SpeedMax
                            "", // 11 SpeedRange
                            "", // 12 SpeedConsistency
                            "", // 13 CPULoadEncoder
                            "", // 14 CPUClock
                            "", // 15 Passes
                            "", // 16 Parameters
                            "", // 17 Encoder
                            "", // 18 Version
                            "", // 19 EncoderDirectory
                            "", // 20 FastestEncoder
                            "", // 21 BestSize
                            "", // 22 SameSize
                            info.DirectoryPath, // 23 AudioFileDirectory
                            "MD5 calculation failed", // 24 MD5
                            "", // 25 Duplicates
                            info.ErrorDetails // 26 Errors
                            );
                            dataGridViewLogDetectDupes.Rows[rowIndex].DefaultCellStyle.ForeColor = Color.Gray;
                        }


                        // --- STAGE 2.5: LOG DUPLICATE GROUPS ---
                        foreach (var kvp in hashDict.Where(g => g.Value.Count > 1))
                        {
                            string duplicatesList = string.Join(", ", kvp.Value.Select(path =>
                        audioFileInfoCache[path].FileName
                    ));

                            foreach (string path in kvp.Value)
                            {
                                var info = audioFileInfoCache[path];

                                int rowIndex = dataGridViewLogDetectDupes.Rows.Add(
                                info.FileName, // 0 Name
                                "", // 1 Channels
                                "", // 2 BitDepth
                                "", // 3 SamplingRate
                                "", // 4 InputFileSize
                                "", // 5 OutputFileSize
                                "", // 6 Compression
                                "", // 7 Time
                                "", // 8 Speed
                                "", // 9 SpeedMin
                                "", // 10 SpeedMax
                                "", // 11 SpeedRange
                                "", // 12 SpeedConsistency
                                "", // 13 CPULoadEncoder
                                "", // 14 CPUClock
                                "", // 15 Passes
                                "", // 16 Parameters
                                "", // 17 Encoder
                                "", // 18 Version
                                "", // 19 EncoderDirectory
                                "", // 20 FastestEncoder
                                "", // 21 BestSize
                                "", // 22 SameSize
                                info.DirectoryPath, // 23 AudioFileDirectory
                                kvp.Key, // 24 MD5
                                duplicatesList, // 25 Duplicates
                                "" // 26 Errors
                                );
                                dataGridViewLogDetectDupes.Rows[rowIndex].DefaultCellStyle.ForeColor = Color.Brown;
                            }
                        }

                        // --- STAGE 2.6: MANAGE LOG TAB VISIBILITY ---
                        if (dataGridViewLogDetectDupes.Rows.Count > 0)
                        {
                            tabControlLog.SelectedTab = DetectDupes;
                        }

                        // --- STAGE 2.7: REORDER DUPLICATE GROUPS IN LISTVIEW ---
                        var allItems = listViewAudioFiles.Items.Cast<ListViewItem>().ToList();
                        var duplicateGroups = hashDict
                        .Where(kvp => kvp.Value.Count > 1)
                        .Select(kvp => new
                        {
                            Group = kvp.Value,
                            Primary = itemsToCheck.FirstOrDefault(p => kvp.Value.Contains(p)) ?? kvp.Value.First()
                        })
                        .OrderBy(g => g.Primary) // Order groups by primary file path
                        .ToList();

                        listViewAudioFiles.BeginUpdate();
                        try
                        {
                            // Remove ALL items first
                            listViewAudioFiles.Items.Clear();

                            // Add duplicate groups first (with primary item first in group)
                            foreach (var group in duplicateGroups)
                            {
                                var groupItems = allItems.Where(item => item.Tag is string tag && group.Group.Contains(tag)).ToList();
                                var primaryItem = groupItems.FirstOrDefault(item => item.Tag!.ToString() == group.Primary);
                                var otherItems = groupItems.Where(item => item != primaryItem).ToList();

                                if (primaryItem != null)
                                    listViewAudioFiles.Items.Add(primaryItem);
                                foreach (var item in otherItems)
                                    listViewAudioFiles.Items.Add(item);
                            }

                            // Add non-duplicate items - only those NOT in any duplicate group
                            var duplicatePaths = duplicateGroups.SelectMany(g => g.Group).ToHashSet();
                            var nonDuplicateItems = allItems.Where(item => item.Tag is string tag && !duplicatePaths.Contains(tag));

                            foreach (var item in nonDuplicateItems)
                                listViewAudioFiles.Items.Add(item);
                        }
                        finally
                        {
                            listViewAudioFiles.EndUpdate();
                        }
                    }); // End of UI update Invoke
                }); // End of background Task.Run
            }
            catch (OperationCanceledException)
            {
                // Operation was cancelled - exit silently
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (button != null && !button.IsDisposed)
                {
                    button.Invoke((MethodInvoker)(() =>
                    {
                        button.Text = originalText;
                        button.Enabled = true;
                    }));
                }

                // Show summary message to user
                if (!cts.IsCancellationRequested && hashDict != null && filesWithMD5Errors != null)
                {
                    Invoke((MethodInvoker)(() =>
                    {
                        int totalFiles = listViewAudioFiles.Items.Count;
                        int duplicateGroups = hashDict.Count(g => g.Value.Count > 1);
                        int duplicateFiles = hashDict.Where(g => g.Value.Count > 1).Sum(g => g.Value.Count);
                        int filesWithErrors = filesWithMD5Errors.Count;

                        string message = $"Duplicate detection completed.\n\n" +
                        $"Total files processed: {totalFiles}\n" +
                        $"Duplicate groups found: {duplicateGroups}\n" +
                        $"Duplicate files (total): {duplicateFiles}\n" +
                        $"Files with MD5 errors: {filesWithErrors}\n\n" +
                        $"Primary files are CHECKED.\n" +
                        $"Duplicate files are UNCHECKED.";

                        MessageBox.Show(message, "Duplicate Detection Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }));
                }

                cts.Dispose();
            }
        }
        private async void ButtonTestForErrors_Click(object? sender, EventArgs e)
        {
            var button = (Button)sender!;
            var originalText = button.Text;
            var cts = new CancellationTokenSource();

            try
            {
                button.Text = "In progress...";
                button.Enabled = false;

                // --- STAGE 1: COLLECT DATA FROM UI ---
                var (flacFilePaths, encoderPath, useWarningsAsErrors) = await Task.Run(() =>
                {
                    List<string> flacFilePaths = [];
                    string? encoderPath = null;
                    bool useWarningsAsErrors = false;

                    Invoke((MethodInvoker)delegate
                    {
                        // Clear previous results
                        for (int i = dataGridViewLogTestForErrors.Rows.Count - 1; i >= 0; i--)
                        {
                            if (dataGridViewLogTestForErrors.Rows[i].Cells["MD5"].Value?.ToString() == "Integrity Check Failed")
                                dataGridViewLogTestForErrors.Rows.RemoveAt(i);
                        }

                        // Remove missing files
                        var itemsToRemove = listViewAudioFiles.Items.Cast<ListViewItem>()
                            .Where(item => item.Tag != null && !File.Exists(item.Tag.ToString())).ToList();

                        foreach (var item in itemsToRemove)
                            listViewAudioFiles.Items.Remove(item);

                        if (itemsToRemove.Count > 0)
                        {
                            UpdateGroupBoxAudioFilesHeader();
                            ShowTemporaryAudioFileRemovedMessage($"{itemsToRemove.Count} file(s) removed");
                        }

                        // Collect FLAC files and settings using cache
                        flacFilePaths.AddRange(listViewAudioFiles.Items.Cast<ListViewItem>()
                            .Where(item => item.Tag != null)
                            .Select(item => item.Tag!.ToString()!)
                            .Where(filePath => audioFileInfoCache[filePath].Extension == ".flac")
                            );

                        encoderPath = listViewEncoders.Items
                            .Cast<ListViewItem>()
                            .FirstOrDefault(item =>
                            {
                                if (item.Tag == null) return false;
                                string path = item.Tag.ToString()!;

                                return encoderInfoCache[path].Extension == ".exe";
                            })
                            ?.Tag!.ToString();

                        useWarningsAsErrors = checkBoxWarningsAsErrors.Checked;
                    });

                    return (flacFilePaths, encoderPath, useWarningsAsErrors);
                });

                // Validation
                if (flacFilePaths.Count == 0)
                {
                    MessageBox.Show("No FLAC files found in the list.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                if (string.IsNullOrEmpty(encoderPath) || !File.Exists(encoderPath))
                {
                    MessageBox.Show("No encoders found in the list.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // --- STAGE 2: PARALLEL INTEGRITY TEST ---
                var errorResults = new ConcurrentBag<(string FileName, string FilePath, string Message)>();
                var semaphore = new SemaphoreSlim(Environment.ProcessorCount);

                await Task.WhenAll(flacFilePaths.Select(async filePath =>
                {
                    if (cts.Token.IsCancellationRequested) return;

                    await semaphore.WaitAsync(cts.Token);
                    try
                    {
                        string fileName = audioFileInfoCache[filePath].FileName;

                        using var process = new Process();
                        process.StartInfo = new ProcessStartInfo
                        {
                            FileName = encoderPath,
                            Arguments = $" --test --silent{(useWarningsAsErrors ? " --warnings-as-errors" : "")} \"{filePath}\"",
                            UseShellExecute = false,
                            RedirectStandardError = true,
                            RedirectStandardOutput = true,
                            CreateNoWindow = true,
                            StandardErrorEncoding = Encoding.UTF8,
                            StandardOutputEncoding = Encoding.UTF8
                        };

                        process.Start();

                        var errorTask = process.StandardError.ReadToEndAsync();
                        var outputTask = process.StandardOutput.ReadToEndAsync();

                        await process.WaitForExitAsync(cts.Token);

                        string errorOutput = await errorTask;
                        string output = await outputTask; // Not used: flac --test --silent never writes to stdout (only stderr), per official docs and source code.
                        if (process.ExitCode != 0)
                        {
                            string message = errorOutput;

                            // Check for illegal instruction (e.g. AVX-512 on older CPU)
                            if (process.ExitCode == unchecked((int)0xC000001D))
                            {
                                message = "Process failed: Illegal instruction (e.g. AVX-512 not supported on this CPU).";
                            }
                            else if (string.IsNullOrWhiteSpace(errorOutput))
                            {
                                message = "Unknown error (non-zero exit code).";
                            }
                            else
                            {
                                message = errorOutput.Trim();
                            }

                            errorResults.Add((fileName, filePath, message));
                        }
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        string fileName = audioFileInfoCache[filePath].FileName;
                        errorResults.Add((fileName, filePath, $"Process failed: {ex.Message}"));
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }));

                if (cts.Token.IsCancellationRequested) return;

                // --- STAGE 3: UPDATE UI ---
                await InvokeAsync(() =>
                {
                    dataGridViewLogTestForErrors.SuspendLayout();
                    try
                    {
                        var sortedResults = errorResults.OrderBy(r => r.FilePath).ToList();

                        var rowsToAdd = sortedResults.Select(result =>
                        {
                            string directoryPath = audioFileInfoCache[result.FilePath].DirectoryPath;

                            var row = new DataGridViewRow();
                            row.CreateCells(dataGridViewLogTestForErrors);
                            row.SetValues(
                            result.FileName, // 0 Name
                            "", // 1 Channels
                            "", // 2 BitDepth
                            "", // 3 SamplingRate
                            "", // 4 InputFileSize
                            "", // 5 OutputFileSize
                            "", // 6 Compression
                            "", // 7 Time
                            "", // 8 Speed
                            "", // 9 SpeedMin
                            "", // 10 SpeedMax
                            "", // 11 SpeedRange
                            "", // 12 SpeedConsistency
                            "", // 13 CPULoadEncoder
                            "", // 14 CPUClock
                            "", // 15 Passes
                            "", // 16 Parameters
                            "", // 17 Encoder
                            "", // 18 Version
                            "", // 19 EncoderDirectory
                            "", // 20 FastestEncoder
                            "", // 21 BestSize
                            "", // 22 SameSize
                            directoryPath, // 23 AudioFileDirectory
                            "Integrity Check Failed", // 24 MD5
                            "", // 25 Duplicates
                            result.Message // 26 Errors
                            );
                            row.DefaultCellStyle.ForeColor = Color.Red;
                            return row;
                        }).ToList();

                        if (rowsToAdd.Count > 0)
                            dataGridViewLogTestForErrors.Rows.AddRange(rowsToAdd.ToArray());

                        if (dataGridViewLogTestForErrors.Rows.Count > 0)
                        {
                            tabControlLog.SelectedTab = TestForErrors;
                        }
                    }
                    finally
                    {
                        dataGridViewLogTestForErrors.ResumeLayout();
                    }

                    MessageBox.Show(
                    errorResults.IsEmpty
                    ? "All FLAC files passed the integrity test."
                    : $"{errorResults.Count} FLAC file(s) failed the integrity test.",
                    "Test Complete",
                    MessageBoxButtons.OK,
                    errorResults.IsEmpty ? MessageBoxIcon.Information : MessageBoxIcon.Warning
                    );
                });
            }
            catch (OperationCanceledException)
            {
                // Ignore cancellation
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred during the integrity test: {ex.Message}", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (!button.IsDisposed)
                {
                    button.Text = originalText;
                    button.Enabled = true;
                }
                cts.Dispose();
            }
        }

        private void ButtonStop_Click(object? sender, EventArgs e)
        {
            _isEncodingStopped = true; // Flag to request encoding stop
            _isPaused = false; // Reset pause flag
            _pauseEvent.Set(); // Unblock execution

            if (_process != null)
            {
                try
                {
                    // Check if process is running
                    if (!_process.HasExited)
                    {
                        _process.Kill(); // Terminate the process
                        ShowTemporaryStoppedMessage("Stopped");
                    }
                    else
                    {
                        ShowTemporaryStoppedMessage("Stopped");
                    }
                }
                catch (Exception)
                {
                    ShowTemporaryStoppedMessage("Stopped");
                }
                finally
                {
                    progressBarEncoder.Value = 0;
                    progressBarDecoder.Value = 0;
                    progressBarEncoder.ManualText = "";
                    progressBarDecoder.ManualText = "";
                }
            }
        }
        private void ButtonPauseResume_Click(object? sender, EventArgs e)
        {
            _isPaused = !_isPaused; // Toggle pause flag

            if (_isPaused)
            {
                buttonPauseResume.Text = "Resume";
                _pauseEvent.Reset(); // Block execution
            }
            else
            {
                buttonPauseResume.Text = "Pause";
                _pauseEvent.Set(); // Unblock execution
            }
        }

        // Encoder and Decoder options
        private void Button5CompressionLevel_Click(object? sender, EventArgs e)
        {
            textBoxCompressionLevel.Text = "5";
        }
        private void ButtonMaxCompressionLevel_Click(object? sender, EventArgs e)
        {
            textBoxCompressionLevel.Text = "8";
        }
        private void ButtonHalfCores_Click(object? sender, EventArgs e)
        {
            textBoxThreads.Text = (physicalCores / 2).ToString(); // Set half of the cores
        }
        private void ButtonSetMaxCores_Click(object? sender, EventArgs e)
        {
            textBoxThreads.Text = physicalCores.ToString(); // Set maximum number of cores
        }
        private void ButtonSetHalfThreads_Click(object? sender, EventArgs e)
        {
            textBoxThreads.Text = (threadCount / 2).ToString(); // Set half of the threads
        }
        private void ButtonSetMaxThreads_Click(object? sender, EventArgs e)
        {
            textBoxThreads.Text = threadCount.ToString(); // Set maximum number of threads
        }
        private void ButtonClearCommandLineEncoder_Click(object? sender, EventArgs e)
        {
            textBoxCommandLineOptionsEncoder.Clear(); // Clear textCommandLineOptions
        }
        private void ButtonClearCommandLineDecoder_Click(object? sender, EventArgs e)
        {
            textBoxCommandLineOptionsDecoder.Clear(); // Clear textCommandLineOptions
        }
        private void ButtonEpr8_Click(object? sender, EventArgs e)
        {
            // Check if -epr8 is already in textBoxAdditionalArguments
            if (!textBoxCommandLineOptionsEncoder.Text.Contains("-epr8"))
            {
                // If not, add it
                textBoxCommandLineOptionsEncoder.AppendText(" -epr8"); // Add with space before text
            }
        }
        private void ButtonAsubdividetukey5flattop_Click(object? sender, EventArgs e)
        {
            // Check if -A "subdivide_tukey(5);flattop" is already in textBoxAdditionalArguments
            if (!textBoxCommandLineOptionsEncoder.Text.Contains("-A \"subdivide_tukey(5);flattop\""))
            {
                // If not, add it
                textBoxCommandLineOptionsEncoder.AppendText(" -A \"subdivide_tukey(5);flattop\""); // Add with space before text
            }
        }
        private void ButtonNoPadding_Click(object? sender, EventArgs e)
        {
            // Check if --no-padding is already in textBoxAdditionalArguments
            if (!textBoxCommandLineOptionsEncoder.Text.Contains("--no-padding"))
            {
                // If not, add it
                textBoxCommandLineOptionsEncoder.AppendText(" --no-padding"); // Add with space before text
            }
        }
        private void ButtonNoSeektable_Click(object? sender, EventArgs e)
        {
            // Check if --no-seektable is already in textBoxAdditionalArguments
            if (!textBoxCommandLineOptionsEncoder.Text.Contains("--no-seektable"))
            {
                // If not, add it
                textBoxCommandLineOptionsEncoder.AppendText(" --no-seektable"); // Add with space before text
            }
        }

        private void TextBoxCompressionLevel_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;

                ButtonAddJobToJobListEncoder_Click(sender, e);
            }
        }
        private void TextBoxThreads_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;

                ButtonAddJobToJobListEncoder_Click(sender, e);
            }
        }
        private void TextBoxCommandLineOptionsEncoder_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;

                ButtonAddJobToJobListEncoder_Click(sender, e);
            }
        }
        private void TextBoxCommandLineOptionsDecoder_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;

                ButtonAddJobToJobListDecoder_Click(sender, e);
            }
        }

        // General methods
        private static void MoveSelectedItemsForListview(ListView listView, int direction)
        {
            // Get selected items and sort them by indices
            var selectedItems = listView.SelectedItems.Cast<ListViewItem>()
            .OrderBy(item => item.Index)
            .ToList();
            // If no items are selected, exit the method
            if (selectedItems.Count == 0)
                return;
            // If moving down, we'll take items in reverse order
            if (direction > 0)
            {
                selectedItems.Reverse(); // Reverse the list for moving down
            }
            // Suspend ListView updates to reduce flickering
            listView.BeginUpdate();
            try
            {
                // Move items
                foreach (var item in selectedItems)
                {
                    int currentIndex = item.Index;
                    int newIndex = currentIndex + direction;
                    // Check boundaries
                    if (newIndex < 0 || newIndex >= listView.Items.Count)
                        return; // If out of bounds, exit the method

                    // Remove item from current position
                    listView.Items.Remove(item);
                    // Insert item at new position
                    listView.Items.Insert(newIndex, item);
                }
                // Update selection
                UpdateSelection(selectedItems, listView);
            }
            finally
            {
                // Resume ListView updates
                listView.EndUpdate();
            }
        }
        private static void MoveListViewItemToRecycleBin(ListView listView, Func<ListViewItem, bool> predicate, string itemType, Action updateHeaderCallback)
        {
            var filesToRemove = new List<string>();

            foreach (ListViewItem item in listView.Items)
            {
                if (predicate(item))
                {
                    filesToRemove.Add(item.Tag!.ToString()!);
                }
            }

            if (filesToRemove.Count == 0)
            {
                MessageBox.Show($"There are no {itemType} to delete.", "Information",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var result = MessageBox.Show(
                $"Are you sure you want to move all {itemType} to the Recycle Bin?",
                "Confirmation",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                foreach (var file in filesToRemove)
                {
                    Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(file,
                        Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs,
                        Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin);
                }

                foreach (var file in filesToRemove)
                {
                    var itemToRemove = listView.Items.Cast<ListViewItem>()
                        .FirstOrDefault(i => i.Tag?.ToString() == file);
                    if (itemToRemove != null)
                    {
                        listView.Items.Remove(itemToRemove);
                    }
                }

                updateHeaderCallback();

                MessageBox.Show($"{itemType} have been moved to the Recycle Bin.", "Deletion",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        private static void UpdateSelection(List<ListViewItem> selectedItems, ListView listView)
        {
            // Clear selection from all items
            foreach (ListViewItem item in listView.Items)
            {
                item.Selected = false;
            }
            // Select moved items
            foreach (var item in selectedItems)
            {
                item.Selected = true; // Set selection on moved items
            }
            listView.Focus(); // Set focus on the list
        }
        private static void MoveSelectedItemsForDataGridViewEx(DataGridViewEx dataGridView, int direction)
        {
            // Validate inputs and check for selection
            if (dataGridView.Rows.Count == 0 || dataGridView.SelectedRows.Count == 0)
            {
                return; // Nothing to move
            }

            // Get the indices of all selected rows and sort them in ascending order
            var selectedIndices = dataGridView.SelectedRows.Cast<DataGridViewRow>()
            .Select(row => row.Index)
            .OrderBy(index => index)
            .ToList();

            // Check boundaries: Ensure no selected row is at the edge where it cannot move further
            // For moving UP, no selected row should be at index 0
            // For moving DOWN, no selected row should be at the last index (Count - 1)
            if ((direction == -1 && selectedIndices.Contains(0)) || (direction == 1 && selectedIndices.Contains(dataGridView.Rows.Count - 1)))
            {
                return; // Out of bounds for at least one selected row
            }

            // Sort indices based on direction to handle index shifting correctly during MoveRow
            // Moving UP: Process rows from top to bottom (lowest index first) to prevent index shifts affecting subsequent moves.
            // Moving DOWN: Process rows from bottom to top (highest index first) for the same reason.
            List<int> sortedIndices = (direction == -1)
            ? selectedIndices.OrderBy(i => i).ToList()      // 0, 1, 2, ...
            : selectedIndices.OrderByDescending(i => i).ToList(); // ..., 2, 1, 0

            // Perform the move operation for each selected row in the calculated order
            foreach (int originalIndex in sortedIndices)
            {
                int targetIndex = originalIndex + direction; // Calculate the target index (original +/- 1)

                // Move the row using the custom MoveRow method from DataGridViewEx
                // It handles internal validation and index adjustments during the move.
                dataGridView.MoveRow(originalIndex, targetIndex);
            }

            // Calculate the new indices for the moved rows
            var newSelectedIndices = selectedIndices.Select(originalIdx => originalIdx + direction).OrderBy(i => i).ToList();

            // Restore selection and focus on the rows at their new positions
            dataGridView.ClearSelection();
            dataGridView.CurrentCell = null; // Clear the current cell to avoid potential issues

            foreach (int newIdx in newSelectedIndices)
            {
                // Double-check bounds as indices might have shifted unexpectedly in complex scenarios
                if (newIdx >= 0 && newIdx < dataGridView.Rows.Count)
                {
                    dataGridView.Rows[newIdx].Selected = true;

                    // Set the current cell to the first row of the newly selected block for better UX
                    if (newIdx == newSelectedIndices.First())
                    {
                        dataGridView.CurrentCell = dataGridView.Rows[newIdx].Cells[0]; // Focus the first cell of the first moved row
                    }
                }
            }
        }
        private static CheckState GetCheckStateForContextMenuItem(int count, int total)
        {
            if (total == 0) return CheckState.Unchecked;
            if (count == 0) return CheckState.Unchecked;
            if (count == total) return CheckState.Checked;
            return CheckState.Indeterminate;
        }
        private void ContextMenu_Closing(object? sender, ToolStripDropDownClosingEventArgs e)
        {
            e.Cancel = e.CloseReason == ToolStripDropDownCloseReason.ItemClicked &&
                       ((ToolStripDropDown)sender!).Items
                           .OfType<ToolStripMenuItem>()
                           .Where(item => item.Tag?.ToString() == "KeepOpened")
                           .Any(item => item.Selected || item.Pressed);
        }

        // Temporary labels
        private void ShowTemporaryStoppedMessage(string message)
        {
            labelStopped.Text = message;
            labelStopped.Visible = true;

            temporaryMessageTimer.Stop();
            temporaryMessageTimer.Interval = 3000;
            temporaryMessageTimer.Start();
        }
        private void ShowTemporaryAudioFileRemovedMessage(string message)
        {
            labelAudioFileRemoved.Text = message;
            labelAudioFileRemoved.Visible = true;

            temporaryMessageTimer.Stop();
            temporaryMessageTimer.Interval = 8000;
            temporaryMessageTimer.Start();
        }

        private void ButtonSelectTempFolder_Click(object? sender, EventArgs e)
        {
            using FolderBrowserDialog folderBrowserDialog = new();
            folderBrowserDialog.Description = "Select temp folder";
            // If path is saved in settings, set it
            if (Directory.Exists(tempFolderPath))
            {
                folderBrowserDialog.SelectedPath = tempFolderPath;
            }
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                // Get selected path
                tempFolderPath = folderBrowserDialog.SelectedPath;
                // Save path in settings
                SaveSettings(); // This will also need to be changed to save the path
            }
        }
        private static void DeleteFileIfExists(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    // Remove "read-only" attribute if set
                    File.SetAttributes(filePath, FileAttributes.Normal);
                    File.Delete(filePath);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unable to delete {filePath}: {ex.Message}",
                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Plots settings
        private readonly List<ScottPlot.Plottable.ScatterPlot> allIndividualSeries = [];
        private readonly List<ScottPlot.Plottable.ScatterPlot> allAggregatedSeries = [];
        private ScottPlot.Plottable.ScatterPlot? idealCPULoadLineSingle;
        private ScottPlot.Plottable.ScatterPlot? idealCPULoadLineMultiplot;

        private readonly List<(ScottPlot.Plottable.ScatterPlot Series, string Label)> allScatterSeriesSpeedByThreads = [];
        private ScottPlot.Plottable.Tooltip? dynamicTooltipSpeedByThreads;
        private ScottPlot.Plottable.Tooltip? dynamicTooltipMultiplotSpeedByThreads;

        private readonly List<(ScottPlot.Plottable.ScatterPlot Series, string Label)> allScatterSeriesCPULoadByThreads = [];
        private ScottPlot.Plottable.Tooltip? dynamicTooltipCPULoadByThreads;
        private ScottPlot.Plottable.Tooltip? dynamicTooltipMultiplotCPULoadByThreads;

        private readonly List<(ScottPlot.Plottable.ScatterPlot Series, string Label)> allScatterSeriesCPUClockByThreads = [];
        private ScottPlot.Plottable.Tooltip? dynamicTooltipCPUClockByThreads;
        private ScottPlot.Plottable.Tooltip? dynamicTooltipMultiplotCPUClockByThreads;

        private List<string> allParamsSpeedByParameters = [];
        private readonly List<(ScottPlot.Plottable.ScatterPlot Series, string Label)> allScatterSeriesSpeedByParameters = [];
        private ScottPlot.Plottable.Tooltip? dynamicTooltipSpeedByParameters;
        private ScottPlot.Plottable.Tooltip? dynamicTooltipMultiplotSpeedByParameters;

        private List<string> allParamsCompressionByParameters = [];
        private readonly List<(ScottPlot.Plottable.ScatterPlot Series, string Label)> allScatterSeriesCompressionByParameters = [];
        private ScottPlot.Plottable.Tooltip? dynamicTooltipCompressionByParameters;
        private ScottPlot.Plottable.Tooltip? dynamicTooltipMultiplotCompressionByParameters;

        private void CheckBoxShowIndividualFilesPlots_CheckedChanged(object? sender, EventArgs e)
        {
            UpdateSeriesVisibility();
        }
        private void CheckBoxShowAggregatedByEncoderPlots_CheckedChanged(object? sender, EventArgs e)
        {
            UpdateSeriesVisibility();
        }
        private void CheckBoxShowIdealCPULoadLine_CheckedChanged(object? sender, EventArgs e)
        {
            UpdateSeriesVisibility();
        }
        private void CheckBoxShowTooltipsOnPlots_CheckedChanged(object? sender, EventArgs e)
        {
            if (checkBoxShowTooltipsOnPlots.Checked)
            {
                plotScalingPlotSpeedByThreads.MouseMove += PlotScalingPlotSpeedByThreads_MouseMove;
                plotScalingPlotSpeedByThreads.MouseLeave += PlotScalingPlotSpeedByThreads_MouseLeave;
                plotScalingMultiPlotSpeedByThreads.MouseMove += PlotScalingMultiPlotSpeedByThreads_MouseMove;
                plotScalingMultiPlotSpeedByThreads.MouseLeave += PlotScalingMultiPlotSpeedByThreads_MouseLeave;
                plotScalingPlotCPULoadByThreads.MouseMove += PlotScalingPlotCPULoadByThreads_MouseMove;
                plotScalingPlotCPULoadByThreads.MouseLeave += PlotScalingPlotCPULoadByThreads_MouseLeave;
                plotScalingMultiPlotCPULoadByThreads.MouseMove += PlotScalingMultiPlotCPULoadByThreads_MouseMove;
                plotScalingMultiPlotCPULoadByThreads.MouseLeave += PlotScalingMultiPlotCPULoadByThreads_MouseLeave;
                plotScalingPlotCPUClockByThreads.MouseMove += PlotScalingPlotCPUClockByThreads_MouseMove;
                plotScalingPlotCPUClockByThreads.MouseLeave += PlotScalingPlotCPUClockByThreads_MouseLeave;
                plotScalingMultiPlotCPUClockByThreads.MouseMove += PlotScalingMultiPlotCPUClockByThreads_MouseMove;
                plotScalingMultiPlotCPUClockByThreads.MouseLeave += PlotScalingMultiPlotCPUClockByThreads_MouseLeave;
                plotScalingPlotSpeedByParameters.MouseMove += PlotScalingPlotSpeedByParameters_MouseMove;
                plotScalingPlotSpeedByParameters.MouseLeave += PlotScalingPlotSpeedByParameters_MouseLeave;
                plotScalingMultiPlotSpeedByParameters.MouseMove += PlotScalingMultiPlotSpeedByParameters_MouseMove;
                plotScalingMultiPlotSpeedByParameters.MouseLeave += PlotScalingMultiPlotSpeedByParameters_MouseLeave;
                plotScalingPlotCompressionByParameters.MouseMove += PlotScalingPlotCompressionByParameters_MouseMove;
                plotScalingPlotCompressionByParameters.MouseLeave += PlotScalingPlotCompressionByParameters_MouseLeave;
                plotScalingMultiPlotCompressionByParameters.MouseMove += PlotScalingMultiPlotCompressionByParameters_MouseMove;
                plotScalingMultiPlotCompressionByParameters.MouseLeave += PlotScalingMultiPlotCompressionByParameters_MouseLeave;
            }
            else
            {
                plotScalingPlotSpeedByThreads.MouseMove -= PlotScalingPlotSpeedByThreads_MouseMove;
                plotScalingPlotSpeedByThreads.MouseLeave -= PlotScalingPlotSpeedByThreads_MouseLeave;
                plotScalingMultiPlotSpeedByThreads.MouseMove -= PlotScalingMultiPlotSpeedByThreads_MouseMove;
                plotScalingMultiPlotSpeedByThreads.MouseLeave -= PlotScalingMultiPlotSpeedByThreads_MouseLeave;
                plotScalingPlotCPULoadByThreads.MouseMove -= PlotScalingPlotCPULoadByThreads_MouseMove;
                plotScalingPlotCPULoadByThreads.MouseLeave -= PlotScalingPlotCPULoadByThreads_MouseLeave;
                plotScalingMultiPlotCPULoadByThreads.MouseMove -= PlotScalingMultiPlotCPULoadByThreads_MouseMove;
                plotScalingMultiPlotCPULoadByThreads.MouseLeave -= PlotScalingMultiPlotCPULoadByThreads_MouseLeave;
                plotScalingPlotCPUClockByThreads.MouseMove -= PlotScalingPlotCPUClockByThreads_MouseMove;
                plotScalingPlotCPUClockByThreads.MouseLeave -= PlotScalingPlotCPUClockByThreads_MouseLeave;
                plotScalingMultiPlotCPUClockByThreads.MouseMove -= PlotScalingMultiPlotCPUClockByThreads_MouseMove;
                plotScalingMultiPlotCPUClockByThreads.MouseLeave -= PlotScalingMultiPlotCPUClockByThreads_MouseLeave;
                plotScalingPlotSpeedByParameters.MouseMove -= PlotScalingPlotSpeedByParameters_MouseMove;
                plotScalingPlotSpeedByParameters.MouseLeave -= PlotScalingPlotSpeedByParameters_MouseLeave;
                plotScalingMultiPlotSpeedByParameters.MouseMove -= PlotScalingMultiPlotSpeedByParameters_MouseMove;
                plotScalingMultiPlotSpeedByParameters.MouseLeave -= PlotScalingMultiPlotSpeedByParameters_MouseLeave;
                plotScalingPlotCompressionByParameters.MouseMove -= PlotScalingPlotCompressionByParameters_MouseMove;
                plotScalingPlotCompressionByParameters.MouseLeave -= PlotScalingPlotCompressionByParameters_MouseLeave;
                plotScalingMultiPlotCompressionByParameters.MouseMove -= PlotScalingMultiPlotCompressionByParameters_MouseMove;
                plotScalingMultiPlotCompressionByParameters.MouseLeave -= PlotScalingMultiPlotCompressionByParameters_MouseLeave;

                var plotsToRefresh = new List<ScottPlot.FormsPlot>();

                if (dynamicTooltipSpeedByThreads != null) { plotScalingPlotSpeedByThreads.Plot.Remove(dynamicTooltipSpeedByThreads); dynamicTooltipSpeedByThreads = null; plotsToRefresh.Add(plotScalingPlotSpeedByThreads); }
                if (dynamicTooltipMultiplotSpeedByThreads != null) { plotScalingMultiPlotSpeedByThreads.Plot.Remove(dynamicTooltipMultiplotSpeedByThreads); dynamicTooltipMultiplotSpeedByThreads = null; plotsToRefresh.Add(plotScalingMultiPlotSpeedByThreads); }

                if (dynamicTooltipCPULoadByThreads != null) { plotScalingPlotCPULoadByThreads.Plot.Remove(dynamicTooltipCPULoadByThreads); dynamicTooltipCPULoadByThreads = null; plotsToRefresh.Add(plotScalingPlotCPULoadByThreads); }
                if (dynamicTooltipMultiplotCPULoadByThreads != null) { plotScalingMultiPlotCPULoadByThreads.Plot.Remove(dynamicTooltipMultiplotCPULoadByThreads); dynamicTooltipMultiplotCPULoadByThreads = null; plotsToRefresh.Add(plotScalingMultiPlotCPULoadByThreads); }

                if (dynamicTooltipCPUClockByThreads != null) { plotScalingPlotCPUClockByThreads.Plot.Remove(dynamicTooltipCPUClockByThreads); dynamicTooltipCPUClockByThreads = null; plotsToRefresh.Add(plotScalingPlotCPUClockByThreads); }
                if (dynamicTooltipMultiplotCPUClockByThreads != null) { plotScalingMultiPlotCPUClockByThreads.Plot.Remove(dynamicTooltipMultiplotCPUClockByThreads); dynamicTooltipMultiplotCPUClockByThreads = null; plotsToRefresh.Add(plotScalingMultiPlotCPUClockByThreads); }

                if (dynamicTooltipSpeedByParameters != null) { plotScalingPlotSpeedByParameters.Plot.Remove(dynamicTooltipSpeedByParameters); dynamicTooltipSpeedByParameters = null; plotsToRefresh.Add(plotScalingPlotSpeedByParameters); }
                if (dynamicTooltipMultiplotSpeedByParameters != null) { plotScalingMultiPlotSpeedByParameters.Plot.Remove(dynamicTooltipMultiplotSpeedByParameters); dynamicTooltipMultiplotSpeedByParameters = null; plotsToRefresh.Add(plotScalingMultiPlotSpeedByParameters); }

                if (dynamicTooltipCompressionByParameters != null) { plotScalingPlotCompressionByParameters.Plot.Remove(dynamicTooltipCompressionByParameters); dynamicTooltipCompressionByParameters = null; plotsToRefresh.Add(plotScalingPlotCompressionByParameters); }
                if (dynamicTooltipMultiplotCompressionByParameters != null) { plotScalingMultiPlotCompressionByParameters.Plot.Remove(dynamicTooltipMultiplotCompressionByParameters); dynamicTooltipMultiplotCompressionByParameters = null; plotsToRefresh.Add(plotScalingMultiPlotCompressionByParameters); }

                foreach (var plot in plotsToRefresh) plot.Refresh();
            }
        }
        private void UpdateSeriesVisibility()
        {
            foreach (var series in allIndividualSeries)
                series.IsVisible = checkBoxShowIndividualFilesPlots.Checked;
            foreach (var series in allAggregatedSeries)
                series.IsVisible = checkBoxShowAggregatedByEncoderPlots.Checked;
            if (idealCPULoadLineSingle != null)
                idealCPULoadLineSingle.IsVisible = checkBoxShowIdealCPULoadLine.Checked;
            if (idealCPULoadLineMultiplot != null)
                idealCPULoadLineMultiplot.IsVisible = checkBoxShowIdealCPULoadLine.Checked;

            plotScalingPlotSpeedByThreads.Refresh();
            plotScalingPlotCPULoadByThreads.Refresh();
            plotScalingPlotCPUClockByThreads.Refresh();
            plotScalingPlotSpeedByParameters.Refresh();
            plotScalingPlotCompressionByParameters.Refresh();
            plotScalingMultiPlotSpeedByThreads.Refresh();
            plotScalingMultiPlotCPULoadByThreads.Refresh();
            plotScalingMultiPlotCPUClockByThreads.Refresh();
            plotScalingMultiPlotSpeedByParameters.Refresh();
            plotScalingMultiPlotCompressionByParameters.Refresh();
        }
        private void CheckBoxDrawMultiplots_CheckedChanged(object? sender, EventArgs e)
        {
            TabPage? previouslyActiveTab = tabControlScalingPlots.SelectedTab;

            if (checkBoxDrawMultiplots.Checked)
            {

                if (!tabControlScalingPlots.TabPages.Contains(tabPageMultiplotByThreads))
                    tabControlScalingPlots.TabPages.Add(tabPageMultiplotByThreads);

                if (!tabControlScalingPlots.TabPages.Contains(tabPageMultiplotByParameters))
                    tabControlScalingPlots.TabPages.Add(tabPageMultiplotByParameters);

                if (previouslyActiveTab == tabPageSpeedByThreads ||
                    previouslyActiveTab == tabPageCPULoadByThreads ||
                    previouslyActiveTab == tabPageCPUClockByThreads)
                {
                    tabControlScalingPlots.SelectedTab = tabPageMultiplotByThreads;
                }
                else if (previouslyActiveTab == tabPageSpeedByParameters ||
                         previouslyActiveTab == tabPageCompressionByParameters)
                {
                    tabControlScalingPlots.SelectedTab = tabPageMultiplotByParameters;
                }
                else
                {
                    tabControlScalingPlots.SelectedTab = tabPageMultiplotByThreads;
                }

                tabControlScalingPlots.TabPages.Remove(tabPageSpeedByThreads);
                tabControlScalingPlots.TabPages.Remove(tabPageCPULoadByThreads);
                tabControlScalingPlots.TabPages.Remove(tabPageCPUClockByThreads);
                tabControlScalingPlots.TabPages.Remove(tabPageSpeedByParameters);
                tabControlScalingPlots.TabPages.Remove(tabPageCompressionByParameters);
            }
            else
            {
                if (!tabControlScalingPlots.TabPages.Contains(tabPageSpeedByThreads))
                    tabControlScalingPlots.TabPages.Add(tabPageSpeedByThreads);

                if (!tabControlScalingPlots.TabPages.Contains(tabPageCPULoadByThreads))
                    tabControlScalingPlots.TabPages.Add(tabPageCPULoadByThreads);

                if (!tabControlScalingPlots.TabPages.Contains(tabPageCPUClockByThreads))
                    tabControlScalingPlots.TabPages.Add(tabPageCPUClockByThreads);

                if (!tabControlScalingPlots.TabPages.Contains(tabPageSpeedByParameters))
                    tabControlScalingPlots.TabPages.Add(tabPageSpeedByParameters);

                if (!tabControlScalingPlots.TabPages.Contains(tabPageCompressionByParameters))
                    tabControlScalingPlots.TabPages.Add(tabPageCompressionByParameters);

                if (previouslyActiveTab == tabPageMultiplotByThreads)
                {
                    tabControlScalingPlots.SelectedTab = tabPageSpeedByThreads;
                }
                else if (previouslyActiveTab == tabPageMultiplotByParameters)
                {
                    tabControlScalingPlots.SelectedTab = tabPageSpeedByParameters;
                }
                else
                {
                    tabControlScalingPlots.SelectedTab = tabPageSpeedByThreads;
                }

                tabControlScalingPlots.TabPages.Remove(tabPageMultiplotByThreads);
                tabControlScalingPlots.TabPages.Remove(tabPageMultiplotByParameters);
            }

            tabControlScalingPlots.SelectedTab?.Refresh();
        }
        private void CheckBoxWrapLongPlotLabels_CheckedChanged(object? sender, EventArgs e)
        {
            textBoxWrapLongPlotLabels.Enabled = checkBoxWrapLongPlotLabels.Checked;
        }

        private void CheckBoxAutoAnalyzeLog_CheckedChanged(object? sender, EventArgs e)
        {

        }
        private void CheckBoxWarningsAsErrors_CheckedChanged(object? sender, EventArgs e)
        {
            warningsAsErrorsToolStripMenuItemAudioFiles.Checked = checkBoxWarningsAsErrors.Checked;
        }
        private void CheckBoxPreventSleep_CheckedChanged(object? sender, EventArgs e)
        {
            if (checkBoxPreventSleep.Checked)
            {
                _ = SetThreadExecutionState(ES_CONTINUOUS | ES_SYSTEM_REQUIRED);
            }
            else
            {
                _ = SetThreadExecutionState(ES_CONTINUOUS);
            }
        }
        private void CheckBoxCheckForUpdatesOnStartup_CheckedChanged(object? sender, EventArgs e)
        {
            if (checkBoxCheckForUpdatesOnStartup.Checked)
            {
                _ = CheckForUpdatesAsync();
            }
            else
            {
                programVersionIgnored = null;
                SaveSettings();
            }
        }
        private async Task CheckForUpdatesAsync()
        {
            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.UserAgent.ParseAdd("FLAC-Benchmark-H-App");

                string programVersionOnlineUrl = "https://raw.githubusercontent.com/hat3k/FLAC-Benchmark-H/master/Version.txt";
                string programVersionOnline = (await client.GetStringAsync(programVersionOnlineUrl).ConfigureAwait(false)).Trim();

                System.Version programVersionCurrentParsed = ParseVersion(programVersionCurrent);
                System.Version programVersionOnlineParsed = ParseVersion(programVersionOnline);

                if (programVersionOnlineParsed != null && programVersionCurrentParsed != null && programVersionOnlineParsed > programVersionCurrentParsed)
                {
                    if (programVersionIgnored == programVersionOnline)
                        return;

                    Invoke((MethodInvoker)delegate
                    {
                        ShowDialogUpdateAvailable(programVersionOnline);
                    });
                }
            }
            catch (Exception ex)
            {
                void ShowDialogUpdateError()
                {
                    MessageBox.Show(this,
                        ex.Message,
                        "Error checking for updates",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }

                if (InvokeRequired)
                    Invoke((MethodInvoker)ShowDialogUpdateError);
                else
                    ShowDialogUpdateError();
            }
        }
        private void ShowDialogUpdateAvailable(string programVersionOnline)
        {
            var result = MessageBox.Show(
            this,
            $"A new version is available!\n\nCurrent version:\t{programVersionCurrent}\nLatest version:\t{programVersionOnline}\n\nClick 'Cancel' to ignore this update.\nDo you want to open the releases page?",
            "Update Available",
            MessageBoxButtons.YesNoCancel,
            MessageBoxIcon.Information);

            if (result == DialogResult.Yes)
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "https://github.com/hat3k/FLAC-Benchmark-H/releases",
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this,
                        $"Failed to open the releases page:\n{ex.Message}",
                        "Browser Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
            }
            else if (result == DialogResult.Cancel)
            {
                programVersionIgnored = programVersionOnline;
                SaveSettings();
            }
        }
        private static System.Version ParseVersion(string versionString)
        {
            if (string.IsNullOrWhiteSpace(versionString))
                return new System.Version("0.0.0");

            var versionStringParts = versionString.Split([' '], StringSplitOptions.RemoveEmptyEntries);

            // Expect format "1.7.6 build 20251201"
            if (versionStringParts.Length >= 3 && versionStringParts[1].Equals("build", StringComparison.OrdinalIgnoreCase))
            {
                if (int.TryParse(versionStringParts[2], out int versionComponentBuildNumber))
                {
                    string[] versionComponents = versionStringParts[0].Split('.');
                    if (versionComponents.Length >= 2)
                    {
                        int versionComponentMajor = int.TryParse(versionComponents[0], out int versionComponentMajorParsed) ? versionComponentMajorParsed : 0;
                        int versionComponentMinor = int.TryParse(versionComponents[1], out int versionComponentMinorParsed) ? versionComponentMinorParsed : 0;
                        int versionComponentPatch = (versionComponents.Length > 2 && int.TryParse(versionComponents[2], out int versionComponentPatchParsed)) ? versionComponentPatchParsed : 0;

                        return new System.Version(versionComponentMajor, versionComponentMinor, versionComponentPatch, versionComponentBuildNumber);
                    }
                }
            }

            // If format is not recognized, consider version very old
            return new System.Version("0.0.0");
        }

        private void ButtonAbout_Click(object? sender, EventArgs e)
        {
            // Form the message
            string message = $"FLAC Benchmark-H {programVersionCurrent}\n\n" +
            "Do you want to visit the project's homepage?\n" +
            "https://github.com/hat3k/FLAC-Benchmark-H";

            // Show dialog
            DialogResult result = MessageBox.Show(
            message,
            "About",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Information,
            MessageBoxDefaultButton.Button1);

            // If user clicked Yes - open browser
            if (result == DialogResult.Yes)
            {
                try
                {
                    System.Diagnostics.Process.Start(new ProcessStartInfo
                    {
                        FileName = "https://github.com/hat3k/FLAC-Benchmark-H",
                        UseShellExecute = true,
                        Verb = "open"
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to open URL: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void EnableListViewDoubleBuffering()
        {
            var listViewType = typeof(ListView);
            var doubleBufferedProperty = listViewType.GetProperty("DoubleBuffered", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

            // Apply to all ListViews including OwnerDraw ones
            doubleBufferedProperty?.SetValue(listViewEncoders, true, null);
            doubleBufferedProperty?.SetValue(listViewAudioFiles, true, null);
        }

        // FORM LOAD
        private async void Form1_Load(object? sender, EventArgs e)
        {
            Text = $"FLAC Benchmark-H [{programVersionCurrent}]";
            progressBarEncoder.ManualText = string.Empty;
            progressBarDecoder.ManualText = string.Empty;
            labelStopped.Text = string.Empty;
            labelAudioFileRemoved.Text = string.Empty;
            EnableListViewDoubleBuffering();

            InitializeMediaInfoPool();

            // Ensure tabs are rendered
            foreach (TabPage tab in new[] {
                tabPageMiscSettings,
                tabPagePlotsSettings,
                tabPageLogsSettings,
                tabPageQuickSettings,
                DetectDupes,
                ScalingPlots,
                TestForErrors,
                Benchmark })
            {
                tabControlLog.SelectedTab = tab;
                Application.DoEvents();

                // Apply auto-width to all log tabs
                switch (tab)
                {
                    case var _ when tab == DetectDupes:
                        dataGridViewLogDetectDupes.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells);
                        break;
                    case var _ when tab == TestForErrors:
                        dataGridViewLogTestForErrors.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells);
                        break;
                    case var _ when tab == Benchmark:
                        dataGridViewLog.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells);
                        break;
                }
            }

            LoadSettings();
            LoadEncoders();
            LoadAudioFiles();
            LoadJobs();

            CheckBoxDrawMultiplots_CheckedChanged(null, EventArgs.Empty);
            CheckBoxShowTooltipsOnPlots_CheckedChanged(null, EventArgs.Empty);

            checkAllToolStripMenuItemAudioFiles.DropDown.Closing += ContextMenu_Closing;
            selectAllToolStripMenuItemAudioFiles.DropDown.Closing += ContextMenu_Closing;
            toolsToolStripMenuItemAudioFiles.DropDown.Closing += ContextMenu_Closing;

            ActiveControl = null; // Remove focus from all elements
        }
        private void Form1_FormClosing(object? sender, FormClosingEventArgs e)
        {
            // Save user settings and lists
            SaveSettings();     // General settings

            if (!_isProcessingEncodersQueue && !_isRefreshingEncoders)
            {
                SaveEncoders(); // Save encoders ONLY if loading is NOT in progress
            }

            SaveAudioFiles();   // Audio files list
            SaveJobs();         // Job list

            // Clean up MediaInfo pool
            while (_mediaInfoPool.TryDequeue(out var mediaInfo))
            {
                mediaInfo?.Close();
                if (mediaInfo is IDisposable disposable)
                    disposable.Dispose();
            }

            // Stop and dispose UI timers and performance counters
            cpuUsageTimer?.Stop();
            cpuUsageTimer?.Dispose();
            temporaryMessageTimer?.Stop();
            temporaryMessageTimer?.Dispose();
            cpuLoadCounter?.Dispose();

            // Dispose pause/resume synchronization object
            _pauseEvent?.Dispose();

            // Unsubscribe from context menu events
            checkAllToolStripMenuItemAudioFiles.DropDown.Closing -= ContextMenu_Closing;
            selectAllToolStripMenuItemAudioFiles.DropDown.Closing -= ContextMenu_Closing;
            toolsToolStripMenuItemAudioFiles.DropDown.Closing -= ContextMenu_Closing;

            // Clean up temporary files based on user preference
            try
            {
                if (Directory.Exists(tempFolderPath))
                {
                    if (checkBoxClearTempFolder.Checked)
                    {
                        // Option 1: Delete entire temp folder (user requested full cleanup)
                        Directory.Delete(tempFolderPath, true);
                    }
                    else
                    {
                        // Option 2: Delete only warm-up files (preserve other temp files for debugging)
                        foreach (var file in Directory.GetFiles(tempFolderPath, "temp_warmup_*.*"))
                        {
                            try { File.Delete(file); } catch { }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log but don't block app exit
                Debug.WriteLine($"Failed to clean temp folder: {ex.Message}");
            }
        }
    }
}