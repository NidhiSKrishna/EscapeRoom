using UnityEngine;
using UnityEngine.InputSystem;

namespace EscapeRoom.Player
{
    /// <summary>
    /// Handles first-person mouse look, player body yaw, camera vertical pitch clamping, and cursor locking.
    /// Uses the Unity Input System for mouse input and Escape key detection.
    /// Locomotion and translation are delegated to PlayerController.
    /// 1 Unity unit = 1 meter.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerLook : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The child Camera transform to pitch vertically.")]
        [SerializeField] private Transform playerCamera;

        [Header("Mouse Sensitivity")]
        [Tooltip("Horizontal mouse sensitivity (yaw).")]
        [SerializeField] private float sensitivityX = 0.12f;

        [Tooltip("Vertical mouse sensitivity (pitch).")]
        [SerializeField] private float sensitivityY = 0.12f;

        [Tooltip("Invert vertical look direction.")]
        [SerializeField] private bool invertY = false;

        [Header("Pitch Limits (Degrees)")]
        [Tooltip("Maximum upward look angle (degrees).")]
        [Range(-89f, 0f)]
        [SerializeField] private float minPitch = -85.0f;

        [Tooltip("Maximum downward look angle (degrees).")]
        [Range(0f, 89f)]
        [SerializeField] private float maxPitch = 85.0f;

        [Header("Camera Height (Meters)")]
        [Tooltip("Eye level height relative to the player pivot (meters).")]
        [SerializeField] private float cameraHeight = 1.65f;

        [Header("Cursor Management")]
        [Tooltip("Automatically lock cursor to game window on Start.")]
        [SerializeField] private bool autoLockCursor = true;

        private float currentPitch = 0f;
        private bool isCursorLocked = false;

        public Transform PlayerCamera
        {
            get => playerCamera;
            set => playerCamera = value;
        }

        public float SensitivityX
        {
            get => sensitivityX;
            set => sensitivityX = Mathf.Max(0.001f, value);
        }

        public float SensitivityY
        {
            get => sensitivityY;
            set => sensitivityY = Mathf.Max(0.001f, value);
        }

        public float CameraHeight
        {
            get => cameraHeight;
            set
            {
                cameraHeight = Mathf.Max(0.2f, value);
                ApplyCameraHeight();
            }
        }

        public bool IsCursorLocked => isCursorLocked;

        private void Start()
        {
            if (playerCamera == null)
            {
                var cam = GetComponentInChildren<Camera>();
                if (cam != null)
                {
                    playerCamera = cam.transform;
                }
            }

            ApplyCameraHeight();

            if (autoLockCursor && !EscapeRoom.UI.AtticBriefingUI.IsBriefingOpen && !EscapeRoom.UI.PhoneIntroUI.IsIntroOpen)
            {
                SetCursorLock(true);
            }
        }

        private void Update()
        {
            HandleCursorToggleInput();

            if (isCursorLocked)
            {
                ProcessMouseLook();
            }
        }

        public void ApplyCameraHeight()
        {
            if (playerCamera != null)
            {
                Vector3 localPos = playerCamera.localPosition;
                localPos.y = cameraHeight;
                playerCamera.localPosition = localPos;
            }
        }

        private void HandleCursorToggleInput()
        {
            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame && !isCursorLocked)
            {
                if (CanLockCursor())
                {
                    SetCursorLock(true);
                }
            }
        }

        private bool CanLockCursor()
        {
            if (EscapeRoom.UI.KeypadUI.Instance != null && EscapeRoom.UI.KeypadUI.Instance.IsOpen) return false;
            if (EscapeRoom.Interaction.ClueInteractable.IsAnyClueOpen) return false;
            if (EscapeRoom.UI.AtticBriefingUI.IsBriefingOpen) return false;
            if (EscapeRoom.UI.PhoneIntroUI.IsIntroOpen) return false;
            if (EscapeRoom.UI.ObjectiveHUD.IsExpandedViewOpen) return false;
            if (EscapeRoom.UI.ExitCodeDisplayUI.IsDisplayOpen) return false;
            if (EscapeRoom.Core.PauseManager.Instance != null && EscapeRoom.Core.PauseManager.Instance.IsPaused) return false;
            if (EscapeRoom.Core.GameManager.Instance != null && EscapeRoom.Core.GameManager.Instance.HasEscaped) return false;
            if (EscapeRoom.UI.EscapeUI.Instance != null && EscapeRoom.UI.EscapeUI.Instance.IsOpen) return false;
            return true;
        }

        public void SetCursorLock(bool locked)
        {
            isCursorLocked = locked;
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        private void ProcessMouseLook()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;

            Vector2 delta = mouse.delta.ReadValue();

            float yawDelta = delta.x * sensitivityX;
            float pitchDelta = delta.y * sensitivityY * (invertY ? -1f : 1f);

            // Horizontal rotation (yaw) turns the entire player body
            transform.Rotate(Vector3.up * yawDelta);

            // Vertical rotation (pitch) tilts the camera with clamping
            currentPitch -= pitchDelta;
            currentPitch = Mathf.Clamp(currentPitch, minPitch, maxPitch);

            if (playerCamera != null)
            {
                playerCamera.localRotation = Quaternion.Euler(currentPitch, 0f, 0f);
            }
        }
    }
}
