using System;
using Unity.Entities;
using UnityEngine;

namespace KrasCore
{
    public struct RuntimeMaterial : IComponentData, IEnableableComponent
    {
        public MaterialLookup Lookup;
        
        public UnityObjectRef<Material> Value;

        public RuntimeMaterial(Material srcMaterial, Texture mainTexture)
        {
            Lookup = new MaterialLookup
            {
                SrcMaterial = srcMaterial,
                Texture = mainTexture
            };
            Value = default;
        }
    }
    
    public struct MaterialLookup : IEquatable<MaterialLookup>
    {
        public UnityObjectRef<Material> SrcMaterial;
        public UnityObjectRef<Texture> Texture;
        
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