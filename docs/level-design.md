# 레벨 디자인 매뉴얼

> "이걸 하려면 뭘 만지면 되지?"를 찾아보는 실무 매뉴얼. 설계 이론은 `design.md`/`polish.md` 참조, 여기는 작업 방법만 다룬다.
> 값은 2026-09-11 기준 실제 씬 값이다. 나중에 값이 바뀌었으면 여기 적힌 숫자보다 씬에서 직접 확인한 값을 믿을 것.

---

## 길을 한 칸 늘리고 싶다

1. Hierarchy에서 `Path > Tiles > Tile_19`(마지막 타일) 선택, Ctrl+D로 복제
2. 이름을 `Tile_20`으로 변경, `Tiles`가 부모인지 확인(자식 순서 = 타일 순서이므로 맨 뒤에 와야 함)
3. Position을 이전 타일에서 진행 방향으로 정확히 **2유닛** 이동한 값으로 설정(타일 간격은 항상 2)
4. 벽 슬롯도 같이 늘려야 함 → "벽 슬롯을 추가하고 싶다" 참조
5. `Path > PathTilesVisual`에도 같은 위치에 길 비주얼(Plane, Y=0.02, `M_PathTile.mat`)을 하나 복제해 추가

⚠️ 마지막 타일 위치가 바뀌면 도착 지점(`StageManager`가 자동으로 마지막 타일로 판정)과 `ArrivalPortal` 위치도 옮겨야 함

## 길을 꺾고 싶다 (코너 만들기)

1. 꺾이는 지점의 타일 Position을 원하는 방향으로 어긋나게 배치(예: Z축 진행 → X축 진행으로 90도 꺾기)
2. `PathManager.GetTileForward()`는 "이전 타일 → 이 타일" 방향을 그 타일의 진행 방향으로 씀(코너 타일도 예외 없음)
3. 벽 슬롯 좌/우 위치는 이 진행 방향 기준 수직 오프셋으로 계산해서 손으로 배치해야 함 → "벽 슬롯 위치를 옮기고 싶다" 참조
4. 코너 타일 자체의 슬롯 하나가 길 안쪽(포켓)에 겹칠 수 있음 → "코너에서 슬롯이 길 위에 겹친다" 참조

⚠️ NavMeshSurface는 `Floor` 하나로 이미 전체를 덮고 있어 재베이크 불필요(길 모양은 시각적 배치일 뿐 NavMesh와 무관)

## 길 전체를 다른 모양으로 새로 깔고 싶다

1. `Path > Tiles` 하위 타일들을 원하는 모양·개수로 재배치(Ctrl+D로 늘리거나 필요 없는 타일 삭제)
2. `Path > WallSlots`도 타일 수만큼 좌/우 슬롯을 다시 배치(각 타일마다 2개)
3. `Path > PathTilesVisual`도 같은 개수·위치로 다시 배치
4. 도착 지점이 바뀌므로 `ArrivalPortal` 위치도 마지막 타일로 이동
5. 몬스터·재화·시야 범위는 새 길 모양에 맞춰 전부 재조정 필요(길이 바뀌면 밸런스도 다시 잡아야 함)

⚠️ 타일 순서는 Hierarchy 자식 순서로 읽히므로, 재배치 후 자식 순서가 실제 진행 순서와 일치하는지 반드시 확인

---

## 벽 슬롯을 추가하고 싶다

1. `Path > WallSlots` 하위의 기존 슬롯 하나(예: `Slot_00_L`) 선택, Ctrl+D로 복제
2. 이름을 `Slot_XX_L`/`Slot_XX_R` 규칙대로 변경(XX는 타일 번호 2자리)
3. Position 계산: 타일 중심에서 진행 방향에 **수직으로 1유닛** 오프셋, **Y=0.75** 고정
   - 진행 방향이 +Z(세로 구간)면 Left=X-1 / Right=X+1, 회전 Y=0
   - 진행 방향이 +X(가로 구간)면 Left=Z+1 / Right=Z-1, 회전 Y=90
4. `WallSlot` 컴포넌트의 `Tile Index`/`Side` 값을 타일 번호·L or R에 맞게 설정
5. 길 옆 받침대(`SlotPedestal`)도 같이 놓고 싶으면 `Assets/Prefabs/SlotPedestal.prefab`을 같은 Position(Y만 0)·같은 회전으로 배치

