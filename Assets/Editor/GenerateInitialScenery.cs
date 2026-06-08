using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class GenerateInitialScenery
{
    private const int SceneVersion = 10;
    private const string ScenePath = "Assets/Scenes/InitialScenery.unity";
    private const string MaterialFolder = "Assets/GeneratedAssets/Materials";
    private const string PrefabFolder = "Assets/GeneratedAssets/Prefabs";
    private const string PlayerPrefabPath = PrefabFolder + "/Pippaloski.prefab";
    private const string GeneratedVersionKey = "Pippaloski.InitialScenery.GeneratedVersion";
    private const string ImportedDogPrefabPath = "Assets/Simple Blocky Dogs Animated/1.prefabs/lassie_dog_animated.prefab";
    private const string ImportedDogMaterialPath = "Assets/Simple Blocky Dogs Animated/5.materials/world_Color.mat";
    private const string ImportedDogTexturePath = "Assets/Simple Blocky Dogs Animated/5.materials/dogs_texture.png";
    private const string CastleFbxPath = "Assets/Modular Castle/Assets/models/castle.fbx";
    private const string CastleMaterialPath = "Assets/Modular Castle/Assets/Materials/castle.mat";
    private const string CastleTexturePath = "Assets/Modular Castle/Assets/textures/castle.png";

    // Auto-generation disabled — scene is now hand-crafted.
    // Use Tools/Pippaloski/Generate Initial Scenery only to rebuild from scratch intentionally.

    [MenuItem("Tools/Pippaloski/Generate Initial Scenery")]
    public static void CreateScene()
    {
        CreateScene(true);
    }

    private static void CreateScene(bool showDialog)
    {
        EnsureFolder("Assets/GeneratedAssets");
        EnsureFolder(MaterialFolder);
        EnsureFolder(PrefabFolder);
        FixImportedDogMaterial();

        var skyMat = CreateMaterial("Sunset Sky", new Color(0.35f, 0.62f, 0.95f), 0.1f);
        var waterMat = CreateMaterial("Island Water", new Color(0.05f, 0.45f, 0.72f), 0.75f);
        var islandMat = CreateMaterial("Large Green Island", new Color(0.24f, 0.62f, 0.25f), 0.3f);
        var shoreMat = CreateMaterial("Shallow Shore", new Color(0.23f, 0.68f, 0.78f), 0.55f);
        var cloudMat = CreateMaterial("Soft Clouds", new Color(1f, 0.94f, 0.82f), 0.05f);
        var dogBlackMat = CreateMaterial("Border Collie Black Fur", new Color(0.03f, 0.03f, 0.035f), 0.25f);
        var dogWhiteMat = CreateMaterial("Border Collie White Fur", new Color(0.92f, 0.9f, 0.84f), 0.2f);
        var dogPinkMat = CreateMaterial("Dog Tongue", new Color(0.95f, 0.38f, 0.42f), 0.35f);

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "InitialScenery";

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.62f, 0.74f, 0.9f);
        RenderSettings.ambientEquatorColor = new Color(0.44f, 0.5f, 0.42f);
        RenderSettings.ambientGroundColor = new Color(0.2f, 0.17f, 0.12f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.7f, 0.82f, 0.92f);
        RenderSettings.fogStartDistance = 80f;
        RenderSettings.fogEndDistance = 400f;

        CreateCamera();
        var sunLight = CreateSunLight();
        RenderSettings.sun = sunLight;

        CreatePlane("Painted Sky Backdrop", skyMat, new Vector3(0f, 40f, 200f), new Vector3(90f, 0f, 0f), new Vector3(60f, 1f, 30f));

        CreateDecorativePlane("Surrounding Water", waterMat, new Vector3(0f, -0.12f, 14f), Vector3.zero, new Vector3(60f, 1f, 55f));
        CreateDecorativeCylinder("Shallow Water Ring", shoreMat, new Vector3(0f, -0.07f, 14f), new Vector3(135f, 0.03f, 110f));
        CreateIslandCylinder("Main Empty Green Island", islandMat, new Vector3(0f, 0.08f, 14f), new Vector3(120f, 0.45f, 95f));

        CreateCloud("Cloud Front Left", cloudMat, new Vector3(-13f, 14f, 34f), 1.15f);
        CreateCloud("Cloud Front Right", cloudMat, new Vector3(10f, 15.5f, 36f), 0.95f);
        CreateCloud("Cloud High Left", cloudMat, new Vector3(-22f, 20f, 48f), 1.35f);
        CreateCloud("Cloud High Center", cloudMat, new Vector3(0f, 22f, 55f), 1.1f);
        CreateCloud("Cloud High Right", cloudMat, new Vector3(24f, 19f, 50f), 1.3f);
        CreateCloud("Cloud Far Left", cloudMat, new Vector3(-34f, 17f, 62f), 1f);
        CreateCloud("Cloud Far Right", cloudMat, new Vector3(35f, 18f, 64f), 1.05f);

        FixCastleMaterial();
        PlaceCastle(new Vector3(0f, 0.53f, 14f));

        var player = CreateBorderColliePlayer(dogBlackMat, dogWhiteMat, dogPinkMat, new Vector3(0f, 2f, 14f));
        PrefabUtility.SaveAsPrefabAsset(player, PlayerPrefabPath);

        if (!Directory.Exists("Assets/Scenes"))
        {
            Directory.CreateDirectory("Assets/Scenes");
        }

        EditorSceneManager.SaveScene(scene, ScenePath);
        AddSceneToBuildSettings();
        EditorPrefs.SetInt(GeneratedVersionKey, SceneVersion);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (showDialog)
        {
            EditorUtility.DisplayDialog("Pippaloski Scenery", "Generated " + ScenePath, "Lovely");
        }
    }

    [MenuItem("Tools/Pippaloski/Fix Castle Materials")]
    public static void FixCastleMaterial()
    {
        var material = AssetDatabase.LoadAssetAtPath<Material>(CastleMaterialPath);
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(CastleTexturePath);
        if (material == null)
            return;

        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        if (shader != null)
            material.shader = shader;

        if (texture != null)
        {
            if (material.HasProperty("_BaseMap"))
                material.SetTexture("_BaseMap", texture);
            if (material.HasProperty("_MainTex"))
                material.SetTexture("_MainTex", texture);
        }

        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", Color.white);
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", Color.white);

        EditorUtility.SetDirty(material);
        AssetDatabase.SaveAssets();
    }

    private static void PlaceCastle(Vector3 position)
    {
        var castleFbx = AssetDatabase.LoadAssetAtPath<GameObject>(CastleFbxPath);
        if (castleFbx == null)
        {
            Debug.LogWarning("Castle FBX not found at " + CastleFbxPath);
            return;
        }

        var castle = (GameObject)PrefabUtility.InstantiatePrefab(castleFbx);
        castle.name = "Castle";
        castle.transform.position = position;

        // Add a MeshCollider to every mesh in the castle so the dog can't walk through walls
        foreach (var mf in castle.GetComponentsInChildren<MeshFilter>())
        {
            var go = mf.gameObject;
            if (go.GetComponent<Collider>() == null)
            {
                var mc = go.AddComponent<MeshCollider>();
                mc.sharedMesh = mf.sharedMesh;
            }
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
        cameraObject.transform.position = new Vector3(0f, 10f, -19f);
        cameraObject.transform.rotation = Quaternion.Euler(28f, 0f, 0f);
        var camera = cameraObject.AddComponent<Camera>();
        camera.fieldOfView = 58f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.45f, 0.7f, 0.96f);
        cameraObject.AddComponent<AudioListener>();
    }

    private static GameObject CreateBorderColliePlayer(Material blackMat, Material whiteMat, Material tongueMat, Vector3 position)
    {
        var player = new GameObject("Player Border Collie");
        player.transform.position = position;

        var controller = player.AddComponent<CharacterController>();
        controller.center = new Vector3(0f, 0.85f, 0f);
        controller.radius = 0.55f;
        controller.height = 1.75f;
        player.AddComponent<PlayerDogController>();

        var importedDogPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ImportedDogPrefabPath);
        if (importedDogPrefab != null)
        {
            var importedDog = (GameObject)PrefabUtility.InstantiatePrefab(importedDogPrefab);
            importedDog.name = "Imported Lassie Collie Model";
            importedDog.transform.SetParent(player.transform);
            importedDog.transform.localPosition = new Vector3(0f, 0.15f, 0f);
            var animator = importedDog.GetComponentInChildren<Animator>();
            if (animator != null)
            {
                animator.enabled = false;
            }
            importedDog.transform.localRotation = Quaternion.identity;
            importedDog.transform.localScale = new Vector3(0.45f, 0.45f, 0.45f);
            return player;
        }

        CreateDogPart("Body", player.transform, PrimitiveType.Capsule, blackMat, new Vector3(0f, 0.55f, 0f), new Vector3(90f, 0f, 0f), new Vector3(0.85f, 1.25f, 0.85f));
        CreateDogPart("White Chest", player.transform, PrimitiveType.Sphere, whiteMat, new Vector3(0f, 0.56f, 0.55f), Vector3.zero, new Vector3(0.55f, 0.5f, 0.3f));
        CreateDogPart("Head", player.transform, PrimitiveType.Sphere, blackMat, new Vector3(0f, 1.14f, 0.98f), Vector3.zero, new Vector3(0.72f, 0.58f, 0.68f));
        CreateDogPart("White Face Stripe", player.transform, PrimitiveType.Cube, whiteMat, new Vector3(0f, 1.18f, 1.34f), Vector3.zero, new Vector3(0.18f, 0.42f, 0.08f));
        CreateDogPart("Muzzle", player.transform, PrimitiveType.Sphere, whiteMat, new Vector3(0f, 1.03f, 1.42f), Vector3.zero, new Vector3(0.42f, 0.28f, 0.28f));
        CreateDogPart("Tongue", player.transform, PrimitiveType.Cube, tongueMat, new Vector3(0f, 0.9f, 1.58f), Vector3.zero, new Vector3(0.18f, 0.05f, 0.18f));

        CreateDogPart("Left Ear", player.transform, PrimitiveType.Cube, blackMat, new Vector3(-0.37f, 1.45f, 1.04f), new Vector3(0f, 0f, 25f), new Vector3(0.25f, 0.45f, 0.18f));
        CreateDogPart("Right Ear", player.transform, PrimitiveType.Cube, blackMat, new Vector3(0.37f, 1.45f, 1.04f), new Vector3(0f, 0f, -25f), new Vector3(0.25f, 0.45f, 0.18f));

        CreateDogPart("Left Front Leg", player.transform, PrimitiveType.Capsule, whiteMat, new Vector3(-0.38f, 0.2f, 0.65f), Vector3.zero, new Vector3(0.22f, 0.42f, 0.22f));
        CreateDogPart("Right Front Leg", player.transform, PrimitiveType.Capsule, whiteMat, new Vector3(0.38f, 0.2f, 0.65f), Vector3.zero, new Vector3(0.22f, 0.42f, 0.22f));
        CreateDogPart("Left Back Leg", player.transform, PrimitiveType.Capsule, blackMat, new Vector3(-0.38f, 0.2f, -0.55f), Vector3.zero, new Vector3(0.22f, 0.42f, 0.22f));
        CreateDogPart("Right Back Leg", player.transform, PrimitiveType.Capsule, blackMat, new Vector3(0.38f, 0.2f, -0.55f), Vector3.zero, new Vector3(0.22f, 0.42f, 0.22f));
        CreateDogPart("Tail", player.transform, PrimitiveType.Capsule, blackMat, new Vector3(0f, 0.8f, -1.05f), new Vector3(-55f, 0f, 0f), new Vector3(0.18f, 0.65f, 0.18f));

        return player;
    }

    private static void CreateDogPart(string name, Transform parent, PrimitiveType type, Material material, Vector3 localPosition, Vector3 localRotation, Vector3 localScale)
    {
        var part = GameObject.CreatePrimitive(type);
        part.name = name;
        part.transform.SetParent(parent);
        part.transform.localPosition = localPosition;
        part.transform.localRotation = Quaternion.Euler(localRotation);
        part.transform.localScale = localScale;
        part.GetComponent<Renderer>().sharedMaterial = material;
        Object.DestroyImmediate(part.GetComponent<Collider>());
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

    private static void CreateCylinder(string name, Material material, Vector3 position, Vector3 scale)
    {
        var cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cylinder.name = name;
        cylinder.transform.position = position;
        cylinder.transform.localScale = scale;
        cylinder.GetComponent<Renderer>().sharedMaterial = material;
    }

    // Island surface: replaces the broken CapsuleCollider (PhysX uses max XZ scale as radius,
    // turning non-uniform cylinders into giant invisible spheres) with a MeshCollider that
    // matches the actual oval cylinder geometry — no invisible rectangular corners into the water.
    private static void CreateIslandCylinder(string name, Material material, Vector3 position, Vector3 scale)
    {
        var cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cylinder.name = name;
        cylinder.transform.position = position;
        cylinder.transform.localScale = scale;
        cylinder.GetComponent<Renderer>().sharedMaterial = material;
        Object.DestroyImmediate(cylinder.GetComponent<CapsuleCollider>());
        var mc = cylinder.AddComponent<MeshCollider>();
        mc.sharedMesh = cylinder.GetComponent<MeshFilter>().sharedMesh;
    }

    // Decorative cylinder with no collider (shore ring, etc.).
    private static void CreateDecorativeCylinder(string name, Material material, Vector3 position, Vector3 scale)
    {
        var cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cylinder.name = name;
        cylinder.transform.position = position;
        cylinder.transform.localScale = scale;
        cylinder.GetComponent<Renderer>().sharedMaterial = material;
        Object.DestroyImmediate(cylinder.GetComponent<CapsuleCollider>());
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

    // Decorative plane with no collider so the dog can fall through it.
    private static void CreateDecorativePlane(string name, Material material, Vector3 position, Vector3 rotation, Vector3 scale)
    {
        var plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        plane.name = name;
        plane.transform.position = position;
        plane.transform.rotation = Quaternion.Euler(rotation);
        plane.transform.localScale = scale;
        plane.GetComponent<Renderer>().sharedMaterial = material;
        Object.DestroyImmediate(plane.GetComponent<MeshCollider>());
    }

    private static void CreateSphere(string name, Material material, Vector3 position, Vector3 scale)
    {
        var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = name;
        sphere.transform.position = position;
        sphere.transform.localScale = scale;
        sphere.GetComponent<Renderer>().sharedMaterial = material;
    }

    [MenuItem("Tools/Pippaloski/Fix Imported Dog Materials")]
    public static void FixImportedDogMaterial()
    {
        var material = AssetDatabase.LoadAssetAtPath<Material>(ImportedDogMaterialPath);
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(ImportedDogTexturePath);
        if (material == null)
        {
            return;
        }

        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        if (shader != null)
        {
            material.shader = shader;
        }

        if (texture != null)
        {
            if (material.HasProperty("_BaseMap"))
            {
                material.SetTexture("_BaseMap", texture);
            }

            if (material.HasProperty("_MainTex"))
            {
                material.SetTexture("_MainTex", texture);
            }
        }

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", Color.white);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", Color.white);
        }

        if (material.HasProperty("_Smoothness"))
        {
            material.SetFloat("_Smoothness", 0.25f);
        }

        EditorUtility.SetDirty(material);
        AssetDatabase.SaveAssets();
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
