using UnityEditor;
using UnityEngine;

namespace FireAlt.Core.Editor
{
    public static class FindMissingScriptsTool
    {
        [MenuItem("Tools/Find Missing Scripts")]
        private static void FindMissingScriptsMenuItem()
        {
            foreach (var gameObject in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include))
            {
                foreach (var component in gameObject.GetComponents<Component>())
                {
                    if (component == null)
                    {
                        Debug.LogWarning($"GameObject found with missing script {gameObject.name}", gameObject);
                        break;
                    }
                }
            }
        }
    }
}
