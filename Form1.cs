using MediaInfoLib;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Management;
using System.Text;
using System.Text.RegularExpressions;
using System.Drawing;
using System.Windows.Forms;
using System.Security.Cryptography;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using ClosedXML.Excel;
using DocumentFormat.OpenXml;

namespace FLAC_Benchmark_H
{
    public partial class Form1 : Form
    {
        private int physicalCores;
        private int threadCount;
        private Process _process; // Field to store the current process
        private const string SettingsGeneralFilePath = "Settings_general.txt"; // Path to the settings file
        private const string SettingsEncodersFilePath = "Settings_flac_executables.txt"; // Path to the file for saving executable files
        private const string SettingsAudioFilesFilePath = "Settings_audio_files.txt"; // Path to the file for saving audio files
        private const string SettingsJobsFilePath = "Settings_jobs.txt"; // Path to the jobs file
        private Stopwatch stopwatch;
        private PerformanceCounter cpuCounter = null;
        private bool performanceCountersAvailable = false;
        private System.Windows.Forms.Timer cpuUsageTimer; // Explicitly specify that this is a Timer from System.Windows.Forms
        private bool _isEncodingStopped = false;
        private bool isExecuting = false; // Flag to track if the process is running
        private bool _isPaused = false; // Pause flag
        private string tempFolderPath; // Field to store the path to the temporary folder
        private bool isCpuInfoLoaded = false;
        public string programVersionCurrent = "1.2 build 20250804"; // Current program version
        public string programVersionIgnored = null; // To store the ignored version

