using System;
using ZeroUI.Core.Common;
using ZeroUI.Core.Data;

namespace ZeroUI.Samples.BenchmarkDemo.Data
{
    public sealed class ZeroInventorySource : IZeroVirtualSource
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

                case 1: // Mã Vật Tư
                    buffer.Text = item.ItemCode.AsSpan();
                    buffer.Alignment = CellAlignment.Left;
                    break;

                case 2: // Tên Vật Tư
                    buffer.Text = item.ItemName.AsSpan();
                    buffer.Alignment = CellAlignment.Left;
                    break;

                case 3: // Số Lượng
                    buffer.Text = item.Quantity.ToString().AsSpan();
                    buffer.Alignment = CellAlignment.Right;
                    break;

                case 4: // Đơn Giá
                    buffer.Text = item.UnitPrice.ToString("N0").AsSpan();
                    buffer.Alignment = CellAlignment.Right;
                    break;

                case 5: // Thành Tiền
                    buffer.Text = item.TotalAmount.ToString("N0").AsSpan();
                    buffer.Alignment = CellAlignment.Right;
                    break;

                case 6: // Số Lô
                    buffer.Text = item.LotNumber.AsSpan();
                    buffer.Alignment = CellAlignment.Center;
                    break;

                case 7: // Trạng Thái
                    buffer.Text = item.Status.AsSpan();
                    buffer.Alignment = CellAlignment.Center;
                    // Colors in Win32 0x00BBGGRR format
                    if (item.Status == "Đã nhập kho" || item.Status == "Hoàn thành")
                    {
                        buffer.TextColor = 0x002E7D32; // Green
                    }
                    else if (item.Status == "Chờ kiểm tra")
                    {
                        buffer.TextColor = 0x000080FF; // Orange
                    }
                    else if (item.Status == "Tạm giữ")
                    {
                        buffer.TextColor = 0x002020D0; // Red
                    }
                    break;
            }
        }
    }
}
