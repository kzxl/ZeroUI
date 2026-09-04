using System;
using ZeroUI.Core.Common;
using ZeroUI.Core.Data;

namespace ZeroUI.Samples.WpfDemo.Data
{
    /// <summary>
    /// Procedural virtual data source for extreme datasets (1,000,000 to 10,000,000+ rows).
    /// Generates cell values completely on-the-fly without keeping multi-gigabyte managed objects in RAM.
    /// </summary>
    public sealed class ZeroProceduralSource : IZeroVirtualSource, IZeroSortableSource
    {
        private readonly int _totalRowCount;

        private static readonly string[] Categories = new[]
        {
            "MCU STM32F407VGT6 LQFP100", "TI TPS54302 Buck 28V 3A", "Winbond Flash 128Mb",
            "Module ESP32-WROOM-32E", "Ceramic Cap 0805 10uF 25V", "Resistor 0603 10kΩ 1%",
            "Power Inductor 10uH 3.5A", "Crystal SMD 16.000MHz", "Schottky Diode SS34 40V 3A",
            "USB Type-C 16-Pin IPX7", "Terminal Block 5.08mm 4P", "Solid State Relay 5V",
            "Solenoid Valve 4V210-08 24V", "Compact Air Cylinder 25mm", "Optical Sensor Keyence PZ-G41N",
            "Hybrid Stepper Nema 23 2.8Nm", "Linear Guide Rail HIWIN HGH20", "Ball Screw SFU1605-600mm",
            "Roller Bearing SKF 6205", "SMT Lead-Free Solder Paste"
        };

        private static readonly string[] Statuses = new[]
        {
            "Passed OQC", "Pending IQC", "Safety Stock", "Inspection Hold", "Restock Needed"
        };

        public ZeroProceduralSource(int totalRowCount = 10000000)
        {
            _totalRowCount = totalRowCount;
        }

        public int TotalRowCount => _totalRowCount;
        public int TotalColumnCount => 11;

        public void GetCellValue(int rowIndex, int columnIndex, ref CellValueBuffer buffer)
        {
            if (rowIndex < 0 || rowIndex >= _totalRowCount) return;

            int catIdx = (int)((uint)rowIndex % (uint)Categories.Length);

            switch (columnIndex)
            {
                case 0:
                    buffer.Text = (rowIndex % 3 != 0) ? "true".AsSpan() : "false".AsSpan();
                    buffer.Alignment = CellAlignment.Center;
                    break;
                case 1:
                    buffer.Text = Categories[catIdx].AsSpan();
                    buffer.Alignment = CellAlignment.Left;
                    break;
                case 2:
                    buffer.Text = (rowIndex + 1).ToString().AsSpan();
                    buffer.Alignment = CellAlignment.Right;
                    break;
                case 3:
                    buffer.Text = $"SKU-{(1000000 + rowIndex % 9000000)}".AsSpan();
                    buffer.Alignment = CellAlignment.Left;
                    break;
                case 4:
                    buffer.Text = Categories[(catIdx + 3) % Categories.Length].AsSpan();
                    buffer.Alignment = CellAlignment.Left;
                    break;
                case 5:
                    int qty = 10 + (int)((uint)(rowIndex * 17) % 15000);
                    buffer.Text = qty.ToString().AsSpan();
                    buffer.Alignment = CellAlignment.Right;
                    break;
                case 6:
                    double price = 500.0 + ((uint)(rowIndex * 31) % 450000);
                    buffer.Text = price.ToString("N0").AsSpan();
                    buffer.Alignment = CellAlignment.Right;
                    break;
                case 7:
                    double total = (10 + ((rowIndex * 17) % 15000)) * (500.0 + ((rowIndex * 31) % 450000));
                    buffer.Text = total.ToString("N0").AsSpan();
                    buffer.Alignment = CellAlignment.Right;
                    break;
                case 8:
                    buffer.DataBarPercent = (70f + ((rowIndex * 7) % 30)) / 100f;
                    buffer.Alignment = CellAlignment.Center;
                    break;
                case 9:
                    buffer.Text = $"LOT-{202600 + (rowIndex % 99):000000}".AsSpan();
                    buffer.Alignment = CellAlignment.Center;
                    break;
                case 10:
                    string status = Statuses[(rowIndex % Statuses.Length)];
                    buffer.Text = status.AsSpan();
                    buffer.Alignment = CellAlignment.Center;
                    break;
            }
        }

        public int CompareRows(int rowA, int rowB, int columnIndex)
        {
            if (rowA == rowB) return 0;
            return columnIndex switch
            {
                0 => (rowA % 3).CompareTo(rowB % 3),
                1 => string.Compare(Categories[rowA % Categories.Length], Categories[rowB % Categories.Length], StringComparison.Ordinal),
                2 => rowA.CompareTo(rowB),
                5 => ((rowA * 17) % 15000).CompareTo((rowB * 17) % 15000),
                6 => ((rowA * 31) % 450000).CompareTo((rowB * 31) % 450000),
                8 => ((rowA * 7) % 30).CompareTo((rowB * 7) % 30),
                _ => rowA.CompareTo(rowB)
            };
        }
    }
}
