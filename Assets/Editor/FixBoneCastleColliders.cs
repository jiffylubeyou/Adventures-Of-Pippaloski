using UnityEditor;
using UnityEngine;

/// <summary>
/// The Sun Temple building prefab (nested inside Bone Castle) only ships with
/// colliders on the ground floor — the upper structure and roof have none, so
/// the player falls straight through them.
///
/// This adds MeshColliders to every STRUCTURAL piece that lacks a collider,
/// while skipping decoration (books, flowers, candles, particles) and
/// anything too small to matter. Colliders are added to the source
/// Bld_Temple prefab, so the Bone Castle prefab inherits them automatically.
///
/// Run via Tools > Pippaloski > Fix Bone Castle Colliders.
/// Safe to run multiple times.
/// </summary>
public static class FixBoneCastleColliders
{
    private const string TemplePrefabPath =
        "Assets/Sun_Temple/Prefabs/Buildings_Prefabbed/Bld_Temple.prefab";

    // Decorative stuff the player never needs to collide with
    private static readonly string[] SkipPrefixes = { "Prop_", "Fol_", "FX_", "Particle" };

    // Ignore pieces smaller than this (world units, longest dimension)
    private const float MinWorldSize = 0.3f;

    [MenuItem("Tools/Pippaloski/Fix Bone Castle Colliders")]
    public static void Fix()
    {
        int added = 0, skippedDeco = 0, skippedSmall = 0;

        using (var scope = new PrefabUtility.EditPrefabContentsScope(TemplePrefabPath))
        {
            foreach (var mf in scope.prefabContentsRoot
                         .GetComponentsInChildren<MeshFilter>(includeInactive: true))
            {
                if (mf.sharedMesh == null) continue;
                if (mf.GetComponent<Collider>() != null) continue;

                if (IsDecoration(mf.gameObject.name))
                {
                    skippedDeco++;
                    continue;
                }

                Vector3 worldSize = Vector3.Scale(mf.sharedMesh.bounds.size, mf.transform.lossyScale);
                if (Mathf.Max(worldSize.x, worldSize.y, worldSize.z) < MinWorldSize)
                {
                    skippedSmall++;
                    continue;
                }

                var mc = mf.gameObject.AddComponent<MeshCollider>();
                mc.sharedMesh = mf.sharedMesh;
                added++;
            }
        }

        AssetDatabase.SaveAssets();

        EditorUtility.DisplayDialog("Fix Bone Castle Colliders",
            $"Done!\n\n" +
            $"• {added} MeshCollider(s) added to structural pieces.\n" +
            $"• {skippedDeco} decoration piece(s) skipped (props/foliage/FX).\n" +
            $"• {skippedSmall} tiny piece(s) skipped.\n\n" +
            "The Bone Castle prefab inherits these automatically.",
            "OK");
    }

    private static bool IsDecoration(string name)
    {
        foreach (var prefix in SkipPrefixes)
            if (name.StartsWith(prefix)) return true;
        return false;
    }
}
