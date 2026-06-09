using UnityEngine;

public class CoinPickup : MonoBehaviour
{
    [SerializeField] private int value    = 1;
    [SerializeField] private float bobHeight = 0.15f;
    [SerializeField] private float bobSpeed  = 2f;
    [SerializeField] private float spinSpeed = 120f;

    private Vector3 startPos;

    private void Start()
    {
        startPos = transform.position;

        var col = gameObject.AddComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius    = 0.8f;

        if (GetComponentInChildren<Renderer>() == null)
            BuildCoinVisual();
    }

    private void Update()
    {
        transform.position = new Vector3(
            startPos.x,
            startPos.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight,
            startPos.z);
        transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<PlayerDogController>() == null &&
            !other.CompareTag("Player")) return;

        GameState.AddCoins(value);
        Destroy(gameObject);
    }

    private void BuildCoinVisual()
    {
        // Flat gold cylinder to look like a coin
        var coin = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        coin.name = "Coin Mesh";
        coin.transform.SetParent(transform);
        coin.transform.localPosition = Vector3.zero;
        coin.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        coin.transform.localScale    = new Vector3(0.4f, 0.06f, 0.4f);
        Destroy(coin.GetComponent<Collider>());

        var mat = new Material(coin.GetComponent<Renderer>().sharedMaterial);
        var gold = new Color(1f, 0.78f, 0.1f);
        mat.color = gold;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", gold);
        if (mat.HasProperty("_Color"))     mat.SetColor("_Color",     gold);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.8f);
        if (mat.HasProperty("_Metallic"))   mat.SetFloat("_Metallic",   0.9f);
        coin.GetComponent<Renderer>().material = mat;
    }
}
