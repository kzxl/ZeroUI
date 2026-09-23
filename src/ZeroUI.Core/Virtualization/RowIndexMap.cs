using System;
using System.Runtime.CompilerServices;

namespace ZeroUI.Core.Virtualization
{
    /// <summary>
    /// Zero-allocation view-to-model row mapping array with virtual identity support.
    /// Sorting and filtering operate strictly on index integers without moving data objects.
    /// When unsorted and unfiltered, operates in O(1) virtual identity mode with 0 allocations.
    /// </summary>
    public sealed class RowIndexMap
    {
        private int[] _map;
        private int _activeCount;
        private bool _isIdentity;

        public RowIndexMap(int initialCapacity = 1000)
        {
            _map = new int[initialCapacity];
            _activeCount = 0;
            _isIdentity = true;
        }

        public bool IsIdentity => _isIdentity;

        public int ActiveCount
        {
            get => _activeCount;
            set
            {
                if (_isIdentity)
                {
                    _activeCount = Math.Max(0, value);
                }
                else
                {
                    _activeCount = Math.Min(value, _map.Length);
                }
            }
        }

        public int this[int visualIndex]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _isIdentity ? visualIndex : _map[visualIndex];
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                if (_isIdentity) EnsureMaterialized();
                _map[visualIndex] = value;
            }
        }

        public void EnsureCapacity(int count)
        {
            if (_map.Length < count)
            {
                int newCap = Math.Max(count, _map.Length * 2);
                Array.Resize(ref _map, newCap);
            }
        }

        public void EnsureMaterialized()
        {
            if (!_isIdentity) return;
            EnsureCapacity(_activeCount);
            for (int i = 0; i < _activeCount; i++)
            {
                _map[i] = i;
            }
            _isIdentity = false;
        }

        public void ResetIdentity(int count)
        {
            _activeCount = Math.Max(0, count);
            _isIdentity = true;
        }

        public void SetUnderlyingBuffer(int[] buffer, int activeCount)
        {
            _map = buffer ?? Array.Empty<int>();
            _activeCount = activeCount;
            _isIdentity = false;
        }

        public void Sort(Comparison<int> comparison)
        {
            if (_activeCount <= 1) return;
            EnsureMaterialized();
            Array.Sort(_map, 0, _activeCount, new ComparisonComparer<int>(comparison));
        }

        public void Sort(System.Collections.Generic.IComparer<int> comparer)
        {
            if (_activeCount <= 1) return;
            EnsureMaterialized();
            Array.Sort(_map, 0, _activeCount, comparer);
        }

        public void Filter(Func<int, bool> predicate, int totalModelCount)
        {
            EnsureCapacity(totalModelCount);
            _isIdentity = false;
            int count = 0;
            for (int i = 0; i < totalModelCount; i++)
            {
                if (predicate(i))
                {
                    _map[count++] = i;
                }
            }
            _activeCount = count;
        }

        public Span<int> AsSpan()
        {
            EnsureMaterialized();
            return new Span<int>(_map, 0, _activeCount);
        }

        private sealed class ComparisonComparer<T> : System.Collections.Generic.IComparer<T>
        {
            private readonly Comparison<T> _comparison;
            public ComparisonComparer(Comparison<T> comparison) => _comparison = comparison;
            public int Compare(T? x, T? y) => _comparison(x!, y!);
        }

    }
}
