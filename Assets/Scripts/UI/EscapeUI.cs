using UnityEngine;
using UnityEngine.InputSystem;
using EscapeRoom.Core;
using EscapeRoom.Player;

namespace EscapeRoom.UI
{
    /// <summary>
    /// Full-screen escape celebration and result screen.
    /// Displays 'YOU ESCAPED' banner with 'PLAY AGAIN' and 'MAIN MENU' actions.
    /// Unlocks cursor and disables player movement upon escape.
    /// </summary>
    [DisallowMultipleComponent]
    public class EscapeUI : MonoBehaviour
    {
        private static EscapeUI instance;
        public static EscapeUI Instance => instance;

        [Header("State")]
        [SerializeField] private bool isOpen = false;

        public bool IsOpen => isOpen;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        private void Update()
        {
            if (!isOpen) return;

            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.rKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame)
                {
                    OnPlayAgainClicked();
                }
            }
        }

        /// <summary>
        /// Displays the escape result screen and unlocks the mouse cursor.
        /// </summary>
        public void ShowEscapeScreen()
        {
            isOpen = true;

            // Unlock mouse cursor
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            var look = FindAnyObjectByType<PlayerLook>();
            if (look != null)
            {
                look.SetCursorLock(false);
            }

            Debug.Log("[EscapeUI] Escape screen displayed.");
        }

        private void OnPlayAgainClicked()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.RestartGame();
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(
                    UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
            }
        }

        private void OnMainMenuClicked()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ReturnToMainMenu();
            }
        }

        private void OnGUI()
        {
            if (!isOpen) return;

            // Full-screen dark overlay
            Color oldColor = GUI.color;
            GUI.color = new Color(0.04f, 0.05f, 0.08f, 0.90f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);

            float panelWidth = Mathf.Min(520f, Screen.width * 0.9f);
            float panelHeight = 320f;
            float panelX = (Screen.width - panelWidth) * 0.5f;
            float panelY = (Screen.height - panelHeight) * 0.5f;

            // Inner card
            GUI.color = new Color(0.10f, 0.12f, 0.16f, 0.95f);
            GUI.Box(new Rect(panelX, panelY, panelWidth, panelHeight), GUIContent.none);

            // Border accent (gold)
            GUI.color = new Color(0.96f, 0.78f, 0.20f, 0.90f);
            GUI.Box(new Rect(panelX + 4, panelY + 4, panelWidth - 8, panelHeight - 8), GUIContent.none);

            // Card background fill
            GUI.color = new Color(0.14f, 0.16f, 0.20f, 0.98f);
            GUI.Box(new Rect(panelX + 6, panelY + 6, panelWidth - 12, panelHeight - 12), GUIContent.none);

            // Title "YOU ESCAPED"
            GUI.color = Color.white;
            GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 32,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.98f, 0.85f, 0.25f) }
            };
            GUI.Label(new Rect(panelX, panelY + 35f, panelWidth, 48), "YOU ESCAPED", titleStyle);

            // Subtitle
            GUIStyle subStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.85f, 0.90f, 0.95f) }
            };
            GUI.Label(new Rect(panelX + 20, panelY + 95f, panelWidth - 40, 40),
                "Congratulations! You solved all the puzzles and unlocked the facility exit.", subStyle);

            // Divider line
            GUI.color = new Color(0.5f, 0.5f, 0.6f, 0.5f);
            GUI.DrawTexture(new Rect(panelX + 40, panelY + 145f, panelWidth - 80, 2), Texture2D.whiteTexture);

            // Buttons
            float btnW = 200f;
            float btnH = 44f;
            float btnX = panelX + (panelWidth - btnW) * 0.5f;

            GUIStyle playAgainStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };

            GUI.backgroundColor = new Color(0.20f, 0.70f, 0.35f);
            if (GUI.Button(new Rect(btnX, panelY + 170f, btnW, btnH), "PLAY AGAIN", playAgainStyle))
            {
                OnPlayAgainClicked();
            }

            GUI.backgroundColor = new Color(0.35f, 0.38f, 0.45f);
            GUIStyle menuStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            if (GUI.Button(new Rect(btnX, panelY + 230f, btnW, 36f), "MAIN MENU", menuStyle))
            {
                OnMainMenuClicked();
            }

            GUI.backgroundColor = Color.white;
            GUI.color = oldColor;
        }
    }
}
