using System;
using FireAlt.Core.Utility;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace FireAlt.Core.Collections
{
    /// <summary>
    /// Entry point for renting pooled <see cref="UnsafeArray{T}" /> views.
    /// </summary>
    /// <typeparam name="T">The unmanaged element type.</typeparam>
    public struct UnsafeArrayPool<T>
        where T : unmanaged
    {
        /// <summary>
        /// Rents a pooled <see cref="UnsafeArray{T}" />.
        /// </summary>
        /// <param name="length">The array length required by the caller.</param>
        /// <param name="options">Whether newly exposed memory should be cleared.</param>
        /// <returns>A pooled unsafe-array wrapper that must be disposed to return its backing allocation.</returns>
        /// <remarks>
        /// The wrapper owns the safety handle for the returned unsafe array view.
        /// </remarks>
        public static PooledUnsafeArray<T> Rent(int length, NativeArrayOptions options = NativeArrayOptions.ClearMemory)
        {
            return default(PooledUnsafeArray<T>).Create(length, options);
        }
    }
    
    /// <summary>
    /// Disposable owner for a rented <see cref="UnsafeArray{T}" /> view.
    /// </summary>
    /// <typeparam name="T">The unmanaged element type.</typeparam>
    /// <remarks>
    /// The unsafe array aliases a pooled unsafe-list buffer. Disposing the wrapper releases the array safety handle
    /// and returns the unsafe-list header and backing buffer to the shared pool.
    /// </remarks>
    public unsafe struct PooledUnsafeArray<T> : IDisposable
        where T : unmanaged
    {
        private Ref<UnsafeList<T>> _list;
        private UnsafeArray<T> _array;

        /// <summary>
        /// Gets the rented unsafe array.
        /// </summary>
        /// <value>The unsafe-array view over the pooled unsafe-list data.</value>
        public UnsafeArray<T> Array => _array;
        
        internal PooledUnsafeArray<T> Create(int length, NativeArrayOptions options = NativeArrayOptions.ClearMemory)
        {
            _array = default;
            _list = ListPool.RentUnsafeList<T>(length);
            _list.Value.Resize(length, options);

            _array = UnsafeArrayUtility.ConvertExistingDataToUnsafeArray<T>(_list.Value.Ptr, _list.Value.Length, Allocator.None);
            return this;
        }
        
        /// <summary>
        /// Returns the rented unsafe array backing storage to the thread-local pool.
        /// </summary>
        public void Dispose()
        {
            if (!_array.IsCreated)
            {
                return;
            }

            ListPool.ReturnUnsafeList(_list);
            _array = default;
        }
    }
}