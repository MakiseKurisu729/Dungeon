using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// SPEC-DIAG-SKILL 一次性运行期探针（Play 模式）。
/// 目的：用断言定位「按 Q 无任何反应」的第一个失效节点。
/// 顺序：场景控制器普查 → 指示器消费者 → 第一退出点 → 预制体槽位 →
///       CastChain Q → CastChain W → EmptySlots → 结论。
/// 只读生产状态；夹具在内存中创建并在结束前销毁；不写场景、不改资产。
/// </summary>
public class DIAG_ProbeRunner : MonoBehaviour
{
    const string ScenePath = "Assets/Scenes/DungeonWorld.unity";
    const string FinishedKey = "DIAG.Finished";
    const string ExitCodeKey = "DIAG.ExitCode";

    static readonly Vector3 FixturePos = new Vector3(50f, 0f, 50f);

    static DIAG_Evidence _ev;
    static bool _finished;
    static string _exitReason = "";
    static string _conclusion = "";

    int _run;
    string _mode;
    float _deadline;

    SkillInputController[] _controllers = new SkillInputController[0];
    SkillIndicator[] _indicators = new SkillIndicator[0];
    int _controllerCount;
    string _firstExit = "UNKNOWN";

    bool _qPass;
    bool _wPass;
    bool _emptyPass;

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    IEnumerator Start()
    {
        int parsed;
        string envRun = Environment.GetEnvironmentVariable("DIAG_RUN");
        if (int.TryParse(envRun, out parsed)) _run = parsed;
        if (_run <= 0) _run = 1;
        string envMode = Environment.GetEnvironmentVariable("DIAG_MODE");
        _mode = string.IsNullOrEmpty(envMode) ? "probe" : envMode.Trim().ToLowerInvariant();

        _ev = new DIAG_Evidence(_run, _mode);
        _deadline = Time.realtimeSinceStartup + 300f;

        Debug.Log("[DIAG] runner start; mode=" + _mode + "; run=" + _run +
                  "; unity=" + Application.unityVersion +
                  "; evidenceDir=" + _ev.EvidenceDir + "; head=" + GetHead());

        float waited = 0f;
        while (SceneManager.GetActiveScene().path != ScenePath && waited < 25f)
        {
            waited += Time.unscaledDeltaTime;
            yield return null;
        }
        Debug.Log("[DIAG] active scene=" + SceneManager.GetActiveScene().path + "; waited=" + waited.ToString("0.###") + "s");

        yield return new WaitForSecondsRealtime(1.0f);

        yield return RunSafe("Diag.Scene.SkillInputController", AssertController());
        yield return RunSafe("Diag.Scene.Indicator", AssertIndicator());
        yield return RunSafe("Diag.Input.FirstExit", AssertFirstExit());
        yield return RunSafe("Diag.Prefab.Slots", AssertPrefabSlots());
        yield return RunSafe("Diag.CastChain.Q", AssertQ());
        yield return RunSafe("Diag.CastChain.W", AssertW());
        yield return RunSafe("Diag.CastChain.EmptySlots", AssertEmptySlots());

        _conclusion = (_controllerCount == 0 && _qPass && _emptyPass)
            ? "Q 下游施法链可独立跑通；失效点在输入层，第一个未执行节点 = SkillInputController.Update()"
              + (_wPass
                  ? "；W 下游链亦可跑通"
                  : "；W 另有一个独立资产缺陷（W_Slash_Effect 解析为 BashEffect），不属输入层")
            : "见断言数据（Q/W 链条或控制器状态与预期不符）";
        Debug.Log("[DIAG][CONCLUSION] " + _conclusion);

        Finish();
    }

    void Update()
    {
        if (_finished || _ev == null) return;
        if (Time.realtimeSinceStartup <= _deadline) return;
        _ev.InfraFailure("all", "Runner.Watchdog", "运行超时（300 秒）被强制结束", "");
        _exitReason = "watchdog timeout";
        Finish();
    }

    IEnumerator RunSafe(string id, IEnumerator routine)
    {
        while (true)
        {
            object cur;
            try
            {
                if (!routine.MoveNext()) break;
                cur = routine.Current;
            }
            catch (Exception e)
            {
                Debug.LogError("[DIAG] assertion " + id + " threw: " + e);
                _ev.InfraFailure(id, id, e.Message, e.StackTrace);
                yield break;
            }
            yield return cur;
        }
    }

