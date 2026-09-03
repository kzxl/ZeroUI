using System;
using ZeroUI.Core.Layout;

namespace ZeroUI.Core.Input
{
    public enum HitRegion : byte
    {
        None = 0,
        Header = 1,
        ColumnResizeGrip = 2,
        Cell = 3,
        RowIndicator = 4
    }

    public readonly struct HitTestResult
    {
        public readonly HitRegion Region;
        public readonly int RowIndex;
        public readonly int ColumnIndex;
        public readonly int ResizeColumnIndex;
        public readonly CellBounds Bounds;

        public HitTestResult(HitRegion region, int rowIndex, int columnIndex, int resizeColIndex, in CellBounds bounds)
        {
            Region = region;
            RowIndex = rowIndex;
            ColumnIndex = columnIndex;
            ResizeColumnIndex = resizeColIndex;
            Bounds = bounds;
        }

        public static HitTestResult Empty => new HitTestResult(HitRegion.None, -1, -1, -1, default);
    }

    public static class SpatialHitTester
    {
        private const int GripTolerance = 4; // +/- 4px around column boundary

        public static HitTestResult HitTest(
            int clientX,
            int clientY,
            int headerHeight,
            int defaultRowHeight,
            int scrollX,
            int scrollY,
            int[] columnWidths,
            int totalCols,
            int totalRows)
        {
            if (clientX < 0 || clientY < 0 || totalCols <= 0 || totalRows <= 0)
            {
                return HitTestResult.Empty;
            }

            // 1. Check Header Region
            if (clientY < headerHeight)
            {
                int currentX = -scrollX;
                for (int c = 0; c < totalCols; c++)
                {
                    int colWidth = columnWidths[c];
                    int colRight = currentX + colWidth;

                    // Check column resize grip at right boundary
                    if (Math.Abs(clientX - colRight) <= GripTolerance)
                    {
                        return new HitTestResult(
                            HitRegion.ColumnResizeGrip,
                            -1,
                            c,
                            c,
                            new CellBounds(currentX, 0, colWidth, headerHeight));
                    }

                    if (clientX >= currentX && clientX < colRight)
                    {
                        return new HitTestResult(
                            HitRegion.Header,
                            -1,
                            c,
                            -1,
                            new CellBounds(currentX, 0, colWidth, headerHeight));
                    }
                    currentX = colRight;
                }
                return HitTestResult.Empty;
            }

            // 2. Check Data Cells Region
            int dataY = clientY - headerHeight;
            int visualRow = (dataY + scrollY) / defaultRowHeight;

            if (visualRow < 0 || visualRow >= totalRows)
            {
                return HitTestResult.Empty;
            }

            int cellY = headerHeight + (visualRow * defaultRowHeight) - scrollY;

            int curX = -scrollX;
            for (int c = 0; c < totalCols; c++)
            {
                int colWidth = columnWidths[c];
                int colRight = curX + colWidth;

                if (clientX >= curX && clientX < colRight)
                {
                    return new HitTestResult(
                        HitRegion.Cell,
                        visualRow,
                        c,
                        -1,
                        new CellBounds(curX, cellY, colWidth, defaultRowHeight));
                }
                curX = colRight;
            }

            return HitTestResult.Empty;
        }
    }
}
