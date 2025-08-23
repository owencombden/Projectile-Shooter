using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Empress.UITK.VirtuaStickExtensions;

namespace Empress.UITK.Demo {

#if UNITY_2023_2 || UNITY_6000_0_OR_NEWER
    [UxmlElement(nameof(VirtuaStick2DPlatformer))]
#endif
    public partial class VirtuaStick2DPlatformer : VirtuaStick {

        /// <summary>
        /// 
        /// </summary>
#if UNITY_2023_2 || UNITY_6000_0_OR_NEWER
        [UxmlAttribute]
#endif
        public float ArrowsSpacing {
            get => m_ArrowsSpacing;
            set {
                m_ArrowsSpacing = value;
                m_Arrows[0].style.marginRight = value;
                m_Arrows[1].style.marginLeft = value;
            }
        }
        private float m_ArrowsSpacing;

        #region UXML TRAITS

#if !(UNITY_2023_2 || UNITY_6000_0_OR_NEWER)
        public new class UxmlFactory : UxmlFactory<VirtuaStick2DPlatformer, UxmlTraits> { }

        public new class UxmlTraits : VisualElement.UxmlTraits {

            readonly UxmlFloatAttributeDescription m_ArrowsSpacing = new() {
                name = "arrows-spacing",
                defaultValue = 0f
            };

            readonly UxmlFloatAttributeDescription m_OffsetX = new() {
                name = "offset-x",
                defaultValue = 0f
            };

            readonly UxmlFloatAttributeDescription m_OffsetY = new() {
                name = "offset-y",
                defaultValue = 0f
            };

            readonly UxmlFloatAttributeDescription m_HandleMoveRadius = new() {
                name = "handle-move-radius",
                defaultValue = 55f
            };

            readonly UxmlBoolAttributeDescription m_DebugInfo = new() {
                name = "debug-info",
                defaultValue = false
            };

            readonly UxmlEnumAttributeDescription<CornerAlign> m_DebugAlign = new() {
                name = "debug-align",
                defaultValue = CornerAlign.TopLeft
            };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc) {
                base.Init(ve, bag, cc);
                var stick = (VirtuaStick2DPlatformer)ve;

                stick.Dynamic = false;
                stick.AxisConstraint = InputAxisConstraint.Horizontal;
                stick.DirectionalArrow = false;

                stick.ArrowsSpacing = m_ArrowsSpacing.GetValueFromBag(bag, cc);
                stick.OffsetX = m_OffsetX.GetValueFromBag(bag, cc);
                stick.OffsetY = m_OffsetY.GetValueFromBag(bag, cc);
                stick.HandleMoveRadius = m_HandleMoveRadius.GetValueFromBag(bag, cc);
                stick.DebugInfo = m_DebugInfo.GetValueFromBag(bag, cc);
                stick.DebugAlign = m_DebugAlign.GetValueFromBag(bag, cc);
            }
        }
#endif
        #endregion

        protected readonly VisualElement m_ArrowHContent;
        protected readonly VisualElement[] m_Arrows = new VisualElement[2];

        protected static readonly Texture2D[] m_ArrowTex = new Texture2D[2];

        public VirtuaStick2DPlatformer() : base() {
            if (m_ArrowTex.Any(t => !t)) {
                m_ArrowTex[0] = Resources.Load<Texture2D>("UITK-VS-left-arrow-long");
                m_ArrowTex[1] = Resources.Load<Texture2D>("UITK-VS-right-arrow-long");
            }

            StickPixels = 300f;
            m_Stick.SetBorder(2.5f);
            m_Stick.SetBorderRadius(300f);
            m_Stick.SetBorderColor(Color.gray);

#if UNITY_2023_2 || UNITY_6000_0_OR_NEWER
            Dynamic = false;
            AxisConstraint = InputAxisConstraint.Horizontal;
            DirectionalArrow = false;
#endif
            m_Handle.style.backgroundColor = Color.clear;
            m_StickContent.RemoveFromHierarchy();
            
            m_ArrowHContent = new VisualElement {
                name = "arrow-content",
                style = {
                    flexGrow = 1,
                    flexDirection = FlexDirection.Row,
                    justifyContent = Justify.Center
                }
            };

            for (var i = 0; i < 2; i++) {
                m_Arrows[i] = new VisualElement {
                    name = i == 0 ? "arrow-left" : "arrow-right",
                    style = {
                        backgroundImage = m_ArrowTex[i],
                        alignSelf = Align.Center,
                        opacity = 0.5f,
                        width = Length.Percent(100),
                        height = Length.Percent(50),
#if UNITY_2022_2_OR_NEWER
                        backgroundPositionX = new BackgroundPosition(BackgroundPositionKeyword.Center),
                        backgroundPositionY = new BackgroundPosition(BackgroundPositionKeyword.Center),
                        backgroundRepeat = new BackgroundRepeat(Repeat.NoRepeat, Repeat.NoRepeat),
                        backgroundSize = new BackgroundSize(BackgroundSizeType.Contain),
#else
                        unityBackgroundScaleMode = ScaleMode.ScaleToFit,
#endif
                    }
                };

                m_Arrows[i].AddToClassList(i == 0 ? "vs-arrow-left" : "vs-arrow-right");
                m_Arrows[i].AddTransition("scale", new TimeValue(.25f, TimeUnit.Second), EasingMode.EaseOut);
                m_Arrows[i].AddTransition("opacity", new TimeValue(.35f, TimeUnit.Second), EasingMode.EaseOut);
                m_ArrowHContent.Add(m_Arrows[i]);
            }

            m_Stick.Add(m_ArrowHContent);
        }

        protected override void OnGeometryChanged(GeometryChangedEvent e) { }

        protected override Vector2 GetDirection(Vector2 directionBase) {
            directionBase.y = 0f;
            return directionBase;
        }

        protected override void OnPointerUp(PointerUpEvent e) {
            base.OnPointerUp(e);
            UpdateDpad();
        }

        protected override void OnPointerMove(PointerMoveEvent e) {
            base.OnPointerMove(e);
            UpdateDpad();
        }

        void UpdateDpad() {
            // Calculate input angle from direction vector (-180° to 180°)
            // Atan2 returns angle between positive x-axis and the vector
            var angle = Mathf.Atan2(m_Direction.y, m_Direction.x) * Mathf.Rad2Deg;
            var direction = -1;

            // Early return if input magnitude is too small (deadzone)
            if (m_Direction.magnitude < 0.1f) {
                // Reset all arrows when no meaningful input
                for (var i = 0; i < 2; i++) {
                    var dpad = m_Arrows[i];

                    if (dpad.transform.scale.x <= 1f)
                        continue;

                    dpad.style.opacity = 0.5f;
                    dpad.transform.scale = Vector2.one;
                }

                return;
            }

            // Determine which arrow to highlight based on angle sectors
            if (angle >= -45f && angle < 45f)
                direction = 1; // Right
            else if (angle >= 135f || angle < -135f)
                direction = 0; // Left

            // Apply highlights
            for (var i = 0; i < 2; i++) {
                var isActive = direction == i;
                var dpad = m_Arrows[i];

                if (isActive && dpad.transform.scale.x <= 1f) {
                    dpad.style.opacity = 1f;
                    dpad.transform.scale = Vector2.one * 1.3f;
                }
                else if (!isActive && dpad.transform.scale.x > 1f) {
                    dpad.style.opacity = 0.5f;
                    dpad.transform.scale = Vector2.one;
                }
            }
        }
    }
}
