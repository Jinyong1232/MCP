using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

/// design.md 10장 "검증 로그 수집"용 통계 기록기. 검증 의도①(재화 밸런스)·②(정보 제한) 관련 지표를
/// 모아뒀다가 스테이지 클리어/실패 시점에 CSV 한 줄로 기록한다. 기존 스크립트는 건드리지 않고
/// 각 매니저가 이미 갖고 있는 이벤트를 구독하는 방식으로만 동작한다.
public class PlayStatsLogger : MonoBehaviour
{
    [SerializeField] private StageManager stageManager;
    [SerializeField] private PathManager pathManager;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private CurrencyManager currencyManager;
    [SerializeField] private WallPlacementManager wallPlacementManager;

    // 스테이지1(튜토리얼)은 일시정지 개입이 많아 반응 시간 데이터가 오염되므로 측정에서 제외한다.
    // (design.md 10.3 "스테이지 1은 반응 시간 측정 대상에서 제외한다")
    [SerializeField] private bool excludeReactionTimeThisStage = false;

    // 비워두면 기존과 동일하게 로컬 CSV만 기록한다. 채워두면 같은 데이터를 구글 시트(Apps Script 웹앱)로도
    // 추가 전송한다 — 측정 항목·로컬 CSV 기록은 전혀 바뀌지 않고, 전송 경로만 하나 더 생기는 것.
    [SerializeField] private string sheetsWebAppUrl;

    private readonly Dictionary<MonsterTower, float> revealTimeByMonster = new Dictionary<MonsterTower, float>();
    private readonly HashSet<MonsterTower> blockedMonsters = new HashSet<MonsterTower>();
    private readonly HashSet<int> threatenedTiles = new HashSet<int>();
    private readonly HashSet<int> checkedPassedTiles = new HashSet<int>();
    private readonly List<float> reactionTimes = new List<float>();

    private int wallsPlacedCount;
    private int missedThreatTileCount;

    private void Start()
    {
        if (stageManager == null) stageManager = FindFirstObjectByType<StageManager>();
        if (pathManager == null) pathManager = FindFirstObjectByType<PathManager>();
        if (playerHealth == null) playerHealth = FindFirstObjectByType<PlayerHealth>();
        if (currencyManager == null) currencyManager = FindFirstObjectByType<CurrencyManager>();
        if (wallPlacementManager == null) wallPlacementManager = FindFirstObjectByType<WallPlacementManager>();

        if (stageManager != null)
        {
            stageManager.OnCleared += HandleCleared;
            stageManager.OnFailed += HandleFailed;
        }
        if (wallPlacementManager != null) wallPlacementManager.OnWallPlaced += HandleWallPlaced;
        if (pathManager != null) pathManager.OnPlayerTileIndexChanged += HandleTileIndexChanged;

        foreach (MonsterTower monster in FindObjectsByType<MonsterTower>(FindObjectsSortMode.None))
        {
            FogVisibility fog = monster.GetComponent<FogVisibility>();
            if (fog == null) continue;
            fog.OnRevealed += () => HandleMonsterRevealed(monster);
        }
    }

    private void HandleMonsterRevealed(MonsterTower monster)
    {
        if (revealTimeByMonster.ContainsKey(monster)) return;
        revealTimeByMonster[monster] = Time.time;

        if (pathManager == null) return;
        for (int i = 0; i < pathManager.TileCount; i++)
        {
            float dist = Vector3.Distance(pathManager.GetTilePosition(i), monster.transform.position);
            if (dist <= monster.AttackRange) threatenedTiles.Add(i);
        }
    }

    private void HandleWallPlaced(WallSlot slot)
    {
        wallsPlacedCount++;
        if (pathManager == null) return;

        Vector3 tilePos = pathManager.GetTilePosition(slot.TileIndex);
        foreach (KeyValuePair<MonsterTower, float> kvp in revealTimeByMonster)
        {
            MonsterTower monster = kvp.Key;
            if (monster == null || blockedMonsters.Contains(monster)) continue;

            float dist = Vector3.Distance(tilePos, monster.transform.position);
            if (dist <= monster.AttackRange)
            {
                reactionTimes.Add(Time.time - kvp.Value);
                blockedMonsters.Add(monster);
            }
        }
    }

    private void HandleTileIndexChanged(int newTileIndex)
    {
        int passedTile = newTileIndex - 1;
        if (passedTile < 0 || checkedPassedTiles.Contains(passedTile)) return;
        checkedPassedTiles.Add(passedTile);

        if (!threatenedTiles.Contains(passedTile)) return;
        if (pathManager.IsSlotOccupied(passedTile, SlotSide.Left) || pathManager.IsSlotOccupied(passedTile, SlotSide.Right)) return;

        missedThreatTileCount++;
    }

