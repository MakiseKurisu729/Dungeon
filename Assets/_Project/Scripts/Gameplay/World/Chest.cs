using System.Collections;
using UnityEngine;
using Dungeon.Content;
using Dungeon.Core;
using Dungeon.Gameplay.Interaction;
using Dungeon.Gameplay.Items;
using Dungeon.Gameplay.Units;
namespace Dungeon.Gameplay.World {

    /// <summary>
    /// 通用宝箱：实现 <see cref="IInteractionTarget"/>，由交互执行器走到范围后调用 Interact/OpenBy
    /// （开盖 + 内容物现身）。开启完成时抛 <see cref="Opened"/> 事件，面板由 UIManager 订阅打开，
    /// 宝箱本身不再直接调用 UI。
    /// </summary>
    public class Chest : MonoBehaviour, IInteractionTarget
    {
        [InspectorName("盖子")]
        public Transform lid;
        [InspectorName("开启角度")]
        public float openAngle = -110f;
        [InspectorName("开启时长")]
        public float openDuration = 0.4f;

        /// <summary>开箱完成（盖子播完、内容物已显形）。参数为本宝箱，订阅方据此取 Contents 开面板。</summary>
        public static event System.Action<Chest> Opened;

        bool _opened;
        Inventory _inv;
        Interactable _interact;

        /// <summary>宝箱背包（无 Inventory 组件时为 null）。</summary>
        public Inventory Contents { get { return _inv; } }

        void Awake()
        {
            _inv = GetComponent<Inventory>();
            _interact = GetComponent<Interactable>();
            foreach (var ci in GetComponentsInChildren<ChestItem>(true))
                ci.gameObject.SetActive(false);
        }

        void Start()
        {
            if (_inv != null)
            {
                _inv.items = new System.Collections.Generic.List<ItemData>(new ItemData[_inv.capacity]);
                foreach (var ci in GetComponentsInChildren<ChestItem>(true))
                    if (ci.data != null) _inv.Add(ci.data);
            }
        }

        /// <summary>角色走到交互范围后调用。</summary>
        public void OpenBy(CharacterBrain unit)
        {
            if (_opened) return;
            _opened = true;
            StartCoroutine(OpenSequence());
        }

        // ===== IInteractionTarget =====

        public Transform Anchor { get { return transform; } }

        public string Hint { get { return _interact != null ? _interact.hint : null; } }

        public InteractionPriority Priority { get { return InteractionPriority.Default; } }

        public bool KeyInteractable { get { return false; } }

        public bool IsAvailable(CharacterBrain unit) { return !_opened; }

        public float Radius(CharacterBrain unit) { return InteractionDefinition.RadiusFor(this, unit); }

        /// <summary>执行体 = 原 OpenBy（已开箱时为幂等空操作）。</summary>
        public void Interact(CharacterBrain unit) { OpenBy(unit); }

        IEnumerator OpenSequence()
        {
            if (lid != null)
            {
                Quaternion from = lid.localRotation;
                Quaternion to = from * Quaternion.Euler(openAngle, 0f, 0f);
                float t = 0f;
                while (t < openDuration)
                {
                    t += Time.deltaTime;
                    lid.localRotation = Quaternion.Slerp(from, to, Mathf.Clamp01(t / openDuration));
                    yield return null;
                }
                lid.localRotation = to;
            }

            foreach (var ci in GetComponentsInChildren<ChestItem>(true))
                ci.gameObject.SetActive(true);

            var onOpened = Opened;
            if (onOpened != null) onOpened(this);
        }

        public void Take(ItemData item)
        {
            foreach (var ci in GetComponentsInChildren<ChestItem>(false))
            {
                if (ci.data == item) { Destroy(ci.gameObject); break; }
            }
            if (item.goldValue > 0) RunState.AddGold(item.goldValue);
            if (_inv != null && _inv.items.Count == 0) MessageFeed.SetMessage("宝箱已取空");
        }
    }
}