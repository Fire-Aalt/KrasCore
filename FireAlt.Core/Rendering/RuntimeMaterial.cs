using Unity.Entities;
using UnityEngine;

namespace FireAlt.Core.Rendering
{
    public struct RuntimeMaterial : IComponentData
    {
        public UnityObjectRef<Material> Value;
    }
}