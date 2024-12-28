using MediaInfoLib;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Management;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Security.Cryptography;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using ClosedXML.Excel;

namespace FLAC_Benchmark_H
{
    public partial class Form1 : Form
    {
        private int physicalCores;
        private int threadCount;
        private Process _process; // Поле для хранения текущего процесса
        private const string SettingsFilePath = "Settings_general.txt"; // Путь к файлу настроек
        private const string JobsFilePath = "Settings_jobs.txt"; // Путь к файлу jobs
        private const string executablesFilePath = "Settings_flac_executables.txt"; // Путь к файлу для сохранения исполняемых файлов
        private const string audioFilesFilePath = "Settings_audio_files.txt"; // Путь к файлу для сохранения аудиофайлов
        private Stopwatch stopwatch;
        private PerformanceCounter cpuCounter;
        private System.Windows.Forms.Timer cpuUsageTimer; // Указываем явно, что это Timer из System.Windows.Forms
        private bool _isEncodingStopped = false;
        private bool isExecuting = false; // Флаг для отслеживания, выполняется ли процесс
        private string tempFolderPath; // Поле для хранения пути к временной папке
        private bool isCpuInfoLoaded = false;
        public Form1()
        {
            InitializeComponent();
            InitializeDragAndDrop(); // Инициализация drag-and-drop
            this.FormClosing += Form1_FormClosing; // Регистрация обработчика события закрытия формы
            this.listViewFlacExecutables.KeyDown += ListViewFlacExecutables_KeyDown;
            this.listViewAudioFiles.KeyDown += ListViewAudioFiles_KeyDown;
            this.listViewJobs.KeyDown += ListViewJobs_KeyDown;
            LoadCPUInfo(); // Загружаем информацию о процессоре
            stopwatch = new Stopwatch(); // Инициализация объекта Stopwatch
            cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            cpuUsageTimer = new System.Windows.Forms.Timer(); // Явно указываем System.Windows.Forms.Timer
            cpuUsageTimer.Interval = 250; // Каждые 250 мс
            cpuUsageTimer.Tick += async (sender, e) => await UpdateCpuUsageAsync();
            cpuUsageTimer.Start();
            InitializedataGridViewLog();
            tempFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp"); // Инициализация пути к временной папке
            _process = new Process(); // Initialize _process to avoid nullability warning

            // Включаем пользовательскую отрисовку для listViewJobs
            listViewJobs.OwnerDraw = true;
            listViewJobs.DrawColumnHeader += ListViewJobs_DrawColumnHeader;
            listViewJobs.DrawSubItem += ListViewJobs_DrawSubItem;
            comboBoxCPUPriority.SelectedIndex = 3;
        }
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

        private string NormalizeSpaces(string input)
        {
            return Regex.Replace(input.Trim(), @"\s+", " "); // Удаляем лишние пробелы внутри строки
        }

