# SlimePOP (Project B) — Claude 작업 메모

다른 PC(집·회사)에서도 같은 맥락으로 이어서 작업하려고 남기는 파일. 사용자와는 한국어로 대화함.
**2026-11-06 Version 1.0.0으로 완성** (발표용 빌드). 아래 "다음 할 일"은 이후에 더 손볼 때의 후보.
기획은 `기획.txt`, 구현 계획은 `구현 계획서.txt`.

## 작업 규칙
- **UI는 Codex 담당.** UI 스크립트·씬은 요청이 없으면 만들지 않음. 규칙·네트워크·AI 같은 UI 아닌 쪽을 만들고, UI가 부를 이벤트·메서드를 열어 둠.
  Codex가 파일을 바꿀 수 있으니 고치기 전에 다시 읽고, 내가 안 한 변경은 되돌리지 않음.
- **연출은 모두 DOTween** (코루틴 X). 순수 규칙 코드(`00. Core`, `02. Board`, `07. AI` 대부분)에는 넣지 않음.
- 사용자가 `AiDifficultyTable.cs` 숫자를 직접 자주 고침. 고치기 전에 지금 값을 다시 읽을 것.
- 회사 PC에서는 PowerShell PATH에 git이 없음 → `C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\IDE\CommonExtensions\Microsoft\TeamFoundation\Team Explorer\Git\cmd\git.exe`

