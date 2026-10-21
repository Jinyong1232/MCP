# 진행 상황

> **세션 시작 시 이 파일을 먼저 읽고** 어디까지 진행했는지 파악한 뒤 이어서 작업할 것.
> 한 단계를 마칠 때마다 이 파일을 갱신할 것.
> 전체 단계 목록은 `docs/design.md` 7번 섹션 참조.

---

## 🎯 다음 세션에 할 일 (2026-09-11 세션 종료 시점 최종 정리)

**design.md 0~10단계는 2026-09-10에 전부 완료.** `docs/polish.md` 순서(P1→P6)로 진행 중이며, 그중 UI 작업은 `docs/ui-design.md`가 정한 U1→U6 순서를 따로 따른다.

### 지금 당장 다음에 할 일

**→ U5(결과 화면 재구성, `ui-design.md` 4장) 진행.** 세션 시작하면 사용자에게 "U5 진행할까요?" 확인부터 하고 시작할 것(직전 세션이 U4 완료 보고 후 여기서 끝났음).

U5에서 참고할 것:
- ui-design.md 4-1(클리어): 항목이 위에서부터 0.2초 간격 순차 등장, 숫자는 카운트업, "사용한 벽이 적을수록 강조색" 요소, 별 등급은 자리만 비워두기(구현 안 함)
- ui-design.md 4-2(실패): 화면이 서서히 회색으로, 진행도 바를 그대로 보여주기, 피격 지점 표시(가능하면)
- 현재 결과 화면(`ResultText`/`RetryButton`)은 U1에서 폰트만 바꿨을 뿐 레이아웃은 그대로 — U4처럼 기존 오브젝트를 재구성하되 삭제하지 말 것
- U5 다음은 U6(애니메이션 폴리시: 바 보간·카운트업·슬롯 전환 — 이미 HUD 자체엔 일부 적용됨, U6은 결과 화면 쪽 마무리 성격)

### UI 작업 진행 상황 (`docs/ui-design.md` U1~U6)

| 단계 | 상태 |
|---|---|
| U1 폰트 교체 | ✅ 완료 — 전체 TMP 마이그레이션 + NEXON Lv1 Gothic |
| U2 Canvas Scaler/앵커 | ✅ 완료 — 1920×1080, ScaleWithScreenSize, Match 0.5 |
| U3 9-slice | Layer Lab 에셋이 이미 9-slice 상태라 별도 임포트 작업 없이 U4에서 바로 사용함 |
| U4 인게임 HUD 재구성 | ✅ 완료(2026-09-11) — 아래 "U4 인게임 HUD" 상세 항목 참조 |
| U5 결과 화면 재구성 | ❌ 미착수 — **다음 작업** |
| U6 애니메이션 폴리시 | ❌ 미착수 |

### 사용자가 확정한 사항 (임의로 뒤집지 말 것)

- 캐릭터 에셋(Fox/Chicken) 교체 안 함 — 배경/환경 에셋만 확장
- `visionRadius` 관련 수치는 사용자가 먼저 말하기 전까지 절대 손대지 않음
- 게임 제목: "여우는 죄가 없다" (메인 메뉴 `TitleText`)
- 메인 메뉴 제목/버튼은 **절차적(procedural) 룩 유지** — Layer Lab GUI 팩으로 한 번 교체했다가 "화려하고 배경과 안 어울린다"는 피드백으로 원복함. **인게임 HUD만 Layer Lab 에셋 사용**, 메인 메뉴는 건드리지 말 것

### 보류/선택 작업 (사용자가 필요하다고 할 때)

- `PlayStats.csv` 데이터 수집(P4, 사람이 직접 여러 번 플레이) — 현재 1줄만 쌓여 있음
- 데이터 쌓인 뒤 10.4 기준(재화 0~1개, 체력 30~70%, 반응시간 1.5~3초)과 비교해 수치 재조정(P5)
- `docs/tutorial.md` 5장 연출(P6)
- 사용자가 예고한 기상(weather)/환경 확장 기능(`Low Poly Nature Bundle` 기반, 구체 범위는 아직 미정 — 사용자가 지정할 것)

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
- **이 문서의 기록 중 "SampleScene"을 언급하는 부분은 전부 지금의 `Stage1(Test).unity`를 가리킴** — 과거 기록이라 이름을 소급 수정하지 않음, 다음 세션에서 혼동 주의

## 현재 단계

**design.md 0~10단계 전부 구현 완료(2026-09-10), 현재는 `docs/polish.md` P1 완료 상태(2026-09-11).** 프로젝트는 "플레이 가능한 완성된 프로토타입"이며, 지금은 폴리싱 단계(P1~P6) 진행 중.

**오늘 완료한 것 요약** (자세한 경위·버그 수정 과정은 아래 "완료한 작업"의 날짜별 항목 참조 — 특히 튜토리얼 벽 슬롯 클릭 문제는 여러 차례 재보고·재수정을 거쳤으니 시간순으로 읽을 것):
1. **6단계** — 스테이지1 몬스터 배치(사용자 직접 배치, `Monster_A~D`)
2. **7단계** — 몬스터 종류 확장: `Monster_AreaType`(광역형)·`Monster_DPSType`(고DPS형) 프리팹 생성, 빨강/파랑 임시 색상 구분
3. **8단계** — 스테이지2를 별도 씬(`Stage2.unity`)으로 분리, 3종 혼합 배치(중첩 구간 다수)·좁은 시야(5)·빠듯한 재화(7)로 구성, 재도전 리셋 로직(`StageManager.RetryStage()`)
4. **설계 외 추가** — `docs/tutorial.md` 기획에 맞춘 `TutorialManager`(트리거 5종, 강제 슬롯 클릭, 스테이지별 시퀀스) 신규 구현. 실제 클릭 테스트 과정에서 발견된 버그 다수 수정(오버레이가 3D 월드를 가려서 슬롯이 안 보이던 문제, 튜토리얼 강제 슬롯이 하드코딩이라 이미 지나간 타일을 가리키던 문제, 코너에서 벽 슬롯이 길목에 겹치던 문제 등)
5. **9단계** — 재도전 버튼 UI 연결. 씬에 `EventSystem`이 아예 없어서 uGUI 버튼 클릭이 전혀 반응 안 하던 근본 원인 발견·수정(두 씬 모두)
6. **10단계** — `PlayStatsLogger` 신규 작성. 재화 밸런스(검증 의도①)·정보 제한/반응시간(검증 의도②) 지표를 클리어·실패 시점마다 프로젝트 루트 `PlayStats.csv`에 CSV 한 줄로 누적 기록. 스테이지1은 튜토리얼 개입 때문에 반응시간 측정 제외(design.md 10.3 지침)
7. **기타 버그 수정** — 사망한 Player를 몬스터가 계속 공격하던 문제(reference.md 명세 "사망한 대상 제외" 누락분 보완)

**남은 것**: `PlayStats.csv` 데이터를 실제로 여러 번 플레이해서 쌓고 10.4 기준(재화 0~1개, 체력 30~70%, 반응시간 1.5~3초)과 비교하는 것과 영상 촬영은 사람이 진행. 그 외는 위 "다음 세션에 할 일" 참조

---

## 완료한 작업

> 아래는 날짜순(최신이 위) 상세 기록. 같은 날 안에서도 여러 번 반복 수정된 항목(특히 튜토리얼 벽 슬롯 클릭 문제)은 등장 순서가 곧 시간순이니 참고할 것.

### 메인 메뉴 배경을 3D 환경 씬으로 교체 + 제목 변경 (2026-09-11)

- 사용자 요청: 제목을 "여우는 죄가 없다"로 변경, 배경을 더 밝게, 단색 대신 배경 에셋을 활용해 게임 화면처럼 꾸며달라
- `TitleText` 문구 변경(+ 길어진 글자 수에 맞춰 박스 폭 1000→1400, 폰트 크기 110→90)
- **메인 메뉴에 실제 3D 환경을 구성**(기존 단색 `Background` Image는 제거):
  - `Directional Light` 신규 추가(스테이지와 동일한 각도 50/330/0) — 기존엔 조명이 아예 없어 3D 오브젝트를 넣어도 안 보였을 것
  - `Floor`(Plane, `M_Floor.mat` 잔디색, 22×22유닛) 신규 배치
  - `MenuEnvironment` 컨테이너 아래 나무·바위·식생 14개를 P2 환경 데코 때와 동일한 방식(에셋별 실측 후 스케일 역산: 나무 3.5~5.5, 바위/식생 0.6~1.4)으로 무작위 배치
  - `Fox.prefab` 인스턴스(`MenuFox`)를 전경에 세워뒀다가, 사용자 요청으로 다시 제거함("여우는 빼고 배경으로만 가자") — 최종적으로 메인 메뉴엔 배경(바닥+나무+바위+식생)만 남음
  - 카메라를 쿼터뷰로 재배치(위치 (0,6.5,-8), 피치 28도, FOV 55), Clear Flags는 Solid Color 유지하되 하늘색을 밝게(`(0.65,0.82,0.95)`)로 바꿔 바닥 밖 영역도 밝아 보이게 함
- **검증**: Play 모드 스크린샷으로 밝은 하늘색+잔디 위에 나무/바위/여우가 자연스럽게 어우러지고, 제목·버튼과 겹치지 않는 것 확인. 콘솔 에러 없음. 씬 저장 완료
- `docs/text-editing-guide.md`의 제목 예시 문구도 최신값으로 갱신
- 이후 사용자 피드백("여우는 빼고 배경으로만", "제목 가시성이 안좋다") 반영: `MenuFox` 제거, `TitleText` 색상을 골드/앰버(`(1, 0.82, 0.25)`)로 바꾸고 진한 갈색 아웃라인(인스턴스 전용 머티리얼, 공용 폰트 머티리얼은 안 건드림) 추가 — 하늘색·잔디 배경 둘 다에서 잘 읽히는지 Play 모드로 확인 완료
- **추가 피드백("제목에 테두리를 살짝, 배경이랑 어울리게")**: `TitleText` 뒤에 둥근 테두리 패널(`TitleFrame`) 신규 추가. GUI 에셋이 아직 없어(U3 전) **절차적으로 생성한 9-slice 스프라이트**(`Assets/Textures/T_TitleFrame.png` — 라운드 사각형 + 테두리를 픽셀 단위로 직접 그려서 PNG로 저장, Sprite Border 40px로 9-slice 설정) 사용 — 크림색 반투명 안쪽(파치먼트/나무 팻말 느낌) + 진한 갈색 테두리로 자연 배경 톤과 어울리게 색 선정. `Image` 컴포넌트 `Type=Sliced`로 어떤 크기에서도 테두리가 안 뭉개지게 함
- **검증**: Play 모드 스크린샷으로 나무 팻말 같은 둥근 패널이 제목을 감싸고, 하늘/잔디 어디에 걸쳐도 가독성이 좋은 것 확인. 콘솔 에러 없음. 씬 저장 완료
- 추가 피드백("제목 글씨 색만 살짝 어둡게"): `TitleText` 색상을 `(1, 0.82, 0.25)` → `(0.80, 0.58, 0.12)`(더 진한 앰버/머스터드 톤)로 조정, Play 모드로 확인 후 저장

### GUI 에셋(`Layer Lab GUI Pro-CasualGame`) 반영해 메인 메뉴 UI 재작업 (2026-09-11)

- 사용자가 `Assets/Layer Lab` 폴더에 GUI 에셋 팩(`Layer Lab GUI Pro-CasualGame`, 프리팹 386개 — 버튼/프레임/팝업/슬라이더/아이콘 등)을 임포트해뒀다고 알려줌 — ui-design.md U3(에셋 스프라이트 임포트) 진행이 이제 가능해짐. "기존에 만든 것도 다시 다듬어달라"는 요청대로 **메인 메뉴의 절차적 플레이스홀더(둥근 테두리 패널·단색 버튼)를 전부 실제 GUI 에셋으로 교체**함
- **제목 리본**: `Prefabs_Component_Labels/Title_Ribbon_Orange.prefab`(별 장식 아이콘 2개 포함된 9-slice 리본 배너)을 인스턴스화해 기존에 직접 만들었던 절차적 `TitleFrame`+`TitleText`를 대체. 리본이 소유한 `Text_Title`에 NEXON Bold 폰트·"여우는 죄가 없다" 문구를 그대로 적용(리본의 기본 폰트는 한글 미지원 라틴 전용 폰트라 교체 필수였음)
- **버튼**: `Prefabs_Component_Buttons/Button01_225_BtnText_Green`(시작하기) / `..._Gray`(종료)로 기존 단색 `Image` 버튼 교체 — 9-slice 배경 + 눌림/호버 색상 트랜지션이 이미 구성되어 있어 그대로 재사용. 버튼 라벨도 NEXON Regular 폰트로 교체(마찬가지로 기본 폰트가 한글 미지원), `MainMenuController.StartGame`/`QuitGame` 클릭 이벤트 재연결
- **색 다듬기**: 제목 텍스트가 주황 리본 위에서 기존 앰버 색으로는 대비가 약해 흰색+진한 갈색 아웃라인으로 변경 — 리본과 잘 어울리면서 가독성 확보
- **사소한 정리**: 배경 장식(`MenuEnvironment`) 중 카메라에 너무 가깝게 배치돼 있던 꽃 하나(`Flower_a_TwoS_11`)가 종료 버튼 근처에서 파란 얼룩처럼 겹쳐 보이던 것을 발견해 배경 쪽으로 재배치
- 절차적으로 만들었던 `Assets/Textures/T_TitleFrame.png`는 더 이상 안 쓰지만 파일 삭제 금지 규칙(및 안전상 이유)에 따라 지우지 않고 남겨둠(미사용)
- **검증**: Play 모드 스크린샷으로 리본+버튼이 자연스러운 게임 UI처럼 보이는 것 확인, 시작하기 버튼 클릭 시 실제로 `Stage1(Test)`가 로드되는 것 재확인. 콘솔 에러 없음. 씬 저장 완료
- 다음 단계(U4: 인게임 HUD를 같은 GUI 팩으로 재구성)는 아직 진행 안 함 — 필요하면 이어서 요청할 것

### UI 작업 착수: U1(폰트 교체) + 메인 메뉴 씬 신규 + 텍스트 자유 수정 매뉴얼 (2026-09-11)

- 사용자 요청: "docs폴더에 ui-design.md 참조. 폰트는 Assets/Fonts/NEXON Lv1 Gothic_OTF_TTF 폴더에 있는거로... ui 작업을 시작하면서 게임 메뉴 화면도 같이 만들어줘... 게임 이름은 임시로 여우와 닭... 텍스트들을 자유롭게 수정할 수 있도록 간단한 매뉴얼도"
- `docs/ui-design.md` 확인 — U1~U6 순서 중 **U1(폰트 교체)만 이번에 진행**, 나머지는 "한 단계씩 보고 후 진행" 지침대로 보류

**U1 — 폰트 교체**
- TMP 필수 리소스가 프로젝트에 없어서(`TMP_Settings` 인스턴스 없음 → 폰트 에셋 생성 시 NullReferenceException) `Library/PackageCache`에 있는 `TMP Essential Resources.unitypackage`를 비대화상자로 직접 임포트해 해결(메뉴 명령 `Window/TextMeshPro/Import TMP Essential Resources`는 확인 대화상자에서 멈춰서 자동화 불가했음)
- **신규 TMP 폰트 에셋**: `Assets/Fonts/TMP/NEXONLv1Gothic-Regular SDF.asset`(본문용) / `NEXONLv1Gothic-Bold SDF.asset`(제목/결과 문구용) — `TMP_FontAsset.CreateFontAsset`로 **Dynamic 아틀라스 방식**(ui-design.md 5장 권장대로) 생성. 한글 다수 포함해 `TryAddCharacters` 테스트 통과 확인
- **기존 레거시 `UnityEngine.UI.Text` 전부 TextMeshPro(`TMP_Text`/`TextMeshProUGUI`)로 마이그레이션**: 스크립트 5개(`CurrencyUI`/`PlayerHealthUI`/`DamageNumberPopup`/`TutorialManager`/`StageManager`) 필드 타입 변경, 두 씬(`Stage1(Test)`/`Stage2`)의 Text 오브젝트 7개(`CurrencyText`/`HealthText`/`ResultText`/`RetryButton>Label`/`TutorialBodyText`/`TutorialHintText`/`TutorialTitleText`)와 `DamageNumber.prefab` 전부 컴포넌트 교체 + 스크립트 참조 재연결. 제목류(`ResultText`/`TutorialTitleText`)는 Bold, 나머지는 Regular
  - `TutorialTitleText`는 확인 결과 애초에 `activeSelf=False`(비활성)로, 현재 `TutorialManager`엔 대응 필드 자체가 없는 미사용 요소였음 — 폰트만 맞춰두고 로직은 안 건드림
- **부수 작업(텍스트 자유 수정 매뉴얼과 겹치는 리팩터링)**: 스크립트에 하드코딩돼 있던 문구를 전부 `[SerializeField] private string`로 추출 — `CurrencyUI.label`("남은 벽"), `PlayerHealthUI.label`("체력"), `TutorialManager.hintSlotClickMessage`/`hintAdvanceMessage`, `StageManager.clearMessage`/`gameOverMessage`/`gameClearMessage`/`nextStageButtonLabel`/`retryButtonLabel`/`menuButtonLabel` — 값은 기존과 동일하게 유지, Inspector에서 코드 수정 없이 바꿀 수 있게만 함
- **버그 발견·수정**: `DamageNumber.prefab`을 안 고치고 넘어갔다가 Play 모드 테스트 중 `DamageNumberPopup.Initialize`에서 NRE 발생(스크립트 필드 타입만 바뀌고 프리팹의 실제 컴포넌트는 그대로라 참조가 끊김) → 뒤늦게 발견해 프리팹도 동일하게 변환·재연결
- **검증**: 두 씬 모두 Play 모드에서 HUD("남은 벽: N", "체력: 100/100")·튜토리얼 팝업·결과 화면(`GAME CLEAR`+버튼 라벨)이 새 폰트로 정상 렌더링되는 것 확인(리플렉션으로 실제 텍스트/활성 상태 재확인, 한글 깨짐 없음). 콘솔 에러 없음(중간에 발견된 DamageNumber NRE는 수정 후 재확인해 사라짐). 두 씬 모두 저장

**메인 메뉴 씬 신규 (`Assets/Scenes/MainMenu.unity`)**
- ui-design.md 범위 밖(신규 요청)이라 별도 항목으로 진행. Canvas(Screen Space-Overlay, CanvasScaler 1920×1080/Match 0.5 — U2에서 할 예정이던 설정을 신규 씬이라 처음부터 적용)에 배경(단색, 게임 톤에 맞춘 어두운 초록빛 갈색) + 타이틀("여우와 닭", Bold) + `시작하기`/`종료` 버튼
- ⚠️ **GUI 에셋이 아직 없어(U3 전) 배경·버튼 전부 기본 `Image`(단색)로 대체함** — ui-design.md 원칙③ 기록 규칙에 따라 남김. 나중에 9-slice 에셋이 들어오면 교체 필요
- 신규 스크립트 `MainMenuController.cs` — `StartGame()`(`Stage1(Test)` 로드) / `QuitGame()`(에디터에서는 Play 모드 종료, 빌드에서는 `Application.Quit()`)
- **`StageManager.cs` 수정**: `menuSceneName`(기본값 "MainMenu") 필드 추가, 마지막 스테이지 클리어 시 버튼을 숨기는 대신 `GoToMenu()`(→ 메뉴 씬 로드)로 연결 — 지난 세션에 "메뉴 씬 생기면 연결하기로" 남겨뒀던 것을 지금 마무리함
- Build Settings에 `MainMenu.unity`를 0번(맨 앞)으로 등록(`Stage1(Test)`/`Stage2` 뒤로 밀림, 씬 이름 기반 로드라 인덱스 변경은 기존 로직에 영향 없음)
- **검증**: Play 모드에서 `시작하기` 클릭 → `Stage1(Test)` 로드 확인, 이어서 `Stage2`에서 클리어 처리 후 `메뉴로 돌아가기` 클릭 → `MainMenu` 로드까지 전체 순환(메뉴→스테이지1→스테이지2→메뉴) 확인. 콘솔 에러 없음

**`docs/text-editing-guide.md` 신규 작성**
- 위에서 필드로 뺀 문구들의 위치(어느 씬, 어느 오브젝트, 어느 컴포넌트·필드)를 전부 표로 정리 — 코드를 몰라도 Inspector만으로 게임 이름·HUD 이름표·결과 문구·튜토리얼 문구·메뉴 버튼 문구를 바꿀 수 있음
- 폰트가 TextMeshPro로 통일됐다는 것, 두 스테이지 씬을 따로 고쳐야 한다는 것도 주의사항으로 명시

### P3 — 레벨 디자인 매뉴얼 작성 (2026-09-11)

- 사용자: "다음 작업 진행하자" — `docs/polish.md` 순서대로 P3 착수
- **`docs/level-design.md` 신규 작성** — polish.md P3 형식 그대로: 작업 단위별 클릭 경로, 이론 설명 없이 방법만, 각 항목 5~6줄 내외
- 작성 전 관련 스크립트 전부 재확인(`PathManager`/`PlayerAutoMover`/`PlayerHealth`/`WallPlacementManager`/`MonsterTower`/`CurrencyManager`/`TutorialStep`/`TutorialPage`)해서 **기억이 아니라 실제 코드·씬 값**으로 작성(polish.md P3 지침 그대로 준수)
- 다룬 항목: 길(늘리기/꺾기/새로 깔기), 벽 슬롯(추가/이동/코너 겹침 규칙), 몬스터(추가/종류 변경/수치 변경/특정 구간 위협), 수치(재화/시야/체력·이동속도), 스테이지(신규 제작/튜토리얼 문구·순서/튜토리얼 강제 슬롯 위치) + 맨 뒤 오브젝트 표·새 스테이지 체크리스트
- ⚠️ "시야 범위를 바꾸고 싶다" 항목엔 최근 사용자 지침(먼저 확인받을 것)을 그대로 반영해둠
- 코너 슬롯 규칙(`Slot_09_R`)·타일 간격(2유닛)·슬롯 오프셋 공식(수직 1유닛, Y=0.75)·재화(6/7)·시야(현재 둘 다 8)·체력(100)·이동속도(3.6)·벽 비용(1) 등 전부 이번에 직접 재조회한 실제 값 사용
- 문서 자체는 검증할 게임 로직이 없어 콘솔/Play 모드 검증 대상 아님(문서 작성 작업)

### 길이 뜬 느낌 + 벽 슬롯 받침대가 길에 가려짐 + 몬스터가 지형에 파묻힘 수정 (2026-09-11)

- 사용자 제보: "길에 가려져서 벽 슬롯 받침대가 안보이는 구간들이 있고, 자세히 보면 길이 살짝 떠있는 느낌이야" + "바닥에서 지형이 살짝 높은 부분에 닭의 모습이 살짝 가려져 있는 부분들도 있는데"
- **원인 진단**: 바로 앞 작업에서 안개(`FogGround`)가 바닥 굴곡(`GroundBumps`, 최고 약 0.55)보다 높아야 해서 0.01→0.6으로 올렸는데, 그러면서 안개보다 위에 있어야 하는 길(`PathTile`)도 덩달아 0.02→0.65로 올려버렸음 — **높이만으로 "안개보다 위, 그런데 원래 위치에 가깝게"를 동시에 만족시킬 수 없는 근본적인 모순**이었고, 이게 "길이 뜬 느낌"과 "받침대가 안 보임"(붕 뜬 길 가장자리가 시야를 가림) 둘 다의 원인이었음
- **몬스터 파묻힘 원인**: 바닥 굴곡을 만들 때 길 주변만 평평하게 뚫어놨고 몬스터 위치는 고려하지 않아서, 몬스터가 길에서 떨어진 굴곡 지형 위에 그대로 서 있어 다리 아래가 지형에 묻히는 경우가 생김
- **근본적인 해결(높이 쌓기 대신 렌더링 순서로 해결)**:
  - `M_PathTile.mat`을 Transparent로 바꾸고 `renderQueue=3100`(안개 `M_Fog.mat`의 3000보다 큼)으로 지정 — **길이 항상 안개보다 나중에 그려지도록 렌더 순서로 강제**함으로써, 길의 실제 높이는 원래값(0.02, 바닥에 딱 붙은 높이)으로 되돌려도 안개 위에 항상 표시됨. `ArrivalPortal`도 같은 원리로 `M_Portal.mat` `renderQueue=3200`(길보다 더 나중)으로 설정, 로컬 Y도 원래값(Disc 0.03/Particles 0.05)으로 복구
  - 안개(`FogGround`, 0.6)는 그대로 둠 — 이건 실제로 지형 굴곡보다 높아야 하는 게 맞는 요구사항이라 문제 없음
  - **이 방식이 안전한 이유**: 안개는 원래도 Transparent라 순서 조정만으로 충분했고, 길이 렌더 순서상 나중에 그려지긴 해도 **깊이 테스트(ZTest)는 정상적으로 유지**해서 Player·벽·몬스터처럼 실제로 카메라에 더 가까운 오브젝트에는 여전히 정상적으로 가려짐(즉 "안개만 무시하고 나머지는 정상 처리") — Play 모드에서 Player가 길 위에 서 있어도 발이 이상하게 파묻히지 않는 것 확인해 이 부분 회귀 없음을 검증함
