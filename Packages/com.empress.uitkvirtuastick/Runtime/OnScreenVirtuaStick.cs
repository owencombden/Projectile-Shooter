using UnityEngine;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.UIElements;

namespace Empress.UITK {

    /// <summary>
    /// Input System bridge for VirtuaStick UI controls.
    /// Converts touch/pointer input into standardized Vector2 input actions.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class OnScreenVirtuaStick : OnScreenControl {

        [InputControl(layout = "Vector2")]
        [SerializeField, Tooltip("Input action path to bind stick control (e.g. '<Gamepad>/leftStick')")]
        protected string m_StickPath;

        /// <summary>
        /// Gets or sets the bound input action path
        /// </summary>
        protected override string controlPathInternal {
            get => m_StickPath;
            set => m_StickPath = value;
        }

        // =====================================================================
        // UI References
        // =====================================================================

        /// <summary>
        /// 
        /// </summary>
        public UIDocument UIDocument { get; protected set; }

        /// <summary>
        /// 
        /// </summary>
        public VisualElement Root => UIDocument ? UIDocument.rootVisualElement : null;

        /// <summary>
        /// 
        /// </summary>
        public VirtuaStick VirtuaStick { get; protected set; }

        // =====================================================================
        // Initialization
        // =====================================================================

        /// <summary>
        /// Initializes UI element references
        /// </summary>
        protected virtual void Awake() {
            UIDocument = GetComponent<UIDocument>();

            if (TryGetComponent(out VirtuaStickProcedural vsp)) {
                vsp.OnValidate();
                VirtuaStick = vsp.VirtuaStick;
            }
            else
                VirtuaStick = Root.Q<VirtuaStick>();
        }

        // =====================================================================
        // Event Handling
        // =====================================================================

        /// <summary>
        /// Enables input event subscriptions
        /// </summary>
        protected override void OnEnable() {
            base.OnEnable();
            if (VirtuaStick == null) return;

            VirtuaStick.OnDrag += OnDrag;
            VirtuaStick.OnEndDrag += OnEndDrag;
        }

        /// <summary>
        /// Cleans up input event subscriptions
        /// </summary>
        protected override void OnDisable() {
            base.OnDisable();
            if (VirtuaStick == null) return;

            VirtuaStick.OnDrag -= OnDrag;
            VirtuaStick.OnEndDrag -= OnEndDrag;
        }

        // =====================================================================
        // Input Processing
        // =====================================================================

        /// <summary>
        /// Processes ongoing drag input
        /// </summary>
        /// <param name="delta">Normalized input direction (values clamped to -1..1 range)</param>
        protected virtual void OnDrag(Vector2 delta) {
            SendValueToControl(delta);
        }

        /// <summary>
        /// Handles stick release event
        /// </summary>
        protected virtual void OnEndDrag() {
            SendValueToControl(Vector2.zero);
        }
    }
}
