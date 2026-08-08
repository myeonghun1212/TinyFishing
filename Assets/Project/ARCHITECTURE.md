# Tiny Fishing Architecture

## 런타임 구성

`Pond FPV`는 다음 컴포넌트를 직렬화 참조로 연결합니다.

- `TinyFishingGameManager`: 라운드 상태, 점수, 어종 선택과 게임 흐름을 관리합니다.
- `TinyFishingInputService`: 센서 입력과 에디터·Standalone fallback 입력을 제공합니다.
- `FishingRodController`: 입력 방향을 낚싯대 회전에 반영합니다.
- `TinyFishingHUD`: 상태, 점수, 진행도, 물고기와 플레이어 마커를 표시합니다.
- `FishSpawnVolume`: `FishPool`을 기준으로 물고기 프리팹을 생성합니다.

상태 흐름은 `ReadyToCast → WaitingForBite → Reeling → RoundResult → ReadyToCast`입니다.
`ReelProgressModel`과 `FishDriftDriver`는 MonoBehaviour가 아닌 순수 런타임 모델로 게임
매니저에서 생성됩니다.

## 코드와 데이터

- `Scripts/Core`: 게임 매니저, 상태, 스폰 볼륨
- `Scripts/Input`: 입력 인터페이스와 구현
- `Scripts/Fishing`: 릴 진행, 물고기 이동, 낚싯대와 부력 동작
- `Scripts/UI`: HUD 바인딩
- `Scripts/Data`: `TinyFishingConfig`, `FishPool`, `FishDefinition`
- `Data`: 설정 및 어종별 ScriptableObject 인스턴스

`FishDefinition`은 기존 데이터 에셋과의 호환성을 위해 `NanFishing.Data` namespace를
유지합니다. 나머지 게임 코드는 `TinyFishing` namespace 계층을 사용합니다.

## 주요 데이터 흐름

1. 입력 서비스가 캐스팅, 릴 입력과 방향 값을 제공합니다.
2. 게임 매니저가 상태를 전환하고 물고기·릴 모델을 갱신합니다.
3. HUD와 낚싯대 컨트롤러가 게임 상태 및 입력을 시각화합니다.
4. 스폰 볼륨은 `DefaultFishPool`과 어종 프리팹을 사용해 배경 물고기를 구성합니다.
5. 최고 점수는 `PlayerPrefs`의 `TinyFishing.BestScore` 키에 저장됩니다.

## 씬 및 빌드 정책

- `Pond FPV`가 유일한 활성 빌드 씬입니다.
- `Pond Start Menu`는 보존하지만 현재 빌드 목록에는 포함하지 않습니다.
- 씬과 데이터의 객체 참조를 유지하기 위해 자산 이동 시 `.meta`와 GUID를 보존합니다.
