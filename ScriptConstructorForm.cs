using DocumentFormat.OpenXml.Bibliography;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace FLAC_Benchmark_H
{
    public struct ScriptJobData(bool isChecked, string jobType, string passes, string parameters)
    {
        public bool IsChecked { get; } = isChecked;
        public string JobType { get; } = jobType;
        public string Passes { get; } = passes;
        public string Parameters { get; } = parameters;
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

            _debounceTimer = new System.Windows.Forms.Timer();
            _debounceTimer.Interval = 300; // 300 milliseconds delay
            _debounceTimer.Tick += (s, e) =>
            {
                _debounceTimer.Stop(); // Stop the timer
                PreviewJobs(); // Perform the actual parsing
            };

        }
        private System.Windows.Forms.Timer _debounceTimer;

        /// <summary>
        /// Called when the form is fully displayed.
        /// Ensures help text is visible, preview is updated, and input field has focus.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string InitialScriptText { get; set; }

        private void ScriptConstructorForm_Shown(object? sender, EventArgs e)
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

        private void LoadHelpText()
        {
            richTextBoxScriptHelp.Clear();
            richTextBoxScriptHelp.SuspendLayout(); // Suspend layout updates

            try
            {
                // Introduction
                AppendTextWithStyle(richTextBoxScriptHelp,
                    "This tool lets you add a single script to the Job List that will automatically expand into many individual jobs when executed.\n" +
                    "Instead of adding each configuration manually, define a compact template and let the system generate all combinations for you.\n\n" +
                    "The constructor supports both numeric ranges and explicit text values.\n\n",
                    FontStyle.Regular, Color.Black);

                // Warnings
                AppendTextWithStyle(richTextBoxScriptHelp,
                    "⚠️ Use square brackets [ ] for scripts. FLAC command line does not use them.\n" +
                    "⚠️ Only NUMERIC values inside [] are iterated for ranges (e.g., [1..4]).\n" +
                    "⚠️ Text values are processed as-is (e.g., [fast, best]).\n\n",
                    FontStyle.Regular, Color.OrangeRed);

                // Syntax Guide Title
                AppendTextWithStyle(richTextBoxScriptHelp, "Syntax Guide:\n\n", FontStyle.Bold, Color.Black);

                // Syntax Section 1: Basic Range
                AppendTextWithStyle(richTextBoxScriptHelp, "1. Basic Range\n", FontStyle.Bold, Color.Black);
                AppendTextWithStyle(richTextBoxScriptHelp,
                    "   [min..max] generates consecutive numeric values (step = 1 by default).\n\n",
                    FontStyle.Regular, Color.Black);
                AppendTextWithStyle(richTextBoxScriptHelp, "   Example:\n", FontStyle.Regular, Color.Black);
                AppendTextWithStyle(richTextBoxScriptHelp,
                    "     -j[4..8]\n" +
                    "   Expands to:\n" +
                    "     -j4\n" +
                    "     -j5\n" +
                    "     -j6\n" +
                    "     -j7\n" +
                    "     -j8\n\n",
                    FontStyle.Regular, Color.Black);

                // Syntax Section 2: Range with Step
                AppendTextWithStyle(richTextBoxScriptHelp, "2. Range with Step\n", FontStyle.Bold, Color.Black);
                AppendTextWithStyle(richTextBoxScriptHelp,
                    "   [min..max..step] defines a custom numeric increment (positive or negative).\n\n",
                    FontStyle.Regular, Color.Black);
                AppendTextWithStyle(richTextBoxScriptHelp, "   Example:\n", FontStyle.Regular, Color.Black);
                AppendTextWithStyle(richTextBoxScriptHelp,
                    "     -j[2..8..2]\n" +
                    "   Expands to:\n" +
                    "     -j2\n" +
                    "     -j4\n" +
                    "     -j6\n" +
                    "     -j8\n\n",
                    FontStyle.Regular, Color.Black);

                // Syntax Section 3: Explicit Values
                AppendTextWithStyle(richTextBoxScriptHelp, "3. Explicit Values\n", FontStyle.Bold, Color.Black);
                AppendTextWithStyle(richTextBoxScriptHelp,
                    "   [value1, value2, value3] lists individual values (numbers or text).\n" +
                    "   An empty value (e.g., [value,]) creates a version with and without 'value'.\n\n",
                    FontStyle.Regular, Color.Black);
                AppendTextWithStyle(richTextBoxScriptHelp, "   Example #1:\n", FontStyle.Regular, Color.Black);
                AppendTextWithStyle(richTextBoxScriptHelp,
                    "     -j[2, 4, 10, 16]\n" +
                    "   Expands to:\n" +
                    "     -j2\n" +
                    "     -j4\n" +
                    "     -j10\n" +
                    "     -j16\n\n",
                    FontStyle.Regular, Color.Black);
                AppendTextWithStyle(richTextBoxScriptHelp, "   Example #2:\n", FontStyle.Regular, Color.Black);
                AppendTextWithStyle(richTextBoxScriptHelp,
                    "   Text values:\n" +
                    "     --[fast, best]\n" +
                    "   Expands to:\n" +
                    "     --fast\n" +
                    "     --best\n\n",
                    FontStyle.Regular, Color.Black);
                AppendTextWithStyle(richTextBoxScriptHelp, "   Example #3:\n", FontStyle.Regular, Color.Black);
                AppendTextWithStyle(richTextBoxScriptHelp,
                    "   Make a flag optional:\n" +
                    "     -8 [-e,]\n" +
                    "   Expands to:\n" +
                    "     -8 -e\n" +
                    "     -8\n\n",
                    FontStyle.Regular, Color.Black);

                // Syntax Section 4: Complex Expression
                AppendTextWithStyle(richTextBoxScriptHelp, "4. Complex Expression\n", FontStyle.Bold, Color.Black);
                AppendTextWithStyle(richTextBoxScriptHelp,
                    "   Combine ranges, steps, and explicit values in any order.\n" +
                    "   Each [...] block is expanded independently.\n\n",
                    FontStyle.Regular, Color.Black);
                AppendTextWithStyle(richTextBoxScriptHelp, "   Example:\n", FontStyle.Regular, Color.Black);
                AppendTextWithStyle(richTextBoxScriptHelp,
                    "     --[fast, best] -j[2..4, 8..12..2, 16]\n" +
                    "   Expands to:\n" +
                    "     --fast -j2\n" +
                    "     --fast -j3\n" +
                    "     --fast -j4\n" +
                    "     --fast -j8\n" +
                    "     --fast -j10\n" +
                    "     --fast -j12\n" +
                    "     --fast -j16\n" +
                    "     --best -j2\n" +
                    "     --best -j3\n" +
                    "     --best -j4\n" +
                    "     --best -j8\n" +
                    "     --best -j10\n" +
                    "     --best -j12\n" +
                    "     --best -j16\n\n",
                    FontStyle.Regular, Color.Black);

                // Notes
                AppendTextWithStyle(richTextBoxScriptHelp, "Notes:\n", FontStyle.Bold, Color.Gray);
                AppendTextWithStyle(richTextBoxScriptHelp,
                    "• The resulting Script will appear as a single Job in the main window's \"Job List\" panel.\n" +
                    "• Use cautiously: large ranges or many combined options may generate many jobs.\n" +
                    "• You can preview how many Jobs are generated by your Script before adding it.\n" +
                    "• Decimal separator is period '.' (e.g., 0.5, not 0,5).\n" +
                    "• Negative integers are supported in ranges: [-2..2], [-1.0..1.0..0.5].\n",
                    FontStyle.Regular, Color.Black);
            }
            finally
            {
                richTextBoxScriptHelp.ResumeLayout(); // Resume layout updates
            }
        }
        /// <summary>
        /// Appends text to the RichTextBox with specified font style and color.
        /// </summary>
        /// <param name="rtb">The RichTextBox to append to.</param>
        /// <param name="text">The text to append.</param>
        /// <param name="fontStyle">The font style for the text.</param>
        /// <param name="color">The color for the text.</param>
        private static void AppendTextWithStyle(RichTextBox rtb, string text, FontStyle fontStyle, Color color)
        {
            rtb.SelectionFont = new Font(rtb.Font, fontStyle);
            rtb.SelectionColor = color;
            rtb.AppendText(text);
        }

        private void PreviewJobs()
        {
            // Clear DataGridView rows for preview
            dataGridViewPreviewJobsListMadeByScript.Rows.Clear();

            string? scriptLine = comboBoxScript.Text?.Trim();
            if (string.IsNullOrWhiteSpace(scriptLine))
            {
                labelPreviewJobsListMadeByScript.Text = "Preview Job List (0 items)";
                labelPreviewJobsListMadeByScript.ForeColor = SystemColors.ControlText; // Default color
                buttonAddJobToJobListScript.Enabled = false;
                return;
            }

            // --- NEW: Basic syntax checks for common errors ---

            // 1. Check for balanced brackets
            if (scriptLine.Count(c => c == '[') != scriptLine.Count(c => c == ']'))
            {
                labelPreviewJobsListMadeByScript.Text = "Error: Unbalanced brackets";
                labelPreviewJobsListMadeByScript.ForeColor = Color.Red;
                buttonAddJobToJobListScript.Enabled = false;
                return;
            }

            // 2. Check for invalid ellipsis (three dots)
            if (scriptLine.Contains("..."))
            {
                labelPreviewJobsListMadeByScript.Text = "Error: Invalid syntax. Use '..' for ranges.";
                labelPreviewJobsListMadeByScript.ForeColor = Color.Red;
                buttonAddJobToJobListScript.Enabled = false;
                return;
            }

            // 3. Check for incomplete range (e.g., [1.. or ..5])
            if (scriptLine.Contains("..") && (scriptLine.EndsWith("..") || scriptLine.Contains("[..")))
            {
                labelPreviewJobsListMadeByScript.Text = "Error: Incomplete range";
                labelPreviewJobsListMadeByScript.ForeColor = Color.Red;
                buttonAddJobToJobListScript.Enabled = false;
                return;
            }

            // --- NEW: Notify user that parsing has started ---
            labelPreviewJobsListMadeByScript.Text = "Parsing script...                             ";
            labelPreviewJobsListMadeByScript.ForeColor = Color.Blue;
            labelPreviewJobsListMadeByScript.Refresh(); // Force UI update before heavy operation

            // --- Attempt to expand the script ---
            var expanded = ScriptParser.ExpandScriptLine(scriptLine);

            if (expanded.Count == 0)
            {
                // Parser returned empty list, likely due to an error not caught by basic checks
                labelPreviewJobsListMadeByScript.Text = "Error: Invalid script syntax";
                labelPreviewJobsListMadeByScript.ForeColor = Color.Red;
                buttonAddJobToJobListScript.Enabled = false;
            }
            else
            {
                // Success: display the preview
                foreach (string param in expanded)
                {
                    // Add row to DataGridView for preview
                    dataGridViewPreviewJobsListMadeByScript.Rows.Add(true, "Encode", "1", param); // Checkbox (true), Job Type, Passes, Parameters
                }
                labelPreviewJobsListMadeByScript.Text = $"Preview Job List ({expanded.Count} items)";
                labelPreviewJobsListMadeByScript.ForeColor = SystemColors.ControlText; // Default color
                buttonAddJobToJobListScript.Enabled = true; // Enable the button only if parsing was successful
            }

            // Always clear selection after update
            dataGridViewPreviewJobsListMadeByScript.ClearSelection();
            dataGridViewPreviewJobsListMadeByScript.CurrentCell = null;
        }
        private void ComboBoxScript_TextChanged(object? sender, EventArgs e)
        {
            _debounceTimer.Stop();
            _debounceTimer.Start();
        }
        private void ButtonAddJobToJobListScript_Click(object? sender, EventArgs e)
        {
            string scriptText = comboBoxScript.Text.Trim();
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
        private void ScriptConstructorForm_MouseDown(object? sender, MouseEventArgs e)
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
        private void dataGridViewPreviewJobsListMadeByScript_MouseDown(object? sender, MouseEventArgs e)
        {
            var hitTest = dataGridViewPreviewJobsListMadeByScript.HitTest(e.X, e.Y);
            if (hitTest.RowIndex == -1 && hitTest.ColumnIndex == -1)
            {
                dataGridViewPreviewJobsListMadeByScript.ClearSelection();
            }
        }
        private void dataGridViewPreviewJobsListMadeByScript_KeyDown(object? sender, KeyEventArgs e)
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

        private void buttonCloseScriptConstructorForm_Click(object? sender, EventArgs e)
        {
            Close();
        }
    }
}