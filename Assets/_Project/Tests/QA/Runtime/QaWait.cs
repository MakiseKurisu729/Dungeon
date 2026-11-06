using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 协程等待/轮询：全部以 Time.deltaTime 累加超时（与原各套件实现同口径），超时静默返回。
/// 只做等待，不做断言、不写证据。
/// </summary>
public static class QaWait
{
    /// <summary>等到 animator 的 layer 0 状态名等于 stateName，超时静默返回。</summary>
    public static IEnumerator WaitState(Animator animator, string stateName, float timeout)
    {
        float t = 0f;
        while (t < timeout)
        {
            if (animator != null && QaFixtures.StateName(animator) == stateName) yield break;
            yield return null;
            t += Time.deltaTime;
        }
    }

    /// <summary>等到所有 animator 的 layer 0 状态名都等于 stateName（任一为 null 视为未满足），超时静默返回。</summary>
    public static IEnumerator WaitAllStates(IEnumerable<Animator> animators, string stateName, float timeout)
    {
        float t = 0f;
        while (t < timeout)
        {
            if (AllStates(animators, stateName)) yield break;
            yield return null;
            t += Time.deltaTime;
        }
    }

    /// <summary>等到条件成立，超时静默返回。</summary>
    public static IEnumerator WaitUntil(Func<bool> condition, float timeout)
    {
        float t = 0f;
        while (t < timeout)
        {
            if (condition != null && condition()) yield break;
            yield return null;
            t += Time.deltaTime;
        }
    }

    /// <summary>按 Time.deltaTime 累加等待 seconds 秒。</summary>
    public static IEnumerator WaitSeconds(float seconds)
    {
        yield return WaitUntil(null, seconds);
    }

    /// <summary>等待固定帧数上限（只 yield 帧，不设超时）。</summary>
    public static IEnumerator WaitFrames(int frames)
    {
        for (int i = 0; i < frames; i++) yield return null;
    }

    /// <summary>等到场景里没有活跃的伤害飘字，超时静默返回。</summary>
    public static IEnumerator WaitNoNumbers(float timeout)
    {
        yield return WaitUntil(
            () => UnityEngine.Object.FindObjectsByType<DamageNumber>(
                FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length == 0,
            timeout);
    }

    static bool AllStates(IEnumerable<Animator> animators, string stateName)
    {
        if (animators == null) return false;
        foreach (var a in animators)
        {
            if (a == null || QaFixtures.StateName(a) != stateName) return false;
        }
        return true;
    }
}
