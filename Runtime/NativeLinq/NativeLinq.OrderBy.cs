using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace KrasCore
{
    public static partial class NativeLinqExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static OrderedQuery<T, TEnumerator, AscendingComparer<T>> OrderBy<T, TEnumerator>(this Query<T, TEnumerator> source)
            where T : unmanaged, IComparable<T>
            where TEnumerator : unmanaged, IEnumerator<T>
        {
            return source.OrderBy(new AscendingComparer<T>());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static OrderedQuery<T, TEnumerator, DescendingComparer<T>> OrderByDescending<T, TEnumerator>(this Query<T, TEnumerator> source)
            where T : unmanaged, IComparable<T>
            where TEnumerator : unmanaged, IEnumerator<T>
        {
            return source.OrderBy(new DescendingComparer<T>());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NativeList<T> OrderBy<T, TEnumerator>(this Query<T, TEnumerator> source, AllocatorManager.AllocatorHandle allocator)
            where T : unmanaged, IComparable<T>
            where TEnumerator : unmanaged, IEnumerator<T>
        {
            return source.OrderBy(new AscendingComparer<T>(), allocator);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NativeList<T> OrderByDescending<T, TEnumerator>(this Query<T, TEnumerator> source, AllocatorManager.AllocatorHandle allocator)
            where T : unmanaged, IComparable<T>
            where TEnumerator : unmanaged, IEnumerator<T>
        {
            return source.OrderBy(new DescendingComparer<T>(), allocator);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static OrderedQuery<T, TEnumerator, ThenByComparer<T, TComparer, AscendingComparer<T>>> ThenBy<T, TEnumerator, TComparer>(
            this OrderedQuery<T, TEnumerator, TComparer> source)
            where T : unmanaged, IComparable<T>
            where TEnumerator : unmanaged, IEnumerator<T>
            where TComparer : unmanaged, IComparer<T>
        {
            return source.ThenBy(new AscendingComparer<T>());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static OrderedQuery<T, TEnumerator, ThenByComparer<T, TComparer, DescendingComparer<T>>> ThenByDescending<T, TEnumerator, TComparer>(
            this OrderedQuery<T, TEnumerator, TComparer> source)
            where T : unmanaged, IComparable<T>
            where TEnumerator : unmanaged, IEnumerator<T>
            where TComparer : unmanaged, IComparer<T>
        {
            return source.ThenBy(new DescendingComparer<T>());
        }
    }

    public partial struct Query<T, TEnumerator>
        where T : unmanaged
        where TEnumerator : unmanaged, IEnumerator<T>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public OrderedQuery<T, TEnumerator, TComparer> OrderBy<TComparer>(TComparer comparer)
            where TComparer : unmanaged, IComparer<T>
        {
            return new OrderedQuery<T, TEnumerator, TComparer>(this, comparer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public OrderedQuery<T, TEnumerator, ReverseComparer<T, TComparer>> OrderByDescending<TComparer>(TComparer comparer)
            where TComparer : unmanaged, IComparer<T>
        {
            return new OrderedQuery<T, TEnumerator, ReverseComparer<T, TComparer>>(this, new ReverseComparer<T, TComparer>(comparer));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeList<T> OrderBy<TComparer>(TComparer comparer, AllocatorManager.AllocatorHandle allocator)
            where TComparer : unmanaged, IComparer<T>
        {
            var list = ToNativeList(allocator);
            list.Sort(comparer);
            return list;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeList<T> OrderByDescending<TComparer>(TComparer comparer, AllocatorManager.AllocatorHandle allocator)
            where TComparer : unmanaged, IComparer<T>
        {
            var list = ToNativeList(allocator);
            list.Sort(new ReverseComparer<T, TComparer>(comparer));
            return list;
        }
    }

    public struct OrderedQuery<T, TEnumerator, TComparer>
        where T : unmanaged
        where TEnumerator : unmanaged, IEnumerator<T>
        where TComparer : unmanaged, IComparer<T>
    {
        private Query<T, TEnumerator> _source;
        private TComparer _comparer;

        public OrderedQuery(Query<T, TEnumerator> source, TComparer comparer)
        {
            _source = source;
            _comparer = comparer;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public OrderedQuery<T, TEnumerator, ThenByComparer<T, TComparer, TThenComparer>> ThenBy<TThenComparer>(TThenComparer comparer)
            where TThenComparer : unmanaged, IComparer<T>
        {
            return new OrderedQuery<T, TEnumerator, ThenByComparer<T, TComparer, TThenComparer>>(
                _source,
                new ThenByComparer<T, TComparer, TThenComparer>(_comparer, comparer));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public OrderedQuery<T, TEnumerator, ThenByComparer<T, TComparer, ReverseComparer<T, TThenComparer>>> ThenByDescending<TThenComparer>(TThenComparer comparer)
            where TThenComparer : unmanaged, IComparer<T>
        {
            return ThenBy(new ReverseComparer<T, TThenComparer>(comparer));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeArray<T> ToNativeArray(AllocatorManager.AllocatorHandle allocator)
        {
            var array = _source.ToNativeArray(allocator);
            array.Sort(_comparer);
            return array;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe UnsafeArray<T> ToUnsafeArray(Allocator allocator)
        {
            var array = _source.ToUnsafeArray(allocator);
            NativeSortExtension.Sort((T*)array.GetUnsafePtr(), array.Length, _comparer);
            return array;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UnsafeList<T> ToUnsafeList(AllocatorManager.AllocatorHandle allocator)
        {
            var list = _source.ToUnsafeList(allocator);
            list.Sort(_comparer);
            return list;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeList<T> ToNativeList(AllocatorManager.AllocatorHandle allocator)
        {
            var list = _source.ToNativeList(allocator);
            list.Sort(_comparer);
            return list;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T[] ToManagedArray()
        {
            var list = ToNativeList(Allocator.Temp);
            try
            {
                return list.ToManagedArray();
            }
            finally
            {
                list.Dispose();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public List<T> ToManagedList()
        {
            var array = ToManagedArray();
            return new List<T>(array);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public OrderedQueryEnumerator<T, TEnumerator, TComparer> GetEnumerator()
        {
            return new OrderedQueryEnumerator<T, TEnumerator, TComparer>(_source, _comparer);
        }
    }

    public struct OrderedQueryEnumerator<T, TEnumerator, TComparer> : IEnumerator<T>
        where T : unmanaged
        where TEnumerator : unmanaged, IEnumerator<T>
        where TComparer : unmanaged, IComparer<T>
    {
        private Query<T, TEnumerator> _source;
        private TComparer _comparer;
        private NativeList<T> _list;
        private int _index;
        private bool _initialized;

        public OrderedQueryEnumerator(Query<T, TEnumerator> source, TComparer comparer)
        {
            _source = source;
            _comparer = comparer;
            _list = default;
            _index = -1;
            _initialized = false;
        }

        public T Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _list[_index];
        }

        object System.Collections.IEnumerator.Current => Current;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            if (!_initialized)
            {
                _list = _source.ToNativeList(Allocator.Temp);
                _list.Sort(_comparer);
                _initialized = true;
            }

            _index++;
            return _index < _list.Length;
        }

        public void Reset()
        {
            if (_list.IsCreated)
            {
                _list.Dispose();
            }

            _list = default;
            _index = -1;
            _initialized = false;
        }

        public void Dispose()
        {
            if (_list.IsCreated)
            {
                _list.Dispose();
                _list = default;
            }
        }
    }

    public struct ThenByComparer<T, TPrimaryComparer, TSecondaryComparer> : IComparer<T>
        where T : unmanaged
        where TPrimaryComparer : unmanaged, IComparer<T>
        where TSecondaryComparer : unmanaged, IComparer<T>
    {
        private TPrimaryComparer _primaryComparer;
        private TSecondaryComparer _secondaryComparer;

        public ThenByComparer(TPrimaryComparer primaryComparer, TSecondaryComparer secondaryComparer)
        {
            _primaryComparer = primaryComparer;
            _secondaryComparer = secondaryComparer;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Compare(T x, T y)
        {
            var result = _primaryComparer.Compare(x, y);
            return result != 0 ? result : _secondaryComparer.Compare(x, y);
        }
    }

    public struct ReverseComparer<T, TComparer> : IComparer<T>
        where T : unmanaged
        where TComparer : unmanaged, IComparer<T>
    {
        private TComparer _comparer;

        public ReverseComparer(TComparer comparer)
        {
            _comparer = comparer;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Compare(T x, T y)
        {
            return _comparer.Compare(y, x);
        }
    }
}
