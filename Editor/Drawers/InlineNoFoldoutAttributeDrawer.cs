using ArtificeToolkit.Editor;
using BovineLabs.Core.Editor.Helpers;
using BovineLabs.Core.Editor.Inspectors;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace KrasCore.Editor
{
    [CustomPropertyDrawer(typeof(InlineNoFoldoutAttribute))]
    public class InlineNoFoldoutAttributeDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var root = new VisualElement();

            if (((InlineNoFoldoutAttribute)attribute).DrawPropertyName)
            {
                var lbl = new Label
                {
                    text = property.displayName,
                };
                root.Add(lbl);
            }
            
            foreach (var prop in SerializedHelper.GetChildren(property))
            {
                var element = CreateElement(prop);
                if (element != null)
                {
                    root.Add(element);
                }
            }
            
            return root;
        }
        
        protected virtual VisualElement CreateElement(SerializedProperty property)
        {
            return PropertyUtil.CreateProperty(property, property.serializedObject);
        }
    }
}