using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace KrasCore.Editor
{
    [CustomPropertyDrawer(typeof(EnumToggleButtonsAttribute))]
    public class EnumToggleButtonsPropertyDrawer : PropertyDrawer
    {
        private const float BUTTON_MIN_WIDTH = 50f;
        private const float BUTTON_HEIGHT = 28f;
        private const float BUTTON_BORDER_WIDTH = 1f;

        private static readonly Color DARK_BUTTON_BG = new(0.345f, 0.345f, 0.345f, 1f); // #585858
        private static readonly Color DARK_BUTTON_TEXT = new(0.92f, 0.92f, 0.92f, 1f);
        private static readonly Color DARK_BUTTON_BG_PRESSED = new(0.702f, 0.420f, 0.129f, 1f); // #b36b21
        private static readonly Color DARK_BUTTON_TEXT_PRESSED = new(1f, 0.96f, 0.88f, 1f);
        private static readonly Color DARK_BUTTON_BORDER = new(0.137f, 0.137f, 0.137f, 1f); // #232323

        private static readonly Color LIGHT_BUTTON_BG = new(0.812f, 0.812f, 0.812f, 1f); // #cfcfcf
        private static readonly Color LIGHT_BUTTON_TEXT = new(0.14f, 0.14f, 0.14f, 1f);
        private static readonly Color LIGHT_BUTTON_BG_PRESSED = new(0.847f, 0.518f, 0.184f, 1f); // #d8842f
        private static readonly Color LIGHT_BUTTON_TEXT_PRESSED = Color.white;
        private static readonly Color LIGHT_BUTTON_BORDER = new(0.6f, 0.6f, 0.6f, 1f); // #999999

        private struct ButtonState
        {
            public Button Button;
            public int Value;
            public bool ExactMatch;
        }

        private sealed class EnumToggleButtonsField : BaseField<int>
        {
            public EnumToggleButtonsField(string label, bool hideLabel)
                : this(label, hideLabel, new VisualElement())
            {
            }

            private EnumToggleButtonsField(string label, bool hideLabel, VisualElement inputElement)
                : base(label, inputElement)
            {
                this.InputElement = inputElement;
                this.AddToClassList(alignedFieldUssClassName);
                this.style.width = Length.Percent(100);
                this.style.flexGrow = 1f;

                this.InputElement.AddToClassList("unity-property-field__input");
                this.InputElement.style.width = Length.Percent(100);
                this.InputElement.style.flexGrow = 1f;
                this.InputElement.style.minWidth = 0f;

                if (hideLabel)
                {
                    this.labelElement.style.display = DisplayStyle.None;
                    this.AddToClassList(noLabelVariantUssClassName);
                }
                else
                {
                    this.labelElement.AddToClassList("unity-property-field__label");
                    this.labelElement.style.unityTextAlign = TextAnchor.MiddleLeft;
                }
            }

            public VisualElement InputElement { get; }
        }

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            if (property.propertyType != SerializedPropertyType.Enum)
            {
                return new PropertyField(property);
            }

            var enumType = this.fieldInfo?.FieldType;
            if (enumType == null || !enumType.IsEnum)
            {
                return new PropertyField(property);
            }

            var isDarkTheme = EditorGUIUtility.isProSkin;
            var attribute = (EnumToggleButtonsAttribute)this.attribute;
            var useFlags = enumType.IsDefined(typeof(FlagsAttribute), false);
            var enumValues = Enum.GetValues(enumType);
            var allFlags = CalculateAllFlags(enumValues);

            var root = new EnumToggleButtonsField(property.displayName, attribute.HideLabel);
            var buttonsContainer = root.InputElement;
            buttonsContainer.style.flexDirection = FlexDirection.Row;
            buttonsContainer.style.flexWrap = Wrap.Wrap;
            buttonsContainer.style.justifyContent = Justify.FlexStart;

            var states = new List<ButtonState>();

            if (useFlags)
            {
                var allButton = BuildButton("All", isDarkTheme);
                allButton.clicked += () =>
                {
                    property.serializedObject.Update();
                    property.enumValueFlag = property.enumValueFlag == allFlags ? 0 : allFlags;
                    property.serializedObject.ApplyModifiedProperties();
                    UpdateButtonVisuals(property, states, useFlags, isDarkTheme);
                };

                buttonsContainer.Add(allButton);
                states.Add(new ButtonState { Button = allButton, Value = allFlags, ExactMatch = true });
            }

            foreach (var enumValue in enumValues)
            {
                var valueName = enumValue.ToString();
                var valueInt = Convert.ToInt32(enumValue);
                var button = BuildButton(valueName, isDarkTheme);
                button.clicked += () =>
                {
                    property.serializedObject.Update();
                    property.enumValueFlag = useFlags ? property.enumValueFlag ^ valueInt : valueInt;
                    property.serializedObject.ApplyModifiedProperties();
                    UpdateButtonVisuals(property, states, useFlags, isDarkTheme);
                };

                buttonsContainer.Add(button);
                states.Add(new ButtonState
                {
                    Button = button,
                    Value = valueInt,
                    ExactMatch = useFlags && valueInt == 0,
                });
            }

            void UpdateVisuals()
            {
                UpdateButtonVisuals(property, states, useFlags, isDarkTheme);
            }

            root.TrackPropertyValue(property, _ => UpdateVisuals());
            Undo.undoRedoPerformed += UpdateVisuals;
            root.RegisterCallback<DetachFromPanelEvent>(_ => Undo.undoRedoPerformed -= UpdateVisuals);

            UpdateVisuals();
            return root;
        }

        private static int CalculateAllFlags(Array enumValues)
        {
            var allFlags = 0;
            foreach (var enumValue in enumValues)
            {
                allFlags |= Convert.ToInt32(enumValue);
            }

            return allFlags;
        }

        private static Button BuildButton(string label, bool isDarkTheme)
        {
            var button = new Button { text = label };
            button.style.minWidth = BUTTON_MIN_WIDTH;
            button.style.height = BUTTON_HEIGHT;
            button.style.flexGrow = 1f;
            button.style.marginRight = 1f;
            button.style.marginBottom = 1f;
            button.style.borderTopWidth = BUTTON_BORDER_WIDTH;
            button.style.borderRightWidth = BUTTON_BORDER_WIDTH;
            button.style.borderBottomWidth = BUTTON_BORDER_WIDTH;
            button.style.borderLeftWidth = BUTTON_BORDER_WIDTH;
            button.style.unityFontStyleAndWeight = FontStyle.Bold;
            button.style.unityTextAlign = TextAnchor.MiddleCenter;

            ApplyButtonStyle(button, false, isDarkTheme);
            return button;
        }

        private static void UpdateButtonVisuals(
            SerializedProperty property,
            IReadOnlyList<ButtonState> states,
            bool useFlags,
            bool isDarkTheme)
        {
            property.serializedObject.Update();
            var currentValue = property.enumValueFlag;

            for (var i = 0; i < states.Count; i++)
            {
                var state = states[i];
                var isPressed = IsPressed(currentValue, state.Value, state.ExactMatch, useFlags);
                ApplyButtonStyle(state.Button, isPressed, isDarkTheme);
            }
        }

        private static bool IsPressed(int currentValue, int buttonValue, bool exactMatch, bool useFlags)
        {
            if (exactMatch || !useFlags)
            {
                return currentValue == buttonValue;
            }

            return (currentValue & buttonValue) == buttonValue;
        }

        private static void ApplyButtonStyle(Button button, bool isPressed, bool isDarkTheme)
        {
            if (isPressed)
            {
                button.style.backgroundColor = isDarkTheme ? DARK_BUTTON_BG_PRESSED : LIGHT_BUTTON_BG_PRESSED;
                button.style.color = isDarkTheme ? DARK_BUTTON_TEXT_PRESSED : LIGHT_BUTTON_TEXT_PRESSED;
            }
            else
            {
                button.style.backgroundColor = isDarkTheme ? DARK_BUTTON_BG : LIGHT_BUTTON_BG;
                button.style.color = isDarkTheme ? DARK_BUTTON_TEXT : LIGHT_BUTTON_TEXT;
            }

            var borderColor = isDarkTheme ? DARK_BUTTON_BORDER : LIGHT_BUTTON_BORDER;
            button.style.borderTopColor = borderColor;
            button.style.borderRightColor = borderColor;
            button.style.borderBottomColor = borderColor;
            button.style.borderLeftColor = borderColor;
        }
    }
}
