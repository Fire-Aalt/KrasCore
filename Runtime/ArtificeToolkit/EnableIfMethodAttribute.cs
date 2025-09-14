using ArtificeToolkit.Attributes;

namespace KrasCore
{
    public class EnableIfMethodAttribute : CustomAttribute, IArtifice_ArrayAppliedAttribute
    {
        public readonly string MethodName;
        public readonly object[] Values;
        
        /// <summary> Property will be enabled if value parameter matches the property value </summary>
        public EnableIfMethodAttribute(string methodName, object value)
        {
            MethodName = methodName;
            Values = new object[1];
            Values[0] = value;
        }
        
        /// <summary> Property will be enabled if any value matches the property value </summary>
        public EnableIfMethodAttribute(string methodName, params object[] values)
        {
            MethodName = methodName;
            Values = values;
        }
    }
}