using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Mathematics;

namespace KrasCore
{
    public interface IPredicate<T>
        where T : unmanaged
    {
        bool Match(in T value);
    }

    public interface ISelector<TSource, out TResult>
        where TSource : unmanaged
        where TResult : unmanaged
    {
        TResult Select(in TSource value);
    }

    public interface INativeAccumulator<T>
        where T : unmanaged
    {
        T Add(in T total, in T value);

        T Divide(in T total, int count);
    }

    public interface INativeEqualityComparer<T>
        where T : unmanaged
    {
        bool Equals(in T left, in T right);
    }

    public static class NativeLinq
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NativeQuery<T, TEnumerator> From<T, TEnumerator>(TEnumerator enumerator)
            where T : unmanaged
            where TEnumerator : unmanaged, IEnumerator<T>
        {
            return new NativeQuery<T, TEnumerator>(enumerator);
        }
    }

    public struct NativeAscendingComparer<T> : IComparer<T>
        where T : unmanaged, IComparable<T>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Compare(T x, T y)
        {
            return x.CompareTo(y);
        }
    }

    public struct NativeDescendingComparer<T> : IComparer<T>
        where T : unmanaged, IComparable<T>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Compare(T x, T y)
        {
            return y.CompareTo(x);
        }
    }

    public struct NativeEqualityComparer<T> : INativeEqualityComparer<T>
        where T : unmanaged, IEquatable<T>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(in T left, in T right)
        {
            return left.Equals(right);
        }
    }

    public struct NativeIntAccumulator : INativeAccumulator<int>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Add(in int total, in int value)
        {
            return total + value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Divide(in int total, int count)
        {
            return total / count;
        }
    }

    public struct NativeSByteAccumulator : INativeAccumulator<sbyte>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public sbyte Add(in sbyte total, in sbyte value)
        {
            return (sbyte)(total + value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public sbyte Divide(in sbyte total, int count)
        {
            return (sbyte)(total / count);
        }
    }

    public struct NativeByteAccumulator : INativeAccumulator<byte>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte Add(in byte total, in byte value)
        {
            return (byte)(total + value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte Divide(in byte total, int count)
        {
            return (byte)(total / count);
        }
    }

    public struct NativeShortAccumulator : INativeAccumulator<short>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public short Add(in short total, in short value)
        {
            return (short)(total + value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public short Divide(in short total, int count)
        {
            return (short)(total / count);
        }
    }

    public struct NativeUShortAccumulator : INativeAccumulator<ushort>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ushort Add(in ushort total, in ushort value)
        {
            return (ushort)(total + value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ushort Divide(in ushort total, int count)
        {
            return (ushort)(total / count);
        }
    }

    public struct NativeUIntAccumulator : INativeAccumulator<uint>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint Add(in uint total, in uint value)
        {
            return total + value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint Divide(in uint total, int count)
        {
            return total / (uint)count;
        }
    }

    public struct NativeLongAccumulator : INativeAccumulator<long>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long Add(in long total, in long value)
        {
            return total + value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long Divide(in long total, int count)
        {
            return total / count;
        }
    }

    public struct NativeULongAccumulator : INativeAccumulator<ulong>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong Add(in ulong total, in ulong value)
        {
            return total + value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong Divide(in ulong total, int count)
        {
            return total / (ulong)count;
        }
    }

    public struct NativeFloatAccumulator : INativeAccumulator<float>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float Add(in float total, in float value)
        {
            return total + value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float Divide(in float total, int count)
        {
            return total / count;
        }
    }

    public struct NativeDoubleAccumulator : INativeAccumulator<double>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double Add(in double total, in double value)
        {
            return total + value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double Divide(in double total, int count)
        {
            return total / count;
        }
    }

    public struct NativeInt2Accumulator : INativeAccumulator<int2>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int2 Add(in int2 total, in int2 value)
        {
            return total + value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int2 Divide(in int2 total, int count)
        {
            return total / count;
        }
    }

    public struct NativeInt3Accumulator : INativeAccumulator<int3>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int3 Add(in int3 total, in int3 value)
        {
            return total + value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int3 Divide(in int3 total, int count)
        {
            return total / count;
        }
    }

    public struct NativeInt4Accumulator : INativeAccumulator<int4>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int4 Add(in int4 total, in int4 value)
        {
            return total + value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int4 Divide(in int4 total, int count)
        {
            return total / count;
        }
    }

    public struct NativeUInt2Accumulator : INativeAccumulator<uint2>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint2 Add(in uint2 total, in uint2 value)
        {
            return total + value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint2 Divide(in uint2 total, int count)
        {
            return total / (uint)count;
        }
    }

    public struct NativeUInt3Accumulator : INativeAccumulator<uint3>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint3 Add(in uint3 total, in uint3 value)
        {
            return total + value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint3 Divide(in uint3 total, int count)
        {
            return total / (uint)count;
        }
    }

    public struct NativeUInt4Accumulator : INativeAccumulator<uint4>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint4 Add(in uint4 total, in uint4 value)
        {
            return total + value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint4 Divide(in uint4 total, int count)
        {
            return total / (uint)count;
        }
    }

    public struct NativeFloat2Accumulator : INativeAccumulator<float2>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float2 Add(in float2 total, in float2 value)
        {
            return total + value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float2 Divide(in float2 total, int count)
        {
            return total / count;
        }
    }

    public struct NativeFloat3Accumulator : INativeAccumulator<float3>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float3 Add(in float3 total, in float3 value)
        {
            return total + value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float3 Divide(in float3 total, int count)
        {
            return total / count;
        }
    }

    public struct NativeFloat4Accumulator : INativeAccumulator<float4>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float4 Add(in float4 total, in float4 value)
        {
            return total + value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float4 Divide(in float4 total, int count)
        {
            return total / count;
        }
    }

    public struct NativeDouble2Accumulator : INativeAccumulator<double2>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double2 Add(in double2 total, in double2 value)
        {
            return total + value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double2 Divide(in double2 total, int count)
        {
            return total / count;
        }
    }

    public struct NativeDouble3Accumulator : INativeAccumulator<double3>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double3 Add(in double3 total, in double3 value)
        {
            return total + value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double3 Divide(in double3 total, int count)
        {
            return total / count;
        }
    }

    public struct NativeDouble4Accumulator : INativeAccumulator<double4>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double4 Add(in double4 total, in double4 value)
        {
            return total + value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double4 Divide(in double4 total, int count)
        {
            return total / count;
        }
    }
}
