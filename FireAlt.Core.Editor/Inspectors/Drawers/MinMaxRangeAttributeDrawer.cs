using FireAlt.Core.Inspectors;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace FireAlt.Core.Editor.Inspectors
{
    [CustomPropertyDrawer(typeof(MinMaxRangeAttribute))]
    public class MinMaxRangeAttributeDrawer : PropertyDrawer
    {
        /// <inheritdoc />
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var attr = (MinMaxRangeAttribute)attribute;

            if (property.type == "Vector2" || property.type == "float2")
            {
                var px = property.FindPropertyRelative("x");
                var py = property.FindPropertyRelative("y");

                var ve = new Foldout
                {
                    text = property.displayName,
                    value = true,
                };

                var minMaxField = new MinMaxSlider("Range", px.floatValue, py.floatValue, attr.Min, attr.Max);
                minMaxField.AddToClassList(MinMaxSlider.alignedFieldUssClassName);

                var minField = new FloatField("Min") { value = px.floatValue };
                minField.AddToClassList(FloatField.alignedFieldUssClassName);

                var maxField = new FloatField("Max") { value = py.floatValue };
                maxField.AddToClassList(FloatField.alignedFieldUssClassName);

                minMaxField.RegisterValueChangedCallback(evt =>
                {
                    var v = evt.newValue;
                    px.floatValue = v.x;
                    py.floatValue = v.y;

                    property.serializedObject.ApplyModifiedProperties();
                    minField.SetValueWithoutNotify(v.x);
                    maxField.SetValueWithoutNotify(v.y);
                });

                minField.RegisterValueChangedCallback(evt =>
                {
                    var v = Mathf.Max(evt.newValue, attr.Min);
                    minField.SetValueWithoutNotify(v);
                    minMaxField.minValue = v;
                });

                maxField.RegisterValueChangedCallback(evt =>
                {
                    var v = Mathf.Min(evt.newValue, attr.Max);
                    maxField.SetValueWithoutNotify(v);
                    minMaxField.maxValue = v;
                });

                ve.Add(minMaxField);
                ve.Add(minField);
                ve.Add(maxField);
                return ve;
            }

            if (property.type == "Vector2Int" ||  property.type == "int2")
            {
                var px = property.FindPropertyRelative("x");
                var py = property.FindPropertyRelative("y");

                var x = px.intValue;
                var y = py.intValue;

                var min = attr.Min <= int.MinValue ? int.MinValue : Mathf.CeilToInt(attr.Min);
                var max = attr.Max >= int.MaxValue ? int.MaxValue : Mathf.FloorToInt(attr.Max);

                var ve = new Foldout
                {
                    text = property.displayName,
                    value = true,
                };

                var minMaxField = new MinMaxSlider("Range", x, y, min, max);
                minMaxField.AddToClassList(MinMaxSlider.alignedFieldUssClassName);

                var minField = new IntegerField("Min") { value = px.intValue };
                minField.AddToClassList(MinMaxSlider.alignedFieldUssClassName);

                var maxField = new IntegerField("Max") { value = py.intValue };
                maxField.AddToClassList(MinMaxSlider.alignedFieldUssClassName);

                minMaxField.RegisterValueChangedCallback(evt =>
                {
                    var v = evt.newValue;

                    px.intValue = v.x <= int.MinValue ? int.MinValue : Mathf.CeilToInt(v.x);
                    py.intValue = v.y >= int.MaxValue ? int.MaxValue : Mathf.FloorToInt(v.y);

                    property.serializedObject.ApplyModifiedProperties();
                    minField.SetValueWithoutNotify(px.intValue);
                    maxField.SetValueWithoutNotify(py.intValue);
                });

                minField.RegisterValueChangedCallback(evt =>
                {
                    var v = Mathf.CeilToInt(Mathf.Max(evt.newValue, min));
                    minField.SetValueWithoutNotify(v);
                    minMaxField.minValue = v;
                });

                maxField.RegisterValueChangedCallback(evt =>
                {
                    var v = Mathf.FloorToInt(Mathf.Min(evt.newValue, max));
                    maxField.SetValueWithoutNotify(v);
                    minMaxField.maxValue = v;
                });

                ve.Add(minMaxField);
                ve.Add(minField);
                ve.Add(maxField);
                return ve;
            }

            return new Label($"{nameof(MinMaxRangeAttribute)} only works on these types: {nameof(Vector2)}, {nameof(Vector2Int)}, {nameof(float2)}, {nameof(int2)}");
        }
    }
}