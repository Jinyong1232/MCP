using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 스킬 이펙트 공용 도구.
/// 텍스처, 머티리얼, 파티클 기본 세팅을 코드로 만들어서 별도 에셋 없이 이펙트를 구성한다.
/// </summary>
public static class SkillVfxLibrary
{
    public const string ParticleShaderName = "Universal Render Pipeline/Particles/Unlit";

    // 안개(FogOfWarGround, 3000)보다 나중에 그려서 이펙트가 안개에 묻히지 않게 한다
    public const int RenderQueue = 3100;

    // Ring 텍스처에서 고리가 가장 진한 지점(반지름 대비 비율)
    public const float RingPeak = 0.86f;

    static Texture2D softCircle;
    static Texture2D ring;
    static Texture2D hardCircle;

    /// <summary>가운데가 진하고 바깥으로 흐려지는 원(중심부터 완만하게 옅어짐 — 은은한 채움용)</summary>
    public static Texture2D SoftCircle => softCircle != null ? softCircle : (softCircle = BuildRadial(64, RadialMode.Soft));

    /// <summary>가장자리 근처만 진한 고리</summary>
    public static Texture2D Ring => ring != null ? ring : (ring = BuildRadial(128, RadialMode.Ring));

    /// <summary>가장자리 근처(바깥 15%)만 부드럽게 페이드되고 나머지는 꽉 찬 알파 — 눈에 잘 띄어야 하는
    /// 경고 표시 등에 씀(SoftCircle은 중심에서부터 계속 옅어져서 경고용으로 쓰기엔 너무 은은함).</summary>
    public static Texture2D HardCircle => hardCircle != null ? hardCircle : (hardCircle = BuildRadial(64, RadialMode.Hard));

    enum RadialMode { Soft, Ring, Hard }

