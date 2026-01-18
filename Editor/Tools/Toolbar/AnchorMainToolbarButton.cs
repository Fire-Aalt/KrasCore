using System;
using BovineLabs.Anchor;
using BovineLabs.Anchor.Toolbar;
using BovineLabs.Core.ConfigVars;
using Unity.AppUI.MVVM;
using Unity.Burst;
using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.UIElements;
using Color = UnityEngine.Color;

namespace KrasCore.Editor
{
    [Configurable]
    public class AnchorMainToolbarButton
    {
        [ConfigVar("krascore.anchor-toolbar.show-on-start", true, "Should the toolbar be shown on startup", true, true)]
        private static readonly SharedStatic<bool> Show = SharedStatic<bool>.GetOrCreate<AnchorMainToolbarButton, EnabledVar>();

        private static bool _isVisible;

        [InitializeOnLoadMethod]
        public static void Init()
        {
            EditorApplication.playModeStateChanged += PlayModeChanged;
        }

        private static void PlayModeChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.EnteredPlayMode)
            {
                var toolbarView = AnchorApp.current.services.GetRequiredService<ToolbarView>();
                toolbarView.schedule.Execute(() =>
                {
                    var newIsVisible = !(bool)ReflectionUtils.GetField(toolbarView, "toolbarHidden").GetValue(toolbarView);
                    if (newIsVisible != _isVisible)
                    {
                        Style();
                        MainToolbar.Refresh("Anchor/CreateAnchorToolbar");
                    }
                    _isVisible = newIsVisible;
                }).Every(250);

                SetToolbarVisibility(toolbarView, Show.Data);
                
                Style();
                MainToolbar.Refresh("Anchor/CreateAnchorToolbar");
            }
            else if (change == PlayModeStateChange.EnteredEditMode)
            {
                Style();
                MainToolbar.Refresh("Anchor/CreateAnchorToolbar");
            }
        }

        [MainToolbarElement("Anchor/Create Anchor Toolbar", defaultDockPosition = MainToolbarDockPosition.Middle)]
        public static MainToolbarElement CreateAnchorToolbar() {
            var icon = EditorGUIUtility.IconContent("SettingsIcon").image as Texture2D;
            var content = new MainToolbarContent(icon);
            
            var element = new MainToolbarButton(content, () =>
            {
                if (!Application.isPlaying)
                {
                    Show.Data = !Show.Data;
                    Style();
                    return;
                }
                
                _isVisible = !_isVisible;
                Style();

                var toolbarView = AnchorApp.current.services.GetRequiredService<ToolbarView>();
                SetToolbarVisibility(toolbarView, _isVisible);
            });
            
            return element;
        }

        private static void SetToolbarVisibility(ToolbarView toolbarView, bool visible)
        {
            if (visible)
            {
                var restore = ReflectionUtils.GetCallMethod(toolbarView, "RestoreToolbar");
                restore.Invoke(toolbarView, null);
            }
            else
            {
                var hide = ReflectionUtils.GetCallMethod(toolbarView, "HideToolbar");
                hide.Invoke(toolbarView, null);
            }
        }

        private static void Style()
        {
            MainToolbarElementStyler.StyleElement<EditorToolbarButton>("Anchor/CreateAnchorToolbar", element =>
            {
                if (!Application.isPlaying)
                {
                    element.style.backgroundColor = Show.Data ? Color.darkGreen : Color.darkRed;
                }
                else
                {
                    element.style.backgroundColor = _isVisible ? Color.cadetBlue : StyleKeyword.None;
                }
            });
        }

        private static SharedStatic<bool> GetAnchorToolbarConfigVar()
        {
            const string anchorToolbar = "anchor.toolbar";

            foreach (var (configVar, field) in ConfigVarManager.FindAllConfigVars())
            {
                if (!configVar.Name.Equals(anchorToolbar)) continue;
                var value = field.GetValue(null);

                if (value is SharedStatic<bool> sharedStatic)
                {
                    return sharedStatic;
                }
            }
            throw new Exception($"ConfigVar {anchorToolbar} not found");
        }
        
        private struct EnabledVar
        {
        }
    }
}