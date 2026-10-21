using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

// 스킬 종류 4가지(docs/feature-expansion.md 2장).
public enum SkillType
{
    Accelerate,
    Invincible,
    Stop,
    Flare
}

// 스킬 하나의 씬(스테이지)별 설정값. 인스펙터에서 스테이지마다 다르게 지정한다.
[Serializable]
public class SkillConfig
{
    public SkillType type;
    public Sprite icon;
    [Min(0)] public int totalUses;
    public float duration;
    // 스테이지 시작 시 잠긴 상태로 시작할지 — 잠긴 동안은 버튼 자체가 안 보이고(횟수 0으로 노출),
    // SkillManager.UnlockSkill()이 호출되면 totalUses만큼 실제로 해금된다(스테이지2 튜토리얼용).
    public bool startLocked;
}

// 스킬 하나의 현재 상태 스냅샷 — UI가 이 값만 보고 그린다.
public readonly struct SkillState
{
    public readonly SkillType Type;
    public readonly Sprite Icon;
    public readonly int Remaining;
    public readonly int Total;
    public readonly bool IsActive;
    public readonly float ActiveRatio; // 지속시간 동안 1→0으로 줄어드는 게이지 비율

    public SkillState(SkillType type, Sprite icon, int remaining, int total, bool isActive, float activeRatio)
    {
        Type = type;
        Icon = icon;
        Remaining = remaining;
        Total = total;
        IsActive = isActive;
        ActiveRatio = activeRatio;
    }
}

/// 스킬 4종(가속/무적/정지/조명탄)의 남은 횟수·효과 적용/해제를 관리한다(docs/feature-expansion.md 2장).
/// 스킬별 사용 횟수는 씬(스테이지)마다 인스펙터에서 정하며, 0이면 그 스킬은 버튼 자체가 숨겨진다.
/// 같은 스킬은 효과가 끝나기 전까지 중복 사용할 수 없고, 서로 다른 스킬은 동시에 쓸 수 있다.
/// 시각 연출은 이 클래스가 아니라 `SkillVfxController`/`FlareVfx`가 전담한다(docs/skill-vfx.md) —
/// 여기서는 그 둘을 트리거만 하고, 실제 게임 효과(속도 배율/무적 플래그/정지/시야 등록)만 갖고 있다.
public class SkillManager : MonoBehaviour
{
    [SerializeField] private SkillConfig[] skills;
    [SerializeField] private PlayerAutoMover playerAutoMover;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private StageManager stageManager;
    [SerializeField] private WallPlacementManager wallPlacementManager; // 조명탄 조준 중 벽 배치 입력을 잠근다

    [Header("스킬 수치")]
    [SerializeField] private float accelerateSpeedMultiplier = 2f;
    [SerializeField] private float flareRadius = 5f;
    [SerializeField] private float flareGroundHeight = 0f; // 바닥 클릭 판정용 평면 높이

    [Header("시각 연출 (docs/skill-vfx.md)")]
    [SerializeField] private SkillVfxController vfx; // Player에 붙임
    [SerializeField] private FlareVfx.Settings flareVfxSettings;

    private int[] remainingUses;
    private int[] effectiveTotal; // 화면에 노출되는 총 횟수 — 잠긴 스킬은 해금 전까지 0
    private bool[] isActive;
    private float[] activeEndTime;
    private float[] activeDuration;
    private bool resultShown;
    private bool awaitingFlareTarget;
    private int pendingFlareIndex = -1;
    private Transform activeFlareSource; // VisionSourceRegistry 등록용 표식(시각 요소 없음)
    // 정지 스킬만 다시 누르면 지속시간이 남아있어도 즉시 풀리게 하기 위해 코루틴 참조를 따로 들고 있는다.
    private Coroutine stopRoutine;

    // 조명탄 조준 중 마우스 위치에 밝힐 범위를 미리 보여주는 원(확정 전까지만 존재)
    private Transform flareAimPreview;
    private Material flareAimFillMat, flareAimEdgeMat;
    private readonly List<UnityEngine.Object> flareAimOwned = new List<UnityEngine.Object>();

