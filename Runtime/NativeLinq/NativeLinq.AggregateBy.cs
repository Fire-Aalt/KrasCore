using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Collections;

namespace KrasCore
{
    public static partial class NativeLinqExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Query<KeyValuePair<TAccumulate, TAccumulate>, NativeArray<KeyValuePair<TAccumulate, TAccumulate>>.Enumerator>
            AggregateBy<TSource, TAccumulate, TEnumerator, TKeySelector, TAggregator>(
                this Query<TSource, TEnumerator> source,
                TKeySelector keySelector,
                TAccumulate seed,
                TAggregator aggregator)
            where TSource : unmanaged
            where TAccumulate : unmanaged, IEquatable<TAccumulate>
            where TEnumerator : unmanaged, IEnumerator<TSource>
            where TKeySelector : unmanaged, ISelector<TSource, TAccumulate>
            where TAggregator : unmanaged, IAggregator<TAccumulate, TSource>
        {
            return NativeLinqUtilities.AggregateBy<TSource, TAccumulate, TAccumulate, TEnumerator, TKeySelector, TAggregator>(
                source.GetEnumerator(),
                keySelector,
                seed,
                aggregator,
                Allocator.Temp).AsQuery();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Query<KeyValuePair<TKey, TAccumulate>, NativeArray<KeyValuePair<TKey, TAccumulate>>.Enumerator>
            AggregateBy<TSource, TKey, TAccumulate, TEnumerator, TKeySelector, TAggregator>(
                this Query<TSource, TEnumerator> source,
                TKeySelector keySelector,
                TAccumulate seed,
                TAggregator aggregator)
            where TSource : unmanaged
            where TKey : unmanaged, IEquatable<TKey>
            where TAccumulate : unmanaged
            where TEnumerator : unmanaged, IEnumerator<TSource>
            where TKeySelector : unmanaged, ISelector<TSource, TKey>
            where TAggregator : unmanaged, IAggregator<TAccumulate, TSource>
        {
            return NativeLinqUtilities.AggregateBy<TSource, TKey, TAccumulate, TEnumerator, TKeySelector, TAggregator>(
                source.GetEnumerator(),
                keySelector,
                seed,
                aggregator,
                Allocator.Temp).AsQuery();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Query<KeyValuePair<TKey, TAccumulate>, NativeArray<KeyValuePair<TKey, TAccumulate>>.Enumerator>
            AggregateBy<TSource, TKey, TAccumulate, TEnumerator, TKeySelector, TSeedSelector, TAggregator>(
                this Query<TSource, TEnumerator> source,
                TKeySelector keySelector,
                TSeedSelector seedSelector,
                TAggregator aggregator)
            where TSource : unmanaged
            where TKey : unmanaged, IEquatable<TKey>
            where TAccumulate : unmanaged
            where TEnumerator : unmanaged, IEnumerator<TSource>
            where TKeySelector : unmanaged, ISelector<TSource, TKey>
            where TSeedSelector : unmanaged, ISelector<TKey, TAccumulate>
            where TAggregator : unmanaged, IAggregator<TAccumulate, TSource>
        {
            return NativeLinqUtilities.AggregateBy<TSource, TKey, TAccumulate, TEnumerator, TKeySelector, TSeedSelector, TAggregator>(
                source.GetEnumerator(),
                keySelector,
                seedSelector,
                aggregator,
                Allocator.Temp).AsQuery();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NativeList<KeyValuePair<TAccumulate, TAccumulate>>
            ToAggregateBy<TSource, TAccumulate, TEnumerator, TKeySelector, TAggregator>(
                this Query<TSource, TEnumerator> source,
                TKeySelector keySelector,
                TAccumulate seed,
                TAggregator aggregator,
                AllocatorManager.AllocatorHandle allocator)
            where TSource : unmanaged
            where TAccumulate : unmanaged, IEquatable<TAccumulate>
            where TEnumerator : unmanaged, IEnumerator<TSource>
            where TKeySelector : unmanaged, ISelector<TSource, TAccumulate>
            where TAggregator : unmanaged, IAggregator<TAccumulate, TSource>
        {
            return NativeLinqUtilities.AggregateBy<TSource, TAccumulate, TAccumulate, TEnumerator, TKeySelector, TAggregator>(
                source.GetEnumerator(),
                keySelector,
                seed,
                aggregator,
                allocator);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NativeList<KeyValuePair<TKey, TAccumulate>>
            ToAggregateBy<TSource, TKey, TAccumulate, TEnumerator, TKeySelector, TAggregator>(
                this Query<TSource, TEnumerator> source,
                TKeySelector keySelector,
                TAccumulate seed,
                TAggregator aggregator,
                AllocatorManager.AllocatorHandle allocator)
            where TSource : unmanaged
            where TKey : unmanaged, IEquatable<TKey>
            where TAccumulate : unmanaged
            where TEnumerator : unmanaged, IEnumerator<TSource>
            where TKeySelector : unmanaged, ISelector<TSource, TKey>
            where TAggregator : unmanaged, IAggregator<TAccumulate, TSource>
        {
            return NativeLinqUtilities.AggregateBy<TSource, TKey, TAccumulate, TEnumerator, TKeySelector, TAggregator>(
                source.GetEnumerator(),
                keySelector,
                seed,
                aggregator,
                allocator);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NativeList<KeyValuePair<TKey, TAccumulate>>
            ToAggregateBy<TSource, TKey, TAccumulate, TEnumerator, TKeySelector, TSeedSelector, TAggregator>(
                this Query<TSource, TEnumerator> source,
                TKeySelector keySelector,
                TSeedSelector seedSelector,
                TAggregator aggregator,
                AllocatorManager.AllocatorHandle allocator)
            where TSource : unmanaged
            where TKey : unmanaged, IEquatable<TKey>
            where TAccumulate : unmanaged
            where TEnumerator : unmanaged, IEnumerator<TSource>
            where TKeySelector : unmanaged, ISelector<TSource, TKey>
            where TSeedSelector : unmanaged, ISelector<TKey, TAccumulate>
            where TAggregator : unmanaged, IAggregator<TAccumulate, TSource>
        {
            return NativeLinqUtilities.AggregateBy<TSource, TKey, TAccumulate, TEnumerator, TKeySelector, TSeedSelector, TAggregator>(
                source.GetEnumerator(),
                keySelector,
                seedSelector,
                aggregator,
                allocator);
        }
    }

    public partial struct Query<T, TEnumerator>
        where T : unmanaged
        where TEnumerator : unmanaged, IEnumerator<T>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Query<KeyValuePair<TAccumulate, TAccumulate>, NativeArray<KeyValuePair<TAccumulate, TAccumulate>>.Enumerator>
            AggregateBy<TAccumulate, TKeySelector, TAggregator>(
                TKeySelector keySelector,
                TAccumulate seed,
                TAggregator aggregator)
            where TAccumulate : unmanaged, IEquatable<TAccumulate>
            where TKeySelector : unmanaged, ISelector<T, TAccumulate>
            where TAggregator : unmanaged, IAggregator<TAccumulate, T>
        {
            return NativeLinqUtilities.AggregateBy<T, TAccumulate, TAccumulate, TEnumerator, TKeySelector, TAggregator>(
                GetEnumerator(),
                keySelector,
                seed,
                aggregator,
                Allocator.Temp).AsQuery();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Query<KeyValuePair<TKey, TAccumulate>, NativeArray<KeyValuePair<TKey, TAccumulate>>.Enumerator>
            AggregateBy<TKey, TAccumulate, TKeySelector, TAggregator>(
                TKeySelector keySelector,
                TAccumulate seed,
                TAggregator aggregator)
            where TKey : unmanaged, IEquatable<TKey>
            where TAccumulate : unmanaged
            where TKeySelector : unmanaged, ISelector<T, TKey>
            where TAggregator : unmanaged, IAggregator<TAccumulate, T>
        {
            return NativeLinqUtilities.AggregateBy<T, TKey, TAccumulate, TEnumerator, TKeySelector, TAggregator>(
                GetEnumerator(),
                keySelector,
                seed,
                aggregator,
                Allocator.Temp).AsQuery();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Query<KeyValuePair<TKey, TAccumulate>, NativeArray<KeyValuePair<TKey, TAccumulate>>.Enumerator>
            AggregateBy<TKey, TAccumulate, TKeySelector, TSeedSelector, TAggregator>(
                TKeySelector keySelector,
                TSeedSelector seedSelector,
                TAggregator aggregator)
            where TKey : unmanaged, IEquatable<TKey>
            where TAccumulate : unmanaged
            where TKeySelector : unmanaged, ISelector<T, TKey>
            where TSeedSelector : unmanaged, ISelector<TKey, TAccumulate>
            where TAggregator : unmanaged, IAggregator<TAccumulate, T>
        {
            return NativeLinqUtilities.AggregateBy<T, TKey, TAccumulate, TEnumerator, TKeySelector, TSeedSelector, TAggregator>(
                GetEnumerator(),
                keySelector,
                seedSelector,
                aggregator,
                Allocator.Temp).AsQuery();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeList<KeyValuePair<TAccumulate, TAccumulate>> ToAggregateBy<TAccumulate, TKeySelector, TAggregator>(
            TKeySelector keySelector,
            TAccumulate seed,
            TAggregator aggregator,
            AllocatorManager.AllocatorHandle allocator)
            where TAccumulate : unmanaged, IEquatable<TAccumulate>
            where TKeySelector : unmanaged, ISelector<T, TAccumulate>
            where TAggregator : unmanaged, IAggregator<TAccumulate, T>
        {
            return NativeLinqUtilities.AggregateBy<T, TAccumulate, TAccumulate, TEnumerator, TKeySelector, TAggregator>(
                GetEnumerator(),
                keySelector,
                seed,
                aggregator,
                allocator);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeList<KeyValuePair<TKey, TAccumulate>> ToAggregateBy<TKey, TAccumulate, TKeySelector, TAggregator>(
            TKeySelector keySelector,
            TAccumulate seed,
            TAggregator aggregator,
            AllocatorManager.AllocatorHandle allocator)
            where TKey : unmanaged, IEquatable<TKey>
            where TAccumulate : unmanaged
            where TKeySelector : unmanaged, ISelector<T, TKey>
            where TAggregator : unmanaged, IAggregator<TAccumulate, T>
        {
            return NativeLinqUtilities.AggregateBy<T, TKey, TAccumulate, TEnumerator, TKeySelector, TAggregator>(
                GetEnumerator(),
                keySelector,
                seed,
                aggregator,
                allocator);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeList<KeyValuePair<TKey, TAccumulate>> ToAggregateBy<TKey, TAccumulate, TKeySelector, TSeedSelector, TAggregator>(
            TKeySelector keySelector,
            TSeedSelector seedSelector,
            TAggregator aggregator,
            AllocatorManager.AllocatorHandle allocator)
            where TKey : unmanaged, IEquatable<TKey>
            where TAccumulate : unmanaged
            where TKeySelector : unmanaged, ISelector<T, TKey>
            where TSeedSelector : unmanaged, ISelector<TKey, TAccumulate>
            where TAggregator : unmanaged, IAggregator<TAccumulate, T>
        {
            return NativeLinqUtilities.AggregateBy<T, TKey, TAccumulate, TEnumerator, TKeySelector, TSeedSelector, TAggregator>(
                GetEnumerator(),
                keySelector,
                seedSelector,
                aggregator,
                allocator);
        }
    }

    internal static partial class NativeLinqUtilities
    {
        private const int DEFAULT_AGGREGATE_BY_CAPACITY = 64;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NativeList<KeyValuePair<TKey, TAccumulate>>
            AggregateBy<TSource, TKey, TAccumulate, TEnumerator, TKeySelector, TAggregator>(
                TEnumerator source,
                TKeySelector keySelector,
                TAccumulate seed,
                TAggregator aggregator,
                AllocatorManager.AllocatorHandle allocator)
            where TSource : unmanaged
            where TKey : unmanaged, IEquatable<TKey>
            where TAccumulate : unmanaged
            where TEnumerator : unmanaged, IEnumerator<TSource>
            where TKeySelector : unmanaged, ISelector<TSource, TKey>
            where TAggregator : unmanaged, IAggregator<TAccumulate, TSource>
        {
            var keyToAggregateIndex = new NativeHashMap<TKey, int>(DEFAULT_AGGREGATE_BY_CAPACITY, Allocator.Temp);
            var aggregates = new NativeList<KeyValuePair<TKey, TAccumulate>>(DEFAULT_AGGREGATE_BY_CAPACITY, allocator);

            while (source.MoveNext())
            {
                var value = source.Current;
                var key = keySelector.Select(in value);

                if (!keyToAggregateIndex.TryGetValue(key, out var aggregateIndex))
                {
                    if (aggregates.Length == keyToAggregateIndex.Capacity)
                    {
                        keyToAggregateIndex.Capacity *= 2;
                    }

                    aggregateIndex = aggregates.Length;
                    keyToAggregateIndex.Add(key, aggregateIndex);

                    var aggregate = aggregator.Aggregate(in seed, in value);
                    aggregates.Add(new KeyValuePair<TKey, TAccumulate>(key, aggregate));
                }
                else
                {
                    ref var aggregatePair = ref aggregates.ElementAt(aggregateIndex);
                    var current = aggregatePair.Value;
                    var aggregate = aggregator.Aggregate(in current, in value);
                    aggregatePair = new KeyValuePair<TKey, TAccumulate>(aggregatePair.Key, aggregate);
                }
            }

            source.Dispose();
            return aggregates;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NativeList<KeyValuePair<TKey, TAccumulate>>
            AggregateBy<TSource, TKey, TAccumulate, TEnumerator, TKeySelector, TSeedSelector, TAggregator>(
                TEnumerator source,
                TKeySelector keySelector,
                TSeedSelector seedSelector,
                TAggregator aggregator,
                AllocatorManager.AllocatorHandle allocator)
            where TSource : unmanaged
            where TKey : unmanaged, IEquatable<TKey>
            where TAccumulate : unmanaged
            where TEnumerator : unmanaged, IEnumerator<TSource>
            where TKeySelector : unmanaged, ISelector<TSource, TKey>
            where TSeedSelector : unmanaged, ISelector<TKey, TAccumulate>
            where TAggregator : unmanaged, IAggregator<TAccumulate, TSource>
        {
            var keyToAggregateIndex = new NativeHashMap<TKey, int>(DEFAULT_AGGREGATE_BY_CAPACITY, Allocator.Temp);
            var aggregates = new NativeList<KeyValuePair<TKey, TAccumulate>>(DEFAULT_AGGREGATE_BY_CAPACITY, allocator);

            while (source.MoveNext())
            {
                var value = source.Current;
                var key = keySelector.Select(in value);

                if (!keyToAggregateIndex.TryGetValue(key, out var aggregateIndex))
                {
                    if (aggregates.Length == keyToAggregateIndex.Capacity)
                    {
                        keyToAggregateIndex.Capacity *= 2;
                    }

                    aggregateIndex = aggregates.Length;
                    keyToAggregateIndex.Add(key, aggregateIndex);

                    var seed = seedSelector.Select(in key);
                    var aggregate = aggregator.Aggregate(in seed, in value);
                    aggregates.Add(new KeyValuePair<TKey, TAccumulate>(key, aggregate));
                }
                else
                {
                    ref var aggregatePair = ref aggregates.ElementAt(aggregateIndex);
                    var current = aggregatePair.Value;
                    var aggregate = aggregator.Aggregate(in current, in value);
                    aggregatePair = new KeyValuePair<TKey, TAccumulate>(aggregatePair.Key, aggregate);
                }
            }

            source.Dispose();
            return aggregates;
        }
    }
}
