using Microsoft.VisualBasic.FileIO;
using NAudio.Wave;
using System.Diagnostics;
using System.IO.Compression;
using System.Management;


namespace FLAC_Benchmark_H
{
    public partial class Form1 : Form
    {
        private const string LogFileName = "log.txt"; // ��� ���-�����, � ������� ����� ������������ ����� ����������
        private const string SettingsFileName = "settings.txt"; // ��� ����� � �����������
        private Process _process; // ���� ��� �������� �������� ��������
        // ��������� ���� ��� �������� ���������� � ���������� � ���������� �����
        private int physicalCores;
        private int logicalCores;

        public Form1()
        {
            InitializeComponent();
            progressBar.Minimum = 0;
            progressBar.Maximum = 100;
            progressBar.Value = 0; // ������������� ��������� ��������
            LoadSettings(); // ��������� ��������� ��� ������ ����������
            this.FormClosing += Form1_FormClosing; // ����������� ����������� ������� �������� �����
            this.KeyPreview = true; // �������� ����������� ��������� ������� ���������� �� ������ �����
            this.KeyDown += Form1_KeyDown; // ���������� ���������� ������� KeyDown
            LoadAudioProperties(); // ��������� �������� ��� �������������
                                   //          CheckFlacVersion(); // �������� ������ flac.exe
            LoadFlacExecutables(); // ��������� .exe ����� ��� �������������
            LoadCPUInfo(); // ��������� ���������� � ����������


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

        // ����� ��� ������ � �������� ���� .exe ������ ����� flac
        private void LoadFlacExecutables()
        {
            string flacDirectory = Path.Combine(Environment.CurrentDirectory, "flac");
            if (Directory.Exists(flacDirectory))
            {
                var exeFiles = Directory.GetFiles(flacDirectory, "*.exe");

                listBoxFlacExecutables.Items.Clear(); // ������� ���������� ��������
                if (exeFiles.Length > 0)
                {
                    listBoxFlacExecutables.Items.AddRange(exeFiles.Select(Path.GetFileName).ToArray()); // ��������� ��������� �����
                    listBoxFlacExecutables.SelectedIndex = 0; // �������� ������ ������� �� ���������
                }
                else
                {
                    MessageBox.Show("No .exe files found in the flac directory.", "Error");
                }
            }
            else
            {
                MessageBox.Show("Flac directory not found.", "Error");
            }
        }

        //        private void CheckFlacVersion()
        //        {
        //            try
        //            {
        //        // ����������� ������� ��� ��������� ������ flac.exe
        //                ProcessStartInfo startInfo = new ProcessStartInfo()
        //                {
        //        FileName = "flac.exe",
        //        Arguments = "--version",
        //        UseShellExecute = false,
        //        RedirectStandardOutput = true,
        //        CreateNoWindow = true
        //    };
        //
        //                using (Process process = new Process())
        //                {
        //        process.StartInfo = startInfo;
        //        process.Start();
        //
        //        // ������ ������������ ������
        //        string output = process.StandardOutput.ReadToEnd();
        //        process.WaitForExit();
        //
        //                    // ������������� ����� � labelFlacUsedVersion
        //                    if (!string.IsNullOrEmpty(output))
        //                    {
        //        // ����� �������� �������������� ���������, ���� ����� ������� ������ ����� ������
        //        labelFlacUsedVersion.Text = "Using version: " + output.Trim(); // ������� ������� �� �����
        //    }
        //                    else
        //                    {
        //        labelFlacUsedVersion.Text = "Version not available";
        //    }
        //    }
        //    }
        //            catch (Exception ex)
        //            {
        //        labelFlacUsedVersion.Text = "Error: " + ex.Message; // ��������� ������
        //    }
        //}
        private void LoadAudioProperties()
        {
            string currentDirectory = Environment.CurrentDirectory;
            string wavFilePath = Path.Combine(currentDirectory, "input.wav");
            string flacFilePath = Path.Combine(currentDirectory, "input.flac");

            // �������� ����������������� ��� WAV �����
            if (File.Exists(wavFilePath))
            {
                TimeSpan wavDuration = GetAudioDuration(wavFilePath);
                long wavDurationInMilliseconds = (long)wavDuration.TotalMilliseconds; // �������� ����������������� � �������������
                labelWavFileProperties.Text = $"WAV Duration: {wavDuration.Hours:D2}:{wavDuration.Minutes:D2}:{wavDuration.Seconds:D2}:{wavDuration.Milliseconds} ({wavDurationInMilliseconds} ms)";
            }
            else
            {
                labelWavFileProperties.Text = "WAV file not found.";
            }

            // �������� ����������������� ��� FLAC �����
            if (File.Exists(flacFilePath))
            {
                TimeSpan flacDuration = GetAudioDuration(flacFilePath);
                long flacDurationInMilliseconds = (long)flacDuration.TotalMilliseconds; // �������� ����������������� � �������������
                labelFlacFileProperties.Text = $"FLAC Duration: {flacDuration.Hours:D2}:{flacDuration.Minutes:D2}:{flacDuration.Seconds:D2}:{flacDuration.Milliseconds} ({flacDurationInMilliseconds} ms)";
            }
            else
            {
                labelFlacFileProperties.Text = "FLAC file not found.";
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
                buttonStart_Click(sender, e); // �������� ����� ��� ������� ��������
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
                radioEncode.Checked.ToString(), // ��������� ��������� ����������� Encode
                radioReEncode.Checked.ToString() // ��������� ��������� ����������� Re-encode
            };

            File.WriteAllLines(SettingsFileName, settings);
        }

        private void LoadSettings()
        {
            if (File.Exists(SettingsFileName))
            {
                var settings = File.ReadAllLines(SettingsFileName);
                // ���������, ��� ���� �������� �������� ��� ������� 5 ���������
                if (settings.Length >= 5)
                {
                    textBoxCompressionLevel.Text = settings[0];
                    textBoxThreads.Text = settings[1];
                    textBoxAdditionalArguments.Text = settings[2];

                    // ������������� ��������� �����������, ���� ��� ����
                    radioEncode.Checked = bool.TryParse(settings[3], out bool encodeChecked) && encodeChecked;
                    radioReEncode.Checked = bool.TryParse(settings[4], out bool reEncodeChecked) && reEncodeChecked;
                }
                else if (settings.Length >= 3) // ���� ���� ������ 3 ��������, ��������� ��
                {
                    textBoxCompressionLevel.Text = settings[0];
                    textBoxThreads.Text = settings[1];
                    textBoxAdditionalArguments.Text = settings[2];
                    // ����������� ��������� � ��������� �� ���������
                }
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            SaveSettings(); // ��������� ��������� ����� ��������� �����
        }

        private async void buttonStart_Click(object sender, EventArgs e)
        {
            // ���������, ������ �� .exe ���� �� ComboBox
            if (listBoxFlacExecutables.SelectedItem == null)
            {
                MessageBox.Show("Please select a FLAC executable file.");
                return;
            }

            // ������ ���� � ���������� ����� .exe
            string selectedFlacFile = Path.Combine(Environment.CurrentDirectory, "flac", listBoxFlacExecutables.SelectedItem.ToString());

            try
            {
                // �������� ������� ���������� ����������
                string currentDirectory = Environment.CurrentDirectory;

                string inputFile; // ���������� ������� ����
                string outputFile = Path.Combine(currentDirectory, "output.flac"); // �������� ���� �������� �������

                // ��������� ������ FLAC ����� ������� ��������
                string flacVersion = await Task.Run(() => GetFlacVersion(selectedFlacFile));
                labelFlacUsedVersion.Text = "Using version: " + flacVersion;

                // ��������� ���������, ����� ���������� ����� ������
                await Task.Delay(100); // ��������� �������� ��� ���������� ����������

                // ���������, ����� ����������� �������
                string encodingType = string.Empty; // ���������� ��� ������ ���� �����������

                if (radioEncode.Checked)
                {
                    inputFile = Path.Combine(currentDirectory, "input.wav"); // ��������� ������� ���� ��� �����������
                    encodingType = "WAV>FLAC:"; // ������������� ��� �����������

                }
                else if (radioReEncode.Checked)
                {
                    inputFile = Path.Combine(currentDirectory, "input.flac"); // ��������� ������� ���� ��� ���������������
                    encodingType = "FLAC>FLAC:"; // ������������� ��� �����������

                }
                else
                {
                    MessageBox.Show("Please select either 'Encode' or 'Re-encode'.");
                    return;
                }

                // ��������� ������������� �������� �����
                if (!File.Exists(inputFile))
                {
                    MessageBox.Show($"There is no input file '{inputFile}'. Please ensure the file is present in the app directory.");
                    return;
                }

                // �������� �������� �� TextBox � ���������, �������� �� ��� ������
                if (!int.TryParse(textBoxCompressionLevel.Text, out int compressionLevelValue) || compressionLevelValue < 0)
                {
                    MessageBox.Show("Please enter a number for compression level from 0 to 8");
                    return;
                }

                // �������� �������� �� TextBox � ���������, �������� �� ��� ������
                if (!int.TryParse(textBoxThreads.Text, out int threadsValue) || threadsValue < 1)
                {
                    MessageBox.Show("Please enter a number of threads (minimum 1)");
                    return;
                }

                // �������� ����� �� ���������� ����
                string additionalArgumentsText = textBoxAdditionalArguments.Text; // �������� textBoxInput �� ��� ������ ���������� ����

                // ������� ������
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                ProcessStartInfo startInfo = new ProcessStartInfo()
                {
                    FileName = selectedFlacFile, // ���������� ��������� ���������� ���� � flac
                    Arguments = $"-{compressionLevelValue} -j{threadsValue} {additionalArgumentsText} -f \"{inputFile}\" -o \"{outputFile}\"",
                    // ������������ �������� (������������� �� ������ �������)
                    UseShellExecute = false,
                    CreateNoWindow = true // �������� ���� �������
                };

                // ��������� �������
                using (Process process = new Process())
                {
                    process.StartInfo = startInfo;

                    // ���������� ������ ��� ���������� ���������
                    process.EnableRaisingEvents = true;

                    process.Start(); // ��������� �������

                    // ������������� ��������� �������� �� ������������
                    process.PriorityClass = ProcessPriorityClass.High;

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
                TimeSpan audioDuration = GetAudioDuration(inputFile);
                double audioDurationInMilliSeconds = audioDuration.TotalMilliseconds; // �������� ����������������� � ��������

                // �������� ������ ��������� ����� � ������
                FileInfo outputFileInfo = new FileInfo(outputFile);
                long fileSizeInBytes = outputFileInfo.Exists ? outputFileInfo.Length : 0;

                // ������������ �������� �����������
                double speed = audioDurationInMilliSeconds / elapsedMilliseconds; // �� ������� ��� ����������� �������

                // ���������� ���������� � ���-����
                string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\t{encodingType}\t{fileSizeInBytes} bytes\t{elapsedMilliseconds} ms (x{speed:F3})\t-{compressionLevelValue}\t-j{threadsValue}\t{additionalArgumentsText}\tVersion: {flacVersion}\tEXE: {Path.GetFileName(selectedFlacFile)}";
                File.AppendAllText(LogFileName, logEntry.Trim() + Environment.NewLine); // ���������, ��� ����� ���� ������������ Trim � ����� �������

                // ��������� ��������� ���� � ������
                UpdateLogTextBox(logEntry);

                // ���������� ��������-���
                progressBar.Style = ProgressBarStyle.Blocks; // ��������� ����� "����������"
                progressBar.Value = 0; // ���������� �������� ��������-����
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
            // ������������� ������ .exe ������
            LoadFlacExecutables();

            // ������������� �������� �����������
            LoadAudioProperties();
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
    }
}
