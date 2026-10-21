using System.Collections.Generic;
using UnityEngine;

/// 몬스터의 공격 사거리를 바닥에 원형으로 표시한다. Player가 캐릭터 기준 원형 시야 범위 안에
/// 처음 들어오는 순간 한 번만 나타났다가 `displayDuration` 후 사라진다(계속 떠 있지 않음).
/// FogVisibility(몬스터 본체 표시)는 한 번 드러나면 영구 유지되지만, 이 사거리 표시는 그와 별개로
/// "처음 발견했을 때 잠깐 보여주는 안내" 역할만 하고 이후에는 다시 나타나지 않는다.
/// 시각적으로는 얇고 밝은 "코어" 링 위에 넓고 은은한 "글로우" 링, 그리고 반투명 채움 원을 겹쳐서
/// 범위를 확실히 보여준다(MonsterRange_ref.png 참고). 채움 원은 몬스터(불투명 오브젝트)보다 항상
/// 뒤에 그려지도록 일반 깊이 테스트를 쓰는 셰이더(SkillVfxLibrary 절차적 머티리얼)로 만들어서,
/// 링(항상 위에 그려지는 ZTest Always 전용 셰이더)과 달리 몬스터를 가리지 않는다.
[RequireComponent(typeof(LineRenderer))]
public class AttackRangeIndicator : MonoBehaviour
{
    [SerializeField] private MonsterTower monsterTower;
    [SerializeField] private Transform player;
    [SerializeField] private float visionRadius = 8f;
    [SerializeField] private int segmentCount = 48;
    [SerializeField] private float lineWidth = 0.04f;
    // 채움 원의 높이 기준 — GroundBumps 지형 굴곡(최대 약 0.51유닛)보다 높게 잡아야 채움이 지형에
    // 가려지지 않는다(2026-09-17). 링은 ZTest Always라 애초에 지형에 안 가려지지만, 채움은 몬스터에게는
    // 가려지도록 일반 깊이 테스트를 쓰는 만큼 지형 굴곡에도 같이 가려졌었음 — 몬스터/캐릭터 몸통 높이보다는
    // 한참 낮게 유지해 그 밑에 깔리는 것은 그대로 유지한다.
    [SerializeField] private float heightOffset = 0.6f;
    [SerializeField] private float displayDuration = 1f;

    // 코어 링(밝은 중심선)이 블룸으로 번지도록 1을 넘는 HDR 색을 코드로 만든다 — 셰이더/머티리얼
    // 에셋은 그대로 두고(기존 M_RangeIndicator.mat = 바깥 글로우 링) 코어만 절차적으로 추가한다.
    [SerializeField] private Color coreColor = new Color(3f, 0.65f, 0.25f, 0.95f);
    [SerializeField] private float coreWidthRatio = 0.35f; // 글로우 링 두께 대비 코어 두께 비율

    // 범위 안쪽을 옅게 채우는 색 — HDR 아님(넓은 면적에 블룸이 과하게 번지는 걸 피함).
    [SerializeField] private Color fillColor = new Color(1f, 0.35f, 0.1f, 0.32f);

    private LineRenderer lineRenderer; // 바깥 글로우 링(기존 M_RangeIndicator.mat)
    private LineRenderer coreLine;     // 안쪽 밝은 코어 링(런타임 생성)
    private MeshRenderer fillRenderer; // 범위 채움 원(런타임 생성, 몬스터에게 가려짐)
    private readonly List<Object> owned = new List<Object>();
    private bool revealed = false;
    private float timeSinceRevealed = 0f;

