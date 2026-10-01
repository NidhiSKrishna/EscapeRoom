using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using EscapeRoom.Core;
using EscapeRoom.Interaction;
using EscapeRoom.Player;
using EscapeRoom.UI;

namespace EscapeRoom.Editor
{
    /// <summary>
    /// Automated setup tool for the guided, story-driven escape room flow:
    /// - Generates the 7 ObjectiveStep ScriptableObject assets
    /// - Creates and configures Assets/Scenes/MainMenu.unity
    /// - Updates Build Settings (0: MainMenu, 1: EscapeRoom_Main, 2: SampleScene)
    /// - Populates EscapeRoom_Main with ObjectiveManager, ProgressionAdapter, ObjectiveHUD, BannerUI, AtticBriefingUI, and FlashlightController
    /// </summary>
    [InitializeOnLoad]
    public static class GuidedEscapeRoomSetup
    {
        public const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";
        public const string MainScenePath = "Assets/Scenes/EscapeRoom_Main.unity";
        public const string ObjectivesFolder = "Assets/UI/Objectives";

        [MenuItem("Tools/Escape Room/Setup Guided Flow & Objectives", false, 30)]
        public static void RunFullSetup()
        {
            Debug.Log("<b><color=#337ab7>[GuidedEscapeRoomSetup]</color> Starting full guided flow setup...</b>");

            // 1. Create ObjectiveStep ScriptableObject assets
            ObjectiveStep[] steps = EnsureObjectiveAssets();

            // 2. Setup MainMenu scene
            EnsureMainMenuScene();

            // 3. Update Build Settings
            UpdateBuildSettings();

            // 4. Setup EscapeRoom_Main scene
            EnsureEscapeRoomMainSetup(steps);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("<b><color=#5cb85c>[GuidedEscapeRoomSetup] Guided flow and objectives setup complete!</color></b>");
        }

        public static ObjectiveStep[] EnsureObjectiveAssets()
        {
            if (!AssetDatabase.IsValidFolder("Assets/UI"))
            {
                AssetDatabase.CreateFolder("Assets", "UI");
            }
            if (!AssetDatabase.IsValidFolder(ObjectivesFolder))
            {
                AssetDatabase.CreateFolder("Assets/UI", "Objectives");
            }

            var stepDefs = new (string filename, string title, string instruction, string flag, string room)[]
            {
                ("Step1_LockboxKey", "Lockbox Key", "Find the Lockbox Key on the shelf.", "LOCKBOX_KEY_ACQUIRED", "Attic"),
                ("Step2_OpenLockbox", "Open Lockbox", "Use the Lockbox Key to open the lockbox.", "LOCKBOX_OPENED", "Attic"),
                ("Step3_ExitNote", "Exit Code Note", "Read the Exit Code Note inside the lockbox.", "EXIT_NOTE_READ", "Attic"),
                ("Step4_KeypadCode", "Exit Keypad", "Enter the code from the note on the exit keypad.", "EXIT_CODE_ACCEPTED", "Attic"),
                ("Step5_StorageKeycard", "Storage Keycard", "Search the storage room and find the keycard.", "KEYCARD_ACQUIRED", "Storage Room"),
                ("Step6_SecurityReader", "Security Reader", "Use the keycard on the security reader to reach the cellar.", "CELLAR_UNLOCKED", "Cellar"),
                ("Step7_FinalExit", "Final Exit", "Find the key in the crate and unlock the final exit.", "FINAL_EXIT_UNLOCKED", "Cellar")
            };

            ObjectiveStep[] result = new ObjectiveStep[stepDefs.Length];

            for (int i = 0; i < stepDefs.Length; i++)
            {
                var def = stepDefs[i];
                string assetPath = $"{ObjectivesFolder}/{def.filename}.asset";

                ObjectiveStep step = AssetDatabase.LoadAssetAtPath<ObjectiveStep>(assetPath);
                if (step == null)
                {
                    step = ScriptableObject.CreateInstance<ObjectiveStep>();
                    step.Configure(def.title, def.instruction, def.flag, def.room);
                    AssetDatabase.CreateAsset(step, assetPath);
                    Debug.Log($"[GuidedEscapeRoomSetup] Created ObjectiveStep asset: '{assetPath}'.");
                }
                else
                {
                    step.Configure(def.title, def.instruction, def.flag, def.room);
                    EditorUtility.SetDirty(step);
                }

                result[i] = step;
            }

            AssetDatabase.SaveAssets();
            return result;
        }

