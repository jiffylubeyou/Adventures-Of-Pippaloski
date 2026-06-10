using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// One-click setup for the pirate ship enemies:
///   1. Adds RaftHealth to the Low Poly Raft prefab (health bar + sinking).
///   2. Adds a hull BoxCollider and PirateShipAI to the 99_Ship_L1 prefab,
///      sized from its renderers, with water level matched to the raft's.
///   3. Repositions any ship already in the scene that is stuck on land, and
///      spawns ships in open water until there are 3 total.
///
/// Run via Tools > Pippaloski > Setup Pirate Ships. Safe to run multiple times.
/// </summary>
public static class SetupPirateShips
{
    private const string ShipPrefabPath = "Assets/GeneratedAssets/Prefabs/99_Ship_L1.prefab";
    private const string RaftPrefabPath = "Assets/Low Poly Boats & Raft/Low Poly Raft/Low Poly Prefab/Low Poly Raft.prefab";
    private const int    TargetShipCount = 3;

    [MenuItem("Tools/Pippaloski/Setup Pirate Ships")]
    public static void Setup()
    {
        float waterLevel = SetupRaftPrefab(out bool raftHealthAdded);
        bool shipConfigured = SetupShipPrefab(waterLevel, out float hullYOffset);
        int repositioned, spawned;
        PopulateScene(waterLevel, hullYOffset, out repositioned, out spawned);

        AssetDatabase.SaveAssets();

        EditorUtility.DisplayDialog("Setup Pirate Ships",
            "Done!\n\n" +
            $"• Raft prefab: {(raftHealthAdded ? "RaftHealth added" : "RaftHealth already present")}.\n" +
            $"• Ship prefab: {(shipConfigured ? "PirateShipAI + hull collider configured" : "already configured")}.\n" +
            $"• Scene: {repositioned} ship(s) moved off land, {spawned} new ship(s) spawned.\n\n" +
            "Press Play, board the raft, and watch your back.",
            "OK");
    }

    // ── Raft prefab ───────────────────────────────────────────────────────────

