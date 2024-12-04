using System;
using System.Management;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Text;
using MediaInfoLib;
namespace FLAC_Benchmark_H
{
    public partial class Form1 : Form
    {
        private int physicalCores;
        private int threadCount;
        private Process _process; // ���� ��� �������� �������� ��������
        private const string SettingsFilePath = "Settings_general.txt"; // ���� � ����� ��������
        private const string JobsFilePath = "Settings_joblist.txt"; // ���� � ����� jobs
        private const string executablesFilePath = "Settings_flac_executables.txt"; // ���� � ����� ��� ���������� ����������� ������
        private const string audioFilesFilePath = "Settings_audio_files.txt"; // ���� � ����� ��� ���������� �����������
        private Stopwatch stopwatch;
        private PerformanceCounter cpuCounter;
        private System.Windows.Forms.Timer cpuUsageTimer; // ��������� ����, ��� ��� Timer �� System.Windows.Forms
        private bool _isEncodingStopped = false;
        private string tempFolderPath; // ���� ��� �������� ���� � ��������� �����


        public Form1()
        {
            InitializeComponent();
            InitializeDragAndDrop(); // ������������� drag-and-drop
            this.FormClosing += Form1_FormClosing; // ����������� ����������� ������� �������� �����
            this.listViewFlacExecutables.KeyDown += ListViewFlacExecutables_KeyDown;
            this.listViewAudioFiles.KeyDown += ListViewAudioFiles_KeyDown;
            LoadCPUInfo(); // ��������� ���������� � ����������
            this.KeyPreview = true;
            stopwatch = new Stopwatch(); // ������������� ������� Stopwatch
            cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            cpuUsageTimer = new System.Windows.Forms.Timer(); // ���� ��������� System.Windows.Forms.Timer
            cpuUsageTimer.Interval = 250; // ������ 250 ��
            cpuUsageTimer.Tick += (sender, e) => UpdateCpuUsage();
            cpuUsageTimer.Start();
            InitializedataGridViewLog();
            tempFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp"); // ������������� ���� � ��������� �����


        }
        // ����� ��� �������� ���������� � ����������
        private void LoadCPUInfo()
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
                        physicalCores += int.Parse(obj["NumberOfCores"].ToString());
                        threadCount += int.Parse(obj["ThreadCount"].ToString());
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
            }
            catch (Exception ex)
            {
                // ���������� ������ � labelCPUinfo
                labelCPUinfo.Text = "Error loading CPU info: " + ex.Message;
            }
        }
        private void UpdateCpuUsage()
        {
            float cpuUsage = cpuCounter.NextValue();
            labelCPUinfo.Text = $"Your system has:\nCores: {physicalCores}, Threads: {threadCount}\nCPU Usage: {cpuUsage:F2}%";
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
            LoadJobsQueue(); // ��������� ���������� jobs.txt ����� �������� ������ ��������
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
            $"TempFolderPath={tempFolderPath}" // ��������� ���� � ��������� �����
        };
                File.WriteAllLines(SettingsFilePath, settings);
                SaveExecutables(); // ���������� ����������� ������
                SaveAudioFiles(); // ���������� �����������
                SaveJobsQueue(); // ��������� ���������� textBoxJobList
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving settings: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // �������� ����������� ������
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
                            var item = new ListViewItem(Path.GetFileName(parts[0]));
                            item.Tag = parts[0]; // ������ ���� �������� � Tag
                            item.Checked = bool.Parse(parts[1]);
                            listViewFlacExecutables.Items.Add(item);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading executables: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
        // ���������� ����������� ������
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
        // ����� ��������� ��� �������� ��������� CheckedListBox � ������� ������
        private void LoadJobsQueue()
        {
            // ������� ����� jobs.txt ����� ��� ���������
            BackupJobsFile();
            if (File.Exists(JobsFilePath))
            {
                try
                {
                    string content = File.ReadAllText(JobsFilePath);
                    textBoxJobList.Text = content;
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
        private void SaveJobsQueue()
        {
            try
            {
                File.WriteAllText(JobsFilePath, textBoxJobList.Text);
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
            // ��������� �������������� ������ � TextBox ��� ������� �����
            textBoxJobList.AllowDrop = true;
            textBoxJobList.DragEnter += TextBoxJobList_DragEnter;
            textBoxJobList.DragDrop += TextBoxJobList_DragDrop;
        }
        // ���������� DragEnter ��� ListViewFlacExecutables
        private void ListViewFlacExecutables_DragEnter(object sender, DragEventArgs e)
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
        private void ListViewFlacExecutables_DragDrop(object sender, DragEventArgs e)
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
                    var item = new ListViewItem(Path.GetFileName(file))
                    {
                        Tag = file,
                        Checked = true // ������������� ��������� �� ���������
                    };
                    listViewFlacExecutables.Items.Add(item);
                }
            }
        }

        // ����������� ����� ��� ���������� ����������� ������ � ListView
        private void AddExecutableFiles(string directory)
        {
            try
            {
                // ������� ��� .exe ����� � ������� ����������
                var exeFiles = Directory.GetFiles(directory, "*.exe", SearchOption.AllDirectories);
                foreach (var exeFile in exeFiles)
                {
                    var item = new ListViewItem(Path.GetFileName(exeFile))
                    {
                        Tag = exeFile,
                        Checked = true // ������������� ��������� �� ���������
                    };
                    listViewFlacExecutables.Items.Add(item);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error accessing directory: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ���������� DragEnter ��� ListViewAudioFiles
        private void ListViewAudioFiles_DragEnter(object sender, DragEventArgs e)
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
        private void ListViewAudioFiles_DragDrop(object sender, DragEventArgs e)
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

        // ����������� ����� ��� ���������� ����������� �� ����������
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
                            string audioFilePath = parts[0];
                            bool isChecked = bool.Parse(parts[1]);
                            AddAudioFileToListView(audioFilePath, isChecked); // ���������� ����� �����
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading audio files: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
        // ����� ���������� ����������� � ListView
        private void AddAudioFileToListView(string audioFile, bool isChecked = true)
        {
            var item = new ListViewItem(Path.GetFileName(audioFile))
            {
                Tag = audioFile,
                Checked = isChecked // ������������� ��������� �� ���������
            };

            var (duration, bitDepth, samplingRate, fileSize) = GetAudioInfo(audioFile); // �������� ������������, �����������, ������� ������������� � ������ ����������

            item.SubItems.Add(Convert.ToInt64(duration).ToString("N0") + " ms"); // ��������� ������������ � ����������
            item.SubItems.Add(bitDepth + " bit"); // ��������� ����������� � ����������
            item.SubItems.Add(samplingRate); // ��������� ������� ������������� � ����������
            item.SubItems.Add(Convert.ToInt64(fileSize).ToString("N0") + " bytes"); // ��������� ������ � ���������� � ���������������


            listViewAudioFiles.Items.Add(item); // ��������� ������� � ListView
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

        // ���������� DragEnter ��� TextBoxJobList
        private void TextBoxJobList_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                bool hasTxtFiles = files.Any(file => Path.GetExtension(file).Equals(".txt", StringComparison.OrdinalIgnoreCase));
                e.Effect = hasTxtFiles ? DragDropEffects.Copy : DragDropEffects.None;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }
        // ���������� DragDrop ��� TextBoxJobList
        private void TextBoxJobList_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            textBoxJobList.Clear(); // ������� textBox ����� �����������
            foreach (var file in files)
            {
                if (Path.GetExtension(file).Equals(".txt", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        string content = File.ReadAllText(file);
                        textBoxJobList.AppendText(content + Environment.NewLine); // ��������� ���������� �����
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error reading file: {ex.Message}");
                    }
                }
            }
        }
        private void ListViewFlacExecutables_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
                buttonRemoveEncoder.PerformClick();
        }
        private void ListViewAudioFiles_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
                buttonRemoveAudiofile.PerformClick();
        }
        private void InitializedataGridViewLog()
        {
            // ��������� DataGridView (�� �������)
            dataGridViewLog.Columns.Add("FileName", "File Name");
            dataGridViewLog.Columns.Add("InputFileSize", "Input File Size");
            dataGridViewLog.Columns.Add("OutputFileSize", "Output File Size");
            dataGridViewLog.Columns.Add("Compression", "Compression");
            dataGridViewLog.Columns.Add("TimeTaken", "Time Taken");
            dataGridViewLog.Columns.Add("Executable", "Binary");
            dataGridViewLog.Columns.Add("Parameters", "Parameters");
            // ��������� ������������ ��� �������
            dataGridViewLog.Columns["InputFileSize"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewLog.Columns["OutputFileSize"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewLog.Columns["TimeTaken"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewLog.Columns["Compression"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;





            listViewFlacExecutables.Columns.Add("FileName", "File Name");
            listViewFlacExecutables.Columns.Add("Version", "Version");
            listViewFlacExecutables.Columns.Add("Size", "Size");

        }
        // FORM LOAD
        private void Form1_Load(object sender, EventArgs e)
        {
            LoadSettings(); // �������� ��������

        }
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            SaveSettings(); // ���������� ��������
        }
        private void groupBoxEncoders_Enter(object sender, EventArgs e)
        {
        }
        private void listViewFlacExecutables_SelectedIndexChanged(object sender, EventArgs e)
        {
        }
        private void buttonAddEncoders_Click(object sender, EventArgs e)
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
                        var item = new ListViewItem(Path.GetFileName(file))
                        {
                            Tag = file,
                            Checked = true // ������������� ���������
                        };
                        listViewFlacExecutables.Items.Add(item);
                    }
                }
            }
        }
        private void buttonRemoveEncoder_Click(object sender, EventArgs e)
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
        private void buttonClearEncoders_Click(object sender, EventArgs e)
        {
            listViewFlacExecutables.Items.Clear();
        }
        private void groupBoxAudioFiles_Enter(object sender, EventArgs e)
        {
        }
        private void buttonAddAudioFiles_Click(object sender, EventArgs e)
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
        private void buttonRemoveAudiofile_Click(object sender, EventArgs e)
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
        private void buttonClearAudioFiles_Click(object sender, EventArgs e)
        {
            listViewAudioFiles.Items.Clear();
        }
        private void groupBoxEncoderSettings_Enter(object sender, EventArgs e)
        {
        }
        private void labelCompressionLevel_Click(object sender, EventArgs e)
        {
        }
        private void textBoxCompressionLevel_TextChanged(object sender, EventArgs e)
        {
        }
        private void labelSetCompression_Click(object sender, EventArgs e)
        {
        }
        private void button5CompressionLevel_Click(object sender, EventArgs e)
        {
            textBoxCompressionLevel.Text = "5";
        }
        private void buttonMaxCompressionLevel_Click(object sender, EventArgs e)
        {
            textBoxCompressionLevel.Text = "8";
        }
        private void labelThreads_Click(object sender, EventArgs e)
        {
        }
        private void textBoxThreads_TextChanged(object sender, EventArgs e)
        {
        }
        private void labelSetCores_Click(object sender, EventArgs e)
        {
        }
        private void buttonHalfCores_Click(object sender, EventArgs e)
        {
            textBoxThreads.Text = (physicalCores / 2).ToString(); // ������������� �������� ����
        }
        private void buttonSetMaxCores_Click(object sender, EventArgs e)
        {
            textBoxThreads.Text = physicalCores.ToString(); // ������������� ������������ ���������� ����
        }
        private void labelSetThreads_Click(object sender, EventArgs e)
        {
        }
        private void buttonSetHalfThreads_Click(object sender, EventArgs e)
        {
            textBoxThreads.Text = (threadCount / 2).ToString(); // ������������� �������� �������
        }
        private void buttonSetMaxThreads_Click(object sender, EventArgs e)
        {
            textBoxThreads.Text = threadCount.ToString(); // ������������� ������������ ���������� �������
        }
        private void checkBoxHighPriority_CheckedChanged(object sender, EventArgs e)
        {
        }
        private void labelCommandLine_Click(object sender, EventArgs e)
        {
        }
        private void textBoxCommandLineOptions_TextChanged(object sender, EventArgs e)
        {
        }
        private void buttonClearCommandLine_Click(object sender, EventArgs e)
        {
            textBoxCommandLineOptionsEncoder.Clear(); // ������� textCommandLineOptions
        }
        private void buttonepr8_Click(object sender, EventArgs e)
        {
            // ���������, ���������� �� -epr8 � textBoxAdditionalArguments
            if (!textBoxCommandLineOptionsEncoder.Text.Contains("-epr8"))
            {
                // ���� ���, ��������� ���
                textBoxCommandLineOptionsEncoder.AppendText(" -epr8"); // ��������� � �������� ����� �������
            }
        }
        private void buttonAsubdividetukey5flattop_Click(object sender, EventArgs e)
        {
            // ���������, ���������� �� -A "subdivide_tukey(5);flattop" � textBoxAdditionalArguments
            if (!textBoxCommandLineOptionsEncoder.Text.Contains("-A \"subdivide_tukey(5);flattop\""))
            {
                // ���� ���, ��������� ���
                textBoxCommandLineOptionsEncoder.AppendText(" -A \"subdivide_tukey(5);flattop\""); // ��������� � �������� ����� �������
            }
        }
        private void buttonNoPadding_Click(object sender, EventArgs e)
        {
            // ���������, ���������� �� --no-padding � textBoxAdditionalArguments
            if (!textBoxCommandLineOptionsEncoder.Text.Contains("--no-padding"))
            {
                // ���� ���, ��������� ���
                textBoxCommandLineOptionsEncoder.AppendText(" --no-padding"); // ��������� � �������� ����� �������
            }
        }
        private void buttonNoSeektable_Click(object sender, EventArgs e)
        {
            // ���������, ���������� �� --no-seektable � textBoxAdditionalArguments
            if (!textBoxCommandLineOptionsEncoder.Text.Contains("--no-seektable"))
            {
                // ���� ���, ��������� ���
                textBoxCommandLineOptionsEncoder.AppendText(" --no-seektable"); // ��������� � �������� ����� �������
            }
        }
        private async void buttonStartEncode_Click(object sender, EventArgs e)
        {
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
                        return; // ������� �� ������
                    }

                    // �������� �������� �� ��������� �����
                    string compressionLevel = textBoxCompressionLevel.Text;
                    string threads = textBoxThreads.Text;
                    string commandLine = textBoxCommandLineOptionsEncoder.Text;

                    // ��������� ��������� ��� �������
                    string outputFilePath = Path.Combine(tempFolderPath, "temp_encoded.flac"); // ��� ��������� �����
                                                                                               // ��������� ������� ���������
                    string arguments = $"\"{audioFile}\" -{compressionLevel} {commandLine}";

                    // ��������� �������� -j{threads} ������ ���� threads ������ 1
                    if (int.TryParse(threads, out int threadCount) && threadCount > 1)
                    {
                        arguments += $" -j{threads}"; // ��������� -j{threads}
                    }
                    arguments += $" -f -o \"{outputFilePath}\""; // ��������� ��������� ���������

                    // ��������� ��������� (��� �������� � ��������� ������)
                    string parameters = $"-{compressionLevel} {commandLine}";
                    if (threadCount > 1)
                    {
                        parameters += $" -j{threads}";
                    }
                    // ��������� ������� � ���������� ����������
                    try
                    {
                        FileInfo inputFileInfo = new FileInfo(audioFile);
                        long inputSize = inputFileInfo.Length; // ������ �������� �����
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
                        // ����� ���������� �������� ��������� ������ ��������� �����
                        FileInfo outputFile = new FileInfo(outputFilePath);
                        if (outputFile.Exists)
                        {
                            long outputSize = outputFile.Length; // ������ ��������� �����
                            TimeSpan timeTaken = stopwatch.Elapsed;

                            // ���������� �������� ������
                            double compressionPercentage = ((double)outputSize / inputSize) * 100;

                            // �������� ������ ��� ����� ��� �����������
                            string audioFileName = Path.GetFileName(audioFile);

                            // �������� ������ � ��������� ����� �����
                            string startName = audioFileName.Length > 22 ? audioFileName.Substring(0, 22) : audioFileName;
                            string endName = audioFileName.Length > 22 ? audioFileName.Substring(audioFileName.Length - 22) : "";

                            // ���������� ������ � ���������
                            string logFileName = startName + (string.IsNullOrEmpty(endName) ? "" : "...") + endName;

                            // �������: ���������� � ��� ������ ���� ������� �� ��� ����������
                            if (!_isEncodingStopped)
                            {
                                // ��������� ������ � ���
                                var rowIndex = dataGridViewLog.Rows.Add(audioFileName, $"{inputSize:n0}", $"{outputSize:n0}", $"{compressionPercentage:F3}%", $"{timeTaken.TotalMilliseconds:F3}", Path.GetFileName(executable), parameters);

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

                                // ����������� � ����
                                File.AppendAllText("log.txt", $"{DateTime.Now}: {logFileName}\tEncoded with: {Path.GetFileName(executable)}\tResulting FLAC size: {outputSize} bytes\tCompression: {compressionPercentage:F3}%\tTotal encoding time: {timeTaken.TotalMilliseconds:F3} ms\tParameters: {parameters.Trim()}{Environment.NewLine}");
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
        private async void buttonStartDecode_Click(object sender, EventArgs e)
        {
            // ������������� ���� ���������
            _isEncodingStopped = false;
            // �������� ��������� ���������� ��� �������� ������, ���� �����
            Directory.CreateDirectory(tempFolderPath);
            // �������� ���������� .exe �����
            var selectedExecutables = listViewFlacExecutables.CheckedItems
            .Cast<ListViewItem>()
            .Select(item => item.Tag.ToString()) // �������� ������ ���� �� Tag
            .ToList();
            // �������� ���������� ����������, �� ������ � ����������� .flac
            var selectedAudioFiles = listViewAudioFiles.CheckedItems
            .Cast<ListViewItem>()
            .Select(item => item.Tag.ToString())
            .Where(file => Path.GetExtension(file).Equals(".flac", StringComparison.OrdinalIgnoreCase)) // ������ .wav �����
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
                        return; // ������� �� ������
                    }
                    // �������� �������� �� ��������� �����
                    string threads = textBoxThreads.Text;
                    string commandLine = textBoxCommandLineOptionsDecoder.Text;
                    // ��������� ��������� ��� �������
                    string outputFileName = "temp_decoded.wav"; // ��� ��������� �����
                    string outputFilePath = Path.Combine(tempFolderPath, outputFileName);
                    string arguments = $"\"{audioFile}\" -d {commandLine} -f -o \"{outputFilePath}\"";
                    try
                    {
                        await Task.Run(() =>
                        {
                            using (_process = new Process())
                            {
                                _process.StartInfo = new ProcessStartInfo
                                {
                                    FileName = executable,
                                    Arguments = arguments,
                                    UseShellExecute = false,
                                    CreateNoWindow = true,
                                };
                                stopwatch.Reset();
                                stopwatch.Start();
                                if (!_isEncodingStopped)
                                {
                                    _process.Start();
                                }
                                // ������������� ��������� ��������
                                _process.PriorityClass = checkBoxHighPriority.Checked
                                ? ProcessPriorityClass.High
                                : ProcessPriorityClass.Normal;
                                _process.WaitForExit(); // ���������� ���������� ��������
                                stopwatch.Stop();
                            }
                        });
                        // ����� ���������� �������� ��������� �������� �����
                        FileInfo outputFile = new FileInfo(outputFilePath);
                        if (outputFile.Exists)
                        {
                            long fileSize = outputFile.Length;
                            TimeSpan timeTaken = stopwatch.Elapsed;
                            // �������� ������ ��� ����� ��� �����������
                            string audioFileName = Path.GetFileName(audioFile);
                            if (!_isEncodingStopped)
                            {
                                // ���������� ���������� � ���
                                dataGridViewLog.Rows.Add(audioFileName, fileSize, "", $"{timeTaken.TotalMilliseconds:F3}", Path.GetFileName(executable));
                                File.AppendAllText("log.txt", $"{audioFileName}\tdecoded with {Path.GetFileName(executable)}\tResulting file size: {fileSize} bytes\tTotal decoding time: {timeTaken.TotalMilliseconds:F3} ms\n");
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
        private void labelFlacUsedVersion_Click(object sender, EventArgs e)
        {
        }
        private void progressBar_Click(object sender, EventArgs e)
        {
        }
        private void groupBoxJobSettings_Enter(object sender, EventArgs e)
        {
        }
        private void radioButtonEncode_CheckedChanged(object sender, EventArgs e)
        {
        }
        private void radioButtonDecode_CheckedChanged(object sender, EventArgs e)
        {
        }
        private void buttonAddJobToJobList_Click(object sender, EventArgs e)
        {
        }
        private void groupBoxJobList_Enter(object sender, EventArgs e)
        {
        }
        private void textBoxJobList_TextChanged(object sender, EventArgs e)
        {
        }
        private void buttonStartJobList_Click(object sender, EventArgs e)
        {
        }
        private void buttonImportJobList_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";
                openFileDialog.Title = "Open Job List";
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        string content = File.ReadAllText(openFileDialog.FileName);
                        textBoxJobList.Text = content;
                        MessageBox.Show("Job list imported successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error importing job list: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }
        private void buttonExportJobList_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";
                saveFileDialog.Title = "Save Job List";
                string fileName = $"Settings_joblist {DateTime.Now:yyyy-MM-dd}.txt"; // ������ YYYYMMDD
                saveFileDialog.FileName = fileName; // ������������� ��������� ��� �����
                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        File.WriteAllText(saveFileDialog.FileName, textBoxJobList.Text);
                        //   MessageBox.Show("Job list exported successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error exporting job list: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }
        private void buttonClearJobList_Click(object sender, EventArgs e)
        {
            textBoxJobList.Clear(); // ������� textBoxJobList
        }
        private void groupLog_Enter(object sender, EventArgs e)
        {
        }
        private void textBoxLog_TextChanged(object sender, EventArgs e)
        {
        }
        private void buttonOpenLogtxt_Click(object sender, EventArgs e)
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
        private void buttonCopyLog_Click(object sender, EventArgs e)
        {
            // �������� ����� �� textBoxLog � ����� ������
            if (!string.IsNullOrWhiteSpace(textBoxLog.Text))
            {
                Clipboard.SetText(textBoxLog.Text);
                //MessageBox.Show("Log copied to clipboard!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("There is no log to copy.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        private void buttonClearLog_Click(object sender, EventArgs e)
        {
            dataGridViewLog.Rows.Clear();
        }
        private void buttonStop_Click(object sender, EventArgs e)
        {
            // ������������� ����, ��� ����������� �����������
            _isEncodingStopped = true;
            // ���� ������� �������, ��� ����� ����������
            if (_process != null && !_process.HasExited)
            {
                try
                {
                    _process.Kill(); // ��������� �������
                    _process.Dispose(); // ����������� �������
                    _process = null; // �������� �������
                    MessageBox.Show("Encoding process has been stopped.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error stopping the process: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show("No encoding process is running.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        private void checkBoxClearTempFolder_CheckedChanged(object sender, EventArgs e)
        {
        }

        private void buttonSelectTempFolder_Click(object sender, EventArgs e)
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

    }
}