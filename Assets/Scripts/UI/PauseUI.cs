using UnityEngine;
using EscapeRoom.Core;

namespace EscapeRoom.UI
{
    /// <summary>
    /// Screen-space Pause Menu UI rendering RESUME, RESTART, and MAIN MENU options.
    /// Operates during Time.timeScale = 0f.
    /// </summary>
    [DisallowMultipleComponent]
    public class PauseUI : MonoBehaviour
    {
        private static PauseUI instance;
        public static PauseUI Instance => instance;

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

        public void ShowPauseMenu()
        {
            isOpen = true;
        }

        public void HidePauseMenu()
        {
            isOpen = false;
        }

        private void OnResumeClicked()
        {
            if (PauseManager.Instance != null)
            {
                PauseManager.Instance.ResumeGame();
            }
        }

        private void OnRestartClicked()
        {
            if (PauseManager.Instance != null)
            {
                PauseManager.Instance.RestartGame();
            }
            else if (GameManager.Instance != null)
            {
                GameManager.Instance.RestartGame();
            }
        }

        private void OnMainMenuClicked()
        {
            if (PauseManager.Instance != null)
            {
                PauseManager.Instance.ResumeGame();
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.ReturnToMainMenu();
            }
        }

        private void OnGUI()
        {
            if (!isOpen) return;

            // Translucent dark background overlay
            Color oldColor = GUI.color;
            GUI.color = new Color(0.04f, 0.05f, 0.08f, 0.75f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);

            float panelWidth = 320f;
            float panelHeight = 280f;
            float panelX = (Screen.width - panelWidth) * 0.5f;
            float panelY = (Screen.height - panelHeight) * 0.5f;

            // Frame background
            GUI.color = new Color(0.12f, 0.14f, 0.18f, 0.96f);
            GUI.Box(new Rect(panelX, panelY, panelWidth, panelHeight), GUIContent.none);

            // Border accent
            GUI.color = new Color(0.35f, 0.55f, 0.85f, 0.85f);
            GUI.Box(new Rect(panelX + 4, panelY + 4, panelWidth - 8, panelHeight - 8), GUIContent.none);

            // Inner background
            GUI.color = new Color(0.15f, 0.17f, 0.22f, 0.98f);
            GUI.Box(new Rect(panelX + 6, panelY + 6, panelWidth - 12, panelHeight - 12), GUIContent.none);

            // Header title
            GUI.color = Color.white;
            GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 24,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.95f, 0.95f, 0.95f) }
            };
            GUI.Label(new Rect(panelX, panelY + 24f, panelWidth, 36), "PAUSED", titleStyle);

            // Buttons
            float btnW = 200f;
            float btnH = 40f;
            float btnX = panelX + (panelWidth - btnW) * 0.5f;
            float startY = panelY + 80f;
            float spacing = 50f;

            GUIStyle btnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };

            // RESUME
            GUI.backgroundColor = new Color(0.20f, 0.65f, 0.35f);
            if (GUI.Button(new Rect(btnX, startY, btnW, btnH), "RESUME", btnStyle))
            {
                OnResumeClicked();
            }

            // RESTART
            GUI.backgroundColor = new Color(0.35f, 0.40f, 0.50f);
            if (GUI.Button(new Rect(btnX, startY + spacing, btnW, btnH), "RESTART", btnStyle))
            {
                OnRestartClicked();
            }

            // MAIN MENU
            GUI.backgroundColor = new Color(0.30f, 0.32f, 0.38f);
            if (GUI.Button(new Rect(btnX, startY + spacing * 2f, btnW, btnH), "MAIN MENU", btnStyle))
            {
                OnMainMenuClicked();
            }

            GUI.backgroundColor = Color.white;
            GUI.color = oldColor;
        }
    }
}
