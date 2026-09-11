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

        [Fact]
        public void TokenItem_ConstructorsAndEquality_WorkCorrectly()
        {
            var t1 = new TokenItem("Sensor-A1");
            var t2 = new TokenItem("sensor-a1", 101, "📡", "#10B981");
            var t3 = new TokenItem("Sensor-B2");

            Assert.Equal("Sensor-A1", t1.Text);
            Assert.Equal("Sensor-A1", t1.ToString());
            Assert.Equal(101, t2.Value);
            Assert.Equal("📡", t2.Glyph);
            Assert.Equal("#10B981", t2.ColorHex);

            // Case-insensitive token equality
            Assert.True(t1.Equals(t2));
            Assert.True(t1 == t2);
            Assert.False(t1 == t3);
            Assert.Equal(t1.GetHashCode(), t2.GetHashCode());
        }

        [Fact]
        public void TokenChangedEventArgs_InitializedCorrectly()
        {
            var token = new TokenItem("Priority-Hot");
            var args = new TokenChangedEventArgs(TokenChangeAction.Added, token);

            Assert.Equal(TokenChangeAction.Added, args.Action);
            Assert.Equal(token, args.Item);
        }

        [Fact]
        public void ColorPalette_StandardPalette_ContainsValidHexColors()
        {
            Assert.NotEmpty(ColorPalette.StandardPalette);
            Assert.True(ColorPalette.StandardPalette.Count >= 20);

            foreach (var hex in ColorPalette.StandardPalette)
            {
                Assert.StartsWith("#", hex);
                Assert.True(hex.Length == 7 || hex.Length == 9);
            }
        }

        [Fact]
        public void ColorPalette_IndustrialStatusPalette_ContainsExpectedColors()
        {
            Assert.NotEmpty(ColorPalette.IndustrialStatusPalette);
            Assert.Contains("#10B981", ColorPalette.IndustrialStatusPalette); // Running / Normal
            Assert.Contains("#EF4444", ColorPalette.IndustrialStatusPalette); // Alarm / Critical
        }

        [Fact]
        public void TimeSpanModel_SteppingAndClamping_WorkCorrectly()
        {
            var model = new TimeSpanModel(TimeSpan.FromHours(2))
            {
                Minimum = TimeSpan.Zero,
                Maximum = TimeSpan.FromDays(5)
            };

            Assert.Equal(TimeSpan.FromHours(2), model.Value);

            // Step Hours
            model.FocusedPart = TimeSpanPart.Hours;
            model.StepUp();
            Assert.Equal(TimeSpan.FromHours(3), model.Value);

            model.StepDown();
            Assert.Equal(TimeSpan.FromHours(2), model.Value);

            // Step Minutes
            model.FocusedPart = TimeSpanPart.Minutes;
            model.StepUp();
            Assert.Equal(new TimeSpan(2, 1, 0), model.Value);

            // Bounds clamping
            model.Value = TimeSpan.FromDays(10);
            Assert.Equal(TimeSpan.FromDays(5), model.Value); // Clamped to maximum

            model.Value = TimeSpan.FromDays(-1);
            Assert.Equal(TimeSpan.Zero, model.Value); // Clamped to minimum
        }

        [Fact]
        public void TimeSpanModel_FormattingAndParsing_WorkCorrectly()
        {
            var ts = new TimeSpan(1, 2, 30, 45); // 1d 02:30:45
            string clock = TimeSpanModel.FormatTimeSpan(ts, TimeSpanFormatMode.Clock, showDays: true);
            Assert.Contains("1d", clock);
            Assert.Contains("02:30:45", clock);

            string verbose = TimeSpanModel.FormatTimeSpan(ts, TimeSpanFormatMode.Verbose, showDays: true);
            Assert.Contains("1d", verbose);
            Assert.Contains("2h", verbose);
            Assert.Contains("30m", verbose);
            Assert.Contains("45s", verbose);

            // Parsing shorthand
            Assert.True(TimeSpanModel.TryParse("90s", out var parsedSec));
            Assert.Equal(TimeSpan.FromSeconds(90), parsedSec);

            Assert.True(TimeSpanModel.TryParse("15m", out var parsedMin));
            Assert.Equal(TimeSpan.FromMinutes(15), parsedMin);

            Assert.True(TimeSpanModel.TryParse("2h", out var parsedHr));
            Assert.Equal(TimeSpan.FromHours(2), parsedHr);

            Assert.True(TimeSpanModel.TryParse("3d", out var parsedDay));
            Assert.Equal(TimeSpan.FromDays(3), parsedDay);

            Assert.True(TimeSpanModel.TryParse("01:15:30", out var parsedClock));
            Assert.Equal(new TimeSpan(1, 15, 30), parsedClock);
        }

        [Fact]
        public void HyperlinkModel_DetectKindAndArgs_WorkCorrectly()
        {
            Assert.Equal(HyperlinkEditKind.Web, HyperlinkHelper.DetectKind("https://zeroui.io"));
            Assert.Equal(HyperlinkEditKind.Email, HyperlinkHelper.DetectKind("support@zeroui.io"));
            Assert.Equal(HyperlinkEditKind.Email, HyperlinkHelper.DetectKind("mailto:admin@zeroui.io"));
            Assert.Equal(HyperlinkEditKind.File, HyperlinkHelper.DetectKind("C:\\ZeroPlatform\\Config.json"));

            Assert.True(HyperlinkHelper.TryCreateUri("zeroui.io", out var uri));
            Assert.NotNull(uri);
            Assert.Equal("https", uri!.Scheme);

            var args = new HyperlinkNavigateEventArgs("https://zeroui.io", uri);
            Assert.False(args.Cancel);
            Assert.False(args.Handled);
            args.Cancel = true;
            Assert.True(args.Cancel);
        }
    }
}
