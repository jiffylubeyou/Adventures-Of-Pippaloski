using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Coin-based shop attached to the Sensei.
// Add ShopItem entries in the Inspector, then call OpenShop() from a DialogueLine
// (wire it up via a dialogue option when you're ready to build the shop out).
public class SenseiShop : MonoBehaviour
{
    [System.Serializable]
    public class ShopItem
    {
        public string itemName    = "Item";
        [TextArea(1, 2)]
        public string description = "A mysterious item.";
        public int    price       = 10;
        // Optionally set a GameState flag when purchased
        public string grantsFlag  = "";
    }

    [Header("Shop Items")]
    [SerializeField] private List<ShopItem> items = new List<ShopItem>();

    // Call this from a DialogueLine response or button to open the shop UI
    public void OpenShop()
    {
        // TODO: build shop UI — ready to implement when you need it
        Debug.Log("[SenseiShop] OpenShop() called — shop UI not yet built.");
    }

    // Attempt to buy an item by index. Returns true on success.
    public bool TryBuy(int index)
    {
        if (index < 0 || index >= items.Count) return false;
        var item = items[index];

        if (!GameState.SpendCoins(item.price))
        {
            Debug.Log($"[SenseiShop] Not enough coins for {item.itemName} (costs {item.price}).");
            return false;
        }

        if (!string.IsNullOrEmpty(item.grantsFlag))
            GameState.SetFlag(item.grantsFlag);

        Debug.Log($"[SenseiShop] Bought {item.itemName}!");
        return true;
    }
}
