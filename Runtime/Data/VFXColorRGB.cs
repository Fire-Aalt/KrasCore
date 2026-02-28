using Unity.Mathematics;
using UnityEngine;

namespace KrasCore
{
    public struct VFXColorRGB
    {
        public float3 Value;

        public VFXColorRGB(Color color)
        {
            //Value = color.linear.AsFloat3();
            Value = ToVFXColor(color);
        }
        
        public static implicit operator VFXColorRGB(Color color) => new(color);
        public static implicit operator Vector3(VFXColorRGB color) => color.Value;

        public static Vector3 ToVFXColor(float4 rgba)
        {
            return ColorUtils.GammaToLinearSpace(rgba.xyz);
        }
        
        public static Vector3 ToVFXColor(float3 rgb)
        {
            return ColorUtils.GammaToLinearSpace(rgb);
        }
        
        public static float3 ToVFXColor(Color color)
        {
            return ColorUtils.GammaToLinearSpace(color.AsFloat3());
        }
    }
}
