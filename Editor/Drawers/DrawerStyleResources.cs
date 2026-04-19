using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace KrasCore.Editor
{
    [InitializeOnLoad]
    public static class DrawerStyleResources
    {
        public static readonly StyleSheet CommonStyleSheet;
        public static readonly StyleSheet EnumToggleButtonsStyleSheet;
        public static readonly StyleSheet InlineScriptableObjectStyleSheet;

        private const string ValidationRoot = "com.firealt.krascore";

        static DrawerStyleResources()
        {
            CommonStyleSheet = Load<StyleSheet>("Styles/DrawerCommon.uss");
            EnumToggleButtonsStyleSheet = Load<StyleSheet>("Styles/EnumToggleButtonsDrawer.uss");
            InlineScriptableObjectStyleSheet = Load<StyleSheet>("Styles/InlineScriptableObjectDrawer.uss");
        }

        private static T Load<T>(string path) where T : Object
        {
            return AssetDatabaseUtils.LoadEditorResource<T>(path, ValidationRoot);
        }
    }
}
