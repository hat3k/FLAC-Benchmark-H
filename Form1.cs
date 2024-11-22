using NAudio.Wave; // Необходимо для работы с аудиофайлами
using System.Diagnostics; // Необходимо для работы с процессами
using System.Management; // Необходимо для получения информации о процессоре
using System.Text; // Необходимо для работы с StringBuilder


namespace FLAC_Benchmark_H
{
    public partial class Form1 : Form
    {
        private const string LogFileName = "log.txt"; // Имя лог-файла, в который будет записываться время выполнения
        private const string SettingsFileName = "settings.txt"; // Имя файла с настройками
        private Process _process; // Поле для хранения текущего процесса
        private int physicalCores; // Объявляем поля для хранения информации о физических и логических ядрах
        private int logicalCores;
        private string flacAudioDir; // Поле для хранения пути к каталогу flac_audio


        public Form1()
        {
            InitializeComponent();
            progressBar.Minimum = 0;
            progressBar.Maximum = 100;
            progressBar.Value = 0; // Устанавливаем начальное значение
            LoadSettings(); // Загружаем настройки при старте приложения
            CheckAndCreateDirectories(); // Проверка и создание директорий
            LoadAudioFiles(); // Загружаем аудиофайлы при инициализации
            this.FormClosing += Form1_FormClosing; // Регистрация обработчика события закрытия формы
            this.KeyPreview = true; // Включаем возможность перехвата событий клавиатуры на уровне формы
            this.KeyDown += Form1_KeyDown; // Подключаем обработчик события KeyDown
            LoadFlacExecutables(); // Загружаем .exe файлы при инициализации
            LoadCPUInfo(); // Загружаем информацию о процессоре


        }
        private void CheckAndCreateDirectories()
        {
            string flacExeDir = Path.Combine(Environment.CurrentDirectory, "flac_exe");
            flacAudioDir = Path.Combine(Environment.CurrentDirectory, "flac_audio"); // Теперь это поле класса
            string wavAudioDir = Path.Combine(Environment.CurrentDirectory, "wav_audio");

            // Проверяем и создаем папку flac_exe
            if (!Directory.Exists(flacExeDir))
            {
                Directory.CreateDirectory(flacExeDir);
            }

            // Проверяем и создаем flac_audio
            if (!Directory.Exists(flacAudioDir))
            {
                Directory.CreateDirectory(flacAudioDir);
            }

            // Проверяем и создаем wav_audio
            if (!Directory.Exists(wavAudioDir))
            {
                Directory.CreateDirectory(wavAudioDir);
            }
        }

