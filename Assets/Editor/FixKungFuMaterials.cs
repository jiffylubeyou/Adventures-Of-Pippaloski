using UnityEditor;
using UnityEngine;

public static class FixKungFuMaterials
{
    [MenuItem("Tools/Pippaloski/Fix Kung Fu Pack Materials")]
    public static void Fix()
    {
        var urpShader = Shader.Find("Universal Render Pipeline/Lit");
        if (urpShader == null)
        {
            EditorUtility.DisplayDialog("Error", "URP Lit shader not found. Make sure URP is installed.", "OK");
            return;
        }

        var guids = AssetDatabase.FindAssets("t:Material", new[] { "Assets/Kung Fu Pack" });
        int fixedCount = 0;

        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var mat  = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) continue;
            if (mat.shader.name.Contains("Universal Render Pipeline")) continue;

            // Grab values before switching
            Color   baseColor  = mat.HasProperty("_Color")      ? mat.GetColor("_Color")          : Color.white;
            Texture albedo     = mat.HasProperty("_MainTex")     ? mat.GetTexture("_MainTex")      : null;
            Texture normalMap  = mat.HasProperty("_BumpMap")     ? mat.GetTexture("_BumpMap")      : null;
            float   smoothness = mat.HasProperty("_Glossiness")  ? mat.GetFloat("_Glossiness")     : 0.3f;
            float   metallic   = mat.HasProperty("_Metallic")    ? mat.GetFloat("_Metallic")       : 0f;

            // If no texture is assigned yet, try to find the matching albedo by name convention
            if (albedo == null)
                albedo = FindAlbedoForMaterial(path);

            mat.shader = urpShader;

            if (mat.HasProperty("_BaseColor"))                    mat.SetColor("_BaseColor", baseColor);
            if (mat.HasProperty("_BaseMap") && albedo != null)    mat.SetTexture("_BaseMap", albedo);
            if (mat.HasProperty("_BumpMap") && normalMap != null) mat.SetTexture("_BumpMap", normalMap);
            if (mat.HasProperty("_Smoothness"))                   mat.SetFloat("_Smoothness", smoothness);
            if (mat.HasProperty("_Metallic"))                     mat.SetFloat("_Metallic", metallic);

            EditorUtility.SetDirty(mat);
            fixedCount++;
        }

        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("Done", $"Fixed {fixedCount} Kung Fu Pack material(s).", "Nice");
    }

    // Searches the Textures folder for an albedo whose filename starts with the material name.
    private static Texture FindAlbedoForMaterial(string matPath)
    {
        var matName = System.IO.Path.GetFileNameWithoutExtension(matPath);
        var texGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Kung Fu Pack/Assets/Textures" });

        foreach (var guid in texGuids)
        {
            var texPath = AssetDatabase.GUIDToAssetPath(guid);
            var texFile = System.IO.Path.GetFileName(texPath).ToLower();

            // Match files that start with the material name and contain "albedo"
            if (texFile.StartsWith(matName.ToLower()) && texFile.Contains("albedo"))
                return AssetDatabase.LoadAssetAtPath<Texture>(texPath);
        }

        // Broader fallback: any albedo texture inside a subfolder named after the material
        foreach (var guid in texGuids)
        {
            var texPath = AssetDatabase.GUIDToAssetPath(guid);
            if (texPath.ToLower().Contains("/" + matName.ToLower() + "/") && texPath.ToLower().Contains("albedo"))
                return AssetDatabase.LoadAssetAtPath<Texture>(texPath);
        }

        return null;
    }
}
