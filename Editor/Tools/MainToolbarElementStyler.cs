using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace KrasCore.Editor
{
    public static class MainToolbarElementStyler {
        public static void StyleElement<T>(string elementName, Action<T> styleAction) where T : VisualElement {
            EditorApplication.delayCall += () => {
                ApplyStyle(elementName, (element) => {
                    T targetElement;

                    if (element is T typedElement) {
                        targetElement = typedElement;
                    } else {
                        targetElement = element.Query<T>().First();
                    }

                    if (targetElement != null) {
                        styleAction(targetElement);
                    }
                });
            };
        }

        private static void ApplyStyle(string elementName, Action<VisualElement> styleCallback) {
            var element = FindElementByName(elementName);
            if (element != null) {
                styleCallback(element);
            }
        }
        
        private static Type GetMainToolbarWindowType()
        {
            var editorAsm = typeof(EditorWindow).Assembly;
            var t = editorAsm.GetType("UnityEditor.MainToolbarWindow", throwOnError: true, ignoreCase: false);
            return t;
        }
        
        private static VisualElement FindElementByName(string name) {
            var window = (EditorWindow)Resources.FindObjectsOfTypeAll(GetMainToolbarWindowType()).FirstOrDefault();
            if (window == null) throw new Exception("Unable to find MainToolbarWindow");
            var root = window.rootVisualElement;
            
            VisualElement element;
            return (element = root.FindElementByName(name)) != null 
                ? element 
                : null;
        }
        
        private static VisualElement FindElement(this VisualElement element, Func<VisualElement, bool> predicate) {
            if (predicate(element)) {
                return element;
            }
            return element.Query<VisualElement>().Where(predicate).First();
        }
        
        private static VisualElement FindElementByName(this VisualElement element, string name) {
            return element.FindElement(e => e.name == name);
        }
    }
}