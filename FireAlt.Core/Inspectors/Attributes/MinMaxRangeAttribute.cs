using System;
using UnityEngine;

namespace FireAlt.Core.Inspectors
{
    [AttributeUsage(AttributeTargets.Field)]
    public class MinMaxRangeAttribute : PropertyAttribute
    {
        public MinMaxRangeAttribute(float min, float max)
        {
            Min = min;
            Max = max;
        }

        public float Min { get; }

        public float Max { get; }
    }
}