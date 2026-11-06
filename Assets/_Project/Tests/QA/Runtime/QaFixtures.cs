using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Dungeon.Content;
using Dungeon.Core;

/// <summary>
/// Play 模式夹具与观测工具：玩家探针、命中收集器、内存目标单位、选择、动画状态名。
/// 全部为无状态纯函数/工厂；不持有跨调用静态状态，不销毁不属于自己创建的对象。
/// </summary>
public static class QaFixtures
{
    /// <summary>生产玩家单位名（场景固定三角色）。</summary>
    public static readonly string[] PlayerNames = { "Knight_Unit", "Archer_Unit", "Mage_Unit" };

    public const string Locomotion = "Locomotion";
    public const string AttackMelee = "AttackMelee";
    public const string AttackDraw = "Attack_Draw";
    public const string AttackRelease = "Attack_Release";
    public const string HitA = "Hit_A";

    /// <summary>
    /// 单个玩家单位的运行期探针（超集：SPEC01 的周期/事件字段 + REFACTOR 的技能/救援字段）。
    /// </summary>
    public class PlayerProbe
    {
        public string name;
        public UnitHealth health;
        public CharacterBrain brain;
        public Animator animator;
        public Selectable selectable;
        public MeleeCombat sword;
        public RangedCombat ranged;
        public SkillSystem skills;
        public DownedRescue rescue;
        public Vector3 home;
        public float attackSpeed = 1f;
        /// <summary>攻击动画周期 = attackLen / attackSpeed。</summary>
        public float cycle = 1f;
        /// <summary>攻击窗口实际占位周期（含 2.8s 下限）。</summary>
        public float effectiveCycle = 1f;

        public int drawEntries;
        public int releaseEntries;
        public int meleeEntries;
        public int fireCount;
        public string lastState = "";
        public bool lastFired;
        public float firstDrawTime = -1f;
        public float firstFireTime = -1f;
        public List<Dictionary<string, object>> fireEvents = new List<Dictionary<string, object>>();
        public List<Dictionary<string, object>> hitEvents = new List<Dictionary<string, object>>();

        public Transform tx { get { return health != null ? health.transform : null; } }
    }

    /// <summary>
    /// 目标受击收集器：按 source 过滤（0 = 不过滤），记录命中瞬间的动画状态与归一化时间。
    /// <paramref name="includeElement"/> 为 false 时不带 element 字段（REFACTOR 口径）；
    /// <paramref name="maxEvents"/> 为事件上限（SPEC01 = 500、REFACTOR = 200）。
    /// </summary>
    public class HitCollector
    {
        public UnitHealth Target;
        public int FilterSource;
        public int Count;
        public readonly List<Dictionary<string, object>> Events = new List<Dictionary<string, object>>();

        readonly int _maxEvents;
        readonly bool _includeElement;

        public HitCollector(int maxEvents = 500, bool includeElement = true)
        {
            _maxEvents = maxEvents;
            _includeElement = includeElement;
        }

        public void Subscribe(UnitHealth target, int filterSource)
        {
            Target = target;
            FilterSource = filterSource;
            if (Target != null) Target.OnHit += OnHit;
        }

        public void Unsubscribe()
        {
            if (Target != null) Target.OnHit -= OnHit;
        }

        void OnHit(DamageInfo info)
        {
            if (FilterSource != 0 && (info.source == null || info.source.GetInstanceID() != FilterSource)) return;
            Count++;
            string state = "none";
            float nt = -1f;
            int sourceId = 0;
            if (info.source != null)
            {
                sourceId = info.source.GetInstanceID();
                Animator a = info.source.GetComponentInChildren<Animator>();
                state = StateName(a);
                nt = StateNorm(a);
            }
            if (Events.Count < _maxEvents)
            {
                var ev = QaJson.Dict("time", Time.time, "sourceId", sourceId, "state", state,
                                     "normalizedTime", nt, "amount", info.finalAmount);
                if (_includeElement) ev["element"] = DamageElementName(info);
                Events.Add(ev);
            }
        }
    }

    /// <summary>按场景固定顺序找齐三个玩家单位（找不到的直接不放进结果）。</summary>
    public static List<PlayerProbe> FindPlayers()
    {
        var players = new List<PlayerProbe>();
        foreach (string n in PlayerNames)
        {
            PlayerProbe p = FindPlayer(n);
            if (p != null) players.Add(p);
        }
        return players;
    }