        // Метод для загрузки информации о процессоре
        private void LoadCPUInfo()
        {
            if (!isCpuInfoLoaded)
            {
                try
                {
                    physicalCores = 0;
                    threadCount = 0;
                    // Создаем запрос для получения информации о процессорах
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
                    // Обновляем метку с информацией о процессоре
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
                    // Записываем ошибку в labelCPUinfo
                    labelCPUinfo.Text = "Error loading CPU info: " + ex.Message;
                }
            }
        }
        private async Task UpdateCpuUsageAsync()
        {
            float cpuUsage = await Task.Run(() => cpuCounter.NextValue());
            labelCPUinfo.Text = $"Your system has:\nCores: {physicalCores}, Threads: {threadCount}\nCPU Usage: {cpuUsage:F2}%";
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
                SaveExecutables();
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
            LoadExecutables(); // Загрузка исполняемых файлов
            LoadAudioFiles(); // Загрузка аудиофайлов
            LoadJobs(); // Загружаем содержимое Settings_joblist.txt
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

        private void InitializeDragAndDrop()
        {
            // Разрешаем перетаскивание файлов в ListView для программ
            listViewFlacExecutables.AllowDrop = true;
            listViewFlacExecutables.DragEnter += ListViewFlacExecutables_DragEnter;
            listViewFlacExecutables.DragDrop += ListViewFlacExecutables_DragDrop;
            // Разрешаем перетаскивание файлов в ListView для аудиофайлов
            listViewAudioFiles.AllowDrop = true;
            listViewAudioFiles.DragEnter += ListViewAudioFiles_DragEnter;
            listViewAudioFiles.DragDrop += ListViewAudioFiles_DragDrop;
            // Разрешаем перетаскивание файлов в ListView для очереди задач
            listViewJobs.AllowDrop = true;
            listViewJobs.DragEnter += ListViewJobs_DragEnter;
            listViewJobs.DragDrop += ListViewJobs_DragDrop;
        }

        // Executables
        private void ListViewFlacExecutables_DragEnter(object? sender, DragEventArgs e)
        {
            if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
            {
                string[] files = (string[]?)e.Data.GetData(DataFormats.FileDrop) ?? Array.Empty<string>();
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
        private async void ListViewFlacExecutables_DragDrop(object? sender, DragEventArgs e)
        {
            string[] files = (string[]?)e.Data?.GetData(DataFormats.FileDrop) ?? Array.Empty<string>();
            foreach (var file in files)
            {
                if (Directory.Exists(file))
                {
                    await AddExecutableFiles(file); // Асинхронно добавляем исполняемые файлы
                }
                else if (Path.GetExtension(file).Equals(".exe", StringComparison.OrdinalIgnoreCase))
                {
                    AddExecutableFileToListView(file, true); // Добавляем сразу, без Task.Run
                }
            }
        }
        // Рекурсивный метод для добавления исполняемых файлов в ListView
        private async Task AddExecutableFiles(string directory)
        {
            try
            {
                var exeFiles = Directory.GetFiles(directory, "*.exe", SearchOption.AllDirectories);
                foreach (var file in exeFiles)
                {
                    AddExecutableFileToListView(file, true); // Сразу добавляем без Task.Run
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error accessing directory: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        // Загрузка исполняемых файлов из файла txt
        private async void LoadExecutables()
        {
            if (File.Exists(executablesFilePath))
            {
                try
                {
                    string[] lines = await File.ReadAllLinesAsync(executablesFilePath);
                    listViewFlacExecutables.Items.Clear();
                    foreach (var line in lines)
                    {
                        var parts = line.Split('~');
                        if (parts.Length == 2)
                        {
                            string executablePath = parts[0]; // Полный путь
                            bool isChecked = bool.Parse(parts[1]); // Статус "выделено"
                            AddExecutableFileToListView(executablePath, isChecked); // Добавляем сразу
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading executables: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
        // Общий метод добавления исполняемых файлов в ListView
        private void AddExecutableFileToListView(string executable, bool isChecked = true)
        {
            // Запускаем действие в основном потоке
            if (listViewFlacExecutables.InvokeRequired)
            {
                listViewFlacExecutables.Invoke((MethodInvoker)(() => AddExecutableFileToListView(executable, isChecked)));
                return;
            }

            var version = GetExecutableInfo(executable);
            long fileSize = new FileInfo(executable).Length;
            DateTime lastModifiedDate = new FileInfo(executable).LastWriteTime;

            var item = new ListViewItem(Path.GetFileName(executable))
            {
                Tag = executable,
                Checked = isChecked
            };

            item.SubItems.Add(version);
            item.SubItems.Add($"{fileSize:n0} bytes");
            item.SubItems.Add(lastModifiedDate.ToString("yyyy.MM.dd HH:mm"));

            listViewFlacExecutables.Items.Add(item);
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
                        AddExecutableFileToListView(file); // Используем общий метод
                    }
                }
            }
        }
        private void buttonUpEncoder_Click(object sender, EventArgs e)
        {
            MoveSelectedItems(listViewFlacExecutables, -1); // Передаём -1 для перемещения вверх
        }
        private void buttonDownEncoder_Click(object sender, EventArgs e)
        {
            MoveSelectedItems(listViewFlacExecutables, 1); // Передаём 1 для перемещения вниз
        }
        private void buttonRemoveEncoder_Click(object? sender, EventArgs e)
        {
            // Удаляем выделенные элементы из listViewFlacExecutables
            for (int i = listViewFlacExecutables.Items.Count - 1; i >= 0; i--)
            {
                if (listViewFlacExecutables.Items[i].Selected) // Проверяем, выделен ли элемент
                {
                    listViewFlacExecutables.Items.RemoveAt(i); // Удаляем элемент
                }
            }
        }
        private void buttonClearEncoders_Click(object? sender, EventArgs e)
        {
            listViewFlacExecutables.Items.Clear();
        }
        private string GetExecutableInfo(string executablePath)
        {
            using (Process process = new Process())
            {
                process.StartInfo.FileName = executablePath;
                process.StartInfo.Arguments = "--version";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true; // Перенаправляем стандартный вывод
                process.StartInfo.CreateNoWindow = true;
                process.Start();
                string version = process.StandardOutput.ReadLine(); // Читаем первую строку вывода
                process.WaitForExit();
                return version; // Возвращаем только версию
            }
        }

        //Audio files
        private void ListViewAudioFiles_DragEnter(object? sender, DragEventArgs e)
        {
            if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
            {
                string[] files = (string[]?)e.Data.GetData(DataFormats.FileDrop) ?? Array.Empty<string>();
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
        private async void ListViewAudioFiles_DragDrop(object? sender, DragEventArgs e)
        {
            string[] files = (string[]?)e.Data?.GetData(DataFormats.FileDrop) ?? Array.Empty<string>();
            await AddAudioFiles(files); // Вызываем один общий метод для обработки файлов и папок
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
            return Path.GetExtension(file).Equals(".wav", StringComparison.OrdinalIgnoreCase) ||
                   Path.GetExtension(file).Equals(".flac", StringComparison.OrdinalIgnoreCase);
        }
        // Метод для добавления аудиофайлов из директории в ListView
        private async Task AddAudioFiles(IEnumerable<string> files)
        {
            try
            {
                var audioFiles = new List<string>();

                foreach (var file in files)
                {
                    if (Directory.Exists(file))
                    {
                        // Если это директория, добавляем все аудиофайлы из неё
                        var directoryFiles = Directory.GetFiles(file, "*.wav", SearchOption.AllDirectories)
                            .Concat(Directory.GetFiles(file, "*.flac", SearchOption.AllDirectories));
                        audioFiles.AddRange(directoryFiles);
                    }
                    else if (IsAudioFile(file))
                    {
                        audioFiles.Add(file); // Если это файл, добавляем его в список
                    }
                }

                // Создаем задачи для создания ListViewItem для всех найденных аудиофайлов
                var tasks = audioFiles.Select(file => Task.Run(() => CreateListViewItem(file))).ToList();

                var items = await Task.WhenAll(tasks); // Ожидаем завершения всех задач

                foreach (var item in items)
                {
                    listViewAudioFiles.Items.Add(item); // Добавляем элементы в UI потоке
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error accessing files: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        // Метод для создания элемента ListViewItem
        private ListViewItem CreateListViewItem(string audioFile)
        {
            var item = new ListViewItem(Path.GetFileName(audioFile))
            {
                Tag = audioFile, // Хранение полного пути
                Checked = true // Устанавливаем выделение по умолчанию
            };

            // Получаем информацию о файле без MD5
            var (duration, bitDepth, samplingRate) = GetAudioInfo(audioFile);
            long fileSize = new FileInfo(audioFile).Length;
            item.SubItems.Add(duration != "N/A" ? Convert.ToInt64(duration).ToString("N0") + " ms" : duration);
            item.SubItems.Add(bitDepth + " bit");
            item.SubItems.Add(samplingRate);
            item.SubItems.Add($"{fileSize:n0} bytes");
            item.SubItems.Add(""); // Устанавливаем для MD5 по умолчанию

            return item;
        }
        // Метод для загрузки аудиофайлов из файла txt
        private async void LoadAudioFiles()
        {
            if (File.Exists(audioFilesFilePath))
            {
                try
                {
                    string[] lines = await File.ReadAllLinesAsync(audioFilesFilePath);
                    listViewAudioFiles.Items.Clear();

                    var tasks = lines.Select(async line =>
                    {
                        var parts = line.Split('~');
                        if (parts.Length == 2)
                        {
                            string audioFilePath = parts[0]; // Полный путь
                            bool isChecked = bool.Parse(parts[1]); // Статус "выделено"
                            var item = await Task.Run(() => CreateListViewItem(audioFilePath)); // Создание элемента
                            item.Checked = isChecked; // Установка статуса чекбокса
                            return item;
                        }
                        return null; // Возвращаем null, если не удалось создать элемент
                    }).Where(item => item != null); // Фильтруем null

                    var items = await Task.WhenAll(tasks); // Ожидаем завершения всех задач
                    foreach (var item in items)
                    {
                        listViewAudioFiles.Items.Add(item); // Добавляем только непустые элементы
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading audio files: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
        // Метод для получения длительности и разрядности аудиофайла
        private (string duration, string bitDepth, string samplingRate) GetAudioInfo(string audioFile)
        {
            var mediaInfo = new MediaInfoLib.MediaInfo();
            mediaInfo.Open(audioFile);

            string duration = mediaInfo.Get(StreamKind.Audio, 0, "Duration") ?? "N/A";
            string bitDepth = mediaInfo.Get(StreamKind.Audio, 0, "BitDepth") ?? "N/A";
            string samplingRate = mediaInfo.Get(StreamKind.Audio, 0, "SamplingRate/String") ?? "N/A";

            mediaInfo.Close(); // Закрываем mediaInfo здесь

            return (duration, bitDepth, samplingRate);
        }
        private async void buttonDetectDupesAudioFiles_Click(object sender, EventArgs e)
        {
            var hashDict = new Dictionary<string, List<ListViewItem>>();

            // Проверка и отображение колонки MD5
            if (listViewAudioFiles.Columns[5].Width == 0)
            {
                listViewAudioFiles.Columns[5].Width = 230; // Показываем колонку MD5
            }

            var tasks = listViewAudioFiles.Items.Cast<ListViewItem>().Select(async item =>
            {
                string filePath = item.Tag.ToString(); // Получаем путь файла

                // Вычисляем MD5 асинхронно
                string md5Hash = await Task.Run(() => GetFileMD5Hash(filePath));

                if (!string.IsNullOrEmpty(md5Hash))
                {
                    // Устанавливаем MD5 в подэлемент
                    item.SubItems[5].Text = md5Hash;

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
        // Объединённый метод для получения MD5 хеша
        private string GetFileMD5Hash(string filePath)
        {
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
                                    // Читаем аудиоданные из блока "data"
                                    byte[] audioData = reader.ReadBytes((int)chunkSize);

                                    // Рассчитываем хэш
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
        private void buttonUpAudioFile_Click(object sender, EventArgs e)
        {
            MoveSelectedItems(listViewAudioFiles, -1); // Передаём -1 для перемещения вверх
        }
        private void buttonDownAudioFile_Click(object sender, EventArgs e)
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
        private void buttonClearAudioFiles_Click(object? sender, EventArgs e)
        {
            listViewAudioFiles.Items.Clear();
        }

        // Действия клавиш
        private void ListViewFlacExecutables_KeyDown(object? sender, KeyEventArgs e)
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
        private void buttonUpJob_Click(object sender, EventArgs e)
        {
            MoveSelectedItems(listViewJobs, -1); // Передаём -1 для перемещения вверх
        }
        private void buttonDownJob_Click(object sender, EventArgs e)
        {
            MoveSelectedItems(listViewJobs, 1); // Передаём 1 для перемещения вниз
        }
        private void buttonRemoveJob_Click(object sender, EventArgs e)
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
        private void buttonAddJobToJobListEncoder_Click(object sender, EventArgs e)
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
        private void buttonAddJobToJobListDecoder_Click(object sender, EventArgs e)
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
        private void buttonPlusPass_Click(object sender, EventArgs e)
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
        private void buttonMinusPass_Click(object sender, EventArgs e)
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
        private void buttonCopyJobs_Click(object sender, EventArgs e)
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
        private void buttonPasteJobs_Click(object sender, EventArgs e)
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
        private async void buttonStartJobList_Click(object sender, EventArgs e)
        {
            if (isExecuting) return; // Проверяем, выполняется ли уже процесс
            isExecuting = true; // Устанавливаем флаг выполнения
            foreach (ListViewItem item in listViewJobs.Items)
            {
                // Проверяем, отмечена ли задача
                if (item.Checked)
                {
                    string jobType = NormalizeSpaces(item.Text);
                    int passes = int.Parse(item.SubItems[1].Text.Trim());
                    for (int i = 0; i < passes; i++) // Цикл для количества проходов
                    {
                        if (string.Equals(jobType, "Encode", StringComparison.OrdinalIgnoreCase))
                        {
                            // Устанавливаем флаг остановки
                            _isEncodingStopped = false;
                            // Создаём временную директорию для выходного файла
                            Directory.CreateDirectory(tempFolderPath);
                            // Получаем выделенные .exe файлы
                            var selectedExecutables = listViewFlacExecutables.CheckedItems
                            .Cast<ListViewItem>()
                            .Select(i => NormalizeSpaces(i.Tag.ToString())) // Получаем полный путь из Tag
                            .ToList();
                            // Получаем выделенные аудиофайлы
                            var selectedAudioFiles = listViewAudioFiles.CheckedItems
                            .Cast<ListViewItem>()
                            .Select(i => NormalizeSpaces(i.Tag.ToString())) // Получаем полный путь из Tag
                            .ToList();
                            // Проверяем, есть ли выбранные исполняемые файлы и аудиофайлы
                            if (selectedExecutables.Count == 0 || selectedAudioFiles.Count == 0)
                            {
                                MessageBox.Show("Please select at least one executable and one audio file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                isExecuting = false; // Сбрасываем флаг перед возвратом
                                return;
                            }
                            ProcessPriorityClass priorityClass;
                            switch (comboBoxCPUPriority.SelectedItem.ToString())
                            {
                                case "Low":
                                    priorityClass = ProcessPriorityClass.Idle;
                                    break;
                                case "BelowNormal":
                                    priorityClass = ProcessPriorityClass.BelowNormal;
                                    break;
                                case "Normal":
                                    priorityClass = ProcessPriorityClass.Normal;
                                    break;
                                case "AboveNormal":
                                    priorityClass = ProcessPriorityClass.AboveNormal;
                                    break;
                                case "High":
                                    priorityClass = ProcessPriorityClass.High;
                                    break;
                                case "RealTime":
                                    priorityClass = ProcessPriorityClass.RealTime;
                                    break;
                                default:
                                    priorityClass = ProcessPriorityClass.Normal; // Значение по умолчанию
                                    break;
                            }
                            foreach (var executable in selectedExecutables)
                            {
                                foreach (var audioFile in selectedAudioFiles)
                                {
                                    if (_isEncodingStopped)
                                    {
                                        isExecuting = false; // Сбрасываем флаг перед возвратом
                                        return; // Выходим, если остановка запроса
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
                                            using (_process = new Process()) // Сохраняем процесс в поле _process
                                            {
                                                _process.StartInfo = new ProcessStartInfo
                                                {
                                                    FileName = executable,
                                                    Arguments = arguments,
                                                    UseShellExecute = false,
                                                    CreateNoWindow = true,
                                                };
                                                // Запускаем отсчет времени
                                                stopwatch.Reset();
                                                stopwatch.Start();
                                                if (!_isEncodingStopped)
                                                {
                                                    _process.Start();
                                                    // Устанавливаем приоритет процесса, если он начал успешно
                                                    try
                                                    {
                                                        if (!_process.HasExited)
                                                        {
                                                            _process.PriorityClass = priorityClass;
                                                        }
                                                    }
                                                    catch (InvalidOperationException)
                                                    {
                                                        // Процесс завершён, логируем или обрабатываем по мере необходимости
                                                    }
                                                    if (!_process.HasExited)
                                                    {
                                                        _process.WaitForExit();
                                                    }
                                                    stopwatch.Stop();
                                                }
                                            }
                                        });
                                        // Условие: записывать в лог только если процесс не был остановлен
                                        if (!_isEncodingStopped)
                                        {
                                            LogProcessResults(outputFilePath, audioFile, parameters, executable);
                                        }
                                        else
                                        {
                                            // MessageBox.Show("Output file was not created.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                            isExecuting = false; // Сбрасываем флаг перед возвратом
                                            return;
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        MessageBox.Show($"Error starting encoding process: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                        isExecuting = false; // Сбрасываем флаг перед возвратом
                                        return;
                                    }
                                }
                            }
                        }
                        else if (string.Equals(jobType, "Decode", StringComparison.OrdinalIgnoreCase))
                        {
                            // Устанавливаем флаг остановки
                            _isEncodingStopped = false;
                            // Создаём временную директорию для выходного файла
                            Directory.CreateDirectory(tempFolderPath);
                            // Получаем выделенные .exe файлы
                            var selectedExecutables = listViewFlacExecutables.CheckedItems
                            .Cast<ListViewItem>()
                            .Select(item => NormalizeSpaces(item.Tag.ToString())) // Получаем полный путь из Tag
                            .ToList();
                            // Получаем выделенные аудиофайлы, но только с расширением .flac
                            var selectedAudioFiles = listViewAudioFiles.CheckedItems
                            .Cast<ListViewItem>()
                            .Select(item => NormalizeSpaces(item.Tag.ToString())) // Получаем полный путь из Tag
                            .Where(file => Path.GetExtension(file).Equals(".flac", StringComparison.OrdinalIgnoreCase)) // Только .flac файлы
                            .ToList();
                            // Проверяем, есть ли выбранные исполняемые файлы и аудиофайлы
                            if (selectedExecutables.Count == 0 || selectedAudioFiles.Count == 0)
                            {
                                MessageBox.Show("Please select at least one executable and one FLAC audio file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                isExecuting = false; // Сбрасываем флаг перед возвратом
                                return;
                            }
                            ProcessPriorityClass priorityClass;
                            switch (comboBoxCPUPriority.SelectedItem.ToString())
                            {
                                case "Low":
                                    priorityClass = ProcessPriorityClass.Idle;
                                    break;
                                case "BelowNormal":
                                    priorityClass = ProcessPriorityClass.BelowNormal;
                                    break;
                                case "Normal":
                                    priorityClass = ProcessPriorityClass.Normal;
                                    break;
                                case "AboveNormal":
                                    priorityClass = ProcessPriorityClass.AboveNormal;
                                    break;
                                case "High":
                                    priorityClass = ProcessPriorityClass.High;
                                    break;
                                case "RealTime":
                                    priorityClass = ProcessPriorityClass.RealTime;
                                    break;
                                default:
                                    priorityClass = ProcessPriorityClass.Normal; // Значение по умолчанию
                                    break;
                            }
                            foreach (var executable in selectedExecutables)
                            {
                                foreach (var audioFile in selectedAudioFiles)
                                {
                                    if (_isEncodingStopped)
                                    {
                                        return; // Выходим, если остановка запроса
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
                                            using (_process = new Process()) // Сохраняем процесс в поле _process
                                            {
                                                _process.StartInfo = new ProcessStartInfo
                                                {
                                                    FileName = executable,
                                                    Arguments = arguments,
                                                    UseShellExecute = false,
                                                    CreateNoWindow = true,
                                                };
                                                // Запускаем отсчет времени
                                                stopwatch.Reset();
                                                stopwatch.Start();
                                                if (!_isEncodingStopped)
                                                {
                                                    _process.Start();
                                                    // Устанавливаем приоритет процесса, если он начал успешно
                                                    try
                                                    {
                                                        if (!_process.HasExited)
                                                        {
                                                            _process.PriorityClass = priorityClass;
                                                        }
                                                    }
                                                    catch (InvalidOperationException)
                                                    {
                                                        // Процесс завершён, логируем или обрабатываем по мере необходимости
                                                    }
                                                    if (!_process.HasExited)
                                                    {
                                                        _process.WaitForExit();
                                                    }
                                                    stopwatch.Stop();
                                                }
                                            }
                                        });
                                        // Условие: записывать в лог только если процесс не был остановлен
                                        if (!_isEncodingStopped)
                                        {
                                            LogProcessResults(outputFilePath, audioFile, parameters, executable);
                                        }
                                        else
                                        {
                                            // MessageBox.Show("Output file was not created.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                            isExecuting = false; // Сбрасываем флаг перед возвратом
                                            return;
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        MessageBox.Show($"Error starting decoding process: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                        isExecuting = false; // Сбрасываем флаг перед возвратом
                                        return;
                                    }
                                }
                            }
                        }
                    }
                }
                isExecuting = false; // Сбрасываем флаг после завершения
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

        //Actions
        private async void buttonStartEncode_Click(object? sender, EventArgs e)
        {
            if (isExecuting) return; // Проверяем, выполняется ли уже процесс
            isExecuting = true; // Устанавливаем флаг выполнения

            // Устанавливаем флаг остановки
            _isEncodingStopped = false;
            // Создаём временную директорию для выходного файла
            Directory.CreateDirectory(tempFolderPath);
            // Получаем выделенные .exe файлы
            var selectedExecutables = listViewFlacExecutables.CheckedItems
            .Cast<ListViewItem>()
            .Select(item => item.Tag.ToString()) // Получаем полный путь из Tag
            .ToList();
            // Получаем выделенные аудиофайлы
            var selectedAudioFiles = listViewAudioFiles.CheckedItems
            .Cast<ListViewItem>()
            .Select(item => item.Tag.ToString()) // Получаем полный путь из Tag
            .ToList();
            // Проверяем, есть ли выбранные исполняемые файлы и аудиофайлы
            if (selectedExecutables.Count == 0 || selectedAudioFiles.Count == 0)
            {
                MessageBox.Show("Please select at least one executable and one audio file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                isExecuting = false; // Сбрасываем флаг, если нет файлов
                return;
            }
            ProcessPriorityClass priorityClass;
            switch (comboBoxCPUPriority.SelectedItem.ToString())
            {
                case "Low":
                    priorityClass = ProcessPriorityClass.Idle;
                    break;
                case "BelowNormal":
                    priorityClass = ProcessPriorityClass.BelowNormal;
                    break;
                case "Normal":
                    priorityClass = ProcessPriorityClass.Normal;
                    break;
                case "AboveNormal":
                    priorityClass = ProcessPriorityClass.AboveNormal;
                    break;
                case "High":
                    priorityClass = ProcessPriorityClass.High;
                    break;
                case "RealTime":
                    priorityClass = ProcessPriorityClass.RealTime;
                    break;
                default:
                    priorityClass = ProcessPriorityClass.Normal; // Значение по умолчанию
                    break;
            }
            foreach (var executable in selectedExecutables)
            {
                foreach (var audioFile in selectedAudioFiles)
                {
                    if (_isEncodingStopped)
                    {
                        isExecuting = false; // Сбрасываем флаг перед выходом
                        return; // Выходим, если остановка запроса
                    }
                    // Получаем значения из текстовых полей и формируем аргументы...
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
                    // Формируем аргументы для запуска
                    string outputFilePath = Path.Combine(tempFolderPath, "temp_encoded.flac"); // Имя выходного файла
                    string arguments = $"\"{audioFile}\" {parameters} -f -o \"{outputFilePath}\"";
                    // Запускаем процесс и дожидаемся завершения
                    try
                    {
                        await Task.Run(() =>
                        {
                            using (_process = new Process()) // Сохраняем процесс в поле _process
                            {
                                _process.StartInfo = new ProcessStartInfo
                                {
                                    FileName = executable,
                                    Arguments = arguments,
                                    UseShellExecute = false,
                                    CreateNoWindow = true,
                                };
                                // Запускаем отсчет времени
                                stopwatch.Reset();
                                stopwatch.Start();
                                if (!_isEncodingStopped)
                                {
                                    _process.Start();
                                    // Устанавливаем приоритет процесса, если он начал успешно
                                    try
                                    {
                                        if (!_process.HasExited)
                                        {
                                            _process.PriorityClass = priorityClass;
                                        }
                                    }
                                    catch (InvalidOperationException)
                                    {
                                        // Процесс завершён, логируем или обрабатываем по мере необходимости
                                    }
                                    if (!_process.HasExited)
                                    {
                                        _process.WaitForExit();
                                    }
                                    stopwatch.Stop();
                                }
                            }
                        });
                        // Условие: записывать в лог только если процесс не был остановлен
                        if (!_isEncodingStopped)
                        {
                            LogProcessResults(outputFilePath, audioFile, parameters, executable);
                        }
                        else
                        {
                            // MessageBox.Show("Output file was not created.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            isExecuting = false; // Сбрасываем флаг перед возвратом
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error starting encoding process: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        isExecuting = false; // Сбрасываем флаг перед возвратом
                        return;
                    }
                }
            }
            // Сбрасываем флаг выполнения после завершения
            isExecuting = false;
        }
        private async void buttonStartDecode_Click(object? sender, EventArgs e)
        {
            if (isExecuting) return; // Проверяем, выполняется ли уже процесс
            isExecuting = true; // Устанавливаем флаг выполнения
                                // Устанавливаем флаг остановки
            _isEncodingStopped = false;
            // Создаём временную директорию для выходного файла
            Directory.CreateDirectory(tempFolderPath);
            // Получаем выделенные .exe файлы
            var selectedExecutables = listViewFlacExecutables.CheckedItems
            .Cast<ListViewItem>()
            .Select(item => item.Tag.ToString()) // Получаем полный путь из Tag
            .ToList();
            // Получаем выделенные аудиофайлы, но только с расширением .flac
            var selectedAudioFiles = listViewAudioFiles.CheckedItems
            .Cast<ListViewItem>()
            .Select(item => item.Tag.ToString()) // Получаем полный путь из Tag
            .Where(file => Path.GetExtension(file).Equals(".flac", StringComparison.OrdinalIgnoreCase)) // Только .flac файлы
            .ToList();
            // Проверяем, есть ли выбранные исполняемые файлы и аудиофайлы
            if (selectedExecutables.Count == 0 || selectedAudioFiles.Count == 0)
            {
                MessageBox.Show("Please select at least one executable and one FLAC audio file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                isExecuting = false; // Сбрасываем флаг, если нет файлов
                return;
            }
            ProcessPriorityClass priorityClass;
            switch (comboBoxCPUPriority.SelectedItem.ToString())
            {
                case "Low":
                    priorityClass = ProcessPriorityClass.Idle;
                    break;
                case "BelowNormal":
                    priorityClass = ProcessPriorityClass.BelowNormal;
                    break;
                case "Normal":
                    priorityClass = ProcessPriorityClass.Normal;
                    break;
                case "AboveNormal":
                    priorityClass = ProcessPriorityClass.AboveNormal;
                    break;
                case "High":
                    priorityClass = ProcessPriorityClass.High;
                    break;
                case "RealTime":
                    priorityClass = ProcessPriorityClass.RealTime;
                    break;
                default:
                    priorityClass = ProcessPriorityClass.Normal; // Значение по умолчанию
                    break;
            }
            foreach (var executable in selectedExecutables)
            {
                foreach (var audioFile in selectedAudioFiles)
                {
                    if (_isEncodingStopped)
                    {
                        isExecuting = false; // Сбрасываем флаг перед выходом
                        return; // Выходим, если остановка запроса
                    }
                    // Формируем строку с параметрами
                    string commandLine = NormalizeSpaces(textBoxCommandLineOptionsDecoder.Text).Trim();
                    // Формируем аргументы для запуска
                    string outputFilePath = Path.Combine(tempFolderPath, "temp_decoded.wav"); // Имя выходного файла
                    string arguments = $"\"{audioFile}\" -d {commandLine} -f -o \"{outputFilePath}\"";
                    // Запускаем процесс и дожидаемся завершения
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
                                // Запускаем отсчет времени
                                stopwatch.Reset();
                                stopwatch.Start();
                                if (!_isEncodingStopped)
                                {
                                    _process.Start();
                                    // Устанавливаем приоритет процесса, если он начал успешно
                                    try
                                    {
                                        if (!_process.HasExited)
                                        {
                                            _process.PriorityClass = priorityClass;
                                        }
                                    }
                                    catch (InvalidOperationException)
                                    {
                                        // Процесс завершён, логируем или обрабатываем по мере необходимости
                                    }
                                    if (!_process.HasExited)
                                    {
                                        _process.WaitForExit();
                                    }
                                    stopwatch.Stop();
                                }
                            }
                        });
                        // Условие: записывать в лог только если процесс не был остановлен
                        if (!_isEncodingStopped)
                        {
                            LogProcessResults(outputFilePath, audioFile, commandLine, executable);
                        }
                        else
                        {
                            // MessageBox.Show("Output file was not created.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            isExecuting = false; // Сбрасываем флаг перед возвратом
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error starting decoding process: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        isExecuting = false; // Сбрасываем флаг перед возвратом
                        return;
                    }
                }
            }
            isExecuting = false; // Сбрасываем флаг после завершения
        }

        // Log
        private void InitializedataGridViewLog()
        {
            // Настройка DataGridView
            dataGridViewLog.Columns.Add("FilePath", "File Path"); // Скрытый столбец для папки файла
            dataGridViewLog.Columns.Add("Name", "Name");
            dataGridViewLog.Columns.Add("InputFileSize", "In. Size");
            dataGridViewLog.Columns.Add("OutputFileSize", "Out. Size");
            dataGridViewLog.Columns.Add("Compression", "Compr.");
            dataGridViewLog.Columns.Add("Time", "Time");
            dataGridViewLog.Columns.Add("Speed", "Speed");
            dataGridViewLog.Columns.Add("Parameters", "Parameters");
            dataGridViewLog.Columns.Add("Executable", "Binary");
            dataGridViewLog.Columns.Add("Version", "Version");
            dataGridViewLog.Columns.Add("BestSize", "Best Size");
            dataGridViewLog.Columns.Add("SameSize", "Same Size");

            // Установка выравнивания для колонок
            dataGridViewLog.Columns["InputFileSize"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewLog.Columns["OutputFileSize"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewLog.Columns["Compression"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewLog.Columns["Time"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewLog.Columns["Speed"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

            // Скрываем столбец с полным путем
            dataGridViewLog.Columns["FilePath"].Visible = false;
        }
        private void LogProcessResults(string outputFilePath, string audioFile, string parameters, string executable)
        {
            FileInfo outputFile = new FileInfo(outputFilePath);
            if (outputFile.Exists)
            {
                // Создаем CultureInfo для форматирования с точками как разделителями разрядов
                NumberFormatInfo numberFormat = new CultureInfo("en-US").NumberFormat;
                numberFormat.NumberGroupSeparator = " ";

                // Получаем информацию о входящем аудиофайле
                FileInfo inputFileInfo = new FileInfo(audioFile);
                long inputSize = inputFileInfo.Length; // Размер входного файла
                var (duration, _, _) = GetAudioInfo(audioFile);
                long durationMs = Convert.ToInt64(duration);
                string inputSizeFormatted = inputSize.ToString("N0", numberFormat);

                // Получаем только имя входящего файла для логирования
                string audioFileName = Path.GetFileName(audioFile);
                // Получаем путь к директории файла
                string audioFileDirectory = Path.GetDirectoryName(audioFile) ?? string.Empty;

                // Формируем короткое имя входящего файла
                string audioFileNameShort = audioFileName.Length > 30
                ? $"{audioFileName.Substring(0, 15)}...{audioFileName.Substring(audioFileName.Length - 15)}"
                : audioFileName.PadRight(33);

                // Получаем информацию о выходящем аудиофайле
                long outputSize = outputFile.Length; // Размер выходного файла
                string outputSizeFormatted = outputSize.ToString("N0", numberFormat);
                TimeSpan timeTaken = stopwatch.Elapsed;
                double compressionPercentage = ((double)outputSize / inputSize) * 100;
                double encodingSpeed = (double)durationMs / timeTaken.TotalMilliseconds;
                // Получаем информацию о версии exe файла
                var version = GetExecutableInfo(executable);
                // Добавление записи в лог DataGridView
                int rowIndex = dataGridViewLog.Rows.Add(
                audioFileDirectory,
                audioFileName,
                inputSizeFormatted,
                outputSizeFormatted,
                $"{compressionPercentage:F3}%",
                $"{timeTaken.TotalMilliseconds:F3}",
                $"{encodingSpeed:F3}x",
                parameters,
                Path.GetFileName(executable),
                version);
                // Установка цвета текста в зависимости от сравнения размеров файлов
                dataGridViewLog.Rows[rowIndex].Cells[3].Style.ForeColor = outputSize < inputSize ? Color.Green : (outputSize > inputSize ? Color.Red : dataGridViewLog.Rows[rowIndex].Cells[2].Style.ForeColor);
                dataGridViewLog.Rows[rowIndex].Cells[4].Style.ForeColor = compressionPercentage < 100 ? Color.Green : (compressionPercentage > 100 ? Color.Red : dataGridViewLog.Rows[rowIndex].Cells[3].Style.ForeColor);
                dataGridViewLog.Rows[rowIndex].Cells[6].Style.ForeColor = encodingSpeed > 1 ? Color.Green : (encodingSpeed < 1 ? Color.Red : dataGridViewLog.Rows[rowIndex].Cells[5].Style.ForeColor);
                // Прокручиваем DataGridView вниз к последней добавленной строке
                dataGridViewLog.FirstDisplayedScrollingRowIndex = dataGridViewLog.Rows.Count - 1;
                dataGridViewLog.ClearSelection(); // Очищаем выделение
                // Логирование в файл
                File.AppendAllText("log.txt", $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {audioFileNameShort}\tInput size: {inputSize}\tOutput size: {outputSize} bytes\tCompression: {compressionPercentage:F3}%\tTime: {timeTaken.TotalMilliseconds:F3} ms\tSpeed: {encodingSpeed:F3}x\tParameters: {parameters.Trim()}\tBinary: {Path.GetFileName(executable)}\tVersion: {version}{Environment.NewLine}");
            }
        }
        private void buttonAnalyzeLog_Click(object sender, EventArgs e)
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
        }
        private void buttonLogToExcel_Click(object sender, EventArgs e)
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
                worksheet.Column(ParametersIndex).Style.NumberFormat.Format = "@"; // Формат для отображения парметров

                // Установка фильтра на заголовки
                worksheet.RangeUsed().SetAutoFilter();


                // Замораживаем первую строку (заголовки)
                worksheet.SheetView.FreezeRows(1);

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
                        UseShellExecute = true // Убедитесь, что используется оболочка
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

        private void buttonStop_Click(object sender, EventArgs e)
        {
            _isEncodingStopped = true; // Флаг о просьбе остановки кодирования
            if (_process != null)
            {
                try
                {
                    // Проверяем, запущен ли процесс
                    if (!_process.HasExited)
                    {
                        _process.Kill(); // Завершаем процесс
                        ShowTemporaryStoppedMessage("Encoding process has been stopped.");
                    }
                    else
                    {
                        ShowTemporaryStoppedMessage("The encoding process has already exited.");
                    }
                }
                catch (Exception ex)
                {
                    ShowTemporaryStoppedMessage($"Error stopping process: {ex.Message}");
                }
                finally
                {
                    _process.Dispose(); // Освобождаем ресурсы
                    _process = null; // Обнуляем ссылку на процесс
                }
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
            listViewAudioFiles.Columns[5].Width = 0; // Скрываем колонку MD5
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
            if (checkBoxClearTempFolder.Checked)
            {
                // Удаляем папку и все содержимое, если она существует
                if (Directory.Exists(tempFolderPath)) Directory.Delete(tempFolderPath, true);
            }
        }
    }
}