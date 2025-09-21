using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace FLAC_Benchmark_H
{
    /// <summary>
    /// Form for creating parameter scripts like "-j{1..4}" that expand into multiple jobs.
    /// Automatically previews expanded jobs in real time.
    /// </summary>
    public partial class ScriptConstructorForm : Form
    {
        public ScriptConstructorForm()
        {
            InitializeComponent();
            // Use Shown event for reliable focus and layout setup
            this.Shown += ScriptConstructorForm_Shown;
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
        /// Draws column headers using default system style.
        /// Required when OwnerDraw is enabled.
        /// </summary>
        private void ListViewPreview_DrawColumnHeader(object sender, DrawListViewColumnHeaderEventArgs e)
        {
            e.DrawDefault = true;
        }

        /// <summary>
        /// Custom drawing for subitems: draws checkboxes and color-coded job types.
        /// Colors: Green = Encode, Red = Decode.
        /// </summary>
        private void ListViewPreview_DrawSubItem(object sender, DrawListViewSubItemEventArgs e)
        {
            if (e.ColumnIndex == 0 && e.Item != null) // Job Type column
            {
                e.DrawBackground();

                // Draw checkbox if enabled
                if (listViewPreviewJobsListMadeByScript.CheckBoxes)
                {
                    CheckBoxRenderer.DrawCheckBox(e.Graphics,
                        new Point(e.Bounds.Left + 4, e.Bounds.Top + 2),
                        e.Item.Checked
                            ? System.Windows.Forms.VisualStyles.CheckBoxState.CheckedNormal
                            : System.Windows.Forms.VisualStyles.CheckBoxState.UncheckedNormal);
                }

                // Set text color based on job type
                Color textColor = e.SubItem?.Text.Contains("Encode", StringComparison.OrdinalIgnoreCase) == true
                    ? Color.Green
                    : e.SubItem?.Text.Contains("Decode", StringComparison.OrdinalIgnoreCase) == true
                    ? Color.Red
                    : e.Item.ForeColor;

                using (var brush = new SolidBrush(textColor))
                {
                    Rectangle textBounds = new Rectangle(
                        e.Bounds.Left + (listViewPreviewJobsListMadeByScript.CheckBoxes ? 20 : 0),
                        e.Bounds.Top,
                        e.Bounds.Width - (listViewPreviewJobsListMadeByScript.CheckBoxes ? 20 : 0),
                        e.Bounds.Height);

                    e.Graphics.DrawString(e.SubItem?.Text ?? string.Empty,
                        e.SubItem?.Font ?? e.Item.Font ?? Font,
                        brush, textBounds);
                }

                e.DrawFocusRectangle(e.Bounds);
            }
            else
            {
                e.DrawDefault = true; // Default drawing for other columns
            }
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
                richTextBoxScriptHelp.AppendText("⚠️ Only NUMERIC values inside {} are iterated.\n\n");

                // Syntax Guide
                richTextBoxScriptHelp.SelectionColor = Color.Black;
                richTextBoxScriptHelp.SelectionFont = new Font(richTextBoxScriptHelp.Font, FontStyle.Bold);
                richTextBoxScriptHelp.AppendText("Syntax Guide:\n\n");

                // 1. Basic Range
                richTextBoxScriptHelp.SelectionFont = new Font(richTextBoxScriptHelp.Font, FontStyle.Bold);
                richTextBoxScriptHelp.AppendText("1. Basic Range\n");
                richTextBoxScriptHelp.SelectionFont = new Font(richTextBoxScriptHelp.Font, FontStyle.Regular);
                richTextBoxScriptHelp.AppendText("   {min..max} generates consecutive values.\n");
                richTextBoxScriptHelp.AppendText("   Example:\n");
                richTextBoxScriptHelp.AppendText("     -j{4..8}\n");
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
                richTextBoxScriptHelp.AppendText("   {min..max..step} defines an increment.\n");
                richTextBoxScriptHelp.AppendText("   Example:\n");
                richTextBoxScriptHelp.AppendText("     -j{2..8..2}\n");
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
                richTextBoxScriptHelp.AppendText("   {value1, value2, value3} lists individual values.\n");
                richTextBoxScriptHelp.AppendText("   Example:\n");
                richTextBoxScriptHelp.AppendText("     -j{2, 4, 10, 16}\n");
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
                richTextBoxScriptHelp.AppendText("     -j{2..4, 8..12..2, 16}\n");
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
                richTextBoxScriptHelp.AppendText("• The resulting script will appear in the main window's \"Job List\" panel.\n");
                richTextBoxScriptHelp.AppendText("• Use cautiously: large ranges may generate many jobs.\n");
                richTextBoxScriptHelp.AppendText("• You may preview how many Jobs are generated by your script.\n");
            }
            finally
            {
                richTextBoxScriptHelp.ResumeLayout(); // Resume layout updates
            }
        }

        /// <summary>
        /// Updates the preview list with expanded jobs from the current script.
        /// Called automatically on every text change or form load.
        /// </summary>
        private void PreviewJobs()
        {
            listViewPreviewJobsListMadeByScript.Items.Clear();

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
                var item = new ListViewItem("Encode");
                item.SubItems.Add("1");
                item.SubItems.Add(param);
                listViewPreviewJobsListMadeByScript.Items.Add(item);
            }

            labelPreviewJobsListMadeByScript.Text = $"Preview Job List ({expanded.Count} items)";
            buttonAddJobToJobListScript.Enabled = expanded.Count > 0;
        }

        /// <summary>
        /// Triggered when script text changes — updates preview instantly.
        /// </summary>
        private void ComboBoxScript_TextChanged(object sender, EventArgs e)
        {
            PreviewJobs();
        }

        /// <summary>
        /// Adds the current script to the main form's job list.
        /// Only allowed if the script produces valid jobs.
        /// </summary>
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
                    "Script produced no valid jobs. Check syntax (e.g. {min..max}).",
                    "Invalid Script",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            OnJobsAdded?.Invoke(new List<ListViewItem>
            {
                new ListViewItem("Encode") { Checked = true }
                    .AddSubItems("1", scriptText)
            });
        }

        /// <summary>
        /// Event raised when user wants to add the script to the main job list.
        /// </summary>
        public event Action<List<ListViewItem>> OnJobsAdded;

        /// <summary>
        /// Initializes UI components and event handlers once, when control is created.
        /// Prevents duplicate subscriptions.
        /// </summary>
        protected override void OnCreateControl()
        {
            base.OnCreateControl();

            if (!DesignMode)
            {
                LoadHelpText();
                comboBoxScript.TextChanged += ComboBoxScript_TextChanged;
                buttonAddJobToJobListScript.Click += ButtonAddJobToJobListScript_Click;

                listViewPreviewJobsListMadeByScript.OwnerDraw = true;
                listViewPreviewJobsListMadeByScript.DrawColumnHeader += ListViewPreview_DrawColumnHeader;
                listViewPreviewJobsListMadeByScript.DrawSubItem += ListViewPreview_DrawSubItem;
            }
        }

        private void buttonCloseScriptConstructorForm_Click(object sender, EventArgs e)
        {
            Close();
        }
    }

    /// <summary>
    /// Helper extension to simplify adding subitems to a ListViewItem.
    /// </summary>
    internal static class ListViewItemExtensions
    {
        public static ListViewItem AddSubItems(this ListViewItem item, params string[] subItems)
        {
            foreach (string subItem in subItems)
            {
                item.SubItems.Add(subItem);
            }
            return item;
        }
    }
}