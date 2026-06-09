using UnityEngine;

/// <summary>
/// Place on the burger GameObject (WitchLunchQuest adds this automatically).
/// When Pippaloski walks into it, sets the "has_burger" flag and destroys the pickup.
/// </summary>
[RequireComponent(typeof(Collider))]
public class BurgerPickup : MonoBehaviour
{
    private void Start()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsPlayer(other)) return;

        GameState.SetFlag("has_burger");
        Debug.Log("[BurgerPickup] Burger picked up!");
        Destroy(gameObject);
    }

    private static bool IsPlayer(Collider other)
    {
        return other.CompareTag("Player")
            || other.GetComponent<PlayerDogController>() != null
            || other.name == "Pippaloski";
    }
}
