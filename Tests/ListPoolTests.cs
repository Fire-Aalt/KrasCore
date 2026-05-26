using System;
using System.Reflection;
using KrasCore.Collections;
using NUnit.Framework;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace KrasCore.Tests
{
    public class ListPoolTests
    {
        [SetUp]
        public void SetUp()
        {
            InitializeListPool();
        }

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

        private static void InitializeListPool()
        {
            var type = typeof(NativeListPool<int>).Assembly.GetType("KrasCore.Collections.ListPool", true);
            var method = type.GetMethod("Initialize", BindingFlags.Public | BindingFlags.Static);
            method.Invoke(null, null);
        }
    }
}
