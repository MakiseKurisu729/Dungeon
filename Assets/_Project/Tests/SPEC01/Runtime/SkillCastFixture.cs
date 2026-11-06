using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>一次施法探针的原始观测值（不做断言判定，判定由调用方按工单口径完成）。</summary>
public class CastProbeResult
{
    public int slot = -1;
    public string slotName = "none";
    public string effectType = "none";

    public bool castReturn;
    public string exception;

    public float manaBefore = -1f;
    public float manaAfter = -1f;
    public float manaDelta;

    public float cdRemaining = -1f;
    public float cdTotal = -1f;
    public float swordTimer = -2f;

    public string animStateBefore = "none";
    public float animPhaseBefore = -1f;
    public float animNormalizedBefore = -1f;
    public bool animEntered;
    public float animEntrySeconds = -1f;
    public int animPollFrames;
    public string animStateAfter = "none";
    public float animPhaseAfter = -1f;
    public float animNormalizedAfter = -1f;
    public bool animInTransitionAfter;

    public float targetHpBefore = -1f;
    public float targetHpAfter = -1f;
    public float expectedDamage = -1f;
    public float damageApplied;
    public bool damageObserved;
    public float damageWaitSeconds = -1f;

    public Dictionary<string, object> Data()
    {
        return new Dictionary<string, object>
        {
            { "slot", slot },
            { "slotName", slotName },
            { "effectType", effectType },
            { "castReturn", castReturn },
            { "exception", exception },
            { "manaBefore", manaBefore },
            { "manaAfter", manaAfter },
            { "manaDelta", manaDelta },
            { "cdRemaining", cdRemaining },
            { "cdTotal", cdTotal },
            { "swordTimer", swordTimer },
            { "animStateBefore", animStateBefore },
            { "animPhaseBefore", animPhaseBefore },
            { "animNormalizedBefore", animNormalizedBefore },
            { "animEntered", animEntered },
            { "animEntrySeconds", animEntrySeconds },
            { "animPollFrames", animPollFrames },
            { "animStateAfter", animStateAfter },
            { "animPhaseAfter", animPhaseAfter },
            { "animNormalizedAfter", animNormalizedAfter },
            { "animInTransitionAfter", animInTransitionAfter },
            { "targetHpBefore", targetHpBefore },
            { "targetHpAfter", targetHpAfter },
            { "expectedDamage", expectedDamage },
            { "damageApplied", damageApplied },
            { "damageObserved", damageObserved },
            { "damageWaitSeconds", damageWaitSeconds }
        };
    }
}

/// <summary>
/// 施法夹具（SPEC01 常驻共用）：
/// 在 Play 模式内存中实例化 Knight_Unit 预制体 + 造一个怪物靶，
/// 提供「准备到 Locomotion 稳态」与「逐槽施放并采样」的协程；结束前销毁，不写场景。
///
/// 关键点：
/// - 用「非激活父物体」承接预制体实例，先禁用 CharacterBrain / NavMeshAgent，再激活；
///   避免无 NavMesh 环境下 NavMeshAgent 在 Awake 里刷 Console 错误（SPEC01 的零错误断言会受影响）。
/// - Bash 进入过渡带 HasExitTime=0.732（PlayerBase.controller），因此起手前等 Locomotion
///   相位进入 [0.74, 0.97]，确保「0.5s 内进入 Bash」是产品行为而不是动画相位运气。
/// </summary>
public class SkillCastFixture : IDisposable
{
    public const string KnightPrefabPath = "Assets/_Project/Prefabs/Knight_Unit.prefab";

    public GameObject Holder;
    public GameObject CasterGo;
    public GameObject TargetGo;
    public UnitHealth Target;
    public UnitStats TargetStats;

    public SkillSystem Caster;
    public UnitMana Mana;
    public MeleeCombat Sword;
    public Animator Animator;

    public string Error;
    public float PrepareSeconds = -1f;
    public int PrepareFrames;
    public string PrepareState = "none";
    public float PreparePhase = -1f;
    public bool PrepareLocomotion;
    public float PhaseSeconds = -1f;

    static FieldInfo _swordTimerField;

