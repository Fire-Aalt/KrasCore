using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;

namespace KrasCore
{
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct UnsafePerThreadData<T> : INativeDisposable
        where T : unmanaged
    {
        // Cache line aligned storage avoids false sharing between worker threads.
        public const int PER_THREAD_DATA_SIZE = JobsUtility.CacheLineSize;

        [NativeDisableUnsafePtrRestriction] private byte* perThreadData;

        private int perThreadDataStride;
        private AllocatorManager.AllocatorHandle allocator;

        public bool IsCreated;

        public UnsafePerThreadData(AllocatorManager.AllocatorHandle allocator)
        {
            this = default;
            Initialize(allocator);
        }

        private void Initialize(AllocatorManager.AllocatorHandle allocatorHandle)
        {
            allocator = allocatorHandle;
            perThreadDataStride = CalculatePerThreadDataStride();

            int maxThreadCount = JobsUtility.ThreadIndexCount;
            int align = UnsafeUtility.AlignOf<T>();
            int allocationSize = perThreadDataStride * maxThreadCount;

            perThreadData = (byte*)UnsafeUtility.Malloc(allocationSize, align, allocatorHandle.ToAllocator);
            UnsafeUtility.MemClear(perThreadData, allocationSize);

            IsCreated = true;
        }

        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(AllocatorManager.AllocatorHandle) })]
        internal static UnsafePerThreadData<T>* Create<TAllocator>(ref TAllocator allocator, NativeArrayOptions options = NativeArrayOptions.UninitializedMemory)
            where TAllocator : unmanaged, AllocatorManager.IAllocator
        {
            UnsafePerThreadData<T>* unsafePerThreadData = allocator.Allocate(default(UnsafePerThreadData<T>), 1);
            *unsafePerThreadData = new UnsafePerThreadData<T>(allocator.Handle);

            return unsafePerThreadData;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int CalculatePerThreadDataStride()
        {
            int dataSize = UnsafeUtility.SizeOf<T>();
            int roundedToCacheLine = ((dataSize + PER_THREAD_DATA_SIZE - 1) / PER_THREAD_DATA_SIZE) * PER_THREAD_DATA_SIZE;
            int minCacheLineSize = roundedToCacheLine < PER_THREAD_DATA_SIZE ? PER_THREAD_DATA_SIZE : roundedToCacheLine;

            int alignment = UnsafeUtility.AlignOf<T>();
            return ((minCacheLineSize + alignment - 1) / alignment) * alignment;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T* GetUnsafeThreadData(int threadIndex)
        {
            return (T*)(perThreadData + threadIndex * perThreadDataStride);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte* GetPerThreadDataPtr()
        {
            return perThreadData;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetPerThreadDataStride()
        {
            return perThreadDataStride;
        }

        public void Clear()
        {
            if (!IsCreated)
                return;

            UnsafeUtility.MemClear(perThreadData, perThreadDataStride * JobsUtility.ThreadIndexCount);
        }

        public void Clear(in T value)
        {
            if (!IsCreated)
                return;

            for (int i = 0; i < JobsUtility.ThreadIndexCount; i++)
                *GetUnsafeThreadData(i) = value;
        }

        public ThreadReader AsThreadReader()
        {
            return new ThreadReader(ref this);
        }

        public ThreadWriter AsThreadWriter()
        {
            return new ThreadWriter(ref this);
        }

        public static void Destroy(UnsafePerThreadData<T>* unsafePerThreadData)
        {
            var allocator = unsafePerThreadData->allocator;
            unsafePerThreadData->Dispose();
            AllocatorManager.Free(allocator, unsafePerThreadData);
        }

        public void Dispose()
        {
            if (!IsCreated)
                return;

            UnsafeUtility.Free(perThreadData, allocator.ToAllocator);
            perThreadData = null;
            perThreadDataStride = 0;
            allocator = Allocator.None;
            IsCreated = false;
        }

        [BurstCompile]
        private struct DisposeJob : IJob
        {
            public UnsafePerThreadData<T> Data;

            public void Execute()
            {
                Data.Dispose();
            }
        }

        public JobHandle Dispose(JobHandle inputDeps)
        {
            return new DisposeJob
            {
                Data = this
            }.Schedule(inputDeps);
        }

        public struct ThreadWriter
        {
            [NativeDisableUnsafePtrRestriction] private readonly byte* perThreadDataPtr;
            [NativeSetThreadIndex] private int threadIndex;

            private readonly int threadDataStride;

            internal ThreadWriter(ref UnsafePerThreadData<T> data)
            {
                perThreadDataPtr = data.perThreadData;
                threadDataStride = data.perThreadDataStride;

                threadIndex = 0;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Set(in T data)
            {
                *GetThreadDataPtr(threadIndex) = data;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ref T GetRef()
            {
                return ref UnsafeUtility.AsRef<T>(GetThreadDataPtr(threadIndex));
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private T* GetThreadDataPtr(int index)
            {
                return (T*)(perThreadDataPtr + index * threadDataStride);
            }
        }

        public struct ThreadReader
        {
            [NativeDisableUnsafePtrRestriction] private readonly byte* perThreadDataPtr;
            [NativeSetThreadIndex] private int threadIndex;

            private readonly int threadDataStride;

            internal ThreadReader(ref UnsafePerThreadData<T> data)
            {
                perThreadDataPtr = data.perThreadData;
                threadDataStride = data.perThreadDataStride;

                threadIndex = 0;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public T Get()
            {
                return *GetThreadDataPtr(threadIndex);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private T* GetThreadDataPtr(int index)
            {
                return (T*)(perThreadDataPtr + index * threadDataStride);
            }
        }
    }
}
