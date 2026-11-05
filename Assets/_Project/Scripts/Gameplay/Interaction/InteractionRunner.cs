using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 单位侧交互执行器：与 <see cref="CharacterBrain"/> 同物体，由 <see cref="Ensure"/> 在运行时按需挂载
/// （不写预制体/场景，零资产改动）。
/// 职责 = 原 CharacterBrain 的“算落点 + 走过去 + 记住交互待办 + 到位补播 Interact 并执行”，
/// 外加输入层要的“范围内 F 交互 / 提示轮询”。
/// </summary>
public sealed class InteractionRunner : MonoBehaviour
{
    /// <summary>支持“靠近按 F”的目标（KeyInteractable=true）在 OnEnable/OnDisable 里登记/注销。
    /// 输入层只遍历这张表，不按具体类型分支，新增交互物不需要改输入层。</summary>
    static readonly List<IInteractionTarget> KeyTargets = new List<IInteractionTarget>();

    CharacterBrain _brain;
    IInteractionTarget _pending;

    /// <summary>已存在则复用，否则 AddComponent（运行时组件，不落盘）。</summary>
    public static InteractionRunner Ensure(CharacterBrain brain)
    {
        if (brain == null) return null;
        var runner = brain.GetComponent<InteractionRunner>();
        if (runner == null) runner = brain.gameObject.AddComponent<InteractionRunner>();
        runner._brain = brain;
        return runner;
    }

    /// <summary>登记“可控 F 交互”的目标（仅 KeyInteractable=true 的目标需要调用）。</summary>
    public static void RegisterKeyTarget(IInteractionTarget target)
    {
        if (target == null || KeyTargets.Contains(target)) return;
        KeyTargets.Add(target);
    }

    /// <summary>注销；目标销毁时由 OnDisable 调用。重复注销安全。</summary>
    public static void UnregisterKeyTarget(IInteractionTarget target)
    {
        if (target == null) return;
        KeyTargets.Remove(target);
    }

    /// <summary>是否还有“走到交互物后补一次 Interact”的待办。</summary>
    public bool HasPending { get { return _pending != null; } }

    /// <summary>原 CharacterBrain.CommandInteract：算落点 + CommandMove + 记待办。
    /// 技能期间交互指令无效（门禁与原 CommandInteract 相同，由 CharacterBrain 判定）。</summary>
    public void Order(IInteractionTarget target)
    {
        if (target == null || _brain == null) return;
        if (!_brain.CanAcceptInteract) return;

        var anchor = target.Anchor;
        if (anchor == null) return;

        float r = target.Radius(_brain);
        Vector3 d = _brain.transform.position - anchor.position; d.y = 0f;
        if (d.sqrMagnitude < 0.01f) d = Vector3.forward;
        _brain.CommandMove(anchor.position + d.normalized * r);
        _pending = target;
    }

    /// <summary>撤销“走到交互物落点后自动补播一次 Interact”的待办（原 CancelPendingInteract）。</summary>
    public void CancelPending() { _pending = null; }

    /// <summary>供输入层 F 键：范围内最近的可 KeyInteractable 目标 → 立即执行。没目标返回 false。</summary>
    public bool TryKeyInteract()
    {
        var target = NearestKeyTarget();
        if (target == null) return false;
        target.Interact(_brain);
        return true;
    }

    /// <summary>供输入层轮询：范围内最近的可 KeyInteractable 目标提示（无则 null）。</summary>
    public string PollKeyHint()
    {
        var target = NearestKeyTarget();
        return target != null ? target.Hint : null;
    }

    /// <summary>到达检测（等价原 CharacterBrain.Update 内联逻辑）：上一帧在走、这一帧停了、且有交互待办
    /// → 补播一次 Interact 并执行目标。调用方负责在本帧返回后跳过追击/普攻决策。</summary>
    public void OnMovedTick(bool wasMoving, bool isMoving)
    {
        if (_pending == null) return;
        if (!wasMoving || isMoving) return;

        var target = _pending;
        _pending = null;                       // 先清待办：目标执行里可能再次 Order（如救援）
        if (_brain != null && _brain.unitAnimator != null) _brain.unitAnimator.SetTrigger("Interact");
        target.Interact(_brain);
    }

    /// <summary>最近的可 F 交互目标：3D 距离（与重构前 DungeonDoor.Update 的距离口径一致）且在自身半径内。</summary>
    IInteractionTarget NearestKeyTarget()
    {
        if (_brain == null) return null;
        var p = _brain.transform.position;
        IInteractionTarget best = null;
        float bestDistance = float.MaxValue;

        for (int i = KeyTargets.Count - 1; i >= 0; i--)
        {
            var target = KeyTargets[i];
            var component = target as Component;
            if (component == null) { KeyTargets.RemoveAt(i); continue; }   // 已销毁的残留

            if (!target.KeyInteractable) continue;
            var anchor = target.Anchor;
            if (anchor == null) continue;
            if (!target.IsAvailable(_brain)) continue;

            float distance = Vector3.Distance(p, anchor.position);
            if (distance > target.Radius(_brain)) continue;
            if (distance >= bestDistance) continue;
            bestDistance = distance;
            best = target;
        }
        return best;
    }
}
