using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 조명탄 이펙트: 발사 → 포물선 비행 → 착지 섬광 → 밝힘 유지 → 사라짐.
/// 안개를 걷거나 몬스터를 드러내는 실제 기능은 하지 않는다.
/// 착지 시 onLanded에서 SkillManager가 시야 원을 등록하고,
/// 밝힘 시간이 끝나면 onFinished가 호출된다(시야 원 해제에 사용 가능).
///
/// 사용:
///   FlareVfx.Launch(발사위치, 착지위치, settings, onLanded: ..., onFinished: ...);
///   재도전 시 FlareVfx.ClearAll();
///
/// 바닥 높이는 Y=0 기준이다.
/// </summary>
public class FlareVfx : MonoBehaviour
{
    [Serializable]
    public class Settings
    {
        public float flightTime = 0.5f;
        public float arcHeight = 3f;
        [Tooltip("착지 후 밝히는 시간. SkillManager의 조명탄 지속시간과 같은 값을 넘길 것")]
        public float revealDuration = 3f;
        [Tooltip("표시용 반경. SkillManager의 조명탄 반경과 같은 값을 넘길 것")]
        public float revealRadius = 5f;
        public float fadeOutTime = 0.5f;
        [Tooltip("바닥 빛이 그려지는 높이. 안개 평면(Y 0.6)보다 살짝 위")]
        public float groundDiscHeight = 0.62f;
        public Color coreColor = new Color(1f, 0.9f, 0.6f, 1f);
        public Color glowColor = new Color(1f, 0.62f, 0.25f, 1f);
        [Tooltip("URP 파이프라인 에셋에서 Additional Lights가 꺼져 있으면 빛이 안 보인다(바닥 빛 원은 그대로 보임)")]
        public bool useLight = true;
        public float lightIntensity = 3f;
        public Material particleMaterialTemplate;
    }

    const float FlashTime = 0.18f;

    static readonly List<FlareVfx> active = new List<FlareVfx>();

    public static FlareVfx Launch(Vector3 from, Vector3 to, Settings settings,
        Action onLanded = null, Action onFinished = null)
    {
        var go = new GameObject("VFX_Flare");
        var fx = go.AddComponent<FlareVfx>();
        fx.Begin(from, to, settings ?? new Settings(), onLanded, onFinished);
        return fx;
    }

    /// <summary>진행 중인 조명탄 이펙트를 즉시 제거(콜백은 호출하지 않음)</summary>
    public static void ClearAll()
    {
        for (int i = active.Count - 1; i >= 0; i--)
        {
            if (active[i] != null) Destroy(active[i].gameObject);
        }
        active.Clear();
    }

    readonly List<UnityEngine.Object> owned = new List<UnityEngine.Object>();
    Settings s;
    Action onLanded, onFinished;
    Vector3 from, to;

    Material matSoftAdd;

    Transform head;
    MeshRenderer headCore, headHalo;
    TrailRenderer headTrail;
    ParticleSystem headSparks;

    Transform landRoot;
    MeshRenderer flash, disc, edge;
    Material flashMat, discMat, edgeMat;
    ParticleSystem burst, embers;
    Light glow;

    void Awake() => active.Add(this);

    void OnDestroy()
    {
        active.Remove(this);
        SkillVfxLibrary.DestroyOwned(owned);
    }

    void Begin(Vector3 from, Vector3 to, Settings settings, Action onLanded, Action onFinished)
    {
        s = settings;
        this.from = from;
        this.to = to;
        this.onLanded = onLanded;
        this.onFinished = onFinished;
        transform.position = from;

        matSoftAdd = SkillVfxLibrary.CreateMaterial(s.particleMaterialTemplate, SkillVfxLibrary.SoftCircle, true, Color.white, owned);
        BuildHead();
        StartCoroutine(Run());
    }

