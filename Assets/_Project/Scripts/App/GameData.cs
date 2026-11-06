using Dungeon.Core;
using Dungeon.Gameplay.Combat;
using Dungeon.Gameplay.Units;
namespace Dungeon.App {
    /// <summary>【薄的适配层】旧 GameData API：全部转发到 RunState（金币/击杀）、MetaState（等级/升级）、MessageFeed（消息）。
    /// 新代码请直接用 RunState / MetaState / MessageFeed；本类只为测试与未迁移引用保留，签名与可写性与重构前一致。</summary>
    public static class GameData
    {
        public static int gold { get { return RunState.Gold; } set { RunState.Gold = value; } }
        public static int level { get { return MetaState.Level; } set { MetaState.Level = value; } }
        public static int kills { get { return RunState.Kills; } set { RunState.Kills = value; } }
        public static string message { get { return MessageFeed.Message; } set { MessageFeed.Message = value; } }

        public static event System.Action OnChanged
        {
            add { MessageFeed.Changed += value; }
            remove { MessageFeed.Changed -= value; }
        }

        public static void AddGold(int g) { RunState.AddGold(g); MessageFeed.RaiseChanged(); }
        public static void AddKill() { RunState.AddKill(); }
        public static void SetMessage(string m) { MessageFeed.SetMessage(m); }

        public static int LevelCost() { return MetaState.LevelCost(); }

        public static bool TryLevelUp(MeleeCombat sw, UnitHealth uh) { return MetaState.TryLevelUp(sw, uh); }
    }
}
