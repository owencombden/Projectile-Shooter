using UnityEngine;
using UnityEngine.InputSystem;

namespace Empress.UITK.Demo {

    [RequireComponent(typeof(PlayerInput))]
    [RequireComponent(typeof(CharacterController))]
    public class SimplePlayerController : MonoBehaviour {

        [SerializeField] private float m_MoveSpeed = 5f;
        [SerializeField] private float m_RotationSpeed = 10f;

        private CharacterController m_CC;
        private Vector2 m_Input;

        void Awake() {
            m_CC = GetComponent<CharacterController>();
        }

        void Update() {
            var inputDirection = new Vector3(m_Input.x, 0f, m_Input.y);

            if (inputDirection.sqrMagnitude > .01f) {
                var camForward = Camera.main.transform.forward;
                var camRight = Camera.main.transform.right;

                camForward.y = 0f;
                camRight.y = 0f;
                camForward.Normalize();
                camRight.Normalize();

                var moveDir = camForward * m_Input.y + camRight * m_Input.x;
                moveDir.y = 0f;
                moveDir.Normalize();

                var targetRotation = Quaternion.LookRotation(moveDir);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    m_RotationSpeed * Time.deltaTime
                );

                var speedFactor = inputDirection.magnitude;
                m_CC.Move(m_MoveSpeed * speedFactor * Time.deltaTime * moveDir);
            }
        }

        public void Input_OnMove(InputAction.CallbackContext context) {
            m_Input = context.ReadValue<Vector2>();

            if (m_Input.sqrMagnitude > 1f)
                m_Input.Normalize();
        }
    }
}
