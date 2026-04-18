using NUnit.Framework;
using Unity.Collections;
using Unity.Jobs.LowLevel.Unsafe;

namespace KrasCore.Tests
{
    public class NativeThreadDataTests
    {
        [Test]
        public void ThreadWriterAndReader_CurrentThreadSlot_RoundTrip()
        {
            var data = new NativeThreadData<TestData>(Allocator.Persistent);

            try
            {
                var writer = data.AsThreadWriter();
                var first = new TestData { A = 11, B = 22 };

                writer.Set(in first);
                var pointerValue = data.GetThreadDataRef(0);
                Assert.That(pointerValue.A, Is.EqualTo(11));
                Assert.That(pointerValue.B, Is.EqualTo(22));

                ref var valueRef = ref writer.GetRef();
                valueRef.A = 33;
                valueRef.B = 44;

                var reader = data.AsThreadReader();
                var read = reader.Get();
                Assert.That(read.A, Is.EqualTo(33));
                Assert.That(read.B, Is.EqualTo(44));
            }
            finally
            {
                data.Dispose();
            }
        }

        [Test]
        public void Clear_CustomValue_AppliesToEveryThreadSlot()
        {
            var data = new NativeThreadData<TestData>(Allocator.Persistent);
            var fill = new TestData { A = 99, B = 123 };

            try
            {
                data.Clear(in fill);

                for (var i = 0; i < JobsUtility.ThreadIndexCount; i++)
                {
                    var value = data.GetThreadDataRef(i);
                    Assert.That(value.A, Is.EqualTo(fill.A));
                    Assert.That(value.B, Is.EqualTo(fill.B));
                }
            }
            finally
            {
                data.Dispose();
            }
        }

        [Test]
        public void Enumerator_Foreach_IteratesThreadSlotsInOrder()
        {
            var data = new NativeThreadData<TestData>(Allocator.Persistent);

            try
            {
                for (var i = 0; i < JobsUtility.ThreadIndexCount; i++)
                {
                    data.GetThreadDataRef(i) = new TestData
                    {
                        A = i + 500,
                        B = i * 3
                    };
                }

                var visited = 0;
                foreach (var value in data)
                {
                    Assert.That(value.A, Is.EqualTo(visited + 500));
                    Assert.That(value.B, Is.EqualTo(visited * 3));
                    visited++;
                }

                Assert.That(visited, Is.EqualTo(JobsUtility.ThreadIndexCount));
            }
            finally
            {
                data.Dispose();
            }
        }

        private struct TestData
        {
            public int A;
            public int B;
        }
    }
}
