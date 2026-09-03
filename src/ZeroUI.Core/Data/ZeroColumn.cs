using ZeroUI.Core.Common;

namespace ZeroUI.Core.Data
{
    public sealed class ZeroColumn
    {
        public string HeaderText { get; set; } = string.Empty;
        public int Width { get; set; } = 100;
        public int MinWidth { get; set; } = 30;
        public int MaxWidth { get; set; } = 1200;
        public CellAlignment Alignment { get; set; } = CellAlignment.Left;
        public SortDirection SortOrder { get; set; } = SortDirection.None;
        public bool IsVisible { get; set; } = true;

        public ZeroColumn() { }

        public ZeroColumn(string headerText, int width = 100, CellAlignment alignment = CellAlignment.Left)
        {
            HeaderText = headerText;
            Width = width;
            Alignment = alignment;
        }
    }
}