    // UI가 구독해서 전체 버튼을 다시 그린다(스킬 개수가 적어 개별 diff 없이 매번 전부 갱신해도 충분함).
    public event Action OnSkillStateChanged;
    // 스킬을 실제로 사용한 순간(효과 즉시 발동형은 그 즉시, 조명탄은 조준 시작 시점) 발행 —
    // TutorialManager가 "이 스킬을 실제로 써야 진행" 게이트에 구독해서 쓴다.
    public event Action<SkillType> OnSkillUsed;

    public int SkillCount => skills != null ? skills.Length : 0;

    // CanUse()의 결과가 SkillManager 자신의 상태 변화 없이 바뀔 수 있는 경우(예: 튜토리얼 팝업이 닫혀서
    // TutorialManager.IsOpen만 바뀐 경우) UI를 강제로 다시 그리게 한다. SkillBarUI는 OnSkillStateChanged에만
    // 반응하므로, 이런 "외부 요인" 변화는 이걸 호출해줘야 반영된다.
    public void RefreshUI() => OnSkillStateChanged?.Invoke();

    private void Awake()
    {
        EnsureInitialized();
    }

    // SkillBarUI 등 다른 오브젝트의 OnEnable이 이 컴포넌트의 Awake보다 먼저 실행될 수 있어(스크립트 실행 순서
    // 미보장), 배열이 필요한 시점에 항상 이 함수로 초기화 여부를 보장한다.
    private void EnsureInitialized()
    {
        if (remainingUses != null) return;
        int count = skills != null ? skills.Length : 0;
        remainingUses = new int[count];
        effectiveTotal = new int[count];
        isActive = new bool[count];
        activeEndTime = new float[count];
        activeDuration = new float[count];
        for (int i = 0; i < count; i++)
        {
            effectiveTotal[i] = skills[i].startLocked ? 0 : skills[i].totalUses;
            remainingUses[i] = effectiveTotal[i];
        }
    }

    // 잠긴 스킬을 해금한다 — 설정된 totalUses만큼 실제로 사용 가능해지고, 버튼이 화면에 나타난다.
    // 이미 해금된(또는 잠기지 않은) 스킬에 호출하면 아무 효과 없다.
    public void UnlockSkill(SkillType type)
    {
        EnsureInitialized();
        for (int i = 0; i < skills.Length; i++)
        {
            if (skills[i].type != type) continue;
            if (effectiveTotal[i] >= skills[i].totalUses) return; // 이미 해금됨
            effectiveTotal[i] = skills[i].totalUses;
            remainingUses[i] = skills[i].totalUses;
            OnSkillStateChanged?.Invoke();
            return;
        }
    }

    private void OnEnable()
    {
        if (stageManager != null)
        {
            stageManager.OnCleared += HandleResultShown;
            stageManager.OnFailed += HandleResultShown;
            stageManager.OnRetried += HandleRetry;
        }
        if (playerHealth != null) playerHealth.OnBlockedByInvincibility += HandleShieldBlocked;
    }

    private void OnDisable()
    {
        if (stageManager != null)
        {
            stageManager.OnCleared -= HandleResultShown;
            stageManager.OnFailed -= HandleResultShown;
            stageManager.OnRetried -= HandleRetry;
        }
        if (playerHealth != null) playerHealth.OnBlockedByInvincibility -= HandleShieldBlocked;
    }

    private void Update()
    {
        if (Keyboard.current != null)
        {
            if (Keyboard.current.digit1Key.wasPressedThisFrame) TryUseSkill(0);
            else if (Keyboard.current.digit2Key.wasPressedThisFrame) TryUseSkill(1);
            else if (Keyboard.current.digit3Key.wasPressedThisFrame) TryUseSkill(2);
            else if (Keyboard.current.digit4Key.wasPressedThisFrame) TryUseSkill(3);
        }

        if (awaitingFlareTarget) HandleFlareTargeting();
    }

    // 조명탄 조준 중 — 매 프레임 마우스 위치로 미리보기 원을 따라오게 하고, 클릭하면 그 지점을 착지 지점으로 확정한다.
    private void HandleFlareTargeting()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        Ray ray = cam.ScreenPointToRay(Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero);
        Plane ground = new Plane(Vector3.up, new Vector3(0f, flareGroundHeight, 0f));
        if (!ground.Raycast(ray, out float enter)) return;

