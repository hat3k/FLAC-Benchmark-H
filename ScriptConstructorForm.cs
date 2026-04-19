using System.Text;

namespace FLAC_Benchmark_H
{
    public readonly struct ScriptJobData(bool isChecked, string jobType, string passes, string parameters)
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
            LoadHelpText();

            _debounceTimer = new System.Windows.Forms.Timer
            {
                Interval = 300 // 300 milliseconds delay
            };
            _debounceTimer.Tick += (s, e) =>
            {
                _debounceTimer.Stop(); // Stop the timer
                PreviewJobs(); // Perform the actual parsing
            };

            _jobAddedTimer = new System.Windows.Forms.Timer
            {
                Interval = 3000, // 3 seconds display time
            };
            _jobAddedTimer.Tick += (s, e) =>
            {
                _jobAddedTimer.Stop();
                labelScripConstructorJobAdded.Visible = false;
            };
        }
        private readonly System.Windows.Forms.Timer _debounceTimer;
        private readonly System.Windows.Forms.Timer _jobAddedTimer;

        private const int MaxHistoryItems = 50;
        private void ScriptConstructorForm_Load(object? sender, EventArgs e)
        {
            if (Owner is Form1 mainForm)
            {
                // Read saved setting from Form1 and apply to checkbox
                checkBoxScriptShowHelp.Checked = mainForm.scriptShowHelp;
                splitContainerScriptConstructor.Panel2Collapsed = !mainForm.scriptShowHelp;

                // Load last state into both ComboBoxes
                comboBoxScriptEncode.Text = mainForm.lastEncodeScript;
                comboBoxScriptDecode.Text = mainForm.lastDecodeScript;

                // Load history into both ComboBoxes
                comboBoxScriptEncode.Items.Clear();
                comboBoxScriptEncode.Items.AddRange([.. mainForm.scriptEncodeHistory]);
                comboBoxScriptDecode.Items.Clear();
                comboBoxScriptDecode.Items.AddRange([.. mainForm.scriptDecodeHistory]);
            }
        }
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
            _debounceTimer.Stop();
            _debounceTimer.Start();

            // 3. Focus the ACTIVE ComboBox (Encode or Decode)
            ComboBox activeBox = radioButtonScriptEncode.Checked ? comboBoxScriptEncode : comboBoxScriptDecode;
            activeBox.SelectionStart = activeBox.Text.Length;
            activeBox.SelectionLength = 0;
            activeBox.Select();
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

        public void SetInitialScriptData(string jobType, string? parameters)
        {
            ComboBox targetComboBoxScript;

            if (jobType.Equals("Encode", StringComparison.OrdinalIgnoreCase))
            {
                radioButtonScriptEncode.Checked = true;
                targetComboBoxScript = comboBoxScriptEncode;
            }
            else
            {
                radioButtonScriptDecode.Checked = true;
                targetComboBoxScript = comboBoxScriptDecode;
            }

            targetComboBoxScript.Text = parameters ?? string.Empty;

            void setCursor()
            {
                targetComboBoxScript.SelectionStart = targetComboBoxScript.Text.Length;
                targetComboBoxScript.SelectionLength = 0;
                targetComboBoxScript.Select();
            }

            if (IsHandleCreated)
            {
                _ = BeginInvoke(setCursor);
            }
            else
            {
                setCursor();
            }
        }

        private void PreviewJobs()
        {
            // Clear DataGridView rows for preview
            dataGridViewPreviewJobsListMadeByScript.Rows.Clear();

            // Get active script
            string? scriptLine;
            string jobType;

            if (radioButtonScriptEncode.Checked)
            {
                scriptLine = comboBoxScriptEncode.Text?.Trim();
                jobType = "Encode";
            }
            else
            {
                scriptLine = comboBoxScriptDecode.Text?.Trim();
                jobType = "Decode";
            }

            if (string.IsNullOrWhiteSpace(scriptLine))
            {
                labelPreviewJobsListMadeByScript.Text = "Preview Job List (0 items)";
                labelPreviewJobsListMadeByScript.ForeColor = SystemColors.ControlText; // Default color
                buttonAddJobToJobListScript.Enabled = false;
                return;
            }

            // Basic syntax checks for common errors

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

            // Notify user that parsing has started
            labelPreviewJobsListMadeByScript.Text = "Parsing script...";
            labelPreviewJobsListMadeByScript.ForeColor = Color.Blue;
            labelPreviewJobsListMadeByScript.Refresh(); // Force UI update before heavy operation

            // Attempt to expand the script
            List<string> expanded = ScriptParser.ExpandScriptLine(scriptLine);

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
                    _ = dataGridViewPreviewJobsListMadeByScript.Rows.Add(true, jobType, "1", param); // Checkbox (true), Job Type, Passes, Parameters
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
            string scriptText;
            string jobType;

            if (radioButtonScriptEncode.Checked)
            {
                scriptText = comboBoxScriptEncode.Text.Trim();
                jobType = "Encode";
            }
            else
            {
                scriptText = comboBoxScriptDecode.Text.Trim();
                jobType = "Decode";
            }

            if (string.IsNullOrWhiteSpace(scriptText))
            {
                _ = MessageBox.Show("Script is empty.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            List<string> expanded = ScriptParser.ExpandScriptLine(scriptText);
            if (expanded.Count == 0)
            {
                _ = MessageBox.Show(
                    "Script produced no valid jobs. Check syntax (e.g. [min..max]).",
                    "Invalid Script",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            SaveBothScriptsToHistory();

            List<ScriptJobData> jobsToAdd =
            [
                new(true, jobType, "1", scriptText) // Pass the script text as the 'parameter'
            ];

            OnJobsAdded?.Invoke(jobsToAdd);

            // Show or refresh "Job added" label with auto-hide timer
            labelScripConstructorJobAdded.Visible = false;
            _jobAddedTimer.Stop();
            labelScripConstructorJobAdded.Visible = true;
            _jobAddedTimer.Start();
        }

        public event Action<List<ScriptJobData>> OnJobsAdded = delegate { };

        private void DataGridViewPreviewJobsListMadeByScript_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            // Check if it's the 'Job Type' column (assuming it's the second column, index 1)
            if (e.ColumnIndex == 1 && e.Value is string cellValue)
            {
                if (cellValue.Equals("Encode", StringComparison.OrdinalIgnoreCase))
                {
                    e.CellStyle.ForeColor = Color.Green;
                    e.FormattingApplied = true;
                }
                else if (cellValue.Equals("Decode", StringComparison.OrdinalIgnoreCase))
                {
                    e.CellStyle.ForeColor = Color.Red;
                    e.FormattingApplied = true;
                }
            }
        }
        private void ScriptConstructorForm_MouseDown(object? sender, MouseEventArgs e)
        {
            // Get the location of the mouse click relative to the DataGridView
            Point clickPoint = dataGridViewPreviewJobsListMadeByScript.PointToClient(PointToScreen(e.Location));

            // Check if the click point is outside the bounds of the DataGridView
            if (!dataGridViewPreviewJobsListMadeByScript.ClientRectangle.Contains(clickPoint))
            {
                // Clear selection if clicked outside the DataGridView
                dataGridViewPreviewJobsListMadeByScript.ClearSelection();
                dataGridViewPreviewJobsListMadeByScript.CurrentCell = null;
            }
        }
        private void DataGridViewPreviewJobsListMadeByScript_MouseDown(object? sender, MouseEventArgs e)
        {
            DataGridView.HitTestInfo hitTest = dataGridViewPreviewJobsListMadeByScript.HitTest(e.X, e.Y);
            if (hitTest.RowIndex == -1 && hitTest.ColumnIndex == -1)
            {
                dataGridViewPreviewJobsListMadeByScript.ClearSelection();
            }
        }
        private void DataGridViewPreviewJobsListMadeByScript_KeyDown(object? sender, KeyEventArgs e)
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
            StringBuilder jobsText = new();

            if (dataGridViewPreviewJobsListMadeByScript.SelectedRows.Count > 0)
            {
                // --- LOGIC FOR SELECTED ROWS ---
                // Get the indices of the selected rows / Sort indices in ascending order (top -> down)
                List<int> selectedIndices = [.. dataGridViewPreviewJobsListMadeByScript.SelectedRows.Cast<DataGridViewRow>()
                                                                 .Select(row => row.Index)
                                                                 .OrderBy(index => index)];

                // Iterate through rows in the order of their ascending index
                foreach (int index in selectedIndices)
                {
                    // Verify the index is valid (just in case)
                    if (index >= 0 && index < dataGridViewPreviewJobsListMadeByScript.Rows.Count)
                    {
                        DataGridViewRow row = dataGridViewPreviewJobsListMadeByScript.Rows[index];

                        // Get values from the respective cells
                        // Assuming columns are: Column1CheckBox (0), Column2JobType (1), Column3Passes (2), Column4Parameters (3)
                        bool isChecked = Convert.ToBoolean(row.Cells["Column1CheckBox"].Value);
                        string type = row.Cells["Column2JobType"].Value?.ToString() ?? "";
                        string passes = row.Cells["Column3Passes"].Value?.ToString() ?? "";
                        string parameters = row.Cells["Column4Parameters"].Value?.ToString() ?? "";

                        // Format the row data as a single line
                        string status = isChecked ? "Checked" : "Unchecked";
                        _ = jobsText.AppendLine($"{status}|{type}|{passes}|{parameters}");
                    }
                }
            }
            else
            {
                // --- LOGIC FOR ALL ROWS (when nothing is selected) ---
                // Iterate through all rows (excluding the potential new row)
                foreach (DataGridViewRow row in dataGridViewPreviewJobsListMadeByScript.Rows)
                {
                    if (row.IsNewRow)
                    {
                        continue; // Skip the new row
                    }

                    // Get values from the respective cells
                    bool isChecked = Convert.ToBoolean(row.Cells["Column1CheckBox"].Value);
                    string type = row.Cells["Column2JobType"].Value?.ToString() ?? "";
                    string passes = row.Cells["Column3Passes"].Value?.ToString() ?? "";
                    string parameters = row.Cells["Column4Parameters"].Value?.ToString() ?? "";

                    // Format the row data as a single line
                    string status = isChecked ? "Checked" : "Unchecked";
                    _ = jobsText.AppendLine($"{status}|{type}|{passes}|{parameters}");
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
                _ = MessageBox.Show("No jobs to copy.", "Information",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void SaveBothScriptsCurrentState()
        {
            if (Owner is Form1 mainForm)
            {
                mainForm.lastEncodeScript = comboBoxScriptEncode.Text?.Trim() ?? string.Empty;
                mainForm.lastDecodeScript = comboBoxScriptDecode.Text?.Trim() ?? string.Empty;
            }
        }
        private void SaveBothScriptsToHistory()
        {
            if (Owner is Form1 mainForm)
            {
                // Encode history
                string encodeScript = comboBoxScriptEncode.Text?.Trim() ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(encodeScript))
                {
                    _ = mainForm.scriptEncodeHistory.RemoveAll(item => item == encodeScript);
                    mainForm.scriptEncodeHistory.Insert(0, encodeScript);

                    if (mainForm.scriptEncodeHistory.Count > MaxHistoryItems)
                    {
                        mainForm.scriptEncodeHistory.RemoveRange(MaxHistoryItems, mainForm.scriptEncodeHistory.Count - MaxHistoryItems);
                    }
                }

                // Decode history
                string decodeScript = comboBoxScriptDecode.Text?.Trim() ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(decodeScript))
                {
                    _ = mainForm.scriptDecodeHistory.RemoveAll(item => item == decodeScript);
                    mainForm.scriptDecodeHistory.Insert(0, decodeScript);

                    if (mainForm.scriptDecodeHistory.Count > MaxHistoryItems)
                    {
                        mainForm.scriptDecodeHistory.RemoveRange(MaxHistoryItems, mainForm.scriptDecodeHistory.Count - MaxHistoryItems);
                    }
                }

                // Update both comboboxes history (causes flickering)
                comboBoxScriptEncode.Items.Clear();
                comboBoxScriptEncode.Items.AddRange([.. mainForm.scriptEncodeHistory]);
                comboBoxScriptDecode.Items.Clear();
                comboBoxScriptDecode.Items.AddRange([.. mainForm.scriptDecodeHistory]);
            }
        }

        private void RadioButtonScript_CheckedChanged(object sender, EventArgs e)
        {
            comboBoxScriptEncode.Enabled = radioButtonScriptEncode.Checked;
            buttonClearScriptEncode.Enabled = radioButtonScriptEncode.Checked;

            comboBoxScriptDecode.Enabled = radioButtonScriptDecode.Checked;
            buttonClearScriptDecode.Enabled = radioButtonScriptDecode.Checked;

            ComboBox activeComboBoxScript = radioButtonScriptEncode.Checked ? comboBoxScriptEncode : comboBoxScriptDecode;
            ComboBox inactiveComboBoxScript = radioButtonScriptEncode.Checked ? comboBoxScriptDecode : comboBoxScriptEncode;

            inactiveComboBoxScript.SelectionStart = 0;
            inactiveComboBoxScript.SelectionLength = 0;
            activeComboBoxScript.SelectionStart = activeComboBoxScript.Text.Length;
            activeComboBoxScript.SelectionLength = 0;
            activeComboBoxScript.Select();

            _debounceTimer.Stop();
            _debounceTimer.Start();
        }
        private void CheckBoxScriptShowHelp_CheckedChanged(object sender, EventArgs e)
        {
            splitContainerScriptConstructor.Panel2Collapsed = !checkBoxScriptShowHelp.Checked;

            // Update Form1's setting immediately when user toggles
            if (Owner is Form1 mainForm)
            {
                mainForm.scriptShowHelp = checkBoxScriptShowHelp.Checked;
            }
        }

        private void ButtonClearScriptComboBox_Click(object sender, EventArgs e)
        {
            if (sender == buttonClearScriptEncode)
            {
                comboBoxScriptEncode.Text = string.Empty;
            }
            else if (sender == buttonClearScriptDecode)
            {
                comboBoxScriptDecode.Text = string.Empty;
            }
        }
        private void ButtonCloseScriptConstructorForm_Click(object? sender, EventArgs e)
        {
            Close();
        }
        private void ScriptConstructorForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            SaveBothScriptsCurrentState();
            SaveBothScriptsToHistory();
        }
    }
}