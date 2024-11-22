using NAudio.Wave; // ���������� ��� ������ � ������������
using System.Diagnostics; // ���������� ��� ������ � ����������
using System.Management; // ���������� ��� ��������� ���������� � ����������
using System.Text; // ���������� ��� ������ � StringBuilder


namespace FLAC_Benchmark_H
{
    public partial class Form1 : Form
    {
        private const string LogFileName = "log.txt"; // ��� ���-�����, � ������� ����� ������������ ����� ����������
        private const string SettingsFileName = "settings.txt"; // ��� ����� � �����������
        private Process _process; // ���� ��� �������� �������� ��������
        private int physicalCores; // ��������� ���� ��� �������� ���������� � ���������� � ���������� �����
        private int logicalCores;
        private string flacAudioDir; // ���� ��� �������� ���� � �������� flac_audio


        public Form1()
        {
            InitializeComponent();
            progressBar.Minimum = 0;
            progressBar.Maximum = 100;
            progressBar.Value = 0; // ������������� ��������� ��������
            LoadSettings(); // ��������� ��������� ��� ������ ����������
            CheckAndCreateDirectories(); // �������� � �������� ����������
            LoadAudioFiles(); // ��������� ���������� ��� �������������
            this.FormClosing += Form1_FormClosing; // ����������� ����������� ������� �������� �����
            this.KeyPreview = true; // �������� ����������� ��������� ������� ���������� �� ������ �����
            this.KeyDown += Form1_KeyDown; // ���������� ���������� ������� KeyDown
            LoadFlacExecutables(); // ��������� .exe ����� ��� �������������
            LoadCPUInfo(); // ��������� ���������� � ����������


        }
        private void CheckAndCreateDirectories()
        {
            string flacExeDir = Path.Combine(Environment.CurrentDirectory, "flac_exe");
            flacAudioDir = Path.Combine(Environment.CurrentDirectory, "flac_audio"); // ������ ��� ���� ������
            string wavAudioDir = Path.Combine(Environment.CurrentDirectory, "wav_audio");

            // ��������� � ������� ����� flac_exe
            if (!Directory.Exists(flacExeDir))
            {
                Directory.CreateDirectory(flacExeDir);
            }

            // ��������� � ������� flac_audio
            if (!Directory.Exists(flacAudioDir))
            {
                Directory.CreateDirectory(flacAudioDir);
            }

            // ��������� � ������� wav_audio
            if (!Directory.Exists(wavAudioDir))
            {
                Directory.CreateDirectory(wavAudioDir);
            }
        }

        private void LoadAudioFiles()
        {
            string currentDirectory = Environment.CurrentDirectory;
            // flacAudioDir ��� �������� ��� ���� ������, ��� �� ��� ������ ����������

            string wavAudioDir = Path.Combine(currentDirectory, "wav_audio");

            listBoxAudioFiles.Items.Clear(); // ������� ���������� �������� ������

            // ��������� FLAC �����
            if (Directory.Exists(flacAudioDir))
            {
                var flacFiles = Directory.GetFiles(flacAudioDir, "*.flac");
                foreach (var file in flacFiles)
                {
                    // ��������� � ������ ������ �� �����, ������� �� �������� "_FLAC_Benchmark_H_output" � �����
                    if (!Path.GetFileName(file).Contains("_FLAC_Benchmark_H_output"))
                    {
                        listBoxAudioFiles.Items.Add(Path.GetFileName(file)); // ��������� ������ ��� �����
                    }
                }
            }

            // ��������� WAV �����
            if (Directory.Exists(wavAudioDir))
            {
                var wavFiles = Directory.GetFiles(wavAudioDir, "*.wav");
                foreach (var file in wavFiles)
                {
                    // ��������� � ������ ������ �� �����, ������� �� �������� "_FLAC_Benchmark_H_output" � �����
                    if (!Path.GetFileName(file).Contains("_FLAC_Benchmark_H_output"))
                    {
                        listBoxAudioFiles.Items.Add(Path.GetFileName(file)); // ��������� ������ ��� �����
                    }
                }
            }
        }

