using System;
using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;

namespace KrasCore.Tests
{
    public class UnsafeBitMaskRangeTests
    {
        [Test]
        public void EmptyRange_ReturnsNoIndices()
        {
            var range = new UnsafeBitMaskRange(256, Allocator.Persistent);

            try
            {
                Assert.That(range.Count, Is.EqualTo(0));
                Assert.That(range.TryGetFirstSet(out var first), Is.False);
                Assert.That(first, Is.EqualTo(-1));
                Assert.That(range.TryGetLastSet(out var last), Is.False);
                Assert.That(last, Is.EqualTo(-1));
                Assert.That(range.TryGetRange(out var start, out var end), Is.False);
                Assert.That(start, Is.EqualTo(-1));
                Assert.That(end, Is.EqualTo(-1));
            }
            finally
            {
                range.Dispose();
            }
        }

        [Test]
        public void SetSingleBit_ReturnsSameFirstLastAndRange()
        {
            var range = new UnsafeBitMaskRange(256, Allocator.Persistent);

            try
            {
                Assert.That(range.Set(123), Is.True);
                Assert.That(range.Count, Is.EqualTo(1));
                Assert.That(range.IsSet(123), Is.True);
                Assert.That(range.TryGetFirstSet(out var first), Is.True);
                Assert.That(first, Is.EqualTo(123));
                Assert.That(range.TryGetLastSet(out var last), Is.True);
                Assert.That(last, Is.EqualTo(123));
                Assert.That(range.TryGetRange(out int2 indexRange), Is.True);
                Assert.That(indexRange, Is.EqualTo(new int2(123, 123)));
            }
            finally
            {
                range.Dispose();
            }
        }

        [Test]
        public void SparseIndicesAcrossMultipleLevels_ReturnCorrectBounds()
        {
            var range = new UnsafeBitMaskRange(8192, Allocator.Persistent);

            try
            {
                range.Set(3);
                range.Set(4097);
                range.Set(8188);

                Assert.That(range.LevelCount, Is.EqualTo(3));
                Assert.That(range.TryGetFirstSet(out var first), Is.True);
                Assert.That(first, Is.EqualTo(3));
                Assert.That(range.TryGetLastSet(out var last), Is.True);
                Assert.That(last, Is.EqualTo(8188));
                Assert.That(range.TryGetRange(out var start, out var end), Is.True);
                Assert.That(start, Is.EqualTo(3));
                Assert.That(end, Is.EqualTo(8188));
            }
            finally
            {
                range.Dispose();
            }
        }

        [Test]
        public void UnsetBoundaryBits_PromotesNextLiveIndices()
        {
            var range = new UnsafeBitMaskRange(4096, Allocator.Persistent);

            try
            {
                range.Set(4);
                range.Set(130);
                range.Set(2048);
                range.Set(3070);

                Assert.That(range.Unset(4), Is.True);
                Assert.That(range.TryGetFirstSet(out var first), Is.True);
                Assert.That(first, Is.EqualTo(130));

                Assert.That(range.Unset(3070), Is.True);
                Assert.That(range.TryGetLastSet(out var last), Is.True);
                Assert.That(last, Is.EqualTo(2048));
            }
            finally
            {
                range.Dispose();
            }
        }

        [Test]
        public void SetUnsetAndClear_AreIdempotent()
        {
            var range = new UnsafeBitMaskRange(512, Allocator.Persistent);

            try
            {
                Assert.That(range.Set(10), Is.True);
                Assert.That(range.Set(10), Is.False);
                Assert.That(range.Count, Is.EqualTo(1));

                Assert.That(range.Unset(10), Is.True);
                Assert.That(range.Unset(10), Is.False);
                Assert.That(range.Count, Is.EqualTo(0));

                range.Set(100);
                range.Set(200);
                range.Clear();

                Assert.That(range.Count, Is.EqualTo(0));
                Assert.That(range.TryGetRange(out _, out _), Is.False);
            }
            finally
            {
                range.Dispose();
            }
        }

        [Test]
        public void NegativeLength_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                _ = new UnsafeBitMaskRange(-1, Allocator.Persistent);
            });
        }

        [Test]
        public void OutOfRangeIndex_Throws()
        {
            var range = new UnsafeBitMaskRange(32, Allocator.Persistent);

            try
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => range.Set(32));
                Assert.Throws<ArgumentOutOfRangeException>(() => range.IsSet(-1));
                Assert.Throws<ArgumentOutOfRangeException>(() => range.Unset(999));
            }
            finally
            {
                range.Dispose();
            }
        }
    }
}
