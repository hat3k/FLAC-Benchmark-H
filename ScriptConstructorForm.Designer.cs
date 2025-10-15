namespace FLAC_Benchmark_H
{
    partial class ScriptConstructorForm
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
            richTextBoxScriptHelp = new RichTextBox();
            buttonClearScript = new Button();
            labelTypeYourScript = new Label();
            buttonCloseScriptConstructorForm = new Button();
            buttonAddJobToJobListScript = new Button();
            comboBoxScript = new ComboBox();
            labelPreviewJobsListMadeByScript = new Label();
            listViewPreviewJobsListMadeByScript = new ListView();
            JobType = new ColumnHeader();
            Passes = new ColumnHeader();
            Parameters = new ColumnHeader();
            SuspendLayout();
            // 
            // richTextBoxScriptHelp
            // 
            richTextBoxScriptHelp.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            richTextBoxScriptHelp.Location = new Point(12, 12);
            richTextBoxScriptHelp.Name = "richTextBoxScriptHelp";
            richTextBoxScriptHelp.ReadOnly = true;
            richTextBoxScriptHelp.Size = new Size(760, 207);
            richTextBoxScriptHelp.TabIndex = 0;
            richTextBoxScriptHelp.Text = "";
            // 
            // buttonClearScript
            // 
            buttonClearScript.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            buttonClearScript.Location = new Point(717, 240);
            buttonClearScript.Name = "buttonClearScript";
            buttonClearScript.Size = new Size(55, 23);
            buttonClearScript.TabIndex = 3;
            buttonClearScript.Text = "Clear";
            buttonClearScript.UseVisualStyleBackColor = true;
            // 
            // labelTypeYourScript
            // 
            labelTypeYourScript.AutoSize = true;
            labelTypeYourScript.Location = new Point(12, 222);
            labelTypeYourScript.Name = "labelTypeYourScript";
            labelTypeYourScript.Size = new Size(94, 15);
            labelTypeYourScript.TabIndex = 1;
            labelTypeYourScript.Text = "Type your Script:";
            // 
            // buttonCloseScriptConstructorForm
            // 
            buttonCloseScriptConstructorForm.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonCloseScriptConstructorForm.Location = new Point(717, 526);
            buttonCloseScriptConstructorForm.Name = "buttonCloseScriptConstructorForm";
            buttonCloseScriptConstructorForm.Size = new Size(55, 23);
            buttonCloseScriptConstructorForm.TabIndex = 7;
            buttonCloseScriptConstructorForm.Text = "Close";
            buttonCloseScriptConstructorForm.UseVisualStyleBackColor = true;
            buttonCloseScriptConstructorForm.Click += buttonCloseScriptConstructorForm_Click;
            // 
            // buttonAddJobToJobListScript
            // 
            buttonAddJobToJobListScript.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonAddJobToJobListScript.Location = new Point(616, 526);
            buttonAddJobToJobListScript.Name = "buttonAddJobToJobListScript";
            buttonAddJobToJobListScript.Size = new Size(95, 23);
            buttonAddJobToJobListScript.TabIndex = 6;
            buttonAddJobToJobListScript.Text = "Add to Job List";
            buttonAddJobToJobListScript.UseVisualStyleBackColor = true;
            // 
            // comboBoxScript
            // 
            comboBoxScript.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            comboBoxScript.FormattingEnabled = true;
            comboBoxScript.Location = new Point(12, 240);
            comboBoxScript.Name = "comboBoxScript";
            comboBoxScript.Size = new Size(699, 23);
            comboBoxScript.TabIndex = 2;
            comboBoxScript.Text = "-[0..8]";
            // 
            // labelPreviewJobsListMadeByScript
            // 
            labelPreviewJobsListMadeByScript.AutoSize = true;
            labelPreviewJobsListMadeByScript.Location = new Point(12, 266);
            labelPreviewJobsListMadeByScript.Name = "labelPreviewJobsListMadeByScript";
            labelPreviewJobsListMadeByScript.Size = new Size(93, 15);
            labelPreviewJobsListMadeByScript.TabIndex = 4;
            labelPreviewJobsListMadeByScript.Text = "Preview Job List:";
            // 
            // listViewPreviewJobsListMadeByScript
            // 
            listViewPreviewJobsListMadeByScript.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            listViewPreviewJobsListMadeByScript.Columns.AddRange(new ColumnHeader[] { JobType, Passes, Parameters });
            listViewPreviewJobsListMadeByScript.FullRowSelect = true;
            listViewPreviewJobsListMadeByScript.Location = new Point(12, 284);
            listViewPreviewJobsListMadeByScript.Name = "listViewPreviewJobsListMadeByScript";
            listViewPreviewJobsListMadeByScript.OwnerDraw = true;
            listViewPreviewJobsListMadeByScript.Size = new Size(760, 236);
            listViewPreviewJobsListMadeByScript.TabIndex = 5;
            listViewPreviewJobsListMadeByScript.UseCompatibleStateImageBehavior = false;
            listViewPreviewJobsListMadeByScript.View = View.Details;
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
            // ScriptConstructorForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(784, 561);
            Controls.Add(listViewPreviewJobsListMadeByScript);
            Controls.Add(labelPreviewJobsListMadeByScript);
            Controls.Add(comboBoxScript);
            Controls.Add(buttonAddJobToJobListScript);
            Controls.Add(buttonCloseScriptConstructorForm);
            Controls.Add(labelTypeYourScript);
            Controls.Add(buttonClearScript);
            Controls.Add(richTextBoxScriptHelp);
            DoubleBuffered = true;
            MinimumSize = new Size(300, 432);
            Name = "ScriptConstructorForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Script Constructor";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private RichTextBox richTextBoxScriptHelp;
        private Button buttonClearScript;
        private Label labelTypeYourScript;
        private Button buttonCloseScriptConstructorForm;
        private Button buttonAddJobToJobListScript;
        private ComboBox comboBoxScript;
        private Label labelPreviewJobsListMadeByScript;
        private ListView listViewPreviewJobsListMadeByScript;
        private ColumnHeader JobType;
        private ColumnHeader Passes;
        private ColumnHeader Parameters;
    }
}