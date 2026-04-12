namespace KrasCore.Editor.UI
{
    using System;
    using System.Collections.Generic;
    using BovineLabs.Core.Editor.SearchWindow;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.UIElements;

    public class SearchElement : BaseField<int>
    {
        private readonly List<SearchView.Item> items;
        private readonly Button componentButton;

        public SearchElement(List<SearchView.Item> items, string defaultText, string displayName = "")
            : this(items, defaultText, displayName, new VisualElement())
        {
        }

        private SearchElement(List<SearchView.Item> items, string defaultText, string displayName, VisualElement element)
            : base(displayName, element)
        {
            this.AddToClassList(BaseField<string>.alignedFieldUssClassName);
            this.AddToClassList(TextInputBaseField<string>.ussClassName);

            element.AddToClassList("unity-base-field__input");
            element.AddToClassList("unity-base-popup-field__input");
            element.AddToClassList("unity-popup-field__input");
            element.AddToClassList("unity-property-field__input");

            this.componentButton = new Button();
            element.Add(this.componentButton);
            this.componentButton.AddToClassList("unity-base-popup-field__text");
            this.componentButton.RemoveFromClassList("unity-button");

            var image = new VisualElement();
            element.Add(image);
            image.AddToClassList("unity-base-popup-field__arrow");

            this.items = items ?? new List<SearchView.Item>();
            this.labelElement.style.minWidth = 60;

            this.componentButton.clicked += () =>
            {
                if (!TryGetPopupRect(element, out var popupRect))
                {
                    return;
                }

                var searchWindow = SearchWindow.Create();
                searchWindow.Title = displayName;
                searchWindow.Items = this.items;
                searchWindow.OnSelection += item =>
                {
                    this.OnSelection?.Invoke(item);
                    this.componentButton.text = this.SetText(item);
                };

                searchWindow.position = popupRect;
                searchWindow.ShowPopup();
            };

            this.componentButton.text = defaultText;
        }

        public event Action<SearchView.Item> OnSelection;

        public Func<SearchView.Item, string> SetText { get; set; } = item => item.Name;

        public float Height { get; set; } = 315;

        public string Text
        {
            get => this.componentButton.text;
            set => this.componentButton.text = value;
        }

        public void SetValue(int index)
        {
            var item = this.items[index];

            this.OnSelection?.Invoke(item);
            this.componentButton.text = this.SetText(item);
        }

        private bool TryGetPopupRect(VisualElement element, out Rect popupRect)
        {
            var hostWindow = EditorWindow.focusedWindow ?? EditorWindow.mouseOverWindow;
            if (hostWindow == null)
            {
                var editorWindows = Resources.FindObjectsOfTypeAll<EditorWindow>();
                if (editorWindows != null && editorWindows.Length > 0)
                {
                    hostWindow = editorWindows[0];
                }
            }

            if (hostWindow == null)
            {
                popupRect = default;
                return false;
            }

            var hostWindowRect = hostWindow.position;
            Rect worldBounds;
            if (this.labelElement.parent == null)
            {
                worldBounds = element.worldBound;
            }
            else
            {
                worldBounds = this.labelElement.worldBound;
                worldBounds.width += element.worldBound.width;
            }

            popupRect = new Rect(
                hostWindowRect.x + worldBounds.x,
                hostWindowRect.y + worldBounds.y + worldBounds.height,
                worldBounds.width,
                this.Height);

            return true;
        }
    }
}
