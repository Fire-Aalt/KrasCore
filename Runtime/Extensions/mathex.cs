using System.Diagnostics.CodeAnalysis;
using Unity.Mathematics;
using Unity.Transforms;
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
        
        public static float AngleBetweenDegrees(float2 a, float2 b)
        {
            var la = length(a);
            var lb = length(b);
            if (la < EPSILON || lb < EPSILON) return 0f;

            var d = dot(a, b) / (la * lb);
            d = clamp(d, -1f, 1f);
            return degrees(acos(d));
        }
        
        /// <summary>
        /// Starts at (1, 0), goes anticlockwise
        /// </summary>
        public static float AngleDegrees(float2 v)
        {
            return degrees(atan2(v.y, v.x));
        }
        
        public static LocalTransform CombineLocalTransforms(LocalTransform root, LocalTransform child)
        {
            return new LocalTransform
            {
                Position = root.Position + child.Position,
                Rotation = mul(root.Rotation, child.Rotation),
                Scale = root.Scale * child.Scale
            };
        }
    }
}