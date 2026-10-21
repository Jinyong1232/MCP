using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어에 붙이는 스킬 이펙트(순간 가속 / 무적 / 정지).
/// 보이는 것만 담당하고, 속도·피해·이동 같은 실제 효과는 SkillManager 쪽에서 처리한다.
///
/// 연결:
///   가속 시작  → PlayDash(지속시간)      / 강제 종료 → StopDash()
///   무적 시작  → PlayShield(지속시간)    / 강제 종료 → StopShield()
///   무적 중 피격 무시 → PulseShieldHit()
///   정지 시작  → PlayFreeze(지속시간)    / 강제 종료 → StopFreeze()
///   재도전     → StopAll()
///
/// 지속시간은 scaled time 기준이라 튜토리얼 정지(timeScale=0) 중에는 이펙트도 멈춘다.
/// </summary>
[DisallowMultipleComponent]
public class SkillVfxController : MonoBehaviour
{
    [Header("공통")]
    [Tooltip("URP Particles/Unlit 머티리얼 에셋. 비워두면 코드로 만들지만 빌드에서 셰이더가 빠질 수 있어 지정을 권장")]
    [SerializeField] Material particleMaterialTemplate;
    [SerializeField] float characterHeight = 1.2f;

    [Header("순간 가속")]
    [SerializeField] Color dashTrailColor = new Color(1f, 0.97f, 0.9f, 0.75f);
    [SerializeField] Color dashDustColor = new Color(0.86f, 0.78f, 0.62f, 0.7f);
    [SerializeField] float dashTrailTime = 0.3f;
    [SerializeField] float dashTrailWidth = 0.55f;
    // 발밑 트레일만으로는 "빨라졌다"는 게 잘 안 느껴진다는 피드백으로 추가 — 캐릭터 옆을 스쳐 지나가는
    // 잔상 줄무늬(스트레치드 빌보드라 속도에 비례해 늘어남, 이동 방향과 무관하게 항상 카메라 기준으로 보임)
    [SerializeField] Color dashStreakColor = new Color(1f, 0.95f, 0.8f, 0.9f);
    [SerializeField] float dashStreakRate = 45f;

    [Header("무적")]
    [SerializeField] Color shieldColor = new Color(0.44f, 0.83f, 1f, 1f);
    [SerializeField] float shieldRadius = 0.9f;
    [SerializeField] float shieldWarnTime = 0.5f;
    [SerializeField] float shieldBlinkInterval = 0.08f;

    [Header("정지")]
    [SerializeField] Color freezeColor = new Color(1f, 0.76f, 0.28f, 1f);
    [SerializeField] float freezeGaugeRadius = 0.8f;
    [SerializeField] float freezeGaugeWidth = 0.07f;
    [SerializeField] float freezeIconHeight = 2.2f;

    const int GaugeSegments = 64;
    const float FreezeShockTime = 0.25f;

    readonly List<Object> owned = new List<Object>();
    bool built;

    Transform followRoot; // 플레이어 위치·회전을 따라감
    Transform worldRoot;  // 위치만 따라가고 회전은 고정(게이지, 아이콘)

    Material matSolidAdd;
    Material matSoft;
    Material matSoftAdd;
    Material matSolidAlpha;

    // 순간 가속
    TrailRenderer dashTrail;
    ParticleSystem dashDust;
    ParticleSystem dashBurst;
    ParticleSystem dashStreaks;
    Coroutine dashRoutine;

    // 무적
    Transform shieldRoot;
    MeshRenderer shieldFill, shieldRim, shieldGround;
    Material shieldFillMat, shieldRimMat;
    ParticleSystem shieldBurst;
    float shieldDuration, shieldRemaining, shieldHitFlash;
    bool shieldActive;

    // 정지
    Transform freezeRoot;
    LineRenderer gaugeBg, gaugeFg;
    Transform freezeIcon;
    Renderer[] freezeIconRenderers;
    MeshRenderer freezeShock;
    Material freezeShockMat;
    float freezeDuration, freezeRemaining, freezeShockElapsed;
    bool freezeActive;

    void Awake() => Build();

    void OnDisable() => StopAll();

    void OnDestroy() => SkillVfxLibrary.DestroyOwned(owned);

    void Update()
    {
        if (!built) return;
        float dt = Time.deltaTime;
        if (shieldActive) TickShield(dt);
        if (freezeActive) TickFreeze(dt);
    }

    void LateUpdate()
    {
        if (built) worldRoot.rotation = Quaternion.identity;
    }

    // ───────────────────────── 공개 API ─────────────────────────

