# Touch To Start

- Unity asset: `Assets/4. SlimeAsset/TouchToStart_Crayon.png`
- Exact text: `Touch To Start`.
- 2172 × 724 transparent RGBA PNG. The generated image is preserved without pixel edits.
- Black/charcoal crayon lettering only. The mint capsule, green border, yellow play icon and accent marks were removed at the user's request.
- The existing PNG was replaced while retaining its `.meta` GUID, so already assigned Sprite references stay connected. The earlier button artwork is backed up as `BeforeTextOnly.png`.
- Supplied `.meta` configures a single UI Sprite, uncompressed alpha, no mipmaps, centered pivot, bilinear filtering, clamp wrapping and a 4096 texture-size limit.

## 연결

1. Canvas 안에 UI Image 또는 Button을 만들고 `Source Image`에 `TouchToStart_Crayon`을 지정한다.
2. Image의 `Preserve Aspect`를 켜고 원하는 크기로 배치한다. 이 PNG는 글자만 있는 투명 이미지이므로 버튼 배경을 별도로 추가할 필요가 없다.
3. `UIManager_Start`의 `Rect Game Start`, `Img Game Start`, `Btn Game Start`에 해당 오브젝트의 RectTransform, Image, Button을 각각 연결한다.
4. 기존 코드가 타이틀 글자 연출 이후 시작 버튼을 등장시키고 반짝이게 한다. 클릭하면 `M Scene Name`에 지정한 씬으로 이동한다.
5. 에디터 Play Mode에서 Game 뷰를 클릭하고 T를 누르면 전체 인트로를 다시 확인할 수 있다.

씬 배치와 기존 에셋은 변경하지 않았다. 문구는 래스터 이미지에 포함되어 있으므로 별도 TMP 텍스트를 겹쳐 놓을 필요가 없다.

## 검증

- 생성 결과를 눈으로 확인해 문구, 대소문자, 글자 잘림과 스타일을 검토했다.
- PNG 크기와 RGBA 알파, 양쪽 모서리 alpha=0을 확인했다.
- 이 작업은 에셋과 임포트 메타만 추가했다. 새 Sprite를 사용자가 배치한 후 실제 크기와 버튼 연결을 Unity에서 확인한다.
- 내장 `image_gen` 도구를 사용했다. 프롬프트는 `Prompts.md`에 기록했다.
