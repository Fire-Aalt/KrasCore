using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Collections;

namespace KrasCore
{
    public struct Group<TKey, T>
        where TKey : unmanaged
        where T : unmanaged
    {
        private TKey _key;
        private NativeList<T> _values;

        public Group(TKey key, T value, AllocatorManager.AllocatorHandle allocator)
        {
            _key = key;
            _values = new NativeList<T>(1, allocator);
            _values.Add(value);
        }

        public TKey Key
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _key;
        }

        public int Length
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _values.Length;
        }

        public NativeArray<T> Values
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _values.AsArray();
        }

        public T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _values[index];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Add(in T value)
        {
            _values.Add(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeArray<T>.Enumerator GetEnumerator()
        {
            return _values.AsArray().GetEnumerator();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Query<T, NativeArray<T>.Enumerator> AsQuery()
        {
            return _values.AsQuery();
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
            var list = new NativeList<T>(_values.Length, allocator);
            for (var i = 0; i < _values.Length; i++)
            {
                list.Add(_values[i]);
            }

            return list;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T[] ToManagedArray()
        {
            var array = new T[_values.Length];
            for (var i = 0; i < _values.Length; i++)
            {
                array[i] = _values[i];
            }

            return array;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public List<T> ToManagedList()
        {
            var list = new List<T>(_values.Length);
            for (var i = 0; i < _values.Length; i++)
            {
                list.Add(_values[i]);
            }

            return list;
        }

        internal void Dispose()
        {
            if (_values.IsCreated)
            {
                _values.Dispose();
                _values = default;
            }
        }
    }

    public struct GroupedQuery<TKey, T> : IDisposable
        where TKey : unmanaged
        where T : unmanaged
    {
        private NativeList<Group<TKey, T>> _groups;
        private int _valueCount;

        public GroupedQuery(NativeList<Group<TKey, T>> groups, int valueCount)
        {
            _groups = groups;
            _valueCount = valueCount;
        }

        public bool IsCreated => _groups.IsCreated;

        public int GroupCount => _groups.Length;

        public int ValueCount => _valueCount;

        public NativeArray<Group<TKey, T>> Groups => _groups.AsArray();

        public Group<TKey, T> this[int index] => _groups[index];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Query<Group<TKey, T>, GroupedQueryEnumerator<TKey, T>> AsQuery()
        {
            return new Query<Group<TKey, T>, GroupedQueryEnumerator<TKey, T>>(GetEnumerator());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public GroupedQueryEnumerator<TKey, T> GetEnumerator()
        {
            return new GroupedQueryEnumerator<TKey, T>(_groups.AsArray());
        }

        public void Dispose()
        {
            if (!_groups.IsCreated)
            {
                return;
            }

            for (var i = 0; i < _groups.Length; i++)
            {
                ref var group = ref _groups.ElementAt(i);
                group.Dispose();
            }

            _groups.Dispose();
            _groups = default;
            _valueCount = 0;
        }
    }

    public struct GroupedQueryEnumerator<TKey, T> : IEnumerator<Group<TKey, T>>
        where TKey : unmanaged
        where T : unmanaged
    {
        private NativeArray<Group<TKey, T>> _groups;
        private int _index;

        public GroupedQueryEnumerator(NativeArray<Group<TKey, T>> groups)
        {
            _groups = groups;
            _index = -1;
        }

        public Group<TKey, T> Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _groups[_index];
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
}