        Vector3 hoverPosition = ray.GetPoint(enter);
        UpdateFlareAimPreview(hoverPosition);

        if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame) return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        awaitingFlareTarget = false;
        if (wallPlacementManager != null) wallPlacementManager.InputLocked = false;
        ClearFlareAimPreview();
        LaunchFlare(pendingFlareIndex, hoverPosition);
    }

    // 조준 중 마우스 주변에 조명탄이 밝힐 범위를 원(채움+테두리)으로 미리 보여준다.
    // 실제 조명탄 이펙트(FlareVfx)와 같은 색·높이를 써서 "이 자리에 그 효과가 생긴다"는 것을 자연스럽게 연결한다.
    private void EnsureFlareAimPreview(Vector3 startPosition)
    {
        if (flareAimPreview != null) return;

        var root = new GameObject("FlareAimPreview").transform;
        root.position = startPosition;

        Color glow = flareVfxSettings != null ? flareVfxSettings.glowColor : new Color(1f, 0.62f, 0.25f, 1f);
        Material template = flareVfxSettings != null ? flareVfxSettings.particleMaterialTemplate : null;
        float discHeight = flareVfxSettings != null ? flareVfxSettings.groundDiscHeight : 0.62f;

        flareAimFillMat = SkillVfxLibrary.CreateMaterial(template, SkillVfxLibrary.SoftCircle, true,
            new Color(glow.r, glow.g, glow.b, 0.22f), flareAimOwned);
        SkillVfxLibrary.CreateFlatQuad("AimFill", root, flareAimFillMat, flareRadius * 2f, discHeight);

        flareAimEdgeMat = SkillVfxLibrary.CreateMaterial(template, SkillVfxLibrary.Ring, true,
            new Color(glow.r, glow.g, glow.b, 0.75f), flareAimOwned);
        SkillVfxLibrary.CreateFlatQuad("AimEdge", root, flareAimEdgeMat, flareRadius * 2f / SkillVfxLibrary.RingPeak, discHeight + 0.01f);

        flareAimPreview = root;
    }

    private void UpdateFlareAimPreview(Vector3 position)
    {
        EnsureFlareAimPreview(position);
        flareAimPreview.position = position;
    }

    private void ClearFlareAimPreview()
    {
        if (flareAimPreview != null) Destroy(flareAimPreview.gameObject);
        flareAimPreview = null;
        SkillVfxLibrary.DestroyOwned(flareAimOwned);
    }

    // FlareVfx가 발사~비행~착지~페이드를 전담한다. SkillManager는 착지 시점(onLanded)에 맞춰서만
    // 시야 등록을 시작하고, 끝(onFinished)에 해제한다 — 밝힘 시간은 착지 시점부터 센다(docs/skill-vfx.md 3-4).
    private void LaunchFlare(int index, Vector3 targetPosition)
    {
        float duration = skills[index].duration;
        if (flareVfxSettings != null)
        {
            flareVfxSettings.revealRadius = flareRadius;
            flareVfxSettings.revealDuration = duration;
        }

        Vector3 from = playerAutoMover.transform.position + Vector3.up;
        FlareVfx.Launch(from, targetPosition, flareVfxSettings,
            onLanded: () =>
            {
                activeDuration[index] = duration;
                activeEndTime[index] = Time.time + duration;

                GameObject marker = new GameObject("FlareVisionSource");
                marker.transform.position = targetPosition;
                activeFlareSource = marker.transform;
                VisionSourceRegistry.Register(activeFlareSource, flareRadius);
                OnSkillUsed?.Invoke(skills[index].type);
                OnSkillStateChanged?.Invoke();
            },
            onFinished: () =>
            {
                if (activeFlareSource != null)
                {
                    VisionSourceRegistry.Unregister(activeFlareSource);
                    Destroy(activeFlareSource.gameObject);
                    activeFlareSource = null;
                }
                isActive[index] = false;
                OnSkillStateChanged?.Invoke();
            });
    }

    private void HandleResultShown()
    {
        resultShown = true;
        StopAllSkillEffects();
        OnSkillStateChanged?.Invoke();
    }

    // 재도전 시 호출 — 남은 횟수를 전부 복원하고, 진행 중이던 효과는 즉시 종료 처리한다.
    private void HandleRetry()
    {
        EnsureInitialized();
        resultShown = false;
        // 재도전하면 그 스테이지에서 진행한 모든 것이 처음부터 다시 재생되어야 한다는 정책
        // (2026-09-17, 모든 스테이지 공통)에 따라, 튜토리얼로 해금했던 스킬도 다시 잠긴 상태로 되돌린다.
        for (int i = 0; i < skills.Length; i++)
        {
            effectiveTotal[i] = skills[i].startLocked ? 0 : skills[i].totalUses;
            remainingUses[i] = effectiveTotal[i];
        }
        StopAllSkillEffects();
        OnSkillStateChanged?.Invoke();
    }

    // 사망/결과 화면/재도전처럼 스킬 효과가 중간에 끊겨야 하는 모든 경우에 공용으로 쓴다
    // (docs/skill-vfx.md 4-3, 4-4). 게임 로직 상태와 시각 연출(vfx)을 함께 즉시 정리한다.
    private void StopAllSkillEffects()
    {
        for (int i = 0; i < isActive.Length; i++) isActive[i] = false;

        if (playerAutoMover != null)
        {
            playerAutoMover.SetSpeedMultiplier(this, 1f);
            playerAutoMover.SetPaused(false);
        }
        if (playerHealth != null) playerHealth.SetInvincible(false);

        if (awaitingFlareTarget && wallPlacementManager != null) wallPlacementManager.InputLocked = false;
        awaitingFlareTarget = false;
        pendingFlareIndex = -1;
        ClearFlareAimPreview();
        if (activeFlareSource != null)
        {
            VisionSourceRegistry.Unregister(activeFlareSource);
            Destroy(activeFlareSource.gameObject);
            activeFlareSource = null;
        }

        if (vfx != null) vfx.StopAll();
        FlareVfx.ClearAll();
    }

    public SkillState GetState(int index)
    {
        EnsureInitialized();
        SkillConfig cfg = skills[index];
        float ratio = 0f;
        if (isActive[index] && activeDuration[index] > 0f)
        {
            ratio = Mathf.Clamp01((activeEndTime[index] - Time.time) / activeDuration[index]);
        }
        return new SkillState(cfg.type, cfg.icon, remainingUses[index], effectiveTotal[index], isActive[index], ratio);
    }

    public bool CanUse(int index)
    {
        EnsureInitialized();
        if (skills == null || index < 0 || index >= skills.Length) return false;
        if (effectiveTotal[index] <= 0) return false;
        if (remainingUses[index] <= 0) return false;
        if (isActive[index]) return false;
        if (playerHealth != null && playerHealth.IsDead) return false;
        if (resultShown) return false;
        // 튜토리얼이 열려있어도, "이 스킬을 실제로 써야 진행" 게이트가 지금 이 스킬을 기다리는 중이면 허용한다.
        if (TutorialManager.IsOpen && !(TutorialManager.IsAwaitingSkillUse && TutorialManager.RequiredSkillType == skills[index].type)) return false;
        return true;
    }

    public void TryUseSkill(int index)
    {
        // 정지 스킬은 사용 중일 때 다시 누르면(같은 버튼/숫자키) 지속시간이 남아있어도 즉시 풀린다
        // (사용자 요청, 2026-09-18) — 추가 사용 횟수는 소모하지 않는다.
        if (skills != null && index >= 0 && index < skills.Length
            && skills[index].type == SkillType.Stop && isActive[index])
        {
            CancelStop(index);
            return;
        }

        // 조명탄은 조준 중(아직 착지 전 — 실제 "사용"으로 안 침) 다시 누르면 조준을 취소한다
        // (사용자 요청, 2026-09-18) — 실제 사용이 아니므로 이미 차감했던 횟수를 되돌린다.
        if (skills != null && index >= 0 && index < skills.Length
            && skills[index].type == SkillType.Flare && awaitingFlareTarget && pendingFlareIndex == index)
        {
            CancelFlareAim(index);
            return;
        }

        if (!CanUse(index)) return;

        remainingUses[index]--;
        isActive[index] = true;

        if (skills[index].type == SkillType.Flare)
        {
            // 조준 중에는 지속시간 게이지를 표시하지 않는다 — 착지 시점(LaunchFlare의 onLanded)부터 카운트.
            activeDuration[index] = 0f;
            activeEndTime[index] = 0f;
            pendingFlareIndex = index;
            awaitingFlareTarget = true;
            if (wallPlacementManager != null) wallPlacementManager.InputLocked = true;
            // ⚠️ 조명탄은 버튼을 누른 시점(OnSkillUsed)이 아니라 실제로 목표 지점에 착지하는 시점에
            // "진짜 사용"으로 친다(2026-09-17, 사용자 요청) — LaunchFlare()의 onLanded에서 발행.
        }
        else
        {
            activeDuration[index] = skills[index].duration;
            activeEndTime[index] = Time.time + skills[index].duration;
            OnSkillUsed?.Invoke(skills[index].type);

            switch (skills[index].type)
            {
                case SkillType.Accelerate: StartCoroutine(RunAccelerate(index)); break;
                case SkillType.Invincible: StartCoroutine(RunInvincible(index)); break;
                case SkillType.Stop: stopRoutine = StartCoroutine(RunStop(index)); break;
            }
        }

        OnSkillStateChanged?.Invoke();
    }

    private void HandleShieldBlocked()
    {
        if (vfx != null) vfx.PulseShieldHit();
    }

    private IEnumerator RunAccelerate(int index)
    {
        playerAutoMover.SetSpeedMultiplier(this, accelerateSpeedMultiplier);
        if (vfx != null) vfx.PlayDash(activeDuration[index]);

        yield return WaitScaled(activeDuration[index]);

        playerAutoMover.SetSpeedMultiplier(this, 1f);
        isActive[index] = false;
        OnSkillStateChanged?.Invoke();
    }

    private IEnumerator RunInvincible(int index)
    {
        playerHealth.SetInvincible(true);
        if (vfx != null) vfx.PlayShield(activeDuration[index]);

        yield return WaitScaled(activeDuration[index]);

        playerHealth.SetInvincible(false);
        isActive[index] = false;
        OnSkillStateChanged?.Invoke();
    }

    private IEnumerator RunStop(int index)
    {
        playerAutoMover.SetPaused(true);
        if (vfx != null) vfx.PlayFreeze(activeDuration[index]);

        yield return WaitScaled(activeDuration[index]);

        playerAutoMover.SetPaused(false);
        isActive[index] = false;
        stopRoutine = null;
        OnSkillStateChanged?.Invoke();
    }

    // TryUseSkill()에서 정지 스킬을 사용 중에 다시 눌렀을 때 호출 — 남은 지속시간을 버리고 즉시 풀어준다.
    private void CancelStop(int index)
    {
        if (stopRoutine != null)
        {
            StopCoroutine(stopRoutine);
            stopRoutine = null;
        }
        if (playerAutoMover != null) playerAutoMover.SetPaused(false);
        if (vfx != null) vfx.StopFreeze();
        isActive[index] = false;
        OnSkillStateChanged?.Invoke();
    }

    // TryUseSkill()에서 조명탄 조준 중에 다시 눌렀을 때 호출 — 조준만 취소하고, 착지 전이라 실제
    // 사용이 아니므로 버튼을 누른 시점에 미리 차감해뒀던 사용 횟수를 되돌린다.
    private void CancelFlareAim(int index)
    {
        awaitingFlareTarget = false;
        pendingFlareIndex = -1;
        if (wallPlacementManager != null) wallPlacementManager.InputLocked = false;
        ClearFlareAimPreview();
        remainingUses[index]++;
        isActive[index] = false;
        OnSkillStateChanged?.Invoke();
    }

    // 튜토리얼 정지(timeScale=0) 중에는 지속시간도 함께 멈춰야 하므로 scaled time을 쓴다.
    private static IEnumerator WaitScaled(float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            yield return null;
        }
    }
}
