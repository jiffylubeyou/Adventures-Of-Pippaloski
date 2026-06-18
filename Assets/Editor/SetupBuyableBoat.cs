using UnityEditor;
using UnityEngine;

/// <summary>
/// Wires up the "buyable boat" feature in one shot:
///
///   1. Builds a configured boat prefab at
///      Assets/GeneratedAssets/Prefabs/Buyable Boat.prefab from the
///      Low-Poly 3D Boat model — adds MeshColliders to its parts (so
///      cannonballs hit it and the player can't walk through it), a
///      RaftController gated behind the "boat_speedboat_owned" flag, and a
///      RaftHealth so pirates can sink it like the raft.
///
///   2. Turns Lil Zoinks into a boat seller — adds a SenseiShop selling the
///      Speedboat for 250 coins (granting that flag) and flips his
///      "I want to buy a boat" dialogue line to open the shop.
///
/// After running, drag Buyable Boat.prefab onto the water where you want it and
/// fine-tune its seatOffset / hullHalfExtents in the inspector (the green/red
/// gizmo box shows the land-collision probe).
///
/// Run via Tools > Pippaloski > Setup Buyable Boat. Safe to run multiple times.
///
/// To add MORE boats later: duplicate Buyable Boat.prefab, swap the model, give
/// its RaftController a new requireFlag + messages, and add a matching SenseiShop
/// item whose grantsFlag is that flag. No code changes needed.
/// </summary>
public static class SetupBuyableBoat
{
    private const string BoatModelPath =
        "Assets/Low-Poly 3D Boat Model/Prefab/boat Prefab.prefab";
    private const string OutputBoatPath =
        "Assets/GeneratedAssets/Prefabs/Buyable Boat.prefab";
    private const string LilZoinksPath =
        "Assets/GeneratedAssets/Prefabs/Lil Zoinks.prefab";

    // Runtime GameState lives in Assembly-CSharp, which editor scripts reference,
    // so we use its constant directly — boat flag and shop grant can't drift apart.
    private const string OwnedFlag   = GameState.SpeedboatOwned;
    private const int    BoatPrice   = 250;
    private const int    IslandLayer = 8;

