using Unity.Entities;

namespace KrasCore
{
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
    [UpdateInGroup(typeof(AfterSceneSystemGroup))]
    [UpdateAfter(typeof(DestructionSystemGroup))]
    public partial class CleanupSystemGroup : ComponentSystemGroup { }
}