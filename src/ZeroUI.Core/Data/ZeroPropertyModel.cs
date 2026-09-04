using System;
using System.Collections.Generic;
using System.Reflection;

namespace ZeroUI.Core.Data
{
    public class PropertyCategoryGroup
    {
        public string Name { get; set; } = string.Empty;
        public bool IsExpanded { get; set; } = true;
        public List<ZeroPropertyItem> Items { get; } = new List<ZeroPropertyItem>();

        public PropertyCategoryGroup(string name)
        {
            Name = name;
        }
    }

    /// <summary>
    /// Headless model for ZeroPropertyGrid.
    /// Supports automatic reflection analysis of any C# object or manual property registration.
    /// </summary>
    public class ZeroPropertyModel
    {
        private readonly List<ZeroPropertyItem> _items = new List<ZeroPropertyItem>();
        private readonly List<PropertyCategoryGroup> _categories = new List<PropertyCategoryGroup>();
        private object? _selectedObject;
        private string _searchFilter = string.Empty;

        public IReadOnlyList<ZeroPropertyItem> Items => _items;
        public IReadOnlyList<PropertyCategoryGroup> Categories => _categories;
        public object? SelectedObject => _selectedObject;

        public string SearchFilter
        {
            get => _searchFilter;
            set
            {
                if (_searchFilter != value)
                {
                    _searchFilter = value ?? string.Empty;
                    RebuildCategories();
                }
            }
        }

        public event EventHandler? ModelChanged;
        public event EventHandler<PropertyValueChangedEventArgs>? PropertyValueChanged;

        public void SetSelectedObject(object? target)
        {
            _selectedObject = target;
            _items.Clear();

            if (target != null)
            {
                var props = target.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
                for (int i = 0; i < props.Length; i++)
                {
                    var prop = props[i];
                    if (!prop.CanRead) continue;

                    var item = ZeroPropertyItem.FromPropertyInfo(prop, target);
                    item.ValueChanged += (s, e) =>
                    {
                        if (_selectedObject != null && prop.CanWrite)
                        {
                            try
                            {
                                prop.SetValue(_selectedObject, Convert.ChangeType(e.NewValue, prop.PropertyType));
                            }
                            catch { }
                        }
                        PropertyValueChanged?.Invoke(s, e);
                    };
                    _items.Add(item);
                }
            }

            RebuildCategories();
        }

        public void AddItem(ZeroPropertyItem item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            item.ValueChanged += (s, e) => PropertyValueChanged?.Invoke(s, e);
            _items.Add(item);
            RebuildCategories();
        }

        public void Clear()
        {
            _selectedObject = null;
            _items.Clear();
            _categories.Clear();
            ModelChanged?.Invoke(this, EventArgs.Empty);
        }

        public void ToggleCategory(string categoryName)
        {
            for (int i = 0; i < _categories.Count; i++)
            {
                if (string.Equals(_categories[i].Name, categoryName, StringComparison.OrdinalIgnoreCase))
                {
                    _categories[i].IsExpanded = !_categories[i].IsExpanded;
                    ModelChanged?.Invoke(this, EventArgs.Empty);
                    break;
                }
            }
        }

        private void RebuildCategories()
        {
            _categories.Clear();
            var dict = new Dictionary<string, PropertyCategoryGroup>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < _items.Count; i++)
            {
                var item = _items[i];
                if (!string.IsNullOrWhiteSpace(_searchFilter))
                {
                    if (item.DisplayName.IndexOf(_searchFilter, StringComparison.OrdinalIgnoreCase) < 0 &&
                        item.Name.IndexOf(_searchFilter, StringComparison.OrdinalIgnoreCase) < 0 &&
                        item.Category.IndexOf(_searchFilter, StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        continue;
                    }
                }

                if (!dict.TryGetValue(item.Category, out var group))
                {
                    group = new PropertyCategoryGroup(item.Category);
                    dict[item.Category] = group;
                    _categories.Add(group);
                }
                group.Items.Add(item);
            }

            ModelChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