        // ����� ��� �������� ���������� � ����������
        private void LoadCPUInfo()
        {
            try
            {
                physicalCores = 0;
                logicalCores = 0;

                // ������� ������ ��� ��������� ���������� � �����������
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("select * from Win32_Processor"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        physicalCores += int.Parse(obj["NumberOfCores"].ToString());
                        logicalCores += int.Parse(obj["NumberOfLogicalProcessors"].ToString());
                    }
                }

                // ��������� ����� � ����������� � ����������
                labelCPUinfo.Text = $"You system has: Physical Cores: {physicalCores}, Logical Threads: {logicalCores}";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading CPU info: " + ex.Message);
            }
        }

        // ����� ��� ������ � �������� ���� .exe ������ � ����� flac_exe
        private void LoadFlacExecutables()
        {
            string flacExeDirectory = Path.Combine(Environment.CurrentDirectory, "flac_exe");
            if (Directory.Exists(flacExeDirectory))
            {
                var exeFiles = Directory.GetFiles(flacExeDirectory, "*.exe");

                listBoxFlacExecutables.Items.Clear(); // ������� ���������� ��������
                if (exeFiles.Length > 0)
                {
                    listBoxFlacExecutables.Items.AddRange(exeFiles.Select(Path.GetFileName).ToArray()); // ��������� ��������� �����
                    listBoxFlacExecutables.SelectedIndex = 0; // �������� ������ ������� �� ���������
                }
                else
                {
                    MessageBox.Show("No .exe files found in the flac_exe directory.", "Error");
                }
            }
            else
            {
                MessageBox.Show("Flac_exe directory not found.", "Error");
            }
        }


