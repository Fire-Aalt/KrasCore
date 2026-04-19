using System;
using UnityEngine;

namespace KrasCore
{
    [AttributeUsage(AttributeTargets.Field)]
    public class InspectorReadOnlyAttribute : PropertyAttribute
    {
        
    }
}