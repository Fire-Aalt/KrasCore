using System;
using KrasCore.NZCore;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace ProceduralGeneration
{
    public unsafe struct UnsafeParallelListPtrWrapper<T> : IDisposable
        where T : unmanaged
    {
        [NativeDisableUnsafePtrRestriction] public UnsafeParallelList<T>* Ptr;
        
        public UnsafeParallelListPtrWrapper(int initialCapacity, AllocatorManager.AllocatorHandle allocator)
        {
            this = default;
            AllocatorManager.AllocatorHandle temp = allocator;
            Initialize(initialCapacity, ref temp);
        }
        
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(AllocatorManager.AllocatorHandle) })]
        private void Initialize<TAllocator>(int initialCapacity, ref TAllocator allocator) where TAllocator : unmanaged, AllocatorManager.IAllocator
        {
            Ptr = UnsafeParallelList<T>.Create(initialCapacity, ref allocator);
        }
        
        public void Dispose()
        {
            UnsafeParallelList<T>.Destroy(Ptr);
            Ptr = null;
        }
    }
}