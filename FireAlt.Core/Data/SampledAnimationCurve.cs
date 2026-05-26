using Unity.Assertions;
using UnityEngine;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Entities;

namespace KrasCore
{
    public struct SampledAnimationCurve
    {
        private BlobArray<float> _sampledFloat;

        public static BlobAssetReference<SampledAnimationCurve> CreateBlobAsset(IBaker baker, AnimationCurve curve, int samples = 256)
        {
            var builder = new BlobBuilder(Allocator.Temp);

            ref var data = ref builder.ConstructRoot<SampledAnimationCurve>();

            data.Initialize(ref builder, curve, samples);
            var reference = builder.CreateBlobAssetReference<SampledAnimationCurve>(Allocator.Persistent);
            baker.AddBlobAsset(ref reference, out _);
            
            return reference;
        }
        
        public void Initialize(ref BlobBuilder blobBuilder, AnimationCurve curve, int samples = 256)
        {
            Assert.IsTrue(samples >= 2, "Samples must be 2 or higher.");
            
            var sampledValues = blobBuilder.Allocate(ref _sampledFloat, samples);

            float timeFrom = curve.keys[0].time;
            float timeTo = curve.keys[^1].time;
            float timeStep = (timeTo - timeFrom) / (samples - 1);

            for (int i = 0; i < samples; i++)
            {
                sampledValues[i] = curve.Evaluate(timeFrom + (i * timeStep));
            }
        }

        /// <param name="time">Must be from 0 to 1</param>
        public float EvaluateLerp(float time)
        {
            int len = _sampledFloat.Length - 1;
            float clamp01 = time < 0 ? 0 : (time > 1 ? 1 : time);
            float floatIndex = (clamp01 * len);
            int floorIndex = (int)math.floor(floatIndex);
            if (floorIndex == len)
            {
                return _sampledFloat[len];
            }

            float lowerValue = _sampledFloat[floorIndex];
            float higherValue = _sampledFloat[floorIndex + 1];
            return math.lerp(lowerValue, higherValue, math.frac(floatIndex));
        }
    }
}
