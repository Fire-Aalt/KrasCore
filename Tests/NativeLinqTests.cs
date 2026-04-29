using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using NUnit.Framework;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;

namespace KrasCore.Tests
{
    public class NativeLinqTests
    {
        [Test]
        public void Where_ToNativeList_FiltersValues()
        {
            var input = new NativeArray<int>(new[] { 0, 1, 2, 3 }, Allocator.Persistent);

            try
            {
                var filtered = input
                    .AsQuery()
                    .Where(new GreaterThan { Threshold = 1 })
                    .ToNativeList(Allocator.Temp);

                try
                {
                    Assert.That(filtered.Length, Is.EqualTo(2));
                    Assert.That(filtered[0], Is.EqualTo(2));
                    Assert.That(filtered[1], Is.EqualTo(3));
                }
                finally
                {
                    filtered.Dispose();
                }
            }
            finally
            {
                input.Dispose();
            }
        }

        [Test]
        public void Where_Materialize_ReturnsRequestedCollectionTypes()
        {
            var input = new NativeArray<int>(new[] { 0, 1, 2, 3 }, Allocator.Persistent);
            var array = default(NativeArray<int>);
            var unsafeArray = default(UnsafeArray<int>);
            var unsafeList = default(UnsafeList<int>);

            try
            {
                array = input
                    .AsQuery()
                    .Where(new GreaterThan { Threshold = 1 })
                    .ToNativeArray(Allocator.Temp);
                unsafeArray = input
                    .AsQuery()
                    .Where(new GreaterThan { Threshold = 1 })
                    .ToUnsafeArray(Allocator.Temp);
                unsafeList = input
                    .AsQuery()
                    .Where(new GreaterThan { Threshold = 1 })
                    .ToUnsafeList(Allocator.Temp);

                var managedArray = input
                    .AsQuery()
                    .Where(new GreaterThan { Threshold = 1 })
                    .ToManagedArray();
                var managedList = input
                    .AsQuery()
                    .Where(new GreaterThan { Threshold = 1 })
                    .ToManagedList();

                Assert.That(array.Length, Is.EqualTo(2));
                Assert.That(array[0], Is.EqualTo(2));
                Assert.That(array[1], Is.EqualTo(3));

                Assert.That(unsafeArray.Length, Is.EqualTo(2));
                Assert.That(unsafeArray[0], Is.EqualTo(2));
                Assert.That(unsafeArray[1], Is.EqualTo(3));

                Assert.That(unsafeList.Length, Is.EqualTo(2));
                Assert.That(unsafeList[0], Is.EqualTo(2));
                Assert.That(unsafeList[1], Is.EqualTo(3));

                Assert.That(managedArray.Length, Is.EqualTo(2));
                Assert.That(managedArray[0], Is.EqualTo(2));
                Assert.That(managedArray[1], Is.EqualTo(3));

                Assert.That(managedList.Count, Is.EqualTo(2));
                Assert.That(managedList[0], Is.EqualTo(2));
                Assert.That(managedList[1], Is.EqualTo(3));
            }
            finally
            {
                if (array.IsCreated)
                {
                    array.Dispose();
                }

                if (unsafeArray.IsCreated)
                {
                    unsafeArray.Dispose();
                }

                if (unsafeList.IsCreated)
                {
                    unsafeList.Dispose();
                }

                input.Dispose();
            }
        }

        [Test]
        public void Select_ProjectsValues()
        {
            var input = new NativeArray<int>(new[] { 1, 2, 3 }, Allocator.Persistent);

            try
            {
                var mapped = input
                    .AsQuery()
                    .Select(new MultiplyByTwo())
                    .ToNativeList(Allocator.Temp);

                try
                {
                    Assert.That(mapped.Length, Is.EqualTo(3));
                    Assert.That(mapped[0], Is.EqualTo(2));
                    Assert.That(mapped[1], Is.EqualTo(4));
                    Assert.That(mapped[2], Is.EqualTo(6));
                }
                finally
                {
                    mapped.Dispose();
                }
            }
            finally
            {
                input.Dispose();
            }
        }

