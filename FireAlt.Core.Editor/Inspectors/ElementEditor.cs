using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace FireAlt.Core.Editor.Inspectors
{
    /// <summary> Provides a custom editor ([CustomEditor(typeof(T))]) with custom element but will fall back to PropertyField if not overriden. </summary>
    public abstract class ElementEditor : UnityEditor.Editor
    {
        private VisualElement _parent;

        protected VisualElement Parent => _parent!;

        protected virtual bool IncludeScript => true;

        protected bool MultiEditing => targets.Length > 1;

        /// <inheritdoc/>
        public sealed override VisualElement CreateInspectorGUI()
        {
            _parent = new VisualElement();

            if (IncludeScript)
            {
                var scriptProperty = serializedObject.FindProperty("m_Script");
                var scriptElement = CreatePropertyField(scriptProperty, serializedObject);
                scriptElement.SetEnabled(false);
                Parent.Add(scriptElement);
            }

            var createElements = PreElementCreation(_parent);
            if (createElements)
            {
                foreach (var property in SerializedHelper.IterateAllChildren(serializedObject, false))
                {
                    var element = CreateElement(property);
                    if (element != null)
                    {
                        Parent.Add(element);
                    }
                }
            }

            PostElementCreation(Parent, createElements);

            return Parent;
        }

        protected static PropertyField CreatePropertyField(SerializedProperty property, SerializedObject serializedObject)
        {
            return PropertyUtils.CreateProperty(property, serializedObject);
        }

        protected static PropertyField CreatePropertyField(SerializedProperty property)
        {
            return CreatePropertyField(property, property.serializedObject);
        }

        protected virtual VisualElement CreateElement(SerializedProperty property)
        {
            return CreatePropertyField(property, serializedObject);
        }

        protected virtual bool PreElementCreation(VisualElement root)
        {
            return true;
        }

        protected virtual void PostElementCreation(VisualElement root, bool createdElements)
        {
        }

        /// <summary> Create a foldout without margins so it lines up with the inspector listviews. </summary>
        /// <param name="text"> Text value of the foldout. </param>
        /// <param name="value"> Default value of the foldout. </param>
        /// <returns> A new foldout. </returns>
        protected static Foldout CreateFoldout(string text, bool value = false)
        {
            var foldout = new Foldout { text = text };
            foldout.AddToClassList("unity-list-view__foldout-header");
            foldout.contentContainer.style.marginLeft = 0;
            foldout.Q<Toggle>().style.marginLeft = -12;
            foldout.value = value;
            return foldout;
        }
    }
}