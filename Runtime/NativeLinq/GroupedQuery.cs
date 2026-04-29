using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Collections;

namespace KrasCore
{
    public struct GroupRange<TKey>
        where TKey : unmanaged
    {
        public TKey Key;
        public int HeadIndex;
        public int TailIndex;
        public int Length;
    }

    public struct Group<TKey, T>
        where TKey : unmanaged
        where T : unmanaged
    {
        private GroupRange<TKey> _range;
        private NativeArray<T> _values;
        private NativeArray<int> _nextIndexes;

        public Group(GroupRange<TKey> range, NativeArray<T> values, NativeArray<int> nextIndexes)
        {
            _range = range;
            _values = values;
            _nextIndexes = nextIndexes;
        }

        public TKey Key
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _range.Key;
        }

        public int Length
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _range.Length;
        }

        public T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                if ((uint)index >= (uint)_range.Length)
                {
                    throw new IndexOutOfRangeException();
                }
#endif

                var valueIndex = _range.HeadIndex;
                for (var i = 0; i < index; i++)
                {
                    valueIndex = _nextIndexes[valueIndex];
                }

                return _values[valueIndex];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public GroupEnumerator<T> GetEnumerator()
        {
            return new GroupEnumerator<T>(_values, _nextIndexes, _range.HeadIndex);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Query<T, GroupEnumerator<T>> AsQuery()
        {
            return new Query<T, GroupEnumerator<T>>(GetEnumerator());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeArray<T> ToNativeArray(AllocatorManager.AllocatorHandle allocator)
        {
            var list = ToNativeList(Allocator.Temp);
            return list.ToArray(allocator);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeList<T> ToNativeList(AllocatorManager.AllocatorHandle allocator)
        {
            var list = new NativeList<T>(_range.Length, allocator);
            foreach (var value in this)
            {
                list.Add(value);
            }

            return list;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T[] ToManagedArray()
        {
            var array = new T[_range.Length];
            var index = 0;
            foreach (var value in this)
            {
                array[index] = value;
                index++;
            }

            return array;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public List<T> ToManagedList()
        {
            var list = new List<T>(_range.Length);
            foreach (var value in this)
            {
                list.Add(value);
            }

            return list;
        }
    }

    public struct GroupedQuery<TKey, T> : IDisposable
        where TKey : unmanaged
        where T : unmanaged
    {
        private NativeList<GroupRange<TKey>> _groups;
        private NativeList<T> _values;
        private NativeList<int> _nextIndexes;

        public GroupedQuery(NativeList<GroupRange<TKey>> groups, NativeList<T> values, NativeList<int> nextIndexes)
        {
            _groups = groups;
            _values = values;
            _nextIndexes = nextIndexes;
        }

        public bool IsCreated => _groups.IsCreated && _values.IsCreated && _nextIndexes.IsCreated;

        public int GroupCount => _groups.Length;

        public int ValueCount => _values.Length;

        public NativeArray<GroupRange<TKey>> GroupRanges => _groups.AsArray();

        public NativeArray<T> Values => _values.AsArray();

        public NativeArray<int> NextIndexes => _nextIndexes.AsArray();

        public Group<TKey, T> this[int index] => new Group<TKey, T>(_groups[index], _values.AsArray(), _nextIndexes.AsArray());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Query<Group<TKey, T>, GroupedQueryEnumerator<TKey, T>> AsQuery()
        {
            return new Query<Group<TKey, T>, GroupedQueryEnumerator<TKey, T>>(GetEnumerator());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public GroupedQueryEnumerator<TKey, T> GetEnumerator()
        {
            return new GroupedQueryEnumerator<TKey, T>(_groups.AsArray(), _values.AsArray(), _nextIndexes.AsArray());
        }

        public void Dispose()
        {
            if (_groups.IsCreated)
            {
                _groups.Dispose();
            }

            if (_values.IsCreated)
            {
                _values.Dispose();
            }

            if (_nextIndexes.IsCreated)
            {
                _nextIndexes.Dispose();
            }
        }
    }

    public struct GroupedQueryEnumerator<TKey, T> : IEnumerator<Group<TKey, T>>
        where TKey : unmanaged
        where T : unmanaged
    {
        private NativeArray<GroupRange<TKey>> _groups;
        private NativeArray<T> _values;
        private NativeArray<int> _nextIndexes;
        private int _index;

        public GroupedQueryEnumerator(NativeArray<GroupRange<TKey>> groups, NativeArray<T> values, NativeArray<int> nextIndexes)
        {
            _groups = groups;
            _values = values;
            _nextIndexes = nextIndexes;
            _index = -1;
        }

        public Group<TKey, T> Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new Group<TKey, T>(_groups[_index], _values, _nextIndexes);
        }

        object System.Collections.IEnumerator.Current => Current;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            _index++;
            return _index < _groups.Length;
        }

        public void Reset()
        {
            _index = -1;
        }

        public void Dispose()
        {
        }
    }

    public struct GroupEnumerator<T> : IEnumerator<T>
        where T : unmanaged
    {
        private NativeArray<T> _values;
        private NativeArray<int> _nextIndexes;
        private int _headIndex;
        private int _nextIndex;
        private int _currentIndex;

        public GroupEnumerator(NativeArray<T> values, NativeArray<int> nextIndexes, int headIndex)
        {
            _values = values;
            _nextIndexes = nextIndexes;
            _headIndex = headIndex;
            _nextIndex = headIndex;
            _currentIndex = -1;
        }

        public T Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _values[_currentIndex];
        }

        object System.Collections.IEnumerator.Current => Current;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            if (_nextIndex < 0)
            {
                return false;
            }

            _currentIndex = _nextIndex;
            _nextIndex = _nextIndexes[_currentIndex];
            return true;
        }

        public void Reset()
        {
            _nextIndex = _headIndex;
            _currentIndex = -1;
        }

        public void Dispose()
        {
        }
    }
}
