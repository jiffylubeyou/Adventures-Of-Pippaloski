using UnityEngine;
using UnityEngine.UI;

// Drop this on any persistent GameObject (or let it self-create).
// It draws a coin icon + count in the top-right corner at all times.
public class CoinHUD : MonoBehaviour
{
    private Text  countText;
    private int   lastCount = -1;

    private void Awake()
    {
        BuildUI();
    }

    private void Update()
    {
        if (GameState.Coins != lastCount)
        {
            lastCount       = GameState.Coins;
            countText.text  = "⬤  " + GameState.Coins;   // gold circle + number
        }
    }

    private void BuildUI()
    {
        var canvas = new GameObject("Coin HUD Canvas").AddComponent<Canvas>();
        canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        canvas.gameObject.AddComponent<CanvasScaler>().uiScaleMode =
            CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvas.gameObject.AddComponent<GraphicRaycaster>();

        // Background pill
        var bg = new GameObject("Coin BG");
        bg.transform.SetParent(canvas.transform, false);
        var bgRect = bg.AddComponent<RectTransform>();
        bgRect.anchorMin        = new Vector2(1f, 1f);
        bgRect.anchorMax        = new Vector2(1f, 1f);
        bgRect.pivot            = new Vector2(1f, 1f);
        bgRect.sizeDelta        = new Vector2(120f, 40f);
        bgRect.anchoredPosition = new Vector2(-16f, -16f);
        var bgImg  = bg.AddComponent<Image>();
        bgImg.color = new Color(0f, 0f, 0f, 0.5f);

        // Coin icon (gold circle)
        var icon = new GameObject("Coin Icon");
        icon.transform.SetParent(bg.transform, false);
        var iconRect = icon.AddComponent<RectTransform>();
        iconRect.anchorMin        = new Vector2(0f, 0.5f);
        iconRect.anchorMax        = new Vector2(0f, 0.5f);
        iconRect.pivot            = new Vector2(0f, 0.5f);
        iconRect.sizeDelta        = new Vector2(26f, 26f);
        iconRect.anchoredPosition = new Vector2(8f, 0f);
        var iconImg   = icon.AddComponent<Image>();
        iconImg.color = new Color(1f, 0.78f, 0.1f);

        // Count text
        var textObj = new GameObject("Coin Count");
        textObj.transform.SetParent(bg.transform, false);
        var textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin  = new Vector2(0f, 0f);
        textRect.anchorMax  = new Vector2(1f, 1f);
        textRect.offsetMin  = new Vector2(40f, 0f);
        textRect.offsetMax  = new Vector2(-8f, 0f);
        countText           = textObj.AddComponent<Text>();
        countText.text      = "0";
        countText.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        countText.fontSize  = 22;
        countText.fontStyle = FontStyle.Bold;
        countText.alignment = TextAnchor.MiddleLeft;
        countText.color     = Color.white;

        var shadow = textObj.AddComponent<Shadow>();
        shadow.effectColor    = new Color(0f, 0f, 0f, 0.7f);
        shadow.effectDistance = new Vector2(1f, -1f);

        DontDestroyOnLoad(canvas.gameObject);
    }
}