    /// <summary>按名字（允许 "名字 后缀" / "名字(后缀)" 的生产命名）找单个玩家单位。</summary>
    public static PlayerProbe FindPlayer(string name)
    {
        foreach (var h in UnityEngine.Object.FindObjectsByType<UnitHealth>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (h == null || h.team != Team.Player) continue;
            if (!(h.name == name || h.name.StartsWith(name + " ", StringComparison.Ordinal) ||
                  h.name.StartsWith(name + "(", StringComparison.Ordinal))) continue;

            var p = new PlayerProbe();
            p.name = name;
            p.health = h;
            p.brain = h.GetComponent<CharacterBrain>();
            p.animator = h.GetComponentInChildren<Animator>();
            p.selectable = h.GetComponent<Selectable>();
            p.sword = h.GetComponent<MeleeCombat>();
            p.ranged = h.GetComponent<RangedCombat>();
            p.skills = h.GetComponent<SkillSystem>();
            p.rescue = h.GetComponent<DownedRescue>();
            p.home = h.transform.position;
            p.attackSpeed = p.brain != null && p.brain.stats != null ? p.brain.stats.attackSpeed : 1f;
            float len = p.sword != null ? p.sword.attackLen : (p.ranged != null ? p.ranged.attackLen : 1f);
            float spd = p.attackSpeed > 0f ? p.attackSpeed : 1f;
            p.cycle = len / spd;
            p.effectiveCycle = Mathf.Max(p.cycle, 2.8f / spd);
            return p;
        }
        return null;
    }

    /// <summary>走生产选择入口 SelectionManager.SingleSelect(Selectable) 选中该单位。</summary>
    public static void Select(PlayerProbe p)
    {
        var sm = SelectionManager.instance;
        if (sm == null || p == null || p.health == null) return;
        var sel = p.selectable != null ? p.selectable : p.health.GetComponent<Selectable>();
        if (sel == null) return;
        var m = typeof(SelectionManager).GetMethod("SingleSelect", BindingFlags.Instance | BindingFlags.NonPublic);
        if (m != null) m.Invoke(sm, new object[] { sel });
    }

    /// <summary>
    /// 运行期内存目标单位（高 HP、无 AI、无伤害、静态）。调用方负责销毁返回的 GameObject 与其 UnitStats。
    /// </summary>
    public static GameObject CreateTarget(string name, Vector3 pos, bool showDamageNumber = true)
    {
        var go = new GameObject(name);
        go.SetActive(false);
        go.transform.position = pos;

        var stats = ScriptableObject.CreateInstance<UnitStats>();
        stats.team = Team.Monster;
        stats.maxHp = 100000f;
        stats.damage = 0f;
        stats.attackRange = 0f;
        stats.preferredRange = 0f;
        stats.aggroRange = 0f;
        stats.moveSpeed = 0f;
        stats.attackSpeed = 1f;

        var uh = go.AddComponent<UnitHealth>();
        uh.stats = stats;
        uh.showDamageNumber = showDamageNumber;

        var col = go.AddComponent<CapsuleCollider>();
        col.center = new Vector3(0f, 0.9f, 0f);
        col.height = 1.8f;
        col.radius = 0.4f;

        go.SetActive(true);
        return go;
    }

    /// <summary>layer 0 动画状态名（未识别的状态返回 "Other:&lt;hash&gt;"）。</summary>
    public static string StateName(Animator a, int layer = 0)
    {
        if (a == null) return "null";
        if (layer < 0 || layer >= a.layerCount) return "layer_out_of_range";
        var st = a.GetCurrentAnimatorStateInfo(layer);
        if (st.IsName(Locomotion)) return Locomotion;
        if (st.IsName(AttackMelee)) return AttackMelee;
        if (st.IsName(AttackDraw)) return AttackDraw;
        if (st.IsName(AttackRelease)) return AttackRelease;
        if (st.IsName(HitA)) return HitA;
        if (st.IsName("Q_1")) return "Q_1";
        if (st.IsName("W_1")) return "W_1";
        if (st.IsName("E_1")) return "E_1";
        if (st.IsName("R_1")) return "R_1";
        if (st.IsName("Interact")) return "Interact";
        if (st.IsName("Downed")) return "Downed";
        if (st.IsName("Rise")) return "Rise";
        return "Other:" + st.shortNameHash;
    }

    /// <summary>layer 0 归一化时间（&gt;1 时钳到 1）。</summary>
    public static float StateNorm(Animator a, int layer = 0)
    {
        if (a == null || layer < 0 || layer >= a.layerCount) return -1f;
        float n = a.GetCurrentAnimatorStateInfo(layer).normalizedTime;
        return n <= 1f ? n : 1f;
    }

    /// <summary>反射读取 DamageInfo.element 的枚举名（字段缺失返回 "&lt;missing&gt;"）。</summary>
    public static string DamageElementName(DamageInfo info)
    {
        var f = typeof(DamageInfo).GetField("element", BindingFlags.Public | BindingFlags.Instance);
        if (f == null) return "<missing>";
        object v = f.GetValue(info);
        return v != null ? v.ToString() : "<null>";
    }
}
