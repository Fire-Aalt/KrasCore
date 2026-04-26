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
        T Zero();

        T Add(in T total, in T value);

        T Divide(in T total, int count);
    }

    public interface INativeEqualityComparer<T>
        where T : unmanaged
    {
        bool Equals(in T left, in T right);
    }

    public static partial class NativeLinq
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NativeEnumerable<T, TEnumerator> From<T, TEnumerator>(TEnumerator enumerator)
            where T : unmanaged
            where TEnumerator : unmanaged, IEnumerator<T>
        {
            return new NativeEnumerable<T, TEnumerator>(enumerator);
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
        public int Zero()
        {
            return 0;
        }

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

    public struct NativeFloatAccumulator : INativeAccumulator<float>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float Zero()
        {
            return 0f;
        }

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

    public struct NativeFloat2Accumulator : INativeAccumulator<float2>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float2 Zero()
        {
            return default;
        }

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
        public float3 Zero()
        {
            return default;
        }

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
        public float4 Zero()
        {
            return default;
        }

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

}


