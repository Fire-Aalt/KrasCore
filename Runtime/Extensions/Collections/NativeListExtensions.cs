using Unity.Collections;
using Unity.Mathematics;

namespace KrasCore
{
    public static class NativeListExtensions
    {
        public static void Remove<T>(this NativeList<T> list, T element)
            where T : unmanaged
        {
            for (int i = list.Length - 1; i >= 0; i--)
            {
                if (list[i].GetHashCode() != element.GetHashCode())
                    continue;

                list.RemoveAt(i);
            }
        }
        
        public static void EnsureCapacity<T>(this NativeList<T> list, int minCapacity, bool setLengthNoClear = false) where T : unmanaged
        {
            var newCapacity = math.max(list.Capacity, minCapacity);

            while (list.Capacity < newCapacity)
            {
                list.Capacity *= 2;
            }

            if (setLengthNoClear)
            {
                list.SetLengthNoClear(minCapacity);
            }
        }
        
        public static unsafe void SetLengthNoClear<T>(this NativeList<T> list, int length) where T : unmanaged
        {
            var data = list.m_ListData;
            data->Length = length;
        }
        
        public static void Reverse<T>(this NativeList<T> list) where T : unmanaged
        {
            int count = list.Length;
            int halfLength = count / 2;
            
            for (int i = 0; i < halfLength; i++)
            {
                int oppositeIndex = count - i - 1;
                
                (list[i], list[oppositeIndex]) = (list[oppositeIndex], list[i]);
            }
        }
        
        public static T[] ToManagedArray<T>(this NativeList<T> nativeList) where T : unmanaged
        {
            var managedArray = new T[nativeList.Length];
        
            for (int i = 0; i < nativeList.Length; i++)
            {
                managedArray[i] = nativeList[i];
            }
            return managedArray;
        }
    }
}