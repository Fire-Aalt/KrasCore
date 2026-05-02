using NUnit.Framework;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

// ReSharper disable Unity.BurstFunctionSignatureContainsManagedTypes

namespace KrasCore.Tests
{
    public class NativeLinqBurstedDelegatesTests
    {
        [Test]
        public void DelegatePipeline_WhereSelectSum_UsesUnmanagedCapturedValues()
        {
            var input = new NativeArray<int>(new[] { 0, 1, 2, 3 }, Allocator.TempJob);
            var output = new NativeArray<float>(1, Allocator.TempJob);

            new BurstAggregateByJob
            {
                Input = input,
                Output = output,
            }.Schedule().Complete();
            
            // var result = input
            //     .AsQuery()
            //     .Where(value => value > min)
            //     .Select(value => (float)(value * factor))
            //     .FirstOrDefault(val => val > 5);    
            //.Sum(value => value + offset);

            //Assert.That(result, Is.EqualTo(19));
            try
            {
                Assert.That(output[0], Is.GreaterThan(5));
            }
            finally
            {
                input.Dispose();
                output.Dispose();
            }
        }

        [Test]
        public void DelegateAggregateBy_UsesMultipleNonAdjacentDelegates()
        {
            var input = new NativeArray<int>(new[] { 1, 2, 3, 4 }, Allocator.TempJob);
            var output = new NativeArray<int>(5, Allocator.TempJob);

            new BurstDelegateAggregateByJob
            {
                Input = input,
                Output = output,
            }.Schedule().Complete();

            try
            {
                Assert.That(output[0], Is.EqualTo(2));
                Assert.That(output[1], Is.EqualTo(1));
                Assert.That(output[2], Is.EqualTo(16));
                Assert.That(output[3], Is.EqualTo(0));
                Assert.That(output[4], Is.EqualTo(18));
            }
            finally
            {
                input.Dispose();
                output.Dispose();
            }
        }
        
        [BurstCompile(CompileSynchronously = true)]
        private struct BurstAggregateByJob : IJob
        {
            [ReadOnly]
            public NativeArray<int> Input;

            public NativeArray<float> Output;
            
            public void Execute()
            {
                var min = 1;
                var factor = 3;
                
                var result = Input
                    .AsQuery()
                    .Where(value => value > min)
                    .Select(value => (float)(value * factor))
                    .FirstOrDefault(Check);

                Output[0] = result;
            }
            
            private static bool Check(float val)
            {
                return val > 5;
            }
        }

        [BurstCompile(CompileSynchronously = true)]
        private struct BurstDelegateAggregateByJob : IJob
        {
            [ReadOnly]
            public NativeArray<int> Input;

            public NativeArray<int> Output;

            public void Execute()
            {
                var keyMask = 1;
                var addend = 1;

                var aggregates = Input
                    .AsQuery()
                    .AggregateBy(
                        value => (byte)(value & keyMask),
                        10,
                        (aggregate, value) => aggregate + value + addend)
                    .ToNativeList(Allocator.Temp);

                Output[0] = aggregates.Length;
                Output[1] = aggregates[0].Key;
                Output[2] = aggregates[0].Value;
                Output[3] = aggregates[1].Key;
                Output[4] = aggregates[1].Value;

                aggregates.Dispose();
            }
        }
    }
}
