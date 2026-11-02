# 타이틀 글자 아틀라스 연결

## 준비된 에셋

- `Assets/4. SlimeAsset/SlimePop_TitleLetters.png`
- 투명 PNG 한 장, 4열 × 2행. `Sprite Mode: Multiple`과 글자별 분할 메타가 포함되어 있다.
- 위: `Title_S`, `Title_L`, `Title_I`, `Title_M`
- 아래: `Title_E`, `Title_P`, `Title_O`, `Title_Exclamation`
- `I`는 점이 없는 대문자이며, 위아래 가로획이 있다.
- 기존 통짜 로고와 씬 배치는 유지했다. 새 글자의 위치·크기·레이어는 사용자가 편집한다.

## Unity에서 배치

1. Project 창에서 PNG 왼쪽 화살표를 펼치면 8개 Sprite가 나온다.
2. 타이틀 Canvas 안에 UI Image를 11개 만들고 다음 순서로 Sprite를 지정한다.
   `S, L, I, M, E, P, O, P, !, !, !`
3. P는 같은 Sprite를 쓰는 **서로 다른 Image 오브젝트 2개**, 느낌표도 **서로 다른 Image 오브젝트 3개**를 만든다.
4. 각 Image의 `Preserve Aspect`를 켜고 `Raycast Target`을 끈다. `Set Native Size` 후 전체 크기를 조절해도 된다. E와 첫 P 사이의 위치를 벌려 단어 간격을 만든다.
5. 원하는 위치·크기·회전·Hierarchy 순서로 정리한다. 글자들은 같은 부모 아래의 형제 오브젝트로 두는 것이 편하다. Layout Group과 Animator가 동시에 글자 Transform을 제어하지 않도록 한다.

## 연출 연결

타이틀의 `UIManager_Start` → **Title Letter Slam**에서:

- `Title Letters`: 크기 11, 위의 Image 오브젝트를 재생 순서대로 연결한다. Sprite가 아니라 각 오브젝트의 RectTransform을 넣는다.
- `Letter Interval`: 기본 0.12초. SLIME 내부와 POP 내부의 각 글자가 출발하는 간격. 느낌표 3개는 동시에 출발한다.
- `Letter Duration`: 기본 0.24초. 접근 → 눌림 → 반동 → 정착 시간.
- `Letter Start Scale`: 기본 3. 원래 크기의 3배에서 접근한다.
- `Letter Start Height`: 기본 120. 배치한 위치보다 위에서 접근한다.
- `Slime To Pop Delay`: 기본 0.3초. E가 완전히 착지한 뒤 첫 P가 출발하기까지의 대기 시간.
- `Pop To Exclamation Delay`: 기본 0.35초. POP의 마지막 P가 완전히 착지한 뒤 느낌표 3개가 동시에 출발하기까지의 대기 시간.
- `Title Hold Duration`: 기본 0.4초. 마지막 글자가 정착한 뒤 시작 버튼 연출까지 대기한다.

단어 사이의 공간은 Image 위치로, 단어 사이의 대기 시간은 두 Delay 값으로 각각 조절한다. 시간 간격을 위해 배열에 빈 항목을 추가할 필요는 없으며, 빈 항목은 건너뛴다.

각 글자는 편집한 위치·크기·회전을 도착점으로 사용한다. 부모 관계와 형제 순서는 변경하지 않는다. 실행 중 타임스케일이 0이어도 재생된다.

에디터 Play Mode에서 Game 뷰를 클릭한 뒤 **T**를 누르면 인트로를 처음부터 다시 볼 수 있다. 재생 도중 반복해서 눌러도 기존 코루틴을 멈추고 원래 글자 배치를 복원한 다음 재시작한다. 이 단축키는 기존 `DebugCheat`의 다른 테스트 키와 함께 에디터·Development Build에서만 동작한다. 컴포넌트 컨텍스트 메뉴의 `Replay Title Intro`로도 재생할 수 있다.

기존 `Img Logo`에 연결된 통짜 로고 Image는 글자가 하나 이상 연결되면 자동으로 숨긴다. 부모 GameObject는 끄지 않아 그 아래 글자들도 유지된다. 글자 배열이 비어 있으면 기존 통짜 로고 연출이 재생된다. 시작 버튼이 연결되어 있으면 모든 글자와 버튼 연출이 끝난 후 클릭을 허용한다.

## 검증

- 실제 프로젝트 소스를 Unity 6000.3.9f1 참조로 C# 컴파일한다.
- `ValidateTitleLetters.cs`는 분리된 임시 Unity 프로젝트에서 아틀라스 임포트와 Play Mode 연출을 확인하기 위한 도구다. 실제 게임의 `Assets`에는 넣지 않는다.
- 결과는 `Validation.txt`에 저장한다. 원래 타이틀 씬에는 새 글자를 자동 배치하거나 연결하지 않는다.
- 이번 검증은 Unity 6000.3.9f1의 분리된 headless Play Mode에서 122개 항목을 통과했다. SLIME 이후와 POP 이후의 개별 대기 시간, 느낌표 세 개의 동일한 시작 시각·진행도, 완료 후 재생, 재생 도중 연속 재시작, 원래 배치 복원, 버튼 클릭 잠금을 확인했다. 새 임시 프로젝트의 Unity 검색 인덱스(`UnityEditor.Search.SearchDatabase`) 오류는 검증 대상에서 제외했으며, 게임 스크립트의 오류는 실패로 처리한다. 최종 화면 배치와 사운드는 사용자가 글자를 연결한 뒤 확인한다.
- 이미지 생성 방식과 프롬프트는 `Prompts.md`에 기록했다.
