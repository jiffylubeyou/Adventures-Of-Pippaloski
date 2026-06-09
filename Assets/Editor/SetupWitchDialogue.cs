using UnityEditor;
using UnityEngine;

public static class SetupWitchDialogue
{
    private const string PrefabPath = "Assets/GeneratedAssets/Prefabs/Witch.prefab";

    [MenuItem("Tools/Pippaloski/Setup Witch Dialogue")]
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

            var trigger = root.GetComponent<DialogueTrigger>();
            if (trigger == null)
                trigger = root.AddComponent<DialogueTrigger>();

            var so = new SerializedObject(trigger);

            so.FindProperty("npcName").stringValue =
                "Witch";

            so.FindProperty("greeting").stringValue =
                "Heheheh... another visitor? How... delightful.";

            // Seed two starter lines — user edits them in the Inspector
            var linesProp = so.FindProperty("lines");
            linesProp.arraySize = 2;

            var line0 = linesProp.GetArrayElementAtIndex(0);
            line0.FindPropertyRelative("playerPrompt").stringValue =
                "Who are you?";
            line0.FindPropertyRelative("npcResponse").stringValue =
                "I am the Witch of the Misty Wood. I have lived here far longer than you can imagine, little pup.";
            line0.FindPropertyRelative("closesDialogue").boolValue = false;

            var line1 = linesProp.GetArrayElementAtIndex(1);
            line1.FindPropertyRelative("playerPrompt").stringValue =
                "Goodbye.";
            line1.FindPropertyRelative("npcResponse").stringValue =
                "Yes, yes. Run along now.";
            line1.FindPropertyRelative("closesDialogue").boolValue = true;

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("Done",
            "Witch dialogue configured.\n\n" +
            "• Greeting: \"Heheheh... another visitor? How... delightful.\"\n" +
            "• Two starter lines added — customise them on the DialogueTrigger in the Inspector.\n" +
            "• All the same features work: oneTimeOnly, setsFlags, requiredFlags, etc.",
            "Nice");
    }
}
