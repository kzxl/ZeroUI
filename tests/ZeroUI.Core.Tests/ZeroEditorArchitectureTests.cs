using System;
using System.Collections.Generic;
using Xunit;
using ZeroUI.Core.Data;
using ZeroUI.Core.Editors;

namespace ZeroUI.Core.Tests
{
    public class ZeroEditorArchitectureTests
    {
        private class MockTypedEditor<T> : IZeroEditor<T> where T : notnull
        {
            private T _value = default!;
            private bool _isModified;
            private bool _readOnly;

            public event EventHandler<ValueChangingEventArgs<T>>? ValueChanging;
            public event EventHandler? ValueChanged;
            public event EventHandler? EditValueChanged;

            public T Value
            {
                get => _value;
                set
                {
                    if (EqualityComparer<T>.Default.Equals(_value, value)) return;

                    var args = new ValueChangingEventArgs<T>(_value, value);
                    ValueChanging?.Invoke(this, args);
                    if (args.Cancel) return;

                    _value = value;
                    _isModified = true;
                    ValueChanged?.Invoke(this, EventArgs.Empty);
                    EditValueChanged?.Invoke(this, EventArgs.Empty);
                }
            }

            public object? EditValue
            {
                get => _value;
                set
                {
                    if (value == null)
                    {
                        Value = default!;
                    }
                    else if (value is T typed)
                    {
                        Value = typed;
                    }
                    else
                    {
                        try
                        {
                            Value = (T)Convert.ChangeType(value, typeof(T));
                        }
                        catch
                        {
                            Value = default!;
                        }
                    }
                }
            }

            public bool IsModified
            {
                get => _isModified;
                set => _isModified = value;
            }

            public bool ReadOnly
            {
                get => _readOnly;
                set => _readOnly = value;
            }

            public void Reset()
            {
                _value = default!;
                _isModified = false;
                ValueChanged?.Invoke(this, EventArgs.Empty);
                EditValueChanged?.Invoke(this, EventArgs.Empty);
            }

            public void Clear() => Reset();
        }

        [Fact]
        public void TypedEditor_ValueChanging_CanCancelMutation()
        {
            var editor = new MockTypedEditor<int>();
            editor.Value = 10;
            Assert.Equal(10, editor.Value);

            // Cancel any value > 100
            editor.ValueChanging += (s, e) =>
            {
                if (e.NewValue > 100)
                {
                    e.Cancel = true;
                }
            };

            editor.Value = 50;
            Assert.Equal(50, editor.Value);

            editor.Value = 150; // Should be cancelled
            Assert.Equal(50, editor.Value);
        }

        [Fact]
        public void TypedEditor_EditValue_BridgesToTypedValue()
        {
            var editor = new MockTypedEditor<decimal>();
            editor.EditValue = "123.45";

            Assert.Equal(123.45m, editor.Value);
            Assert.True(editor.IsModified);

            editor.Reset();
            Assert.Equal(0m, editor.Value);
            Assert.False(editor.IsModified);
        }

        [Fact]
        public void ZeroClipboardHelper_FormatTsv_GeneratesValidTsv()
        {
            var matrix = new string[,]
            {
                { "ID", "Name", "Score" },
                { "101", "Alice", "95.5" },
                { "102", "Bob", "88.0" }
            };

            string tsv = ZeroClipboardHelper.FormatTsv(matrix);

            Assert.Equal("ID\tName\tScore\r\n101\tAlice\t95.5\r\n102\tBob\t88.0", tsv);
        }

        [Fact]
        public void ZeroClipboardHelper_ParseTsv_ParsesZeroAllocCellsCorrectly()
        {
            string tsv = "Header1\tHeader2\r\nVal1\tVal2\r\nVal3\tVal4";
            var parsed = new List<(int row, int col, string val)>();

            ZeroClipboardHelper.ParseTsv(tsv.AsSpan(), (row, col, cellSpan) =>
            {
                parsed.Add((row, col, cellSpan.ToString()));
            });

            Assert.Equal(6, parsed.Count);
            Assert.Equal((0, 0, "Header1"), parsed[0]);
            Assert.Equal((0, 1, "Header2"), parsed[1]);
            Assert.Equal((1, 0, "Val1"), parsed[2]);
            Assert.Equal((1, 1, "Val2"), parsed[3]);
            Assert.Equal((2, 0, "Val3"), parsed[4]);
            Assert.Equal((2, 1, "Val4"), parsed[5]);
        }

        [Fact]
        public void EditorButtonModel_DefaultProperties_InitializedCorrectly()
        {
            var btn = new EditorButtonModel(EditorButtonKind.BrowseFile);

            Assert.Equal(EditorButtonKind.BrowseFile, btn.Kind);
            Assert.True(btn.Visible);
            Assert.True(btn.IsEnabled);
            Assert.Equal("Browse file...", btn.ToolTip);
        }

        [Theory]
        [InlineData(EditorButtonKind.BrowseFile, "Browse file...")]
        [InlineData(EditorButtonKind.BrowseFolder, "Browse folder...")]
        [InlineData(EditorButtonKind.Clear, "Clear input")]
        [InlineData(EditorButtonKind.Copy, "Copy to clipboard")]
        [InlineData(EditorButtonKind.Search, "Search")]
        [InlineData(EditorButtonKind.DropDown, "Show options")]
        [InlineData(EditorButtonKind.Undo, "Undo")]
        [InlineData(EditorButtonKind.Redo, "Redo")]
        [InlineData(EditorButtonKind.Custom, "")]
        public void EditorButtonModel_GetDefaultToolTip_ReturnsExpectedText(EditorButtonKind kind, string expected)
        {
            string tooltip = EditorButtonModel.GetDefaultToolTip(kind);
            Assert.Equal(expected, tooltip);
        }

        [Fact]
        public void EditorButtonClickEventArgs_HandledFlag_CanBeSet()
        {
            var args = new EditorButtonClickEventArgs(EditorButtonKind.Clear, "tag_123");

            Assert.Equal(EditorButtonKind.Clear, args.Kind);
            Assert.Equal("tag_123", args.Tag);
            Assert.False(args.Handled);

            args.Handled = true;
            Assert.True(args.Handled);
        }
    }
}
