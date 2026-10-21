using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;


/// 스테이지의 클리어/실패를 판정한다. Player가 마지막 타일에 도달하면 클리어,
/// PlayerHealth가 사망 이벤트를 발행하면 실패로 처리하고 결과 텍스트를 표시한다.
public class StageManager : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private PathManager pathManager;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private CurrencyManager currencyManager;
    [SerializeField] private WallPlacementManager wallPlacementManager;
    [SerializeField] private FogOfWarGround fogOfWarGround;
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private Button retryButton;
    [SerializeField] private ResultPanelUI resultPanelUI;
    [SerializeField] private ProgressBarUI progressBarUI;
    [SerializeField] private Button menuButton; // 실패 화면 전용 — 다시 도전 버튼 위에 항상 함께 보임


    [SerializeField] private float arrivalThreshold = 0.5f;
    // 클리어 시 넘어갈 다음 스테이지 씬 이름. 비워두면 "마지막 스테이지"로 취급해 메뉴로 돌아가는 버튼을 보여준다.
    [SerializeField] private string nextSceneName;
    [SerializeField] private string menuSceneName = "MainMenu";
    // 마지막 스테이지(nextSceneName 비어있음)를 클리어한 순간 자동으로 여는 설문 링크. 비워두면 안 열림.
    [SerializeField] private string surveyFormUrl;

    [Header("표시 문구 (자유롭게 수정 가능)")]
    [SerializeField] private string clearMessage = "CLEAR";
    [SerializeField] private string gameOverMessage = "GAME OVER";
    [SerializeField] private string gameClearMessage = "GAME CLEAR";
    [SerializeField] private string nextStageButtonLabel = "다음 스테이지";
    [SerializeField] private string retryButtonLabel = "다시 도전";
    [SerializeField] private string menuButtonLabel = "메뉴로 돌아가기";
    [SerializeField] private string failMenuButtonLabel = "메인 메뉴로";

    [Header("실패 화면 버튼 배치")]
    [SerializeField] private float retryButtonDefaultY = 46f;
    [SerializeField] private float retryButtonRaisedY = 130f;


    private bool isFinished;
    private float stageStartTime;
    // 타일 인덱스별 누적 피해량(U5 실패 화면의 "가장 피해를 많이 입은 구간" 표시용)
    private readonly Dictionary<int, float> damageByTile = new Dictionary<int, float>();


    // 클리어/실패가 확정되는 순간 발행(검증 로그 수집 등 외부 시스템이 구독). RetryStage로 다시 초기화된
    // 뒤 재도전해서 또 끝나면 다시 발행된다.
    public event System.Action OnCleared;
    public event System.Action OnFailed;
    // 재도전(RetryStage) 시작 시 발행 — 스킬 등 다른 시스템이 여기에 맞춰 자체 상태를 초기화한다.
    public event System.Action OnRetried;

private void OnEnable()
    {
        playerHealth.OnDied += HandleFail;
        playerHealth.OnDamaged += HandleDamaged;
    }

private void OnDisable()
    {
        playerHealth.OnDied -= HandleFail;
        playerHealth.OnDamaged -= HandleDamaged;
    }

private void Start()
    {
        if (currencyManager == null) currencyManager = FindFirstObjectByType<CurrencyManager>();
        if (wallPlacementManager == null) wallPlacementManager = FindFirstObjectByType<WallPlacementManager>();
        if (fogOfWarGround == null) fogOfWarGround = FindFirstObjectByType<FogOfWarGround>();
        if (resultPanelUI == null) resultPanelUI = FindFirstObjectByType<ResultPanelUI>();
        if (progressBarUI == null) progressBarUI = FindFirstObjectByType<ProgressBarUI>();

        if (retryButton != null) retryButton.gameObject.SetActive(false);
        if (menuButton != null)
        {
            menuButton.onClick.RemoveAllListeners();
            menuButton.onClick.AddListener(GoToMenu);
            TMP_Text label = menuButton.GetComponentInChildren<TMP_Text>();
            if (label != null) label.text = failMenuButtonLabel;
            menuButton.gameObject.SetActive(false);
        }
        stageStartTime = Time.time;
    }

    private void Update()
    {
        if (isFinished) return;
        if (pathManager.TileCount == 0) return;

        Vector3 goalPosition = pathManager.GetTilePosition(pathManager.TileCount - 1);
        if (Vector3.Distance(player.position, goalPosition) <= arrivalThreshold)
        {
            HandleClear();
        }
    }

