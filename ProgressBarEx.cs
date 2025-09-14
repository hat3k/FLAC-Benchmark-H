using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FLAC_Benchmark_H
{
    /// <summary>
    /// Extended ProgressBar that supports displaying text over the bar (percent, count, or custom).
    /// </summary>
    [ToolboxBitmap(typeof(ProgressBar))] // Use same icon as standard ProgressBar
    public class ProgressBarEx : ProgressBar
    {
        public const int WM_PAINT = 0xF;
        public const int WS_EX_COMPOSITED = 0x2000_000;

        private TextDisplayType _style = TextDisplayType.Percent;
        private string _manualText = "";

        public ProgressBarEx()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.ResizeRedraw, true);
        }

        /// <summary>
        /// Gets or sets the type of text to display on the progress bar.
        /// </summary>
        [Category("Appearance")]
        [Description("Specifies what type of text to display: None, Percent, Count, or Manual.")]
        [DefaultValue(TextDisplayType.Percent)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public TextDisplayType DisplayType
        {
            get { return _style; }
            set
            {
                if (_style != value)
                {
                    _style = value;
                    Invalidate();
                }
            }
        }

        /// <summary>
        /// Gets or sets the manual text to display when DisplayType is set to Manual.
        /// </summary>
        [Category("Appearance")]
        [Description("The text to display when DisplayType is set to Manual.")]
        [DefaultValue("")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public string ManualText
        {
            get { return _manualText; }
            set
            {
                if (_manualText != value)
                {
                    _manualText = value ?? "";
                    Invalidate();
                }
            }
        }

        /// <summary>
        /// Gets or sets the color of the text displayed on the progress bar.
        /// </summary>
        [Category("Appearance")]
        [Description("The color of the text displayed on the progress bar.")]
        [DefaultValue(typeof(Color), "ControlText")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public Color TextColor { get; set; } = SystemColors.ControlText;

        /// <summary>
        /// Gets or sets the font of the text displayed on the progress bar.
        /// </summary>
        [Category("Appearance")]
        [Description("The font used for the text on the progress bar.")]
        [Browsable(true)]
        [EditorBrowsable(EditorBrowsableState.Always)]
        public override Font Font
        {
            get => base.Font;
            set => base.Font = value;
        }

        protected override CreateParams CreateParams
        {
            get
            {
                var parms = base.CreateParams;
                parms.ExStyle |= WS_EX_COMPOSITED; // Enable double-buffering at OS level
                return parms;
            }
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            if (m.Msg == WM_PAINT)
                AdditionalPaint(m);
        }

        private void AdditionalPaint(Message m)
        {
            if (DisplayType == TextDisplayType.None) return;

            string text = GetDisplayText();
            if (string.IsNullOrEmpty(text)) return;

            using (var g = Graphics.FromHwnd(Handle))
            {
                var rect = new Rectangle(0, 0, Width, Height);

                TextRenderer.DrawText(
                    g,                         // graphics
                    text,                      // text
                    Font,                      // font
                    rect,                      // bounds
                    TextColor,                 // foreColor
                    TextFormatFlags.HorizontalCenter |
                    TextFormatFlags.VerticalCenter |
                    TextFormatFlags.SingleLine |
                    TextFormatFlags.NoPadding |
                    TextFormatFlags.PreserveGraphicsClipping
                );
            }
        }

        private string GetDisplayText()
        {
            return DisplayType switch
            {
                TextDisplayType.Percent => Maximum > 0
                    ? $"{(int)(100.0 * Value / Maximum)} %"
                    : "0 %",

                TextDisplayType.Count => $"{Value} / {Maximum}",

                TextDisplayType.Manual => _manualText,

                _ => ""
            };
        }

        /// <summary>
        /// Supported display modes for the progress bar text.
        /// </summary>
        public enum TextDisplayType
        {
            /// <summary>No text is displayed.</summary>
            None,

            /// <summary>Displays progress as a percentage (e.g., "50 %").</summary>
            Percent,

            /// <summary>Displays progress as a fraction (e.g., "3 / 10").</summary>
            Count,

            /// <summary>Displays custom text specified in ManualText.</summary>
            Manual
        }
    }
}