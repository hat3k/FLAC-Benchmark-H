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
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            DataGridViewCellStyle dataGridViewCellStyle1 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle2 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle3 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle4 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle5 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle6 = new DataGridViewCellStyle();
            groupBoxEncoderSettings = new GroupBox();
            labelCompressionLevel = new Label();
            textBoxCompressionLevel = new TextBox();
            labelSetCompression = new Label();
            button5CompressionLevel = new Button();
            buttonMaxCompressionLevel = new Button();
            labelThreads = new Label();
            textBoxThreads = new TextBox();
            labelSetCores = new Label();
            buttonHalfCores = new Button();
            buttonSetMaxCores = new Button();
            labelSetThreads = new Label();
            buttonSetHalfThreads = new Button();
            buttonSetMaxThreads = new Button();
            labelCommandLineEncoder = new Label();
            textBoxCommandLineOptionsEncoder = new TextBox();
            buttonClearCommandLineEncoder = new Button();
            buttonEpr8 = new Button();
            buttonAsubdividetukey5flattop = new Button();
            buttonNoPadding = new Button();
            buttonNoSeektable = new Button();
            buttonStartEncode = new Button();
            buttonAddJobToJobListEncoder = new Button();
            buttonScriptConstructor = new Button();
            progressBarEncoder = new ProgressBarEx();
            checkBoxWarmupPass = new CheckBox();
            labelCpuUsageTitle = new Label();
            labelCpuInfo = new Label();
            buttonStop = new Button();
            buttonStartDecode = new Button();
            buttonOpenLogtxt = new Button();
            buttonClearLog = new Button();
            groupBoxEncoders = new GroupBox();
            listViewEncoders = new ListView();
            FileNameExe = new ColumnHeader();
            VersionExe = new ColumnHeader();
            EncoderDirectory = new ColumnHeader();
            SizeExe = new ColumnHeader();
            DateExe = new ColumnHeader();
            buttonAddEncoders = new Button();
            buttonUpEncoder = new Button();
            buttonDownEncoder = new Button();
            buttonRemoveEncoder = new Button();
            buttonClearEncoders = new Button();
            groupBoxAudioFiles = new GroupBox();
            listViewAudioFiles = new ListView();
            FileName = new ColumnHeader();
            Channels = new ColumnHeader();
            BitDepth = new ColumnHeader();
            SamplingRate = new ColumnHeader();
            Duration = new ColumnHeader();
            InputAudioFileSize = new ColumnHeader();
            MD5Hash = new ColumnHeader();
            FilePath = new ColumnHeader();
            buttonAddAudioFiles = new Button();
            buttonUpAudioFile = new Button();
            buttonDownAudioFile = new Button();
            buttonRemoveAudioFile = new Button();
            buttonDetectDupesAudioFiles = new Button();
            buttonTestForErrors = new Button();
            checkBoxWarningsAsErrors = new CheckBox();
            buttonClearUnchecked = new Button();
            buttonClearAudioFiles = new Button();
            labelAudioFileRemoved = new Label();
            groupBoxJobsList = new GroupBox();
            dataGridViewJobs = new DataGridViewEx();
            Column1CheckBox = new DataGridViewCheckBoxColumn();
            Column2JobType = new DataGridViewTextBoxColumn();
            Column3Passes = new DataGridViewTextBoxColumn();
            Column4Parameters = new DataGridViewTextBoxColumn();
            buttonStartJobList = new Button();
            buttonUpJob = new Button();
            buttonDownJob = new Button();
            buttonRemoveJob = new Button();
            buttonMinusPass = new Button();
            labelPasses = new Label();
            buttonPlusPass = new Button();
            buttonExportJobList = new Button();
            buttonImportJobList = new Button();
            buttonCopyJobs = new Button();
            buttonPasteJobs = new Button();
            buttonClearJobList = new Button();
            groupBoxLog = new GroupBox();
            tabControlLog = new TabControl();
            Benchmark = new TabPage();
            dataGridViewLog = new DataGridViewEx();
            ScalingPlots = new TabPage();
            tabControlScalingPlots = new TabControl();
            tabPageSpeedByThreads = new TabPage();
            plotScalingPlotSpeedByThreads = new ScottPlot.FormsPlot();
            tabPageCPULoadByThreads = new TabPage();
            plotScalingPlotCPULoadByThreads = new ScottPlot.FormsPlot();
            tabPageCPUClockByThreads = new TabPage();
            plotScalingPlotCPUClockByThreads = new ScottPlot.FormsPlot();
            tabPageMultiplotByThreads = new TabPage();
            tableLayoutPanelMultiPlotByThreads = new TableLayoutPanel();
            plotScalingMultiPlotSpeedByThreads = new ScottPlot.FormsPlot();
            plotScalingMultiPlotCPULoadByThreads = new ScottPlot.FormsPlot();
            plotScalingMultiPlotCPUClockByThreads = new ScottPlot.FormsPlot();
            tabPageSpeedByParameters = new TabPage();
            plotScalingPlotSpeedByParameters = new ScottPlot.FormsPlot();
            tabPageCompressionByParameters = new TabPage();
            plotScalingPlotCompressionByParameters = new ScottPlot.FormsPlot();
            tabPageMultiplotByParameters = new TabPage();
            tableLayoutPanelMultiPlotByParameters = new TableLayoutPanel();
            plotScalingMultiPlotSpeedByParameters = new ScottPlot.FormsPlot();
            plotScalingMultiPlotCompressionByParameters = new ScottPlot.FormsPlot();
            DetectDupes = new TabPage();
            dataGridViewLogDetectDupes = new DataGridViewEx();
            TestForErrors = new TabPage();
            dataGridViewLogTestForErrors = new DataGridViewEx();
            buttonPauseResume = new Button();
            labelStopped = new Label();
            buttonAnalyzeLog = new Button();
            buttonLogToExcel = new Button();
            buttonCopyLogAsBBCode = new Button();
            buttonCopyLog = new Button();
            buttonLogColumnsAutoWidth = new Button();
            buttonDataGridViewLogSettings = new Button();
            groupBoxSettings = new GroupBox();
            tabControlSettings = new TabControl();
            tabPageQuickSettings = new TabPage();
            checkBoxRemoveMetadata = new CheckBox();
            checkBoxPreventSleep = new CheckBox();
            checkBoxAutoAnalyzeLog = new CheckBox();
            labelCPUPriority = new Label();
            comboBoxCPUPriority = new ComboBox();
            tabPageLogsSettings = new TabPage();
            checkBoxClearLogsOnExit = new CheckBox();
            checkBoxClearLogsOnExitIncludeDetectDupes = new CheckBox();
            checkBoxClearLogsOnExitIncludeTestForErrors = new CheckBox();
            checkBoxClearLogsOnExitIncludeLogTXT = new CheckBox();
            tabPagePlotsSettings = new TabPage();
            checkBoxDrawMultiplots = new CheckBox();
            checkBoxShowIndividualFilesPlots = new CheckBox();
            checkBoxShowAggregatedByEncoderPlots = new CheckBox();
            checkBoxShowIdealCPULoadLine = new CheckBox();
            checkBoxShowTooltipsOnPlots = new CheckBox();
            tabPageMiscSettings = new TabPage();
            buttonSelectTempFolder = new Button();
            checkBoxClearTempFolder = new CheckBox();
            checkBoxAddMD5OnLoadWav = new CheckBox();
            checkBoxCheckForUpdatesOnStartup = new CheckBox();
            groupBoxDecoderSettings = new GroupBox();
            labelCommandLineDecoder = new Label();
            textBoxCommandLineOptionsDecoder = new TextBox();
            buttonClearCommandLineDecoder = new Button();
            buttonAddJobToJobListDecoder = new Button();
            progressBarDecoder = new ProgressBarEx();
            toolTip1 = new ToolTip(components);
            groupBoxInformation = new GroupBox();
            labelCpuUsageValue = new Label();
            buttonAbout = new Button();
            groupBoxEncoderSettings.SuspendLayout();
            groupBoxEncoders.SuspendLayout();
            groupBoxAudioFiles.SuspendLayout();
            groupBoxJobsList.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridViewJobs).BeginInit();
            groupBoxLog.SuspendLayout();
            tabControlLog.SuspendLayout();
            Benchmark.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridViewLog).BeginInit();
            ScalingPlots.SuspendLayout();
            tabControlScalingPlots.SuspendLayout();
            tabPageSpeedByThreads.SuspendLayout();
            tabPageCPULoadByThreads.SuspendLayout();
            tabPageCPUClockByThreads.SuspendLayout();
            tabPageMultiplotByThreads.SuspendLayout();
            tableLayoutPanelMultiPlotByThreads.SuspendLayout();
            tabPageSpeedByParameters.SuspendLayout();
            tabPageCompressionByParameters.SuspendLayout();
            tabPageMultiplotByParameters.SuspendLayout();
            tableLayoutPanelMultiPlotByParameters.SuspendLayout();
            DetectDupes.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridViewLogDetectDupes).BeginInit();
            TestForErrors.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridViewLogTestForErrors).BeginInit();
            groupBoxSettings.SuspendLayout();
            tabControlSettings.SuspendLayout();
            tabPageQuickSettings.SuspendLayout();
            tabPageLogsSettings.SuspendLayout();
            tabPagePlotsSettings.SuspendLayout();
            tabPageMiscSettings.SuspendLayout();
            groupBoxDecoderSettings.SuspendLayout();
            groupBoxInformation.SuspendLayout();
            SuspendLayout();
            // 
            // groupBoxEncoderSettings
            // 
            groupBoxEncoderSettings.Controls.Add(labelCompressionLevel);
            groupBoxEncoderSettings.Controls.Add(textBoxCompressionLevel);
            groupBoxEncoderSettings.Controls.Add(labelSetCompression);
            groupBoxEncoderSettings.Controls.Add(button5CompressionLevel);
            groupBoxEncoderSettings.Controls.Add(buttonMaxCompressionLevel);
            groupBoxEncoderSettings.Controls.Add(labelThreads);
            groupBoxEncoderSettings.Controls.Add(textBoxThreads);
            groupBoxEncoderSettings.Controls.Add(labelSetCores);
            groupBoxEncoderSettings.Controls.Add(buttonHalfCores);
            groupBoxEncoderSettings.Controls.Add(buttonSetMaxCores);
            groupBoxEncoderSettings.Controls.Add(labelSetThreads);
            groupBoxEncoderSettings.Controls.Add(buttonSetHalfThreads);
            groupBoxEncoderSettings.Controls.Add(buttonSetMaxThreads);
            groupBoxEncoderSettings.Controls.Add(labelCommandLineEncoder);
            groupBoxEncoderSettings.Controls.Add(textBoxCommandLineOptionsEncoder);
            groupBoxEncoderSettings.Controls.Add(buttonClearCommandLineEncoder);
            groupBoxEncoderSettings.Controls.Add(buttonEpr8);
            groupBoxEncoderSettings.Controls.Add(buttonAsubdividetukey5flattop);
            groupBoxEncoderSettings.Controls.Add(buttonNoPadding);
            groupBoxEncoderSettings.Controls.Add(buttonNoSeektable);
            groupBoxEncoderSettings.Controls.Add(buttonStartEncode);
            groupBoxEncoderSettings.Controls.Add(buttonAddJobToJobListEncoder);
            groupBoxEncoderSettings.Controls.Add(buttonScriptConstructor);
            groupBoxEncoderSettings.Controls.Add(progressBarEncoder);
            groupBoxEncoderSettings.Location = new Point(12, 278);
            groupBoxEncoderSettings.MinimumSize = new Size(630, 171);
            groupBoxEncoderSettings.Name = "groupBoxEncoderSettings";
            groupBoxEncoderSettings.Size = new Size(630, 171);
            groupBoxEncoderSettings.TabIndex = 2;
            groupBoxEncoderSettings.TabStop = false;
            groupBoxEncoderSettings.Text = "Encoder Settings";
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
            // textBoxCompressionLevel
            // 
            textBoxCompressionLevel.Location = new Point(122, 21);
            textBoxCompressionLevel.Name = "textBoxCompressionLevel";
            textBoxCompressionLevel.Size = new Size(28, 23);
            textBoxCompressionLevel.TabIndex = 1;
            textBoxCompressionLevel.Text = "8";
            textBoxCompressionLevel.TextAlign = HorizontalAlignment.Center;
            // 
            // labelSetCompression
            // 
            labelSetCompression.AutoSize = true;
            labelSetCompression.Location = new Point(170, 24);
            labelSetCompression.Name = "labelSetCompression";
            labelSetCompression.Size = new Size(26, 15);
            labelSetCompression.TabIndex = 2;
            labelSetCompression.Text = "Set:";
            // 
            // button5CompressionLevel
            // 
            button5CompressionLevel.Location = new Point(202, 21);
            button5CompressionLevel.Name = "button5CompressionLevel";
            button5CompressionLevel.Size = new Size(53, 23);
            button5CompressionLevel.TabIndex = 3;
            button5CompressionLevel.Text = "Default";
            button5CompressionLevel.UseVisualStyleBackColor = true;
            button5CompressionLevel.Click += Button5CompressionLevel_Click;
            // 
            // buttonMaxCompressionLevel
            // 
            buttonMaxCompressionLevel.Location = new Point(261, 21);
            buttonMaxCompressionLevel.Name = "buttonMaxCompressionLevel";
            buttonMaxCompressionLevel.Size = new Size(53, 23);
            buttonMaxCompressionLevel.TabIndex = 4;
            buttonMaxCompressionLevel.Text = "MAX";
            buttonMaxCompressionLevel.UseVisualStyleBackColor = true;
            buttonMaxCompressionLevel.Click += ButtonMaxCompressionLevel_Click;
            // 
            // labelThreads
            // 
            labelThreads.AutoSize = true;
            labelThreads.Location = new Point(65, 54);
            labelThreads.Name = "labelThreads";
            labelThreads.Size = new Size(51, 15);
            labelThreads.TabIndex = 5;
            labelThreads.Text = "Threads:";
            toolTip1.SetToolTip(labelThreads, "If you use FLAC 1.4.3 and earlier set this parameter to 1 or 0");
            // 
            // textBoxThreads
            // 
            textBoxThreads.Location = new Point(122, 51);
            textBoxThreads.Name = "textBoxThreads";
            textBoxThreads.Size = new Size(28, 23);
            textBoxThreads.TabIndex = 6;
            textBoxThreads.Text = "1";
            textBoxThreads.TextAlign = HorizontalAlignment.Center;
            toolTip1.SetToolTip(textBoxThreads, "If you use FLAC 1.4.3 and earlier set this parameter to 1 or 0");
            // 
            // labelSetCores
            // 
            labelSetCores.AutoSize = true;
            labelSetCores.Location = new Point(156, 54);
            labelSetCores.Name = "labelSetCores";
            labelSetCores.Size = new Size(40, 15);
            labelSetCores.TabIndex = 7;
            labelSetCores.Text = "Cores:";
            // 
            // buttonHalfCores
            // 
            buttonHalfCores.Location = new Point(202, 51);
            buttonHalfCores.Name = "buttonHalfCores";
            buttonHalfCores.Size = new Size(53, 23);
            buttonHalfCores.TabIndex = 8;
            buttonHalfCores.Text = "50%";
            buttonHalfCores.UseVisualStyleBackColor = true;
            buttonHalfCores.Click += ButtonHalfCores_Click;
            // 
            // buttonSetMaxCores
            // 
            buttonSetMaxCores.Location = new Point(261, 51);
            buttonSetMaxCores.Name = "buttonSetMaxCores";
            buttonSetMaxCores.Size = new Size(53, 23);
            buttonSetMaxCores.TabIndex = 9;
            buttonSetMaxCores.Text = "100%";
            buttonSetMaxCores.UseVisualStyleBackColor = true;
            buttonSetMaxCores.Click += ButtonSetMaxCores_Click;
            // 
            // labelSetThreads
            // 
            labelSetThreads.AutoSize = true;
            labelSetThreads.Location = new Point(322, 54);
            labelSetThreads.Name = "labelSetThreads";
            labelSetThreads.Size = new Size(51, 15);
            labelSetThreads.TabIndex = 10;
            labelSetThreads.Text = "Threads:";
            // 
            // buttonSetHalfThreads
            // 
            buttonSetHalfThreads.Location = new Point(379, 51);
            buttonSetHalfThreads.Name = "buttonSetHalfThreads";
            buttonSetHalfThreads.Size = new Size(53, 23);
            buttonSetHalfThreads.TabIndex = 11;
            buttonSetHalfThreads.Text = "50%";
            buttonSetHalfThreads.UseVisualStyleBackColor = true;
            buttonSetHalfThreads.Click += ButtonSetHalfThreads_Click;
            // 
            // buttonSetMaxThreads
            // 
            buttonSetMaxThreads.Location = new Point(438, 51);
            buttonSetMaxThreads.Name = "buttonSetMaxThreads";
            buttonSetMaxThreads.Size = new Size(53, 23);
            buttonSetMaxThreads.TabIndex = 12;
            buttonSetMaxThreads.Text = "100%";
            buttonSetMaxThreads.UseVisualStyleBackColor = true;
            buttonSetMaxThreads.Click += ButtonSetMaxThreads_Click;
            // 
            // labelCommandLineEncoder
            // 
            labelCommandLineEncoder.AutoSize = true;
            labelCommandLineEncoder.Location = new Point(27, 83);
            labelCommandLineEncoder.Name = "labelCommandLineEncoder";
            labelCommandLineEncoder.Size = new Size(89, 15);
            labelCommandLineEncoder.TabIndex = 13;
            labelCommandLineEncoder.Text = "Command line:";
            // 
            // textBoxCommandLineOptionsEncoder
            // 
            textBoxCommandLineOptionsEncoder.Location = new Point(122, 80);
            textBoxCommandLineOptionsEncoder.Name = "textBoxCommandLineOptionsEncoder";
            textBoxCommandLineOptionsEncoder.Size = new Size(440, 23);
            textBoxCommandLineOptionsEncoder.TabIndex = 14;
            // 
            // buttonClearCommandLineEncoder
            // 
            buttonClearCommandLineEncoder.Location = new Point(568, 80);
            buttonClearCommandLineEncoder.Name = "buttonClearCommandLineEncoder";
            buttonClearCommandLineEncoder.Size = new Size(55, 23);
            buttonClearCommandLineEncoder.TabIndex = 15;
            buttonClearCommandLineEncoder.Text = "Clear";
            buttonClearCommandLineEncoder.UseVisualStyleBackColor = true;
            buttonClearCommandLineEncoder.Click += ButtonClearCommandLineEncoder_Click;
            // 
            // buttonEpr8
            // 
            buttonEpr8.Location = new Point(122, 109);
            buttonEpr8.Name = "buttonEpr8";
            buttonEpr8.Size = new Size(43, 23);
            buttonEpr8.TabIndex = 16;
            buttonEpr8.Text = "-epr8";
            buttonEpr8.UseVisualStyleBackColor = true;
            buttonEpr8.Click += ButtonEpr8_Click;
            // 
            // buttonAsubdividetukey5flattop
            // 
            buttonAsubdividetukey5flattop.Location = new Point(171, 109);
            buttonAsubdividetukey5flattop.Name = "buttonAsubdividetukey5flattop";
            buttonAsubdividetukey5flattop.Size = new Size(179, 23);
            buttonAsubdividetukey5flattop.TabIndex = 17;
            buttonAsubdividetukey5flattop.Text = "-A \"subdivide_tukey(5);flattop\"";
            buttonAsubdividetukey5flattop.UseVisualStyleBackColor = true;
            buttonAsubdividetukey5flattop.Click += ButtonAsubdividetukey5flattop_Click;
            // 
            // buttonNoPadding
            // 
            buttonNoPadding.Location = new Point(356, 109);
            buttonNoPadding.Name = "buttonNoPadding";
            buttonNoPadding.Size = new Size(100, 23);
            buttonNoPadding.TabIndex = 18;
            buttonNoPadding.Text = "No Padding";
            buttonNoPadding.UseVisualStyleBackColor = true;
            buttonNoPadding.Click += ButtonNoPadding_Click;
            // 
            // buttonNoSeektable
            // 
            buttonNoSeektable.Location = new Point(462, 109);
            buttonNoSeektable.Name = "buttonNoSeektable";
            buttonNoSeektable.Size = new Size(100, 23);
            buttonNoSeektable.TabIndex = 19;
            buttonNoSeektable.Text = "No Seektable";
            buttonNoSeektable.UseVisualStyleBackColor = true;
            buttonNoSeektable.Click += ButtonNoSeektable_Click;
            // 
            // buttonStartEncode
            // 
            buttonStartEncode.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            buttonStartEncode.Location = new Point(6, 141);
            buttonStartEncode.Name = "buttonStartEncode";
            buttonStartEncode.Size = new Size(110, 23);
            buttonStartEncode.TabIndex = 20;
            buttonStartEncode.Text = "Encode";
            toolTip1.SetToolTip(buttonStartEncode, "Encode all checked audio files using all checked encoders with the specified parameters.");
            buttonStartEncode.UseVisualStyleBackColor = true;
            buttonStartEncode.Click += ButtonStartEncode_Click;
            // 
            // buttonAddJobToJobListEncoder
            // 
            buttonAddJobToJobListEncoder.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            buttonAddJobToJobListEncoder.Location = new Point(122, 141);
            buttonAddJobToJobListEncoder.Name = "buttonAddJobToJobListEncoder";
            buttonAddJobToJobListEncoder.Size = new Size(110, 23);
            buttonAddJobToJobListEncoder.TabIndex = 21;
            buttonAddJobToJobListEncoder.Text = "Add to Job List";
            toolTip1.SetToolTip(buttonAddJobToJobListEncoder, "This will add encoding parameters to a job list.\r\n\r\nLater you may use them to benchmark all checked encoders with all checked audiofiles.");
            buttonAddJobToJobListEncoder.UseVisualStyleBackColor = true;
            buttonAddJobToJobListEncoder.Click += ButtonAddJobToJobListEncoder_Click;
            // 
            // buttonScriptConstructor
            // 
            buttonScriptConstructor.Location = new Point(238, 141);
            buttonScriptConstructor.Name = "buttonScriptConstructor";
            buttonScriptConstructor.Size = new Size(112, 23);
            buttonScriptConstructor.TabIndex = 22;
            buttonScriptConstructor.Text = "Script Constructor";
            buttonScriptConstructor.UseVisualStyleBackColor = true;
            buttonScriptConstructor.Click += ButtonScriptConstructor_Click;
            // 
            // progressBarEncoder
            // 
            progressBarEncoder.DisplayType = ProgressBarEx.TextDisplayType.Manual;
            progressBarEncoder.Location = new Point(356, 141);
            progressBarEncoder.Name = "progressBarEncoder";
            progressBarEncoder.Size = new Size(206, 23);
            progressBarEncoder.TabIndex = 23;
            // 
            // checkBoxWarmupPass
            // 
            checkBoxWarmupPass.AutoSize = true;
            checkBoxWarmupPass.Location = new Point(4, 32);
            checkBoxWarmupPass.Name = "checkBoxWarmupPass";
            checkBoxWarmupPass.Size = new Size(128, 19);
            checkBoxWarmupPass.TabIndex = 1;
            checkBoxWarmupPass.Text = "Add Warm-up Pass";
            toolTip1.SetToolTip(checkBoxWarmupPass, resources.GetString("checkBoxWarmupPass.ToolTip"));
            checkBoxWarmupPass.UseVisualStyleBackColor = true;
            // 
            // labelCpuUsageTitle
            // 
            labelCpuUsageTitle.Location = new Point(5, 49);
            labelCpuUsageTitle.Name = "labelCpuUsageTitle";
            labelCpuUsageTitle.Size = new Size(69, 30);
            labelCpuUsageTitle.TabIndex = 1;
            labelCpuUsageTitle.Text = "CPU Load:\r\nCPU Clock:";
            // 
            // labelCpuInfo
            // 
            labelCpuInfo.Location = new Point(5, 19);
            labelCpuInfo.Name = "labelCpuInfo";
            labelCpuInfo.Size = new Size(132, 30);
            labelCpuInfo.TabIndex = 0;
            // 
            // buttonStop
            // 
            buttonStop.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            buttonStop.Location = new Point(6, 364);
            buttonStop.Name = "buttonStop";
            buttonStop.Size = new Size(110, 23);
            buttonStop.TabIndex = 1;
            buttonStop.Text = "Stop all (Esc)";
            toolTip1.SetToolTip(buttonStop, "Stop all encoding and decoding jobs.");
            buttonStop.UseVisualStyleBackColor = true;
            buttonStop.Click += ButtonStop_Click;
            // 
            // buttonStartDecode
            // 
            buttonStartDecode.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            buttonStartDecode.Location = new Point(6, 141);
            buttonStartDecode.Name = "buttonStartDecode";
            buttonStartDecode.Size = new Size(110, 23);
            buttonStartDecode.TabIndex = 3;
            buttonStartDecode.Text = "Decode";
            toolTip1.SetToolTip(buttonStartDecode, "Decode all checked audio files using all checked encoders with the specified parameters.");
            buttonStartDecode.UseVisualStyleBackColor = true;
            buttonStartDecode.Click += ButtonStartDecode_Click;
            // 
            // buttonOpenLogtxt
            // 
            buttonOpenLogtxt.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonOpenLogtxt.Location = new Point(564, 364);
            buttonOpenLogtxt.Name = "buttonOpenLogtxt";
            buttonOpenLogtxt.Size = new Size(85, 23);
            buttonOpenLogtxt.TabIndex = 6;
            buttonOpenLogtxt.Text = "Open log.txt";
            buttonOpenLogtxt.UseVisualStyleBackColor = true;
            buttonOpenLogtxt.Click += ButtonOpenLogtxt_Click;
            // 
            // buttonClearLog
            // 
            buttonClearLog.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonClearLog.Location = new Point(716, 364);
            buttonClearLog.Name = "buttonClearLog";
            buttonClearLog.Size = new Size(55, 23);
            buttonClearLog.TabIndex = 8;
            buttonClearLog.Text = "Clear";
            toolTip1.SetToolTip(buttonClearLog, "Clear current log tab.\r\nHold 'Shift' to clear all tabs.\r\n\r\nIf the \"Benchmark\" tab is cleared:\r\nRemoves all displayed entries and reset internal benchmark history.");
            buttonClearLog.UseVisualStyleBackColor = true;
            buttonClearLog.Click += ButtonClearLog_Click;
            // 
            // groupBoxEncoders
            // 
            groupBoxEncoders.Controls.Add(listViewEncoders);
            groupBoxEncoders.Controls.Add(buttonAddEncoders);
            groupBoxEncoders.Controls.Add(buttonUpEncoder);
            groupBoxEncoders.Controls.Add(buttonDownEncoder);
            groupBoxEncoders.Controls.Add(buttonRemoveEncoder);
            groupBoxEncoders.Controls.Add(buttonClearEncoders);
            groupBoxEncoders.Location = new Point(12, 12);
            groupBoxEncoders.MinimumSize = new Size(777, 0);
            groupBoxEncoders.Name = "groupBoxEncoders";
            groupBoxEncoders.Size = new Size(777, 260);
            groupBoxEncoders.TabIndex = 0;
            groupBoxEncoders.TabStop = false;
            groupBoxEncoders.Text = "Choose Encoder (Drag'n'Drop of files and folders is available)";
            // 
            // listViewEncoders
            // 
            listViewEncoders.Activation = ItemActivation.OneClick;
            listViewEncoders.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            listViewEncoders.CheckBoxes = true;
            listViewEncoders.Columns.AddRange(new ColumnHeader[] { FileNameExe, VersionExe, EncoderDirectory, SizeExe, DateExe });
            listViewEncoders.FullRowSelect = true;
            listViewEncoders.Location = new Point(6, 22);
            listViewEncoders.Name = "listViewEncoders";
            listViewEncoders.Size = new Size(765, 202);
            listViewEncoders.TabIndex = 0;
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
            VersionExe.Width = 100;
            // 
            // EncoderDirectory
            // 
            EncoderDirectory.Text = "File Path";
            EncoderDirectory.Width = 130;
            // 
            // SizeExe
            // 
            SizeExe.Tag = "SizeExe";
            SizeExe.Text = "Size";
            SizeExe.TextAlign = HorizontalAlignment.Right;
            SizeExe.Width = 93;
            // 
            // DateExe
            // 
            DateExe.Tag = "DateExe";
            DateExe.Text = "Date";
            DateExe.Width = 100;
            // 
            // buttonAddEncoders
            // 
            buttonAddEncoders.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            buttonAddEncoders.Font = new Font("Segoe UI", 9F);
            buttonAddEncoders.Location = new Point(6, 230);
            buttonAddEncoders.Name = "buttonAddEncoders";
            buttonAddEncoders.Size = new Size(110, 23);
            buttonAddEncoders.TabIndex = 1;
            buttonAddEncoders.Text = "Add encoders";
            buttonAddEncoders.UseVisualStyleBackColor = true;
            buttonAddEncoders.Click += ButtonAddEncoders_Click;
            // 
            // buttonUpEncoder
            // 
            buttonUpEncoder.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            buttonUpEncoder.Location = new Point(122, 230);
            buttonUpEncoder.Name = "buttonUpEncoder";
            buttonUpEncoder.Size = new Size(24, 23);
            buttonUpEncoder.TabIndex = 2;
            buttonUpEncoder.Text = "▲";
            buttonUpEncoder.UseVisualStyleBackColor = true;
            buttonUpEncoder.Click += ButtonUpEncoder_Click;
            // 
            // buttonDownEncoder
            // 
            buttonDownEncoder.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            buttonDownEncoder.Location = new Point(152, 230);
            buttonDownEncoder.Name = "buttonDownEncoder";
            buttonDownEncoder.Size = new Size(24, 23);
            buttonDownEncoder.TabIndex = 3;
            buttonDownEncoder.Text = "▼";
            buttonDownEncoder.UseVisualStyleBackColor = true;
            buttonDownEncoder.Click += ButtonDownEncoder_Click;
            // 
            // buttonRemoveEncoder
            // 
            buttonRemoveEncoder.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            buttonRemoveEncoder.Font = new Font("Segoe UI", 9F);
            buttonRemoveEncoder.Location = new Point(182, 230);
            buttonRemoveEncoder.Name = "buttonRemoveEncoder";
            buttonRemoveEncoder.Size = new Size(24, 23);
            buttonRemoveEncoder.TabIndex = 4;
            buttonRemoveEncoder.Text = "❌";
            buttonRemoveEncoder.UseVisualStyleBackColor = true;
            buttonRemoveEncoder.Click += ButtonRemoveEncoder_Click;
            // 
            // buttonClearEncoders
            // 
            buttonClearEncoders.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonClearEncoders.Font = new Font("Segoe UI", 9F);
            buttonClearEncoders.Location = new Point(716, 230);
            buttonClearEncoders.Name = "buttonClearEncoders";
            buttonClearEncoders.Size = new Size(55, 23);
            buttonClearEncoders.TabIndex = 5;
            buttonClearEncoders.Text = "Clear";
            buttonClearEncoders.UseVisualStyleBackColor = true;
            buttonClearEncoders.Click += ButtonClearEncoders_Click;
            // 
            // groupBoxAudioFiles
            // 
            groupBoxAudioFiles.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            groupBoxAudioFiles.Controls.Add(listViewAudioFiles);
            groupBoxAudioFiles.Controls.Add(buttonAddAudioFiles);
            groupBoxAudioFiles.Controls.Add(buttonUpAudioFile);
            groupBoxAudioFiles.Controls.Add(buttonDownAudioFile);
            groupBoxAudioFiles.Controls.Add(buttonRemoveAudioFile);
            groupBoxAudioFiles.Controls.Add(buttonDetectDupesAudioFiles);
            groupBoxAudioFiles.Controls.Add(buttonTestForErrors);
            groupBoxAudioFiles.Controls.Add(checkBoxWarningsAsErrors);
            groupBoxAudioFiles.Controls.Add(buttonClearUnchecked);
            groupBoxAudioFiles.Controls.Add(buttonClearAudioFiles);
            groupBoxAudioFiles.Controls.Add(labelAudioFileRemoved);
            groupBoxAudioFiles.Location = new Point(795, 12);
            groupBoxAudioFiles.MinimumSize = new Size(777, 0);
            groupBoxAudioFiles.Name = "groupBoxAudioFiles";
            groupBoxAudioFiles.Size = new Size(777, 260);
            groupBoxAudioFiles.TabIndex = 1;
            groupBoxAudioFiles.TabStop = false;
            groupBoxAudioFiles.Text = "Choose Audio Files (Drag'n'Drop of files and folders is available)";
            // 
            // listViewAudioFiles
            // 
            listViewAudioFiles.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            listViewAudioFiles.CheckBoxes = true;
            listViewAudioFiles.Columns.AddRange(new ColumnHeader[] { FileName, Channels, BitDepth, SamplingRate, Duration, InputAudioFileSize, MD5Hash, FilePath });
            listViewAudioFiles.FullRowSelect = true;
            listViewAudioFiles.Location = new Point(6, 22);
            listViewAudioFiles.Name = "listViewAudioFiles";
            listViewAudioFiles.Size = new Size(765, 202);
            listViewAudioFiles.TabIndex = 0;
            listViewAudioFiles.UseCompatibleStateImageBehavior = false;
            listViewAudioFiles.View = View.Details;
            // 
            // FileName
            // 
            FileName.Tag = "FileName";
            FileName.Text = "File Name";
            FileName.Width = 387;
            // 
            // Channels
            // 
            Channels.Tag = "Channels";
            Channels.Text = "Channels";
            Channels.TextAlign = HorizontalAlignment.Right;
            Channels.Width = 40;
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
            // Duration
            // 
            Duration.Tag = "Duration";
            Duration.Text = "Duration";
            Duration.TextAlign = HorizontalAlignment.Right;
            Duration.Width = 100;
            // 
            // InputAudioFileSize
            // 
            InputAudioFileSize.Tag = "InputAudioFileSize";
            InputAudioFileSize.Text = "Size";
            InputAudioFileSize.TextAlign = HorizontalAlignment.Right;
            InputAudioFileSize.Width = 124;
            // 
            // MD5Hash
            // 
            MD5Hash.Tag = "MD5Hash";
            MD5Hash.Text = "MD5 Hash";
            MD5Hash.Width = 230;
            // 
            // FilePath
            // 
            FilePath.Tag = "FilePath";
            FilePath.Text = "File Path";
            FilePath.Width = 250;
            // 
            // buttonAddAudioFiles
            // 
            buttonAddAudioFiles.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            buttonAddAudioFiles.Location = new Point(6, 230);
            buttonAddAudioFiles.Name = "buttonAddAudioFiles";
            buttonAddAudioFiles.Size = new Size(110, 23);
            buttonAddAudioFiles.TabIndex = 1;
            buttonAddAudioFiles.Text = "Add audio files";
            buttonAddAudioFiles.UseVisualStyleBackColor = true;
            buttonAddAudioFiles.Click += ButtonAddAudioFiles_Click;
            // 
            // buttonUpAudioFile
            // 
            buttonUpAudioFile.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            buttonUpAudioFile.Location = new Point(122, 230);
            buttonUpAudioFile.Name = "buttonUpAudioFile";
            buttonUpAudioFile.Size = new Size(24, 23);
            buttonUpAudioFile.TabIndex = 2;
            buttonUpAudioFile.Text = "▲";
            buttonUpAudioFile.UseVisualStyleBackColor = true;
            buttonUpAudioFile.Click += ButtonUpAudioFile_Click;
            // 
            // buttonDownAudioFile
            // 
            buttonDownAudioFile.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            buttonDownAudioFile.Location = new Point(152, 230);
            buttonDownAudioFile.Name = "buttonDownAudioFile";
            buttonDownAudioFile.Size = new Size(24, 23);
            buttonDownAudioFile.TabIndex = 3;
            buttonDownAudioFile.Text = "▼";
            buttonDownAudioFile.UseVisualStyleBackColor = true;
            buttonDownAudioFile.Click += ButtonDownAudioFile_Click;
            // 
            // buttonRemoveAudioFile
            // 
            buttonRemoveAudioFile.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            buttonRemoveAudioFile.Location = new Point(182, 230);
            buttonRemoveAudioFile.Name = "buttonRemoveAudioFile";
            buttonRemoveAudioFile.Size = new Size(24, 23);
            buttonRemoveAudioFile.TabIndex = 4;
            buttonRemoveAudioFile.Text = "❌";
            buttonRemoveAudioFile.UseVisualStyleBackColor = true;
            buttonRemoveAudioFile.Click += ButtonRemoveAudioFile_Click;
            // 
            // buttonDetectDupesAudioFiles
            // 
            buttonDetectDupesAudioFiles.Anchor = AnchorStyles.Left;
            buttonDetectDupesAudioFiles.Location = new Point(238, 230);
            buttonDetectDupesAudioFiles.Name = "buttonDetectDupesAudioFiles";
            buttonDetectDupesAudioFiles.Size = new Size(110, 23);
            buttonDetectDupesAudioFiles.TabIndex = 5;
            buttonDetectDupesAudioFiles.Text = "Detect Dupes";
            toolTip1.SetToolTip(buttonDetectDupesAudioFiles, resources.GetString("buttonDetectDupesAudioFiles.ToolTip"));
            buttonDetectDupesAudioFiles.UseVisualStyleBackColor = true;
            buttonDetectDupesAudioFiles.Click += ButtonDetectDupesAudioFiles_Click;
            // 
            // buttonTestForErrors
            // 
            buttonTestForErrors.Anchor = AnchorStyles.Left;
            buttonTestForErrors.Location = new Point(354, 230);
            buttonTestForErrors.Name = "buttonTestForErrors";
            buttonTestForErrors.Size = new Size(110, 23);
            buttonTestForErrors.TabIndex = 6;
            buttonTestForErrors.Text = "Test for Errors";
            toolTip1.SetToolTip(buttonTestForErrors, resources.GetString("buttonTestForErrors.ToolTip"));
            buttonTestForErrors.UseVisualStyleBackColor = true;
            buttonTestForErrors.Click += ButtonTestForErrors_Click;
            // 
            // checkBoxWarningsAsErrors
            // 
            checkBoxWarningsAsErrors.Anchor = AnchorStyles.Left;
            checkBoxWarningsAsErrors.AutoSize = true;
            checkBoxWarningsAsErrors.Location = new Point(470, 233);
            checkBoxWarningsAsErrors.Name = "checkBoxWarningsAsErrors";
            checkBoxWarningsAsErrors.Size = new Size(123, 19);
            checkBoxWarningsAsErrors.TabIndex = 7;
            checkBoxWarningsAsErrors.Text = "Warnings as errors";
            toolTip1.SetToolTip(checkBoxWarningsAsErrors, "Treat all warnings as errors when performing FLAC files test.\r\n\r\nWarnings will be shown in the log in addition to errors.");
            checkBoxWarningsAsErrors.UseVisualStyleBackColor = true;
            checkBoxWarningsAsErrors.CheckedChanged += CheckBoxWarningsAsErrors_CheckedChanged;
            // 
            // buttonClearUnchecked
            // 
            buttonClearUnchecked.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonClearUnchecked.Location = new Point(600, 230);
            buttonClearUnchecked.Name = "buttonClearUnchecked";
            buttonClearUnchecked.Size = new Size(110, 23);
            buttonClearUnchecked.TabIndex = 8;
            buttonClearUnchecked.Text = "Clear unchecked";
            toolTip1.SetToolTip(buttonClearUnchecked, "Clears all unchecked files from the list.\r\n\r\nHold 'Shift' while clicking to move unchecked files to the Recycle Bin.");
            buttonClearUnchecked.UseVisualStyleBackColor = true;
            buttonClearUnchecked.Click += ButtonClearUnchecked_Click;
            // 
            // buttonClearAudioFiles
            // 
            buttonClearAudioFiles.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonClearAudioFiles.Location = new Point(716, 230);
            buttonClearAudioFiles.Name = "buttonClearAudioFiles";
            buttonClearAudioFiles.Size = new Size(55, 23);
            buttonClearAudioFiles.TabIndex = 9;
            buttonClearAudioFiles.Text = "Clear";
            buttonClearAudioFiles.UseVisualStyleBackColor = true;
            buttonClearAudioFiles.Click += ButtonClearAudioFiles_Click;
            // 
            // labelAudioFileRemoved
            // 
            labelAudioFileRemoved.AutoSize = true;
            labelAudioFileRemoved.BackColor = SystemColors.ActiveCaption;
            labelAudioFileRemoved.Location = new Point(453, 0);
            labelAudioFileRemoved.Name = "labelAudioFileRemoved";
            labelAudioFileRemoved.Size = new Size(28, 15);
            labelAudioFileRemoved.TabIndex = 10;
            labelAudioFileRemoved.Text = "Text";
            // 
            // groupBoxJobsList
            // 
            groupBoxJobsList.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
            groupBoxJobsList.Controls.Add(dataGridViewJobs);
            groupBoxJobsList.Controls.Add(buttonStartJobList);
            groupBoxJobsList.Controls.Add(buttonUpJob);
            groupBoxJobsList.Controls.Add(buttonDownJob);
            groupBoxJobsList.Controls.Add(buttonRemoveJob);
            groupBoxJobsList.Controls.Add(buttonMinusPass);
            groupBoxJobsList.Controls.Add(labelPasses);
            groupBoxJobsList.Controls.Add(buttonPlusPass);
            groupBoxJobsList.Controls.Add(buttonExportJobList);
            groupBoxJobsList.Controls.Add(buttonImportJobList);
            groupBoxJobsList.Controls.Add(buttonCopyJobs);
            groupBoxJobsList.Controls.Add(buttonPasteJobs);
            groupBoxJobsList.Controls.Add(buttonClearJobList);
            groupBoxJobsList.Location = new Point(12, 455);
            groupBoxJobsList.MinimumSize = new Size(777, 120);
            groupBoxJobsList.Name = "groupBoxJobsList";
            groupBoxJobsList.Size = new Size(777, 394);
            groupBoxJobsList.TabIndex = 6;
            groupBoxJobsList.TabStop = false;
            groupBoxJobsList.Text = "Job List (Drag'n'Drop is available)";
            // 
            // dataGridViewJobs
            // 
            dataGridViewJobs.AllowDrop = true;
            dataGridViewJobs.AllowUserToAddRows = false;
            dataGridViewJobs.AllowUserToResizeRows = false;
            dataGridViewJobs.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dataGridViewJobs.BackgroundColor = SystemColors.Window;
            dataGridViewJobs.BorderStyle = BorderStyle.Fixed3D;
            dataGridViewJobs.ClipboardCopyMode = DataGridViewClipboardCopyMode.Disable;
            dataGridViewJobs.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dataGridViewJobs.Columns.AddRange(new DataGridViewColumn[] { Column1CheckBox, Column2JobType, Column3Passes, Column4Parameters });
            dataGridViewJobs.GridColor = SystemColors.Control;
            dataGridViewJobs.Location = new Point(6, 22);
            dataGridViewJobs.Name = "dataGridViewJobs";
            dataGridViewJobs.RowHeadersVisible = false;
            dataGridViewJobs.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGridViewJobs.Size = new Size(765, 336);
            dataGridViewJobs.TabIndex = 0;
            // 
            // Column1CheckBox
            // 
            Column1CheckBox.FillWeight = 20F;
            Column1CheckBox.Frozen = true;
            Column1CheckBox.HeaderText = "";
            Column1CheckBox.Name = "Column1CheckBox";
            Column1CheckBox.Width = 20;
            // 
            // Column2JobType
            // 
            Column2JobType.FillWeight = 60F;
            Column2JobType.HeaderText = "Job Type";
            Column2JobType.Name = "Column2JobType";
            Column2JobType.ReadOnly = true;
            Column2JobType.Resizable = DataGridViewTriState.True;
            Column2JobType.Width = 60;
            // 
            // Column3Passes
            // 
            Column3Passes.FillWeight = 48F;
            Column3Passes.HeaderText = "Passes";
            Column3Passes.Name = "Column3Passes";
            Column3Passes.Width = 48;
            // 
            // Column4Parameters
            // 
            Column4Parameters.HeaderText = "Parameters";
            Column4Parameters.Name = "Column4Parameters";
            Column4Parameters.Width = 634;
            // 
            // buttonStartJobList
            // 
            buttonStartJobList.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            buttonStartJobList.Location = new Point(6, 364);
            buttonStartJobList.Name = "buttonStartJobList";
            buttonStartJobList.Size = new Size(110, 23);
            buttonStartJobList.TabIndex = 1;
            buttonStartJobList.Text = "Start Job List";
            toolTip1.SetToolTip(buttonStartJobList, "Start all checked jobs to encode and decode all checked audio files using all checked encoders with listed parameters.");
            buttonStartJobList.UseVisualStyleBackColor = true;
            buttonStartJobList.Click += ButtonStartJobList_Click;
            // 
            // buttonUpJob
            // 
            buttonUpJob.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            buttonUpJob.Location = new Point(122, 364);
            buttonUpJob.Name = "buttonUpJob";
            buttonUpJob.Size = new Size(24, 23);
            buttonUpJob.TabIndex = 2;
            buttonUpJob.Text = "▲";
            buttonUpJob.UseVisualStyleBackColor = true;
            buttonUpJob.Click += ButtonUpJob_Click;
            // 
            // buttonDownJob
            // 
            buttonDownJob.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            buttonDownJob.Location = new Point(151, 364);
            buttonDownJob.Name = "buttonDownJob";
            buttonDownJob.Size = new Size(24, 23);
            buttonDownJob.TabIndex = 3;
            buttonDownJob.Text = "▼";
            buttonDownJob.UseVisualStyleBackColor = true;
            buttonDownJob.Click += ButtonDownJob_Click;
            // 
            // buttonRemoveJob
            // 
            buttonRemoveJob.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            buttonRemoveJob.Location = new Point(181, 364);
            buttonRemoveJob.Name = "buttonRemoveJob";
            buttonRemoveJob.Size = new Size(24, 23);
            buttonRemoveJob.TabIndex = 4;
            buttonRemoveJob.Text = "❌";
            buttonRemoveJob.UseVisualStyleBackColor = true;
            buttonRemoveJob.Click += ButtonRemoveJob_Click;
            // 
            // buttonMinusPass
            // 
            buttonMinusPass.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            buttonMinusPass.Location = new Point(231, 364);
            buttonMinusPass.Name = "buttonMinusPass";
            buttonMinusPass.Size = new Size(24, 23);
            buttonMinusPass.TabIndex = 5;
            buttonMinusPass.Text = "➖";
            toolTip1.SetToolTip(buttonMinusPass, "You can add several passes of the same job to obtain averaged results.");
            buttonMinusPass.UseVisualStyleBackColor = true;
            buttonMinusPass.Click += ButtonMinusPass_Click;
            // 
            // labelPasses
            // 
            labelPasses.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            labelPasses.AutoSize = true;
            labelPasses.Location = new Point(261, 368);
            labelPasses.Name = "labelPasses";
            labelPasses.Size = new Size(41, 15);
            labelPasses.TabIndex = 6;
            labelPasses.Text = "Passes";
            toolTip1.SetToolTip(labelPasses, "You can add several passes of the same job to obtain averaged results.");
            // 
            // buttonPlusPass
            // 
            buttonPlusPass.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            buttonPlusPass.Location = new Point(307, 364);
            buttonPlusPass.Name = "buttonPlusPass";
            buttonPlusPass.Size = new Size(24, 23);
            buttonPlusPass.TabIndex = 7;
            buttonPlusPass.Text = "➕";
            toolTip1.SetToolTip(buttonPlusPass, "You can add several passes of the same job to obtain averaged results.");
            buttonPlusPass.UseVisualStyleBackColor = true;
            buttonPlusPass.Click += ButtonPlusPass_Click;
            // 
            // buttonExportJobList
            // 
            buttonExportJobList.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            buttonExportJobList.Location = new Point(472, 364);
            buttonExportJobList.Name = "buttonExportJobList";
            buttonExportJobList.Size = new Size(55, 23);
            buttonExportJobList.TabIndex = 8;
            buttonExportJobList.Text = "Export";
            buttonExportJobList.UseVisualStyleBackColor = true;
            buttonExportJobList.Click += ButtonExportJobList_Click;
            // 
            // buttonImportJobList
            // 
            buttonImportJobList.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            buttonImportJobList.Location = new Point(533, 364);
            buttonImportJobList.Name = "buttonImportJobList";
            buttonImportJobList.Size = new Size(55, 23);
            buttonImportJobList.TabIndex = 9;
            buttonImportJobList.Text = "Import";
            buttonImportJobList.UseVisualStyleBackColor = true;
            buttonImportJobList.Click += ButtonImportJobList_Click;
            // 
            // buttonCopyJobs
            // 
            buttonCopyJobs.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonCopyJobs.Location = new Point(594, 364);
            buttonCopyJobs.Name = "buttonCopyJobs";
            buttonCopyJobs.Size = new Size(55, 23);
            buttonCopyJobs.TabIndex = 10;
            buttonCopyJobs.Text = "Copy";
            toolTip1.SetToolTip(buttonCopyJobs, "You may copy joblist to notepad to edit and to paste it back.");
            buttonCopyJobs.UseVisualStyleBackColor = true;
            buttonCopyJobs.Click += ButtonCopyJobs_Click;
            // 
            // buttonPasteJobs
            // 
            buttonPasteJobs.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonPasteJobs.Location = new Point(655, 364);
            buttonPasteJobs.Name = "buttonPasteJobs";
            buttonPasteJobs.Size = new Size(55, 23);
            buttonPasteJobs.TabIndex = 11;
            buttonPasteJobs.Text = "Paste";
            toolTip1.SetToolTip(buttonPasteJobs, "You may copy joblist to notepad to edit and to paste it back.");
            buttonPasteJobs.UseVisualStyleBackColor = true;
            buttonPasteJobs.Click += ButtonPasteJobs_Click;
            // 
            // buttonClearJobList
            // 
            buttonClearJobList.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonClearJobList.Location = new Point(716, 364);
            buttonClearJobList.Name = "buttonClearJobList";
            buttonClearJobList.Size = new Size(55, 23);
            buttonClearJobList.TabIndex = 12;
            buttonClearJobList.Text = "Clear";
            buttonClearJobList.UseVisualStyleBackColor = true;
            buttonClearJobList.Click += ButtonClearJobList_Click;
            // 
            // groupBoxLog
            // 
            groupBoxLog.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            groupBoxLog.Controls.Add(tabControlLog);
            groupBoxLog.Controls.Add(buttonStop);
            groupBoxLog.Controls.Add(buttonPauseResume);
            groupBoxLog.Controls.Add(labelStopped);
            groupBoxLog.Controls.Add(buttonAnalyzeLog);
            groupBoxLog.Controls.Add(buttonLogToExcel);
            groupBoxLog.Controls.Add(buttonCopyLogAsBBCode);
            groupBoxLog.Controls.Add(buttonOpenLogtxt);
            groupBoxLog.Controls.Add(buttonCopyLog);
            groupBoxLog.Controls.Add(buttonClearLog);
            groupBoxLog.Controls.Add(buttonLogColumnsAutoWidth);
            groupBoxLog.Location = new Point(795, 455);
            groupBoxLog.MinimumSize = new Size(777, 120);
            groupBoxLog.Name = "groupBoxLog";
            groupBoxLog.Size = new Size(777, 394);
            groupBoxLog.TabIndex = 7;
            groupBoxLog.TabStop = false;
            groupBoxLog.Text = "Log";
            // 
            // tabControlLog
            // 
            tabControlLog.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            tabControlLog.Controls.Add(Benchmark);
            tabControlLog.Controls.Add(ScalingPlots);
            tabControlLog.Controls.Add(DetectDupes);
            tabControlLog.Controls.Add(TestForErrors);
            tabControlLog.Location = new Point(6, 19);
            tabControlLog.Name = "tabControlLog";
            tabControlLog.SelectedIndex = 0;
            tabControlLog.Size = new Size(767, 340);
            tabControlLog.TabIndex = 0;
            // 
            // Benchmark
            // 
            Benchmark.Controls.Add(dataGridViewLog);
            Benchmark.Location = new Point(4, 24);
            Benchmark.Name = "Benchmark";
            Benchmark.Padding = new Padding(3);
            Benchmark.Size = new Size(759, 312);
            Benchmark.TabIndex = 0;
            Benchmark.Tag = "";
            Benchmark.Text = "Benchmark";
            Benchmark.UseVisualStyleBackColor = true;
            // 
            // dataGridViewLog
            // 
            dataGridViewLog.AllowUserToAddRows = false;
            dataGridViewLog.AllowUserToOrderColumns = true;
            dataGridViewLog.AllowUserToResizeRows = false;
            dataGridViewLog.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dataGridViewLog.BackgroundColor = SystemColors.Control;
            dataGridViewLog.BorderStyle = BorderStyle.Fixed3D;
            dataGridViewCellStyle1.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle1.BackColor = SystemColors.Control;
            dataGridViewCellStyle1.Font = new Font("Segoe UI", 9F);
            dataGridViewCellStyle1.ForeColor = SystemColors.WindowText;
            dataGridViewCellStyle1.SelectionBackColor = SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = DataGridViewTriState.True;
            dataGridViewLog.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            dataGridViewLog.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dataGridViewCellStyle2.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = SystemColors.Control;
            dataGridViewCellStyle2.Font = new Font("Segoe UI", 9F);
            dataGridViewCellStyle2.ForeColor = SystemColors.ControlText;
            dataGridViewCellStyle2.SelectionBackColor = SystemColors.Highlight;
            dataGridViewCellStyle2.SelectionForeColor = SystemColors.HighlightText;
            dataGridViewCellStyle2.WrapMode = DataGridViewTriState.False;
            dataGridViewLog.DefaultCellStyle = dataGridViewCellStyle2;
            dataGridViewLog.GridColor = SystemColors.Control;
            dataGridViewLog.Location = new Point(2, 4);
            dataGridViewLog.Name = "dataGridViewLog";
            dataGridViewLog.ReadOnly = true;
            dataGridViewLog.RowHeadersVisible = false;
            dataGridViewLog.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGridViewLog.Size = new Size(753, 305);
            dataGridViewLog.TabIndex = 0;
            // 
            // ScalingPlots
            // 
            ScalingPlots.Controls.Add(tabControlScalingPlots);
            ScalingPlots.Location = new Point(4, 24);
            ScalingPlots.Name = "ScalingPlots";
            ScalingPlots.Size = new Size(759, 312);
            ScalingPlots.TabIndex = 3;
            ScalingPlots.Tag = "";
            ScalingPlots.Text = "Scaling Plots";
            ScalingPlots.UseVisualStyleBackColor = true;
            // 
            // tabControlScalingPlots
            // 
            tabControlScalingPlots.Controls.Add(tabPageSpeedByThreads);
            tabControlScalingPlots.Controls.Add(tabPageCPULoadByThreads);
            tabControlScalingPlots.Controls.Add(tabPageCPUClockByThreads);
            tabControlScalingPlots.Controls.Add(tabPageMultiplotByThreads);
            tabControlScalingPlots.Controls.Add(tabPageSpeedByParameters);
            tabControlScalingPlots.Controls.Add(tabPageCompressionByParameters);
            tabControlScalingPlots.Controls.Add(tabPageMultiplotByParameters);
            tabControlScalingPlots.Dock = DockStyle.Fill;
            tabControlScalingPlots.Location = new Point(0, 0);
            tabControlScalingPlots.Name = "tabControlScalingPlots";
            tabControlScalingPlots.SelectedIndex = 0;
            tabControlScalingPlots.Size = new Size(759, 312);
            tabControlScalingPlots.TabIndex = 0;
            // 
            // tabPageSpeedByThreads
            // 
            tabPageSpeedByThreads.Controls.Add(plotScalingPlotSpeedByThreads);
            tabPageSpeedByThreads.Location = new Point(4, 24);
            tabPageSpeedByThreads.Name = "tabPageSpeedByThreads";
            tabPageSpeedByThreads.Size = new Size(751, 284);
            tabPageSpeedByThreads.TabIndex = 0;
            tabPageSpeedByThreads.Text = "Speed by Threads";
            toolTip1.SetToolTip(tabPageSpeedByThreads, "Scale Speed by Threads");
            tabPageSpeedByThreads.UseVisualStyleBackColor = true;
            // 
            // plotScalingPlotSpeedByThreads
            // 
            plotScalingPlotSpeedByThreads.Dock = DockStyle.Fill;
            plotScalingPlotSpeedByThreads.Location = new Point(0, 0);
            plotScalingPlotSpeedByThreads.Margin = new Padding(4, 3, 4, 3);
            plotScalingPlotSpeedByThreads.Name = "plotScalingPlotSpeedByThreads";
            plotScalingPlotSpeedByThreads.Size = new Size(751, 284);
            plotScalingPlotSpeedByThreads.TabIndex = 0;
            // 
            // tabPageCPULoadByThreads
            // 
            tabPageCPULoadByThreads.Controls.Add(plotScalingPlotCPULoadByThreads);
            tabPageCPULoadByThreads.Location = new Point(4, 24);
            tabPageCPULoadByThreads.Name = "tabPageCPULoadByThreads";
            tabPageCPULoadByThreads.Size = new Size(751, 284);
            tabPageCPULoadByThreads.TabIndex = 1;
            tabPageCPULoadByThreads.Text = "CPU Load by Threads";
            toolTip1.SetToolTip(tabPageCPULoadByThreads, "Scale CPU Load by Threads");
            tabPageCPULoadByThreads.UseVisualStyleBackColor = true;
            // 
            // plotScalingPlotCPULoadByThreads
            // 
            plotScalingPlotCPULoadByThreads.Dock = DockStyle.Fill;
            plotScalingPlotCPULoadByThreads.Location = new Point(0, 0);
            plotScalingPlotCPULoadByThreads.Margin = new Padding(4, 3, 4, 3);
            plotScalingPlotCPULoadByThreads.Name = "plotScalingPlotCPULoadByThreads";
            plotScalingPlotCPULoadByThreads.Size = new Size(751, 284);
            plotScalingPlotCPULoadByThreads.TabIndex = 0;
            // 
            // tabPageCPUClockByThreads
            // 
            tabPageCPUClockByThreads.Controls.Add(plotScalingPlotCPUClockByThreads);
            tabPageCPUClockByThreads.Location = new Point(4, 24);
            tabPageCPUClockByThreads.Name = "tabPageCPUClockByThreads";
            tabPageCPUClockByThreads.Size = new Size(751, 284);
            tabPageCPUClockByThreads.TabIndex = 2;
            tabPageCPUClockByThreads.Text = "CPU Clock by Threads";
            toolTip1.SetToolTip(tabPageCPUClockByThreads, "Scale CPU Clock by Threads");
            tabPageCPUClockByThreads.UseVisualStyleBackColor = true;
            // 
            // plotScalingPlotCPUClockByThreads
            // 
            plotScalingPlotCPUClockByThreads.Dock = DockStyle.Fill;
            plotScalingPlotCPUClockByThreads.Location = new Point(0, 0);
            plotScalingPlotCPUClockByThreads.Margin = new Padding(4, 3, 4, 3);
            plotScalingPlotCPUClockByThreads.Name = "plotScalingPlotCPUClockByThreads";
            plotScalingPlotCPUClockByThreads.Size = new Size(751, 284);
            plotScalingPlotCPUClockByThreads.TabIndex = 0;
            // 
            // tabPageMultiplotByThreads
            // 
            tabPageMultiplotByThreads.Controls.Add(tableLayoutPanelMultiPlotByThreads);
            tabPageMultiplotByThreads.Location = new Point(4, 24);
            tabPageMultiplotByThreads.Name = "tabPageMultiplotByThreads";
            tabPageMultiplotByThreads.Size = new Size(751, 284);
            tabPageMultiplotByThreads.TabIndex = 7;
            tabPageMultiplotByThreads.Text = "Multiplot by Threads";
            tabPageMultiplotByThreads.UseVisualStyleBackColor = true;
            // 
            // tableLayoutPanelMultiPlotByThreads
            // 
            tableLayoutPanelMultiPlotByThreads.ColumnCount = 1;
            tableLayoutPanelMultiPlotByThreads.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanelMultiPlotByThreads.Controls.Add(plotScalingMultiPlotSpeedByThreads, 0, 0);
            tableLayoutPanelMultiPlotByThreads.Controls.Add(plotScalingMultiPlotCPULoadByThreads, 0, 1);
            tableLayoutPanelMultiPlotByThreads.Controls.Add(plotScalingMultiPlotCPUClockByThreads, 0, 2);
            tableLayoutPanelMultiPlotByThreads.Dock = DockStyle.Fill;
            tableLayoutPanelMultiPlotByThreads.Location = new Point(0, 0);
            tableLayoutPanelMultiPlotByThreads.Name = "tableLayoutPanelMultiPlotByThreads";
            tableLayoutPanelMultiPlotByThreads.RowCount = 3;
            tableLayoutPanelMultiPlotByThreads.RowStyles.Add(new RowStyle(SizeType.Percent, 33.3333321F));
            tableLayoutPanelMultiPlotByThreads.RowStyles.Add(new RowStyle(SizeType.Percent, 33.3333359F));
            tableLayoutPanelMultiPlotByThreads.RowStyles.Add(new RowStyle(SizeType.Percent, 33.3333359F));
            tableLayoutPanelMultiPlotByThreads.Size = new Size(751, 284);
            tableLayoutPanelMultiPlotByThreads.TabIndex = 0;
            // 
            // plotScalingMultiPlotSpeedByThreads
            // 
            plotScalingMultiPlotSpeedByThreads.Dock = DockStyle.Fill;
            plotScalingMultiPlotSpeedByThreads.Location = new Point(4, 3);
            plotScalingMultiPlotSpeedByThreads.Margin = new Padding(4, 3, 4, 3);
            plotScalingMultiPlotSpeedByThreads.Name = "plotScalingMultiPlotSpeedByThreads";
            plotScalingMultiPlotSpeedByThreads.Size = new Size(743, 88);
            plotScalingMultiPlotSpeedByThreads.TabIndex = 0;
            // 
            // plotScalingMultiPlotCPULoadByThreads
            // 
            plotScalingMultiPlotCPULoadByThreads.Dock = DockStyle.Fill;
            plotScalingMultiPlotCPULoadByThreads.Location = new Point(4, 97);
            plotScalingMultiPlotCPULoadByThreads.Margin = new Padding(4, 3, 4, 3);
            plotScalingMultiPlotCPULoadByThreads.Name = "plotScalingMultiPlotCPULoadByThreads";
            plotScalingMultiPlotCPULoadByThreads.Size = new Size(743, 88);
            plotScalingMultiPlotCPULoadByThreads.TabIndex = 1;
            // 
            // plotScalingMultiPlotCPUClockByThreads
            // 
            plotScalingMultiPlotCPUClockByThreads.Dock = DockStyle.Fill;
            plotScalingMultiPlotCPUClockByThreads.Location = new Point(4, 191);
            plotScalingMultiPlotCPUClockByThreads.Margin = new Padding(4, 3, 4, 3);
            plotScalingMultiPlotCPUClockByThreads.Name = "plotScalingMultiPlotCPUClockByThreads";
            plotScalingMultiPlotCPUClockByThreads.Size = new Size(743, 90);
            plotScalingMultiPlotCPUClockByThreads.TabIndex = 2;
            // 
            // tabPageSpeedByParameters
            // 
            tabPageSpeedByParameters.Controls.Add(plotScalingPlotSpeedByParameters);
            tabPageSpeedByParameters.Location = new Point(4, 24);
            tabPageSpeedByParameters.Name = "tabPageSpeedByParameters";
            tabPageSpeedByParameters.Size = new Size(751, 284);
            tabPageSpeedByParameters.TabIndex = 3;
            tabPageSpeedByParameters.Text = "Speed by Parameters";
            toolTip1.SetToolTip(tabPageSpeedByParameters, "Scale Speed by Parameters");
            tabPageSpeedByParameters.UseVisualStyleBackColor = true;
            // 
            // plotScalingPlotSpeedByParameters
            // 
            plotScalingPlotSpeedByParameters.Dock = DockStyle.Fill;
            plotScalingPlotSpeedByParameters.Location = new Point(0, 0);
            plotScalingPlotSpeedByParameters.Margin = new Padding(4, 3, 4, 3);
            plotScalingPlotSpeedByParameters.Name = "plotScalingPlotSpeedByParameters";
            plotScalingPlotSpeedByParameters.Size = new Size(751, 284);
            plotScalingPlotSpeedByParameters.TabIndex = 0;
            // 
            // tabPageCompressionByParameters
            // 
            tabPageCompressionByParameters.Controls.Add(plotScalingPlotCompressionByParameters);
            tabPageCompressionByParameters.Location = new Point(4, 24);
            tabPageCompressionByParameters.Name = "tabPageCompressionByParameters";
            tabPageCompressionByParameters.Size = new Size(751, 284);
            tabPageCompressionByParameters.TabIndex = 4;
            tabPageCompressionByParameters.Text = "Compression by Parameters";
            toolTip1.SetToolTip(tabPageCompressionByParameters, "Scale Compression by Parameters");
            tabPageCompressionByParameters.UseVisualStyleBackColor = true;
            // 
            // plotScalingPlotCompressionByParameters
            // 
            plotScalingPlotCompressionByParameters.Dock = DockStyle.Fill;
            plotScalingPlotCompressionByParameters.Location = new Point(0, 0);
            plotScalingPlotCompressionByParameters.Margin = new Padding(4, 3, 4, 3);
            plotScalingPlotCompressionByParameters.Name = "plotScalingPlotCompressionByParameters";
            plotScalingPlotCompressionByParameters.Size = new Size(751, 284);
            plotScalingPlotCompressionByParameters.TabIndex = 0;
            // 
            // tabPageMultiplotByParameters
            // 
            tabPageMultiplotByParameters.Controls.Add(tableLayoutPanelMultiPlotByParameters);
            tabPageMultiplotByParameters.Location = new Point(4, 24);
            tabPageMultiplotByParameters.Name = "tabPageMultiplotByParameters";
            tabPageMultiplotByParameters.Size = new Size(751, 284);
            tabPageMultiplotByParameters.TabIndex = 8;
            tabPageMultiplotByParameters.Text = "Multiplot by Parameters";
            tabPageMultiplotByParameters.UseVisualStyleBackColor = true;
            // 
            // tableLayoutPanelMultiPlotByParameters
            // 
            tableLayoutPanelMultiPlotByParameters.ColumnCount = 1;
            tableLayoutPanelMultiPlotByParameters.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanelMultiPlotByParameters.Controls.Add(plotScalingMultiPlotSpeedByParameters, 0, 0);
            tableLayoutPanelMultiPlotByParameters.Controls.Add(plotScalingMultiPlotCompressionByParameters, 0, 1);
            tableLayoutPanelMultiPlotByParameters.Dock = DockStyle.Fill;
            tableLayoutPanelMultiPlotByParameters.Location = new Point(0, 0);
            tableLayoutPanelMultiPlotByParameters.Name = "tableLayoutPanelMultiPlotByParameters";
            tableLayoutPanelMultiPlotByParameters.RowCount = 2;
            tableLayoutPanelMultiPlotByParameters.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tableLayoutPanelMultiPlotByParameters.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tableLayoutPanelMultiPlotByParameters.Size = new Size(751, 284);
            tableLayoutPanelMultiPlotByParameters.TabIndex = 0;
            // 
            // plotScalingMultiPlotSpeedByParameters
            // 
            plotScalingMultiPlotSpeedByParameters.Dock = DockStyle.Fill;
            plotScalingMultiPlotSpeedByParameters.Location = new Point(4, 3);
            plotScalingMultiPlotSpeedByParameters.Margin = new Padding(4, 3, 4, 3);
            plotScalingMultiPlotSpeedByParameters.Name = "plotScalingMultiPlotSpeedByParameters";
            plotScalingMultiPlotSpeedByParameters.Size = new Size(743, 136);
            plotScalingMultiPlotSpeedByParameters.TabIndex = 3;
            // 
            // plotScalingMultiPlotCompressionByParameters
            // 
            plotScalingMultiPlotCompressionByParameters.Dock = DockStyle.Fill;
            plotScalingMultiPlotCompressionByParameters.Location = new Point(4, 145);
            plotScalingMultiPlotCompressionByParameters.Margin = new Padding(4, 3, 4, 3);
            plotScalingMultiPlotCompressionByParameters.Name = "plotScalingMultiPlotCompressionByParameters";
            plotScalingMultiPlotCompressionByParameters.Size = new Size(743, 136);
            plotScalingMultiPlotCompressionByParameters.TabIndex = 4;
            // 
            // DetectDupes
            // 
            DetectDupes.Controls.Add(dataGridViewLogDetectDupes);
            DetectDupes.Location = new Point(4, 24);
            DetectDupes.Name = "DetectDupes";
            DetectDupes.Padding = new Padding(3);
            DetectDupes.Size = new Size(759, 312);
            DetectDupes.TabIndex = 1;
            DetectDupes.Tag = "";
            DetectDupes.Text = "Detect Dupes";
            DetectDupes.UseVisualStyleBackColor = true;
            // 
            // dataGridViewLogDetectDupes
            // 
            dataGridViewLogDetectDupes.AllowUserToAddRows = false;
            dataGridViewLogDetectDupes.AllowUserToOrderColumns = true;
            dataGridViewLogDetectDupes.AllowUserToResizeRows = false;
            dataGridViewLogDetectDupes.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dataGridViewLogDetectDupes.BackgroundColor = SystemColors.Control;
            dataGridViewLogDetectDupes.BorderStyle = BorderStyle.Fixed3D;
            dataGridViewCellStyle3.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle3.BackColor = SystemColors.Control;
            dataGridViewCellStyle3.Font = new Font("Segoe UI", 9F);
            dataGridViewCellStyle3.ForeColor = SystemColors.WindowText;
            dataGridViewCellStyle3.SelectionBackColor = SystemColors.Highlight;
            dataGridViewCellStyle3.SelectionForeColor = SystemColors.HighlightText;
            dataGridViewCellStyle3.WrapMode = DataGridViewTriState.True;
            dataGridViewLogDetectDupes.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle3;
            dataGridViewLogDetectDupes.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dataGridViewCellStyle4.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle4.BackColor = SystemColors.Control;
            dataGridViewCellStyle4.Font = new Font("Segoe UI", 9F);
            dataGridViewCellStyle4.ForeColor = SystemColors.ControlText;
            dataGridViewCellStyle4.SelectionBackColor = SystemColors.Highlight;
            dataGridViewCellStyle4.SelectionForeColor = SystemColors.HighlightText;
            dataGridViewCellStyle4.WrapMode = DataGridViewTriState.False;
            dataGridViewLogDetectDupes.DefaultCellStyle = dataGridViewCellStyle4;
            dataGridViewLogDetectDupes.GridColor = SystemColors.Control;
            dataGridViewLogDetectDupes.Location = new Point(2, 4);
            dataGridViewLogDetectDupes.Name = "dataGridViewLogDetectDupes";
            dataGridViewLogDetectDupes.ReadOnly = true;
            dataGridViewLogDetectDupes.RowHeadersVisible = false;
            dataGridViewLogDetectDupes.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGridViewLogDetectDupes.Size = new Size(753, 305);
            dataGridViewLogDetectDupes.TabIndex = 0;
            // 
            // TestForErrors
            // 
            TestForErrors.Controls.Add(dataGridViewLogTestForErrors);
            TestForErrors.Location = new Point(4, 24);
            TestForErrors.Name = "TestForErrors";
            TestForErrors.Size = new Size(759, 312);
            TestForErrors.TabIndex = 2;
            TestForErrors.Tag = "";
            TestForErrors.Text = "Test for Errors";
            TestForErrors.UseVisualStyleBackColor = true;
            // 
            // dataGridViewLogTestForErrors
            // 
            dataGridViewLogTestForErrors.AllowUserToAddRows = false;
            dataGridViewLogTestForErrors.AllowUserToOrderColumns = true;
            dataGridViewLogTestForErrors.AllowUserToResizeRows = false;
            dataGridViewLogTestForErrors.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dataGridViewLogTestForErrors.BackgroundColor = SystemColors.Control;
            dataGridViewLogTestForErrors.BorderStyle = BorderStyle.Fixed3D;
            dataGridViewCellStyle5.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle5.BackColor = SystemColors.Control;
            dataGridViewCellStyle5.Font = new Font("Segoe UI", 9F);
            dataGridViewCellStyle5.ForeColor = SystemColors.WindowText;
            dataGridViewCellStyle5.SelectionBackColor = SystemColors.Highlight;
            dataGridViewCellStyle5.SelectionForeColor = SystemColors.HighlightText;
            dataGridViewCellStyle5.WrapMode = DataGridViewTriState.True;
            dataGridViewLogTestForErrors.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle5;
            dataGridViewLogTestForErrors.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dataGridViewCellStyle6.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle6.BackColor = SystemColors.Control;
            dataGridViewCellStyle6.Font = new Font("Segoe UI", 9F);
            dataGridViewCellStyle6.ForeColor = SystemColors.ControlText;
            dataGridViewCellStyle6.SelectionBackColor = SystemColors.Highlight;
            dataGridViewCellStyle6.SelectionForeColor = SystemColors.HighlightText;
            dataGridViewCellStyle6.WrapMode = DataGridViewTriState.False;
            dataGridViewLogTestForErrors.DefaultCellStyle = dataGridViewCellStyle6;
            dataGridViewLogTestForErrors.GridColor = SystemColors.Control;
            dataGridViewLogTestForErrors.Location = new Point(2, 4);
            dataGridViewLogTestForErrors.Name = "dataGridViewLogTestForErrors";
            dataGridViewLogTestForErrors.ReadOnly = true;
            dataGridViewLogTestForErrors.RowHeadersVisible = false;
            dataGridViewLogTestForErrors.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGridViewLogTestForErrors.Size = new Size(753, 305);
            dataGridViewLogTestForErrors.TabIndex = 0;
            // 
            // buttonPauseResume
            // 
            buttonPauseResume.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            buttonPauseResume.Enabled = false;
            buttonPauseResume.Location = new Point(122, 364);
            buttonPauseResume.Name = "buttonPauseResume";
            buttonPauseResume.Size = new Size(85, 23);
            buttonPauseResume.TabIndex = 2;
            buttonPauseResume.Text = "Pause";
            toolTip1.SetToolTip(buttonPauseResume, "Pause after processing current file");
            buttonPauseResume.UseVisualStyleBackColor = true;
            // 
            // labelStopped
            // 
            labelStopped.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            labelStopped.AutoSize = true;
            labelStopped.ForeColor = Color.Red;
            labelStopped.Location = new Point(212, 368);
            labelStopped.Name = "labelStopped";
            labelStopped.Size = new Size(51, 15);
            labelStopped.TabIndex = 26;
            labelStopped.Text = "Stopped";
            // 
            // buttonAnalyzeLog
            // 
            buttonAnalyzeLog.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonAnalyzeLog.Location = new Point(266, 364);
            buttonAnalyzeLog.Name = "buttonAnalyzeLog";
            buttonAnalyzeLog.Size = new Size(85, 23);
            buttonAnalyzeLog.TabIndex = 3;
            buttonAnalyzeLog.Text = "Analyze log";
            toolTip1.SetToolTip(buttonAnalyzeLog, resources.GetString("buttonAnalyzeLog.ToolTip"));
            buttonAnalyzeLog.UseVisualStyleBackColor = true;
            buttonAnalyzeLog.Click += ButtonAnalyzeLog_Click;
            // 
            // buttonLogToExcel
            // 
            buttonLogToExcel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonLogToExcel.Location = new Point(357, 364);
            buttonLogToExcel.Name = "buttonLogToExcel";
            buttonLogToExcel.Size = new Size(85, 23);
            buttonLogToExcel.TabIndex = 4;
            buttonLogToExcel.Text = "Log to Excel";
            buttonLogToExcel.UseVisualStyleBackColor = true;
            buttonLogToExcel.Click += ButtonLogToExcel_Click;
            // 
            // buttonCopyLogAsBBCode
            // 
            buttonCopyLogAsBBCode.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonCopyLogAsBBCode.Location = new Point(448, 364);
            buttonCopyLogAsBBCode.Name = "buttonCopyLogAsBBCode";
            buttonCopyLogAsBBCode.Size = new Size(110, 23);
            buttonCopyLogAsBBCode.TabIndex = 5;
            buttonCopyLogAsBBCode.Text = "Copy as BBCode";
            toolTip1.SetToolTip(buttonCopyLogAsBBCode, "Copy current log as a BBCode to paste it in forums as a table.");
            buttonCopyLogAsBBCode.UseVisualStyleBackColor = true;
            buttonCopyLogAsBBCode.Click += ButtonCopyLogAsBBCode_Click;
            // 
            // buttonCopyLog
            // 
            buttonCopyLog.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonCopyLog.Location = new Point(655, 364);
            buttonCopyLog.Name = "buttonCopyLog";
            buttonCopyLog.Size = new Size(55, 23);
            buttonCopyLog.TabIndex = 7;
            buttonCopyLog.Text = "Copy";
            toolTip1.SetToolTip(buttonCopyLog, "Copy current log.");
            buttonCopyLog.UseVisualStyleBackColor = true;
            buttonCopyLog.Click += ButtonCopyLog_Click;
            // 
            // buttonLogColumnsAutoWidth
            // 
            buttonLogColumnsAutoWidth.Location = new Point(39, 0);
            buttonLogColumnsAutoWidth.Name = "buttonLogColumnsAutoWidth";
            buttonLogColumnsAutoWidth.Size = new Size(42, 18);
            buttonLogColumnsAutoWidth.TabIndex = 9;
            buttonLogColumnsAutoWidth.Text = "⟷";
            toolTip1.SetToolTip(buttonLogColumnsAutoWidth, "Set columns auto-width.\r\nHold 'Shift' to apply to all tabs.");
            buttonLogColumnsAutoWidth.UseVisualStyleBackColor = true;
            buttonLogColumnsAutoWidth.Click += ButtonLogColumnsAutoWidth_Click;
            // 
            // buttonDataGridViewLogSettings
            // 
            buttonDataGridViewLogSettings.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            buttonDataGridViewLogSettings.Location = new Point(3, 5);
            buttonDataGridViewLogSettings.Name = "buttonDataGridViewLogSettings";
            buttonDataGridViewLogSettings.Size = new Size(164, 23);
            buttonDataGridViewLogSettings.TabIndex = 0;
            buttonDataGridViewLogSettings.Text = "Log Columns Settings";
            buttonDataGridViewLogSettings.UseVisualStyleBackColor = true;
            buttonDataGridViewLogSettings.Click += ButtonDataGridViewLogSettings_Click;
            // 
            // groupBoxSettings
            // 
            groupBoxSettings.Controls.Add(tabControlSettings);
            groupBoxSettings.Location = new Point(1218, 278);
            groupBoxSettings.MinimumSize = new Size(354, 171);
            groupBoxSettings.Name = "groupBoxSettings";
            groupBoxSettings.Size = new Size(354, 171);
            groupBoxSettings.TabIndex = 5;
            groupBoxSettings.TabStop = false;
            groupBoxSettings.Text = "Settings";
            // 
            // tabControlSettings
            // 
            tabControlSettings.Controls.Add(tabPageQuickSettings);
            tabControlSettings.Controls.Add(tabPageLogsSettings);
            tabControlSettings.Controls.Add(tabPagePlotsSettings);
            tabControlSettings.Controls.Add(tabPageMiscSettings);
            tabControlSettings.Dock = DockStyle.Fill;
            tabControlSettings.Location = new Point(3, 19);
            tabControlSettings.Name = "tabControlSettings";
            tabControlSettings.SelectedIndex = 0;
            tabControlSettings.Size = new Size(348, 149);
            tabControlSettings.TabIndex = 0;
            // 
            // tabPageQuickSettings
            // 
            tabPageQuickSettings.Controls.Add(checkBoxRemoveMetadata);
            tabPageQuickSettings.Controls.Add(checkBoxWarmupPass);
            tabPageQuickSettings.Controls.Add(checkBoxPreventSleep);
            tabPageQuickSettings.Controls.Add(checkBoxAutoAnalyzeLog);
            tabPageQuickSettings.Controls.Add(labelCPUPriority);
            tabPageQuickSettings.Controls.Add(comboBoxCPUPriority);
            tabPageQuickSettings.Location = new Point(4, 24);
            tabPageQuickSettings.Name = "tabPageQuickSettings";
            tabPageQuickSettings.Size = new Size(340, 121);
            tabPageQuickSettings.TabIndex = 0;
            tabPageQuickSettings.Text = "Quick";
            tabPageQuickSettings.UseVisualStyleBackColor = true;
            // 
            // checkBoxRemoveMetadata
            // 
            checkBoxRemoveMetadata.AutoSize = true;
            checkBoxRemoveMetadata.Location = new Point(4, 8);
            checkBoxRemoveMetadata.Name = "checkBoxRemoveMetadata";
            checkBoxRemoveMetadata.Size = new Size(122, 19);
            checkBoxRemoveMetadata.TabIndex = 0;
            checkBoxRemoveMetadata.Text = "Remove metadata";
            toolTip1.SetToolTip(checkBoxRemoveMetadata, resources.GetString("checkBoxRemoveMetadata.ToolTip"));
            checkBoxRemoveMetadata.UseVisualStyleBackColor = true;
            // 
            // checkBoxPreventSleep
            // 
            checkBoxPreventSleep.AutoSize = true;
            checkBoxPreventSleep.Location = new Point(4, 56);
            checkBoxPreventSleep.Name = "checkBoxPreventSleep";
            checkBoxPreventSleep.Size = new Size(131, 19);
            checkBoxPreventSleep.TabIndex = 2;
            checkBoxPreventSleep.Text = "Prevent Sleep mode";
            toolTip1.SetToolTip(checkBoxPreventSleep, "When enabled, prevents the computer from entering sleep or hibernation mode\r\n\r\nNote: This does not prevent the display from turning off.");
            checkBoxPreventSleep.UseVisualStyleBackColor = true;
            checkBoxPreventSleep.CheckedChanged += CheckBoxPreventSleep_CheckedChanged;
            // 
            // checkBoxAutoAnalyzeLog
            // 
            checkBoxAutoAnalyzeLog.AutoSize = true;
            checkBoxAutoAnalyzeLog.Location = new Point(4, 80);
            checkBoxAutoAnalyzeLog.Name = "checkBoxAutoAnalyzeLog";
            checkBoxAutoAnalyzeLog.Size = new Size(165, 19);
            checkBoxAutoAnalyzeLog.TabIndex = 3;
            checkBoxAutoAnalyzeLog.Text = "Analyze Log on Jobs finish";
            toolTip1.SetToolTip(checkBoxAutoAnalyzeLog, "Starts log analysis automatically after all encoding/decoding jobs are finished.");
            checkBoxAutoAnalyzeLog.UseVisualStyleBackColor = true;
            checkBoxAutoAnalyzeLog.CheckedChanged += CheckBoxAutoAnalyzeLog_CheckedChanged;
            // 
            // labelCPUPriority
            // 
            labelCPUPriority.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            labelCPUPriority.AutoSize = true;
            labelCPUPriority.Location = new Point(184, 9);
            labelCPUPriority.Name = "labelCPUPriority";
            labelCPUPriority.Size = new Size(48, 15);
            labelCPUPriority.TabIndex = 4;
            labelCPUPriority.Text = "Priority:";
            toolTip1.SetToolTip(labelCPUPriority, "Encoding/Decoding process priority");
            // 
            // comboBoxCPUPriority
            // 
            comboBoxCPUPriority.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            comboBoxCPUPriority.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBoxCPUPriority.FormattingEnabled = true;
            comboBoxCPUPriority.Items.AddRange(new object[] { "RealTime", "High", "AboveNormal", "Normal", "BelowNormal", "Idle" });
            comboBoxCPUPriority.Location = new Point(238, 5);
            comboBoxCPUPriority.MaxDropDownItems = 5;
            comboBoxCPUPriority.Name = "comboBoxCPUPriority";
            comboBoxCPUPriority.Size = new Size(97, 23);
            comboBoxCPUPriority.TabIndex = 5;
            toolTip1.SetToolTip(comboBoxCPUPriority, "Encoding/Decoding process priority");
            // 
            // tabPageLogsSettings
            // 
            tabPageLogsSettings.Controls.Add(buttonDataGridViewLogSettings);
            tabPageLogsSettings.Controls.Add(checkBoxClearLogsOnExit);
            tabPageLogsSettings.Controls.Add(checkBoxClearLogsOnExitIncludeDetectDupes);
            tabPageLogsSettings.Controls.Add(checkBoxClearLogsOnExitIncludeTestForErrors);
            tabPageLogsSettings.Controls.Add(checkBoxClearLogsOnExitIncludeLogTXT);
            tabPageLogsSettings.Location = new Point(4, 24);
            tabPageLogsSettings.Name = "tabPageLogsSettings";
            tabPageLogsSettings.Size = new Size(340, 121);
            tabPageLogsSettings.TabIndex = 3;
            tabPageLogsSettings.Text = "Logs";
            tabPageLogsSettings.UseVisualStyleBackColor = true;
            // 
            // checkBoxClearLogsOnExit
            // 
            checkBoxClearLogsOnExit.AutoSize = true;
            checkBoxClearLogsOnExit.Location = new Point(185, 8);
            checkBoxClearLogsOnExit.Name = "checkBoxClearLogsOnExit";
            checkBoxClearLogsOnExit.Size = new Size(120, 19);
            checkBoxClearLogsOnExit.TabIndex = 1;
            checkBoxClearLogsOnExit.Text = "Clear Logs on exit";
            checkBoxClearLogsOnExit.UseVisualStyleBackColor = true;
            checkBoxClearLogsOnExit.Visible = false;
            // 
            // checkBoxClearLogsOnExitIncludeDetectDupes
            // 
            checkBoxClearLogsOnExitIncludeDetectDupes.AutoSize = true;
            checkBoxClearLogsOnExitIncludeDetectDupes.Enabled = false;
            checkBoxClearLogsOnExitIncludeDetectDupes.Location = new Point(197, 31);
            checkBoxClearLogsOnExitIncludeDetectDupes.Name = "checkBoxClearLogsOnExitIncludeDetectDupes";
            checkBoxClearLogsOnExitIncludeDetectDupes.Size = new Size(96, 19);
            checkBoxClearLogsOnExitIncludeDetectDupes.TabIndex = 2;
            checkBoxClearLogsOnExitIncludeDetectDupes.Text = "Detect Dupes";
            checkBoxClearLogsOnExitIncludeDetectDupes.UseVisualStyleBackColor = true;
            checkBoxClearLogsOnExitIncludeDetectDupes.Visible = false;
            // 
            // checkBoxClearLogsOnExitIncludeTestForErrors
            // 
            checkBoxClearLogsOnExitIncludeTestForErrors.AutoSize = true;
            checkBoxClearLogsOnExitIncludeTestForErrors.Enabled = false;
            checkBoxClearLogsOnExitIncludeTestForErrors.Location = new Point(197, 56);
            checkBoxClearLogsOnExitIncludeTestForErrors.Name = "checkBoxClearLogsOnExitIncludeTestForErrors";
            checkBoxClearLogsOnExitIncludeTestForErrors.Size = new Size(97, 19);
            checkBoxClearLogsOnExitIncludeTestForErrors.TabIndex = 3;
            checkBoxClearLogsOnExitIncludeTestForErrors.Text = "Test for Errors";
            checkBoxClearLogsOnExitIncludeTestForErrors.UseVisualStyleBackColor = true;
            checkBoxClearLogsOnExitIncludeTestForErrors.Visible = false;
            // 
            // checkBoxClearLogsOnExitIncludeLogTXT
            // 
            checkBoxClearLogsOnExitIncludeLogTXT.AutoSize = true;
            checkBoxClearLogsOnExitIncludeLogTXT.Enabled = false;
            checkBoxClearLogsOnExitIncludeLogTXT.Location = new Point(197, 81);
            checkBoxClearLogsOnExitIncludeLogTXT.Name = "checkBoxClearLogsOnExitIncludeLogTXT";
            checkBoxClearLogsOnExitIncludeLogTXT.Size = new Size(63, 19);
            checkBoxClearLogsOnExitIncludeLogTXT.TabIndex = 4;
            checkBoxClearLogsOnExitIncludeLogTXT.Text = "Log.txt";
            checkBoxClearLogsOnExitIncludeLogTXT.UseVisualStyleBackColor = true;
            checkBoxClearLogsOnExitIncludeLogTXT.Visible = false;
            // 
            // tabPagePlotsSettings
            // 
            tabPagePlotsSettings.Controls.Add(checkBoxDrawMultiplots);
            tabPagePlotsSettings.Controls.Add(checkBoxShowIndividualFilesPlots);
            tabPagePlotsSettings.Controls.Add(checkBoxShowAggregatedByEncoderPlots);
            tabPagePlotsSettings.Controls.Add(checkBoxShowIdealCPULoadLine);
            tabPagePlotsSettings.Controls.Add(checkBoxShowTooltipsOnPlots);
            tabPagePlotsSettings.Location = new Point(4, 24);
            tabPagePlotsSettings.Name = "tabPagePlotsSettings";
            tabPagePlotsSettings.Size = new Size(340, 121);
            tabPagePlotsSettings.TabIndex = 2;
            tabPagePlotsSettings.Text = "Plots";
            tabPagePlotsSettings.UseVisualStyleBackColor = true;
            // 
            // checkBoxDrawMultiplots
            // 
            checkBoxDrawMultiplots.AutoSize = true;
            checkBoxDrawMultiplots.Location = new Point(4, 8);
            checkBoxDrawMultiplots.Name = "checkBoxDrawMultiplots";
            checkBoxDrawMultiplots.Size = new Size(110, 19);
            checkBoxDrawMultiplots.TabIndex = 0;
            checkBoxDrawMultiplots.Text = "Draw Multiplots";
            toolTip1.SetToolTip(checkBoxDrawMultiplots, "Toggle between individual plots and consolidated multiplot view with shared axes.");
            checkBoxDrawMultiplots.UseVisualStyleBackColor = true;
            checkBoxDrawMultiplots.CheckedChanged += CheckBoxDrawMultiplots_CheckedChanged;
            // 
            // checkBoxShowIndividualFilesPlots
            // 
            checkBoxShowIndividualFilesPlots.AutoSize = true;
            checkBoxShowIndividualFilesPlots.Checked = true;
            checkBoxShowIndividualFilesPlots.CheckState = CheckState.Checked;
            checkBoxShowIndividualFilesPlots.Location = new Point(4, 32);
            checkBoxShowIndividualFilesPlots.Name = "checkBoxShowIndividualFilesPlots";
            checkBoxShowIndividualFilesPlots.Size = new Size(102, 19);
            checkBoxShowIndividualFilesPlots.TabIndex = 1;
            checkBoxShowIndividualFilesPlots.Text = "Individual files";
            toolTip1.SetToolTip(checkBoxShowIndividualFilesPlots, "Show/hide individual file series on scaling graphs");
            checkBoxShowIndividualFilesPlots.UseVisualStyleBackColor = true;
            checkBoxShowIndividualFilesPlots.CheckedChanged += CheckBoxShowIndividualFilesPlots_CheckedChanged;
            // 
            // checkBoxShowAggregatedByEncoderPlots
            // 
            checkBoxShowAggregatedByEncoderPlots.AutoSize = true;
            checkBoxShowAggregatedByEncoderPlots.Checked = true;
            checkBoxShowAggregatedByEncoderPlots.CheckState = CheckState.Checked;
            checkBoxShowAggregatedByEncoderPlots.Location = new Point(4, 56);
            checkBoxShowAggregatedByEncoderPlots.Name = "checkBoxShowAggregatedByEncoderPlots";
            checkBoxShowAggregatedByEncoderPlots.Size = new Size(150, 19);
            checkBoxShowAggregatedByEncoderPlots.TabIndex = 2;
            checkBoxShowAggregatedByEncoderPlots.Text = "Aggregated by Encoder";
            toolTip1.SetToolTip(checkBoxShowAggregatedByEncoderPlots, "Show/hide aggregated series (bold lines) representing average performance across all files for each encoder/parameter combination.");
            checkBoxShowAggregatedByEncoderPlots.UseVisualStyleBackColor = true;
            checkBoxShowAggregatedByEncoderPlots.CheckedChanged += CheckBoxShowAggregatedByEncoderPlots_CheckedChanged;
            // 
            // checkBoxShowIdealCPULoadLine
            // 
            checkBoxShowIdealCPULoadLine.AutoSize = true;
            checkBoxShowIdealCPULoadLine.Location = new Point(4, 80);
            checkBoxShowIdealCPULoadLine.Name = "checkBoxShowIdealCPULoadLine";
            checkBoxShowIdealCPULoadLine.Size = new Size(131, 19);
            checkBoxShowIdealCPULoadLine.TabIndex = 3;
            checkBoxShowIdealCPULoadLine.Text = "Ideal CPU Load line ";
            toolTip1.SetToolTip(checkBoxShowIdealCPULoadLine, "Show/hide ideal CPU load reference line (100% per thread, linear scaling).");
            checkBoxShowIdealCPULoadLine.UseVisualStyleBackColor = true;
            checkBoxShowIdealCPULoadLine.CheckedChanged += CheckBoxShowIdealCPULoadLine_CheckedChanged;
            // 
            // checkBoxShowTooltipsOnPlots
            // 
            checkBoxShowTooltipsOnPlots.AutoSize = true;
            checkBoxShowTooltipsOnPlots.Checked = true;
            checkBoxShowTooltipsOnPlots.CheckState = CheckState.Checked;
            checkBoxShowTooltipsOnPlots.Location = new Point(185, 8);
            checkBoxShowTooltipsOnPlots.Name = "checkBoxShowTooltipsOnPlots";
            checkBoxShowTooltipsOnPlots.Size = new Size(67, 19);
            checkBoxShowTooltipsOnPlots.TabIndex = 4;
            checkBoxShowTooltipsOnPlots.Text = "Tooltips";
            toolTip1.SetToolTip(checkBoxShowTooltipsOnPlots, "Enable/disable interactive tooltips showing series name and exact X/Y values when hovering over data points.");
            checkBoxShowTooltipsOnPlots.UseVisualStyleBackColor = true;
            checkBoxShowTooltipsOnPlots.CheckedChanged += CheckBoxShowTooltipsOnPlots_CheckedChanged;
            // 
            // tabPageMiscSettings
            // 
            tabPageMiscSettings.Controls.Add(buttonSelectTempFolder);
            tabPageMiscSettings.Controls.Add(checkBoxClearTempFolder);
            tabPageMiscSettings.Controls.Add(checkBoxAddMD5OnLoadWav);
            tabPageMiscSettings.Controls.Add(checkBoxCheckForUpdatesOnStartup);
            tabPageMiscSettings.Location = new Point(4, 24);
            tabPageMiscSettings.Name = "tabPageMiscSettings";
            tabPageMiscSettings.Size = new Size(340, 121);
            tabPageMiscSettings.TabIndex = 1;
            tabPageMiscSettings.Text = "Misc";
            tabPageMiscSettings.UseVisualStyleBackColor = true;
            // 
            // buttonSelectTempFolder
            // 
            buttonSelectTempFolder.Location = new Point(3, 5);
            buttonSelectTempFolder.Name = "buttonSelectTempFolder";
            buttonSelectTempFolder.Size = new Size(164, 23);
            buttonSelectTempFolder.TabIndex = 0;
            buttonSelectTempFolder.Text = "Select Temp folder";
            buttonSelectTempFolder.UseVisualStyleBackColor = true;
            buttonSelectTempFolder.Click += ButtonSelectTempFolder_Click;
            // 
            // checkBoxClearTempFolder
            // 
            checkBoxClearTempFolder.AutoSize = true;
            checkBoxClearTempFolder.Location = new Point(4, 32);
            checkBoxClearTempFolder.Name = "checkBoxClearTempFolder";
            checkBoxClearTempFolder.Size = new Size(157, 19);
            checkBoxClearTempFolder.TabIndex = 1;
            checkBoxClearTempFolder.Text = "Clear temp folder on exit";
            checkBoxClearTempFolder.UseVisualStyleBackColor = true;
            // 
            // checkBoxAddMD5OnLoadWav
            // 
            checkBoxAddMD5OnLoadWav.AutoSize = true;
            checkBoxAddMD5OnLoadWav.Location = new Point(185, 8);
            checkBoxAddMD5OnLoadWav.Name = "checkBoxAddMD5OnLoadWav";
            checkBoxAddMD5OnLoadWav.Size = new Size(150, 19);
            checkBoxAddMD5OnLoadWav.TabIndex = 2;
            checkBoxAddMD5OnLoadWav.Text = "Add MD5 on .WAV load";
            toolTip1.SetToolTip(checkBoxAddMD5OnLoadWav, "Calculate MD5 when loading .wav files into the list.\r\nThis may significantly slow down the loading process.\r\nBut \"Detect dupes\" will work faster.\r\n\r\nDoes not affect encoding or decoding speed.");
            checkBoxAddMD5OnLoadWav.UseVisualStyleBackColor = true;
            // 
            // checkBoxCheckForUpdatesOnStartup
            // 
            checkBoxCheckForUpdatesOnStartup.AutoSize = true;
            checkBoxCheckForUpdatesOnStartup.Location = new Point(185, 32);
            checkBoxCheckForUpdatesOnStartup.Name = "checkBoxCheckForUpdatesOnStartup";
            checkBoxCheckForUpdatesOnStartup.Size = new Size(122, 19);
            checkBoxCheckForUpdatesOnStartup.TabIndex = 3;
            checkBoxCheckForUpdatesOnStartup.Text = "Check for updates";
            toolTip1.SetToolTip(checkBoxCheckForUpdatesOnStartup, "Check for updates on startup.\r\n\r\nCheck for updates immediately when re-enabled.");
            checkBoxCheckForUpdatesOnStartup.UseVisualStyleBackColor = true;
            checkBoxCheckForUpdatesOnStartup.CheckedChanged += CheckBoxCheckForUpdatesOnStartup_CheckedChanged;
            // 
            // groupBoxDecoderSettings
            // 
            groupBoxDecoderSettings.Controls.Add(labelCommandLineDecoder);
            groupBoxDecoderSettings.Controls.Add(textBoxCommandLineOptionsDecoder);
            groupBoxDecoderSettings.Controls.Add(buttonClearCommandLineDecoder);
            groupBoxDecoderSettings.Controls.Add(buttonStartDecode);
            groupBoxDecoderSettings.Controls.Add(buttonAddJobToJobListDecoder);
            groupBoxDecoderSettings.Controls.Add(progressBarDecoder);
            groupBoxDecoderSettings.Location = new Point(795, 278);
            groupBoxDecoderSettings.MinimumSize = new Size(417, 171);
            groupBoxDecoderSettings.Name = "groupBoxDecoderSettings";
            groupBoxDecoderSettings.Size = new Size(417, 171);
            groupBoxDecoderSettings.TabIndex = 4;
            groupBoxDecoderSettings.TabStop = false;
            groupBoxDecoderSettings.Text = "Decoder Settings";
            // 
            // labelCommandLineDecoder
            // 
            labelCommandLineDecoder.AutoSize = true;
            labelCommandLineDecoder.Location = new Point(27, 24);
            labelCommandLineDecoder.Name = "labelCommandLineDecoder";
            labelCommandLineDecoder.Size = new Size(89, 15);
            labelCommandLineDecoder.TabIndex = 0;
            labelCommandLineDecoder.Text = "Command line:";
            // 
            // textBoxCommandLineOptionsDecoder
            // 
            textBoxCommandLineOptionsDecoder.Location = new Point(122, 21);
            textBoxCommandLineOptionsDecoder.Name = "textBoxCommandLineOptionsDecoder";
            textBoxCommandLineOptionsDecoder.Size = new Size(228, 23);
            textBoxCommandLineOptionsDecoder.TabIndex = 1;
            // 
            // buttonClearCommandLineDecoder
            // 
            buttonClearCommandLineDecoder.Location = new Point(356, 21);
            buttonClearCommandLineDecoder.Name = "buttonClearCommandLineDecoder";
            buttonClearCommandLineDecoder.Size = new Size(55, 23);
            buttonClearCommandLineDecoder.TabIndex = 2;
            buttonClearCommandLineDecoder.Text = "Clear";
            buttonClearCommandLineDecoder.UseVisualStyleBackColor = true;
            buttonClearCommandLineDecoder.Click += ButtonClearCommandLineDecoder_Click;
            // 
            // buttonAddJobToJobListDecoder
            // 
            buttonAddJobToJobListDecoder.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            buttonAddJobToJobListDecoder.Location = new Point(122, 141);
            buttonAddJobToJobListDecoder.Name = "buttonAddJobToJobListDecoder";
            buttonAddJobToJobListDecoder.Size = new Size(110, 23);
            buttonAddJobToJobListDecoder.TabIndex = 4;
            buttonAddJobToJobListDecoder.Text = "Add to Job List";
            toolTip1.SetToolTip(buttonAddJobToJobListDecoder, "This will add decoding parameters to a job list.\r\n\r\nLater you may use them to benchmark all checked encoders with all checked audiofiles.\r\n");
            buttonAddJobToJobListDecoder.UseVisualStyleBackColor = true;
            buttonAddJobToJobListDecoder.Click += ButtonAddJobToJobListDecoder_Click;
            // 
            // progressBarDecoder
            // 
            progressBarDecoder.DisplayType = ProgressBarEx.TextDisplayType.Manual;
            progressBarDecoder.Location = new Point(238, 141);
            progressBarDecoder.Name = "progressBarDecoder";
            progressBarDecoder.Size = new Size(112, 23);
            progressBarDecoder.TabIndex = 5;
            // 
            // toolTip1
            // 
            toolTip1.AutoPopDelay = 20000;
            toolTip1.InitialDelay = 500;
            toolTip1.ReshowDelay = 100;
            // 
            // groupBoxInformation
            // 
            groupBoxInformation.Controls.Add(labelCpuInfo);
            groupBoxInformation.Controls.Add(labelCpuUsageTitle);
            groupBoxInformation.Controls.Add(labelCpuUsageValue);
            groupBoxInformation.Controls.Add(buttonAbout);
            groupBoxInformation.Location = new Point(648, 278);
            groupBoxInformation.MinimumSize = new Size(141, 171);
            groupBoxInformation.Name = "groupBoxInformation";
            groupBoxInformation.Size = new Size(141, 171);
            groupBoxInformation.TabIndex = 3;
            groupBoxInformation.TabStop = false;
            groupBoxInformation.Text = "Information";
            // 
            // labelCpuUsageValue
            // 
            labelCpuUsageValue.Location = new Point(69, 49);
            labelCpuUsageValue.Name = "labelCpuUsageValue";
            labelCpuUsageValue.Size = new Size(66, 30);
            labelCpuUsageValue.TabIndex = 2;
            labelCpuUsageValue.TextAlign = ContentAlignment.TopRight;
            // 
            // buttonAbout
            // 
            buttonAbout.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            buttonAbout.Location = new Point(6, 141);
            buttonAbout.Name = "buttonAbout";
            buttonAbout.Size = new Size(129, 23);
            buttonAbout.TabIndex = 3;
            buttonAbout.Text = "About";
            buttonAbout.UseVisualStyleBackColor = true;
            buttonAbout.Click += ButtonAbout_Click;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            AutoScroll = true;
            AutoScrollMinSize = new Size(0, 600);
            CancelButton = buttonStop;
            ClientSize = new Size(1584, 861);
            Controls.Add(groupBoxEncoders);
            Controls.Add(groupBoxAudioFiles);
            Controls.Add(groupBoxEncoderSettings);
            Controls.Add(groupBoxInformation);
            Controls.Add(groupBoxDecoderSettings);
            Controls.Add(groupBoxSettings);
            Controls.Add(groupBoxJobsList);
            Controls.Add(groupBoxLog);
            DoubleBuffered = true;
            Icon = (Icon)resources.GetObject("$this.Icon");
            KeyPreview = true;
            Name = "Form1";
            Text = "FLAC Benchmark-H";
            Load += Form1_Load;
            groupBoxEncoderSettings.ResumeLayout(false);
            groupBoxEncoderSettings.PerformLayout();
            groupBoxEncoders.ResumeLayout(false);
            groupBoxAudioFiles.ResumeLayout(false);
            groupBoxAudioFiles.PerformLayout();
            groupBoxJobsList.ResumeLayout(false);
            groupBoxJobsList.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridViewJobs).EndInit();
            groupBoxLog.ResumeLayout(false);
            groupBoxLog.PerformLayout();
            tabControlLog.ResumeLayout(false);
            Benchmark.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dataGridViewLog).EndInit();
            ScalingPlots.ResumeLayout(false);
            tabControlScalingPlots.ResumeLayout(false);
            tabPageSpeedByThreads.ResumeLayout(false);
            tabPageCPULoadByThreads.ResumeLayout(false);
            tabPageCPUClockByThreads.ResumeLayout(false);
            tabPageMultiplotByThreads.ResumeLayout(false);
            tableLayoutPanelMultiPlotByThreads.ResumeLayout(false);
            tabPageSpeedByParameters.ResumeLayout(false);
            tabPageCompressionByParameters.ResumeLayout(false);
            tabPageMultiplotByParameters.ResumeLayout(false);
            tableLayoutPanelMultiPlotByParameters.ResumeLayout(false);
            DetectDupes.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dataGridViewLogDetectDupes).EndInit();
            TestForErrors.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dataGridViewLogTestForErrors).EndInit();
            groupBoxSettings.ResumeLayout(false);
            tabControlSettings.ResumeLayout(false);
            tabPageQuickSettings.ResumeLayout(false);
            tabPageQuickSettings.PerformLayout();
            tabPageLogsSettings.ResumeLayout(false);
            tabPageLogsSettings.PerformLayout();
            tabPagePlotsSettings.ResumeLayout(false);
            tabPagePlotsSettings.PerformLayout();
            tabPageMiscSettings.ResumeLayout(false);
            tabPageMiscSettings.PerformLayout();
            groupBoxDecoderSettings.ResumeLayout(false);
            groupBoxDecoderSettings.PerformLayout();
            groupBoxInformation.ResumeLayout(false);
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
        private Button buttonStartEncode;
        private Button buttonEpr8;
        private Button buttonAsubdividetukey5flattop;
        private Button buttonNoPadding;
        private Button buttonNoSeektable;
        private Button buttonClearCommandLineEncoder;
        private Button buttonClearLog;
        private GroupBox groupBoxEncoders;
        private Button buttonOpenLogtxt;
        private Label labelCpuInfo;
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
        private Button buttonExportJobList;
        private Button buttonImportJobList;
        private Button buttonClearJobList;
        private Button buttonAddEncoders;
        private Button buttonClearEncoders;
        private Button buttonAddAudioFiles;
        private Button buttonClearAudioFiles;
        private Button buttonRemoveEncoder;
        private Button buttonRemoveAudioFile;
        private GroupBox groupBoxSettings;
        private Button buttonStartJobList;
        private Button buttonAddJobToJobListEncoder;
        private Button buttonCopyLog;
        private Label labelCommandLineEncoder;
        private Button buttonStop;
        private GroupBox groupBoxDecoderSettings;
        private Label labelCommandLineDecoder;
        private Button buttonAddJobToJobListDecoder;
        private TextBox textBoxCommandLineOptionsDecoder;
        private Button buttonClearCommandLineDecoder;
        private Label labelSetCompression;
        private CheckBox checkBoxClearTempFolder;
        private ColumnHeader FileName;
        private ColumnHeader Duration;
        private ColumnHeader BitDepth;
        private ColumnHeader SamplingRate;
        private ColumnHeader InputAudioFileSize;
        private Button buttonSelectTempFolder;
        private ColumnHeader FileNameExe;
        private ColumnHeader SizeExe;
        private ColumnHeader DateExe;
        private ColumnHeader VersionExe;
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
        private Button buttonPauseResume;
        private Label labelCpuUsageTitle;
        private CheckBox checkBoxAddMD5OnLoadWav;
        private CheckBox checkBoxRemoveMetadata;
        private GroupBox groupBoxInformation;
        private Button buttonAbout;
        private CheckBox checkBoxCheckForUpdatesOnStartup;
        private Button buttonLogColumnsAutoWidth;
        private CheckBox checkBoxAutoAnalyzeLog;
        private ColumnHeader EncoderDirectory;
        private Label labelAudioFileRemoved;
        private Button buttonCopyLogAsBBCode;
        private CheckBox checkBoxWarningsAsErrors;
        private CheckBox checkBoxWarmupPass;
        private Label labelCpuUsageValue;
        private ProgressBarEx progressBarEncoder;
        private ProgressBarEx progressBarDecoder;
        private DataGridViewEx dataGridViewLog;
        private CheckBox checkBoxPreventSleep;
        private Button buttonScriptConstructor;
        private TabControl tabControlLog;
        private TabPage Benchmark;
        private TabPage DetectDupes;
        private TabPage TestForErrors;
        private DataGridViewEx dataGridViewLogDetectDupes;
        private DataGridViewEx dataGridViewLogTestForErrors;
        private Button buttonDataGridViewLogSettings;
        private ColumnHeader Channels;
        private DataGridViewEx dataGridViewJobs;
        private DataGridViewCheckBoxColumn Column1CheckBox;
        private DataGridViewTextBoxColumn Column2JobType;
        private DataGridViewTextBoxColumn Column3Passes;
        private DataGridViewTextBoxColumn Column4Parameters;
        private TabPage ScalingPlots;
        private ScottPlot.FormsPlot plotScalingPlotSpeedByThreads;
        private TabControl tabControlScalingPlots;
        private TabPage tabPageSpeedByThreads;
        private TabPage tabPageCPULoadByThreads;
        private ScottPlot.FormsPlot plotScalingPlotCPULoadByThreads;
        private TabPage tabPageCPUClockByThreads;
        private ScottPlot.FormsPlot plotScalingPlotCPUClockByThreads;
        private TabPage tabPageSpeedByParameters;
        private TabPage tabPageCompressionByParameters;
        private ScottPlot.FormsPlot plotScalingPlotSpeedByParameters;
        private ScottPlot.FormsPlot plotScalingPlotCompressionByParameters;
        private TabControl tabControlSettings;
        private TabPage tabPageQuickSettings;
        private TabPage tabPageMiscSettings;
        private TabPage tabPagePlotsSettings;
        private CheckBox checkBoxShowIndividualFilesPlots;
        private CheckBox checkBoxClearLogsOnExit;
        private TabPage tabPageLogsSettings;
        private CheckBox checkBoxClearLogsOnExitIncludeLogTXT;
        private CheckBox checkBoxClearLogsOnExitIncludeTestForErrors;
        private CheckBox checkBoxClearLogsOnExitIncludeDetectDupes;
        private CheckBox checkBoxShowAggregatedByEncoderPlots;
        private CheckBox checkBoxDrawMultiplots;
        private TabPage tabPageMultiplotByThreads;
        private TableLayoutPanel tableLayoutPanelMultiPlotByThreads;
        private ScottPlot.FormsPlot plotScalingMultiPlotCPULoadByThreads;
        private ScottPlot.FormsPlot plotScalingMultiPlotSpeedByThreads;
        private ScottPlot.FormsPlot plotScalingMultiPlotCPUClockByThreads;
        private TabPage tabPageMultiplotByParameters;
        private TableLayoutPanel tableLayoutPanelMultiPlotByParameters;
        private ScottPlot.FormsPlot plotScalingMultiPlotCompressionByParameters;
        private ScottPlot.FormsPlot plotScalingMultiPlotSpeedByParameters;
        private CheckBox checkBoxShowIdealCPULoadLine;
        private CheckBox checkBoxShowTooltipsOnPlots;
    }
}
