using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// 낙석 함정(주기형). 대기→경고→낙하→충돌→잔해를 반복한다(docs/feature-expansion.md 3-2).
/// 벽으로는 막을 수 없고, 안개/조명탄 범위 밖에서는 경고 원·바위 전부 안 보인다.
public class RockfallTrap : MonoBehaviour
{
    // 렌더러가 없는 별도 자식(FogSensor)에 붙여서 넘겨받는다 — 같은 오브젝트에 두면 FogVisibility.Awake()가
    // 경고 원/바위 렌더러까지 자동으로 찾아 자기 방식대로 켜고 꺼버려서, 이 스크립트가 단계(Phase)별로
    // 직접 제어하려는 것과 충돌한다(예: Idle 단계인데 시야에 들어왔다고 바위가 보여버리는 문제).
    [SerializeField] private FogVisibility fogVisibility;

    [Header("주기 타이밍(초)")]
    [SerializeField] private float idleDuration = 2.5f;
    [SerializeField] private float warningDuration = 1.2f;
    [SerializeField] private float fallDuration = 0.3f;
    [SerializeField] private float debrisDuration = 0.5f;
    // 함정마다 다르게 줘서 여러 개가 동시에 안 떨어지게 한다.
    [SerializeField] private float startDelay = 0f;

    [Header("판정")]
    [SerializeField] private float impactRadius = 1.5f;
    [SerializeField] private float damage = 15f;
    [SerializeField] private LayerMask targetLayerMask;

    [Header("바위 비주얼")]
    [SerializeField] private GameObject rockPrefab; // Low Poly Nature Bundle 바위(Round_Rocks 등)
    // rockPrefab의 임포트 기본 머티리얼이 밋밋해서, 기존에 이미 쓰고 있는 바위 재질(Rocks_Moss 등)로 덮어씌운다.
    // sharedMaterial로 지정 — 원본 에셋을 바꾸는 게 아니라 이 인스턴스가 그 머티리얼을 "참조"만 하는 것.
    [SerializeField] private Material rockMaterialOverride;
    [SerializeField] private float rockScale = 0.9f;
    [SerializeField] private float rockRestLocalY = 0.5f; // 바위 밑면이 바닥에 닿는 높이(피벗 기준)
    [SerializeField] private float rockFallStartLocalY = 4.5f;
    [SerializeField] private float debrisShakeAmount = 0.08f;

    // 채움만 있던 첫 버전이 너무 옅어서(SoftCircle이 중심부터 계속 흐려지는 텍스처라 경고용으론 존재감이
    // 약함, 특히 새 안개의 밝은 시야 원과 겹치면 거의 안 보임) 채움을 HardCircle(가장자리만 페이드)로
    // 바꾸고 알파를 올린 뒤, 경계를 확실히 보여주는 밝은 고리(Ring)를 추가로 겹쳤다(2026-09-16).
    [Header("경고 원(캐릭터에는 자연스럽게 가려지되 눈에 띄게)")]
    [SerializeField] private Color warningFillColor = new Color(1f, 0.15f, 0.1f, 0.75f);
    [SerializeField] private Color warningRingColor = new Color(1f, 0.35f, 0.05f, 0.95f);
    [SerializeField] private float warningHeightOffset = 0.05f;

    [Header("피해 숫자 팝업(선택)")]
    [SerializeField] private GameObject damageNumberPrefab;
    [SerializeField] private Transform damageNumberParent;

    private enum Phase { Idle, Warning, Falling, Debris }

    private Phase phase = Phase.Idle;

    private Transform rockVisual;
    private Renderer[] rockRenderers;
    private Transform warningDisc;
    private Renderer warningRenderer;
    private Transform warningRing;
    private Renderer warningRingRenderer;
    private readonly List<Object> ownedVfx = new List<Object>();

    private Coroutine cycleCoroutine;

    private void Awake()
    {
        BuildRockVisual();
        BuildWarningDisc();
    }

    private void OnDestroy()
    {
        SkillVfxLibrary.DestroyOwned(ownedVfx);
    }

    private void Start()
    {
        cycleCoroutine = StartCoroutine(RunCycle());
    }

