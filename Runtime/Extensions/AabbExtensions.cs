using System.Runtime.CompilerServices;
using Unity.Mathematics;

namespace KrasCore
{
    public static class AabbExtensions
    {
        /// <summary>
        /// Returns true if two AABBs overlap in 3D (partial or full containment).
        /// Touching faces/edges/corners counts as overlap.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Overlaps(in this AABB a, in AABB b)
        {
            // Separating Axis Theorem for AABBs:
            // no overlap if one is strictly on one side along any axis.
            var aMin = a.Min;
            var aMax = a.Max;
            var bMin = b.Min;
            var bMax = b.Max;

            return (aMin.x <= bMax.x) & (aMax.x >= bMin.x) &
                   (aMin.y <= bMax.y) & (aMax.y >= bMin.y) &
                   (aMin.z <= bMax.z) & (aMax.z >= bMin.z);
        }
    }
}