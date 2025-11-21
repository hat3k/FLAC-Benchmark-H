using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Text;

namespace FLAC_Benchmark_H
{
    /// <summary>
    /// Represents the data for a single script job to be added to the main job list.
    /// </summary>
    public struct ScriptJobData
    {
        public bool IsChecked { get; }
        public string JobType { get; }
        public string Passes { get; }
        public string Parameters { get; }

        public ScriptJobData(bool isChecked, string jobType, string passes, string parameters)
        {
            IsChecked = isChecked;
            JobType = jobType;
            Passes = passes;
            Parameters = parameters;
        }
    }

    /// <summary>
    /// Form for creating parameter scripts like "-j[1..4]" that expand into multiple jobs.
    /// Automatically previews expanded jobs in real time.
    /// </summary>
    public partial class ScriptConstructorForm : Form
    {
        public ScriptConstructorForm()
        {
            InitializeComponent();
            // Use Shown event for reliable focus and layout setup
            this.Shown += ScriptConstructorForm_Shown;
            this.MouseDown += ScriptConstructorForm_MouseDown;

        }

        /// <summary>
        /// Called when the form is fully displayed.
        /// Ensures help text is visible, preview is updated, and input field has focus.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string InitialScriptText { get; set; }

        private void ScriptConstructorForm_Shown(object sender, EventArgs e)
        {
            // 1. Scroll help text to the very top
            if (richTextBoxScriptHelp.TextLength > 0)
            {
                richTextBoxScriptHelp.SelectionStart = 0;
                richTextBoxScriptHelp.SelectionLength = 0;
                richTextBoxScriptHelp.ScrollToCaret();
            }

            // 2. Update preview based on current script text
            PreviewJobs();

            // 3. Set focus to the input combo box
            comboBoxScript.Select();

            if (!string.IsNullOrEmpty(InitialScriptText))
            {
                comboBoxScript.Text = InitialScriptText;
                comboBoxScript.SelectionStart = comboBoxScript.Text.Length;
                comboBoxScript.SelectionLength = 0;
            }

            // 4. Position cursor at the end of existing text
            comboBoxScript.SelectionStart = comboBoxScript.Text.Length;
            comboBoxScript.SelectionLength = 0;
        }

