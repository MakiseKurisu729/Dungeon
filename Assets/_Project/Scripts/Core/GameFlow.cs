using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 队伍级流程：玩家单位死亡 → 倒地退出战斗；全员死亡 → 团灭冻结，R 重开。
/// </summary>
public class GameFlow : MonoBehaviour
{
    public static GameFlow instance;

    bool _wiped;
    int _playerTotal, _playerDead;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        foreach (var h in FindObjectsByType<UnitHealth>(FindObjectsSortMode.None))
        {
            if (h.team != Team.Player) continue;
            _playerTotal++;
            var captured = h;
            h.OnDeath += () => HandlePlayerDeath(captured);
        }
    }

    void HandlePlayerDeath(UnitHealth h)
    {
        _playerDead++;

        var brain = h.GetComponent<UnitBrain>();
        if (brain != null) brain.enabled = false;
        var agent = h.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null) agent.enabled = false;
        var sel = h.GetComponentInChildren<Selectable>();
        if (sel != null) sel.gameObject.SetActive(false);
        var anim = h.GetComponentInChildren<Animator>();
        if (anim != null) anim.Play("Death_A");

        Debug.Log("有角色倒下了！");
        if (_playerDead >= _playerTotal && !_wiped)
        {
            _wiped = true;
            Time.timeScale = 0f;
            GameData.SetMessage("全队覆没！按 R 重来");
        }
    }

    void Update()
    {
        if (_wiped && Input.GetKeyDown(KeyCode.R))
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}