    private void Update()
    {
        // 페이드 없이 즉시 on/off — "지금 이 순간 시야 안에 있는가"(FogVisibility.IsCurrentlyVisible,
        // 시야를 벗어나면 다시 false로 돌아감)와 "지금 단계에 보여야 하는가"를 매 프레임 함께 판단.
        bool currentlyVisible = fogVisibility != null && fogVisibility.IsCurrentlyVisible;
        bool warningVisible = currentlyVisible && (phase == Phase.Warning || phase == Phase.Falling);
        if (warningRenderer != null) warningRenderer.enabled = warningVisible;
        if (warningRingRenderer != null) warningRingRenderer.enabled = warningVisible;

        bool rockVisible = currentlyVisible && (phase == Phase.Falling || phase == Phase.Debris);
        if (rockRenderers != null)
        {
            foreach (Renderer r in rockRenderers)
            {
                if (r != null) r.enabled = rockVisible;
            }
        }
    }

    private IEnumerator RunCycle()
    {
        if (startDelay > 0f) yield return new WaitForSeconds(startDelay);

        while (true)
        {
            yield return Idle();
            yield return Warning();
            yield return Fall();
            Impact();
            yield return Debris();
        }
    }

    private IEnumerator Idle()
    {
        phase = Phase.Idle;
        SetWarningScale(0f);
        yield return new WaitForSeconds(idleDuration);
    }

    private IEnumerator Warning()
    {
        phase = Phase.Warning;
        float t = 0f;
        while (t < warningDuration)
        {
            t += Time.deltaTime;
            SetWarningScale(Mathf.Clamp01(t / warningDuration));
            yield return null;
        }
        SetWarningScale(1f);
    }

