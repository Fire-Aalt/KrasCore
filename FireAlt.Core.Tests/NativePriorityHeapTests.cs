using System;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Collections;

namespace KrasCore.Tests
{
    public class NativePriorityHeapTests
    {
        [Test]
        public void EnqueueAndTryDequeue_UsesItemOrdering()
        {
            var heap = new NativePriorityHeap<OrderedValue>(Allocator.Persistent);

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
        public void Dispose_AccessAfterDispose_ThrowsWithCollectionChecks()
        {
            var heap = new NativePriorityHeap<OrderedValue>(Allocator.Persistent);
            heap.Enqueue(new OrderedValue(1, 1));
            heap.Dispose();

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            Assert.That(() => heap.Enqueue(new OrderedValue(2, 2)), Throws.Exception);
            Assert.That(() => _ = heap.Count, Throws.Exception);
#else
            Assert.Pass("Collection checks are disabled.");
#endif
        }

        [Test]
        public void Enumerator_VisitsItemsWithoutDequeuing()
        {
            var heap = new NativePriorityHeap<OrderedValue>(Allocator.Persistent);

            try
            {
                heap.Enqueue(new OrderedValue(3, 30));
                heap.Enqueue(new OrderedValue(1, 10));
                heap.Enqueue(new OrderedValue(2, 20));
                heap.Enqueue(new OrderedValue(1, 5));

                var items = new List<OrderedValue>();
                var enumerator = heap.GetEnumerator();

                while (enumerator.MoveNext())
                {
                    items.Add(enumerator.Current);
                }

                CollectionAssert.AreEquivalent(new[]
                {
                    new OrderedValue(1, 5),
                    new OrderedValue(1, 10),
                    new OrderedValue(2, 20),
                    new OrderedValue(3, 30),
                }, items);
                Assert.That(heap.Count, Is.EqualTo(4));

                Assert.That(heap.TryDequeue(out var first), Is.True);
                Assert.That(first, Is.EqualTo(new OrderedValue(1, 5)));
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
