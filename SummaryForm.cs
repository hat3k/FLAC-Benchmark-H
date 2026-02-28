using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace FLAC_Benchmark_H
{
    public partial class SummaryForm : Form
    {
        // P/Invoke declaration to control control redrawing via Windows API
        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
        private const int WM_SETREDRAW = 0x000B;

        // Map of link start positions to link information
        private readonly Dictionary<int, LinkInfo> _linkMap = [];

        // Tracks whether mouse is currently over a link (for cursor optimization)
        private bool _isOverLink = false;

        // Collapsed/expanded state flags for collapsible sections
        private bool _filesWithoutMd5Expanded = false;
        private bool _filesWithMd5ErrorsExpanded = false;
        private bool _longPathsExpanded = false;
        private bool _filesWithoutChannelsExpanded = false;

        // Data storage for collapsible sections (populated on initial load)
        private List<string> _filesWithoutMd5Data = [];
        private List<string> _filesWithMd5ErrorsData = [];
        private List<string> _longPathsData = [];
        private List<string> _filesWithoutChannelsData = [];

        // Original summary data (stored for redrawing when sections toggle)
        private int _totalFiles, _checkedFiles, _uncheckedFiles;
        private long _totalSize;
        private string _totalDuration = string.Empty;
        private int _flacCount, _wavCount;
        private long _flacSize, _wavSize;
        private double _flacPercent, _wavPercent;
        private List<string> _samplingRates = [], _bitDepths = [], _channels = [], _writingLibraries = [];
        private int _filesWithMd5, _filesWithoutMd5, _md5Errors;

        // Inner class: Stores metadata about clickable links in the RichTextBox
        private class LinkInfo
        {
            public int Length { get; set; }       // Length of the link text
            public int LinkId { get; set; }       // Section ID (-1 for file path links)
            public string? FilePath { get; set; } // File path (null for section toggle links)
        }

        public SummaryForm()
        {
            InitializeComponent();

            // Enable double-buffering for RichTextBox via reflection (reduces flicker)
            _ = typeof(RichTextBox).InvokeMember("DoubleBuffered",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.SetProperty,
                null, richTextBoxSummary, [true]);
        }

        // Initializes the summary form with structured data.
        public void SetSummaryData(
            int totalFiles,
            int checkedFiles,
            int uncheckedFiles,
            long totalSize,
            string totalDuration,
            int flacCount,
            long flacSize,
            double flacPercent,
            int wavCount,
            long wavSize,
            double wavPercent,
            List<string> samplingRates,
            List<string> bitDepths,
            List<string> channels,
            int filesWithMd5,
            int filesWithoutMd5,
            int md5Errors,
            List<string> filesWithoutMd5List,
            List<string> filesWithMd5Errors,
            List<string> longPathItems,
            List<string> writingLibraries,
            List<string> filesWithoutChannels)
        {
            // Store all summary data for redrawing
            _totalFiles = totalFiles; _checkedFiles = checkedFiles; _uncheckedFiles = uncheckedFiles;
            _totalSize = totalSize; _totalDuration = totalDuration;
            _flacCount = flacCount; _flacSize = flacSize; _flacPercent = flacPercent;
            _wavCount = wavCount; _wavSize = wavSize; _wavPercent = wavPercent;
            _samplingRates = samplingRates; _bitDepths = bitDepths; _channels = channels;
            _filesWithMd5 = filesWithMd5; _filesWithoutMd5 = filesWithoutMd5; _md5Errors = md5Errors;
            _writingLibraries = writingLibraries;

            // Store data for collapsible sections
            _filesWithoutMd5Data = filesWithoutMd5List;
            _filesWithMd5ErrorsData = filesWithMd5Errors;
            _longPathsData = longPathItems;
            _filesWithoutChannelsData = filesWithoutChannels;

            // Reset all section states to collapsed and trigger initial render
            _filesWithoutMd5Expanded = false;
            _filesWithMd5ErrorsExpanded = false;
            _longPathsExpanded = false;
            _filesWithoutChannelsExpanded = false;

            RefreshSummary();
        }

        /// <summary>
        /// Renders the complete summary text with proper formatting and clickable links.
        /// Uses WM_SETREDRAW to prevent flickering during bulk updates.
        /// </summary>
        private void RefreshSummary()
        {
            // Disable redrawing during bulk text operations (prevents flicker)
            _ = SendMessage(richTextBoxSummary.Handle, WM_SETREDRAW, 0, IntPtr.Zero);

            try
            {
                // Save current scroll position to restore after redraw
                int firstVisibleIndex = GetFirstVisibleCharIndex();

                // Build entire text content in memory before applying to control
                StringBuilder sb = new();
                List<(int position, LinkInfo info)> linkInfos = [];

                // Appends normal (non-link) text to the StringBuilder
                void AppendNormal(string text)
                {
                    _ = sb.Append(text);
                }

                // Appends a file path as a clickable link and records its metadata
                void AppendPathLink(string path)
                {
                    int linkStart = sb.Length;
                    _ = sb.Append(path);
                    linkInfos.Add((linkStart, new LinkInfo
                    {
                        Length = path.Length,
                        LinkId = -1,        // -1 indicates a file path link
                        FilePath = path
                    }));
                }

                // Appends a section toggle link ("Show/Hide files") and records its metadata
                void AppendExpandableLink(string linkText, int linkId)
                {
                    int linkStart = sb.Length;
                    _ = sb.Append(linkText);
                    linkInfos.Add((linkStart, new LinkInfo
                    {
                        Length = linkText.Length,
                        LinkId = linkId,    // 1-4 identifies which section to toggle
                        FilePath = null
                    }));
                }

                // SECTION: General statistics
                AppendNormal("GENERAL\n");
                AppendNormal("───────────────────────────────────────\n");
                AppendNormal($"Total Files:          {_totalFiles}\n");
                AppendNormal($"Checked:              {_checkedFiles}\n");
                AppendNormal($"Unchecked:            {_uncheckedFiles}\n\n");
                AppendNormal($"Total Size:           {_totalSize / (1024.0 * 1024.0 * 1024.0):F2} GB\n");
                AppendNormal($"Total Duration:       {_totalDuration}\n\n");

                // SECTION: Format distribution (FLAC/WAV)
                AppendNormal("FORMAT\n");
                AppendNormal("───────────────────────────────────────\n");
                AppendNormal("Format    Files    Size      %\n");
                AppendNormal("───────────────────────────────────────\n");
                AppendNormal($"FLAC      {_flacCount,-7}{_flacSize / (1024.0 * 1024.0 * 1024.0),6:F1} GB   {_flacPercent:F1}%\n");
                AppendNormal($"WAV       {_wavCount,-7}{_wavSize / (1024.0 * 1024.0 * 1024.0),6:F1} GB   {_wavPercent:F1}%\n\n");

                // SECTION: Audio properties summary
                AppendNormal("AUDIO PROPERTIES\n");
                AppendNormal("───────────────────────────────────────\n");
                AppendNormal($"Sampling Rates:  {string.Join(", ", _samplingRates)}\n");
                AppendNormal($"Bit Depths:      {string.Join(", ", _bitDepths)}\n");
                AppendNormal($"Channels:        {string.Join(", ", _channels)}\n");

                // SECTION: Files without channel information (collapsible)
                if (_filesWithoutChannelsData.Count > 0)
                {
                    AppendNormal($"\nFILES WITHOUT CHANNEL INFORMATION\n");
                    AppendNormal($"───────────────────────────────────────\n");
                    AppendNormal($"Total: {_filesWithoutChannelsData.Count} file(s)\n");

                    if (_filesWithoutChannelsExpanded)
                    {
                        // Section expanded: show toggle link + file list
                        AppendExpandableLink("▼ Hide files", 4);
                        AppendNormal("\n");
                        foreach (string path in _filesWithoutChannelsData)
                        {
                            AppendNormal("  • ");
                            AppendPathLink(path);
                            AppendNormal("\n");
                        }
                    }
                    else
                    {
                        // Section collapsed: show toggle link only
                        AppendExpandableLink("▶ Show files", 4);
                        AppendNormal("\n");
                    }
                }

                // SECTION: MD5 status summary
                AppendNormal($"\nMD5 STATUS\n");
                AppendNormal($"───────────────────────────────────────\n");
                AppendNormal($"Files with MD5:     {_filesWithMd5}\n");
                AppendNormal($"Files without MD5:  {_filesWithoutMd5}\n");
                AppendNormal($"MD5 errors:         {_md5Errors}\n");

                // SECTION: Files without MD5 (collapsible)
                if (_filesWithoutMd5Data.Count > 0)
                {
                    AppendNormal($"\nFILES WITHOUT MD5\n");
                    AppendNormal($"───────────────────────────────────────\n");
                    AppendNormal($"Total: {_filesWithoutMd5Data.Count} file(s)\n");

                    if (_filesWithoutMd5Expanded)
                    {
                        AppendExpandableLink("▼ Hide files", 1);
                        AppendNormal("\n");
                        foreach (string path in _filesWithoutMd5Data)
                        {
                            AppendNormal("  • ");
                            AppendPathLink(path);
                            AppendNormal("\n");
                        }
                    }
                    else
                    {
                        AppendExpandableLink("▶ Show files", 1);
                        AppendNormal("\n");
                    }
                }

                // SECTION: Files with MD5 errors (collapsible)
                if (_filesWithMd5ErrorsData.Count > 0)
                {
                    AppendNormal($"\nFILES WITH MD5 ERRORS\n");
                    AppendNormal($"───────────────────────────────────────\n");
                    AppendNormal($"Total: {_filesWithMd5ErrorsData.Count} file(s)\n");

                    if (_filesWithMd5ErrorsExpanded)
                    {
                        AppendExpandableLink("▼ Hide files", 2);
                        AppendNormal("\n");
                        foreach (string path in _filesWithMd5ErrorsData)
                        {
                            AppendNormal("  • ");
                            AppendPathLink(path);
                            AppendNormal("\n");
                        }
                    }
                    else
                    {
                        AppendExpandableLink("▶ Show files", 2);
                        AppendNormal("\n");
                    }
                }

                // SECTION: Long paths (≥260 chars) - Windows MAX_PATH limit warning
                if (_longPathsData.Count > 0)
                {
                    AppendNormal($"\nLONG PATHS (≥260 characters - Windows MAX_PATH limit)\n");
                    AppendNormal($"───────────────────────────────────────\n");
                    AppendNormal($"⚠️ Warning: {_longPathsData.Count} file(s) may have compatibility issues!\n");

                    if (_longPathsExpanded)
                    {
                        AppendExpandableLink("▼ Hide files", 3);
                        AppendNormal("\n");
                        foreach (string path in _longPathsData)
                        {
                            AppendNormal($"  • {path.Length} characters: ");
                            AppendPathLink(path);
                            AppendNormal("\n");
                        }
                    }
                    else
                    {
                        AppendExpandableLink("▶ Show files", 3);
                        AppendNormal("\n");
                    }
                }

                // SECTION: Writing library statistics (FLAC encoder info)
                if (_writingLibraries.Count > 0)
                {
                    AppendNormal($"\nWRITING LIBRARY (FLAC files only)\n");
                    AppendNormal($"───────────────────────────────────────\n");
                    foreach (string lib in _writingLibraries)
                    {
                        AppendNormal($"  • {lib}\n");
                    }
                }

                // Apply constructed text to RichTextBox in single operation
                richTextBoxSummary.Clear();
                richTextBoxSummary.AppendText(sb.ToString());

                // Populate link map with position - metadata mappings
                _linkMap.Clear();
                foreach ((int pos, LinkInfo? info) in linkInfos)
                {
                    _linkMap[pos] = info;
                }

                // Apply visual styles (colors, underlines) to all registered links
                ApplyLinkStyles();

                // Restore scroll position to maintain user context after redraw
                RestoreScrollPosition(firstVisibleIndex);
            }
            finally
            {
                // Re-enable redrawing and force immediate refresh
                _ = SendMessage(richTextBoxSummary.Handle, WM_SETREDRAW, 1, IntPtr.Zero);
                richTextBoxSummary.Invalidate();    // Mark control area as dirty (needs redraw)
                richTextBoxSummary.Update();        // Force immediate repaint
                richTextBoxSummary.Refresh();       // Additional refresh guarantee
                // Application.DoEvents();          // Optional: process pending messages
            }
        }

        /// <summary>
        /// Gets the character index of the first visible line in the RichTextBox.
        /// Used to preserve scroll position during redraws.
        /// </summary>
        private int GetFirstVisibleCharIndex()
        {
            try
            {
                // Calculate position with small offset from top-left corner
                Point topLeft = new(2, 2);
                int firstVisibleIndex = richTextBoxSummary.GetCharIndexFromPosition(topLeft);

                // Validate index is within text bounds
                if (firstVisibleIndex >= 0 && firstVisibleIndex < richTextBoxSummary.TextLength)
                {
                    return firstVisibleIndex;
                }
            }
            catch
            {
                // Silently handle edge cases and default to beginning
            }

            return 0;
        }

        // Restores the scroll position after a redraw operation.
        private void RestoreScrollPosition(int targetCharIndex)
        {
            try
            {
                if (targetCharIndex >= 0 && targetCharIndex < richTextBoxSummary.TextLength)
                {
                    richTextBoxSummary.Select(targetCharIndex, 0);
                    richTextBoxSummary.ScrollToCaret();
                }
                else if (richTextBoxSummary.TextLength > 0)
                {
                    // Fallback: scroll to beginning if target index is invalid
                    richTextBoxSummary.Select(0, 0);
                    richTextBoxSummary.ScrollToCaret();
                }
            }
            catch
            {
                // Ignore scroll errors to avoid disrupting UX
            }
        }

        /// <summary>
        /// Applies visual styling (color, underline) to all registered links.
        /// Section links: dark green; File links: blue.
        /// </summary>
        private void ApplyLinkStyles()
        {
            Font normalFont = richTextBoxSummary.Font;

            foreach (KeyValuePair<int, LinkInfo> kvp in _linkMap)
            {
                int start = kvp.Key;
                int length = kvp.Value.Length;

                // Ensure link range is within valid text bounds
                if (start >= 0 && length > 0 && start + length <= richTextBoxSummary.TextLength)
                {
                    richTextBoxSummary.Select(start, length);

                    if (kvp.Value.LinkId >= 0)
                    {
                        // Section toggle link: dark green, underlined
                        richTextBoxSummary.SelectionFont = new Font(normalFont, FontStyle.Underline);
                        richTextBoxSummary.SelectionColor = Color.DarkGreen;
                    }
                    else
                    {
                        // File path link: blue, underlined
                        richTextBoxSummary.SelectionFont = new Font(normalFont, FontStyle.Underline);
                        richTextBoxSummary.SelectionColor = Color.Blue;
                    }
                }
            }
        }

        /// <summary>
        /// Handles mouse clicks on the RichTextBox to activate links.
        /// Distinguishes between section toggle links and file path links.
        /// </summary>
        private void RichTextBoxSummary_MouseClick(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
            {
                return;
            }

            int cursorPos = richTextBoxSummary.GetCharIndexFromPosition(e.Location);

            foreach (KeyValuePair<int, LinkInfo> kvp in _linkMap)
            {
                int linkStart = kvp.Key;
                LinkInfo linkInfo = kvp.Value;

                // Check if click position falls within this link's range
                if (cursorPos >= linkStart && cursorPos < linkStart + linkInfo.Length)
                {
                    if (linkInfo.LinkId >= 0)
                    {
                        // Section toggle link: expand/collapse section
                        ToggleSection(linkInfo.LinkId);
                        return;
                    }
                    else if (linkInfo.FilePath != null && File.Exists(linkInfo.FilePath))
                    {
                        // File path link: open file/folder in Explorer
                        OpenFileInExplorer(linkInfo.FilePath);
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Toggles the expanded/collapsed state of a collapsible section
        /// and triggers a full redraw to reflect the change.
        /// </summary>
        private void ToggleSection(int linkId)
        {
            switch (linkId)
            {
                case 1: _filesWithoutMd5Expanded = !_filesWithoutMd5Expanded; break;
                case 2: _filesWithMd5ErrorsExpanded = !_filesWithMd5ErrorsExpanded; break;
                case 3: _longPathsExpanded = !_longPathsExpanded; break;
                case 4: _filesWithoutChannelsExpanded = !_filesWithoutChannelsExpanded; break;
                default: return;
            }

            // Redraw entire summary with updated section state
            RefreshSummary();
        }

        // Handles mouse movement to update cursor appearance over links.
        private void RichTextBoxSummary_MouseMove(object? sender, MouseEventArgs e)
        {
            int cursorPos = richTextBoxSummary.GetCharIndexFromPosition(e.Location);

            // Default cursor for empty areas
            if (cursorPos < 0)
            {
                richTextBoxSummary.Cursor = Cursors.Default;
                return;
            }

            // Check if cursor is currently over any registered link
            bool newIsOverLink = _linkMap.Any(kvp =>
            {
                int linkStart = kvp.Key;
                int linkLength = kvp.Value.Length;
                return cursorPos >= linkStart && cursorPos < linkStart + linkLength;
            });

            // Only update cursor if state actually changed (prevents flicker)
            if (newIsOverLink != _isOverLink)
            {
                _isOverLink = newIsOverLink;
                richTextBoxSummary.Cursor = _isOverLink ? Cursors.Hand : Cursors.IBeam;
            }
        }

        /// <summary>
        /// Opens a file or folder in Windows Explorer.
        /// Handles long paths (≥260 chars) by opening parent folder instead.
        /// </summary>
        private static void OpenFileInExplorer(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    if (filePath.Length >= 260)
                    {
                        // Long path: open parent folder (Explorer can't select long-path files)
                        string? directory = Path.GetDirectoryName(filePath);
                        if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory))
                        {
                            _ = Process.Start(new ProcessStartInfo
                            {
                                FileName = "explorer.exe",
                                Arguments = $"\"{directory}\"",
                                UseShellExecute = true
                            });
                        }
                    }
                    else
                    {
                        // Normal path: select file in Explorer
                        _ = Process.Start(new ProcessStartInfo
                        {
                            FileName = "explorer.exe",
                            Arguments = $"/select,\"{filePath}\"",
                            UseShellExecute = true
                        });
                    }
                }
                else if (Directory.Exists(filePath))
                {
                    // Path is a directory: open it directly
                    _ = Process.Start(new ProcessStartInfo
                    {
                        FileName = "explorer.exe",
                        Arguments = $"\"{filePath}\"",
                        UseShellExecute = true
                    });
                }
                else
                {
                    // File not found: show warning
                    _ = MessageBox.Show($"File not found:\n\n{filePath}", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                // Handle unexpected errors gracefully
                _ = MessageBox.Show($"Failed to open: {filePath}\n\n{ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Copies the entire summary text to the clipboard
        private void ButtonCopySummaryAudioFiles_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(richTextBoxSummary.Text))
            {
                Clipboard.SetText(richTextBoxSummary.Text);
            }
        }

        // Closes the SummaryForm
        private void ButtonCloseSummaryAudioFiles_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}