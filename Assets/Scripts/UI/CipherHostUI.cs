using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EscapeRoom.Core;
using EscapeRoom.Interaction;

namespace EscapeRoom.UI
{
    /// <summary>
    /// CIPHER — the interactive digital guide character for the Escape Room.
    /// 
    /// Cipher is a friendly, mysterious AI that guides the player through Stage 1.
    /// Implemented as a compact IMGUI portrait panel — lightweight, no 3D model required.
    /// 
    /// Features:
    ///   • Contextual dialogue on objective changes
    ///   • Three-level progressive hint system
    ///   • Inactivity detection (offers help after ~60 seconds stuck)
    ///   • Timer warning messages (5min, 2min, 1min, 30sec)
    ///   • Interactive [ASK CIPHER] button
    ///   • Connects to ObjectiveManager for current-step awareness
    ///   • Non-horror tone: confident, playful, helpful
    /// </summary>
    [DisallowMultipleComponent]
    public class CipherHostUI : MonoBehaviour
    {
        private static CipherHostUI instance;
        public static CipherHostUI Instance => instance;

        // ── Config ────────────────────────────────────────────────────────────
        [Header("Cipher Settings")]
        [SerializeField] private string hostName = "CIPHER";
        [SerializeField] private float  messageDisplayDuration = 5.5f;
        [SerializeField] private float  inactivityTimeout      = 60f;
        [SerializeField] private float  hintOfferCooldown      = 90f;

        [Header("Portrait Colors")]
        [SerializeField] private Color accentColor   = new Color(0.25f, 0.90f, 1.0f);
        [SerializeField] private Color portraitColor = new Color(0.12f, 0.22f, 0.32f);

        private Texture2D portraitTexture;

        private void EnsurePortraitLoaded()
        {
            if (portraitTexture != null) return;
            try
            {
                string path = System.IO.Path.Combine(Application.dataPath, "Art/Sprites/CipherPortrait.jpg");
                if (System.IO.File.Exists(path))
                {
                    byte[] bytes = System.IO.File.ReadAllBytes(path);
                    portraitTexture = new Texture2D(2, 2, TextureFormat.RGBA32, true);
                    portraitTexture.LoadImage(bytes);
                    portraitTexture.filterMode = FilterMode.Bilinear;
                    portraitTexture.anisoLevel = 8;
                    portraitTexture.wrapMode = TextureWrapMode.Clamp;
                    portraitTexture.Apply(true, true);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[CipherHostUI] Could not load portrait texture: " + ex.Message);
            }
        }

        // ── State ─────────────────────────────────────────────────────────────
        private bool  isDialogueOpen   = false;
        private bool  isHintOfferOpen  = false;
        private bool  isActive         = false;  // true once gameplay starts
        private float dialogueTimer    = 0f;
        private float inactivityTimer  = 0f;
        private float hintCooldownTimer = 0f;
        private float panelAlpha       = 0f;
        private string currentMessage  = "";
        private int   lastObjectiveStep = -1;
        private int   currentHintLevel  = 0;   // 0 = none offered yet for this objective

        // ── GUI Styles ────────────────────────────────────────────────────────
        private GUIStyle nameStyle, msgStyle, btnStyle, smallBtnStyle;
        private bool stylesBuilt = false;

        // ── Hint Database ─────────────────────────────────────────────────────
        // Keyed by completion flag of the CURRENT objective step.
        private static readonly Dictionary<string, string[]> HintsByFlag =
            new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["LOCKBOX_KEY_ACQUIRED"] = new[]
            {
                "\"Ciphers don't open boxes. Keys do.\"",
                "\"Check the places where someone would normally leave a small key.\"",
                "\"Look near the desk. The key is close.\""
            },
            ["LOCKBOX_OPENED"] = new[]
            {
                "\"That lock isn't going to open itself.\"",
                "\"You have the key. Use it on the lockbox.\"",
                "\"Approach the lockbox, press E to unlock, then open it.\""
            },
            ["EXIT_NOTE_READ"] = new[]
            {
                "\"There's something inside that box. Look carefully.\"",
                "\"Take a closer look at what's inside the lockbox.\"",
                "\"There's a note inside with information you need. Read it.\""
            },
            ["EXIT_CODE_ACCEPTED"] = new[]
            {
                "\"That note had numbers on it for a reason.\"",
                "\"Find the keypad terminal and enter what the note said.\"",
                "\"Go to the keypad and enter the code from the note. It's 4 digits.\""
            }
        };

