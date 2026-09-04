using Xunit;
using ZeroUI.Core.Common;
using ZeroUI.Core.Data;

namespace ZeroUI.Core.Tests
{
    public class CellValidatingEventArgsTests
    {
        [Fact]
        public void CellValidatingEventArgs_InitializesPropertiesCorrectly()
        {
            var args = new CellValidatingEventArgs(
                visualRowIndex: 2,
                modelRowIndex: 10,
                columnIndex: 3,
                oldValue: "100",
                newValue: "-50");

            Assert.Equal(2, args.VisualRowIndex);
            Assert.Equal(10, args.ModelRowIndex);
            Assert.Equal(3, args.ColumnIndex);
            Assert.Equal("100", args.OldValue);
            Assert.Equal("-50", args.NewValue);
            Assert.False(args.Cancel);
            Assert.Null(args.ErrorMessage);
        }

        [Fact]
        public void CellValidatingEventArgs_CanCancelAndSetErrorMessage()
        {
            var args = new CellValidatingEventArgs(0, 0, 1, "A", "B");
            args.Cancel = true;
            args.ErrorMessage = "Value must not be B";

            Assert.True(args.Cancel);
            Assert.Equal("Value must not be B", args.ErrorMessage);
        }

        [Fact]
        public void ZeroColumn_ColumnTypeDefaultsToTextAndCanBeConfigured()
        {
            var col = new ZeroColumn("Test", 100);
            Assert.Equal(GridColumnType.Text, col.ColumnType);

            col.ColumnType = GridColumnType.Numeric;
            Assert.Equal(GridColumnType.Numeric, col.ColumnType);

            col.ColumnType = GridColumnType.Boolean;
            Assert.Equal(GridColumnType.Boolean, col.ColumnType);

            col.ColumnType = GridColumnType.DateTime;
            Assert.Equal(GridColumnType.DateTime, col.ColumnType);
        }
    }
}
