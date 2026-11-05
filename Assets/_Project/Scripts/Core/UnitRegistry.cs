using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 楼层级单位注册表：把「场上谁算单位」收敛为单一事实源，替代逐帧 / 多次
/// <c>FindObjectsByType&lt;UnitHealth&gt;</c> 全场扫描（`Docs/01` §1.4 不变量 8）。
///
/// 生命周期：<see cref="UnitHealth"/> 在 Awake 末注册、OnDestroy 注销；
/// OnEnable / OnDisable 只维护「是否激活」（<b>不</b>注销），因此 SetActive(false→true) 不丢身份。
///
/// 查询语义与替换前的全场扫描保持一致：
///   - 默认<b>排除未激活</b>（等价 FindObjectsInactive.Exclude）；
///   - 需要含未激活时必须显式 <c>includeInactive: true</c>；
///   - 距离一律用 3D（含 Y），与 Unity Physics/Vector3.Distance 口径一致；
///   - <b>不</b>隐式跳过 dead 单位——由调用方按自己的口径传 filter / 自行判断。
///
/// 返回的列表是注册表内部缓存视图（只读、随注册/激活变化就地刷新），调用方<b>不要</b>长期持有
/// 修改、也不要向下转型成 List；需要独立副本时自行复制。
/// </summary>
public static class UnitRegistry
{
    /// <summary>已注册总数（含未激活）。</summary>
    public static int Count { get { return _units.Count; } }

    // ===== 注册数据 =====
    static readonly List<UnitHealth> _units = new List<UnitHealth>();                  // 注册顺序；含未激活
    static readonly Dictionary<UnitHealth, UnitId> _ids = new Dictionary<UnitHealth, UnitId>();
    static readonly Dictionary<int, UnitHealth> _byId = new Dictionary<int, UnitHealth>();
    static readonly HashSet<UnitHealth> _active = new HashSet<UnitHealth>();

    // ===== 查询视图（懒重建，零分配）=====
    static readonly List<UnitHealth> _viewAll = new List<UnitHealth>();                // 全部注册（含未激活）
    static readonly List<UnitHealth> _viewActive = new List<UnitHealth>();             // 仅激活
    static readonly List<UnitHealth> _viewPlayers = new List<UnitHealth>();            // 激活 + Player
    static readonly List<UnitHealth> _viewMonsters = new List<UnitHealth>();           // 激活 + Monster
    static readonly List<UnitHealth> _viewPlayersAny = new List<UnitHealth>();         // 含未激活 + Player
    static readonly List<UnitHealth> _viewMonstersAny = new List<UnitHealth>();        // 含未激活 + Monster

    static bool _dirty = true;
    static int _nextId;

