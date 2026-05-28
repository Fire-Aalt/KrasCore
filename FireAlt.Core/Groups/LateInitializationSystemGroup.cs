using Unity.Entities;

namespace FireAlt.Core.Groups
{
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
    [UpdateInGroup(typeof(BeginSimulationSystemGroup))]
    public partial class LateInitializationSystemGroup : ComponentSystemGroup { }
}