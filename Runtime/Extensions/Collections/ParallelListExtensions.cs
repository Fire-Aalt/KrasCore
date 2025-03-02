using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using KrasCore.NZCore;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs.LowLevel.Unsafe;

namespace KrasCore
{
    public static class ParallelListExtensions
    {
        public static Enumerator<T> GetEnumerator<T>(this ParallelList<T> list) 
            where T: unmanaged
        {
            return new Enumerator<T>(ref list);
        }
        
        public struct Enumerator<T> : IEnumerator<T>
            where T: unmanaged
        {
            private UnsafeList<T> _threadList;
            private ParallelList<T> _list;
            private int _index;
            private int _thread;
            private T _value;

            public void Dispose()
            {
            }

            public Enumerator(ref ParallelList<T> list) 
            {
                _list = list;
                _index = 0;
                _thread = 0;
                _value = default(T);
                _threadList = _list.GetUnsafeList(_thread);
            }
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                if (_index < _threadList.Length)
                {
                    _value = _threadList[_index];
                    _index++;
                    return true;
                }

                _thread++;
                _index = 0;
                
                while (_thread < JobsUtility.ThreadIndexCount)
                {
                    _threadList = _list.GetUnsafeList(_thread);
                    
                    if (_index < _threadList.Length)
                    {
                        _value = _threadList[_index];
                        _index++;
                        return true;
                    }
                    
                    _thread++;
                    _index = 0;
                }
                
                _value = default;
                return false;
            }

            public void Reset()
            {
                _thread = 0;
                _index = 0;
            }

            public T Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)] get => _value;
            }

            object IEnumerator.Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)] get => Current;
            }
        }
    }
}