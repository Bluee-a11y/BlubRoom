using UnityEngine;
using UnityEngine.SceneManagement;

namespace ClubhousePC
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerMotor : MonoBehaviour
    {
        public Transform View;
        public float WalkSpeed = 5f;
        public float JumpHeight = 1.1f;
        public float MouseSensitivity = 2f;
        public float StandingHeight = 1.8f;
        public float CrouchingHeight = 1.1f;
        public bool IsCrouching { get; private set; }
        public bool IsThirdPerson { get; private set; }

        private CharacterController controller;
        private Renderer[] localPlayerVisuals;
        private Transform offlineCapsule;
        private Vector3 offlineCapsulePosition;
        private Vector3 offlineCapsuleScale;
        private float verticalSpeed;
        private float pitch;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            localPlayerVisuals = GetComponentsInChildren<Renderer>(true);
            foreach (var visual in localPlayerVisuals)
            {
                if (visual.gameObject.name != "Capsule") continue;
                offlineCapsule = visual.transform;
                offlineCapsulePosition = offlineCapsule.localPosition;
                offlineCapsuleScale = offlineCapsule.localScale;
                var extraCollider = offlineCapsule.GetComponent<Collider>();
                if (extraCollider != null) extraCollider.enabled = false;
                break;
            }
            SetLocalVisuals(false);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Update()
        {
            if (SceneManager.GetActiveScene().name == "BlubCenter" && transform.position.y < -8f)
            {
                controller.enabled = false;
                transform.position = new Vector3(0f, 4f, -3.5f);
                controller.enabled = true;
                verticalSpeed = 0f;
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Cursor.lockState = Cursor.lockState == CursorLockMode.Locked
                    ? CursorLockMode.None : CursorLockMode.Locked;
                Cursor.visible = Cursor.lockState != CursorLockMode.Locked;
            }

            if (Input.GetKeyDown(KeyCode.RightShift))
            {
                IsThirdPerson = !IsThirdPerson;
                SetLocalVisuals(IsThirdPerson);
            }

            var mobile = MobileControls.Current != null && MobileControls.Current.Enabled;
            var wantsCrouch = mobile ? MobileControls.Current.Crouching :
                Input.GetKey(KeyCode.LeftControl);
            IsCrouching = wantsCrouch;
            var targetHeight = IsCrouching ? CrouchingHeight : StandingHeight;
            controller.height = Mathf.Lerp(controller.height, targetHeight, 14f * Time.deltaTime);
            controller.center = new Vector3(0, controller.height * 0.5f, 0);
            if (IsThirdPerson)
            {
                // A single locked chase-camera position and angle.
                View.localPosition = new Vector3(0f, 2.4f, -6f);
            }
            else
            {
                var viewPosition = View.localPosition;
                viewPosition.x = 0f;
                viewPosition.y = Mathf.Lerp(viewPosition.y, IsCrouching ? 1.0f : 1.6f, 14f * Time.deltaTime);
                viewPosition.z = 0f;
                View.localPosition = viewPosition;
            }

            if (Cursor.lockState != CursorLockMode.Locked && !mobile) return;

            var look = mobile ? MobileControls.Current.ConsumeLook() * 0.08f :
                new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
            transform.Rotate(0, look.x * MouseSensitivity, 0);
            if (IsThirdPerson)
            {
                View.localRotation = Quaternion.Euler(8f, 0f, 0f);
            }
            else
            {
                pitch = Mathf.Clamp(pitch - look.y * MouseSensitivity, -85, 85);
                View.localRotation = Quaternion.Euler(pitch, 0, 0);
            }

            var input = mobile ? MobileControls.Current.Move :
                new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            var move = transform.right * input.x + transform.forward * input.y;
            move = Vector3.ClampMagnitude(move, 1f) * WalkSpeed;

            if (controller.isGrounded)
            {
                verticalSpeed = -2f;
                if (mobile ? MobileControls.Current.ConsumeJump() : Input.GetButtonDown("Jump"))
                    verticalSpeed = Mathf.Sqrt(JumpHeight * 2f * 20f);
            }
            verticalSpeed -= 20f * Time.deltaTime;
            move.y = verticalSpeed;
            controller.Move(move * Time.deltaTime);
        }

        private void SetLocalVisuals(bool visible)
        {
            if (localPlayerVisuals == null) return;
            foreach (var visual in localPlayerVisuals)
            {
                if (visual.GetComponent<TextMesh>() != null) continue;
                var isHead = visual.gameObject.name.ToLowerInvariant().Contains("head");
                visual.enabled = visible || !isHead;
            }

            if (offlineCapsule != null)
            {
                offlineCapsule.localPosition = visible
                    ? offlineCapsulePosition
                    : new Vector3(offlineCapsulePosition.x, 0.45f, offlineCapsulePosition.z);
                offlineCapsule.localScale = visible
                    ? offlineCapsuleScale
                    : new Vector3(offlineCapsuleScale.x * 0.65f,
                        offlineCapsuleScale.y * 0.45f, offlineCapsuleScale.z * 0.65f);
            }
        }

    }
}
