using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace KrasCore
{
    public partial struct Query<T, TEnumerator>
        where T : unmanaged
        where TEnumerator : unmanaged, IEnumerator<T>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Query<T, SelectQuery<T, T, TEnumerator, TSelector>> Select<TSelector>(TSelector selector)
            where TSelector : unmanaged, ISelector<T, T>
        {
            return new Query<T, SelectQuery<T, T, TEnumerator, TSelector>>(
                new SelectQuery<T, T, TEnumerator, TSelector>(GetEnumerator(), selector));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Query<TResult, SelectQuery<T, TResult, TEnumerator, TSelector>> Select<TResult, TSelector>(TSelector selector)
            where TResult : unmanaged
            where TSelector : unmanaged, ISelector<T, TResult>
        {
            return new Query<TResult, SelectQuery<T, TResult, TEnumerator, TSelector>>(
                new SelectQuery<T, TResult, TEnumerator, TSelector>(GetEnumerator(), selector));
        }
    }

    public struct SelectQuery<TSource, TResult, TEnumerator, TSelector> : IEnumerator<TResult>
        where TSource : unmanaged
        where TResult : unmanaged
        where TEnumerator : unmanaged, IEnumerator<TSource>
        where TSelector : unmanaged, ISelector<TSource, TResult>
    {
        private TEnumerator _source;
        private TSelector _selector;

        public SelectQuery(TEnumerator source, TSelector selector)
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
