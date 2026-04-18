using System;
using Unity.Collections;
using Unity.Jobs;

namespace KrasCore
{
    /// <summary>
    /// A convenience wrapper that pairs a <see cref="NativeThreadList{T}"/> producer container with a flattened <see cref="NativeList{T}"/> output list.
    /// </summary>
    /// <remarks>
    /// Use this when you repeatedly collect data in parallel and then need a single contiguous list for sequential processing.
    /// Common patterns include:
    /// <list type="bullet">
    /// <item><description>Produce in jobs through <see cref="AsThreadWriter"/> backed by the internal per-thread list.</description></item>
    /// <item><description>Materialize to <see cref="List"/> with <see cref="CopyParallelToListSingle"/> (job-based) or <see cref="CopyParallelToList"/> (immediate).</description></item>
    /// <item><description>Reuse frame-to-frame by calling <see cref="Clear"/> before the next production pass.</description></item>
    /// </list>
    /// </remarks>
    public struct NativeThreadToListMapper<T> : IDisposable where T : unmanaged
    {
        public NativeThreadList<T> NativeThreadList;
        public NativeList<T> List;

        public NativeThreadToListMapper(int capacity, Allocator allocator)
        {
            NativeThreadList = new NativeThreadList<T>(capacity, allocator);
            List = new NativeList<T>(capacity, allocator);
        }

        public void Clear()
        {
            NativeThreadList.Clear();
            List.Clear();
        }

        public JobHandle CopyParallelToListSingle(JobHandle dependency, UnsafeThreadList<T>.UnsafeParallelListToArraySingleThreaded jobStud = default)
        {
            return NativeThreadList.CopyToListSingle(ref List, dependency, jobStud);
        }

        public void CopyParallelToList()
        {
            NativeThreadList.CopyToList(ref List);
        }
        
        public NativeThreadList<T>.ThreadWriter AsThreadWriter() => NativeThreadList.AsThreadWriter();
        public NativeThreadList<T>.ThreadReader AsThreadReader() => NativeThreadList.AsThreadReader();
        
        public void Dispose()
        {
            NativeThreadList.Dispose();
            List.Dispose();
        }
    }
}
