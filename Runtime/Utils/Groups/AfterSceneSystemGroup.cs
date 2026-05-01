using Unity.Entities;
using Unity.Scenes;

namespace KrasCore
{
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
#if BL_CORE_EXTENSIONS
    [UpdateInGroup(typeof(BovineLabs.Core.Groups.AfterSceneSystemGroup))]
#else
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(SceneSystemGroup))]
#endif
    public partial class AfterSceneSystemGroup : ComponentSystemGroup { }
}