# 진행 상황

> **세션 시작 시 이 파일을 먼저 읽고** 어디까지 진행했는지 파악한 뒤 이어서 작업할 것.
> 한 단계를 마칠 때마다 이 파일을 갱신할 것.
> 전체 단계 목록은 `docs/design.md` 7번 섹션 참조.
>
> ⚠️ **2026-09-14 정리**: 이 파일이 900줄까지 누적돼 비효율적이라, 날짜별 상세 버그 수정 경위(디버깅 과정·시행착오)는 전부 `PROGRESS_ARCHIVE.md`로 옮기고 이 파일은 "현재 어디까지 왔고 다음에 뭘 할지" 중심으로 재구성함. 특정 버그의 원인·해결 과정을 다시 찾아봐야 할 때만 아카이브를 참고할 것.

---

## 🎯 다음 세션에 할 일

### ✅ 완료 — 튜토리얼 강제 벽 슬롯이 벽 배치 grace 때문에 너무 뒤쪽에 뜨는 문제 수정 + 재빌드 (2026-09-21, 같은 날 후속2)

사용자 제보: 아까(같은 날 앞선 작업) 추가한 벽 배치 판정 여유(`wallPlacementGraceTiles=1`) 때문에, Stage1(Tutorial) "벽 세우기" 튜토리얼(일시정지 ③)에서 강제로 클릭해야 하는 벽 슬롯이 너무 뒤쪽(이미 지나온 것처럼 보이는 자리)에 나타난다.

1. **원인**: 이 강제 슬롯은 하드코딩이 아니라 "첫 피격이 실제로 발생하는 순간 플레이어 위치 기준으로 아직 안 지나간 가장 가까운 빈 슬롯"을 매번 동적으로 계산하는 방식(`TutorialManager.TryFindNextPlaceableSlot`, 2026-09-10에 하드코딩 슬롯이 이미 지나간 타일을 가리켜 소프트락 나던 버그를 고치며 도입된 로직, 자세한 경위는 `PROGRESS_ARCHIVE.md` 참고). 이 로직이 내부적으로 `PathManager.CanPlaceAt()`을 그대로 썼는데, 여기에 방금 추가된 `wallPlacementGraceTiles`가 적용되면서 "현재 타일"까지도 배치 가능으로 판정 → 강제 슬롯이 플레이어보다 한 칸 앞이 아니라 플레이어가 막 도달한 바로 그 타일(화면상 이미 지나온 것처럼 보이는 자리)을 고르게 됐던 것.
2. **⚠️ 검토했지만 채택 안 한 방법(사용자에게 먼저 확인)**: "타일3 왼쪽으로 그냥 고정"하는 방법은 2026-09-10에 겪은 것과 똑같은 소프트락 재발 위험이 있어(첫 피격이 늦게 나서 플레이어가 이미 타일3을 지나친 상태일 수 있음) 사용자 확인 후 배제.
3. **수정(`TutorialManager.cs`, 스크립트만, 씬 변경 없음)**: `TryFindNextPlaceableSlot()`이 `pathManager.CanPlaceAt(t)`(grace 포함) 대신 `t > pathManager.GetCurrentPlayerTileIndex()`(grace 미적용, 예전 기준)로 직접 판정하도록 변경. 일반 자유 배치의 grace는 그대로 유지되고, 이 튜토리얼 강제 슬롯 탐색 한 곳만 "플레이어보다 확실히 앞선 자리"로 되돌림. 동적 탐색 구조 자체는 그대로라 2026-09-10 소프트락 버그는 재발하지 않음.
4. **검증**: Play 모드에서 `pathManager.SetCurrentPlayerTileIndex(5)` 후 reflection으로 `TryFindNextPlaceableSlot` 직접 호출 → `CanPlaceAt(5)=True`(grace 때문에 여전히 참)임에도 결과는 정확히 **타일6**(현재 타일보다 큰 첫 타일)로 나옴 — 수정 전이라면 타일5를 골랐을 상황. 콘솔 신규 에러 없음(무관한 기존 'NoSubscription' 에러 제외).
5. 재빌드 완료(`Builds/Windows/여우는 죄가 없다.exe`).

### ✅ 완료 — Stage5 가시함정 시야 버그 수정 + 재빌드 (2026-09-21, 같은 날 후속)

사용자 제보: "가시 함정이 한번 시야에 들어와서 보이기 시작한 이후에 안 보이는 현상이 있는 것 같다".

1. **원인**: `FogVisibility.cs`는 시야 판정을 부착된 게임오브젝트 자신의 `transform.position` 기준으로 하는데, `SpikeTrap.cs`는 실제 가시 판정 박스를 `tileIndexA`/`tileIndexB` 타일 좌표로 매 프레임 별도 계산하고 오브젝트 자신의 transform은 그 위치로 옮기지 않는 구조. Stage5의 `SpikeTrap40_41`(부모 `Trap` 오브젝트 하위) 오브젝트 자신의 위치가 `(1.07, 0, 24.04)`로, 실제 가시가 서 있는 타일(40·41) 중심 `(-17, 0, 24)`와 약 18유닛 떨어져 있었음(visionRadius=8보다 훨씬 큼) — 배치/복제 시 transform을 옮기지 않은 개별 실수. 그래서 플레이어가 엉뚱한 지점(x≈1 근처)을 지날 때 먼저 보였다가, 실제 가시 쪽(x≈-17)으로 다가갈수록 오히려 거리가 멀어져 다시 숨어버리는 현상이 발생했음.
   - **비교 확인**: Stage2(Tutorial)의 가시함정은 transform이 타일 중심과 정확히 일치(거리 0), Stage3의 가시함정 2개도 거리 0.26/0.71로 정상 — 코드 자체의 구조적 버그가 아니라 Stage5의 이 오브젝트 1개만의 배치 실수였음.
2. **수정**: `SpikeTrap40_41`의 local position을 `(-17.24, 0, 17.45)`로 옮겨(부모 `Trap` 오브젝트 오프셋 `(0.24,0,6.55)` 반영, world 기준 `(-17,0,24)`) 실제 판정 박스 중심과 일치시킴(오차 0.005유닛). **스크립트 변경 없음, 씬 값만 수정.** Stage5 씬 저장 완료, 콘솔 신규 에러 없음(무관한 기존 'NoSubscription' 에러 제외).
3. 재빌드 완료(`Builds/Windows/여우는 죄가 없다.exe`).

### ✅ 완료 — 정지 스킬 튜토리얼 문구 보강 + 벽 배치 판정 여유 추가 (2026-09-21)

사용자 요청 2가지:
1. **정지 스킬 튜토리얼 안내 보강**: Stage2(Tutorial)의 `stop_flare_unlock` 스텝 첫 페이지 문구에 "다시 누르면 즉시 풀립니다."를 추가(2026-09-18에 구현한 재입력 즉시 해제 기능이 튜토리얼 문구에는 반영 안 돼 있었음). 최종 문구: "정지 스킬이 열렸습니다. 잠깐 멈춰서 상황을 파악할 수 있습니다. 다시 누르면 즉시 풀립니다. (숫자 2번으로도 사용할 수 있습니다)". `execute_code`+`SerializedObject`로 `TutorialManager.steps` 배열 직접 수정, 씬 저장 완료.
2. **벽 배치 "이미 지나감" 판정이 너무 빡빡함**: `PathManager.CanPlaceAt(tileIndex)`가 `tileIndex > currentPlayerTileIndex`(현재 타일 자체도 배치 불가)였던 것을, 새 필드 `wallPlacementGraceTiles`(기본값 **1**)를 추가해 `tileIndex > currentPlayerTileIndex - wallPlacementGraceTiles`로 변경 — 즉 **플레이어가 막 도달한 것으로 판정된 현재 타일까지는 배치를 허용**(그 이전 타일은 여전히 불가). 스크립트 기본값이라 기존 씬 전부(스테이지1~5, 템플릿)에 별도 씬 수정 없이 자동 적용됨. Play 모드에서 `SetCurrentPlayerTileIndex(5)` 후 `CanPlaceAt` 결과로 확인: 타일3·4=false, 타일5(현재)·6=true(기존엔 타일5도 false였음).
   - ⚠️ **임의로 정한 값**: `wallPlacementGraceTiles=1`은 "현재 타일 하나만 추가 허용"하는 최소한의 조정값 — 사용자가 지적한 "아직 안 지나간 것 같은데 막힌다"는 체감이 이걓로 충분한지 실플레이 후 확인 필요. 부족하면 값을 늘릴 것(예: 2로 하면 현재 타일 기준 1칸 더 이전 타일까지 허용).
- 컴파일 에러 없음, Stage2(Tutorial) 씬 저장 완료(PathManager는 스크립트 기본값이라 다른 씬은 저장 불필요).
- 위 변경사항 반영해 재빌드 완료(`Builds/Windows/여우는 죄가 없다.exe`, 에러 0개, 경고 486개는 기존과 동일한 일반 경고).

### ✅ 완료 — 조명탄 조준 취소 기능 + 재빌드 (2026-09-18, 같은 날 후속11)

사용자 요청: 조명탄 조준 중(착지 전) 다시 누르면 취소되게, 실제 사용이 아니므로 횟수 차감 안 함. `SkillManager.cs`에 `CancelFlareAim()` 추가 — `TryUseSkill()`에서 `awaitingFlareTarget && pendingFlareIndex == index`(아직 착지 전 조준 상태)일 때 이 분기로 빠져서 조준 취소 + 조준 미리보기 제거 + 벽 배치 입력 잠금 해제 + **버튼 누른 시점에 미리 차감했던 `remainingUses`를 다시 복원**. 착지 후(리빌 지속시간 중)는 이미 실제로 쓴 것이라 취소 대상 아님(그 상태에선 `awaitingFlareTarget`이 이미 false라 자동으로 분기 안 탐).
- Play 모드에서 확인: 조준 시작 시 remaining 2→1, 재입력으로 취소 시 remaining 1→2 복원, `isActive`/`awaitingFlareTarget` 전부 정상 해제. 컴파일 에러 없음.
- 재빌드 완료(`Builds/Windows/여우는 죄가 없다.exe`, 에러 0개).

### ✅ 완료 — 정지 스킬 재입력 시 즉시 해제 + 재빌드 (2026-09-18, 같은 날 후속10)

사용자 요청: 정지 스킬을 쓴 상태에서 같은 버튼/숫자키(2번)를 한 번 더 누르면 남은 지속시간을 버리고 즉시 풀리게. `SkillManager.cs` 수정:
- `RunStop` 코루틴 참조를 `stopRoutine` 필드에 저장.
- `TryUseSkill()` 맨 앞에 "정지 스킬이 이미 활성 중이면(`isActive`) 그 코루틴을 `StopCoroutine`으로 중단 + `playerAutoMover.SetPaused(false)` + `vfx.StopFreeze()` + `isActive=false`로 즉시 해제"하는 분기(`CancelStop`) 추가 — **추가 사용 횟수는 소모하지 않음**(취소는 무료).
- 가속·무적·조명탄은 이번 요청 범위 밖이라 그대로 둠(기존처럼 지속시간 동안 재사용 불가).
- Play 모드에서 `TryUseSkill(1)` 두 번 호출로 확인: 1차 후 `isActive=true remaining=1`(2회 중 1회 사용), 2차 후 `isActive=false remaining=1`(횟수 그대로), `PlayerAutoMover.IsPaused=false` 정상 복귀. 컴파일 에러 없음.
- 재빌드 완료(`Builds/Windows/여우는 죄가 없다.exe`, 에러 0개).

### ✅ 완료 — 스킬 순서 전 스테이지 통일 + 가속 스킬 튜토리얼 숫자키 안내 + 재빌드 (2026-09-18, 같은 날 후속8/9)

1. **스킬 튜토리얼 숫자키 안내**: Stage2(Tutorial)의 스킬 해금 문구 4개(가속/정지/조명탄/무적)에 "(숫자 N번으로도 사용할 수 있습니다)" 안내를 추가(각 스킬이 배열에서 몇 번째 슬롯인지에 따라 1~4 매핑, `SkillManager.Update()`의 `digit1~4Key` 바인딩과 일치시킴).
2. **스킬 순서 통일**: 사용자가 플레이해보고 "스테이지마다 스킬 순서가 다르다"고 지적 — Stage3·Stage4·Stage5·`Stage_Template`의 `SkillManager.skills` 배열이 전부 [가속,무적,정지,조명탄] 순서였던 것을 Stage2(Tutorial) 기준([가속,정지,조명탄,무적], 즉 숫자키 1=가속/2=정지/3=조명탄/4=무적)에 맞춰 재배열. **각 스킬의 `totalUses`/`duration`/`startLocked`/아이콘 값은 그대로 유지**하고 배열 순서만 바꿈(`execute_code`+`SerializedObject`로 타입별 백업 후 재배치, 전부 검증 로그로 원래 값 유지 확인). Play 모드에서 Stage3 기준으로 key1~4가 실제로 가속/정지/조명탄/무적 순서로 매핑되는 것 확인.
3. 사용자가 직접 다른 수치(밸런스) 조정도 진행한 상태에서 재빌드 요청 → 콘솔 에러 없음 확인 후 `Builds/Windows/여우는 죄가 없다.exe` 재빌드(에러 0개), 이후 스킬 순서 통일까지 반영해 한 번 더 재빌드 완료.

### ✅ 완료 — 첫 빌드 배포 + 플레이테스트 설문 자동 연결 (2026-09-18, 같은 날 후속7)

1. **첫 Windows 빌드**: `Builds/Windows/여우는 죄가 없다.exe`로 빌드(제품명도 "MCP" 기본값에서 "여우는 죄가 없다"로 변경). 에러 0개(경고 486개는 일반적인 빌드 경고). 사용자가 `플레이_안내문.txt`(게임 소개·설치법·조작법·수집 데이터 안내)를 직접 작성해 같은 폴더에 둠.
2. **구글 폼 설문 자동 연결**: 사용자가 구글 폼을 직접 만들어 링크 제공. `StageManager.cs`에 `surveyFormUrl` 필드 추가 — 마지막 스테이지(Stage5) "GAME CLEAR" 시점에 `Application.OpenURL()`로 자동 오픈. 추가로 "끝까지 클리어 못 하고 중간에 그만두는 사람도 잡고 싶다"는 요청으로 `MainMenuController.cs`에도 같은 필드를 추가해 "종료" 버튼 클릭 시에도 열리게 함(창 닫기/Alt+F4 강제종료는 타이밍상 불안정해서 제외, 명시적 종료 버튼만 확실히 보장).
3. Play 모드에서 두 스크립트 모두 실제 URL 값이 정상 반영된 것 확인, 컴파일 에러 없음, 변경사항 반영해 재빌드 완료(2번째 빌드, 18초 만에 증분 빌드 성공).

### ✅ 완료 — Stage4 배경 데코 회색 표시 버그 수정 (2026-09-18, 같은 날 후속6)

빌드 전 최종 확인 중 사용자가 Stage4의 새 데코 34개가 전부 회색으로 보인다고 지적. 확인해보니 전부 `MeshRenderer.sharedMaterial`이 URP 기본 폴백 머티리얼(`Packages/.../Lit.mat`)로 들어가 있었음(에셋 원본 머티리얼이 안 연결된 상태) — 나무 8종은 `Trees.mat`, 바위류(`Rock_Sharp_*`/`Rock_Round_*`/`Rock_Square_*`/`Rock_Block_*`)는 `Rocks_Moss.mat`, 크리스탈은 `Crystals.mat`, 버섯·죽은관목은 `Vegetation.mat`로 각각 올바른 카테고리 머티리얼을 찾아 일괄 재연결(`execute_code`로 이름 접두사 매칭). 스크린샷으로 나무는 초록잎+갈색줄기, 크리스탈은 청록색, 바위는 이끼색으로 정상 표시되는 것 확인. 씬 저장, 콘솔 에러 없음.

### ✅ 완료 — `PlayStatsLogger` 구글 시트 자동 업로드 추가 (2026-09-18, 같은 날 후속5)

design.md 10장 검증 로그를 빌드 배포 후에도 수동으로 파일을 걷어올 필요 없이 자동 수집하기 위해, 사용자가 구글 시트+Apps Script 웹앱(POST 받아서 행 추가)을 직접 만들고, Unity 쪽에 전송 코드를 추가함.
1. `PlayStatsLogger.cs`에 `sheetsWebAppUrl`(SerializeField, 비워두면 기존과 동일하게 로컬 CSV만 기록) 필드 추가. **기존 측정 항목·로컬 CSV 기록 로직은 전혀 안 바꿈** — `WriteRow()` 끝에 같은 데이터를 `UnityWebRequest.Post`로 그 URL에 추가 전송하는 코드만 붙임(`SendToGoogleSheets` 코루틴, 실패해도 경고 로그만 남기고 게임에 영향 없음).
2. **삽질 기록**: Apps Script 웹앱은 POST 처리 후 항상 결과화면(googleusercontent.com)으로 302 리다이렉트하는데, `UnityWebRequest`가 이 리다이렉트를 자동으로 따라가려 하면 응답을 못 받고 타임아웃남(curl도 동일 증상 확인함— POST+`-L`로 리다이렉트 따라가면 411/405 에러). 실제 시트 기록(`doPost` 실행)은 최초 POST 시점에 이미 끝나므로 리다이렉트를 따라갈 필요가 없음 — `request.redirectLimit = 0`으로 막고, `responseCode`가 200 또는 302면 성공으로 판정하도록 수정. Play 모드에서 `StageManager.HandleClear()`를 직접 호출해 실제로 시트에 행이 추가되는 것까지 확인(콘솔 "구글 시트 전송 완료 (code=302)").
   - ⚠️ 참고: 도메인 리로드 직후(Play 모드 진입 바로 몇 초 이내) 호출하면 간헐적으로 타임아웃(코드 0)이 뜬 적이 있음 — 실제 플레이에서는 스테이지 클리어까지 시간이 걸리니 문제 없을 것으로 판단, 그래도 재발하면 참고할 것.
3. `Stage1(Tutorial)`/`Stage2(Tutorial)`/`Stage3`/`Stage4`/`Stage5`/`Stage_Template` 전부의 `PlayStatsLogger.sheetsWebAppUrl`에 동일한 웹앱 URL 설정, 씬 저장 완료. 컴파일 에러 없음.

### ✅ 완료 — Stage4 후속 조정: visionRadius 통일 + 낙석 딜레이 하한 0.15초 (2026-09-18, 같은 날 후속4)

Stage4 완성 후 사용자가 직접 밸런스 확인차 낙석 함정 일부(RockfallTrap (3))를 삭제하고 간격을 재조정함(9개→8개). 이어서 요청 2가지 반영:
1. **visionRadius 통일**: `Monster_DPSType`×2(`AttackRangeIndicator`+`FogVisibility`)와 낙석 함정 8개의 `FogSensor`(`FogVisibility`) — 전부 템플릿 기본값 8로 남아있던 것을 다른 스테이지와 통일된 **7**로 변경. `Monster_Basic_Sample`×4·`FogGround`는 이미 7이라 그대로 둠.
2. **낙석 타이밍 하한 0.15초**: `idleDuration`/`startDelay`가 0.15초 미만인 것만 재추첨(0.15~0.6초 범위) — `RockfallTrap (1).idleDuration`(0.148→0.299), `RockfallTrap (6).idleDuration`(0.015→0.368), `RockfallTrap (6).startDelay`(0.061→0.501), `RockfallTrap (7).startDelay`(0.091→0.394). 0.15 이상이던 나머지 값은 그대로 둠. `warningDuration`/`fallDuration`은 요청대로 손 안 댐(참고: 사용자가 직접 조정해 전부 0.4/0.2로 이미 바뀌어 있었음 — 이번 작업에서 건드리지 않음).
- `execute_code`+`SerializedObject`로 일괄 처리, 씬 저장 완료, 콘솔 에러 없음(무관한 기존 'NoSubscription' 에러 제외).

### ✅ 완료 — 도미노 낙석 스테이지(Stage4) 완성 + 스테이지 3-way 리네임 (2026-09-18, 같은 날 후속3)

사용자가 "직선 길에 낙석 함정만 도미노처럼 설치해서 가속 스킬을 타이밍 맞게 써야 지나갈 수 있는 스테이지"를 `Stage_Template`을 복제해 `Stage5.unity`로 직접 제작(타일 35개 직선, 몬스터 6마리, 낙석 함정 9개, 가시함정 없음). 이후 "이 스테이지를 Stage4로, 기존 Stage4는 Stage3으로, 기존 Stage3는 Stage5로 바꿔달라"는 요청에 따라 코디네이터가 먼저 파일을 3-way 리네임했고, 이어서 새 Stage4 내용을 표준 절차로 완성했다.

1. **스테이지 3-way 리네임(코디네이터가 직접 처리)**: `Assets/Scenes/Stage3.unity`(임시명 경유)→`Stage4.unity`→`Stage5.unity` 순으로 `manage_asset`(move) 3회 실행. ⚠️ **버그 발견**: 이 환경의 `manage_asset` move/rename 액션이 실제로는 성공해도 `"success": false`를 반환함 — 매번 Bash로 실제 파일 목록을 재확인해서 검증함(신뢰 금지, 재발 가능성 있음). GUID는 리네임 후에도 유지됨(확인됨). git이 없는 프로젝트라 리네임 전에 원본 3개 씬을 `...scratchpad/scene-backup-20260918/`에 백업해둠(작업엔 영향 없음, 그대로 둘 것). Build Settings도 `manage_build(scenes)`로 MainMenu→Stage1→Stage2→Stage3→Stage4→Stage5 순서로 재정렬. `Stage3.unity`(구 Stage4 내용)는 `nextSceneName="Stage4"`+`StageLabel="Stage 3"`로, `Stage5.unity`(구 Stage3 내용)는 `nextSceneName=""`(이제 마지막 스테이지)+`StageLabel="Stage 5"`로 수정 완료. `Stage2(Tutorial)`의 `nextSceneName="Stage3"`는 변경 불필요(문자열이라 리네임된 파일을 자동으로 다시 가리킴).
2. **Stage4(새 도미노 스테이지) 완성**:
   - `Floor`(NavMesh) pos(0,0,0)/scale(3,1,6)→**pos(0,0,34)/scale(3,1,8)**(X[-15,15] Z[-6,74])로 확장 후 재베이크 — 원래 범위로는 타일 다수·몬스터 상당수가 NavMesh 밖이었음(Stage3·4 제작 때와 같은 종류의 버그, 미리 잡음). 35타일 전부 NavMesh 확인.
   - `WallSlots`/`SlotPedestals` 70개(35타일×2) 생성 — **완전 직선 경로라 코너 예외 처리가 전혀 필요 없었음**(과거 Stage3의 "코너 슬롯 겹침" 문제 자체가 발생 안 함).
   - `ArrivalPortal`을 마지막 타일(Tile_34, 월드좌표 (0,0,68))에 배치.
   - `GroundBumps`(core, Floor와 동일 범위)/`GroundBumps_Extra`(도넛형, X[-26,26] Z[-12,88]) 절차 생성 — 평탄화 계산 시 **여러 후보 지점 중 최솟값(가장 강한 평탄화)을 쓰는 수정된 방식**을 처음부터 적용(2026-09-17 Stage3 버그 재발 방지). 메시 에셋은 `Assets/Mesh/M_GroundBumps_StageDomino.asset`/`_Extra.asset`으로 저장(⚠️ 리네임으로 번호가 섞여서 "Stage4"라는 이름을 구 Stage4 메시 에셋이 이미 쓰고 있어 이름 충돌 방지용으로 `StageDomino`라는 별도 이름을 씀 — 나중에 정리하고 싶으면 참고).
   - `FogGround`를 `GroundBumps_Extra`와 정확히 같은 범위로 설정: pos(0,0.6,38)/scale(5.2,1,10) — 2026-09-18에 Stage3·4에서 겪은 "안개 미커버 경계선" 버그를 처음부터 피함.
   - **배경 데코**: 낙석/협곡 테마로 34개 배치 — 사용자 요청대로 Stage1/2에 있는 큰 나무 8종(Acacia/Apple/Random_Tree_crk/Fir/Oak/Palm/Pine_Tree_OneSided/Random_Tree_s, 기존에 검증된 안전 스케일 그대로 재사용, 특히 `Pine_Tree_OneSided_l_01`은 0.18~0.3만 사용) 12그루 + 뾰족바위·죽은관목·버섯·크리스탈·큰바위 등 18종 22개. **`FogVisibility`는 붙이지 않음**(배경/데코는 항상 보이는 게 규칙). 경로·몬스터·함정과 최소 3유닛, 데코 간 최소 2.2유닛 간격을 코드로 강제. 통로 정면(코너 없는 직선 중앙)에는 안 놓고 옆(X 4~24 / -24~-4 범위)에만 배치해서 낙석을 미리 보는 시야를 안 가리게 함.
   - **스킬/재화**: 공식대로 가속=**10**(낙석9+1), 정지=조명탄=**2**(Ceil(35/30)), 무적=**0**(가시함정 없음, 사용자가 명시적으로 요청) — Play 모드 스크린샷으로 무적 버튼이 자동 숨김되는 것 확인. `CurrencyManager.totalCurrency`=**6**(사용자가 직접 확정).
   - **핵심 기믹 — 낙석 타이밍 랜덤화**: 9개 `RockfallTrap` 전부의 `idleDuration`/`startDelay`를 각각 독립적으로 `Random.Range(0,0.6)`초로 재설정(스크립트 코드는 안 건드림, 씬 값만 고정). `warningDuration`(1.2)·`fallDuration`(0.3)·`debrisDuration`(0.5)·`impactRadius`·`damage`는 전부 그대로. 실제 사용한 값(Z 오름차순):

     | 트랩(월드 Z) | idleDuration | startDelay |
     |---|---|---|
     | Z=8.08 | 0.325 | 0.196 |
     | Z=14.04 | 0.148 | 0.345 |
     | Z=20.05 | 0.208 | 0.232 |
     | Z=26.02 | 0.071 | 0.507 |
     | Z=32.02 | 0.398 | 0.159 |
     | Z=38.06 | 0.574 | 0.447 |
     | Z=44.05 | 0.015 | 0.061 |
     | Z=51.91 | 0.529 | 0.091 |
     | Z=60.03 | 0.442 | 0.364 |
   - `StageLabel`="Stage 4", `StageManager.nextSceneName`="Stage5"로 설정.
3. **⚠️ 발견했지만 안 고친 것(visionRadius라 임의 변경 금지 규칙 때문에 보고만 함)**: 이 스테이지의 `Monster_DPSType` 2마리와 낙석 함정 9개의 `FogSensor`(전부 `FogVisibility`, 일부는 `AttackRangeIndicator`)의 `visionRadius`가 **8**로 남아있음(템플릿 기본값, 한 번도 7로 안 맞춰짐) — `Monster_Basic_Sample` 4마리는 7로 정상. 프로젝트 표준(전 스테이지 7 통일)과 다름. 사용자 확인 후 7로 맞출지 결정 필요.
4. **검증**: Play 모드 스크린샷으로 Stage4 라벨·스킬바(가속10/정지2/조명탄2, 무적 버튼 숨김) 확인, 통로 중간·끝 지점 스크린샷으로 시야 안 막힘 확인, `NavMeshAgent.isOnNavMesh=true` 확인. 콘솔에 관계없는 기존 'NoSubscription' 에러 외 신규 에러 없음. 씬 저장 완료.

### ✅ 완료 — 4개 스테이지 배경 데코 추가 배치(배경이 심심하다는 피드백) (2026-09-18, 같은 날 후속2)

사용자 요청: 배경이 심심함 → Stage1·2에는 배경 오브젝트를 더 다양하게, Stage3·4에는 Stage1/2에 있는 나무 종류도 넣고 다른 오브젝트도 추가. 제약: 시야를 완전히 막을 정도로 크면 안 되지만 부분적으로 가리는 건 허용.

1. **Stage1(Tutorial)**: 기존에 없던 종류(버섯 Mushroom_a_02/c_03, 죽은 관목 Dead_Bush_a_02/b_03, 갈대 Reed_a_02/b_04, 뾰족바위 Rock_Sharp_m_05/l_02, 크리스탈 Crystal_m_04) 총 18개 추가. 전부 `Assets/LMHPOLY/Low Poly Nature Bundle/` 기존 에셋 재사용(새 프리팹 제작 없음).
2. **Stage2(Tutorial)**: Stage1과 겹치지 않는 다른 종류(버섯 Mushroom_e_05/k_04, 죽은 관목 Dead_Bush_a_04, 갈대 Reed_a_05, 뾰족바위 Rock_Sharp_m_12, 크리스탈 Crystal_crk_m_01/Crystals_l_02) 총 19개 추가 — 같은 카테고리라도 스테이지마다 다른 변형을 써서 "스테이지마다 다르게" 원칙 유지.
3. **Stage3**: Stage1/2에서 쓰는 큰 나무 8종(Acacia/Apple/Random_Tree_crk/Fir/Oak/Palm/Pine_Tree_OneSided/Random_Tree_s, 전부 과거에 검증된 안전 스케일 범위 그대로 재사용 — 특히 `Pine_Tree_OneSided_l_01`은 스케일 1이면 높이 19~25유닛까지 치솟는 위험한 에셋이라 반드시 0.2~0.3 스케일만 사용) 15그루 + 기존 바위·크리스탈 테마를 보완하는 새 종류(Rock_Arch_03, Stonehenge_02, Crystal_m_08) 4개, 총 19개 추가.
4. **Stage4**: 같은 나무 8종 12그루 + 기존 관목·꽃·풀 테마를 보완하는 새 종류(Dead_Bush_b_01, Reed_b_02, Mushroom_i_03) 8개, 총 20개 추가.
5. **배치 방식**: 각 씬의 `Environment` 오브젝트 하위에 프리팹 인스턴스로 배치(`PrefabUtility.InstantiatePrefab`, 리네임·신규 에셋 생성 없음), 경로 타일·몬스터·함정·도착 포탈과 최소 3유닛, 기존 데코와 최소 2.0~2.2유닛 간격을 코드로 강제하며 무작위 배치(회전 무작위, 크기는 사전에 실측한 안전 범위 내 무작위).
   - ⚠️ **정정(같은 작업 중)**: 처음엔 새 오브젝트에도 기존 데코처럼 `FogVisibility`를 붙이고 `visionRadius`를 복사했었는데, 사용자 확인 결과 **실제 규칙은 "몬스터·함정 등 게임플레이 오브젝트만 시야 제한을 받고, 배경/데코는 시야 밖이어도 항상 보여야 한다"**였음(Stage1의 기존 데코 중에도 앞쪽 일부만 `FogVisibility`가 있고 나중에 "여백 채우기"로 추가된 뒤쪽 데코들은 원래부터 없었던 것이 이 규칙의 증거). **이번에 새로 추가한 오브젝트에 한해서만** 방금 붙인 `FogVisibility`를 전부 제거함(4개 씬 총 76개: Stage1 18/Stage2 19/Stage3 19/Stage4 20, 전부 개수 일치 확인). 기존에 원래 있던 데코의 `FogVisibility`(붙어있는 것도, 없는 것도)는 이번 작업 범위 밖이라 건드리지 않음.
6. **검증 중 발견+수정**: Stage4에서 시작 지점 바로 뒤쪽(카메라가 바라보는 통로 축 연장선상)에 배치된 `Acacia_Tree_crk_l_01` 1그루가 실측 거리는 4유닛 이상 떨어져 있었는데도, 카메라가 통로를 따라 낮은 각도로 비스듬히 내려다보는 특성상 화면상으로는 캐릭터를 거의 완전히 가려 보이는 문제를 Play 모드 스크린샷으로 발견 — 해당 1그루만 `(-8,0,-6)`으로 재배치해 해결(수치상 거리 체크만으로는 못 잡는, 카메라 시점 기준 착시적 겹침 사례라 기록해둠 — 다음에 통로 시작/끝 지점 바로 앞뒤에 오브젝트를 배치할 때는 거리뿐 아니라 실제 스크린샷으로 한 번 더 확인할 것).
7. **검증**: 4개 씬 전부 Play 모드 진입 후 시작 지점 스크린샷으로 경로가 완전히 가려지지 않는 것 확인, Stage3·4는 나무가 배치된 구간도 추가로 확인. 콘솔 신규 에러 없음(무관한 기존 'NoSubscription' AI 구독 에러, 그리고 다른 세션 활동으로 보이는 과거 `SpikeTrap.cs` 관련 Quaternion 경고는 이번 작업과 무관 — 재확인 시 더 이상 나타나지 않음). 4개 씬 전부 저장 완료.
8. **⚠️ 특이사항 기록**: 이번 작업 도중 한때 활성 씬이 내 의도와 다르게(예: `Stage_Template`) 바뀌어 있는 현상이 여러 번 관측됨 — 같은 Unity 에디터를 동시에 다른 세션(메인 대화)이 조작하고 있어서 씬 전환이 서로 겹친 것으로 추정됨. 이후로는 씬 열기+검증+배치+저장을 전부 하나의 `execute_code` 호출 안에서 원자적으로 처리(중간에 활성 씬을 재확인하고, 저장 직전에도 다시 확인해 다른 씬으로 바뀌어 있으면 저장을 건너뛰도록 방어)해서 안전하게 마무리함. 앞으로 Unity MCP로 여러 세션이 동시에 작업할 가능성이 있다면 이 레이스 컨디션을 감안할 것.

### ✅ 완료 — 가속 스킬 배율 너프 2.0→1.5 (2026-09-18, 같은 날 후속)

사용자가 "가속 스킬이 너무 빨라지는 것 같다"고 지적, 확인 후 1.5로 너프 요청. `SkillManager.accelerateSpeedMultiplier`(기존 2.0, 스크립트 기본값 그대로 방치돼 있었음)를 **1.5**로 변경 — `Stage2(Tutorial)`/`Stage3`/`Stage4`/`Stage_Template` 전부(스킬 시스템이 있는 씬 전체) 동일하게 적용, `Stage1(Tutorial)`은 `SkillManager` 자체가 없어 해당 없음. `PlayerAutoMover.maxSpeedMultiplier`(상한값 2.5)는 안 건드림. Play 모드에서 런타임 값 1.5 반영 확인, 컴파일 에러 없음, 네 씬 저장 완료.

### ✅ 완료 — Stage3·Stage4 안개 미커버 범위 수정 + 메인메뉴 타이틀 이미지 교체 (2026-09-18)

