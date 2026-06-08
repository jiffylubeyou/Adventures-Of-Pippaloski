using UnityEditor;
using UnityEngine;

public static class FixSunTempleMaterials
{
    [MenuItem("Tools/Pippaloski/Fix Sun Temple Materials")]
    public static void Fix()
    {
        var urpShader = Shader.Find("Universal Render Pipeline/Lit");
        if (urpShader == null)
        {
            EditorUtility.DisplayDialog("Error", "URP Lit shader not found. Make sure URP is installed.", "OK");
            return;
        }

        var guids = AssetDatabase.FindAssets("t:Material", new[] { "Assets/Sun_Temple" });
        int fixed_count = 0;

        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var mat  = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) continue;

            // Skip if already on a URP shader
            if (mat.shader.name.Contains("Universal Render Pipeline")) continue;

            // Grab values from the old shader before switching
            Color baseColor = Color.white;
            Texture mainTex = null;
            float smoothness = 0.5f;
            float metallic   = 0f;
            Texture normalMap = null;

            if (mat.HasProperty("_Color"))       baseColor  = mat.GetColor("_Color");
            if (mat.HasProperty("_MainTex"))      mainTex    = mat.GetTexture("_MainTex");
            if (mat.HasProperty("_Glossiness"))   smoothness = mat.GetFloat("_Glossiness");
            if (mat.HasProperty("_Metallic"))     metallic   = mat.GetFloat("_Metallic");
            if (mat.HasProperty("_BumpMap"))      normalMap  = mat.GetTexture("_BumpMap");

            mat.shader = urpShader;

            // Apply values to URP properties
            if (mat.HasProperty("_BaseColor"))   mat.SetColor("_BaseColor", baseColor);
            if (mat.HasProperty("_BaseMap") && mainTex != null)
                mat.SetTexture("_BaseMap", mainTex);
            if (mat.HasProperty("_Smoothness"))  mat.SetFloat("_Smoothness", smoothness);
            if (mat.HasProperty("_Metallic"))    mat.SetFloat("_Metallic", metallic);
            if (mat.HasProperty("_BumpMap") && normalMap != null)
                mat.SetTexture("_BumpMap", normalMap);

            EditorUtility.SetDirty(mat);
            fixed_count++;
        }

        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("Done",
            $"Converted {fixed_count} Sun Temple materials to URP.", "Great");
    }
}
