using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FLAC_Benchmark_H
{
    /// <summary>
    /// Extended DataGridView with double-buffering enabled to eliminate flickering and improve performance.
    /// Ideal for high-frequency updates (e.g., logging hundreds of test results).
    /// </summary>
    public class DataGridViewEx : DataGridView
    {
        public DataGridViewEx()
        {
            // Enable double buffering at the control level
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.DoubleBuffer, true);
        }

        // Optional: Override CreateParams for extra stability on Windows 10/11
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x02000000; // WS_EX_COMPOSITED — forces composited painting (Windows Vista+)
                return cp;
            }
        }
    }
}