1. **Stage3·Stage4 안개(`FogGround`) 배경 미커버 버그 발견+수정**: 사용자가 "Stage4에 안개가 전부 안 깔린 것 같다"고 지적. 확인해보니 `FogGround`(안개 셰이더가 실제로 그려지는 평면)가 `Floor`/NavMesh 범위에만 맞춰져 있고, 카메라가 실제로 보는 배경 확장 지형 `GroundBumps_Extra`는 그보다 훨씬 넓어서, `FogGround`가 없는 바깥 영역은 안개 자체가 안 그려져 항상 밝게 보이는 뚜렷한 경계선(seam)이 있었음. 2026-09-16에 스테이지1/2/템플릿에서 이미 한 번 고쳤던 것과 같은 문제("배경 확장 영역까지 안개를 덮을 것")가 Stage3·4를 새로 만들 때 재발한 것. **수정**: 두 스테이지 모두 `FogGround`를 `GroundBumps_Extra`와 같은 범위로 확장 — Stage4는 position(7.11,0.6,18)/scale(7.6,1,5.8) (기존 (6.5,0.6,13)/(3.5,1,4.2)에서 변경), Stage3는 position(-1,0.6,22)/scale(10.8,1,8.0) (기존 (0,0.6,16)/(6.2,1,5.2)에서 변경). `Floor`(NavMesh)는 그대로 둠 — 시각적 안개 평면만 넓힌 것이라 게임플레이 영향 없음. Play 모드에서 경계 지점 스크린샷으로 seam이 사라진 것, 평소 게임플레이 화면은 그대로인 것 확인. 컴파일 에러 없음, 두 씬 저장 완료.
2. **메인메뉴 타이틀 이미지 교체**: 사용자가 `Assets/UI_Image/main.png`(투명 배경 PNG, 여우 얼굴+"여우는 죄가 없다" 글자가 이미 그려진 완성형 타이틀 로고)를 준비해와서 요청. 기존 `TitleText`(TMP 텍스트)와 `TitleFrame`(베이지색 배경 판)은 비활성화(삭제 안 함)하고, `main.png`를 Sprite(2D and UI)로 임포트 설정 변경 후 새 `TitleLogo` 오브젝트(Image, preserveAspect=true, anchor 기존 타이틀 자리와 동일)로 교체함 — 로고 자체에 배경 그림이 이미 포함돼 있어 기존 베이지 판과 안 어울려서 판은 껐음(필요하면 다시 켤 수 있음). Play 모드 스크린샷으로 확인 완료.

### ✅ 완료 — Stage3 벽 개수 조정 + Stage4 제작 + Stage_Template 복원 (2026-09-17, 같은 날 후속10)

1. **Stage3 벽 개수**: `totalCurrency` 5 → **10**으로 조정(확인 결과 Stage1은 몬스터당 1.5개, Stage2는 1.4개인데 Stage3는 몬스터8마리에 5개=0.6개로 템플릿 기본값이 그대로 남아있던 것 — 사용자가 직접 10으로 확정).
2. **`Stage_Template.unity`가 프로젝트에서 사라진 상태였음(발견)**: 그래서 사용자가 Stage4를 새로 만들 때 복사할 템플릿이 없어 수동으로 만들 수밖에 없었던 것. Stage4를 검사해보니(Missing Script 없음·프리팹 Connected·`PathManager.tileRoot` 정상·`NavMeshSurface` 설정 정상·`Managers`/`TutorialManager`의 빈 참조들도 전부 `FindFirstObjectByType` 자동 해결 패턴으로 Stage3와 동일) **실제로는 정상 작동하는 씬**이라 처음부터 다시 만들 필요는 없었음 — 사용자 확인 후 그대로 진행.
   - **`Stage_Template.unity` 복원**: Stage4.unity를 복제해 새로 만들고, 타일 36개→3개(직선 샘플), 몬스터 7마리→타입별 1개(`Monster_Basic/Area/DPS_Sample`), 함정 5개→2개(`RockfallTrap`, `SpikeTrap`)로 정리. `CurrencyManager.totalCurrency`=5, `SkillManager.skills` 전부 `totalUses=0`·`startLocked=false`, `TutorialManager.tutorialEnabled=false`, `StageManager.nextSceneName`="", `StageLabel` 텍스트를 "Stage N"으로 정리. `WallSlots`/`SlotPedestals`/`Environment`/`GroundBumps`/`ArrivalPortal`은 원래부터 없어서 그대로 유지(스테이지마다 새로 만드는 것이 맞음). Build Settings에는 포함 안 함(재생용 씬이 아니므로).
3. **Stage4 제작**(Stage3와 동일 절차): 배치 확인(타일36·몬스터7[기본3/광역1/DPS3]·함정5[낙석3/가시2]) → `Floor`/`FogGround`를 pos(6.5,0,13) scale(3.5,1,4.2)로 넓히고 NavMesh 재베이크(전 타일 확인) → `WallSlots`/`SlotPedestals` 72개 생성(코너 2곳: 타일14·21, 겹침 0건) → `ArrivalPortal` 마지막 타일에 배치 → `GroundBumps`/`GroundBumps_Extra` 절차 생성(이번엔 처음부터 [[스테이지3에서 고친 평탄화 버그]] 반영해서 생성, 타일 근처 높이 0 확인) → 데코 50개 배치(스테이지1·2·3과 다른 테마 — 관목·꽃·풀 위주, 키 큰 나무 없음, 사전에 각 프리팹 높이 검증 후 배치) → 스킬 공식 적용(가속4/정지2/조명탄2/무적2) → 튜토리얼 비활성화 → 벽 개수 10(스테이지3과 동일 비율) → `StageLabel` "Stage 2" 오타 발견+"Stage 4"로 수정(Stage4도 템플릿 복사 흔적으로 같은 오타 있었음) → Stage3의 `nextSceneName`을 "Stage4"로 연결 → Build Settings에 Stage4 추가.
4. **Play모드 검증**: 벽 배치(일반+코너 2곳) 정상, 마지막 타일 도달 시 GAME CLEAR+메뉴로 돌아가기 정상 진입, 벽 개수 텍스트("10개 중 3개 사용") 정상 표시, timeScale 정지 정상. 게임 카메라 시점 스크린샷으로 바닥 이어짐·시야 가림 없음 확인.

### ✅ 완료 — Stage3 바닥/나무 시야 가림 버그 수정 (2026-09-17, 같은 날 후속9)

**요청**: Stage3 바닥이 길을 가리는 부분이 많고, 키 큰 나무가 시야를 가림. 다른 곳에 오브젝트 추가 배치.

1. **바닥 버그(근본 원인)**: `GroundBumps` 절차 생성의 `FlattenFactor` 함수가 `flattenPts` 리스트를 순회하다 **첫 번째로 조건에 맞는 점에서 바로 반환**하는 구조였음 — 실제로 가장 가까운(완전 평탄화해야 할) 타일보다 리스트 순서상 앞선 타일이 "약한 평탄화" 조건만 만족해도 그 값으로 반환돼버려서, 타일 중심에서 1유닛밖에 안 떨어진 지점에 높이 0.37유닛짜리 굴곡이 튀어나옴(경로 바로 위로 초록 바닥이 솟아 길을 가리는 것처럼 보임). **수정**: 모든 점을 순회해 **최소값(가장 강한 평탄화)**을 쓰도록 변경 — 타일 반경 1.2유닛 이내 높이 0으로 확인. 진폭도 0.5→0.28로 낮추고 타일 평탄화 반경도 2.2→2.5로 소폭 확대. `GroundBumps_Extra`도 카메라 사각 코너에 살짝 빈틈이 보여 범위를 더 넓힘(X[-55,53] Z[-18,62]).
2. **나무 시야 가림(근본 원인)**: 데코 배치 시 모든 프리팹에 동일한 랜덤 스케일(0.85~1.35)을 적용했는데, `Low Poly Nature Bundle`의 일부 나무 원본 메시가 이 게임 스케일 기준으로 원래 매우 큼(`Birch_Tree_l_04`는 스케일1에서 높이 12.65유닛, `Acacia_Tree_crk_l_01`은 스케일1에서 높이 약 12~13유닛). Stage3에 배치한 `Birch_Tree_l_04`(6그루)·`Acacia_Tree_crk_l_01`(3그루)가 각각 최대 16.6유닛까지 치솟아 카메라 시야를 가림. **수정**: 해당 인스턴스를 높이가 검증된 작은 나무(`Fir_Tree_s_BT_09`, `Dead_Tree_s_04`)로 교체. `Rock_Square_l_01`(스케일1에서 높이 4유닛)도 일부 축소.
   - **같은 문제가 Stage1·Stage2에도 이미 있었음(내가 추가한 것 + 원래 있던 것 둘 다)**: `Pine_Tree_OneSided_l_01`이 스케일1(원래 배치, 내가 건드리지 않은 것)에서 Stage1은 19.75유닛, Stage2는 25.2유닛이나 됨 — 원래부터 있던 설계 누락으로 보임. 두 스테이지 모두 높이 7유닛 넘는 오브젝트(각 6개)를 찾아 목표 높이 4.5유닛으로 일괄 축소.
3. Stage3 외곽 빈 공간에 데코 25개 추가 배치(안전 확인된 프리팹만 사용, 총 80개).

**검증**: 실제 게임 카메라 위치/각도(플레이어 오프셋 (0,17,-12), 회전 (55,0,0))로 여러 지점(시작 구간·코너 2곳)에서 포지션 지정 스크린샷 촬영해 수정 전/후 비교 — 끊겨 보이던 길이 전부 이어져 보이고, 튀어나온 나무 없음을 눈으로 확인.

### ✅ 완료 — Stage3 제작 (2026-09-17, 같은 날 후속8)

사용자가 `Stage_Template`을 복사해 타일·몬스터·함정을 직접 배치 완료(50타일, 몬스터 8기: 기본4/광역2/DPS2, 함정 4개: 낙석3+가시1). 확인 결과 템플릿에서 사용자가 지운 게 배경 그라운드뿐 아니라 **`WallSlots`·`SlotPedestals`·`Environment`·`GroundBumps`/`GroundBumps_Extra`·`ArrivalPortal`이 전부 없는 상태**였음(`docs/level-design.md`의 "3장 절차에서 매번 새로 만드는 것들" 목록과 일치 — 템플릿엔 원래 이것들이 없고 스테이지마다 새로 만드는 게 정상 흐름).

1. **Floor/NavMesh 범위 부족(발견한 버그)**: 경로 타일이 X[-22,20] Z[0,36]까지 뻗는데 `Floor`(NavMesh 포함)는 기본값 X[-15,15] Z[-30,30]뿐이라 타일 다수가 NavMesh 밖에 있었음(플레이어가 걸을 수 없는 상태). `Floor`/`FogGround`를 pos(0,0,16) scale(6.2,1,5.2) (X[-31,31] Z[-10,42])로 재설정 후 `NavMeshSurface.BuildNavMesh()` 재베이크 → 50타일 전부 NavMesh 위에 있음 확인.
2. **`WallSlots`/`SlotPedestals` 전무(발견한 버그, 있으면 벽 배치 자체가 불가능한 상태였음)**: 100개(50타일×2) 코드로 생성. 코너 4곳(타일8·18·22·43)은 `docs/level-design.md`의 "코너에서 슬롯이 길 위에 겹친다" 규칙(진입/진출 방향이 다른 쪽 슬롯만 진출 방향 오프셋으로 교체)을 좌표 공식으로 일반화해 자동 적용 — 4곳 전부 겹침 없이 배치 확인.
3. `ArrivalPortal` 마지막 타일(49, (-22,0,36))에 배치.
4. `GroundBumps`(코어, Floor 범위와 동일)·`GroundBumps_Extra`(카메라 시야 여유 X[-46,44]Z[-9,53]까지 커버, 코어 영역 제외한 도넛형) 절차 생성 — 타일 반경 2.2·몬스터 반경 2.4는 평탄화.
5. `Environment` 데코 55개 — 스테이지1·2와 다른 테마(바위·크리스탈 위주, 나무 비중 줄임)로 자유 배치. 타일/몬스터/포탈과 최소 거리 확보(겹침 0건 확인).
6. **스킬 개수 공식 적용**(`SkillManager.skills`): 가속=4(낙석3+1), 정지=2, 조명탄=2(Ceil(50/30)=2), 무적=1(가시1) — `startLocked` 전부 `false`.
7. `TutorialManager.tutorialEnabled=false`, `steps` 비움(튜토리얼 없음).
8. **스테이지 연결**: `Stage2(Tutorial)`의 `StageManager.nextSceneName`을 `"Stage3"`로 채움(→ 클리어 시 자동으로 "GAME CLEAR"+메뉴 대신 일반 "CLEAR"+"다음 스테이지" 분기를 탐, 코드 로직상 별도 텍스트 수정 불필요). Stage3의 `nextSceneName`은 비워둠(새로운 마지막 스테이지).
9. Build Settings에 `Stage3.unity` 추가(4개 씬: MainMenu/Stage1/Stage2/Stage3).
10. **오타 발견+수정**: Stage3의 결과 화면 `StageLabel`이 템플릿에서 복사된 "Stage 2" 그대로였음 → "Stage 3"로 수정. (Stage1은 "Stage 1" 정상 확인.)
11. Stage1·Stage2도 "배경 여백이 느껴진다"는 요청으로 기존 에셋 재사용해 외곽 여백에 데코 추가(Stage1 +15개, Stage2 +18개) — 기존 데코·타일·몬스터와 최소 3유닛 간격 유지.

**Play모드 검증**: 벽 배치(일반 슬롯 + 코너 4곳 전부) 정상 동작, 마지막 타일 도달 시 `HandleClear()`가 "마지막 스테이지" 분기(GAME CLEAR+메뉴로 돌아가기) 정상 진입, `Time.timeScale=0` 정지 확인. 콘솔 에러 없음(진짜 신규 에러 없음 — Unity AI Assistant 관련 구독 에러는 무관한 기존 캐시 로그).

**⚠️ 미검증**: reflection으로 `StageManager.Update()`를 직접 호출하는 방식은 실제 프레임 진행 없이 테스트해서(`Time.frameCount`가 안 늘어남) `ResultPanelUI`의 카운트업 코루틴(체력바·벽 개수·시간)이 실제로 값 갱신하는 건 눈으로 확인 못 함(로직 자체는 Stage1·2와 동일 코드라 문제 없을 것으로 판단). 재도전(`RetryStage`) 버튼도 Stage3에서 직접 눌러서 확인은 안 함 — 다음 세션에 사람이 직접 플레이해서 최종 확인 권장. `CurrencyManager.totalCurrency`는 템플릿 기본값 5를 그대로 둠(사용자가 명시적으로 요청한 공식이 없어서 임의로 안 바꿈 — 필요하면 알려달라고 할 것).

### 클리어/실패 시 게임 즉시 정지 (2026-09-17, 같은 날 후속6, 모든 스테이지 공통)

**요청**: 클리어/실패 후에도 근처 닭들이 계속 공격하는 게 보여서 어색함 — 결과가 나오면 즉시 멈춰달라.

**수정(`StageManager.cs`)**: `HandleClear()`/`HandleFail()` 끝(이벤트 발행 뒤)에 `Time.timeScale = 0f`를 추가해 몬스터 공격·투사체·이동 등 게임 월드 전체를 즉시 정지시킴. 결과 화면(`ResultPanelUI`)의 카운트업·배경 페이드는 전부 `Time.unscaledDeltaTime` 기반이라 영향 없이 정상 재생됨(튜토리얼 일시정지와 같은 원리 재사용).
- 이벤트 발행 **뒤**에 설정한 이유: 그 순간 튜토리얼이 열려있었다면 `TutorialManager`의 강제 종료 처리가 `timeScale`을 1로 되돌리므로, 그보다 나중에 0으로 덮어써야 확실히 멈춘다.
- `Time.timeScale`은 씬이 바뀌어도 유지되는 전역값이라, 정지시킨 채로 다음 씬으로 넘어가면 그 씬이 처음부터 멈춘 채 시작해버림 — `GoToNextStage()`/`GoToMenu()`/`RetryStage()`에 전부 `Time.timeScale = 1f` 복구 추가.

**검증(Play 모드, 실측)**: Stage1·Stage2 양쪽에서 `HandleClear()`/`HandleFail()` 직접 호출 → `Time.timeScale=0` 확인, 결과 화면은 정상적으로 페이드인/애니메이션됨(스크린샷 확인). `RetryStage()` 호출 후 `Time.timeScale=1`로 복구되는 것 확인. 컴파일 에러 없음. 스크립트만 수정, 씬 저장 불필요.

### 조명탄 게이트 정밀화 — "버튼 클릭"이 아니라 "실제 착지"를 사용으로 인정 (2026-09-17, 같은 날 후속5)

**요청**: 조명탄 튜토리얼 게이트가 스킬 버튼을 누르기만 해도(조준 모드 진입만 해도) 넘어가버림 — 실제로 목표 지점에 착지까지 해야 넘어가도록.

**수정(`SkillManager.cs`)**: `OnSkillUsed` 발행 시점을 스킬 종류별로 분리 — 가속/무적/정지는 기존처럼 버튼을 누르는 즉시(효과가 그 순간 바로 발동하므로) 발행하지만, **조명탄만 `LaunchFlare()`의 `onLanded` 콜백(실제 착지 시점)으로 옮김**. 버튼을 눌러 조준 모드에 들어가는 것만으로는 게이트가 안 풀리고, 월드에 실제로 조준해서 착지시켜야 풀림.

**⚠️ 이 수정만으로는 소프트락 발생 — 함께 고침**: 조명탄 게이트가 열린 동안은 `Time.timeScale=0`(튜토리얼 일시정지)인데, `FlareVfx`의 비행·밝힘 애니메이션 타이머가 `Time.deltaTime`(scaled)을 쓰고 있어서 **착지 자체가 영원히 안 일어나는 소프트락**이 됨. `FlareVfx.cs`의 비행/밝힘/정리 대기 코루틴을 전부 `Time.unscaledDeltaTime`/`WaitForSecondsRealtime`로 교체(프로젝트 기존 원칙 "튜토리얼 연출은 unscaledDeltaTime으로 구동"과 일치, 평소(timeScale=1) 동작에는 영향 없음).

**검증(Play 모드, 실측)**: 조명탄 버튼만 누른 직후 `IsOpen=true`(안 풀림) 확인 → `LaunchFlare()`를 직접 호출해 착지까지 진행시킨 뒤 `stop_flare_unlock`이 `firedKeys`에 들어가며 정상 종료, 대기 중이던 다음 단계(낙석)로 자연스럽게 이어지는 것까지 확인. 컴파일 에러 없음. 스크립트만 수정, 씬 저장 불필요.

### 정지/조명탄 게이트 강화 + 조명탄 유도 방식 변경 + 가시함정 시야 버그 수정 (2026-09-17, 같은 날 후속4)

1. **정지 스킬도 실사용 게이트 추가**: `stop_flare_unlock` 단계 page0(정지 안내)에 `requireSkillUse=true, requiredSkillType=Stop` 추가 — 이제 정지를 실제로 써야 조명탄 페이지로 넘어감(조명탄과 동일한 패턴)
2. **조명탄 유도 방식 전면 변경**: 기존엔 `VisionSourceRegistry.Register`로 목표 지점(가시함정)을 실제로 "공개"시켰는데, 이러면 가시함정 오브젝트 본체가 시야 밖인데도 미리 드러나서 "낙석 함정·몬스터처럼 시야 밖에선 안 보여야 한다"는 규칙이 깨짐(사용자 지적). **안개·시야와 완전히 무관하게 항상 위에 그려지는 별도 표식 원(마커)**으로 교체(`TutorialManager.ShowWorldMarker`/`HideWorldMarker`, `AttackRangeIndicator`와 같은 `Custom/RangeIndicatorOverlay` 오버레이 셰이더 재사용 — 벽 슬롯 고스트 미리보기와 같은 성격의 "순수 안내용" 취급). 문구도 "저 앞 가시밭을 비춰서 미리 확인해보세요" → **"표시된 곳을 조명탄으로 밝혀보세요"** 로 변경
3. **⚠️ 별도로 발견한 심각한 기존 버그 — `SpikeTrap`의 Transform 위치가 실제 위험 구역과 무관했음**: `SpikeTrap`은 `tileIndexA`/`tileIndexB`(23/24)로 실제 가시 위치·판정 박스를 전부 월드 좌표로 계산하는데(오브젝트 자신의 Transform과 무관), **오브젝트 자체의 Transform은 에디터에서 배치된 엉뚱한 위치(타일4~5 근처, `(-8,0,3)`)에 그대로 남아있었음**. `FogVisibility`(가시함정도 `[RequireComponent]`로 가지고 있음)는 `transform.position` 기준으로 거리 계산을 하므로, **가시함정의 "보이고 안 보이고" 판정이 실제 위험 구역이 아니라 이 엉뚱한 위치를 기준으로 동작하고 있었음** — 사용자가 지적한 "가시함정이 낙석/몬스터처럼 시야 규칙을 안 따른다"는 문제의 진짜 원인. **수정**: `SpikeTrap` 오브젝트를 실제 위험 구역 중심(타일23·24 중점, `(12,0,17)`)으로 이동(스폰 로직은 전부 절대 좌표 계산이라 스파이크 비주얼·판정 박스 위치엔 영향 없음, `FogVisibility` 판정만 정상화됨)
4. **조명탄 마커 앵커 신규 배치**: `FlareTargetMarker_23_24`(빈 오브젝트, 타일23·24 중점)를 씬에 추가해 `stop_flare_unlock.worldHighlightTarget`으로 연결(SpikeTrap 자체를 가리키게 해도 되지만, "튜토리얼 전용 안내 지점"과 "실제 게임 오브젝트"를 분리해두는 게 이후 유지보수에 더 안전하다고 판단)

**검증(Play 모드, 실측)**: 타일18에서 정지 게이트 → 정지 사용 → 조명탄 게이트(마커가 타일23·24 정중앙에 정확히 뜨는 것 좌표로 확인, 이 시점에 가시함정 본체는 `anySpikeVisible=false`) → 조명탄 사용 → 대기 중이던 낙석 단계가 이어서 열림(정상, pendingStep 큐 동작) → 타일21에서 무적 게이트 확인. 가시함정 시야: 타일18(멀리)·타일23 직후 각각 `IsCurrentlyVisible=false/true`로 정상 전환, 다시 타일18로 물러나면 `false`로 재차 숨는 것까지 확인(스타크래프트식 동적 재은폐 정상). 컴파일 에러 없음, 씬 저장 완료.

### 스킬 게이트 페이지에서 버튼이 여전히 안 눌리던 문제 추가 수정 (2026-09-17, 같은 날 후속3)
**증상**: 타일 정지 버그 수정 후에도 "튜토리얼은 열리는데 그 상태에서 스킬을 사용 못해 진행이 안 됨" 재보고.
**원인**: `SkillManager.RefreshUI()` 호출을 `TutorialManager.ClosePause()`에만 넣어뒀는데, 실제로 `requireSkillUse` 게이트로 진입하는 시점은 **단계가 "해금"되는 시점(page0)이 아니라 그보다 뒤 페이지(예: page1)**라서, 해금 시점의 한 번뿐인 갱신으로는 게이트 페이지 진입 시점의 `CanUse()` 결과(그새 `IsAwaitingSkillUse`가 true로 바뀜)를 못 따라잡고 있었음.
**수정**: `ShowCurrentPage()` 끝에서도 매 페이지 전환마다 `skillManager.RefreshUI()` 호출하도록 추가.
**검증**: Play 모드에서 타일18 진입 → 정지 페이지 → 조명탄 게이트 페이지로 넘어가자마자(추가 조작 없이) `SkillManager.CanUse(Flare)=true` 및 실제 `Button.interactable=true` 확인.

### ⚠️ 치명적 버그 발견·수정 — `PlayerAutoMover`의 타일 도달 판정이 코너에서 영구히 멈추는 문제 (2026-09-17, 같은 날 후속2)

**증상**: "낙석+가속 튜토리얼 이후로는 아무 튜토리얼도 안 열림" — 사용자가 실제 플레이로 재확인했는데도 여전히 재현됨(직전 수정으로는 해결 안 됨).

**진짜 원인(실측으로 확정)**: `PlayerAutoMover.Update()`의 `reachedTileIndex` 갱신 로직이 "다음 타일 정중앙에서 `arrivalThreshold`(0.15유닛, 매우 좁음) 이내"일 때만 증가하는 구조였음. **코너(타일9→10, 조향이 미리 꺾이는 구간)를 도는 동안 이 좁은 반경을 단 한 번이라도 못 맞히면, `reachedTileIndex`가 그 타일에서 영구히 멈춰버림** — 이 while 루프는 오직 "바로 다음 타일"만 검사하므로, 한 번 놓치면 플레이어가 실제로 아무리 멀리 가도 절대 못 따라잡음. 실측으로 확인: 타일7에서 멈춘 채 실제 캐릭터 위치는 타일21까지 진행되는 것을 직접 확인함(Play 모드에서 `PathManager.GetCurrentPlayerTileIndex()` vs 실제 `transform.position` 비교로 검증). 이 때문에 타일18/21 기반 트리거(스테이지2 스킬 해금)뿐 아니라 **진행바 UI도 스테이지1·2 공통으로 코너를 돈 뒤부터 멈춰있었을 가능성이 있음**(같은 값을 사용).
- 참고: 어제(2026-09-17) 오전에 먼저 발견해 고친 "한 프레임에 여러 타일을 동시에 넘으면 중간 타일이 씹히는" 문제(`SetCurrentPlayerTileIndex`를 루프 밖에서 한 번만 부르던 것)는 진짜 원인이 아니었음 — 그 수정도 유효하지만, 이번에 찾은 "영구 정지" 버그가 훨씬 심각하고 실제 증상의 근본 원인이었음

**수정(`PlayerAutoMover.cs`)**: 다음 타일에 정확히 근접 못 해도, **"다음 타일보다 그 다음 타일에 더 가까워졌다"면 이미 지나친 것으로 보고 따라잡는** 캐치업 로직 추가. `arrivalThreshold` 자체(정상 상황에서의 정밀한 타이밍)는 그대로 유지하고, 코너 등에서 어긋났을 때만 보정하는 방식이라 기존 타이밍에 영향 없음.

**검증(Play 모드, 실측)**: 스테이지2를 처음부터 자연 진행(워프 없이 실제 `NavMeshAgent` 이동)시키며 코너 통과 전후 `reachedTileIndex` vs 실제 위치를 비교 — 수정 전엔 타일7에서 멈춘 채 위치만 타일21까지 진행(완전 재현), 수정 후엔 코너를 돈 뒤에도 타일10, 타일18까지 실제 위치와 거의 정확히 맞아떨어지며 계속 따라잡는 것 확인. 컴파일 에러 없음.

**남은 것**: 스테이지1도 같은 스크립트를 공유하므로 이 수정의 영향을 받음(원래도 버그가 있었지만 스테이지1은 `TileReached` 트리거를 안 써서 티가 덜 났을 뿐 — 진행바 등에는 영향 있었을 수 있음). 별도 회귀 테스트는 안 함, 필요하면 다음에 확인.

### 가속 스킬 사용 횟수 2→1로 변경 (2026-09-17, 같은 날 후속2)
`SkillManager.skills[0]`(Accelerate)의 `totalUses`를 2에서 1로 변경(사용자 요청). Stage2 씬만 해당.

### Stage2 스킬 튜토리얼 — 실플레이 버그 수정 + 재도전 전체 초기화 정책 (2026-09-17, 같은 날 후속)

사용자가 실제로 플레이해보고 발견한 문제 3가지 + 정책 변경 1가지를 수정함.

1. **문구 수정**: DPS형 조우 문구에 "이 검은 닭은", 광역형 조우 문구에 "이 빨간 닭은"으로 몬스터 색을 명시(사용자 확인)
2. **트리거 타일 정밀화**: "마지막 코너 진입 직전" 정지/조명탄 해금 트리거가 모호했던 것을 **"17번 타일을 벗어나는 지점"** = `tileIndex=18`로 명확히 지정(19→18로 변경)
3. **버그 — 가속 스킬 "UI만 열리고 활성화 안 됨"**: `SkillBarUI`는 `SkillManager.OnSkillStateChanged` 이벤트가 발행될 때만 다시 그려지는데, 스킬이 튜토리얼 도중 해금(`UnlockSkill`)되면 그 시점엔 `TutorialManager.IsOpen`이 아직 `true`라 `CanUse()`가 계속 false → 버튼이 회색(비활성) 상태로 그려진 채 그대로 남아있었음(튜토리얼이 닫힐 때 다시 그려주는 코드가 없었음). **수정**: `SkillManager.RefreshUI()`(신규, `OnSkillStateChanged`를 그냥 다시 발행) 추가 + `TutorialManager.ClosePause()`에서 팝업이 닫힐 때마다 호출
4. **버그 — 낙석 이후 튜토리얼이 전혀 안 열림**: `TileReached` 트리거(`HandleTileIndexChanged`)가 `MonsterSeen`과 달리 "이미 다른 튜토리얼이 열려있는지" 확인하는 가드가 없었음 — 겹치면 `OpenPause()`가 중복 호출되어 `previousTimeScale`이 0으로 덮어써지고, 나중에 닫힐 때 `timeScale`이 0에 영구히 멈춰버리는 구조적 결함이었음. **수정**: `MonsterSeen`과 동일한 `pendingStep` 대기열 방식 적용
5. **정책 변경 — 재도전 시 전체 초기화(모든 스테이지 공통, 사용자 명시적 요청)**: 기존엔 `FogVisibility.everRevealed`(첫 발견 여부)가 재도전해도 유지되도록 **의도적으로** 설계되어 있었음(스테이지1 튜토리얼이 재도전마다 다시 뜨지 않게 하려는 목적). 이걸 뒤집어서 **재도전하면 그 스테이지에서 진행한 모든 것이 처음부터 다시 재생**되도록 변경:
   - `FogVisibility.ResetVisibility()` — `everRevealed`도 함께 초기화(⚠️ 스테이지1도 영향받음 — 재도전 시 튜토리얼이 이제 다시 뜸. 기존엔 명시적으로 "다시 안 뜨게" 설계돼 있던 부분이라 인지해둘 것)
   - `TutorialManager` — `StageManager.OnRetried` 구독 추가, `firedKeys` 전체 클리어
   - `SkillManager.HandleRetry()` — 기존엔 해금 상태(`effectiveTotal`)를 유지했는데, 이제 `startLocked` 스킬은 재도전 시 다시 0으로 잠김

**검증(Play 모드)**: 깨끗한 세션에서 타일18 도달 → 정지+조명탄 해금(문구 확인) → 조명탄 실사용 → 타일21 도달 → 무적 해금(문구 확인) → 실사용까지 전부 재확인. 가속 스킬은 낙석+광역형 튜토리얼이 연달아(대기열) 닫힌 직후 `CanUse()=true`로 정상 전환되는 것 확인. `RetryStage()` 호출 후 스킬 4개 전부 `Total=0`(재잠김) + `firedKeys` 0개(전부 초기화) 확인.
⚠️ **테스트 중 참고**: `Bash sleep`으로 실시간 경과를 기다리며 테스트하는 도중 "PlayerLoop internal function called recursively" 에디터 내부 오류가 발생하면서 플레이 세션이 오염된 것으로 보이는 현상 발견(플레이어 위치·튜토리얼 진행이 뒤죽박죽됨) — 이후 세션을 완전히 재시작(`stop`→`play`)하니 정상으로 돌아옴. 이 환경에서 Play 모드 테스트 중 `Bash sleep`으로 실시간을 기다리는 방식은 피하고, 위치 워프+직접 상태 조회 방식을 쓸 것

### Stage2 스킬 튜토리얼 시스템 신규 구현 (2026-09-17)

**요청**: 스테이지2 시작 시 스킬 UI 전부 비활성화 → 몬스터 종류(고DPS/광역)·낙석 함정·정지/조명탄·무적 각각을 처음 조우/도달할 때 순차적으로 튜토리얼과 함께 해당 스킬이 하나씩 해금되는 구조. 일부는 "실제로 그 스킬을 써야" 진행되도록(가시함정 앞 조명탄·무적) 강제.

**시스템 확장(기존 구조 최대한 재사용)**:
- `SkillConfig.startLocked`(신규 bool) — 스테이지 시작 시 잠금 여부. `SkillManager`에 `effectiveTotal[]`(노출되는 총 횟수, 잠긴 동안 0) 배열을 추가해 실제 `totalUses`는 그대로 두고 노출만 감춤 — `UnlockSkill(SkillType)` 호출 시 `effectiveTotal`을 진짜 값으로 복원(기존 "횟수 0이면 버튼 자동 숨김" 로직을 그대로 재사용, UI 쪽은 손 안 댐)
- `SkillManager.OnSkillUsed`(신규 이벤트, `SkillType` 전달) — 스킬을 실제로 쓴 순간(가속/무적/정지는 즉시, 조명탄은 조준 시작 시점) 발행
- `TutorialStep`에 `unlockSkillsOnShow`(SkillType[])·`worldHighlightTarget`/`worldHighlightRadius`(신규) 추가 — 단계가 열릴 때 스킬 해금 + `VisionSourceRegistry`로 특정 위치를 안개 너머로 미리 보여줌(가시함정 위치를 조명탄 유도 단계에서 미리 보여주는 용도)
- `TutorialPage`에 `requireSkillUse`/`requiredSkillType`(신규) 추가 — 기존 `requireSlotClick`(강제 슬롯 클릭)과 완전히 같은 패턴으로 "지정 스킬을 실제로 써야만" 다음 페이지로 진행(`TutorialManager`에 `IsAwaitingSkillUse`/`RequiredSkillType` static 추가, `Update()`에서 이 상태면 일반 클릭 진행을 막음)
- `SkillManager.CanUse()` 수정 — 원래 튜토리얼이 열려있으면(`TutorialManager.IsOpen`) 스킬 사용 자체가 전부 막혀있었는데, "지금 이 스킬 사용을 기다리는 중"이면 예외적으로 허용하도록 조건 추가(안 그러면 애초에 게이트를 통과할 방법이 없음)
- ⚠️ **테스트 중 발견한 안전 문제(수정 완료)**: 튜토리얼이 열려 있는 도중(`timeScale=0`) 스테이지가 끝나버리면(클리어/실패) `TutorialManager.IsOpen`이 계속 `true`로 남아 `timeScale`이 0에 멈춘 채 재도전해도 게임이 얼어붙는 문제 발견 — 정상 플레이에서는 일시정지 중엔 피해도 같이 멈추므로 거의 일어나지 않지만(테스트 중 리플렉션으로 상태를 억지로 조작하다 재현됨), 혹시 모를 상황에 대비해 `TutorialManager`가 `StageManager.OnCleared`/`OnFailed`를 구독해서 열려있으면 강제로 닫도록 방어 코드 추가

