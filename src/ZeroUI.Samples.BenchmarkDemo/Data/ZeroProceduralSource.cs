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
    public sealed class ZeroProceduralSource : IZeroVirtualSource, IZeroSortableSource
    {

        private readonly int _totalRowCount;
        private readonly char[] _scratch = new char[64];

        private static readonly string[] Categories = new[]
        {
            "STM32F407VGT6 LQFP100 Microcontroller",
            "TI TPS54302DDCR Buck Converter 28V 3A",
            "Winbond W25Q128JVS 128Mb SPI Flash",
            "ESP32-WROOM-32E Wi-Fi/BLE Module",
            "SMD 0805 10uF 25V X7R Ceramic Capacitor",
            "Precision Thin Film Resistor 0603 10kΩ 1%",
            "SMD Power Inductor 10uH 3.5A Shielded",
            "SMD 3225 16.000MHz Crystal Oscillator",
            "Schottky Rectifier Diode SS34 40V 3A SMC",
            "USB Type-C 16-Pin SMT IPX7 Connector",
            "Phoenix 5.08mm 4P Terminal Block",
            "Omron G3MB-202P 5V Solid State Relay",
            "Airtac 4V210-08 24VDC Solenoid Valve",
            "SMC MGPM25-50Z Compact Pneumatic Cylinder",
            "Keyence PZ-G41N Optical Sensor",
            "Hybrid Stepper Motor Nema 23 2.8Nm",
            "HIWIN HGH20CA Linear Guide Block",
            "TBI Motion SFU1605-600mm Ball Screw",
            "SKF 6205-2RSH/C3 Deep Groove Ball Bearing",
            "Senju M705 SAC305 Lead-Free Solder Paste"
        };

        private static readonly string[] CodePrefixes = new[]
        {
            "IC", "PWR", "MEM", "IOT", "CAP", "RES", "IND", "XTAL", "DIO", "USBC",
            "TERM", "REL", "VAL", "CYL", "SENS", "STEP", "RAIL", "SCREW", "BRG", "SOLD"
        };


        private static readonly string[] Statuses = new[]
        {
            "Passed OQC",
            "Pending Inspection",
            "Safety Stock",
            "In Quarantine",
            "Restock Required"
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

                case 1: // Item Code
                    buffer.Text = (CodePrefixes[catIdx] + "-" + (rowIndex + 1).ToString("D7")).AsSpan();
                    buffer.Alignment = CellAlignment.Left;
                    break;

                case 2: // Item Name
                    buffer.Text = Categories[catIdx].AsSpan();
                    buffer.Alignment = CellAlignment.Left;
                    break;

                case 3: // Quantity
                    buffer.Text = qty.ToString().AsSpan();
                    buffer.Alignment = CellAlignment.Right;
                    break;

                case 4: // Unit Price ($)
                    buffer.Text = unitPrice.ToString("N0").AsSpan();
                    buffer.Alignment = CellAlignment.Right;
                    break;

                case 5: // Total Amount ($)
                    buffer.Text = totalAmount.ToString("N0").AsSpan();
                    buffer.Alignment = CellAlignment.Right;
                    break;

                case 6: // Lot Number
                    buffer.Text = ("LOT-2026-" + ((rowIndex % 9999) + 1).ToString("D4")).AsSpan();
                    buffer.Alignment = CellAlignment.Center;
                    break;

                case 7: // Status
                    buffer.Text = Statuses[statusIdx].AsSpan();
                    buffer.Alignment = CellAlignment.Center;
                    buffer.BackColor = StatusBgColors[statusIdx];
                    buffer.HasCustomBackground = true;
                    break;
            }
        }

        public int CompareRows(int rowA, int rowB, int columnIndex)
        {
            switch (columnIndex)
            {
                case 0:
                case 1:
                    return rowA.CompareTo(rowB);

                case 2:
                    int catA = (rowA ^ 0x5555) % Categories.Length;
                    if (catA < 0) catA = -catA;
                    int catB = (rowB ^ 0x5555) % Categories.Length;
                    if (catB < 0) catB = -catB;
                    return string.Compare(Categories[catA], Categories[catB], StringComparison.OrdinalIgnoreCase);

                case 3:
                    long qtyA = ((long)rowA * 37) % 2500 + 1;
                    long qtyB = ((long)rowB * 37) % 2500 + 1;
                    return qtyA.CompareTo(qtyB);

                case 4:
                    double priceA = (((rowA * 17) % 500) + 15) * 1000.0;
                    double priceB = (((rowB * 17) % 500) + 15) * 1000.0;
                    return priceA.CompareTo(priceB);

                case 5:
                    long qA = ((long)rowA * 37) % 2500 + 1;
                    double pA = (((rowA * 17) % 500) + 15) * 1000.0;
                    long qB = ((long)rowB * 37) % 2500 + 1;
                    double pB = (((rowB * 17) % 500) + 15) * 1000.0;
                    return (qA * pA).CompareTo(qB * pB);

                case 6:
                    int lotA = (rowA % 9999) + 1;
                    int lotB = (rowB % 9999) + 1;
                    return lotA.CompareTo(lotB);

                case 7:
                    int statA = (rowA ^ 0xAAAA) % Statuses.Length;
                    if (statA < 0) statA = -statA;
                    int statB = (rowB ^ 0xAAAA) % Statuses.Length;
                    if (statB < 0) statB = -statB;
                    return string.Compare(Statuses[statA], Statuses[statB], StringComparison.OrdinalIgnoreCase);

                default:
                    return rowA.CompareTo(rowB);
            }
        }
    }
}

