using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Collections.LowLevel.Unsafe
{
}
namespace KrasCore
{
    public static class ParallelHashMapExtensions
    {
        public static unsafe UnsafeParallelMultiHashMap<TKey, TValue>.ParallelWriter AsParallelWriter<TKey, TValue>(this UnsafeParallelMultiHashMap<TKey, TValue> map, int threadIndex)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            UnsafeParallelMultiHashMap<TKey, TValue>.ParallelWriter writer;

            writer.m_ThreadIndex = threadIndex;
            writer.m_Buffer = map.m_Buffer;

            return writer;
        }
            
        public static void EnsureCapacity<TKey, TValue>(this NativeParallelHashMap<TKey, TValue> map, int newDataCount) where TKey : unmanaged, IEquatable<TKey> where TValue : unmanaged
        {
            int newCount = map.Count() + newDataCount;

            while (map.Capacity < newCount)
            {
                map.Capacity *= 2;
            }
        }
        
        public static void EnsureCapacity<TKey, TValue>(this NativeParallelMultiHashMap<TKey, TValue> map, int newDataCount) where TKey : unmanaged, IEquatable<TKey> where TValue : unmanaged
        {
            int newCount = map.Count() + newDataCount;

            while (map.Capacity < newCount)
            {
                map.Capacity *= 2;
            }
        }
        
        public static void ToNativeLists<TKey, TValue>(this NativeParallelHashMap<TKey, TValue> map,
            ref NativeList<TKey> keyList, ref NativeList<TValue> valueList, int offset = 0) 
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            map.m_HashMapData.ConvertToList(ref keyList, ref valueList, offset);
        }
        
        private static unsafe void ConvertToList<TKey, TValue>(this UnsafeParallelHashMap<TKey, TValue> data, 
            ref NativeList<TKey> keyList, ref NativeList<TValue> valueList, int offset)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            var dataCount = data.Count();
            
            if (keyList.Capacity < dataCount)
            {
                keyList.Capacity = dataCount;
                valueList.Capacity = dataCount;
            }
            GetKeyValueArrays(data.m_Buffer, dataCount, keyList.AsArray(), valueList.AsArray(), offset);
        }
        
        public static void ToNativeArrays<TKey, TValue>(this NativeParallelHashMap<TKey, TValue> map,
            ref NativeArray<TKey> keyArray, ref NativeArray<TValue> valueArray, int offset = 0) 
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            map.m_HashMapData.ConvertToArray(ref keyArray, ref valueArray, offset);
        }
        
        private static unsafe void ConvertToArray<TKey, TValue>(this UnsafeParallelHashMap<TKey, TValue> data, 
            ref NativeArray<TKey> keyList, ref NativeArray<TValue> valueList, int offset)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            GetKeyValueArrays(data.m_Buffer, data.Count(), keyList, valueList, offset);
        }
        
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new [] { typeof(int), typeof(int) })]
        private static unsafe void GetKeyValueArrays<TKey, TValue>(UnsafeParallelHashMapData* data, int dataCount,
            NativeArray<TKey> keyList, NativeArray<TValue> valueList, int offset)
            where TKey : unmanaged
            where TValue : unmanaged
        {
            var bucketArray = (int*)data->buckets;
            var bucketNext = (int*)data->next;

            for (int i = 0, count = 0, max = dataCount, capacityMask = data->bucketCapacityMask
                 ; i <= capacityMask && count < max
                 ; ++i
                )
            {
                int bucket = bucketArray[i];

                while (bucket != -1)
                {
                    keyList[offset + count] = UnsafeUtility.ReadArrayElement<TKey>(data->keys, bucket);
                    valueList[offset + count] = UnsafeUtility.ReadArrayElement<TValue>(data->values, bucket);
                    count++;
                    bucket = bucketNext[bucket];
                }
            }
        }
    }
}