        // ── Objective-change dialogues ─────────────────────────────────────────
        private static readonly Dictionary<string, string> ObjectiveStartDialogue =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["LOCKBOX_KEY_ACQUIRED"] =
                "\"Find the key first. I'll be here if you need me.\"",
            ["LOCKBOX_OPENED"] =
                "\"Good. Now try that key on the locked box.\"",
            ["EXIT_NOTE_READ"] =
                "\"Nice. There's something inside. Take a closer look.\"",
            ["EXIT_CODE_ACCEPTED"] =
                "\"That note has the code you need for the keypad.\""
        };

        // ── Completion acknowledgement dialogues ───────────────────────────────
        private static readonly Dictionary<string, string> CompletionDialogue =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["LOCKBOX_KEY_ACQUIRED"] =
                "\"Found it. Now use it on the lockbox.\"",
            ["LOCKBOX_OPENED"] =
                "\"Nice. There's something inside the box. Take a closer look.\"",
            ["EXIT_NOTE_READ"] =
                "\"That note contains the code. Enter it on the keypad terminal.\"",
            ["EXIT_CODE_ACCEPTED"] =
                "\"That's it. The exit is unlocked. Move quickly.\""
        };

        // ── Timer warning dialogues ────────────────────────────────────────────
        private static readonly Dictionary<float, string> TimerWarningDialogue =
            new Dictionary<float, string>
        {
            [300f] = "\"You're halfway through the available time. Stay focused.\"",
            [120f] = "\"Two minutes remaining. Focus.\"",
            [60f]  = "\"One minute. If you're stuck, ask me for a hint.\"",
            [30f]  = "\"Thirty seconds.\""
        };

        // ── Lifecycle ─────────────────────────────────────────────────────────
        private void Awake()
        {
            if (instance != null && instance != this) { Destroy(gameObject); return; }
            instance = this;
        }

        private void OnDestroy()
        {
            if (instance == this) instance = null;
        }

        private void OnEnable()
        {
            if (ObjectiveManager.Instance != null)
            {
                ObjectiveManager.Instance.OnStepChanged    += HandleStepChanged;
                ObjectiveManager.Instance.OnObjectiveCompleted += HandleObjectiveCompleted;
                ObjectiveManager.Instance.OnAllObjectivesCompleted += HandleAllCompleted;
            }

            if (StageTimer.Instance != null)
            {
                StageTimer.Instance.OnWarningThreshold += HandleTimerWarning;
                StageTimer.Instance.OnTimerExpired     += HandleTimerExpired;
            }
        }

        private void OnDisable()
        {
            if (ObjectiveManager.Instance != null)
            {
                ObjectiveManager.Instance.OnStepChanged    -= HandleStepChanged;
                ObjectiveManager.Instance.OnObjectiveCompleted -= HandleObjectiveCompleted;
                ObjectiveManager.Instance.OnAllObjectivesCompleted -= HandleAllCompleted;
            }

            if (StageTimer.Instance != null)
            {
                StageTimer.Instance.OnWarningThreshold -= HandleTimerWarning;
                StageTimer.Instance.OnTimerExpired     -= HandleTimerExpired;
            }
        }

        private void Start()
        {
            // Re-bind in case singletons weren't ready in OnEnable
            if (ObjectiveManager.Instance != null)
            {
                ObjectiveManager.Instance.OnStepChanged       -= HandleStepChanged;
                ObjectiveManager.Instance.OnStepChanged       += HandleStepChanged;
                ObjectiveManager.Instance.OnObjectiveCompleted -= HandleObjectiveCompleted;
                ObjectiveManager.Instance.OnObjectiveCompleted += HandleObjectiveCompleted;
                ObjectiveManager.Instance.OnAllObjectivesCompleted -= HandleAllCompleted;
                ObjectiveManager.Instance.OnAllObjectivesCompleted += HandleAllCompleted;
            }
            if (StageTimer.Instance != null)
            {
                StageTimer.Instance.OnWarningThreshold -= HandleTimerWarning;
                StageTimer.Instance.OnWarningThreshold += HandleTimerWarning;
                StageTimer.Instance.OnTimerExpired     -= HandleTimerExpired;
                StageTimer.Instance.OnTimerExpired     += HandleTimerExpired;
            }
        }

        private void Update()
        {
            if (!isActive) return;

            // Fade panel
            float targetAlpha = isDialogueOpen || isHintOfferOpen ? 1f : 0f;
            panelAlpha = Mathf.MoveTowards(panelAlpha, targetAlpha, Time.unscaledDeltaTime * 4f);

            // Dialogue timer
            if (isDialogueOpen)
            {
                dialogueTimer -= Time.deltaTime;
                if (dialogueTimer <= 0f)
                {
                    isDialogueOpen = false;
                }
            }

            // Inactivity tracker
            if (!isDialogueOpen && !isHintOfferOpen && hintCooldownTimer <= 0f)
            {
                inactivityTimer += Time.deltaTime;
                if (inactivityTimer >= inactivityTimeout)
                {
                    inactivityTimer  = 0f;
                    hintCooldownTimer = hintOfferCooldown;
                    OfferHintPrompt();
                }
            }
            else if (!isDialogueOpen)
            {
                // Reset inactivity when player is actively progressing
            }

            if (hintCooldownTimer > 0f)
                hintCooldownTimer -= Time.deltaTime;
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Activate Cipher after gameplay begins (called by AtticBriefingUI or game start).</summary>
        public void Activate()
        {
            isActive = true;
            currentHintLevel = 0;
            inactivityTimer  = 0f;
            ShowMessage("\"You're finally awake.\"\n\"Don't worry. You're not in danger.\"\n\"Find your way out and I'll help you with what comes next.\"",
                        duration: 7f);
            Debug.Log("[CipherHostUI] Cipher activated.");
        }

        /// <summary>Show a Cipher dialogue message for the given duration.</summary>
        public void ShowMessage(string message, float duration = -1f)
        {
            currentMessage  = message;
            dialogueTimer   = duration > 0f ? duration : messageDisplayDuration;
            isDialogueOpen  = true;
            isHintOfferOpen = false;
            inactivityTimer = 0f;
        }

        /// <summary>Give the next level hint for the current objective. Cycles through 3 levels.</summary>
        public void GiveHint()
        {
            isHintOfferOpen = false;
            var step = ObjectiveManager.Instance?.CurrentStep;
            if (step == null) { ShowMessage("\"No hint available right now.\""); return; }

            string flag = step.CompletionFlag;
            if (!HintsByFlag.TryGetValue(flag, out string[] hints))
            {
                ShowMessage("\"You're on the right track. Keep exploring.\"");
                return;
            }

            int idx = Mathf.Clamp(currentHintLevel, 0, hints.Length - 1);
            ShowMessage(hints[idx], duration: messageDisplayDuration + 2f);
            if (currentHintLevel < hints.Length - 1) currentHintLevel++;

            inactivityTimer   = 0f;
            hintCooldownTimer = hintOfferCooldown * 0.5f;
        }

        /// <summary>Called by the [ASK CIPHER] button.</summary>
        public void OnAskCipherClicked()
        {
            GiveHint();
        }

        // ── Event Handlers ────────────────────────────────────────────────────

        private void HandleStepChanged(int stepIndex, ObjectiveStep step)
        {
            if (!isActive || step == null) return;
            currentHintLevel = 0;
            inactivityTimer  = 0f;

            if (ObjectiveStartDialogue.TryGetValue(step.CompletionFlag, out string msg))
            {
                StartCoroutine(DelayedMessage(msg, 1.2f));
            }
        }

        private void HandleObjectiveCompleted(ObjectiveStep step)
        {
            if (!isActive || step == null) return;
            currentHintLevel = 0;
            inactivityTimer  = 0f;

            if (CompletionDialogue.TryGetValue(step.CompletionFlag, out string msg))
            {
                StartCoroutine(DelayedMessage(msg, 0.8f));
            }
        }

        private void HandleAllCompleted()
        {
            if (!isActive) return;
            // Stop timer
            StageTimer.Instance?.StopTimer();
            string remaining = StageTimer.Instance != null
                ? StageTimer.FormatTime(StageTimer.Instance.TimeRemaining)
                : "--:--";
            ShowMessage($"\"Excellent work.\"\n\"Time remaining: {remaining}\"\n\"That was only the first room.\"",
                        duration: 8f);
        }

        private void HandleTimerWarning(float threshold)
        {
            if (!isActive) return;
            if (TimerWarningDialogue.TryGetValue(threshold, out string msg))
            {
                StartCoroutine(DelayedMessage(msg, 0.5f));
            }
        }

        private void HandleTimerExpired()
        {
            if (!isActive) return;
            isDialogueOpen  = false;
            isHintOfferOpen = false;
            isActive        = false;
        }

        private IEnumerator DelayedMessage(string msg, float delay)
        {
            yield return new WaitForSeconds(delay);
            ShowMessage(msg);
        }

        private void OfferHintPrompt()
        {
            if (!isActive || isHintOfferOpen || isDialogueOpen) return;
            isHintOfferOpen = true;
            isDialogueOpen  = false;
        }

        // ── OnGUI ─────────────────────────────────────────────────────────────
        private void OnGUI()
        {
            // Suppress during non-gameplay screens
            if (AtticBriefingUI.IsBriefingOpen) return;
            if (PhoneIntroUI.IsIntroOpen)        return;
            if (PauseManager.Instance != null && PauseManager.Instance.IsPaused) return;

            if (!isActive) return;

            if (!stylesBuilt) BuildStyles();

            // ── Ask Cipher button — always visible during active gameplay ──────
            DrawAskCipherButton();

            // ── Cipher dialogue panel — only when open ─────────────────────────
            float eff = panelAlpha;
            if (eff > 0.02f)
            {
                DrawPanel(eff);
            }
        }

        private void DrawPanel(float alpha)
        {
            EnsurePortraitLoaded();
            float sw = Screen.width, sh = Screen.height;

            // Panel: bottom-left side character display
            float pw = Mathf.Min(450f, sw * 0.48f);
            float ph = isHintOfferOpen ? 190f : 160f;
            float px = 14f;
            float py = sh - ph - 68f;

            Color old = GUI.color;

            // Dark backing
            GUI.color = new Color(0.04f, 0.08f, 0.12f, 0.94f * alpha);
            GUI.DrawTexture(new Rect(px, py, pw, ph), Texture2D.whiteTexture);

            // Cyan left accent border
            GUI.color = accentColor * new Color(1, 1, 1, 0.95f * alpha);
            GUI.DrawTexture(new Rect(px, py, 4f, ph), Texture2D.whiteTexture);

            // Character Portrait - Enlarged 90x105 for sharp high clarity
            float portW = 90f;
            float portH = 105f;
            float portX = px + 12f;
            float portY = py + 12f;

            // Outer cyan border frame around portrait
            GUI.color = accentColor * new Color(1, 1, 1, 0.85f * alpha);
            GUI.DrawTexture(new Rect(portX - 2, portY - 2, portW + 4, portH + 4), Texture2D.whiteTexture);

            // Inner dark backdrop
            GUI.color = new Color(0.02f, 0.04f, 0.06f, alpha);
            GUI.DrawTexture(new Rect(portX, portY, portW, portH), Texture2D.whiteTexture);

            if (portraitTexture != null)
            {
                GUI.color = new Color(1f, 1f, 1f, alpha);
                GUI.DrawTexture(new Rect(portX, portY, portW, portH), portraitTexture, ScaleMode.ScaleToFit);
            }
            else
            {
                GUI.color = accentColor * new Color(1, 1, 1, alpha);
                float cx = portX + portW * 0.5f, cy = portY + portH * 0.5f;
                GUI.DrawTexture(new Rect(cx - 2, cy - 20, 4, 40), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(cx - 20, cy - 2, 40, 4), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(cx - 10, cy - 10, 20, 20), Texture2D.whiteTexture);
            }

            GUI.color = new Color(1f, 1f, 1f, alpha);

            // Name label: CIPHER
            float textX = px + portW + 18f;
            float textW = pw - (portW + 28f);
            nameStyle.normal.textColor = accentColor * new Color(1, 1, 1, alpha);
            GUI.Label(new Rect(textX, py + 10, textW, 22), hostName + "  ·  DIGITAL GUIDE", nameStyle);

            // Divider
            GUI.color = accentColor * new Color(1, 1, 1, 0.40f * alpha);
            GUI.DrawTexture(new Rect(textX, py + 34, textW - 4f, 1), Texture2D.whiteTexture);

            GUI.color = new Color(1f, 1f, 1f, alpha);

            if (isHintOfferOpen)
            {
                DrawHintOffer(textX, py, textW, ph, alpha);
            }
            else if (isDialogueOpen)
            {
                DrawDialogue(textX, py, textW, ph, alpha);
            }

            GUI.color = old;
        }

        private void DrawDialogue(float textX, float py, float textW, float ph, float alpha)
        {
            msgStyle.normal.textColor = new Color(1.0f, 1.0f, 1.0f, alpha);
            GUI.Label(new Rect(textX, py + 42, textW, ph - 52), currentMessage, msgStyle);
        }

        private void DrawHintOffer(float textX, float py, float textW, float ph, float alpha)
        {
            msgStyle.normal.textColor = new Color(1.0f, 1.0f, 1.0f, alpha);
            GUI.Label(new Rect(textX, py + 40, textW, 46),
                "\"You've been working on this for a while.\"\n\"Would you like a hint?\"", msgStyle);

            float btnW = 75f, btnH = 26f;
            float btnY = py + 112f;

            GUI.backgroundColor = new Color(0.20f, 0.70f, 0.35f, alpha);
            smallBtnStyle.normal.textColor = Color.white;
            if (GUI.Button(new Rect(textX, btnY, btnW, btnH), "YES", smallBtnStyle))
            {
                GiveHint();
            }

            GUI.backgroundColor = new Color(0.30f, 0.33f, 0.40f, alpha);
            if (GUI.Button(new Rect(textX + 85f, btnY, btnW, btnH), "NOT YET", smallBtnStyle))
            {
                isHintOfferOpen = false;
                inactivityTimer = 0f;
            }

            GUI.backgroundColor = Color.white;
        }

        private void DrawAskCipherButton()
        {
            if (!isActive) return;

            // Suppress during modals
            if (KeypadUI.Instance != null && KeypadUI.Instance.IsOpen) return;
            if (ClueInteractable.IsAnyClueOpen) return;
            if (SymbolClueInteractable.IsAnyClueOpen) return;

            if (!stylesBuilt) BuildStyles();

            string keyName = InputConfig.InteractKeyName;
            string buttonLabel = $"[ {keyName} ] ASK CIPHER";

            float bw = 150f, bh = 32f;
            float bx = 14f;
            float by = Screen.height - bh - 14f;

            Color old = GUI.color;

            GUI.color = new Color(0.06f, 0.10f, 0.14f, 0.88f);
            GUI.DrawTexture(new Rect(bx, by, bw, bh), Texture2D.whiteTexture);
            GUI.color = accentColor * new Color(1, 1, 1, 0.80f);
            GUI.DrawTexture(new Rect(bx, by, 3f, bh), Texture2D.whiteTexture);
            GUI.color = Color.white;

            GUI.backgroundColor = new Color(0.10f, 0.14f, 0.18f, 1f);
            var askStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize  = 11, fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal    = { textColor = Color.white }
            };
            if (GUI.Button(new Rect(bx, by, bw, bh), buttonLabel, askStyle))
            {
                OnAskCipherClicked();
            }
            GUI.backgroundColor = Color.white;
            GUI.color = old;
        }

        // ── Styles ────────────────────────────────────────────────────────────
        private void BuildStyles()
        {
            nameStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 13, fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };
            msgStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 12, fontStyle = FontStyle.Normal,
                alignment = TextAnchor.UpperLeft, wordWrap = true
            };
            btnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize  = 14, fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal    = { textColor = Color.white }
            };
            smallBtnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize  = 11, fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal    = { textColor = Color.white }
            };
            stylesBuilt = true;
        }
    }
}
