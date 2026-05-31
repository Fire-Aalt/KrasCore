using UnityEngine.UIElements;

namespace FireAlt.Core.Extensions
{
    public static class VisualElementExtensions
    {
        public static T Name<T>(this T element, string name) where T : VisualElement
        {
            element.name = name;
            return element;
        }
        
        public static T Enabled<T>(this T element, bool enabled) where T : VisualElement
        {
            element.SetEnabled(enabled);
            return element;
        }
        
        public static T Children<T>(this T element, params VisualElement[] children) where T : VisualElement
        {
            foreach (var child in children)
            {
                element.Add(child);
            }
            return element;
        }
        
        public static T Classes<T>(this T element, params string[] classes) where T : VisualElement
        {
            foreach (var @class in classes)
            {
                element.AddToClassList(@class);
            }
            return element;
        }
    }
}