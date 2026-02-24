using BovineLabs.Core.Utility;
using Unity.Collections;

namespace KrasCore
{
    public static class PooledNativeListExtensions
    {
        public static PooledNativeList<T> WithCapacity<T>(this PooledNativeList<T> pooled, int minCapacity) 
            where T : unmanaged
        {
            if (pooled.List.Capacity < minCapacity)
            {
                pooled.List.SetCapacity(minCapacity);
            }
            return pooled;
        }
        
        public static NativeArray<T> AsArray<T>(this PooledNativeList<T> pooled, int length, NativeArrayOptions options = NativeArrayOptions.ClearMemory) 
            where T : unmanaged
        {
            var list = pooled.List;
            list.Resize(length, options);
            
            return list.AsArray();
        }
    }
}