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
            splitContainerScriptConstructor = new SplitContainer();
            labelTypeYourScript = new Label();
            radioButtonScriptEncode = new RadioButton();
            comboBoxScriptEncode = new ComboBox();
            buttonClearScriptEncode = new Button();
            radioButtonScriptDecode = new RadioButton();
            comboBoxScriptDecode = new ComboBox();
            buttonClearScriptDecode = new Button();
            labelPreviewJobsListMadeByScript = new Label();
            dataGridViewPreviewJobsListMadeByScript = new DataGridViewEx();
            Column1CheckBox = new DataGridViewCheckBoxColumn();
            Column2JobType = new DataGridViewTextBoxColumn();
            Column3Passes = new DataGridViewTextBoxColumn();
            Column4Parameters = new DataGridViewTextBoxColumn();
            checkBoxScriptShowHelp = new CheckBox();
            labelScripConstructorJobAdded = new Label();
            buttonAddJobToJobListScript = new Button();
            buttonCloseScriptConstructorForm = new Button();
            richTextBoxScriptHelp = new RichTextBox();
            ((System.ComponentModel.ISupportInitialize)splitContainerScriptConstructor).BeginInit();
            splitContainerScriptConstructor.Panel1.SuspendLayout();
            splitContainerScriptConstructor.Panel2.SuspendLayout();
            splitContainerScriptConstructor.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridViewPreviewJobsListMadeByScript).BeginInit();
            SuspendLayout();
            // 
            // splitContainerScriptConstructor
            // 
            splitContainerScriptConstructor.Dock = DockStyle.Fill;
            splitContainerScriptConstructor.Location = new Point(0, 0);
            splitContainerScriptConstructor.Margin = new Padding(0);
            splitContainerScriptConstructor.Name = "splitContainerScriptConstructor";
            // 
            // splitContainerScriptConstructor.Panel1
            // 
            splitContainerScriptConstructor.Panel1.Controls.Add(labelTypeYourScript);
            splitContainerScriptConstructor.Panel1.Controls.Add(radioButtonScriptEncode);
            splitContainerScriptConstructor.Panel1.Controls.Add(comboBoxScriptEncode);
            splitContainerScriptConstructor.Panel1.Controls.Add(buttonClearScriptEncode);
            splitContainerScriptConstructor.Panel1.Controls.Add(radioButtonScriptDecode);
            splitContainerScriptConstructor.Panel1.Controls.Add(comboBoxScriptDecode);
            splitContainerScriptConstructor.Panel1.Controls.Add(buttonClearScriptDecode);
            splitContainerScriptConstructor.Panel1.Controls.Add(labelPreviewJobsListMadeByScript);
            splitContainerScriptConstructor.Panel1.Controls.Add(dataGridViewPreviewJobsListMadeByScript);
            splitContainerScriptConstructor.Panel1.Controls.Add(checkBoxScriptShowHelp);
            splitContainerScriptConstructor.Panel1.Controls.Add(labelScripConstructorJobAdded);
            splitContainerScriptConstructor.Panel1.Controls.Add(buttonAddJobToJobListScript);
            splitContainerScriptConstructor.Panel1.Controls.Add(buttonCloseScriptConstructorForm);
            // 
            // splitContainerScriptConstructor.Panel2
            // 
            splitContainerScriptConstructor.Panel2.Controls.Add(richTextBoxScriptHelp);
            splitContainerScriptConstructor.Panel2.Padding = new Padding(3, 12, 12, 12);
            splitContainerScriptConstructor.Size = new Size(784, 561);
            splitContainerScriptConstructor.SplitterDistance = 435;
            splitContainerScriptConstructor.TabIndex = 0;
            // 
            // labelTypeYourScript
            // 
            labelTypeYourScript.AutoSize = true;
            labelTypeYourScript.Location = new Point(12, 9);
            labelTypeYourScript.Name = "labelTypeYourScript";
            labelTypeYourScript.Size = new Size(94, 15);
            labelTypeYourScript.TabIndex = 1;
            labelTypeYourScript.Text = "Type your Script:";
            // 
            // radioButtonScriptEncode
            // 
            radioButtonScriptEncode.AutoSize = true;
            radioButtonScriptEncode.Checked = true;
            radioButtonScriptEncode.Location = new Point(15, 30);
            radioButtonScriptEncode.Name = "radioButtonScriptEncode";
            radioButtonScriptEncode.Size = new Size(64, 19);
            radioButtonScriptEncode.TabIndex = 2;
            radioButtonScriptEncode.TabStop = true;
            radioButtonScriptEncode.Text = "Encode";
            radioButtonScriptEncode.UseVisualStyleBackColor = true;
            radioButtonScriptEncode.CheckedChanged += RadioButtonScriptEncode_CheckedChanged;
            // 
            // comboBoxScriptEncode
            // 
            comboBoxScriptEncode.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            comboBoxScriptEncode.FormattingEnabled = true;
            comboBoxScriptEncode.Location = new Point(94, 28);
            comboBoxScriptEncode.Name = "comboBoxScriptEncode";
            comboBoxScriptEncode.Size = new Size(268, 23);
            comboBoxScriptEncode.TabIndex = 3;
            comboBoxScriptEncode.Text = "-[0..8]";
            comboBoxScriptEncode.TextChanged += ComboBoxScript_TextChanged;
            // 
            // buttonClearScriptEncode
            // 
            buttonClearScriptEncode.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            buttonClearScriptEncode.Location = new Point(368, 28);
            buttonClearScriptEncode.Name = "buttonClearScriptEncode";
            buttonClearScriptEncode.Size = new Size(55, 23);
            buttonClearScriptEncode.TabIndex = 4;
            buttonClearScriptEncode.Text = "Clear";
            buttonClearScriptEncode.UseVisualStyleBackColor = true;
            buttonClearScriptEncode.Click += ButtonClearScriptComboBox_Click;
            // 
            // radioButtonScriptDecode
            // 
            radioButtonScriptDecode.AutoSize = true;
            radioButtonScriptDecode.Location = new Point(15, 55);
            radioButtonScriptDecode.Name = "radioButtonScriptDecode";
            radioButtonScriptDecode.Size = new Size(65, 19);
            radioButtonScriptDecode.TabIndex = 5;
            radioButtonScriptDecode.Text = "Decode";
            radioButtonScriptDecode.UseVisualStyleBackColor = true;
            radioButtonScriptDecode.CheckedChanged += RadioButtonScriptDecode_CheckedChanged;
            // 
            // comboBoxScriptDecode
            // 
            comboBoxScriptDecode.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            comboBoxScriptDecode.Enabled = false;
            comboBoxScriptDecode.FormattingEnabled = true;
            comboBoxScriptDecode.Location = new Point(94, 53);
            comboBoxScriptDecode.Name = "comboBoxScriptDecode";
            comboBoxScriptDecode.Size = new Size(268, 23);
            comboBoxScriptDecode.TabIndex = 6;
            comboBoxScriptDecode.TextChanged += ComboBoxScript_TextChanged;
            // 
            // buttonClearScriptDecode
            // 
            buttonClearScriptDecode.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            buttonClearScriptDecode.Enabled = false;
            buttonClearScriptDecode.Location = new Point(368, 53);
            buttonClearScriptDecode.Name = "buttonClearScriptDecode";
            buttonClearScriptDecode.Size = new Size(55, 23);
            buttonClearScriptDecode.TabIndex = 7;
            buttonClearScriptDecode.Text = "Clear";
            buttonClearScriptDecode.UseVisualStyleBackColor = true;
            buttonClearScriptDecode.Click += ButtonClearScriptComboBox_Click;
            // 
            // labelPreviewJobsListMadeByScript
            // 
            labelPreviewJobsListMadeByScript.AutoSize = true;
            labelPreviewJobsListMadeByScript.Location = new Point(12, 79);
            labelPreviewJobsListMadeByScript.Name = "labelPreviewJobsListMadeByScript";
            labelPreviewJobsListMadeByScript.Size = new Size(93, 15);
            labelPreviewJobsListMadeByScript.TabIndex = 8;
            labelPreviewJobsListMadeByScript.Text = "Preview Job List:";
            // 
            // dataGridViewPreviewJobsListMadeByScript
            // 
            dataGridViewPreviewJobsListMadeByScript.AllowUserToAddRows = false;
            dataGridViewPreviewJobsListMadeByScript.AllowUserToDeleteRows = false;
            dataGridViewPreviewJobsListMadeByScript.AllowUserToResizeRows = false;
            dataGridViewPreviewJobsListMadeByScript.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dataGridViewPreviewJobsListMadeByScript.BackgroundColor = SystemColors.Window;
            dataGridViewPreviewJobsListMadeByScript.BorderStyle = BorderStyle.Fixed3D;
            dataGridViewPreviewJobsListMadeByScript.ClipboardCopyMode = DataGridViewClipboardCopyMode.EnableWithoutHeaderText;
            dataGridViewPreviewJobsListMadeByScript.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dataGridViewPreviewJobsListMadeByScript.Columns.AddRange(new DataGridViewColumn[] { Column1CheckBox, Column2JobType, Column3Passes, Column4Parameters });
            dataGridViewPreviewJobsListMadeByScript.GridColor = SystemColors.Control;
            dataGridViewPreviewJobsListMadeByScript.Location = new Point(12, 97);
            dataGridViewPreviewJobsListMadeByScript.Name = "dataGridViewPreviewJobsListMadeByScript";
            dataGridViewPreviewJobsListMadeByScript.ReadOnly = true;
            dataGridViewPreviewJobsListMadeByScript.RowHeadersVisible = false;
            dataGridViewPreviewJobsListMadeByScript.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGridViewPreviewJobsListMadeByScript.Size = new Size(411, 424);
            dataGridViewPreviewJobsListMadeByScript.TabIndex = 9;
            dataGridViewPreviewJobsListMadeByScript.CellFormatting += DataGridViewPreviewJobsListMadeByScript_CellFormatting;
            dataGridViewPreviewJobsListMadeByScript.KeyDown += DataGridViewPreviewJobsListMadeByScript_KeyDown;
            dataGridViewPreviewJobsListMadeByScript.MouseDown += DataGridViewPreviewJobsListMadeByScript_MouseDown;
            // 
            // Column1CheckBox
            // 
            Column1CheckBox.FillWeight = 20F;
            Column1CheckBox.Frozen = true;
            Column1CheckBox.HeaderText = "";
            Column1CheckBox.Name = "Column1CheckBox";
            Column1CheckBox.ReadOnly = true;
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
            Column3Passes.ReadOnly = true;
            Column3Passes.Width = 48;
            // 
            // Column4Parameters
            // 
            Column4Parameters.HeaderText = "Parameters";
            Column4Parameters.Name = "Column4Parameters";
            Column4Parameters.ReadOnly = true;
            Column4Parameters.Width = 629;
            // 
            // checkBoxScriptShowHelp
            // 
            checkBoxScriptShowHelp.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            checkBoxScriptShowHelp.AutoSize = true;
            checkBoxScriptShowHelp.Checked = true;
            checkBoxScriptShowHelp.CheckState = CheckState.Checked;
            checkBoxScriptShowHelp.Location = new Point(12, 530);
            checkBoxScriptShowHelp.Name = "checkBoxScriptShowHelp";
            checkBoxScriptShowHelp.Size = new Size(83, 19);
            checkBoxScriptShowHelp.TabIndex = 10;
            checkBoxScriptShowHelp.Text = "Show Help";
            checkBoxScriptShowHelp.UseVisualStyleBackColor = true;
            checkBoxScriptShowHelp.CheckedChanged += CheckBoxScriptShowHelp_CheckedChanged;
            // 
            // labelScripConstructorJobAdded
            // 
            labelScripConstructorJobAdded.AutoSize = true;
            labelScripConstructorJobAdded.ForeColor = Color.Green;
            labelScripConstructorJobAdded.Location = new Point(200, 531);
            labelScripConstructorJobAdded.Name = "labelScripConstructorJobAdded";
            labelScripConstructorJobAdded.Size = new Size(61, 15);
            labelScripConstructorJobAdded.TabIndex = 13;
            labelScripConstructorJobAdded.Text = "Job added";
            labelScripConstructorJobAdded.Visible = false;
            // 
            // buttonAddJobToJobListScript
            // 
            buttonAddJobToJobListScript.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonAddJobToJobListScript.Location = new Point(267, 527);
            buttonAddJobToJobListScript.Name = "buttonAddJobToJobListScript";
            buttonAddJobToJobListScript.Size = new Size(95, 23);
            buttonAddJobToJobListScript.TabIndex = 11;
            buttonAddJobToJobListScript.Text = "Add to Job List";
            buttonAddJobToJobListScript.UseVisualStyleBackColor = true;
            buttonAddJobToJobListScript.Click += ButtonAddJobToJobListScript_Click;
            // 
            // buttonCloseScriptConstructorForm
            // 
            buttonCloseScriptConstructorForm.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonCloseScriptConstructorForm.Location = new Point(368, 527);
            buttonCloseScriptConstructorForm.Name = "buttonCloseScriptConstructorForm";
            buttonCloseScriptConstructorForm.Size = new Size(55, 23);
            buttonCloseScriptConstructorForm.TabIndex = 12;
            buttonCloseScriptConstructorForm.Text = "Close";
            buttonCloseScriptConstructorForm.UseVisualStyleBackColor = true;
            buttonCloseScriptConstructorForm.Click += ButtonCloseScriptConstructorForm_Click;
            // 
            // richTextBoxScriptHelp
            // 
            richTextBoxScriptHelp.Dock = DockStyle.Fill;
            richTextBoxScriptHelp.Location = new Point(3, 12);
            richTextBoxScriptHelp.Name = "richTextBoxScriptHelp";
            richTextBoxScriptHelp.ReadOnly = true;
            richTextBoxScriptHelp.Size = new Size(330, 537);
            richTextBoxScriptHelp.TabIndex = 0;
            richTextBoxScriptHelp.Text = "";
            // 
            // ScriptConstructorForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(784, 561);
            Controls.Add(splitContainerScriptConstructor);
            DoubleBuffered = true;
            MinimumSize = new Size(300, 432);
            Name = "ScriptConstructorForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Script Constructor";
            Shown += ScriptConstructorForm_Shown;
            MouseDown += ScriptConstructorForm_MouseDown;
            splitContainerScriptConstructor.Panel1.ResumeLayout(false);
            splitContainerScriptConstructor.Panel1.PerformLayout();
            splitContainerScriptConstructor.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainerScriptConstructor).EndInit();
            splitContainerScriptConstructor.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dataGridViewPreviewJobsListMadeByScript).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private SplitContainer splitContainerScriptConstructor;
        private Label labelTypeYourScript;
        private RadioButton radioButtonScriptEncode;
        private ComboBox comboBoxScriptEncode;
        private Button buttonClearScriptEncode;
        private RadioButton radioButtonScriptDecode;
        private ComboBox comboBoxScriptDecode;
        private Button buttonClearScriptDecode;
        private Label labelPreviewJobsListMadeByScript;
        private DataGridViewEx dataGridViewPreviewJobsListMadeByScript;
        private DataGridViewCheckBoxColumn Column1CheckBox;
        private DataGridViewTextBoxColumn Column2JobType;
        private DataGridViewTextBoxColumn Column3Passes;
        private DataGridViewTextBoxColumn Column4Parameters;
        private CheckBox checkBoxScriptShowHelp;
        private Label labelScripConstructorJobAdded;
        private Button buttonAddJobToJobListScript;
        private Button buttonCloseScriptConstructorForm;
        private RichTextBox richTextBoxScriptHelp;
    }
}