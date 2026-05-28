using FireAlt.Core.Collections;
using Unity.Assertions;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace FireAlt.Core.Utility
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
            throw new System.Exception("Result is out of range");
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
            throw new System.Exception("Result is out of range");
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
            throw new System.Exception("Result is out of range");
        }

        public static NativeList<int> SelectRandomIndices(ref Random rng, int indicesCount, int maxSelectedCount)
        {
            using var possibleIndices = NativeListPool<int>.Rent();
            for (int i = 0; i < indicesCount; i++)
            {
                possibleIndices.List.Add(i);
            }
            
            var selectedIndices = new NativeList<int>(indicesCount, Allocator.Temp);
            while (selectedIndices.Length < maxSelectedCount && !possibleIndices.List.IsEmpty)
            {
                var selectedIndex = rng.NextInt(0, possibleIndices.List.Length);
                selectedIndices.Add(possibleIndices.List[selectedIndex]);
                possibleIndices.List.RemoveAtSwapBack(selectedIndex);
            }

            return selectedIndices;
        }
    }

    public interface IWeightedRandom
    {
        int GetWeight();
    }
}