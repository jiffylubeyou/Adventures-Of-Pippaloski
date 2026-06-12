using UnityEditor;
using UnityEngine;

/// <summary>
/// Boats don't use physics collision against land — RaftController and
/// PirateShipAI both sweep a Physics.CheckBox against the "Island" layer (8)
/// before moving. The Boat Purchase Island prefab was created on the Default
/// layer, so boats sail straight through it even though it has MeshColliders.
///
/// This puts every GameObject in the Boat Purchase Island prefab on the
/// Island layer so the player's raft and the pirate ships treat it as land.
///
/// Run via Tools > Pippaloski > Set Boat Purchase Island Layer.
/// Safe to run multiple times.
/// </summary>
public static class SetBoatPurchaseIslandLayer
{
    private const string IslandPrefabPath =
        "Assets/GeneratedAssets/Prefabs/Boat Purchase Island.prefab";

    private const int IslandLayer = 8;

    [MenuItem("Tools/Pippaloski/Set Boat Purchase Island Layer")]
    public static void Fix()
    {
        int changed = 0, total = 0;

        using (var scope = new PrefabUtility.EditPrefabContentsScope(IslandPrefabPath))
        {
            foreach (var t in scope.prefabContentsRoot
                         .GetComponentsInChildren<Transform>(includeInactive: true))
            {
                total++;
                if (t.gameObject.layer == IslandLayer) continue;
                t.gameObject.layer = IslandLayer;
                changed++;
            }
        }

        AssetDatabase.SaveAssets();

        EditorUtility.DisplayDialog("Set Boat Purchase Island Layer",
            $"Done!\n\n" +
            $"• {changed} of {total} object(s) moved to the Island layer (8).\n\n" +
            "The raft and pirate ships will now treat the island as land.",
            "OK");
    }
}
