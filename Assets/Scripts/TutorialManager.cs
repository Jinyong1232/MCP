using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

/// docs/tutorial.md 기반 튜토리얼 일시정지 시스템. 지정한 트리거가 발생하면 게임을 멈추고(Time.timeScale=0)
/// 순서대로 문구를 보여준다. 일부 페이지는 아무 곳이나 클릭해서는 못 넘어가고, 지정된 벽 슬롯에 실제로
/// 벽을 세워야만 다음으로 진행된다(docs/tutorial.md 3장 "일시정지 ③" 참조).
/// 기존 스크립트(PathManager/PlayerHealth/WallPlacementManager/FogVisibility)를 건드리지 않고
/// 그것들이 이미 들고 있는 이벤트를 구독하는 방식으로만 동작한다.
public class TutorialManager : MonoBehaviour
{
    [SerializeField] private bool tutorialEnabled = true;
    [SerializeField] private TutorialStep[] steps;

    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text bodyText;
    [SerializeField] private TMP_Text hintText;
    [SerializeField] private float dimmedOverlayAlpha = 0.12f;
    [SerializeField] private string hintSlotClickMessage = "표시된 벽 슬롯을 클릭하세요";
    [SerializeField] private string hintSkillUseMessage = "표시된 스킬을 사용해보세요";
    [SerializeField] private string hintAdvanceMessage = "클릭하거나 아무 키나 누르면 계속됩니다";
    // 페이지가 뜬 직후 이 시간 동안은 클릭/키 입력으로 못 넘긴다(직전 클릭이 그대로 이어져 실수로 스킵되는 것 방지).
    [SerializeField] private float minPageDisplayTime = 0.5f;

    [SerializeField] private PathManager pathManager;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private WallPlacementManager wallPlacementManager;
    [SerializeField] private SkillManager skillManager; // 스테이지2 스킬 해금/사용 게이트용(스테이지1엔 없음, null이면 관련 기능 비활성)
    [SerializeField] private StageManager stageManager; // 클리어/실패로 스테이지가 끝나면 열려있는 튜토리얼을 강제로 닫기 위함

    [Header("벽 개수 제한 공개(특정 몬스터 조우 시 전용)")]
    [SerializeField] private GameObject wallLimitUIRoot; // Canvas/CurrencyIcons — 공개 전까지 비활성 상태로 시작
    [SerializeField] private UIHighlightEffect uiHighlight;


    public static bool IsOpen { get; private set; }
    public static bool IsAwaitingSlotClick { get; private set; }
    public static int RequiredSlotTileIndex { get; private set; } = -1;
    public static SlotSide RequiredSlotSide { get; private set; }
    public static bool IsAwaitingSkillUse { get; private set; }
    public static SkillType RequiredSkillType { get; private set; }

    private readonly HashSet<string> firedKeys = new HashSet<string>();
    private readonly List<TutorialStep> tileReachedSteps = new List<TutorialStep>();

    private TutorialStep pendingStep; // IsOpen 상태라 바로 못 열고 대기 중인 단계(최대 1개)
    private TutorialStep activeStep;
    private int activePageIndex;
    private float pageShownAtUnscaledTime;
    private float previousTimeScale = 1f;
    private GameObject slotHighlight;
    private Image overlayImage;
    private Color overlayNormalColor;
    private LineRenderer worldMarkerRing; // "여기를 밝혀라" 같은 튜토리얼 유도용 표식(안개 무관, 항상 위에 그려짐)
    private Material worldMarkerMat;

