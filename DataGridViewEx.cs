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

            // Optional: Enable additional styles for smoother rendering
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            SetStyle(ControlStyles.SupportsTransparentBackColor, true);
        }

        // Override CreateParams for extra stability on Windows 10/11
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
            if (fromIndex < 0 || fromIndex >= Rows.Count ||
                toIndex < 0 || toIndex >= Rows.Count)
            {
                return false; // Index out of bounds
            }

            if (fromIndex == toIndex)
            {
                return true; // No move needed
            }

            // Suspend painting to prevent flickering during the move
            SuspendLayout();

            try
            {
                // Get the row to move
                DataGridViewRow rowToMove = Rows[fromIndex];

                // Store CurrentCell info before moving
                DataGridViewCell? previousCurrentCell = CurrentCell;
                int previousCurrentColumnIndex = previousCurrentCell?.ColumnIndex ?? -1;

                // Remove the row from its current position
                Rows.RemoveAt(fromIndex);

                // Insert the row at the new position
                Rows.Insert(toIndex, rowToMove);

                // Preserve selection
                if (rowToMove.Selected)
                {
                    rowToMove.Selected = true;
                }

                // Restore CurrentCell if it was on the moved row
                if (previousCurrentCell != null && previousCurrentCell.RowIndex == fromIndex)
                {
                    if (previousCurrentColumnIndex >= 0 &&
                        previousCurrentColumnIndex < Rows[toIndex].Cells.Count)
                    {
                        CurrentCell = Rows[toIndex].Cells[previousCurrentColumnIndex];
                    }
                }
                // If CurrentCell was on a row that shifted due to the move, update it
                else if (previousCurrentCell != null)
                {
                    int newRowIndex = previousCurrentCell.RowIndex;

                    // Adjust index if the row shifted
                    if (fromIndex < toIndex)
                    {
                        // Moving down: rows between fromIndex and toIndex shift up
                        if (previousCurrentCell.RowIndex > fromIndex &&
                            previousCurrentCell.RowIndex <= toIndex)
                        {
                            newRowIndex = previousCurrentCell.RowIndex - 1;
                        }
                    }
                    else
                    {
                        // Moving up: rows between toIndex and fromIndex shift down
                        if (previousCurrentCell.RowIndex >= toIndex &&
                            previousCurrentCell.RowIndex < fromIndex)
                        {
                            newRowIndex = previousCurrentCell.RowIndex + 1;
                        }
                    }

                    if (newRowIndex >= 0 && newRowIndex < Rows.Count &&
                        previousCurrentColumnIndex >= 0 &&
                        previousCurrentColumnIndex < Rows[newRowIndex].Cells.Count)
                    {
                        CurrentCell = Rows[newRowIndex].Cells[previousCurrentColumnIndex];
                    }
                }

                return true;
            }
            finally
            {
                // Resume painting
                ResumeLayout();
            }
        }
    }
}