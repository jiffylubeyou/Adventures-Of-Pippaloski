using UnityEditor;
using UnityEngine;

public static class SetupWitchDialogue
{
    private const string PrefabPath = "Assets/GeneratedAssets/Prefabs/Witch.prefab";

    [MenuItem("Tools/Pippaloski/Setup Witch Components")]
    public static void Setup()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null)
        {
            EditorUtility.DisplayDialog("Error", "Could not find prefab at:\n" + PrefabPath, "OK");
            return;
        }

        using (var scope = new PrefabUtility.EditPrefabContentsScope(PrefabPath))
        {
            var root = scope.prefabContentsRoot;

            // Add DialogueTrigger only if missing — never touch existing one
            if (root.GetComponent<DialogueTrigger>() == null)
                root.AddComponent<DialogueTrigger>();

            // Add WitchLunchQuest only if missing
            if (root.GetComponent<WitchLunchQuest>() == null)
                root.AddComponent<WitchLunchQuest>();
        }

        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("Done",
            "Witch components set up.\n\n" +
            "• DialogueTrigger: added if it was missing (existing one left untouched).\n" +
            "• WitchLunchQuest: added.\n\n" +
            "On the dialogue line that ends with 'Be quick my child...'\n" +
            "tick 'Starts Witch Lunch Quest' in the Inspector.",
            "OK");
    }
}
