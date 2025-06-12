using System;
using Unity.Assertions;
using Unity.Collections;
using Unity.Entities;
using Random = Unity.Mathematics.Random;

namespace KrasCore
{
    public static class RandomUtils
    {
        public static int NextVariant(ref Random rng, in NativeArray<int> weights, int weightSum)
        {
            return NextVariant(rng.NextFloat(), weights, weightSum);
        }
        
        public static int NextVariant(ref Random rng, ref BlobArray<int> weights, int weightSum)
        {
            return NextVariant(rng.NextFloat(), ref weights, weightSum);
        }

        public static int NextVariant<T>(ref Random rng, ref BlobArray<T> weights, int weightSum)
            where T : unmanaged, IWeightedRandom
        {
            return NextVariant(rng.NextFloat(), ref weights, weightSum);
        }
        
        public static int NextVariant(float randomValue, in NativeArray<int> weights, int weightSum)
        {
            Assert.IsFalse(weightSum == 0, "WeightSum is zero");
            var sum = 0f;

            for (int variant = 0; variant < weights.Length; variant++)
            {
                sum += weights[variant] / (float)weightSum;
                if (sum >= randomValue)
                {
                    return variant;
                }
            }
            throw new Exception("Result is out of range");
        }
        
        public static int NextVariant(float randomValue, ref BlobArray<int> weights, int weightSum)
        {
            Assert.IsFalse(weightSum == 0, "WeightSum is zero");
            var sum = 0f;

            for (int variant = 0; variant < weights.Length; variant++)
            {
                sum += weights[variant] / (float)weightSum;
                if (sum >= randomValue)
                {
                    return variant;
                }
            }
            throw new Exception("Result is out of range");
        }
        
        public static int NextVariant<T>(float randomValue, ref BlobArray<T> weights, int weightSum)
            where T : unmanaged, IWeightedRandom
        {
            Assert.IsFalse(weightSum == 0, "WeightSum is zero");
            var sum = 0f;

            for (int variant = 0; variant < weights.Length; variant++)
            {
                sum += weights[variant].GetWeight() / (float)weightSum;
                if (sum >= randomValue)
                {
                    return variant;
                }
            }
            throw new Exception("Result is out of range");
        }
    }

    public interface IWeightedRandom
    {
        int GetWeight();
    }
}