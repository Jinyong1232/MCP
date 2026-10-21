using UnityEngine;

/// 튜토리얼 일시정지 한 번(docs/tutorial.md의 ①~⑥ 각각)에 대응하는 데이터.
/// key는 세션 중 한 번만 발동시키기 위한 식별자.
[System.Serializable]
public class TutorialStep
{
    public string key;
    public TutorialTrigger trigger;
    public int tileIndex = -1; // trigger == TileReached일 때만 사용
    public FogVisibility targetMonster; // trigger == MonsterSeen일 때만 사용
    // 이 단계가 열리는 순간 벽 개수 제한을 활성화하고, 숨겨져 있던 남은 벽 UI를 공개+강조한다.
    public bool enableWallLimitOnShow;
    // 이 단계가 열리는 순간 해금(횟수 부여)할 스킬들 — 스테이지2 시작 시 전부 잠겨있다가 순차 해금되는 용도.
    public SkillType[] unlockSkillsOnShow;
    // 이 단계가 열리는 동안 시야 밖이어도 미리 보여줄 대상(예: 마지막 가시함정) — null이면 사용 안 함.
    public Transform worldHighlightTarget;
    public float worldHighlightRadius = 5f;
    public TutorialPage[] pages;
}
