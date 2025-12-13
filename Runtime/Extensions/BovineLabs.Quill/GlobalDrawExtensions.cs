#if BL_QUILL
using BovineLabs.Quill;
using Unity.Mathematics;
using UnityEngine;

namespace KrasCore.Quill
{
    public static class GlobalDrawEx
    {
        public static void WirePlane(float3 position, float3 direction, float2 size, Color color, float duration = 0f)
        {
            const float epsilon = 1e-8f;

            // Validate and normalize normal
            var n = direction;
            var lenSq = math.lengthsq(n);
            if (lenSq < epsilon)
                throw new System.ArgumentException("direction (normal) must be non-zero", nameof(direction));
            n = math.normalize(n);

            var worldUp = new float3(0f, 1f, 0f);
            
            if (math.abs(math.dot(n, worldUp)) > 0.999f)
                worldUp = new float3(1f, 0f, 0f);

            // Create local tangent & bitangent (orthonormal basis)
            var tangent = math.normalize(math.cross(worldUp, n));
            var bitangent = math.cross(n, tangent); // already orthogonal; length = 1 if tangent & n are normalized

            var halfWidth  = size.x * 0.5f;
            var halfHeight = size.y * 0.5f;

            // corners relative to center
            var p0 = position - tangent * halfWidth - bitangent * halfHeight; // bottom-left
            var p1 = position - tangent * halfWidth + bitangent * halfHeight; // top-left
            var p2 = position + tangent * halfWidth + bitangent * halfHeight; // top-right
            var p3 = position + tangent * halfWidth - bitangent * halfHeight; // bottom-right
            
            GlobalDraw.Quad(p0, p1, p2, p3, color, duration);
        }
    }
}
#endif