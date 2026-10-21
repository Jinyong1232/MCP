using UnityEngine;
using TMPro;

/// 피해량 숫자를 피격 위치 위쪽에 잠깐 띄웠다가 위로 떠오르며 사라지게 한다.
/// Screen Space - Overlay 캔버스 하위에 배치되어 매 프레임 월드 좌표를 화면 좌표로 변환한다.
public class DamageNumberPopup : MonoBehaviour
{
    [SerializeField] private TMP_Text text;
    [SerializeField] private float floatWorldSpeed = 1f;
    [SerializeField] private float lifetime = 0.8f;

    private Vector3 worldPosition;
    private float elapsed;
    private RectTransform rectTransform;
    private Camera targetCamera;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        targetCamera = Camera.main;
    }

    public void Initialize(Vector3 worldPos, float damageAmount)
    {
        worldPosition = worldPos;
        text.text = damageAmount.ToString("0");
        UpdateScreenPosition();
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        worldPosition += Vector3.up * floatWorldSpeed * Time.deltaTime;
        UpdateScreenPosition();

        float fadeStart = lifetime * 0.5f;
        if (elapsed > fadeStart)
        {
            float t = (elapsed - fadeStart) / (lifetime - fadeStart);
            Color c = text.color;
            c.a = Mathf.Lerp(1f, 0f, t);
            text.color = c;
        }

        if (elapsed >= lifetime)
        {
            Destroy(gameObject);
        }
    }

    private void UpdateScreenPosition()
    {
        if (targetCamera == null) return;
        Vector3 screenPos = targetCamera.WorldToScreenPoint(worldPosition);
        rectTransform.position = screenPos;
    }
}
