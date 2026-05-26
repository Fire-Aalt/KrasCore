using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs.LowLevel.Unsafe;

namespace KrasCore.Collections.Pooled
{
    /// <summary>
    /// A pooled wrapper around UnsafeList that reuses allocated memory across instances to reduce allocation pressure.
    /// Uses thread-local pools to avoid contention in multi-threaded scenarios.
    /// </summary>
    /// <typeparam name="T">The unmanaged element type.</typeparam>
    public struct NativeListPool<T>
        where T : unmanaged
    {
        /// <summary>
        /// Creates a new PooledNativeList instance, either from the thread-local pool or by allocating a new one.
        /// </summary>
        /// <returns>A PooledNativeList instance ready for use.</returns>
        /// <remarks>
        /// This method is thread-safe and will reuse previously disposed instances when available.
        /// The returned instance must be disposed to return it to the pool.
        /// </remarks>
        public static PooledNativeList<T> Rent(int minCapacity = 0)
        {
            return default(PooledNativeList<T>).Create(minCapacity);
        }
    }

    /// <summary>
    /// A pool for UnsafeList instances that share backing storage with <see cref="NativeListPool{T}" />.
    /// </summary>
    /// <typeparam name="T">The unmanaged element type.</typeparam>
    public struct UnsafeListPool<T>
        where T : unmanaged
    {
        /// <summary>
        /// Creates a new PooledUnsafeList instance, either from the thread-local pool or by allocating a new one.
        /// </summary>
        /// <returns>A PooledUnsafeList instance ready for use.</returns>
        public static PooledUnsafeList<T> Rent(int minCapacity = 0)
        {
            return default(PooledUnsafeList<T>).Create(minCapacity);
        }
    }
    
    /// <summary>
    /// A pooled wrapper around NativeList that reuses allocated memory across instances to reduce allocation pressure.
    /// Uses thread-local pools to avoid contention in multi-threaded scenarios.
    /// </summary>
    /// <typeparam name="T">The unmanaged element type.</typeparam>
    public unsafe struct PooledUnsafeList<T> : IDisposable
        where T : unmanaged
    {
        private Ref<UnsafeList<T>> _list;

        /// <summary>
        /// Gets the underlying UnsafeList instance.
        /// </summary>
        /// <value>The wrapped UnsafeList that can be used for all list operations.</value>
        public ref UnsafeList<T> List => ref _list.Value;
        
        internal PooledUnsafeList<T> Create(int minCapacity)
        {
            _list = ListPool.RentUnsafeList<T>(minCapacity);
            return this;
        }

        public UnsafeArray<T> AsUnsafeArray(int length, NativeArrayOptions options = NativeArrayOptions.ClearMemory) 
        {
            List.Resize(length, options);
            return UnsafeArrayUtility.ConvertExistingDataToUnsafeArray<T>(List.Ptr, List.Length, Allocator.None);
        }
        
        /// <summary>
        /// Disposes the PooledUnsafeList and returns the underlying memory to the thread-local pool for reuse.
        /// </summary>
        /// <remarks>
        /// This method clears the list contents and converts it back to a byte list for storage in the pool.
        /// The instance should not be used after disposal.
        /// </remarks>
        public void Dispose()
        {
            if (_list == null)
            {
                return;
            }

            ListPool.ReturnUnsafeList(_list);
            _list = null;
        }
    }
    
    
    /// <summary>
    /// A pooled wrapper around NativeList that reuses allocated memory across instances to reduce allocation pressure.
    /// Uses thread-local pools to avoid contention in multi-threaded scenarios.
    /// </summary>
    /// <typeparam name="T">The unmanaged element type.</typeparam>
    public unsafe struct PooledNativeList<T> : IDisposable
        where T : unmanaged
    {
        private NativeList<T> _list;

        /// <summary>
        /// Gets the underlying NativeList instance.
        /// </summary>
        /// <value>The wrapped NativeList that can be used for all list operations.</value>
        public NativeList<T> List => _list;
        
        internal PooledNativeList<T> Create(int minCapacity)
        {
            _list = default;
            _list.m_ListData = ListPool.RentUnsafeList<T>(minCapacity);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            _list.m_Safety = CollectionHelper.CreateSafetyHandle(ListPool.Pool.Data.Allocator);
            CollectionHelper.SetStaticSafetyId<NativeList<T>>(ref _list.m_Safety, ref NativeList<T>.s_staticSafetyId.Data);
            AtomicSafetyHandle.SetBumpSecondaryVersionOnScheduleWrite(_list.m_Safety, true);
#endif

            return this;
        }

        public NativeArray<T> AsArray(int length, NativeArrayOptions options = NativeArrayOptions.ClearMemory) 
        {
            _list.Resize(length, options);
            return _list.AsArray();
        }
        
        
        /// <summary>
        /// Disposes the PooledNativeList and returns the underlying memory to the thread-local pool for reuse.
        /// </summary>
        /// <remarks>
        /// This method clears the list contents and converts it back to a byte list for storage in the pool.
        /// The instance should not be used after disposal.
        /// </remarks>
        public void Dispose()
        {
            if (!_list.IsCreated)
            {
                return;
            }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckDeallocateAndThrow(_list.m_Safety);
            AtomicSafetyHandle.Release(_list.m_Safety);
#endif

            ListPool.ReturnUnsafeList(new Ref<UnsafeList<T>>(_list.m_ListData));
            _list = default;
        }
    }

