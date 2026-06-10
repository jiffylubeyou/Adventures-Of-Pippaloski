using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Attach to each obelisk GameObject alongside DialogueTrigger and SenseiShop.
///
/// Each obelisk has a unique ID and a display name shown in the travel menu.
/// When the player buys the Fast Travel upgrade from this obelisk's shop,
/// the flag  "fasttravel_{obeliskId}"  is set.
///
/// Obelisks that share the upgrade can see one another in the travel menu.
/// Selecting a destination instantly moves the player to that obelisk's
/// arrival point.
///
/// Setup per obelisk:
///   1. Add ObeliskManager, SenseiShop, and DialogueTrigger.
///   2. Give each a unique Obelisk Id (e.g. "desert", "jungle", "beach").
///   3. Set Display Name (shown in the travel picker).
///   4. Optionally assign an Arrival Point transform (defaults to this GameObject).
///   5. In SenseiShop set:
///        shopTitle  = "Obelisk Shop"  (or whatever)
///        Add item:  name="Fast Travel Network"
///                   grantsFlag="fasttravel_{yourObeliskId}"
///                   oneTimePurchase=true
///   6. In DialogueTrigger add two lines:
///        Line A:  playerPrompt="Visit the shop"   opensShop=true
///        Line B:  playerPrompt="Travel to another obelisk"
///                 requiredFlags=["fasttravel_{yourObeliskId}"]
///                 opensTravelMenu=true
/// </summary>
public class ObeliskManager : MonoBehaviour
{
    [Header("Identity")]
    [Tooltip("Unique short ID used for the fast-travel flag, e.g. 'desert' or 'obelisk_01'.")]
    [SerializeField] private string obeliskId = "obelisk_01";

    [Tooltip("Name shown in the travel destination picker.")]
    [SerializeField] private string displayName = "Obelisk";

    [Tooltip("Where the player appears when teleporting here. Leave empty to use this GameObject's position.")]
    [SerializeField] private Transform arrivalPoint;

    // ── Static registry ───────────────────────────────────────────────────────
    private static readonly Dictionary<string, ObeliskManager> Registry
        = new Dictionary<string, ObeliskManager>();

    // ── Travel UI ─────────────────────────────────────────────────────────────
    private GameObject travelCanvas;
    private Transform  destinationParent;
    private bool       uiBuilt = false;

    // ── Fast-travel flag helpers ──────────────────────────────────────────────
    public string FastTravelFlag => "fasttravel_" + obeliskId;
    public bool   HasFastTravel  => GameState.HasFlag(FastTravelFlag);
    public string DisplayName    => displayName;
    public string ObeliskId      => obeliskId;

    // ─────────────────────────────────────────────────────────────────────────

    private void OnEnable()
    {
        Registry[obeliskId] = this;
    }

    private void OnDisable()
    {
        Registry.Remove(obeliskId);
    }

    // ── Called by DialogueTrigger when opensTravelMenu line is chosen ─────────

    public void OpenTravelMenu()
    {
        if (!uiBuilt) BuildTravelUI();
        RefreshDestinations();
        travelCanvas.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
        var player = FindObjectOfType<PlayerDogController>();
        if (player != null) player.MotorEnabled = false;
    }

    private void CloseTravelMenu()
    {
        travelCanvas.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;
        var player = FindObjectOfType<PlayerDogController>();
        if (player != null) player.MotorEnabled = true;
    }

    // ── Teleport ──────────────────────────────────────────────────────────────

    public void TeleportPlayerHere()
    {
        var playerObj = GameObject.FindGameObjectWithTag("Player")
                     ?? GameObject.Find("Pippaloski")
                     ?? GameObject.Find("Player Border Collie");
        if (playerObj == null) { Debug.LogWarning("[ObeliskManager] Player not found!"); return; }

        Vector3 dest = arrivalPoint != null
            ? arrivalPoint.position
            : transform.position + transform.forward * 2f + Vector3.up * 0.5f;

        // CharacterController must be disabled to warp via transform
        var cc = playerObj.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
        playerObj.transform.position = dest;
        if (cc != null) cc.enabled = true;

        Debug.Log("[ObeliskManager] Teleported player to " + displayName);
    }

    // ── Build Travel UI ───────────────────────────────────────────────────────