        [Test]
        public void SelectMany_FlattensEnumerators()
        {
            var input = new NativeArray<int>(new[] { 1, 3 }, Allocator.Persistent);

            try
            {
                var flattened = input
                    .AsQuery()
                    .SelectMany<int, FixedList32Bytes<int>.Enumerator, DuplicateWithOffset>(new DuplicateWithOffset())
                    .ToNativeList(Allocator.Temp);

                try
                {
                    Assert.That(flattened.Length, Is.EqualTo(4));
                    Assert.That(flattened[0], Is.EqualTo(1));
                    Assert.That(flattened[1], Is.EqualTo(11));
                    Assert.That(flattened[2], Is.EqualTo(3));
                    Assert.That(flattened[3], Is.EqualTo(13));
                }
                finally
                {
                    flattened.Dispose();
                }
            }
            finally
            {
                input.Dispose();
            }
        }

        [Test]
        public void GroupBy_GroupsValuesPreservingGroupAndElementOrder()
        {
            var input = new NativeArray<GroupRecord>(
                new[]
                {
                    new GroupRecord { Group = 2, Value = 10 },
                    new GroupRecord { Group = 1, Value = 20 },
                    new GroupRecord { Group = 2, Value = 30 },
                    new GroupRecord { Group = 1, Value = 40 },
                    new GroupRecord { Group = 3, Value = 50 },
                },
                Allocator.Persistent);

            try
            {
                var grouped = input
                    .AsQuery()
                    .ToLookup<int, GroupRecordKeySelector>(new GroupRecordKeySelector(), Allocator.Temp);

                try
                {
                    Assert.That(grouped.GroupCount, Is.EqualTo(3));
                    Assert.That(grouped.ValueCount, Is.EqualTo(5));

                    Assert.That(grouped[0].Key, Is.EqualTo(2));
                    Assert.That(grouped[0].Length, Is.EqualTo(2));
                    Assert.That(grouped[0][0].Value, Is.EqualTo(10));
                    Assert.That(grouped[0][1].Value, Is.EqualTo(30));

                    Assert.That(grouped[1].Key, Is.EqualTo(1));
                    Assert.That(grouped[1].Length, Is.EqualTo(2));
                    Assert.That(grouped[1][0].Value, Is.EqualTo(20));
                    Assert.That(grouped[1][1].Value, Is.EqualTo(40));

                    Assert.That(grouped[2].Key, Is.EqualTo(3));
                    Assert.That(grouped[2].Length, Is.EqualTo(1));
                    Assert.That(grouped[2][0].Value, Is.EqualTo(50));
                }
                finally
                {
                    grouped.Dispose();
                }
            }
            finally
            {
                input.Dispose();
            }
        }

        [Test]
        public void GroupBy_GroupsCanBeEnumeratedAndMaterialized()
        {
            var input = new NativeArray<int>(new[] { 1, 2, 3, 4, 5 }, Allocator.Persistent);

            try
            {
                var grouped = input
                    .AsQuery()
                    .ToLookup(new ModuloTwoSelector(), Allocator.Temp);

                try
                {
                    var oddSum = 0;
                    var even = default(NativeList<int>);

                    foreach (var group in grouped)
                    {
                        if (group.Key == 1)
                        {
                            foreach (var value in group)
                            {
                                oddSum += value;
                            }
                        }
                        else
                        {
                            even = group.AsQuery().ToNativeList(Allocator.Temp);
                        }
                    }

                    try
                    {
                        Assert.That(oddSum, Is.EqualTo(9));
                        Assert.That(even.Length, Is.EqualTo(2));
                        Assert.That(even[0], Is.EqualTo(2));
                        Assert.That(even[1], Is.EqualTo(4));
                        Assert.That(grouped.AsQuery().Select<int, GroupLengthSelector>(new GroupLengthSelector()).Sum(), Is.EqualTo(5));
                    }
                    finally
                    {
                        if (even.IsCreated)
                        {
                            even.Dispose();
                        }
                    }
                }
                finally
                {
                    grouped.Dispose();
                }
            }
            finally
            {
                input.Dispose();
            }
        }

