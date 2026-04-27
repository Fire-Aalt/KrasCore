using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using NUnit.Framework;
using Unity.Burst;
using Unity.Collections;
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
        public void OrderBy_SortsAscending()
        {
            var input = new NativeArray<int>(new[] { 3, 1, 2 }, Allocator.Persistent);

            try
            {
                var ordered = input.AsQuery().OrderBy(Allocator.Temp);

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
                var ordered = input.AsQuery().OrderByDescending(Allocator.Temp);

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
    }

    internal struct CustomAccumulatedValue
    {
        public int Value;
    }

    internal struct CustomAccumulatedValueAccumulator : INativeAccumulator<CustomAccumulatedValue>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public CustomAccumulatedValue Add(in CustomAccumulatedValue total, in CustomAccumulatedValue value)
        {
            return new CustomAccumulatedValue { Value = total.Value + value.Value };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public CustomAccumulatedValue Divide(in CustomAccumulatedValue total, int count)
        {
            return new CustomAccumulatedValue { Value = total.Value / count };
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
