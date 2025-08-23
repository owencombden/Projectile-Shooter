using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Empress.UITK.VirtuaStickExtensions;

namespace Empress.UITK.Demo {

#if UNITY_2023_2 || UNITY_6000_0_OR_NEWER
    [UxmlElement(nameof(VirtuaStickDpad))]
#endif
    public partial class VirtuaStickDpad : VirtuaStick {

        public enum Dpad {
            Up,
            Down,
            Left,
            Right
        }

#if UNITY_2023_2 || UNITY_6000_0_OR_NEWER
        [UxmlAttribute]
#endif
        public Color DpadColor {
            get => m_DpadColor;
            set {
                m_DpadColor = value;
                for (var i = 0; i < 4; i++)
                    m_Dpad[i].style.unityBackgroundImageTintColor = value;
            }
        }
        protected Color m_DpadColor = Color.white;

#if UNITY_2023_2 || UNITY_6000_0_OR_NEWER
        [UxmlAttribute]
#endif
        public float FadeDuration { get; set; } = 0.5f;

#if UNITY_2023_2 || UNITY_6000_0_OR_NEWER
        [UxmlAttribute]
#endif
        public float MinFade {
            get => m_MinFade;
            set {
                m_MinFade = value;
                for (var i = 0; i < 4; i++)
                    m_Dpad[i].style.opacity = m_MinFade;
            }
        }
        protected float m_MinFade = 0.25f;

        #region UXML TRAITS

#if !(UNITY_2023_2 || UNITY_6000_0_OR_NEWER)
        public new class UxmlFactory : UxmlFactory<VirtuaStickDpad, UxmlTraits> { }

        public new class UxmlTraits : VirtuaStick.UxmlTraits { // Inherit from base class traits

            readonly UxmlColorAttributeDescription m_DpadColor = new() {
                name = "dpad-color",
                defaultValue = Color.white
            };

            readonly UxmlFloatAttributeDescription m_FadeDuration = new() {
                name = "fade-duration",
                defaultValue = 0.5f
            };

            readonly UxmlFloatAttributeDescription m_MinFade = new() {
                name = "min-fade",
                defaultValue = 0.25f
            };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc) {
                base.Init(ve, bag, cc);
                var stick = (VirtuaStickDpad)ve;
                stick.DpadColor = m_DpadColor.GetValueFromBag(bag, cc);
                stick.FadeDuration = m_FadeDuration.GetValueFromBag(bag, cc);
                stick.MinFade = m_MinFade.GetValueFromBag(bag, cc);
            }
        }
#endif
        #endregion

        protected readonly VisualElement m_DpadContainer;
        protected readonly VisualElement m_DpadHContainer;
        protected readonly VisualElement[] m_Dpad = new VisualElement[4];
        protected readonly List<Dpad> m_HighlightedArrows = new();

        public VirtuaStickDpad() : base() {
            StickPixels = 300f;
            HandleSizePercent = 40f;

            m_DpadContainer = new VisualElement {
                name = "dpad-container",
                style = {
                    position = Position.Absolute,
                    alignItems = Align.Center,
                    justifyContent = Justify.SpaceAround,
                    top = Length.Percent(0),
                    bottom = Length.Percent(0),
                    left = Length.Percent(0),
                    right = Length.Percent(0)
                },
            };

            m_DpadHContainer = new VisualElement {
                name = "dpad-horizontal-container",
                style = {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    justifyContent = Justify.SpaceAround,
                    width = Length.Percent(150)
                },
            };

            for (var i = 0; i < 4; i++) {
                m_Dpad[i] = new VisualElement {
                    name = $"dpad-{((Dpad)i).ToString().ToLower()}",
                    style = {
                        backgroundImage = m_ArrowTexture,
                        unityBackgroundImageTintColor = DpadColor,
                        width = 40,
                        height = 40,
#if UNITY_2022_2_OR_NEWER
                        backgroundPositionX = new BackgroundPosition(BackgroundPositionKeyword.Center),
                        backgroundPositionY = new BackgroundPosition(BackgroundPositionKeyword.Center),
                        backgroundRepeat = new BackgroundRepeat(Repeat.NoRepeat, Repeat.NoRepeat),
                        backgroundSize = new BackgroundSize(BackgroundSizeType.Contain),
#else
                        unityBackgroundScaleMode = ScaleMode.ScaleToFit,
#endif
                        rotate = new Rotate(GetArrowAngle(i))
                    }
                };

                m_Dpad[i].AddTransition("scale", new TimeValue(FadeDuration, TimeUnit.Second), EasingMode.EaseOut);
            }

            m_DpadContainer.Add(m_Dpad[(int)Dpad.Up]);
            m_DpadContainer.Add(m_DpadHContainer);
            m_DpadHContainer.Add(m_Dpad[(int)Dpad.Left]);
            m_DpadHContainer.Add(m_Dpad[(int)Dpad.Right]);
            m_DpadContainer.Add(m_Dpad[(int)Dpad.Down]);

            m_Stick.Add(m_DpadContainer);
        }

