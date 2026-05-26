using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace FireAlt.Core.Collections
{
    /// <summary>
    /// Entry point for renting pooled <see cref="UnsafeList{T}" /> wrappers.
    /// </summary>
    /// <typeparam name="T">The unmanaged element type.</typeparam>
    public struct UnsafeListPool<T>
        where T : unmanaged
    {
        /// <summary>
        /// Rents a pooled <see cref="UnsafeList{T}" />.
        /// </summary>
        /// <param name="minCapacity">The minimum element capacity required by the caller.</param>
        /// <returns>A pooled unsafe-list wrapper that must be disposed to return its backing allocation.</returns>
        public static PooledUnsafeList<T> Rent(int minCapacity = 0)
        {
            return default(PooledUnsafeList<T>).Create(minCapacity);
        }
    }
    
    /// <summary>
    /// Disposable owner for a rented <see cref="UnsafeList{T}" />.
    /// </summary>
    /// <typeparam name="T">The unmanaged element type.</typeparam>
    /// <remarks>
    /// The wrapper stores a reference to a pooled unsafe-list header. Mutating <see cref="List" /> updates that shared header
    /// directly.
    /// </remarks>
    public unsafe struct PooledUnsafeList<T> : IDisposable
        where T : unmanaged
    {
        private Ref<UnsafeList<T>> _list;

        /// <summary>
        /// Gets the rented unsafe list.
        /// </summary>
        /// <value>A reference to the pooled unsafe-list header.</value>
        public ref UnsafeList<T> List => ref _list.Value;
        
        internal PooledUnsafeList<T> Create(int minCapacity)
        {
            _list = ListPool.RentUnsafeList<T>(minCapacity);
            return this;
        }

        /// <summary>
        /// Resizes the rented list and returns an unsafe array view over its current buffer.
        /// </summary>
        /// <param name="length">The desired list length.</param>
        /// <param name="options">Whether newly exposed memory should be cleared.</param>
        /// <returns>An unsafe array aliasing the rented list buffer.</returns>
        public UnsafeArray<T> AsUnsafeArray(int length, NativeArrayOptions options = NativeArrayOptions.ClearMemory) 
        {
            List.Resize(length, options);
            return UnsafeArrayUtility.ConvertExistingDataToUnsafeArray<T>(List.Ptr, List.Length, Allocator.None);
        }
        
        /// <summary>
        /// Returns the rented unsafe list to the thread-local pool.
        /// </summary>
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
}