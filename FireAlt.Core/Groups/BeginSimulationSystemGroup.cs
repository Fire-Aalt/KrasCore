using Unity.Entities;

namespace FireAlt.Core.Groups
{
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
#if BL_CORE_EXTENSIONS
    [UpdateInGroup(typeof(BovineLabs.Core.Groups.BeginSimulationSystemGroup))]
#else
    [UpdateInGroup(typeof(BeginSimulationSystemGroup), OrderFirst = true)]
#endif
    public partial class BeginSimulationSystemGroup : ComponentSystemGroup { }
}