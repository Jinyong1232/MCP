using UnityEngine;
using UnityEngine.UI;

/// 출발지 대비 현재 위치를 얇은 진행도 바로 표시한다. 현재 타일 인덱스 ÷ 전체 타일 수로 계산하며,
/// 마커 아이콘을 바 위에서 좌우로 움직여 위치를 보여준다(끝의 깃발 아이콘은 고정 장식).
public class ProgressBarUI : MonoBehaviour
{
    [SerializeField] private PathManager pathManager;
    [SerializeField] private Slider progressSlider;
    [SerializeField] private RectTransform markerIcon;
    [SerializeField] private RectTransform barRect;
    [SerializeField] private RectTransform hitMarker; // 실패(U5) 시 가장 피해를 많이 입은 구간을 표시. 평소는 비활성.


    private void Awake()
    {
        if (pathManager == null) pathManager = FindFirstObjectByType<PathManager>();
    }

    private void OnEnable()
    {
        if (pathManager != null) pathManager.OnPlayerTileIndexChanged += HandleTileChanged;
    }

    private void OnDisable()
    {
        if (pathManager != null) pathManager.OnPlayerTileIndexChanged -= HandleTileChanged;
    }

    private void Start()
    {
        HandleTileChanged(pathManager.GetCurrentPlayerTileIndex());
    }

    // 실패 직후 StageManager가 호출 — barRect 위에 pct 위치에 피격 지점 마커를 표시한다.
    public void ShowHitMarker(float pct)
    {
        if (hitMarker == null || barRect == null) return;

        float halfWidth = barRect.rect.width * 0.5f;
        float x = Mathf.Lerp(-halfWidth, halfWidth, Mathf.Clamp01(pct));
        hitMarker.anchoredPosition = new Vector2(x, hitMarker.anchoredPosition.y);
        hitMarker.gameObject.SetActive(true);
    }

    // 재도전 시 StageManager가 호출해 마커를 지운다.
    public void HideHitMarker()
    {
        if (hitMarker != null) hitMarker.gameObject.SetActive(false);
    }

    
private void HandleTileChanged(int tileIndex)
    {
        if (pathManager.TileCount <= 1) return;

        float pct = (float)tileIndex / (pathManager.TileCount - 1);
        if (progressSlider != null) progressSlider.value = pct;

        if (markerIcon != null && barRect != null)
        {
            float halfWidth = barRect.rect.width * 0.5f;
            float x = Mathf.Lerp(-halfWidth, halfWidth, pct);
            markerIcon.anchoredPosition = new Vector2(x, markerIcon.anchoredPosition.y);
        }
    }
}
