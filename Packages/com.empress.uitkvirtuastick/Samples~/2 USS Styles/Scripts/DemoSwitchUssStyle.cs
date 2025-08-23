using UnityEngine;
using UnityEngine.UIElements;

namespace Empress.UITK.Demo {

    [RequireComponent(typeof(UIDocument))]
    public class DemoSwitchUssStyle : MonoBehaviour {

        [SerializeField] private StyleSheet[] m_UssStyles;

        void Awake() {
            if (m_UssStyles == null || m_UssStyles.Length == 0) {
                Debug.LogWarning("No styles assigned to m_UssStyles.");
                return;
            }

            var root = GetComponent<UIDocument>().rootVisualElement;
            var virtuaStick = root.Q<VirtuaStick>();
            var styleButton = root.Q<Button>("Style_Button");
            var index = 0;

            styleButton.text = $"Style: {m_UssStyles[0].name}";

            styleButton.clicked += () => {
                virtuaStick.styleSheets.Remove(m_UssStyles[index]);
                index = (index + 1) % m_UssStyles.Length;
                virtuaStick.styleSheets.Add(m_UssStyles[index]);
                styleButton.text = $"Style: {m_UssStyles[index].name}";
            };
        }
    }
}
