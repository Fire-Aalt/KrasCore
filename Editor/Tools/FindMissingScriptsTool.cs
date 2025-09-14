using UnityEditor;
using UnityEngine;

namespace Game
{
    public static class FindMissingScriptsTool
    {
        [MenuItem("Tools/Find Missing Scripts")]
        private static void FindMissingScriptsMenuItem()
        {
            foreach (var gameObject in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
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