        /// <summary>
        /// Loads formatted help text explaining script syntax and rules.
        /// Includes examples and warnings about invalid formats.
        /// </summary>
        private void LoadHelpText()
        {
            richTextBoxScriptHelp.Clear();
            richTextBoxScriptHelp.SuspendLayout(); // Suspend layout updates

            try
            {
                // Introduction
                richTextBoxScriptHelp.SelectionFont = new Font(richTextBoxScriptHelp.Font, FontStyle.Regular);
                richTextBoxScriptHelp.SelectionColor = Color.Black;
                richTextBoxScriptHelp.AppendText("This tool allows you to define a complete job list using a compact script format.\n");
                richTextBoxScriptHelp.AppendText("Instead of adding jobs one by one, you can write parameter templates that will be expanded into multiple test configurations when executed.\n\n");

                // Warnings
                richTextBoxScriptHelp.SelectionColor = Color.OrangeRed;
                richTextBoxScriptHelp.AppendText("⚠️ Only NUMERIC values inside [] are iterated (integers or decimals).\n\n");

                // Syntax Guide
                richTextBoxScriptHelp.SelectionColor = Color.Black;
                richTextBoxScriptHelp.SelectionFont = new Font(richTextBoxScriptHelp.Font, FontStyle.Bold);
                richTextBoxScriptHelp.AppendText("Syntax Guide:\n\n");

                // 1. Basic Range
                richTextBoxScriptHelp.SelectionFont = new Font(richTextBoxScriptHelp.Font, FontStyle.Bold);
                richTextBoxScriptHelp.AppendText("1. Basic Range\n");
                richTextBoxScriptHelp.SelectionFont = new Font(richTextBoxScriptHelp.Font, FontStyle.Regular);
                richTextBoxScriptHelp.AppendText("   [min..max] generates consecutive values (step = 1 by default).\n");
                richTextBoxScriptHelp.AppendText("   Example:\n");
                richTextBoxScriptHelp.AppendText("     -j[4..8]\n");
                richTextBoxScriptHelp.AppendText("   Expands to:\n");
                richTextBoxScriptHelp.SelectionFont = new Font(richTextBoxScriptHelp.Font, FontStyle.Italic);
                richTextBoxScriptHelp.AppendText("     -j4\n");
                richTextBoxScriptHelp.AppendText("     -j5\n");
                richTextBoxScriptHelp.AppendText("     -j6\n");
                richTextBoxScriptHelp.AppendText("     -j7\n");
                richTextBoxScriptHelp.AppendText("     -j8\n\n");
                richTextBoxScriptHelp.SelectionFont = new Font(richTextBoxScriptHelp.Font, FontStyle.Regular);

                // 2. Range with Step
                richTextBoxScriptHelp.SelectionFont = new Font(richTextBoxScriptHelp.Font, FontStyle.Bold);
                richTextBoxScriptHelp.AppendText("2. Range with Step\n");
                richTextBoxScriptHelp.SelectionFont = new Font(richTextBoxScriptHelp.Font, FontStyle.Regular);
                richTextBoxScriptHelp.AppendText("   [min..max..step] defines custom increment (positive or negative).\n");
                richTextBoxScriptHelp.AppendText("   Example:\n");
                richTextBoxScriptHelp.AppendText("     -j[2..8..2]\n");
                richTextBoxScriptHelp.AppendText("   Expands to:\n");
                richTextBoxScriptHelp.SelectionFont = new Font(richTextBoxScriptHelp.Font, FontStyle.Italic);
                richTextBoxScriptHelp.AppendText("     -j2\n");
                richTextBoxScriptHelp.AppendText("     -j4\n");
                richTextBoxScriptHelp.AppendText("     -j6\n");
                richTextBoxScriptHelp.AppendText("     -j8\n\n");
                richTextBoxScriptHelp.SelectionFont = new Font(richTextBoxScriptHelp.Font, FontStyle.Regular);

                // 3. Explicit Values
                richTextBoxScriptHelp.SelectionFont = new Font(richTextBoxScriptHelp.Font, FontStyle.Bold);
                richTextBoxScriptHelp.AppendText("3. Explicit Values\n");
                richTextBoxScriptHelp.SelectionFont = new Font(richTextBoxScriptHelp.Font, FontStyle.Regular);
                richTextBoxScriptHelp.AppendText("   [value1, value2, value3] lists individual values.\n");
                richTextBoxScriptHelp.AppendText("   Example:\n");
                richTextBoxScriptHelp.AppendText("     -j[2, 4, 10, 16]\n");
                richTextBoxScriptHelp.AppendText("   Expands to:\n");
                richTextBoxScriptHelp.SelectionFont = new Font(richTextBoxScriptHelp.Font, FontStyle.Italic);
                richTextBoxScriptHelp.AppendText("     -j2\n");
                richTextBoxScriptHelp.AppendText("     -j4\n");
                richTextBoxScriptHelp.AppendText("     -j10\n");
                richTextBoxScriptHelp.AppendText("     -j16\n\n");
                richTextBoxScriptHelp.SelectionFont = new Font(richTextBoxScriptHelp.Font, FontStyle.Regular);

                // 4. Complex Expression
                richTextBoxScriptHelp.SelectionFont = new Font(richTextBoxScriptHelp.Font, FontStyle.Bold);
                richTextBoxScriptHelp.AppendText("4. Complex Expression\n");
                richTextBoxScriptHelp.SelectionFont = new Font(richTextBoxScriptHelp.Font, FontStyle.Regular);
                richTextBoxScriptHelp.AppendText("   Combine ranges, steps and values in any order.\n");
                richTextBoxScriptHelp.AppendText("   Example:\n");
                richTextBoxScriptHelp.AppendText("     -j[2..4, 8..12..2, 16]\n");
                richTextBoxScriptHelp.AppendText("   Expands to:\n");
                richTextBoxScriptHelp.SelectionFont = new Font(richTextBoxScriptHelp.Font, FontStyle.Italic);
                richTextBoxScriptHelp.AppendText("     -j2\n");
                richTextBoxScriptHelp.AppendText("     -j3\n");
                richTextBoxScriptHelp.AppendText("     -j4\n");
                richTextBoxScriptHelp.AppendText("     -j8\n");
                richTextBoxScriptHelp.AppendText("     -j10\n");
                richTextBoxScriptHelp.AppendText("     -j12\n");
                richTextBoxScriptHelp.AppendText("     -j16\n\n");
                richTextBoxScriptHelp.SelectionFont = new Font(richTextBoxScriptHelp.Font, FontStyle.Regular);

                // Notes
                richTextBoxScriptHelp.SelectionFont = new Font(richTextBoxScriptHelp.Font, FontStyle.Bold);
                richTextBoxScriptHelp.SelectionColor = Color.Gray;
                richTextBoxScriptHelp.AppendText("Notes:\n");
                richTextBoxScriptHelp.SelectionFont = new Font(richTextBoxScriptHelp.Font, FontStyle.Regular);
                richTextBoxScriptHelp.SelectionColor = Color.Black;
                richTextBoxScriptHelp.AppendText("• The resulting Script will appear as a Job in the main window's \"Job List\" panel.\n");
                richTextBoxScriptHelp.AppendText("• Use cautiously: large ranges may generate many jobs.\n");
                richTextBoxScriptHelp.AppendText("• You can preview how many Jobs are generated by your Script.\n");
                richTextBoxScriptHelp.AppendText("• Decimal separator is period '.' (e.g., 0.5, not 0,5).\n");
                richTextBoxScriptHelp.AppendText("• Negative integers are supported: [-2..2], [-1.0..1.0..0.5]\n");
            }
            finally
            {
                richTextBoxScriptHelp.ResumeLayout(); // Resume layout updates
            }
        }

