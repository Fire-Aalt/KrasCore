using System;
using FireAlt.Core.Collections;
using NUnit.Framework;
using Unity.Collections.LowLevel.Unsafe;

namespace FireAlt.Core.Tests
{
    public class ListPoolTests
    {
        [Test]
        public void PooledNativeList_AsArray_ResizesAndAliasesList()
        {
            var pooled = NativeListPool<int>.Rent(2);

            try
            {
                var array = pooled.AsArray(3);

                Assert.That(array.Length, Is.EqualTo(3));
                Assert.That(pooled.List.Length, Is.EqualTo(3));
                Assert.That(pooled.List.Capacity, Is.GreaterThanOrEqualTo(3));

                array[0] = 10;
                array[1] = 20;
                array[2] = 30;

                Assert.That(pooled.List[0], Is.EqualTo(10));
                Assert.That(pooled.List[1], Is.EqualTo(20));
                Assert.That(pooled.List[2], Is.EqualTo(30));
            }
            finally
            {
                pooled.Dispose();
            }
        }

        [Test]
        public void PooledUnsafeList_AsUnsafeArray_ResizesAndAliasesList()
        {
            var pooled = UnsafeListPool<int>.Rent(2);

            try
            {
                var array = pooled.AsUnsafeArray(3);

                Assert.That(array.Length, Is.EqualTo(3));
                Assert.That(pooled.List.Length, Is.EqualTo(3));
                Assert.That(pooled.List.Capacity, Is.GreaterThanOrEqualTo(3));

                array[0] = 40;
                array[1] = 50;
                array[2] = 60;

                Assert.That(pooled.List[0], Is.EqualTo(40));
                Assert.That(pooled.List[1], Is.EqualTo(50));
                Assert.That(pooled.List[2], Is.EqualTo(60));
            }
            finally
            {
                pooled.Dispose();
            }
        }

        [Test]
        public void PooledNativeArray_Array_HasRequestedLengthAndWritableElements()
        {
            var pooled = NativeArrayPool<int>.Rent(3);

            try
            {
                var array = pooled.Array;

                Assert.That(array.Length, Is.EqualTo(3));
                Assert.That(array[0], Is.Zero);
                Assert.That(array[1], Is.Zero);
                Assert.That(array[2], Is.Zero);

                array[0] = 70;
                array[1] = 80;
                array[2] = 90;

                Assert.That(pooled.Array[0], Is.EqualTo(70));
                Assert.That(pooled.Array[1], Is.EqualTo(80));
                Assert.That(pooled.Array[2], Is.EqualTo(90));
            }
            finally
            {
                pooled.Dispose();
            }
        }

        [Test]
        public void PooledNativeArray_ArrayCopies_AliasSameBuffer()
        {
            var pooled = NativeArrayPool<int>.Rent(2);

            try
            {
                var first = pooled.Array;
                var second = pooled.Array;

                first[0] = 100;
                second[1] = 200;

                Assert.That(second[0], Is.EqualTo(100));
                Assert.That(first[1], Is.EqualTo(200));
                Assert.That(pooled.Array[0], Is.EqualTo(100));
                Assert.That(pooled.Array[1], Is.EqualTo(200));
            }
            finally
            {
                pooled.Dispose();
            }
        }

        [Test]
        public void PooledUnsafeArray_Array_HasRequestedLengthAndWritableElements()
        {
            var pooled = UnsafeArrayPool<int>.Rent(3);

            try
            {
                var array = pooled.Array;

                Assert.That(array.Length, Is.EqualTo(3));
                Assert.That(array[0], Is.Zero);
                Assert.That(array[1], Is.Zero);
                Assert.That(array[2], Is.Zero);

                array[0] = 700;
                array[1] = 800;
                array[2] = 900;

                Assert.That(pooled.Array[0], Is.EqualTo(700));
                Assert.That(pooled.Array[1], Is.EqualTo(800));
                Assert.That(pooled.Array[2], Is.EqualTo(900));
            }
            finally
            {
                pooled.Dispose();
            }
        }

        [Test]
        public void PooledUnsafeArray_ArrayCopies_AliasSameBuffer()
        {
            var pooled = UnsafeArrayPool<int>.Rent(2);

            try
            {
                var first = pooled.Array;
                var second = pooled.Array;

                first[0] = 1000;
                second[1] = 2000;

                Assert.That(second[0], Is.EqualTo(1000));
                Assert.That(first[1], Is.EqualTo(2000));
                Assert.That(pooled.Array[0], Is.EqualTo(1000));
                Assert.That(pooled.Array[1], Is.EqualTo(2000));
            }
            finally
            {
                pooled.Dispose();
            }
        }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        [Test]
        public void PooledNativeArray_Dispose_InvalidatesArrayCopies()
        {
            var pooled = NativeArrayPool<int>.Rent(1);
            var array = pooled.Array;

            array[0] = 1234;
            pooled.Dispose();

            Assert.Throws<ObjectDisposedException>(() => _ = array[0]);
        }
#endif

