using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace KrasCore.Editor
{
    [DrawerPriority(1, 0, 0)]
    public class InlineNoFoldoutAttributeDrawer : OdinAttributeDrawer<InlineNoFoldoutAttribute>
    {
        protected override void DrawPropertyLayout(GUIContent label)
        {
            if (Attribute.DrawPropertyName)
            {
                EditorGUI.LabelField(EditorGUILayout.GetControlRect(GUILayout.ExpandWidth(true)), label);
            }
            
            foreach (var property in Property.Children)
            {
                property.Draw();
            }
        }
    }
}