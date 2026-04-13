using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace FLAC_Benchmark_H
{
    public partial class SummaryForm : Form
    {
        // P/Invoke declaration to control control redrawing via Windows API
        [LibraryImport("user32.dll", EntryPoint = "SendMessageW")]
        private static partial IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
        private const int WM_SETREDRAW = 0x000B;

        // Map of link start positions to link information
        private readonly Dictionary<int, LinkInfo> _linkMap = [];

        // Track visited file paths with O(1) lookup
        private readonly Dictionary<string, bool> _visitedFilePaths = [];

        // Tracks whether mouse is currently over a link (for cursor optimization)
        private bool _isOverLink = false;

        // Collapsed/expanded state flags for collapsible sections
        private bool _filesWithoutMd5Expanded = false;
        private bool _filesWithMd5ErrorsExpanded = false;
        private bool _longPathsExpanded = false;
        private bool _filesWithoutChannelsExpanded = false;
        private bool _filesWithoutSamplingRateExpanded = false;
        private bool _filesWithoutBitDepthExpanded = false;
        private bool _filesWithoutDurationExpanded = false;
        private bool _filesWithBestCompressionExpanded = false;
        private bool _filesWithWorstCompressionExpanded = false;
        private bool _filesWithAvgCompressionExpanded = false;
        private bool _filesWithLowestBitRateExpanded = false;
        private bool _filesWithHighestBitRateExpanded = false;
        private bool _filesWithAvgBitRateExpanded = false;

        // Data storage for collapsible sections (populated on initial load)
        private List<string> _flacFilesWithoutMd5Data = [];
        private List<string> _filesWithMd5ErrorsData = [];
        private List<string> _longPathsData = [];
        private List<string> _filesWithoutChannelsData = [];
        private List<string> _filesWithoutSamplingRate = [];
        private List<string> _filesWithoutBitDepth = [];
        private List<string> _filesWithoutDuration = [];
        private List<string> _filesWithBestCompression = [];
        private List<string> _filesWithWorstCompression = [];
        private List<string> _filesWithAvgCompression = [];
        private List<string> _filesWithLowestBitRate = [];
        private List<string> _filesWithHighestBitRate = [];
        private List<string> _filesWithAvgBitRate = [];

        // Original summary data (stored for redrawing when sections toggle)
        private int _totalFiles;
        private long _totalSize;
        private string _totalDuration = string.Empty;
        private int _flacFiles, _wavFiles;
        private long _flacSize, _wavSize;
        private string _flacDuration = string.Empty;
        private string _wavDuration = string.Empty;
        private List<string> _samplingRates = [], _bitDepths = [], _channels = [], _writingLibraries = [];

        // Percents
        private double _flacFilesPercent;
        private double _flacSizePercent;
        private double _flacDurationPercent;
        private double _wavFilesPercent;
        private double _wavSizePercent;
        private double _wavDurationPercent;

        // FLAC bitrate and compression statistics
        private List<double> _flacBitRates = [];
        private List<double> _flacCompressionRatios = [];
        private double _avgFlacBitRate = 0;
        private double _lowestFlacBitRate = 0;
        private double _highestFlacBitRate = 0;
        private double _avgFlacCompression = 0;
        private double _bestFlacCompression = 0;
        private double _worstFlacCompression = 0;

        // Inner class: Stores metadata about clickable links in the RichTextBox
        private class LinkInfo
        {
            public int Length { get; set; }       // Length of the link text
            public int LinkId { get; set; }       // Section ID (-1 for file path links)
            public string? FilePath { get; set; } // File path (null for section toggle links)
            public bool Visited { get; set; }     // Whether this link has been clicked
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
            long totalSize,
            string totalDuration,

            // FLAC
            int flacFiles,
            long flacSize,
            double flacFilesPercent,
            double flacSizePercent,
            string flacDuration,
            double flacDurationPercent,

            // WAV
            int wavFiles,
            long wavSize,
            double wavFilesPercent,
            double wavSizePercent,
            string wavDuration,
            double wavDurationPercent,

            List<string> samplingRates,
            List<string> bitDepths,
            List<string> channels,
            List<string> filesWithoutMd5List,
            List<string> filesWithMd5Errors,
            List<string> longPathItems,
            List<string> writingLibraries,
            List<string> filesWithoutChannels,
            List<string> filesWithoutSamplingRate,
            List<string> filesWithoutBitDepth,
            List<string> filesWithoutDuration,

            // FLAC bitrate and compression data
            List<double> flacBitRates,
            List<double> flacCompressionRatios,

            // Compression extreme files
            List<string> filesWithBestCompression,
            List<string> filesWithWorstCompression,
            List<string> filesWithAvgCompression,

            // Bitrate extreme files
            List<string> filesWithLowestBitRate,
            List<string> filesWithHighestBitRate,
            List<string> filesWithAvgBitRate)
        {
            // Store all summary data for redrawing
            _totalFiles = totalFiles;
            _totalSize = totalSize;
            _totalDuration = totalDuration;

            // FLAC
            _flacFiles = flacFiles;
            _flacSize = flacSize;
            _flacFilesPercent = flacFilesPercent;
            _flacSizePercent = flacSizePercent;
            _flacDuration = flacDuration;
            _flacDurationPercent = flacDurationPercent;

            // WAV
            _wavFiles = wavFiles;
            _wavSize = wavSize;
            _wavFilesPercent = wavFilesPercent;
            _wavSizePercent = wavSizePercent;
            _wavDuration = wavDuration;
            _wavDurationPercent = wavDurationPercent;

            _samplingRates = samplingRates;
            _bitDepths = bitDepths;
            _channels = channels;
            _writingLibraries = writingLibraries;

            // Store data for collapsible sections
            _flacFilesWithoutMd5Data = filesWithoutMd5List;
            _filesWithMd5ErrorsData = filesWithMd5Errors;
            _longPathsData = longPathItems;
            _filesWithoutChannelsData = filesWithoutChannels;
            _filesWithoutSamplingRate = filesWithoutSamplingRate;
            _filesWithoutBitDepth = filesWithoutBitDepth;
            _filesWithoutDuration = filesWithoutDuration;

            // Store FLAC bitrate and compression data
            _flacBitRates = flacBitRates;
            _flacCompressionRatios = flacCompressionRatios;

            // Store compression extreme files
            _filesWithBestCompression = filesWithBestCompression;
            _filesWithWorstCompression = filesWithWorstCompression;
            _filesWithAvgCompression = filesWithAvgCompression;

            // Store bitrate extreme files
            _filesWithLowestBitRate = filesWithLowestBitRate;
            _filesWithHighestBitRate = filesWithHighestBitRate;
            _filesWithAvgBitRate = filesWithAvgBitRate;

            // Calculate statistics
            if (_flacBitRates.Count > 0)
            {
                _lowestFlacBitRate = _flacBitRates.Min();
                _highestFlacBitRate = _flacBitRates.Max();
                _avgFlacBitRate = _flacBitRates.Average();
            }

            if (_flacCompressionRatios.Count > 0)
            {
                _bestFlacCompression = _flacCompressionRatios.Min();
                _worstFlacCompression = _flacCompressionRatios.Max();
                _avgFlacCompression = _flacCompressionRatios.Average();
            }

            // Clear visited state when new data is loaded
            _visitedFilePaths.Clear();

            // Reset all section states to collapsed and trigger initial render
            _filesWithoutMd5Expanded = false;
            _filesWithMd5ErrorsExpanded = false;
            _longPathsExpanded = false;
            _filesWithoutChannelsExpanded = false;
            _filesWithoutSamplingRateExpanded = false;
            _filesWithoutBitDepthExpanded = false;
            _filesWithoutDurationExpanded = false;
            _filesWithBestCompressionExpanded = false;
            _filesWithWorstCompressionExpanded = false;
            _filesWithAvgCompressionExpanded = false;
            _filesWithLowestBitRateExpanded = false;
            _filesWithHighestBitRateExpanded = false;
            _filesWithAvgBitRateExpanded = false;

            RefreshSummary();
        }

        // Renders the summary with formatted text, clickable links, and collapsible sections.
        private void RefreshSummary()
        {
            // Disable control redrawing to prevent flicker during bulk text operations
            _ = SendMessage(richTextBoxSummary.Handle, WM_SETREDRAW, 0, IntPtr.Zero);

            try
            {
                // Preserve scroll position to restore after content update
                int firstVisibleIndex = GetFirstVisibleCharIndex();

                // Build content in memory before applying to control (improves performance)
                StringBuilder sb = new();
                List<(int position, LinkInfo info)> linkInfos = [];

                // Local helper: appends plain text to the StringBuilder
                void AppendNormal(string text)
                {
                    _ = sb.Append(text);
                }

                // Appends a formatted row with an inline toggle link ("[+] Show files")
                void AppendRowWithToggle(string label, string value, string toggleText, int linkId)
                {
                    // Format base row without trailing \n
                    string baseRow = FormatRow(label, value).TrimEnd('\n');
                    AppendNormal(baseRow);

                    // Append and register the toggle link
                    int linkStart = sb.Length;
                    _ = sb.Append(toggleText);
                    linkInfos.Add((linkStart, new LinkInfo
                    {
                        Length = toggleText.Length,
                        LinkId = linkId,
                        FilePath = null,
                        Visited = false
                    }));

                    // Finish the line
                    AppendNormal("\n");
                }

                // Local helper: appends a clickable file path and records its metadata for link handling
                void AppendPathLink(string path)
                {
                    int linkStart = sb.Length;
                    _ = sb.Append(path);

                    // O(1) lookup in dictionary instead of O(n) foreach
                    bool wasVisited = _visitedFilePaths.ContainsKey(path);

                    linkInfos.Add((linkStart, new LinkInfo
                    {
                        Length = path.Length,
                        LinkId = -1,        // -1 identifies file path links
                        FilePath = path,
                        Visited = wasVisited
                    }));
                }

                // Formats a row with fixed-width columns for consistent alignment when copied
                string FormatRow(string label, string value1, string? value2 = null)
                {
                    const int labelWidth = 20;
                    const int value1Width = 21;
                    const int value2Width = 10;

                    return value2 != null
                        ? $"{label,-labelWidth}{value1,-value1Width}{value2,value2Width}\n"
                        : $"{label,-labelWidth}{value1,-value1Width}\n";
                }

                // Local helper: renders a property list with consistent formatting
                void AppendPropertyList(string label, List<string> items)
                {
                    AppendNormal($"{label}\n");
                    foreach (string item in items)
                    {
                        int lastParen = item.LastIndexOf(" (");

                        if (lastParen > 0 && item.EndsWith(')'))
                        {
                            string value = item[..lastParen];
                            string count = item.Substring(lastParen + 2, item.Length - lastParen - 3);
                            AppendNormal(FormatRow($"  • {value}:", count));
                        }
                        else
                        {
                            AppendNormal($"  • {item}\n");
                        }
                    }
                }

                // Formats percentage: whole numbers show as "100%", decimals as "95,545%"
                string FormatPercent(double percent)
                {
                    return percent >= 99.9995 ? "100 %" : $"{percent:F3} %";
                }

                // === GENERAL STATISTICS ===
                AppendNormal("GENERAL\n");
                AppendNormal($"────────────────────────────────────────────────────────────\n");
                AppendNormal(FormatRow("Total Files:", $"{_totalFiles}"));
                AppendNormal(FormatRow("Total Size:", $"{_totalSize / (1024.0 * 1024.0 * 1024.0):F3} GB"));
                AppendNormal(FormatRow("Total Duration:", _totalDuration));
                AppendNormal("\n");

                // === FLAC STATISTICS ===
                if (_flacFiles > 0)
                {
                    AppendNormal(FormatRow("FLAC Files:", $"{_flacFiles}", FormatPercent(_flacFilesPercent)));
                    AppendNormal(FormatRow("FLAC Size:", $"{_flacSize / (1024.0 * 1024.0 * 1024.0):F3} GB", FormatPercent(_flacSizePercent)));
                    AppendNormal(FormatRow("FLAC Duration:", _flacDuration, FormatPercent(_flacDurationPercent)));
                    AppendNormal("\n");
                }

                // === WAV STATISTICS ===
                if (_wavFiles > 0)
                {
                    AppendNormal(FormatRow("WAV Files:", $"{_wavFiles}", FormatPercent(_wavFilesPercent)));
                    AppendNormal(FormatRow("WAV Size:", $"{_wavSize / (1024.0 * 1024.0 * 1024.0):F3} GB", FormatPercent(_wavSizePercent)));
                    AppendNormal(FormatRow("WAV Duration:", _wavDuration, FormatPercent(_wavDurationPercent)));
                    AppendNormal("\n");
                }

                // === AUDIO PROPERTIES ===
                AppendNormal("AUDIO PROPERTIES\n");
                AppendNormal($"────────────────────────────────────────────────────────────\n");
                AppendPropertyList("Sampling Rates", _samplingRates);
                AppendNormal("\n");
                AppendPropertyList("Bit Depths", _bitDepths);
                AppendNormal("\n");
                AppendPropertyList("Channels", _channels);
                AppendNormal("\n");

                // === WRITING LIBRARY (FLAC encoders) ===
                if (_writingLibraries.Count > 0)
                {
                    AppendNormal("Writing library (FLAC files only)\n");

                    foreach (string item in _writingLibraries)
                    {
                        int lastParen = item.LastIndexOf(" (");

                        if (lastParen > 0 && item.EndsWith(')'))
                        {
                            string value = item[..lastParen];
                            string count = item.Substring(lastParen + 2, item.Length - lastParen - 3);

                            // 41 = labelWidth (20) + value1Width (21), 10 = value2Width
                            string libraryLabel = $"  • {value}:";
                            AppendNormal($"{libraryLabel,-41}{count,10}\n");
                        }
                        else
                        {
                            AppendNormal($"  • {item}\n");
                        }
                    }
                    AppendNormal("\n");
                }

                // === FLAC BITRATE STATISTICS ===
                if (_flacFiles > 0 && _flacBitRates.Count > 0)
                {
                    AppendNormal("FLAC BITRATE STATISTICS\n");
                    AppendNormal($"────────────────────────────────────────────────────────────\n");

                    // Average bitrate with link to avg files
                    string avgBitrateToggleText = _filesWithAvgBitRateExpanded ? "[-] Hide files" : "[+] Show files";
                    AppendRowWithToggle("Avg bitrate:", $"{_avgFlacBitRate:F0} kb/s", avgBitrateToggleText, 11);

                    if (_filesWithAvgBitRateExpanded)
                    {
                        foreach (string path in _filesWithAvgBitRate)
                        {
                            AppendNormal("  • ");
                            AppendPathLink(path);
                            AppendNormal("\n");
                        }
                        AppendNormal("\n");
                    }

                    // Best bitrate (minimum) with link to min files
                    string lowestBitrateToggleText = _filesWithLowestBitRateExpanded ? "[-] Hide files" : "[+] Show files";
                    AppendRowWithToggle("Lowest bitrate:", $"{_lowestFlacBitRate:F0} kb/s", lowestBitrateToggleText, 12);

                    if (_filesWithLowestBitRateExpanded)
                    {
                        foreach (string path in _filesWithLowestBitRate)
                        {
                            AppendNormal("  • ");
                            AppendPathLink(path);
                            AppendNormal("\n");
                        }
                        AppendNormal("\n");
                    }

                    // Worst bitrate (maximum) with link to max files
                    string highestBitrateToggleText = _filesWithHighestBitRateExpanded ? "[-] Hide files" : "[+] Show files";
                    AppendRowWithToggle("Highest bitrate:", $"{_highestFlacBitRate:F0} kb/s", highestBitrateToggleText, 13);

                    if (_filesWithHighestBitRateExpanded)
                    {
                        foreach (string path in _filesWithHighestBitRate)
                        {
                            AppendNormal("  • ");
                            AppendPathLink(path);
                            AppendNormal("\n");
                        }
                        AppendNormal("\n");
                    }

                    AppendNormal("\n");
                }

                // === FLAC COMPRESSION STATISTICS ===
                if (_flacFiles > 0 && _flacCompressionRatios.Count > 0)
                {
                    AppendNormal("FLAC COMPRESSION STATISTICS\n");
                    AppendNormal($"────────────────────────────────────────────────────────────\n");

                    // Average compression with link to avg files
                    string avgToggleText = _filesWithAvgCompressionExpanded ? "[-] Hide files" : "[+] Show files";
                    AppendRowWithToggle("Avrg compression:", FormatPercent(_avgFlacCompression * 100), avgToggleText, 10);

                    if (_filesWithAvgCompressionExpanded)
                    {
                        foreach (string path in _filesWithAvgCompression)
                        {
                            AppendNormal("  • ");
                            AppendPathLink(path);
                            AppendNormal("\n");
                        }
                        AppendNormal("\n");
                    }

                    // Best compression (minimum) with link to best files
                    string bestToggleText = _filesWithBestCompressionExpanded ? "[-] Hide files" : "[+] Show files";
                    AppendRowWithToggle("Best compression:", FormatPercent(_bestFlacCompression * 100), bestToggleText, 8);

                    if (_filesWithBestCompressionExpanded)
                    {
                        foreach (string path in _filesWithBestCompression)
                        {
                            AppendNormal("  • ");
                            AppendPathLink(path);
                            AppendNormal("\n");
                        }
                        AppendNormal("\n");
                    }

                    // Worst compression (maximum) with link to worst files
                    string worstToggleText = _filesWithWorstCompressionExpanded ? "[-] Hide files" : "[+] Show files";
                    AppendRowWithToggle("Worst compression:", FormatPercent(_worstFlacCompression * 100), worstToggleText, 9);

                    if (_filesWithWorstCompressionExpanded)
                    {
                        foreach (string path in _filesWithWorstCompression)
                        {
                            AppendNormal("  • ");
                            AppendPathLink(path);
                            AppendNormal("\n");
                        }
                        AppendNormal("\n");
                    }
                    AppendNormal("\n");
                }

                // === POTENTIAL PROBLEMS ===
                AppendNormal($"POTENTIAL PROBLEMS\n");
                AppendNormal($"────────────────────────────────────────────────────────────\n");

                // FLAC files without MD5 (collapsible section)
                if (_flacFilesWithoutMd5Data.Count == 0)
                {
                    AppendNormal(FormatRow("FLAC without MD5:", "0"));
                }
                else
                {
                    string toggleText = _filesWithoutMd5Expanded ? "[-] Hide files" : "[+] Show files";
                    AppendRowWithToggle("FLAC without MD5:", $"{_flacFilesWithoutMd5Data.Count}", toggleText, 1);

                    if (_filesWithoutMd5Expanded)
                    {
                        foreach (string path in _flacFilesWithoutMd5Data)
                        {
                            AppendNormal("  • ");
                            AppendPathLink(path);
                            AppendNormal("\n");
                        }
                        AppendNormal("\n");
                    }
                }

                // === FILES WITH MD5 ERRORS (collapsible) ===
                if (_filesWithMd5ErrorsData.Count == 0)
                {
                    AppendNormal(FormatRow("MD5 Errors:", "0"));
                }
                else
                {
                    string toggleText = _filesWithMd5ErrorsExpanded ? "[-] Hide files" : "[+] Show files";
                    AppendRowWithToggle("MD5 Errors:", $"{_filesWithMd5ErrorsData.Count}", toggleText, 2);

                    if (_filesWithMd5ErrorsExpanded)
                    {
                        foreach (string path in _filesWithMd5ErrorsData)
                        {
                            AppendNormal("  • ");
                            AppendPathLink(path);
                            AppendNormal("\n");
                        }
                        AppendNormal("\n");
                    }
                }

                // === LONG PATHS WARNING (≥260 characters) ===
                if (_longPathsData.Count == 0)
                {
                    AppendNormal(FormatRow("Long path (>259):", "0"));
                }
                else
                {
                    string toggleText = _longPathsExpanded ? "[-] Hide files" : "[+] Show files";
                    AppendRowWithToggle("Long path (>259):", $"{_longPathsData.Count}", toggleText, 3);

                    if (_longPathsExpanded)
                    {
                        foreach (string path in _longPathsData)
                        {
                            AppendNormal($"  • {path.Length} characters: ");
                            AppendPathLink(path);
                            AppendNormal("\n");
                        }
                        AppendNormal("\n");
                    }
                }

                // === FILES WITHOUT CHANNEL INFO ===
                if (_filesWithoutChannelsData.Count > 0)
                {
                    string toggleText = _filesWithoutChannelsExpanded ? "[-] Hide files" : "[+] Show files";
                    AppendRowWithToggle("No 'Channel' info:", $"{_filesWithoutChannelsData.Count}", toggleText, 4);

                    if (_filesWithoutChannelsExpanded)
                    {
                        foreach (string path in _filesWithoutChannelsData)
                        {
                            AppendNormal("  • ");
                            AppendPathLink(path);
                            AppendNormal("\n");
                        }
                        AppendNormal("\n");
                    }
                }

                // === FILES WITHOUT SAMPLING RATE ===
                if (_filesWithoutSamplingRate.Count > 0)
                {
                    string toggleText = _filesWithoutSamplingRateExpanded ? "[-] Hide files" : "[+] Show files";
                    AppendRowWithToggle("No 'Sampling Rate':", $"{_filesWithoutSamplingRate.Count}", toggleText, 5);

                    if (_filesWithoutSamplingRateExpanded)
                    {
                        foreach (string path in _filesWithoutSamplingRate)
                        {
                            AppendNormal("  • ");
                            AppendPathLink(path);
                            AppendNormal("\n");
                        }
                        AppendNormal("\n");
                    }
                }

                // === FILES WITHOUT BIT DEPTH ===
                if (_filesWithoutBitDepth.Count > 0)
                {
                    string toggleText = _filesWithoutBitDepthExpanded ? "[-] Hide files" : "[+] Show files";
                    AppendRowWithToggle("No 'Bit Depth':", $"{_filesWithoutBitDepth.Count}", toggleText, 6);

                    if (_filesWithoutBitDepthExpanded)
                    {
                        foreach (string path in _filesWithoutBitDepth)
                        {
                            AppendNormal("  • ");
                            AppendPathLink(path);
                            AppendNormal("\n");
                        }
                        AppendNormal("\n");
                    }
                }

                // === FILES WITHOUT DURATION ===
                if (_filesWithoutDuration.Count > 0)
                {
                    string toggleText = _filesWithoutDurationExpanded ? "[-] Hide files" : "[+] Show files";
                    AppendRowWithToggle("No 'Duration':", $"{_filesWithoutDuration.Count}", toggleText, 7);

                    if (_filesWithoutDurationExpanded)
                    {
                        foreach (string path in _filesWithoutDuration)
                        {
                            AppendNormal("  • ");
                            AppendPathLink(path);
                            AppendNormal("\n");
                        }
                        AppendNormal("\n");
                    }
                }

                // Remove trailing newlines for clean output (optional)
                while (sb.Length > 0 && sb[^1] == '\n')
                {
                    sb.Length--;
                }

                // Add trailing newline for clean output (optional)
                _ = sb.Append('\n');

                // Apply constructed content to RichTextBox in single operation
                richTextBoxSummary.Clear();
                richTextBoxSummary.AppendText(sb.ToString());

                // Register link positions for click handling
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
                richTextBoxSummary.Invalidate();    // Mark control as needing repaint
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
        /// Section links: dark green; Unvisited file links: blue; Visited file links: purple.
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
                        // File path link: underlined
                        richTextBoxSummary.SelectionFont = new Font(normalFont, FontStyle.Underline);

                        // Use the Visited property from LinkInfo
                        richTextBoxSummary.SelectionColor = kvp.Value.Visited ? Color.Purple : Color.Blue;
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
                        // Mark as visited in both LinkInfo and dictionary
                        linkInfo.Visited = true;
                        _visitedFilePaths[linkInfo.FilePath] = true;

                        // Update color immediately without full redraw
                        UpdateLinkColor(linkStart, linkInfo.Length, Color.Purple);

                        // Open file/folder in Explorer
                        OpenFileInExplorer(linkInfo.FilePath);
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Updates the color of a specific link without redrawing everything.
        /// </summary>
        private void UpdateLinkColor(int start, int length, Color color)
        {
            try
            {
                richTextBoxSummary.Select(start, length);
                richTextBoxSummary.SelectionColor = color;
                richTextBoxSummary.DeselectAll();
            }
            catch
            {
                // Fallback to full redraw if partial update fails
                RefreshSummary();
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
                case 5: _filesWithoutSamplingRateExpanded = !_filesWithoutSamplingRateExpanded; break;
                case 6: _filesWithoutBitDepthExpanded = !_filesWithoutBitDepthExpanded; break;
                case 7: _filesWithoutDurationExpanded = !_filesWithoutDurationExpanded; break;
                case 8: _filesWithBestCompressionExpanded = !_filesWithBestCompressionExpanded; break;
                case 9: _filesWithWorstCompressionExpanded = !_filesWithWorstCompressionExpanded; break;
                case 10: _filesWithAvgCompressionExpanded = !_filesWithAvgCompressionExpanded; break;
                case 11: _filesWithAvgBitRateExpanded = !_filesWithAvgBitRateExpanded; break;
                case 12: _filesWithLowestBitRateExpanded = !_filesWithLowestBitRateExpanded; break;
                case 13: _filesWithHighestBitRateExpanded = !_filesWithHighestBitRateExpanded; break;
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