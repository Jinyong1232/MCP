# 게임 텍스트 수정 매뉴얼

> 게임에 나오는 문구를 코드를 몰라도 Inspector에서 직접 고칠 수 있도록, 문구들을 전부 필드로 빼뒀다.
> 아래 표를 보고 **Hierarchy에서 오브젝트 선택 → Inspector에서 컴포넌트 찾기 → 필드 값 수정**하면 된다.

---

## 게임 제목 (메인 메뉴)

- 씬: `MainMenu.unity`
- Hierarchy: `Canvas > TitleText` 선택
- Inspector의 `TextMeshPro - Text (UI)` 컴포넌트 → `Text` 입력창의 내용을 바꾸면 됨(현재 "여우는 죄가 없다", 임시 제목)

## 인게임 HUD 문구 (체력 / 남은 벽 / 스테이지 표시)

- 씬: `Stage1(Test).unity`, `Stage2.unity` (양쪽 다 따로 고쳐야 함)
- 체력 바(`Canvas > HealthBar`)와 남은 벽 아이콘(`Canvas > CurrencyIcons`)은 하트/방패 아이콘으로 바뀌어서 별도의 이름표 문구가 없음 — 숫자(`100`, `6`)도 게임 로직이 자동으로 채우는 값이라 수정 불가
- 우상단 스테이지 표시만 자유 문구: `Canvas > StageLabel` 선택 → `TextMeshPro - Text (UI)` 컴포넌트 → `Text` 필드 (현재 Stage1은 "Stage 1", Stage2는 "Stage 2")

## 클리어 / 실패 결과 화면 문구

- 씬: `Stage1(Test).unity`, `Stage2.unity`
- Hierarchy: `Managers` 선택 → `Stage Manager` 컴포넌트 → "표시 문구 (자유롭게 수정 가능)" 항목들
  - `Clear Message` — 스테이지 클리어 시 큰 글씨 (현재 "CLEAR")
  - `Game Over Message` — 실패 시 큰 글씨 (현재 "GAME OVER")
  - `Game Clear Message` — 마지막 스테이지 클리어 시 큰 글씨 (현재 "GAME CLEAR")
  - `Next Stage Button Label` — 클리어 후 버튼 문구 (현재 "다음 스테이지")
  - `Retry Button Label` — 실패 후 버튼 문구 (현재 "다시 도전")
  - `Menu Button Label` — 마지막 스테이지 클리어 후 버튼 문구 (현재 "메뉴로 돌아가기")

## 튜토리얼 문구

- 씬: `Stage1(Test).unity` (튜토리얼이 있는 씬만 해당, `Stage2.unity`는 현재 튜토리얼 단계가 비어 있음)
- Hierarchy: `Managers` 선택 → `Tutorial Manager` 컴포넌트 → `Steps` 배열
  - 각 `Step`을 펼치면 `Pages` 배열이 있고, 그 안의 `Text`가 화면에 뜨는 실제 문구
- 하단 안내 문구(진행 방법 안내)도 수정 가능: 같은 컴포넌트의 `Hint Slot Click Message`(슬롯 클릭을 기다릴 때, 현재 "표시된 벽 슬롯을 클릭하세요")와 `Hint Advance Message`(그냥 다음으로 넘어갈 때, 현재 "클릭하거나 아무 키나 누르면 계속됩니다")

## 메인 메뉴 버튼 문구

- 씬: `MainMenu.unity`
- `Canvas > StartButton > Label` — 시작 버튼 문구 (현재 "시작하기")
- `Canvas > QuitButton > Label` — 종료 버튼 문구 (현재 "종료")
- 각 오브젝트의 `TextMeshPro - Text (UI)` 컴포넌트 → `Text` 필드 수정

---

## 주의할 것

- 전부 **TextMeshPro** 텍스트라 폰트는 자동으로 NEXON Lv1 Gothic으로 통일되어 있음 — 새로 텍스트 오브젝트를 추가할 때도 이 폰트를 쓸 것(`Assets/Fonts/TMP/NEXONLv1Gothic-Regular SDF.asset` 또는 `-Bold SDF.asset`)
- 스테이지1·스테이지2는 **각각 별도의 씬 파일**이라, HUD/결과 화면 문구를 통일하고 싶으면 두 씬 모두에서 같은 값으로 고쳐야 함
- 문구 길이가 너무 길어지면 버튼/패널 크기를 벗어날 수 있음(현재는 자동 줄바꿈만 켜져 있고 자동 크기 조절은 없음) — 짧게 유지하거나 필요하면 알려주면 오브젝트 크기도 같이 조정 가능