    private void BuildTravelUI()
    {
        uiBuilt = true;

        travelCanvas = new GameObject("Obelisk Travel Canvas");
        var canvas = travelCanvas.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 35;
        travelCanvas.AddComponent<CanvasScaler>().uiScaleMode =
            CanvasScaler.ScaleMode.ScaleWithScreenSize;
        travelCanvas.AddComponent<GraphicRaycaster>();
        DontDestroyOnLoad(travelCanvas);

        // Dim overlay
        var bg = new GameObject("Bg");
        bg.transform.SetParent(travelCanvas.transform, false);
        var bgR = bg.AddComponent<RectTransform>();
        bgR.anchorMin = Vector2.zero; bgR.anchorMax = Vector2.one;
        bgR.offsetMin = bgR.offsetMax = Vector2.zero;
        bg.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);

        // Panel
        var panel = new GameObject("Panel");
        panel.transform.SetParent(travelCanvas.transform, false);
        var panelR = panel.AddComponent<RectTransform>();
        panelR.anchorMin        = new Vector2(0.5f, 0.5f);
        panelR.anchorMax        = new Vector2(0.5f, 0.5f);
        panelR.pivot            = new Vector2(0.5f, 0.5f);
        panelR.sizeDelta        = new Vector2(480f, 420f);
        panelR.anchoredPosition = Vector2.zero;
        panel.AddComponent<Image>().color = new Color(0.08f, 0.08f, 0.08f, 0.97f);

        // Header
        var headerGO = new GameObject("Header");
        headerGO.transform.SetParent(panel.transform, false);
        var headerR = headerGO.AddComponent<RectTransform>();
        headerR.anchorMin = new Vector2(0f, 1f); headerR.anchorMax = new Vector2(1f, 1f);
        headerR.pivot     = new Vector2(0.5f, 1f);
        headerR.offsetMin = new Vector2(0f, -56f); headerR.offsetMax = Vector2.zero;
        var titleT = headerGO.AddComponent<Text>();
        titleT.text      = "Fast Travel";
        titleT.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleT.fontSize  = 28; titleT.fontStyle = FontStyle.Bold;
        titleT.alignment = TextAnchor.MiddleCenter;
        titleT.color     = new Color(1f, 0.78f, 0.1f);

        // Subtitle
        var subGO = new GameObject("Sub");
        subGO.transform.SetParent(panel.transform, false);
        var subR = subGO.AddComponent<RectTransform>();
        subR.anchorMin = new Vector2(0f, 1f); subR.anchorMax = new Vector2(1f, 1f);
        subR.pivot     = new Vector2(0.5f, 1f);
        subR.offsetMin = new Vector2(16f, -82f); subR.offsetMax = new Vector2(-16f, -56f);
        var subT = subGO.AddComponent<Text>();
        subT.text      = "Choose a destination";
        subT.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        subT.fontSize  = 16; subT.alignment = TextAnchor.MiddleCenter;
        subT.color     = new Color(0.7f, 0.7f, 0.7f);

        // Gold divider
        var div = new GameObject("Divider");
        div.transform.SetParent(panel.transform, false);
        var divR = div.AddComponent<RectTransform>();
        divR.anchorMin = new Vector2(0f, 1f); divR.anchorMax = new Vector2(1f, 1f);
        divR.pivot     = new Vector2(0.5f, 1f);
        divR.offsetMin = new Vector2(16f, -86f); divR.offsetMax = new Vector2(-16f, -84f);
        div.AddComponent<Image>().color = new Color(1f, 0.78f, 0.1f, 0.4f);

        // Destination list
        var listGO = new GameObject("Destinations");
        listGO.transform.SetParent(panel.transform, false);
        var listR = listGO.AddComponent<RectTransform>();
        listR.anchorMin = new Vector2(0f, 0f); listR.anchorMax = new Vector2(1f, 1f);
        listR.offsetMin = new Vector2(16f, 60f); listR.offsetMax = new Vector2(-16f, -90f);
        var vlg = listGO.AddComponent<VerticalLayoutGroup>();
        vlg.spacing               = 8f;
        vlg.padding               = new RectOffset(0, 0, 4, 4);
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth      = true;
        vlg.childControlHeight     = true;
        destinationParent = listGO.transform;