    [MenuItem("Tools/Pippaloski/Setup Buyable Boat")]
    public static void Setup()
    {
        string boatResult = BuildBoatPrefab();
        string shopResult = WireLilZoinksShop();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Setup Buyable Boat",
            "Done!\n\n" +
            "BOAT PREFAB:\n" + boatResult + "\n\n" +
            "LIL ZOINKS SHOP:\n" + shopResult + "\n\n" +
            "Next: drag 'Buyable Boat.prefab' onto the water and tune its " +
            "seatOffset / hullHalfExtents in the inspector.",
            "OK");
    }

    // ── 1. Build the configured boat prefab ───────────────────────────────────

    private static string BuildBoatPrefab()
    {
        var source = AssetDatabase.LoadAssetAtPath<GameObject>(BoatModelPath);
        if (source == null)
            return "ERROR: boat model not found at " + BoatModelPath;

        // Work on a temporary scene instance, then save it as a brand-new prefab.
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(source);
        // Unpack so the saved asset is a self-contained prefab, not a variant that
        // re-inherits (and could lose) our added components.
        PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely,
            InteractionMode.AutomatedAction);
        instance.name = "Buyable Boat";
        instance.transform.position = Vector3.zero;
        instance.transform.rotation = Quaternion.identity;

        int colliders = AddMeshColliders(instance);

        // Measure the hull so we can sit the boat IN the water and size the
        // boarding range to the boat, not a small flat raft.
        Bounds bounds = MeasureBounds(instance);   // relative to the root at origin
        float  height = bounds.size.y;
        // Force the keel ~20% of the hull height below the surface, whatever the
        // model's pivot happens to be, so it floats believably instead of on top.
        float  hullYOffset = -bounds.min.y - 0.2f * height;
        // Reach from the centre pivot out past the hull so the prompt appears when
        // the player walks up to the side/bow.
        float  boardRange  = Mathf.Max(6f, bounds.extents.magnitude + 2f);

        // RaftController — gated behind the shop-purchase flag
        var raft = instance.GetComponent<RaftController>() ?? instance.AddComponent<RaftController>();
        var rso = new SerializedObject(raft);
        SetString(rso, "requireFlag",   OwnedFlag);
        SetString(rso, "boardPrompt",   "Press E to ride the boat");
        SetString(rso, "lockedMessage", "Buy this boat from Lil Zoinks first!");
        SetFloat (rso, "waterLevel",    4f);                 // project water height
        SetFloat (rso, "hullYOffset",   hullYOffset);        // sink it into the water
        SetFloat (rso, "boardRange",    boardRange);
        SetInt   (rso, "groundLayer",   1 << IslandLayer);   // Island layer mask
        rso.ApplyModifiedPropertiesWithoutUndo();

        // RaftHealth — lets pirates damage/sink it like the raft
        if (instance.GetComponent<RaftHealth>() == null)
            instance.AddComponent<RaftHealth>();

        PrefabUtility.SaveAsPrefabAsset(instance, OutputBoatPath, out bool ok);
        Object.DestroyImmediate(instance);

        return ok
            ? $"Created {OutputBoatPath}\n  • {colliders} MeshCollider(s), RaftController (flag '{OwnedFlag}'), RaftHealth\n  • auto hullYOffset {hullYOffset:0.00}, boardRange {boardRange:0.0}"
            : "ERROR: failed to save " + OutputBoatPath;
    }

    // Combined renderer bounds with the boat at the origin (identity rotation),
    // so min/center/extents are measured relative to the prefab root pivot.
    private static Bounds MeasureBounds(GameObject root)
    {
        var rends = root.GetComponentsInChildren<Renderer>();
        if (rends.Length == 0) return new Bounds(Vector3.zero, Vector3.one);
        Bounds b = rends[0].bounds;
        foreach (var r in rends) b.Encapsulate(r.bounds);
        return b;
    }

    // Mirrors AddMeshCollidersToChildren: one MeshCollider per uncovered MeshFilter.
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

    // ── 2. Wire Lil Zoinks' shop ──────────────────────────────────────────────

    private static string WireLilZoinksShop()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(LilZoinksPath) == null)
            return "ERROR: Lil Zoinks prefab not found at " + LilZoinksPath;

        bool addedShop = false, dialogueFixed = false;

        using (var scope = new PrefabUtility.EditPrefabContentsScope(LilZoinksPath))
        {
            var root = scope.prefabContentsRoot;

            // --- SenseiShop with the Speedboat item ---
            var shop = root.GetComponent<SenseiShop>();
            if (shop == null) { shop = root.AddComponent<SenseiShop>(); addedShop = true; }

            var shopSO = new SerializedObject(shop);
            SetString(shopSO, "shopTitle", "Lil Zoinks' Boat Shop™");

            var items = shopSO.FindProperty("items");
            int existing = FindShopItemByFlag(items, OwnedFlag);
            if (existing < 0)
            {
                items.arraySize += 1;
                var item = items.GetArrayElementAtIndex(items.arraySize - 1);
                item.FindPropertyRelative("itemName").stringValue        = "Speedboat";
                item.FindPropertyRelative("description").stringValue     =
                    "A sleek motorboat. Faster than that flimsy raft.";
                item.FindPropertyRelative("price").intValue              = BoatPrice;
                item.FindPropertyRelative("grantsFlag").stringValue      = OwnedFlag;
                item.FindPropertyRelative("oneTimePurchase").boolValue   = true;
                item.FindPropertyRelative("purchased").boolValue         = false;
            }
            shopSO.ApplyModifiedPropertiesWithoutUndo();

            // --- DialogueTrigger: make "I want to buy a boat" open the shop ---
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

        return $"  • SenseiShop {(addedShop ? "added" : "already present")} (Speedboat, {BoatPrice} coins)\n" +
               $"  • 'Buy a boat' line {(dialogueFixed ? "now opens the shop" : "NOT found — set opensShop by hand")}";
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
        var p = so.FindProperty(name);
        if (p != null) p.stringValue = value;
    }
    private static void SetFloat(SerializedObject so, string name, float value)
    {
        var p = so.FindProperty(name);
        if (p != null) p.floatValue = value;
    }
    private static void SetInt(SerializedObject so, string name, int value)
    {
        var p = so.FindProperty(name);
        if (p != null) p.intValue = value;
    }
}
