# Casting

Status: Implemented

## Goal

플레이어가 낚시대를 Casting 할 수 있다.

## Related Docs

- ../design/core-loop.md
- ../systems/fishing_basis.md
- ../systems/fishing_input.md

## Behavior

Casting 이벤트 발생 시 낚시대를 던진다.

`CastingEventChannel`을 수신하면 낚싯대를 회전시키고 찌를 포물선 궤적으로 착수 지점까지 이동시킨다.
찌가 도착하면 `Waiting` 상태로 전환하며, 시작과 착수 시 UnityEvent를 통해 효과음 등을 연결할 수 있다.

## Rules

input에서 Casting 이벤트가 수신되면, 찌를 던지는 애니메이션을 실행하고, 게임 상태를 Waiting으로 변경한다.

- 상태는 세션별 런타임 객체가 `Ready → Casting → Waiting` 순서로 관리한다.
- `Casting` 또는 `Waiting` 중 다시 수신한 캐스팅 입력은 무시한다.
- `ResetCast()`는 진행 중인 캐스팅을 취소하고 낚싯대와 찌를 시작 위치 및 `Ready` 상태로 복구한다. 컴포넌트 비활성화 시에도 복구한다.
- 비행 시간, 궤적 높이, 낚싯대 회전 각도는 `CastingSettings` ScriptableObject에서 설정한다.
- 게임 진행 상태는 `FishingStateEventChannel`로 알린다.

## Input Scope

현재 `CastingInput`은 캐스팅 확인용 입력 어댑터다. 화면 높이의 12% 이상 위쪽으로 스와이프하거나 마우스로 드래그한 뒤 놓으면 캐스팅 이벤트를 발생시킨다. 수평 이동보다 위쪽 이동이 커야 하며, UI 위에서 시작한 입력과 취소된 터치는 제외한다. 에디터에서는 Space 키도 사용할 수 있다.

자이로 센서 처리, 좌우·상하 조준, 입력 모드 전환, 민감도 API 및 재보정은 `fishing_input.md`의 별도 입력 시스템 범위다. 해당 시스템은 동일한 `CastingEventChannel.Raise()`에 캐스팅 입력을 연결할 수 있다. 입질 대기 이후 후킹·릴링·포획 로직은 이번 기능에 포함하지 않는다.

## Demo Verification

`Assets/Project/Scene/Casting Demo.unity`를 열어 Play 모드로 진입한다. 데모를 생성할 때는 에디터 메뉴 `Nan/Create Casting Demo` 또는 `Nan.Editor.CastingDemoBuilder.Build()`를 사용한다.

위쪽 드래그 또는 Space로 캐스팅한다. 화면의 상태 표시가 `Ready → Casting → Waiting`으로 바뀌고, 낚싯대와 찌가 움직인 뒤 찌가 착수 지점에 멈추는지 확인한다.

- 비행 중 또는 `Waiting`에서 다시 입력해도 새 캐스팅이 시작되지 않아야 한다.
- `Waiting`에서 `Cast again`을 누르면 시작 위치와 `Ready`로 돌아가 다시 캐스팅할 수 있어야 한다.
- 아래 방향·짧은 드래그는 캐스팅을 시작하지 않아야 한다.

## Verification

- Unity 6000.3.20f1 컴파일 성공, 오류 없음.
- EditMode 10개 및 PlayMode 3개 테스트 통과 (총 13개).
- 실제 데모 씬의 Play 모드에서 `CastingInput.RequestCast()`를 통한 이벤트 전달과 최종 `Waiting` 상태를 확인했다. 시작 및 착수 화면에서 찌와 낚싯줄 위치도 확인했다.
- Android 실기기의 자이로 및 터치 입력은 검증하지 않았다.

## Acceptance Criteria

- [x] 낚시대 던지기 애니메이션
- [x] Casting 후 게임 상태 변경
