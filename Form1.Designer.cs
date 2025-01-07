﻿namespace FLAC_Benchmark_H
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
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            DataGridViewCellStyle dataGridViewCellStyle3 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle4 = new DataGridViewCellStyle();
            groupBoxEncoderSettings = new GroupBox();
            labelEncoderProgress = new Label();
            labelCommandLineEncoder = new Label();
            buttonAddJobToJobListEncoder = new Button();
            buttonStartEncode = new Button();
            progressBarEncoder = new ProgressBar();
            labelSetThreads = new Label();
            labelSetCores = new Label();
            textBoxCommandLineOptionsEncoder = new TextBox();
            labelSetCompression = new Label();
            buttonepr8 = new Button();
            buttonSetHalfThreads = new Button();
            buttonAsubdividetukey5flattop = new Button();
            buttonSetMaxThreads = new Button();
            buttonNoPadding = new Button();
            buttonHalfCores = new Button();
            buttonNoSeektable = new Button();
            buttonSetMaxCores = new Button();
            buttonClearCommandLineEncoder = new Button();
            buttonMaxCompressionLevel = new Button();
            button5CompressionLevel = new Button();
            labelThreads = new Label();
            textBoxCompressionLevel = new TextBox();
            labelCompressionLevel = new Label();
            textBoxThreads = new TextBox();
            buttonStop = new Button();
            buttonStartDecode = new Button();
            labelCPUinfo = new Label();
            buttonOpenLogtxt = new Button();
            buttonClearLog = new Button();
            groupBoxEncoders = new GroupBox();
            buttonDownEncoder = new Button();
            buttonUpEncoder = new Button();
            buttonRemoveEncoder = new Button();
            listViewEncoders = new ListView();
            FileNameExe = new ColumnHeader();
            VersionExe = new ColumnHeader();
            SizeEexe = new ColumnHeader();
            DateExe = new ColumnHeader();
            buttonAddEncoders = new Button();
            buttonClearEncoders = new Button();
            groupBoxAudioFiles = new GroupBox();
            buttonClearUnchecked = new Button();
            buttonDownAudioFile = new Button();
            buttonTestForErrors = new Button();
            buttonDetectDupesAudioFiles = new Button();
            buttonUpAudioFile = new Button();
            listViewAudioFiles = new ListView();
            FileName = new ColumnHeader();
            Duration = new ColumnHeader();
            BitDepth = new ColumnHeader();
            SamplingRate = new ColumnHeader();
            Size = new ColumnHeader();
            MD5Hash = new ColumnHeader();
            FilePath = new ColumnHeader();
            buttonRemoveAudiofile = new Button();
            buttonAddAudioFiles = new Button();
            buttonClearAudioFiles = new Button();
            groupBoxJobsList = new GroupBox();
            labelPasses = new Label();
            buttonMinusPass = new Button();
            buttonPlusPass = new Button();
            buttonDownJob = new Button();
            buttonUpJob = new Button();
            buttonRemoveJob = new Button();
            buttonCopyJobs = new Button();
            buttonPasteJobs = new Button();
            listViewJobs = new ListView();
            JobType = new ColumnHeader();
            Passes = new ColumnHeader();
            Parameters = new ColumnHeader();
            buttonStartJobList = new Button();
            buttonExportJobList = new Button();
            buttonImportJobList = new Button();
            buttonClearJobList = new Button();
            groupBoxLog = new GroupBox();
            buttonLogToExcel = new Button();
            buttonAnalyzeLog = new Button();
            labelStopped = new Label();
            dataGridViewLog = new DataGridView();
            buttonCopyLog = new Button();
            groupBoxSystemandSettings = new GroupBox();
            labelCPUPriority = new Label();
            comboBoxCPUPriority = new ComboBox();
            buttonSelectTempFolder = new Button();
            checkBoxClearTempFolder = new CheckBox();
            groupBoxDecoderSettings = new GroupBox();
            labelDecoderProgress = new Label();
            labelCommandLineDecoder = new Label();
            buttonAddJobToJobListDecoder = new Button();
            textBoxCommandLineOptionsDecoder = new TextBox();
            progressBarDecoder = new ProgressBar();
            buttonClearCommandLineDecoder = new Button();
            toolTip1 = new ToolTip(components);
            groupBoxEncoderSettings.SuspendLayout();
            groupBoxEncoders.SuspendLayout();
            groupBoxAudioFiles.SuspendLayout();
            groupBoxJobsList.SuspendLayout();
            groupBoxLog.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridViewLog).BeginInit();
            groupBoxSystemandSettings.SuspendLayout();
            groupBoxDecoderSettings.SuspendLayout();
            SuspendLayout();
            // 
            // groupBoxEncoderSettings
            // 
            groupBoxEncoderSettings.Controls.Add(labelEncoderProgress);
            groupBoxEncoderSettings.Controls.Add(labelCommandLineEncoder);
            groupBoxEncoderSettings.Controls.Add(buttonAddJobToJobListEncoder);
            groupBoxEncoderSettings.Controls.Add(buttonStartEncode);
            groupBoxEncoderSettings.Controls.Add(progressBarEncoder);
            groupBoxEncoderSettings.Controls.Add(labelSetThreads);
            groupBoxEncoderSettings.Controls.Add(labelSetCores);
            groupBoxEncoderSettings.Controls.Add(textBoxCommandLineOptionsEncoder);
            groupBoxEncoderSettings.Controls.Add(labelSetCompression);
            groupBoxEncoderSettings.Controls.Add(buttonepr8);
            groupBoxEncoderSettings.Controls.Add(buttonSetHalfThreads);
            groupBoxEncoderSettings.Controls.Add(buttonAsubdividetukey5flattop);
            groupBoxEncoderSettings.Controls.Add(buttonSetMaxThreads);
            groupBoxEncoderSettings.Controls.Add(buttonNoPadding);
            groupBoxEncoderSettings.Controls.Add(buttonHalfCores);
            groupBoxEncoderSettings.Controls.Add(buttonNoSeektable);
            groupBoxEncoderSettings.Controls.Add(buttonSetMaxCores);
            groupBoxEncoderSettings.Controls.Add(buttonClearCommandLineEncoder);
            groupBoxEncoderSettings.Controls.Add(buttonMaxCompressionLevel);
            groupBoxEncoderSettings.Controls.Add(button5CompressionLevel);
            groupBoxEncoderSettings.Controls.Add(labelThreads);
            groupBoxEncoderSettings.Controls.Add(textBoxCompressionLevel);
            groupBoxEncoderSettings.Controls.Add(labelCompressionLevel);
            groupBoxEncoderSettings.Controls.Add(textBoxThreads);
            groupBoxEncoderSettings.Location = new Point(12, 278);
            groupBoxEncoderSettings.Name = "groupBoxEncoderSettings";
            groupBoxEncoderSettings.Size = new Size(777, 171);
            groupBoxEncoderSettings.TabIndex = 0;
            groupBoxEncoderSettings.TabStop = false;
            groupBoxEncoderSettings.Text = "Encoder Settings";
            // 
            // labelEncoderProgress
            // 
            labelEncoderProgress.AutoSize = true;
            labelEncoderProgress.Location = new Point(379, 145);
            labelEncoderProgress.Name = "labelEncoderProgress";
            labelEncoderProgress.Size = new Size(0, 15);
            labelEncoderProgress.TabIndex = 25;
            // 
            // labelCommandLineEncoder
            // 
            labelCommandLineEncoder.AutoSize = true;
            labelCommandLineEncoder.Location = new Point(27, 83);
            labelCommandLineEncoder.Name = "labelCommandLineEncoder";
            labelCommandLineEncoder.Size = new Size(89, 15);
            labelCommandLineEncoder.TabIndex = 24;
            labelCommandLineEncoder.Text = "Command line:";
            // 
            // buttonAddJobToJobListEncoder
            // 
            buttonAddJobToJobListEncoder.Location = new Point(122, 141);
            buttonAddJobToJobListEncoder.Name = "buttonAddJobToJobListEncoder";
            buttonAddJobToJobListEncoder.Size = new Size(110, 23);
            buttonAddJobToJobListEncoder.TabIndex = 24;
            buttonAddJobToJobListEncoder.Text = "Add to Job List";
            toolTip1.SetToolTip(buttonAddJobToJobListEncoder, "This will add encoding parameters to a job list.\r\n\r\nLater you may use them to bencmark all checked encoders with all checked audiofiles.");
            buttonAddJobToJobListEncoder.UseVisualStyleBackColor = true;
            buttonAddJobToJobListEncoder.Click += buttonAddJobToJobListEncoder_Click;
            // 
            // buttonStartEncode
            // 
            buttonStartEncode.Location = new Point(6, 141);
            buttonStartEncode.Name = "buttonStartEncode";
            buttonStartEncode.Size = new Size(110, 23);
            buttonStartEncode.TabIndex = 1;
            buttonStartEncode.Text = "Encode";
            toolTip1.SetToolTip(buttonStartEncode, "Encode all checked audio files using all checked encoders with the specified parameters.");
            buttonStartEncode.UseVisualStyleBackColor = true;
            buttonStartEncode.Click += buttonStartEncode_Click;
            // 
            // progressBarEncoder
            // 
            progressBarEncoder.Location = new Point(238, 141);
            progressBarEncoder.Name = "progressBarEncoder";
            progressBarEncoder.Size = new Size(324, 23);
            progressBarEncoder.TabIndex = 4;
            // 
            // labelSetThreads
            // 
            labelSetThreads.AutoSize = true;
            labelSetThreads.Location = new Point(322, 54);
            labelSetThreads.Name = "labelSetThreads";
            labelSetThreads.Size = new Size(51, 15);
            labelSetThreads.TabIndex = 21;
            labelSetThreads.Text = "Threads:";
            // 
            // labelSetCores
            // 
            labelSetCores.AutoSize = true;
            labelSetCores.Location = new Point(156, 54);
            labelSetCores.Name = "labelSetCores";
            labelSetCores.Size = new Size(40, 15);
            labelSetCores.TabIndex = 20;
            labelSetCores.Text = "Cores:";
            // 
            // textBoxCommandLineOptionsEncoder
            // 
            textBoxCommandLineOptionsEncoder.Location = new Point(122, 80);
            textBoxCommandLineOptionsEncoder.Name = "textBoxCommandLineOptionsEncoder";
            textBoxCommandLineOptionsEncoder.Size = new Size(440, 23);
            textBoxCommandLineOptionsEncoder.TabIndex = 4;
            // 
            // labelSetCompression
            // 
            labelSetCompression.AutoSize = true;
            labelSetCompression.Location = new Point(170, 24);
            labelSetCompression.Name = "labelSetCompression";
            labelSetCompression.Size = new Size(26, 15);
            labelSetCompression.TabIndex = 19;
            labelSetCompression.Text = "Set:";
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
            buttonSetHalfThreads.Location = new Point(379, 51);
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
            buttonAsubdividetukey5flattop.Size = new Size(192, 23);
            buttonAsubdividetukey5flattop.TabIndex = 6;
            buttonAsubdividetukey5flattop.Text = "-A \"subdivide_tukey(5);flattop\"";
            buttonAsubdividetukey5flattop.UseVisualStyleBackColor = true;
            buttonAsubdividetukey5flattop.Click += buttonAsubdividetukey5flattop_Click;
            // 
            // buttonSetMaxThreads
            // 
            buttonSetMaxThreads.Location = new Point(438, 51);
            buttonSetMaxThreads.Name = "buttonSetMaxThreads";
            buttonSetMaxThreads.Size = new Size(53, 23);
            buttonSetMaxThreads.TabIndex = 18;
            buttonSetMaxThreads.Text = "100%";
            buttonSetMaxThreads.UseVisualStyleBackColor = true;
            buttonSetMaxThreads.Click += buttonSetMaxThreads_Click;
            // 
            // buttonNoPadding
            // 
            buttonNoPadding.Location = new Point(376, 109);
            buttonNoPadding.Name = "buttonNoPadding";
            buttonNoPadding.Size = new Size(90, 23);
            buttonNoPadding.TabIndex = 9;
            buttonNoPadding.Text = "No Padding";
            buttonNoPadding.UseVisualStyleBackColor = true;
            buttonNoPadding.Click += buttonNoPadding_Click;
            // 
            // buttonHalfCores
            // 
            buttonHalfCores.Location = new Point(202, 51);
            buttonHalfCores.Name = "buttonHalfCores";
            buttonHalfCores.Size = new Size(53, 23);
            buttonHalfCores.TabIndex = 18;
            buttonHalfCores.Text = "50%";
            buttonHalfCores.UseVisualStyleBackColor = true;
            buttonHalfCores.Click += buttonHalfCores_Click;
            // 
            // buttonNoSeektable
            // 
            buttonNoSeektable.Location = new Point(472, 109);
            buttonNoSeektable.Name = "buttonNoSeektable";
            buttonNoSeektable.Size = new Size(90, 23);
            buttonNoSeektable.TabIndex = 10;
            buttonNoSeektable.Text = "No Seektable";
            buttonNoSeektable.UseVisualStyleBackColor = true;
            buttonNoSeektable.Click += buttonNoSeektable_Click;
            // 
            // buttonSetMaxCores
            // 
            buttonSetMaxCores.Location = new Point(261, 51);
            buttonSetMaxCores.Name = "buttonSetMaxCores";
            buttonSetMaxCores.Size = new Size(53, 23);
            buttonSetMaxCores.TabIndex = 18;
            buttonSetMaxCores.Text = "100%";
            buttonSetMaxCores.UseVisualStyleBackColor = true;
            buttonSetMaxCores.Click += buttonSetMaxCores_Click;
            // 
            // buttonClearCommandLineEncoder
            // 
            buttonClearCommandLineEncoder.Location = new Point(568, 80);
            buttonClearCommandLineEncoder.Name = "buttonClearCommandLineEncoder";
            buttonClearCommandLineEncoder.Size = new Size(55, 23);
            buttonClearCommandLineEncoder.TabIndex = 11;
            buttonClearCommandLineEncoder.Text = "Clear";
            buttonClearCommandLineEncoder.UseVisualStyleBackColor = true;
            buttonClearCommandLineEncoder.Click += buttonClearCommandLineEncoder_Click;
            // 
            // buttonMaxCompressionLevel
            // 
            buttonMaxCompressionLevel.Location = new Point(261, 21);
            buttonMaxCompressionLevel.Name = "buttonMaxCompressionLevel";
            buttonMaxCompressionLevel.Size = new Size(53, 23);
            buttonMaxCompressionLevel.TabIndex = 18;
            buttonMaxCompressionLevel.Text = "MAX";
            buttonMaxCompressionLevel.UseVisualStyleBackColor = true;
            buttonMaxCompressionLevel.Click += buttonMaxCompressionLevel_Click;
            // 
            // button5CompressionLevel
            // 
            button5CompressionLevel.Location = new Point(202, 21);
            button5CompressionLevel.Name = "button5CompressionLevel";
            button5CompressionLevel.Size = new Size(53, 23);
            button5CompressionLevel.TabIndex = 18;
            button5CompressionLevel.Text = "Default";
            button5CompressionLevel.UseVisualStyleBackColor = true;
            button5CompressionLevel.Click += button5CompressionLevel_Click;
            // 
            // labelThreads
            // 
            labelThreads.AutoSize = true;
            labelThreads.Location = new Point(65, 54);
            labelThreads.Name = "labelThreads";
            labelThreads.Size = new Size(51, 15);
            labelThreads.TabIndex = 0;
            labelThreads.Text = "Threads:";
            // 
            // textBoxCompressionLevel
            // 
            textBoxCompressionLevel.Location = new Point(122, 21);
            textBoxCompressionLevel.Name = "textBoxCompressionLevel";
            textBoxCompressionLevel.Size = new Size(28, 23);
            textBoxCompressionLevel.TabIndex = 2;
            textBoxCompressionLevel.Text = "8";
            textBoxCompressionLevel.TextAlign = HorizontalAlignment.Center;
            // 
            // labelCompressionLevel
            // 
            labelCompressionLevel.AutoSize = true;
            labelCompressionLevel.Location = new Point(6, 24);
            labelCompressionLevel.Name = "labelCompressionLevel";
            labelCompressionLevel.Size = new Size(110, 15);
            labelCompressionLevel.TabIndex = 0;
            labelCompressionLevel.Text = "Compression Level:";
            // 
            // textBoxThreads
            // 
            textBoxThreads.Location = new Point(122, 51);
            textBoxThreads.Name = "textBoxThreads";
            textBoxThreads.Size = new Size(28, 23);
            textBoxThreads.TabIndex = 3;
            textBoxThreads.Text = "1";
            textBoxThreads.TextAlign = HorizontalAlignment.Center;
            // 
            // buttonStop
            // 
            buttonStop.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            buttonStop.Location = new Point(6, 364);
            buttonStop.Name = "buttonStop";
            buttonStop.Size = new Size(110, 23);
            buttonStop.TabIndex = 25;
            buttonStop.Text = "Stop all (Esc)";
            toolTip1.SetToolTip(buttonStop, "Stop all encoding and decoding jobs.");
            buttonStop.UseVisualStyleBackColor = true;
            buttonStop.Click += buttonStop_Click;
            // 
            // buttonStartDecode
            // 
            buttonStartDecode.Location = new Point(6, 141);
            buttonStartDecode.Name = "buttonStartDecode";
            buttonStartDecode.Size = new Size(110, 23);
            buttonStartDecode.TabIndex = 23;
            buttonStartDecode.Text = "Decode";
            toolTip1.SetToolTip(buttonStartDecode, "Decode all checked audio files using all checked encoders with the specified parameters.");
            buttonStartDecode.UseVisualStyleBackColor = true;
            buttonStartDecode.Click += buttonStartDecode_Click;
            // 
            // labelCPUinfo
            // 
            labelCPUinfo.Location = new Point(6, 24);
            labelCPUinfo.Name = "labelCPUinfo";
            labelCPUinfo.Size = new Size(161, 64);
            labelCPUinfo.TabIndex = 17;
            labelCPUinfo.Text = "CPU Info";
            // 
            // buttonOpenLogtxt
            // 
            buttonOpenLogtxt.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonOpenLogtxt.Location = new Point(564, 364);
            buttonOpenLogtxt.Name = "buttonOpenLogtxt";
            buttonOpenLogtxt.Size = new Size(85, 23);
            buttonOpenLogtxt.TabIndex = 16;
            buttonOpenLogtxt.Text = "Open log.txt";
            buttonOpenLogtxt.UseVisualStyleBackColor = true;
            buttonOpenLogtxt.Click += buttonOpenLogtxt_Click;
            // 
            // buttonClearLog
            // 
            buttonClearLog.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonClearLog.Location = new Point(716, 364);
            buttonClearLog.Name = "buttonClearLog";
            buttonClearLog.Size = new Size(55, 23);
            buttonClearLog.TabIndex = 12;
            buttonClearLog.Text = "Clear";
            buttonClearLog.UseVisualStyleBackColor = true;
            buttonClearLog.Click += buttonClearLog_Click;
            // 
            // groupBoxEncoders
            // 
            groupBoxEncoders.Controls.Add(buttonDownEncoder);
            groupBoxEncoders.Controls.Add(buttonUpEncoder);
            groupBoxEncoders.Controls.Add(buttonRemoveEncoder);
            groupBoxEncoders.Controls.Add(listViewEncoders);
            groupBoxEncoders.Controls.Add(buttonAddEncoders);
            groupBoxEncoders.Controls.Add(buttonClearEncoders);
            groupBoxEncoders.Location = new Point(12, 12);
            groupBoxEncoders.Name = "groupBoxEncoders";
            groupBoxEncoders.Size = new Size(777, 260);
            groupBoxEncoders.TabIndex = 3;
            groupBoxEncoders.TabStop = false;
            groupBoxEncoders.Text = "Choose Encoder (Drag'n'Drop of files and folders is available)";
            // 
            // buttonDownEncoder
            // 
            buttonDownEncoder.Location = new Point(152, 230);
            buttonDownEncoder.Name = "buttonDownEncoder";
            buttonDownEncoder.Size = new Size(24, 23);
            buttonDownEncoder.TabIndex = 27;
            buttonDownEncoder.Text = "▼";
            buttonDownEncoder.UseVisualStyleBackColor = true;
            buttonDownEncoder.Click += buttonDownEncoder_Click;
            // 
            // buttonUpEncoder
            // 
            buttonUpEncoder.Location = new Point(122, 230);
            buttonUpEncoder.Name = "buttonUpEncoder";
            buttonUpEncoder.Size = new Size(24, 23);
            buttonUpEncoder.TabIndex = 26;
            buttonUpEncoder.Text = "▲";
            buttonUpEncoder.UseVisualStyleBackColor = true;
            buttonUpEncoder.Click += buttonUpEncoder_Click;
            // 
            // buttonRemoveEncoder
            // 
            buttonRemoveEncoder.Font = new Font("Segoe UI", 9F);
            buttonRemoveEncoder.Location = new Point(182, 230);
            buttonRemoveEncoder.Name = "buttonRemoveEncoder";
            buttonRemoveEncoder.Size = new Size(24, 23);
            buttonRemoveEncoder.TabIndex = 11;
            buttonRemoveEncoder.Text = "❌";
            buttonRemoveEncoder.UseVisualStyleBackColor = true;
            buttonRemoveEncoder.Click += buttonRemoveEncoder_Click;
            // 
            // listViewEncoders
            // 
            listViewEncoders.CheckBoxes = true;
            listViewEncoders.Columns.AddRange(new ColumnHeader[] { FileNameExe, VersionExe, SizeEexe, DateExe });
            listViewEncoders.FullRowSelect = true;
            listViewEncoders.Location = new Point(6, 22);
            listViewEncoders.Name = "listViewEncoders";
            listViewEncoders.Size = new Size(765, 202);
            listViewEncoders.TabIndex = 25;
            listViewEncoders.UseCompatibleStateImageBehavior = false;
            listViewEncoders.View = View.Details;
            // 
            // FileNameExe
            // 
            FileNameExe.Tag = "FileNameExe";
            FileNameExe.Text = "File Name";
            FileNameExe.Width = 381;
            // 
            // VersionExe
            // 
            VersionExe.Tag = "VersionExe";
            VersionExe.Text = "Version";
            VersionExe.Width = 170;
            // 
            // SizeEexe
            // 
            SizeEexe.Tag = "SizeExe";
            SizeEexe.Text = "Size";
            SizeEexe.TextAlign = HorizontalAlignment.Right;
            SizeEexe.Width = 93;
            // 
            // DateExe
            // 
            DateExe.Tag = "DateExe";
            DateExe.Text = "Date";
            DateExe.Width = 100;
            // 
            // buttonAddEncoders
            // 
            buttonAddEncoders.Font = new Font("Segoe UI", 9F);
            buttonAddEncoders.Location = new Point(6, 230);
            buttonAddEncoders.Name = "buttonAddEncoders";
            buttonAddEncoders.Size = new Size(110, 23);
            buttonAddEncoders.TabIndex = 11;
            buttonAddEncoders.Text = "Add encoders";
            buttonAddEncoders.UseVisualStyleBackColor = true;
            buttonAddEncoders.Click += buttonAddEncoders_Click;
            // 
            // buttonClearEncoders
            // 
            buttonClearEncoders.Font = new Font("Segoe UI", 9F);
            buttonClearEncoders.Location = new Point(716, 230);
            buttonClearEncoders.Name = "buttonClearEncoders";
            buttonClearEncoders.Size = new Size(55, 23);
            buttonClearEncoders.TabIndex = 11;
            buttonClearEncoders.Text = "Clear";
            buttonClearEncoders.UseVisualStyleBackColor = true;
            buttonClearEncoders.Click += buttonClearEncoders_Click;
            // 
            // groupBoxAudioFiles
            // 
            groupBoxAudioFiles.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            groupBoxAudioFiles.Controls.Add(buttonClearUnchecked);
            groupBoxAudioFiles.Controls.Add(buttonDownAudioFile);
            groupBoxAudioFiles.Controls.Add(buttonTestForErrors);
            groupBoxAudioFiles.Controls.Add(buttonDetectDupesAudioFiles);
            groupBoxAudioFiles.Controls.Add(buttonUpAudioFile);
            groupBoxAudioFiles.Controls.Add(listViewAudioFiles);
            groupBoxAudioFiles.Controls.Add(buttonRemoveAudiofile);
            groupBoxAudioFiles.Controls.Add(buttonAddAudioFiles);
            groupBoxAudioFiles.Controls.Add(buttonClearAudioFiles);
            groupBoxAudioFiles.Location = new Point(795, 12);
            groupBoxAudioFiles.Name = "groupBoxAudioFiles";
            groupBoxAudioFiles.Size = new Size(777, 260);
            groupBoxAudioFiles.TabIndex = 3;
            groupBoxAudioFiles.TabStop = false;
            groupBoxAudioFiles.Text = "Choose Audio Files (Drag'n'Drop of files and folders is available)";
            // 
            // buttonClearUnchecked
            // 
            buttonClearUnchecked.Anchor = AnchorStyles.Right;
            buttonClearUnchecked.Location = new Point(610, 230);
            buttonClearUnchecked.Name = "buttonClearUnchecked";
            buttonClearUnchecked.Size = new Size(100, 23);
            buttonClearUnchecked.TabIndex = 29;
            buttonClearUnchecked.Text = "Clear uncheked";
            toolTip1.SetToolTip(buttonClearUnchecked, "Clears all unchecked files from the list.\r\n\r\nHold 'Shift' while clicking to move unchecked files to the Recycle Bin.");
            buttonClearUnchecked.UseVisualStyleBackColor = true;
            buttonClearUnchecked.Click += buttonClearUnchecked_Click;
            // 
            // buttonDownAudioFile
            // 
            buttonDownAudioFile.Location = new Point(152, 230);
            buttonDownAudioFile.Name = "buttonDownAudioFile";
            buttonDownAudioFile.Size = new Size(24, 23);
            buttonDownAudioFile.TabIndex = 27;
            buttonDownAudioFile.Text = "▼";
            buttonDownAudioFile.UseVisualStyleBackColor = true;
            buttonDownAudioFile.Click += buttonDownAudioFile_Click;
            // 
            // buttonTestForErrors
            // 
            buttonTestForErrors.Anchor = AnchorStyles.Left;
            buttonTestForErrors.Enabled = false;
            buttonTestForErrors.Location = new Point(354, 230);
            buttonTestForErrors.Name = "buttonTestForErrors";
            buttonTestForErrors.Size = new Size(110, 23);
            buttonTestForErrors.TabIndex = 1;
            buttonTestForErrors.Text = "Test for errors";
            buttonTestForErrors.UseVisualStyleBackColor = true;
            buttonTestForErrors.Visible = false;
            // 
            // buttonDetectDupesAudioFiles
            // 
            buttonDetectDupesAudioFiles.Anchor = AnchorStyles.Left;
            buttonDetectDupesAudioFiles.Location = new Point(238, 230);
            buttonDetectDupesAudioFiles.Name = "buttonDetectDupesAudioFiles";
            buttonDetectDupesAudioFiles.Size = new Size(110, 23);
            buttonDetectDupesAudioFiles.TabIndex = 28;
            buttonDetectDupesAudioFiles.Text = "Detect dupes";
            toolTip1.SetToolTip(buttonDetectDupesAudioFiles, resources.GetString("buttonDetectDupesAudioFiles.ToolTip"));
            buttonDetectDupesAudioFiles.UseVisualStyleBackColor = true;
            buttonDetectDupesAudioFiles.Click += buttonDetectDupesAudioFiles_Click;
            // 
            // buttonUpAudioFile
            // 
            buttonUpAudioFile.Location = new Point(122, 230);
            buttonUpAudioFile.Name = "buttonUpAudioFile";
            buttonUpAudioFile.Size = new Size(24, 23);
            buttonUpAudioFile.TabIndex = 26;
            buttonUpAudioFile.Text = "▲";
            buttonUpAudioFile.UseVisualStyleBackColor = true;
            buttonUpAudioFile.Click += buttonUpAudioFile_Click;
            // 
            // listViewAudioFiles
            // 
            listViewAudioFiles.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            listViewAudioFiles.CheckBoxes = true;
            listViewAudioFiles.Columns.AddRange(new ColumnHeader[] { FileName, Duration, BitDepth, SamplingRate, Size, MD5Hash, FilePath });
            listViewAudioFiles.FullRowSelect = true;
            listViewAudioFiles.Location = new Point(6, 22);
            listViewAudioFiles.Name = "listViewAudioFiles";
            listViewAudioFiles.Size = new Size(765, 202);
            listViewAudioFiles.TabIndex = 25;
            listViewAudioFiles.UseCompatibleStateImageBehavior = false;
            listViewAudioFiles.View = View.Details;
            // 
            // FileName
            // 
            FileName.Tag = "FileName";
            FileName.Text = "File Name";
            FileName.Width = 387;
            // 
            // Duration
            // 
            Duration.Tag = "Duration";
            Duration.Text = "Duration";
            Duration.TextAlign = HorizontalAlignment.Right;
            Duration.Width = 100;
            // 
            // BitDepth
            // 
            BitDepth.Tag = "BitDepth";
            BitDepth.Text = "Bit Depth";
            BitDepth.TextAlign = HorizontalAlignment.Right;
            BitDepth.Width = 62;
            // 
            // SamplingRate
            // 
            SamplingRate.Tag = "SamplingRate";
            SamplingRate.Text = "Samp. Rate";
            SamplingRate.TextAlign = HorizontalAlignment.Right;
            SamplingRate.Width = 71;
            // 
            // Size
            // 
            Size.Tag = "Size";
            Size.Text = "Size";
            Size.TextAlign = HorizontalAlignment.Right;
            Size.Width = 124;
            // 
            // MD5Hash
            // 
            MD5Hash.Tag = "MD5Hash";
            MD5Hash.Text = "MD5 Hash";
            MD5Hash.Width = 230;
            // 
            // FilePath
            // 
            FilePath.Text = "File Path";
            FilePath.Width = 250;
            // 
            // buttonRemoveAudiofile
            // 
            buttonRemoveAudiofile.Location = new Point(182, 230);
            buttonRemoveAudiofile.Name = "buttonRemoveAudiofile";
            buttonRemoveAudiofile.Size = new Size(24, 23);
            buttonRemoveAudiofile.TabIndex = 11;
            buttonRemoveAudiofile.Text = "❌";
            buttonRemoveAudiofile.UseVisualStyleBackColor = true;
            buttonRemoveAudiofile.Click += buttonRemoveAudiofile_Click;
            // 
            // buttonAddAudioFiles
            // 
            buttonAddAudioFiles.Location = new Point(6, 230);
            buttonAddAudioFiles.Name = "buttonAddAudioFiles";
            buttonAddAudioFiles.Size = new Size(110, 23);
            buttonAddAudioFiles.TabIndex = 11;
            buttonAddAudioFiles.Text = "Add audio files";
            buttonAddAudioFiles.UseVisualStyleBackColor = true;
            buttonAddAudioFiles.Click += buttonAddAudioFiles_Click;
            // 
            // buttonClearAudioFiles
            // 
            buttonClearAudioFiles.Anchor = AnchorStyles.Right;
            buttonClearAudioFiles.Location = new Point(716, 230);
            buttonClearAudioFiles.Name = "buttonClearAudioFiles";
            buttonClearAudioFiles.Size = new Size(55, 23);
            buttonClearAudioFiles.TabIndex = 11;
            buttonClearAudioFiles.Text = "Clear";
            buttonClearAudioFiles.UseVisualStyleBackColor = true;
            buttonClearAudioFiles.Click += buttonClearAudioFiles_Click;
            // 
            // groupBoxJobsList
            // 
            groupBoxJobsList.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
            groupBoxJobsList.Controls.Add(labelPasses);
            groupBoxJobsList.Controls.Add(buttonMinusPass);
            groupBoxJobsList.Controls.Add(buttonPlusPass);
            groupBoxJobsList.Controls.Add(buttonDownJob);
            groupBoxJobsList.Controls.Add(buttonUpJob);
            groupBoxJobsList.Controls.Add(buttonRemoveJob);
            groupBoxJobsList.Controls.Add(buttonCopyJobs);
            groupBoxJobsList.Controls.Add(buttonPasteJobs);
            groupBoxJobsList.Controls.Add(listViewJobs);
            groupBoxJobsList.Controls.Add(buttonStartJobList);
            groupBoxJobsList.Controls.Add(buttonExportJobList);
            groupBoxJobsList.Controls.Add(buttonImportJobList);
            groupBoxJobsList.Controls.Add(buttonClearJobList);
            groupBoxJobsList.Location = new Point(12, 455);
            groupBoxJobsList.Name = "groupBoxJobsList";
            groupBoxJobsList.Size = new Size(777, 394);
            groupBoxJobsList.TabIndex = 5;
            groupBoxJobsList.TabStop = false;
            groupBoxJobsList.Text = "Job List (Drag'n'Drop is available)";
            // 
            // labelPasses
            // 
            labelPasses.Anchor = AnchorStyles.Bottom;
            labelPasses.AutoSize = true;
            labelPasses.Location = new Point(270, 368);
            labelPasses.Name = "labelPasses";
            labelPasses.Size = new Size(44, 15);
            labelPasses.TabIndex = 33;
            labelPasses.Text = "Passes:";
            toolTip1.SetToolTip(labelPasses, "You can add several passes of the same job to obtain averaged results.");
            // 
            // buttonMinusPass
            // 
            buttonMinusPass.Anchor = AnchorStyles.Bottom;
            buttonMinusPass.Location = new Point(350, 364);
            buttonMinusPass.Name = "buttonMinusPass";
            buttonMinusPass.Size = new Size(24, 23);
            buttonMinusPass.TabIndex = 32;
            buttonMinusPass.Text = "➖";
            toolTip1.SetToolTip(buttonMinusPass, "You can add several passes of the same job to obtain averaged results.");
            buttonMinusPass.UseVisualStyleBackColor = true;
            buttonMinusPass.Click += buttonMinusPass_Click;
            // 
            // buttonPlusPass
            // 
            buttonPlusPass.Anchor = AnchorStyles.Bottom;
            buttonPlusPass.Location = new Point(320, 364);
            buttonPlusPass.Name = "buttonPlusPass";
            buttonPlusPass.Size = new Size(24, 23);
            buttonPlusPass.TabIndex = 31;
            buttonPlusPass.Text = "➕";
            toolTip1.SetToolTip(buttonPlusPass, "You can add several passes of the same job to obtain averaged results.");
            buttonPlusPass.UseVisualStyleBackColor = true;
            buttonPlusPass.Click += buttonPlusPass_Click;
            // 
            // buttonDownJob
            // 
            buttonDownJob.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            buttonDownJob.Location = new Point(151, 364);
            buttonDownJob.Name = "buttonDownJob";
            buttonDownJob.Size = new Size(24, 23);
            buttonDownJob.TabIndex = 30;
            buttonDownJob.Text = "▼";
            buttonDownJob.UseVisualStyleBackColor = true;
            buttonDownJob.Click += buttonDownJob_Click;
            // 
            // buttonUpJob
            // 
            buttonUpJob.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            buttonUpJob.Location = new Point(122, 364);
            buttonUpJob.Name = "buttonUpJob";
            buttonUpJob.Size = new Size(24, 23);
            buttonUpJob.TabIndex = 29;
            buttonUpJob.Text = "▲";
            buttonUpJob.UseVisualStyleBackColor = true;
            buttonUpJob.Click += buttonUpJob_Click;
            // 
            // buttonRemoveJob
            // 
            buttonRemoveJob.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            buttonRemoveJob.Location = new Point(181, 364);
            buttonRemoveJob.Name = "buttonRemoveJob";
            buttonRemoveJob.Size = new Size(24, 23);
            buttonRemoveJob.TabIndex = 28;
            buttonRemoveJob.Text = "❌";
            buttonRemoveJob.UseVisualStyleBackColor = true;
            buttonRemoveJob.Click += buttonRemoveJob_Click;
            // 
            // buttonCopyJobs
            // 
            buttonCopyJobs.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonCopyJobs.Location = new Point(594, 364);
            buttonCopyJobs.Name = "buttonCopyJobs";
            buttonCopyJobs.Size = new Size(55, 23);
            buttonCopyJobs.TabIndex = 27;
            buttonCopyJobs.Text = "Copy";
            toolTip1.SetToolTip(buttonCopyJobs, "You may copy joblist to notepad to edit and to paste it back.");
            buttonCopyJobs.UseVisualStyleBackColor = true;
            buttonCopyJobs.Click += buttonCopyJobs_Click;
            // 
            // buttonPasteJobs
            // 
            buttonPasteJobs.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonPasteJobs.Location = new Point(655, 364);
            buttonPasteJobs.Name = "buttonPasteJobs";
            buttonPasteJobs.Size = new Size(55, 23);
            buttonPasteJobs.TabIndex = 26;
            buttonPasteJobs.Text = "Paste";
            toolTip1.SetToolTip(buttonPasteJobs, "You may copy joblist to notepad to edit and to paste it back.");
            buttonPasteJobs.UseVisualStyleBackColor = true;
            buttonPasteJobs.Click += buttonPasteJobs_Click;
            // 
            // listViewJobs
            // 
            listViewJobs.AllowDrop = true;
            listViewJobs.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
            listViewJobs.CheckBoxes = true;
            listViewJobs.Columns.AddRange(new ColumnHeader[] { JobType, Passes, Parameters });
            listViewJobs.FullRowSelect = true;
            listViewJobs.Location = new Point(6, 22);
            listViewJobs.Name = "listViewJobs";
            listViewJobs.OwnerDraw = true;
            listViewJobs.Size = new Size(765, 336);
            listViewJobs.TabIndex = 25;
            listViewJobs.UseCompatibleStateImageBehavior = false;
            listViewJobs.View = View.Details;
            // 
            // JobType
            // 
            JobType.Tag = "JobType";
            JobType.Text = "Job Type";
            JobType.Width = 66;
            // 
            // Passes
            // 
            Passes.Tag = "Passes";
            Passes.Text = "Passes";
            Passes.TextAlign = HorizontalAlignment.Center;
            Passes.Width = 46;
            // 
            // Parameters
            // 
            Parameters.Tag = "Parameters";
            Parameters.Text = "Parameters";
            Parameters.Width = 632;
            // 
            // buttonStartJobList
            // 
            buttonStartJobList.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            buttonStartJobList.Location = new Point(6, 364);
            buttonStartJobList.Name = "buttonStartJobList";
            buttonStartJobList.Size = new Size(110, 23);
            buttonStartJobList.TabIndex = 24;
            buttonStartJobList.Text = "Start Job List";
            toolTip1.SetToolTip(buttonStartJobList, "Start all checked jobs to encode and decode all checked audio files using all checked encoders with listed parameters.");
            buttonStartJobList.UseVisualStyleBackColor = true;
            buttonStartJobList.Click += buttonStartJobList_Click;
            // 
            // buttonExportJobList
            // 
            buttonExportJobList.Anchor = AnchorStyles.Bottom;
            buttonExportJobList.Location = new Point(507, 364);
            buttonExportJobList.Name = "buttonExportJobList";
            buttonExportJobList.Size = new Size(55, 23);
            buttonExportJobList.TabIndex = 3;
            buttonExportJobList.Text = "Export";
            buttonExportJobList.UseVisualStyleBackColor = true;
            buttonExportJobList.Click += buttonExportJobList_Click;
            // 
            // buttonImportJobList
            // 
            buttonImportJobList.Anchor = AnchorStyles.Bottom;
            buttonImportJobList.Location = new Point(446, 364);
            buttonImportJobList.Name = "buttonImportJobList";
            buttonImportJobList.Size = new Size(55, 23);
            buttonImportJobList.TabIndex = 3;
            buttonImportJobList.Text = "Import";
            buttonImportJobList.UseVisualStyleBackColor = true;
            buttonImportJobList.Click += buttonImportJobList_Click;
            // 
            // buttonClearJobList
            // 
            buttonClearJobList.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonClearJobList.Location = new Point(716, 364);
            buttonClearJobList.Name = "buttonClearJobList";
            buttonClearJobList.Size = new Size(55, 23);
            buttonClearJobList.TabIndex = 11;
            buttonClearJobList.Text = "Clear";
            buttonClearJobList.UseVisualStyleBackColor = true;
            buttonClearJobList.Click += buttonClearJobList_Click;
            // 
            // groupBoxLog
            // 
            groupBoxLog.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            groupBoxLog.Controls.Add(buttonLogToExcel);
            groupBoxLog.Controls.Add(buttonAnalyzeLog);
            groupBoxLog.Controls.Add(labelStopped);
            groupBoxLog.Controls.Add(buttonStop);
            groupBoxLog.Controls.Add(dataGridViewLog);
            groupBoxLog.Controls.Add(buttonClearLog);
            groupBoxLog.Controls.Add(buttonCopyLog);
            groupBoxLog.Controls.Add(buttonOpenLogtxt);
            groupBoxLog.Location = new Point(795, 455);
            groupBoxLog.Name = "groupBoxLog";
            groupBoxLog.Size = new Size(777, 394);
            groupBoxLog.TabIndex = 6;
            groupBoxLog.TabStop = false;
            groupBoxLog.Text = "Log";
            // 
            // buttonLogToExcel
            // 
            buttonLogToExcel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonLogToExcel.Location = new Point(473, 364);
            buttonLogToExcel.Name = "buttonLogToExcel";
            buttonLogToExcel.Size = new Size(85, 23);
            buttonLogToExcel.TabIndex = 28;
            buttonLogToExcel.Text = "Log to Excel";
            buttonLogToExcel.UseVisualStyleBackColor = true;
            buttonLogToExcel.Click += buttonLogToExcel_Click;
            // 
            // buttonAnalyzeLog
            // 
            buttonAnalyzeLog.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonAnalyzeLog.Location = new Point(382, 364);
            buttonAnalyzeLog.Name = "buttonAnalyzeLog";
            buttonAnalyzeLog.Size = new Size(85, 23);
            buttonAnalyzeLog.TabIndex = 27;
            buttonAnalyzeLog.Text = "Analyze log";
            toolTip1.SetToolTip(buttonAnalyzeLog, resources.GetString("buttonAnalyzeLog.ToolTip"));
            buttonAnalyzeLog.UseVisualStyleBackColor = true;
            buttonAnalyzeLog.Click += buttonAnalyzeLog_Click;
            // 
            // labelStopped
            // 
            labelStopped.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            labelStopped.AutoSize = true;
            labelStopped.ForeColor = Color.Red;
            labelStopped.Location = new Point(122, 368);
            labelStopped.Name = "labelStopped";
            labelStopped.Size = new Size(0, 15);
            labelStopped.TabIndex = 26;
            // 
            // dataGridViewLog
            // 
            dataGridViewLog.AllowUserToAddRows = false;
            dataGridViewLog.AllowUserToOrderColumns = true;
            dataGridViewLog.AllowUserToResizeRows = false;
            dataGridViewLog.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dataGridViewLog.BackgroundColor = SystemColors.Control;
            dataGridViewLog.BorderStyle = BorderStyle.Fixed3D;
            dataGridViewCellStyle3.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle3.BackColor = SystemColors.Control;
            dataGridViewCellStyle3.Font = new Font("Segoe UI", 9F);
            dataGridViewCellStyle3.ForeColor = SystemColors.WindowText;
            dataGridViewCellStyle3.SelectionBackColor = SystemColors.Highlight;
            dataGridViewCellStyle3.SelectionForeColor = SystemColors.HighlightText;
            dataGridViewCellStyle3.WrapMode = DataGridViewTriState.True;
            dataGridViewLog.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle3;
            dataGridViewLog.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dataGridViewCellStyle4.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle4.BackColor = SystemColors.Control;
            dataGridViewCellStyle4.Font = new Font("Segoe UI", 9F);
            dataGridViewCellStyle4.ForeColor = SystemColors.ControlText;
            dataGridViewCellStyle4.SelectionBackColor = SystemColors.Highlight;
            dataGridViewCellStyle4.SelectionForeColor = SystemColors.HighlightText;
            dataGridViewCellStyle4.WrapMode = DataGridViewTriState.False;
            dataGridViewLog.DefaultCellStyle = dataGridViewCellStyle4;
            dataGridViewLog.GridColor = SystemColors.Control;
            dataGridViewLog.Location = new Point(6, 22);
            dataGridViewLog.Name = "dataGridViewLog";
            dataGridViewLog.ReadOnly = true;
            dataGridViewLog.RowHeadersVisible = false;
            dataGridViewLog.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGridViewLog.Size = new Size(765, 336);
            dataGridViewLog.TabIndex = 17;
            // 
            // buttonCopyLog
            // 
            buttonCopyLog.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonCopyLog.Location = new Point(655, 364);
            buttonCopyLog.Name = "buttonCopyLog";
            buttonCopyLog.Size = new Size(55, 23);
            buttonCopyLog.TabIndex = 16;
            buttonCopyLog.Text = "Copy";
            buttonCopyLog.UseVisualStyleBackColor = true;
            buttonCopyLog.Click += buttonCopyLog_Click;
            // 
            // groupBoxSystemandSettings
            // 
            groupBoxSystemandSettings.Controls.Add(labelCPUPriority);
            groupBoxSystemandSettings.Controls.Add(comboBoxCPUPriority);
            groupBoxSystemandSettings.Controls.Add(buttonSelectTempFolder);
            groupBoxSystemandSettings.Controls.Add(checkBoxClearTempFolder);
            groupBoxSystemandSettings.Controls.Add(labelCPUinfo);
            groupBoxSystemandSettings.Location = new Point(1399, 278);
            groupBoxSystemandSettings.Name = "groupBoxSystemandSettings";
            groupBoxSystemandSettings.Size = new Size(173, 171);
            groupBoxSystemandSettings.TabIndex = 25;
            groupBoxSystemandSettings.TabStop = false;
            groupBoxSystemandSettings.Text = "System and Settings";
            // 
            // labelCPUPriority
            // 
            labelCPUPriority.AutoSize = true;
            labelCPUPriority.Location = new Point(6, 90);
            labelCPUPriority.Name = "labelCPUPriority";
            labelCPUPriority.Size = new Size(48, 15);
            labelCPUPriority.TabIndex = 26;
            labelCPUPriority.Text = "Priority:";
            // 
            // comboBoxCPUPriority
            // 
            comboBoxCPUPriority.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBoxCPUPriority.FormattingEnabled = true;
            comboBoxCPUPriority.Items.AddRange(new object[] { "RealTime", "High", "AboveNormal", "Normal", "BelowNormal", "Idle" });
            comboBoxCPUPriority.Location = new Point(60, 87);
            comboBoxCPUPriority.MaxDropDownItems = 6;
            comboBoxCPUPriority.Name = "comboBoxCPUPriority";
            comboBoxCPUPriority.Size = new Size(107, 23);
            comboBoxCPUPriority.TabIndex = 25;
            // 
            // buttonSelectTempFolder
            // 
            buttonSelectTempFolder.Location = new Point(6, 141);
            buttonSelectTempFolder.Name = "buttonSelectTempFolder";
            buttonSelectTempFolder.Size = new Size(161, 23);
            buttonSelectTempFolder.TabIndex = 23;
            buttonSelectTempFolder.Text = "Select temp folder";
            buttonSelectTempFolder.UseVisualStyleBackColor = true;
            buttonSelectTempFolder.Click += buttonSelectTempFolder_Click;
            // 
            // checkBoxClearTempFolder
            // 
            checkBoxClearTempFolder.AutoSize = true;
            checkBoxClearTempFolder.Location = new Point(6, 116);
            checkBoxClearTempFolder.Name = "checkBoxClearTempFolder";
            checkBoxClearTempFolder.Size = new Size(157, 19);
            checkBoxClearTempFolder.TabIndex = 18;
            checkBoxClearTempFolder.Text = "Clear temp folder on exit";
            checkBoxClearTempFolder.UseVisualStyleBackColor = true;
            // 
            // groupBoxDecoderSettings
            // 
            groupBoxDecoderSettings.Controls.Add(labelDecoderProgress);
            groupBoxDecoderSettings.Controls.Add(buttonStartDecode);
            groupBoxDecoderSettings.Controls.Add(labelCommandLineDecoder);
            groupBoxDecoderSettings.Controls.Add(buttonAddJobToJobListDecoder);
            groupBoxDecoderSettings.Controls.Add(textBoxCommandLineOptionsDecoder);
            groupBoxDecoderSettings.Controls.Add(progressBarDecoder);
            groupBoxDecoderSettings.Controls.Add(buttonClearCommandLineDecoder);
            groupBoxDecoderSettings.Location = new Point(795, 278);
            groupBoxDecoderSettings.Name = "groupBoxDecoderSettings";
            groupBoxDecoderSettings.Size = new Size(598, 171);
            groupBoxDecoderSettings.TabIndex = 26;
            groupBoxDecoderSettings.TabStop = false;
            groupBoxDecoderSettings.Text = "Decoder Settings";
            // 
            // labelDecoderProgress
            // 
            labelDecoderProgress.AutoSize = true;
            labelDecoderProgress.BackColor = SystemColors.Control;
            labelDecoderProgress.Location = new Point(373, 145);
            labelDecoderProgress.Name = "labelDecoderProgress";
            labelDecoderProgress.Size = new Size(0, 15);
            labelDecoderProgress.TabIndex = 25;
            // 
            // labelCommandLineDecoder
            // 
            labelCommandLineDecoder.AutoSize = true;
            labelCommandLineDecoder.Location = new Point(27, 24);
            labelCommandLineDecoder.Name = "labelCommandLineDecoder";
            labelCommandLineDecoder.Size = new Size(89, 15);
            labelCommandLineDecoder.TabIndex = 24;
            labelCommandLineDecoder.Text = "Command line:";
            // 
            // buttonAddJobToJobListDecoder
            // 
            buttonAddJobToJobListDecoder.Location = new Point(122, 141);
            buttonAddJobToJobListDecoder.Name = "buttonAddJobToJobListDecoder";
            buttonAddJobToJobListDecoder.Size = new Size(110, 23);
            buttonAddJobToJobListDecoder.TabIndex = 24;
            buttonAddJobToJobListDecoder.Text = "Add to Job List";
            toolTip1.SetToolTip(buttonAddJobToJobListDecoder, "This will add decoding parameters to a job list.\r\n\r\nLater you may use them to bencmark all checked encoders with all checked audiofiles.\r\n");
            buttonAddJobToJobListDecoder.UseVisualStyleBackColor = true;
            buttonAddJobToJobListDecoder.Click += buttonAddJobToJobListDecoder_Click;
            // 
            // textBoxCommandLineOptionsDecoder
            // 
            textBoxCommandLineOptionsDecoder.Location = new Point(122, 21);
            textBoxCommandLineOptionsDecoder.Name = "textBoxCommandLineOptionsDecoder";
            textBoxCommandLineOptionsDecoder.Size = new Size(409, 23);
            textBoxCommandLineOptionsDecoder.TabIndex = 4;
            // 
            // progressBarDecoder
            // 
            progressBarDecoder.Location = new Point(238, 141);
            progressBarDecoder.Name = "progressBarDecoder";
            progressBarDecoder.Size = new Size(293, 23);
            progressBarDecoder.TabIndex = 4;
            // 
            // buttonClearCommandLineDecoder
            // 
            buttonClearCommandLineDecoder.Location = new Point(537, 21);
            buttonClearCommandLineDecoder.Name = "buttonClearCommandLineDecoder";
            buttonClearCommandLineDecoder.Size = new Size(55, 23);
            buttonClearCommandLineDecoder.TabIndex = 11;
            buttonClearCommandLineDecoder.Text = "Clear";
            buttonClearCommandLineDecoder.UseVisualStyleBackColor = true;
            buttonClearCommandLineDecoder.Click += buttonClearCommandLineDecoder_Click;
            // 
            // toolTip1
            // 
            toolTip1.AutoPopDelay = 20000;
            toolTip1.InitialDelay = 500;
            toolTip1.ReshowDelay = 100;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = buttonStop;
            ClientSize = new Size(1584, 861);
            Controls.Add(groupBoxDecoderSettings);
            Controls.Add(groupBoxLog);
            Controls.Add(groupBoxSystemandSettings);
            Controls.Add(groupBoxJobsList);
            Controls.Add(groupBoxAudioFiles);
            Controls.Add(groupBoxEncoders);
            Controls.Add(groupBoxEncoderSettings);
            DoubleBuffered = true;
            Icon = (Icon)resources.GetObject("$this.Icon");
            KeyPreview = true;
            Name = "Form1";
            Text = "FLAC Benchmark-H [beta 0.9 build 20250105.1]";
            Load += Form1_Load;
            groupBoxEncoderSettings.ResumeLayout(false);
            groupBoxEncoderSettings.PerformLayout();
            groupBoxEncoders.ResumeLayout(false);
            groupBoxAudioFiles.ResumeLayout(false);
            groupBoxJobsList.ResumeLayout(false);
            groupBoxJobsList.PerformLayout();
            groupBoxLog.ResumeLayout(false);
            groupBoxLog.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridViewLog).EndInit();
            groupBoxSystemandSettings.ResumeLayout(false);
            groupBoxSystemandSettings.PerformLayout();
            groupBoxDecoderSettings.ResumeLayout(false);
            groupBoxDecoderSettings.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private GroupBox groupBoxEncoderSettings;
        private ListView listViewEncoders;
        private Label labelCompressionLevel;
        private Label labelThreads;
        private TextBox textBoxThreads;
        private TextBox textBoxCompressionLevel;
        private TextBox textBoxCommandLineOptionsEncoder;
        private ProgressBar progressBarEncoder;
        private Button buttonStartEncode;
        private Button buttonepr8;
        private Button buttonAsubdividetukey5flattop;
        private Button buttonNoPadding;
        private Button buttonNoSeektable;
        private Button buttonClearCommandLineEncoder;
        private Button buttonClearLog;
        private GroupBox groupBoxEncoders;
        private Button buttonOpenLogtxt;
        private Label labelCPUinfo;
        private Button buttonSetMaxCores;
        private Button buttonHalfCores;
        private Button buttonSetHalfThreads;
        private Button buttonSetMaxThreads;
        private Button button5CompressionLevel;
        private Button buttonMaxCompressionLevel;
        private Label labelSetCores;
        private Label labelSetThreads;
        private GroupBox groupBoxAudioFiles;
        private ListView listViewAudioFiles;
        private Button buttonStartDecode;
        private GroupBox groupBoxJobsList;
        private GroupBox groupLog;
        private Button buttonExportJobList;
        private Button buttonImportJobList;
        private Button buttonClearJobList;
        private Button buttonAddEncoders;
        private Button buttonClearEncoders;
        private Button buttonAddAudioFiles;
        private Button buttonClearAudioFiles;
        private Button buttonRemoveEncoder;
        private Button buttonRemoveAudiofile;
        private GroupBox groupBoxSystemandSettings;
        private Button buttonStartJobList;
        private Button buttonAddJobToJobListEncoder;
        private Button buttonCopyLog;
        private Label labelCommandLineEncoder;
        private Button buttonStop;
        private DataGridView dataGridViewLog;
        private GroupBox groupBoxDecoderSettings;
        private Label labelCommandLineDecoder;
        private Button buttonAddJobToJobListDecoder;
        private TextBox textBoxCommandLineOptionsDecoder;
        private Button buttonClearCommandLineDecoder;
        private ProgressBar progressBarDecoder;
        private Label labelSetCompression;
        private CheckBox checkBoxClearTempFolder;
        private ColumnHeader FileName;
        private ColumnHeader Duration;
        private ColumnHeader BitDepth;
        private ColumnHeader SamplingRate;
        private ColumnHeader Size;
        private Button buttonSelectTempFolder;
        private ColumnHeader FileNameExe;
        private ColumnHeader Version;
        private ColumnHeader SizeEexe;
        private ColumnHeader DateExe;
        private ColumnHeader VersionExe;
        private ListView listViewJobs;
        private ColumnHeader JobType;
        private ColumnHeader Parameters;
        private Button buttonCopyJobs;
        private Button buttonPasteJobs;
        private Button buttonRemoveJob;
        private Button buttonDownEncoder;
        private Button buttonUpEncoder;
        private Button buttonDownAudioFile;
        private Button buttonUpAudioFile;
        private Button buttonDownJob;
        private Button buttonUpJob;
        private Label labelStopped;
        private ColumnHeader Passes;
        private Button buttonMinusPass;
        private Button buttonPlusPass;
        private Label labelPasses;
        private Button buttonAnalyzeLog;
        private ComboBox comboBoxCPUPriority;
        private Label labelCPUPriority;
        private Button buttonDetectDupesAudioFiles;
        private ColumnHeader MD5Hash;
        private GroupBox groupBoxLog;
        private Button buttonLogToExcel;
        private Button buttonClearUnchecked;
        private Button buttonTestForErrors;
        private ColumnHeader FilePath;
        private ToolTip toolTip1;
        private Label labelEncoderProgress;
        private Label labelDecoderProgress;
    }
}
