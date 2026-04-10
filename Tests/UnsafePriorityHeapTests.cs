using System;
using NUnit.Framework;
using Unity.Collections;

namespace KrasCore.Tests
{
    public class UnsafePriorityHeapTests
    {
        [Test]
        public void EnqueueAndTryDequeue_UsesItemOrdering()
        {
            var heap = new UnsafePriorityHeap<OrderedValue>(Allocator.Persistent);

            try
            {
                heap.Enqueue(new OrderedValue(3, 30));
                heap.Enqueue(new OrderedValue(1, 10));
                heap.Enqueue(new OrderedValue(2, 20));
                heap.Enqueue(new OrderedValue(1, 5));

                Assert.That(heap.Peek(), Is.EqualTo(new OrderedValue(1, 5)));

                Assert.That(heap.TryDequeue(out var first), Is.True);
                Assert.That(first, Is.EqualTo(new OrderedValue(1, 5)));

                Assert.That(heap.TryDequeue(out var second), Is.True);
                Assert.That(second, Is.EqualTo(new OrderedValue(1, 10)));

                Assert.That(heap.TryDequeue(out var third), Is.True);
                Assert.That(third, Is.EqualTo(new OrderedValue(2, 20)));

                Assert.That(heap.TryDequeue(out var fourth), Is.True);
                Assert.That(fourth, Is.EqualTo(new OrderedValue(3, 30)));

                Assert.That(heap.TryDequeue(out _), Is.False);
            }
            finally
            {
                heap.Dispose();
            }
        }

        [Test]
        public void ArrayConstructor_HeapifiesItemOrdering()
        {
            var items = new[]
            {
                new OrderedValue(4, 40),
                new OrderedValue(1, 20),
                new OrderedValue(3, 30),
                new OrderedValue(1, 10),
                new OrderedValue(2, 50),
            };

            var heap = new UnsafePriorityHeap<OrderedValue>(new NativeArray<OrderedValue>(items, Allocator.Temp), Allocator.Persistent);

            try
            {
                var expected = new[]
                {
                    new OrderedValue(1, 10),
                    new OrderedValue(1, 20),
                    new OrderedValue(2, 50),
                    new OrderedValue(3, 30),
                    new OrderedValue(4, 40),
                };

                for (var i = 0; i < expected.Length; i++)
                {
                    Assert.That(heap.TryDequeue(out var item), Is.True);
                    Assert.That(item, Is.EqualTo(expected[i]));
                }
            }
            finally
            {
                heap.Dispose();
            }
        }

        [Test]
        public void EmptyHeap_TryMethodsReturnFalse_AndPeekThrows()
        {
            var heap = new UnsafePriorityHeap<OrderedValue>(Allocator.Persistent);

            try
            {
                Assert.That(heap.TryPeek(out var peekItem), Is.False);
                Assert.That(peekItem, Is.EqualTo(default(OrderedValue)));

                Assert.That(heap.TryDequeue(out var item), Is.False);
                Assert.That(item, Is.EqualTo(default(OrderedValue)));

                Assert.Throws<InvalidOperationException>(() => heap.Peek());
            }
            finally
            {
                heap.Dispose();
            }
        }

        private readonly struct OrderedValue : IComparable<OrderedValue>, IEquatable<OrderedValue>
        {
            public readonly int Priority;
            public readonly int TieBreaker;

            public OrderedValue(int priority, int tieBreaker)
            {
                Priority = priority;
                TieBreaker = tieBreaker;
            }

            public int CompareTo(OrderedValue other)
            {
                var priorityCompare = Priority.CompareTo(other.Priority);
                return priorityCompare != 0 ? priorityCompare : TieBreaker.CompareTo(other.TieBreaker);
            }

            public bool Equals(OrderedValue other)
            {
                return Priority == other.Priority && TieBreaker == other.TieBreaker;
            }

            public override bool Equals(object obj)
            {
                return obj is OrderedValue other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (Priority * 397) ^ TieBreaker;
                }
            }
        }
    }
}
