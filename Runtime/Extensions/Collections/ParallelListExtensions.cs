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
        
        public struct Enumerator<T> : IEnumerator<T>, IEnumerator, IDisposable
            where T: unmanaged
        {
            private UnsafeList<T> _threadList;
            private ParallelList<T> _list;
            private int m_Index;
            private int _thread;
            private T value;

            public void Dispose()
            {
            }

            public Enumerator(ref ParallelList<T> list) 
            {
                _list = list;
                m_Index = -1;
                _thread = 0;
                value = default(T);
                _threadList = _list.GetUnsafeList(_thread);
            }
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                if (m_Index < _threadList.Length)
                {
                    this.value = _threadList[m_Index];
                    m_Index++;
                    return true;
                }

                _thread++;
                m_Index = 0;
                
                while (_thread < JobsUtility.ThreadIndexCount)
                {
                    _threadList = _list.GetUnsafeList(_thread);
                    
                    if (m_Index < _threadList.Length)
                    {
                        this.value = _threadList[m_Index];
                        m_Index++;
                        return true;
                    }
                    
                    _thread++;
                    m_Index = 0;
                }
                
                this.value = default (T);
                return false;
            }

            public void Reset()
            {
                _thread = 0;
                this.m_Index = -1;
            }

            public T Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)] get => this.value;
            }

            object IEnumerator.Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)] get => (object) this.Current;
            }
        }
    }
}