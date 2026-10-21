using UnityEngine;

/// 몬스터가 발사하는 투사체. 발사 시점에 조준 위치 기준으로 방향을 한 번 계산해 직선으로 이동하고,
/// 대상과 충돌하면 피해를 전달한 뒤 반환(파괴)된다. 벽(WallVision 레이어)에 맞으면 피해 없이 그 자리에서 막힌다.
/// 유지시간이 지나면 아무 것과도 충돌하지 않아도 반환된다.
/// 이동 판정은 매 FixedUpdate마다 이번 프레임 이동 구간을 SphereCast로 미리 검사하는 방식이다 —
/// 고속 이동 시 Trigger의 Continuous 충돌 감지가 프레임 사이의 얇은 벽을 그대로 통과(터널링)하는
/// 문제가 있어, OnTriggerEnter 대신 이동 전 구간 검사로 대체했다.
public class Projectile : MonoBehaviour
{
    [SerializeField] private float speed = 12f;
    [SerializeField] private float lifetime = 3f;
    [SerializeField] private LayerMask targetLayerMask;
    [SerializeField] private LayerMask wallLayerMask;
    [SerializeField] private GameObject damageNumberPrefab;
    [SerializeField] private Transform damageNumberParent;

    private Rigidbody rb;
    private float hitRadius;
    private Vector3 direction;
    private float damage;
    private float elapsed;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        SphereCollider sphereCollider = GetComponent<SphereCollider>();
        hitRadius = sphereCollider != null ? sphereCollider.radius : 0.05f;
    }

    public void Launch(Vector3 aimPoint, float damageAmount)
    {
        direction = (aimPoint - transform.position).normalized;
        damage = damageAmount;
        transform.rotation = Quaternion.LookRotation(direction);
    }

    private void FixedUpdate()
    {
        float stepDistance = speed * Time.fixedDeltaTime;
        int combinedMask = targetLayerMask | wallLayerMask;

        // 자기 자신의 콜라이더와 겹치는 지점(현재 위치)에서 바로 캐스트하면 스스로를 맞은 것으로 오검출할
        // 수 있어, 자기 콜라이더 반경만큼 진행 방향으로 띄운 지점부터 검사한다.
        Vector3 castOrigin = rb.position + direction * hitRadius;

        if (Physics.SphereCast(castOrigin, hitRadius, direction, out RaycastHit hit, stepDistance, combinedMask))
        {
            HandleHit(hit.collider);
        }
        else
        {
            rb.MovePosition(rb.position + direction * stepDistance);
        }

        elapsed += Time.fixedDeltaTime;
        if (elapsed >= lifetime)
        {
            Destroy(gameObject);
        }
    }

    private void HandleHit(Collider other)
    {
        int otherLayerBit = 1 << other.gameObject.layer;

        if ((otherLayerBit & wallLayerMask.value) != 0)
        {
            Debug.Log($"[Projectile] {other.name}(벽)에 막힘");
            Destroy(gameObject);
            return;
        }

        if ((otherLayerBit & targetLayerMask.value) == 0) return;

        IDamageable damageable = other.GetComponent<IDamageable>();
        bool applied = damageable != null && damageable.TakeDamage(damage);

        if (applied)
        {
            Debug.Log($"[Projectile] {other.name}에게 명중 (피해 {damage})");
            SpawnDamageNumber(other.ClosestPoint(transform.position));
        }
        else
        {
            // 무적 등으로 막힌 경우 — 벽에 막힌 것과 동일하게 피해 처리·숫자 표시 없이 그냥 소멸한다.
            Debug.Log($"[Projectile] {other.name}에게 막힘(무적 등)");
        }

        Destroy(gameObject);
    }

    private void SpawnDamageNumber(Vector3 worldPosition)
    {
        if (damageNumberPrefab == null) return;

        // 프리팹 에셋은 씬 오브젝트(Canvas)를 직접 참조할 수 없으므로 런타임에 찾아 캐시한다
        if (damageNumberParent == null)
        {
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas != null) damageNumberParent = canvas.transform;
        }

        GameObject popupObj = Instantiate(damageNumberPrefab, damageNumberParent);
        DamageNumberPopup popup = popupObj.GetComponent<DamageNumberPopup>();
        popup.Initialize(worldPosition + Vector3.up * 0.5f, damage);
    }
}
