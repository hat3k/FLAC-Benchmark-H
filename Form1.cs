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
        private Process _process; // Поле для хранения текущего процесса
        private const string SettingsFilePath = "Settings_general.txt"; // Путь к файлу настроек
        private const string JobsFilePath = "Settings_jobs.txt"; // Путь к файлу jobs
        private const string encodersFilePath = "Settings_flac_executables.txt"; // Путь к файлу для сохранения исполняемых файлов
        private const string audioFilesFilePath = "Settings_audio_files.txt"; // Путь к файлу для сохранения аудиофайлов
        private Stopwatch stopwatch;
        private PerformanceCounter cpuCounter;
        private System.Windows.Forms.Timer cpuUsageTimer; // Указываем явно, что это Timer из System.Windows.Forms
        private bool _isEncodingStopped = false;
        private bool isExecuting = false; // Флаг для отслеживания, выполняется ли процесс
        private bool _isPaused = false; // Флаг паузы
        private string tempFolderPath; // Поле для хранения пути к временной папке
        private bool isCpuInfoLoaded = false;
        public Form1()
        {
            InitializeComponent();
            InitializeDragAndDrop(); // Инициализация drag-and-drop
            this.FormClosing += Form1_FormClosing; // Регистрация обработчика события закрытия формы
            this.listViewEncoders.KeyDown += ListViewEncoders_KeyDown;
            this.listViewAudioFiles.KeyDown += ListViewAudioFiles_KeyDown;
            this.listViewJobs.KeyDown += ListViewJobs_KeyDown;
            this.textBoxCompressionLevel.KeyDown += new KeyEventHandler(this.textBoxCompressionLevel_KeyDown);
            this.textBoxThreads.KeyDown += new KeyEventHandler(this.textBoxThreads_KeyDown);
            this.textBoxCommandLineOptionsEncoder.KeyDown += new KeyEventHandler(this.textBoxCommandLineOptionsEncoder_KeyDown);
            this.textBoxCommandLineOptionsDecoder.KeyDown += new KeyEventHandler(this.textBoxCommandLineOptionsDecoder_KeyDown);
            LoadCPUInfoAsync(); // Загружаем информацию о процессоре
            stopwatch = new Stopwatch(); // Инициализация объекта Stopwatch
            cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            cpuUsageTimer = new System.Windows.Forms.Timer(); // Явно указываем System.Windows.Forms.Timer
            cpuUsageTimer.Interval = 250; // Каждые 250 мс
            cpuUsageTimer.Tick += async (sender, e) => await UpdateCpuUsageAsync();
            cpuUsageTimer.Start();
            InitializedataGridViewLog();

            tempFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp"); // Инициализация пути к временной папке
            _process = new Process(); // Initialize _process to avoid nullability warning

            dataGridViewLog.CellContentClick += dataGridViewLog_CellContentClick;
            buttonPauseResume.Click += buttonPauseResume_Click;


            // Включаем пользовательскую отрисовку для listViewJobs
            listViewJobs.OwnerDraw = true;
            listViewJobs.DrawColumnHeader += ListViewJobs_DrawColumnHeader;
            listViewJobs.DrawSubItem += ListViewJobs_DrawSubItem;
            comboBoxCPUPriority.SelectedIndex = 3;

        }
        private string NormalizeSpaces(string input)
        {
            return Regex.Replace(input.Trim(), @"\s+", " "); // Удаляем лишние пробелы внутри строки
        }

        // Метод для загрузки информации о процессоре
        private async void LoadCPUInfoAsync()
        {
            if (!isCpuInfoLoaded)
            {
                physicalCores = 0;
                threadCount = 0;

                // Создаем запрос для получения информации о процессорах
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

                // Обновляем метку с информацией о процессоре на UI потоке
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
            float cpuUsage = await Task.Run(() => cpuCounter.NextValue());
            labelCpuUsage.Text = $"CPU Usage: {cpuUsage:F2}%";
        }

        // Метод для сохранения настроек
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
                    $"ClearTempFolderOnExit={checkBoxClearTempFolder.Checked}"
                };
                File.WriteAllLines(SettingsFilePath, settings);
                SaveEncoders();
                SaveAudioFiles(); // Сохранение аудиофайлов
                SaveJobs(); // Сохраняем содержимое jobList
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving settings: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        // Метод для загрузки настроек
        private void LoadSettings()
        {
            // Загрузка пути к временной папке
            tempFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp");
            try
            {
                string[] lines = File.ReadAllLines(SettingsFilePath);
                foreach (var line in lines)
                {
                    var parts = line.Split(new[] { '=' }, 2); // Разделяем строку на ключ и значение, ограничиваем отделение по первому знаку '='
                    if (parts.Length == 2)
                    {
                        var key = parts[0].Trim();
                        var value = parts[1].Trim();
                        // Загружаем значения в соответствующие поля
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
                        }
                    }
                }
            }
            catch
            {
            }
            LoadEncoders(); // Загрузка исполняемых файлов
            LoadAudioFiles(); // Загрузка аудиофайлов
            LoadJobs(); // Загружаем содержимое Settings_joblist.txt
        }
        private void SaveEncoders()
        {
            try
            {
                var encoders = listViewEncoders.Items
                .Cast<ListViewItem>()
                .Select(item => $"{item.Tag}~{item.Checked}")
                .ToArray();
                File.WriteAllLines(encodersFilePath, encoders);
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
                File.WriteAllLines(audioFilesFilePath, audioFiles);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving audio files: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void InitializeDragAndDrop()
        {
            // Разрешаем перетаскивание файлов в ListView для программ
            listViewEncoders.AllowDrop = true;
            listViewEncoders.DragEnter += ListViewEncoders_DragEnter;
            listViewEncoders.DragDrop += ListViewEncoders_DragDrop;
            // Разрешаем перетаrскивание файлов в ListView для аудиофайлов
            listViewAudioFiles.AllowDrop = true;
            listViewAudioFiles.DragEnter += ListViewAudioFiles_DragEnter;
            listViewAudioFiles.DragDrop += ListViewAudioFiles_DragDrop;
            // Разрешаем перетаскивание файлов в ListView для очереди задач
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
                // Проверим, есть ли хотя бы один файл с расширением .exe
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
        private async void ListViewEncoders_DragDrop(object? sender, DragEventArgs e)
        {
            string[] files = (string[]?)e.Data?.GetData(DataFormats.FileDrop) ?? Array.Empty<string>();
            if (files.Length > 0)
            {
                var tasks = files.Select(async file =>
                {
                    if (Directory.Exists(file))
                    {
                        await AddEncoders(file); // Асинхронно добавляем исполняемые файлы
                    }
                    else if (Path.GetExtension(file).Equals(".exe", StringComparison.OrdinalIgnoreCase))
                    {
                        var item = await CreateEncoderListViewItem(file, true); // Создаем элемент списка
                        return item; // Возвращаем созданный элемент
                    }
                    return null; // Возвращаем null, если это не .exe файл
                });

                var items = await Task.WhenAll(tasks); // Ждем завершения всех задач

                // Добавляем элементы в ListView
                foreach (var item in items)
                {
                    if (item != null)
                    {
                        listViewEncoders.Items.Add(item); // Добавляем элемент в ListView
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
                    var tasks = openFileDialog.FileNames.Select(async file =>
                    {
                        var item = await CreateEncoderListViewItem(file, true); // Создание элемента списка
                        return item; // Возвращаем созданный элемент
                    });

                    var items = await Task.WhenAll(tasks); // Ждем завершения всех задач

                    foreach (var item in items)
                    {
                        if (item != null)
                        {
                            listViewEncoders.Items.Add(item); // Добавляем элементы в ListView
                        }
                    }
                }
            }
        }
        // Рекурсивный метод для добавления исполняемых файлов в ListView
        private async Task AddEncoders(string directory)
        {
            try
            {
                var exeFiles = Directory.GetFiles(directory, "*.exe", SearchOption.AllDirectories);
                var tasks = exeFiles.Select(async file =>
                {
                    var item = await CreateEncoderListViewItem(file, true); // Создаем элемент и возвращаем его
                    return item; // Возвращаем созданный элемент
                });

                var items = await Task.WhenAll(tasks); // Ждем завершения всех задач

                foreach (var item in items)
                {
                    if (item != null)
                    {
                        listViewEncoders.Items.Add(item); // Добавляем элементы в ListView
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error accessing directory: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        // Загрузка исполняемых файлов из файла txt
        private async void LoadEncoders()
        {
            if (File.Exists(encodersFilePath))
            {
                try
                {
                    string[] lines = await File.ReadAllLinesAsync(encodersFilePath);
                    listViewEncoders.Items.Clear(); // Очищаем ListView

                    var tasks = lines.Select(async line =>
                    {
                        var parts = line.Split('~');
                        if (parts.Length == 2)
                        {
                            string encoderPath = parts[0]; // Удаляем лишние пробелы
                            bool isChecked = bool.Parse(parts[1]); // Статус "выделено"
                            var item = await CreateEncoderListViewItem(encoderPath, isChecked); // Создаем элемент
                            return item; // Возвращаем созданный элемент
                        }
                        return null; // Возвращаем null, если не удалось создать элемент
                    });

                    var items = await Task.WhenAll(tasks); // Ожидаем завершения всех задач

                    // Добавляем только непустые элементы в ListView
                    foreach (var item in items)
                    {
                        if (item != null)
                        {
                            listViewEncoders.Items.Add(item); // Добавляем элемент в ListView
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading encoders: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
        private async Task<ListViewItem> CreateEncoderListViewItem(string encoderPath, bool isChecked)
        {
            if (!File.Exists(encoderPath))
            {
                return null; // Если файл не найден, возвращаем null
            }

            // Получаем информацию о кодере
            var encoderInfo = await GetEncoderInfo(encoderPath); // Асинхронно получаем информацию

            // Создаем элемент ListViewItem
            var item = new ListViewItem(Path.GetFileName(encoderPath))
            {
                Tag = encoderPath,
                Checked = isChecked
            };

            // Заполняем подэлементы
            item.SubItems.Add(encoderInfo.Version);
            item.SubItems.Add($"{encoderInfo.FileSize:n0} bytes");
            item.SubItems.Add(encoderInfo.LastModified.ToString("yyyy.MM.dd HH:mm"));

            return item;
        }

        private void buttonUpEncoder_Click(object? sender, EventArgs e)
        {
            MoveSelectedItems(listViewEncoders, -1); // Передаём -1 для перемещения вверх
        }
        private void buttonDownEncoder_Click(object? sender, EventArgs e)
        {
            MoveSelectedItems(listViewEncoders, 1); // Передаём 1 для перемещения вниз
        }
        private void buttonRemoveEncoder_Click(object? sender, EventArgs e)
        {
            // Удаляем выделенные элементы из listViewEncoders
            for (int i = listViewEncoders.Items.Count - 1; i >= 0; i--)
            {
                if (listViewEncoders.Items[i].Selected) // Проверяем, выделен ли элемент
                {
                    listViewEncoders.Items.RemoveAt(i); // Удаляем элемент
                }
            }
        }
        private void buttonClearEncoders_Click(object? sender, EventArgs e)
        {
            listViewEncoders.Items.Clear();
        }
        private async Task<EncoderInfo> GetEncoderInfo(string encoderPath)
        {
            // Проверка на наличие информации в кэше
            if (encoderInfoCache.TryGetValue(encoderPath, out var cachedInfo))
            {
                return cachedInfo; // Возвращаем кэшированную информацию
            }

            // Получаем размер файла и дату последнего изменения
            long fileSize = new FileInfo(encoderPath).Length;
            DateTime lastModified = new FileInfo(encoderPath).LastWriteTime;

            // Получаем имя файла и путь к директории
            string fileName = Path.GetFileName(encoderPath);
            string directoryPath = Path.GetDirectoryName(encoderPath);

            string version = "N/A"; // Значение по умолчанию для версии

            // Получаем информацию о версии кодера
            try
            {
                version = await Task.Run(() =>
                {
                    using (Process process = new Process())
                    {
                        process.StartInfo.FileName = encoderPath;
                        process.StartInfo.Arguments = "--version"; // Аргумент для получения версии
                        process.StartInfo.UseShellExecute = false;
                        process.StartInfo.RedirectStandardOutput = true; // Перенаправляем стандартный вывод
                        process.StartInfo.CreateNoWindow = true;

                        process.Start();
                        string result = process.StandardOutput.ReadLine(); // Читаем первую строку вывода
                        process.WaitForExit();

                        return result ?? "N/A"; // Возвращаем "N/A", если версия не найдена
                    }
                });
            }
            catch (Exception)
            {
                version = "N/A"; // Возвращаем "N/A" в случае ошибки
            }

            // Создаем объект EncoderInfo
            var encoderInfo = new EncoderInfo
            {
                EncoderPath = encoderPath,
                FileName = fileName,
                DirectoryPath = directoryPath,
                Version = version,
                FileSize = fileSize,
                LastModified = lastModified
            };

            // Добавляем новую информацию в кэш
            encoderInfoCache[encoderPath] = encoderInfo; // Кэшируем информацию
            return encoderInfo;
        }

        // Класс для хранения информации о кодере
        private class EncoderInfo
        {
            public string EncoderPath { get; set; }
            public string FileName { get; set; }
            public string DirectoryPath { get; set; }
            public string Version { get; set; }
            public long FileSize { get; set; }
            public DateTime LastModified { get; set; }
        }
        private ConcurrentDictionary<string, EncoderInfo> encoderInfoCache = new ConcurrentDictionary<string, EncoderInfo>();

        //Audio files
        private void ListViewAudioFiles_DragEnter(object? sender, DragEventArgs e)
        {
            if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
            {
                string[] files = (string[]?)e.Data.GetData(DataFormats.FileDrop) ?? Array.Empty<string>();
                // Проверим, есть ли хотя бы один аудиофайл
                bool hasAudioFiles = files.Any(file =>
                    Directory.Exists(file) ||
                    IsAudioFile(file)); // Используем функцию IsAudioFile
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
                        // Получаем все audio-файлы в директории
                        var directoryFiles = Directory.GetFiles(file, "*.wav", SearchOption.AllDirectories)
                            .Concat(Directory.GetFiles(file, "*.flac", SearchOption.AllDirectories));

                        // Создаем ListViewItem для каждого найденного аудиофайла
                        var items = await Task.WhenAll(directoryFiles.Select(f => Task.Run(() => CreateListViewItem(f))));
                        return items; // Возвращаем массив элементов ListViewItem
                    }
                    else if (IsAudioFile(file) && File.Exists(file))
                    {
                        var item = await Task.Run(() => CreateListViewItem(file)); // Создаем элемент списка
                        return new[] { item }; // Возвращаем массив с одним элементом
                    }

                    return Array.Empty<ListViewItem>(); // Возвращаем пустой массив, если это не аудиофайл
                });

                var itemsList = await Task.WhenAll(tasks); // Ждем завершения всех задач

                // Добавляем элементы в ListView
                foreach (var itemList in itemsList)
                {
                    if (itemList != null && itemList.Length > 0)
                    {
                        listViewAudioFiles.Items.AddRange(itemList); // Добавляем массив элементов в ListView
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
                        var item = await Task.Run(() => CreateListViewItem(file)); // Создание элемента списка
                        item.Checked = true; // Устанавливаем статус "выделено"
                        return item;
                    });

                    var items = await Task.WhenAll(tasks); // Ждем завершения всех задач

                    foreach (var item in items)
                    {
                        if (item != null)
                        {
                            listViewAudioFiles.Items.Add(item); // Добавляем элементы в ListView
                        }
                    }
                }
            }
        }
        private bool IsAudioFile(string file)
        {
            string extension = Path.GetExtension(file);
            return extension.Equals(".wav", StringComparison.OrdinalIgnoreCase) ||
                   extension.Equals(".flac", StringComparison.OrdinalIgnoreCase);
        }
        // Метод для загрузки аудиофайлов из файла txt
        private async void LoadAudioFiles()
        {
            if (File.Exists(audioFilesFilePath))
            {
                try
                {
                    // Читаем все строки из файла
                    string[] lines = await File.ReadAllLinesAsync(audioFilesFilePath);
                    listViewAudioFiles.Items.Clear(); // Очищаем ListView

                    var tasks = lines.Select(async line =>
                    {
                        var parts = line.Split('~');
                        if (parts.Length == 2)
                        {
                            string audioFilePath = parts[0]; // Удаляем лишние пробелы
                            bool isChecked = bool.Parse(parts[1]); // Читаем статус "выделено"

                            // Проверка на пустой путь
                            if (!string.IsNullOrEmpty(audioFilePath))
                            {
                                // Создание элемента ListViewItem
                                var item = await Task.Run(() => CreateListViewItem(audioFilePath));

                                // Проверка, что элемент не равен null
                                if (item != null)
                                {
                                    item.Checked = isChecked; // Устанавливаем статус чекбокса
                                    return item; // Возвращаем созданный элемент
                                }
                            }
                        }

                        return null; // Возвращаем null, если не удалось создать элемент
                    }).Where(item => item != null); // Фильтруем null

                    var items = await Task.WhenAll(tasks); // Ожидаем завершения всех задач

                    // Добавляем только непустые элементы в ListView
                    foreach (var item in items)
                    {
                        if (item != null && listViewAudioFiles != null) // Проверяем, что listViewAudioFiles не null
                        {
                            listViewAudioFiles.Items.Add(item); // Добавляем элемент в ListView
                        }
                    }
                }
                catch (Exception ex) // Обрабатываем исключения
                {
                    MessageBox.Show($"Error loading audio files: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
        // Метод для создания элемента ListViewItem
        private async Task<ListViewItem> CreateListViewItem(string audioFile)
        {
            if (!File.Exists(audioFile))
            {
                throw new FileNotFoundException("Audio file not found", audioFile);
            }

            // Используем метод GetAudioInfo для получения информации о файле
            var audioFileInfo = await GetAudioInfo(audioFile);

            // Создаем элемент ListViewItem
            var item = new ListViewItem(Path.GetFileName(audioFile))
            {
                Tag = audioFile,
                Checked = true
            };

            // Заполняем подэлементы
            item.SubItems.Add($"{audioFileInfo.Duration:n0} ms");
            item.SubItems.Add(audioFileInfo.BitDepth + " bit");
            item.SubItems.Add(audioFileInfo.SamplingRate);
            item.SubItems.Add($"{audioFileInfo.FileSize:n0} bytes");
            item.SubItems.Add(audioFileInfo.Md5Hash);
            item.SubItems.Add(audioFileInfo.DirectoryPath);
            return item;
        }
        // Метод для получения длительности и разрядности аудиофайла
        private async Task<AudioFileInfo> GetAudioInfo(string audioFile)
        {
            // Проверка на наличие информации в кэше
            if (audioInfoCache.TryGetValue(audioFile, out var cachedInfo))
            {
                return cachedInfo; // Возвращаем кэшированную информацию
            }

            var mediaInfo = new MediaInfoLib.MediaInfo();
            mediaInfo.Open(audioFile);

            string duration = mediaInfo.Get(StreamKind.Audio, 0, "Duration") ?? "N/A";
            string bitDepth = mediaInfo.Get(StreamKind.Audio, 0, "BitDepth") ?? "N/A";
            string samplingRate = mediaInfo.Get(StreamKind.Audio, 0, "SamplingRate/String") ?? "N/A";
            long fileSize = new FileInfo(audioFile).Length;
            string md5Hash = "N/A"; // Значение по умолчанию для MD5

            // Определяем тип файла и получаем соответствующий MD5
            if (Path.GetExtension(audioFile).Equals(".flac", StringComparison.OrdinalIgnoreCase))
            {
                md5Hash = mediaInfo.Get(StreamKind.Audio, 0, "MD5_Unencoded") ?? "N/A"; // Получаем MD5 для FLAC
            }
            else if (Path.GetExtension(audioFile).Equals(".wav", StringComparison.OrdinalIgnoreCase))
            {
                md5Hash = await CalculateWavMD5Async(audioFile); // Асинхронный метод для расчета MD5 для WAV
            }

            mediaInfo.Close();

            // Добавляем новую информацию в кэш
            var audioFileInfo = new AudioFileInfo
            {
                FilePath = audioFile,
                DirectoryPath = Path.GetDirectoryName(audioFile),
                FileName = Path.GetFileName(audioFile),
                Duration = duration,
                BitDepth = bitDepth,
                SamplingRate = samplingRate,
                FileSize = fileSize,
                Md5Hash = md5Hash
            };

            audioInfoCache[audioFile] = audioFileInfo; // Кэшируем информацию
            return audioFileInfo;
        }
        // Класс для хранения информации об аудиофайле
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
        private List<AudioFileInfo> audioFileInfoList = new List<AudioFileInfo>();
        private ConcurrentDictionary<string, AudioFileInfo> audioInfoCache = new ConcurrentDictionary<string, AudioFileInfo>();
        private async Task<string> CalculateWavMD5Async(string filePath)
        {
            using (var stream = File.OpenRead(filePath))
            {
                using (var md5 = MD5.Create())
                {
                    using (BinaryReader reader = new BinaryReader(stream))
                    {
                        // Проверяем заголовок RIFF
                        if (reader.ReadUInt32() != 0x46464952) // "RIFF"
                            return "Invalid WAV file";

                        reader.ReadUInt32(); // Читаем общий размер файла (не используется)

                        if (reader.ReadUInt32() != 0x45564157) // "WAVE"
                            return "Invalid WAV file";

                        // Читаем блоки
                        while (reader.BaseStream.Position < reader.BaseStream.Length)
                        {
                            uint chunkId = reader.ReadUInt32();
                            uint chunkSize = reader.ReadUInt32();

                            if (chunkId == 0x20746D66) // "fmt "
                            {
                                // Пропускаем блок "fmt "
                                reader.BaseStream.Seek(chunkSize, SeekOrigin.Current);
                            }
                            else if (chunkId == 0x61746164) // "data"
                            {
                                // Проверяем на допустимость размера
                                if (chunkSize < 0 || chunkSize > int.MaxValue)
                                {
                                    return "Invalid WAV file";
                                }

                                // Читаем аудиоданные из блока "data"
                                byte[] audioData = reader.ReadBytes((int)chunkSize);
                                return BitConverter.ToString(md5.ComputeHash(audioData)).Replace("-", "").ToUpperInvariant();
                            }
                            else
                            {
                                // Пропускаем блок
                                reader.BaseStream.Seek(chunkSize, SeekOrigin.Current);
                            }
                        }
                    }
                }
            }

            return "MD5 calculation failed"; // Если ничего не найдено
        }
        private string GetFileMD5Hash(string filePath, string existingMD5 = "N/A")
        {
            // Проверяем, есть ли существующий MD5 хеш, и если он не N/A, то возвращаем его
            if (!string.IsNullOrEmpty(existingMD5) && existingMD5 != "N/A")
            {
                return existingMD5;
            }

            if (filePath.EndsWith(".flac", StringComparison.OrdinalIgnoreCase))
            {
                // Используем metaflac для получения MD5
                using (var process = new Process())
                {
                    process.StartInfo.FileName = "metaflac.exe"; // Убедитесь, что путь корректен
                    process.StartInfo.Arguments = $"--show-md5sum \"{filePath}\""; // Аргумент для получения MD5
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.RedirectStandardOutput = true; // Редиректируем вывод
                    process.StartInfo.CreateNoWindow = true; // Скрываем окно консоли
                    process.Start();
                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();

                    // Обработка вывода для извлечения MD5
                    string[] lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    if (lines.Length > 0)
                    {
                        return lines[0].Split(' ')[0].Trim().ToUpperInvariant(); // Возвращаем только MD5 хеш в верхнем регистре
                    }
                }
                // Если MD5 не найден, возвращаем "N/A"
                return "N/A";
            }
            else if (filePath.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
            {
                using (var stream = File.OpenRead(filePath))
                {
                    using (var md5 = MD5.Create())
                    {
                        using (BinaryReader reader = new BinaryReader(stream))
                        {
                            // Проверяем заголовок RIFF
                            if (reader.ReadUInt32() != 0x46464952) // "RIFF"
                                return "Invalid WAV file";

                            reader.ReadUInt32(); // Читаем общий размер файла (не используется)
                            if (reader.ReadUInt32() != 0x45564157) // "WAVE"
                                return "Invalid WAV file";

                            // Читаем блоки
                            while (reader.BaseStream.Position < reader.BaseStream.Length)
                            {
                                uint chunkId = reader.ReadUInt32();
                                uint chunkSize = reader.ReadUInt32();

                                if (chunkId == 0x20746D66) // "fmt "
                                {
                                    // Пропускаем блок "fmt "
                                    reader.BaseStream.Seek(chunkSize, SeekOrigin.Current);
                                }
                                else if (chunkId == 0x61746164) // "data"
                                {
                                    // Проверяем на допустимость размера
                                    if (chunkSize < 0 || chunkSize > int.MaxValue)
                                    {
                                        return "Invalid WAV file";
                                    }

                                    // Читаем аудиоданные из блока "data"
                                    byte[] audioData = reader.ReadBytes((int)chunkSize);
                                    return BitConverter.ToString(md5.ComputeHash(audioData)).Replace("-", "").ToUpperInvariant();
                                }
                                else
                                {
                                    // Пропускаем блок
                                    reader.BaseStream.Seek(chunkSize, SeekOrigin.Current);
                                }
                            }
                        }
                    }
                }
            }
            return "N/A"; // Возвращаем "N/A" для других форматов
        }
        private async void buttonDetectDupesAudioFiles_Click(object? sender, EventArgs e)
        {
            var hashDict = new Dictionary<string, List<ListViewItem>>();

            var tasks = listViewAudioFiles.Items.Cast<ListViewItem>().Select(async item =>
            {
                string filePath = item.Tag.ToString(); // Получаем путь файла
                string md5Hash = item.SubItems[5].Text; // Пытаемся получить MD5 из подэлемента

                // Проверяем, если MD5 хеш отсутствует, вычисляем его
                if (string.IsNullOrEmpty(md5Hash) || md5Hash == "00000000000000000000000000000000" || md5Hash == "Invalid WAV file")
                {
                    md5Hash = await Task.Run(() => GetFileMD5Hash(filePath));
                    item.SubItems[5].Text = md5Hash; // Устанавливаем вычисленный MD5 в подэлемент
                }

                // Проверяем, является ли хеш валидным
                if (!string.IsNullOrEmpty(md5Hash) && md5Hash != "00000000000000000000000000000000" && md5Hash != "Invalid WAV file")
                {
                    if (hashDict.ContainsKey(md5Hash))
                    {
                        hashDict[md5Hash].Add(item);
                    }
                    else
                    {
                        hashDict[md5Hash] = new List<ListViewItem> { item }; // Создаем новый список дубликатов
                    }
                }
            });

            await Task.WhenAll(tasks); // Ждем завершения всех задач

            // Список дубликатов
            foreach (var kvp in hashDict)
            {
                if (kvp.Value.Count > 1)
                {
                    // Делаем только первый элемент выделенным, остальные - невыделенными
                    for (int i = 0; i < kvp.Value.Count; i++)
                    {
                        kvp.Value[i].Checked = (i == 0); // Отметьте только первый файл
                    }
                }
            }

            // Перемещаем дубликаты в верхнюю часть ListView
            foreach (var kvp in hashDict)
            {
                if (kvp.Value.Count > 1)
                {
                    foreach (var dupItem in kvp.Value)
                    {
                        listViewAudioFiles.Items.Remove(dupItem);
                        listViewAudioFiles.Items.Insert(0, dupItem); // Вставляем в начало списка
                    }
                }
            }
        }

        private void buttonUpAudioFile_Click(object? sender, EventArgs e)
        {
            MoveSelectedItems(listViewAudioFiles, -1); // Передаём -1 для перемещения вверх
        }
        private void buttonDownAudioFile_Click(object? sender, EventArgs e)
        {
            MoveSelectedItems(listViewAudioFiles, 1); // Передаём 1 для перемещения вниз
        }
        private void buttonRemoveAudiofile_Click(object? sender, EventArgs e)
        {
            // Удаляем выделенные элементы из listViewAudioFiles
            for (int i = listViewAudioFiles.Items.Count - 1; i >= 0; i--)
            {
                if (listViewAudioFiles.Items[i].Selected) // Проверяем, выделен ли элемент
                {
                    listViewAudioFiles.Items.RemoveAt(i); // Удаляем элемент
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
            // Настройка DataGridView
            dataGridViewLog.Columns.Add("Name", "Name");
            dataGridViewLog.Columns.Add("InputFileSize", "In. Size");
            dataGridViewLog.Columns.Add("OutputFileSize", "Out. Size");
            dataGridViewLog.Columns.Add("Compression", "Compr.");
            dataGridViewLog.Columns.Add("Time", "Time");
            dataGridViewLog.Columns.Add("Speed", "Speed");
            dataGridViewLog.Columns.Add("Parameters", "Parameters");
            dataGridViewLog.Columns.Add("Encoder", "Binary");
            dataGridViewLog.Columns.Add("Version", "Version");
            dataGridViewLog.Columns.Add("BestSize", "Best Size");
            dataGridViewLog.Columns.Add("SameSize", "Same Size");
            var filePathColumn = new DataGridViewLinkColumn
            {
                Name = "FilePath",
                HeaderText = "File Path",
                DataPropertyName = "FilePath" // Связываем с колонкой FilePath
            };
            dataGridViewLog.Columns.Add(filePathColumn);

            // Установка выравнивания для колонок
            dataGridViewLog.Columns["InputFileSize"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewLog.Columns["OutputFileSize"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewLog.Columns["Compression"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewLog.Columns["Time"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewLog.Columns["Speed"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

            // Скрываем столбец с полным путем
            //dataGridViewLog.Columns["FilePath"].Visible = false;
        }
        private void dataGridViewLog_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            // Проверяем, что кликнули по ячейке в колонке "FilePath"
            if (e.ColumnIndex == dataGridViewLog.Columns["FilePath"].Index && e.RowIndex >= 0)
            {
                // Получаем полный путь к директории
                string directoryPath = dataGridViewLog.Rows[e.RowIndex].Cells["FilePath"].Value?.ToString();
                // Получаем название файла
                string fileName = dataGridViewLog.Rows[e.RowIndex].Cells["Name"].Value?.ToString(); // Предполагается, что в этой ячейке хранится имя выходного файла

                // Проверяем, что оба значения не пустые
                if (!string.IsNullOrEmpty(directoryPath) && !string.IsNullOrEmpty(fileName))
                {
                    // Формируем полный путь к файлу
                    string fullPath = Path.Combine(directoryPath, fileName);

                    // Открываем проводник с указанным путем и выделяем файл
                    System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{fullPath}\"");
                }
            }
        }

        private async Task LogProcessResults(string outputFilePath, string audioFile, string parameters, string encoder)
        {
            FileInfo outputFile = new FileInfo(outputFilePath);
            if (outputFile.Exists)
            {
                // Создаем CultureInfo для форматирования с точками как разделителями разрядов
                NumberFormatInfo numberFormat = new CultureInfo("en-US").NumberFormat;
                numberFormat.NumberGroupSeparator = " ";

                // Получаем информацию о входящем аудиофайле из кэша
                var audioFileInfo = await GetAudioInfo(audioFile);

                // Извлекаем данные из кэша
                string audioFileName = audioFileInfo.FileName; // Используем имя файла из кэша
                long inputSize = audioFileInfo.FileSize; // Получаем размер из информации о файле
                string inputSizeFormatted = inputSize.ToString("N0", numberFormat);
                long durationMs = Convert.ToInt64(audioFileInfo.Duration); // Используем длительность из кэша
                string audioFileDirectory = audioFileInfo.DirectoryPath;

                // Формируем короткое имя входящего файла
                //string audioFileNameShort = audioFileName.Length > 30
                //    ? $"{audioFileName.Substring(0, 15)}...{audioFileName.Substring(audioFileName.Length - 15)}"
                //    : audioFileName.PadRight(33);

                // Получаем информацию о выходящем аудиофайле
                long outputSize = outputFile.Length;
                string outputSizeFormatted = outputSize.ToString("N0", numberFormat);
                TimeSpan timeTaken = stopwatch.Elapsed;
                double compressionPercentage = ((double)outputSize / inputSize) * 100;
                double encodingSpeed = (double)durationMs / timeTaken.TotalMilliseconds;

                // Получаем информацию о кодере из кэша
                // Здесь мы вызываем GetEncoderInfo, но с проверкой на кэш
                var encoderInfo = await GetEncoderInfo(encoder); // Получаем информацию об енкодере

                // Добавление записи в лог DataGridView
                int rowIndex = dataGridViewLog.Rows.Add(
                    audioFileName,             // 0
                    inputSizeFormatted,        // 1
                    outputSizeFormatted,       // 2
                    $"{compressionPercentage:F3}%", // 3
                    $"{timeTaken.TotalMilliseconds:F3}", // 4
                    $"{encodingSpeed:F3}x",    // 5
                    parameters,                // 6
                    encoderInfo.FileName,      // 7 (Имя файла кодера из кэша)
                    encoderInfo.Version,       // 8 (Версия кодера из кэша)
                    string.Empty,              // 9 (BestSize)
                    string.Empty,              // 10 (SameSize)
                    audioFileDirectory         // 11 (FilePath)
                );

                // Установка цвета текста в зависимости от сравнения размеров файлов
                dataGridViewLog.Rows[rowIndex].Cells[2].Style.ForeColor = outputSize < inputSize ? System.Drawing.Color.Green :
                    outputSize > inputSize ? System.Drawing.Color.Red : dataGridViewLog.Rows[rowIndex].Cells[2].Style.ForeColor;

                dataGridViewLog.Rows[rowIndex].Cells[3].Style.ForeColor = compressionPercentage < 100 ? System.Drawing.Color.Green :
                    compressionPercentage > 100 ? System.Drawing.Color.Red : dataGridViewLog.Rows[rowIndex].Cells[3].Style.ForeColor;

                dataGridViewLog.Rows[rowIndex].Cells[5].Style.ForeColor = encodingSpeed > 1 ? System.Drawing.Color.Green :
                    encodingSpeed < 1 ? System.Drawing.Color.Red : dataGridViewLog.Rows[rowIndex].Cells[5].Style.ForeColor;

                // Прокручиваем DataGridView вниз к последней добавленной строке
                //dataGridViewLog.FirstDisplayedScrollingRowIndex = dataGridViewLog.Rows.Count - 1;

                // Логирование в файл
                File.AppendAllText("log.txt", $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {audioFileName}\tInput size: {inputSize}\tOutput size: {outputSize} bytes\tCompression: {compressionPercentage:F3}%\tTime: {timeTaken.TotalMilliseconds:F3} ms\tSpeed: {encodingSpeed:F3}x\tParameters: {parameters.Trim()}\tBinary: {encoderInfo.FileName}\tVersion: {encoderInfo.Version}{Environment.NewLine}");
            }
        }

        private void buttonAnalyzeLog_Click(object? sender, EventArgs e)
        {
            AnalyzeBestSize(); // Запускаем анализ при нажатии кнопки
        }
        private async void AnalyzeBestSize()
        {
            var dataRows = new List<(string fileName, long outputSize, int rowIndex)>();
            int rowCount = dataGridViewLog.Rows.Count;

            // Получаем данные в основном потоке
            for (int i = 0; i < rowCount; i++)
            {
                var row = dataGridViewLog.Rows[i];
                if (row.Cells["Name"].Value is string fileName &&
                    row.Cells["OutputFileSize"].Value is string outputSizeStr &&
                    long.TryParse(outputSizeStr.Replace(" ", "").Trim(), out long outputSize))
                {
                    dataRows.Add((fileName, outputSize, i));
                }
            }

            // Группируем выходные размеры
            var outputSizeGroups = dataRows
                .GroupBy(dataRow => dataRow.fileName)
                .ToDictionary(g => g.Key, g => g.Select(x => x.outputSize).ToList());

            // Находим минимальные размеры
            var smallestSizes = new ConcurrentDictionary<string, (long minSize, int count)>();

            // Обрабатываем данные параллельно
            Parallel.ForEach(dataRows, dataRow =>
            {
                var (fileName, outputSize, rowIndex) = dataRow;

                smallestSizes.AddOrUpdate(
                    fileName,
                    (outputSize, 1),
                    (key, existingValue) =>
                    {
                        var (minSize, count) = existingValue;
                        if (outputSize < minSize)
                        {
                            return (outputSize, 1);
                        }
                        else if (outputSize == minSize)
                        {
                            return (minSize, count + 1);
                        }
                        return existingValue;
                    }
                );
            });

            // Обновляем интерфейс после обработки данных
            await Task.Run(() =>
            {
                this.Invoke((Action)(() =>
                {
                    foreach (var fileEntry in smallestSizes)
                    {
                        string fileName = fileEntry.Key;
                        long smallestSize = fileEntry.Value.minSize;

                        var indices = dataRows.Where(x => x.fileName == fileName).Select(x => x.rowIndex).ToList();

                        foreach (int index in indices)
                        {
                            var objRow = dataGridViewLog.Rows[index];
                            long rowOutputSize = dataRows[index].outputSize;

                            // Обновляем столбец BestSize
                            objRow.Cells["BestSize"].Value = (rowOutputSize == smallestSize) ? "smallest size" : string.Empty;
                        }

                        // Проверка на одинаковые размеры
                        bool hasSameSize = outputSizeGroups[fileName].Distinct().Count() < outputSizeGroups[fileName].Count;
                        foreach (int index in indices)
                        {
                            var objRow = dataGridViewLog.Rows[index];
                            objRow.Cells["SameSize"].Value = (hasSameSize && outputSizeGroups[fileName].Count(g => g == dataRows[index].outputSize) > 1) ? "has same size" : string.Empty;
                        }
                    }
                }));
            });
            SortDataGridView();
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
            public string BestSize { get; set; }
            public string SameSize { get; set; }
            public string FilePath { get; set; }

            public Color OutputForeColor { get; set; } // Цвет для OutputFileSize
            public Color CompressionForeColor { get; set; } // Цвет для Compression
            public Color SpeedForeColor { get; set; } // Цвет для Speed
        }
        private void SortDataGridView()
        {
            // Собираем данные из DataGridView в список
            var dataToSort = new List<LogEntry>();
            foreach (DataGridViewRow row in dataGridViewLog.Rows)
            {
                if (row.IsNewRow) continue; // Пропускаем новую строку

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
                    BestSize = row.Cells["BestSize"].Value?.ToString(),
                    SameSize = row.Cells["SameSize"].Value?.ToString(),
                    FilePath = row.Cells["FilePath"].Value?.ToString(),

                    OutputForeColor = row.Cells[2].Style.ForeColor, // Цвет для OutputFileSize
                    CompressionForeColor = row.Cells[3].Style.ForeColor, // Цвет для Compression
                    SpeedForeColor = row.Cells[5].Style.ForeColor // Цвет для Speed
                };

                dataToSort.Add(logEntry);
            }

            // Выполняем многоуровневую сортировку
            var sortedData = dataToSort
                .OrderBy(x => x.FilePath)
                .ThenBy(x => x.Name)
                .ThenBy(x => x.Parameters)
                .ThenBy(x => x.Encoder)
                .ToList();

            // Очищаем DataGridView и добавляем отсортированные данные
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
                    data.BestSize,
                    data.SameSize,
                    data.FilePath);


                // Установка цвета текста
                dataGridViewLog.Rows[rowIndex].Cells[2].Style.ForeColor = data.OutputForeColor; // OutputFileSize
                dataGridViewLog.Rows[rowIndex].Cells[3].Style.ForeColor = data.CompressionForeColor; // Compression
                dataGridViewLog.Rows[rowIndex].Cells[5].Style.ForeColor = data.SpeedForeColor; // Speed
            }
            dataGridViewLog.ClearSelection();
        }
        private void buttonLogToExcel_Click(object? sender, EventArgs e)
        {
            // Создаем новый Excel файл
            using (var workbook = new XLWorkbook())
            {
                // Добавляем новый лист
                var worksheet = workbook.Worksheets.Add("Log Data");

                // Добавляем заголовки колонок
                int columnCount = dataGridViewLog.Columns.Count;
                for (int i = 0; i < columnCount; i++)
                {
                    worksheet.Cell(1, i + 1).Value = dataGridViewLog.Columns[i].HeaderText;
                    worksheet.Cell(1, i + 1).Style.Font.Bold = true; // Устанавливаем жирный шрифт для заголовков
                }

                // Добавляем строки данных
                for (int i = 0; i < dataGridViewLog.Rows.Count; i++)
                {
                    for (int j = 0; j < columnCount; j++)
                    {
                        var cellValue = dataGridViewLog.Rows[i].Cells[j].Value;

                        // Записываем значения для размеров файлов
                        if (j == dataGridViewLog.Columns["InputFileSize"].Index || j == dataGridViewLog.Columns["OutputFileSize"].Index)
                        {
                            if (cellValue != null && long.TryParse(cellValue.ToString().Replace(" ", ""), out long numericValue))
                            {
                                worksheet.Cell(i + 2, j + 1).Value = numericValue; // Записываем как число

                            }
                        }
                        else if (j == dataGridViewLog.Columns["Compression"].Index)
                        {
                            if (cellValue != null && double.TryParse(cellValue.ToString().Replace("%", "").Trim(), out double compressionValue))
                            {
                                worksheet.Cell(i + 2, j + 1).Value = compressionValue / 100; // Записываем значение в диапазоне от 0 до 1
                            }
                        }
                        else if (j == dataGridViewLog.Columns["Time"].Index) // Обработка столбца Time
                        {
                            if (cellValue != null && double.TryParse(cellValue.ToString(), out double timeSpanValue))
                            {
                                worksheet.Cell(i + 2, j + 1).Value = timeSpanValue; // Записываем общее количество секунд
                            }
                        }
                        else if (j == dataGridViewLog.Columns["Speed"].Index)
                        {
                            if (cellValue != null && double.TryParse(cellValue.ToString().Replace("x", "").Trim(), out double speedValue))
                            {
                                worksheet.Cell(i + 2, j + 1).Value = speedValue; // Записываем значение скорости
                            }
                        }
                        else if (j == dataGridViewLog.Columns["Parameters"].Index) // Обработка столбца Parameters
                        {
                            worksheet.Cell(i + 2, j + 1).Value = cellValue?.ToString() ?? string.Empty; // Записываем значение как текст
                        }
                        else
                        {
                            worksheet.Cell(i + 2, j + 1).Value = cellValue?.ToString() ?? string.Empty; // Записываем значение как строку
                        }
                        // Копируем цвет текста, если он установлен
                        if (dataGridViewLog.Rows[i].Cells[j].Style.ForeColor != Color.Empty)
                        {
                            var color = dataGridViewLog.Rows[i].Cells[j].Style.ForeColor;
                            worksheet.Cell(i + 2, j + 1).Style.Font.FontColor = XLColor.FromArgb(color.A, color.R, color.G, color.B);
                        }
                    }
                }

                // Установка формата с разделителем разрядов для столбцов с размерами файлов
                int inputFileSizeIndex = dataGridViewLog.Columns["InputFileSize"].Index + 1; // +1 для 1-основанных индексов
                worksheet.Column(inputFileSizeIndex).Style.NumberFormat.Format = "#,##0"; // Формат целого числа с разделителями

                int outputFileSizeIndex = dataGridViewLog.Columns["OutputFileSize"].Index + 1; // +1 для 1-основанных индексов
                worksheet.Column(outputFileSizeIndex).Style.NumberFormat.Format = "#,##0"; // Формат целого числа с разделителями

                // Установка формата для столбца Compression как процент
                int compressionIndex = dataGridViewLog.Columns["Compression"].Index + 1; // +1 для 1-основанных индексов
                worksheet.Column(compressionIndex).Style.NumberFormat.Format = "0.000%"; // Формат числа с 3 знаками после запятой

                // Установка формата для столбца Time
                int timeIndex = dataGridViewLog.Columns["Time"].Index + 1; // +1 для 1-основанных индексов
                worksheet.Column(timeIndex).Style.NumberFormat.Format = "0.000"; // Формат для отображения времени

                // Установка формата для столбца Speed
                int speedIndex = dataGridViewLog.Columns["Speed"].Index + 1; // +1 для 1-основанных индексов
                worksheet.Column(speedIndex).Style.NumberFormat.Format = "0.000"; // Формат для отображения скорости

                // Установка формата для столбца Parameters
                int ParametersIndex = dataGridViewLog.Columns["Parameters"].Index + 1; // +1 для 1-основанных индексов
                worksheet.Column(ParametersIndex).Style.NumberFormat.Format = "@"; // Формат для отображения параметров

                // Установка фильтра на заголовки
                worksheet.RangeUsed().SetAutoFilter();

                // Замораживаем первую строку (заголовки)
                worksheet.SheetView.FreezeRows(1);

                // Настройка ширины столбцов на авто
                worksheet.Columns().AdjustToContents();

                // Задаем цвет заливки для первого ряда
                worksheet.Row(1).Style.Fill.SetBackgroundColor(XLColor.FromHtml("4F81BD"));
                worksheet.Row(1).Style.Font.FontColor = XLColor.White; // Устанавливаем цвет шрифта в белый для контраста

                // Формируем имя файла на основе текущей даты и времени
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss");
                string fileName = $"Log {timestamp}.xlsx";

                // Получаем путь к папке, где находится исполняемый файл
                string folderPath = AppDomain.CurrentDomain.BaseDirectory;
                string fullPath = Path.Combine(folderPath, fileName);

                // Сохраняем файл
                workbook.SaveAs(fullPath);

                // Открываем файл по умолчанию
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
            // Путь к файлу логирования
            string logFilePath = "log.txt";
            // Проверяем существует ли файл
            if (File.Exists(logFilePath))
            {
                try
                {
                    // Открываем log.txt с помощью стандартного текстового редактора
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = logFilePath,
                        UseShellExecute = true // Это откроет файл с помощью ассоциированного приложения
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
            // Создаем StringBuilder для сбора текста логов
            StringBuilder logText = new StringBuilder();
            // Проходим по строкам в DataGridView и собираем текст
            foreach (DataGridViewRow row in dataGridViewLog.Rows)
            {
                // Предполагаем, что вы хотите собирать текст из всех ячеек строки
                foreach (DataGridViewCell cell in row.Cells)
                {
                    logText.Append(cell.Value?.ToString() + "\t"); // Используем табуляцию для разделения ячеек
                }
                logText.AppendLine(); // Переход на новую строку после каждой строки DataGridView
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

        // Действия клавиш
        private void ListViewEncoders_KeyDown(object? sender, KeyEventArgs e)
        {
            // Проверяем, нажата ли клавиша Delete
            if (e.KeyCode == Keys.Delete)
            {
                buttonRemoveEncoder.PerformClick();
            }

            // Проверяем, нажаты ли Ctrl и A одновременно
            if (e.Control && e.KeyCode == Keys.A)
            {
                e.Handled = true; // Отменяем стандартное поведение

                // Выделяем все элементы
                foreach (ListViewItem item in listViewEncoders.Items)
                {
                    item.Selected = true; // Устанавливаем выделение для каждого элемента
                }
            }
        }
        private void ListViewAudioFiles_KeyDown(object? sender, KeyEventArgs e)
        {
            // Проверяем, нажата ли клавиша Delete
            if (e.KeyCode == Keys.Delete)
            {
                buttonRemoveAudiofile.PerformClick();
            }

            // Проверяем, нажаты ли Ctrl и A одновременно
            if (e.Control && e.KeyCode == Keys.A)
            {
                e.Handled = true; // Отменяем стандартное поведение

                // Выделяем все элементы
                foreach (ListViewItem item in listViewAudioFiles.Items)
                {
                    item.Selected = true; // Устанавливаем выделение для каждого элемента
                }
            }
        }
        private void ListViewJobs_KeyDown(object? sender, KeyEventArgs e)
        {
            // Проверяем, нажата ли клавиша Delete
            if (e.KeyCode == Keys.Delete)
            {
                buttonRemoveJob.PerformClick();
            }

            // Проверяем, нажаты ли Ctrl и A одновременно
            if (e.Control && e.KeyCode == Keys.A)
            {
                e.Handled = true; // Отменяем стандартное поведение

                // Выделяем все элементы
                foreach (ListViewItem item in listViewJobs.Items)
                {
                    item.Selected = true; // Устанавливаем выделение для каждого элемента
                }
            }

            // Обработка Ctrl+C (Копирование)
            if (e.Control && e.KeyCode == Keys.C)
            {
                buttonCopyJobs.PerformClick();
                e.Handled = true; // Отменяем стандартное поведение
            }

            // Обработка Ctrl+V (Вставка)
            if (e.Control && e.KeyCode == Keys.V)
            {
                buttonPasteJobs.PerformClick();
                e.Handled = true; // Отменяем стандартное поведение
            }
        }

        // Jobs
        private void ListViewJobs_DrawColumnHeader(object? sender, DrawListViewColumnHeaderEventArgs e)
        {
            e.DrawDefault = true;
        }
        private void ListViewJobs_DrawSubItem(object? sender, DrawListViewSubItemEventArgs e)
        {
            if (e.ColumnIndex == 0) // Колонка с типом задачи (Encode/Decode)
            {
                e.DrawBackground();
                // Отрисовка чекбокса
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
                    // Смещаем текст вправо, чтобы не перекрывать чекбокс
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
                // Проверяем наличие .txt или .bak файлов или директорий
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
                    LoadJobsFromFile(file); // Загружаем задачи из файла
                }
            }
        }
        private async void AddJobsFromDirectory(string directory)
        {
            try
            {
                // Ищем все .txt и .bak файлы в текущей директории
                var txtFiles = await Task.Run(() => Directory.GetFiles(directory, "*.txt", SearchOption.AllDirectories));
                var bakFiles = await Task.Run(() => Directory.GetFiles(directory, "*.bak", SearchOption.AllDirectories));

                // Объединяем массивы файлов
                var allFiles = txtFiles.Concat(bakFiles);

                foreach (var file in allFiles)
                {
                    LoadJobsFromFile(file); // Загружаем задачи из найденного файла
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
        private void LoadJobs()
        {
            BackupJobsFile();
            if (File.Exists(JobsFilePath))
            {
                try
                {
                    string[] lines = File.ReadAllLines(JobsFilePath);
                    listViewJobs.Items.Clear(); // Очищаем список перед загрузкой

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
        private void AddJobsToListView(string job, bool isChecked = true, string passes = "", string parameters = "")
        {
            var item = new ListViewItem(job) { Checked = isChecked };
            item.SubItems.Add(passes); // Добавляем количество проходов
            item.SubItems.Add(parameters); // Добавляем параметры
            listViewJobs.Items.Add(item); // Добавляем элемент в ListView
        }
        private void BackupJobsFile()
        {
            try
            {
                if (File.Exists(JobsFilePath))
                {
                    string backupPath = $"{JobsFilePath}.bak";
                    File.Copy(JobsFilePath, backupPath, true); // Копируем файл, если такой уже существует, перезаписываем
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating backup for jobs file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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
                        foreach (string fileName in openFileDialog.FileNames) // Обрабатываем каждый выбранный файл
                        {
                            string[] lines = await Task.Run(() => File.ReadAllLines(fileName));
                            foreach (var line in lines)
                            {
                                // Нормализуем строку
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
            MoveSelectedItems(listViewJobs, -1); // Передаём -1 для перемещения вверх
        }
        private void buttonDownJob_Click(object? sender, EventArgs e)
        {
            MoveSelectedItems(listViewJobs, 1); // Передаём 1 для перемещения вниз
        }
        private void buttonRemoveJob_Click(object? sender, EventArgs e)
        {
            // Удаляем выделенные элементы из listViewJobs
            for (int i = listViewJobs.Items.Count - 1; i >= 0; i--)
            {
                if (listViewJobs.Items[i].Selected) // Проверяем, выделен ли элемент
                {
                    listViewJobs.Items.RemoveAt(i); // Удаляем элемент
                }
            }
        }
        private void buttonClearJobList_Click(object? sender, EventArgs e)
        {
            listViewJobs.Items.Clear(); // Очищаем listViewJobs
        }
        private void buttonAddJobToJobListEncoder_Click(object? sender, EventArgs e)
        {
            // Получаем значения из текстовых полей и формируем параметры
            string compressionLevel = NormalizeSpaces(textBoxCompressionLevel.Text);
            string threads = NormalizeSpaces(textBoxThreads.Text);
            string commandLine = NormalizeSpaces(textBoxCommandLineOptionsEncoder.Text);

            // Формируем строку с параметрами
            string parameters = $"-{compressionLevel} {commandLine}".Trim();

            // Добавляем количество потоков, если оно больше 1
            if (int.TryParse(threads, out int threadCount) && threadCount > 1)
            {
                parameters += $" -j{threads}"; // добавляем флаг -j{threads}
            }

            // Проверяем, существует ли уже задача в последнем элементе
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

            // Если такая задача уже существует, увеличиваем количество проходов
            if (existingItem != null)
            {
                int currentPasses = int.Parse(existingItem.SubItems[1].Text);
                existingItem.SubItems[1].Text = (currentPasses + 1).ToString();
            }
            else
            {
                // Если задачи ещё не существует, добавляем новую с 1 проходом
                var newItem = new ListViewItem(jobName) { Checked = true };
                newItem.SubItems.Add("1"); // Устанавливаем количество проходов по умолчанию
                newItem.SubItems.Add(parameters);
                listViewJobs.Items.Add(newItem);
            }
        }
        private void buttonAddJobToJobListDecoder_Click(object? sender, EventArgs e)
        {
            // Получаем значения из текстовых полей и формируем параметры
            string commandLine = NormalizeSpaces(textBoxCommandLineOptionsDecoder.Text);
            string parameters = commandLine; // Параметры для декодирования

            // Проверяем, существует ли уже задача в последнем элементе
            string jobName = "Decode";
            ListViewItem existingItem = null;

            if (listViewJobs.Items.Count > 0)
            {
                ListViewItem lastItem = listViewJobs.Items[listViewJobs.Items.Count - 1];
                if (lastItem.Text == jobName && lastItem.SubItems[2].Text == commandLine)
                {
                    existingItem = lastItem;
                }
            }

            // Если такая задача уже существует, увеличиваем количество проходов
            if (existingItem != null)
            {
                int currentPasses = int.Parse(existingItem.SubItems[1].Text);
                existingItem.SubItems[1].Text = (currentPasses + 1).ToString();
            }
            else
            {
                // Если задачи ещё не существует, добавляем новую с 1 проходом
                var newItem = new ListViewItem(jobName) { Checked = true };
                newItem.SubItems.Add("1"); // Устанавливаем количество проходов по умолчанию
                newItem.SubItems.Add(parameters);
                listViewJobs.Items.Add(newItem);
            }
        }
        private void buttonPlusPass_Click(object? sender, EventArgs e)
        {
            listViewJobs.BeginUpdate(); // Отключаем перерисовку

            try
            {
                foreach (ListViewItem item in listViewJobs.SelectedItems)
                {
                    int currentPasses = int.Parse(item.SubItems[1].Text); // Получаем текущее значение
                    currentPasses++; // Увеличиваем на 1
                    item.SubItems[1].Text = currentPasses.ToString(); // Обновляем значение в ячейке
                }
            }
            finally
            {
                listViewJobs.EndUpdate(); // Включаем перерисовку
            }
        }
        private void buttonMinusPass_Click(object? sender, EventArgs e)
        {
            listViewJobs.BeginUpdate(); // Отключаем перерисовку

            try
            {
                foreach (ListViewItem item in listViewJobs.SelectedItems)
                {
                    int currentPasses = int.Parse(item.SubItems[1].Text); // Получаем текущее значение
                    if (currentPasses > 1) // Убеждаемся, что значение больше 1
                    {
                        currentPasses--; // Уменьшаем на 1
                        item.SubItems[1].Text = currentPasses.ToString(); // Обновляем значение в ячейке
                    }
                }
            }
            finally
            {
                listViewJobs.EndUpdate(); // Включаем перерисовку
            }
        }
        private void buttonCopyJobs_Click(object? sender, EventArgs e)
        {
            StringBuilder jobsText = new StringBuilder();

            // Проверяем, есть ли выделенные элементы
            var itemsToCopy = listViewJobs.SelectedItems.Count > 0
                ? listViewJobs.SelectedItems.Cast<ListViewItem>()
                : listViewJobs.Items.Cast<ListViewItem>();

            foreach (var item in itemsToCopy)
            {
                jobsText.AppendLine($"{NormalizeSpaces(item.Text)}~{item.Checked}~{NormalizeSpaces(item.SubItems[1].Text)}~{NormalizeSpaces(item.SubItems[2].Text)}");
            }

            // Копируем текст в буфер обмена
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
                // Получаем текст из буфера обмена
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
        private void SaveJobs()
        {
            try
            {
                var jobList = listViewJobs.Items.Cast<ListViewItem>()
                .Select(item => $"{item.Text}~{item.Checked}~{item.SubItems[1].Text}~{item.SubItems[2].Text}") // Сохраняем текст, состояние чекбокса, количество проходов и параметры
                .ToArray();
                File.WriteAllLines(JobsFilePath, jobList);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving jobs to file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private async void buttonStartJobList_Click(object? sender, EventArgs e)
        {
            dataGridViewLog.ClearSelection(); // Очищаем выделение

            if (isExecuting) return; // Проверяем, выполняется ли уже процесс
            isExecuting = true; // Устанавливаем флаг выполнения
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
                // Получаем выделенные энкодеры
                var selectedEncoders = listViewEncoders.Items.Cast<ListViewItem>()
                    .Where(item => item.Checked)
                    .Select(item => item.Tag.ToString()) // Получаем полный путь из Tag
                    .ToList();

                // Получаем все выделенные аудиофайлы .wav и .flac
                var selectedAudioFiles = listViewAudioFiles.Items.Cast<ListViewItem>()
                    .Where(item => item.Checked &&
                        (Path.GetExtension(item.Tag.ToString()).Equals(".wav", StringComparison.OrdinalIgnoreCase) ||
                         Path.GetExtension(item.Tag.ToString()).Equals(".flac", StringComparison.OrdinalIgnoreCase)))
                    .Select(item => item.Tag.ToString()) // Получаем полный путь из Tag
                    .ToList();

                // Получаем все выделенные аудиофайлы .flac
                var selectedFlacAudioFiles = selectedAudioFiles
                    .Where(file => Path.GetExtension(file).Equals(".flac", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                // Считаем количество задач и проходов по Encode
                int totalEncodeTasks = listViewJobs.Items
                    .Cast<ListViewItem>()
                    .Where(item => item.Checked && string.Equals(NormalizeSpaces(item.Text), "Encode", StringComparison.OrdinalIgnoreCase))
                    .Sum(item => int.Parse(item.SubItems[1].Text.Trim()));

                // Считаем количество задач и проходов по Decode
                int totalDecodeTasks = listViewJobs.Items
                    .Cast<ListViewItem>()
                    .Where(item => item.Checked && string.Equals(NormalizeSpaces(item.Text), "Decode", StringComparison.OrdinalIgnoreCase))
                    .Sum(item => int.Parse(item.SubItems[1].Text.Trim()));

                // 1. Проверяем, есть ли хотя бы один энкодер
                if (selectedEncoders.Count == 0)
                {
                    MessageBox.Show("Select at least one encoder.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    isExecuting = false; // Сбрасываем флаг перед возвратом
                    return;
                }

                // 2. Проверяем, есть ли хотя бы одна задача (Encode или Decode)
                if (totalEncodeTasks == 0 && totalDecodeTasks == 0)
                {
                    MessageBox.Show("Select at least one job.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    isExecuting = false; // Сбрасываем флаг перед возвратом
                    return;
                }

                // 3. Проверяем, есть ли FLAC-файлы, если задачи Decode отмечены, а задачи Encode — нет
                if (totalDecodeTasks > 0 && totalEncodeTasks == 0 && selectedFlacAudioFiles.Count == 0)
                {
                    MessageBox.Show("Select at least one FLAC file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    isExecuting = false; // Сбрасываем флаг перед возвратом
                    return;
                }
                
                // 4. Проверяем, есть ли аудио файлы, если задачи Decode отмечены, а задачи Encode — нет
                if (totalDecodeTasks > 0 && totalEncodeTasks == 0 && selectedAudioFiles.Count == 0)
                {
                    MessageBox.Show("Select at least one FLAC file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    isExecuting = false; // Сбрасываем флаг перед возвратом
                    return;
                }

                // 5. Проверяем, есть ли хотя бы один аудиофайл
                if (selectedAudioFiles.Count == 0)
                {
                    MessageBox.Show("Select at least one audio file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    isExecuting = false; // Сбрасываем флаг перед возвратом
                    return;
                }

                // Устанавливаем максимальные значения для прогресс-баров
                progressBarEncoder.Maximum = selectedEncoders.Count * selectedAudioFiles.Count * totalEncodeTasks;
                progressBarDecoder.Maximum = selectedEncoders.Count * selectedFlacAudioFiles.Count * totalDecodeTasks;

                // Сбрасываем прогресс-бары
                progressBarEncoder.Value = 0;
                progressBarDecoder.Value = 0;

                labelEncoderProgress.Text = $"{progressBarEncoder.Value}/{progressBarEncoder.Maximum}";
                labelDecoderProgress.Text = $"{progressBarDecoder.Value}/{progressBarDecoder.Maximum}";

                // Создаём временную директорию для выходного файла
                Directory.CreateDirectory(tempFolderPath);

                foreach (ListViewItem item in listViewJobs.Items)
                {
                    // Проверяем, отмечена ли задача
                    if (item.Checked)
                    {
                        string jobType = NormalizeSpaces(item.Text);
                        int passes = int.Parse(item.SubItems[1].Text.Trim());

                        for (int i = 0; i < passes; i++) // Цикл для количества проходов
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
                                    foreach (var audioFile in selectedAudioFiles)
                                    {
                                        if (_isEncodingStopped)
                                        {
                                            isExecuting = false;
                                            return;
                                        }

                                        // Формируем строку с параметрами
                                        string parameters = NormalizeSpaces(item.SubItems[2].Text.Trim());
                                        // Формируем аргументы для запуска
                                        string outputFilePath = Path.Combine(tempFolderPath, "temp_encoded.flac"); // Имя выходного файла
                                        string arguments = $"\"{audioFile}\" {parameters} -f -o \"{outputFilePath}\"";

                                        // Запускаем процесс и дожидаемся завершения
                                        try
                                        {
                                            await Task.Run(() =>
                                            {
                                                if (_isPaused)
                                                {
                                                    _pauseEvent.Wait(); // Ожидание паузы в фоновом потоке
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

                                                        // Устанавливаем приоритет процесса
                                                        try
                                                        {
                                                            if (!_process.HasExited)
                                                            {
                                                                _process.PriorityClass = GetProcessPriorityClass(comboBoxCPUPriority.SelectedItem.ToString());
                                                            }
                                                        }
                                                        catch (InvalidOperationException)
                                                        {
                                                            // Процесс завершён, логируем или обрабатываем по мере необходимости
                                                        }

                                                        _process.WaitForExit();
                                                    }

                                                    stopwatch.Stop();
                                                }
                                            });

                                            if (!_isEncodingStopped)
                                            {
                                                LogProcessResults(outputFilePath, audioFile, parameters, encoder);
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
                                    foreach (var audioFile in selectedFlacAudioFiles)
                                    {
                                        if (_isEncodingStopped)
                                        {
                                            isExecuting = false;
                                            return;
                                        }

                                        // Формируем строку с параметрами
                                        string parameters = NormalizeSpaces(item.SubItems[2].Text.Trim());
                                        // Формируем аргументы для запуска
                                        string outputFilePath = Path.Combine(tempFolderPath, "temp_decoded.wav"); // Имя выходного файла
                                        string arguments = $"\"{audioFile}\" -d {parameters} -f -o \"{outputFilePath}\"";

                                        // Запускаем процесс и дожидаемся завершения
                                        try
                                        {
                                            await Task.Run(() =>
                                            {
                                                if (_isPaused)
                                                {
                                                    _pauseEvent.Wait(); // Ожидание паузы в фоновом потоке
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

                                                        // Устанавливаем приоритет процесса
                                                        try
                                                        {
                                                            if (!_process.HasExited)
                                                            {
                                                                _process.PriorityClass = GetProcessPriorityClass(comboBoxCPUPriority.SelectedItem.ToString());
                                                            }
                                                        }
                                                        catch (InvalidOperationException)
                                                        {
                                                            // Процесс завершён, логируем или обрабатываем по мере необходимости
                                                        }

                                                        _process.WaitForExit();
                                                    }

                                                    stopwatch.Stop();
                                                }
                                            });

                                            if (!_isEncodingStopped)
                                            {
                                                LogProcessResults(outputFilePath, audioFile, parameters, encoder);
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
            textBoxThreads.Text = (physicalCores / 2).ToString(); // Устанавливаем половину ядер
        }
        private void buttonSetMaxCores_Click(object? sender, EventArgs e)
        {
            textBoxThreads.Text = physicalCores.ToString(); // Устанавливаем максимальное количество ядер
        }
        private void buttonSetHalfThreads_Click(object? sender, EventArgs e)
        {
            textBoxThreads.Text = (threadCount / 2).ToString(); // Устанавливаем половину потоков
        }
        private void buttonSetMaxThreads_Click(object? sender, EventArgs e)
        {
            textBoxThreads.Text = threadCount.ToString(); // Устанавливаем максимальное количество потоков
        }
        private void buttonClearCommandLineEncoder_Click(object? sender, EventArgs e)
        {
            textBoxCommandLineOptionsEncoder.Clear(); // Очищаем textCommandLineOptions
        }
        private void buttonClearCommandLineDecoder_Click(object? sender, EventArgs e)
        {
            textBoxCommandLineOptionsDecoder.Clear(); // Очищаем textCommandLineOptions
        }
        private void buttonepr8_Click(object? sender, EventArgs e)
        {
            // Проверяем, содержится ли -epr8 в textBoxAdditionalArguments
            if (!textBoxCommandLineOptionsEncoder.Text.Contains("-epr8"))
            {
                // Если нет, добавляем его
                textBoxCommandLineOptionsEncoder.AppendText(" -epr8"); // Добавляем с пробелом перед текстом
            }
        }
        private void buttonAsubdividetukey5flattop_Click(object? sender, EventArgs e)
        {
            // Проверяем, содержится ли -A "subdivide_tukey(5);flattop" в textBoxAdditionalArguments
            if (!textBoxCommandLineOptionsEncoder.Text.Contains("-A \"subdivide_tukey(5);flattop\""))
            {
                // Если нет, добавляем его
                textBoxCommandLineOptionsEncoder.AppendText(" -A \"subdivide_tukey(5);flattop\""); // Добавляем с пробелом перед текстом
            }
        }
        private void buttonNoPadding_Click(object? sender, EventArgs e)
        {
            // Проверяем, содержится ли --no-padding в textBoxAdditionalArguments
            if (!textBoxCommandLineOptionsEncoder.Text.Contains("--no-padding"))
            {
                // Если нет, добавляем его
                textBoxCommandLineOptionsEncoder.AppendText(" --no-padding"); // Добавляем с пробелом перед текстом
            }
        }
        private void buttonNoSeektable_Click(object? sender, EventArgs e)
        {
            // Проверяем, содержится ли --no-seektable в textBoxAdditionalArguments
            if (!textBoxCommandLineOptionsEncoder.Text.Contains("--no-seektable"))
            {
                // Если нет, добавляем его
                textBoxCommandLineOptionsEncoder.AppendText(" --no-seektable"); // Добавляем с пробелом перед текстом
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
            _isEncodingStopped = true; // Флаг о просьбе остановки кодирования
            _isPaused = false; // Сбрасываем флаг паузы
            _pauseEvent.Set(); // Разблокируем выполнение

            if (_process != null)
            {
                try
                {
                    // Проверяем, запущен ли процесс
                    if (!_process.HasExited)
                    {
                        _process.Kill(); // Завершаем процесс
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
                    _process.Dispose(); // Освобождаем ресурсы
                    _process = null; // Обнуляем ссылку на процесс
                    progressBarEncoder.Value = 0;
                    progressBarDecoder.Value = 0;
                    labelEncoderProgress.Text = $"";
                    labelDecoderProgress.Text = $"";
                    dataGridViewLog.ClearSelection(); // Очищаем выделение
                }
            }
        }
        private void buttonPauseResume_Click(object sender, EventArgs e)
        {
            _isPaused = !_isPaused; // Переключаем флаг паузы

            if (_isPaused)
            {
                buttonPauseResume.Text = "Resume";
                _pauseEvent.Reset(); // Блокируем выполнение
            }
            else
            {
                buttonPauseResume.Text = "Pause";
                _pauseEvent.Set(); // Разблокируем выполнение
            }
        }

        // Actions
        private ManualResetEventSlim _pauseEvent = new ManualResetEventSlim(true); // Изначально не на паузе
        private async void buttonStartEncode_Click(object? sender, EventArgs e)
        {
            dataGridViewLog.ClearSelection(); // Очищаем выделение

            if (isExecuting) return; // Проверяем, выполняется ли уже процесс
            isExecuting = true; // Устанавливаем флаг выполнения
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
                // Получаем выделенные енкодеры
                var selectedEncoders = listViewEncoders.CheckedItems
                    .Cast<ListViewItem>()
                    .Select(item => item.Tag.ToString()) // Получаем полный путь из Tag
                    .ToList();

                // Получаем выделенные аудиофайлы
                var selectedAudioFiles = listViewAudioFiles.CheckedItems
                    .Cast<ListViewItem>()
                    .Select(item => item.Tag.ToString()) // Получаем полный путь из Tag
                    .ToList();

                // Проверяем, есть ли выбранные енкодеры и аудиофайлы
                if (selectedEncoders.Count == 0 && selectedAudioFiles.Count == 0)
                {
                    MessageBox.Show("Please select at least one encoder and one audio file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    isExecuting = false; // Сбрасываем флаг, если нет файлов
                    return;
                }

                // Проверяем, есть ли выбранные енкодеры
                if (selectedEncoders.Count == 0)
                {
                    MessageBox.Show("Please select at least one encoder.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    isExecuting = false; // Сбрасываем флаг, если нет файлов
                    return;
                }

                // Проверяем, есть ли выбранные аудио файлы
                if (selectedAudioFiles.Count == 0)
                {
                    MessageBox.Show("Please select at least one audio file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    isExecuting = false; // Сбрасываем флаг, если нет файлов
                    return;
                }

                int totalTasks = selectedEncoders.Count * selectedAudioFiles.Count;
                progressBarEncoder.Maximum = totalTasks; // Максимальное значение прогресс-бара
                progressBarEncoder.Value = 0; // Сбросить значение прогресс-бара

                foreach (var encoder in selectedEncoders)
                {
                    foreach (var audioFile in selectedAudioFiles)
                    {
                        if (_isEncodingStopped)
                        {
                            isExecuting = false;
                            return;
                        }
                        
                        // Формируем строку с параметрами
                        string compressionLevel = NormalizeSpaces(textBoxCompressionLevel.Text);
                        string threads = NormalizeSpaces(textBoxThreads.Text);
                        string commandLine = NormalizeSpaces(textBoxCommandLineOptionsEncoder.Text);
                        string parameters = $"-{compressionLevel} {commandLine}".Trim();

                        // Добавляем количество потоков, если оно больше 1
                        if (int.TryParse(threads, out int threadCount) && threadCount > 1)
                        {
                            parameters += $" -j{threads}"; // добавляем флаг -j{threads}
                        }

                        // Формируем аргументы для запуска
                        string outputFilePath = Path.Combine(tempFolderPath, "temp_encoded.flac"); // Имя выходного файла
                        string arguments = $"\"{audioFile}\" {parameters} -f -o \"{outputFilePath}\"";

                        // Запускаем процесс и дожидаемся завершения
                        try
                        {
                            await Task.Run(() =>
                            {
                                if (_isPaused)
                                {
                                    _pauseEvent.Wait(); // Ожидание паузы в фоновом потоке
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

                                        // Устанавливаем приоритет процесса
                                        try
                                        {
                                            if (!_process.HasExited)
                                            {
                                                _process.PriorityClass = GetProcessPriorityClass(comboBoxCPUPriority.SelectedItem.ToString());
                                            }
                                        }
                                        catch (InvalidOperationException)
                                        {
                                            // Процесс завершён, логируем или обрабатываем по мере необходимости
                                        }

                                        _process.WaitForExit();
                                    }

                                    stopwatch.Stop();
                                }
                            });

                            if (!_isEncodingStopped)
                            {
                                LogProcessResults(outputFilePath, audioFile, parameters, encoder);
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
            dataGridViewLog.ClearSelection(); // Очищаем выделение

            if (isExecuting) return; // Проверяем, выполняется ли уже процесс
            isExecuting = true; // Устанавливаем флаг выполнения
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
                // Получаем выделенные .exe файлы
                var selectedEncoders = listViewEncoders.CheckedItems
                    .Cast<ListViewItem>()
                    .Select(item => item.Tag.ToString()) // Получаем полный путь из Tag
                    .ToList();

                // Получаем выделенные аудиофайлы, но только с расширением .flac
                var selectedFlacAudioFiles = listViewAudioFiles.CheckedItems
                    .Cast<ListViewItem>()
                    .Select(item => item.Tag.ToString()) // Получаем полный путь из Tag
                    .Where(file => Path.GetExtension(file).Equals(".flac", StringComparison.OrdinalIgnoreCase)) // Только .flac файлы
                    .ToList();

                // Проверяем, есть ли выбранные исполняемые файлы и аудиофайлы
                if (selectedEncoders.Count == 0 && selectedFlacAudioFiles.Count == 0)
                {
                    MessageBox.Show("Please select at least one executable and one FLAC audio file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    isExecuting = false; // Сбрасываем флаг, если нет файлов
                    return;
                }

                // Проверяем, есть ли выбранные исполняемые файлы
                if (selectedEncoders.Count == 0)
                {
                    MessageBox.Show("Please select at least one encoder.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    isExecuting = false; // Сбрасываем флаг, если нет файлов
                    return;
                }

                // Проверяем, есть ли выбранные FLAC файлы
                if (selectedFlacAudioFiles.Count == 0)
                {
                    MessageBox.Show("Please select at least one FLAC audio file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    isExecuting = false; // Сбрасываем флаг, если нет файлов
                    return;
                }

                int totalTasks = selectedEncoders.Count * selectedFlacAudioFiles.Count;
                progressBarDecoder.Maximum = totalTasks; // Максимальное значение прогресс-бара
                progressBarDecoder.Value = 0; // Сбросить значение прогресс-бара

                foreach (var encoder in selectedEncoders)
                {
                    foreach (var audioFile in selectedFlacAudioFiles)
                    {
                        if (_isEncodingStopped)
                        {
                            isExecuting = false;
                            return;
                        }

                        // Формируем строку с параметрами
                        string commandLine = NormalizeSpaces(textBoxCommandLineOptionsDecoder.Text);
                        string parameters = $"{commandLine}".Trim();

                        // Формируем аргументы для запуска
                        string outputFilePath = Path.Combine(tempFolderPath, "temp_decoded.wav"); // Имя выходного файла
                        string arguments = $"\"{audioFile}\" -d {parameters} -f -o \"{outputFilePath}\"";

                        // Запускаем процесс и дожидаемся завершения
                        try
                        {
                            await Task.Run(() =>
                            {
                                if (_isPaused)
                                {
                                    _pauseEvent.Wait(); // Ожидание паузы в фоновом потоке
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

                                        // Устанавливаем приоритет процесса
                                        try
                                        {
                                            if (!_process.HasExited)
                                            {
                                                _process.PriorityClass = GetProcessPriorityClass(comboBoxCPUPriority.SelectedItem.ToString());
                                            }
                                        }
                                        catch (InvalidOperationException)
                                        {
                                            // Процесс завершён, логируем или обрабатываем по мере необходимости
                                        }

                                        _process.WaitForExit();
                                    }

                                    stopwatch.Stop();
                                }
                            });

                            if (!_isEncodingStopped)
                            {
                                LogProcessResults(outputFilePath, audioFile, parameters, encoder);
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

        // General methods
        private void MoveSelectedItems(ListView listView, int direction)
        {
            // Получаем выделенные элементы и сортируем их по индексам
            var selectedItems = listView.SelectedItems.Cast<ListViewItem>()
            .OrderBy(item => item.Index)
            .ToList();
            // Если выделенных элементов нет, выходим из метода
            if (selectedItems.Count == 0)
                return;
            // Если перемещение вниз, мы будем взимать элементы в обратном порядке
            if (direction > 0)
            {
                selectedItems.Reverse(); // Переворачиваем список для перемещения вниз
            }
            // Приостанавливаем обновление ListView для снижения мерцания
            listView.BeginUpdate();
            try
            {
                // Перемещение элементов
                foreach (var item in selectedItems)
                {
                    int currentIndex = item.Index;
                    int newIndex = currentIndex + direction;
                    // Проверяем границы
                    if (newIndex < 0 || newIndex >= listView.Items.Count)
                        return; // Если выход за пределы, выходим из метода
                                // Удаляем элемент из текущего места
                    listView.Items.Remove(item);
                    // Вставляем элемент на новое место
                    listView.Items.Insert(newIndex, item);
                }
                // Обновляем выделение
                UpdateSelection(selectedItems, listView);
            }
            finally
            {
                // Возобновляем обновление ListView
                listView.EndUpdate();
            }
        }
        private void UpdateSelection(List<ListViewItem> selectedItems, ListView listView)
        {
            // Снимаем выделение со всех элементов
            foreach (ListViewItem item in listView.Items)
            {
                item.Selected = false;
            }
            // Выделяем перемещенные элементы
            foreach (var item in selectedItems)
            {
                item.Selected = true; // Устанавливаем выделение на перемещенные элементы
            }
            listView.Focus(); // Ставим фокус на список
        }
        
        // Метод для получения приоритета процесса
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
                    return ProcessPriorityClass.Normal; // Значение по умолчанию
            }
        }

        private void ShowTemporaryStoppedMessage(string message)
        {
            labelStopped.Text = message; // Устанавливаем текст сообщения
            labelStopped.Visible = true; // Делаем метку видимой

            System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer(); // Явно используем пространство имен
            timer.Interval = 4000; // Задаем интервал 2 секунды
            timer.Tick += (s, e) =>
            {
                labelStopped.Visible = false; // Скрываем метку
                timer.Stop(); // Останавливаем таймер
                timer.Dispose(); // Освобождаем ресурсы
            };
            timer.Start(); // Запускаем таймер
        }
        private void buttonSelectTempFolder_Click(object? sender, EventArgs e)
        {
            using (FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog())
            {
                folderBrowserDialog.Description = "Select temp folder";
                // Если путь сохранён в настройках, устанавливаем его
                if (Directory.Exists(tempFolderPath))
                {
                    folderBrowserDialog.SelectedPath = tempFolderPath;
                }
                if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
                {
                    // Получаем выбранный путь
                    tempFolderPath = folderBrowserDialog.SelectedPath;
                    // Сохраняем путь в настройках
                    SaveSettings(); // Это также нужно будет изменить, чтобы сохранить путь
                }
            }
        }

        // FORM LOAD
        private void Form1_Load(object? sender, EventArgs e)
        {
            LoadSettings(); // Загрузка настроек
            this.ActiveControl = null; // Снимаем фокус с всех элементов
        }
        private void Form1_FormClosing(object? sender, FormClosingEventArgs e)
        {
            // Сохранение настроек перед закрытием
            SaveSettings();
            // Остановка таймера
            cpuUsageTimer.Stop();
            cpuUsageTimer.Dispose();
            _pauseEvent.Dispose(); // Освобождаем ресурсы
            cpuCounter.Dispose(); // Освобождаем ресурсы



            if (checkBoxClearTempFolder.Checked)
            {
                // Проверяем, существует ли временная папка
                if (Directory.Exists(tempFolderPath))
                {
                    var tempEncodedFilePath = Path.Combine(tempFolderPath, "temp_encoded.flac");
                    var tempDecodedFilePath = Path.Combine(tempFolderPath, "temp_decoded.wav");

                    // Удаляем временные файлы, если они существуют
                    if (File.Exists(tempEncodedFilePath))
                    {
                        File.Delete(tempEncodedFilePath);
                    }

                    if (File.Exists(tempDecodedFilePath))
                    {
                        File.Delete(tempDecodedFilePath);
                    }

                    // Проверяем, если после удаления файлов в папке больше ничего не осталось
                    if (Directory.GetFiles(tempFolderPath).Length == 0)
                    {
                        // Удаляем папку, если она пустая
                        Directory.Delete(tempFolderPath);
                    }
                }
            }
        }
    }
}