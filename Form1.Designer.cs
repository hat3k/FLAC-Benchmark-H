namespace FLAC_Benchmark_H
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            groupBoxEncoderSettings = new GroupBox();
            labelSetThreads = new Label();
            labelSetCores = new Label();
            comboBoxFlacExecutables = new ComboBox();
            labelSetCompression = new Label();
            buttonSetHalfThreads = new Button();
            buttonSetMaxThreads = new Button();
            buttonHalfCores = new Button();
            buttonSetMaxCores = new Button();
            buttonMaxCompressionLevel = new Button();
            button5CompressionLevel = new Button();
            labelCPUinfo = new Label();
            buttonOpenLogtxt = new Button();
            labelFlacUsedVersion = new Label();
            labelFlacFileProperties = new Label();
            labelWavFileProperties = new Label();
            labelThreads = new Label();
            buttonClearLog = new Button();
            textBoxCompressionLevel = new TextBox();
            buttonClear = new Button();
            labelCompressionLevel = new Label();
            textBoxThreads = new TextBox();
            buttonNoSeektable = new Button();
            buttonNoPadding = new Button();
            radioReEncode = new RadioButton();
            radioEncode = new RadioButton();
            buttonAsubdividetukey5flattop = new Button();
            buttonepr8 = new Button();
            progressBar = new ProgressBar();
            buttonStart = new Button();
            labelAdditionalArguments = new Label();
            textBoxAdditionalArguments = new TextBox();
            textBoxFlacExecutables = new TextBox();
            listBoxFlacExecutables = new ListBox();
            groupBoxEncoders = new GroupBox();
            buttonReloadFlacExetutablesAndAudioFies = new Button();
            groupBoxEncoderSettings.SuspendLayout();
            groupBoxEncoders.SuspendLayout();
            SuspendLayout();
            // 
            // groupBoxEncoderSettings
            // 
            groupBoxEncoderSettings.Controls.Add(labelSetThreads);
            groupBoxEncoderSettings.Controls.Add(labelSetCores);
            groupBoxEncoderSettings.Controls.Add(comboBoxFlacExecutables);
            groupBoxEncoderSettings.Controls.Add(labelSetCompression);
            groupBoxEncoderSettings.Controls.Add(buttonSetHalfThreads);
            groupBoxEncoderSettings.Controls.Add(buttonSetMaxThreads);
            groupBoxEncoderSettings.Controls.Add(buttonHalfCores);
            groupBoxEncoderSettings.Controls.Add(buttonSetMaxCores);
            groupBoxEncoderSettings.Controls.Add(buttonMaxCompressionLevel);
            groupBoxEncoderSettings.Controls.Add(button5CompressionLevel);
            groupBoxEncoderSettings.Controls.Add(labelCPUinfo);
            groupBoxEncoderSettings.Controls.Add(buttonOpenLogtxt);
            groupBoxEncoderSettings.Controls.Add(labelFlacUsedVersion);
            groupBoxEncoderSettings.Controls.Add(labelFlacFileProperties);
            groupBoxEncoderSettings.Controls.Add(labelWavFileProperties);
            groupBoxEncoderSettings.Controls.Add(labelThreads);
            groupBoxEncoderSettings.Controls.Add(buttonClearLog);
            groupBoxEncoderSettings.Controls.Add(textBoxCompressionLevel);
            groupBoxEncoderSettings.Controls.Add(buttonClear);
            groupBoxEncoderSettings.Controls.Add(labelCompressionLevel);
            groupBoxEncoderSettings.Controls.Add(textBoxThreads);
            groupBoxEncoderSettings.Controls.Add(buttonNoSeektable);
            groupBoxEncoderSettings.Controls.Add(buttonNoPadding);
            groupBoxEncoderSettings.Controls.Add(radioReEncode);
            groupBoxEncoderSettings.Controls.Add(radioEncode);
            groupBoxEncoderSettings.Controls.Add(buttonAsubdividetukey5flattop);
            groupBoxEncoderSettings.Controls.Add(buttonepr8);
            groupBoxEncoderSettings.Controls.Add(progressBar);
            groupBoxEncoderSettings.Controls.Add(buttonStart);
            groupBoxEncoderSettings.Controls.Add(labelAdditionalArguments);
            groupBoxEncoderSettings.Controls.Add(textBoxAdditionalArguments);
            groupBoxEncoderSettings.Location = new Point(313, 12);
            groupBoxEncoderSettings.Name = "groupBoxEncoderSettings";
            groupBoxEncoderSettings.Size = new Size(984, 244);
            groupBoxEncoderSettings.TabIndex = 0;
            groupBoxEncoderSettings.TabStop = false;
            groupBoxEncoderSettings.Text = "Encoder Settings";
            // 
            // labelSetThreads
            // 
            labelSetThreads.AutoSize = true;
            labelSetThreads.Location = new Point(411, 56);
            labelSetThreads.Name = "labelSetThreads";
            labelSetThreads.Size = new Size(70, 15);
            labelSetThreads.TabIndex = 21;
            labelSetThreads.Text = "Set Threads:";
            // 
            // labelSetCores
            // 
            labelSetCores.AutoSize = true;
            labelSetCores.Location = new Point(225, 56);
            labelSetCores.Name = "labelSetCores";
            labelSetCores.Size = new Size(57, 15);
            labelSetCores.TabIndex = 20;
            labelSetCores.Text = "Set cores:";
            // 
            // comboBoxFlacExecutables
            // 
            comboBoxFlacExecutables.Enabled = false;
            comboBoxFlacExecutables.FormattingEnabled = true;
            comboBoxFlacExecutables.Location = new Point(468, 0);
            comboBoxFlacExecutables.Name = "comboBoxFlacExecutables";
            comboBoxFlacExecutables.Size = new Size(516, 23);
            comboBoxFlacExecutables.TabIndex = 2;
            comboBoxFlacExecutables.Visible = false;
            comboBoxFlacExecutables.SelectedIndexChanged += comboBoxFlacExecutables_SelectedIndexChanged;
            // 
            // labelSetCompression
            // 
            labelSetCompression.AutoSize = true;
            labelSetCompression.Location = new Point(183, 26);
            labelSetCompression.Name = "labelSetCompression";
            labelSetCompression.Size = new Size(99, 15);
            labelSetCompression.TabIndex = 19;
            labelSetCompression.Text = "Set Compression:";
            // 
            // buttonSetHalfThreads
            // 
            buttonSetHalfThreads.Location = new Point(487, 52);
            buttonSetHalfThreads.Name = "buttonSetHalfThreads";
            buttonSetHalfThreads.Size = new Size(53, 23);
            buttonSetHalfThreads.TabIndex = 18;
            buttonSetHalfThreads.Text = "50%";
            buttonSetHalfThreads.UseVisualStyleBackColor = true;
            buttonSetHalfThreads.Click += buttonSetHalfThreads_Click;
            // 
            // buttonSetMaxThreads
            // 
            buttonSetMaxThreads.Location = new Point(546, 52);
            buttonSetMaxThreads.Name = "buttonSetMaxThreads";
            buttonSetMaxThreads.Size = new Size(53, 23);
            buttonSetMaxThreads.TabIndex = 18;
            buttonSetMaxThreads.Text = "100%";
            buttonSetMaxThreads.UseVisualStyleBackColor = true;
            buttonSetMaxThreads.Click += buttonSetMaxThreads_Click;
            // 
            // buttonHalfCores
            // 
            buttonHalfCores.Location = new Point(293, 52);
            buttonHalfCores.Name = "buttonHalfCores";
            buttonHalfCores.Size = new Size(53, 23);
            buttonHalfCores.TabIndex = 18;
            buttonHalfCores.Text = "50%";
            buttonHalfCores.UseVisualStyleBackColor = true;
            buttonHalfCores.Click += buttonHalfCores_Click;
            // 
            // buttonSetMaxCores
            // 
            buttonSetMaxCores.Location = new Point(352, 52);
            buttonSetMaxCores.Name = "buttonSetMaxCores";
            buttonSetMaxCores.Size = new Size(53, 23);
            buttonSetMaxCores.TabIndex = 18;
            buttonSetMaxCores.Text = "100%";
            buttonSetMaxCores.UseVisualStyleBackColor = true;
            buttonSetMaxCores.Click += buttonSetMaxCores_Click;
            // 
            // buttonMaxCompressionLevel
            // 
            buttonMaxCompressionLevel.Location = new Point(352, 22);
            buttonMaxCompressionLevel.Name = "buttonMaxCompressionLevel";
            buttonMaxCompressionLevel.Size = new Size(53, 23);
            buttonMaxCompressionLevel.TabIndex = 18;
            buttonMaxCompressionLevel.Text = "MAX";
            buttonMaxCompressionLevel.UseVisualStyleBackColor = true;
            buttonMaxCompressionLevel.Click += buttonMaxCompressionLevel_Click;
            // 
            // button5CompressionLevel
            // 
            button5CompressionLevel.Location = new Point(293, 22);
            button5CompressionLevel.Name = "button5CompressionLevel";
            button5CompressionLevel.Size = new Size(53, 23);
            button5CompressionLevel.TabIndex = 18;
            button5CompressionLevel.Text = "Default";
            button5CompressionLevel.UseVisualStyleBackColor = true;
            button5CompressionLevel.Click += button5CompressionLevel_Click;
            // 
            // labelCPUinfo
            // 
            labelCPUinfo.AutoSize = true;
            labelCPUinfo.Location = new Point(606, 56);
            labelCPUinfo.Name = "labelCPUinfo";
            labelCPUinfo.Size = new Size(298, 15);
            labelCPUinfo.TabIndex = 17;
            labelCPUinfo.Text = "Your system has: Physical cores: XX, Logical threads: XX";
            labelCPUinfo.Click += labelCPUinfo_Click;
            // 
            // buttonOpenLogtxt
            // 
            buttonOpenLogtxt.Location = new Point(812, 211);
            buttonOpenLogtxt.Name = "buttonOpenLogtxt";
            buttonOpenLogtxt.Size = new Size(85, 23);
            buttonOpenLogtxt.TabIndex = 16;
            buttonOpenLogtxt.Text = "Open log.txt";
            buttonOpenLogtxt.UseVisualStyleBackColor = true;
            buttonOpenLogtxt.Click += buttonOpenLogtxt_Click;
            // 
            // labelFlacUsedVersion
            // 
            labelFlacUsedVersion.AutoSize = true;
            labelFlacUsedVersion.Location = new Point(411, 215);
            labelFlacUsedVersion.Name = "labelFlacUsedVersion";
            labelFlacUsedVersion.Size = new Size(81, 15);
            labelFlacUsedVersion.TabIndex = 15;
            labelFlacUsedVersion.Text = "Using version:";
            labelFlacUsedVersion.Click += labelFlacUsedVersion_Click;
            // 
            // labelFlacFileProperties
            // 
            labelFlacFileProperties.AutoSize = true;
            labelFlacFileProperties.Location = new Point(203, 188);
            labelFlacFileProperties.Name = "labelFlacFileProperties";
            labelFlacFileProperties.Size = new Size(112, 15);
            labelFlacFileProperties.TabIndex = 14;
            labelFlacFileProperties.Text = "FLAC File Properties";
            labelFlacFileProperties.TextAlign = ContentAlignment.TopRight;
            // 
            // labelWavFileProperties
            // 
            labelWavFileProperties.AutoSize = true;
            labelWavFileProperties.Location = new Point(206, 163);
            labelWavFileProperties.Name = "labelWavFileProperties";
            labelWavFileProperties.Size = new Size(109, 15);
            labelWavFileProperties.TabIndex = 13;
            labelWavFileProperties.Text = "WAV File Properties";
            labelWavFileProperties.TextAlign = ContentAlignment.TopRight;
            // 
            // labelThreads
            // 
            labelThreads.AutoSize = true;
            labelThreads.Location = new Point(80, 56);
            labelThreads.Name = "labelThreads";
            labelThreads.Size = new Size(51, 15);
            labelThreads.TabIndex = 0;
            labelThreads.Text = "Threads:";
            // 
            // buttonClearLog
            // 
            buttonClearLog.Location = new Point(903, 211);
            buttonClearLog.Name = "buttonClearLog";
            buttonClearLog.Size = new Size(75, 23);
            buttonClearLog.TabIndex = 12;
            buttonClearLog.Text = "Clear Log";
            buttonClearLog.UseVisualStyleBackColor = true;
            buttonClearLog.Click += buttonClearLog_Click;
            // 
            // textBoxCompressionLevel
            // 
            textBoxCompressionLevel.Location = new Point(139, 22);
            textBoxCompressionLevel.Name = "textBoxCompressionLevel";
            textBoxCompressionLevel.Size = new Size(28, 23);
            textBoxCompressionLevel.TabIndex = 2;
            textBoxCompressionLevel.Text = "8";
            textBoxCompressionLevel.TextAlign = HorizontalAlignment.Center;
            // 
            // buttonClear
            // 
            buttonClear.Location = new Point(903, 83);
            buttonClear.Name = "buttonClear";
            buttonClear.Size = new Size(75, 23);
            buttonClear.TabIndex = 11;
            buttonClear.Text = "Clear";
            buttonClear.UseVisualStyleBackColor = true;
            buttonClear.Click += buttonClear_Click;
            // 
            // labelCompressionLevel
            // 
            labelCompressionLevel.AutoSize = true;
            labelCompressionLevel.Location = new Point(21, 25);
            labelCompressionLevel.Name = "labelCompressionLevel";
            labelCompressionLevel.Size = new Size(110, 15);
            labelCompressionLevel.TabIndex = 0;
            labelCompressionLevel.Text = "Compression Level:";
            // 
            // textBoxThreads
            // 
            textBoxThreads.Location = new Point(139, 53);
            textBoxThreads.Name = "textBoxThreads";
            textBoxThreads.Size = new Size(28, 23);
            textBoxThreads.TabIndex = 3;
            textBoxThreads.Text = "1";
            textBoxThreads.TextAlign = HorizontalAlignment.Center;
            // 
            // buttonNoSeektable
            // 
            buttonNoSeektable.Location = new Point(469, 112);
            buttonNoSeektable.Name = "buttonNoSeektable";
            buttonNoSeektable.Size = new Size(89, 23);
            buttonNoSeektable.TabIndex = 10;
            buttonNoSeektable.Text = "No Seektable";
            buttonNoSeektable.UseVisualStyleBackColor = true;
            buttonNoSeektable.Click += buttonNoSeektable_Click;
            // 
            // buttonNoPadding
            // 
            buttonNoPadding.Location = new Point(380, 112);
            buttonNoPadding.Name = "buttonNoPadding";
            buttonNoPadding.Size = new Size(83, 23);
            buttonNoPadding.TabIndex = 9;
            buttonNoPadding.Text = "No Padding";
            buttonNoPadding.UseVisualStyleBackColor = true;
            buttonNoPadding.Click += buttonNoPadding_Click;
            // 
            // radioReEncode
            // 
            radioReEncode.AutoSize = true;
            radioReEncode.Location = new Point(6, 186);
            radioReEncode.Name = "radioReEncode";
            radioReEncode.Size = new Size(193, 19);
            radioReEncode.TabIndex = 8;
            radioReEncode.Text = "Re-encode (needs an input.flac)";
            radioReEncode.UseVisualStyleBackColor = true;
            radioReEncode.CheckedChanged += radioReEncode_CheckedChanged;
            // 
            // radioEncode
            // 
            radioEncode.AutoSize = true;
            radioEncode.Checked = true;
            radioEncode.Location = new Point(6, 161);
            radioEncode.Name = "radioEncode";
            radioEncode.Size = new Size(177, 19);
            radioEncode.TabIndex = 7;
            radioEncode.TabStop = true;
            radioEncode.Text = "Encode (needs an input.wav)";
            radioEncode.UseVisualStyleBackColor = true;
            radioEncode.CheckedChanged += radioEncode_CheckedChanged;
            // 
            // buttonAsubdividetukey5flattop
            // 
            buttonAsubdividetukey5flattop.Location = new Point(192, 112);
            buttonAsubdividetukey5flattop.Name = "buttonAsubdividetukey5flattop";
            buttonAsubdividetukey5flattop.Size = new Size(182, 23);
            buttonAsubdividetukey5flattop.TabIndex = 6;
            buttonAsubdividetukey5flattop.Text = "-A \"subdivide_tukey(5);flattop\"";
            buttonAsubdividetukey5flattop.UseVisualStyleBackColor = true;
            buttonAsubdividetukey5flattop.Click += buttonAsubdividetukey5flattop_Click;
            // 
            // buttonepr8
            // 
            buttonepr8.Location = new Point(138, 112);
            buttonepr8.Name = "buttonepr8";
            buttonepr8.Size = new Size(48, 23);
            buttonepr8.TabIndex = 5;
            buttonepr8.Text = "-epr8";
            buttonepr8.UseVisualStyleBackColor = true;
            buttonepr8.Click += buttonepr8_Click;
            // 
            // progressBar
            // 
            progressBar.Location = new Point(137, 211);
            progressBar.Name = "progressBar";
            progressBar.Size = new Size(669, 23);
            progressBar.TabIndex = 4;
            // 
            // buttonStart
            // 
            buttonStart.Location = new Point(4, 211);
            buttonStart.Name = "buttonStart";
            buttonStart.Size = new Size(127, 23);
            buttonStart.TabIndex = 1;
            buttonStart.Text = "Start";
            buttonStart.UseVisualStyleBackColor = true;
            buttonStart.Click += buttonStart_Click;
            // 
            // labelAdditionalArguments
            // 
            labelAdditionalArguments.AutoSize = true;
            labelAdditionalArguments.Location = new Point(6, 86);
            labelAdditionalArguments.Name = "labelAdditionalArguments";
            labelAdditionalArguments.Size = new Size(125, 15);
            labelAdditionalArguments.TabIndex = 1;
            labelAdditionalArguments.Text = "Additional arguments:";
            // 
            // textBoxAdditionalArguments
            // 
            textBoxAdditionalArguments.Location = new Point(139, 83);
            textBoxAdditionalArguments.Name = "textBoxAdditionalArguments";
            textBoxAdditionalArguments.Size = new Size(758, 23);
            textBoxAdditionalArguments.TabIndex = 4;
            // 
            // textBoxFlacExecutables
            // 
            textBoxFlacExecutables.Location = new Point(313, 262);
            textBoxFlacExecutables.Multiline = true;
            textBoxFlacExecutables.Name = "textBoxFlacExecutables";
            textBoxFlacExecutables.PlaceholderText = "Log (there is also additional log file in the app folder)";
            textBoxFlacExecutables.ReadOnly = true;
            textBoxFlacExecutables.ScrollBars = ScrollBars.Both;
            textBoxFlacExecutables.Size = new Size(984, 446);
            textBoxFlacExecutables.TabIndex = 1;
            textBoxFlacExecutables.WordWrap = false;
            // 
            // listBoxFlacExecutables
            // 
            listBoxFlacExecutables.FormattingEnabled = true;
            listBoxFlacExecutables.Location = new Point(6, 25);
            listBoxFlacExecutables.Name = "listBoxFlacExecutables";
            listBoxFlacExecutables.Size = new Size(283, 634);
            listBoxFlacExecutables.TabIndex = 2;
            listBoxFlacExecutables.SelectedIndexChanged += listBox1_SelectedIndexChanged;
            // 
            // groupBoxEncoders
            // 
            groupBoxEncoders.Controls.Add(buttonReloadFlacExetutablesAndAudioFies);
            groupBoxEncoders.Controls.Add(listBoxFlacExecutables);
            groupBoxEncoders.Location = new Point(12, 12);
            groupBoxEncoders.Name = "groupBoxEncoders";
            groupBoxEncoders.Size = new Size(295, 696);
            groupBoxEncoders.TabIndex = 3;
            groupBoxEncoders.TabStop = false;
            groupBoxEncoders.Text = "Choose encoder binary";
            // 
            // buttonReloadFlacExetutablesAndAudioFies
            // 
            buttonReloadFlacExetutablesAndAudioFies.Location = new Point(6, 667);
            buttonReloadFlacExetutablesAndAudioFies.Name = "buttonReloadFlacExetutablesAndAudioFies";
            buttonReloadFlacExetutablesAndAudioFies.Size = new Size(283, 23);
            buttonReloadFlacExetutablesAndAudioFies.TabIndex = 4;
            buttonReloadFlacExetutablesAndAudioFies.Text = "Reload all";
            buttonReloadFlacExetutablesAndAudioFies.UseVisualStyleBackColor = true;
            buttonReloadFlacExetutablesAndAudioFies.Click += buttonReloadFlacExetutablesAndAudioFies_Click;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1309, 719);
            Controls.Add(groupBoxEncoders);
            Controls.Add(textBoxFlacExecutables);
            Controls.Add(groupBoxEncoderSettings);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "Form1";
            Text = "FLAC Benchmark-H [beta 0.5 build 20241122.0128]";
            groupBoxEncoderSettings.ResumeLayout(false);
            groupBoxEncoderSettings.PerformLayout();
            groupBoxEncoders.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private GroupBox groupBoxEncoderSettings;
        private Label labelCompressionLevel;
        private Label labelThreads;
        private TextBox textBoxThreads;
        private TextBox textBoxCompressionLevel;
        private Label labelAdditionalArguments;
        private TextBox textBoxAdditionalArguments;
        private ProgressBar progressBar;
        private Button buttonStart;
        private TextBox textBoxFlacExecutables;
        private Button buttonepr8;
        private Button buttonAsubdividetukey5flattop;
        private RadioButton radioReEncode;
        private RadioButton radioEncode;
        private Button buttonNoPadding;
        private Button buttonNoSeektable;
        private Button buttonClear;
        private Button buttonClearLog;
        private Label labelFlacFileProperties;
        private Label labelWavFileProperties;
        private Label labelFlacUsedVersion;
        private ComboBox comboBoxFlacExecutables;
        private ListBox listBoxFlacExecutables;
        private GroupBox groupBoxEncoders;
        private Button buttonOpenLogtxt;
        private Button buttonReloadFlacExetutablesAndAudioFies;
        private Label labelCPUinfo;
        private Button buttonSetMaxCores;
        private Button buttonHalfCores;
        private Button buttonSetHalfThreads;
        private Button buttonSetMaxThreads;
        private Button button5CompressionLevel;
        private Label labelSetCompression;
        private Button buttonMaxCompressionLevel;
        private Label labelSetCores;
        private Label labelSetThreads;
    }
}