**Stage2 씬 배선**:
- `SkillManager.skills` 배열을 해금 순서에 맞춰 재배열 + 전부 `startLocked=true`: **가속(2회)→정지(1회)→조명탄(1회)→무적(1회)**(기존 순서는 가속/무적/정지/조명탄이었음 — 키보드 1~4 단축키도 이 새 순서를 따름)
- `TutorialManager.steps`(5단계) 신규 구성:
  1. `dps_type_intro` — `MonsterSeen`(`Monster_DPS_1`) — "이 닭은 공격이 빠르고 강합니다. 방심하면 순식간에 위험해집니다."
  2. `area_type_intro` — `MonsterSeen`(`Monster_Area_1`) — "이 닭은 공격 범위가 아주 넓습니다. 멀리서도 노려볼 수 있으니 주의하세요."
  3. `rockfall_and_accelerate` — `MonsterSeen`(`RockfallTrap_Tile14`의 `FogSensor` 자식 — 필드명은 `targetMonster`지만 함정에도 그대로 재사용 가능함을 확인) — 2페이지("낙석 경고"+"가속 해금"), 가속 해금
  4. `stop_flare_unlock` — `TileReached`(타일19, 2차 코너 직전) — 2페이지(정지 해금 안내 / 조명탄 해금 안내+`requireSkillUse=Flare`), 정지+조명탄 동시 해금, `worldHighlightTarget`=`SpikeTrap_23_24`
  5. `invincible_unlock` — `TileReached`(타일21, 가시함정 2칸 전) — 1페이지(`requireSkillUse=Invincible`), 무적 해금
- ⚠️ **"마지막 코너 진입 직전 몬스터 사거리를 벗어나면"은 타일 근사로 대체**(사용자 확인) — 실제로는 타일19~20 코너를 위협하는 몬스터가 없어서(가장 가까운 DPS_2/3은 타일23 부근), 정확한 사거리 이탈 판정 대신 타일19 도달을 트리거로 씀
- **조명탄/무적 게이트는 강제**(사용자 확인) — 하이라이트만 하고 넘어가는 게 아니라 기존 강제 슬롯 클릭(③단계)과 동일하게 실제 사용해야 진행

**검증(Play 모드, 리플렉션+실측)**: 5단계 전부 실제로 순서대로 발동하는 것을 스크린샷+상태 조회로 확인. 특히 ④조명탄 단계에서 다른 스킬(가속) 사용이 올바르게 막히고 지정된 스킬(조명탄)만 게이트를 통과시키는 것, ⑤무적 단계에서 실제 사용 시 정상적으로 튜토리얼이 닫히는 것 확인. `Monster_DPS_1`/`Area_1` 자연 조우 시 튜토리얼이 스크립트 개입 없이 자연스럽게 발동하는 것도 확인(첫 페이지는 Stage2 시작 직후 자동 이동만으로 재현됨). 컴파일 에러 없음. 씬 저장 완료

**남은 것**: 실제 사람이 처음부터 끝까지 플레이하며 문구 타이밍·난이도·조명탄 목표지점 정밀도 등을 체감 확인할 것(리플렉션 테스트는 로직 검증까지만 함)

**⚠️ UI 재디자인 작업 전체 되돌림(2026-09-17, 같은 날 후속)**: 아래 "UI 재디자인" 작업을 마친 뒤 사용자가 "그냥 ui 작업하기 전으로 되돌려주고, 메인화면도 다시 되돌려줘"라고 요청 → 메인 메뉴(`MainMenu.unity`)·`Stage1(Tutorial)` 결과 화면(`ResultPanel`)·`StageManager.cs`를 전부 작업 전 상태로 원복함(아래 상세 기록은 "이런 시도를 했었다"는 참고용으로 남겨두고, 실제 씬 상태는 원복 완료).
- **완전히 정확하게 복원된 것**: 메인 메뉴 전체(TitleFrame+TitleText+버튼 색상), 결과 화면 카드/배너/아이콘 추가분 제거, `PanelBG` 크기(700x600)·행 폭(580) 원복, 배경 어둡기(클리어 검정0.72/실패 회색0.6, 서로 다른 원래 값으로) 원복, `StageManager.cs`의 아이콘 관련 필드·`ShowResult` 시그니처 원복
- **⚠️ 완전 동일 복원이 아닌 것(원래 값을 기록 안 해뒀던 부분)**: 결과 화면의 `TitleText`(CLEAR/GAME OVER 문구) 폰트 스타일과 `RetryButton`/`MenuButton`의 배경 스프라이트·색상은 정확한 원래 값을 몰라서 무난한 기본값(NEXON Bold 56pt 어두운 갈색 텍스트, 버튼은 Unity 기본 UI 스프라이트+흰색)으로 재구성함 — 혹시 이전과 미묘하게 달라 보이면 알려주실 것
- **다음에 할 일**: 사용자가 메인 메뉴 로고 이미지만 별도로 만들어올 예정 — 그때 가서 **로고 교체만** 진행할 것(카드·버튼·배너 등 나머지 재디자인은 다시 하지 않음). `Assets/UI_Image/`·`Assets/Textures/UI/`에 오늘 만든 에셋들은 남겨둠(나중에 다시 쓸 수도 있어 삭제 안 함)

### (참고용, 원복됨) UI 재디자인 시도 — 메인 메뉴 + 클리어/실패 화면 (2026-09-17)

### UI 재디자인 — 메인 메뉴 + 클리어/실패 화면 (2026-09-17)

- **배경**: 사용자가 다른 AI로 참고 이미지(`메인화면ui.png`, `clear_fail.png`)에서 구성 요소를 추출해 `Assets/UI_Image/`에 8장(jpg, 흰 배경) 추가함 — 타이틀 로고, 주황/갈색 버튼 판자(뒤 2개가 실제 버튼 모양), 빈 팝업 패널 프레임(웃는 여우 포함), 아이콘 6종(발바닥/별/하트/톱니/새로고침/집) 시트, STAGE CLEAR·GAME OVER 배너, 여우 토퍼(웃는/우는) 시트
- **에셋 처리(코드로 직접 처리, Photoshop 없이)**: `execute_code`로 Texture2D를 직접 조작 — 커넥티드 컴포넌트(플러드필)로 흰 배경 위 각 오브젝트의 바운딩 박스를 찾고, 흰색→투명 알파(경계 230~250 사이 선형 램프로 부드럽게)로 변환해 `Assets/Textures/UI/`에 개별 PNG로 저장 후 Sprite로 임포트(`alphaIsTransparency`, `Uncompressed`, 버튼 판자는 `spriteBorder`(35,6,35,6)로 9-slice)
  - ⚠️ **아이콘 좌표 삽질**: 유니티 텍스처 Y축이 뒤집혀 있는 걸 처음에 잘못 계산해서 발바닥/기어/새로고침/집 라벨이 서로 밀려서 잘못 매핑됐었음(예: "UI_Icon_Paw.png"가 실제로는 별 이미지) — 6개 블롭을 개별 크롭해서 하나씩 눈으로 확인 후 정확한 좌표로 재작업함. **이런 유형(그리드 이미지에서 좌표로 여러 개 잘라낼 때) 작업 시 라벨링 후 반드시 하나씩 스크린샷으로 확인할 것**
  - 주황 버튼은 실제 주황 에셋이 없어서(제공된 "메인 주황색 우드 버튼.jpg"는 버튼이 아니라 빈 명패 프레임이었음, 안 씀) 갈색 버튼 판자를 HSV 리컬러(hue=0.07, sat=0.85, value 부스트)로 직접 만듦
  - 클리어 카드와 실패 카드는 원본이 하나(웃는 여우 포함 프레임)뿐이라, 실패용은 상단 임계값(unity Y>700, 픽셀 샘플링으로 여우·잎 장식이 끝나는 지점 확인)을 크롭해 여우 없는 빈 카드(`UI_BlankCard_NoTop`)를 별도로 만들어 재사용
- **메인 메뉴(`MainMenu.unity`)**: 기존 절차적 `TitleFrame`+`TitleText`(TMP)를 제거하고 로고 이미지 하나로 교체(사용자가 이후 "타이틀 로고2"로 재교체 요청, 반영 완료). `StartButton`(주황 버튼+발바닥 아이콘)/`QuitButton`(갈색 버튼+톱니 아이콘)은 배경 스프라이트+9-slice로 교체하고, 아이콘+기존 라벨을 `HorizontalLayoutGroup`으로 묶어 가운데 정렬. 폰트는 기존 `NEXONLv1Gothic-Regular SDF` 그대로 유지(사용자가 통일 여부 확인차 질문, 이미 통일되어 있어 변경 없음)
- **클리어/실패 화면(`Stage1(Tutorial).unity`, `ResultPanelUI`+`StageManager`)**:
  - `PanelBG` 크기를 700x600→**620x820**(카드 원본 비율에 맞춰 세로로 김)으로 변경
  - `ClearGroup`/`FailGroup` 각각에 카드 배경(`CardBG`, preserveAspect)과 상단 배너(`TitleBanner`, STAGE CLEAR/GAME OVER, 카드 위로 겹쳐 띄움) 추가. 기존 동적 텍스트 `TitleText`(TMP, "CLEAR"/"GAME OVER"/"GAME CLEAR" 상황별 텍스트)는 삭제 — `StageManager.resultText` 참조는 null이 되지만 코드에 null 체크가 있어 안전(⚠️ 최종 스테이지 클리어 시 "GAME CLEAR" 메시지는 이제 화면에 안 뜸, STAGE CLEAR 배너로 동일하게 보임 — 현재 Stage1→Stage2 흐름에서는 영향 없음)
  - `HealthRow`/`WallsRow`/`TimeRow`/`FailProgressText` 폭을 580→**460**으로 줄임(카드 폭이 좁아져서 기존 폭이 카드 테두리 밖으로 삐져나왔던 것 수정)
  - `RetryButton`/`MenuButton`도 메인 메뉴와 같은 방식(플랭크 스프라이트+아이콘+`HorizontalLayoutGroup`)으로 재구성. **`RetryButton`은 클리어("다음 스테이지")/실패("다시 도전") 공용이라 아이콘도 상황별로 바뀌어야 해서** `StageManager.cs`에 `retryButtonIcon`(Image 참조)·`retryIconClear`(발바닥)·`retryIconFail`(새로고침) 필드 추가 + `ShowResult()`에 아이콘 파라미터 추가(기본값 null이라 기존 호출부 호환)
  - **배경 어둡기 통일**(사용자가 처음에 지적한 부분): `ResultPanelUI.backdropClearColor`(기존 검정 0.72)와 `backdropFailColor`(기존 회색 0.6)가 서로 달랐던 것을 **둘 다 검정 (0,0,0,0.68)로 통일**
  - **검증**: Play 모드에서 `StageManager.HandleClear()`/`HandleFail()`을 리플렉션으로 직접 호출해 두 화면 모두 스크린샷 확인 — 카드 안에 내용이 잘 들어맞고, 버튼 아이콘·라벨이 상황에 맞게 바뀌는 것 확인. 튜토리얼 팝업이 겹쳐 보이는 현상이 있었으나 이건 리플렉션으로 튜토리얼 dismiss 없이 강제로 상태를 바꿔서 생긴 테스트 환경 아티팩트(실제 플레이 흐름에서는 튜토리얼이 이미 닫힌 뒤 클리어/실패에 도달하므로 무관)
- **남은 작업**: `Stage2(Tutorial).unity`에도 동일하게 적용 필요(미착수). `Assets/Textures/UI/_debug/`에 좌표 확인용 임시 크롭 파일들이 남아있음(삭제는 임의로 안 함, 필요시 사용자 요청으로 정리)

### "닭 공격범위 표시가 닭이 없는 길까지 뒤덮는다" 버그 — 원인 파악·수정 완료(2026-09-17)

- **증상**: "닭 공격범위가 표시될때 모든 공격범위 레이어가 동시에 표시되는 버그가 있는거같은데, 닭이 없는 길 주변에도 공격범위처럼 빨간색 장판이 생겨" → 처음엔 `Monster_C`/`Monster_D`(3.96유닛 간격, 둘 다 사거리6) 배치 겹침으로 오판했다가, 사용자가 "닭 하나만 있어도 그렇다"고 재지적해 재조사함
- **진짜 원인(사용자가 인스펙터에서 직접 발견)**: `AttackRangeIndicator`의 채움 원(`RangeIndicator_Fill`, Quad)이 몬스터 루트에 로컬 스케일로 붙어있는데, 2026-09-16에 닭 프리팹 스케일을 2→4로 키운 여파로 **부모(몬스터) 스케일이 그대로 곱해져 실제 사거리보다 4배 큰 크기**(사거리6 기준 의도한 지름12 → 실제 48)로 렌더링되고 있었음. 링(`RangeIndicator_Core`/바깥 링, `LineRenderer.useWorldSpace=true`)은 월드 좌표를 직접 계산해서 그리기 때문에 스케일 영향이 없어 정상이었고, 채움 원만 어긋나 있었음 — 그래서 닭 한 마리만 있어도 실제 사거리(6~10유닛)보다 훨씬 넓은 범위가 덮인 것처럼 보였던 것
- **수정(`AttackRangeIndicator.cs`, `BuildFill()`)**: `SkillVfxController.CompensateScale()`과 동일한 패턴 적용 — 채움 Quad를 몬스터에 바로 붙이지 않고, 부모 스케일의 역수를 가진 보정용 빈 오브젝트(`RangeIndicator_FillScale`)를 하나 끼워서 최종 월드 크기가 항상 사거리와 정확히 일치하도록 함
- **검증(Play 모드, 실측+스크린샷)**: 수정 전 `RangeIndicator_Fill`의 `lossyScale`이 (48,48,4)(링은 정상적으로 사거리와 일치하는 (4,4,4)×원래 지름)였던 것을, 수정 후 (12,12,1)로 확인(사거리6 기준 지름12와 정확히 일치, 링과 동일). `Monster_A` 단독 발견 상황 스크린샷으로도 채움 원이 링 경계 안에 딱 맞게 그려지고, 빈 들판까지 덮던 현상이 사라진 것 확인. 컴파일 에러 없음(무관한 `NoSubscription`만 존재)
- **적용 범위**: 스크립트 레벨 수정이라 씬/프리팹을 건드릴 필요 없이 `Chicken`/`Monster_AreaType`/`Monster_DPSType`을 쓰는 모든 씬(`Stage1(Tutorial)`/`Stage2`/`Stage_Template`)에 자동 적용됨 — 별도 씬 저장 불필요
- **남은 할 일**: 사용자가 실제 플레이로 스테이지2 등에서도 정상으로 보이는지 확인해줄 것

**추가 요청 — 사거리 표시가 지형 굴곡에 가려지는 문제 수정(2026-09-17, 같은 날 후속)**

- **요청**: "core, fillscale 두 개를 지형보다 레이어를 올려서 항상 보이게 할 수는 없어? 캐릭터/몬스터보다는 낮아서 그 밑에 깔리고, 지금 지형에 좀 가려져서 보이는게 어색해"
- **원인 확인**: `RangeIndicator_Core`(안쪽 밝은 링)는 `Custom/RangeIndicatorOverlay` 셰이더가 `ZTest Always`로 하드코딩돼 있어 원래도 지형에 안 가려짐(확인 완료). 실제로 가려지는 건 `RangeIndicator_Fill`(채움)뿐 — 몬스터에게는 가려지도록 일반 ZTest를 쓰는데, 이 때문에 `GroundBumps`/`GroundBumps_Extra` 지형의 시각적 굴곡(메시 높이 최대 약 0.51유닛, 콜라이더 없는 순수 비주얼 굴곡이라 Raycast로는 안 잡히고 메시 바운즈로 확인)에도 똑같이 가려지고 있었음
- **수정(`AttackRangeIndicator.cs`)**: `heightOffset` 필드를 0.05→**0.6**(지형 굴곡 최대치 0.51보다 여유 있게 높음, 몬스터/캐릭터 몸통 높이(1.5~2유닛대)보다는 한참 낮음)으로 올리고, 채움 원의 높이 계산도 기존 `heightOffset * 0.5f`(링의 절반 높이)에서 `heightOffset`(링과 동일 높이)로 변경 — 다르게 유지할 이유가 없었고, 절반 높이로는 지형 굴곡을 못 넘음. 세 프리팹(`Chicken`/`Monster_AreaType`/`Monster_DPSType`) 전부 이 필드가 스크립트 기본값이 아니라 개별 저장된 값이라 `heightOffset`을 직접 0.6으로 갱신해 저장함
- **검증**: Play 모드에서 `RangeIndicator_Fill`의 world Y가 0.6으로 반영된 것 확인, `Monster_A` 단독 발견 스크린샷으로도 채움 원이 정상적으로 보이는 것 확인. 컴파일 에러 없음(무관한 `NoSubscription`만 존재)
- **참고**: 채움은 여전히 일반 ZTest라 몬스터/캐릭터에게는 그대로 가려짐(요청대로 유지) — 이번 수정은 지형에 대해서만 안 가려지게 한 것

**⚠️ 우선순위 배경**: 2026-09-15에 사용자가 `docs/feature-expansion.md`(스킬 4종 / 함정 2종 / 빙결 지대 확장, 마감 2026-09-17 목요일)를 추가하고 이 작업을 최우선으로 진행하자고 함. **U6·P4~P6은 이 확장 작업이 끝날 때까지 보류.** design.md 0~10단계(2026-09-10 완료)·polish.md P1~P3(2026-09-11 완료)·UI U1~U5(2026-09-14 완료)·`UIHighlightEffect` 재설계(2026-09-15 완료)는 그대로 유지된 기반이다. E3(함정)까지 기능 구현 완료(2026-09-16) — 이후 사용자가 직접 스테이지 배치 작업 중이며, 오늘은 배경/안개 관련 버그 수정 위주였음(아래 날짜별 기록 참고).

**오늘(2026-09-16) 완료 요약** — 전부 세 씬(`Stage1(Test)`/`Stage2`/`Stage_Template`)에 저장 완료, 컴파일 에러 없음:
1. 스테이지 타일 빠르게 잇기 에디터 도구(`PathManagerEditor.cs`) 신규 제작 + 비주얼 생성·선택 유지 후속 수정
2. 낙석 함정 경고 원 렌더 순서 수정(캐릭터를 안 가리게) → 이후 "너무 안 보인다" 재지적으로 `HardCircle`+`Ring` 조합으로 재보강(최종 확인 완료, "지금 딱 좋아")
3. 스테이지1·2 배경 확장 — 1차로 기존 바닥을 스케일로 늘렸다가 안개 왜곡 버그로 전면 되돌리고, "기존 바닥은 안 건드리고 주변부만 새로 이어붙이는" 방식으로 재작업
4. 안개 시스템 전면 재설계 — 텍스처 영구 채색 방식 → 셰이더 기반 완전 동적 방식(`Custom/FogOfWarShroud`)으로 교체, 몬스터·함정도 시야 벗어나면 다시 숨도록 변경
5. 시야범위를 스테이지1 기준(7)으로 전 씬 통일
6. ⚠️ 미해결: `Stage_Template.unity`에 원인 불명 빈 `Plane` 오브젝트 41개(삭제 시도가 안전장치에 막힘, 사용자 확인 대기 중)

### 스테이지 타일 배치 도구 + 낙석 함정 렌더 순서 + 배경 확장 3종 (2026-09-16)

- **스테이지 타일 빠르게 잇기 도구(`Assets/Scripts/Editor/PathManagerEditor.cs`, 신규)**: `PathManager` 커스텀 인스펙터에 방향 버튼(↑←→↓) 4개 + "마지막 타일 되돌리기"를 추가 — 항상 마지막 타일 기준 2유닛 격자로만 다음 타일을 추가해 간격 어긋남이 구조적으로 불가능. 이후 사용자 피드백 2건 반영: ① "타일이랑은 깔리는데 비주얼적으로 안깔려서 확인하기가 힘들어" → `AddTile`이 로직용 `Tile_XX` 마커만 만들던 것을, 최종 결과물과 같은 방식(`PathTilesVisual` 아래 `Plane`, Y 0.02, `M_PathTile`)으로 길 바닥 비주얼도 같이 생성하도록 수정. ② "타일을 추가해도 계속 Path를 클릭한 상태로 유지되게" → 새로 만든 타일로 선택(`Selection.activeGameObject`)을 옮기던 코드를 제거해 버튼 연타로 빠르게 이어붙일 수 있게 함. 사용자 확인("좋아 잘 된다") 완료
- **낙석 함정 경고 원이 캐릭터를 가리는 문제 수정(`RockfallTrap.cs`)**: 기존엔 `AttackRangeIndicator`의 "테두리 링" 연출과 같은 `Custom/RangeIndicatorOverlay`(ZTest Always) 셰이더를 꽉 찬 원(Cylinder)에 그대로 재사용해서, 깊이 검사를 아예 안 하는 바람에 캐릭터보다 항상 위에 그려졌음. `AttackRangeIndicator`의 "채워지는 원" 부분이 실제로는 이 오버레이 셰이더가 아니라 `SkillVfxLibrary.CreateMaterial`(일반 ZTest 반투명, `SoftCircle` 텍스처) + `CreateFlatQuad`를 쓰고 있는 걸 확인하고 그 방식 그대로 재사용(Cylinder→Quad, `ownedVfx` 리스트로 `OnDestroy`에서 정리). Play 모드에서 여우를 함정 위로 텔레포트시키고 경고 단계를 리플렉션으로 강제해 스크린샷으로 검증 — 경고 원이 캐릭터 뒤로 자연스럽게 가려지는 것 확인(지형에 가려질 걱정은 없음: 함정은 항상 경로 타일 위에 있고 그 주변은 `GroundBumps` 평탄화 반경(2.2유닛)이라 굴곡이 없음)
- **스테이지1·스테이지2 배경 확장 — 1차 시도(`Floor`/`FogGround` 스케일 확대) → 사용자 지적으로 전면 되돌리고 "추가" 방식으로 재작업**:
  - ⚠️ **문서 오류 발견(그대로 유효)**: 하단 "확정 수치"엔 카메라(`CM_TopDown`) 회전이 "완전 탑다운(90,0,0)"이라고 적혀있었는데, 세 씬(`Stage1(Test)`/`Stage2`/`Stage_Template`) 전부 실제 값은 **(55,0,0)**이었음(Play 모드에서 `cam.transform.eulerAngles`로 직접 확인). 아래 "확정 수치" 항목을 실측값으로 정정함
  - **화면 범위 실측 방법(그대로 유효)**: Play 모드에서 `Camera.main.ViewportPointToRay`로 화면 네 모서리를 Y=0 평면과 교차시켜 실제로 화면에 보이는 바닥 사각형을 구함 → 결과: 여우(카메라 추적 대상) 기준 **X ±23.89유닛, Z 뒤로 -8.69/앞으로 +16.32유닛**(카메라가 항상 여우보다 뒤에서 앞을 내려보므로 앞쪽이 더 넓게 보임)
  - **1차 시도(폐기)**: `Floor`/`FogGround`를 각각 X[-34,38] 등으로 스케일 자체를 확대하고, `GroundBumps`도 그 범위 전체를 다시 절차 생성하는 방식으로 진행함
  - ⚠️ **사용자 지적**: "배경을 늘리는 방식으로 하지말고 그냥 추가해줘, 캐릭터의 시야반경 표현이랑 몬스터들 공격 범위 표현이랑 전부 이상해져있어"
  - **원인 진단**: `FogOfWarGround.cs`가 월드 좌표→안개 텍스처 UV 변환에 **자기 자신의 `worldSize`/`worldCenter` 필드**(Transform 스케일과 별개인 직렬화 값)를 쓰는데, `Floor`/`FogGround`의 Transform 스케일만 바꾸고 이 두 필드는 갱신하지 않았음 — 그 결과 실제 평면 크기와 안개가 "원이라고 믿고 계산하는" 크기가 어긋나면서 시야가 걷히는 범위가 원이 아니라 타원으로 찌그러져 보였음(공격 사거리 표시 자체(`AttackRangeIndicator`)는 안개와 무관한 독립 오브젝트라 왜곡되지 않지만, 찌그러진 안개 경계와 뒤섞여 보이면서 "다 이상하다"는 인상을 준 것으로 추정). **추가로 확인**: 이 `worldSize`/`worldCenter` 불일치는 사실 오늘 작업 이전부터 이미 있었음 — 두 씬 모두 값이 `(30,60)`/`(0,0)`으로 저장돼 있었는데, 이는 **아주 처음(확장 이력이 전혀 없던 시절)** 크기와만 일치하고, 그 이후 한 번(스테이지1 "동쪽 확장") 있었던 이전 세션 스케일 변경 때도 이 필드를 안 맞춰서 이미 미세하게 어긋나 있었던 것(오늘 변경 폭이 훨씬 커서 눈에 띄게 드러난 것뿐)
  - **되돌리기**: 두 스테이지 모두 `Floor`/`FogGround`를 `worldSize`/`worldCenter`와 정확히 일치하는 **가장 처음 크기**(scale (3,1,6), pos (0,0,0), X[-15,15] Z[-30,30])로 원복. 오늘 추가했던 데코(스테이지2 40개/스테이지1 42개, `Environment`의 뒤쪽 자식들)도 전부 제거. `GroundBumps`도 이 범위에 맞춰 다시 절차 생성(스트레치 없이 깨끗하게)
  - **재작업 방식(사용자 요청대로 "추가")**: 기존 `Floor`/`FogGround`(=안개·시야가 실제로 동작하는 진짜 플레이 영역)는 **Transform을 다시는 건드리지 않고 그대로 둠**. 대신 그 바깥 카메라 화면 범위만큼을 covering하는 **별도의 새 지형 오브젝트(`GroundBumps_Extra`, 신규 메시 에셋 `M_GroundBumps_Stage1/2_Extra.asset`)를 새로 만들어서 옆에 이어 붙임** — 기존 `GroundBumps`와 같은 절차 생성 로직(2유닛 격자·Perlin 노이즈·경로/몬스터 평탄화)을 원래 바닥 범위 바깥쪽 격자 칸에만 적용해 만듦(원래 영역과 정확히 이어지고 겹치지 않음). 이 주변부는 안개·NavMesh와 무관한 순수 배경이라 `FogOfWarGround`의 `worldSize` 같은 걸 아예 새로 계산할 필요가 없음 — 이번 버그의 근본 원인 자체가 구조적으로 재발 불가능해짐
  - **데코**: 새로 만든 주변부 영역에만(기존 코어 영역 제외) 스테이지2는 40개, 스테이지1은 42개를 기존과 같은 거리 기반 회피 배치로 추가
  - **검증**: 두 스테이지 모두 Play 모드에서 (1) 안개를 켠 채로 시야가 걷히는 모양이 원래대로 정원(正圓)인지 스크린샷 확인(왜곡 사라짐 확인) (2) `FogGround`를 잠깐 꺼서 진짜 빈 공간(void)이 없는지 경로 시작/끝 지점에서 스크린샷 확인 — 둘 다 정상. `NavMeshSurface.BuildNavMesh()` 재빌드(주변부엔 Collider가 없어 자동으로 NavMesh 대상에서 제외됨, 기존 코어 영역만 그대로 걸어다닐 수 있음, 의도한 대로). 두 씬 모두 저장 완료
  - **스테이지 템플릿(`Stage_Template.unity`)**: 같은 방식으로 되돌림 — `Floor`/`FogGround`를 처음 크기(scale (3,1,6), pos (0,0,0))로 원복하고, 주변부는 `GroundBumps_Extra`(`M_GroundBumps_Template_Extra.asset`)만 추가(데코는 일부러 안 채워넣음 — 스테이지마다 배경을 다르게 하라는 기존 방침([[feedback_vary_stage_backgrounds]]) 때문에, 템플릿 단계에서 고정 데코를 깔아두면 모든 새 스테이지가 같은 데코로 시작하게 돼버림). `NavMeshSurface.useGeometry`를 `PhysicsColliders`로 고친 것(예전부터 있던 별개 버그)은 이번 되돌리기와 무관해 그대로 유지
  - ⚠️ **미해결 — 사용자 확인 필요**: `Stage_Template.unity` 씬 루트에 이름 없는 `Plane` 오브젝트 41개가 원점(0,0,0)에 아무 것과도 연결 안 된 채 남아있음(이전 세션 타일 도구 디버깅 잔재로 추정). 삭제 시도가 안전장치(Irreversible Local Destruction)에 막혀서 그대로 두었음 — 확인 후 정리 여부 알려주시면 처리
  - **안개(FogOfWarGround) 관련 참고**: 검증 중 `FogGround`를 통째로 꺼서 화면이 온통 초록색으로 보인 걸 사용자가 "시야범위가 늘어나 보인다"고 지적 — 실제 `visionRadius` 수치는 안 건드렸고 검증 목적으로 안개 오브젝트를 잠깐 꺼둔 것뿐이었음(바로 원복). 사용자가 "시야범위"라는 말에 실제 수치뿐 아니라 안개가 걷히는 비쥬얼적 범위도 포함해서 쓴다고 확인해줘서 메모리에 기록해둠 — 이후 실제로 이 안개 쪽에서 진짜 버그(위 `worldSize` 불일치)가 나온 걸 보면 이 구분을 명확히 해둔 게 도움이 됐음

### 안개 시스템 전면 재설계 — 스타크래프트식 셰이더 안개 + 동적 재은폐 + 주변부까지 확장 + 시야 1 축소 (2026-09-16)

- **요청 3가지**: ① 오늘 새로 추가한 주변부 배경(`GroundBumps_Extra`)에도 안개를 깔 것 ② 시야범위·안개 걷히는 비쥬얼 범위 둘 다 1만큼 축소 ③ 스타크래프트 안개를 레퍼런스로, 지금보다 어둡지만 반투명해서 배경/길은 은은히 보이고 몬스터·함정 같은 주요 오브젝트는 시야범위 안에 들어와야만 보이도록 개선
- **`FogOfWarGround.cs` 전면 재작성**: 기존엔 512×N 텍스처에 원을 `SetPixels32`로 직접 칠해서 한 번 걷힌 영역은 영구히 투명 유지하는 방식(`worldSize`/`worldCenter`로 월드→UV 변환)이었음. 이걸 완전히 버리고 새 커스텀 셰이더(`Custom/FogOfWarShroud`, `Assets/Shader/FogOfWarShroud.shader`)로 교체 — 프래그먼트 셰이더가 정점의 **월드 좌표를 직접** 받아서 플레이어(+`VisionSourceRegistry`의 조명탄 등 소스, 최대 8개)까지의 거리를 매 프레임 계산해 원 안쪽은 완전히 걷고 바깥쪽은 `fogColor`로 덮는다(경계는 `edgeSoftness`만큼 부드럽게 번짐, `MaterialPropertyBlock`으로 매 프레임 갱신). 텍스처/UV/월드 크기 개념 자체가 없어져서 **바로 위에서 겪은 `worldSize` 불일치 버그 계열이 구조적으로 다시는 발생할 수 없음** — 부수효과로 `FogGround`를 Floor 크기와 무관하게 자유롭게 키울 수 있게 돼서 ①이 자연스럽게 해결됨(오늘 추가한 주변부까지 포함해서 카메라 화면 전체를 덮도록 세 씬 모두 `FogGround` 재배치: 스테이지2 pos(2,0.6,5)/scale(7.2,1,7), 스테이지1 pos(2,0.6,0)/scale(7.2,1,6), 템플릿 pos(0,0.6,5)/scale(7.2,1,7))
- **"영구 공개" 폐지 → 상태 저장 없는 완전 동적 방식**: 매 프레임 현재 위치로 다시 계산할 뿐이라 `ResetFog()`가 사실상 할 일이 없어져 빈 메서드로 남김(`StageManager`와의 호환용)
- **`FogVisibility.cs` 수정(③, 몬스터·함정이 시야범위 안에 있을 때만 보이게)**: 기존엔 `revealed` bool이 한 번 true가 되면 절대 안 돌아가는 구조였음. `LateUpdate`에서 매 프레임 `distance <= visionRadius` 여부를 다시 판정해서 렌더러 `.enabled`를 그때그때 켜고 끄도록 변경 — 시야를 벗어나면 다시 숨는다. 다만 튜토리얼의 "처음 발견" 트리거(`OnRevealed` 이벤트)는 최초 1회만 발행하는 기존 동작을 그대로 유지해야 해서(재도전마다 튜토리얼이 다시 뜨면 안 됨) `everRevealed`(1회성)와 새 `IsCurrentlyVisible` 프로퍼티(매 프레임 갱신)를 분리해 둘 다 만족하게 함
- **`RockfallTrap.cs` 연쇄 수정**: 경고 원/바위 표시가 `FogVisibility.OnRevealed`(1회성 이벤트)로 한 번 캐시한 `revealedByFog` bool을 계속 쓰고 있었는데, 이러면 한 번 봤던 함정은 플레이어가 멀어져도 영구히 계속 보여버려서 ③ 요구사항과 어긋남. `revealedByFog` 필드·이벤트 구독을 전부 제거하고 `Update()`에서 `fogVisibility.IsCurrentlyVisible`을 매 프레임 직접 읽도록 교체(`ResetTrap()`의 관련 초기화 코드도 정리). `SpikeTrap.cs`는 원래부터 자기 소유 `FogVisibility`가 가시 렌더러를 직접 켜고 끄는 구조라 `FogVisibility.cs` 자체 수정만으로 자동으로 동적 재은폐가 적용됨(추가 수정 불필요)
- **시야범위 1 축소(②)**: 스테이지1은 전부 8→**7**로 일괄 축소(원래도 전부 8로 일치돼 있었음). 스테이지2는 축소 전에 **기존 불일치를 먼저 정정**해야 했음 — 실제로 열어보니 `FogOfWarGround`/`AttackRangeIndicator`/몬스터·함정의 `FogVisibility`는 8(디폴트값 그대로, 스테이지2용으로 한 번도 맞춰진 적 없었음), 데코의 `FogVisibility`만 5(문서에 적힌 "스테이지2 visionRadius=5"와 일치)로 서로 어긋나 있었음 → 전부 5로 통일한 뒤 1을 줄여 **4**로 확정(`FogVisibility` 33개, `AttackRangeIndicator` 5개, `FogOfWarGround` 1개 전부 일괄 반영). 이 불일치는 오늘 작업과 무관하게 예전부터 있던 것으로 보임(같이 발견해서 정리)
- **안개 색/밝기**: `fogColor` 기본값을 어둡지만 반투명하게(RGBA(0.05,0.05,0.08,0.6)) 잡음 — 처음엔 0.72로 했다가 스크린샷으로 보니 거의 다 가려 보여서 0.6으로 낮춤(배경/길이 또렷하게 비쳐 보이는 것 확인). `Assets/Materials/M_FogOfWarShroud.mat`(신규) 하나를 세 씬이 공유
- **검증(Play 모드, 스크린샷+실측)**: (1) 시야 경계가 부드러운 정원으로 보이고 경계 밖 배경·길이 은은하게 비쳐 보임 확인 (2) 몬스터 근처로 텔레포트 → 몬스터 표시+HP 감소(공격 확인) → 멀리 텔레포트 → 몬스터가 다시 안 보이는 것까지 스크린샷으로 확인(동적 재은폐 정상 동작) (3) `RockfallTrap`도 같은 방식으로 경고 원이 시야 안/밖에서 나타났다 사라지는 것 확인, 캐릭터를 가리지 않는 것도 그대로 유지됨. 컴파일 에러 없음. 세 씬(`Stage1(Test)`/`Stage2`/`Stage_Template`) 모두 저장 완료
- **시야범위 재통일**: 검증 직후 사용자가 "시야범위는 전부 다 통일시킬거고 스테이지1 기준으로 전부 똑같이 맞춰줘"라고 해서 스테이지2를 4→**7**로 다시 올림(스테이지1/`Stage_Template`은 이미 7). 세 씬 전부 `FogOfWarGround`·`FogVisibility`·`AttackRangeIndicator`가 7로 완전히 동일
- **낙석 경고 원이 너무 안 보이는 문제 추가 수정(`RockfallTrap.cs`, `SkillVfxLibrary.cs`)**: 검증 스크린샷으로는 괜찮아 보였는데, 사용자가 실제로 보니 "너무 안 보인다"고 지적. 원인은 채움에 쓰던 `SoftCircle` 텍스처가 중심부터 이미 흐려지는 완만한 그라데이션이라(가장자리 근처는 알파가 0에 가까움) 경고용으로 쓰기엔 존재감이 약했고, 특히 새로 바뀐 안개의 밝은 시야 원(같은 자리에 겹침)과 대비가 잘 안 됐음. `SkillVfxLibrary.cs`에 `HardCircle`(바깥 15%만 페이드, 나머지는 꽉 찬 알파) 텍스처를 추가해 채움에 쓰고, 알파도 0.55→0.75로 올림(`warningFillColor`, 필드명도 명확하게 변경). 추가로 경계를 확실히 보여주는 밝은 `Ring` 텍스처 고리(`warningRingColor`, RGBA(1,0.35,0.05,0.95))를 채움 위에 겹쳐서 "위험 범위 경계"가 한눈에 보이게 함(둘 다 같은 `SetWarningScale`로 같이 커짐, 같은 `IsCurrentlyVisible`+phase 조건으로 같이 켜짐/꺼짐). 캐릭터를 가리지 않는 기존 동작(일반 ZTest, `SkillVfxLibrary.CreateMaterial`)은 그대로 유지. Play 모드 스크린샷으로 뚜렷한 주황색 원+고리가 보이는 것, 캐릭터를 여전히 안 가리는 것 확인. 스크립트/텍스처만 바뀐 거라 씬 저장은 불필요(`isDirty=false` 확인)

