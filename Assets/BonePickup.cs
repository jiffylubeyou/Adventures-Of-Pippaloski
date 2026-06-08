using UnityEngine;

public class BonePickup : MonoBehaviour
{
    [SerializeField] private float bobHeight = 0.18f;
    [SerializeField] private float bobSpeed  = 1.8f;
    [SerializeField] private float spinSpeed = 90f;

    private Vector3 startPos;

    private void Start()
    {
        startPos = transform.position;

        var trigger = gameObject.AddComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.radius = 1.2f;

        if (GetComponentInChildren<Renderer>() == null)
            BuildBoneVisual();
    }

    private void Update()
    {
        float newY = startPos.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = new Vector3(startPos.x, newY, startPos.z);
        transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World);
    }

    private void OnTriggerEnter(Collider other)
    {
        var dog = other.GetComponent<PlayerDogController>();
        if (!other.CompareTag("Player") && dog == null) return;

        GameState.SetFlag(GameState.HasBone);

        if (dog == null) dog = FindObjectOfType<PlayerDogController>();
        if (dog != null) dog.ShowBoneCollected();

        Destroy(gameObject);
    }

    private void BuildBoneVisual()
    {
        var temp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        var mat = new Material(temp.GetComponent<Renderer>().sharedMaterial);
        Destroy(temp);
        // Set color for both URP and Standard shader
        var boneColor = new Color(0.94f, 0.90f, 0.82f);
        mat.color = boneColor;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", boneColor);
        if (mat.HasProperty("_Color"))     mat.SetColor("_Color",     boneColor);

        CreatePart("Shaft",  PrimitiveType.Capsule, mat, Vector3.zero,                  new Vector3(0f, 0f, 90f), new Vector3(0.18f, 0.45f, 0.18f));
        CreatePart("KnobL1", PrimitiveType.Sphere,  mat, new Vector3(-0.52f,  0.12f, 0f), Vector3.zero,           Vector3.one * 0.28f);
        CreatePart("KnobL2", PrimitiveType.Sphere,  mat, new Vector3(-0.52f, -0.12f, 0f), Vector3.zero,           Vector3.one * 0.28f);
        CreatePart("KnobR1", PrimitiveType.Sphere,  mat, new Vector3( 0.52f,  0.12f, 0f), Vector3.zero,           Vector3.one * 0.28f);
        CreatePart("KnobR2", PrimitiveType.Sphere,  mat, new Vector3( 0.52f, -0.12f, 0f), Vector3.zero,           Vector3.one * 0.28f);
    }

    private void CreatePart(string partName, PrimitiveType type, Material mat,
        Vector3 localPos, Vector3 localRot, Vector3 localScale)
    {
        var part = GameObject.CreatePrimitive(type);
        part.name = partName;
        part.transform.SetParent(transform);
        part.transform.localPosition = localPos;
        part.transform.localRotation = Quaternion.Euler(localRot);
        part.transform.localScale    = localScale;
        part.GetComponent<Renderer>().sharedMaterial = mat;
        Destroy(part.GetComponent<Collider>());
    }
}
