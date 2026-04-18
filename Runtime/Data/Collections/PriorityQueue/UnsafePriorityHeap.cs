using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace KrasCore
{
    [StructLayout(LayoutKind.Sequential)]
    [DebuggerDisplay("Count = {Count}, Capacity = {Capacity}, IsCreated = {IsCreated}")]
    public unsafe struct UnsafePriorityHeap<T> : IDisposable
        where T : unmanaged, IComparable<T>
    {
        private const int Arity = 4;
        private const int Log2Arity = 2;

        private UnsafeList<T> _nodes;

        public int Count => _nodes.IsCreated ? _nodes.Length : 0;
        public int Capacity => _nodes.IsCreated ? _nodes.Capacity : 0;
        public bool IsCreated => _nodes.IsCreated;

        public UnsafePriorityHeap(AllocatorManager.AllocatorHandle allocator)
            : this(0, allocator)
        {
        }

        public UnsafePriorityHeap(int initialCapacity, AllocatorManager.AllocatorHandle allocator)
        {
            CheckInitialCapacity(initialCapacity);
            _nodes = new UnsafeList<T>(initialCapacity, allocator);
        }

        public UnsafePriorityHeap(NativeArray<T> items, AllocatorManager.AllocatorHandle allocator)
        {
            if (!items.IsCreated)
            {
                throw new ArgumentNullException(nameof(items));
            }

            _nodes = new UnsafeList<T>(items.Length, allocator);
            _nodes.Resize(items.Length);

            var nodes = _nodes.Ptr;
            for (var i = 0; i < items.Length; i++)
            {
                nodes[i] = items[i];
            }

            if (items.Length > 1)
            {
                Heapify();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Enqueue(T item)
        {
            var currentSize = Count;

            if (_nodes.Capacity == currentSize)
            {
                Grow(currentSize + 1);
            }

            _nodes.Resize(currentSize + 1);
            MoveUp(item, currentSize);
        }

        public bool TryDequeue(out T item)
        {
            if (Count == 0)
            {
                item = default;
                return false;
            }

            item = _nodes.Ptr[0];
            RemoveRootNode();
            return true;
        }

        public bool TryPeek(out T item)
        {
            if (Count == 0)
            {
                item = default;
                return false;
            }

            item = _nodes.Ptr[0];
            return true;
        }

        public T Peek()
        {
            if (Count == 0)
            {
                ThrowHeapEmpty();
            }

            return _nodes.Ptr[0];
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

        private void MoveUp(T node, int nodeIndex)
        {
            var nodes = _nodes.Ptr;

            while (nodeIndex > 0)
            {
                var parentIndex = GetParentIndex(nodeIndex);
                var parent = nodes[parentIndex];
                if (Compare(node, parent) < 0)
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

        private void MoveDown(T node, int nodeIndex)
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
                    if (Compare(candidate, minChild) < 0)
                    {
                        minChild = candidate;
                        minChildIndex = i;
                    }
                }

                if (Compare(node, minChild) <= 0)
                {
                    break;
                }

                nodes[nodeIndex] = minChild;
                nodeIndex = minChildIndex;
            }

            nodes[nodeIndex] = node;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int Compare(T left, T right)
        {
            return left.CompareTo(right);
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

        private static void ThrowHeapEmpty()
        {
            throw new InvalidOperationException("The priority heap is empty.");
        }
    }
}
