using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class RaftController : MonoBehaviour
{
    [Header("Boarding")]
    [SerializeField] private float boardRange = 3f;
    [SerializeField] private Vector3 seatOffset = new Vector3(0f, 1.2f, 0f);
    [SerializeField] private float raftCamDistance = 22f;
    [SerializeField] private float raftCamHeight   = 6f;

    [Header("Raft Movement")]
    [SerializeField] private float raftSpeed     = 5f;
    [SerializeField] private float raftTurnSpeed = 90f;  // degrees per second

    [Header("Water")]
    [Tooltip("World Y the raft floats at. Match your water plane height.")]
    [SerializeField] private float waterLevel = 0f;

    [Header("Land Collision")]
    [Tooltip("Half-extents of the collision box swept ahead of the raft. X = half-width, Y = half-height (make tall to catch any island height), Z = half depth of the probe box.")]
    [SerializeField] private Vector3 hullHalfExtents = new Vector3(1.5f, 8f, 0.6f);
    [Tooltip("How far ahead of the raft front edge to start the collision check.")]
    [SerializeField] private float probeDistance = 0.5f;
    [Tooltip("Layer mask containing the island / terrain. Set this to the 'Island' layer after running Tools > Pippaloski > Setup Island Layer.")]
    [SerializeField] private LayerMask groundLayer = ~0;  // default: everything

    private PlayerDogController playerController;
    private CharacterController playerCharacterController;
    private Transform playerTransform;

    private bool playerAboard = false;

    // Prompt UI
    private Text promptText;
    private Text noKeysText;

    // Queried by PirateShipAI / RaftHealth
    public static RaftController Instance { get; private set; }
    public bool PlayerAboard => playerAboard;
    public float WaterLevel  => waterLevel;

    // Set by RaftHealth while the raft is sinking — freezes driving and the
    // water-surface snap so the sink animation can move the raft freely.
    [HideInInspector] public bool ControlsLocked = false;

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        var playerObj = GameObject.Find("Pippaloski");
        if (playerObj != null)
        {
            playerTransform          = playerObj.transform;
            playerController         = playerObj.GetComponent<PlayerDogController>();
            playerCharacterController = playerObj.GetComponent<CharacterController>();
        }

        promptText = CreatePromptUI();
        noKeysText = CreateNoKeysUI();

    }

    private void Update()
    {
        if (playerTransform == null) return;

        if (ControlsLocked)
        {
            promptText.gameObject.SetActive(false);
            return;
        }

        float dist = Vector3.Distance(transform.position, playerTransform.position);
        bool inRange = dist <= boardRange;

        if (!playerAboard)
        {
            promptText.gameObject.SetActive(inRange);

            if (inRange && GetInteractPressed())
            {
                if (GameState.HasFlag(GameState.QuestComplete))
                    Board();
                else
                    StartCoroutine(ShowNoKeysMessage());
            }
        }
        else
        {
            promptText.gameObject.SetActive(false);

            if (GetInteractPressed())
            {
                Disembark();
                return;
            }

            DriveRaft();
        }

        // Keep raft on water surface
        var pos = transform.position;
        pos.y = waterLevel;
        transform.position = pos;
    }

    private void DriveRaft()
    {
        var input = GetMovementInput();

        // Turning is always allowed
        transform.Rotate(Vector3.up, input.x * raftTurnSpeed * Time.deltaTime, Space.Self);

        if (Mathf.Abs(input.y) > 0.01f)
        {
            float   stepDist  = raftSpeed * Time.deltaTime;
            Vector3 moveDir   = transform.forward * Mathf.Sign(input.y);

            if (!IsLandAhead(moveDir, stepDist))
                transform.Translate(Vector3.forward * input.y * stepDist, Space.Self);
        }
    }

    // Sweeps a tall box forward in the movement direction.
    // The box is centred on the raft, pushed ahead by the hull depth + probeDistance,
    // and is tall enough to catch island geometry at ANY world height, so we don't
    // need to know or guess the island's exact Y position.
    private bool IsLandAhead(Vector3 moveDir, float stepDist)
    {
        // Centre the box just ahead of the raft's front face
        float   reach     = hullHalfExtents.z + probeDistance + stepDist;
        Vector3 boxCenter = transform.position + moveDir * reach;
        // Keep the box centred on the raft's Y (waterLevel), but hullHalfExtents.y is
        // set tall (default 8) so it spans well above and below to catch any island height.

        bool hit = Physics.CheckBox(
            boxCenter,
            hullHalfExtents,            // half-extents of the probe box
            transform.rotation,         // aligned with raft facing
            groundLayer,
            QueryTriggerInteraction.Ignore);

        return hit;
    }

    private void Board()
    {
        playerAboard = true;

        playerController.MotorEnabled       = false;
        playerController.SuppressDrown      = true;
        playerController.CamDistanceOverride = raftCamDistance;
        playerController.CamHeightOverride   = raftCamHeight;

        // Disable CharacterController so it doesn't fight the parenting
        playerCharacterController.enabled = false;

        playerTransform.SetParent(transform);
        playerTransform.localPosition = seatOffset;
        playerTransform.localRotation = Quaternion.identity;
    }

    private void Disembark()
    {
        playerAboard = false;

        playerTransform.SetParent(null);

        // Put player just off the side of the raft so they don't immediately re-trigger boarding
        playerTransform.position = transform.position + transform.right * 2f + Vector3.up * 0.5f;

        playerCharacterController.enabled = true;

        playerController.MotorEnabled       = true;
        playerController.SuppressDrown      = false;
        playerController.CamDistanceOverride = 0f;
        playerController.CamHeightOverride   = 0f;
    }

    // ---------- input ----------

    private static Vector2 GetMovementInput()
    {
#if ENABLE_INPUT_SYSTEM
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb == null) return Vector2.zero;
        float x = (kb.dKey.isPressed || kb.rightArrowKey.isPressed ? 1f : 0f)
                - (kb.aKey.isPressed || kb.leftArrowKey.isPressed  ? 1f : 0f);
        float z = (kb.wKey.isPressed || kb.upArrowKey.isPressed    ? 1f : 0f)
                - (kb.sKey.isPressed || kb.downArrowKey.isPressed  ? 1f : 0f);
        return Vector2.ClampMagnitude(new Vector2(x, z), 1f);