    private static float SetupRaftPrefab(out bool added)
    {
        added = false;
        float waterLevel = 4f;

        var root = PrefabUtility.LoadPrefabContents(RaftPrefabPath);
        try
        {
            var controller = root.GetComponent<RaftController>();
            if (controller != null)
            {
                var so = new SerializedObject(controller);
                var prop = so.FindProperty("waterLevel");
                if (prop != null) waterLevel = prop.floatValue;
            }

            if (root.GetComponent<RaftHealth>() == null)
            {
                root.AddComponent<RaftHealth>();
                added = true;
                PrefabUtility.SaveAsPrefabAsset(root, RaftPrefabPath);
            }
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        return waterLevel;
    }

    // ── Ship prefab ───────────────────────────────────────────────────────────

    private static bool SetupShipPrefab(float waterLevel, out float hullYOffset)
    {
        bool changed = false;
        hullYOffset = 0f;

        var root = PrefabUtility.LoadPrefabContents(ShipPrefabPath);
        try
        {
            // Combined world bounds of the visible hull/masts
            var renderers = root.GetComponentsInChildren<Renderer>(false);
            Bounds bounds = new Bounds(root.transform.position, Vector3.one);
            bool first = true;
            foreach (var r in renderers)
            {
                if (first) { bounds = r.bounds; first = false; }
                else bounds.Encapsulate(r.bounds);
            }

            // Hull collider: bottom ~35% of the total bounds (the rest is masts
            // and sails), slightly narrower than the yards.
            var box = root.GetComponent<BoxCollider>();
            if (box == null)
            {
                box = root.AddComponent<BoxCollider>();
                changed = true;
            }

            float hullHeight = bounds.size.y * 0.35f;
            Vector3 worldCenter = new Vector3(bounds.center.x, bounds.min.y + hullHeight * 0.5f, bounds.center.z);
            Vector3 worldSize   = new Vector3(bounds.size.x * 0.7f, hullHeight, bounds.size.z * 0.95f);

            Vector3 scale = root.transform.lossyScale;
            box.center = root.transform.InverseTransformPoint(worldCenter);
            box.size   = new Vector3(worldSize.x / scale.x, worldSize.y / scale.y, worldSize.z / scale.z);

            // Sit the keel a believable depth below the waterline
            float pivotToBottom = root.transform.position.y - bounds.min.y;
            float draft = Mathf.Clamp(bounds.size.z * 0.06f, 0.8f, 2.5f);
            hullYOffset = pivotToBottom - draft;

            var ai = root.GetComponent<PirateShipAI>();
            if (ai == null)
            {
                ai = root.AddComponent<PirateShipAI>();
                changed = true;
            }
            ai.waterLevel  = waterLevel;
            ai.hullYOffset = hullYOffset;
            ai.groundLayer = LayerMask.GetMask("Island");

            PrefabUtility.SaveAsPrefabAsset(root, ShipPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        return changed;
    }

    // ── Scene population ──────────────────────────────────────────────────────

    private static void PopulateScene(float waterLevel, float hullYOffset,
        out int repositioned, out int spawned)
    {
        repositioned = 0;
        spawned = 0;

        int islandMask = LayerMask.GetMask("Island");
        var raft = Object.FindFirstObjectByType<RaftController>();
        Vector3 center = raft != null ? raft.transform.position : Vector3.zero;

        // Fix any existing ships that are parked on land
        var existing = Object.FindObjectsByType<PirateShipAI>(FindObjectsSortMode.None);
        foreach (var ship in existing)
        {
            Vector3 p = ship.transform.position;
            if (IsOnLand(new Vector3(p.x, waterLevel, p.z), islandMask))
            {
                if (FindOpenWater(center, waterLevel, islandMask, out Vector3 spot))
                {
                    Undo.RecordObject(ship.transform, "Move Pirate Ship Off Land");
                    ship.transform.position = new Vector3(spot.x, waterLevel + hullYOffset, spot.z);
                    repositioned++;
                }
            }
        }

        // Top up to the target count
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ShipPrefabPath);
        int toSpawn = TargetShipCount - existing.Length;
        for (int i = 0; i < toSpawn; i++)
        {
            if (!FindOpenWater(center, waterLevel, islandMask, out Vector3 spot)) break;

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = $"Pirate Ship {existing.Length + spawned + 1}";
            instance.transform.position = new Vector3(spot.x, waterLevel + hullYOffset, spot.z);
            instance.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            Undo.RegisterCreatedObjectUndo(instance, "Spawn Pirate Ship");
            spawned++;
        }

        if (repositioned > 0 || spawned > 0)
        {
            var scene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
    }

    private static bool IsOnLand(Vector3 pos, int islandMask)
    {
        return Physics.CheckBox(pos, new Vector3(15f, 25f, 15f),
            Quaternion.identity, islandMask, QueryTriggerInteraction.Ignore);
    }

    private static bool FindOpenWater(Vector3 center, float waterLevel, int islandMask, out Vector3 result)
    {
        for (int attempt = 0; attempt < 200; attempt++)
        {
            float angle  = Random.Range(0f, Mathf.PI * 2f);
            float radius = Random.Range(80f, 180f);
            var candidate = new Vector3(
                center.x + Mathf.Cos(angle) * radius,
                waterLevel,
                center.z + Mathf.Sin(angle) * radius);

            if (IsOnLand(candidate, islandMask)) continue;

            // Keep ships spread out and off the raft's doorstep
            bool tooClose = false;
            foreach (var other in Object.FindObjectsByType<PirateShipAI>(FindObjectsSortMode.None))
            {
                if (Vector3.Distance(other.transform.position, candidate) < 60f)
                {
                    tooClose = true;
                    break;
                }
            }
            if (tooClose) continue;

            result = candidate;
            return true;
        }

        result = center;
        return false;
    }
}
