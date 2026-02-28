namespace FLAC_Benchmark_H
{
    partial class SummaryForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            richTextBoxSummary = new RichTextBox();
            buttonCopySummaryAudioFiles = new Button();
            buttonCloseSummaryAudioFiles = new Button();
            SuspendLayout();
            // 
            // richTextBoxSummary
            // 
            richTextBoxSummary.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            richTextBoxSummary.DetectUrls = false;
            richTextBoxSummary.Font = new Font("Consolas", 9F, FontStyle.Regular, GraphicsUnit.Point, 204);
            richTextBoxSummary.Location = new Point(12, 12);
            richTextBoxSummary.Name = "richTextBoxSummary";
            richTextBoxSummary.ReadOnly = true;
            richTextBoxSummary.Size = new Size(760, 508);
            richTextBoxSummary.TabIndex = 0;
            richTextBoxSummary.Text = "";
            richTextBoxSummary.WordWrap = false;
            richTextBoxSummary.MouseClick += RichTextBoxSummary_MouseClick;
            richTextBoxSummary.MouseMove += RichTextBoxSummary_MouseMove;
            // 
            // buttonCopySummaryAudioFiles
            // 
            buttonCopySummaryAudioFiles.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonCopySummaryAudioFiles.Location = new Point(656, 526);
            buttonCopySummaryAudioFiles.Name = "buttonCopySummaryAudioFiles";
            buttonCopySummaryAudioFiles.Size = new Size(55, 23);
            buttonCopySummaryAudioFiles.TabIndex = 1;
            buttonCopySummaryAudioFiles.Text = "Copy";
            buttonCopySummaryAudioFiles.UseVisualStyleBackColor = true;
            buttonCopySummaryAudioFiles.Click += ButtonCopySummaryAudioFiles_Click;
            // 
            // buttonCloseSummaryAudioFiles
            // 
            buttonCloseSummaryAudioFiles.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonCloseSummaryAudioFiles.Location = new Point(717, 526);
            buttonCloseSummaryAudioFiles.Name = "buttonCloseSummaryAudioFiles";
            buttonCloseSummaryAudioFiles.Size = new Size(55, 23);
            buttonCloseSummaryAudioFiles.TabIndex = 2;
            buttonCloseSummaryAudioFiles.Text = "Close";
            buttonCloseSummaryAudioFiles.UseVisualStyleBackColor = true;
            buttonCloseSummaryAudioFiles.Click += ButtonCloseSummaryAudioFiles_Click;
            // 
            // SummaryForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(784, 561);
            Controls.Add(richTextBoxSummary);
            Controls.Add(buttonCopySummaryAudioFiles);
            Controls.Add(buttonCloseSummaryAudioFiles);
            DoubleBuffered = true;
            MinimumSize = new Size(300, 432);
            Name = "SummaryForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Audio Files Summary";
            ResumeLayout(false);
        }

        #endregion

        private RichTextBox richTextBoxSummary;
        private Button buttonCopySummaryAudioFiles;
        private Button buttonCloseSummaryAudioFiles;
    }
}