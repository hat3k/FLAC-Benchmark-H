using MediaInfoLib;
using System.Diagnostics;
using System.Globalization;
using System.Management;
using System.Text;
using System.Windows.Forms;
namespace FLAC_Benchmark_H
{
    public partial class Form1 : Form
    {
        private int physicalCores;
        private int threadCount;
        private Process _process; // ���� ��� �������� �������� ��������
        private const string SettingsFilePath = "Settings_general.txt"; // ���� � ����� ��������
        private const string JobsFilePath = "Settings_jobs.txt"; // ���� � ����� jobs
        private const string executablesFilePath = "Settings_flac_executables.txt"; // ���� � ����� ��� ���������� ����������� ������
        private const string audioFilesFilePath = "Settings_audio_files.txt"; // ���� � ����� ��� ���������� �����������
        private Stopwatch stopwatch;
        private PerformanceCounter cpuCounter;
        private System.Windows.Forms.Timer cpuUsageTimer; // ��������� ����, ��� ��� Timer �� System.Windows.Forms
        private bool _isEncodingStopped = false;
        private string tempFolderPath; // ���� ��� �������� ���� � ��������� �����
        private bool isCpuInfoLoaded = false;
        public Form1()
        {
            InitializeComponent();
            InitializeDragAndDrop(); // ������������� drag-and-drop
            this.FormClosing += Form1_FormClosing; // ����������� ����������� ������� �������� �����
            this.listViewFlacExecutables.KeyDown += ListViewFlacExecutables_KeyDown;
            this.listViewAudioFiles.KeyDown += ListViewAudioFiles_KeyDown;
            this.listViewJobs.KeyDown += ListViewJobs_KeyDown;
            LoadCPUInfo(); // ��������� ���������� � ����������
            this.KeyPreview = true;
            stopwatch = new Stopwatch(); // ������������� ������� Stopwatch
            cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            cpuUsageTimer = new System.Windows.Forms.Timer(); // ���� ��������� System.Windows.Forms.Timer
            cpuUsageTimer.Interval = 250; // ������ 250 ��
            cpuUsageTimer.Tick += async (sender, e) => await UpdateCpuUsageAsync();
            cpuUsageTimer.Start();
            InitializedataGridViewLog();
            tempFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp"); // ������������� ���� � ��������� �����
            _process = new Process(); // Initialize _process to avoid nullability warning

            // �������� ���������������� ��������� ��� listViewJobs
            listViewJobs.OwnerDraw = true;
            listViewJobs.DrawColumnHeader += ListViewJobs_DrawColumnHeader;
            listViewJobs.DrawSubItem += ListViewJobs_DrawSubItem;
        }
        private void ListViewJobs_DrawColumnHeader(object sender, DrawListViewColumnHeaderEventArgs e)
        {
            e.DrawDefault = true;
        }
        private void ListViewJobs_DrawSubItem(object sender, DrawListViewSubItemEventArgs e)
        {
            if (e.ColumnIndex == 0) // ������� � ����� ������ (Encode/Decode)
            {
                e.DrawBackground();

                // ��������� ��������
                if (listViewJobs.CheckBoxes)
                {
                    CheckBoxRenderer.DrawCheckBox(e.Graphics,
                        new Point(e.Bounds.Left + 4, e.Bounds.Top + 2),
                        e.Item.Checked ? System.Windows.Forms.VisualStyles.CheckBoxState.CheckedNormal
                                       : System.Windows.Forms.VisualStyles.CheckBoxState.UncheckedNormal);
                }

                Color textColor = e.SubItem?.Text.Contains("Encode", StringComparison.OrdinalIgnoreCase) == true
                    ? Color.Green
                    : e.SubItem?.Text.Contains("Decode", StringComparison.OrdinalIgnoreCase) == true
                        ? Color.Red
                        : e.Item?.ForeColor ?? Color.Black;

                using (var brush = new SolidBrush(textColor))
                {
                    // ������� ����� ������, ����� �� ����������� �������
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
        // ����� ��� �������� ���������� � ����������
        private void LoadCPUInfo()
        {
            if (!isCpuInfoLoaded)
            {
                try
                {
                    physicalCores = 0;
                    threadCount = 0;
                    // ������� ������ ��� ��������� ���������� � �����������
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
                    // ��������� ����� � ����������� � ����������
                    if (physicalCores > 0 && threadCount > 0)
                    {
                        labelCPUinfo.Text = $"Your system has:\nCores: {physicalCores}, Threads: {threadCount}";
                    }
                    else
                    {
                        labelCPUinfo.Text = "Unable to retrieve CPU information.";
                    }
                    isCpuInfoLoaded = true;
                }
                catch (Exception ex)
                {
                    // ���������� ������ � labelCPUinfo
                    labelCPUinfo.Text = "Error loading CPU info: " + ex.Message;
                }
            }
        }
        private async Task UpdateCpuUsageAsync()
        {
            float cpuUsage = await Task.Run(() => cpuCounter.NextValue());
            labelCPUinfo.Text = $"Your system has:\nCores: {physicalCores}, Threads: {threadCount}\nCPU Usage: {cpuUsage:F2}%";
        }
        private string GetExecutableInfo(string executablePath)
        {
            using (Process process = new Process())
            {
                process.StartInfo.FileName = executablePath;
                process.StartInfo.Arguments = "--version";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true; // �������������� ����������� �����
                process.StartInfo.CreateNoWindow = true;
                process.Start();
                string version = process.StandardOutput.ReadLine(); // ������ ������ ������ ������
                process.WaitForExit();
                return version; // ���������� ������ ������
            }
        }
        // ����� ��� ��������� ������������ � ����������� ����������
        private (string duration, string bitDepth, string samplingRate, string size) GetAudioInfo(string audioFile)
        {
            var mediaInfo = new MediaInfoLib.MediaInfo();
            mediaInfo.Open(audioFile);
            string duration = mediaInfo.Get(StreamKind.Audio, 0, "Duration") ?? "N/A";
            string bitDepth = mediaInfo.Get(StreamKind.Audio, 0, "BitDepth") ?? "N/A";
            string samplingRate = mediaInfo.Get(StreamKind.Audio, 0, "SamplingRate/String") ?? "N/A";
            string fileSize = mediaInfo.Get(StreamKind.General, 0, "FileSize") ?? "N/A";
            mediaInfo.Close();
            return (duration, bitDepth, samplingRate, fileSize);
        }
        private void BackupJobsFile()
        {
            try
            {
                if (File.Exists(JobsFilePath))
                {
                    string backupPath = $"{JobsFilePath}.bak";
                    File.Copy(JobsFilePath, backupPath, true); // �������� ����, ���� ����� ��� ����������, ��������������
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating backup for jobs file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ����� ��� ���������� ��������
        private void SaveSettings()
        {
            try
            {
                var settings = new[]
                {
                    $"CompressionLevel={textBoxCompressionLevel.Text}",
                    $"Threads={textBoxThreads.Text}",
                    $"CommandLineOptions={textBoxCommandLineOptionsEncoder.Text}",
                    $"HighPriority={checkBoxHighPriority.Checked}",
                    $"TempFolderPath={tempFolderPath}",
                    $"ClearTempFolderOnExit={checkBoxClearTempFolder.Checked}"
            };
                File.WriteAllLines(SettingsFilePath, settings);
                SaveExecutables();
                SaveAudioFiles(); // ���������� �����������
                SaveJobs(); // ��������� ���������� jobList
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving settings: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ����� ��� �������� ��������
        private void LoadSettings()
        {
            // �������� ���� � ��������� �����
            tempFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp");
            try
            {
                string[] lines = File.ReadAllLines(SettingsFilePath);
                foreach (var line in lines)
                {
                    var parts = line.Split(new[] { '=' }, 2); // ��������� ������ �� ���� � ��������, ������������ ��������� �� ������� ����� '='
                    if (parts.Length == 2)
                    {
                        var key = parts[0].Trim();
                        var value = parts[1].Trim();
                        // ��������� �������� � ��������������� ����
                        switch (key)
                        {
                            case "CompressionLevel":
                                textBoxCompressionLevel.Text = value;
                                break;
                            case "Threads":
                                textBoxThreads.Text = value;
                                break;
                            case "CommandLineOptions":
                                textBoxCommandLineOptionsEncoder.Text = value;
                                break;
                            case "HighPriority":
                                checkBoxHighPriority.Checked = bool.Parse(value);
                                break;
                            case "TempFolderPath": // ��������� ���� � ��������� �����
                                tempFolderPath = value;
                                break;
                            case "ClearTempFolderOnExit":
                                checkBoxClearTempFolder.Checked = bool.Parse(value);
                                break;
                        }
                    }
                }
            }
            catch
            {
            }
            // ����������� ���������� ���������� �� ����, ��� �� �������� ���� ��������
            LoadExecutables(); // �������� ����������� ������
            LoadAudioFiles(); // �������� �����������
            LoadJobs(); // ��������� ���������� Settings_joblist.txt ����� �������� ������ ��������
        }

        private void SaveExecutables()
        {
            try
            {
                var executables = listViewFlacExecutables.Items
                .Cast<ListViewItem>()
                .Select(item => $"{item.Tag}~{item.Checked}")
                .ToArray();
                File.WriteAllLines(executablesFilePath, executables);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving executables: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                File.WriteAllLines(audioFilesFilePath, audioFiles);
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
                    .Select(item => $"{item.Text}~{item.Checked}~{item.SubItems[1].Text}") // ��������� �����, ��������� �������� � ���������
                    .ToArray();
                File.WriteAllLines(JobsFilePath, jobList);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving jobs to file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void InitializeDragAndDrop()
        {
            // ��������� �������������� ������ � ListView ��� ��������
            listViewFlacExecutables.AllowDrop = true;
            listViewFlacExecutables.DragEnter += ListViewFlacExecutables_DragEnter;
            listViewFlacExecutables.DragDrop += ListViewFlacExecutables_DragDrop;
            // ��������� �������������� ������ � ListView ��� �����������
            listViewAudioFiles.AllowDrop = true;
            listViewAudioFiles.DragEnter += ListViewAudioFiles_DragEnter;
            listViewAudioFiles.DragDrop += ListViewAudioFiles_DragDrop;
            // ��������� �������������� ������ � ListView ��� ������� �����
            listViewJobs.AllowDrop = true;
            listViewJobs.DragEnter += ListViewJobs_DragEnter;
            listViewJobs.DragDrop += ListViewJobs_DragDrop;
        }

        // ���������� DragEnter ��� ListViewFlacExecutables
        private void ListViewFlacExecutables_DragEnter(object? sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                bool hasExeFiles = files.Any(file =>
                Directory.Exists(file) ||
                Path.GetExtension(file).Equals(".exe", StringComparison.OrdinalIgnoreCase));
                e.Effect = hasExeFiles ? DragDropEffects.Copy : DragDropEffects.None;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }
        // ���������� DragDrop ��� ListViewFlacExecutables
        private void ListViewFlacExecutables_DragDrop(object? sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            foreach (var file in files)
            {
                if (Directory.Exists(file)) // ���� ��� �����, ���� ����������� ����� ������ ����������
                {
                    AddExecutableFiles(file);
                }
                else if (Path.GetExtension(file).Equals(".exe", StringComparison.OrdinalIgnoreCase))
                {
                    AddExecutableFileToListView(file); // ���������� ����� �����
                }
            }
        }
        // ����������� ����� ��� ���������� ����������� ������ � ListView
        private void AddExecutableFiles(string directory)
        {
            try
            {
                // ������� ��� ���������� � ��������� ������������ exe � ������� ����������
                var exeFiles = Directory.GetFiles(directory, "*.exe", SearchOption.AllDirectories);
                foreach (var exeFile in exeFiles)
                {
                    AddExecutableFileToListView(exeFile); // ���������� ����� �����
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error accessing directory: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        // �������� ����������� ������ �� ����� txt
        private void LoadExecutables()
        {
            if (File.Exists(executablesFilePath))
            {
                try
                {
                    string[] lines = File.ReadAllLines(executablesFilePath);
                    listViewFlacExecutables.Items.Clear();
                    foreach (var line in lines)
                    {
                        var parts = line.Split('~');
                        if (parts.Length == 2)
                        {
                            string executablePath = parts[0]; // ������ ����
                            bool isChecked = bool.Parse(parts[1]); // ������ "��������"
                            AddExecutableFileToListView(executablePath, isChecked); // �������� ����� ����������
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading executables: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
        // ����� ����� ���������� ����������� ������ � ListView
        private void AddExecutableFileToListView(string executable, bool isChecked = true)
        {
            var version = GetExecutableInfo(executable); // �������� ������ ������������ �����
            long fileSize = new FileInfo(executable).Length; // �������� ������ �����
            DateTime lastModifiedDate = new FileInfo(executable).LastWriteTime; // �������� ���� ��������� �����
            var item = new ListViewItem(Path.GetFileName(executable))
            {
                Tag = executable, // ������ ���� �������� � Tag
                Checked = isChecked // ������������� ��������� �� ���������
            };
            item.SubItems.Add(version); // ��������� ������ � ������ �������
            item.SubItems.Add($"{fileSize:n0} bytes"); // ��������� ������ � ������ �������
            item.SubItems.Add(lastModifiedDate.ToString("yyyy.MM.dd HH:mm")); // ��������� ���� ��������� � �������� �������
            listViewFlacExecutables.Items.Add(item); // ��������� ������� � ListView
        }

        // ���������� DragEnter ��� ListViewAudioFiles
        private void ListViewAudioFiles_DragEnter(object? sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                bool hasAudioFiles = files.Any(file =>
                Directory.Exists(file) ||
                Path.GetExtension(file).Equals(".wav", StringComparison.OrdinalIgnoreCase) ||
                Path.GetExtension(file).Equals(".flac", StringComparison.OrdinalIgnoreCase));
                e.Effect = hasAudioFiles ? DragDropEffects.Copy : DragDropEffects.None;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }
        // ���������� DragDrop ��� ListViewAudioFiles
        private void ListViewAudioFiles_DragDrop(object? sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            foreach (var file in files)
            {
                if (Directory.Exists(file)) // ���� ��� �����, ���� ���������� ������ ����������
                {
                    AddAudioFiles(file);
                }
                else if (Path.GetExtension(file).Equals(".wav", StringComparison.OrdinalIgnoreCase) ||
                Path.GetExtension(file).Equals(".flac", StringComparison.OrdinalIgnoreCase))
                {
                    AddAudioFileToListView(file); // ���������� ����� �����
                }
            }
        }
        // ����������� ����� ��� ���������� ����������� �� ���������� � ListView
        private void AddAudioFiles(string directory)
        {
            try
            {
                // ������� ��� ���������� � ��������� ������������ � ������� ����������
                var audioFiles = Directory.GetFiles(directory, "*.wav", SearchOption.AllDirectories)
                .Concat(Directory.GetFiles(directory, "*.flac", SearchOption.AllDirectories));
                foreach (var audioFile in audioFiles)
                {
                    AddAudioFileToListView(audioFile); // ���������� ����� �����
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error accessing directory: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        // ����� ��� �������� ����������� �� ����� txt
        private void LoadAudioFiles()
        {
            if (File.Exists(audioFilesFilePath))
            {
                try
                {
                    string[] lines = File.ReadAllLines(audioFilesFilePath);
                    listViewAudioFiles.Items.Clear();
                    foreach (var line in lines)
                    {
                        var parts = line.Split('~');
                        if (parts.Length == 2)
                        {
                            string audioFilePath = parts[0]; // ������ ����
                            bool isChecked = bool.Parse(parts[1]); // ������ "��������"
                            AddAudioFileToListView(audioFilePath, isChecked); // �������� ����� ����������
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading audio files: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
        // ����� ����� ���������� ����������� � ListView
        private void AddAudioFileToListView(string audioFile, bool isChecked = true)
        {
            var item = new ListViewItem(Path.GetFileName(audioFile))
            {
                Tag = audioFile, // ������ ���� �������� � Tag
                Checked = isChecked // ������������� ��������� �� ���������
            };
            var (duration, bitDepth, samplingRate, fileSize) = GetAudioInfo(audioFile); // �������� ���������� � �����
            item.SubItems.Add(Convert.ToInt64(duration).ToString("N0") + " ms"); // ������������
            item.SubItems.Add(bitDepth + " bit"); // �����������
            item.SubItems.Add(samplingRate); // ������� �������������
            item.SubItems.Add(Convert.ToInt64(fileSize).ToString("N0") + " bytes"); // ������ �����
            listViewAudioFiles.Items.Add(item); // ��������� ������� � ListView
        }

        // ���������� DragEnter ��� ListViewJobs
        private void ListViewJobs_DragEnter(object? sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                bool hasTxtFiles = files.Any(file =>
                    Directory.Exists(file) ||
                    Path.GetExtension(file).Equals(".txt", StringComparison.OrdinalIgnoreCase));
                e.Effect = hasTxtFiles ? DragDropEffects.Copy : DragDropEffects.None;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }
        // ���������� DragDrop ��� ListViewJobs
        private void ListViewJobs_DragDrop(object? sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            foreach (var file in files)
            {
                if (Directory.Exists(file)) // ���� ��� �����, ���� .txt ����� ������ ����������
                {
                    AddJobsFromDirectory(file);
                }
                else if (Path.GetExtension(file).Equals(".txt", StringComparison.OrdinalIgnoreCase))
                {
                    LoadJobsFromFile(file); // ���������� ����� �����
                }
            }
        }
        // ����������� ����� ��� ���������� ����� �� ���������� � ListView
        private void AddJobsFromDirectory(string directory)
        {
            try
            {
                // ������� ��� .txt ����� � �������� ����������� � ������� ����������
                var txtFiles = Directory.GetFiles(directory, "*.txt", SearchOption.AllDirectories);
                foreach (var txtFile in txtFiles)
                {
                    LoadJobsFromFile(txtFile); // ��������� ������ �� ���������� .txt �����
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error accessing directory: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        // ����� �������� ����� �� �����
        private void LoadJobsFromFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                MessageBox.Show("The specified file does not exist.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                string[] lines = File.ReadAllLines(filePath);
            //   listViewJobs.Items.Clear(); // ������� ������������ �������� ����� ��������� �����

                foreach (var line in lines)
                {
                    var parts = line.Split('~'); // ��������� ������ �� �����
                    if (parts.Length == 3 && bool.TryParse(parts[1], out bool isChecked))
                    {
                        string jobName = parts[0];
                        string parameters = parts[2];
                        AddJobsToListView(jobName, isChecked, parameters); // ��������� ������ � ListView
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

        // ����� ��� �������� ����� �� ����� txt
        private void LoadJobs()
        {
            BackupJobsFile();
            if (File.Exists(JobsFilePath))
            {
                try
                {
                    string[] lines = File.ReadAllLines(JobsFilePath);
                    listViewJobs.Items.Clear(); // ������� ������ ����� ���������
                    foreach (var line in lines)
                    {
                        var parts = line.Split('~'); // ��������� ����� �� �����, ��������� �������� � ���������
                        if (parts.Length == 3 && bool.TryParse(parts[1], out bool isChecked))
                        {
                            var item = new ListViewItem(parts[0]) { Checked = isChecked }; // ������������� ��������� ��������
                            item.SubItems.Add(parts[2]); // ������ �������: ���������
                            listViewJobs.Items.Add(item);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading jobs from file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
        // ����� ����� ���������� ����� � ListView
        private void AddJobsToListView(string job, bool isChecked = true, string parameters = "")
        {
            var item = new ListViewItem(job)
            {
                Checked = isChecked // ������������� ��������� �� ��������� 
            };

            item.SubItems.Add(parameters); // ��������� ��������� ��� ������ ������� 
            listViewJobs.Items.Add(item); // ��������� ������� � ListView
        }

        private void ListViewFlacExecutables_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
                buttonRemoveEncoder.PerformClick();
        }
        private void ListViewAudioFiles_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
                buttonRemoveAudiofile.PerformClick();
        }
        private void ListViewJobs_KeyDown(object? sender, KeyEventArgs e)
        {
         //   if (e.KeyCode == Keys.Delete)
         //       buttonRemoveAudiofile.PerformClick();
        }

        private void InitializedataGridViewLog()
        {
            // ��������� DataGridView (�� �������)
            dataGridViewLog.Columns.Add("FileName", "File Name");
            dataGridViewLog.Columns.Add("InputFileSize", "Input File Size");
            dataGridViewLog.Columns.Add("OutputFileSize", "Output File Size");
            dataGridViewLog.Columns.Add("Compression", "Compression");
            dataGridViewLog.Columns.Add("Time", "Time");
            dataGridViewLog.Columns.Add("Speed", "Speed");
            dataGridViewLog.Columns.Add("Parameters", "Parameters");
            dataGridViewLog.Columns.Add("Executable", "Binary");
            dataGridViewLog.Columns.Add("Version", "Version");
            // ��������� ������������ ��� �������
            dataGridViewLog.Columns["InputFileSize"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewLog.Columns["OutputFileSize"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewLog.Columns["Compression"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewLog.Columns["Time"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewLog.Columns["Speed"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        }
        // FORM LOAD
        private void Form1_Load(object? sender, EventArgs e)
        {
            LoadSettings(); // �������� ��������
        }
        private void Form1_FormClosing(object? sender, FormClosingEventArgs e)
        {
            // ���������� �������� ����� ���������
            SaveSettings();
            // ��������� �������
            cpuUsageTimer.Stop();
            cpuUsageTimer.Dispose();
            if (checkBoxClearTempFolder.Checked)
            {
                // ������� ����� � ��� ����������, ���� ��� ����������
                if (Directory.Exists(tempFolderPath)) Directory.Delete(tempFolderPath, true);
            }
        }
        private void buttonAddEncoders_Click(object? sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Title = "Select Executable Files";
                openFileDialog.Filter = "Executable Files (*.exe)|*.exe|All Files (*.*)|*.*";
                openFileDialog.Multiselect = true;
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    foreach (var file in openFileDialog.FileNames)
                    {
                        AddExecutableFileToListView(file); // ���������� ����� �����
                    }
                }
            }
        }
        private void buttonRemoveEncoder_Click(object? sender, EventArgs e)
        {
            // ������� ���������� �������� �� listViewFlacExecutables
            for (int i = listViewFlacExecutables.Items.Count - 1; i >= 0; i--)
            {
                if (listViewFlacExecutables.Items[i].Selected) // ���������, ������� �� �������
                {
                    listViewFlacExecutables.Items.RemoveAt(i); // ������� �������
                }
            }
        }
        private void buttonClearEncoders_Click(object? sender, EventArgs e)
        {
            listViewFlacExecutables.Items.Clear();
        }
        private void buttonAddAudioFiles_Click(object? sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Title = "Select Audio Files";
                openFileDialog.Filter = "Audio Files (*.flac;*.wav)|*.flac;*.wav|All Files (*.*)|*.*";
                openFileDialog.Multiselect = true;
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    foreach (var file in openFileDialog.FileNames)
                    {
                        AddAudioFileToListView(file); // ���������� ����� �����
                    }
                }
            }
        }
        private void buttonRemoveAudiofile_Click(object? sender, EventArgs e)
        {
            // ������� ���������� �������� �� listViewAudioFiles
            for (int i = listViewAudioFiles.Items.Count - 1; i >= 0; i--)
            {
                if (listViewAudioFiles.Items[i].Selected) // ���������, ������� �� �������
                {
                    listViewAudioFiles.Items.RemoveAt(i); // ������� �������
                }
            }
        }
        private void buttonClearAudioFiles_Click(object? sender, EventArgs e)
        {
            listViewAudioFiles.Items.Clear();
        }
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
            textBoxThreads.Text = (physicalCores / 2).ToString(); // ������������� �������� ����
        }
        private void buttonSetMaxCores_Click(object? sender, EventArgs e)
        {
            textBoxThreads.Text = physicalCores.ToString(); // ������������� ������������ ���������� ����
        }
        private void buttonSetHalfThreads_Click(object? sender, EventArgs e)
        {
            textBoxThreads.Text = (threadCount / 2).ToString(); // ������������� �������� �������
        }
        private void buttonSetMaxThreads_Click(object? sender, EventArgs e)
        {
            textBoxThreads.Text = threadCount.ToString(); // ������������� ������������ ���������� �������
        }
        private void buttonClearCommandLineEncoder_Click(object? sender, EventArgs e)
        {
            textBoxCommandLineOptionsEncoder.Clear(); // ������� textCommandLineOptions
        }
        private void buttonClearCommandLineDecoder_Click(object? sender, EventArgs e)
        {
            textBoxCommandLineOptionsDecoder.Clear(); // ������� textCommandLineOptions
        }
        private void buttonepr8_Click(object? sender, EventArgs e)
        {
            // ���������, ���������� �� -epr8 � textBoxAdditionalArguments
            if (!textBoxCommandLineOptionsEncoder.Text.Contains("-epr8"))
            {
                // ���� ���, ��������� ���
                textBoxCommandLineOptionsEncoder.AppendText(" -epr8"); // ��������� � �������� ����� �������
            }
        }
        private void buttonAsubdividetukey5flattop_Click(object? sender, EventArgs e)
        {
            // ���������, ���������� �� -A "subdivide_tukey(5);flattop" � textBoxAdditionalArguments
            if (!textBoxCommandLineOptionsEncoder.Text.Contains("-A \"subdivide_tukey(5);flattop\""))
            {
                // ���� ���, ��������� ���
                textBoxCommandLineOptionsEncoder.AppendText(" -A \"subdivide_tukey(5);flattop\""); // ��������� � �������� ����� �������
            }
        }
        private void buttonNoPadding_Click(object? sender, EventArgs e)
        {
            // ���������, ���������� �� --no-padding � textBoxAdditionalArguments
            if (!textBoxCommandLineOptionsEncoder.Text.Contains("--no-padding"))
            {
                // ���� ���, ��������� ���
                textBoxCommandLineOptionsEncoder.AppendText(" --no-padding"); // ��������� � �������� ����� �������
            }
        }
        private void buttonNoSeektable_Click(object? sender, EventArgs e)
        {
            // ���������, ���������� �� --no-seektable � textBoxAdditionalArguments
            if (!textBoxCommandLineOptionsEncoder.Text.Contains("--no-seektable"))
            {
                // ���� ���, ��������� ���
                textBoxCommandLineOptionsEncoder.AppendText(" --no-seektable"); // ��������� � �������� ����� �������
            }
        }
        private async void buttonStartEncode_Click(object? sender, EventArgs e)
        {
            // ������������� ���� ���������
            _isEncodingStopped = false;
            // ������ ��������� ���������� ��� ��������� �����
            Directory.CreateDirectory(tempFolderPath);
            // �������� ���������� .exe �����
            var selectedExecutables = listViewFlacExecutables.CheckedItems
            .Cast<ListViewItem>()
            .Select(item => item.Tag.ToString()) // �������� ������ ���� �� Tag
            .ToList();
            // �������� ���������� ����������
            var selectedAudioFiles = listViewAudioFiles.CheckedItems
            .Cast<ListViewItem>()
            .Select(item => item.Tag.ToString()) // �������� ������ ���� �� Tag
            .ToList();
            // ���������, ���� �� ��������� ����������� ����� � ����������
            if (selectedExecutables.Count == 0 || selectedAudioFiles.Count == 0)
            {
                MessageBox.Show("Please select at least one executable and one audio file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            foreach (var executable in selectedExecutables)
            {
                foreach (var audioFile in selectedAudioFiles)
                {
                    if (_isEncodingStopped)
                    {
                        return; // �������, ���� ��������� �������
                    }
                    // �������� ���������� �� ����������
                    var (duration, _, _, _) = GetAudioInfo(audioFile);
                    long durationMs = Convert.ToInt64(duration);
                    // �������� �������� �� ��������� ����� � ��������� ���������...
                    string compressionLevel = textBoxCompressionLevel.Text;
                    string threads = textBoxThreads.Text;
                    string commandLine = textBoxCommandLineOptionsEncoder.Text;
                    // ��������� ��������� ��� �������
                    string outputFilePath = Path.Combine(tempFolderPath, "temp_encoded.flac"); // ��� ��������� �����
                    string arguments = $"\"{audioFile}\" -{compressionLevel} {commandLine}";
                    // ��������� �������� -j{threads} ������ ���� threads ������ 1
                    if (int.TryParse(threads, out int threadCount) && threadCount > 1)
                    {
                        arguments += $" -j{threads}";
                    }
                    // ��������� ��������� ���������
                    arguments += $" -f -o \"{outputFilePath}\"";
                    // ��������� ��������� (��� �������� � ��������� ������)
                    string parameters = $"-{compressionLevel} {commandLine}";
                    if (threadCount > 1)
                    {
                        parameters += $" -j{threads}";
                    }
                    // ��������� ������� � ���������� ����������
                    try
                    {
                        await Task.Run(() =>
                        {
                            using (_process = new Process()) // ��������� ������� � ���� _process
                            {
                                _process.StartInfo = new ProcessStartInfo
                                {
                                    FileName = executable,
                                    Arguments = arguments,
                                    UseShellExecute = false,
                                    CreateNoWindow = true,
                                };
                                // ��������� ������ �������
                                stopwatch.Reset();
                                stopwatch.Start();
                                if (!_isEncodingStopped) // ��������� �������� ����� ��������
                                {
                                    _process.Start();
                                }
                                // ������������� ��������� �������� �� �������, ���� ������� �������
                                _process.PriorityClass = checkBoxHighPriority.Checked
                                ? ProcessPriorityClass.High
                                : ProcessPriorityClass.Normal;
                                _process.WaitForExit(); // ��������� ���������� ��������
                                stopwatch.Stop();
                            }
                        });
                        // �������: ���������� � ��� ������ ���� ������� �� ��� ����������
                        if (!_isEncodingStopped)
                        {
                            // ������� CultureInfo ��� �������������� � ������� ��� ������������� ��������
                            NumberFormatInfo numberFormat = new CultureInfo("en-US").NumberFormat;
                            numberFormat.NumberGroupSeparator = ".";

                            FileInfo inputFileInfo = new FileInfo(audioFile);
                            long inputSize = inputFileInfo.Length; // ������ �������� �����
                            string inputSizeFormatted = inputSize.ToString("N0", numberFormat);

                            FileInfo outputFile = new FileInfo(outputFilePath);
                            if (outputFile.Exists)
                            {
                                long outputSize = outputFile.Length; // ������ ��������� �����
                                string outputSizeFormatted = outputSize.ToString("N0", numberFormat);
                                TimeSpan timeTaken = stopwatch.Elapsed;
                                // ���������� �������� ������
                                double compressionPercentage = ((double)outputSize / inputSize) * 100;
                                // ������������ ��������� �������� ����������� � ������������
                                double encodingSpeed = (double)durationMs / timeTaken.TotalMilliseconds;
                                // �������� ������ ��� ����� ��� �����������
                                string audioFileName = Path.GetFileName(audioFile);
                                // ��������� �������� ��� �����
                                string audioFileNameShort;
                                if (audioFileName.Length > 30) // ���� ��� ����� ������� 24 ��������
                                {
                                    string startName = audioFileName.Substring(0, 15);
                                    string endName = audioFileName.Substring(audioFileName.Length - 15);
                                    audioFileNameShort = $"{startName}...{endName}";
                                }
                                else
                                {
                                    // ���� ��� ����� 33 ������� ��� ������, ���������� ��� ������� � ��������� ������� �� 24
                                    audioFileNameShort = audioFileName + new string(' ', 33 - audioFileName.Length);
                                }
                                // �������� ���������� � ������ exe �����
                                var version = GetExecutableInfo(executable);
                                // ���������� ������ � ��� DataGridView
                                int rowIndex = dataGridViewLog.Rows.Add(
                                audioFileName,
                                inputSizeFormatted,
                                outputSizeFormatted,
                                $"{compressionPercentage:F3}%",
                                $"{timeTaken.TotalMilliseconds:F3}",
                                $"{encodingSpeed:F3}x",
                                parameters,
                                Path.GetFileName(executable),
                                version);
                                // ��������� ����� ������ � ����������� �� ��������� �������� ������
                                if (outputSize > inputSize)
                                {
                                    dataGridViewLog.Rows[rowIndex].Cells[2].Style.ForeColor = Color.Red; // �������� ������ ������
                                }
                                else if (outputSize < inputSize)
                                {
                                    dataGridViewLog.Rows[rowIndex].Cells[2].Style.ForeColor = Color.Green; // �������� ������ ������
                                }
                                // ������������ DataGridView ���� � ��������� ����������� ������
                                dataGridViewLog.FirstDisplayedScrollingRowIndex = dataGridViewLog.Rows.Count - 1;
                                // ������� ���������, ����� ������ ����� � ������ ������
                                dataGridViewLog.ClearSelection();
                                // ����������� � ����
                                File.AppendAllText("log.txt", $"{DateTime.Now}: {audioFileNameShort}\tInput size: {inputSize}\tOutput size: {outputSize} bytes\tCompression: {compressionPercentage:F3}%\tTime: {timeTaken.TotalMilliseconds:F3} ms\tEncoding Speed: {encodingSpeed:F3}x\tParameters: {parameters.Trim()}\tEncoded with: {Path.GetFileName(executable)}\tVersion: {version}{Environment.NewLine}");
                            }
                        }
                        else
                        {
                            MessageBox.Show("Output file was not created.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error starting encoding process: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }
        private async void buttonStartDecode_Click(object? sender, EventArgs e)
        {
            // ������������� ���� ���������
            _isEncodingStopped = false;
            // ������ ��������� ���������� ��� ��������� �����
            Directory.CreateDirectory(tempFolderPath);
            // �������� ���������� .exe �����
            var selectedExecutables = listViewFlacExecutables.CheckedItems
            .Cast<ListViewItem>()
            .Select(item => item.Tag.ToString()) // �������� ������ ���� �� Tag
            .ToList();
            // �������� ���������� ����������, �� ������ � ����������� .flac
            var selectedAudioFiles = listViewAudioFiles.CheckedItems
            .Cast<ListViewItem>()
            .Select(item => item.Tag.ToString()) // �������� ������ ���� �� Tag
            .Where(file => Path.GetExtension(file).Equals(".flac", StringComparison.OrdinalIgnoreCase)) // ������ .flac �����
            .ToList();
            // ���������, ���� �� ��������� ����������� ����� � ����������
            if (selectedExecutables.Count == 0 || selectedAudioFiles.Count == 0)
            {
                MessageBox.Show("Please select at least one executable and one FLAC audio file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            foreach (var executable in selectedExecutables)
            {
                foreach (var audioFile in selectedAudioFiles)
                {
                    if (_isEncodingStopped)
                    {
                        return; // �������, ���� ��������� �������
                    }
                    // �������� ���������� �� ����������
                    var (duration, _, _, _) = GetAudioInfo(audioFile);
                    long durationMs = Convert.ToInt64(duration);
                    // �������� �������� �� ��������� ����� � ��������� ���������...
                    string threads = textBoxThreads.Text;
                    string commandLine = textBoxCommandLineOptionsDecoder.Text;
                    // ��������� ��������� ��� �������
                    string outputFilePath = Path.Combine(tempFolderPath, "temp_decoded.wav"); // ��� ��������� �����
                    string arguments = $"\"{audioFile}\" -d {commandLine} -f -o \"{outputFilePath}\"";
                    // ��������� ��������� (��� �������� � ��������� ������)
                    string parameters = $"{commandLine}";
                    // ��������� ������� � ���������� ����������
                    try
                    {
                        await Task.Run(() =>
                        {
                            using (_process = new Process()) // ��������� ������� � ���� _process
                            {
                                _process.StartInfo = new ProcessStartInfo
                                {
                                    FileName = executable,
                                    Arguments = arguments,
                                    UseShellExecute = false,
                                    CreateNoWindow = true,
                                };
                                // ��������� ������ �������
                                stopwatch.Reset();
                                stopwatch.Start();
                                if (!_isEncodingStopped) // ��������� �������� ����� ��������
                                {
                                    _process.Start();
                                }
                                // ������������� ��������� �������� �� �������, ���� ������� �������
                                _process.PriorityClass = checkBoxHighPriority.Checked
                                ? ProcessPriorityClass.High
                                : ProcessPriorityClass.Normal;
                                _process.WaitForExit(); // ��������� ���������� ��������
                                stopwatch.Stop();
                            }
                        });
                        // �������: ���������� � ��� ������ ���� ������� �� ��� ����������
                        if (!_isEncodingStopped)
                        {
                            // ������� CultureInfo ��� �������������� � ������� ��� ������������� ��������
                            NumberFormatInfo numberFormat = new CultureInfo("en-US").NumberFormat;
                            numberFormat.NumberGroupSeparator = ".";

                            FileInfo inputFileInfo = new FileInfo(audioFile);
                            long inputSize = inputFileInfo.Length; // ������ �������� �����
                            string inputSizeFormatted = inputSize.ToString("N0", numberFormat);

                            FileInfo outputFile = new FileInfo(outputFilePath);
                            if (outputFile.Exists)
                            {
                                long outputSize = outputFile.Length; // ������ ��������� �����
                                string outputSizeFormatted = outputSize.ToString("N0", numberFormat);
                                TimeSpan timeTaken = stopwatch.Elapsed;
                                // ������������ ��������� �������� ������������� � ������������
                                double decodingSpeed = (double)durationMs / timeTaken.TotalMilliseconds;
                                // �������� ������ ��� ����� ��� �����������
                                string audioFileName = Path.GetFileName(audioFile);
                                // ��������� �������� ��� �����
                                string audioFileNameShort;
                                if (audioFileName.Length > 30) // ���� ��� ����� ������� 24 ��������
                                {
                                    string startName = audioFileName.Substring(0, 15);
                                    string endName = audioFileName.Substring(audioFileName.Length - 15);
                                    audioFileNameShort = $"{startName}...{endName}";
                                }
                                else
                                {
                                    // ���� ��� ����� 33 ������� ��� ������, ���������� ��� ������� � ��������� ������� �� 24
                                    audioFileNameShort = audioFileName + new string(' ', 33 - audioFileName.Length);
                                }
                                // �������� ���������� � ������ exe �����
                                var version = GetExecutableInfo(executable);
                                // ���������� ������ � ��� DataGridView
                                int rowIndex = dataGridViewLog.Rows.Add(
                                audioFileName,
                                inputSizeFormatted,
                                outputSizeFormatted,
                                "",
                                $"{timeTaken.TotalMilliseconds:F3}",
                                $"{decodingSpeed:F3}x",
                                parameters,
                                Path.GetFileName(executable),
                                version);
                                // ������������ DataGridView ���� � ��������� ����������� ������
                                dataGridViewLog.FirstDisplayedScrollingRowIndex = dataGridViewLog.Rows.Count - 1;
                                // ������� ���������, ����� ������ ����� � ������ ������
                                dataGridViewLog.ClearSelection();
                                // ����������� � ����
                                File.AppendAllText("log.txt", $"{DateTime.Now}: {audioFileNameShort}\tInput size: {inputSize}\tOutput size: {outputSize} bytes\t\tTime: {timeTaken.TotalMilliseconds:F3} ms\tDecoding Speed: {decodingSpeed:F3}x\tParameters: {parameters.Trim()}\tDecoded with: {Path.GetFileName(executable)}\tVersion: {version}{Environment.NewLine}");
                            }
                        }
                        else
                        {
                            MessageBox.Show("Output file was not created.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error starting decoding process: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }
        private void buttonImportJobList_Click(object? sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";
                openFileDialog.Title = "Open Job List";
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        string[] lines = File.ReadAllLines(openFileDialog.FileName); // ���������� ��������� ����
                    //    listViewJobs.Items.Clear(); // ������� ������ ����� ��������� �����

                        foreach (var line in lines)
                        {
                            var parts = line.Split('~'); // ��������� ������ �� �����
                            if (parts.Length == 3 && bool.TryParse(parts[1], out bool isChecked))
                            {
                                string jobName = parts[0];
                                string parameters = parts[2];
                                AddJobsToListView(jobName, isChecked, parameters); // ��������� ������ � ListView
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
            }
        }
        private void buttonExportJobList_Click(object? sender, EventArgs e)
        {
            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";
                saveFileDialog.Title = "Save Job List";
                string fileName = $"Settings_joblist {DateTime.Now:yyyy-MM-dd}.txt"; // ������ YYYY-MM-DD
                saveFileDialog.FileName = fileName; // ������������� ��������� ��� �����
                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        var jobList = listViewJobs.Items.Cast<ListViewItem>()
                            .Select(item => $"{item.Text}~{item.Checked}~{item.SubItems[1].Text}") // �������� �����, ��������� �������� � ���������
                            .ToArray(); // ��������� � ����� �������
                        File.WriteAllLines(saveFileDialog.FileName, jobList);
                    //    MessageBox.Show("Job list exported successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error exporting job list: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }
        private void buttonClearJobList_Click(object? sender, EventArgs e)
        {
            listViewJobs.Items.Clear(); // ������� listViewJobList
        }
        private void buttonOpenLogtxt_Click(object? sender, EventArgs e)
        {
            // ���� � ����� �����������
            string logFilePath = "log.txt";
            // ��������� ���������� �� ����
            if (File.Exists(logFilePath))
            {
                try
                {
                    // ��������� log.txt � ������� ������������ ���������� ���������
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = logFilePath,
                        UseShellExecute = true // ��� ������� ���� � ������� ���������������� ����������
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
            // ������� StringBuilder ��� ����� ������ �����
            StringBuilder logText = new StringBuilder();

            // �������� �� ������� � DataGridView � �������� �����
            foreach (DataGridViewRow row in dataGridViewLog.Rows)
            {
                // ������������, ��� �� ������ �������� ����� �� ���� ����� ������
                foreach (DataGridViewCell cell in row.Cells)
                {
                    logText.Append(cell.Value?.ToString() + "\t"); // ���������� ��������� ��� ���������� �����
                }
                logText.AppendLine(); // ������� �� ����� ������ ����� ������ ������ DataGridView
            }

            if (logText.Length > 0)
            {
                Clipboard.SetText(logText.ToString());
                MessageBox.Show("Log copied to clipboard!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
        private void buttonStop_Click(object sender, EventArgs e)
        {
            _isEncodingStopped = true; // ���� � ������� ��������� �����������
            if (_process != null)
            {
                try
                {
                    // ���������, ������� �� �������
                    if (!_process.HasExited)
                    {
                        _process.Kill(); // ��������� �������
                        MessageBox.Show("Encoding process has been stopped.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show("The encoding process has already exited.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                catch (Exception ex)
                {
                }
                finally
                {
                    _process.Dispose(); // ����������� �������
                    _process = null; // �������� ������ �� �������
                }
            }
            else
            {
            }
        }
        private void buttonSelectTempFolder_Click(object? sender, EventArgs e)
        {
            using (FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog())
            {
                folderBrowserDialog.Description = "Select temp folder";
                // ���� ���� ������� � ����������, ������������� ���
                if (Directory.Exists(tempFolderPath))
                {
                    folderBrowserDialog.SelectedPath = tempFolderPath;
                }
                if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
                {
                    // �������� ��������� ����
                    tempFolderPath = folderBrowserDialog.SelectedPath;
                    // ��������� ���� � ����������
                    SaveSettings(); // ��� ����� ����� ����� ��������, ����� ��������� ����
                }
            }
        }

        private void buttonAddJobToJobListEncoder_Click(object sender, EventArgs e)
        {
            // �������� �������� �� ��������� ����� � ��������� ���������
            string compressionLevel = textBoxCompressionLevel.Text;
            string threads = textBoxThreads.Text;
            string commandLine = textBoxCommandLineOptionsEncoder.Text;

            // ��������� ������ � �����������
            string parameters = $"-{compressionLevel} {commandLine}";

            // ��������� ���������� �������, ���� ��� ������ 1
            if (int.TryParse(threads, out int threadCount) && threadCount > 1)
            {
                parameters += $" -j{threads}"; // ��������� ���� -j{threads}
            }

            // ������� ����� ������� ������ ��� �����������
            var item = new ListViewItem("Encode") // ������ ������� - Encode
            {
                Checked = true // ������������� ������� � ��������� "���������"
            };
            item.SubItems.Add(parameters); // ������ ������� - parameters

            // ��������� ������� � listViewJobList
            listViewJobs.Items.Add(item);
        }
        private void buttonAddJobToJobListDecoder_Click(object sender, EventArgs e)
        {
            // �������� �������� �� ��������� ����� � ��������� ���������
            string commandLine = textBoxCommandLineOptionsDecoder.Text;

            // ��������� ������ � �����������
            string parameters = commandLine; // ��������� ��� �������������

            // ������� ����� ������� ������ ��� �������������
            var item = new ListViewItem("Decode") // ������ ������� - Decode
            {
                Checked = true // ������������� ������� � ��������� "���������"
            };
            item.SubItems.Add(parameters); // ������ ������� - parameters

            // ��������� ������� � listViewJobList
            listViewJobs.Items.Add(item);
        }

        private void buttonCopyJobs_Click(object sender, EventArgs e)
        {
            StringBuilder jobsText = new StringBuilder();

            // ���������, ���� �� ���������� ��������
            if (listViewJobs.SelectedItems.Count > 0)
            {
                // �������� ������ ���������� ������
                foreach (ListViewItem item in listViewJobs.SelectedItems)
                {
                    jobsText.AppendLine($"{item.Text}~{item.Checked}~{item.SubItems[1].Text}");
                }
            }
            else
            {
                // ���� ������ �� ��������, �������� ��� ������
                foreach (ListViewItem item in listViewJobs.Items)
                {
                    jobsText.AppendLine($"{item.Text}~{item.Checked}~{item.SubItems[1].Text}");
                }
            }

            // �������� ����� � ����� ������
            if (jobsText.Length > 0)
            {
                Clipboard.SetText(jobsText.ToString());
            //    MessageBox.Show("Jobs copied to clipboard.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("No jobs to copy.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        private void buttonPasteJobs_Click(object sender, EventArgs e)
        {
            try
            {
                // �������� ����� �� ������ ������
                string clipboardText = Clipboard.GetText();

                // ���������, ���� ����� �� ������
                if (!string.IsNullOrEmpty(clipboardText))
                {
                    string[] lines = clipboardText.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries); // ��������� �� ������

                    foreach (var line in lines)
                    {
                        var parts = line.Split('~'); // ��������� ������ �� �����
                        if (parts.Length == 3 && bool.TryParse(parts[1], out bool isChecked))
                        {
                            string jobName = parts[0];
                            string parameters = parts[2];
                            AddJobsToListView(jobName, isChecked, parameters); // ��������� ������ � ListView
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

    }
}