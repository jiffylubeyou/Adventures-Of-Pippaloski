using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class GenerateInitialScenery
{
    private const string ScenePath = "Assets/Scenes/InitialScenery.unity";
    private const string MaterialFolder = "Assets/GeneratedAssets/Materials";

    [InitializeOnLoadMethod]
    private static void CreateSceneOnFirstImport()
    {
        EditorApplication.delayCall += () =>
        {
            if (!File.Exists(ScenePath))
            {
                CreateScene(false);
            }
        };
    }

    [MenuItem("Tools/Pippaloski/Generate Initial Scenery")]
    public static void CreateScene()
    {
        CreateScene(true);
    }

    private static void CreateScene(bool showDialog)
    {
        EnsureFolder("Assets/GeneratedAssets");
        EnsureFolder(MaterialFolder);

        var skyMat = CreateMaterial("Sunset Sky", new Color(0.35f, 0.62f, 0.95f), 0.1f);
        var sunMat = CreateMaterial("Warm Sun", new Color(1f, 0.72f, 0.22f), 0.5f);
        var grassMat = CreateMaterial("Meadow Grass", new Color(0.18f, 0.56f, 0.24f), 0.2f);
        var hillFarMat = CreateMaterial("Distant Hills", new Color(0.28f, 0.47f, 0.34f), 0.25f);
        var hillNearMat = CreateMaterial("Near Hills", new Color(0.13f, 0.42f, 0.2f), 0.2f);
        var trunkMat = CreateMaterial("Tree Trunks", new Color(0.36f, 0.2f, 0.11f), 0.35f);
        var leafMat = CreateMaterial("Tree Leaves", new Color(0.1f, 0.38f, 0.14f), 0.15f);
        var cloudMat = CreateMaterial("Soft Clouds", new Color(1f, 0.94f, 0.82f), 0.05f);
        var flowerMat = CreateMaterial("Tiny Flowers", new Color(1f, 0.86f, 0.24f), 0.25f);

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "InitialScenery";

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.62f, 0.74f, 0.9f);
        RenderSettings.ambientEquatorColor = new Color(0.44f, 0.5f, 0.42f);
        RenderSettings.ambientGroundColor = new Color(0.2f, 0.17f, 0.12f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.7f, 0.82f, 0.92f);
        RenderSettings.fogStartDistance = 35f;
        RenderSettings.fogEndDistance = 120f;

        CreateCamera();
        var sunLight = CreateSunLight();
        RenderSettings.sun = sunLight;

        CreatePlane("Painted Sky Backdrop", skyMat, new Vector3(0f, 18f, 50f), new Vector3(90f, 0f, 0f), new Vector3(12f, 1f, 7f));
        CreateSphere("Sun Disk", sunMat, new Vector3(13f, 18f, 42f), new Vector3(4.5f, 4.5f, 0.25f));

        CreatePlane("Meadow", grassMat, new Vector3(0f, -0.05f, 10f), Vector3.zero, new Vector3(18f, 1f, 14f));

        CreateHill("Far Left Hill", hillFarMat, new Vector3(-18f, 0.5f, 34f), new Vector3(16f, 4f, 4f));
        CreateHill("Far Right Hill", hillFarMat, new Vector3(17f, 0.8f, 32f), new Vector3(18f, 5f, 4f));
        CreateHill("Near Rolling Hill", hillNearMat, new Vector3(-5f, 0.7f, 21f), new Vector3(28f, 5f, 7f));

        CreateCloud("Cloud Left", cloudMat, new Vector3(-10f, 14f, 36f), 1.15f);
        CreateCloud("Cloud Right", cloudMat, new Vector3(7f, 16f, 38f), 0.9f);

        CreateTree("Left Tree", trunkMat, leafMat, new Vector3(-8.5f, 0f, 13f), 1.2f);
        CreateTree("Right Tree", trunkMat, leafMat, new Vector3(9f, 0f, 16f), 0.95f);

        for (var i = 0; i < 22; i++)
        {
            var x = -13f + i * 1.25f;
            var z = 4f + Mathf.Sin(i * 1.7f) * 1.7f + (i % 4) * 1.15f;
            CreateFlower("Flower " + (i + 1), flowerMat, new Vector3(x, 0.08f, z), 0.18f + (i % 3) * 0.03f);
        }

        if (!Directory.Exists("Assets/Scenes"))
        {
            Directory.CreateDirectory("Assets/Scenes");
        }

        EditorSceneManager.SaveScene(scene, ScenePath);
        AddSceneToBuildSettings();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (showDialog)
        {
            EditorUtility.DisplayDialog("Pippaloski Scenery", "Generated " + ScenePath, "Lovely");
        }
    }

    private static Light CreateSunLight()
    {
        var lightObject = new GameObject("Warm Sunlight");
        lightObject.transform.rotation = Quaternion.Euler(42f, -28f, 0f);
        var light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = new Color(1f, 0.82f, 0.55f);
        light.intensity = 2.2f;
        light.shadows = LightShadows.Soft;
        return light;
    }

    private static void CreateCamera()
    {
        var cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0f, 4.5f, -12f);
        cameraObject.transform.rotation = Quaternion.Euler(13f, 0f, 0f);
        var camera = cameraObject.AddComponent<Camera>();
        camera.fieldOfView = 55f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.45f, 0.7f, 0.96f);
        cameraObject.AddComponent<AudioListener>();
    }

    private static void CreateTree(string name, Material trunkMat, Material leafMat, Vector3 position, float scale)
    {
        var root = new GameObject(name);
        root.transform.position = position;

        var trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trunk.name = "Trunk";
        trunk.transform.SetParent(root.transform);
        trunk.transform.localPosition = new Vector3(0f, 1.1f * scale, 0f);
        trunk.transform.localScale = new Vector3(0.35f * scale, 1.1f * scale, 0.35f * scale);
        trunk.GetComponent<Renderer>().sharedMaterial = trunkMat;

        var leaves = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        leaves.name = "Leaves";
        leaves.transform.SetParent(root.transform);
        leaves.transform.localPosition = new Vector3(0f, 2.7f * scale, 0f);
        leaves.transform.localScale = new Vector3(2.1f * scale, 1.6f * scale, 2.1f * scale);
        leaves.GetComponent<Renderer>().sharedMaterial = leafMat;
    }

    private static void CreateCloud(string name, Material material, Vector3 position, float scale)
    {
        var root = new GameObject(name);
        root.transform.position = position;

        var offsets = new[]
        {
            new Vector3(-1.2f, 0f, 0f),
            new Vector3(0f, 0.28f, 0f),
            new Vector3(1.15f, 0f, 0f),
            new Vector3(0.55f, -0.18f, 0f)
        };

        for (var i = 0; i < offsets.Length; i++)
        {
            var puff = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            puff.name = "Puff " + (i + 1);
            puff.transform.SetParent(root.transform);
            puff.transform.localPosition = offsets[i] * scale;
            puff.transform.localScale = new Vector3(2.1f, 1.05f, 0.45f) * scale;
            puff.GetComponent<Renderer>().sharedMaterial = material;
        }
    }

    private static void CreateFlower(string name, Material material, Vector3 position, float scale)
    {
        var flower = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        flower.name = name;
        flower.transform.position = position;
        flower.transform.localScale = new Vector3(scale, scale, scale);
        flower.GetComponent<Renderer>().sharedMaterial = material;
    }

    private static void CreateHill(string name, Material material, Vector3 position, Vector3 scale)
    {
        var hill = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        hill.name = name;
        hill.transform.position = position;
        hill.transform.localScale = scale;
        hill.GetComponent<Renderer>().sharedMaterial = material;
    }

    private static void CreatePlane(string name, Material material, Vector3 position, Vector3 rotation, Vector3 scale)
    {
        var plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        plane.name = name;
        plane.transform.position = position;
        plane.transform.rotation = Quaternion.Euler(rotation);
        plane.transform.localScale = scale;
        plane.GetComponent<Renderer>().sharedMaterial = material;
    }

    private static void CreateSphere(string name, Material material, Vector3 position, Vector3 scale)
    {
        var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = name;
        sphere.transform.position = position;
        sphere.transform.localScale = scale;
        sphere.GetComponent<Renderer>().sharedMaterial = material;
    }

    private static Material CreateMaterial(string name, Color color, float smoothness)
    {
        var materialPath = MaterialFolder + "/" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(material, materialPath);
        }

        material.color = color;
        material.SetFloat("_Smoothness", smoothness);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void AddSceneToBuildSettings()
    {
        var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        if (scenes.Exists(scene => scene.path == ScenePath))
        {
            return;
        }

        scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
        {
            return;
        }

        var parent = Path.GetDirectoryName(path)?.Replace("\\", "/");
        var folder = Path.GetFileName(path);
        AssetDatabase.CreateFolder(parent, folder);
    }
}
