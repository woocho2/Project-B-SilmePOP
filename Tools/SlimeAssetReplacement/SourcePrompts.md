# SlimeAsset 직접 교체

기존 `Assets/4. SlimeAsset`의 PNG 51개를 승인된 낙서풍 리소스로 교체했습니다. `Face_None.png`, `Costume_None.png`의 비어 있는 투명 이미지는 유지했습니다. 파일명, PNG 가로·세로 크기, 모든 `.meta` 바이트와 GUID/스프라이트 ID/분할 영역은 유지했습니다. 기존 시트의 활성 스프라이트 영역별로 승인된 그림을 내보냈습니다.

`manifest.json`에 각 파일의 교체 전·후 SHA-256, 원본 메타 해시, 크기, 대응하는 그림이 있습니다. `ReplacementSheet.png`는 전체 교체 이미지 목록입니다. 원본은 `Tools/SlimeAssetBeforeDoodle-v1`에 있습니다.

슬라임·표정·코스튬·마스코트·돋보기·섞기·지우개·설정·닫기는 기존 `Assets/Resources/DoodleLab/Sprites.png`, 패널·슬라이더는 `Frames.png`, 배경은 `Notebook.png`, 타이틀은 `Logo.png`에서 각각 내보냈습니다. 테마별 색조는 이전 스타일과 동일합니다. 폭탄 시트의 작은 조각 스프라이트는 낙서 별 모양을 사용합니다.

가운데가 빈 테두리와 시작 문구에는 내장 image_gen 도구를 사용했습니다. 생성 지시문은 다음과 같습니다.

## Hollow frame

Use case: precise-object-edit. Asset type: standalone reusable game UI border PNG, not an atlas. Use the input hand-drawn UI sheet as the art source and preserve its charcoal pencil line style. Produce one wide 16:9 rectangular notebook BORDER ONLY: softly irregular charcoal double-outline rounded rectangle with tiny butter-yellow masking tape at upper corners. The entire middle opening must be genuinely transparent alpha, not cream-filled, white, grey, checkerboard or black. Also transparent outside the outline. Only a narrow 3 percent strip around the perimeter contains drawn ink and tape, leaving at least 90 percent of the image area as empty transparent opening for actual game controls. Flat front view, safe outside margin 3 percent, no text, no shadows, no objects, no other frames. Match the approved game's doodle notebook style exactly. This replaces the old hollow ornate frame asset.

## Game Start

Use case: text-localization. Asset type: standalone game start lettering sprite. Change the lettering of the supplied doodle logo to exactly 'GAME START' in one horizontal line. Keep the same hand-drawn chunky charcoal pencil outline and dry colored-pencil hatching, handmade uneven baseline, with pale mint and butter-yellow letter fills. All letters easy to read. Transparent background, no panel, no button frame, no mascot, no extra words, no decorative objects, no 3D shadows. Compact horizontal wordmark with a comfortable transparent margin. This is the GAME START lettering asset belonging to the same SLIME POP doodle game art set.