    internal static class ListPool
    {
        // About 1MB per thread
        private const int MAX_POOL_SIZE_PER_THREAD = 16;
        private const int MAX_BYTES_PER_LIST = 64 * 1024;

        internal static readonly SharedStatic<Data> Pool = SharedStatic<Data>.GetOrCreate<Data>();

        internal static unsafe Ref<UnsafeList<T>> RentUnsafeList<T>(int minCapacity)
            where T : unmanaged
        {
            ref var data = ref Pool.Data;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (!data.IsCreated)
            {
                throw new InvalidOperationException("PooledNativeList pool not initialized.");
            }
#endif

            ref var lp = ref data.GetThreadList();
            if (lp.Length == 0)
            {
                var listData = AllocatorManager.Allocate<UnsafeList<T>>(Pool.Data.Allocator);
                *listData = new UnsafeList<T>(minCapacity, data.Allocator);
                return new Ref<UnsafeList<T>>(listData);
            }

            var byteList = lp[^1];
            lp.RemoveAt(lp.Length - 1);
            
            var list = UnsafeUtility.As<Ref<UnsafeList<byte>>, Ref<UnsafeList<T>>>(ref byteList);
            var capacity = byteList.Value.Capacity / UnsafeUtility.SizeOf<T>();
            if (capacity < minCapacity)
            {
                list.Value.Capacity = minCapacity;
            }
            else
            {
                list.Value.m_capacity = capacity;
            }

            list.Value.m_length = 0;
            return list;
        }
        
        internal static unsafe void ReturnUnsafeList<T>(Ref<UnsafeList<T>> list)
            where T : unmanaged
        {
            ref var refByteList = ref UnsafeUtility.As<Ref<UnsafeList<T>>, Ref<UnsafeList<byte>>>(ref list);
            ref var byteList = ref refByteList.Value;
            byteList.Clear();

            byteList.m_capacity = list.Value.Capacity * UnsafeUtility.SizeOf<T>();
            byteList.m_length = 0;

            ref var lp = ref Pool.Data.GetThreadList();
            if (lp.Length < MAX_POOL_SIZE_PER_THREAD && byteList.Capacity < MAX_BYTES_PER_LIST)
            {
                lp.Add(refByteList);
            }
            else
            {
                byteList.Dispose();
                AllocatorManager.Free(Pool.Data.Allocator, refByteList.GetUnsafePtr());
            }
        }

        /// <summary>
        /// Initializes the global pool data structure used by all PooledNativeList instances.
        /// </summary>
        /// <remarks>
        /// This method is called automatically during Unity initialization and should not be called manually.
        /// Creates thread-local storage for each worker thread to avoid contention.
        /// </remarks>
#if !UNITY_EDITOR
        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
#endif
        public static void Initialize()
        {
            if (Pool.Data.IsCreated)
            {
                return;
            }

            Pool.Data = new Data(Allocator.Domain);
        }

        internal struct Data
        {
            public AllocatorManager.AllocatorHandle Allocator;
            private NativeThreadData<ThreadData> _buffer;

            public Data(AllocatorManager.AllocatorHandle allocator)
            {
                Allocator = allocator;
                _buffer = new NativeThreadData<ThreadData>(allocator);
                
                for (var i = 0; i < _buffer.Length; i++)
                {
                    _buffer.GetThreadDataRef(i).ThreadList = new UnsafeList<Ref<UnsafeList<byte>>>(0, allocator);
                }
            }

            public readonly bool IsCreated => _buffer.IsCreated;

            public ref UnsafeList<Ref<UnsafeList<byte>>> GetThreadList()
            {
#if UNITY_EDITOR
                UnityEngine.Debug.Assert(JobsUtility.IsExecutingJob || UnityEditorInternal.InternalEditorUtility.CurrentThreadIsMainThread(),
                    "Can only be used on main or worker threads");
#endif
                return ref _buffer.GetThreadDataRef(JobsUtility.ThreadIndex).ThreadList;
            }

            public void Dispose()
            {
                if (!IsCreated)
                {
                    return;
                }

                foreach (var threadData in _buffer)
                {
                    foreach (var list in threadData.ThreadList)
                    {
                        list.Value.Dispose();
                    }
                    threadData.ThreadList.Dispose();
                }
                _buffer = default;
            }
        }

        private struct ThreadData
        {
            public UnsafeList<Ref<UnsafeList<byte>>> ThreadList;
        }
    }
}
