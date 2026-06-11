using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Health for the player's raft. Lives on the same GameObject as RaftController.
///
/// - A health bar appears at the bottom of the screen while Pippaloski is aboard.
/// - Cannonball hits call ApplyDamage(). Damage only counts while aboard.
/// - At zero health the raft sinks, then the raft (player still aboard) is
///   teleported back to where it was ~rewindSeconds ago and health refills.
/// </summary>
[RequireComponent(typeof(RaftController))]
public class RaftHealth : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;

    [Header("Sinking / Respawn")]
    [Tooltip("How far back in time the raft is rewound after sinking.")]
    [SerializeField] private float rewindSeconds = 10f;
    [SerializeField] private float sinkDuration  = 3f;
    [SerializeField] private float sinkDepth     = 3f;

    private RaftController controller;
    private float health;
    private bool  sinking;

    // Rolling buffer of (position, rotation, timestamp), recorded every
    // RecordInterval seconds, kept slightly longer than rewindSeconds.
    private readonly Queue<(Vector3 pos, Quaternion rot, float time)> history
        = new Queue<(Vector3, Quaternion, float)>();
    private float lastRecordTime = -1f;
    private const float RecordInterval = 0.5f;

    // UI
    private GameObject barRoot;
    private RectTransform fillRect;
    private Image fillImage;
    private Image damageFlash;
    private Text  sankText;
    private const float BarWidth = 320f;

    public bool IsSinking => sinking;

    private void Awake()
    {
        controller = GetComponent<RaftController>();
        health = maxHealth;
        BuildUI();
    }

    private void Update()
    {
        if (!sinking)
            RecordPosition();

        barRoot.SetActive(controller.PlayerAboard || sinking);
    }

    public void ApplyDamage(float amount)
    {
        // Pirates only threaten the player while they're sailing
        if (sinking || !controller.PlayerAboard) return;

        health = Mathf.Max(0f, health - amount);
        UpdateBar();
        StopCoroutine(nameof(FlashDamage));
        StartCoroutine(nameof(FlashDamage));

        if (health <= 0f)
            StartCoroutine(SinkAndRespawn());
    }

    // ---------- position history ----------

    private void RecordPosition()
    {
        float now = Time.time;
        if (now - lastRecordTime < RecordInterval) return;

        lastRecordTime = now;
        history.Enqueue((transform.position, transform.rotation, now));

        // Keep a little more than the rewind window
        float cutoff = now - (rewindSeconds + 2f);
        while (history.Count > 1 && history.Peek().time < cutoff)
            history.Dequeue();
    }

    // ---------- sinking ----------

    private IEnumerator SinkAndRespawn()
    {
        sinking = true;
        controller.ControlsLocked = true;

        sankText.gameObject.SetActive(true);

        // Sink: drift down and roll over
        float elapsed = 0f;
        Quaternion startRot = transform.rotation;
        Quaternion sunkRot  = startRot * Quaternion.Euler(0f, 0f, 30f);
        Vector3    startPos = transform.position;

        while (elapsed < sinkDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / sinkDuration;
            transform.position = startPos + Vector3.down * (sinkDepth * t);
            transform.rotation = Quaternion.Slerp(startRot, sunkRot, t);
            yield return null;
        }

        // Rewind: oldest entry in the buffer is ~rewindSeconds in the past
        Vector3    targetPos = startPos;
        Quaternion targetRot = startRot;
        if (history.Count > 0)
        {
            var oldest = history.Peek();
            targetPos = oldest.pos;
            targetRot = oldest.rot;
        }
        history.Clear();

        targetPos.y = controller.WaterLevel;
        transform.SetPositionAndRotation(targetPos, targetRot);

        health = maxHealth;
        UpdateBar();

        controller.ControlsLocked = false;
        sinking = false;

        yield return new WaitForSeconds(2f);
        sankText.gameObject.SetActive(false);
    }

    private IEnumerator FlashDamage()
    {
        damageFlash.color = new Color(1f, 0f, 0f, 0.25f);
        float t = 0f;
        while (t < 0.4f)
        {
            t += Time.deltaTime;
            damageFlash.color = new Color(1f, 0f, 0f, Mathf.Lerp(0.25f, 0f, t / 0.4f));
            yield return null;
        }
        damageFlash.color = Color.clear;
    }

    // ---------- UI ----------

    private void UpdateBar()
    {
        float pct = health / maxHealth;
        fillRect.sizeDelta = new Vector2(BarWidth * pct, fillRect.sizeDelta.y);
        fillImage.color = Color.Lerp(new Color(0.85f, 0.2f, 0.15f), new Color(0.25f, 0.8f, 0.3f), pct);
    }

    private void BuildUI()
    {
        var canvas = new GameObject("Raft Health Canvas").AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 12;
        canvas.gameObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvas.gameObject.AddComponent<GraphicRaycaster>();

        // Full-screen red flash for damage feedback
        var flashObj = new GameObject("Damage Flash");
        flashObj.transform.SetParent(canvas.transform, false);
        var flashRect = flashObj.AddComponent<RectTransform>();
        flashRect.anchorMin = Vector2.zero;
        flashRect.anchorMax = Vector2.one;
        flashRect.sizeDelta = Vector2.zero;
        damageFlash = flashObj.AddComponent<Image>();
        damageFlash.color = Color.clear;
        damageFlash.raycastTarget = false;

        // Bar container — top-left corner
        barRoot = new GameObject("Raft Health Bar");
        barRoot.transform.SetParent(canvas.transform, false);
        var rootRect = barRoot.AddComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0f, 1f);
        rootRect.anchorMax = new Vector2(0f, 1f);
        rootRect.pivot     = new Vector2(0f, 1f);
        rootRect.sizeDelta = new Vector2(BarWidth + 8f, 30f);
        rootRect.anchoredPosition = new Vector2(20f, -20f);

        // Background / border
        var bg = barRoot.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.6f);

        // Fill — anchored to the left edge so width = health
        var fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(barRoot.transform, false);
        fillRect = fillObj.AddComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0f, 0.5f);
        fillRect.anchorMax = new Vector2(0f, 0.5f);
        fillRect.pivot     = new Vector2(0f, 0.5f);
        fillRect.sizeDelta = new Vector2(BarWidth, 22f);
        fillRect.anchoredPosition = new Vector2(4f, 0f);
        fillImage = fillObj.AddComponent<Image>();

        // "your raft sank!" message — same style as the drown message
        var textObj = new GameObject("Raft Sank Text");
        textObj.transform.SetParent(canvas.transform, false);
        var textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.sizeDelta = new Vector2(600f, 120f);
        textRect.anchoredPosition = new Vector2(0f, 120f);
        sankText = textObj.AddComponent<Text>();
        sankText.text      = "your raft sank!";
        sankText.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        sankText.fontSize  = 32;
        sankText.fontStyle = FontStyle.Bold;
        sankText.alignment = TextAnchor.MiddleCenter;
        sankText.color     = Color.white;
        var shadow = textObj.AddComponent<Shadow>();
        shadow.effectColor    = new Color(0f, 0f, 0f, 0.7f);
        shadow.effectDistance = new Vector2(3f, -3f);
        textObj.SetActive(false);

        barRoot.SetActive(false);
        UpdateBar();
    }
}
