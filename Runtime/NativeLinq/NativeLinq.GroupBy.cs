using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Collections;

namespace KrasCore
{
    public static partial class NativeLinqExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GroupedQuery<T, T> GroupBy<T, TEnumerator, TKeySelector>(this Query<T, TEnumerator> source,
            TKeySelector keySelector,
            AllocatorManager.AllocatorHandle allocator)
            where T : unmanaged, IEquatable<T>
            where TEnumerator : unmanaged, IEnumerator<T>
            where TKeySelector : unmanaged, ISelector<T, T>
        {
            return NativeLinqUtilities.GroupBy<T, T, TEnumerator, TKeySelector>(
                source.GetEnumerator(),
                keySelector,
                allocator);
        }
    }

    public partial struct Query<T, TEnumerator>
        where T : unmanaged
        where TEnumerator : unmanaged, IEnumerator<T>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public GroupedQuery<TKey, T> GroupBy<TKey, TKeySelector>(
            TKeySelector keySelector,
            AllocatorManager.AllocatorHandle allocator)
            where TKey : unmanaged, IEquatable<TKey>
            where TKeySelector : unmanaged, ISelector<T, TKey>
        {
            return NativeLinqUtilities.GroupBy<T, TKey, TEnumerator, TKeySelector>(
                GetEnumerator(),
                keySelector,
                allocator);
        }
    }

    internal static partial class NativeLinqUtilities
    {
        private const int DEFAULT_GROUP_BY_CAPACITY = 64;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GroupedQuery<TKey, T> GroupBy<T, TKey, TEnumerator, TKeySelector>(
            TEnumerator source,
            TKeySelector keySelector,
            AllocatorManager.AllocatorHandle allocator)
            where T : unmanaged
            where TKey : unmanaged, IEquatable<TKey>
            where TEnumerator : unmanaged, IEnumerator<T>
            where TKeySelector : unmanaged, ISelector<T, TKey>
        {
            var keyToGroupIndex = new NativeHashMap<TKey, int>(DEFAULT_GROUP_BY_CAPACITY, Allocator.Temp);
            var groups = new NativeList<GroupRange<TKey>>(DEFAULT_GROUP_BY_CAPACITY, allocator);
            var values = new NativeList<T>(DEFAULT_GROUP_BY_CAPACITY, allocator);
            var nextIndexes = new NativeList<int>(DEFAULT_GROUP_BY_CAPACITY, allocator);

            while (source.MoveNext())
            {
                var value = source.Current;
                var key = keySelector.Select(in value);
                var valueIndex = values.Length;

                var defaultRange = default(GroupRange<TKey>);
                ref var group = ref defaultRange;
                if (!keyToGroupIndex.TryGetValue(key, out var groupIndex))
                {
                    if (groups.Length == keyToGroupIndex.Capacity)
                    {
                        keyToGroupIndex.Capacity *= 2;
                    }

                    groupIndex = groups.Length;
                    keyToGroupIndex.Add(key, groupIndex);
                    groups.Add(new GroupRange<TKey>
                    {
                        Key = key,
                        HeadIndex = valueIndex,
                        TailIndex = valueIndex,
                        Length = 0,
                    });

                    group = ref groups.ElementAt(groupIndex);
                }
                else
                {
                    group = ref groups.ElementAt(groupIndex);
                    nextIndexes[group.TailIndex] = valueIndex;
                    group.TailIndex = valueIndex;
                }

                group.Length++;
                values.Add(value);
                nextIndexes.Add(-1);
            }

            source.Dispose();

            return new GroupedQuery<TKey, T>(groups, values, nextIndexes);
        }
    }
}