- **몬스터 파묻힘 해결**: `GroundBumps`용 높이 계산에 **각 몬스터 위치 기준 반경 2.4유닛 평탄화**를 길 회랑과 동일한 방식(스무스스텝 블렌드)으로 추가. 스테이지마다 몬스터 배치가 달라서 **씬별로 별도 메시**(`Assets/Mesh/M_GroundBumps_Stage1.asset`, `M_GroundBumps_Stage2.asset`)를 새로 생성해 각 씬의 실제 몬스터 좌표를 반영함(기존 공유 메시 `M_GroundBumps.asset`는 더 이상 안 씀, 삭제는 안 하고 미사용 상태로 둠)
- ⚠️ **에디터 실수 기록**: 첫 시도 때 실수로 Play 모드 상태에서 씬 오브젝트(길 위치, 지형 메시 참조)를 수정했다가 Play 모드 종료 시 전부 되돌아가버린 것을 뒤늦게 발견 — Edit 모드인지 `Application.isPlaying`으로 반드시 확인 후 재작업함. 이후 스크립트/머티리얼처럼 **에셋 단위 변경은 Play 모드여도 영구 저장되지만, 씬 오브젝트 변경은 Edit 모드에서 해야 저장됨**을 다시 확인
- **검증**: 두 씬 모두 Play 모드에서 ①길이 바닥에 딱 붙어 보이는 것 ②길 양옆 받침대가 안 가려지고 뚜렷이 보이는 것 ③아직 하나도 안 밟은(안개 안 걷힌) 구간의 길·도착 포탈도 여전히 정상적으로 보이는 것(렌더 순서 방식이 실제로 "항상 보임" 요구사항을 만족시키는지 핵심 검증) ④Player가 길 위에 서도 발이 파묻히지 않는 것(회귀 없음) ⑤몬스터 근처 지형이 평평해져 몸체가 파묻히지 않는 것을 스크린샷으로 확인. 콘솔 에러 없음. 두 씬 모두 저장

### 몬스터 사거리 표시 레이어 수정 + 벽 슬롯 받침대 신규 + 벽돌 색상 검증/수정 (2026-09-11)

**① 몬스터 사거리 표시가 지형에 가려지는 문제**
- 사용자 제보: "몬스터 공격범위가 지형지물에 가려져서 안보일때가 있어"
- **원인**: `AttackRangeIndicator`가 원을 그리는 높이는 `transform.position.y + heightOffset(0.05)` 고정값인데, 바로 앞서 추가한 `GroundBumps`(바닥 굴곡, 최고 약 0.55)가 이보다 훨씬 높아져서 원의 일부가 굴곡진 지형 아래로 파묻히는 상황이 생김
- **수정**: 지형 높이를 따라가게 만드는 대신, **`M_RangeIndicator.mat`에 깊이 테스트를 무시하는 전용 셰이더를 새로 만들어 적용** — 신규 `Assets/Shader/RangeIndicatorOverlay.shader`(URP 호환 Unlit, `ZTest Always`/`ZWrite Off`)로 교체, 어떤 지형·장식물이 있어도 항상 위에 그려지게 함(향후 굴곡이 더 생기거나 큰 바위가 추가돼도 다시 깨지지 않는 근본적인 해결)
- 이 머티리얼은 모든 몬스터가 공유하므로 한 곳만 고치면 두 스테이지 전체에 자동 적용됨
- **검증**: Play 모드 스크린샷으로 사거리 원이 지형 굴곡 위에 항상 온전히 표시되는 것 확인. 콘솔 에러 없음

**② 벽 슬롯 받침대(길/배경 경계 + "여기에 벽을 세울 수 있다"는 지지대 느낌) 신규 추가**
- 사용자 요청: 길 바깥 벽 슬롯 위치에 길과 배경의 경계 역할 + 벽이 세워질 지지대 느낌의 오브젝트가 있으면 좋겠음 — 적당한 에셋 있으면 만들어 적용, 없으면 알려달라고 함
- 확인 결과 기존 `WallSlot`은 순수 데이터(콜라이더+스크립트)만 있고 **시각 요소가 전혀 없었음**
- 이 번들엔 딱 맞는 기성 "펜스/받침대" 프리팹은 없었지만, **`Rocks/Rock Assets/.../Tiles`의 납작한 디딤돌형 바위(`Rock_Tile_m_16`/`_17`, 원본 그대로 이미 납작한 판석 모양)가 정확히 이 용도에 맞음**을 확인해 이걸로 제작함
- **신규 메시 `Assets/Mesh/M_SlotPedestal.asset`**: 두 판석을 나란히 결합(폭 1.9·두께 0.45·높이 0.14 정도의 낮은 연석) — 벽돌 벽과 동일한 "여러 조각을 합쳐 메시 1개로" 기법 재사용
- **신규 프리팹 `Assets/Prefabs/SlotPedestal.prefab`**: 위 메시 + `Rocks_Snow.mat`(연한 회색 돌색, 벽의 벽돌색과 구분되는 중립 톤), 콜라이더 없음(순수 장식, 벽 슬롯의 레이캐스트 판정을 방해하지 않음)
- **씬 배치**: 두 씬 모두 `FindObjectsByType<WallSlot>`로 40개 슬롯을 전부 찾아 각 슬롯의 XZ 위치(Y는 0으로, 슬롯 자체의 Y=0.75는 벽 중심 높이라 받침대에는 안 씀)·회전을 그대로 복사해 `SlotPedestals` 컨테이너 아래 배치
- **검증**: 에디터·Play 모드 스크린샷으로 길 양옆에 낮은 돌 연석 줄이 생겨 길과 배경(잔디)의 경계가 뚜렷해진 것 확인. 콘솔 에러 없음. 두 씬 모두 저장

**③ 벽돌 벽 색상 검증**
- 사용자 제보: "벽이 벽돌 느낌으로는 됐는데 색이 좀 어색한거 같아 검증 한번 해줘"
- **검증 결과**: 지난 작업에서 쓴 `Brick_01~05` 메시가 참조한 `Rocks_Snow.mat`의 UV 영역이 원색 그대로 **꽤 진한 주황색**이었음(과일석 대신 비비드한 오렌지) — Demo_07이 실제로 쓴 `Rock_Block`류(흰색·회색 톤)와는 다른 부분을 참조하고 있었던 것이 원인. 직접 비교 스크린샷으로 확인(`Rock_Block_m_03`은 흰색에 가까운데 `Brick` 메시만 주황이었음)
- **수정**: `Rocks_Snow.mat`을 복제한 전용 머티리얼 **`Assets/Materials/M_BrickWall.mat`** 신규 생성, `_BaseColor`를 `(0.62, 0.5, 0.42)`(차분한 적갈색 톤)로 틴트해 채도를 낮춤 — 원본 `Rocks_Snow.mat`은 다른 바위 데코에서 계속 그대로 쓰이므로 건드리지 않고 별도 머티리얼로 분리함
- `Wall.prefab`의 `BrickFace` 자식 머티리얼을 이걸로 교체(프리팹 단위라 두 스테이지 자동 적용)
- **검증**: 임시 인스턴스로 배치해 스크린샷 비교 — 기존 비비드 주황 → 차분한 벽돌색으로 톤 다운된 것 확인. 콘솔 에러 없음

### 바닥 굴곡·잔디 색상 + 벽돌 벽 (Demo_01/Demo_07 참고) (2026-09-11)

- 사용자 요청: "바닥을 Demo_01 참고해서 굴곡 있게(길 제외) + 좀 더 잔디/얼룩 느낌으로", "벽을 Demo_07처럼 벽돌 느낌으로"
- **Demo_01/Demo_07을 직접 열어 원본 방식 확인**(둘 다 조사 후 저장하지 않고 `EditorSceneManager.OpenScene`으로 원래 씬으로 복귀해 vendor 데모 씬은 건드리지 않음):
  - Demo_01의 "얼룩" 있는 굴곡 바닥은 텍스처가 아니라 **울퉁불퉁한 지형 메시 자체의 각진 면(flat/faceted shading)이 빛을 다르게 받아서 생기는 효과**였음(재질은 텍스처 없는 단색 `MT_Terrain_01.mat`, 색상 `(0.573,0.773,0.349)`)
  - Demo_07의 "벽돌" 벽은 특수 벽돌 텍스처가 아니라 **작은 `Rock_Block`류 메시를 여러 개 쌓아 벽처럼 배치한 것**(재질은 기존에 이미 URP로 변환해둔 `Rocks_Snow.mat` 그대로)

**바닥**
- `M_Floor.mat` 색상을 Demo_01과 동일하게 `(0.573, 0.773, 0.349)`로 변경(기존보다 더 밝고 선명한 잔디색)
- **신규 메시 `Assets/Mesh/M_GroundBumps.asset`**: 30×60 바닥 전체를 2유닛 격자로 절차적 생성, 각 삼각형마다 정점을 공유하지 않고 따로 둬서(flat shading) 저폴리 각진 음영이 나오게 함. 격자점 높이는 정수 좌표 해시 기반 랜덤(0~약 0.55)이며, **경로(20타일 ㄴ자) 중심선 기준 버퍼(2.2유닛) 안쪽은 완전히 평평(높이 0)** — 스무스스텝으로 버퍼 경계에서 자연스럽게 굴곡이 시작되도록 블렌드
  - `GroundBumps`라는 새 오브젝트(콜라이더 없음, Default 레이어)로 두 씬에 배치 — **기존 `Floor`(NavMeshSurface·MeshCollider 보유, 실제 이동/충돌 담당)는 전혀 건드리지 않음**, 순수 시각용 오버레이라 NavMesh 재베이크 불필요(재베이크 안 함, Player 이동 영향 없음 확인)
  - ⚠️ 처음엔 `FogVisibility`를 붙여서 안개로 가리려 했으나, 이 컴포넌트는 "물체 하나의 피벗과 Player 사이 거리"로 판정하는 방식이라 30×60 전체를 덮는 거대한 단일 메시에는 안 맞음(피벗이 원점이라 원점 근처에 있을 때만 전체가 켜지는 식으로 오작동) → **제거하고 대신 기존 안개(`FogOfWarGround`) 뒤에 완전히 가려지도록 높이 레이어를 재조정**하는 방식으로 변경(아래)
  - **높이 레이어 재조정**(굴곡 최고점이 기존 안개보다 높아서 안개 밖으로 튀어나와 보이던 문제 수정): `FogOfWarGround` Y 0.01→**0.6**, `PathTile_00~19` Y 0.02→**0.65**, `ArrivalPortal` 프리팹의 `Disc`/`Particles` 로컬 Y 0.03/0.05→**0.66/0.68** (Floor 0 < GroundBumps 0~0.55 < FogGround 0.6 < PathTile/Portal 0.65~0.68 < Wall 0.75 순서로 재정렬). 두 씬 모두 동일 적용, `ArrivalPortal`은 프리팹 수정이라 자동 반영됨
- **검증**: Play 모드에서 Player를 안개 밖 지점으로 워프시켜 상공 스크린샷으로 각진 저폴리 음영이 뚜렷이 보이는 것 확인(1차 시도는 진폭 0.22·1유닛 격자로 너무 매끈해서 안 보였음 → 격자 2유닛·진폭 0.55·정수 해시 기반 랜덤으로 재조정 후에야 Demo_01과 비슷한 느낌이 남). 경로는 각 씬에서 여전히 완전히 평평하게 보임. 콘솔 에러 없음. 두 씬 모두 저장 완료

**벽**
- **`Assets/Mesh/M_BrickWall.asset` 신규**: `Rocks/Rock Assets/.../Bricks/Brick_01~05.fbx`(실제 벽돌 비율, 원본 약 0.255×0.075×0.10) 18개를 6행×3열로 배치해 하나의 메시로 합침(`Mesh.CombineMeshes`) — 벽 하나당 벽돌 18개가 있어도 런타임엔 메시 1개(드로우콜 1회)라 무겁지 않음
- **`Wall.prefab` 구조 변경**: 루트는 `BoxCollider`만 유지(size를 직접 `(0.2,1.5,2)`로 지정, `WallVision` 레이어 그대로) + 자식 2개 — `Backing`(기존 회색 큐브, `M_Wall.mat` — 벽돌 틈으로 배경이 비치지 않게 막는 뒷판 역할) + `BrickFace`(벽돌 결합 메시, `Rocks_Snow.mat`, 살짝 더 두껍게(0.22) 튀어나오게 배치해 입체감을 줌)
- `WallPlacementManager`는 `Instantiate(prefab, position, rotation)`으로만 벽을 생성하고 내부 구조에 의존하지 않는 것을 스크립트로 먼저 확인한 뒤 진행함(안전하게 구조 변경 가능함을 확인)
- **검증**: 씬에 임시 인스턴스로 배치해 스크린샷 확인 — 주황빛 벽돌이 여러 줄로 쌓인 벽 형태로 뚜렷이 보임(Demo_07과 유사한 느낌). 임시 인스턴스는 확인 후 삭제. 프리팹 단위 변경이라 두 스테이지 모두 자동 적용됨. 콘솔 에러 없음

### 스테이지2 시야를 스테이지1과 동일하게 되돌림 + 관련 튜토리얼 제거 (2026-09-11)

- 사용자가 스테이지2 시야가 좁은 이유(design.md 3.7/8단계 의도적 밸런스)를 물어봐서 설명함 → **"일단 내가 말하기 전에는 시야 범위 건들지마, 시야를 stage1이랑 똑같이 만들어주고 2stage 처음부분에 시야가 좁아진다는 튜토리얼은 없애줘"**라고 명확히 지시함
- ⚠️ **새 지침 기록(메모리)**: 앞으로 사용자가 명시적으로 말하기 전에는 `visionRadius` 관련 수치를 먼저 손대지 말 것
- **`Stage2.unity` 변경**:
  - 몬스터 6마리(`Monster_DPS_1/2`·`Monster_Area_1/2`·`Monster_Basic_1/2`) 전부 `FogVisibility.visionRadius`·`AttackRangeIndicator.visionRadius`를 5 → **8**로 변경(스테이지1과 동일)
  - `FogOfWarGround.visionRadius`도 5 → **8**로 변경
  - `TutorialManager.steps`에서 `stage2_vision_limited`(트리거=StageStart, "이제 시야가 좁아집니다. 멀리 있는 위협은 가까이 가야 보입니다.") 스텝 삭제 — 배열이 비어 스테이지2엔 이제 튜토리얼 팝업이 하나도 없음
- **검증**: 콘솔 에러 없음, 씬 저장 완료. Play 모드 실측 재검증은 안 함(단순 수치 변경 + 튜토리얼 스텝 삭제라 리스크 낮다고 판단, 필요하면 요청 시 재확인)
- ⚠️ 참고: `CurrencyManager.totalCurrency=7`(스테이지1=6)은 이번 요청과 무관해 그대로 둠 — 시야만 되돌렸을 뿐 재화·몬스터 배치 등 다른 난이도 요소는 안 건드림

### 도착 지점 포탈 이펙트 신규 추가 (2026-09-11)

- 사용자 요청: "도착 지점에 '이 곳은 도착 지점이다' 같은 포탈 효과를 줄 수 있어?"
- 확인해보니 도착 지점은 원래 시각 오브젝트가 전혀 없었음 — `StageManager.Update()`가 `pathManager.GetTilePosition(TileCount-1)`과 Player 거리만 비교하는 순수 판정이라 "여기가 도착 지점"이라는 표시가 지금까지 하나도 없었음(design.md 6단계 "도착 지점 트리거 배치"는 실제로는 트리거 오브젝트 없이 거리 계산으로만 구현돼 있었음)
- **신규 스크립트 `ArrivalPortalEffect.cs`** — 원판(`disc`)을 매 프레임 Y축으로 천천히 회전시키고, `Sin` 곡선으로 스케일을 은은하게 맥동(숨쉬듯 커졌다 작아짐)시킴. `StageManager`의 도착 판정과는 완전히 무관한 순수 시각 효과(클리어 판정 로직은 그대로 거리 기반)
- **신규 머티리얼**: `M_Portal.mat`(URP Unlit, Transparent, 청록색 반투명 `(0.2,0.85,1,0.55)`), `M_PortalSpark.mat`(URP Particles/Unlit, Additive 블렌드, 밝은 하늘색)
- **신규 프리팹 `Assets/Prefabs/ArrivalPortal.prefab`**:
  - `Disc`(Cylinder 프리미티브를 얇게 눌러 원판 모양으로, Collider는 제거 — 순수 비주얼이라 클릭/충돌 판정에 관여하면 안 됨) + `M_Portal` 머티리얼
  - `Particles`(ParticleSystem, 원형 Shape로 위쪽을 향해 반짝이는 파티클을 지속 방출 — 수명 1.6초, 초당 18개, `ColorOverLifetime`으로 나타났다 사라지는 페이드 적용) + `M_PortalSpark` 머티리얼, Additive라 겹쳐도 밝게 빛나 보임
  - 루트에 `ArrivalPortalEffect` 부착, `disc` 필드에 `Disc` Transform 연결
- **씬 배치**: 두 씬(`Stage1(Test)`/`Stage2`) 모두 마지막 타일(Tile_19, `(12,0,8)`) 위치에 `ArrivalPortal` 인스턴스 배치
- **의도적으로 `FogVisibility`는 붙이지 않음** — design.md 3.7 "경로 자체는 항상 보인다"와 동일한 선상에서, 이미 항상 보이는 길의 끝에 있는 도착 표시라 굳이 안개로 가릴 필요가 없다고 판단(몬스터·사거리처럼 숨겨야 할 "위협 정보"가 아님). 다르게 원하면(예: 근처에 가야만 보이게) 알려주면 됨
- **검증**: Play 모드에서 `NavMeshAgent.Warp`로 Player를 도착 지점까지 이동시켜 포탈이 실제로 드러나는 것 확인(초록 바닥·황토 길과 뚜렷이 구분되는 청록 원판), `ParticleSystem.Simulate(1f)`로 강제 진행시켜 파티클이 초당 약 18개 비율로 정상 방출되는 것 확인(`isPlaying=true`인데 `particleCount=0`이었던 건 프레임이 거의 안 지나서였고, 강제 시뮬레이션 결과 정상 작동 확인됨). 콘솔 에러 없음. 두 씬 모두 저장 완료
- ⚠️ 참고: 포탈이 Player 도착과 동시에 자연스럽게 발동하는 클리어 판정(결과 화면 뜨는 것)과 겹쳐 보이는 것도 확인함 — 순서상 문제 없음(포탈은 상시 표시, 클리어는 도착 거리로 별도 판정)

### `Low Poly Nature Bundle` 전체 머티리얼 URP 변환 (2026-09-11)

- 사용자가 임포트해온 에셋의 데모 씬을 직접 둘러보다가 "아직 분홍으로 보이는 것들이 좀 있다" 제보 — 이전(P2) 작업에서는 실제로 쓴 18개 프리팹이 참조하는 공유 머티리얼 4개만 URP로 변환해뒀었고, 번들 전체(94개 머티리얼)는 그대로였음
- **번들 전체 머티리얼 셰이더 조사**: `Assets/LMHPOLY` 아래 머티리얼 94개 전수 조사 → `Standard (Specular setup)` 58개 + `Standard` 1개(둘 다 분홍 원인) + 레거시 파티클 셰이더 5개(`Legacy Shaders/Particles/Alpha Blended`·`Particles/Standard Unlit`·`Legacy Shaders/Particles/Additive (Soft)`×2·`Legacy Shaders/Particles/Multiply (Double)` — 구름/낙엽/불꽃/연기 등 Bonus VFX용, 이것도 URP에서 분홍/미표시됨) + `Skybox/Procedural` 21개(이건 URP에서도 정상 동작하는 내장 셰이더라 손 안 댐) + 이미 변환된 `Universal Render Pipeline/Lit` 9개
- **Standard 계열 59개 → URP `Lit`**: 기존 P2 변환 로직과 동일한 방식(_MainTex→_BaseMap 등, 타일링/오프셋 유지) + 이번엔 Emission(_EmissionColor/_EmissionMap, 키워드 활성 시에만)과 Cutout/Fade/Transparent 모드(_Mode 값에 따라 _AlphaClip 또는 _Surface=Transparent+블렌드 설정)까지 함께 처리하도록 로직 보강(P2 때는 96개 중 4개만 다뤄서 전부 Opaque·비발광이라 이 부분이 필요 없었음)
- **레거시 파티클 5개 → URP `Particles/Unlit`**: 원본 셰이더별로 블렌드 모드 매칭 — `Leaves.mat`/`Snow.mat`(Alpha Blended)는 알파 블렌드, `FireRed.mat`/`FireYellow.mat`(Additive Soft)는 가산 블렌드, `Smoke.mat`(Multiply Double)는 곱연산 블렌드로 `_Blend`/`_SrcBlend`/`_DstBlend` 설정, `_Surface=Transparent` 공통 적용
- **검증**: 변환 후 전체 재조사 결과 `Assets/LMHPOLY` 아래 머티리얼이 `Universal Render Pipeline/Lit` 68개 + `Universal Render Pipeline/Particles/Unlit` 5개 + `Skybox/Procedural` 21개로 전부 URP 호환 셰이더임을 확인(합계 94개, 빠짐없음). 실제로 번들의 데모 씬(`Trees/Demo/Asset _Scenes/Tree_Assets_Demo.unity`)을 열어 스크린샷으로 나무들이 전부 정상 색상으로 보이는 것 확인 — 열어본 뒤 실수로 저장하지 않도록 `EditorSceneManager.OpenScene`으로 되돌아옴(변경사항 버림). 콘솔 에러 없음
- ⚠️ 참고: 이번 변환은 "실제로 쓴 것만"이 아니라 **번들 전체**를 대상으로 함 — 나중에 새로운 나무/바위/식생/지형 에셋을 이 번들에서 추가로 가져다 써도 이미 전부 URP로 보임(다시 변환할 필요 없음)

### 바닥·길·벽 기본 색상 정리 (2026-09-11)

- 사용자 요청: "기본 바닥이랑 길이랑 벽도 꾸며줄 수 있어?" (P2 환경 데코레이션에 이어서)
- 확인 결과 `Floor`와 `Wall.prefab`이 둘 다 URP 패키지 기본 `Lit.mat`(무채색 회색, 텅 빈 상태)을 그대로 쓰고 있었음 — 프로젝트 전용 머티리얼이 아예 없었음
- **`Assets/Materials/M_Floor.mat` 신규 생성**(URP/Lit, 초록빛 잔디색 `(0.42,0.60,0.30)`) — 두 씬(`Stage1(Test)`/`Stage2`)의 `Floor` MeshRenderer에 적용. 새로 배치한 나무·바위·식생과 어울리는 톤으로 선정
- **`Assets/Materials/M_Wall.mat` 신규 생성**(URP/Lit, 밝은 회색 돌색 `(0.55,0.55,0.58)`) — `Wall.prefab`의 머티리얼 교체(프리팹 단위라 런타임에 새로 세워지는 벽 전부에 자동 적용됨). 바닥(초록)·길(황토)과 뚜렷이 구분되어 "막는 벽"으로 잘 읽힘
- **`M_PathTile.mat` 색상만 살짝 보정**(`(0.62,0.52,0.38)` → `(0.72,0.58,0.38)`, 더 따뜻하고 밝은 흙길 톤) — 셰이더(Unlit)는 그대로 유지함(길을 항상 또렷하게 보여주기 위한 의도적 선택이었을 것으로 판단, 괜히 바꾸지 않음)
- 이번 작업은 `Floor`/`Wall.prefab`이 씬·프리팹 단위라 두 스테이지 모두 자동 반영됨(길 색상도 마찬가지)
- **검증**: 상공 스크린샷으로 초록 바닥 위에 황토색 길이 뚜렷이 드러나는 것 확인, `Wall.prefab` 임시 인스턴스를 씬에 놓고 스크린샷으로 밝은 회색 벽이 바닥/길과 잘 구분되는 것 확인 후 임시 인스턴스 삭제. 두 씬 모두 저장. 콘솔 에러 없음

### 클리어 결과 화면 분리: 클리어=다음 스테이지, 실패=다시 도전 (2026-09-11)

- 사용자 제보: "실패했을 때는 다시 도전이 뜨는 게 맞는데, 지금 성공해도 다시 도전이 떠. 성공했을 때는 다음 스테이지로 넘어가는 문구로 바꿔줘"
- **확인 필요했던 것**: 스테이지2(마지막 스테이지)는 다음 스테이지가 없어서 어떻게 처리할지 사용자에게 물음 → **답변: "나중에 메인 메뉴 씬을 추가하고, 거기서는 'GAME CLEAR' + '메뉴로 돌아가기'로 가자"** — 메뉴 씬은 아직 없으므로 지금은 스테이지2 클리어 시 문구만 띄우고 버튼은 숨김(메뉴 씬 생기면 연결할 것, 잊지 말 것)
- **`StageManager.cs` 수정**:
  - `nextSceneName`(string, `[SerializeField]`) 필드 추가 — 클리어 시 넘어갈 다음 씬 이름. 비어있으면 "마지막 스테이지"로 취급
  - `ShowResult(message, buttonAction, buttonLabel)`로 시그니처 변경 — 클리어/실패마다 버튼의 동작과 라벨을 다르게 지정할 수 있도록 함(기존엔 문구만 받고 버튼은 항상 "다시 도전"·`RetryStage` 고정이었음)
  - `HandleClear()`: `nextSceneName`이 있으면 "CLEAR" + "다음 스테이지" 버튼(`GoToNextStage`), 없으면 "GAME CLEAR" + 버튼 없음(`ShowResult`에 `buttonAction=null` 전달 시 버튼 자체를 비활성화하도록 처리)
  - `HandleFail()`: 기존과 동일하게 "GAME OVER" + "다시 도전" 버튼(`RetryStage`) — 동작 자체는 안 바뀜, `ShowResult` 시그니처만 맞춤
  - `GoToNextStage()` 신규 — `SceneManager.LoadScene(nextSceneName)`
  - `Start()`에서 하던 `retryButton.onClick.AddListener(RetryStage)` 고정 배선을 제거 — 이제 `ShowResult`가 매번 `RemoveAllListeners()` 후 그 결과에 맞는 동작을 다시 붙임. 버튼의 라벨 텍스트도 `GetComponentInChildren<Text>()`로 찾아 매번 갱신
