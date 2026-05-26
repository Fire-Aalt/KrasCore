using Unity.Mathematics;

namespace KrasCore
{
    public static class RandomExtensions
    {
        public static float2 NextPointInUnitCircle(ref this Random random)
        {
            var angle = random.NextFloat(0f, math.PI * 2f);
            math.sincos(angle, out float s, out float c);

            var radius = math.sqrt(random.NextFloat());

            return new float2(c, s) * radius;
        }
        
        public static float3 NextPointInUnitSphere(ref this Random random)
        {
            var dir = random.NextFloat3Direction();
            var radius = math.pow(random.NextFloat(), 1f / 3f);
            return dir * radius;
        }
    }
}