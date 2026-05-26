// using System;
// using ArtificeToolkit.Attributes;
//
// namespace KrasCore
// {
//     [AttributeUsage(AttributeTargets.Method)]
//     public class ButtonEnableIfMethodAttribute : CustomAttribute
//     {
//         public readonly string MethodName;
//         public readonly object[] Values;
//         
//         public readonly bool ShouldAddOnSlidingPanel = false;
//         public readonly string[] ParameterNames = null;
//         
//         /// <summary> Property will be enabled if value parameter matches the property value </summary>
//         public ButtonEnableIfMethodAttribute(string methodName, object value)
//         {
//             MethodName = methodName;
//             Values = new object[1];
//             Values[0] = value;
//         }
//         
//         /// <summary> Property will be enabled if any value matches the property value </summary>
//         public ButtonEnableIfMethodAttribute(string methodName, params object[] values)
//         {
//             MethodName = methodName;
//             Values = values;
//         }
//     }
// }