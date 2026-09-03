using System;
using ZeroUI.Core.Common;
using ZeroUI.Core.Data;

namespace ZeroUI.Samples.BenchmarkDemo.Data
{
    /// <summary>
    /// Procedural virtual data source for extreme datasets (1,000,000 to 10,000,000+ rows).
    /// Generates cell values completely on-the-fly without keeping multi-gigabyte managed objects in RAM.
    /// Memory footprint is virtually ZERO beyond the grid's own index map.
    /// </summary>
    public sealed class ZeroProceduralSource : IZeroVirtualSource
    {
        private readonly int _totalRowCount;
        private readonly char[] _scratch = new char[64];

        private static readonly string[] Categories = new[]
        {
            "Thép hình I-Beam 200x100",
            "Nhôm đúc định hình AL6063",
            "Bulong cường độ cao M16x80",
            "Tấm lót cao su EPDM 5mm",
            "Vòng bi công nghiệp SKF-6205",
            "Dây hàn Mig 1.2mm Cu-Coated",
            "Cáp điện hạ thế 3x16+1x10mm",
            "Ống thép không gỉ SUS304",
            "Bản mã hàn gia cường 10mm",
            "Sơn chống rỉ Epoxy 2 thành phần"
        };

        private static readonly string[] Statuses = new[]
        {
            "Đạt tiêu chuẩn",
            "Chờ nghiệm thu",
            "Tồn kho an toàn",
            "Hàng kiểm định",
            "Cần bổ sung"
        };

        private static readonly uint[] StatusBgColors = new uint[]
        {
            0x00E8F8E8, // Soft Green (Win32 BGR)
            0x00FFF0E0, // Soft Orange/Yellow
            0x00F0F0FF, // Soft Blue
            0x00F8E8F8, // Soft Purple
            0x00E8E8FF  // Soft Red
        };

        public ZeroProceduralSource(int totalRowCount)
        {
            _totalRowCount = Math.Max(0, totalRowCount);
        }

        public int TotalRowCount => _totalRowCount;
        public int TotalColumnCount => 8;

        public void GetCellValue(int rowIndex, int columnIndex, ref CellValueBuffer buffer)
        {
            if (rowIndex < 0 || rowIndex >= _totalRowCount) return;

            int catIdx = (rowIndex ^ 0x5555) % Categories.Length;
            if (catIdx < 0) catIdx = -catIdx;

            int statusIdx = (rowIndex ^ 0xAAAA) % Statuses.Length;
            if (statusIdx < 0) statusIdx = -statusIdx;

            long qty = ((long)rowIndex * 37) % 2500 + 1;
            double unitPrice = (((rowIndex * 17) % 500) + 15) * 1000.0;
            double totalAmount = qty * unitPrice;

            switch (columnIndex)
            {
                case 0: // ID
                    buffer.Text = (rowIndex + 1).ToString().AsSpan();
                    buffer.Alignment = CellAlignment.Right;
                    break;

                case 1: // Mã Vật Tư
                    buffer.Text = ("VT-" + (rowIndex + 1).ToString("D8")).AsSpan();
                    buffer.Alignment = CellAlignment.Left;
                    break;

                case 2: // Tên Vật Tư
                    buffer.Text = Categories[catIdx].AsSpan();
                    buffer.Alignment = CellAlignment.Left;
                    break;

                case 3: // Số Lượng
                    buffer.Text = qty.ToString().AsSpan();
                    buffer.Alignment = CellAlignment.Right;
                    break;

                case 4: // Đơn Giá (VNĐ)
                    buffer.Text = unitPrice.ToString("N0").AsSpan();
                    buffer.Alignment = CellAlignment.Right;
                    break;

                case 5: // Thành Tiền (VNĐ)
                    buffer.Text = totalAmount.ToString("N0").AsSpan();
                    buffer.Alignment = CellAlignment.Right;
                    break;

                case 6: // Số Lô
                    buffer.Text = ("LOT-2026-" + ((rowIndex % 9999) + 1).ToString("D4")).AsSpan();
                    buffer.Alignment = CellAlignment.Center;
                    break;

                case 7: // Trạng Thái
                    buffer.Text = Statuses[statusIdx].AsSpan();
                    buffer.Alignment = CellAlignment.Center;
                    buffer.BackColor = StatusBgColors[statusIdx];
                    buffer.HasCustomBackground = true;
                    break;
            }
        }
    }
}
