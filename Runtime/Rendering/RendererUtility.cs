using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;

namespace KrasCore
{
    [BurstCompile]
    public static class RendererUtility
    {
        public static float4 GetUvAtlas(Sprite sprite)
        {
            var ratio = new Vector2(1f / sprite.texture.width, 1f / sprite.texture.height);
            var size = Vector2.Scale(sprite.textureRect.size, ratio);
            var offset = Vector2.Scale(sprite.textureRect.position, ratio);
            return new float4(size.x, size.y, offset.x, offset.y);
        }
        
        public static void GetUVCorners(Sprite sprite, out float2 minUv, out float2 maxUv)
        {
            var rect = sprite.textureRect;
            var texture = sprite.texture;
            
            minUv = new float2(
                rect.position.x / texture.width,
                rect.position.y / texture.height);
            maxUv = minUv + new float2(
                rect.size.x / texture.width,
                rect.size.y / texture.height);
        }
        
        public static float2 GetNormalizedPivot(Sprite sprite)
        {
            var newPivot = sprite.pivot - sprite.textureRectOffset;
            return newPivot / sprite.textureRect.size;
        }
        
        public static float2 GetRectScale(Sprite sprite, float4 uvAtlas)
        {
            return sprite.GetNativeSize(uvAtlas.xy);
        }
        
        private static float2 GetNativeSize(this Sprite source)
        {
            return new float2(source.texture.width, source.texture.height) / source.pixelsPerUnit;
        }

        private static float2 GetNativeSize(this Sprite source, in float2 uvAtlas)
        {
            return source.GetNativeSize() * uvAtlas;
        }

        public static RenderParams CreateRenderParams(MeshRenderer meshRenderer, Camera camera, MaterialPropertyBlock matProps)
        {
            return new RenderParams(meshRenderer.sharedMaterial)
            {
                camera = camera,
                matProps = matProps,
                rendererPriority = meshRenderer.rendererPriority,
                shadowCastingMode = meshRenderer.shadowCastingMode,
                lightProbeUsage = meshRenderer.lightProbeUsage,
                motionVectorMode = meshRenderer.motionVectorGenerationMode,
                receiveShadows = meshRenderer.receiveShadows,
                reflectionProbeUsage = meshRenderer.reflectionProbeUsage,
                instanceID = meshRenderer.GetInstanceID(),
            };
        }
    }
}