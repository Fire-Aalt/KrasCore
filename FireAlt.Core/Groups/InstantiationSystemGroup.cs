using Unity.Entities;

namespace FireAlt.Core.Groups
{
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
    [UpdateInGroup(typeof(AfterSceneSystemGroup))]
    public partial class InstantiationSystemGroup : ComponentSystemGroup { }
}
