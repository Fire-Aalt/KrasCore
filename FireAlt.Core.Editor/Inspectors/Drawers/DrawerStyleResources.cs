using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace FireAlt.Core.Editor.Inspectors
{
    [InitializeOnLoad]
    public static class DrawerStyleResources
    {
        public static readonly StyleSheet CommonStyleSheet;
        public static readonly StyleSheet EnumToggleButtonsStyleSheet;
        public static readonly StyleSheet InlineScriptableObjectStyleSheet;

        static DrawerStyleResources()
        {
            CommonStyleSheet = Load<StyleSheet>("Styles/DrawerCommon.uss");
            EnumToggleButtonsStyleSheet = Load<StyleSheet>("Styles/EnumToggleButtonsDrawer.uss");
            InlineScriptableObjectStyleSheet = Load<StyleSheet>("Styles/InlineScriptableObjectDrawer.uss");
        }

        private static T Load<T>(string path) where T : Object
        {
            return AssetDatabaseUtils.LoadEditorResource<T>(path, "com.firealt.krascore");
        }
    }
}
