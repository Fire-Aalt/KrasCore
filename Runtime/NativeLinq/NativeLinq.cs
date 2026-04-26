using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace KrasCore
{
    public interface IPredicate<T>
        where T : unmanaged
    {
        bool Match(in T value);
    }

    public interface ISelector<TSource, out TResult>
        where TSource : unmanaged
        where TResult : unmanaged
    {
        TResult Select(in TSource value);
    }

    public interface INativeAccumulator<T>
        where T : unmanaged
    {
        T Zero();

        T Add(in T total, in T value);

        T Divide(in T total, int count);
    }

    public interface INativeEqualityComparer<T>
        where T : unmanaged
    {
        bool Equals(in T left, in T right);
    }

    public static class NativeLinq
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NativeEnumerable<T, TEnumerator> From<T, TEnumerator>(TEnumerator enumerator)
            where T : unmanaged
            where TEnumerator : unmanaged, IEnumerator<T>
        {
            return new NativeEnumerable<T, TEnumerator>(enumerator);
        }
    }

    public static class NativeLinqExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NativeEnumerable<T, NativeArray<T>.Enumerator> AsNativeEnumerable<T>(this NativeArray<T> collection)
            where T : unmanaged
        {
            return new NativeEnumerable<T, NativeArray<T>.Enumerator>(collection.GetEnumerator());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NativeEnumerable<T, NativeArray<T>.Enumerator> AsNativeEnumerable<T>(this NativeList<T> collection)
            where T : unmanaged
        {
            return new NativeEnumerable<T, NativeArray<T>.Enumerator>(collection.GetEnumerator());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NativeEnumerable<T, UnsafeList<T>.Enumerator> AsNativeEnumerable<T>(this UnsafeList<T> collection)
            where T : unmanaged
        {
            return new NativeEnumerable<T, UnsafeList<T>.Enumerator>(collection.GetEnumerator());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NativeEnumerable<T, NativeQueue<T>.Enumerator> AsNativeEnumerable<T>(this NativeQueue<T> collection)
            where T : unmanaged
        {
            return new NativeEnumerable<T, NativeQueue<T>.Enumerator>(collection.AsReadOnly().GetEnumerator());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NativeEnumerable<T, NativeHashSet<T>.Enumerator> AsNativeEnumerable<T>(this NativeHashSet<T> collection)
            where T : unmanaged, IEquatable<T>
        {
            return new NativeEnumerable<T, NativeHashSet<T>.Enumerator>(collection.GetEnumerator());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NativeEnumerable<T, NativeParallelHashSet<T>.Enumerator> AsNativeEnumerable<T>(this NativeParallelHashSet<T> collection)
            where T : unmanaged, IEquatable<T>
        {
            return new NativeEnumerable<T, NativeParallelHashSet<T>.Enumerator>(collection.GetEnumerator());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NativeList<T> OrderBy<T, TEnumerator>(
            this NativeEnumerable<T, TEnumerator> source,
            AllocatorManager.AllocatorHandle allocator)
            where T : unmanaged, IComparable<T>
            where TEnumerator : unmanaged, IEnumerator<T>
        {
            return source.OrderBy(new NativeAscendingComparer<T>(), allocator);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NativeList<T> OrderByDescending<T, TEnumerator>(
            this NativeEnumerable<T, TEnumerator> source,
            AllocatorManager.AllocatorHandle allocator)
            where T : unmanaged, IComparable<T>
            where TEnumerator : unmanaged, IEnumerator<T>
        {
            return source.OrderBy(new NativeDescendingComparer<T>(), allocator);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NativeList<T> OrderBy<T, TEnumerator, TPredicate>(
            this NativeWhereEnumerable<T, TEnumerator, TPredicate> source,
            AllocatorManager.AllocatorHandle allocator)
            where T : unmanaged, IComparable<T>
            where TEnumerator : unmanaged, IEnumerator<T>
            where TPredicate : unmanaged, IPredicate<T>
        {
            return source.OrderBy(new NativeAscendingComparer<T>(), allocator);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NativeList<T> OrderByDescending<T, TEnumerator, TPredicate>(
            this NativeWhereEnumerable<T, TEnumerator, TPredicate> source,
            AllocatorManager.AllocatorHandle allocator)
            where T : unmanaged, IComparable<T>
            where TEnumerator : unmanaged, IEnumerator<T>
            where TPredicate : unmanaged, IPredicate<T>
        {
            return source.OrderBy(new NativeDescendingComparer<T>(), allocator);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NativeList<TResult> OrderBy<TSource, TResult, TEnumerator, TSelector>(
            this NativeSelectEnumerable<TSource, TResult, TEnumerator, TSelector> source,
            AllocatorManager.AllocatorHandle allocator)
            where TSource : unmanaged
            where TResult : unmanaged, IComparable<TResult>
            where TEnumerator : unmanaged, IEnumerator<TSource>
            where TSelector : unmanaged, ISelector<TSource, TResult>
        {
            return source.OrderBy(new NativeAscendingComparer<TResult>(), allocator);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NativeList<TResult> OrderByDescending<TSource, TResult, TEnumerator, TSelector>(
            this NativeSelectEnumerable<TSource, TResult, TEnumerator, TSelector> source,
            AllocatorManager.AllocatorHandle allocator)
            where TSource : unmanaged
            where TResult : unmanaged, IComparable<TResult>
            where TEnumerator : unmanaged, IEnumerator<TSource>
            where TSelector : unmanaged, ISelector<TSource, TResult>
        {
            return source.OrderBy(new NativeDescendingComparer<TResult>(), allocator);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NativeList<TResult> OrderBy<TSource, TResult, TSourceEnumerator, TInnerEnumerator, TSelector>(
            this NativeSelectManyEnumerable<TSource, TResult, TSourceEnumerator, TInnerEnumerator, TSelector> source,
            AllocatorManager.AllocatorHandle allocator)
            where TSource : unmanaged
            where TResult : unmanaged, IComparable<TResult>
            where TSourceEnumerator : unmanaged, IEnumerator<TSource>
            where TInnerEnumerator : unmanaged, IEnumerator<TResult>
            where TSelector : unmanaged, ISelector<TSource, TInnerEnumerator>
        {
            return source.OrderBy(new NativeAscendingComparer<TResult>(), allocator);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NativeList<TResult> OrderByDescending<TSource, TResult, TSourceEnumerator, TInnerEnumerator, TSelector>(
            this NativeSelectManyEnumerable<TSource, TResult, TSourceEnumerator, TInnerEnumerator, TSelector> source,
            AllocatorManager.AllocatorHandle allocator)
            where TSource : unmanaged
            where TResult : unmanaged, IComparable<TResult>
            where TSourceEnumerator : unmanaged, IEnumerator<TSource>
            where TInnerEnumerator : unmanaged, IEnumerator<TResult>
            where TSelector : unmanaged, ISelector<TSource, TInnerEnumerator>
        {
            return source.OrderBy(new NativeDescendingComparer<TResult>(), allocator);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Min<T, TEnumerator>(this NativeEnumerable<T, TEnumerator> source)
            where T : unmanaged, IComparable<T>
            where TEnumerator : unmanaged, IEnumerator<T>
        {
            return source.Min(new NativeAscendingComparer<T>());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Max<T, TEnumerator>(this NativeEnumerable<T, TEnumerator> source)
            where T : unmanaged, IComparable<T>
            where TEnumerator : unmanaged, IEnumerator<T>
        {
            return source.Max(new NativeAscendingComparer<T>());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Contains<T, TEnumerator>(this NativeEnumerable<T, TEnumerator> source, T value)
            where T : unmanaged, IEquatable<T>
            where TEnumerator : unmanaged, IEnumerator<T>
        {
            return source.Contains(value, new NativeEqualityComparer<T>());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool SequenceEquals<T, TEnumerator, TOtherEnumerator>(
            this NativeEnumerable<T, TEnumerator> source,
            NativeEnumerable<T, TOtherEnumerator> other)
            where T : unmanaged, IEquatable<T>
            where TEnumerator : unmanaged, IEnumerator<T>
            where TOtherEnumerator : unmanaged, IEnumerator<T>
        {
            return source.SequenceEquals(other.GetEnumerator(), new NativeEqualityComparer<T>());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Min<T, TEnumerator, TPredicate>(this NativeWhereEnumerable<T, TEnumerator, TPredicate> source)
            where T : unmanaged, IComparable<T>
            where TEnumerator : unmanaged, IEnumerator<T>
            where TPredicate : unmanaged, IPredicate<T>
        {
            return source.Min(new NativeAscendingComparer<T>());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Max<T, TEnumerator, TPredicate>(this NativeWhereEnumerable<T, TEnumerator, TPredicate> source)
            where T : unmanaged, IComparable<T>
            where TEnumerator : unmanaged, IEnumerator<T>
            where TPredicate : unmanaged, IPredicate<T>
        {
            return source.Max(new NativeAscendingComparer<T>());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Contains<T, TEnumerator, TPredicate>(
            this NativeWhereEnumerable<T, TEnumerator, TPredicate> source,
            T value)
            where T : unmanaged, IEquatable<T>
            where TEnumerator : unmanaged, IEnumerator<T>
            where TPredicate : unmanaged, IPredicate<T>
        {
            return source.Contains(value, new NativeEqualityComparer<T>());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool SequenceEquals<T, TEnumerator, TPredicate, TOtherEnumerator>(
            this NativeWhereEnumerable<T, TEnumerator, TPredicate> source,
            NativeEnumerable<T, TOtherEnumerator> other)
            where T : unmanaged, IEquatable<T>
            where TEnumerator : unmanaged, IEnumerator<T>
            where TPredicate : unmanaged, IPredicate<T>
            where TOtherEnumerator : unmanaged, IEnumerator<T>
        {
            return source.SequenceEquals(other.GetEnumerator(), new NativeEqualityComparer<T>());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TResult Min<TSource, TResult, TEnumerator, TSelector>(
            this NativeSelectEnumerable<TSource, TResult, TEnumerator, TSelector> source)
            where TSource : unmanaged
            where TResult : unmanaged, IComparable<TResult>
            where TEnumerator : unmanaged, IEnumerator<TSource>
            where TSelector : unmanaged, ISelector<TSource, TResult>
        {
            return source.Min(new NativeAscendingComparer<TResult>());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TResult Max<TSource, TResult, TEnumerator, TSelector>(
            this NativeSelectEnumerable<TSource, TResult, TEnumerator, TSelector> source)
            where TSource : unmanaged
            where TResult : unmanaged, IComparable<TResult>
            where TEnumerator : unmanaged, IEnumerator<TSource>
            where TSelector : unmanaged, ISelector<TSource, TResult>
        {
            return source.Max(new NativeAscendingComparer<TResult>());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Contains<TSource, TResult, TEnumerator, TSelector>(
            this NativeSelectEnumerable<TSource, TResult, TEnumerator, TSelector> source,
            TResult value)
            where TSource : unmanaged
            where TResult : unmanaged, IEquatable<TResult>
            where TEnumerator : unmanaged, IEnumerator<TSource>
            where TSelector : unmanaged, ISelector<TSource, TResult>
        {
            return source.Contains(value, new NativeEqualityComparer<TResult>());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool SequenceEquals<TSource, TResult, TEnumerator, TSelector, TOtherEnumerator>(
            this NativeSelectEnumerable<TSource, TResult, TEnumerator, TSelector> source,
            NativeEnumerable<TResult, TOtherEnumerator> other)
            where TSource : unmanaged
            where TResult : unmanaged, IEquatable<TResult>
            where TEnumerator : unmanaged, IEnumerator<TSource>
            where TSelector : unmanaged, ISelector<TSource, TResult>
            where TOtherEnumerator : unmanaged, IEnumerator<TResult>
        {
            return source.SequenceEquals(other.GetEnumerator(), new NativeEqualityComparer<TResult>());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TResult Min<TSource, TResult, TSourceEnumerator, TInnerEnumerator, TSelector>(
            this NativeSelectManyEnumerable<TSource, TResult, TSourceEnumerator, TInnerEnumerator, TSelector> source)
            where TSource : unmanaged
            where TResult : unmanaged, IComparable<TResult>
            where TSourceEnumerator : unmanaged, IEnumerator<TSource>
            where TInnerEnumerator : unmanaged, IEnumerator<TResult>
            where TSelector : unmanaged, ISelector<TSource, TInnerEnumerator>
        {
            return source.Min(new NativeAscendingComparer<TResult>());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TResult Max<TSource, TResult, TSourceEnumerator, TInnerEnumerator, TSelector>(
            this NativeSelectManyEnumerable<TSource, TResult, TSourceEnumerator, TInnerEnumerator, TSelector> source)
            where TSource : unmanaged
            where TResult : unmanaged, IComparable<TResult>
            where TSourceEnumerator : unmanaged, IEnumerator<TSource>
            where TInnerEnumerator : unmanaged, IEnumerator<TResult>
            where TSelector : unmanaged, ISelector<TSource, TInnerEnumerator>
        {
            return source.Max(new NativeAscendingComparer<TResult>());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Contains<TSource, TResult, TSourceEnumerator, TInnerEnumerator, TSelector>(
            this NativeSelectManyEnumerable<TSource, TResult, TSourceEnumerator, TInnerEnumerator, TSelector> source,
            TResult value)
            where TSource : unmanaged
            where TResult : unmanaged, IEquatable<TResult>
            where TSourceEnumerator : unmanaged, IEnumerator<TSource>
            where TInnerEnumerator : unmanaged, IEnumerator<TResult>
            where TSelector : unmanaged, ISelector<TSource, TInnerEnumerator>
        {
            return source.Contains(value, new NativeEqualityComparer<TResult>());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool SequenceEquals<TSource, TResult, TSourceEnumerator, TInnerEnumerator, TSelector, TOtherEnumerator>(
            this NativeSelectManyEnumerable<TSource, TResult, TSourceEnumerator, TInnerEnumerator, TSelector> source,
            NativeEnumerable<TResult, TOtherEnumerator> other)
            where TSource : unmanaged
            where TResult : unmanaged, IEquatable<TResult>
            where TSourceEnumerator : unmanaged, IEnumerator<TSource>
            where TInnerEnumerator : unmanaged, IEnumerator<TResult>
            where TSelector : unmanaged, ISelector<TSource, TInnerEnumerator>
            where TOtherEnumerator : unmanaged, IEnumerator<TResult>
        {
            return source.SequenceEquals(other.GetEnumerator(), new NativeEqualityComparer<TResult>());
        }
    }

    public struct NativeAscendingComparer<T> : IComparer<T>
        where T : unmanaged, IComparable<T>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Compare(T x, T y)
        {
            return x.CompareTo(y);
        }
    }

    public struct NativeDescendingComparer<T> : IComparer<T>
        where T : unmanaged, IComparable<T>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Compare(T x, T y)
        {
            return y.CompareTo(x);
        }
    }

    public struct NativeEqualityComparer<T> : INativeEqualityComparer<T>
        where T : unmanaged, IEquatable<T>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(in T left, in T right)
        {
            return left.Equals(right);
        }
    }

    public struct NativeIntAccumulator : INativeAccumulator<int>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Zero()
        {
            return 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Add(in int total, in int value)
        {
            return total + value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Divide(in int total, int count)
        {
            return total / count;
        }
    }

    public struct NativeFloatAccumulator : INativeAccumulator<float>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float Zero()
        {
            return 0f;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float Add(in float total, in float value)
        {
            return total + value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float Divide(in float total, int count)
        {
            return total / count;
        }
    }

    public struct NativeFloat2Accumulator : INativeAccumulator<float2>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float2 Zero()
        {
            return default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float2 Add(in float2 total, in float2 value)
        {
            return total + value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float2 Divide(in float2 total, int count)
        {
            return total / count;
        }
    }

    public struct NativeFloat3Accumulator : INativeAccumulator<float3>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float3 Zero()
        {
            return default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float3 Add(in float3 total, in float3 value)
        {
            return total + value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float3 Divide(in float3 total, int count)
        {
            return total / count;
        }
    }

    public struct NativeFloat4Accumulator : INativeAccumulator<float4>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float4 Zero()
        {
            return default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float4 Add(in float4 total, in float4 value)
        {
            return total + value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float4 Divide(in float4 total, int count)
        {
            return total / count;
        }
    }

    public struct NativeEnumerable<T, TEnumerator>
        where T : unmanaged
        where TEnumerator : unmanaged, IEnumerator<T>
    {
        private TEnumerator _enumerator;

        public NativeEnumerable(TEnumerator enumerator)
        {
            _enumerator = enumerator;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Enumerator GetEnumerator()
        {
            return new Enumerator(_enumerator);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeWhereEnumerable<T, Enumerator, TPredicate> Where<TPredicate>(TPredicate predicate)
            where TPredicate : unmanaged, IPredicate<T>
        {
            return new NativeWhereEnumerable<T, Enumerator, TPredicate>(GetEnumerator(), predicate);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeSelectEnumerable<T, TResult, Enumerator, TSelector> Select<TResult, TSelector>(TSelector selector)
            where TResult : unmanaged
            where TSelector : unmanaged, ISelector<T, TResult>
        {
            return new NativeSelectEnumerable<T, TResult, Enumerator, TSelector>(GetEnumerator(), selector);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeSelectManyEnumerable<T, TResult, Enumerator, TInnerEnumerator, TSelector> SelectMany<TResult, TInnerEnumerator, TSelector>(
            TSelector selector)
            where TResult : unmanaged
            where TInnerEnumerator : unmanaged, IEnumerator<TResult>
            where TSelector : unmanaged, ISelector<T, TInnerEnumerator>
        {
            return new NativeSelectManyEnumerable<T, TResult, Enumerator, TInnerEnumerator, TSelector>(GetEnumerator(), selector);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeList<T> ToNativeList(AllocatorManager.AllocatorHandle allocator)
        {
            return NativeLinqUtilities.ToNativeList<T, Enumerator>(GetEnumerator(), allocator);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryFirst(out T value)
        {
            return NativeLinqUtilities.TryFirst<T, Enumerator>(GetEnumerator(), out value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T First()
        {
            return NativeLinqUtilities.First<T, Enumerator>(GetEnumerator());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T FirstOrDefault()
        {
            return NativeLinqUtilities.FirstOrDefault<T, Enumerator>(GetEnumerator());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T FirstOrDefault<TPredicate>(TPredicate predicate)
            where TPredicate : unmanaged, IPredicate<T>
        {
            return NativeLinqUtilities.FirstOrDefault<T, Enumerator, TPredicate>(GetEnumerator(), predicate);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Sum<TAccumulator>(TAccumulator accumulator)
            where TAccumulator : unmanaged, INativeAccumulator<T>
        {
            return NativeLinqUtilities.Sum<T, Enumerator, TAccumulator>(GetEnumerator(), accumulator);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Average<TAccumulator>(TAccumulator accumulator)
            where TAccumulator : unmanaged, INativeAccumulator<T>
        {
            return NativeLinqUtilities.Average<T, Enumerator, TAccumulator>(GetEnumerator(), accumulator);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Min<TComparer>(TComparer comparer)
            where TComparer : unmanaged, IComparer<T>
        {
            return NativeLinqUtilities.Min<T, Enumerator, TComparer>(GetEnumerator(), comparer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Max<TComparer>(TComparer comparer)
            where TComparer : unmanaged, IComparer<T>
        {
            return NativeLinqUtilities.Max<T, Enumerator, TComparer>(GetEnumerator(), comparer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains<TEqualityComparer>(T value, TEqualityComparer comparer)
            where TEqualityComparer : unmanaged, INativeEqualityComparer<T>
        {
            return NativeLinqUtilities.Contains<T, Enumerator, TEqualityComparer>(GetEnumerator(), value, comparer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool SequenceEquals<TOtherEnumerator, TEqualityComparer>(
            TOtherEnumerator other,
            TEqualityComparer comparer)
            where TOtherEnumerator : unmanaged, IEnumerator<T>
            where TEqualityComparer : unmanaged, INativeEqualityComparer<T>
        {
            return NativeLinqUtilities.SequenceEquals<T, Enumerator, TOtherEnumerator, TEqualityComparer>(
                GetEnumerator(),
                other,
                comparer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeList<T> OrderBy<TComparer>(TComparer comparer, AllocatorManager.AllocatorHandle allocator)
            where TComparer : unmanaged, IComparer<T>
        {
            return NativeLinqUtilities.OrderBy<T, Enumerator, TComparer>(GetEnumerator(), comparer, allocator);
        }

        public struct Enumerator : IEnumerator<T>
        {
            private TEnumerator _source;

            public Enumerator(TEnumerator source)
            {
                _source = source;
            }

            public T Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _source.Current;
            }

            object System.Collections.IEnumerator.Current => Current;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                return _source.MoveNext();
            }

            public void Reset()
            {
                _source.Reset();
            }

            public void Dispose()
            {
                _source.Dispose();
            }
        }
    }

    public struct NativeWhereEnumerable<T, TEnumerator, TPredicate>
        where T : unmanaged
        where TEnumerator : unmanaged, IEnumerator<T>
        where TPredicate : unmanaged, IPredicate<T>
    {
        private TEnumerator _enumerator;
        private TPredicate _predicate;

        public NativeWhereEnumerable(TEnumerator enumerator, TPredicate predicate)
        {
            _enumerator = enumerator;
            _predicate = predicate;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Enumerator GetEnumerator()
        {
            return new Enumerator(_enumerator, _predicate);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeWhereEnumerable<T, Enumerator, TNextPredicate> Where<TNextPredicate>(TNextPredicate predicate)
            where TNextPredicate : unmanaged, IPredicate<T>
        {
            return new NativeWhereEnumerable<T, Enumerator, TNextPredicate>(GetEnumerator(), predicate);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeSelectEnumerable<T, TResult, Enumerator, TSelector> Select<TResult, TSelector>(TSelector selector)
            where TResult : unmanaged
            where TSelector : unmanaged, ISelector<T, TResult>
        {
            return new NativeSelectEnumerable<T, TResult, Enumerator, TSelector>(GetEnumerator(), selector);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeSelectManyEnumerable<T, TResult, Enumerator, TInnerEnumerator, TSelector> SelectMany<TResult, TInnerEnumerator, TSelector>(
            TSelector selector)
            where TResult : unmanaged
            where TInnerEnumerator : unmanaged, IEnumerator<TResult>
            where TSelector : unmanaged, ISelector<T, TInnerEnumerator>
        {
            return new NativeSelectManyEnumerable<T, TResult, Enumerator, TInnerEnumerator, TSelector>(GetEnumerator(), selector);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeList<T> ToNativeList(AllocatorManager.AllocatorHandle allocator)
        {
            return NativeLinqUtilities.ToNativeList<T, Enumerator>(GetEnumerator(), allocator);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryFirst(out T value)
        {
            return NativeLinqUtilities.TryFirst<T, Enumerator>(GetEnumerator(), out value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T First()
        {
            return NativeLinqUtilities.First<T, Enumerator>(GetEnumerator());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T FirstOrDefault()
        {
            return NativeLinqUtilities.FirstOrDefault<T, Enumerator>(GetEnumerator());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T FirstOrDefault<TNextPredicate>(TNextPredicate predicate)
            where TNextPredicate : unmanaged, IPredicate<T>
        {
            return NativeLinqUtilities.FirstOrDefault<T, Enumerator, TNextPredicate>(GetEnumerator(), predicate);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Sum<TAccumulator>(TAccumulator accumulator)
            where TAccumulator : unmanaged, INativeAccumulator<T>
        {
            return NativeLinqUtilities.Sum<T, Enumerator, TAccumulator>(GetEnumerator(), accumulator);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Average<TAccumulator>(TAccumulator accumulator)
            where TAccumulator : unmanaged, INativeAccumulator<T>
        {
            return NativeLinqUtilities.Average<T, Enumerator, TAccumulator>(GetEnumerator(), accumulator);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Min<TComparer>(TComparer comparer)
            where TComparer : unmanaged, IComparer<T>
        {
            return NativeLinqUtilities.Min<T, Enumerator, TComparer>(GetEnumerator(), comparer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Max<TComparer>(TComparer comparer)
            where TComparer : unmanaged, IComparer<T>
        {
            return NativeLinqUtilities.Max<T, Enumerator, TComparer>(GetEnumerator(), comparer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains<TEqualityComparer>(T value, TEqualityComparer comparer)
            where TEqualityComparer : unmanaged, INativeEqualityComparer<T>
        {
            return NativeLinqUtilities.Contains<T, Enumerator, TEqualityComparer>(GetEnumerator(), value, comparer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool SequenceEquals<TOtherEnumerator, TEqualityComparer>(
            TOtherEnumerator other,
            TEqualityComparer comparer)
            where TOtherEnumerator : unmanaged, IEnumerator<T>
            where TEqualityComparer : unmanaged, INativeEqualityComparer<T>
        {
            return NativeLinqUtilities.SequenceEquals<T, Enumerator, TOtherEnumerator, TEqualityComparer>(
                GetEnumerator(),
                other,
                comparer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeList<T> OrderBy<TComparer>(TComparer comparer, AllocatorManager.AllocatorHandle allocator)
            where TComparer : unmanaged, IComparer<T>
        {
            return NativeLinqUtilities.OrderBy<T, Enumerator, TComparer>(GetEnumerator(), comparer, allocator);
        }

        public struct Enumerator : IEnumerator<T>
        {
            private TEnumerator _source;
            private TPredicate _predicate;
            private T _current;

            public Enumerator(TEnumerator source, TPredicate predicate)
            {
                _source = source;
                _predicate = predicate;
                _current = default;
            }

            public T Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _current;
            }

            object System.Collections.IEnumerator.Current => Current;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                while (_source.MoveNext())
                {
                    var value = _source.Current;
                    if (!_predicate.Match(in value))
                    {
                        continue;
                    }

                    _current = value;
                    return true;
                }

                return false;
            }

            public void Reset()
            {
                _source.Reset();
                _current = default;
            }

            public void Dispose()
            {
                _source.Dispose();
            }
        }
    }

    public struct NativeSelectEnumerable<TSource, TResult, TEnumerator, TSelector>
        where TSource : unmanaged
        where TResult : unmanaged
        where TEnumerator : unmanaged, IEnumerator<TSource>
        where TSelector : unmanaged, ISelector<TSource, TResult>
    {
        private TEnumerator _enumerator;
        private TSelector _selector;

        public NativeSelectEnumerable(TEnumerator enumerator, TSelector selector)
        {
            _enumerator = enumerator;
            _selector = selector;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Enumerator GetEnumerator()
        {
            return new Enumerator(_enumerator, _selector);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeWhereEnumerable<TResult, Enumerator, TPredicate> Where<TPredicate>(TPredicate predicate)
            where TPredicate : unmanaged, IPredicate<TResult>
        {
            return new NativeWhereEnumerable<TResult, Enumerator, TPredicate>(GetEnumerator(), predicate);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeSelectEnumerable<TResult, TNextResult, Enumerator, TNextSelector> Select<TNextResult, TNextSelector>(TNextSelector selector)
            where TNextResult : unmanaged
            where TNextSelector : unmanaged, ISelector<TResult, TNextResult>
        {
            return new NativeSelectEnumerable<TResult, TNextResult, Enumerator, TNextSelector>(GetEnumerator(), selector);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeSelectManyEnumerable<TResult, TNextResult, Enumerator, TInnerEnumerator, TNextSelector> SelectMany<TNextResult, TInnerEnumerator, TNextSelector>(
            TNextSelector selector)
            where TNextResult : unmanaged
            where TInnerEnumerator : unmanaged, IEnumerator<TNextResult>
            where TNextSelector : unmanaged, ISelector<TResult, TInnerEnumerator>
        {
            return new NativeSelectManyEnumerable<TResult, TNextResult, Enumerator, TInnerEnumerator, TNextSelector>(GetEnumerator(), selector);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeList<TResult> ToNativeList(AllocatorManager.AllocatorHandle allocator)
        {
            return NativeLinqUtilities.ToNativeList<TResult, Enumerator>(GetEnumerator(), allocator);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryFirst(out TResult value)
        {
            return NativeLinqUtilities.TryFirst<TResult, Enumerator>(GetEnumerator(), out value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TResult First()
        {
            return NativeLinqUtilities.First<TResult, Enumerator>(GetEnumerator());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TResult FirstOrDefault()
        {
            return NativeLinqUtilities.FirstOrDefault<TResult, Enumerator>(GetEnumerator());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TResult FirstOrDefault<TPredicate>(TPredicate predicate)
            where TPredicate : unmanaged, IPredicate<TResult>
        {
            return NativeLinqUtilities.FirstOrDefault<TResult, Enumerator, TPredicate>(GetEnumerator(), predicate);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TResult Sum<TAccumulator>(TAccumulator accumulator)
            where TAccumulator : unmanaged, INativeAccumulator<TResult>
        {
            return NativeLinqUtilities.Sum<TResult, Enumerator, TAccumulator>(GetEnumerator(), accumulator);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TResult Average<TAccumulator>(TAccumulator accumulator)
            where TAccumulator : unmanaged, INativeAccumulator<TResult>
        {
            return NativeLinqUtilities.Average<TResult, Enumerator, TAccumulator>(GetEnumerator(), accumulator);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TResult Min<TComparer>(TComparer comparer)
            where TComparer : unmanaged, IComparer<TResult>
        {
            return NativeLinqUtilities.Min<TResult, Enumerator, TComparer>(GetEnumerator(), comparer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TResult Max<TComparer>(TComparer comparer)
            where TComparer : unmanaged, IComparer<TResult>
        {
            return NativeLinqUtilities.Max<TResult, Enumerator, TComparer>(GetEnumerator(), comparer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains<TEqualityComparer>(TResult value, TEqualityComparer comparer)
            where TEqualityComparer : unmanaged, INativeEqualityComparer<TResult>
        {
            return NativeLinqUtilities.Contains<TResult, Enumerator, TEqualityComparer>(GetEnumerator(), value, comparer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool SequenceEquals<TOtherEnumerator, TEqualityComparer>(
            TOtherEnumerator other,
            TEqualityComparer comparer)
            where TOtherEnumerator : unmanaged, IEnumerator<TResult>
            where TEqualityComparer : unmanaged, INativeEqualityComparer<TResult>
        {
            return NativeLinqUtilities.SequenceEquals<TResult, Enumerator, TOtherEnumerator, TEqualityComparer>(
                GetEnumerator(),
                other,
                comparer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeList<TResult> OrderBy<TComparer>(TComparer comparer, AllocatorManager.AllocatorHandle allocator)
            where TComparer : unmanaged, IComparer<TResult>
        {
            return NativeLinqUtilities.OrderBy<TResult, Enumerator, TComparer>(GetEnumerator(), comparer, allocator);
        }

        public struct Enumerator : IEnumerator<TResult>
        {
            private TEnumerator _source;
            private TSelector _selector;

            public Enumerator(TEnumerator source, TSelector selector)
            {
                _source = source;
                _selector = selector;
            }

            public TResult Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    var value = _source.Current;
                    return _selector.Select(in value);
                }
            }

            object System.Collections.IEnumerator.Current => Current;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                return _source.MoveNext();
            }

            public void Reset()
            {
                _source.Reset();
            }

            public void Dispose()
            {
                _source.Dispose();
            }
        }
    }

    public struct NativeSelectManyEnumerable<TSource, TResult, TSourceEnumerator, TInnerEnumerator, TSelector>
        where TSource : unmanaged
        where TResult : unmanaged
        where TSourceEnumerator : unmanaged, IEnumerator<TSource>
        where TInnerEnumerator : unmanaged, IEnumerator<TResult>
        where TSelector : unmanaged, ISelector<TSource, TInnerEnumerator>
    {
        private TSourceEnumerator _sourceEnumerator;
        private TSelector _selector;

        public NativeSelectManyEnumerable(TSourceEnumerator sourceEnumerator, TSelector selector)
        {
            _sourceEnumerator = sourceEnumerator;
            _selector = selector;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Enumerator GetEnumerator()
        {
            return new Enumerator(_sourceEnumerator, _selector);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeWhereEnumerable<TResult, Enumerator, TPredicate> Where<TPredicate>(TPredicate predicate)
            where TPredicate : unmanaged, IPredicate<TResult>
        {
            return new NativeWhereEnumerable<TResult, Enumerator, TPredicate>(GetEnumerator(), predicate);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeSelectEnumerable<TResult, TNextResult, Enumerator, TNextSelector> Select<TNextResult, TNextSelector>(TNextSelector selector)
            where TNextResult : unmanaged
            where TNextSelector : unmanaged, ISelector<TResult, TNextResult>
        {
            return new NativeSelectEnumerable<TResult, TNextResult, Enumerator, TNextSelector>(GetEnumerator(), selector);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeSelectManyEnumerable<TResult, TNextResult, Enumerator, TNextInnerEnumerator, TNextSelector> SelectMany<TNextResult, TNextInnerEnumerator, TNextSelector>(
            TNextSelector selector)
            where TNextResult : unmanaged
            where TNextInnerEnumerator : unmanaged, IEnumerator<TNextResult>
            where TNextSelector : unmanaged, ISelector<TResult, TNextInnerEnumerator>
        {
            return new NativeSelectManyEnumerable<TResult, TNextResult, Enumerator, TNextInnerEnumerator, TNextSelector>(GetEnumerator(), selector);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeList<TResult> ToNativeList(AllocatorManager.AllocatorHandle allocator)
        {
            return NativeLinqUtilities.ToNativeList<TResult, Enumerator>(GetEnumerator(), allocator);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryFirst(out TResult value)
        {
            return NativeLinqUtilities.TryFirst<TResult, Enumerator>(GetEnumerator(), out value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TResult First()
        {
            return NativeLinqUtilities.First<TResult, Enumerator>(GetEnumerator());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TResult FirstOrDefault()
        {
            return NativeLinqUtilities.FirstOrDefault<TResult, Enumerator>(GetEnumerator());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TResult FirstOrDefault<TPredicate>(TPredicate predicate)
            where TPredicate : unmanaged, IPredicate<TResult>
        {
            return NativeLinqUtilities.FirstOrDefault<TResult, Enumerator, TPredicate>(GetEnumerator(), predicate);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TResult Sum<TAccumulator>(TAccumulator accumulator)
            where TAccumulator : unmanaged, INativeAccumulator<TResult>
        {
            return NativeLinqUtilities.Sum<TResult, Enumerator, TAccumulator>(GetEnumerator(), accumulator);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TResult Average<TAccumulator>(TAccumulator accumulator)
            where TAccumulator : unmanaged, INativeAccumulator<TResult>
        {
            return NativeLinqUtilities.Average<TResult, Enumerator, TAccumulator>(GetEnumerator(), accumulator);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TResult Min<TComparer>(TComparer comparer)
            where TComparer : unmanaged, IComparer<TResult>
        {
            return NativeLinqUtilities.Min<TResult, Enumerator, TComparer>(GetEnumerator(), comparer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TResult Max<TComparer>(TComparer comparer)
            where TComparer : unmanaged, IComparer<TResult>
        {
            return NativeLinqUtilities.Max<TResult, Enumerator, TComparer>(GetEnumerator(), comparer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains<TEqualityComparer>(TResult value, TEqualityComparer comparer)
            where TEqualityComparer : unmanaged, INativeEqualityComparer<TResult>
        {
            return NativeLinqUtilities.Contains<TResult, Enumerator, TEqualityComparer>(GetEnumerator(), value, comparer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool SequenceEquals<TOtherEnumerator, TEqualityComparer>(
            TOtherEnumerator other,
            TEqualityComparer comparer)
            where TOtherEnumerator : unmanaged, IEnumerator<TResult>
            where TEqualityComparer : unmanaged, INativeEqualityComparer<TResult>
        {
            return NativeLinqUtilities.SequenceEquals<TResult, Enumerator, TOtherEnumerator, TEqualityComparer>(
                GetEnumerator(),
                other,
                comparer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeList<TResult> OrderBy<TComparer>(TComparer comparer, AllocatorManager.AllocatorHandle allocator)
            where TComparer : unmanaged, IComparer<TResult>
        {
            return NativeLinqUtilities.OrderBy<TResult, Enumerator, TComparer>(GetEnumerator(), comparer, allocator);
        }

        public struct Enumerator : IEnumerator<TResult>
        {
            private TSourceEnumerator _source;
            private TInnerEnumerator _inner;
            private TSelector _selector;
            private TResult _current;
            private bool _hasInner;

            public Enumerator(TSourceEnumerator source, TSelector selector)
            {
                _source = source;
                _inner = default;
                _selector = selector;
                _current = default;
                _hasInner = false;
            }

            public TResult Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _current;
            }

            object System.Collections.IEnumerator.Current => Current;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                while (true)
                {
                    if (_hasInner && _inner.MoveNext())
                    {
                        _current = _inner.Current;
                        return true;
                    }

                    if (_hasInner)
                    {
                        _inner.Dispose();
                    }

                    if (!_source.MoveNext())
                    {
                        _hasInner = false;
                        return false;
                    }

                    var value = _source.Current;
                    _inner = _selector.Select(in value);
                    _hasInner = true;
                }
            }

            public void Reset()
            {
                if (_hasInner)
                {
                    _inner.Dispose();
                }

                _source.Reset();
                _inner = default;
                _current = default;
                _hasInner = false;
            }

            public void Dispose()
            {
                if (_hasInner)
                {
                    _inner.Dispose();
                }

                _source.Dispose();
            }
        }
    }

    internal static class NativeLinqUtilities
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NativeList<T> ToNativeList<T, TEnumerator>(TEnumerator enumerator, AllocatorManager.AllocatorHandle allocator)
            where T : unmanaged
            where TEnumerator : unmanaged, IEnumerator<T>
        {
            var list = new NativeList<T>(allocator);
            while (enumerator.MoveNext())
            {
                list.Add(enumerator.Current);
            }

            enumerator.Dispose();
            return list;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NativeList<T> OrderBy<T, TEnumerator, TComparer>(
            TEnumerator enumerator,
            TComparer comparer,
            AllocatorManager.AllocatorHandle allocator)
            where T : unmanaged
            where TEnumerator : unmanaged, IEnumerator<T>
            where TComparer : unmanaged, IComparer<T>
        {
            var list = ToNativeList<T, TEnumerator>(enumerator, allocator);
            list.Sort(comparer);
            return list;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryFirst<T, TEnumerator>(TEnumerator enumerator, out T value)
            where T : unmanaged
            where TEnumerator : unmanaged, IEnumerator<T>
        {
            if (enumerator.MoveNext())
            {
                value = enumerator.Current;
                enumerator.Dispose();
                return true;
            }

            value = default;
            enumerator.Dispose();
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T First<T, TEnumerator>(TEnumerator enumerator)
            where T : unmanaged
            where TEnumerator : unmanaged, IEnumerator<T>
        {
            if (TryFirst<T, TEnumerator>(enumerator, out var value))
            {
                return value;
            }

            throw new InvalidOperationException("The NativeLinq source contains no elements.");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T FirstOrDefault<T, TEnumerator>(TEnumerator enumerator)
            where T : unmanaged
            where TEnumerator : unmanaged, IEnumerator<T>
        {
            return TryFirst<T, TEnumerator>(enumerator, out var value) ? value : default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T FirstOrDefault<T, TEnumerator, TPredicate>(TEnumerator enumerator, TPredicate predicate)
            where T : unmanaged
            where TEnumerator : unmanaged, IEnumerator<T>
            where TPredicate : unmanaged, IPredicate<T>
        {
            while (enumerator.MoveNext())
            {
                var value = enumerator.Current;
                if (predicate.Match(in value))
                {
                    enumerator.Dispose();
                    return value;
                }
            }

            enumerator.Dispose();
            return default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Sum<T, TEnumerator, TAccumulator>(TEnumerator enumerator, TAccumulator accumulator)
            where T : unmanaged
            where TEnumerator : unmanaged, IEnumerator<T>
            where TAccumulator : unmanaged, INativeAccumulator<T>
        {
            var total = accumulator.Zero();
            while (enumerator.MoveNext())
            {
                var value = enumerator.Current;
                total = accumulator.Add(in total, in value);
            }

            enumerator.Dispose();
            return total;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Average<T, TEnumerator, TAccumulator>(TEnumerator enumerator, TAccumulator accumulator)
            where T : unmanaged
            where TEnumerator : unmanaged, IEnumerator<T>
            where TAccumulator : unmanaged, INativeAccumulator<T>
        {
            var total = accumulator.Zero();
            var count = 0;
            while (enumerator.MoveNext())
            {
                var value = enumerator.Current;
                total = accumulator.Add(in total, in value);
                count++;
            }

            enumerator.Dispose();
            return count == 0 ? default : accumulator.Divide(in total, count);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Min<T, TEnumerator, TComparer>(TEnumerator enumerator, TComparer comparer)
            where T : unmanaged
            where TEnumerator : unmanaged, IEnumerator<T>
            where TComparer : unmanaged, IComparer<T>
        {
            if (!enumerator.MoveNext())
            {
                enumerator.Dispose();
                throw new InvalidOperationException("The NativeLinq source contains no elements.");
            }

            var best = enumerator.Current;
            while (enumerator.MoveNext())
            {
                var value = enumerator.Current;
                if (comparer.Compare(value, best) < 0)
                {
                    best = value;
                }
            }

            enumerator.Dispose();
            return best;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Max<T, TEnumerator, TComparer>(TEnumerator enumerator, TComparer comparer)
            where T : unmanaged
            where TEnumerator : unmanaged, IEnumerator<T>
            where TComparer : unmanaged, IComparer<T>
        {
            if (!enumerator.MoveNext())
            {
                enumerator.Dispose();
                throw new InvalidOperationException("The NativeLinq source contains no elements.");
            }

            var best = enumerator.Current;
            while (enumerator.MoveNext())
            {
                var value = enumerator.Current;
                if (comparer.Compare(value, best) > 0)
                {
                    best = value;
                }
            }

            enumerator.Dispose();
            return best;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Contains<T, TEnumerator, TEqualityComparer>(
            TEnumerator enumerator,
            T value,
            TEqualityComparer comparer)
            where T : unmanaged
            where TEnumerator : unmanaged, IEnumerator<T>
            where TEqualityComparer : unmanaged, INativeEqualityComparer<T>
        {
            while (enumerator.MoveNext())
            {
                var current = enumerator.Current;
                if (comparer.Equals(in current, in value))
                {
                    enumerator.Dispose();
                    return true;
                }
            }

            enumerator.Dispose();
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool SequenceEquals<T, TLeftEnumerator, TRightEnumerator, TEqualityComparer>(
            TLeftEnumerator left,
            TRightEnumerator right,
            TEqualityComparer comparer)
            where T : unmanaged
            where TLeftEnumerator : unmanaged, IEnumerator<T>
            where TRightEnumerator : unmanaged, IEnumerator<T>
            where TEqualityComparer : unmanaged, INativeEqualityComparer<T>
        {
            while (true)
            {
                var leftHasValue = left.MoveNext();
                var rightHasValue = right.MoveNext();
                if (leftHasValue != rightHasValue)
                {
                    left.Dispose();
                    right.Dispose();
                    return false;
                }

                if (!leftHasValue)
                {
                    left.Dispose();
                    right.Dispose();
                    return true;
                }

                var leftValue = left.Current;
                var rightValue = right.Current;
                if (!comparer.Equals(in leftValue, in rightValue))
                {
                    left.Dispose();
                    right.Dispose();
                    return false;
                }
            }
        }
    }
}
