using System;
using Unity.Mathematics;
using UnityEngine;

namespace KrasCore.Data
{
    [Serializable]
    public struct SpriteProperties : IEquatable<SpriteProperties>
    {
        public float2 normalizedPivot;
        public float2 rectScale;
        public float4 uvAtlas;

        public float2 UvScale => new float2(uvAtlas.x, uvAtlas.y);
        public float2 UvBias => new float2(uvAtlas.z, uvAtlas.w);
        
        public SpriteProperties(Sprite sprite)
        {
            if (sprite != null)
            {
                uvAtlas = RendererUtility.GetUvAtlas(sprite);
                normalizedPivot = RendererUtility.GetNormalizedPivot(sprite);
                rectScale = RendererUtility.GetRectScale(sprite, uvAtlas);
            }
            else
            {
                uvAtlas = new float4(1f, 1f, 0, 0);
                normalizedPivot = new float2(0.5f, 0.5f);
                rectScale = new float2(0f, 0f);
            }
        }

        public bool Equals(SpriteProperties other)
        {
            return normalizedPivot.Equals(other.normalizedPivot) && rectScale.Equals(other.rectScale) && uvAtlas.Equals(other.uvAtlas);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(normalizedPivot, rectScale, uvAtlas);
        }
    }
}