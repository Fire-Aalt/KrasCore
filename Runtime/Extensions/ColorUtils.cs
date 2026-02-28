using Unity.Mathematics;
using UnityEngine;

namespace KrasCore
{
    public static class ColorUtils
    {
        public static float3 AsFloat3(this Color color)
        {
            return new float3(color.r, color.g, color.b);
        }
        
        public static float4 AsFloat4(this Color color)
        {
            return new float4(color.r, color.g, color.b, color.a);
        }
        
        public static float3 GammaToLinearSpace(float3 value)
        {
            var low = value / 12.92f;
            var high = math.pow((value + 0.055f) / 1.055f, 2.4f);
            return math.select(low, high, value > 0.04045f);
        }
    }
}