- **씬 배선**: `Stage1(Test).unity`의 `Managers > StageManager`에 `nextSceneName = "Stage2"` 설정. `Stage2.unity`는 기본값(빈 문자열) 그대로 둠(마지막 스테이지)
- ⚠️ **Build Settings 변경(원래 P6까지 미루기로 했던 것)**: `SceneManager.LoadScene("Stage2")`는 Stage2가 Build Settings에 등록돼 있어야 에디터 Play 모드에서도 정상 동작함(안 되어 있으면 "not been added to the build settings" 에러) — 이 기능 자체가 요구하는 하드 디펜던시라 지금 등록함(`Stage1(Test).unity`/`Stage2.unity` 둘 다 Build Settings에 추가)
- **검증**: Play 모드에서 리플렉션으로 `HandleClear()` 직접 호출 → 스테이지1에서 "CLEAR"+"다음 스테이지" 버튼 확인 → 버튼 클릭(`onClick.Invoke()`) 시 실제로 `Stage2` 씬으로 전환되는 것 확인(`get_active` 결과로 재확인) → 넘어간 Stage2에서 다시 `HandleClear()` 호출 시 "GAME CLEAR"+버튼 비활성 확인 → `RetryStage()` 호출 후 `HandleFail()` 호출로 "GAME OVER"+"다시 도전" 버튼이 여전히 정상 동작하는 것과 버튼 클릭 시 실제로 리셋되는 것까지 확인. 콘솔 에러 없음. 두 씬 모두 저장 완료

### P2 — 환경 데코레이션 (캐릭터 에셋 교체 대신, 2026-09-11)

- **사용자 결정**: 몬스터 타입별 전용 캐릭터 에셋 교체는 하지 않기로 함. 대신 배경·환경 에셋만 준비해서 임포트함 — 추후 기상(weather)/환경 기능 확장의 기반으로 쓸 예정. `Fox`/`Chicken`은 계속 그대로 사용
- **임포트된 에셋**: `Assets/LMHPOLY/Low Poly Nature Bundle`(에셋스토어 자연물 번들, 약 21,300개 서브 에셋 — Modular Terrain/Rocks/Trees/Vegetation 등). 사용자가 직접 임포트함
- **머티리얼 URP 변환**: 이 번들의 공유 머티리얼 4개(`Trees.mat`, `Rocks_Snow.mat`, `Vegetation.mat`, `Terrain_08.mat`)가 전부 Built-in `Standard (Specular setup)` 셰이더였음(변환 안 하면 분홍색 — polish.md P2 경고 그대로 재현됨) → `Universal Render Pipeline/Lit`로 변환(_MainTex→_BaseMap, _Color→_BaseColor, _Glossiness→_Smoothness, _Metallic=0, 텍스처 타일링/오프셋 유지). 이 4개는 번들 전체에서 공유되는 머티리얼이라 이 변환으로 번들 내 다른 에셋을 나중에 써도 자동으로 URP로 보임
- **스테이지1·2에 환경 데코레이션 배치**: 나무 8종·바위 5종·식생(수풀/꽃/풀) 5종, 총 18개 프리팹을 골라 각 씬에 26개씩(총 52개) 무작위 배치
  - 씬마다 `Environment`라는 빈 오브젝트를 루트에 만들고 그 아래에 전부 자식으로 배치
  - **배치 제외 구역**: 경로(20타일 ㄴ자) 중심선에서 좌우 약 2.5유닛 이내, 몬스터 위치에서 2.5유닛 이내, 기존 배치물끼리 2.2유닛 이내는 제외 — 경로·슬롯·몬스터 사거리 표시를 가리지 않도록 함
  - **크기 정규화**: 이 에셋 번들은 대형 야외 지형용이라 원본 스케일이 우리 게임 스케일(타일 2유닛, Fox 약 1.8유닛)과 전혀 안 맞음(예: 기본 크기 그대로면 나무 한 그루가 20유닛에 달함) — 각 인스턴스를 실제로 배치해보고 렌더러 바운드를 측정한 뒤, **나무는 높이 기준 3.5~6유닛, 바위·식생은 최대 변 기준 0.5~1.6유닛**이 되도록 개별로 스케일을 역산해 적용(원본 스케일이 제각각이라 일괄 배율로는 해결 안 됨)
  - 회전은 Y축 무작위(0~360도)로 자연스러운 배치감을 줌
  - **레이어는 전부 `Default`로 명시 설정**(polish.md 경고대로 — `WallVision`에 올리면 장식물이 투사체를 막아버림). 어떤 프리팹도 콜라이더가 `WallVision`/`AttackTarget` 레이어에 있지 않음을 확인
  - **전부 `FogVisibility` 컴포넌트 부착, visionRadius를 씬 값에 맞춤**(스테이지1=8, 스테이지2=5) — 안 붙이면 안개 위로 튀어나와 보이는 문제(polish.md 경고) 방지. Play 모드 스크린샷으로 시야 밖에서는 완전히 안 보이고, 시야 안에 들어오면 몬스터와 동일하게 드러나는 것 확인
  - Navigation Static 표시는 건드리지 않음(경로에서 충분히 떨어뜨려 배치했으므로 NavMesh 재베이크 불필요 — 실제로 재베이크 안 함)
  - 기존 `PathTile_00~19`(임시 길 비주얼)는 이번에 손대지 않음(polish.md는 "정식 경로 비주얼이 생기면 정리 대상"이라고 했지만, 이번 데코레이션은 그 정식 비주얼이 아니므로 유지)
- **검증**: 두 씬 모두 Play 모드로 진입해 스크린샷 확인 — 나무·바위 크기가 Fox/타일과 자연스럽게 어울림, 안개 밖 데코레이션은 안 보이다가 시야 반경 안에 들어오면 드러남(스테이지1 visionRadius=8, 스테이지2=5 각각 정상 반영). 튜토리얼 진행에도 지장 없음(스테이지1 시작 팝업, 스테이지2 시야 축소 안내 팝업 정상 출력). 콘솔 에러 없음(무관한 `NoSubscription`만 존재). 두 씬 모두 저장 완료
- ⚠️ 참고: 이 번들은 나무/바위/식생 외에도 Modular Terrain(지형 타일)·구름·불꽃 파티클 등도 포함하고 있음 — 지금은 손대지 않았고, 사용자가 예고한 기상/환경 기능 확장 때 활용 가능

### P1 — 프리팹 값 덤프 + 낡은 기록 정리 (2026-09-11)

`docs/polish.md` P1 지침대로, 에셋 교체(P2) 중 프리팹이 초기화될 경우의 **복구 기준 값**을 실제 값(옛 기록이 아니라 프리팹/씬을 직접 열어 확인한 값)으로 남긴다.

**전체 점검(P1 착수 전)**: 콘솔 에러 없음(무관한 `NoSubscription` 뿐), `Assets/Fox`·`Assets/Prefabs` 6종 전부 존재해 에셋 교체 전 상태 그대로 확인. `Assets/Scenes`에 `Stage1(Test).unity`/`Stage2.unity` 둘 다 존재. `PlayStats.csv`에 이미 실제 플레이 데이터 1줄(스테이지1 CLEAR, 재화0/체력40%/벽6개)이 쌓여 있음(P4 일부 진행됨). ⚠️ **새로 발견: Build Settings에 `Stage2.unity`가 등록돼 있지 않음**(현재 `Stage1(Test).unity` 하나만 등록) — 이 상태로 빌드하면 스테이지2가 빠짐. 아직 미해결, 사용자 확인 필요.

#### 1-1. 프리팹 값 덤프 (전부 헤드리스 로드로 실측)