        [Test]
        public void AggregateBy_AggregatesValuesPreservingKeyOrder()
        {
            var input = new NativeArray<GroupRecord>(
                new[]
                {
                    new GroupRecord { Group = 2, Value = 10 },
                    new GroupRecord { Group = 1, Value = 20 },
                    new GroupRecord { Group = 2, Value = 30 },
                    new GroupRecord { Group = 1, Value = 40 },
                    new GroupRecord { Group = 3, Value = 50 },
                },
                Allocator.Persistent);

            try
            {
                var aggregates = input
                    .AsQuery()
                    .AggregateBy(
                        new GroupRecordKeySelector(),
                        0,
                        new GroupRecordValueSumAggregator())
                    .ToNativeList(Allocator.TempJob);

                try
                {
                    Assert.That(aggregates.Length, Is.EqualTo(3));

                    Assert.That(aggregates[0].Key, Is.EqualTo(2));
                    Assert.That(aggregates[0].Value, Is.EqualTo(40));

                    Assert.That(aggregates[1].Key, Is.EqualTo(1));
                    Assert.That(aggregates[1].Value, Is.EqualTo(60));

                    Assert.That(aggregates[2].Key, Is.EqualTo(3));
                    Assert.That(aggregates[2].Value, Is.EqualTo(50));
                }
                finally
                {
                    aggregates.Dispose();
                }
            }
            finally
            {
                input.Dispose();
            }
        }

        [Test]
        public void AggregateBy_WithSeedSelector_InitializesPerKey()
        {
            var input = new NativeArray<GroupRecord>(
                new[]
                {
                    new GroupRecord { Group = 2, Value = 1 },
                    new GroupRecord { Group = 1, Value = 4 },
                    new GroupRecord { Group = 2, Value = 3 },
                },
                Allocator.Persistent);

            try
            {
                var aggregates = input
                    .AsQuery()
                    .ToAggregatedBy<int, int, GroupRecordKeySelector, KeyTimesTenSeedSelector, GroupRecordValueSumAggregator>(
                        new GroupRecordKeySelector(),
                        new KeyTimesTenSeedSelector(),
                        new GroupRecordValueSumAggregator(),
                        Allocator.TempJob);

                try
                {
                    Assert.That(aggregates.Length, Is.EqualTo(2));

                    Assert.That(aggregates[0].Key, Is.EqualTo(2));
                    Assert.That(aggregates[0].Value, Is.EqualTo(24));

                    Assert.That(aggregates[1].Key, Is.EqualTo(1));
                    Assert.That(aggregates[1].Value, Is.EqualTo(14));
                }
                finally
                {
                    aggregates.Dispose();
                }
            }
            finally
            {
                input.Dispose();
            }
        }

        [Test]
        public void OrderBy_SortsAscending()
        {
            var input = new NativeArray<int>(new[] { 3, 1, 2 }, Allocator.Persistent);

            try
            {
                var ordered = input.AsQuery().ToOrderedBy(Allocator.Temp);
                
                try
                {
                    Assert.That(ordered.Length, Is.EqualTo(3));
                    Assert.That(ordered[0], Is.EqualTo(1));
                    Assert.That(ordered[1], Is.EqualTo(2));
                    Assert.That(ordered[2], Is.EqualTo(3));
                }
                finally
                {
                    ordered.Dispose();
                }
            }
            finally
            {
                input.Dispose();
            }
        }

