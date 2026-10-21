using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// 스킬 버튼 하나의 표시를 담당한다(docs/feature-expansion.md 2-2). 아이콘 + 남은 횟수 + 단축키 숫자,
/// 효과 진행 중에는 지속시간 게이지를 보여주고, 횟수를 다 쓰면 어둡게 바뀌며 클릭이 막힌다.
/// 총 사용 횟수가 0인 스킬은 버튼 자체를 비활성화해 숨긴다(스테이지가 늘면서 스킬이 하나씩 해금되는 구성용).
public class SkillButtonUI : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text countText;
    [SerializeField] private TMP_Text hotkeyText;
    [SerializeField] private Image gaugeImage; // Image.Type.Filled(Radial360) — 지속시간 동안 줄어듦
    [SerializeField] private Button button;
    // 아이콘 스프라이트가 흰색 실루엣이라(Layer Lab 팩 공통) 버튼 배경(흰 원)과 겹치면 안 보임 —
    // 어두운 색으로 틴트해서 대비를 준다.
    [SerializeField] private Color normalColor = new Color(0.28f, 0.22f, 0.16f, 1f);
    [SerializeField] private Color depletedColor = new Color(0.28f, 0.22f, 0.16f, 0.35f);

    private SkillManager manager;
    private int skillIndex;

    // SkillBarUI가 생성 직후 한 번 호출해 이 버튼이 어떤 스킬(인덱스)을 다루는지 연결한다.
    public void Bind(SkillManager targetManager, int index, int hotkeyNumber)
    {
        manager = targetManager;
        skillIndex = index;
        if (hotkeyText != null) hotkeyText.text = hotkeyNumber.ToString();
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => manager.TryUseSkill(skillIndex));
        }
    }

    public void Refresh(SkillState state, bool canUse)
    {
        bool visible = state.Total > 0;
        gameObject.SetActive(visible);
        if (!visible) return;

        if (iconImage != null)
        {
            iconImage.sprite = state.Icon;
            bool depleted = state.Remaining <= 0 && !state.IsActive;
            iconImage.color = depleted ? depletedColor : normalColor;
        }
        if (countText != null) countText.text = state.Remaining.ToString();

        if (gaugeImage != null)
        {
            gaugeImage.gameObject.SetActive(state.IsActive);
            gaugeImage.fillAmount = state.ActiveRatio;
        }

        if (button != null) button.interactable = canUse;
    }
}
