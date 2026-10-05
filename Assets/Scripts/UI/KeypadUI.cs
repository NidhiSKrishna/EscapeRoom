using UnityEngine;
using UnityEngine.InputSystem;
using EscapeRoom.Player;
using EscapeRoom.Puzzle;

namespace EscapeRoom.UI
{
    /// <summary>
    /// Interactive on-screen numpad terminal modal for the Keypad system.
    /// Provides 0-9 numeric buttons, Clear, Enter, and Close.
    /// Supports direct keyboard input (0-9, Backspace, Enter, Esc) as well as mouse clicks.
    /// Manages cursor lock/unlock and rejects input when UI is closed.
    /// </summary>
    [DisallowMultipleComponent]
    public class KeypadUI : MonoBehaviour
    {
        private static KeypadUI instance;
        public static KeypadUI Instance => instance;

        private KeypadController activeKeypad;
        private string currentInput = "";
        private bool isOpen = false;
        private string statusMessage = "ENTER PASSCODE";
        private Color statusColor = Color.cyan;
        private float statusResetTimer = 0f;
        private PlayerLook cachedPlayerLook;

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

        private void Update()
        {
            if (!isOpen) return;

            if (statusResetTimer > 0f)
            {
                statusResetTimer -= Time.deltaTime;
                if (statusResetTimer <= 0f)
                {
                    statusMessage = "ENTER PASSCODE";
                    statusColor = Color.cyan;
                }
            }

            HandleKeyboardInput();
        }

        public void Open(KeypadController keypad)
        {
            activeKeypad = keypad;
            currentInput = "";
            isOpen = true;
            statusMessage = "ENTER PASSCODE";
            statusColor = Color.cyan;
            statusResetTimer = 0f;

            // Unlock cursor
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

            Debug.Log($"<color=#337ab7><b>[KeypadUI]</b></color> Opened keypad UI for '{keypad.gameObject.name}'.");
        }

        public void Close()
        {
            if (!isOpen) return;
            isOpen = false;
            activeKeypad = null;
            currentInput = "";

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

            Debug.Log("[KeypadUI] Closed keypad UI.");
        }

        private void HandleKeyboardInput()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                Close();
                return;
            }

