using UnityEngine;
using UnityEngine.InputSystem;
using EscapeRoom.Core;
using EscapeRoom.Interaction;
using EscapeRoom.UI;

namespace EscapeRoom.Player
{
    /// <summary>
    /// Controls player flashlight equipped with a spotlight attached to PlayerCamera.
    /// Toggled via F key. Drains battery only when ON and not in any modal/pause state.
    /// Highly optimized for Intel Arc GPU (no heavy real-time shadows).
    /// Resets battery upon scene load / restart.
    /// </summary>
    [DisallowMultipleComponent]
    public class FlashlightController : MonoBehaviour
    {
        private static FlashlightController instance;
        public static FlashlightController Instance => instance;

        [Header("Spotlight References")]
        [Tooltip("The spotlight child GameObject or component.")]
        [SerializeField] private Light spotlight;

        [Header("Light Parameters")]
        [SerializeField] private float intensity = 2.2f;
        [SerializeField] private float range = 14f;
        [SerializeField] private float spotAngle = 48f;
        [SerializeField] private float innerSpotAngle = 26f;
        [SerializeField] private Color lightColor = new Color(0.98f, 0.95f, 0.88f);

        [Header("Battery Settings")]
        [Tooltip("Total battery duration in seconds.")]
        [SerializeField] private float maxBatterySeconds = 300f; // 5 minutes
        [SerializeField] private float currentBattery = 300f;

        [Header("State")]
        [SerializeField] private bool isOn = false;

        public bool IsOn => isOn;
        public float BatteryFraction => maxBatterySeconds > 0 ? Mathf.Clamp01(currentBattery / maxBatterySeconds) : 0f;
        public float CurrentBattery => currentBattery;
        public float MaxBatterySeconds => maxBatterySeconds;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(this);
                return;
            }
            instance = this;

            EnsureSpotlightSetup();
            ResetBattery();
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        public void EnsureSpotlightSetup()
        {
            if (spotlight == null)
            {
                // Check if already a child light
                spotlight = GetComponentInChildren<Light>(true);
            }

            if (spotlight == null)
            {
                Transform parentTransform = transform;
                var cam = GetComponentInChildren<Camera>();
                if (cam != null)
                {
                    parentTransform = cam.transform;
                }

                var lightGo = new GameObject("Flashlight_Spotlight");
                lightGo.transform.SetParent(parentTransform, false);
                lightGo.transform.localPosition = new Vector3(0.2f, -0.15f, 0.3f);
                lightGo.transform.localRotation = Quaternion.identity;

                spotlight = lightGo.AddComponent<Light>();
            }

            // Configure spotlight properties
            spotlight.type = LightType.Spot;
            spotlight.intensity = intensity;
            spotlight.range = range;
            spotlight.spotAngle = spotAngle;
            spotlight.innerSpotAngle = innerSpotAngle;
            spotlight.color = lightColor;
            spotlight.shadows = LightShadows.None; // Optimized for Intel Arc
            spotlight.enabled = isOn;
        }

        private void Update()
        {
            HandleInput();
            ProcessBatteryDrain();
        }

        private void HandleInput()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.fKey.wasPressedThisFrame)
            {
                if (CanToggleFlashlight())
                {
                    ToggleFlashlight();
                }
            }
        }

        public bool CanToggleFlashlight()
        {
            if (AtticBriefingUI.IsBriefingOpen) return false;
            if (PhoneIntroUI.IsIntroOpen) return false;
            if (ClueInteractable.IsAnyClueOpen) return false;
            if (KeypadUI.Instance != null && KeypadUI.Instance.IsOpen) return false;
            if (PauseManager.Instance != null && PauseManager.Instance.IsPaused) return false;
            if (EscapeUI.Instance != null && EscapeUI.Instance.IsOpen) return false;
            return true;
        }

        public bool CanDrainBattery()
        {
            if (!isOn) return false;
            if (Time.timeScale <= 0f) return false;
            if (AtticBriefingUI.IsBriefingOpen) return false;
            if (PhoneIntroUI.IsIntroOpen) return false;
            if (ClueInteractable.IsAnyClueOpen) return false;
            if (KeypadUI.Instance != null && KeypadUI.Instance.IsOpen) return false;
            if (PauseManager.Instance != null && PauseManager.Instance.IsPaused) return false;
            if (EscapeUI.Instance != null && EscapeUI.Instance.IsOpen) return false;
            return true;
        }

        public void ToggleFlashlight()
        {
            if (currentBattery <= 0f && !isOn)
            {
                FeedbackHUD.ShowMessage("Flashlight battery is dead!", new Color(0.95f, 0.40f, 0.40f));
                return;
            }

            isOn = !isOn;
            if (spotlight != null)
            {
                spotlight.enabled = isOn;
            }

            string state = isOn ? "ON" : "OFF";
            FeedbackHUD.ShowMessage($"Flashlight {state}", isOn ? new Color(0.98f, 0.95f, 0.50f) : new Color(0.70f, 0.70f, 0.70f));
            Debug.Log($"[FlashlightController] Flashlight toggled: {state}. Battery: {currentBattery:F1}s.");
        }

        private void ProcessBatteryDrain()
        {
            if (!CanDrainBattery()) return;

            currentBattery -= Time.deltaTime;
            if (currentBattery <= 0f)
            {
                currentBattery = 0f;
                isOn = false;
                if (spotlight != null)
                {
                    spotlight.enabled = false;
                }
                FeedbackHUD.ShowMessage("Flashlight battery depleted!", new Color(0.95f, 0.35f, 0.35f));
            }
        }

        public void ResetBattery()
        {
            currentBattery = maxBatterySeconds;
            isOn = false;
            if (spotlight != null)
            {
                spotlight.enabled = false;
            }
            Debug.Log("[FlashlightController] Flashlight reset to full charge (OFF).");
        }

        private void OnGUI()
        {
            // Do not show indicator when modal or pause screens are active
            if (AtticBriefingUI.IsBriefingOpen || PhoneIntroUI.IsIntroOpen) return;
            if (PauseManager.Instance != null && PauseManager.Instance.IsPaused) return;
            if (EscapeUI.Instance != null && EscapeUI.Instance.IsOpen) return;
            if (ClueInteractable.IsAnyClueOpen) return;
            if (KeypadUI.Instance != null && KeypadUI.Instance.IsOpen) return;

            // Small HUD indicator in bottom-right corner
            float boxW = 160f;
            float boxH = 30f;
            float boxX = Screen.width - boxW - 20f;
            float boxY = Screen.height - boxH - 20f;

            Color oldColor = GUI.color;

            // Indicator background
            GUI.color = new Color(0.08f, 0.10f, 0.14f, 0.75f);
            GUI.Box(new Rect(boxX, boxY, boxW, boxH), GUIContent.none);

            int batteryPercent = Mathf.RoundToInt(BatteryFraction * 100f);
            Color textColor = isOn ? new Color(0.98f, 0.92f, 0.35f) : new Color(0.60f, 0.65f, 0.70f);

            var labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = textColor }
            };

            string statusText = isOn ? $"[F] LIGHT ON ({batteryPercent}%)" : $"[F] LIGHT OFF ({batteryPercent}%)";
            GUI.color = Color.white;
            GUI.Label(new Rect(boxX, boxY, boxW, boxH), statusText, labelStyle);

            GUI.color = oldColor;
        }
    }
}