        public Form1()
        {
            InitializeComponent();
            InitializeDragAndDrop(); // Initialize drag-and-drop
            this.FormClosing += Form1_FormClosing; // Register the form closing event handler
            this.listViewEncoders.KeyDown += ListViewEncoders_KeyDown;
            this.listViewAudioFiles.KeyDown += ListViewAudioFiles_KeyDown;
            this.listViewJobs.KeyDown += ListViewJobs_KeyDown;
            this.textBoxCompressionLevel.KeyDown += new KeyEventHandler(this.textBoxCompressionLevel_KeyDown);
            this.textBoxThreads.KeyDown += new KeyEventHandler(this.textBoxThreads_KeyDown);
            this.textBoxCommandLineOptionsEncoder.KeyDown += new KeyEventHandler(this.textBoxCommandLineOptionsEncoder_KeyDown);
            this.textBoxCommandLineOptionsDecoder.KeyDown += new KeyEventHandler(this.textBoxCommandLineOptionsDecoder_KeyDown);
            LoadCPUInfoAsync(); // Load CPU information
            stopwatch = new Stopwatch(); // Initialize Stopwatch object
            try
            {
                cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
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
            InitializedataGridViewLog();

            tempFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp"); // Initialize the path to the temporary folder
            _process = new Process(); // Initialize _process to avoid nullability warning

            dataGridViewLog.CellContentClick += dataGridViewLog_CellContentClick;
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

                // Create a query to get processor information
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

            if (performanceCountersAvailable && cpuCounter != null)
            {
                float cpuUsage = cpuCounter.NextValue();
                labelCpuUsage.Text = $"CPU Usage: {cpuUsage:F2}%";
            }
            else
            {
                labelCpuUsage.Text = "CPU Usage: N/A";
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
                    $"AutoAnalyzeLog={checkBoxAutoAnalyzeLog.Checked}",
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
                            case "AutoAnalyzeLog":
                                checkBoxAutoAnalyzeLog.Checked = bool.Parse(value);
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
        }
        private void buttonClearEncoders_Click(object? sender, EventArgs e)
        {
            listViewEncoders.Items.Clear();
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
        }
        private async void buttonAddAudioFiles_Click(object? sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Title = "Select Audio Files";
                openFileDialog.Filter = "Audio Files (*.flac;*.wav)|*.flac;*.wav|All Files (*.*)|*.*";
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
            item.SubItems.Add($"{audioFileInfo.Duration:n0} ms");
            item.SubItems.Add(audioFileInfo.BitDepth + " bit");
            item.SubItems.Add(audioFileInfo.SamplingRate);
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
            string bitDepth = mediaInfo.Get(StreamKind.Audio, 0, "BitDepth") ?? "N/A";
            string samplingRate = mediaInfo.Get(StreamKind.Audio, 0, "SamplingRate/String") ?? "N/A";
            long fileSize = new FileInfo(audioFilePath).Length;
            string md5Hash = "N/A"; // Default value for MD5

            // Determine the file type and get the corresponding MD5
            if (Path.GetExtension(audioFilePath).Equals(".flac", StringComparison.OrdinalIgnoreCase))
            {
                md5Hash = mediaInfo.Get(StreamKind.Audio, 0, "MD5_Unencoded") ?? "N/A"; // Get MD5 for FLAC
            }
            else if (Path.GetExtension(audioFilePath).Equals(".wav", StringComparison.OrdinalIgnoreCase) && (checkBoxAddMD5OnLoadWav.Checked))
            {
                md5Hash = await CalculateWavMD5Async(audioFilePath); // Async method to calculate MD5 for WAV
            }

            mediaInfo.Close();

            // Add new information to the cache
            var audioFileInfo = new AudioFileInfo
            {
                FilePath = audioFilePath,
                DirectoryPath = Path.GetDirectoryName(audioFilePath),
                FileName = Path.GetFileName(audioFilePath),
                Duration = duration,
                BitDepth = bitDepth,
                SamplingRate = samplingRate,
                FileSize = fileSize,
                Md5Hash = md5Hash
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
            public string Duration { get; set; }
            public string BitDepth { get; set; }
            public string SamplingRate { get; set; }
            public long FileSize { get; set; }
            public string Md5Hash { get; set; }
        }
        private ConcurrentDictionary<string, AudioFileInfo> audioInfoCache = new ConcurrentDictionary<string, AudioFileInfo>();

        private async Task<string> CalculateWavMD5Async(string audioFilePath)
        {
            using (var stream = File.OpenRead(audioFilePath))
            {
                using (var md5 = MD5.Create())
                {
                    using (BinaryReader reader = new BinaryReader(stream))
                    {
                        // Check RIFF header
                        if (reader.ReadUInt32() != 0x46464952) // "RIFF"
                            return "Invalid WAV file";

                        reader.ReadUInt32(); // Read total file size (not used)

                        if (reader.ReadUInt32() != 0x45564157) // "WAVE"
                            return "Invalid WAV file";

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
                                // Check for valid size
                                if (chunkSize < 0 || chunkSize > int.MaxValue)
                                {
                                    return "Invalid WAV file";
                                }

                                // Read audio data from "data" chunk
                                byte[] audioData = reader.ReadBytes((int)chunkSize);
                                return BitConverter.ToString(md5.ComputeHash(audioData)).Replace("-", "").ToUpperInvariant();
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

            return "MD5 calculation failed"; // If nothing is found
        }
        private async Task<string> CalculateFlacMD5Async(string flacFilePath)
        {
            return await Task.Run(async () =>
            {
                try
                {
                    string encoderExePath = null;

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
                        // Show a warning message from the UI thread
                        await this.InvokeAsync(() =>
                        {
                            MessageBox.Show(
                            "No encoder (.exe) is loaded in the Encoders list.\n" +
                            "Please add at least one encoder (e.g., flac.exe) to calculate MD5 for FLAC files.",
                            "No Encoder Found",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning
                            );
                        });

                        return "No .exe encoder found in the list";
                    }

                    if (!Directory.Exists(tempFolderPath))
                    {
                        try
                        {
                            Directory.CreateDirectory(tempFolderPath);
                        }
                        catch (Exception ex)
                        {
                            return $"Failed to create temp folder: {ex.Message}";
                        }
                    }
                    // ---

                    // Create a temporary WAV file path
                    string tempWavFile = Path.Combine(tempFolderPath, "temp_flac_md5_decode.wav");
                    // Build the command line for decoding
                    string arguments = $"\"{flacFilePath}\" -d --no-preserve-modtime -f -o \"{tempWavFile}\"";

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
                            return $"Decode failed: {errorOutput.Trim()}";
                        }

                        // Verify that the temporary WAV file was created
                        if (!File.Exists(tempWavFile))
                        {
                            return "Temporary WAV file was not created";
                        }

                        // Calculate MD5 for the temporary WAV file
                        string md5Hash = CalculateWavMD5Async(tempWavFile).Result;

                        // Clean up: delete the temporary file
                        try
                        {
                            File.Delete(tempWavFile);
                        }
                        catch { }

                        // Return the calculated MD5 hash
                        return md5Hash;
                    }
                }
                catch (Exception ex)
                {
                    return $"Error: {ex.Message}";
                }
            });
        }


        private async void buttonDetectDupesAudioFiles_Click(object? sender, EventArgs e)
        {
            // Dictionary to group ListViewItems by their MD5 hash.
            // Key: MD5 hash string. Value: List of items with that hash (potential duplicates).
            var hashDict = new Dictionary<string, List<ListViewItem>>();

            // List to store information about files for which MD5 calculation failed.
            // Stores the item and the full error message for logging.
            var filesWithMD5Errors = new List<(ListViewItem Item, string FullErrorMessage)>();

            // --- NEW LOGIC STEP 1: Select ALL items initially ---
            // This ensures that files which are NOT duplicates will remain selected.
            // It also provides a known starting state before applying duplicate-specific logic.
            foreach (ListViewItem item in listViewAudioFiles.Items)
            {
                item.Checked = true;
            }
            // --- END OF NEW LOGIC STEP 1 ---

            // --- MD5 Calculation Loop ---
            // Iterate through each item in the ListView to ensure it has a valid MD5 hash.
            // Note: The body of this loop calculates hashes sequentially, waiting for each file's
            // MD5 task to complete before moving to the next. Parallelization occurs within
            // the calculation of a single file's MD5 (e.g., inside CalculateWavMD5Async/CalculateFlacMD5Async).
            var items = listViewAudioFiles.Items.Cast<ListViewItem>().ToList();
            foreach (var item in items)
            {
                string filePath = item.Tag.ToString(); // Get the full file path from the item's Tag
                string md5Hash = item.SubItems[5].Text; // Get the current MD5 value from the 6th column (index 5)

                // Check if the MD5 hash is missing, invalid, or a placeholder indicating it needs recalculation.
                // Common cases: unset hash from '--no-md5', previous calculation errors, or not yet calculated.
                if (string.IsNullOrEmpty(md5Hash) ||
                md5Hash == "00000000000000000000000000000000" || // Unset MD5 marker
                md5Hash == "Invalid WAV file" || // Error marker from WAV MD5 calculation
                md5Hash == "N/A") // Placeholder for files where MD5 wasn't calculated
                {
                    string fileExtension = Path.GetExtension(filePath).ToLowerInvariant();

                    // Calculate MD5 based on the file type.
                    if (fileExtension == ".wav")
                    {
                        // Offload the synchronous MD5 calculation to a background thread to keep the UI responsive.
                        md5Hash = await Task.Run(() => CalculateWavMD5Async(filePath));

                        // Determine if the MD5 calculation for the WAV file resulted in an error.
                        bool hasError = md5Hash == "Invalid WAV file" || md5Hash == "MD5 calculation failed";

                        // Update the UI: Display either the calculated MD5 hash or a short error label.
                        item.SubItems[5].Text = hasError ? "MD5 calculation failed" : md5Hash;

                        // If there was an error, add the item and the full error message to the error list for logging.
                        if (hasError)
                        {
                            filesWithMD5Errors.Add((item, md5Hash)); // md5Hash contains the full error here
                        }
                    }
                    else if (fileExtension == ".flac")
                    {
                        // Asynchronously decode the FLAC file to a temporary WAV and calculate its MD5.
                        // This method handles process execution and internal MD5 calculation.
                        string fullErrorOrMd5 = await CalculateFlacMD5Async(filePath);

                        // Determine if the FLAC processing or internal MD5 calculation resulted in an error.
                        // Checks against known error prefixes/messages.
                        bool hasError = fullErrorOrMd5.StartsWith("Error:") ||
                        fullErrorOrMd5.StartsWith("Decode failed:") ||
                        fullErrorOrMd5 == "No .exe encoder found in the list" ||
                        fullErrorOrMd5 == "Encoder path invalid" ||
                        fullErrorOrMd5 == "Temporary WAV file was not created" ||
                        fullErrorOrMd5 == "Invalid WAV file" || // Error from internal WAV MD5 calc
                        fullErrorOrMd5 == "MD5 calculation failed"; // Error from internal WAV MD5 calc

                        // Update the UI: Display either the calculated MD5 hash or a short error label.
                        item.SubItems[5].Text = hasError ? "MD5 calculation failed" : fullErrorOrMd5;

                        // If there was an error, add the item and the full error message to the error list for logging.
                        if (hasError)
                        {
                            filesWithMD5Errors.Add((item, fullErrorOrMd5)); // fullErrorOrMd5 contains the full error here
                        }

                        // Update md5Hash variable for subsequent logic (cache update, grouping).
                        // If no error, fullErrorOrMd5 IS the MD5 hash.
                        md5Hash = fullErrorOrMd5;
                    }
                    // For other file types, md5Hash remains "N/A" or another placeholder.
                    // No MD5 is calculated to prevent errors or unnecessary processing.

                    // --- Update Application Cache ---
                    // Ensure the application's cache reflects the latest MD5 status (calculated or failed)
                    // for this specific file path.
                    if (item.Tag is string existingFilePath)
                    {
                        if (audioInfoCache.TryGetValue(existingFilePath, out var cachedInfo))
                        {
                            // If the file is already in the cache, update its MD5 hash.
                            cachedInfo.Md5Hash = md5Hash; // Use the potentially shortened label or the actual hash
                            audioInfoCache[existingFilePath] = cachedInfo; // Reassign for clarity/consistency
                        }
                        else
                        {
                            // If the file is new to the cache, create and add a new AudioFileInfo object.
                            var newInfo = new AudioFileInfo
                            {
                                FilePath = existingFilePath,
                                Md5Hash = md5Hash, // Use the potentially shortened label or the actual hash
                                FileName = Path.GetFileName(existingFilePath),
                                DirectoryPath = Path.GetDirectoryName(existingFilePath)
                                // Other properties might be populated elsewhere or lazily.
                            };
                            audioInfoCache.TryAdd(existingFilePath, newInfo);
                        }
                    }
                    // --- End of Cache Update ---
                } // End of MD5 Calculation Block

                // --- Group Files by MD5 Hash ---
                // After ensuring md5Hash is calculated or confirmed as needing no calculation,
                // check if the hash is valid for grouping into potential duplicates.
                if (!string.IsNullOrEmpty(md5Hash) &&
                md5Hash != "00000000000000000000000000000000" && // Explicitly exclude the "unset" MD5 marker
                md5Hash != "Invalid WAV file" && // Exclude specific error markers
                md5Hash != "MD5 calculation failed" && // Exclude specific error markers
                md5Hash != "N/A") // Exclude placeholders
                {
                    // The hash is valid. Add the ListViewItem to the dictionary under its MD5 key.
                    // This effectively groups files by their audio content hash.
                    if (hashDict.ContainsKey(md5Hash))
                    {
                        // If the hash already exists, add the current item to the existing list.
                        hashDict[md5Hash].Add(item);
                    }
                    else
                    {
                        // If the hash is new, create a new list with the current item and add it to the dictionary.
                        hashDict[md5Hash] = new List<ListViewItem> { item };
                    }
                }
                // --- End of Hash Grouping ---
            } // End of foreach item loop for MD5 calculation and grouping
              // --- End of MD5 Calculation Loop ---

            // --- Logging Setup ---
            // Ensure the dedicated "Error" column exists in the log DataGridView for detailed messages.
            if (!dataGridViewLog.Columns.Contains("Error"))
            {
                var errorColumn = new DataGridViewTextBoxColumn();
                errorColumn.Name = "Error"; // Unique identifier for the column
                errorColumn.HeaderText = "Error"; // Display name for the column header
                errorColumn.Visible = false; // Start hidden, only show if errors occur
                errorColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells; // Auto-size for content
                dataGridViewLog.Columns.Add(errorColumn); // Add the column to the DataGridView
            }

            // Clear previous MD5 calculation error results from the log to avoid stale entries.
            // Specifically target rows marked as "MD5 calculation failed" in the Duplicates column.
            // Iterate backwards to safely remove items from the collection.
            for (int i = dataGridViewLog.Rows.Count - 1; i >= 0; i--)
            {
                DataGridViewRow row = dataGridViewLog.Rows[i];
                if (row.Cells["Duplicates"].Value?.ToString() == "MD5 calculation failed")
                {
                    dataGridViewLog.Rows.RemoveAt(i);
                }
            }
            // --- End of Logging Setup ---

            // --- Log MD5 Errors ---
            // Add detailed error results to the log grid for files whose MD5 could not be calculated.
            foreach (var (errorItem, fullErrorMessage) in filesWithMD5Errors)
            {
                string filePath = errorItem.Tag.ToString();
                string fileName = Path.GetFileName(filePath);

                // Add a new row to the log grid with details about the MD5 calculation failure.
                int rowIndex = dataGridViewLog.Rows.Add(
                fileName, // Name: File name
                string.Empty, // InputFileSize
                string.Empty, // OutputFileSize
                string.Empty, // Compression
                string.Empty, // Time
                string.Empty, // Speed
                string.Empty, // Parameters
                string.Empty, // Encoder
                string.Empty, // Version
                string.Empty, // Encoder directory path
                string.Empty, // FastestEncoder
                string.Empty, // BestSize
                string.Empty, // SameSize
                Path.GetDirectoryName(filePath), // AudioFileDirectory: Path to the file's folder
                "MD5 calculation failed", // MD5: Status message
                string.Empty // Duplicates: Leave empty for errors
                );

                // Set the full, detailed error message in the dedicated "Error" column for diagnostics.
                dataGridViewLog.Rows[rowIndex].Cells["Error"].Value = fullErrorMessage;
                // Visually distinguish error rows by setting their text color to Gray.
                dataGridViewLog.Rows[rowIndex].DefaultCellStyle.ForeColor = Color.Gray;
            }

            // Control visibility of the "Error" column based on whether any errors were logged.
            // Keeps the log clean when there are no issues but makes errors prominent when they occur.
            bool hasAnyLogErrors = dataGridViewLog.Rows
            .Cast<DataGridViewRow>()
            .Any(row => row.Cells["Error"].Value != null && !string.IsNullOrEmpty(row.Cells["Error"].Value.ToString()));

            dataGridViewLog.Columns["Error"].Visible = hasAnyLogErrors;
            // --- End of Log MD5 Errors ---

            // --- NEW LOGIC STEP 2: Deselect Non-Primary Duplicates ---
            // This implements the desired behavior:
            // 1. All items were initially checked.
            // 2. Now, for each group of files with the SAME MD5 hash (actual duplicates):
            // a. Keep the FIRST item in the group CHECKED (this is the "primary").
            // b. UNCHECK ALL OTHER items in the same group (these are the "non-primary duplicates").
            // Result:
            // - Unique files (groups with Count == 1) remain CHECKED.
            // - Primary duplicates (first in each group with Count > 1) remain CHECKED.
            // - Non-primary duplicates (second, third, etc. in each group with Count > 1) become UNCHECKED.
            foreach (var kvp in hashDict)
            {
                // Process only groups that contain actual duplicates (more than one file with the same hash)
                if (kvp.Value.Count > 1)
                {
                    // Iterate through the list of duplicate items for this specific hash,
                    // STARTING FROM THE SECOND ITEM (i = 1).
                    for (int i = 1; i < kvp.Value.Count; i++)
                    {
                        // Uncheck all subsequent items in the group.
                        // These represent the non-primary duplicates.
                        kvp.Value[i].Checked = false;
                    }
                    // Note: kvp.Value[0] (the first item) REMAINS CHECKED because the loop starts from i=1.
                    // Items that are unique (groups with Count == 1) are unaffected and remain checked
                    // because they do not enter this `if (kvp.Value.Count > 1)` block.
                }
            }
            // --- END OF NEW LOGIC STEP 2 ---

            // --- Post-Processing: Move and Log Duplicates ---
            // Finally, iterate through the groups again to perform actions on confirmed duplicate sets.
            foreach (var kvp in hashDict)
            {
                // Confirm again that this group contains duplicates (more than one file)
                if (kvp.Value.Count > 1)
                {
                    // --- UI Update: Move Duplicates to Top ---
                    // Physically move all items in this duplicate group to the top of the ListView
                    // for immediate visual feedback to the user.
                    foreach (var dupItem in kvp.Value)
                    {
                        // Ensure the item actually exists in the list before trying to move it
                        if (listViewAudioFiles.Items.Contains(dupItem))
                        {
                            // Remove the item from its current position
                            listViewAudioFiles.Items.Remove(dupItem);
                            // Insert it at the very beginning of the list (index 0)
                            listViewAudioFiles.Items.Insert(0, dupItem);
                        }
                    }
                    // --- End of UI Update ---

                    // --- Log Each Duplicate ---
                    // Add a detailed entry for each file in the duplicate group to the log grid.
                    foreach (var dupItem in kvp.Value)
                    {
                        string filePath = dupItem.Tag.ToString();
                        string fileName = Path.GetFileName(filePath);
                        string md5Hash = dupItem.SubItems[5].Text; // Get the MD5 hash from the item's subitem

                        // Create a comma-separated list of filenames for all items in this duplicate group
                        // This provides a quick overview of which files are linked as duplicates.
                        string duplicatesList = string.Join(", ", kvp.Value.Select(item => Path.GetFileName(item.Tag.ToString())));

                        // Add a comprehensive row to the log grid detailing this specific duplicate file.
                        int rowIndex = dataGridViewLog.Rows.Add(
                        fileName, // Name: The name of this specific duplicate file
                        string.Empty, // InputFileSize
                        string.Empty, // OutputFileSize
                        string.Empty, // Compression
                        string.Empty, // Time
                        string.Empty, // Speed
                        string.Empty, // Parameters
                        string.Empty, // Encoder
                        string.Empty, // Version
                        string.Empty, // Encoder directory path
                        string.Empty, // FastestEncoder
                        string.Empty, // BestSize
                        string.Empty, // SameSize
                        Path.GetDirectoryName(filePath), // AudioFileDirectory: Folder path of this file
                        md5Hash, // MD5: The hash that identifies this group of duplicates
                        duplicatesList // Duplicates: List of all filenames in this group
                        );

                        // Visually distinguish rows representing duplicate files by setting their text color to Brown.
                        dataGridViewLog.Rows[rowIndex].DefaultCellStyle.ForeColor = Color.Brown;
                    }
                    // --- End of Logging Each Duplicate ---
                }
            }
            // --- End of Post-Processing ---
        }
        private async void buttonTestForErrors_Click(object? sender, EventArgs e)
        {
            var errorResults = new List<(string FileName, string FilePath, string Message)>();

            // Get the list of FLAC files from the audio files list
            var flacItems = listViewAudioFiles.Items.Cast<ListViewItem>()
            .Where(item => Path.GetExtension(item.Tag.ToString()).Equals(".flac", StringComparison.OrdinalIgnoreCase))
            .ToList();

            // Process each file sequentially
            foreach (var item in flacItems)
            {
                string filePath = item.Tag.ToString();
                string fileName = Path.GetFileName(filePath);


                var encoderItem = listViewEncoders.Items
                .Cast<ListViewItem>()
                .FirstOrDefault(item =>
                Path.GetExtension(item.Text).Equals(".exe", StringComparison.OrdinalIgnoreCase));

                string encoderPath = encoderItem?.Tag?.ToString();

                if (string.IsNullOrEmpty(encoderPath))
                {
                    errorResults.Add((fileName, filePath, "No .exe encoder found"));
                    continue;
                }

                try
                {
                    using (var process = new Process())
                    {
                        process.StartInfo.FileName = encoderPath;
                        process.StartInfo.Arguments = $"--test --silent \"{filePath}\"";
                        process.StartInfo.UseShellExecute = false;
                        process.StartInfo.RedirectStandardError = true;
                        process.StartInfo.RedirectStandardOutput = true;
                        process.StartInfo.CreateNoWindow = true;

                        process.Start();

                        string errorOutput = await process.StandardError.ReadToEndAsync();
                        string output = await process.StandardOutput.ReadToEndAsync();
                        await process.WaitForExitAsync();

                        if (process.ExitCode != 0)
                        {
                            string message = string.IsNullOrEmpty(errorOutput.Trim()) ? "Unknown error" : errorOutput.Trim();
                            errorResults.Add((fileName, filePath, message));
                        }
                    }
                }
                catch (Exception ex)
                {
                    errorResults.Add((fileName, filePath, $"Exception: {ex.Message}"));
                }
            }

            // Ensure the "Error" column exists and is initially hidden
            if (!dataGridViewLog.Columns.Contains("Error"))
            {
                var errorColumn = new DataGridViewTextBoxColumn();
                errorColumn.Name = "Error";
                errorColumn.HeaderText = "Error";
                errorColumn.Visible = false;
                errorColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                dataGridViewLog.Columns.Add(errorColumn);
            }

            // Clear previous integrity check results from the log
            foreach (DataGridViewRow row in dataGridViewLog.Rows)
            {
                if (row.Cells["Duplicates"].Value?.ToString() == "Integrity Check Failed")
                {
                    dataGridViewLog.Rows.Remove(row);
                }
            }

            // Add error results to the log grid
            foreach (var result in errorResults)
            {
                int rowIndex = dataGridViewLog.Rows.Add(
                result.FileName,
                string.Empty, // InputFileSize
                string.Empty, // OutputFileSize
                string.Empty, // Compression
                string.Empty, // Time
                string.Empty, // Speed
                string.Empty, // Parameters
                string.Empty, // Encoder
                string.Empty, // Version
                string.Empty, // Encoder directory path
                string.Empty, // FastestEncoder
                string.Empty, // BestSize
                string.Empty, // SameSize
                Path.GetDirectoryName(result.FilePath), // AudioFileDirectory
                "Integrity Check Failed", // MD5 - placeholder
                string.Empty // Duplicates
                );

                // Set the error message in the dedicated Error column
                dataGridViewLog.Rows[rowIndex].Cells["Error"].Value = result.Message;
                dataGridViewLog.Rows[rowIndex].DefaultCellStyle.ForeColor = Color.Red;
            }

            // Show the "Error" column only if there are errors
            dataGridViewLog.Columns["Error"].Visible = dataGridViewLog.Rows
            .Cast<DataGridViewRow>()
            .Any(row => row.Cells["Error"].Value != null && !string.IsNullOrEmpty(row.Cells["Error"].Value.ToString()));

            if (errorResults.Count == 0)
            {
                MessageBox.Show("All FLAC files passed the integrity test.", "Test Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
        private void buttonRemoveAudiofile_Click(object? sender, EventArgs e)
        {
            // Remove selected items from listViewAudioFiles
            for (int i = listViewAudioFiles.Items.Count - 1; i >= 0; i--)
            {
                if (listViewAudioFiles.Items[i].Selected) // Check if the item is selected
                {
                    listViewAudioFiles.Items.RemoveAt(i); // Remove the item
                }
            }
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

                MessageBox.Show("Unchecked audio files have been moved to the recycle bin.", "Deletion", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        private void buttonClearAudioFiles_Click(object? sender, EventArgs e)
        {
            listViewAudioFiles.Items.Clear();
        }

        // Log
        private void InitializedataGridViewLog()
        {
            // Configure DataGridView
            dataGridViewLog.Columns.Add("Name", "Name");
            dataGridViewLog.Columns.Add("InputFileSize", "In. Size");
            dataGridViewLog.Columns.Add("OutputFileSize", "Out. Size");
            dataGridViewLog.Columns.Add("Compression", "Compr.");
            dataGridViewLog.Columns.Add("Time", "Time");
            dataGridViewLog.Columns.Add("Speed", "Speed");
            dataGridViewLog.Columns.Add("Parameters", "Parameters");
            dataGridViewLog.Columns.Add("Encoder", "Encoder");
            dataGridViewLog.Columns.Add("Version", "Version");
            var encoderDirectoryColumn = new DataGridViewLinkColumn
            {
                Name = "EncoderDirectory",
                HeaderText = "Encoder Directory",
                DataPropertyName = "EncoderDirectory" // Bind to the Encoder's Directory Path column
            };
            dataGridViewLog.Columns.Add(encoderDirectoryColumn);
            dataGridViewLog.Columns.Add("FastestEncoder", "Fastest Encoder");
            dataGridViewLog.Columns.Add("BestSize", "Best Size");
            dataGridViewLog.Columns.Add("SameSize", "Same Size");
            var audioFileDirectoryColumn = new DataGridViewLinkColumn
            {
                Name = "AudioFileDirectory",
                HeaderText = "Audio File Directory",
                DataPropertyName = "AudioFileDirectory" // Bind to the Audio File's Directory Path column

            };
            dataGridViewLog.Columns.Add(audioFileDirectoryColumn);
            dataGridViewLog.Columns.Add("MD5", "MD5");
            dataGridViewLog.Columns.Add("Duplicates", "Duplicates");


            // Set alignment for columns
            dataGridViewLog.Columns["InputFileSize"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewLog.Columns["OutputFileSize"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewLog.Columns["Compression"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewLog.Columns["Time"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewLog.Columns["Speed"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

            // Hide the column with the full path
            //dataGridViewLog.Columns["FilePath"].Visible = false;
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

        private async Task LogProcessResults(string outputFilePath, string audioFilePath, string parameters, string encoder)
        {
            FileInfo outputFile = new FileInfo(outputFilePath);
            if (outputFile.Exists)
            {
                // Create CultureInfo for formatting with dots as thousand separators
                NumberFormatInfo numberFormat = new CultureInfo("en-US").NumberFormat;
                numberFormat.NumberGroupSeparator = " ";

                // Get input audio file information from cache
                var audioFileInfo = await GetAudioInfo(audioFilePath);

                // Extract data from cache
                string audioFileName = audioFileInfo.FileName; // Use file name from cache
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
                TimeSpan timeTaken = stopwatch.Elapsed;
                double compressionPercentage = ((double)outputSize / inputSize) * 100;
                double encodingSpeed = (double)durationMs / timeTaken.TotalMilliseconds;

                // Get encoder information from cache
                // Here we call GetEncoderInfo but with cache check
                var encoderInfo = await GetEncoderInfo(encoder); // Get encoder info

                // Add record to DataGridView log
                int rowIndex = dataGridViewLog.Rows.Add(
                    audioFileName,             // 0
                    inputSizeFormatted,        // 1
                    outputSizeFormatted,       // 2
                    $"{compressionPercentage:F3}%", // 3
                    $"{timeTaken.TotalMilliseconds:F3}", // 4
                    $"{encodingSpeed:F3}x",    // 5
                    parameters,                // 6
                    encoderInfo.FileName,      // 7 (Encoder file name from cache)
                    encoderInfo.Version,       // 8 (Encoder version from cache)
                    encoderInfo.DirectoryPath, // 9 (Encoder's directory path from cache)
                    string.Empty,              // 10 (FastestEncoder)
                    string.Empty,              // 11 (BestSize)
                    string.Empty,              // 12 (SameSize)
                    audioFileDirectory,        // 13 (Audio File Path)
                    Md5Hash                    // 14 (Md5Hash)
                );

                // Set text color based on file size comparison
                dataGridViewLog.Rows[rowIndex].Cells[2].Style.ForeColor = outputSize < inputSize ? System.Drawing.Color.Green :
                    outputSize > inputSize ? System.Drawing.Color.Red : dataGridViewLog.Rows[rowIndex].Cells[2].Style.ForeColor;

                dataGridViewLog.Rows[rowIndex].Cells[3].Style.ForeColor = compressionPercentage < 100 ? System.Drawing.Color.Green :
                    compressionPercentage > 100 ? System.Drawing.Color.Red : dataGridViewLog.Rows[rowIndex].Cells[3].Style.ForeColor;

                dataGridViewLog.Rows[rowIndex].Cells[5].Style.ForeColor = encodingSpeed > 1 ? System.Drawing.Color.Green :
                    encodingSpeed < 1 ? System.Drawing.Color.Red : dataGridViewLog.Rows[rowIndex].Cells[5].Style.ForeColor;

                // Scroll DataGridView down to the last added row
                //dataGridViewLog.FirstDisplayedScrollingRowIndex = dataGridViewLog.Rows.Count - 1;

                // Logging to file
                File.AppendAllText("log.txt", $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {audioFilePath}\tInput size: {inputSize}\tOutput size: {outputSize} bytes\tCompression: {compressionPercentage:F3}%\tTime: {timeTaken.TotalMilliseconds:F3} ms\tSpeed: {encodingSpeed:F3}x\tParameters: {parameters.Trim()}\tEncoder: {encoderInfo.FileName}\tVersion: {encoderInfo.Version}\tEncoder Path: {encoderInfo.DirectoryPath}{Environment.NewLine}");
            }
        }
        private void buttonLogColumnsAutoWidth_Click(object sender, EventArgs e)
        {
            dataGridViewLog.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells);
        }
        private void buttonAnalyzeLog_Click(object? sender, EventArgs e)
        {
            AnalyzeLog();
        }
        private async void AnalyzeLog()
        {
            var groupedEntries = new Dictionary<string, List<LogEntry>>();

            // 1. Collect all entries and group by Audio File Directory + Audio File Name + Encoder Directory + Encoder + Parameters
            foreach (DataGridViewRow row in dataGridViewLog.Rows)
            {
                if (row.IsNewRow) continue;

                string audioFileDirectory = row.Cells["AudioFileDirectory"].Value?.ToString();
                string fileName = row.Cells["Name"].Value?.ToString();
                string encoderDirectory = row.Cells["EncoderDirectory"].Value?.ToString();
                string encoder = row.Cells["Encoder"].Value?.ToString();
                string parameters = row.Cells["Parameters"].Value?.ToString();
                string speedStr = row.Cells["Speed"].Value?.ToString()?.Replace("x", "")?.Trim();

                if (string.IsNullOrEmpty(audioFileDirectory) ||
                    string.IsNullOrEmpty(fileName) ||
                    string.IsNullOrEmpty(encoderDirectory) ||
                    string.IsNullOrEmpty(encoder) ||
                    string.IsNullOrEmpty(parameters) ||
                    !double.TryParse(speedStr, out double speed))
                {
                    continue;
                }

                string key = $"{audioFileDirectory}|{fileName}|{encoderDirectory}|{encoder}|{parameters}";

                if (!groupedEntries.ContainsKey(key))
                    groupedEntries[key] = new List<LogEntry>();

                // Convert DataGridView row to LogEntry object
                var logEntry = new LogEntry
                {
                    Name = fileName,
                    Encoder = encoder,
                    Parameters = parameters,
                    Speed = speedStr,
                    SpeedForeColor = row.Cells["Speed"].Style.ForeColor,

                    InputFileSize = row.Cells["InputFileSize"].Value?.ToString(),
                    OutputFileSize = row.Cells["OutputFileSize"].Value?.ToString(),
                    Compression = row.Cells["Compression"].Value?.ToString(),
                    Time = row.Cells["Time"].Value?.ToString(),
                    Version = row.Cells["Version"].Value?.ToString(),
                    EncoderDirectory = row.Cells["EncoderDirectory"].Value?.ToString(),
                    FastestEncoder = row.Cells["FastestEncoder"].Value?.ToString(),
                    BestSize = row.Cells["BestSize"].Value?.ToString(),
                    SameSize = row.Cells["SameSize"].Value?.ToString(),
                    AudioFileDirectory = row.Cells["AudioFileDirectory"].Value?.ToString(),
                    MD5 = row.Cells["MD5"].Value?.ToString(),
                    Duplicates = row.Cells["Duplicates"].Value?.ToString(),

                    OutputForeColor = row.Cells["OutputFileSize"].Style.ForeColor,
                    CompressionForeColor = row.Cells["Compression"].Style.ForeColor
                };

                groupedEntries[key].Add(logEntry);
            }

            // 2. For each group calculate average speed and keep one entry
            var resultEntries = new List<LogEntry>();
            foreach (var group in groupedEntries.Values)
            {
                double avgSpeed = group.Average(x => double.Parse(x.Speed.Replace("x", "").Trim()));
                var bestEntry = group.First(); // Take the first entry as a template
                bestEntry.Speed = avgSpeed.ToString("F3") + "x";
                resultEntries.Add(bestEntry);
            }

            // 3. Split into encoding and decoding
            var encodeGroups = resultEntries.Where(e => !e.Parameters.Split(' ').Any(p => p == "-d" || p == "--decode")).ToList();
            var decodeGroups = resultEntries.Where(e => e.Parameters.Split(' ').Any(p => p == "-d" || p == "--decode")).ToList();

            // 4. Analysis for encoding: find fastest encoder and smallest file sizes
            var encodeFileParamGroups = encodeGroups.GroupBy(e => Path.Combine(e.AudioFileDirectory ?? "", e.Name ?? "") + "|" + e.Parameters).ToList();

            // 4.1 Find fastest encoder for each group
            foreach (var group in encodeFileParamGroups)
            {
                if (group.Count() <= 1) continue; // Skip groups with only one entry

                double maxSpeed = group.Max(e => double.Parse(e.Speed.Replace("x", "").Trim()));
                foreach (var entry in group)
                {
                    bool isFastest = (double.Parse(entry.Speed.Replace("x", "").Trim()) >= maxSpeed - 0.01);
                    entry.FastestEncoder = isFastest ? "fastest encoder" : string.Empty;
                }
            }

            // 4.2 Find smallest size for each group
            var fileSizeGroups = encodeGroups.GroupBy(e => Path.Combine(e.AudioFileDirectory ?? "", e.Name ?? "") + "|" + e.Parameters).ToList();

            var smallestSizes = new Dictionary<string, long>();
            var fileSizeCounts = new Dictionary<string, Dictionary<long, int>>();

            foreach (var group in fileSizeGroups)
            {
                if (group.Count() <= 1) continue; // Skip groups with only one entry

                string key = group.Key;
                var sizes = group.Select(e =>
                {
                    long.TryParse(e.OutputFileSize?.Replace(" ", "").Trim(), out long size);
                    return size;
                }).Where(size => size > 0).ToList();
                if (sizes.Count == 0) continue;
                long minSize = sizes.Min();
                smallestSizes[key] = minSize;

                var countDict = new Dictionary<long, int>();
                foreach (var size in sizes)
                {
                    if (!countDict.ContainsKey(size))
                        countDict[size] = 0;
                    countDict[size]++;
                }
                fileSizeCounts[key] = countDict;
            }

            // 4.3 Apply BestSize and SameSize flags
            var finalEncodeEntries = new List<LogEntry>();
            foreach (var entry in encodeGroups)
            {
                long.TryParse(entry.OutputFileSize?.Replace(" ", "").Trim(), out long outputSize);

                string key = Path.Combine(entry.AudioFileDirectory ?? "", entry.Name ?? "") + "|" + entry.Parameters;

                // If group has more than one entry
                bool isGroupLargeEnough = fileSizeGroups.FirstOrDefault(g => g.Key == key)?.Count() > 1;

                entry.BestSize = isGroupLargeEnough
                    && smallestSizes.TryGetValue(key, out long minSize)
                    && outputSize == minSize
                    ? "smallest size"
                    : string.Empty;

                entry.SameSize = isGroupLargeEnough
                    && fileSizeCounts.TryGetValue(key, out var sizeCount)
                    && sizeCount.TryGetValue(outputSize, out int count)
                    && count > 1
                    ? "has same size"
                    : string.Empty;

                finalEncodeEntries.Add(entry);
            }

            // 5. Analysis for decoding: find fastest decoder
            var decodeFileParamGroups = decodeGroups.GroupBy(e => Path.Combine(e.AudioFileDirectory ?? "", e.Name ?? "") + "|" + e.Parameters).ToList();

            foreach (var group in decodeFileParamGroups)
            {
                if (group.Count() <= 1) continue; // Skip groups with only one entry

                double maxSpeed = group.Max(e => double.Parse(e.Speed.Replace("x", "").Trim()));
                foreach (var entry in group)
                {
                    bool isFastest = (double.Parse(entry.Speed.Replace("x", "").Trim()) >= maxSpeed - 0.01);
                    entry.FastestEncoder = isFastest ? "fastest decoder" : string.Empty;
                }
            }

            // 6. Merge results
            var finalEntries = finalEncodeEntries.Concat(decodeGroups).ToList();

            // 7. Update DataGridView
            await this.InvokeAsync(() =>
            {
                dataGridViewLog.Rows.Clear();

                foreach (var entry in finalEntries)
                {
                    int rowIndex = dataGridViewLog.Rows.Add(
                        entry.Name,
                        entry.InputFileSize,
                        entry.OutputFileSize,
                        entry.Compression,
                        entry.Time,
                        entry.Speed,
                        entry.Parameters,
                        entry.Encoder,
                        entry.Version,
                        entry.EncoderDirectory,
                        entry.FastestEncoder,
                        entry.BestSize,
                        entry.SameSize,
                        entry.AudioFileDirectory,
                        entry.MD5,
                        entry.Duplicates
                    );

                    // Restore colors
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
            public string InputFileSize { get; set; }
            public string OutputFileSize { get; set; }
            public string Compression { get; set; }
            public string Time { get; set; }
            public string Speed { get; set; }
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

            public Color OutputForeColor { get; set; } // Color for OutputFileSize
            public Color CompressionForeColor { get; set; } // Color for Compression
            public Color SpeedForeColor { get; set; } // Color for Speed
        }
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
                    InputFileSize = row.Cells["InputFileSize"].Value?.ToString(),
                    OutputFileSize = row.Cells["OutputFileSize"].Value?.ToString(),
                    Compression = row.Cells["Compression"].Value?.ToString(),
                    Time = row.Cells["Time"].Value?.ToString(),
                    Speed = row.Cells["Speed"].Value?.ToString(),
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

                    OutputForeColor = row.Cells[2].Style.ForeColor, // Color for OutputFileSize
                    CompressionForeColor = row.Cells[3].Style.ForeColor, // Color for Compression
                    SpeedForeColor = row.Cells[5].Style.ForeColor // Color for Speed
                };

                dataToSort.Add(logEntry);
            }

            // Perform multi-level sorting
            var sortedData = dataToSort
                .OrderBy(x => x.AudioFileDirectory)
                .ThenBy(x => x.Name)
                .ThenBy(x => x.Parameters)
                .ThenBy(x => x.EncoderDirectory)
                .ThenBy(x => x.Encoder)
                .ToList();

            // Clear DataGridView and add sorted data
            dataGridViewLog.Rows.Clear();
            foreach (var data in sortedData)
            {
                int rowIndex = dataGridViewLog.Rows.Add(
                    data.Name,
                    data.InputFileSize,
                    data.OutputFileSize,
                    data.Compression,
                    data.Time,
                    data.Speed,
                    data.Parameters,
                    data.Encoder,
                    data.Version,
                    data.EncoderDirectory,
                    data.FastestEncoder,
                    data.BestSize,
                    data.SameSize,
                    data.AudioFileDirectory,
                    data.MD5,
                    data.Duplicates);


                // Set text color
                dataGridViewLog.Rows[rowIndex].Cells[2].Style.ForeColor = data.OutputForeColor; // OutputFileSize
                dataGridViewLog.Rows[rowIndex].Cells[3].Style.ForeColor = data.CompressionForeColor; // Compression
                dataGridViewLog.Rows[rowIndex].Cells[5].Style.ForeColor = data.SpeedForeColor; // Speed
            }
            dataGridViewLog.ClearSelection();
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
                            if (cellValue != null && long.TryParse(cellValue.ToString().Replace(" ", ""), out long numericValue))
                            {
                                worksheet.Cell(i + 2, j + 1).Value = numericValue; // Write as number

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
            dataGridViewLog.Rows.Clear();
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
                buttonRemoveAudiofile.PerformClick();
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

        // Actions (Buttons)
        private ManualResetEventSlim _pauseEvent = new ManualResetEventSlim(true); // Initially not paused
        private async void buttonStartEncode_Click(object? sender, EventArgs e)
        {
            dataGridViewLog.ClearSelection(); // Clear selection

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
                labelEncoderProgress.Text = string.Empty;
                labelDecoderProgress.Text = string.Empty;
                dataGridViewLog.ClearSelection();
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
                    isExecuting = false; // Reset the flag if there are no files
                    return;
                }

                // 5. Check if there is at least one audio file
                if (selectedAudioFiles.Count == 0)
                {
                    MessageBox.Show("Select at least one audio file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    isExecuting = false; // Reset the flag if there are no files
                    return;
                }

                // Set maximum values for the progress bar
                progressBarEncoder.Maximum = selectedEncoders.Count * selectedAudioFiles.Count;
                // Reset the progress bar
                progressBarEncoder.Value = 0;
                labelEncoderProgress.Text = $"{progressBarEncoder.Value}/{progressBarEncoder.Maximum}";

                // Create a temporary directory for the output file
                Directory.CreateDirectory(tempFolderPath);

                foreach (var encoder in selectedEncoders)
                {
                    foreach (var audioFilePath in selectedAudioFiles)
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

                        // Start the process and wait for completion
                        try
                        {
                            await Task.Run(() =>
                            {
                                if (_isPaused)
                                {
                                    _pauseEvent.Wait(); // Wait for pause in the background thread
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

                                    stopwatch.Reset();
                                    stopwatch.Start();

                                    if (!_isEncodingStopped)
                                    {
                                        _process.Start();

                                        // Set process priority
                                        try
                                        {
                                            if (!_process.HasExited)
                                            {
                                                _process.PriorityClass = GetProcessPriorityClass(comboBoxCPUPriority.SelectedItem.ToString());
                                            }
                                        }
                                        catch (InvalidOperationException)
                                        {
                                            // Process has completed, log or handle as needed
                                        }

                                        _process.WaitForExit();
                                    }

                                    stopwatch.Stop();
                                }
                            });

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
                                LogProcessResults(outputFilePath, audioFilePath, parameters, encoder);
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Error starting encoding process: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            isExecuting = false;
                            return;
                        }
                        progressBarEncoder.Invoke((MethodInvoker)(() =>
                        {
                            progressBarEncoder.Value++;
                            labelEncoderProgress.Text = $"{progressBarEncoder.Value}/{progressBarEncoder.Maximum}";
                        }));
                    }
                }
                if (checkBoxAutoAnalyzeLog.Checked)
                {
                    AnalyzeLog();
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
                    labelEncoderProgress.Text = string.Empty;
                    labelDecoderProgress.Text = string.Empty;
                    dataGridViewLog.ClearSelection();
                }));
            }
        }
        private async void buttonStartDecode_Click(object? sender, EventArgs e)
        {
            dataGridViewLog.ClearSelection(); // Clear selection

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
                labelEncoderProgress.Text = string.Empty;
                labelDecoderProgress.Text = string.Empty;
                dataGridViewLog.ClearSelection();
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
                    isExecuting = false; // Reset the flag if there are no files
                    return;
                }

                // 5. Check if there is at least one .flac audio file
                if (selectedFlacAudioFiles.Count == 0)
                {
                    MessageBox.Show("Select at least one FLAC audio file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    isExecuting = false; // Reset the flag if there are no files
                    return;
                }

                // Set maximum values for the progress bar
                progressBarDecoder.Maximum = selectedEncoders.Count * selectedFlacAudioFiles.Count;
                // Reset the progress bar
                progressBarDecoder.Value = 0; // Reset progress bar value
                labelEncoderProgress.Text = $"{progressBarDecoder.Value}/{progressBarDecoder.Maximum}";

                // Create a temporary directory for the output file
                Directory.CreateDirectory(tempFolderPath);

                foreach (var encoder in selectedEncoders)
                {
                    foreach (var audioFilePath in selectedFlacAudioFiles)
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

                        // Start the process and wait for completion
                        try
                        {
                            await Task.Run(() =>
                            {
                                if (_isPaused)
                                {
                                    _pauseEvent.Wait(); // Wait for pause in the background thread
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

                                    stopwatch.Reset();
                                    stopwatch.Start();

                                    if (!_isEncodingStopped)
                                    {
                                        _process.Start();

                                        // Set process priority
                                        try
                                        {
                                            if (!_process.HasExited)
                                            {
                                                _process.PriorityClass = GetProcessPriorityClass(comboBoxCPUPriority.SelectedItem.ToString());
                                            }
                                        }
                                        catch (InvalidOperationException)
                                        {
                                            // Process has completed, log or handle as needed
                                        }

                                        _process.WaitForExit();
                                    }

                                    stopwatch.Stop();
                                }
                            });

                            if (!_isEncodingStopped)
                            {
                                LogProcessResults(outputFilePath, audioFilePath, parameters, encoder);
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Error starting decoding process: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            isExecuting = false;
                            return;
                        }
                        progressBarDecoder.Invoke((MethodInvoker)(() =>
                        {
                            progressBarDecoder.Value++;
                            labelDecoderProgress.Text = $"{progressBarDecoder.Value}/{progressBarDecoder.Maximum}";
                        }));
                    }
                }
                if (checkBoxAutoAnalyzeLog.Checked)
                {
                    AnalyzeLog();
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
                    labelEncoderProgress.Text = string.Empty;
                    labelDecoderProgress.Text = string.Empty;
                    dataGridViewLog.ClearSelection();
                }));
            }
        }
        private async void buttonStartJobList_Click(object? sender, EventArgs e)
        {
            dataGridViewLog.ClearSelection(); // Clear selection

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
                labelEncoderProgress.Text = string.Empty;
                labelDecoderProgress.Text = string.Empty;
                dataGridViewLog.ClearSelection();
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

                // Count the number of tasks and passes for Encode
                int totalEncodeTasks = listViewJobs.Items
                    .Cast<ListViewItem>()
                    .Where(item => item.Checked && string.Equals(NormalizeSpaces(item.Text), "Encode", StringComparison.OrdinalIgnoreCase))
                    .Sum(item => int.Parse(item.SubItems[1].Text.Trim()));

                // Count the number of tasks and passes for Decode
                int totalDecodeTasks = listViewJobs.Items
                    .Cast<ListViewItem>()
                    .Where(item => item.Checked && string.Equals(NormalizeSpaces(item.Text), "Decode", StringComparison.OrdinalIgnoreCase))
                    .Sum(item => int.Parse(item.SubItems[1].Text.Trim()));

                // 1. Check if there is at least one encoder
                if (selectedEncoders.Count == 0)
                {
                    MessageBox.Show("Select at least one encoder.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    isExecuting = false; // Reset the flag if there are no files
                    return;
                }

                // 2. Check if there is at least one task (Encode or Decode)
                if (totalEncodeTasks == 0 && totalDecodeTasks == 0)
                {
                    MessageBox.Show("Select at least one job.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    isExecuting = false; // Reset the flag before returning
                    return;
                }

                // 3. Check if there are FLAC files if Decode tasks are checked but Encode tasks are not
                if (totalDecodeTasks > 0 && totalEncodeTasks == 0 && selectedFlacAudioFiles.Count == 0)
                {
                    MessageBox.Show("Select at least one FLAC file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    isExecuting = false; // Reset the flag before returning
                    return;
                }

                // 4. Check if there are audio files if Decode tasks are checked but Encode tasks are not
                if (totalDecodeTasks > 0 && totalEncodeTasks == 0 && selectedAudioFiles.Count == 0)
                {
                    MessageBox.Show("Select at least one FLAC file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    isExecuting = false; // Reset the flag before returning
                    return;
                }

                // 5. Check if there is at least one audio file
                if (selectedAudioFiles.Count == 0)
                {
                    MessageBox.Show("Select at least one audio file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    isExecuting = false; // Reset the flag if there are no files
                    return;
                }

                // Set maximum values for progress bars
                progressBarEncoder.Maximum = selectedEncoders.Count * selectedAudioFiles.Count * totalEncodeTasks;
                progressBarDecoder.Maximum = selectedEncoders.Count * selectedFlacAudioFiles.Count * totalDecodeTasks;

                // Reset progress bars
                progressBarEncoder.Value = 0;
                progressBarDecoder.Value = 0;

                labelEncoderProgress.Text = $"{progressBarEncoder.Value}/{progressBarEncoder.Maximum}";
                labelDecoderProgress.Text = $"{progressBarDecoder.Value}/{progressBarDecoder.Maximum}";

                // Create a temporary directory for the output file
                Directory.CreateDirectory(tempFolderPath);

                foreach (ListViewItem item in listViewJobs.Items)
                {
                    // Check if the task is checked
                    if (item.Checked)
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
                                foreach (var encoder in selectedEncoders)
                                {
                                    foreach (var audioFilePath in selectedAudioFiles)
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

                                        // Start the process and wait for completion
                                        try
                                        {
                                            await Task.Run(() =>
                                            {
                                                if (_isPaused)
                                                {
                                                    _pauseEvent.Wait(); // Wait for pause in the background thread
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

                                                    stopwatch.Reset();
                                                    stopwatch.Start();

                                                    if (!_isEncodingStopped)
                                                    {
                                                        _process.Start();

                                                        // Set process priority
                                                        try
                                                        {
                                                            if (!_process.HasExited)
                                                            {
                                                                _process.PriorityClass = GetProcessPriorityClass(comboBoxCPUPriority.SelectedItem.ToString());
                                                            }
                                                        }
                                                        catch (InvalidOperationException)
                                                        {
                                                            // Process has completed, log or handle as needed
                                                        }

                                                        _process.WaitForExit();
                                                    }

                                                    stopwatch.Stop();
                                                }
                                            });

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
                                                LogProcessResults(outputFilePath, audioFilePath, parameters, encoder);
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            MessageBox.Show($"Error starting encoding process: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                            isExecuting = false;
                                            return;
                                        }
                                        progressBarEncoder.Invoke((MethodInvoker)(() =>
                                        {
                                            progressBarEncoder.Value++;
                                            labelEncoderProgress.Text = $"{progressBarEncoder.Value}/{progressBarEncoder.Maximum}";
                                        }));
                                    }
                                }
                            }
                            else if (string.Equals(jobType, "Decode", StringComparison.OrdinalIgnoreCase))
                            {
                                foreach (var encoder in selectedEncoders)
                                {
                                    foreach (var audioFilePath in selectedFlacAudioFiles)
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

                                        // Start the process and wait for completion
                                        try
                                        {
                                            await Task.Run(() =>
                                            {
                                                if (_isPaused)
                                                {
                                                    _pauseEvent.Wait(); // Wait for pause in the background thread
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

                                                    stopwatch.Reset();
                                                    stopwatch.Start();

                                                    if (!_isEncodingStopped)
                                                    {
                                                        _process.Start();

                                                        // Set process priority
                                                        try
                                                        {
                                                            if (!_process.HasExited)
                                                            {
                                                                _process.PriorityClass = GetProcessPriorityClass(comboBoxCPUPriority.SelectedItem.ToString());
                                                            }
                                                        }
                                                        catch (InvalidOperationException)
                                                        {
                                                            // Process has completed, log or handle as needed
                                                        }

                                                        _process.WaitForExit();
                                                    }

                                                    stopwatch.Stop();
                                                }
                                            });

                                            if (!_isEncodingStopped)
                                            {
                                                LogProcessResults(outputFilePath, audioFilePath, parameters, encoder);
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            MessageBox.Show($"Error starting decoding process: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                            isExecuting = false;
                                            return;
                                        }
                                        progressBarDecoder.Invoke((MethodInvoker)(() =>
                                        {
                                            progressBarDecoder.Value++;
                                            labelDecoderProgress.Text = $"{progressBarDecoder.Value}/{progressBarDecoder.Maximum}";
                                        }));
                                    }
                                }
                            }
                        }
                    }
                }
                if (checkBoxAutoAnalyzeLog.Checked)
                {
                    AnalyzeLog();
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
                    labelEncoderProgress.Text = string.Empty;
                    labelDecoderProgress.Text = string.Empty;
                    dataGridViewLog.ClearSelection();
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
        private void buttonepr8_Click(object? sender, EventArgs e)
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
                    _process.Dispose(); // Release resources
                    _process = null; // Null the process reference
                    progressBarEncoder.Value = 0;
                    progressBarDecoder.Value = 0;
                    labelEncoderProgress.Text = $"";
                    labelDecoderProgress.Text = $"";
                    dataGridViewLog.ClearSelection(); // Clear selection
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
            timer.Interval = 4000; // Set interval to 2 seconds
            timer.Tick += (s, e) =>
            {
                labelStopped.Visible = false; // Hide label
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
        private async void checkBoxCheckForUpdatesOnStartup_CheckedChanged(object sender, EventArgs e)
        {
            await CheckForUpdatesAsync();
        }
        private async Task CheckForUpdatesAsync()
        {
            if (!checkBoxCheckForUpdatesOnStartup.Checked)
                return;

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
            catch (Exception ex)
            {
                MessageBox.Show($"Error checking for updates:\n{ex.Message}",
                              "Network Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private Version ParseVersion(string versionStr)
        {
            if (string.IsNullOrEmpty(versionStr))
                return new Version("0.0.0");

            var parts = versionStr.Split(' ');
            if (parts.Length >= 3 && parts[1] == "build")
            {
                if (int.TryParse(parts[2], out int buildNumber))
                {
                    // Version will be compared in Major.Minor.Build format
                    return new Version($"{parts[0]}.{buildNumber}");
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

            LoadSettings(); // Load settings
            LoadEncoders(); // Load executable files
            LoadAudioFiles(); // Load audio files
            LoadJobs(); // Load contents of Settings_joblist.txt
            await CheckForUpdatesAsync();
            this.ActiveControl = null; // Remove focus from all elements

        }
        private void Form1_FormClosing(object? sender, FormClosingEventArgs e)
        {
            SaveSettings(); // Save settings before closing
            SaveEncoders(); // Save encoder list
            SaveAudioFiles(); // Save audio file list
            SaveJobs(); // Save jobList contents

            cpuUsageTimer?.Stop();
            cpuUsageTimer?.Dispose();

            _pauseEvent?.Dispose();
            cpuCounter?.Dispose();

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