            if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame)
            {
                PressEnter();
                return;
            }

            if (keyboard.backspaceKey.wasPressedThisFrame)
            {
                PressClear();
                return;
            }

            // Numeric keys
            if (keyboard.digit0Key.wasPressedThisFrame || keyboard.numpad0Key.wasPressedThisFrame) PressDigit("0");
            else if (keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame) PressDigit("1");
            else if (keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame) PressDigit("2");
            else if (keyboard.digit3Key.wasPressedThisFrame || keyboard.numpad3Key.wasPressedThisFrame) PressDigit("3");
            else if (keyboard.digit4Key.wasPressedThisFrame || keyboard.numpad4Key.wasPressedThisFrame) PressDigit("4");
            else if (keyboard.digit5Key.wasPressedThisFrame || keyboard.numpad5Key.wasPressedThisFrame) PressDigit("5");
            else if (keyboard.digit6Key.wasPressedThisFrame || keyboard.numpad6Key.wasPressedThisFrame) PressDigit("6");
            else if (keyboard.digit7Key.wasPressedThisFrame || keyboard.numpad7Key.wasPressedThisFrame) PressDigit("7");
            else if (keyboard.digit8Key.wasPressedThisFrame || keyboard.numpad8Key.wasPressedThisFrame) PressDigit("8");
            else if (keyboard.digit9Key.wasPressedThisFrame || keyboard.numpad9Key.wasPressedThisFrame) PressDigit("9");
        }

        public void PressDigit(string digit)
        {
            if (!isOpen || activeKeypad == null) return;

            int maxLen = activeKeypad.MaxCodeLength;
            if (currentInput.Length < maxLen)
            {
                currentInput += digit;
                // ── Digit beep ───────────────────────────────────────────────
                EscapeRoom.Audio.EscapeRoomAudio.Play(
                    EscapeRoom.Audio.EscapeRoomAudio.SoundId.KeypadDigit, 0.8f);
            }
        }

        public void PressClear()
        {
            if (!isOpen) return;
            currentInput = "";
        }

        public void PressEnter()
        {
            if (!isOpen || activeKeypad == null) return;

            if (string.IsNullOrEmpty(currentInput))
            {
                statusMessage = "CODE EMPTY";
                statusColor = Color.yellow;
                statusResetTimer = 1.5f;
                return;
            }

            bool accepted = activeKeypad.SubmitCode(currentInput);
            if (accepted)
            {
                // ── Correct code sound ────────────────────────────────────────
                EscapeRoom.Audio.EscapeRoomAudio.Play(
                    EscapeRoom.Audio.EscapeRoomAudio.SoundId.KeypadCorrect);

                statusMessage = "ACCESS GRANTED";
                statusColor = new Color(0.3f, 1f, 0.4f);
                statusResetTimer = 2.0f;
                // Auto close UI after successful entry
                Close();
            }
            else
            {
                // ── Wrong code sound ──────────────────────────────────────────
                EscapeRoom.Audio.EscapeRoomAudio.Play(
                    EscapeRoom.Audio.EscapeRoomAudio.SoundId.KeypadWrong);

                statusMessage = "ACCESS DENIED";
                statusColor = new Color(1f, 0.3f, 0.3f);
                statusResetTimer = 1.5f;
                currentInput = "";
            }
        }

        private void OnGUI()
        {
            if (!isOpen || Event.current.type != EventType.Repaint && Event.current.type != EventType.MouseDown && Event.current.type != EventType.MouseUp)
                return;

            float panelWidth = 320f;
            float panelHeight = 440f;
            float panelX = (Screen.width - panelWidth) * 0.5f;
            float panelY = (Screen.height - panelHeight) * 0.5f;

            // Semi-transparent backdrop
            Color oldColor = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.65f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);

            // Keypad outer case
            GUI.color = new Color(0.12f, 0.13f, 0.15f, 0.98f);
            GUI.Box(new Rect(panelX, panelY, panelWidth, panelHeight), GUIContent.none);

            // Screen frame
            GUI.color = new Color(0.04f, 0.05f, 0.07f, 1f);
            GUI.Box(new Rect(panelX + 20, panelY + 20, panelWidth - 40, 75), GUIContent.none);

            // Screen digital text (Status + Digits)
            GUI.color = statusColor;
            GUIStyle statusStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperCenter,
                normal = { textColor = statusColor }
            };
            GUI.Label(new Rect(panelX + 25, panelY + 24, panelWidth - 50, 20), statusMessage, statusStyle);

            GUI.color = Color.white;
            string displayDigits = currentInput;
            int maxLen = activeKeypad != null ? activeKeypad.MaxCodeLength : 4;
            while (displayDigits.Length < maxLen)
            {
                displayDigits += "_ ";
            }

            GUIStyle digitStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 24,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };
            GUI.Label(new Rect(panelX + 25, panelY + 44, panelWidth - 50, 45), displayDigits, digitStyle);

            // Grid buttons (1-9, Clear, 0, Enter)
            string[] buttons = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "CLR", "0", "ENT" };
            float startX = panelX + 24f;
            float startY = panelY + 110f;
            float btnW = 82f;
            float btnH = 55f;
            float gapX = 12f;
            float gapY = 10f;

            GUIStyle btnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };

            for (int i = 0; i < buttons.Length; i++)
            {
                int row = i / 3;
                int col = i % 3;
                float bx = startX + col * (btnW + gapX);
                float by = startY + row * (btnH + gapY);

                string label = buttons[i];
                if (label == "ENT")
                {
                    GUI.backgroundColor = new Color(0.2f, 0.7f, 0.3f);
                }
                else if (label == "CLR")
                {
                    GUI.backgroundColor = new Color(0.8f, 0.3f, 0.2f);
                }
                else
                {
                    GUI.backgroundColor = new Color(0.35f, 0.38f, 0.42f);
                }

                if (GUI.Button(new Rect(bx, by, btnW, btnH), label, btnStyle))
                {
                    if (label == "CLR") PressClear();
                    else if (label == "ENT") PressEnter();
                    else PressDigit(label);
                }
            }

            // Close button at bottom
            GUI.backgroundColor = new Color(0.25f, 0.25f, 0.28f);
            GUIStyle closeStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };

            if (GUI.Button(new Rect(panelX + 35, panelY + panelHeight - 48, panelWidth - 70, 32), "Close (ESC)", closeStyle))
            {
                Close();
            }

            GUI.backgroundColor = Color.white;
            GUI.color = oldColor;
        }
    }
}
