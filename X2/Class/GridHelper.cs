using System;
using System.Windows.Forms;

namespace X2.Class
{
    internal static class GridHelper
    {
        public static void HideIdColumns(DataGridView grid)
        {
            if (grid == null)
                return;

            foreach (DataGridViewColumn column in grid.Columns)
            {
                if (column.Name.StartsWith("ID", StringComparison.OrdinalIgnoreCase))
                    column.Visible = false;
            }
        }
    }
}
