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
            progressBar = new ProgressBar();
            buttonStart = new Button();
            labelAdditionalArguments = new Label();
            labelThreads = new Label();
            textBoxAdditionalArguments = new TextBox();
            textBoxThreads = new TextBox();
            labelCompressionLevel = new Label();
            textBoxCompressionLevel = new TextBox();
            textBoxLog = new TextBox();
            groupBoxEncoderSettings.SuspendLayout();
            SuspendLayout();
            // 
            // groupBoxEncoderSettings
            // 
            groupBoxEncoderSettings.Controls.Add(progressBar);
            groupBoxEncoderSettings.Controls.Add(buttonStart);
            groupBoxEncoderSettings.Controls.Add(labelAdditionalArguments);
            groupBoxEncoderSettings.Controls.Add(labelThreads);
            groupBoxEncoderSettings.Controls.Add(textBoxAdditionalArguments);
            groupBoxEncoderSettings.Controls.Add(textBoxThreads);
            groupBoxEncoderSettings.Controls.Add(labelCompressionLevel);
            groupBoxEncoderSettings.Controls.Add(textBoxCompressionLevel);
            groupBoxEncoderSettings.Location = new Point(12, 12);
            groupBoxEncoderSettings.Name = "groupBoxEncoderSettings";
            groupBoxEncoderSettings.Size = new Size(760, 256);
            groupBoxEncoderSettings.TabIndex = 0;
            groupBoxEncoderSettings.TabStop = false;
            groupBoxEncoderSettings.Text = "Encoder Settings";
            // 
            // progressBar
            // 
            progressBar.Location = new Point(139, 227);
            progressBar.Name = "progressBar";
            progressBar.Size = new Size(615, 23);
            progressBar.TabIndex = 4;
            // 
            // buttonStart
            // 
            buttonStart.Location = new Point(6, 227);
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
            labelAdditionalArguments.Location = new Point(6, 83);
            labelAdditionalArguments.Name = "labelAdditionalArguments";
            labelAdditionalArguments.Size = new Size(127, 15);
            labelAdditionalArguments.TabIndex = 1;
            labelAdditionalArguments.Text = "Additional Arguments:";
            // 
            // labelThreads
            // 
            labelThreads.AutoSize = true;
            labelThreads.Location = new Point(82, 54);
            labelThreads.Name = "labelThreads";
            labelThreads.Size = new Size(51, 15);
            labelThreads.TabIndex = 0;
            labelThreads.Text = "Threads:";
            // 
            // textBoxAdditionalArguments
            // 
            textBoxAdditionalArguments.Location = new Point(139, 80);
            textBoxAdditionalArguments.Name = "textBoxAdditionalArguments";
            textBoxAdditionalArguments.Size = new Size(615, 23);
            textBoxAdditionalArguments.TabIndex = 4;
            // 
            // textBoxThreads
            // 
            textBoxThreads.Location = new Point(139, 51);
            textBoxThreads.Name = "textBoxThreads";
            textBoxThreads.Size = new Size(28, 23);
            textBoxThreads.TabIndex = 3;
            textBoxThreads.Text = "1";
            textBoxThreads.TextAlign = HorizontalAlignment.Center;
            // 
            // labelCompressionLevel
            // 
            labelCompressionLevel.AutoSize = true;
            labelCompressionLevel.Location = new Point(23, 25);
            labelCompressionLevel.Name = "labelCompressionLevel";
            labelCompressionLevel.Size = new Size(110, 15);
            labelCompressionLevel.TabIndex = 0;
            labelCompressionLevel.Text = "Compression Level:";
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
            // textBoxLog
            // 
            textBoxLog.Location = new Point(12, 274);
            textBoxLog.Multiline = true;
            textBoxLog.Name = "textBoxLog";
            textBoxLog.PlaceholderText = "Log (there is also additional log file in the app folder)";
            textBoxLog.ReadOnly = true;
            textBoxLog.ScrollBars = ScrollBars.Both;
            textBoxLog.Size = new Size(760, 275);
            textBoxLog.TabIndex = 1;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(784, 561);
            Controls.Add(textBoxLog);
            Controls.Add(groupBoxEncoderSettings);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "Form1";
            Text = "FLAC Benchmark-H [beta 0.1]";
            groupBoxEncoderSettings.ResumeLayout(false);
            groupBoxEncoderSettings.PerformLayout();
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
        private TextBox textBoxLog;
    }
}