        [Test]
        public void OrderByDescending_SortsDescending()
        {
            var input = new NativeArray<int>(new[] { 3, 1, 2 }, Allocator.Persistent);

            try
            {
                var ordered = input.AsQuery().ToOrderedByDescending(Allocator.Temp);

                try
                {
                    Assert.That(ordered.Length, Is.EqualTo(3));
                    Assert.That(ordered[0], Is.EqualTo(3));
                    Assert.That(ordered[1], Is.EqualTo(2));
                    Assert.That(ordered[2], Is.EqualTo(1));
                }
                finally
                {
                    ordered.Dispose();
                }
            }
            finally
            {
                input.Dispose();
            }
        }

        [Test]
        public void ThenBy_ComposesPrimaryAndSecondaryComparers()
        {
            var input = new NativeArray<SortRecord>(
                new[]
                {
                    new SortRecord { Primary = 1, Secondary = 2 },
                    new SortRecord { Primary = 0, Secondary = 5 },
                    new SortRecord { Primary = 1, Secondary = 1 },
                    new SortRecord { Primary = 0, Secondary = 3 },
                },
                Allocator.Persistent);

            try
            {
                var ordered = input
                    .AsQuery()
                    .OrderBy(new PrimaryComparer())
                    .ThenBy(new SecondaryComparer())
                    .ToNativeList(Allocator.Temp);

                try
                {
                    Assert.That(ordered.Length, Is.EqualTo(4));
                    Assert.That(ordered[0], Is.EqualTo(new SortRecord { Primary = 0, Secondary = 3 }));
                    Assert.That(ordered[1], Is.EqualTo(new SortRecord { Primary = 0, Secondary = 5 }));
                    Assert.That(ordered[2], Is.EqualTo(new SortRecord { Primary = 1, Secondary = 1 }));
                    Assert.That(ordered[3], Is.EqualTo(new SortRecord { Primary = 1, Secondary = 2 }));
                }
                finally
                {
                    ordered.Dispose();
                }
            }
            finally
            {
                input.Dispose();
            }
        }

        [Test]
        public void ThenByDescending_ComposesPrimaryAndDescendingSecondaryComparers()
        {
            var input = new NativeArray<SortRecord>(
                new[]
                {
                    new SortRecord { Primary = 1, Secondary = 2 },
                    new SortRecord { Primary = 0, Secondary = 5 },
                    new SortRecord { Primary = 1, Secondary = 1 },
                    new SortRecord { Primary = 0, Secondary = 3 },
                },
                Allocator.Persistent);

            try
            {
                var ordered = input
                    .AsQuery()
                    .OrderBy(new PrimaryComparer())
                    .ThenByDescending(new SecondaryComparer())
                    .ToNativeList(Allocator.Temp);

                try
                {
                    Assert.That(ordered.Length, Is.EqualTo(4));
                    Assert.That(ordered[0], Is.EqualTo(new SortRecord { Primary = 0, Secondary = 5 }));
                    Assert.That(ordered[1], Is.EqualTo(new SortRecord { Primary = 0, Secondary = 3 }));
                    Assert.That(ordered[2], Is.EqualTo(new SortRecord { Primary = 1, Secondary = 2 }));
                    Assert.That(ordered[3], Is.EqualTo(new SortRecord { Primary = 1, Secondary = 1 }));
                }
                finally
                {
                    ordered.Dispose();
                }
            }
            finally
            {
                input.Dispose();
            }
        }

        [Test]
        public void First_ReturnsFirstElement()
        {
            var input = new NativeArray<int>(new[] { 9, 10 }, Allocator.Persistent);

            try
            {
                Assert.That(input.AsQuery().First(), Is.EqualTo(9));
            }
            finally
            {
                input.Dispose();
            }
        }

        [Test]
        public void FirstOrDefault_ReturnsDefaultForEmptySource()
        {
            var input = new NativeArray<int>(0, Allocator.Persistent);

            try
            {
                Assert.That(input.AsQuery().FirstOrDefault(), Is.EqualTo(0));
            }
            finally
            {
                input.Dispose();
            }
        }

