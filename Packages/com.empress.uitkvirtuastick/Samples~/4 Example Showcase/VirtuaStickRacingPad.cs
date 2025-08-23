using UnityEngine;
using UnityEngine.UIElements;
using Empress.UITK.VirtuaStickExtensions;

namespace Empress.UITK.Demo {

#if UNITY_2023_2 || UNITY_6000_0_OR_NEWER
    [UxmlElement(nameof(VirtuaStickRacingPad))]
#endif
    public partial class VirtuaStickRacingPad : VirtuaStick {

        #region UXML TRAITS

#if !(UNITY_2023_2 || UNITY_6000_0_OR_NEWER)
        public new class UxmlFactory : UxmlFactory<VirtuaStickRacingPad, UxmlTraits> { }

        public new class UxmlTraits : VirtuaStick.UxmlTraits {

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc) {
                base.Init(ve, bag, cc);
                var stick = (VirtuaStickRacingPad)ve;
                stick.Dynamic = false;
                stick.DirectionalArrow = false;
            }
        }
#endif
        #endregion

        public VirtuaStickRacingPad() : base() {
#if UNITY_2023_2 || UNITY_6000_0_OR_NEWER
            Dynamic = false;
            DirectionalArrow = false;
#endif
            StickPixels = 300f;
            m_Stick.style.backgroundImage = Resources.Load<Texture2D>("UITK-VS-vehicle-stick");
            m_Stick.SetBorder(0f);
            m_Handle.style.opacity = 0f;
            m_Stick.AddTransition("rotate", new TimeValue(.35f, TimeUnit.Second), EasingMode.EaseOut);
        }

        protected override void OnGeometryChanged(GeometryChangedEvent e) { }

        protected override void OnPointerMove(PointerMoveEvent e) {
            base.OnPointerMove(e);

            if (!IsDragging)
                return;

            var angle = Mathf.Atan2(m_Direction.y, m_Direction.x) * Mathf.Rad2Deg;

            if (m_Stick.style.rotate.value.angle != 90f && angle >= -45f && angle < 45f)
                m_Stick.style.rotate = new Rotate(90f); // Right
            else if (m_Stick.style.rotate.value.angle != -90f && angle >= 135f || angle < -135f)
                m_Stick.style.rotate = new Rotate(-90f); // Left
        }

        protected override void OnPointerUp(PointerUpEvent e) {
            base.OnPointerUp(e);
            m_Stick.style.rotate = new Rotate(0f);
        }
    }
}
