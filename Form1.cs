using System;
using System.Management;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Text;
using MediaInfoLib;
namespace FLAC_Benchmark_H
{
    public partial class Form1 : Form
    {
        private int physicalCores;
        private int threadCount;
        private Process _process; // Поле для хранения текущего процесса
        private const string SettingsFilePath = "Settings_general.txt"; // Путь к файлу настроек
        private const string JobsFilePath = "Settings_joblist.txt"; // Путь к файлу jobs
        private const string executablesFilePath = "Settings_flac_executables.txt"; // Путь к файлу для сохранения исполняемых файлов
        private const string audioFilesFilePath = "Settings_audio_files.txt"; // Путь к файлу для сохранения аудиофайлов
        private Stopwatch stopwatch;
        private PerformanceCounter cpuCounter;
        private System.Windows.Forms.Timer cpuUsageTimer; // Указываем явно, что это Timer из System.Windows.Forms
        private bool _isEncodingStopped = false;
        private string tempFolderPath; // Поле для хранения пути к временной папке


        public Form1()
        {
            InitializeComponent();
            InitializeDragAndDrop(); // Инициализация drag-and-drop
            this.FormClosing += Form1_FormClosing; // Регистрация обработчика события закрытия формы
            this.listViewFlacExecutables.KeyDown += ListViewFlacExecutables_KeyDown;
            this.listViewAudioFiles.KeyDown += ListViewAudioFiles_KeyDown;
            LoadCPUInfo(); // Загружаем информацию о процессоре
            this.KeyPreview = true;
            stopwatch = new Stopwatch(); // Инициализация объекта Stopwatch
            cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            cpuUsageTimer = new System.Windows.Forms.Timer(); // Явно указываем System.Windows.Forms.Timer
            cpuUsageTimer.Interval = 250; // Каждые 250 мс
            cpuUsageTimer.Tick += (sender, e) => UpdateCpuUsage();
            cpuUsageTimer.Start();
            InitializedataGridViewLog();
            tempFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp"); // Инициализация пути к временной папке


        }
        // Метод для загрузки информации о процессоре
        private void LoadCPUInfo()
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
                        physicalCores += int.Parse(obj["NumberOfCores"].ToString());
                        threadCount += int.Parse(obj["ThreadCount"].ToString());
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
            }
            catch (Exception ex)
            {
                // Записываем ошибку в labelCPUinfo
                labelCPUinfo.Text = "Error loading CPU info: " + ex.Message;
            }
        }
        private void UpdateCpuUsage()
        {
            float cpuUsage = cpuCounter.NextValue();
            labelCPUinfo.Text = $"Your system has:\nCores: {physicalCores}, Threads: {threadCount}\nCPU Usage: {cpuUsage:F2}%";
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
            LoadJobsQueue(); // Загружаем содержимое jobs.txt после загрузки других настроек
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
            $"TempFolderPath={tempFolderPath}" // Сохраняем путь к временной папке
        };
                File.WriteAllLines(SettingsFilePath, settings);
                SaveExecutables(); // Сохранение исполняемых файлов
                SaveAudioFiles(); // Сохранение аудиофайлов
                SaveJobsQueue(); // Сохраняем содержимое textBoxJobList
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving settings: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Загрузка исполняемых файлов
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
                            item.Tag = parts[0]; // Полный путь хранится в Tag
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
        // Сохранение исполняемых файлов
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
        // Новая структура для хранения элементов CheckedListBox с полными путями
        private void LoadJobsQueue()
        {
            // Создаем бэкап jobs.txt перед его загрузкой
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
                    File.Copy(JobsFilePath, backupPath, true); // Копируем файл, если такой уже существует, перезаписываем
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
            // Разрешаем перетаскивание файлов в ListView для программ
            listViewFlacExecutables.AllowDrop = true;
            listViewFlacExecutables.DragEnter += ListViewFlacExecutables_DragEnter;
            listViewFlacExecutables.DragDrop += ListViewFlacExecutables_DragDrop;
            // Разрешаем перетаскивание файлов в ListView для аудиофайлов
            listViewAudioFiles.AllowDrop = true;
            listViewAudioFiles.DragEnter += ListViewAudioFiles_DragEnter;
            listViewAudioFiles.DragDrop += ListViewAudioFiles_DragDrop;
            // Разрешаем перетаскивание файлов в TextBox для очереди задач
            textBoxJobList.AllowDrop = true;
            textBoxJobList.DragEnter += TextBoxJobList_DragEnter;
            textBoxJobList.DragDrop += TextBoxJobList_DragDrop;
        }
        // Обработчик DragEnter для ListViewFlacExecutables
        private void ListViewFlacExecutables_DragEnter(object sender, DragEventArgs e)
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
        private void ListViewFlacExecutables_DragDrop(object sender, DragEventArgs e)
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
                    var item = new ListViewItem(Path.GetFileName(file))
                    {
                        Tag = file,
                        Checked = true // Устанавливаем выделение по умолчанию
                    };
                    listViewFlacExecutables.Items.Add(item);
                }
            }
        }

        // Рекурсивный метод для добавления исполняемых файлов в ListView
        private void AddExecutableFiles(string directory)
        {
            try
            {
                // Находим все .exe файлы в текущей директории
                var exeFiles = Directory.GetFiles(directory, "*.exe", SearchOption.AllDirectories);
                foreach (var exeFile in exeFiles)
                {
                    var item = new ListViewItem(Path.GetFileName(exeFile))
                    {
                        Tag = exeFile,
                        Checked = true // Устанавливаем выделение по умолчанию
                    };
                    listViewFlacExecutables.Items.Add(item);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error accessing directory: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Обработчик DragEnter для ListViewAudioFiles
        private void ListViewAudioFiles_DragEnter(object sender, DragEventArgs e)
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
        private void ListViewAudioFiles_DragDrop(object sender, DragEventArgs e)
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

        // Рекурсивный метод для добавления аудиофайлов из директории
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
                            string audioFilePath = parts[0];
                            bool isChecked = bool.Parse(parts[1]);
                            AddAudioFileToListView(audioFilePath, isChecked); // Используем общий метод
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading audio files: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
        // Общее добавление аудиофайлов в ListView
        private void AddAudioFileToListView(string audioFile, bool isChecked = true)
        {
            var item = new ListViewItem(Path.GetFileName(audioFile))
            {
                Tag = audioFile,
                Checked = isChecked // Устанавливаем выделение по умолчанию
            };

            var (duration, bitDepth, samplingRate, fileSize) = GetAudioInfo(audioFile); // Получаем длительность, разрядность, частоту дискретизации и размер аудиофайла

            item.SubItems.Add(Convert.ToInt64(duration).ToString("N0") + " ms"); // Добавляем длительность в подэлемент
            item.SubItems.Add(bitDepth + " bit"); // Добавляем разрядность в подэлемент
            item.SubItems.Add(samplingRate); // Добавляем частоту дискретизации в подэлемент
            item.SubItems.Add(Convert.ToInt64(fileSize).ToString("N0") + " bytes"); // Добавляем размер в подэлемент с форматированием


            listViewAudioFiles.Items.Add(item); // Добавляем элемент в ListView
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

        // Обработчик DragEnter для TextBoxJobList
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
        // Обработчик DragDrop для TextBoxJobList
        private void TextBoxJobList_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            textBoxJobList.Clear(); // Очищаем textBox перед добавлением
            foreach (var file in files)
            {
                if (Path.GetExtension(file).Equals(".txt", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        string content = File.ReadAllText(file);
                        textBoxJobList.AppendText(content + Environment.NewLine); // Добавляем содержимое файла
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
        private void InitializedataGridViewLog()
        {
            // Настройка DataGridView (по желанию)
            dataGridViewLog.Columns.Add("FileName", "File Name");
            dataGridViewLog.Columns.Add("InputFileSize", "Input File Size");
            dataGridViewLog.Columns.Add("OutputFileSize", "Output File Size");
            dataGridViewLog.Columns.Add("Compression", "Compression");
            dataGridViewLog.Columns.Add("TimeTaken", "Time Taken");
            dataGridViewLog.Columns.Add("Executable", "Binary");
            dataGridViewLog.Columns.Add("Parameters", "Parameters");
            // Установка выравнивания для колонок
            dataGridViewLog.Columns["InputFileSize"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewLog.Columns["OutputFileSize"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewLog.Columns["TimeTaken"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridViewLog.Columns["Compression"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;





            listViewFlacExecutables.Columns.Add("FileName", "File Name");
            listViewFlacExecutables.Columns.Add("Version", "Version");
            listViewFlacExecutables.Columns.Add("Size", "Size");

        }
        // FORM LOAD
        private void Form1_Load(object sender, EventArgs e)
        {
            LoadSettings(); // Загрузка настроек

        }
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            SaveSettings(); // Сохранение настроек
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
                            Checked = true // Устанавливаем выделение
                        };
                        listViewFlacExecutables.Items.Add(item);
                    }
                }
            }
        }
        private void buttonRemoveEncoder_Click(object sender, EventArgs e)
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
        private void buttonClearEncoders_Click(object sender, EventArgs e)
        {
            listViewFlacExecutables.Items.Clear();
        }
        private void groupBoxAudioFiles_Enter(object sender, EventArgs e)
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
                        AddAudioFileToListView(file); // Используем общий метод
                    }
                }
            }
        }
        private void buttonRemoveAudiofile_Click(object sender, EventArgs e)
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
        private void buttonClearAudioFiles_Click(object sender, EventArgs e)
        {
            listViewAudioFiles.Items.Clear();
        }
        private void groupBoxEncoderSettings_Enter(object sender, EventArgs e)
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
            textBoxThreads.Text = (physicalCores / 2).ToString(); // Устанавливаем половину ядер
        }
        private void buttonSetMaxCores_Click(object sender, EventArgs e)
        {
            textBoxThreads.Text = physicalCores.ToString(); // Устанавливаем максимальное количество ядер
        }
        private void labelSetThreads_Click(object sender, EventArgs e)
        {
        }
        private void buttonSetHalfThreads_Click(object sender, EventArgs e)
        {
            textBoxThreads.Text = (threadCount / 2).ToString(); // Устанавливаем половину потоков
        }
        private void buttonSetMaxThreads_Click(object sender, EventArgs e)
        {
            textBoxThreads.Text = threadCount.ToString(); // Устанавливаем максимальное количество потоков
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
            textBoxCommandLineOptionsEncoder.Clear(); // Очищаем textCommandLineOptions
        }
        private void buttonepr8_Click(object sender, EventArgs e)
        {
            // Проверяем, содержится ли -epr8 в textBoxAdditionalArguments
            if (!textBoxCommandLineOptionsEncoder.Text.Contains("-epr8"))
            {
                // Если нет, добавляем его
                textBoxCommandLineOptionsEncoder.AppendText(" -epr8"); // Добавляем с пробелом перед текстом
            }
        }
        private void buttonAsubdividetukey5flattop_Click(object sender, EventArgs e)
        {
            // Проверяем, содержится ли -A "subdivide_tukey(5);flattop" в textBoxAdditionalArguments
            if (!textBoxCommandLineOptionsEncoder.Text.Contains("-A \"subdivide_tukey(5);flattop\""))
            {
                // Если нет, добавляем его
                textBoxCommandLineOptionsEncoder.AppendText(" -A \"subdivide_tukey(5);flattop\""); // Добавляем с пробелом перед текстом
            }
        }
        private void buttonNoPadding_Click(object sender, EventArgs e)
        {
            // Проверяем, содержится ли --no-padding в textBoxAdditionalArguments
            if (!textBoxCommandLineOptionsEncoder.Text.Contains("--no-padding"))
            {
                // Если нет, добавляем его
                textBoxCommandLineOptionsEncoder.AppendText(" --no-padding"); // Добавляем с пробелом перед текстом
            }
        }
        private void buttonNoSeektable_Click(object sender, EventArgs e)
        {
            // Проверяем, содержится ли --no-seektable в textBoxAdditionalArguments
            if (!textBoxCommandLineOptionsEncoder.Text.Contains("--no-seektable"))
            {
                // Если нет, добавляем его
                textBoxCommandLineOptionsEncoder.AppendText(" --no-seektable"); // Добавляем с пробелом перед текстом
            }
        }
        private async void buttonStartEncode_Click(object sender, EventArgs e)
        {
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
                return;
            }

            foreach (var executable in selectedExecutables)
            {
                foreach (var audioFile in selectedAudioFiles)
                {

                    if (_isEncodingStopped)
                    {
                        return; // Выходим из метода
                    }

                    // Получаем значения из текстовых полей
                    string compressionLevel = textBoxCompressionLevel.Text;
                    string threads = textBoxThreads.Text;
                    string commandLine = textBoxCommandLineOptionsEncoder.Text;

                    // Формируем аргументы для запуска
                    string outputFilePath = Path.Combine(tempFolderPath, "temp_encoded.flac"); // Имя выходного файла
                                                                                               // Формируем базовые аргументы
                    string arguments = $"\"{audioFile}\" -{compressionLevel} {commandLine}";

                    // Добавляем аргумент -j{threads} только если threads больше 1
                    if (int.TryParse(threads, out int threadCount) && threadCount > 1)
                    {
                        arguments += $" -j{threads}"; // Добавляем -j{threads}
                    }
                    arguments += $" -f -o \"{outputFilePath}\""; // Добавляем остальные аргументы

                    // Добавляем параметры (без входящих и исходящих файлов)
                    string parameters = $"-{compressionLevel} {commandLine}";
                    if (threadCount > 1)
                    {
                        parameters += $" -j{threads}";
                    }
                    // Запускаем процесс и дожидаемся завершения
                    try
                    {
                        FileInfo inputFileInfo = new FileInfo(audioFile);
                        long inputSize = inputFileInfo.Length; // Размер входного файла
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
                        // После завершения процесса проверяем размер выходного файла
                        FileInfo outputFile = new FileInfo(outputFilePath);
                        if (outputFile.Exists)
                        {
                            long outputSize = outputFile.Length; // Размер выходного файла
                            TimeSpan timeTaken = stopwatch.Elapsed;

                            // Вычисление процента сжатия
                            double compressionPercentage = ((double)outputSize / inputSize) * 100;

                            // Получаем только имя файла для логирования
                            string audioFileName = Path.GetFileName(audioFile);

                            // Получаем начало и окончание имени файла
                            string startName = audioFileName.Length > 22 ? audioFileName.Substring(0, 22) : audioFileName;
                            string endName = audioFileName.Length > 22 ? audioFileName.Substring(audioFileName.Length - 22) : "";

                            // Объединяем начало и окончание
                            string logFileName = startName + (string.IsNullOrEmpty(endName) ? "" : "...") + endName;

                            // Условие: записывать в лог только если процесс не был остановлен
                            if (!_isEncodingStopped)
                            {
                                // Добавляем запись в лог
                                var rowIndex = dataGridViewLog.Rows.Add(audioFileName, $"{inputSize:n0}", $"{outputSize:n0}", $"{compressionPercentage:F3}%", $"{timeTaken.TotalMilliseconds:F3}", Path.GetFileName(executable), parameters);

                                // Установка цвета текста в зависимости от сравнения размеров файлов
                                if (outputSize > inputSize)
                                {
                                    dataGridViewLog.Rows[rowIndex].Cells[2].Style.ForeColor = Color.Red; // Выходной размер больше
                                }
                                else if (outputSize < inputSize)
                                {
                                    dataGridViewLog.Rows[rowIndex].Cells[2].Style.ForeColor = Color.Green; // Выходной размер меньше
                                }

                                // Прокручиваем DataGridView вниз к последней добавленной строке
                                dataGridViewLog.FirstDisplayedScrollingRowIndex = dataGridViewLog.Rows.Count - 1;

                                // Логирование в файл
                                File.AppendAllText("log.txt", $"{DateTime.Now}: {logFileName}\tEncoded with: {Path.GetFileName(executable)}\tResulting FLAC size: {outputSize} bytes\tCompression: {compressionPercentage:F3}%\tTotal encoding time: {timeTaken.TotalMilliseconds:F3} ms\tParameters: {parameters.Trim()}{Environment.NewLine}");
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
        private async void buttonStartDecode_Click(object sender, EventArgs e)
        {
            // Устанавливаем флаг остановки
            _isEncodingStopped = false;
            // Создадим временную директорию для выходных файлов, если нужно
            Directory.CreateDirectory(tempFolderPath);
            // Получаем выделенные .exe файлы
            var selectedExecutables = listViewFlacExecutables.CheckedItems
            .Cast<ListViewItem>()
            .Select(item => item.Tag.ToString()) // Получаем полный путь из Tag
            .ToList();
            // Получаем выделенные аудиофайлы, но только с расширением .flac
            var selectedAudioFiles = listViewAudioFiles.CheckedItems
            .Cast<ListViewItem>()
            .Select(item => item.Tag.ToString())
            .Where(file => Path.GetExtension(file).Equals(".flac", StringComparison.OrdinalIgnoreCase)) // Только .wav файлы
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
                        return; // Выходим из метода
                    }
                    // Получаем значения из текстовых полей
                    string threads = textBoxThreads.Text;
                    string commandLine = textBoxCommandLineOptionsDecoder.Text;
                    // Формируем аргументы для запуска
                    string outputFileName = "temp_decoded.wav"; // Имя выходного файла
                    string outputFilePath = Path.Combine(tempFolderPath, outputFileName);
                    string arguments = $"\"{audioFile}\" -d {commandLine} -f -o \"{outputFilePath}\"";
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
                                stopwatch.Reset();
                                stopwatch.Start();
                                if (!_isEncodingStopped)
                                {
                                    _process.Start();
                                }
                                // Устанавливаем приоритет процесса
                                _process.PriorityClass = checkBoxHighPriority.Checked
                                ? ProcessPriorityClass.High
                                : ProcessPriorityClass.Normal;
                                _process.WaitForExit(); // Дожидаемся завершения процесса
                                stopwatch.Stop();
                            }
                        });
                        // После завершения процесса проверяем выходные файлы
                        FileInfo outputFile = new FileInfo(outputFilePath);
                        if (outputFile.Exists)
                        {
                            long fileSize = outputFile.Length;
                            TimeSpan timeTaken = stopwatch.Elapsed;
                            // Получаем только имя файла для логирования
                            string audioFileName = Path.GetFileName(audioFile);
                            if (!_isEncodingStopped)
                            {
                                // Записываем информацию в лог
                                dataGridViewLog.Rows.Add(audioFileName, fileSize, "", $"{timeTaken.TotalMilliseconds:F3}", Path.GetFileName(executable));
                                File.AppendAllText("log.txt", $"{audioFileName}\tdecoded with {Path.GetFileName(executable)}\tResulting file size: {fileSize} bytes\tTotal decoding time: {timeTaken.TotalMilliseconds:F3} ms\n");
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
                string fileName = $"Settings_joblist {DateTime.Now:yyyy-MM-dd}.txt"; // Формат YYYYMMDD
                saveFileDialog.FileName = fileName; // Устанавливаем начальное имя файла
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
            textBoxJobList.Clear(); // Очищаем textBoxJobList
        }
        private void groupLog_Enter(object sender, EventArgs e)
        {
        }
        private void textBoxLog_TextChanged(object sender, EventArgs e)
        {
        }
        private void buttonOpenLogtxt_Click(object sender, EventArgs e)
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
        private void buttonCopyLog_Click(object sender, EventArgs e)
        {
            // Копируем текст из textBoxLog в буфер обмена
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
            dataGridViewLog.Rows.Clear();
        }
        private void buttonStop_Click(object sender, EventArgs e)
        {
            // Устанавливаем флаг, что кодирование остановлено
            _isEncodingStopped = true;
            // Если процесс запущен, его нужно остановить
            if (_process != null && !_process.HasExited)
            {
                try
                {
                    _process.Kill(); // Завершаем процесс
                    _process.Dispose(); // Освобождаем ресурсы
                    _process = null; // Обнуляем процесс
                    MessageBox.Show("Encoding process has been stopped.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error stopping the process: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show("No encoding process is running.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        private void checkBoxClearTempFolder_CheckedChanged(object sender, EventArgs e)
        {
        }

        private void buttonSelectTempFolder_Click(object sender, EventArgs e)
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

    }
}