    // ============================================================
    // 场景：控制器 / 指示器 / 第一退出点
    // ============================================================
    IEnumerator AssertController()
    {
        float waited = 0f;
        while (waited < 3f)
        {
            _controllers = FindObjectsByType<SkillInputController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            if (_controllers.Length > 0) break;
            waited += Time.unscaledDeltaTime;
            yield return null;
        }
        _controllers = FindObjectsByType<SkillInputController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        _controllerCount = _controllers.Length;
        _indicators = FindObjectsByType<SkillIndicator>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        bool pass = _controllerCount == 1;
        _ev.Record("DIAG-SKILL", "Diag.Scene.SkillInputController",
            Dict("expectedCount", 1, "note", "DungeonWorld 运行期应恰好存在 1 个 SkillInputController"),
            Dict("count", _controllerCount, "waitedSeconds", waited),
            pass, pass ? "pass" : "product_fail", "production",
            Dict("scene", SceneManager.GetActiveScene().path,
                 "waitedSeconds", waited,
                 "controllerCount", _controllerCount,
                 "controllerNames", NamesOf(_controllers),
                 "indicatorCount", _indicators.Length,
                 "note", "count=0 即复现目标缺陷：输入层组件缺失，Update() 从未执行"));
    }

    IEnumerator AssertIndicator()
    {
        yield return null;
        _indicators = FindObjectsByType<SkillIndicator>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        bool matOk = _indicators.Length == 1 && _indicators[0].mat != null;
        bool consumerOk = false;
        int consumerIndicatorId = -1;
        string controllerIndicator = "none";
        if (_controllers.Length == 1 && _controllers[0] != null)
        {
            var ind = _controllers[0].indicator;
            if (ind != null)
            {
                consumerIndicatorId = ind.GetInstanceID();
                controllerIndicator = ind.name;
                consumerOk = System.Array.IndexOf(_indicators, ind) >= 0;
            }
        }
        bool pass = _indicators.Length == 1 && matOk && consumerOk;

        _ev.Record("DIAG-SKILL", "Diag.Scene.Indicator",
            Dict("expectedCount", 1, "matAssigned", true, "hasConsumer", true),
            Dict("count", _indicators.Length, "matAssigned", matOk,
                 "hasConsumer", consumerOk, "consumerIndicatorId", consumerIndicatorId),
            pass, pass ? "pass" : "product_fail", "production",
            Dict("count", _indicators.Length,
                 "matAssigned", matOk,
                 "matName", _indicators.Length == 1 && _indicators[0].mat != null ? _indicators[0].mat.name : "none",
                 "hasConsumer", consumerOk,
                 "controllerIndicator", controllerIndicator,
                 "indicatorNames", NamesOf(_indicators),
                 "note", "有指示器、无输入消费者 = 孤儿组件（当前复现）"));
    }

    IEnumerator AssertFirstExit()
    {
        yield return null;
        var sm = SelectionManager.instance;
        Selectable primary = sm != null ? sm.Primary : null;
        string primaryName = primary != null ? primary.name : "none";
        bool primaryHasSkillSystem = false;
        string slot0 = "n/a";
        int slot0Effects = -1;

        if (_controllerCount == 0)
        {
            _firstExit = "MISSING_CONTROLLER";
        }
        else if (primary == null)
        {
            _firstExit = "NO_PRIMARY";
        }
        else
        {
            var ss = primary.GetComponentInParent<SkillSystem>();
            primaryHasSkillSystem = ss != null;
            if (ss == null)
            {
                _firstExit = "NO_PRIMARY";
            }
            else
            {
                var s = ss.GetSkill(0);
                slot0 = s != null ? s.name : "<empty>";
                slot0Effects = s != null && s.effects != null ? s.effects.Length : 0;
                if (s == null) _firstExit = "EMPTY_SLOT";
                else if (s.effects == null || s.effects.Length == 0) _firstExit = "NO_EFFECTS";
                else _firstExit = "NONE";
            }
        }

        bool pass = _firstExit == "MISSING_CONTROLLER";
        _ev.Record("DIAG-SKILL", "Diag.Input.FirstExit",
            Dict("expected", "MISSING_CONTROLLER", "note", "按 Q 首个失效节点"),
            Dict("firstExit", _firstExit,
                 "controllerCount", _controllerCount,
                 "selectionManagerInstance", sm != null,
                 "primary", primaryName,
                 "primaryHasSkillSystem", primaryHasSkillSystem,
                 "slot0", slot0,
                 "slot0Effects", slot0Effects),
            pass, pass ? "pass" : "product_fail", "production",
            Dict("enumeratedOrder", "MISSING_CONTROLLER → NO_PRIMARY → EMPTY_SLOT → NO_EFFECTS → NONE",
                 "controllerCount", _controllerCount,
                 "selectionManagerInstance", sm != null,
                 "primary", primaryName,
                 "primaryHasSkillSystem", primaryHasSkillSystem,
                 "slot0", slot0,
                 "slot0Effects", slot0Effects,
                 "note", "该枚举按生产 Update() 的早退顺序排列"));
    }

