using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using EscapeRoom.Core;
using EscapeRoom.Player;
using EscapeRoom.UI;

namespace EscapeRoom.Stage2
{
    /// <summary>
    /// Trigger zone placed at the Storage Room exit corridor.
    /// Stepping into the trigger concludes Stage 2, displays the victory/celebration modal,
    /// stops the stage timer, plays the triumphant fanfare, and provides the CompleteStage2()
    /// transition method ready for team integration.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public class Stage2ExitTrigger : MonoBehaviour
    {
        private static Stage2ExitTrigger instance;
        public static Stage2ExitTrigger Instance => instance;

        [Header("Next Stage Integration")]
        [Tooltip("The name of the next stage scene (e.g. Stage3, Cellar) for future integration.")]
        [SerializeField] private string nextSceneName = "";

        [Header("State")]
        private static bool isStage2Complete = false;
        private float completionTime = 0f;
        private string completionRating = "S-RANK";

        public static bool IsStage2Complete => isStage2Complete;
        public float CompletionTime => completionTime;
        public string CompletionRating => completionRating;

        public static event Action OnStage2Completed;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            isStage2Complete = false;

            var col = GetComponent<Collider>();
            if (col != null)
            {
                col.isTrigger = true;
            }
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (isStage2Complete) return;

            // Only player triggers exit
            if (other.GetComponent<PlayerController>() != null || other.CompareTag("Player") || other.GetComponentInParent<PlayerController>() != null)
            {
                TriggerStage2Complete();
            }
        }

        public void TriggerStage2Complete()
        {
            if (isStage2Complete) return;

            isStage2Complete = true;

            // Complete final objective step
            EscapeRoom.Core.ObjectiveManager.Instance?.CompleteFlag("STAGE2_COMPLETE");

            // Stop Timer
            var timer = Stage2Timer.Instance;
            if (timer != null)
            {
                timer.StopTimer();
                completionTime = timer.TotalTimeSeconds - timer.RemainingTime;
            }
            else
            {
                completionTime = Time.timeSinceLevelLoad;
            }

            // Calculate Rating
            if (completionTime < 180f)
                completionRating = "EXEMPLARY [S-RANK]";
            else if (completionTime < 360f)
                completionRating = "PROFICIENT [A-RANK]";
            else
                completionRating = "QUALIFIED [B-RANK]";

            // Disarm siren if still active
            Stage2SecurityAlarmEvent.Instance?.DisarmAlarm();

            // Play completion audio
            Stage2Audio.Instance?.PlayStageComplete();

            // Unlock mouse cursor for completion UI
            var look = FindAnyObjectByType<PlayerLook>();
            if (look != null)
            {
                look.SetCursorLock(false);
            }

            OnStage2Completed?.Invoke();
            Debug.Log($"<color=#5cb85c><b>[Stage2ExitTrigger]</b></color> STAGE 2 COMPLETE! Time: {completionTime:F1}s | Rating: {completionRating}");
        }

        /// <summary>
        /// Transition method for integration into the main project.
        /// Loads nextSceneName if configured, otherwise restarts stage or notifies GameManager.
        /// </summary>
        public void CompleteStage2()
        {
            Debug.Log($"[Stage2ExitTrigger] CompleteStage2() invoked. Next scene target: '{nextSceneName}'.");

            if (!string.IsNullOrEmpty(nextSceneName) && Application.CanStreamedLevelBeLoaded(nextSceneName))
            {
                SceneManager.LoadScene(nextSceneName);
            }
            else
            {
                // Standalone reload
                var active = SceneManager.GetActiveScene();
                if (active.buildIndex >= 0)
                    SceneManager.LoadScene(active.buildIndex);
                else
                    SceneManager.LoadScene(active.name);
            }
        }