⚠️ 벽은 `WallVision` 레이어, 슬롯은 `WallSlot` 레이어를 쓴다 — 새 슬롯의 Layer가 `WallSlot`인지 꼭 확인(아니면 클릭이 안 먹힘)

## 벽 슬롯 위치를 옮기고 싶다

1. `Path > WallSlots`에서 대상 슬롯 선택
2. Position만 원하는 자리로 이동(Y=0.75는 유지 — 벽 프리팹의 세로 중심 높이와 맞물려 있음)
3. 진행 방향이 바뀌는 자리가 아니면 회전은 그대로 둬도 됨
4. 근처에 `SlotPedestal`을 배치해뒀다면 같이 옮기고 Y만 0으로 맞출 것

⚠️ 슬롯을 옮기면 `docs/tutorial.md`가 지정하는 강제 클릭 위치(타일3 Left)와 어긋날 수 있음 → "튜토리얼에서 벽을 세우라고 지정하는 위치를 바꾸고 싶다" 참조

## 코너에서 슬롯이 길 위에 겹친다

- **원인**: 슬롯 위치 공식이 "진입 방향 기준 수직 오프셋" 하나뿐인데, 코너 타일은 진입 방향과 진출 방향이 달라서 한쪽 슬롯이 다음 구간이 시작되는 안쪽 포켓을 가리키게 됨
- **해결 규칙**: 문제되는 슬롯만 **진출 방향(다음 구간) 세그먼트가 쓰는 가장자리 오프셋으로 교체**한다. 대각선(45도)이나 이등분선 계산은 쓰지 않는다 — 항상 축 정렬(0°/90°) 유지
- 예시(현재 스테이지의 코너 타일 9): `Slot_09_L`은 원래 공식 그대로(X-1, 회전 0), `Slot_09_R`만 다음 구간(가로 진행) 규칙을 가져와 (Z+1, 회전 90)로 수동 교체함

⚠️ 나머지 39개 슬롯은 전부 공식 그대로 — 코너 타일 딱 하나만 예외적으로 손으로 고치는 것임

---

## 몬스터를 추가하고 싶다

1. `Assets/Prefabs/Chicken.prefab`(기본형) / `Monster_AreaType.prefab`(광역형) / `Monster_DPSType.prefab`(고DPS형) 중 하나를 씬으로 드래그
2. 원하는 위치에 배치(Y=0). 겹침 구간을 만들려면 기존 몬스터와 사거리(`Attack Range`)가 겹치는 위치로 계산해서 배치
3. 바닥 굴곡(`GroundBumps`)에 몸통이 파묻히지 않으려면 반경 2.4유닛 안쪽은 자동으로 평탄화되어 있음 — 그보다 새 위치가 애매하면 평탄화 반경 재생성 필요(스크립트로 절차 생성, 아래 "오브젝트 한눈에 보기" 참고)
4. `FogVisibility`/`AttackRangeIndicator`의 `Vision Radius`를 그 씬의 값(현재 두 스테이지 모두 8)과 동일하게 맞출 것

⚠️ 몬스터는 고정형이다. NavMeshAgent를 붙이거나 추적 로직을 넣지 말 것(design.md 3.5)

## 몬스터 종류를 바꾸고 싶다

1. 기존 몬스터 오브젝트를 선택해 위치·회전을 메모
2. 삭제 후 원하는 종류의 프리팹(`Chicken`/`Monster_AreaType`/`Monster_DPSType`)을 같은 위치에 배치
3. `Vision Radius`(`FogVisibility`/`AttackRangeIndicator`)를 씬 값에 맞출 것(프리팹 기본값은 8이지만 스테이지에 따라 다를 수 있음)

## 몬스터 사거리·공격력을 바꾸고 싶다

1. 대상 몬스터의 `MonsterTower` 컴포넌트 선택
2. `Attack Range`(사거리) / `Attack Cooldown`(공격 간격, 초) / `Attack Damage`(공격력) 값 수정
3. 현재 3종 기준값: 기본형 6/1.5/10, 광역형 10/1.5/6, 고DPS형 4/0.7/20 — 새 값도 이 상대적 트레이드오프(사거리↔공격력)를 지키는 게 좋음
4. `AttackRangeIndicator`가 그리는 원이 `Attack Range`를 자동으로 따라가므로 별도 동기화 불필요

