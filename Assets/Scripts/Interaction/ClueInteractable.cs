using UnityEngine;
using UnityEngine.InputSystem;
using EscapeRoom.Player;
using EscapeRoom.UI;

namespace EscapeRoom.Interaction
{
    /// <summary>
    /// Interactive note/document that displays readable clue text in a clean screen-space modal.
    /// Implements IInteractable.
    /// Can be dismissed with Escape, E, Space, or on-screen Close button.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class ClueInteractable : MonoBehaviour, IInteractable
    {
        [Header("Clue Content")]
        [Tooltip("Header title displayed at the top of the reading panel.")]
        [SerializeField] private string clueTitle = "Maintenance Security Note";

        [TextArea(4, 12)]
        [Tooltip("The readable clue text content.")]
        [SerializeField] private string clueText =
            "FACILITY OVERRIDE PROTOCOL\n\n" +
            "In case of lockouts, the door bypass code is calculated from facility blueprint metrics:\n\n" +
            "  1. Room Width (meters): 8\n" +
            "  2. Total Wooden Crates: 4\n" +
            "  3. Room Height rounded: 3\n" +
            "  4. Exit Doorway Width: 1\n\n" +
            "Assemble the four digits in sequence to authorize emergency release.";

        [Header("Interaction Settings")]
        [SerializeField] private string promptText = "Press E to examine note";

        private static ClueInteractable activeClue;
        public static bool IsAnyClueOpen => activeClue != null && activeClue.isReading;
        public static ClueInteractable ActiveClue => activeClue;

        private bool isReading = false;
        private PlayerLook cachedPlayerLook;

        public string ClueTitle
        {
            get => clueTitle;
            set => clueTitle = value;
        }

        public string ClueText
        {
            get => clueText;
            set => clueText = value;
        }

        public string InteractionPrompt => promptText;
        public bool CanInteract => !isReading && gameObject.activeInHierarchy;
        public bool IsReading => isReading;

        private void Update()
        {
            if (!isReading) return;

            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.escapeKey.wasPressedThisFrame ||
                    keyboard.eKey.wasPressedThisFrame ||
                    keyboard.spaceKey.wasPressedThisFrame)
                {
                    CloseClue();
                }
            }
        }

        public void Interact()
        {
            if (isReading) return;

            OpenClue();
        }

        public void OpenClue()
        {
            isReading = true;
            activeClue = this;

            // Unlock cursor for reading/interaction
            if (cachedPlayerLook == null)
            {
                cachedPlayerLook = FindAnyObjectByType<PlayerLook>();
            }

            if (cachedPlayerLook != null)
            {
                cachedPlayerLook.SetCursorLock(false);
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            FeedbackHUD.ShowMessage("Reading clue: " + clueTitle, new Color(0.90f, 0.90f, 0.70f));
            Debug.Log($"<color=#337ab7><b>[ClueInteractable]</b></color> Opened clue '{clueTitle}'.");
        }

        public void CloseClue()
        {
            if (!isReading) return;
            isReading = false;
            if (activeClue == this) activeClue = null;

            // Re-lock cursor
            if (cachedPlayerLook != null)
            {
                cachedPlayerLook.SetCursorLock(true);
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            Debug.Log($"[ClueInteractable] Closed clue '{clueTitle}'.");
        }

        private void OnDestroy()
        {
            if (activeClue == this)
            {
                activeClue = null;
            }
        }

        private void OnGUI()
        {
            if (!isReading) return;

            float panelWidth = Mathf.Min(560f, Screen.width * 0.85f);
            float panelHeight = Mathf.Min(400f, Screen.height * 0.80f);

            float panelX = (Screen.width - panelWidth) * 0.5f;
            float panelY = (Screen.height - panelHeight) * 0.5f;

            // Semi-transparent backdrop to dim the game world
            Color oldColor = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.65f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);

            // Document card background
            GUI.color = new Color(0.12f, 0.13f, 0.16f, 0.96f);
            GUI.Box(new Rect(panelX, panelY, panelWidth, panelHeight), GUIContent.none);

            // Inner border accent
            GUI.color = new Color(0.85f, 0.70f, 0.35f, 0.90f);
            GUI.Box(new Rect(panelX + 6, panelY + 6, panelWidth - 12, panelHeight - 12), GUIContent.none);

            // Background fill
            GUI.color = new Color(0.16f, 0.17f, 0.20f, 0.98f);
            GUI.Box(new Rect(panelX + 8, panelY + 8, panelWidth - 16, panelHeight - 16), GUIContent.none);

            // Title
            GUI.color = Color.white;
            GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.96f, 0.82f, 0.45f) }
            };
            GUI.Label(new Rect(panelX + 20, panelY + 20, panelWidth - 40, 32), clueTitle, titleStyle);

            // Divider line
            GUI.color = new Color(0.4f, 0.4f, 0.5f, 0.6f);
            GUI.DrawTexture(new Rect(panelX + 24, panelY + 54, panelWidth - 48, 2), Texture2D.whiteTexture);

            // Clue body text
            GUI.color = Color.white;
            GUIStyle bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.UpperLeft,
                wordWrap = true,
                normal = { textColor = new Color(0.92f, 0.92f, 0.95f) }
            };
            GUI.Label(new Rect(panelX + 28, panelY + 68, panelWidth - 56, panelHeight - 130), clueText, bodyStyle);

            // Close button at bottom
            float btnWidth = 140f;
            float btnHeight = 32f;
            float btnX = panelX + (panelWidth - btnWidth) * 0.5f;
            float btnY = panelY + panelHeight - 44f;

            GUIStyle btnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };

            if (GUI.Button(new Rect(btnX, btnY, btnWidth, btnHeight), "Close (ESC)", btnStyle))
            {
                CloseClue();
            }

            GUI.color = oldColor;
        }
    }
}
