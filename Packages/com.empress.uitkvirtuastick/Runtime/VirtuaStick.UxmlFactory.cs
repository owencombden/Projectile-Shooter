#if !(UNITY_2023_2 || UNITY_6000_0_OR_NEWER)
using UnityEngine.UIElements;

namespace Empress.UITK {

    public partial class VirtuaStick : VisualElement {

        public new class UxmlFactory : UxmlFactory<VirtuaStick, UxmlTraits> { }

        public new class UxmlTraits : VisualElement.UxmlTraits {

            readonly UxmlEnumAttributeDescription<VisualStyleSource> m_StyleSource = new() {
                name = "style-source",
                defaultValue = VisualStyleSource.Procedural
            };

            readonly UxmlEnumAttributeDescription<InputAxisConstraint> m_AxisConstraint = new() {
                name = "axis-constraint",
                defaultValue = InputAxisConstraint.Free
            };

            readonly UxmlBoolAttributeDescription m_Dynamic = new() {
                name = "dynamic",
                defaultValue = true
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

            readonly UxmlBoolAttributeDescription m_DirectionalArrow = new() {
                name = "directional-arrow",
                defaultValue = true
            };

            readonly UxmlFloatAttributeDescription m_DirectionalArrowSmooth = new() {
                name = "directional-arrow-smooth",
                defaultValue = 0.15f
            };

            readonly UxmlBoolAttributeDescription m_ShowStickArea = new() {
                name = "show-stick-area",
                defaultValue = false
            };

            readonly UxmlBoolAttributeDescription m_DebugInfo = new() {
                name = "debug-info",
                defaultValue = false
            };

            readonly UxmlEnumAttributeDescription<CornerAlign> m_DebugAlign = new() {
                name = "debug-align",
                defaultValue = CornerAlign.TopLeft
            };

            readonly UxmlFloatAttributeDescription m_DebugInfoSize = new() {
                name = "debug-info-size",
                defaultValue = 14f
            };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc) {
                base.Init(ve, bag, cc);
                var stick = (VirtuaStick)ve;

                stick.StyleSource = m_StyleSource.GetValueFromBag(bag, cc);
                stick.AxisConstraint = m_AxisConstraint.GetValueFromBag(bag, cc);
                stick.Dynamic = m_Dynamic.GetValueFromBag(bag, cc);
                stick.OffsetX = m_OffsetX.GetValueFromBag(bag, cc);
                stick.OffsetY = m_OffsetY.GetValueFromBag(bag, cc);
                stick.HandleMoveRadius = m_HandleMoveRadius.GetValueFromBag(bag, cc);
                stick.DirectionalArrow = m_DirectionalArrow.GetValueFromBag(bag, cc);
                stick.DirectionalArrowSmooth = m_DirectionalArrowSmooth.GetValueFromBag(bag, cc);
                stick.ShowStickArea = m_ShowStickArea.GetValueFromBag(bag, cc);
                stick.DebugInfo = m_DebugInfo.GetValueFromBag(bag, cc);
                stick.DebugAlign = m_DebugAlign.GetValueFromBag(bag, cc);
                stick.DebugInfoSize = m_DebugInfoSize.GetValueFromBag(bag, cc);
            }
        }
    }
}
#endif
