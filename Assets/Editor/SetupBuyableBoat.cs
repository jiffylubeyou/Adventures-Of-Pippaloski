using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Builds the buyable boats and wires Lil Zoinks' shop to sell them.
///
/// Each boat in the Boats table below gets a configured prefab under
/// Assets/GeneratedAssets/Prefabs/ — the source model + MeshColliders + a
/// RaftController carrying the shared "buyable boat" profile (Shift-sprint,
/// hide-rider, speed/turn, Island land mask, gated behind its own owned-flag)
/// + a RaftHealth so pirates can sink it. The boat's geometry fields
/// (hullYOffset / boardRange / disembarkOffset) are auto-computed from its
/// bounds as STARTING POINTS — tune them in the inspector afterward.
///
/// Then Lil Zoinks gets a SenseiShop with one item per boat (granting that
/// boat's flag) and his "I want to buy a boat" line is set to open the shop.
///
/// Run via Tools > Pippaloski > Setup Buyable Boats. Safe to run repeatedly:
/// a boat prefab that already exists is LEFT ALONE (so hand-tuning survives),
/// and shop items are only added if missing. To add a new boat, add a row to
/// the Boats table and a flag to GameState — no other code changes.
/// </summary>
public static class SetupBuyableBoat
{
    private const string LilZoinksPath =
        "Assets/GeneratedAssets/Prefabs/Lil Zoinks.prefab";
    private const int IslandLayer = 8;

    private struct BoatDef
    {
        public string modelPath;     // source art prefab
        public string outputPath;    // configured prefab we generate
        public string displayName;   // name in-game / in the shop
        public string ownedFlag;     // GameState flag granted on purchase + required to board
        public int    price;
        public string description;   // shop blurb
    }

    // Add a row here (plus a GameState flag) to introduce another buyable boat.
    private static readonly BoatDef[] Boats =
    {
        new BoatDef
        {
            modelPath   = "Assets/Low-Poly 3D Boat Model/Prefab/boat Prefab.prefab",
            outputPath  = "Assets/GeneratedAssets/Prefabs/Chug Boat.prefab",
            displayName = "Chug Boat",
            ownedFlag   = GameState.SpeedboatOwned,
            price       = 250,
            description = "A trusty chug boat. Sturdier than that flimsy raft.",
        },
        new BoatDef
        {
            modelPath   = "Assets/MedievalShip/Prefabs/Galleon.prefab",
            outputPath  = "Assets/GeneratedAssets/Prefabs/Galleon.prefab",
            displayName = "Galleon",
            ownedFlag   = GameState.GalleonOwned,
            price       = 250,
            description = "A mighty medieval galleon. The pride of the fleet.",
        },
    };

