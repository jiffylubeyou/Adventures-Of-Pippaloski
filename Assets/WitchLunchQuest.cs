using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Attach to the Witch GameObject alongside DialogueTrigger.
/// When StartQuest() is called (triggered by the "startsWitchLunchQuest" dialogue flag):
///   - A countdown timer starts and is shown on screen.
///   - The burger spawns at burgerSpawnPoint (or 8 units in front of the witch).
///   - When the player returns to the witch with the burger and presses E, they earn
///     (secondsRemaining + 5) coins.
///   - If time runs out the burger despawns and the quest fails.
/// </summary>
public class WitchLunchQuest : MonoBehaviour
{
    [Header("Quest Settings")]
    [Tooltip("How many seconds the player has to retrieve the burger.")]
    [SerializeField] private float questDuration = 60f;

    [Tooltip("Where the burger spawns. Leave empty to auto-place in front of the witch.")]
    [SerializeField] private Transform burgerSpawnPoint;

    [Tooltip("Optional: assign a burger prefab. Leave empty to use the auto-generated one.")]
    [SerializeField] private GameObject burgerPrefab;

    [Tooltip("How close the player must be (and press E) to deliver the burger. " +
             "Should be >= the witch's talkRadius (default 4) so the prompt appears in time.")]
    [SerializeField] private float deliveryRadius = 5f;

    // ── state ─────────────────────────────────────────────────────────────────
    private bool        questActive    = false;
    private float       timeLeft       = 0f;
    private GameObject  spawnedBurger  = null;
    private Transform   playerTransform;

    // ── sibling component (normal witch dialogue) ─────────────────────────────
    private DialogueTrigger dialogueTrigger;

    // ── HUD ───────────────────────────────────────────────────────────────────
    private GameObject hudRoot;
    private Text        timerLabel;
    private Text        statusLabel;

    // ─────────────────────────────────────────────────────────────────────────

    private void Start()
    {
        var playerObj = GameObject.FindGameObjectWithTag("Player")
                     ?? GameObject.Find("Pippaloski")
                     ?? GameObject.Find("Player Border Collie");
        if (playerObj != null) playerTransform = playerObj.transform;

        dialogueTrigger = GetComponent<DialogueTrigger>();

        BuildHUD();
        hudRoot.SetActive(false);
    }

    private void Update()
    {
        bool hasBurger = GameState.HasFlag("has_burger");

        // ── Delivery intercept ────────────────────────────────────────────────
        // While the player is carrying the burger, suppress the witch's normal
        // dialogue and show a custom "deliver" prompt so pressing E triggers
        // delivery instead of opening the regular witch menu.
        if (questActive && hasBurger && playerTransform != null)
        {
            float dist    = Vector3.Distance(transform.position, playerTransform.position);
            bool  inRange = dist <= deliveryRadius;

            // Disable normal DialogueTrigger so pressing E won't open regular dialogue.
            if (dialogueTrigger != null)
                dialogueTrigger.enabled = false;

            if (inRange)
            {
                DialogueUI.GetOrCreate().RequestPrompt(this, "E  deliver lunch to Witch", dist);

                if (WasTalkPressed())
                {
                    DialogueUI.GetOrCreate().ReleasePrompt(this);
                    DeliverBurger();
                    return;
                }
            }
            else
            {
                DialogueUI.GetOrCreate().ReleasePrompt(this);
            }
        }
        else
        {
            // Not in delivery mode — make sure normal dialogue is restored.
            if (dialogueTrigger != null && !dialogueTrigger.enabled)
                dialogueTrigger.enabled = true;

            DialogueUI.GetOrCreate().ReleasePrompt(this);
        }

        // ── Timer tick ────────────────────────────────────────────────────────
        if (!questActive) return;

        timeLeft -= Time.deltaTime;

        int secs = Mathf.CeilToInt(Mathf.Max(timeLeft, 0f));
        timerLabel.text  = secs + "s";
        timerLabel.color = secs <= 10 ? new Color(1f, 0.3f, 0.2f) : Color.white;

        statusLabel.text = hasBurger ? "Return to the Witch!" : "Retrieve the burger!";

        if (timeLeft <= 0f)
            FailQuest();
    }

