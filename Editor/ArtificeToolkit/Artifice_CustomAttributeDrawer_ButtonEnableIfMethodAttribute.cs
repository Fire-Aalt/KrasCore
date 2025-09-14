// using System;
// using System.Collections.Generic;
// using System.Linq;
// using System.Reflection;
// using System.Text;
// using ArtificeToolkit.Attributes;
// using ArtificeToolkit.Editor;
// using ArtificeToolkit.Editor.Artifice_CustomAttributeDrawers;
// using UnityEditor;
// using UnityEngine.UIElements;
//
// namespace KrasCore.Editor
// {
//     [Serializable]
//     [Artifice_CustomAttributeDrawer(typeof(ButtonEnableIfMethodAttribute))]
//     public class Artifice_CustomAttributeDrawer_ButtonEnableIfMethodAttribute : Artifice_CustomAttributeDrawer
//     {
//         private Button _button;
//         
//         /// <summary> Returns button for method button GUI from a serialized object or property. Works with multiselect as well. </summary>
//         public VisualElement CreateMethodGUI<T>(T serializedData, MethodInfo methodInfo) where T : class
//         {
//             _attribute = (ButtonEnableIfMethodAttribute)Attribute;
//             _trackedMethod = ReflectionUtils.GetCallMethod(_methodOwner, _attribute.MethodName);
//             
//             _button = new Button(() =>
//             {
//                 var serializedObject = serializedData switch
//                 {
//                     SerializedObject obj => obj,
//                     SerializedProperty property => property.serializedObject,
//                     _ => throw new ArgumentException("Invalid serialized data type.")
//                 };
//                 
//                 var targets = serializedObject.targetObjects;
//                 serializedObject.Update();
//
//                 foreach (var target in targets)
//                 {
//                     // We need to find the invocation target of the Button method, since it can belong to nested property of the SerializedObject. Thus target is not enough.
//                     object invocationTarget = null;
//                     var invocationSerializedObject = new SerializedObject(target);
//                     if (serializedData is SerializedObject)
//                         invocationTarget = invocationSerializedObject.targetObject;
//                     else
//                     {
//                         var serializedProperty = serializedData as SerializedProperty;
//                         invocationTarget = invocationSerializedObject.FindProperty(serializedProperty.propertyPath).GetTarget<object>();
//                     }
//                  
//                     // Get parameter values specific to this target (you may need to refactor GetParameterList to support this)
//                     var parametersList = GetParameterListForTarget(invocationTarget);
//                     
//                     if (methodInfo.GetParameters().Length != parametersList.Count)
//                         throw new ArgumentException(
//                             $"[Artifice/Button] Parameters count do not match with method {methodInfo.Name}");
//
//                     methodInfo.Invoke(invocationTarget, parametersList.ToArray());
//                 }
//
//                 serializedObject.ApplyModifiedProperties();
//             });
//
//             _button.text = AddSpacesBeforeCapitals(methodInfo.Name);
//             _button.styleSheets.Add(Artifice_Utilities.GetStyle(GetType()));
//             _button.AddToClassList("button");
//
//             
//             _methodOwner = serializedData switch
//             {
//                 SerializedObject obj => obj.targetObject,
//                 SerializedProperty property => SerializationUtils.GetParentObject(property),
//                 _ => throw new ArgumentException("Invalid serialized data type.")
//             };
//             
//
//             // UpdateRootVisibility();
//             // _button.schedule.Execute(UpdateRootVisibility).Every(250); 
//             
//             return _button;
//         }
//         
//         private void UpdateRootVisibility()
//         {
//             var trackedValue = _trackedMethod.Invoke(_methodOwner, null);
//             
//             if (_attribute.Values.Any(value => Artifice_Utilities.AreEqual(trackedValue, value)))
//                 _button.RemoveFromClassList("hide");
//             else
//                 _button.AddToClassList("hide");
//         }
//         
//         private ButtonEnableIfMethodAttribute _attribute;
//         private MethodInfo _trackedMethod;
//         private object _methodOwner;
//
//         /// <summary> Retrieves a list of parameters for the method invocation based on the attribute parameter names. </summary>
//         private List<object> GetParameterListForTarget(object target)
//         {
//             var parametersList = new List<object>();
//
//             foreach (var parameterName in _attribute.ParameterNames)
//             {
//                 var field = target.GetType().GetField(parameterName,
//                     BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
//
//                 if (field == null)
//                 {
//                     var property = target.GetType().GetProperty(parameterName,
//                         BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
//
//                     if (property == null)
//                         throw new ArgumentException($"[Artifice/Button] Cannot find parameter '{parameterName}' on {target}");
//
//                     parametersList.Add(property.GetValue(target));
//                 }
//                 else
//                 {
//                     parametersList.Add(field.GetValue(target));
//                 }
//             }
//
//             return parametersList;
//         }
//
//         private string AddSpacesBeforeCapitals(string input)
//         {
//             if (string.IsNullOrEmpty(input))
//                 return input;
//
//             var spacedString = new StringBuilder();
//             spacedString.Append(input[0]);
//
//             for (int i = 1; i < input.Length; i++)
//             {
//                 if (char.IsUpper(input[i]))
//                 {
//                     spacedString.Append(' '); // Add a space before capital letter
//                 }
//
//                 spacedString.Append(input[i]);
//             }
//
//             return spacedString.ToString();
//         }
//     }
// }