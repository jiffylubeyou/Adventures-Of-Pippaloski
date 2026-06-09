using UnityEditor;
using UnityEngine;

public static class SetupSenseiDialogue
{
    private const string PrefabPath = "Assets/GeneratedAssets/Prefabs/Sensei 桜夢.prefab";

    [MenuItem("Tools/Pippaloski/Setup Sensei Dialogue")]
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

            // ---- DialogueTrigger ----
            var trigger = root.GetComponent<DialogueTrigger>();
            if (trigger == null)
                trigger = root.AddComponent<DialogueTrigger>();

            // Use SerializedObject so private [SerializeField] fields are reachable
            var so = new SerializedObject(trigger);

            so.FindProperty("npcName").stringValue =
                "Sensei 桜夢";

            so.FindProperty("greeting").stringValue =
                "Hee hee hee, I'm a ninja.";

            // Seed one placeholder line — user replaces this with their own chains
            var linesProp = so.FindProperty("lines");
            linesProp.arraySize = 1;
            var line0 = linesProp.GetArrayElementAtIndex(0);
            line0.FindPropertyRelative("playerPrompt").stringValue  = "Goodbye.";
            line0.FindPropertyRelative("npcResponse").stringValue   = "Hee hee hee.";
            line0.FindPropertyRelative("closesDialogue").boolValue  = true;

            so.ApplyModifiedPropertiesWithoutUndo();

            // ---- SenseiShop stub ----
            if (root.GetComponent<SenseiShop>() == null)
                root.AddComponent<SenseiShop>();
        }

        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("Done",
            "Sensei dialogue configured.\n\n" +
            "• Greeting: \"Hee hee hee, I'm a ninja.\"\n" +
            "• Add your own dialogue lines on the DialogueTrigger in the Inspector.\n" +
            "• SenseiShop component added — shop items go there when you're ready.",
            "Nice");
    }
}
