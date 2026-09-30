using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using EscapeRoom.Interaction;
using EscapeRoom.Puzzle;
using EscapeRoom.Core;
using EscapeRoom.UI;
using UnityEngine.ProBuilder;

namespace EscapeRoom.Editor
{
    /// <summary>
    /// Automated builder for constructing the complete prototype puzzle chain and game-flow systems:
    /// 1. World Key (ID: room_key) on shelf
    /// 2. Locked Container on table (requires room_key)
    /// 3. Clue Document inside container (revealed on open, hints code 8431)
    /// 4. Keypad Terminal beside exit doorway (configurable code: 8431)
    /// 5. Exit Door inside doorway frame (unlocked by keypad)
    /// 6. Escape Trigger beyond doorway (calls GameManager.CompleteEscape)
    /// 7. Core Managers: GameManager, PauseManager, EscapeUI, PauseUI, FeedbackHUD
    /// Menu Item: Tools > Escape Room > Build Prototype Puzzle
    /// </summary>
    public static class PuzzleBuilder
    {
        public const string KeyName = "Key_Room";
        public const string ContainerName = "Container_Lockbox";
        public const string ClueName = "Clue_Document";
        public const string KeypadName = "Terminal_Keypad";
        public const string ExitDoorName = "Exit_Door";
        public const string EscapeTriggerName = "Escape_Trigger";
        public const string GameManagersName = "GameManagers";

        public const string DefaultKeyId = "room_key";
        public const string DefaultKeypadCode = "8431";

        [MenuItem("Tools/Escape Room/Build Prototype Puzzle", false, 20)]
        public static void BuildPrototypePuzzleMenu()
        {
            BuildPuzzle(forceRebuild: false);
        }

        [MenuItem("Tools/Escape Room/Rebuild Prototype Puzzle (Force)", false, 21)]
        public static void RebuildPrototypePuzzleMenu()
        {
            BuildPuzzle(forceRebuild: true);
        }

        public static void BuildPuzzle(bool forceRebuild = false)
        {
            Debug.Log("<b><color=#337ab7>[PuzzleBuilder]</color> Building Prototype Puzzle Chain & Game Flow...</b>");

            // Ensure active scene is EscapeRoom_Main
            var activeScene = EditorSceneManager.GetActiveScene();

            // Locate or create parent container under Environment
            GameObject envGo = GameObject.Find("Environment");
            if (envGo == null)
            {
                Debug.LogWarning("[PuzzleBuilder] 'Environment' root not found. Building room first...");
                EscapeRoomBuilder.BuildRoom(RoomConfiguration.CreatePrototypeDefault());
                envGo = GameObject.Find("Environment");
            }

            Transform propsTrans = envGo.transform.Find("Props");
            Transform puzzleParent = propsTrans != null ? propsTrans.Find("Puzzle") : null;

            if (puzzleParent == null)
            {
                GameObject puzzleContainer = new GameObject("Puzzle");
                if (propsTrans != null)
                {
                    puzzleContainer.transform.SetParent(propsTrans, false);
                }
                else
                {
                    puzzleContainer.transform.SetParent(envGo.transform, false);
                }
                puzzleParent = puzzleContainer.transform;
                Undo.RegisterCreatedObjectUndo(puzzleContainer, "Create Puzzle Root");
            }

            // Remove temporary test object if present
            RemoveDeprecatedTestObjects();

            // Ensure Player has InventorySystem
            EnsurePlayerInventory();

            // Ensure Core Game-Flow Managers (GameManager, PauseManager, EscapeUI, PauseUI, FeedbackHUD)
            EnsureGameManagers();

            // Materials
            Material keyMat = EscapeRoomBuilder.GetOrCreateMaterial("M_Proto_Key", new Color(0.96f, 0.78f, 0.18f), 0.75f, 0.40f);
            Material boxMat = EscapeRoomBuilder.GetOrCreateMaterial("M_Proto_Lockbox", new Color(0.24f, 0.25f, 0.28f), 0.50f, 0.30f);
            Material clueMat = EscapeRoomBuilder.GetOrCreateMaterial("M_Proto_Clue", new Color(0.93f, 0.89f, 0.78f), 0.05f, 0.10f);
            Material keypadMat = EscapeRoomBuilder.GetOrCreateMaterial("M_Proto_Keypad", new Color(0.18f, 0.20f, 0.24f), 0.60f, 0.35f);
            Material doorMat = EscapeRoomBuilder.GetOrCreateMaterial("M_Proto_Door", new Color(0.38f, 0.26f, 0.17f), 0.20f, 0.20f);

            // 1. Build Exit Door
            DoorController doorCtrl = BuildExitDoor(puzzleParent, doorMat, forceRebuild);

            // 2. Build Keypad Terminal (connected directly to Door as authoritative path)
            KeypadController keypadCtrl = BuildKeypad(puzzleParent, keypadMat, doorCtrl, forceRebuild);

            // 3. Build Escape Trigger (beyond exit door)
            EscapeTrigger escapeTrigger = BuildEscapeTrigger(puzzleParent, forceRebuild);

            // 4. Build Clue Document
            ClueInteractable clue = BuildClue(puzzleParent, clueMat, forceRebuild);

            // 5. Build Locked Container (housing the clue)
            LockedContainer container = BuildLockedContainer(puzzleParent, boxMat, clue, forceRebuild);

            // 6. Build Key Item (on shelf)
            KeyItem key = BuildKey(puzzleParent, keyMat, forceRebuild);

            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);

