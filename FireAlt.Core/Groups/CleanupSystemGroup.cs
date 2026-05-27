using Unity.Entities;

namespace FireAlt.Core.Groups
{
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
    [UpdateInGroup(typeof(AfterSceneSystemGroup))]
    [UpdateAfter(typeof(DestructionSystemGroup))]
    public partial class CleanupSystemGroup : ComponentSystemGroup { }
}