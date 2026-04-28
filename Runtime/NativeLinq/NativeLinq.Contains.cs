using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace KrasCore
{
    public static partial class NativeLinqExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Contains<T, TEnumerator>(this Query<T, TEnumerator> source, T value)
            where T : unmanaged, IEquatable<T>
            where TEnumerator : unmanaged, IEnumerator<T>
        {
            return source.Contains(value, new NativeEqualityComparer<T>());
        }
    }

    public partial struct Query<T, TEnumerator>
        where T : unmanaged
        where TEnumerator : unmanaged, IEnumerator<T>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains<TEqualityComparer>(T value, TEqualityComparer comparer)
            where TEqualityComparer : unmanaged, INativeEqualityComparer<T>
        {
            var enumerator = GetEnumerator();
            while (enumerator.MoveNext())
            {
                var current = enumerator.Current;
                if (comparer.Equals(in current, in value))
                {
                    enumerator.Dispose();
                    return true;
                }
            }

            enumerator.Dispose();
            return false;
        }
    }
}