        public static void EnsureMainMenuScene()
        {
            if (File.Exists(MainMenuScenePath))
            {
                Debug.Log($"[GuidedEscapeRoomSetup] MainMenu scene already exists at '{MainMenuScenePath}'.");
                return;
            }

            // Create new scene
            Scene currentActive = EditorSceneManager.GetActiveScene();
            Scene menuScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Create Main Camera
            GameObject camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            Camera cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.04f, 0.05f, 0.07f, 1f);
            camGo.AddComponent<AudioListener>();
            camGo.transform.position = new Vector3(0f, 1f, -10f);

            // Create Directional Light
            GameObject lightGo = new GameObject("Directional Light");
            Light light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 0.5f;
            light.color = new Color(0.85f, 0.90f, 1f);
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            // Create MainMenu GameObject
            GameObject menuGo = new GameObject("MainMenu");
            PhoneIntroUI phoneIntro = menuGo.AddComponent<PhoneIntroUI>();
            MainMenuUI menuUI = menuGo.AddComponent<MainMenuUI>();

            // Link serialized reference
            SerializedObject so = new SerializedObject(menuUI);
            so.FindProperty("phoneIntroUI").objectReferenceValue = phoneIntro;
            so.ApplyModifiedProperties();

            // Save Scene
            EditorSceneManager.SaveScene(menuScene, MainMenuScenePath);
            Debug.Log($"<color=#5cb85c><b>[GuidedEscapeRoomSetup]</b></color> Created '{MainMenuScenePath}' with MainMenuUI and PhoneIntroUI.");

            // Restore previous active scene if valid
            if (!string.IsNullOrEmpty(currentActive.path) && File.Exists(currentActive.path))
            {
                EditorSceneManager.OpenScene(currentActive.path);
            }
        }

        public static void UpdateBuildSettings()
        {
            var newScenes = new EditorBuildSettingsScene[]
            {
                new EditorBuildSettingsScene(MainMenuScenePath, true),
                new EditorBuildSettingsScene(MainScenePath, true),
                new EditorBuildSettingsScene(SceneHousekeeper.SampleScenePath, false)
            };

            EditorBuildSettings.scenes = newScenes;
            Debug.Log("[GuidedEscapeRoomSetup] Build Settings updated: 0 -> MainMenu (enabled), 1 -> EscapeRoom_Main (enabled).");
        }

