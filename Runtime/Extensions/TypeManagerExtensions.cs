using System;
using Unity.Entities;

namespace KrasCore
{
    public static class TypeManagerExtensions
    {
        public static void BeginAddAnyTypesSequence()
        {
            TypeManager.ShutdownSharedStatics();
        }
        
        public static void TryAddAnyType(Type type)
        {
            if (!TypeManager.TryGetTypeIndex(type, out _))
            {
                TypeManager.GetOrCreateTypeIndexUnsafe(type);
            }
        }
        
        public static void EndAddAnyTypesSequence()
        {
            TypeManager.InitializeSharedStatics();
        }
    }
}