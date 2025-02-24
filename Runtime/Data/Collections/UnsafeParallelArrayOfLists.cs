using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs.LowLevel.Unsafe;
using SpinLock = Unity.Entities.LowLevel.SpinLock;

namespace KrasCore
{
    [NativeContainer]
    [StructLayout(LayoutKind.Sequential)]
    [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(int) })]
    public unsafe struct UnsafeParallelArrayOfLists<T> : IDisposable
        where T : unmanaged, IEquatable<T>
    {
        [NativeDisableUnsafePtrRestriction]
        public byte* Ptr;
        
        [NativeDisableUnsafePtrRestriction]
        public T* Buffer;

        [NativeDisableUnsafePtrRestriction]
        public int* Lengths;
        
        [NativeDisableUnsafePtrRestriction]
        private SpinLock* _locks;

        public int InternalListCapacity;

        private AtomicSafetyHandle m_Safety;
        private int m_SafetyIndexHint;
        private static readonly SharedStatic<int> s_staticSafetyId = SharedStatic<int>.GetOrCreate<UnsafeParallelArrayOfLists<T>>();
        
        private AllocatorManager.AllocatorHandle _allocator;
        private int _sizeInBytes;

        public UnsafeParallelArrayOfLists(int internalListsCount, int internalListCapacity, AllocatorManager.AllocatorHandle allocator, NativeArrayOptions options = NativeArrayOptions.ClearMemory)
        {
            Allocate(internalListsCount, internalListCapacity, allocator, out this);
            
            UnsafeUtility.MemClear(Lengths, internalListsCount * sizeof(int));
            UnsafeUtility.MemClear(_locks, internalListsCount * sizeof(SpinLock));
            
            if (options != NativeArrayOptions.ClearMemory)
            {
                return;
            }

            UnsafeUtility.MemClear(Buffer, internalListsCount * internalListCapacity * (long)UnsafeUtility.SizeOf<T>());
        }

        public ParallelWriter AsParallelWriter() =>
            new((UnsafeParallelArrayOfLists<T>*)UnsafeUtility.AddressOf(ref this), ref m_Safety);

        public ParallelRemover AsParallelRemover() =>
            new((UnsafeParallelArrayOfLists<T>*)UnsafeUtility.AddressOf(ref this), ref m_Safety);
        
        public Enumerator GetEnumerator(int listIndex)
        {
            return new Enumerator(this, listIndex);
        }
        
        private static void Allocate(int internalListsCount, int internalListCapacity, AllocatorManager.AllocatorHandle allocator, out UnsafeParallelArrayOfLists<T> array)
        {
            var length = internalListsCount * internalListCapacity;
            CheckAllocateArguments(length, allocator);
            array = default;
            
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            CollectionHelper.CheckAllocator(allocator.Handle);

            array.m_Safety = CollectionHelper.CreateSafetyHandle(allocator.Handle);
            CollectionHelper.InitNativeContainer<T>(array.m_Safety);

            CollectionHelper.SetStaticSafetyId<NativeList<T>>(ref array.m_Safety, ref s_staticSafetyId.Data);

            array.m_SafetyIndexHint = (allocator.Handle).AddSafetyHandle(array.m_Safety);

            AtomicSafetyHandle.SetBumpSecondaryVersionOnScheduleWrite(array.m_Safety, true);
#endif
            var bufferSize = UnsafeUtility.SizeOf<T>() * length;
            var lengthsSize = sizeof(int) * internalListsCount;
            var locksSize = sizeof(SpinLock) * internalListsCount;
            array._sizeInBytes = bufferSize + lengthsSize + locksSize;
            
            array.Ptr = (byte*)allocator.Allocate(sizeof(byte), JobsUtility.CacheLineSize, array._sizeInBytes);
            array.Buffer = (T*)(array.Ptr);
            array.Lengths = (int*)(array.Ptr + bufferSize);
            array._locks = (SpinLock*)(array.Ptr + bufferSize + lengthsSize);
            
            array._allocator = allocator;
            array.InternalListCapacity = internalListCapacity;
        }
        
        public T* GetListData(int listIndex)
        {
            return Buffer + listIndex * InternalListCapacity;
        }
        
        [WriteAccessRequired]
        public void Dispose()
        {
            if ((IntPtr)Buffer == IntPtr.Zero)
            {
                throw new ObjectDisposedException("The UnsafeArray is already disposed.");
            }

            if (_allocator == Allocator.Invalid)
            {
                throw new InvalidOperationException("The UnsafeArray can not be Disposed because it was not allocated with a valid allocator.");
            }

            if (_allocator > Allocator.None)
            {
                _allocator.Free(Ptr, _sizeInBytes);
                Ptr = null;
                Buffer = null;
                Lengths = null;
                _locks = null;
            }

            Buffer = null;
        }
        
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        private static void CheckAllocateArguments(int length, AllocatorManager.AllocatorHandle allocator)
        {
            if (allocator <= Allocator.None)
            {
                throw new ArgumentException("Allocator must be Temp, TempJob or Persistent", nameof(allocator));
            }

            if (allocator >= Allocator.FirstUserIndex)
            {
                throw new ArgumentException("Use CollectionHelper.CreateUnsafeArray for custom allocator", nameof(allocator));
            }

            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length), "Length must be >= 0");
            }
        }
        
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckHasEnoughCapacity(int length, int index)
        {
            if (InternalListCapacity < index + length)
            {
                throw new InvalidOperationException($"AddNoResize assumes that list capacity is sufficient (Capacity {InternalListCapacity}, Length {length}), requested length {length}!");
            }
        }
        
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckNotOutOfBounds(int length, int index)
        {
            if (index < 0 || index >= length)
            {
                throw new InvalidOperationException($"RemoveAtSwapBack assumes that index is valid (Index {index}, Length {length})!");
            }
        }
        
        public struct Enumerator : IEnumerator<T>
        {
            private readonly T* _ptr;
            private readonly int _count;
            private int _index;

            internal Enumerator(UnsafeParallelArrayOfLists<T> array, int listIndex)
            {
                _ptr = array.Buffer + listIndex * array.InternalListCapacity;
                _count = array.Lengths[listIndex];
                _index = -1;
            }

            public T Current => _ptr[_index];

            public bool MoveNext()
            {
                _index++;
                return _index < _count;
            }

            public void Reset()
            {
                _index = -1;
            }

            object IEnumerator.Current => Current;

            public void Dispose()
            {
            }
        }
        
        [NativeContainer]
        [NativeContainerIsAtomicWriteOnly]
        public struct ParallelWriter
        {
            [NativeDisableUnsafePtrRestriction]
            public UnsafeParallelArrayOfLists<T>* Array;
            
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            private AtomicSafetyHandle m_Safety;
            private static readonly SharedStatic<int> s_staticSafetyId = SharedStatic<int>.GetOrCreate<ParallelWriter>();

            [GenerateTestsForBurstCompatibility(CompileTarget = GenerateTestsForBurstCompatibilityAttribute.BurstCompatibleCompileTarget.Editor)]
            internal ParallelWriter(UnsafeParallelArrayOfLists<T>* array, ref AtomicSafetyHandle safety)
            {
                Array = array;
                m_Safety = safety;
                CollectionHelper.SetStaticSafetyId<ParallelWriter>(ref m_Safety, ref s_staticSafetyId.Data);
            }
#else
            internal ParallelWriter(UnsafeParallelArrayOfLists<T>* array)
            {
                Array = array;
            }
#endif
            
            [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(int) })]
            public void AddNoResize(int listIndex, T value)
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
                var idx = Interlocked.Increment(ref UnsafeUtility.ArrayElementAsRef<int>(Array->Lengths, listIndex)) - 1;
                Array->CheckHasEnoughCapacity(idx, 1);
                UnsafeUtility.WriteArrayElement(Array->Buffer, listIndex * Array->InternalListCapacity + idx, value);
            }
        }
        
        [NativeContainer]
        [NativeContainerIsAtomicWriteOnly]
        public struct ParallelRemover
        {
            [NativeDisableUnsafePtrRestriction]
            public UnsafeParallelArrayOfLists<T>* Array;
            
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            private AtomicSafetyHandle m_Safety;
            private static readonly SharedStatic<int> s_staticSafetyId = SharedStatic<int>.GetOrCreate<ParallelWriter>();

            [GenerateTestsForBurstCompatibility(CompileTarget = GenerateTestsForBurstCompatibilityAttribute.BurstCompatibleCompileTarget.Editor)]
            internal ParallelRemover(UnsafeParallelArrayOfLists<T>* array, ref AtomicSafetyHandle safety)
            {
                Array = array;
                m_Safety = safety;
                CollectionHelper.SetStaticSafetyId<ParallelWriter>(ref m_Safety, ref s_staticSafetyId.Data);
            }
#else
            internal ParallelRemover(UnsafeParallelArrayOfLists<T>* array)
            {
                Array = array;
            }
#endif
            
            [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(int) })]
            public void Remove(int listIndex, T element)
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
                ref var spinLock = ref UnsafeUtility.ArrayElementAsRef<SpinLock>(Array->_locks, listIndex);
                spinLock.Acquire();

                try
                {
                    var baseIndex = listIndex * Array->InternalListCapacity;
                    ref int length = ref UnsafeUtility.ArrayElementAsRef<int>(Array->Lengths, listIndex);
                    var index = -1;
                    for (int i = 0; i < length; i++)
                    {
                        if (Array->Buffer[baseIndex + i].Equals(element))
                        {
                            index = i;
                            break;
                        }
                    }

                    if (index == -1)
                    {
                        return;
                    }

                    Array->CheckNotOutOfBounds(length, index);
                    length--;

                    if (index < length)
                    {
                        Array->Buffer[baseIndex + index] = Array->Buffer[baseIndex + length];
                    }
                }
                finally
                {
                    spinLock.Release();
                }
            }
        }
    }
}