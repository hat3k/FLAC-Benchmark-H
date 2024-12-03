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
            DataGridViewCellStyle dataGridViewCellStyle2 = new DataGridViewCellStyle();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            groupBoxEncoderSettings = new GroupBox();
            labelCommandLineEncoder = new Label();
            buttonAddJobToJobListEncoder = new Button();
            labelFlacUsedVersion = new Label();
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
            checkBoxHighPriority = new CheckBox();
            buttonStartDecode = new Button();
            labelCPUinfo = new Label();
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
            groupBoxSystemandSettings = new GroupBox();
            checkBoxClearTempFolder = new CheckBox();
            groupBoxDecoderSettings = new GroupBox();
            label1 = new Label();
            buttonAddJobToJobListDecoder = new Button();
            textBoxCommandLineOptionsDecoder = new TextBox();
            progressBarDecoder = new ProgressBar();
            buttonClearCommandLineDecoder = new Button();
            groupBoxEncoderSettings.SuspendLayout();
            groupBoxEncoders.SuspendLayout();
            groupBoxAudioFiles.SuspendLayout();
            groupBoxJobsList.SuspendLayout();
            groupLog.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridViewLog).BeginInit();
            groupBoxSystemandSettings.SuspendLayout();
            groupBoxDecoderSettings.SuspendLayout();
            SuspendLayout();
            // 
            // groupBoxEncoderSettings
            // 
            groupBoxEncoderSettings.Controls.Add(labelCommandLineEncoder);
            groupBoxEncoderSettings.Controls.Add(buttonAddJobToJobListEncoder);
            groupBoxEncoderSettings.Controls.Add(labelFlacUsedVersion);
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
            groupBoxEncoderSettings.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 204);
            groupBoxEncoderSettings.Location = new Point(622, 12);
            groupBoxEncoderSettings.Name = "groupBoxEncoderSettings";
            groupBoxEncoderSettings.Size = new Size(650, 171);
            groupBoxEncoderSettings.TabIndex = 0;
            groupBoxEncoderSettings.TabStop = false;
            groupBoxEncoderSettings.Text = "Encoder Settings";
            groupBoxEncoderSettings.Enter += groupBoxEncoderSettings_Enter;
            // 
            // labelCommandLineEncoder
            // 
            labelCommandLineEncoder.AutoSize = true;
            labelCommandLineEncoder.Font = new Font("Segoe UI", 9F);
            labelCommandLineEncoder.Location = new Point(27, 83);
            labelCommandLineEncoder.Name = "labelCommandLineEncoder";
            labelCommandLineEncoder.Size = new Size(89, 15);
            labelCommandLineEncoder.TabIndex = 24;
            labelCommandLineEncoder.Text = "Command line:";
            labelCommandLineEncoder.Click += labelCommandLine_Click;
            // 
            // buttonAddJobToJobListEncoder
            // 
            buttonAddJobToJobListEncoder.Enabled = false;
            buttonAddJobToJobListEncoder.Font = new Font("Segoe UI", 9F);
            buttonAddJobToJobListEncoder.Location = new Point(122, 141);
            buttonAddJobToJobListEncoder.Name = "buttonAddJobToJobListEncoder";
            buttonAddJobToJobListEncoder.Size = new Size(110, 23);
            buttonAddJobToJobListEncoder.TabIndex = 24;
            buttonAddJobToJobListEncoder.Text = "Add to Job List";
            buttonAddJobToJobListEncoder.UseVisualStyleBackColor = true;
            buttonAddJobToJobListEncoder.Click += buttonAddJobToJobList_Click;
            // 
            // labelFlacUsedVersion
            // 
            labelFlacUsedVersion.AutoSize = true;
            labelFlacUsedVersion.Enabled = false;
            labelFlacUsedVersion.Font = new Font("Segoe UI", 9F);
            labelFlacUsedVersion.Location = new Point(540, -3);
            labelFlacUsedVersion.Name = "labelFlacUsedVersion";
            labelFlacUsedVersion.Size = new Size(81, 15);
            labelFlacUsedVersion.TabIndex = 15;
            labelFlacUsedVersion.Text = "Using version:";
            labelFlacUsedVersion.Visible = false;
            labelFlacUsedVersion.Click += labelFlacUsedVersion_Click;
            // 
            // buttonStartEncode
            // 
            buttonStartEncode.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 204);
            buttonStartEncode.Location = new Point(6, 141);
            buttonStartEncode.Name = "buttonStartEncode";
            buttonStartEncode.Size = new Size(110, 23);
            buttonStartEncode.TabIndex = 1;
            buttonStartEncode.Text = "Encode";
            buttonStartEncode.UseVisualStyleBackColor = true;
            buttonStartEncode.Click += buttonStartEncode_Click;
            // 
            // progressBarEncoder
            // 
            progressBarEncoder.Enabled = false;
            progressBarEncoder.Location = new Point(238, 141);
            progressBarEncoder.Name = "progressBarEncoder";
            progressBarEncoder.Size = new Size(324, 23);
            progressBarEncoder.TabIndex = 4;
            progressBarEncoder.Click += progressBar_Click;
            // 
            // labelSetThreads
            // 
            labelSetThreads.AutoSize = true;
            labelSetThreads.Font = new Font("Segoe UI", 9F);
            labelSetThreads.Location = new Point(322, 54);
            labelSetThreads.Name = "labelSetThreads";
            labelSetThreads.Size = new Size(51, 15);
            labelSetThreads.TabIndex = 21;
            labelSetThreads.Text = "Threads:";
            labelSetThreads.Click += labelSetThreads_Click;
            // 
            // labelSetCores
            // 
            labelSetCores.AutoSize = true;
            labelSetCores.Font = new Font("Segoe UI", 9F);
            labelSetCores.Location = new Point(156, 54);
            labelSetCores.Name = "labelSetCores";
            labelSetCores.Size = new Size(40, 15);
            labelSetCores.TabIndex = 20;
            labelSetCores.Text = "Cores:";
            labelSetCores.Click += labelSetCores_Click;
            // 
            // textBoxCommandLineOptionsEncoder
            // 
            textBoxCommandLineOptionsEncoder.Font = new Font("Segoe UI", 9F);
            textBoxCommandLineOptionsEncoder.Location = new Point(122, 80);
            textBoxCommandLineOptionsEncoder.Name = "textBoxCommandLineOptionsEncoder";
            textBoxCommandLineOptionsEncoder.Size = new Size(440, 23);
            textBoxCommandLineOptionsEncoder.TabIndex = 4;
            textBoxCommandLineOptionsEncoder.TextChanged += textBoxCommandLineOptions_TextChanged;
            // 
            // labelSetCompression
            // 
            labelSetCompression.AutoSize = true;
            labelSetCompression.Font = new Font("Segoe UI", 9F);
            labelSetCompression.Location = new Point(170, 24);
            labelSetCompression.Name = "labelSetCompression";
            labelSetCompression.Size = new Size(26, 15);
            labelSetCompression.TabIndex = 19;
            labelSetCompression.Text = "Set:";
            labelSetCompression.Click += labelSetCompression_Click;
            // 
            // buttonepr8
            // 
            buttonepr8.Font = new Font("Segoe UI", 9F);
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
            buttonSetHalfThreads.Font = new Font("Segoe UI", 9F);
            buttonSetHalfThreads.Location = new Point(379, 50);
            buttonSetHalfThreads.Name = "buttonSetHalfThreads";
            buttonSetHalfThreads.Size = new Size(53, 23);
            buttonSetHalfThreads.TabIndex = 18;
            buttonSetHalfThreads.Text = "50%";
            buttonSetHalfThreads.UseVisualStyleBackColor = true;
            buttonSetHalfThreads.Click += buttonSetHalfThreads_Click;
            // 
            // buttonAsubdividetukey5flattop
            // 
            buttonAsubdividetukey5flattop.Font = new Font("Segoe UI", 9F);
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
            buttonSetMaxThreads.Font = new Font("Segoe UI", 9F);
            buttonSetMaxThreads.Location = new Point(438, 50);
            buttonSetMaxThreads.Name = "buttonSetMaxThreads";
            buttonSetMaxThreads.Size = new Size(53, 23);
            buttonSetMaxThreads.TabIndex = 18;
            buttonSetMaxThreads.Text = "100%";
            buttonSetMaxThreads.UseVisualStyleBackColor = true;
            buttonSetMaxThreads.Click += buttonSetMaxThreads_Click;
            // 
            // buttonNoPadding
            // 
            buttonNoPadding.Font = new Font("Segoe UI", 9F);
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
            buttonHalfCores.Font = new Font("Segoe UI", 9F);
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
            buttonNoSeektable.Font = new Font("Segoe UI", 9F);
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
            buttonSetMaxCores.Font = new Font("Segoe UI", 9F);
            buttonSetMaxCores.Location = new Point(261, 50);
            buttonSetMaxCores.Name = "buttonSetMaxCores";
            buttonSetMaxCores.Size = new Size(53, 23);
            buttonSetMaxCores.TabIndex = 18;
            buttonSetMaxCores.Text = "100%";
            buttonSetMaxCores.UseVisualStyleBackColor = true;
            buttonSetMaxCores.Click += buttonSetMaxCores_Click;
            // 
            // buttonClearCommandLineEncoder
            // 
            buttonClearCommandLineEncoder.Font = new Font("Segoe UI", 9F);
            buttonClearCommandLineEncoder.Location = new Point(568, 80);
            buttonClearCommandLineEncoder.Name = "buttonClearCommandLineEncoder";
            buttonClearCommandLineEncoder.Size = new Size(75, 23);
            buttonClearCommandLineEncoder.TabIndex = 11;
            buttonClearCommandLineEncoder.Text = "Clear";
            buttonClearCommandLineEncoder.UseVisualStyleBackColor = true;
            buttonClearCommandLineEncoder.Click += buttonClearCommandLine_Click;
            // 
            // buttonMaxCompressionLevel
            // 
            buttonMaxCompressionLevel.Font = new Font("Segoe UI", 9F);
            buttonMaxCompressionLevel.Location = new Point(261, 20);
            buttonMaxCompressionLevel.Name = "buttonMaxCompressionLevel";
            buttonMaxCompressionLevel.Size = new Size(53, 23);
            buttonMaxCompressionLevel.TabIndex = 18;
            buttonMaxCompressionLevel.Text = "MAX";
            buttonMaxCompressionLevel.UseVisualStyleBackColor = true;
            buttonMaxCompressionLevel.Click += buttonMaxCompressionLevel_Click;
            // 
            // button5CompressionLevel
            // 
            button5CompressionLevel.Font = new Font("Segoe UI", 9F);
            button5CompressionLevel.Location = new Point(202, 20);
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
            labelThreads.Font = new Font("Segoe UI", 9F);
            labelThreads.Location = new Point(65, 54);
            labelThreads.Name = "labelThreads";
            labelThreads.Size = new Size(51, 15);
            labelThreads.TabIndex = 0;
            labelThreads.Text = "Threads:";
            labelThreads.Click += labelThreads_Click;
            // 
            // textBoxCompressionLevel
            // 
            textBoxCompressionLevel.Font = new Font("Segoe UI", 9F);
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
            labelCompressionLevel.Font = new Font("Segoe UI", 9F);
            labelCompressionLevel.Location = new Point(6, 24);
            labelCompressionLevel.Name = "labelCompressionLevel";
            labelCompressionLevel.Size = new Size(110, 15);
            labelCompressionLevel.TabIndex = 0;
            labelCompressionLevel.Text = "Compression Level:";
            labelCompressionLevel.Click += labelCompressionLevel_Click;
            // 
            // textBoxThreads
            // 
            textBoxThreads.Font = new Font("Segoe UI", 9F);
            textBoxThreads.Location = new Point(122, 51);
            textBoxThreads.Name = "textBoxThreads";
            textBoxThreads.Size = new Size(28, 23);
            textBoxThreads.TabIndex = 3;
            textBoxThreads.Text = "1";
            textBoxThreads.TextAlign = HorizontalAlignment.Center;
            textBoxThreads.TextChanged += textBoxThreads_TextChanged;
            // 
            // buttonStop
            // 
            buttonStop.Font = new Font("Segoe UI", 9F);
            buttonStop.Location = new Point(6, 230);
            buttonStop.Name = "buttonStop";
            buttonStop.Size = new Size(154, 23);
            buttonStop.TabIndex = 25;
            buttonStop.Text = "Stop Encoding/Decoding";
            buttonStop.UseVisualStyleBackColor = true;
            buttonStop.Click += buttonStop_Click;
            // 
            // checkBoxHighPriority
            // 
            checkBoxHighPriority.AutoSize = true;
            checkBoxHighPriority.Font = new Font("Segoe UI", 9F);
            checkBoxHighPriority.Location = new Point(6, 177);
            checkBoxHighPriority.Name = "checkBoxHighPriority";
            checkBoxHighPriority.Size = new Size(93, 19);
            checkBoxHighPriority.TabIndex = 22;
            checkBoxHighPriority.Text = "High Priority";
            checkBoxHighPriority.UseVisualStyleBackColor = true;
            checkBoxHighPriority.CheckedChanged += checkBoxHighPriority_CheckedChanged;
            // 
            // buttonStartDecode
            // 
            buttonStartDecode.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 204);
            buttonStartDecode.Location = new Point(6, 53);
            buttonStartDecode.Name = "buttonStartDecode";
            buttonStartDecode.Size = new Size(110, 23);
            buttonStartDecode.TabIndex = 23;
            buttonStartDecode.Text = "Decode";
            buttonStartDecode.UseVisualStyleBackColor = true;
            buttonStartDecode.Click += buttonStartDecode_Click;
            // 
            // labelCPUinfo
            // 
            labelCPUinfo.Font = new Font("Segoe UI", 9F);
            labelCPUinfo.Location = new Point(6, 24);
            labelCPUinfo.Name = "labelCPUinfo";
            labelCPUinfo.Size = new Size(154, 50);
            labelCPUinfo.TabIndex = 17;
            labelCPUinfo.Text = "CPU Info";
            // 
            // buttonOpenLogtxt
            // 
            buttonOpenLogtxt.Location = new Point(559, 392);
            buttonOpenLogtxt.Name = "buttonOpenLogtxt";
            buttonOpenLogtxt.Size = new Size(85, 23);
            buttonOpenLogtxt.TabIndex = 16;
            buttonOpenLogtxt.Text = "Open log.txt";
            buttonOpenLogtxt.UseVisualStyleBackColor = true;
            buttonOpenLogtxt.Click += buttonOpenLogtxt_Click;
            // 
            // buttonClearLog
            // 
            buttonClearLog.Location = new Point(741, 392);
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
            groupBoxEncoders.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 204);
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
            buttonRemoveEncoder.Font = new Font("Segoe UI", 9F);
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
            listViewFlacExecutables.Font = new Font("Segoe UI", 9F);
            listViewFlacExecutables.FullRowSelect = true;
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
            buttonAddEncoders.Font = new Font("Segoe UI", 9F);
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
            buttonClearEncoders.Font = new Font("Segoe UI", 9F);
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
            groupBoxAudioFiles.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 204);
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
            listViewAudioFiles.Font = new Font("Segoe UI", 9F);
            listViewAudioFiles.FullRowSelect = true;
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
            buttonRemoveAudiofile.Font = new Font("Segoe UI", 9F);
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
            buttonAddAudioFiles.Font = new Font("Segoe UI", 9F);
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
            buttonClearAudioFiles.Font = new Font("Segoe UI", 9F);
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
            groupLog.Size = new Size(822, 422);
            groupLog.TabIndex = 6;
            groupLog.TabStop = false;
            groupLog.Text = "Log";
            groupLog.Enter += groupLog_Enter;
            // 
            // dataGridViewLog
            // 
            dataGridViewLog.AllowUserToAddRows = false;
            dataGridViewLog.AllowUserToOrderColumns = true;
            dataGridViewLog.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
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
            dataGridViewLog.Location = new Point(6, 22);
            dataGridViewLog.Name = "dataGridViewLog";
            dataGridViewLog.ReadOnly = true;
            dataGridViewLog.RowHeadersVisible = false;
            dataGridViewLog.RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.AutoSizeToDisplayedHeaders;
            dataGridViewLog.Size = new Size(810, 364);
            dataGridViewLog.TabIndex = 17;
            // 
            // buttonCopyLog
            // 
            buttonCopyLog.Enabled = false;
            buttonCopyLog.Location = new Point(650, 392);
            buttonCopyLog.Name = "buttonCopyLog";
            buttonCopyLog.Size = new Size(85, 23);
            buttonCopyLog.TabIndex = 16;
            buttonCopyLog.Text = "Copy Log";
            buttonCopyLog.UseVisualStyleBackColor = true;
            buttonCopyLog.Click += buttonCopyLog_Click;
            // 
            // groupBoxSystemandSettings
            // 
            groupBoxSystemandSettings.Controls.Add(buttonStop);
            groupBoxSystemandSettings.Controls.Add(checkBoxClearTempFolder);
            groupBoxSystemandSettings.Controls.Add(labelCPUinfo);
            groupBoxSystemandSettings.Controls.Add(checkBoxHighPriority);
            groupBoxSystemandSettings.Location = new Point(1278, 12);
            groupBoxSystemandSettings.Name = "groupBoxSystemandSettings";
            groupBoxSystemandSettings.Size = new Size(166, 260);
            groupBoxSystemandSettings.TabIndex = 25;
            groupBoxSystemandSettings.TabStop = false;
            groupBoxSystemandSettings.Text = "System and Settings";
            groupBoxSystemandSettings.Enter += groupBoxJobSettings_Enter;
            // 
            // checkBoxClearTempFolder
            // 
            checkBoxClearTempFolder.AutoSize = true;
            checkBoxClearTempFolder.Enabled = false;
            checkBoxClearTempFolder.Location = new Point(6, 201);
            checkBoxClearTempFolder.Name = "checkBoxClearTempFolder";
            checkBoxClearTempFolder.Size = new Size(157, 19);
            checkBoxClearTempFolder.TabIndex = 18;
            checkBoxClearTempFolder.Text = "Clear temp folder on exit";
            checkBoxClearTempFolder.UseVisualStyleBackColor = true;
            checkBoxClearTempFolder.CheckedChanged += checkBoxClearTempFolder_CheckedChanged;
            // 
            // groupBoxDecoderSettings
            // 
            groupBoxDecoderSettings.Controls.Add(buttonStartDecode);
            groupBoxDecoderSettings.Controls.Add(label1);
            groupBoxDecoderSettings.Controls.Add(buttonAddJobToJobListDecoder);
            groupBoxDecoderSettings.Controls.Add(textBoxCommandLineOptionsDecoder);
            groupBoxDecoderSettings.Controls.Add(progressBarDecoder);
            groupBoxDecoderSettings.Controls.Add(buttonClearCommandLineDecoder);
            groupBoxDecoderSettings.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 204);
            groupBoxDecoderSettings.Location = new Point(622, 189);
            groupBoxDecoderSettings.Name = "groupBoxDecoderSettings";
            groupBoxDecoderSettings.Size = new Size(650, 83);
            groupBoxDecoderSettings.TabIndex = 26;
            groupBoxDecoderSettings.TabStop = false;
            groupBoxDecoderSettings.Text = "Decoder Settings";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Segoe UI", 9F);
            label1.Location = new Point(27, 24);
            label1.Name = "label1";
            label1.Size = new Size(89, 15);
            label1.TabIndex = 24;
            label1.Text = "Command line:";
            label1.Click += labelCommandLine_Click;
            // 
            // buttonAddJobToJobListDecoder
            // 
            buttonAddJobToJobListDecoder.Enabled = false;
            buttonAddJobToJobListDecoder.Font = new Font("Segoe UI", 9F);
            buttonAddJobToJobListDecoder.Location = new Point(122, 53);
            buttonAddJobToJobListDecoder.Name = "buttonAddJobToJobListDecoder";
            buttonAddJobToJobListDecoder.Size = new Size(110, 23);
            buttonAddJobToJobListDecoder.TabIndex = 24;
            buttonAddJobToJobListDecoder.Text = "Add to Job List";
            buttonAddJobToJobListDecoder.UseVisualStyleBackColor = true;
            buttonAddJobToJobListDecoder.Click += buttonAddJobToJobList_Click;
            // 
            // textBoxCommandLineOptionsDecoder
            // 
            textBoxCommandLineOptionsDecoder.Font = new Font("Segoe UI", 9F);
            textBoxCommandLineOptionsDecoder.Location = new Point(122, 21);
            textBoxCommandLineOptionsDecoder.Name = "textBoxCommandLineOptionsDecoder";
            textBoxCommandLineOptionsDecoder.Size = new Size(440, 23);
            textBoxCommandLineOptionsDecoder.TabIndex = 4;
            textBoxCommandLineOptionsDecoder.TextChanged += textBoxCommandLineOptions_TextChanged;
            // 
            // progressBarDecoder
            // 
            progressBarDecoder.Enabled = false;
            progressBarDecoder.Location = new Point(238, 53);
            progressBarDecoder.Name = "progressBarDecoder";
            progressBarDecoder.Size = new Size(324, 23);
            progressBarDecoder.TabIndex = 4;
            progressBarDecoder.Click += progressBar_Click;
            // 
            // buttonClearCommandLineDecoder
            // 
            buttonClearCommandLineDecoder.Font = new Font("Segoe UI", 9F);
            buttonClearCommandLineDecoder.Location = new Point(568, 21);
            buttonClearCommandLineDecoder.Name = "buttonClearCommandLineDecoder";
            buttonClearCommandLineDecoder.Size = new Size(75, 23);
            buttonClearCommandLineDecoder.TabIndex = 11;
            buttonClearCommandLineDecoder.Text = "Clear";
            buttonClearCommandLineDecoder.UseVisualStyleBackColor = true;
            buttonClearCommandLineDecoder.Click += buttonClearCommandLine_Click;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1457, 711);
            Controls.Add(groupBoxDecoderSettings);
            Controls.Add(groupLog);
            Controls.Add(groupBoxSystemandSettings);
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
            groupBoxSystemandSettings.ResumeLayout(false);
            groupBoxSystemandSettings.PerformLayout();
            groupBoxDecoderSettings.ResumeLayout(false);
            groupBoxDecoderSettings.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private GroupBox groupBoxEncoderSettings;
        private ListView listViewFlacExecutables;
        private Label labelCompressionLevel;
        private Label labelThreads;
        private TextBox textBoxThreads;
        private TextBox textBoxCompressionLevel;
        private TextBox textBoxCommandLineOptionsEncoder;
        private ProgressBar progressBarEncoder;
        private Button buttonStartEncode;
        private TextBox textBoxLog;
        private Button buttonepr8;
        private Button buttonAsubdividetukey5flattop;
        private Button buttonNoPadding;
        private Button buttonNoSeektable;
        private Button buttonClearCommandLineEncoder;
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
        private GroupBox groupBoxSystemandSettings;
        private Button buttonStartJobList;
        private Button buttonAddJobToJobListEncoder;
        private Button buttonCopyLog;
        private Label labelCommandLineEncoder;
        private Button buttonStop;
        private DataGridView dataGridViewLog;
        private GroupBox groupBoxDecoderSettings;
        private Label label1;
        private Button buttonAddJobToJobListDecoder;
        private TextBox textBoxCommandLineOptionsDecoder;
        private Button buttonClearCommandLineDecoder;
        private ProgressBar progressBarDecoder;
        private Label labelSetCompression;
        private CheckBox checkBoxClearTempFolder;
    }
}
