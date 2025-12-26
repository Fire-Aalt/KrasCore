using System;
using System.Reflection;
using UnityEditor;

namespace KrasCore.Editor
{
    [InitializeOnLoad]
    public static class GameViewVSyncFix
    {
        private const string VSyncEnabledKey = "VSyncEnabled";
        private const BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        
        static GameViewVSyncFix()
        {
            EditorApplication.playModeStateChanged += PlayModeStateChanged;
            EditorApplication.delayCall += () =>
            {
                var gameView = GetGameView();
                var prop = GetVSyncPropertyInfo(gameView);
                SetVSyncEnabled(gameView, prop, true);
            };
        }

        private static void PlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                var gameView = GetGameView();
                var prop = GetVSyncPropertyInfo(gameView);
                var enabled = ScopedEditorPrefs.GetBool(VSyncEnabledKey);
                SetVSyncEnabled(gameView, prop, enabled);
            }
            else if (state == PlayModeStateChange.EnteredEditMode)
            {
                var gameView = GetGameView();
                var prop = GetVSyncPropertyInfo(gameView);
                ScopedEditorPrefs.SetBool(VSyncEnabledKey, GetVSyncEnabled(gameView, prop));
                
                // Reset is needed because otherwise it is not applied
                SetVSyncEnabled(gameView, prop, false);
                SetVSyncEnabled(gameView, prop, true);
            }
        }

        private static EditorWindow GetGameView()
        {
            var gameView = EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"), false, null, false);
            if (gameView == null)
            {
                throw new Exception("GameView not found");
            }
            return gameView;
        }

        private static PropertyInfo GetVSyncPropertyInfo(EditorWindow gameView)
        {
            var prop = gameView.GetType().GetProperty("vSyncEnabled", InstanceFlags);
            if (prop == null)
            {
                throw new Exception("VSync property not found");
            }
            return prop;
        }
        
        private static void SetVSyncEnabled(EditorWindow gameView, PropertyInfo prop, bool value)
        {
            var setMethod = prop.GetSetMethod(true);
            setMethod?.Invoke(gameView, new object[] { value });
        }
        
        private static bool GetVSyncEnabled(EditorWindow gameView, PropertyInfo prop)
        {
            var setMethod = prop.GetGetMethod(true);
            return (bool)setMethod.Invoke(gameView, null);
        }
    }
}