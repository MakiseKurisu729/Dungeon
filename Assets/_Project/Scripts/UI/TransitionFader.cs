// ⑥ TransitionFader.cs
using System.Collections;
using UnityEngine;
using Dungeon.Core;
using Dungeon.Gameplay.World;
namespace Dungeon.UI {

    /// <summary>过场黑屏：黑屏淡入 → 执行传送 → 亮屏淡出。Canvas 上挂，配全屏黑 Image + CanvasGroup。</summary>
    public class TransitionFader : MonoBehaviour
    {
        public static TransitionFader instance;
        public CanvasGroup canvasGroup;
        public float fadeTime = 0.9f;

        void Awake()
        {
            instance = this;
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
        }

        void OnEnable() { EventBus.Subscribe<TransitionRequest>(OnTransitionRequest); }

        void OnDisable() { EventBus.Unsubscribe<TransitionRequest>(OnTransitionRequest); }

        void OnTransitionRequest(TransitionRequest req) { Transition(req.midAction); }

        public static void Transition(System.Action midAction)
        {
            if (instance != null) instance.StartCoroutine(instance.Run(midAction));
        }

        IEnumerator Run(System.Action mid)
        {
            canvasGroup.blocksRaycasts = true;
            float t = 0f;
            while (t < fadeTime) { t += Time.deltaTime; canvasGroup.alpha = Mathf.Clamp01(t / fadeTime); yield return null; }
            mid?.Invoke();
            yield return null;
            t = 0f;
            while (t < fadeTime) { t += Time.deltaTime; canvasGroup.alpha = 1f - Mathf.Clamp01(t / fadeTime); yield return null; }
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
        }
    }
}