    private IEnumerator Fall()
    {
        phase = Phase.Falling;
        float t = 0f;
        while (t < fallDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / fallDuration);
            float eased = p * p; // 중력 가속 느낌으로 점점 빨라지게
            float y = Mathf.Lerp(rockFallStartLocalY, rockRestLocalY, eased);
            rockVisual.localPosition = new Vector3(0f, y, 0f);
            yield return null;
        }
        rockVisual.localPosition = new Vector3(0f, rockRestLocalY, 0f);
    }

    // 충돌 판정은 여기서 딱 한 번만 이뤄진다 — Update에서 매 프레임 검사하지 않으므로
    // 바위가 떨어진 뒤에 원 안으로 들어와도 맞지 않는다(기획서 요구사항).
    private void Impact()
    {
        SetWarningScale(0f); // 경고 원은 충돌 순간 사라짐(잔해 단계엔 바위 자체가 위험 신호)

        Collider[] hits = Physics.OverlapSphere(transform.position, impactRadius, targetLayerMask);
        foreach (Collider col in hits)
        {
            IDamageable damageable = col.GetComponent<IDamageable>();
            if (damageable == null) continue;

            bool applied = damageable.TakeDamage(damage);
            if (applied) SpawnDamageNumber(col.ClosestPoint(transform.position));
        }
    }

    private IEnumerator Debris()
    {
        phase = Phase.Debris;
        Vector3 basePos = new Vector3(0f, rockRestLocalY, 0f);
        float t = 0f;
        while (t < debrisDuration)
        {
            t += Time.deltaTime;
            float remaining = 1f - (t / debrisDuration);
            float shake = debrisShakeAmount * remaining;
            rockVisual.localPosition = basePos + new Vector3(Random.Range(-shake, shake), 0f, Random.Range(-shake, shake));
            yield return null;
        }
        rockVisual.localPosition = basePos;
    }

    // 재도전 시 호출 — 진행 중이던 주기를 끊고 처음(대기, 원래 offset 포함)부터 다시 시작한다.
    public void ResetTrap()
    {
        if (cycleCoroutine != null) StopCoroutine(cycleCoroutine);
        phase = Phase.Idle;
        SetWarningScale(0f);
        if (rockVisual != null) rockVisual.localPosition = new Vector3(0f, rockRestLocalY, 0f);
        cycleCoroutine = StartCoroutine(RunCycle());
    }

    private void SetWarningScale(float ratio)
    {
        float diameter = impactRadius * 2f * Mathf.Max(0f, ratio);
        if (warningDisc != null) warningDisc.localScale = new Vector3(diameter, diameter, 1f);
        if (warningRing != null) warningRing.localScale = new Vector3(diameter, diameter, 1f);
    }

    private void BuildRockVisual()
    {
        GameObject rockGo = rockPrefab != null
            ? Instantiate(rockPrefab)
            : GameObject.CreatePrimitive(PrimitiveType.Sphere);
        rockGo.name = "RockVisual";
        rockGo.transform.SetParent(transform, false);
        rockGo.transform.localScale = Vector3.one * rockScale;
        rockGo.transform.localPosition = new Vector3(0f, rockRestLocalY, 0f);
        rockGo.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

        Collider col = rockGo.GetComponentInChildren<Collider>();
        if (col != null) Destroy(col); // 순수 비주얼 — 낙석 판정은 OverlapSphere로 따로 처리

        rockVisual = rockGo.transform;
        rockRenderers = rockGo.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer r in rockRenderers)
        {
            if (rockMaterialOverride != null) r.sharedMaterial = rockMaterialOverride;
            r.enabled = false;
        }
    }

    // 원래 사거리 표시(AttackRangeIndicator)와 같은 ZTest Always 오버레이 셰이더를 썼었는데, 그 셰이더는
    // "테두리 링"처럼 캐릭터에 안 가려져야 하는 형태를 위한 것이라 속이 꽉 찬 원에 쓰면 캐릭터·몬스터보다도
    // 항상 위에 그려져버렸다(깊이 검사를 아예 안 하므로). AttackRangeIndicator의 "채워지는 원" 부분은 실제로는
    // 이 오버레이 셰이더가 아니라 일반 깊이 검사를 쓰는 반투명 재질(SkillVfxLibrary.CreateMaterial)을 쓰고
    // 있어서 캐릭터에 자연스럽게 가려진다 — 그 방식을 그대로 재사용한다.
    // 채움만으로는(SoftCircle) 존재감이 너무 약해서(특히 밝은 시야 원과 겹치면 거의 안 보임) HardCircle로
    // 바꾸고, 경계를 확실히 보여주는 밝은 고리(Ring)를 채움 위에 겹쳐서 "지금 여기가 위험하다"가 한눈에
    // 들어오게 했다(2026-09-16, 사용자가 "경고 범위가 너무 안 보인다"고 지적해서 보강).
    // 지형에 가려질 걱정은 없음: 낙석 함정은 항상 경로 타일 위에 놓이고, 그 주변은 GroundBumps 생성 시
    // 이미 평탄화 반경(2.2유닛)으로 처리되는 구역이라 경고 원(최대 반지름 impactRadius=1.5) 근처엔
    // 지형 굴곡이 없다.
    private void BuildWarningDisc()
    {
        Material fillMat = SkillVfxLibrary.CreateMaterial(null, SkillVfxLibrary.HardCircle, false, warningFillColor, ownedVfx);
        warningRenderer = SkillVfxLibrary.CreateFlatQuad("WarningDisc", transform, fillMat, 0f, warningHeightOffset);
        warningRenderer.enabled = false;
        warningDisc = warningRenderer.transform;

        Material ringMat = SkillVfxLibrary.CreateMaterial(null, SkillVfxLibrary.Ring, false, warningRingColor, ownedVfx);
        warningRingRenderer = SkillVfxLibrary.CreateFlatQuad("WarningRing", transform, ringMat, 0f, warningHeightOffset + 0.01f);
        warningRingRenderer.enabled = false;
        warningRing = warningRingRenderer.transform;
    }

    private void SpawnDamageNumber(Vector3 worldPosition)
    {
        if (damageNumberPrefab == null) return;

        if (damageNumberParent == null)
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas != null) damageNumberParent = canvas.transform;
        }

        GameObject popupObj = Instantiate(damageNumberPrefab, damageNumberParent);
        DamageNumberPopup popup = popupObj.GetComponent<DamageNumberPopup>();
        popup.Initialize(worldPosition + Vector3.up * 0.5f, damage);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, impactRadius);
    }
}
