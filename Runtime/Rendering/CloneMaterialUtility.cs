using UnityEngine;

namespace KrasCore
{
    public static class CloneMaterialUtility
    {
        private static readonly SecondarySpriteTexture[] Buffer = new SecondarySpriteTexture[64];

        public static Material CloneFromLookup(MaterialLookup lookup)
        {
            return lookup.Sprite != default 
                ? Clone(lookup.SrcMaterial, lookup.Sprite) 
                : Clone(lookup.SrcMaterial, lookup.Texture);
        }
        
        public static Material Clone(Material srcMaterial, Sprite sprite)
        {
            var mat = Clone(srcMaterial, sprite.texture);
            
            var count = sprite.GetSecondaryTextures(Buffer);
            for (int i = 0; i < count; i++)
            {
                var secondaryTexture = Buffer[i];

                if (mat.HasTexture(secondaryTexture.name))
                {
                    mat.SetTexture(secondaryTexture.name, secondaryTexture.texture);
                }
            }
            
            return mat;
        }

        public static Material Clone(Material srcMaterial, Texture texture)
        {
            var mat = new Material(srcMaterial)
            {
                mainTexture = texture
            };
            return mat;
        }
    }
}