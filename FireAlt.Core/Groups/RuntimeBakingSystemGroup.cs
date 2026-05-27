using Unity.Entities;

namespace FireAlt.Core.Groups
{
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
    [UpdateInGroup(typeof(InitializationSystemGroup), OrderLast = true)]
    public partial class RuntimeBakingSystemGroup : ComponentSystemGroup { }
}