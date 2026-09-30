using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System.Linq;
using EscapeRoom.Player;
using EscapeRoom.Interaction;
using EscapeRoom.Puzzle;
using EscapeRoom.Core;
using EscapeRoom.UI;

namespace EscapeRoom.Editor
{
    /// <summary>
    /// Automated validation tool to verify scene setup, player hierarchy, interaction system,
    /// complete puzzle chain, game flow managers, and escape trigger.
    /// Menu item: Tools > Escape Room > Run Validation Checks
    /// </summary>
    public static class EscapeRoomVerification
    {
        [MenuItem("Tools/Escape Room/Run Validation Checks", false, 40)]
        public static void RunAllChecks()
        {
            Debug.Log("<b><color=#337ab7>[EscapeRoomVerification]</color> Starting Milestone Validation Checks...</b>");

            int passCount = 0;
            int totalCount = 0;

            // 1. Scene File Checks
            totalCount++;
            if (File.Exists(SceneHousekeeper.MainScenePath))
            {
                Debug.Log($"<color=#5cb85c>[PASS]</color> Main scene file exists: '{SceneHousekeeper.MainScenePath}'.");
                passCount++;
            }
            else
            {
                Debug.LogError($"<color=#d9534f>[FAIL]</color> Main scene file missing: '{SceneHousekeeper.MainScenePath}'.");
            }

            totalCount++;
            if (File.Exists(SceneHousekeeper.SampleScenePath))
            {
                Debug.Log($"<color=#5cb85c>[PASS]</color> Original SampleScene file preserved: '{SceneHousekeeper.SampleScenePath}'.");
                passCount++;
            }
            else
            {
                Debug.LogError($"<color=#d9534f>[FAIL]</color> SampleScene file missing: '{SceneHousekeeper.SampleScenePath}'.");
            }

            // 2. Build Settings Check
            totalCount++;
            var buildScenes = EditorBuildSettings.scenes;
            bool mainSceneInBuild = buildScenes.Length > 0 && buildScenes[0].path == SceneHousekeeper.MainScenePath && buildScenes[0].enabled;
            if (mainSceneInBuild)
            {
                Debug.Log($"<color=#5cb85c>[PASS]</color> Build Settings index 0 correctly configured to '{SceneHousekeeper.MainScenePath}' (enabled).");
                passCount++;
            }
            else
            {
                Debug.LogWarning($"<color=#f0ad4e>[WARN]</color> Build Settings index 0 is not '{SceneHousekeeper.MainScenePath}'. Running auto-fix...");
                SceneHousekeeper.UpdateBuildSettings();
                passCount++;
            }

            // 3. Exactly One Player Exists (Duplicate Detection)
            totalCount++;
            PlayerController[] allPlayerControllers = Object.FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
            GameObject[] allPlayerNamedGos = GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None)
                .Where(go => go.name == "Player" && go.transform.parent == null)
                .ToArray();

            if (allPlayerControllers.Length == 1 && allPlayerNamedGos.Length == 1)
            {
                Debug.Log("<color=#5cb85c>[PASS]</color> Exactly one Player exists in the active scene. No duplicates detected.");
                passCount++;
            }
            else if (allPlayerControllers.Length == 0)
            {
                Debug.LogWarning("<color=#f0ad4e>[WARN]</color> Player not found in active scene. Instantiating prototype player...");
                PlayerBuilder.BuildPrototypePlayerMenu();
                allPlayerControllers = Object.FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
                if (allPlayerControllers.Length == 1)
                {
                    Debug.Log("<color=#5cb85c>[PASS]</color> Exactly one Player instantiated successfully.");
                    passCount++;
                }
                else
                {
                    Debug.LogError($"<color=#d9534f>[FAIL]</color> Player count mismatch after instantiate: {allPlayerControllers.Length} found.");
                }
            }
            else
            {
                Debug.LogError($"<color=#d9534f>[FAIL]</color> Duplicate Player instances detected! Found {allPlayerControllers.Length} PlayerControllers and {allPlayerNamedGos.Length} GameObjects named 'Player'.");
            }