    void BuildHead()
    {
        head = new GameObject("Flare_Head").transform;
        head.SetParent(transform, false);

        var coreMat = SkillVfxLibrary.CreateMaterial(s.particleMaterialTemplate, null, true, s.coreColor, owned);
        var core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        core.name = "Flare_Core";
        SkillVfxLibrary.StripCollider(core);
        core.transform.SetParent(head, false);
        core.transform.localScale = Vector3.one * 0.25f;
        headCore = core.GetComponent<MeshRenderer>();
        headCore.sharedMaterial = coreMat;
        SkillVfxLibrary.NoShadow(headCore);

        var haloMat = SkillVfxLibrary.CreateMaterial(s.particleMaterialTemplate, SkillVfxLibrary.SoftCircle, true,
            new Color(s.glowColor.r, s.glowColor.g, s.glowColor.b, 0.8f), owned);
        headHalo = SkillVfxLibrary.CreateFlatQuad("Flare_Halo", head, haloMat, 0.9f, 0f);

        var trailMat = SkillVfxLibrary.CreateMaterial(s.particleMaterialTemplate, null, true, Color.white, owned);
        headTrail = head.gameObject.AddComponent<TrailRenderer>();
        headTrail.time = 0.35f;
        headTrail.minVertexDistance = 0.05f;
        headTrail.widthCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0f));
        headTrail.widthMultiplier = 0.22f;
        var g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(s.coreColor, 0f), new GradientColorKey(s.glowColor, 1f) },
            new[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0f, 1f) });
        headTrail.colorGradient = g;
        headTrail.sharedMaterial = trailMat;
        SkillVfxLibrary.NoShadow(headTrail);

        headSparks = SkillVfxLibrary.CreateParticleSystem("Flare_Sparks", head, matSoftAdd);
        var m = headSparks.main;
        m.loop = true;
        m.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.4f);
        m.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.14f);
        m.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.6f);
        m.startColor = s.coreColor;
        m.maxParticles = 40;
        var e = headSparks.emission;
        e.rateOverTime = 35f;
        var sh = headSparks.shape;
        sh.enabled = true;
        sh.shapeType = ParticleSystemShapeType.Sphere;
        sh.radius = 0.05f;
        SkillVfxLibrary.SizeOverLifetime(headSparks, 1f, 0.2f);
        SkillVfxLibrary.FadeOverLifetime(headSparks, 0.2f);
        headSparks.Play();
    }

    void BuildLanding()
    {
        landRoot = new GameObject("Flare_Land").transform;
        landRoot.SetParent(transform, false);
        landRoot.position = new Vector3(to.x, 0f, to.z);
        landRoot.rotation = Quaternion.identity;

        float r = s.revealRadius;

        // 착지 섬광
        flashMat = SkillVfxLibrary.CreateMaterial(s.particleMaterialTemplate, SkillVfxLibrary.SoftCircle, true, s.coreColor, owned);
        flash = SkillVfxLibrary.CreateFlatQuad("Flare_Flash", landRoot, flashMat, 0.5f, s.groundDiscHeight + 0.05f);

        // 밝힌 범위의 바닥 빛
        discMat = SkillVfxLibrary.CreateMaterial(s.particleMaterialTemplate, SkillVfxLibrary.SoftCircle, true,
            new Color(s.glowColor.r, s.glowColor.g, s.glowColor.b, 0f), owned);
        disc = SkillVfxLibrary.CreateFlatQuad("Flare_Disc", landRoot, discMat, r * 2f, s.groundDiscHeight);

        // 밝힌 범위의 경계선
        edgeMat = SkillVfxLibrary.CreateMaterial(s.particleMaterialTemplate, SkillVfxLibrary.Ring, true,
            new Color(s.glowColor.r, s.glowColor.g, s.glowColor.b, 0f), owned);
        edge = SkillVfxLibrary.CreateFlatQuad("Flare_Edge", landRoot, edgeMat, r * 2f / SkillVfxLibrary.RingPeak, s.groundDiscHeight + 0.01f);

        // 착지 불꽃
        burst = SkillVfxLibrary.CreateParticleSystem("Flare_Burst", landRoot, matSoftAdd);
        burst.transform.localPosition = new Vector3(0f, s.groundDiscHeight + 0.1f, 0f);
        var bm = burst.main;
        bm.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.7f);
        bm.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.2f);
        bm.startSpeed = new ParticleSystem.MinMaxCurve(3f, 6f);
        bm.startColor = s.coreColor;
        bm.gravityModifier = 0.8f;
        bm.maxParticles = 60;
        var bs = burst.shape;
        bs.enabled = true;
        bs.shapeType = ParticleSystemShapeType.Cone;
        bs.angle = 70f;
        bs.radius = 0.2f;
        bs.rotation = new Vector3(-90f, 0f, 0f);
        SkillVfxLibrary.SizeOverLifetime(burst, 1f, 0.2f);
        SkillVfxLibrary.FadeOverLifetime(burst, 0.3f);

        // 밝히는 동안 천천히 떠오르는 불씨
        embers = SkillVfxLibrary.CreateParticleSystem("Flare_Embers", landRoot, matSoftAdd);
        embers.transform.localPosition = new Vector3(0f, s.groundDiscHeight, 0f);
        var em = embers.main;
        em.loop = true;
        em.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 1.8f);
        em.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.12f);
        em.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.3f);
        em.startColor = s.glowColor;
        em.maxParticles = 40;
        var ee = embers.emission;
        ee.rateOverTime = 12f;
        var es = embers.shape;
        es.enabled = true;
        es.shapeType = ParticleSystemShapeType.Circle;
        es.radius = r * 0.6f;
        es.rotation = new Vector3(90f, 0f, 0f);
        var vol = embers.velocityOverLifetime;
        vol.enabled = true;
        vol.space = ParticleSystemSimulationSpace.Local;
        vol.x = new ParticleSystem.MinMaxCurve(0f);
        vol.y = new ParticleSystem.MinMaxCurve(0.6f);
        vol.z = new ParticleSystem.MinMaxCurve(0f);
        SkillVfxLibrary.FadeOverLifetime(embers, 0.3f);

        if (s.useLight)
        {
            var lightGo = new GameObject("Flare_Light");
            lightGo.transform.SetParent(landRoot, false);
            lightGo.transform.localPosition = new Vector3(0f, 2.5f, 0f);
            glow = lightGo.AddComponent<Light>();
            glow.type = LightType.Point;
            glow.range = r * 1.6f;
            glow.color = s.glowColor;
            glow.intensity = 0f;
            glow.shadows = LightShadows.None;
        }
    }

    IEnumerator Run()
    {
        // 1) 비행
        // ⚠️ unscaledDeltaTime 사용(2026-09-17): 조명탄은 "실제로 착지해야 튜토리얼이 넘어가는" 게이트에
        // 쓰이는데, 그 튜토리얼이 열려있는 동안은 Time.timeScale=0이라 scaled deltaTime을 쓰면 비행이
        // 영원히 안 끝나(착지를 못 해) 게이트가 절대 안 풀리는 소프트락이 생긴다.
        float flight = Mathf.Max(0.05f, s.flightTime);
        float t = 0f;
        while (t < flight)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / flight);
            float h = 4f * k * (1f - k);
            head.position = Vector3.Lerp(from, to, k) + Vector3.up * (s.arcHeight * h);
            head.localScale = Vector3.one * (1f + 0.6f * h); // 높이 올라갈수록 크게 보여 고도감
            yield return null;
        }

        head.position = to;
        headCore.enabled = false;
        headHalo.enabled = false;
        headTrail.emitting = false;
        headSparks.Stop(true, ParticleSystemStopBehavior.StopEmitting);

        // 2) 착지
        BuildLanding();
        burst.Play();
        burst.Emit(40);
        embers.Play();
        onLanded?.Invoke();

        // 3) 섬광 + 밝힘 유지 + 페이드
        float duration = Mathf.Max(0.01f, s.revealDuration);
        float fadeOut = Mathf.Clamp(s.fadeOutTime, 0.01f, duration);
        float hold = duration - fadeOut;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float dt = Time.unscaledDeltaTime;
            elapsed += dt;

            if (flash.enabled)
            {
                float fk = Mathf.Clamp01(elapsed / FlashTime);
                float size = Mathf.Lerp(0.5f, s.revealRadius * 0.9f, fk);
                flash.transform.localScale = new Vector3(size, size, 1f);
                SkillVfxLibrary.SetAlpha(flashMat, s.coreColor, 1f - fk);
                if (fk >= 1f) flash.enabled = false;
            }

            float rampIn = Mathf.Clamp01(elapsed / 0.15f);
            float fade = elapsed <= hold ? 1f : 1f - Mathf.Clamp01((elapsed - hold) / fadeOut);
            float pulse = 1f + 0.08f * Mathf.Sin(elapsed * Mathf.PI * 2f * 1.5f);

            SkillVfxLibrary.SetAlpha(discMat, s.glowColor, 0.3f * fade * rampIn * pulse);
            SkillVfxLibrary.SetAlpha(edgeMat, s.glowColor, 0.55f * fade * rampIn);
            if (glow != null) glow.intensity = s.lightIntensity * fade * rampIn * pulse;

            if (elapsed > hold && embers.isEmitting)
                embers.Stop(true, ParticleSystemStopBehavior.StopEmitting);

            yield return null;
        }

        flash.enabled = false;
        disc.enabled = false;
        edge.enabled = false;
        if (glow != null) glow.enabled = false;
        onFinished?.Invoke();

        // 4) 남은 입자가 사라질 때까지 기다렸다가 정리
        yield return new WaitForSecondsRealtime(2f);
        Destroy(gameObject);
    }
}
