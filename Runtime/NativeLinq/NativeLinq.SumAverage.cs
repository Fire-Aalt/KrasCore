using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace KrasCore
{
    public partial struct NativeEnumerable<T, TEnumerator>
        where T : unmanaged
        where TEnumerator : unmanaged, IEnumerator<T>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Sum<TAccumulator>(TAccumulator accumulator)
            where TAccumulator : unmanaged, INativeAccumulator<T>
        {
            return NativeLinqUtilities.Sum<T, Enumerator, TAccumulator>(GetEnumerator(), accumulator);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Average<TAccumulator>(TAccumulator accumulator)
            where TAccumulator : unmanaged, INativeAccumulator<T>
        {
            return NativeLinqUtilities.Average<T, Enumerator, TAccumulator>(GetEnumerator(), accumulator);
        }
    }

    public partial struct NativeWhereEnumerable<T, TEnumerator, TPredicate>
        where T : unmanaged
        where TEnumerator : unmanaged, IEnumerator<T>
        where TPredicate : unmanaged, IPredicate<T>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Sum<TAccumulator>(TAccumulator accumulator)
            where TAccumulator : unmanaged, INativeAccumulator<T>
        {
            return NativeLinqUtilities.Sum<T, Enumerator, TAccumulator>(GetEnumerator(), accumulator);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Average<TAccumulator>(TAccumulator accumulator)
            where TAccumulator : unmanaged, INativeAccumulator<T>
        {
            return NativeLinqUtilities.Average<T, Enumerator, TAccumulator>(GetEnumerator(), accumulator);
        }
    }

    public partial struct NativeSelectEnumerable<TSource, TResult, TEnumerator, TSelector>
        where TSource : unmanaged
        where TResult : unmanaged
        where TEnumerator : unmanaged, IEnumerator<TSource>
        where TSelector : unmanaged, ISelector<TSource, TResult>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TResult Sum<TAccumulator>(TAccumulator accumulator)
            where TAccumulator : unmanaged, INativeAccumulator<TResult>
        {
            return NativeLinqUtilities.Sum<TResult, Enumerator, TAccumulator>(GetEnumerator(), accumulator);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TResult Average<TAccumulator>(TAccumulator accumulator)
            where TAccumulator : unmanaged, INativeAccumulator<TResult>
        {
            return NativeLinqUtilities.Average<TResult, Enumerator, TAccumulator>(GetEnumerator(), accumulator);
        }
    }

    public partial struct NativeSelectManyEnumerable<TSource, TResult, TSourceEnumerator, TInnerEnumerator, TSelector>
        where TSource : unmanaged
        where TResult : unmanaged
        where TSourceEnumerator : unmanaged, IEnumerator<TSource>
        where TInnerEnumerator : unmanaged, IEnumerator<TResult>
        where TSelector : unmanaged, ISelector<TSource, TInnerEnumerator>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TResult Sum<TAccumulator>(TAccumulator accumulator)
            where TAccumulator : unmanaged, INativeAccumulator<TResult>
        {
            return NativeLinqUtilities.Sum<TResult, Enumerator, TAccumulator>(GetEnumerator(), accumulator);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TResult Average<TAccumulator>(TAccumulator accumulator)
            where TAccumulator : unmanaged, INativeAccumulator<TResult>
        {
            return NativeLinqUtilities.Average<TResult, Enumerator, TAccumulator>(GetEnumerator(), accumulator);
        }
    }

    internal static partial class NativeLinqUtilities
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Sum<T, TEnumerator, TAccumulator>(TEnumerator enumerator, TAccumulator accumulator)
            where T : unmanaged
            where TEnumerator : unmanaged, IEnumerator<T>
            where TAccumulator : unmanaged, INativeAccumulator<T>
        {
            var total = accumulator.Zero();
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
            where TAccumulator : unmanaged, INativeAccumulator<T>
        {
            var total = accumulator.Zero();
            var count = 0;
            while (enumerator.MoveNext())
            {
                var value = enumerator.Current;
                total = accumulator.Add(in total, in value);
                count++;
            }

            enumerator.Dispose();
            return count == 0 ? default : accumulator.Divide(in total, count);
        }
    }
}
