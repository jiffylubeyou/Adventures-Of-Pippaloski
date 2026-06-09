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
                     ?? GameObject.Find("Pippaloski")
                     ?? GameObject.Find("Player Border Collie");
        if (playerObj != null)
            player = playerObj.transform;
    }

    private void Update()
    {
        if (player == null) return;

        float dist = Vector3.Distance(transform.position, player.position);
        inRange = dist <= talkRadius;

        if (inRange && !talking)
        {
            ui.RequestPrompt(this, "E  talk to " + npcName, dist);
            if (WasTalkPressed())
                BeginDialogue();
        }
        else
        {
            ui.ReleasePrompt(this);
        }

        if (talking && WasCancelPressed())
            EndDialogue();
    }

    private void BeginDialogue()
    {
        talking = true;
        ui.ReleasePrompt(this);
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

        if (chosen.clearsFlags != null)
            foreach (var f in chosen.clearsFlags)
                GameState.ClearFlag(f);

        if (chosen.setsFlags != null)
            foreach (var f in chosen.setsFlags)
                GameState.SetFlag(f);

        if (chosen.grantsBoatAccess)
            GameState.SetFlag(GameState.QuestComplete);

        if (chosen.opensShop)
        {
            GameState.SetFlag(GameState.ShopUnlocked);
            var shop = GetComponent<SenseiShop>();
            ui.ShowResponse(npcName, chosen.npcResponse, () =>
            {
                EndDialogue();
                shop?.OpenShop();
            });
        }
        else if (chosen.closesDialogue)
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

    private void EndDialogue()
    {
        talking = false;
        ui.CloseDialogue();
        // Update will re-register the prompt next frame if still in range
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
    // All listed GameState flags must be set for this line to show
    public string[] requiredFlags;
    // If any listed GameState flags are set, this line is hidden
    public string[] forbiddenFlags;
    // These GameState flags are cleared when this line is chosen
    public string[] clearsFlags;
    // These GameState flags are set when this line is chosen
    public string[] setsFlags;
    // Tick this on Chief's boat-grant line to unlock the raft
    public bool grantsBoatAccess;
    // Tick this on the line whose response should open the shop afterward
    public bool opensShop;
    // If filled, these options appear after the NPC responds instead of going back to the root menu
    public DialogueLine[] followUpLines;
}
