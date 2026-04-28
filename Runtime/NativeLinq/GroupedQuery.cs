using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Collections;

namespace KrasCore
{
    public struct NativeLinqGroupRange<TKey>
        where TKey : unmanaged
    {
        public TKey Key;
        public int StartIndex;
        public int Length;
    }

    public struct NativeLinqGroup<TKey, T>
        where TKey : unmanaged
        where T : unmanaged
    {
        private NativeLinqGroupRange<TKey> _range;
        private NativeList<T> _values;

        public NativeLinqGroup(NativeLinqGroupRange<TKey> range, NativeList<T> values)
        {
            _range = range;
            _values = values;
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

                return _values[_range.StartIndex + index];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeLinqGroupEnumerator<T> GetEnumerator()
        {
            return new NativeLinqGroupEnumerator<T>(_values.AsArray(), _range.StartIndex, _range.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Query<T, NativeLinqGroupEnumerator<T>> AsQuery()
        {
            return new Query<T, NativeLinqGroupEnumerator<T>>(GetEnumerator());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeArray<T> ToNativeArray(AllocatorManager.AllocatorHandle allocator)
        {
            var list = ToNativeList(Allocator.Temp);
            try
            {
                return list.ToArray(allocator);
            }
            finally
            {
                list.Dispose();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeList<T> ToNativeList(AllocatorManager.AllocatorHandle allocator)
        {
            var list = new NativeList<T>(_range.Length, allocator);
            for (var i = 0; i < _range.Length; i++)
            {
                list.Add(this[i]);
            }

            return list;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T[] ToManagedArray()
        {
            var array = new T[_range.Length];
            for (var i = 0; i < _range.Length; i++)
            {
                array[i] = this[i];
            }

            return array;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public List<T> ToManagedList()
        {
            var list = new List<T>(_range.Length);
            for (var i = 0; i < _range.Length; i++)
            {
                list.Add(this[i]);
            }

            return list;
        }
    }

    public struct GroupedQuery<TKey, T> : IDisposable
        where TKey : unmanaged
        where T : unmanaged
    {
        private NativeList<NativeLinqGroupRange<TKey>> _groups;
        private NativeList<T> _values;

        public GroupedQuery(NativeList<NativeLinqGroupRange<TKey>> groups, NativeList<T> values)
        {
            _groups = groups;
            _values = values;
        }

        public bool IsCreated
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _groups.IsCreated && _values.IsCreated;
        }

        public int GroupCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _groups.Length;
        }

        public int ValueCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _values.Length;
        }

        public NativeArray<NativeLinqGroupRange<TKey>> GroupRanges
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _groups.AsArray();
        }

        public NativeArray<T> Values
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _values.AsArray();
        }

        public NativeLinqGroup<TKey, T> this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new NativeLinqGroup<TKey, T>(_groups[index], _values);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public GroupedQueryEnumerator<TKey, T> GetEnumerator()
        {
            return new GroupedQueryEnumerator<TKey, T>(_groups, _values);
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
        }
    }

    public struct GroupedQueryEnumerator<TKey, T> : IEnumerator<NativeLinqGroup<TKey, T>>
        where TKey : unmanaged
        where T : unmanaged
    {
        private NativeList<NativeLinqGroupRange<TKey>> _groups;
        private NativeList<T> _values;
        private int _index;

        public GroupedQueryEnumerator(NativeList<NativeLinqGroupRange<TKey>> groups, NativeList<T> values)
        {
            _groups = groups;
            _values = values;
            _index = -1;
        }

        public NativeLinqGroup<TKey, T> Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new NativeLinqGroup<TKey, T>(_groups[_index], _values);
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

    public struct NativeLinqGroupEnumerator<T> : IEnumerator<T>
        where T : unmanaged
    {
        private NativeArray<T> _values;
        private int _startIndex;
        private int _length;
        private int _index;

        public NativeLinqGroupEnumerator(NativeArray<T> values, int startIndex, int length)
        {
            _values = values;
            _startIndex = startIndex;
            _length = length;
            _index = -1;
        }

        public T Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _values[_startIndex + _index];
        }

        object System.Collections.IEnumerator.Current => Current;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            _index++;
            return _index < _length;
        }

        public void Reset()
        {
            _index = -1;
        }

        public void Dispose()
        {
        }
    }
}
