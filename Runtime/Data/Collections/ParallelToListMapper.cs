using System;
using KrasCore.NZCore;
using Unity.Collections;
using Unity.Jobs;

namespace KrasCore
{
    // Single
    // [assembly: RegisterGenericJobType(typeof(ParallelList<YourType>.UnsafeParallelListToArraySingleThreaded))]
    
    // Multi
    // [assembly: RegisterGenericJobType(typeof(ParallelList<YourType>.PrepareParallelListCopyJob))]
    // [assembly: RegisterGenericJobType(typeof(ParallelList<YourType>.ParallelListToArrayMultiThreaded))]
    public struct ParallelToListMapper<T> : IDisposable where T : unmanaged
    {
        public ParallelList<T> ParallelList;
        public NativeList<T> List;

        public ParallelToListMapper(int capacity, Allocator allocator)
        {
            ParallelList = new ParallelList<T>(capacity, allocator);
            List = new NativeList<T>(capacity, allocator);
        }

        public void Clear()
        {
            ParallelList.Clear();
            List.Clear();
        }

        public JobHandle CopyParallelToListSingle(JobHandle dependency)
        {
            return ParallelList.CopyToArraySingle(ref List, dependency);
        }

        public ParallelList<T>.ThreadWriter AsThreadWriter() => ParallelList.AsThreadWriter();
        public ParallelList<T>.ThreadReader AsThreadReader() => ParallelList.AsThreadReader();
        
        public void Dispose()
        {
            ParallelList.Dispose();
            List.Dispose();
        }
    }
}