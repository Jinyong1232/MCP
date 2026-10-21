/// 피해를 받을 수 있는 대상이 구현하는 인터페이스
public interface IDamageable
{
    // 무적 등으로 피해가 막히면 false를 반환한다 — 호출부(투사체 등)가 이 값으로
    // "실제로 맞았는지"를 판단해 데미지 숫자 표시 여부 등을 결정한다.
    bool TakeDamage(float amount);
    bool IsDead { get; }
}
