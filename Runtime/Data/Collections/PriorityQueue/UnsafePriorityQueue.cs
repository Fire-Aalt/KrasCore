using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace KrasCore
{
    public struct DefaultPriorityComparer : IComparer<int>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Compare(int x, int y)
        {
            return x.CompareTo(y);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    [DebuggerDisplay("Count = {Count}, Capacity = {Capacity}, IsCreated = {IsCreated}")]
    public struct UnsafePriorityQueue<T> : IDisposable
        where T : unmanaged
    {
        private UnsafePriorityQueue<T, DefaultPriorityComparer> _queue;

        public int Count => _queue.Count;
        public int Capacity => _queue.Capacity;
        public bool IsCreated => _queue.IsCreated;

        public UnsafePriorityQueue(AllocatorManager.AllocatorHandle allocator)
            : this(0, allocator)
        {
        }

        public UnsafePriorityQueue(int initialCapacity, AllocatorManager.AllocatorHandle allocator)
        {
            _queue = new UnsafePriorityQueue<T, DefaultPriorityComparer>(initialCapacity, allocator);
        }

        public UnsafePriorityQueue(T[] items, int[] priorities, AllocatorManager.AllocatorHandle allocator)
        {
            _queue = new UnsafePriorityQueue<T, DefaultPriorityComparer>(items, priorities, allocator);
        }

        public void Enqueue(T item, int priority)
        {
            _queue.Enqueue(item, priority);
        }

        public bool TryDequeue(out T item, out int priority)
        {
            return _queue.TryDequeue(out item, out priority);
        }

        public bool TryPeek(out T item, out int priority)
        {
            return _queue.TryPeek(out item, out priority);
        }

        public T Peek()
        {
            return _queue.Peek();
        }

        public void Clear()
        {
            _queue.Clear();
        }

        public int EnsureCapacity(int capacity)
        {
            return _queue.EnsureCapacity(capacity);
        }

        public void TrimExcess()
        {
            _queue.TrimExcess();
        }

        public void Dispose()
        {
            _queue.Dispose();
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    [DebuggerDisplay("Count = {Count}, Capacity = {Capacity}, IsCreated = {IsCreated}")]
    public unsafe struct UnsafePriorityQueue<T, TComparer> : IDisposable
        where T : unmanaged
        where TComparer : unmanaged, IComparer<int>
    {
        private const int Arity = 4;
        private const int Log2Arity = 2;

        private UnsafeList<Entry> _nodes;
        private TComparer _comparer;

        public int Count => _nodes.IsCreated ? _nodes.Length : 0;
        public int Capacity => _nodes.IsCreated ? _nodes.Capacity : 0;
        public bool IsCreated => _nodes.IsCreated;

        public UnsafePriorityQueue(AllocatorManager.AllocatorHandle allocator)
            : this(0, allocator, default)
        {
        }

        public UnsafePriorityQueue(int initialCapacity, AllocatorManager.AllocatorHandle allocator)
            : this(initialCapacity, allocator, default)
        {
        }

        public UnsafePriorityQueue(int initialCapacity, AllocatorManager.AllocatorHandle allocator, TComparer comparer)
        {
            CheckInitialCapacity(initialCapacity);
            _nodes = new UnsafeList<Entry>(initialCapacity, allocator);
            _comparer = comparer;
        }

        public UnsafePriorityQueue(T[] items, int[] priorities, AllocatorManager.AllocatorHandle allocator)
            : this(items, priorities, allocator, default)
        {
        }

        public UnsafePriorityQueue(T[] items, int[] priorities, AllocatorManager.AllocatorHandle allocator, TComparer comparer)
        {
            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            if (priorities == null)
            {
                throw new ArgumentNullException(nameof(priorities));
            }

            if (items.Length != priorities.Length)
            {
                throw new ArgumentException("Items and priorities must have the same length.");
            }

            _nodes = new UnsafeList<Entry>(items.Length, allocator);
            _comparer = comparer;
            _nodes.Resize(items.Length);

            var nodes = _nodes.Ptr;
            for (var i = 0; i < items.Length; i++)
            {
                nodes[i] = new Entry(items[i], priorities[i]);
            }

            if (items.Length > 1)
            {
                Heapify();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Enqueue(T item, int priority)
        {
            var currentSize = Count;

            if (_nodes.Capacity == currentSize)
            {
                Grow(currentSize + 1);
            }

            _nodes.Resize(currentSize + 1);
            MoveUp(new Entry(item, priority), currentSize);
        }

        public bool TryDequeue(out T item, out int priority)
        {
            if (Count == 0)
            {
                item = default;
                priority = default;
                return false;
            }

            var root = _nodes.Ptr[0];
            item = root.Element;
            priority = root.Priority;
            RemoveRootNode();
            return true;
        }

        public bool TryPeek(out T item, out int priority)
        {
            if (Count == 0)
            {
                item = default;
                priority = default;
                return false;
            }

            var root = _nodes.Ptr[0];
            item = root.Element;
            priority = root.Priority;
            return true;
        }

        public T Peek()
        {
            if (Count == 0)
            {
                ThrowQueueEmpty();
            }

            return _nodes.Ptr[0].Element;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            if (_nodes.IsCreated)
            {
                _nodes.Clear();
            }
        }

        public int EnsureCapacity(int capacity)
        {
            CheckInitialCapacity(capacity);

            if (_nodes.Capacity < capacity)
            {
                Grow(capacity);
            }

            return _nodes.Capacity;
        }

        public void TrimExcess()
        {
            if (!_nodes.IsCreated || _nodes.Length == _nodes.Capacity)
            {
                return;
            }

            _nodes.Capacity = _nodes.Length;
        }

        public void Dispose()
        {
            if (_nodes.IsCreated)
            {
                _nodes.Dispose();
            }
        }

        private void RemoveRootNode()
        {
            var newSize = Count - 1;
            var nodes = _nodes.Ptr;

            if (newSize > 0)
            {
                var lastNode = nodes[newSize];
                _nodes.Resize(newSize);
                MoveDown(lastNode, 0);
            }
            else
            {
                _nodes.Resize(0);
            }
        }

        private void Heapify()
        {
            var lastParentWithChildren = GetParentIndex(Count - 1);
            for (var index = lastParentWithChildren; index >= 0; index--)
            {
                MoveDown(_nodes.Ptr[index], index);
            }
        }

        private void MoveUp(Entry node, int nodeIndex)
        {
            var nodes = _nodes.Ptr;

            while (nodeIndex > 0)
            {
                var parentIndex = GetParentIndex(nodeIndex);
                var parent = nodes[parentIndex];
                if (Compare(node.Priority, parent.Priority) < 0)
                {
                    nodes[nodeIndex] = parent;
                    nodeIndex = parentIndex;
                }
                else
                {
                    break;
                }
            }

            nodes[nodeIndex] = node;
        }

        private void MoveDown(Entry node, int nodeIndex)
        {
            var nodes = _nodes.Ptr;
            var size = Count;

            while (true)
            {
                var firstChildIndex = GetFirstChildIndex(nodeIndex);
                if (firstChildIndex >= size)
                {
                    break;
                }

                var minChild = nodes[firstChildIndex];
                var minChildIndex = firstChildIndex;
                var childIndexUpperBound = math.min(firstChildIndex + Arity, size);

                for (var i = firstChildIndex + 1; i < childIndexUpperBound; i++)
                {
                    var candidate = nodes[i];
                    if (Compare(candidate.Priority, minChild.Priority) < 0)
                    {
                        minChild = candidate;
                        minChildIndex = i;
                    }
                }

                if (Compare(node.Priority, minChild.Priority) <= 0)
                {
                    break;
                }

                nodes[nodeIndex] = minChild;
                nodeIndex = minChildIndex;
            }

            nodes[nodeIndex] = node;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int Compare(int x, int y)
        {
            return _comparer.Compare(x, y);
        }

        private void Grow(int minCapacity)
        {
            var newCapacity = _nodes.Capacity * 2;
            newCapacity = math.max(newCapacity, _nodes.Capacity + 4);
            if (newCapacity < minCapacity)
            {
                newCapacity = minCapacity;
            }

            _nodes.Capacity = newCapacity;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetParentIndex(int index)
        {
            return (index - 1) >> Log2Arity;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetFirstChildIndex(int index)
        {
            return (index << Log2Arity) + 1;
        }

        private static void CheckInitialCapacity(int initialCapacity)
        {
            if (initialCapacity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(initialCapacity), "Capacity must be >= 0");
            }
        }

        [DoesNotReturn]
        private static void ThrowQueueEmpty()
        {
            throw new InvalidOperationException("The priority queue is empty.");
        }

        [StructLayout(LayoutKind.Sequential)]
        private readonly struct Entry
        {
            public readonly T Element;
            public readonly int Priority;

            public Entry(T element, int priority)
            {
                Element = element;
                Priority = priority;
            }
        }
    }
}
