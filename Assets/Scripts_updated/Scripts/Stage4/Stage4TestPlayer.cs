using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Stage 4 - TEMPORARY stand-in player, only for testing Stage 4 on its own.
/// WASD move, Shift run, mouse look, E interacts with Stage4Interactable objects.
/// Uses the NEW Input System (Project Settings > Player > Active Input Handling).
///
/// DO NOT ship this. The spec says to use the project's existing player.
/// When the real player exists: delete this player object from the Stage 4 scene and use theirs.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class Stage4TestPlayer : MonoBehaviour
{
    /// <summary>Set to true while a UI panel (keypad, terminal) is open. Frees the mouse, stops movement.</summary>
    public static bool InputLocked;

    [Header("References")]
    [SerializeField] private Camera playerCamera;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float sprintMultiplier = 1.6f;
    [SerializeField] private float gravity = -20f;

    [Header("Look")]
    [SerializeField] private float mouseSensitivity = 0.12f;
    [SerializeField] private float maxLookAngle = 85f;

    [Header("Interaction")]
    [SerializeField] private float interactDistance = 3f;
    [SerializeField] private bool showCrosshairAndPrompt = true;

    private CharacterController controller;
    private float pitch;
    private float verticalVelocity;
    private Stage4Interactable current;
    private GUIStyle promptStyle;

    // =====================================================================
    // UNITY
    // =====================================================================

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (playerCamera == null) playerCamera = GetComponentInChildren<Camera>();
        if (playerCamera == null) playerCamera = Camera.main;
        InputLocked = false;
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnDisable()
    {
        InputLocked = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void Update()
    {
        UpdateCursor();

        if (InputLocked)
        {
            current = null;
            return;
        }

        if (Cursor.lockState == CursorLockMode.Locked) Look();
        Move();
        UpdateInteraction();
    }

    // =====================================================================
    // CURSOR
    // =====================================================================

    private void UpdateCursor()
    {
        if (InputLocked)
        {
            if (Cursor.lockState != CursorLockMode.None) Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return;
        }

        // Esc frees the mouse (so you can reach the Unity Editor). Clicking the Game view locks it again.
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame && Cursor.lockState == CursorLockMode.Locked)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return;
        }

        Mouse mouse = Mouse.current;
        if (Cursor.lockState != CursorLockMode.Locked && mouse != null && mouse.leftButton.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    // =====================================================================
    // LOOK / MOVE
    // =====================================================================

    private void Look()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null || playerCamera == null) return;

        Vector2 delta = mouse.delta.ReadValue() * mouseSensitivity;
        transform.Rotate(0f, delta.x, 0f);

        pitch = Mathf.Clamp(pitch - delta.y, -maxLookAngle, maxLookAngle);
        playerCamera.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    private void Move()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        float x = (keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f);
        float z = (keyboard.wKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed ? 1f : 0f);

        Vector3 move = transform.right * x + transform.forward * z;
        if (move.sqrMagnitude > 1f) move.Normalize();

        float speed = moveSpeed * (keyboard.leftShiftKey.isPressed ? sprintMultiplier : 1f);

        if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
        verticalVelocity += gravity * Time.deltaTime;

        Vector3 velocity = move * speed + Vector3.up * verticalVelocity;
        controller.Move(velocity * Time.deltaTime);
    }

    // =====================================================================
    // INTERACTION (key E)
    // =====================================================================

    private void UpdateInteraction()
    {
        current = null;
        if (playerCamera == null) return;

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, interactDistance, ~0, QueryTriggerInteraction.Ignore))
        {
            Stage4Interactable target = hit.collider.GetComponentInParent<Stage4Interactable>();
            if (target != null && target.CanInteract) current = target;
        }

        Keyboard keyboard = Keyboard.current;
        if (current != null && keyboard != null && keyboard.eKey.wasPressedThisFrame)
        {
            current.Interact();
        }
    }

    // =====================================================================
    // SIMPLE ON-SCREEN CROSSHAIR AND PROMPT (temporary; the real UI comes later)
    // =====================================================================

    private void OnGUI()
    {
        if (!showCrosshairAndPrompt || InputLocked) return;

        float cx = Screen.width * 0.5f;
        float cy = Screen.height * 0.5f;
        GUI.Box(new Rect(cx - 2f, cy - 2f, 4f, 4f), GUIContent.none);

        if (current == null) return;

        if (promptStyle == null)
        {
            promptStyle = new GUIStyle(GUI.skin.box);
            promptStyle.fontSize = 16;
            promptStyle.alignment = TextAnchor.MiddleCenter;
            promptStyle.normal.textColor = Color.white;
        }

        GUI.Box(new Rect(cx - 190f, cy + 40f, 380f, 34f), current.PromptText, promptStyle);
    }
}
