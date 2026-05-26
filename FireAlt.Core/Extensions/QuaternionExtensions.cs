using Unity.Transforms;
using static Unity.Mathematics.math;
using quaternion = Unity.Mathematics.quaternion;

namespace KrasCore
{
    public static class QuaternionExtensions
    {
        /// <summary>
        /// From <see cref="Unity.Mathematics.math"/>
        /// </summary>
        /// <param name="q"></param>
        /// <returns></returns>
        public static float ComputeYAngle(this quaternion q)
        {
            const float epsilon = 1e-6f;
            const float cutoff = (1f - 2f * epsilon) * (1f - 2f * epsilon);

            // prepare the data
            var qv = q.value;
            var d1 = qv * qv.wwww * float4(2f); //xw, yw, zw, ww
            var d2 = qv * qv.yzxw * float4(2f); //xy, yz, zx, ww
            var d3 = qv * qv;

            var y1 = d2.y - d1.x;
            if (y1 * y1 < cutoff)
            {
                var z1 = d2.z + d1.y;
                var z2 = d3.z + d3.w - d3.x - d3.y;
                return atan2(z1, z2);
            }
            //zxz
            return 0f;
        }
        

    }
}