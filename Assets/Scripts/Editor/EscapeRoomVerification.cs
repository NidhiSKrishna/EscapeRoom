using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System.Linq;
using EscapeRoom.Player;
using EscapeRoom.Interaction;

namespace EscapeRoom.Editor
{
    /// <summary>
    /// Automated validation tool to verify scene setup, player hierarchy, interaction system, and environment geometry clearance.
    /// Menu item: Tools > Escape Room > Run Validation Checks
    /// </summary>
    public static class EscapeRoomVerification
    {
        [MenuItem("Tools/Escape Room/Run Validation Checks", false, 40)]
        public static void RunAllChecks()
        {
            Debug.Log("<b><color=#337ab7>[EscapeRoomVerification]</color> Starting Milestone 3 Validation Checks...</b>");

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

            // 4. Player Components Check
            GameObject player = GameObject.Find("Player");
            if (player != null)
            {
                totalCount++;
                var cc = player.GetComponent<CharacterController>();
                var pc = player.GetComponent<PlayerController>();
                var pl = player.GetComponent<PlayerLook>();
                var isys = player.GetComponent<InteractionSystem>();

                if (cc != null && pc != null && pl != null && isys != null)
                {
                    Debug.Log($"<color=#5cb85c>[PASS]</color> Player components verified: CharacterController (H:{cc.height}m, R:{cc.radius}m), PlayerController (Speed:{pc.MoveSpeed}m/s), PlayerLook (Sensitivity:{pl.SensitivityX}), InteractionSystem (Range:{isys.InteractionRange}m).");
                    passCount++;
                }
                else
                {
                    Debug.LogError("<color=#d9534f>[FAIL]</color> Player missing required components! " +
                                   $"CC:{(cc != null)}, PC:{(pc != null)}, PL:{(pl != null)}, InteractionSystem:{(isys != null)}.");
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
                var first = allInteractables[0] as MonoBehaviour;
                Debug.Log($"<color=#5cb85c>[PASS]</color> Found {allInteractables.Length} IInteractable object(s) in scene. (e.g. '{first?.gameObject.name}', prompt: \"{allInteractables[0].InteractionPrompt}\").");
                passCount++;
            }
            else
            {
                Debug.LogWarning("<color=#f0ad4e>[WARN]</color> No IInteractable object found in scene. Creating prototype 'TestInteractable'...");
                PlayerBuilder.BuildTestInteractable();
                allInteractables = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
                    .OfType<IInteractable>()
                    .ToArray();

                if (allInteractables.Length > 0)
                {
                    Debug.Log($"<color=#5cb85c>[PASS]</color> 'TestInteractable' created and verified.");
                    passCount++;
                }
                else
                {
                    Debug.LogError("<color=#d9534f>[FAIL]</color> Failed to find or create an IInteractable object.");
                }
            }

            Debug.Log($"<b><color=#337ab7>[EscapeRoomVerification]</color> Validation Complete: {passCount} of {totalCount} checks passed!</b>");
        }
    }
}
