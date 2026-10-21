using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// Player의 체력을 관리한다. 피해를 받으면 Model을 잠시 붉게 물들이고, 체력이 0 이하가 되면
/// 이동/자동 공격 관련 동작을 정지시키고 사망 애니메이션을 재생한다.
public class PlayerHealth : MonoBehaviour, IDamageable
{
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private Animator animator;
    [SerializeField] private Renderer modelRenderer;
    [SerializeField] private Color hitColor = Color.red;
    [SerializeField] private float hitFlashDuration = 0.15f;

    private PlayerAutoMover autoMover;
    private NavMeshAgent agent;
    private MaterialPropertyBlock propertyBlock;
    private Coroutine flashCoroutine;
    private float currentHealth;
    private bool isDead;
    private bool isInvincible;

    public float MaxHealth => maxHealth;
    public float CurrentHealth => currentHealth;
    public bool IsDead => isDead;
    public bool IsInvincible => isInvincible;

    public event System.Action<float, float> OnHealthChanged;
    public event System.Action OnDied;
    // 실제로 피해를 입었을 때만 발행(ResetHealth로 체력이 복원될 때는 발행 안 함) — "첫 피격" 트리거용
    public event System.Action<float> OnDamaged;
    // 무적 상태라 피해를 막았을 때 발행 — 스킬 이펙트(보호막 번쩍임) 트리거용(docs/skill-vfx.md 4-2)
    public event System.Action OnBlockedByInvincibility;

    private void Awake()
    {
        currentHealth = maxHealth;
        autoMover = GetComponent<PlayerAutoMover>();
        agent = GetComponent<NavMeshAgent>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (modelRenderer == null) modelRenderer = GetComponentInChildren<Renderer>();
        propertyBlock = new MaterialPropertyBlock();
    }

    // 무적 등으로 피해가 막히면 false를 반환 — 호출부(Projectile 등)가 "실제로 맞은 것"과
    // "그냥 막힌 것"을 구분해서 데미지 숫자 표시 여부를 결정할 수 있게 한다.
    public bool TakeDamage(float amount)
    {
        if (isDead) return false;
        if (isInvincible)
        {
            OnBlockedByInvincibility?.Invoke();
            return false;
        }

        currentHealth = Mathf.Max(0f, currentHealth - amount);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        OnDamaged?.Invoke(amount);
        FlashHit();

        if (currentHealth <= 0f)
        {
            Die();
        }

        return true;
    }

    private void FlashHit()
    {
        if (modelRenderer == null) return;

        if (flashCoroutine != null) StopCoroutine(flashCoroutine);
        flashCoroutine = StartCoroutine(FlashHitRoutine());
    }

    private IEnumerator FlashHitRoutine()
    {
        if (propertyBlock == null) propertyBlock = new MaterialPropertyBlock();

        modelRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor("_BaseColor", hitColor);
        modelRenderer.SetPropertyBlock(propertyBlock);

        yield return new WaitForSeconds(hitFlashDuration);

        propertyBlock.Clear();
        modelRenderer.SetPropertyBlock(propertyBlock);
        flashCoroutine = null;
    }

    private void Die()
    {
        isDead = true;

        if (autoMover != null) autoMover.enabled = false;
        if (agent != null && agent.isOnNavMesh) agent.isStopped = true;
        if (animator != null) animator.SetTrigger("Die");

        OnDied?.Invoke();
    }

    // 무적 스킬 전용 — 켜져 있는 동안 TakeDamage가 전부 무시된다(투사체 등은 평소처럼 소멸하되 피해만 0).
    public void SetInvincible(bool value)
    {
        isInvincible = value;
    }

    // 재도전 시 호출 — 체력을 최대치로 되돌리고 이동/사망 상태를 초기화한다.
    public void ResetHealth()
    {
        currentHealth = maxHealth;
        isDead = false;
        isInvincible = false;

        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
            flashCoroutine = null;
        }
        if (modelRenderer != null && propertyBlock != null)
        {
            propertyBlock.Clear();
            modelRenderer.SetPropertyBlock(propertyBlock);
        }

        if (agent != null && agent.isOnNavMesh) agent.isStopped = false;
        if (autoMover != null) autoMover.enabled = true;
        if (animator != null)
        {
            animator.Rebind();
            animator.Update(0f);
        }

        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }
}
