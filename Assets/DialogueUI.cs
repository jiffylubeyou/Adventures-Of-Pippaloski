using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// Self-building dialogue canvas. One instance lives in the scene; DialogueTrigger calls into it.
/// </summary>
public class DialogueUI : MonoBehaviour
{
    // ---- singleton ----
    private static DialogueUI instance;
    public static DialogueUI GetOrCreate()
    {
        if (instance != null) return instance;
        var go = new GameObject("Dialogue UI");
        instance = go.AddComponent<DialogueUI>();
        return instance;
    }

    // ---- prompt ownership ----
    // Each NPC registers itself every frame it is in range.
    // The UI resolves which one to show (nearest) once per frame in LateUpdate.
    private struct PromptRequest
    {
        public string  message;
        public float   distance;
    }
    private readonly Dictionary<object, PromptRequest> promptRequests = new Dictionary<object, PromptRequest>();

    // ---- UI references ----
    private GameObject promptObj;
    private Text promptText;

    private GameObject panelObj;
    private Text speakerText;
    private Text bodyText;
    private Transform optionParent;
    private readonly List<GameObject> optionButtons = new List<GameObject>();

    private GameObject continueButtonObj;
    private Action continueAction;

    private void Awake()
    {
        instance = this;
        BuildCanvas();
        HideAll();
    }

    // ================================================================
    //  Public API
    // ================================================================

    // Called every frame by any NPC that is in range.
    // 'key' is just the NPC's MonoBehaviour instance — used as a unique token.
    public void RequestPrompt(object key, string message, float distance)
    {
        promptRequests[key] = new PromptRequest { message = message, distance = distance };
    }

    // Called when an NPC goes out of range or starts talking.
    public void ReleasePrompt(object key)
    {
        promptRequests.Remove(key);
    }

    // Resolves the closest request and updates the prompt display once per frame.
    private void LateUpdate()
    {
        if (panelObj != null && panelObj.activeSelf)
        {
            // Dialogue panel is open — suppress all prompts
            promptRequests.Clear();
            promptObj.SetActive(false);
            return;
        }

        if (promptRequests.Count == 0)
        {
            promptObj.SetActive(false);
            return;
        }

        string  bestMessage  = null;
        float   bestDist     = float.MaxValue;
        foreach (var kv in promptRequests)
        {
            if (kv.Value.distance < bestDist)
            {
                bestDist    = kv.Value.distance;
                bestMessage = kv.Value.message;
            }
        }

        promptText.text = bestMessage;
        promptObj.SetActive(true);
    }

    // Legacy helpers kept so nothing else breaks
    public void ShowPrompt(string message) { /* now driven by RequestPrompt */ }
    public void HidePrompt()              { /* now driven by ReleasePrompt  */ }

    public void OpenDialogue(string speaker, string body, DialogueLine[] options, Action<DialogueLine> onChosen,
        Action onEmptyContinue = null)
    {
        panelObj.SetActive(true);
        speakerText.text = speaker;
        bodyText.text = body;

        ClearOptions();
        continueButtonObj.SetActive(false);

        foreach (var line in options)
        {
            var line_captured = line;
            AddOptionButton(line.playerPrompt, () => onChosen(line_captured));
        }

        // No response options — show a Continue button so the player isn't
        // trapped. Used by zones that just announce something.
        if (options == null || options.Length == 0)
        {
            continueAction = onEmptyContinue ?? CloseDialogue;
            continueButtonObj.SetActive(true);
        }

        // Unlock cursor and disable movement while talking
        SetPlayerMovement(false);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ShowResponse(string speaker, string body, Action onContinue)
    {
        speakerText.text = speaker;
        bodyText.text = body;
        ClearOptions();
        continueAction = onContinue;
        continueButtonObj.SetActive(true);
    }

    public void CloseDialogue()
    {
        panelObj.SetActive(false);
        ClearOptions();
        SetPlayerMovement(true);
    }

    // ================================================================
    //  Internal helpers
    // ================================================================

    private void HideAll()
    {
        promptObj.SetActive(false);
        panelObj.SetActive(false);
    }

    private void ClearOptions()
    {
        foreach (var b in optionButtons)
            Destroy(b);
        optionButtons.Clear();
    }

    private void AddOptionButton(string label, Action onClick)
    {
        var btn = new GameObject("Option");
        btn.transform.SetParent(optionParent, false);

        var rect = btn.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0f, 44f);

        var le = btn.AddComponent<LayoutElement>();
        le.preferredHeight = 44f;
        le.flexibleWidth = 1f;

        var img = btn.AddComponent<Image>();
        img.color = new Color(0.15f, 0.15f, 0.15f, 0.85f);

        var button = btn.AddComponent<Button>();
        var colors = button.colors;
        colors.highlightedColor = new Color(0.3f, 0.3f, 0.3f, 1f);
        colors.pressedColor = new Color(0.5f, 0.5f, 0.5f, 1f);
        button.colors = colors;
        button.onClick.AddListener(() => onClick());

        var textObj = new GameObject("Label");
        textObj.transform.SetParent(btn.transform, false);
        var tr = textObj.AddComponent<RectTransform>();
        tr.anchorMin = Vector2.zero;
        tr.anchorMax = Vector2.one;
        tr.offsetMin = new Vector2(10f, 0f);
        tr.offsetMax = new Vector2(-10f, 0f);
        var t = textObj.AddComponent<Text>();
        t.text = label;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = 20;
        t.color = Color.white;
        t.alignment = TextAnchor.MiddleLeft;

        optionButtons.Add(btn);
    }