        [Test]
        public unsafe void NativeThenUnsafe_RentsSameReturnedAllocationFromSharedPool()
        {
            var native = NativeListPool<int>.Rent(4);
            IntPtr nativeBuffer;

            try
            {
                native.List.Add(123);
                nativeBuffer = (IntPtr)native.List.GetUnsafeList()->Ptr;
            }
            finally
            {
                native.Dispose();
            }

            var unsafeList = UnsafeListPool<int>.Rent(1);

            try
            {
                Assert.That((IntPtr)unsafeList.List.Ptr, Is.EqualTo(nativeBuffer));
                Assert.That(unsafeList.List.Length, Is.Zero);
                Assert.That(unsafeList.List.Capacity, Is.GreaterThanOrEqualTo(1));
            }
            finally
            {
                unsafeList.Dispose();
            }
        }

        [Test]
        public unsafe void UnsafeThenNative_RentsSameReturnedAllocationFromSharedPool()
        {
            var unsafeList = UnsafeListPool<int>.Rent(4);
            IntPtr unsafeBuffer;

            try
            {
                unsafeList.List.Add(456);
                unsafeBuffer = (IntPtr)unsafeList.List.Ptr;
            }
            finally
            {
                unsafeList.Dispose();
            }

            var native = NativeListPool<int>.Rent(1);

            try
            {
                Assert.That((IntPtr)native.List.GetUnsafeList()->Ptr, Is.EqualTo(unsafeBuffer));
                Assert.That(native.List.Length, Is.Zero);
                Assert.That(native.List.Capacity, Is.GreaterThanOrEqualTo(1));
            }
            finally
            {
                native.Dispose();
            }
        }

        [Test]
        public unsafe void NativeArrayThenNativeList_RentsSameReturnedAllocationFromSharedPool()
        {
            var nativeArray = NativeArrayPool<int>.Rent(4);
            IntPtr arrayBuffer;

            try
            {
                var array = nativeArray.Array;
                array[0] = 321;
                arrayBuffer = (IntPtr)NativeArrayUnsafeUtility.GetUnsafePtr(array);
            }
            finally
            {
                nativeArray.Dispose();
            }

            var nativeList = NativeListPool<int>.Rent(1);

            try
            {
                Assert.That((IntPtr)nativeList.List.GetUnsafeList()->Ptr, Is.EqualTo(arrayBuffer));
                Assert.That(nativeList.List.Length, Is.Zero);
                Assert.That(nativeList.List.Capacity, Is.GreaterThanOrEqualTo(1));
            }
            finally
            {
                nativeList.Dispose();
            }
        }

        [Test]
        public unsafe void UnsafeListThenNativeArray_RentsSameReturnedAllocationFromSharedPool()
        {
            var unsafeList = UnsafeListPool<int>.Rent(4);
            IntPtr unsafeBuffer;

            try
            {
                unsafeList.List.Add(654);
                unsafeBuffer = (IntPtr)unsafeList.List.Ptr;
            }
            finally
            {
                unsafeList.Dispose();
            }

            var nativeArray = NativeArrayPool<int>.Rent(2);

            try
            {
                Assert.That((IntPtr)NativeArrayUnsafeUtility.GetUnsafePtr(nativeArray.Array), Is.EqualTo(unsafeBuffer));
                Assert.That(nativeArray.Array.Length, Is.EqualTo(2));
            }
            finally
            {
                nativeArray.Dispose();
            }
        }

        [Test]
        public unsafe void UnsafeArrayThenNativeArray_RentsSameReturnedAllocationFromSharedPool()
        {
            var unsafeArray = UnsafeArrayPool<int>.Rent(4);
            IntPtr unsafeArrayBuffer;

            try
            {
                var array = unsafeArray.Array;
                array[0] = 987;
                unsafeArrayBuffer = (IntPtr)array.GetUnsafePtr();
            }
            finally
            {
                unsafeArray.Dispose();
            }

            var nativeArray = NativeArrayPool<int>.Rent(2);

            try
            {
                Assert.That((IntPtr)NativeArrayUnsafeUtility.GetUnsafePtr(nativeArray.Array), Is.EqualTo(unsafeArrayBuffer));
                Assert.That(nativeArray.Array.Length, Is.EqualTo(2));
            }
            finally
            {
                nativeArray.Dispose();
            }
        }

        [Test]
        public unsafe void NativeListThenUnsafeArray_RentsSameReturnedAllocationFromSharedPool()
        {
            var nativeList = NativeListPool<int>.Rent(4);
            IntPtr nativeListBuffer;

            try
            {
                nativeList.List.Add(789);
                nativeListBuffer = (IntPtr)nativeList.List.GetUnsafeList()->Ptr;
            }
            finally
            {
                nativeList.Dispose();
            }

            var unsafeArray = UnsafeArrayPool<int>.Rent(2);

            try
            {
                Assert.That((IntPtr)unsafeArray.Array.GetUnsafePtr(), Is.EqualTo(nativeListBuffer));
                Assert.That(unsafeArray.Array.Length, Is.EqualTo(2));
            }
            finally
            {
                unsafeArray.Dispose();
            }
        }
    }
}