    private void Awake()
    {
        if (monsterTower == null) monsterTower = GetComponent<MonsterTower>();
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) player = playerObj.transform;
        }

        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.loop = true;
        lineRenderer.useWorldSpace = true;
        lineRenderer.positionCount = segmentCount;
        lineRenderer.widthMultiplier = lineWidth;
        lineRenderer.enabled = false;

        BuildCoreLine();
        BuildFill();
    }

    private void BuildCoreLine()
    {
        var go = new GameObject("RangeIndicator_Core");
        go.transform.SetParent(transform, false);

        coreLine = go.AddComponent<LineRenderer>();
        coreLine.loop = true;
        coreLine.useWorldSpace = true;
        coreLine.positionCount = segmentCount;
        coreLine.widthMultiplier = lineWidth * coreWidthRatio;
        coreLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        coreLine.receiveShadows = false;

        var mat = new Material(lineRenderer.sharedMaterial != null
            ? lineRenderer.sharedMaterial.shader
            : Shader.Find("Custom/RangeIndicatorOverlay"));
        mat.SetColor("_BaseColor", coreColor);
        coreLine.sharedMaterial = mat;
        coreLine.enabled = false;
    }

    // 링과 달리 일반 ZTest(LEqual)를 쓰는 SkillVfxLibrary 머티리얼로 만든다 — 몬스터(불투명, 먼저 그려짐)가
    // 이미 깊이버퍼에 써놓은 값보다 뒤에 있는 채움 픽셀은 깊이 테스트에서 걸러져 몬스터 위에 덮이지 않는다.
    private void BuildFill()
    {
        // 링(LineRenderer, useWorldSpace)은 월드 좌표로 직접 그려서 몬스터 스케일과 무관하지만, 이 Quad는
        // 로컬 스케일로 크기를 정하기 때문에 부모(몬스터 루트) 스케일이 그대로 곱해진다. 몬스터 크기를
        // 키우면(예: 2026-09-16 닭 스케일 2→4) 채움 원이 링보다 몇 배나 커지는 문제가 있어, 부모 스케일의
        // 역수를 가진 보정용 부모를 하나 끼워 최종 월드 크기가 항상 사거리와 정확히 일치하게 한다
        // (SkillVfxController.CompensateScale과 동일한 패턴).
        var compensator = new GameObject("RangeIndicator_FillScale");
        compensator.transform.SetParent(transform, false);
        Vector3 parentScale = transform.lossyScale;
        compensator.transform.localScale = new Vector3(Inv(parentScale.x), Inv(parentScale.y), Inv(parentScale.z));

        Material fillMat = SkillVfxLibrary.CreateMaterial(null, SkillVfxLibrary.SoftCircle, false, fillColor, owned);
        fillRenderer = SkillVfxLibrary.CreateFlatQuad("RangeIndicator_Fill", compensator.transform, fillMat, 1f, heightOffset);
        fillRenderer.enabled = false;
    }

    private static float Inv(float v) => Mathf.Abs(v) < 0.0001f ? 1f : 1f / v;

    private void Start()
    {
        float radius = monsterTower.AttackRange;
        DrawCircle(lineRenderer, radius);
        DrawCircle(coreLine, radius);
        fillRenderer.transform.localScale = new Vector3(radius * 2f, radius * 2f, 1f);
    }

    private void LateUpdate()
    {
        if (player == null) return;

        if (!revealed)
        {
            float distance = Vector3.Distance(player.position, transform.position);
            if (distance <= visionRadius || VisionSourceRegistry.IsWithinAnySource(transform.position))
            {
                revealed = true;
                timeSinceRevealed = 0f;
                lineRenderer.enabled = true;
                coreLine.enabled = true;
                fillRenderer.enabled = true;
            }
            return;
        }

        if (lineRenderer.enabled)
        {
            timeSinceRevealed += Time.deltaTime;
            if (timeSinceRevealed >= displayDuration)
            {
                lineRenderer.enabled = false;
                coreLine.enabled = false;
                fillRenderer.enabled = false;
            }
        }
    }

    // 재도전 시 호출 — 다시 처음 발견되지 않은 상태로 되돌린다(다음에 시야에 들어오면 다시 표시됨).
    public void ResetIndicator()
    {
        revealed = false;
        timeSinceRevealed = 0f;
        lineRenderer.enabled = false;
        if (coreLine != null) coreLine.enabled = false;
        if (fillRenderer != null) fillRenderer.enabled = false;
    }

    private void OnDestroy()
    {
        SkillVfxLibrary.DestroyOwned(owned);
    }

    private void DrawCircle(LineRenderer target, float radius)
    {
        Vector3 center = transform.position + Vector3.up * heightOffset;
        for (int i = 0; i < segmentCount; i++)
        {
            float angle = i * Mathf.PI * 2f / segmentCount;
            Vector3 point = center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
            target.SetPosition(i, point);
        }
    }
}
