using System;
using BovineLabs.Core.Collections;
using Unity.Collections;

namespace KrasCore
{
    public struct NativeDynamicPerfectHashMap<TKey, TValue> : IDisposable
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged, IEquatable<TValue>
    {
        public NativePerfectHashMap<TKey, TValue> Map;
        public NativeHashMap<TKey, TValue> DynamicMap;

        private readonly TValue _defaultValue;
        private readonly Allocator _allocator;
        
        public NativeDynamicPerfectHashMap(int initialCapacity, Allocator allocator, TValue defaultValue = default)
        {
            DynamicMap = new NativeHashMap<TKey, TValue>(initialCapacity, allocator);
            Map = default;
            _defaultValue = defaultValue;
            _allocator = allocator;
        }
        
        // public ref TValue GetDynamicAsRef(TKey key)
        // {
        //     var result = TryAdd(key, item);
        //
        //     if (!result)
        //     {
        //         ThrowKeyAlreadyAdded(key);
        //     }
        // }
        //
        public void DynamicAdd(TKey key, TValue item)
        {
            DynamicMap.Add(key, item);
        }
        
        public void DynamicRemove(TKey key)
        {
            DynamicMap.Remove(key);
        }

        public void ApplyDynamicChanges()
        {
            if (Map.IsCreated) Map.Dispose();
            var keyValueArrays = DynamicMap.GetKeyValueArrays(Allocator.Temp);
            Map = new NativePerfectHashMap<TKey, TValue>(keyValueArrays.Keys, keyValueArrays.Values, _defaultValue, _allocator);
        }

        public void Dispose()
        {
            if (Map.IsCreated) Map.Dispose();
            DynamicMap.Dispose();
        }
    }
}