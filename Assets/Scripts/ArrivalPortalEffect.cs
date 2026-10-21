using UnityEngine;

/// 도착 지점 포탈 효과 — 원판을 천천히 회전시키고 스케일을 은은하게 맥동시켜 "여기가 목적지"임을 알린다.
/// 게임 판정(StageManager의 도착 거리 체크)과는 무관한 순수 시각 효과.
public class ArrivalPortalEffect : MonoBehaviour
{
    [SerializeField] private Transform disc;
    [SerializeField] private float rotateSpeed = 20f;
    [SerializeField] private float pulseSpeed = 1.5f;
    [SerializeField] private float pulseScaleAmount = 0.08f;

    private Vector3 baseScale;

    private void Awake()
    {
        if (disc != null) baseScale = disc.localScale;
    }

    private void Update()
    {
        if (disc == null) return;

        disc.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.Self);

        float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseScaleAmount;
        disc.localScale = baseScale * pulse;
    }
}
