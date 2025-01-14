using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using Object = UnityEngine.Object;

namespace KrasCore
{
    [UpdateInGroup(typeof(PresentationSystemGroup), OrderFirst = true)]
    public partial class RuntimeMaterialSystem : SystemBase
    {
        private readonly Dictionary<MaterialLookup, Material> _materials = new();

        protected override void OnUpdate()
        {
            foreach (var (runtimeMaterialRW, enabled) in SystemAPI.Query<RefRW<RuntimeMaterial>, EnabledRefRW<RuntimeMaterial>>())
            {
                var lookup = runtimeMaterialRW.ValueRO.Lookup;
                if (!_materials.TryGetValue(lookup, out var mat))
                {
                    mat = new Material(lookup.SrcMaterial)
                    {
                        mainTexture = lookup.Texture
                    };
                    _materials.Add(lookup, mat);
                }

                runtimeMaterialRW.ValueRW.Value = mat;
                enabled.ValueRW = false;
            }
        }
        
        protected override void OnDestroy()
        {
            foreach (var kvp in _materials)
            {
                Object.Destroy(kvp.Value);
            }
        }
    }
}