⚠️ 몬스터는 공격 각도 판정을 쓰지 않는다(원형 사거리만). 각도 판정을 되살리지 말 것(reference.md 2번 제외 목록)

## 몬스터가 특정 구간만 위협하게 하고 싶다

- 별도 설정이 없다 — 순전히 **위치와 `Attack Range`의 기하학적 관계**로만 결정됨
- 몬스터를 선택하면 Scene 뷰에 빨간 원(사거리)이 그려짐(`OnDrawGizmosSelected`) — 이 원이 어느 타일과 겹치는지 보면서 위치를 조정
- 특정 타일만 위협하게 하려면: 그 타일까지의 거리가 `Attack Range`보다 작고, 다른 타일까지의 거리는 `Attack Range`보다 크도록 위치를 옮기면 됨

---

## 벽(재화) 개수를 바꾸고 싶다

1. `Managers` 선택 → `Currency Manager` 컴포넌트 → `Total Currency` 값 수정(현재 스테이지1=6, 스테이지2=7)
2. 벽 1개당 비용을 바꾸려면 같은 오브젝트의 `Wall Placement Manager` → `Wall Cost`(현재 1)

## 시야 범위를 바꾸고 싶다

⚠️ **먼저 사용자에게 확인할 것** — `visionRadius`는 밸런스 핵심 수치라 임의로 건드리지 않기로 합의됨(2026-09-11)

1. (허락받은 뒤) `Managers`가 아니라 **몬스터마다** 있는 `FogVisibility`/`AttackRangeIndicator`의 `Vision Radius`를 전부 동일한 값으로 맞춤
2. `FogGround` 오브젝트의 `FogOfWarGround` 컴포넌트 `Vision Radius`도 같은 값으로 맞춤
3. 세 군데(몬스터마다 2개 + FogGround 1개) 전부 같은 값이어야 함 — 하나라도 다르면 시야·안개·사거리 표시가 서로 어긋남
4. `반응 시간 = Vision Radius ÷ Player 이동속도(NavMeshAgent Speed)`가 바뀌므로 design.md 10.2 기준(1.5~3초)과 비교해볼 것

## 플레이어 체력·이동속도를 바꾸고 싶다

1. `Player` 선택 → `Player Health` 컴포넌트 → `Max Health`(현재 100)
2. 이동속도는 `NavMeshAgent` 컴포넌트 → `Speed`(현재 3.6) — 시야 범위와 묶여서 반응 시간에 영향을 주므로 같이 검토할 것

---

## 새 스테이지를 만들고 싶다

1. 기존 씬(`Stage1(Test).unity` 또는 `Stage2.unity`) 하나를 복제(Project 창에서 Ctrl+D) 후 이름 변경
2. `File > Build Settings`에서 새 씬을 등록(`manage_build`로도 가능)
3. 몬스터 배치·재화(`Total Currency`)·시야(`Vision Radius`, 세 군데)를 새 난이도에 맞게 재조정
4. `Managers > Stage Manager`의 `Next Scene Name`을 다음 스테이지 씬 이름으로 설정(마지막 스테이지면 비워둠 → "GAME CLEAR" 문구만 뜨고 버튼 없음)
5. 길 모양을 바꿨다면 "길 전체를 다른 모양으로 새로 깔고 싶다" 참조, 그대로 재사용한다면 `ArrivalPortal` 위치만 마지막 타일로 확인
6. 환경 데코(나무·바위·바닥 굴곡 등)는 선택 사항 — 필요하면 새로 배치(스크립트 기반 절차 생성이라 단순 복제로는 안 됨, 문의할 것)

⚠️ 새 씬에는 `EventSystem`(+ `InputSystemUIInputModule`)이 반드시 있어야 uGUI 버튼이 눌림(복제로 만들면 이미 있음)

## 튜토리얼 문구나 순서를 바꾸고 싶다

