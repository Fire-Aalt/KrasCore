using UnityEditor;
using UnityEditor.UIElements;

namespace FireAlt.Core.Editor
{
    public static class PropertyUtils
    {
        public static PropertyField CreateProperty(SerializedProperty property, SerializedObject serializedObject)
        {
            var field = new PropertyField(property)
            {
                name = "PropertyField:" + property?.propertyPath,
            };

            field.Bind(serializedObject);
            return field;
        }

        public static PropertyField CreateProperty(SerializedProperty property)
        {
            var field = new PropertyField(property)
            {
                name = "PropertyField:" + property?.propertyPath,
            };

            return field;
        }
    }
}