    public static SkillCastFixture Create(Vector3 origin, bool createTarget)
    {
        var fx = new SkillCastFixture();
        GameObject prefab = null;
#if UNITY_EDITOR
        try { prefab = AssetDatabase.LoadAssetAtPath<GameObject>(KnightPrefabPath); }
        catch (Exception e) { fx.Error = e.GetType().Name + ": " + e.Message; }
#else
        fx.Error = "非编辑器环境无法加载预制体";
#endif
        if (prefab == null)
        {
            if (string.IsNullOrEmpty(fx.Error)) fx.Error = "无法加载 " + KnightPrefabPath;
            return fx;
        }

        try
        {
            var holder = new GameObject("DIAG_SkillFixtureHolder");
            holder.transform.position = Vector3.zero;
            holder.SetActive(false);
            fx.Holder = holder;

            var go = UnityEngine.Object.Instantiate(prefab, origin, Quaternion.identity, holder.transform);
            go.name = "DIAG_SkillFixture_Knight";
            fx.CasterGo = go;

            var brain = go.GetComponent<CharacterBrain>();
            if (brain != null) brain.enabled = false;
            var agent = go.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent != null) agent.enabled = false;

            fx.Caster = go.GetComponent<SkillSystem>();
            fx.Mana = go.GetComponent<UnitMana>();
            fx.Sword = go.GetComponent<MeleeCombat>();
            fx.Animator = go.GetComponentInChildren<Animator>(true);

            if (fx.Caster == null || fx.Mana == null || fx.Animator == null)
            {
                fx.Error = "夹具缺少 SkillSystem/UnitMana/Animator";
                fx.Dispose();
                return fx;
            }

            if (createTarget) fx.CreateTarget(origin + new Vector3(0f, 0f, 1.8f));

            holder.SetActive(true);
        }
        catch (Exception e)
        {
            fx.Error = e.GetType().Name + ": " + e.Message;
            fx.Dispose();
        }
        return fx;
    }

    void CreateTarget(Vector3 pos)
    {
        var go = new GameObject("DIAG_SkillTarget");
        go.SetActive(false);
        go.transform.position = pos;

        TargetStats = ScriptableObject.CreateInstance<UnitStats>();
        TargetStats.team = Team.Monster;
        TargetStats.maxHp = 1000f;
        TargetStats.damage = 0f;
        TargetStats.attackRange = 0f;
        TargetStats.preferredRange = 0f;
        TargetStats.aggroRange = 0f;
        TargetStats.moveSpeed = 0f;
        TargetStats.attackSpeed = 1f;

        var uh = go.AddComponent<UnitHealth>();
        uh.stats = TargetStats;
        uh.showDamageNumber = false;

        var col = go.AddComponent<CapsuleCollider>();
        col.center = new Vector3(0f, 0.9f, 0f);
        col.height = 1.8f;
        col.radius = 0.4f;

        go.SetActive(true);
        TargetGo = go;
        Target = uh;
    }

    /// <summary>等待 UnitMana.Start → 设 Locomotion 参数 → 等进入 Locomotion → 等到出口相位。</summary>
    public IEnumerator Prepare(float timeout)
    {
        float t = 0f;
        int frames = 0;
        while (t < timeout && (Mana == null || Mana.current <= 0.01f))
        {
            t += Time.unscaledDeltaTime;
            frames++;
            yield return null;
        }

        ApplyLocomotionParams();

        // batchmode 无渲染输出时，远离相机的夹具可能被 Animator 剔除而完全不推进；
        // 夹具强制 AlwaysAnimate，保证观测到的是产品状态机而不是渲染剔除。
        if (Animator != null) Animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

        yield return EnsureLocomotion(timeout);
        yield return WaitExitPhase(8f);

        PrepareSeconds = t;
        PrepareFrames = frames;
        PrepareState = State();
        PreparePhase = Phase();
        PrepareLocomotion = IsState("Locomotion");
    }

    void ApplyLocomotionParams()
    {
        if (Animator == null) return;
        Animator.SetBool("Grounded", true);
        Animator.SetFloat("SpeedX", 0f);
        Animator.SetFloat("SpeedZ", 0f);
        float asp = 1f;
        if (Sword != null && Sword.stats != null && Sword.stats.attackSpeed > 0f) asp = Sword.stats.attackSpeed;
        Animator.SetFloat("AttackSpeed", asp);
    }

    public IEnumerator EnsureLocomotion(float timeout)
    {
        float t = 0f;
        while (t < timeout && !IsState("Locomotion"))
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    IEnumerator WaitExitPhase(float timeout)
    {
        float t = 0f;
        PhaseSeconds = -1f;
        while (t < timeout)
        {
            if (IsState("Locomotion"))
            {
                float ph = Phase();
                // Bash 进入过渡 HasExitTime=0.732：必须让状态"跨过"出口时刻才会触发，
                // 因此起手相位取 [0.65,0.72]，保证 SetTrigger 后 ~0.15 个状态下周期内跨线。
                if (ph >= 0.65f && ph <= 0.72f) { PhaseSeconds = t; yield break; }
            }
            t += Time.unscaledDeltaTime;
            yield return null;
        }
        PhaseSeconds = t;
    }

    /// <summary>
    /// 施放一个槽位并采样：同帧蓝量/CD、动画进入耗时、Bash 命中帧后的靶血量。
    /// expectedState 为空则不做动画等待；non-Bash 效果不做伤害等待。
    /// </summary>
    public IEnumerator Cast(int slot, Transform target, string expectedState, float animTimeout, CastProbeResult r)
    {
        r.slot = slot;
        var skill = Caster != null ? Caster.GetSkill(slot) : null;
        r.slotName = skill != null ? skill.name : "<empty>";
        r.effectType = skill != null && skill.effects != null && skill.effects.Length > 0 && skill.effects[0] != null
            ? skill.effects[0].GetType().Name : "none";

        r.animStateBefore = State();
        r.animPhaseBefore = Phase();
        r.animNormalizedBefore = Normalized();
        r.targetHpBefore = Target != null ? Target.hp : -1f;
        r.manaBefore = Mana != null ? Mana.current : -1f;

        try { r.castReturn = Caster != null && Caster.Cast(slot, target); }
        catch (Exception e) { r.exception = e.GetType().Name + ": " + e.Message; }

        r.manaAfter = Mana != null ? Mana.current : -1f;
        r.manaDelta = r.manaBefore - r.manaAfter;
        r.cdRemaining = Caster != null ? Caster.CdRemaining(slot) : -1f;
        r.cdTotal = Caster != null ? Caster.CdTotal(slot) : -1f;
        r.swordTimer = GetSwordTimer();

        if (!string.IsNullOrEmpty(expectedState))
        {
            float t = 0f;
            int frames = 0;
            while (t < animTimeout)
            {
                if (IsState(expectedState)) { r.animEntered = true; r.animEntrySeconds = t; break; }
                t += Time.unscaledDeltaTime;
                frames++;
                yield return null;
            }
            r.animPollFrames = frames;
            if (!r.animEntered) r.animEntrySeconds = -1f;
            r.animStateAfter = State();
            r.animPhaseAfter = Phase();
            r.animNormalizedAfter = Normalized();
            r.animInTransitionAfter = Animator != null && Animator.IsInTransition(0);
        }

        var bash = skill != null && skill.effects != null && skill.effects.Length > 0
            ? skill.effects[0] as BashEffect : null;
        if (bash != null && target != null && r.castReturn)
        {
            r.expectedDamage = Sword != null ? Sword.GetRawDamage() : -1f;
            float wait = bash.hitTime + 0.4f;
            float t = 0f;
            while (t < wait) { t += Time.unscaledDeltaTime; yield return null; }
            r.damageWaitSeconds = t;
            if (Target != null)
            {
                r.targetHpAfter = Target.hp;
                r.damageApplied = r.targetHpBefore - r.targetHpAfter;
                r.damageObserved = r.damageApplied > 0f;
            }
        }
        else if (Target != null)
        {
            r.targetHpAfter = Target.hp;
        }
    }

    public bool IsState(string stateName)
    {
        return Animator != null && Animator.GetCurrentAnimatorStateInfo(0).IsName(stateName);
    }

    public string State()
    {
        if (Animator == null) return "no_animator";
        var st = Animator.GetCurrentAnimatorStateInfo(0);
        string[] known = { "Locomotion", "AttackMelee", "Attack_Draw", "Attack_Release", "Hit_A", "Q_1", "W_1", "E_1", "R_1", "Interact", "Downed", "Rise" };
        for (int i = 0; i < known.Length; i++)
            if (st.IsName(known[i])) return known[i];
        return "Other:" + st.shortNameHash;
    }

    public float Phase()
    {
        if (Animator == null) return -1f;
        float n = Animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
        if (n < 0f) return -1f;
        return n - Mathf.Floor(n);
    }

    public float Normalized()
    {
        if (Animator == null) return -1f;
        return Animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
    }

    public Dictionary<string, object> AnimatorInfo()
    {
        if (Animator == null) return new Dictionary<string, object> { { "animator", "null" } };
        return new Dictionary<string, object>
        {
            { "enabled", Animator.enabled },
            { "isActiveAndEnabled", Animator.isActiveAndEnabled },
            { "cullingMode", Animator.cullingMode.ToString() },
            { "updateMode", Animator.updateMode.ToString() },
            { "speed", Animator.speed },
            { "layerCount", Animator.layerCount },
            { "controller", Animator.runtimeAnimatorController != null ? Animator.runtimeAnimatorController.name : "none" }
        };
    }

    public string CasterPath()
    {
        return CasterGo != null ? CasterGo.name : "none";
    }

    float GetSwordTimer()
    {
        if (Sword == null) return -2f;
        if (_swordTimerField == null)
            _swordTimerField = typeof(MeleeCombat).GetField("_timer", BindingFlags.Instance | BindingFlags.NonPublic);
        if (_swordTimerField == null) return -2f;
        object v = _swordTimerField.GetValue(Sword);
        return v is float f ? f : -2f;
    }

    public void Dispose()
    {
        if (TargetGo != null) UnityEngine.Object.Destroy(TargetGo);
        TargetGo = null;
        if (TargetStats != null) UnityEngine.Object.Destroy(TargetStats);
        TargetStats = null;
        if (Holder != null) UnityEngine.Object.Destroy(Holder);
        Holder = null;
        CasterGo = null;
        Target = null;
        Caster = null;
        Mana = null;
        Sword = null;
        Animator = null;
    }
}
