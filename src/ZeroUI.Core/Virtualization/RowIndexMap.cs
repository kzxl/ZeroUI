using System;
using System.Runtime.CompilerServices;

namespace ZeroUI.Core.Virtualization
{
    /// <summary>
    /// Zero-allocation view-to-model row mapping array.
    /// Sorting and filtering operate strictly on index integers without moving data objects.
    /// </summary>
    public sealed class RowIndexMap
    {
        private int[] _map;
        private int _activeCount;

        public RowIndexMap(int initialCapacity = 1000)
        {
            _map = new int[initialCapacity];
            _activeCount = 0;
        }

        public int ActiveCount
        {
            get => _activeCount;
            set => _activeCount = Math.Min(value, _map.Length);
        }

        public int this[int visualIndex]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _map[visualIndex];
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => _map[visualIndex] = value;
        }

        public void EnsureCapacity(int count)
        {
            if (_map.Length < count)
            {
                int newCap = Math.Max(count, _map.Length * 2);
                Array.Resize(ref _map, newCap);
            }
        }

        public void ResetIdentity(int count)
        {
            EnsureCapacity(count);
            _activeCount = count;
            for (int i = 0; i < count; i++)
            {
                _map[i] = i;
            }
        }

        public void Sort(Comparison<int> comparison)
        {
            if (_activeCount <= 1) return;
            Array.Sort(_map, 0, _activeCount, new ComparisonComparer<int>(comparison));
        }

        private sealed class ComparisonComparer<T> : System.Collections.Generic.IComparer<T>
        {
            private readonly Comparison<T> _comparison;
            public ComparisonComparer(Comparison<T> comparison) => _comparison = comparison;
            public int Compare(T? x, T? y) => _comparison(x!, y!);
        }

    }
}
