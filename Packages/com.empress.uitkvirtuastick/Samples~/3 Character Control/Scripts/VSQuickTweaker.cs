using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Empress.UITK.Demo {

    [RequireComponent(typeof(UIDocument))]
    public class VSQuickTweaker : MonoBehaviour {

        [SerializeField] private VirtuaStickProcedural m_VSP;
        [SerializeField] private string m_ContainerName;

        void Awake() {
            if (!m_VSP)
                return;

            var root = GetComponent<UIDocument>().rootVisualElement;
            var constraintValues = Enum.GetValues(typeof(InputAxisConstraint)).Cast<InputAxisConstraint>().ToArray();
            var dynamicButton = root.Q(m_ContainerName).Q<Button>("Dynamic");
            var axisButton = root.Q(m_ContainerName).Q<Button>("Axis");
            var showStickAreaButton = root.Q(m_ContainerName).Q<Button>("ShowStickArea");
            var debugInfoButton = root.Q(m_ContainerName).Q<Button>("DebugInfo");

            dynamicButton.text = $"Dynamic: {(m_VSP.dynamic ? "ON" : "OFF")}";
            axisButton.text = $"Axis: {m_VSP.axisConstraint}";
            showStickAreaButton.text = $"Show Stick Area: {(m_VSP.showStickArea ? "ON" : "OFF")}";
            debugInfoButton.text = $"Debug Info: {(m_VSP.debugInfo ? "ON" : "OFF")}";

            dynamicButton.clicked += () => {
                m_VSP.dynamic = !m_VSP.dynamic;
                dynamicButton.text = $"Dynamic: {(m_VSP.dynamic ? "ON" : "OFF")}";
                m_VSP.OnValidate();
            };
            axisButton.clicked += () => {
                m_VSP.axisConstraint = (InputAxisConstraint)(((int)m_VSP.axisConstraint + 1) % constraintValues.Length);
                axisButton.text = $"Axis: {m_VSP.axisConstraint}";
                m_VSP.OnValidate();
            };
            showStickAreaButton.clicked += () => {
                m_VSP.showStickArea = !m_VSP.showStickArea;
                showStickAreaButton.text = $"Show Stick Area: {(m_VSP.showStickArea ? "ON" : "OFF")}";
                m_VSP.OnValidate();
            };
            debugInfoButton.clicked += () => {
                m_VSP.debugInfo = !m_VSP.debugInfo;
                debugInfoButton.text = $"Debug Info: {(m_VSP.debugInfo ? "ON" : "OFF")}";
                m_VSP.OnValidate();
            };
        }
    }
}
