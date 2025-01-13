using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace KrasCore
{
    [BurstCompile]
    public static class RandomUtils
    {
        [BurstCompile]
        public static int NextVariant(ref Random rng, in NativeArray<int> weights, int weightSum)
        {
            float randomValue = rng.NextFloat();
            return NextVariant(randomValue, weights, weightSum);
        }
        
        [BurstCompile]
        public static int NextVariant(ref Random rng, ref BlobArray<int> weights, int weightSum)
        {
            float randomValue = rng.NextFloat();
            return NextVariant(randomValue, ref weights, weightSum);
        }

        [BurstCompile]
        public static int NextVariant(float randomValue, in NativeArray<int> weights, int weightSum)
        {
            float sum = 0f;

            int total = weightSum;
            if (total == 0)
            {
                return -1;
            }

            for (int variant = 0; variant < weights.Length; variant++)
            {
                sum += weights[variant] / (float)total;
                if (sum >= randomValue)
                {
                    return variant;
                }
            }
            return -1;
        }
        
        [BurstCompile]
        public static int NextVariant(float randomValue, ref BlobArray<int> weights, int weightSum)
        {
            float sum = 0f;

            int total = weightSum;
            if (total == 0)
            {
                return -1;
            }

            for (int variant = 0; variant < weights.Length; variant++)
            {
                sum += weights[variant] / (float)total;
                if (sum >= randomValue)
                {
                    return variant;
                }
            }
            return -1;
        }
    }
}