        private void LoadAudioFiles()
        {
            string currentDirectory = Environment.CurrentDirectory;
            // flacAudioDir уже объявлен как поле класса, тут мы его просто используем

            string wavAudioDir = Path.Combine(currentDirectory, "wav_audio");

            listBoxAudioFiles.Items.Clear(); // Очищаем предыдущие элементы списка

            // Загружаем FLAC файлы
            if (Directory.Exists(flacAudioDir))
            {
                var flacFiles = Directory.GetFiles(flacAudioDir, "*.flac");
                foreach (var file in flacFiles)
                {
                    // Добавляем в список только те файлы, которые не содержат "_FLAC_Benchmark_H_output" в имени
                    if (!Path.GetFileName(file).Contains("_FLAC_Benchmark_H_output"))
                    {
                        listBoxAudioFiles.Items.Add(Path.GetFileName(file)); // Добавляем только имя файла
                    }
                }
            }

            // Загружаем WAV файлы
            if (Directory.Exists(wavAudioDir))
            {
                var wavFiles = Directory.GetFiles(wavAudioDir, "*.wav");
                foreach (var file in wavFiles)
                {
                    // Добавляем в список только те файлы, которые не содержат "_FLAC_Benchmark_H_output" в имени
                    if (!Path.GetFileName(file).Contains("_FLAC_Benchmark_H_output"))
                    {
                        listBoxAudioFiles.Items.Add(Path.GetFileName(file)); // Добавляем только имя файла
                    }
                }
            }
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

        // Метод для поиска и загрузки всех .exe файлов в папке flac_exe
        private void LoadFlacExecutables()
        {
            string flacExeDirectory = Path.Combine(Environment.CurrentDirectory, "flac_exe");
            if (Directory.Exists(flacExeDirectory))
            {
                var exeFiles = Directory.GetFiles(flacExeDirectory, "*.exe");

                listBoxFlacExecutables.Items.Clear(); // Очищаем предыдущие элементы
                if (exeFiles.Length > 0)
                {
                    listBoxFlacExecutables.Items.AddRange(exeFiles.Select(Path.GetFileName).ToArray()); // Добавляем найденные файлы
                    listBoxFlacExecutables.SelectedIndex = 0; // Выбираем первый элемент по умолчанию
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
                return reader.TotalTime; // Возвращает продолжительность в виде TimeSpan
            }
        }
        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            // Проверяем, была ли нажата клавиша Enter
            if (e.KeyCode == Keys.Enter)
            {
                buttonStartEncode_Click(sender, e); // Вызываем метод для запуска процесса
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
                checkBoxHighPriority.Checked.ToString() // Добавляем состояние чекбокса High Priority

            };

            File.WriteAllLines(SettingsFileName, settings);
        }

        private void LoadSettings()
        {
            if (File.Exists(SettingsFileName))
            {
                var settings = File.ReadAllLines(SettingsFileName);
                // Проверяем, что файл настроек содержит как минимум 4 элемента
                {
                    textBoxCompressionLevel.Text = settings[0];
                    textBoxThreads.Text = settings[1];
                    textBoxAdditionalArguments.Text = settings[2];
                    checkBoxHighPriority.Checked = bool.TryParse(settings[3], out bool highPriorityChecked) && highPriorityChecked; // Загружаем состояние чекбокса
                }
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            SaveSettings(); // Сохраняем настройки перед закрытием формы
        }

        private async void buttonStartEncode_Click(object sender, EventArgs e)
        {
            // Проверяем, выбран ли .exe файл из ListBox
            if (listBoxFlacExecutables.SelectedItem == null)
            {
                MessageBox.Show("Please select a FLAC executable file.");
                return;
            }

            // Полный путь к выбранному файлу .exe в папке flac_exe
            string selectedFlacFile = Path.Combine(Environment.CurrentDirectory, "flac_exe", listBoxFlacExecutables.SelectedItem.ToString());

            try
            {
                // Получаем текущую директорию приложения
                string currentDirectory = Environment.CurrentDirectory;

                // Настройка для записи логов
                StringBuilder logBuilder = new StringBuilder();


                // Получаем выбранные файлы из listBoxAudioFiles
                foreach (var item in listBoxAudioFiles.Items)
                {
                    string fileName = item.ToString(); // Получаем только имя файла
                    string inputFilePath;

                    // Определяем, является ли файл FLAC или WAV
                    if (fileName.EndsWith(".flac", StringComparison.OrdinalIgnoreCase))
                    {
                        inputFilePath = Path.Combine(flacAudioDir, fileName); // Путь к FLAC
                    }
                    else if (fileName.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
                    {
                        // Для WAV путь будет в другой директории
                        inputFilePath = Path.Combine(currentDirectory, "wav_audio", fileName); // Путь к WAV
                    }
                    else
                    {
                        continue; // Пропустить файлы других типов
                    }

                    string outputFilePath = Path.Combine(Path.GetDirectoryName(inputFilePath),
                        Path.GetFileNameWithoutExtension(inputFilePath) + "_FLAC_Benchmark_H_output.flac");

                    // Обновляем версию FLAC перед началом процесса
                    string flacVersion = await Task.Run(() => GetFlacVersion(selectedFlacFile));
                    labelFlacUsedVersion.Text = "Using version: " + flacVersion;

                    // Обновляем интерфейс, чтобы отобразить новую версию
                    await Task.Delay(100); // Небольшая задержка для обновления интерфейса


                    // Проверяем существование входного файла
                    if (!File.Exists(inputFilePath))
                    {
                        MessageBox.Show($"There is no input file '{inputFilePath}'. Please ensure the file is present in the app directory.");
                        return;
                    }

                    // Получаем размер входного файла
                    FileInfo inputFileInfo = new FileInfo(inputFilePath);
                    long inputFileSize = inputFileInfo.Length;

                    // Компрессия - Получаем значение из TextBox и проверяем, является ли оно числом
                    if (!int.TryParse(textBoxCompressionLevel.Text, out int compressionLevelValue) || compressionLevelValue < 0)
                    {
                        MessageBox.Show("Please enter a number for compression level from 0 to 8");
                        return;
                    }

                    // Потоки - Получаем значение из TextBox и проверяем, является ли оно числом
                    if (!int.TryParse(textBoxThreads.Text, out int threadsValue) || threadsValue < 1)
                    {
                        MessageBox.Show("Please enter a number of threads (minimum 1)");
                        return;
                    }

                    // Дополнительные аргументы - Получаем текст из текстового поля
                    string additionalArgumentsText = textBoxAdditionalArguments.Text;

                    // Создаем таймер
                    Stopwatch stopwatch = new Stopwatch();
                    stopwatch.Start();

                    ProcessStartInfo startInfo = new ProcessStartInfo()
                    {
                        FileName = selectedFlacFile, // Используем выбранный переменный путь к flac
                        Arguments = $"-{compressionLevelValue} -j{threadsValue} {additionalArgumentsText} -f \"{inputFilePath}\" -o \"{outputFilePath}\"",
                        UseShellExecute = false, // Использовать оболочку (настраивается по вашему желанию)
                        CreateNoWindow = true // Скрываем окно консоли
                    };

                    // Запускаем процесс
                    using (Process process = new Process())
                    {
                        process.StartInfo = startInfo;

                        // Обработчик вывода для обновления прогресса
                        process.EnableRaisingEvents = true;

                        process.Start(); // Запускаем процесс

                        // Устанавливаем приоритет процесса на высокий, если чекбокс включен
                        if (checkBoxHighPriority.Checked)
                        {
                            process.PriorityClass = ProcessPriorityClass.High;
                        }
                        else
                        {
                            process.PriorityClass = ProcessPriorityClass.Normal; // Устанавливаем нормальный приоритет
                        }

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
                    TimeSpan audioDuration = GetAudioDuration(inputFilePath);
                    double audioDurationInMilliSeconds = audioDuration.TotalMilliseconds; // Получаем продолжительность в секундах

                    // Получаем размер выходного файла в байтах
                    FileInfo outputFileInfo = new FileInfo(outputFilePath);
                    long outputFileSize = outputFileInfo.Exists ? outputFileInfo.Length : 0;

                    // Рассчитываем процент сжатия
                    double compressionPercentage = ((double)(outputFileSize) / inputFileSize) * 100;

                    // Рассчитываем скорость кодирования
                    double speed = audioDurationInMilliSeconds / elapsedMilliseconds; // Во сколько раз кодирование быстрее

                    // Дописываем информацию в лог-файл
                    string logEntry = $"{outputFileSize} bytes ({compressionPercentage:F3}%)\t{elapsedMilliseconds} ms (x{speed:F3})\t-{compressionLevelValue}\t-j{threadsValue}\t{additionalArgumentsText}\tVersion: {flacVersion}\tEXE: {Path.GetFileName(selectedFlacFile)}\t{Path.GetFileName(inputFilePath)}"; // Изменение здесь
                    File.AppendAllText(LogFileName, logEntry.Trim() + Environment.NewLine); // Убедитесь, что здесь тоже используется Trim и новая строчка

                    // Обновляем текстовое поле с логами
                    UpdateLogTextBox(logEntry);

                    // Сбрасываем прогресс-бар
                    progressBar.Style = ProgressBarStyle.Blocks; // Выключаем стиль "маркировка"
                    progressBar.Value = 0; // Сбрасываем значение прогресс-бара
                }
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
            // Проверка и создание директорий
            CheckAndCreateDirectories();

            // Перезагружаем список .exe файлов
            LoadFlacExecutables();

            // Перезагружаем список аудиофайлов
            LoadAudioFiles(); // Добавляем загрузку аудио файлов

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

        private void checkBoxHighPriority_CheckedChanged(object sender, EventArgs e)
        {

        }

        private async void buttonStartDecode_Click(object sender, EventArgs e)
        {
            // Проверяем, выбран ли .exe файл из ListBox
            if (listBoxFlacExecutables.SelectedItem == null)
            {
                MessageBox.Show("Please select a FLAC executable file.");
                return;
            }

            // Полный путь к выбранному файлу .exe в папке flac_exe
            string selectedFlacFile = Path.Combine(Environment.CurrentDirectory, "flac_exe", listBoxFlacExecutables.SelectedItem.ToString());

            try
            {
                // Получаем текущую директорию приложения
                string currentDirectory = Environment.CurrentDirectory;

                // Получаем выбранные файлы из listBoxAudioFiles
                foreach (var item in listBoxAudioFiles.Items)
                {
                    string fileName = item.ToString(); // Получаем только имя файла

                    // Проверяем, является ли файл FLAC
                    if (!fileName.EndsWith(".flac", StringComparison.OrdinalIgnoreCase))
                    {
                        continue; // Пропускаем файлы других типов
                    }

                    string inputFilePath = Path.Combine(flacAudioDir, fileName); // Путь к FLAC
                    string outputFilePath = Path.Combine(flacAudioDir, Path.GetFileNameWithoutExtension(inputFilePath) + "_FLAC_Benchmark_H_output.wav");

                    // Обновляем версию FLAC перед началом процесса
                    string flacVersion = await Task.Run(() => GetFlacVersion(selectedFlacFile));
                    labelFlacUsedVersion.Text = "Using version: " + flacVersion;

                    // Обновляем интерфейс, чтобы отобразить новую версию
                    await Task.Delay(100); // Небольшая задержка для обновления интерфейса

                    // Проверяем существование входного файла
                    if (!File.Exists(inputFilePath))
                    {
                        MessageBox.Show($"There is no input file '{inputFilePath}'. Please ensure the file is present in the app directory.");
                        return;
                    }



                    // Создаем таймер
                    Stopwatch stopwatch = new Stopwatch();
                    stopwatch.Start();

                    ProcessStartInfo startInfo = new ProcessStartInfo()
                    {
                        FileName = selectedFlacFile, // Используем выбранный исполняемый файл FLAC
                        Arguments = $"-d -f \"{inputFilePath}\" -o \"{outputFilePath}\"", // Аргументы для декодирования
                        UseShellExecute = false,
                        CreateNoWindow = true // Скрываем консольное окно
                    };

                    // Запускаем процесс
                    using (Process process = new Process())
                    {
                        process.StartInfo = startInfo;
                        process.EnableRaisingEvents = true;

                        process.Start(); // Запускаем процесс

                        // Устанавливаем приоритет процесса на высокий, если чекбокс включен
                        if (checkBoxHighPriority.Checked)
                        {
                            process.PriorityClass = ProcessPriorityClass.High;
                        }
                        else
                        {
                            process.PriorityClass = ProcessPriorityClass.Normal; // Устанавливаем нормальный приоритет
                        }

                        // Ждем завершения процесса
                        await Task.Run(() => process.WaitForExit());

                        process.WaitForExit(); // Ждем завершения процесса

                        // Проверяем код завершения процесса
                        if (process.ExitCode != 0)
                        {
                            MessageBox.Show("Error during decoding", "Error");
                            return;
                        }
                    }

                    stopwatch.Stop(); // Останавливаем таймер

                    // Получаем время выполнения в миллисекундах
                    long elapsedMilliseconds = stopwatch.ElapsedMilliseconds;

                    // Получаем продолжительность аудиофайла
                    TimeSpan audioDuration = GetAudioDuration(inputFilePath);
                    double audioDurationInMilliSeconds = audioDuration.TotalMilliseconds; // Продолжительность в миллисекундах
                    // Получаем размер выходного файла в байтах
                    FileInfo outputFileInfo = new FileInfo(outputFilePath);
                    long outputFileSize = outputFileInfo.Exists ? outputFileInfo.Length : 0;

                    // Рассчитываем скорость декодирования
                    double speed = audioDurationInMilliSeconds / elapsedMilliseconds; // Во сколько раз декодирование быстрее

                    // Дописываем информацию в лог-файл
                    string logEntry = $"{outputFileSize} bytes\t{elapsedMilliseconds} ms (x{speed:F3})\tVersion: {flacVersion}\tInput: {Path.GetFileName(inputFilePath)}\tOutput: {Path.GetFileName(outputFilePath)}";
                    File.AppendAllText(LogFileName, logEntry.Trim() + Environment.NewLine); // Логируем информацию

                    // Обновляем текстовое поле с логами
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