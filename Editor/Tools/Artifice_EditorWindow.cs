using ArtificeToolkit.Editor;
using UnityEditor;

namespace KrasCore.Editor
{
    public class Artifice_EditorWindow :  EditorWindow
    {
        public virtual void CreateGUI()
        {
            rootVisualElement.Add(new ArtificeDrawer().CreateInspectorGUI(new SerializedObject(this)));
        }
    }
}