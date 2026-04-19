namespace FLAC_Benchmark_H
{
    public partial class DataGridViewLogSettingsForm : Form
    {
        private readonly DataGridView _dataGridView;
        private readonly Dictionary<string, (CheckBox checkBox, TextBox textBox)> _controls;

        public DataGridViewLogSettingsForm(DataGridView dataGridView)
        {
            InitializeComponent();
            _dataGridView = dataGridView ?? throw new ArgumentNullException(nameof(dataGridView));

            // Link textbox with column names
            _controls = new Dictionary<string, (CheckBox, TextBox)>
            {
                { "Name", (checkBoxLogSettingsName, textBoxLogSettingsName) },
                { "Channels", (checkBoxLogSettingsChannels, textBoxLogSettingsChannels) },
                { "BitDepth", (checkBoxLogSettingsBitDepth, textBoxLogSettingsBitDepth) },
                { "SamplingRate", (checkBoxLogSettingsSamplingRate, textBoxLogSettingsSamplingRate) },
                { "InputFileSize", (checkBoxLogSettingsInputSize, textBoxLogSettingsInputSize) },
                { "OutputFileSize", (checkBoxLogSettingsOutputSize, textBoxLogSettingsOutputSize) },
                { "Compression", (checkBoxLogSettingsCompression, textBoxLogSettingsCompression) },
                { "InputBitRateAudio", (checkBoxLogSettingsInputBitRateAudio, textBoxLogSettingsInputBitRateAudio) },
                { "OutputBitRateAudio", (checkBoxLogSettingsOutputBitRateAudio, textBoxLogSettingsOutputBitRateAudio) },
                { "InputCompressionAudio", (checkBoxLogSettingsInputCompressionAudio, textBoxLogSettingsInputCompressionAudio) },
                { "OutputCompressionAudio", (checkBoxLogSettingsOutputCompressionAudio, textBoxLogSettingsOutputCompressionAudio) },
                { "Time", (checkBoxLogSettingsTime, textBoxLogSettingsTime) },
                { "Speed", (checkBoxLogSettingsSpeed, textBoxLogSettingsSpeed) },
                { "SpeedMin", (checkBoxLogSettingsSpeedMinimum, textBoxLogSettingsSpeedMinimum) },
                { "SpeedMax", (checkBoxLogSettingsSpeedMaximum, textBoxLogSettingsSpeedMaximum) },
                { "SpeedRange", (checkBoxLogSettingsSpeedRange, textBoxLogSettingsSpeedRange) },
                { "SpeedConsistency", (checkBoxLogSettingsSpeedConsistency, textBoxLogSettingsSpeedConsistency) },
                { "CPULoadEncoder", (checkBoxLogSettingsCPULoad, textBoxLogSettingsCPULoad) },
                { "CPUClock", (checkBoxLogSettingsCPUClock, textBoxLogSettingsCPUClock) },
                { "Passes", (checkBoxLogSettingsPasses, textBoxLogSettingsPasses) },
                { "Parameters", (checkBoxLogSettingsParameters, textBoxLogSettingsParameters) },
                { "Encoder", (checkBoxLogSettingsEncoder, textBoxLogSettingsEncoder) },
                { "Version", (checkBoxLogSettingsVersion, textBoxLogSettingsVersion) },
                { "EncoderDirectory", (checkBoxLogSettingsEncoderDirectory, textBoxLogSettingsEncoderDirectory) },
                { "FastestEncoder", (checkBoxLogSettingsFastestEncoder, textBoxLogSettingsFastestEncoder) },
                { "BestSize", (checkBoxLogSettingsBestSize, textBoxLogSettingsBestSize) },
                { "SameSize", (checkBoxLogSettingsSameSize, textBoxLogSettingsSameSize) },
                { "AudioFileDirectory", (checkBoxLogSettingsAudioFileDirectory, textBoxLogSettingsAudioFileDirectory) },
                { "MD5", (checkBoxLogSettingsMD5, textBoxLogSettingsMD5) },
                { "Errors", (checkBoxLogSettingsErrors, textBoxLogSettingsErrors) }
            };

            LoadCurrentSettings();
            AttachEventHandlers();
        }

        private void LoadCurrentSettings()
        {
            foreach (KeyValuePair<string, (CheckBox checkBox, TextBox textBox)> kvp in _controls)
            {
                string columnName = kvp.Key;
                (CheckBox? checkBox, TextBox? textBox) = kvp.Value;

                if (_dataGridView.Columns[columnName] is DataGridViewColumn col)
                {
                    checkBox.Checked = col.Visible;
                    textBox.Text = col.HeaderText;
                }
            }
        }

        private void AttachEventHandlers()
        {
            foreach ((string _, (CheckBox? checkBox, TextBox? textBox)) in _controls)
            {
                checkBox.CheckedChanged += OnVisibilityChanged;
                textBox.TextChanged += OnHeaderTextChanged;
            }

            buttonOkLogSettings.Click += (s, e) => Close();
            buttonResetLogSettingsToDefault.Click += OnResetToDefault;
        }

        private void OnVisibilityChanged(object? sender, EventArgs e)
        {
            if (sender is CheckBox cb)
            {
                // Find column name by checkbox
                foreach (KeyValuePair<string, (CheckBox checkBox, TextBox textBox)> kvp in _controls)
                {
                    if (kvp.Value.checkBox == cb)
                    {
                        if (_dataGridView.Columns[kvp.Key] is DataGridViewColumn col)
                        {
                            col.Visible = cb.Checked;
                        }
                        break;
                    }
                }
            }
        }

        private void OnHeaderTextChanged(object? sender, EventArgs e)
        {
            if (sender is TextBox tb)
            {
                foreach (KeyValuePair<string, (CheckBox checkBox, TextBox textBox)> kvp in _controls)
                {
                    if (kvp.Value.textBox == tb)
                    {
                        if (_dataGridView.Columns[kvp.Key] is DataGridViewColumn col)
                        {
                            col.HeaderText = tb.Text;
                        }
                        break;
                    }
                }
            }
        }

        private void OnResetToDefault(object? sender, EventArgs e)
        {
            // Reset to defaults (as in InitializeDataGridViewLog)
            Dictionary<string, bool> defaultVisibility = new()
            {
                { "Name", true },
                { "Channels", false },
                { "BitDepth", false },
                { "SamplingRate", false },
                { "InputFileSize", true },
                { "OutputFileSize", true },
                { "Compression", true },
                { "InputBitRateAudio", false },
                { "OutputBitRateAudio", false },
                { "InputCompressionAudio", false },
                { "OutputCompressionAudio", false },
                { "Time", false },
                { "Speed", true },
                { "SpeedMin", false },
                { "SpeedMax", false },
                { "SpeedRange", false },
                { "SpeedConsistency", false },
                { "CPULoadEncoder", true },
                { "CPUClock", true },
                { "Passes", true },
                { "Parameters", true },
                { "Encoder", true },
                { "Version", false },
                { "EncoderDirectory", false },
                { "FastestEncoder", true },
                { "BestSize", true },
                { "SameSize", true },
                { "AudioFileDirectory", true },
                { "MD5", false },
                { "Errors", false }
            };

            Dictionary<string, string> defaultHeaders = new()
            {
                { "Name", "Name" },
                { "Channels", "Ch." },
                { "BitDepth", "Bit" },
                { "SamplingRate", "Samp. Rt." },
                { "InputFileSize", "In. Size" },
                { "OutputFileSize", "Out. Size" },
                { "Compression", "Compr." },
                { "InputBitRateAudio", "In. Bit Rt." },
                { "OutputBitRateAudio", "Out. Bit Rt." },
                { "InputCompressionAudio", "In. Compr. (Audio)" },
                { "OutputCompressionAudio", "Out. Compr. (Audio)" },
                { "Time", "Time" },
                { "Speed", "Speed" },
                { "SpeedMin", "Speed Min." },
                { "SpeedMax", "Speed Max." },
                { "SpeedRange", "Range" },
                { "SpeedConsistency", "Speed Consistency" },
                { "CPULoadEncoder", "CPU Load" },
                { "CPUClock", "CPU Clock" },
                { "Passes", "Passes" },
                { "Parameters", "Parameters" },
                { "Encoder", "Encoder" },
                { "Version", "Version" },
                { "EncoderDirectory", "Encoder Directory" },
                { "FastestEncoder", "Fastest Encoder" },
                { "BestSize", "Best Size" },
                { "SameSize", "Same Size" },
                { "AudioFileDirectory", "Audio File Directory" },
                { "MD5", "MD5" },
                { "Errors", "Errors" }
            };

            foreach (KeyValuePair<string, (CheckBox checkBox, TextBox textBox)> kvp in _controls)
            {
                string colName = kvp.Key;
                (CheckBox? cb, TextBox? tb) = kvp.Value;

                bool visible = defaultVisibility.GetValueOrDefault(colName, true);
                string header = defaultHeaders.GetValueOrDefault(colName, colName);

                if (_dataGridView.Columns[colName] is DataGridViewColumn col)
                {
                    col.Visible = visible;
                    col.HeaderText = header;
                }

                cb.Checked = visible;
                tb.Text = header;
            }
        }
    }
}