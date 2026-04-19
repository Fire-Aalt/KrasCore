using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace KrasCore.Editor
{
    [CustomPropertyDrawer(typeof(InlineScriptableObjectAttribute))]
    public class InlineScriptableObjectPropertyDrawer : PropertyDrawer
    {
        private const string SCRIPT_PROPERTY_NAME = "m_Script";
        private const float ROOT_VERTICAL_MARGIN = 5f;
        private const float BOX_BORDER_WIDTH = 1f;
        private const float ACCENT_BORDER_WIDTH = 3f;
        private static readonly Color ACCENT_ORANGE = new(0.702f, 0.420f, 0.129f, 1f); // #b36b21
        private static readonly Color ACCENT_ORANGE_TEXT = new(0.941f, 0.702f, 0.431f, 1f); // #f0b36e

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var container = new VisualElement();
            StyleRoot(container);

            if (property.propertyType != SerializedPropertyType.ObjectReference)
            {
                container.Add(new Label("InlineScriptableObjectAttribute can only be used on Object references."));
                container.Add(new PropertyField(property));
                return container;
            }

            var fieldType = this.fieldInfo?.FieldType;
            if (fieldType == null || !typeof(ScriptableObject).IsAssignableFrom(fieldType))
            {
                container.Add(new Label("InlineScriptableObjectAttribute can only be used on ScriptableObject fields."));
                container.Add(new PropertyField(property));
                return container;
            }

            var objectField = new ObjectField(property.displayName)
            {
                objectType = fieldType,
                value = property.objectReferenceValue,
            };

            objectField.AddToClassList(BaseField<Object>.alignedFieldUssClassName);
            StyleObjectField(objectField);

            var headerContainer = new VisualElement();
            StyleHeader(headerContainer);
            headerContainer.Add(objectField);

            var bodyContainer = new VisualElement();
            StyleBody(bodyContainer);

            container.Add(headerContainer);
            container.Add(bodyContainer);

            objectField.RegisterValueChangedCallback(change =>
            {
                property.objectReferenceValue = change.newValue;
                property.serializedObject.ApplyModifiedProperties();
                Rebuild(bodyContainer, objectField, property);
            });

            container.TrackPropertyValue(property, _ => Rebuild(bodyContainer, objectField, property));
            Rebuild(bodyContainer, objectField, property);

            return container;
        }

        private static void Rebuild(VisualElement bodyContainer, ObjectField objectField, SerializedProperty property)
        {
            objectField.SetValueWithoutNotify(property.objectReferenceValue);
            bodyContainer.Clear();

            if (property.hasMultipleDifferentValues)
            {
                AddSubtleInfoLabel(bodyContainer, "Multiple different values.");
                return;
            }

            if (property.objectReferenceValue == null)
            {
                return;
            }

            if (property.objectReferenceValue is not ScriptableObject)
            {
                AddSubtleInfoLabel(bodyContainer, "Assigned reference is not a ScriptableObject.");
                return;
            }

            var serializedObject = new SerializedObject(property.objectReferenceValue);
            var iterator = serializedObject.GetIterator();
            var enterChildren = true;
            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (iterator.name == SCRIPT_PROPERTY_NAME)
                {
                    continue;
                }

                var child = iterator.Copy();
                var childField = new PropertyField(child);
                childField.Bind(serializedObject);
                bodyContainer.Add(childField);
            }
        }

        private static void AddSubtleInfoLabel(VisualElement parent, string text)
        {
            var label = new Label(text);
            label.style.color = new Color(1f, 1f, 1f, 0.45f);
            label.style.marginTop = 2f;
            parent.Add(label);
        }

        private static void StyleRoot(VisualElement container)
        {
            container.style.marginTop = ROOT_VERTICAL_MARGIN;
            container.style.marginBottom = ROOT_VERTICAL_MARGIN;
        }

        private static void StyleHeader(VisualElement header)
        {
            header.style.backgroundColor = new Color(0.165f, 0.165f, 0.165f, 1f);
            header.style.borderTopWidth = BOX_BORDER_WIDTH;
            header.style.borderBottomWidth = BOX_BORDER_WIDTH;
            header.style.borderLeftWidth = ACCENT_BORDER_WIDTH;
            header.style.borderRightWidth = BOX_BORDER_WIDTH;
            header.style.borderTopColor = Color.black;
            header.style.borderBottomColor = Color.black;
            header.style.borderLeftColor = ACCENT_ORANGE;
            header.style.borderRightColor = Color.black;
            header.style.paddingTop = 4f;
            header.style.paddingBottom = 4f;
            header.style.paddingLeft = 6f;
            header.style.paddingRight = 6f;
        }

        private static void StyleBody(VisualElement body)
        {
            body.style.backgroundColor = new Color(0.247f, 0.247f, 0.247f, 1f);
            body.style.borderLeftWidth = BOX_BORDER_WIDTH;
            body.style.borderRightWidth = BOX_BORDER_WIDTH;
            body.style.borderBottomWidth = BOX_BORDER_WIDTH;
            body.style.borderLeftColor = Color.black;
            body.style.borderRightColor = Color.black;
            body.style.borderBottomColor = Color.black;
            body.style.paddingTop = 4f;
            body.style.paddingBottom = 6f;
            body.style.paddingLeft = 6f;
            body.style.paddingRight = 6f;
        }

        private static void StyleObjectField(ObjectField objectField)
        {
            objectField.style.marginTop = 0f;
            objectField.style.marginBottom = 0f;

            if (objectField.labelElement != null)
            {
                objectField.labelElement.style.color = ACCENT_ORANGE_TEXT;
                objectField.labelElement.style.unityFontStyleAndWeight = FontStyle.Bold;
            }
        }
    }
}
