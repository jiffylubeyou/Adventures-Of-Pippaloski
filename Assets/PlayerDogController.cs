using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(CharacterController))]
public class PlayerDogController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float sprintSpeed = 11f;
    [SerializeField] private float turnSpeed = 12f;
    [SerializeField] private float gravity = -18f;
    [SerializeField] private float jumpForce = 14f;

    [Header("Camera")]
    [SerializeField] private Transform cameraTarget;   // assign the camera rig pivot here
    [SerializeField] private float camDistance = 14f;
    [SerializeField] private float camHeight = 2f;
    [SerializeField] private float camSmoothing = 6f;
    [SerializeField] private float mouseSensitivity = 0.12f;

    [Header("Drowning")]
    [SerializeField] private float drownY = -2f;
    [SerializeField] private float safePositionMemory = 2.5f; // seconds of grounded history to keep

    // Powerup flags — enable these when the player earns the ability
    [Header("Powerups (unlock at runtime)")]
    public bool hasDoubleJump = false;
    public bool hasGlide = false;
    public bool hasFly = false;

    [Header("Powerup Tuning")]
    [SerializeField] private float glideGravity = -2f;
    [SerializeField] private float flySpeed = 7f;

    private CharacterController controller;
    private float verticalVelocity;
    private bool usedDoubleJump;
    private bool isGliding;
    private bool isFlying;

    // Camera state
    private float camYaw;
    private float camPitch = 15f;
    private Vector3 camVelocity;

    private Camera mainCam;
    private Vector3 spawnPoint;
    private Text drownText;

    // Rolling buffer of grounded positions recorded over the last safePositionMemory seconds.
    // Each entry is (worldPosition, timestamp). We keep only the tail we need.
    private readonly Queue<(Vector3 pos, float time)> groundedHistory = new Queue<(Vector3, float)>();
    private float lastGroundedRecordTime = -1f;
    private const float GroundedRecordInterval = 0.1f; // record a point every 100 ms

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        mainCam = Camera.main;
        camYaw = transform.eulerAngles.y;
        spawnPoint = transform.position;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        drownText = CreateDrownUI();
    }

    private void Update()
    {
        if (transform.position.y < drownY)
        {
            Respawn();
            return;
        }

        bool grounded = controller.isGrounded;

        if (grounded)
        {
            usedDoubleJump = false;
            isGliding = false;
            isFlying = false;
            if (verticalVelocity < 0f)
                verticalVelocity = -2f;

            RecordGroundedPosition();
        }

        RotateCamera();

        var rawInput = GetMovementInput();

        // Build camera-relative flat move direction
        var camForward = Vector3.ProjectOnPlane(mainCam.transform.forward, Vector3.up).normalized;
        var camRight   = Vector3.ProjectOnPlane(mainCam.transform.right,   Vector3.up).normalized;
        var moveDir    = (camForward * rawInput.y + camRight * rawInput.x);

        if (moveDir.sqrMagnitude > 0.001f)
        {
            var targetRot = Quaternion.LookRotation(moveDir, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, turnSpeed * Time.deltaTime);
        }

        HandleVertical(grounded, moveDir);

        float currentSpeed = isFlying ? flySpeed : (GetSprintHeld() ? sprintSpeed : moveSpeed);
        var motion = moveDir * currentSpeed;
        motion.y = verticalVelocity;
        controller.Move(motion * Time.deltaTime);
    }

    private void LateUpdate()
    {
        PositionCamera();
    }

    private void HandleVertical(bool grounded, Vector3 moveDir)
    {
        bool jumpPressed = GetJumpPressed();

        if (hasFly && GetFlyHeld())
        {
            isFlying = true;
            isGliding = false;
            verticalVelocity = flySpeed;
            return;
        }

        isFlying = false;

        if (jumpPressed)
        {
            if (grounded)
            {
                verticalVelocity = jumpForce;
            }
            else if (hasDoubleJump && !usedDoubleJump)
            {
                verticalVelocity = jumpForce;
                usedDoubleJump = true;
                isGliding = false;
            }
        }

        // Glide: hold jump while airborne and falling
        if (hasGlide && !grounded && GetJumpHeld() && verticalVelocity < 0f)
            isGliding = true;
        else if (!GetJumpHeld())
            isGliding = false;

        float activeGravity = isGliding ? glideGravity : gravity;
        verticalVelocity += activeGravity * Time.deltaTime;
    }

    private void RotateCamera()
    {
        var mouse = GetMouseDelta();
        camYaw   += mouse.x * mouseSensitivity;
        camPitch -= mouse.y * mouseSensitivity;
        camPitch  = Mathf.Clamp(camPitch, -10f, 60f);
    }

    private void PositionCamera()
    {
        var pivot = transform.position + Vector3.up * camHeight;
        var rot   = Quaternion.Euler(camPitch, camYaw, 0f);
        var desiredPos = pivot - rot * Vector3.forward * camDistance;

        // Simple obstacle pull-in: spherecast back to avoid clipping
        if (Physics.SphereCast(pivot, 0.2f, desiredPos - pivot, out var hit,
                camDistance, ~LayerMask.GetMask("Player"), QueryTriggerInteraction.Ignore))
        {
            desiredPos = pivot + (desiredPos - pivot).normalized * (hit.distance - 0.1f);
        }

        mainCam.transform.position = Vector3.SmoothDamp(mainCam.transform.position, desiredPos, ref camVelocity, 1f / camSmoothing);
        mainCam.transform.LookAt(pivot);
    }

    // ---------- death UI ----------

    private static Text CreateDrownUI()
    {
        var canvas = new GameObject("Drown UI Canvas").AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        canvas.gameObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvas.gameObject.AddComponent<GraphicRaycaster>();

        var textObj = new GameObject("Drown Text");
        textObj.transform.SetParent(canvas.transform, false);

        var rect = textObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(600f, 120f);
        rect.anchoredPosition = new Vector2(0f, 120f);

        var text = textObj.AddComponent<Text>();
        text.text = "you died!";
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 32;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = new Color(1f, 1f, 1f, 1f);

        // Dark drop shadow
        var shadow = textObj.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.7f);
        shadow.effectDistance = new Vector2(3f, -3f);

        textObj.SetActive(false);
        return text;
    }

    private void ShowDrownMessage()
    {
        StopCoroutine(nameof(HideDrownMessage));
        drownText.gameObject.SetActive(true);
        StartCoroutine(nameof(HideDrownMessage));
    }

    private IEnumerator HideDrownMessage()
    {
        yield return new WaitForSeconds(2f);
        drownText.gameObject.SetActive(false);
    }

    // ---------- respawn / position history ----------

    private void RecordGroundedPosition()
    {
        float now = Time.time;
        if (now - lastGroundedRecordTime < GroundedRecordInterval)
            return;

        lastGroundedRecordTime = now;
        groundedHistory.Enqueue((transform.position, now));

        // Prune entries older than the memory window
        float cutoff = now - safePositionMemory;
        while (groundedHistory.Count > 1 && groundedHistory.Peek().time < cutoff)
            groundedHistory.Dequeue();
    }

    private void Respawn()
    {
        // Use the oldest entry in the history buffer (furthest back in time within the window).
        // This is the position the dog was at ~safePositionMemory seconds ago, on solid ground.
        Vector3 target = groundedHistory.Count > 0 ? groundedHistory.Peek().pos : spawnPoint;

        controller.enabled = false;
        transform.position = target;
        controller.enabled = true;
        verticalVelocity = 0f;

        ShowDrownMessage();
    }

    // ---------- input helpers ----------

    private static Vector2 GetMouseDelta()
    {
#if ENABLE_INPUT_SYSTEM
        var mouse = Mouse.current;
        return mouse != null ? mouse.delta.ReadValue() : Vector2.zero;
#elif ENABLE_LEGACY_INPUT_MANAGER
        return new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
#else
        return Vector2.zero;
#endif
    }

    private static Vector2 GetMovementInput()
    {
#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
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

    private static bool GetJumpPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetButtonDown("Jump");
#else
        return false;
#endif
    }

    private static bool GetJumpHeld()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.spaceKey.isPressed;
#elif ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetButton("Jump");
#else
        return false;
#endif
    }

    private static bool GetSprintHeld()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed;
#elif ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKey(KeyCode.LeftShift);
#else
        return false;
#endif
    }

    private static bool GetFlyHeld()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.spaceKey.isPressed;
#elif ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetButton("Jump");
#else
        return false;
#endif
    }
}
