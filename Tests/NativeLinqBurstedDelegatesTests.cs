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
    }
}
