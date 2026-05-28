using Unity.Entities;

namespace FireAlt.Core.Groups
{
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(Unity.Transforms.TransformSystemGroup))]
    public partial class BeforeTransformSystemGroup : ComponentSystemGroup { }
}