### ⚠️ 긴급 수정 — 스테이지1 벽 세우기 튜토리얼(강제 슬롯 클릭)이 멈추는 회귀 버그 (2026-09-16)

- **증상**: 스테이지1에서 `first_hit` 튜토리얼 3페이지(타일3 Left 강제 슬롯 클릭)가 뜬 뒤, 슬롯을 클릭해도 벽이 안 세워지고 튜토리얼이 진행되지 않음
- **원인**: E1(스킬 시스템, 2026-09-15)에서 `WallPlacementManager.Update()`에 `EventSystem.current.IsPointerOverGameObject()` 가드를 새로 추가함(스킬 버튼 클릭이 벽 배치로 새지 않게 하려는 목적). 그런데 튜토리얼 오버레이(`TutorialPopupPanel`)는 화면 전체를 덮는 `Image`(anchor 0~1, `raycastTarget=true`)라서, 강제 슬롯 클릭 페이지에서 알파값만 0.12로 낮추고(2026-09-14 수정) 오버레이 자체는 계속 활성 상태로 남아있음 → `IsPointerOverGameObject()`가 "화면 전체가 UI 위"라고 판단해 **슬롯을 포함한 모든 클릭을 무조건 막아버림**. 클릭이 이 가드에서 조용히 걸러져서(이 분기엔 `Debug.Log`도 없음) 콘솔에 아무 단서도 안 남는 게 증상을 더 헷갈리게 만들었음
  - 이건 2026-09-10에 이미 한 번 고쳤던 "오버레이가 슬롯 클릭을 막는다" 계열 버그(당시 원인: 오버레이가 3D 월드를 가려서 안 보였음)와 증상은 비슷하지만 **원인은 다름** — 그때는 `WallPlacementManager`가 `EventSystem`을 거치지 않는 순수 `Physics.Raycast`라 오버레이와 원래 무관했는데(아카이브에 이 점이 "장점"으로 명시돼 있었음), 어제 E1이 그 가드를 새로 얹으면서 과거엔 없던 경로로 재발한 것
- **수정(`TutorialManager.cs` `Awake()`)**: 오버레이 `Image`의 `raycastTarget`을 `false`로 고정 — 이 오버레이는 순수 시각용 배경일 뿐, 클릭 차단은 이미 `WallPlacementManager.InputLocked`/`IsAwaitingSlotClick`으로 전부 제어되고 있어서 `raycastTarget`이 애초에 필요 없었음. 스크립트 한 곳만 고치면 되고 씬/프리팹은 건드리지 않음(스테이지2도 같은 컴포넌트를 쓰므로 동일하게 적용됨)
- **검증(Play 모드, 리플렉션)**: `stage_start` 팝업 닫기 → `first_hit` 발동 → 슬롯 클릭 요구 페이지(gate=타일3 Left) 진입까지 재현 → 이 상태에서 `overlay.raycastTarget=False`, `EventSystem.current.IsPointerOverGameObject()=False` 확인(수정 전이라면 여기서 `True`가 나와 막혔을 부분) → 실제 카메라 화면좌표로 `Physics.Raycast`를 쏴서 `Slot_03_L`이 정확히 잡히는 것 확인 → `TryPlaceWall`을 그 슬롯으로 호출 → `WallsPlaced=1`로 배치 성공 + 튜토리얼이 정상적으로 닫히는 것(`IsOpen=False`)까지 전부 확인. 컴파일 에러 없음(무관한 `NoSubscription`만 존재). 씬은 수정하지 않아 `isDirty=false` 그대로
- **참고**: 이 가드(`IsPointerOverGameObject`)가 앞으로 스킬 버튼 외의 다른 전체 화면 UI(결과 화면 오버레이 등)에도 있다면 같은 패턴으로 막힐 수 있음 — 새 UI를 추가할 때 화면 전체를 덮는 배경 `Image`는 클릭을 막을 필요가 없다면 기본적으로 `raycastTarget=false`로 만들 것

### 여우 이동이 타일마다 뚝뚝 끊기는 문제 수정 (2026-09-16)

- **참고(환경 이슈)**: 이 환경(MCP만 붙은 헤드리스 상태)은 Play 모드에서 `Time.time`이 아예 멈춰버리는 문제가 있었음(과거 세션에도 기록됨) — 원인은 `Application.runInBackground=false`(기본값)라 에디터가 포커스를 못 받으면 플레이어 루프 자체가 안 돌아감. `Application.runInBackground=true`로 임시 설정(Play 모드 종료로 자동 원복, 프로젝트에 영구 반영 안 함)하면 정상적으로 시간이 흐름 — 앞으로 이 프로젝트에서 실측 속도/움직임 검증이 필요하면 이 방법을 재사용할 것
- **1차 진단(실측)**: `PlayerAutoMover`가 타일 하나하나를 `NavMeshAgent.SetDestination()`으로 순서대로 넘기는데, `agent.speed(4)`/`acceleration(8)` 조합의 제동거리(`v²/2a`=1.0유닛)가 타일 간격(2유닛)의 절반이나 됨. `autoBraking`이 켜진 채라 NavMeshAgent가 각 타일을 "최종 목적지"로 착각해 도착 훨씬 전부터(remain≈0.9~1.2부터) 스스로 감속하고, 그 뒤 `arrivalThreshold(0.15)`까지 거의 다 도착해야 다음 타일을 지정하니 **타일마다 감속→재가속이 반복**됨(로그로 실측: 최고 4.0 → 바닥 1.6~2.0대까지 떨어졌다 재상승을 매 타일 반복)
- **1차 수정**: `PlayerAutoMover.Awake()`에서 `agent.autoBraking = false` 설정. 직선 구간(타일1~9, 12~19)에서는 이것만으로 속도가 4.0에 고정된 채 전혀 끊기지 않는 것을 실측 확인
- ⚠️ **1차 수정만으로는 코너(타일9→10, 유일한 회전 지점)에서 새로운 문제 발견**: 브레이크 없이 고속으로 코너에 진입하니 경로를 크게 벗어났다가(Z가 8→9.11까지 오버슈트) 급하게 되돌아오는 **역주행성 S자 뒤뚱거림**이 코너 다음 타일(11번)에서까지 이어짐(방향 벡터가 북→동을 지나 서쪽까지 스윙) — 감속 스터터보다 시각적으로 더 어색한 움직임이라 그대로 둘 수 없었음
- **2차 시도(기각)**: `arrivalThreshold`를 0.15→0.5로 올려서(다음 타일로 더 일찍 조향) 코너 진입을 완화해보려 했으나, 이 값은 `PathManager.SetCurrentPlayerTileIndex()`(진행바·튜토리얼 강제 슬롯·가시함정 등 전부 이 값 기준)에도 그대로 쓰이고 있어서, 늘리자마자 **`first_hit` 튜토리얼의 하드코딩된 강제 슬롯(타일3 Left)이 이미 지나친 타일 취급되어(`CanPlaceAt`가 false) 튜토리얼이 영영 진행 불가능해지는 회귀**가 실측으로 확인됨(2026-09-10에 이미 한 번 고쳤던 "이미 지나친 타일을 가리킴" 버그가 다른 경로로 재발). 이 방식은 폐기
- **최종 수정(`PlayerAutoMover.cs`)**: "게임 로직에 보고하는 도달 판정"과 "NavMeshAgent 조향 목표"를 완전히 분리
  - `reachedTileIndex`(신규) — 기존과 똑같이 `arrivalThreshold(0.15)` 기준, 단 `agent.remainingDistance` 대신 **실제 플레이어 위치와 다음 타일 좌표 사이의 실측 거리**로 독립 계산해 `pathManager.SetCurrentPlayerTileIndex()`에 보고(진행바·튜토리얼·가시함정 등 기존 타이밍 100% 유지)
  - `steeringTileIndex`(신규, 기존 `targetTileIndex`를 역할 분리하며 개명) — 새 필드 `steeringLookahead(0.6)` 기준으로 `arrivalThreshold`보다 훨씬 여유 있게 미리 다음 타일로 `SetDestination()`을 넘겨서, NavMeshAgent가 코너 앞에서 미리 방향을 틀 시간을 줌
  - 결과적으로 코너 근처에서만 조향이 더 부드러워지고, 직선 구간·이벤트 타이밍(튜토리얼 등)은 전혀 영향받지 않음
- **최종 검증(Play 모드, 실측 로그, 전체 20타일 완주)**:
  - 직선 구간: 시작~타일9, 타일12~19 전 구간 `speed=4.00` 그대로 유지, 끊김 없음
  - 코너(타일9→10): 속도가 4.00→2.61까지만 완만히 떨어졌다가 다시 4.00으로 자연스럽게 복귀, 방향 벡터가 북(0,1)→동(1,0)으로 역주행 없이 단조롭게 회전, Z좌표 오버슈트도 8.74까지만(이전엔 9.11+역주행) 완만하게 줄어들며 수렴 — 뒤뚱거림 완전히 사라짐
  - `first_hit` 튜토리얼 게이트: `reachedTileIndex=2`(요구 슬롯=타일3 Left)일 때 정확히 발동해 `CanPlaceAt=True` 확인 — 수정 전 원래 안전했던 타이밍 그대로 보존됨
  - 마지막 타일(19번, 경로 끝) 도달 시 속도가 그대로 뚝 멈추는 것은 실제 종점 도착(더 이상 갈 곳 없음)이라 정상 동작, 버그 아님
  - 컴파일 에러 없음. 두 씬(`Stage1(Test)`, `Stage2`) 모두 `Player`의 `PlayerAutoMover`/`NavMeshAgent`가 프리팹 참조라 오버라이드 없이 그대로 적용됨(씬 자체는 수정 안 해서 `isDirty=false`)
- **`Assets/Fox/Prefabs/Fox.prefab` 반영**: `NavMeshAgent.autoBraking=false`(인스펙터 표시를 실제 동작과 일치시킴, 코드가 항상 강제하므로 기능적으로는 이전에도 이미 꺼진 상태였음), `PlayerAutoMover.steeringLookahead=0.6` 확정값으로 저장. `arrivalThreshold(0.15)`는 그대로 유지 — ⚠️ **2026-09-16 저녁 업데이트**: 이 `steeringLookahead`(고정 거리) 필드는 가속 스킬 코너 이탈 버그를 고치면서 `steeringLookaheadTime`(고정 시간, 0.15)로 대체됨. 아래 "사용자 제보 버그 2건 수정" 절 참고
- ⚠️ **참고(이번 작업과 무관하게 발견한 사항)**: PROGRESS.md 하단 "확정 수치"엔 Fox 이동속도가 3.5로 기록돼 있는데, 실측한 프리팹·두 씬의 `NavMeshAgent.Speed`는 전부 **4**였음(2026-09-15 "이동속도 관리 방식 정리" 이후 언젠가 4로 다시 바뀐 것으로 보임). 이번 작업 범위 밖이라 건드리지 않았고 기록만 남김 — 다음에 이동속도를 만질 일이 있으면 실제 값(4)을 기준으로 확인할 것

### 캐릭터 크기 조정 — 여우 0.85배 / 닭 2배, 전부 프리팹에 반영 (2026-09-16)

- **요청**: 여우(Fox) 스케일을 현재의 약 0.85배로 축소, 닭(Chicken 계열)은 현재의 2배로 확대. "프리팹에 적용해서 전부 바뀔 수 있게" 해달라는 요청에 따라 씬 인스턴스가 아니라 **프리팹 원본**을 수정(두 씬의 `Player`/몬스터 인스턴스는 전부 프리팹 참조라 오버라이드 없이 자동으로 반영됨, 씬 자체는 건드릴 필요 없었음)
- **적용한 스케일**: `Fox.prefab` 루트 스케일 (1,1,1)→**(0.85,0.85,0.85)**. `Chicken.prefab`/`Monster_AreaType.prefab`/`Monster_DPSType.prefab` 루트 스케일 (2,2,2)→**(4,4,4)**(전부 "루트=로직, `Model` 자식=비주얼" 구조라 루트 스케일 하나로 시각 모델·콜라이더가 전부 같이 커짐/작아짐 — `CapsuleCollider`는 로컬 값이라 스케일에 자동으로 곱해져서 별도 수정 불필요)
- **부가 확인 사항 (요청받은 대로 하나씩 점검)**:
  - **사거리 원(`AttackRangeIndicator`)·시야 판정(`FogVisibility`)**: 코드를 확인해보니 둘 다 `transform.position` 기준 월드 절대 거리(`Vector3.Distance`, `Physics.OverlapSphere`)로 계산해서 **몬스터 스케일과 완전히 무관** — 크기를 키워도 사거리·시야 범위(숫자)는 전혀 안 바뀜(확인 완료, 수정 불필요)
  - **투사체 발사 높이(`MonsterTower.projectileSpawnHeight`)**: 이 값은 `transform.position + Vector3.up * 값`처럼 월드 절대 높이로 더해지는 방식이라 Collider와 달리 **스케일에 자동으로 안 곱해짐** — 스케일 2배에 맞춰 0.6→**1.2**로 수동 조정(기존에도 몸통의 60% 높이였는데, 그 비율 그대로 유지). 같은 이유로 `targetAimHeight`(Fox의 몸 어디를 조준할지, 0.9)도 Fox가 0.85배로 작아진 만큼 **0.765**로 축소(세 몬스터 프리팹 전부 동일 적용)
  - **`NavMeshAgent` 반지름·높이(Fox)**: `CapsuleCollider`와 달리 NavMeshAgent의 Radius/Height는 트랜스폼 스케일에 자동으로 안 곱해지는 걸 확인 — 0.5/1.8 → **0.425/1.53**(0.85배)로 수동 조정
  - **여우 체력바**: `PlayerHealthUI`는 화면 고정 HUD(Screen Space)라 캐릭터 크기와 무관 — 확인만 하고 손댈 것 없음
  - **스킬 이펙트 높이(`SkillVfxController`)**: 이 컴포넌트는 원래 Fox 프리팹이 아니라 **스테이지2 씬의 `Player` 인스턴스에만** 붙어있음(스킬은 스테이지2 전용이라 기존에도 그랬음, E0 때 프리팹에 반영됐던 `PlayerAutoMover`/`PlayerHealth`와는 다른 상태). 코드 자체는 `CompensateScale()`로 스케일과 무관하게 항상 같은 절대 크기로 이펙트를 그리도록 이미 설계돼 있어서, Fox가 작아진 만큼 수치도 같이 줄여줘야 비율이 맞음 → `characterHeight` 1.8→1.53, `shieldRadius` 0.9→0.765, `freezeIconHeight` 2.2→1.87(전부 0.85배)로 스테이지2 씬 인스턴스에 직접 반영(프리팹에 없는 컴포넌트라 프리팹 편집으로는 안 됨)
  - **닭 몸이 길·벽 슬롯을 침범하는지**: 실측으로 확인 — 두 스테이지의 몬스터 중 벽 슬롯에 가장 가까운 개체(스테이지1 Monster_B, 스테이지2 Monster_DPS_1)의 실제 시각 모델(Model 자식의 렌더러 바운드, 스케일 4 반영) 표면과 가장 가까운 벽 슬롯 사이 간격을 계산 — 각각 1.15유닛, 1.22유닛 여유로 **침범 없음** 확인
- **검증**: 두 씬 모두 Play 모드에서 스크린샷 확인
  - 스테이지2: 플레이어를 무적 처리 후 `Monster_DPS_1` 근처로 위치시켜 투사체 사거리 원·크기 비교 스크린샷 확인 — 작아진 여우와 커진 닭이 배경·벽 슬롯 받침대와 비교해 자연스러운 비율로 보임, 닭이 슬롯 받침대 줄과 겹치지 않음
  - 스테이지1: `Monster_C`를 강제로 공개시켜 근접 스크린샷 확인 — 발이 바닥에 제대로 붙어있고(파묻히거나 뜨지 않음) 벽 슬롯 받침대 줄과 몸통이 겹치지 않는 것 확인
  - ⚠️ **이번 작업과 무관하게 발견한 별개의 사전 존재 버그 → 삭제 처리(2026-09-16, 같은 날 후속)**: 스테이지2의 `Monster_Basic_2`/`Monster_DPS_2` 두 인스턴스는 `Model` 자식 밑에 실제 메시(`Chicken_001`)가 없고 뼈대(`Chicken_001_rig`)만 남아있어서 렌더러가 비어있었음(몸이 안 보이는 상태로 추정). 사용자 판단: "고치기보다는 일단 없애놓고 다음 작업으로 — 어차피 스테이지2는 내가 싹 다시 배치할 것"이라 **두 GameObject를 씬에서 삭제**(`Stage2.unity` 저장 완료). 스테이지2는 현재 몬스터 4마리(`Monster_Basic_1`/`Monster_Area_1`/`Monster_Area_2`/`Monster_DPS_1`)만 남은 임시 상태 — **사용자가 스테이지2 배치를 전면 재작업할 예정이므로, 다음 세션에서 몬스터 개수·배치가 오늘 기록과 달라져 있어도 정상**(재배치 완료 전까지는 이 문서의 스테이지2 몬스터 배치 관련 옛 기록을 신뢰하지 말 것)
- 컴파일 에러 없음. `Fox.prefab`/`Chicken.prefab`/`Monster_AreaType.prefab`/`Monster_DPSType.prefab` 4개 프리팹 에셋과 `Stage2.unity`(SkillVfxController 값만) 저장 완료. `Stage1(Test).unity`은 씬 자체는 수정할 게 없어서 그대로(`isDirty=false`)

### `docs/feature-expansion.md` 진행 상황 — 7번 섹션의 E0→E6 순서를 그대로 따른다

- **E0 (확장 준비) — ✅ 완료(2026-09-15)**: 아래 "E0 — 확장 준비" 절 참조
- **E1 (스킬 시스템) — ✅ 로직은 완료(2026-09-15)**: 아래 "E1 — 스킬 시스템" 절 참조. 사용자가 스킬 횟수는 기획서 그대로("나중에 수정하기 편하게만 해달라") 확답
- **E2 (조명탄) — ✅ 로직은 완료(2026-09-15)**: 아래 "E2 — 조명탄" 절 참조. 기획서 기본안("4타일 앞 자동")이 아니라 **사용자 지정으로 처음부터 클릭 지정 방식**으로 구현함(기획서 8번 확인 질문에 대한 사용자 답변)
- **스킬 시각 연출 — ✅ 재구현 완료(2026-09-15, 다섯 번째 작업)**: 아래 "스킬 시각 연출 재구현" 절 참조. 사용자가 준 `docs/skill-vfx.md` 스펙 + `SkillVfxLibrary`/`SkillVfxController`/`FlareVfx` 참고 코드를 그대로 씬에 연결해 4개 스킬 전부 시각 효과 확인 완료(무적 보호막이 "감싸는 느낌"으로 제대로 렌더링되는 것 포함)
- **E3(함정) — ✅ 완료(2026-09-16)**: 낙석(주기형) + 가시함정(상시형) 신규 구현. 아래 "E3 — 함정" 절 참조. 이번 마감(2026-09-17)의 목표 범위(범위 축소 결정)를 달성함
- ⚠️ **범위 축소 결정(2026-09-16, 사용자)**: "시간 관계상 기능 구현은 E3까지만" — 마감(2026-09-17)을 고려해 **E3(함정)까지만 완료를 목표**로 하고, E4(빙결 지대)는 시간이 남으면 착수, 안 되면 다음으로 미룸. E5(튜토리얼·배치·마무리)/E6(빌드)은 그 이후. 순서 자체(E0→E6)는 그대로 유지하되 우선순위/마감 기준만 E3로 낮춘 것 — 임의 판단 금지, 이 결정 그대로 따를 것
- E4 (빙결 지대) / E5 (튜토리얼·배치·마무리) / E6 (빌드) — 순서대로 미착수. E6은 **사용자가 빌드를 지시한 뒤에만** 진행(기획서 명시)
- 8번 섹션의 나머지 확인 사항(5~10스테이지 제작 시점, 스테이지2 몰아넣기 방식 동의, 5장 배치안 타일 위치)은 해당 단계(E3~E5)에 도달했을 때 그때그때 물어볼 것 — 한 번에 다 묻지 않음

### E0 — 확장 준비 (2026-09-15 완료)

- **문제**: `Assets/Fox/Prefabs/Fox.prefab`의 루트(`Fox`, Tag=Player)에는 원래 `Transform`/`CapsuleCollider`/`NavMeshAgent`만 있고 `PlayerAutoMover`/`PlayerHealth`가 없었음(두 씬의 `Player` 인스턴스에만 개별로 붙어있던 상태). 새 스테이지에 `Fox.prefab`을 그대로 놓으면 두 스크립트가 빠진 채로 나오는 문제
- **처리**: 사용자 승인 받은 뒤 `manage_prefabs(modify_contents)`로 프리팹 루트에 `PlayerAutoMover`(arrivalThreshold=0.15)/`PlayerHealth`(maxHealth=100, hitColor=빨강, hitFlashDuration=0.15) 추가. 씬 전용 참조(`pathManager` 등)는 비워둠(null)
- ⚠️ **예상 부작용 발견·수정**: 프리팹에 두 컴포넌트를 추가하니, 이미 그 두 컴포넌트를 인스턴스에 개별로 붙여뒀던 기존 두 씬(`Stage1(Test)`, `Stage2`)의 `Player`에 **컴포넌트가 중복**으로 붙어버림(프리팹이 새로 정의한 것 + 기존 인스턴스가 갖고 있던 것, 각 타입당 2개씩). 그대로 뒀으면 이동/체력 로직이 두 번씩 실행되는 심각한 버그가 됐을 것 — 두 씬 모두에서 **새로 생긴 쪽(씬 참조가 비어있는 index 0)**을 `manage_components(remove)`로 제거하고, 기존에 씬 참조(`pathManager`=해당 씬의 `Path`)가 올바르게 연결된 원래 컴포넌트만 남김. `MainMenu` 씬에는 Fox 인스턴스 자체가 없어 해당 없음
- **검증**: 두 씬 모두 컴포넌트 5개(중복 없음)로 정리된 것 확인. `Stage1(Test)`에서 Play 모드로 직접 재생해 Player가 정상적으로 타일을 따라 이동하고(`NavMeshAgent.isOnNavMesh=true`, 목적지 추적), 체력 100/100으로 정상 초기화되는 것 확인. 콘솔 에러 없음(무관한 `NoSubscription`만 존재). 두 씬 모두 저장 완료
- **게이트 통과**: 빈 씬에 `Fox.prefab`을 새로 놓으면 두 스크립트가 기본값으로 붙어 나옴(prefab get_info로 확인) / 기존 두 스테이지 동작에 변화 없음(중복 제거 후 재생 테스트로 확인)

### E1 — 스킬 시스템 (2026-09-15 완료, 스테이지2 전용)

- **신규 스크립트**: `SkillManager.cs`(로직 전담 — 남은 횟수·효과 적용/해제, `CurrencyManager` 패턴과 동일하게 UI 의존성 없음), `SkillButtonUI.cs`(버튼 하나의 표시), `SkillBarUI.cs`(4개 슬롯을 `SkillManager` 상태에 맞춰 갱신). `SkillType` enum(Accelerate/Invincible/Stop/Flare)과 `SkillConfig`(씬별 설정: 아이콘/총 횟수/지속시간)도 `SkillManager.cs`에 포함
- **기존 스크립트 수정**:
  - `PlayerAutoMover.cs`: 출처(source)별 속도 배율을 등록·합산하고 상한(기본 2.5)을 적용하는 `SetSpeedMultiplier(object, float)` 추가(빙결 지대 등 나중에 추가될 배율도 이 한 곳에 합류하도록 설계, 기획서 4-2-1 요구사항). 정지 스킬용 `SetPaused(bool)` 추가 — 멈추는 동안 이동 애니메이션도 꺼짐
  - `PlayerHealth.cs`: `SetInvincible(bool)` 추가, `TakeDamage`가 무적 중이면 무시(투사체 등은 평소처럼 소멸하되 피해만 0)
  - `StageManager.cs`: `OnRetried` 이벤트 추가(`RetryStage()` 시작 시 발행) — `SkillManager`가 이걸 구독해 재도전 시 남은 횟수·진행 중 효과를 초기화함(스테이지 매니저는 스킬 시스템의 존재를 전혀 모름, 이벤트로만 연결)
  - `WallPlacementManager.cs`: 클릭 처리 맨 앞에 `EventSystem.current.IsPointerOverGameObject()` 가드 추가 — 스킬 버튼 등 UI 클릭이 벽 배치로 새지 않게 함
- **UI 구성**: `Canvas/SkillBar`(하단 중앙, `HorizontalLayoutGroup` 가운데 정렬) 아래 Layer Lab `Button_Circle128_White` 4개(`SkillButton_1~4`) — 각각 아이콘(`Pictoicon_Speed`/`Shield`/`Control_Pause`/`Light`, 64px), 남은 횟수(우하단)·단축키(좌상단) TMP 숫자, 지속시간 게이지(원형 Radial360 Fill, 진행 중에만 표시) 자식으로 구성. 키보드 1~2~3~4(`Keyboard.current`)는 `SkillManager.Update()`가 직접 처리
- **Player 자식 연출**: `TrailRenderer`(가속, `M_SkillTrail.mat` 신규), `InvincibleShield`(무적, Sphere+`M_SkillShield.mat` 신규, SphereCollider는 제거), `PauseIcon`(정지, SpriteRenderer+`Icon_PictoIcon_Pause`, 카메라가 완전 탑다운이라 X축 90도 회전으로 눕혀서 위를 보게 함) — 전부 평소엔 비활성, 효과 중에만 활성화
- ⚠️ **버그 발견·수정 1(가림 문제와 같은 원인 계열)**: 처음 쓴 Layer Lab Picto 아이콘들이 전부 **흰색 실루엣**이라(픽셀 샘플링으로 확인) 흰색 원형 버튼 배경 위에서 안 보였음 — `SkillButtonUI`의 `normalColor` 기본값을 흰색에서 어두운 갈색 계열로 변경해 대비 확보
  - ⚠️ **연쇄 실수·복구**: 텍스처 확인용으로 `isReadable=true`로 바꿨던 것을 확인 직후 바로 `false`로 되돌림(원래 상태 복원 습관화)
- ⚠️ **버그 발견·수정 2**: `SkillBarUI.OnEnable()`이 `SkillManager.Awake()`보다 먼저 실행되는 경우(오브젝트 간 실행 순서 미보장)가 있어 `NullReferenceException` 발생 — `SkillManager`에 `EnsureInitialized()` 가드를 추가해 `GetState`/`CanUse`/`HandleRetry` 등 배열을 쓰는 진입점 전부에서 안전하게 지연 초기화하도록 수정
- **검증(Play 모드, 리플렉션 없이 public API 직접 호출)**:
  - 가속: 사용 시 속도 3.6→7.2(×2) 확인, 지속시간(1.5초) 경과 후 3.6으로 원복·남은 횟수 유지(2→1) 확인
  - 무적: 사용 중 보호막 활성화 + 30 피해를 줘도 체력 불변(100 유지) 확인
  - 정지: 사용 시 `NavMeshAgent.isStopped=true` + 정지 아이콘 표시 확인
  - 같은 스킬 중복 사용 시도 → `CanUse`가 false로 막는 것 확인
  - 사망 중에는 `CanUse`가 전부 false(스킬 사용 불가) 확인
  - **재도전**: `StageManager.RetryStage()` 호출 → 체력 100 회복 + 사용했던 스킬 횟수까지 전부 총량으로 복원되는 것 확인
  - 조명탄(횟수 0)은 스크린샷에서 버튼 자체가 안 보이는 것 확인(자동 숨김 정상 동작)
  - 스크린샷으로 3개 버튼(가속/무적/정지)이 하단 중앙에 정렬되고, 사용한 스킬은 게이지/소진 시 어둡게 변하는 것도 확인
  - ⚠️ 이날 세션 내내 반복된 "에디터 포커스 없으면 Play 모드 시간이 멈춤" 현상이 여기서도 발생해(무적 스킬 지속시간 종료를 실시간으로 못 봄), 가속 스킬로 이미 "지속시간 경과 후 정상 종료"를 실측 확인했으므로 같은 코드 패턴(코루틴+scaled time)을 쓰는 무적/정지도 로직상 동일하게 동작한다고 판단
  - 벽 클릭 가드(`IsPointerOverGameObject`)는 구조적으로 표준적인 방식이라 별도 클릭 시뮬레이션 없이 코드 검토로 확인. 실제 마우스로 스킬 버튼을 눌러보는 것은 사용자가 에디터에서 확인해줄 것
- **게이트 통과 여부**: 효과·지속시간 일치 ✅ / 횟수 소진 시 사용 불가 ✅ / 재도전 시 전부 초기화 ✅ / 벽 배치와 안 겹침(구조적 확인) ✅ / 튜토리얼 정지 중 사용 불가는 `TutorialManager.IsOpen` 정적 플래그를 그대로 참조하는 방식이라 로직상 보장됨(스테이지2에 현재 튜토리얼 단계가 없어 실측은 못 함)
- **스테이지1은 건드리지 않음**(기획서 요구대로) — `SkillManager`/`SkillBar`는 스테이지2 씬에만 존재

### E2 — 조명탄 (2026-09-15 완료, 스테이지2 전용)

- **사용자 결정**: 기획서 기본안은 "4타일 앞 자동 조준, 클릭 지정은 시간 남으면 나중에"였으나, 사용자가 **"처음부터 클릭 지정으로"** 진행하라고 확정 → 자동 조준 버전은 만들지 않고 클릭 지정만 구현함
- **신규**: `VisionSourceRegistry.cs` — 플레이어 시야 외에 추가로 시야를 밝히는 소스(조명탄 등)를 등록하는 전역 정적 목록(`Register`/`Unregister`/`IsWithinAnySource`/`Snapshot`). 각 컴포넌트 고유의 `visionRadius`는 절대 건드리지 않고, 그 판정에 OR 조건으로 추가만 함(기획서 2-3④·E2 게이트 요구사항)
- **기존 컴포넌트 수정(전부 기존 `visionRadius` 판정에 OR로 추가하는 방식, 기존 값 변경 없음)**:
  - `FogVisibility.cs` / `AttackRangeIndicator.cs`: `distance <= visionRadius` 조건에 `|| VisionSourceRegistry.IsWithinAnySource(transform.position)` 추가
  - `FogOfWarGround.cs`: `RevealAt(Vector3)` → `RevealAt(Vector3, float radius)`로 시그니처 변경(반경을 인자로 받게), `LateUpdate()`에서 플레이어 위치 처리 후 `VisionSourceRegistry.Snapshot()`을 순회하며 추가로 걷어냄
- **`SkillManager.cs`에 조명탄 흐름 추가**:
  - 스킬 버튼(4번) 클릭 → `awaitingFlareTarget=true` + `WallPlacementManager.InputLocked=true`(조준 중 벽 배치 클릭과 안 겹치게)
  - `Update()`에서 다음 좌클릭을 지면(Y=0 평면, `Plane.Raycast`)과의 교차점으로 확정 — 콜라이더 레이어에 의존하지 않아 나무·바위·몬스터 콜라이더에 가려질 걱정이 없음. UI 위 클릭은 무시(`EventSystem.IsPointerOverGameObject`)
  - 확정되면 `RunFlare` 코루틴 — 착지 지점에 `Light`(Point, range=10=반경5×2, 따뜻한 색) 생성 + `VisionSourceRegistry.Register(그 지점, 반경5)`, 지속시간(3초) 후 `Unregister`+오브젝트 파괴
  - 재도전 시 조준 대기 중이었거나 조명탄이 켜져 있었으면 전부 취소·정리하고 벽 배치 잠금도 풀림(`HandleRetry`에 추가)
