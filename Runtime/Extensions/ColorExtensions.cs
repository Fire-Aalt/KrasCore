using Unity.Mathematics;
using UnityEngine;

namespace KrasCore
{
    public static class ColorExtensions
    {
        public static float3 AsFloat3(this Color color)
        {
            return new float3(color.r, color.g, color.b);
        }
        
        public static float4 AsFloat4(this Color color)
        {
            return new float4(color.r, color.g, color.b, color.a);
        }
    }
}