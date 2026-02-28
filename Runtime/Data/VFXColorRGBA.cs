using Unity.Mathematics;
using UnityEngine;

namespace KrasCore
{
    public struct VFXColorRGBA
    {
        public float4 Value;

        public VFXColorRGBA(Color color)
        {
            Value = color.linear.AsFloat4();
        }
        
        public static implicit operator VFXColorRGBA(Color color) => new(color);
        public static implicit operator Vector4(VFXColorRGBA color) => color.Value;
    }
}