    public void PlayDash(float duration)
    {
        if (!isActiveAndEnabled) return;
        Build();
        if (dashRoutine != null) StopCoroutine(dashRoutine);
        dashRoutine = StartCoroutine(DashRoutine(duration));
    }

    public void StopDash()
    {
        if (dashRoutine != null)
        {
            StopCoroutine(dashRoutine);
            dashRoutine = null;
        }
        EndDash();
    }

    public void PlayShield(float duration)
    {
        if (!isActiveAndEnabled) return;
        Build();
        shieldDuration = Mathf.Max(0.01f, duration);
        shieldRemaining = shieldDuration;
        shieldHitFlash = 0f;
        shieldActive = true;
        SetShieldVisible(true);
        shieldBurst.Play();
        shieldBurst.Emit(18);
    }

    public void StopShield()
    {
        shieldActive = false;
        if (built) SetShieldVisible(false);
    }

    /// <summary>무적 중 피해를 막았을 때 호출하면 보호막이 잠깐 번쩍인다</summary>
    public void PulseShieldHit()
    {
        if (shieldActive) shieldHitFlash = 1f;
    }

    public void PlayFreeze(float duration)
    {
        if (!isActiveAndEnabled) return;
        Build();
        freezeDuration = Mathf.Max(0.01f, duration);
        freezeRemaining = freezeDuration;
        freezeShockElapsed = 0f;
        freezeActive = true;
        SetFreezeVisible(true);
        SetArc(gaugeBg, 1f);
        SetArc(gaugeFg, 1f);
    }

    public void StopFreeze()
    {
        freezeActive = false;
        if (built) SetFreezeVisible(false);
    }

    /// <summary>재도전 등에서 모든 스킬 이펙트를 즉시 제거</summary>
    public void StopAll()
    {
        StopDash();
        StopShield();
        StopFreeze();
        if (!built) return;
        dashTrail.Clear();
        dashDust.Clear();
        dashBurst.Clear();
        dashStreaks.Clear();
        shieldBurst.Clear();
    }

    // ───────────────────────── 생성 ─────────────────────────

    void Build()
    {
        if (built) return;
        built = true;

        followRoot = new GameObject("VFX_Skill").transform;
        followRoot.SetParent(transform, false);
        worldRoot = new GameObject("VFX_Skill_World").transform;
        worldRoot.SetParent(transform, false);
        CompensateScale(followRoot);
        CompensateScale(worldRoot);

        matSolidAdd = SkillVfxLibrary.CreateMaterial(particleMaterialTemplate, null, true, Color.white, owned);
        matSoft = SkillVfxLibrary.CreateMaterial(particleMaterialTemplate, SkillVfxLibrary.SoftCircle, false, Color.white, owned);
        matSoftAdd = SkillVfxLibrary.CreateMaterial(particleMaterialTemplate, SkillVfxLibrary.SoftCircle, true, Color.white, owned);
        matSolidAlpha = SkillVfxLibrary.CreateMaterial(particleMaterialTemplate, null, false, Color.white, owned);

        BuildDash();
        BuildShield();
        BuildFreeze();
    }

    // 캐릭터 프리팹 스케일과 상관없이 이펙트 크기를 월드 단위로 맞춘다
    void CompensateScale(Transform t)
    {
        Vector3 s = transform.lossyScale;
        t.localScale = new Vector3(Inv(s.x), Inv(s.y), Inv(s.z));
    }

    static float Inv(float v) => Mathf.Abs(v) < 0.0001f ? 1f : 1f / v;

