#if BL_CORE
using BovineLabs.Core.Groups;
using BovineLabs.Core.LifeCycle;
using Unity.Entities;

namespace KrasCore
{
    [UpdateInGroup(typeof(AfterSceneSystemGroup))]
    [UpdateAfter(typeof(DestructionSystemGroup))]
    public partial class CleanupSystemGroup : ComponentSystemGroup { }
}
#endif