using System.Collections;
using UnityEngine;
namespace Dungeon.Gameplay.Units {

    /// <summary>沉睡骷髅：初始摆死亡动画末帧（倒地不动、AI 冻结）。
    /// WakeUp() 播放起身动画，起身后恢复索敌。</summary>
    public class SleepingSkeleton : MonoBehaviour
    {
        [Tooltip("起身动画状态名（KayKit 起身剪辑，在 FBX 动画页确认名字）")]
        public string riseState = "Skeletons_Rise";
        [Tooltip("起身动画时长（秒，FBX 动画页读）")]
        public float riseDuration = 1.2f;
        [Tooltip("倒地摆 Pose 用的状态名")]
        public string deathState = "Skeletons_Death";

        MonsterBrain _brain;
        Animator _animator;

        void Start()
        {
            _brain = GetComponent<MonsterBrain>();
            _animator = GetComponentInChildren<Animator>();
            if (_animator != null) _animator.Play(deathState, 0, 1f);   // 末帧 = 倒地
            if (_brain != null) _brain.enabled = false;
        }

        public void WakeUp()
        {
            StartCoroutine(WakeRoutine());
        }

        IEnumerator WakeRoutine()
        {
            if (_animator != null) _animator.Play(riseState);
            yield return new WaitForSeconds(riseDuration);
            if (_brain != null) _brain.enabled = true;
        }
    }
}