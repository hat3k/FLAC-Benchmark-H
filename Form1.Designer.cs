namespace FLAC_Benchmark_H
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;

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
            DataGridViewCellStyle dataGridViewCellStyle1 = new DataGridViewCellStyle();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            groupBoxEncoderSettings = new GroupBox();
            buttonStop = new Button();
            labelCommandLine = new Label();
            labelFlacUsedVersion = new Label();
            buttonStartEncode = new Button();
            progressBar = new ProgressBar();
            buttonStartDecode = new Button();
            checkBoxHighPriority = new CheckBox();
            labelSetThreads = new Label();
            labelSetCores = new Label();
            textBoxCommandLineOptions = new TextBox();
            labelSetCompression = new Label();
            buttonepr8 = new Button();
            buttonSetHalfThreads = new Button();
            buttonAsubdividetukey5flattop = new Button();
            buttonSetMaxThreads = new Button();
            buttonNoPadding = new Button();
            buttonHalfCores = new Button();
            buttonNoSeektable = new Button();
            buttonSetMaxCores = new Button();
            buttonClearCommandLine = new Button();
            buttonMaxCompressionLevel = new Button();
            button5CompressionLevel = new Button();
            labelCPUinfo = new Label();
            labelThreads = new Label();
            textBoxCompressionLevel = new TextBox();
            labelCompressionLevel = new Label();
            textBoxThreads = new TextBox();
            buttonOpenLogtxt = new Button();
            buttonClearLog = new Button();
            groupBoxEncoders = new GroupBox();
            buttonRemoveEncoder = new Button();
            listViewFlacExecutables = new ListView();
            buttonAddEncoders = new Button();
            buttonClearEncoders = new Button();
            groupBoxAudioFiles = new GroupBox();
            listViewAudioFiles = new ListView();
            buttonRemoveAudiofile = new Button();
            buttonAddAudioFiles = new Button();
            buttonClearAudioFiles = new Button();
            groupBoxJobsList = new GroupBox();
            textBoxJobList = new TextBox();
            buttonStartJobList = new Button();
            buttonExportJobList = new Button();
            buttonImportJobList = new Button();
            buttonClearJobList = new Button();
            groupLog = new GroupBox();
            dataGridViewLog = new DataGridView();
            buttonCopyLog = new Button();
            groupBoxJobSettings = new GroupBox();
            radioButtonDecode = new RadioButton();
            radioButtonEncode = new RadioButton();
            buttonAddJobToJobList = new Button();
            groupBoxEncoderSettings.SuspendLayout();
            groupBoxEncoders.SuspendLayout();
            groupBoxAudioFiles.SuspendLayout();
            groupBoxJobsList.SuspendLayout();
            groupLog.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridViewLog).BeginInit();
            groupBoxJobSettings.SuspendLayout();
            SuspendLayout();
            // 
            // groupBoxEncoderSettings
            // 
            groupBoxEncoderSettings.Controls.Add(buttonStop);
            groupBoxEncoderSettings.Controls.Add(labelCommandLine);
            groupBoxEncoderSettings.Controls.Add(labelFlacUsedVersion);
            groupBoxEncoderSettings.Controls.Add(buttonStartEncode);
            groupBoxEncoderSettings.Controls.Add(progressBar);
            groupBoxEncoderSettings.Controls.Add(buttonStartDecode);
            groupBoxEncoderSettings.Controls.Add(checkBoxHighPriority);
            groupBoxEncoderSettings.Controls.Add(labelSetThreads);
            groupBoxEncoderSettings.Controls.Add(labelSetCores);
            groupBoxEncoderSettings.Controls.Add(textBoxCommandLineOptions);
            groupBoxEncoderSettings.Controls.Add(labelSetCompression);
            groupBoxEncoderSettings.Controls.Add(buttonepr8);
            groupBoxEncoderSettings.Controls.Add(buttonSetHalfThreads);
            groupBoxEncoderSettings.Controls.Add(buttonAsubdividetukey5flattop);
            groupBoxEncoderSettings.Controls.Add(buttonSetMaxThreads);
            groupBoxEncoderSettings.Controls.Add(buttonNoPadding);
            groupBoxEncoderSettings.Controls.Add(buttonHalfCores);
            groupBoxEncoderSettings.Controls.Add(buttonNoSeektable);
            groupBoxEncoderSettings.Controls.Add(buttonSetMaxCores);
            groupBoxEncoderSettings.Controls.Add(buttonClearCommandLine);
            groupBoxEncoderSettings.Controls.Add(buttonMaxCompressionLevel);
            groupBoxEncoderSettings.Controls.Add(button5CompressionLevel);
            groupBoxEncoderSettings.Controls.Add(labelCPUinfo);
            groupBoxEncoderSettings.Controls.Add(labelThreads);
            groupBoxEncoderSettings.Controls.Add(textBoxCompressionLevel);
            groupBoxEncoderSettings.Controls.Add(labelCompressionLevel);
            groupBoxEncoderSettings.Controls.Add(textBoxThreads);
            groupBoxEncoderSettings.Location = new Point(622, 12);
            groupBoxEncoderSettings.Name = "groupBoxEncoderSettings";
            groupBoxEncoderSettings.Size = new Size(710, 260);
            groupBoxEncoderSettings.TabIndex = 0;
            groupBoxEncoderSettings.TabStop = false;
            groupBoxEncoderSettings.Text = "Encoder Settings";
            groupBoxEncoderSettings.Enter += groupBoxEncoderSettings_Enter;
            // 
            // buttonStop
            // 
            buttonStop.Location = new Point(572, 230);
            buttonStop.Name = "buttonStop";
            buttonStop.Size = new Size(75, 23);
            buttonStop.TabIndex = 25;
            buttonStop.Text = "Stop";
            buttonStop.UseVisualStyleBackColor = true;
            buttonStop.Click += buttonStop_Click;
            // 
            // labelCommandLine
            // 
            labelCommandLine.AutoSize = true;
            labelCommandLine.Location = new Point(27, 83);
            labelCommandLine.Name = "labelCommandLine";
            labelCommandLine.Size = new Size(89, 15);
            labelCommandLine.TabIndex = 24;
            labelCommandLine.Text = "Command line:";
            labelCommandLine.Click += labelCommandLine_Click;
            // 
            // labelFlacUsedVersion
            // 
            labelFlacUsedVersion.AutoSize = true;
            labelFlacUsedVersion.Enabled = false;
            labelFlacUsedVersion.Location = new Point(228, 234);
            labelFlacUsedVersion.Name = "labelFlacUsedVersion";
            labelFlacUsedVersion.Size = new Size(81, 15);
            labelFlacUsedVersion.TabIndex = 15;
            labelFlacUsedVersion.Text = "Using version:";
            labelFlacUsedVersion.Visible = false;
            labelFlacUsedVersion.Click += labelFlacUsedVersion_Click;
            // 
            // buttonStartEncode
            // 
            buttonStartEncode.Location = new Point(6, 230);
            buttonStartEncode.Name = "buttonStartEncode";
            buttonStartEncode.Size = new Size(100, 23);
            buttonStartEncode.TabIndex = 1;
            buttonStartEncode.Text = "Encode";
            buttonStartEncode.UseVisualStyleBackColor = true;
            buttonStartEncode.Click += buttonStartEncode_Click;
            // 
            // progressBar
            // 
            progressBar.Location = new Point(218, 230);
            progressBar.Name = "progressBar";
            progressBar.Size = new Size(348, 23);
            progressBar.TabIndex = 4;
            progressBar.Click += progressBar_Click;
            // 
            // buttonStartDecode
            // 
            buttonStartDecode.Location = new Point(112, 230);
            buttonStartDecode.Name = "buttonStartDecode";
            buttonStartDecode.Size = new Size(100, 23);
            buttonStartDecode.TabIndex = 23;
            buttonStartDecode.Text = "Decode";
            buttonStartDecode.UseVisualStyleBackColor = true;
            buttonStartDecode.Click += buttonStartDecode_Click;
            // 
            // checkBoxHighPriority
            // 
            checkBoxHighPriority.AutoSize = true;
            checkBoxHighPriority.Location = new Point(572, 53);
            checkBoxHighPriority.Name = "checkBoxHighPriority";
            checkBoxHighPriority.Size = new Size(93, 19);
            checkBoxHighPriority.TabIndex = 22;
            checkBoxHighPriority.Text = "High Priority";
            checkBoxHighPriority.UseVisualStyleBackColor = true;
            checkBoxHighPriority.CheckedChanged += checkBoxHighPriority_CheckedChanged;
            // 
            // labelSetThreads
            // 
            labelSetThreads.AutoSize = true;
            labelSetThreads.Location = new Point(378, 54);
            labelSetThreads.Name = "labelSetThreads";
            labelSetThreads.Size = new Size(70, 15);
            labelSetThreads.TabIndex = 21;
            labelSetThreads.Text = "Set Threads:";
            labelSetThreads.Click += labelSetThreads_Click;
            // 
            // labelSetCores
            // 
            labelSetCores.AutoSize = true;
            labelSetCores.Location = new Point(198, 54);
            labelSetCores.Name = "labelSetCores";
            labelSetCores.Size = new Size(59, 15);
            labelSetCores.TabIndex = 20;
            labelSetCores.Text = "Set Cores:";
            labelSetCores.Click += labelSetCores_Click;
            // 
            // textBoxCommandLineOptions
            // 
            textBoxCommandLineOptions.Location = new Point(122, 80);
            textBoxCommandLineOptions.Name = "textBoxCommandLineOptions";
            textBoxCommandLineOptions.Size = new Size(444, 23);
            textBoxCommandLineOptions.TabIndex = 4;
            textBoxCommandLineOptions.TextChanged += textBoxCommandLineOptions_TextChanged;
            // 
            // labelSetCompression
            // 
            labelSetCompression.AutoSize = true;
            labelSetCompression.Location = new Point(156, 24);
            labelSetCompression.Name = "labelSetCompression";
            labelSetCompression.Size = new Size(99, 15);
            labelSetCompression.TabIndex = 19;
            labelSetCompression.Text = "Set Compression:";
            labelSetCompression.Click += labelSetCompression_Click;
            // 
            // buttonepr8
            // 
            buttonepr8.Location = new Point(122, 109);
            buttonepr8.Name = "buttonepr8";
            buttonepr8.Size = new Size(50, 23);
            buttonepr8.TabIndex = 5;
            buttonepr8.Text = "-epr8";
            buttonepr8.UseVisualStyleBackColor = true;
            buttonepr8.Click += buttonepr8_Click;
            // 
            // buttonSetHalfThreads
            // 
            buttonSetHalfThreads.Location = new Point(454, 50);
            buttonSetHalfThreads.Name = "buttonSetHalfThreads";
            buttonSetHalfThreads.Size = new Size(53, 23);
            buttonSetHalfThreads.TabIndex = 18;
            buttonSetHalfThreads.Text = "50%";
            buttonSetHalfThreads.UseVisualStyleBackColor = true;
            buttonSetHalfThreads.Click += buttonSetHalfThreads_Click;
            // 
            // buttonAsubdividetukey5flattop
            // 
            buttonAsubdividetukey5flattop.Location = new Point(178, 109);
            buttonAsubdividetukey5flattop.Name = "buttonAsubdividetukey5flattop";
            buttonAsubdividetukey5flattop.Size = new Size(195, 23);
            buttonAsubdividetukey5flattop.TabIndex = 6;
            buttonAsubdividetukey5flattop.Text = "-A \"subdivide_tukey(5);flattop\"";
            buttonAsubdividetukey5flattop.UseVisualStyleBackColor = true;
            buttonAsubdividetukey5flattop.Click += buttonAsubdividetukey5flattop_Click;
            // 
            // buttonSetMaxThreads
            // 
            buttonSetMaxThreads.Location = new Point(513, 50);
            buttonSetMaxThreads.Name = "buttonSetMaxThreads";
            buttonSetMaxThreads.Size = new Size(53, 23);
            buttonSetMaxThreads.TabIndex = 18;
            buttonSetMaxThreads.Text = "100%";
            buttonSetMaxThreads.UseVisualStyleBackColor = true;
            buttonSetMaxThreads.Click += buttonSetMaxThreads_Click;
            // 
            // buttonNoPadding
            // 
            buttonNoPadding.Location = new Point(380, 109);
            buttonNoPadding.Name = "buttonNoPadding";
            buttonNoPadding.Size = new Size(90, 23);
            buttonNoPadding.TabIndex = 9;
            buttonNoPadding.Text = "No Padding";
            buttonNoPadding.UseVisualStyleBackColor = true;
            buttonNoPadding.Click += buttonNoPadding_Click;
            // 
            // buttonHalfCores
            // 
            buttonHalfCores.Location = new Point(261, 50);
            buttonHalfCores.Name = "buttonHalfCores";
            buttonHalfCores.Size = new Size(53, 23);
            buttonHalfCores.TabIndex = 18;
            buttonHalfCores.Text = "50%";
            buttonHalfCores.UseVisualStyleBackColor = true;
            buttonHalfCores.Click += buttonHalfCores_Click;
            // 
            // buttonNoSeektable
            // 
            buttonNoSeektable.Location = new Point(476, 109);
            buttonNoSeektable.Name = "buttonNoSeektable";
            buttonNoSeektable.Size = new Size(90, 23);
            buttonNoSeektable.TabIndex = 10;
            buttonNoSeektable.Text = "No Seektable";
            buttonNoSeektable.UseVisualStyleBackColor = true;
            buttonNoSeektable.Click += buttonNoSeektable_Click;
            // 
            // buttonSetMaxCores
            // 
            buttonSetMaxCores.Location = new Point(320, 50);
            buttonSetMaxCores.Name = "buttonSetMaxCores";
            buttonSetMaxCores.Size = new Size(53, 23);
            buttonSetMaxCores.TabIndex = 18;
            buttonSetMaxCores.Text = "100%";
            buttonSetMaxCores.UseVisualStyleBackColor = true;
            buttonSetMaxCores.Click += buttonSetMaxCores_Click;
            // 
            // buttonClearCommandLine
            // 
            buttonClearCommandLine.Location = new Point(572, 80);
            buttonClearCommandLine.Name = "buttonClearCommandLine";
            buttonClearCommandLine.Size = new Size(75, 23);
            buttonClearCommandLine.TabIndex = 11;
            buttonClearCommandLine.Text = "Clear";
            buttonClearCommandLine.UseVisualStyleBackColor = true;
            buttonClearCommandLine.Click += buttonClearCommandLine_Click;
            // 
            // buttonMaxCompressionLevel
            // 
            buttonMaxCompressionLevel.Location = new Point(320, 20);
            buttonMaxCompressionLevel.Name = "buttonMaxCompressionLevel";
            buttonMaxCompressionLevel.Size = new Size(53, 23);
            buttonMaxCompressionLevel.TabIndex = 18;
            buttonMaxCompressionLevel.Text = "MAX";
            buttonMaxCompressionLevel.UseVisualStyleBackColor = true;
            buttonMaxCompressionLevel.Click += buttonMaxCompressionLevel_Click;
            // 
            // button5CompressionLevel
            // 
            button5CompressionLevel.Location = new Point(261, 20);
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
            labelCPUinfo.Location = new Point(124, 0);
            labelCPUinfo.Name = "labelCPUinfo";
            labelCPUinfo.Size = new Size(298, 15);
            labelCPUinfo.TabIndex = 17;
            labelCPUinfo.Text = "Your system has: Physical cores: XX, Logical threads: XX";
            labelCPUinfo.Click += labelCPUinfo_Click;
            // 
            // labelThreads
            // 
            labelThreads.AutoSize = true;
            labelThreads.Location = new Point(65, 54);
            labelThreads.Name = "labelThreads";
            labelThreads.Size = new Size(51, 15);
            labelThreads.TabIndex = 0;
            labelThreads.Text = "Threads:";
            labelThreads.Click += labelThreads_Click;
            // 
            // textBoxCompressionLevel
            // 
            textBoxCompressionLevel.Location = new Point(122, 21);
            textBoxCompressionLevel.Name = "textBoxCompressionLevel";
            textBoxCompressionLevel.Size = new Size(28, 23);
            textBoxCompressionLevel.TabIndex = 2;
            textBoxCompressionLevel.Text = "8";
            textBoxCompressionLevel.TextAlign = HorizontalAlignment.Center;
            textBoxCompressionLevel.TextChanged += textBoxCompressionLevel_TextChanged;
            // 
            // labelCompressionLevel
            // 
            labelCompressionLevel.AutoSize = true;
            labelCompressionLevel.Location = new Point(6, 24);
            labelCompressionLevel.Name = "labelCompressionLevel";
            labelCompressionLevel.Size = new Size(110, 15);
            labelCompressionLevel.TabIndex = 0;
            labelCompressionLevel.Text = "Compression Level:";
            labelCompressionLevel.Click += labelCompressionLevel_Click;
            // 
            // textBoxThreads
            // 
            textBoxThreads.Location = new Point(122, 51);
            textBoxThreads.Name = "textBoxThreads";
            textBoxThreads.Size = new Size(28, 23);
            textBoxThreads.TabIndex = 3;
            textBoxThreads.Text = "1";
            textBoxThreads.TextAlign = HorizontalAlignment.Center;
            textBoxThreads.TextChanged += textBoxThreads_TextChanged;
            // 
            // buttonOpenLogtxt
            // 
            buttonOpenLogtxt.Location = new Point(578, 392);
            buttonOpenLogtxt.Name = "buttonOpenLogtxt";
            buttonOpenLogtxt.Size = new Size(85, 23);
            buttonOpenLogtxt.TabIndex = 16;
            buttonOpenLogtxt.Text = "Open log.txt";
            buttonOpenLogtxt.UseVisualStyleBackColor = true;
            buttonOpenLogtxt.Click += buttonOpenLogtxt_Click;
            // 
            // buttonClearLog
            // 
            buttonClearLog.Location = new Point(760, 392);
            buttonClearLog.Name = "buttonClearLog";
            buttonClearLog.Size = new Size(75, 23);
            buttonClearLog.TabIndex = 12;
            buttonClearLog.Text = "Clear Log";
            buttonClearLog.UseVisualStyleBackColor = true;
            buttonClearLog.Click += buttonClearLog_Click;
            // 
            // groupBoxEncoders
            // 
            groupBoxEncoders.Controls.Add(buttonRemoveEncoder);
            groupBoxEncoders.Controls.Add(listViewFlacExecutables);
            groupBoxEncoders.Controls.Add(buttonAddEncoders);
            groupBoxEncoders.Controls.Add(buttonClearEncoders);
            groupBoxEncoders.Location = new Point(12, 12);
            groupBoxEncoders.Name = "groupBoxEncoders";
            groupBoxEncoders.Size = new Size(299, 260);
            groupBoxEncoders.TabIndex = 3;
            groupBoxEncoders.TabStop = false;
            groupBoxEncoders.Text = "Choose Encoder (Drag'n'Drop is available)";
            groupBoxEncoders.Enter += groupBoxEncoders_Enter;
            // 
            // buttonRemoveEncoder
            // 
            buttonRemoveEncoder.Location = new Point(112, 230);
            buttonRemoveEncoder.Name = "buttonRemoveEncoder";
            buttonRemoveEncoder.Size = new Size(100, 23);
            buttonRemoveEncoder.TabIndex = 11;
            buttonRemoveEncoder.Text = "Remove file";
            buttonRemoveEncoder.UseVisualStyleBackColor = true;
            buttonRemoveEncoder.Click += buttonRemoveEncoder_Click;
            // 
            // listViewFlacExecutables
            // 
            listViewFlacExecutables.CheckBoxes = true;
            listViewFlacExecutables.Location = new Point(6, 22);
            listViewFlacExecutables.Name = "listViewFlacExecutables";
            listViewFlacExecutables.Size = new Size(287, 202);
            listViewFlacExecutables.TabIndex = 25;
            listViewFlacExecutables.UseCompatibleStateImageBehavior = false;
            listViewFlacExecutables.View = View.List;
            listViewFlacExecutables.SelectedIndexChanged += listViewFlacExecutables_SelectedIndexChanged;
            // 
            // buttonAddEncoders
            // 
            buttonAddEncoders.Location = new Point(6, 230);
            buttonAddEncoders.Name = "buttonAddEncoders";
            buttonAddEncoders.Size = new Size(100, 23);
            buttonAddEncoders.TabIndex = 11;
            buttonAddEncoders.Text = "Add encoders";
            buttonAddEncoders.UseVisualStyleBackColor = true;
            buttonAddEncoders.Click += buttonAddEncoders_Click;
            // 
            // buttonClearEncoders
            // 
            buttonClearEncoders.Location = new Point(218, 230);
            buttonClearEncoders.Name = "buttonClearEncoders";
            buttonClearEncoders.Size = new Size(75, 23);
            buttonClearEncoders.TabIndex = 11;
            buttonClearEncoders.Text = "Clear";
            buttonClearEncoders.UseVisualStyleBackColor = true;
            buttonClearEncoders.Click += buttonClearEncoders_Click;
            // 
            // groupBoxAudioFiles
            // 
            groupBoxAudioFiles.Controls.Add(listViewAudioFiles);
            groupBoxAudioFiles.Controls.Add(buttonRemoveAudiofile);
            groupBoxAudioFiles.Controls.Add(buttonAddAudioFiles);
            groupBoxAudioFiles.Controls.Add(buttonClearAudioFiles);
            groupBoxAudioFiles.Location = new Point(317, 12);
            groupBoxAudioFiles.Name = "groupBoxAudioFiles";
            groupBoxAudioFiles.Size = new Size(299, 260);
            groupBoxAudioFiles.TabIndex = 3;
            groupBoxAudioFiles.TabStop = false;
            groupBoxAudioFiles.Text = "Choose Audio Files (Drag'n'Drop is available)";
            groupBoxAudioFiles.Enter += groupBoxAudioFiles_Enter;
            // 
            // listViewAudioFiles
            // 
            listViewAudioFiles.CheckBoxes = true;
            listViewAudioFiles.Location = new Point(6, 22);
            listViewAudioFiles.Name = "listViewAudioFiles";
            listViewAudioFiles.Size = new Size(287, 202);
            listViewAudioFiles.TabIndex = 25;
            listViewAudioFiles.UseCompatibleStateImageBehavior = false;
            listViewAudioFiles.View = View.List;
            listViewAudioFiles.SelectedIndexChanged += listViewAudioFiles_SelectedIndexChanged;
            // 
            // buttonRemoveAudiofile
            // 
            buttonRemoveAudiofile.Location = new Point(112, 230);
            buttonRemoveAudiofile.Name = "buttonRemoveAudiofile";
            buttonRemoveAudiofile.Size = new Size(100, 23);
            buttonRemoveAudiofile.TabIndex = 11;
            buttonRemoveAudiofile.Text = "Remove file";
            buttonRemoveAudiofile.UseVisualStyleBackColor = true;
            buttonRemoveAudiofile.Click += buttonRemoveAudiofile_Click;
            // 
            // buttonAddAudioFiles
            // 
            buttonAddAudioFiles.Location = new Point(6, 230);
            buttonAddAudioFiles.Name = "buttonAddAudioFiles";
            buttonAddAudioFiles.Size = new Size(100, 23);
            buttonAddAudioFiles.TabIndex = 11;
            buttonAddAudioFiles.Text = "Add audio files";
            buttonAddAudioFiles.UseVisualStyleBackColor = true;
            buttonAddAudioFiles.Click += buttonAddAudioFiles_Click;
            // 
            // buttonClearAudioFiles
            // 
            buttonClearAudioFiles.Location = new Point(218, 230);
            buttonClearAudioFiles.Name = "buttonClearAudioFiles";
            buttonClearAudioFiles.Size = new Size(75, 23);
            buttonClearAudioFiles.TabIndex = 11;
            buttonClearAudioFiles.Text = "Clear";
            buttonClearAudioFiles.UseVisualStyleBackColor = true;
            buttonClearAudioFiles.Click += buttonClearAudioFiles_Click;
            // 
            // groupBoxJobsList
            // 
            groupBoxJobsList.Controls.Add(textBoxJobList);
            groupBoxJobsList.Controls.Add(buttonStartJobList);
            groupBoxJobsList.Controls.Add(buttonExportJobList);
            groupBoxJobsList.Controls.Add(buttonImportJobList);
            groupBoxJobsList.Controls.Add(buttonClearJobList);
            groupBoxJobsList.Enabled = false;
            groupBoxJobsList.Location = new Point(12, 278);
            groupBoxJobsList.Name = "groupBoxJobsList";
            groupBoxJobsList.Size = new Size(604, 422);
            groupBoxJobsList.TabIndex = 5;
            groupBoxJobsList.TabStop = false;
            groupBoxJobsList.Text = "Job List (Drag'n'Drop is available)";
            groupBoxJobsList.Enter += groupBoxJobList_Enter;
            // 
            // textBoxJobList
            // 
            textBoxJobList.Location = new Point(6, 22);
            textBoxJobList.Multiline = true;
            textBoxJobList.Name = "textBoxJobList";
            textBoxJobList.PlaceholderText = "You may edit this text";
            textBoxJobList.ScrollBars = ScrollBars.Both;
            textBoxJobList.Size = new Size(592, 364);
            textBoxJobList.TabIndex = 1;
            textBoxJobList.WordWrap = false;
            textBoxJobList.TextChanged += textBoxJobList_TextChanged;
            // 
            // buttonStartJobList
            // 
            buttonStartJobList.Location = new Point(6, 392);
            buttonStartJobList.Name = "buttonStartJobList";
            buttonStartJobList.Size = new Size(100, 23);
            buttonStartJobList.TabIndex = 24;
            buttonStartJobList.Text = "Start job list";
            buttonStartJobList.UseVisualStyleBackColor = true;
            buttonStartJobList.Click += buttonStartJobList_Click;
            // 
            // buttonExportJobList
            // 
            buttonExportJobList.Location = new Point(218, 392);
            buttonExportJobList.Name = "buttonExportJobList";
            buttonExportJobList.Size = new Size(100, 23);
            buttonExportJobList.TabIndex = 3;
            buttonExportJobList.Text = "Export";
            buttonExportJobList.UseVisualStyleBackColor = true;
            buttonExportJobList.Click += buttonExportJobList_Click;
            // 
            // buttonImportJobList
            // 
            buttonImportJobList.Location = new Point(112, 392);
            buttonImportJobList.Name = "buttonImportJobList";
            buttonImportJobList.Size = new Size(100, 23);
            buttonImportJobList.TabIndex = 3;
            buttonImportJobList.Text = "Import";
            buttonImportJobList.UseVisualStyleBackColor = true;
            buttonImportJobList.Click += buttonImportJobList_Click;
            // 
            // buttonClearJobList
            // 
            buttonClearJobList.Location = new Point(523, 392);
            buttonClearJobList.Name = "buttonClearJobList";
            buttonClearJobList.Size = new Size(75, 23);
            buttonClearJobList.TabIndex = 11;
            buttonClearJobList.Text = "Clear";
            buttonClearJobList.UseVisualStyleBackColor = true;
            buttonClearJobList.Click += buttonClearJobList_Click;
            // 
            // groupLog
            // 
            groupLog.Controls.Add(dataGridViewLog);
            groupLog.Controls.Add(buttonClearLog);
            groupLog.Controls.Add(buttonCopyLog);
            groupLog.Controls.Add(buttonOpenLogtxt);
            groupLog.Location = new Point(622, 278);
            groupLog.Name = "groupLog";
            groupLog.Size = new Size(841, 422);
            groupLog.TabIndex = 6;
            groupLog.TabStop = false;
            groupLog.Text = "Log";
            groupLog.Enter += groupLog_Enter;
            // 
            // dataGridViewLog
            // 
            dataGridViewLog.AllowUserToAddRows = false;
            dataGridViewLog.AllowUserToOrderColumns = true;
            dataGridViewLog.BackgroundColor = SystemColors.Control;
            dataGridViewLog.BorderStyle = BorderStyle.Fixed3D;
            dataGridViewLog.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewCellStyle1.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = SystemColors.Control;
            dataGridViewCellStyle1.Font = new Font("Segoe UI", 9F);
            dataGridViewCellStyle1.ForeColor = SystemColors.ControlText;
            dataGridViewCellStyle1.SelectionBackColor = SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = DataGridViewTriState.False;
            dataGridViewLog.DefaultCellStyle = dataGridViewCellStyle1;
            dataGridViewLog.GridColor = SystemColors.Control;
            dataGridViewLog.Location = new Point(6, 22);
            dataGridViewLog.Name = "dataGridViewLog";
            dataGridViewLog.ReadOnly = true;
            dataGridViewLog.RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.AutoSizeToDisplayedHeaders;
            dataGridViewLog.Size = new Size(829, 364);
            dataGridViewLog.TabIndex = 17;
            // 
            // buttonCopyLog
            // 
            buttonCopyLog.Location = new Point(669, 392);
            buttonCopyLog.Name = "buttonCopyLog";
            buttonCopyLog.Size = new Size(85, 23);
            buttonCopyLog.TabIndex = 16;
            buttonCopyLog.Text = "Copy Log";
            buttonCopyLog.UseVisualStyleBackColor = true;
            buttonCopyLog.Click += buttonCopyLog_Click;
            // 
            // groupBoxJobSettings
            // 
            groupBoxJobSettings.Controls.Add(radioButtonDecode);
            groupBoxJobSettings.Controls.Add(radioButtonEncode);
            groupBoxJobSettings.Controls.Add(buttonAddJobToJobList);
            groupBoxJobSettings.Enabled = false;
            groupBoxJobSettings.Location = new Point(1338, 12);
            groupBoxJobSettings.Name = "groupBoxJobSettings";
            groupBoxJobSettings.Size = new Size(125, 260);
            groupBoxJobSettings.TabIndex = 25;
            groupBoxJobSettings.TabStop = false;
            groupBoxJobSettings.Text = "Job settings";
            groupBoxJobSettings.Enter += groupBoxJobSettings_Enter;
            // 
            // radioButtonDecode
            // 
            radioButtonDecode.AutoSize = true;
            radioButtonDecode.Location = new Point(6, 47);
            radioButtonDecode.Name = "radioButtonDecode";
            radioButtonDecode.Size = new Size(65, 19);
            radioButtonDecode.TabIndex = 25;
            radioButtonDecode.Text = "Decode";
            radioButtonDecode.UseVisualStyleBackColor = true;
            radioButtonDecode.CheckedChanged += radioButtonDecode_CheckedChanged;
            // 
            // radioButtonEncode
            // 
            radioButtonEncode.AutoSize = true;
            radioButtonEncode.Checked = true;
            radioButtonEncode.Location = new Point(6, 22);
            radioButtonEncode.Name = "radioButtonEncode";
            radioButtonEncode.Size = new Size(64, 19);
            radioButtonEncode.TabIndex = 25;
            radioButtonEncode.TabStop = true;
            radioButtonEncode.Text = "Encode";
            radioButtonEncode.UseVisualStyleBackColor = true;
            radioButtonEncode.CheckedChanged += radioButtonEncode_CheckedChanged;
            // 
            // buttonAddJobToJobList
            // 
            buttonAddJobToJobList.Location = new Point(6, 230);
            buttonAddJobToJobList.Name = "buttonAddJobToJobList";
            buttonAddJobToJobList.Size = new Size(113, 23);
            buttonAddJobToJobList.TabIndex = 24;
            buttonAddJobToJobList.Text = "Add to Job List";
            buttonAddJobToJobList.UseVisualStyleBackColor = true;
            buttonAddJobToJobList.Click += buttonAddJobToJobList_Click;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1477, 711);
            Controls.Add(groupLog);
            Controls.Add(groupBoxJobSettings);
            Controls.Add(groupBoxJobsList);
            Controls.Add(groupBoxAudioFiles);
            Controls.Add(groupBoxEncoders);
            Controls.Add(groupBoxEncoderSettings);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "Form1";
            Text = "FLAC Benchmark-H [beta 0.8 build 20241202.1]";
            Load += Form1_Load;
            groupBoxEncoderSettings.ResumeLayout(false);
            groupBoxEncoderSettings.PerformLayout();
            groupBoxEncoders.ResumeLayout(false);
            groupBoxAudioFiles.ResumeLayout(false);
            groupBoxJobsList.ResumeLayout(false);
            groupBoxJobsList.PerformLayout();
            groupLog.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dataGridViewLog).EndInit();
            groupBoxJobSettings.ResumeLayout(false);
            groupBoxJobSettings.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private GroupBox groupBoxEncoderSettings;
        private ListView listViewFlacExecutables;
        private Label labelCompressionLevel;
        private Label labelThreads;
        private TextBox textBoxThreads;
        private TextBox textBoxCompressionLevel;
        private TextBox textBoxCommandLineOptions;
        private ProgressBar progressBar;
        private Button buttonStartEncode;
        private TextBox textBoxLog;
        private Button buttonepr8;
        private Button buttonAsubdividetukey5flattop;
        private Button buttonNoPadding;
        private Button buttonNoSeektable;
        private Button buttonClearCommandLine;
        private Button buttonClearLog;
        private Label labelFlacUsedVersion;
        private GroupBox groupBoxEncoders;
        private Button buttonOpenLogtxt;
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
        private GroupBox groupBoxAudioFiles;
        private ListView listViewAudioFiles;
        private CheckBox checkBoxHighPriority;
        private Button buttonStartDecode;
        private GroupBox groupBoxJobsList;
        private GroupBox groupLog;
        private Button buttonExportJobList;
        private Button buttonImportJobList;
        private Button buttonClearJobList;
        private TextBox textBoxJobList;
        private Button buttonAddEncoders;
        private Button buttonClearEncoders;
        private Button buttonAddAudioFiles;
        private Button buttonClearAudioFiles;
        private Button buttonRemoveEncoder;
        private Button buttonRemoveAudiofile;
        private GroupBox groupBoxJobSettings;
        private Button buttonStartJobList;
        private Button buttonAddJobToJobList;
        private RadioButton radioButtonDecode;
        private RadioButton radioButtonEncode;
        private Button buttonCopyLog;
        private Label labelCommandLine;
        private Button buttonStop;
        private DataGridView dataGridViewLog;
    }
}
