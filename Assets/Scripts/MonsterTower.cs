using UnityEngine;

/// 고정형 몬스터 타워. 이동하지 않고 제자리에서 원형 사거리 내의 Player를 탐지한다.
/// 사거리 안에 대상이 있는 동안은 매 프레임 그쪽을 바라보도록 회전하고, 쿨다운마다 거리 검사 후
/// 대상이 있으면 항상 투사체를 발사한다. 벽 차단은 투사체 쪽(Projectile)에서 물리 충돌로 처리한다
/// — 발사 자체는 막지 않고, 날아가던 투사체가 벽에 맞아 막히는 형태로 표현한다.
public class MonsterTower : MonoBehaviour
{
    [SerializeField] private float attackRange = 6f;
    [SerializeField] private float attackCooldown = 1.5f;
    [SerializeField] private float attackDamage = 10f;
    [SerializeField] private LayerMask targetLayerMask;
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float projectileSpawnHeight = 0.3f;
    [SerializeField] private float targetAimHeight = 0.9f;
    [SerializeField] private float rotationSpeed = 360f;

    private float cooldownTimer;

    public float AttackRange => attackRange;

    private void Update()
    {
        Transform target = FindClosestTarget();
        if (target != null)
        {
            FaceTarget(target);
        }

        cooldownTimer -= Time.deltaTime;
        if (cooldownTimer > 0f) return;

        cooldownTimer = attackCooldown;
        if (target == null) return;

        Vector3 origin = transform.position + Vector3.up * projectileSpawnHeight;
        Vector3 aimPoint = target.position + Vector3.up * targetAimHeight;

        Debug.Log($"[MonsterTower] {name}: 투사체 발사");
        FireProjectile(origin, aimPoint);
    }

    private Transform FindClosestTarget()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, attackRange, targetLayerMask);
        if (hits.Length == 0) return null;

        return FindClosest(hits);
    }

    private void FaceTarget(Transform target)
    {
        Vector3 direction = target.position - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f) return;

        Quaternion desiredRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, desiredRotation, rotationSpeed * Time.deltaTime);
    }

    private Transform FindClosest(Collider[] hits)
    {
        Transform closest = null;
        float closestSqrDist = float.MaxValue;
        for (int i = 0; i < hits.Length; i++)
        {
            if (IsDead(hits[i].transform)) continue;

            float sqrDist = (hits[i].transform.position - transform.position).sqrMagnitude;
            if (sqrDist < closestSqrDist)
            {
                closestSqrDist = sqrDist;
                closest = hits[i].transform;
            }
        }
        return closest;
    }

    private bool IsDead(Transform target)
    {
        IDamageable damageable = target.GetComponent<IDamageable>();
        return damageable != null && damageable.IsDead;
    }

    private void FireProjectile(Vector3 origin, Vector3 aimPoint)
    {
        GameObject projectileObj = Instantiate(projectilePrefab, origin, Quaternion.identity);
        Projectile projectile = projectileObj.GetComponent<Projectile>();
        projectile.Launch(aimPoint, attackDamage);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
