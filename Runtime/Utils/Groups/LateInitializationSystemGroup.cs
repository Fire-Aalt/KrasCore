using BovineLabs.Core.Groups;
using Unity.Entities;

namespace KrasCore
{
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
    [UpdateInGroup(typeof(BeginSimulationSystemGroup))]
    //    [UpdateInGroup(typeof(BeginSimulationSystemGroup), OrderFirst = true)]
    //    [UpdateAfter(typeof(SceneInitializeSystem))]
    public partial class LateInitializationSystemGroup : ComponentSystemGroup { }
}