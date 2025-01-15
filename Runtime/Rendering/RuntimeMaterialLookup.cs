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
            : this(srcMaterial, sprite.texture) {}
    }
    
    public struct MaterialLookup : IEquatable<MaterialLookup>
    {
        public UnityObjectRef<Material> SrcMaterial;
        public UnityObjectRef<Texture> Texture;
        
        public MaterialLookup(Material srcMaterial, Texture mainTexture)
        {
            SrcMaterial = srcMaterial;
            Texture = mainTexture;
        }
        
        public bool Equals(MaterialLookup other)
        {
            return SrcMaterial.Equals(other.SrcMaterial) && Texture.Equals(other.Texture);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(SrcMaterial, Texture);
        }
    }
}