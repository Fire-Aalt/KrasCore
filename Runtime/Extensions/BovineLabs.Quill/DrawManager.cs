#if BL_QUILL

using System.Collections.Generic;
using System.Diagnostics;
using BovineLabs.Quill;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

namespace KrasCore.Quill
{
    [InitializeOnLoad]
    internal static class DrawManager
    {
        private static readonly List<IDraw> Drawers = new(64);
        private static readonly List<IDraw> SelectedDrawers = new(64);
        
        private static readonly List<GameObject> RootsBuffer = new(64);
        private static readonly List<IDraw> DrawersBuffer = new(128);
        
        static DrawManager()
        {
            Rebuild();

            // Scene and hierarchy events
            EditorApplication.hierarchyChanged += Rebuild;
            EditorSceneManager.sceneOpened += (_, _) => Rebuild();
            EditorSceneManager.sceneClosed += _ => Rebuild();
            
            // Prefab stage events
            PrefabStage.prefabStageDirtied += _ => Rebuild();
            PrefabStage.prefabStageOpened += _ => Rebuild();
            PrefabStage.prefabStageClosing += _ => Rebuild();
            
            Selection.selectionChanged += SelectionChanged;
            DrawEditor.Update += Update;
        }

        private static void SelectionChanged()
        {
            SelectedDrawers.Clear();

            foreach (var root in Selection.gameObjects)
            {
                CollectDrawersFromRoot(root, SelectedDrawers);
            }
        }
        
        private static void Update()
        {
            Stopwatch.Restart();
            foreach (var drawer in Drawers)
            {
                if (!CanDraw(drawer)) continue;
                drawer.Draw();
            }
            
            foreach (var drawer in SelectedDrawers)
            {
                if (!CanDraw(drawer)) continue;
                drawer.DrawSelected();
            }
            
            Stopwatch.Stop();
        }

        private static bool CanDraw(IDraw drawer)
        {
            var mb = drawer as MonoBehaviour;
            if (mb == null) return false;
            return mb.enabled && mb.gameObject.activeInHierarchy;
        }

        private static readonly Stopwatch Stopwatch = new();
        
        private static void Rebuild()
        {
            Stopwatch.Restart();
            FindAllDrawers();
            Stopwatch.Stop();
            
            Debug.Log(Drawers.Count);
            Debug.Log(Stopwatch.Elapsed.TotalMilliseconds);
        }
        
        static void FindAllDrawers()
        {
            Drawers.Clear();
            
            // 1) Collect from all loaded scenes (main editing scenes)
            int sceneCount = SceneManager.sceneCount;
            for (int i = 0; i < sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.IsValid() || !scene.isLoaded) continue;

                RootsBuffer.Clear();
                scene.GetRootGameObjects(RootsBuffer);
                
                foreach (var root in RootsBuffer)
                {
                    CollectDrawersFromRoot(root, Drawers);
                }
            }
            
            // 2) If currently in Prefab Mode, also include the prefab contents root (prefab-editing scene)
            var currentPrefab = PrefabStageUtility.GetCurrentPrefabStage();
            if (currentPrefab != null)
            {
                var prefabRoot = currentPrefab.prefabContentsRoot;
                if (prefabRoot != null)
                {
                    CollectDrawersFromRoot(prefabRoot, Drawers);
                }
            }
        }

        private static void CollectDrawersFromRoot(GameObject root, List<IDraw> destination)
        {
            DrawersBuffer.Clear();
            root.GetComponentsInChildren(false, DrawersBuffer);
            
            foreach (var drawer in DrawersBuffer)
            {
                destination.Add(drawer);
            }
        }
    }
}

#endif