        public static void EnsureEscapeRoomMainSetup(ObjectiveStep[] steps = null)
        {
            Scene activeScene = EditorSceneManager.GetActiveScene();
            bool wasMainOpen = (activeScene.path == MainScenePath);

            if (!wasMainOpen)
            {
                if (File.Exists(MainScenePath))
                {
                    activeScene = EditorSceneManager.OpenScene(MainScenePath);
                }
                else
                {
                    Debug.LogWarning("[GuidedEscapeRoomSetup] EscapeRoom_Main does not exist yet.");
                    return;
                }
            }

            if (steps == null || steps.Length == 0)
            {
                steps = EnsureObjectiveAssets();
            }

            bool sceneModified = false;

            // 1. Setup GameManagers GameObject
            GameObject managersGo = GameObject.Find(PuzzleBuilder.GameManagersName);
            if (managersGo == null)
            {
                managersGo = new GameObject(PuzzleBuilder.GameManagersName);
                Undo.RegisterCreatedObjectUndo(managersGo, "Create GameManagers");
                sceneModified = true;
            }

            // Ensure Core Managers
            if (managersGo.GetComponent<GameManager>() == null)
            {
                managersGo.AddComponent<GameManager>();
                sceneModified = true;
            }
            if (managersGo.GetComponent<PauseManager>() == null)
            {
                managersGo.AddComponent<PauseManager>();
                sceneModified = true;
            }
            if (managersGo.GetComponent<EscapeUI>() == null)
            {
                managersGo.AddComponent<EscapeUI>();
                sceneModified = true;
            }
            if (managersGo.GetComponent<PauseUI>() == null)
            {
                managersGo.AddComponent<PauseUI>();
                sceneModified = true;
            }
            if (managersGo.GetComponent<FeedbackHUD>() == null)
            {
                managersGo.AddComponent<FeedbackHUD>();
                sceneModified = true;
            }

            // ObjectiveManager
            var om = managersGo.GetComponent<ObjectiveManager>();
            if (om == null)
            {
                om = managersGo.AddComponent<ObjectiveManager>();
                sceneModified = true;
            }
            om.SetSteps(steps);
            EditorUtility.SetDirty(om);

            // ObjectiveProgressionAdapter
            if (managersGo.GetComponent<ObjectiveProgressionAdapter>() == null)
            {
                managersGo.AddComponent<ObjectiveProgressionAdapter>();
                sceneModified = true;
            }

            // ObjectiveHUD
            if (managersGo.GetComponent<ObjectiveHUD>() == null)
            {
                managersGo.AddComponent<ObjectiveHUD>();
                sceneModified = true;
            }

            // BannerUI
            if (managersGo.GetComponent<BannerUI>() == null)
            {
                managersGo.AddComponent<BannerUI>();
                sceneModified = true;
            }

            // AtticBriefingUI
            if (managersGo.GetComponent<AtticBriefingUI>() == null)
            {
                managersGo.AddComponent<AtticBriefingUI>();
                sceneModified = true;
            }

            // 2. Setup Player FlashlightController
            GameObject playerGo = GameObject.Find("Player");
            if (playerGo != null)
            {
                var flashlight = playerGo.GetComponent<FlashlightController>();
                if (flashlight == null)
                {
                    flashlight = playerGo.AddComponent<FlashlightController>();
                    sceneModified = true;
                }
                flashlight.EnsureSpotlightSetup();
                EditorUtility.SetDirty(playerGo);
            }

            // 3. Room Banner Triggers
            GameObject triggersParent = GameObject.Find("RoomBannerTriggers");
            if (triggersParent == null)
            {
                triggersParent = new GameObject("RoomBannerTriggers");
                sceneModified = true;
            }

            // Attic Banner Trigger
            Transform atticTrigTrans = triggersParent.transform.Find("Trigger_Attic");
            if (atticTrigTrans == null)
            {
                GameObject atticTrigGo = new GameObject("Trigger_Attic");
                atticTrigGo.transform.SetParent(triggersParent.transform, false);
                atticTrigGo.transform.position = new Vector3(0f, 1.2f, -4f);

                BoxCollider col = atticTrigGo.AddComponent<BoxCollider>();
                col.isTrigger = true;
                col.size = new Vector3(8f, 3f, 8f);

                var rbt = atticTrigGo.AddComponent<RoomBannerTrigger>();
                rbt.RoomTitle = "ATTIC";
                rbt.TipText = "Search carefully. Something useful is hidden here.";
                sceneModified = true;
            }

            // Storage Room Banner Trigger (Infrastructure placeholder beyond exit door)
            Transform storageTrigTrans = triggersParent.transform.Find("Trigger_StorageRoom");
            if (storageTrigTrans == null)
            {
                GameObject storageTrigGo = new GameObject("Trigger_StorageRoom");
                storageTrigGo.transform.SetParent(triggersParent.transform, false);
                storageTrigGo.transform.position = new Vector3(0f, 1.2f, 10f);

                BoxCollider col = storageTrigGo.AddComponent<BoxCollider>();
                col.isTrigger = true;
                col.size = new Vector3(6f, 3f, 6f);

                var rbt = storageTrigGo.AddComponent<RoomBannerTrigger>();
                rbt.RoomTitle = "STORAGE ROOM";
                rbt.TipText = "Keep your eyes open. You're not alone.";
                sceneModified = true;
            }

            // Cellar Banner Trigger (Infrastructure placeholder)
            Transform cellarTrigTrans = triggersParent.transform.Find("Trigger_Cellar");
            if (cellarTrigTrans == null)
            {
                GameObject cellarTrigGo = new GameObject("Trigger_Cellar");
                cellarTrigGo.transform.SetParent(triggersParent.transform, false);
                cellarTrigGo.transform.position = new Vector3(0f, 1.2f, 20f);

                BoxCollider col = cellarTrigGo.AddComponent<BoxCollider>();
                col.isTrigger = true;
                col.size = new Vector3(6f, 3f, 6f);

                var rbt = cellarTrigGo.AddComponent<RoomBannerTrigger>();
                rbt.RoomTitle = "THE CELLAR";
                rbt.TipText = "Find the key. This is your final way out.";
                sceneModified = true;
            }

            if (sceneModified)
            {
                EditorSceneManager.MarkSceneDirty(activeScene);
                EditorSceneManager.SaveScene(activeScene);
                Debug.Log("[GuidedEscapeRoomSetup] EscapeRoom_Main updated with all guided flow systems and saved.");
            }
        }
    }
}
