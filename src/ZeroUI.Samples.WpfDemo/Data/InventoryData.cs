using System;
using ZeroUI.Core.Common;
using ZeroUI.Core.Data;

namespace ZeroUI.Samples.WpfDemo.Data
{
    public struct InventoryItem
    {
        public bool IsActive;
        public string Category;
        public int Id;
        public string ItemCode;
        public string ItemName;
        public int Quantity;
        public double UnitPrice;
        public double TotalAmount;
        public float YieldRate;
        public string LotNumber;
        public string Status;

        public InventoryItem(bool active, string category, int id, string code, string name, int qty, double price, float yieldRate, string lot, string status)
        {
            IsActive = active;
            Category = category;
            Id = id;
            ItemCode = code;
            ItemName = name;
            Quantity = qty;
            UnitPrice = price;
            TotalAmount = qty * price;
            YieldRate = yieldRate;
            LotNumber = lot;
            Status = status;
        }
    }

    public sealed class ZeroWpfInventorySource : IZeroVirtualSource, IZeroSortableSource, IZeroEditableSource
    {
        private readonly InventoryItem[] _items;

        public ZeroWpfInventorySource(InventoryItem[] items)
        {
            _items = items;
        }

        public int TotalRowCount => _items.Length;
        public int TotalColumnCount => 11;
        public InventoryItem[] Items => _items;

        public void GetCellValue(int rowIndex, int columnIndex, ref CellValueBuffer buffer)
        {
            if (rowIndex < 0 || rowIndex >= _items.Length) return;
            ref readonly var item = ref _items[rowIndex];

            switch (columnIndex)
            {
                case 0:
                    buffer.Text = item.IsActive ? "true".AsSpan() : "false".AsSpan();
                    buffer.Alignment = CellAlignment.Center;
                    break;
                case 1:
                    buffer.Text = item.Category.AsSpan();
                    buffer.Alignment = CellAlignment.Left;
                    break;
                case 2:
                    buffer.Text = item.Id.ToString().AsSpan();
                    buffer.Alignment = CellAlignment.Right;
                    break;
                case 3:
                    buffer.Text = item.ItemCode.AsSpan();
                    buffer.Alignment = CellAlignment.Left;
                    break;
                case 4:
                    buffer.Text = item.ItemName.AsSpan();
                    buffer.Alignment = CellAlignment.Left;
                    break;
                case 5:
                    buffer.Text = item.Quantity.ToString().AsSpan();
                    buffer.Alignment = CellAlignment.Right;
                    break;
                case 6:
                    buffer.Text = item.UnitPrice.ToString("N0").AsSpan();
                    buffer.Alignment = CellAlignment.Right;
                    break;
                case 7:
                    buffer.Text = item.TotalAmount.ToString("N0").AsSpan();
                    buffer.Alignment = CellAlignment.Right;
                    break;
                case 8:
                    buffer.DataBarPercent = item.YieldRate;
                    buffer.Alignment = CellAlignment.Center;
                    break;
                case 9:
                    buffer.Text = item.LotNumber.AsSpan();
                    buffer.Alignment = CellAlignment.Center;
                    break;
                case 10:
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
                0 => a.IsActive.CompareTo(b.IsActive),
                1 => string.Compare(a.Category, b.Category, StringComparison.Ordinal),
                2 => a.Id.CompareTo(b.Id),
                3 => string.Compare(a.ItemCode, b.ItemCode, StringComparison.Ordinal),
                4 => string.Compare(a.ItemName, b.ItemName, StringComparison.Ordinal),
                5 => a.Quantity.CompareTo(b.Quantity),
                6 => a.UnitPrice.CompareTo(b.UnitPrice),
                7 => a.TotalAmount.CompareTo(b.TotalAmount),
                8 => a.YieldRate.CompareTo(b.YieldRate),
                9 => string.Compare(a.LotNumber, b.LotNumber, StringComparison.Ordinal),
                10 => string.Compare(a.Status, b.Status, StringComparison.Ordinal),
                _ => 0
            };
        }

        public bool IsCellEditable(int rowIndex, int columnIndex)
        {
            // Allow editing: 0: Active, 4: ItemName, 5: Quantity, 6: UnitPrice, 8: YieldRate, 9: LotNumber, 10: Status
            return columnIndex is 0 or 4 or 5 or 6 or 8 or 9 or 10;
        }

        public bool SetCellValue(int rowIndex, int columnIndex, string? newValue)
        {
            if (rowIndex < 0 || rowIndex >= _items.Length) return false;
            ref var item = ref _items[rowIndex];

            switch (columnIndex)
            {
                case 0:
                    if (bool.TryParse(newValue, out bool active))
                    {
                        item.IsActive = active;
                        return true;
                    }
                    return false;
                case 4:
                    item.ItemName = newValue ?? string.Empty;
                    return true;
                case 5:
                    if (int.TryParse(newValue, out int qty))
                    {
                        item.Quantity = qty;
                        item.TotalAmount = qty * item.UnitPrice;
                        return true;
                    }
                    return false;
                case 6:
                    if (double.TryParse(newValue, out double price))
                    {
                        item.UnitPrice = price;
                        item.TotalAmount = item.Quantity * price;
                        return true;
                    }
                    return false;
                case 8:
                    if (float.TryParse(newValue?.Replace("%", ""), out float yield))
                    {
                        item.YieldRate = Math.Clamp(yield > 1f ? yield / 100f : yield, 0f, 1f);
                        return true;
                    }
                    return false;
                case 9:
                    item.LotNumber = newValue ?? string.Empty;
                    return true;
                case 10:
                    item.Status = newValue ?? string.Empty;
                    return true;
                default:
                    return false;
            }
        }

        public static InventoryItem[] Generate(int count)
        {
            var items = new InventoryItem[count];
            string[] categories = new[]
            {
                "Semiconductors", "Passive Components", "Optoelectronics",
                "Electromechanical", "Power Modules", "Interconnects"
            };

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
                bool active = (id % 3) != 0;
                string category = categories[id % categories.Length];
                string code = $"SKU-{100000 + (id % 900000)}";
                string name = names[id % names.Length];
                int qty = 10 + (id * 17) % 5000;
                double price = 1000 + (id * 31) % 250000;
                float yieldRate = (70f + (id * 7) % 30) / 100f;
                string lot = $"LOT-{202600 + (id % 99):000000}";
                string status = statuses[id % statuses.Length];

                items[i] = new InventoryItem(active, category, id, code, name, qty, price, yieldRate, lot, status);
            }

            return items;
        }
    }
}
