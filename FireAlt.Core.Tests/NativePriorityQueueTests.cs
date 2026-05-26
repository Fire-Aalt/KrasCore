using System.Collections.Generic;
using NUnit.Framework;
using Unity.Collections;

namespace KrasCore.Tests
{
    public class NativePriorityQueueTests
    {
        [Test]
        public void EnqueueAndTryDequeue_ReturnsAscendingPriorities()
        {
            var list = new NativePriorityQueue<int>(Allocator.Persistent);

            try
            {
                list.Enqueue(30, 3);
                list.Enqueue(10, 1);
                list.Enqueue(40, 4);
                list.Enqueue(20, 2);

                Assert.That(list.Count, Is.EqualTo(4));
                Assert.That(list.Peek(), Is.EqualTo(10));

                Assert.That(list.TryDequeue(out var first, out var firstPriority), Is.True);
                Assert.That(first, Is.EqualTo(10));
                Assert.That(firstPriority, Is.EqualTo(1));

                Assert.That(list.TryDequeue(out var second, out var secondPriority), Is.True);
                Assert.That(second, Is.EqualTo(20));
                Assert.That(secondPriority, Is.EqualTo(2));

                Assert.That(list.TryDequeue(out var third, out var thirdPriority), Is.True);
                Assert.That(third, Is.EqualTo(30));
                Assert.That(thirdPriority, Is.EqualTo(3));

                Assert.That(list.TryDequeue(out var fourth, out var fourthPriority), Is.True);
                Assert.That(fourth, Is.EqualTo(40));
                Assert.That(fourthPriority, Is.EqualTo(4));

                Assert.That(list.TryDequeue(out _, out _), Is.False);
            }
            finally
            {
                list.Dispose();
            }
        }

        [Test]
        public void Dispose_AccessAfterDispose_ThrowsWithCollectionChecks()
        {
            var list = new NativePriorityQueue<int>(Allocator.Persistent);
            list.Enqueue(1, 1);
            list.Dispose();

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            Assert.That(() => list.Enqueue(2, 2), Throws.Exception);
            Assert.That(() => _ = list.Count, Throws.Exception);
#else
            Assert.Pass("Collection checks are disabled.");
#endif
        }

        [Test]
        public void Enumerator_VisitsItemsAndPrioritiesWithoutDequeuing()
        {
            var list = new NativePriorityQueue<int>(Allocator.Persistent);

            try
            {
                list.Enqueue(30, 3);
                list.Enqueue(10, 1);
                list.Enqueue(40, 4);
                list.Enqueue(20, 2);

                var items = new List<int>();
                var priorities = new List<int>();
                var enumerator = list.GetEnumerator();

                while (enumerator.MoveNext())
                {
                    items.Add(enumerator.Current);
                    priorities.Add(enumerator.CurrentPriority);
                }

                CollectionAssert.AreEquivalent(new[] { 10, 20, 30, 40 }, items);
                CollectionAssert.AreEquivalent(new[] { 1, 2, 3, 4 }, priorities);
                Assert.That(list.Count, Is.EqualTo(4));

                Assert.That(list.TryDequeue(out var first, out var firstPriority), Is.True);
                Assert.That(first, Is.EqualTo(10));
                Assert.That(firstPriority, Is.EqualTo(1));
            }
            finally
            {
                list.Dispose();
            }
        }

        [Test]
        public void CustomComparer_CanInvertPriorityOrdering()
        {
            var list = new NativePriorityQueue<int, DescendingComparer>(Allocator.Persistent);

            try
            {
                list.Enqueue(1, 1);
                list.Enqueue(2, 2);
                list.Enqueue(3, 3);

                Assert.That(list.Peek(), Is.EqualTo(3));

                Assert.That(list.TryDequeue(out var first, out var firstPriority), Is.True);
                Assert.That(first, Is.EqualTo(3));
                Assert.That(firstPriority, Is.EqualTo(3));

                Assert.That(list.TryDequeue(out var second, out var secondPriority), Is.True);
                Assert.That(second, Is.EqualTo(2));
                Assert.That(secondPriority, Is.EqualTo(2));

                Assert.That(list.TryDequeue(out var third, out var thirdPriority), Is.True);
                Assert.That(third, Is.EqualTo(1));
                Assert.That(thirdPriority, Is.EqualTo(1));
            }
            finally
            {
                list.Dispose();
            }
        }

        [Test]
        public void CustomComparer_Enumerator_VisitsItemsAndPrioritiesWithoutDequeuing()
        {
            var list = new NativePriorityQueue<int, DescendingComparer>(Allocator.Persistent);

            try
            {
                list.Enqueue(1, 1);
                list.Enqueue(2, 2);
                list.Enqueue(3, 3);

                var items = new List<int>();
                var priorities = new List<int>();
                var enumerator = list.GetEnumerator();

                while (enumerator.MoveNext())
                {
                    items.Add(enumerator.Current);
                    priorities.Add(enumerator.CurrentPriority);
                }

                CollectionAssert.AreEquivalent(new[] { 1, 2, 3 }, items);
                CollectionAssert.AreEquivalent(new[] { 1, 2, 3 }, priorities);
                Assert.That(list.Count, Is.EqualTo(3));

                Assert.That(list.TryDequeue(out var first, out var firstPriority), Is.True);
                Assert.That(first, Is.EqualTo(3));
                Assert.That(firstPriority, Is.EqualTo(3));
            }
            finally
            {
                list.Dispose();
            }
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
