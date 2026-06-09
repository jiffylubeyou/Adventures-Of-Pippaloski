using UnityEditor;
using UnityEngine;
using System.IO;

/// <summary>
/// Converts Easy Primitive People and Bizulka materials from Built-in Standard to URP Lit.
/// Run via Tools > Pippaloski > Fix New Asset Materials.
/// </summary>
public static class FixNewAssetMaterials
{
    [MenuItem("Tools/Pippaloski/Fix New Asset Materials")]
    public static void Fix()
    {
        var urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null)
        {
            EditorUtility.DisplayDialog("Fix New Asset Materials",
                "Could not find the URP Lit shader. Make sure URP is installed.", "OK");
            return;
        }

        int count = 0;
        count += FixFolder("Assets/Easy Primitive People", urpLit);
        count += FixFolder("Assets/Bizulka",               urpLit);
        count += FixFolder("Assets/Metal_Grids_Textures",        urpLit);
        count += FixFolder("Assets/Building FastFood Drinks",    urpLit);
        count += FixFolder("Assets/CGM_EgyptPack",             urpLit);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Fix New Asset Materials",
            $"Done! Converted {count} material(s) to URP Lit.", "Great");
    }

    private static int FixFolder(string folderPath, Shader urpLit)
    {
        int count = 0;
        string[] guids = AssetDatabase.FindAssets("t:Material", new[] { folderPath });

        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) continue;

            string shaderName = mat.shader != null ? mat.shader.name : "";

            // Only touch Built-in shaders
            if (!shaderName.StartsWith("Standard") &&
                !shaderName.StartsWith("Legacy") &&
                !shaderName.StartsWith("Mobile") &&
                shaderName != "Diffuse" &&
                shaderName != "Specular")
                continue;

            // Cache Built-in values before swapping shader
            Color   baseColor       = mat.HasProperty("_Color")            ? mat.GetColor("_Color")               : Color.white;
            Texture albedo          = mat.HasProperty("_MainTex")          ? mat.GetTexture("_MainTex")            : null;
            Texture normalMap       = mat.HasProperty("_BumpMap")          ? mat.GetTexture("_BumpMap")            : null;
            float   normalScale     = mat.HasProperty("_BumpScale")        ? mat.GetFloat("_BumpScale")            : 1f;
            Texture occlusion       = mat.HasProperty("_OcclusionMap")     ? mat.GetTexture("_OcclusionMap")       : null;
            Texture metallicGloss   = mat.HasProperty("_MetallicGlossMap") ? mat.GetTexture("_MetallicGlossMap")  : null;
            float   metallic        = mat.HasProperty("_Metallic")         ? mat.GetFloat("_Metallic")             : 0f;
            // GlossMapScale is the smoothness multiplier when a metallic-gloss map is used
            float   smoothness      = mat.HasProperty("_GlossMapScale")    ? mat.GetFloat("_GlossMapScale")
                                    : mat.HasProperty("_Glossiness")       ? mat.GetFloat("_Glossiness")           : 0.5f;
            Color   emission        = mat.HasProperty("_EmissionColor")    ? mat.GetColor("_EmissionColor")        : Color.black;
            // Alpha cutout: Built-in _Mode == 1 means AlphaTest/Cutout
            bool    isAlphaCutout   = mat.HasProperty("_Mode") && Mathf.RoundToInt(mat.GetFloat("_Mode")) == 1;
            float   alphaCutoff     = mat.HasProperty("_Cutoff")           ? mat.GetFloat("_Cutoff")               : 0.5f;

            // Swap to URP Lit
            mat.shader = urpLit;

            // Map Built-in → URP Lit properties
            mat.SetColor("_BaseColor", baseColor);
            if (albedo != null) mat.SetTexture("_BaseMap", albedo);
            if (normalMap != null)
            {
                mat.SetTexture("_BumpMap",  normalMap);
                mat.SetFloat("_BumpScale",  normalScale);
                mat.EnableKeyword("_NORMALMAP");
            }
            if (occlusion != null) mat.SetTexture("_OcclusionMap", occlusion);
            if (metallicGloss != null)
            {
                mat.SetTexture("_MetallicGlossMap", metallicGloss);
                mat.EnableKeyword("_METALLICSPECGLOSSMAP");
            }
            mat.SetFloat("_Metallic",   metallic);
            mat.SetFloat("_Smoothness", smoothness);

            // Alpha cutout mode
            if (isAlphaCutout)
            {
                mat.SetFloat("_AlphaClip", 1f);
                mat.SetFloat("_Cutoff",    alphaCutoff);
                mat.SetFloat("_Surface",   0f);          // 0 = Opaque surface + alpha clip
                mat.EnableKeyword("_ALPHATEST_ON");
                mat.renderQueue = 2450;
            }

            if (emission != Color.black)
            {
                mat.SetColor("_EmissionColor", emission);
                mat.EnableKeyword("_EMISSION");
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
            }

            EditorUtility.SetDirty(mat);
            count++;
        }

        return count;
    }
}
