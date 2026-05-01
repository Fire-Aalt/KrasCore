using System;
using ArtificeToolkit.Editor;
using UnityEditor;
using UnityEngine.UIElements;

namespace KrasCore.Editor
{
    public class Artifice_EditorWindow : EditorWindow
    {
        public virtual void CreateGUI()
        {
            rootVisualElement.Add(new ArtificeDrawer().CreateInspectorGUI(new SerializedObject(this)));
        }
        
        protected void RegisterEnableButtonIf(string buttonName, Func<bool> showCondition)
        {
            var modifyButton = rootVisualElement.Q<Button>(buttonName);
            modifyButton.schedule.Execute(UpdateButtonVisibility).Every(250);
            
            void UpdateButtonVisibility()
            {
                modifyButton.style.display = showCondition() ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }
    }
}