using Unity.Entities;

namespace KrasCore
{
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
    [UpdateInGroup(typeof(AfterSceneSystemGroup))]
    public partial class InstantiationSystemGroup : ComponentSystemGroup { }
}