    static Texture2D BuildRadial(int size, RadialMode mode)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            name = "VFX_" + mode
        };

        var px = new Color32[size * size];
        float half = (size - 1) * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x - half) / half;
                float dy = (y - half) / half;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a;
                switch (mode)
                {
                    case RadialMode.Ring:
                        a = Mathf.Clamp01(1f - Mathf.Abs(d - RingPeak) / 0.12f);
                        a *= a;
                        break;
                    case RadialMode.Hard:
                        a = 1f - Mathf.Clamp01((d - 0.85f) / 0.15f);
                        break;
                    default:
                        a = Mathf.Clamp01(1f - d);
                        a *= a;
                        break;
                }
                px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
        }
        tex.SetPixels32(px);
        tex.Apply(false, true);
        return tex;
    }

    /// <summary>
    /// 투명 머티리얼 생성. template이 있으면 복사해서 쓰고, 없으면 Shader.Find로 만든다.
    /// 빌드에서는 Shader.Find가 실패할 수 있으니 인스펙터에 템플릿 지정을 권장.
    /// 생성한 머티리얼은 owned에 담아두고, 소유자가 파괴될 때 DestroyOwned로 정리한다.
    /// </summary>
    public static Material CreateMaterial(Material template, Texture tex, bool additive, Color tint, List<Object> owned)
    {
        Material m;
        if (template != null)
        {
            m = new Material(template);
        }
        else
        {
            var shader = Shader.Find(ParticleShaderName);
            if (shader == null)
            {
                Debug.LogWarning($"[SkillVfx] '{ParticleShaderName}' 셰이더를 찾지 못했습니다. 인스펙터에 파티클 머티리얼 템플릿을 지정하세요.");
                shader = Shader.Find("Sprites/Default");
            }
            m = new Material(shader);
        }

        m.name = additive ? "VFX_Additive (Runtime)" : "VFX_Alpha (Runtime)";
        SetupTransparent(m, additive);

        if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex);
        else m.mainTexture = tex;

        SetColor(m, tint);
        m.renderQueue = RenderQueue;
        owned?.Add(m);
        return m;
    }

    static void SetupTransparent(Material m, bool additive)
    {
        SetFloatIfExists(m, "_Surface", 1f);
        SetFloatIfExists(m, "_Blend", additive ? 2f : 0f);
        SetFloatIfExists(m, "_SrcBlend", (float)BlendMode.SrcAlpha);
        SetFloatIfExists(m, "_DstBlend", additive ? (float)BlendMode.One : (float)BlendMode.OneMinusSrcAlpha);
        SetFloatIfExists(m, "_SrcBlendAlpha", (float)BlendMode.One);
        SetFloatIfExists(m, "_DstBlendAlpha", additive ? (float)BlendMode.One : (float)BlendMode.OneMinusSrcAlpha);
        SetFloatIfExists(m, "_ZWrite", 0f);
        SetFloatIfExists(m, "_Cull", (float)CullMode.Off);
        m.SetOverrideTag("RenderType", "Transparent");
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
    }

    static void SetFloatIfExists(Material m, string prop, float value)
    {
        if (m.HasProperty(prop)) m.SetFloat(prop, value);
    }

    public static void SetColor(Material m, Color c)
    {
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        else m.color = c;
    }

    public static void SetAlpha(Material m, Color baseColor, float alpha)
    {
        SetColor(m, new Color(baseColor.r, baseColor.g, baseColor.b, Mathf.Clamp01(alpha)));
    }

    /// <summary>재생 정지 상태로 파티클 시스템을 만든다. 세부 값은 호출한 쪽에서 설정.</summary>
    public static ParticleSystem CreateParticleSystem(string name, Transform parent, Material mat)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);

        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.playOnAwake = false;
        main.loop = false;
        main.duration = 1f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        main.startSpeed = 0f;

        var emission = ps.emission;
        emission.rateOverTime = 0f;

        var shape = ps.shape;
        shape.enabled = false;

        var r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = mat;
        r.renderMode = ParticleSystemRenderMode.Billboard;
        NoShadow(r);
        return ps;
    }

    public static void SizeOverLifetime(ParticleSystem ps, float from, float to)
    {
        var s = ps.sizeOverLifetime;
        s.enabled = true;
        s.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, from, 1f, to));
    }

    /// <summary>수명 동안 hold 비율까지 유지하다가 0으로 사라짐</summary>
    public static void FadeOverLifetime(ParticleSystem ps, float hold = 0.2f)
    {
        var g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, hold), new GradientAlphaKey(0f, 1f) });

        var c = ps.colorOverLifetime;
        c.enabled = true;
        c.color = new ParticleSystem.MinMaxGradient(g);
    }

    /// <summary>바닥에 눕힌 사각판(위에서 보임). 콜라이더와 그림자 제거.</summary>
    public static MeshRenderer CreateFlatQuad(string name, Transform parent, Material mat, float size, float height)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = name;
        StripCollider(go);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(0f, height, 0f);
        go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        go.transform.localScale = new Vector3(size, size, 1f);

        var r = go.GetComponent<MeshRenderer>();
        r.sharedMaterial = mat;
        NoShadow(r);
        return r;
    }

    /// <summary>
    /// 기본 도형에 붙는 콜라이더 제거.
    /// 남아 있으면 투사체나 벽 설치 레이캐스트에 걸린다.
    /// </summary>
    public static void StripCollider(GameObject go)
    {
        var c = go.GetComponent<Collider>();
        if (c == null) return;
        c.enabled = false;
        Object.Destroy(c);
    }

    public static void NoShadow(Renderer r)
    {
        r.shadowCastingMode = ShadowCastingMode.Off;
        r.receiveShadows = false;
        r.lightProbeUsage = LightProbeUsage.Off;
        r.reflectionProbeUsage = ReflectionProbeUsage.Off;
    }

    public static void DestroyOwned(List<Object> owned)
    {
        for (int i = 0; i < owned.Count; i++)
        {
            if (owned[i] != null) Object.Destroy(owned[i]);
        }
        owned.Clear();
    }
}