        [Test]
        public void FirstOrDefault_WithPredicate_ReturnsFirstMatch()
        {
            var input = new NativeArray<int>(new[] { 1, 2, 3 }, Allocator.Persistent);

            try
            {
                Assert.That(input.AsQuery().FirstOrDefault(new GreaterThan { Threshold = 1 }), Is.EqualTo(2));
                Assert.That(input.AsQuery().FirstOrDefault(new GreaterThan { Threshold = 10 }), Is.EqualTo(0));
            }
            finally
            {
                input.Dispose();
            }
        }

        [Test]
        public void Sum_UsesBuiltInAccumulator()
        {
            var input = new NativeArray<int>(new[] { 1, 2, 3 }, Allocator.Persistent);

            try
            {
                Assert.That(input.AsQuery().Sum(), Is.EqualTo(6));
            }
            finally
            {
                input.Dispose();
            }
        }

        [Test]
        public void Sum_WithSelector_UsesBuiltInAccumulator()
        {
            var input = new NativeArray<GroupRecord>(
                new[]
                {
                    new GroupRecord { Group = 1, Value = 10 },
                    new GroupRecord { Group = 2, Value = 20 },
                    new GroupRecord { Group = 3, Value = 30 },
                },
                Allocator.Persistent);

            try
            {
                Assert.That(input.AsQuery().Sum(new GroupRecordKeySelector()), Is.EqualTo(6));
            }
            finally
            {
                input.Dispose();
            }
        }

        [Test]
        public void Average_UsesBuiltInAccumulator()
        {
            var input = new NativeArray<float>(new[] { 1f, 2f, 3f }, Allocator.Persistent);

            try
            {
                Assert.That(input.AsQuery().Average(), Is.EqualTo(2f));
            }
            finally
            {
                input.Dispose();
            }
        }

        [Test]
        public void Average_WithSelector_UsesBuiltInAccumulator()
        {
            var input = new NativeArray<GroupRecord>(
                new[]
                {
                    new GroupRecord { Group = 2, Value = 10 },
                    new GroupRecord { Group = 4, Value = 20 },
                    new GroupRecord { Group = 6, Value = 30 },
                },
                Allocator.Persistent);

            try
            {
                Assert.That(input.AsQuery().Average(new GroupRecordKeySelector()), Is.EqualTo(4));
            }
            finally
            {
                input.Dispose();
            }
        }

        [Test]
        public void SumAndAverage_UseBuiltInVectorAccumulator()
        {
            var input = new NativeArray<float3>(
                new[] { new float3(1f, 2f, 3f), new float3(3f, 4f, 5f) },
                Allocator.Persistent);

            try
            {
                Assert.That(input.AsQuery().Sum(), Is.EqualTo(new float3(4f, 6f, 8f)));
                Assert.That(input.AsQuery().Average(), Is.EqualTo(new float3(2f, 3f, 4f)));
            }
            finally
            {
                input.Dispose();
            }
        }

        [Test]
        public void SumAndAverage_CanBeExtendedForCustomStructs()
        {
            var input = new NativeArray<CustomAccumulatedValue>(
                new[]
                {
                    new CustomAccumulatedValue { Value = 1 },
                    new CustomAccumulatedValue { Value = 2 },
                    new CustomAccumulatedValue { Value = 3 },
                },
                Allocator.Persistent);

            try
            {
                Assert.That(input.AsQuery().Sum().Value, Is.EqualTo(6));
                Assert.That(input.AsQuery().Average().Value, Is.EqualTo(2));
            }
            finally
            {
                input.Dispose();
            }
        }

        [Test]
        public void MinAndMax_ReturnExtremes()
        {
            var input = new NativeArray<int>(new[] { 3, 1, 2 }, Allocator.Persistent);

            try
            {
                Assert.That(input.AsQuery().Min(), Is.EqualTo(1));
                Assert.That(input.AsQuery().Max(), Is.EqualTo(3));
            }
            finally
            {
                input.Dispose();
            }
        }

