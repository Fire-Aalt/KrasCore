using System;
using UnityEngine;

namespace FireAlt.Core
{
    [AttributeUsage(AttributeTargets.Field)]
    public class InspectorReadOnlyAttribute : PropertyAttribute
    {
        
    }
}