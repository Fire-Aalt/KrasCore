using Unity.Entities;

namespace FireAlt.Core
{
    public struct SyncTransformToEntity : ICleanupComponentData
    {
        public int TransformId;
    }
}