        [Test]
        public void Contains_UsesNativeEquality()
        {
            var input = new NativeArray<int>(new[] { 1, 2, 3 }, Allocator.Persistent);

            try
            {
                Assert.That(input.AsQuery().Contains(2), Is.True);
                Assert.That(input.AsQuery().Contains(4), Is.False);
            }
            finally
            {
                input.Dispose();
            }
        }

        [Test]
        public void SequenceEquals_ComparesValuesInOrder()
        {
            var left = new NativeArray<int>(new[] { 1, 2, 3 }, Allocator.Persistent);
            var right = new NativeArray<int>(new[] { 1, 2, 3 }, Allocator.Persistent);
            var different = new NativeArray<int>(new[] { 1, 3, 2 }, Allocator.Persistent);

            try
            {
                Assert.That(left.AsQuery().SequenceEquals(right.AsQuery()), Is.True);
                Assert.That(left.AsQuery().SequenceEquals(different.AsQuery()), Is.False);
            }
            finally
            {
                left.Dispose();
                right.Dispose();
                different.Dispose();
            }
        }

        [Test]
        public void From_WrapsCustomUnmanagedEnumerableEnumerator()
        {
            var collection = new CustomEnumerable(5, 3);
            var values = NativeLinq
                .From<int, CustomEnumerable.Enumerator>(collection.GetEnumerator())
                .ToNativeList(Allocator.Temp);

            try
            {
                Assert.That(values.Length, Is.EqualTo(3));
                Assert.That(values[0], Is.EqualTo(5));
                Assert.That(values[1], Is.EqualTo(6));
                Assert.That(values[2], Is.EqualTo(7));
            }
            finally
            {
                values.Dispose();
            }
        }

        [Test]
        public void Foreach_EnumeratesLazyQuery()
        {
            var input = new NativeArray<int>(new[] { 0, 1, 2, 3 }, Allocator.Persistent);

            try
            {
                var sum = 0;
                foreach (var value in input.AsQuery().Where(new GreaterThan { Threshold = 1 }))
                {
                    sum += value;
                }

                Assert.That(sum, Is.EqualTo(5));
            }
            finally
            {
                input.Dispose();
            }
        }

        [Test]
        public void BurstJob_CanExecuteNativeLinqGroupBy()
        {
            var input = new NativeArray<int>(new[] { 1, 2, 3, 4, 5 }, Allocator.Persistent);
            var output = new NativeArray<int>(5, Allocator.Persistent);

            try
            {
                new BurstGroupByJob
                {
                    Input = input,
                    Output = output,
                }.Schedule().Complete();

                Assert.That(output[0], Is.EqualTo(2));
                Assert.That(output[1], Is.EqualTo(5));
                Assert.That(output[2], Is.EqualTo(1));
                Assert.That(output[3], Is.EqualTo(3));
                Assert.That(output[4], Is.EqualTo(9));
            }
            finally
            {
                input.Dispose();
                output.Dispose();
            }
        }

        [Test]
        public void BurstJob_CanExecuteNativeLinqAggregateBy()
        {
            var input = new NativeArray<GroupRecord>(
                new[]
                {
                    new GroupRecord { Group = 2, Value = 10 },
                    new GroupRecord { Group = 1, Value = 20 },
                    new GroupRecord { Group = 2, Value = 30 },
                },
                Allocator.Persistent);
            var output = new NativeArray<int>(5, Allocator.Persistent);

            try
            {
                new BurstAggregateByJob
                {
                    Input = input,
                    Output = output,
                }.Schedule().Complete();

                Assert.That(output[0], Is.EqualTo(2));
                Assert.That(output[1], Is.EqualTo(2));
                Assert.That(output[2], Is.EqualTo(40));
                Assert.That(output[3], Is.EqualTo(1));
                Assert.That(output[4], Is.EqualTo(20));
            }
            finally
            {
                input.Dispose();
                output.Dispose();
            }
        }

