using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace KrasCore
{
    public partial struct NativeEnumerable<T, TEnumerator>
        where T : unmanaged
        where TEnumerator : unmanaged, IEnumerator<T>
    {
        private TEnumerator _enumerator;

        public NativeEnumerable(TEnumerator enumerator)
        {
            _enumerator = enumerator;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Enumerator GetEnumerator()
        {
            return new Enumerator(_enumerator);
        }

        public struct Enumerator : IEnumerator<T>
        {
            private TEnumerator _source;

            public Enumerator(TEnumerator source)
            {
                _source = source;
            }

            public T Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _source.Current;
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
}


