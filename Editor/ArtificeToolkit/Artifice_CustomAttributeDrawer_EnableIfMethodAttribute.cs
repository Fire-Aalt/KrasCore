using System.Linq;
using System.Reflection;
using ArtificeToolkit.Editor;
using ArtificeToolkit.Editor.Artifice_CustomAttributeDrawers;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace KrasCore.Editor
{
    [Artifice_CustomAttributeDrawer(typeof(EnableIfMethodAttribute))]
    public class Artifice_CustomAttributeDrawer_EnableIfMethodAttribute : Artifice_CustomAttributeDrawer
    {
        private EnableIfMethodAttribute _attribute;
        private MethodInfo _trackedMethod;
        private object _methodOwner;
        private VisualElement _targetElem;
        
        public override VisualElement OnWrapGUI(SerializedProperty property, VisualElement root)
        {
            _targetElem = root;
            _attribute = (EnableIfMethodAttribute)Attribute;

            _methodOwner = SerializationUtils.GetParentObject(property);
            _trackedMethod = ReflectionUtils.GetCallMethod(_methodOwner, _attribute.MethodName);
            
            var trackerElement = new VisualElement();
            trackerElement.name = "Tracker Element";
            trackerElement.tooltip = "Used only for TrackPropertyValue method";
            _targetElem.Add(trackerElement);

            UpdateRootVisibility();
            trackerElement.schedule.Execute(UpdateRootVisibility).Every(250); 
            
            return _targetElem;
        }

        private void UpdateRootVisibility()
        {
            var trackedValue = _trackedMethod.Invoke(_methodOwner, null);
            if (_attribute.Values.Any(value => Artifice_Utilities.AreEqual(trackedValue, value)))
                _targetElem.RemoveFromClassList("hide");
            else
                _targetElem.AddToClassList("hide");
        }
    }
}