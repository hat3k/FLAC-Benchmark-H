using ClosedXML.Excel;
using DocumentFormat.OpenXml;
using MediaInfoLib;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FLAC_Benchmark_H
{
    public partial class Form1 : Form
    {
        // Application version
        public string programVersionCurrent = "1.7.4 build 20251004"; // Current app version
        public string programVersionIgnored = null;                   // Previously ignored update

        // Hardware info
        private int physicalCores; // Number of physical CPU cores
        private int threadCount;   // Total logical threads

        // CPU monitoring
        private PerformanceCounter cpuLoadCounter = null;           // CPU Load counter (whole system)
        private bool performanceCountersAvailable = false;          // True if counters initialized
        private System.Windows.Forms.Timer cpuUsageTimer;           // Updates CPU usage label
        private System.Windows.Forms.Timer temporaryMessageTimer;   // Updates temporary messages

        private PerformanceCounter _cpuClockCounter;            // CPU clock counter (as % of base freq)
        private List<double> _cpuClockReadings;
        private int _baseClockMhz = 0;                          // Base CPU frequency in MHz

        // UI state
        private bool isCpuInfoLoaded = false;
        private ScriptConstructorForm? scriptForm = null;

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
        private Process _process;                                           // Running process instance
        private bool isExecuting = false;                                   // True if encoding/decoding is active
        private bool _isEncodingStopped = false;                            // Request to stop
        private bool _isPaused = false;                                     // Pause state
        private ManualResetEvent _pauseEvent = new ManualResetEvent(true);  // Controls pause/resume sync for encoding thread

        public Form1()
        {
            InitializeComponent();
            InitializeDragAndDrop(); // Initialize drag-and-drop
            this.FormClosing += Form1_FormClosing; // Register the form closing event handler
            this.listViewEncoders.KeyDown += ListViewEncoders_KeyDown;
            this.listViewAudioFiles.KeyDown += ListViewAudioFiles_KeyDown;
            this.listViewJobs.KeyDown += ListViewJobs_KeyDown;
            this.dataGridViewLog.KeyDown += DataGridViewLog_KeyDown;
            this.dataGridViewLogDetectDupes.KeyDown += DataGridViewLogDetectDupes_KeyDown;
            this.dataGridViewLogTestForErrors.KeyDown += DataGridViewLogTestForErrors_KeyDown;
            this.textBoxCompressionLevel.KeyDown += new KeyEventHandler(this.textBoxCompressionLevel_KeyDown);
            this.textBoxThreads.KeyDown += new KeyEventHandler(this.textBoxThreads_KeyDown);
            this.textBoxCommandLineOptionsEncoder.KeyDown += new KeyEventHandler(this.textBoxCommandLineOptionsEncoder_KeyDown);
            this.textBoxCommandLineOptionsDecoder.KeyDown += new KeyEventHandler(this.textBoxCommandLineOptionsDecoder_KeyDown);
            LoadCPUInfoAsync();

            // Initialize CPU Usage counter (Current CPU Load in %)
            try
            {
                cpuLoadCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                performanceCountersAvailable = true;
            }
            catch (Exception ex) when (
            ex is InvalidOperationException || ex is System.ComponentModel.Win32Exception)
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
            cpuUsageTimer = new System.Windows.Forms.Timer(); // Explicitly specify System.Windows.Forms.Timer
            cpuUsageTimer.Interval = 250; // Every 250 ms
            cpuUsageTimer.Tick += async (sender, e) => await UpdateCpuUsageAsync();
            cpuUsageTimer.Start();

            temporaryMessageTimer = new System.Windows.Forms.Timer();
            temporaryMessageTimer.Tick += (s, e) =>
            {
                try
                {
                    if (!this.IsDisposed && !this.Disposing)
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

            InitializedataGridViewLog();
            InitializedataGridViewLogDetectDupes();
            InitializedataGridViewLogTestForErrors();

            tempFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp"); // Initialize the path to the temporary folder
            _process = new Process(); // Initialize _process to avoid nullability warning

            dataGridViewLog.CellContentClick += dataGridViewLog_CellContentClick;
            dataGridViewLog.MouseDown += dataGridViewLog_MouseDown;

            dataGridViewLogDetectDupes.CellContentClick += dataGridViewLogDetectDupes_CellContentClick;
            dataGridViewLogDetectDupes.MouseDown += dataGridViewLogDetectDupes_MouseDown;

            dataGridViewLogTestForErrors.CellContentClick += dataGridViewLogTestForErrors_CellContentClick;
            dataGridViewLogTestForErrors.MouseDown += dataGridViewLogTestForErrors_MouseDown;

            buttonPauseResume.Click += buttonPauseResume_Click;

            // Enable custom drawing for listViewJobs
            listViewJobs.OwnerDraw = true;
            listViewJobs.DrawColumnHeader += ListViewJobs_DrawColumnHeader;
            listViewJobs.DrawSubItem += ListViewJobs_DrawSubItem;
            comboBoxCPUPriority.SelectedIndex = 3;
        }

        private string NormalizeSpaces(string input)
        {
            return Regex.Replace(input.Trim(), @"\s+", " "); // Remove extra spaces inside the string
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
                    using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("select * from Win32_Processor"))
                    {
                        foreach (ManagementObject obj in searcher.Get())
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

            switch (priorityText)
            {
                case "Idle": return ProcessPriorityClass.Idle;
                case "BelowNormal": return ProcessPriorityClass.BelowNormal;
                case "Normal": return ProcessPriorityClass.Normal;
                case "AboveNormal": return ProcessPriorityClass.AboveNormal;
                case "High": return ProcessPriorityClass.High;
                case "RealTime": return ProcessPriorityClass.RealTime;
                default: return ProcessPriorityClass.Normal;
            }
        }

        // Method to save settings to .txt files
        private void SaveSettings()
        {
            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                string tempPathToSave = tempFolderPath;

                if (!string.IsNullOrEmpty(tempFolderPath) &&
                    tempFolderPath.StartsWith(baseDir, StringComparison.OrdinalIgnoreCase))
                {
                    string relativePart = tempFolderPath.Substring(baseDir.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    tempPathToSave = $".\\{relativePart}";
                }

                var columnVisibility = string.Join(",",
                    dataGridViewLog.Columns.Cast<DataGridViewColumn>()
                        .Select(col => $"{col.Name}:{col.Visible}"));

                var columnHeaders = string.Join(",",
                    dataGridViewLog.Columns.Cast<DataGridViewColumn>()
                        .Select(col => $"{col.Name}:{col.HeaderText.Replace(":", "\\:")}"));

                var settings = new List<string>
                {
                    $"CompressionLevel={textBoxCompressionLevel.Text}",
                    $"Threads={textBoxThreads.Text}",
                    $"CommandLineOptionsEncoder={textBoxCommandLineOptionsEncoder.Text}",
                    $"CommandLineOptionsDecoder={textBoxCommandLineOptionsDecoder.Text}",
                    $"CPUPriority={comboBoxCPUPriority.SelectedItem}",
                    $"TempFolderPath={tempPathToSave}",
                    $"ClearTempFolderOnExit={checkBoxClearTempFolder.Checked}",
                    $"RemoveMetadata={checkBoxRemoveMetadata.Checked}",
                    $"AddMD5OnLoadWav={checkBoxAddMD5OnLoadWav.Checked}",
                    $"AddWarmupPass={checkBoxWarmupPass.Checked}",
                    $"WarningsAsErrors={checkBoxWarningsAsErrors.Checked}",
                    $"AutoAnalyzeLog={checkBoxAutoAnalyzeLog.Checked}",
                    $"PreventSleep={checkBoxPreventSleep.Checked}",
                    $"CheckForUpdatesOnStartup={checkBoxCheckForUpdatesOnStartup.Checked}",
                    $"IgnoredVersion={programVersionIgnored ?? ""}",
                    $"LogColumnVisibility={columnVisibility}",
                    $"LogColumnHeaders={columnHeaders}"
                };

                File.WriteAllLines(SettingsGeneralFilePath, settings);
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
                var lines = listViewJobs.Items.Cast<ListViewItem>()
                    .Select(item =>
                    {
                        string status = item.Checked ? "Checked" : "Unchecked";
                        string type = item.Text;
                        string passes = item.SubItems[1].Text;
                        string parameters = item.SubItems[2].Text;

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
                    var parts = line.Split(new[] { '=' }, 2);
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
                        case "CPUPriority":
                            comboBoxCPUPriority.SelectedItem = value;
                            break;
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
                            checkBoxClearTempFolder.Checked = bool.TryParse(value, out bool clear) ? clear : false;
                            break;
                        case "RemoveMetadata":
                            checkBoxRemoveMetadata.Checked = bool.TryParse(value, out bool remove) ? remove : false;
                            break;
                        case "AddMD5OnLoadWav":
                            checkBoxAddMD5OnLoadWav.Checked = bool.TryParse(value, out bool addMd5) ? addMd5 : false;
                            break;
                        case "AddWarmupPass":
                            checkBoxWarmupPass.Checked = bool.TryParse(value, out bool warmup) ? warmup : false;
                            break;
                        case "WarningsAsErrors":
                            checkBoxWarningsAsErrors.Checked = bool.TryParse(value, out bool warnings) ? warnings : false;
                            break;
                        case "AutoAnalyzeLog":
                            checkBoxAutoAnalyzeLog.Checked = bool.TryParse(value, out bool analyze) ? analyze : false;
                            break;
                        case "PreventSleep":
                            checkBoxPreventSleep.Checked = bool.TryParse(value, out bool prevent) ? prevent : false;
                            break;
                        case "CheckForUpdatesOnStartup":
                            checkBoxCheckForUpdatesOnStartup.Checked = bool.TryParse(value, out bool check) ? check : false;
                            break;
                        case "IgnoredVersion":
                            programVersionIgnored = string.IsNullOrEmpty(value) ? null : value;
                            break;
                        case "LogColumnVisibility":
                            LoadDataGridViewLogColumnVisibility(value);
                            break;
                        case "LogColumnHeaders":
                            LoadDataGridViewLogColumnHeaders(value);
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
                    catch (Exception ex)
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
                listViewEncoders.Items.Clear();

                groupBoxEncoders.Text = "Loading...";

                var missingFiles = new List<string>();
                var tasks = lines.Select(async line =>
                {
                    if (string.IsNullOrWhiteSpace(line))
                        return null;

                    if (line.StartsWith("Checked|") || line.StartsWith("Unchecked|"))
                    {
                        // New format: Checked|path
                        int separatorIndex = line.IndexOf('|');
                        if (separatorIndex == -1 || separatorIndex == line.Length - 1)
                            return null;

                        bool isChecked = line.StartsWith("Checked");
                        string encoderPath = line.Substring(separatorIndex + 1);

                        if (!string.IsNullOrEmpty(encoderPath) && File.Exists(encoderPath))
                        {
                            var item = await Task.Run(() => CreateListViewEncodersItem(encoderPath, isChecked));
                            if (item != null)
                            {
                                item.Checked = isChecked;
                                return item;
                            }
                        }
                        else
                        {
                            missingFiles.Add(encoderPath);
                        }
                    }
                    else if (line.Contains('~'))
                    {
                        // Old format: path~checked
                        var parts = line.Split('~');
                        if (parts.Length >= 2 && bool.TryParse(parts[parts.Length - 1], out bool isChecked))
                        {
                            string encoderPath = string.Join("~", parts.Take(parts.Length - 1));

                            if (!string.IsNullOrEmpty(encoderPath) && File.Exists(encoderPath))
                            {
                                var item = await Task.Run(() => CreateListViewEncodersItem(encoderPath, isChecked));
                                if (item != null)
                                {
                                    item.Checked = isChecked;
                                    return item;
                                }
                            }
                            else
                            {
                                missingFiles.Add(encoderPath);
                            }
                        }
                    }
                    return null;
                });

                var items = await Task.WhenAll(tasks);

                foreach (var item in items)
                {
                    if (item != null)
                    {
                        listViewEncoders.Items.Add(item);
                    }
                }

                SaveEncoders();

                if (missingFiles.Count > 0)
                {
                    string warningMessage = $"The following encoders were missing and not loaded:\n\n" +
                                          string.Join("\n", missingFiles.Select(Path.GetFileName)) +
                                          "\n\nCheck if they still exist on your system.";

                    MessageBox.Show(warningMessage, "Missing Encoders",
                                  MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading encoders: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            UpdateGroupBoxEncodersHeader();
        }
        private async void LoadAudioFiles()
        {
            if (!File.Exists(SettingsAudioFilesFilePath))
                return;

            try
            {
                string[] lines = await File.ReadAllLinesAsync(SettingsAudioFilesFilePath);
                listViewAudioFiles.Items.Clear();

                groupBoxAudioFiles.Text = "Loading...";

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
                            var item = await Task.Run(() => CreateListViewAudioFilesItem(audioFilePath, isChecked));
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
                        if (parts.Length >= 2 && bool.TryParse(parts[parts.Length - 1], out bool isChecked))
                        {
                            string audioFilePath = string.Join("~", parts.Take(parts.Length - 1));

                            if (!string.IsNullOrEmpty(audioFilePath) && File.Exists(audioFilePath))
                            {
                                var item = await Task.Run(() => CreateListViewAudioFilesItem(audioFilePath, isChecked));
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

                foreach (var item in items)
                {
                    if (item != null)
                    {
                        listViewAudioFiles.Items.Add(item);
                    }
                }

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
        private void LoadDataGridViewLogColumnVisibility(string visibilityString)
        {
            var pairs = visibilityString.Split(',');
            foreach (var pair in pairs)
            {
                var parts = pair.Split(':');
                if (parts.Length == 2 && bool.TryParse(parts[1], out bool visible))
                {
                    string columnName = parts[0];
                    if (dataGridViewLog.Columns[columnName] is DataGridViewColumn col)
                    {
                        col.Visible = visible;
                    }
                }
            }
        }
        private void LoadDataGridViewLogColumnHeaders(string headersString)
        {
            var pairs = headersString.Split(',');
            foreach (var pair in pairs)
            {
                int lastColonIndex = pair.LastIndexOf(':');
                if (lastColonIndex > 0 && lastColonIndex < pair.Length - 1)
                {
                    string columnName = pair.Substring(0, lastColonIndex);
                    string headerText = pair.Substring(lastColonIndex + 1).Replace("\\:", ":");

                    if (dataGridViewLog.Columns[columnName] is DataGridViewColumn col)
                    {
                        col.HeaderText = headerText;
                    }
                }
            }
        }
        private void LoadJobs()
        {
            BackupJobsFile();
            if (!File.Exists(SettingsJobsFilePath))
                return;

            try
            {
                string[] lines = File.ReadAllLines(SettingsJobsFilePath);
                listViewJobs.Items.Clear();

                foreach (string line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    if (line.StartsWith("Checked|") || line.StartsWith("Unchecked|"))
                    {
                        // New format: Checked|Type|Passes|Parameters
                        int firstBar = line.IndexOf('|');
                        int secondBar = line.IndexOf('|', firstBar + 1);
                        int thirdBar = line.IndexOf('|', secondBar + 1);

                        if (firstBar != -1 && secondBar != -1 && thirdBar != -1 && thirdBar != line.Length - 1)
                        {
                            bool isChecked = line.StartsWith("Checked");
                            string type = line.Substring(firstBar + 1, secondBar - firstBar - 1);
                            string passes = line.Substring(secondBar + 1, thirdBar - secondBar - 1);
                            string parameters = line.Substring(thirdBar + 1);

                            var item = new ListViewItem(type) { Checked = isChecked };
                            item.SubItems.Add(passes);
                            item.SubItems.Add(parameters);
                            listViewJobs.Invoke(new Action(() => listViewJobs.Items.Add(item)));
                            continue;
                        }
                    }
                    else if (line.Contains('~'))
                    {
                        // Old format: Text~Checked~Passes~Parameters
                        var parts = line.Split('~');
                        if (parts.Length == 4 && bool.TryParse(parts[1], out bool isChecked))
                        {
                            var item = new ListViewItem(NormalizeSpaces(parts[0])) { Checked = isChecked };
                            item.SubItems.Add(NormalizeSpaces(parts[2]));
                            item.SubItems.Add(NormalizeSpaces(parts[3]));
                            listViewJobs.Invoke(new Action(() => listViewJobs.Items.Add(item)));
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
                MessageBox.Show($"Error creating backup for jobs file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void InitializeDragAndDrop()
        {
            // Enable file drag-and-drop for the encoders ListView
            listViewEncoders.AllowDrop = true;
            listViewEncoders.DragEnter += ListViewEncoders_DragEnter;
            listViewEncoders.DragDrop += ListViewEncoders_DragDrop;
            // Enable file drag-and-drop for the audio files ListView
            listViewAudioFiles.AllowDrop = true;
            listViewAudioFiles.DragEnter += ListViewAudioFiles_DragEnter;
            listViewAudioFiles.DragDrop += ListViewAudioFiles_DragDrop;
            // Enable file drag-and-drop for the jobs ListView
            listViewJobs.AllowDrop = true;
            listViewJobs.DragEnter += ListViewJobs_DragEnter;
            listViewJobs.DragDrop += ListViewJobs_DragDrop;
        }

        // Encoders
        private void ListViewEncoders_DragEnter(object? sender, DragEventArgs e)
        {
            if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
            {
                string[] files = (string[]?)e.Data.GetData(DataFormats.FileDrop) ?? Array.Empty<string>();
                // Check if there's at least one .exe file that is NOT metaflac.exe
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
            string[] files = (string[]?)e.Data?.GetData(DataFormats.FileDrop) ?? Array.Empty<string>();
            if (files.Length > 0)
            {
                groupBoxEncoders.Text = "Choose Encoder (Drag'n'Drop of files and folders is available) - loading...";

                var tasks = files.Select(async file =>
                {
                    if (Directory.Exists(file))
                    {
                        await AddEncoders(file); // Asynchronously add executable files from directory
                    }
                    else if (Path.GetExtension(file).Equals(".exe", StringComparison.OrdinalIgnoreCase))
                    {
                        // Skip metaflac.exe to prevent it from being added to the encoders list
                        if (Path.GetFileName(file).Equals("metaflac.exe", StringComparison.OrdinalIgnoreCase))
                        {
                            return null; // Return null for metaflac.exe
                        }

                        var item = await CreateListViewEncodersItem(file, true); // Create a list item
                        return item; // Return the created item
                    }
                    return null; // Return null if it's not a directory or .exe file
                });

                var items = await Task.WhenAll(tasks); // Wait for all tasks to complete

                // Add items to the ListView
                foreach (var item in items)
                {
                    if (item != null)
                    {
                        listViewEncoders.Items.Add(item); // Add the item to the ListView
                    }
                }
            }
            UpdateGroupBoxEncodersHeader();
        }
        private async void buttonAddEncoders_Click(object? sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Title = "Select Executable Files";
                openFileDialog.Filter = "Executable Files (*.exe)|*.exe|All Files (*.*)|*.*";
                openFileDialog.Multiselect = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    groupBoxEncoders.Text = "Choose Encoder (Drag'n'Drop of files and folders is available) - loading...";

                    var tasks = openFileDialog.FileNames
                        // Filter out metaflac.exe to prevent adding it to the encoders list
                        .Where(file => !Path.GetFileName(file).Equals("metaflac.exe", StringComparison.OrdinalIgnoreCase))
                        .Select(async file =>
                        {
                            var item = await CreateListViewEncodersItem(file, true); // Create a list item
                            return item; // Return the created item
                        });

                    var items = await Task.WhenAll(tasks); // Wait for all tasks to complete

                    foreach (var item in items)
                    {
                        if (item != null)
                        {
                            listViewEncoders.Items.Add(item); // Add items to the ListView
                        }
                    }
                }
            }
            UpdateGroupBoxEncodersHeader();
        }
        private async Task AddEncoders(string directory)
        {
            try
            {
                // Get all .exe files, but exclude metaflac.exe
                var exeFiles = Directory.GetFiles(directory, "*.exe", SearchOption.AllDirectories)
                    .Where(file => !Path.GetFileName(file).Equals("metaflac.exe", StringComparison.OrdinalIgnoreCase))
                    .ToList(); // ToList() to execute the query and avoid issues if directory changes

                var tasks = exeFiles.Select(async file =>
                {
                    var item = await CreateListViewEncodersItem(file, true); // Create an item and return it
                    return item; // Return the created item
                });

                var items = await Task.WhenAll(tasks); // Wait for all tasks to complete

                foreach (var item in items)
                {
                    if (item != null)
                    {
                        listViewEncoders.Items.Add(item); // Add items to the ListView
                    }
                }
                UpdateGroupBoxEncodersHeader();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error accessing directory: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private async Task<ListViewItem> CreateListViewEncodersItem(string encoderPath, bool isChecked)
        {
            if (!File.Exists(encoderPath))
            {
                return null; // If the file is not found, return null
            }

            // Get encoder information
            var encoderInfo = await GetEncoderInfo(encoderPath); // Asynchronously get information

            // Create a ListViewItem
            var item = new ListViewItem(Path.GetFileName(encoderPath))
            {
                Tag = encoderPath,
                Checked = isChecked
            };

            // Fill subitems
            item.SubItems.Add(encoderInfo.Version);
            item.SubItems.Add(encoderInfo.DirectoryPath);
            item.SubItems.Add($"{encoderInfo.FileSize:n0} bytes");
            item.SubItems.Add(encoderInfo.LastModified.ToString("yyyy.MM.dd HH:mm"));

            return item;
        }
        private async Task<EncoderInfo> GetEncoderInfo(string encoderPath)
        {
            // Check if the information is in the cache
            if (encoderInfoCache.TryGetValue(encoderPath, out var cachedInfo))
            {
                return cachedInfo; // Return cached information
            }

            // Get file size and last modified date
            long fileSize = new FileInfo(encoderPath).Length;
            DateTime lastModified = new FileInfo(encoderPath).LastWriteTime;

            // Get file name and directory path
            string fileName = Path.GetFileName(encoderPath);
            string directoryPath = Path.GetDirectoryName(encoderPath);

            string version = "N/A"; // Default value for version

            // Get encoder version information
            try
            {
                version = await Task.Run(() =>
                {
                    using (Process process = new Process())
                    {
                        process.StartInfo.FileName = encoderPath;
                        process.StartInfo.Arguments = "--version"; // Argument to get the version
                        process.StartInfo.UseShellExecute = false;
                        process.StartInfo.RedirectStandardOutput = true; // Redirect standard output
                        process.StartInfo.CreateNoWindow = true;

                        process.Start();
                        string result = process.StandardOutput.ReadLine(); // Read the first line of output
                        process.WaitForExit();

                        return result ?? "N/A"; // Return "N/A" if the version is not found
                    }
                });
            }
            catch (Exception)
            {
                version = "N/A"; // Return "N/A" in case of an error
            }

            // Create an EncoderInfo object
            var encoderInfo = new EncoderInfo
            {
                FilePath = encoderPath,
                DirectoryPath = directoryPath,
                FileName = fileName,
                Version = version,
                FileSize = fileSize,
                LastModified = lastModified
            };

            // Add new information to the cache
            encoderInfoCache[encoderPath] = encoderInfo; // Cache the information
            return encoderInfo;
        }

        // Class to store encoder information
        private class EncoderInfo
        {
            public string FilePath { get; set; }
            public string DirectoryPath { get; set; }
            public string FileName { get; set; }
            public string Version { get; set; }
            public long FileSize { get; set; }
            public DateTime LastModified { get; set; }
        }
        private ConcurrentDictionary<string, EncoderInfo> encoderInfoCache = new ConcurrentDictionary<string, EncoderInfo>();

        private void buttonUpEncoder_Click(object? sender, EventArgs e)
        {
            MoveSelectedItems(listViewEncoders, -1); // Pass -1 to move up
        }
        private void buttonDownEncoder_Click(object? sender, EventArgs e)
        {
            MoveSelectedItems(listViewEncoders, 1); // Pass 1 to move down
        }
        private void buttonRemoveEncoder_Click(object? sender, EventArgs e)
        {
            // Remove selected items from listViewEncoders
            for (int i = listViewEncoders.Items.Count - 1; i >= 0; i--)
            {
                if (listViewEncoders.Items[i].Selected) // Check if the item is selected
                {
                    listViewEncoders.Items.RemoveAt(i); // Remove the item
                }
            }
            UpdateGroupBoxEncodersHeader();
        }
        private void buttonClearEncoders_Click(object? sender, EventArgs e)
        {
            listViewEncoders.Items.Clear();
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

        //Audio files
        private void ListViewAudioFiles_DragEnter(object? sender, DragEventArgs e)
        {
            if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
            {
                string[] files = (string[]?)e.Data.GetData(DataFormats.FileDrop) ?? Array.Empty<string>();
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
            string[] files = (string[]?)e.Data?.GetData(DataFormats.FileDrop) ?? Array.Empty<string>();
            if (files.Length > 0)
            {
                groupBoxAudioFiles.Text = "Choose Audio Files (Drag'n'Drop of files and folders is available) - loading...";

                var tasks = files.Select(async file =>
                {
                    if (Directory.Exists(file))
                    {
                        // Get all audio files in the directory
                        var directoryFiles = Directory.GetFiles(file, "*.wav", SearchOption.AllDirectories)
                            .Concat(Directory.GetFiles(file, "*.flac", SearchOption.AllDirectories));

                        // Create a ListViewItem for each found audio file
                        var items = await Task.WhenAll(directoryFiles.Select(f => Task.Run(() => CreateListViewAudioFilesItem(f, true))));
                        return items; // Return an array of ListViewItem
                    }
                    else if (IsAudioFile(file) && File.Exists(file))
                    {
                        var item = await Task.Run(() => CreateListViewAudioFilesItem(file, true)); // Create a list item
                        return new[] { item }; // Return an array with one item
                    }

                    return Array.Empty<ListViewItem>(); // Return an empty array if it's not an audio file
                });

                var itemsList = await Task.WhenAll(tasks); // Wait for all tasks to complete

                // Add items to the ListView
                foreach (var itemList in itemsList)
                {
                    if (itemList != null && itemList.Length > 0)
                    {
                        listViewAudioFiles.Items.AddRange(itemList); // Add an array of items to the ListView
                    }
                }
            }
            UpdateGroupBoxAudioFilesHeader();
        }
        private async void buttonAddAudioFiles_Click(object? sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Title = "Select Audio Files";
                openFileDialog.Filter = "Audio Files (*.flac;*.wav)|*.flac;*.wav|FLAC Files (*.flac)|*.flac|WAV Files (*.wav)|*.wav|All Files (*.*)|*.*";
                openFileDialog.Multiselect = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    groupBoxAudioFiles.Text = "Choose Audio Files (Drag'n'Drop of files and folders is available) - loading...";

                    var tasks = openFileDialog.FileNames.Select(async file =>
                    {
                        var item = await Task.Run(() => CreateListViewAudioFilesItem(file, true)); // Create a list item
                        item.Checked = true; // Set the "checked" status
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
        private bool IsAudioFile(string audioFilePath)
        {
            string extension = Path.GetExtension(audioFilePath);
            return extension.Equals(".wav", StringComparison.OrdinalIgnoreCase) ||
                   extension.Equals(".flac", StringComparison.OrdinalIgnoreCase);
        }
        private async Task<ListViewItem> CreateListViewAudioFilesItem(string audioFilePath, bool isChecked)
        {
            if (!File.Exists(audioFilePath))
            {
                throw new FileNotFoundException("Audio file not found", audioFilePath);
            }

            // Use the GetAudioInfo method to get file information
            var audioFileInfo = await GetAudioInfo(audioFilePath);

            // Create a ListViewItem
            var item = new ListViewItem(Path.GetFileName(audioFilePath))
            {
                Tag = audioFilePath,
                Checked = true
            };

            // Fill subitems
            item.SubItems.Add(audioFileInfo.BitDepthString);
            item.SubItems.Add(audioFileInfo.SamplingRateString);
            item.SubItems.Add($"{audioFileInfo.Duration:n0} ms");
            item.SubItems.Add($"{audioFileInfo.FileSize:n0} bytes");
            item.SubItems.Add(audioFileInfo.Md5Hash);
            item.SubItems.Add(audioFileInfo.DirectoryPath);
            return item;
        }
        private async Task<AudioFileInfo> GetAudioInfo(string audioFilePath)
        {
            // Check if the information is in the cache
            if (audioInfoCache.TryGetValue(audioFilePath, out var cachedInfo))
            {
                return cachedInfo; // Return cached information
            }

            var mediaInfo = new MediaInfoLib.MediaInfo();
            mediaInfo.Open(audioFilePath);

            string duration = mediaInfo.Get(StreamKind.Audio, 0, "Duration") ?? "N/A";
            string bitDepth = mediaInfo.Get(StreamKind.Audio, 0, "BitDepth") ?? "N/A";                      // Number only
            string bitDepthString = mediaInfo.Get(StreamKind.Audio, 0, "BitDepth/String") ?? "N/A";         // Number + bits
            string samplingRate = mediaInfo.Get(StreamKind.Audio, 0, "SamplingRate") ?? "N/A";              // Number only (e.g. 44100)
            string samplingRateString = mediaInfo.Get(StreamKind.Audio, 0, "SamplingRate/String") ?? "N/A"; // Number + kHz (44.1 kHz)
            FileInfo file = new FileInfo(audioFilePath);
            long fileSize = file.Length;
            DateTime lastWriteTime = file.LastWriteTime;
            string extension = Path.GetExtension(audioFilePath).ToLowerInvariant();
            string md5Hash = "N/A"; // Default value for MD5

            // Determine the file type and get the corresponding MD5
            if (extension == ".flac")
            {
                md5Hash = mediaInfo.Get(StreamKind.Audio, 0, "MD5_Unencoded") ?? "N/A";
            }
            else if (extension == ".wav" && checkBoxAddMD5OnLoadWav.Checked)
            {
                md5Hash = await CalculateWavMD5Async(audioFilePath);
            }

            mediaInfo.Close();

            // Add new information to the cache
            var audioFileInfo = new AudioFileInfo
            {
                FilePath = audioFilePath,
                DirectoryPath = Path.GetDirectoryName(audioFilePath),
                FileName = Path.GetFileName(audioFilePath),
                Extension = extension,
                BitDepth = bitDepth,
                BitDepthString = bitDepthString,
                SamplingRate = samplingRate,
                SamplingRateString = samplingRateString,
                Duration = duration,
                FileSize = fileSize,
                Md5Hash = md5Hash,
                LastWriteTime = lastWriteTime
            };

            audioInfoCache[audioFilePath] = audioFileInfo; // Cache the information
            return audioFileInfo;
        }

        // Class to store audio file information
        private class AudioFileInfo
        {
            public string FilePath { get; set; }
            public string DirectoryPath { get; set; }
            public string FileName { get; set; }
            public string Extension { get; set; }
            public string BitDepth { get; set; }
            public string BitDepthString { get; set; }
            public string SamplingRate { get; set; }
            public string SamplingRateString { get; set; }
            public string Duration { get; set; }
            public long FileSize { get; set; }
            public DateTime LastWriteTime { get; set; }

            public string Md5Hash { get; set; }
            public string ErrorDetails { get; set; }
        }
        private ConcurrentDictionary<string, AudioFileInfo> audioInfoCache = new ConcurrentDictionary<string, AudioFileInfo>();

        /// <summary>
        /// Calculates the MD5 hash of the PCM audio data in a WAV file by reading only the "data" chunk.
        /// This ensures that metadata (like ID3 tags) does not affect the hash, making it suitable for duplicate detection.
        /// </summary>
        /// <param name="audioFilePath">Path to the WAV file.</param>
        /// <returns>The MD5 hash as a 32-character uppercase hex string, or "MD5 calculation failed" on error.</returns>
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
                    string errorMessage = "Invalid WAV file: Missing RIFF header.";
                    UpdateCacheWithMD5Error(audioFilePath, errorMessage);
                    return "MD5 calculation failed";
                }

                reader.ReadUInt32(); // Skip file size

                // Validate WAVE header
                if (reader.ReadUInt32() != 0x45564157) // "WAVE"
                {
                    string errorMessage = "Invalid WAV file: Missing WAVE header.";
                    UpdateCacheWithMD5Error(audioFilePath, errorMessage);
                    return "MD5 calculation failed";
                }

                // Process chunks
                while (reader.BaseStream.Position < reader.BaseStream.Length)
                {
                    uint chunkId = reader.ReadUInt32();
                    uint chunkSize = reader.ReadUInt32();

                    if (chunkId == 0x20746D66) // "fmt "
                    {
                        reader.BaseStream.Seek(chunkSize, SeekOrigin.Current);
                    }
                    else if (chunkId == 0x61746164) // "data"
                    {
                        // Validate data chunk bounds
                        if (reader.BaseStream.Position + chunkSize > reader.BaseStream.Length)
                        {
                            string errorMessage = "Invalid WAV file: 'data' chunk size exceeds file bounds.";
                            UpdateCacheWithMD5Error(audioFilePath, errorMessage);
                            return "MD5 calculation failed";
                        }

                        long bytesToRead = chunkSize;
                        byte[] buffer = new byte[8192];
                        long totalBytesRead = 0;

                        // Stream the data chunk and update MD5 incrementally
                        while (totalBytesRead < bytesToRead)
                        {
                            int bytesToReadThisIteration = (int)Math.Min(buffer.Length, bytesToRead - totalBytesRead);
                            int bytesRead = await reader.BaseStream.ReadAsync(buffer, 0, bytesToReadThisIteration);

                            if (bytesRead == 0)
                            {
                                string errorMessage = "Unexpected end of file while reading 'data' chunk.";
                                UpdateCacheWithMD5Error(audioFilePath, errorMessage);
                                return "MD5 calculation failed";
                            }

                            md5.TransformBlock(buffer, 0, bytesRead, null, 0);
                            totalBytesRead += bytesRead;
                        }

                        // Finalize hash
                        md5.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                        string md5Hash = BitConverter.ToString(md5.Hash).Replace("-", "").ToUpperInvariant();

                        // Update cache with result
                        if (audioInfoCache.TryGetValue(audioFilePath, out var cachedInfo))
                        {
                            cachedInfo.Md5Hash = md5Hash;
                            cachedInfo.ErrorDetails = null;
                        }
                        else
                        {
                            var newInfo = new AudioFileInfo
                            {
                                FilePath = audioFilePath,
                                Md5Hash = md5Hash,
                                FileName = Path.GetFileName(audioFilePath),
                                DirectoryPath = Path.GetDirectoryName(audioFilePath),
                                ErrorDetails = null
                            };
                            audioInfoCache.TryAdd(audioFilePath, newInfo);
                        }

                        return md5Hash;
                    }
                    else
                    {
                        // Skip unknown chunk
                        reader.BaseStream.Seek(chunkSize, SeekOrigin.Current);
                    }
                }

                // No "data" chunk found
                string noDataChunkError = "Invalid WAV file: No data chunk found.";
                UpdateCacheWithMD5Error(audioFilePath, noDataChunkError);
                return "MD5 calculation failed";
            }
            catch (Exception ex)
            {
                string errorMessage = $"Error calculating MD5 for WAV file: {ex.Message}";
                UpdateCacheWithMD5Error(audioFilePath, errorMessage);
                return "MD5 calculation failed";
            }
        }

        /// <summary>
        /// Calculates the MD5 hash of the decoded PCM audio from a FLAC file by decoding it to a temporary WAV.
        /// The MD5 is computed from the decoded audio to ensure bit-perfect comparison with other formats.
        /// </summary>
        /// <param name="flacFilePath">Path to the FLAC file.</param>
        /// <returns>The MD5 hash of the decoded audio, or "MD5 calculation failed" on error.</returns>
        private async Task<string> CalculateFlacMD5Async(string flacFilePath)
        {
            try
            {
                string encoderExePath = null;
                string errorMessageDetails = null;

                // Get encoder path from UI thread
                await this.InvokeAsync(() =>
                {
                    var encoderItem = listViewEncoders.Items
                        .Cast<ListViewItem>()
                        .FirstOrDefault(item => Path.GetExtension(item.Text).Equals(".exe", StringComparison.OrdinalIgnoreCase));
                    encoderExePath = encoderItem?.Tag?.ToString();
                });

                if (string.IsNullOrEmpty(encoderExePath) || !File.Exists(encoderExePath))
                {
                    errorMessageDetails = "No .exe encoder found in the list";
                    UpdateCacheWithMD5Error(flacFilePath, errorMessageDetails);
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
                        errorMessageDetails = $"Failed to create temp folder: {ex.Message}";
                        UpdateCacheWithMD5Error(flacFilePath, errorMessageDetails);
                        return "MD5 calculation failed";
                    }
                }

                // Create unique temp WAV file
                string tempWavFile = Path.Combine(tempFolderPath, $"temp_flac_md5_{Guid.NewGuid()}.wav");
                string arguments = $"\"{flacFilePath}\" -d --no-preserve-modtime --silent -f -o \"{tempWavFile}\"";

                using var process = new Process();
                process.StartInfo.FileName = encoderExePath;
                process.StartInfo.Arguments = arguments;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.CreateNoWindow = true;

                process.Start();

                string errorOutput = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (process.ExitCode != 0)
                {
                    errorMessageDetails = $"Decode failed: {errorOutput.Trim()}";
                    UpdateCacheWithMD5Error(flacFilePath, errorMessageDetails);
                    try { if (File.Exists(tempWavFile)) File.Delete(tempWavFile); } catch { }
                    return "MD5 calculation failed";
                }

                if (!File.Exists(tempWavFile))
                {
                    errorMessageDetails = "Temporary WAV file was not created";
                    UpdateCacheWithMD5Error(flacFilePath, errorMessageDetails);
                    return "MD5 calculation failed";
                }

                // Calculate MD5 of decoded WAV
                string wavMd5Result = await CalculateWavMD5Async(tempWavFile);

                // Clean up temp file
                try { File.Delete(tempWavFile); } catch { }

                if (wavMd5Result == "MD5 calculation failed")
                {
                    string tempWavErrorDetails = "Unknown error during MD5 calculation of decoded WAV";
                    if (audioInfoCache.TryGetValue(tempWavFile, out var tempInfo) && !string.IsNullOrEmpty(tempInfo.ErrorDetails))
                    {
                        tempWavErrorDetails = tempInfo.ErrorDetails;
                    }
                    UpdateCacheWithMD5Error(flacFilePath, tempWavErrorDetails);
                    return "MD5 calculation failed";
                }

                // Update cache with result
                if (audioInfoCache.TryGetValue(flacFilePath, out var cachedInfo))
                {
                    cachedInfo.Md5Hash = wavMd5Result;
                    cachedInfo.ErrorDetails = null;
                }
                else
                {
                    var newInfo = new AudioFileInfo
                    {
                        FilePath = flacFilePath,
                        Md5Hash = wavMd5Result,
                        FileName = Path.GetFileName(flacFilePath),
                        DirectoryPath = Path.GetDirectoryName(flacFilePath),
                        ErrorDetails = null
                    };
                    audioInfoCache.TryAdd(flacFilePath, newInfo);
                }

                return wavMd5Result;
            }
            catch (Exception ex)
            {
                string errorMessageDetails = $"Error: {ex.Message}";
                UpdateCacheWithMD5Error(flacFilePath, errorMessageDetails);
                return "MD5 calculation failed";
            }
        }

        /// <summary>
        /// Updates or creates an AudioFileInfo entry in the cache with an MD5 error status.
        /// Ensures consistent error reporting across the application.
        /// </summary>
        /// <param name="filePath">Path to the file being processed.</param>
        /// <param name="errorDetails">Detailed error message to store.</param>
        private void UpdateCacheWithMD5Error(string filePath, string errorDetails)
        {
            if (audioInfoCache.TryGetValue(filePath, out var cachedInfo))
            {
                cachedInfo.Md5Hash = "MD5 calculation failed";
                cachedInfo.ErrorDetails = errorDetails;
            }
            else
            {
                var newInfo = new AudioFileInfo
                {
                    FilePath = filePath,
                    Md5Hash = "MD5 calculation failed",
                    FileName = Path.GetFileName(filePath),
                    DirectoryPath = Path.GetDirectoryName(filePath),
                    ErrorDetails = errorDetails
                };
                audioInfoCache.TryAdd(filePath, newInfo);
            }
        }

        private async void buttonDetectDupesAudioFiles_Click(object? sender, EventArgs e)
        {
            var button = (Button)sender;
            var originalText = button.Text;
            var cts = new CancellationTokenSource();

            // Declare variables for summary message
            Dictionary<string, List<string>> hashDict = null;
            List<string> filesWithMD5Errors = null;

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
                this.Invoke((MethodInvoker)delegate
                {
                    foreach (ListViewItem item in listViewAudioFiles.Items)
                    {
                        string filePath = item.Tag?.ToString() ?? string.Empty;
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
                        ShowTemporaryAudioFileRemovedMessage($"{itemsToRemove.Count} file(s) were not found on disk and have been removed from the list.");
                    }
                });

                if (listViewAudioFiles.Items.Count == 0)
                {
                    MessageBox.Show("No audio files to process.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // --- STAGE 0.2: COLLECT FILE PATHS (on UI thread) ---
                var filePaths = new List<string>();
                this.Invoke((MethodInvoker)delegate
                {
                    filePaths.AddRange(listViewAudioFiles.Items.Cast<ListViewItem>().Select(item => item.Tag.ToString()));
                });

                // --- STAGE 1: PERFORM DUPLICATE DETECTION IN BACKGROUND THREAD ---
                await Task.Run(async () =>
                {
                    hashDict = new Dictionary<string, List<string>>(); // Group files by MD5 hash.
                    filesWithMD5Errors = new List<string>(); // Track paths of files with MD5 errors.
                    var itemsToCheck = new List<string>();   // Paths of files to mark as checked (primary).
                    var itemsToUncheck = new List<string>(); // Paths of files to mark as unchecked (non-primary duplicates).

                    // --- STAGE 1.1: CALCULATE OR RETRIEVE MD5 HASHES ---
                    foreach (string filePath in filePaths)
                    {
                        if (cts.Token.IsCancellationRequested)
                            return;

                        string md5Hash = audioInfoCache.TryGetValue(filePath, out var info) ? info.Md5Hash : null;

                        if (string.IsNullOrEmpty(md5Hash) ||
                            md5Hash == "MD5 calculation failed" ||
                            md5Hash == "00000000000000000000000000000000" ||
                            md5Hash == "N/A")
                        {
                            string fileExtension = Path.GetExtension(filePath).ToLowerInvariant();
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

                            // Update the cache with the new MD5 hash — preserve existing object
                            if (audioInfoCache.TryGetValue(filePath, out var infoToUpdate))
                            {
                                infoToUpdate.Md5Hash = md5Hash;
                                // Do NOT replace the object — preserve other properties (ErrorDetails, LastWriteTime, etc.)
                            }
                            else
                            {
                                // Create new cache entry if it doesn't exist
                                var newInfo = new AudioFileInfo
                                {
                                    FilePath = filePath,
                                    Md5Hash = md5Hash,
                                    FileName = Path.GetFileName(filePath),
                                    DirectoryPath = Path.GetDirectoryName(filePath),
                                    Extension = fileExtension
                                };
                                audioInfoCache.TryAdd(filePath, newInfo);
                            }
                        }

                        if (!string.IsNullOrEmpty(md5Hash) && md5Hash != "MD5 calculation failed")
                        {
                            if (!hashDict.ContainsKey(md5Hash))
                                hashDict[md5Hash] = new List<string>();
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
                            .Select(path => new { Path = path, Info = audioInfoCache.TryGetValue(path, out var info) ? info : null })
                            .Where(x => x.Info != null)
                            .OrderBy(x => x.Info.Extension == ".flac" ? 0 : 1)          // FLAC > WAV
                            .ThenBy(x => x.Info.DirectoryPath?.Length ?? int.MaxValue)  // Shorter path first
                            .ThenByDescending(x => x.Info.LastWriteTime)                // Newer first
                            .ThenBy(x => x.Path)                                        // Then by path
                            .ToList();

                        if (sortedPaths.Count > 0)
                        {
                            itemsToCheck.Add(sortedPaths[0].Path); // Primary file
                            itemsToUncheck.AddRange(sortedPaths.Skip(1).Select(x => x.Path)); // Others
                        }
                    }

                    // --- STAGE 2: UPDATE USER INTERFACE (on UI thread) ---
                    this.Invoke((MethodInvoker)delegate
                    {
                        // --- STAGE 2.1: CLEAR PREVIOUS RESULTS FROM LOG ---
                        for (int i = dataGridViewLogDetectDupes.Rows.Count - 1; i >= 0; i--)
                        {
                            DataGridViewRow row = dataGridViewLogDetectDupes.Rows[i];
                            if (row.Cells["MD5"].Value?.ToString() == "MD5 calculation failed" ||
                                !string.IsNullOrEmpty(row.Cells["Duplicates"].Value?.ToString()))
                            {
                                dataGridViewLogDetectDupes.Rows.RemoveAt(i);
                            }
                        }

                        // --- STAGE 2.2: UPDATE CHECKBOX STATES IN LISTVIEW ---
                        foreach (ListViewItem item in listViewAudioFiles.Items)
                        {
                            string path = item.Tag.ToString();
                            item.Checked = !itemsToUncheck.Contains(path); // Uncheck non-primary duplicates
                        }

                        // --- STAGE 2.3: UPDATE MD5 DISPLAY IN LISTVIEW ---
                        foreach (ListViewItem item in listViewAudioFiles.Items)
                        {
                            string path = item.Tag.ToString();
                            if (audioInfoCache.TryGetValue(path, out var info))
                            {
                                if (item.SubItems.Count > 5 && item.SubItems[5].Text != info.Md5Hash)
                                {
                                    item.SubItems[5].Text = info.Md5Hash;
                                }
                            }
                        }

                        // --- STAGE 2.4: LOG MD5 CALCULATION ERROR RESULTS ---
                        foreach (string filePath in filesWithMD5Errors)
                        {
                            if (audioInfoCache.TryGetValue(filePath, out var info))
                            {
                                int rowIndex = dataGridViewLogDetectDupes.Rows.Add(
                                    info.FileName, "", "", "", "", "", "", "", "", "", "",
                                    "", "", "", "", "", "", "", "", "", "", "", info.DirectoryPath,
                                    "MD5 calculation failed", "", info.ErrorDetails ?? string.Empty
                                );
                                dataGridViewLogDetectDupes.Rows[rowIndex].DefaultCellStyle.ForeColor = Color.Gray;
                            }
                        }

                        // --- STAGE 2.5: LOG DUPLICATE GROUPS ---
                        foreach (var kvp in hashDict.Where(g => g.Value.Count > 1))
                        {
                            string duplicatesList = string.Join(", ", kvp.Value.Select(path =>
                                audioInfoCache.TryGetValue(path, out var i) ? i.FileName : Path.GetFileName(path)));

                            foreach (string path in kvp.Value)
                            {
                                if (audioInfoCache.TryGetValue(path, out var info))
                                {
                                    int rowIndex = dataGridViewLogDetectDupes.Rows.Add(
                                        info.FileName, "", "", "", "", "", "", "", "", "", "",
                                        "", "", "", "", "", "", "", "", "", "", "", info.DirectoryPath,
                                        kvp.Key, duplicatesList, ""
                                    );
                                    dataGridViewLogDetectDupes.Rows[rowIndex].DefaultCellStyle.ForeColor = Color.Brown;
                                }
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
                                var groupItems = allItems.Where(item => group.Group.Contains(item.Tag.ToString())).ToList();
                                var primaryItem = groupItems.FirstOrDefault(item => item.Tag.ToString() == group.Primary);
                                var otherItems = groupItems.Where(item => item != primaryItem).ToList();

                                if (primaryItem != null)
                                    listViewAudioFiles.Items.Add(primaryItem);
                                foreach (var item in otherItems)
                                    listViewAudioFiles.Items.Add(item);
                            }

                            // Add non-duplicate items — only those NOT in any duplicate group
                            var duplicatePaths = duplicateGroups.SelectMany(g => g.Group).ToHashSet();
                            var nonDuplicateItems = allItems.Where(item => !duplicatePaths.Contains(item.Tag.ToString()));
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
                // Operation was cancelled — exit silently
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
                    this.Invoke((MethodInvoker)(() =>
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
        private async void buttonTestForErrors_Click(object? sender, EventArgs e)
        {
            var button = (Button)sender;
            var originalText = button.Text;
            var cts = new CancellationTokenSource();

            try
            {
                button.Text = "In progress...";
                button.Enabled = false;

                // --- STAGE 1: COLLECT DATA FROM UI ---
                var (flacFilePaths, encoderPath, useWarningsAsErrors) = await Task.Run(() =>
                {
                    List<string> flacFilePaths = new List<string>();
                    string encoderPath = null;
                    bool useWarningsAsErrors = false;

                    this.Invoke((MethodInvoker)delegate
                    {
                        // Clear previous results
                        for (int i = dataGridViewLogTestForErrors.Rows.Count - 1; i >= 0; i--)
                        {
                            if (dataGridViewLogTestForErrors.Rows[i].Cells["MD5"].Value?.ToString() == "Integrity Check Failed")
                                dataGridViewLogTestForErrors.Rows.RemoveAt(i);
                        }

                        // Remove missing files
                        var itemsToRemove = listViewAudioFiles.Items.Cast<ListViewItem>()
                            .Where(item => !File.Exists(item.Tag.ToString())).ToList();

                        foreach (var item in itemsToRemove)
                            listViewAudioFiles.Items.Remove(item);

                        if (itemsToRemove.Count > 0)
                        {
                            UpdateGroupBoxAudioFilesHeader();
                            ShowTemporaryAudioFileRemovedMessage($"{itemsToRemove.Count} file(s) removed");
                        }

                        // Collect FLAC files and settings
                        flacFilePaths.AddRange(listViewAudioFiles.Items.Cast<ListViewItem>()
                            .Where(item => Path.GetExtension(item.Tag.ToString()).Equals(".flac", StringComparison.OrdinalIgnoreCase))
                            .Select(item => item.Tag.ToString()));

                        encoderPath = listViewEncoders.Items
                            .Cast<ListViewItem>()
                            .FirstOrDefault(item => Path.GetExtension(item.Text).Equals(".exe", StringComparison.OrdinalIgnoreCase))
                            ?.Tag?.ToString();

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
                        string fileName = audioInfoCache.TryGetValue(filePath, out var info) ? info.FileName : Path.GetFileName(filePath);

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
                            string message = string.IsNullOrWhiteSpace(errorOutput)
                                ? "Unknown error (non-zero exit code)"
                                : errorOutput.Trim();

                            errorResults.Add((fileName, filePath, message));
                        }
                    }
                    catch (Exception ex) when (!(ex is OperationCanceledException))
                    {
                        errorResults.Add((Path.GetFileName(filePath), filePath, $"Process failed: {ex.Message}"));
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }));

                if (cts.Token.IsCancellationRequested) return;

                // --- STAGE 3: UPDATE UI ---
                await this.InvokeAsync(() =>
                {
                    dataGridViewLogTestForErrors.SuspendLayout();
                    try
                    {
                        var rowsToAdd = errorResults.Select(result =>
                        {
                            string directoryPath = audioInfoCache.TryGetValue(result.FilePath, out var info)
                                ? info.DirectoryPath : Path.GetDirectoryName(result.FilePath);

                            var row = new DataGridViewRow();
                            row.CreateCells(dataGridViewLogTestForErrors);
                            row.SetValues(
                                result.FileName, "", "", "", "", "", "", "", "", "", "",
                                "", "", "", "", "", "", "", "", "", "", "", directoryPath,
                                "Integrity Check Failed", "", result.Message
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
                        errorResults.Count == 0
                            ? "All FLAC files passed the integrity test."
                            : $"{errorResults.Count} FLAC file(s) failed the integrity test.",
                        "Test Complete",
                        MessageBoxButtons.OK,
                        errorResults.Count == 0 ? MessageBoxIcon.Information : MessageBoxIcon.Warning
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

        private void buttonUpAudioFile_Click(object? sender, EventArgs e)
        {
            MoveSelectedItems(listViewAudioFiles, -1); // Pass -1 to move up
        }
        private void buttonDownAudioFile_Click(object? sender, EventArgs e)
        {
            MoveSelectedItems(listViewAudioFiles, 1); // Pass 1 to move down
        }
        private void buttonRemoveAudioFile_Click(object? sender, EventArgs e)
        {
            // Remove selected items from listViewAudioFiles
            for (int i = listViewAudioFiles.Items.Count - 1; i >= 0; i--)
            {
                if (listViewAudioFiles.Items[i].Selected) // Check if the item is selected
                {
                    listViewAudioFiles.Items.RemoveAt(i); // Remove the item
                }
            }
            UpdateGroupBoxAudioFilesHeader();
        }
        private void buttonClearUnchecked_Click(object? sender, EventArgs e)
        {
            // Check if the Shift key is pressed
            if (ModifierKeys == Keys.Shift)
            {
                MoveUncheckedToRecycleBin();
            }
            else
            {
                // Create a list to remember the indices of unchecked items
                List<int> itemsToRemove = new List<int>();

                // Iterate through the list items and add unchecked items to the removal list
                for (int i = 0; i < listViewAudioFiles.Items.Count; i++)
                {
                    if (!listViewAudioFiles.Items[i].Checked)
                    {
                        itemsToRemove.Add(i); // Store the index of the unchecked item
                    }
                }

                // Remove items starting from the end of the list to avoid index shifting
                for (int i = itemsToRemove.Count - 1; i >= 0; i--)
                {
                    listViewAudioFiles.Items.RemoveAt(itemsToRemove[i]); // Remove the item
                }
            }
            UpdateGroupBoxAudioFilesHeader();
        }
        private void MoveUncheckedToRecycleBin()
        {
            var itemsToRemove = new List<string>();

            // Gather the paths of unchecked items
            foreach (ListViewItem item in listViewAudioFiles.Items)
            {
                if (!item.Checked)
                {
                    itemsToRemove.Add(item.Tag.ToString()); // Add the file path for removal
                }
            }

            // If there are no unchecked items, show a message and return
            if (itemsToRemove.Count == 0)
            {
                MessageBox.Show("There are no unchecked audio files to delete.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Ask for user confirmation
            var result = MessageBox.Show("Are you sure you want to move all unchecked files to the recycle bin?", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                // Move the files to the recycle bin
                foreach (var file in itemsToRemove)
                {
                    Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(file, Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs, Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin);
                }

                // Remove entries from ListView
                foreach (string file in itemsToRemove)
                {
                    var itemToRemove = listViewAudioFiles.Items.Cast<ListViewItem>().FirstOrDefault(i => i.Tag.ToString() == file);
                    if (itemToRemove != null)
                    {
                        listViewAudioFiles.Items.Remove(itemToRemove);
                    }
                }
                UpdateGroupBoxAudioFilesHeader();
                MessageBox.Show("Unchecked audio files have been moved to the recycle bin.", "Deletion", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        private void buttonClearAudioFiles_Click(object? sender, EventArgs e)
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

        // Log Stettings
        private DataGridViewLogSettingsForm? _logSettingsForm = null;
        private void buttonDataGridViewLogSettings_Click(object sender, EventArgs e)
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

        // Log Benchmark
        private void InitializedataGridViewLog()
        {
            // Configure DataGridView
            dataGridViewLog.Columns.Add("Name", "Name");
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
            dataGridViewLog.Columns["BitDepth"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewLog.Columns["SamplingRate"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewLog.Columns["InputFileSize"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewLog.Columns["OutputFileSize"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewLog.Columns["Compression"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewLog.Columns["Time"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewLog.Columns["Speed"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewLog.Columns["SpeedMin"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewLog.Columns["SpeedMax"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewLog.Columns["SpeedRange"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewLog.Columns["SpeedConsistency"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewLog.Columns["CPULoadEncoder"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewLog.Columns["CPUClock"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewLog.Columns["Passes"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

            // Hide optional columns by default
            dataGridViewLog.Columns["Name"].Visible = true;
            dataGridViewLog.Columns["BitDepth"].Visible = true;
            dataGridViewLog.Columns["SamplingRate"].Visible = true;
            dataGridViewLog.Columns["InputFileSize"].Visible = true;
            dataGridViewLog.Columns["OutputFileSize"].Visible = true;
            dataGridViewLog.Columns["Compression"].Visible = true;
            dataGridViewLog.Columns["Time"].Visible = true;
            dataGridViewLog.Columns["Speed"].Visible = true;
            dataGridViewLog.Columns["SpeedMin"].Visible = true;
            dataGridViewLog.Columns["SpeedMax"].Visible = true;
            dataGridViewLog.Columns["SpeedRange"].Visible = true;
            dataGridViewLog.Columns["SpeedConsistency"].Visible = true;
            dataGridViewLog.Columns["CPULoadEncoder"].Visible = true;
            dataGridViewLog.Columns["CPUClock"].Visible = true;
            dataGridViewLog.Columns["Passes"].Visible = true;
            dataGridViewLog.Columns["Parameters"].Visible = true;
            dataGridViewLog.Columns["Encoder"].Visible = true;
            dataGridViewLog.Columns["Version"].Visible = true;
            dataGridViewLog.Columns["EncoderDirectory"].Visible = true;
            dataGridViewLog.Columns["FastestEncoder"].Visible = true;
            dataGridViewLog.Columns["BestSize"].Visible = true;
            dataGridViewLog.Columns["SameSize"].Visible = true;
            dataGridViewLog.Columns["AudioFileDirectory"].Visible = true;
            dataGridViewLog.Columns["MD5"].Visible = true;
            dataGridViewLog.Columns["Duplicates"].Visible = false;
            dataGridViewLog.Columns["Errors"].Visible = false;

            foreach (DataGridViewColumn column in dataGridViewLog.Columns)
            {
                column.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }
        }
        private void dataGridViewLog_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            // Prevent interaction with the new row or invalid cell
            if (e.RowIndex < 0) return;

            string columnName = dataGridViewLog.Columns[e.ColumnIndex].Name;

            // 1. Handle click on "AudioFileDirectory" column - path to the audio file directory
            if (columnName == "AudioFileDirectory")
            {
                string directoryPath = dataGridViewLog.Rows[e.RowIndex].Cells["AudioFileDirectory"].Value?.ToString();
                string fileName = dataGridViewLog.Rows[e.RowIndex].Cells["Name"].Value?.ToString();

                if (!string.IsNullOrEmpty(directoryPath) &&
                    !string.IsNullOrEmpty(fileName))
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
                string directoryPath = dataGridViewLog.Rows[e.RowIndex].Cells["EncoderDirectory"].Value?.ToString();
                string encoderFileName = dataGridViewLog.Rows[e.RowIndex].Cells["Encoder"].Value?.ToString();

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
        private void dataGridViewLog_MouseDown(object sender, MouseEventArgs e)
        {
            var hitTest = dataGridViewLog.HitTest(e.X, e.Y);
            if (hitTest.RowIndex == -1 && hitTest.ColumnIndex == -1)
            {
                dataGridViewLog.ClearSelection();
            }
        }

        // Log Detect Dupes
        private void InitializedataGridViewLogDetectDupes()
        {
            // Configure DataGridView
            dataGridViewLogDetectDupes.Columns.Add("Name", "Name");
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
            dataGridViewLogDetectDupes.Columns["BitDepth"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewLogDetectDupes.Columns["SamplingRate"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewLogDetectDupes.Columns["InputFileSize"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewLogDetectDupes.Columns["OutputFileSize"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewLogDetectDupes.Columns["Compression"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewLogDetectDupes.Columns["Time"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewLogDetectDupes.Columns["Speed"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewLogDetectDupes.Columns["SpeedMin"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewLogDetectDupes.Columns["SpeedMax"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewLogDetectDupes.Columns["SpeedRange"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewLogDetectDupes.Columns["SpeedConsistency"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewLogDetectDupes.Columns["CPULoadEncoder"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewLogDetectDupes.Columns["CPUClock"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewLogDetectDupes.Columns["Passes"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

            // Hide or show columns by default
            dataGridViewLogDetectDupes.Columns["Name"].Visible = true;
            dataGridViewLogDetectDupes.Columns["BitDepth"].Visible = false;
            dataGridViewLogDetectDupes.Columns["SamplingRate"].Visible = false;
            dataGridViewLogDetectDupes.Columns["InputFileSize"].Visible = false;
            dataGridViewLogDetectDupes.Columns["OutputFileSize"].Visible = false;
            dataGridViewLogDetectDupes.Columns["Compression"].Visible = false;
            dataGridViewLogDetectDupes.Columns["Time"].Visible = false;
            dataGridViewLogDetectDupes.Columns["Speed"].Visible = false;
            dataGridViewLogDetectDupes.Columns["SpeedMin"].Visible = false;
            dataGridViewLogDetectDupes.Columns["SpeedMax"].Visible = false;
            dataGridViewLogDetectDupes.Columns["SpeedRange"].Visible = false;
            dataGridViewLogDetectDupes.Columns["SpeedConsistency"].Visible = false;
            dataGridViewLogDetectDupes.Columns["CPULoadEncoder"].Visible = false;
            dataGridViewLogDetectDupes.Columns["CPUClock"].Visible = false;
            dataGridViewLogDetectDupes.Columns["Passes"].Visible = false;
            dataGridViewLogDetectDupes.Columns["Parameters"].Visible = false;
            dataGridViewLogDetectDupes.Columns["Encoder"].Visible = false;
            dataGridViewLogDetectDupes.Columns["Version"].Visible = false;
            dataGridViewLogDetectDupes.Columns["EncoderDirectory"].Visible = false;
            dataGridViewLogDetectDupes.Columns["FastestEncoder"].Visible = false;
            dataGridViewLogDetectDupes.Columns["BestSize"].Visible = false;
            dataGridViewLogDetectDupes.Columns["SameSize"].Visible = false;
            dataGridViewLogDetectDupes.Columns["AudioFileDirectory"].Visible = true;
            dataGridViewLogDetectDupes.Columns["MD5"].Visible = true;
            dataGridViewLogDetectDupes.Columns["Duplicates"].Visible = true;
            dataGridViewLogDetectDupes.Columns["Errors"].Visible = true;

            foreach (DataGridViewColumn column in dataGridViewLogDetectDupes.Columns)
            {
                column.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }
        }
        private void dataGridViewLogDetectDupes_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            // Prevent interaction with the new row or invalid cell
            if (e.RowIndex < 0) return;

            string columnName = dataGridViewLogDetectDupes.Columns[e.ColumnIndex].Name;

            // 1. Handle click on "AudioFileDirectory" column - path to the audio file directory
            if (columnName == "AudioFileDirectory")
            {
                string directoryPath = dataGridViewLogDetectDupes.Rows[e.RowIndex].Cells["AudioFileDirectory"].Value?.ToString();
                string fileName = dataGridViewLogDetectDupes.Rows[e.RowIndex].Cells["Name"].Value?.ToString();

                if (!string.IsNullOrEmpty(directoryPath) &&
                    !string.IsNullOrEmpty(fileName))
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
                string directoryPath = dataGridViewLogDetectDupes.Rows[e.RowIndex].Cells["EncoderDirectory"].Value?.ToString();
                string encoderFileName = dataGridViewLogDetectDupes.Rows[e.RowIndex].Cells["Encoder"].Value?.ToString();

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
        private void dataGridViewLogDetectDupes_MouseDown(object sender, MouseEventArgs e)
        {
            var hitTest = dataGridViewLogDetectDupes.HitTest(e.X, e.Y);
            if (hitTest.RowIndex == -1 && hitTest.ColumnIndex == -1)
            {
                dataGridViewLogDetectDupes.ClearSelection();
            }
        }

        // Log Test for Errors
        private void InitializedataGridViewLogTestForErrors()
        {
            // Configure DataGridView
            dataGridViewLogTestForErrors.Columns.Add("Name", "Name");
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
            dataGridViewLogTestForErrors.Columns["BitDepth"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewLogTestForErrors.Columns["SamplingRate"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewLogTestForErrors.Columns["InputFileSize"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewLogTestForErrors.Columns["OutputFileSize"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewLogTestForErrors.Columns["Compression"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewLogTestForErrors.Columns["Time"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewLogTestForErrors.Columns["Speed"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewLogTestForErrors.Columns["SpeedMin"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewLogTestForErrors.Columns["SpeedMax"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewLogTestForErrors.Columns["SpeedRange"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewLogTestForErrors.Columns["SpeedConsistency"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewLogTestForErrors.Columns["CPULoadEncoder"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewLogTestForErrors.Columns["CPUClock"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewLogTestForErrors.Columns["Passes"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

            // Hide or show columns by default
            dataGridViewLogTestForErrors.Columns["Name"].Visible = true;
            dataGridViewLogTestForErrors.Columns["BitDepth"].Visible = false;
            dataGridViewLogTestForErrors.Columns["SamplingRate"].Visible = false;
            dataGridViewLogTestForErrors.Columns["InputFileSize"].Visible = false;
            dataGridViewLogTestForErrors.Columns["OutputFileSize"].Visible = false;
            dataGridViewLogTestForErrors.Columns["Compression"].Visible = false;
            dataGridViewLogTestForErrors.Columns["Time"].Visible = false;
            dataGridViewLogTestForErrors.Columns["Speed"].Visible = false;
            dataGridViewLogTestForErrors.Columns["SpeedMin"].Visible = false;
            dataGridViewLogTestForErrors.Columns["SpeedMax"].Visible = false;
            dataGridViewLogTestForErrors.Columns["SpeedRange"].Visible = false;
            dataGridViewLogTestForErrors.Columns["SpeedConsistency"].Visible = false;
            dataGridViewLogTestForErrors.Columns["CPULoadEncoder"].Visible = false;
            dataGridViewLogTestForErrors.Columns["CPUClock"].Visible = false;
            dataGridViewLogTestForErrors.Columns["Passes"].Visible = false;
            dataGridViewLogTestForErrors.Columns["Parameters"].Visible = false;
            dataGridViewLogTestForErrors.Columns["Encoder"].Visible = false;
            dataGridViewLogTestForErrors.Columns["Version"].Visible = false;
            dataGridViewLogTestForErrors.Columns["EncoderDirectory"].Visible = false;
            dataGridViewLogTestForErrors.Columns["FastestEncoder"].Visible = false;
            dataGridViewLogTestForErrors.Columns["BestSize"].Visible = false;
            dataGridViewLogTestForErrors.Columns["SameSize"].Visible = false;
            dataGridViewLogTestForErrors.Columns["AudioFileDirectory"].Visible = true;
            dataGridViewLogTestForErrors.Columns["MD5"].Visible = true;
            dataGridViewLogTestForErrors.Columns["Duplicates"].Visible = false;
            dataGridViewLogTestForErrors.Columns["Errors"].Visible = true;

            foreach (DataGridViewColumn column in dataGridViewLogTestForErrors.Columns)
            {
                column.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }
        }
        private void dataGridViewLogTestForErrors_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            // Prevent interaction with the new row or invalid cell
            if (e.RowIndex < 0) return;

            string columnName = dataGridViewLogTestForErrors.Columns[e.ColumnIndex].Name;

            // 1. Handle click on "AudioFileDirectory" column - path to the audio file directory
            if (columnName == "AudioFileDirectory")
            {
                string directoryPath = dataGridViewLogTestForErrors.Rows[e.RowIndex].Cells["AudioFileDirectory"].Value?.ToString();
                string fileName = dataGridViewLogTestForErrors.Rows[e.RowIndex].Cells["Name"].Value?.ToString();

                if (!string.IsNullOrEmpty(directoryPath) &&
                    !string.IsNullOrEmpty(fileName))
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
                string directoryPath = dataGridViewLogTestForErrors.Rows[e.RowIndex].Cells["EncoderDirectory"].Value?.ToString();
                string encoderFileName = dataGridViewLogTestForErrors.Rows[e.RowIndex].Cells["Encoder"].Value?.ToString();

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
        private void dataGridViewLogTestForErrors_MouseDown(object sender, MouseEventArgs e)
        {
            var hitTest = dataGridViewLogTestForErrors.HitTest(e.X, e.Y);
            if (hitTest.RowIndex == -1 && hitTest.ColumnIndex == -1)
            {
                dataGridViewLogTestForErrors.ClearSelection();
            }
        }

        private async Task LogProcessResults(string outputFilePath, string audioFilePath, string parameters, string encoder, TimeSpan elapsedTime, TimeSpan userProcessorTime, TimeSpan privilegedProcessorTime, double avgClock)
        {
            FileInfo outputFile = new FileInfo(outputFilePath);
            if (!outputFile.Exists)
                return;

            // Create CultureInfo for formatting with spaces as thousand separators
            NumberFormatInfo numberFormat = new CultureInfo("en-US").NumberFormat;
            numberFormat.NumberGroupSeparator = " ";

            // Get input audio file information from cache
            var audioFileInfo = await GetAudioInfo(audioFilePath);

            // Extract data from cache
            string audioFileName = audioFileInfo.FileName; // Use file name from cache
            long samplingRate = long.TryParse(audioFileInfo.SamplingRate, out long temp) ? temp : 0;
            string samplingRateFormatted = samplingRate.ToString("N0", numberFormat);
            long inputSize = audioFileInfo.FileSize; // Get size from file info
            string inputSizeFormatted = inputSize.ToString("N0", numberFormat);
            long durationMs = Convert.ToInt64(audioFileInfo.Duration); // Use duration from cache
            string audioFileDirectory = audioFileInfo.DirectoryPath;
            string Md5Hash = audioFileInfo.Md5Hash;

            // Form short name for input file
            //string audioFileNameShort = audioFileName.Length > 30
            //    ? $"{audioFileName.Substring(0, 15)}...{audioFileName.Substring(audioFileName.Length - 15)}"
            //    : audioFileName.PadRight(33);

            // Get output audio file information
            long outputSize = outputFile.Length;
            string outputSizeFormatted = outputSize.ToString("N0", numberFormat);

            double compressionPercentage = ((double)outputSize / inputSize) * 100;
            double encodingSpeed = (double)durationMs / elapsedTime.TotalMilliseconds;

            // Get encoder information from cache
            var encoderInfo = await GetEncoderInfo(encoder); // Get encoder info

            // Calculate CPU Load
            double totalCpuTime = (userProcessorTime + privilegedProcessorTime).TotalMilliseconds;
            double cpuLoadEncoder = elapsedTime.TotalMilliseconds > 0 ? (totalCpuTime / elapsedTime.TotalMilliseconds) * 100 : 0;

            // Create benchmark pass object FIRST
            var benchmarkPass = new BenchmarkPass
            {
                AudioFilePath = audioFilePath,
                EncoderPath = encoder,
                Parameters = parameters,
                InputSize = inputSize,
                OutputSize = outputSize,
                Time = elapsedTime.TotalMilliseconds,
                Speed = encodingSpeed,
                CPULoadEncoder = cpuLoadEncoder,
                CPUClock = avgClock,
                BitDepth = audioFileInfo.BitDepth,
                SamplingRate = audioFileInfo.SamplingRate,
                Timestamp = DateTime.Now
            };

            // Add raw data of the Pass to cache
            _benchmarkPasses.Add(benchmarkPass);

            // Add record to DataGridView log
            int rowIndex = dataGridViewLog.Rows.Add(
                audioFileName,                          //  0 "Name"
                audioFileInfo.BitDepth,                 //  1 "BitDepth"
                samplingRateFormatted,                  //  2 "SamplingRate"
                inputSizeFormatted,                     //  3 "InputFileSize"
                outputSizeFormatted,                    //  4 "OutputFileSize"
                $"{compressionPercentage:F3}%",         //  5 "Compression"
                $"{elapsedTime.TotalMilliseconds:F3}",  //  6 "Time"
                $"{encodingSpeed:F3}x",                 //  7 "Speed"
                string.Empty,                           //  8 "SpeedMin"
                string.Empty,                           //  9 "SpeedMax"
                string.Empty,                           // 10 "SpeedRange"
                string.Empty,                           // 11 "SpeedConsistency"
                $"{cpuLoadEncoder:F3}%",                // 12 "CPULoadEncoder"
                $"{avgClock:F0} MHz",                   // 13 "CPUClock"
                "1",                                    // 14 "Passes"
                parameters,                             // 15 "Parameters"
                encoderInfo.FileName,                   // 16 "Encoder"
                encoderInfo.Version,                    // 17 "Version"
                encoderInfo.DirectoryPath,              // 18 "EncoderDirectory"
                string.Empty,                           // 19 "FastestEncoder"
                string.Empty,                           // 20 "BestSize"
                string.Empty,                           // 21 "SameSize"
                audioFileDirectory,                     // 22 "AudioFileDirectory"
                Md5Hash,                                // 23 "MD5"
                string.Empty,                           // 24 "Duplicates"
                string.Empty                            // 25 "Errors"
            );

            // Add a tag to Log raw
            dataGridViewLog.Rows[rowIndex].Tag = benchmarkPass;

            // Set text color based on file size comparison
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

            // Log to file
            File.AppendAllText("log.txt",
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
        private void buttonLogColumnsAutoWidth_Click(object sender, EventArgs e)
        {
            bool autoWidthAllTabs = ModifierKeys.HasFlag(Keys.Shift);

            if (autoWidthAllTabs)
            {
                // Auto-resize columns in ALL three DataGridViews
                dataGridViewLog.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells);
                dataGridViewLogDetectDupes.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells);
                dataGridViewLogTestForErrors.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells);
            }
            else
            {
                // Auto-resize only the currently selected tab
                DataGridView activeGrid = tabControlLog.SelectedTab switch
                {
                    _ when tabControlLog.SelectedTab == Benchmark => dataGridViewLog,
                    _ when tabControlLog.SelectedTab == DetectDupes => dataGridViewLogDetectDupes,
                    _ when tabControlLog.SelectedTab == TestForErrors => dataGridViewLogTestForErrors,
                    _ => null
                };

                if (activeGrid != null)
                {
                    activeGrid.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells);
                }
            }
        }
        private async void buttonAnalyzeLog_Click(object? sender, EventArgs e)
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
                    AvgCPUClock = g.Where(p => p.CPUClock > 0).Any()
                        ? g.Where(p => p.CPUClock > 0).Average(p => p.CPUClock)
                        : 0,
                    MinOutputSize = g.Min(p => p.OutputSize),
                    MaxOutputSize = g.Max(p => p.OutputSize),
                    InputSize = g.First().InputSize,
                    BitDepth = g.First().BitDepth,
                    SamplingRate = g.First().SamplingRate,
                    Speeds = g.Where(p => p.Speed > 0).Select(p => p.Speed).OrderBy(s => s).ToList(), // Extract sorted speeds once
                    LatestPass = g.OrderByDescending(p => p.Timestamp).First() // Latest pass for final size
                })
                .ToList();

            var resultEntries = new List<LogEntry>();

            foreach (var group in grouped)
            {
                // Get file and encoder info for display
                var audioFileInfo = await GetAudioInfo(group.AudioFilePath);
                var encoderInfo = await GetEncoderInfo(group.EncoderPath);

                // Configure number format with space as thousand separator (e.g. 1 234 567)
                NumberFormatInfo numberFormat = new CultureInfo("en-US").NumberFormat;
                numberFormat.NumberGroupSeparator = " ";

                string inputSizeFormatted = group.InputSize.ToString("N0", numberFormat);

                // Use the final OutputSize (after metaflac used or not) for analysis
                long outputSizeFinal = group.LatestPass.OutputSize;
                string outputSizeFormatted = outputSizeFinal.ToString("N0", numberFormat);

                // Calculate Compression using the final output size
                double compressionPercentage = ((double)outputSizeFinal / group.InputSize) * 100;

                // Format SamplingRate for display (e.g. 44100 -> "44 100")
                string samplingRateFormatted = long.TryParse(group.SamplingRate, out long sr) ? sr.ToString("N0", numberFormat) : "N/A";

                // Determine output file directory
                string audioFileDirectory = audioFileInfo?.DirectoryPath ?? Path.GetDirectoryName(group.AudioFilePath) ?? string.Empty;

                // --- Speed Stability Analysis ---
                var speeds = group.Speeds; // Use pre-extracted list

                string speedMin = "", speedMax = "", speedRange = "", speedConsistency = "";

                if (group.PassesCount > 1 && speeds.Any())
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
                    Name = audioFileInfo?.FileName ?? Path.GetFileName(group.AudioFilePath),
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
                    Encoder = encoderInfo?.FileName ?? Path.GetFileName(group.EncoderPath),
                    Version = encoderInfo?.Version ?? "N/A",
                    EncoderDirectory = encoderInfo?.DirectoryPath ?? Path.GetDirectoryName(group.EncoderPath) ?? string.Empty,
                    AudioFileDirectory = audioFileDirectory,
                    MD5 = audioFileInfo?.Md5Hash ?? "N/A",

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
                double maxSpeed = entries.Max(e => double.TryParse(e.Speed?.Replace("x", "").Trim(), out double s) ? s : 0.0);
                foreach (var entry in entries)
                {
                    if (double.TryParse(entry.Speed?.Replace("x", "").Trim(), out double speed) && speed >= maxSpeed - 0.01)
                        entry.FastestEncoder = "fastest encoder";
                }

                // Analyze output file sizes
                var validEntries = entries
                    .Where(e => long.TryParse(e.OutputFileSize?.Replace(" ", "").Trim(), out long size) && size > 0)
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

                double maxSpeed = group.Max(e => double.TryParse(e.Speed?.Replace("x", "").Trim(), out double s) ? s : 0.0);
                foreach (var entry in group)
                {
                    if (double.TryParse(entry.Speed?.Replace("x", "").Trim(), out double speed) && speed >= maxSpeed - 0.01)
                        entry.FastestEncoder = "fastest decoder";
                }
            }

            // 5. Merge results
            var finalEntries = finalEncodeEntries.Concat(decodeGroups).ToList();

            // 6. Update UI
            await this.InvokeAsync(() =>
            {
                dataGridViewLog.Rows.Clear();
                foreach (var entry in finalEntries)
                {
                    int rowIndex = dataGridViewLog.Rows.Add(
                        entry.Name,
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

                SortDataGridView(); // Resort after analysis
                tabControlLog.SelectedTab = Benchmark; // Ensure Benchmark tab is active after analysis
            });
        }
        private class LogEntry
        {
            public string Name { get; set; }
            public string BitDepth { get; set; }
            public string SamplingRate { get; set; }
            public string InputFileSize { get; set; }
            public string OutputFileSize { get; set; }
            public string Compression { get; set; }
            public string Time { get; set; }
            public string Speed { get; set; }
            public string SpeedMin { get; set; }
            public string SpeedMax { get; set; }
            public string SpeedRange { get; set; }
            public double SpeedP50 { get; set; }
            public double SpeedP90 { get; set; }
            public string SpeedConsistency { get; set; }
            public string CPULoadEncoder { get; set; }
            public string CPUClock { get; set; }
            public string Passes { get; set; }
            public string Parameters { get; set; }
            public string Encoder { get; set; }
            public string Version { get; set; }
            public string EncoderDirectory { get; set; }
            public string FastestEncoder { get; set; }
            public string BestSize { get; set; }
            public string SameSize { get; set; }
            public string AudioFileDirectory { get; set; }
            public string MD5 { get; set; }
            public string Duplicates { get; set; }
            public string Errors { get; set; }

            public Color OutputForeColor { get; set; } // Color for OutputFileSize
            public Color CompressionForeColor { get; set; } // Color for Compression
            public Color SpeedForeColor { get; set; } // Color for Speed
        }
        private class BenchmarkPass
        {
            public string AudioFilePath { get; set; }
            public string EncoderPath { get; set; }
            public string Parameters { get; set; }
            public long InputSize { get; set; }
            public long OutputSize { get; set; }
            public double Time { get; set; }
            public double Speed { get; set; }
            public double CPULoadEncoder { get; set; }
            public double CPUClock { get; set; }
            public string BitDepth { get; set; }
            public string SamplingRate { get; set; }
            public DateTime Timestamp { get; set; }
        }
        private readonly List<BenchmarkPass> _benchmarkPasses = new();
        private void SortDataGridView()
        {
            // Collect data from DataGridView into a list
            var dataToSort = new List<LogEntry>();
            foreach (DataGridViewRow row in dataGridViewLog.Rows)
            {
                if (row.IsNewRow) continue; // Skip new row

                var logEntry = new LogEntry
                {
                    Name = row.Cells["Name"].Value?.ToString(),
                    BitDepth = row.Cells["BitDepth"].Value?.ToString(),
                    SamplingRate = row.Cells["SamplingRate"].Value?.ToString(),
                    InputFileSize = row.Cells["InputFileSize"].Value?.ToString(),
                    OutputFileSize = row.Cells["OutputFileSize"].Value?.ToString(),
                    Compression = row.Cells["Compression"].Value?.ToString(),
                    Time = row.Cells["Time"].Value?.ToString(),
                    Speed = row.Cells["Speed"].Value?.ToString(),
                    SpeedMin = row.Cells["SpeedMin"].Value?.ToString(),
                    SpeedMax = row.Cells["SpeedMax"].Value?.ToString(),
                    SpeedRange = row.Cells["SpeedRange"].Value?.ToString(),
                    SpeedConsistency = row.Cells["SpeedConsistency"].Value?.ToString(),
                    CPULoadEncoder = row.Cells["CPULoadEncoder"].Value?.ToString(),
                    CPUClock = row.Cells["CPUClock"].Value?.ToString(),
                    Passes = row.Cells["Passes"].Value?.ToString(),
                    Parameters = row.Cells["Parameters"].Value?.ToString(),
                    Encoder = row.Cells["Encoder"].Value?.ToString(),
                    Version = row.Cells["Version"].Value?.ToString(),
                    EncoderDirectory = row.Cells["EncoderDirectory"].Value?.ToString(),
                    FastestEncoder = row.Cells["FastestEncoder"].Value?.ToString(),
                    BestSize = row.Cells["BestSize"].Value?.ToString(),
                    SameSize = row.Cells["SameSize"].Value?.ToString(),
                    AudioFileDirectory = row.Cells["AudioFileDirectory"].Value?.ToString(),
                    MD5 = row.Cells["MD5"].Value?.ToString(),
                    Duplicates = row.Cells["Duplicates"].Value?.ToString(),
                    Errors = row.Cells["Errors"].Value?.ToString(),

                    OutputForeColor = row.Cells["OutputFileSize"].Style.ForeColor, // Color for OutputFileSize
                    CompressionForeColor = row.Cells["Compression"].Style.ForeColor, // Color for Compression
                    SpeedForeColor = row.Cells["Speed"].Style.ForeColor // Color for Speed
                };

                dataToSort.Add(logEntry);
            }

            // Perform multi-level sorting with natural sort for Parameters
            var sortedData = dataToSort
                .OrderBy(x => x.AudioFileDirectory)
                .ThenBy(x => x.Name)
                .ThenBy(x => x.Parameters, new NaturalStringComparer())
                .ThenBy(x => x.EncoderDirectory)
                .ThenBy(x => x.Encoder)
                .ToList();

            // Clear DataGridView and add sorted data
            dataGridViewLog.Rows.Clear();
            foreach (var data in sortedData)
            {
                int rowIndex = dataGridViewLog.Rows.Add(
                    data.Name,
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
        private class NaturalStringComparer : IComparer<string>
        {
            public int Compare(string x, string y)
            {
                if (x == null && y == null) return 0;
                if (x == null) return -1;
                if (y == null) return 1;

                return CompareNatural(x, y);
            }

            private static int CompareNatural(string strA, string strB)
            {
                int i1 = 0, i2 = 0;
                while (i1 < strA.Length && i2 < strB.Length)
                {
                    if (char.IsDigit(strA[i1]) && char.IsDigit(strB[i2]))
                    {
                        long n1 = 0, n2 = 0;
                        while (i1 < strA.Length && char.IsDigit(strA[i1]))
                            n1 = n1 * 10 + (strA[i1++] - '0');
                        while (i2 < strB.Length && char.IsDigit(strB[i2]))
                            n2 = n2 * 10 + (strB[i2++] - '0');

                        if (n1 != n2)
                            return n1.CompareTo(n2);
                    }
                    else
                    {
                        int cmp = char.ToLower(strA[i1]).CompareTo(char.ToLower(strB[i2]));
                        if (cmp != 0)
                            return cmp;
                        i1++;
                        i2++;
                    }
                }
                return strA.Length.CompareTo(strB.Length);
            }
        }

        // Log to Excel, copy, clear
        private void buttonLogToExcel_Click(object? sender, EventArgs e)
        {
            using (var workbook = new XLWorkbook())
            {
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
        }
        private void ExportDataGridViewToWorksheet(XLWorkbook workbook, DataGridView dgv, string sheetName)
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
                    if (colName == "InputFileSize" || colName == "OutputFileSize")
                    {
                        if (cellValue != null && long.TryParse(cellValue.ToString().Replace(" ", ""), out long val))
                            sheetCell.Value = val;
                        else
                            sheetCell.Value = cellValue?.ToString() ?? string.Empty;
                    }
                    else if (colName == "BitDepth" || colName == "SamplingRate")
                    {
                        if (cellValue != null && long.TryParse(cellValue.ToString().Replace(" ", ""), out long val))
                            sheetCell.Value = val;
                        else
                            sheetCell.Value = cellValue?.ToString() ?? string.Empty;
                    }
                    else if (colName == "Compression" || colName == "CPULoadEncoder" || colName == "SpeedConsistency")
                    {
                        if (cellValue != null && double.TryParse(cellValue.ToString().Replace("%", "").Trim(), out double val))
                            sheetCell.Value = val / 100.0;
                        else
                            sheetCell.Value = cellValue?.ToString() ?? string.Empty;
                    }
                    else if (colName == "Time" || colName == "Speed" || colName == "SpeedMin" ||
                             colName == "SpeedMax" || colName == "SpeedRange" || colName == "CPUClock")
                    {
                        if (cellValue != null)
                        {
                            string cleanValue = cellValue.ToString()
                                .Replace("x", "").Replace("MHz", "").Trim();
                            if (double.TryParse(cleanValue, out double val))
                                sheetCell.Value = val;
                            else
                                sheetCell.Value = cellValue.ToString();
                        }
                        else
                        {
                            sheetCell.Value = string.Empty;
                        }
                    }
                    else if (colName == "Passes")
                    {
                        if (cellValue != null && int.TryParse(cellValue.ToString(), out int val))
                            sheetCell.Value = val;
                        else
                            sheetCell.Value = cellValue?.ToString() ?? string.Empty;
                    }
                    else if (colName == "EncoderDirectory" || colName == "AudioFileDirectory")
                    {
                        string path = cellValue?.ToString() ?? string.Empty;
                        sheetCell.Value = path;
                        if (Directory.Exists(path))
                            sheetCell.SetHyperlink(new XLHyperlink(path));
                    }
                    else
                    {
                        sheetCell.Value = cellValue?.ToString() ?? string.Empty;
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
            worksheet.RangeUsed().SetAutoFilter();
            worksheet.SheetView.FreezeRows(1);
            worksheet.Columns().AdjustToContents();

            // Header style
            worksheet.Row(1).Style.Fill.SetBackgroundColor(XLColor.FromHtml("4F81BD"));
            worksheet.Row(1).Style.Font.FontColor = XLColor.White;
        }
        private void buttonCopyLogAsBBCode_Click(object? sender, EventArgs e)
        {
            try
            {
                int rowCount = dataGridViewLog.Rows.Count;

                // Check if there are any non-new rows
                bool hasRows = false;
                for (int i = 0; i < rowCount; i++)
                {
                    if (!dataGridViewLog.Rows[i].IsNewRow)
                    {
                        hasRows = true;
                        break;
                    }
                }

                if (!hasRows)
                {
                    MessageBox.Show("There is no log to copy.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Define base columns to include
                var baseColumnsToInclude = new HashSet<string>
                {
                    "BitDepth",
                    "SamplingRate",
                    "InputFileSize",
                    "OutputFileSize",
                    "Compression",
                    "Time",
                    "Speed",
                    "SpeedMin",
                    "SpeedMax",
                    "SpeedRange",
                    "SpeedConsistency",
                    "CPULoadEncoder",
                    "CPUClock",
                    "Passes",
                    "Parameters",
                    "Encoder",
                    "Version"
                };

                // Check if Shift is pressed
                bool includeNameColumn = ModifierKeys.HasFlag(Keys.Shift);

                if (includeNameColumn)
                {
                    baseColumnsToInclude.Add("Name");
                }

                // Pre-cache included columns in display order
                var includedColumns = dataGridViewLog.Columns
                    .Cast<DataGridViewColumn>()
                    .Where(col => baseColumnsToInclude.Contains(col.Name))
                    .OrderBy(col => col.DisplayIndex)
                    .Select(col => new { col.Index, col.HeaderText })
                    .ToArray();

                if (includedColumns.Length == 0)
                {
                    MessageBox.Show("No valid columns to copy.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Estimate capacity and build BBCode
                int estimatedCapacity = rowCount * includedColumns.Length * 20;
                StringBuilder bbCodeText = new StringBuilder(estimatedCapacity);

                bbCodeText.AppendLine("[table]");

                // Headers
                bbCodeText.Append("[tr]");
                foreach (var col in includedColumns)
                {
                    bbCodeText.Append($"[td][b]{col.HeaderText}[/b][/td]");
                }
                bbCodeText.AppendLine("[/tr]");

                // Rows
                for (int rowIndex = 0; rowIndex < rowCount; rowIndex++)
                {
                    DataGridViewRow row = dataGridViewLog.Rows[rowIndex];
                    if (row.IsNewRow) continue;

                    bbCodeText.Append("[tr]");
                    foreach (var col in includedColumns)
                    {
                        string cellValue = row.Cells[col.Index]?.Value?.ToString() ?? "";
                        bbCodeText.Append($"[td]{cellValue}[/td]");
                    }
                    bbCodeText.AppendLine("[/tr]");
                }

                bbCodeText.Append("[/table]");

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
        private void buttonOpenLogtxt_Click(object? sender, EventArgs e)
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
        private void buttonCopyLog_Click(object? sender, EventArgs e)
        {
            // Determine which DataGridView corresponds to the currently selected tab
            DataGridView activeGrid = tabControlLog.SelectedTab switch
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
        private void buttonClearLog_Click(object? sender, EventArgs e)
        {
            // Check if Shift key is pressed
            bool clearAllTabs = ModifierKeys.HasFlag(Keys.Shift);

            if (clearAllTabs)
            {
                // CLEAR ALL THREE TABS
                dataGridViewLog.Rows.Clear();
                dataGridViewLogDetectDupes.Rows.Clear();
                dataGridViewLogTestForErrors.Rows.Clear();

                // Hide optional columns 
                dataGridViewLog.Columns["Errors"].Visible = false;

                // Clear internal benchmark cache (only relevant to Benchmark tab)
                _benchmarkPasses.Clear();

                // Clear selections
                dataGridViewLog.ClearSelection();
                dataGridViewLogDetectDupes.ClearSelection();
                dataGridViewLogTestForErrors.ClearSelection();

                // Switch to Benchmark tab after full clear
                tabControlLog.SelectedTab = Benchmark;
            }
            else
            {
                // CLEAR ONLY THE CURRENTLY SELECTED TAB
                if (tabControlLog.SelectedTab == Benchmark)
                {
                    dataGridViewLog.Rows.Clear();
                    dataGridViewLog.Columns["Errors"].Visible = false;
                    _benchmarkPasses.Clear();
                    dataGridViewLog.ClearSelection();
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

        // Key actions
        private void ListViewEncoders_KeyDown(object? sender, KeyEventArgs e)
        {
            // Check if Delete key is pressed
            if (e.KeyCode == Keys.Delete)
            {
                buttonRemoveEncoder.PerformClick();
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
                buttonRemoveAudioFile.PerformClick();
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
        private void ListViewJobs_KeyDown(object? sender, KeyEventArgs e)
        {
            // Check if Delete key is pressed
            if (e.KeyCode == Keys.Delete)
            {
                buttonRemoveJob.PerformClick();
            }

            // Check if Ctrl and A are pressed simultaneously
            if (e.Control && e.KeyCode == Keys.A)
            {
                e.Handled = true; // Cancel default behavior

                // Select all items
                foreach (ListViewItem item in listViewJobs.Items)
                {
                    item.Selected = true; // Set selection for each item
                }
            }

            // Handle Ctrl+C (Copy)
            if (e.Control && e.KeyCode == Keys.C)
            {
                buttonCopyJobs.PerformClick();
                e.Handled = true; // Cancel default behavior
            }

            // Handle Ctrl+V (Paste)
            if (e.Control && e.KeyCode == Keys.V)
            {
                buttonPasteJobs.PerformClick();
                e.Handled = true; // Cancel default behavior
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
        private void ListViewJobs_DrawColumnHeader(object? sender, DrawListViewColumnHeaderEventArgs e)
        {
            e.DrawDefault = true;
        }
        private void ListViewJobs_DrawSubItem(object? sender, DrawListViewSubItemEventArgs e)
        {
            if (e.ColumnIndex == 0 && e.Item != null)
            {
                e.DrawBackground();

                // Draw checkbox if enabled
                if (listViewJobs.CheckBoxes)
                {
                    CheckBoxRenderer.DrawCheckBox(e.Graphics,
                        new Point(e.Bounds.Left + 4, e.Bounds.Top + 2),
                        e.Item.Checked
                            ? System.Windows.Forms.VisualStyles.CheckBoxState.CheckedNormal
                            : System.Windows.Forms.VisualStyles.CheckBoxState.UncheckedNormal);
                }

                // Determine text color based on job type
                Color textColor = e.Item.ForeColor;

                if (e.SubItem?.Text != null)
                {
                    if (e.SubItem.Text.Contains("Encode", StringComparison.OrdinalIgnoreCase))
                        textColor = Color.Green;
                    else if (e.SubItem.Text.Contains("Decode", StringComparison.OrdinalIgnoreCase))
                        textColor = Color.Red;
                }

                using var brush = new SolidBrush(textColor);

                // Shift text to the right to avoid overlapping with checkbox
                Rectangle textBounds = new Rectangle(
                    e.Bounds.Left + (listViewJobs.CheckBoxes ? 20 : 0),
                    e.Bounds.Top,
                    e.Bounds.Width - (listViewJobs.CheckBoxes ? 20 : 0),
                    e.Bounds.Height);

                e.Graphics.DrawString(
                    e.SubItem?.Text ?? string.Empty,
                    e.SubItem?.Font ?? e.Item.Font ?? this.Font,
                    brush,
                    textBounds,
                    StringFormat.GenericDefault);

                e.DrawFocusRectangle(e.Bounds);
            }
            else
            {
                e.DrawDefault = true;
            }
        }
        private void ListViewJobs_DragEnter(object? sender, DragEventArgs e)
        {
            if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
            {
                string[] files = (string[]?)e.Data.GetData(DataFormats.FileDrop) ?? Array.Empty<string>();
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
        private void ListViewJobs_DragDrop(object? sender, DragEventArgs e)
        {
            string[] files = (string[]?)e.Data?.GetData(DataFormats.FileDrop) ?? Array.Empty<string>();
            foreach (var file in files)
            {
                if (Directory.Exists(file))
                {
                    AddJobsFromDirectory(file);
                }
                else if (Path.GetExtension(file).Equals(".txt", StringComparison.OrdinalIgnoreCase) ||
                         Path.GetExtension(file).Equals(".bak", StringComparison.OrdinalIgnoreCase))
                {
                    LoadJobsFromFile(file); // Load jobs from file
                }
            }
        }
        private async void AddJobsFromDirectory(string directory)
        {
            try
            {
                // Find all .txt and .bak files in current directory
                var txtFiles = await Task.Run(() => Directory.GetFiles(directory, "*.txt", SearchOption.AllDirectories));
                var bakFiles = await Task.Run(() => Directory.GetFiles(directory, "*.bak", SearchOption.AllDirectories));

                // Combine file arrays
                var allFiles = txtFiles.Concat(bakFiles);

                foreach (var file in allFiles)
                {
                    LoadJobsFromFile(file); // Load jobs from found file
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

                            AddJobsToListView(type, isChecked, passes, parameters);
                            continue;
                        }
                    }
                    else if (line.Contains('~'))
                    {
                        var parts = line.Split('~');
                        if (parts.Length == 4 && bool.TryParse(parts[1], out bool isChecked))
                        {
                            string jobName = NormalizeSpaces(parts[0]);
                            string passes = NormalizeSpaces(parts[2]);
                            string parameters = NormalizeSpaces(parts[3]);
                            AddJobsToListView(jobName, isChecked, passes, parameters);
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
        private void AddJobsToListView(string job, bool isChecked = true, string passes = "", string parameters = "")
        {
            var item = new ListViewItem(job) { Checked = isChecked };
            item.SubItems.Add(passes); // Add number of passes
            item.SubItems.Add(parameters); // Add parameters
            listViewJobs.Items.Add(item); // Add item to ListView
        }
        private async void buttonImportJobList_Click(object? sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
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

                                        AddJobsToListView(type, isChecked, passes, parameters);
                                        continue;
                                    }
                                }
                                else if (line.Contains('~'))
                                {
                                    var parts = line.Split('~');
                                    if (parts.Length == 4 && bool.TryParse(parts[1], out bool isChecked))
                                    {
                                        string jobName = NormalizeSpaces(parts[0]);
                                        string passes = NormalizeSpaces(parts[2]);
                                        string parameters = NormalizeSpaces(parts[3]);
                                        AddJobsToListView(jobName, isChecked, passes, parameters);
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
        }
        private void buttonExportJobList_Click(object? sender, EventArgs e)
        {
            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";
                saveFileDialog.Title = "Save Job List";
                string fileName = $"Settings_joblist {DateTime.Now:yyyy-MM-dd}.txt";
                saveFileDialog.FileName = fileName;
                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        var jobList = listViewJobs.Items.Cast<ListViewItem>()
                            .Select(item =>
                            {
                                string status = item.Checked ? "Checked" : "Unchecked";
                                string type = item.Text;
                                string passes = item.SubItems[1].Text;
                                string parameters = item.SubItems[2].Text;
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
        }
        private void buttonUpJob_Click(object? sender, EventArgs e)
        {
            MoveSelectedItems(listViewJobs, -1); // Pass -1 to move up
        }
        private void buttonDownJob_Click(object? sender, EventArgs e)
        {
            MoveSelectedItems(listViewJobs, 1); // Pass 1 to move down
        }
        private void buttonRemoveJob_Click(object? sender, EventArgs e)
        {
            // Remove selected items from listViewJobs
            for (int i = listViewJobs.Items.Count - 1; i >= 0; i--)
            {
                if (listViewJobs.Items[i].Selected) // Check if item is selected
                {
                    listViewJobs.Items.RemoveAt(i); // Remove item
                }
            }
        }
        private void buttonClearJobList_Click(object? sender, EventArgs e)
        {
            listViewJobs.Items.Clear(); // Clear listViewJobs
        }
        private void buttonAddJobToJobListEncoder_Click(object? sender, EventArgs e)
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

            // Check if job already exists in last item
            string jobName = "Encode";
            ListViewItem existingItem = null;

            if (listViewJobs.Items.Count > 0)
            {
                ListViewItem lastItem = listViewJobs.Items[listViewJobs.Items.Count - 1];
                if (lastItem.Text == jobName && lastItem.SubItems[2].Text == parameters)
                {
                    existingItem = lastItem;
                }
            }

            // If job already exists, increment pass count
            if (existingItem != null)
            {
                int currentPasses = int.Parse(existingItem.SubItems[1].Text);
                existingItem.SubItems[1].Text = (currentPasses + 1).ToString();
            }
            else
            {
                // If job doesn't exist, add new one with 1 pass
                var newItem = new ListViewItem(jobName) { Checked = true };
                newItem.SubItems.Add("1"); // Set default pass count
                newItem.SubItems.Add(parameters);
                listViewJobs.Items.Add(newItem);
            }
        }
        private void buttonAddJobToJobListDecoder_Click(object? sender, EventArgs e)
        {
            // Get values from text fields and form parameters
            string commandLine = NormalizeSpaces(textBoxCommandLineOptionsDecoder.Text);
            // Form parameter string
            string parameters = $"-d {commandLine}".Trim();

            // Check if job already exists in last item
            string jobName = "Decode";
            ListViewItem existingItem = null;

            if (listViewJobs.Items.Count > 0)
            {
                ListViewItem lastItem = listViewJobs.Items[listViewJobs.Items.Count - 1];
                if (lastItem.Text == jobName && lastItem.SubItems[2].Text == parameters)
                {
                    existingItem = lastItem;
                }
            }

            // If job already exists, increment pass count
            if (existingItem != null)
            {
                int currentPasses = int.Parse(existingItem.SubItems[1].Text);
                existingItem.SubItems[1].Text = (currentPasses + 1).ToString();
            }
            else
            {
                // If job doesn't exist, add new one with 1 pass
                var newItem = new ListViewItem(jobName) { Checked = true };
                newItem.SubItems.Add("1"); // Set default pass count
                newItem.SubItems.Add(parameters);
                listViewJobs.Items.Add(newItem);
            }
        }
        private void buttonPlusPass_Click(object? sender, EventArgs e)
        {
            listViewJobs.BeginUpdate(); // Disable redrawing

            try
            {
                foreach (ListViewItem item in listViewJobs.SelectedItems)
                {
                    int currentPasses = int.Parse(item.SubItems[1].Text); // Get current value
                    currentPasses++; // Increment by 1
                    item.SubItems[1].Text = currentPasses.ToString(); // Update cell value
                }
            }
            finally
            {
                listViewJobs.EndUpdate(); // Enable redrawing
            }
        }
        private void buttonMinusPass_Click(object? sender, EventArgs e)
        {
            listViewJobs.BeginUpdate(); // Disable redrawing

            try
            {
                foreach (ListViewItem item in listViewJobs.SelectedItems)
                {
                    int currentPasses = int.Parse(item.SubItems[1].Text); // Get current value
                    if (currentPasses > 1) // Make sure value is greater than 1
                    {
                        currentPasses--; // Decrement by 1
                        item.SubItems[1].Text = currentPasses.ToString(); // Update cell value
                    }
                }
            }
            finally
            {
                listViewJobs.EndUpdate(); // Enable redrawing
            }
        }
        private void buttonCopyJobs_Click(object? sender, EventArgs e)
        {
            StringBuilder jobsText = new StringBuilder();

            var itemsToCopy = listViewJobs.SelectedItems.Count > 0
                ? listViewJobs.SelectedItems.Cast<ListViewItem>()
                : listViewJobs.Items.Cast<ListViewItem>();

            foreach (var item in itemsToCopy)
            {
                string status = item.Checked ? "Checked" : "Unchecked";
                string type = item.Text;
                string passes = item.SubItems[1].Text;
                string parameters = item.SubItems[2].Text;

                jobsText.AppendLine($"{status}|{type}|{passes}|{parameters}");
            }

            if (jobsText.Length > 0)
            {
                Clipboard.SetText(jobsText.ToString());
            }
            else
            {
                MessageBox.Show("No jobs to copy.", "Information",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        private void buttonPasteJobs_Click(object? sender, EventArgs e)
        {
            try
            {
                string clipboardText = Clipboard.GetText();
                if (!string.IsNullOrEmpty(clipboardText))
                {
                    string[] lines = clipboardText.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var line in lines)
                    {
                        if (string.IsNullOrWhiteSpace(line))
                            continue;

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

                                AddJobsToListView(type, isChecked, passes, parameters);
                                continue;
                            }
                        }
                        else if (line.Contains('~'))
                        {
                            var parts = line.Split('~');
                            if (parts.Length == 4 && bool.TryParse(parts[1], out bool isChecked))
                            {
                                string jobName = parts[0];
                                string passes = parts[2];
                                string parameters = parts[3];
                                AddJobsToListView(jobName, isChecked, passes, parameters);
                                continue;
                            }
                        }

                        MessageBox.Show($"Invalid line format: {line}", "Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
                else
                {
                    MessageBox.Show("Clipboard is empty.", "Information",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error pasting jobs: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Script Constructor
        private ScriptConstructorForm? _scriptForm = null;
        private void buttonScriptConstructor_Click(object sender, EventArgs e)
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
                _scriptForm = new ScriptConstructorForm();
                _scriptForm.InitialScriptText = parameters;

                _scriptForm.OnJobsAdded += (jobs) =>
                {
                    foreach (var job in jobs)
                    {
                        listViewJobs.Items.Add(job);
                    }
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
        private async void buttonStartEncode_Click(object? sender, EventArgs e)
        {
            // Create a temporary directory for the output file
            Directory.CreateDirectory(tempFolderPath);

            if (isExecuting) return; // Check if process is already running
            isExecuting = true; // Set execution flag
            _isEncodingStopped = false;
            _isPaused = false;
            _pauseEvent.Set();

            this.Invoke((MethodInvoker)(() =>
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
                .Select(item => item.Tag.ToString()) // Get full path from Tag
                .ToList();

                // Get all selected .wav and .flac audio files
                var selectedAudioFiles = listViewAudioFiles.Items.Cast<ListViewItem>()
                .Where(item => item.Checked &&
                (Path.GetExtension(item.Tag.ToString()).Equals(".wav", StringComparison.OrdinalIgnoreCase) ||
                Path.GetExtension(item.Tag.ToString()).Equals(".flac", StringComparison.OrdinalIgnoreCase)))
                .Select(item => item.Tag.ToString()) // Get full path from Tag
                .ToList();

                // 1. Check if there is at least one encoder
                if (selectedEncoders.Count == 0)
                {
                    MessageBox.Show("Select at least one encoder.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    isExecuting = false;
                    return;
                }

                // 2. Check if there is at least one audio file
                if (selectedAudioFiles.Count == 0)
                {
                    MessageBox.Show("Select at least one audio file (WAV or FLAC).", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

                        string outputFilePath = Path.Combine(tempFolderPath, "temp_warmup.flac");
                        string arguments = $"\"{firstAudioFile}\" {parameters} --no-preserve-modtime -f -o \"{outputFilePath}\"";

                        this.Invoke((MethodInvoker)(() =>
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
                                    DeleteFileIfExists(outputFilePath);

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
                                DeleteFileIfExists(outputFilePath);

                                this.Invoke((MethodInvoker)(() =>
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
                    foreach (var encoder in selectedEncoders)
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
                        _cpuClockReadings = new List<double>();
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
                                        FileName = encoder,
                                        Arguments = arguments,
                                        UseShellExecute = false,
                                        CreateNoWindow = true,
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

                                            _process.WaitForExit();
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
                            if (_cpuClockReadings.Any() && _baseClockMhz > 0)
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
                                        using (var metaflacProcess = new Process())
                                        {
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
                                    }
                                    catch (Exception ex)
                                    {
                                        MessageBox.Show($"Error removing metadata: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                    }
                                }
                            }

                            if (!_isEncodingStopped)
                            {
                                await LogProcessResults(outputFilePath, audioFilePath, parameters, encoder, elapsedTime, userProcessorTime, privilegedProcessorTime, avgClock);
                            }
                        }
                        catch (Exception ex)
                        {
                            clockTimer.Stop();
                            MessageBox.Show($"Error starting encoding process: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

                this.Invoke((MethodInvoker)(() =>
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
        private async void buttonStartDecode_Click(object? sender, EventArgs e)
        {
            // Create a temporary directory for the output file
            Directory.CreateDirectory(tempFolderPath);

            if (isExecuting) return; // Check if process is already running
            isExecuting = true; // Set execution flag
            _isEncodingStopped = false;
            _isPaused = false;
            _pauseEvent.Set();

            this.Invoke((MethodInvoker)(() =>
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
                .Select(item => item.Tag.ToString()) // Get full path from Tag
                .ToList();

                // Get all selected .flac audio files
                var selectedFlacAudioFiles = listViewAudioFiles.Items.Cast<ListViewItem>()
                .Where(item => item.Checked &&
                (Path.GetExtension(item.Tag.ToString()).Equals(".flac", StringComparison.OrdinalIgnoreCase)))
                .Select(item => item.Tag.ToString()) // Get full path from Tag
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

                    if (!string.IsNullOrEmpty(firstAudioFile) && !string.IsNullOrEmpty(firstEncoder))
                    {
                        // Use current UI settings to form parameters
                        string commandLine = NormalizeSpaces(textBoxCommandLineOptionsDecoder.Text);
                        string parameters = $"-d {commandLine}".Trim();

                        string outputFilePath = Path.Combine(tempFolderPath, "temp_warmup.wav");
                        string arguments = $"\"{firstAudioFile}\" {parameters} --no-preserve-modtime -f -o \"{outputFilePath}\"";

                        this.Invoke((MethodInvoker)(() =>
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
                                    DeleteFileIfExists(outputFilePath);

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
                                DeleteFileIfExists(outputFilePath);

                                this.Invoke((MethodInvoker)(() =>
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
                    foreach (var encoder in selectedEncoders)
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
                        _cpuClockReadings = new List<double>();
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
                                        FileName = encoder,
                                        Arguments = arguments,
                                        UseShellExecute = false,
                                        CreateNoWindow = true,
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

                                            _process.WaitForExit();
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
                            if (_cpuClockReadings.Any() && _baseClockMhz > 0)
                            {
                                double avgPercent = _cpuClockReadings.Average();
                                avgClock = (avgPercent / 100.0) * _baseClockMhz;
                            }

                            if (!_isEncodingStopped)
                            {
                                await LogProcessResults(outputFilePath, audioFilePath, parameters, encoder, elapsedTime, userProcessorTime, privilegedProcessorTime, avgClock);
                            }
                        }
                        catch (Exception ex)
                        {
                            clockTimer.Stop();
                            MessageBox.Show($"Error starting decoding process: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

                this.Invoke((MethodInvoker)(() =>
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
        private async void buttonStartJobList_Click(object? sender, EventArgs e)
        {
            // Create a temporary directory for the output file
            Directory.CreateDirectory(tempFolderPath);

            if (isExecuting) return; // Check if process is already running
            isExecuting = true; // Set execution flag
            _isEncodingStopped = false;
            _isPaused = false;
            _pauseEvent.Set();

            this.Invoke((MethodInvoker)(() =>
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
                .Select(item => item.Tag.ToString()) // Get full path from Tag
                .ToList();

                // Get all selected .wav and .flac audio files
                var selectedAudioFiles = listViewAudioFiles.Items.Cast<ListViewItem>()
                .Where(item => item.Checked &&
                (Path.GetExtension(item.Tag.ToString()).Equals(".wav", StringComparison.OrdinalIgnoreCase) ||
                Path.GetExtension(item.Tag.ToString()).Equals(".flac", StringComparison.OrdinalIgnoreCase)))
                .Select(item => item.Tag.ToString()) // Get full path from Tag
                .ToList();

                // Get all selected .flac audio files
                var selectedFlacAudioFiles = selectedAudioFiles
                .Where(file => Path.GetExtension(file).Equals(".flac", StringComparison.OrdinalIgnoreCase))
                .ToList();

                // Create expanded Job List (Virtual Job List)
                var listViewJobsExpanded = new List<ListViewItem>();

                foreach (ListViewItem item in listViewJobs.Items)
                {
                    if (item.Checked)
                    {
                        string parameters = NormalizeSpaces(item.SubItems[2].Text.Trim());

                        // Check if parameters contain script patterns (like [0..8] or [1,2,3])
                        if (parameters.Contains('[') && parameters.Contains(']'))
                        {
                            // Expand script using ScriptParser
                            var expandedParameters = ScriptParser.ExpandScriptLine(parameters);

                            foreach (string expandedParam in expandedParameters)
                            {
                                // Create new job item for each expanded parameter set
                                ListViewItem newItem = new ListViewItem(item.Text);
                                newItem.SubItems.Add(item.SubItems[1].Text);
                                newItem.SubItems.Add(expandedParam);
                                newItem.Checked = true;
                                listViewJobsExpanded.Add(newItem);
                            }
                        }
                        else
                        {
                            // Regular job without script expansion
                            listViewJobsExpanded.Add(item);
                        }
                    }
                }

                // Count the number of tasks and passes for Encode
                int totalEncodeTasks = listViewJobsExpanded
                .Where(item => string.Equals(NormalizeSpaces(item.Text), "Encode", StringComparison.OrdinalIgnoreCase))
                .Sum(item => int.Parse(item.SubItems[1].Text.Trim()));

                // Count the number of tasks and passes for Decode
                int totalDecodeTasks = listViewJobsExpanded
                .Where(item => string.Equals(NormalizeSpaces(item.Text), "Decode", StringComparison.OrdinalIgnoreCase))
                .Sum(item => int.Parse(item.SubItems[1].Text.Trim()));

                // 1. Check if there is at least one job
                if (totalEncodeTasks == 0 && totalDecodeTasks == 0)
                {
                    MessageBox.Show("Select at least one job.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    isExecuting = false;
                    return;
                }

                // 2. Check if there is at least one encoder
                if (selectedEncoders.Count == 0)
                {
                    MessageBox.Show("Select at least one encoder.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    isExecuting = false;
                    return;
                }

                // 3. Check if there are audio files for the tasks
                if (totalEncodeTasks > 0)
                {
                    // For encoding: any WAV/FLAC files
                    if (selectedAudioFiles.Count == 0)
                    {
                        MessageBox.Show("Select at least one audio file (WAV or FLAC).", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                    // Find the first job in listViewJobsExpanded that will actually be executed
                    ListViewItem firstExecutableJobItem = null;
                    string jobType = null;
                    string audioFilePath = null;
                    string outputFilePath = null;

                    foreach (var jobItem in listViewJobsExpanded)
                    {
                        string type = NormalizeSpaces(jobItem.Text);
                        if (string.Equals(type, "Encode", StringComparison.OrdinalIgnoreCase) && selectedAudioFiles.Any())
                        {
                            firstExecutableJobItem = jobItem;
                            jobType = type;
                            audioFilePath = selectedAudioFiles.First();
                            outputFilePath = Path.Combine(tempFolderPath, "temp_warmup.flac");
                            break;
                        }
                        else if (string.Equals(type, "Decode", StringComparison.OrdinalIgnoreCase) && selectedFlacAudioFiles.Any())
                        {
                            firstExecutableJobItem = jobItem;
                            jobType = type;
                            audioFilePath = selectedFlacAudioFiles.First();
                            outputFilePath = Path.Combine(tempFolderPath, "temp_warmup.wav");
                            break;
                        }
                    }


                    string parameters = NormalizeSpaces(firstExecutableJobItem.SubItems[2].Text.Trim());
                    var firstEncoder = selectedEncoders.FirstOrDefault();
                    if (string.IsNullOrEmpty(firstEncoder)) return;

                    string arguments = $"\"{audioFilePath}\" {parameters} --no-preserve-modtime -f -o \"{outputFilePath}\"";

                    this.Invoke((MethodInvoker)(() =>
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
                                DeleteFileIfExists(outputFilePath);

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
                            DeleteFileIfExists(outputFilePath);

                            this.Invoke((MethodInvoker)(() =>
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

                foreach (ListViewItem item in listViewJobsExpanded)
                {
                    string jobType = NormalizeSpaces(item.Text);
                    int passes = int.Parse(item.SubItems[1].Text.Trim());

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
                                foreach (var encoder in selectedEncoders)
                                {
                                    if (_isEncodingStopped)
                                    {
                                        isExecuting = false;
                                        return;
                                    }

                                    // Form the parameter string
                                    string parameters = NormalizeSpaces(item.SubItems[2].Text.Trim());

                                    // Form the arguments for execution
                                    string outputFilePath = Path.Combine(tempFolderPath, "temp_encoded.flac"); // Output file name
                                    DeleteFileIfExists(outputFilePath); // Delete the old file
                                    string arguments = $"\"{audioFilePath}\" {parameters} --no-preserve-modtime -f -o \"{outputFilePath}\"";

                                    // Prepare for CPU clock monitoring
                                    _cpuClockReadings = new List<double>();
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
                                                    FileName = encoder,
                                                    Arguments = arguments,
                                                    UseShellExecute = false,
                                                    CreateNoWindow = true,
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

                                                        _process.WaitForExit();
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
                                        if (_cpuClockReadings.Any() && _baseClockMhz > 0)
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
                                                    using (var metaflacProcess = new Process())
                                                    {
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
                                                }
                                                catch (Exception ex)
                                                {
                                                    MessageBox.Show($"Error removing metadata: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                                }
                                            }
                                        }

                                        if (!_isEncodingStopped)
                                        {
                                            await LogProcessResults(outputFilePath, audioFilePath, parameters, encoder, elapsedTime, userProcessorTime, privilegedProcessorTime, avgClock);
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        clockTimer.Stop();
                                        MessageBox.Show($"Error starting encoding process: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                                foreach (var encoder in selectedEncoders)
                                {
                                    if (_isEncodingStopped)
                                    {
                                        isExecuting = false;
                                        return;
                                    }

                                    // Form the parameter string
                                    string parameters = NormalizeSpaces(item.SubItems[2].Text.Trim());

                                    // Form the arguments for execution
                                    string outputFilePath = Path.Combine(tempFolderPath, "temp_decoded.wav"); // Output file name
                                    DeleteFileIfExists(outputFilePath); // Delete the old file
                                    string arguments = $"\"{audioFilePath}\" {parameters} --no-preserve-modtime -f -o \"{outputFilePath}\"";

                                    // Prepare for CPU clock monitoring
                                    _cpuClockReadings = new List<double>();
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
                                                    FileName = encoder,
                                                    Arguments = arguments,
                                                    UseShellExecute = false,
                                                    CreateNoWindow = true,
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

                                                        _process.WaitForExit();
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
                                        if (_cpuClockReadings.Any() && _baseClockMhz > 0)
                                        {
                                            double avgPercent = _cpuClockReadings.Average();
                                            avgClock = (avgPercent / 100.0) * _baseClockMhz;
                                        }

                                        if (!_isEncodingStopped)
                                        {
                                            await LogProcessResults(outputFilePath, audioFilePath, parameters, encoder, elapsedTime, userProcessorTime, privilegedProcessorTime, avgClock);
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        clockTimer.Stop();
                                        MessageBox.Show($"Error starting decoding process: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

                this.Invoke((MethodInvoker)(() =>
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

        // Encoder and Decoder options
        private void button5CompressionLevel_Click(object? sender, EventArgs e)
        {
            textBoxCompressionLevel.Text = "5";
        }
        private void buttonMaxCompressionLevel_Click(object? sender, EventArgs e)
        {
            textBoxCompressionLevel.Text = "8";
        }
        private void buttonHalfCores_Click(object? sender, EventArgs e)
        {
            textBoxThreads.Text = (physicalCores / 2).ToString(); // Set half of the cores
        }
        private void buttonSetMaxCores_Click(object? sender, EventArgs e)
        {
            textBoxThreads.Text = physicalCores.ToString(); // Set maximum number of cores
        }
        private void buttonSetHalfThreads_Click(object? sender, EventArgs e)
        {
            textBoxThreads.Text = (threadCount / 2).ToString(); // Set half of the threads
        }
        private void buttonSetMaxThreads_Click(object? sender, EventArgs e)
        {
            textBoxThreads.Text = threadCount.ToString(); // Set maximum number of threads
        }
        private void buttonClearCommandLineEncoder_Click(object? sender, EventArgs e)
        {
            textBoxCommandLineOptionsEncoder.Clear(); // Clear textCommandLineOptions
        }
        private void buttonClearCommandLineDecoder_Click(object? sender, EventArgs e)
        {
            textBoxCommandLineOptionsDecoder.Clear(); // Clear textCommandLineOptions
        }
        private void buttonEpr8_Click(object? sender, EventArgs e)
        {
            // Check if -epr8 is already in textBoxAdditionalArguments
            if (!textBoxCommandLineOptionsEncoder.Text.Contains("-epr8"))
            {
                // If not, add it
                textBoxCommandLineOptionsEncoder.AppendText(" -epr8"); // Add with space before text
            }
        }
        private void buttonAsubdividetukey5flattop_Click(object? sender, EventArgs e)
        {
            // Check if -A "subdivide_tukey(5);flattop" is already in textBoxAdditionalArguments
            if (!textBoxCommandLineOptionsEncoder.Text.Contains("-A \"subdivide_tukey(5);flattop\""))
            {
                // If not, add it
                textBoxCommandLineOptionsEncoder.AppendText(" -A \"subdivide_tukey(5);flattop\""); // Add with space before text
            }
        }
        private void buttonNoPadding_Click(object? sender, EventArgs e)
        {
            // Check if --no-padding is already in textBoxAdditionalArguments
            if (!textBoxCommandLineOptionsEncoder.Text.Contains("--no-padding"))
            {
                // If not, add it
                textBoxCommandLineOptionsEncoder.AppendText(" --no-padding"); // Add with space before text
            }
        }
        private void buttonNoSeektable_Click(object? sender, EventArgs e)
        {
            // Check if --no-seektable is already in textBoxAdditionalArguments
            if (!textBoxCommandLineOptionsEncoder.Text.Contains("--no-seektable"))
            {
                // If not, add it
                textBoxCommandLineOptionsEncoder.AppendText(" --no-seektable"); // Add with space before text
            }
        }

        private void textBoxCompressionLevel_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;

                buttonAddJobToJobListEncoder_Click(sender, e);
            }
        }
        private void textBoxThreads_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;

                buttonAddJobToJobListEncoder_Click(sender, e);
            }
        }
        private void textBoxCommandLineOptionsEncoder_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;

                buttonAddJobToJobListEncoder_Click(sender, e);
            }
        }
        private void textBoxCommandLineOptionsDecoder_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;

                buttonAddJobToJobListDecoder_Click(sender, e);
            }
        }

        private void buttonStop_Click(object? sender, EventArgs e)
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
                catch (Exception ex)
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
        private void buttonPauseResume_Click(object sender, EventArgs e)
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

        // General methods
        private void MoveSelectedItems(ListView listView, int direction)
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
        private void UpdateSelection(List<ListViewItem> selectedItems, ListView listView)
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
            temporaryMessageTimer.Interval = 6000;
            temporaryMessageTimer.Start();
        }

        private void buttonSelectTempFolder_Click(object? sender, EventArgs e)
        {
            using (FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog())
            {
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
        }
        private void DeleteFileIfExists(string filePath)
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
        private void checkBoxAutoAnalyzeLog_CheckedChanged(object sender, EventArgs e)
        {

        }
        private void checkBoxWarningsAsErrors_CheckedChanged(object sender, EventArgs e)
        {

        }
        private void checkBoxPreventSleep_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxPreventSleep.Checked)
            {
                SetThreadExecutionState(ES_CONTINUOUS | ES_SYSTEM_REQUIRED);
            }
            else
            {
                SetThreadExecutionState(ES_CONTINUOUS);
            }
        }
        private void checkBoxCheckForUpdatesOnStartup_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxCheckForUpdatesOnStartup.Checked)
            {
                _ = CheckForUpdatesAsync();
            }
        }
        private async Task CheckForUpdatesAsync()
        {
            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.UserAgent.ParseAdd("FLAC-Benchmark-H-App");

                string programVersionLatestUrl = "https://raw.githubusercontent.com/hat3k/FLAC-Benchmark-H/master/Version.txt";
                string programVersionLatestOnline = await client.GetStringAsync(programVersionLatestUrl).ConfigureAwait(false);
                programVersionLatestOnline = programVersionLatestOnline.Trim();

                Version current = ParseVersion(programVersionCurrent);
                Version latest = ParseVersion(programVersionLatestOnline);

                if (latest != null && current != null && latest > current)
                {
                    if (programVersionIgnored == programVersionLatestOnline)
                        return;

                    var result = MessageBox.Show(
                    $"A new version is available!\n\nCurrent version:\t{programVersionCurrent}\nLatest version:\t{programVersionLatestOnline}\n\nClick 'Cancel' to ignore this update.\nDo you want to open the releases page?",
                    "Update Available",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Information);

                    if (result == DialogResult.Yes)
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = "https://github.com/hat3k/FLAC-Benchmark-H/releases",
                            UseShellExecute = true
                        });
                    }
                    else if (result == DialogResult.Cancel)
                    {
                        programVersionIgnored = programVersionLatestOnline;
                    }
                }
            }
            catch (TaskCanceledException)
            {
                // Timeout or network issue - silently ignore
            }
            catch (HttpRequestException)
            {
                // Network unreachable, HTTP error - silently ignore
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error checking for updates:\n{ex.Message}",
                "Network Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private Version ParseVersion(string versionStr)
        {
            if (string.IsNullOrWhiteSpace(versionStr))
                return new Version("0.0.0");

            var parts = versionStr.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 3 && parts[1].Equals("build", StringComparison.OrdinalIgnoreCase))
            {
                if (int.TryParse(parts[2], out int buildNumber))
                {
                    string[] versionParts = parts[0].Split('.');
                    if (versionParts.Length >= 2)
                    {
                        int major = int.TryParse(versionParts[0], out int m) ? m : 0;
                        int minor = int.TryParse(versionParts[1], out int n) ? n : 0;
                        int patch = (versionParts.Length > 2 && int.TryParse(versionParts[2], out int p)) ? p : 0;

                        return new Version(major, minor, patch, buildNumber);
                    }
                }
            }

            // If format is not recognized, consider version very old
            return new Version("0.0.0");
        }
        private void buttonAbout_Click(object sender, EventArgs e)
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
            doubleBufferedProperty?.SetValue(listViewJobs, true, null);
        }

        // FORM LOAD
        private async void Form1_Load(object? sender, EventArgs e)
        {
            this.Text = $"FLAC Benchmark-H [{programVersionCurrent}]";
            progressBarEncoder.ManualText = string.Empty;
            progressBarDecoder.ManualText = string.Empty;
            labelStopped.Text = string.Empty;
            labelAudioFileRemoved.Text = string.Empty;
            EnableListViewDoubleBuffering();

            LoadSettings();
            LoadEncoders();
            LoadAudioFiles();
            LoadJobs();

            // Apply auto-width to all log tabs
            foreach (TabPage tab in new[] { DetectDupes, TestForErrors, Benchmark })
            {
                tabControlLog.SelectedTab = tab;
                Application.DoEvents(); // Ensure the tab is rendered

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

            this.ActiveControl = null; // Remove focus from all elements
        }
        private void Form1_FormClosing(object? sender, FormClosingEventArgs e)
        {
            // Save user settings and lists
            SaveSettings();        // General settings
            SaveEncoders();        // Encoder list
            SaveAudioFiles();      // Audio files list
            SaveJobs();            // Job list

            // Stop and dispose UI timers and performance counters
            cpuUsageTimer?.Stop();
            cpuUsageTimer?.Dispose();
            temporaryMessageTimer?.Stop();
            temporaryMessageTimer?.Dispose();
            cpuLoadCounter?.Dispose();

            // Dispose pause/resume synchronization object
            _pauseEvent?.Dispose();

            // Optionally clean up temporary folder
            if (checkBoxClearTempFolder.Checked)
            {
                try
                {
                    if (Directory.Exists(tempFolderPath))
                    {
                        // Delete entire folder with all contents
                        Directory.Delete(tempFolderPath, true);
                    }
                }
                catch (Exception ex)
                {
                    // Log or silently ignore - app is closing anyway
                    Debug.WriteLine($"Failed to delete temp folder: {ex.Message}");
                }
            }
        }
    }
}