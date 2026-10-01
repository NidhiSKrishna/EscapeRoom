using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using EscapeRoom.UI;

namespace EscapeRoom.Interaction
{
    /// <summary>
    /// Interactive container that starts locked and requires a specific item ID (e.g. 'room_key') in inventory to open.
    /// Implements IInteractable.
    /// Rotates a lid/cover smoothly and reveals contents upon opening.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class LockedContainer : MonoBehaviour, IInteractable
    {
        [Header("Key Requirement")]
        [Tooltip("The item ID required in the player's inventory to unlock this container.")]
        [SerializeField] private string requiredItemId = "room_key";

        [Tooltip("Friendly name of the required key for prompt display.")]
        [SerializeField] private string requiredItemName = "Room Key";

        [Tooltip("Whether to consume/remove the key from the inventory upon opening.")]
        [SerializeField] private bool consumeKey = false;

        [Header("State")]
        [SerializeField] private bool isLocked = true;
        [SerializeField] private bool isOpen = false;

        [Header("Visual / Animation Elements")]
        [Tooltip("Optional child transform that hinges/rotates open.")]
        [SerializeField] private Transform lidTransform;

        [Tooltip("Target local euler rotation angles for open state.")]
        [SerializeField] private Vector3 openRotationOffset = new Vector3(-105f, 0f, 0f);

        [Tooltip("Duration of the lid opening animation in seconds.")]
        [SerializeField] private float openDuration = 0.8f;

        [Tooltip("Optional hidden contents object inside the container revealed on opening.")]
        [SerializeField] private GameObject contentsObject;

        [Header("Prompts")]
        [SerializeField] private string lockedMissingKeyPrompt = "Locked (Requires {0})";
        [SerializeField] private string lockedHaveKeyPrompt = "Press E to unlock with {0}";
        [SerializeField] private string openPrompt = "Press E to open";
        [SerializeField] private string openedPrompt = "Container is already open";

        [Header("Events")]
        public UnityEvent onContainerUnlocked;
        public UnityEvent onContainerOpened;

        private Quaternion closedRotation;
        private Coroutine openCoroutine;

        public string RequiredItemId
        {
            get => requiredItemId;
            set => requiredItemId = value;
        }

        public bool IsLocked => isLocked;
        public bool IsOpen => isOpen;

        public Transform LidTransform
        {
            get => lidTransform;
            set => lidTransform = value;
        }

        public GameObject ContentsObject
        {
            get => contentsObject;
            set => contentsObject = value;
        }

        public string InteractionPrompt
        {
            get
            {
                if (isOpen)
                {
                    return openedPrompt;
                }

                if (isLocked)
                {
                    bool hasKey = InventorySystem.Instance != null && InventorySystem.Instance.HasItem(requiredItemId);
                    return hasKey
                        ? string.Format(lockedHaveKeyPrompt, requiredItemName)
                        : string.Format(lockedMissingKeyPrompt, requiredItemName);
                }

                return openPrompt;
            }
        }

        public bool CanInteract => !isOpen;

        private void Awake()
        {
            if (lidTransform != null)
            {
                closedRotation = lidTransform.localRotation;
            }

            if (contentsObject == null)
            {
                var clueChild = GetComponentInChildren<ClueInteractable>(true);
                if (clueChild != null)
                {
                    contentsObject = clueChild.gameObject;
                }
            }

            if (isOpen)
            {
                Collider rootCol = GetComponent<Collider>();
                if (rootCol != null) rootCol.enabled = false;
                if (contentsObject != null) contentsObject.SetActive(true);
            }
            else if (contentsObject != null && isLocked)
            {
                contentsObject.SetActive(false);
            }
        }

        public void Interact()
        {
            if (isOpen) return;

            if (isLocked)
            {
                InventorySystem inventory = InventorySystem.Instance;
                if (inventory == null)
                {
                    inventory = FindAnyObjectByType<InventorySystem>();
                }

                bool hasKey = inventory != null && inventory.HasItem(requiredItemId);

                if (!hasKey)
                {
                    Debug.Log($"[LockedContainer] '{gameObject.name}' is locked. Requires '{requiredItemId}'.");
                    FeedbackHUD.ShowMessage($"Locked! Requires {requiredItemName}.", new Color(0.95f, 0.40f, 0.40f));
                    return;
                }

                // Player holds the required key!
                isLocked = false;
                Debug.Log($"<color=#5cb85c><b>[LockedContainer]</b></color> '{gameObject.name}' unlocked using '{requiredItemId}'!");
                FeedbackHUD.ShowMessage($"Unlocked container with {requiredItemName}!", new Color(0.40f, 0.90f, 0.45f));
                onContainerUnlocked?.Invoke();

                if (consumeKey && inventory != null)
                {
                    inventory.RemoveItem(requiredItemId);
                }
            }

            // Open the container
            OpenContainer();
        }

        public void OpenContainer()
        {
            if (isOpen) return;
            isOpen = true;

            if (openCoroutine != null)
            {
                StopCoroutine(openCoroutine);
            }

            if (lidTransform != null && gameObject.activeInHierarchy)
            {
                openCoroutine = StartCoroutine(AnimateLidOpen());
            }

            if (contentsObject == null)
            {
                var clueChild = GetComponentInChildren<ClueInteractable>(true);
                if (clueChild != null)
                {
                    contentsObject = clueChild.gameObject;
                }
            }

            if (contentsObject != null)
            {
                contentsObject.SetActive(true);
            }

            // Disable container interaction collider so player can directly look at and target contents
            Collider rootCol = GetComponent<Collider>();
            if (rootCol != null)
            {
                rootCol.enabled = false;
            }

            Debug.Log($"<color=#5cb85c><b>[LockedContainer]</b></color> '{gameObject.name}' is now open.");
            onContainerOpened?.Invoke();
        }

        private IEnumerator AnimateLidOpen()
        {
            Quaternion startRot = lidTransform.localRotation;
            Quaternion targetRot = closedRotation * Quaternion.Euler(openRotationOffset);
            float elapsed = 0f;

            while (elapsed < openDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / openDuration);
                // Smooth step curve
                float smoothT = Mathf.SmoothStep(0f, 1f, t);
                lidTransform.localRotation = Quaternion.Slerp(startRot, targetRot, smoothT);
                yield return null;
            }

            lidTransform.localRotation = targetRot;
        }
    }
}