    // ============================================================
    // 预制体槽位（只读资产）
    // ============================================================
    IEnumerator AssertPrefabSlots()
    {
        GameObject prefab = null;
        string loadError = null;
#if UNITY_EDITOR
        try { prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SkillCastFixture.KnightPrefabPath); }
        catch (Exception e) { loadError = e.GetType().Name + ": " + e.Message; }
#else
        loadError = "非编辑器环境无法加载预制体";
#endif
        if (prefab == null)
        {
            _ev.BlockedAssertion("DIAG-SKILL", "Diag.Prefab.Slots",
                Dict("path", SkillCastFixture.KnightPrefabPath),
                "无法加载预制体: " + (loadError ?? "null"), Dict("loadError", loadError));
            yield break;
        }

        var ss = prefab.GetComponent<SkillSystem>();
        int len = ss != null && ss.skills != null ? ss.skills.Length : -1;
        var names = new List<object>();
        for (int i = 0; i < 4; i++)
        {
            SkillData s = ss != null && ss.skills != null && i < ss.skills.Length ? ss.skills[i] : null;
            names.Add(s != null ? s.name : "<empty>");
        }
        bool pass = ss != null && len == 4 &&
                    ss.skills[0] != null && ss.skills[1] != null &&
                    ss.skills[2] == null && ss.skills[3] == null;

