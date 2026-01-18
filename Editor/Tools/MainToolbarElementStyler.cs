using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace KrasCore.Editor
{
    public static class MainToolbarElementStyler {
        public static void StyleElement<T>(string elementName, System.Action<T> styleAction) where T : VisualElement {
            EditorApplication.delayCall += () => {
                ApplyStyle(elementName, (element) => {
                    T targetElement = null;

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

        private static void ApplyStyle(string elementName, System.Action<VisualElement> styleCallback) {
            var element = FindElementByName(elementName);
            if (element != null) {
                styleCallback(element);
            }
        }

        private static VisualElement FindElementByName(string name) {
            var windows = Resources.FindObjectsOfTypeAll<EditorWindow>();
            foreach (var window in windows) {
                var root = window.rootVisualElement;
                if (root == null) continue;
                
            
                VisualElement element;
                if ((element = root.FindElementByName(name)) != null) return element;
            }
            return null;
        }
        
        static VisualElement FindElement(this VisualElement element, System.Func<VisualElement, bool> predicate) {
            if (predicate(element)) {
                return element;
            }
            return element.Query<VisualElement>().Where(predicate).First();
        }
        
        public static VisualElement FindElementByName(this VisualElement element, string name) {
            return element.FindElement(e => e.name == name);
        }
    }
}