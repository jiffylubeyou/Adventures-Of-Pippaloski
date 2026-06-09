using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SenseiShop : MonoBehaviour
{
    [System.Serializable]
    public class ShopItem
    {
        public string itemName    = "Item";
        [TextArea(1, 2)]
        public string description = "A mysterious item.";
        public int    price       = 10;
        // GameState flag set on purchase (leave blank for none)
        public string grantsFlag  = "";
        // Hide this item once it has been bought
        public bool   oneTimePurchase = false;
        [HideInInspector] public bool purchased = false;
    }

    [Header("Shop Items")]
    [SerializeField] private List<ShopItem> items = new List<ShopItem>();

    // ── UI state ──────────────────────────────────────────────────
    private GameObject  canvasObj;
    private Transform   itemListParent;
    private Text        coinDisplay;
    private bool        shopBuilt = false;

    // ── Public entry point ────────────────────────────────────────

    public void OpenShop()
    {
        if (!shopBuilt) BuildShopUI();
        RefreshItems();
        canvasObj.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;

        var player = FindObjectOfType<PlayerDogController>();
        if (player != null) player.MotorEnabled = false;
    }

    // ── Build UI (called once) ────────────────────────────────────

    private void BuildShopUI()
    {
        shopBuilt = true;

        // Root canvas
        canvasObj = new GameObject("Sensei Shop Canvas");
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 30;
        canvasObj.AddComponent<CanvasScaler>().uiScaleMode =
            CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObj.AddComponent<GraphicRaycaster>();
        DontDestroyOnLoad(canvasObj);

        // Dim overlay
        var overlay = MakeImage("Overlay", canvas.transform,
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero,
            new Color(0f, 0f, 0f, 0.6f));

        // Panel
        var panel = MakeImage("Shop Panel", canvas.transform,
            new Vector2(0.2f, 0.1f), new Vector2(0.8f, 0.9f), new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero,
            new Color(0.07f, 0.07f, 0.07f, 0.97f));

        // Title
        var title = MakeText("Title", panel.transform, 32, FontStyle.Bold,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -50f), new Vector2(0f, 0f));
        title.text      = "Sensei's Shop";
        title.alignment = TextAnchor.MiddleCenter;
        title.color     = new Color(1f, 0.78f, 0.1f);

        // Divider
        var div = MakeImage("Divider", panel.transform,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            new Vector2(16f, -55f), new Vector2(-16f, -52f),
            new Color(1f, 0.78f, 0.1f, 0.5f));

        // Coin display (top-right of panel)
        var coinObj = MakeText("Coins", panel.transform, 22, FontStyle.Bold,
            new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-16f, -50f), new Vector2(0f, 0f));
        coinObj.alignment = TextAnchor.UpperRight;
        coinObj.color     = new Color(1f, 0.85f, 0.2f);
        coinDisplay       = coinObj;

        // Scrollable item list
        var scrollObj = new GameObject("Scroll");
        scrollObj.transform.SetParent(panel.transform, false);
        var scrollRect = scrollObj.AddComponent<RectTransform>();
        scrollRect.anchorMin        = new Vector2(0f, 0f);
        scrollRect.anchorMax        = new Vector2(1f, 1f);
        scrollRect.offsetMin        = new Vector2(16f, 60f);
        scrollRect.offsetMax        = new Vector2(-16f, -65f);
        var scroll = scrollObj.AddComponent<ScrollRect>();
        scroll.horizontal = false;

        var viewport = new GameObject("Viewport");
        viewport.transform.SetParent(scrollObj.transform, false);
        var vpRect = viewport.AddComponent<RectTransform>();
        vpRect.anchorMin = Vector2.zero; vpRect.anchorMax = Vector2.one;
        vpRect.offsetMin = vpRect.offsetMax = Vector2.zero;
        viewport.AddComponent<Image>().color = Color.clear;
        viewport.AddComponent<Mask>().showMaskGraphic = false;
        scroll.viewport = vpRect;

        var content = new GameObject("Content");
        content.transform.SetParent(viewport.transform, false);
        var contentRect = content.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot     = new Vector2(0.5f, 1f);
        contentRect.offsetMin = contentRect.offsetMax = Vector2.zero;
        var vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.spacing             = 8f;
        vlg.padding             = new RectOffset(0, 0, 0, 8);
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlHeight     = true;
        vlg.childControlWidth      = true;
        content.AddComponent<ContentSizeFitter>().verticalFit =
            ContentSizeFitter.FitMode.PreferredSize;
        scroll.content      = contentRect;
        itemListParent      = content.transform;

        // Close button
        var closeBtn = MakeImage("Close Button", panel.transform,
            new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f),
            new Vector2(-16f, 16f), new Vector2(0f, 56f),
            new Color(0.6f, 0.1f, 0.1f, 1f));
        var closeBtnComp = closeBtn.gameObject.AddComponent<Button>();
        closeBtnComp.onClick.AddListener(CloseShop);
        var closeTxt = MakeText("Label", closeBtn.transform, 20, FontStyle.Bold,
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero);
        closeTxt.text      = "Close";
        closeTxt.alignment = TextAnchor.MiddleCenter;

        canvasObj.SetActive(false);
    }

    // ── Refresh item rows each time shop opens ────────────────────

    private void RefreshItems()
    {
        coinDisplay.text = "⬤  " + GameState.Coins;

        // Clear old rows
        foreach (Transform child in itemListParent)
            Destroy(child.gameObject);

        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i];
            if (item.oneTimePurchase && item.purchased) continue;

            int capturedIndex = i;
            bool canAfford    = GameState.Coins >= item.price;

            // Row background
            var row = new GameObject("Row_" + item.itemName);
            row.transform.SetParent(itemListParent, false);
            var rowImg = row.AddComponent<Image>();
            rowImg.color = new Color(0.15f, 0.15f, 0.15f, 0.9f);
            var le = row.AddComponent<LayoutElement>();
            le.preferredHeight = 72f;
            le.flexibleWidth   = 1f;

            // Item name
            var nameT = MakeText("Name", row.transform, 20, FontStyle.Bold,
                new Vector2(0f, 0.5f), new Vector2(0.6f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(12f, 0f), new Vector2(0f, 0f));
            nameT.text      = item.itemName;
            nameT.alignment = TextAnchor.MiddleLeft;

            // Description
            var descT = MakeText("Desc", row.transform, 15, FontStyle.Normal,
                new Vector2(0f, 0f), new Vector2(0.6f, 0.45f), new Vector2(0f, 0f),
                new Vector2(12f, 6f), new Vector2(0f, 0f));
            descT.text      = item.description;
            descT.color     = new Color(0.75f, 0.75f, 0.75f);
            descT.alignment = TextAnchor.LowerLeft;

            // Price tag
            var priceT = MakeText("Price", row.transform, 18, FontStyle.Bold,
                new Vector2(0.6f, 0.5f), new Vector2(0.75f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero);
            priceT.text      = "⬤ " + item.price;
            priceT.color     = canAfford ? new Color(1f, 0.78f, 0.1f) : new Color(0.6f, 0.4f, 0.4f);
            priceT.alignment = TextAnchor.MiddleCenter;

            // Buy button
            var buyBg = MakeImage("Buy", row.transform,
                new Vector2(0.75f, 0.15f), new Vector2(0.97f, 0.85f), new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero,
                canAfford ? new Color(0.1f, 0.45f, 0.15f) : new Color(0.25f, 0.25f, 0.25f));
            var buyBtn = buyBg.gameObject.AddComponent<Button>();
            buyBtn.interactable = canAfford;
            buyBtn.onClick.AddListener(() => OnBuyClicked(capturedIndex));
            var buyTxt = MakeText("Label", buyBg.transform, 18, FontStyle.Bold,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero);
            buyTxt.text      = "Buy";
            buyTxt.alignment = TextAnchor.MiddleCenter;
        }
    }

    private void OnBuyClicked(int index)
    {
        var item = items[index];
        if (!GameState.SpendCoins(item.price)) return;

        if (!string.IsNullOrEmpty(item.grantsFlag))
            GameState.SetFlag(item.grantsFlag);

        if (item.oneTimePurchase)
            item.purchased = true;

        RefreshItems();   // update coin display + grey out bought items
    }

    private void CloseShop()
    {
        canvasObj.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;

        var player = FindObjectOfType<PlayerDogController>();
        if (player != null) player.MotorEnabled = true;
    }

    // ── UI helpers ────────────────────────────────────────────────

    private static Image MakeImage(string name, Transform parent,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
        Vector2 offsetMin, Vector2 offsetMax, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var r = go.AddComponent<RectTransform>();
        r.anchorMin = anchorMin; r.anchorMax = anchorMax; r.pivot = pivot;
        r.offsetMin = offsetMin; r.offsetMax = offsetMax;
        var img = go.AddComponent<Image>();
        img.color = color;
        return img;
    }

    private static Text MakeText(string name, Transform parent, int size, FontStyle style,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
        Vector2 offsetMin, Vector2 offsetMax)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var r = go.AddComponent<RectTransform>();
        r.anchorMin = anchorMin; r.anchorMax = anchorMax; r.pivot = pivot;
        r.offsetMin = offsetMin; r.offsetMax = offsetMax;
        var t = go.AddComponent<Text>();
        t.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize  = size;
        t.fontStyle = style;
        t.color     = Color.white;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        return t;
    }
}