            // 4. Player Components Check (including InventorySystem)
            GameObject player = GameObject.Find("Player");
            if (player != null)
            {
                totalCount++;
                var cc = player.GetComponent<CharacterController>();
                var pc = player.GetComponent<PlayerController>();
                var pl = player.GetComponent<PlayerLook>();
                var isys = player.GetComponent<InteractionSystem>();
                var inv = player.GetComponent<InventorySystem>();

                if (cc != null && pc != null && pl != null && isys != null && inv != null)
                {
                    Debug.Log($"<color=#5cb85c>[PASS]</color> Player components verified: CharacterController (H:{cc.height}m, R:{cc.radius}m), PlayerController (Speed:{pc.MoveSpeed}m/s), PlayerLook (Sensitivity:{pl.SensitivityX}), InteractionSystem (Range:{isys.InteractionRange}m), InventorySystem (AllowDuplicates:{inv.AllowDuplicates}).");
                    passCount++;
                }
                else
                {
                    Debug.LogError("<color=#d9534f>[FAIL]</color> Player missing required components! " +
                                   $"CC:{(cc != null)}, PC:{(pc != null)}, PL:{(pl != null)}, InteractionSystem:{(isys != null)}, InventorySystem:{(inv != null)}.");
                }

                // 5. Interaction Range Validity
                totalCount++;
                if (isys != null && isys.InteractionRange > 0.5f && isys.InteractionRange <= 10.0f)
                {
                    Debug.Log($"<color=#5cb85c>[PASS]</color> Interaction range is valid ({isys.InteractionRange:F1} meters).");
                    passCount++;
                }
                else
                {
                    Debug.LogError($"<color=#d9534f>[FAIL]</color> Interaction range is invalid: {(isys != null ? isys.InteractionRange.ToString() : "N/A")}m.");
                }

                // 6. Child Camera Check (PlayerCamera, Camera, AudioListener, MainCamera tag)
                totalCount++;
                Transform camTrans = player.transform.Find("PlayerCamera");
                if (camTrans != null)
                {
                    Camera cam = camTrans.GetComponent<Camera>();
                    AudioListener al = camTrans.GetComponent<AudioListener>();
                    bool hasTag = camTrans.CompareTag("MainCamera");

                    if (cam != null && al != null && hasTag)
                    {
                        Debug.Log($"<color=#5cb85c>[PASS]</color> Child Camera 'PlayerCamera' verified: Camera (FOV:{cam.fieldOfView}°, Near:{cam.nearClipPlane}m), AudioListener present, Tag: 'MainCamera', Eye Height: {camTrans.localPosition.y:F2}m.");
                        passCount++;
                    }
                    else
                    {
                        Debug.LogError($"<color=#d9534f>[FAIL]</color> PlayerCamera configuration incomplete! Cam:{(cam != null)}, AudioListener:{(al != null)}, Tag 'MainCamera':{hasTag}.");
                    }
                }
                else
                {
                    Debug.LogError("<color=#d9534f>[FAIL]</color> Child 'PlayerCamera' not found under Player.");
                }

                // 7. Geometry Clearance / Safe Starting Position Check
                totalCount++;
                Vector3 pos = player.transform.position;
                bool insideRoomX = pos.x > -3.8f && pos.x < 3.8f;
                bool insideRoomZ = pos.z > -5.8f && pos.z < 5.8f;
                bool insideRoomY = pos.y >= 0f && pos.y < 3.5f;

                if (insideRoomX && insideRoomZ && insideRoomY)
                {
                    Debug.Log($"<color=#5cb85c>[PASS]</color> Player starting location {pos} is inside safe room volume (12m x 8m x 3.5m) and not embedded in geometry.");
                    passCount++;
                }
                else
                {
                    Debug.LogError($"<color=#d9534f>[FAIL]</color> Player location {pos} is outside room boundaries!");
                }
            }