    private void Awake()
    {
        if (panel != null)
        {
            panel.SetActive(false);
            overlayImage = panel.GetComponent<Image>();
            if (overlayImage != null)
            {
                overlayNormalColor = overlayImage.color;
                // 이 오버레이는 화면 전체를 덮는 순수 시각용 배경이라, 클릭 자체는 InputLocked/IsAwaitingSlotClick으로
                // 이미 완전히 제어되고 있음. raycastTarget=true로 두면 스킬 시스템(E1) 추가 시 WallPlacementManager에
                // 새로 붙은 EventSystem.IsPointerOverGameObject() 가드가 "화면 전체가 UI 위"라고 판단해버려서,
                // 오버레이가 떠 있는 동안(강제 슬롯 클릭 포함) 벽 슬롯 클릭이 전부 씹히는 회귀 버그가 있었다.
                overlayImage.raycastTarget = false;
            }
        }
        IsOpen = false;
        IsAwaitingSlotClick = false;
        IsAwaitingSkillUse = false;
    }

private void Start()
    {
        if (!tutorialEnabled) return;

        if (pathManager == null) pathManager = FindFirstObjectByType<PathManager>();
        if (playerHealth == null) playerHealth = FindFirstObjectByType<PlayerHealth>();
        if (wallPlacementManager == null) wallPlacementManager = FindFirstObjectByType<WallPlacementManager>();
        if (skillManager == null) skillManager = FindFirstObjectByType<SkillManager>();
        if (stageManager == null) stageManager = FindFirstObjectByType<StageManager>();

        if (playerHealth != null) playerHealth.OnDamaged += HandleFirstHit;
        if (wallPlacementManager != null) wallPlacementManager.OnWallPlaced += HandleWallPlaced;
        if (pathManager != null) pathManager.OnPlayerTileIndexChanged += HandleTileIndexChanged;
        if (skillManager != null) skillManager.OnSkillUsed += HandleSkillUsed;
        // 튜토리얼이 열려 있는 도중(timeScale=0) 스테이지가 끝나는 건 정상 흐름에선 일어나지 않지만
        // (일시정지 중엔 피해도 함께 멈추므로), 혹시 모를 예외 상황에 대비해 안전하게 강제로 닫는다 —
        // 안 닫으면 timeScale이 0에 멈춘 채로 재도전해도 게임이 얼어붙는다.
        if (stageManager != null)
        {
            stageManager.OnCleared += ForceCloseIfOpen;
            stageManager.OnFailed += ForceCloseIfOpen;
            stageManager.OnRetried += HandleRetried;
        }

        foreach (FogVisibility fog in FindObjectsByType<FogVisibility>(FindObjectsSortMode.None))
        {
            FogVisibility captured = fog;
            captured.OnRevealed += () => HandleMonsterRevealed(captured);
        }

        if (steps != null)
        {
            foreach (TutorialStep step in steps)
            {
                if (step.trigger == TutorialTrigger.TileReached) tileReachedSteps.Add(step);
            }
        }

        FireFirstMatchingStep(TutorialTrigger.StageStart);
    }

