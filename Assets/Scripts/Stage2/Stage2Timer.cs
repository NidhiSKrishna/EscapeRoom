using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using EscapeRoom.Core;
using EscapeRoom.UI;

namespace EscapeRoom.Stage2
{
    /// <summary>
    /// Independent 9-minute room timer for Stage 2 (Storage Room).
    /// Displays a sleek countdown timer on the HUD, pauses during modal/pause screens,
    /// turns warning amber/red as time wanes, and provides a clean stage reset upon expiration.
    /// </summary>
    [DisallowMultipleComponent]
    public class Stage2Timer : MonoBehaviour
    {
        private static Stage2Timer instance;
        public static Stage2Timer Instance => instance;

        [Header("Timer Parameters")]
        [Tooltip("Total stage time limit in seconds (9 minutes = 540 seconds).")]
        [SerializeField] private float totalTimeSeconds = 540.0f; // 9 minutes

        [Header("State")]
        [SerializeField] private float remainingTime;
        [SerializeField] private bool isRunning = true;
        [SerializeField] private bool isExpired = false;

        public float RemainingTime => remainingTime;
        public float TotalTimeSeconds => totalTimeSeconds;
        public bool IsRunning => isRunning;
        public bool IsExpired => isExpired;

        public static event Action OnTimerExpired;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            ResetTimer();
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        public void ResetTimer()
        {
            remainingTime = totalTimeSeconds;
            isRunning = true;
            isExpired = false;
        }

        public void StopTimer()
        {
            isRunning = false;
        }

        private void Update()
        {
            if (!isRunning || isExpired) return;

            // Pause if game is paused or modals are active
            if (Time.timeScale <= 0f) return;
            if (PauseManager.Instance != null && PauseManager.Instance.IsPaused) return;
            if (Stage2ExitTrigger.IsStage2Complete) return;

            remainingTime -= Time.deltaTime;

            if (remainingTime <= 0f)
            {
                remainingTime = 0f;
                isExpired = true;
                isRunning = false;
                HandleTimerExpired();
            }
        }

        private void HandleTimerExpired()
        {
            FeedbackHUD.ShowMessage("STAGE 2 TIME LIMIT EXPIRED! Resetting stage...", new Color(0.95f, 0.35f, 0.35f));
            Stage2Audio.Instance?.PlayWrongAnswer();
            OnTimerExpired?.Invoke();

            // Clean scene reload after brief delay
            Invoke(nameof(ReloadStage), 2.5f);
        }

        private void ReloadStage()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void OnGUI()
        {
            if (Stage2ExitTrigger.IsStage2Complete) return;
            if (PauseManager.Instance != null && PauseManager.Instance.IsPaused) return;

            // Compact timer HUD widget at top-right
            float boxW = 160f;
            float boxH = 34f;
            float boxX = Screen.width - boxW - 20f;
            float boxY = 20f;

            Color oldColor = GUI.color;

            // Dark semi-transparent backdrop
            GUI.color = new Color(0.08f, 0.10f, 0.14f, 0.85f);
            GUI.Box(new Rect(boxX, boxY, boxW, boxH), GUIContent.none);

            // Determine text color based on urgency: normal is white #FFFFFF
            Color textColor = Color.white;
            if (remainingTime < 60f)
            {
                textColor = new Color(0.95f, 0.35f, 0.35f); // Red (< 1 min)
            }
            else if (remainingTime < 180f)
            {
                textColor = new Color(0.98f, 0.75f, 0.25f); // Amber (< 3 min)
            }

            int minutes = Mathf.FloorToInt(remainingTime / 60f);
            int seconds = Mathf.FloorToInt(remainingTime % 60f);

            var timerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = textColor }
            };

            GUI.color = Color.white;
            GUI.Label(new Rect(boxX, boxY, boxW, boxH), $"TIME: {minutes:D2}:{seconds:D2}", timerStyle);

            GUI.color = oldColor;
        }
    }
}
