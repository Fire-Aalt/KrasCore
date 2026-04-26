using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace KrasCore
{
    public partial struct NativeEnumerable<T, TEnumerator>
        where T : unmanaged
        where TEnumerator : unmanaged, IEnumerator<T>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeWhereEnumerable<T, Enumerator, TPredicate> Where<TPredicate>(TPredicate predicate)
            where TPredicate : unmanaged, IPredicate<T>
        {
            return new NativeWhereEnumerable<T, Enumerator, TPredicate>(GetEnumerator(), predicate);
        }
    }

    public partial struct NativeWhereEnumerable<T, TEnumerator, TPredicate>
        where T : unmanaged
        where TEnumerator : unmanaged, IEnumerator<T>
        where TPredicate : unmanaged, IPredicate<T>
    {
        private TEnumerator _enumerator;
        private TPredicate _predicate;

        public NativeWhereEnumerable(TEnumerator enumerator, TPredicate predicate)
        {
            _enumerator = enumerator;
            _predicate = predicate;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Enumerator GetEnumerator()
        {
            return new Enumerator(_enumerator, _predicate);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeWhereEnumerable<T, Enumerator, TNextPredicate> Where<TNextPredicate>(TNextPredicate predicate)
            where TNextPredicate : unmanaged, IPredicate<T>
        {
            return new NativeWhereEnumerable<T, Enumerator, TNextPredicate>(GetEnumerator(), predicate);
        }

        public struct Enumerator : IEnumerator<T>
        {
            private TEnumerator _source;
            private TPredicate _predicate;
            private T _current;

            public Enumerator(TEnumerator source, TPredicate predicate)
            {
                _source = source;
                _predicate = predicate;
                _current = default;
            }

            public T Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _current;
            }

            object System.Collections.IEnumerator.Current => Current;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                while (_source.MoveNext())
                {
                    var value = _source.Current;
                    if (!_predicate.Match(in value))
                    {
                        continue;
                    }

                    _current = value;
                    return true;
                }

                return false;
            }

            public void Reset()
            {
                _source.Reset();
                _current = default;
            }

            public void Dispose()
            {
                _source.Dispose();
            }
        }
    }

    public partial struct NativeSelectEnumerable<TSource, TResult, TEnumerator, TSelector>
        where TSource : unmanaged
        where TResult : unmanaged
        where TEnumerator : unmanaged, IEnumerator<TSource>
        where TSelector : unmanaged, ISelector<TSource, TResult>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeWhereEnumerable<TResult, Enumerator, TPredicate> Where<TPredicate>(TPredicate predicate)
            where TPredicate : unmanaged, IPredicate<TResult>
        {
            return new NativeWhereEnumerable<TResult, Enumerator, TPredicate>(GetEnumerator(), predicate);
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
        public NativeWhereEnumerable<TResult, Enumerator, TPredicate> Where<TPredicate>(TPredicate predicate)
            where TPredicate : unmanaged, IPredicate<TResult>
        {
            return new NativeWhereEnumerable<TResult, Enumerator, TPredicate>(GetEnumerator(), predicate);
        }
    }
}


