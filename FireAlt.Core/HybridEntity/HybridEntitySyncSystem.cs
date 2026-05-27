using FireAlt.Core.Groups;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Jobs;
using UnityEngine.Profiling;

namespace FireAlt.Core
{
    [RequireMatchingQueriesForUpdate]
    [WorldSystemFilter(WorldSystemFilterFlags.Editor | WorldSystemFilterFlags.Default)]
    [UpdateInGroup(typeof(BeginSimulationSystemGroup))]
    public partial class HybridEntitySyncSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            Profiler.BeginSample("Initialize New Entities");
            if (!SystemAPI.QueryBuilder().WithAll<HybridEntitySync>().WithAbsent<Transform>().Build().IsEmpty)
            {
                var initEcb = new EntityCommandBuffer(Allocator.Temp);
                
                foreach (var (link, self) in SystemAPI.Query<HybridEntitySync>()
                             .WithEntityAccess()
                             .WithOptions(EntityQueryOptions.IncludeDisabledEntities | EntityQueryOptions.IncludePrefab))
                {
                    initEcb.AddComponent(self, link.MonoBehaviour.transform);
                }
                
                initEcb.Playback(EntityManager);
            }
            Profiler.EndSample();
            
            Profiler.BeginSample("SetEnabled");
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            
            foreach (var (link, self) in SystemAPI.Query<HybridEntitySync>()
                         .WithEntityAccess()
                         .WithOptions(EntityQueryOptions.IncludeDisabledEntities))
            {
                var mb = link.MonoBehaviour;

                var enabled = HybridEntityUtils.IsEntityEnabled(mb);
                if (enabled != EntityManager.IsEnabled(self))
                {
                    ecb.SetEnabled(self, enabled);
                }
            }

            if (!ecb.IsEmpty)
            {
                ecb.Playback(EntityManager);
            }
            Profiler.EndSample();

            var query = SystemAPI.QueryBuilder().WithAll<HybridEntitySync, Transform>().Build();
            var transformAccessArray = query.GetTransformAccessArray();
            var entities = query.ToEntityListAsync(WorldUpdateAllocator, Dependency, out var dependency);

            Dependency = new SyncTransformsJob
            {
                Entities = entities.AsDeferredJobArray(),
                LocalToWorld = SystemAPI.GetComponentLookup<LocalToWorld>(),
                LocalTransform = SystemAPI.GetComponentLookup<LocalTransform>(),
                PostTransformMatrix = SystemAPI.GetComponentLookup<PostTransformMatrix>(),
            }.ScheduleReadOnly(transformAccessArray, 16, dependency);
        }
        
        [BurstCompile]
        private struct SyncTransformsJob : IJobParallelForTransform
        {
            [ReadOnly]
            public NativeArray<Entity> Entities;
            
            [NativeDisableParallelForRestriction]
            public ComponentLookup<LocalToWorld> LocalToWorld;
            [NativeDisableParallelForRestriction]
            public ComponentLookup<LocalTransform> LocalTransform;
            [NativeDisableParallelForRestriction]
            public ComponentLookup<PostTransformMatrix> PostTransformMatrix;
            
            public void Execute(int index, [ReadOnly] TransformAccess transform)
            {
                var entity = Entities[index];
                
                var hasLocalTransform = LocalTransform.TryGetRefRW(entity, out var localTransformRW);
                var hasPostTransformMatrix = PostTransformMatrix.TryGetRefRW(entity, out var postTransformMatrixRW);

                if (!hasLocalTransform && !hasPostTransformMatrix && LocalToWorld.TryGetRefRW(entity, out var ltwRW))
                {
                    ltwRW.ValueRW.Value = float4x4.TRS(transform.position, transform.rotation, transform.localScale);
                }
                
                if (hasLocalTransform)
                {
                    localTransformRW.ValueRW.Position = transform.position;
                    localTransformRW.ValueRW.Rotation = transform.rotation;
                
                    localTransformRW.ValueRW.Scale = HybridEntityUtils.IsNonUniformScale(transform) 
                        ? 1f 
                        : transform.localScale.x;
                }

                if (hasPostTransformMatrix)
                {
                    postTransformMatrixRW.ValueRW = new PostTransformMatrix { Value = float4x4.Scale(transform.localScale) };
                }
            }
        }
    }
}