private void HandleClear()
    {
        isFinished = true;
        if (string.IsNullOrEmpty(nextSceneName))
        {
            // 마지막 스테이지 — 메뉴로 돌아가는 버튼을 보여준다
            ShowResult(gameClearMessage, GoToMenu, menuButtonLabel);
            // 플레이테스트 설문 자동 연결 — 게임을 정말 끝까지 클리어한 시점에만 한 번 연다.
            if (!string.IsNullOrEmpty(surveyFormUrl)) Application.OpenURL(surveyFormUrl);
        }
        else
        {
            ShowResult(clearMessage, GoToNextStage, nextStageButtonLabel);
        }

        // 클리어/게임클리어 화면은 버튼 1개만 쓴다 — 기본 위치로 되돌리고 실패 전용 메뉴 버튼은 숨긴다.
        SetRetryButtonPosition(retryButtonDefaultY);
        if (menuButton != null) menuButton.gameObject.SetActive(false);

        if (resultPanelUI != null)
        {
            float curHealth = playerHealth != null ? playerHealth.CurrentHealth : 0f;
            float maxHealth = playerHealth != null ? playerHealth.MaxHealth : 1f;
            int wallsUsed = wallPlacementManager != null ? wallPlacementManager.WallsPlaced : 0;
            int wallsTotal = currencyManager != null ? currencyManager.TotalCurrency : 0;
            float clearTime = Time.time - stageStartTime;
            resultPanelUI.PlayClear(curHealth, maxHealth, wallsUsed, wallsTotal, clearTime);
        }

        OnCleared?.Invoke();

        // 클리어/실패 직후에도 근처 닭들이 계속 공격하는 게 어색하다는 피드백(2026-09-17) — 게임을 즉시 멈춘다.
        // 결과 화면 자체(카운트업·배경 페이드)는 전부 Time.unscaledDeltaTime 기반이라 이 정지의 영향을 안 받는다.
        // 이벤트 발행 "뒤"에 설정하는 이유: 혹시 이 순간 튜토리얼이 열려있었다면 그 강제 종료 처리(ClosePause)가
        // timeScale을 1로 되돌리므로, 그보다 나중에 0으로 덮어써야 최종적으로 확실히 멈춘 상태가 된다.
        Time.timeScale = 0f;
    }

private void HandleFail()
    {
        if (isFinished) return;
        isFinished = true;
        ShowResult(gameOverMessage, RetryStage, retryButtonLabel);

        // 실패 화면은 다시 도전 버튼을 위로 올리고, 그 아래에 메인 메뉴 버튼을 함께 보여준다.
        SetRetryButtonPosition(retryButtonRaisedY);
        if (menuButton != null) menuButton.gameObject.SetActive(true);

        float reachedPct = 0f;
        if (pathManager != null && pathManager.TileCount > 1)
        {
            reachedPct = (float)pathManager.GetCurrentPlayerTileIndex() / (pathManager.TileCount - 1);
        }
        if (resultPanelUI != null) resultPanelUI.PlayFail(reachedPct);

        if (progressBarUI != null && damageByTile.Count > 0 && pathManager != null && pathManager.TileCount > 1)
        {
            int worstTile = 0;
            float worstDamage = -1f;
            foreach (KeyValuePair<int, float> entry in damageByTile)
            {
                if (entry.Value > worstDamage)
                {
                    worstDamage = entry.Value;
                    worstTile = entry.Key;
                }
            }
            float worstPct = (float)worstTile / (pathManager.TileCount - 1);
            progressBarUI.ShowHitMarker(worstPct);
        }

        OnFailed?.Invoke();

        // 클리어와 동일하게 실패 직후에도 즉시 게임을 멈춘다(위 HandleClear() 주석 참고).
        Time.timeScale = 0f;
    }

