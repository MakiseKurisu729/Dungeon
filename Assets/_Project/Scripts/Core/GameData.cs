using UnityEngine;

/// <summary>局内静态数据：金币/等级/击杀/消息。static 字段跨场景保留。</summary>
public static class GameData
{
    public static int gold = 0;
    public static int level = 1;
    public static int kills = 0;
    public static string message = "";

    public static event System.Action OnChanged;

    public static void AddGold(int g) { gold += g; OnChanged?.Invoke(); }
    public static void AddKill() { kills++; }
    public static void SetMessage(string m) { message = m; OnChanged?.Invoke(); }

    public static int LevelCost() { return 50 * level; }

    public static bool TryLevelUp(MeleeCombat sw, UnitHealth uh)
    {
        if (gold < LevelCost()) return false;
        gold -= LevelCost();
        level++;
        if (sw != null) sw.dmgMult += 0.25f;
        if (uh != null) uh.AddMaxHp(20f);
        OnChanged?.Invoke();
        return true;
    }
}