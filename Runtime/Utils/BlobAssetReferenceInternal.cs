using Unity.Entities;

namespace KrasCore
{
    public static class BlobAssetReferenceInternal
    {
        public static long GetHash<T>(this BlobAssetReference<T> blobAssetReference)
            where T : unmanaged
        {
            return blobAssetReference.m_data.m_Align8Union;
        }
        
        public static void ResetHash<T>(this ref BlobAssetReference<T> blobAssetReference)
            where T : unmanaged
        {
            blobAssetReference.m_data.m_Align8Union = 0;
        }
    }
}