    private void Update()
    {
        if (!IsOpen) return;
        if (IsAwaitingSlotClick) return; // 이 페이지는 지정 슬롯 클릭으로만 넘어감(HandleWallPlaced가 처리)
        if (IsAwaitingSkillUse) return; // 이 페이지는 지정 스킬 사용으로만 넘어감(HandleSkillUsed가 처리)

        // 튜토리얼이 뜨기 직전에 하던 클릭이 그대로 이어져 문구를 읽기도 전에 넘어가는 것을 방지.
        if (Time.unscaledTime - pageShownAtUnscaledTime < minPageDisplayTime) return;

        bool advancePressed = (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            || (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame);
        if (advancePressed) AdvancePage();
    }



    // "첫 피격"은 어느 몬스터가 언제 맞히느냐에 따라 실시간으로 달라지는 사건이라, 강제 클릭 대상 슬롯을
    // 데이터에 고정값으로 박아두면 이동 속도·타이밍이 조금만 바뀌어도 "이미 지나친 자리"가 되어 튜토리얼이
    // 막혀버리는 문제가 반복됐다(2026-09-10, 2026-09-16 두 번 재발). TryFireStep에서 이 트리거일 때만
    // 발동 시점의 실제 위치 기준으로 슬롯을 다시 계산해 덮어쓰므로 여기서는 트리거만 넘긴다.
    private void HandleFirstHit(float amount)
    {
        FireFirstMatchingStep(TutorialTrigger.FirstHit);
    }

    private void HandleWallPlaced(WallSlot slot)
    {
        if (IsAwaitingSlotClick && slot.TileIndex == RequiredSlotTileIndex && slot.Side == RequiredSlotSide)
        {
            IsAwaitingSlotClick = false;
            AdvancePage();
            // 튜토리얼이 강제로 유도한 배치는 "첫 벽 배치" 트리거로 세지 않는다 — 그래야 재개 직후
            // "투사체가 벽에 막히는 장면"을 먼저 보고, 다음번 자유 배치 때 벽 개수 안내가 뜬다.
            return;
        }

        FireFirstMatchingStep(TutorialTrigger.FirstWallPlaced);
    }

    private void ForceCloseIfOpen()
    {
        if (IsOpen) ClosePause();
    }

    // 재도전 시 호출 — 그 스테이지에서 진행한 모든 것이 처음부터 다시 재생되어야 한다는 정책
    // (2026-09-17, 모든 스테이지 공통)에 따라 이미 본 튜토리얼 기록을 전부 지운다. FogVisibility.everRevealed도
    // 함께 초기화되므로(StageManager가 모든 FogVisibility.ResetVisibility()를 호출), MonsterSeen 트리거도 다시 발동한다.
    private void HandleRetried()
    {
        ForceCloseIfOpen();
        firedKeys.Clear();
        pendingStep = null;
    }

    // 스킬을 실제로 사용한 순간 호출된다 — 지금 페이지가 그 스킬 사용을 기다리는 중일 때만 다음으로 넘긴다.
    private void HandleSkillUsed(SkillType type)
    {
        if (IsAwaitingSkillUse && RequiredSkillType == type)
        {
            IsAwaitingSkillUse = false;
            AdvancePage();
        }
    }

    // MonsterSeen과 마찬가지로, 다른 튜토리얼이 이미 열려있는 도중이면 바로 열지 않고 대기시켰다가
    // 그 팝업이 닫히는 즉시 이어서 연다(이 가드가 없으면 이미 열려있는데 또 OpenPause()가 불려서
    // previousTimeScale이 0으로 덮어써지고, 나중에 닫힐 때 timeScale이 0에 영구히 멈추는 문제가 있었다).
    private void HandleTileIndexChanged(int tileIndex)
    {
        foreach (TutorialStep step in tileReachedSteps)
        {
            if (step.tileIndex != tileIndex) continue;
            if (IsOpen) pendingStep = step;
            else TryFireStep(step);
            return;
        }
    }

    // 게임이 멈춰 있는 동안에는 이동/공격/입력이 전부 Time.deltaTime=0이라 진행이 안 되므로,
    // 팝업이 떠 있는 동안 다른 트리거가 새로 발생할 일은 구조적으로 없다(그래서 큐 없이 단순 무시해도 안전).
    private void FireFirstMatchingStep(TutorialTrigger trigger)
    {
        if (IsOpen || steps == null) return;
        foreach (TutorialStep step in steps)
        {
            if (step.trigger == trigger) { TryFireStep(step); return; }
        }
    }

private void TryFireStep(TutorialStep step)
    {
        if (string.IsNullOrEmpty(step.key) || firedKeys.Contains(step.key)) return;
        if (step.pages == null || step.pages.Length == 0) return;

        firedKeys.Add(step.key);
        activeStep = step;
        activePageIndex = 0;

        // "첫 피격"처럼 발동 시점을 예측할 수 없는 트리거는, 페이지에 미리 적어둔 고정 슬롯 대신
        // 지금 이 순간 실제로 배치 가능한 가장 가까운 슬롯으로 다시 계산해 덮어쓴다.
        if (step.trigger == TutorialTrigger.FirstHit) PatchDynamicGate(step);

        OpenPause();
        ShowCurrentPage();

        if (step.enableWallLimitOnShow) RevealWallLimit();

        if (skillManager != null && step.unlockSkillsOnShow != null)
        {
            foreach (SkillType type in step.unlockSkillsOnShow) skillManager.UnlockSkill(type);
        }

        // ⚠️ 예전엔 VisionSourceRegistry로 대상을 실제로 "공개"시켰는데, 그러면 가시함정 같은 오브젝트
        // 본체가 시야 밖인데도 미리 드러나버려서 "낙석 함정·몬스터처럼 시야 밖에선 안 보여야 한다"는 규칙이
        // 깨졌다(2026-09-17, 사용자 지적). 대상을 직접 공개하는 대신, 안개와 무관하게 항상 보이는
        // 별도의 표식(마커)만 그 위치에 띄운다 — 실제 오브젝트는 원래 규칙대로 시야에 들어와야만 보인다.
        if (step.worldHighlightTarget != null)
        {
            ShowWorldMarker(step.worldHighlightTarget.position, step.worldHighlightRadius);
        }
    }

    // 안개·거리와 무관하게 항상 위에 그려지는 표식 원(AttackRangeIndicator의 링과 동일한 오버레이 셰이더
    // 재사용) — "여기를 조명탄으로 밝혀라" 같은 튜토리얼 유도용. 실제 게임 오브젝트가 아니라 순수 UI성
    // 안내라 시야 규칙의 적용을 받지 않는 게 맞다(벽 슬롯 고스트 미리보기와 같은 취급).
    private void ShowWorldMarker(Vector3 position, float radius)
    {
        HideWorldMarker();
        var go = new GameObject("TutorialWorldMarker");
        worldMarkerRing = go.AddComponent<LineRenderer>();
        worldMarkerRing.loop = true;
        worldMarkerRing.useWorldSpace = true;
        worldMarkerRing.positionCount = 48;
        worldMarkerRing.widthMultiplier = 0.18f;
        worldMarkerRing.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        worldMarkerRing.receiveShadows = false;

        Shader shader = Shader.Find("Custom/RangeIndicatorOverlay");
        worldMarkerMat = new Material(shader);
        worldMarkerMat.SetColor("_BaseColor", new Color(3f, 2.2f, 0.2f, 1f)); // HDR 골드 — 블룸으로 눈에 띄게
        worldMarkerRing.sharedMaterial = worldMarkerMat;

        for (int i = 0; i < 48; i++)
        {
            float angle = i * Mathf.PI * 2f / 48;
            Vector3 point = position + new Vector3(Mathf.Cos(angle) * radius, 0.1f, Mathf.Sin(angle) * radius);
            worldMarkerRing.SetPosition(i, point);
        }
    }

    private void HideWorldMarker()
    {
        if (worldMarkerRing != null)
        {
            Destroy(worldMarkerRing.gameObject);
            worldMarkerRing = null;
        }
        if (worldMarkerMat != null)
        {
            Destroy(worldMarkerMat);
            worldMarkerMat = null;
        }
    }

    // step 안에서 requireSlotClick인 페이지들의 gateTileIndex/gateSide를, 지금 플레이어 위치 기준으로
    // 아직 지나치지 않고 비어있는(IsSlotOccupied 아님) 가장 가까운 슬롯으로 덮어쓴다.
    // TutorialStep/TutorialPage는 일반 C# 클래스라 이 변경은 이 씬 인스턴스의 런타임 값에만 적용되고
    // 에디터에 저장된 원본 데이터는 그대로 남는다.
    private void PatchDynamicGate(TutorialStep step)
    {
        if (pathManager == null || step.pages == null) return;

        foreach (TutorialPage page in step.pages)
        {
            if (!page.requireSlotClick) continue;
            if (!TryFindNextPlaceableSlot(out int tileIndex, out SlotSide side))
            {
                Debug.Log("[TutorialManager] 강제 슬롯으로 쓸 수 있는 빈 자리를 못 찾음(모든 슬롯이 이미 채워졌거나 지나침)");
                continue;
            }
            page.gateTileIndex = tileIndex;
            page.gateSide = side;
        }
    }

    // 현재 타일 바로 다음부터 순서대로 검사해, 배치 가능하고 비어있는 가장 가까운 슬롯 하나를 찾는다.
    // ⚠️ 일부러 pathManager.CanPlaceAt()을 쓰지 않는다 — CanPlaceAt은 wallPlacementGraceTiles(2026-09-21 추가)만큼
    // 이미 도달한 현재 타일까지도 배치 가능으로 쳐주는데, 그걸 그대로 쓰면 이 강제 슬롯이 플레이어 바로 발밑(화면상
    // 이미 지나온 것처럼 보이는 자리)을 가리키게 된다. 이 튜토리얼 슬롯은 "플레이어보다 확실히 앞선 자리"여야
    // 자연스럽게 보이므로, grace를 적용하지 않는 예전 기준(현재 타일보다 순수하게 큰 인덱스)을 그대로 쓴다.
    private bool TryFindNextPlaceableSlot(out int tileIndex, out SlotSide side)
    {
        int currentTile = pathManager.GetCurrentPlayerTileIndex();
        for (int t = 0; t < pathManager.TileCount; t++)
        {
            if (t <= currentTile) continue;
            if (!pathManager.IsSlotOccupied(t, SlotSide.Left)) { tileIndex = t; side = SlotSide.Left; return true; }
            if (!pathManager.IsSlotOccupied(t, SlotSide.Right)) { tileIndex = t; side = SlotSide.Right; return true; }
        }
        tileIndex = -1;
        side = SlotSide.Left;
        return false;
    }

    // 이 단계가 처음 열릴 때만 호출된다(firedKeys 가드로 인해 재도전 재발동 안 됨).
    // 벽 개수 제한을 켜고, 그동안 숨겨두었던 남은 벽 UI를 공개+강조한다.
    private void RevealWallLimit()
    {
        if (wallPlacementManager != null) wallPlacementManager.WallLimitEnabled = true;
        if (wallLimitUIRoot == null) return;

        wallLimitUIRoot.SetActive(true);
        wallLimitUIRoot.transform.SetAsLastSibling(); // 튜토리얼 오버레이보다 위에서 그려지도록

        if (uiHighlight != null)
        {
            // CurrencyIcons 루트는 340x40으로 고정된 빈 껍데기 사각형이라(배경 이미지 없음), 실제로 보이는
            // 아이콘 개수와 무관하게 크기가 늘 같았다 — 강조 대상은 CurrencyUI가 실제로 채운 콘텐츠
            // (슬롯 모드면 SlotContainer, 폴백 모드면 FallbackGroup)의 RectTransform이어야 벽 개수만큼만 감싼다.
            CurrencyUI currencyUI = wallLimitUIRoot.GetComponent<CurrencyUI>();
            RectTransform rt = currencyUI != null ? currencyUI.ActiveContentRect : wallLimitUIRoot.GetComponent<RectTransform>();
            if (rt != null)
            {
                // SlotContainer의 ContentSizeFitter는 다음 레이아웃 패스에서야 크기를 갱신하므로,
                // 방금 활성화된 이번 프레임에 바로 읽으면 스테일된 크기를 잡는다 — 강제로 즉시 재계산.
                LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
                uiHighlight.Highlight(rt);
            }
        }
    }


    private void AdvancePage()
    {
        activePageIndex++;
        if (activeStep == null || activePageIndex >= activeStep.pages.Length)
        {
            ClosePause();
            return;
        }
        ShowCurrentPage();
    }

    private void ShowCurrentPage()
    {
        TutorialPage page = activeStep.pages[activePageIndex];
        if (bodyText != null) bodyText.text = page.text;
        pageShownAtUnscaledTime = Time.unscaledTime;

        HideSlotHighlight();
        IsAwaitingSlotClick = false;
        IsAwaitingSkillUse = false;

        if (page.requireSlotClick)
        {
            IsAwaitingSlotClick = true;
            RequiredSlotTileIndex = page.gateTileIndex;
            RequiredSlotSide = page.gateSide;
            if (wallPlacementManager != null)
            {
                wallPlacementManager.InputLocked = false;
                wallPlacementManager.RestrictToSlot(page.gateTileIndex, page.gateSide);
            }
            if (hintText != null) hintText.text = hintSlotClickMessage;

            // 화면 전체를 덮는 어두운 오버레이 때문에 3D 월드(슬롯 위치)가 안 보여서 클릭할 수 없었던
            // 문제 수정 — 슬롯 클릭을 기다리는 동안은 오버레이를 거의 걷어내 월드가 보이게 한다.
            SetOverlayDimmed(true);

            WallSlot targetSlot = FindSlot(page.gateTileIndex, page.gateSide);
            if (targetSlot != null) ShowSlotHighlight(targetSlot.transform);
        }
        else if (page.requireSkillUse)
        {
            IsAwaitingSkillUse = true;
            RequiredSkillType = page.requiredSkillType;
            if (wallPlacementManager != null) wallPlacementManager.InputLocked = true;
            if (hintText != null) hintText.text = hintSkillUseMessage;

            // 스킬 버튼 클릭(UI)과 조명탄의 월드 조준을 둘 다 봐야 하므로 슬롯 클릭 때와 동일하게 걷어낸다.
            SetOverlayDimmed(true);
        }
        else
        {
            if (wallPlacementManager != null)
            {
                wallPlacementManager.InputLocked = true;
                wallPlacementManager.ClearSlotFilter();
            }
            if (hintText != null) hintText.text = hintAdvanceMessage;
            SetOverlayDimmed(false);
        }

        // 스킬 게이트(requireSkillUse) 진입/이탈은 CanUse()의 결과를 바꾸지만 SkillManager 자신의 상태는
        // 안 바뀌므로, 페이지가 바뀔 때마다(해금 시점이 아니라) 매번 갱신해줘야 버튼이 그 즉시 눌리게 된다
        // (해금은 단계가 "열릴 때" 한 번뿐인데, 실제로 스킬 사용을 요구하는 페이지는 보통 그보다 뒤에 나온다).
        if (skillManager != null) skillManager.RefreshUI();
    }

    private void SetOverlayDimmed(bool dimmed)
    {
        if (overlayImage == null) return;
        Color c = overlayNormalColor;
        overlayImage.color = dimmed ? new Color(c.r, c.g, c.b, dimmedOverlayAlpha) : overlayNormalColor;
    }

    private WallSlot FindSlot(int tileIndex, SlotSide side)
    {
        foreach (WallSlot slot in FindObjectsByType<WallSlot>(FindObjectsSortMode.None))
        {
            if (slot.TileIndex == tileIndex && slot.Side == side) return slot;
        }
        return null;
    }

    // 슬롯 클릭을 강제로 요구하는 페이지에서, 어느 슬롯인지 보이도록 실제 벽 모양의 반투명
    // "고스트" 프리뷰를 그 슬롯 위치·회전에 정확히 맞춰 띄운다(Wall.prefab과 동일 크기,
    // 오프셋 없이 슬롯 자리 그대로 — 클릭 위치와 표시 위치가 어긋나지 않도록).
    private void ShowSlotHighlight(Transform slotTransform)
    {
        slotHighlight = GameObject.CreatePrimitive(PrimitiveType.Cube);
        slotHighlight.name = "TutorialSlotHighlight";
        Collider col = slotHighlight.GetComponent<Collider>();
        if (col != null) Destroy(col);

        slotHighlight.transform.position = slotTransform.position;
        slotHighlight.transform.rotation = slotTransform.rotation;
        slotHighlight.transform.localScale = new Vector3(0.2f, 1.5f, 2f); // Wall.prefab과 동일 크기

        Renderer renderer = slotHighlight.GetComponent<Renderer>();
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        Material mat = new Material(shader != null ? shader : Shader.Find("Standard"));
        Color ghostColor = new Color(1f, 0.9f, 0.3f, 0.3f);
        mat.SetFloat("_Surface", 1f); // Transparent
        mat.SetFloat("_Blend", 0f); // Alpha
        mat.SetOverrideTag("RenderType", "Transparent");
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", ghostColor);
        else mat.color = ghostColor;
        renderer.material = mat;
    }

    private void HideSlotHighlight()
    {
        if (slotHighlight != null)
        {
            Destroy(slotHighlight);
            slotHighlight = null;
        }
    }

    private void OpenPause()
    {
        if (panel != null) panel.SetActive(true);
        IsOpen = true;
        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
    }

private void ClosePause()
    {
        if (panel != null) panel.SetActive(false);
        IsOpen = false;
        IsAwaitingSlotClick = false;
        IsAwaitingSkillUse = false;
        activeStep = null;
        HideSlotHighlight();
        if (uiHighlight != null) uiHighlight.Clear();
        if (wallPlacementManager != null)
        {
            wallPlacementManager.InputLocked = false;
            wallPlacementManager.ClearSlotFilter();
        }
        HideWorldMarker();
        Time.timeScale = previousTimeScale;

        // 팝업이 열려있던 동안 해금된 스킬은 CanUse()가 계속 false(튜토리얼 열림)로 막혀서 버튼이
        // 회색인 채로 그려져 있었다 — 닫히는 시점에 한 번 더 갱신해서 실제로 눌러지게 한다.
        if (skillManager != null) skillManager.RefreshUI();

        if (pendingStep != null)
        {
            TutorialStep next = pendingStep;
            pendingStep = null;
            TryFireStep(next);
        }
    }


    // 몬스터 하나가 새로 공개될 때마다 호출된다. 기존 "첫 몬스터" 안내(순서 무관)와
    // 이 몬스터를 지정해둔 특정 단계(예: Monster_C 조우)를 둘 다 검사한다.
    private void HandleMonsterRevealed(FogVisibility fog)
    {
        FireFirstMatchingStep(TutorialTrigger.FirstMonsterSeen);
        FireMonsterSpecificStep(fog);
    }

    // trigger == MonsterSeen이면서 targetMonster가 방금 공개된 그 몬스터와 일치하는 단계만 찾아 발동한다.
    // 같은 이벤트 처리 중(HandleMonsterRevealed) 앞서 호출한 FireFirstMatchingStep이 막 팝업을 열어
    // IsOpen이 true가 된 경우에도 유실되지 않도록, 그때는 대기시켰다가 그 팝업이 닫히는 즉시 이어서 연다.
    private void FireMonsterSpecificStep(FogVisibility fog)
    {
        if (steps == null) return;
        foreach (TutorialStep step in steps)
        {
            if (step.trigger == TutorialTrigger.MonsterSeen && step.targetMonster == fog)
            {
                if (IsOpen) pendingStep = step;
                else TryFireStep(step);
                return;
            }
        }
    }
}
