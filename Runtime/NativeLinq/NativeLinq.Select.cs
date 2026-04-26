using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace KrasCore
{
    public partial struct NativeEnumerable<T, TEnumerator>
        where T : unmanaged
        where TEnumerator : unmanaged, IEnumerator<T>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeSelectEnumerable<T, TResult, Enumerator, TSelector> Select<TResult, TSelector>(TSelector selector)
            where TResult : unmanaged
            where TSelector : unmanaged, ISelector<T, TResult>
        {
            return new NativeSelectEnumerable<T, TResult, Enumerator, TSelector>(GetEnumerator(), selector);
        }
    }

    public partial struct NativeWhereEnumerable<T, TEnumerator, TPredicate>
        where T : unmanaged
        where TEnumerator : unmanaged, IEnumerator<T>
        where TPredicate : unmanaged, IPredicate<T>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeSelectEnumerable<T, TResult, Enumerator, TSelector> Select<TResult, TSelector>(TSelector selector)
            where TResult : unmanaged
            where TSelector : unmanaged, ISelector<T, TResult>
        {
            return new NativeSelectEnumerable<T, TResult, Enumerator, TSelector>(GetEnumerator(), selector);
        }
    }

    public partial struct NativeSelectEnumerable<TSource, TResult, TEnumerator, TSelector>
        where TSource : unmanaged
        where TResult : unmanaged
        where TEnumerator : unmanaged, IEnumerator<TSource>
        where TSelector : unmanaged, ISelector<TSource, TResult>
    {
        private TEnumerator _enumerator;
        private TSelector _selector;

        public NativeSelectEnumerable(TEnumerator enumerator, TSelector selector)
        {
            _enumerator = enumerator;
            _selector = selector;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Enumerator GetEnumerator()
        {
            return new Enumerator(_enumerator, _selector);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeSelectEnumerable<TResult, TNextResult, Enumerator, TNextSelector> Select<TNextResult, TNextSelector>(TNextSelector selector)
            where TNextResult : unmanaged
            where TNextSelector : unmanaged, ISelector<TResult, TNextResult>
        {
            return new NativeSelectEnumerable<TResult, TNextResult, Enumerator, TNextSelector>(GetEnumerator(), selector);
        }

        public struct Enumerator : IEnumerator<TResult>
        {
            private TEnumerator _source;
            private TSelector _selector;

            public Enumerator(TEnumerator source, TSelector selector)
            {
                _source = source;
                _selector = selector;
            }

            public TResult Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    var value = _source.Current;
                    return _selector.Select(in value);
                }
            }

            object System.Collections.IEnumerator.Current => Current;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                return _source.MoveNext();
            }

            public void Reset()
            {
                _source.Reset();
            }

            public void Dispose()
            {
                _source.Dispose();
            }
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
        public NativeSelectEnumerable<TResult, TNextResult, Enumerator, TNextSelector> Select<TNextResult, TNextSelector>(TNextSelector selector)
            where TNextResult : unmanaged
            where TNextSelector : unmanaged, ISelector<TResult, TNextResult>
        {
            return new NativeSelectEnumerable<TResult, TNextResult, Enumerator, TNextSelector>(GetEnumerator(), selector);
        }
    }
}


