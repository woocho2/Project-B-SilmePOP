# 스케치북 물감 팔레트

## 연결

1. Assets/2. Prefab/PastelColorPicker.prefab을 OptionPanel의 마지막 자식으로 배치합니다. 프리팹 루트는 활성 상태로 둡니다. 안쪽 Popup만 스크립트가 열고 닫습니다.
2. UIManager_Menu의 OptionPanel 항목에서 Btn Slime Color에 OptionPanel/Color/Slime의 Button을 연결합니다.
3. 같은 항목의 Slime Color Picker에 배치한 PastelColorPicker 컴포넌트를 연결합니다.

Slime 오브젝트에 Button이 없다면 직접 추가하고 Target Graphic은 해당 슬라임 Base의 Image로 지정합니다. 예전 FlexibleColorPicker를 여는 On Click 연결이 있다면 그 연결만 제거합니다. 새 연결은 UIManager_Menu가 등록하므로 On Click에 Open을 중복 추가할 필요는 없습니다.

씬과 기존 OptionPanel/Slime 프리팹은 수정하지 않았습니다. 기존 배치와 자동 UI 생성 코드를 변경하거나 추가하지 않았으며, 새 팔레트는 편집 가능한 정적 프리팹입니다.

## 동작

- 슬라임 버튼: 물감 팝업을 DOTween으로 엽니다.
- 10개의 물감: 미리보기와 선택 체크만 변경합니다. 실제 ChangeData는 아직 바꾸지 않습니다.
- 적용 버튼: ChangeData.UseDoodlePalette = false, SelectedColor = 미리보기 색으로 확정한 뒤 팝업을 닫습니다. 기존 Change_Slime 및 ColorChanged 구독자가 이때 갱신됩니다.
- 선택한 물감에 빨간 체크를 표시하고 슬라임 본체 미리보기와 색상 이름을 갱신합니다.
- 여러 색을 비교할 수 있도록 색을 눌러도 팝업은 유지됩니다.
- X, 팝업 바깥, Esc: 미리보기 선택을 버리고 닫습니다. 옵션 패널이 비활성화돼도 미적용 색은 저장하지 않습니다. 다음에 열 때 실제 적용된 색을 다시 표시합니다.
- 콜백과 ColorChanged는 OnEnable에서 구독하고 OnDisable에서 해제합니다.
- RGB/HSV 계산이나 동적 컬러 셰이더 없이 물감 스프라이트 한 장을 열 가지 색으로 틴트합니다. FlexibleColorPicker에 의존하지 않습니다.
- 흰색은 그대로 유지하고, 검정은 부드러운 차콜, 남색은 채도를 낮춘 남색으로 구성했습니다.

| 색상 | HEX |
| --- | --- |
| 하얀색 | #FFFFFF |
| 검정색 | #3C3C48 |
| 빨간색 | #F28D8D |
| 파란색 | #93BDF1 |
| 분홍색 | #F3B6D0 |
| 노란색 | #F6DE91 |
| 초록색 | #8CC5A1 |
| 연두색 | #C9DF94 |
| 갈색 | #B5917D |
| 남색 | #697A9D |

색은 프리팹의 Swatches 배열에서 수정할 수 있습니다. 프리팹에 적힌 이름과 색상 라벨도 함께 바꿉니다.

## 파일

- Assets/3. Scripts/New/09. UI/PastelColorPicker.cs: 팔레트 표시·선택·DOTween·이벤트 관리.
- Assets/3. Scripts/UIManager_Menu.cs: 슬라임 색상 버튼과 OpenSlimeColorPicker 연결 필드.
- Assets/2. Prefab/PastelColorPicker.prefab: 종이 팝업, 미리보기, 물감 버튼 5열 x 2행, 이름, 체크, 닫기 버튼.
- Assets/4. SlimeAsset/UI_Palette_Paint_White.png: 생성 원본 그대로 저장한 중립 회색조의 틴트용 물감 스프라이트.
- Tools/PastelColorPicker/Preview.png: 분홍색을 고른 상태의 별도 구성 미리보기. Unity 실행 화면이 아니며 글꼴은 미리보기용 Malgun Gothic입니다. 프리팹은 기존 KATURI TMP 폰트를 사용합니다.
- Tools/PastelColorPicker/Prompts.md: image_gen 생성 방식·프롬프트·원본 경로.
- Tools/PastelColorPicker/Preview.ps1: 원본 에셋을 바꾸지 않고 구성 미리보기만 합성합니다.

사용자 요청에 따라 Unity 실행, 컴파일, 테스트는 수행하지 않았습니다.
