# NAN Tiny Fishing Demo

`Pond FPV`를 단일 플레이 씬으로 사용하는 모바일 낚시 데모입니다. 기기 센서 입력과
에디터용 마우스·키보드 입력을 지원하며, 캐스팅부터 입질 대기, 릴 조작, 결과 표시까지
한 라운드 흐름을 제공합니다.

## 실행

1. Unity `6000.3.20f1`로 프로젝트를 엽니다.
2. `Assets/Project/Scenes/Pond FPV.unity`를 엽니다.
3. Play Mode를 시작합니다.

Build Settings에는 `Pond FPV`만 활성화되어 있습니다. `Pond Start Menu`는 보존된 별도
씬이며 현재 빌드 진입점에는 포함되지 않습니다.

## 폴더 구조

| 경로 | 내용 |
|---|---|
| `Scenes` | `Pond FPV`, `Pond Start Menu` |
| `Scripts` | TinyFishing 런타임 코드와 `FishDefinition` 데이터 타입 |
| `Data` | 게임 설정, 어종 정의, 물고기 풀 |
| `Prefabs/Fish` | 어종별 물고기 프리팹과 전용 재질 |
| `Prefabs/Fishing` | 낚싯대와 찌 프리팹 |
| `Art/Materials` | 낚시 및 환경 재질 |
| `Art/UI` | 낚시 HUD 스프라이트 |

## 주요 조작

- 모바일: 기기 동작으로 캐스팅·낚싯대 방향을 제어하고 화면 입력으로 릴을 감습니다.
- 에디터/Standalone: 키보드와 마우스 fallback 입력을 사용합니다.
- 라운드 흐름: `ReadyToCast → WaitingForBite → Reeling → RoundResult`.

## 유지보수 원칙

- `TinyFishing.*` 및 `NanFishing.Data.FishDefinition` namespace는 직렬화 호환성을 위해
  유지합니다.
- 씬, 프리팹, ScriptableObject 연결은 Unity GUID를 기준으로 관리합니다.
- 서드파티 코드는 `Assets/Unity Assets` 아래에서 별도로 관리합니다.
