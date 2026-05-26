using Unity.Assertions;

namespace KrasCore
{
    public static class ArrayExtensions
    {
        public static T[] ExpandAdd<T>(this T[] array, T item)
        {
            var newArray = new T[array.Length + 1];
            array.CopyTo(newArray, 0);
            newArray[^1] = item;
            return newArray;
        }
        
        public static T[] ShrinkRemoveAt<T>(this T[] array, int index)
        {
            Assert.IsTrue(array.Length - 1 >= 0);

            var excluded = false;
            var newArray = new T[array.Length - 1];
            for (int i = 0; i < array.Length; i++)
            {
                if (!excluded)
                {
                    if (i != index)
                        newArray[i] = array[i];
                    else
                        excluded = true;
                }
                else
                {
                    newArray[i-1] = array[i];
                }
            }
            return newArray;
        }
    }
}