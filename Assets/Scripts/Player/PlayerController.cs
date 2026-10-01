using UnityEngine;
using UnityEngine.InputSystem;

namespace EscapeRoom.Player
{
    /// <summary>
    /// Handles first-person locomotion, gravity, and ground detection using Unity's CharacterController.
    /// Uses the Unity Input System for keyboard input.
    /// Rotation and camera orientation are delegated to PlayerLook.
    /// 1 Unity unit = 1 meter.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [DisallowMultipleComponent]
    public class PlayerController : MonoBehaviour
    {
        [Header("Locomotion (Meters / Sec)")]
        [Tooltip("Horizontal movement speed in meters per second.")]
        [SerializeField] private float moveSpeed = 4.0f;

        [Tooltip("Acceleration factor for smoothing horizontal input.")]
        [SerializeField] private float acceleration = 14.0f;

        [Header("Gravity & Physics")]
        [Tooltip("Downward gravitational acceleration in m/s^2.")]
        [SerializeField] private float gravity = -19.62f;

        [Tooltip("Slight downward velocity applied when grounded to maintain surface contact.")]
        [SerializeField] private float groundedGravity = -2.0f;

        [Header("Ground Detection")]
        [Tooltip("Extra distance below the CharacterController base to check for ground surfaces.")]
        [SerializeField] private float groundCheckDistance = 0.15f;

        [Tooltip("Radius of the sphere cast used for ground checking.")]
        [SerializeField] private float groundCheckRadius = 0.35f;

        [Tooltip("Layers recognized as walkable ground.")]
        [SerializeField] private LayerMask groundMask = ~0;

        private CharacterController characterController;
        private Vector3 horizontalVelocity;
        private float verticalVelocityY;
        private bool isGrounded;

        public bool IsGrounded => isGrounded;

        public float MoveSpeed
        {
            get => moveSpeed;
            set => moveSpeed = Mathf.Max(0.1f, value);
        }

        public float Gravity
        {
            get => gravity;
            set => gravity = value;
        }

        public float CurrentSpeed => horizontalVelocity.magnitude;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
        }

        private void Update()
        {
            UpdateGroundState();
            ProcessMovement();
            ApplyGravity();
        }

        private void UpdateGroundState()
        {
            // CharacterController's native ground flag
            bool ccGrounded = characterController.isGrounded;

            // Secondary sphere check at feet for extra stability on edges, slopes, and seams
            Vector3 feetPosition = new Vector3(
                transform.position.x,
                transform.position.y + characterController.radius - groundCheckDistance,
                transform.position.z);

            bool sphereGrounded = Physics.CheckSphere(feetPosition, groundCheckRadius, groundMask, QueryTriggerInteraction.Ignore);

            isGrounded = ccGrounded || sphereGrounded;
        }

        private void ProcessMovement()
        {
            Vector2 input = ReadMoveInput();

            // Transform input relative to the player's horizontal orientation
            Vector3 moveDirection = (transform.right * input.x + transform.forward * input.y);
            if (moveDirection.sqrMagnitude > 1f)
            {
                moveDirection.Normalize();
            }

            Vector3 targetVelocity = moveDirection * moveSpeed;
            horizontalVelocity = Vector3.MoveTowards(
                horizontalVelocity,
                targetVelocity,
                acceleration * Time.deltaTime);

            characterController.Move(horizontalVelocity * Time.deltaTime);
        }

        private Vector2 ReadMoveInput()
        {
            if (EscapeRoom.Interaction.ClueInteractable.IsAnyClueOpen ||
                (EscapeRoom.UI.KeypadUI.Instance != null && EscapeRoom.UI.KeypadUI.Instance.IsOpen) ||
                (EscapeRoom.UI.AtticBriefingUI.IsBriefingOpen) ||
                (EscapeRoom.UI.PhoneIntroUI.IsIntroOpen) ||
                (EscapeRoom.UI.ObjectiveHUD.IsExpandedViewOpen) ||
                (EscapeRoom.Core.PauseManager.Instance != null && EscapeRoom.Core.PauseManager.Instance.IsPaused) ||
                (EscapeRoom.Core.GameManager.Instance != null && EscapeRoom.Core.GameManager.Instance.HasEscaped))
            {
                return Vector2.zero;
            }

            Vector2 input = Vector2.zero;

            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) input.y += 1f;
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) input.y -= 1f;
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) input.x -= 1f;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) input.x += 1f;
            }

            return input;
        }

        private void ApplyGravity()
        {
            if (isGrounded && verticalVelocityY < 0f)
            {
                verticalVelocityY = groundedGravity;
            }
            else
            {
                verticalVelocityY += gravity * Time.deltaTime;
            }

            Vector3 verticalDisplacement = new Vector3(0f, verticalVelocityY, 0f) * Time.deltaTime;
            characterController.Move(verticalDisplacement);
        }

        private void OnDrawGizmosSelected()
        {
            if (characterController == null)
            {
                characterController = GetComponent<CharacterController>();
            }

            if (characterController != null)
            {
                Gizmos.color = isGrounded ? Color.green : Color.red;
                Vector3 feetPosition = new Vector3(
                    transform.position.x,
                    transform.position.y + characterController.radius - groundCheckDistance,
                    transform.position.z);
                Gizmos.DrawWireSphere(feetPosition, groundCheckRadius);
            }
        }
    }
}
