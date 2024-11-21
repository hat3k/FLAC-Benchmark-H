using System.Diagnostics;
using System.IO.Compression;

namespace FLAC_Benchmark_H
{
    public partial class Form1 : Form
    {
        private const string LogFileName = "log.txt"; // Имя лог-файла, в который будет записываться время выполнения
        private const string SettingsFileName = "settings.txt"; // Имя файла с настройками
        private Process _process; // Поле для хранения текущего процесса

        public Form1()
        {
            InitializeComponent();
            progressBar.Minimum = 0;
            progressBar.Maximum = 100;
            progressBar.Value = 0; // Устанавливаем начальное значение
            LoadSettings(); // Загружаем настройки при старте приложения
            this.FormClosing += Form1_FormClosing; // Регистрация обработчика события закрытия формы
            this.KeyPreview = true; // Включаем возможность перехвата событий клавиатуры на уровне формы
            this.KeyDown += Form1_KeyDown; // Подключаем обработчик события KeyDown
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            // Проверяем, была ли нажата клавиша Enter
            if (e.KeyCode == Keys.Enter)
            {
                buttonStart_Click(sender, e); // Вызываем метод для запуска процесса
                e.SuppressKeyPress = true; // предотвращаем дальнейшую обработку нажатия клавиши
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
            SaveSettings(); // Сохраняем настройки перед закрытием формы
        }

        private void buttonStart_Click(object sender, EventArgs e)
        {
            try
            {
                // Получаем текущую директорию приложения
                string currentDirectory = Environment.CurrentDirectory;

                // Указываем входной и выходной файлы
                string inputFile = Path.Combine(currentDirectory, "input.flac");
                string outputFile = Path.Combine(currentDirectory, "output.flac");



                // Получаем значение из TextBox и проверяем, является ли оно числом
                if (!int.TryParse(textBoxCompressionLevel.Text, out int compressionLevelValue) || compressionLevelValue < 0)
                {
                    MessageBox.Show("Please enter a number for compression level from 0 to 8");
                    return;
                }

                // Получаем значение из TextBox и проверяем, является ли оно числом
                if (!int.TryParse(textBoxThreads.Text, out int threadsValue) || threadsValue < 1)
                {
                    MessageBox.Show("Please enter a number of threads (minimum 1)");
                    return;
                }

                // Получаем текст из текстового поля
                string additionalArgumentsText = textBoxAdditionalArguments.Text; // Замените textBoxInput на имя вашего текстового поля

                // Проверяем существование входного файла
                if (!File.Exists(inputFile))
                {
                    MessageBox.Show("There is no input file. Please copy any .flac file in app directory and rename it to input.flac");
                    return;
                }

                // Создаем таймер
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                ProcessStartInfo startInfo = new ProcessStartInfo()
                {
                    FileName = "cmd.exe",
                    Arguments = $"/C flac.exe -{compressionLevelValue} -j{threadsValue} {additionalArgumentsText} -f \"{inputFile}\" -o \"{outputFile}\"",
                    // Использовать оболочку (настраивается по вашему желанию)
                    UseShellExecute = false,
                    CreateNoWindow = true // Скрываем окно консоли
                };

                // Запускаем процесс
                using (Process process = new Process())
                {
                    process.StartInfo = startInfo;

                    // Обработчик вывода для обновления прогресса
                    process.EnableRaisingEvents = true;

                    process.Start(); // Запускаем процесс

                    // Устанавливаем приоритет процесса на максимальный
                    process.PriorityClass = ProcessPriorityClass.High;

                    // Обновляем прогресс (в случае длительного выполнения можно делать по другому)
                    for (int i = 0; i <= 100; i++)
                    {
                        if (!process.HasExited)
                        {
                            Invoke(new Action(() => progressBar.Value = i));
                            System.Threading.Thread.Sleep(50); // Эмуляция прогресса
                        }
                    }

                    process.WaitForExit(); // Ждем завершения процесса

                    // Проверяем код завершения процесса
                    if (process.ExitCode != 0)
                    {
                        MessageBox.Show("Error", "Error");
                        return;
                    }
                }

                stopwatch.Stop(); // Останавливаем таймер

                // Получаем время выполнения в миллисекундах
                long elapsedMilliseconds = stopwatch.ElapsedMilliseconds;

                // Получаем размер выходного файла в байтах
                FileInfo outputFileInfo = new FileInfo(outputFile);
                long fileSizeInBytes = outputFileInfo.Exists ? outputFileInfo.Length : 0;

                // Дописываем информацию в лог-файл
                string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\t-{compressionLevelValue}\t-j{threadsValue}\t{additionalArgumentsText}\t{elapsedMilliseconds} ms\t{fileSizeInBytes} bytes\n";
                File.AppendAllText(LogFileName, logEntry); // Дописываем в лог-файл

                // Обновляем текстовое поле с логами
                UpdateLogTextBox(logEntry);

                // Сбрасываем прогресс-бар
                progressBar.Style = ProgressBarStyle.Blocks; // Выключаем стиль "маркировка"
                progressBar.Value = 0; // Сбрасываем значение прогресс-бара
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Error");
            }
        }
        private void UpdateLogTextBox(string logEntry)
        {
            // Добавляем новую запись в текстовое поле
            textBoxLog.AppendText(logEntry + Environment.NewLine); // textBoxLog - это имя вашего TextBox
            textBoxLog.ScrollToCaret(); // Прокручиваем к низу, чтобы показать последнюю запись на экране
        }
    }
}
