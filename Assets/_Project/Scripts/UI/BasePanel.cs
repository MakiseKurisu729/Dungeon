using UnityEngine;

/// <summary>
/// 面板基类（三段生命周期）：
/// OnInit = 首次打开前一次（缓存引用）；OnOpen = 每次打开（收本次参数）；OnClose = 每次关闭（清本次状态）。
/// 面板自身禁止调用 SetActive——显隐 100% 归 UIManager 驱动；场景里面板物体保持激活（隐藏由代码做）。
/// </summary>
public abstract class BasePanel : MonoBehaviour
{
    bool _inited;
    public bool IsOpen { get; private set; }
    public int SerialId { get; private set; }   // 本次打开的序号（单调递增，调试/对账用）

    public void Open(int serialId, object userData)
    {
        if (IsOpen) return;
        if (!_inited) { _inited = true; OnInit(); }
        IsOpen = true;
        SerialId = serialId;
        Debug.Log($"[BasePanel] {name}.Open → SetActive(true)，之前 activeSelf={gameObject.activeSelf}");
        gameObject.SetActive(true);
        OnOpen(userData);
    }

    public void Close()
    {
        if (!IsOpen) return;
        OnClose();
        IsOpen = false;
        Debug.Log($"[BasePanel] {name}.Close → SetActive(false)");
        gameObject.SetActive(false);
    }

    protected virtual void OnInit() { }
    protected virtual void OnOpen(object userData) { }
    protected virtual void OnClose() { }
}