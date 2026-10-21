using UnityEngine;

/// 플레이어를 중심으로 한 원형 시야 범위 "안에 있는 동안에만" 오브젝트의 시각적 모델(Renderer)을
/// 보여준다(스타크래프트식 — 시야를 벗어나면 다시 숨김). 몬스터뿐 아니라 함정·환경 오브젝트에도
/// 그대로 붙여 재사용할 수 있는 범용 컴포넌트.
public class FogVisibility : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private float visionRadius = 8f;
    [SerializeField] private Renderer[] targetRenderers;

    // 현재 프레임에 실제로 보이는 상태인지(시야 안/밖에 따라 계속 바뀜) — RockfallTrap처럼 렌더러를
    // 직접 제어하는 쪽에서 "지금 공개된 상태인가"를 매 프레임 물어볼 때 씀.
    public bool IsCurrentlyVisible { get; private set; }

    // 시야 밖에 있다가 처음 드러나는 순간 딱 한 번만 발행된다(튜토리얼 팝업 등 "처음 발견" 트리거용).
    // 이후 시야를 들락날락해도 다시 발행되지 않음 — 렌더러 표시 자체는 IsCurrentlyVisible/매 프레임
    // 갱신되는 가시성으로 따로 처리되고, 이 이벤트는 순수하게 "처음 한 번" 전용이다.
    public event System.Action OnRevealed;
    private bool everRevealed;

    private void Awake()
    {
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) player = playerObj.transform;
        }

        if (targetRenderers == null || targetRenderers.Length == 0)
        {
            MeshRenderer[] meshRenderers = GetComponentsInChildren<MeshRenderer>(true);
            SkinnedMeshRenderer[] skinnedRenderers = GetComponentsInChildren<SkinnedMeshRenderer>(true);
            targetRenderers = new Renderer[meshRenderers.Length + skinnedRenderers.Length];
            meshRenderers.CopyTo(targetRenderers, 0);
            skinnedRenderers.CopyTo(targetRenderers, meshRenderers.Length);
        }

        SetVisible(false);
    }

    private void LateUpdate()
    {
        if (player == null) return;

        float distance = Vector3.Distance(player.position, transform.position);
        bool inVision = distance <= visionRadius || VisionSourceRegistry.IsWithinAnySource(transform.position);

        if (inVision != IsCurrentlyVisible)
        {
            IsCurrentlyVisible = inVision;
            SetVisible(inVision);
        }

        if (inVision && !everRevealed)
        {
            everRevealed = true;
            OnRevealed?.Invoke();
        }
    }

    private void SetVisible(bool visible)
    {
        foreach (Renderer r in targetRenderers)
        {
            if (r != null) r.enabled = visible;
        }
    }

    // 재도전 시 호출 — 다시 시야 밖에 있는 것처럼 숨긴다. "처음 발견" 여부(everRevealed)도 함께
    // 초기화한다 — 재도전하면 그 스테이지에서 진행한 모든 것(튜토리얼 포함)이 처음부터 다시 재생되어야
    // 한다는 정책(2026-09-17, 모든 스테이지 공통)에 따라, 한 번 본 튜토리얼도 재도전 시 다시 뜬다.
    public void ResetVisibility()
    {
        IsCurrentlyVisible = false;
        everRevealed = false;
        SetVisible(false);
    }

    // Awake 이후(예: 절차적으로 생성되는 함정 비주얼처럼 런타임에 나중에 렌더러가 만들어지는 경우)
    // 감시 대상 렌더러를 다시 지정한다. 현재 공개 상태를 그대로 반영해 즉시 맞는 가시성으로 맞춘다.
    public void SetTargetRenderers(Renderer[] renderers)
    {
        targetRenderers = renderers;
        SetVisible(IsCurrentlyVisible);
    }
}