    void BuildDash()
    {
        // 잔상 트레일
        var trailGo = new GameObject("Dash_Trail");
        trailGo.transform.SetParent(followRoot, false);
        trailGo.transform.localPosition = new Vector3(0f, characterHeight * 0.45f, 0f);

        dashTrail = trailGo.AddComponent<TrailRenderer>();
        dashTrail.time = dashTrailTime;
        dashTrail.minVertexDistance = 0.05f;
        dashTrail.widthCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0f));
        dashTrail.widthMultiplier = dashTrailWidth;
        dashTrail.numCapVertices = 2;
        var g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(dashTrailColor, 0f), new GradientColorKey(dashTrailColor, 1f) },
            new[] { new GradientAlphaKey(dashTrailColor.a, 0f), new GradientAlphaKey(0f, 1f) });
        dashTrail.colorGradient = g;
        dashTrail.sharedMaterial = matSolidAdd;
        dashTrail.emitting = false;
        SkillVfxLibrary.NoShadow(dashTrail);

        // 발밑 흙먼지 (이동 거리 기준으로 뿜음)
        dashDust = SkillVfxLibrary.CreateParticleSystem("Dash_Dust", followRoot, matSoft);
        dashDust.transform.localPosition = new Vector3(0f, 0.08f, 0f);
        var m = dashDust.main;
        m.loop = true;
        m.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.55f);
        m.startSize = new ParticleSystem.MinMaxCurve(0.25f, 0.5f);
        m.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.6f);
        m.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        m.startColor = dashDustColor;
        m.gravityModifier = -0.02f;
        m.maxParticles = 80;
        var e = dashDust.emission;
        e.rateOverDistance = 6f;
        var sh = dashDust.shape;
        sh.enabled = true;
        sh.shapeType = ParticleSystemShapeType.Cone;
        sh.angle = 35f;
        sh.radius = 0.15f;
        sh.rotation = new Vector3(-90f, 0f, 0f); // 위쪽으로
        SkillVfxLibrary.SizeOverLifetime(dashDust, 0.6f, 1.4f);
        SkillVfxLibrary.FadeOverLifetime(dashDust, 0.2f);

        // 시작 순간 바닥에 퍼지는 고리
        dashBurst = SkillVfxLibrary.CreateParticleSystem("Dash_Burst", followRoot, matSoftAdd);
        dashBurst.transform.localPosition = new Vector3(0f, 0.1f, 0f);
        var bm = dashBurst.main;
        bm.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.35f);
        bm.startSize = new ParticleSystem.MinMaxCurve(0.18f, 0.32f);
        bm.startSpeed = new ParticleSystem.MinMaxCurve(2f, 3f);
        bm.startColor = dashTrailColor;
        bm.maxParticles = 30;
        var bs = dashBurst.shape;
        bs.enabled = true;
        bs.shapeType = ParticleSystemShapeType.Circle;
        bs.radius = 0.2f;
        bs.rotation = new Vector3(90f, 0f, 0f); // 바닥과 평행
        SkillVfxLibrary.SizeOverLifetime(dashBurst, 1f, 0.3f);
        SkillVfxLibrary.FadeOverLifetime(dashBurst, 0.1f);

        // 몸 옆을 스쳐 지나가는 잔상 줄무늬 — 로컬 공간에서 뒤쪽으로 흘려보내 "빠르게 스쳐감" 느낌을 준다.
        // Stretched Billboard는 로컬 속도에 비례해 저절로 길게 늘어나므로 속도감이 직관적으로 드러난다.
        dashStreaks = SkillVfxLibrary.CreateParticleSystem("Dash_Streaks", followRoot, matSoftAdd);
        dashStreaks.transform.localPosition = new Vector3(0f, characterHeight * 0.5f, 0f);
        var stm = dashStreaks.main;
        stm.loop = true;
        stm.simulationSpace = ParticleSystemSimulationSpace.Local;
        stm.startLifetime = new ParticleSystem.MinMaxCurve(0.28f, 0.4f);
        stm.startSize = new ParticleSystem.MinMaxCurve(0.14f, 0.22f);
        stm.startSpeed = new ParticleSystem.MinMaxCurve(7f, 11f); // Stretch 렌더링이 이 속도를 그대로 참조해 길이를 결정한다
        stm.startColor = dashStreakColor;
        stm.maxParticles = 60;
        var ste = dashStreaks.emission;
        ste.rateOverTime = 0f; // 재생 중에만 코드로 켬(DashRoutine)
        var sts = dashStreaks.shape;
        sts.enabled = true;
        sts.shapeType = ParticleSystemShapeType.Box;
        sts.scale = new Vector3(1.3f, characterHeight * 0.6f, 0.05f);
        sts.position = new Vector3(0f, 0f, 0.15f);
        sts.rotation = new Vector3(0f, 180f, 0f); // 로컬 -Z(뒤쪽)으로 발사
        SkillVfxLibrary.FadeOverLifetime(dashStreaks, 0.15f);

        var streakRenderer = dashStreaks.GetComponent<ParticleSystemRenderer>();
        streakRenderer.renderMode = ParticleSystemRenderMode.Stretch;
        streakRenderer.lengthScale = 4f;
        streakRenderer.velocityScale = 0.2f;
    }

    void BuildShield()
    {
        shieldRoot = new GameObject("Shield").transform;
        shieldRoot.SetParent(followRoot, false);
        shieldRoot.localPosition = new Vector3(0f, characterHeight * 0.5f, 0f);

        // 은은한 구체
        shieldFillMat = SkillVfxLibrary.CreateMaterial(particleMaterialTemplate, null, true, shieldColor, owned);
        var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = "Shield_Fill";
        SkillVfxLibrary.StripCollider(sphere);
        sphere.transform.SetParent(shieldRoot, false);
        sphere.transform.localScale = Vector3.one * (shieldRadius * 2f);
        shieldFill = sphere.GetComponent<MeshRenderer>();
        shieldFill.sharedMaterial = shieldFillMat;
        SkillVfxLibrary.NoShadow(shieldFill);

        // 위에서 봤을 때 보호막 외곽선 역할을 하는 고리
        shieldRimMat = SkillVfxLibrary.CreateMaterial(particleMaterialTemplate, SkillVfxLibrary.Ring, true, shieldColor, owned);
        shieldRim = SkillVfxLibrary.CreateFlatQuad("Shield_Rim", shieldRoot, shieldRimMat,
            shieldRadius * 2f / SkillVfxLibrary.RingPeak, 0f);

        // 발밑 빛
        var groundMat = SkillVfxLibrary.CreateMaterial(particleMaterialTemplate, SkillVfxLibrary.SoftCircle, true,
            new Color(shieldColor.r, shieldColor.g, shieldColor.b, 0.35f), owned);
        shieldGround = SkillVfxLibrary.CreateFlatQuad("Shield_Ground", followRoot, groundMat, shieldRadius * 2.4f, 0.05f);

        // 시작 순간 흩어지는 입자
        shieldBurst = SkillVfxLibrary.CreateParticleSystem("Shield_Burst", shieldRoot, matSoftAdd);
        var m = shieldBurst.main;
        m.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.45f);
        m.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.22f);
        m.startSpeed = new ParticleSystem.MinMaxCurve(2f, 3.2f);
        m.startColor = shieldColor;
        m.maxParticles = 30;
        var sh = shieldBurst.shape;
        sh.enabled = true;
        sh.shapeType = ParticleSystemShapeType.Sphere;
        sh.radius = 0.3f;
        SkillVfxLibrary.SizeOverLifetime(shieldBurst, 1f, 0.2f);
        SkillVfxLibrary.FadeOverLifetime(shieldBurst, 0.2f);

        SetShieldVisible(false);
    }

    void BuildFreeze()
    {
        freezeRoot = new GameObject("Freeze").transform;
        freezeRoot.SetParent(worldRoot, false);

        // 발밑 원형 게이지 (12시에서 시계 방향으로 줄어듦)
        var gauge = new GameObject("Freeze_Gauge").transform;
        gauge.SetParent(freezeRoot, false);
        gauge.localPosition = new Vector3(0f, 0.06f, 0f);
        gauge.localRotation = Quaternion.Euler(90f, 0f, 0f);
        gaugeBg = CreateLine("Gauge_Bg", gauge, new Color(freezeColor.r, freezeColor.g, freezeColor.b, 0.25f));
        gaugeFg = CreateLine("Gauge_Fg", gauge, new Color(freezeColor.r, freezeColor.g, freezeColor.b, 0.95f));

        // 머리 위 일시정지 아이콘
        freezeIcon = new GameObject("Freeze_Icon").transform;
        freezeIcon.SetParent(freezeRoot, false);
        freezeIcon.localPosition = new Vector3(0f, freezeIconHeight, 0f);

        var backMat = SkillVfxLibrary.CreateMaterial(particleMaterialTemplate, SkillVfxLibrary.SoftCircle, false,
            new Color(0f, 0f, 0f, 0.5f), owned);
        SkillVfxLibrary.CreateFlatQuad("Icon_Back", freezeIcon, backMat, 0.9f, 0f);

        for (int i = 0; i < 2; i++)
        {
            var bar = SkillVfxLibrary.CreateFlatQuad("Icon_Bar" + i, freezeIcon, matSolidAlpha, 1f, 0.01f);
            bar.transform.localScale = new Vector3(0.13f, 0.4f, 1f);
            bar.transform.localPosition = new Vector3(i == 0 ? -0.1f : 0.1f, 0.01f, 0f);
        }
        freezeIconRenderers = freezeIcon.GetComponentsInChildren<Renderer>(true);

        // 시작 순간 퍼지는 고리
        freezeShockMat = SkillVfxLibrary.CreateMaterial(particleMaterialTemplate, SkillVfxLibrary.Ring, true, freezeColor, owned);
        freezeShock = SkillVfxLibrary.CreateFlatQuad("Freeze_Shock", freezeRoot, freezeShockMat, 1f, 0.07f);

        SetFreezeVisible(false);
    }

    LineRenderer CreateLine(string name, Transform parent, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.loop = false;
        lr.alignment = LineAlignment.TransformZ;
        lr.sharedMaterial = matSolidAlpha;
        lr.widthMultiplier = freezeGaugeWidth;
        lr.startColor = color;
        lr.endColor = color;
        lr.numCapVertices = 2;
        lr.positionCount = 0;
        SkillVfxLibrary.NoShadow(lr);
        return lr;
    }

    // ───────────────────────── 진행 ─────────────────────────

    IEnumerator DashRoutine(float duration)
    {
        dashTrail.Clear();
        dashTrail.emitting = true;
        dashDust.Play();
        dashBurst.Play();
        dashBurst.Emit(16);
        dashStreaks.Play();
        var ste = dashStreaks.emission;
        ste.rateOverTime = dashStreakRate;

        yield return new WaitForSeconds(duration);

        dashRoutine = null;
        EndDash();
    }

    void EndDash()
    {
        if (!built) return;
        dashTrail.emitting = false;
        dashDust.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        var ste = dashStreaks.emission;
        ste.rateOverTime = 0f;
        dashStreaks.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }

    void TickShield(float dt)
    {
        shieldRemaining -= dt;
        if (shieldRemaining <= 0f)
        {
            StopShield();
            return;
        }

        shieldHitFlash = Mathf.MoveTowards(shieldHitFlash, 0f, dt / 0.15f);
        float elapsed = shieldDuration - shieldRemaining;
        float pulse = Mathf.Sin(elapsed * Mathf.PI * 2f * 2.5f);

        shieldRoot.localScale = Vector3.one * (1f + 0.04f * pulse + 0.12f * shieldHitFlash);
        SkillVfxLibrary.SetAlpha(shieldFillMat, shieldColor, 0.16f + 0.04f * pulse + 0.35f * shieldHitFlash);
        SkillVfxLibrary.SetAlpha(shieldRimMat, shieldColor, 0.85f + 0.15f * shieldHitFlash);

        // 끝나기 직전 깜빡임
        bool visible = true;
        if (shieldRemaining <= shieldWarnTime)
            visible = Mathf.FloorToInt(shieldRemaining / shieldBlinkInterval) % 2 == 0;
        SetShieldVisible(visible);
    }

    void TickFreeze(float dt)
    {
        freezeRemaining -= dt;
        if (freezeRemaining <= 0f)
        {
            StopFreeze();
            return;
        }

        SetArc(gaugeFg, freezeRemaining / freezeDuration);

        float elapsed = freezeDuration - freezeRemaining;
        freezeIcon.localPosition = new Vector3(0f,
            freezeIconHeight + Mathf.Sin(elapsed * Mathf.PI * 2f * 1.2f) * 0.06f, 0f);

        if (freezeShock.enabled)
        {
            freezeShockElapsed += dt;
            float k = Mathf.Clamp01(freezeShockElapsed / FreezeShockTime);
            float size = Mathf.Lerp(0.3f, 1.8f, k);
            freezeShock.transform.localScale = new Vector3(size, size, 1f);
            SkillVfxLibrary.SetAlpha(freezeShockMat, freezeColor, 1f - k);
            if (k >= 1f) freezeShock.enabled = false;
        }
    }

    void SetArc(LineRenderer lr, float fraction)
    {
        if (fraction <= 0f)
        {
            lr.positionCount = 0;
            return;
        }

        int count = Mathf.Max(2, Mathf.CeilToInt(GaugeSegments * fraction) + 1);
        lr.positionCount = count;
        for (int i = 0; i < count; i++)
        {
            float t = (float)i / (count - 1) * fraction;
            float angle = Mathf.PI * 0.5f - t * Mathf.PI * 2f;
            lr.SetPosition(i, new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * freezeGaugeRadius);
        }
    }

    void SetShieldVisible(bool visible)
    {
        shieldFill.enabled = visible;
        shieldRim.enabled = visible;
        shieldGround.enabled = visible;
    }

    void SetFreezeVisible(bool visible)
    {
        gaugeBg.enabled = visible;
        gaugeFg.enabled = visible;
        for (int i = 0; i < freezeIconRenderers.Length; i++) freezeIconRenderers[i].enabled = visible;
        freezeShock.enabled = visible;
        if (visible)
        {
            freezeShock.transform.localScale = new Vector3(0.3f, 0.3f, 1f);
            SkillVfxLibrary.SetAlpha(freezeShockMat, freezeColor, 1f);
        }
    }
}
