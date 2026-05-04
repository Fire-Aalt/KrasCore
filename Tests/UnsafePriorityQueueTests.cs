using System;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Collections;

namespace KrasCore.Tests
{
    public class UnsafePriorityQueueTests
    {
        [Test]
        public void EnqueueAndTryDequeue_DefaultComparer_ReturnsAscendingPriorities()
        {
            var queue = new UnsafePriorityQueue<int>(Allocator.Persistent);

            try
            {
                queue.Enqueue(30, 3);
                queue.Enqueue(10, 1);
                queue.Enqueue(40, 4);
                queue.Enqueue(20, 2);

                Assert.That(queue.Count, Is.EqualTo(4));
                Assert.That(queue.Peek(), Is.EqualTo(10));

                Assert.That(queue.TryDequeue(out var first, out var firstPriority), Is.True);
                Assert.That(first, Is.EqualTo(10));
                Assert.That(firstPriority, Is.EqualTo(1));

                Assert.That(queue.TryDequeue(out var second, out var secondPriority), Is.True);
                Assert.That(second, Is.EqualTo(20));
                Assert.That(secondPriority, Is.EqualTo(2));

                Assert.That(queue.TryDequeue(out var third, out var thirdPriority), Is.True);
                Assert.That(third, Is.EqualTo(30));
                Assert.That(thirdPriority, Is.EqualTo(3));

                Assert.That(queue.TryDequeue(out var fourth, out var fourthPriority), Is.True);
                Assert.That(fourth, Is.EqualTo(40));
                Assert.That(fourthPriority, Is.EqualTo(4));

                Assert.That(queue.TryDequeue(out _, out _), Is.False);
            }
            finally
            {
                queue.Dispose();
            }
        }

        [Test]
        public void ArrayConstructor_HeapifiesWithoutSequentialEnqueue()
        {
            var items = new[] { 600, 100, 400, 200, 500, 300 };
            var priorities = new[] { 6, 1, 4, 2, 5, 3 };
            var queue = new UnsafePriorityQueue<int>(items, priorities, Allocator.Persistent);

            try
            {
                var expectedItems = new[] { 100, 200, 300, 400, 500, 600 };

                for (var expectedPriority = 1; expectedPriority <= priorities.Length; expectedPriority++)
                {
                    Assert.That(queue.TryDequeue(out var item, out var priority), Is.True);
                    Assert.That(priority, Is.EqualTo(expectedPriority));
                    Assert.That(item, Is.EqualTo(expectedItems[expectedPriority - 1]));
                }
            }
            finally
            {
                queue.Dispose();
            }
        }

        [Test]
        public void CustomComparer_CanInvertPriorityOrdering()
        {
            var queue = new UnsafePriorityQueue<int, DescendingComparer>(Allocator.Persistent);

            try
            {
                queue.Enqueue(1, 1);
                queue.Enqueue(2, 2);
                queue.Enqueue(3, 3);

                Assert.That(queue.Peek(), Is.EqualTo(3));

                Assert.That(queue.TryDequeue(out var first, out var firstPriority), Is.True);
                Assert.That(first, Is.EqualTo(3));
                Assert.That(firstPriority, Is.EqualTo(3));

                Assert.That(queue.TryDequeue(out var second, out var secondPriority), Is.True);
                Assert.That(second, Is.EqualTo(2));
                Assert.That(secondPriority, Is.EqualTo(2));

                Assert.That(queue.TryDequeue(out var third, out var thirdPriority), Is.True);
                Assert.That(third, Is.EqualTo(1));
                Assert.That(thirdPriority, Is.EqualTo(1));
            }
            finally
            {
                queue.Dispose();
            }
        }

        [Test]
        public void Enumerator_VisitsItemsAndPrioritiesWithoutDequeuing()
        {
            var queue = new UnsafePriorityQueue<int>(Allocator.Persistent);

            try
            {
                queue.Enqueue(30, 3);
                queue.Enqueue(10, 1);
                queue.Enqueue(40, 4);
                queue.Enqueue(20, 2);

                var items = new List<int>();
                var priorities = new List<int>();
                var enumerator = queue.GetEnumerator();

                while (enumerator.MoveNext())
                {
                    items.Add(enumerator.Current);
                    priorities.Add(enumerator.CurrentPriority);
                }

                CollectionAssert.AreEquivalent(new[] { 10, 20, 30, 40 }, items);
                CollectionAssert.AreEquivalent(new[] { 1, 2, 3, 4 }, priorities);
                Assert.That(queue.Count, Is.EqualTo(4));

                Assert.That(queue.TryDequeue(out var first, out var firstPriority), Is.True);
                Assert.That(first, Is.EqualTo(10));
                Assert.That(firstPriority, Is.EqualTo(1));
            }
            finally
            {
                queue.Dispose();
            }
        }

        [Test]
        public void CustomComparer_Enumerator_VisitsItemsAndPrioritiesWithoutDequeuing()
        {
            var queue = new UnsafePriorityQueue<int, DescendingComparer>(Allocator.Persistent);

            try
            {
                queue.Enqueue(1, 1);
                queue.Enqueue(2, 2);
                queue.Enqueue(3, 3);

                var items = new List<int>();
                var priorities = new List<int>();
                var enumerator = queue.GetEnumerator();

                while (enumerator.MoveNext())
                {
                    items.Add(enumerator.Current);
                    priorities.Add(enumerator.CurrentPriority);
                }

                CollectionAssert.AreEquivalent(new[] { 1, 2, 3 }, items);
                CollectionAssert.AreEquivalent(new[] { 1, 2, 3 }, priorities);
                Assert.That(queue.Count, Is.EqualTo(3));

                Assert.That(queue.TryDequeue(out var first, out var firstPriority), Is.True);
                Assert.That(first, Is.EqualTo(3));
                Assert.That(firstPriority, Is.EqualTo(3));
            }
            finally
            {
                queue.Dispose();
            }
        }

        [Test]
        public void EmptyQueue_TryMethodsReturnFalse_AndPeekThrows()
        {
            var queue = new UnsafePriorityQueue<int>(Allocator.Persistent);

            try
            {
                Assert.That(queue.TryPeek(out var peekItem, out var peekPriority), Is.False);
                Assert.That(peekItem, Is.EqualTo(0));
                Assert.That(peekPriority, Is.EqualTo(0));

                Assert.That(queue.TryDequeue(out var item, out var priority), Is.False);
                Assert.That(item, Is.EqualTo(0));
                Assert.That(priority, Is.EqualTo(0));

                Assert.Throws<InvalidOperationException>(() => queue.Peek());
            }
            finally
            {
                queue.Dispose();
            }
        }

        [Test]
        public void ArrayConstructor_LengthMismatch_Throws()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                _ = new UnsafePriorityQueue<int>(new[] { 1, 2 }, new[] { 1 }, Allocator.Persistent);
            });
        }

        private struct DescendingComparer : IComparer<int>
        {
            public int Compare(int x, int y)
            {
                return y.CompareTo(x);
            }
        }
    }
}
