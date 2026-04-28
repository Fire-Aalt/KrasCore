using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace KrasCore
{
    public static partial class NativeLinqExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Sum<T, TEnumerator, TAccumulator>(this Query<T, TEnumerator> source)
            where T : unmanaged
            where TEnumerator : unmanaged, IEnumerator<T>
            where TAccumulator : unmanaged, IAccumulator<T>
        {
            return NativeLinqUtilities.Sum<T, TEnumerator, TAccumulator>(source.GetEnumerator(), default);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Average<T, TEnumerator, TAccumulator>(this Query<T, TEnumerator> source)
            where T : unmanaged
            where TEnumerator : unmanaged, IEnumerator<T>
            where TAccumulator : unmanaged, IAccumulator<T>
        {
            return NativeLinqUtilities.Average<T, TEnumerator, TAccumulator>(source.GetEnumerator(), default);
        }
    }

    public partial struct Query<T, TEnumerator>
        where T : unmanaged
        where TEnumerator : unmanaged, IEnumerator<T>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Sum<TAccumulator>(TAccumulator accumulator)
            where TAccumulator : unmanaged, IAccumulator<T>
        {
            return NativeLinqUtilities.Sum<T, TEnumerator, TAccumulator>(GetEnumerator(), accumulator);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Average<TAccumulator>(TAccumulator accumulator)
            where TAccumulator : unmanaged, IAccumulator<T>
        {
            return NativeLinqUtilities.Average<T, TEnumerator, TAccumulator>(GetEnumerator(), accumulator);
        }
    }

    internal static partial class NativeLinqUtilities
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Sum<T, TEnumerator, TAccumulator>(TEnumerator enumerator, TAccumulator accumulator)
            where T : unmanaged
            where TEnumerator : unmanaged, IEnumerator<T>
            where TAccumulator : unmanaged, IAccumulator<T>
        {
            var total = default(T);
            while (enumerator.MoveNext())
            {
                var value = enumerator.Current;
                total = accumulator.Add(in total, in value);
            }

            enumerator.Dispose();
            return total;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Average<T, TEnumerator, TAccumulator>(TEnumerator enumerator, TAccumulator accumulator)
            where T : unmanaged
            where TEnumerator : unmanaged, IEnumerator<T>
            where TAccumulator : unmanaged, IAccumulator<T>
        {
            var total = default(T);
            var count = 0u;
            while (enumerator.MoveNext())
            {
                var value = enumerator.Current;
                total = accumulator.Add(in total, in value);
                count++;
            }

            enumerator.Dispose();
            return count == 0u ? default : accumulator.Divide(in total, count);
        }
    }
}
