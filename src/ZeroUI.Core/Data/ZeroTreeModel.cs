using System;
using System.Collections.Generic;

namespace ZeroUI.Core.Data
{
    /// <summary>
    /// Headless hierarchical model for ZeroTreeList / ZeroTreeGrid.
    /// Provides depth-first flattening of visible nodes for ultra-fast $O(1)$ virtualized rendering.
    /// </summary>
    public class ZeroTreeModel
    {
        private readonly List<ZeroTreeNode> _roots = new List<ZeroTreeNode>();
        private readonly List<ZeroTreeNode> _flattenedVisibleNodes = new List<ZeroTreeNode>();
        private bool _isDirty = true;

        public IReadOnlyList<ZeroTreeNode> Roots => _roots;

        public event EventHandler? ModelChanged;

        public ZeroTreeNode AddRoot(ZeroTreeNode node)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));
            node.Parent = null;
            _roots.Add(node);
            _isDirty = true;
            ModelChanged?.Invoke(this, EventArgs.Empty);
            return node;
        }

        public ZeroTreeNode AddRoot(params string[] cellValues)
        {
            var node = new ZeroTreeNode(cellValues);
            AddRoot(node);
            return node;
        }

        public bool RemoveRoot(ZeroTreeNode node)
        {
            if (node != null && _roots.Remove(node))
            {
                _isDirty = true;
                ModelChanged?.Invoke(this, EventArgs.Empty);
                return true;
            }
            return false;
        }

        public void Clear()
        {
            _roots.Clear();
            _flattenedVisibleNodes.Clear();
            _isDirty = true;
            ModelChanged?.Invoke(this, EventArgs.Empty);
        }

        public int VisibleNodeCount
        {
            get
            {
                EnsureFlattened();
                return _flattenedVisibleNodes.Count;
            }
        }

        public ZeroTreeNode GetVisibleNode(int visualIndex)
        {
            EnsureFlattened();
            if (visualIndex >= 0 && visualIndex < _flattenedVisibleNodes.Count)
            {
                return _flattenedVisibleNodes[visualIndex];
            }
            throw new ArgumentOutOfRangeException(nameof(visualIndex));
        }

        public int IndexOf(ZeroTreeNode node)
        {
            EnsureFlattened();
            return _flattenedVisibleNodes.IndexOf(node);
        }

        public void ToggleExpand(ZeroTreeNode node)
        {
            if (node == null || !node.HasChildren) return;
            node.IsExpanded = !node.IsExpanded;
            _isDirty = true;
            ModelChanged?.Invoke(this, EventArgs.Empty);
        }

        public void ExpandAll()
        {
            SetExpandRecursive(_roots, true);
            _isDirty = true;
            ModelChanged?.Invoke(this, EventArgs.Empty);
        }

        public void CollapseAll()
        {
            SetExpandRecursive(_roots, false);
            _isDirty = true;
            ModelChanged?.Invoke(this, EventArgs.Empty);
        }

        private static void SetExpandRecursive(IEnumerable<ZeroTreeNode> nodes, bool isExpanded)
        {
            foreach (var node in nodes)
            {
                node.IsExpanded = isExpanded;
                if (node.HasChildren)
                {
                    SetExpandRecursive(node.Children, isExpanded);
                }
            }
        }

        public void Invalidate()
        {
            _isDirty = true;
            ModelChanged?.Invoke(this, EventArgs.Empty);
        }

        private void EnsureFlattened()
        {
            if (!_isDirty) return;

            _flattenedVisibleNodes.Clear();
            for (int i = 0; i < _roots.Count; i++)
            {
                FlattenNodeRecursive(_roots[i]);
            }
            _isDirty = false;
        }

        private void FlattenNodeRecursive(ZeroTreeNode node)
        {
            if (!node.IsVisible) return;
            _flattenedVisibleNodes.Add(node);

            if (node.IsExpanded && node.HasChildren)
            {
                for (int i = 0; i < node.Children.Count; i++)
                {
                    FlattenNodeRecursive(node.Children[i]);
                }
            }
        }
    }
}
