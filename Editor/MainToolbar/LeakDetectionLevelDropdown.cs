using System;
using Unity.Collections;
using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.UIElements;

namespace KrasCore.Editor
{
    public class LeakDetectionLevelDropdown
    {
        private const string Path = "KrasCore/Leak Detection Level";
        private static readonly string Name = StringUtils.RemoveAllWhitespace(Path);

        private static Image _iconImage;
        private static TextElement _label;
        
        [MainToolbarElement(Path, defaultDockPosition = MainToolbarDockPosition.Middle)]
        public static MainToolbarElement LeakDetectionLevel()
        {
            var content = new MainToolbarContent((Texture2D)null);
            var element = new MainToolbarDropdown(content, ShowDropdownMenu);
            
            MainToolbarUtils.StyleElement(Name, null, e => e.Q<EditorToolbarDropdown>(), e =>
            {
                if (MainToolbarUtils.Exists(_iconImage)) return;
                
                _iconImage = new Image
                {
                    image = GetIcon(),
                    name = "IconImage"
                };
                _iconImage.AddToClassList("unity-editor-toolbar-element__icon");
                e.Add(_iconImage);
                _iconImage.SendToBack();
            });
            ApplyStyle();
            return element;
        }

        private static void ShowDropdownMenu(Rect dropDownRect)
        {
            var menu = new GenericMenu();

            var modeNames = Enum.GetNames(typeof(NativeLeakDetectionMode));
            foreach (var modeName in modeNames)
            {
                var mode = Enum.Parse<NativeLeakDetectionMode>(modeName);
                var isOn = NativeLeakDetection.Mode == mode;
                
                menu.AddItem(new GUIContent(modeName), isOn, () =>
                {
                    NativeLeakDetection.Mode = mode;
                    ApplyStyle();
                });
            }
            menu.DropDown(dropDownRect);
        }
        
        private static void ApplyStyle()
        {
            MainToolbarUtils.StyleElement(Name, _iconImage, null, e =>
            {
                _iconImage = e;
                _iconImage.image = GetIcon();
            });
            MainToolbarUtils.StyleElement(Name, _label, e => e.Q<TextElement>("EditorToolbarButtonText"), e =>
            {
                _label = e;
                switch (NativeLeakDetection.Mode)
                {
                    case NativeLeakDetectionMode.Disabled:
                    case NativeLeakDetectionMode.Enabled:
                        _label.text = NativeLeakDetection.Mode.ToString();
                        break;
                    case NativeLeakDetectionMode.EnabledWithStackTrace:
                        _label.text = "StackTrace";
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                _label.style.display = DisplayStyle.Flex;
                _label.style.maxWidth = 200;
            });
        }
        
        private static Texture2D GetIcon()
        {
            string iconName;
            switch (NativeLeakDetection.Mode)
            {
                case NativeLeakDetectionMode.Disabled:
                    iconName = "d_DebuggerDisabled";
                    break;
                case NativeLeakDetectionMode.Enabled:
                    iconName = "d_DebuggerEnabled";
                    break;
                case NativeLeakDetectionMode.EnabledWithStackTrace:
                    iconName = "d_DebuggerAttached";
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            var icon = EditorGUIUtility.IconContent(iconName).image as Texture2D;
            return icon;
        }
    }
}