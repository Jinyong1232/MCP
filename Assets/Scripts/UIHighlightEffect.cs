using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// 화면 UI 요소에 "여기를 보라"는 강조 연출을 준다(docs/tutorial.md 5.2).
/// 대상 주변에 얇은 사각 테두리(가운데가 뚫린 4개 막대로 직접 구성 — 외부 에셋의 가운데가 채워진
/// 스프라이트를 썼다가 대상을 가려버린 적이 있어 확실히 뚫려 있도록 직접 그린다)가 크게 시작해
/// 대상 크기로 수축하며 나타나고, 대상의 오른쪽 아래 모서리 쪽에 화살표가 놓여 안쪽(대상)을 향해
/// 살짝 펄스한다. 대상 자체도 살짝 확대된다. Time.unscaledDeltaTime으로 구동해
/// Time.timeScale=0(튜토리얼 정지 중)에도 재생된다.
/// 화면 가장자리에 붙어있는 HUD 요소(예: 상단 바로 아래 남은 벽 UI)를 강조할 때 테두리·화살표가
/// 화면 밖으로 튀어나가지 않도록, 루트 Canvas 영역 안쪽으로 항상 위치를 clamp한다.
public class UIHighlightEffect : MonoBehaviour
{
    [SerializeField] private Sprite arrowSprite;
    [SerializeField] private Color ringColor = new Color(1f, 0.85f, 0.3f, 0.95f);
    [SerializeField] private Color arrowColor = new Color(1f, 0.98f, 0.92f, 1f);
    [SerializeField] private Color arrowOutlineColor = new Color(0.15f, 0.1f, 0.05f, 0.9f); // 어떤 배경 위에서도 화살표가 보이도록 하는 테두리
    [SerializeField] private float ringThickness = 3f; // 테두리 막대 굵기
    [SerializeField] private Vector2 padding = new Vector2(24f, 20f);
    [SerializeField] private float startSizeMultiplier = 1.6f;
    [SerializeField] private float shrinkDuration = 0.35f;
    [SerializeField] private float scalePunch = 1.15f;
    [SerializeField] private float arrowSize = 44f;
    [SerializeField] private float arrowGap = 14f; // 대상 모서리와 화살표 사이 여백
    [SerializeField] private float bounceDistance = 8f; // 모서리 대각선 방향으로 펄스하는 거리
    [SerializeField] private float bounceSpeed = 3f;
    [SerializeField] private float screenEdgeMargin = 12f; // Canvas 가장자리에서 이만큼은 항상 띄운다

    private RectTransform activeRing;
    private Image[] activeRingBars;
    private RectTransform activeArrow;
    private RectTransform highlightedTarget;
    private RectTransform canvasRect;
    private Vector3 targetOriginalScale = Vector3.one;
    private Coroutine routine;

