using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace KrasCore.Editor
{
    [CustomPropertyDrawer(typeof(ShowIfAttribute))]
    public class ShowIfPropertyDrawer : PropertyDrawer
    {
        private const BindingFlags MEMBER_BINDING_FLAGS =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private const string ARRAY_PATH_TOKEN = ".Array.data[";

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var root = new VisualElement();
            root.Add(new PropertyField(property));

            void RefreshVisibility()
            {
                root.style.display = ShouldShow(property) ? DisplayStyle.Flex : DisplayStyle.None;
            }

            root.TrackSerializedObjectValue(property.serializedObject, _ => RefreshVisibility());
            root.schedule.Execute(RefreshVisibility).Every(200);
            RefreshVisibility();
            return root;
        }

        private bool ShouldShow(SerializedProperty property)
        {
            var conditions = GetConditions();
            var targets = property.serializedObject.targetObjects;
            if (targets == null || targets.Length == 0)
            {
                return true;
            }

            for (var i = 0; i < targets.Length; i++)
            {
                if (ShouldShowForTarget(targets[i], property.propertyPath, conditions))
                {
                    return true;
                }
            }

            return false;
        }

        private ShowIfCondition[] GetConditions()
        {
            ShowIfAttribute[] attributes;
            if (fieldInfo == null)
            {
                attributes = Array.Empty<ShowIfAttribute>();
            }
            else
            {
                attributes = fieldInfo.GetCustomAttributes(typeof(ShowIfAttribute), true)
                    .OfType<ShowIfAttribute>()
                    .ToArray();
            }

            if (attributes.Length == 0 && attribute is ShowIfAttribute single)
            {
                attributes = new[] { single };
            }

            var groupedConditions = new Dictionary<string, List<object>>(StringComparer.Ordinal);
            for (var i = 0; i < attributes.Length; i++)
            {
                var attr = attributes[i];
                if (attr == null || string.IsNullOrWhiteSpace(attr.ConditionMemberName))
                {
                    continue;
                }

                if (!groupedConditions.TryGetValue(attr.ConditionMemberName, out var expectedValues))
                {
                    expectedValues = new List<object>();
                    groupedConditions.Add(attr.ConditionMemberName, expectedValues);
                }

                if (attr.Values == null || attr.Values.Length == 0)
                {
                    expectedValues.Add(true);
                    continue;
                }

                expectedValues.AddRange(attr.Values);
            }

            var result = new ShowIfCondition[groupedConditions.Count];
            var index = 0;
            foreach (var pair in groupedConditions)
            {
                result[index++] = new ShowIfCondition(pair.Key, pair.Value);
            }

            return result;
        }

        private static bool ShouldShowForTarget(object target, string propertyPath, IReadOnlyList<ShowIfCondition> conditions)
        {
            if (target == null || conditions.Count == 0)
            {
                return true;
            }

            var parentObject = GetParentObject(target, propertyPath) ?? target;
            for (var i = 0; i < conditions.Count; i++)
            {
                var condition = conditions[i];
                if (!TryGetConditionValue(parentObject, target, condition.MemberName, out var conditionValue))
                {
                    continue;
                }

                if (!MatchesAny(conditionValue, condition.ExpectedValues))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool MatchesAny(object conditionValue, IReadOnlyList<object> expectedValues)
        {
            for (var i = 0; i < expectedValues.Count; i++)
            {
                if (AreEqual(conditionValue, expectedValues[i]))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool AreEqual(object left, object right)
        {
            if (ReferenceEquals(left, right))
            {
                return true;
            }

            if (left == null || right == null)
            {
                return false;
            }

            var leftType = left.GetType();
            var rightType = right.GetType();

            if ((leftType.IsEnum || rightType.IsEnum) && TryConvertToInt64(left, out var leftInt) &&
                TryConvertToInt64(right, out var rightInt))
            {
                return leftInt == rightInt;
            }

            if (IsNumericType(leftType) && IsNumericType(rightType))
            {
                return Convert.ToDecimal(left, CultureInfo.InvariantCulture) ==
                       Convert.ToDecimal(right, CultureInfo.InvariantCulture);
            }

            return left.Equals(right);
        }

        private static bool TryConvertToInt64(object value, out long result)
        {
            try
            {
                result = Convert.ToInt64(value, CultureInfo.InvariantCulture);
                return true;
            }
            catch
            {
                result = 0;
                return false;
            }
        }

        private static bool IsNumericType(Type type)
        {
            var typeCode = Type.GetTypeCode(type);
            return typeCode == TypeCode.Byte
                || typeCode == TypeCode.SByte
                || typeCode == TypeCode.UInt16
                || typeCode == TypeCode.UInt32
                || typeCode == TypeCode.UInt64
                || typeCode == TypeCode.Int16
                || typeCode == TypeCode.Int32
                || typeCode == TypeCode.Int64
                || typeCode == TypeCode.Decimal
                || typeCode == TypeCode.Double
                || typeCode == TypeCode.Single;
        }

        private static bool TryGetConditionValue(object primary, object fallback, string memberName, out object value)
        {
            if (TryGetMemberOrMethodValue(primary, memberName, out value))
            {
                return true;
            }

            if (!ReferenceEquals(primary, fallback) && TryGetMemberOrMethodValue(fallback, memberName, out value))
            {
                return true;
            }

            value = null;
            return false;
        }

        private static bool TryGetMemberOrMethodValue(object target, string memberName, out object value)
        {
            if (target == null || string.IsNullOrWhiteSpace(memberName))
            {
                value = null;
                return false;
            }

            if (TryGetMemberValue(target, memberName, out value))
            {
                return true;
            }

            return TryInvokeMethod(target, memberName, out value);
        }

        private static bool TryGetMemberValue(object target, string memberName, out object value)
        {
            if (target == null || string.IsNullOrWhiteSpace(memberName))
            {
                value = null;
                return false;
            }

            var type = target.GetType();
            while (type != null)
            {
                var field = type.GetField(memberName, MEMBER_BINDING_FLAGS | BindingFlags.DeclaredOnly);
                if (field != null)
                {
                    value = field.GetValue(target);
                    return true;
                }

                var property = type.GetProperty(memberName, MEMBER_BINDING_FLAGS | BindingFlags.DeclaredOnly);
                if (property != null && property.CanRead && property.GetIndexParameters().Length == 0)
                {
                    value = property.GetValue(target);
                    return true;
                }

                type = type.BaseType;
            }

            value = null;
            return false;
        }

        private static bool TryInvokeMethod(object target, string methodName, out object value)
        {
            var type = target.GetType();
            while (type != null)
            {
                var method = type.GetMethod(methodName, MEMBER_BINDING_FLAGS | BindingFlags.DeclaredOnly);
                if (method != null && method.GetParameters().Length == 0)
                {
                    value = method.Invoke(target, null);
                    return true;
                }

                type = type.BaseType;
            }

            value = null;
            return false;
        }

        private static object GetParentObject(object root, string propertyPath)
        {
            if (root == null || string.IsNullOrEmpty(propertyPath))
            {
                return root;
            }

            var tokens = Tokenize(propertyPath);
            if (tokens.Count == 0)
            {
                return root;
            }

            tokens.RemoveAt(tokens.Count - 1);
            return WalkPath(root, tokens);
        }

        private static object WalkPath(object root, IReadOnlyList<PathToken> tokens)
        {
            var current = root;
            for (var i = 0; i < tokens.Count; i++)
            {
                var token = tokens[i];
                if (token.IsIndexer)
                {
                    if (!TryGetIndexedValue(current, token.Index, out current))
                    {
                        return null;
                    }
                }
                else if (!TryGetMemberValue(current, token.MemberName, out current))
                {
                    return null;
                }
            }

            return current;
        }

        private static List<PathToken> Tokenize(string propertyPath)
        {
            var normalizedPath = propertyPath.Replace(ARRAY_PATH_TOKEN, "[");
            var rawSegments = normalizedPath.Split('.');
            var tokens = new List<PathToken>(rawSegments.Length);

            for (var i = 0; i < rawSegments.Length; i++)
            {
                var segment = rawSegments[i];
                if (string.IsNullOrEmpty(segment))
                {
                    continue;
                }

                ParseSegment(segment, tokens);
            }

            return tokens;
        }

        private static void ParseSegment(string segment, ICollection<PathToken> tokens)
        {
            var cursor = 0;
            while (cursor < segment.Length)
            {
                var openBracketIndex = segment.IndexOf('[', cursor);
                if (openBracketIndex < 0)
                {
                    var remainingMember = segment.Substring(cursor);
                    if (!string.IsNullOrEmpty(remainingMember))
                    {
                        tokens.Add(PathToken.Member(remainingMember));
                    }

                    break;
                }

                if (openBracketIndex > cursor)
                {
                    tokens.Add(PathToken.Member(segment.Substring(cursor, openBracketIndex - cursor)));
                }

                var closeBracketIndex = segment.IndexOf(']', openBracketIndex + 1);
                if (closeBracketIndex < 0)
                {
                    break;
                }

                var indexText = segment.Substring(openBracketIndex + 1, closeBracketIndex - openBracketIndex - 1);
                if (int.TryParse(indexText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var index))
                {
                    tokens.Add(PathToken.Indexer(index));
                }

                cursor = closeBracketIndex + 1;
            }
        }

        private static bool TryGetIndexedValue(object source, int index, out object value)
        {
            value = null;
            if (source == null || index < 0)
            {
                return false;
            }

            if (source is IList list)
            {
                if (index >= list.Count)
                {
                    return false;
                }

                value = list[index];
                return true;
            }

            if (source is IEnumerable enumerable)
            {
                var currentIndex = 0;
                foreach (var item in enumerable)
                {
                    if (currentIndex == index)
                    {
                        value = item;
                        return true;
                    }

                    currentIndex++;
                }
            }

            return false;
        }

        private readonly struct ShowIfCondition
        {
            public ShowIfCondition(string memberName, IReadOnlyList<object> expectedValues)
            {
                MemberName = memberName;
                ExpectedValues = expectedValues;
            }

            public string MemberName { get; }
            public IReadOnlyList<object> ExpectedValues { get; }
        }

        private readonly struct PathToken
        {
            private PathToken(string memberName, int index, bool isIndexer)
            {
                MemberName = memberName;
                Index = index;
                IsIndexer = isIndexer;
            }

            public string MemberName { get; }
            public int Index { get; }
            public bool IsIndexer { get; }

            public static PathToken Member(string memberName)
            {
                return new PathToken(memberName, -1, false);
            }

            public static PathToken Indexer(int index)
            {
                return new PathToken(null, index, true);
            }
        }
    }
}