private void ShowResult(string message, UnityEngine.Events.UnityAction buttonAction, string buttonLabel)
    {
        if (resultText != null)
        {
            resultText.text = message;
        }
        if (retryButton == null) return;

        if (buttonAction == null)
        {
            retryButton.gameObject.SetActive(false);
            return;
        }

        retryButton.onClick.RemoveAllListeners();
        retryButton.onClick.AddListener(buttonAction);
        TMP_Text label = retryButton.GetComponentInChildren<TMP_Text>();
        if (label != null) label.text = buttonLabel;
        retryButton.gameObject.SetActive(true);
    }

    private void GoToNextStage()
    {
        // Time.timeScale은 씬을 넘어가도 유지되는 전역 값이라, 클리어/실패로 멈춰둔 0을 그대로 안 풀면
        // 다음 스테이지가 처음부터 멈춘 채로 시작해버린다.
        Time.timeScale = 1f;
        SceneManager.LoadScene(nextSceneName);
    }

    private void GoToMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(menuSceneName);
    }

// retryButton의 Y 좌표만 바꿔 실패/클리어 화면에서 높이를 다르게 배치한다.
    private void SetRetryButtonPosition(float y)
    {
        if (retryButton == null) return;
        RectTransform rt = retryButton.GetComponent<RectTransform>();
        if (rt == null) return;
        rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, y);
    }


// PlayerHealth.OnDamaged 구독 — 피격 시점의 현재 타일에 피해량을 누적해 "가장 위험했던 구간"을 추적한다.
    private void HandleDamaged(float amount)
    {
        int tileIndex = pathManager != null ? pathManager.GetCurrentPlayerTileIndex() : 0;
        damageByTile.TryGetValue(tileIndex, out float existing);
        damageByTile[tileIndex] = existing + amount;
    }


    // 재도전 — 재화 총량·몬스터 배치(시야/사거리 표시 상태 포함)·경로를 모두 시작 상태로 되돌린다. (design.md 3.8)
// 재도전 — 재화 총량·몬스터 배치(시야/사거리 표시 상태 포함)·경로를 모두 시작 상태로 되돌린다. (design.md 3.8)
// 재도전 — 재화 총량·몬스터 배치(시야/사거리 표시 상태 포함)·경로를 모두 시작 상태로 되돌린다. (design.md 3.8)
    public void RetryStage()
    {
        isFinished = false;
        Time.timeScale = 1f; // 클리어/실패로 멈춰둔 게임을 재도전 시 반드시 풀어준다.
        if (resultPanelUI != null) resultPanelUI.HideAll();
        if (retryButton != null) retryButton.gameObject.SetActive(false);
        if (menuButton != null) menuButton.gameObject.SetActive(false);
        SetRetryButtonPosition(retryButtonDefaultY);
        if (progressBarUI != null) progressBarUI.HideHitMarker();
        damageByTile.Clear();
        stageStartTime = Time.time;

        if (wallPlacementManager != null) wallPlacementManager.ClearAllWalls();
        if (pathManager != null) pathManager.ResetProgress();
        if (currencyManager != null) currencyManager.ResetCurrency();
        if (fogOfWarGround != null) fogOfWarGround.ResetFog();

        if (playerHealth != null) playerHealth.ResetHealth();
        PlayerAutoMover autoMover = player != null ? player.GetComponent<PlayerAutoMover>() : null;
        if (autoMover != null) autoMover.ResetMovement();

        foreach (FogVisibility fog in FindObjectsByType<FogVisibility>(FindObjectsSortMode.None))
        {
            fog.ResetVisibility();
        }
        foreach (AttackRangeIndicator indicator in FindObjectsByType<AttackRangeIndicator>(FindObjectsSortMode.None))
        {
            indicator.ResetIndicator();
        }
        foreach (RockfallTrap trap in FindObjectsByType<RockfallTrap>(FindObjectsSortMode.None))
        {
            trap.ResetTrap();
        }
        foreach (SpikeTrap trap in FindObjectsByType<SpikeTrap>(FindObjectsSortMode.None))
        {
            trap.ResetTrap();
        }

        // 다른 시스템(스킬 등)에 재도전을 알리는 건 맨 마지막 — 그래야 그쪽에서 CanUse 등을 다시 계산할 때
        // 위에서 이미 끝난 체력 회복(IsDead=false) 등 최신 상태를 보고 판단한다.
        OnRetried?.Invoke();
    }
}
