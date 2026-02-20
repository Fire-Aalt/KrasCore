using BovineLabs.Quill;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace KrasCore
{
    [BurstCompile]
    public static class TrajectoryUtils
    {
        private const int PreviewLinesCount = 64;
        private const int PreviewSamples = 256;

        [BurstCompile]
        public static void DrawTrajectory(in float3 initialPos, float gravity, float initialVelocity, float angleDeg, float angleDivergence, in Color color)
        {
            if (angleDivergence == 0f)
            {
                var maxPreviewX = GetMaxPreviewX(initialPos.y, gravity, initialVelocity, angleDeg);
                DrawTrajectory(initialPos, gravity, initialVelocity, color, angleDeg, maxPreviewX);
            }
            else
            {
                for (var i = 0; i < PreviewSamples; i++)
                {
                    var t = (float)i / (PreviewSamples - 1);
                    var divergentAngleDeg = angleDeg + math.lerp(-angleDivergence, angleDivergence, t);
                    var maxPreviewX = GetMaxPreviewX(initialPos.y, gravity, initialVelocity, divergentAngleDeg);

                    DrawTrajectory(initialPos, gravity, initialVelocity, color, divergentAngleDeg, maxPreviewX);
                }
            }
        }

        private static void DrawTrajectory(float3 initialPos, float gravity, float initialVelocity, Color color, float angleDeg, float maxPreviewX)
        {
            if (maxPreviewX <= 0f)
            {
                return;
            }

            EvaluateProjectileMotion(initialPos, gravity, initialVelocity, angleDeg, maxPreviewX, Allocator.Temp, out var linePoints);
            GlobalDraw.Lines(linePoints.ToArray(Allocator.Temp), color);
        }

        [BurstCompile]
        public static void EvaluateProjectileMotion(in float3 initialPos, float gravity, float initialVelocity,
            float angleDeg, float maxPreviewX, Allocator allocator, out NativeList<float3> linePoints)
        {
            linePoints = new NativeList<float3>(64, allocator);

            var angleRad = angleDeg * Mathf.Deg2Rad;
            linePoints.Add(GetProjectilePos(initialPos, gravity, initialVelocity, angleRad, 0f));
            for (var i = 1; i <= PreviewLinesCount; i++)
            {
                var xDisplacement = maxPreviewX * i / PreviewLinesCount;
                var pos = GetProjectilePos(initialPos, gravity, initialVelocity, angleRad, xDisplacement);

                linePoints.Add(pos);

                if (pos.y == 0f)
                {
                    break;
                }

                if (i < PreviewLinesCount)
                {
                    linePoints.Add(pos);
                }
            }
        }

        public static float3 GetProjectilePos(float3 initialPos, float g, float v0, float angleRad, float xDisplacement)
        {
            var x = initialPos.x + xDisplacement;
            var y = math.max(EvalProjectileY(initialPos.y, g, v0, angleRad, xDisplacement), 0);
            var z = initialPos.z;
            return new float3(x, y, z);
        }

        public static float EvalProjectileY(float h, float g, float v0, float angleRad, float x)
        {
            var cosA = math.cos(angleRad);
            var tanA = math.tan(angleRad);

            return h + x * tanA - g * (x * x) / (2f * v0 * v0 * (cosA * cosA));
        }

        private static float GetMaxPreviewX(float initialHeight, float gravity, float initialVelocity, float angleDeg)
        {
            if (gravity <= 0f || initialVelocity <= 0f)
            {
                return 0f;
            }

            var angleRad = angleDeg * Mathf.Deg2Rad;
            var sinA = math.sin(angleRad);
            var cosA = math.cos(angleRad);
            var velocitySquared = initialVelocity * initialVelocity;
            var discriminant = sinA * sinA + 2f * gravity * initialHeight / velocitySquared;

            if (discriminant < 0f)
            {
                return 0f;
            }

            var maxPreviewX = velocitySquared * cosA * (sinA + math.sqrt(discriminant)) / gravity;
            return math.max(maxPreviewX, 0f);
        }
    }
}
