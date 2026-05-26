using Unity.Entities;

namespace KrasCore
{
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
#if BL_CORE_EXTENSIONS
    [UpdateInGroup(typeof(BovineLabs.Core.Groups.BeginSimulationSystemGroup), OrderFirst = true)]
#if !BL_DISABLE_LIFECYCLE
    [UpdateAfter(typeof(BovineLabs.Core.LifeCycle.SceneInitializeSystem))]
#endif
#else
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
#endif
    public partial class LateInitializationSystemGroup : ComponentSystemGroup { }
}