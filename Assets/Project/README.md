# NAN Fishing Demo

스마트폰의 자세 센서와 터치 입력을 이용하는 세로형 낚시 데모입니다. 플레이어는
캐스팅한 뒤 화면을 누른 채 물고기의 방향에 맞춰 휴대폰을 움직여 줄 장력을 관리합니다.

현재 실기기에서는 좌우 조작이 휴대폰을 수평면에서 왼쪽·오른쪽으로 방향 전환하는
동작처럼 느껴집니다. 향후 목표는 세로로 든 휴대폰을 좌우로 기울이는 동작으로
`Direction`을 제어하는 것입니다. 현재 센서 계산과 변경 시 주의점은
[ARCHITECTURE.md](ARCHITECTURE.md#9-향후-좌우-기울이기-입력으로-변경하기)에 정리되어 있습니다.

## 개발 환경

| 항목 | 버전/설정 |
|---|---|
| Unity Editor | `6000.3.20f1` |
| Unity Input System | `1.19.0` |
| Universal Render Pipeline | `17.3.0` |
| Unity Test Framework | `1.6.0` |
| 기본 화면 방향 | Portrait |
| 목표 프레임 속도 | 60 FPS |
| 시작 씬 | `Assets/Project/Scenes/SampleScene.unity` |

프로젝트가 사용하는 정확한 패키지 목록은 `Packages/manifest.json`, Unity 버전은
`ProjectSettings/ProjectVersion.txt`에서 확인할 수 있습니다.

## 실행 방법

1. Unity `6000.3.20f1`로 프로젝트를 엽니다.
2. `Assets/Project/Scenes/SampleScene.unity`를 엽니다.
3. Play Mode에 진입합니다.
4. 에디터에서는 `Enter`로 첫 캐스팅을 실행하면 게임 세션이 시작됩니다.

씬의 저해상도 배경, 부두, 소품, 카메라와 조명은 `Environment` 계층 아래에 저장되어
있습니다. 런타임의 `DemoBootstrap`은 입력·게임플레이 서비스를 생성하고 씬에 저장된
HUD 및 데이터 에셋을 연결합니다.

### 에디터 조작

- `Enter`: 캐스팅
- `Space` 또는 마우스 왼쪽 버튼 누르기: 릴 감기
- `Left`/`Right` 또는 `A`/`D`: 플레이어 방향 조절
- 마우스를 위로 스와이프: 터치 대체 캐스팅

### Android 조작

- 시작 후 휴대폰을 약 0.5초간 정지해 기준 자세를 보정합니다.
- 휴대폰을 뒤로 움직였다가 앞으로 휘둘러 캐스팅합니다.
- 화면을 누르고 있는 동안 릴을 감습니다.
- 휴대폰 자세로 물고기의 좌우 방향을 따라갑니다.
- `RECALIBRATE` 버튼으로 현재 자세를 중립 자세로 다시 지정합니다.

> 현재 Android 좌우 입력은 기기와 화면 좌표계에 따라 수평 방향 전환처럼 느껴질 수
> 있습니다. 화면에 표시되는 `Direction`과 실제 센서 축을 동일한 개념으로 가정하지
> 마십시오.

## 처음 코드를 읽는 순서

1. `Scripts/Core/DemoBootstrap.cs`: 씬 데이터 검증과 런타임 서비스 조립을 담당합니다.
2. `Scripts/Core/GameFlowController.cs`: 보정, 세션 시작·종료, 점수, 저장, HUD 이벤트를 연결합니다.
3. `Scripts/Input/MotionInputService.cs`: 자세 센서, 가속도계, 터치, 마우스와 키보드를 공통 입력으로 변환합니다.
4. `Scripts/Fishing/FishingController.cs`: 캐스팅, 입질, 물고기 생성, 릴링과 라운드 결과를 진행합니다.
5. `Scripts/Fishing/LineTensionModel.cs`: 플레이어와 물고기 방향 차이로 진행도와 줄 장력을 계산합니다.
6. `Scripts/UI/FishingHUD.cs`: 상태 패널과 장력·진행도·방향 시각화를 갱신합니다.

전체 의존 관계, 상태 전이, 이벤트, 수식과 유지보수 지침은
[아키텍처 문서](ARCHITECTURE.md)를 참고하십시오.

## 주요 에셋

| 경로 | 용도 |
|---|---|
| `Scenes/SampleScene.unity` | 실행 씬과 환경·HUD 오브젝트 |
| `Scenes/GameSetting.asset` | 세션, 센서, 장력과 점수 밸런스 |
| `Scenes/FishDefinitions/` | 어종별 점수, 저항, 이동과 외형 데이터 |
| `Art/Materials/` | 현재 데모용 URP 재질 |
| `InputSystem_Actions.inputactions` | EventSystem 등 공용 Input System 액션 |

게임플레이 입력은 생성된 Input Actions 클래스를 사용하지 않고
`MotionInputService`가 Input System 장치를 직접 읽습니다.

## 씬과 UI 재생성 도구

- `Tools > NAN Fishing > Build Editable Scene`
  - 기존 `Environment` 오브젝트를 제거하고 데모 환경을 다시 생성합니다.
  - `FishingRod`, `BobberAnchor`, `FishPool` 등 런타임이 이름으로 찾는 오브젝트도
    포함되므로 커스텀 환경 작업 전에 반드시 백업 또는 버전 관리 상태를 확인하십시오.
- `Tools > NAN Fishing > Build Scene UI`
  - 기존 `FishingHUD`를 제거하고 Canvas와 세 상태 패널을 다시 생성합니다.
  - 직접 수정한 UI 계층과 직렬화 참조가 교체될 수 있습니다.

두 도구 모두 현재 씬을 실제로 변경합니다. 단순히 Play Mode로 실행하기 위해 재생성할
필요는 없습니다.

## 아트 핸드오프

프로덕션 에셋은 `Art/Characters/Fish`, `Art/Environment`, `Art/Props`,
`Art/Materials`, `Art/VFX` 아래에 배치하는 것을 권장합니다.

- 절차형 물고기는 `FishDefinition.prefab`을 지정하여 교체합니다.
- 환경 소품은 씬 또는 프리팹에서 교체하고 게임플레이 코드가 모델 내부 계층에
  의존하지 않도록 유지합니다.
- 단위는 미터, 소품의 전방은 `+Z`, 피벗은 하단 중앙을 기준으로 합니다.
- 가능한 경우 하나의 공용 URP 재질 팔레트를 사용합니다.
- 공통 밸런스는 `GameBalanceConfig`, 어종별 값은 `FishDefinition`에서 관리합니다.

## 테스트

Edit Mode 테스트는 `Scripts/Editor/NanFishingTests.cs`에 있습니다. 현재 테스트는
타임어택 점수·콤보와 줄 장력 모델을 검증하지만, 모바일 센서 축·보정·캐스팅 제스처는
자동화되어 있지 않습니다. 센서 변경 시 Android 실기기 검증이 반드시 필요합니다.
