using Unity.Entities;

namespace KrasCore
{
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
    [UpdateInGroup(typeof(AfterSceneSystemGroup))]
    [UpdateAfter(typeof(InstantiationSystemGroup))]
    public partial class DestructionSystemGroup : ComponentSystemGroup { }
}