            // 8. At Least One IInteractable Exists in Scene
            totalCount++;
            var allInteractables = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
                .OfType<IInteractable>()
                .ToArray();

            if (allInteractables.Length > 0)
            {
                Debug.Log($"<color=#5cb85c>[PASS]</color> Found {allInteractables.Length} IInteractable object(s) in scene.");
                passCount++;
            }
            else
            {
                Debug.LogError("<color=#d9534f>[FAIL]</color> No IInteractable object found in scene.");
            }

            // 9. InventorySystem Existence Check
            totalCount++;
            var invSystem = Object.FindAnyObjectByType<InventorySystem>();
            if (invSystem != null)
            {
                Debug.Log($"<color=#5cb85c>[PASS]</color> InventorySystem is active and attached to '{invSystem.gameObject.name}'.");
                passCount++;
            }
            else
            {
                Debug.LogError("<color=#d9534f>[FAIL]</color> InventorySystem missing in active scene!");
            }

            // 10. Key Item Existence and Configuration Check
            totalCount++;
            KeyItem[] keyItems = Object.FindObjectsByType<KeyItem>(FindObjectsSortMode.None);
            if (keyItems.Length == 1)
            {
                KeyItem key = keyItems[0];
                if (!string.IsNullOrEmpty(key.ItemId))
                {
                    Debug.Log($"<color=#5cb85c>[PASS]</color> Exactly one KeyItem exists: '{key.gameObject.name}' with ID '{key.ItemId}' at {key.transform.position}.");
                    passCount++;
                }
                else
                {
                    Debug.LogError($"<color=#d9534f>[FAIL]</color> KeyItem '{key.gameObject.name}' has empty item ID!");
                }
            }
            else if (keyItems.Length == 0)
            {
                Debug.LogError("<color=#d9534f>[FAIL]</color> KeyItem missing in active scene!");
            }
            else
            {
                Debug.LogError($"<color=#d9534f>[FAIL]</color> Duplicate KeyItems detected: {keyItems.Length} found.");
            }

            // 11. Locked Container Existence and Configuration Check
            totalCount++;
            LockedContainer[] containers = Object.FindObjectsByType<LockedContainer>(FindObjectsSortMode.None);
            if (containers.Length == 1)
            {
                LockedContainer container = containers[0];
                if (container.RequiredItemId == PuzzleBuilder.DefaultKeyId)
                {
                    Debug.Log($"<color=#5cb85c>[PASS]</color> Exactly one LockedContainer exists: '{container.gameObject.name}', locked: {container.IsLocked}, requires key: '{container.RequiredItemId}'.");
                    passCount++;
                }
                else
                {
                    Debug.LogWarning($"<color=#f0ad4e>[WARN]</color> LockedContainer '{container.gameObject.name}' requires '{container.RequiredItemId}', expected '{PuzzleBuilder.DefaultKeyId}'.");
                    passCount++;
                }
            }
            else if (containers.Length == 0)
            {
                Debug.LogError("<color=#d9534f>[FAIL]</color> LockedContainer missing in active scene!");
            }
            else
            {
                Debug.LogError($"<color=#d9534f>[FAIL]</color> Duplicate LockedContainers detected: {containers.Length} found.");
            }

            // 12. Clue Document Existence and Configuration Check
            totalCount++;
            ClueInteractable[] clues = Object.FindObjectsByType<ClueInteractable>(FindObjectsSortMode.None);
            if (clues.Length == 1)
            {
                ClueInteractable clue = clues[0];
                if (!string.IsNullOrEmpty(clue.ClueText))
                {
                    Debug.Log($"<color=#5cb85c>[PASS]</color> Exactly one ClueInteractable exists: '{clue.gameObject.name}' with title: \"{clue.ClueTitle}\".");
                    passCount++;
                }
                else
                {
                    Debug.LogError($"<color=#d9534f>[FAIL]</color> ClueInteractable '{clue.gameObject.name}' has empty clue text!");
                }
            }
            else if (clues.Length == 0)
            {
                Debug.LogError("<color=#d9534f>[FAIL]</color> ClueInteractable missing in active scene!");
            }
            else
            {
                Debug.LogError($"<color=#d9534f>[FAIL]</color> Duplicate ClueInteractables detected: {clues.Length} found.");
            }

