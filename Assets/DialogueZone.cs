using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Place this on any empty GameObject inside a building (or anywhere else).
/// When the player walks inside the radius, dialogue opens automatically.
///
/// Setup:
///   1. Create an empty GameObject (e.g. "Obelisk Voice").
///   2. Add this component.
///   3. Set Speaker Name, Greeting, and your Dialogue Lines in the Inspector.
///   4. Adjust Trigger Radius to cover the area you want.
///
/// The zone draws a yellow wire-sphere gizmo in the Scene view so you can
/// see exactly where it reaches.
/// </summary>
public class DialogueZone : MonoBehaviour
{
    [Header("Zone")]
    [Tooltip("Radius in world units. Player entering this sphere triggers dialogue.")]
    [SerializeField] private float triggerRadius = 5f;

    [Tooltip("If true, dialogue only triggers once ever. Uncheck to repeat each time the player re-enters.")]
    [SerializeField] private bool oneShot = false;

    [Header("Dialogue")]
    [SerializeField] private string speakerName = "Ancient Voice";

    [TextArea(2, 5)]
    [SerializeField] private string greeting = "You have entered a sacred place...";

    [SerializeField] private DialogueLine[] lines = new DialogueLine[]
    {
        new DialogueLine
        {
            playerPrompt   = "What is this place?",
            npcResponse    = "This obelisk has stood for a thousand years, watching over the land.",
        },
        new DialogueLine
        {
            playerPrompt   = "I'll be on my way.",
            npcResponse    = "May the sands guide your paws.",
            closesDialogue = true
        }
    };

    // ── state ─────────────────────────────────────────────────────────────────
    private DialogueUI  ui;
    private Transform   player;
    private bool        playerInZone   = false;
    private bool        dialogueOpen   = false;
    private bool        triggered      = false;   // for oneShot

    private readonly HashSet<int> consumedLines  = new HashSet<int>();
    private readonly HashSet<int> unlockedLines  = new HashSet<int>();

    // ─────────────────────────────────────────────────────────────────────────

    private void Start()
    {
        ui = DialogueUI.GetOrCreate();

        var playerObj = GameObject.FindGameObjectWithTag("Player")
                     ?? GameObject.Find("Pippaloski")
                     ?? GameObject.Find("Player Border Collie");
        if (playerObj != null) player = playerObj.transform;
    }

    private void Update()
    {
        if (player == null) return;

        float dist = Vector3.Distance(transform.position, player.position);
        bool  wasInZone = playerInZone;
        playerInZone = dist <= triggerRadius;

        // Player just entered the zone
        if (playerInZone && !wasInZone)
        {
            if (!oneShot || !triggered)
                OpenDialogue();
        }

        // Player left the zone while dialogue was open — close it
        if (!playerInZone && wasInZone && dialogueOpen)
            CloseDialogue();

        // Allow ESC to close
        if (dialogueOpen && WasCancelPressed())
            CloseDialogue();
    }

    // ─────────────────────────────────────────────────────────────────────────

    private void OpenDialogue()
    {
        if (dialogueOpen) return;
        triggered    = true;
        dialogueOpen = true;
        ui.OpenDialogue(speakerName, greeting, GetAvailableLines(), OnOptionChosen);
    }

    private void CloseDialogue()
    {
        dialogueOpen = false;
        consumedLines.Clear();   // reset so re-entry shows all lines again (unless oneShot)
        unlockedLines.Clear();
        ui.CloseDialogue();
    }

    private void OnOptionChosen(DialogueLine chosen)
    {
        int idx = System.Array.IndexOf(lines, chosen);
        if (chosen.oneTimeOnly && idx >= 0) consumedLines.Add(idx);

        if (chosen.unlocksLineIndices != null)
            foreach (var i in chosen.unlocksLineIndices) unlockedLines.Add(i);
        if (chosen.locksLineIndices != null)
            foreach (var i in chosen.locksLineIndices) consumedLines.Add(i);
        if (chosen.clearsFlags != null)
            foreach (var f in chosen.clearsFlags) GameState.ClearFlag(f);
        if (chosen.setsFlags != null)
            foreach (var f in chosen.setsFlags) GameState.SetFlag(f);

        if (chosen.closesDialogue)
        {
            ui.ShowResponse(speakerName, chosen.npcResponse, CloseDialogue);
        }
        else if (chosen.followUpLines != null && chosen.followUpLines.Length > 0)
        {
            ui.ShowResponse(speakerName, chosen.npcResponse, () =>
                ui.OpenDialogue(speakerName, "...", chosen.followUpLines, OnOptionChosen));
        }
        else
        {
            ui.ShowResponse(speakerName, chosen.npcResponse, () =>
                ui.OpenDialogue(speakerName, "...", GetAvailableLines(), OnOptionChosen));
        }
    }

    private DialogueLine[] GetAvailableLines()
    {
        var available = new List<DialogueLine>();
        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            if (consumedLines.Contains(i)) continue;
            if (line.startsLocked && !unlockedLines.Contains(i)) continue;
            if (line.requiredFlags != null)
            {
                bool pass = true;
                foreach (var f in line.requiredFlags)
                    if (!GameState.HasFlag(f)) { pass = false; break; }
                if (!pass) continue;
            }
            if (line.forbiddenFlags != null)
            {
                bool blocked = false;
                foreach (var f in line.forbiddenFlags)
                    if (GameState.HasFlag(f)) { blocked = true; break; }
                if (blocked) continue;
            }
            available.Add(line);
        }
        return available.ToArray();
    }

    // ─────────────────────────────────────────────────────────────────────────

    private static bool WasCancelPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Escape);
#endif
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.9f, 0.2f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, triggerRadius);
        Gizmos.color = new Color(1f, 0.9f, 0.2f, 0.07f);
        Gizmos.DrawSphere(transform.position, triggerRadius);
    }

    // Draw a subtle gizmo even when not selected so you can see zones in the scene
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.9f, 0.2f, 0.15f);
        Gizmos.DrawWireSphere(transform.position, triggerRadius);
    }
}
