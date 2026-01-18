using BovineLabs.Anchor;
using BovineLabs.Anchor.Toolbar;
using BovineLabs.Core.ConfigVars;
using Unity.AppUI.MVVM;
using Unity.Burst;
using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.UIElements;

namespace KrasCore.Editor
{
    [Configurable]
    public class ShowAnchorToolbarButton
    {
        private const string Path = "KrasCore/Show Anchor Toolbar";
        private static readonly string Name = StringUtils.RemoveAllWhitespace(Path);
        
        [ConfigVar("krascore.anchor-toolbar.show-on-start", true, "Should the toolbar be shown on startup", true, true)]
        private static readonly SharedStatic<bool> ShowOnStart = SharedStatic<bool>.GetOrCreate<ShowAnchorToolbarButton, EnabledVar>();

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
                
                var button = FindButtonWithTrailingIcon(toolbarView.panel.visualTree, "x");
                button.RemoveFromHierarchy();

                SetToolbarVisibility(toolbarView, ShowOnStart.Data);
                QueueStyleUpdate();
            }
            else if (change == PlayModeStateChange.EnteredEditMode)
            {
                QueueStyleUpdate();
            }
        }

        [MainToolbarElement(Path, defaultDockPosition = MainToolbarDockPosition.Middle)]
        public static MainToolbarElement ShowAnchorToolbar() {
            var icon = EditorGUIUtility.IconContent("CustomTool").image as Texture2D;
            var content = new MainToolbarContent(icon);
            
            var element = new MainToolbarButton(content, ButtonClicked);
            QueueStyleUpdate();
            return element;
        }

        private static void ButtonClicked()
        {
            QueueStyleUpdate();
            if (!Application.isPlaying)
            {
                ShowOnStart.Data = !ShowOnStart.Data;
                return;
            }
            
            var toolbarView = AnchorApp.current.services.GetRequiredService<ToolbarView>();
            SetToolbarVisibility(toolbarView, !_isVisible);
        }

        private static void SetToolbarVisibility(ToolbarView toolbarView, bool visible)
        {
            _isVisible = visible;
            if (_isVisible)
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
        
        private static void QueueStyleUpdate()
        {
            MainToolbarElementStyler.StyleElement<EditorToolbarButton>(Name, element =>
            {
                if (!Application.isPlaying)
                {
                    element.style.backgroundColor = ShowOnStart.Data ? new Color(0, 1, 0, 0.1f) : new Color(1, 0, 0, 0.1f);
                }
                else
                {
                    element.style.backgroundColor = _isVisible ? new Color(68f / 255f, 93f / 255f, 120f / 255f) : StyleKeyword.None;
                }
            });
            MainToolbar.Refresh(Name);
        }
        
        private static Unity.AppUI.UI.Button FindButtonWithTrailingIcon(VisualElement root, string trailingIcon)
        {
            return root.Query<Unity.AppUI.UI.Button>().Where(b => b.trailingIcon == trailingIcon).First();
        }
        
        private struct EnabledVar
        {
        }
    }
}