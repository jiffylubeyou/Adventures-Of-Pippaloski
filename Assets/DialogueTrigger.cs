using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class DialogueTrigger : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] private float talkRadius = 4f;

    [Header("Dialogue")]
    [SerializeField] private string npcName = "Chief";
    [TextArea(2, 5)]
    [SerializeField] private string greeting = "Woof! Welcome to the island, young pup. I am Chief, guardian of this land.";
    [SerializeField] private DialogueLine[] lines = new DialogueLine[]
    {
        new DialogueLine
        {
            playerPrompt = "I would like to embark on a quest.",
            npcResponse  = "A brave pup! Very well. The island needs a hero. Seek out the ancient bone hidden beyond the misty hills. Return it to me and you shall be rewarded."
        },
        new DialogueLine
        {
            playerPrompt = "Good bye.",
            npcResponse  = "Safe travels, pup.",
            closesDialogue = true
        }
    };

    private DialogueUI ui;
    private Transform player;
    private bool inRange;
    private bool talking;

    private readonly HashSet<int> consumedLines = new HashSet<int>();
    private readonly HashSet<int> unlockedLines  = new HashSet<int>();

    private void Start()
    {
        ui = DialogueUI.GetOrCreate();
        var playerObj = GameObject.FindGameObjectWithTag("Player")
                     ?? GameObject.Find("Player Border Collie");
        if (playerObj != null)
            player = playerObj.transform;
    }

    private void Update()
    {
        if (player == null) return;

        float dist = Vector3.Distance(transform.position, player.position);
        bool wasInRange = inRange;
        inRange = dist <= talkRadius;

        if (inRange && !talking)
        {
            ui.ShowPrompt("E  talk to " + npcName);
            if (WasTalkPressed())
                BeginDialogue();
        }
        else if (!inRange && !talking)
        {
            ui.HidePrompt();
        }

        if (talking && WasCancelPressed())
            EndDialogue();
    }

    private void BeginDialogue()
    {
        talking = true;
        ui.HidePrompt();
        ui.OpenDialogue(npcName, greeting, GetAvailableLines(), OnOptionChosen);
    }

    private void OnOptionChosen(DialogueLine chosen)
    {
        // Consume one-time lines and process locks/unlocks
        int chosenIndex = System.Array.IndexOf(lines, chosen);
        if (chosen.oneTimeOnly && chosenIndex >= 0)
            consumedLines.Add(chosenIndex);

        if (chosen.unlocksLineIndices != null)
            foreach (var i in chosen.unlocksLineIndices)
                unlockedLines.Add(i);

        if (chosen.locksLineIndices != null)
            foreach (var i in chosen.locksLineIndices)
                consumedLines.Add(i);

        if (chosen.closesDialogue)
        {
            ui.ShowResponse(npcName, chosen.npcResponse, EndDialogue);
        }
        else if (chosen.followUpLines != null && chosen.followUpLines.Length > 0)
        {
            ui.ShowResponse(npcName, chosen.npcResponse, () =>
                ui.OpenDialogue(npcName, "...", chosen.followUpLines, OnOptionChosen));
        }
        else
        {
            ui.ShowResponse(npcName, chosen.npcResponse, () =>
                ui.OpenDialogue(npcName, "Anything else?", GetAvailableLines(), OnOptionChosen));
        }
    }

    private DialogueLine[] GetAvailableLines()
    {
        var available = new List<DialogueLine>();
        for (int i = 0; i < lines.Length; i++)
        {
            if (consumedLines.Contains(i)) continue;
            if (lines[i].startsLocked && !unlockedLines.Contains(i)) continue;
            available.Add(lines[i]);
        }
        return available.ToArray();
    }

    private void EndDialogue()
    {
        talking = false;
        ui.CloseDialogue();
        if (inRange)
            ui.ShowPrompt("E  talk to " + npcName);
    }

    private static bool WasTalkPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.E);
#endif
    }

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
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, talkRadius);
    }
}

[System.Serializable]
public class DialogueLine
{
    [TextArea(1, 2)]
    public string playerPrompt;
    [TextArea(2, 4)]
    public string npcResponse;
    public bool closesDialogue;
    // Disappears permanently after being chosen once
    public bool oneTimeOnly;
    // Hidden until another line unlocks it via unlocksLineIndices
    public bool startsLocked;
    // Indices of root lines to unlock when this option is chosen
    public int[] unlocksLineIndices;
    // Indices of root lines to permanently hide when this option is chosen
    public int[] locksLineIndices;
    // If filled, these options appear after the NPC responds instead of going back to the root menu
    public DialogueLine[] followUpLines;
}
