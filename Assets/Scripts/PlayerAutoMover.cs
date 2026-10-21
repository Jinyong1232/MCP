using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// Player 자동 이동. 입력 없이 경로 타일을 순서대로 목적지로 삼아 이동한다.
[RequireComponent(typeof(NavMeshAgent))]
public class PlayerAutoMover : MonoBehaviour
{
    [SerializeField] private PathManager pathManager;
    // 타일에 실제로 "도달했다"고 게임 로직(진행바·튜토리얼·가시함정 등)에 보고하는 기준 거리.
    // 아래 steeringLookahead와 분리되어 있어, 코너를 매끄럽게 도는 것과 무관하게 항상 같은 시점에 발동한다.
    [SerializeField] private float arrivalThreshold = 0.15f;
    // NavMeshAgent가 다음 타일로 목적지를 미리 바꾸는 "반응 시간"(코너를 매끄럽게 돌기 위한 순수 이동 보정용).
    // 고정 거리가 아니라 시간으로 두고 매 프레임 현재 속도를 곱해서 실제 미리보기 거리를 구한다 — 고정 거리였을 때는
    // 가속 스킬 등으로 속도가 빨라지면 코너 앞에서 방향을 트는 데 쓸 수 있는 시간이 그만큼 줄어들어(거리/속도),
    // 코너에서 경로를 크게 벗어나며 휘청이는 문제가 있었다(예: 기본 속도에서는 거의 안 벗어났는데 가속 2배 시
    // 반경 4유닛 가까이 벗어나는 것을 실측으로 확인). 시간 기준으로 두면 속도가 몇 배가 되든 반응 시간이 동일하게
    // 유지돼 코너를 도는 정도가 일정하다.
    [SerializeField] private float steeringLookaheadTime = 0.15f;
    // 속도가 0에 가까워도(예: 정지 직후 재가속 시작 순간) 최소한의 미리보기 거리는 유지한다.
    [SerializeField] private float minSteeringLookahead = 0.3f;
    [SerializeField] private Animator animator;
    // 스킬(가속)·지형(빙결 등) 등 여러 출처가 동시에 속도를 배율로 곱할 수 있어, 한 곳에서 전부 합산하고
    // 이 상한을 넘지 못하게 막는다(docs/feature-expansion.md 4-2-1).
    [SerializeField] private float maxSpeedMultiplier = 2.5f;
    // NavMeshAgent는 목적지 방향이 꺾여도 velocity를 acceleration만큼씩만 그쪽으로 바꾼다 — 이 값이 낮으면
    // (기본값 8) 속도가 빠를수록 방향을 꺾는 데 필요한 거리가 늘어나(자동차가 빠를수록 같은 핸들 힘으론
    // 회전 반경이 커지는 것과 같은 원리) 코너에서 경로를 크게 벗어난다. 충분히 큰 값으로 고정해두면 속도가
    // 얼마든 코너에서 거의 그 자리에서 바로 꺾이듯 방향이 바뀐다(실측: 60 이상이면 가속 스킬 최대 배율
    // 2.5배에서도 코너 이탈이 사실상 0에 가까움). 굳이 속도 배율에 비례시킬 필요 없이 항상 이 값 하나로 고정.
    [SerializeField] private float corneringAcceleration = 60f;

    private NavMeshAgent agent;
    // NavMeshAgent가 현재 목적지로 삼고 있는 타일(조향용, steeringLookahead 기준으로 미리 넘어감)
    private int steeringTileIndex;
    // 게임 로직에 "도달"로 보고하는 타일(arrivalThreshold 기준, 실제 위치와의 거리로 독립 계산)
    private int reachedTileIndex;
    private float baseSpeed;
    private float baseAngularSpeed;
    private bool paused;
    private readonly Dictionary<object, float> speedMultipliers = new Dictionary<object, float>();