    [MenuItem("Tools/Pippaloski/Setup Buyable Boats")]
    public static void Setup()
    {
        var sb = new StringBuilder();

        sb.AppendLine("BOATS:");
        foreach (var def in Boats)
            sb.AppendLine("  • " + BuildBoatPrefab(def));

        sb.AppendLine();
        sb.AppendLine("SHOP:");
        sb.AppendLine(WireLilZoinksShop());

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Setup Buyable Boats",
            sb.ToString() +
            "\nNewly created boats: drag them onto the water and tune " +
            "seatOffset / hullYOffset / disembarkOffset in the inspector.",
            "OK");
    }

    // ── Build one configured boat prefab ──────────────────────────────────────

    private static string BuildBoatPrefab(BoatDef def)
    {
        // Never clobber a boat that already exists — it may be hand-tuned.
        if (AssetDatabase.LoadAssetAtPath<GameObject>(def.outputPath) != null)
            return $"{def.displayName}: already exists, left as-is";

        var source = AssetDatabase.LoadAssetAtPath<GameObject>(def.modelPath);
        if (source == null)
            return $"{def.displayName}: ERROR — model not found at {def.modelPath}";

        var instance = (GameObject)PrefabUtility.InstantiatePrefab(source);
        PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely,
            InteractionMode.AutomatedAction);
        instance.name = def.displayName;
        instance.transform.position = Vector3.zero;
        instance.transform.rotation = Quaternion.identity;

        int colliders = AddMeshColliders(instance);

        // Auto geometry from the hull bounds (root at origin → relative to pivot).
        Bounds  bounds = MeasureBounds(instance);
        float   height = bounds.size.y;
        float   hullYOffset     = -bounds.min.y - 0.2f * height;          // ~20% submerged
        float   boardRange      = Mathf.Max(6f, bounds.extents.magnitude + 2f);
        Vector3 disembarkOffset = new Vector3(bounds.extents.x + 1.5f, bounds.max.y + 1f, 0f);

        var raft = instance.GetComponent<RaftController>() ?? instance.AddComponent<RaftController>();
        var rso  = new SerializedObject(raft);

        // Per-boat identity
        SetString(rso, "requireFlag",   def.ownedFlag);
        SetString(rso, "boardPrompt",   "Press E to ride the " + def.displayName);
        SetString(rso, "lockedMessage", "Buy this boat from Lil Zoinks first!");

        // Shared "buyable boat" profile (same values as the Chug Boat)
        SetFloat (rso, "waterLevel",           4f);
        SetFloat (rso, "raftSpeed",            50f);
        SetFloat (rso, "raftTurnSpeed",        90f);
        SetBool  (rso, "canSprint",            true);
        SetFloat (rso, "sprintMultiplier",     2f);
        SetBool  (rso, "hideRiderWhileAboard", true);
        SetFloat (rso, "raftCamDistance",      22f);
        SetFloat (rso, "raftCamHeight",        6f);
        SetInt   (rso, "groundLayer",          1 << IslandLayer);

        // Geometry starting points (tune per boat)
        SetFloat  (rso, "hullYOffset",     hullYOffset);
        SetFloat  (rso, "boardRange",      boardRange);
        SetVector3(rso, "disembarkOffset", disembarkOffset);

        rso.ApplyModifiedPropertiesWithoutUndo();

        if (instance.GetComponent<RaftHealth>() == null)
            instance.AddComponent<RaftHealth>();

        PrefabUtility.SaveAsPrefabAsset(instance, def.outputPath, out bool ok);
        Object.DestroyImmediate(instance);

        return ok
            ? $"{def.displayName}: created ({colliders} colliders; auto hullYOffset {hullYOffset:0.0}, boardRange {boardRange:0.0})"
            : $"{def.displayName}: ERROR — failed to save {def.outputPath}";
    }

    private static int AddMeshColliders(GameObject root)
    {
        int count = 0;
        foreach (var mf in root.GetComponentsInChildren<MeshFilter>(includeInactive: true))
        {
            if (mf.sharedMesh == null) continue;
            if (mf.GetComponent<Collider>() != null) continue;
            var mc = mf.gameObject.AddComponent<MeshCollider>();
            mc.sharedMesh = mf.sharedMesh;
            count++;
        }
        return count;
    }

    private static Bounds MeasureBounds(GameObject root)
    {
        var rends = root.GetComponentsInChildren<Renderer>();
        if (rends.Length == 0) return new Bounds(Vector3.zero, Vector3.one);
        Bounds b = rends[0].bounds;
        foreach (var r in rends) b.Encapsulate(r.bounds);
        return b;
    }

    // ── Wire Lil Zoinks' shop (idempotent) ────────────────────────────────────

    private static string WireLilZoinksShop()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(LilZoinksPath) == null)
            return "  ERROR — Lil Zoinks prefab not found at " + LilZoinksPath;

        int  added = 0;
        bool dialogueFixed = false;

        using (var scope = new PrefabUtility.EditPrefabContentsScope(LilZoinksPath))
        {
            var root = scope.prefabContentsRoot;

            var shop = root.GetComponent<SenseiShop>() ?? root.AddComponent<SenseiShop>();
            var shopSO = new SerializedObject(shop);
            SetString(shopSO, "shopTitle", "Lil Zoinks' Boat Shop™");

            var items = shopSO.FindProperty("items");
            foreach (var def in Boats)
            {
                if (FindShopItemByFlag(items, def.ownedFlag) >= 0) continue;  // already sold
                items.arraySize += 1;
                var item = items.GetArrayElementAtIndex(items.arraySize - 1);
                item.FindPropertyRelative("itemName").stringValue      = def.displayName;
                item.FindPropertyRelative("description").stringValue   = def.description;
                item.FindPropertyRelative("price").intValue            = def.price;
                item.FindPropertyRelative("grantsFlag").stringValue    = def.ownedFlag;
                item.FindPropertyRelative("oneTimePurchase").boolValue = true;
                item.FindPropertyRelative("purchased").boolValue       = false;
                added++;
            }
            shopSO.ApplyModifiedPropertiesWithoutUndo();

            var dlg = root.GetComponent<DialogueTrigger>();
            if (dlg != null)
            {
                var dlgSO = new SerializedObject(dlg);
                var lines = dlgSO.FindProperty("lines");
                for (int i = 0; i < lines.arraySize; i++)
                {
                    var line   = lines.GetArrayElementAtIndex(i);
                    var prompt = line.FindPropertyRelative("playerPrompt").stringValue ?? "";
                    if (prompt.ToLowerInvariant().Contains("buy a boat"))
                    {
                        line.FindPropertyRelative("opensShop").boolValue      = true;
                        line.FindPropertyRelative("closesDialogue").boolValue = false;
                        dialogueFixed = true;
                    }
                }
                dlgSO.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        return $"  • {added} new shop item(s) added\n" +
               $"  • 'Buy a boat' line {(dialogueFixed ? "opens the shop" : "NOT found — set opensShop by hand")}";
    }

    private static int FindShopItemByFlag(SerializedProperty items, string flag)
    {
        for (int i = 0; i < items.arraySize; i++)
        {
            var grants = items.GetArrayElementAtIndex(i).FindPropertyRelative("grantsFlag");
            if (grants != null && grants.stringValue == flag) return i;
        }
        return -1;
    }

    // ── serialized-field helpers ──────────────────────────────────────────────

    private static void SetString(SerializedObject so, string name, string value)
    {
        var p = so.FindProperty(name); if (p != null) p.stringValue = value;
    }
    private static void SetFloat(SerializedObject so, string name, float value)
    {
        var p = so.FindProperty(name); if (p != null) p.floatValue = value;
    }
    private static void SetInt(SerializedObject so, string name, int value)
    {
        var p = so.FindProperty(name); if (p != null) p.intValue = value;
    }
    private static void SetBool(SerializedObject so, string name, bool value)
    {
        var p = so.FindProperty(name); if (p != null) p.boolValue = value;
    }
    private static void SetVector3(SerializedObject so, string name, Vector3 value)
    {
        var p = so.FindProperty(name); if (p != null) p.vector3Value = value;
    }
}
