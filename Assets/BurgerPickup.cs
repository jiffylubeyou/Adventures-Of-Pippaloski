using UnityEngine;

/// <summary>
/// Added automatically to the burger by WitchLunchQuest when it spawns.
/// The spawn code guarantees a SphereCollider trigger + kinematic Rigidbody
/// on this same GameObject, so OnTriggerEnter fires reliably.
/// </summary>
public class BurgerPickup : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (!IsPlayer(other)) return;

        GameState.SetFlag("has_burger");
        Debug.Log("[BurgerPickup] Picked up!");
        Destroy(gameObject);
    }

    private static bool IsPlayer(Collider other)
    {
        return other.CompareTag("Player")
            || other.GetComponent<PlayerDogController>() != null
            || other.name == "Pippaloski";
    }
}
