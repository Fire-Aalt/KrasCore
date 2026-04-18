using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace KrasCore
{
    [NativeContainer]
    [StructLayout(LayoutKind.Sequential)]
    [DebuggerDisplay("Count = {Count}, Capacity = {Capacity}, IsCreated = {IsCreated}")]
    public struct NativePriorityQueue<T> : IDisposable
        where T : unmanaged
    {
        private UnsafePriorityQueue<T> _queue;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        private AtomicSafetyHandle m_Safety;
        private static readonly SharedStatic<int> s_staticSafetyId = SharedStatic<int>.GetOrCreate<NativePriorityQueue<T>>();
#endif

        public int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                CheckRead();
                return _queue.Count;
            }
        }

        public int Capacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                CheckRead();
                return _queue.Capacity;
            }
        }

        public bool IsCreated => _queue.IsCreated;

        public NativePriorityQueue(AllocatorManager.AllocatorHandle allocator)
            : this(0, allocator)
        {
        }

        public NativePriorityQueue(int initialCapacity, AllocatorManager.AllocatorHandle allocator)
        {
            this = default;
            _queue = new UnsafePriorityQueue<T>(initialCapacity, allocator);

            try
            {
                InitializeSafety(allocator);
            }
            catch
            {
                _queue.Dispose();
                throw;
            }
        }

        public NativePriorityQueue(T[] items, int[] priorities, AllocatorManager.AllocatorHandle allocator)
        {
            this = default;
            _queue = new UnsafePriorityQueue<T>(items, priorities, allocator);

            try
            {
                InitializeSafety(allocator);
            }
            catch
            {
                _queue.Dispose();
                throw;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Enqueue(T item, int priority)
        {
            CheckWrite();
            _queue.Enqueue(item, priority);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryDequeue(out T item, out int priority)
        {
            CheckWrite();
            return _queue.TryDequeue(out item, out priority);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryPeek(out T item, out int priority)
        {
            CheckRead();
            return _queue.TryPeek(out item, out priority);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Peek()
        {
            CheckRead();
            return _queue.Peek();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            CheckWrite();
            _queue.Clear();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int EnsureCapacity(int capacity)
        {
            CheckWrite();
            return _queue.EnsureCapacity(capacity);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void TrimExcess()
        {
            CheckWrite();
            _queue.TrimExcess();
        }

        public void Dispose()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            CollectionHelper.DisposeSafetyHandle(ref m_Safety);
#endif
            _queue.Dispose();
            _queue = default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void InitializeSafety(AllocatorManager.AllocatorHandle allocator)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            CollectionHelper.CheckAllocator(allocator);
            m_Safety = CollectionHelper.CreateSafetyHandle(allocator);
            CollectionHelper.InitNativeContainer<T>(m_Safety);
            CollectionHelper.SetStaticSafetyId<NativePriorityQueue<T>>(ref m_Safety, ref s_staticSafetyId.Data);
            AtomicSafetyHandle.SetBumpSecondaryVersionOnScheduleWrite(m_Safety, true);
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckRead()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckWrite()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
        }
    }

    [NativeContainer]
    [StructLayout(LayoutKind.Sequential)]
    [DebuggerDisplay("Count = {Count}, Capacity = {Capacity}, IsCreated = {IsCreated}")]
    public struct NativePriorityQueue<T, TComparer> : IDisposable
        where T : unmanaged
        where TComparer : unmanaged, IComparer<int>
    {
        private UnsafePriorityQueue<T, TComparer> _queue;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        private AtomicSafetyHandle m_Safety;
        private static readonly SharedStatic<int> s_staticSafetyId = SharedStatic<int>.GetOrCreate<NativePriorityQueue<T, TComparer>>();
#endif

        public int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                CheckRead();
                return _queue.Count;
            }
        }

        public int Capacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                CheckRead();
                return _queue.Capacity;
            }
        }

        public bool IsCreated => _queue.IsCreated;

        public NativePriorityQueue(AllocatorManager.AllocatorHandle allocator)
            : this(0, allocator, default)
        {
        }

        public NativePriorityQueue(int initialCapacity, AllocatorManager.AllocatorHandle allocator)
            : this(initialCapacity, allocator, default)
        {
        }

        public NativePriorityQueue(int initialCapacity, AllocatorManager.AllocatorHandle allocator, TComparer comparer)
        {
            this = default;
            _queue = new UnsafePriorityQueue<T, TComparer>(initialCapacity, allocator, comparer);

            try
            {
                InitializeSafety(allocator);
            }
            catch
            {
                _queue.Dispose();
                throw;
            }
        }

        public NativePriorityQueue(T[] items, int[] priorities, AllocatorManager.AllocatorHandle allocator)
            : this(items, priorities, allocator, default)
        {
        }

        public NativePriorityQueue(T[] items, int[] priorities, AllocatorManager.AllocatorHandle allocator, TComparer comparer)
        {
            this = default;
            _queue = new UnsafePriorityQueue<T, TComparer>(items, priorities, allocator, comparer);

            try
            {
                InitializeSafety(allocator);
            }
            catch
            {
                _queue.Dispose();
                throw;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Enqueue(T item, int priority)
        {
            CheckWrite();
            _queue.Enqueue(item, priority);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryDequeue(out T item, out int priority)
        {
            CheckWrite();
            return _queue.TryDequeue(out item, out priority);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryPeek(out T item, out int priority)
        {
            CheckRead();
            return _queue.TryPeek(out item, out priority);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Peek()
        {
            CheckRead();
            return _queue.Peek();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            CheckWrite();
            _queue.Clear();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int EnsureCapacity(int capacity)
        {
            CheckWrite();
            return _queue.EnsureCapacity(capacity);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void TrimExcess()
        {
            CheckWrite();
            _queue.TrimExcess();
        }

        public void Dispose()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            CollectionHelper.DisposeSafetyHandle(ref m_Safety);
#endif
            _queue.Dispose();
            _queue = default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void InitializeSafety(AllocatorManager.AllocatorHandle allocator)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            CollectionHelper.CheckAllocator(allocator);
            m_Safety = CollectionHelper.CreateSafetyHandle(allocator);
            CollectionHelper.InitNativeContainer<T>(m_Safety);
            CollectionHelper.SetStaticSafetyId<NativePriorityQueue<T, TComparer>>(ref m_Safety, ref s_staticSafetyId.Data);
            AtomicSafetyHandle.SetBumpSecondaryVersionOnScheduleWrite(m_Safety, true);
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckRead()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckWrite()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
        }
    }
}
