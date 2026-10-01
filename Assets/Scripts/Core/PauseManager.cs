using System;
using UnityEngine;
using UnityEngine.InputSystem;
using EscapeRoom.UI;
using EscapeRoom.Interaction;
using EscapeRoom.Player;

namespace EscapeRoom.Core
{
    /// <summary>
    /// Manages game pause state and prioritizes Escape key input.
    /// Priority order:
    /// 1. If Keypad modal is open -> Close Keypad
    /// 2. If Clue modal is open -> Close Clue
    /// 3. Otherwise -> Toggle Pause/Resume
    /// </summary>
    [DisallowMultipleComponent]
    public class PauseManager : MonoBehaviour
    {
        private static PauseManager instance;
        public static PauseManager Instance => instance;

        [Header("State")]
        [SerializeField] private bool isPaused = false;

        public event Action<bool> OnPauseStateChanged;

        public bool IsPaused => isPaused;

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
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                HandleEscapeKeyPress();
            }
        }

        /// <summary>
        /// Evaluates priority when Escape is pressed.
        /// </summary>
        public void HandleEscapeKeyPress()
        {
            // Priority 1: Keypad modal is open -> let Keypad close, do not pause
            if (KeypadUI.Instance != null && KeypadUI.Instance.IsOpen)
            {
                return;
            }

            // Priority 2: Clue modal is open -> let Clue close, do not pause
            if (ClueInteractable.IsAnyClueOpen)
            {
                return;
            }

            // Priority 3: Briefing or Intro is open -> do not pause or bypass with Escape
            if (AtticBriefingUI.IsBriefingOpen || PhoneIntroUI.IsIntroOpen)
            {
                return;
            }

            // Priority 4: Expanded Objective view is open -> close it, do not pause
            if (ObjectiveHUD.IsExpandedViewOpen && ObjectiveHUD.Instance != null)
            {
                ObjectiveHUD.Instance.CloseExpandedView();
                return;
            }

            // Priority 5: Game already completed (escaped) -> do not pause
            if (GameManager.Instance != null && GameManager.Instance.HasEscaped)
            {
                return;
            }

            // Priority 6: Toggle pause state
            TogglePause();
        }

        public void TogglePause()
        {
            if (isPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }

        public void PauseGame()
        {
            if (isPaused) return;
            isPaused = true;
            Time.timeScale = 0f;

            // Unlock mouse cursor for menu navigation
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            var playerLook = FindAnyObjectByType<PlayerLook>();
            if (playerLook != null)
            {
                playerLook.SetCursorLock(false);
            }

            if (PauseUI.Instance != null)
            {
                PauseUI.Instance.ShowPauseMenu();
            }

            Debug.Log("[PauseManager] Game paused.");
            OnPauseStateChanged?.Invoke(true);
        }

        public void ResumeGame()
        {
            if (!isPaused) return;
            isPaused = false;
            Time.timeScale = 1f;

            if (PauseUI.Instance != null)
            {
                PauseUI.Instance.HidePauseMenu();
            }

            // Lock cursor back to gameplay
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            var playerLook = FindAnyObjectByType<PlayerLook>();
            if (playerLook != null)
            {
                playerLook.SetCursorLock(true);
            }

            Debug.Log("[PauseManager] Game resumed.");
            OnPauseStateChanged?.Invoke(false);
        }

        public void RestartGame()
        {
            ResumeGame();
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
    }
}
