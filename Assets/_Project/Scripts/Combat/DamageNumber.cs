using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 伤害数字：世界空间 Canvas + Text，向上飘、面向相机、渐隐销毁。
/// 用法：DamageNumber.Show(世界坐标, "25", Color.yellow);
/// </summary>
public class DamageNumber : MonoBehaviour
{
    public static void Show(Vector3 worldPos, string content, Color color)
    {
        var go = new GameObject("DamageNumber");
        go.transform.position = worldPos + Vector3.up * 0.25f;

        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        var rt = (RectTransform)go.transform;
        rt.sizeDelta = new Vector2(200f, 60f);
        rt.localScale = Vector3.one * 0.006f;   // 世界宽度约 1.2 米

        var tgo = new GameObject("txt");
        tgo.transform.SetParent(go.transform, false);
        var text = tgo.AddComponent<Text>();
        text.rectTransform.sizeDelta = new Vector2(200f, 60f);
        text.text = content;
        text.fontSize = 40;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = color;
        text.raycastTarget = false;

        go.AddComponent<DamageNumber>()._text = text;
    }

    Text _text;
    float _life = 0.8f;

    void Update()
    {
        transform.position += Vector3.up * (Time.deltaTime * 0.9f);
        if (Camera.main != null) transform.rotation = Camera.main.transform.rotation;  // 面向相机

        _life -= Time.deltaTime;
        if (_text != null)
        {
            var c = _text.color;
            c.a = Mathf.Clamp01(_life / 0.35f);
            _text.color = c;
        }
        if (_life <= 0f) Destroy(gameObject);
    }
}