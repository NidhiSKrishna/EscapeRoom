using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace EscapeRoom.UI
{
    /// <summary>
    /// Handles Main Menu UI with 'WELCOME TO ESCAPE ROOM 1', 'PLAY', and 'QUIT' buttons.
    /// Strictly excludes any Continue button as required by design specification.
    /// PLAY launches PhoneIntroUI before advancing to Attic Briefing.
    /// </summary>
    [DisallowMultipleComponent]
    public class MainMenuUI : MonoBehaviour
    {
        private static MainMenuUI instance;
        public static MainMenuUI Instance => instance;

        [Header("References")]
        [SerializeField] private PhoneIntroUI phoneIntroUI;

        [Header("Menu State")]
        [SerializeField] private bool isMenuVisible = true;

        public bool IsMenuVisible => isMenuVisible;

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

        private void Start()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (phoneIntroUI == null)
            {
                phoneIntroUI = FindAnyObjectByType<PhoneIntroUI>();
            }

            if (phoneIntroUI == null)
            {
                var go = new GameObject("PhoneIntroUI");
                phoneIntroUI = go.AddComponent<PhoneIntroUI>();
            }
        }

        public void OnPlayClicked()
        {
            Debug.Log("[MainMenuUI] PLAY clicked. Opening Phone Intro...");
            isMenuVisible = false;

            if (phoneIntroUI != null)
            {
                phoneIntroUI.OpenIntro();
            }
        }

        public void OnQuitClicked()
        {
            Debug.Log("[MainMenuUI] QUIT requested.");
#if UNITY_EDITOR
            Debug.Log("<color=#f0ad4e>[MainMenuUI]</color> Quit requested in Editor Play Mode.");
#else
            Application.Quit();
#endif
        }

        private void OnGUI()
        {
            if (!isMenuVisible) return;

            Color oldColor = GUI.color;

            // Full screen moody dark atmospheric background
            GUI.color = new Color(0.04f, 0.05f, 0.07f, 1f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);

            // Centered menu panel
            float panelWidth = Mathf.Min(500f, Screen.width * 0.9f);
            float panelHeight = 360f;
            float panelX = (Screen.width - panelWidth) * 0.5f;
            float panelY = (Screen.height - panelHeight) * 0.5f;

            // Card frame
            GUI.color = new Color(0.10f, 0.12f, 0.16f, 0.95f);
            GUI.Box(new Rect(panelX, panelY, panelWidth, panelHeight), GUIContent.none);

            // Border accent
            GUI.color = new Color(0.96f, 0.78f, 0.20f, 0.90f);
            GUI.Box(new Rect(panelX + 3, panelY + 3, panelWidth - 6, panelHeight - 6), GUIContent.none);

            // Inner fill
            GUI.color = new Color(0.12f, 0.14f, 0.18f, 1f);
            GUI.Box(new Rect(panelX + 5, panelY + 5, panelWidth - 10, panelHeight - 10), GUIContent.none);

            GUI.color = Color.white;

            // Subtitle: WELCOME TO
            var subTitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.75f, 0.80f, 0.88f) }
            };
            GUI.Label(new Rect(panelX + 20, panelY + 32, panelWidth - 40, 24), "WELCOME TO", subTitleStyle);

            // Main Title: ESCAPE ROOM 1
            var titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 32,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.98f, 0.85f, 0.35f) }
            };
            GUI.Label(new Rect(panelX + 20, panelY + 60, panelWidth - 40, 48), "ESCAPE ROOM 1", titleStyle);

            // Divider line
            GUI.color = new Color(0.96f, 0.78f, 0.20f, 0.40f);
            GUI.DrawTexture(new Rect(panelX + 50, panelY + 120, panelWidth - 100, 2), Texture2D.whiteTexture);
            GUI.color = Color.white;

            // PLAY Button
            float btnW = 240f;
            float btnH = 50f;
            float btnX = panelX + (panelWidth - btnW) * 0.5f;
            float btnY = panelY + 155f;

            var playStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.black }
            };

            GUI.backgroundColor = new Color(0.98f, 0.82f, 0.25f, 1f);
            if (GUI.Button(new Rect(btnX, btnY, btnW, btnH), "PLAY", playStyle))
            {
                OnPlayClicked();
            }

            // QUIT Button
            float quitY = btnY + 70f;
            var quitStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 15,
                normal = { textColor = new Color(0.85f, 0.88f, 0.92f) }
            };

            GUI.backgroundColor = new Color(0.24f, 0.26f, 0.32f, 1f);
            if (GUI.Button(new Rect(btnX, quitY, btnW, 42f), "QUIT", quitStyle))
            {
                OnQuitClicked();
            }

            GUI.backgroundColor = Color.white;
            GUI.color = oldColor;
        }
    }
}
