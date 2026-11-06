using UnityEngine;
using UnityEngine.UI;
using Dungeon.Core;

/// <summary>HUD：金币/等级/消息 + 按 U 升级。血蓝条由头顶 WorldHealthBar 负责。</summary>
public class HUD : MonoBehaviour
{
    [InspectorName("金币文本")]
    public Text goldText;
    [InspectorName("等级文本")]
    public Text levelText;
    [InspectorName("消息文本")]
    public Text messageText;

    string _lastMessage;
    float _messageTimer;

    void Update()
    {
        if (goldText != null)
            goldText.text = $"金币：{RunState.Gold}（升级费用 {MetaState.LevelCost()}，按 U）";
        if (levelText != null)
            levelText.text = $"等级：{MetaState.Level}  击杀：{RunState.Kills}";

        if (messageText != null)
        {
            if (MessageFeed.Message != _lastMessage)
            {
                _lastMessage = MessageFeed.Message;
                _messageTimer = 2.5f;
                messageText.text = _lastMessage;
            }
            if (_messageTimer > 0f)
            {
                _messageTimer -= Time.deltaTime;
                if (_messageTimer <= 0f) messageText.text = "";
            }
        }

        if (Input.GetKeyDown(KeyCode.U))
        {
            var root = SelectionManager.instance != null && SelectionManager.instance.Primary != null
                ? SelectionManager.instance.Primary.transform.root : null;
            var uh = root != null ? root.GetComponent<UnitHealth>() : null;
            var sw = root != null ? root.GetComponent<MeleeCombat>() : null;

            if (MetaState.TryLevelUp(sw, uh))
                MessageFeed.SetMessage($"升级！现在 {MetaState.Level} 级");
            else
                MessageFeed.SetMessage("金币不足！");
        }
    }
}