    // ── Called by DialogueTrigger when "startsWitchLunchQuest" line is chosen ──

    public void StartQuest()
    {
        if (questActive) return;

        questActive = true;
        timeLeft    = questDuration;

        GameState.ClearFlag("has_burger");
        SpawnBurger();

        statusLabel.text = "Retrieve the burger!";
        hudRoot.SetActive(true);

        Debug.Log("[WitchLunchQuest] Quest started! Timer: " + questDuration + "s");
    }

    // ─────────────────────────────────────────────────────────────────────────

    private void DeliverBurger()
    {
        questActive = false;
        int reward  = Mathf.CeilToInt(timeLeft) + 5;

        GameState.ClearFlag("has_burger");
        HideHUD();

        // Re-enable normal dialogue after the delivery conversation ends.
        if (dialogueTrigger != null)
            dialogueTrigger.enabled = true;

        var ui = DialogueUI.GetOrCreate();
        var rewardLine = new DialogueLine
        {
            playerPrompt   = "Here is your lunch!",
            npcResponse    = "Ohoho, magnificent! And with " + Mathf.CeilToInt(timeLeft) +
                             " seconds to spare. Here are your " + reward + " coins, as promised!",
            closesDialogue = true
        };

        ui.OpenDialogue("Witch", "You're back already!", new[] { rewardLine }, _ =>
        {
            GameState.AddCoins(reward);
            ui.CloseDialogue();
            Debug.Log("[WitchLunchQuest] Delivered! Rewarded " + reward + " coins.");
        });
    }

    private void FailQuest()
    {
        questActive = false;
        GameState.ClearFlag("has_burger");

        // Re-enable normal dialogue.
        if (dialogueTrigger != null)
            dialogueTrigger.enabled = true;

        DialogueUI.GetOrCreate().ReleasePrompt(this);

        if (spawnedBurger != null) Destroy(spawnedBurger);

        timerLabel.text  = "0s";
        statusLabel.text = "Too slow! Come back later.";

        Debug.Log("[WitchLunchQuest] Failed — time ran out.");
        Invoke(nameof(HideHUD), 3f);
    }

    private void HideHUD() => hudRoot.SetActive(false);

    // ── Burger spawning ───────────────────────────────────────────────────────

    private void SpawnBurger()
    {
        Vector3 pos = burgerSpawnPoint != null
            ? burgerSpawnPoint.position
            : transform.position + transform.forward * 8f;

        if (burgerPrefab != null)
            spawnedBurger = Instantiate(burgerPrefab, pos, Quaternion.identity);
        else
            spawnedBurger = BuildGeneratedBurger(pos);

        // Always add a known-good trigger collider on the root so BurgerPickup
        // can reliably detect the player regardless of what the prefab contains.
        var trigger       = spawnedBurger.AddComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.radius    = 1f;

        // CharacterController fires OnTriggerEnter reliably when the trigger
        // object also has a kinematic Rigidbody.
        if (spawnedBurger.GetComponent<Rigidbody>() == null)
        {
            var rb         = spawnedBurger.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity  = false;
        }

        if (spawnedBurger.GetComponent<BurgerPickup>() == null)
            spawnedBurger.AddComponent<BurgerPickup>();
    }

