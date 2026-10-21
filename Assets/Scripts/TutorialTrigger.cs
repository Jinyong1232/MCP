/// 튜토리얼 단계를 발동시키는 조건 (docs/tutorial.md 6장 "트리거 종류" 참조)
public enum TutorialTrigger
{
    StageStart,
    FirstMonsterSeen,
    FirstHit,
    FirstWallPlaced,
    TileReached,
    // 특정 몬스터(TutorialStep.targetMonster)가 처음 공개되는 시점. FirstMonsterSeen과 달리
    // "몇 번째로 발견되든 상관없는 첫 몬스터"가 아니라 지정한 그 몬스터를 콕 집어 발동시킨다.
    MonsterSeen
}
