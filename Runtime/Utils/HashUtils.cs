using Unity.Mathematics;

namespace KrasCore
{
    public class HashUtils
    {
        /// <summary>
        /// Hash function for seeded random seeds, unique for each position
        /// </summary>
        public static uint Simple(uint seed, in int2 pos)
        {
            // Combine position into a single 64-bit value for better hash distribution
            ulong combined = ((ulong)pos.x << 32) | (uint)pos.y;
            uint hash = seed;

            hash ^= (uint)(combined & 0xFFFFFFFF); 
            hash *= 0x85EBCA6B;
            hash ^= hash >> 13;
            hash ^= (uint)(combined >> 32);       
            hash *= 0xC2B2AE35;
            hash ^= hash >> 16;

            return hash + 1;
        }
    }
}