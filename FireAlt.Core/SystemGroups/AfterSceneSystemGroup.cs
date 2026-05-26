using Unity.Entities;

namespace FireAlt.Core
{
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
#if BL_CORE_EXTENSIONS
    [UpdateInGroup(typeof(BovineLabs.Core.Groups.AfterSceneSystemGroup))]
#else
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(Unity.Scenes.SceneSystemGroup))]
#endif
    public partial class AfterSceneSystemGroup : ComponentSystemGroup { }
}