- **씬 반영**: `Managers`의 `SkillManager`에 `wallPlacementManager` 참조 연결, 조명탄 `totalUses` 0→1로 변경(E1 때는 미구현이라 0으로 숨겨뒀던 것)
- **검증(Play 모드)**: `TryUseSkill(3)` 호출로 조준 모드 진입 시 `WallPlacementManager.InputLocked=true` 확인 → 리플렉션으로 `RunFlare` 직접 호출(실제 클릭 시뮬레이션이 안 되는 환경이라, 클릭으로 위치가 정해진 "이후" 상황을 재현) → 안 보이던 몬스터 위치에 조명탄을 쏘자 `VisionSourceRegistry.IsWithinAnySource(그 몬스터 위치)`가 true로 바뀌는 것 확인, `Light` 컴포넌트 값(Point/range10/따뜻한 색)도 정상 확인 → `Unregister` 후 다시 false로 돌아오는 것 확인 → `RetryStage()` 호출로 조명탄 횟수 복원(1) + 활성 상태 해제 + 벽 배치 잠금 해제까지 전부 확인. 콘솔 에러 없음(무관한 `NoSubscription`만 존재)
- ⚠️ **실시간 재생으로 직접 못 본 부분**: 이날 세션 내내 반복된 "에디터 포커스 없으면 Play 모드 시간이 멈춤" 현상이 여기서도 발생해(`Time.frameCount`가 1에서 안 움직임), `FogVisibility`/`AttackRangeIndicator`의 `LateUpdate()`가 실제로 몬스터를 드러내는 장면과 실제 마우스 클릭으로 위치를 지정하는 것 자체는 실시간으로 확인 못함 — 위 방식(직접 호출)으로 로직 자체의 정확성만 확인함. **사용자가 에디터에서 직접 스킬 4번을 눌러 바닥을 클릭해보고, 조명탄 범위 안 몬스터·사거리·안개가 실제로 걷히는지 한 번 확인해주면 좋음**
- **게이트 통과 여부**: 조명탄 범위의 몬스터·사거리·안개가 드러남(레지스트리 값으로 확인, 실시간 시각 확인은 미완) ✅(부분) / 각 컴포넌트의 `visionRadius` 값 불변(코드상 OR 추가만 했으므로 확정) ✅ / 기존 플레이어 시야 동작 무변화(같은 이유로 확정) ✅

### E3 — 함정: 낙석(주기형) + 가시함정(상시형) (2026-09-16 완료, 스테이지2 전용)

- **신규 스크립트**:
  - `RockfallTrap.cs` — 대기(2.5초)→경고(1.2초, 원이 0→목표 반경까지 커짐)→낙하(0.3초, 중력 가속 느낌으로 위에서 떨어짐)→충돌(그 순간 한 번만 `Physics.OverlapSphere`로 판정)→잔해(0.5초, 흔들리다 사라짐)를 반복하는 코루틴. `startDelay`로 함정마다 위상을 다르게 둘 수 있음
  - `SpikeTrap.cs` — 지정한 연속 두 타일 위에 항상 가시가 솟아있고, 그 위에 있는 동안 0.25초마다 4 피해
  - `FogVisibility.cs`에 `SetTargetRenderers(Renderer[])` 공개 메서드 추가 — 절차적으로 나중에(Start 시점) 생성되는 비주얼(가시 등)을 안개 규칙에 편입시키기 위한 최소 추가
- **판정에 쓴 설계 판단**:
  - **낙석 경고 원**: "지형에 안 가려지게"가 요구사항이라 `AttackRangeIndicator`가 쓰는 것과 같은 `Custom/RangeIndicatorOverlay`(ZTest Always) 셰이더를 재사용하되, 링이 아니라 "점점 채워지는 원"이 필요해서 얇은 실린더(Cylinder) 프리미티브를 0→목표 지름으로 키우는 방식으로 구현(솔리드 색이라 원형 실루엣 자체가 곧 "채워짐"을 표현)
  - **낙석 비주얼과 게임 로직 분리(중요)**: 낙석은 대기/경고/낙하/충돌/잔해에 따라 "지금 이 순간 보여야 하는가"가 계속 바뀌는데, `FogVisibility`를 낙석 오브젝트에 직접 붙이면 시야에 들어오는 순간 지금 단계와 무관하게 렌더러를 강제로 켜버려서 충돌함(예: 대기 중인데 시야에 들어왔다고 바위가 보여버림). 그래서 `FogVisibility`는 렌더러가 없는 별도 자식(`FogSensor`)에 붙이고, `RockfallTrap`이 "안개로 공개됐는가"(이벤트 구독)와 "지금 단계가 보여야 하는가"를 매 프레임 직접 곱해서 최종 가시성을 결정하도록 분리함
  - **가시함정 판정 방식(물리 오버랩 박스 vs 타일 인덱스, 기획서가 선택·근거 명시를 요구함)**: `PathManager`가 보고하는 "현재 타일"은 `arrivalThreshold` 기준으로 갱신되고, 어제 수정한 코너 스티어링(`steeringLookahead`) 때문에 실제 위치가 타일 중심선에서 다소 벗어날 수 있음 — 가시는 "물리적으로 그 자리에 있는가"가 핵심이라 타일 인덱스 비교 대신, 두 타일의 실제 진행 방향(`PathManager.GetTileForward`, 벽 슬롯 배치와 동일 기준)에 맞춰 회전시킨 `Physics.OverlapBox`로 두 타일 영역을 정확히 덮어 판정하도록 선택함(타일 인덱스보다 실제 위치에 항상 더 정확함)
  - **가시 비주얼**: Unity 기본 프리미티브에 원뿔이 없어서, 원뿔 메시를 코드로 직접 생성(밑면 다각형 8각형 + 꼭짓점)하는 헬퍼를 `SpikeTrap.cs` 안에 작음. 금속 회색 `URP/Lit` 머티리얼(Metallic 0.75) 인스턴스를 만들어 입힘. 구간 안에 8개를 살짝 어긋나게 뿌려 배치
  - **낙석 바위**: `Low Poly Nature Bundle`의 `Rock_Round_l_01.fbx`를 그대로 인스턴스화하되, 임포트 기본 머티리얼이 밋밋해서 이미 프로젝트에서 쓰고 있던 `Rocks_Moss.mat`(SlotPedestal 등과 동일 재질, `sharedMaterial`로 참조만 — 원본 에셋은 안 건드림)로 덮어씀. 판정용 콜라이더는 제거(순수 비주얼, 충돌 판정은 `OverlapSphere`로 별도 처리)
  - **재도전**: `StageManager.RetryStage()`에 `FindObjectsByType<RockfallTrap>()`/`FindObjectsByType<SpikeTrap>()` 순회 후 `ResetTrap()` 호출을 추가(기존 `FogVisibility`/`AttackRangeIndicator` 일괄 리셋과 동일한 패턴). 낙석은 원래 `startDelay`부터 처음(대기)으로 재시작, 가시는 틱 타이머만 초기화
- **스테이지2 배치**(기획서 5-3 제안 그대로): 가시함정 = 타일6~7(세그먼트A 후반, 몬스터 사거리와 겹치는 구간), 낙석 2개 = 타일9(코너)·타일11(코너 지난 직후), 위상은 절반 주기(2.25초)만큼 어긋나게. `Traps` 그룹으로 하이어라키 정리
- **검증(Play 모드, 실측 로그 — 헤드리스 환경 특성상 `Application.runInBackground=true` 필요, 이전 작업들과 동일)**:
  - **경고→낙하→충돌→잔해 타이밍**: 로그로 각 단계 지속시간 실측 — 경고 약 1.1~1.2초(원 반지름이 0→3.0까지 선형 증가), 낙하 약 0.3초(가속 낙하), 잔해 약 0.4~0.5초, 전체 한 주기 약 4.47~4.5초로 기획서 수치(대기2.5+경고1.2+낙하0.3+잔해0.5=4.5)와 거의 정확히 일치
  - **원 안에서만 맞는다**: 사거리 밖(6유닛 이격)으로 워프한 사이클 → 피해 없음(hp 88→88) / 사거리 안(정중앙)으로 워프한 다음 사이클 → 정확히 15 피해(hp 88→73, 피해량도 설정값과 일치) 확인
  - **무적 중 피해 0**: 사거리 안 + 무적 상태로 다음 사이클 진입 → hp 변화 없음(73→73) 확인
  - **가시 위 틱 피해**: 몬스터를 잠깐 꺼두고 구간 중앙에 정지시켜 관찰 → `OnDamaged` 이벤트가 정확히 4 피해로 약 0.25초 간격마다(0.24~0.25초 실측) 꾸준히 발행되는 것을 25회 연속 확인(hp 100→0). 구간을 벗어나자 즉시 틱이 멈추는 것도 확인(이후 무관한 낙석 피해만 별도로 발생)
  - **안개 밖에서는 안 보인다**: 재도전 직후(공개 전) 낙석 바위·경고 원, 가시 8개 렌더러 전부 `enabled=false` 확인
  - **벽이 함정을 막지 않는다**: 코드 검토로 확정 — 두 함정 모두 `WallVision` 레이어를 전혀 참조하지 않고 `AttackTarget` 레이어만 검사하므로 구조적으로 벽과 무관함
  - **결과 화면 피격 마커**: 별도 재현은 안 했지만, 두 함정 모두 기존 `PlayerHealth.TakeDamage()`를 그대로 거쳐 `OnDamaged`를 발행하므로 `StageManager.HandleDamaged`(타일별 피해 누적)가 몬스터 피해와 동일하게 자동으로 동작함(코드 경로 동일, 이미 검증된 메커니즘 재사용)
  - 컴파일 에러 없음. `Stage2.unity` 저장 완료, `Stage1(Test).unity`은 함정을 넣지 않아 무관(그대로)
- **게이트 통과 여부**: 경고→낙하→충돌 순서·시간 ✅ / 원 안에서만 맞음 ✅ / 가시 틱 피해 ✅ / 무적 중 피해 0 ✅ / 벽이 함정을 막지 않음 ✅(코드상 확정) / 안개 밖에서는 안 보임 ✅ / 결과 화면 피격 마커 반영 ✅(자동 경로, 별도 재현은 생략)
- **다음**: 시간 관계상 이번 마감엔 E4(빙결 지대)는 여유 되면, 급하면 E5로 건너뜀(2026-09-16 사용자 결정, 위 "범위 축소 결정" 참조)

### 기능 구현 종료 → 스테이지 제작 단계로 전환, `docs/stage-add-guide.md` 1장(템플릿 씬) 완료 (2026-09-16)

- **전환 결정**: 사용자가 "기능 구현 작업은 여기까지만 하고 이제 본격적으로 스테이지들을 만들 것"이라고 확정. `docs/feature-expansion.md` 기준 작업은 E3에서 멈추고(E4 이후는 보류), 이제부터는 사용자가 새로 준 `docs/stage-add-guide.md`(템플릿 씬 + MCP 위임 절차) 순서를 따름 — 이 문서는 `feature-expansion.md`와 별개 문서이며 서로 수정하지 않음
- ⚠️ **문서 자체 모순 발견·보고**: `stage-add-guide.md` 1-4는 "이 문서 4장의 함정 표를 실제 값으로 고친다"고 MCP에게 지시하지만, `CLAUDE.md`의 "절대 하지 말 것"에 `docs/` 폴더 수정이 명시돼 있어 직접 충돌함 — **`docs/` 폴더는 건드리지 않고**, 대신 아래에 실제 값을 기록해두는 것으로 대체함(사용자가 원하면 본인이 문서를 직접 고치거나, 예외를 명시적으로 허락해줄 것을 요청함, 아직 답 없음):
  - 낙석 프리팹(가칭 `Trap_Rockfall`) → 실제로는 `Assets/Prefabs/RockfallTrap.prefab`(컴포넌트명 `RockfallTrap`)
  - 가시 프리팹(가칭 `Trap_Spike`) → 실제로는 `Assets/Prefabs/SpikeTrap.prefab`(컴포넌트명 `SpikeTrap`)
  - 인스펙터 항목은 어제 사용자 질문에 답했던 목록과 동일(위 "저런 수치들은..." 답변 참고 — `RockfallTrap`: Idle/Warning/Fall/Debris Duration, Start Delay, Impact Radius, Damage, Rock Scale 등 / `SpikeTrap`: Tile Index A·B, Tick Interval, Tick Damage, Spike Count 등)
- **1-4 사전 확인 사항 처리**:
  - `Fox.prefab`에 `PlayerAutoMover`/`PlayerHealth` 있는지 → 이미 E0(2026-09-15)에서 해결됨, 재확인만 하고 넘어감
  - 함정 프리팹 실제 이름·경로 → 위 표로 대체(문서 미수정)
- **`Assets/Scenes/Stage_Template.unity` 신규 생성**(1장 절차대로, `Stage2.unity`를 원본으로 복제 후 가공 — **Build Settings에는 추가 안 함**):
  - **유지**: `Main Camera`(+Brain)/`CM_TopDown`/`Directional Light`/`Global Volume`, `Floor`(NavMeshSurface)/`FogGround`, `Managers`(6개 매니저 컴포넌트 그대로), `Canvas`(HUD 전체), `EventSystem`, `Player`(`Fox.prefab` 인스턴스 — `PlayerAutoMover`/`PlayerHealth`/`SkillVfxController` 포함 확인)
  - **삭제**(3장 절차에서 매번 새로 만드는 것들): `Environment`(데코 26개), `ArrivalPortal`, `GroundBumps`, `SlotPedestals`, `Traps`, `Path/WallSlots`(40개) — 전부 문서 1-2의 "들어가지 않는 것" 목록 그대로
  - **경로 축소**: `Path/Tiles`·`Path/PathTilesVisual`을 기존 20칸에서 **6칸 ㄴ자 예시**로 정리(`Tile_00`~`Tile_05`, 2유닛 간격, 좌표 (0,0,0)→(0,0,2)→(0,0,4)→(0,0,6)→(2,0,6)→(4,0,6), 가로세로만·대각선 없음). 길 비주얼(`PathTile_00~05`, Y=0.02, `M_PathTile`)도 같이 6개로 맞춤
  - **몬스터 예시 3마리 신규 배치**: `Monster_Basic_Sample`(`Chicken.prefab`) / `Monster_Area_Sample`(`Monster_AreaType.prefab`) / `Monster_DPS_Sample`(`Monster_DPSType.prefab`), 예시 경로 옆(겹치지 않게) 배치. 기존 스테이지2의 몬스터 4마리는 전부 삭제하고 이걸로 교체
  - **`Player` 위치**: `Tile_00`(0,0,0)으로 이동
  - **매니저 설정값**(표 1-1 기준으로 기존 스테이지2 값에서 바꾼 것만): `CurrencyManager.totalCurrency` 7→**5**, `SkillManager`의 스킬 4종(가속/무적/정지/조명탄) `totalUses` 전부 **0**(→ 스킬 버튼은 `SkillButtonUI.Refresh()`가 `Total>0`일 때만 보이게 이미 구현돼 있어서 자동으로 다 숨겨짐, 별도 UI 작업 불필요). `WallPlacementManager`(`wallLimitEnabled=true`/`wallCost=1`)·`TutorialManager`(`steps` 비어있음)·`StageManager`(`nextSceneName` 비어있음)·`PlayStatsLogger`(`excludeReactionTimeThisStage=false`)는 스테이지2 원본 값이 표 1-1과 이미 일치해서 변경 없음
  - `Floor`/`NavMeshSurface` 재빌드(삭제·이동에 맞춰). `Floor`/`FogGround` 크기 자체는 스테이지2 원본(30×60) 그대로 둠 — 표 1-1에 "기존 값 그대로"로 명시돼 있고, 작아진 예시 경로를 덮기엔 넉넉해서 문제 없음(실제 새 스테이지의 바닥 크기 맞추기는 3-2 절차에서 그때그때 함)
- **검증**: 씬 하이어라키 루트 14개(표 1-1과 정확히 일치: 카메라·조명·볼륨·바닥·안개·경로·플레이어·매니저·캔버스·이벤트시스템·샘플 몬스터 3마리), `Path` 자식 2개(`Tiles`/`PathTilesVisual`, `WallSlots`는 삭제됨)로 구조 확인. 씬 뷰 스크린샷으로 ㄴ자 예시 경로·플레이어·몬스터 3마리 배치 눈으로 확인. 컴파일 에러 없음. Build Settings에 안 들어간 것 확인. 씬 저장 완료
- ⚠️ **미완료 — 다음 세션(또는 바로 이어서)**: 1-4의 세 번째 항목("템플릿 완성 후, 템플릿을 복제해 예시 경로 그대로 3장 절차를 한 번 수행해 시작→클리어까지 동작하는지 확인")은 아직 안 함 — 별도의 큰 단계라 템플릿 완성 보고 후 진행 여부를 먼저 확인받기로 함. 이후 실제 스테이지 제작은 문서 2장(사용자가 타일·몬스터 배치) → 5장 양식으로 요청 → 3장 절차(MCP) 순서를 따름

### 오늘 남은 작업 순서 합의 + 스테이지1 배경 확장 (2026-09-16)

- **사용자가 정한 오늘 남은 작업 순서**: ① 사용자가 스테이지2를 기획 의도에 맞춰 직접 재배치(에디터에서 직접 작업) → 관련 튜토리얼 스텝을 나중에 알려주면 그때 반영 ② 스테이지1·2 둘 다 "화면 밖(빈 배경)이 보이는" 문제 고치기 ③ 사용자가 추가 스테이지를 만들면 그 스테이지에 맞게 기능 연결하기
- ⚠️ **에디터 동시 사용 관련 안내**: 사용자가 "내가 스테이지3 만드는 동안 스테이지1 작업 맡겨도 되냐"고 물어봄 → 현재 MCP에 연결된 Unity 인스턴스가 **1개뿐**이라(`mcpforunity://instances` 확인) 같은 에디터를 진짜 동시에 쓸 수는 없다고 답함(씬 전환·Play 모드·스크립트 재컴파일이 전부 공유 상태라 서로 방해됨). 순서대로(사용자가 저장 후 넘겨주면 그때 작업) 진행하기로 함 — **그래서 사용자가 지금 스테이지2를 만지고 있을 가능성이 있는 동안은 스테이지2를 건드리지 않고, 스테이지1만 먼저 처리함**
- **스테이지1 "화면 밖이 보이는" 문제 — 원인 확인 후 수정**:
  - **원인**: `Floor`/`FogGround`가 30×60 크기로 원점(0,0,0) 중심에 고정돼 있는데, 실제 경로 범위(X:-8~12, Z:-10~8)가 그 중심에서 한쪽으로 치우쳐 있어 **동쪽(+X) 여유가 3유닛밖에 안 됨**(서쪽 7, 남북은 20+유닛으로 과하게 넉넉함). 카메라가 경로 동쪽 끝(마지막 타일, 코너) 쪽을 비출 때 이 3유닛을 넘어서는 화면 영역에 `Floor`/`FogGround`가 아예 없어서 뒤쪽 빈 배경이 그대로 보였던 것 — 스크린샷으로 실제 마지막 타일 근처에서 재현 확인(오른쪽에 아무 텍스처 없는 평평한 회색 영역이 뚜렷하게 보임)
  - **수정**: 서·남·북쪽은 이미 여유가 충분해 그대로 두고, **동쪽만 확장** — `Floor`/`FogGround`를 폭 30→35(중심 X 0→2.5)로 넓혀 동쪽 여유를 3→8유닛으로 늘림(깊이 60은 그대로, 원래도 넉넉했음). 커스텀 지형 굴곡 메시 `GroundBumps`(`M_GroundBumps_Stage1`)는 새로 굽지 않고 같은 비율(X만 30→35, 약 1.17배)로 스케일만 늘려서 맞춤 — 완전히 새로 만드는 것보다 간단하지만 배경 굴곡이 미세하게 늘어나 보일 수 있음(눈에 띄면 다음에 새로 구워달라고 말씀해주세요)
  - **새로 넓어진 동쪽 구간(X 14~19)에 데코 6개 추가**(3-6 규칙대로 경로·서로 2.5 이상 이격, Y축 무작위 회전, 레이어 `Default`, `FogVisibility` 부착): 참나무·사과나무·소나무 각 1그루, 바위 2개(원형·사각), 덤불 1개 — 기존 씬에 이미 쓰인 프리팹만 재사용해서 톤이 통일되게 함
  - `Floor`의 `NavMeshSurface` 재빌드
  - **검증**: Play 모드에서 마지막 타일 코너(동쪽 끝)와 시작 타일(서쪽) 양쪽 다 스크린샷으로 확인 — 이전엔 평평한 회색 빈 공간이 보이던 동쪽이 이제 안개 낀 지형과 새 데코(나무·바위 실루엣)로 자연스럽게 이어짐. 서쪽은 원래도 문제없었고 여전히 정상. 컴파일 에러 없음. 씬 저장 완료
- **스테이지2는 아직 그대로 둠** — 사용자가 재배치 작업 끝내고 넘겨주면 같은 방식(동쪽 여유 확인 후 필요하면 확장)으로 이어서 처리할 예정. 스테이지2는 좌표 체계가 스테이지1과 동일(X:-8~12, Z:-10~8)이라 아마 같은 문제가 있을 가능성이 높음 — 확인 후 보고할 것

### 사용자 제보 버그 2건 수정 — 스테이지1 클리어 안 됨 / 코너에서 벗어남(가속 시 심함) (2026-09-16)

- **버그 A — 스테이지1 도착해도 클리어가 안 됨**
  - **원인**: 방금 위 "배경 확장" 작업에서 `Floor`의 `NavMeshSurface`를 재빌드했는데, 이 컴포넌트의 `useGeometry` 설정이 원래부터 `RenderMeshes`(렌더러 기준으로 굽기) + `collectObjects=All` + `layerMask=전체`였음 — 즉 걸어다니는 바닥(`Floor`)뿐 아니라 눈에 보이는 **모든 렌더러**(장식 굴곡 메시 `GroundBumps` 포함, `Y` 방향 두께 0.55)까지 통째로 지면 높이 계산에 들어가버림. 그 결과 실제 구운 NavMesh가 시각적 바닥(Y=0)이 아니라 `GroundBumps` 표면 높이(Y≈0.65)에 만들어졌고, 도착 지점에서 플레이어가 Y=0.65에 서게 되면서 `StageManager`의 클리어 판정(`Vector3.Distance(player, goal) <= arrivalThreshold(0.5)`)이 `0.65 > 0.5`라 절대 통과하지 못했음(경로 전체가 이 높이였어서 걷는 동안 눈으로는 거의 티가 안 났음)
  - **수정**: `Floor`의 `NavMeshSurface.useGeometry`를 `RenderMeshes` → **`PhysicsColliders`**로 변경 후 재빌드. `GroundBumps`는 렌더러만 있고 콜라이더가 없어서 이 방식으로 바꾸면 자동으로 굽기 대상에서 빠지고, 실제 바닥(`Floor`의 `MeshCollider`)만 기준이 됨
  - **검증**: 재빌드 후 `NavMesh.SamplePosition`으로 경로 전 구간 재확인 → 전부 Y≈0.07(사실상 평평)로 정상화. Play 모드에서 플레이어를 마지막 타일로 워프 → `isFinished=True`, CLEAR 화면(다음 스테이지 버튼 포함) 정상 표시 확인. 이동도 평소처럼 정상(장애물에 안 걸림) 확인. 스테이지1 씬 저장 완료
  - ⚠️ **스테이지2도 재확인 필요**: 오늘 스테이지2는 NavMesh를 재빌드한 적 없어서(플로어 확장 작업을 아직 안 함) 지금 당장은 이 문제가 없을 가능성이 높지만, 같은 `useGeometry=RenderMeshes` 설정을 그대로 쓰고 있어서 **나중에 스테이지2 바닥도 재빌드하게 되면 똑같이 터질 수 있음** — 스테이지2 작업 때 미리 `PhysicsColliders`로 같이 바꿔둘 것
- **버그 B — 코너에서 여우가 길을 벗어남, 가속 스킬 쓰면 훨씬 심함**
  - **원인**: 어제(2026-09-16 오전) 만든 코너 스티어링 수정(`steeringLookahead`)은 "다음 타일로 조향을 미리 시작하는 거리"만 다뤘는데, 실측해보니 코너 이탈의 진짜 원인은 그게 아니라 **`NavMeshAgent.acceleration`(가속도)이 고정값(8)이라는 것**이었음 — 이동 속도(`agent.speed`)만 스킬 배율만큼 올라가고 가속도는 그대로면, 훨씬 커진 속도 벡터를 같은 가속도로 꺾어야 해서(자동차가 빠를수록 같은 핸들 힘으로는 회전 반경이 커지는 것과 동일한 원리) 방향 전환에 필요한 거리가 늘어나 코너를 훨씬 크게 벗어남
  - **진단 과정**: `steeringLookahead`를 거리 대신 "속도 × 반응시간" 방식으로 바꿔봤지만(가속 2배 시 코너 이탈 폭 거의 그대로), `angularSpeed`(회전 속도)만 같이 배율을 맞춰봐도 별 차이 없었음(둘 다 실측으로 확인) — `acceleration`까지 같이 배율을 맞추자 비로소 개선됨
  - **수정(`PlayerAutoMover.cs`)**: `ApplySpeedMultiplier()`에서 `agent.speed`뿐 아니라 `agent.angularSpeed`·`agent.acceleration`도 전부 같은 배율로 함께 조정하도록 변경(스킬 등으로 해제되면 셋 다 원래 배수만큼 자동으로 같이 돌아옴). 추가로 `steeringLookahead`도 고정 거리(0.6) 대신 "반응 시간(0.15초) × 현재 속도"로 계산해 속도가 얼마든 반응 시간이 같게 유지(최소값 0.3 보장)
  - **검증(Play 모드, 스테이지2 코너 실측, 기본 몬스터 비활성화 + 무적 처리)**:
    - 기본 속도(배율 없음): 코너 최대 이탈 **0.75유닛**(수정 전과 동일 — 회귀 없음 확인)
    - 가속 2배 적용, 수정 전: 코너에서 최대 **약 3.9유닛**까지 벗어나며 왔다갔다 휘청이는 궤적(원 경로를 완전히 벗어나 한 바퀴 돌아오는 수준)
    - 가속 2배 적용, 수정 후: 코너 최대 이탈 **1.45유닛**으로 줄고, 휘청임 없이 매끄럽게 꺾어 정상적으로 종점까지 도착
  - `Fox.prefab`에 `steeringLookaheadTime=0.15`/`minSteeringLookahead=0.3` 확정값으로 저장(기존 `steeringLookahead` 필드는 이름을 바꿔 대체됨)
  - 컴파일 에러 없음. 스테이지2는 테스트만 하고 실제 씬은 변경하지 않음(사용자가 재배치 작업 중일 수 있어 저장 안 함, `isDirty=false` 확인)
- **버그 B 후속 — 사용자가 "그래도 살짝 벗어나는데, 꺾이는 지점에서 바로 방향 전환은 안 되냐"고 재요청(시간 부족 우려 언급)**: 위 수정(속도 배율만큼 `acceleration`도 같이 올림)은 절반의 해결이었을 뿐, 근본적으로 `NavMeshAgent.acceleration` 자체가 낮으면(기본 8) 속도가 빠를수록 방향 전환에 필요한 거리가 늘어나는 구조라 완전히 없애려면 **가속도를 속도 배율에 비례가 아니라 아예 충분히 큰 고정값으로** 둬야 한다는 걸 실험으로 확인(60 이상이면 가속 스킬 최대 배율(2.5배)에서도 코너 이탈이 사실상 0). 다행히 스크립트 한 줄 수준의 수정이라 빠르게 처리 가능했음(사용자에게 "오래 걸리면 말해달라"는 요청을 받았으나 실제로는 금방 끝남)
  - **수정(`PlayerAutoMover.cs`)**: 속도 배율에 비례해서 `acceleration`을 같이 올리던 로직을 제거하고, 대신 `corneringAcceleration`(고정값 60)을 `Awake()`에서 한 번만 설정하도록 단순화(더 이상 배율과 무관하게 항상 높은 값 유지)
  - **검증**: 코너 최대 이탈이 기본 속도에서 0.75→**약 0.01유닛**(사실상 0), 가속 2배에서 1.45→**약 0.01유닛**, 가속 최대(2.5배)에서도 **약 0.01유닛**으로 전부 사실상 완전히 사라짐. 직선 구간 속도도 끊김 없이 유지되는 것 확인(회귀 없음). `Fox.prefab`에 `corneringAcceleration=60` 확정값 저장. 컴파일 에러 없음

### 버그 A/B 수정 직후 재발 — 스테이지1 벽 세우기 튜토리얼 또 막힘 (2026-09-16, 같은 날 세 번째 제보)

- **사용자 제보**: "1스테이지 벽 세우기 튜토리얼이 안 된다, 계속 똑같은 버그가 발생한다"
- **재현·원인**: 실제로 자연 진행시켜보니 `first_hit` 게이트가 열리는 순간 `reachedTileIndex`가 이미 정확히 3이라 `CanPlaceAt(3)`가 `False`가 되어 하드코딩된 "타일3 Left" 슬롯이 배치 불가능한 상태로 게이트가 열렸음 — **2026-09-10에 한 번 고쳤다가 "단순하게 고정값으로"라는 사용자 요청으로 되돌렸던 것과 완전히 같은 종류의 버그**가, 바로 위에서 한 코너링 가속도 수정으로 초반 타일 통과 타이밍이 미묘하게 빨라지면서 재발한 것. "고정 타일 번호"라는 설계 자체가 이동 타이밍을 조금만 바꿔도 계속 깨지는 구조적 문제라고 보고 → 사용자가 **근본 해결(2026-09-10에 만들었다가 되돌렸던 동적 계산 방식 재도입)**을 선택
- **수정(`TutorialManager.cs`)**: `TryFireStep()`에서 트리거가 `FirstHit`일 때만, 지금 이 순간 플레이어 위치 기준으로 `PathManager.CanPlaceAt`/`IsSlotOccupied`를 검사해 **아직 지나치지 않고 비어있는 가장 가까운 슬롯**을 다시 계산해 그 스텝의 `requireSlotClick` 페이지 `gateTileIndex`/`gateSide`를 덮어쓰는 `PatchDynamicGate`/`TryFindNextPlaceableSlot` 추가. `TutorialStep`/`TutorialPage`가 일반 C# 클래스라 이 변경은 런타임 인스턴스에만 적용되고 에디터에 저장된 원본 데이터(에셋)는 그대로 남음
- **검증(Play 모드)**:
  - 정상 진행: 게이트가 `reached=2`(아직 타일3 전)일 때 열려 `요구=(3 Left)`, `CanPlaceAt=True`로 정상 배치 가능 확인
  - **일부러 재현한 극단 케이스**: 플레이어를 타일4로 강제 이동(타일3을 이미 지나친 상태)시킨 뒤 `first_hit`을 직접 발동 → 예전이라면 여전히 "타일3"을 가리켜 막혔을 상황인데, 이번엔 동적 계산이 지나친 타일3을 건너뛰고 **타일5 Left**(다음으로 배치 가능한 가장 가까운 슬롯)를 정확히 찾아내는 것 확인
  - 컴파일 에러 없음
- ⚠️ **테스트 중 별개로 발견한 것(이번 버그와 무관, 참고만)**: 테스트 도중 에디터의 현재 열린 씬이 의도치 않게 `Stage_Template`로 바뀌어 있던 적이 있었음(정확한 원인 재현은 못 함 — MCP stop/play 사이 어딘가에서 발생한 것으로 추정, 실제 게임 동작과는 무관한 에디터 워크플로 이슈로 보임). 이 과정에서 `Stage_Template`의 `Floor` `NavMeshSurface`가 아직 `useGeometry=RenderMeshes`(스테이지1에서 고쳤던 것과 같은 설정)인 채로 한 번 구워졌다는 것도 같이 확인됨 — **템플릿으로 실제 스테이지를 만들기 전에 `PhysicsColliders`로 바꿔야 스테이지1과 같은 "클리어 안 됨" 버그를 안 물려받음**(아직 실사용 전이라 지금 당장 고치지는 않음, 다음에 템플릿으로 실제 스테이지 작업할 때 처리)

### 스테이지2 경로 연장(사용자가 직접 타일20~25 수동 배치) 후속 작업 일체 (2026-09-16)

- **배경**: 사용자가 스테이지2를 기획 의도에 맞춰 직접 재배치하는 중, `Path/Tiles` 아래에 `Tile_20`~`Tile_25`를 손으로 추가(타일19(12,8)에서 북쪽(+Z)으로 새 코너를 꺾어 연장). 간격이 손으로 만들어 정확하지 않다며 정리 요청
- **처리 내역**(전부 스테이지2에만 적용, 스테이지1은 무관):
  1. **타일 좌표 스냅**: `Tile_20`~`Tile_25`를 타일19 기준 X=12 고정, Z=10/12/14/16/18/20의 정확한 2유닛 격자로 스냅
  2. **길 바닥 비주얼**: `PathTile_20`~`PathTile_25` 신규 생성(Y=0.02, `M_PathTile`, 기존 타일과 동일 규격)
  3. **벽 슬롯 + 받침대**: 새 타일 6개에 좌/우 슬롯 12개 + 받침대 12개 신규 생성. **코너(타일19→20) 보정**: 기존 타일19의 Left 슬롯은 원래 "막다른 길" 전제로 만들어진 값이라, 이번에 새로 꺾이는 코너의 안쪽(진행 방향이 왼쪽으로 꺾이는 코너라 안쪽은 Left)과 겹쳐버림 — 기존 코너(타일9→10, 오른쪽으로 꺾임)에서 실제 적용된 패턴을 역산해 일반화한 규칙(안쪽 슬롯은 "나가는 방향 기준 바깥쪽" 공식으로 대체, 이번엔 왼쪽으로 꺾이므로 나가는 방향(+Z)의 Right 공식 적용)대로 `(13, 0.75, 8)` 회전 `(0,0,0)`으로 재배치. 대응 받침대도 같이 이동
  4. **도착 포탈**: 새 마지막 타일(25번, (12,0,20))로 이동
  5. **바닥·안개 확장**: `Floor`/`FogGround`를 동쪽으로 확장(폭 30→35, 오늘 스테이지1에서 고쳤던 것과 완전히 같은 문제 — 서·남·북은 이미 여유 충분, 동쪽만 3유닛으로 타이트했던 것. 스테이지2도 좌표 구조가 스테이지1과 같아서 미리 예상했던 대로 같은 문제였음)
  6. **지형 굴곡 메시(`M_GroundBumps_Stage2`) 재생성**: 전용 생성 스크립트가 따로 없어 코드로 새로 절차 생성(2유닛 격자, flat shading, 경로 전체 타일 2.2유닛·몬스터 2.4유닛 반경 평탄, Perlin 노이즈로 자연스러운 굴곡, 새로 넓어진 바닥 범위 전체를 덮도록). **파일 삭제 금지 규칙 때문에 기존 에셋 파일을 지우지 않고 같은 파일의 메시 내용만 교체**
  7. **NavMesh**: `Floor`의 `NavMeshSurface.useGeometry`를 `RenderMeshes`→**`PhysicsColliders`**로 변경 후 재빌드(스테이지1에서 겪은 "장식 굴곡 메시 높이에 구워져서 클리어 판정이 안 되는" 버그를 스테이지2가 물려받기 전에 미리 예방 — 지난 세션에 남겨둔 참고 메모대로 처리)
