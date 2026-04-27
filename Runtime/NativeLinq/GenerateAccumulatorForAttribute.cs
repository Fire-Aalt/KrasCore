using System;

namespace KrasCore
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public sealed class GenerateAccumulatorForAttribute : Attribute
    {
        public GenerateAccumulatorForAttribute(Type type, DivisorType divisorType)
        {
            Type = type;
            DivisorType = divisorType;
        }

        public Type Type { get; }

        public DivisorType DivisorType { get; }
    }

    public enum DivisorType
    {
        Int,
        UInt,
    }
}