**`Assets/Fox/Prefabs/Fox.prefab`** (플레이어)
- 루트 `Fox`: Tag=`Player`, Layer=`Default`(0), Transform=pos(0,0,0)/rot(0,0,0)/**scale(1,1,1)**
- `CapsuleCollider`: Radius=0.5, Height=1.8, Center=(0, 0.9, 0), Direction=Y-Axis, IsTrigger=False
- `NavMeshAgent`: AgentTypeID=0, Radius=0.5, Height=1.8, BaseOffset=0, Speed=3.5, AngularSpeed=120, Acceleration=8, StoppingDistance=0, ObstacleAvoidance=High Quality
- ⚠️ **`PlayerAutoMover`/`PlayerHealth`는 이 프리팹 에셋 자체에는 없음.** 씬(`Stage1(Test)`)의 `Player` 인스턴스에서 `PrefabUtility.GetAddedComponents`로 확인한 결과 두 스크립트 모두 **인스턴스 전용 오버라이드**로만 존재함(프리팹 에셋에 Apply된 적 없음). 지금 씬 인스턴스가 유지되는 한 문제없지만, 나중에 `Fox.prefab`을 새로 인스턴스화하거나 프리팹을 리셋하면 이 두 컴포넌트가 빠진 채로 나옴 — **P2에서 프리팹을 재구성할 때 반드시 두 스크립트가 남아있는지 확인할 것**
- `Model` 자식: pos(0,0,0)/scale(1,1,1)
  - `Animator`: Avatar=`Assets/Fox/FBXs/Fox.fbx`, Controller=`Assets/Animation/Anicon_Fox.controller`, ApplyRootMotion=False
  - `fox`(메쉬) 자식: `SkinnedMeshRenderer`, Mesh=`Assets/Fox/FBXs/Fox.fbx`, Material=`Assets/Fox/Materials/M_Fox.mat`
  - `FoxTransform`(리그 본 계층, `Fox_Pelvis` 이하) — 값 생략(리깅 정보라 교체 대상 아님)

**`Assets/Prefabs/Chicken.prefab`** (기본형 몬스터)
- 루트 `Chicken`: Tag=`Untagged`, Layer=`Default`(0), Transform=pos(0,0,0)/rot(0,0,0)/**scale(2,2,2)**
- `CapsuleCollider`: Radius=0.25, Height=0.5, Center=(0, 0.25, 0)
- `MonsterTower`: attackRange=**6**, attackCooldown=**1.5**, attackDamage=**10**, targetLayerMask=256(AttackTarget), projectilePrefab=`Projectile_Egg`, projectileSpawnHeight=0.6, targetAimHeight=0.9, rotationSpeed=360
- `LineRenderer`: Material=`Assets/Materials/M_RangeIndicator.mat`
- `AttackRangeIndicator`(프리팹 기본값): visionRadius=8, segmentCount=48, lineWidth=0.04, heightOffset=0.05, displayDuration=1 — ⚠️ 실제로는 씬마다 값이 오버라이드돼 있음(1-2 참조)
- `FogVisibility`(프리팹 기본값): visionRadius=8 — ⚠️ 이것도 씬마다 오버라이드됨(1-2 참조)
- `Model` 자식: `Animator`(Controller=`Assets/Animation/Anicon_Chicken.controller`, Avatar=null/Generic)
  - `Chicken_001`(메쉬): `SkinnedMeshRenderer`, Mesh=`Assets/Mesh/Chicken.fbx`, Material=`Assets/Material/Chicken.mat`
  - `Chicken_001_rig`(리그 본 계층) — 값 생략

**`Assets/Prefabs/Monster_AreaType.prefab`** (광역형) — Chicken과 동일 구조, 값만 다름
- `MonsterTower`: attackRange=**10**, attackCooldown=**1.5**, attackDamage=**6**
- Material=`Assets/Material/Chicken_AreaType_Red.mat` (나머지 필드는 Chicken과 동일)

**`Assets/Prefabs/Monster_DPSType.prefab`** (고DPS형) — Chicken과 동일 구조, 값만 다름
- `MonsterTower`: attackRange=**4**, attackCooldown=**0.7**, attackDamage=**20**
- Material=`Assets/Material/Chicken_DPSType_Blue.mat` (나머지 필드는 Chicken과 동일)

**`Assets/Prefabs/Projectile_Egg.prefab`** (투사체)
- 루트 `Projectile_Egg`: Tag=`Untagged`, **Layer=`Default`(0) — 정상**(design.md 경고대로 `WallVision`이면 안 됨, 확인 결과 이상 없음), scale=**(2,2,2)**
- `MeshFilter`/`MeshRenderer`: Mesh=`Assets/Mesh/Projectile_Egg.fbx`, Material=`Assets/Material/Projectile_Egg.mat`
- `SphereCollider`: Radius=0.05, Center=(0, 0.0499, 0), **IsTrigger=True**
- `Rigidbody`: Mass=1, UseGravity=False, **IsKinematic=True**, CollisionDetection=Continuous Dynamic
- `Projectile`: speed=25, lifetime=3, targetLayerMask=256(AttackTarget), wallLayerMask=512(WallVision), damageNumberPrefab=`DamageNumber`

**`Assets/Prefabs/Wall.prefab`** (벽)
- 루트 `Wall`: Tag=`Untagged`, **Layer=`WallVision`(9) — 정상**, Transform=pos(0, 0.75, 0)/rot(0,0,0)/**scale(0.2, 1.5, 2)**
- `BoxCollider`: Size=(1,1,1), Center=(0,0,0), IsTrigger=False (스케일로 실제 크기 결정)
- `MeshRenderer`: 기본 큐브 메쉬, Material=URP 기본 `Lit.mat`(전용 머티리얼 없음, placeholder)

#### 1-2. 씬 배치 값 덤프 (실제 씬을 열어 리플렉션으로 확인)

**`Stage1(Test).unity`**
| 오브젝트 | 월드 좌표 | Y회전 | MonsterTower(range/cd/dmg) |
|---|---|---|---|
| `Monster_A` | (-12, 0, -1.9) | 90° | 6 / 1.5 / 10 (기본형) |
| `Monster_B` | (-8.01, 0, 11.13) | 180° | 6 / 1.5 / 10 |
| `Monster_C` | (1.99, 0, 3.81) | 0° | 6 / 1.5 / 10 |
| `Monster_D` | (5.95, 0, 3.81) | 0° | 6 / 1.5 / 10 |

- 4마리 모두 `FogVisibility.visionRadius=8`, `AttackRangeIndicator.visionRadius=8`(lineWidth=0.04)
- `CurrencyManager.totalCurrency=6`, `WallPlacementManager.wallCost=1`
- `FogOfWarGround`: visionRadius=8, textureWidth=512, fogColor=(0.4, 0.4, 0.43, 0.96)
- `TutorialManager.dimmedOverlayAlpha=0.12`, 강제 슬롯(`first_hit` step 2페이지) = **gateTileIndex=3, gateSide=Left**(기존 기록과 일치)
- `StageManager.arrivalThreshold=0.5`, `PlayStatsLogger.excludeReactionTimeThisStage=True`

**`Stage2.unity`**
| 오브젝트 | 월드 좌표 | MonsterTower(range/cd/dmg) |
|---|---|---|
| `Monster_DPS_1` | (-11, 0, -8) | 4 / 0.7 / 20 |
| `Monster_Area_1` | (-14.5, 0, -2) | 10 / 1.5 / 6 |
| `Monster_Basic_1` | (-13, 0, 4) | 6 / 1.5 / 10 |
| `Monster_DPS_2` | (2, 0, 11) | 4 / 0.7 / 20 |
| `Monster_Area_2` | (9, 0, 14) | 10 / 1.5 / 6 |
| `Monster_Basic_2` | (-4, 0, 3) | 6 / 1.5 / 10 |

- 6마리 모두 `FogVisibility.visionRadius=5`
- `CurrencyManager.totalCurrency=7`
- `FogOfWarGround`: visionRadius=5, textureWidth=512, fogColor=(0.4, 0.4, 0.43, 0.96) — Stage1과 fogColor 동일, visionRadius만 다름(정상)
- `PlayStatsLogger.excludeReactionTimeThisStage=False`

> ⚠️ Stage1의 `Monster_A/B` 좌표는 6단계 최초 배치 기록((-12,0,-4)/(-12,0,4))과 다름 — 이후 사용자가 직접 재조정한 것으로 보임(코너 슬롯 작업 등 과정에서). **위 표의 값이 현재 실제 값이며, 앞으로는 이 표를 기준으로 삼을 것.**

#### 1-3. 낡은 기록 정리

아래 5곳을 실제 구현과 맞게 수정함(각 위치에 인라인으로 "P1 정리" 표시 남김):
- 3단계 검증 게이트: "벽 있음: 발사 자체가 안 됨" → "몬스터는 항상 발사하되 투사체가 벽에 막혀 소멸"로 수정
- 0단계 검증 게이트: "Fox 재검증 필요" 표시 제거(실제 마우스 플레이로 확인 완료됨을 반영)
- `OnAttackHit` Animation Event 기록: 공격 애니메이션 자체를 안 쓰기로 확정됐으므로 무효 기록임을 명시
- 테스트 씬 Monster 좌표 `(-6,0,11)`: `Monster_A~D` 등으로 대체되어 폐기된 값임을 명시
- 프리팹 초기화 이슈: 원인 확인됨(사용자의 수동 FBX 리임포트, Unity/MCP 버그 아님)으로 갱신, 체크 완료 처리

### 10단계 — 검증 로그 수집 (`PlayStatsLogger`) (2026-09-10)
- 사용자가 9단계 관련 버그(벽 슬롯 클릭·재도전 버튼)를 실제 클릭으로 전부 확인 완료 → 9단계 마무리로 보고 design.md 10단계로 진행(9단계 끝나기 전엔 손대지 말라는 지침을 지켜 이제서야 착수)
- `StageManager`에 `OnCleared`/`OnFailed` 이벤트 추가(기존 `HandleClear`/`HandleFail` 로직은 그대로, 끝에 이벤트 발행만 추가)
- **`PlayStatsLogger.cs` 신규 작성** — 기존 스크립트를 건드리지 않고 각 매니저의 기존 이벤트(`WallPlacementManager.OnWallPlaced`, `PathManager.OnPlayerTileIndexChanged`, `FogVisibility.OnRevealed`, `StageManager.OnCleared/OnFailed`)만 구독해서 동작
  - **검증 의도① 재화 밸런스**: 클리어/실패 시점의 남은 재화·남은 체력(%)·배치한 벽 개수·위협 타일 총 개수(공개된 몬스터들의 사거리 안에 든 타일을 전부 모음, 합집합)
  - **검증 의도② 정보 제한**: 몬스터가 처음 공개된 시점(`Time.time`)부터, 그 몬스터의 위협 범위 안 타일에 벽이 처음 세워진 시점까지의 시간을 몬스터별로 기록해 평균 냄(`Time.time`은 `Time.timeScale=0`일 때 멈추므로 튜토리얼 일시정지 시간이 자동으로 안 섞임 — tutorial.md 7장이 이미 짚어둔 부분과 정확히 맞아떨어짐). 위협받은 타일인데 끝내 막지 못하고 지나친 타일 수도 카운트
  - **스테이지1은 반응시간 측정에서 제외**(design.md 10.3: "튜토리얼 개입이 많아 데이터가 오염됨") — `excludeReactionTimeThisStage` 플래그로 스테이지1만 켜둠, 이 경우 CSV에 `N/A`로 기록됨
  - CSV 파일 위치: 프로젝트 루트의 `PlayStats.csv`(Assets 폴더 밖, `Application.dataPath/..`) — 매 클리어/실패마다 한 줄씩 추가되며, 여러 번 플레이하면 계속 쌓임(design.md 10.3 "여러 번 플레이하면 줄이 쌓이게" 그대로)
- **씬 배선**: 두 씬(`Stage1(Test).unity`/`Stage2.unity`) 모두 `Managers`에 `PlayStatsLogger` 컴포넌트 추가. 스테이지1만 `excludeReactionTimeThisStage=true`로 설정, 스테이지2는 기본값(false) 그대로
- **검증**: 각 씬에서 Play 모드로 몬스터 발견 → 벽 배치 → 클리어/실패까지 실제로 돌려서 CSV에 한 줄씩 쌓이는 것을 파일로 직접 열어 확인 — 스테이지1 행은 `avgReactionTimeSec=N/A`, 스테이지2 행은 숫자값으로 정확히 구분되는 것, `remainingCurrency`·`threatenedTileCount`·`wallsPlaced` 등 나머지 수치도 실제 상황과 일치하는 것 확인. 콘솔 에러 없음. 테스트용으로 쌓인 데이터 2줄은 실제 측정 데이터와 섞이지 않도록 삭제하고 빈 파일 상태로 정리해둠(다음 실제 플레이부터 새로 쌓임)
- ⚠️ "반응시간" 계산은 지나치게 정밀하게 만들지 않음(design.md 9번 "수치는 대충 넣고 조정" 원칙) — 벽이 어느 슬롯(Left/Right)에 세워졌는지는 안 가리고, 그 타일이 해당 몬스터 사거리 안이면 무조건 "그 몬스터에 대한 대응"으로 침. 정말 그 방향을 막았는지(반대쪽에 세운 건 아닌지)까지는 검증 안 함 — 필요하면 나중에 정교화 가능
- 10.4(결과 분석·수치 조정)는 실제 데이터가 쌓여야 의미가 있으므로 사람이 여러 번 플레이한 뒤에 진행

### 부록 — 재도전 버튼이 눌러도 반응 없던 문제 수정: 씬에 EventSystem이 없었음 (2026-09-10)
- 사용자 확인: 벽 슬롯 클릭은 이제 정상 작동함. 대신 재도전 버튼을 눌러도 아무 반응이 없다는 새 제보
- **원인**: `RetryStage()` 연결·UGUI 버튼 자체는 9단계에서 이미 제대로 구현돼 있었지만, **씬에 `EventSystem`이 아예 없었음**. uGUI `Button`의 클릭(`onClick`)은 `EventSystem`(+ 이 프로젝트는 New Input System 전용이라 `InputSystemUIInputModule`)이 있어야 마우스 클릭을 감지해서 전달하는데, 이게 없으니 버튼이 화면에 보이긴 해도 클릭 자체가 씹혔던 것 — 반면 `WallPlacementManager`는 `EventSystem`을 거치지 않고 `Physics.Raycast`로 월드를 직접 쏘는 방식이라 이 문제와 무관하게 잘 작동했던 것(그래서 벽 슬롯은 되고 버튼만 안 됐던 이유)
- `Canvas`에는 `GraphicRaycaster`가 이미 있었음(클릭 감지의 절반은 갖춰져 있었음) — `EventSystem` 쪽만 빠져 있었음
- **수정**: 두 씬 모두에 `EventSystem` 오브젝트 신규 생성, `EventSystem` + `InputSystemUIInputModule` 컴포넌트 부착(레거시 `StandaloneInputModule`이 아니라 New Input System용으로 — 이 프로젝트 입력 규칙과 일치)
- 콘솔 에러 없음. 앞으로 uGUI 버튼/토글 등 클릭 기반 UI를 추가로 만들 때도 이 `EventSystem`을 그대로 재사용하면 됨(씬마다 하나만 있으면 됨)

### 부록 — 튜토리얼 ③ 재재보고: 코드는 전부 정상인데 실제 클릭이 여전히 안 됨 — 진단 로그 추가 + 타일3 Left로 이동 (2026-09-10)
- 사용자 재재보고: "또 벽 슬롯 클릭이 안돼, 다시 한번 더 체크해보고 벽슬롯위치를 타일3 left로 옮겨줘"
- **이번엔 코드/데이터를 전부 다시 훑었지만 이상을 못 찾음**: `slotLayerMask=1024`(WallSlot 레이어만), 대상 슬롯(`Slot_02_L`) 레이어 10·콜라이더 활성·트리거 아님·크기 정상, `WallPlacementManager` 컴포넌트 활성 상태, `InputLocked=false`, `RestrictToSlot` 값 정상 — 전부 확인했지만 문제 없음
- **재현 시도의 한계 확인**: 이 세션에서 리모트로 `Mouse.current.leftButton.wasPressedThisFrame`을 흉내 내려고 `InputSystem.QueueStateEvent`로 클릭 이벤트를 직접 주입해봤는데, `isPressed`는 true가 되지만 `wasPressedThisFrame`(정확히 "이번 프레임에 눌렸다"는 판정)은 Unity 플레이어 루프가 실제로 한 프레임을 다 돌아야만 갱신돼서, 원격 도구로는 "진짜 클릭"을 완벽히 재현할 수 없음이 확인됨 — 그래서 지금까지의 검증은 전부 `TryPlaceWall`을 직접 호출하는 방식(로직은 검증되지만 `Update()`의 입력 감지 자체는 실제로 못 탄 상태)이었다는 한계가 있었음
- **의심되는 원인(코드 문제가 아닐 가능성)**: 에디터 창 목록을 조회해보니 Game/Scene/Hierarchy 등 모든 창의 위치가 `x=-32000` 근처(화면 밖/최소화 상태로 보이는 좌표)로 나옴 — 이 세션의 유니티 에디터 창 자체가 정상적인 화면 포커스를 못 받는 환경일 가능성이 있음(이번 세션 내내 반복됐던 "포커스 잃으면 프레임이 안 도는" 현상과 같은 근본 원인일 수 있음). 이게 맞다면 코드를 아무리 고쳐도 입력 자체가 Unity에 정상 전달 안 되는 것이라 제 쪽에서 수정할 수 있는 범위 밖일 수 있음
- **일단 한 조치**:
  1. `WallPlacementManager.Update()`/`TryPlaceWall()`의 각 판정 단계마다 `Debug.Log` 추가(어디서 막히는지 콘솔에 그대로 남게) — 클릭했는데 안 되면 이제 콘솔에 반드시 원인이 찍힘("레이어에 아무것도 안 맞음" / "지정된 슬롯이 아님" / "이미 지나침" / "이미 벽 있음" / "재화 부족" / "InputLocked라서 무시" 등)
  2. 요청하신 대로 강제 클릭 대상을 **타일3 Left**로 이동(Monster_A 방향과 일치하는 것도 재확인함)
- 다음에 다시 안 되면, 클릭 직후 콘솔(Console 창)에 뭐라고 찍히는지 알려주시면 정확한 원인을 바로 좁힐 수 있음

### 부록 — 튜토리얼 ③ 동적 슬롯 계산을 다시 고정 슬롯으로 되돌림 (2026-09-10, 사용자 판단)
- 사용자 피드백: "기능은 잘 작동하는데 지금 벽 위치가 반대야, 어차피 이건 튜토리얼용으로 한번만 하면 되는거니까 매번 다르게 구현할 필요없이 튜토리얼용으로 한곳만 지정해놓으면 될거같아"
- **원인**: 바로 전에 넣은 "실제 피격 시점에 가장 가까운 배치 가능 슬롯을 동적으로 계산"하는 로직이, 어느 쪽(Left/Right)이 실제로 위협 몬스터를 막는 방향인지는 고려하지 않고 그냥 "가장 가까운 빈 슬롯"만 찾다 보니, 위협과 반대쪽(막아도 소용없는 쪽) 슬롯을 고를 수 있었음
- **수정**: 사용자 판단을 그대로 따라 동적 계산 로직(`PatchGateToNextPlaceableSlot`/`FindNextPlaceableSlot`)을 제거하고, `HandleFirstHit`를 단순히 `FireFirstMatchingStep`만 호출하도록 되돌림 — 이제 강제 클릭 대상은 항상 씬에 저장된 고정값 하나
- 고정값은 실제 `Monster_A` 위치(-12,0,-1.9)를 기준으로 다시 계산해서 확정: **타일2 Left**(세그먼트A에서 Monster_A에게 처음 위협받는 타일이면서, Monster_A가 있는 -X 방향과 일치하는 쪽) — 스크린샷으로 몬스터-고스트벽-경로 위치 관계를 직접 눈으로 보고 방향이 맞는지 확인함
- **검증**: Play 모드에서 첫 피격 유도 → 게이트가 정확히 타일2 Left로 고정되는 것 확인 → 상공 스크린샷으로 몬스터가 있는 쪽에 고스트 벽이 뜨는 것(반대쪽 아님) 확인 → 실제로 그 슬롯에 벽을 배치해 정상적으로 세워지고 튜토리얼이 닫히는 것까지 확인. 콘솔 에러 없음
- ⚠️ 참고(사용자가 감수하기로 한 트레이드오프): 고정 슬롯이라 만약 실제 플레이에서 첫 피격이 Monster_A가 아닌 다른 몬스터에게서 먼저 발생하거나, Player가 타일2를 이미 지나친 뒤에 피격당하면 다시 "이미 지나간 자리라 못 세워짐" 상황이 재발할 수 있음 — 사용자가 "튜토리얼은 한 번만 잘 되면 된다"는 전제로 단순함을 선택한 것이라 별도 조치는 안 함(필요해지면 그때 다시 다룰 것)

### 부록 — 튜토리얼 ③ 진짜 근본 원인 발견: 하드코딩된 슬롯이 이미 지나간 타일이었음 (2026-09-10, 오버레이 수정 후에도 재발)
- 사용자 재재보고: 오버레이를 걷어내고 고스트 벽으로 바꿨는데도 여전히 클릭이 안 됨. "해당 튜토리얼의 벽 슬롯도 다음칸으로 옮겨줘"
- **진짜 근본 원인**: `TutorialStep` 데이터에 강제 클릭 대상 슬롯을 **설계 시점에 타일2 Left로 하드코딩**해뒀었음. 그런데 "첫 피격"은 실제로 **어느 몬스터가 먼저 맞히느냐에 따라 실시간으로 달라지는 사건**이라, 실제 플레이에서 첫 피격이 일어나는 시점엔 Player가 이미 타일2(심지어 타일3)를 훨씬 지나쳐 있는 경우가 많았음. `PathManager.CanPlaceAt(tileIndex)`는 "Player 현재 타일보다 앞쪽"만 배치를 허용하므로, 이미 지나간 타일2를 아무리 정확히 클릭해도 `WallPlacementManager.TryPlaceWall`의 `CanPlaceAt` 체크에서 **조용히(에러 없이) 무시**됐던 것 — 레이캐스트·클릭 판정 자체는 완전히 정상이었는데, "이미 지나간 자리라 애초에 못 세우는 자리"를 계속 가리키고 있었던 것이 진짜 문제
- **수정(근본적으로, 사용자가 요청한 "다음 칸으로" 를 일반화)**: 슬롯을 설계 시점에 고정하지 않고, **"첫 피격" 이벤트가 실제로 발동하는 바로 그 순간 Player의 실제 현재 위치를 기준으로 동적으로 계산**하도록 변경(`PatchGateToNextPlaceableSlot`/`FindNextPlaceableSlot`) — 그 시점에 아직 지나치지 않고(`CanPlaceAt`) 비어있는(`IsSlotOccupied` 아님) 가장 가까운 다음 슬롯을 찾아 그 자리를 강제 클릭 대상으로 씀. 이제 어떤 몬스터가 먼저 맞히든, Player가 얼마나 진행했든 **항상 실제로 세울 수 있는 자리**를 가리키게 됨(하드코딩된 값이 다시 낡아버리는 문제가 구조적으로 재발하지 않음)
  - 씬에 저장된 기본값(설계 시점 폴백)도 사용자 요청대로 타일2 Left → **타일3 Left**로 한 칸 옮겨둠(다만 실제로는 위 동적 계산이 거의 항상 이 값을 덮어쓰므로 폴백용)
- **검증**: Play 모드에서 Player를 타일10까지 강제 이동시켜(타일2·3을 한참 지나친 상황 재현) 첫 피격을 유도 → 게이트가 정확히 **타일11 Left**(그 시점 기준 가장 가까운 배치 가능한 다음 슬롯)로 동적 계산되는 것과 `CanPlaceAt(11)=True`인 것 확인 → 그 슬롯에 실제로 벽 배치 호출 시 정상적으로 세워지고 튜토리얼이 닫히는 것(`IsOpen=False`)까지 확인. 콘솔 에러 없음
- 스크립트만 수정(씬의 `TutorialManager` 컴포넌트 폴백 값 1곳만 갱신)이라 두 씬 모두 새 로직 적용됨(스테이지2는 강제 슬롯 클릭 단계 자체가 없어서 이 버그와 무관)

### 부록 — 튜토리얼 ③(벽 슬롯 강제 클릭) 실제 진행 불가 버그 수정 (2026-09-10, 위 시각 표시 추가 후 재보고)
- 사용자 재보고: 시각 표시(노란 원반)를 추가했는데도 여전히 다음으로 안 넘어가고 표시된 슬롯도 안 보임. 추가로 "벽을 거의 투명화시킨 상태로(기존 에디터의 벽슬롯처럼) 나타내달라"는 비주얼 요청
- **진짜 원인 발견**: 클릭 판정 로직 자체(레이캐스트 → `WallSlot` 감지 → 배치)는 정상이었음(Play 모드에서 실제 카메라 화면좌표로 레이캐스트를 쏴서 정확히 대상 슬롯이 잡히는 것을 확인함). 문제는 **전체 화면을 덮는 어두운 반투명 오버레이(`TutorialPopupPanel`의 배경 `Image`, alpha 0.78)가 Screen Space-Overlay 캔버스라 3D 월드보다 항상 위에 그려진다는 점** — 슬롯도, 지난번에 추가한 노란 원반 표시도 전부 이 어두운 막 뒤(정확히는 아래 레이어)에 가려져서 **화면상으로 아예 안 보였던 것**(클릭 좌표 계산 자체는 문제없었지만, 사람이 눈으로 보고 클릭할 대상 자체가 안 보이니 실질적으로 진행 불가능한 상태였음). tutorial.md 5.1에도 "강조 대상만 오버레이 위로 올려 원래 밝기 유지"라고 이 문제를 이미 전제하고 있었는데, 그 처리를 빠뜨렸음
- **수정 1(핵심)**: `TutorialManager`가 오버레이 `Image` 컴포넌트를 `Awake()`에서 캐싱해두고, 슬롯 클릭을 기다리는 페이지(`requireSlotClick`)에 들어갈 때 `SetOverlayDimmed(true)`로 오버레이 알파를 0.78 → **0.12**로 대폭 낮춰 3D 월드(슬롯과 하이라이트)가 실제로 보이도록 함. 일반 문구 페이지로 돌아가면 원래 알파로 복원
- **수정 2(비주얼 요청 반영)**: `ShowSlotHighlight()`를 노란 원반(공중에 떠 있는 얇은 디스크) → **`Wall.prefab`과 동일한 크기(0.2, 1.5, 2)의 반투명 "고스트 벽"**으로 교체. 슬롯 위치·회전에 정확히 맞춰 배치(공중에 띄우던 오프셋 제거 — 위치가 어긋나 있던 것도 겸사겸사 없앰). URP Lit 셰이더를 Transparent Surface Type으로 스크립트에서 직접 설정(alpha 0.3, 연한 노란빛)해 "거의 투명화된 벽"처럼 보이게 함. 콜라이더는 없음(클릭 판정을 방해하지 않도록 순수 시각 전용)
- **검증**: Play 모드에서 실제로 첫 피격 → 슬롯 강제 페이지까지 재현 → 스크린샷으로 오버레이가 걷혀 월드가 잘 보이고 고스트 벽이 슬롯 자리에 정확히 뜨는 것 확인 → 실제로 `WallPlacementManager.TryPlaceWall`을 그 슬롯에 대해 호출해 **①벽이 실제로 생성되고 ②재화가 정상 차감(남은 벽 6→5)되고 ③튜토리얼이 정상적으로 닫히는 것(`IsOpen=False`, `timeScale=1`)까지 스크린샷으로 최종 확인**. 콘솔 에러 없음
- 스크립트만 수정(씬 데이터 변경 없음)이라 두 씬 모두 자동 적용됨
- 사용자 제보: Player가 죽은 뒤에도 몬스터가 계속 공격함
- reference.md 1번 섹션 "공격 대상 탐색" 명세에 원래 "사망한 대상은 제외" 항목이 있었는데 누락돼 있었음(3단계 구현 당시 핵심 메커니즘 검증에 집중하느라 빠짐) — 이번에 채워 넣음
- `IDamageable`에 `bool IsDead { get; }` 추가(기존 `PlayerHealth.IsDead` 프로퍼티가 이미 시그니처가 같아 별도 수정 없이 인터페이스 구현 충족)
- `MonsterTower.FindClosest()`가 사거리 안 대상들을 훑을 때 `IDamageable.IsDead`가 true인 대상은 후보에서 제외하도록 수정 — 대상 전부가 사망 상태면 `null` 반환(발사도 안 하고 바라보기도 멈춤)
- 스크립트만 수정(씬 데이터 변경 없음)이라 두 씬(`Stage1(Test)`/`Stage2`) 모두 자동 적용됨
- **검증**: Play 모드에서 Player를 몬스터 사거리 안에 두고 생존 상태일 때 탐지되는 것 확인 → `TakeDamage(1000)`으로 사망시킨 뒤 같은 탐지 함수 호출 시 대상을 찾지 못하는(`null`) 것 확인. 콘솔 에러 없음

### 9단계 — 벽 슬롯 코너 배치를 일반 규칙으로 개선 (2026-09-10, 45도 시도 후 재작업)
- 사용자 제보: 벽 슬롯을 "길 타일 양옆"으로 배치하는 규칙이 경로가 꺾이는 지점(코너)에서 슬롯이 길목에 겹치는 문제를 계속 일으킴(그동안 `Slot_09_R`을 몬스터 방향 보고 수동 오버라이드한 게 바로 이 문제였음) — 매번 수동으로 고치지 않아도 되게 일반 규칙으로 바꿔달라는 요청(안 되면 수동 수정을 쉽게 만드는 것도 허용)
- **원인**: 슬롯 위치 공식이 "그 타일로 들어오는 방향(`PathManager.GetTileForward`) 기준 좌/우 수직 오프셋" 하나만 쓰는데, 코너 타일(타일9)은 들어오는 방향(세그먼트A, +Z)과 나가는 방향(세그먼트B, +X)이 달라서, 들어오는 방향 기준 "오른쪽"이 실제로는 코너 안쪽 포켓(세그먼트B가 시작되는 쪽)을 가리킴
- **1차 시도(이등분선/45도) → 사용자 반려**: 두 방향의 법선을 이등분해 대각선으로 배치했으나, 사용자가 "45도 벽이 아니라 타일 모서리에 맞춰 바깥에 설치"해야 한다고 재지시 — 기존 방식(축 정렬, 타일 중심에서 1유닛 떨어진 타일 가장자리에 꼭 맞게 배치)을 그대로 유지하되 위치만 코너 바깥쪽으로 옮기는 것이 맞는 방향이었음
- **최종 해결**: 코너 타일(9)에서, 문제였던 슬롯(원래 "들어오는 방향 기준 오른쪽" = 포켓 안쪽)을 **나가는 방향(세그먼트B)이 이미 쓰고 있는 가장자리 규칙("왼쪽" = z+1)으로 바꿔치기** — 즉 세그먼트B의 다른 9개 타일이 전부 쓰는 것과 완전히 같은 패턴(타일 중심에서 북쪽으로 1유닛, 회전 Y=90)을 타일9에도 그대로 적용. 축 정렬(0°/90°만 사용)과 "타일 가장자리에 꼭 맞는 오프셋" 원칙을 그대로 지키면서, 코너 안쪽이 아니라 바깥쪽 가장자리를 골라 쓰는 것만 다름
  - `Slot_09_L`: (-9,0.75,9) 45도 → **(-9,0.75,8) 0도로 원상복구**(애초에 문제 없던 슬롯, 손대지 않아야 했음)
  - `Slot_09_R`: (-7,0.75,7) 45도 → **(-8,0.75,9) 90도**(세그먼트B의 "Left" 가장자리 규칙을 타일9에 적용한 것 — 옛 수동 오버라이드값 (-7.99,0.75,9)도 사실 이거랑 거의 같은 자리였음, 각도만 부정확했음)
  - 나머지 38개 슬롯은 변화 없음
  - `Stage1(Test).unity`/`Stage2.unity` 두 씬 모두 동일하게 적용
- **일반화한 규칙(다음에 코너가 또 생기면 적용할 것)**: 코너 타일에서 "들어오는 방향 기준" 좌/우 중 하나가 다음 구간(나가는 방향) 쪽 포켓에 겹치면, **그 슬롯만 나가는 방향 세그먼트가 쓰는 가장자리 오프셋으로 교체**한다(축 정렬 유지, 타일 중심에서 1유닛 오프셋 유지) — 대각선/이등분선 계산은 쓰지 않는다
- **검증**: Play 모드에서 `Slot_09_L`/`Slot_09_R`에 실제로 벽을 배치해 스크린샷으로 확인 — 두 벽이 코너를 감싸는 ㄱ자 형태로 타일 가장자리에 꼭 맞게 축 정렬돼 배치되고 경로(황토색 타일)를 전혀 가리지 않는 것 확인. 콘솔 에러 없음
- 이 작업으로 "다음 할 일"에 오래 남아있던 `Slot_09_R` 수동 오버라이드 항목 해소

### 9단계 — 재도전 버튼 UI 연결 (2026-09-10)
- design.md 9단계 "재도전 버튼, 재화 표시 UI 등 마무리 요소 구성" 중 재도전 버튼 작업(재화 UI는 기존 `CurrencyText`로 이미 충분해서 손 안 댐)
- `StageManager.cs`에 `retryButton`(Button) 필드 추가 — `Start()`에서 `retryButton.onClick.AddListener(RetryStage)`로 8단계 때 만든 리셋 로직에 바로 연결. `ShowResult()`(클리어·실패 공통)에서 버튼을 보이게 하고, `RetryStage()` 안에서 다시 숨김
- UGUI `RetryButton`(+ 자식 `Label` Text, `malgun.ttf`, "다시 도전") 신규 생성 — `ResultText` 바로 아래(화면 중앙 기준 y=-140) 배치, 평소엔 비활성 상태. **두 씬(`Stage1(Test).unity`/`Stage2.unity`) 모두 동일하게 구성**
- **검증**: Play 모드에서 ①체력을 즉사시켜 GAME OVER 유도 → 버튼이 보이는지 확인 → `button.onClick.Invoke()`로 실제 클릭과 동일하게 실행 → 체력/재화/Player 위치가 전부 시작 상태로 복원되고 결과 텍스트·버튼이 다시 숨겨지는 것 확인. ②Player를 마지막 타일 위치로 옮겨 CLEAR 유도 → 버튼 클릭 → 마찬가지로 정상 리셋되는 것 확인. 두 씬 모두 동일하게 검증. 콘솔 에러 없음
- ⚠️ **환경 이슈 재발(참고용 기록)**: 이번에도 에디터 창이 포커스를 잃은 상태에서 Play 모드가 "playmode_transition"에 멈춰 `Time.frameCount`가 전혀 진행되지 않는 현상이 발생(이전 세션에도 기록됐던 문제, `EditorWindow.FocusWindowIfItsOpen`으로 강제 포커스 시도했으나 이번엔 안 풀림). 그래서 "실제 시간 흐름에 맡겨 자연 주행 후 도착"이 아니라 **Player 위치를 직접 목표 지점으로 옮기고 `StageManager.Update()`를 리플렉션으로 직접 호출**해 클리어 판정 로직 자체를 검증하는 방식을 사용함 — 자연 주행(NavMeshAgent 이동) 자체는 1·4단계에서 이미 별도로 실측 검증됐고 이번 세션에서 변경한 적 없으므로 문제없다고 판단
- 아직 안 한 것: 실제 마우스로 처음부터 끝까지 플레이해보는 것(사람이 직접 해야 함 — MCP로는 진짜 마우스 클릭 시뮬레이션이 불가능해서 전 과정 리플렉션으로 대체 검증함)

### 부록 — `docs/tutorial.md` 핵심 구조 구현: 트리거 5종 + 강제 슬롯 클릭 + 스테이지별 시퀀스 (2026-09-10)
- 점검 결과 발견한 갭을 바탕으로 사용자 확인: **연출(카메라 줌인/하이라이트/파티클/슬로우모션 등)은 제외**하고, **트리거 구조 + 스테이지별 on/off + 강제진행(슬롯 클릭)** 핵심 구조부터 구현하기로 함
- **기존 `TutorialPopupManager`/`MonsterTutorialTrigger`는 이 구조와 안 맞아 대체함**(파일 삭제는 CLAUDE.md 금지 항목이라 실제로 지우지는 않았고, 사용을 완전히 중단함 — 필요 없으면 사용자가 직접 지워도 됨):
  - `MonsterTutorialTrigger` 컴포넌트를 몬스터 프리팹 3종에서 제거(타입별 개별 팝업 대신 "스테이지에서 첫 몬스터"로 1회만 뜨게 바뀌었기 때문)
  - `Managers`의 `TutorialPopupManager` 컴포넌트를 `TutorialManager`로 교체
- **신규 스크립트**: `TutorialManager.cs`(오케스트레이터) + `TutorialTrigger.cs`(enum) + `TutorialPage.cs`/`TutorialStep.cs`(직렬화 데이터, "한 파일에 한 클래스" 규칙대로 분리)
  - 트리거 5종 지원: `StageStart`/`FirstMonsterSeen`/`FirstHit`/`FirstWallPlaced`/`TileReached` — 전부 각 매니저가 발행하는 이벤트를 구독하는 방식(기존 스크립트에 튜토리얼 코드를 직접 넣지 않음, tutorial.md 6장 지침 그대로 준수)
  - 한 단계(`TutorialStep`)는 여러 페이지(`TutorialPage`)를 순서대로 보여줄 수 있음. 페이지별로 "아무 데나 클릭"(기본) 또는 "지정된 벽 슬롯을 실제로 클릭해야만 진행"(`requireSlotClick`) 선택 가능 — 일시정지③의 "체력 UI 강조 → 슬롯 강조, 슬롯 클릭 강제" 요구사항 구현
  - `tutorialEnabled` 불린 + 씬마다 독립된 `steps` 배열로 스테이지별 on/off 구현(스테이지1은 5단계 전부, 스테이지2는 ⑥ 하나만 — 씬이 아예 분리돼 있어 자연스럽게 격리됨)
- **각 매니저에 이벤트 추가(기존 로직은 안 건드림)**:
  - `PathManager.OnPlayerTileIndexChanged` — 타일 인덱스가 실제로 바뀔 때만 발행
  - `PlayerHealth.OnDamaged` — 실제 피해를 입을 때만 발행(`ResetHealth`의 체력 복원과는 구분됨 — 재도전 시 오발동 방지)
  - `WallPlacementManager.OnWallPlaced` + `InputLocked`(입력 전면 차단) + `RestrictToSlot`/`ClearSlotFilter`(특정 슬롯만 허용) — 전부 "튜토리얼"이라는 이름 없이 범용 API로 노출해서 `WallPlacementManager` 자체는 튜토리얼의 존재를 모르게 함
- **"벽을 처음 세운 직후"(④) 판정에서 의도적으로 튜토리얼이 강제로 유도한 배치는 제외**하도록 설계함 — 그래야 ③ 직후 "투사체가 벽에 막히는 장면"을 먼저 보고, 그다음 자유롭게 벽을 세울 때 ④가 뜸(③과 ④가 같은 프레임에 연달아 뜨는 것을 방지, tutorial.md의 "재개 후: 투사체가 그 벽에 맞고 사라지는 장면을 반드시 보게 배치" 요구와 부합)
- **스테이지1 실제 배치**(사용자가 배치한 `Monster_A~D` 기준으로 리플렉션 스캔해 산출):
  - ①시작 직전 → `StageStart`
  - ②첫 몬스터 발견 → `FirstMonsterSeen`
  - ③첫 피격(2페이지, 2번째 페이지가 슬롯 강제) → `FirstHit`, 강제 슬롯 = **타일2 Left**(Monster_A가 처음 위협하는 실제 첫 타일로 계산해서 선정)
  - ④벽 개수 안내 → `FirstWallPlaced`
  - ⑤지나간 슬롯 안내 → `TileReached`(타일7 — Monster_A 구간(타일2~6)과 Monster_B 구간(타일8~11) 사이 안전 지대, "두 번째 몬스터 구간 진입 전"에 정확히 대응)
  - ⚠️ tutorial.md도 "타일 인덱스는 실제 스테이지 조립 시 조정한다"고 명시함 — 지금 값은 **현재 몬스터 배치 기준으로 계산한 값**이라, 나중에 몬스터 위치를 바꾸면 타일2/타일7도 같이 재계산해야 함
- **스테이지2 배치**: ⑥ 시야 제한 안내(`StageStart`) 1개만
- **제외한 범위(연출)**: `Time.timeScale` 0.2초 보간, 오버레이 위 강조 노출, `UIHighlight`/`WorldHighlight`/`CameraZoom`, 타이핑 효과, 깜빡이는 안내문구, 상시 피드백 연출 8종(5.5), 첫 벽 차단 슬로우모션, 결과화면 카운트업/실패 위치 표시, 클리어/실패 화면 추가 문구 — 전부 미구현. 지금은 문구가 즉시 나타나고 즉시 사라지는 형태
- **검증**: Play 모드에서 스테이지1 전체 시퀀스를 순서대로 강제 발동시켜 하나씩 확인 — ①시작 즉시 발동+정확한 문구 → ②몬스터 인접 시 발동 → ③피격 시 2페이지 순차 표시, 2페이지에서 `WallPlacementManager`의 슬롯 필터(`slotFilterActive=true, tile=2, side=Left`)가 정확히 걸리는 것과 지정 슬롯에 벽을 세우면 정확히 진행되는 것 확인 → ④실제로 유효한(아직 안 지나간) 슬롯에 벽을 세우자 정상 발동 → ⑤플레이어가 타일7에 도달하자 자동으로 발동(강제 호출 없이 실제 게임 진행만으로 확인됨). 스테이지2 ⑥도 동일하게 시작 즉시 발동·해제 확인. 콘솔 에러 없음
- ⚠️ **테스트 중 발견한 사소한 설계 한계**: 어떤 트리거 조건이 "이미 다른 팝업이 떠 있는 동안" 충족되면 큐 없이 그냥 버려진다(재시도 안 됨). 게임 중에는 일시정지 중 `Time.deltaTime=0`이라 다른 트리거 조건(이동/피격/배치)도 구조적으로 같이 멈추므로 실제 플레이에서는 거의 발생하지 않지만, 이번 테스트처럼 리플렉션으로 강제 호출하며 텀을 두면 재현됨(④ 테스트 때 실제로 한 번 겪음 — 이미 지나간 타일에 벽을 두려다 실패해서 결국 유효한 타일로 재시도해 확인함). 실제 플레이에는 지장 없지만 참고할 것
- 다음(연출 붙이기)을 진행할지는 사용자가 다시 정하면 됨

### 부록 — `docs/tutorial.md` 신규 문서 확인 + 기존 튜토리얼 팝업 시스템 점검 (2026-09-10)
- 사용자가 `docs/tutorial.md`(튜토리얼 상세 기획, design.md 6장 하위 문서)를 새로 추가함 — 학습 순서 7단계(①~⑦), 스테이지1의 일시정지 5회(①~⑤)·스테이지2의 일시정지 1회(⑥) 시퀀스, 연출 목록(강조 방식·카메라 줌인·상시 피드백), `TutorialManager` 시스템 요구사항이 상세히 정리돼 있음
- 요청: 문서 확인 후, 지금까지 구현한 `TutorialPopupManager` 시스템으로 점검
- **Play 모드 실측 점검**: 기존에 만든 "몬스터 처음 발견 시 팝업" 트리거를 실제로 발동시켜 스크린샷으로 확인 — 일시정지(`timeScale=0`)·패널 표시·제목/본문/안내문구 렌더링까지 정상 작동 확인(엔진 자체는 built 그대로 재사용 가능)
- **기획서 대비 갭 분석 결과** (자세한 목록은 사용자에게 보고, 요약만 기록):
  - 엔진(`TutorialPopupManager`의 pause/show/dismiss-once-per-key 구조)은 재사용 가능한 기반이지만, **트리거 세분화가 기획과 다름** — 지금은 "몬스터 타입별(기본/광역/DPS) 최초 발견" 3종인데, tutorial.md는 "스테이지 전체에서 첫 몬스터를 만난 순간" 1회(범용 문구)로 설계돼 있음
  - **스테이지별 on/off가 구현 안 돼 있음** — tutorial.md는 스테이지2에서 몬스터 소개성 팝업이 전혀 뜨면 안 되고(⑥ 하나만), 스테이지1에서 먼저 플레이해야만 "이미 본 키"로 처리되는데, `shownKeys`가 씬(오브젝트) 로컬 상태라 **스테이지2를 단독으로 먼저 플레이하면 몬스터 타입 소개 팝업이 그대로 떠버림** — 기획과 충돌하는 실측 확인된 문제
  - 그 외 미구현 항목: 트리거 ①(스테이지 시작)·③(첫 피격, 특정 슬롯 강제 클릭 진행+다른 슬롯 잠금)·④(첫 벽 배치)·⑤(특정 타일 도달) 전부 미연결, `Time.timeScale` 0.2초 보간, 오버레이 위 강조 대상 노출, 강조 연출 3종(UIHighlight/WorldHighlight/CameraZoom), 상시 피드백 연출 8종(5.5), 첫 벽 차단 슬로우모션, 결과화면 카운트업/실패 위치 표시 등
- 이 갭 목록을 바탕으로 사용자에게 우선순위·범위를 확인한 뒤 이어서 구현 예정(현재 대화 참조)

### 부록 — 튜토리얼 설명 팝업 시스템 신규 추가 (설계 외 기능, 2026-09-10)
- 사용자 요청: "기획서에 없던 내용인데, stage2까지는 튜토리얼 개념인데 중간중간 화면을 멈춰서 정보를 알려줄만한 시스템이 필요해" — 예시로 "stage2에서 처음 DPS몬스터를 마주쳤을 때 화면이 멈추며 설명이 나오는" 것을 듦
- 확인 후 결정된 사항(사용자 답변):
  - **트리거 종류**: 지금은 "몬스터 타입별 처음 발견 시"만 테스트로 연결. 그 외 트리거(스테이지 시작 직후, 첫 벽 배치 시점 등)는 사용자가 추후 별도 기획서를 만들면 그때 추가 — 지금은 **언제든 새 트리거를 꽂을 수 있는 범용 뼈대만** 구현
  - **적용 범위**: 스테이지1·스테이지2 모두
  - **닫는 방식**: 마우스 클릭 또는 아무 키 입력으로 직접 닫기
- **`TutorialPopupManager.cs`(신규)** — 범용 일시정지+팝업 매니저. `TryShow(key, title, body)`를 호출하면: 처음 보는 key면 `Time.timeScale = 0`으로 게임을 멈추고 패널에 제목/본문을 띄움 → 클릭/아무 키 입력 감지 시 패널을 닫고 `timeScale`을 원래대로 복원. 같은 key는 세션 중(재도전해도) 다시 뜨지 않도록 `HashSet<string>`으로 기억 — 이 클래스는 "언제 띄울지"를 전혀 모르고 순수히 "어떻게 띄우고 닫을지"만 담당해서, 호출하는 쪽만 다르면 어떤 트리거든 재사용 가능
- **`FogVisibility.cs`에 `OnRevealed` 이벤트 추가** — 시야 밖에 있다가 처음 드러나는 순간 딱 한 번 발행. 기존 "몬스터 숨김" 로직은 전혀 안 건드리고 이벤트만 얹음
- **`MonsterTutorialTrigger.cs`(신규)** — `FogVisibility.OnRevealed`를 구독해 `TutorialPopupManager.TryShow`를 호출하는 연결 고리. `tutorialKey`/`title`/`body`를 인스펙터에서 지정
- 몬스터 프리팹 3종(`Chicken`/`Monster_AreaType`/`Monster_DPSType`) 전부에 `MonsterTutorialTrigger` 부착, design.md 3.6 표 문구를 그대로 재사용해 테스트용 설명 작성:
  - 기본형: "사거리·쿨다운·공격력이 모두 평범한 기준점 몬스터입니다."
  - 광역형: "사거리가 넓어 많은 칸을 얕게 위협합니다. 전부 막으려면 재화 소모가 큽니다."
  - 고DPS형: "사거리는 좁지만 공격력이 높고 쿨다운이 짧습니다. 막지 않으면 급격히 위험합니다 — 우선적으로 막아야 하는 대상입니다."
  - 프리팹 단위로 붙였기 때문에 **두 스테이지 어디에 배치된 인스턴스든(지금은 물론 나중에 추가되는 것도) 자동으로 동작**함
- **UI 구성**: `Canvas` 하위에 `TutorialPopupPanel`(전체화면 반투명 검정) + `TutorialTitleText`/`TutorialBodyText`/`TutorialHintText`(전부 `malgun.ttf`, 한글 깨짐 방지 — 기존 컨벤션 그대로) — **두 씬(`Stage1(Test).unity`/`Stage2.unity`) 모두에 동일하게 구성**하고 `Managers`의 `TutorialPopupManager` 컴포넌트에 연결
- **`WallPlacementManager`에 가드 추가** — 팝업이 떠 있는 동안(`TutorialPopupManager.IsOpen`) 클릭이 그대로 뒤로 새어나가 벽이 배치되는 것을 막기 위해 `Update()` 맨 앞에서 체크
- ⚠️ **재도전 시 팝업을 다시 보여줄지**는 설계 문서에 없어 임의로 판단함 — "한 번 본 설명을 재도전마다 또 보여주는 건 번거로움"으로 보고 `RetryStage()`에서 튜토리얼 표시 여부(`shownKeys`)는 초기화하지 않기로 함. 다르게 원하면 알려주면 됨
- **검증**: Play 모드에서 `TryShow` 직접 호출로 `IsOpen`/`timeScale`/패널 활성화가 의도대로 바뀌는 것 확인 → Player를 몬스터 바로 옆으로 이동시키고 `FogVisibility.LateUpdate`를 강제 실행해 `OnRevealed → MonsterTutorialTrigger → TutorialPopupManager.TryShow` 체인이 실제로 이어지는 것과 팝업 제목("기본형 몬스터")이 올바르게 뜨는 것 확인. 콘솔 에러 없음
- ⚠️ **작업 중 발견한 사고 및 복구**: `Monster_AreaType.prefab`/`Monster_DPSType.prefab`에 `manage_prefabs(modify_contents)`로 컴포넌트를 추가하는 과정에서 MCP 연결이 두 번 끊겼다 재시도했는데(`Unity plugin session ... disconnected`), 그 사이 **스테이지2에 배치해둔 몬스터 6마리 중 5마리의 위치가 의도한 좌표에서 벗어나 있는 것을 뒤늦게 발견**(예: `Monster_DPS_1`이 (-11,0,-8) → (-8.12,0,10.23)로 이동). 정확한 원인은 특정하지 못했으나(헤드리스 프리팹 수정 중 씬이 다른 씬(Stage2)으로 전환돼 있었던 것과 연결 재시도가 겹친 정황), **다행히 의도한 좌표를 정확히 기록해 두었어서 전부 원래 값으로 복원**하고 재검증(사거리 중첩 위치 재확인)·재저장함. `Stage1(Test)` 씬의 몬스터(`Monster_A~D`)는 좌표가 다르긴 했으나 이는 사용자가 직접 배치하며 조정한 값과 일치해 문제 없음을 확인
  - **교훈**: 헤드리스 프리팹 편집(`manage_prefabs`) 도중 연결이 끊겼다 재시도되는 경우, 그 프리팹의 인스턴스가 배치된 씬들의 좌표를 편집 후 반드시 재확인할 것
- ⚠️ **추가로 발견·수정한 문제**: 튜토리얼 설명 텍스트를 `manage_prefabs`에 JSON 유니코드 이스케이프(`\uXXXX`)로 직접 입력하는 과정에서 손계산 실수로 오타가 3곳 발생(고DPS형 "좁지만"→"좋지만"/"좍지만", 광역형 "얕게"→"얷게"/"얱게"). 임시 인스턴스로 실제 저장된 텍스트를 하나하나 읽어 대조하다 발견 → `execute_code`로 프리팹을 직접 열어(`LoadPrefabContents`) 한글을 리터럴로 그대로 써서 수정(유니코드 이스케이프 손계산 대신 이 방법이 훨씬 안전함, 다음에 한글 텍스트를 컴포넌트 프로퍼티로 넣을 때는 이 방식을 우선 고려할 것). 최종적으로 3개 프리팹 텍스트 전부 임시 인스턴스로 재확인 완료

### 8단계 — 스테이지 2 조립 (씬 분리 + 3종 혼합 배치) (2026-09-10)
- 사용자 확인 사항: 스테이지2는 **새 Unity Scene으로 분리**(현재 씬 안에서 경로/몬스터 세트만 교체하는 방식은 채택 안 함)
- `SampleScene.unity`를 저장 후 복제해 `Assets/Scenes/Stage2.unity` 신규 생성, 해당 씬을 로드해서 작업
- 기존 스테이지1용 몬스터 4마리(`Monster_A~D`, 기본형만 사용)를 삭제하고 3종 혼합 몬스터 6마리를 새로 배치:
  - 세그먼트A(타일0~9): `Monster_DPS_1`(고DPS, (-11,0,-8)) / `Monster_Area_1`(광역, (-14.5,0,-2)) / `Monster_Basic_1`(기본, (-13,0,4))
  - 세그먼트B(타일10~19): `Monster_DPS_2`(고DPS, (2,0,11)) / `Monster_Area_2`(광역, (9,0,14)) / `Monster_Basic_2`(기본, (-4,0,3))
  - 좌표는 사거리 겹침이 여러 구간에서 생기도록 계산 — 리플렉션으로 20개 타일 전부 스캔해 검증한 결과 **타일1~2·5~8·14~15, 총 3곳(사실상 연속 구간 포함 4군데)에서 사거리 중첩** 발생, 전체 20타일 중 19개 타일이 위협받음(코너 타일9만 안전지대) — design.md 8단계 "중첩 구간 다수" 요건 충족
- `visionRadius`를 8(스테이지1 유지)에서 **5로 축소** — 6마리 몬스터의 `FogVisibility`/`AttackRangeIndicator`, 그리고 `FogGround`의 `FogOfWarGround`까지 전부 동일하게 맞춤(3.7 주석대로 세 군데+몬스터마다 동기화). Player 이동속도(3.6)의 반응시간 = 5/3.6 ≈ 1.39초로 design.md 10.2 기대값(1.5~3초) 하한에 가까운 빡빡한 값 — "좁은 시야" 요건
- `CurrencyManager.totalCurrency`를 스테이지1(6)과 별개로 **7**로 설정 — 위협받는 19개 타일을 전부 막으려면 훨씬 부족하고(약 37%만 커버 가능), 고DPS형이 위협하는 "필수" 타일 위주로만 겨우 막을 수 있는 빠듯한 수준(design.md 4번 밸런스 공식의 의도와 일치) — "빠듯한 재화" 요건
- 스테이지1과 동일하게 경로 형태(20타일 ㄴ자)는 그대로 재사용(사용자가 스테이지1에서 이미 확정한 경로 그대로 스테이지2도 이어받음 — 별도 경로 재설계는 요청받지 않음)
- **검증**: Play 모드에서 리플렉션으로 20개 타일 전체를 스캔해 사거리 중첩 위치·총 위협 타일 수 확인(위 수치). 콘솔 에러 없음. Stage2 씬 저장 후 `SampleScene.unity`(스테이지1)로 다시 전환해둠(현재 열려있는 씬은 스테이지1)
- ⚠️ 반복 재도전 테스트(design.md 8단계 세 번째 항목)는 리셋 로직 자체 검증(아래 부록)으로 커버함. 실제 마우스 클릭 기반 풀 플레이스루는 여전히 미실시 — 사용자가 직접 플레이해보고 난이도 피드백 주면 좋음

### 부록 — 재도전(리셋) 로직 작성 (2026-09-10)
- design.md 3.8 "재도전: 재화 총량·몬스터 배치·경로를 동일하게 리셋"에 따라, 관련 매니저/컴포넌트마다 `Reset*()` 공개 메서드를 추가하고 `StageManager.RetryStage()`가 전부 orchestrate하도록 구현
  - `CurrencyManager.ResetCurrency()` — 시작 총량으로 복원, `OnCurrencyChanged` 재발행
  - `PathManager.ResetProgress()` — `currentPlayerTileIndex`를 0으로, 모든 타일의 좌/우 슬롯 점유 상태를 해제
  - `WallPlacementManager` — 배치한 벽을 `placedWalls` 리스트로 추적하도록 수정, `ClearAllWalls()`로 전부 파괴
  - `PlayerHealth.ResetHealth()` — 체력 최대치로 복원, 피격 색상 프로퍼티블록 초기화, `isDead` 해제, `Animator.Rebind()`로 사망 상태 초기화, 이동 재개
  - `PlayerAutoMover.ResetMovement()` — `NavMeshAgent.Warp`로 0번 타일 위치로 순간이동 후 1번 타일부터 다시 자동 이동 시작
  - `FogVisibility.ResetVisibility()` / `AttackRangeIndicator.ResetIndicator()` — 몬스터별로 "처음 발견 안 된 상태"로 되돌려, 재도전 시 다시 시야 밖에 숨겨지고 사거리 표시도 다시 처음 발견 때처럼 1회성으로 뜨게 함
  - `FogOfWarGround.ResetFog()` — 텍스처를 다시 안개로 전부 채움(기존 Awake의 채우기 로직을 `FillFog()`로 분리해 재사용)
  - `StageManager.RetryStage()` — 위 전부를 순서대로 호출 + `isFinished`/결과 텍스트 초기화. `CurrencyManager`/`WallPlacementManager`/`FogOfWarGround`는 `Start()`에서 `FindFirstObjectByType`로 자동 연결(씬마다 수동 배선 안 해도 되게)
- ⚠️ **UI 재도전 버튼 연결은 9단계(폴리싱) 몫으로 보류** — 이번엔 `RetryStage()` 로직 자체만 완성(공개 메서드라 버튼 OnClick에 바로 연결 가능한 상태)
- **검증**: Play 모드에서 체력 차감·재화 소모·슬롯 점유·Player 이동·몬스터 시야 공개를 리플렉션/직접 호출로 어질러놓은 뒤 `RetryStage()` 호출 → 체력 100/100, 재화 시작량으로 복원, 슬롯 점유 해제, Player가 0번 타일 위치로 복귀(다음 프레임부터 다시 1번 타일 향해 자동 이동 재개), `FogVisibility.revealed`가 다시 `false`로 확인. 별도로 실제 `WallPlacementManager.TryPlaceWall`로 벽 하나를 배치한 뒤 `RetryStage()` 호출 시 `placedWalls` 리스트가 0개로(벽 오브젝트 파괴) 되는 것도 확인. 콘솔 에러 없음

### 부록 — 몬스터 타입별 임시 색상 구분 (2026-09-10)
- 사용자 요청: 테스트 단계에서도 비주얼 차이가 있으면 좋겠다 — 기본형은 그대로 두고 광역형/고DPS형에 각각 빨간색/파란색 입혀달라
- 원본 `Chicken.mat`(URP/Lit, `_BaseColor` 흰색 — 텍스처 그대로 노출)은 기본형이 계속 쓰므로 건드리지 않음. 대신 복제해서 `Chicken_AreaType_Red.mat`(`_BaseColor`≈(1,0.25,0.25))·`Chicken_DPSType_Blue.mat`(`_BaseColor`≈(0.45,0.6,1)) 신규 생성 후 각각 `Monster_AreaType.prefab`/`Monster_DPSType.prefab`의 `Model/Chicken_001`(SkinnedMeshRenderer) 머티리얼로 교체(헤드리스 프리팹 편집)
- 텍스처 위에 곱연산(Tint)으로 입히는 방식이라 완전한 원색은 아니고, 파란색 쪽은 원본 텍스처의 따뜻한 색감 때문에 약간 남색/보라빛이 도는 파랑으로 나옴 — 사용자가 원하면 `_BaseColor` 값만 더 조정하면 됨(플레이스홀더라 정밀 매칭은 안 함)
- ⚠️ 이 색상도 어차피 나중에 사용자가 몬스터 타입별 전용 에셋을 가져오면 통째로 교체될 임시 구분용(위 "몬스터/캐릭터 에셋 교체 가능성" 참고)
- **검증**: 두 프리팹을 씬에 임시 인스턴스로 놓고 Play 모드 스크린샷으로 기본형(주황빛 원본)과 뚜렷이 구분되는 빨강/파랑을 확인 후 임시 인스턴스 삭제. 콘솔 에러 없음

### 7단계 — 몬스터 종류 확장 (1차, 프리팹 2종 생성) (2026-09-10)
- design.md 3.6 표대로 `MonsterTower`는 이미 모든 밸런스 필드(`attackRange`/`attackCooldown`/`attackDamage`)가 `[SerializeField] private`로 인스펙터 노출돼 있어 스크립트 수정 없이 바로 진행(별도 클래스 안 만듦 — 설계 원칙 그대로 준수)
- `Chicken.prefab`을 복제해 프리팹 2종 신규 생성, `MonsterTower` 컴포넌트 값만 수정:
  - `Monster_AreaType.prefab`(광역 사거리형): attackRange 6→**10**(큼), attackCooldown **1.5**(보통, 유지), attackDamage 10→**6**(낮음~보통)
  - `Monster_DPSType.prefab`(고DPS형): attackRange 6→**4**(좁음), attackCooldown 1.5→**0.7**(짧음), attackDamage 10→**20**(높음)
  - 기존 `Chicken.prefab`(기본형: range 6/cooldown 1.5/damage 10)은 변경 없이 그대로 기준점 역할 유지
  - design.md 9번 "수치는 대충 넣고 플레이테스트하며 조정"에 따라 위 수치는 3종의 상대적 차이(사거리·쿨다운·데미지 트레이드오프)만 명확히 나게 잡은 러프한 값 — 실제 플레이해보고 너무 세거나 약하면 조정 필요
  - ⚠️ 두 프리팹 모두 비주얼은 그대로 Chicken 모델(placeholder) — **사용자가 몬스터 타입별 전용 에셋을 별도로 준비해올 예정이라고 밝혀서, 나중에 이 두 프리팹(과 Fox/Chicken 포함 캐릭터 에셋 전반)의 비주얼이 교체될 수 있음**. 로직(루트)과 비주얼(Model 자식)이 분리된 기존 구조라 나중에 Model만 갈아끼우면 됨
- **검증**: 씬에 두 프리팹을 임시 인스턴스로 생성해 `MonsterTower` 컴포넌트 값이 의도대로 저장됐는지 확인(AreaType: 10/1.5/6, DPSType: 4/0.7/20) 후 임시 인스턴스 삭제. 콘솔 에러 없음
- 아직 스테이지에 실제 배치는 안 함 — 3종을 섞어 배치하는 건 8단계(스테이지2) 작업 범위

### 부록 — 몬스터 사거리 표시 방식 변경: 얇게 + "처음 발견 시 1초만 표시" (2026-09-10)
- 배경: 몬스터 배치는 사용자가 직접 함(`Monster_A/B` 2마리는 기존대로 유지, `Monster_C`/`Monster_D` 2마리 추가 배치) — 배치 자체는 더 이상 건드리지 않음
- 사용자 요청: 사거리 표시 원이 너무 두꺼워 난잡해 보인다 → 훨씬 얇게. 그리고 시야 범위 안에 계속 떠 있지 말고, 처음 시야에 들어와 발견되는 순간에만 나타났다가 1초 후 사라지는 방식으로 변경
- `AttackRangeIndicator.cs` 수정:
  - `lineWidth` 기본값 0.15 → 0.04 (씬의 몬스터 4마리 컴포넌트 값도 동일하게 갱신)
  - 기존엔 매 프레임 `distance <= visionRadius` 결과를 그대로 `lineRenderer.enabled`에 대입해 시야 안에 있는 내내 계속 표시되던 방식 → `revealed`(최초 발견 여부)와 `timeSinceRevealed`(경과 시간) 상태를 추가해, **시야에 처음 들어오는 순간 한 번만 켜지고 `displayDuration`(기본 1초) 뒤 꺼지면 이후로는 다시 켜지지 않도록** 변경(몬스터 본체는 `FogVisibility`로 계속 보이지만, 사거리 표시는 "처음 발견 안내" 역할만 하고 사라짐)
- **검증**: Play 모드에서 리플렉션으로 4마리 몬스터의 `AttackRangeIndicator` 내부 상태(`revealed`/`timeSinceRevealed`/`LineRenderer.enabled`)를 조회 — 전부 `revealed=true`, `timer≈1.0` 시점에 `enabled=false`로 정확히 꺼져 있는 것을 확인(처음 발견 후 1초 시점에 자동으로 사라짐이 검증됨). 콘솔 에러 없음

### 6단계 — 스테이지 1 조립 (1차, 몬스터 배치) (2026-09-10)
- 사용자 확인 사항: ①기존 테스트용 20타일 ㄴ자 경로를 그대로 스테이지1 경로로 확정 ②자유배치돼 있던 몬스터 4마리(`Monster`~`Monster (3)`)는 전부 삭제 후 기본형 2마리만 새로 배치
- 삭제: `MonsterTower` 컴포넌트 기준으로 씬의 몬스터 4마리를 전부 찾아 삭제
- 신규 배치: `Chicken.prefab` 인스턴스 2개를 `Monster_A`(-12,0,-4), `Monster_B`(-12,0,4)로 생성(기존 프리팹 기본값 그대로 사용: attackRange=6/attackCooldown=1.5s/attackDamage=10, visionRadius=8)
  - 좌표는 design.md 6단계 요건("기본형 1~2마리, 겹침 구간 1곳, 넉넉한 시야, 여유있는 재화")을 만족하도록 계산해서 정함: 두 몬스터 모두 경로(X=-8, Z=-10~8)에서 X방향으로 4유닛 띄운 위치, Z는 각각 -4/+4로 대칭 배치. 사거리(6)와 겹쳐서 세그먼트 A(타일1~9)가 전부 위협받고, 정확히 타일5(월드 (-8,0,0)) 한 곳만 두 몬스터 사거리가 동시에 겹치는 지점이 되도록 산정
  - 시야(visionRadius=8)·재화(총량10, 벽1개=1)는 기존값 그대로 유지 — 위협 타일 9개 중 필요한 슬롯만 막으면 되므로 전부 막아도 재화 안에서 가능(여유 있는 재화 요건과 부합)
- **검증**: Play 모드에서 리플렉션 없이 직접 좌표 계산으로 검증(실시간 프레임이 멈추는 환경 문제로 실제 이동 관찰이 어려워 `Vector3.Distance`로 정밀 계산) — 타일1~4는 Monster_A만, 타일6~9는 Monster_B만, **타일5만 두 몬스터 사거리에 동시에 들어감(겹침 구간 1곳 요건 정확히 충족)**, 타일0과 코너 이후 세그먼트B(타일10~19)는 완전히 안전지대로 확인. 콘솔 에러 없음
- ⚠️ 실제 마우스 클릭으로 벽을 배치해가며 체력 소모·난이도 체감을 확인하는 실측 플레이테스트는 이번 세션에서 못함(에디터 실시간 재생이 멈추는 환경 문제 — 아래 "이슈" 참고). 사용자가 직접 플레이해보고 난이도가 안 맞으면 몬스터 위치/재화·시야 값을 조정할 것

### 부록 — 안개 색상 재조정: 원래(진한 회색)와 직전(연한 회색)의 중간 톤으로 (2026-09-10)
- 사용자 피드백: 방금 바꾼 연한 톤이 너무 밝다 — 원래 값(0.06,0.06,0.08,1)과 직전 값(0.75,0.75,0.78,0.92)의 중간 정도가 딱 좋을 것 같다
- `FogOfWarGround.fogColor` 기본값을 두 값의 산술 중간 `(0.4, 0.4, 0.43, 0.96)`으로 변경(스크립트 기본값 + 씬의 `FogGround` 컴포넌트 값 모두 갱신)
- **검증**: Play 모드 스크린샷으로 안개가 기존보다 어둡고 원래보다는 밝은 중간 톤 회색으로 보이는 것 확인. 콘솔 에러 없음

### 부록 — 길이 안개에 가려지던 문제 수정: 길은 안개와 무관하게 항상 표시 (2026-09-10)
- 사용자 요청: "길은 안개랑 별개로 미리 보여줄 수 있어?" — 확인해보니 design.md 3.7에 이미 "경로 자체는 항상 보인다"는 규칙이 명시돼 있었는데 실제로는 지켜지지 않고 있었음
- **원인**: 길 타일 비주얼(`PathTile_00~19`, Y=0.02)보다 안개 평면(`FogGround`, Y=0.05)이 더 높은 위치에 떠 있어서, 안개가 아직 안 걷힌 구간에서는 안개가 길 위를 덮어 가리고 있었음(3D 뎁스 테스트상 카메라에 더 가까운 안개가 이김)
- **수정**: `FogGround`의 Y 위치를 0.05 → 0.01로 낮춤(Floor Y=0 < FogGround Y=0.01 < PathTile Y=0.02 순서) — 안개는 여전히 맨바닥(Floor) 위는 덮지만, 길 타일은 안개보다 카메라에 더 가까워져 안개 유무와 상관없이 항상 위에 그려짐. 별도의 "항상 보이기" 로직을 새로 짤 필요 없이 렌더링 순서(높이)만 조정해 해결
- **검증**: Play 모드 진입 직후(플레이어가 아직 대부분 안 지나간 상태) 상공 스크린샷으로 ㄴ자 경로 전체가 안개 상태와 무관하게 처음부터 끝까지 다 보이는 것 확인, 실제 게임 카메라 스크린샷으로도 시야 범위 밖(안개 진 구간)까지 이어지는 길이 계속 보이는 것 확인. 콘솔 에러 없음
- ⚠️ 참고: 몬스터 본체·사거리 표시는 기존대로 `FogVisibility`/`AttackRangeIndicator`(거리 기반 렌더러 on/off)로 시야 제한이 그대로 유지됨 — 이번 수정은 바닥 안개(`FogOfWarGround`)와 길 타일 사이의 렌더링 순서만 바꾼 것이라 몬스터 숨김에는 영향 없음

### 부록 — 안개 색상을 더 뿌옇게 + 시야 원 테두리 해상도 개선 (2026-09-10)
- 사용자 요청: 안개가 너무 검은색이라 좀 더 뿌옇게(연한 느낌으로) 바꿔달라, 시야 원 테두리가 계단식으로 거칠어 보이니 해상도를 높여 더 둥글게 해달라
- `FogOfWarGround.cs` 기본값 변경: `fogColor` 진한 회색 `(0.06,0.06,0.08,1)` → 연한 뿌연 회색 `(0.75,0.75,0.78,0.92)`. 겸사겸사 그동안 안 쓰이고 있던 `fogColor.a`(알파)도 실제 픽셀 알파에 반영하도록 수정(기존엔 항상 255 고정이라 알파 슬라이더가 있어도 효과가 없었음) — alpha 0.92라 안개 너머로 바닥이 아주 살짝 비쳐 보이는 옅은 안개 느낌 추가
- `textureWidth` 기본값 128 → 512로 상향(세로는 비율에 맞춰 자동 계산되어 1024)— design.md/이전 세션 메모에 "성능 여유 있음, 필요하면 올리기"로 남겨둔 항목. 원 둘레의 계단현상이 눈에 띄게 줄어듦
- 씬의 `FogGround` 컴포넌트 값도 새 기본값과 동일하게 갱신
- **검증**: Play 모드 스크린샷으로 안개가 옅은 회색으로 바뀌고 원 테두리가 이전보다 훨씬 매끄러워진 것 확인. 콘솔 에러 없음(무관한 NoSubscription만 존재)
- 참고 수치라 사용자가 실제로 보고 더 연하게/진하게, 더 둥글게 원하면 `fogColor`/`textureWidth` 값을 다시 조정하면 됨

### 부록 — 바닥 안개가 정반대 위치에서 걷히던 버그 수정 (2026-09-10)
- 사용자 제보: "안개가 걷히는 표현의 위치가 정 반대에서 시작해"
- **원인 확인**: Play 모드에서 리플렉션으로 `FogOfWarGround.RevealAt(Vector3)`을 직접 호출해 검증 — 월드 좌표 `(12, 0, -25)`(바닥 기준 우측·하단 근처)를 넘겼는데도 실제로 안개가 걷힌 자리는 화면상 **좌측·상단**(정확히 대척점)에 나타남. 반대쪽 대각선 `(-14, 0, 28)`로도 재검증해 정확히 반대쪽(우측·하단)에 나타나는 것을 재확인 — 두 축(X/Z) 모두 180도 뒤집힌 상태였음
- 스크립트 자체 로직(`WorldToUV`→픽셀 인덱스→`SetPixels32`)은 내부적으로 일관돼 있었으나, 기본 Unity `Plane` 프리미티브 메쉬의 UV가 로컬 X/Z축과 정반대 방향으로 매핑되어 있어서 생긴 불일치였음(메쉬 자체의 UV 특성 — Plane 회전은 (0,0,0)으로 정상이었음)
- **수정**: `FogOfWarGround.cs`의 `WorldToUV()`에서 계산한 `u`, `v`를 각각 `1 - u`, `1 - v`로 반전
- **검증**: 같은 두 리플렉션 테스트(대각선 양끝 좌표)를 수정 후 재실행 — 이번엔 각각 의도한 위치(우측·하단 / 좌측·상단)에 정확히 안개가 걷히는 것을 상공 스크린샷으로 확인. 콘솔 에러 없음

### 부록 — 바닥 안개(FogOfWarGround)를 씬에 다시 추가 (2026-09-10)
- "다음 할 일"에 남아있던 항목 처리: 녹화 때문에 임시로 빼놨던 `FogGround`를 다시 씬에 구성
- `Floor`(Plane scale (3,1,6), pos (0,0,0)) 기준 스펙대로 새 `FogGround`(Plane, pos (0,0.05,0), scale (3,1,6)) 생성 → `MeshCollider` 제거(시야 차단용 순수 비주얼이라 충돌 불필요) → `MeshRenderer` 머티리얼을 `M_Fog.mat`로 설정 → `FogOfWarGround` 컴포넌트 부착
  - 컴포넌트 필드는 전부 기존 저장된 기본값 그대로 사용(visionRadius=8, worldSize=(30,60), worldCenter=(0,0), textureWidth=128) — `Floor` 크기·위치와 정확히 일치해 별도 조정 불필요. `player` 필드는 비워둠(Awake에서 Player 태그로 자동 탐색하도록 기존 설계 그대로)
  - ⚠️ `manage_components.set_property`로 `MeshRenderer`의 `sharedMaterial` 프로퍼티명을 직접 지정하는 시도는 실패("SerializedProperty 'sharedMaterial' not found") → `m_Materials`(배열)로 지정하니 정상 적용됨(5단계 최초 작업 때 `LineRenderer` 머티리얼 지정에서 겪었던 것과 유사한 종류의 프로퍼티명 이슈)
- **검증**: Play 모드 진입 → 스크린샷으로 캐릭터 주변만 밝게 드러나고 나머지는 어두운 안개로 덮인 것 확인, 콘솔에 새 에러 없음(무관한 NoSubscription 에러만 존재). 이 테스트에서는 벽을 세우지 않아 캐릭터가 몬스터 2마리 사거리가 겹치는 지점에서 빠르게 GAME OVER 처리됐는데, 이는 안개 시스템과 무관한 예상된 결과(테스트 목적이 안개 확인이었으므로 벽 배치는 하지 않음)

### 부록 — UI 텍스트 변경: "재화" → "남은 벽" (2026-09-09)
- `CurrencyUI.cs`의 표시 텍스트를 `"재화: {amount}"` → `"남은 벽: {amount}"`로 변경. 벽 1개 비용이 1(`WallPlacementManager.wallCost`)이라 `CurrencyManager.totalCurrency` 값이 곧 "세울 수 있는 벽 개수"와 같음 — 로직 변경 없이 텍스트만 수정
- 개수 조절 방법(사용자에게 안내함): `Managers` 오브젝트 → `Currency Manager` 컴포넌트 → `Total Currency` 필드(기본 10). 비용 자체를 바꾸려면 같은 오브젝트의 `Wall Placement Manager` → `Wall Cost` 필드

### 부록 — 타일 9번 벽 슬롯을 몬스터 방향으로 수동 조정 (2026-09-09, 녹화용 급한 요청)
- 사용자 제보: (코너 버그 수정 이후) 이번엔 타일 9·10 사이에 슬롯이 생기고, 타일 9의 원래 슬롯이 사라진 것처럼 보인다
- 확인 결과 버그는 아니었음 — 코너 버그 수정으로 `Slot_09_R`이 타일 9 기준 +X 방향(1유닛)으로 정확히 옮겨졌는데, 마침 그 방향이 타일 10(코너 너머)과 겹쳐 보이는 위치였음. 다만 사용자가 원한 건 수식대로의 위치가 아니라 **사거리 안에 든 몬스터(`Monster (1)`, `(-7.98,0,10.9)`)의 시야를 실제로 막는 위치**
- `Slot_09_R`을 타일 9 → 해당 몬스터 방향으로 1유닛 옮긴 지점 `(-7.99, 0.75, 9)`, 회전은 그 방향 시야를 막도록 Y=90으로 수동 재배치(공식 계산이 아니라 이 슬롯 하나만 수동 오버라이드)
- ⚠️ **주의**: 이 위치는 좌/우 슬롯 자동 배치 공식(진행방향 수직 오프셋)을 따르지 않는 **수동 예외**임. 나중에 몬스터 배치를 바꾸거나 타일 9 근처를 재작업할 때 이 슬롯이 다시 "이상해 보일" 수 있으니 참고할 것. 경로 중심선(X=-8)에 매우 가까운 위치라 시각적으로 좀 애매하지만, 벽은 NavMesh에 영향 없어 이동 자체는 문제 없음

### 부록 — 시야 위치가 플레이어와 안 맞는 문제 조사 및 수정 (2026-09-09)
- 사용자 제보: "시야 위치가 플레이어랑 안 맞는거같은데"
- **원인 발견**: `FogOfWarGround`/`FogVisibility`/`AttackRangeIndicator` 세 스크립트 모두 `Update()`에서 Player 위치를 읽고 있었음. Unity에서 서로 다른 컴포넌트의 `Update()` 호출 순서는 보장되지 않으므로, NavMeshAgent가 그 프레임에 위치를 갱신하기 *전에* 이 스크립트들이 먼저 읽어버리면 한 프레임 지연된(살짝 뒤처진) 위치를 기준으로 시야가 계산될 수 있음 — 캐릭터가 빠르게 움직일수록 눈에 띄는 밀림 현상으로 나타날 수 있음
- **수정**: 세 스크립트 모두 `Update()` → `LateUpdate()`로 변경 — `LateUpdate`는 그 프레임의 모든 `Update`(NavMeshAgent 이동 포함)가 끝난 뒤 실행되는 것이 보장되므로, 항상 그 프레임의 최종 확정 위치를 기준으로 시야를 계산하게 됨
- **검증 중 발견한 별도 이슈(버그 아님, 환경 제약)**: 리플렉션으로 좌표 계산 자체는 여러 지점(경로 시작 타일, 끝 타일, 현재 위치)에서 전부 정확히 일치함을 확인(텍스처 픽셀 데이터, UV 변환, 재질/텍스처 참조 전부 일치). 그런데도 스크린샷에 반영이 안 되는 현상이 있어 깊게 파본 결과 — **에디터 창이 포커스를 잃으면 `Time.frameCount`가 아예 멈춰서 렌더링 자체가 완전히 정지**하고, 이 상태에서 찍는 스크린샷은 전부 정지되기 이전 시점의 오래된 화면이라는 것을 확인함(데이터는 이미 정확히 갱신돼 있어도 화면엔 반영 안 됨). 즉 안개 시스템 자체의 버그가 아니라 이번 세션의 MCP 원격 테스트 환경 한계였음 — 사용자가 직접 포커스 있는 에디터에서 플레이해보면 정상적으로 보여야 함
- 디버깅 중 텍스처에 임시로 칠했던 테스트 색상(빨간 사각형 등)은 전부 정상 안개 상태로 복원함
- **추가 제보 및 진짜 원인**: `LateUpdate` 수정 이후에도 "안개가 걷히는 위치가 캐릭터 위치랑 자꾸 달라진다 / 도착하는(향하는) 쪽에서 시작되는 것처럼 보인다"는 재제보 — 확인해보니 `CM_TopDown`의 `CinemachineFollow.TrackerSettings.PositionDamping`이 (1,1,1)로 설정돼 있었음. 카메라가 실제 Player 위치를 1초 시간상수로 부드럽게 지연 추적하는 반면, 안개는 지연 없는 실제(순간) 위치를 기준으로 걷히기 때문에 — 화면에 보이는 캐릭터(카메라가 뒤처져서 실제보다 늦게 보임)와 안개 구멍(항상 정확한 실시간 위치)이 서로 어긋나 보였던 것. `PositionDamping`/`RotationDamping`을 전부 (0,0,0)으로 변경해 카메라가 지연 없이 즉시 따라가도록 수정
- **검증**: Play 모드에서 `MainCamera.position`과 `Player.position + FollowOffset`을 실시간 비교 — 오차 0.000으로 완전히 일치(지연 없음) 확인. 스크린샷으로도 캐릭터가 안개 걷힌 영역 경계와 정확히 일치하는 것 확인
- **바닥 안개(FogOfWarGround) 임시 제거(2026-09-09, 녹화 시간 부족으로 사용자 요청)**: 캐릭터 중심 원형 시야(몬스터 숨김·사거리 표시)는 정상 작동 확인됐으므로 그대로 유지. `FogGround` 씬 오브젝트만 제거(스크립트 파일 `FogOfWarGround.cs`는 그대로 남겨둠, 필요하면 나중에 다시 씬에 추가해 재사용 가능). 콘솔 에러 없음, Play 모드 정상 확인

### 부록 — 타일 8·9번 사이 경로 한가운데에 벽이 생기던 버그 수정 (2026-09-09)
- 사용자 제보: 타일 8번과 9번 사이에 벽이 생겨서 이동 경로를 막는다
- **원인**: 타일 9는 코너(직선 구간 A→B 전환) 바로 직전 타일인데, `PathManager.GetTileForward()`가 "다음 타일" 기준으로 방향을 계산하다 보니 타일 9의 진행 방향이 코너 이후 방향(+X)으로 잘못 계산됨. 이 잘못된 방향을 기준으로 2단계에서 슬롯을 배치할 때, 타일 9의 좌/우 슬롯이 옆(X축)이 아니라 앞뒤(Z축)로 오프셋되면서 정확히 타일 8-9 사이 경로 중앙선(X=-8) 위에 놓이게 됨
- **수정**: `GetTileForward()`를 "다음 타일 방향" 대신 "이전 타일에서 이 타일로 들어오는 방향" 기준으로 변경(첫 타일만 예외적으로 다음 타일 기준) — 코너 진입 직전 타일이 코너 이후 방향에 영향받지 않도록 함. 이미 잘못 배치돼 있던 `Slot_09_L`((-8,0.75,9)→(-9,0.75,8))·`Slot_09_R`((-8,0.75,7)→(-7,0.75,8))의 위치·회전을 타일 8과 같은 패턴으로 직접 수정
- **검증**: 전체 40개 슬롯을 좌표 기준으로 스캔해 경로 중앙선 위에 있는 슬롯이 더 없는지 확인(이상 없음). Play 모드에서 리플렉션으로 `Slot_09_R`에 벽 배치를 직접 호출해 (-7, 0.75, 8) 위치(경로 옆)에 정상 생성되는 것 확인

### 부록 — 길 타일 임시 비주얼 추가 (2026-09-09, 녹화용 급한 요청)
- 사용자 요청: 길로 설정된 타일이 주변 바닥과 구분되게, 임시로라도 "길" 느낌이 나게 해달라(시간 없음)
- `M_PathTile.mat` 신규 생성(URP/Unlit, 황토색 계열 `(0.62,0.52,0.38)`) — 임시용이라 라이팅 계산 없는 Unlit로 빠르게 처리
- `Path/PathTilesVisual` 하위에 `PathTile_00~19`(Plane 프리미티브, 2x2 유닛, Y=0.02, Collider 제거) 20개를 각 타일 위치에 생성해 길 형태(L자)가 시각적으로 드러나게 함 — 순수 비주얼용, 로직/충돌 없음
- 검증: Play 모드 스크린샷으로 L자 경로가 주변 바닥과 뚜렷이 구분되는 것 확인, 콘솔 에러 없음

### 부록 — 5단계 전면 재설계: 타일 기반 시야 → 캐릭터 중심 원형 시야 (2026-09-09)
- 사용자 피드백: 타일/바닥 기준의 시야 제한이 부자연스럽다 — 캐릭터를 중심으로 한 원형 범위로 바꿔달라는 요청(기획 변경)
- `FogVisibility.cs` 전면 수정 — `PathManager`/타일 인덱스/`hideWhenRevealed`/`fixedTileIndex` 전부 제거. 대신 `Player` Transform(태그로 자동 탐색) 기준 `Vector3.Distance`로 원형 반경(`visionRadius`, 기본 8) 안에 들어오면 드러나고, 한 번 드러나면 계속 유지(영구 공개)하는 단일 책임 컴포넌트로 단순화
- `AttackRangeIndicator.cs`도 동일하게 타일 인덱스 대신 Player와의 실제 거리로 판정하도록 수정(단, 이쪽은 기존대로 비영구적 — 시야 범위를 벗어나면 다시 사라짐)
- **`FogOfWarGround.cs` 신규 작성** — 바닥 전체를 덮는 안개를 텍스처 한 장으로 실시간으로 걷어내는 방식(고전적인 Fog of War 텍스처 기법). 시작 시 바닥 크기에 맞는 `Texture2D`를 전부 불투명 안개색으로 채우고, 매 프레임 Player의 월드 좌표를 텍스처 좌표로 환산해 그 주변 원형 영역의 알파를 0(완전 투명)으로 지움 — 텍스처는 다시 채워지지 않으므로 걷힌 영역이 영구적으로 유지됨(안개 재생성 없음). 재질은 URP/Unlit Transparent이며 베이스 컬러를 흰색으로 두고 텍스처의 RGBA를 그대로 사용
- 씬 구성 변경: 기존 `Path/FogTiles`(경로 폭 전용, 20개 오브젝트) 전부 제거 → 바닥 전체를 덮는 단일 `FogGround`(Plane, 바닥과 동일 크기/위치, Collider 없음) 하나로 대체, `FogOfWarGround` 부착(visionRadius=8, textureWidth=128)
- **검증**: Play 모드에서 리플렉션으로 `RevealAt`을 직접 호출 — ①Player 위치 기준 정확히 원형으로 안개가 걷히는 것을 스크린샷으로 확인 ②경로를 따라 여러 지점을 순서대로 공개시켜 지나온 자리들이 서로 겹치며 계속 남아있는(트레일 형태로 누적) 것을 확인 — 캐릭터가 지나간 자리는 원형으로 걷히고 그 흔적이 영구히 남는 의도한 동작 확인
- ⚠️ 텍스처 해상도(128폭, 세로는 바닥 종횡비에 맞춰 256 자동 계산)가 낮아 원 테두리가 계단 형태로 다소 거칠게 보임 — 필요하면 `textureWidth`를 높여 부드럽게 만들 수 있음(성능 여유 있음)
- ⚠️ `AttackRangeIndicator`/`FogVisibility`/`FogOfWarGround` 세 곳 모두 `visionRadius`를 독립적으로 갖고 있음(공유 설정 객체 없음) — 현재는 전부 8로 통일해뒀지만, 나중에 값을 조정할 때 세 군데 다 같이 맞춰야 함

### 부록 — 몬스터 공격이 벽을 관통하던 치명적 버그 수정 (2026-09-09)
- 사용자 제보: "몬스터 공격이 벽을 통과하는거같다" — 실제로 핵심 메커니즘(★)이 깨져있던 심각한 버그였음
- **원인 ①(직접 원인, 터널링)**: 사용자가 직접 투사체 속도를 25로 올렸는데(허용된 자유 조정), 물리 프레임 간격(0.02초) 동안 이동 거리가 0.5유닛으로 벽 두께(0.2유닛)보다 커져 `Discrete` 충돌 감지로는 프레임 사이에 벽을 그냥 통과해버림(터널링). `Projectile`이 `transform.position`을 직접 바꾸는 방식이라 `Rigidbody.collisionDetectionMode`를 `Continuous`로 바꿔도 물리 엔진이 그 이동을 감지하지 못한다는 것도 함께 확인
- **원인 ②(더 근본적, 훨씬 이전부터 있던 버그)**: `Projectile_Egg.prefab`이 실수로 `WallVision`(벽) 레이어에 올라가 있었음(0단계에서 한 번 고쳤던 것이 이후 리임포트/재구성 과정에서 다시 되돌아간 것으로 추정). 기존 `OnTriggerEnter` 방식에서는 자기 자신과는 트리거가 발생하지 않아 드러나지 않다가, 이번에 충돌 판정 방식을 바꾸면서 겉으로 드러남
- **최종 해결책**: `Projectile.cs`를 매 `FixedUpdate`마다 이번 프레임 이동 구간을 `Physics.SphereCast`로 미리 검사하는 방식으로 재작성 — Trigger 기반 `OnTriggerEnter`를 완전히 대체(고속 이동에서 Trigger의 Continuous 충돌 감지가 신뢰할 수 없었음). 자기 자신의 콜라이더와 겹치는 현재 위치에서 바로 캐스트하면 스스로를 맞은 것으로 오검출하므로, 자기 반지름만큼 진행 방향으로 띄운 지점부터 캐스트하도록 처리
- `Projectile_Egg.prefab` 레이어를 `Default`로 복구
- **검증**: Play 모드에서 `Physics.simulationMode = SimulationMode.Script` + `Physics.Simulate()`로 물리 스텝을 수동 강제 진행시켜(에디터 포커스 문제로 실시간 진행이 막혀있어 채택한 방법) 검증 — ①벽 없이 발사 시 목표까지 정상 도달·명중 ②벽을 사이에 두고 발사(투사체 속도 25 그대로) 시 벽 위치에서 정확히 멈추고 "막힘" 로그 출력, 벽 너머로 전혀 넘어가지 않음을 확인. 실제 사용자가 올린 속도(25)에서도 재현 검증 완료

### 부록 — 5단계 재재보완: 안개를 경로 폭이 아니라 바닥 전체로 확장 + 시야 4칸으로 축소 + 카메라 재조정 (2026-09-09)
- 사용자 피드백: 안개 타일이 경로(2유닛 폭)만 덮어서 카메라 화면에는 여전히 안개가 잘 안 느껴짐 — 시야 밖은 화면 전체가 어두운 안개로 덮여야 함. 시야 거리도 5→4칸으로 줄이고 그만큼 카메라를 좀 더 당겨(확대)달라는 요청
- `FogVisibility.cs`에 `fixedTileIndex`(기본 -1) 필드 추가 — 지정 시 위치 기반 자동 타일 탐지 대신 이 값을 그대로 사용. 안개 타일을 바닥 전체 폭으로 넓히면 그 중심 위치가 실제 경로 타일 위치에서 멀어져 자동 탐지가 틀어질 수 있어, 생성 시점에 알고 있는 정확한 타일 인덱스를 직접 박아넣기 위함
- `FogTile_00~19` 재구성: 세그먼트 A(경로가 Z축 진행)는 X 방향으로 폭 34유닛(바닥 전체 X범위 -15~15 커버) × Z 두께 2유닛, 세그먼트 B(경로가 X축 진행)는 반대로 Z 방향 폭 64유닛(바닥 전체 Z범위 -30~30 커버) × X 두께 2유닛으로 변경 — 진행 방향에 수직인 "안개 벽"이 바닥 전체 폭을 가로질러 덮도록 함. 각 타일에 `fixedTileIndex`를 명시적으로 지정
- `visionTileDistance`를 5→4로 축소 (Chicken.prefab의 `AttackRangeIndicator`·`FogVisibility`, FogTile 20개 전부)
- `CM_TopDown` FOV를 60→48로 축소(살짝 확대) — FollowOffset (0,17,-12)/피치 55도는 유지
- **검증**: 실제 게임 카메라(FOV 48) 스크린샷 — 화면 절반 이상이 안개로 덮이고 Player 주변 좁은 영역만 드러남, 이전보다 훨씬 뚜렷한 "시야가 가려진" 느낌 확인. 위에서 내려다보는 구도로도 바닥 전체가 안개로 덮여있다가 Player 주변만 뚫려있는 것을 재확인

### 부록 — 5단계 재보완: 바닥에 실제 안개 타일 추가 (2026-09-09)
- 사용자 피드백: 몬스터만 숨겨서는 "시야가 가려진 느낌"이 안 남 — 실제로 안개처럼 시야가 차단된 시각 효과가 필요
- `FogVisibility.cs`에 `hideWhenRevealed`(bool) 옵션 추가 — 기존 동작(시야 밖=숨김, 안=공개)의 정반대(시야 밖=보임(안개 자체), 안=숨김)도 같은 컴포넌트로 처리하도록 확장. `ComputeVisible(revealed)`로 두 모드를 한 코드에서 분기
- `M_Fog.mat` 신규 생성(URP/Unlit, Surface Type=Transparent, 진한 회색 `(0.06,0.06,0.08,0.97)`) — 처음엔 플로어 색(0.5,0.5,0.5)과 거의 같은 회색으로 만들어서 안 보이는 문제가 있었음, 뚜렷이 대비되는 어두운 색으로 재조정
- `Path/FogTiles` 컨테이너 하위에 경로 타일 20개 위치 각각에 `FogTile_00~19`(Plane 프리미티브, 2x2 유닛, Y=0.05, MeshCollider 제거, `FogVisibility(hideWhenRevealed=true)`) 생성 — 경로 자체를 안개로 덮고, 시야 안에 들어온 타일만 순차적으로 걷히며 한 번 걷히면 계속 유지(기존 sticky reveal 로직 재사용, 별도 상태 관리 불필요 — Player 타일 인덱스가 단조 증가하기 때문에 자동으로 영구 공개됨)
- **버그 발견 및 수정**: 안개 타일을 만들었는데도 실제 게임 카메라(`CM_TopDown`)에서는 화면 가장자리에 살짝만 보임 → 원인은 카메라 FOV가 40으로 좁아서 시야 범위가 좁았음. `manage_camera(set_lens)`로 FOV를 60으로 확장해 안개 벽이 화면에 충분히 잡히도록 수정
- 검증: 위에서 내려다보는 포지션 스크린샷으로 안개가 정확히 L자 경로 모양대로 덮여있음을 확인 → 실제 게임 카메라(FOV 60) 스크린샷으로 정상 프레임에 안개 벽이 잡히는 것 확인 → 리플렉션으로 Player 타일 인덱스를 0→12로 강제 이동시켜 "지나온 타일(0~5)은 계속 공개, 건너뛴 타일(6~11)은 안 지나갔으므로 계속 안개, 새로 진입한 타일(12~17)은 즉시 공개"를 확인 — 순차적 안개 걷힘과 영구 공개 모두 의도대로 작동
- ⚠️ 안개 타일은 시야 차단 전용이며 Collider가 없어 NavMesh·벽 배치 레이캐스트(`WallSlot` 레이어)에 영향 없음

### 부록 — 5단계 보완: 몬스터(및 향후 환경) 자체를 시야 밖에서 완전히 숨김 (2026-09-09)
- 사용자 피드백: 기존 5단계 구현(공격 사거리 표시만 시야로 가림)은 잘못됐고, "안개처럼" 몬스터 본체를 포함해 시야 밖 대상을 아예 안 보이게 해야 함. 경로 자체는 원래 설계(design.md 3.7 "경로 자체는 보여도 된다")대로 항상 보이게 유지
- `FogVisibility.cs` 신규 작성 — 범용 재사용 컴포넌트. 자기 위치의 타일 인덱스를 구해 Player의 현재 타일 인덱스와 비교(`AttackRangeIndicator`와 동일한 타일 거리 공식), 시야 밖이면 자식의 모든 `MeshRenderer`/`SkinnedMeshRenderer`를 비활성화. 한 번 시야 안에 들어오면 `revealed` 플래그로 영구 고정(다시 멀어져도 계속 보임)
  - 향후 환경 오브젝트가 추가되면 이 컴포넌트만 붙이면 동일하게 동작하도록 범용으로 설계(몬스터 전용 로직 아님)
- `Chicken.prefab` 루트에 `FogVisibility` 컴포넌트 추가(헤드리스 프리팹 편집)
- 검증: 리플렉션으로 Player 타일 인덱스를 강제 조작해 3단계로 확인 — ①멀리 있을 때(player=-10) Chicken의 `SkinnedMeshRenderer.enabled=False` ②사거리 안(player=2) 진입 시 `True`로 전환 ③다시 멀어져도(player=-10) `True` 유지(영구 공개) — 의도한 대로 정확히 동작
- **테스트 씬에서 바로 확인하기 위해 Monster 위치를 재배치**: 기존 (-11,0,0)(타일 인덱스 5, 시야값과 같아 시작 즉시 공개되던 위치) → (-6,0,11)(타일 인덱스 10)로 이동. Player 타일 0에서 시작하면 `distanceAhead=10`으로 시야(5) 밖이라 처음엔 완전히 숨겨져 있다가, Player가 타일 5에 도달하는 순간 몬스터 모델+사거리 표시가 동시에 드러남을 Play 모드에서 실측 확인(리플렉션으로 `SetCurrentPlayerTileIndex` 조작 후 스크린샷 확인)
  - ⚠️ 이 위치는 여전히 인프라 검증용 임시 배치이며, 실제 스테이지 몬스터 배치는 6/8단계에서 별도로 구성함
  - **[폐기됨]** 이 (-6,0,11) 좌표는 6단계에서 `Monster_A~D`(스테이지1)/`Monster_DPS_1~2`·`Monster_Area_1~2`·`Monster_Basic_1~2`(스테이지2)로 완전히 대체되어 지금은 씬에 존재하지 않음. 현재 실제 배치 좌표는 P1 프리팹·씬 값 덤프 기록 참조(P1 정리, 2026-09-11)

### 부록 — 몬스터가 사거리 안의 Player를 계속 바라보게 처리 (2026-09-09)
- `MonsterTower.cs` 수정: 기존에는 쿨다운마다 한 번씩만 `OverlapSphere`로 대상을 찾아 즉시 발사하던 구조였는데, 이를 매 프레임 대상 탐지(`FindClosestTarget`)로 분리하고 대상이 사거리 안에 있는 동안은 매 프레임 `FaceTarget`으로 그쪽을 향해 회전(Y축만, `Quaternion.RotateTowards`로 부드럽게)하도록 변경. 발사 로직 자체(쿨다운, 벽 판정과 무관)는 그대로 유지
- `rotationSpeed`(초당 360도, 테스트용) 필드 신규 추가
- 검증: 리플렉션으로 Player를 몬스터 사거리 안으로 이동시켜 몬스터가 그 방향으로 회전함을 확인, 이후 Player가 자동 이동으로 사거리를 벗어나자 더 이상 회전하지 않고 마지막 각도를 유지함을 확인(사거리 밖에서는 안 따라감 — 의도한 동작)

### 5단계 — 시야 제한 표시 + 카메라 확장 + 몬스터/투사체 2배 스케일 (2026-09-09 완료)
- 사용자 요청: 시야(정보) 제한이 되는 만큼 카메라 뷰 자체는 더 넓게 당겨서 답답함을 줄이고, 넓어진 만큼 Monster(Chicken)와 투사체 크기를 2배로 키워달라
- `AttackRangeIndicator.cs` 신규 작성 — `LineRenderer`로 몬스터의 `attackRange` 반경을 바닥에 원형(48분할)으로 그림. `pathManager.GetCurrentPlayerTileIndex()`와 몬스터 자신의 타일 인덱스 차이가 `visionTileDistance`(기본 5) 이내일 때만 `lineRenderer.enabled=true` — 실제 시야 차단이 아니라 "정보 제한" 연출용 표시
  - `MonsterTower.cs`에 `public float AttackRange => attackRange;` getter 추가(인디케이터가 반경 값을 읽어가도록)
  - `Assets/Materials/M_RangeIndicator.mat` 신규 생성(URP/Unlit, 주황 `[1,0.3,0.1,0.8]`) — `manage_components`로 LineRenderer 머티리얼 프로퍼티명을 지정하는 시도(`"material"`/`"m_Materials"`)가 모두 실패해 `execute_code`로 `lineRenderer.sharedMaterial` 직접 대입
  - `Chicken.prefab`에 `LineRenderer` + `AttackRangeIndicator` 컴포넌트 추가
- `Chicken.prefab`과 `Projectile_Egg.prefab` 루트 스케일을 (1,1,1)→(2,2,2)로 변경. `MonsterTower.projectileSpawnHeight`를 0.3→0.6으로 보정(2배 커진 몸통 기준 발사 높이 재조정)
- `CM_TopDown`의 `CinemachineFollow.FollowOffset`을 (0,10,-7)→(0,17,-12)로 확장(55도 쿼터뷰 각도는 유지) — 뷰 범위를 넓혀 답답함 해소
- 씬의 `Monster` 인스턴스 위치/회전이 그동안의 테스트로 살짝 틀어져 있던 것을 (-11,0,0)/(0,0,0)으로 재정리
- **버그 발견 및 수정**: Play 테스트 직후 콘솔에 `ArgumentNullException: Value cannot be null. Parameter name: dest` 5건 발생 — `PlayerHealth.FlashHitRoutine()`이 `Awake()`에서만 초기화되는 `propertyBlock` 필드를 널 체크 없이 사용하다가, 세션 중 반복된 스크립트 재컴파일(도메인 리로드)로 `propertyBlock`이 null인 채로 재진입해 발생. `FlashHitRoutine` 진입 시 `propertyBlock`이 null이면 새로 생성하도록 방어 코드 추가해 해결
- **검증**: Play 모드 진입 → 리플렉션으로 `PlayerHealth.TakeDamage(15)` 직접 호출해 체력 정상 감소(100→85) 및 예외 미발생 확인(콘솔에 무관한 NoSubscription 에러만 존재) → `AttackRangeIndicator`의 `monsterTileIndex`(5)와 `GetCurrentPlayerTileIndex()`(0) 비교로 `lineRenderer.enabled=true` 정상 확인 → 스크린샷으로 넓어진 카메라 뷰와 주황 원형 시야 표시 육안 확인

### 부록 — 피해량 숫자 팝업 UI (4단계 마무리, 2026-09-09)
- `DamageNumberPopup.cs` 작성 — 월드 좌표를 매 프레임 `Camera.WorldToScreenPoint`로 화면 좌표 변환(Screen Space-Overlay 캔버스 하위에서 RectTransform.position에 화면 픽셀 좌표를 직접 대입하는 방식). 살짝 위로 떠오르며 후반부에 페이드아웃 후 자기 파괴
- `DamageNumber.prefab` 생성(Text + `DamageNumberPopup`)
- `Projectile.cs`에 `SpawnDamageNumber` 추가 — 대상에게 명중(`IDamageable.TakeDamage` 호출) 직후 피격 지점 근처에 팝업 생성. **벽에 막힐 때는 생성 안 함**(피해가 없으므로)
  - ⚠️ 프리팹 에셋은 씬의 Canvas를 직접 참조할 수 없어서(직렬화 시 씬 오브젝트 참조 불가), `damageNumberParent`가 비어있으면 런타임에 `FindFirstObjectByType<Canvas>()`로 찾아 캐싱하도록 처리
- 검증: 리플렉션으로 `MonsterTower.TryAttack()` → `Projectile.OnTriggerEnter()`를 직접 호출해 확인 — 체력 100→90 정상 감소, 피해량 텍스트가 정확히 "10"으로 표시되는 팝업 생성 확인

### 부록 — 긴급 플레이테스트 대응: 4단계 앞당김 + 버그 2건 수정 (2026-09-09)
- 사용자가 "오늘 당장 녹화해야 해서 단계 순서 무시하고 지금 플레이에 필요한 기능부터 만들어달라"고 요청 — 이번 작업은 CLAUDE.md의 "한 단계씩" 원칙을 사용자가 명시적으로 일시 해제한 예외
- **`PlayerHealth.cs` 작성** — `IDamageable` 구현(3단계에서 만든 인터페이스가 그대로 연결됨, Projectile 쪽 코드 변경 전혀 없이 작동). 피해를 받으면 `MaterialPropertyBlock`으로 Model을 잠깐 붉게 물들였다가 복구(피격 색상, 재질 인스턴스 생성 없이 처리). 체력 0 이하가 되면 `PlayerAutoMover` 비활성화 + `NavMeshAgent.isStopped=true` + Animator `Die` 트리거 + `OnDied` 이벤트 발행
- **`PlayerHealthUI.cs`** — `CurrencyUI`와 동일 패턴으로 체력 텍스트 표시
- **`StageManager.cs` 작성** — Player 위치와 마지막 타일 위치 사이 거리로 클리어 판정(타일 인덱스 기반이 아니라 실제 거리 기준 — 이유는 아래 버그 참조), `PlayerHealth.OnDied`로 실패 판정. 한 번 끝나면(`isFinished`) 이후 어느 쪽 이벤트가 와도 결과가 덮어써지지 않음(선착 우선)
- 씬에 `HealthText`, `ResultText`(가운데 큰 글씨, CLEAR/GAME OVER) UI 추가, `Player`에 `PlayerHealth` 부착, `Managers`에 `StageManager` 부착·연결
- **버그 발견 및 수정 ①: Fox가 이동해도 걷기 애니메이션이 재생되지 않던 문제(사용자 제보)** — 원인은 `PlayerAutoMover`가 애초에 Animator를 전혀 참조하지 않아서 `IsMove` 파라미터를 아무도 설정하지 않고 있었음(1단계 때부터 있던 누락). `PlayerAutoMover`에 Animator 참조(자동 탐색) 추가하고 매 프레임 `agent.velocity`로 `IsMove` 갱신하도록 수정
- **버그 발견 및 수정 ②(더 심각함): `PathManager`가 타일을 20개가 아니라 21개로 읽고 있었음** — 2단계에서 `WallSlots`(벽 슬롯 40개 담는 컨테이너)를 `Path`의 **직속 자식**으로 만들었는데, `PathManager.BuildTilesFromChildren()`은 `tileRoot`의 자식을 전부(이름 구분 없이) 타일로 취급해서 `WallSlots` 자체가 21번째 "타일"(월드 원점 (0,0,0))로 잘못 포함됨. Player가 실제 경로를 다 돌고 나서 이 가짜 마지막 타일(원점)까지 걸어가서 멈추는 버그로 나타남 — 걷기 애니메이션 버그를 파던 중 우연히 발견함
  - 수정: `Path` 아래에 `Tiles` 컨테이너를 새로 만들어 `Tile_00~19`를 그 밑으로 재배치하고, `PathManager.tileRoot`를 `Tiles`로 재지정. `WallSlots`는 `Path`의 형제로 그대로 유지
- 카메라 각도를 완전 수직(90°)에서 쿼터뷰 느낌(피치 55°, FollowOffset (0,10,-7))으로 조정 — 사용자 요청
- **Play 모드에서 전체 루프 리플렉션/직접 호출로 종단 검증**: 타일 수 20개 확인 → 이동 중 `IsMove=true`+`Fox_Walk_InPlace` 상태 확인(스크린샷으로 달리는 포즈 육안 확인) → 실제로 마지막 타일(12,0,8)까지 도달해 `StageManager`가 "CLEAR" 표시 확인 → `TakeDamage` 호출로 체력 감소(100→40→0)·UI 갱신·사망 시 이동 정지 확인 → 이미 CLEAR된 이후라 GAME OVER로 덮어쓰지 않는 것까지 확인(1회성 래치 정상 작동)
- ⚠️ 이번에도 넣지 않은 것(시간 우선순위상 보류): 피해량 숫자 팝업 UI(화면 좌표 변환 필요, 손이 많이 감), 재도전 버튼, 5단계 시야 제한. 필요하면 다음에 이어서

---

### 3단계 — 몬스터 반전 로직 ★핵심 (2026-09-09 완료)
- `IDamageable.cs` 작성 — `TakeDamage(float)` 인터페이스. PlayerHealth(4단계)가 아직 없어서 Projectile은 대상에게 `GetComponent<IDamageable>()`이 없으면 안전하게 아무 동작도 안 함(null 체크). 4단계에서 PlayerHealth가 이 인터페이스만 구현하면 Projectile 쪽 코드 변경 없이 바로 연동됨
- `Projectile.cs` 작성 — 발사 시점에 조준 위치 기준 방향을 한 번 계산(`Launch`), 이후 그 방향으로 직선 이동. `OnTriggerEnter`로 대상 레이어(AttackTarget) 충돌 감지, `IDamageable.TakeDamage` 호출 후 반환(파괴). 유지시간(3초) 지나면 충돌 없어도 반환
- `MonsterTower.cs` 작성 — 고정형(이동 로직 없음). 쿨다운마다: ①`Physics.OverlapSphere`로 원형 사거리 내 `AttackTarget` 레이어 탐색(가장 가까운 대상 선택) → ②탐지 위치~조준 위치 사이 `WallVision` 레이어만 검사하는 Raycast로 벽 차단 확인 → ③통과 시 투사체 발사, 막히면 그대로 다음 쿨다운까지 대기(재탐색 로직 재사용 = 자동으로 "막은 만큼 안 맞는다" 구현됨) ※ **이후 아래 "부록 — 벽 차단 방식 변경"에서 ②번 로직은 제거되고 투사체 쪽으로 이동함. 최신 동작은 부록 참조**
- `Projectile_Egg.prefab`: 원래 있어야 할 `Rigidbody`(kinematic, useGravity=false)가 리임포트 과정에서 사라져있던 것을 발견해 복구 + `Projectile` 스크립트 연결(targetLayerMask=AttackTarget). Rigidbody가 없으면 Player 쪽에도 Rigidbody가 없어서 `OnTriggerEnter` 자체가 발생하지 않는 문제였음(Unity 트리거 규칙: 최소 한쪽에 Rigidbody 필요)
- `Chicken.prefab`: `MonsterTower` 스크립트 연결 (attackRange=6 / attackCooldown=1.5s / attackDamage=10 — 테스트용 수치, 실제 밸런스는 6/8단계에서 조정. targetLayerMask=AttackTarget, wallVisionLayerMask=WallVision, projectilePrefab=Projectile_Egg.prefab)
- 씬의 `Monster`(Chicken.prefab 인스턴스)를 경로 근처(-11, 0, 0)로 테스트 배치 — 이전엔 경로에서 너무 멀어(최소 거리 8~10) 사거리 6 안에 전혀 들어오지 않았음. 이 위치는 순수 테스트용이며 실제 스테이지 몬스터 배치는 6/8단계에서 별도로 함
- **핵심 메커니즘(★ 검증 게이트) 확인 완료**: Play 모드에서 리플렉션으로 `MonsterTower.TryAttack()`을 직접 호출해 검증
  - 벽 없음: 투사체 정상 발사(생성 위치 정확) → Player와 충돌해 `[Projectile] Player에게 명중` 로그 확인, 실제 자동 쿨다운 루프로도 반복 발사 확인
  - 벽 있음(Monster-Player 사이에 `Wall.prefab` 배치): 같은 호출인데도 투사체가 전혀 생성되지 않음 → Raycast 차단 로직이 정상 작동함을 확인
  - **"벽을 세우면 몬스터 공격을 안 맞는다"가 실제로 동작함 — design.md 3단계 검증 게이트 통과**
### 부록 — 벽 차단 방식 변경: "발사 자체를 막음" → "투사체가 벽에 맞아 막힘" (2026-09-09)
- 사용자 피드백: 공격 애니메이션은 이대로(없이) 가고, 벽이 있어도 몬스터는 계속 발사하되 투사체가 벽에 맞아 막히는 느낌으로 바꿔달라고 요청
- `MonsterTower`에서 벽 차단 레이캐스트(`IsBlockedByWall`)와 `wallVisionLayerMask` 필드를 완전히 제거 — 사거리 안에 대상이 있으면 무조건 발사
- `Projectile`에 `wallLayerMask`(WallVision) 필드 추가. `OnTriggerEnter`에서 벽 레이어와 먼저 충돌 검사해 맞으면 피해 없이 그 자리에서 파괴(반환) — 대상(Player) 충돌 판정보다 우선 처리
- Play 모드에서 리플렉션으로 재검증: 벽이 있어도 `투사체 발사` 로그는 매 쿨다운마다 계속 뜨고, 투사체가 벽에 닿으면 `Projectile] ...(벽)에 막힘` 로그가 뜨며 파괴됨(Player에게 명중 로그는 안 뜸) → 의도한 대로 동작 확인
- 이 변경으로 `MonsterTower`는 더 단순해짐(거리 검사 → 발사, 벽 관련 로직 없음), 벽 차단은 전적으로 투사체의 물리 충돌로 처리

- ⚠️ **알려진 공백(4단계 이전 처리 예정)**: 공격 애니메이션이 없음. 사용자 확인: 이번 프로토타입에서는 공격 애니메이션 없이 진행하기로 함. `Anicon_Chicken.controller`를 이번 세션에 재구성할 때 Idle/Move/Die만 넣고 Attack 상태를 안 만들었는데, 애초에 Chicken용 공격 애니메이션 클립 자체가 현재 `Assets/Animation/`에 없음(이전 `Anim_Monster_ Attack.anim`이 삭제됨). 지금은 몬스터가 시각적 모션 없이 투사체만 발사함 — 애니메이션 자산이 준비되면 `Anicon_Chicken`에 Attack 상태 추가하고 `MonsterTower`에서 트리거하는 작업 필요
- ⚠️ Play 모드 테스트 중 에디터 창 포커스가 다시 여러 번 끊김 — 이번엔 `execute_code`로 `EditorApplication.isPaused=false` 직접 호출 시 잠깐 정상 진행되다가도 다시 멈추는 패턴 확인. 안정적인 실시간 관찰이 어려워 리플렉션 직접 호출 방식으로 검증을 대체함 (콘솔 로그 조회(`read_console`)도 이 과정에서 가끔 최신 로그를 못 가져오는 현상 있었음 — Unity 자체 Editor.log 파일로 교차 확인)

### 부록 — 탑다운 카메라 버그 수정 (2026-09-09)
- 사용자가 Play 모드에서 카메라가 탑다운이 아니라고 지적, 확인 결과 두 가지 문제 발견:
  1. `CM_TopDown`의 `CinemachineFollow.FollowTarget`이 `null`이었음 — Fox로 Player를 교체하며 기존 Player 오브젝트를 삭제·재생성할 때 Follow 참조를 다시 연결하지 않아서 발생한 회귀
  2. 애초에(0단계 때부터) 진짜 탑다운 각도가 아니었음 — 카메라 회전이 (0,0,0), `FollowOffset`이 (0,0,-10)으로 캐릭터 뒤에서 같은 높이로 따라가는 3인칭에 가까운 구조였음
- `manage_camera(set_target)`으로 Follow/LookAt을 새 `Player`(Fox 인스턴스)로 재연결. `FollowOffset`을 (0, 14, 0.001)로, `CM_TopDown`의 회전을 (90, 0, 0)으로 바꿔 캐릭터 바로 위에서 수직으로 내려다보며 따라가는 카메라로 변경
- Play 모드 스크린샷으로 정상 작동 확인(캐릭터가 항상 화면 중앙, 수직 상공에서 따라옴)
- ⚠️ 참고: `LookAt`도 Player로 설정했지만 현재 Aim 컴포넌트(Rotation Composer 등)는 없음 — 지금은 고정 회전(90,0,0)만으로 충분하지만, 5단계(시야 제한 카메라)에서 카메라 리그를 제대로 만들 때 이 부분 다시 정리할 것

### 2단계 — 벽 배치 & 재화 (2026-09-09 완료)
- 사용자 확인 사항: 벽 비주얼=단순 큐브 placeholder, 재화 시작 총량=10, 벽 1개당 비용=1 (전부 테스트용, 실제 밸런스는 6/8단계에서 조정)
- `SlotSide.cs` (Left/Right 구분 enum) 작성
- `WallSlot.cs` 작성 — 타일 인덱스·좌우 구분만 보유하는 데이터 컴포넌트 (클릭은 WallPlacementManager가 중앙에서 처리)
- `CurrencyManager.cs` 작성 — 시작 시 총량 지급, `TrySpend`, `OnCurrencyChanged` 이벤트
- `WallPlacementManager.cs` 작성 — **New Input System(`Mouse.current`)로 클릭 위치를 레이캐스트**해 `WallSlot` 감지 → `PathManager.CanPlaceAt` + 슬롯 점유 + 재화 확인 후 벽 프리팹 생성
  - ⚠️ 이 프로젝트는 `Active Input Handling`이 "Input System Package (New)"로 설정돼 있어(`ProjectSettings.asset`에서 확인) **legacy `OnMouseDown`은 동작하지 않음** — 반드시 New Input System 기반으로 구현해야 함. 다음 단계에서 유사한 클릭/입력 처리를 만들 때도 동일하게 주의할 것
- `PathManager.cs`에 `GetTileForward`, `IsSlotOccupied`, `SetSlotOccupied` 메서드 추가 (타일별 좌/우 슬롯 점유 상태는 기존 `TileData`의 `LeftSlotOccupied`/`RightSlotOccupied` 필드를 그대로 재사용)
- `CurrencyUI.cs` 작성 — CurrencyManager 값을 UGUI Text로 표시. **스크립트 실행 순서 버그 발견 후 수정**: `OnEnable`에서 바로 `CurrentCurrency`를 읽으면 다른 오브젝트의 `CurrencyManager.Awake()`가 아직 실행 전이라 0으로 보임 → 초기 표시를 `Start()`로 이동해 해결 (재사용 가능한 교훈: 다른 매니저 값을 초기 표시할 때는 `Start` 사용)
- 씬 구성: `Wall.prefab`(큐브, `WallVision` 레이어, BoxCollider) 생성. `Path/WallSlots` 하위에 타일 20개 × 좌우 2개 = **40개 WallSlot 마커** 배치(각 타일의 진행 방향 기준 좌/우, 새 레이어 `WallSlot`(10번) 적용). `Managers` 오브젝트에 `CurrencyManager`+`WallPlacementManager` 부착. `Canvas`(Screen Space-Overlay) + `CurrencyText`(UGUI Text) 구성
  - ⚠️ Canvas를 코드로 생성하면 기본 `renderMode`가 World Space(2)로 잡히는 것을 발견 → Screen Space-Overlay(0)로 명시적으로 수정. 다음에 Canvas를 새로 만들 때도 renderMode를 확인할 것
  - ⚠️ 기본 UGUI `Text`의 내장 폰트(Arial)는 한글 글리프가 없어 한글 텍스트가 깨짐(빈 사각형) → Windows 시스템 폰트 `malgun.ttf`(맑은 고딕)를 `Assets/Fonts/`로 임포트해 연결. **한글 UI 텍스트를 쓸 때마다 폰트가 한글을 지원하는지 확인할 것** (4단계 피해량 숫자 UI 등에서도 동일하게 주의)
- Play 모드에서 리플렉션으로 배치 로직 직접 호출해 검증: 이미 지나간 타일(0번) 배치 차단 확인, 앞쪽 타일(5번) 배치 성공 확인(재화 10→9, 슬롯 점유 처리, 벽 오브젝트가 올바른 위치·레이어(`WallVision`)로 생성됨), 같은 슬롯 중복 배치 차단 확인. 스크린샷으로 재화 UI와 벽 렌더링 육안 확인
  - **미검증**: 실제 마우스 클릭(레이캐스트 입력 경로 자체)은 리플렉션 호출로 우회 검증했고 사람이 직접 클릭해서 확인하지는 않음 — 다음 세션에서 실제로 클릭해서 벽이 배치되는지 확인 권장

### 부록 — Player 캐릭터 에셋을 Fox로 교체 (2026-09-09)
- 애니메이션 연결을 재검토하던 중 리깅 타입 불일치(Humanoid ↔ Generic)로 어떤 조합도 애니메이션이 정상 재생되지 않는 문제가 반복 발생. 사용자가 **기존 Player 에셋(Monster.prefab, Monster_Male.fbx)을 전부 정리하고 새 "Fox" 에셋으로 교체**하기로 결정
- 사용자가 `Assets/Fox/` 폴더에 새 캐릭터 에셋(모델, 애니메이션 클립 다수, 데모용 Animator Controller, 데모 씬)을 임포트함. `Monster.prefab`/`Monster_Male.fbx`는 사용자가 직접 삭제함
- **역할 확정**: `Fox.prefab` = Player, `Chicken.prefab` = Monster(고정형 타워)(변경 없음) — CLAUDE.md도 이에 맞춰 갱신됨
- 사용자가 기존 `Anicon_Player.controller`/`Anim_Player_*` 클립 이름을 `Anicon_Chicken.controller`/`Anim_Chicken_*`로 직접 변경(GUID 유지) — Chicken(Monster 역할)에 재사용하려는 의도. 기존 `Anicon_Monster`/`Anim_Monster_*`는 삭제함
- 위 변경사항 반영해 진행한 작업:
  - `Fox.prefab`을 0단계 규격대로 재구성: `Model` 자식 생성 → 기존 `fox` 메쉬 + `FoxTransform` 리그를 `Model` 아래로 이동 → `Model`에 Animator 재부착(avatar=`Fox.fbx`, applyRootMotion=false) → 루트에 `CapsuleCollider`(Radius 0.5/Height 1.8/Center 0,0.9,0), `NavMeshAgent`(Radius 0.5/Height 1.8/BaseOffset 0), Tag=`Player` 추가
  - `Chicken.prefab`도 동일 구조로 재건(이 세션 중 리임포트로 두 번째 손상되어 재작업함): `Model` 자식 + 루트 `CapsuleCollider`(Radius 0.25/Height 0.5/Center 0,0.25,0)
  - `Anicon_Chicken.controller`를 처음부터 새로 빌드: `IsMove`(Bool)/`Die`(Trigger) 파라미터, Idle=`Anim_Chicken_Idle`/Move=`Anim_Chicken_Move`/Die=`Anim_Chicken_Die` 상태 구성. `Chicken.prefab`의 Model Animator에 연결(avatar=null/Generic)
  - `Anicon_Fox.controller`를 신규 생성: 동일 파라미터 패턴, Idle=`Fox_Idle`/Move=`Fox_Walk_InPlace`/Die=`Fox_Falling`(임시 — Fox 팩에 전용 사망 애니메이션이 없어 대체 클립 사용, 4단계에서 교체 검토) 상태 구성. `Fox.prefab`의 Model Animator에 연결(avatar=`Fox.fbx`)
  - 씬의 기존 `Player`(삭제된 Monster.prefab 참조로 깨져있던 인스턴스, 사용자 확인 후 삭제)를 삭제하고 `Fox.prefab` 인스턴스로 재생성 → 위치(Tile_00), Tag=`Player`, Layer=`AttackTarget`, `PlayerAutoMover` 컴포넌트 + `pathManager` 참조 재설정
  - `Monster`(Chicken.prefab 인스턴스)는 프리팹 수정이 자동 반영되어 그대로 유지
- **Play 모드 검증 완료(사용자 확인, 2026-09-09)**: Fox의 Walk 애니메이션이 정상 재생됨을 확인
- **Fox 머티리얼(`M_Fox.mat`)이 URP 미변환 상태(Built-in `Standard (Specular setup)` 셰이더)라 분홍색으로 깨져 보이던 문제 발견 → `execute_code`로 `Universal Render Pipeline/Lit`로 변환, 텍스처(_BaseMap=T_Fox_BC, _BumpMap=T_Fox_Normal, _OcclusionMap=T_Fox_AO)·Specular 워크플로 값 그대로 이전. Scene 뷰에서 정상 렌더링 확인 (design.md 0단계 "머티리얼을 URP 셰이더로 변환" 항목과 동일한 종류의 필수 작업이었음, Fox 에셋 임포트 시 누락돼 있었음)
- 세션 중 애니메이션 관련 에셋(컨트롤러/클립)이 사용자에 의해 실시간으로 이름 변경·삭제되는 일이 여러 번 있었고, 그때마다 Unity 도메인 리로드가 발생해 MCP 연결이 여러 차례 끊김(재시도로 복구됨) — 특이사항으로 기록만 해둠, 문제로 조치할 필요는 없음

### 1단계 — 경로·타일 데이터 구조 (2026-09-09 완료)
- 세션 시작 시 프로젝트 상태 점검: 씬 하이어라키·프리팹·콘솔 모두 PROGRESS.md 기록과 일치, 이상 없음 확인 후 진행
- `TileData.cs` 작성 (index, worldPosition, leftSlotOccupied, rightSlotOccupied — design.md 5.1 스펙 그대로)
- `PathManager.cs` 작성 — 자식 오브젝트(타일 마커)로부터 `TileData[]` 생성, `GetTileIndexAt`, `CanPlaceAt`, `SetCurrentPlayerTileIndex`/`GetCurrentPlayerTileIndex` 제공. Scene 뷰에 노란 와이어큐브 Gizmo로 타일 그리드 표시(에디터에서 바로 정렬 확인 가능)
- `PlayerAutoMover.cs` 작성 — NavMeshAgent로 타일을 0→1→2…순서로 순차 목적지 설정(웨이포인트 방식). 매 프레임 "마지막으로 도달한 타일 인덱스"를 PathManager에 전달
- 씬에 `Path` 오브젝트 생성, 그 아래 테스트 경로용 타일 마커 20개(`Tile_00`~`Tile_19`, 빈 오브젝트) 배치 — 직선 10칸 + 90도 회전 + 직선 10칸의 ㄴ자(L자) 경로
- `Path`에 `PathManager` 부착, `Player`에 `PlayerAutoMover` 부착 후 `pathManager` 참조 연결
- `Player` 시작 위치를 `Tile_00` 위치로 이동
- Play 모드 실측: Player가 20개 타일을 순서대로 통과해 마지막 타일(12,0,8)까지 정확히 도달, 꺾이는 지점에서 진행 방향(+X)으로 자연스럽게 회전. Scene 뷰 Gizmo로 타일 그리드가 L자 경로와 정확히 정렬되는 것도 확인. 콘솔 에러 없음(무관한 AI Generators 구독 에러만 존재)

### 0단계 — 프로젝트 셋업 & 에셋 임포트 (2026-09-07 완료)
- Input System, AI Navigation은 기존에 설치돼 있었음. **Cinemachine(3.1.7) 신규 설치** (사용자 승인 받고 진행 — CLAUDE.md "패키지 추가 금지"에 걸리는 사항이라 사전 확인함)
- 레이어 2개 생성: `AttackTarget`(8번), `WallVision`(9번)
- 프리팹 구조를 design.md 규격(루트=로직, `Model` 자식=비주얼)대로 재구성
  - `Monster.prefab`(Player용): 루트에 `NavMeshAgent` + `CapsuleCollider` 추가, Tag=`Player`
  - `Chicken.prefab`(Monster용): 루트에 `CapsuleCollider`만 추가 (고정형이라 NavMeshAgent 없음)
  - `Projectile_Egg.prefab`: 루트에 `Rigidbody`(kinematic) + `SphereCollider`(Trigger)
  - 세 프리팹 모두 기존 메쉬+Animator는 `Model` 자식으로 이동. 프리팹 파일명·루트 오브젝트 이름은 리네임 금지 규칙 때문에 그대로 유지
- 공격 애니메이션(`Anim_Monster_ Attack`)에 타격 프레임 Animation Event 추가 (`OnAttackHit`)
- 씬 구성: `Floor`(Plane, Static, 30x60 유닛) 추가 + `NavMeshSurface` 베이크, Cinemachine 탑뷰 카메라(`CM_TopDown`, Player Follow) 구성
- `Player`/`Monster` 인스턴스를 씬에 배치, Play 모드에서 NavMesh 이동·애니메이션 재생·모델 표시 검증 완료 (아래 검증 게이트 참조)

**작업 중 발견해서 같이 고친 버그 (기존 상태의 문제, 이번 세션 이전부터 있었음):**
- `Monster.prefab`(Player용)과 `Chicken.prefab`(Monster용)에 Animator Controller가 **서로 뒤바뀌어** 연결돼 있었음 → 올바르게 교체
- `Anicon_Player` 컨트롤러가 **State가 하나도 없이 비어있었음** → Idle/Move/Die 상태 구성
- `Projectile_Egg.prefab` 루트에 이름 없는 레이어(9번)가 잘못 걸려있었음 → Default로 정리

---

## 다음 할 일

> design.md 0~10단계는 전부 끝남(위 "🎯 다음 세션에 할 일" 참조). 아래는 그 외 남은 실무 항목들.

- **몬스터 타입별 전용 에셋 도입 예정** — 사용자가 별도로 준비해올 것이라고 밝힘. 현재 `Monster_AreaType`/`Monster_DPSType`는 `Chicken` 모델에 색만 입힌 placeholder(로직 루트 + `Model` 자식 분리 구조라 나중에 Model만 교체하면 됨). 새 에셋이 오면 `Fox`/`Chicken` 포함 캐릭터 에셋 전반이 바뀔 수 있음(memory 파일 `monster_character_assets_may_change` 참고)
- `visionRadius`(스테이지1=8, 스테이지2=5, 세 컴포넌트에 개별 설정됨) — 사용자가 직접 플레이하며 추가 튜닝 필요 시 `AttackRangeIndicator`/`FogVisibility`/`FogOfWarGround` 세 군데 다 맞출 것
- `PlayStats.csv`(프로젝트 루트) — 실제로 여러 번 플레이해서 데이터를 쌓은 뒤 design.md 10.4 기준(남은 재화 0~1개, 남은 체력 30~70%, 반응시간 1.5~3초)과 비교, 어긋나면 10.4 표에 따라 재화 총량·몬스터 수치·`visionRadius` 조정
- `docs/tutorial.md` 5장 연출(카메라 줌인, UI/월드 하이라이트, 상시 피드백 8종, 슬로우모션 등) — 9단계에서 의도적으로 범위 밖으로 뺀 것, 사용자가 필요하다고 하면 이어서 구현
- 옛 `TutorialPopupManager.cs`/`MonsterTutorialTrigger.cs` — 더 이상 안 쓰지만 파일 삭제 금지 규칙 때문에 프로젝트에 남아있음. 필요 없으면 사용자가 직접 지우거나, "지워도 된다"고 명시적으로 알려주면 정리 가능
- 영상 촬영, `PlayStats.csv` 데이터 기반 최종 수치 조정, 정성 설문(design.md 10.1/10.2) — 전부 사람이 진행

---

## 결정된 사항

> 설계 문서에 없어서 작업 중 정하게 된 값·이름·규칙을 기록.
> 다음 세션에 다른 값으로 만들어 어긋나는 것을 막기 위함.

### 2026-09-10 세션 핵심 결정 (빠른 참조용 — 경위는 "완료한 작업"의 해당 날짜 항목 참조)
- **씬 2개로 완전 분리**: `Stage1(Test).unity`(튜토리얼, `TutorialManager` 5단계 시퀀스) / `Stage2.unity`(본 시연, `TutorialManager` 1단계만)
- **벽 슬롯 코너 배치 규칙**: 축 정렬(0°/90°) 유지, 코너 안쪽(포켓) 대신 나가는 방향 구간이 쓰는 바깥쪽 가장자리를 그대로 갖다 쓴다. 대각선/이등분선 계산은 쓰지 않음(1차 시도했다가 사용자가 반려)
- **튜토리얼 강제 슬롯 클릭(③)**: 동적 계산 대신 씬에 고정값 하나(`TutorialManager`의 `first_hit` 스텝, 현재 타일3 Left)로 관리 — "튜토리얼은 한 번만 잘 되면 된다"는 사용자 판단. 몬스터 배치가 바뀌면 이 값도 재계산해서 맞춰야 함(방법: 몬스터 위치·사거리로 "처음 위협받는 타일" 계산 후 그 몬스터가 있는 쪽 Side 선택)
- **uGUI 버튼(재도전 등)을 쓰려면 씬에 `EventSystem`(+ `InputSystemUIInputModule`) 필수** — 둘 다 없으면 버튼이 보여도 클릭이 전혀 반응 안 함. 두 씬 모두 추가해둠, 새 씬 만들 때 잊지 말 것
- **`PlayStats.csv` 위치**: 프로젝트 루트(`Assets` 폴더 밖, `Application.dataPath/..`). 클리어/실패마다 한 줄씩 누적
- **몬스터 타입별 전용 에셋이 나중에 들어올 예정** — 지금 `Monster_AreaType`/`Monster_DPSType`는 `Chicken` 모델 + 색상 틴트만 입힌 placeholder

- 공격 대상 레이어명: `AttackTarget` (8번 슬롯), Player 루트에 적용
- 벽 시야 차단 레이어명: `WallVision` (9번 슬롯) — 실제 벽 프리팹은 2단계에서 생성 예정
- ~~Player(`Monster.prefab`) NavMeshAgent/CapsuleCollider 수치~~ → Player 에셋이 Fox로 교체되면서 아래 값으로 대체됨
- **Player(`Fox.prefab`) NavMeshAgent**: Radius 0.5 / Height 1.8 / Base Offset 0 — Fox 메쉬 바운드(가로 1.0 x 높이 1.78) 기준으로 산정
- **Player(`Fox.prefab`) CapsuleCollider**: Radius 0.5 / Height 1.8 / Center (0, 0.9, 0)
- Monster(`Chicken.prefab`) CapsuleCollider: Radius 0.25 / Height 0.5 / Center (0, 0.25, 0) (스케일은 5단계에서 2배로 변경, 콜라이더 자체 수치는 로컬 기준 그대로)
- **5단계 확정 수치**: Chicken/Projectile_Egg 루트 스케일 (2,2,2), `projectileSpawnHeight` 0.6, `visionTileDistance` 5타일, `CM_TopDown` FollowOffset (0,17,-12)
- **테스트 씬 Monster 위치(최신)**: (-6, 0, 11), 타일 인덱스 10 — 시야 밖에서 시작해 Player가 타일 5에 도달하면 드러나는 것을 보여주기 위한 임시 배치 (이전 (-11,0,0)/타일 5 위치는 폐기)
- **시야 시스템 최종 확정(타일 기반 폐기 → 캐릭터 중심 원형)**: `visionTileDistance`(타일 개수) 방식은 완전히 폐기하고 `visionRadius`(월드 유닛, 기본 8) 기반 원형 시야로 교체. `AttackRangeIndicator`/`FogVisibility`/`FogOfWarGround` 세 곳 모두 Player와의 실제 거리(`Vector3.Distance`)로 판정
- **안개 바닥(최종)**: 경로 폭 전용 `FogTile` 20개 방식은 폐기. 바닥 전체를 덮는 단일 `FogGround`(Plane) + `FogOfWarGround`(실시간 텍스처 마스킹, textureWidth=128) 하나로 대체 — 캐릭터가 지나간 자리를 원형으로 걷어내며 영구 기록
- **`CM_TopDown` FOV 최종 확정**: 40 → 60 → **48** (FollowOffset (0,17,-12)/피치 55도는 유지)
- **`CM_TopDown` Damping 확정**: PositionDamping/RotationDamping을 (1,1,1) → **(0,0,0)**으로 변경(지연 없이 즉시 추적) — 안개는 실시간 위치를 기준으로 걷히는데 카메라만 지연 추적하면 화면상 캐릭터와 안개 경계가 어긋나 보이므로, 이 게임에서는 카메라 댐핑을 쓰지 않기로 함
- **Projectile 충돌 판정 방식 확정**: `OnTriggerEnter` 대신 `FixedUpdate`에서 매 프레임 이동 구간을 `Physics.SphereCast`로 사전 검사하는 방식(고속 이동 시 Trigger 기반 Continuous 충돌 감지가 신뢰할 수 없어서 변경). `Projectile_Egg.prefab` 레이어는 `Default`로 고정(벽 레이어에 올라가면 자기 자신을 벽으로 오검출함 — 다시 실수로 바꾸지 말 것)
- Projectile SphereCollider(Trigger) Radius 0.05 / Rigidbody: useGravity=false, isKinematic=true
- `Anicon_Chicken`/`Anicon_Fox` 컨트롤러 공통 파라미터: `IsMove`(Bool), `Die`(Trigger) (Idle↔Move는 IsMove 조건, AnyState→Die는 Die 트리거) — 기존 Anicon_Player/Anicon_Monster와 동일한 패턴 유지
- 몬스터 공격 수치(테스트용): attackRange=6, attackCooldown=1.5s, attackDamage=10 — 실제 밸런스는 6/8단계에서 조정
- **벽 차단 방식(사용자 확정)**: 몬스터는 벽 유무와 상관없이 사거리 안이면 항상 발사한다. 벽은 발사를 막는 게 아니라, 날아가는 투사체를 물리적으로 막는다(투사체가 `WallVision` 레이어와 충돌하면 그 자리에서 파괴). "벽 너머 대상 제외"(사전 레이캐스트로 발사 자체를 취소)가 아니라 "투사체가 벽에 맞아 막힘"으로 시각적 피드백을 준다
- **공격 애니메이션 없음(사용자 확정)**: 이번 프로토타입은 몬스터 공격 애니메이션 없이 진행. 투사체 발사/충돌만으로 공격을 표현
- ⚠️ **[무효 기록]** 공격 Animation Event 함수명: `OnAttackHit` — 바로 위 줄에서 "공격 애니메이션 없음"으로 확정됐으므로 이 항목은 실제로 적용되지 않았음. 이후 3~10단계 어디에서도 `OnAttackHit`은 만들어지지 않았고, 몬스터는 투사체 발사/충돌만으로 공격을 표현함(P1 정리, 2026-09-11)
- 캐릭터 리깅 타입: Player 모델(`Fox.fbx`)은 **Generic/No Avatar**(Humanoid 아님, 본 이름이 `Fox_Pelvis` 등 커스텀 체계), Monster 모델(`Chicken.fbx`)도 Generic/No Avatar
- Fox의 Die 애니메이션은 전용 클립이 없어 `Fox_Falling`을 임시로 사용 — 4단계(체력·피격 표현)에서 더 적절한 클립으로 교체할지 검토할 것
- 벽 배치 관련 테스트값(사용자 확인): 재화 시작 총량 10, 벽 1개당 비용 1, 벽 비주얼은 단순 큐브 placeholder
- 벽 슬롯 전용 레이어명: `WallSlot` (10번 슬롯) — 클릭 레이캐스트 전용, `WallVision`(9번, 실제 벽)과는 별개
- 벽 슬롯 위치: 타일 중심에서 진행 방향 기준 좌/우로 1유닛 이격, Y=0.75(벽 프리팹 중심 높이와 동일). 벽 크기: (0.2, 1.5, 2) — 얇고 타일 길이(2유닛)만큼 긴 패널 형태
- 벽 배치 클릭 입력은 `WallPlacementManager`가 `Mouse.current` + `Physics.Raycast(slotLayerMask)`로 중앙 처리 (개별 `WallSlot`에 `OnMouseDown` 사용 안 함 — 프로젝트가 New Input System 전용 설정이라 legacy 마우스 이벤트가 동작하지 않음)
- Floor 크기: Plane scale (3,1,6) → 30 x 60 유닛
- 1단계 테스트 경로(사용자 확인): 타일 크기 2x2 유닛, 20칸, ㄴ자(L자, 꺾이는 구간 1곳) 형태. 실제 스테이지 레이아웃이 아니라 인프라 검증용 임시 경로이며 6/8단계에서 별도로 조립 예정
- 타일 마커는 씬에 `Path` 하위의 빈 오브젝트(`Tile_00`~`Tile_19`)로 배치, `PathManager`가 Awake 시 자식 순서대로 읽어 `TileData[]` 구성. 좌표: 세그먼트 A는 X=-8 고정, Z=-10→8(2씩 증가, 10칸) / 세그먼트 B는 Z=8 고정, X=-6→12(2씩 증가, 10칸)
- `PlayerAutoMover`는 design.md 원문("도착 지점 자동 설정")을 "타일을 순서대로 다음 목적지로 웨이포인트 삼아 순차 이동"으로 해석해 구현 — 열린 평지에서 NavMeshAgent가 최종 목적지로 직선 이동하면 타일 인덱스·꺾이는 경로 개념이 무의미해지므로, design.md 3.1 "Player는 타일을 순서대로 자동 통과한다"에 맞춰 이렇게 결정함
- Player의 현재 타일 인덱스는 "마지막으로 도달한 타일(목표 타일 - 1)"로 정의 — 2단계 `CanPlaceAt` 배치 제약 검사의 기준이 됨

---

## 이슈 / 막힌 부분

> MCP로 처리 불가능했던 작업, 미해결 버그, 확인이 필요한 사항 등.

- [x] `Monster.prefab`/`Chicken.prefab`의 Animator Controller가 서로 뒤바뀌어 있던 버그 → 0단계에서 수정 완료
- [x] `Anicon_Player` 컨트롤러가 비어있던 문제 → 0단계에서 State/Parameter 구성으로 해결
- [x] `Projectile_Egg.prefab`에 정체불명의 레이어(9번, 이름없음)가 걸려있던 것 → Default로 정리

- [x] Play 모드 테스트 중, Unity 에디터 창이 포커스를 잃은 상태(`is_focused:false`)에서는 Play 모드 진입이 "playmode_transition"에 멈춘 채 시뮬레이션이 진행되지 않음 → 에디터 창이 포커스를 얻어야 정상 진행됨. **이번 세션에서 여러 번 반복 확인됨(사용자가 에디터에서 병행 작업 중이면 특히 자주 발생). 다음 세션에서 MCP로 Play 모드 검증할 때는 먼저 에디터 창 포커스 상태를 확인할 것**
- [x] `Fox.prefab`↔`Anicon_Fox` 조합 Walk 애니메이션 재생 확인(사용자 확인, 2026-09-09). `Chicken.prefab`↔`Anicon_Chicken` 조합은 아직 육안 확인 안 됨 — Monster 로직(3단계) 붙일 때 같이 확인할 것
- [x] Fox 머티리얼이 URP 미변환으로 분홍색 깨짐 → `Universal Render Pipeline/Lit`로 변환 완료
- [x] 프리팹 구조(`Model` 자식 + 루트 컴포넌트)가 소스 FBX 리임포트 시 반복적으로 초기화되는 현상 — **원인 확인됨: 사용자의 수동 FBX 리임포트가 원인.** Unity나 MCP 쪽 버그가 아님(P1 정리, 2026-09-11). **캐릭터 에셋을 다시 임포트한 뒤에는 항상 프리팹 구조부터 재확인할 것** — 이 대비를 위해 P1에서 프리팹 6종의 현재 값을 전부 덤프해 복구 기준으로 남겨둠(아래 참조)

---

## 검증 게이트 통과 현황

> `docs/design.md`에 표시된 검증 게이트. 통과 전에는 다음 단계로 넘어가지 않는다.

- [x] **0단계** — 모델이 정상 표시되고 애니메이션 재생, NavMesh 이동 가능 (2026-09-07 Play 모드 실측 당시 기준: Player=`Monster` 모델, Monster=`Chicken` 모델). Player가 `Fox` 모델로 정상 표시되고 애니메이션이 재생되는 것은 이후 실제 마우스 플레이로 확인 완료됨(P1 정리, 2026-09-11 — "재검증 필요" 표시 제거)
- [x] **3단계** — 벽을 세우면 몬스터 공격을 안 맞는다 (★ 이 게임의 핵심 메커니즘) — 2026-09-09 리플렉션 직접 호출로 검증 완료. **현재 방식 기준으로 다시 적으면: 벽 없음 → 투사체 발사 + 명중 / 벽 있음 → 몬스터는 여전히 매 쿨다운 발사하지만 투사체가 벽에 맞아 피해 없이 소멸**(design.md 2장 "차단 방식" 확정 사항 — "발사 자체가 안 됨"이라는 옛 표현은 부정확한 기록이었음, P1 정리 2026-09-11). 위 "3단계" 항목 참조