    /// <summary>进入 Play 时重置静态状态（域重载关闭时也保证不串数据），并挂场景卸载清理钩子。</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetForNewSession()
    {
        _units.Clear();
        _ids.Clear();
        _byId.Clear();
        _active.Clear();
        _viewAll.Clear();
        _viewActive.Clear();
        _viewPlayers.Clear();
        _viewMonsters.Clear();
        _viewPlayersAny.Clear();
        _viewMonstersAny.Clear();
        _nextId = 0;
        _dirty = true;

        SceneManager.sceneUnloaded -= OnSceneUnloaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    /// <summary>场景卸载后清理已销毁却未走到 OnDestroy 的残留登记。
    /// 只剔除已销毁项、不动存活项——单场景重载时新场景的 Awake 可能早于旧场景卸载，整表 Clear 会误伤新单位。</summary>
    static void OnSceneUnloaded(Scene scene)
    {
        bool anyDead = false;
        for (int i = 0; i < _units.Count; i++)
        {
            if (_units[i] == null) { anyDead = true; break; }
        }
        if (!anyDead) return;

        var survivors = new List<UnitHealth>(_units.Count);
        var survivorIds = new Dictionary<UnitHealth, UnitId>(_units.Count);
        for (int i = 0; i < _units.Count; i++)
        {
            UnitHealth u = _units[i];
            if (u == null) continue;
            survivors.Add(u);
            UnitId id;
            if (_ids.TryGetValue(u, out id)) survivorIds[u] = id;
        }

        _units.Clear();
        _units.AddRange(survivors);
        _ids.Clear();
        _byId.Clear();
        _active.Clear();
        for (int i = 0; i < survivors.Count; i++)
        {
            UnitHealth u = survivors[i];
            UnitId id;
            if (!survivorIds.TryGetValue(u, out id))
            {
                id = new UnitId(++_nextId);
                survivorIds[u] = id;
            }
            _ids[u] = id;
            _byId[id.Value] = u;
            if (u.gameObject.activeInHierarchy) _active.Add(u);
        }
        _dirty = true;
    }

    // ============================================================
    // 注册 / 激活
    // ============================================================

    /// <summary>注册单位（幂等）。由 <see cref="UnitHealth"/> 在 Awake 末尾调用。</summary>
    public static void Register(UnitHealth unit)
    {
        if (unit == null) return;

        if (_ids.ContainsKey(unit))
        {
            // 重复注册（例如域重载后重新 Awake）：只同步激活态，不改身份
            SetActiveState(unit, unit.gameObject.activeInHierarchy);
            return;
        }

        UnitId id = new UnitId(++_nextId);
        _ids[unit] = id;
        _byId[id.Value] = unit;
        _units.Add(unit);
        if (unit.gameObject.activeInHierarchy) _active.Add(unit);
        _dirty = true;
    }

    /// <summary>注销单位（幂等）。由 <see cref="UnitHealth"/> 在 OnDestroy 调用。</summary>
    public static void Unregister(UnitHealth unit)
    {
        if (unit == null) return;

        UnitId id;
        if (!_ids.TryGetValue(unit, out id)) return;

        _ids.Remove(unit);
        UnitHealth current;
        if (_byId.TryGetValue(id.Value, out current) && current == unit) _byId.Remove(id.Value);
        _units.Remove(unit);
        _active.Remove(unit);
        _dirty = true;
    }

    /// <summary>维护「激活」标志（OnEnable / OnDisable 调用）。未注册时忽略——Awake 会按当时激活态注册。</summary>
    public static void SetActiveState(UnitHealth unit, bool active)
    {
        if (unit == null) return;
        if (!_ids.ContainsKey(unit)) return;

        bool changed = active ? _active.Add(unit) : _active.Remove(unit);
        if (changed) _dirty = true;
    }

    /// <summary>清空所有登记（不销毁对象）。用于显式重开/切换楼层；不要在有查询正在遍历时调用。</summary>
    public static void Clear()
    {
        _units.Clear();
        _ids.Clear();
        _byId.Clear();
        _active.Clear();
        _dirty = true;
    }

    // ============================================================
    // 查询
    // ============================================================

    /// <summary>全部已注册单位；默认排除未激活（等价 FindObjectsInactive.Exclude）。</summary>
    public static IReadOnlyList<UnitHealth> All(bool includeInactive = false)
    {
        EnsureViews();
        return includeInactive ? _viewAll : _viewActive;
    }

    /// <summary>按队伍查询；默认排除未激活。</summary>
    public static IReadOnlyList<UnitHealth> OfTeam(Team team, bool includeInactive = false)
    {
        EnsureViews();
        if (team == Team.Player) return includeInactive ? _viewPlayersAny : _viewPlayers;
        return includeInactive ? _viewMonstersAny : _viewMonsters;
    }

    /// <summary>球形范围查询（3D 距离，<c>&lt;= radius</c>）。<paramref name="results"/> 会先被清空再填充。</summary>
    public static void InRadius(Vector3 center, float radius, List<UnitHealth> results,
                               Team? team = null, bool includeInactive = false)
    {
        if (results == null) return;
        EnsureViews();

        results.Clear();
        IReadOnlyList<UnitHealth> source = includeInactive ? _viewAll : _viewActive;
        float r2 = radius * radius;
        for (int i = 0; i < source.Count; i++)
        {
            UnitHealth u = source[i];
            if (u == null) continue;
            if (team.HasValue && u.team != team.Value) continue;
            if ((u.transform.position - center).sqrMagnitude > r2) continue;
            results.Add(u);
        }
    }

    /// <summary>范围内最近的指定队伍单位（3D 距离，严格 <c>&lt; maxRange</c>，与替换前的 <c>d &lt; bestD</c> 口径一致）。
    /// 不隐式跳过 dead；需要时用 <paramref name="filter"/> 传入调用方自己的判定。</summary>
    public static UnitHealth Nearest(Vector3 center, float maxRange, Team team,
                                    System.Func<UnitHealth, bool> filter = null)
    {
        EnsureViews();

        IReadOnlyList<UnitHealth> source = team == Team.Player ? _viewPlayers : _viewMonsters;
        UnitHealth best = null;
        float bestD = maxRange;
        for (int i = 0; i < source.Count; i++)
        {
            UnitHealth u = source[i];
            if (u == null) continue;
            if (filter != null && !filter(u)) continue;
            float d = Vector3.Distance(center, u.transform.position);
            if (d < bestD) { bestD = d; best = u; }
        }
        return best;
    }

    /// <summary>按句柄取单位；无效 / 已注销 / 已销毁返回 null。</summary>
    public static UnitHealth Find(UnitId id)
    {
        if (!id.IsValid) return null;
        UnitHealth unit;
        if (!_byId.TryGetValue(id.Value, out unit)) return null;
        return unit;
    }

    /// <summary>取单位句柄；未注册返回 false（<paramref name="id"/> = <see cref="UnitId.None"/>）。</summary>
    public static bool TryGetId(UnitHealth unit, out UnitId id)
    {
        if (unit == null) { id = UnitId.None; return false; }
        return _ids.TryGetValue(unit, out id);
    }

    // ============================================================
    // 内部
    // ============================================================

    static void EnsureViews()
    {
        if (!_dirty) return;
        _dirty = false;

        _viewAll.Clear();
        _viewActive.Clear();
        _viewPlayers.Clear();
        _viewMonsters.Clear();
        _viewPlayersAny.Clear();
        _viewMonstersAny.Clear();

        for (int i = 0; i < _units.Count; i++)
        {
            UnitHealth u = _units[i];
            if (u == null) continue;   // 已销毁、尚未走到 OnDestroy 的瞬时残留

            _viewAll.Add(u);
            if (u.team == Team.Player) _viewPlayersAny.Add(u);
            else _viewMonstersAny.Add(u);

            if (!_active.Contains(u)) continue;
            _viewActive.Add(u);
            if (u.team == Team.Player) _viewPlayers.Add(u);
            else _viewMonsters.Add(u);
        }
    }
}
