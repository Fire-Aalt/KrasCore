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
            var keyToGroupIndex = new NativeParallelHashMap<TKey, int>(16, Allocator.Temp);
            var sourceValues = new NativeList<T>(Allocator.Temp);
            var sourceGroupIndexes = new NativeList<int>(Allocator.Temp);
            var groups = new NativeList<GroupRange<TKey>>(allocator);

            while (source.MoveNext())
            {
                var value = source.Current;
                var key = keySelector.Select(in value);

                if (!keyToGroupIndex.TryGetValue(key, out var groupIndex))
                {
                    if (keyToGroupIndex.Count() == keyToGroupIndex.Capacity)
                    {
                        keyToGroupIndex.Capacity *= 2;
                    }

                    groupIndex = groups.Length;
                    keyToGroupIndex.Add(key, groupIndex);
                    groups.Add(new GroupRange<TKey>
                    {
                        Key = key,
                        StartIndex = 0,
                        Length = 0,
                    });
                }

                var group = groups[groupIndex];
                group.Length++;
                groups[groupIndex] = group;

                sourceValues.Add(value);
                sourceGroupIndexes.Add(groupIndex);
            }

            source.Dispose();

            var values = new NativeList<T>(sourceValues.Length, allocator);
            values.Resize(sourceValues.Length, NativeArrayOptions.UninitializedMemory);

            var writeIndexes = new NativeList<int>(groups.Length, Allocator.Temp);
            writeIndexes.Resize(groups.Length, NativeArrayOptions.UninitializedMemory);

            var startIndex = 0;
            for (var i = 0; i < groups.Length; i++)
            {
                var group = groups[i];
                group.StartIndex = startIndex;
                groups[i] = group;
                writeIndexes[i] = startIndex;
                startIndex += group.Length;
            }

            for (var i = 0; i < sourceValues.Length; i++)
            {
                var groupIndex = sourceGroupIndexes[i];
                var valueIndex = writeIndexes[groupIndex];
                values[valueIndex] = sourceValues[i];
                writeIndexes[groupIndex] = valueIndex + 1;
            }

            return new GroupedQuery<TKey, T>(groups, values);
        }
    }
}