            Debug.Log("<b><color=#5cb85c>[PuzzleBuilder] Prototype Puzzle Chain Built Successfully!</color></b>\n" +
                      $" - Key: '{key.gameObject.name}' on shelf at {key.transform.position} (ID: {key.ItemId})\n" +
                      $" - Container: '{container.gameObject.name}' on table at {container.transform.position} (Requires: {container.RequiredItemId})\n" +
                      $" - Clue: '{clue.gameObject.name}' inside container\n" +
                      $" - Keypad: '{keypadCtrl.gameObject.name}' at {keypadCtrl.transform.position} (Code: {keypadCtrl.TargetCode})\n" +
                      $" - Exit Door: '{doorCtrl.gameObject.name}' at {doorCtrl.transform.position}\n" +
                      $" - Escape Trigger: '{escapeTrigger.gameObject.name}' at {escapeTrigger.transform.position}");
        }

        private static void RemoveDeprecatedTestObjects()
        {
            GameObject testObj = GameObject.Find("TestInteractable");
            if (testObj != null)
            {
                Undo.DestroyObjectImmediate(testObj);
                Debug.Log("[PuzzleBuilder] Removed deprecated TestInteractable from scene.");
            }
        }

        private static void EnsureGameManagers()
        {
            GameObject managersGo = GameObject.Find(GameManagersName);
            if (managersGo == null)
            {
                managersGo = new GameObject(GameManagersName);
                Undo.RegisterCreatedObjectUndo(managersGo, "Create GameManagers");
            }

            if (managersGo.GetComponent<GameManager>() == null)
            {
                managersGo.AddComponent<GameManager>();
            }

            if (managersGo.GetComponent<PauseManager>() == null)
            {
                managersGo.AddComponent<PauseManager>();
            }

            if (managersGo.GetComponent<EscapeUI>() == null)
            {
                managersGo.AddComponent<EscapeUI>();
            }

            if (managersGo.GetComponent<PauseUI>() == null)
            {
                managersGo.AddComponent<PauseUI>();
            }

            if (managersGo.GetComponent<FeedbackHUD>() == null)
            {
                managersGo.AddComponent<FeedbackHUD>();
            }

            Debug.Log("[PuzzleBuilder] Core GameManagers verified (GameManager, PauseManager, EscapeUI, PauseUI, FeedbackHUD).");
        }

        private static void EnsurePlayerInventory()
        {
            GameObject player = GameObject.Find("Player");
            if (player != null && player.GetComponent<InventorySystem>() == null)
            {
                player.AddComponent<InventorySystem>();
                Debug.Log("[PuzzleBuilder] Attached InventorySystem to Player.");
            }
        }

        private static KeyItem BuildKey(Transform parent, Material mat, bool forceRebuild)
        {
            GameObject existing = GameObject.Find(KeyName);
            if (existing != null)
            {
                if (!forceRebuild) return existing.GetComponent<KeyItem>();
                Undo.DestroyObjectImmediate(existing);
            }

            // Positioned on the shelf at X = -3.55m, Y = 1.25m (middle shelf ledge), Z = -2.00m
            Vector3 keyPos = new Vector3(-3.55f, 1.24f, -2.00f);
            Vector3 keySize = new Vector3(0.12f, 0.04f, 0.22f);

            var pb = EscapeRoomBuilder.CreateProBuilderCube(KeyName, parent, keyPos, keySize, Quaternion.Euler(0f, 25f, 0f), mat);
            KeyItem keyItem = pb.gameObject.AddComponent<KeyItem>();
            keyItem.ConfigureItem(DefaultKeyId, "Room Key", "A solid brass key that fits an old lockbox.");

            return keyItem;
        }