        // Close button
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
        closeBtn.onClick.AddListener(CloseTravelMenu);
        AddLabel(closeGO.transform, "Cancel", 18, FontStyle.Bold, Color.white);

        travelCanvas.SetActive(false);
    }

    private void RefreshDestinations()
    {
        // Clear old buttons
        for (int i = destinationParent.childCount - 1; i >= 0; i--)
            DestroyImmediate(destinationParent.GetChild(i).gameObject);

        bool anyAvailable = false;

        foreach (var kv in Registry)
        {
            var other = kv.Value;

            // Don't show this obelisk (you're already here)
            if (other.obeliskId == obeliskId) continue;

            // Only show obelisks that have purchased fast travel
            if (!other.HasFastTravel) continue;

            anyAvailable = true;
            BuildDestinationRow(other);
        }

        if (!anyAvailable)
            BuildNoDestinationsLabel();
    }

    private void BuildDestinationRow(ObeliskManager target)
    {
        var row = new GameObject("Row_" + target.obeliskId);
        row.transform.SetParent(destinationParent, false);
        row.AddComponent<Image>().color = new Color(0.16f, 0.16f, 0.16f);
        var rowLE = row.AddComponent<LayoutElement>();
        rowLE.preferredHeight = 62f;
        rowLE.flexibleWidth   = 1f;
        var hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.padding               = new RectOffset(14, 12, 0, 0);
        hlg.spacing               = 10f;
        hlg.childAlignment        = TextAnchor.MiddleLeft;
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = true;
        hlg.childControlWidth      = true;
        hlg.childControlHeight     = true;

        // Name label (expands to fill)
        var nameGO = new GameObject("Name");
        nameGO.transform.SetParent(row.transform, false);
        nameGO.AddComponent<LayoutElement>().flexibleWidth = 1f;
        var nameT = nameGO.AddComponent<Text>();
        nameT.text      = target.displayName;
        nameT.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        nameT.fontSize  = 20; nameT.fontStyle = FontStyle.Bold;
        nameT.color     = Color.white;
        nameT.alignment = TextAnchor.MiddleLeft;

        // Travel button
        var btnGO = new GameObject("TravelBtn");
        btnGO.transform.SetParent(row.transform, false);
        btnGO.AddComponent<Image>().color = new Color(0.1f, 0.35f, 0.55f);
        var btnLE = btnGO.AddComponent<LayoutElement>();
        btnLE.preferredWidth  = 90f;
        btnLE.preferredHeight = 40f;
        var btn = btnGO.AddComponent<Button>();
        var captured = target;
        btn.onClick.AddListener(() =>
        {
            CloseTravelMenu();
            captured.TeleportPlayerHere();
        });
        AddLabel(btnGO.transform, "Travel", 16, FontStyle.Bold, Color.white);
    }

    private void BuildNoDestinationsLabel()
    {
        var go = new GameObject("NoDestinations");
        go.transform.SetParent(destinationParent, false);
        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = 60f; le.flexibleWidth = 1f;
        var t = go.AddComponent<Text>();
        t.text      = "No other obelisks have purchased\nFast Travel yet.";
        t.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize  = 16; t.alignment = TextAnchor.MiddleCenter;
        t.color     = new Color(0.6f, 0.6f, 0.6f);
    }

    private static void AddLabel(Transform parent, string text, int size,
                                  FontStyle style, Color color)
    {
        var go = new GameObject("Label");
        go.transform.SetParent(parent, false);
        var r = go.AddComponent<RectTransform>();
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
        r.offsetMin = r.offsetMax = Vector2.zero;
        var t = go.AddComponent<Text>();
        t.text      = text;
        t.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize  = size; t.fontStyle = style;
        t.alignment = TextAnchor.MiddleCenter;
        t.color     = color;
    }

    private void OnDrawGizmosSelected()
    {
        // Show arrival point in scene view
        Vector3 dest = arrivalPoint != null
            ? arrivalPoint.position
            : transform.position + transform.forward * 2f;
        Gizmos.color = new Color(0.1f, 0.6f, 1f);
        Gizmos.DrawWireSphere(dest, 0.4f);
        Gizmos.DrawLine(transform.position, dest);
    }
}
