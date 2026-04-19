using Unity.Properties;
using UnityEditor;

namespace KrasCore.Editor
{
    public static class SerializationUtils
    {
        private const float CHARACTER_WIDTH_ESTIMATE = 7.5f;

        public static object GetParentObject(SerializedProperty property)
        {
            var path = property.propertyPath;
            var i = path.LastIndexOf('.');
            
            if (i < 0)
            {
                return property.serializedObject.targetObject;
            }
            
            var parent = property.serializedObject.FindProperty(path.Substring(0, i));
            return parent.boxedValue;
        }
        
        public static PropertyPath ToPropertyPath(SerializedProperty property)
        {
            var path = property.propertyPath;
            // For lists
            path = path.Replace(".Array.data[", "[");
            // For arrays (untested)
            path = path.Replace(".data[", "[");
            
            return new PropertyPath(path);
        }

        public static SerializedProperty FindRelativeProperty(SerializedProperty property, string relativePropertyPath)
        {
            if (property == null || string.IsNullOrEmpty(relativePropertyPath))
            {
                return null;
            }

            var rootProperty = property.serializedObject.FindProperty(property.propertyPath);
            return rootProperty?.FindPropertyRelative(relativePropertyPath);
        }

        public static SerializedProperty FindSiblingProperty(SerializedProperty property, string siblingPropertyName)
        {
            if (property == null || string.IsNullOrEmpty(siblingPropertyName))
            {
                return null;
            }

            var path = property.propertyPath;
            var i = path.LastIndexOf('.');
            if (i <= 0)
            {
                return null;
            }

            return property.serializedObject.FindProperty($"{path.Substring(0, i)}.{siblingPropertyName}");
        }

        public static string TrimNameToWidth(string name, float width)
        {
            if (string.IsNullOrEmpty(name))
            {
                return name ?? string.Empty;
            }

            var maxLength = width / CHARACTER_WIDTH_ESTIMATE;
            if (name.Length < maxLength)
            {
                return name;
            }

            var parts = name.Split('.');
            var trimmedName = parts[^1];
            var length = trimmedName.Length;

            for (var p = parts.Length - 2; p >= 0; p--)
            {
                length += parts[p].Length + 1;
                if (length > maxLength)
                {
                    return trimmedName;
                }

                trimmedName = parts[p] + "." + trimmedName;
            }

            return trimmedName;
        }
    }
}
