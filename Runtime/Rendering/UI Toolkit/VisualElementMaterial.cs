using Unity.Assertions;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;
//using Cysharp.Threading.Tasks;

namespace KrasCore
{
    /// <summary>
    /// Creates an instance of a material of a VisualElement
    /// </summary>
    public class VisualElementMaterial
    {
        private Material _instance;
        
        public Material Instance => _instance;
        public bool IsCreated => _instance != null;
        
        public VisualElementMaterial(VisualElement target)
        {
            if (Application.isPlaying)
            {
                //CreateTask(target).Forget();
                
                target.schedule.Execute(() =>
                {
                    target.schedule.Execute(() =>
                    {
                        Create(target);
                    });
                });
            }
        }

        // public async UniTaskVoid CreateTask(VisualElement target)
        // {
        //     await UniTask.WaitUntil(target, t => t.resolvedStyle.unityMaterial.material != null);
        //     Create(target);
        // }

        private void Create(VisualElement target)
        {
            var mat = target.resolvedStyle.unityMaterial.material;
            Assert.IsNotNull(mat, $"Material is null on {target}. Instancing failed");
                    
            _instance = new Material(mat);
            target.style.unityMaterial = _instance;
        }

        ~VisualElementMaterial()
        {
            if (Application.isPlaying)
            {
                Object.Destroy(_instance);
            }
        }
    }
}