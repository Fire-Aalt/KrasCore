using BovineLabs.Core.Utility;
using Unity.Assertions;
using Unity.Collections;
using Unity.Mathematics;

namespace KrasCore.Quill
{
    public static class DrawerImpl
    {
        public static NativeArray<float3x3> SolidSphere(PooledNativeList<float3x3> pooledTriangles, float3 center, float radius, int sideCount)
        {
            Assert.IsTrue(radius > 0f, "Radius must be greater than 0.");
            Assert.IsTrue(sideCount >= 3f, "SideCount must be greater than 3.");
            
            var longitudeCount = math.max(3, sideCount);
            var latitudeBands = math.max(2, sideCount);
            var ringCount = latitudeBands - 1;
            var triangleCount = 2 * longitudeCount * ringCount;

            using var pooledRings = PooledNativeList<float3>.Make();
            var rings = pooledRings.AsArray(ringCount * longitudeCount);
            
            var triangles = pooledTriangles.AsArray(triangleCount);
            
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

            return triangles;
        }
        
        public static void CapsuleFromPoints(float3 start, float3 end, float radius, out float3 center, out quaternion rotation, out float height)
        {
            var dir = end - start;
            var length = math.length(dir);

            center = (start + end) * 0.5f;

            height = length + 2f * radius;
            rotation = mathex.RotationFromUpToDirection(dir, math.up());
            
            if (length <= 1e-6f)
            {
                height = 2f * radius;
                rotation = quaternion.identity;
            }
        }
        
        public static void SquareXZ(in float3 position, in float2 size, out float3 p0, out float3 p1, out float3 p2, out float3 p3)
        {
            var size3Half = new float3(size.x, 0, size.y) / 2f;
            p0 = position - size3Half;
            p1 = new float3(position.x - size3Half.x, position.y, position.z + size3Half.z);
            p2 = position + size3Half;
            p3 = new float3(position.x + size3Half.x, position.y, position.z - size3Half.z);
        }
        
        public static void SquareXY(in float3 position, in float2 size, out float3 p0, out float3 p1, out float3 p2, out float3 p3)
        {
            var size3Half = new float3(size.x, size.y, 0) / 2f;
            p0 = position - size3Half;
            p1 = new float3(position.x - size3Half.x, position.y + size3Half.y, position.z );
            p2 = position + size3Half;
            p3 = new float3(position.x + size3Half.x, position.y - size3Half.y, position.z);
        }
        
        public static void Trajectory(NativeList<float3> points, float3 initialPos, float gravity, float initialVelocity,
            float angleDeg, float angleDivergence)
        {
            const int trajectoryPreviewSamples = 256;
            
            if (angleDivergence == 0f)
            {
                Trajectory(points, initialPos, gravity, initialVelocity, angleDeg);
            }
            else
            {
                for (var i = 0; i < trajectoryPreviewSamples; i++)
                {
                    var t = (float)i / (trajectoryPreviewSamples - 1);
                    var divergentAngleDeg = angleDeg + math.lerp(-angleDivergence, angleDivergence, t);

                    Trajectory(points, initialPos, gravity, initialVelocity, divergentAngleDeg);
                }
            }
        }
        
        private static void Trajectory(NativeList<float3> points, float3 initialPos, float gravity, float initialVelocity, float angleDeg)
        {
            const int trajectoryPreviewLinesCount = 64;
            
            var maxDistance = TrajectoryUtils.GetMaxTrajectoryDistance(initialPos.y, gravity, initialVelocity, angleDeg);
            if (math.abs(maxDistance) <= 0.0001f)
            {
                return;
            }
            TrajectoryUtils.EvaluateProjectileMotion(points, initialPos, gravity, initialVelocity, angleDeg, maxDistance, trajectoryPreviewLinesCount);
        }
    }
}