        private TimeSpan GetAudioDuration(string filePath)
        {
            using (var reader = new AudioFileReader(filePath))
            {
                return reader.TotalTime; // ���������� ����������������� � ���� TimeSpan
            }
        }
        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            // ���������, ���� �� ������ ������� Enter
            if (e.KeyCode == Keys.Enter)
            {
                buttonStartEncode_Click(sender, e); // �������� ����� ��� ������� ��������
                e.SuppressKeyPress = true; // ������������� ���������� ��������� ������� �������
            }
        }

        private void SaveSettings()
        {
            var settings = new[]
            {
                textBoxCompressionLevel.Text,
                textBoxThreads.Text,
                textBoxAdditionalArguments.Text,
                checkBoxHighPriority.Checked.ToString() // ��������� ��������� �������� High Priority

            };

            File.WriteAllLines(SettingsFileName, settings);
        }

        private void LoadSettings()
        {
            if (File.Exists(SettingsFileName))
            {
                var settings = File.ReadAllLines(SettingsFileName);
                // ���������, ��� ���� �������� �������� ��� ������� 4 ��������
                {
                    textBoxCompressionLevel.Text = settings[0];
                    textBoxThreads.Text = settings[1];
                    textBoxAdditionalArguments.Text = settings[2];
                    checkBoxHighPriority.Checked = bool.TryParse(settings[3], out bool highPriorityChecked) && highPriorityChecked; // ��������� ��������� ��������
                }
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            SaveSettings(); // ��������� ��������� ����� ��������� �����
        }

        private async void buttonStartEncode_Click(object sender, EventArgs e)
        {
            // ���������, ������ �� .exe ���� �� ListBox
            if (listBoxFlacExecutables.SelectedItem == null)
            {
                MessageBox.Show("Please select a FLAC executable file.");
                return;
            }

            // ������ ���� � ���������� ����� .exe � ����� flac_exe
            string selectedFlacFile = Path.Combine(Environment.CurrentDirectory, "flac_exe", listBoxFlacExecutables.SelectedItem.ToString());

            try
            {
                // �������� ������� ���������� ����������
                string currentDirectory = Environment.CurrentDirectory;

                // ��������� ��� ������ �����
                StringBuilder logBuilder = new StringBuilder();


                // �������� ��������� ����� �� listBoxAudioFiles
                foreach (var item in listBoxAudioFiles.Items)
                {
                    string fileName = item.ToString(); // �������� ������ ��� �����
                    string inputFilePath;

                    // ����������, �������� �� ���� FLAC ��� WAV
                    if (fileName.EndsWith(".flac", StringComparison.OrdinalIgnoreCase))
                    {
                        inputFilePath = Path.Combine(flacAudioDir, fileName); // ���� � FLAC
                    }
                    else if (fileName.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
                    {
                        // ��� WAV ���� ����� � ������ ����������
                        inputFilePath = Path.Combine(currentDirectory, "wav_audio", fileName); // ���� � WAV
                    }
                    else
                    {
                        continue; // ���������� ����� ������ �����
                    }

                    string outputFilePath = Path.Combine(Path.GetDirectoryName(inputFilePath),
                        Path.GetFileNameWithoutExtension(inputFilePath) + "_FLAC_Benchmark_H_output.flac");

                    // ��������� ������ FLAC ����� ������� ��������
                    string flacVersion = await Task.Run(() => GetFlacVersion(selectedFlacFile));
                    labelFlacUsedVersion.Text = "Using version: " + flacVersion;

                    // ��������� ���������, ����� ���������� ����� ������
                    await Task.Delay(100); // ��������� �������� ��� ���������� ����������


                    // ��������� ������������� �������� �����
                    if (!File.Exists(inputFilePath))
                    {
                        MessageBox.Show($"There is no input file '{inputFilePath}'. Please ensure the file is present in the app directory.");
                        return;
                    }

                    // �������� ������ �������� �����
                    FileInfo inputFileInfo = new FileInfo(inputFilePath);
                    long inputFileSize = inputFileInfo.Length;

                    // ���������� - �������� �������� �� TextBox � ���������, �������� �� ��� ������
                    if (!int.TryParse(textBoxCompressionLevel.Text, out int compressionLevelValue) || compressionLevelValue < 0)
                    {
                        MessageBox.Show("Please enter a number for compression level from 0 to 8");
                        return;
                    }

                    // ������ - �������� �������� �� TextBox � ���������, �������� �� ��� ������
                    if (!int.TryParse(textBoxThreads.Text, out int threadsValue) || threadsValue < 1)
                    {
                        MessageBox.Show("Please enter a number of threads (minimum 1)");
                        return;
                    }

                    // �������������� ��������� - �������� ����� �� ���������� ����
                    string additionalArgumentsText = textBoxAdditionalArguments.Text;

                    // ������� ������
                    Stopwatch stopwatch = new Stopwatch();
                    stopwatch.Start();

                    ProcessStartInfo startInfo = new ProcessStartInfo()
                    {
                        FileName = selectedFlacFile, // ���������� ��������� ���������� ���� � flac
                        Arguments = $"-{compressionLevelValue} -j{threadsValue} {additionalArgumentsText} -f \"{inputFilePath}\" -o \"{outputFilePath}\"",
                        UseShellExecute = false, // ������������ �������� (������������� �� ������ �������)
                        CreateNoWindow = true // �������� ���� �������
                    };

                    // ��������� �������
                    using (Process process = new Process())
                    {
                        process.StartInfo = startInfo;

                        // ���������� ������ ��� ���������� ���������
                        process.EnableRaisingEvents = true;

                        process.Start(); // ��������� �������

                        // ������������� ��������� �������� �� �������, ���� ������� �������
                        if (checkBoxHighPriority.Checked)
                        {
                            process.PriorityClass = ProcessPriorityClass.High;
                        }
                        else
                        {
                            process.PriorityClass = ProcessPriorityClass.Normal; // ������������� ���������� ���������
                        }

                        // ��������� �������� (� ������ ����������� ���������� ����� ������ �� �������)
                        for (int i = 0; i <= 100; i++)
                        {
                            if (!process.HasExited)
                            {
                                Invoke(new Action(() => progressBar.Value = i));
                                System.Threading.Thread.Sleep(50); // �������� ���������
                            }
                        }

                        process.WaitForExit(); // ���� ���������� ��������

                        // ��������� ��� ���������� ��������
                        if (process.ExitCode != 0)
                        {
                            MessageBox.Show("Error", "Error");
                            return;
                        }
                    }

                    stopwatch.Stop(); // ������������� ������

                    // �������� ����� ���������� � �������������
                    long elapsedMilliseconds = stopwatch.ElapsedMilliseconds;

                    // �������� ����������������� ����������
                    TimeSpan audioDuration = GetAudioDuration(inputFilePath);
                    double audioDurationInMilliSeconds = audioDuration.TotalMilliseconds; // �������� ����������������� � ��������

                    // �������� ������ ��������� ����� � ������
                    FileInfo outputFileInfo = new FileInfo(outputFilePath);
                    long outputFileSize = outputFileInfo.Exists ? outputFileInfo.Length : 0;

                    // ������������ ������� ������
                    double compressionPercentage = ((double)(outputFileSize) / inputFileSize) * 100;

                    // ������������ �������� �����������
                    double speed = audioDurationInMilliSeconds / elapsedMilliseconds; // �� ������� ��� ����������� �������

                    // ���������� ���������� � ���-����
                    string logEntry = $"{outputFileSize} bytes ({compressionPercentage:F3}%)\t{elapsedMilliseconds} ms (x{speed:F3})\t-{compressionLevelValue}\t-j{threadsValue}\t{additionalArgumentsText}\tVersion: {flacVersion}\tEXE: {Path.GetFileName(selectedFlacFile)}\t{Path.GetFileName(inputFilePath)}"; // ��������� �����
                    File.AppendAllText(LogFileName, logEntry.Trim() + Environment.NewLine); // ���������, ��� ����� ���� ������������ Trim � ����� �������

                    // ��������� ��������� ���� � ������
                    UpdateLogTextBox(logEntry);

                    // ���������� ��������-���
                    progressBar.Style = ProgressBarStyle.Blocks; // ��������� ����� "����������"
                    progressBar.Value = 0; // ���������� �������� ��������-����
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Error");
            }
        }
        // ����� ��� ��������� ������ flac
        private string GetFlacVersion(string flacFilePath)
        {
            try
            {
                // ����������� ������� ��� ��������� ������ flac.exe
                ProcessStartInfo startInfo = new ProcessStartInfo()
                {
                    FileName = flacFilePath, // ���������� ���� � flac.exe
                    Arguments = "--version",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };

                using (Process process = new Process())
                {
                    process.StartInfo = startInfo;
                    process.Start();

                    // ������ ������������ ������
                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();

                    // ���������� ����� ������ ��� ��������� � ���, ��� ������ ����������
                    return !string.IsNullOrEmpty(output) ? output.Trim() : "Version not available";
                }
            }
            catch (Exception ex)
            {
                return "Error: " + ex.Message; // ���������� ������
            }
        }

        private void UpdateLogTextBox(string logEntry)
        {
            // ��������� ����� ������ � ��������� ����
            textBoxFlacExecutables.AppendText(logEntry.Trim() + Environment.NewLine); // ���������� Trim ��� �������� ������ ��������
            textBoxFlacExecutables.ScrollToCaret(); // ������������ � ����, ����� �������� ��������� ������ �� ������
        }

        private void buttonepr8_Click(object sender, EventArgs e)
        {
            // ���������, ���������� �� -epr8 � textBoxAdditionalArguments
            if (!textBoxAdditionalArguments.Text.Contains("-epr8"))
            {
                // ���� ���, ��������� ���
                textBoxAdditionalArguments.AppendText(" -epr8"); // ��������� � �������� ����� �������
            }
        }

        private void buttonAsubdividetukey5flattop_Click(object sender, EventArgs e)
        {
            // ���������, ���������� �� -A "subdivide_tukey(5);flattop" � textBoxAdditionalArguments
            if (!textBoxAdditionalArguments.Text.Contains("-A \"subdivide_tukey(5);flattop\""))
            {
                // ���� ���, ��������� ���
                textBoxAdditionalArguments.AppendText(" -A \"subdivide_tukey(5);flattop\""); // ��������� � �������� ����� �������
            }
        }

        private void buttonNoPadding_Click(object sender, EventArgs e)
        {
            // ���������, ���������� �� --no-padding � textBoxAdditionalArguments
            if (!textBoxAdditionalArguments.Text.Contains("--no-padding"))
            {
                // ���� ���, ��������� ���
                textBoxAdditionalArguments.AppendText(" --no-padding"); // ��������� � �������� ����� �������
            }
        }

        private void buttonNoSeektable_Click(object sender, EventArgs e)
        {
            // ���������, ���������� �� --no-seektable � textBoxAdditionalArguments
            if (!textBoxAdditionalArguments.Text.Contains("--no-seektable"))
            {
                // ���� ���, ��������� ���
                textBoxAdditionalArguments.AppendText(" --no-seektable"); // ��������� � �������� ����� �������
            }
        }
        private void radioEncode_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void radioReEncode_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void buttonClear_Click(object sender, EventArgs e)
        {
            {
                // ������� ��������� ���� ��� �������������� ����������
                textBoxAdditionalArguments.Text = string.Empty;
            }
        }

        private void buttonClearLog_Click(object sender, EventArgs e)
        {
            // ������� ��������� ���� ����
            textBoxFlacExecutables.Text = string.Empty;
        }

        private void labelFlacUsedVersion_Click(object sender, EventArgs e)
        {

        }

        private void comboBoxFlacExecutables_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void buttonOpenLogtxt_Click(object sender, EventArgs e)
        {
            // ���������� ������ ���� � ����� log.txt
            string logFilePath = Path.Combine(Environment.CurrentDirectory, LogFileName);

            // ���������, ���������� �� ����
            if (File.Exists(logFilePath))
            {
                try
                {
                    // ��������� log.txt � ��������� ��������� �� ���������
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = logFilePath,
                        UseShellExecute = true // ���������� ��� �������� ����� � ������� �������� ����������
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Could not open log file: {ex.Message}", "Error");
                }
            }
            else
            {
                MessageBox.Show("Log file does not exist.", "Error");
            }
        }

        private void buttonReloadFlacExetutablesAndAudioFies_Click(object sender, EventArgs e)
        {
            // �������� � �������� ����������
            CheckAndCreateDirectories();

            // ������������� ������ .exe ������
            LoadFlacExecutables();

            // ������������� ������ �����������
            LoadAudioFiles(); // ��������� �������� ����� ������

        }

        private void labelCPUinfo_Click(object sender, EventArgs e)
        {

        }

        private void buttonHalfCores_Click(object sender, EventArgs e)
        {
            // ������������ �������� ���������� ����
            int halfCores = physicalCores / 2;
            // ��������, ��� �������� �� ������ 1
            textBoxThreads.Text = Math.Max(halfCores, 1).ToString();
        }

        private void buttonSetMaxCores_Click(object sender, EventArgs e)
        {
            // ������������� �������� ���������� ����
            textBoxThreads.Text = Math.Max(physicalCores, 1).ToString();
        }

        private void buttonSetHalfThreads_Click(object sender, EventArgs e)
        {
            // ������������ �������� ���������� �������
            int halfThreads = logicalCores / 2;
            // ��������, ��� �������� �� ������ 1
            textBoxThreads.Text = Math.Max(halfThreads, 1).ToString();
        }

        private void buttonSetMaxThreads_Click(object sender, EventArgs e)
        {
            // ������������� �������� ���������� �������
            textBoxThreads.Text = Math.Max(logicalCores, 1).ToString();
        }

        private void button5CompressionLevel_Click(object sender, EventArgs e)
        {
            textBoxCompressionLevel.Text = "5"; // ������������� �������� 5
        }

        private void buttonMaxCompressionLevel_Click(object sender, EventArgs e)
        {
            textBoxCompressionLevel.Text = "8"; // ������������� �������� 8

        }

        private void checkBoxHighPriority_CheckedChanged(object sender, EventArgs e)
        {

        }

        private async void buttonStartDecode_Click(object sender, EventArgs e)
        {
            // ���������, ������ �� .exe ���� �� ListBox
            if (listBoxFlacExecutables.SelectedItem == null)
            {
                MessageBox.Show("Please select a FLAC executable file.");
                return;
            }

            // ������ ���� � ���������� ����� .exe � ����� flac_exe
            string selectedFlacFile = Path.Combine(Environment.CurrentDirectory, "flac_exe", listBoxFlacExecutables.SelectedItem.ToString());

            try
            {
                // �������� ������� ���������� ����������
                string currentDirectory = Environment.CurrentDirectory;

                // �������� ��������� ����� �� listBoxAudioFiles
                foreach (var item in listBoxAudioFiles.Items)
                {
                    string fileName = item.ToString(); // �������� ������ ��� �����

                    // ���������, �������� �� ���� FLAC
                    if (!fileName.EndsWith(".flac", StringComparison.OrdinalIgnoreCase))
                    {
                        continue; // ���������� ����� ������ �����
                    }

                    string inputFilePath = Path.Combine(flacAudioDir, fileName); // ���� � FLAC
                    string outputFilePath = Path.Combine(flacAudioDir, Path.GetFileNameWithoutExtension(inputFilePath) + "_FLAC_Benchmark_H_output.wav");

                    // ��������� ������ FLAC ����� ������� ��������
                    string flacVersion = await Task.Run(() => GetFlacVersion(selectedFlacFile));
                    labelFlacUsedVersion.Text = "Using version: " + flacVersion;

                    // ��������� ���������, ����� ���������� ����� ������
                    await Task.Delay(100); // ��������� �������� ��� ���������� ����������

                    // ��������� ������������� �������� �����
                    if (!File.Exists(inputFilePath))
                    {
                        MessageBox.Show($"There is no input file '{inputFilePath}'. Please ensure the file is present in the app directory.");
                        return;
                    }



                    // ������� ������
                    Stopwatch stopwatch = new Stopwatch();
                    stopwatch.Start();

                    ProcessStartInfo startInfo = new ProcessStartInfo()
                    {
                        FileName = selectedFlacFile, // ���������� ��������� ����������� ���� FLAC
                        Arguments = $"-d -f \"{inputFilePath}\" -o \"{outputFilePath}\"", // ��������� ��� �������������
                        UseShellExecute = false,
                        CreateNoWindow = true // �������� ���������� ����
                    };

                    // ��������� �������
                    using (Process process = new Process())
                    {
                        process.StartInfo = startInfo;
                        process.EnableRaisingEvents = true;

                        process.Start(); // ��������� �������

                        // ������������� ��������� �������� �� �������, ���� ������� �������
                        if (checkBoxHighPriority.Checked)
                        {
                            process.PriorityClass = ProcessPriorityClass.High;
                        }
                        else
                        {
                            process.PriorityClass = ProcessPriorityClass.Normal; // ������������� ���������� ���������
                        }

                        // ���� ���������� ��������
                        await Task.Run(() => process.WaitForExit());

                        process.WaitForExit(); // ���� ���������� ��������

                        // ��������� ��� ���������� ��������
                        if (process.ExitCode != 0)
                        {
                            MessageBox.Show("Error during decoding", "Error");
                            return;
                        }
                    }

                    stopwatch.Stop(); // ������������� ������

                    // �������� ����� ���������� � �������������
                    long elapsedMilliseconds = stopwatch.ElapsedMilliseconds;

                    // �������� ����������������� ����������
                    TimeSpan audioDuration = GetAudioDuration(inputFilePath);
                    double audioDurationInMilliSeconds = audioDuration.TotalMilliseconds; // ����������������� � �������������
                    // �������� ������ ��������� ����� � ������
                    FileInfo outputFileInfo = new FileInfo(outputFilePath);
                    long outputFileSize = outputFileInfo.Exists ? outputFileInfo.Length : 0;

                    // ������������ �������� �������������
                    double speed = audioDurationInMilliSeconds / elapsedMilliseconds; // �� ������� ��� ������������� �������

                    // ���������� ���������� � ���-����
                    string logEntry = $"{outputFileSize} bytes\t{elapsedMilliseconds} ms (x{speed:F3})\tVersion: {flacVersion}\tInput: {Path.GetFileName(inputFilePath)}\tOutput: {Path.GetFileName(outputFilePath)}";
                    File.AppendAllText(LogFileName, logEntry.Trim() + Environment.NewLine); // �������� ����������

                    // ��������� ��������� ���� � ������
                    UpdateLogTextBox(logEntry);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Error");
            }
        }



    }
}