using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs.LowLevel.Unsafe;

namespace KrasCore.Pooling
{
    /// <summary>
    /// A pooled wrapper around NativeList that reuses allocated memory across instances to reduce allocation pressure.
    /// Uses thread-local pools to avoid contention in multi-threaded scenarios.
    /// </summary>
    /// <typeparam name="T">The unmanaged element type.</typeparam>
    public unsafe struct PooledNativeList<T> : IDisposable
        where T : unmanaged
    {
        private NativeList<T> _list;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        private AtomicSafetyHandle _oldHandle;
#endif

        /// <summary>
        /// Gets the underlying NativeList instance.
        /// </summary>
        /// <value>The wrapped NativeList that can be used for all list operations.</value>
        public NativeList<T> List => _list;

        private PooledNativeList<T> Create()
        {
            ref var data = ref PooledNativeList.Pool.Data;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (!data.IsCreated)
            {
                throw new InvalidOperationException("PooledNativeList pool not initialized.");
            }
#endif

            ref var lp = ref PooledNativeList.Pool.Data.GetThreadList();
            if (lp.Length == 0)
            {
                // Nothing in the pool, just create a new one
                _list = new NativeList<T>(0, data.Allocator);
            }
            else
            {
                // Pop an existing list out
                var byteList = lp[^1];
                lp.RemoveAt(lp.Length - 1);

                _list = UnsafeUtility.As<NativeList<byte>, NativeList<T>>(ref byteList);
                _list.m_ListData->m_capacity = byteList.Capacity / UnsafeUtility.SizeOf<T>();
            }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            // Replace our safety as it's not valid within the job as we've stored these inside another container so can't be injected
            _oldHandle = _list.m_Safety;
            _list.m_Safety = AtomicSafetyHandle.Create();
#endif

            return this;
        }

        /// <summary>
        /// Creates a new PooledNativeList instance, either from the thread-local pool or by allocating a new one.
        /// </summary>
        /// <returns>A PooledNativeList instance ready for use.</returns>
        /// <remarks>
        /// This method is thread-safe and will reuse previously disposed instances when available.
        /// The returned instance must be disposed to return it to the pool.
        /// </remarks>
        public static PooledNativeList<T> Make()
        {
            return default(PooledNativeList<T>).Create();
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

            ref var lp = ref PooledNativeList.Pool.Data.GetThreadList();

            _list.Clear();

            // Convert back to a byte list
            ref var byteList = ref UnsafeUtility.As<NativeList<T>, NativeList<byte>>(ref _list);
            byteList.m_ListData->m_capacity = _list.Capacity * UnsafeUtility.SizeOf<T>();

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            // Release the Temp handle
            AtomicSafetyHandle.CheckDeallocateAndThrow(byteList.m_Safety);
            AtomicSafetyHandle.Release(byteList.m_Safety);
            byteList.m_Safety = _oldHandle;
#endif

            // Only add back to pool if we haven't exceeded the max size
            if (lp.Length < PooledNativeList.MAX_POOL_SIZE_PER_THREAD)
            {
                lp.Add(byteList);
            }
            else
            {
                // Pool is full, dispose the list instead
                byteList.Dispose();
            }

            _list = default;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            _oldHandle = default;
#endif
        }
    }

    internal static class PooledNativeList
    {
        internal const int MAX_POOL_SIZE_PER_THREAD = 8;
        internal static readonly SharedStatic<Data> Pool = SharedStatic<Data>.GetOrCreate<Data>();

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
                    _buffer.GetThreadDataRef(i).ThreadList = new UnsafeList<NativeList<byte>>(0, allocator);
                }
            }

            public readonly bool IsCreated => _buffer.IsCreated;

            public ref UnsafeList<NativeList<byte>> GetThreadList()
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
                        list.Dispose();
                    }
                    threadData.ThreadList.Dispose();
                }
                _buffer = default;
            }
        }

        private struct ThreadData
        {
            public UnsafeList<NativeList<byte>> ThreadList;
        }
    }
}