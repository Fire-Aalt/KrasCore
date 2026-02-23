#if BL_QUILL
using BovineLabs.Quill;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace KrasCore.Quill
{
    public static class DrawerExtensions
    {
        public static void RectangleXZ(this Drawer drawer, float3 position, float2 size, Color color, float duration = 0f)
        {
            GetSquareXZVertices(position, size, out var p0, out var p1, out var p2, out var p3);
        
            drawer.Quad(p0, p1, p2, p3, color, duration);
        }
        
        public static void SolidRectangleXZ(this Drawer drawer, float3 position, float2 size, Color color, float duration = 0f)
        {
            GetSquareXZVertices(position, size, out var p0, out var p1, out var p2, out var p3);
            
            drawer.SolidQuad(p0, p1, p2, p3, color, duration);
        }
        
        public static void SolidRectangleXY(this Drawer drawer, float3 position, float2 size, Color color, float duration = 0f)
        {
            GetSquareXYVertices(position, size, out var p0, out var p1, out var p2, out var p3);
            
            drawer.SolidQuad(p0, p1, p2, p3, color, duration);
        }

        public static void SolidSphere(this Drawer drawer, float3 center, float radius, int sideCount, Color color, float duration = 0f)
        {
            if (radius <= 0f || sideCount < 3)
            {
                return;
            }

            var longitudeCount = math.max(3, sideCount);
            var latitudeBands = math.max(2, sideCount);
            var ringCount = latitudeBands - 1;
            var triangleCount = 2 * longitudeCount * ringCount;

            var rings = new NativeArray<float3>(ringCount * longitudeCount, Allocator.Temp);
            var triangles = new NativeArray<float3x3>(triangleCount, Allocator.Temp);

            try
            {
                for (var ring = 0; ring < ringCount; ring++)
                {
                    var phi = math.PI * (ring + 1) / latitudeBands;
                    var y = math.cos(phi) * radius;
                    var ringRadius = math.sin(phi) * radius;
                    var ringOffset = ring * longitudeCount;

                    for (var side = 0; side < longitudeCount; side++)
                    {
                        var theta = (2f * math.PI * side) / longitudeCount;
                        var x = math.cos(theta) * ringRadius;
                        var z = math.sin(theta) * ringRadius;
                        rings[ringOffset + side] = center + new float3(x, y, z);
                    }
                }

                var top = center + new float3(0f, radius, 0f);
                var bottom = center + new float3(0f, -radius, 0f);
                var triangleIndex = 0;

                for (var side = 0; side < longitudeCount; side++)
                {
                    var next = (side + 1) % longitudeCount;
                    triangles[triangleIndex++] = new float3x3(top, rings[next], rings[side]);
                }

                for (var ring = 0; ring < ringCount - 1; ring++)
                {
                    var upperOffset = ring * longitudeCount;
                    var lowerOffset = (ring + 1) * longitudeCount;

                    for (var side = 0; side < longitudeCount; side++)
                    {
                        var next = (side + 1) % longitudeCount;
                        var upperCurrent = rings[upperOffset + side];
                        var upperNext = rings[upperOffset + next];
                        var lowerCurrent = rings[lowerOffset + side];
                        var lowerNext = rings[lowerOffset + next];

                        triangles[triangleIndex++] = new float3x3(upperCurrent, upperNext, lowerCurrent);
                        triangles[triangleIndex++] = new float3x3(upperNext, lowerNext, lowerCurrent);
                    }
                }

                var lastRingOffset = (ringCount - 1) * longitudeCount;
                for (var side = 0; side < longitudeCount; side++)
                {
                    var next = (side + 1) % longitudeCount;
                    var current = rings[lastRingOffset + side];
                    var nextPoint = rings[lastRingOffset + next];
                    triangles[triangleIndex++] = new float3x3(bottom, current, nextPoint);
                }

                drawer.SolidTriangles(triangles, color, duration);
            }
            finally
            {
                if (triangles.IsCreated)
                {
                    triangles.Dispose();
                }

                if (rings.IsCreated)
                {
                    rings.Dispose();
                }
            }
        }
        
        private static void GetSquareXZVertices(in float3 position, in float2 size, out float3 p0, out float3 p1, out float3 p2, out float3 p3)
        {
            var size3Half = new float3(size.x, 0, size.y) / 2f;
            p0 = position - size3Half;
            p1 = new float3(position.x - size3Half.x, position.y, position.z + size3Half.z);
            p2 = position + size3Half;
            p3 = new float3(position.x + size3Half.x, position.y, position.z - size3Half.z);
        }
        
        private static void GetSquareXYVertices(in float3 position, in float2 size, out float3 p0, out float3 p1, out float3 p2, out float3 p3)
        {
            var size3Half = new float3(size.x, size.y, 0) / 2f;
            p0 = position - size3Half;
            p1 = new float3(position.x - size3Half.x, position.y + size3Half.y, position.z );
            p2 = position + size3Half;
            p3 = new float3(position.x + size3Half.x, position.y - size3Half.y, position.z);
        }
        
        public static void CapsuleFromPoints(this Drawer drawer, float3 start, float3 end, float radius, int sideCount, Color color, float duration = 0f)
        {
            var dir = end - start;
            var length = math.length(dir);

            var center = (start + end) * 0.5f;

            var cylinderHeight = length + 2f * radius;
            var rot = mathex.RotationFromUpToDirection(dir, math.up());
            
            if (length <= 1e-6f)
            {
                cylinderHeight = 2f * radius;
                rot = quaternion.identity;
            }
            
            drawer.Capsule(center, rot, cylinderHeight, radius, sideCount, color, duration);
        }
    }
}
#endif
