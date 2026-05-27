using Unity.Entities;

namespace FireAlt.Core.Groups
{
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
#if BL_CORE_EXTENSIONS
    [UpdateInGroup(typeof(BovineLabs.Core.Groups.BeginSimulationSystemGroup), OrderFirst = true)]
#if !BL_DISABLE_LIFECYCLE
    [UpdateAfter(typeof(BovineLabs.Core.LifeCycle.SceneInitializeSystem))]
#endif
#else
    [UpdateInGroup(typeof(BeginSimulationSystemGroup))]
#endif
    public partial class LateInitializationSystemGroup : ComponentSystemGroup { }
}