using Unity.Entities;
using UnityEngine;

namespace FireAlt.Core
{
    public struct HybridEntitySync : IComponentData
    {
        public UnityObjectRef<MonoBehaviour> MonoBehaviour;

        public HybridEntitySync(MonoBehaviour monoBehaviour)
        {
            MonoBehaviour = monoBehaviour;
        }
    }
}