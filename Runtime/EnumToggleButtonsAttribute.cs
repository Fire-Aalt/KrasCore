using System;
using UnityEngine;

namespace KrasCore
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Enum, AllowMultiple = false)]
    public class EnumToggleButtonsAttribute : PropertyAttribute
    {
        public readonly bool HideLabel;

        public EnumToggleButtonsAttribute(bool hideLabel = false)
        {
            HideLabel = hideLabel;
        }
    }
}
