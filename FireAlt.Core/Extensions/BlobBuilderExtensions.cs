using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

// https://gitlab.com/tertle/com.bovinelabs.core

namespace KrasCore
{
    public static unsafe class BlobBuilderExtensions
    {
        /// <summary>
        /// Allocates a <see cref="BlobArray{T}"/> inside <paramref name="builder"/> and copies the full contents of <paramref name="src"/> into it.
        /// </summary>
        public static void Construct<T>(this ref BlobBuilder builder, ref BlobArray<T> dest, in NativeArray<T> src) where T : unmanaged
        {
            var blobArr = builder.Allocate(ref dest, src.Length);

            // bulk-copy
            var dst = UnsafeUtility.AddressOf(ref blobArr[0]);
            var srcPtr = src.GetUnsafeReadOnlyPtr();
            var bytes  = (long)src.Length * UnsafeUtility.SizeOf<T>();

            UnsafeUtility.MemCpy(dst, srcPtr, bytes);
        }

        /// <summary>
        /// Allocates a <see cref="BlobArray{T}"/> inside <paramref name="builder"/> and copies the full contents of <paramref name="src"/> into it.
        /// </summary>
        public static void Construct<T>(this ref BlobBuilder builder, ref BlobArray<T> dest, in NativeList<T> src) where T : unmanaged
        {
            var list = src;
            builder.Construct(ref dest, list.AsArray());
        }
    }
}