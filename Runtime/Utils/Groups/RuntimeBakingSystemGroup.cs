using Unity.Entities;

namespace KrasCore
{
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
    [UpdateInGroup(typeof(InitializationSystemGroup), OrderLast = true)]
    public partial class RuntimeBakingSystemGroup : ComponentSystemGroup { }
}