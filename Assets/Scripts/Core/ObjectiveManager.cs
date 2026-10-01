using System;
using System.Collections.Generic;
using UnityEngine;

namespace EscapeRoom.Core
{
    /// <summary>
    /// Manages the ordered list of ObjectiveStep assets, tracks progression,
    /// verifies completion flags, and raises events on step changes.
    /// Resets progression on scene load / restart.
    /// </summary>
    [DisallowMultipleComponent]
    public class ObjectiveManager : MonoBehaviour
    {
        private static ObjectiveManager instance;
        public static ObjectiveManager Instance => instance;

        [Header("Objective Steps (Ordered)")]
        [Tooltip("Ordered sequence of objective steps defining game progression.")]
        [SerializeField] private ObjectiveStep[] steps = new ObjectiveStep[0];

        [Header("Runtime State")]
        [SerializeField] private int currentStepIndex = 0;
        [SerializeField] private bool isSystemActive = true;

        public event Action<int, ObjectiveStep> OnStepChanged;
        public event Action<ObjectiveStep> OnObjectiveCompleted;
        public event Action OnAllObjectivesCompleted;

        public ObjectiveStep[] Steps => steps;
        public int CurrentStepIndex => currentStepIndex;
        public int TotalSteps => steps != null ? steps.Length : 0;
        public bool IsSystemActive
        {
            get => isSystemActive;
            set => isSystemActive = value;
        }

        public ObjectiveStep CurrentStep
        {
            get
            {
                if (steps != null && currentStepIndex >= 0 && currentStepIndex < steps.Length)
                {
                    return steps[currentStepIndex];
                }
                return null;
            }
        }

        public bool IsAllCompleted => steps != null && currentStepIndex >= steps.Length;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(this);
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
            // Initial notification for HUD
            NotifyStepChanged();
        }

        /// <summary>
        /// Attempts to complete the current objective step using the provided completion flag.
        /// Only advances if the flag matches the current step's completionFlag.
        /// </summary>
        public bool CompleteFlag(string flag)
        {
            if (!isSystemActive) return false;
            if (string.IsNullOrEmpty(flag)) return false;
            if (IsAllCompleted) return false;

            var current = CurrentStep;
            if (current == null) return false;

            if (string.Equals(current.CompletionFlag, flag, StringComparison.OrdinalIgnoreCase))
            {
                Debug.Log($"<color=#5cb85c><b>[ObjectiveManager]</b></color> Objective Completed: Step {currentStepIndex + 1}/{TotalSteps} ('{current.Instruction}') [Flag: {flag}]");

                OnObjectiveCompleted?.Invoke(current);
                currentStepIndex++;

                if (IsAllCompleted)
                {
                    Debug.Log("<b><color=#5cb85c>[ObjectiveManager] All Objectives Successfully Completed!</color></b>");
                    OnAllObjectivesCompleted?.Invoke();
                }

                NotifyStepChanged();
                return true;
            }

            return false;
        }

        public void SetSteps(ObjectiveStep[] newSteps)
        {
            steps = newSteps;
            currentStepIndex = 0;
            NotifyStepChanged();
        }

        public void ResetObjectives()
        {
            currentStepIndex = 0;
            isSystemActive = true;
            Debug.Log("[ObjectiveManager] Objectives reset to Step 1.");
            NotifyStepChanged();
        }

        public void ActivateSystem()
        {
            isSystemActive = true;
            NotifyStepChanged();
        }

        public void NotifyStepChanged()
        {
            OnStepChanged?.Invoke(currentStepIndex, CurrentStep);
        }
    }
}