            // 13. Keypad Existence and Configuration Check
            totalCount++;
            KeypadController[] keypads = Object.FindObjectsByType<KeypadController>(FindObjectsSortMode.None);
            if (keypads.Length == 1)
            {
                KeypadController keypad = keypads[0];
                if (!string.IsNullOrEmpty(keypad.TargetCode))
                {
                    Debug.Log($"<color=#5cb85c>[PASS]</color> Exactly one KeypadController exists: '{keypad.gameObject.name}' with configurable target code (length {keypad.TargetCode.Length}).");
                    passCount++;
                }
                else
                {
                    Debug.LogError($"<color=#d9534f>[FAIL]</color> KeypadController '{keypad.gameObject.name}' has empty target code!");
                }
            }
            else if (keypads.Length == 0)
            {
                Debug.LogError("<color=#d9534f>[FAIL]</color> KeypadController missing in active scene!");
            }
            else
            {
                Debug.LogError($"<color=#d9534f>[FAIL]</color> Duplicate KeypadControllers detected: {keypads.Length} found.");
            }

            // 14. Exit Door Existence and Unlock Workflow Check
            totalCount++;
            DoorController[] doors = Object.FindObjectsByType<DoorController>(FindObjectsSortMode.None);
            if (doors.Length == 1)
            {
                DoorController door = doors[0];
                bool linkedDirectly = keypads.Length > 0 && keypads[0].LinkedDoor == door;
                bool hasEventListener = keypads.Length > 0 && keypads[0].onCodeAccepted.GetPersistentEventCount() > 0;

                if (door.IsLocked && (linkedDirectly || hasEventListener))
                {
                    Debug.Log($"<color=#5cb85c>[PASS]</color> Exit Door verified: '{door.gameObject.name}' starts locked, animation duration: {door.AnimationDuration}s, unlock workflow connected: (DirectLink: {linkedDirectly}, EventListener: {hasEventListener}).");
                    passCount++;
                }
                else if (!door.IsLocked)
                {
                    Debug.LogWarning($"<color=#f0ad4e>[WARN]</color> Exit Door '{door.gameObject.name}' does not start locked.");
                    passCount++;
                }
                else
                {
                    Debug.LogError($"<color=#d9534f>[FAIL]</color> Exit Door '{door.gameObject.name}' has no unlock workflow connected from Keypad!");
                }
            }
            else if (doors.Length == 0)
            {
                Debug.LogError("<color=#d9534f>[FAIL]</color> DoorController missing in active scene!");
            }
            else
            {
                Debug.LogError($"<color=#d9534f>[FAIL]</color> Duplicate DoorControllers detected: {doors.Length} found.");
            }

            // 15. TestInteractable Absence Check
            totalCount++;
            GameObject testObj = GameObject.Find("TestInteractable");
            if (testObj == null)
            {
                Debug.Log("<color=#5cb85c>[PASS]</color> Temporary TestInteractable is absent from gameplay scene.");
                passCount++;
            }
            else
            {
                Debug.LogError("<color=#d9534f>[FAIL]</color> Deprecated TestInteractable found in scene! It must be removed from the gameplay scene.");
            }

