using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs.LowLevel.Unsafe;

namespace KrasCore
{
    public static class NativeThreadListExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe NativeArray<int> GetStartIndexArray<T>(this NativeThreadList<T> nativeThreadList, ref SystemState state)
            where T : unmanaged
        {
            return nativeThreadList._unsafeParallelList->GetStartIndexArray(ref state);
        }
        
        public static NativeArray<int> GetStartIndexArray<T>(this UnsafeThreadList<T> unsafeThreadList, ref SystemState state)
            where T : unmanaged
        {
            NativeArray<int> lengths = new NativeArray<int>();
            lengths.Initialize(JobsUtility.ThreadIndexCount, state.WorldUpdateAllocator, NativeArrayOptions.UninitializedMemory);

            int count = 0;
            for (int i = 0; i < JobsUtility.ThreadIndexCount; i++)
            {
                lengths[i] = count;
                count += unsafeThreadList.GetPerThreadList(i).List.m_length;
            }

            return lengths;
        }
    }
}