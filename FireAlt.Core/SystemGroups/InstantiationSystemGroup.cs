using Unity.Entities;

namespace FireAlt.Core
{
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
    [UpdateInGroup(typeof(AfterSceneSystemGroup))]
    public partial class InstantiationSystemGroup : ComponentSystemGroup { }
}
