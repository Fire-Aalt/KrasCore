#if UNITY_EDITOR
using Unity.Entities;
using UnityEngine;

namespace KrasCore.Editor
{
    public class HybridEntitySync : IComponentData
    {
        public readonly MonoBehaviour MonoBehaviour;

        public HybridEntitySync()
        {
        }

        public HybridEntitySync(MonoBehaviour monoBehaviour)
        {
            MonoBehaviour = monoBehaviour;
        }
    }
}
#endif