        _ev.Record("DIAG-SKILL", "Diag.Prefab.Slots",
            Dict("slotCount", 4, "Q", "非空", "W", "非空", "E", "空", "R", "空"),
            Dict("slotCount", len, "slotNames", names),
            pass, pass ? "pass" : "product_fail", "production",
            Dict("prefab", SkillCastFixture.KnightPrefabPath,
                 "slotCount", len,
                 "slotNames", names,
                 "Q", names[0],
                 "W", names[1],
                 "E", names[2],
                 "R", names[3],
                 "note", "直接读取预制体资产，不实例化、不写资产"));
    }

    // ============================================================
    // 施法链
    // ============================================================
    IEnumerator AssertQ()
    {
        SkillCastFixture fx = null;
        var r = new CastProbeResult();
        try { fx = SkillCastFixture.Create(FixturePos, true); }
        catch (Exception e)
        {
            _ev.InfraFailure("Diag.CastChain.Q", "Diag.CastChain.Q", "夹具创建抛异常: " + e.Message, e.StackTrace);
            yield break;
        }
        if (fx == null || !string.IsNullOrEmpty(fx.Error))
        {
            string err = fx != null ? fx.Error : "null";
            if (fx != null) fx.Dispose();
            _ev.InfraFailure("Diag.CastChain.Q", "Diag.CastChain.Q", "夹具创建失败: " + err, "");
            yield break;
        }

        try
        {
            yield return fx.Prepare(6f);
            if (!fx.PrepareLocomotion)
            {
                _ev.BlockedAssertion("Diag.CastChain.Q", "Diag.CastChain.Q",
                    Dict("expectedState", "Bash", "damage", "hitTime 后靶掉血"),
                    "夹具未能进入 Locomotion（preState=" + fx.PrepareState + "）",
                    Dict("prepareState", fx.PrepareState, "preparePhase", fx.PreparePhase));
                yield break;
            }

            yield return fx.Cast(0, fx.Target != null ? fx.Target.transform : null, "Bash", 0.5f, r);

            bool manaOk = Mathf.Abs(r.manaDelta - 20f) < 0.01f;
            bool cdOk = r.cdRemaining > 0f && Mathf.Abs(r.cdRemaining - 6f) <= 0.05f;
            bool dmgOk = r.damageObserved && Mathf.Abs(r.damageApplied - r.expectedDamage) < 0.01f;
            bool animOk = r.animEntered && r.animEntrySeconds >= 0f && r.animEntrySeconds <= 0.5f;
            _qPass = r.castReturn && manaOk && cdOk && dmgOk && animOk;

            _ev.Record("Diag.CastChain.Q", "Diag.CastChain.Q",
                Dict("castReturn", true, "manaDelta", 20f, "cdRemaining", ">0 且≈6",
                     "targetDamage", "== MeleeCombat.GetRawDamage()", "animState", "Bash（≤0.5s）"),
                r.Data(),
                _qPass, _qPass ? "pass" : "product_fail", "production",
                Dict("slot", 0,
                     "fixturePath", fx.CasterPath(),
                     "fixturePos", V3(FixturePos),
                     "prepareState", fx.PrepareState,
                     "preparePhase", fx.PreparePhase,
                     "prepareSeconds", fx.PrepareSeconds,
                     "phaseWaitSeconds", fx.PhaseSeconds,
                     "animator", fx.AnimatorInfo(),
                     "manaOk", manaOk,
                     "cdOk", cdOk,
                     "damageOk", dmgOk,
                     "animOk", animOk,
                     "targetMaxHp", fx.TargetStats != null ? fx.TargetStats.maxHp : -1f));
        }
        finally
        {
            if (fx != null) fx.Dispose();
        }
        yield return null;
    }

    IEnumerator AssertW()
    {
        SkillCastFixture fx = null;
        var r = new CastProbeResult();
        try { fx = SkillCastFixture.Create(FixturePos, false); }
        catch (Exception e)
        {
            _ev.InfraFailure("Diag.CastChain.W", "Diag.CastChain.W", "夹具创建抛异常: " + e.Message, e.StackTrace);
            yield break;
        }
        if (fx == null || !string.IsNullOrEmpty(fx.Error))
        {
            string err = fx != null ? fx.Error : "null";
            if (fx != null) fx.Dispose();
            _ev.InfraFailure("Diag.CastChain.W", "Diag.CastChain.W", "夹具创建失败: " + err, "");
            yield break;
        }

        try
        {
            yield return fx.Prepare(6f);
            if (!fx.PrepareLocomotion)
            {
                _ev.BlockedAssertion("Diag.CastChain.W", "Diag.CastChain.W",
                    Dict("expectedState", "Skill", "swordTimer", ">=0"),
                    "夹具未能进入 Locomotion（preState=" + fx.PrepareState + "）",
                    Dict("prepareState", fx.PrepareState, "preparePhase", fx.PreparePhase));
                yield break;
            }

            yield return fx.Cast(1, null, "Skill", 0.5f, r);

            bool manaOk = Mathf.Abs(r.manaDelta - 30f) < 0.01f;
            bool cdOk = r.cdRemaining > 0f;
            bool timerOk = r.swordTimer >= 0f;
            bool animOk = r.animEntered && r.animEntrySeconds >= 0f && r.animEntrySeconds <= 0.5f;
            _wPass = r.castReturn && manaOk && cdOk && timerOk && animOk;

            _ev.Record("Diag.CastChain.W", "Diag.CastChain.W",
                Dict("castReturn", true, "manaDelta", 30f, "cdRemaining", ">0",
                     "swordTimer", ">=0（反射 _timer，证明效果已执行）", "animState", "Skill（≤0.5s）"),
                r.Data(),
                _wPass, _wPass ? "pass" : "product_fail", "production",
                Dict("slot", 1,
                     "fixturePath", fx.CasterPath(),
                     "fixturePos", V3(FixturePos),
                     "prepareState", fx.PrepareState,
                     "preparePhase", fx.PreparePhase,
                     "prepareSeconds", fx.PrepareSeconds,
                     "phaseWaitSeconds", fx.PhaseSeconds,
                     "animator", fx.AnimatorInfo(),
                     "manaOk", manaOk,
                     "cdOk", cdOk,
                     "timerOk", timerOk,
                     "animOk", animOk));
        }
        finally
        {
            if (fx != null) fx.Dispose();
        }
        yield return null;
    }

    IEnumerator AssertEmptySlots()
    {
        SkillCastFixture fx = null;
        var e = new CastProbeResult();
        var rr = new CastProbeResult();
        try { fx = SkillCastFixture.Create(FixturePos, false); }
        catch (Exception ex)
        {
            _ev.InfraFailure("Diag.CastChain.EmptySlots", "Diag.CastChain.EmptySlots",
                "夹具创建抛异常: " + ex.Message, ex.StackTrace);
            yield break;
        }
        if (fx == null || !string.IsNullOrEmpty(fx.Error))
        {
            string err = fx != null ? fx.Error : "null";
            if (fx != null) fx.Dispose();
            _ev.InfraFailure("Diag.CastChain.EmptySlots", "Diag.CastChain.EmptySlots",
                "夹具创建失败: " + err, "");
            yield break;
        }

        try
        {
            yield return fx.Prepare(6f);
            if (!fx.PrepareLocomotion)
            {
                _ev.BlockedAssertion("Diag.CastChain.EmptySlots", "Diag.CastChain.EmptySlots",
                    Dict("castReturn", false, "manaDelta", 0f, "exception", null),
                    "夹具未能进入 Locomotion（preState=" + fx.PrepareState + "）",
                    Dict("prepareState", fx.PrepareState));
                yield break;
            }

            yield return fx.Cast(2, null, null, 0f, e);
            yield return fx.Cast(3, null, null, 0f, rr);

            bool pass = !e.castReturn && !rr.castReturn &&
                        Mathf.Abs(e.manaDelta) < 0.0001f && Mathf.Abs(rr.manaDelta) < 0.0001f &&
                        string.IsNullOrEmpty(e.exception) && string.IsNullOrEmpty(rr.exception);
            _emptyPass = pass;

            _ev.Record("Diag.CastChain.EmptySlots", "Diag.CastChain.EmptySlots",
                Dict("castReturn2", false, "castReturn3", false, "manaDelta", 0f, "exception", null),
                Dict("slot2", e.Data(), "slot3", rr.Data()),
                pass, pass ? "pass" : "product_fail", "production",
                Dict("slot2", e.Data(),
                     "slot3", rr.Data(),
                     "slotNames", new List<object> { e.slotName, rr.slotName },
                     "note", "空槽=设计内静默，非缺陷（不修）"));
        }
        finally
        {
            if (fx != null) fx.Dispose();
        }
        yield return null;
    }

    // ============================================================
    // 收尾
    // ============================================================
    void Finish()
    {
        if (_finished) return;
        _finished = true;

        if (_ev != null)
        {
            _ev.WriteSummary(ScenePath, string.IsNullOrEmpty(_exitReason) ? "completed" : _exitReason,
                _conclusion, GetHead());
        }

        int code;
        if (_ev == null || _ev.Infra > 0) code = 3;
        else if (_ev.Fail > 0 || _ev.Blocked > 0) code = 2;
        else code = 0;

        Debug.Log("[DIAG] finish; run=" + _run + "; exit=" + code +
                  "; pass=" + (_ev != null ? _ev.Pass : -1) +
                  "; fail=" + (_ev != null ? _ev.Fail : -1) +
                  "; blocked=" + (_ev != null ? _ev.Blocked : -1) +
                  "; infra=" + (_ev != null ? _ev.Infra : -1) +
                  "; observed=" + (_ev != null ? _ev.Observed : -1));

#if UNITY_EDITOR
        SessionState.SetBool(FinishedKey, true);
        SessionState.SetInt(ExitCodeKey, code);
        try
        {
            if (_ev != null)
                File.WriteAllText(_ev.ExitCodePath(), code.ToString() + Environment.NewLine,
                    new System.Text.UTF8Encoding(false));
        }
        catch (Exception e) { Debug.LogWarning("[DIAG] exit code file write failed: " + e.Message); }

        Destroy(gameObject);
        Time.timeScale = 1f;
        EditorApplication.isPlaying = false;
#else
        Time.timeScale = 1f;
#endif
    }

    // ============================================================
    // 工具
    // ============================================================
    static string GetHead()
    {
#if UNITY_EDITOR
        return SessionState.GetString("DIAG.Head", "unknown");
#else
        return "n/a";
#endif
    }

    static List<object> NamesOf(System.Array arr)
    {
        var list = new List<object>();
        if (arr == null) return list;
        for (int i = 0; i < arr.Length; i++)
        {
            var o = arr.GetValue(i) as UnityEngine.Object;
            list.Add(o != null ? o.name : "null");
        }
        return list;
    }

    static Dictionary<string, object> Dict(params object[] kvs)
    {
        var d = new Dictionary<string, object>();
        for (int i = 0; i + 1 < kvs.Length; i += 2)
            d[(string)kvs[i]] = kvs[i + 1];
        return d;
    }

    static Dictionary<string, object> V3(Vector3 v)
    {
        return new Dictionary<string, object> { { "x", v.x }, { "y", v.y }, { "z", v.z } };
    }
}
