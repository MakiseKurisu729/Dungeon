using UnityEngine;

/// <summary>点击优先级：救援必须先于“选中/攻击”分支（与重构前 SelectionManager.TryCommandRescue 的优先顺序一致）。</summary>
public enum InteractionPriority
{
    Default = 0,
    Rescue = 10
}

/// <summary>
/// 交互目标契约：目标只回答“落点在哪 / 还能不能交互 / 走到多近 / 做什么”，
/// 不再自己读 Input、不查 SelectionManager、不开关 UI。
/// 单位怎么走过去、到位后补播什么动作，由 <see cref="InteractionRunner"/> 负责。
/// </summary>
public interface IInteractionTarget
{
    /// <summary>落点基准（原 CharacterBrain.CommandInteract 里的 it.transform）。</summary>
    Transform Anchor { get; }

    /// <summary>提示文案（原 Interactable.hint；可空）。</summary>
    string Hint { get; }

    /// <summary>点击优先级：救援 = Rescue，其余 = Default。</summary>
    InteractionPriority Priority { get; }

    /// <summary>是否支持“靠近按 F 瞬发”（当前只有地牢门为 true）。</summary>
    bool KeyInteractable { get; }

    /// <summary>当前是否还能交互（已开宝箱 / 未倒地 / 已用门 = false）。</summary>
    bool IsAvailable(CharacterBrain unit);

    /// <summary>走到多近算到位（原 CharacterBrain.CommandInteract 的 r 计算迁到目标侧）。</summary>
    float Radius(CharacterBrain unit);

    /// <summary>执行（开箱 / 下达救援指令 / 进入地牢）。</summary>
    void Interact(CharacterBrain unit);
}
