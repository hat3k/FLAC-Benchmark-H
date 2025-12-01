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
                cp.ExStyle |= 0x02000000; // WS_EX_COMPOSITED - forces composited painting (Windows Vista+)
                return cp;
            }
        }

        /// <summary>
        /// Moves a row from one index to another within the DataGridView.
        /// </summary>
        /// <param name="fromIndex">The index of the row to move.</param>
        /// <param name="toIndex">The index to move the row to.</param>
        /// <returns>True if the move was successful, false otherwise.</returns>
        public bool MoveRow(int fromIndex, int toIndex)
        {
            // Validate indices
            if (fromIndex < 0 || fromIndex >= this.Rows.Count || toIndex < 0 || toIndex >= this.Rows.Count)
            {
                return false; // Index out of bounds
            }

            if (fromIndex == toIndex)
            {
                return true; // No move needed
            }

            // Get the row to move
            DataGridViewRow rowToMove = this.Rows[fromIndex];

            // Remove the row from its current position
            this.Rows.RemoveAt(fromIndex);

            // Insert the row at the new position
            this.Rows.Insert(toIndex, rowToMove);

            // Preserve selection and current cell if the moved row was selected or current
            if (rowToMove.Selected)
            {
                // The row might have lost selection during RemoveAt/Insert
                // Ensure it's selected at the new index
                rowToMove.Selected = true;
            }

            if (this.CurrentCell != null && this.CurrentCell.OwningRow.Index == toIndex)
            {
                // CurrentCell might already be correct after the move, but ensure it's valid
                // If the row being moved WAS the current row, CurrentCell should now point to the correct new index
                // If a row ABOVE the current cell was moved down past it, or vice versa, CurrentCell index changes automatically
                // If the moved row WAS the current cell, it's now at toIndex. We just need to ensure it's valid.
                // If the row moved was the current row, CurrentCell.RowIndex will automatically reflect the new index after Insert.
                // However, if the old CurrentCell became invalid (e.g., due to other operations), we might need to reset it.
                // Usually, just ensuring the row is selected and focusing the grid is sufficient.
            }
            // Optionally, explicitly set CurrentCell if needed based on specific logic
            // e.g., if you always want the first cell of the moved row to be current:
            // this.CurrentCell = this.Rows[toIndex].Cells[0]; // Only if desired

            return true;
        }
    }
}