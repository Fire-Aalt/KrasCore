using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Mathematics;

namespace KrasCore
{
    public static partial class NativeLinqExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static sbyte Sum<TEnumerator>(this Query<sbyte, TEnumerator> source)
            where TEnumerator : unmanaged, IEnumerator<sbyte>
        {
            return source.Sum<sbyte, TEnumerator, NativeSByteAccumulator>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte Sum<TEnumerator>(this Query<byte, TEnumerator> source)
            where TEnumerator : unmanaged, IEnumerator<byte>
        {
            return source.Sum<byte, TEnumerator, NativeByteAccumulator>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static short Sum<TEnumerator>(this Query<short, TEnumerator> source)
            where TEnumerator : unmanaged, IEnumerator<short>
        {
            return source.Sum<short, TEnumerator, NativeShortAccumulator>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort Sum<TEnumerator>(this Query<ushort, TEnumerator> source)
            where TEnumerator : unmanaged, IEnumerator<ushort>
        {
            return source.Sum<ushort, TEnumerator, NativeUShortAccumulator>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Sum<TEnumerator>(this Query<int, TEnumerator> source)
            where TEnumerator : unmanaged, IEnumerator<int>
        {
            return source.Sum<int, TEnumerator, NativeIntAccumulator>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint Sum<TEnumerator>(this Query<uint, TEnumerator> source)
            where TEnumerator : unmanaged, IEnumerator<uint>
        {
            return source.Sum<uint, TEnumerator, NativeUIntAccumulator>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long Sum<TEnumerator>(this Query<long, TEnumerator> source)
            where TEnumerator : unmanaged, IEnumerator<long>
        {
            return source.Sum<long, TEnumerator, NativeLongAccumulator>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong Sum<TEnumerator>(this Query<ulong, TEnumerator> source)
            where TEnumerator : unmanaged, IEnumerator<ulong>
        {
            return source.Sum<ulong, TEnumerator, NativeULongAccumulator>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Sum<TEnumerator>(this Query<float, TEnumerator> source)
            where TEnumerator : unmanaged, IEnumerator<float>
        {
            return source.Sum<float, TEnumerator, NativeFloatAccumulator>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Sum<TEnumerator>(this Query<double, TEnumerator> source)
            where TEnumerator : unmanaged, IEnumerator<double>
        {
            return source.Sum<double, TEnumerator, NativeDoubleAccumulator>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int2 Sum<TEnumerator>(this Query<int2, TEnumerator> source)
            where TEnumerator : unmanaged, IEnumerator<int2>
        {
            return source.Sum<int2, TEnumerator, NativeInt2Accumulator>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int3 Sum<TEnumerator>(this Query<int3, TEnumerator> source)
            where TEnumerator : unmanaged, IEnumerator<int3>
        {
            return source.Sum<int3, TEnumerator, NativeInt3Accumulator>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int4 Sum<TEnumerator>(this Query<int4, TEnumerator> source)
            where TEnumerator : unmanaged, IEnumerator<int4>
        {
            return source.Sum<int4, TEnumerator, NativeInt4Accumulator>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint2 Sum<TEnumerator>(this Query<uint2, TEnumerator> source)
            where TEnumerator : unmanaged, IEnumerator<uint2>
        {
            return source.Sum<uint2, TEnumerator, NativeUInt2Accumulator>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint3 Sum<TEnumerator>(this Query<uint3, TEnumerator> source)
            where TEnumerator : unmanaged, IEnumerator<uint3>
        {
            return source.Sum<uint3, TEnumerator, NativeUInt3Accumulator>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint4 Sum<TEnumerator>(this Query<uint4, TEnumerator> source)
            where TEnumerator : unmanaged, IEnumerator<uint4>
        {
            return source.Sum<uint4, TEnumerator, NativeUInt4Accumulator>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 Sum<TEnumerator>(this Query<float2, TEnumerator> source)
            where TEnumerator : unmanaged, IEnumerator<float2>
        {
            return source.Sum<float2, TEnumerator, NativeFloat2Accumulator>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 Sum<TEnumerator>(this Query<float3, TEnumerator> source)
            where TEnumerator : unmanaged, IEnumerator<float3>
        {
            return source.Sum<float3, TEnumerator, NativeFloat3Accumulator>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4 Sum<TEnumerator>(this Query<float4, TEnumerator> source)
            where TEnumerator : unmanaged, IEnumerator<float4>
        {
            return source.Sum<float4, TEnumerator, NativeFloat4Accumulator>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double2 Sum<TEnumerator>(this Query<double2, TEnumerator> source)
            where TEnumerator : unmanaged, IEnumerator<double2>
        {
            return source.Sum<double2, TEnumerator, NativeDouble2Accumulator>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double3 Sum<TEnumerator>(this Query<double3, TEnumerator> source)
            where TEnumerator : unmanaged, IEnumerator<double3>
        {
            return source.Sum<double3, TEnumerator, NativeDouble3Accumulator>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double4 Sum<TEnumerator>(this Query<double4, TEnumerator> source)
            where TEnumerator : unmanaged, IEnumerator<double4>
        {
            return source.Sum<double4, TEnumerator, NativeDouble4Accumulator>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static sbyte Average<TEnumerator>(this Query<sbyte, TEnumerator> source)
            where TEnumerator : unmanaged, IEnumerator<sbyte>
        {
            return source.Average<sbyte, TEnumerator, NativeSByteAccumulator>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte Average<TEnumerator>(this Query<byte, TEnumerator> source)
            where TEnumerator : unmanaged, IEnumerator<byte>
        {
            return source.Average<byte, TEnumerator, NativeByteAccumulator>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static short Average<TEnumerator>(this Query<short, TEnumerator> source)
            where TEnumerator : unmanaged, IEnumerator<short>
        {
            return source.Average<short, TEnumerator, NativeShortAccumulator>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort Average<TEnumerator>(this Query<ushort, TEnumerator> source)
            where TEnumerator : unmanaged, IEnumerator<ushort>
        {
            return source.Average<ushort, TEnumerator, NativeUShortAccumulator>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Average<TEnumerator>(this Query<int, TEnumerator> source)
            where TEnumerator : unmanaged, IEnumerator<int>
        {
            return source.Average<int, TEnumerator, NativeIntAccumulator>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint Average<TEnumerator>(this Query<uint, TEnumerator> source)
            where TEnumerator : unmanaged, IEnumerator<uint>
        {
            return source.Average<uint, TEnumerator, NativeUIntAccumulator>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long Average<TEnumerator>(this Query<long, TEnumerator> source)
            where TEnumerator : unmanaged, IEnumerator<long>
        {
            return source.Average<long, TEnumerator, NativeLongAccumulator>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong Average<TEnumerator>(this Query<ulong, TEnumerator> source)
            where TEnumerator : unmanaged, IEnumerator<ulong>
        {
            return source.Average<ulong, TEnumerator, NativeULongAccumulator>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Average<TEnumerator>(this Query<float, TEnumerator> source)
            where TEnumerator : unmanaged, IEnumerator<float>
        {
            return source.Average<float, TEnumerator, NativeFloatAccumulator>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Average<TEnumerator>(this Query<double, TEnumerator> source)
            where TEnumerator : unmanaged, IEnumerator<double>
        {
            return source.Average<double, TEnumerator, NativeDoubleAccumulator>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int2 Average<TEnumerator>(this Query<int2, TEnumerator> source)
            where TEnumerator : unmanaged, IEnumerator<int2>
        {
            return source.Average<int2, TEnumerator, NativeInt2Accumulator>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int3 Average<TEnumerator>(this Query<int3, TEnumerator> source)
            where TEnumerator : unmanaged, IEnumerator<int3>
        {
            return source.Average<int3, TEnumerator, NativeInt3Accumulator>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int4 Average<TEnumerator>(this Query<int4, TEnumerator> source)
            where TEnumerator : unmanaged, IEnumerator<int4>
        {
            return source.Average<int4, TEnumerator, NativeInt4Accumulator>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint2 Average<TEnumerator>(this Query<uint2, TEnumerator> source)
            where TEnumerator : unmanaged, IEnumerator<uint2>
        {
            return source.Average<uint2, TEnumerator, NativeUInt2Accumulator>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint3 Average<TEnumerator>(this Query<uint3, TEnumerator> source)
            where TEnumerator : unmanaged, IEnumerator<uint3>
        {
            return source.Average<uint3, TEnumerator, NativeUInt3Accumulator>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint4 Average<TEnumerator>(this Query<uint4, TEnumerator> source)
            where TEnumerator : unmanaged, IEnumerator<uint4>
        {
            return source.Average<uint4, TEnumerator, NativeUInt4Accumulator>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 Average<TEnumerator>(this Query<float2, TEnumerator> source)
            where TEnumerator : unmanaged, IEnumerator<float2>
        {
            return source.Average<float2, TEnumerator, NativeFloat2Accumulator>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 Average<TEnumerator>(this Query<float3, TEnumerator> source)
            where TEnumerator : unmanaged, IEnumerator<float3>
        {
            return source.Average<float3, TEnumerator, NativeFloat3Accumulator>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4 Average<TEnumerator>(this Query<float4, TEnumerator> source)
            where TEnumerator : unmanaged, IEnumerator<float4>
        {
            return source.Average<float4, TEnumerator, NativeFloat4Accumulator>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double2 Average<TEnumerator>(this Query<double2, TEnumerator> source)
            where TEnumerator : unmanaged, IEnumerator<double2>
        {
            return source.Average<double2, TEnumerator, NativeDouble2Accumulator>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double3 Average<TEnumerator>(this Query<double3, TEnumerator> source)
            where TEnumerator : unmanaged, IEnumerator<double3>
        {
            return source.Average<double3, TEnumerator, NativeDouble3Accumulator>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double4 Average<TEnumerator>(this Query<double4, TEnumerator> source)
            where TEnumerator : unmanaged, IEnumerator<double4>
        {
            return source.Average<double4, TEnumerator, NativeDouble4Accumulator>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Sum<T, TEnumerator, TAccumulator>(this Query<T, TEnumerator> source)
            where T : unmanaged
            where TEnumerator : unmanaged, IEnumerator<T>
            where TAccumulator : unmanaged, INativeAccumulator<T>
        {
            return NativeLinqUtilities.Sum<T, TEnumerator, TAccumulator>(source.GetEnumerator(), default);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Average<T, TEnumerator, TAccumulator>(this Query<T, TEnumerator> source)
            where T : unmanaged
            where TEnumerator : unmanaged, IEnumerator<T>
            where TAccumulator : unmanaged, INativeAccumulator<T>
        {
            return NativeLinqUtilities.Average<T, TEnumerator, TAccumulator>(source.GetEnumerator(), default);
        }
    }

    public partial struct Query<T, TEnumerator>
        where T : unmanaged
        where TEnumerator : unmanaged, IEnumerator<T>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Sum<TAccumulator>(TAccumulator accumulator)
            where TAccumulator : unmanaged, INativeAccumulator<T>
        {
            return NativeLinqUtilities.Sum<T, TEnumerator, TAccumulator>(GetEnumerator(), accumulator);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Average<TAccumulator>(TAccumulator accumulator)
            where TAccumulator : unmanaged, INativeAccumulator<T>
        {
            return NativeLinqUtilities.Average<T, TEnumerator, TAccumulator>(GetEnumerator(), accumulator);
        }
    }

    internal static partial class NativeLinqUtilities
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Sum<T, TEnumerator, TAccumulator>(TEnumerator enumerator, TAccumulator accumulator)
            where T : unmanaged
            where TEnumerator : unmanaged, IEnumerator<T>
            where TAccumulator : unmanaged, INativeAccumulator<T>
        {
            var total = default(T);
            while (enumerator.MoveNext())
            {
                var value = enumerator.Current;
                total = accumulator.Add(in total, in value);
            }

            enumerator.Dispose();
            return total;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Average<T, TEnumerator, TAccumulator>(TEnumerator enumerator, TAccumulator accumulator)
            where T : unmanaged
            where TEnumerator : unmanaged, IEnumerator<T>
            where TAccumulator : unmanaged, INativeAccumulator<T>
        {
            var total = default(T);
            var count = 0;
            while (enumerator.MoveNext())
            {
                var value = enumerator.Current;
                total = accumulator.Add(in total, in value);
                count++;
            }

            enumerator.Dispose();
            return count == 0 ? default : accumulator.Divide(in total, count);
        }
    }
}
