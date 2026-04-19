using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace KrasCore.Editor
{
    [RequireMatchingQueriesForUpdate]
    [WorldSystemFilter(WorldSystemFilterFlags.Editor)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(TransformSystemGroup))]
    public partial class HybridEntitySyncSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (link, self) in SystemAPI.Query<HybridEntitySync>()
                         .WithEntityAccess()
                         .WithOptions(EntityQueryOptions.IncludeDisabledEntities))
            {
                var mb = link.MonoBehaviour;

                var enabled = HybridEntityUtils.IsEntityEnabled(mb);
                ecb.SetEnabled(self, enabled);
            }
            
            ecb.Playback(EntityManager);
            
            foreach (var (ltwRW, link) in SystemAPI.Query<RefRW<LocalToWorld>, HybridEntitySync>()
                         .WithNone<LocalTransform, PostTransformMatrix>())
            {
                var transform = link.MonoBehaviour.transform;
                ltwRW.ValueRW.Value = float4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
            }
            
            foreach (var (localTransformRW, link) in SystemAPI.Query<RefRW<LocalTransform>, HybridEntitySync>())
            {
                ref var localTransform = ref localTransformRW.ValueRW;
                var transform = link.MonoBehaviour.transform;

                localTransform.Position = transform.position;
                localTransform.Rotation = transform.rotation;
                
                localTransform.Scale = HybridEntityUtils.IsNonUniformScale(transform) 
                    ? 1f 
                    : transform.lossyScale.x;
            }
            
            foreach (var (postTransformRW, link) in SystemAPI.Query<RefRW<PostTransformMatrix>, HybridEntitySync>())
            {
                var transform = link.MonoBehaviour.transform;
                postTransformRW.ValueRW = new PostTransformMatrix { Value = float4x4.Scale(transform.lossyScale) };
            }
            
        }
    }
}