    // 대상 RectTransform 주변에 강조 연출을 재생한다. 이미 재생 중이면 이전 것을 정리하고 새로 시작한다.
    public void Highlight(RectTransform target)
    {
        if (target == null) return;
        Clear();

        highlightedTarget = target;
        targetOriginalScale = target.localScale;

        Canvas canvas = target.GetComponentInParent<Canvas>();
        canvasRect = canvas != null ? canvas.rootCanvas.GetComponent<RectTransform>() : null;

        GameObject ringGO = new GameObject("TutorialHighlightRing", typeof(RectTransform));
        ringGO.transform.SetParent(target.parent, false);
        activeRing = ringGO.GetComponent<RectTransform>();

        activeRingBars = new[]
        {
            CreateRingBar(activeRing, "Top", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, ringThickness)),
            CreateRingBar(activeRing, "Bottom", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, ringThickness)),
            CreateRingBar(activeRing, "Left", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(ringThickness, 0f)),
            CreateRingBar(activeRing, "Right", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(ringThickness, 0f)),
        };

        if (arrowSprite != null)
        {
            GameObject arrowGO = new GameObject("TutorialHighlightArrow", typeof(RectTransform));
            arrowGO.transform.SetParent(target.parent, false);
            activeArrow = arrowGO.GetComponent<RectTransform>();
            activeArrow.sizeDelta = new Vector2(arrowSize, arrowSize);
            Image arrowImg = arrowGO.AddComponent<Image>();
            arrowImg.sprite = arrowSprite;
            arrowImg.raycastTarget = false;
            arrowImg.color = arrowColor;
            Shadow outline = arrowGO.AddComponent<Shadow>();
            outline.effectColor = arrowOutlineColor;
            outline.effectDistance = new Vector2(2f, -2f);
            outline.useGraphicAlpha = true;
        }

        routine = StartCoroutine(PlayRoutine(target));
    }

    // 테두리 막대 하나를 부모 사각형의 한 변에 꽉 붙여 늘어나도록(anchor stretch) 만든다.
    // 부모(activeRing)의 sizeDelta가 바뀌면 앵커 기준으로 자동으로 같이 늘어나므로, 애니메이션 중 매 프레임
    // 막대 크기를 따로 갱신할 필요가 없다. 4개 막대만 그리고 가운데는 아예 만들지 않아 항상 확실하게 뚫려 있다.
    private static Image CreateRingBar(RectTransform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 sizeDelta)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.sizeDelta = sizeDelta;
        rt.anchoredPosition = Vector2.zero;
        Image img = go.AddComponent<Image>();
        img.raycastTarget = false;
        return img;
    }

    // 강조 연출을 즉시 종료하고 대상 스케일을 원래대로 되돌린다. 재생 중이 아니면 아무 일도 하지 않는다.
    public void Clear()
    {
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }
        if (activeRing != null) Destroy(activeRing.gameObject);
        if (activeArrow != null) Destroy(activeArrow.gameObject);
        if (highlightedTarget != null) highlightedTarget.localScale = targetOriginalScale;
        activeRing = null;
        activeRingBars = null;
        activeArrow = null;
        highlightedTarget = null;
        canvasRect = null;
    }

    private IEnumerator PlayRoutine(RectTransform target)
    {
        Vector2 targetSize = target.rect.size + padding;
        float t = 0f;
        while (t < shrinkDuration)
        {
            t += Time.unscaledDeltaTime;
            float f = EaseOutCubic(Mathf.Clamp01(t / shrinkDuration));

            if (activeRing != null)
            {
                Vector2 ringSize = Vector2.Lerp(targetSize * startSizeMultiplier, targetSize, f);
                activeRing.sizeDelta = ringSize;
                activeRing.position = ClampToCanvas(GetCenterWorld(target), ringSize);
                Color c = ringColor;
                c.a = Mathf.Lerp(0f, ringColor.a, f);
                SetRingColor(c);
            }
            yield return null;
        }

        target.localScale = targetOriginalScale * scalePunch;

        while (true)
        {
            if (activeRing != null)
            {
                activeRing.position = ClampToCanvas(GetCenterWorld(target), targetSize);
            }
            if (activeArrow != null)
            {
                PlaceArrowAtBottomRightCorner(target, targetSize);
            }
            yield return null;
        }
    }

    private void SetRingColor(Color c)
    {
        if (activeRingBars == null) return;
        for (int i = 0; i < activeRingBars.Length; i++)
        {
            if (activeRingBars[i] != null) activeRingBars[i].color = c;
        }
    }

    // target의 pivot이 아니라 실제 시각적 중심(rect.center)의 월드 좌표를 구한다.
    // CurrencyIcons/SlotContainer처럼 pivot이 (0, 0.5)인 좌측 정렬 UI에도 링/화살표가 정확히 중앙에 오도록 하기 위함 —
    // pivot 기준(target.position)을 그대로 쓰면 pivot이 중앙이 아닌 대상에서는 중심이 어긋난다.
    private static Vector3 GetCenterWorld(RectTransform target)
    {
        return target.TransformPoint(target.rect.center);
    }

    // 대상 사각 테두리의 오른쪽 아래 모서리 바깥쪽에 화살표를 놓고, 대각선 안쪽(대상 중심)을 향해 회전시킨다.
    private void PlaceArrowAtBottomRightCorner(RectTransform target, Vector2 targetSize)
    {
        Vector3 centerWorld = GetCenterWorld(target);
        Vector2 half = targetSize * 0.5f;
        Vector2 diag = new Vector2(1f, -1f).normalized; // 오른쪽 아래 대각선 방향

        float pulse = (Mathf.Sin(Time.unscaledTime * bounceSpeed) + 1f) * 0.5f * bounceDistance;
        Vector2 offset = new Vector2(half.x, -half.y) + diag * (arrowGap + pulse);
        Vector3 desired = centerWorld + (Vector3)offset;
        activeArrow.position = ClampToCanvas(desired, new Vector2(arrowSize, arrowSize));

        Vector2 pointDir = -diag; // 모서리에서 대상 중심 쪽(왼쪽 위)을 바라봄
        float angleDeg = Mathf.Atan2(pointDir.y, pointDir.x) * Mathf.Rad2Deg;
        activeArrow.rotation = Quaternion.Euler(0f, 0f, angleDeg + 90f); // 화살표 스프라이트 기본 방향(아래)을 pointDir로 정렬
    }

    // ring/arrow가 루트 Canvas 영역(화면) 밖으로 나가지 않도록 중심 위치를 안쪽으로 당긴다.
    private Vector3 ClampToCanvas(Vector3 worldPos, Vector2 size)
    {
        if (canvasRect == null) return worldPos;

        Vector3 local = canvasRect.InverseTransformPoint(worldPos);
        Rect r = canvasRect.rect;
        float halfW = size.x * 0.5f + screenEdgeMargin;
        float halfH = size.y * 0.5f + screenEdgeMargin;

        float minX = r.xMin + halfW;
        float maxX = r.xMax - halfW;
        float minY = r.yMin + halfH;
        float maxY = r.yMax - halfH;

        if (minX <= maxX) local.x = Mathf.Clamp(local.x, minX, maxX);
        if (minY <= maxY) local.y = Mathf.Clamp(local.y, minY, maxY);

        return canvasRect.TransformPoint(local);
    }

    private static float EaseOutCubic(float x)
    {
        return 1f - Mathf.Pow(1f - x, 3f);
    }
}
