using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using EscapeRoom.Core;
using EscapeRoom.UI;
using EscapeRoom.Player;

namespace EscapeRoom.Stage3
{
    [RequireComponent(typeof(Collider))]
    public class Stage3ExitTrigger : MonoBehaviour
    {
        [Header("Exit Configuration")]
        [Tooltip("The name of the next scene to load. Leave empty to just show completion.")]
        [SerializeField] private string nextSceneName = "";

        [Tooltip("Exit must be explicitly unlocked by SecurityRoomManager.")]
        [SerializeField] private bool requireAllPuzzlesSolved = true;

        public bool HasTriggered { get; private set; }

        // IMPORTANT:
        // Exit starts LOCKED.
        private bool exitUnlocked = false;

        public event System.Action OnStageComplete;

        private void Awake()
        {
            Collider col = GetComponent<Collider>();

            if (col != null)
            {
                col.isTrigger = true;
            }

            exitUnlocked = false;
            HasTriggered = false;
        }

        private void Reset()
        {
            Collider col = GetComponent<Collider>();

            if (col != null)
            {
                col.isTrigger = true;
            }
        }

        /// <summary>
        /// Called by SecurityRoomManager ONLY after the final Stage 3 puzzle
        /// has been successfully completed.
        /// </summary>
        public void UnlockExit()
        {
            exitUnlocked = true;

            Debug.Log(
                "<color=#00ff88><b>[Stage3ExitTrigger]</b></color> " +
                "EXIT UNLOCKED - all Stage 3 objectives completed."
            );

            FeedbackHUD.ShowMessage(
                "All security systems cleared! Exit unlocked.",
                new Color(0.40f, 0.95f, 0.45f)
            );
        }

        private void OnTriggerEnter(Collider other)
        {
            if (HasTriggered)
                return;

            bool isPlayer =
                other.CompareTag("Player") ||
                other.GetComponent<CharacterController>() != null ||
                other.GetComponentInParent<PlayerController>() != null;

            if (!isPlayer)
                return;

            // =========================================================
            // IMPORTANT SAFETY CHECK
            // =========================================================

            if (requireAllPuzzlesSolved && !exitUnlocked)
            {
                Debug.Log(
                    "<color=#ffcc00><b>[Stage3ExitTrigger]</b></color> " +
                    "Exit touched, but Stage 3 is NOT complete yet. Exit remains locked."
                );

                FeedbackHUD.ShowMessage(
                    "EXIT LOCKED",
                    new Color(1f, 0.75f, 0.20f)
                );

                return;
            }

            // =========================================================
            // STAGE 3 REALLY IS COMPLETE
            // =========================================================

            HasTriggered = true;

            Debug.Log(
                "<color=#5cb85c><b>[Stage3ExitTrigger]</b></color> " +
                "Stage 3 exit triggered AFTER all objectives were completed."
            );

            OnStageComplete?.Invoke();

            if (ObjectiveManager.Instance != null)
            {
                ObjectiveManager.Instance.SetFinalEscaped("STAGE 3 COMPLETE — FACILITY ESCAPED!");
            }

            if (BannerUI.Instance != null)
            {
                BannerUI.Instance.ShowBanner(
                    "STAGE 3 COMPLETE — FACILITY ESCAPED!",
                    "Security Control Room cleared."
                );
            }

            FeedbackHUD.ShowMessage(
                "STAGE 3 COMPLETE — FACILITY ESCAPED!",
                new Color(0.40f, 0.95f, 0.45f)
            );

            if (!string.IsNullOrEmpty(nextSceneName))
            {
                StartCoroutine(LoadNextSceneRoutine());
            }
        }

        private IEnumerator LoadNextSceneRoutine()
        {
            yield return new WaitForSeconds(3f);

            SceneManager.LoadScene(nextSceneName);
        }

        private void OnDrawGizmos()
        {
            Collider col = GetComponent<Collider>();

            if (col != null)
            {
                Gizmos.color = new Color(0f, 1f, 1f, 0.5f);
                Gizmos.matrix = transform.localToWorldMatrix;

                if (col is BoxCollider box)
                {
                    Gizmos.DrawWireCube(
                        box.center,
                        box.size
                    );
                }
                else
                {
                    Gizmos.DrawWireCube(
                        col.bounds.center - transform.position,
                        col.bounds.size
                    );
                }
            }
        }
    }
}