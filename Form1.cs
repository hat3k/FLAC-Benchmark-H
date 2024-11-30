using System;
using System.Management;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Text;
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
                    labelCPUinfo.Text = $"Your system has: Cores: {physicalCores}, Threads: {threadCount}";
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
            labelCPUinfo.Text = $"Your system has: Cores: {physicalCores}, Threads: {threadCount} CPU Usage: {cpuUsage:F2}%";
        }
        // ����� ��� �������� ��������
        private void LoadSettings()
        {
            if (File.Exists(SettingsFilePath))
            {
                try
                {
                    string[] lines = File.ReadAllLines(SettingsFilePath);
                    foreach (var line in lines)
                    {
                        var parts = line.Split('='); // ��������� ������ �� ���� � ��������
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
                                    textBoxCommandLineOptions.Text = value;
                                    break;
                                case "HighPriority":
                                    checkBoxHighPriority.Checked = bool.Parse(value);
                                    break;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading settings: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
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
            $"CommandLineOptions={textBoxCommandLineOptions.Text}",
            $"HighPriority={checkBoxHighPriority.Checked}"
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

        // ����������� ��������� ��� �������� � ���������� �����������
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
                            var item = new ListViewItem(Path.GetFileName(parts[0]));
                            item.Tag = parts[0]; // ������ ���� �������� � Tag
                            item.Checked = bool.Parse(parts[1]);
                            listViewAudioFiles.Items.Add(item);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading audio files: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
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
                bool hasExeFiles = files.Any(file => Path.GetExtension(file).Equals(".exe", StringComparison.OrdinalIgnoreCase));
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
                if (Path.GetExtension(file).Equals(".exe", StringComparison.OrdinalIgnoreCase))
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
        // ���������� DragEnter ��� ListViewAudioFiles
        private void ListViewAudioFiles_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                bool hasAudioFiles = files.Any(file =>
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
                if (Path.GetExtension(file).Equals(".wav", StringComparison.OrdinalIgnoreCase) ||
                    Path.GetExtension(file).Equals(".flac", StringComparison.OrdinalIgnoreCase))
                {
                    var item = new ListViewItem(Path.GetFileName(file))
                    {
                        Tag = file,
                        Checked = true // ������������� ��������� �� ���������
                    };
                    listViewAudioFiles.Items.Add(item);
                }
            }
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
        private void listViewAudioFiles_SelectedIndexChanged(object sender, EventArgs e)
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
                        var item = new ListViewItem(Path.GetFileName(file))
                        {
                            Tag = file,
                            Checked = true // ������������� ���������
                        };
                        listViewAudioFiles.Items.Add(item);
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
        private void labelCPUinfo_Click(object sender, EventArgs e)
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
            textBoxCommandLineOptions.Clear(); // ������� textCommandLineOptions
        }
        private void buttonepr8_Click(object sender, EventArgs e)
        {
            // ���������, ���������� �� -epr8 � textBoxAdditionalArguments
            if (!textBoxCommandLineOptions.Text.Contains("-epr8"))
            {
                // ���� ���, ��������� ���
                textBoxCommandLineOptions.AppendText(" -epr8"); // ��������� � �������� ����� �������
            }
        }
        private void buttonAsubdividetukey5flattop_Click(object sender, EventArgs e)
        {
            // ���������, ���������� �� -A "subdivide_tukey(5);flattop" � textBoxAdditionalArguments
            if (!textBoxCommandLineOptions.Text.Contains("-A \"subdivide_tukey(5);flattop\""))
            {
                // ���� ���, ��������� ���
                textBoxCommandLineOptions.AppendText(" -A \"subdivide_tukey(5);flattop\""); // ��������� � �������� ����� �������
            }
        }
        private void buttonNoPadding_Click(object sender, EventArgs e)
        {
            // ���������, ���������� �� --no-padding � textBoxAdditionalArguments
            if (!textBoxCommandLineOptions.Text.Contains("--no-padding"))
            {
                // ���� ���, ��������� ���
                textBoxCommandLineOptions.AppendText(" --no-padding"); // ��������� � �������� ����� �������
            }
        }
        private void buttonNoSeektable_Click(object sender, EventArgs e)
        {
            // ���������, ���������� �� --no-seektable � textBoxAdditionalArguments
            if (!textBoxCommandLineOptions.Text.Contains("--no-seektable"))
            {
                // ���� ���, ��������� ���
                textBoxCommandLineOptions.AppendText(" --no-seektable"); // ��������� � �������� ����� �������
            }
        }
        private async void buttonStartEncode_Click(object sender, EventArgs e)
        {
            // ������ ��������� ���������� ��� ��������� �����
            string tempFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp");
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
                    // �������� �������� �� ��������� �����
                    string compressionLevel = textBoxCompressionLevel.Text;
                    string threads = textBoxThreads.Text;
                    string commandLine = textBoxCommandLineOptions.Text;

                    // ��������� ��������� ��� �������
                    string outputFileName = "temp_encoded.flac"; // ��� ��������� �����
                    string outputFilePath = Path.Combine(tempFolderPath, outputFileName);
                    string arguments = $"\"{audioFile}\" -{compressionLevel} {commandLine} -j{threads} -f -o \"{outputFilePath}\"";

                    // ��������� ������� � ���������� ����������
                    try
                    {
                        await Task.Run(() =>
                        {
                            using (var process = new Process())
                            {
                                process.StartInfo = new ProcessStartInfo
                                {
                                    FileName = executable,
                                    Arguments = arguments,
                                    UseShellExecute = false,
                                    CreateNoWindow = true,
                                };

                                // ��������� ������ �������
                                stopwatch.Reset();  // �������� ���������� ����������
                                stopwatch.Start(); // ��������� ������ �������
                                process.Start();

                                // ������������� ��������� �������� �� �������, ���� ������� �������
                                if (checkBoxHighPriority.Checked)
                                {
                                    process.PriorityClass = ProcessPriorityClass.High;
                                }
                                else
                                {
                                    process.PriorityClass = ProcessPriorityClass.Normal; // ������������� ���������� ���������
                                }

                                process.WaitForExit(); // ��������� ���������� ��������
                                stopwatch.Stop(); // ������������� ������ �������
                            }
                        });

                        // ����� ���������� �������� ��������� ������ ��������� �����
                        FileInfo outputFile = new FileInfo(outputFilePath);

                        if (outputFile.Exists)
                        {
                            long fileSize = outputFile.Length; // ������ ����� � ������
                            string fileSizeFormatted = $"{fileSize} bytes"; // �������������� � ��
                            TimeSpan timeTaken = stopwatch.Elapsed;

                            // �������� ������ ��� ����� ��� �����������
                            string audioFileName = Path.GetFileName(audioFile);

                            // ���������� ���������� � textBoxLog
                            textBoxLog.AppendText($"{audioFileName}\t{fileSizeFormatted}\t{timeTaken.TotalMilliseconds:F3} ms\t{Path.GetFileName(executable)}" + Environment.NewLine);

                            // ����� ���������� � log.txt
                            File.AppendAllText("log.txt", $"{audioFileName}\tencoded with {Path.GetFileName(executable)}\tResulting FLAC size: {fileSizeFormatted}\tTotal encoding time: {timeTaken.TotalMilliseconds:F3} ms\n");
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

            // MessageBox.Show("Encoding completed!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void buttonStartDecode_Click(object sender, EventArgs e)
        {
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
            textBoxLog.Clear(); // ������� textBoxLog
        }

        private void buttonStop_Click(object sender, EventArgs e)
        {

        }
    }
}