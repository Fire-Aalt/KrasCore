using Unity.Entities;

namespace KrasCore
{
    [UpdateInGroup(typeof(InitializationSystemGroup), OrderLast = true)]
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
    public partial class RuntimeBakingSystemGroup : ComponentSystemGroup { }
}