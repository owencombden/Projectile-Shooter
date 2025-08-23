using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Empress.UITK.VirtuaStickExtensions {

    /// <summary>
    /// 
    /// </summary>
    public static class UITKExtensions {

        /// <summary>
        /// Applies standard background styles to any VisualElement
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        public static VisualElement ApplyBackgroundStyle(this VisualElement element) {
#if UNITY_2022_2_OR_NEWER
            element.style.backgroundPositionX = new BackgroundPosition(BackgroundPositionKeyword.Center);
            element.style.backgroundPositionY = new BackgroundPosition(BackgroundPositionKeyword.Center);
            element.style.backgroundRepeat = new BackgroundRepeat(Repeat.NoRepeat, Repeat.NoRepeat);
            element.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);
#else
            element.style.unityBackgroundScaleMode = ScaleMode.ScaleToFit;
#endif
            return element;
        }

        /// <summary>
        /// Resets style properties of a VisualElement to their default (initial) values
        /// </summary>
        /// <param name="element">The VisualElement to modify</param>
        /// <returns>The modified VisualElement for method chaining</returns>
        public static VisualElement ResetStyle(this VisualElement element) {
            element.style.width = StyleKeyword.Null;
            element.style.height = StyleKeyword.Null;
            element.style.flexGrow = StyleKeyword.Null;
            element.style.alignItems = StyleKeyword.Null;
            element.style.justifyContent = StyleKeyword.Null;

            element.style.borderTopWidth = StyleKeyword.Null;
            element.style.borderRightWidth = StyleKeyword.Null;
            element.style.borderBottomWidth = StyleKeyword.Null;
            element.style.borderLeftWidth = StyleKeyword.Null;

            element.style.borderTopLeftRadius = StyleKeyword.Null;
            element.style.borderTopRightRadius = StyleKeyword.Null;
            element.style.borderBottomLeftRadius = StyleKeyword.Null;
            element.style.borderBottomRightRadius = StyleKeyword.Null;

            element.style.borderTopColor = StyleKeyword.Null;
            element.style.borderBottomColor = StyleKeyword.Null;
            element.style.borderLeftColor = StyleKeyword.Null;
            element.style.borderRightColor = StyleKeyword.Null;

            element.style.paddingTop = StyleKeyword.Null;
            element.style.paddingBottom = StyleKeyword.Null;
            element.style.paddingLeft = StyleKeyword.Null;
            element.style.paddingRight = StyleKeyword.Null;

            element.style.backgroundColor = StyleKeyword.Null;
            element.style.backgroundImage = StyleKeyword.Null;
            element.style.unityBackgroundImageTintColor = StyleKeyword.Null;

#if UNITY_2022_2_OR_NEWER
            element.style.backgroundPositionX = StyleKeyword.Null;
            element.style.backgroundPositionY = StyleKeyword.Null;
            element.style.backgroundRepeat = StyleKeyword.Null;
            element.style.backgroundSize = StyleKeyword.Null;
#else
            element.style.unityBackgroundScaleMode = StyleKeyword.Null;
#endif
            return element;
        }

        /// <summary>
        /// Adds a transition effect to a specified CSS property of a VisualElement.
        /// </summary>
        /// <param name="element">The VisualElement to which the transition will be applied.</param>
        /// <param name="property">The CSS property to transition (e.g., "opacity", "background-color").</param>
        /// <param name="timeValue">The duration of the transition.</param>
        /// <param name="easing">The easing function to use for the transition (default is EaseInOut).</param>
        public static void AddTransition(this VisualElement element, string property, TimeValue timeValue, EasingMode easing = EasingMode.EaseInOut) {
            element.style.transitionProperty = new StyleList<StylePropertyName>(
                new List<StylePropertyName>(element.resolvedStyle.transitionProperty) { property }
            );
            element.style.transitionDuration = new StyleList<TimeValue>(
                new List<TimeValue>(element.resolvedStyle.transitionDuration) { timeValue }
            );
            element.style.transitionTimingFunction = new StyleList<EasingFunction>(
                new List<EasingFunction>(element.resolvedStyle.transitionTimingFunction) { easing }
            );
        }

        /// <summary>
        /// Applies a fade-in animation to a VisualElement, transitioning from a minimum opacity to fully visible.
        /// </summary>
        /// <param name="element">The VisualElement to animate.</param>
        /// <param name="minFade">The starting opacity value (0 = fully transparent, 1 = fully opaque).</param>
        /// <param name="duration">The duration of the animation in seconds.</param>
        public static void FadeIn(this VisualElement element, float minFade = 0f, float duration = 1f) {
            element.style.opacity = minFade;
            element.style.transitionDuration = new List<TimeValue> { new(duration, TimeUnit.Second) };
            element.style.opacity = 1;
        }

        /// <summary>
        /// Applies a fade-out animation to a VisualElement, transitioning from fully visible to a minimum opacity.
        /// </summary>
        /// <param name="element">The VisualElement to animate.</param>
        /// <param name="minFade">The ending opacity value (0 = fully transparent, 1 = fully opaque).</param>
        /// <param name="duration">The duration of the animation in seconds.</param>
        public static void FadeOut(this VisualElement element, float minFade = 0f, float duration = 1f) {
            element.style.opacity = 1;
            element.style.transitionDuration = new List<TimeValue> { new(duration, TimeUnit.Second) };
            element.style.opacity = minFade;
        }

        /// <summary>
        /// Sets a uniform margin for all sides of a VisualElement.
        /// </summary>
        /// <param name="element">The VisualElement to apply the margin to.</param>
        /// <param name="margin">The margin value for all sides (top, right, bottom, left).</param>
        public static void SetMargin(this VisualElement element, float margin) {
            SetMargin(element, margin, margin, margin, margin);
        }

        /// <summary>
        /// Sets individual margin values for each side of a VisualElement.
        /// </summary>
        /// <param name="element">The VisualElement to apply the margin to.</param>
        /// <param name="top">The margin value for the top side.</param>
        /// <param name="bottom">The margin value for the bottom side.</param>
        /// <param name="left">The margin value for the left side.</param>
        /// <param name="right">The margin value for the right side.</param>
        public static void SetMargin(this VisualElement element, float top, float bottom, float left, float right) {
            element.style.marginTop = top;
            element.style.marginBottom = bottom;
            element.style.marginLeft = left;
            element.style.marginRight = right;
        }

        /// <summary>
        /// Sets a uniform padding for all sides of a VisualElement.
        /// </summary>
        /// <param name="element">The VisualElement to apply the padding to.</param>
        /// <param name="padding">The padding value for all sides (top, right, bottom, left).</param>
        public static void SetPadding(this VisualElement element, float padding) {
            SetPadding(element, padding, padding, padding, padding);
        }

        /// <summary>
        /// Sets individual padding values for each side of a VisualElement.
        /// </summary>
        /// <param name="element">The VisualElement to apply the padding to.</param>
        /// <param name="top">The padding value for the top side.</param>
        /// <param name="bottom">The padding value for the bottom side.</param>
        /// <param name="left">The padding value for the left side.</param>
        /// <param name="right">The padding value for the right side.</param>
        public static void SetPadding(this VisualElement element, float top, float bottom, float left, float right) {
            element.style.paddingTop = top;
            element.style.paddingBottom = bottom;
            element.style.paddingLeft = left;
            element.style.paddingRight = right;
        }

        /// <summary>
        /// Sets a uniform border width for all sides of a VisualElement.
        /// </summary>
        /// <param name="element">The VisualElement to apply the border width to.</param>
        /// <param name="border">The border width value for all sides (top, right, bottom, left).</param>
        public static void SetBorder(this VisualElement element, float border) {
            SetBorder(element, border, border, border, border);
        }

        /// <summary>
        /// Sets individual border width values for each side of a VisualElement.
        /// </summary>
        /// <param name="element">The VisualElement to apply the border width to.</param>
        /// <param name="top">The border width value for the top side.</param>
        /// <param name="bottom">The border width value for the bottom side.</param>
        /// <param name="left">The border width value for the left side.</param>
        /// <param name="right">The border width value for the right side.</param>
        public static void SetBorder(this VisualElement element, float top, float bottom, float left, float right) {
            element.style.borderTopWidth = top;
            element.style.borderRightWidth = right;
            element.style.borderBottomWidth = bottom;
            element.style.borderLeftWidth = left;
        }

        /// <summary>
        /// Sets a uniform border color for all sides of a VisualElement.
        /// </summary>
        /// <param name="element">The VisualElement to apply the border color to.</param>
        /// <param name="borderColor">The border color for all sides (top, right, bottom, left).</param>
        public static void SetBorderColor(this VisualElement element, Color borderColor) {
            SetBorderColor(element, borderColor, borderColor, borderColor, borderColor);
        }

        /// <summary>
        /// Sets individual border colors for each side of a VisualElement.
        /// </summary>
        /// <param name="element">The VisualElement to apply the border colors to.</param>
        /// <param name="top">The border color for the top side.</param>
        /// <param name="bottom">The border color for the bottom side.</param>
        /// <param name="left">The border color for the left side.</param>
        /// <param name="right">The border color for the right side.</param>
        public static void SetBorderColor(this VisualElement element, Color top, Color bottom, Color left, Color right) {
            element.style.borderTopColor = top;
            element.style.borderRightColor = right;
            element.style.borderBottomColor = bottom;
            element.style.borderLeftColor = left;
        }

        public static void SetBorderRadius(this VisualElement element, float radius) {
            SetBorderRadius(element, radius, radius, radius, radius);
        }

        public static void SetBorderRadius(this VisualElement element, float topLeft, float topRight, float bottomLeft, float bottomRight) {
            element.style.borderTopLeftRadius = topLeft;
            element.style.borderTopRightRadius = topRight;
            element.style.borderBottomLeftRadius = bottomLeft;
            element.style.borderBottomRightRadius = bottomRight;
        }
    }
}
