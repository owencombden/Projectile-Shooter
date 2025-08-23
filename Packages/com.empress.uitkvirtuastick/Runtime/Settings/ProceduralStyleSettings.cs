using System;
using UnityEngine;
using UnityEngine.UIElements;
using Empress.UITK.VirtuaStickExtensions;

namespace Empress.UITK {

    /// <summary>
    /// Container for procedural style settings used to customize VirtuaStick visual elements
    /// </summary>
    [Serializable]
    public struct ProceduralStyleSettings {

        /// <summary>Base color of the element</summary>
        public Color color;

        /// <summary>Background texture (optional)</summary>
        public Texture2D background;

        /// <summary>Tint color applied to background texture</summary>
        public Color backgroundTint;

        /// <summary>Border widths for each edge</summary>
        public RectEdge border;

        /// <summary>Border colors for each edge</summary>
        public RectColor borderColor;

        /// <summary>Corner radius values for each edge</summary>
        public RectEdge radius;

        /// <summary>Margin values for each edge</summary>
        public RectEdge margin;

        [NonSerialized] public VirtuaStick virtuaStick;
        [NonSerialized] public VisualElement element;

        /// <summary>
        /// Creates a new ProceduralStyleSettings instance
        /// </summary>
        /// <param name="virtuaStick">Parent VirtuaStick instance</param>
        /// <param name="element">Target VisualElement to style</param>
        public ProceduralStyleSettings(VirtuaStick virtuaStick, VisualElement element) {
            this.virtuaStick = virtuaStick;
            this.element = element;
            color = Color.clear;
            background = null;
            backgroundTint = Color.clear;
            border = new RectEdge();
            borderColor = new RectColor();
            radius = new RectEdge();
            margin = new RectEdge();
        }

        /// <summary>
        /// Applies all style settings to the target VisualElement
        /// </summary>
        public readonly void Update() {
            if (!CanEditStyles()) return;
            element.style.backgroundColor = color;
            element.style.backgroundImage = background ? new StyleBackground(background) : StyleKeyword.Null;
            element.style.unityBackgroundImageTintColor = backgroundTint;
            element.SetBorder(border.top, border.bottom, border.left, border.right);
            element.SetBorderColor(borderColor.top, borderColor.bottom, borderColor.left, borderColor.right);
            element.SetBorderRadius(radius.top, radius.bottom, radius.left, radius.right);
            element.SetMargin(margin.top, margin.bottom, margin.left, margin.right);
        }

        /// <summary>
        /// Updates the target element and applies all style settings
        /// </summary>
        /// <param name="virtuaStick">New VirtuaStick reference</param>
        /// <param name="element">New VisualElement target</param>
        public void Update(VirtuaStick virtuaStick, VisualElement element) {
            this.virtuaStick = virtuaStick;
            this.element = element;
            Update();
        }

        /// <summary>
        /// Checks if styles can be applied to the target element
        /// </summary>
        /// <returns>True if styles can be applied</returns>
        private readonly bool CanEditStyles() =>
            virtuaStick != null &&
            virtuaStick.StyleSource == VisualStyleSource.Procedural;
    }
}
