using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public static class FixMissingColliders
{
    [MenuItem("Tools/Pippaloski/Fix Missing Terrain Colliders")]
    public static void Fix()
    {
        var fixed_ = new List<string>();

        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            foreach (var mf in root.GetComponentsInChildren<MeshFilter>(true))
            {
                var go   = mf.gameObject;
                var mesh = mf.sharedMesh;
                if (mesh == null) continue;

                // Skip anything that already has any collider
                if (go.GetComponentInChildren<Collider>() != null) continue;

                // Skip small objects and UI / particle things
                var r = go.GetComponent<Renderer>();
                if (r == null) continue;
                if (r.bounds.size.magnitude < 3f) continue;

                // Skip objects that are clearly decorative by name
                var nameLower = go.name.ToLower();
                if (nameLower.Contains("cloud") ||
                    nameLower.Contains("sky")   ||
                    nameLower.Contains("water") ||
                    nameLower.Contains("backdrop")) continue;

                var mc = go.AddComponent<MeshCollider>();
                mc.sharedMesh = mesh;
                EditorUtility.SetDirty(go);
                fixed_.Add(go.name);
            }
        }

        if (fixed_.Count == 0)
        {
            EditorUtility.DisplayDialog("Fix Missing Colliders",
                "No large meshes without colliders found.", "OK");
            return;
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            SceneManager.GetActiveScene());

        EditorUtility.DisplayDialog("Fix Missing Colliders",
            $"Added MeshCollider to {fixed_.Count} object(s):\n" +
            string.Join("\n", fixed_), "Great");
    }
}
