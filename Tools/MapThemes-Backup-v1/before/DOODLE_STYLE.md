# SLIME POP — Doodle Lab

낙서 실험실 시안에 맞춘 에셋과 화면 스타일입니다. `Assets/4. SlimeAsset`의 기존 PNG 51개를 낙서풍 이미지로 직접 교체했습니다. 비어 있는 투명 이미지 2개와 모든 메타 파일은 유지했습니다.

## 구성

- `Assets/Resources/DoodleLab/Notebook.png`: 여백이 넓은 실험 노트 배경.
- `Assets/Resources/DoodleLab/Sprites.png`: 4×4 시트. 몸체, 얼굴, 왕관, 악마, 천사, 돋보기, 섞기, 지우개, 일시정지, 설정, 닫기, 홈, 마스코트, 별, 모래시계, 도움말.
- `Assets/Resources/DoodleLab/Frames.png`: 2×4 시트. 기본, 노랑, 민트, 코랄, 파랑, 게임판, 테두리, 테이프 라벨. 가장자리는 9-slice로 유지됩니다.
- `Assets/Resources/DoodleLab/Logo.png`: 투명 로고.
- `Assets/3. Scripts/DoodleLabSkin.cs`: runtime screen layout, notebook styling and default light leaf-green palette across the three main scenes.

Digits remain live TMP text. The approved rounded A uses a broad, tip-free body, a separate low face and 0.48 bold digits. Custom colors replace the default green palette; dark bodies use white digits and bright bodies use black. Eight map themes retain their notebook tints and customization bindings.

시간 막대는 남은 시간에 따라 줄어듭니다. 타이머와 점수는 기존 갱신 이벤트를 유지한 채 메인 Canvas의 새 프레임으로 배치했습니다. 볼륨·테마·얼굴·코스튬 버튼은 기존 이벤트 연결을 유지합니다.

## 확인

`Assets/1. Scenes/1. GameStart.unity`부터 재생합니다. 메뉴·옵션·게임·게임오버에서 동일한 에셋을 사용합니다. 게임 씬의 시작 카운트다운 이후 17×10 슬라임과 스킬 버튼이 표시됩니다. 화면 배치는 재생 시 적용되며, 배경/프리팹/테마의 에셋 참조는 씬과 프리팹에도 저장되어 있습니다.

## 생성 방식과 프롬프트 세트 요약

OpenAI 내장 image_gen 도구를 사용했습니다. 승인받은 게임 화면 시안은 스타일 참조로만 사용했고, 실제 상호작용할 UI나 숫자를 배경에 그리지 않았습니다. 투명 자산은 실제 알파 채널을 사용합니다.

1. Notebook: "Production game background texture. STYLE REFERENCE ONLY: approved hand-drawn SLIME POP notebook-laboratory game. Clean 16:9 landscape background; warm ivory graph paper, subtle blue grid and pencil grain; central 84 percent empty for real gameplay/UI; charcoal laboratory flasks, mint liquid, research notebooks, leaves and masking tape only at far margins. Flat 2D, no text, numbers, logo, controls or slime grid."
2. Sprites: "Production transparent RGBA sprite atlas, exactly 4 columns by 4 rows, one isolated sprite per cell, no labels/grid/background. Charcoal ink and dry colored pencil. Row 1: white face-free slime body; face-only overlay; crown-only overlay; devil horns/tail-only overlay. Row 2: halo/wings-only overlay; magnifier; circular mix arrows; coral eraser. Row 3: pause; gear; close X; home. Row 4: mint mascot with face; yellow star; hourglass; question mark. Consistent scale, safe gutters, no baked numbers."
3. Frames: "Production transparent UI frame atlas, exactly 2 columns by 4 rows, eight identical landscape cells. Empty hand-drawn rounded rectangle sticker frames, charcoal double-sketch outlines, dry pencil grain, usable empty centers. Reading order: ivory, butter yellow, mint, coral, sky blue, cream graph-paper panel, cream outlined button, torn ivory paper with yellow tape. No text/icons/characters; true transparent gutters; usable as nine-slice sprites."
4. Logo: "Production title logo, exact text SLIME POP!!, playful hand-lettered charcoal pencil outlines, mint/turquoise SLIME, coral POP, yellow exclamation marks, dry colored pencil hatching, imperfect baseline, tiny mint slime by the final punctuation. Compact horizontal lockup, true transparent RGBA, no background, taped rectangle or extra text."

기존 게임 규칙, 드래그 판정, 점수 계산, 스킬 횟수, 사운드는 이 스타일 변경의 대상이 아닙니다.

## 검증과 백업

Unity 6000.3.9f1의 별도 프로젝트 복사본에서 Play Mode로 실행했습니다. 1920×1080 캡처를 확인했고, 신규 텍스처 4개와 시트 스프라이트 24개의 로딩, 보드 170개 생성, 숫자 범위, 일시정지/재개, 섞기, 재시작, 사용자 색상, 테마 변경, 게임오버 전환, 볼륨 값 연결을 확인했습니다. 게임 코드 전체의 별도 C# 컴파일도 오류 없이 통과했습니다. 마우스로 빠르게 드래그하는 수동 플레이 검증은 별도입니다.

- 실제 화면과 검증 결과: `Tools/DoodleLabPreview/`
- 교체 전 파일과 변경 목록: `Tools/DoodleLabBackup-v1/`

기존 PNG 51개는 현재 낙서풍 이미지로 직접 교체했습니다. PNG 원본은 `Tools/SlimeAssetBeforeDoodle-v1`에 별도로 백업했습니다. 기존 씬·코드의 백업도 유지됩니다. 원복 시 백업의 `before` 아래 파일을 같은 상대 경로에 복원할 수 있습니다. 새 낙서 리소스와 스크립트의 목록은 백업의 `manifest.json`에 있습니다.

직접 교체 파일 목록과 전체 미리보기: `Tools/SlimeAssetReplacement/manifest.json`, `Tools/SlimeAssetReplacement/ReplacementSheet.png`. PNG 크기와 기존 GUID, 스프라이트 분할 영역은 유지했습니다.

## Rounded A update

The large, plump A body approved in the follow-up is applied to `Assets/4. SlimeAsset/Slime/` and the live `Resources/DoodleLab/Sprites.png` atlas. Body, face and the three costume PNGs were updated with all existing dimensions and metadata preserved. Other icons and empty face/costume selections remain intact.

Final game captures and validation: `Tools/RoundedSlimeA/`. Full built-in image_gen prompts: `Tools/RoundedSlimeA/SourcePrompts.md`. Previous versions of this update's files: `Tools/RoundedSlimeA-Backup-v1/before/` (same relative paths).