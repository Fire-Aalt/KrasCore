using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace KrasCore.Editor
{
    public static class HybridEntityUtils
    {
        private static readonly bool ShowRuntime;
        
        static HybridEntityUtils()
        {
            ShowRuntime = EditorPrefs.GetBool("Unity.Entities.Streaming.SubScene.LiveConversionSceneViewShowRuntime", false);
        }
        
        public static bool IsEntityEnabled(MonoBehaviour mb)
        {
            if (InSubScene(mb))
            {
                return mb.isActiveAndEnabled && !ShowRuntime;
            }
            return mb.isActiveAndEnabled;
        }
        
        public static bool InSubScene(MonoBehaviour component) => component.gameObject.scene.isSubScene;
        public static bool InPrefabStage(MonoBehaviour component) => EditorSceneManager.IsPreviewScene(component.gameObject.scene);
        
        public static bool IsNonUniformScale(Transform transform)
        {
            var localScale = transform.localScale;
            return !Mathf.Approximately(localScale.x, localScale.y) || !Mathf.Approximately(localScale.y, localScale.z);
        }
    }
}