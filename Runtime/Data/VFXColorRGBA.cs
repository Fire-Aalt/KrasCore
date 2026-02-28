using Unity.Mathematics;
using UnityEngine;

namespace KrasCore
{
    public struct VFXColorRGBA
    {
        public float4 Value;

        public VFXColorRGBA(Color color)
        {
            Value = ToVFXColor(color);
        }
        
        public static implicit operator VFXColorRGBA(Color color) => new(color);
        public static implicit operator Vector4(VFXColorRGBA color) => color.Value;
        
        public static Vector4 ToVFXColor(float4 rgba)
        {
            var rgb = ColorUtils.GammaToLinearSpace(rgba.xyz);
            return new Vector4(rgb.x, rgb.y, rgb.z, rgba.w);
        }
        
        public static float4 ToVFXColor(Color color)
        {
            var rgb = ColorUtils.GammaToLinearSpace(color.AsFloat3());
            return new float4(rgb.x, rgb.y, rgb.z, color.a);
        }
    }
}