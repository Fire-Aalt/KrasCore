using System;
using Unity.Entities;
using UnityEngine;

namespace KrasCore
{
    public struct RuntimeMaterialLookup : IComponentData, IEnableableComponent
    {
        public MaterialLookup Value;
        
        public RuntimeMaterialLookup(Material srcMaterial, Texture mainTexture)
        {
            Value = new MaterialLookup(srcMaterial, mainTexture);
        }

        public RuntimeMaterialLookup(Material srcMaterial, Sprite sprite)
        {
            Value = new MaterialLookup(srcMaterial, sprite);
        }
    }
    
    public struct MaterialLookup : IEquatable<MaterialLookup>
    {
        public UnityObjectRef<Material> SrcMaterial;
        public UnityObjectRef<Texture> Texture;
        public UnityObjectRef<Sprite> Sprite;
        
        public MaterialLookup(Material srcMaterial, Texture mainTexture)
        {
            SrcMaterial = srcMaterial;
            Texture = mainTexture;
            Sprite = default;
        }
        
        public MaterialLookup(Material srcMaterial, Sprite sprite)
        {
            SrcMaterial = srcMaterial;
            Sprite = sprite;
            Texture = default;
        }
        
        public bool Equals(MaterialLookup other)
        {
            return SrcMaterial.Equals(other.SrcMaterial) && Texture.Equals(other.Texture) && Sprite.Equals(other.Sprite);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(SrcMaterial, Texture, Sprite);
        }
    }
}