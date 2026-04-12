using System;
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
    public struct NativePriorityHeap<T> : IDisposable
        where T : unmanaged, IComparable<T>
    {
        private UnsafePriorityHeap<T> _heap;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        private AtomicSafetyHandle m_Safety;
        private static readonly SharedStatic<int> s_staticSafetyId = SharedStatic<int>.GetOrCreate<NativePriorityHeap<T>>();
#endif

        public int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                CheckRead();
                return _heap.Count;
            }
        }

        public int Capacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                CheckRead();
                return _heap.Capacity;
            }
        }

        public bool IsCreated => _heap.IsCreated;

        public NativePriorityHeap(AllocatorManager.AllocatorHandle allocator)
            : this(0, allocator)
        {
        }

        public NativePriorityHeap(int initialCapacity, AllocatorManager.AllocatorHandle allocator)
        {
            this = default;
            _heap = new UnsafePriorityHeap<T>(initialCapacity, allocator);

            try
            {
                InitializeSafety(allocator);
            }
            catch
            {
                _heap.Dispose();
                throw;
            }
        }

        public NativePriorityHeap(NativeArray<T> items, AllocatorManager.AllocatorHandle allocator)
        {
            this = default;
            _heap = new UnsafePriorityHeap<T>(items, allocator);

            try
            {
                InitializeSafety(allocator);
            }
            catch
            {
                _heap.Dispose();
                throw;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Enqueue(T item)
        {
            CheckWrite();
            _heap.Enqueue(item);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryDequeue(out T item)
        {
            CheckWrite();
            return _heap.TryDequeue(out item);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryPeek(out T item)
        {
            CheckRead();
            return _heap.TryPeek(out item);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Peek()
        {
            CheckRead();
            return _heap.Peek();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            CheckWrite();
            _heap.Clear();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int EnsureCapacity(int capacity)
        {
            CheckWrite();
            return _heap.EnsureCapacity(capacity);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void TrimExcess()
        {
            CheckWrite();
            _heap.TrimExcess();
        }

        public void Dispose()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            CollectionHelper.DisposeSafetyHandle(ref m_Safety);
#endif
            _heap.Dispose();
            _heap = default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void InitializeSafety(AllocatorManager.AllocatorHandle allocator)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            CollectionHelper.CheckAllocator(allocator);
            m_Safety = CollectionHelper.CreateSafetyHandle(allocator);
            CollectionHelper.InitNativeContainer<T>(m_Safety);
            CollectionHelper.SetStaticSafetyId<NativePriorityHeap<T>>(ref m_Safety, ref s_staticSafetyId.Data);
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
