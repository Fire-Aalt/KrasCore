using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BovineLabs.Core.Collections;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace KrasCore
{
    [StructLayout(LayoutKind.Sequential)]
    [DebuggerDisplay("Start = {StartIndex}, Count = {Count}, IsValid = {IsValid}")]
    public readonly struct MemoryPtr : IEquatable<MemoryPtr>
    {
        public readonly int StartIndex;
        public readonly int Count;

        public bool IsValid => StartIndex >= 0 && Count > 0;
        public int EndIndex => StartIndex + Count - 1;

        public MemoryPtr(int startIndex, int count)
        {
            StartIndex = startIndex;
            Count = count;
        }

        public bool Equals(MemoryPtr other)
        {
            return StartIndex == other.StartIndex && Count == other.Count;
        }

        public override bool Equals(object obj)
        {
            return obj is MemoryPtr other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (StartIndex * 397) ^ Count;
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    [DebuggerDisplay("Stride = {Stride}, Allocations = {AllocationCount}, UsedLength = {UsedLength}, Capacity = {Capacity}, IsCreated = {IsCreated}")]
    public unsafe struct UnsafeHeapMemory : IDisposable
    {
        private const int INVALID_INDEX = -1;
        private const int INITIAL_METADATA_CAPACITY = 16;
        private const int BIN_COUNT = 31;

        private UnsafeList<byte> _data;
        private UnsafeList<Block> _blocks;
        private UnsafeList<int> _recycledBlockIndices;
        private UnsafeList<int> _freeBinHeads;
        private UnsafeHashMap<int, int> _allocatedByStart;

        private int _stride;
        private int _addressHead;
        private int _addressTail;
        private int _usedLength;
        private int _allocationCount;
        private int _minAllocatedStart;
        private int _maxAllocatedEnd;

        public bool IsCreated => _data.IsCreated;
        public int Stride => _stride;
        public int Capacity => _data.IsCreated ? _data.Capacity / _stride : 0;
        public int CapacityBytes => _data.IsCreated ? _data.Capacity : 0;
        public int UsedLength => _usedLength;
        public int UsedLengthBytes => _usedLength * _stride;
        public int AllocationCount => _allocationCount;
        public UnsafeList<byte> DataList => _data;
        
        internal Allocator allocatorLabel;

        public UnsafeHeapMemory(int stride, Allocator allocator)
            : this(stride, 0, allocator)
        {
        }

        public UnsafeHeapMemory(int stride, int initialCapacity, Allocator allocator)
        {
            allocatorLabel = allocator;
            CheckStride(stride);
            CheckInitialCapacity(initialCapacity);

            _stride = stride;
            _data = new UnsafeList<byte>(GetRequiredBytes(initialCapacity, stride), allocator);
            _data.Resize(0);

            _blocks = new UnsafeList<Block>(INITIAL_METADATA_CAPACITY, allocator);
            _blocks.Resize(0);

            _recycledBlockIndices = new UnsafeList<int>(INITIAL_METADATA_CAPACITY, allocator);
            _recycledBlockIndices.Resize(0);

            _freeBinHeads = new UnsafeList<int>(BIN_COUNT, allocator);
            _freeBinHeads.Resize(BIN_COUNT);

            _allocatedByStart = new UnsafeHashMap<int, int>(INITIAL_METADATA_CAPACITY, allocator);

            _addressHead = INVALID_INDEX;
            _addressTail = INVALID_INDEX;
            _usedLength = 0;
            _allocationCount = 0;
            _minAllocatedStart = 0;
            _maxAllocatedEnd = -1;
            ResetFreeBinHeads();
        }

        public MemoryPtr Allocate(int count)
        {
            CheckAllocateCount(count);

            var freeBlockIndex = FindBestFitFreeBlock(count);
            return freeBlockIndex != INVALID_INDEX ? AllocateFromFreeBlock(freeBlockIndex, count) : AllocateAtEnd(count);
        }

        public MemoryPtr Allocate<T>(UnsafeArray<T> source)
            where T : unmanaged
        {
            CheckTypeStride<T>();

            var ptr = Allocate(source.Length);
            var byteCount = GetRequiredBytes(source.Length, _stride);
            UnsafeUtility.MemCpy(_data.Ptr + GetByteOffset(ptr.StartIndex), source.GetUnsafePtr(), byteCount);
            return ptr;
        }

        public MemoryPtr Allocate<T>(NativeArray<T> source)
            where T : unmanaged
        {
            CheckTypeStride<T>();

            if (!source.IsCreated)
            {
                throw new ArgumentNullException(nameof(source));
            }

            var ptr = Allocate(source.Length);
            var byteCount = GetRequiredBytes(source.Length, _stride);
            UnsafeUtility.MemCpy(_data.Ptr + GetByteOffset(ptr.StartIndex), source.GetUnsafeReadOnlyPtr(), byteCount);
            return ptr;
        }

        public MemoryPtr Allocate(UnsafeArray<byte> source)
        {
            if (!source.IsCreated)
            {
                throw new ArgumentNullException(nameof(source));
            }
            CheckLengthStride(source.Length);
            var typeLength = source.Length / _stride;

            var ptr = Allocate(typeLength);
            var byteCount = GetRequiredBytes(typeLength, _stride);
            UnsafeUtility.MemCpy(_data.Ptr + GetByteOffset(ptr.StartIndex), source.GetUnsafePtr(), byteCount);
            return ptr;
        }
        
        public void Free(MemoryPtr ptr)
        {
            CheckMemoryPtrForFree(ptr, out var blockIndex);

            var freedBlock = _blocks[blockIndex];
            _allocatedByStart.Remove(ptr.StartIndex);
            _allocationCount--;

            freedBlock.IsFree = 1;
            freedBlock.PrevFree = INVALID_INDEX;
            freedBlock.NextFree = INVALID_INDEX;
            _blocks[blockIndex] = freedBlock;

            var mergedIndex = MergeNeighbors(blockIndex);
            AddFreeBlockToBins(mergedIndex);
            TrimFreeTail();

            if (_allocationCount == 0)
            {
                _minAllocatedStart = 0;
                _maxAllocatedEnd = -1;
                return;
            }

            if (ptr.StartIndex == _minAllocatedStart || ptr.EndIndex == _maxAllocatedEnd)
            {
                RecomputeValidRange();
            }
        }

        public bool Contains(MemoryPtr ptr)
        {
            return TryGetAllocatedBlockIndex(ptr, out _);
        }

        public bool TryGetValidRange(out int2 range)
        {
            if (_allocationCount == 0)
            {
                range = default;
                return false;
            }

            range = new int2(_minAllocatedStart, _maxAllocatedEnd);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T ElementAt<T>(MemoryPtr ptr, int index)
            where T : unmanaged
        {
            CheckTypeStride<T>();
            CheckElementIndex(ptr, index, out var absoluteIndex);
            return ref UnsafeUtility.AsRef<T>(_data.Ptr + GetByteOffset(absoluteIndex));
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UnsafeArray<T> ArrayAt<T>(MemoryPtr ptr)
            where T : unmanaged
        {
            CheckTypeStride<T>();
            CheckMemoryPtrForFree(ptr, out _);

            var arrayPtr = _data.Ptr + GetByteOffset(ptr.StartIndex);
            return UnsafeArrayUtility.ConvertExistingDataToUnsafeArray<T>(arrayPtr, ptr.Count, allocatorLabel);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UnsafeArray<byte> ArrayAtUnsafe(MemoryPtr ptr)
        {
            CheckMemoryPtrForFree(ptr, out _);

            var arrayPtr = _data.Ptr + GetByteOffset(ptr.StartIndex);
            return UnsafeArrayUtility.ConvertExistingDataToUnsafeArray<byte>(arrayPtr, ptr.Count * _stride, allocatorLabel);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte* GetUnsafePtr(MemoryPtr ptr)
        {
            if (!TryGetAllocatedBlockIndex(ptr, out _))
            {
                throw new ArgumentException("MemoryPtr is not currently allocated.", nameof(ptr));
            }

            return _data.Ptr + GetByteOffset(ptr.StartIndex);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T* GetUnsafePtr<T>(MemoryPtr ptr)
            where T : unmanaged
        {
            CheckTypeStride<T>();
            return (T*)GetUnsafePtr(ptr);
        }

        public int EnsureCapacity(int capacity)
        {
            CheckInitialCapacity(capacity);
            EnsureDataCapacity(capacity);
            return Capacity;
        }

        public void Clear()
        {
            if (!_data.IsCreated)
            {
                return;
            }

            _data.Resize(0);
            _blocks.Resize(0);
            _recycledBlockIndices.Resize(0);
            ResetFreeBinHeads();
            _allocatedByStart.Clear();

            _addressHead = INVALID_INDEX;
            _addressTail = INVALID_INDEX;
            _usedLength = 0;
            _allocationCount = 0;
            _minAllocatedStart = 0;
            _maxAllocatedEnd = -1;
        }

        public void Dispose()
        {
            if (_data.IsCreated)
            {
                _data.Dispose();
            }

            if (_blocks.IsCreated)
            {
                _blocks.Dispose();
            }

            if (_recycledBlockIndices.IsCreated)
            {
                _recycledBlockIndices.Dispose();
            }

            if (_freeBinHeads.IsCreated)
            {
                _freeBinHeads.Dispose();
            }

            if (_allocatedByStart.IsCreated)
            {
                _allocatedByStart.Dispose();
            }

            _stride = 0;
            _addressHead = INVALID_INDEX;
            _addressTail = INVALID_INDEX;
            _usedLength = 0;
            _allocationCount = 0;
            _minAllocatedStart = 0;
            _maxAllocatedEnd = -1;
        }

        private MemoryPtr AllocateAtEnd(int count)
        {
            if (count > int.MaxValue - _usedLength)
            {
                throw new InvalidOperationException("Allocation would exceed maximum supported size.");
            }

            var start = _usedLength;
            var endExclusive = start + count;
            EnsureLength(endExclusive);

            var blockIndex = CreateBlock(start, count, isFree: false);
            AppendByAddress(blockIndex);
            if (!_allocatedByStart.TryAdd(start, blockIndex))
            {
                throw new InvalidOperationException("Allocator tracking state is inconsistent.");
            }

            _usedLength = endExclusive;
            _allocationCount++;
            UpdateRangeOnAllocate(start, count);

            return new MemoryPtr(start, count);
        }

        private MemoryPtr AllocateFromFreeBlock(int blockIndex, int count)
        {
            RemoveFreeBlockFromBins(blockIndex);

            var block = _blocks[blockIndex];
            var start = block.Start;

            if (block.Count == count)
            {
                block.IsFree = 0;
                block.PrevFree = INVALID_INDEX;
                block.NextFree = INVALID_INDEX;
                _blocks[blockIndex] = block;
            }
            else
            {
                var remainingStart = block.Start + count;
                var remainingCount = block.Count - count;
                var remainingIndex = CreateBlock(remainingStart, remainingCount, isFree: true);

                InsertAfterByAddress(blockIndex, remainingIndex);

                block.Count = count;
                block.IsFree = 0;
                block.PrevFree = INVALID_INDEX;
                block.NextFree = INVALID_INDEX;
                _blocks[blockIndex] = block;

                AddFreeBlockToBins(remainingIndex);
            }

            if (!_allocatedByStart.TryAdd(start, blockIndex))
            {
                throw new InvalidOperationException("Allocator tracking state is inconsistent.");
            }

            _allocationCount++;
            UpdateRangeOnAllocate(start, count);

            return new MemoryPtr(start, count);
        }

        private int MergeNeighbors(int blockIndex)
        {
            var mergedIndex = blockIndex;
            mergedIndex = MergeWithPrevious(mergedIndex);
            mergedIndex = MergeWithNext(mergedIndex);
            return mergedIndex;
        }

        private int MergeWithPrevious(int blockIndex)
        {
            var block = _blocks[blockIndex];
            var previousIndex = block.PrevByAddress;
            if (previousIndex == INVALID_INDEX)
            {
                return blockIndex;
            }

            var previous = _blocks[previousIndex];
            if (previous.IsAlive == 0 || previous.IsFree == 0 || previous.Start + previous.Count != block.Start)
            {
                return blockIndex;
            }

            RemoveFreeBlockFromBins(previousIndex);
            previous.Count += block.Count;
            _blocks[previousIndex] = previous;

            UnlinkByAddress(blockIndex);
            RecycleBlock(blockIndex);

            return previousIndex;
        }

        private int MergeWithNext(int blockIndex)
        {
            var block = _blocks[blockIndex];
            var nextIndex = block.NextByAddress;
            if (nextIndex == INVALID_INDEX)
            {
                return blockIndex;
            }

            var next = _blocks[nextIndex];
            if (next.IsAlive == 0 || next.IsFree == 0 || block.Start + block.Count != next.Start)
            {
                return blockIndex;
            }

            RemoveFreeBlockFromBins(nextIndex);

            block.Count += next.Count;
            _blocks[blockIndex] = block;

            UnlinkByAddress(nextIndex);
            RecycleBlock(nextIndex);

            return blockIndex;
        }

        private void TrimFreeTail()
        {
            while (_addressTail != INVALID_INDEX)
            {
                var tailIndex = _addressTail;
                var tailBlock = _blocks[tailIndex];
                if (tailBlock.IsAlive == 0 || tailBlock.IsFree == 0)
                {
                    break;
                }

                RemoveFreeBlockFromBins(tailIndex);
                _usedLength = tailBlock.Start;
                _data.Resize(GetByteOffset(_usedLength));

                UnlinkByAddress(tailIndex);
                RecycleBlock(tailIndex);
            }
        }

        private int FindBestFitFreeBlock(int requestedCount)
        {
            var startBin = GetBinIndex(requestedCount);
            var bestIndex = INVALID_INDEX;
            var bestSize = int.MaxValue;
            var bestStart = int.MaxValue;

            for (var bin = startBin; bin < BIN_COUNT; bin++)
            {
                var current = _freeBinHeads[bin];
                while (current != INVALID_INDEX)
                {
                    var block = _blocks[current];
                    if (block.IsAlive != 0 && block.IsFree != 0 && block.Count >= requestedCount)
                    {
                        if (block.Count < bestSize || (block.Count == bestSize && block.Start < bestStart))
                        {
                            bestIndex = current;
                            bestSize = block.Count;
                            bestStart = block.Start;
                        }
                    }

                    current = block.NextFree;
                }

                if (bestIndex != INVALID_INDEX)
                {
                    if (bestSize == requestedCount)
                    {
                        return bestIndex;
                    }

                    if (bin + 1 < BIN_COUNT)
                    {
                        var nextBinLowerBound = GetBinLowerBound(bin + 1);
                        if (nextBinLowerBound > bestSize)
                        {
                            return bestIndex;
                        }
                    }
                }
            }

            return bestIndex;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdateRangeOnAllocate(int start, int count)
        {
            var end = start + count - 1;
            if (_allocationCount == 1)
            {
                _minAllocatedStart = start;
                _maxAllocatedEnd = end;
                return;
            }

            _minAllocatedStart = math.min(_minAllocatedStart, start);
            _maxAllocatedEnd = math.max(_maxAllocatedEnd, end);
        }

        private void RecomputeValidRange()
        {
            var firstAllocated = _addressHead;
            while (firstAllocated != INVALID_INDEX && _blocks[firstAllocated].IsFree != 0)
            {
                firstAllocated = _blocks[firstAllocated].NextByAddress;
            }

            var lastAllocated = _addressTail;
            while (lastAllocated != INVALID_INDEX && _blocks[lastAllocated].IsFree != 0)
            {
                lastAllocated = _blocks[lastAllocated].PrevByAddress;
            }

            if (firstAllocated == INVALID_INDEX || lastAllocated == INVALID_INDEX)
            {
                throw new InvalidOperationException("Allocator tracking state is inconsistent.");
            }

            var first = _blocks[firstAllocated];
            var last = _blocks[lastAllocated];

            _minAllocatedStart = first.Start;
            _maxAllocatedEnd = last.Start + last.Count - 1;
        }

        private void EnsureLength(int requiredLength)
        {
            EnsureDataCapacity(requiredLength);
            var requiredBytes = GetByteOffset(requiredLength);
            if (_data.Length < requiredBytes)
            {
                _data.Resize(requiredBytes);
            }
        }

        private void EnsureDataCapacity(int requiredCapacity)
        {
            var requiredBytes = GetByteOffset(requiredCapacity);
            if (_data.Capacity >= requiredBytes)
            {
                return;
            }

            var minGrowthBytes = GetRequiredBytes(4, _stride);
            long newCapacity = math.max(minGrowthBytes, _data.Capacity);
            while (newCapacity < requiredBytes)
            {
                newCapacity <<= 1;
                if (newCapacity > int.MaxValue)
                {
                    newCapacity = requiredBytes;
                    break;
                }
            }

            _data.Capacity = (int)newCapacity;
        }

        private int CreateBlock(int start, int count, bool isFree)
        {
            var index = RentBlockIndex();
            _blocks[index] = new Block
            {
                Start = start,
                Count = count,
                PrevByAddress = INVALID_INDEX,
                NextByAddress = INVALID_INDEX,
                PrevFree = INVALID_INDEX,
                NextFree = INVALID_INDEX,
                IsFree = (byte)(isFree ? 1 : 0),
                IsAlive = 1,
            };
            return index;
        }

        private int RentBlockIndex()
        {
            if (_recycledBlockIndices.Length > 0)
            {
                var lastIndex = _recycledBlockIndices.Length - 1;
                var blockIndex = _recycledBlockIndices[lastIndex];
                _recycledBlockIndices.Resize(lastIndex);
                return blockIndex;
            }

            var index = _blocks.Length;
            _blocks.Resize(index + 1);
            return index;
        }

        private void RecycleBlock(int blockIndex)
        {
            var block = _blocks[blockIndex];
            block.IsAlive = 0;
            block.IsFree = 0;
            block.PrevByAddress = INVALID_INDEX;
            block.NextByAddress = INVALID_INDEX;
            block.PrevFree = INVALID_INDEX;
            block.NextFree = INVALID_INDEX;
            _blocks[blockIndex] = block;
            _recycledBlockIndices.Add(blockIndex);
        }

        private void AppendByAddress(int blockIndex)
        {
            var block = _blocks[blockIndex];
            block.PrevByAddress = _addressTail;
            block.NextByAddress = INVALID_INDEX;
            _blocks[blockIndex] = block;

            if (_addressTail != INVALID_INDEX)
            {
                var tail = _blocks[_addressTail];
                tail.NextByAddress = blockIndex;
                _blocks[_addressTail] = tail;
            }
            else
            {
                _addressHead = blockIndex;
            }

            _addressTail = blockIndex;
        }

        private void InsertAfterByAddress(int previousIndex, int insertedIndex)
        {
            var previous = _blocks[previousIndex];
            var nextIndex = previous.NextByAddress;

            var inserted = _blocks[insertedIndex];
            inserted.PrevByAddress = previousIndex;
            inserted.NextByAddress = nextIndex;
            _blocks[insertedIndex] = inserted;

            previous.NextByAddress = insertedIndex;
            _blocks[previousIndex] = previous;

            if (nextIndex != INVALID_INDEX)
            {
                var next = _blocks[nextIndex];
                next.PrevByAddress = insertedIndex;
                _blocks[nextIndex] = next;
            }
            else
            {
                _addressTail = insertedIndex;
            }
        }

        private void UnlinkByAddress(int blockIndex)
        {
            var block = _blocks[blockIndex];
            var previousIndex = block.PrevByAddress;
            var nextIndex = block.NextByAddress;

            if (previousIndex != INVALID_INDEX)
            {
                var previous = _blocks[previousIndex];
                previous.NextByAddress = nextIndex;
                _blocks[previousIndex] = previous;
            }
            else
            {
                _addressHead = nextIndex;
            }

            if (nextIndex != INVALID_INDEX)
            {
                var next = _blocks[nextIndex];
                next.PrevByAddress = previousIndex;
                _blocks[nextIndex] = next;
            }
            else
            {
                _addressTail = previousIndex;
            }

            block.PrevByAddress = INVALID_INDEX;
            block.NextByAddress = INVALID_INDEX;
            _blocks[blockIndex] = block;
        }

        private void AddFreeBlockToBins(int blockIndex)
        {
            var block = _blocks[blockIndex];
            if (block.IsAlive == 0 || block.IsFree == 0)
            {
                throw new InvalidOperationException("Attempted to add a non-free block to free bins.");
            }

            var bin = GetBinIndex(block.Count);
            var head = _freeBinHeads[bin];

            block.PrevFree = INVALID_INDEX;
            block.NextFree = head;
            _blocks[blockIndex] = block;

            if (head != INVALID_INDEX)
            {
                var headBlock = _blocks[head];
                headBlock.PrevFree = blockIndex;
                _blocks[head] = headBlock;
            }

            _freeBinHeads[bin] = blockIndex;
        }

        private void RemoveFreeBlockFromBins(int blockIndex)
        {
            var block = _blocks[blockIndex];
            if (block.IsAlive == 0 || block.IsFree == 0)
            {
                return;
            }

            var bin = GetBinIndex(block.Count);

            if (block.PrevFree != INVALID_INDEX)
            {
                var previous = _blocks[block.PrevFree];
                previous.NextFree = block.NextFree;
                _blocks[block.PrevFree] = previous;
            }
            else if (_freeBinHeads[bin] == blockIndex)
            {
                _freeBinHeads[bin] = block.NextFree;
            }

            if (block.NextFree != INVALID_INDEX)
            {
                var next = _blocks[block.NextFree];
                next.PrevFree = block.PrevFree;
                _blocks[block.NextFree] = next;
            }

            block.PrevFree = INVALID_INDEX;
            block.NextFree = INVALID_INDEX;
            _blocks[blockIndex] = block;
        }

        private bool TryGetAllocatedBlockIndex(MemoryPtr ptr, out int blockIndex)
        {
            blockIndex = INVALID_INDEX;

            if (!ptr.IsValid)
            {
                return false;
            }

            if (!_allocatedByStart.TryGetValue(ptr.StartIndex, out blockIndex))
            {
                return false;
            }

            var block = _blocks[blockIndex];
            return block.IsAlive != 0 && block.IsFree == 0 && block.Count == ptr.Count;
        }

        private void CheckElementIndex(MemoryPtr ptr, int index, out int absoluteIndex)
        {
            if (!TryGetAllocatedBlockIndex(ptr, out _))
            {
                throw new ArgumentException("MemoryPtr is not currently allocated.", nameof(ptr));
            }

            if ((uint)index >= (uint)ptr.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index), $"Index must be in [0, {ptr.Count - 1}].");
            }

            absoluteIndex = ptr.StartIndex + index;
        }

        private void CheckMemoryPtrForFree(MemoryPtr ptr, out int blockIndex)
        {
            if (!ptr.IsValid)
            {
                throw new ArgumentException("MemoryPtr is invalid.", nameof(ptr));
            }

            if (!_allocatedByStart.TryGetValue(ptr.StartIndex, out blockIndex))
            {
                throw new InvalidOperationException("MemoryPtr is not currently allocated.");
            }

            var block = _blocks[blockIndex];
            if (block.IsAlive == 0 || block.IsFree != 0)
            {
                throw new InvalidOperationException("MemoryPtr is not currently allocated.");
            }

            if (block.Count != ptr.Count)
            {
                throw new ArgumentException("MemoryPtr count does not match the active allocation.", nameof(ptr));
            }
        }

        private void ResetFreeBinHeads()
        {
            for (var i = 0; i < BIN_COUNT; i++)
            {
                _freeBinHeads[i] = INVALID_INDEX;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int GetByteOffset(int elementIndex)
        {
            var bytes = (long)elementIndex * _stride;
            if (bytes > int.MaxValue)
            {
                throw new InvalidOperationException("Required byte offset exceeds supported size.");
            }

            return (int)bytes;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckTypeStride<T>()
            where T : unmanaged
        {
            var size = UnsafeUtility.SizeOf<T>();
            if (size != _stride)
            {
                throw new ArgumentException($"Type stride mismatch. Heap stride is {_stride} bytes but type '{typeof(T).Name}' is {size} bytes.");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckLengthStride(int byteLength)
        {
            if (byteLength % _stride != 0)
            {
                throw new ArgumentException($"Type stride mismatch. Heap stride is {_stride} bytes but passed byte length of {byteLength} is impossible with that stride");
            }
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetBinIndex(int size)
        {
            var index = 0;
            var value = size;

            while ((value >>= 1) != 0 && index < BIN_COUNT - 1)
            {
                index++;
            }

            return index;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetBinLowerBound(int bin)
        {
            return 1 << bin;
        }

        private static int GetRequiredBytes(int elementCount, int stride)
        {
            if (elementCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(elementCount), "Element count must be >= 0.");
            }

            var bytes = (long)elementCount * stride;
            if (bytes > int.MaxValue)
            {
                throw new InvalidOperationException("Requested size exceeds supported byte range.");
            }

            return (int)bytes;
        }

        private static void CheckStride(int stride)
        {
            if (stride <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(stride), "Stride must be > 0.");
            }
        }

        private static void CheckInitialCapacity(int initialCapacity)
        {
            if (initialCapacity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(initialCapacity), "Capacity must be >= 0.");
            }
        }

        private static void CheckAllocateCount(int count)
        {
            if (count <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), "Allocation count must be > 0.");
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Block
        {
            public int Start;
            public int Count;
            public int PrevByAddress;
            public int NextByAddress;
            public int PrevFree;
            public int NextFree;
            public byte IsFree;
            public byte IsAlive;
        }
    }
}