        private static LockedContainer BuildLockedContainer(Transform parent, Material mat, ClueInteractable clue, bool forceRebuild)
        {
            GameObject existing = GameObject.Find(ContainerName);
            if (existing != null)
            {
                if (!forceRebuild) return existing.GetComponent<LockedContainer>();
                Undo.DestroyObjectImmediate(existing);
            }

            // Placed on the table: Table top is at Y = 0.85m, pos at X = 1.50m, Z = -0.40m
            Vector3 rootPos = new Vector3(1.50f, 0.85f, -0.40f);
            GameObject containerRoot = new GameObject(ContainerName);
            containerRoot.transform.SetParent(parent, false);
            containerRoot.transform.position = rootPos;
            Undo.RegisterCreatedObjectUndo(containerRoot, "Create Locked Container");

            // Box base
            Vector3 baseSize = new Vector3(0.44f, 0.20f, 0.32f);
            Vector3 basePos = new Vector3(rootPos.x, rootPos.y + baseSize.y * 0.5f, rootPos.z);
            var basePb = EscapeRoomBuilder.CreateProBuilderCube("Lockbox_Base", containerRoot.transform, basePos, baseSize, Quaternion.identity, mat);

            // Lid hinge root (pivot at top-back edge of base)
            Vector3 hingePos = new Vector3(rootPos.x, rootPos.y + baseSize.y, rootPos.z + baseSize.z * 0.5f);
            GameObject lidHinge = new GameObject("Lockbox_LidHinge");
            lidHinge.transform.SetParent(containerRoot.transform, false);
            lidHinge.transform.position = hingePos;

            // Lid slab child (offset from hinge so it covers the base)
            Vector3 lidSize = new Vector3(0.46f, 0.04f, 0.34f);
            Vector3 lidPos = new Vector3(hingePos.x, hingePos.y + lidSize.y * 0.5f, hingePos.z - lidSize.z * 0.5f);
            var lidPb = EscapeRoomBuilder.CreateProBuilderCube("Lockbox_LidSlab", lidHinge.transform, lidPos, lidSize, Quaternion.identity, mat);

            // Add LockedContainer to containerRoot
            BoxCollider rootCollider = containerRoot.AddComponent<BoxCollider>();
            rootCollider.size = new Vector3(0.50f, 0.28f, 0.40f);
            rootCollider.center = new Vector3(0f, 0.14f, 0f);

            LockedContainer locked = containerRoot.AddComponent<LockedContainer>();
            locked.RequiredItemId = DefaultKeyId;
            locked.LidTransform = lidHinge.transform;
            locked.ContentsObject = clue != null ? clue.gameObject : null;

            if (clue != null)
            {
                clue.transform.SetParent(containerRoot.transform, true);
                clue.gameObject.SetActive(false); // Hidden until container is unlocked
            }

            return locked;
        }

        private static ClueInteractable BuildClue(Transform parent, Material mat, bool forceRebuild)
        {
            GameObject existing = GameObject.Find(ClueName);
            if (existing != null)
            {
                if (!forceRebuild) return existing.GetComponent<ClueInteractable>();
                Undo.DestroyObjectImmediate(existing);
            }

            // Inside lockbox at table height: Y = 0.96m
            Vector3 cluePos = new Vector3(1.50f, 0.96f, -0.40f);
            Vector3 clueSize = new Vector3(0.26f, 0.015f, 0.18f);

            var pb = EscapeRoomBuilder.CreateProBuilderCube(ClueName, parent, cluePos, clueSize, Quaternion.identity, mat);
            ClueInteractable clue = pb.gameObject.AddComponent<ClueInteractable>();
            clue.ClueTitle = "Facility Security Override Note";
            clue.ClueText =
                "FACILITY OVERRIDE PROTOCOL\n\n" +
                "In case of lockouts, the door bypass code is calculated from facility blueprint metrics:\n\n" +
                "  1. Room Width (meters): 8\n" +
                "  2. Total Wooden Crates: 4\n" +
                "  3. Room Height rounded: 3\n" +
                "  4. Exit Doorway Width: 1\n\n" +
                "Assemble the four digits in sequence to authorize emergency release.";

            return clue;
        }

