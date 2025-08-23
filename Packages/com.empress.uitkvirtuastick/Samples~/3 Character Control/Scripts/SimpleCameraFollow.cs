using UnityEngine;
using UnityEngine.InputSystem;

namespace Empress.UITK.Demo {

    public class SimpleCameraFollow : MonoBehaviour {

        [SerializeField] private Transform m_Target;
        [SerializeField] private float m_RotationSpeed = 100f;
        [SerializeField, Min(-80)] private float m_MinYaw = 10f;
        [SerializeField, Min(0)] private float m_MaxYaw = 45f;
        [SerializeField] private InputAction m_InputAction;

        private Vector3 m_InitialLocalOffset;
        private Vector2 m_Input;
        private float m_Yaw;
        private float m_Pitch;

        void Start() {
            if (!m_Target)
                return;

            var euler = transform.rotation.eulerAngles;
            m_Yaw = euler.y;
            m_Pitch = euler.x;

            var initialRotation = Quaternion.Euler(euler.x, euler.y, 0f);
            m_InitialLocalOffset = Quaternion.Inverse(initialRotation) * (transform.position - m_Target.position);
        }

        void OnEnable() {
            m_InputAction.Enable();
            m_InputAction.performed += Input_OnRotate;
            m_InputAction.canceled += Input_OnRotate;
        }

        void OnDisable() {
            m_InputAction.Disable();
            m_InputAction.performed -= Input_OnRotate;
            m_InputAction.canceled -= Input_OnRotate;
        }

        void LateUpdate() {
            if (!m_Target)
                return;

            m_Yaw += m_Input.x * m_RotationSpeed * Time.deltaTime;
            m_Pitch -= m_Input.y * m_RotationSpeed * Time.deltaTime;
            m_Pitch = Mathf.Clamp(m_Pitch, m_MinYaw, m_MaxYaw);

            var rotation = Quaternion.Euler(m_Pitch, m_Yaw, 0f);
            var rotatedOffset = rotation * m_InitialLocalOffset;

            transform.SetPositionAndRotation(m_Target.position + rotatedOffset, rotation);
        }

        void Input_OnRotate(InputAction.CallbackContext context) {
            m_Input = context.ReadValue<Vector2>();

            if (m_Input.sqrMagnitude > 1f) 
                m_Input.Normalize();
        }
    }
}
