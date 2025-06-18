using System;
using System.Collections.Generic;
using BovineLabs.Core.Cache;
using BovineLabs.Core.Collections;

namespace Unity.Collections.LowLevel.Unsafe
{
    public static class NativeHashMapExtensions
    {
        public static unsafe ref TValue GetValueAsRef<TKey, TValue>(
            this NativeHashMap<TKey, TValue> hashMap,
            in TKey key)
            where TValue : unmanaged
            where TKey : unmanaged, IEquatable<TKey>
        {
            ref var data = ref hashMap.m_Data;
            int idx = data->Find(key);

            if (-1 != idx)
            {
                return ref UnsafeUtility.ArrayElementAsRef<TValue>(data->Ptr, idx);
            }
            
            throw new KeyNotFoundException($"Key '{key}' not found in the NativeHashMap.");
        }
        
        public static unsafe TValue* GetValueAsPointer<TKey, TValue>(
            this NativeHashMap<TKey, TValue> hashMap,
            in TKey key)
            where TValue : unmanaged
            where TKey : unmanaged, IEquatable<TKey>
        {
            ref var data = ref hashMap.m_Data;
            int idx = data->Find(key);

            if (-1 != idx)
            {
                return (TValue*)UnsafeUtility.AddressOf(ref UnsafeUtility.ArrayElementAsRef<TValue>(data->Ptr, idx));
            }
            
            throw new KeyNotFoundException($"Key '{key}' not found in the NativeHashMap.");
        }

    }
}