using System.Diagnostics;
using System.IO.Compression;

namespace FLAC_Benchmark_H
{
    public partial class Form1 : Form
    {
        private const string LogFileName = "log.txt"; // ��� ���-�����, � ������� ����� ������������ ����� ����������
        private const string SettingsFileName = "settings.txt"; // ��� ����� � �����������
        private Process _process; // ���� ��� �������� �������� ��������

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
                textBoxAdditionalArguments.Text
            };

            File.WriteAllLines(SettingsFileName, settings);
        }

        private void LoadSettings()
        {
            if (File.Exists(SettingsFileName))
            {
                var settings = File.ReadAllLines(SettingsFileName);
                if (settings.Length >= 3)
                {
                    textBoxCompressionLevel.Text = settings[0];
                    textBoxThreads.Text = settings[1];
                    textBoxAdditionalArguments.Text = settings[2];
                }
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            SaveSettings(); // ��������� ��������� ����� ��������� �����
        }

        private void buttonStart_Click(object sender, EventArgs e)
        {
            try
            {
                // �������� ������� ���������� ����������
                string currentDirectory = Environment.CurrentDirectory;

                // ��������� ������� � �������� �����
                string inputFile = Path.Combine(currentDirectory, "input.flac");
                string outputFile = Path.Combine(currentDirectory, "output.flac");



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

                // ��������� ������������� �������� �����
                if (!File.Exists(inputFile))
                {
                    MessageBox.Show("There is no input file. Please copy any .flac file in app directory and rename it to input.flac");
                    return;
                }

                // ������� ������
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                ProcessStartInfo startInfo = new ProcessStartInfo()
                {
                    FileName = "cmd.exe",
                    Arguments = $"/C flac.exe -{compressionLevelValue} -j{threadsValue} {additionalArgumentsText} -f \"{inputFile}\" -o \"{outputFile}\"",
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

                // �������� ������ ��������� ����� � ������
                FileInfo outputFileInfo = new FileInfo(outputFile);
                long fileSizeInBytes = outputFileInfo.Exists ? outputFileInfo.Length : 0;

                // ���������� ���������� � ���-����
                string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\t-{compressionLevelValue}\t-j{threadsValue}\t{additionalArgumentsText}\t{elapsedMilliseconds} ms\t{fileSizeInBytes} bytes\n";
                File.AppendAllText(LogFileName, logEntry); // ���������� � ���-����

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
        private void UpdateLogTextBox(string logEntry)
        {
            // ��������� ����� ������ � ��������� ����
            textBoxLog.AppendText(logEntry + Environment.NewLine); // textBoxLog - ��� ��� ������ TextBox
            textBoxLog.ScrollToCaret(); // ������������ � ����, ����� �������� ��������� ������ �� ������
        }
    }
}
