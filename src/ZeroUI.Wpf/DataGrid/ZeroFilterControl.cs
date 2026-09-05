using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ZeroUI.Core.Data;
using ZeroUI.Core.Localization;
using ZeroUI.Wpf.Theme;

namespace ZeroUI.Wpf.DataGrid
{
    /// <summary>
    /// Modern Enterprise Visual Filter / Query Builder control for ZeroUI WPF.
    /// Provides an interactive hierarchical tree allowing users to construct complex
    /// multi-level logical filter expressions [AND/OR] with type-safe operators.
    /// </summary>
    public class FilterControl : Control
    {
        private readonly GroupFilterNode _rootGroup = new GroupFilterNode(FilterGroupOperator.And);
        private readonly List<string> _availableFields = new List<string>();
        private StackPanel? _treeStack;

        public event EventHandler? FilterChanged;

        public GroupFilterNode RootGroup => _rootGroup;
        public List<string> AvailableFields => _availableFields;

        static FilterControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(FilterControl), new FrameworkPropertyMetadata(typeof(FilterControl)));
        }

        public FilterControl()
        {
            Background = ZeroWpfTheme.BgCard;
            BorderBrush = ZeroWpfTheme.BorderDefault;
            BorderThickness = new Thickness(1);
            Width = 560;
            Height = 280;

            ZeroLocalizer.CultureChanged += (s, e) => RebuildTreeUI();

            _rootGroup.AddCondition("Status", FilterComparisonOperator.Equals, "Active");
            BuildVisualTemplate();
        }

        private void BuildVisualTemplate()
        {
            var border = new Border
            {
                Background = Background,
                BorderBrush = BorderBrush,
                BorderThickness = BorderThickness,
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(10)
            };

            var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
            _treeStack = new StackPanel();
            scroll.Content = _treeStack;

            border.Child = scroll;
            AddVisualChild(border);
            AddLogicalChild(border);

            RebuildTreeUI();
        }

        public void SetColumns(IEnumerable<ZeroColumn> columns)
        {
            _availableFields.Clear();
            if (columns != null)
            {
                foreach (var c in columns)
                {
                    string name = !string.IsNullOrEmpty(c.FieldName) ? c.FieldName : c.HeaderText;
                    if (!string.IsNullOrEmpty(name)) _availableFields.Add(name);
                }
            }
            RebuildTreeUI();
        }

        public void RebuildTreeUI()
        {
            if (_treeStack == null) return;
            _treeStack.Children.Clear();
            RenderGroupNode(_rootGroup, 0);
        }

        private void RenderGroupNode(GroupFilterNode group, int indentLevel)
        {
            var grpRow = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(indentLevel * 24, 2, 0, 4)
            };

            // [AND/OR] Toggle Button
            var btnOp = new Button
            {
                Content = group.Operator.GetLocalizedName(),
                FontWeight = FontWeights.Bold,
                Width = 72,
                Height = 26,
                Background = group.Operator == FilterGroupOperator.Or ? ZeroWpfTheme.WarningAccent : ZeroWpfTheme.PrimaryAccent,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 6, 0)
            };
            btnOp.Click += (s, e) =>
            {
                group.Operator = (group.Operator == FilterGroupOperator.And) ? FilterGroupOperator.Or : FilterGroupOperator.And;
                btnOp.Content = group.Operator.GetLocalizedName();
                btnOp.Background = group.Operator == FilterGroupOperator.Or ? ZeroWpfTheme.WarningAccent : ZeroWpfTheme.PrimaryAccent;
                FilterChanged?.Invoke(this, EventArgs.Empty);
            };
            grpRow.Children.Add(btnOp);

            // [+] Add Condition
            var btnAddCond = new Button
            {
                Content = ZeroLocalizer.GetString(ZeroStringId.FilterAddCondition),
                Height = 26,
                Padding = new Thickness(8, 0, 8, 0),
                Margin = new Thickness(0, 0, 6, 0)
            };
            btnAddCond.Click += (s, e) =>
            {
                string field = _availableFields.Count > 0 ? _availableFields[0] : "Field";
                group.AddCondition(field, FilterComparisonOperator.Equals, "");
                RebuildTreeUI();
                FilterChanged?.Invoke(this, EventArgs.Empty);
            };
            grpRow.Children.Add(btnAddCond);

            // [+] Add Group
            var btnAddGrp = new Button
            {
                Content = ZeroLocalizer.GetString(ZeroStringId.FilterAddGroup),
                Height = 26,
                Padding = new Thickness(8, 0, 8, 0)
            };
            btnAddGrp.Click += (s, e) =>
            {
                group.AddGroup(FilterGroupOperator.And);
                RebuildTreeUI();
                FilterChanged?.Invoke(this, EventArgs.Empty);
            };
            grpRow.Children.Add(btnAddGrp);

            _treeStack?.Children.Add(grpRow);

            // Render Children
            for (int i = 0; i < group.Children.Count; i++)
            {
                int childIndex = i;
                var child = group.Children[i];

                if (child is ConditionFilterNode cond)
                {
                    RenderConditionNode(group, cond, childIndex, indentLevel + 1);
                }
                else if (child is GroupFilterNode subGroup)
                {
                    RenderGroupNode(subGroup, indentLevel + 1);
                }
            }
        }

        private void RenderConditionNode(GroupFilterNode parentGroup, ConditionFilterNode cond, int index, int indentLevel)
        {
            var row = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(indentLevel * 24, 2, 0, 2)
            };

            // Delete [X] Button
            var btnDel = new Button
            {
                Content = "✕",
                Width = 26,
                Height = 26,
                Foreground = ZeroWpfTheme.DangerAccent,
                Margin = new Thickness(0, 0, 6, 0)
            };
            btnDel.Click += (s, e) =>
            {
                parentGroup.Children.RemoveAt(index);
                RebuildTreeUI();
                FilterChanged?.Invoke(this, EventArgs.Empty);
            };
            row.Children.Add(btnDel);

            // Field ComboBox
            var cboField = new ComboBox
            {
                Width = 140,
                Height = 26,
                Margin = new Thickness(0, 0, 6, 0)
            };
            if (_availableFields.Count > 0)
            {
                foreach (var f in _availableFields) cboField.Items.Add(f);
                cboField.SelectedItem = _availableFields.Contains(cond.FieldName) ? cond.FieldName : _availableFields[0];
            }
            else
            {
                cboField.Items.Add(cond.FieldName);
                cboField.SelectedIndex = 0;
            }
            cboField.SelectionChanged += (s, e) =>
            {
                cond.FieldName = cboField.SelectedItem?.ToString() ?? "";
                FilterChanged?.Invoke(this, EventArgs.Empty);
            };
            row.Children.Add(cboField);

            // Operator ComboBox
            var cboOp = new ComboBox
            {
                Width = 140,
                Height = 26,
                Margin = new Thickness(0, 0, 6, 0)
            };
            FilterOperatorDisplayItem? selectedOpItem = null;
            foreach (FilterComparisonOperator op in Enum.GetValues(typeof(FilterComparisonOperator)))
            {
                var item = new FilterOperatorDisplayItem(op);
                cboOp.Items.Add(item);
                if (op == cond.Operator) selectedOpItem = item;
            }
            cboOp.SelectedItem = selectedOpItem;
            cboOp.SelectionChanged += (s, e) =>
            {
                if (cboOp.SelectedItem is FilterOperatorDisplayItem item)
                {
                    cond.Operator = item.Operator;
                    FilterChanged?.Invoke(this, EventArgs.Empty);
                }
            };
            row.Children.Add(cboOp);

            // Value TextBox
            var txtVal = new TextBox
            {
                Text = cond.Value,
                Width = 140,
                Height = 26,
                VerticalContentAlignment = VerticalAlignment.Center,
                Padding = new Thickness(4, 0, 4, 0)
            };
            txtVal.TextChanged += (s, e) =>
            {
                cond.Value = txtVal.Text;
                FilterChanged?.Invoke(this, EventArgs.Empty);
            };
            row.Children.Add(txtVal);

            _treeStack?.Children.Add(row);
        }

        public string GetSqlWhere() => _rootGroup.ToSqlWhere();
        public string GetDisplayString() => _rootGroup.ToDisplayString();
    }

    /// <summary>
    /// Backward-compatibility alias for <see cref="FilterControl"/>.
    /// </summary>
    public class ZeroFilterControl : FilterControl
    {
    }

    internal sealed class FilterOperatorDisplayItem
    {
        public FilterComparisonOperator Operator { get; }
        public FilterOperatorDisplayItem(FilterComparisonOperator op) => Operator = op;
        public override string ToString() => Operator.GetLocalizedName();
    }
}
