using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SenseiShop : MonoBehaviour
{
    [System.Serializable]
    public class ShopItem
    {
        public string itemName        = "Item";
        [TextArea(1, 2)]
        public string description     = "A mysterious item.";
        public int    price           = 10;
        public string grantsFlag      = "";
        public bool   oneTimePurchase = false;
        [HideInInspector] public bool purchased = false;
    }

    [Header("Shop Identity")]
    [Tooltip("Title shown at the top of the shop window.")]
    [SerializeField] private string shopTitle = "Sensei's Shop";

    [Header("Shop Items")]
    [SerializeField] private List<ShopItem> items = new List<ShopItem>();

    private GameObject canvasObj;
    private Transform  itemListParent;
    private Text       coinDisplay;
    private bool       shopBuilt = false;

    // ── Public entry ──────────────────────────────────────────────

    public void OpenShop()
    {
        Debug.Log("[SenseiShop] OpenShop called on " + gameObject.name);
        if (!shopBuilt) BuildShopUI();
        RefreshItems();
        canvasObj.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
        var player = FindObjectOfType<PlayerDogController>();
        if (player != null) player.MotorEnabled = false;
    }

    // ── Build UI ──────────────────────────────────────────────────

    private void BuildShopUI()
    {
        shopBuilt = true;

        // Canvas
        canvasObj = new GameObject(shopTitle + " Canvas");
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 30;
        canvasObj.AddComponent<CanvasScaler>().uiScaleMode =
            CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObj.AddComponent<GraphicRaycaster>();
        DontDestroyOnLoad(canvasObj);

        // Dim background
        var bg = new GameObject("Bg");
        bg.transform.SetParent(canvasObj.transform, false);
        var bgR = bg.AddComponent<RectTransform>();
        bgR.anchorMin = Vector2.zero; bgR.anchorMax = Vector2.one;
        bgR.offsetMin = bgR.offsetMax = Vector2.zero;
        bg.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);

        // Panel — fixed pixel size, centred
        var panel = new GameObject("Panel");
        panel.transform.SetParent(canvasObj.transform, false);
        var panelR = panel.AddComponent<RectTransform>();
        panelR.anchorMin        = new Vector2(0.5f, 0.5f);
        panelR.anchorMax        = new Vector2(0.5f, 0.5f);
        panelR.pivot            = new Vector2(0.5f, 0.5f);
        panelR.sizeDelta        = new Vector2(560f, 460f);
        panelR.anchoredPosition = Vector2.zero;
        panel.AddComponent<Image>().color = new Color(0.08f, 0.08f, 0.08f, 0.97f);

        // ── Header row ───────────────────────────────────────────
        var header = new GameObject("Header");
        header.transform.SetParent(panel.transform, false);
        var headerR = header.AddComponent<RectTransform>();
        headerR.anchorMin        = new Vector2(0f, 1f);
        headerR.anchorMax        = new Vector2(1f, 1f);
        headerR.pivot            = new Vector2(0.5f, 1f);
        headerR.offsetMin        = new Vector2(0f, -56f);
        headerR.offsetMax        = new Vector2(0f, 0f);

        var titleT = header.AddComponent<Text>();
        titleT.text      = shopTitle;
        titleT.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleT.fontSize  = 28;
        titleT.fontStyle = FontStyle.Bold;
        titleT.alignment = TextAnchor.MiddleCenter;
        titleT.color     = new Color(1f, 0.78f, 0.1f);

        // Coin display — top right of panel
        var coinGO = new GameObject("CoinDisplay");
        coinGO.transform.SetParent(panel.transform, false);
        var coinR = coinGO.AddComponent<RectTransform>();
        coinR.anchorMin        = new Vector2(1f, 1f);
        coinR.anchorMax        = new Vector2(1f, 1f);
        coinR.pivot            = new Vector2(1f, 1f);
        coinR.sizeDelta        = new Vector2(140f, 40f);
        coinR.anchoredPosition = new Vector2(-12f, -8f);
        coinDisplay            = coinGO.AddComponent<Text>();
        coinDisplay.font       = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        coinDisplay.fontSize   = 20;
        coinDisplay.fontStyle  = FontStyle.Bold;
        coinDisplay.alignment  = TextAnchor.MiddleRight;
        coinDisplay.color      = new Color(1f, 0.85f, 0.2f);

        // Gold divider line
        var div = new GameObject("Divider");
        div.transform.SetParent(panel.transform, false);
        var divR = div.AddComponent<RectTransform>();
        divR.anchorMin = new Vector2(0f, 1f);
        divR.anchorMax = new Vector2(1f, 1f);
        divR.pivot     = new Vector2(0.5f, 1f);
        divR.offsetMin = new Vector2(16f, -58f);
        divR.offsetMax = new Vector2(-16f, -56f);
        div.AddComponent<Image>().color = new Color(1f, 0.78f, 0.1f, 0.4f);

        // ── Item list — simple VLG, no scroll ────────────────────
        var listGO = new GameObject("ItemList");
        listGO.transform.SetParent(panel.transform, false);
        var listR = listGO.AddComponent<RectTransform>();
        listR.anchorMin = new Vector2(0f, 0f);
        listR.anchorMax = new Vector2(1f, 1f);
        listR.offsetMin = new Vector2(16f, 60f);
        listR.offsetMax = new Vector2(-16f, -64f);
        var vlg = listGO.AddComponent<VerticalLayoutGroup>();
        vlg.spacing               = 8f;
        vlg.padding               = new RectOffset(0, 0, 4, 4);
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth      = true;
        vlg.childControlHeight     = true;
        vlg.childAlignment         = TextAnchor.UpperCenter;
        itemListParent = listGO.transform;

        // ── Close button ─────────────────────────────────────────
        var closeGO = new GameObject("CloseBtn");
        closeGO.transform.SetParent(panel.transform, false);
        var closeR = closeGO.AddComponent<RectTransform>();
        closeR.anchorMin        = new Vector2(0.5f, 0f);
        closeR.anchorMax        = new Vector2(0.5f, 0f);
        closeR.pivot            = new Vector2(0.5f, 0f);
        closeR.sizeDelta        = new Vector2(120f, 38f);
        closeR.anchoredPosition = new Vector2(0f, 12f);
        closeGO.AddComponent<Image>().color = new Color(0.55f, 0.1f, 0.1f);
        var closeBtn = closeGO.AddComponent<Button>();
        closeBtn.onClick.AddListener(CloseShop);
        var closeLbl = new GameObject("Label");
        closeLbl.transform.SetParent(closeGO.transform, false);
        var closeLblR = closeLbl.AddComponent<RectTransform>();
        closeLblR.anchorMin = Vector2.zero; closeLblR.anchorMax = Vector2.one;
        closeLblR.offsetMin = closeLblR.offsetMax = Vector2.zero;
        var closeTxt = closeLbl.AddComponent<Text>();
        closeTxt.text      = "Close";
        closeTxt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        closeTxt.fontSize  = 20; closeTxt.fontStyle = FontStyle.Bold;
        closeTxt.alignment = TextAnchor.MiddleCenter;

        canvasObj.SetActive(false);
    }

    // ── Refresh rows ──────────────────────────────────────────────

    private void RefreshItems()
    {
        coinDisplay.text = "⬤  " + GameState.Coins;

        for (int c = itemListParent.childCount - 1; c >= 0; c--)
            DestroyImmediate(itemListParent.GetChild(c).gameObject);

        foreach (var item in items)
        {
            if (item.oneTimePurchase && item.purchased) continue;
            BuildRow(item);
        }
    }

    private void BuildRow(ShopItem item)
    {
        bool canAfford = GameState.Coins >= item.price;
        int  idx       = items.IndexOf(item);

        // Row container
        var row = new GameObject("Row");
        row.transform.SetParent(itemListParent, false);
        row.AddComponent<Image>().color = new Color(0.16f, 0.16f, 0.16f);
        var rowLE = row.AddComponent<LayoutElement>();
        rowLE.preferredHeight = 68f;
        rowLE.flexibleWidth   = 1f;
        var hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.padding               = new RectOffset(12, 12, 0, 0);
        hlg.spacing               = 10f;
        hlg.childAlignment        = TextAnchor.MiddleLeft;
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = true;
        hlg.childControlWidth      = true;
        hlg.childControlHeight     = true;

        // Info (name + desc stacked)
        var info = new GameObject("Info");
        info.transform.SetParent(row.transform, false);
        info.AddComponent<LayoutElement>().flexibleWidth = 1f;
        var infoVLG = info.AddComponent<VerticalLayoutGroup>();
        infoVLG.childForceExpandWidth  = true;
        infoVLG.childForceExpandHeight = false;
        infoVLG.childControlWidth      = true;
        infoVLG.childControlHeight     = true;
        infoVLG.childAlignment         = TextAnchor.MiddleLeft;
        infoVLG.spacing = 2f;

        AddText(info.transform, item.itemName, 19, FontStyle.Bold,
            Color.white, 28f);
        AddText(info.transform, item.description, 13, FontStyle.Normal,
            new Color(0.7f, 0.7f, 0.7f), 22f);

        // Price
        var priceColor = canAfford ? new Color(1f, 0.8f, 0.15f) : new Color(0.6f, 0.35f, 0.35f);
        var priceLE = AddText(row.transform, "⬤ " + item.price, 17, FontStyle.Bold,
            priceColor, 0f);
        priceLE.GetComponent<Text>().alignment = TextAnchor.MiddleCenter;
        priceLE.GetComponent<LayoutElement>().preferredWidth = 72f;

        // Buy button
        var buyGO = new GameObject("Buy");
        buyGO.transform.SetParent(row.transform, false);
        buyGO.AddComponent<Image>().color =
            canAfford ? new Color(0.1f, 0.42f, 0.14f) : new Color(0.22f, 0.22f, 0.22f);
        var buyBtnLE = buyGO.AddComponent<LayoutElement>();
        buyBtnLE.preferredWidth  = 72f;
        buyBtnLE.preferredHeight = 40f;
        var buyBtn = buyGO.AddComponent<Button>();
        buyBtn.interactable = canAfford;
        int captured = idx;
        buyBtn.onClick.AddListener(() => OnBuyClicked(captured));

        var buyLbl = new GameObject("Lbl");
        buyLbl.transform.SetParent(buyGO.transform, false);
        var buyLblR = buyLbl.AddComponent<RectTransform>();
        buyLblR.anchorMin = Vector2.zero; buyLblR.anchorMax = Vector2.one;
        buyLblR.offsetMin = buyLblR.offsetMax = Vector2.zero;
        var buyT = buyLbl.AddComponent<Text>();
        buyT.text = "Buy"; buyT.fontSize = 17; buyT.fontStyle = FontStyle.Bold;
        buyT.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        buyT.alignment = TextAnchor.MiddleCenter;
    }

    // Creates a Text GO, adds LayoutElement with preferredHeight, returns the GO
    private static GameObject AddText(Transform parent, string content,
        int size, FontStyle style, Color color, float preferredHeight)
    {
        var go = new GameObject("Text");
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<Text>();
        t.text      = content;
        t.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize  = size;
        t.fontStyle = style;
        t.color     = color;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        var le = go.AddComponent<LayoutElement>();
        if (preferredHeight > 0f) le.preferredHeight = preferredHeight;
        le.flexibleWidth = 1f;
        return go;
    }

    // ── Buy / Close ───────────────────────────────────────────────

    private void OnBuyClicked(int index)
    {
        if (index < 0 || index >= items.Count) return;
        var item = items[index];
        if (!GameState.SpendCoins(item.price)) return;
        if (!string.IsNullOrEmpty(item.grantsFlag)) GameState.SetFlag(item.grantsFlag);
        if (item.oneTimePurchase) item.purchased = true;
        RefreshItems();
    }

    private void CloseShop()
    {
        canvasObj.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;
        var player = FindObjectOfType<PlayerDogController>();
        if (player != null) player.MotorEnabled = true;
    }
}
