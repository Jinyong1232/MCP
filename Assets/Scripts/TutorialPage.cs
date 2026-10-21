using UnityEngine;

/// 튜토리얼 한 단계 안에서 순서대로 보여주는 문구 한 장.
/// requireSlotClick이 true면 아무 곳이나 클릭해서는 못 넘어가고, 지정된 벽 슬롯에
/// 실제로 벽을 세워야만 다음으로 진행된다(docs/tutorial.md 3장 "일시정지 ③" 참조).
/// requireSkillUse가 true면 같은 방식으로, 지정한 스킬을 실제로 사용해야만 다음으로 진행된다
/// (스테이지2 스킬 튜토리얼용 — 가시함정 앞 조명탄/무적 유도).
[System.Serializable]
public class TutorialPage
{
    [TextArea] public string text;
    public bool requireSlotClick;
    public int gateTileIndex = -1;
    public SlotSide gateSide;
    public bool requireSkillUse;
    public SkillType requiredSkillType;
}
