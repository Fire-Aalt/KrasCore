using UnityEditor;
using UnityEngine.UIElements;

namespace KrasCore.Editor
{
    [CustomPropertyDrawer(typeof(InspectorReadOnlyAttribute))]
    public class InspectorReadOnlyPropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var propertyField = PropertyUtils.CreateProperty(property, property.serializedObject);
            propertyField.SetEnabled(false);
            return propertyField;
        }
    }
}