using UnityEngine;

/// 화면 하단 중앙 스킬 버튼 4개를 SkillManager 상태에 맞춰 갱신한다(docs/feature-expansion.md 2-2).
/// 버튼 슬롯은 항상 4개를 미리 배치해두고, 사용 횟수가 0인 스킬의 슬롯만 비활성화해 숨긴다 —
/// HorizontalLayoutGroup이 활성화된 슬롯만 모아 자동으로 가운데 정렬한다.
public class SkillBarUI : MonoBehaviour
{
    [SerializeField] private SkillManager skillManager;
    [SerializeField] private SkillButtonUI[] slots; // Hierarchy 순서 = 스킬 배열 순서 = 단축키 1~4

    private bool bound;

    private void OnEnable()
    {
        if (skillManager != null) skillManager.OnSkillStateChanged += Refresh;
        BindSlotsOnce();
        Refresh();
    }

    private void OnDisable()
    {
        if (skillManager != null) skillManager.OnSkillStateChanged -= Refresh;
    }

    private void BindSlotsOnce()
    {
        if (bound || skillManager == null) return;
        int count = Mathf.Min(slots.Length, skillManager.SkillCount);
        for (int i = 0; i < count; i++)
        {
            slots[i].Bind(skillManager, i, i + 1);
        }
        bound = true;
    }

    private void Refresh()
    {
        if (skillManager == null) return;
        int count = Mathf.Min(slots.Length, skillManager.SkillCount);
        for (int i = 0; i < count; i++)
        {
            slots[i].Refresh(skillManager.GetState(i), skillManager.CanUse(i));
        }
    }
}
