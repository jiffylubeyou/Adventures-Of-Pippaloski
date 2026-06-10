using UnityEditor;
using UnityEngine;

/// <summary>
/// Fixes two issues with the Stylized Pirate Ship asset:
///   1. Normal maps are stored in OpenGL format (G channel inverted vs DirectX).
///      Sets them to Normal Map type and flips the green channel on import.
///   2. Smoothness multiplier was set to 1.0 — lowered to 0.5 so the ship
///      doesn't look like a mirror.
///
/// Run via Tools > Pippaloski > Fix Pirate Ship Materials.
/// Safe to run multiple times.
/// </summary>
public static class FixPirateShipMaterials
{
    private const string FolderPath = "Assets/Stylized_Pirate_Ship";

    [MenuItem("Tools/Pippaloski/Fix Pirate Ship Materials")]
    public static void Fix()
    {
        int normalFixed = FixNormalMaps();
        int matsFixed   = FixSmoothness();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Fix Pirate Ship Materials",
            $"Done!\n\n" +
            $"• {normalFixed} OpenGL normal map(s) re-imported with G-channel flip.\n" +
            $"• {matsFixed} material(s) had smoothness lowered to 0.5.",
            "OK");
    }

    // ── Normal maps ───────────────────────────────────────────────────────────

    private static int FixNormalMaps()
    {
        int count = 0;
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { FolderPath });

        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);

            // Only touch textures with "Normal" in the filename
            if (!path.Contains("Normal") && !path.Contains("normal")) continue;

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) continue;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);

            bool needsReimport = false;

            // Set to Normal Map type if not already
            if (importer.textureType != TextureImporterType.NormalMap)
            {
                settings.textureType = TextureImporterType.NormalMap;
                needsReimport = true;
            }

            // Flip green channel for OpenGL normal maps
            if (!settings.flipGreenChannel)
            {
                settings.flipGreenChannel = true;
                needsReimport = true;
            }

            if (needsReimport)
            {
                importer.SetTextureSettings(settings);
                importer.SaveAndReimport();
                count++;
                Debug.Log($"[FixPirateShip] Re-imported normal map: {path}");
            }
        }

        return count;
    }

    // ── Smoothness ────────────────────────────────────────────────────────────

    private static int FixSmoothness()
    {
        int count = 0;
        string[] guids = AssetDatabase.FindAssets("t:Material", new[] { FolderPath });

        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) continue;

            // Only touch URP Lit materials (should already be converted)
            if (mat.shader == null ||
                !mat.shader.name.StartsWith("Universal Render Pipeline")) continue;

            // Lower smoothness multiplier so the MetallicSmoothness texture
            // isn't at full intensity (prevents mirror-like appearance)
            if (mat.HasProperty("_Smoothness") && mat.GetFloat("_Smoothness") > 0.6f)
            {
                mat.SetFloat("_Smoothness", 0.5f);
                EditorUtility.SetDirty(mat);
                count++;
            }
        }

        return count;
    }
}