#elif ENABLE_LEGACY_INPUT_MANAGER
        return Vector2.ClampMagnitude(new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")), 1f);
#else
        return Vector2.zero;
#endif
    }

    private static bool GetInteractPressed()
    {
#if ENABLE_INPUT_SYSTEM
        var kb = UnityEngine.InputSystem.Keyboard.current;
        return kb != null && kb.eKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.E);
#else
        return false;
#endif
    }

    // ---------- debug ----------

    private void OnDrawGizmos()
    {
        float   reach     = hullHalfExtents.z + probeDistance + 0.1f;
        Vector3 boxCenter = transform.position + transform.forward * reach;

        bool hit = Physics.CheckBox(boxCenter, hullHalfExtents, transform.rotation,
            groundLayer, QueryTriggerInteraction.Ignore);

        // Green = clear, red = land detected
        Gizmos.color = hit ? new Color(1f, 0.1f, 0.1f, 0.45f) : new Color(0.1f, 1f, 0.1f, 0.35f);
        Gizmos.matrix = Matrix4x4.TRS(boxCenter, transform.rotation, Vector3.one);
        Gizmos.DrawCube(Vector3.zero, hullHalfExtents * 2f);
        Gizmos.matrix = Matrix4x4.identity;

        // Outline
        Gizmos.color = hit ? Color.red : Color.green;
        Gizmos.matrix = Matrix4x4.TRS(boxCenter, transform.rotation, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, hullHalfExtents * 2f);
        Gizmos.matrix = Matrix4x4.identity;

        // Water level dot
        Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.8f);
        Gizmos.DrawSphere(new Vector3(transform.position.x, waterLevel, transform.position.z), 0.4f);
    }

    // ---------- no-keys flash ----------

    private IEnumerator ShowNoKeysMessage()
    {
        noKeysText.gameObject.SetActive(true);
        yield return new WaitForSeconds(2.5f);
        noKeysText.gameObject.SetActive(false);
    }

    // ---------- UI ----------

    private static Text CreatePromptUI()
    {
        var canvas = new GameObject("Raft Prompt Canvas").AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        canvas.gameObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvas.gameObject.AddComponent<GraphicRaycaster>();

        var textObj = new GameObject("Raft Prompt Text");
        textObj.transform.SetParent(canvas.transform, false);

        var rect = textObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot     = new Vector2(0.5f, 0f);
        rect.sizeDelta = new Vector2(500f, 80f);
        rect.anchoredPosition = new Vector2(0f, 60f);

        var text = textObj.AddComponent<Text>();
        text.text      = "Press E to ride the raft";
        text.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize  = 28;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color     = Color.white;

        var shadow = textObj.AddComponent<Shadow>();
        shadow.effectColor    = new Color(0f, 0f, 0f, 0.8f);
        shadow.effectDistance = new Vector2(2f, -2f);

        textObj.SetActive(false);
        return text;
    }

    private static Text CreateNoKeysUI()
    {
        var canvas = new GameObject("Raft No Keys Canvas").AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 11;
        canvas.gameObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvas.gameObject.AddComponent<GraphicRaycaster>();

        var textObj = new GameObject("No Keys Text");
        textObj.transform.SetParent(canvas.transform, false);

        var rect = textObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot     = new Vector2(0.5f, 0f);
        rect.sizeDelta = new Vector2(500f, 80f);
        // Sits above the "Press E" prompt (prompt is at y=60, this is at y=150)
        rect.anchoredPosition = new Vector2(0f, 150f);

        var text = textObj.AddComponent<Text>();
        text.text      = "You don't have the keys!";
        text.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize  = 28;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color     = new Color(1f, 0.35f, 0.2f);

        var shadow = textObj.AddComponent<Shadow>();
        shadow.effectColor    = new Color(0f, 0f, 0f, 0.8f);
        shadow.effectDistance = new Vector2(2f, -2f);

        textObj.SetActive(false);
        return text;
    }
}
