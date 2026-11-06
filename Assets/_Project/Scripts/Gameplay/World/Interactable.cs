using UnityEngine;
using Dungeon.Gameplay.Interaction;
namespace Dungeon.Gameplay.World {

    /// <summary>可交互标记 + 悬停提示文字（检查器可配，每个交互物各写各的）。
    /// 只承担“点击命中标记 + hint 文案”，交互语义由目标自己实现的 <see cref="IInteractionTarget"/> 提供；
    /// 输入层命中后经 <see cref="Target"/> 拿到目标契约。交互方式是鼠标点击，门额外支持靠近按 F。</summary>
    public class Interactable : MonoBehaviour
    {
        [Tooltip("悬停提示文字（仅显示用），如：点击救援倒地队友 / 点击开启宝箱")]
        public string hint = "点击交互";

        /// <summary>配套的交互目标（同物体优先，其次父级）。没有实现接口时返回 null。</summary>
        public IInteractionTarget Target()
        {
            var target = GetComponent<IInteractionTarget>();
            return target != null ? target : GetComponentInParent<IInteractionTarget>();
        }
    }
}