        private static KeypadController BuildKeypad(Transform parent, Material mat, DoorController linkedDoor, bool forceRebuild)
        {
            GameObject existing = GameObject.Find(KeypadName);
            if (existing != null)
            {
                if (!forceRebuild)
                {
                    var existingCtrl = existing.GetComponent<KeypadController>();
                    if (existingCtrl != null && linkedDoor != null)
                    {
                        existingCtrl.LinkedDoor = linkedDoor;
                    }
                    return existingCtrl;
                }
                Undo.DestroyObjectImmediate(existing);
            }

            // Mounted on the front wall beside the doorway:
            // Doorway is centered at X = 0, wall is at Z = 6.0m.
            // Place at X = 0.90m, Y = 1.45m, Z = 5.86m, facing -Z (into room: Y rotation 180).
            Vector3 pos = new Vector3(0.90f, 1.45f, 5.86f);
            Vector3 size = new Vector3(0.24f, 0.36f, 0.08f);
            Quaternion rot = Quaternion.Euler(0f, 180f, 0f);

            var pb = EscapeRoomBuilder.CreateProBuilderCube(KeypadName, parent, pos, size, rot, mat);
            KeypadController keypad = pb.gameObject.AddComponent<KeypadController>();
            keypad.TargetCode = DefaultKeypadCode;
            // One authoritative unlock path via direct link
            keypad.LinkedDoor = linkedDoor;

            return keypad;
        }

        private static DoorController BuildExitDoor(Transform parent, Material mat, bool forceRebuild)
        {
            GameObject existing = GameObject.Find(ExitDoorName);
            if (existing != null)
            {
                if (!forceRebuild) return existing.GetComponent<DoorController>();
                Undo.DestroyObjectImmediate(existing);
            }

            // The doorway is at Z = 6.0m, inner opening width = 1.04m, height = 2.26m
            // Left jamb inner edge is at X = -0.52m.
            // Place door hinge root at left jamb inner edge: X = -0.52m, Y = 0.02m, Z = 6.00m.
            Vector3 hingePos = new Vector3(-0.52f, 0.02f, 6.00f);
            GameObject doorRoot = new GameObject(ExitDoorName);
            doorRoot.transform.SetParent(parent, false);
            doorRoot.transform.position = hingePos;
            Undo.RegisterCreatedObjectUndo(doorRoot, "Create Exit Door Root");

            // Door leaf slab (centered locally from hinge)
            float doorW = 1.04f;
            float doorH = 2.26f;
            float doorThk = 0.06f;

            Vector3 slabWorldPos = new Vector3(hingePos.x + doorW * 0.5f, hingePos.y + doorH * 0.5f, hingePos.z);
            Vector3 slabSize = new Vector3(doorW, doorH, doorThk);
            var slabPb = EscapeRoomBuilder.CreateProBuilderCube("Door_Slab", doorRoot.transform, slabWorldPos, slabSize, Quaternion.identity, mat);

            // Add DoorController to doorRoot
            BoxCollider col = doorRoot.AddComponent<BoxCollider>();
            col.size = new Vector3(doorW, doorH, doorThk * 2.0f);
            col.center = new Vector3(doorW * 0.5f, doorH * 0.5f, 0f);

            DoorController door = doorRoot.AddComponent<DoorController>();
            door.HingeTransform = doorRoot.transform;
            door.OpenEulerAngles = new Vector3(0f, -95f, 0f);
            door.AnimationDuration = 1.4f;
            door.IsLocked = true;

            return door;
        }

        private static EscapeTrigger BuildEscapeTrigger(Transform parent, bool forceRebuild)
        {
            GameObject existing = GameObject.Find(EscapeTriggerName);
            if (existing != null)
            {
                if (!forceRebuild) return existing.GetComponent<EscapeTrigger>();
                Undo.DestroyObjectImmediate(existing);
            }

            // Placed just outside the exit doorway (threshold at Z = 6.0m, trigger centered at Z = 6.8m)
            Vector3 triggerPos = new Vector3(0f, 1.15f, 6.80f);
            GameObject triggerGo = new GameObject(EscapeTriggerName);
            triggerGo.transform.SetParent(parent, false);
            triggerGo.transform.position = triggerPos;
            Undo.RegisterCreatedObjectUndo(triggerGo, "Create Escape Trigger");

            BoxCollider box = triggerGo.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(1.80f, 2.40f, 1.20f);

            EscapeTrigger trigger = triggerGo.AddComponent<EscapeTrigger>();
            return trigger;
        }
    }
}
