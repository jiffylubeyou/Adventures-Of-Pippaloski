using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Creates an "Island" physics layer and tags the main island prefab instance with it.
/// Run once via Tools > Pippaloski > Setup Island Layer.
/// </summary>
public static class SetupIslandLayer
{
    private const string LayerName = "Island";

    [MenuItem("Tools/Pippaloski/Setup Island Layer")]
    public static void Run()
    {
        int layer = EnsureLayer(LayerName);
        if (layer < 0)
        {
            EditorUtility.DisplayDialog("Setup Island Layer",
                "Could not create the 'Island' layer — all 32 layer slots may be used.\n" +
                "Please manually add 'Island' in Edit > Project Settings > Tags and Layers.",
                "OK");
            return;
        }

        // Tag every GameObject named with "island" (case-insensitive) in the scene
        int count = 0;
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (root.name.ToLower().Contains("island"))
            {
                SetLayerRecursively(root, layer);
                EditorUtility.SetDirty(root);
                count++;
                Debug.Log($"[SetupIslandLayer] Set layer '{LayerName}' on: {root.name}");
            }
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            SceneManager.GetActiveScene());

        EditorUtility.DisplayDialog("Setup Island Layer",
            count > 0
                ? $"Layer '{LayerName}' (index {layer}) created/found.\n" +
                  $"Tagged {count} island object(s) in the scene.\n\n" +
                  "Now select your RaftController in the Inspector and set\n" +
                  "the 'Ground Layer' mask to 'Island'."
                : $"Layer '{LayerName}' (index {layer}) created/found.\n" +
                  "No GameObjects with 'island' in their name were found in the scene.\n\n" +
                  "Please manually set your island's Layer to 'Island' in the Inspector,\n" +
                  "then set the RaftController's 'Ground Layer' to 'Island'.",
            "OK");
    }

    private static void SetLayerRecursively(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform child in go.transform)
            SetLayerRecursively(child.gameObject, layer);
    }

    // Returns the layer index, creating it if it doesn't exist. Returns -1 on failure.
    private static int EnsureLayer(string name)
    {
        int existing = LayerMask.NameToLayer(name);
        if (existing >= 0) return existing;

        var tagManager = new SerializedObject(
            AssetDatabase.LoadAssetAtPath<Object>("ProjectSettings/TagManager.asset"));
        var layers = tagManager.FindProperty("layers");

        // Slots 0-7 are built-in, user layers start at 8
        for (int i = 8; i < layers.arraySize; i++)
        {
            var element = layers.GetArrayElementAtIndex(i);
            if (string.IsNullOrEmpty(element.stringValue))
            {
                element.stringValue = name;
                tagManager.ApplyModifiedProperties();
                return i;
            }
        }
        return -1;
    }
}
