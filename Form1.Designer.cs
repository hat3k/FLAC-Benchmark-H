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
            labelFlacUsedVersion = new Label();
            buttonStartEncode = new Button();
            progressBar = new ProgressBar();
            buttonStartDecode = new Button();
            checkBoxHighPriority = new CheckBox();
            labelSetThreads = new Label();
            labelSetCores = new Label();
            labelAdditionalArguments = new Label();
            textBoxAdditionalArguments = new TextBox();
            labelSetCompression = new Label();
            buttonepr8 = new Button();
            buttonSetHalfThreads = new Button();
            buttonAsubdividetukey5flattop = new Button();
            buttonSetMaxThreads = new Button();
            buttonNoPadding = new Button();
            buttonHalfCores = new Button();
            buttonNoSeektable = new Button();
            buttonSetMaxCores = new Button();
            buttonClear = new Button();
            buttonMaxCompressionLevel = new Button();
            button5CompressionLevel = new Button();
            labelCPUinfo = new Label();
            labelThreads = new Label();
            textBoxCompressionLevel = new TextBox();
            labelCompressionLevel = new Label();
            textBoxThreads = new TextBox();
            buttonOpenLogtxt = new Button();
            buttonClearLog = new Button();
            textBoxFlacExecutables = new TextBox();
            listBoxFlacExecutables = new ListBox();
            groupBoxEncoders = new GroupBox();
            button1 = new Button();
            buttonAddEncoders = new Button();
            buttonClearEncoders = new Button();
            groupBoxAudioFiles = new GroupBox();
            listBoxAudioFiles = new ListBox();
            button2 = new Button();
            buttonAddAudioFiles = new Button();
            buttonClearAudioFiles = new Button();
            groupBoxJobsQueue = new GroupBox();
            textBoxJobsQueue = new TextBox();
            buttonStartJobList = new Button();
            buttonExportJobList = new Button();
            buttonImportJobList = new Button();
            buttonClearJobList = new Button();
            groupLog = new GroupBox();
            button3 = new Button();
            buttonCopyLog = new Button();
            groupBoxJobSettings = new GroupBox();
            radioButtonDecode = new RadioButton();
            radioButtonEncode = new RadioButton();
            buttonAddJobToQueue = new Button();
            groupBoxEncoderSettings.SuspendLayout();
            groupBoxEncoders.SuspendLayout();
            groupBoxAudioFiles.SuspendLayout();
            groupBoxJobsQueue.SuspendLayout();
            groupLog.SuspendLayout();
            groupBoxJobSettings.SuspendLayout();
            SuspendLayout();
            // 
            // groupBoxEncoderSettings
            // 
            groupBoxEncoderSettings.Controls.Add(labelFlacUsedVersion);
            groupBoxEncoderSettings.Controls.Add(buttonStartEncode);
            groupBoxEncoderSettings.Controls.Add(progressBar);
            groupBoxEncoderSettings.Controls.Add(buttonStartDecode);
            groupBoxEncoderSettings.Controls.Add(checkBoxHighPriority);
            groupBoxEncoderSettings.Controls.Add(labelSetThreads);
            groupBoxEncoderSettings.Controls.Add(labelSetCores);
            groupBoxEncoderSettings.Controls.Add(labelAdditionalArguments);
            groupBoxEncoderSettings.Controls.Add(textBoxAdditionalArguments);
            groupBoxEncoderSettings.Controls.Add(labelSetCompression);
            groupBoxEncoderSettings.Controls.Add(buttonepr8);
            groupBoxEncoderSettings.Controls.Add(buttonSetHalfThreads);
            groupBoxEncoderSettings.Controls.Add(buttonAsubdividetukey5flattop);
            groupBoxEncoderSettings.Controls.Add(buttonSetMaxThreads);
            groupBoxEncoderSettings.Controls.Add(buttonNoPadding);
            groupBoxEncoderSettings.Controls.Add(buttonHalfCores);
            groupBoxEncoderSettings.Controls.Add(buttonNoSeektable);
            groupBoxEncoderSettings.Controls.Add(buttonSetMaxCores);
            groupBoxEncoderSettings.Controls.Add(buttonClear);
            groupBoxEncoderSettings.Controls.Add(buttonMaxCompressionLevel);
            groupBoxEncoderSettings.Controls.Add(button5CompressionLevel);
            groupBoxEncoderSettings.Controls.Add(labelCPUinfo);
            groupBoxEncoderSettings.Controls.Add(labelThreads);
            groupBoxEncoderSettings.Controls.Add(textBoxCompressionLevel);
            groupBoxEncoderSettings.Controls.Add(labelCompressionLevel);
            groupBoxEncoderSettings.Controls.Add(textBoxThreads);
            groupBoxEncoderSettings.Location = new Point(622, 12);
            groupBoxEncoderSettings.Name = "groupBoxEncoderSettings";
            groupBoxEncoderSettings.Size = new Size(858, 260);
            groupBoxEncoderSettings.TabIndex = 0;
            groupBoxEncoderSettings.TabStop = false;
            groupBoxEncoderSettings.Text = "Encoder Settings";
            // 
            // labelFlacUsedVersion
            // 
            labelFlacUsedVersion.AutoSize = true;
            labelFlacUsedVersion.Location = new Point(228, 234);
            labelFlacUsedVersion.Name = "labelFlacUsedVersion";
            labelFlacUsedVersion.Size = new Size(81, 15);
            labelFlacUsedVersion.TabIndex = 15;
            labelFlacUsedVersion.Text = "Using version:";
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
            progressBar.Size = new Size(340, 23);
            progressBar.TabIndex = 4;
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
            checkBoxHighPriority.Location = new Point(564, 233);
            checkBoxHighPriority.Name = "checkBoxHighPriority";
            checkBoxHighPriority.Size = new Size(155, 19);
            checkBoxHighPriority.TabIndex = 22;
            checkBoxHighPriority.Text = "Set High Process Priority";
            checkBoxHighPriority.UseVisualStyleBackColor = true;
            checkBoxHighPriority.CheckedChanged += checkBoxHighPriority_CheckedChanged;
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
            labelSetCores.Location = new Point(228, 56);
            labelSetCores.Name = "labelSetCores";
            labelSetCores.Size = new Size(59, 15);
            labelSetCores.TabIndex = 20;
            labelSetCores.Text = "Set Cores:";
            // 
            // labelAdditionalArguments
            // 
            labelAdditionalArguments.AutoSize = true;
            labelAdditionalArguments.Location = new Point(4, 85);
            labelAdditionalArguments.Name = "labelAdditionalArguments";
            labelAdditionalArguments.Size = new Size(127, 15);
            labelAdditionalArguments.TabIndex = 1;
            labelAdditionalArguments.Text = "Additional Arguments:";
            // 
            // textBoxAdditionalArguments
            // 
            textBoxAdditionalArguments.Location = new Point(139, 82);
            textBoxAdditionalArguments.Name = "textBoxAdditionalArguments";
            textBoxAdditionalArguments.Size = new Size(632, 23);
            textBoxAdditionalArguments.TabIndex = 4;
            // 
            // labelSetCompression
            // 
            labelSetCompression.AutoSize = true;
            labelSetCompression.Location = new Point(188, 26);
            labelSetCompression.Name = "labelSetCompression";
            labelSetCompression.Size = new Size(99, 15);
            labelSetCompression.TabIndex = 19;
            labelSetCompression.Text = "Set Compression:";
            // 
            // buttonepr8
            // 
            buttonepr8.Location = new Point(138, 111);
            buttonepr8.Name = "buttonepr8";
            buttonepr8.Size = new Size(48, 23);
            buttonepr8.TabIndex = 5;
            buttonepr8.Text = "-epr8";
            buttonepr8.UseVisualStyleBackColor = true;
            buttonepr8.Click += buttonepr8_Click;
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
            // buttonAsubdividetukey5flattop
            // 
            buttonAsubdividetukey5flattop.Location = new Point(192, 111);
            buttonAsubdividetukey5flattop.Name = "buttonAsubdividetukey5flattop";
            buttonAsubdividetukey5flattop.Size = new Size(182, 23);
            buttonAsubdividetukey5flattop.TabIndex = 6;
            buttonAsubdividetukey5flattop.Text = "-A \"subdivide_tukey(5);flattop\"";
            buttonAsubdividetukey5flattop.UseVisualStyleBackColor = true;
            buttonAsubdividetukey5flattop.Click += buttonAsubdividetukey5flattop_Click;
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
            // buttonNoPadding
            // 
            buttonNoPadding.Location = new Point(380, 111);
            buttonNoPadding.Name = "buttonNoPadding";
            buttonNoPadding.Size = new Size(83, 23);
            buttonNoPadding.TabIndex = 9;
            buttonNoPadding.Text = "No Padding";
            buttonNoPadding.UseVisualStyleBackColor = true;
            buttonNoPadding.Click += buttonNoPadding_Click;
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
            // buttonNoSeektable
            // 
            buttonNoSeektable.Location = new Point(469, 111);
            buttonNoSeektable.Name = "buttonNoSeektable";
            buttonNoSeektable.Size = new Size(89, 23);
            buttonNoSeektable.TabIndex = 10;
            buttonNoSeektable.Text = "No Seektable";
            buttonNoSeektable.UseVisualStyleBackColor = true;
            buttonNoSeektable.Click += buttonNoSeektable_Click;
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
            // buttonClear
            // 
            buttonClear.Location = new Point(777, 82);
            buttonClear.Name = "buttonClear";
            buttonClear.Size = new Size(75, 23);
            buttonClear.TabIndex = 11;
            buttonClear.Text = "Clear";
            buttonClear.UseVisualStyleBackColor = true;
            buttonClear.Click += buttonClear_Click;
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
            labelCPUinfo.Location = new Point(138, 0);
            labelCPUinfo.Name = "labelCPUinfo";
            labelCPUinfo.Size = new Size(298, 15);
            labelCPUinfo.TabIndex = 17;
            labelCPUinfo.Text = "Your system has: Physical cores: XX, Logical threads: XX";
            labelCPUinfo.Click += labelCPUinfo_Click;
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
            // textBoxCompressionLevel
            // 
            textBoxCompressionLevel.Location = new Point(139, 22);
            textBoxCompressionLevel.Name = "textBoxCompressionLevel";
            textBoxCompressionLevel.Size = new Size(28, 23);
            textBoxCompressionLevel.TabIndex = 2;
            textBoxCompressionLevel.Text = "8";
            textBoxCompressionLevel.TextAlign = HorizontalAlignment.Center;
            // 
            // labelCompressionLevel
            // 
            labelCompressionLevel.AutoSize = true;
            labelCompressionLevel.Location = new Point(21, 26);
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
            // buttonOpenLogtxt
            // 
            buttonOpenLogtxt.Location = new Point(721, 392);
            buttonOpenLogtxt.Name = "buttonOpenLogtxt";
            buttonOpenLogtxt.Size = new Size(85, 23);
            buttonOpenLogtxt.TabIndex = 16;
            buttonOpenLogtxt.Text = "Open log.txt";
            buttonOpenLogtxt.UseVisualStyleBackColor = true;
            buttonOpenLogtxt.Click += buttonOpenLogtxt_Click;
            // 
            // buttonClearLog
            // 
            buttonClearLog.Location = new Point(903, 392);
            buttonClearLog.Name = "buttonClearLog";
            buttonClearLog.Size = new Size(75, 23);
            buttonClearLog.TabIndex = 12;
            buttonClearLog.Text = "Clear Log";
            buttonClearLog.UseVisualStyleBackColor = true;
            buttonClearLog.Click += buttonClearLog_Click;
            // 
            // textBoxFlacExecutables
            // 
            textBoxFlacExecutables.Location = new Point(6, 22);
            textBoxFlacExecutables.Multiline = true;
            textBoxFlacExecutables.Name = "textBoxFlacExecutables";
            textBoxFlacExecutables.PlaceholderText = "Log (there is also additional log file in the app folder)";
            textBoxFlacExecutables.ReadOnly = true;
            textBoxFlacExecutables.ScrollBars = ScrollBars.Both;
            textBoxFlacExecutables.Size = new Size(972, 364);
            textBoxFlacExecutables.TabIndex = 1;
            textBoxFlacExecutables.WordWrap = false;
            // 
            // listBoxFlacExecutables
            // 
            listBoxFlacExecutables.FormattingEnabled = true;
            listBoxFlacExecutables.HorizontalScrollbar = true;
            listBoxFlacExecutables.Location = new Point(6, 25);
            listBoxFlacExecutables.Name = "listBoxFlacExecutables";
            listBoxFlacExecutables.Size = new Size(287, 199);
            listBoxFlacExecutables.TabIndex = 2;
            listBoxFlacExecutables.SelectedIndexChanged += listBox1_SelectedIndexChanged;
            // 
            // groupBoxEncoders
            // 
            groupBoxEncoders.Controls.Add(listBoxFlacExecutables);
            groupBoxEncoders.Controls.Add(button1);
            groupBoxEncoders.Controls.Add(buttonAddEncoders);
            groupBoxEncoders.Controls.Add(buttonClearEncoders);
            groupBoxEncoders.Location = new Point(12, 12);
            groupBoxEncoders.Name = "groupBoxEncoders";
            groupBoxEncoders.Size = new Size(299, 260);
            groupBoxEncoders.TabIndex = 3;
            groupBoxEncoders.TabStop = false;
            groupBoxEncoders.Text = "Choose Encoder (Drag'n'Drop is available)";
            // 
            // button1
            // 
            button1.Location = new Point(112, 230);
            button1.Name = "button1";
            button1.Size = new Size(100, 23);
            button1.TabIndex = 11;
            button1.Text = "Remove file";
            button1.UseVisualStyleBackColor = true;
            button1.Click += buttonClear_Click;
            // 
            // buttonAddEncoders
            // 
            buttonAddEncoders.Location = new Point(6, 230);
            buttonAddEncoders.Name = "buttonAddEncoders";
            buttonAddEncoders.Size = new Size(100, 23);
            buttonAddEncoders.TabIndex = 11;
            buttonAddEncoders.Text = "Add encoders";
            buttonAddEncoders.UseVisualStyleBackColor = true;
            buttonAddEncoders.Click += buttonClear_Click;
            // 
            // buttonClearEncoders
            // 
            buttonClearEncoders.Location = new Point(218, 230);
            buttonClearEncoders.Name = "buttonClearEncoders";
            buttonClearEncoders.Size = new Size(75, 23);
            buttonClearEncoders.TabIndex = 11;
            buttonClearEncoders.Text = "Clear";
            buttonClearEncoders.UseVisualStyleBackColor = true;
            buttonClearEncoders.Click += buttonClear_Click;
            // 
            // groupBoxAudioFiles
            // 
            groupBoxAudioFiles.Controls.Add(listBoxAudioFiles);
            groupBoxAudioFiles.Controls.Add(button2);
            groupBoxAudioFiles.Controls.Add(buttonAddAudioFiles);
            groupBoxAudioFiles.Controls.Add(buttonClearAudioFiles);
            groupBoxAudioFiles.Location = new Point(317, 12);
            groupBoxAudioFiles.Name = "groupBoxAudioFiles";
            groupBoxAudioFiles.Size = new Size(299, 260);
            groupBoxAudioFiles.TabIndex = 3;
            groupBoxAudioFiles.TabStop = false;
            groupBoxAudioFiles.Text = "Audio Files (Drag'n'Drop is available)";
            // 
            // listBoxAudioFiles
            // 
            listBoxAudioFiles.FormattingEnabled = true;
            listBoxAudioFiles.HorizontalScrollbar = true;
            listBoxAudioFiles.Location = new Point(6, 25);
            listBoxAudioFiles.Name = "listBoxAudioFiles";
            listBoxAudioFiles.SelectionMode = SelectionMode.MultiExtended;
            listBoxAudioFiles.Size = new Size(287, 199);
            listBoxAudioFiles.TabIndex = 2;
            listBoxAudioFiles.SelectedIndexChanged += listBox1_SelectedIndexChanged;
            // 
            // button2
            // 
            button2.Location = new Point(112, 230);
            button2.Name = "button2";
            button2.Size = new Size(100, 23);
            button2.TabIndex = 11;
            button2.Text = "Remove file";
            button2.UseVisualStyleBackColor = true;
            button2.Click += buttonClear_Click;
            // 
            // buttonAddAudioFiles
            // 
            buttonAddAudioFiles.Location = new Point(6, 230);
            buttonAddAudioFiles.Name = "buttonAddAudioFiles";
            buttonAddAudioFiles.Size = new Size(100, 23);
            buttonAddAudioFiles.TabIndex = 11;
            buttonAddAudioFiles.Text = "Add audio files";
            buttonAddAudioFiles.UseVisualStyleBackColor = true;
            buttonAddAudioFiles.Click += buttonClear_Click;
            // 
            // buttonClearAudioFiles
            // 
            buttonClearAudioFiles.Location = new Point(218, 230);
            buttonClearAudioFiles.Name = "buttonClearAudioFiles";
            buttonClearAudioFiles.Size = new Size(75, 23);
            buttonClearAudioFiles.TabIndex = 11;
            buttonClearAudioFiles.Text = "Clear";
            buttonClearAudioFiles.UseVisualStyleBackColor = true;
            buttonClearAudioFiles.Click += buttonClear_Click;
            // 
            // groupBoxJobsQueue
            // 
            groupBoxJobsQueue.Controls.Add(textBoxJobsQueue);
            groupBoxJobsQueue.Controls.Add(buttonStartJobList);
            groupBoxJobsQueue.Controls.Add(buttonExportJobList);
            groupBoxJobsQueue.Controls.Add(buttonImportJobList);
            groupBoxJobsQueue.Controls.Add(buttonClearJobList);
            groupBoxJobsQueue.Location = new Point(12, 278);
            groupBoxJobsQueue.Name = "groupBoxJobsQueue";
            groupBoxJobsQueue.Size = new Size(604, 422);
            groupBoxJobsQueue.TabIndex = 5;
            groupBoxJobsQueue.TabStop = false;
            groupBoxJobsQueue.Text = "Jobs Queue (Drag'n'Drop is available)";
            // 
            // textBoxJobsQueue
            // 
            textBoxJobsQueue.Location = new Point(6, 22);
            textBoxJobsQueue.Multiline = true;
            textBoxJobsQueue.Name = "textBoxJobsQueue";
            textBoxJobsQueue.PlaceholderText = "You may edit this text";
            textBoxJobsQueue.ScrollBars = ScrollBars.Both;
            textBoxJobsQueue.Size = new Size(592, 364);
            textBoxJobsQueue.TabIndex = 1;
            textBoxJobsQueue.WordWrap = false;
            // 
            // buttonStartJobList
            // 
            buttonStartJobList.Location = new Point(6, 392);
            buttonStartJobList.Name = "buttonStartJobList";
            buttonStartJobList.Size = new Size(100, 23);
            buttonStartJobList.TabIndex = 24;
            buttonStartJobList.Text = "Start job list";
            buttonStartJobList.UseVisualStyleBackColor = true;
            // 
            // buttonExportJobList
            // 
            buttonExportJobList.Location = new Point(218, 392);
            buttonExportJobList.Name = "buttonExportJobList";
            buttonExportJobList.Size = new Size(100, 23);
            buttonExportJobList.TabIndex = 3;
            buttonExportJobList.Text = "Export";
            buttonExportJobList.UseVisualStyleBackColor = true;
            // 
            // buttonImportJobList
            // 
            buttonImportJobList.Location = new Point(112, 392);
            buttonImportJobList.Name = "buttonImportJobList";
            buttonImportJobList.Size = new Size(100, 23);
            buttonImportJobList.TabIndex = 3;
            buttonImportJobList.Text = "Import";
            buttonImportJobList.UseVisualStyleBackColor = true;
            // 
            // buttonClearJobList
            // 
            buttonClearJobList.Location = new Point(523, 392);
            buttonClearJobList.Name = "buttonClearJobList";
            buttonClearJobList.Size = new Size(75, 23);
            buttonClearJobList.TabIndex = 11;
            buttonClearJobList.Text = "Clear";
            buttonClearJobList.UseVisualStyleBackColor = true;
            buttonClearJobList.Click += buttonClear_Click;
            // 
            // groupLog
            // 
            groupLog.Controls.Add(textBoxFlacExecutables);
            groupLog.Controls.Add(buttonClearLog);
            groupLog.Controls.Add(button3);
            groupLog.Controls.Add(buttonCopyLog);
            groupLog.Controls.Add(buttonOpenLogtxt);
            groupLog.Location = new Point(622, 278);
            groupLog.Name = "groupLog";
            groupLog.Size = new Size(984, 422);
            groupLog.TabIndex = 6;
            groupLog.TabStop = false;
            groupLog.Text = "Log";
            // 
            // button3
            // 
            button3.Location = new Point(705, 305);
            button3.Name = "button3";
            button3.Size = new Size(85, 23);
            button3.TabIndex = 16;
            button3.Text = "Open log.txt";
            button3.UseVisualStyleBackColor = true;
            button3.Click += buttonOpenLogtxt_Click;
            // 
            // buttonCopyLog
            // 
            buttonCopyLog.Location = new Point(812, 392);
            buttonCopyLog.Name = "buttonCopyLog";
            buttonCopyLog.Size = new Size(85, 23);
            buttonCopyLog.TabIndex = 16;
            buttonCopyLog.Text = "Copy Log";
            buttonCopyLog.UseVisualStyleBackColor = true;
            buttonCopyLog.Click += buttonOpenLogtxt_Click;
            // 
            // groupBoxJobSettings
            // 
            groupBoxJobSettings.Controls.Add(radioButtonDecode);
            groupBoxJobSettings.Controls.Add(radioButtonEncode);
            groupBoxJobSettings.Controls.Add(buttonAddJobToQueue);
            groupBoxJobSettings.Location = new Point(1486, 12);
            groupBoxJobSettings.Name = "groupBoxJobSettings";
            groupBoxJobSettings.Size = new Size(114, 260);
            groupBoxJobSettings.TabIndex = 25;
            groupBoxJobSettings.TabStop = false;
            groupBoxJobSettings.Text = "Job settings";
            // 
            // radioButtonDecode
            // 
            radioButtonDecode.AutoSize = true;
            radioButtonDecode.Location = new Point(6, 47);
            radioButtonDecode.Name = "radioButtonDecode";
            radioButtonDecode.Size = new Size(65, 19);
            radioButtonDecode.TabIndex = 25;
            radioButtonDecode.TabStop = true;
            radioButtonDecode.Text = "Decode";
            radioButtonDecode.UseVisualStyleBackColor = true;
            // 
            // radioButtonEncode
            // 
            radioButtonEncode.AutoSize = true;
            radioButtonEncode.Location = new Point(6, 22);
            radioButtonEncode.Name = "radioButtonEncode";
            radioButtonEncode.Size = new Size(64, 19);
            radioButtonEncode.TabIndex = 25;
            radioButtonEncode.TabStop = true;
            radioButtonEncode.Text = "Encode";
            radioButtonEncode.UseVisualStyleBackColor = true;
            // 
            // buttonAddJobToQueue
            // 
            buttonAddJobToQueue.Location = new Point(6, 230);
            buttonAddJobToQueue.Name = "buttonAddJobToQueue";
            buttonAddJobToQueue.Size = new Size(100, 23);
            buttonAddJobToQueue.TabIndex = 24;
            buttonAddJobToQueue.Text = "Add to queue";
            buttonAddJobToQueue.UseVisualStyleBackColor = true;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1620, 711);
            Controls.Add(groupLog);
            Controls.Add(groupBoxJobSettings);
            Controls.Add(groupBoxJobsQueue);
            Controls.Add(groupBoxAudioFiles);
            Controls.Add(groupBoxEncoders);
            Controls.Add(groupBoxEncoderSettings);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "Form1";
            Text = "FLAC Benchmark-H [beta 0.8 build 20241127.1]";
            groupBoxEncoderSettings.ResumeLayout(false);
            groupBoxEncoderSettings.PerformLayout();
            groupBoxEncoders.ResumeLayout(false);
            groupBoxAudioFiles.ResumeLayout(false);
            groupBoxJobsQueue.ResumeLayout(false);
            groupBoxJobsQueue.PerformLayout();
            groupLog.ResumeLayout(false);
            groupLog.PerformLayout();
            groupBoxJobSettings.ResumeLayout(false);
            groupBoxJobSettings.PerformLayout();
            ResumeLayout(false);
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
        private Button buttonStartEncode;
        private TextBox textBoxFlacExecutables;
        private Button buttonepr8;
        private Button buttonAsubdividetukey5flattop;
        private Button buttonNoPadding;
        private Button buttonNoSeektable;
        private Button buttonClear;
        private Button buttonClearLog;
        private Label labelFlacUsedVersion;
        private ListBox listBoxFlacExecutables;
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
        private ListBox listBoxAudioFiles;
        private CheckBox checkBoxHighPriority;
        private Button buttonStartDecode;
        private GroupBox groupBoxJobsQueue;
        private GroupBox groupLog;
        private Button buttonExportJobList;
        private Button buttonImportJobList;
        private Button buttonClearJobList;
        private TextBox textBoxJobsQueue;
        private Button buttonAddEncoders;
        private Button buttonClearEncoders;
        private Button buttonAddAudioFiles;
        private Button buttonClearAudioFiles;
        private Button button1;
        private Button button2;
        private GroupBox groupBoxJobSettings;
        private Button buttonStartJobList;
        private Button buttonAddJobToQueue;
        private RadioButton radioButtonDecode;
        private RadioButton radioButtonEncode;
        private Button button3;
        private Button buttonCopyLog;
    }
}
