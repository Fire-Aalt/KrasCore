#if BL_QUILL
using BovineLabs.Quill;
using Unity.Mathematics;
using UnityEngine;

namespace KrasCore
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
        
        private static void GetSquareXZVertices(in float3 position, in float2 size, out float3 p0, out float3 p1, out float3 p2, out float3 p3)
        {
            var size3Half = new float3(size.x, 0, size.y) / 2f;
            p0 = position - size3Half;
            p1 = new float3(position.x - size3Half.x, position.y, position.z + size3Half.z);
            p2 = position + size3Half;
            p3 = new float3(position.x + size3Half.x, position.y, position.z - size3Half.z);
        }
    }
}
#endif