# SlimePOP (Project B) — Claude 작업 메모

다른 PC(집·회사)에서도 같은 맥락으로 이어서 작업하려고 남기는 파일. 사용자와는 한국어로 대화함.
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
| 12. Dev | `DebugCheat`(기록 모드), `PlayerRecord` (에디터·개발 빌드용) |

2026-11-05에 정리하면서 `ChangeData` → `StyleSettings`, `Change_Slime` → `MySlimeColor`, `Change_Theme` → `ThemeSkin`으로 이름을 바꿈 (.meta GUID 유지라 씬 연결은 그대로).

## 게임 규칙 (지금)
- 판 17×10, 숫자 1~9. 직사각형 합이 목표 합(10~15, 판마다 시드로)이면 터짐. 판 시간 **180초**, 아이템은 **판마다 1개 랜덤** (`GameRules.SkillsPerMatch = 1`, 단축키 A).
- 점수 = 기본 + 천사(2B) − 악마(B) + 킹(10) + 조커(목표×2). 난이도 비교는 점수 말고 **슬라임 제거 수**로 함 (사용자 결정).
- 돋보기는 악마 없는 조합 중 점수가 가장 높은 것을 알려 줌 (`MagnifierSkill.FindHint`).

## AI (`Assets/3. Scripts/07. AI`)
- `AiStrategy.Best`: 500판 정도 시뮬레이션으로 찾은 최적 전략. 핵심은 CountWeight −3 (큰 숫자·적은 마리 먼저). 아이템은 막혔을 때만이 아니라 이득이 기준값을 넘으면 씀.
- `AiBrain` 수 고르기 / `AiSkillAdvisor` 아이템 판단 / `AiAgent` 한 명의 행동과 대기 시간 (순수 C#) / `AiPlayer` Unity 래퍼 / `AiSimulator` 헤드리스 판.
- `AiDifficultyTable`: 정확도 쉬움 0 · 보통 1/3 · 어려움 2/3 · 지옥 1. 반응 시간 최소~최대(최대 = 최소×2). 목표 합 11 이상은 `HighSumReactionScale`배 (지옥 제외).
- **지옥은 사람이 못 이기게 설계** (사용자 확인). 다만 지금 2.4~4.8초라 목표 10에서는 사용자가 28% 정도 이김 → 1~2초로 되돌릴지 검토 필요.
- 목표를 정한 근거: 쉬움 ≈ 사용자 평균 제거의 60%, 보통 ≈ 80%, 어려움 ≈ 95% (어려움은 "진땀승"). 이 비율은 가정이니 바꿀 수 있음.

## 기록 모드 (`Assets/3. Scripts/12. Dev`, 에디터·개발 빌드만)
- 게임 화면에서 **T** → 목표 합(10~15) 입력 + Enter → 그 목표 합으로 10판 연속. T 두 번 = 중단. F1 = 목표 합 바꾸기 (기록 중엔 막힘).
- 판마다 `TestRecords/player_games.csv`, 10판이 끝나면 `TestRecords/player_sessions.csv`에 요약. 분석 화면은 `PlayerSessionReport.Describe`.
- `TestRecords/results.csv`, `seeds.txt`는 예전 테스트 모드(AI와 같은 시드 비교) 기록. 그때는 아이템 3개 / 다른 반응 시간이라 지금과 직접 비교하면 안 됨.

## 지금까지 사용자 기록 (2026-11-05, 기록 모드)
| 목표 | 평균 제거 | 범위 | 평균 간격 | 메모 |
|---|---|---|---|---|
| 10 | 107.9 | 87~134 | 3.3초 | 후반 60초엔 13마리뿐 → 판이 먼저 바닥남 (사람 한계가 아니라 판 한계) |
| 11 | 87.8 | 60~104 | 4.8초 | 시간대별로 고르게 지움 → 시간이 모자람. 앞 5판 83 → 뒤 5판 93으로 늘고 있음 |

목표 11에 맞는 반응 시간은 목표 10의 약 1.22배 (fit 결과). AI는 목표 합이 클수록 더 많이 지우는데 사람은 느려지므로,
11~15를 배율 하나로 묶으면 14·15에서 어려움이 너무 셀 가능성이 큼.

## 다음 할 일
1. 사용자가 **목표 12·13·14·15를 10판씩 기록** → `games`, `fit` 모드로 분석 → **목표 합별 배율을 따로 넣을지** 사용자와 정함. 값은 사용자 확인 후에만 바꿈.
2. 지옥 반응 시간 (못 이기게) 다시 보기.
3. 보류 중: "번지는 악마" 규칙 제안(15초마다 옆 기본 슬라임으로 번짐, 상한 20, 파괴·합체로는 없앨 수 있음) — 사용자 확정 전. 호스트 이전, `UploadFinalBoard` 미호출.

## Unity 없이 돌리는 도구 (.NET 10 SDK 필요)
- 테스트: `dotnet test Tools/AiTest` (Assets/Editor/Tests의 순수 C# 테스트, 지금 60개 통과)
- 시뮬레이션: `dotnet run -c Release --project Tools/AiSim -- <모드>`
  - `target [판 수]`: 난이도 × 목표 합 10~15 평균 제거
  - `games TestRecords/player_games.csv`: 사용자가 둔 판을 같은 시드로 AI가 다시 → 난이도별 AI 제거, 사용자가 이긴 비율
  - `fit TestRecords/player_games.csv`: 사용자 평균의 60/80/95/100%를 지우려면 반응 시간이 몇 초여야 하는지 (목표 합별)
  - `bench`, `seed <시드>`, `devil`, `repeat`, `timing`, `records <results.csv>` 등은 예전 실험용
- 새 순수 C# 파일을 만들면 두 csproj의 Compile 목록에도 넣을 것.
- Unity가 csproj를 아직 안 만들었으면 IDE에 CS0246이 뜨는데 실제 오류 아님.
