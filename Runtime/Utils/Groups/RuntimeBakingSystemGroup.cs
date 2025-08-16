using Unity.Entities;
using Unity.Scenes;

namespace KrasCore
{
    [UpdateAfter(typeof(SceneSystemGroup))]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
    public partial class RuntimeBakingSystemGroup : ComponentSystemGroup { }
}