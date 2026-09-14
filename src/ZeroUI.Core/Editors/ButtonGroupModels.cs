using System;
using System.Collections.Generic;
using System.Linq;

namespace ZeroUI.Core.Editors
{
    /// <summary>
    /// Specifies the behavioral type of an item in a button group.
    /// </summary>
    public enum ButtonGroupItemType
    {
        /// <summary>Standard push action button.</summary>
        Push,
        /// <summary>Toggleable button maintaining checked/unchecked state.</summary>
        Toggle,
        /// <summary>Button with dropdown arrow triggering contextual options.</summary>
        DropDown,
        /// <summary>Vertical or horizontal line separator dividing functional blocks.</summary>
        Separator
    }

    /// <summary>
    /// Specifies the visual style variant of a button in a group.
    /// </summary>
    public enum ButtonGroupItemStyle
    {
        Primary,
        Secondary,
        Success,
        Danger,
        Ghost
    }

    /// <summary>
    /// Specifies selection behavior for toggle items inside a button group.
    /// </summary>
    public enum ButtonGroupSelectionMode
    {
        /// <summary>Buttons act independently without enforcing mutual exclusion.</summary>
        None,
        /// <summary>Radio-style mutual exclusion: at most one toggle item can be checked.</summary>
        SingleSelect,
        /// <summary>Checkbox-style: multiple toggle items can be checked concurrently.</summary>
        MultiSelect
    }

    /// <summary>
    /// Represents an individual actionable item within a <see cref="ButtonGroupModel"/>.
    /// </summary>
    public class ButtonGroupItem
    {
        public string Id { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public string? IconGlyph { get; set; }
        public string? ToolTip { get; set; }
        public ButtonGroupItemType Type { get; set; } = ButtonGroupItemType.Push;
        public ButtonGroupItemStyle Style { get; set; } = ButtonGroupItemStyle.Secondary;
        public bool IsChecked { get; set; }
        public bool IsEnabled { get; set; } = true;
        public bool IsVisible { get; set; } = true;
        public string? BadgeText { get; set; }
        public object? Tag { get; set; }
        public Action<ButtonGroupItem>? Action { get; set; }

        public ButtonGroupItem() { }

        public ButtonGroupItem(string id, string text, string? iconGlyph = null, ButtonGroupItemType type = ButtonGroupItemType.Push, Action<ButtonGroupItem>? action = null)
        {
            Id = id;
            Text = text;
            IconGlyph = iconGlyph;
            Type = type;
            Action = action;
        }
    }

    /// <summary>
    /// Event arguments raised when a button item inside a group is clicked.
    /// </summary>
    public class ButtonGroupItemEventArgs : EventArgs
    {
        public ButtonGroupItem Item { get; }
        public int Index { get; }
        public bool Handled { get; set; }

        public ButtonGroupItemEventArgs(ButtonGroupItem item, int index)
        {
            Item = item;
            Index = index;
        }
    }

    /// <summary>
    /// Headless state coordinator and collection manager for button clusters and action strips.
    /// </summary>
    public class ButtonGroupModel
    {
        private readonly List<ButtonGroupItem> _items = new List<ButtonGroupItem>();
        private ButtonGroupSelectionMode _selectionMode = ButtonGroupSelectionMode.None;

        public event EventHandler<ButtonGroupItemEventArgs>? ItemClicked;
        public event EventHandler<ButtonGroupItem>? SelectionChanged;
        public event EventHandler? ItemsChanged;

        public IReadOnlyList<ButtonGroupItem> Items => _items;

        public ButtonGroupSelectionMode SelectionMode
        {
            get => _selectionMode;
            set => _selectionMode = value;
        }

        public int Count => _items.Count;

        public ButtonGroupItem this[int index] => _items[index];

        public ButtonGroupItem? SelectedItem => _items.FirstOrDefault(i => i.Type == ButtonGroupItemType.Toggle && i.IsChecked);

        public void Add(ButtonGroupItem item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            _items.Add(item);
            ItemsChanged?.Invoke(this, EventArgs.Empty);
        }

        public void AddRange(IEnumerable<ButtonGroupItem> items)
        {
            if (items == null) throw new ArgumentNullException(nameof(items));
            _items.AddRange(items);
            ItemsChanged?.Invoke(this, EventArgs.Empty);
        }

        public bool Remove(ButtonGroupItem item)
        {
            if (item == null) return false;
            bool removed = _items.Remove(item);
            if (removed)
            {
                ItemsChanged?.Invoke(this, EventArgs.Empty);
            }
            return removed;
        }

        public void Clear()
        {
            _items.Clear();
            ItemsChanged?.Invoke(this, EventArgs.Empty);
        }

        public ButtonGroupItem? FindById(string id)
        {
            return _items.FirstOrDefault(i => string.Equals(i.Id, id, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Selects an item by index, enforcing single-select mutual exclusion if configured.
        /// </summary>
        public void SelectIndex(int index)
        {
            if (index < 0 || index >= _items.Count) return;
            var target = _items[index];
            if (target.Type != ButtonGroupItemType.Toggle || !target.IsEnabled) return;

            if (_selectionMode == ButtonGroupSelectionMode.SingleSelect)
            {
                foreach (var itm in _items)
                {
                    if (itm.Type == ButtonGroupItemType.Toggle)
                    {
                        itm.IsChecked = ReferenceEquals(itm, target);
                    }
                }
            }
            else
            {
                target.IsChecked = !target.IsChecked;
            }

            SelectionChanged?.Invoke(this, target);
            ItemsChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Handles click on an item, performing action dispatch and state toggling.
        /// </summary>
        public void TriggerClick(int index)
        {
            if (index < 0 || index >= _items.Count) return;
            var item = _items[index];
            if (!item.IsEnabled || item.Type == ButtonGroupItemType.Separator) return;

            if (item.Type == ButtonGroupItemType.Toggle)
            {
                SelectIndex(index);
            }

            item.Action?.Invoke(item);
            ItemClicked?.Invoke(this, new ButtonGroupItemEventArgs(item, index));
        }
    }
}
