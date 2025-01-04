using Unity.Collections;

namespace KrasCore
{
    public static class NativeParallelHashMapExtensions
    {
        public static void EnsureCapacity<TKey, TValue>(this NativeParallelHashMap<TKey, TValue> map, int newDataCount) where TKey : unmanaged, System.IEquatable<TKey> where TValue : unmanaged
        {
            int newCount = map.Count() + newDataCount;

            while (map.Capacity < newCount)
            {
                map.Capacity *= 2;
            }
        }
    }
}