- **검증(Play 모드)**: 몬스터 비활성화 + 무적 처리 후 재생 → 플레이어가 새 코너를 포함해 끝까지 자연스럽게 이동, **새 도착 지점에서 정상적으로 GAME CLEAR 확인**(`isFinished=True`, 결과 화면 정상 표시, 클리어 시간 20.2초). 경로 전체 NavMesh 높이 재확인(전부 Y≈0.02, 평탄). 씬 뷰 스크린샷으로 새 구간 벽 슬롯·바닥·데코 배치 확인. 컴파일 에러 없음. `Stage2.unity` 저장 완료
- **참고로 남겨둔 사항**:
  - 함정(`RockfallTrap_Tile9`/`Tile11`, `SpikeTrap_06_07`)은 전부 새 구간과 멀리 떨어져 있어 그대로 둠(영향 없음)
  - 몬스터 4마리 위치가 이전에 기록해둔 좌표와 달라져 있었음 — 사용자가 재배치 작업을 이미 진행 중이라는 뜻이라, 지형 굴곡 계산 등에는 **지금 씬에 저장된 실제 좌표**를 그대로 사용함(옛 기록 무시)

### 사용자 질문 — 함정 옮기는 법 / 앞으로 스테이지 배경 다양화 / 템플릿에서 타일을 더 쉽게 추가하는 방법 (2026-09-16, 같은 대화)

- **함정 이동 방법 안내**: 낙석(`RockfallTrap`)은 Transform 위치가 곧 판정 중심이라 그냥 드래그. 가시(`SpikeTrap`)는 Transform이 아니라 인스펙터의 `Tile Index A`/`Tile Index B`(정수, 연속된 두 타일)가 실제 판정·비주얼을 전부 결정한다고 안내함(Transform은 하이어라키에서 보기 편하라고 대략 맞춰둔 것뿐)
- ⚠️ **사용자 지시(항상 지킬 것)**: "앞으로 다른 스테이지들 배경을 꾸밀 때 매번 똑같은 형태로 하지 말고 스테이지마다 다른 느낌으로 만들어야 한다" — 데코 프리팹 종류·배치 패턴·지형 굴곡 느낌 등을 스테이지마다 의도적으로 다르게 가져갈 것. 개인 메모리에도 별도 기록해둠(세션이 바뀌어도 유지되도록)
- **타일을 더 쉽고 빠르게 배치하는 방법에 대한 질문 → 절충안으로 구현 완료**: 처음 제안한 "Scene 뷰 클릭 페인팅" 방식은 규모가 크다고 판단해, **방향 버튼 4개 + 되돌리기 버튼짜리 가벼운 도구**로 절충함
  - **신규**: `Assets/Scripts/Editor/PathManagerEditor.cs` — `PathManager`의 커스텀 인스펙터. `Path` 오브젝트를 선택하면 기본 필드 아래에 ↑↓←→ 버튼 4개(각각 "지금 경로의 마지막 타일에서 그 방향으로 정확히 2유닛 이어붙인 새 타일 생성", 이름도 `Tile_XX`로 자동 이어짐)와 "마지막 타일 되돌리기" 버튼이 나타남. 방향이 4개뿐이라 가로·세로만 되고 대각선은 구조적으로 불가능(배치 규칙을 도구 자체가 강제)
  - 특정 씬 전용 도구가 아니라 **`PathManager`가 있는 모든 씬(Stage_Template에서 복제한 새 스테이지 포함)의 인스펙터에 자동으로 나타남** — 별도 설정 없이 바로 사용 가능
  - `Assets/Scripts/Editor/` 폴더에 위치(Editor 전용 코드라 빌드에는 포함 안 됨). 컴파일 에러 없음
  - ⚠️ **실사용 클릭 테스트는 아직 못 함**: 마침 사용자가 스테이지2를 직접 편집 중이라(모리터 5마리로 늘어나 있고 함정도 1개 줄어든 것을 확인 — 저장 안 된 변경사항 있음) 스테이지 전환 자체가 막혀서, 실제로 버튼을 눌러보는 검증은 다음에 사용자가 직접 확인해줘야 함. 코드 로직 자체(마지막 타일 좌표에 방향 오프셋을 더해 새 오브젝트 생성)는 검토 완료했고 단순해서 문제 가능성은 낮음

- **🐛 버그 수정 — 재도전 후 스킬이 활성화되지 않음**: 원인은 `StageManager.RetryStage()`에서 `OnRetried` 이벤트를 맨 앞(체력 회복 등 다른 초기화보다 먼저)에서 발행했던 것. `SkillManager.HandleRetry()`가 이 이벤트를 받아 UI를 즉시 다시 그리는데, 그 시점엔 아직 `PlayerHealth.ResetHealth()`가 호출되기 전이라 `IsDead=true`인 채로 `CanUse()`가 평가되어 버튼이 계속 비활성으로 그려졌음(실제 `CanUse()` 값 자체는 정상이었지만, UI가 그 이후 다시 갱신될 계기가 없어서 그대로 굳어버림). **`OnRetried` 발행 위치를 `RetryStage()` 맨 마지막(체력 회복 등 모든 초기화 이후)으로 이동**해 해결. Play 모드에서 사망→재도전 재현 후 4개 스킬 전부 `CanUse=true`로 돌아오고 버튼도 정상 밝기로 표시되는 것 확인
- **조명탄 조준 범위 미리보기**: 조준 중(`awaitingFlareTarget`) 매 프레임 마우스가 가리키는 바닥 지점에 반경 5 크기의 얇은 링을 그리도록 `SkillManager`에 추가(`Custom/RangeIndicatorOverlay` 셰이더 재사용, 몬스터 사거리 원과 같은 방식이지만 색은 골드로 구분). 확정 시/재도전 시 자동으로 숨김
- **스킬 이펙트를 사용자가 새로 추가한 `Particlecollection_Free samples` 에셋으로 교체**:
  - 무적: 밋밋한 반투명 구체 → `Magic_ring/magic_ring_02` 파티클(마법진)을 캐릭터 발밑에 추가(기존 구체는 렌더러만 꺼서 유지 — 구조는 그대로라 재도전 등 기존 코드 변경 없음)
  - 가속 시작 / 정지 시작 / 조명탄 착지 시점에 각각 `Sprays_02` / `Hit_02` / `Hit_05` 파티클을 한 번 재생하는 `SkillManager.SpawnBurst()` 추가
  - ⚠️ **에셋 호환 문제 2가지 발견·수정** (이 에셋 팩을 앞으로 더 쓸 때도 적용해야 할 내용):
    1. 이 팩의 머티리얼 14개가 전부 **Built-in 렌더 파이프라인 전용 레거시 파티클 셰이더**(`Legacy Shaders/Particles/Additive`, `Alpha Blended` 등)를 쓰고 있어 URP에서 아무것도 안 보였음 — 기존에 알던 "분홍색으로 깨짐" 패턴과 달리 이번엔 완전히 안 보이는 형태였음. 전부 `Universal Render Pipeline/Particles/Unlit`로 일괄 변환하는 스크립트를 짜서 팩 전체(14개 머티리얼)를 한 번에 고침(`_MainTex`→`_BaseMap`, `_TintColor`→`_BaseColor`, Additive/AlphaBlended 구분해서 `_Blend`/`_SrcBlend`/`_DstBlend` 매핑)
    2. 셰이더를 고쳐도 여전히 안 보여서 더 파보니, 이 팩의 파티클들이 **렌더 정렬(Renderer.alignment) = Local**로 돼 있었음 — 일반적인(카메라가 옆에서 보는) 게임을 기준으로 만들어져 있어서, 이 게임의 거의 수직인 탑다운 카메라에서는 파티클이 옆으로 누워 화면에 거의 안 잡히는 각도가 됨. `View`(항상 카메라를 향함)로 강제하니 바로 보임 — `SkillManager.SpawnBurst()`와 무적 마법진 양쪽에 이 보정을 적용함. **이 팩의 다른 이펙트를 나중에 더 쓸 때도 이 두 가지(셰이더 변환 + View 정렬)를 항상 확인할 것**
  - **검증**: Play 모드에서 4개 효과 전부 재생해 스크린샷으로 확인 — 무적은 은은한 흰빛 마법진 글로우, 가속/정지/조명탄은 짧고 작은 먼지·섬광 버스트로 전부 과하지 않게 나옴. 재도전 시에도 정상 재생됨
  - ⚠️ **위 검증은 이후 부정확한 것으로 확인됨** — 사용자가 실제로 보고 "위치도 이상하고 잘 안 보인다"고 재차 피드백을 줌. 아래 "무적/버스트 이펙트 2차 조정" 절 참조

### 무적/버스트 이펙트 2차 조정 (2026-09-15, 같은 날 두 번째 피드백)

- **무적 — 사용자 요청대로 원복+장식**: "기존 캐릭터를 감싸는 느낌은 유지하고 이펙트만 꾸며달라"는 요청에 따라, 바닥에 깔리는 마법진(`MagicRingFX`, 애초에 "감싸는" 느낌이 아니라 바닥 룬 형태라 요청과 안 맞았음)을 다시 비활성화하고 **기존 반투명 구체(`InvincibleShield`)를 다시 주 시각 요소로 복원**. 대신 구체를 더 "무적답게" 꾸미기 위해:
  - `M_SkillShield.mat` 색을 더 밝고 채도 높은 하늘색+알파 상향(0.35→0.5)으로 조정
  - `SkillManager.RunInvincible()`에 은은한 펄스(사인파로 살짝 커졌다 작아짐, `invinciblePulseSpeed`/`invinciblePulseAmount`로 조절) 추가 — "보호막이 숨쉬는" 느낌
  - 재도전 등 중간 강제 종료 시 스케일 드리프트 안 남게 `invincibleShieldBaseScale`(고정값) 기준으로 복원하도록 정리
  - **검증**: Play 모드 스크린샷으로 확인 — 밝은 하늘색 구체가 캐릭터를 확실히 감싸는 모습 재확인. 펄스 애니메이션 자체는 스크린샷 한 장으로는 확인 불가(로직 검토로만 확인)
- **버스트(가속/정지/조명탄 착지) — 위치·크기 조정, 그러나 가시성은 미해결**:
  - 발밑(y=0)이 아니라 몸통 높이(`burstHeight`=0.8)에서 재생하도록, 회전은 프리팹 고유값 대신 `Quaternion.identity`로 고정, 크기 배율(`effectScale`)을 0.6→0.3으로 축소
  - ⚠️ **여전히 스크린샷에서 명확히 확인 못함**: `Sprays_02`/`Hit_02`/`Hit_05` 버스트는 셰이더·정렬·위치·활성 상태·파티클 개수까지 전부 정상으로 확인되는데도(코드로 직접 확인) 실제 스크린샷에는 거의 안 보임 — 아마 이 프리팹들의 기본 색이 회색 계열(예: `Sprays_02` 머티리얼은 `RGBA(0.5,0.5,0.5,0.5)`)이라 초록 바닥 위에서 원래도 옅게 나오도록 만들어진 효과일 가능성이 큼. 무적 마법진처럼 "완전히 안 보이던" 것과는 다른 종류의 문제로 보임(그때는 정렬을 고치자 확실히 보였는데, 이번엔 정렬을 포함해 다 맞는데도 옅음)
  - **미해결 상태로 남겨둠** — 더 밝은 색의 다른 프리팹으로 교체하거나 색을 직접 덧입히는 등 추가 작업이 필요할 수 있음. 다음 세션 또는 사용자 확인 후 계속할 것

### 3차 피드백 — 무적 이펙트 재조정 + 버스트 결국 제거 (2026-09-15, 같은 날 세 번째 피드백)

- **🐛 사고 발견·수정 — 스테이지 시작부터 무적 이펙트가 떠 있던 버그**: 사용자가 "시작하자마자 이펙트가 활성화돼 있다"고 지적. 원인은 내가 앞선 라운드에서 `InvincibleShield`를 씬 편집 중 여러 번 켰다 껐다 하다가 **마지막에 끄는 걸 깜빡하고 활성 상태로 저장**해버린 단순 실수였음. 즉시 비활성으로 되돌리고 저장. **이런 종류의 "편집 중 임시로 켠 오브젝트를 끄는 걸 잊는" 실수를 줄이려면, 씬을 저장하기 직전에 한 번 더 해당 오브젝트들의 활성 상태를 확인하는 습관이 필요함(다음부터 유의)**
- **무적 — "감싸는 느낌 유지 + 효과답게 꾸미기" 요청 재작업**: 사용자가 재확인 결과 "그냥 파란 원이 가리는 형태지 이펙트 느낌이 아니다"라고 재지적. 근본 원인은 **`M_SkillShield.mat`이 처음 만들 때 `_Surface`를 Transparent로 설정하지 않아 `_Surface=0`(Opaque)으로 생성돼 있었던 것** — 알파값을 아무리 낮춰도 완전 불투명하게 렌더링되고 있었음(`manage_material create`로 색만 지정하고 만들면 기본이 Opaque라는 걸 놓침). `_Surface=1`+`_Blend`/`_SrcBlend`/`_DstBlend`/`_ZWrite`를 알파 블렌드에 맞게 설정하고 렌더큐를 Transparent(3000)로 바꾸자 그제서야 캐릭터가 비치는 반투명 보호막으로 제대로 렌더링됨. 여기에 더해 **발밑에 도는 링(`InvincibleRing`, 신규 `LineRenderer`+`Custom/RangeIndicatorOverlay` 셰이더, 초당 90도 회전)을 추가**해 "지금 효과가 활성 중"이라는 것을 명확히 드러냄 — 파티클 에셋 대신 이미 검증된 셰이더 방식이라 안정적으로 보임. 스크린샷으로 캐릭터가 비쳐 보이는 반투명 보호막 + 도는 링 확인 완료
- **버스트(가속/정지/조명탄 착지) — 결국 제거함**: 다른 프리팹(`Sprays_04`/`Hit_04`/`Hit_06`)으로 교체 + 인스턴스 전용 밝기 보정(머티리얼을 2배 밝게, 알파 강제 1) 코드까지 추가했으나 여전히 화면에 안 보임. 직접 파티클 데이터를 찍어보니 **`startSize`가 0.006~0.03 수준으로 원본 자체가 극도로 작게 만들어진 효과였음** — `effectScale`을 3.5까지 올려도(원래 대비 10배 이상) 여전히 안 보여서, 이 시리즈의 파티클들은 이 프로젝트 카메라 거리에서 쓰기에 근본적으로 안 맞는 것으로 판단. **`accelerateBurstPrefab`/`stopBurstPrefab`/`flareBurstPrefab`을 전부 비워서(null) 버스트 재생 자체를 중단함** — `SkillManager.SpawnBurst()`는 프리팹이 null이면 그냥 아무것도 안 하고 넘어가므로 코드 변경 없이 안전하게 비활성화됨. 가속(트레일)·정지(일시정지 아이콘)·조명탄(착지 지점 빛)은 이 버스트와 무관하게 이미 각자 확인된 시각 효과가 있어 실질적인 손실은 없음
- **검증**: Play 모드에서 스테이지 시작 직후(이펙트 없음 확인) → 무적 재생(반투명 보호막+도는 링, 캐릭터 잘 보임) 스크린샷으로 확인. 콘솔 에러 없음

### 스킬 시각 연출 전면 초기화 (2026-09-15, 같은 날 네 번째 피드백)

- **사용자 결정**: 위 3차 조정까지 마쳤음에도 사용자가 "스킬 이펙트 부분은 처음부터 다시 하자, 기획서+참고 C# 코드를 따로 줄 테니 이펙트 작업한 건 전부 없애달라"고 요청. 삭제 범위를 확인한 결과 **스킬 로직(횟수/지속시간/속도 배율/무적 플래그/정지/조명탄 조준+시야 등록)은 그대로 두고, 시각 연출만 전부 제거**하는 것으로 확정
- **씬에서 삭제한 것**: `Player/InvincibleShield`(반투명 보호막 구체 + 그 안의 `MagicRingFX`·`InvincibleRing` 전부 자식으로 함께 삭제됨), `Player/PauseIcon`(정지 아이콘 스프라이트), `Player`의 `TrailRenderer` 컴포넌트(가속 트레일)
- **`SkillManager.cs` 전면 정리**: 아래 항목을 전부 제거하고 순수 게임 로직만 남김
  - 필드: `invincibleShieldVisual`/`invincibleRing`/`pauseIconVisual`/`accelerateTrail`, 펄스·깜빡임·링 관련 수치, 버스트 프리팹 3개·`effectScale`·`burstLifetime`·`burstHeight`, 조명탄 조준 미리보기(`flarePreviewMaterial` 등)
  - 메서드: `SpawnBurst`/`FixParticleAlignment`/`ShowFlarePreview`/`HideFlarePreview`/`DrawInvincibleRing`/`EndEffectVisuals`
  - `RunAccelerate`/`RunInvincible`/`RunStop`/`RunFlare`는 각각 **로직 한 줄(속도 배율 on/off, 무적 플래그 on/off, 정지 on/off, 시야 소스 등록/해제)만 남기고 시각 관련 코드는 전부 삭제**. 조명탄은 착지 지점에 빈 오브젝트(`FlareVisionSource`, 컴포넌트 없음)만 만들어 `VisionSourceRegistry` 등록용 트랜스폼으로만 씀(빛 없음)
- **남겨둔 것(에셋 파일)**: `M_SkillTrail`/`M_SkillShield`/`M_InvincibleRing`/`M_FlarePreviewRing` 머티리얼, `Particlecollection_Free samples` 팩의 셰이더 URP 변환 결과물 — 프로젝트 규칙상 파일은 삭제하지 않으므로 남아있음(현재 아무 데도 참조 안 됨, 필요 없어지면 사용자가 직접 정리하거나 삭제 지시할 것). 셰이더 변환 자체(Legacy Particles → URP Unlit)는 이 에셋 팩을 다시 쓰게 되면 여전히 유효한 사전 작업이라 되돌리지 않음
- **검증**: `Player`가 컴포넌트 5개(Transform/Capsule/NavMeshAgent/PlayerAutoMover/PlayerHealth)로 정확히 E0 완료 시점 상태로 돌아온 것 확인. Play 모드에서 가속+무적 동시 사용 시 로직(속도 7.2, 무적 플래그 true)은 정상 동작하되 화면에는 아무 시각 효과도 없는 것 스크린샷으로 확인. 콘솔 에러 없음
- **다음 세션**: 사용자가 줄 기획서(md)+참고 C# 코드를 받아서 스킬 시각 연출을 새로 설계·구현할 것

### 스킬 시각 연출 재구현 — `docs/skill-vfx.md` 스펙 + 사용자 제공 참고 코드로 완료 (2026-09-15, 같은 날 다섯 번째 작업)

- **사용자가 제공한 것**: `docs/skill-vfx.md`(4개 스킬 전용 상세 VFX 스펙 — 공통 규칙/렌더큐/색상표/스킬별 수치/연결 방법/검증 체크리스트/트러블슈팅 가이드)과 `Assets/Scripts/SkillVfxLibrary.cs`/`SkillVfxController.cs`/`FlareVfx.cs`(전부 순수 코드로 절차적 생성, 외부 에셋 불필요) — 위치는 문서가 말한 `Assets/Scripts/VFX/`가 아니라 `Assets/Scripts/` 바로 밑이지만 동작에는 무해해서 그대로 둠
- **`PlayerHealth.cs` 수정**: `OnBlockedByInvincibility` 이벤트 추가, `TakeDamage`가 무적 중 피해를 막을 때 이 이벤트를 발행하도록 변경(기존엔 그냥 `return`만 함) — 스킬 이펙트가 "피격을 막았다"는 순간을 알 수 있게 함
- **`SkillManager.cs` 재작성**: 순수 로직만 남아있던 상태에 다음을 추가
  - `[SerializeField] SkillVfxController vfx`, `[SerializeField] FlareVfx.Settings flareVfxSettings` 필드 추가
  - `RunAccelerate`/`RunInvincible`/`RunStop`이 기존 로직 직후 각각 `vfx.PlayDash`/`PlayShield`/`PlayFreeze` 호출(지속시간을 그대로 전달 — `SkillVfxController`가 자체 타이머로 알아서 끔)
  - 조명탄 흐름을 `LaunchFlare(index, targetPosition)`로 분리 — 클릭 확정 즉시 `FlareVfx.Launch(from, to, flareVfxSettings, onLanded, onFinished)`를 호출하고, **UI 게이지용 `activeDuration`/`activeEndTime`과 `VisionSourceRegistry` 등록은 `onLanded`(착지 시점)에서**, 해제는 `onFinished`(밝힘 종료 시점)에서 처리 — 발사~착지 비행 시간(0.5초) 동안은 아직 안 밝혀진 상태이므로 게이지가 조준 시점부터 도는 걸 방지
  - `playerHealth.OnBlockedByInvincibility` 구독 → `vfx.PulseShieldHit()` 호출(보호막 피격 시 잠깐 번쩍임)
  - `HandleRetry`/`HandleResultShown`(결과 화면 표시 시에도 즉시 정지되도록 신규 추가) 공용으로 쓰는 `StopAllSkillEffects()` 헬퍼 신설 — 게임 로직 상태 초기화(속도 배율/무적/정지/조준 취소/시야 소스 해제) + `vfx.StopAll()` + `FlareVfx.ClearAll()`을 한 번에 처리
- **씬 반영**: `Assets/Materials/VFX/M_VFX_Particle.mat`(URP `Particles/Unlit`) 신규 생성 후 `Player`에 `SkillVfxController` 컴포넌트 추가(`characterHeight=1.8`=Fox 캡슐 콜라이더 높이와 일치, `particleMaterialTemplate`에 위 머티리얼 연결) → `Managers`의 `SkillManager.vfx`와 `flareVfxSettings.particleMaterialTemplate`도 각각 연결
- **검증(Play 모드)**: 컴파일 에러 없음. 에디터가 OS 포커스를 잃으면 `Time.frameCount`가 멈추는 고질적 문제가 이번에도 발생했으나, `UnityEditor.EditorApplication.Step()`을 직접 호출해 포커스 여부와 무관하게 프레임을 한 장씩 강제로 진행시키는 방법으로 우회함(이번 세션에서 새로 찾은 방법 — 앞으로도 이 문제가 재발하면 이 방법을 우선 시도할 것)
  - 가속: 실제로 캐릭터를 이동시키며 확인 — 발밑에 은은한 흙먼지 파티클과 옅은 잔상 트레일이 보임(너무 화려하지 않음, 스크린샷 확인)
  - 무적: 보호막이 캐릭터를 반투명하게 감싸는 은은한 하늘색 빛으로 렌더링됨(사용자가 3차 피드백에서 지적했던 "그냥 불투명한 파란 원" 문제가 완전히 해소됨 — 캐릭터가 안에서 또렷이 비쳐 보임). `TakeDamage` 중 무적으로 피해가 막히면 `OnBlockedByInvincibility`→`PulseShieldHit()`가 예외 없이 호출되는 것도 확인
  - 정지: 발밑에 12시 방향에서 시계 방향으로 줄어드는 원형 게이지 + 머리 위에 회전하지 않는 일시정지 아이콘(두 개의 바) 확인
  - 조명탄: `LaunchFlare`를 리플렉션으로 직접 호출 → 포물선 비행(빛나는 구체+트레일)이 스크린샷으로 확인됨 → 착지 후 따뜻한 색의 바닥 빛+테두리 링+떠오르는 불씨가 있는 "밝힘" 연출이 안개 경계와 함께 스크린샷으로 확인됨(원 안쪽은 밝고 원 밖은 여전히 회색 안개)
  - 재도전: `StageManager.RetryStage()` 호출 → `StopAllSkillEffects()`가 예외 없이 실행되고 이펙트가 즉시 정리되는 것 확인
  - 콘솔 에러 없음(무관한 `NoSubscription`/`NavMeshAgent` 경고만 존재)
- **게이트 통과 여부**: `docs/skill-vfx.md` 5번 섹션 체크리스트 중 핵심 항목(트레일+먼지 가시성/보호막 감싸는 느낌+피격 펄스/게이지 시계방향 감소+아이콘 비회전/조명탄 포물선→착지→밝힘→소멸) 전부 스크린샷으로 확인 완료. 튜토리얼 정지(timeScale=0) 중 이펙트가 함께 멈추는지, 안개에 가려지지 않는지(렌더큐 3100)는 코드 검토로는 충족하나 실측(실시간 재생)은 이 세션의 포커스 문제로 미완 — 필요하면 사용자가 에디터에서 직접 확인
- **다음 세션**: 스킬 시각 연출 작업 완료 — `feature-expansion.md`의 **E3(함정: 낙석+가시함정)**로 이어갈 것

### 스킬 시각 연출 후속 피드백 — 가속 이펙트 보강 + 조명탄 조준 미리보기 복원 (2026-09-15, 같은 날 여섯 번째 작업)

- **사용자 피드백**: (1) 가속 스킬 파티클이 너무 안 보여서 "빨라졌다"는 느낌이 안 남 → 가진 이펙트 에셋을 써도 되고, 지금 방식으로 충분하면 안 써도 된다고 확답. (2) 스킬 이펙트 전면 초기화 때 조명탄 조준 중 마우스 범위 미리보기(1차 피드백 때 만들었던 것)가 같이 삭제됐으니 복원 요청
- **가속 이펙트 보강 — 에셋 없이 절차적 파티클로 해결**: `Particlecollection_Free samples` 팩을 확인했으나(Line/Sprays/Hit 계열), 이전에 이미 이 팩의 파티클들이 이 프로젝트 카메라 거리에서 근본적으로 너무 작게 만들어졌다는 걸 확인한 전례가 있어(3차 피드백 기록 참고) 새로 에셋을 붙이는 대신 `SkillVfxController.cs`에 **"몸 옆을 스쳐 지나가는 잔상 줄무늬"(`Dash_Streaks`)**를 새로 추가함
  - `ParticleSystemRenderMode.Stretch`(스트레치드 빌보드) 사용 — 파티클의 실제 속도에 비례해 저절로 길게 늘어나므로 속도감이 직관적으로 드러남
  - ⚠️ **시행착오**: 처음엔 `velocityOverLifetime`으로 로컬 -Z 방향 속도를 부여했는데, `Particle.velocity`가 이 값을 반영하지 않아(스크립트로 읽을 수 있는 `velocity`는 초기 속도만 포함, velocity-over-lifetime 등 후처리 모듈은 별도 내부 계산이라 API로 안 드러남) Stretch 렌더링이 길이를 못 잡고 거의 안 보였음 — `shape.rotation=(0,180,0)`으로 박스 샤프의 기본 발사 방향(+Z) 자체를 뒤집고 `main.startSpeed`를 직접 줘서(7~11) 초기 속도 자체에 후방 성분을 실었더니 `Particle.velocity`에도 제대로 반영되고 렌더링도 확실하게 늘어남
  - 캐릭터를 실제로 이동시키며 스크린샷으로 확인 — 밝은 크림색 줄무늬가 캐릭터 옆으로 뚜렷하게 뻗어나가며 "빠르게 스쳐감"이 잘 느껴짐(과하지 않은 선에서)
- **조명탄 조준 미리보기 복원**: `SkillManager.cs`의 `HandleFlareTargeting()`을 "클릭 시에만 레이캐스트"에서 "매 프레임 레이캐스트 + 마우스 위치로 미리보기 갱신, 클릭 시 확정"으로 변경. `EnsureFlareAimPreview`/`UpdateFlareAimPreview`/`ClearFlareAimPreview`를 신설해 `SkillVfxLibrary`(채움 원+테두리 링, `flareVfxSettings.glowColor`/`groundDiscHeight`와 동일한 색·높이 사용 — 실제 조명탄 이펙트와 톤이 이어지도록)로 조준 중에만 존재하는 미리보기 오브젝트를 생성·이동·정리함. `StopAllSkillEffects()`에도 정리 호출 추가(재도전/결과화면 중 조준 중이었으면 같이 사라짐)
  - 이전(1차 피드백 때) 구현과 달리 별도 머티리얼 에셋(`M_FlarePreviewRing.mat`, 지금은 안 쓰이는 채로 남아있음)을 쓰지 않고 `SkillVfxLibrary`의 절차적 텍스처/머티리얼 생성 방식을 그대로 재사용함 — 이번에 새로 만든 시스템과 일관성 유지
  - 검증: `TryUseSkill(3)`으로 조준 모드 진입 후 `UpdateFlareAimPreview`를 직접 호출해 임의 좌표로 이동시켜 스크린샷 확인 — 해당 좌표에 조명탄 반경만큼의 채움+테두리 원이 정상적으로 나타남
- **검증 중 발견한 것(코드 문제 아님)**: `EditorApplication.Step()`으로 프레임을 강제 진행시키는 이번 세션의 우회 방법을 오래 반복하면 Unity 콘솔에 `JobTempAlloc has allocations that are more than the maximum lifespan of 4 frames old` 경고가 뜨는데, 스킬을 전혀 안 쓴 상태에서도 동일하게 발생하는 걸 확인함 — 이번에 추가한 코드와는 무관하고, 비정상적으로 빠르게 프레임을 스텝하는 이 테스트 기법 자체의 부작용으로 판단(실제 플레이에서는 발생하지 않을 것으로 예상)
- **게이트 통과 여부**: 가속 이펙트 가시성 개선 ✅(스크린샷 확인) / 조명탄 조준 미리보기 복원 ✅(스크린샷 확인) / 컴파일·콘솔 에러 없음 ✅

### 스킬 버튼 UI 다듬기 + 무적 피격 완전 무시 (2026-09-15, 같은 날 일곱 번째 작업)

- **사용자 피드백**: (1) 스킬 버튼 좌상단 단축키 숫자는 불필요한 정보라 삭제, (2) 우하단 남은 횟수 숫자를 아이콘 그림 아래로 옮기고 배경과 대비되는 어두운 색으로, (3) 버튼 4개 간격이 너무 붙어있으니 더 벌릴 것, (4) 무적 스킬 사용 중에는 피격 자체를 처리하지 말 것(데미지 숫자도 안 뜨고 그냥 벽에 막히는 느낌으로)
- **버튼 4개(`SkillButton_1~4`) 씬 수정** — 코드 변경 없이 전부 씬/컴포넌트 값만 조정:
  - `Hotkey` 오브젝트(단축키 숫자) 4개 전부 비활성화(`SetActive(false)`) — 파일이나 컴포넌트를 지우진 않고 숨기기만 함(프로젝트 규칙상 삭제 금지 대상은 아니지만 되돌리기 쉬운 쪽을 택함)
  - `Count`(남은 횟수) RectTransform을 앵커/피벗 중앙(0.5,0.5)으로 바꾸고 `anchoredPosition=(0,-47)`로 이동 — 아이콘(72x72, 중심 y=6)의 바로 아래 빈 공간에 위치. 텍스트 색을 아이콘 틴트(`SkillButtonUI.normalColor`)와 같은 계열의 어두운 갈색(`#382919`)으로, 정렬을 중앙으로, 볼드로 변경해 흰 배경과 대비되게 함
  - `SkillBar`의 `HorizontalLayoutGroup.spacing`을 24 → 44로 늘려 버튼 사이 간격 확보
  - 검증: Play 모드 스크린샷으로 단축키 숫자 없음/횟수 숫자가 아이콘 밑에 진한 색으로 표시됨/버튼 간격이 눈에 띄게 넓어짐 확인
- **무적 중 피격 완전 무시**: 근본 원인은 `Projectile.cs`가 `damageable?.TakeDamage(damage)` 호출 결과와 무관하게 항상 `SpawnDamageNumber()`를 호출하고 있었던 것(`PlayerHealth.TakeDamage`는 무적 중이면 내부적으로 피해를 막고 있었지만, "막았다"는 사실을 호출부에 알려줄 방법이 없었음)
  - `IDamageable.TakeDamage`의 반환 타입을 `void` → `bool`로 변경(무적/사망 등으로 막히면 `false`)하고 `PlayerHealth.TakeDamage`가 이를 구현하도록 수정. `IDamageable`의 유일한 실제 구현체가 `PlayerHealth`뿐이라(`MonsterTower`는 `IDamageable`을 구현하는 쪽이 아니라 사용하는 쪽) 영향 범위가 작음을 확인 후 진행
  - `Projectile.HandleHit()`이 이 반환값을 보고 `true`일 때만 로그+`SpawnDamageNumber()`를 호출하고, `false`면 벽에 막힌 것과 동일하게 조용히 소멸(로그만 "막힘"으로 남김)하도록 변경
  - 검증: `PlayerHealth.SetInvincible(true)` 상태에서 `TakeDamage(20)` 호출 → 반환값 `false`, 체력 100 유지 확인 / 무적 해제 후 `TakeDamage(20)` → 반환값 `true`, 체력 80으로 정상 감소 확인. 컴파일 에러 없음
- **게이트 통과 여부**: 단축키 숫자 제거 ✅ / 횟수 위치·색상 변경 ✅ / 버튼 간격 확대 ✅ / 무적 중 피격 무시(숫자·체력 변화 없음) ✅

### 몬스터 공격 범위 표시 다듬기 (2026-09-15, 같은 날 여덟 번째 작업)

- **사용자 피드백**: 몬스터 공격 범위를 보여주는 원이 너무 "짜쳐"(싸구려로) 보임 → 프로젝트 루트에 `MonsterRange_ref.png`(레퍼런스 이미지: 밝은 코어 + 은은한 글로우가 겹친 네온풍 링, "테두리만" 옵션이 최종 선택안으로 표시됨)를 첨부하고 이걸 참고해서 다듬어달라고 요청. 또한 범위가 보여지는 시간(`displayDuration`)이 너무 길게 느껴진다고 함
- **원인**: 기존 `AttackRangeIndicator`는 `LineRenderer` 하나에 `Custom/RangeIndicatorOverlay`(단색 반환) 셰이더 + 폭 0.15의 얇은 단색 선 하나뿐이라 글로우 없이 밋밋했음
- **수정**:
  - `AttackRangeIndicator.cs`에 코드로 두 번째 링(`RangeIndicator_Core`, 런타임 생성 자식 오브젝트)을 추가 — 기존 링(`M_RangeIndicator.mat`, 씬 에셋)은 **넓고 옅은 바깥 글로우**로 유지하고, 새 코어 링은 **얇고 밝은 HDR 색**(`(3, 0.65, 0.25, 0.95)`, 알파값 1을 넘는 RGB로 URP 블룸이 번지게 함)의 런타임 머티리얼(같은 셰이더 재사용)로 만들어 안쪽에 겹침 — 씬의 프로젝트가 이미 Bloom(threshold 1, intensity 0.25)을 켜두고 있어서 셰이더 수정 없이 색상값만으로 네온 느낌을 만들 수 있었음
  - 두 링 다 `revealed`/`displayDuration` 로직에 맞춰 함께 켜지고 꺼지도록 `LateUpdate`/`ResetIndicator` 수정
  - `M_RangeIndicator.mat`(바깥 글로우, 6개 몬스터 전부 공유하는 에셋 하나)의 `_BaseColor`를 `(1, 0.3, 0.1, 0.8)` → `(2.2, 0.4, 0.12, 0.5)`로 변경(더 밝고 더 투명하게, 코어를 감싸는 은은한 후광 역할)
  - 6개 몬스터(`Monster_DPS_1/2`, `Monster_Area_1/2`, `Monster_Basic_1/2`) 전부의 `AttackRangeIndicator.lineWidth`를 0.15→0.22로, `displayDuration`을 1.0→0.5초로 변경(이 두 필드는 스크립트 기본값이 아니라 각 씬 인스턴스에 개별로 저장된 값이라 6곳 전부 직접 수정 필요했음)
  - 이 변경은 스크립트가 코어 링을 절차적으로 만들기 때문에, 코어 링 자체는 씬에서 별도로 만들 필요 없이 6개 몬스터 전부에 자동 적용됨(프리팹이 아닌 개별 씬 오브젝트라 이 부분만 코드로 처리해 반복 작업을 피함)
  - 검증: Play 모드에서 플레이어를 몬스터 근처로 워프(`NavMeshAgent.Warp`)시키고 무적 처리 후 리플렉션으로 강제 공개 → 실제 게임 카메라(포스트프로세싱 적용) 스크린샷으로 얇고 밝은 코어 + 넓고 옅은 바깥 글로우가 겹친 링을 확인. 기존의 밋밋한 단색 얇은 선보다 확실히 개선됨. 컴파일·콘솔 에러 없음
