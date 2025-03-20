using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics.GraphicsIntegration;
using Unity.Transforms;

namespace KrasCore
{
    public static class EntityManagerExtensions
    {
        private const EntityQueryOptions QueryOptions = EntityQueryOptions.IncludeSystems;
        
        public static T GetSingleton<T>(this EntityManager em, bool completeDependency = true)
            where T : unmanaged, IComponentData
        {
            using var query = new EntityQueryBuilder(Allocator.Temp).WithAll<T>().WithOptions(QueryOptions).Build(em);
            if (completeDependency)
            {
                query.CompleteDependency();
            }

            return query.GetSingleton<T>();
        }

        public static void SetEntityInitialTransform(this EntityManager em, Entity entity,
            in float3 positionOffset = default, in quaternion rotationOffset = default, in float scaleOffset = 1)
        {
            var localTransformRW = em.GetComponentDataRW<LocalTransform>(entity);
            localTransformRW.ValueRW.Position += positionOffset;
            localTransformRW.ValueRW.Scale *= scaleOffset;
            
            if (!rotationOffset.Equals(default))
            {
                localTransformRW.ValueRW.Rotation = math.mul(localTransformRW.ValueRW.Rotation, rotationOffset);
            }

            FixInterpolationBufferTransform(em, entity, localTransformRW);
        }

        public static void SetEntityInitialTransform(this EntityManager em, Entity entity, in LocalTransform initialTransform)
        {
            var localTransformRW = em.GetComponentDataRW<LocalTransform>(entity);
            localTransformRW.ValueRW = initialTransform;

            FixInterpolationBufferTransform(em, entity, localTransformRW);
        }
        
        private static void FixInterpolationBufferTransform(EntityManager em, Entity instance, RefRW<LocalTransform> localTransformRW)
        {
            if (em.HasComponent<PhysicsGraphicalInterpolationBuffer>(instance))
            {
                em.GetComponentDataRW<PhysicsGraphicalInterpolationBuffer>(instance).ValueRW.PreviousTransform 
                    = math.RigidTransform(localTransformRW.ValueRW.Rotation, localTransformRW.ValueRW.Position);
            }
        }
        
        public static RefRW<T> GetSingletonRW<T>(this EntityManager em, bool completeDependency = true)
            where T : unmanaged, IComponentData
        {
            using var query = new EntityQueryBuilder(Allocator.Temp).WithAllRW<T>().WithOptions(QueryOptions).Build(em);
            if (completeDependency)
            {
                query.CompleteDependency();
            }

            return query.GetSingletonRW<T>();
        }

        // From https://forum.unity.com/threads/really-hoped-for-refrw-refro-getcomponentrw-ro-entity.1369275/
        /// <summary>
        /// Gets the value of a component for an entity associated with a system.
        /// </summary>
        /// <param name="system">The system handle.</param>
        /// <typeparam name="T">The type of component to retrieve.</typeparam>
        /// <returns>A <see cref="RefRW{T}"/> struct of type T containing access to the component value.</returns>
        /// <exception cref="ArgumentException">Thrown if the component type has no fields.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the system isn't from thie world.</exception>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(BurstCompatibleComponentData) })]
        public static unsafe RefRW<T> GetComponentDataRW<T>(this EntityManager entityManager, Entity entity) where T : unmanaged, IComponentData
        {
            var access = entityManager.GetUncheckedEntityDataAccess();

            var typeIndex = TypeManager.GetTypeIndex<T>();
            var data = access->GetComponentDataRW_AsBytePointer(entity, typeIndex);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            return new RefRW<T>(data, access->DependencyManager->Safety.GetSafetyHandle(typeIndex, false));
#else
        return new RefRW<T>(data);
#endif
        }
    }
}