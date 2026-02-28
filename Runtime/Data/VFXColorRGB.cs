using Unity.Mathematics;
using UnityEngine;

namespace KrasCore
{
    public struct VFXColorRGB
    {
        public float3 Value;

        public VFXColorRGB(Color color)
        {
            Value = color.linear.AsFloat3();
        }
        
        public static implicit operator VFXColorRGB(Color color) => new(color);
        public static implicit operator Vector3(VFXColorRGB color) => color.Value;
    }
}