- **게이트 통과 여부**: 링 비주얼 개선(코어+글로우 2중 링) ✅ / 표시 시간 단축(1.0→0.5초) ✅ / 6개 몬스터 전부 반영 ✅ / 컴파일 에러 없음 ✅
- **후속 조정 1**: 사용자가 "이제 너무 빨리 사라진다"고 해서 `displayDuration`을 0.5 → 0.8초로 재조정(6개 몬스터 전부). 씬 저장 완료
- **후속 조정 2 — 범위 안쪽 채움 추가**: 사용자가 `MonsterRange_ref.png`를 다시 참고해 "링만 있지 말고 안쪽도 비슷한 색의 반투명으로 채워서 범위라는 느낌을 확실히 달라, 단 몬스터가 그 색에 가려지면 안 된다"고 요청
  - `AttackRangeIndicator.cs`에 `BuildFill()`을 추가해 `RangeIndicator_Fill`이라는 절차적 원형 채움(`SkillVfxLibrary.CreateFlatQuad` + `SoftCircle` 텍스처, alpha blend, 색 `(1, 0.35, 0.1, 0.32)`)을 런타임 생성. 링(`Custom/RangeIndicatorOverlay`, `ZTest Always`라서 항상 맨 위에 그려짐)과 달리 채움은 `SkillVfxLibrary`가 만드는 일반 URP 파티클 셰이더(기본 `ZTest LEqual`, 표준 깊이 테스트)를 그대로 써서 — 몬스터(불투명 오브젝트, 먼저 그려져 깊이버퍼를 채움)보다 카메라에서 먼 채움 픽셀은 깊이 테스트에서 걸러져 몬스터를 가리지 않음. 새 셰이더/머티리얼 에셋을 따로 만들 필요 없이 스킬 이펙트용 라이브러리를 재사용
  - 링/코어와 함께 `revealed`/`displayDuration` 로직에 맞춰 같이 켜지고 꺼지도록 연결, `OnDestroy`에서 `SkillVfxLibrary.DestroyOwned`로 런타임 머티리얼 정리
  - 검증: Play 모드에서 플레이어를 몬스터 근처로 워프시키고 강제로 표시한 뒤 실제 게임 카메라 스크린샷으로 확인 — 범위 안쪽이 따뜻한 주황색으로 옅게 채워지고, 몬스터(치킨)와 플레이어(폭스) 모두 채움 위에 정상적으로 보임(가려지지 않음) 확인. 콘솔 에러 없음
- **게이트 통과 여부(후속)**: 표시 시간 재조정 ✅ / 범위 채움 추가 ✅ / 몬스터가 채움에 가려지지 않음 ✅

### 투사체 가시성 개선 (2026-09-15, 같은 날 아홉 번째 작업)

- **배경**: 사용자가 투사체(`Projectile_Egg`) 속도가 느려서 걸어서 피해지는 경우가 많다고 지적 → 속도를 더 올려도 되는지 물어봐서 `Projectile.cs`의 이동 판정이 이미 SphereCast 기반 스윕 검사라 고속에서도 터널링 걱정 없다고 답함(사용자가 직접 `speed`를 12→30으로 올림, 확인됨). 이어서 "지금도 그렇고 더 올리면 가시성이 안 좋을 것 같다"며 투사체 자체의 가시성 개선을 요청(크기를 키워도 좋다고 함)
- **`Assets/Prefabs/Projectile_Egg.prefab` 수정** (프리팹 하나 수정으로 모든 몬스터에 공통 적용됨):
  - 기존엔 루트 오브젝트 하나에 `MeshFilter`/`MeshRenderer`/`SphereCollider`/`Rigidbody`/`Projectile`가 전부 같이 있어서, 메시 크기를 키우면 콜라이더(히트박스)도 같이 커져 버림. **`Visual`이라는 자식 오브젝트를 새로 만들어 `MeshFilter`/`MeshRenderer`만 옮기고 로컬 스케일을 1.8로 키움** — 콜라이더/리지드바디는 루트에 그대로 남아 있어 히트박스(공정성)에는 변화 없이 눈에 보이는 모델만 커짐(이전 대비 약 3.6배 시각적 크기)
  - **`TrailRenderer` 추가**: 새 머티리얼 `Assets/Materials/VFX/M_ProjectileTrail.mat`(URP `Particles/Unlit`, 알파 블렌드로 투명 설정 — `manage_material create`가 기본 Opaque로 만드는 문제를 피하려고 `_Surface`/`_Blend`/`_SrcBlend`/`_DstBlend`/`_ZWrite`를 전부 수동 설정하고, 셰이더 키워드(`_SURFACE_TYPE_TRANSPARENT`)와 렌더큐(3000)까지 코드로 직접 설정함 — 이 키워드/큐 설정을 빠뜨리면 값은 저장되어도 실제 렌더링에는 반영 안 됨, 이번에 새로 확인한 주의사항) + 따뜻한 주황색 그라디언트(시작 알파 0.85 → 끝 0)로 폭 0.12→0.012 테이퍼, 지속시간 0.15초의 짧고 선명한 잔상을 남김
  - ⚠️ **시행착오**: 처음에 `execute_code`로 오래된(stale) 인스턴스 ID를 참조해 `widthCurve`/`colorGradient`를 설정했는데, 그 사이 프리팹 스테이지가 다시 열리면서 ID가 바뀌어 실제로는 엉뚱한(또는 존재하지 않는) 객체에 적용됐던 것으로 추정 — 결과적으로 `TrailRenderer.sharedMaterial`이 저장된 프리팹에 `null`로 남아있어 Play 모드에서 투사체 뒤에 **마젠타색(셰이더 누락 시 기본 색) 줄**이 나타나는 사고로 이어짐. 프리팹 스테이지를 새로 열어 현재 유효한 인스턴스 ID로 `m_Materials`를 다시 지정하고 저장해서 해결 — **프리팹 스테이지를 닫았다 다시 열면 이전에 얻은 인스턴스 ID는 더 이상 유효하지 않다는 것**을 이번에 확인함(다음에도 유의)
  - 검증: Play 모드에서 플레이어를 몬스터 사거리 안으로 워프시키고 무적 처리 후 투사체 발사 → 스크린샷으로 투사체 모델이 이전보다 뚜렷하게 커진 것 확인, `TrailRenderer.sharedMaterial`이 `M_ProjectileTrail`로 정상 연결된 것도 코드로 재확인. 무적 중 피격 무시(막힘 로그) 등 기존 기능도 정상 동작 확인. 콘솔 에러 없음
- **게이트 통과 여부**: 시각적 크기 확대(히트박스 불변) ✅ / 잔상 트레일 추가 ✅ / 마젠타 버그 해결 ✅ / 컴파일·콘솔 에러 없음 ✅

### 플레이어 이동속도 관리 방식 정리 (2026-09-15, 같은 날 열 번째 작업)

- **배경**: 사용자가 가속 스킬이 이동속도에 곱연산되는 방식인지 물어봄 → `PlayerAutoMover.baseSpeed`가 `Awake()` 시점에 `NavMeshAgent.speed`를 그대로 읽어 저장하고, 가속 스킬은 그 위에 배율만 곱하는 구조라 기본 속도를 바꿔도 안전하다고 답변. 이어서 "그럼 Fox 프리팹에서 바꾸면 되는거야?"라고 물어서 확인해보니, **Fox 프리팹의 `NavMeshAgent.speed`(3.5)와 달리 Stage1(Test)·Stage2 두 씬의 `Player` 인스턴스가 전부 3.6으로 개별 오버라이드돼 있어서 프리팹만 고치면 씬에 반영이 안 되는 상태**였음. 사용자가 "오버라이드 말고 프리팹 기준으로 적용되게 정리해달라"고 요청
- **처리**: 두 씬 각각에서 `PrefabUtility.RevertObjectOverride(agent, InteractionMode.AutomatedAction)`로 `Player`의 `NavMeshAgent` 컴포넌트 오버라이드를 되돌림(다른 필드는 이미 프리팹과 값이 같아서 speed만 3.6→3.5로 복귀, 부작용 없음). 두 씬 모두 저장 완료
- **결과**: 이제부터 이동속도는 `Assets/Fox/Prefabs/Fox.prefab`의 `NavMeshAgent.Speed` 값 하나만 바꾸면 Stage1(Test)·Stage2 두 씬에 전부 반영됨(씬별로 따로 바꿀 필요 없음)
- **게이트 통과 여부**: 두 씬 오버라이드 제거 ✅ / 프리팹 값(3.5)으로 통일 ✅ / 콘솔 에러 없음 ✅

### UI 작업 진행 상황 (`docs/ui-design.md` U1~U6)

| 단계 | 상태 |
|---|---|
| U1 폰트 교체 | ✅ 완료 — 전체 TMP 마이그레이션 + NEXON Lv1 Gothic |
| U2 Canvas Scaler/앵커 | ✅ 완료 — 1920×1080, ScaleWithScreenSize, Match 0.5 |
| U3 9-slice | Layer Lab 에셋이 이미 9-slice 상태라 별도 임포트 작업 없이 U4에서 바로 사용함 |
| U4 인게임 HUD 재구성 | ✅ 완료(2026-09-11) — 아래 "U4 인게임 HUD" 상세 항목 참조 |
| U5 결과 화면 재구성 | ✅ 완료(2026-09-14) — 아래 "U5 결과 화면" 상세 항목 참조 |
| U6 애니메이션 폴리시 | ❌ 미착수 — 다음 세션에 범위 확인 |

### 사용자가 확정한 사항 (임의로 뒤집지 말 것)

- 캐릭터 에셋(Fox/Chicken) 교체 안 함 — 배경/환경 에셋만 확장. 단, 몬스터 타입별(광역/고DPS) 전용 에셋은 사용자가 추후 별도로 준비해올 예정(현재는 `Chicken` 모델+색상 틴트만 입힌 placeholder)
- `visionRadius` 관련 수치는 사용자가 먼저 말하기 전까지 절대 손대지 않음
- 게임 제목: "여우는 죄가 없다" (메인 메뉴 `TitleText`)
- 메인 메뉴 제목/버튼은 **절차적(procedural) 룩 유지** — Layer Lab GUI 팩으로 한 번 교체했다가 "화려하고 배경과 안 어울린다"는 피드백으로 원복함. **인게임 HUD만 Layer Lab 에셋 사용**, 메인 메뉴는 건드리지 말 것

### 보류/선택 작업 (사용자가 필요하다고 할 때)

- `PlayStats.csv` 데이터 수집(P4, 사람이 직접 여러 번 플레이) — 현재 1줄만 쌓여 있음. 데이터 쌓인 뒤 design.md 10.4 기준(재화 0~1개, 체력 30~70%, 반응시간 1.5~3초)과 비교해 수치 재조정(P5)
- `docs/tutorial.md` 5장 연출(카메라 줌인, UI/월드 하이라이트, 상시 피드백 8종, 슬로우모션 등, P6) — 9단계에서 의도적으로 범위 밖으로 뺀 것
- 사용자가 예고한 기상(weather)/환경 확장 기능(`Low Poly Nature Bundle` 기반, 구체 범위는 아직 미정 — 사용자가 지정할 것)
- 영상 촬영, `PlayStats.csv` 데이터 기반 최종 수치 조정, 정성 설문(design.md 10.1/10.2) — 전부 사람이 진행
- 옛 `TutorialPopupManager.cs`/`MonsterTutorialTrigger.cs` — 더 이상 안 쓰지만 파일 삭제 금지 규칙 때문에 프로젝트에 남아있음. 필요 없으면 사용자가 직접 지우거나, "지워도 된다"고 명시적으로 알려주면 정리 가능

### 오늘(2026-09-14) 세션에서 한 일 — 전체 요약

1. **HUD 미세 조정**: 우상단 `StageLabel`("Stage 1"/"Stage 2")을 상단 중앙 `ProgressBar` 바로 위로 이동(중앙 정렬), 진행도 바는 그만큼 아래로 이동. 사용자가 우측 상단에 설정 버튼을 추가할 예정이라 자리를 비워달라고 요청함. 두 스테이지 모두 적용·Play 모드로 확인 완료.
2. **U5 — 결과 화면 재구성** (완료): 아래 "U5 결과 화면" 절 참조. 같은 날 후속 피드백으로 배치 재조정 + 실패 화면 메인 메뉴 버튼 추가까지 함께 완료됨.
3. **스테이지1 — 벽 개수 제한 지연 공개 + UI 강조 시스템 신규** (완료, 단 강조 위치는 미세조정 남음): 아래 "벽 개수 제한 지연 공개" 절 참조.
4. **후속 수정 — 강조 화면 이탈 방지 + 튜토리얼 0.5초 스킵 방지** (부분 완료 — ⚠️ **강조 위치 조정은 사용자가 "아직 더 해야 한다"고 확인, 다음 세션 최우선 과제**): 아래 "후속 수정" 절 참조.

### U5 결과 화면 (2026-09-14 완료)

- `Stage1(Test).unity`, `Stage2.unity` 양쪽에 동일하게 적용
- 기존 `ResultText`(TMP)를 `ResultPanel`로 재구성(U4와 같은 패턴 — 오브젝트는 유지하고 컴포넌트 구성을 갈아끼움). TMP 컴포넌트를 떼어내고 `ResultPanelUI.cs`(신규) 부착, 그 아래 실제 패널 구조(`Backdrop`/`PanelBG`/`TitleText`/`ClearGroup`/`FailGroup`)를 새로 생성
- **클리어**: 항목이 위에서부터 0.2초 간격으로 순차 등장(체력 → 사용한 벽 → 클리어 시간 → 별 자리). 체력 숫자·벽 사용 개수·클리어 시간 전부 0에서 실제 값까지 카운트업. 사용한 벽이 적을수록 텍스트가 골드 강조색에 가까워짐(`Color.Lerp`). 별 등급은 `Icon_ImageIcon_StarGrade_s_Off` 3개를 빈 자리로만 표시(로직 없음, design.md 8장 "향후 확장" 그대로)
- **실패**: 배경이 서서히 회색으로 물듦(`Backdrop` 색상 페이드). 화면 상단 `ProgressBar`는 그대로 유지되어 어디까지 갔는지 계속 보임. `PlayerHealth.OnDamaged`를 구독해 피격 시점의 타일 인덱스별로 피해량을 누적(`StageManager.damageByTile`) → 실패 시 가장 많이 맞은 타일을 계산해 `ProgressBarUI`에 새로 추가한 `HitMarker`(빨간 점)로 표시
- **신규/수정 스크립트**: `ResultPanelUI.cs`(신규, 패널 표시·애니메이션 전담) / `StageManager.cs`(클리어·실패 시 통계 계산해 `ResultPanelUI`에 전달, 피해량 타일별 누적, 재도전 시 전부 리셋) / `WallPlacementManager.cs`(`WallsPlaced` getter 추가) / `ProgressBarUI.cs`(`ShowHitMarker`/`HideHitMarker` 추가)
- 버튼(`RetryButton`)은 기존 로직·비주얼 그대로 재사용, `PanelBG` 자식으로 위치만 이동(다시 도전/다음 스테이지/메뉴로 돌아가기 문구 전환은 기존 `StageManager.ShowResult` 로직 그대로)
- 사용한 에셋: Layer Lab `Popoup01~03_White_Bg`(패널 배경, 크림색으로 틴트), `Icon_Heart`/`Icon_Shield`(U4와 공용), `Icon_ImageIcon_StarGrade_s_Off`(별자리)
- **배치 재조정(같은 날 후속 피드백)**: 첫 버전은 "너무 빽빽하다"는 피드백을 받아 패널을 640×480→**700×600**으로 키우고, 행 간격(체력→벽→시간→별)을 54px 안팎에서 **74~78px**로 넓힘. 아이콘-바/텍스트 간격도 46→58px, 체력 바 340→390px, 버튼 220×64→240×68, 별 간격 48→58px·크기 34→38로 함께 키움. 폰트 크기도 타이틀 52→56, 본문 항목 20~24→24~28로 소폭 확대. 두 스테이지 모두 재검증 완료
- **실패 화면에 메인 메뉴 버튼 추가(같은 날 후속 요청)**: 실패(GAME OVER) 시에만 `다시 도전` 버튼을 위로 올리고(y=46→130) 그 아래에 `메인 메뉴로` 버튼을 새로 추가 — 클릭 시 `StageManager.menuSceneName`("MainMenu")으로 씬 전환. 클리어/게임클리어 화면은 기존처럼 버튼 1개만 기본 위치(y=46)에 유지. `StageManager.cs`에 `menuButton` 필드 + `SetRetryButtonPosition()` 헬퍼 추가, `RetryButton`을 복제해 `MenuButton` 생성(같은 비주얼 재사용). 두 스테이지 모두 Play 모드에서 버튼 클릭 시 실제로 MainMenu 씬 전환까지 확인
- ⚠️ **이 작업 중 발견한 이상 현상(나중에 원인 확인됨)**: 세션 도중 제가 시작하지 않은 Play 모드가 Stage2에서 실행 중인 것을 발견(콘솔에 실제 몬스터 공격/피격 로그가 쌓이고 있었음). Play 모드 종료 후 활성 씬이 MainMenu로 복귀됨. 이 과정에서 막 만들어둔 Stage1(Test)의 `MenuButton`이 저장 전에 씬이 전환되며 유실되어 다시 만듦(이후 두 씬 모두 생성 직후 바로 저장하는 방식으로 전환해 재발 방지). 당시엔 원인 미상이었으나, **같은 날 뒤에 있었던 "Play 모드 중 스크립트 재컴파일" 사고(아래 "후속 수정" 절 참조)로 원인이 확인됨 — 사용자가 에디터에서 직접 플레이 테스트하고 있었던 것.** 다른 세션/외부 접속 문제 아니었음
- ⚠️ **버그 발견·수정**: 처음엔 순차 등장 행 사이 대기에 `WaitForSeconds`(scaled)를 썼는데, 스테이지1은 시작하자마자 튜토리얼이 `Time.timeScale=0`으로 멈춰서 그 상태에서 결과 화면이 뜨면 애니메이션이 첫 줄에서 멈춰버림 → `WaitForSecondsRealtime`으로 교체(카운트업 코루틴은 처음부터 `Time.unscaledDeltaTime` 사용 중이었어서 일관성 있게 맞춤)
- **검증**: 두 씬 모두 Play 모드에서 `StageManager.HandleClear()`/`RetryStage()`/`PlayerHealth.TakeDamage()`를 리플렉션·직접 호출로 실행해 클리어(순차 등장+카운트업+벽 강조색), 실패(회색 페이드+피격 마커), 재도전(패널·마커·체력 전부 초기화) 전부 스크린샷으로 확인. 콘솔 에러 없음(무관한 `NoSubscription`만 존재). 두 씬 모두 저장 완료

### 참고: Build Settings / 씬 구성 현황

- Build Settings 순서: `MainMenu.unity`(0번) → `Stage1(Test).unity`(1번) → `Stage2.unity`(2번)
- 결과 화면 흐름: 스테이지1 클리어→"다음 스테이지"(Stage2 로드) / 스테이지2(마지막) 클리어→"GAME CLEAR"+"메뉴로 돌아가기"(MainMenu 로드) / 실패→"다시 도전"(현재 스테이지 재시작)
- 두 스테이지 씬 모두 저장 완료 상태(작업 종료 시점 `isDirty=false` 확인함)

### U4 인게임 HUD (2026-09-11 완료)

- `Stage1(Test).unity`, `Stage2.unity` 양쪽에 동일하게 적용
- **체력 바**: 기존 `HealthText`를 `HealthBar`로 재구성(하트 아이콘 + `Slider_Basic01_White` 기반 게이지 + 숫자, `/100` 생략). `PlayerHealthUI.cs` 전면 재작성 — 피해 시 0.2초 보간, 60%/30% 구간별 색 변화(기본→주황→빨강), 30% 이하에서 천천히 맥박치는 깜빡임
- **남은 벽**: 기존 `CurrencyText`를 `CurrencyIcons`로 재구성(총 개수만큼 방패 아이콘 슬롯 나열, 신규 프리팹 `Assets/Prefabs/UI/WallSlotIcon.prefab`). `CurrencyUI.cs` 전면 재작성 — 벽 배치 시 슬롯이 어두워지며 펀치 애니메이션, 0개가 되면 전체 빨강 고정. 10개 초과 시 아이콘+숫자로 전환하는 `FallbackGroup`도 만들어 두었으나 현재 두 스테이지 모두 총량이 10개 이하(스테이지1=6, 스테이지2=7)라 비활성 상태
- **진행도 바**: 신규 `ProgressBarUI.cs` + 화면 상단 중앙에 얇은 `Slider_Basic01_White` 바, 현재 위치를 `Pictoicon_Mappin` 핀 아이콘으로 표시(좌우 이동), 끝에 `Pictoicon_Flag_0` 고정 장식. `PathManager`의 타일 인덱스 변경 이벤트 기반
- **스테이지 표시**: 우상단에 배경 없는 텍스트만("Stage 1" / "Stage 2")
- 사용한 에셋: Layer Lab의 `Icon_Heart`, `Icon_Shield`(벽 아이콘 대체 — 팩에 벽/벽돌 아이콘이 없어 방패로 대체함), `Pictoicon_Mappin`, `Pictoicon_Flag_0`, `Slider_Basic01_White` 프리팹(체력 바·진행도 바 공용)
- Play 모드에서 피해 적용/재화 소모/타일 인덱스 변경 시뮬레이션으로 색 변화·펀치 애니메이션·마커 이동 전부 확인. HUD가 경로·몬스터 사거리 원을 가리지 않는 것도 스크린샷으로 확인함

## ⚠️ 씬 파일명 변경 (2026-09-10)
- 스테이지1 씬 파일을 `Assets/Scenes/SampleScene.unity` → `Assets/Scenes/Stage1(Test).unity`로 사용자 요청에 따라 리네임함(GUID 동일하게 유지되어 참조 깨짐 없음, Build Settings 경로도 자동 갱신됨 확인)
- **과거 아카이브 기록 중 "SampleScene"을 언급하는 부분은 전부 지금의 `Stage1(Test).unity`를 가리킴**

---

## 현재 단계

**design.md 0~10단계 전부 구현 완료(2026-09-10), `docs/polish.md` P1~P3 완료(2026-09-11), UI 작업 U1~U5 완료(2026-09-14), `UIHighlightEffect` 강조 표시 재설계 완료(2026-09-15).** 프로젝트는 "플레이 가능한 완성된 프로토타입"이었으나, 2026-09-15부터 `docs/feature-expansion.md` 기준 스킬/함정/빙결 지대 확장 작업으로 전환(마감 2026-09-17). U6·P4~P6는 이 확장이 끝난 뒤로 보류. 현재 E0(확장 준비)·E1(스킬 시스템)·E2(조명탄)와 스킬 시각 연출·스킬 버튼 UI·투사체 가시성·이동속도 정리까지 완료(2026-09-15), 벽 슬롯 클릭 회귀 버그·이동 끊김·캐릭터 크기 조정·E3(함정 — 낙석·가시함정)까지 완료(2026-09-16). **다음은 시간이 남으면 E4(빙결 지대), 급하면 바로 E5(튜토리얼·배치·마무리)로**. ⚠️ **시간 관계상 이번 마감(2026-09-17)의 기능 구현 최소 목표는 E3까지였고 이미 달성함**(2026-09-16 사용자 결정, "범위 축소 결정" 참조).

---

## 완료된 것 (마일스톤 요약)

> 날짜순. 각 항목의 상세 경위(원인 진단·시행착오·여러 차례 재보고된 버그 등)는 `PROGRESS_ARCHIVE.md`에서 해당 날짜 제목으로 찾을 것.

- **0단계 — 프로젝트 셋업 & 에셋 임포트** (2026-09-07): Cinemachine 신규 설치(사용자 승인), 레이어 생성(`AttackTarget`/`WallVision`), 프리팹 구조(루트=로직/`Model` 자식=비주얼) 확립. 기존 상태 버그(Animator Controller 뒤바뀜 등) 함께 수정
- **1단계 — 경로·타일 데이터 구조** (2026-09-09): `PathManager`/`TileData`/`PlayerAutoMover`, 20타일 ㄴ자 테스트 경로
- **Player 에셋을 Fox로 교체** (2026-09-09): 기존 `Monster.prefab`(Player용) 폐기, `Fox.prefab`=Player·`Chicken.prefab`=Monster(고정형 타워)로 역할 확정. 카메라를 완전 탑다운으로 재조정
- **2단계 — 벽 배치 & 재화** (2026-09-09): `WallSlot`/`CurrencyManager`/`WallPlacementManager`, 타일당 좌우 슬롯 40개
- **3단계 — 몬스터 반전 로직 ★핵심** (2026-09-09): `MonsterTower`/`Projectile`/`IDamageable`. "벽을 세우면 공격을 안 맞는다" 검증 게이트 통과. 이후 벽 차단 방식을 "발사 자체 차단"에서 **"몬스터는 항상 발사, 투사체가 벽에 맞아 소멸"**로 사용자 요청에 따라 변경 확정(현재 방식)
- **4단계 — 체력·UI·피해 팝업** (2026-09-09): `PlayerHealth`/`StageManager`/`DamageNumberPopup`. 녹화 일정 때문에 단계 순서를 앞당겨 긴급 진행
- **5단계 — 시야 제한** (2026-09-09): 타일 기반 방식에서 **캐릭터 중심 원형 시야**로 전면 재설계(여러 차례 사용자 피드백 반영). `FogVisibility`(대상 숨김)/`AttackRangeIndicator`(사거리 표시)/`FogOfWarGround`(바닥 전체를 덮는 텍스처 마스킹 안개) 세 컴포넌트로 확정, 전부 `visionRadius`(월드 유닛) 기준
- **6단계 — 스테이지1 조립(1차)** (2026-09-10): 기본형 몬스터 `Monster_A~D` 4마리 배치(겹침 구간 계산)
- **7단계 — 몬스터 종류 확장** (2026-09-10): `Monster_AreaType`(광역형)·`Monster_DPSType`(고DPS형) 프리팹 신규, 빨강/파랑 임시 색상 구분
- **8단계 — 스테이지2 조립** (2026-09-10): `Stage2.unity` 별도 씬으로 분리, 3종 혼합 몬스터 6마리 배치(사거리 중첩 다수), 좁은 시야(5)·빠듯한 재화(7). 재도전(`RetryStage`) 리셋 로직 각 매니저에 구현
- **튜토리얼 시스템** (2026-09-10): 기획서(`docs/tutorial.md`)에 맞춰 `TutorialManager`(트리거 5종: `StageStart`/`FirstMonsterSeen`/`FirstHit`/`FirstWallPlaced`/`TileReached`) 신규 구현, 페이지 단위 강제 슬롯 클릭 지원. 기존 `TutorialPopupManager`/`MonsterTutorialTrigger`는 사용 중단(삭제 금지 규칙으로 파일은 남아있음). 실제 클릭 테스트 중 여러 버그 발견·수정 끝에 안정화 — 현재 스테이지1 `first_hit` 강제 슬롯은 **타일3 Left 고정값**(동적 계산 대신 사용자 판단으로 단순화)
- **9단계 — 마무리** (2026-09-10): 재도전 버튼 UI 연결. 씬에 `EventSystem`이 없어 uGUI 버튼이 전혀 반응 안 하던 근본 원인 발견·수정(두 씬 모두). 벽 슬롯 코너 배치를 일반 규칙으로 개선(축 정렬 유지, 나가는 방향 구간의 가장자리를 그대로 사용)
- **10단계 — 검증 로그 수집** (2026-09-10): `PlayStatsLogger` 신규 — 클리어/실패 시점마다 재화 밸런스·정보 제한(반응시간) 지표를 `PlayStats.csv`에 누적 기록. 스테이지1은 튜토리얼 개입으로 반응시간 측정 제외
- **P1 — 프리팹 값 덤프 + 낡은 기록 정리** (2026-09-11): 프리팹 6종·씬 2개의 실측값을 전부 재확인해 기록(아래 "확정 수치" 참고), 실제와 어긋난 옛 기록 5곳 정리
- **P2 — 환경 데코레이션** (2026-09-11): 캐릭터 에셋 교체 대신 `Low Poly Nature Bundle` 임포트, 번들 전체 94개 머티리얼 URP 변환, 나무/바위/식생 스테이지당 26개 무작위 배치(안개 연동)
- **바닥/길/벽 비주얼 다듬기** (2026-09-11): `M_Floor`/`M_Wall`/`M_PathTile` 색상 정리, Demo_01/Demo_07 참고해 지형 굴곡(`GroundBumps`, flat shading)·벽돌 벽(`M_BrickWall`) 제작, 몬스터 사거리 표시가 지형에 가려지는 문제를 전용 오버레이 셰이더로 해결, 벽 슬롯 받침대(`SlotPedestal`) 추가, 도착 지점 포탈 이펙트(`ArrivalPortal`) 추가
- **결과 화면 개선 + 메인 메뉴 씬** (2026-09-11): 클리어="다음 스테이지"/실패="다시 도전"으로 분리, 마지막 스테이지 클리어는 "GAME CLEAR"+메뉴로. `MainMenu.unity` 신규 생성(시작하기/종료 버튼)
- **P3 — 레벨 디자인 매뉴얼** (2026-09-11): `docs/level-design.md` 작성(길/벽 슬롯/몬스터/수치/스테이지 제작 절차)
- **U1 — 폰트 교체** (2026-09-11): TMP Essential Resources 임포트, 전체 레거시 `Text`→TMP 마이그레이션, NEXON Lv1 Gothic 폰트 에셋(Regular/Bold) 생성. 하드코딩 문구를 `[SerializeField] string`으로 추출(`docs/text-editing-guide.md` 작성)
- **메인 메뉴 비주얼 개선** (2026-09-11): 배경을 3D 환경 씬(바닥+나무+바위+여우 배치 후 여우는 제거)으로 교체, 이후 Layer Lab GUI Pro 팩으로 리본 타이틀·버튼 재교체
- **U4 — 인게임 HUD 재구성** (2026-09-11): 위 "U4 인게임 HUD" 절 참조
- **HUD 미세 조정 + U5 — 결과 화면 재구성** (2026-09-14): 위 "오늘 세션에서 한 일" / "U5 결과 화면" 절 참조
- **스테이지1 — 벽 개수 제한 지연 공개 + UI 강조 시스템 신규** (2026-09-14): 아래 "벽 개수 제한 지연 공개" 절 참조
- **`UIHighlightEffect` 강조 표시 재설계** (2026-09-15): 위 "UIHighlightEffect 재설계" 절 참조. 사용자 최종 확인 완료

## 벽 개수 제한 지연 공개 (2026-09-14, 스테이지1 전용)

- **요청 배경**: 사용자 요청 — `Monster_C`(하이어라키 기준)를 마주치기 전에는 벽 개수 제한과 관련 UI를 아예 노출하지 말고, `Monster_C` 조우 시점에 "벽 개수는 제한되어 있다"를 튜토리얼로 알려주면서 그 순간 UI를 공개+강조할 것. tutorial.md 5.2(UI 강조: 원+화살표)가 그동안 미구현 상태였는데, 이 작업으로 처음 구현됨
- **`WallPlacementManager.cs`**: `wallLimitEnabled`(bool, 기본값 `true`) 추가 — `false`면 재화를 전혀 소모하지 않고 무제한 배치. 스테이지1 씬만 초기값을 `false`로 설정, 스테이지2는 스크립트 기본값(`true`) 그대로라 영향 없음
- **`TutorialTrigger.cs`**: `MonsterSeen` 트리거 신규 — 기존 `FirstMonsterSeen`("아무 몬스터나 처음 발견")과 달리 `TutorialStep.targetMonster`로 지정한 **그 몬스터**가 공개될 때만 발동
- **`TutorialStep.cs`**: `targetMonster`(FogVisibility, MonsterSeen 전용) / `enableWallLimitOnShow`(bool) 필드 추가
- **`TutorialManager.cs`**: 기존 스테이지1 step[3]("first_wall_placed", 트리거=`FirstWallPlaced`)을 트리거=`MonsterSeen`+`targetMonster=Monster_C`+`enableWallLimitOnShow=true`로 재설정(문구는 그대로 재사용: "벽은 정해진 개수만 세울 수 있습니다"). 이 단계가 처음 열리는 순간 `RevealWallLimit()`이 `wallPlacementManager.WallLimitEnabled=true` + `CurrencyIcons` 활성화+최상단으로 sibling 이동(오버레이보다 위에 그려지도록) + `UIHighlightEffect.Highlight()` 호출
- **`UIHighlightEffect.cs`(신규)**: tutorial.md 5.2 스펙대로 대상 UI 주변에 **링이 크게 시작해 수축하며 나타나고, 화살표가 위아래로 튕기며, 대상이 1.15배로 확대**되는 범용 강조 컴포넌트. `Time.unscaledDeltaTime` 기반이라 튜토리얼 정지(`timeScale=0`) 중에도 재생됨. 링/화살표 스프라이트는 Layer Lab `Icon_ImageIcon_Tutorial_LineCircle.png` / `Pictoicon_Arrow_Down.Png` 재사용. 다른 튜토리얼 단계에서도 재사용 가능(현재는 이 단계에서만 사용)
- ⚠️ **테스트 중 발견한 버그(수정 완료)**: 같은 프레임/이벤트 처리 중 서로 다른 트리거(예: `FirstMonsterSeen`과 `MonsterSeen`, 또는 `TileReached`와 `MonsterSeen`)가 동시에 조건을 만족하면, 먼저 연 팝업이 `IsOpen=true`를 만들어서 뒤에 검사하는 트리거가 조용히 유실되는 기존 구조적 결함이 있었음(리플렉션 테스트로 실제 재현함 — 플레이어를 `Monster_C` 위치로 순간이동시켰더니 `TileReached(7)` 단계가 먼저 열리며 `MonsterSeen` 단계가 씹힘). `TutorialManager`에 `pendingStep`(1개 슬롯 대기열) 추가 — `FireMonsterSpecificStep`이 `IsOpen`이라 못 열면 대기시켰다가 `ClosePause()`에서 즉시 이어서 연다. 실제 게임 진행 순서(몬스터 A→B→C 순서대로 발견)에서는 이 충돌이 거의 발생하지 않지만, 안전하게 처리해둠
- **재도전 시 상태 유지 확인**: `RetryStage()`는 `wallLimitEnabled`/`CurrencyIcons` 활성 상태를 별도로 리셋하지 않음 — 한 번 `Monster_C`를 만나 제한이 공개된 뒤에는 재도전해도 계속 공개 상태 유지(재화만 총량으로 리셋), `Monster_C`를 아직 못 만난 상태에서 재도전하면 계속 무제한 상태 유지. 둘 다 Play 모드에서 실측 확인
- **검증**: Play 모드에서 몬스터들의 `MonsterTower`를 임시로 비활성화해(공격으로 인한 흐름 방해 방지) `Monster_A` → 자유 배치(재화 안 줄어듦, UI 숨김 유지) → `Monster_C` 워프(튜토리얼 발동+UI 공개+링 강조 스크린샷 확인) → 배치(이제 재화 5로 감소) → 재도전(제한 상태 유지, 재화만 6으로 복원) 순서로 전부 리플렉션 호출+스크린샷으로 확인. 콘솔 에러 없음. 스테이지2는 건드리지 않아 기존 그대로 동작(항상 제한 있음, UI 항상 표시)
- ⚠️ **참고**: 이 지연 구간(Monster_C 이전)에 배치한 "무료 벽"도 `WallPlacementManager.WallsPlaced`(사용한 벽 개수 카운트, 결과 화면에 표시)와 `PlayStatsLogger`의 재화 밸런스 통계에는 그대로 잡힘 — 재화를 안 썼는데 "사용한 벽 개수"에는 포함되는 형태라, P4/P5에서 스테이지1 데이터를 볼 때 이 점을 감안할 것(요청 범위 밖이라 별도 처리는 안 함)

