using NUnit.Framework;
using Unity.Collections;
using Unity.Jobs.LowLevel.Unsafe;

namespace KrasCore.Tests
{
    public unsafe class UnsafePerThreadDataTests
    {
        [Test]
        public void Clear_Default_ResetsAllThreadSlots()
        {
            var data = new UnsafePerThreadData<TestData>(Allocator.Persistent);

            try
            {
                for (var i = 0; i < JobsUtility.ThreadIndexCount; i++)
                {
                    *data.GetUnsafeThreadData(i) = new TestData
                    {
                        A = i + 1,
                        B = (i + 1) * 10
                    };
                }

                data.Clear();

                for (var i = 0; i < JobsUtility.ThreadIndexCount; i++)
                {
                    var value = *data.GetUnsafeThreadData(i);
                    Assert.That(value.A, Is.Zero);
                    Assert.That(value.B, Is.Zero);
                }
            }
            finally
            {
                data.Dispose();
            }
        }

        [Test]
        public void Clear_CustomValue_SetsAllThreadSlots()
        {
            var data = new UnsafePerThreadData<TestData>(Allocator.Persistent);
            var fill = new TestData { A = 123, B = 456 };

            try
            {
                data.Clear(in fill);

                for (var i = 0; i < JobsUtility.ThreadIndexCount; i++)
                {
                    var value = *data.GetUnsafeThreadData(i);
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
        public void ThreadWriter_SetAndGetRef_WritesCurrentThreadSlot()
        {
            var data = new UnsafePerThreadData<TestData>(Allocator.Persistent);

            try
            {
                var writer = data.AsThreadWriter();
                var first = new TestData { A = 5, B = 10 };

                writer.Set(in first);
                var fromPointer = *data.GetUnsafeThreadData(0);
                Assert.That(fromPointer.A, Is.EqualTo(5));
                Assert.That(fromPointer.B, Is.EqualTo(10));

                ref var valueRef = ref writer.GetRef();
                valueRef.A = 999;
                valueRef.B = 321;

                fromPointer = *data.GetUnsafeThreadData(0);
                Assert.That(fromPointer.A, Is.EqualTo(999));
                Assert.That(fromPointer.B, Is.EqualTo(321));
            }
            finally
            {
                data.Dispose();
            }
        }

        [Test]
        public void ThreadReader_Get_ReadsCurrentThreadSlot()
        {
            var data = new UnsafePerThreadData<TestData>(Allocator.Persistent);

            try
            {
                *data.GetUnsafeThreadData(0) = new TestData { A = 42, B = 73 };

                var reader = data.AsThreadReader();
                var value = reader.Get();

                Assert.That(value.A, Is.EqualTo(42));
                Assert.That(value.B, Is.EqualTo(73));
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
