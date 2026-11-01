using UnityEngine;

/// <summary>可交互标记。hint=悬停提示文字（检查器可配，每个交互物各写各的）；
/// 点击该物体 → 选中单位走过去执行其交互逻辑（如 DownedRescue 自动读条）。交互方式是鼠标点击，无按键。</summary>
public class Interactable : MonoBehaviour
{
    [Tooltip("悬停提示文字（仅显示用），如：点击救援倒地队友 / 点击开启宝箱")]
    public string hint = "点击交互";
}