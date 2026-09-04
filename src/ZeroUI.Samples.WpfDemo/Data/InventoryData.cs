using System;
using ZeroUI.Core.Common;
using ZeroUI.Core.Data;

namespace ZeroUI.Samples.WpfDemo.Data
{
    public struct InventoryItem
    {
        public int Id;
        public string ItemCode;
        public string ItemName;
        public int Quantity;
        public double UnitPrice;
        public double TotalAmount;
        public string LotNumber;
        public string Status;

        public InventoryItem(int id, string code, string name, int qty, double price, string lot, string status)
        {
            Id = id;
            ItemCode = code;
            ItemName = name;
            Quantity = qty;
            UnitPrice = price;
            TotalAmount = qty * price;
            LotNumber = lot;
            Status = status;
        }
    }

    public sealed class ZeroWpfInventorySource : IZeroVirtualSource, IZeroSortableSource
    {
        private readonly InventoryItem[] _items;

        public ZeroWpfInventorySource(InventoryItem[] items)
        {
            _items = items;
        }

        public int TotalRowCount => _items.Length;
        public int TotalColumnCount => 8;
        public InventoryItem[] Items => _items;

        public void GetCellValue(int rowIndex, int columnIndex, ref CellValueBuffer buffer)
        {
            if (rowIndex < 0 || rowIndex >= _items.Length) return;
            ref readonly var item = ref _items[rowIndex];

            switch (columnIndex)
            {
                case 0:
                    buffer.Text = item.Id.ToString().AsSpan();
                    buffer.Alignment = CellAlignment.Right;
                    break;
                case 1:
                    buffer.Text = item.ItemCode.AsSpan();
                    buffer.Alignment = CellAlignment.Left;
                    break;
                case 2:
                    buffer.Text = item.ItemName.AsSpan();
                    buffer.Alignment = CellAlignment.Left;
                    break;
                case 3:
                    buffer.Text = item.Quantity.ToString().AsSpan();
                    buffer.Alignment = CellAlignment.Right;
                    break;
                case 4:
                    buffer.Text = item.UnitPrice.ToString("N0").AsSpan();
                    buffer.Alignment = CellAlignment.Right;
                    break;
                case 5:
                    buffer.Text = item.TotalAmount.ToString("N0").AsSpan();
                    buffer.Alignment = CellAlignment.Right;
                    break;
                case 6:
                    buffer.Text = item.LotNumber.AsSpan();
                    buffer.Alignment = CellAlignment.Center;
                    break;
                case 7:
                    buffer.Text = item.Status.AsSpan();
                    buffer.Alignment = CellAlignment.Center;
                    break;
            }
        }

        public int CompareRows(int rowA, int rowB, int columnIndex)
        {
            if (rowA < 0 || rowA >= _items.Length || rowB < 0 || rowB >= _items.Length) return 0;
            ref readonly var a = ref _items[rowA];
            ref readonly var b = ref _items[rowB];

            return columnIndex switch
            {
                0 => a.Id.CompareTo(b.Id),
                1 => string.Compare(a.ItemCode, b.ItemCode, StringComparison.Ordinal),
                2 => string.Compare(a.ItemName, b.ItemName, StringComparison.Ordinal),
                3 => a.Quantity.CompareTo(b.Quantity),
                4 => a.UnitPrice.CompareTo(b.UnitPrice),
                5 => a.TotalAmount.CompareTo(b.TotalAmount),
                6 => string.Compare(a.LotNumber, b.LotNumber, StringComparison.Ordinal),
                7 => string.Compare(a.Status, b.Status, StringComparison.Ordinal),
                _ => 0
            };
        }

        public static InventoryItem[] Generate(int count)
        {
            var items = new InventoryItem[count];
            string[] names = new[]
            {
                "IC Microcontroller STM32F4", "Capacitor 100uF 50V SMD", "Resistor 10k 1% 0805",
                "Inductor 4.7uH Shielded", "MOSFET N-CH 60V 30A", "OLED Display 0.96 inch I2C",
                "Flash Memory SPI 64MB", "Optocoupler PC817 Sharp", "Voltage Regulator AMS1117-3.3",
                "Crystal Oscillator 16MHz", "Relay 12VDC 10A Omron", "Buzzer Piezo 5V Active"
            };

            string[] statuses = new[]
            {
                "Passed OQC", "Pending IQC", "SMT Feeding", "QC Quarantine", "Low Stock"
            };

            for (int i = 0; i < count; i++)
            {
                int id = i + 1;
                string code = $"SKU-{100000 + (id % 900000)}";
                string name = names[id % names.Length];
                int qty = 10 + (id * 17) % 5000;
                double price = 1000 + (id * 31) % 250000;
                string lot = $"LOT-{202600 + (id % 99):000000}";
                string status = statuses[id % statuses.Length];

                items[i] = new InventoryItem(id, code, name, qty, price, lot, status);
            }

            return items;
        }
    }
}
