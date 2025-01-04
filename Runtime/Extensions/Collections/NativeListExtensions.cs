using Unity.Collections;

namespace KrasCore.Runtime
{
    public static class NativeListExtensions
    {
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