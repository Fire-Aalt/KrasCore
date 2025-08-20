using System;
using System.Collections.Generic;

namespace Unity.Collections.LowLevel.Unsafe
{
    public static unsafe class NativeHashMapExtensions
    {
        public static ref TValue GetValueAsRef<TKey, TValue>(ref this UnsafeHashMap<TKey, TValue> hashMap, TKey key)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            var idx = hashMap.m_Data.Find(key);

            if (idx != -1)
            {
                return ref UnsafeUtility.ArrayElementAsRef<TValue>(hashMap.m_Data.Ptr, idx);
            }

            throw new KeyNotFoundException($"Key '{key}' not found in the NativeHashMap.");
        }

        public static ref TValue GetValueAsRef<TKey, TValue>(
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
        
        public static TValue* GetValueAsPointer<TKey, TValue>(
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