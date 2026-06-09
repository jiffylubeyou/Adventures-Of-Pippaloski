using UnityEditor;
using UnityEngine;

public static class RemoveCollidersFromSelected
{
    [MenuItem("Tools/Pippaloski/Remove Colliders From Selected")]
    public static void Remove()
    {
        var targets = Selection.gameObjects;
        if (targets.Length == 0)
        {
            EditorUtility.DisplayDialog("Remove Colliders", "Nothing selected.", "OK");
            return;
        }

        int removed = 0;
        foreach (var go in targets)
        {
            // Also hits colliders on child objects
            foreach (var col in go.GetComponentsInChildren<Collider>(true))
            {
                Undo.DestroyObjectImmediate(col);
                removed++;
            }
        }

        EditorUtility.DisplayDialog("Remove Colliders",
            $"Removed {removed} collider(s) from {targets.Length} selected object(s).", "Done");
    }
}
