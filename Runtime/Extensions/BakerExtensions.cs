using Unity.Entities;

namespace KrasCore
{
    public static class BakerExtensions
    {
        /// <summary>
        /// Only useful when BovineLabs Core is used
        /// </summary>
        /// <param name="baker"></param>
        /// <param name="entity"></param>
        public static void ForceLinkedEntityGroup(this IBaker baker, Entity entity)
        {
            var buffer = baker.AddBuffer<LinkedEntityGroup>(entity);
            buffer.Add(entity);
        }
    }
}