    private static GameObject BuildGeneratedBurger(Vector3 position)
    {
        var root = new GameObject("Burger");
        root.transform.position = position;

        AddLayer(root.transform, "BottomBun", new Color(0.87f, 0.60f, 0.20f), new Vector3(0f, 0.10f, 0f), new Vector3(0.9f, 0.18f, 0.9f));
        AddLayer(root.transform, "Lettuce",   new Color(0.28f, 0.68f, 0.25f), new Vector3(0f, 0.26f, 0f), new Vector3(1.0f, 0.06f, 1.0f));
        AddLayer(root.transform, "Patty",     new Color(0.40f, 0.22f, 0.08f), new Vector3(0f, 0.34f, 0f), new Vector3(0.85f, 0.14f, 0.85f));
        AddLayer(root.transform, "Cheese",    new Color(1.00f, 0.80f, 0.10f), new Vector3(0f, 0.42f, 0f), new Vector3(0.90f, 0.05f, 0.90f));
        AddLayer(root.transform, "TopBun",    new Color(0.87f, 0.60f, 0.20f), new Vector3(0f, 0.60f, 0f), new Vector3(0.85f, 0.30f, 0.85f));

        root.AddComponent<BurgerBobber>();
        return root;
    }

    private static void AddLayer(Transform parent, string layerName, Color color,
                                 Vector3 localPos, Vector3 localScale)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = layerName;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale    = localScale;
        Destroy(go.GetComponent<Collider>());

        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.SetColor("_BaseColor", color);
        go.GetComponent<Renderer>().material = mat;
    }

    // ── HUD ───────────────────────────────────────────────────────────────────

    private void BuildHUD()
    {
        var canvasGO = new GameObject("Witch Quest HUD");
        DontDestroyOnLoad(canvasGO);

        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 25;
        canvasGO.AddComponent<CanvasScaler>().uiScaleMode =
            CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGO.AddComponent<GraphicRaycaster>();

        hudRoot = new GameObject("Root");
        hudRoot.transform.SetParent(canvasGO.transform, false);

        var r = hudRoot.AddComponent<RectTransform>();
        r.anchorMin        = new Vector2(0.5f, 1f);
        r.anchorMax        = new Vector2(0.5f, 1f);
        r.pivot            = new Vector2(0.5f, 1f);
        r.sizeDelta        = new Vector2(300f, 68f);
        r.anchoredPosition = new Vector2(0f, -20f);

        hudRoot.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.65f);

        // Status line (top half)
        var sGO = new GameObject("Status");
        sGO.transform.SetParent(hudRoot.transform, false);
        var sR = sGO.AddComponent<RectTransform>();
        sR.anchorMin = new Vector2(0f, 0.5f); sR.anchorMax = new Vector2(1f, 1f);
        sR.offsetMin = new Vector2(8f, 0f);   sR.offsetMax = new Vector2(-8f, 0f);
        statusLabel = sGO.AddComponent<Text>();
        statusLabel.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        statusLabel.fontSize  = 15;
        statusLabel.alignment = TextAnchor.MiddleCenter;
        statusLabel.color     = new Color(1f, 0.88f, 0.4f);

        // Timer (bottom half, big)
        var tGO = new GameObject("Timer");
        tGO.transform.SetParent(hudRoot.transform, false);
        var tR = tGO.AddComponent<RectTransform>();
        tR.anchorMin = new Vector2(0f, 0f); tR.anchorMax = new Vector2(1f, 0.5f);
        tR.offsetMin = new Vector2(8f, 0f); tR.offsetMax = new Vector2(-8f, 0f);
        timerLabel = tGO.AddComponent<Text>();
        timerLabel.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        timerLabel.fontSize  = 26;
        timerLabel.fontStyle = FontStyle.Bold;
        timerLabel.alignment = TextAnchor.MiddleCenter;
        timerLabel.color     = Color.white;
    }

    // ── Input helper ──────────────────────────────────────────────────────────

    private static bool WasTalkPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.E);
#endif
    }
}

/// <summary>Makes the burger bob up and down so it's easy to spot.</summary>
public class BurgerBobber : MonoBehaviour
{
    private Vector3 startPos;
    private void Start()  => startPos = transform.position;
    private void Update() => transform.position =
        startPos + Vector3.up * (Mathf.Sin(Time.time * 2.5f) * 0.18f);
}
