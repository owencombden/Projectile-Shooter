using UnityEngine;
using UnityEngine.UIElements;
using Empress.UITK.VirtuaStickExtensions;

namespace Empress.UITK.Demo {

#if UNITY_2023_2 || UNITY_6000_0_OR_NEWER
    [UxmlElement(nameof(VirtuaSticPuzzleDpad))]
#endif
    public partial class VirtuaSticPuzzleDpad : VirtuaStickDpad {

        #region UXML TRAITS

#if !(UNITY_2023_2 || UNITY_6000_0_OR_NEWER)
        public new class UxmlFactory : UxmlFactory<VirtuaSticPuzzleDpad, UxmlTraits> { }
#endif
        #endregion

        public VirtuaSticPuzzleDpad() : base() {
            var center = new VisualElement {
                name = "stick-center",
                style = {
                    backgroundColor = new Color(.25f, .25f, .25f, .5f),
                    position = Position.Absolute,
                    top = Length.Percent(0),
                    bottom = Length.Percent(0),
                    left = Length.Percent(0),
                    right = Length.Percent(0)
                },
            };

            center.SetBorderRadius(300f);
            center.SetMargin(100f);
            m_Stick.Add(center);

            for (var i = 0; i < 4; i++)
                m_Dpad[i].AddTransition("opacity", new TimeValue(.25f, TimeUnit.Second), EasingMode.EaseOut);

            m_Stick.AddTransition("opacity", new TimeValue(.08f, TimeUnit.Second), EasingMode.EaseOut);
        }

        protected override void OnGeometryChanged(GeometryChangedEvent e) {
            base.OnGeometryChanged(e);
            m_Stick.SetBorder(5f);
            m_Stick.SetBorderRadius(40f);
            m_Stick.SetBorderColor(new Color(.45f, .25f, .75f, .75f));
            m_Handle.style.opacity = 0;

            for (var i = 0; i < 4; i++) {
                m_Dpad[i].style.width = 55f;
                m_Dpad[i].style.height = 55f;
            }
        }

        protected override void OnPointerDown(PointerDownEvent e) {
            base.OnPointerDown(e);
            m_Stick.style.opacity = 1f;
        }

        protected override void OnPointerUp(PointerUpEvent e) {
            base.OnPointerUp(e);
            m_Stick.style.opacity = .85f;
        }

        protected override Vector2 GetDirection(Vector2 directionBase) {
            if (Mathf.Abs(directionBase.x) > Mathf.Abs(directionBase.y))
                return new Vector2(directionBase.x, 0f); // Horizontal
            else
                return new Vector2(0f, directionBase.y); // Vertical
        }

        protected override void UpdateDpad() {
            // Calculate input angle from direction vector (-180° to 180°)
            // Atan2 returns angle between positive x-axis and the vector
            var angle = Mathf.Atan2(m_Direction.y, m_Direction.x) * Mathf.Rad2Deg;
            Dpad highlightedArrow;

            if (!IsDragging) {
                // Reset all arrows when no meaningful input
                for (var i = 0; i < 4; i++) {
                    var dpad = m_Dpad[i];

                    if (dpad.transform.scale.x <= 1f)
                        continue;

                    dpad.style.opacity = 0.5f;
                    dpad.transform.scale = Vector2.one;
                }
                return;
            }

            // Determine which arrow to highlight based on angle sectors
            if (angle >= -45f && angle < 45f)
                highlightedArrow = Dpad.Right;         // Right sector (-45° to 45°)
            else if (angle >= 45f && angle < 135f)
                highlightedArrow = Dpad.Down;          // Down sector (45° to 135°)
            else if (angle >= 135f || angle < -135f)
                highlightedArrow = Dpad.Left;          // Left sector (135° to -135°)
            else
                highlightedArrow = Dpad.Up;            // Up sector (-45° to -135°)

            // Apply highlights to all arrows
            for (var i = 0; i < 4; i++) {
                // Compare enum values by casting to int
                var isActive = (int)highlightedArrow == i;
                var dpad = m_Dpad[i];

                if (isActive) {
                    dpad.style.opacity = 1f;
                    dpad.transform.scale = Vector2.one * 1.5f;
                }
                else {
                    dpad.style.opacity = MinFade;
                    dpad.transform.scale = Vector2.one;
                }
            }
        }
    }
}
