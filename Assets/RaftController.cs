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

    private PlayerDogController playerController;
    private CharacterController playerCharacterController;
    private Transform playerTransform;

    private bool playerAboard = false;

    // Prompt UI
    private Text promptText;

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
    }

    private void Update()
    {
        if (playerTransform == null) return;

        float dist = Vector3.Distance(transform.position, playerTransform.position);
        bool inRange = dist <= boardRange;

        if (!playerAboard)
        {
            promptText.gameObject.SetActive(inRange);

            if (inRange && GetInteractPressed())
                Board();
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

        // Forward/back
        transform.Translate(Vector3.forward * input.y * raftSpeed * Time.deltaTime, Space.Self);

        // Turn left/right
        transform.Rotate(Vector3.up, input.x * raftTurnSpeed * Time.deltaTime, Space.Self);
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
}