        private void PreviewJobs()
        {
            // Clear DataGridView rows for preview
            dataGridViewPreviewJobsListMadeByScript.Rows.Clear();

            string scriptLine = comboBoxScript.Text?.Trim();
            if (string.IsNullOrWhiteSpace(scriptLine))
            {
                labelPreviewJobsListMadeByScript.Text = "Preview Job List (0 items)";
                buttonAddJobToJobListScript.Enabled = false;
                return;
            }

            var expanded = ScriptParser.ExpandScriptLine(scriptLine);

            foreach (string param in expanded)
            {
                // Add row to DataGridView for preview
                dataGridViewPreviewJobsListMadeByScript.Rows.Add(true, "Encode", "1", param); // Checkbox (true), Job Type, Passes, Parameters
            }

            labelPreviewJobsListMadeByScript.Text = $"Preview Job List ({expanded.Count} items)";
            buttonAddJobToJobListScript.Enabled = expanded.Count > 0;
            dataGridViewPreviewJobsListMadeByScript.ClearSelection();
            dataGridViewPreviewJobsListMadeByScript.CurrentCell = null;
        }
        private void ComboBoxScript_TextChanged(object sender, EventArgs e)
        {
            PreviewJobs();
        }
        private void ButtonAddJobToJobListScript_Click(object sender, EventArgs e)
        {
            string scriptText = comboBoxScript.Text?.Trim();
            if (string.IsNullOrWhiteSpace(scriptText))
            {
                MessageBox.Show("Script is empty.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var expanded = ScriptParser.ExpandScriptLine(scriptText);
            if (expanded.Count == 0)
            {
                MessageBox.Show(
                    "Script produced no valid jobs. Check syntax (e.g. [min..max]).",
                    "Invalid Script",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            var jobsToAdd = new List<ScriptJobData>
            {
                new ScriptJobData(true, "Encode", "1", scriptText) // Pass the script text as the 'parameter'
            };

            OnJobsAdded?.Invoke(jobsToAdd);
        }

        public event Action<List<ScriptJobData>> OnJobsAdded;

        protected override void OnCreateControl()
        {
            base.OnCreateControl();

            if (!DesignMode)
            {
                LoadHelpText();
                comboBoxScript.TextChanged += ComboBoxScript_TextChanged;
                buttonAddJobToJobListScript.Click += ButtonAddJobToJobListScript_Click;

                // Subscribe to CellFormatting event for the new DataGridView to handle colors
                dataGridViewPreviewJobsListMadeByScript.CellFormatting += dataGridViewPreviewJobsListMadeByScript_CellFormatting;
                dataGridViewPreviewJobsListMadeByScript.MouseDown += dataGridViewPreviewJobsListMadeByScript_MouseDown;
                dataGridViewPreviewJobsListMadeByScript.KeyDown += dataGridViewPreviewJobsListMadeByScript_KeyDown;
            }
        }
        private void dataGridViewPreviewJobsListMadeByScript_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            // Check if it's the 'Job Type' column (assuming it's the second column, index 1)
            if (e.ColumnIndex == 1 && e.Value != null)
            {
                string cellValue = e.Value.ToString();
                if (cellValue != null)
                {
                    if (cellValue.Equals("Encode", StringComparison.OrdinalIgnoreCase))
                    {
                        e.CellStyle.ForeColor = Color.Green;
                    }
                    else if (cellValue.Equals("Decode", StringComparison.OrdinalIgnoreCase))
                    {
                        e.CellStyle.ForeColor = Color.Red;
                    }
                    e.FormattingApplied = true; // Indicate that formatting was applied
                }
            }
        }
        private void ScriptConstructorForm_MouseDown(object sender, MouseEventArgs e)
        {
            // Get the location of the mouse click relative to the DataGridView
            Point clickPoint = dataGridViewPreviewJobsListMadeByScript.PointToClient(this.PointToScreen(e.Location));

            // Check if the click point is outside the bounds of the DataGridView
            if (!dataGridViewPreviewJobsListMadeByScript.ClientRectangle.Contains(clickPoint))
            {
                // Clear selection if clicked outside the DataGridView
                dataGridViewPreviewJobsListMadeByScript.ClearSelection();
                dataGridViewPreviewJobsListMadeByScript.CurrentCell = null;
            }
        }
        private void dataGridViewPreviewJobsListMadeByScript_MouseDown(object sender, MouseEventArgs e)
        {
            var hitTest = dataGridViewPreviewJobsListMadeByScript.HitTest(e.X, e.Y);
            if (hitTest.RowIndex == -1 && hitTest.ColumnIndex == -1)
            {
                dataGridViewPreviewJobsListMadeByScript.ClearSelection();
            }
        }
        private void dataGridViewPreviewJobsListMadeByScript_KeyDown(object sender, KeyEventArgs e)
        {
            // Check if Ctrl and A are pressed simultaneously
            if (e.Control && e.KeyCode == Keys.A)
            {
                e.Handled = true; // Cancel default behavior
                e.SuppressKeyPress = true; // Also suppress the key press to prevent beep

                // Select all rows in dataGridViewPreviewJobsListMadeByScript
                dataGridViewPreviewJobsListMadeByScript.SelectAll(); // This is the standard way to select all rows
            }

            // Handle Ctrl+C (Copy in custom format)
            if (e.Control && e.KeyCode == Keys.C)
            {
                CopyJobsFromPreviewDataGridView();
                e.Handled = true; // Cancel default behavior
                e.SuppressKeyPress = true; // Suppress beep
            }
        }
        private void CopyJobsFromPreviewDataGridView()
        {
            StringBuilder jobsText = new StringBuilder();

            if (dataGridViewPreviewJobsListMadeByScript.SelectedRows.Count > 0)
            {
                // --- LOGIC FOR SELECTED ROWS ---
                // Get the indices of the selected rows
                var selectedIndices = dataGridViewPreviewJobsListMadeByScript.SelectedRows.Cast<DataGridViewRow>()
                                                                 .Select(row => row.Index)
                                                                 .OrderBy(index => index) // Sort indices in ascending order (top -> down)
                                                                 .ToList();

                // Iterate through rows in the order of their ascending index
                foreach (int index in selectedIndices)
                {
                    // Verify the index is valid (just in case)
                    if (index >= 0 && index < dataGridViewPreviewJobsListMadeByScript.Rows.Count)
                    {
                        var row = dataGridViewPreviewJobsListMadeByScript.Rows[index];

                        // Get values from the respective cells
                        // Assuming columns are: Column1CheckBox (0), Column2JobType (1), Column3Passes (2), Column4Parameters (3)
                        bool isChecked = Convert.ToBoolean(row.Cells["Column1CheckBox"].Value);
                        string type = row.Cells["Column2JobType"].Value?.ToString() ?? "";
                        string passes = row.Cells["Column3Passes"].Value?.ToString() ?? "";
                        string parameters = row.Cells["Column4Parameters"].Value?.ToString() ?? "";

                        // Format the row data as a single line
                        string status = isChecked ? "Checked" : "Unchecked";
                        jobsText.AppendLine($"{status}|{type}|{passes}|{parameters}");
                    }
                }
            }
            else
            {
                // --- LOGIC FOR ALL ROWS (when nothing is selected) ---
                // Iterate through all rows (excluding the potential new row)
                foreach (DataGridViewRow row in dataGridViewPreviewJobsListMadeByScript.Rows)
                {
                    if (row.IsNewRow) continue; // Skip the new row

                    // Get values from the respective cells
                    bool isChecked = Convert.ToBoolean(row.Cells["Column1CheckBox"].Value);
                    string type = row.Cells["Column2JobType"].Value?.ToString() ?? "";
                    string passes = row.Cells["Column3Passes"].Value?.ToString() ?? "";
                    string parameters = row.Cells["Column4Parameters"].Value?.ToString() ?? "";

                    // Format the row data as a single line
                    string status = isChecked ? "Checked" : "Unchecked";
                    jobsText.AppendLine($"{status}|{type}|{passes}|{parameters}");
                }
            }

            if (jobsText.Length > 0)
            {
                // Set the formatted text to the clipboard
                Clipboard.SetText(jobsText.ToString());
            }
            else
            {
                // Show a message if no rows were found to copy
                MessageBox.Show("No jobs to copy.", "Information",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void buttonCloseScriptConstructorForm_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}