using System;
using ZeroUI.Core.Common;
using ZeroUI.Core.Data;

namespace ZeroUI.Samples.BenchmarkDemo.Data
{
    public sealed class ZeroInventorySource : IZeroVirtualSource, IZeroSortableSource
    {

        private readonly InventoryItem[] _items;
        // Small thread-local or instance scratch buffers for zero-alloc formatting
        private readonly char[] _formatBuffer = new char[64];

        public ZeroInventorySource(InventoryItem[] items)
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
                case 0: // ID
                    buffer.Text = item.Id.ToString().AsSpan();
                    buffer.Alignment = CellAlignment.Right;
                    break;

                case 1: // Item Code
                    buffer.Text = item.ItemCode.AsSpan();
                    buffer.Alignment = CellAlignment.Left;
                    break;

                case 2: // Item Name
                    buffer.Text = item.ItemName.AsSpan();
                    buffer.Alignment = CellAlignment.Left;
                    break;

                case 3: // Quantity
                    buffer.Text = item.Quantity.ToString().AsSpan();
                    buffer.Alignment = CellAlignment.Right;
                    break;

                case 4: // Unit Price
                    buffer.Text = item.UnitPrice.ToString("N0").AsSpan();
                    buffer.Alignment = CellAlignment.Right;
                    break;

                case 5: // Total Amount
                    buffer.Text = item.TotalAmount.ToString("N0").AsSpan();
                    buffer.Alignment = CellAlignment.Right;
                    break;

                case 6: // Lot Number
                    buffer.Text = item.LotNumber.AsSpan();
                    buffer.Alignment = CellAlignment.Center;
                    break;

                case 7: // Status
                    buffer.Text = item.Status.AsSpan();
                    buffer.Alignment = CellAlignment.Center;
                    // Colors in Win32 0x00BBGGRR format
                    if (item.Status == "Passed OQC" || item.Status == "In Warehouse" || item.Status == "Completed")
                    {
                        buffer.TextColor = 0x002E7D32; // Green
                    }
                    else if (item.Status == "Pending IQC" || item.Status == "Pending Inspection")
                    {
                        buffer.TextColor = 0x000080FF; // Amber / Orange
                    }
                    else if (item.Status == "SMT Feeding")
                    {
                        buffer.TextColor = 0x00B05010; // Blue
                    }
                    else if (item.Status == "QC Quarantine" || item.Status == "On Hold")
                    {
                        buffer.TextColor = 0x00802080; // Purple
                    }
                    else if (item.Status == "Low Stock Warning")
                    {
                        buffer.TextColor = 0x002020D0; // Red
                    }
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
                1 => string.Compare(a.ItemCode, b.ItemCode, StringComparison.OrdinalIgnoreCase),
                2 => string.Compare(a.ItemName, b.ItemName, StringComparison.OrdinalIgnoreCase),
                3 => a.Quantity.CompareTo(b.Quantity),
                4 => a.UnitPrice.CompareTo(b.UnitPrice),
                5 => a.TotalAmount.CompareTo(b.TotalAmount),
                6 => string.Compare(a.LotNumber, b.LotNumber, StringComparison.OrdinalIgnoreCase),
                7 => string.Compare(a.Status, b.Status, StringComparison.OrdinalIgnoreCase),
                _ => 0
            };
        }
    }
}

