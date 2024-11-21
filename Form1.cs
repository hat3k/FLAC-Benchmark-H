using Microsoft.VisualBasic.FileIO;
using NAudio.Wave;
using System.Diagnostics;
using System.IO.Compression;
using System.Management;


namespace FLAC_Benchmark_H
{
    public partial class Form1 : Form
    {
        private const string LogFileName = "log.txt"; // Имя лог-файла, в который будет записываться время выполнения
        private const string SettingsFileName = "settings.txt"; // Имя файла с настройками
        private Process _process; // Поле для хранения текущего процесса
        // Объявляем поля для хранения информации о физических и логических ядрах
        private int physicalCores;
        private int logicalCores;

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
            LoadAudioProperties(); // загружаем свойства при инициализации
                                   //          CheckFlacVersion(); // Проверка версии flac.exe
            LoadFlacExecutables(); // Загружаем .exe файлы при инициализации
            LoadCPUInfo(); // Загружаем информацию о процессоре


        }

        // Метод для загрузки информации о процессоре
        private void LoadCPUInfo()
        {
            try
            {
                physicalCores = 0;
                logicalCores = 0;

                // Создаем запрос для получения информации о процессорах
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("select * from Win32_Processor"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        physicalCores += int.Parse(obj["NumberOfCores"].ToString());
                        logicalCores += int.Parse(obj["NumberOfLogicalProcessors"].ToString());
                    }
                }

                // Обновляем метку с информацией о процессоре
                labelCPUinfo.Text = $"You system has: Physical Cores: {physicalCores}, Logical Threads: {logicalCores}";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading CPU info: " + ex.Message);
            }
        }

        // Метод для поиска и загрузки всех .exe файлов папке flac
        private void LoadFlacExecutables()
        {
            string flacDirectory = Path.Combine(Environment.CurrentDirectory, "flac");
            if (Directory.Exists(flacDirectory))
            {
                var exeFiles = Directory.GetFiles(flacDirectory, "*.exe");

                listBoxFlacExecutables.Items.Clear(); // Очищаем предыдущие элементы
                if (exeFiles.Length > 0)
                {
                    listBoxFlacExecutables.Items.AddRange(exeFiles.Select(Path.GetFileName).ToArray()); // Добавляем найденные файлы
                    listBoxFlacExecutables.SelectedIndex = 0; // Выбираем первый элемент по умолчанию
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
        //        // Настраиваем процесс для получения версии flac.exe
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
        //        // Чтение стандартного вывода
        //        string output = process.StandardOutput.ReadToEnd();
        //        process.WaitForExit();
        //
        //                    // Устанавливаем текст в labelFlacUsedVersion
        //                    if (!string.IsNullOrEmpty(output))
        //                    {
        //        // Можно добавить дополнительную обработку, если нужно извлечь только номер версии
        //        labelFlacUsedVersion.Text = "Using version: " + output.Trim(); // Убираем пробелы по краям
        //    }
        //                    else
        //                    {
        //        labelFlacUsedVersion.Text = "Version not available";
        //    }
        //    }
        //    }
        //            catch (Exception ex)
        //            {
        //        labelFlacUsedVersion.Text = "Error: " + ex.Message; // Обработка ошибок
        //    }
        //}
        private void LoadAudioProperties()
        {
            string currentDirectory = Environment.CurrentDirectory;
            string wavFilePath = Path.Combine(currentDirectory, "input.wav");
            string flacFilePath = Path.Combine(currentDirectory, "input.flac");

            // Получаем продолжительность для WAV файла
            if (File.Exists(wavFilePath))
            {
                TimeSpan wavDuration = GetAudioDuration(wavFilePath);
                long wavDurationInMilliseconds = (long)wavDuration.TotalMilliseconds; // Получаем продолжительность в миллисекундах
                labelWavFileProperties.Text = $"WAV Duration: {wavDuration.Hours:D2}:{wavDuration.Minutes:D2}:{wavDuration.Seconds:D2}:{wavDuration.Milliseconds} ({wavDurationInMilliseconds} ms)";
            }
            else
            {
                labelWavFileProperties.Text = "WAV file not found.";
            }

            // Получаем продолжительность для FLAC файла
            if (File.Exists(flacFilePath))
            {
                TimeSpan flacDuration = GetAudioDuration(flacFilePath);
                long flacDurationInMilliseconds = (long)flacDuration.TotalMilliseconds; // Получаем продолжительность в миллисекундах
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
                return reader.TotalTime; // Возвращает продолжительность в виде TimeSpan
            }
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
                textBoxAdditionalArguments.Text,
                radioEncode.Checked.ToString(), // Добавляем состояние радиокнопки Encode
                radioReEncode.Checked.ToString() // Добавляем состояние радиокнопки Re-encode
            };

            File.WriteAllLines(SettingsFileName, settings);
        }

        private void LoadSettings()
        {
            if (File.Exists(SettingsFileName))
            {
                var settings = File.ReadAllLines(SettingsFileName);
                // Проверяем, что файл настроек содержит как минимум 5 элементов
                if (settings.Length >= 5)
                {
                    textBoxCompressionLevel.Text = settings[0];
                    textBoxThreads.Text = settings[1];
                    textBoxAdditionalArguments.Text = settings[2];

                    // Устанавливаем состояния радиокнопок, если они есть
                    radioEncode.Checked = bool.TryParse(settings[3], out bool encodeChecked) && encodeChecked;
                    radioReEncode.Checked = bool.TryParse(settings[4], out bool reEncodeChecked) && reEncodeChecked;
                }
                else if (settings.Length >= 3) // Если есть только 3 элемента, загружаем их
                {
                    textBoxCompressionLevel.Text = settings[0];
                    textBoxThreads.Text = settings[1];
                    textBoxAdditionalArguments.Text = settings[2];
                    // Радиокнопки останутся в значениях по умолчанию
                }
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            SaveSettings(); // Сохраняем настройки перед закрытием формы
        }

        private async void buttonStart_Click(object sender, EventArgs e)
        {
            // Проверяем, выбран ли .exe файл из ComboBox
            if (listBoxFlacExecutables.SelectedItem == null)
            {
                MessageBox.Show("Please select a FLAC executable file.");
                return;
            }

            // Полный путь к выбранному файлу .exe
            string selectedFlacFile = Path.Combine(Environment.CurrentDirectory, "flac", listBoxFlacExecutables.SelectedItem.ToString());

            try
            {
                // Получаем текущую директорию приложения
                string currentDirectory = Environment.CurrentDirectory;

                string inputFile; // Определяем входной файл
                string outputFile = Path.Combine(currentDirectory, "output.flac"); // Выходной файл остается прежним

                // Обновляем версию FLAC перед началом процесса
                string flacVersion = await Task.Run(() => GetFlacVersion(selectedFlacFile));
                labelFlacUsedVersion.Text = "Using version: " + flacVersion;

                // Обновляем интерфейс, чтобы отобразить новую версию
                await Task.Delay(100); // Небольшая задержка для обновления интерфейса

                // Проверяем, какая радиокнопка выбрана
                string encodingType = string.Empty; // Переменная для записи типа кодирования

                if (radioEncode.Checked)
                {
                    inputFile = Path.Combine(currentDirectory, "input.wav"); // Указываем входной файл для кодирования
                    encodingType = "WAV>FLAC:"; // Устанавливаем тип кодирования

                }
                else if (radioReEncode.Checked)
                {
                    inputFile = Path.Combine(currentDirectory, "input.flac"); // Указываем входной файл для перекодирования
                    encodingType = "FLAC>FLAC:"; // Устанавливаем тип кодирования

                }
                else
                {
                    MessageBox.Show("Please select either 'Encode' or 'Re-encode'.");
                    return;
                }

                // Проверяем существование входного файла
                if (!File.Exists(inputFile))
                {
                    MessageBox.Show($"There is no input file '{inputFile}'. Please ensure the file is present in the app directory.");
                    return;
                }

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

                // Создаем таймер
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                ProcessStartInfo startInfo = new ProcessStartInfo()
                {
                    FileName = selectedFlacFile, // Используем выбранный переменный путь к flac
                    Arguments = $"-{compressionLevelValue} -j{threadsValue} {additionalArgumentsText} -f \"{inputFile}\" -o \"{outputFile}\"",
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

                // Получаем продолжительность аудиофайла
                TimeSpan audioDuration = GetAudioDuration(inputFile);
                double audioDurationInMilliSeconds = audioDuration.TotalMilliseconds; // Получаем продолжительность в секундах

                // Получаем размер выходного файла в байтах
                FileInfo outputFileInfo = new FileInfo(outputFile);
                long fileSizeInBytes = outputFileInfo.Exists ? outputFileInfo.Length : 0;

                // Рассчитываем скорость кодирования
                double speed = audioDurationInMilliSeconds / elapsedMilliseconds; // Во сколько раз кодирование быстрее

                // Дописываем информацию в лог-файл
                string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\t{encodingType}\t{fileSizeInBytes} bytes\t{elapsedMilliseconds} ms (x{speed:F3})\t-{compressionLevelValue}\t-j{threadsValue}\t{additionalArgumentsText}\tVersion: {flacVersion}\tEXE: {Path.GetFileName(selectedFlacFile)}";
                File.AppendAllText(LogFileName, logEntry.Trim() + Environment.NewLine); // Убедитесь, что здесь тоже используется Trim и новая строчка

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
        // Метод для получения версии flac
        private string GetFlacVersion(string flacFilePath)
        {
            try
            {
                // Настраиваем процесс для получения версии flac.exe
                ProcessStartInfo startInfo = new ProcessStartInfo()
                {
                    FileName = flacFilePath, // Используем путь к flac.exe
                    Arguments = "--version",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };

                using (Process process = new Process())
                {
                    process.StartInfo = startInfo;
                    process.Start();

                    // Чтение стандартного вывода
                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();

                    // Возвращаем номер версии или сообщение о том, что версия недоступна
                    return !string.IsNullOrEmpty(output) ? output.Trim() : "Version not available";
                }
            }
            catch (Exception ex)
            {
                return "Error: " + ex.Message; // Возвращаем ошибку
            }
        }

        private void UpdateLogTextBox(string logEntry)
        {
            // Добавляем новую запись в текстовое поле
            textBoxFlacExecutables.AppendText(logEntry.Trim() + Environment.NewLine); // Используем Trim для удаления лишних пробелов
            textBoxFlacExecutables.ScrollToCaret(); // Прокручиваем к низу, чтобы показать последнюю запись на экране
        }

        private void buttonepr8_Click(object sender, EventArgs e)
        {
            // Проверяем, содержится ли -epr8 в textBoxAdditionalArguments
            if (!textBoxAdditionalArguments.Text.Contains("-epr8"))
            {
                // Если нет, добавляем его
                textBoxAdditionalArguments.AppendText(" -epr8"); // Добавляем с пробелом перед текстом
            }
        }

        private void buttonAsubdividetukey5flattop_Click(object sender, EventArgs e)
        {
            // Проверяем, содержится ли -A "subdivide_tukey(5);flattop" в textBoxAdditionalArguments
            if (!textBoxAdditionalArguments.Text.Contains("-A \"subdivide_tukey(5);flattop\""))
            {
                // Если нет, добавляем его
                textBoxAdditionalArguments.AppendText(" -A \"subdivide_tukey(5);flattop\""); // Добавляем с пробелом перед текстом
            }
        }

        private void buttonNoPadding_Click(object sender, EventArgs e)
        {
            // Проверяем, содержится ли --no-padding в textBoxAdditionalArguments
            if (!textBoxAdditionalArguments.Text.Contains("--no-padding"))
            {
                // Если нет, добавляем его
                textBoxAdditionalArguments.AppendText(" --no-padding"); // Добавляем с пробелом перед текстом
            }
        }

        private void buttonNoSeektable_Click(object sender, EventArgs e)
        {
            // Проверяем, содержится ли --no-seektable в textBoxAdditionalArguments
            if (!textBoxAdditionalArguments.Text.Contains("--no-seektable"))
            {
                // Если нет, добавляем его
                textBoxAdditionalArguments.AppendText(" --no-seektable"); // Добавляем с пробелом перед текстом
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
                // Очищаем текстовое поле для дополнительных аргументов
                textBoxAdditionalArguments.Text = string.Empty;
            }
        }

        private void buttonClearLog_Click(object sender, EventArgs e)
        {
            // Очищаем текстовое поле лога
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
            // Определяем полный путь к файлу log.txt
            string logFilePath = Path.Combine(Environment.CurrentDirectory, LogFileName);

            // Проверяем, существует ли файл
            if (File.Exists(logFilePath))
            {
                try
                {
                    // Открываем log.txt в текстовом редакторе по умолчанию
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = logFilePath,
                        UseShellExecute = true // Необходимо для открытия файла с помощью внешнего приложения
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
            // Перезагружаем список .exe файлов
            LoadFlacExecutables();

            // Перезагружаем свойства аудиофайлов
            LoadAudioProperties();
        }

        private void labelCPUinfo_Click(object sender, EventArgs e)
        {

        }

        private void buttonHalfCores_Click(object sender, EventArgs e)
        {
            // Рассчитываем половину физических ядер
            int halfCores = physicalCores / 2;
            // Убедимся, что значение не меньше 1
            textBoxThreads.Text = Math.Max(halfCores, 1).ToString();
        }

        private void buttonSetMaxCores_Click(object sender, EventArgs e)
        {
            // Устанавливаем максимум физических ядер
            textBoxThreads.Text = Math.Max(physicalCores, 1).ToString();
        }

        private void buttonSetHalfThreads_Click(object sender, EventArgs e)
        {
            // Рассчитываем половину логических потоков
            int halfThreads = logicalCores / 2;
            // Убедимся, что значение не меньше 1
            textBoxThreads.Text = Math.Max(halfThreads, 1).ToString();
        }

        private void buttonSetMaxThreads_Click(object sender, EventArgs e)
        {
            // Устанавливаем максимум логических потоков
            textBoxThreads.Text = Math.Max(logicalCores, 1).ToString();
        }

        private void button5CompressionLevel_Click(object sender, EventArgs e)
        {
            textBoxCompressionLevel.Text = "5"; // Устанавливаем значение 5
        }

        private void buttonMaxCompressionLevel_Click(object sender, EventArgs e)
        {
            textBoxCompressionLevel.Text = "8"; // Устанавливаем значение 8

        }
    }
}
