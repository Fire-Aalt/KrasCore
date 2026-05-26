using Unity.Entities;
using UnityEngine;

namespace FireAlt.Core
{
    public struct RuntimeMaterial : IComponentData
    {
        public UnityObjectRef<Material> Value;
    }
}