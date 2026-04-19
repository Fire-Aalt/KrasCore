using BovineLabs.Core.Editor.Inspectors;
using UnityEditor;
using UnityEngine.UIElements;

namespace KrasCore.Editor
{
    [CustomPropertyDrawer(typeof(InspectorReadOnlyAttribute))]
    public class InspectorReadOnlyPropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var propertyField = PropertyUtil.CreateProperty(property, property.serializedObject);
            propertyField.SetEnabled(false);
            return propertyField;
        }
    }
}