    private static void SetPlayerMovement(bool enabled)
    {
        var controller = FindObjectOfType<PlayerDogController>();
        if (controller != null)
            controller.enabled = enabled;

        if (enabled)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    // ================================================================
    //  Canvas construction
    // ================================================================

    private void BuildCanvas()
    {
        if (FindObjectOfType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<InputSystemUIInputModule>();
        }

        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        gameObject.AddComponent<GraphicRaycaster>();

        BuildPrompt();
        BuildDialoguePanel();
    }

    private void BuildPrompt()
    {
        promptObj = new GameObject("Prompt");
        promptObj.transform.SetParent(transform, false);

        var rect = promptObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.sizeDelta = new Vector2(400f, 40f);
        rect.anchoredPosition = new Vector2(0f, 60f);

        var bg = promptObj.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.55f);

        var textObj = new GameObject("Text");
        textObj.transform.SetParent(promptObj.transform, false);
        var tr = textObj.AddComponent<RectTransform>();
        tr.anchorMin = Vector2.zero;
        tr.anchorMax = Vector2.one;
        tr.offsetMin = tr.offsetMax = Vector2.zero;
        promptText = textObj.AddComponent<Text>();
        promptText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        promptText.fontSize = 18;
        promptText.color = Color.white;
        promptText.alignment = TextAnchor.MiddleCenter;
    }

    private void BuildDialoguePanel()
    {
        panelObj = new GameObject("Dialogue Panel");
        panelObj.transform.SetParent(transform, false);

        // Anchor to the bottom-centre of the screen, grow upward automatically
        var panelRect = panelObj.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.1f, 0f);
        panelRect.anchorMax = new Vector2(0.9f, 0f);
        panelRect.pivot     = new Vector2(0.5f, 0f);
        panelRect.anchoredPosition = new Vector2(0f, 20f);
        panelRect.sizeDelta = new Vector2(0f, 0f); // height driven by CSF

        var bg = panelObj.AddComponent<Image>();
        bg.color = new Color(0.05f, 0.05f, 0.05f, 0.92f);

        // VerticalLayoutGroup on the panel so children stack and drive its height
        var panelVLG = panelObj.AddComponent<VerticalLayoutGroup>();
        panelVLG.padding               = new RectOffset(0, 0, 0, 8);
        panelVLG.spacing               = 0f;
        panelVLG.childForceExpandWidth  = true;
        panelVLG.childForceExpandHeight = false;
        panelVLG.childControlWidth      = true;
        panelVLG.childControlHeight     = true;

        // ContentSizeFitter makes the panel tall enough to fit everything
        var csf = panelObj.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // ── Speaker name bar ─────────────────────────────────────────────────
        var nameBar = new GameObject("NameBar");
        nameBar.transform.SetParent(panelObj.transform, false);
        nameBar.AddComponent<Image>().color = new Color(0.2f, 0.12f, 0.04f, 1f);
        var nameLE = nameBar.AddComponent<LayoutElement>();
        nameLE.preferredHeight = 36f;
        speakerText = MakeText(nameBar.transform, 22, FontStyle.Bold, TextAnchor.MiddleLeft, new Vector2(12f, 0f));

