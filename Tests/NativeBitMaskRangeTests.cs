using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;

namespace KrasCore.Tests
{
    public class NativeBitMaskRangeTests
    {
        [Test]
        public void SetUnsetAndClear_TracksCountAndBounds()
        {
            var range = new NativeBitMaskRange(512, Allocator.Persistent);

            try
            {
                Assert.That(range.Set(10), Is.True);
                Assert.That(range.Set(10), Is.False);
                Assert.That(range.Set(200), Is.True);
                Assert.That(range.Set(511), Is.True);

                Assert.That(range.Count, Is.EqualTo(3));
                Assert.That(range.TryGetRange(out var start, out var end), Is.True);
                Assert.That(start, Is.EqualTo(10));
                Assert.That(end, Is.EqualTo(511));
                Assert.That(range.TryGetRange(out int2 bounds), Is.True);
                Assert.That(bounds, Is.EqualTo(new int2(10, 511)));

                Assert.That(range.Unset(511), Is.True);
                Assert.That(range.TryGetLastSet(out var last), Is.True);
                Assert.That(last, Is.EqualTo(200));

                range.Clear();
                Assert.That(range.Count, Is.Zero);
                Assert.That(range.TryGetRange(out _, out _), Is.False);
            }
            finally
            {
                range.Dispose();
            }
        }

        [Test]
        public void Dispose_AccessAfterDispose_ThrowsWithCollectionChecks()
        {
            var range = new NativeBitMaskRange(16, Allocator.Persistent);
            range.Set(1);
            range.Dispose();

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            Assert.That(() => range.Set(2), Throws.Exception);
            Assert.That(() => _ = range.Count, Throws.Exception);
#else
            Assert.Pass("Collection checks are disabled.");
#endif
        }
    }
}
