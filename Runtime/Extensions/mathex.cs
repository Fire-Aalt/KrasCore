using System.Diagnostics.CodeAnalysis;
using Unity.Mathematics;
using static Unity.Mathematics.math;

namespace KrasCore
{
    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "matching mathematics package")]
    [SuppressMessage("ReSharper", "SA1300", Justification = "matching mathematics package")]
    [SuppressMessage("ReSharper", "IdentifierTypo", Justification = "lower case causes issues")]
    public static class mathex
    {
        public static float3 ForwardXZFromLocalToWorld(float4x4 localToWorld)
        {
            // Use right (local +X) projected to XZ
            var right = new float3(localToWorld.c0.x, localToWorld.c0.y, localToWorld.c0.z);
            var dir = new float3(right.x, 0f, right.z);
            var len = length(dir);
            if (len > EPSILON) return dir / len;
            
            return new float3(0f, 0f, 1f);
        }
    }
}