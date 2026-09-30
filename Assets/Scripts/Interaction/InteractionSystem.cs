using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EscapeRoom.Interaction
{
    /// <summary>
    /// Centralized raycast interaction system mounted on the Player.
    /// Casts a ray from the center of the PlayerCamera to detect IInteractable objects.
    /// Handles interact key (E) detection via the Unity Input System and renders a clean screen-space prompt.
    /// </summary>
    [DisallowMultipleComponent]
    public class InteractionSystem : MonoBehaviour
    {
        [Header("Detection Settings")]
        [Tooltip("The camera from which the center-screen interaction ray originates.")]
        [SerializeField] private Camera playerCamera;

        [Tooltip("Maximum interaction reach in meters (default 3.0m).")]
        [SerializeField] private float interactionRange = 3.0f;

        [Tooltip("Layer mask filter for interactable raycasting.")]
        [SerializeField] private LayerMask interactionLayers = ~0;

        [Tooltip("Whether interaction raycasts detect trigger colliders.")]
        [SerializeField] private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore;

        [Header("Prompt Display")]
        [Tooltip("Whether to draw the minimal screen-space prompt automatically.")]
        [SerializeField] private bool showScreenPrompt = true;

        [Tooltip("Default prompt text if an interactable has an empty prompt string.")]
        [SerializeField] private string defaultPromptText = "Press E to interact";

        private IInteractable currentInteractable;
        private GameObject currentHitObject;
        private RaycastHit currentHitInfo;

        public event Action<IInteractable> OnInteractableFocused;
        public event Action OnInteractableLost;

        public Camera PlayerCamera
        {
            get => playerCamera;
            set => playerCamera = value;
        }

        public float InteractionRange
        {
            get => interactionRange;
            set => interactionRange = Mathf.Max(0.1f, value);
        }

        public IInteractable CurrentInteractable => currentInteractable;
        public GameObject CurrentHitObject => currentHitObject;
        public bool HasInteractableTarget => currentInteractable != null && currentInteractable.CanInteract;

        private void Start()
        {
            if (playerCamera == null)
            {
                playerCamera = GetComponentInChildren<Camera>();
                if (playerCamera == null)
                {
                    playerCamera = Camera.main;
                }
            }
        }

        private void Update()
        {
            PerformRaycastDetection();
            HandleInteractionInput();
        }

        private void PerformRaycastDetection()
        {
            if (playerCamera == null) return;

            // Cast ray straight through the center of the player camera viewport
            Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            bool hitSomething = Physics.Raycast(ray, out currentHitInfo, interactionRange, interactionLayers, triggerInteraction);

            IInteractable foundInteractable = null;
            GameObject hitGo = null;

            if (hitSomething && currentHitInfo.collider != null)
            {
                hitGo = currentHitInfo.collider.gameObject;
                foundInteractable = currentHitInfo.collider.GetComponent<IInteractable>()
                                    ?? currentHitInfo.collider.GetComponentInParent<IInteractable>();
            }

            // Update focused interactable state
            if (foundInteractable != currentInteractable)
            {
                currentInteractable = foundInteractable;
                currentHitObject = hitGo;

                if (currentInteractable != null && currentInteractable.CanInteract)
                {
                    OnInteractableFocused?.Invoke(currentInteractable);
                }
                else
                {
                    OnInteractableLost?.Invoke();
                }
            }
        }

        private void HandleInteractionInput()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.eKey.wasPressedThisFrame)
            {
                TriggerInteract();
            }
        }

        /// <summary>
        /// Attempts interaction with the currently targeted object.
        /// </summary>
        public void TriggerInteract()
        {
            if (currentInteractable != null && currentInteractable.CanInteract)
            {
                currentInteractable.Interact();
            }
        }

        private void OnGUI()
        {
            if (!showScreenPrompt || Event.current.type != EventType.Repaint) return;

            // Draw subtle center crosshair dot
            DrawCrosshairDot();

            // Draw interaction prompt if looking at a valid interactable
            if (currentInteractable != null && currentInteractable.CanInteract)
            {
                string text = !string.IsNullOrEmpty(currentInteractable.InteractionPrompt)
                    ? currentInteractable.InteractionPrompt
                    : defaultPromptText;

                DrawInteractionPrompt(text);
            }
        }

        private void DrawCrosshairDot()
        {
            float centerX = Screen.width * 0.5f;
            float centerY = Screen.height * 0.5f;
            float dotSize = 4f;

            Color oldColor = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, 0.55f);
            GUI.DrawTexture(new Rect(centerX - dotSize * 0.5f, centerY - dotSize * 0.5f, dotSize, dotSize), Texture2D.whiteTexture);
            GUI.color = oldColor;
        }

        private void DrawInteractionPrompt(string text)
        {
            GUIContent content = new GUIContent(text);
            GUIStyle style = new GUIStyle(GUI.skin.box)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };

            Vector2 size = style.CalcSize(content);
            size.x += 24f;
            size.y += 10f;

            // Position centered horizontally, slightly below center crosshair
            float x = (Screen.width - size.x) * 0.5f;
            float y = (Screen.height * 0.5f) + 35f;

            Color oldColor = GUI.color;
            GUI.color = new Color(0.12f, 0.12f, 0.15f, 0.88f);
            GUI.Box(new Rect(x, y, size.x, size.y), GUIContent.none);

            GUI.color = Color.white;
            GUI.Label(new Rect(x, y, size.x, size.y), content, style);
            GUI.color = oldColor;
        }

        private void OnDrawGizmosSelected()
        {
            if (playerCamera != null)
            {
                Gizmos.color = HasInteractableTarget ? Color.green : Color.cyan;
                Gizmos.DrawRay(playerCamera.transform.position, playerCamera.transform.forward * interactionRange);
            }
        }
    }
}
