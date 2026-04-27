using System;

namespace KrasCore
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public sealed class GenerateAccumulatorForAttribute : Attribute
    {
        public GenerateAccumulatorForAttribute(Type type)
        {
            Type = type;
        }

        public Type Type { get; }
    }
}
