using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace KrasCore
{
    [BurstCompile]
    public static class TrajectoryUtils
    {
        [BurstCompile]
        public static void EvaluateProjectileMotion(in float3 initialPos, float gravity, float initialVelocity,
            float angleDeg, float maxDistance, float linesCount, Allocator allocator, out NativeList<float3> linePoints)
        {
            linePoints = new NativeList<float3>(64, allocator);

            var angleRad = angleDeg * Mathf.Deg2Rad;
            linePoints.Add(GetProjectilePos(initialPos, gravity, initialVelocity, angleRad, 0f));
            for (var i = 1; i <= linesCount; i++)
            {
                var xDisplacement = maxDistance * i / linesCount;
                var pos = GetProjectilePos(initialPos, gravity, initialVelocity, angleRad, xDisplacement);

                linePoints.Add(pos);

                if (pos.y == 0f)
                {
                    break;
                }

                if (i < linesCount)
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

        public static float GetMaxTrajectoryDistance(float initialHeight, float gravity, float initialVelocity, float angleDeg)
        {
            if (gravity <= 0f || initialVelocity <= 0f)
            {
                return 0f;
            }

            var angleRad = angleDeg * Mathf.Deg2Rad;
            var sinA = math.sin(angleRad);
            var cosA = math.cos(angleRad);
            var verticalVelocity = initialVelocity * sinA;
            var horizontalVelocity = initialVelocity * cosA;
            var discriminant = verticalVelocity * verticalVelocity + 2f * gravity * initialHeight;

            if (discriminant < 0f)
            {
                return 0f;
            }

            var timeToGround = (verticalVelocity + math.sqrt(discriminant)) / gravity;
            return horizontalVelocity * math.max(timeToGround, 0f);
        }
    }
}