1. `Managers` → `Tutorial Manager` 컴포넌트 → `Steps` 배열
2. 각 `Step`은 `Key`(식별자)·`Trigger`(발동 조건: StageStart/FirstMonsterSeen/FirstHit/FirstWallPlaced/TileReached)·`Tile Index`(TileReached일 때만 사용)·`Pages` 배열을 가짐
3. 각 `Page`는 `Text`(문구)·`Require Slot Click`(체크하면 지정 슬롯 클릭 전엔 못 넘어감)
4. 순서를 바꾸려면 배열 순서 자체를 드래그로 재배열(단, 실제 발동 순서는 트리거 조건이 게임 중 언제 충족되는지에 따름 — 배열 순서와 무관)
5. 스테이지2는 현재 `Steps`가 비어 있음(튜토리얼 없음) — 필요하면 여기 추가

## 튜토리얼에서 벽을 세우라고 지정하는 위치를 바꾸고 싶다

1. `Managers` → `Tutorial Manager` → `Steps` 중 `Key`가 `first_hit`인 항목 찾기
2. 그 안의 2번째 `Page`(`Require Slot Click`=true인 페이지)의 `Gate Tile Index`/`Gate Side` 값을 원하는 슬롯으로 변경(현재 타일3 Left)
3. 그 슬롯이 실제로 배치 가능한(아직 안 지나간) 자리인지 확인 — 몬스터 배치를 바꿨다면 첫 피격 시점도 바뀌므로 이 값도 재계산 필요

---

## 오브젝트 한눈에 보기

| 오브젝트 | 뭐 하는 것 | 자주 만지는 값 |
|---|---|---|
| `Managers > Currency Manager` | 재화(벽 개수) 관리 | `Total Currency` |
| `Managers > Wall Placement Manager` | 벽 배치 로직 | `Wall Cost` |
| `Managers > Stage Manager` | 클리어/실패 판정, 결과 화면 | `Next Scene Name`, `Arrival Threshold` |
| `Managers > Tutorial Manager` | 튜토리얼 팝업 시퀀스 | `Tutorial Enabled`, `Steps` |
| `Managers > Play Stats Logger` | 클리어/실패 시 CSV 기록 | `Exclude Reaction Time This Stage` |
| `Path > Tiles` | 경로 타일(순서 = 자식 순서) | 각 타일 Position |
| `Path > WallSlots` | 벽 배치 가능 지점 | Position, `Tile Index`/`Side` |
| `Path > PathTilesVisual` | 길 바닥 비주얼(항상 안개 위로 보임) | Position |
| `SlotPedestals` | 벽 슬롯 옆 받침대(장식) | 슬롯 옮기면 같이 옮길 것 |
| `Environment` | 나무·바위·식생 장식 | 장식용, 게임 로직과 무관 |
| `GroundBumps` | 바닥 굴곡(길 제외) 장식 메시 | 스테이지별 별도 메시(스크립트 생성) |
| `FogGround` | 안개(시야 제한) | `Fog Of War Ground` → `Vision Radius` |
| `ArrivalPortal` | 도착 지점 표시 이펙트 | 마지막 타일 위치로 이동 |
| `Monster_*` | 몬스터(고정 타워) | `Monster Tower`, `Fog Visibility`/`Attack Range Indicator`의 `Vision Radius` |
| `Player` | 플레이어(자동 이동) | `Player Health`(체력), `NavMeshAgent`(속도) |
| `Wall.prefab` | 배치되는 벽(벽돌+뒷판 구조) | 프리팹 자체를 수정하면 전 스테이지 반영 |
| `EventSystem` | uGUI 클릭 처리(New Input System) | 새 씬엔 반드시 있어야 함 |

## 새 스테이지 만든 뒤 확인할 것

- [ ] 경로 끝까지 자동 이동하는지 (Play 모드로 실제 확인)
- [ ] 벽 슬롯 클릭 → 벽 배치가 전부 정상 동작하는지 (코너 슬롯 포함)
- [ ] 클리어(도착)·실패(체력 0) 판정과 결과 화면(다음 스테이지 버튼 또는 GAME CLEAR)이 정상인지
- [ ] 재도전 버튼으로 전부(체력·재화·벽·안개·몬스터 시야) 초기 상태로 돌아가는지
- [ ] 안개가 Player를 따라 정상적으로 걷히는지, 길은 안개와 무관하게 항상 보이는지
- [ ] `EventSystem`이 씬에 있어서 버튼 클릭이 실제로 먹히는지
