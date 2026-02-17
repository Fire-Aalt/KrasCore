using System;
using NUnit.Framework;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.PerformanceTesting;

namespace KrasCore.Tests.Editor
{
    public class ParallelListBenchmarkTests
    {
        private const int WorkerCap = 32;
        private const int WarmupRuns = 2;
        private const int MeasureRuns = 10;

        [Test]
        [Performance]
        [Explicit("Benchmark test. Run manually.")]
        [Category("Benchmark")]
        [TestCase(10_000)]
        [TestCase(100_000)]
        [TestCase(1_000_000)]
        public void ParallelWriters_CompareParallelListNativeListNativeQueue(int totalWrites)
        {
            var workerCount = Math.Min(JobsUtility.ThreadIndexCount, WorkerCap);
            var expectedSum = ((long)totalWrites * (totalWrites - 1)) / 2L;

            MeasureParallelList(totalWrites, workerCount, expectedSum);
            MeasureNativeList(totalWrites, workerCount, expectedSum);
            MeasureNativeQueue(totalWrites, workerCount, expectedSum);
        }

        private static void MeasureParallelList(int totalWrites, int workerCount, long expectedSum)
        {
            var perWorkerCapacity = (totalWrites + workerCount - 1) / workerCount;
            var sampleGroup = new SampleGroup($"ParallelList.ThreadWriter/{totalWrites}", SampleUnit.Millisecond);

            var list = default(ParallelList<int>);
            var partialSums = default(NativeArray<long>);

            Measure.Method(() =>
                {
                    new ParallelListWriterJob
                    {
                        Writer = list.AsThreadWriter(),
                        PartialSums = partialSums,
                        TotalWrites = totalWrites,
                        WorkerCount = workerCount
                    }.ScheduleParallel(workerCount, 1, default).Complete();
                })
                .SetUp(() =>
                {
                    list = new ParallelList<int>(perWorkerCapacity, Allocator.TempJob);
                    partialSums = new NativeArray<long>(workerCount, Allocator.TempJob, NativeArrayOptions.ClearMemory);
                })
                .CleanUp(() =>
                {
                    Assert.That(list.Length, Is.EqualTo(totalWrites));
                    Assert.That(Sum(partialSums), Is.EqualTo(expectedSum));

                    partialSums.Dispose();
                    list.Dispose();
                })
                .WarmupCount(WarmupRuns)
                .MeasurementCount(MeasureRuns)
                .SampleGroup(sampleGroup)
                .Run();
        }

        private static void MeasureNativeList(int totalWrites, int workerCount, long expectedSum)
        {
            var sampleGroup = new SampleGroup($"NativeList.ParallelWriter/{totalWrites}", SampleUnit.Millisecond);

            var list = default(NativeList<int>);
            var partialSums = default(NativeArray<long>);

            Measure.Method(() =>
                {
                    new NativeListWriterJob
                    {
                        Writer = list.AsParallelWriter(),
                        PartialSums = partialSums,
                        TotalWrites = totalWrites,
                        WorkerCount = workerCount
                    }.ScheduleParallel(workerCount, 1, default).Complete();
                })
                .SetUp(() =>
                {
                    list = new NativeList<int>(totalWrites, Allocator.TempJob);
                    partialSums = new NativeArray<long>(workerCount, Allocator.TempJob, NativeArrayOptions.ClearMemory);
                })
                .CleanUp(() =>
                {
                    Assert.That(list.Length, Is.EqualTo(totalWrites));
                    Assert.That(Sum(partialSums), Is.EqualTo(expectedSum));

                    partialSums.Dispose();
                    list.Dispose();
                })
                .WarmupCount(WarmupRuns)
                .MeasurementCount(MeasureRuns)
                .SampleGroup(sampleGroup)
                .Run();
        }

        private static void MeasureNativeQueue(int totalWrites, int workerCount, long expectedSum)
        {
            var sampleGroup = new SampleGroup($"NativeQueue.ParallelWriter/{totalWrites}", SampleUnit.Millisecond);

            var queue = default(NativeQueue<int>);
            var partialSums = default(NativeArray<long>);

            Measure.Method(() =>
                {
                    new NativeQueueWriterJob
                    {
                        Writer = queue.AsParallelWriter(),
                        PartialSums = partialSums,
                        TotalWrites = totalWrites,
                        WorkerCount = workerCount
                    }.ScheduleParallel(workerCount, 1, default).Complete();
                })
                .SetUp(() =>
                {
                    queue = new NativeQueue<int>(Allocator.TempJob);
                    partialSums = new NativeArray<long>(workerCount, Allocator.TempJob, NativeArrayOptions.ClearMemory);
                })
                .CleanUp(() =>
                {
                    Assert.That(queue.Count, Is.EqualTo(totalWrites));
                    Assert.That(Sum(partialSums), Is.EqualTo(expectedSum));

                    queue.Dispose();
                    partialSums.Dispose();
                })
                .WarmupCount(WarmupRuns)
                .MeasurementCount(MeasureRuns)
                .SampleGroup(sampleGroup)
                .Run();
        }

        private static long Sum(NativeArray<long> values)
        {
            var result = 0L;
            for (var i = 0; i < values.Length; i++)
            {
                result += values[i];
            }

            return result;
        }

        [BurstCompile(CompileSynchronously = true)]
        private struct ParallelListWriterJob : IJobFor
        {
            public ParallelList<int>.ThreadWriter Writer;
            public NativeArray<long> PartialSums;
            public int TotalWrites;
            public int WorkerCount;

            public void Execute(int workerIndex)
            {
                var start = workerIndex * TotalWrites / WorkerCount;
                var end = (workerIndex + 1) * TotalWrites / WorkerCount;

                var sum = 0L;
                for (var value = start; value < end; value++)
                {
                    Writer.Add(in value, workerIndex);
                    sum += value;
                }

                PartialSums[workerIndex] = sum;
            }
        }

        [BurstCompile(CompileSynchronously = true)]
        private struct NativeListWriterJob : IJobFor
        {
            public NativeList<int>.ParallelWriter Writer;
            public NativeArray<long> PartialSums;
            public int TotalWrites;
            public int WorkerCount;

            public void Execute(int workerIndex)
            {
                var start = workerIndex * TotalWrites / WorkerCount;
                var end = (workerIndex + 1) * TotalWrites / WorkerCount;

                var sum = 0L;
                for (var value = start; value < end; value++)
                {
                    Writer.AddNoResize(value);
                    sum += value;
                }

                PartialSums[workerIndex] = sum;
            }
        }

        [BurstCompile(CompileSynchronously = true)]
        private struct NativeQueueWriterJob : IJobFor
        {
            public NativeQueue<int>.ParallelWriter Writer;
            public NativeArray<long> PartialSums;
            public int TotalWrites;
            public int WorkerCount;

            public void Execute(int workerIndex)
            {
                var start = workerIndex * TotalWrites / WorkerCount;
                var end = (workerIndex + 1) * TotalWrites / WorkerCount;

                var sum = 0L;
                for (var value = start; value < end; value++)
                {
                    Writer.Enqueue(value);
                    sum += value;
                }

                PartialSums[workerIndex] = sum;
            }
        }
    }
}
