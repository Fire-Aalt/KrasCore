using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace KrasCore
{
    [StructLayout(LayoutKind.Sequential)]
    [DebuggerDisplay("Count = {Count}, Length = {Length}, Levels = {LevelCount}, IsCreated = {IsCreated}")]
    public unsafe struct UnsafeBitMaskRange : IDisposable
    {
        private UnsafeList<ulong> _words;
        private UnsafeList<int> _levelOffsets;
        private int _length;
        private int _count;

        public int Length => _length;

        public int Count => _count;

        public int LevelCount => _levelOffsets.IsCreated ? _levelOffsets.Length : 0;

        public bool IsCreated => _words.IsCreated;

        public UnsafeBitMaskRange(AllocatorManager.AllocatorHandle allocator)
            : this(0, allocator)
        {
        }

        public UnsafeBitMaskRange(int length, AllocatorManager.AllocatorHandle allocator)
        {
            CheckLength(length);

            _words = new UnsafeList<ulong>(0, allocator);
            _levelOffsets = new UnsafeList<int>(0, allocator);
            _length = length;
            _count = 0;

            if (length == 0)
            {
                return;
            }

            var levelCount = CalculateLevelCount(length);
            var totalWords = CalculateTotalWordCount(length);

            _words.Resize(totalWords, NativeArrayOptions.ClearMemory);
            _levelOffsets.Resize(levelCount, NativeArrayOptions.UninitializedMemory);
            var wordCount = GetWordCount(length);
            var offset = 0;
            for (var level = 0; level < levelCount; level++)
            {
                _levelOffsets[level] = offset;
                offset += wordCount;

                if (wordCount == 1)
                {
                    break;
                }

                wordCount = GetWordCount(wordCount);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Set(int index)
        {
            CheckIndex(index);

            var leafWordIndex = index >> 6;
            var bitIndex = index & 63;
            var mask = 1UL << bitIndex;

            ref var leafWord = ref GetWordRef(0, leafWordIndex);
            if ((leafWord & mask) != 0)
            {
                return false;
            }

            leafWord |= mask;
            _count++;

            var childWordIndex = leafWordIndex;
            for (var level = 1; level < LevelCount; level++)
            {
                var parentWordIndex = childWordIndex >> 6;
                var parentMask = 1UL << (childWordIndex & 63);

                ref var parentWord = ref GetWordRef(level, parentWordIndex);
                if ((parentWord & parentMask) != 0)
                {
                    break;
                }

                parentWord |= parentMask;
                childWordIndex = parentWordIndex;
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Unset(int index)
        {
            CheckIndex(index);

            var leafWordIndex = index >> 6;
            var bitIndex = index & 63;
            var mask = 1UL << bitIndex;

            ref var leafWord = ref GetWordRef(0, leafWordIndex);
            if ((leafWord & mask) == 0)
            {
                return false;
            }

            leafWord &= ~mask;
            _count--;

            var childWordIndex = leafWordIndex;
            for (var level = 1; level < LevelCount; level++)
            {
                if (GetWord(level - 1, childWordIndex) != 0)
                {
                    break;
                }

                var parentWordIndex = childWordIndex >> 6;
                var parentMask = 1UL << (childWordIndex & 63);

                ref var parentWord = ref GetWordRef(level, parentWordIndex);
                parentWord &= ~parentMask;
                childWordIndex = parentWordIndex;
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsSet(int index)
        {
            CheckIndex(index);

            var word = GetWord(0, index >> 6);
            var mask = 1UL << (index & 63);
            return (word & mask) != 0;
        }

        public void Clear()
        {
            if (!_words.IsCreated || _words.Length == 0)
            {
                _count = 0;
                return;
            }

            UnsafeUtility.MemClear(_words.Ptr, _words.Length * sizeof(ulong));
            _count = 0;
        }

        public bool TryGetFirstSet(out int index)
        {
            if (_count == 0 || _length == 0)
            {
                index = -1;
                return false;
            }

            var wordIndex = 0;
            for (var level = LevelCount - 1; level > 0; level--)
            {
                var summaryWord = GetWord(level, wordIndex);
                var childBitIndex = TrailingZeroCount(summaryWord);
                wordIndex = (wordIndex << 6) + childBitIndex;
            }

            var leafWord = GetWord(0, wordIndex);
            index = (wordIndex << 6) + TrailingZeroCount(leafWord);
            return true;
        }

        public bool TryGetLastSet(out int index)
        {
            if (_count == 0 || _length == 0)
            {
                index = -1;
                return false;
            }

            var wordIndex = 0;
            for (var level = LevelCount - 1; level > 0; level--)
            {
                var summaryWord = GetWord(level, wordIndex);
                var childBitIndex = MostSignificantBitIndex(summaryWord);
                wordIndex = (wordIndex << 6) + childBitIndex;
            }

            var leafWord = GetWord(0, wordIndex);
            index = (wordIndex << 6) + MostSignificantBitIndex(leafWord);
            return true;
        }

        public bool TryGetRange(out int startIndex, out int endIndex)
        {
            if (!TryGetFirstSet(out startIndex))
            {
                endIndex = -1;
                return false;
            }

            TryGetLastSet(out endIndex);
            return true;
        }

        public bool TryGetRange(out int2 range)
        {
            if (!TryGetRange(out var startIndex, out var endIndex))
            {
                range = default;
                return false;
            }

            range = new int2(startIndex, endIndex);
            return true;
        }

        public void Dispose()
        {
            if (_words.IsCreated)
            {
                _words.Dispose();
            }

            if (_levelOffsets.IsCreated)
            {
                _levelOffsets.Dispose();
            }

            _length = 0;
            _count = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ulong GetWord(int level, int wordIndex)
        {
            return UnsafeUtility.AsRef<ulong>((byte*)_words.Ptr + ((_levelOffsets[level] + wordIndex) * sizeof(ulong)));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ref ulong GetWordRef(int level, int wordIndex)
        {
            return ref UnsafeUtility.AsRef<ulong>((byte*)_words.Ptr + ((_levelOffsets[level] + wordIndex) * sizeof(ulong)));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetWordCount(int bitCount)
        {
            return (bitCount + 63) >> 6;
        }

        private static int CalculateLevelCount(int bitCount)
        {
            var levelCount = 0;
            var wordCount = GetWordCount(bitCount);

            while (wordCount > 0)
            {
                levelCount++;
                if (wordCount == 1)
                {
                    break;
                }

                wordCount = GetWordCount(wordCount);
            }

            return levelCount;
        }

        private static int CalculateTotalWordCount(int bitCount)
        {
            var totalWordCount = 0;
            var wordCount = GetWordCount(bitCount);

            while (wordCount > 0)
            {
                totalWordCount += wordCount;
                if (wordCount == 1)
                {
                    break;
                }

                wordCount = GetWordCount(wordCount);
            }

            return totalWordCount;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int TrailingZeroCount(ulong value)
        {
            var count = 0;

            if ((value & 0x00000000FFFFFFFFUL) == 0)
            {
                count += 32;
                value >>= 32;
            }

            if ((value & 0x000000000000FFFFUL) == 0)
            {
                count += 16;
                value >>= 16;
            }

            if ((value & 0x00000000000000FFUL) == 0)
            {
                count += 8;
                value >>= 8;
            }

            if ((value & 0x000000000000000FUL) == 0)
            {
                count += 4;
                value >>= 4;
            }

            if ((value & 0x0000000000000003UL) == 0)
            {
                count += 2;
                value >>= 2;
            }

            if ((value & 0x0000000000000001UL) == 0)
            {
                count += 1;
            }

            return count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int MostSignificantBitIndex(ulong value)
        {
            var index = 0;

            if ((value & 0xFFFFFFFF00000000UL) != 0)
            {
                index += 32;
                value >>= 32;
            }

            if ((value & 0x00000000FFFF0000UL) != 0)
            {
                index += 16;
                value >>= 16;
            }

            if ((value & 0x000000000000FF00UL) != 0)
            {
                index += 8;
                value >>= 8;
            }

            if ((value & 0x00000000000000F0UL) != 0)
            {
                index += 4;
                value >>= 4;
            }

            if ((value & 0x000000000000000CUL) != 0)
            {
                index += 2;
                value >>= 2;
            }

            if ((value & 0x0000000000000002UL) != 0)
            {
                index += 1;
            }

            return index;
        }

        private static void CheckLength(int length)
        {
            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length), "Length must be >= 0.");
            }
        }

        private void CheckIndex(int index)
        {
            if ((uint)index >= (uint)_length)
            {
                throw new ArgumentOutOfRangeException(nameof(index), $"Index must be in the range [0, {_length - 1}].");
            }
        }
    }
}
