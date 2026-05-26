using System;
using UnityEngine;

namespace KrasCore
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
    public class ShowIfAttribute : PropertyAttribute
    {
        public readonly string ConditionMemberName;
        public readonly object[] Values;

        public ShowIfAttribute(string conditionMemberName)
            : this(conditionMemberName, true)
        {
        }

        public ShowIfAttribute(string conditionMemberName, object value)
        {
            ConditionMemberName = conditionMemberName;
            Values = new[] { value };
        }

        public ShowIfAttribute(string conditionMemberName, params object[] values)
        {
            ConditionMemberName = conditionMemberName;
            Values = values == null || values.Length == 0 ? new object[] { true } : values;
        }
    }
}
