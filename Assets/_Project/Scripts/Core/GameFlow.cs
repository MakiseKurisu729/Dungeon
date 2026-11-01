using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>队伍级流程：实时统计"仍站立的玩家数"，全员倒地 → 团灭冻结，R 重开。
/// 倒地表现/救援由 DownedRescue 负责（挂各单位上），本类不再管倒地动作。</summary>
public class GameFlow : MonoBehaviour
{
    public static GameFlow instance;
    public static bool GameStarted;

    public bool IsWiped { get; private set; }

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        GameStarted = false;
    }

    void Update()
    {
        if (IsWiped)
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                GameFlow.GameStarted = false;
                Time.timeScale = 1f;
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }
            return;
        }

        if (!GameStarted) return;

        bool anyStanding = false;
        foreach (var h in FindObjectsByType<UnitHealth>(FindObjectsSortMode.None))
        {
            if (h.team != Team.Player) continue;
            var dr = h.GetComponent<DownedRescue>();
            bool down = dr != null && dr.IsDown;
            if (!h.dead && !down) { anyStanding = true; break; }
        }
        if (!anyStanding)
        {
            IsWiped = true;
            Time.timeScale = 0f;
            GameData.SetMessage("全队覆没！按 R 重来");
        }
    }
}