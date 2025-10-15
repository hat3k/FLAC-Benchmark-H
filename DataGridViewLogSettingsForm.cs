using System;
using System.Collections.Generic;
using System.Windows.Forms;

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
                { "BitDepth", (checkBoxLogSettingsBitDepth, textBoxLogSettingsBitDepth) },
                { "SamplingRate", (checkBoxLogSettingsSamplingRate, textBoxLogSettingsSamplingRate) },
                { "InputFileSize", (checkBoxLogSettingsInputSize, textBoxLogSettingsInputSize) },
                { "OutputFileSize", (checkBoxLogSettingsOutputSize, textBoxLogSettingsOutputSize) },
                { "Compression", (checkBoxLogSettingsCompression, textBoxLogSettingsCompression) },
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
            foreach (var kvp in _controls)
            {
                string columnName = kvp.Key;
                var (checkBox, textBox) = kvp.Value;

                if (_dataGridView.Columns[columnName] is DataGridViewColumn col)
                {
                    checkBox.Checked = col.Visible;
                    textBox.Text = col.HeaderText;
                }
            }
        }

        private void AttachEventHandlers()
        {
            foreach (var (_, (checkBox, textBox)) in _controls)
            {
                checkBox.CheckedChanged += OnVisibilityChanged;
                textBox.TextChanged += OnHeaderTextChanged;
            }

            buttonOkLogSettings.Click += (s, e) => Close();
            buttonResetLogSettingsToDefault.Click += OnResetToDefault;
        }

        private void OnVisibilityChanged(object sender, EventArgs e)
        {
            if (sender is CheckBox cb)
            {
                // Find column name by checkbox
                foreach (var kvp in _controls)
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

        private void OnHeaderTextChanged(object sender, EventArgs e)
        {
            if (sender is TextBox tb)
            {
                foreach (var kvp in _controls)
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

        private void OnResetToDefault(object sender, EventArgs e)
        {
            // Reset to defaults (as in InitializedataGridViewLog)
            var defaultVisibility = new Dictionary<string, bool>
            {
                { "Name", true },
                { "BitDepth", true },
                { "SamplingRate", true },
                { "InputFileSize", true },
                { "OutputFileSize", true },
                { "Compression", true },
                { "Time", true },
                { "Speed", true },
                { "SpeedMin", true },
                { "SpeedMax", true },
                { "SpeedRange", true },
                { "SpeedConsistency", true },
                { "CPULoadEncoder", true },
                { "CPUClock", true },
                { "Passes", true },
                { "Parameters", true },
                { "Encoder", true },
                { "Version", true },
                { "EncoderDirectory", true },
                { "FastestEncoder", true },
                { "BestSize", true },
                { "SameSize", true },
                { "AudioFileDirectory", true },
                { "MD5", true },
                { "Errors", false }
            };

            var defaultHeaders = new Dictionary<string, string>
            {
                { "Name", "Name" },
                { "BitDepth", "Bit Depth" },
                { "SamplingRate", "Samp. Rate" },
                { "InputFileSize", "In. Size" },
                { "OutputFileSize", "Out. Size" },
                { "Compression", "Compr." },
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

            foreach (var kvp in _controls)
            {
                string colName = kvp.Key;
                var (cb, tb) = kvp.Value;

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