#if BL_QUILL && UNITY_EDITOR
using System;
using System.Collections.Generic;
using BovineLabs.Quill;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Pool;

namespace KrasCore.Quill
{
    [InitializeOnLoad]
    internal static class DrawManager
    {
        private static readonly Dictionary<Type, List<IDraw>> Drawers = new(8);
        private static readonly Dictionary<Type, List<IDraw>> SelectedDrawers = new(8);
        
        private static readonly List<IDraw> UninitializedDrawers = new(64);
        private static readonly HashSet<GameObject> SelectedGameObjects = new(16);
        
        static DrawManager()
        {
            Selection.selectionChanged += SelectionChanged;
            DrawEditor.Update += Update;
        }
        
        public static void Register(IDraw drawer)
        {
            AddToMap(drawer, Drawers);
            UninitializedDrawers.Add(drawer);
        }

        private static void SelectionChanged()
        {
            SelectedGameObjects.Clear();
            foreach (var go in Selection.gameObjects)
            {
                SelectedGameObjects.Add(go);
            }

            SelectedDrawers.Clear();
            foreach (var root in SelectedGameObjects)
            {
                ListPool<IDraw>.Get(out var buffer);
                root.GetComponentsInChildren(false, buffer);
            
                foreach (var drawer in buffer)
                {
                    AddToMap(drawer, SelectedDrawers);
                }
                ListPool<IDraw>.Release(buffer);
            }
        }

        private static void Update()
        {
            if (!SceneView.lastActiveSceneView.drawGizmos)
            {
                return;
            }
            
            // Check if already selected
            foreach (var drawer in UninitializedDrawers)
            {
                var transform = ((MonoBehaviour)drawer).transform;
            
                var isSelected = false;
                while (!isSelected && transform.parent != null)
                {
                    isSelected = SelectedGameObjects.Contains(transform.gameObject);
                    transform = transform.parent;
                }

                if (isSelected)
                {
                    AddToMap(drawer, SelectedDrawers);
                }
            }
            UninitializedDrawers.Clear();
            
            // Clear invalid drawers
            foreach (var (_, drawers) in Drawers)
            {
                for (int i = 0; i < drawers.Count; i++)
                {
                    if (drawers[i] as MonoBehaviour) continue;
                    drawers.RemoveAtSwapBack(i);
                    i--;
                }
            }
            
            // Draw always
            foreach (var (type, drawers) in Drawers)
            {
                if (!CanDrawType(drawers, type)) continue;

                foreach (var drawer in drawers)
                {
                    if (!CanDraw(drawer))
                    {
                        continue;
                    }
                    drawer.Draw();
                }
            }
            
            // Draw selected
            foreach (var (type, drawers) in SelectedDrawers)
            {
                if (!CanDrawType(drawers, type)) continue;
                
                foreach (var drawer in drawers)
                {
                    if (!CanDraw(drawer))
                    {
                        continue;
                    }
                    drawer.DrawSelected();
                }
            }
        }
        
        private static bool CanDrawType(List<IDraw> drawers, Type type)
        {
            if (drawers.Count == 0) return false;
            
            var enabled = !GizmoUtility.TryGetGizmoInfo(type, out var gizmoInfo) || gizmoInfo.gizmoEnabled;
            return enabled;
        }

        private static bool CanDraw(IDraw drawer)
        {
            var mb = drawer as MonoBehaviour;
            if (mb == null) return false;
            return mb.isActiveAndEnabled && (mb.hideFlags & HideFlags.HideInHierarchy) == 0;
        }
        
        private static void AddToMap(IDraw drawer, Dictionary<Type, List<IDraw>> map)
        {
            var type = drawer.GetType();
            
            if (!map.ContainsKey(type))
            {
                map.Add(type, new List<IDraw>());
            }

            map[type].Add(drawer);
        }
    }
}
#endif