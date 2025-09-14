using ArtificeToolkit.Editor;
using ArtificeToolkit.Editor.Artifice_CustomAttributeDrawers;
using UnityEditor;
using UnityEngine.UIElements;

namespace KrasCore.Editor
{
    [Artifice_CustomAttributeDrawer(typeof(InlineNoFoldoutAttribute))]
    public class Artifice_CustomAttributeDrawer_InlineNoFoldoutAttribute : Artifice_CustomAttributeDrawer
    {
        public override bool IsReplacingPropertyField => true;

        public override VisualElement OnPropertyGUI(SerializedProperty property)
        {
            var root = new VisualElement();

            if (((InlineNoFoldoutAttribute)Attribute).DrawPropertyName)
            {
                var lbl = new Label
                {
                    text = property.displayName,
                };
                root.Add(lbl);
            }

            var artificeDrawer = new ArtificeDrawer();
            
            foreach (var prop in property.GetVisibleChildren())
            {
                root.Add(artificeDrawer.CreatePropertyGUI(prop, forceArtificeStyle: true));
            }

            return root;
        }
    }
}