    public bool IsPaused => paused;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        baseSpeed = agent.speed;
        baseAngularSpeed = agent.angularSpeed;
        // 타일 하나하나를 SetDestination으로 순서대로 넘기는 구조라, NavMeshAgent가 "여기가 최종 목적지"라고
        // 믿고 도착 전에 알아서 감속(autoBraking)해버리면 타일마다 감속→재가속이 반복돼 이동이 뚝뚝 끊긴다.
        // 다음 타일 전환은 이미 우리가 remainingDistance/arrivalThreshold로 직접 제어하고 있으므로 꺼도 안전하다.
        agent.autoBraking = false;
        agent.acceleration = corneringAcceleration;
    }

    private void Start()
    {
        // 0번 타일에서 시작한다고 가정하고 다음 목표(1번)부터 이동 시작
        steeringTileIndex = 1;
        reachedTileIndex = 0;
        if (pathManager.TileCount > steeringTileIndex)
        {
            agent.SetDestination(pathManager.GetTilePosition(steeringTileIndex));
        }
    }

    private void Update()
    {
        if (paused)
        {
            if (animator != null) animator.SetBool("IsMove", false);
            return;
        }

        if (animator != null)
        {
            animator.SetBool("IsMove", agent.velocity.sqrMagnitude > 0.01f);
        }

        if (pathManager.TileCount == 0) return;

        // 게임 로직용 "도달" 판정 — 조향 목표(steeringTileIndex)가 코너를 매끄럽게 돌기 위해 미리 넘어가도,
        // 실제 위치가 다음 타일에 arrivalThreshold만큼 가까워졌을 때만 독립적으로 갱신한다.
        // (진행바·튜토리얼 강제 슬롯·가시함정 등은 전부 이 값을 기준으로 동작하므로 타이밍이 기존과 동일하게 유지됨)
        // ⚠️ SetCurrentPlayerTileIndex를 루프 "밖"에서 마지막 값으로 한 번만 부르면, 가속 스킬 등으로
        // 한 프레임에 여러 타일 경계를 동시에 넘을 때 중간 타일들이 통째로 씹혀서 TileReached 이벤트가
        // 하나도 안 뜨는 문제가 있었다(2026-09-17) — 루프 "안"에서 매 타일마다 호출해 전부 보고되게 한다.
        while (reachedTileIndex < pathManager.TileCount - 1)
        {
            Vector3 nextPos = pathManager.GetTilePosition(reachedTileIndex + 1);
            float distToNext = Vector3.Distance(transform.position, nextPos);
            bool closeEnough = distToNext <= arrivalThreshold;

            // ⚠️ 실측으로 발견한 훨씬 심각한 버그(2026-09-17): arrivalThreshold(0.15, 매우 좁음)가 요구하는
            // "다음 타일 정중앙에서 0.15유닛 이내"를 코너 근처(steeringLookahead로 미리 꺾는 구간)에서 단 한
            // 번이라도 못 맞히면, reachedTileIndex가 그 타일에서 영원히 멈춰버렸다 — 이 루프는 "다음 타일"만
            // 확인하므로 그 뒤로는 플레이어가 아무리 멀리 가도 절대 못 따라잡고, 그 이후 모든 TileReached
            // 트리거(스테이지2 스킬 해금 등)가 통째로 죽는 재앙적 결과로 이어졌다(실측: 코너를 돈 뒤 타일7에서
            // 멈춘 채 실제 위치는 타일21까지 진행됨). 정확히 근접 못 해도, "다음 타일보다 그 다음 타일에 더
            // 가까워졌다"면 이미 지나친 것으로 보고 따라잡는다.
            bool alreadyPassed = false;
            if (!closeEnough && reachedTileIndex + 2 < pathManager.TileCount)
            {
                float distToFollowing = Vector3.Distance(transform.position, pathManager.GetTilePosition(reachedTileIndex + 2));
                alreadyPassed = distToFollowing < distToNext;
            }

            if (!closeEnough && !alreadyPassed) break;
            reachedTileIndex++;
            pathManager.SetCurrentPlayerTileIndex(reachedTileIndex);
        }

        if (agent.pathPending) return;
        // 조향용 목적지는 "속도 × 반응 시간"만큼 미리 다음 타일로 넘겨서, NavMeshAgent가 코너 앞에서 미리
        // 방향을 틀 여유를 준다(도착 직전까지 기다리면 코너를 급하게 꺾다 휘청임). 거리를 속도에 비례시켜서
        // 가속 스킬 등으로 빨라져도 반응 시간(따라서 코너를 도는 정도)이 항상 비슷하게 유지되게 한다.
        float steeringLookahead = Mathf.Max(agent.speed * steeringLookaheadTime, minSteeringLookahead);
        if (agent.remainingDistance > steeringLookahead) return;

        if (steeringTileIndex < pathManager.TileCount - 1)
        {
            steeringTileIndex++;
            agent.SetDestination(pathManager.GetTilePosition(steeringTileIndex));
        }
    }

    // 재도전 시 호출 — Player를 0번 타일로 되돌리고 다시 1번 타일부터 이동을 시작한다.
    public void ResetMovement()
    {
        enabled = true;
        paused = false;
        speedMultipliers.Clear();
        ApplySpeedMultiplier();
        agent.isStopped = false;
        agent.ResetPath();
        agent.Warp(pathManager.GetTilePosition(0));

        steeringTileIndex = 1;
        reachedTileIndex = 0;
        pathManager.SetCurrentPlayerTileIndex(0);
        if (pathManager.TileCount > steeringTileIndex)
        {
            agent.SetDestination(pathManager.GetTilePosition(steeringTileIndex));
        }
    }

    // 스킬(가속)·지형(빙결) 등 출처(source)별로 배율을 등록한다. 1이면 해당 출처의 배율을 제거한다.
    // 여러 출처의 배율은 전부 곱해지고, 최종값은 maxSpeedMultiplier로 상한이 걸린다.
    public void SetSpeedMultiplier(object source, float multiplier)
    {
        if (Mathf.Approximately(multiplier, 1f)) speedMultipliers.Remove(source);
        else speedMultipliers[source] = multiplier;
        ApplySpeedMultiplier();
    }

    private void ApplySpeedMultiplier()
    {
        float total = 1f;
        foreach (float m in speedMultipliers.Values) total *= m;
        total = Mathf.Min(total, maxSpeedMultiplier);
        agent.speed = baseSpeed * total;
        agent.angularSpeed = baseAngularSpeed * total;
    }

    // 정지 스킬 전용 — 그 자리에서 멈추거나(true) 다시 걷는다(false). 멈춘 동안 이동 애니메이션도 꺼진다.
    public void SetPaused(bool value)
    {
        paused = value;
        if (agent != null && agent.isOnNavMesh) agent.isStopped = value;
    }
}
