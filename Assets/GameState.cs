using System.Collections.Generic;

public static class GameState
{
    private static readonly HashSet<string> flags = new HashSet<string>();

    public static void SetFlag(string flag) => flags.Add(flag);
    public static void ClearFlag(string flag) => flags.Remove(flag);
    public static bool HasFlag(string flag) => flags.Contains(flag);

    public const string HasBone       = "has_bone";
    public const string QuestComplete = "quest_complete";
    public const string ShopUnlocked  = "shop_unlocked";

    // Boats bought from Lil Zoinks' shop. Each buyable boat's RaftController
    // gates boarding behind one of these flags (set by a SenseiShop purchase).
    public const string SpeedboatOwned = "boat_speedboat_owned";

    // ---- Coins ----
    public static int Coins { get; private set; } = 0;

    public static void AddCoins(int amount)    => Coins += amount;
    public static bool SpendCoins(int amount)
    {
        if (Coins < amount) return false;
        Coins -= amount;
        return true;
    }
}
