#if BL_QUILL
using BovineLabs.Quill;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Physics.Authoring;
using UnityEngine;

namespace KrasCore.Quill
{
    [BurstCompile]
    public static class GlobalDrawEx
    {
        private const int TrajectoryPreviewLinesCount = 64;
        private const int TrajectoryPreviewSamples = 256;
        
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

        public static void DrawTrajectory(Transform origin, float defaultGravity, PhysicsBodyAuthoring physicsBodyAuthoring, 
            float initialVelocity, float angleDeg, float angleDivergence, Color color)
        {
            var gravity = defaultGravity;
            if (physicsBodyAuthoring != null)
            {
                gravity *= physicsBodyAuthoring.GravityFactor;
            }
            DrawTrajectory(origin.position, gravity, initialVelocity, angleDeg, angleDivergence, color);
        }
        
        [BurstCompile]
        public static void DrawTrajectory(in float3 initialPos, float gravity, float initialVelocity, float angleDeg, float angleDivergence, in Color color)
        {
            if (angleDivergence == 0f)
            {
                var maxDistance = TrajectoryUtils.GetMaxTrajectoryDistance(initialPos.y, gravity, initialVelocity, angleDeg);
                DrawTrajectory(initialPos, gravity, initialVelocity, color, angleDeg, maxDistance);
            }
            else
            {
                for (var i = 0; i < TrajectoryPreviewSamples; i++)
                {
                    var t = (float)i / (TrajectoryPreviewSamples - 1);
                    var divergentAngleDeg = angleDeg + math.lerp(-angleDivergence, angleDivergence, t);
                    var maxDistance = TrajectoryUtils.GetMaxTrajectoryDistance(initialPos.y, gravity, initialVelocity, divergentAngleDeg);

                    DrawTrajectory(initialPos, gravity, initialVelocity, color, divergentAngleDeg, maxDistance);
                }
            }
        }

        private static void DrawTrajectory(float3 initialPos, float gravity, float initialVelocity, Color color, float angleDeg, float maxDistance)
        {
            if (math.abs(maxDistance) <= 0.0001f)
            {
                return;
            }

            TrajectoryUtils.EvaluateProjectileMotion(initialPos, gravity, initialVelocity, angleDeg, maxDistance, TrajectoryPreviewLinesCount, Allocator.Temp, out var linePoints);
            GlobalDraw.Lines(linePoints.ToArray(Allocator.Temp), color);
        }
    }
}
#endif