        protected override void OnGeometryChanged(GeometryChangedEvent e) { 
            base.OnGeometryChanged(e);
            m_Stick.SetBorder(5f);
        }

        protected float GetArrowAngle(int index) {
            return index switch {
                1 => 180f,
                2 => 270f,
                3 => 90f,
                _ => 0f,
            };
        }

        protected override void OnPointerUp(PointerUpEvent e) {
            base.OnPointerUp(e);
            UpdateDpad();
        }

        protected override void OnPointerMove(PointerMoveEvent e) {
            base.OnPointerMove(e);
            UpdateDpad();
        }

        protected virtual void UpdateDpad() {
            // Calculate input angle from direction vector (-180° to 180°)
            // Atan2 returns angle between positive x-axis and the vector
            var angle = Mathf.Atan2(m_Direction.y, m_Direction.x) * Mathf.Rad2Deg;

            if (!IsDragging) {
                // Reset all arrows when no meaningful input
                for (var i = 0; i < 4; i++) {
                    var dpad = m_Dpad[i];

                    if (dpad.transform.scale.x <= 1f)
                        continue;

                    dpad.FadeOut(MinFade, FadeDuration);
                    dpad.transform.scale = Vector2.one;
                }

                return;
            }

            m_HighlightedArrows.Clear();

            // Normalize angle to [-180, 180]
            angle = Mathf.Repeat(angle + 180f, 360f) - 180f;

            // Define sector centers
            const float rightCenter = 0f;
            const float downCenter = 90f;
            const float leftCenter = 180f;
            const float upCenter = -90f;

            // Define tolerance (half of sector width)
            const float tolerance = 60f;

            // Check each direction with expanded range
            if (Mathf.Abs(angle - rightCenter) <= tolerance || Mathf.Abs(angle + 360f - rightCenter) <= tolerance)
                m_HighlightedArrows.Add(Dpad.Right);

            if (Mathf.Abs(angle - downCenter) <= tolerance)
                m_HighlightedArrows.Add(Dpad.Down);

            if (Mathf.Abs(angle - leftCenter) <= tolerance || Mathf.Abs(angle + 360f - leftCenter) <= tolerance)
                m_HighlightedArrows.Add(Dpad.Left);

            if (Mathf.Abs(angle - upCenter) <= tolerance)
                m_HighlightedArrows.Add(Dpad.Up);

            // Apply highlights to all arrows
            for (int i = 0; i < 4; i++) {
                var dpad = m_Dpad[i];
                var isActive = m_HighlightedArrows.Contains((Dpad)i);

                var currentScale = dpad.transform.scale.x;

                if (isActive && currentScale <= 1f) {
                    dpad.FadeIn(MinFade, FadeDuration);
                    dpad.transform.scale = Vector2.one * 1.5f;
                }
                else if (!isActive && currentScale > 1f) {
                    dpad.FadeOut(MinFade, FadeDuration);
                    dpad.transform.scale = Vector2.one;
                }
            }
        }
    }
}
