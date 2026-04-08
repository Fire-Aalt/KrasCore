using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs.LowLevel.Unsafe;

namespace KrasCore
{
    public static class ParallelListExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe NativeArray<int> GetStartIndexArray<T>(this ParallelList<T> parallelList, ref SystemState state)
            where T : unmanaged
        {
            return parallelList._unsafeParallelList->GetStartIndexArray(ref state);
        }
        
        public static NativeArray<int> GetStartIndexArray<T>(this UnsafeParallelList<T> unsafeParallelList, ref SystemState state)
            where T : unmanaged
        {
            NativeArray<int> lengths = new NativeArray<int>();
            lengths.Initialize(JobsUtility.ThreadIndexCount, state.WorldUpdateAllocator, NativeArrayOptions.UninitializedMemory);

            int count = 0;
            for (int i = 0; i < JobsUtility.ThreadIndexCount; i++)
            {
                lengths[i] = count;
                count += unsafeParallelList.GetPerThreadList(i).List.m_length;
            }

            return lengths;
        }
    }
}