    private void HandleCleared() => WriteRow("CLEAR");
    private void HandleFailed() => WriteRow("GAME OVER");

    private void WriteRow(string result)
    {
        float remainingHealthPercent = (playerHealth != null && playerHealth.MaxHealth > 0f)
            ? (playerHealth.CurrentHealth / playerHealth.MaxHealth) * 100f
            : 0f;
        int remainingCurrency = currencyManager != null ? currencyManager.CurrentCurrency : 0;

        string avgReactionStr = "N/A";
        if (!excludeReactionTimeThisStage && reactionTimes.Count > 0)
        {
            float sum = 0f;
            foreach (float t in reactionTimes) sum += t;
            avgReactionStr = (sum / reactionTimes.Count).ToString("F2");
        }

        string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        string stageName = SceneManager.GetActiveScene().name;

        string path = Path.Combine(Application.dataPath, "..", "PlayStats.csv");
        bool isNewFile = !File.Exists(path);

        using (StreamWriter writer = new StreamWriter(path, append: true))
        {
            if (isNewFile)
            {
                writer.WriteLine("timestamp,stage,result,remainingCurrency,remainingHealthPercent,wallsPlaced,threatenedTileCount,avgReactionTimeSec,missedThreatTileCount");
            }

            writer.WriteLine($"{timestamp},{stageName},{result},{remainingCurrency},{remainingHealthPercent:F1},{wallsPlacedCount},{threatenedTiles.Count},{avgReactionStr},{missedThreatTileCount}");
        }

        Debug.Log($"[PlayStatsLogger] 기록 완료 → {path}");

        if (!string.IsNullOrEmpty(sheetsWebAppUrl))
        {
            StartCoroutine(SendToGoogleSheets(timestamp, stageName, result, remainingCurrency,
                remainingHealthPercent, wallsPlacedCount, threatenedTiles.Count, avgReactionStr, missedThreatTileCount));
        }
    }

    // 로컬 CSV와 완전히 같은 값을 구글 시트로도 보낸다(측정 항목·기록 로직은 그대로, 전송 경로만 추가).
    // 네트워크 실패해도 게임에는 영향 없게 예외를 던지지 않고 경고 로그만 남긴다.
    private IEnumerator SendToGoogleSheets(string timestamp, string stage, string result, int remainingCurrency,
        float remainingHealthPercent, int wallsPlaced, int threatenedTileCount, string avgReactionTimeSec, int missedThreatTileCount)
    {
        WWWForm form = new WWWForm();
        form.AddField("timestamp", timestamp);
        form.AddField("stage", stage);
        form.AddField("result", result);
        form.AddField("remainingCurrency", remainingCurrency);
        form.AddField("remainingHealthPercent", remainingHealthPercent.ToString("F1"));
        form.AddField("wallsPlaced", wallsPlaced);
        form.AddField("threatenedTileCount", threatenedTileCount);
        form.AddField("avgReactionTimeSec", avgReactionTimeSec);
        form.AddField("missedThreatTileCount", missedThreatTileCount);

        // Apps Script 웹앱은 POST를 처리한 뒤 결과 화면(googleusercontent.com)으로 항상 302 리다이렉트한다.
        // 그런데 UnityWebRequest가 그 리다이렉트를 자동으로 따라가려 하면 응답이 오지 않고 타임아웃으로
        // 멈춰버리는 문제가 있었다(실측 확인). 실제 시트 기록(doPost 실행)은 이 첫 POST 요청 시점에
        // 이미 끝나므로, 리다이렉트를 따라갈 필요가 없다 — redirectLimit=0으로 막고 302 자체를 성공으로 본다.
        using (UnityWebRequest request = UnityWebRequest.Post(sheetsWebAppUrl, form))
        {
            request.timeout = 8;
            request.redirectLimit = 0;
            yield return request.SendWebRequest();

            // redirectLimit=0이라 성공해도 "Redirect limit exceeded"라는 에러 문구가 함께 뜨는데, 이건
            // 정상이다(302 자체가 "요청을 잘 처리했다"는 신호) — responseCode로만 성공 여부를 판단한다.
            bool ok = request.responseCode == 200 || request.responseCode == 302;

            if (!ok)
            {
                Debug.LogWarning($"[PlayStatsLogger] 구글 시트 전송 실패(로컬 CSV는 정상 기록됨): {request.error} (code={request.responseCode})");
            }
            else
            {
                Debug.Log($"[PlayStatsLogger] 구글 시트 전송 완료 (code={request.responseCode})");
            }
        }
    }
}
