using Unity.Mathematics;
using static Unity.Mathematics.math;

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
        
        public static float3 ToEuler(this quaternion quaternion) 
        {
            float4 q = quaternion.value;
            double3 res;

            double sinr_cosp = +2.0 * (q.w * q.x + q.y * q.z);
            double cosr_cosp = +1.0 - 2.0 * (q.x * q.x + q.y * q.y);
            res.x = math.atan2(sinr_cosp, cosr_cosp);

            double sinp = +2.0 * (q.w * q.y - q.z * q.x);
            if (math.abs(sinp) >= 1) {
                res.y = math.PI / 2 * math.sign(sinp);
            } else {
                res.y = math.asin(sinp);
            }

            double siny_cosp = +2.0 * (q.w * q.z + q.x * q.y);
            double cosy_cosp = +1.0 - 2.0 * (q.y * q.y + q.z * q.z);
            res.z = math.atan2(siny_cosp, cosy_cosp);

            return (float3) res;
        }
    }
}