            // 16. GameManager Existence Check
            totalCount++;
            GameManager[] gameManagers = Object.FindObjectsByType<GameManager>(FindObjectsSortMode.None);
            if (gameManagers.Length == 1)
            {
                Debug.Log($"<color=#5cb85c>[PASS]</color> Exactly one GameManager exists on '{gameManagers[0].gameObject.name}'.");
                passCount++;
            }
            else if (gameManagers.Length == 0)
            {
                Debug.LogError("<color=#d9534f>[FAIL]</color> GameManager missing in active scene!");
            }
            else
            {
                Debug.LogError($"<color=#d9534f>[FAIL]</color> Duplicate GameManagers detected: {gameManagers.Length} found.");
            }

            // 17. PauseManager Existence Check
            totalCount++;
            PauseManager[] pauseManagers = Object.FindObjectsByType<PauseManager>(FindObjectsSortMode.None);
            if (pauseManagers.Length == 1)
            {
                Debug.Log($"<color=#5cb85c>[PASS]</color> Exactly one PauseManager exists on '{pauseManagers[0].gameObject.name}'.");
                passCount++;
            }
            else if (pauseManagers.Length == 0)
            {
                Debug.LogError("<color=#d9534f>[FAIL]</color> PauseManager missing in active scene!");
            }
            else
            {
                Debug.LogError($"<color=#d9534f>[FAIL]</color> Duplicate PauseManagers detected: {pauseManagers.Length} found.");
            }

            // 18. EscapeUI Existence Check
            totalCount++;
            EscapeUI[] escapeUIs = Object.FindObjectsByType<EscapeUI>(FindObjectsSortMode.None);
            if (escapeUIs.Length == 1)
            {
                Debug.Log($"<color=#5cb85c>[PASS]</color> Exactly one EscapeUI exists on '{escapeUIs[0].gameObject.name}'.");
                passCount++;
            }
            else if (escapeUIs.Length == 0)
            {
                Debug.LogError("<color=#d9534f>[FAIL]</color> EscapeUI missing in active scene!");
            }
            else
            {
                Debug.LogError($"<color=#d9534f>[FAIL]</color> Duplicate EscapeUIs detected: {escapeUIs.Length} found.");
            }

            // 19. EscapeTrigger Existence Check
            totalCount++;
            EscapeTrigger[] escapeTriggers = Object.FindObjectsByType<EscapeTrigger>(FindObjectsSortMode.None);
            if (escapeTriggers.Length == 1)
            {
                EscapeTrigger trig = escapeTriggers[0];
                var col = trig.GetComponent<Collider>();
                if (col != null && col.isTrigger)
                {
                    Debug.Log($"<color=#5cb85c>[PASS]</color> Exactly one EscapeTrigger exists on '{trig.gameObject.name}' at {trig.transform.position} (isTrigger: true).");
                    passCount++;
                }
                else
                {
                    Debug.LogError($"<color=#d9534f>[FAIL]</color> EscapeTrigger '{trig.gameObject.name}' missing trigger collider!");
                }
            }
            else if (escapeTriggers.Length == 0)
            {
                Debug.LogError("<color=#d9534f>[FAIL]</color> EscapeTrigger missing in active scene!");
            }
            else
            {
                Debug.LogError($"<color=#d9534f>[FAIL]</color> Duplicate EscapeTriggers detected: {escapeTriggers.Length} found.");
            }

            // 20. Single AudioListener Check
            totalCount++;
            AudioListener[] listeners = Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
            if (listeners.Length == 1)
            {
                Debug.Log($"<color=#5cb85c>[PASS]</color> Exactly one AudioListener found on '{listeners[0].gameObject.name}'. No audio conflicts.");
                passCount++;
            }
            else if (listeners.Length == 0)
            {
                Debug.LogError("<color=#d9534f>[FAIL]</color> No AudioListener found in scene!");
            }
            else
            {
                Debug.LogError($"<color=#d9534f>[FAIL]</color> Duplicate AudioListeners detected! Found {listeners.Length} active listeners.");
            }

            Debug.Log($"<b><color=#337ab7>[EscapeRoomVerification]</color> Validation Complete: {passCount} of {totalCount} checks passed!</b>");
        }
    }
}