        [Test]
        public void BurstJob_CanExecuteNativeLinqQuery()
        {
            var input = new NativeArray<int>(new[] { 0, 1, 2, 3 }, Allocator.Persistent);
            var output = new NativeArray<int>(9, Allocator.Persistent);

            try
            {
                new BurstQueryJob
                {
                    Input = input,
                    Output = output,
                }.Schedule().Complete();

                Assert.That(output[0], Is.EqualTo(10));
                Assert.That(output[1], Is.EqualTo(0));
                Assert.That(output[2], Is.EqualTo(0));
                Assert.That(output[3], Is.EqualTo(2));
                Assert.That(output[4], Is.EqualTo(6));
                Assert.That(output[5], Is.EqualTo(1));
                Assert.That(output[6], Is.EqualTo(1));
                Assert.That(output[7], Is.EqualTo(3));
                Assert.That(output[8], Is.EqualTo(1));
            }
            finally
            {
                input.Dispose();
                output.Dispose();
            }
        }

        private struct GreaterThan : IPredicate<int>
        {
            public int Threshold;

            public bool Match(in int value)
            {
                return value > Threshold;
            }
        }

        private struct MultiplyByTwo : ISelector<int, int>
        {
            public int Select(in int value)
            {
                return value * 2;
            }
        }

        private struct DuplicateWithOffset : ISelector<int, FixedList32Bytes<int>.Enumerator>
        {
            public FixedList32Bytes<int>.Enumerator Select(in int value)
            {
                var list = new FixedList32Bytes<int>();
                list.Add(value);
                list.Add(value + 10);
                return list.GetEnumerator();
            }
        }

        private struct ModuloTwoSelector : ISelector<int, int>
        {
            public int Select(in int value)
            {
                return value % 2;
            }
        }

        private struct GroupRecord
        {
            public int Group;
            public int Value;
        }

        private struct GroupRecordKeySelector : ISelector<GroupRecord, int>
        {
            public int Select(in GroupRecord value)
            {
                return value.Group;
            }
        }

        private struct KeyTimesTenSeedSelector : ISelector<int, int>
        {
            public int Select(in int value)
            {
                return value * 10;
            }
        }

        private struct GroupRecordValueSumAggregator : IAggregator<int, GroupRecord>
        {
            public int Aggregate(in int aggregate, in GroupRecord value)
            {
                return aggregate + value.Value;
            }
        }

        private struct GroupLengthSelector : ISelector<Group<int, int>, int>
        {
            public int Select(in Group<int, int> value)
            {
                return value.Length;
            }
        }

        private struct SortRecord
        {
            public int Primary;
            public int Secondary;
        }

        private struct PrimaryComparer : IComparer<SortRecord>
        {
            public int Compare(SortRecord x, SortRecord y)
            {
                return x.Primary.CompareTo(y.Primary);
            }
        }

        private struct SecondaryComparer : IComparer<SortRecord>
        {
            public int Compare(SortRecord x, SortRecord y)
            {
                return x.Secondary.CompareTo(y.Secondary);
            }
        }

        private struct CustomEnumerable : IEnumerable<int>
        {
            private int _start;
            private int _count;

            public CustomEnumerable(int start, int count)
            {
                _start = start;
                _count = count;
            }

            public Enumerator GetEnumerator()
            {
                return new Enumerator(_start, _count);
            }

