using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEngine;

namespace KrasCore
{
    public static class MathematicsExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 AsVector4(this float2 a)
        {
            return new Vector4(a.x, a.y, 0, 0);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 AsVector4(this float4 a)
        {
            return new Vector4(a.x, a.y, a.z, a.w);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 AsFloat3(this float2 a, float z = 0f)
        {
            return new float3(a.x, a.y, z);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool2 ToBool2(this float2 value)
        {
            return new bool2(value.x == 1f, value.y == 1f);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 ToFloat2(this bool2 value)
        {
            return new float2(value.x ? 1f : 0f, value.y ? 1f : 0f);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 xz(this Vector3 vector) => new(vector.x, vector.z);
        
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 oxy(this float2 a) => new(0, a.x, a.y);
                
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 xoy(this float2 a) => new(a.x, 0, a.y);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 xyo(this float2 a) => new(a.x, a.y, 0);
    }
}