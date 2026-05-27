using FireAlt.Core.Groups;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine.Jobs;
using UnityEngine.Profiling;

namespace FireAlt.Core
{
    [RequireMatchingQueriesForUpdate]
    [WorldSystemFilter(WorldSystemFilterFlags.Editor | WorldSystemFilterFlags.Default)]
    [UpdateInGroup(typeof(AfterTransformSystemGroup))]
    public partial class HybridEntitySyncSystem : SystemBase
    {
        protected override void OnCreate()
        {
            EntityManager.CreateSingleton(new SyncTransformToEntityContainer(8, Allocator.Persistent));
        }

        protected override void OnDestroy()
        {
            SystemAPI.GetSingleton<SyncTransformToEntityContainer>().Dispose();
        }

        protected override void OnUpdate()
        {
            var singleton = SystemAPI.GetSingletonRW<SyncTransformToEntityContainer>().ValueRW;
            
            Profiler.BeginSample("Initialize New Entities");
            if (!SystemAPI.QueryBuilder().WithAll<HybridEntitySync>().WithAbsent<SyncTransformToEntity>().Build().IsEmpty)
            {
                var initEcb = new EntityCommandBuffer(Allocator.Temp);
                
                foreach (var (link, self) in SystemAPI.Query<HybridEntitySync>()
                             .WithEntityAccess()
                             .WithOptions(EntityQueryOptions.IncludeDisabledEntities | EntityQueryOptions.IncludePrefab))
                {
                    initEcb.AddComponent(self, new SyncTransformToEntity
                    {
                        TransformId = singleton.ReusableTransformAccessArray.AddTransform(self, link.MonoBehaviour.transform)
                    });
                }
                
                initEcb.Playback(EntityManager);
            }
            Profiler.EndSample();
            
            
            Profiler.BeginSample("Cleanup Old Entities");
            var cleanupQuery = SystemAPI.QueryBuilder().WithAll<SyncTransformToEntity>().WithAbsent<HybridEntitySync>().Build();
            if (!cleanupQuery.IsEmpty)
            {
                foreach (var link in SystemAPI.Query<SyncTransformToEntity>()
                             .WithOptions(EntityQueryOptions.IncludeDisabledEntities | EntityQueryOptions.IncludePrefab))
                {
                    singleton.ReusableTransformAccessArray.ReleaseTransform(link.TransformId);
                }
                EntityManager.RemoveComponent<SyncTransformToEntity>(cleanupQuery);
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

            Dependency = new SyncTransformsJob
            {
                Entities = singleton.ReusableTransformAccessArray.AlignedEntities.AsDeferredJobArray(),
                LocalToWorld = SystemAPI.GetComponentLookup<LocalToWorld>(),
                LocalTransform = SystemAPI.GetComponentLookup<LocalTransform>(),
                PostTransformMatrix = SystemAPI.GetComponentLookup<PostTransformMatrix>(),
            }.ScheduleReadOnly(singleton.ReusableTransformAccessArray.Array, 64, Dependency);
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