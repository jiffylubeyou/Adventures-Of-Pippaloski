using UnityEditor;
using UnityEngine;

/// <summary>
/// Select any GameObject (or a prefab asset) in the Project/Hierarchy window,
/// then run Tools > Pippaloski > Add Mesh Colliders To Children.
///
/// For every child (and the root itself) that has a MeshFilter but no
/// MeshCollider, a MeshCollider is added using that child's mesh.
/// Children whose mesh is already covered by a collider are skipped.
///
/// Works on scene instances (Undo-able) and on prefab assets via
/// EditPrefabContentsScope.
/// </summary>
public static class AddMeshCollidersToChildren
{
    [MenuItem("Tools/Pippaloski/Add Mesh Colliders To Children", true)]
    private static bool Validate() => Selection.activeObject != null;

    [MenuItem("Tools/Pippaloski/Add Mesh Colliders To Children")]
    public static void Run()
    {
        var selected = Selection.activeObject;

        // ── Prefab asset selected in Project window ───────────────────────────
        string assetPath = AssetDatabase.GetAssetPath(selected);
        if (!string.IsNullOrEmpty(assetPath) && assetPath.EndsWith(".prefab"))
        {
            int count = 0;
            using (var scope = new PrefabUtility.EditPrefabContentsScope(assetPath))
            {
                count = AddCollidersRecursive(scope.prefabContentsRoot, prefabMode: true);
            }
            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("Add Mesh Colliders",
                $"Added {count} MeshCollider(s) to prefab:\n{assetPath}", "OK");
            return;
        }

        // ── Scene instance selected in Hierarchy ──────────────────────────────
        var go = Selection.activeGameObject;
        if (go == null)
        {
            EditorUtility.DisplayDialog("Add Mesh Colliders",
                "Select a GameObject in the Hierarchy or a prefab in the Project window first.", "OK");
            return;
        }

        Undo.SetCurrentGroupName("Add Mesh Colliders To Children");
        int sceneCount = AddCollidersRecursive(go, prefabMode: false);
        EditorUtility.DisplayDialog("Add Mesh Colliders",
            $"Added {sceneCount} MeshCollider(s) to '{go.name}' and its children.", "OK");
    }

    private static int AddCollidersRecursive(GameObject root, bool prefabMode)
    {
        int count = 0;
        foreach (var mf in root.GetComponentsInChildren<MeshFilter>(includeInactive: true))
        {
            if (mf.sharedMesh == null) continue;
            if (mf.GetComponent<MeshCollider>() != null) continue;   // already has one

            // Skip objects that only have the collider role delegated to a parent
            // (e.g. an existing BoxCollider / CapsuleCollider on the same GO)
            if (mf.GetComponent<Collider>() != null) continue;

            if (prefabMode)
            {
                var mc = mf.gameObject.AddComponent<MeshCollider>();
                mc.sharedMesh = mf.sharedMesh;
            }
            else
            {
                var mc = Undo.AddComponent<MeshCollider>(mf.gameObject);
                mc.sharedMesh = mf.sharedMesh;
            }

            count++;
        }
        return count;
    }
}
