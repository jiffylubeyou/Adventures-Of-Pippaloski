using UnityEditor;
using UnityEngine;

public static class ApplyWindToGrass
{
    private const string GrassMaterialPath = "Assets/GeneratedAssets/Materials/Meadow Grass.mat";
    private const string WindShaderName    = "Pippaloski/GrassWind";

    [MenuItem("Tools/Pippaloski/Fix Wind Pack Grass Materials")]
    public static void FixWindPackMaterials()
    {
        var windShader = Shader.Find(WindShaderName);
        if (windShader == null) { EditorUtility.DisplayDialog("Error", "GrassWind shader not found.", "OK"); return; }

        var noiseTex = FindNoiseTex();
        var guids    = AssetDatabase.FindAssets("t:Material", new[] { "Assets/Nicrom" });
        int count    = 0;

        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var mat  = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) continue;
            if (mat.shader.name.Contains("GrassWind")) continue; // already fixed

            // Only fix vegetation materials, not ground/atlas
            if (!path.Contains("Grass") && !path.Contains("Flower")) continue;

            Color col = mat.HasProperty("_Color") ? mat.GetColor("_Color") : Color.white;
            mat.shader = windShader;
            if (mat.HasProperty("_Color"))           mat.SetColor("_Color", col);
            if (noiseTex != null && mat.HasProperty("_NoiseTexture")) mat.SetTexture("_NoiseTexture", noiseTex);
            SetWindDefaults(mat);
            EditorUtility.SetDirty(mat);
            count++;
        }

        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("Done",
            $"Fixed {count} wind pack material(s).\n\nDrag prefabs from:\nAssets/Nicrom/Shaders/Wind/Prefabs/\ninto your scene to place grass.", "Great");
    }

    [MenuItem("Tools/Pippaloski/Apply Wind To Grass")]
    public static void Apply()
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(GrassMaterialPath);
        if (mat == null)
        {
            EditorUtility.DisplayDialog("Error", "Could not find Meadow Grass material at:\n" + GrassMaterialPath, "OK");
            return;
        }

        var windShader = Shader.Find(WindShaderName);
        if (windShader == null)
        {
            }

        if (windShader == null)
        {
            EditorUtility.DisplayDialog("Error",
                "Could not find the LPW_Vegetation shader. Make sure the Low Poly Wind pack is imported correctly.",
                "OK");
            return;
        }

        // Grab the current green colour before switching shaders
        Color grassColor = mat.HasProperty("_BaseColor") ? mat.GetColor("_BaseColor")
                         : mat.HasProperty("_Color")     ? mat.GetColor("_Color")
                         : new Color(0.18f, 0.56f, 0.24f);

        mat.shader = windShader;

        // Colour
        if (mat.HasProperty("_Color"))     mat.SetColor("_Color", grassColor);

        var noiseTex = FindNoiseTex();
        if (noiseTex != null && mat.HasProperty("_NoiseTexture"))
            mat.SetTexture("_NoiseTexture", noiseTex);

        SetWindDefaults(mat);

        if (mat.HasProperty("_Color")) mat.SetColor("_Color", grassColor);

        EditorUtility.SetDirty(mat);
        AssetDatabase.SaveAssets();

        EditorUtility.DisplayDialog("Done",
            "Meadow Grass material updated.\n\nFor actual grass meshes, run:\nTools > Pippaloski > Fix Wind Pack Grass Materials\nthen drag prefabs from Assets/Nicrom/Shaders/Wind/Prefabs/ into your scene.",
            "Nice");
    }

    private static Texture2D FindNoiseTex()
    {
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(
            "Assets/Nicrom/Shaders/Wind/Assets/Textures/LPW_Grass_Flat_01.png");
        if (tex != null) return tex;
        var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Nicrom" });
        return guids.Length > 0
            ? AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(guids[0]))
            : null;
    }

    private static void SetWindDefaults(Material mat)
    {
        if (mat.HasProperty("_MBAmplitude"))       mat.SetFloat("_MBAmplitude",       1.5f);
        if (mat.HasProperty("_MBAmplitudeOffset")) mat.SetFloat("_MBAmplitudeOffset", 2f);
        if (mat.HasProperty("_MBFrequency"))       mat.SetFloat("_MBFrequency",       1.11f);
        if (mat.HasProperty("_MBWindDir"))         mat.SetFloat("_MBWindDir",         45f);
        if (mat.HasProperty("_MBMaxHeight"))       mat.SetFloat("_MBMaxHeight",       10f);
        if (mat.HasProperty("_Smoothness"))        mat.SetFloat("_Smoothness",        0f);
    }
}
