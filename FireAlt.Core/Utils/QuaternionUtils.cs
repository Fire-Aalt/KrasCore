using Unity.Transforms;
using static Unity.Mathematics.math;
using quaternion = Unity.Mathematics.quaternion;

namespace KrasCore
{
    public class QuaternionUtils
    {
        public static void SmoothRotationZAxis(ref LocalTransform targetTransform, quaternion targetRot, float t)
        {
            var current = EulerZXY(targetTransform.Rotation);
            var target = EulerZXY(targetRot);

            // compute shortest signed delta in radians
            var dz = target.z - current.z;
            dz = atan2(sin(dz), cos(dz));

            var tSaturate = saturate(t);
            var newZ = current.z + dz * tSaturate;

            target.z = newZ;
            targetTransform.Rotation = quaternion.EulerZXY(target);
        }
    }
}