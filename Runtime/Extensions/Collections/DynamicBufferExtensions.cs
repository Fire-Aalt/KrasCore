using System;
using Unity.Entities;

namespace KrasCore
{
    public static class DynamicBufferExtensions
    {
        public static int First<T>(this DynamicBuffer<T> list, T element)
            where T : unmanaged, IEquatable<T>
        {
            for (int i = list.Length - 1; i >= 0; i--)
            {
                if (list[i].GetHashCode() == element.GetHashCode() && list[i].Equals(element))
                {
                    return i;
                }
            }
            return -1;
        }
    }
}