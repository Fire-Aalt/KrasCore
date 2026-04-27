using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace KrasCore
{
    public partial struct Query<T, TEnumerator>
        where T : unmanaged
        where TEnumerator : unmanaged, IEnumerator<T>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Query<TResult, SelectManyQuery<T, TResult, TEnumerator, TInnerEnumerator, TSelector>> SelectMany<TResult, TInnerEnumerator, TSelector>(
            TSelector selector)
            where TResult : unmanaged
            where TInnerEnumerator : unmanaged, IEnumerator<TResult>
            where TSelector : unmanaged, ISelector<T, TInnerEnumerator>
        {
            return new Query<TResult, SelectManyQuery<T, TResult, TEnumerator, TInnerEnumerator, TSelector>>(
                new SelectManyQuery<T, TResult, TEnumerator, TInnerEnumerator, TSelector>(GetEnumerator(), selector));
        }
    }

    public struct SelectManyQuery<TSource, TResult, TSourceEnumerator, TInnerEnumerator, TSelector> : IEnumerator<TResult>
        where TSource : unmanaged
        where TResult : unmanaged
        where TSourceEnumerator : unmanaged, IEnumerator<TSource>
        where TInnerEnumerator : unmanaged, IEnumerator<TResult>
        where TSelector : unmanaged, ISelector<TSource, TInnerEnumerator>
    {
        private TSourceEnumerator _source;
        private TInnerEnumerator _inner;
        private TSelector _selector;
        private TResult _current;
        private bool _hasInner;

        public SelectManyQuery(TSourceEnumerator source, TSelector selector)
        {
            _source = source;
            _inner = default;
            _selector = selector;
            _current = default;
            _hasInner = false;
        }

        public TResult Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _current;
        }

        object System.Collections.IEnumerator.Current => Current;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            while (true)
            {
                if (_hasInner && _inner.MoveNext())
                {
                    _current = _inner.Current;
                    return true;
                }

                if (_hasInner)
                {
                    _inner.Dispose();
                }

                if (!_source.MoveNext())
                {
                    _hasInner = false;
                    return false;
                }

                var value = _source.Current;
                _inner = _selector.Select(in value);
                _hasInner = true;
            }
        }

        public void Reset()
        {
            if (_hasInner)
            {
                _inner.Dispose();
            }

            _source.Reset();
            _inner = default;
            _current = default;
            _hasInner = false;
        }

        public void Dispose()
        {
            if (_hasInner)
            {
                _inner.Dispose();
            }

            _source.Dispose();
        }
    }
}
