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
        public string programVersionCurrent = "1.7.2 build 20250921"; // Current app version
        public string programVersionIgnored = null;                   // Previously ignored update

        // Hardware info
        private int physicalCores; // Number of physical CPU cores
        private int threadCount;   // Total logical threads

        // CPU monitoring
        private PerformanceCounter cpuLoadCounter = null;       // CPU Load counter (whole system)
        private bool performanceCountersAvailable = false;      // True if counters initialized
        private System.Windows.Forms.Timer cpuUsageTimer;       // Updates CPU usage label

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

            tempFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp"); // Initialize the path to the temporary folder
            _process = new Process(); // Initialize _process to avoid nullability warning

            dataGridViewLog.CellContentClick += dataGridViewLog_CellContentClick;
            dataGridViewLog.MouseDown += dataGridViewLog_MouseDown;
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

        // Method to save settings to .txt files
        private void SaveSettings()
        {
            try
            {
                var settings = new[]
                {
                    $"CompressionLevel={textBoxCompressionLevel.Text}",
                    $"Threads={textBoxThreads.Text}",
                    $"CommandLineOptionsEncoder={textBoxCommandLineOptionsEncoder.Text}",
                    $"CommandLineOptionsDecoder={textBoxCommandLineOptionsDecoder.Text}",
                    $"CPUPriority={comboBoxCPUPriority.SelectedItem}",
                    $"TempFolderPath={tempFolderPath}",
                    $"ClearTempFolderOnExit={checkBoxClearTempFolder.Checked}",
                    $"RemoveMetadata={checkBoxRemoveMetadata.Checked}",
                    $"AddMD5OnLoadWav={checkBoxAddMD5OnLoadWav.Checked}",
                    $"AddWarmupPass={checkBoxWarmupPass.Checked}",
                    $"WarningsAsErrors={checkBoxWarningsAsErrors.Checked}",
                    $"AutoAnalyzeLog={checkBoxAutoAnalyzeLog.Checked}",
                    $"PreventSleep={checkBoxPreventSleep.Checked}",
                    $"CheckForUpdatesOnStartup={checkBoxCheckForUpdatesOnStartup.Checked}",
                    $"IgnoredVersion={programVersionIgnored ?? ""}"
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
                .Select(item => $"{item.Tag}~{item.Checked}")
                .ToArray();
                File.WriteAllLines(SettingsEncodersFilePath, encoders);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving encoders: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void SaveAudioFiles()
        {
            try
            {
                var audioFiles = listViewAudioFiles.Items
                .Cast<ListViewItem>()
                .Select(item => $"{item.Tag}~{item.Checked}")
                .ToArray();
                File.WriteAllLines(SettingsAudioFilesFilePath, audioFiles);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving audio files: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void SaveJobs()
        {
            try
            {
                var jobList = listViewJobs.Items.Cast<ListViewItem>()
                .Select(item => $"{item.Text}~{item.Checked}~{item.SubItems[1].Text}~{item.SubItems[2].Text}") // Save text, checkbox state, number of passes, and parameters
                .ToArray();
                File.WriteAllLines(SettingsJobsFilePath, jobList);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving jobs to file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Method to load settings from .txt files
        private void LoadSettings()
        {
            // Load the path to the temporary folder
            tempFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp");
            try
            {
                string[] lines = File.ReadAllLines(SettingsGeneralFilePath);
                foreach (var line in lines)
                {
                    var parts = line.Split(new[] { '=' }, 2); // Split the line into key and value, limit separation to the first '=' sign
                    if (parts.Length == 2)
                    {
                        var key = parts[0].Trim();
                        var value = parts[1].Trim();
                        // Load values into corresponding fields
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
                                tempFolderPath = value;
                                break;
                            case "ClearTempFolderOnExit":
                                checkBoxClearTempFolder.Checked = bool.Parse(value);
                                break;
                            case "RemoveMetadata":
                                checkBoxRemoveMetadata.Checked = bool.Parse(value);
                                break;
                            case "AddMD5OnLoadWav":
                                checkBoxAddMD5OnLoadWav.Checked = bool.Parse(value);
                                break;
                            case "AddWarmupPass":
                                checkBoxWarmupPass.Checked = bool.Parse(value);
                                break;
                            case "WarningsAsErrors":
                                checkBoxWarningsAsErrors.Checked = bool.Parse(value);
                                break;
                            case "AutoAnalyzeLog":
                                checkBoxAutoAnalyzeLog.Checked = bool.Parse(value);
                                break;
                            case "PreventSleep":
                                checkBoxPreventSleep.Checked = bool.Parse(value);
                                break;
                            case "CheckForUpdatesOnStartup":
                                checkBoxCheckForUpdatesOnStartup.Checked = bool.Parse(value);
                                break;
                            case "IgnoredVersion":
                                programVersionIgnored = value;
                                break;
                        }
                    }
                }
            }
            catch
            {
            }
        }
        private async void LoadEncoders()
        {
            if (File.Exists(SettingsEncodersFilePath))
            {
                try
                {
                    // Read all lines from the file
                    string[] lines = await File.ReadAllLinesAsync(SettingsEncodersFilePath);
                    listViewEncoders.Items.Clear(); // Clear the ListView

                    var missingFiles = new List<string>(); // List of missing files
                    var tasks = lines.Select(async line =>
                    {
                        var parts = line.Split('~');
                        if (parts.Length == 2)
                        {
                            string encoderPath = parts[0];
                            bool isChecked = bool.Parse(parts[1]); // Read the "checked" status

                            // Check if the file exists
                            if (!string.IsNullOrEmpty(encoderPath) && File.Exists(encoderPath))
                            {
                                // Create a ListViewItem
                                var item = await Task.Run(() => CreateListViewEncodersItem(encoderPath, isChecked));
                                if (item != null)
                                {
                                    item.Checked = isChecked; // Set the checkbox status
                                    return item; // Return the created item
                                }
                            }
                            else
                            {
                                missingFiles.Add(encoderPath); // Add the path to the missing list
                            }
                        }
                        return null;// Return null if the item couldn't be created
                    });

                    var items = await Task.WhenAll(tasks); // Wait for all tasks to complete

                    // Add only non-null items to the ListView
                    foreach (var item in items)
                    {
                        if (item != null)
                        {
                            listViewEncoders.Items.Add(item); // Add the item to the ListView
                        }
                    }

                    // Show a warning about skipped files
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
                    MessageBox.Show($"Error loading encoders: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                UpdateGroupBoxEncodersHeader();
            }
        }
        private async void LoadAudioFiles()
        {
            if (File.Exists(SettingsAudioFilesFilePath))
            {
                try
                {
                    // Read all lines from the file
                    string[] lines = await File.ReadAllLinesAsync(SettingsAudioFilesFilePath);
                    listViewAudioFiles.Items.Clear(); // Clear the ListView

                    var missingFiles = new List<string>(); // List of missing files
                    var tasks = lines.Select(async line =>
                    {
                        var parts = line.Split('~');
                        if (parts.Length == 2)
                        {
                            string audioFilePath = parts[0];
                            bool isChecked = bool.Parse(parts[1]); // Read the "checked" status

                            // Check if the file exists
                            if (!string.IsNullOrEmpty(audioFilePath) && File.Exists(audioFilePath))
                            {
                                // Create a ListViewItem
                                var item = await Task.Run(() => CreateListViewAudioFilesItem(audioFilePath, isChecked));
                                if (item != null)
                                {
                                    item.Checked = isChecked; // Set the checkbox status
                                    return item; // Return the created item
                                }
                            }
                            else
                            {
                                missingFiles.Add(audioFilePath); // Add the path to the missing list
                            }
                        }
                        return null; // Return null if the item couldn't be created
                    });

                    var items = await Task.WhenAll(tasks); // Wait for all tasks to complete

                    // Add only non-null items to the ListView
                    foreach (var item in items)
                    {
                        if (item != null)
                        {
                            listViewAudioFiles.Items.Add(item);
                        }
                    }

                    // Show a warning about skipped files
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
                    MessageBox.Show($"Error loading audio files: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                UpdateGroupBoxAudioFilesHeader();
            }
        }
        private void LoadJobs()
        {
            BackupJobsFile();
            if (File.Exists(SettingsJobsFilePath))
            {
                try
                {
                    string[] lines = File.ReadAllLines(SettingsJobsFilePath);
                    listViewJobs.Items.Clear(); // Clear the list before loading

                    foreach (var line in lines)
                    {
                        var parts = line.Split('~');
                        if (parts.Length == 4 && bool.TryParse(parts[1], out bool isChecked))
                        {
                            var item = new ListViewItem(NormalizeSpaces(parts[0])) { Checked = isChecked };
                            item.SubItems.Add(NormalizeSpaces(parts[2]));
                            item.SubItems.Add(NormalizeSpaces(parts[3]));
                            listViewJobs.Invoke(new Action(() => listViewJobs.Items.Add(item)));
                        }
                        else
                        {
                            MessageBox.Show($"Invalid line format: {line}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading jobs from file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
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

        private async Task<string> CalculateWavMD5Async(string audioFilePath)
        {
            return await Task.Run(() =>
            {
                try
                {
                    using (var stream = File.OpenRead(audioFilePath))
                    {
                        using (var md5 = MD5.Create())
                        {
                            using (BinaryReader reader = new BinaryReader(stream))
                            {
                                // Check RIFF header
                                if (reader.ReadUInt32() != 0x46464952) // "RIFF"
                                {
                                    string errorMessage = "Invalid WAV file: Missing RIFF header.";
                                    UpdateCacheWithMD5Error(audioFilePath, errorMessage);
                                    return "MD5 calculation failed";
                                }

                                reader.ReadUInt32(); // Read total file size (not used)

                                if (reader.ReadUInt32() != 0x45564157) // "WAVE"
                                {
                                    string errorMessage = "Invalid WAV file: Missing WAVE header.";
                                    UpdateCacheWithMD5Error(audioFilePath, errorMessage);
                                    return "MD5 calculation failed";
                                }

                                // Read chunks
                                while (reader.BaseStream.Position < reader.BaseStream.Length)
                                {
                                    uint chunkId = reader.ReadUInt32();
                                    uint chunkSize = reader.ReadUInt32();

                                    if (chunkId == 0x20746D66) // "fmt "
                                    {
                                        // Skip "fmt " chunk
                                        reader.BaseStream.Seek(chunkSize, SeekOrigin.Current);
                                    }
                                    else if (chunkId == 0x61746164) // "data"
                                    {
                                        // Check if the data chunk size is valid and within file bounds
                                        // This replaces the old check for int.MaxValue
                                        if (reader.BaseStream.Position + chunkSize > reader.BaseStream.Length)
                                        {
                                            string errorMessage = "Invalid WAV file: 'data' chunk size exceeds file bounds.";
                                            UpdateCacheWithMD5Error(audioFilePath, errorMessage);
                                            return "MD5 calculation failed";
                                        }

                                        // Process large data chunks by streaming in blocks
                                        // This approach avoids loading the entire data chunk into memory at once,
                                        // allowing processing of very large WAV files (e.g., > 2GB).
                                        long bytesToRead = chunkSize; // Use long to handle large chunk sizes
                                        byte[] buffer = new byte[8192]; // Buffer size for reading blocks
                                        long totalBytesRead = 0;

                                        // Read and process the data chunk in blocks
                                        while (totalBytesRead < bytesToRead)
                                        {
                                            // Calculate how many bytes to attempt reading in this iteration
                                            // Ensures we don't read past the end of the data chunk
                                            int bytesToReadThisIteration = (int)Math.Min(buffer.Length, bytesToRead - totalBytesRead);

                                            // Read a block of data
                                            int bytesRead = reader.Read(buffer, 0, bytesToReadThisIteration);

                                            // Check for unexpected end of stream
                                            if (bytesRead == 0)
                                            {
                                                string errorMessage = "Unexpected end of file while reading 'data' chunk.";
                                                UpdateCacheWithMD5Error(audioFilePath, errorMessage);
                                                return "MD5 calculation failed";
                                            }

                                            // Update the MD5 hash with the bytes actually read
                                            // TransformBlock is used for incremental hashing
                                            md5.TransformBlock(buffer, 0, bytesRead, null, 0);
                                            totalBytesRead += bytesRead;
                                        }

                                        // Finalize the MD5 hash calculation after all data is processed
                                        // TransformFinalBlock with empty array signals the end of data
                                        md5.TransformFinalBlock(new byte[0], 0, 0);
                                        byte[] hash = md5.Hash;

                                        // Convert the hash bytes to a hexadecimal string
                                        string md5Hash = BitConverter.ToString(hash).Replace("-", "").ToUpperInvariant();

                                        // Update the cache with the calculated hash.
                                        if (audioInfoCache.TryGetValue(audioFilePath, out var cachedInfoWav))
                                        {
                                            // If file exists in cache, update its MD5 hash.
                                            cachedInfoWav.Md5Hash = md5Hash; // Store the actual hash

                                            // Clear any previous error details for this file as calculation was successful.
                                            cachedInfoWav.ErrorDetails = null; // Or string.Empty
                                        }
                                        else
                                        {
                                            // If the file is not yet in the cache, create a new AudioFileInfo.
                                            // This scenario is less likely if GetAudioInfo was called first.
                                            var newInfo = new AudioFileInfo
                                            {
                                                FilePath = audioFilePath,
                                                Md5Hash = md5Hash, // Store the actual hash
                                                FileName = Path.GetFileName(audioFilePath),
                                                DirectoryPath = Path.GetDirectoryName(audioFilePath),
                                                ErrorDetails = null // No error details for successful calculation
                                                                    // Other properties (Duration, BitDepth etc.) would typically be populated by GetAudioInfo
                                            };
                                            audioInfoCache.TryAdd(audioFilePath, newInfo);
                                        }

                                        return md5Hash; // Return the actual hash
                                    }
                                    else
                                    {
                                        // Skip chunk
                                        reader.BaseStream.Seek(chunkSize, SeekOrigin.Current);
                                    }
                                }
                            }
                        }
                    }

                    // If we reach here, no "data" chunk was found
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
            });
        }
        private async Task<string> CalculateFlacMD5Async(string flacFilePath)
        {
            return await Task.Run(async () =>
            {
                try
                {
                    string encoderExePath = null;
                    string errorMessageDetails = null; // To capture specific error details

                    // Get the path to the encoder from the UI thread
                    await this.InvokeAsync(() =>
                    {
                        var encoderItem = listViewEncoders.Items
                        .Cast<ListViewItem>()
                        .FirstOrDefault(item =>
                        Path.GetExtension(item.Text).Equals(".exe", StringComparison.OrdinalIgnoreCase));

                        encoderExePath = encoderItem?.Tag?.ToString();
                    });

                    if (string.IsNullOrEmpty(encoderExePath) || !File.Exists(encoderExePath))
                    {
                        errorMessageDetails = "No .exe encoder found in the list";
                        // Update cache and return standardized error status
                        UpdateCacheWithMD5Error(flacFilePath, errorMessageDetails);
                        return "MD5 calculation failed";
                    }

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

                    // Create a unique temporary WAV file path to prevent conflicts during parallel processing
                    string tempWavFile = Path.Combine(tempFolderPath, $"temp_flac_md5_{Guid.NewGuid()}.wav");

                    // Build the command line for decoding
                    string arguments = $"\"{flacFilePath}\" -d --no-preserve-modtime --silent -f -o \"{tempWavFile}\"";

                    using (var process = new Process())
                    {
                        process.StartInfo.FileName = encoderExePath;
                        process.StartInfo.Arguments = arguments;
                        process.StartInfo.UseShellExecute = false;
                        process.StartInfo.RedirectStandardError = true;
                        process.StartInfo.CreateNoWindow = true;

                        process.Start();

                        string errorOutput = await process.StandardError.ReadToEndAsync();
                        await process.WaitForExitAsync();

                        // Check if the process completed successfully
                        if (process.ExitCode != 0)
                        {
                            errorMessageDetails = $"Decode failed: {errorOutput.Trim()}";
                            UpdateCacheWithMD5Error(flacFilePath, errorMessageDetails);
                            return "MD5 calculation failed";
                        }

                        // Verify that the temporary WAV file was created
                        if (!File.Exists(tempWavFile))
                        {
                            errorMessageDetails = "Temporary WAV file was not created";
                            UpdateCacheWithMD5Error(flacFilePath, errorMessageDetails);
                            return "MD5 calculation failed";
                        }

                        // Calculate MD5 for the temporary WAV file
                        string wavMd5Result = CalculateWavMD5Async(tempWavFile).Result;

                        // Clean up: delete the temporary file
                        try
                        {
                            File.Delete(tempWavFile);
                        }
                        catch { }

                        // Check if calculating MD5 for the decoded WAV produced an error.
                        if (wavMd5Result == "MD5 calculation failed")
                        {
                            // The decoding by flac.exe likely produced a corrupt or unexpected WAV.
                            // The error details for the temp WAV file's MD5 calculation are in its cache entry.
                            string tempWavErrorDetails = "Unknown error during MD5 calculation of decoded WAV";
                            if (audioInfoCache.TryGetValue(tempWavFile, out var tempWavCachedInfo) && !string.IsNullOrEmpty(tempWavCachedInfo.ErrorDetails))
                            {
                                tempWavErrorDetails = $"MD5 calculation of decoded WAV failed: {tempWavCachedInfo.ErrorDetails}";
                            }
                            // Propagate this specific error up for the original FLAC file.
                            UpdateCacheWithMD5Error(flacFilePath, tempWavErrorDetails);
                            return "MD5 calculation failed";
                        }

                        // If wavMd5Result is a valid hash, proceed.
                        string finalMd5Hash = wavMd5Result;

                        // Update the cache for the ORIGINAL FLAC file with the final result.
                        if (audioInfoCache.TryGetValue(flacFilePath, out var cachedInfoFlac))
                        {
                            // If the original FLAC file exists in the cache, update its MD5 hash.
                            cachedInfoFlac.Md5Hash = finalMd5Hash; // Store the actual hash
                                                                   // Clear any previous error details for this file as calculation was successful.
                            cachedInfoFlac.ErrorDetails = null; // Or string.Empty
                        }
                        else
                        {
                            // If the original FLAC file is not yet in the cache — create a new AudioFileInfo.
                            var newInfo = new AudioFileInfo
                            {
                                FilePath = flacFilePath,
                                Md5Hash = finalMd5Hash, // Store the actual hash
                                FileName = Path.GetFileName(flacFilePath),
                                DirectoryPath = Path.GetDirectoryName(flacFilePath),
                                ErrorDetails = null // No error details for successful calculation
                                                    // Other properties (Duration, BitDepth etc.) would typically be populated by GetAudioInfo
                            };
                            audioInfoCache.TryAdd(flacFilePath, newInfo);
                        }

                        // Return the calculated MD5 hash
                        return finalMd5Hash;
                    }
                }
                catch (Exception ex)
                {
                    string errorMessageDetails = $"Error: {ex.Message}";
                    UpdateCacheWithMD5Error(flacFilePath, errorMessageDetails);
                    return "MD5 calculation failed";
                }
            });
        }
        private void UpdateCacheWithMD5Error(string filePath, string errorDetails)
        {
            if (audioInfoCache.TryGetValue(filePath, out var cachedInfoError))
            {
                // If file exists in cache, update its status and error details.
                cachedInfoError.Md5Hash = "MD5 calculation failed"; // Store the standardized error status
                cachedInfoError.ErrorDetails = errorDetails; // Store the full error message/details
            }
            else
            {
                // If the file is not yet in the cache, create a new AudioFileInfo.
                var newInfo = new AudioFileInfo
                {
                    FilePath = filePath,
                    Md5Hash = "MD5 calculation failed", // Store the standardized error status
                    FileName = Path.GetFileName(filePath),
                    DirectoryPath = Path.GetDirectoryName(filePath),
                    ErrorDetails = errorDetails // Store the full error message/details
                };
                audioInfoCache.TryAdd(filePath, newInfo);
            }
        }
        private async void buttonDetectDupesAudioFiles_Click(object? sender, EventArgs e)
        {
            // --- STAGE 0: PREPARE USER INTERFACE ---
            var button = (Button)sender;
            var originalText = button.Text;

            try
            {
                // Disable the button and show a progress indicator.
                button.Text = "In progress...";
                button.Enabled = false;

                // --- STAGE 0.1: CHECK FILE EXISTENCE AND CLEAN UP LISTVIEW ---
                // Run this part in the UI thread as it modifies UI elements.
                var itemsToRemove = new List<ListViewItem>();
                this.Invoke((MethodInvoker)delegate
                {
                    foreach (ListViewItem item in listViewAudioFiles.Items)
                    {
                        string filePath = item.Tag.ToString();
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

                // --- STAGE 0.2: COLLECT FILE PATHS ---
                // Run this part in the UI thread to safely access ListView items.
                var filePaths = new List<string>();
                this.Invoke((MethodInvoker)delegate
                {
                    filePaths.AddRange(listViewAudioFiles.Items.Cast<ListViewItem>().Select(item => item.Tag.ToString()));
                });

                // --- STAGE 1: PERFORM DUPLICATE DETECTION IN BACKGROUND THREAD ---
                await Task.Run(async () =>
                {
                    var hashDict = new Dictionary<string, List<string>>(); // Group files by MD5 hash.
                    var filesWithMD5Errors = new List<string>(); // Track paths of files with MD5 errors.

                    // --- STAGE 1.1: CALCULATE OR RETRIEVE MD5 HASHES ---
                    foreach (string filePath in filePaths)
                    {
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

                            // Update the cache with the new MD5 hash.
                            if (audioInfoCache.TryGetValue(filePath, out var infoToUpdate))
                            {
                                infoToUpdate.Md5Hash = md5Hash;
                                audioInfoCache[filePath] = infoToUpdate;
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

                    // --- STAGE 1.2: DETERMINE PRIMARY DUPLICATE IN EACH GROUP ---
                    var itemsToCheck = new List<string>();   // Paths of files to mark as checked (primary).
                    var itemsToUncheck = new List<string>(); // Paths of files to mark as unchecked (non-primary duplicates).

                    foreach (var kvp in hashDict)
                    {
                        if (kvp.Value.Count > 1) // Process only actual duplicate groups.
                        {
                            // Sort by: .flac > .wav, shorter path, newer file, then alphabetically.
                            var sortedPaths = kvp.Value
                                .Select(path => new { Path = path, Info = audioInfoCache.TryGetValue(path, out var info) ? info : null })
                                .Where(x => x.Info != null)
                                .OrderBy(x => x.Info.Extension == ".flac" ? 0 : 1)
                                .ThenBy(x => x.Info.DirectoryPath?.Length ?? int.MaxValue)
                                .ThenByDescending(x => x.Info.LastWriteTime)
                                .ThenBy(x => x.Path)
                                .ToList();

                            if (sortedPaths.Count > 0)
                            {
                                itemsToCheck.Add(sortedPaths[0].Path); // Primary file.
                                itemsToUncheck.AddRange(sortedPaths.Skip(1).Select(x => x.Path)); // Others.
                            }
                        }
                    }

                    // --- STAGE 2: UPDATE USER INTERFACE ---
                    // All UI modifications must happen on the UI thread.
                    this.Invoke((MethodInvoker)delegate
                    {
                        // --- STAGE 2.1: CLEAR PREVIOUS RESULTS FROM LOG ---
                        for (int i = dataGridViewLog.Rows.Count - 1; i >= 0; i--)
                        {
                            DataGridViewRow row = dataGridViewLog.Rows[i];
                            if (row.Cells["MD5"].Value?.ToString() == "MD5 calculation failed" ||
                                !string.IsNullOrEmpty(row.Cells["Duplicates"].Value?.ToString()))
                            {
                                dataGridViewLog.Rows.RemoveAt(i);
                            }
                        }

                        // --- STAGE 2.2: UPDATE CHECKBOX STATES IN LISTVIEW ---
                        // STEP 1: Initialize all items as checked.
                        foreach (ListViewItem item in listViewAudioFiles.Items)
                        {
                            item.Checked = true;
                        }

                        // STEP 2: Uncheck non-primary duplicates.
                        foreach (ListViewItem item in listViewAudioFiles.Items)
                        {
                            string path = item.Tag.ToString();
                            if (itemsToUncheck.Contains(path))
                            {
                                item.Checked = false;
                            }
                        }

                        // --- STAGE 2.3: UPDATE MD5 DISPLAY IN LISTVIEW ---
                        foreach (ListViewItem item in listViewAudioFiles.Items)
                        {
                            string path = item.Tag.ToString();
                            if (audioInfoCache.TryGetValue(path, out var info))
                            {
                                item.SubItems[5].Text = info.Md5Hash;
                            }
                        }

                        // --- STAGE 2.4: LOG MD5 CALCULATION ERROR RESULTS ---
                        // Add entries for files where MD5 calculation failed to the log grid.
                        foreach (string filePath in filesWithMD5Errors)
                        {
                            // Retrieve file metadata from the cache.
                            if (audioInfoCache.TryGetValue(filePath, out var info))
                            {
                                string fileName = info.FileName;
                                string directoryPath = info.DirectoryPath;
                                // The specific error details are stored in the cache.
                                string errorMessage = info.ErrorDetails ?? string.Empty;

                                int rowIndex = dataGridViewLog.Rows.Add(
                                    fileName,                   //  0: Name
                                    string.Empty,               //  1: BitDepth
                                    string.Empty,               //  2: SamplingRate
                                    string.Empty,               //  3: InputFileSize
                                    string.Empty,               //  4: OutputFileSize
                                    string.Empty,               //  5: Compression
                                    string.Empty,               //  6: Time
                                    string.Empty,               //  7: Speed
                                    string.Empty,               //  8: SpeedMin
                                    string.Empty,               //  9: SpeedMax
                                    string.Empty,               // 10: SpeedRange
                                    string.Empty,               // 11: SpeedConsistency
                                    string.Empty,               // 12: CPULoadEncoder
                                    string.Empty,               // 13: CPUClock
                                    string.Empty,               // 14: Passes
                                    string.Empty,               // 15: Parameters
                                    string.Empty,               // 16: Encoder
                                    string.Empty,               // 17: Version
                                    string.Empty,               // 18: Encoder directory path
                                    string.Empty,               // 19: FastestEncoder
                                    string.Empty,               // 20: BestSize
                                    string.Empty,               // 21: SameSize
                                    directoryPath,              // 22: AudioFileDirectory
                                    "MD5 calculation failed",   // 23: MD5
                                    string.Empty,               // 24: Duplicates
                                    errorMessage                // 25: Errors
                                );

                                // Highlight MD5 error rows in gray for visibility.
                                dataGridViewLog.Rows[rowIndex].DefaultCellStyle.ForeColor = Color.Gray;
                            }
                        }

                        // --- STAGE 2.5: LOG DUPLICATE GROUPS ---
                        // Add entries for each file found in duplicate groups to the log grid.
                        var logEntries = new List<(string FileName, string DirectoryPath, string Md5, string Duplicates)>();
                        foreach (var kvp in hashDict.Where(g => g.Value.Count > 1))
                        {
                            string duplicatesList = string.Join(", ", kvp.Value.Select(path =>
                                audioInfoCache.TryGetValue(path, out var info) ? info.FileName : Path.GetFileName(path)
                            ));

                            foreach (string path in kvp.Value)
                            {
                                if (audioInfoCache.TryGetValue(path, out var info))
                                {
                                    logEntries.Add((
                                        info.FileName,
                                        info.DirectoryPath,
                                        kvp.Key,
                                        duplicatesList
                                    ));
                                }
                            }
                        }

                        foreach (var entry in logEntries)
                        {
                            // Data for 'entry' is pre-fetched from the cache in STAGE 1.2.
                            string fileName = entry.FileName;
                            string directoryPath = entry.DirectoryPath;
                            string md5Hash = entry.Md5;
                            string duplicatesList = entry.Duplicates;
                            // No specific error message for a duplicate entry itself.
                            string errorMessage = string.Empty;

                            int rowIndex = dataGridViewLog.Rows.Add(
                                fileName,            //  0: Name
                                string.Empty,        //  1: BitDepth
                                string.Empty,        //  2: SamplingRate
                                string.Empty,        //  3: InputFileSize
                                string.Empty,        //  4: OutputFileSize
                                string.Empty,        //  5: Compression
                                string.Empty,        //  6: Time
                                string.Empty,        //  7: Speed
                                string.Empty,        //  8: SpeedMin
                                string.Empty,        //  9: SpeedMax
                                string.Empty,        // 10: SpeedRange
                                string.Empty,        // 11: SpeedConsistency
                                string.Empty,        // 12: CPULoadEncoder
                                string.Empty,        // 13: CPUClock
                                string.Empty,        // 14: Passes
                                string.Empty,        // 15: Parameters
                                string.Empty,        // 16: Encoder
                                string.Empty,        // 17: Version
                                string.Empty,        // 18: Encoder directory path
                                string.Empty,        // 19: FastestEncoder
                                string.Empty,        // 20: BestSize
                                string.Empty,        // 21: SameSize
                                directoryPath,       // 22: AudioFileDirectory
                                md5Hash,             // 23: MD5
                                duplicatesList,      // 24: Duplicates
                                errorMessage         // 25: Errors
                            );

                            // Highlight duplicate rows in brown for visibility.
                            dataGridViewLog.Rows[rowIndex].DefaultCellStyle.ForeColor = Color.Brown;
                        }

                        // --- STAGE 2.6: MANAGE LOG COLUMN VISIBILITY ---
                        // Show columns only if they contain data.
                        dataGridViewLog.Columns["Duplicates"].Visible = dataGridViewLog.Rows
                            .Cast<DataGridViewRow>()
                            .Any(row => row.Cells["Duplicates"].Value != null && !string.IsNullOrEmpty(row.Cells["Duplicates"].Value.ToString()));

                        dataGridViewLog.Columns["Errors"].Visible = dataGridViewLog.Rows
                            .Cast<DataGridViewRow>()
                            .Any(row => row.Cells["Errors"].Value != null && !string.IsNullOrEmpty(row.Cells["Errors"].Value.ToString()));

                        // --- STAGE 2.7: REORDER DUPLICATE GROUPS IN LISTVIEW ---
                        var sortedGroups = hashDict
                            .Where(kvp => kvp.Value.Count > 1)
                            .OrderBy(kvp =>
                            {
                                var primaryPath = itemsToCheck.FirstOrDefault(p => kvp.Value.Contains(p));
                                return primaryPath ?? kvp.Value.First();
                            })
                            .ToList();

                        var allListViewItems = listViewAudioFiles.Items.Cast<ListViewItem>().ToList();

                        listViewAudioFiles.BeginUpdate();
                        try
                        {
                            // Process groups in reverse order for correct final positioning.
                            for (int groupIndex = sortedGroups.Count - 1; groupIndex >= 0; groupIndex--)
                            {
                                var kvp = sortedGroups[groupIndex];
                                // Find ListViewItem objects by comparing their Tag.
                                var groupItems = allListViewItems
                                    .Where(item => kvp.Value.Contains(item.Tag.ToString()))
                                    .ToList();

                                var primaryItem = groupItems.FirstOrDefault(item => itemsToCheck.Contains(item.Tag.ToString()));
                                var otherItems = groupItems.Where(item => item != primaryItem).ToList();

                                // Remove all items in the group.
                                foreach (var item in groupItems)
                                {
                                    if (listViewAudioFiles.Items.Contains(item))
                                    {
                                        listViewAudioFiles.Items.Remove(item);
                                    }
                                }

                                // Insert items at the top (index 0) in reverse order.
                                // This places 'primaryItem' visually first in the group.
                                int insertIndex = 0;
                                for (int i = otherItems.Count - 1; i >= 0; i--)
                                {
                                    listViewAudioFiles.Items.Insert(insertIndex, otherItems[i]);
                                }
                                if (primaryItem != null)
                                {
                                    listViewAudioFiles.Items.Insert(insertIndex, primaryItem);
                                }
                            }
                        }
                        finally
                        {
                            listViewAudioFiles.EndUpdate();
                        }
                    }); // End of UI update Invoke.
                }); // End of background Task.Run.
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
            }
        }
        private async void buttonTestForErrors_Click(object? sender, EventArgs e)
        {
            // --- STAGE 0: PREPARE USER INTERFACE ---
            var button = (Button)sender;
            var originalText = button.Text;

            try
            {
                // Disable the button and show a progress indicator.
                button.Text = "In progress...";
                button.Enabled = false;

                // --- STAGE 0.1: COLLECT DATA FROM UI ---
                // Run this part in the UI thread as it reads/modifies UI elements.
                List<string> flacFilePaths = new List<string>();
                string? encoderPath = null;
                bool useWarningsAsErrors = false;

                this.Invoke((MethodInvoker)delegate
                {
                    // --- Clear previous integrity error results from the log ---
                    for (int i = dataGridViewLog.Rows.Count - 1; i >= 0; i--)
                    {
                        DataGridViewRow row = dataGridViewLog.Rows[i];
                        if (row.Cells["MD5"].Value?.ToString() == "Integrity Check Failed")
                        {
                            dataGridViewLog.Rows.RemoveAt(i);
                        }
                    }

                    // --- Check if files physically exist on disk ---
                    var itemsToRemove = new List<ListViewItem>();
                    foreach (ListViewItem item in listViewAudioFiles.Items)
                    {
                        string filePath = item.Tag.ToString();
                        if (!File.Exists(filePath))
                        {
                            itemsToRemove.Add(item);
                        }
                    }
                    foreach (var itemToRemove in itemsToRemove)
                    {
                        listViewAudioFiles.Items.Remove(itemToRemove);
                    }
                    if (itemsToRemove.Count > 0)
                    {
                        UpdateGroupBoxAudioFilesHeader();
                        // Show message on UI thread
                        this.Invoke((MethodInvoker)delegate
                        {
                            ShowTemporaryAudioFileRemovedMessage($"{itemsToRemove.Count} file(s) were not found on disk and have been removed from the list.");
                        });
                    }

                    // Collect paths of all .flac files currently in the list.
                    flacFilePaths.AddRange(
                        listViewAudioFiles.Items.Cast<ListViewItem>()
                            .Where(item => Path.GetExtension(item.Tag.ToString()).Equals(".flac", StringComparison.OrdinalIgnoreCase))
                            .Select(item => item.Tag.ToString())
                    );

                    // Get the path of the first .exe file found in the encoders list.
                    var encoderItem = listViewEncoders.Items
                        .Cast<ListViewItem>()
                        .FirstOrDefault(item => Path.GetExtension(item.Text).Equals(".exe", StringComparison.OrdinalIgnoreCase));
                    encoderPath = encoderItem?.Tag?.ToString();

                    // Get the state of the 'Warnings as Errors' checkbox.
                    useWarningsAsErrors = checkBoxWarningsAsErrors.Checked;
                });

                // Validate collected data before proceeding.
                if (flacFilePaths.Count == 0)
                {
                    MessageBox.Show("No FLAC files found in the list.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                if (string.IsNullOrEmpty(encoderPath))
                {
                    MessageBox.Show("No .exe encoder found in the encoders list.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // --- STAGE 1: PERFORM INTEGRITY TESTS IN BACKGROUND THREAD ---
                List<(string FileName, string FilePath, string Message)> errorResults = new List<(string, string, string)>();
                bool allPassed = true; // Flag to track if all files passed the test.

                await Task.Run(async () =>
                {
                    var localErrorResults = new List<(string FileName, string FilePath, string Message)>();

                    // Test each FLAC file sequentially.
                    foreach (string filePath in flacFilePaths)
                    {
                        // Get the file name, preferably from the cache.
                        string fileName = audioInfoCache.TryGetValue(filePath, out var info) ? info.FileName : Path.GetFileName(filePath);

                        try
                        {
                            using (var process = new Process())
                            {
                                process.StartInfo.FileName = encoderPath;
                                // Build the command line arguments for the flac test.
                                string arguments = " --test --silent";
                                if (useWarningsAsErrors)
                                {
                                    arguments += " --warnings-as-errors";
                                }
                                arguments += $" \"{filePath}\"";

                                process.StartInfo.Arguments = arguments;
                                process.StartInfo.UseShellExecute = false;
                                process.StartInfo.RedirectStandardError = true;
                                process.StartInfo.RedirectStandardOutput = true;
                                process.StartInfo.CreateNoWindow = true;

                                process.StartInfo.StandardErrorEncoding = Encoding.UTF8;
                                process.StartInfo.StandardOutputEncoding = Encoding.UTF8;

                                process.Start();

                                // Asynchronously read output and error streams to prevent deadlocks.
                                var errorTask = process.StandardError.ReadToEndAsync();
                                var outputTask = process.StandardOutput.ReadToEndAsync();

                                await process.WaitForExitAsync();

                                string errorOutput = await errorTask;
                                string output = await outputTask;

                                // If the process exited with a non-zero code, it indicates an error or failure.
                                if (process.ExitCode != 0)
                                {
                                    string message = string.IsNullOrEmpty(errorOutput.Trim()) ? "Unknown error" : errorOutput.Trim();
                                    localErrorResults.Add((fileName, filePath, message));
                                    allPassed = false; // Mark that at least one file failed.
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            // Catch any exceptions during the process execution (e.g., file access issues).
                            localErrorResults.Add((fileName, filePath, $"Exception: {ex.Message}"));
                            allPassed = false; // Mark that at least one file failed.
                        }
                    }
                    // Transfer results from the local list (used inside Task.Run) to the outer list.
                    errorResults = localErrorResults;
                });

                // --- STAGE 2: UPDATE USER INTERFACE ---
                // All UI modifications must happen on the UI thread.
                this.Invoke((MethodInvoker)delegate
                {
                    // --- STAGE 2.1: UPDATE LOG GRID ---
                    // Ensure the 'Errors' column exists in the DataGridView.
                    if (!dataGridViewLog.Columns.Contains("Errors"))
                    {
                        var errorColumn = new DataGridViewTextBoxColumn();
                        errorColumn.Name = "Errors";
                        errorColumn.HeaderText = "Errors";
                        errorColumn.Visible = false; // Will be made visible if needed.
                        errorColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                        dataGridViewLog.Columns.Add(errorColumn);
                    }

                    // --- STAGE 2.2: LOG INTEGRITY CHECK ERROR RESULTS ---
                    // Add entries for files that failed the flac --test to the log grid.
                    foreach (var result in errorResults)
                    {
                        // Retrieve file metadata from the cache. Fallback to Path.* methods if not found (unlikely).
                        string fileName = result.FileName; // Name is already in the result.
                        string directoryPath = audioInfoCache.TryGetValue(result.FilePath, out var info) ? info.DirectoryPath : Path.GetDirectoryName(result.FilePath);
                        string errorMessage = result.Message; // Error message is already in the result.

                        int rowIndex = dataGridViewLog.Rows.Add(
                            fileName,                    //  0: Name
                            string.Empty,                //  1: BitDepth
                            string.Empty,                //  2: SamplingRate
                            string.Empty,                //  3: InputFileSize
                            string.Empty,                //  4: OutputFileSize
                            string.Empty,                //  5: Compression
                            string.Empty,                //  6: Time
                            string.Empty,                //  7: Speed
                            string.Empty,                //  8: SpeedMin
                            string.Empty,                //  9: SpeedMax
                            string.Empty,                // 10: SpeedRange
                            string.Empty,                // 11: SpeedConsistency
                            string.Empty,                // 12: CPULoadEncoder
                            string.Empty,                // 13: CPUClock
                            string.Empty,                // 14: Passes
                            string.Empty,                // 15: Parameters
                            string.Empty,                // 16: Encoder
                            string.Empty,                // 17: Version
                            string.Empty,                // 18: Encoder directory path
                            string.Empty,                // 19: FastestEncoder
                            string.Empty,                // 20: BestSize
                            string.Empty,                // 21: SameSize
                            directoryPath,               // 22: AudioFileDirectory
                            "Integrity Check Failed",    // 23: MD5 (repurposed for this check)
                            string.Empty,                // 24: Duplicates
                            errorMessage                 // 25: Errors
                        );

                        // Highlight integrity check error rows in red for visibility.
                        dataGridViewLog.Rows[rowIndex].DefaultCellStyle.ForeColor = Color.Red;
                        // Note: The error message is added to the row itself, not specifically to the 'Errors' cell,
                        // which is consistent with how other logs are added in this application.
                    }

                    // Show the "Errors" column only if there are any errors present in it.
                    // This ensures the column remains visible if other operations also added errors.
                    dataGridViewLog.Columns["Errors"].Visible = dataGridViewLog.Rows
                        .Cast<DataGridViewRow>()
                        .Any(row => row.Cells["Errors"].Value != null && !string.IsNullOrEmpty(row.Cells["Errors"].Value.ToString()));

                    // Inform the user about the overall result of the test.
                    if (allPassed)
                    {
                        MessageBox.Show("All FLAC files passed the integrity test.", "Test Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                });

            }
            catch (Exception ex)
            {
                // Handle any unexpected errors that occurred outside the main processing loop.
                this.Invoke((MethodInvoker)delegate
                {
                    MessageBox.Show($"An error occurred during the integrity test: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                });
            }
            finally
            {
                // Restore the button's original state regardless of outcome.
                if (button != null && !button.IsDisposed)
                {
                    button.Invoke((MethodInvoker)(() =>
                    {
                        button.Text = originalText;
                        button.Enabled = true;
                    }));
                }
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

        // Log
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

            // 1. Handle click on "AudioFileDirectory" column — path to the audio file directory
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

            // 2. Handle click on "EncoderDirectory" column — path to the encoder's folder
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
            dataGridViewLog.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells);
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
        private void buttonLogToExcel_Click(object? sender, EventArgs e)
        {
            // Create a new Excel file
            using (var workbook = new XLWorkbook())
            {
                // Add a new worksheet
                var worksheet = workbook.Worksheets.Add("Log Data");

                // Add column headers
                int columnCount = dataGridViewLog.Columns.Count;
                for (int i = 0; i < columnCount; i++)
                {
                    worksheet.Cell(1, i + 1).Value = dataGridViewLog.Columns[i].HeaderText;
                    worksheet.Cell(1, i + 1).Style.Font.Bold = true; // Set bold font for headers
                }

                // Add data rows
                for (int i = 0; i < dataGridViewLog.Rows.Count; i++)
                {
                    for (int j = 0; j < columnCount; j++)
                    {
                        var cellValue = dataGridViewLog.Rows[i].Cells[j].Value;

                        // Write values for file sizes
                        if (j == dataGridViewLog.Columns["InputFileSize"].Index || j == dataGridViewLog.Columns["OutputFileSize"].Index)
                        {
                            if (cellValue != null && long.TryParse(cellValue.ToString().Replace(" ", ""), out long inputFileSizeValue))
                            {
                                worksheet.Cell(i + 2, j + 1).Value = inputFileSizeValue; // Write as number
                            }
                        }
                        else if (j == dataGridViewLog.Columns["BitDepth"].Index)
                        {
                            if (cellValue != null && long.TryParse(cellValue.ToString(), out long bitDepthValue))
                            {
                                worksheet.Cell(i + 2, j + 1).Value = bitDepthValue; // Write as number
                            }
                        }
                        else if (j == dataGridViewLog.Columns["SamplingRate"].Index)
                        {
                            if (cellValue != null && long.TryParse(cellValue.ToString().Replace(" ", ""), out long samplingRateValue))
                            {
                                worksheet.Cell(i + 2, j + 1).Value = samplingRateValue; // Write as number
                            }
                        }
                        else if (j == dataGridViewLog.Columns["Compression"].Index)
                        {
                            if (cellValue != null && double.TryParse(cellValue.ToString().Replace("%", "").Trim(), out double compressionValue))
                            {
                                worksheet.Cell(i + 2, j + 1).Value = compressionValue / 100; // Write value in range 0-1
                            }
                        }
                        else if (j == dataGridViewLog.Columns["Time"].Index) // Time column processing
                        {
                            if (cellValue != null && double.TryParse(cellValue.ToString(), out double timeSpanValue))
                            {
                                worksheet.Cell(i + 2, j + 1).Value = timeSpanValue; // Write total seconds
                            }
                        }
                        else if (j == dataGridViewLog.Columns["Speed"].Index)
                        {
                            if (cellValue != null && double.TryParse(cellValue.ToString().Replace("x", "").Trim(), out double speedValue))
                            {
                                worksheet.Cell(i + 2, j + 1).Value = speedValue; // Write speed value
                            }
                        }
                        else if (j == dataGridViewLog.Columns["SpeedMin"].Index || j == dataGridViewLog.Columns["SpeedMax"].Index || j == dataGridViewLog.Columns["SpeedRange"].Index)
                        {
                            if (cellValue != null && double.TryParse(cellValue.ToString().Replace("x", "").Trim(), out double speedValue))
                            {
                                worksheet.Cell(i + 2, j + 1).Value = speedValue;
                            }
                        }
                        else if (j == dataGridViewLog.Columns["SpeedConsistency"].Index)
                        {
                            if (cellValue != null && double.TryParse(cellValue.ToString().Replace("%", "").Trim(), out double value))
                            {
                                worksheet.Cell(i + 2, j + 1).Value = value / 100;
                            }
                        }
                        else if (j == dataGridViewLog.Columns["Passes"].Index)
                        {
                            if (cellValue != null && int.TryParse(cellValue.ToString(), out int passesValue))
                            {
                                worksheet.Cell(i + 2, j + 1).Value = passesValue;
                            }
                        }
                        else if (j == dataGridViewLog.Columns["CPULoadEncoder"].Index)
                        {
                            if (cellValue != null && double.TryParse(cellValue.ToString().Replace("%", "").Trim(), out double cpuLoadValue))
                            {
                                worksheet.Cell(i + 2, j + 1).Value = cpuLoadValue / 100; // Write as percentage (0-1)
                            }
                        }
                        else if (j == dataGridViewLog.Columns["CPUClock"].Index)
                        {
                            if (cellValue != null && double.TryParse(cellValue.ToString().Replace("MHz", "").Trim(), out double clockValue))
                            {
                                worksheet.Cell(i + 2, j + 1).Value = clockValue; // Write as number (MHz)
                            }
                        }
                        else if (j == dataGridViewLog.Columns["EncoderDirectory"].Index)
                        {
                            string path = cellValue?.ToString() ?? string.Empty;
                            var cell = worksheet.Cell(i + 2, j + 1);
                            cell.Value = path;

                            if (Directory.Exists(path))
                            {
                                cell.SetHyperlink(new XLHyperlink(path));
                            }
                        }
                        else if (j == dataGridViewLog.Columns["AudioFileDirectory"].Index)
                        {
                            string path = cellValue?.ToString() ?? string.Empty;
                            var cell = worksheet.Cell(i + 2, j + 1);
                            cell.Value = path;

                            if (Directory.Exists(path))
                            {
                                cell.SetHyperlink(new XLHyperlink(path));
                            }
                        }
                        else if (j == dataGridViewLog.Columns["Parameters"].Index) // Parameters column processing
                        {
                            worksheet.Cell(i + 2, j + 1).Value = cellValue?.ToString() ?? string.Empty; // Write value as text
                        }
                        else
                        {
                            worksheet.Cell(i + 2, j + 1).Value = cellValue?.ToString() ?? string.Empty; // Write value as string
                        }

                        // Copy text color if set
                        if (dataGridViewLog.Rows[i].Cells[j].Style.ForeColor != Color.Empty)
                        {
                            var color = dataGridViewLog.Rows[i].Cells[j].Style.ForeColor;
                            worksheet.Cell(i + 2, j + 1).Style.Font.FontColor = XLColor.FromArgb(color.A, color.R, color.G, color.B);
                        }
                    }
                }

                // Set format for BitDepth column
                int bitDepthIndex = dataGridViewLog.Columns["BitDepth"].Index + 1;
                worksheet.Column(bitDepthIndex).Style.NumberFormat.Format = "0"; // Integer, no decimals

                // Set format for Sampling Rate column
                int samplingRateIndex = dataGridViewLog.Columns["SamplingRate"].Index + 1;
                worksheet.Column(samplingRateIndex).Style.NumberFormat.Format = "#,##0"; // Integer format with separators;

                // Set number format with thousands separator for file size columns
                int inputFileSizeIndex = dataGridViewLog.Columns["InputFileSize"].Index + 1; // +1 for 1-based indexes
                worksheet.Column(inputFileSizeIndex).Style.NumberFormat.Format = "#,##0"; // Integer format with separators

                int outputFileSizeIndex = dataGridViewLog.Columns["OutputFileSize"].Index + 1; // +1 for 1-based indexes
                worksheet.Column(outputFileSizeIndex).Style.NumberFormat.Format = "#,##0"; // Integer format with separators

                // Set format for Compression column as percentage
                int compressionIndex = dataGridViewLog.Columns["Compression"].Index + 1; // +1 for 1-based indexes
                worksheet.Column(compressionIndex).Style.NumberFormat.Format = "0.000%"; // Number format with 3 decimal places

                // Set format for Time column
                int timeIndex = dataGridViewLog.Columns["Time"].Index + 1; // +1 for 1-based indexes
                worksheet.Column(timeIndex).Style.NumberFormat.Format = "0.000"; // Format for displaying time

                // Set format for Speed column
                int speedIndex = dataGridViewLog.Columns["Speed"].Index + 1; // +1 for 1-based indexes
                worksheet.Column(speedIndex).Style.NumberFormat.Format = "0.000"; // Format for displaying speed

                int speedMinIndex = dataGridViewLog.Columns["SpeedMin"].Index + 1;
                worksheet.Column(speedMinIndex).Style.NumberFormat.Format = "0.000";

                int speedMaxIndex = dataGridViewLog.Columns["SpeedMax"].Index + 1;
                worksheet.Column(speedMaxIndex).Style.NumberFormat.Format = "0.000";

                int speedRangeIndex = dataGridViewLog.Columns["SpeedRange"].Index + 1;
                worksheet.Column(speedRangeIndex).Style.NumberFormat.Format = "0.000";

                int speedConsistencyIndex = dataGridViewLog.Columns["SpeedConsistency"].Index + 1;
                worksheet.Column(speedConsistencyIndex).Style.NumberFormat.Format = "0.000%";

                // Set format for CPULoadEncoder column
                int cpuLoadEncoderIndex = dataGridViewLog.Columns["CPULoadEncoder"].Index + 1;
                worksheet.Column(cpuLoadEncoderIndex).Style.NumberFormat.Format = "0.000%";

                // Set format for CPUClock column
                int cpuClockIndex = dataGridViewLog.Columns["CPUClock"].Index + 1;
                worksheet.Column(cpuClockIndex).Style.NumberFormat.Format = "0"; // Integer MHz

                // Set format for Passes column
                int passesIndex = dataGridViewLog.Columns["Passes"].Index + 1;
                worksheet.Column(passesIndex).Style.NumberFormat.Format = "0"; // Integer, no decimals

                // Set format for Parameters column
                int ParametersIndex = dataGridViewLog.Columns["Parameters"].Index + 1; // +1 for 1-based indexes
                worksheet.Column(ParametersIndex).Style.NumberFormat.Format = "@"; // Format for displaying parameters

                // Set filter on headers
                worksheet.RangeUsed().SetAutoFilter();

                // Freeze the first row (headers)
                worksheet.SheetView.FreezeRows(1);

                // Auto-adjust column widths
                worksheet.Columns().AdjustToContents();

                // Set background color for the first row
                worksheet.Row(1).Style.Fill.SetBackgroundColor(XLColor.FromHtml("4F81BD"));
                worksheet.Row(1).Style.Font.FontColor = XLColor.White; // Set font color to white for contrast

                // Create filename based on current date and time
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss");
                string fileName = $"Log {timestamp}.xlsx";

                // Get path to the folder containing the executable
                string folderPath = AppDomain.CurrentDomain.BaseDirectory;
                string fullPath = Path.Combine(folderPath, fileName);

                // Save the file
                workbook.SaveAs(fullPath);

                // Open the file by default
                if (MessageBox.Show($"Log exported to Excel successfully!\n\nSaved as:\n{fullPath}\n\nWould you like to open it?", "Success", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = fullPath,
                        UseShellExecute = true
                    });
                }
            }
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
            // Create StringBuilder to collect log text
            StringBuilder logText = new StringBuilder();
            // Iterate through DataGridView rows and collect text
            foreach (DataGridViewRow row in dataGridViewLog.Rows)
            {
                // Assuming you want to collect text from all cells in the row
                foreach (DataGridViewCell cell in row.Cells)
                {
                    logText.Append(cell.Value?.ToString() + "\t"); // Use tab to separate cells
                }
                logText.AppendLine(); // New line after each DataGridView row
            }
            if (logText.Length > 0)
            {
                Clipboard.SetText(logText.ToString());
                //    MessageBox.Show("Log copied to clipboard!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("There is no log to copy.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        private void buttonClearLog_Click(object? sender, EventArgs e)
        {
            // Clear the DataGridView
            dataGridViewLog.Rows.Clear();

            // Hide optional columns
            dataGridViewLog.Columns["Duplicates"].Visible = false;
            dataGridViewLog.Columns["Errors"].Visible = false;

            // Clear the internal cache of all benchmark passes
            _benchmarkPasses.Clear();

            // Optional: Clear selection and reset state
            dataGridViewLog.ClearSelection();
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
                // Collect objects associated with selected rows via Tag
                var passesToDelete = new List<BenchmarkPass>();

                foreach (DataGridViewRow row in dataGridViewLog.SelectedRows)
                {
                    if (row.IsNewRow) continue;

                    // Get benchmark pass object from row Tag
                    if (row.Tag is BenchmarkPass pass)
                    {
                        passesToDelete.Add(pass);
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

                // Remove corresponding benchmark passes from internal cache
                foreach (var pass in passesToDelete)
                {
                    _benchmarkPasses.Remove(pass); // Remove specific object, not all matching parameters
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
            if (e.ColumnIndex == 0) // Column with job type (Encode/Decode)
            {
                e.DrawBackground();
                // Draw checkbox
                if (listViewJobs.CheckBoxes)
                {
                    CheckBoxRenderer.DrawCheckBox(e.Graphics,
                    new Point(e.Bounds.Left + 4, e.Bounds.Top + 2),
            e.Item?.Checked == true ? System.Windows.Forms.VisualStyles.CheckBoxState.CheckedNormal
                    : System.Windows.Forms.VisualStyles.CheckBoxState.UncheckedNormal);
                }
                Color textColor = e.SubItem?.Text.Contains("Encode", StringComparison.OrdinalIgnoreCase) == true
                ? Color.Green
                : e.SubItem?.Text.Contains("Decode", StringComparison.OrdinalIgnoreCase) == true
                ? Color.Red
                : e.Item?.ForeColor ?? Color.Black;
                using (var brush = new SolidBrush(textColor))
                {
                    // Shift text right to avoid overlapping checkbox
                    Rectangle textBounds = new Rectangle(
                    e.Bounds.Left + (listViewJobs.CheckBoxes ? 20 : 0),
                    e.Bounds.Top,
                    e.Bounds.Width - (listViewJobs.CheckBoxes ? 20 : 0),
                    e.Bounds.Height);
                    e.Graphics.DrawString(e.SubItem?.Text ?? string.Empty,
                    e.SubItem?.Font ?? e.Item?.Font ?? this.Font,
                    brush, textBounds, StringFormat.GenericDefault);
                }
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
                MessageBox.Show("The specified file does not exist.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                string[] lines = await Task.Run(() => File.ReadAllLines(filePath));
                foreach (var line in lines)
                {
                    var parts = line.Split('~');
                    if (parts.Length == 4 && bool.TryParse(parts[1], out bool isChecked))
                    {
                        string jobName = NormalizeSpaces(parts[0]);
                        string passes = NormalizeSpaces(parts[2]);
                        string parameters = NormalizeSpaces(parts[3]);
                        AddJobsToListView(jobName, isChecked, passes, parameters);
                    }
                    else
                    {
                        MessageBox.Show($"Invalid line format: {line}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error reading file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                        foreach (string fileName in openFileDialog.FileNames) // Process each selected file
                        {
                            string[] lines = await Task.Run(() => File.ReadAllLines(fileName));
                            foreach (var line in lines)
                            {
                                // Normalize string
                                string normalizedLine = NormalizeSpaces(line);

                                var parts = normalizedLine.Split('~');
                                if (parts.Length == 4 && bool.TryParse(parts[1], out bool isChecked))
                                {
                                    string jobName = parts[0];
                                    string passes = parts[2];
                                    string parameters = parts[3];
                                    AddJobsToListView(jobName, isChecked, passes, parameters);
                                }
                                else
                                {
                                    MessageBox.Show($"Invalid line format: {normalizedLine}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error reading file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                            .Select(item => $"{item.Text}~{item.Checked}~{item.SubItems[1].Text}~{item.SubItems[2].Text}")
                            .ToArray();
                        File.WriteAllLines(saveFileDialog.FileName, jobList);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error exporting job list: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

            // Check if there are selected items
            var itemsToCopy = listViewJobs.SelectedItems.Count > 0
                ? listViewJobs.SelectedItems.Cast<ListViewItem>()
                : listViewJobs.Items.Cast<ListViewItem>();

            foreach (var item in itemsToCopy)
            {
                jobsText.AppendLine($"{NormalizeSpaces(item.Text)}~{item.Checked}~{NormalizeSpaces(item.SubItems[1].Text)}~{NormalizeSpaces(item.SubItems[2].Text)}");
            }

            // Copy text to clipboard
            if (jobsText.Length > 0)
            {
                Clipboard.SetText(jobsText.ToString());
            }
            else
            {
                MessageBox.Show("No jobs to copy.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        private void buttonPasteJobs_Click(object? sender, EventArgs e)
        {
            try
            {
                // Get text from clipboard
                string clipboardText = Clipboard.GetText();

                if (!string.IsNullOrEmpty(clipboardText))
                {
                    string[] lines = clipboardText.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var line in lines)
                    {
                        var parts = line.Split('~');
                        if (parts.Length == 4 && bool.TryParse(parts[1], out bool isChecked))
                        {
                            string jobName = parts[0];
                            string passes = parts[2];
                            string parameters = parts[3];
                            AddJobsToListView(jobName, isChecked, passes, parameters);
                        }
                        else
                        {
                            MessageBox.Show($"Invalid line format: {line}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }
                }
                else
                {
                    MessageBox.Show("Clipboard is empty.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error pasting jobs: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Script Constructor
        private void buttonScriptConstructor_Click(object sender, EventArgs e)
        {
            string compressionLevel = NormalizeSpaces(textBoxCompressionLevel.Text);
            string threads = NormalizeSpaces(textBoxThreads.Text);
            string commandLine = NormalizeSpaces(textBoxCommandLineOptionsEncoder.Text);

            string parameters = $"-{compressionLevel} {commandLine}".Trim();

            if (int.TryParse(threads, out int threadCount) && threadCount > 1)
            {
                parameters += $" -j{threads}";
            }

            parameters = Regex.Replace(parameters, @"\s+", " ").Trim();

            if (scriptForm == null || scriptForm.IsDisposed)
            {
                scriptForm = new ScriptConstructorForm();

                scriptForm.OnJobsAdded += (jobs) =>
                {
                    foreach (var job in jobs)
                    {
                        listViewJobs.Items.Add(job);
                    }
                };

                scriptForm.FormClosed += (s, e) => scriptForm = null;
            }

            scriptForm.InitialScriptText = parameters;

            scriptForm.Show(this);
            scriptForm.BringToFront();
            scriptForm.Focus();
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
                        // Use current UI settings to form parameters
                        string compressionLevel = NormalizeSpaces(textBoxCompressionLevel.Text);
                        string threads = NormalizeSpaces(textBoxThreads.Text);
                        string commandLine = NormalizeSpaces(textBoxCommandLineOptionsEncoder.Text);
                        string parameters = $"-{compressionLevel} {commandLine}".Trim();

                        if (int.TryParse(threads, out int threadCount) && threadCount > 1)
                        {
                            parameters += $" -j{threads}";
                        }

                        string outputFilePath = Path.Combine(tempFolderPath, "temp_warmup.flac");
                        DeleteFileIfExists(outputFilePath);

                        string arguments = $"\"{firstAudioFile}\" {parameters} --no-preserve-modtime -f -o \"{outputFilePath}\"";

                        string priorityText = (comboBoxCPUPriority.InvokeRequired)
                            ? (string)comboBoxCPUPriority.Invoke(() => comboBoxCPUPriority.SelectedItem?.ToString() ?? "Normal")
                            : (comboBoxCPUPriority.SelectedItem?.ToString() ?? "Normal");

                        try
                        {
                            using (var warmupProcess = new Process())
                            {
                                warmupProcess.StartInfo = new ProcessStartInfo
                                {
                                    FileName = firstEncoder,
                                    Arguments = arguments,
                                    UseShellExecute = false,
                                    CreateNoWindow = true,
                                };

                                var stopwatch = Stopwatch.StartNew();
                                warmupProcess.Start();

                                try
                                {
                                    warmupProcess.PriorityClass = GetProcessPriorityClass(priorityText);
                                }
                                catch (InvalidOperationException) { }

                                // Wait up to 5 seconds
                                bool exited = warmupProcess.WaitForExit(5_000);

                                stopwatch.Stop();

                                if (!exited)
                                {
                                    try
                                    {
                                        warmupProcess.Kill();
                                        Debug.WriteLine("Warm-up pass terminated: exceeded 10 seconds.");
                                    }
                                    catch (Exception killEx)
                                    {
                                        Debug.WriteLine($"Failed to kill warm-up process: {killEx.Message}");
                                    }
                                }
                                else
                                {
                                    Debug.WriteLine($"Warm-up pass completed in {stopwatch.Elapsed.TotalSeconds:F2}s");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Warm-up pass failed: {ex.Message}");
                        }

                        // Clean up temp file
                        DeleteFileIfExists(outputFilePath);
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

                        string priorityText;
                        if (comboBoxCPUPriority.InvokeRequired)
                        {
                            priorityText = (string)comboBoxCPUPriority.Invoke(() => comboBoxCPUPriority.SelectedItem?.ToString() ?? "Normal");
                        }
                        else
                        {
                            priorityText = comboBoxCPUPriority.SelectedItem?.ToString() ?? "Normal";
                        }

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
                                                _process.PriorityClass = GetProcessPriorityClass(priorityText);
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
                        DeleteFileIfExists(outputFilePath);

                        string arguments = $"\"{firstAudioFile}\" {parameters} --no-preserve-modtime -f -o \"{outputFilePath}\"";

                        string priorityText = (comboBoxCPUPriority.InvokeRequired)
                            ? (string)comboBoxCPUPriority.Invoke(() => comboBoxCPUPriority.SelectedItem?.ToString() ?? "Normal")
                            : (comboBoxCPUPriority.SelectedItem?.ToString() ?? "Normal");

                        try
                        {
                            using (var warmupProcess = new Process())
                            {
                                warmupProcess.StartInfo = new ProcessStartInfo
                                {
                                    FileName = firstEncoder,
                                    Arguments = arguments,
                                    UseShellExecute = false,
                                    CreateNoWindow = true,
                                };

                                var stopwatch = Stopwatch.StartNew();
                                warmupProcess.Start();

                                try
                                {
                                    warmupProcess.PriorityClass = GetProcessPriorityClass(priorityText);
                                }
                                catch (InvalidOperationException) { }

                                // Wait up to 5 seconds
                                bool exited = warmupProcess.WaitForExit(5_000);

                                stopwatch.Stop();

                                if (!exited)
                                {
                                    try
                                    {
                                        warmupProcess.Kill();
                                        Debug.WriteLine("Warm-up pass terminated: exceeded 10 seconds.");
                                    }
                                    catch (Exception killEx)
                                    {
                                        Debug.WriteLine($"Failed to kill warm-up process: {killEx.Message}");
                                    }
                                }
                                else
                                {
                                    Debug.WriteLine($"Warm-up pass completed in {stopwatch.Elapsed.TotalSeconds:F2}s");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Warm-up pass failed: {ex.Message}");
                        }

                        // Clean up temp file
                        DeleteFileIfExists(outputFilePath);
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

                        string priorityText;
                        if (comboBoxCPUPriority.InvokeRequired)
                        {
                            priorityText = (string)comboBoxCPUPriority.Invoke(() => comboBoxCPUPriority.SelectedItem?.ToString() ?? "Normal");
                        }
                        else
                        {
                            priorityText = comboBoxCPUPriority.SelectedItem?.ToString() ?? "Normal";
                        }

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
                                                _process.PriorityClass = GetProcessPriorityClass(priorityText);
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

                        // Check if parameters contain script patterns (like {0..8} or {1,2,3})
                        if (parameters.Contains('{') && parameters.Contains('}'))
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
                    // Find first checked job in listViewJobs
                    var firstJobItem = listViewJobs.Items.Cast<ListViewItem>().FirstOrDefault(item => item.Checked);
                    if (firstJobItem == null) return;

                    string jobType = NormalizeSpaces(firstJobItem.Text);
                    string parameters = NormalizeSpaces(firstJobItem.SubItems[2].Text.Trim());

                    // Choose first available file and encoder
                    string audioFilePath = null;
                    string arguments = null;
                    string outputFilePath = null;

                    if (string.Equals(jobType, "Encode", StringComparison.OrdinalIgnoreCase))
                    {
                        audioFilePath = selectedAudioFiles.FirstOrDefault();
                        if (string.IsNullOrEmpty(audioFilePath)) return;

                        outputFilePath = Path.Combine(tempFolderPath, "temp_warmup.flac");
                        DeleteFileIfExists(outputFilePath);
                        arguments = $"\"{audioFilePath}\" {parameters} --no-preserve-modtime -f -o \"{outputFilePath}\"";
                    }
                    else if (string.Equals(jobType, "Decode", StringComparison.OrdinalIgnoreCase))
                    {
                        audioFilePath = selectedFlacAudioFiles.FirstOrDefault();
                        if (string.IsNullOrEmpty(audioFilePath)) return;

                        outputFilePath = Path.Combine(tempFolderPath, "temp_warmup.wav");
                        DeleteFileIfExists(outputFilePath);
                        arguments = $"\"{audioFilePath}\" {parameters} --no-preserve-modtime -f -o \"{outputFilePath}\"";
                    }
                    else
                    {
                        return; // Unknown job type
                    }

                    var firstEncoder = selectedEncoders.FirstOrDefault();
                    if (string.IsNullOrEmpty(firstEncoder)) return;

                    string priorityText = (comboBoxCPUPriority.InvokeRequired)
                        ? (string)comboBoxCPUPriority.Invoke(() => comboBoxCPUPriority.SelectedItem?.ToString() ?? "Normal")
                        : (comboBoxCPUPriority.SelectedItem?.ToString() ?? "Normal");

                    try
                    {
                        using (var warmupProcess = new Process())
                        {
                            warmupProcess.StartInfo = new ProcessStartInfo
                            {
                                FileName = firstEncoder,
                                Arguments = arguments,
                                UseShellExecute = false,
                                CreateNoWindow = true,
                            };

                            var stopwatch = Stopwatch.StartNew();
                            warmupProcess.Start();

                            try
                            {
                                warmupProcess.PriorityClass = GetProcessPriorityClass(priorityText);
                            }
                            catch (InvalidOperationException) { }

                            // Wait up to 5 seconds
                            bool exited = warmupProcess.WaitForExit(5_000);

                            stopwatch.Stop();

                            if (!exited)
                            {
                                try
                                {
                                    warmupProcess.Kill();
                                    Debug.WriteLine("Warm-up pass terminated: exceeded 10 seconds.");
                                }
                                catch (Exception killEx)
                                {
                                    Debug.WriteLine($"Failed to kill warm-up process: {killEx.Message}");
                                }
                            }
                            else
                            {
                                Debug.WriteLine($"Warm-up pass completed in {stopwatch.Elapsed.TotalSeconds:F2}s");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Warm-up pass failed: {ex.Message}");
                    }

                    // Clean up temp file
                    DeleteFileIfExists(outputFilePath);
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

                                    string priorityText;
                                    if (comboBoxCPUPriority.InvokeRequired)
                                    {
                                        priorityText = (string)comboBoxCPUPriority.Invoke(() => comboBoxCPUPriority.SelectedItem?.ToString() ?? "Normal");
                                    }
                                    else
                                    {
                                        priorityText = comboBoxCPUPriority.SelectedItem?.ToString() ?? "Normal";
                                    }

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
                                                            _process.PriorityClass = GetProcessPriorityClass(priorityText);
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

                                    string priorityText;
                                    if (comboBoxCPUPriority.InvokeRequired)
                                    {
                                        priorityText = (string)comboBoxCPUPriority.Invoke(() => comboBoxCPUPriority.SelectedItem?.ToString() ?? "Normal");
                                    }
                                    else
                                    {
                                        priorityText = comboBoxCPUPriority.SelectedItem?.ToString() ?? "Normal";
                                    }

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
                                                            _process.PriorityClass = GetProcessPriorityClass(priorityText);
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

                buttonAddJobToJobListEncoder_Click(sender, e);
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
                        ShowTemporaryStoppedMessage("Process has been stopped.");
                    }
                    else
                    {
                        ShowTemporaryStoppedMessage("Process has already exited.");
                    }
                }
                catch (Exception ex)
                {
                    ShowTemporaryStoppedMessage($"Process is not running.");
                }
                finally
                {
                    progressBarEncoder.Value = 0;
                    progressBarDecoder.Value = 0;
                    progressBarEncoder.ManualText = $"";
                    progressBarDecoder.ManualText = $"";
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

        // Method to get process priority
        private ProcessPriorityClass GetProcessPriorityClass(string priority)
        {
            switch (priority)
            {
                case "Idle":
                    return ProcessPriorityClass.Idle;
                case "BelowNormal":
                    return ProcessPriorityClass.BelowNormal;
                case "Normal":
                    return ProcessPriorityClass.Normal;
                case "AboveNormal":
                    return ProcessPriorityClass.AboveNormal;
                case "High":
                    return ProcessPriorityClass.High;
                case "RealTime":
                    return ProcessPriorityClass.RealTime;
                default:
                    return ProcessPriorityClass.Normal; // Default value
            }
        }

        private void ShowTemporaryStoppedMessage(string message)
        {
            labelStopped.Text = message; // Set message text
            labelStopped.Visible = true; // Make label visible

            System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer(); // Explicitly use namespace
            timer.Interval = 5000; // Set interval to 5 seconds
            timer.Tick += (s, e) =>
            {
                labelStopped.Visible = false; // Hide label
                timer.Stop(); // Stop timer
                timer.Dispose(); // Release resources
            };
            timer.Start(); // Start timer
        }
        private void ShowTemporaryAudioFileRemovedMessage(string message)
        {
            labelAudioFileRemoved.Text = message; // Set message text
            labelAudioFileRemoved.Visible = true; // Make label visible

            System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer(); // Explicitly use namespace
            timer.Interval = 6000; // Set interval to 6 seconds
            timer.Tick += (s, e) =>
            {
                labelAudioFileRemoved.Visible = false; // Hide label
                timer.Stop(); // Stop timer
                timer.Dispose(); // Release resources
            };
            timer.Start(); // Start timer
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

        // FORM LOAD
        private async void Form1_Load(object? sender, EventArgs e)
        {
            this.Text = $"FLAC Benchmark-H [{programVersionCurrent}]";
            progressBarEncoder.ManualText = string.Empty;
            progressBarDecoder.ManualText = string.Empty;

            LoadSettings();
            LoadEncoders();
            LoadAudioFiles();
            LoadJobs();
            this.ActiveControl = null; // Remove focus from all elements
            dataGridViewLog.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells);
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
            cpuLoadCounter?.Dispose();

            // Dispose pause/resume synchronization object
            _pauseEvent?.Dispose();

            // Optionally clean up temporary folder
            if (checkBoxClearTempFolder.Checked)
            {
                // Check if temp folder exists
                if (Directory.Exists(tempFolderPath))
                {
                    var tempEncodedFilePath = Path.Combine(tempFolderPath, "temp_encoded.flac");
                    var tempDecodedFilePath = Path.Combine(tempFolderPath, "temp_decoded.wav");

                    // Delete temp files if they exist
                    if (File.Exists(tempEncodedFilePath))
                    {
                        File.Delete(tempEncodedFilePath);
                    }

                    if (File.Exists(tempDecodedFilePath))
                    {
                        File.Delete(tempDecodedFilePath);
                    }

                    // Check if folder is empty after file deletion
                    if (Directory.GetFiles(tempFolderPath).Length == 0)
                    {
                        // Delete folder if empty
                        Directory.Delete(tempFolderPath);
                    }
                }
            }
        }
    }
}