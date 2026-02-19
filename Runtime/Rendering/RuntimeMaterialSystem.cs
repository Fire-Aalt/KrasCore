using System.Collections.Generic;
using Unity.Entities;
using Unity.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace KrasCore
{
    [RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(RuntimeBakingSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
    public partial class RuntimeMaterialSystem : SystemBase
    {
        private readonly struct BatchMaterial
        {
            public readonly Material Material;
            public readonly BatchMaterialID MaterialID;

            public BatchMaterial(Material material, BatchMaterialID materialID)
            {
                Material = material;
                MaterialID = materialID;
            }
        }
        
        private readonly Dictionary<MaterialLookup, BatchMaterial> _materials = new();
        
        protected override void OnUpdate()
        {
            var entitiesGraphicsSystem = World.GetExistingSystemManaged<EntitiesGraphicsSystem>();
            
            foreach (var (materialMeshInfoRW, lookupRO, enabled) in SystemAPI.Query<RefRW<MaterialMeshInfo>, RefRO<RuntimeMaterialLookup>, EnabledRefRW<RuntimeMaterialLookup>>()
                         .WithOptions(EntityQueryOptions.IncludePrefab))
            {
                var batchMaterial = GetBatchMaterial(lookupRO.ValueRO.Value, entitiesGraphicsSystem, true);

                materialMeshInfoRW.ValueRW.MaterialID = batchMaterial.MaterialID;
                enabled.ValueRW = false;
            }
            
            foreach (var (materialRW, lookupRO, enabled) in SystemAPI.Query<RefRW<RuntimeMaterial>, RefRO<RuntimeMaterialLookup>, EnabledRefRW<RuntimeMaterialLookup>>()
                         .WithOptions(EntityQueryOptions.IncludePrefab))
            {
                var batchMaterial = GetBatchMaterial(lookupRO.ValueRO.Value, entitiesGraphicsSystem, false);

                materialRW.ValueRW.Value = batchMaterial.Material;
                enabled.ValueRW = false;
            }
        }

        private BatchMaterial GetBatchMaterial(MaterialLookup lookup, EntitiesGraphicsSystem entitiesGraphicsSystem, bool registerIfMissing)
        {
            if (!_materials.TryGetValue(lookup, out var batchMaterial))
            {
                batchMaterial = CreateAndRegister(lookup, entitiesGraphicsSystem, registerIfMissing);
                _materials.Add(lookup, batchMaterial);
            }
#if UNITY_EDITOR
            else if (batchMaterial.Material == null)
            {
                batchMaterial = CreateAndRegister(lookup, entitiesGraphicsSystem, registerIfMissing);
                _materials[lookup] = batchMaterial;
            }
#endif
            return batchMaterial;
        }

        private static BatchMaterial CreateAndRegister(MaterialLookup lookup, EntitiesGraphicsSystem entitiesGraphicsSystem, bool registerIfMissing)
        {
            var mat = CloneMaterialUtility.CloneFromLookup(lookup);
            
            BatchMaterialID matId = default;
            if (registerIfMissing)
                matId = entitiesGraphicsSystem.RegisterMaterial(mat);
            
            return new BatchMaterial(mat, matId);
        }

        protected override void OnDestroy()
        {
            foreach (var kvp in _materials)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    Object.DestroyImmediate(kvp.Value.Material);
                else
#endif
                    Object.Destroy(kvp.Value.Material);
            }
        }
    }
}