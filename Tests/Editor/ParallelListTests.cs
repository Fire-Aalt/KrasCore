using System;
using System.Threading.Tasks;
using KrasCore.NZCore;
using NUnit.Framework;
using Unity.Collections;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;

namespace KrasCore.Tests.Editor
{
    public class ParallelListTests
    {
        private const int ManagedParallelSlotCap = 16;
        private const int JobParallelSlotCap = 32;

        [Test]
        public void ThreadWriter_ParallelJobWritesAcrossSlots_PreservesAllValues()
        {
            var slotCount = Math.Min(JobsUtility.ThreadIndexCount, JobParallelSlotCap);
            var itemsPerSlot = 256;
            var expectedCount = slotCount * itemsPerSlot;

            var list = new ParallelList<int>(1, Allocator.Persistent);
            var copied = new NativeList<int>(expectedCount, Allocator.TempJob);

            try
            {
                new WriteThreadSlotsJob
                {
                    Writer = list.AsThreadWriter(),
                    ItemsPerSlot = itemsPerSlot,
                    ValueOffset = 0
                }.ScheduleParallel(slotCount, 1, default).Complete();

                Assert.That(list.Length, Is.EqualTo(expectedCount));
                AssertUniformThreadCounts(list, slotCount, itemsPerSlot);

                list.CopyToListSingle(ref copied, default).Complete();
                Assert.That(copied.Length, Is.EqualTo(expectedCount));
                AssertContainsUniqueContiguousValues(copied, 0, expectedCount);
            }
            finally
            {
                copied.Dispose();
                list.Dispose();
            }
        }

        [Test]
        public void ThreadWriter_ClearAndReuse_MultipleRoundsStaysStable()
        {
            var slotCount = Math.Min(JobsUtility.ThreadIndexCount, JobParallelSlotCap);
            var itemsPerSlot = 128;
            var rounds = 12;
            var valuesPerRound = slotCount * itemsPerSlot;

            var list = new ParallelList<int>(1, Allocator.Persistent);
            var copied = new NativeList<int>(valuesPerRound, Allocator.TempJob);

            try
            {
                for (var round = 0; round < rounds; round++)
                {
                    var valueOffset = round * valuesPerRound;

                    new WriteThreadSlotsJob
                    {
                        Writer = list.AsThreadWriter(),
                        ItemsPerSlot = itemsPerSlot,
                        ValueOffset = valueOffset
                    }.ScheduleParallel(slotCount, 1, default).Complete();

                    Assert.That(list.Length, Is.EqualTo(valuesPerRound));
                    AssertUniformThreadCounts(list, slotCount, itemsPerSlot);

                    copied.Clear();
                    list.CopyToListSingle(ref copied, default).Complete();
                    AssertContainsUniqueContiguousValues(copied, valueOffset, valuesPerRound);
                    Assert.That(Sum(copied), Is.EqualTo(ContiguousRangeSum(valueOffset, valuesPerRound)));

                    list.Clear();
                    Assert.That(list.Length, Is.Zero);
                    AssertUniformThreadCounts(list, slotCount, 0);
                }
            }
            finally
            {
                copied.Dispose();
                list.Dispose();
            }
        }

        [Test]
        public void ChunkWriter_ParallelRoundTrip_ReaderReturnsExpectedData()
        {
            var chunkCount = Math.Min(JobsUtility.ThreadIndexCount, JobParallelSlotCap);
            var itemsPerChunk = 64;
            var expectedCount = chunkCount * itemsPerChunk;

            var list = new ParallelList<int>(1, Allocator.Persistent);

            try
            {
                list.SetChunkCount(chunkCount);

                new WriteChunksJob
                {
                    Writer = list.AsChunkWriter(),
                    ItemsPerChunk = itemsPerChunk,
                    ValueOffset = 0
                }.ScheduleParallel(chunkCount, 1, default).Complete();

                Assert.That(list.GetChunkCount(), Is.EqualTo(chunkCount));
                Assert.That(list.Length, Is.EqualTo(expectedCount));

                var reader = list.AsChunkReader();
                for (var chunk = 0; chunk < chunkCount; chunk++)
                {
                    var count = reader.BeginForEachChunk(chunk);
                    Assert.That(count, Is.EqualTo(itemsPerChunk));
                    Assert.That(reader.GetListIndex(chunk), Is.EqualTo(chunk));

                    for (var i = 0; i < itemsPerChunk; i++)
                    {
                        var expected = chunk * itemsPerChunk + i;
                        var actual = reader.Read();
                        Assert.That(actual, Is.EqualTo(expected));
                    }

                    reader.Reset(chunk);
                    Assert.That(reader.Read(), Is.EqualTo(chunk * itemsPerChunk));
                }
            }
            finally
            {
                list.Dispose();
            }
        }

        private static void AssertUniformThreadCounts(ParallelList<int> list, int slotCount, int expectedPerSlot)
        {
            for (var i = 0; i < slotCount; i++)
            {
                Assert.That(list.GetBlockCount(i), Is.EqualTo(expectedPerSlot), $"Unexpected block count for slot {i}");
            }
        }

        private static void AssertContainsUniqueContiguousValues(NativeList<int> values, int offset, int count)
        {
            var seen = new bool[count];
            var max = offset + count;

            for (var i = 0; i < values.Length; i++)
            {
                var value = values[i];
                Assert.That(value, Is.GreaterThanOrEqualTo(offset));
                Assert.That(value, Is.LessThan(max));

                var idx = value - offset;
                Assert.That(seen[idx], Is.False, $"Duplicate value {value}");
                seen[idx] = true;
            }

            for (var i = 0; i < seen.Length; i++)
            {
                Assert.That(seen[i], Is.True, $"Missing value {offset + i}");
            }
        }

        private static long Sum(NativeList<int> values)
        {
            var result = 0L;
            for (var i = 0; i < values.Length; i++)
            {
                result += values[i];
            }

            return result;
        }

        private static long ContiguousRangeSum(int start, int count)
        {
            var c = (long)count;
            return c * (2L * start + c - 1L) / 2L;
        }

        private struct WriteThreadSlotsJob : IJobFor
        {
            public ParallelList<int>.ThreadWriter Writer;
            public int ItemsPerSlot;
            public int ValueOffset;

            public void Execute(int slotIndex)
            {
                var baseValue = ValueOffset + slotIndex * ItemsPerSlot;
                for (var i = 0; i < ItemsPerSlot; i++)
                {
                    var value = baseValue + i;
                    Writer.Add(in value, slotIndex);
                }
            }
        }

        private struct WriteChunksJob : IJobFor
        {
            public ParallelList<int>.ChunkWriter Writer;
            public int ItemsPerChunk;
            public int ValueOffset;

            public void Execute(int chunkIndex)
            {
                Writer.SetManualThreadIndex(chunkIndex);
                Writer.BeginForEachChunk(chunkIndex);

                var baseValue = ValueOffset + chunkIndex * ItemsPerChunk;
                for (var i = 0; i < ItemsPerChunk; i++)
                {
                    var value = baseValue + i;
                    Writer.Add(in value);
                }

                Writer.EndForEachChunk();
            }
        }
    }
}