            IEnumerator<int> IEnumerable<int>.GetEnumerator()
            {
                return GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public struct Enumerator : IEnumerator<int>
            {
                private int _start;
                private int _count;
                private int _index;

                public Enumerator(int start, int count)
                {
                    _start = start;
                    _count = count;
                    _index = -1;
                }

                public int Current => _start + _index;

                object IEnumerator.Current => Current;

                public bool MoveNext()
                {
                    _index++;
                    return _index < _count;
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

        [BurstCompile(CompileSynchronously = true)]
        private struct BurstQueryJob : IJob
        {
            [ReadOnly]
            public NativeArray<int> Input;

            public NativeArray<int> Output;

            public void Execute()
            {
                var sum = 0;
                foreach (var value in Input
                    .AsQuery()
                    .Where(new GreaterThan { Threshold = 1 })
                    .Select(new MultiplyByTwo()))
                {
                    sum += value;
                }

                Output[0] = sum;
                Output[1] = Input.AsQuery().First();
                Output[2] = Input.AsQuery().Where(new GreaterThan { Threshold = 100 }).FirstOrDefault();
                Output[3] = Input.AsQuery().FirstOrDefault(new GreaterThan { Threshold = 1 });
                Output[4] = Input.AsQuery().Sum();
                Output[5] = Input.AsQuery().Average();
                Output[6] = Input.AsQuery().Contains(2) ? 1 : 0;
                Output[7] = Input.AsQuery().Min() + Input.AsQuery().Max();
                Output[8] = Input.AsQuery().SequenceEquals(Input.AsQuery()) ? 1 : 0;
            }
        }

        [BurstCompile(CompileSynchronously = true)]
        private struct BurstAggregateByJob : IJob
        {
            [ReadOnly]
            public NativeArray<GroupRecord> Input;

            public NativeArray<int> Output;

            public void Execute()
            {
                var aggregates = Input
                    .AsQuery()
                    .ToAggregatedBy(
                        new GroupRecordKeySelector(),
                        0,
                        new GroupRecordValueSumAggregator(),
                        Allocator.Temp);

                Output[0] = aggregates.Length;
                Output[1] = aggregates[0].Key;
                Output[2] = aggregates[0].Value;
                Output[3] = aggregates[1].Key;
                Output[4] = aggregates[1].Value;
            }
        }

        [BurstCompile(CompileSynchronously = true)]
        private struct BurstGroupByJob : IJob
        {
            [ReadOnly]
            public NativeArray<int> Input;

            public NativeArray<int> Output;

            public void Execute()
            {
                var grouped = Input
                    .AsQuery()
                    .ToLookup<int, ModuloTwoSelector>(new ModuloTwoSelector(), Allocator.Temp);

                Output[0] = grouped.GroupCount;
                Output[1] = grouped.ValueCount;
                Output[2] = grouped[0].Key;

                var oddSum = 0;
                foreach (var value in grouped[0].AsQuery())
                {
                    oddSum += value;
                }

                Output[3] = grouped[0].Length;
                Output[4] = oddSum;

                grouped.Dispose();
            }
        }
    }

    internal struct CustomAccumulatedValue
    {
        public int Value;
    }

    internal struct CustomAccumulatedValueAccumulator : IAccumulator<CustomAccumulatedValue>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public CustomAccumulatedValue Add(in CustomAccumulatedValue total, in CustomAccumulatedValue value)
        {
            return new CustomAccumulatedValue { Value = total.Value + value.Value };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public CustomAccumulatedValue Divide(in CustomAccumulatedValue total, uint count)
        {
            return new CustomAccumulatedValue { Value = (int)(total.Value / count) };
        }
    }

    internal static class CustomAccumulatedValueNativeLinqExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static CustomAccumulatedValue Sum<TEnumerator>(this Query<CustomAccumulatedValue, TEnumerator> source)
            where TEnumerator : unmanaged, IEnumerator<CustomAccumulatedValue>
        {
            return source.Sum<CustomAccumulatedValue, TEnumerator, CustomAccumulatedValueAccumulator>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static CustomAccumulatedValue Average<TEnumerator>(this Query<CustomAccumulatedValue, TEnumerator> source)
            where TEnumerator : unmanaged, IEnumerator<CustomAccumulatedValue>
        {
            return source.Average<CustomAccumulatedValue, TEnumerator, CustomAccumulatedValueAccumulator>();
        }
    }
}
