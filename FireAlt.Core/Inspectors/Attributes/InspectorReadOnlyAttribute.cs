using System;
using UnityEngine;

namespace FireAlt.Core.Inspectors
{
    [AttributeUsage(AttributeTargets.Field)]
    public class InspectorReadOnlyAttribute : PropertyAttribute
    {
        
    }
}