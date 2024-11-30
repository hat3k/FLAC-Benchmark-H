using System;
using System.Management;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Text;
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
                    labelCPUinfo.Text = $"Your system has: Cores: {physicalCores}, Threads: {threadCount}";
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
            labelCPUinfo.Text = $"Your system has: Cores: {physicalCores}, Threads: {threadCount} CPU Usage: {cpuUsage:F2}%";
        }
        // Метод для загрузки настроек
        private void LoadSettings()
        {
            if (File.Exists(SettingsFilePath))
            {
                try
                {
                    string[] lines = File.ReadAllLines(SettingsFilePath);
                    foreach (var line in lines)
                    {
                        var parts = line.Split('='); // Разделяем строку на ключ и значение
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
                                    textBoxCommandLineOptions.Text = value;
                                    break;
                                case "HighPriority":
                                    checkBoxHighPriority.Checked = bool.Parse(value);
                                    break;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading settings: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
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
            $"CommandLineOptions={textBoxCommandLineOptions.Text}",
            $"HighPriority={checkBoxHighPriority.Checked}"
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

        // Аналогичные изменения для загрузки и сохранения аудиофайлов
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
                            var item = new ListViewItem(Path.GetFileName(parts[0]));
                            item.Tag = parts[0]; // Полный путь хранится в Tag
                            item.Checked = bool.Parse(parts[1]);
                            listViewAudioFiles.Items.Add(item);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading audio files: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
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
                bool hasExeFiles = files.Any(file => Path.GetExtension(file).Equals(".exe", StringComparison.OrdinalIgnoreCase));
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
                if (Path.GetExtension(file).Equals(".exe", StringComparison.OrdinalIgnoreCase))
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
        // Обработчик DragEnter для ListViewAudioFiles
        private void ListViewAudioFiles_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                bool hasAudioFiles = files.Any(file =>
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
                if (Path.GetExtension(file).Equals(".wav", StringComparison.OrdinalIgnoreCase) ||
                    Path.GetExtension(file).Equals(".flac", StringComparison.OrdinalIgnoreCase))
                {
                    var item = new ListViewItem(Path.GetFileName(file))
                    {
                        Tag = file,
                        Checked = true // Устанавливаем выделение по умолчанию
                    };
                    listViewAudioFiles.Items.Add(item);
                }
            }
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
        private void listViewAudioFiles_SelectedIndexChanged(object sender, EventArgs e)
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
                        var item = new ListViewItem(Path.GetFileName(file))
                        {
                            Tag = file,
                            Checked = true // Устанавливаем выделение
                        };
                        listViewAudioFiles.Items.Add(item);
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
        private void labelCPUinfo_Click(object sender, EventArgs e)
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
            textBoxCommandLineOptions.Clear(); // Очищаем textCommandLineOptions
        }
        private void buttonepr8_Click(object sender, EventArgs e)
        {
            // Проверяем, содержится ли -epr8 в textBoxAdditionalArguments
            if (!textBoxCommandLineOptions.Text.Contains("-epr8"))
            {
                // Если нет, добавляем его
                textBoxCommandLineOptions.AppendText(" -epr8"); // Добавляем с пробелом перед текстом
            }
        }
        private void buttonAsubdividetukey5flattop_Click(object sender, EventArgs e)
        {
            // Проверяем, содержится ли -A "subdivide_tukey(5);flattop" в textBoxAdditionalArguments
            if (!textBoxCommandLineOptions.Text.Contains("-A \"subdivide_tukey(5);flattop\""))
            {
                // Если нет, добавляем его
                textBoxCommandLineOptions.AppendText(" -A \"subdivide_tukey(5);flattop\""); // Добавляем с пробелом перед текстом
            }
        }
        private void buttonNoPadding_Click(object sender, EventArgs e)
        {
            // Проверяем, содержится ли --no-padding в textBoxAdditionalArguments
            if (!textBoxCommandLineOptions.Text.Contains("--no-padding"))
            {
                // Если нет, добавляем его
                textBoxCommandLineOptions.AppendText(" --no-padding"); // Добавляем с пробелом перед текстом
            }
        }
        private void buttonNoSeektable_Click(object sender, EventArgs e)
        {
            // Проверяем, содержится ли --no-seektable в textBoxAdditionalArguments
            if (!textBoxCommandLineOptions.Text.Contains("--no-seektable"))
            {
                // Если нет, добавляем его
                textBoxCommandLineOptions.AppendText(" --no-seektable"); // Добавляем с пробелом перед текстом
            }
        }
        private async void buttonStartEncode_Click(object sender, EventArgs e)
        {
            // Создаём временную директорию для выходного файла
            string tempFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp");
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
                    // Получаем значения из текстовых полей
                    string compressionLevel = textBoxCompressionLevel.Text;
                    string threads = textBoxThreads.Text;
                    string commandLine = textBoxCommandLineOptions.Text;

                    // Формируем аргументы для запуска
                    string outputFileName = "temp_encoded.flac"; // Имя выходного файла
                    string outputFilePath = Path.Combine(tempFolderPath, outputFileName);
                    string arguments = $"\"{audioFile}\" -{compressionLevel} {commandLine} -j{threads} -f -o \"{outputFilePath}\"";

                    // Запускаем процесс и дожидаемся завершения
                    try
                    {
                        await Task.Run(() =>
                        {
                            using (var process = new Process())
                            {
                                process.StartInfo = new ProcessStartInfo
                                {
                                    FileName = executable,
                                    Arguments = arguments,
                                    UseShellExecute = false,
                                    CreateNoWindow = true,
                                };

                                // Запускаем отсчет времени
                                stopwatch.Reset();  // обнулить предыдущие результаты
                                stopwatch.Start(); // Запускаем отсчет времени
                                process.Start();

                                // Устанавливаем приоритет процесса на высокий, если чекбокс включен
                                if (checkBoxHighPriority.Checked)
                                {
                                    process.PriorityClass = ProcessPriorityClass.High;
                                }
                                else
                                {
                                    process.PriorityClass = ProcessPriorityClass.Normal; // Устанавливаем нормальный приоритет
                                }

                                process.WaitForExit(); // Дождаться завершения процесса
                                stopwatch.Stop(); // Останавливаем отсчет времени
                            }
                        });

                        // После завершения процесса проверяем размер выходного файла
                        FileInfo outputFile = new FileInfo(outputFilePath);

                        if (outputFile.Exists)
                        {
                            long fileSize = outputFile.Length; // Размер файла в байтах
                            string fileSizeFormatted = $"{fileSize} bytes"; // Форматирование в КБ
                            TimeSpan timeTaken = stopwatch.Elapsed;

                            // Получаем только имя файла для логирования
                            string audioFileName = Path.GetFileName(audioFile);

                            // Записываем информацию в textBoxLog
                            textBoxLog.AppendText($"{audioFileName}\t{fileSizeFormatted}\t{timeTaken.TotalMilliseconds:F3} ms\t{Path.GetFileName(executable)}" + Environment.NewLine);

                            // Также записываем в log.txt
                            File.AppendAllText("log.txt", $"{audioFileName}\tencoded with {Path.GetFileName(executable)}\tResulting FLAC size: {fileSizeFormatted}\tTotal encoding time: {timeTaken.TotalMilliseconds:F3} ms\n");
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

            // MessageBox.Show("Encoding completed!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void buttonStartDecode_Click(object sender, EventArgs e)
        {
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
            textBoxLog.Clear(); // Очищаем textBoxLog
        }

        private void buttonStop_Click(object sender, EventArgs e)
        {

        }
    }
}