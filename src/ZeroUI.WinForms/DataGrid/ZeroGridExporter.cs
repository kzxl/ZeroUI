using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ZeroUI.Core.Data;

namespace ZeroUI.WinForms.DataGrid
{
    /// <summary>
    /// High-throughput zero-allocation streaming Excel (.xlsx) and CSV exporter for ZeroUI controls.
    /// Capable of streaming 1,000,000+ rows directly to disk at hundreds of thousands of rows per second.
    /// </summary>
    public static class ZeroGridExporter
    {
        public static Task<int> ExportToXlsxAsync(
            IZeroVirtualSource dataSource,
            ZeroGridControl grid,
            string filePath,
            string sheetName = "Sheet1",
            IProgress<int>? progress = null,
            CancellationToken cancellationToken = default)
        {
            if (grid == null) throw new ArgumentNullException(nameof(grid));
            return GridDataExporter.ExportToXlsxAsync(
                dataSource,
                grid.Columns,
                grid.RowCount,
                filePath,
                row => grid.GetModelRowIndex(row),
                sheetName,
                progress,
                cancellationToken);
        }

        public static Task<int> ExportToCsvAsync(
            IZeroVirtualSource dataSource,
            ZeroGridControl grid,
            string filePath,
            IProgress<int>? progress = null,
            CancellationToken cancellationToken = default)
        {
            if (grid == null) throw new ArgumentNullException(nameof(grid));
            return GridDataExporter.ExportToCsvAsync(
                dataSource,
                grid.Columns,
                grid.RowCount,
                filePath,
                row => grid.GetModelRowIndex(row),
                progress,
                cancellationToken);
        }
    }
}
