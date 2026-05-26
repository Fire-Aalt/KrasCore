using Unity.Entities;
using UnityEngine;

namespace KrasCore
{
    public struct RuntimeMaterial : IComponentData
    {
        public UnityObjectRef<Material> Value;
    }
}