### 후속 수정 — 강조 표시 화면 이탈 + 튜토리얼 즉시 스킵 방지 (2026-09-14, 같은 날 추가 피드백)

- **강조 링/화살표가 화면 밖으로 나가던 문제**: `CurrencyIcons`처럼 화면 맨 위쪽에 붙어있는 HUD 요소는 여백이 좁아서, 기존 링 크기(패딩 36·시작 배율 2.2배)와 화살표 위치(대상 위 62px)가 캔버스 상단 경계(1080)를 넘어가버렸음. `UIHighlightEffect.cs` 수정:
  - 패딩 36→20, 시작 배율 2.2→1.6로 줄여 기본 크기 자체를 보수적으로
  - 루트 `Canvas`의 `RectTransform.rect`를 기준으로 링·화살표 위치를 **항상 화면 안쪽으로 clamp**하는 `ClampToCanvas()` 추가(가장자리에서 최소 12px 여백 유지) — 어떤 위치의 UI를 강조하든 앞으로 재발 안 하도록 일반화
  - 화살표는 대상 위쪽에 놓을 공간이 부족해 clamp로 눌리면 자동으로 대상 **아래쪽**에 배치하도록 폴백 추가
  - 씬의 `UIHighlightEffect` 컴포넌트 인스턴스는 이미 예전 기본값(패딩 36 등)이 박혀있어서 코드 기본값만 바꿔서는 반영 안 됨 — `SerializedObject`로 씬 인스턴스 값도 직접 갱신함
- **튜토리얼이 뜨자마자 클릭 한 번에 넘어가버리는 문제**: `TutorialManager`에 `minPageDisplayTime`(기본 0.5초) 추가 — 페이지가 뜬 시점(`Time.unscaledTime` 기준)부터 이 시간 동안은 `Update()`의 클릭/키 입력을 무시. 슬롯 강제 클릭 페이지(`IsAwaitingSlotClick`)는 애초에 이 클릭-스킵 경로를 안 타므로 영향 없음
- ⚠️ **작업 중 겪은 사고 — Play 모드 중 스크립트 재컴파일**: 위 수정을 반영하려 재컴파일했는데, 그 시점에 사용자가 에디터에서 **직접 Stage1을 플레이 중**이었음(벽 6개 설치, 체력 70 등 실제 플레이 흔적 확인). 재컴파일이 Play 모드를 끊지 않고 이어져서, 그 이후의 모든 확인 작업이 저장된 씬이 아니라 **살아있는 플레이 세션**을 대상으로 이뤄짐 — 실수로 그 세션에 남아있던 테스트 벽 6개를 정리하고 제한/UI 상태를 초기화함(사용자에게 계속 플레이 중인지 확인 후 진행 동의 받음). Play 모드를 완전히 종료하니 씬 자체는 마지막 저장 시점 그대로 깨끗하게 남아있었음(`isDirty=false`, 루트 19개) — **다행히 실제 저장 파일 손상은 없었음**
- ⚠️ **참고(재발 방지)**: 스크립트를 고치고 재컴파일하기 전에 **현재 Play 모드 여부와 최근 콘솔 로그(실제 전투 로그 등)를 먼저 확인**해, 사용자가 직접 플레이 중인 상태에서 재컴파일로 세션을 방해하지 않도록 할 것. 이번엔 재컴파일 자체가 세션을 끊지 않아서(최근 Unity의 "Recompile And Continue Playing" 동작) 이 문제가 생겼음
- **검증**: Play 모드에서 몬스터 공격을 임시로 꺼두고 Monster_A→Monster_C 순서로 리플렉션 호출해 재현 — 링이 화면 상단 경계(540, 캔버스 로컬 좌표 기준) 안쪽(512.6)에 정확히 들어오는 것을 수치로 확인, 스크린샷으로도 HUD를 벗어나지 않고 깔끔하게 감싸는 것 확인. `minPageDisplayTime`은 `pageShownAtUnscaledTime` 필드가 올바른 시점에 갱신되는 것을 확인(로직 자체는 단순 비교라 수치 확인으로 충분). ⚠️ 이날 세션 내내 에디터가 포커스를 잃어 프레임이 멈추는 문제가 반복돼(`Time.unscaledTime`이 여러 초 동안 그대로 멈춰있음을 실측 확인) 화살표의 바운스 애니메이션까지는 실시간으로 재생되는 것을 직접 못 봤음 — 로직상 문제 없다고 판단되나, 실제 포커스가 있는 에디터에서 사용자가 한 번 더 눈으로 확인해보면 좋음
- ⚠️ **미완료 — 다음 세션 최우선**: 위 clamp 수정을 사용자에게 스크린샷으로 보여준 뒤, "일단 위치 조정은 아직 더 해야 할 것 같다"는 피드백을 받고 오늘 세션을 마감함. 구체적으로 어느 부분이 아직 어색한지(링 크기 비율? 화살표 각도/위치? 강조 시점의 카메라·다른 UI와의 겹침?)는 다음 세션에서 다시 확인해야 함 — 이 이상은 구체적 지시 없이 임의로 더 조정하지 않았음

### UIHighlightEffect 재설계 (2026-09-15, 스테이지1 전용, 사용자 최종 확인 대기 중)

- **배경**: 2026-09-14에 만든 강조 표시(링+화살표)에 대해 사용자가 "화살표 위치도 애매하고, 링 위치가 벽슬롯 개수 UI랑 안 맞고, 링 생긴 것도 어색하다"고 피드백. 세 가지를 모두 처리함
- **원인 1 — 링 크기가 실제 아이콘 개수와 무관했음**: `TutorialManager.RevealWallLimit()`이 강조 대상으로 넘기던 `wallLimitUIRoot`(`Canvas/CurrencyIcons`)는 RectTransform 크기가 340×40으로 **고정**돼 있었음(배경 이미지도 없는 빈 컨테이너). 실제 아이콘(`SlotContainer`, 6~7개)은 그 안에서 왼쪽 정렬로만 채워지고 컨테이너 자체는 크기가 안 변해서, 링이 항상 아이콘 개수와 무관하게 똑같이 넓은 빈 상자를 감쌌던 것
  - `CurrencyUI.cs`: 실제로 보이는 콘텐츠(슬롯 모드면 `SlotContainer`, 폴백 모드면 `FallbackGroup`)의 RectTransform을 반환하는 `ActiveContentRect` 프로퍼티 추가
  - `SlotContainer`에 `ContentSizeFitter`(Horizontal/Vertical=Preferred Size) 추가 — 이제 아이콘 개수만큼만 폭이 자동으로 늘어남(6개=222, 7개=258 등)
  - `TutorialManager.RevealWallLimit()`이 `wallLimitUIRoot` 대신 `CurrencyUI.ActiveContentRect`를 강조 대상으로 넘기도록 수정. `ContentSizeFitter`는 다음 레이아웃 패스에서야 반영되므로 `LayoutRebuilder.ForceRebuildLayoutImmediate()`로 즉시 강제 재계산 후 `Highlight()` 호출
- **원인 2 — pivot 기준 위치 계산 버그**: `UIHighlightEffect`가 링/화살표 위치를 `target.position`(=대상의 pivot 월드 좌표) 기준으로 계산했는데, `SlotContainer`/`CurrencyIcons`는 pivot이 `(0, 0.5)`(좌측 정렬)라 실제 시각적 중심이 아니었음. `GetCenterWorld(RectTransform)` 헬퍼(`target.TransformPoint(target.rect.center)`)를 추가해 모든 위치 계산을 여기로 통일 — pivot이 어디든 항상 실제 중심 기준으로 맞음
- **링 디자인 교체(1차, 이후 수정됨)**: 기존 `Icon_ImageIcon_Tutorial_LineCircle`(네온 하늘색 원 글로우)는 이 게임의 톤과 안 맞고 좁고 넓은 대상에 씌우면 타원으로 찌그러지는 게 문제라, 처음엔 9-slice 사각 테두리 스프라이트 `BorderFrame_Round01_White1`로 교체했었음(`Image.Type.Sliced`).
  - ⚠️ **바로 다음 피드백에서 발견된 문제**: 사용자가 "지금은 벽 개수 UI를 아예 가려서 안 보인다"고 지적 → 텍스처를 `isReadable=true`로 임시 전환해 `GetPixel`로 실제 픽셀을 직접 찍어보니, `BorderFrame_Round01_White1`도 `BorderFrame_Round02`도 **가운데까지 완전히 불투명한 흰색 패널**이었음(이름은 "Border"지만 실제로는 속이 꽉 찬 판넬 에셋 — 미리보기 이미지만 보고 "테두리 라인"이라고 잘못 추측했던 것). 확인 후 두 텍스처 모두 `isReadable`을 원래 값(false)으로 복구
  - **최종 해결**: 외부 스프라이트에 의존하지 않고, `UIHighlightEffect`가 얇은 막대 4개(위/아래/좌/우, `Image` 컴포넌트에 sprite 없이 색만 입힌 기본 사각형)를 앵커 스트레치로 배치해 사각 테두리를 직접 그리도록 재작성 — 가운데에는애초에 이미지 자체를 만들지 않으므로 항상 확실하게 뚫려 있음. 부모(`activeRing`)의 `sizeDelta`만 바꾸면 4개 막대가 앵커 기준으로 자동으로 같이 늘어나 애니메이션 코드 변경 없이 그대로 재사용됨. 두께는 `ringThickness`(기본 3px)로 조절, 크기는 패딩을 (16,14)→(24,20)으로 살짝 키움(사용자 요청)
- **화살표 재설계**:
  - 1차: "화면 중앙 쪽에서 링을 가리키는 위치"로 캔버스 중심 기준 동적 방향 계산(`PlaceArrowTowardTarget`)을 구현했었으나, 사용자가 다음 피드백에서 **"사각 테두리의 오른쪽 아래 모서리 쪽으로 옮겨서 더 자연스럽게"**를 요청 → 화면 중심이 아니라 **대상 자신의 오른쪽 아래 모서리 기준 고정 대각선**으로 단순화(`PlaceArrowAtBottomRightCorner`): 모서리 바깥쪽에 놓고 대각선 안쪽(대상 중심)을 바라보도록 회전. 여전히 대상 rect 기준 상대 좌표라 다른 UI에도 재사용 가능
  - 기존 골드색 화살표가 골드색 벽 아이콘 배경과 색이 비슷해 거의 안 보였음(사용자가 지적한 가시성 문제의 실제 원인) → 화살표 색을 크림 화이트로 바꾸고 `Shadow` 컴포넌트로 어두운 외곽선 추가, 크기 28→44로 확대(요청대로 과하지 않은 선에서)
  - 기본 방향 확인: `Pictoicon_Arrow_Down` 스프라이트를 회전 0으로 캔버스에 별도로 띄워 실제로 "아래쪽을 가리키는 쉐브론"이 맞는지 스크린샷으로 검증 후 회전 공식(`atan2(dir)+90`) 확정
- **검증**: Play 모드 진입 → 리플렉션으로 `TutorialManager.RevealWallLimit()` 직접 호출 → 스크린샷으로 확인, 총 3라운드(①최초 스프라이트 버전 ②가림 문제 원인 분석용 픽셀 샘플링 ③4-막대 최종 버전) 반복. 세션 내내 에디터가 포커스를 잃어 Play 모드 시간이 거의 멈춰있어서(기존에 문서화된 함정), 매번 실제로 몇 초씩 대기한 뒤 스크린샷을 찍어야 애니메이션이 어느 정도 진행된 상태를 볼 수 있었음. 최종 상태(3라운드 스크린샷 기준): 골드 사각 테두리가 6개 아이콘 폭에 살짝 여유를 두고 감싸며 아이콘이 훤히 들여다보이고, 화이트 화살표가 테두리 오른쪽 아래 모서리 바깥에서 대각선으로 안쪽을 가리키는 모습 확인. 콘솔 에러 없음(무관한 `NoSubscription`만 존재)
- **후속 미세조정(같은 세션)**: 4-막대 버전 확인 후 사용자가 "좌우로 조금만 더 키워서 아이콘을 전부 감싸면 좋겠다"고 요청 → `padding.x` 24→36(세로는 20 유지). 크롭한 스크린샷으로 6개 아이콘이 여유 있게 감싸지는 것 확인(화살표는 `targetSize` 기반 계산이라 패딩 변경만으로 자동으로 같이 밀려남, 별도 코드 수정 불필요)
- **✅ 사용자 최종 확인 완료(2026-09-15)**: 위 패딩 조정 스크린샷을 보여드린 뒤 "좋다" 확답 받음. 이 작업은 더 손댈 필요 없음
- **수정 파일**: `UIHighlightEffect.cs`(전면 재작성 — `ringSprite` 제거, 4-막대 자체 렌더링, `GetCenterWorld`, `PlaceArrowAtBottomRightCorner`), `CurrencyUI.cs`(`ActiveContentRect` 추가), `TutorialManager.cs`(`RevealWallLimit`에서 강조 대상 변경 + 강제 레이아웃 리빌드), 씬(`Stage1(Test).unity`): `SlotContainer`에 `ContentSizeFitter` 추가, `Managers`의 `UIHighlightEffect` 인스턴스 필드값 갱신(padding=(24,20), arrowSize=44, arrowColor=크림 화이트 등)

## 알아두면 좋은 것 (재발 가능한 기술적 함정)

- ⚠️(2026-09-16 같은 날 안에 해소됨, 기록만 남김) 한때 `FogOfWarGround`가 `Floor`/`FogGround`의 실제 Transform 스케일과 무관하게 자기 자신의 직렬화 필드 `worldSize`/`worldCenter`로 월드→안개 텍스처 UV 변환을 계산해서, 바닥 스케일만 바꾸고 이 필드를 안 맞추면 시야 원이 타원으로 찌그러지는 문제가 있었음. 같은 날 안개 시스템을 셰이더 기반(`Custom/FogOfWarShroud`, 월드 좌표를 프래그먼트에서 직접 계산)으로 전면 교체하면서 `worldSize`/`worldCenter` 필드 자체가 없어져 **이 문제 계열은 이제 구조적으로 발생할 수 없음**(자세한 내용은 아래 "안개 시스템 전면 재설계" 항목 및 위쪽 날짜별 기록 참고). 바닥류 오브젝트를 새로 만들 때 "크기와 무관하게 계산되는 방식인지" 한 번씩 확인하는 습관 자체는 여전히 유효함
- uGUI 버튼 클릭이 안 먹으면 씬에 `EventSystem`(+ `InputSystemUIInputModule`)이 있는지부터 확인 — 이 프로젝트는 New Input System 전용이라 레거시 `StandaloneInputModule`/`OnMouseDown`은 동작 안 함
- 마우스 클릭 입력은 항상 `Mouse.current`(New Input System) 기반으로 구현할 것
- Built-in `Standard`/`Standard (Specular)` 셰이더를 쓰는 임포트 에셋은 URP에서 분홍색으로 보임 → `Universal Render Pipeline/Lit`로 변환 필요(새 에셋 임포트 시 항상 확인). 레거시 파티클 셰이더는 `Particles/Unlit`로 변환
- 외부 파티클 이펙트 에셋(예: `Particlecollection_Free samples`)을 새로 쓸 때는 셰이더 변환 말고 **`ParticleSystemRenderer.alignment`도 확인**할 것 — `Local`로 돼 있으면 일반적인(수평에 가까운) 카메라 기준으로 만들어진 것이라, 이 게임의 탑다운 카메라에서는 파티클이 화면과 거의 평행하게 누워 안 보인다(셰이더를 맞게 고쳐도 여전히 안 보임). `View`로 바꾸면 항상 카메라를 향해 보인다(2026-09-15, 무적 스킬 마법진 이펙트에서 겪음 — `SkillManager.SpawnBurst()`에 이 보정이 이미 들어가 있으니 같은 헬퍼로 재생시키면 자동 적용됨)
- 안개/지형보다 항상 위에 그려져야 하는 오브젝트는 위치를 억지로 높이지 말고 머티리얼 `renderQueue`로 순서만 조정할 것(ZTest는 유지되어 실제로 카메라에 더 가까운 오브젝트에는 정상적으로 가려짐) — HUD류처럼 절대 안 가려야 하면 `ZTest Always` 전용 셰이더도 고려
- Unity 에디터 창이 포커스를 잃으면 Play 모드 시뮬레이션이 멈춤(`Time.frameCount` 정지) — MCP로 실시간 관찰이 안 되면 리플렉션 직접 호출로 로직만 검증하는 방식으로 우회
- 고속 이동 투사체는 `OnTriggerEnter` 대신 `FixedUpdate`+`Physics.SphereCast` 사전 검사 방식을 쓸 것(터널링 방지)
- 한글 UI 텍스트는 폰트가 한글 글리프를 지원하는지 항상 확인(현재 전부 NEXON Lv1 Gothic로 통일됨)
- 코드로 Canvas를 생성하면 기본 `renderMode`가 World Space로 잡힘 — Screen Space-Overlay로 명시할 것
- `manage_components`로 머티리얼을 지정할 때 프로퍼티명은 `"material"`이 아니라 `"m_Materials"`
- `manage_material(action=create)`로 색만 지정해 새 머티리얼을 만들면 **기본이 Opaque(`_Surface=0`)** 다 — 알파값(반투명)을 쓰려는 재질이면 반드시 `_Surface=1`(Transparent) + `_Blend`/`_SrcBlend`/`_DstBlend`/`_ZWrite`까지 알파 블렌드에 맞게 직접 설정하고 렌더큐도 Transparent(3000)로 바꿔야 한다. 안 그러면 알파를 아무리 낮춰도 완전 불투명하게 렌더링된다(2026-09-15, 무적 스킬 보호막 머티리얼에서 겪음 — 알파를 여러 번 낮춰봐도 그냥 불투명한 원으로만 보여서 한참 헤맴)
- 파티클 이펙트를 스폰 후 화면에서 확인이 안 될 때는 셰이더/정렬/색만 보지 말고 **`ParticleSystem.GetParticles()`로 실제 파티클의 `size`/`position`/`color`를 직접 찍어볼 것** — 2026-09-15에 겪은 사례는 원본 에셋의 `startSize` 자체가 0.01 안팎으로 극단적으로 작게 만들어져 있어서, 오브젝트 스케일을 10배 넘게 키워도 안 보이는 경우였다(셰이더·정렬은 이미 다 정상이었음). 이런 경우는 무리하게 스케일만 올리기보다 해당 에셋이 이 프로젝트 카메라 조건에 안 맞는다고 보고 다른 방식(자체 제작 등)으로 전환하는 게 낫다
- 헤드리스 프리팹 편집(`manage_prefabs`) 도중 MCP 연결이 끊겼다 재시도되면, 그 프리팹의 인스턴스가 배치된 씬들의 좌표를 반드시 재확인할 것(한 번 실제로 좌표가 틀어진 적 있음)
- 프리팹 구조(`Model` 자식 분리 등)가 초기화되는 원인은 사용자의 수동 FBX 리임포트임(Unity/MCP 버그 아님) — 캐릭터 에셋을 다시 임포트한 뒤에는 항상 프리팹 구조부터 재확인할 것
- Play 모드 중 씬 오브젝트(위치·참조 등)를 수정해도 Play 모드 종료 시 전부 되돌아감 — 에셋(스크립트/머티리리얼) 변경만 영구 저장됨. 반드시 Edit 모드인지 확인 후 씬 오브젝트를 고칠 것
- `TutorialManager`는 팝업이 열려있는 동안(`IsOpen=true`) 새로 발생한 트리거를 그냥 무시한다 — 같은 프레임/이벤트에서 트리거 두 개가 동시에 조건을 만족하면 나중 것이 유실될 수 있음(예: `MonsterSeen`과 `TileReached`가 겹칠 때). `pendingStep` 1슬롯 대기열로 이 경우를 처리해둠(2026-09-14) — 새 트리거 종류를 추가할 때 이 패턴을 참고할 것
- `GameObject.Find`는 **비활성(inactive) 오브젝트를 못 찾는다** — UI를 처음엔 숨겨뒀다가 나중에 공개하는 구조(예: `CurrencyIcons`)를 다룰 때는 `canvasGO.transform.Find("이름")`처럼 Transform 기준으로 찾을 것. 이 프로젝트에서 여러 번 이 실수로 NRE가 났음
- UI 강조/정렬 로직에서 대상 RectTransform의 "중심"이 필요하면 `target.position`(pivot 기준)이 아니라 `target.TransformPoint(target.rect.center)`를 쓸 것 — pivot이 중앙이 아닌 UI(좌측 정렬 등)에서 `target.position`을 중심으로 오인하면 위치가 어긋난다(2026-09-15 `UIHighlightEffect`에서 실제로 겪음)
- 9-slice(`Image.Type.Sliced`) 스프라이트를 강조 테두리 등으로 재사용할 때는 **스프라이트의 `spriteBorder`(9-slice 여백)가 실제 대상 크기보다 작은지 반드시 확인**할 것 — 테두리 합이 대상 높이/너비보다 크면 렌더링이 깨지거나 아예 안 보인다(2026-09-15, `BorderFrame_Round02`(29~36px 테두리)를 46px 높이 대상에 썼다가 아무것도 안 보이는 버그로 겪음 → 11px짜리 얇은 테두리 스프라이트로 교체해 해결)
- `ContentSizeFitter`로 자식 개수에 따라 자동으로 크기가 바뀌는 UI(예: 벽 개수만큼 늘어나는 아이콘 줄)를 그 프레임에 바로 참조해야 하면(강조 표시 등) `LayoutRebuilder.ForceRebuildLayoutImmediate()`로 강제 재계산 후 읽을 것 — 그냥 두면 다음 레이아웃 패스까지 스테일된 크기가 잡힌다. 비활성 상태로 있다가 `SetActive(true)`로 막 활성화된 오브젝트는 특히 더 그렇다(그 프레임에 `Awake`가 막 실행돼 자식이 막 생성된 직후이기 때문)
- 서로 다른 GameObject에 붙은 컴포넌트끼리는 **`Awake`/`OnEnable` 실행 순서가 보장되지 않는다** — A의 `OnEnable`이 B의 `Awake`보다 먼저 실행돼 B의 배열/필드가 아직 null인 채로 참조될 수 있다(2026-09-15, `SkillBarUI.OnEnable`이 `SkillManager.Awake`보다 먼저 실행되며 NRE 발생). 다른 컴포넌트가 구독/참조할 가능성이 있는 매니저는 `Awake`에서 바로 초기화하지 말고, 실제로 값이 쓰이는 진입점(getter 등) 맨 앞에서 "이미 초기화됐나" 확인 후 필요하면 그 자리에서 초기화하는 지연 초기화 가드를 넣을 것
- `manage_material`(assign_material_to_renderer 등)은 **비활성(inactive) GameObject를 대상으로 지정하면 by_id/by_name/by_path 전부 못 찾는다**(`manage_asset`/`manage_components`는 비활성도 잘 찾는 것과 다름) — 평소 꺼둔 채로 시작하는 이펙트 오브젝트(무적 보호막 등)에 머티리얼을 배정할 때는, 먼저 잠깐 `SetActive(true)`로 켠 뒤 배정하고 다시 꺼야 한다
- `manage_components`로 **커스텀 클래스(MonoBehaviour 아님) 배열 필드에 다른 컴포넌트 참조를 넣을 때**(예: `SkillButtonUI[]` 필드), 배열 원소를 그냥 정수 인스턴스 ID로 주면 조용히 null로 들어간다 — `[{"instanceID": <id>}, ...]` 형식으로 감싸서 줘야 실제로 연결된다(단일 필드는 정수/GameObject ID만 줘도 알아서 컴포넌트를 찾아 연결해주는 것과 다름)
- 외부 UI 에셋 팩의 "Border"/"Frame" 이름이 붙은 스프라이트라고 해서 **가운데가 뚫린 테두리 라인이라고 단정하지 말 것** — 미리보기 이미지(흰 배경에 흰/밝은 색 스프라이트)만으로는 진짜 뚫려있는지 판단이 안 된다(2026-09-15, Layer Lab `BorderFrame_Round01_White1`/`Round02` 둘 다 실제로는 가운데까지 완전히 불투명한 판이라 UI를 통째로 가려버렸음). 확실히 확인하려면 텍스처를 임시로 `isReadable=true`로 바꾸고(`manage_asset` modify) `Texture2D.GetPixel()`로 중앙 픽셀의 알파값을 직접 찍어본 뒤 원래 값으로 복구할 것. 확실한 "뚫린 사각 테두리"가 필요하면 차라리 얇은 막대 4개(위/아래/좌/우, sprite 없는 기본 `Image`)를 앵커 스트레치로 직접 조립하는 게 더 안전하고 두께도 정확히 통제된다

## 확정 수치 (임의로 다시 바꾸지 말 것)

**Player(`Fox.prefab`)**: CapsuleCollider Radius 0.5/Height 1.8/Center(0,0.9,0)(로컬값, 스케일에 자동 곱해짐), NavMeshAgent Radius 0.425/Height 1.53(2026-09-16 스케일 0.85배 조정에 맞춰 수동 반영 — Collider와 달리 NavMeshAgent는 스케일에 자동으로 안 곱해짐)/BaseOffset 0/Speed 4(⚠️ 실측값. 이 문서엔 한동안 3.5로 잘못 기록돼 있었음), Tag=`Player`, **스케일(0.85,0.85,0.85)**(2026-09-16, 기존 1,1,1에서 축소)

**Monster(`Chicken.prefab`, 기본형)**: **스케일(4,4,4)**(2026-09-16, 기존 2,2,2에서 확대), CapsuleCollider Radius 0.25/Height 0.5/Center(0,0.25,0)(로컬값, 스케일에 자동 곱해짐), `MonsterTower` range=6/cooldown=1.5/damage=10/projectileSpawnHeight=1.2(스케일 확대에 맞춰 0.6→1.2)/targetAimHeight=0.765(Fox 축소(0.85배)에 맞춰 0.9→0.765, Fox 몸 높이를 조준), `targetLayerMask`=AttackTarget(8번), `visionRadius`=8(씬마다 오버라이드될 수 있음, 스케일과 무관하게 월드좌표 절대값이라 크기 변경 영향 없음 확인됨)
- `Monster_AreaType`(광역형): range=10/cooldown=1.5/damage=6, 스케일·projectileSpawnHeight·targetAimHeight는 기본형과 동일하게 조정됨
- `Monster_DPSType`(고DPS형): range=4/cooldown=0.7/damage=20, 스케일·projectileSpawnHeight·targetAimHeight는 기본형과 동일하게 조정됨

**Projectile_Egg**: 스케일(2,2,2), SphereCollider Radius 0.05(Trigger), Rigidbody kinematic, `Projectile` speed=25/lifetime=3/wallLayerMask=WallVision(9번). Layer는 반드시 `Default`(WallVision에 올라가면 자기 자신을 벽으로 오검출함)

**Wall.prefab**: Layer=`WallVision`(9번), 크기(0.2, 1.5, 2), pos.y=0.75. 구조: `Backing`(뒷판, `M_Wall.mat`) + `BrickFace`(벽돌 결합 메시, `M_BrickWall.mat`)

**레이어**: `AttackTarget`=8, `WallVision`=9, `WallSlot`=10(벽 슬롯 클릭 전용, 실제 벽과는 별개)

**벽 슬롯**: 타일 중심에서 진행 방향 기준 좌/우로 1유닛 이격, Y=0.75. 코너 타일은 "나가는 방향 구간이 쓰는 바깥쪽 가장자리"를 그대로 사용(대각선 계산 금지)

**스테이지1(`Stage1(Test).unity`)**: 재화 총량 6(벽 1개=1), `visionRadius`=**7**(2026-09-16, 8에서 1 줄임 — 안개 재작업과 함께 사용자 요청), 몬스터 4마리(`Monster_A~D`, 전부 기본형). 튜토리얼 `first_hit` 강제 슬롯=타일3 Left

**스테이지2(`Stage2.unity`)**: 재화 총량 7, `visionRadius`=**7**(2026-09-16 — 처음엔 5에서 1 줄여 4로 했었는데, 곧이어 사용자가 "시야범위는 전부 다 통일시킬거고 스테이지1 기준으로 전부 똑같이 맞춰줘"라고 해서 스테이지1과 같은 7로 재조정. 스테이지1/스테이지2/`Stage_Template` 세 씬 전부 `FogOfWarGround`·모든 `FogVisibility`·모든 `AttackRangeIndicator`가 7로 통일됨), 몬스터 6마리(고DPS/광역/기본 각 2마리, 사거리 중첩 다수). 튜토리얼은 시야 안내 1개뿐이었으나 사용자 요청으로 삭제(스테이지1과 시야를 동일하게 되돌리면서)

**안개(2026-09-16 전면 재설계)**: `FogOfWarGround`가 더 이상 텍스처에 영구히 원을 칠하는 방식이 아니라, `Custom/FogOfWarShroud` 셰이더(`M_FogOfWarShroud.mat`)로 매 프레임 플레이어(+조명탄 등 시야 소스) 위치 기준 원을 동적으로 계산한다(스타크래프트 안개 참고 — 시야 밖은 완전히 안 보이는 게 아니라 `fogColor`(기본 RGBA(0.05,0.05,0.08,0.6), 다크하지만 반투명해서 배경/길이 은은히 비침)로 덮이고, 시야를 벗어나면 즉시 다시 어두워짐). 이 방식은 바닥 오브젝트의 Transform 크기와 완전히 무관해서(프래그먼트에서 월드 좌표로 직접 계산) 오늘 낮에 겪은 `worldSize`/`worldCenter` 불일치 버그 자체가 구조적으로 재발 불가능함. `FogVisibility`(몬스터·함정 렌더러 표시)도 "한 번 보이면 영구 공개"에서 "지금 시야 안에 있을 때만 보임"으로 동작이 바뀜(`IsCurrentlyVisible` 프로퍼티 추가, 튜토리얼용 `OnRevealed`는 기존처럼 최초 1회만 발행). `RockfallTrap`의 경고 원/바위 표시도 이 변경에 맞춰 `revealedByFog`(1회성 래치) 대신 `FogVisibility.IsCurrentlyVisible`을 매 프레임 직접 읽도록 수정. `FogGround`는 이제 Floor(실제 이동 가능 영역)와 무관하게 화면에 보여야 하는 범위 전체(코어+오늘 추가한 주변부)를 덮도록 자유롭게 키워도 안전함 — 세 씬(`Stage1(Test)`/`Stage2`/`Stage_Template`) 전부 적용·저장 완료

**카메라(`CM_TopDown`)**: 회전 **(55,0,0)**(⚠️ 2026-09-16 정정 — 이 문서엔 한동안 "완전 탑다운(90,0,0)"으로 잘못 기록돼 있었으나, `Stage1(Test)`/`Stage2`/`Stage_Template` 세 씬 모두 실측하면 55도였음), FollowOffset(0,17,-12), FOV 48, PositionDamping/RotationDamping=(0,0,0)(지연 없이 즉시 추적 — 안개가 실시간 위치 기준이라 카메라 지연이 있으면 화면상 어긋나 보임). 화면에 실제로 보이는 바닥 범위(여우 기준)는 X ±23.89유닛, Z 뒤 -8.69/앞 +16.32유닛(2026-09-16, `ViewportPointToRay`+Y=0 평면 교차로 실측)

**애니메이션**: `Anicon_Fox`/`Anicon_Chicken` 공통 파라미터 `IsMove`(Bool)/`Die`(Trigger). 둘 다 Generic/No Avatar. Fox의 Die는 전용 클립이 없어 `Fox_Falling`을 임시로 사용 중. 공격 애니메이션은 없음(사용자 확정 — 투사체 발사/충돌만으로 공격 표현)

**PlayStats.csv 위치**: 프로젝트 루트(`Assets` 폴더 밖). 클리어/실패마다 한 줄씩 누적

**타일 좌표(1단계 테스트 경로, 2x2유닛 20칸 ㄴ자)**: 세그먼트A는 X=-8 고정·Z=-10→8, 세그먼트B는 Z=8 고정·X=-6→12

## 검증 게이트

`docs/design.md` 0~10단계 검증 게이트 전부 통과(2026-09-07~09-10). 특히 **3단계(★핵심: 벽으로 막으면 몬스터 공격을 안 맞는다)**는 현재 확정 방식(몬스터는 항상 발사, 투사체가 벽에 맞아 소멸) 기준으로도 재검증 완료.

---

📁 날짜별 상세 작업 경위(버그 원인 진단, 시행착오, 여러 차례 재보고된 수정 과정 전체)는 `PROGRESS_ARCHIVE.md` 참조.