        // ── Body text ────────────────────────────────────────────────────────
        var bodyArea = new GameObject("Body");
        bodyArea.transform.SetParent(panelObj.transform, false);
        var bodyLE = bodyArea.AddComponent<LayoutElement>();
        bodyLE.minHeight = 60f;
        bodyLE.flexibleHeight = 1f;
        bodyText = MakeText(bodyArea.transform, 20, FontStyle.Normal, TextAnchor.UpperLeft, new Vector2(12f, 8f));
        bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
        bodyText.verticalOverflow   = VerticalWrapMode.Overflow;

        // ── Divider ──────────────────────────────────────────────────────────
        var divider = new GameObject("Divider");
        divider.transform.SetParent(panelObj.transform, false);
        divider.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.08f);
        var divLE = divider.AddComponent<LayoutElement>();
        divLE.preferredHeight = 1f;

        // ── Options list — grows with the number of buttons ──────────────────
        var optionsArea = new GameObject("Options");
        optionsArea.transform.SetParent(panelObj.transform, false);
        var optLE = optionsArea.AddComponent<LayoutElement>();
        optLE.flexibleHeight = 0f; // let CSF on child drive the height

        var optCSF = optionsArea.AddComponent<ContentSizeFitter>();
        optCSF.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var layout = optionsArea.AddComponent<VerticalLayoutGroup>();
        layout.padding               = new RectOffset(10, 10, 6, 0);
        layout.spacing               = 6f;
        layout.childForceExpandWidth  = true;
        layout.childForceExpandHeight = false;
        layout.childControlHeight     = true;
        layout.childControlWidth      = true;
        optionParent = optionsArea.transform;

        // ── Continue button ──────────────────────────────────────────────────
        continueButtonObj = new GameObject("ContinueButton");
        continueButtonObj.transform.SetParent(panelObj.transform, false);
        var continueLE = continueButtonObj.AddComponent<LayoutElement>();
        continueLE.preferredHeight = 44f;
        var cImg = continueButtonObj.AddComponent<Image>();
        cImg.color = new Color(0.2f, 0.12f, 0.04f, 1f);
        var cBtn = continueButtonObj.AddComponent<Button>();
        cBtn.onClick.AddListener(() => continueAction?.Invoke());

        // Right-align the Continue text
        var cTextGO = new GameObject("Text");
        cTextGO.transform.SetParent(continueButtonObj.transform, false);
        var cTextR = cTextGO.AddComponent<RectTransform>();
        cTextR.anchorMin = Vector2.zero; cTextR.anchorMax = Vector2.one;
        cTextR.offsetMin = new Vector2(0f, 0f); cTextR.offsetMax = new Vector2(-14f, 0f);
        var cText = cTextGO.AddComponent<Text>();
        cText.text      = "Continue  >";
        cText.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        cText.fontSize  = 20;
        cText.fontStyle = FontStyle.Bold;
        cText.alignment = TextAnchor.MiddleRight;
        cText.color     = Color.white;
    }

    private static GameObject MakeChild(string name, Transform parent,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
        Vector2 offsetMin, Vector2 offsetMax)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var r = go.AddComponent<RectTransform>();
        r.anchorMin = anchorMin;
        r.anchorMax = anchorMax;
        r.pivot = pivot;
        r.offsetMin = offsetMin;
        r.offsetMax = offsetMax;
        return go;
    }

    private static Text MakeText(Transform parent, int size, FontStyle style,
        TextAnchor anchor, Vector2 padding)
    {
        var go = new GameObject("Text");
        go.transform.SetParent(parent, false);
        var r = go.AddComponent<RectTransform>();
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = new Vector2(padding.x, 0f);
        r.offsetMax = new Vector2(-padding.x, 0f);
        var t = go.AddComponent<Text>();
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = size;
        t.fontStyle = style;
        t.alignment = anchor;
        t.color = Color.white;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        return t;
    }
}