        private void OnGUI()
        {
            if (!isStage2Complete) return;

            float winW = Mathf.Min(560f, Screen.width * 0.90f);
            float winH = Mathf.Min(420f, Screen.height * 0.90f);
            float winX = (Screen.width - winW) * 0.5f;
            float winY = (Screen.height - winH) * 0.5f;

            Color oldColor = GUI.color;

            // Chassis
            GUI.color = new Color(0.06f, 0.08f, 0.12f, 0.96f);
            GUI.Box(new Rect(winX, winY, winW, winH), GUIContent.none);

            // Green/Cyan Border
            GUI.color = new Color(0.25f, 0.85f, 0.45f, 0.90f);
            GUI.Box(new Rect(winX + 4, winY + 4, winW - 8, winH - 8), GUIContent.none);

            // Banner
            GUI.color = new Color(0.10f, 0.22f, 0.14f, 1f);
            GUI.Box(new Rect(winX + 6, winY + 6, winW - 12, 54f), GUIContent.none);

            // Title (White #FFFFFF)
            var titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };
            GUI.color = Color.white;
            GUI.Label(new Rect(winX + 10, winY + 12, winW - 20, 26f), "✔ STAGE 2: STORAGE ROOM COMPLETE", titleStyle);

            var subStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.75f, 0.95f, 0.80f) }
            };
            GUI.Label(new Rect(winX + 10, winY + 38, winW - 20, 20f), "SECTOR ACCESS BADGE ACQUIRED // EVACUATION PASSAGE OPEN", subStyle);

            float curY = winY + 75f;

            // Stats Card
            GUI.color = new Color(0.04f, 0.06f, 0.09f, 0.95f);
            GUI.Box(new Rect(winX + 24, curY, winW - 48, 175f), GUIContent.none);

            var statHeaderStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };
            GUI.color = Color.white;
            GUI.Label(new Rect(winX + 30, curY + 12, winW - 60, 22f), "MISSION DEBRIEFING", statHeaderStyle);

            int minutes = Mathf.FloorToInt(completionTime / 60f);
            int seconds = Mathf.FloorToInt(completionTime % 60f);

            var bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = Color.white }
            };

            GUI.Label(new Rect(winX + 45, curY + 44, winW - 90, 22f), $"• Clearance Time: {minutes:D2}:{seconds:D2}", bodyStyle);
            GUI.Label(new Rect(winX + 45, curY + 70, winW - 90, 22f), "• Pattern Logic Sequence: Verified", bodyStyle);
            GUI.Label(new Rect(winX + 45, curY + 96, winW - 90, 22f), "• Storage Container Vault: Unlocked", bodyStyle);
            GUI.Label(new Rect(winX + 45, curY + 122, winW - 90, 22f), "• Security Alarm: Disarmed at Reader", bodyStyle);

            var rankStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleRight,
                normal = { textColor = new Color(0.98f, 0.90f, 0.35f) }
            };
            GUI.Label(new Rect(winX + 45, curY + 144, winW - 90, 24f), $"EVALUATION: {completionRating}", rankStyle);

            curY += 195f;

            // Action Buttons
            float btnW = (winW - 60f) * 0.5f;
            float btnH = 44f;

            var actionBtnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            if (GUI.Button(new Rect(winX + 24, curY, btnW, btnH), "REPLAY STAGE 2", actionBtnStyle))
            {
                Stage2Audio.Instance?.PlayButtonPress();
                var active = SceneManager.GetActiveScene();
                if (active.buildIndex >= 0)
                    SceneManager.LoadScene(active.buildIndex);
                else
                    SceneManager.LoadScene(active.name);
            }

            string nextLabel = string.IsNullOrEmpty(nextSceneName) ? "CONTINUE [STAGE COMPLETE]" : "CONTINUE TO NEXT STAGE";
            if (GUI.Button(new Rect(winX + 36 + btnW, curY, btnW, btnH), nextLabel, actionBtnStyle))
            {
                Stage2Audio.Instance?.PlayButtonPress();
                CompleteStage2();
            }

            GUI.color = oldColor;
        }
    }
}
