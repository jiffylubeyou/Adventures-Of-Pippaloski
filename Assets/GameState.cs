using System.Collections.Generic;

public static class GameState
{
    private static readonly HashSet<string> flags = new HashSet<string>();

    public static void SetFlag(string flag) => flags.Add(flag);
    public static void ClearFlag(string flag) => flags.Remove(flag);
    public static bool HasFlag(string flag) => flags.Contains(flag);

    public const string HasBone       = "has_bone";
    public const string QuestComplete = "quest_complete";
}
