using MediaInfoLib;
using System.Diagnostics;
using System.Globalization;
using System.Management;
using System.Text;
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
            this.KeyPreview = true;
            stopwatch = new Stopwatch(); // Инициализация объекта Stopwatch
            cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            cpuUsageTimer = new System.Windows.Forms.Timer(); // Явно указываем System.Windows.Forms.Timer
            cpuUsageTimer.Interval = 250; // Каждые 250 мс
            cpuUsageTimer.Tick += async (sender, e) => await UpdateCpuUsageAsync();
            cpuUsageTimer.Start();
            InitializedataGridViewLog();
            tempFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp"); // Инициализация пути к временной папке
            _process = new Process(); // Initialize _process to avoid nullability warning
            this.buttonStartJobList.Click += new EventHandler(buttonStartJobList_Click);

            // Включаем пользовательскую отрисовку для listViewJobs
            listViewJobs.OwnerDraw = true;
            listViewJobs.DrawColumnHeader += ListViewJobs_DrawColumnHeader;
            listViewJobs.DrawSubItem += ListViewJobs_DrawSubItem;
        }
        private void ListViewJobs_DrawColumnHeader(object sender, DrawListViewColumnHeaderEventArgs e)
        {
            e.DrawDefault = true;
        }
        private void ListViewJobs_DrawSubItem(object sender, DrawListViewSubItemEventArgs e)
        {
            if (e.ColumnIndex == 0) // Колонка с типом задачи (Encode/Decode)
            {
                e.DrawBackground();

                // Отрисовка чекбокса
                if (listViewJobs.CheckBoxes)
                {
                    CheckBoxRenderer.DrawCheckBox(e.Graphics,
                        new Point(e.Bounds.Left + 4, e.Bounds.Top + 2),
                        e.Item.Checked ? System.Windows.Forms.VisualStyles.CheckBoxState.CheckedNormal
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
        // Метод для получения длительности и разрядности аудиофайла
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

        // Метод для сохранения настроек
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
                            case "CommandLineOptions":
                                textBoxCommandLineOptionsEncoder.Text = value;
                                break;
                            case "HighPriority":
                                checkBoxHighPriority.Checked = bool.Parse(value);
                                break;
                            case "TempFolderPath": // Загружаем путь к временной папке
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
            // Продолжение выполнения независимо от того, был ли загружен файл настроек
            LoadExecutables(); // Загрузка исполняемых файлов
            LoadAudioFiles(); // Загрузка аудиофайлов
            LoadJobs(); // Загружаем содержимое Settings_joblist.txt после загрузки других настроек
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
        private void SaveJobs()
        {
            try
            {
                var jobList = listViewJobs.Items.Cast<ListViewItem>()
                    .Select(item => $"{item.Text}~{item.Checked}~{item.SubItems[1].Text}") // Сохраняем текст, состояние чекбокса и параметры
                    .ToArray();
                File.WriteAllLines(JobsFilePath, jobList);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving jobs to file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

        // Обработчик DragEnter для ListViewFlacExecutables
        private void ListViewFlacExecutables_DragEnter(object? sender, DragEventArgs e)
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
        // Обработчик DragDrop для ListViewFlacExecutables
        private void ListViewFlacExecutables_DragDrop(object? sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            foreach (var file in files)
            {
                if (Directory.Exists(file)) // Если это папка, ищем исполняемые файлы внутри рекурсивно
                {
                    AddExecutableFiles(file);
                }
                else if (Path.GetExtension(file).Equals(".exe", StringComparison.OrdinalIgnoreCase))
                {
                    AddExecutableFileToListView(file); // Используем общий метод
                }
            }
        }
        // Рекурсивный метод для добавления исполняемых файлов в ListView
        private void AddExecutableFiles(string directory)
        {
            try
            {
                // Находим все аудиофайлы с заданными расширениями exe в текущей директории
                var exeFiles = Directory.GetFiles(directory, "*.exe", SearchOption.AllDirectories);
                foreach (var exeFile in exeFiles)
                {
                    AddExecutableFileToListView(exeFile); // Используем общий метод
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error accessing directory: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        // Загрузка исполняемых файлов из файла txt
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
                            string executablePath = parts[0]; // Полный путь
                            bool isChecked = bool.Parse(parts[1]); // Статус "выделено"
                            AddExecutableFileToListView(executablePath, isChecked); // Вызываем метод добавления
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
            var version = GetExecutableInfo(executable); // Получаем версию исполняемого файла
            long fileSize = new FileInfo(executable).Length; // Получаем размер файла
            DateTime lastModifiedDate = new FileInfo(executable).LastWriteTime; // Получаем дату изменения файла
            var item = new ListViewItem(Path.GetFileName(executable))
            {
                Tag = executable, // Полный путь хранится в Tag
                Checked = isChecked // Устанавливаем выделение по умолчанию
            };
            item.SubItems.Add(version); // Добавляем версию в вторую колонку
            item.SubItems.Add($"{fileSize:n0} bytes"); // Добавляем размер в третью колонку
            item.SubItems.Add(lastModifiedDate.ToString("yyyy.MM.dd HH:mm")); // Добавляем дату изменения в четвёртую колонку
            listViewFlacExecutables.Items.Add(item); // Добавляем элемент в ListView
        }

        // Обработчик DragEnter для ListViewAudioFiles
        private void ListViewAudioFiles_DragEnter(object? sender, DragEventArgs e)
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
        // Обработчик DragDrop для ListViewAudioFiles
        private void ListViewAudioFiles_DragDrop(object? sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            foreach (var file in files)
            {
                if (Directory.Exists(file)) // Если это папка, ищем аудиофайлы внутри рекурсивно
                {
                    AddAudioFiles(file);
                }
                else if (Path.GetExtension(file).Equals(".wav", StringComparison.OrdinalIgnoreCase) ||
                Path.GetExtension(file).Equals(".flac", StringComparison.OrdinalIgnoreCase))
                {
                    AddAudioFileToListView(file); // Используем общий метод
                }
            }
        }
        // Рекурсивный метод для добавления аудиофайлов из директории в ListView
        private void AddAudioFiles(string directory)
        {
            try
            {
                // Находим все аудиофайлы с заданными расширениями в текущей директории
                var audioFiles = Directory.GetFiles(directory, "*.wav", SearchOption.AllDirectories)
                .Concat(Directory.GetFiles(directory, "*.flac", SearchOption.AllDirectories));
                foreach (var audioFile in audioFiles)
                {
                    AddAudioFileToListView(audioFile); // Используем общий метод
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error accessing directory: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        // Метод для загрузки аудиофайлов из файла txt
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
                            string audioFilePath = parts[0]; // Полный путь
                            bool isChecked = bool.Parse(parts[1]); // Статус "выделено"
                            AddAudioFileToListView(audioFilePath, isChecked); // Вызываем метод добавления
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading audio files: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
        // Общий метод добавления аудиофайлов в ListView
        private void AddAudioFileToListView(string audioFile, bool isChecked = true)
        {
            var item = new ListViewItem(Path.GetFileName(audioFile))
            {
                Tag = audioFile, // Полный путь хранится в Tag
                Checked = isChecked // Устанавливаем выделение по умолчанию
            };
            var (duration, bitDepth, samplingRate, fileSize) = GetAudioInfo(audioFile); // Получаем информацию о файле
            item.SubItems.Add(Convert.ToInt64(duration).ToString("N0") + " ms"); // Длительность
            item.SubItems.Add(bitDepth + " bit"); // Разрядность
            item.SubItems.Add(samplingRate); // Частота дискретизации
            item.SubItems.Add(Convert.ToInt64(fileSize).ToString("N0") + " bytes"); // Размер файла
            listViewAudioFiles.Items.Add(item); // Добавляем элемент в ListView
        }

        // Обработчик DragEnter для ListViewJobs
        private void ListViewJobs_DragEnter(object? sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                bool hasTxtFiles = files.Any(file =>
                    Directory.Exists(file) ||
                    Path.GetExtension(file).Equals(".txt", StringComparison.OrdinalIgnoreCase));
                e.Effect = hasTxtFiles ? DragDropEffects.Copy : DragDropEffects.None;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }
        // Обработчик DragDrop для ListViewJobs
        private void ListViewJobs_DragDrop(object? sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            foreach (var file in files)
            {
                if (Directory.Exists(file)) // Если это папка, ищем .txt файлы внутри рекурсивно
                {
                    AddJobsFromDirectory(file);
                }
                else if (Path.GetExtension(file).Equals(".txt", StringComparison.OrdinalIgnoreCase))
                {
                    LoadJobsFromFile(file); // Используем общий метод
                }
            }
        }
        // Рекурсивный метод для добавления задач из директории в ListView
        private void AddJobsFromDirectory(string directory)
        {
            try
            {
                // Находим все .txt файлы с заданным расширением в текущей директории
                var txtFiles = Directory.GetFiles(directory, "*.txt", SearchOption.AllDirectories);
                foreach (var txtFile in txtFiles)
                {
                    LoadJobsFromFile(txtFile); // Загружаем задачи из найденного .txt файла
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error accessing directory: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        // Метод загрузки задач из файла
        private void LoadJobsFromFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                MessageBox.Show("The specified file does not exist.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                string[] lines = File.ReadAllLines(filePath);
                //   listViewJobs.Items.Clear(); // Очищаем существующие элементы перед загрузкой новых

                foreach (var line in lines)
                {
                    var parts = line.Split('~'); // Разделяем строку на части
                    if (parts.Length == 3 && bool.TryParse(parts[1], out bool isChecked))
                    {
                        string jobName = parts[0];
                        string parameters = parts[2];
                        AddJobsToListView(jobName, isChecked, parameters); // Добавляем задачу в ListView
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

        // Метод для загрузки задач из файла txt
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
                        var parts = line.Split('~'); // Разделяем текст на текст, состояние чекбокса и параметры
                        if (parts.Length == 3 && bool.TryParse(parts[1], out bool isChecked))
                        {
                            var item = new ListViewItem(parts[0]) { Checked = isChecked }; // Устанавливаем состояние чекбокса
                            item.SubItems.Add(parts[2]); // Вторая колонка: параметры
                            listViewJobs.Items.Add(item);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading jobs from file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
        // Общий метод добавления задач в ListView
        private void AddJobsToListView(string job, bool isChecked = true, string parameters = "")
        {
            var item = new ListViewItem(job)
            {
                Checked = isChecked // Устанавливаем выделение по умолчанию 
            };

            item.SubItems.Add(parameters); // Добавляем параметры как второй элемент 
            listViewJobs.Items.Add(item); // Добавляем элемент в ListView
        }

        private void ListViewFlacExecutables_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
                buttonRemoveEncoder.PerformClick();
        }
        private void ListViewAudioFiles_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
                buttonRemoveAudiofile.PerformClick();
        }
        private void ListViewJobs_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
                buttonRemoveJob.PerformClick();
        }
        private void InitializedataGridViewLog()
        {
            // Настройка DataGridView (по желанию)
            dataGridViewLog.Columns.Add("FileName", "Name");
            dataGridViewLog.Columns.Add("InputFileSize", "In. Size");
            dataGridViewLog.Columns.Add("OutputFileSize", "Out. Size");
            dataGridViewLog.Columns.Add("Compression", "Compr.");
            dataGridViewLog.Columns.Add("Time", "Time");
            dataGridViewLog.Columns.Add("Speed", "Speed");
            dataGridViewLog.Columns.Add("Parameters", "Parameters");
            dataGridViewLog.Columns.Add("Executable", "Binary");
            dataGridViewLog.Columns.Add("Version", "Version");
            // Установка выравнивания для колонок
            dataGridViewLog.Columns["InputFileSize"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewLog.Columns["OutputFileSize"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewLog.Columns["Compression"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewLog.Columns["Time"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewLog.Columns["Speed"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        }
        // FORM LOAD
        private void Form1_Load(object? sender, EventArgs e)
        {
            LoadSettings(); // Загрузка настроек
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
        private void buttonAddAudioFiles_Click(object? sender, EventArgs e)
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
                        AddAudioFileToListView(file); // Используем общий метод
                    }
                }
            }
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
        private async void buttonStartEncode_Click(object? sender, EventArgs e)
        {
            if (isExecuting)
            {
                // Если уже выполняется, начинаем остановку
                _isEncodingStopped = true;
                buttonStartEncode.Text = "Stopping..."; // Возможно добавить текст "Stopping..."
                return;
            }

            isExecuting = true; // Устанавливаем флаг выполнения
            // Устанавливаем флаг остановки
            _isEncodingStopped = false;
            buttonStartEncode.Text = "Stop"; // Меняем текст кнопки на "Stop"
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
                buttonStartEncode.Text = "Encode"; // Меняем текст кнопки обратно
                return;
            }
            foreach (var executable in selectedExecutables)
            {
                foreach (var audioFile in selectedAudioFiles)
                {
                    if (_isEncodingStopped)
                    {
                        isExecuting = false; // Сбрасываем флаг перед выходом
                        buttonStartEncode.Text = "Encode"; // Меняем текст кнопки обратно
                        return; // Выходим, если остановка запроса
                    }

                    // Получаем значения из текстовых полей и формируем аргументы...
                    string compressionLevel = textBoxCompressionLevel.Text;
                    string threads = textBoxThreads.Text;
                    string commandLine = textBoxCommandLineOptionsEncoder.Text;

                    // Формируем строку с параметрами
                    string parameters = $"-{compressionLevel} {commandLine}";

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
                                if (!_isEncodingStopped) // Добавляем проверку перед запуском
                                {
                                    _process.Start();
                                }
                                // Устанавливаем приоритет процесса на высокий, если чекбокс включен
                                _process.PriorityClass = checkBoxHighPriority.Checked
                                ? ProcessPriorityClass.High
                                : ProcessPriorityClass.Normal;
                                _process.WaitForExit(); // Дождаться завершения процесса
                                stopwatch.Stop();
                            }
                        });
                        // Условие: записывать в лог только если процесс не был остановлен
                        if (!_isEncodingStopped)
                        {


                            FileInfo outputFile = new FileInfo(outputFilePath);
                            if (outputFile.Exists)
                            {


                                // Создаем CultureInfo для форматирования с точками как разделителями разрядов
                                NumberFormatInfo numberFormat = new CultureInfo("en-US").NumberFormat;
                                numberFormat.NumberGroupSeparator = ".";

                                // Получаем информацию о входящем аудиофайле
                                FileInfo inputFileInfo = new FileInfo(audioFile);
                                long inputSize = inputFileInfo.Length; // Размер входного файла
                                var (duration, _, _, _) = GetAudioInfo(audioFile);
                                long durationMs = Convert.ToInt64(duration);
                                string inputSizeFormatted = inputSize.ToString("N0", numberFormat);

                                // Получаем только имя входящего файла для логирования
                                string audioFileName = Path.GetFileName(audioFile);
                                // Формируем короткое имя входящего файла
                                string audioFileNameShort;
                                if (audioFileName.Length > 30) // Если имя файла длиннее 30 символов
                                {
                                    string startName = audioFileName.Substring(0, 15);
                                    string endName = audioFileName.Substring(audioFileName.Length - 15);
                                    audioFileNameShort = $"{startName}...{endName}";
                                }
                                else
                                {
                                    // Если имя файла 30 символов или меньше, используем его целиком и добавляем пробелы до 30
                                    audioFileNameShort = audioFileName + new string(' ', 33 - audioFileName.Length);
                                }

                                // Получаем информацию о выходящем аудиофайле
                                long outputSize = outputFile.Length; // Размер выходного файла
                                string outputSizeFormatted = outputSize.ToString("N0", numberFormat);
                                TimeSpan timeTaken = stopwatch.Elapsed;
                                // Вычисление процента сжатия
                                double compressionPercentage = ((double)outputSize / inputSize) * 100;
                                // Рассчитываем отношение скорости кодирования к длительности
                                double encodingSpeed = (double)durationMs / timeTaken.TotalMilliseconds;

                                // Получаем информацию о версии exe файла
                                var version = GetExecutableInfo(executable);
                                // Добавление записи в лог DataGridView
                                int rowIndex = dataGridViewLog.Rows.Add(
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
                                dataGridViewLog.Rows[rowIndex].Cells[2].Style.ForeColor = outputSize < inputSize ? Color.Green : (outputSize > inputSize ? Color.Red : dataGridViewLog.Rows[rowIndex].Cells[2].Style.ForeColor);
                                dataGridViewLog.Rows[rowIndex].Cells[3].Style.ForeColor = compressionPercentage < 100 ? Color.Green : (compressionPercentage > 100 ? Color.Red : dataGridViewLog.Rows[rowIndex].Cells[3].Style.ForeColor);
                                // Установка цвета текста для скорости кодирования
                                dataGridViewLog.Rows[rowIndex].Cells[5].Style.ForeColor = encodingSpeed > 1 ? Color.Green : (encodingSpeed < 1 ? Color.Red : dataGridViewLog.Rows[rowIndex].Cells[5].Style.ForeColor);
                                // Очищаем выделение, чтобы убрать фокус с первой строки
                                dataGridViewLog.ClearSelection();
                                // Логирование в файл
                                File.AppendAllText("log.txt", $"{DateTime.Now}: {audioFileNameShort}\tInput size: {inputSize}\tOutput size: {outputSize} bytes\tCompression: {compressionPercentage:F3}%\tTime: {timeTaken.TotalMilliseconds:F3} ms\tEncoding Speed: {encodingSpeed:F3}x\tParameters: {parameters.Trim()}\tEncoded with: {Path.GetFileName(executable)}\tVersion: {version}{Environment.NewLine}");
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

            // Сбрасываем флаг выполнения после завершения
            isExecuting = false;
            buttonStartEncode.Text = "Encode"; // Меняем текст кнопки обратно
        }
        private async void buttonStartDecode_Click(object? sender, EventArgs e)
        {
            if (isExecuting)
            {
                // Если уже выполняется, начинаем остановку
                _isEncodingStopped = true;
                buttonStartDecode.Text = "Stopping...";
                return;
            }

            isExecuting = true; // Устанавливаем флаг выполнения
            // Устанавливаем флаг остановки
            _isEncodingStopped = false;
            buttonStartDecode.Text = "Stop"; // Меняем текст кнопки на "Stop"
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
                buttonStartDecode.Text = "Decode"; // Меняем текст кнопки обратно
                return;
            }
            foreach (var executable in selectedExecutables)
            {
                foreach (var audioFile in selectedAudioFiles)
                {
                    if (_isEncodingStopped)
                    {
                        isExecuting = false; // Сбрасываем флаг перед выходом
                        buttonStartDecode.Text = "Decode"; // Меняем текст кнопки обратно
                        return; // Выходим, если остановка запроса
                    }

                    // Получаем значения из текстовых полей и формируем аргументы...
                    string commandLine = textBoxCommandLineOptionsDecoder.Text;
                    // Формируем строку с параметрами
                    string parameters = $"{commandLine}";
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
                                if (!_isEncodingStopped) // Добавляем проверку перед запуском
                                {
                                    _process.Start();
                                }
                                // Устанавливаем приоритет процесса на высокий, если чекбокс включен
                                _process.PriorityClass = checkBoxHighPriority.Checked
                                ? ProcessPriorityClass.High
                                : ProcessPriorityClass.Normal;
                                _process.WaitForExit(); // Дождаться завершения процесса
                                stopwatch.Stop();
                            }
                        });
                        // Условие: записывать в лог только если процесс не был остановлен
                        if (!_isEncodingStopped)
                        {
                            FileInfo outputFile = new FileInfo(outputFilePath);
                            if (outputFile.Exists)
                            {
                                // Создаем CultureInfo для форматирования с точками как разделителями разрядов
                                NumberFormatInfo numberFormat = new CultureInfo("en-US").NumberFormat;
                                numberFormat.NumberGroupSeparator = ".";

                                // Получаем информацию о входящем аудиофайле
                                FileInfo inputFileInfo = new FileInfo(audioFile);
                                long inputSize = inputFileInfo.Length; // Размер входного файла
                                var (duration, _, _, _) = GetAudioInfo(audioFile);
                                long durationMs = Convert.ToInt64(duration);
                                string inputSizeFormatted = inputSize.ToString("N0", numberFormat);

                                // Получаем только имя входящего файла для логирования
                                string audioFileName = Path.GetFileName(audioFile);
                                // Формируем короткое имя входящего файла
                                string audioFileNameShort;
                                if (audioFileName.Length > 30) // Если имя файла длиннее 30 символов
                                {
                                    string startName = audioFileName.Substring(0, 15);
                                    string endName = audioFileName.Substring(audioFileName.Length - 15);
                                    audioFileNameShort = $"{startName}...{endName}";
                                }
                                else
                                {
                                    // Если имя файла 30 символов или меньше, используем его целиком и добавляем пробелы до 30
                                    audioFileNameShort = audioFileName + new string(' ', 33 - audioFileName.Length);
                                }
                                // Получаем информацию о выходящем аудиофайле
                                long outputSize = outputFile.Length; // Размер выходного файла
                                string outputSizeFormatted = outputSize.ToString("N0", numberFormat);
                                TimeSpan timeTaken = stopwatch.Elapsed;
                                // Рассчитываем отношение скорости декодирования к длительности
                                double decodingSpeed = (double)durationMs / timeTaken.TotalMilliseconds;
                                // Получаем информацию о версии exe файла
                                var version = GetExecutableInfo(executable);
                                // Добавление записи в лог DataGridView
                                int rowIndex = dataGridViewLog.Rows.Add(
                                audioFileName,
                                inputSizeFormatted,
                                outputSizeFormatted,
                                "",
                                $"{timeTaken.TotalMilliseconds:F3}",
                                $"{decodingSpeed:F3}x",
                                parameters,
                                Path.GetFileName(executable),
                                version);
                                // Установка цвета текста в зависимости от сравнения размеров файлов
                                dataGridViewLog.Rows[rowIndex].Cells[2].Style.ForeColor = outputSize < inputSize ? Color.Green : (outputSize > inputSize ? Color.Red : dataGridViewLog.Rows[rowIndex].Cells[2].Style.ForeColor);
                                // Установка цвета текста для скорости декодирования
                                dataGridViewLog.Rows[rowIndex].Cells[5].Style.ForeColor = decodingSpeed > 1 ? Color.Green : (decodingSpeed < 1 ? Color.Red : dataGridViewLog.Rows[rowIndex].Cells[5].Style.ForeColor);
                                // Прокручиваем DataGridView вниз к последней добавленной строке
                                dataGridViewLog.FirstDisplayedScrollingRowIndex = dataGridViewLog.Rows.Count - 1;
                                // Очищаем выделение, чтобы убрать фокус с первой строки
                                dataGridViewLog.ClearSelection();
                                // Логирование в файл
                                File.AppendAllText("log.txt", $"{DateTime.Now}: {audioFileNameShort}\tInput size: {inputSize}\tOutput size: {outputSize} bytes\t\tTime: {timeTaken.TotalMilliseconds:F3} ms\tDecoding Speed: {decodingSpeed:F3}x\tParameters: {parameters.Trim()}\tDecoded with: {Path.GetFileName(executable)}\tVersion: {version}{Environment.NewLine}");
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
            // Сбрасываем флаг выполнения после завершения
            isExecuting = false;
            buttonStartDecode.Text = "Decode"; // Меняем текст кнопки обратно
        }

        private void buttonImportJobList_Click(object? sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";
                openFileDialog.Title = "Open Job List";
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        string[] lines = File.ReadAllLines(openFileDialog.FileName); // Используем выбранный файл
                                                                                     //    listViewJobs.Items.Clear(); // Очищаем список перед загрузкой новых

                        foreach (var line in lines)
                        {
                            var parts = line.Split('~'); // Разделяем строку на части
                            if (parts.Length == 3 && bool.TryParse(parts[1], out bool isChecked))
                            {
                                string jobName = parts[0];
                                string parameters = parts[2];
                                AddJobsToListView(jobName, isChecked, parameters); // Добавляем задачу в ListView
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
            }
        }
        private void buttonExportJobList_Click(object? sender, EventArgs e)
        {
            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";
                saveFileDialog.Title = "Save Job List";
                string fileName = $"Settings_joblist {DateTime.Now:yyyy-MM-dd}.txt"; // Формат YYYY-MM-DD
                saveFileDialog.FileName = fileName; // Устанавливаем начальное имя файла
                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        var jobList = listViewJobs.Items.Cast<ListViewItem>()
                            .Select(item => $"{item.Text}~{item.Checked}~{item.SubItems[1].Text}") // Получаем текст, состояние чекбокса и параметры
                            .ToArray(); // Сохраняем в одном формате
                        File.WriteAllLines(saveFileDialog.FileName, jobList);
                        //    MessageBox.Show("Job list exported successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error exporting job list: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
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
            listViewJobs.Items.Clear(); // Очищаем listViewJobList
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

        private void buttonAddJobToJobListEncoder_Click(object sender, EventArgs e)
        {
            // Получаем значения из текстовых полей и формируем параметры
            string compressionLevel = textBoxCompressionLevel.Text;
            string threads = textBoxThreads.Text;
            string commandLine = textBoxCommandLineOptionsEncoder.Text;

            // Формируем строку с параметрами
            string parameters = $"-{compressionLevel} {commandLine}";

            // Добавляем количество потоков, если оно больше 1
            if (int.TryParse(threads, out int threadCount) && threadCount > 1)
            {
                parameters += $" -j{threads}"; // добавляем флаг -j{threads}
            }

            // Создаем новый элемент списка для кодирования
            var item = new ListViewItem("Encode") // Первая колонка - Encode
            {
                Checked = true // Устанавливаем чекбокс в состояние "проверено"
            };
            item.SubItems.Add(parameters); // Вторая колонка - parameters

            // Добавляем элемент в listViewJobList
            listViewJobs.Items.Add(item);
        }
        private void buttonAddJobToJobListDecoder_Click(object sender, EventArgs e)
        {
            // Получаем значения из текстовых полей и формируем параметры
            string commandLine = textBoxCommandLineOptionsDecoder.Text;

            // Формируем строку с параметрами
            string parameters = commandLine; // Параметры для декодирования

            // Создаем новый элемент списка для декодирования
            var item = new ListViewItem("Decode") // Первая колонка - Decode
            {
                Checked = true // Устанавливаем чекбокс в состояние "проверено"
            };
            item.SubItems.Add(parameters); // Вторая колонка - parameters

            // Добавляем элемент в listViewJobList
            listViewJobs.Items.Add(item);
        }

        private void buttonCopyJobs_Click(object sender, EventArgs e)
        {
            StringBuilder jobsText = new StringBuilder();

            // Проверяем, есть ли выделенные элементы
            if (listViewJobs.SelectedItems.Count > 0)
            {
                // Копируем только выделенные задачи
                foreach (ListViewItem item in listViewJobs.SelectedItems)
                {
                    jobsText.AppendLine($"{item.Text}~{item.Checked}~{item.SubItems[1].Text}");
                }
            }
            else
            {
                // Если ничего не выделено, копируем все задачи
                foreach (ListViewItem item in listViewJobs.Items)
                {
                    jobsText.AppendLine($"{item.Text}~{item.Checked}~{item.SubItems[1].Text}");
                }
            }

            // Копируем текст в буфер обмена
            if (jobsText.Length > 0)
            {
                Clipboard.SetText(jobsText.ToString());
                //    MessageBox.Show("Jobs copied to clipboard.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
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

                // Проверяем, если буфер не пустой
                if (!string.IsNullOrEmpty(clipboardText))
                {
                    string[] lines = clipboardText.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries); // Разделяем на строки

                    foreach (var line in lines)
                    {
                        var parts = line.Split('~'); // Разделяем строку на части
                        if (parts.Length == 3 && bool.TryParse(parts[1], out bool isChecked))
                        {
                            string jobName = parts[0];
                            string parameters = parts[2];
                            AddJobsToListView(jobName, isChecked, parameters); // Добавляем задачу в ListView
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

        private async void buttonStartJobList_Click(object sender, EventArgs e)
        {
            if (isExecuting) return; // Проверяем, выполняется ли уже процесс

            isExecuting = true; // Устанавливаем флаг выполнения
            foreach (ListViewItem item in listViewJobs.Items)
            {
                // Проверяем, отмечена ли задача
                if (item.Checked)
                {
                    string jobType = item.Text;

                    if (string.Equals(jobType, "Encode", StringComparison.OrdinalIgnoreCase))
                    {
                        // Устанавливаем флаг остановки
                        _isEncodingStopped = false;
                        // Создаём временную директорию для выходного файла
                        Directory.CreateDirectory(tempFolderPath);

                        // Получаем выделенные .exe файлы
                        var selectedExecutables = listViewFlacExecutables.CheckedItems
                            .Cast<ListViewItem>()
                            .Select(i => i.Tag.ToString()) // Получаем полный путь из Tag
                            .ToList();

                        // Получаем выделенные аудиофайлы
                        var selectedAudioFiles = listViewAudioFiles.CheckedItems
                            .Cast<ListViewItem>()
                            .Select(i => i.Tag.ToString()) // Получаем полный путь из Tag
                            .ToList();

                        // Проверяем, есть ли выбранные исполняемые файлы и аудиофайлы
                        if (selectedExecutables.Count == 0 || selectedAudioFiles.Count == 0)
                        {
                            MessageBox.Show("Please select at least one executable and one audio file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            isExecuting = false; // Сбрасываем флаг перед возвратом
                            return;
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

                                // Получаем значения из текстовых полей и формируем аргументы...
                                string compressionLevel = textBoxCompressionLevel.Text;
                                string threads = textBoxThreads.Text;
                                string commandLine = textBoxCommandLineOptionsEncoder.Text;

                                // Формируем строку с параметрами
                                string parameters = item.SubItems[1].Text;

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
                                                        _process.PriorityClass = checkBoxHighPriority.Checked
                                                            ? ProcessPriorityClass.High
                                                            : ProcessPriorityClass.Normal;
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
                                        FileInfo outputFile = new FileInfo(outputFilePath);
                                        if (outputFile.Exists)
                                        {
                                            // Создаем CultureInfo для форматирования с точками как разделителями разрядов
                                            NumberFormatInfo numberFormat = new CultureInfo("en-US").NumberFormat;
                                            numberFormat.NumberGroupSeparator = ".";

                                            // Получаем информацию о входящем аудиофайле
                                            FileInfo inputFileInfo = new FileInfo(audioFile);
                                            long inputSize = inputFileInfo.Length; // Размер входного файла
                                            var (duration, _, _, _) = GetAudioInfo(audioFile);
                                            long durationMs = Convert.ToInt64(duration);
                                            string inputSizeFormatted = inputSize.ToString("N0", numberFormat);

                                            // Получаем только имя входящего файла для логирования
                                            string audioFileName = Path.GetFileName(audioFile);
                                            // Формируем короткое имя входящего файла
                                            string audioFileNameShort = audioFileName.Length > 30
                                                ? $"{audioFileName.Substring(0, 15)}...{audioFileName.Substring(audioFileName.Length - 15)}"
                                                : audioFileName.PadRight(33);

                                            // Получаем информацию о выходящем аудиофайле
                                            long outputSize = outputFile.Length; // Размер выходного файла
                                            string outputSizeFormatted = outputSize.ToString("N0", numberFormat);
                                            TimeSpan timeTaken = stopwatch.Elapsed;

                                            // Вычисление процента сжатия
                                            double compressionPercentage = ((double)outputSize / inputSize) * 100;
                                            // Рассчитываем отношение скорости кодирования к длительности
                                            double encodingSpeed = (double)durationMs / timeTaken.TotalMilliseconds;

                                            // Получаем информацию о версии exe файла
                                            var version = GetExecutableInfo(executable);
                                            // Добавление записи в лог DataGridView
                                            int rowIndex = dataGridViewLog.Rows.Add(
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
                                            dataGridViewLog.Rows[rowIndex].Cells[2].Style.ForeColor = outputSize < inputSize ? Color.Green : (outputSize > inputSize ? Color.Red : dataGridViewLog.Rows[rowIndex].Cells[2].Style.ForeColor);
                                            dataGridViewLog.Rows[rowIndex].Cells[3].Style.ForeColor = compressionPercentage < 100 ? Color.Green : (compressionPercentage > 100 ? Color.Red : dataGridViewLog.Rows[rowIndex].Cells[3].Style.ForeColor);
                                            // Установка цвета текста для скорости кодирования
                                            dataGridViewLog.Rows[rowIndex].Cells[5].Style.ForeColor = encodingSpeed > 1 ? Color.Green : (encodingSpeed < 1 ? Color.Red : dataGridViewLog.Rows[rowIndex].Cells[5].Style.ForeColor);
                                            // Прокручиваем DataGridView вниз к последней добавленной строке
                                            dataGridViewLog.FirstDisplayedScrollingRowIndex = dataGridViewLog.Rows.Count - 1;
                                            // Очищаем выделение, чтобы убрать фокус с первой строки
                                            dataGridViewLog.ClearSelection();
                                            // Логирование в файл
                                            File.AppendAllText("log.txt", $"{DateTime.Now}: {audioFileNameShort}\tInput size: {inputSize}\tOutput size: {outputSize} bytes\tCompression: {compressionPercentage:F3}%\tTime: {timeTaken.TotalMilliseconds:F3} ms\tEncoding Speed: {encodingSpeed:F3}x\tParameters: {parameters.Trim()}\tEncoded with: {Path.GetFileName(executable)}\tVersion: {version}{Environment.NewLine}");
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
                    else if (string.Equals(jobType, "Decode", StringComparison.OrdinalIgnoreCase))
                    {
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
                            return;
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
                                string parameters = item.SubItems[1].Text;
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
                                                        _process.PriorityClass = checkBoxHighPriority.Checked
                                                            ? ProcessPriorityClass.High
                                                            : ProcessPriorityClass.Normal;
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
                                        FileInfo outputFile = new FileInfo(outputFilePath);
                                        if (outputFile.Exists)
                                        {
                                            // Создаем CultureInfo для форматирования с точками как разделителями разрядов
                                            NumberFormatInfo numberFormat = new CultureInfo("en-US").NumberFormat;
                                            numberFormat.NumberGroupSeparator = ".";

                                            // Получаем информацию о входящем аудиофайле
                                            FileInfo inputFileInfo = new FileInfo(audioFile);
                                            long inputSize = inputFileInfo.Length; // Размер входного файла
                                            var (duration, _, _, _) = GetAudioInfo(audioFile);
                                            long durationMs = Convert.ToInt64(duration);
                                            string inputSizeFormatted = inputSize.ToString("N0", numberFormat);

                                            // Получаем только имя входящего файла для логирования
                                            string audioFileName = Path.GetFileName(audioFile);
                                            // Формируем короткое имя входящего файла
                                            string audioFileNameShort = audioFileName.Length > 30
                                                ? $"{audioFileName.Substring(0, 15)}...{audioFileName.Substring(audioFileName.Length - 15)}"
                                                : audioFileName.PadRight(33);

                                            // Получаем информацию о выходящем аудиофайле
                                            long outputSize = outputFile.Length; // Размер выходного файла
                                            string outputSizeFormatted = outputSize.ToString("N0", numberFormat);
                                            TimeSpan timeTaken = stopwatch.Elapsed;

                                            // Рассчитываем отношение скорости декодирования к длительности
                                            double decodingSpeed = (double)durationMs / timeTaken.TotalMilliseconds;
                                            // Получаем информацию о версии exe файла
                                            var version = GetExecutableInfo(executable);
                                            // Добавление записи в лог DataGridView
                                            int rowIndex = dataGridViewLog.Rows.Add(
                                                audioFileName,
                                                inputSizeFormatted,
                                                outputSizeFormatted,
                                                "",
                                                $"{timeTaken.TotalMilliseconds:F3}",
                                                $"{decodingSpeed:F3}x",
                                                parameters,
                                                Path.GetFileName(executable),
                                                version);
                                            // Установка цвета текста в зависимости от сравнения размеров файлов
                                            dataGridViewLog.Rows[rowIndex].Cells[2].Style.ForeColor = outputSize < inputSize ? Color.Green : (outputSize > inputSize ? Color.Red : dataGridViewLog.Rows[rowIndex].Cells[2].Style.ForeColor);
                                            // Установка цвета текста для скорости декодирования
                                            dataGridViewLog.Rows[rowIndex].Cells[5].Style.ForeColor = decodingSpeed > 1 ? Color.Green : (decodingSpeed < 1 ? Color.Red : dataGridViewLog.Rows[rowIndex].Cells[5].Style.ForeColor);
                                            // Прокручиваем DataGridView вниз к последней добавленной строке
                                            dataGridViewLog.FirstDisplayedScrollingRowIndex = dataGridViewLog.Rows.Count - 1;
                                            // Очищаем выделение, чтобы убрать фокус с первой строки
                                            dataGridViewLog.ClearSelection();

                                            // Логирование в файл
                                            File.AppendAllText("log.txt", $"{DateTime.Now}: {audioFileNameShort}\tInput size: {inputSize}\tOutput size: {outputSize} bytes\t\tTime: {timeTaken.TotalMilliseconds:F3} ms\tDecoding Speed: {decodingSpeed:F3}x\tParameters: {parameters.Trim()}\tDecoded with: {Path.GetFileName(executable)}\tVersion: {version}{Environment.NewLine}");
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
                }
            }

            isExecuting = false; // Сбрасываем флаг после завершения
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
                        MessageBox.Show("Encoding process has been stopped.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show("The encoding process has already exited.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                catch (Exception ex)
                {
                    //    MessageBox.Show($"Error stopping process: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    _process.Dispose(); // Освобождаем ресурсы
                    _process = null; // Обнуляем ссылку на процесс
                }
            }
        }


    }
}