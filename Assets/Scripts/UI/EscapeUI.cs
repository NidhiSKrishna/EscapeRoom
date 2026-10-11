using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using EscapeRoom.Core;
using EscapeRoom.Player;

namespace EscapeRoom.UI
{
    /// <summary>
    /// Full-screen escape celebration and stage result modal.
    /// Displays 'STAGE 1 CLEARED!' with 'CONTINUE TO STAGE 2', 'REPLAY STAGE 1', and 'MAIN MENU'.
    /// Fully responsive to mouse clicks (GUI.Button and direct MouseDown) and keyboard hotkeys.
    /// Hotkeys: [Enter]/[Space]/[C] = Continue, [R] = Replay Stage 1, [M]/[Esc] = Main Menu.
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

            // Ensure mouse cursor remains unlocked while modal is open
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            bool continuePressed = false;
            bool replayPressed = false;
            bool menuPressed = false;

            // 1. New Input System Keyboard Check
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame ||
                    keyboard.spaceKey.wasPressedThisFrame || keyboard.cKey.wasPressedThisFrame ||
                    keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame)
                {
                    continuePressed = true;
                }
                else if (keyboard.rKey.wasPressedThisFrame || keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame)
                {
                    replayPressed = true;
                }
                else if (keyboard.mKey.wasPressedThisFrame || keyboard.escapeKey.wasPressedThisFrame)
                {
                    menuPressed = true;
                }
            }

            // 2. Legacy Input Fallback Check
            if (!continuePressed && !replayPressed && !menuPressed)
            {
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) ||
                    Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.C) ||
                    Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
                {
                    continuePressed = true;
                }
                else if (Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
                {
                    replayPressed = true;
                }
                else if (Input.GetKeyDown(KeyCode.M) || Input.GetKeyDown(KeyCode.Escape))
                {
                    menuPressed = true;
                }
            }

            if (continuePressed)
            {
                OnContinueStage2Clicked();
            }
            else if (replayPressed)
            {
                OnPlayAgainClicked();
            }
            else if (menuPressed)
            {
                OnMainMenuClicked();
            }
        }

        /// <summary>
        /// Displays the escape result screen, disables player movement/camera, and unlocks the cursor.
        /// </summary>
        public void ShowEscapeScreen()
        {
            isOpen = true;
            Time.timeScale = 0f;

            // Unlock mouse cursor
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            var look = FindAnyObjectByType<PlayerLook>();
            if (look != null)
            {
                look.SetCursorLock(false);
                look.enabled = false;
            }

            var controller = FindAnyObjectByType<PlayerController>();
            if (controller != null)
            {
                controller.enabled = false;
            }

            Debug.Log("<color=#5cb85c><b>[EscapeUI]</b></color> Escape screen displayed. Cursor unlocked, player look/movement disabled.");
        }

        public void OnContinueStage2Clicked()
        {
            Debug.Log("<color=#5cb85c><b>[EscapeUI]</b></color> CONTINUE TO STAGE 2 triggered! Unlocking Stage 2 and loading scene...");
            GameProgress.SetStageUnlocked(2);
            Time.timeScale = 1f;
            isOpen = false;

            var look = FindAnyObjectByType<PlayerLook>();
            if (look != null) look.enabled = true;
            var controller = FindAnyObjectByType<PlayerController>();
            if (controller != null) controller.enabled = true;

            string targetScene = GameProgress.GetStageSceneName(2);
            if (Application.CanStreamedLevelBeLoaded(targetScene))
            {
                SceneManager.LoadScene(targetScene);
            }
            else
            {
                bool loaded = false;
                for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
                {
                    string path = SceneUtility.GetScenePathByBuildIndex(i);
                    if (path.IndexOf("StorageRoom", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        SceneManager.LoadScene(i);
                        loaded = true;
                        break;
                    }
                }
                if (!loaded)
                {
                    SceneManager.LoadScene("StorageRoom");
                }
            }
        }

        public void OnPlayAgainClicked()
        {
            Debug.Log("[EscapeUI] REPLAY STAGE 1 triggered.");
            Time.timeScale = 1f;
            isOpen = false;

            var look = FindAnyObjectByType<PlayerLook>();
            if (look != null) look.enabled = true;
            var controller = FindAnyObjectByType<PlayerController>();
            if (controller != null) controller.enabled = true;

            if (GameManager.Instance != null)
            {
                GameManager.Instance.RestartGame();
            }
            else
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }
        }

        public void OnMainMenuClicked()
        {
            Debug.Log("[EscapeUI] MAIN MENU triggered. Saving Stage 2 unlock and returning to Main Menu...");
            GameProgress.SetStageUnlocked(2);
            Time.timeScale = 1f;
            isOpen = false;

            var look = FindAnyObjectByType<PlayerLook>();
            if (look != null) look.enabled = true;
            var controller = FindAnyObjectByType<PlayerController>();
            if (controller != null) controller.enabled = true;

            if (GameManager.Instance != null)
            {
                GameManager.Instance.ReturnToMainMenu();
            }
            else if (Application.CanStreamedLevelBeLoaded("MainMenu"))
            {
                SceneManager.LoadScene("MainMenu");
            }
            else
            {
                SceneManager.LoadScene(0);
            }
        }

        private void OnGUI()
        {
            if (!isOpen) return;

            // Highest priority depth so clicks and draws are top of stack
            GUI.depth = -100;

            Color oldColor = GUI.color;

            // Full-screen dark overlay
            GUI.color = new Color(0.04f, 0.05f, 0.08f, 0.92f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);

            float panelWidth = Mathf.Min(540f, Screen.width * 0.92f);
            float panelHeight = 370f;
            float panelX = (Screen.width - panelWidth) * 0.5f;
            float panelY = (Screen.height - panelHeight) * 0.5f;

            // Inner card
            GUI.color = new Color(0.10f, 0.12f, 0.16f, 0.98f);
            GUI.Box(new Rect(panelX, panelY, panelWidth, panelHeight), GUIContent.none);

            // Border accent (gold)
            GUI.color = new Color(0.96f, 0.78f, 0.20f, 0.90f);
            GUI.Box(new Rect(panelX + 4, panelY + 4, panelWidth - 8, panelHeight - 8), GUIContent.none);

            // Card background fill
            GUI.color = new Color(0.14f, 0.16f, 0.20f, 0.98f);
            GUI.Box(new Rect(panelX + 6, panelY + 6, panelWidth - 12, panelHeight - 12), GUIContent.none);

            // Title "STAGE 1 CLEARED!"
            GUI.color = Color.white;
            GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 30,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.98f, 0.85f, 0.25f) }
            };
            GUI.Label(new Rect(panelX, panelY + 28f, panelWidth, 42), "STAGE 1 CLEARED!", titleStyle);

            // Subtitle
            GUIStyle subStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.85f, 0.90f, 0.95f) }
            };
            GUI.Label(new Rect(panelX + 20, panelY + 76f, panelWidth - 40, 36),
                "You solved the Attic and unlocked the emergency descent into Sector B.", subStyle);

            // Divider line
            GUI.color = new Color(0.96f, 0.78f, 0.20f, 0.5f);
            GUI.DrawTexture(new Rect(panelX + 40, panelY + 120f, panelWidth - 80, 2), Texture2D.whiteTexture);

            // Button Rectangles
            float btnW = 280f;
            float btnH = 46f;
            float btnX = panelX + (panelWidth - btnW) * 0.5f;

            Rect continueBtnRect = new Rect(btnX, panelY + 138f, btnW, btnH);
            Rect replayBtnRect = new Rect(btnX, panelY + 196f, btnW, 40f);
            Rect menuBtnRect = new Rect(btnX, panelY + 248f, btnW, 40f);

            // Reset GUI color before buttons
            GUI.color = Color.white;

            // 1. CONTINUE TO STAGE 2
            GUIStyle stage2BtnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.black }
            };
            GUI.backgroundColor = new Color(0.25f, 0.88f, 0.45f);
            if (GUI.Button(continueBtnRect, "CONTINUE TO STAGE 2 ► [ENTER / C]", stage2BtnStyle))
            {
                OnContinueStage2Clicked();
            }

            // 2. PLAY AGAIN / REPLAY
            GUIStyle playAgainStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };
            GUI.backgroundColor = new Color(0.20f, 0.60f, 0.80f);
            if (GUI.Button(replayBtnRect, "REPLAY STAGE 1 [R]", playAgainStyle))
            {
                OnPlayAgainClicked();
            }

            // 3. MAIN MENU
            GUI.backgroundColor = new Color(0.32f, 0.35f, 0.42f);
            GUIStyle menuStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };
            if (GUI.Button(menuBtnRect, "MAIN MENU [M / ESC]", menuStyle))
            {
                OnMainMenuClicked();
            }

            // 4. DIRECT MOUSE CLICK FALLBACK
            Event e = Event.current;
            if (e != null && e.type == EventType.MouseDown && e.button == 0)
            {
                if (continueBtnRect.Contains(e.mousePosition))
                {
                    OnContinueStage2Clicked();
                    e.Use();
                }
                else if (replayBtnRect.Contains(e.mousePosition))
                {
                    OnPlayAgainClicked();
                    e.Use();
                }
                else if (menuBtnRect.Contains(e.mousePosition))
                {
                    OnMainMenuClicked();
                    e.Use();
                }
            }

            GUI.backgroundColor = Color.white;
            GUI.color = oldColor;
        }
    }
}
