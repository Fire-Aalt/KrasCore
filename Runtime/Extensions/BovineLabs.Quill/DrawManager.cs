#if BL_QUILL

using System.Collections.Generic;
using BovineLabs.Quill;
using UnityEditor;
using UnityEngine;

namespace KrasCore.Quill
{
    [InitializeOnLoad]
    internal static class DrawManager
    {
        private static readonly HashSet<MonoBehaviourDraw> Drawers = new(64);
        private static readonly List<MonoBehaviourDraw> SelectedDrawers = new(64);
        
        private static readonly List<MonoBehaviourDraw> DrawersBuffer = new(128);
        
        static DrawManager()
        {
            Selection.selectionChanged += SelectionChanged;
            DrawEditor.Update += Update;
        }

        public static void Register(MonoBehaviourDraw drawer)
        {
            Drawers.Add(drawer);
        }
        
        private static void SelectionChanged()
        {
            SelectedDrawers.Clear();

            foreach (var root in Selection.gameObjects)
            {
                CollectDrawersFromRoot(root);
            }
        }
        
        private static void Update()
        {
            // Clear invalid drawers
            DrawersBuffer.Clear();
            foreach (var drawer in Drawers)
            {
                if (drawer == null || drawer.gameObject == null)
                {
                    DrawersBuffer.Add(drawer);
                }
            }

            foreach (var toRemove in DrawersBuffer)
            {
                Drawers.Remove(toRemove);
            }
            
            // Draw always
            foreach (var drawer in Drawers)
            {
                if (!CanDraw(drawer)) continue;
                drawer.Draw();
            }
            
            // Draw selected
            foreach (var drawer in SelectedDrawers)
            {
                if (!CanDraw(drawer)) continue;
                drawer.DrawSelected();
            }
        }

        private static bool CanDraw(MonoBehaviourDraw drawer)
        {
            if (drawer == null) return false;
            return drawer.enabled && drawer.gameObject.activeInHierarchy;
        }
        
        private static void CollectDrawersFromRoot(GameObject root)
        {
            DrawersBuffer.Clear();
            root.GetComponentsInChildren(false, DrawersBuffer);
            
            foreach (var drawer in DrawersBuffer)
            {
                SelectedDrawers.Add(drawer);
            }
        }
    }
}

#endif