## 스크립트 폴더 (`Assets/3. Scripts`)
| 폴더 | 내용 |
|---|---|
| 00. Core | 열거형, 규칙 상수(`GameRules`), 난수, `Singleton`, `SceneLoader` |
| 01. Data | `MatchSettings` (시드 → 목표 합·아이템) |
| 02. Board | 판 모델·생성·조합 찾기·점수 (순수 C#) |
| 03. Match | `GameManager`, 점수판 |
| 04. Slime / 05. Input | 판 화면, 드래그 입력 |
| 06. Skill | 아이템 규칙(`SkillBase`, `SkillSet`)과 `SkillManager` |
| 07. AI | AI (아래) |
| 08. Network | Firebase / 로컬 네트워크 |
| 09. UI | `0. Common`(여러 씬 공용) · `1. Login` · `2. Lobby` · `3. Room` · `4. Game` — 씬별 `UIManager_씬` + `씬_패널` 이름 규칙 |
| 10. Sound | `SoundManager`, `SoundData`, 볼륨 슬라이더 |
| 11. Style | 내 슬라임 색·맵 테마: `StyleSettings`(고른 값), `MySlimeColor`(내 색 칠함), `ThemeSkin`(테마 색·그림), `ThemePalette`, `DoodleLabSkin` |
| 12. Dev | `DebugCheat` (에디터·개발 빌드용 키: 타이틀 T, 게임 F1) |

2026-11-05에 정리하면서 `ChangeData` → `StyleSettings`, `Change_Slime` → `MySlimeColor`, `Change_Theme` → `ThemeSkin`으로 이름을 바꿈 (.meta GUID 유지라 씬 연결은 그대로).

## 게임 규칙 (지금)
- 판 17×10, 숫자 1~9. 직사각형 합이 목표 합(10~15, 판마다 시드로)이면 터짐. 판 시간 **180초**, 아이템은 **판마다 1개 랜덤** (`GameRules.SkillsPerMatch = 1`, 단축키 A).
- 점수 = 기본 + 천사(2B) − 악마(B) + 킹(10) + 조커(목표×2). 난이도 비교는 점수 말고 **슬라임 제거 수**로 함 (사용자 결정).
- 돋보기·조커는 누르는 순간 바로 씀 (11-06 변경). 둘 다 경기 시드 난수(`SkillSet`의 스킬별 난수)라 모든 참가자가 같은 순서로 쓰면 같은 결과.
  - 돋보기: 최고의 수가 아니라 **악마 없는 조합 중 무작위** 하나 (`MagnifierSkill.FindHint`). 일찍 써서 킹 조합 찾는 이득을 없앰.
  - 조커: 자리를 못 고름. 시드로 칸 하나를 뽑고 그 칸에서 가장 가까운 기본 슬라임이 조커 (`JokerSkill`). 원하는 곳(천사 옆)에 넣고 바로 터뜨리는 플레이 방지. 조커가 된 칸은 1.2초 외곽선.
  - 조커가 5초마다 움직이는 곳은 아직 시드가 아님 (`BoardModel.jokerRandom`).

## AI (`Assets/3. Scripts/07. AI`)
- `AiStrategy.Best`: 500판 정도 시뮬레이션으로 찾은 최적 전략. 핵심은 CountWeight −3 (큰 숫자·적은 마리 먼저). 아이템은 막혔을 때만이 아니라 이득이 기준값을 넘으면 씀.
- `AiBrain` 수 고르기 / `AiSkillAdvisor` 아이템 판단 / `AiAgent` 한 명의 행동과 대기 시간 (순수 C#) / `AiPlayer` Unity 래퍼 / `AiSimulator` 헤드리스 판.
- `AiDifficultyTable`: 정확도 쉬움 0 · 보통 1/3 · 어려움 2/3 · 지옥 1. 반응 시간 최소~최대(최대 = 최소×2). 목표 합별 배율 `ReactionScale` (11: 1.2 · 12: 1.35 · 13: 1.5 · 14: 1.65 · 15: 1.65, 지옥 제외, 11-06 사용자가 정함). 이 값으로 사용자가 어려움을 이기는 비율은 목표 합마다 61~68%.
- **지옥은 사람이 못 이기게 설계** (사용자 확인). 다만 지금 2.4~4.8초라 목표 10에서는 사용자가 28% 정도 이김 → 1~2초로 되돌릴지 검토 필요.
- 목표를 정한 근거: 쉬움 ≈ 사용자 평균 제거의 60%, 보통 ≈ 80%, 어려움 ≈ 95% (어려움은 "진땀승"). 이 비율은 가정이니 바꿀 수 있음.

## 발표용 자동 플레이 (`AiAutoPlay`, 11-06)
- 대기실에서 **T** → 내 자리도 AI가 둠 (켜져 있는 동안 1 쉬움 · 2 보통 · 3 어려움 · 4 지옥, 다시 T = 끔). 키는 `05. Input/AutoPlayInput` — 발표를 빌드로 해서 **출시 빌드에도 들어감**. 바꿀 때만 왼쪽 위에 3초 안내.
- 게임에서 `AiPlayer.CreateForMyBoard`가 `AiAgent.PlanOnly`로 수만 고르고, `DragManager.PlayAiDrag`가 드래그 상자를 DOTween으로 늘린 뒤 놓은 것처럼 판정. 아이템은 `SkillManager.AiUse`가 범위를 0.6초 보여 준 뒤 적용. 그동안 내 마우스·아이템 단축키는 막힘.
- 끄는 시간은 반응 시간 안에 들어가서 AI 속도(난이도)는 시뮬레이션과 같음.

## 지금까지 사용자 기록 (예전 기록 모드, 각 10판. 기록 모드 코드는 11-06에 지움, 데이터는 `TestRecords/`에 남김)
| 목표 | 날짜 | 평균 제거 | 범위 | 평균 간격 | 판당 실수 | 메모 |
|---|---|---|---|---|---|---|
| 10 | 11-05 | 107.9 | 87~134 | 3.3초 | 1.7 | 후반 60초엔 13마리뿐 → 판이 먼저 바닥남 (사람 한계가 아니라 판 한계) |
| 11 | 11-05 | 87.8 | 60~104 | 4.8초 | 2.4 | 시간대별로 고르게 지움 → 시간이 모자람 |
| 12 | 11-06 | 84.5 | 54~96 | 5.3초 | 2.6 | 2마리 조합 55% |
| 13 | 11-06 | 79.4 | 50~100 | 5.9초 | 5.3 | 실수가 2배, 아이템 안 쓴 판 5판 |
| 14 | 11-06 | 75.3 | 59~90 | 6.1초 | 7.0 | 후반 60초 20마리로 떨어짐 |
| 15 | 11-06 | 81.2 | 59~113 | 6.5초 | 7.8 | 2마리 조합 27%(6+9, 7+8뿐) → 한 번에 3.1마리씩 지워서 제거 수는 14보다 많음. 뒤 5판이 앞 5판보다 17마리 적음 |

fit 결과(쉬움·보통·어려움 모두 비슷): 목표 10 대비 필요한 반응 시간 배율 **11: 1.22 · 12: 1.37 · 13: 1.55 · 14: 1.67 · 15: 1.69**.
14 → 15는 거의 안 늘어남 (직선식 예상 1.83보다 낮음). 15는 판마다 편차가 커서(표준편차 15.9) 덜 확실함.
기준(목표 10) 반응 시간은 지금 표 값(쉬움 4.8 · 보통 3.6 · 어려움 2.8)이 fit과 거의 같음.
예전처럼 11~15를 1.2배 하나로 묶었을 때는 사용자가 어려움을 이기는 비율이 12: 34% · 13: 13% · 14: 2% · 15: 3%였음 → 11-06에 목표 합별 배율로 바꿈.

## 다음 할 일
1. 목표 합별 배율 적용 완료 (11-06). 사용자가 직접 해 보고 체감 확인 → 필요하면 배율 조정.
2. 지옥 반응 시간 (못 이기게) 다시 보기.
3. 보류 중: "번지는 악마" 규칙 제안(15초마다 옆 기본 슬라임으로 번짐, 상한 20, 파괴·합체로는 없앨 수 있음) — 사용자 확정 전. 호스트 이전.

## 판 연출 (11-06, 모두 DOTween)
- 터짐: `SlimeController.PlayPop` (부풀었다 쏙, 슬라임마다 0.035초 시간차). `SlimeManager.OnSlimesRemoved`가 부름. 합체(0 아님)는 연출 없음.
- +점수: `ScorePopup` (월드 TextMeshPro, 풀). `SlimeManager.ShowScore`를 드래그(`DragManager`)·파괴·합체 0(`SkillManager`)에서 부름. 15점 이상 금색.
- 합이 틀림: 드래그 상자가 빨간 강조 그대로 좌우로 흔들린 뒤 사라짐 (`DragManager.HideBox`).
- 마지막 10초 시간 글자 빨강 + 톡, 5초부터 째깍 소리 / 시간 끝 TIME UP! 1.2초 뒤 결과 화면 (`Game_GamePanel.PlayTimeUp`). 그동안 판은 보임 (`GameManager.HideBoard`는 결과 화면이 뜰 때).

## 결과 화면 (11-06)
- 시간 끝 → `GameManager.GameOver`가 각자 자기 최종 판을, 방장은 AI 판까지 `UploadFinalBoard`로 올리고 방장은 `EndMatch`(방 = Result).
- `UIManager_Game.ShowGameOver`: 다른 패널 끄고 `GameEndPanel`(`Game_EndPanel`, 씬에 안 붙어 있으면 실행 중에 붙임)만.
  `Rank.N Name / Score / Board`를 채우고, 판은 `FinalBoardView`가 검은 칸 안에 동그라미+숫자로 그림. 내 글자는 파란 그라데이션.
- 룸으로 가기 버튼 또는 20초 → 룸 씬. 먼저 돌아가는 사람이 방이 Result일 때만 `ReturnToRoom`(대기 상태로). 받은 최종 판은 `NetworkManager.FinalBoards`에 남겨 둠.

## Unity 없이 돌리는 도구 (.NET 10 SDK 필요)
- 테스트: `dotnet test Tools/AiTest` (Assets/Editor/Tests의 순수 C# 테스트, 지금 62개 통과)
- 시뮬레이션: `dotnet run -c Release --project Tools/AiSim -- <모드>`
  - `target [판 수]`: 난이도 × 목표 합 10~15 평균 제거
  - `games TestRecords/player_games.csv`: 사용자가 둔 판을 같은 시드로 AI가 다시 → 난이도별 AI 제거, 사용자가 이긴 비율
  - `fit TestRecords/player_games.csv`: 사용자 평균의 60/80/95/100%를 지우려면 반응 시간이 몇 초여야 하는지 (목표 합별)
  - `look [판 수]`: 한 수 앞 보기(Lookahead) 켜고 끈 비교. 11-06 결과: 지금 속도에선 ±1마리(차이 없음), 0.5~1초 지옥에서만 +2~5마리
  - `bench`, `seed <시드>`, `devil`, `repeat`, `timing`, `records <results.csv>` 등은 예전 실험용
- 새 순수 C# 파일을 만들면 두 csproj의 Compile 목록에도 넣을 것.
- Unity가 csproj를 아직 안 만들었으면 IDE에 CS0246이 뜨는데 실제 오류 아님.
