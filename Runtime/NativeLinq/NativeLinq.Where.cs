using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace KrasCore
{
    public partial struct NativeQuery<T, TEnumerator>
        where T : unmanaged
        where TEnumerator : unmanaged, IEnumerator<T>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeQuery<T, NativeWhereEnumerator<T, TEnumerator, TPredicate>> Where<TPredicate>(TPredicate predicate)
            where TPredicate : unmanaged, IPredicate<T>
        {
            return new NativeQuery<T, NativeWhereEnumerator<T, TEnumerator, TPredicate>>(
                new NativeWhereEnumerator<T, TEnumerator, TPredicate>(GetEnumerator(), predicate));
        }
    }

    public struct NativeWhereEnumerator<T, TEnumerator, TPredicate> : IEnumerator<T>
        where T : unmanaged
        where TEnumerator : unmanaged, IEnumerator<T>
        where TPredicate : unmanaged, IPredicate<T>
    {
        private TEnumerator _source;
        private TPredicate _predicate;
        private T _current;

        public NativeWhereEnumerator(TEnumerator source, TPredicate predicate)
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
