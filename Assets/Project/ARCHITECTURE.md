# NAN Fishing 개발 아키텍처

이 문서는 프로젝트를 처음 인수한 Unity 개발자가 런타임 구조와 데이터 흐름을 이해하고,
특히 모바일 좌우 입력을 안전하게 유지보수할 수 있도록 작성되었습니다. 설명은 현재
저장소의 구현을 기준으로 하며, 자이로 회전축 변경 코드는 포함하지 않습니다.

## 1. 시스템 개요

게임은 하나의 `SampleScene`과 런타임에 부착되는 세 개의 주요 서비스로 구성됩니다.
씬은 환경, HUD, 데이터 참조를 보유하고 `DemoBootstrap`이 입력, 낚시, 세션 흐름을
조립합니다.

```text
SampleScene
 ├─ DemoBootstrap
 │   ├─ MotionInputService     센서/터치/키보드 → IPlayerInput
 │   ├─ FishingController      캐스팅, 물고기, 릴링, 라운드
 │   └─ GameFlowController     보정, 세션, 점수, 저장, HUD 연결
 ├─ Environment
 │   ├─ FishingRod
 │   ├─ BobberAnchor
 │   └─ FishPool
 └─ FishingHUD
     ├─ StartPanel
     ├─ GameplayPanel
     └─ ResultPanel
```

핵심 의존 방향은 다음과 같습니다.

```text
Input ──IPlayerInput──> Fishing ──events──> Core ──commands──> UI
                             │                 │
                             └── Data <── Modes/Save
```

`FishingController`는 구체 입력 장치가 아니라 `IPlayerInput`만 알지만,
`GameFlowController`는 보정 상태를 표시하기 위해 현재 구현체인
`MotionInputService`를 직접 받습니다.

## 2. 디렉터리와 책임

| 영역 | 주요 타입 | 책임 |
|---|---|---|
| `Scripts/Core` | `DemoBootstrap`, `GameFlowController`, `GameState` | 조립, 전체 세션 흐름, 상태 정의 |
| `Scripts/Input` | `IPlayerInput`, `MotionInputService` | 센서와 대체 입력을 공통 게임 입력으로 변환 |
| `Scripts/Fishing` | `FishingController`, `FishController`, `LineTensionModel` | 캐스팅과 릴링, 물고기 행동, 장력 규칙 |
| `Scripts/Data` | `GameBalanceConfig`, `FishDefinition`, `SaveService` | 밸런스·어종 데이터와 로컬 저장 |
| `Scripts/Modes` | `IGameModeRule`, `TimeAttackRule` | 제한 시간, 점수, 콤보 규칙 |
| `Scripts/UI` | `FishingHUD` | 상태 패널과 플레이 정보 표시 |
| `Scripts/Editor` | 씬/UI 빌더, `NanFishingTests` | 편집 도구와 Edit Mode 테스트 |
| `Scenes` | `SampleScene`, 설정 및 어종 에셋 | 직렬화된 씬과 ScriptableObject 데이터 |
| `Art` | 재질 및 향후 제작 에셋 | 시각 리소스 |

`Assets/Project/Script/CastFishingrod.cs`는 비어 있고 어떤 런타임 책임도 수행하지
않습니다. 이름은 캐스팅 기능처럼 보이지만 실제 캐스팅은 `MotionInputService`와
`FishingController`에 구현되어 있습니다. 혼동을 유발하는 기술 부채이므로 향후 참조
여부를 확인한 뒤 제거하거나 명확한 용도로 대체해야 합니다.

## 3. 씬과 런타임의 경계

### 씬에 저장되는 항목

- `DemoBootstrap`과 `GameSetting`, `fishCatalog` 직렬화 참조
- 카메라, 환경, 조명과 소품
- `FishingRod`, `BobberAnchor`, `FishPool`
- `FishingHUD`와 세 패널 및 모든 Text/Image/Button 참조
- EventSystem이 이미 존재한다면 해당 오브젝트

### 런타임에 생성·부착되는 항목

- `MotionInputService`
- `FishingController`
- `GameFlowController`
- 입질 이후 생성되는 `FishController`와 물고기 인스턴스
- 씬 참조가 없을 때의 제한적인 fallback 오브젝트
- EventSystem이 없을 때의 `EventSystem`과 `InputSystemUIInputModule`

이 구분 때문에 씬에서 `MotionInputService`를 별도로 추가하면 중복 동작 가능성이
있습니다. 서비스 생명주기는 `DemoBootstrap`에 맡기는 것이 현재 설계의 전제입니다.

## 4. 초기화 순서와 참조 연결

`DemoBootstrap.Awake()`는 다음 순서로 실행됩니다.

1. 이미 `GameFlowController`가 있으면 중복 초기화를 막고 종료합니다.
2. `gameSetting`과 `fishCatalog`가 유효한지 검사합니다.
3. 목표 프레임을 60, 화면 방향을 Portrait로 지정합니다.
4. 메인 카메라를 찾거나 생성하고 데모용 위치와 렌더 설정을 적용합니다.
5. `BobberAnchor`와 `FishPool`을 `GameObject.Find`로 찾습니다.
6. EventSystem이 없으면 생성합니다.
7. 같은 GameObject에 `MotionInputService`를 추가하고 설정을 전달합니다.
8. `FishingController`를 추가하고 입력, 설정, 어종, 씬 Transform을 전달합니다.
9. 비활성 오브젝트를 포함해 `FishingHUD`를 찾습니다.
10. `GameFlowController`를 추가하고 위 객체를 연결합니다.

`FishingController.Initialize()`는 별도로 `FishingRod`를 이름으로 검색해 현재
월드 회전을 `rodRestRotation`으로 저장합니다. 따라서 다음 이름은 사실상 런타임
계약입니다.

| 이름/타입 | 검색 위치 | 없을 때 |
|---|---|---|
| `FishingRod` | `FishingController` | 낚싯대 시각 회전만 생략 |
| `BobberAnchor` | `DemoBootstrap` | 오류 로그 후 fallback 생성 |
| `FishPool` | `DemoBootstrap` | 오류 로그 후 fallback 생성 |
| `FishingHUD` 타입 | `DemoBootstrap` | 오류 로그 후 흐름 서비스 생성 중단 |

이름을 바꾸면 컴파일은 성공하지만 런타임 연결이 끊길 수 있습니다. 장기적으로는
`DemoBootstrap`의 직렬화 필드 또는 명시적인 씬 참조 컨테이너로 바꾸는 것이 안전합니다.

## 5. 게임 상태와 실행 흐름

`GameState`는 낚시 라운드 상태이고, 세션 실행 여부는 `GameFlowController`의
`sessionRunning`이 별도로 관리합니다.

| 단계 | 상태/조건 | 핵심 처리 | 다음 단계 |
|---|---|---|---|
| 시작 | `sessionRunning == false` | HUD 시작 패널, `BeginCalibration()` | 보정 |
| 보정 | `IsCalibrated == false` | 정지 상태 누적 후 기준 자세 저장 | 캐스팅 대기 |
| 캐스팅 대기 | `Casting` | 캐스트 이벤트 수신 | 첫 캐스트가 세션 시작 |
| 찌 이동/대기 | `WaitingForBite` | 코루틴으로 포물선 이동, 랜덤 지연 | 물고기 생성 |
| 릴링 | `Reeling` | 물고기와 장력을 매 프레임 계산 | 성공 또는 줄 끊김 |
| 라운드 결과 | `CatchResult` | 점수·콤보·수집 갱신 | 지연 후 `Casting` |
| 세션 종료 | `SessionResult` | 점수 저장 및 결과 패널 | 재시작 |

첫 `CastPerformed` 이벤트는 두 곳에서 받습니다. `GameFlowController`는 첫 캐스트로
타임어택 세션을 시작하고, `FishingController`는 실제 찌 투척을 시작합니다. 이벤트
구독 순서에 의존해 기능을 설계하지 않도록 주의해야 합니다.

### 코루틴 처리

`FishingController.CastAndBiteRoutine()`은 찌를 0.55초 동안 포물선으로 이동시키고
`biteDelayRange`만큼 기다린 후 물고기를 생성합니다.
`ReturnToCastingRoutine()`은 결과를 `catchResultDuration` 동안 보여준 뒤 다음
캐스팅으로 돌아갑니다.

### 프레임 처리

- `MotionInputService.Update()`: 입력 장치와 보정, 캐스팅 제스처 갱신
- `FishingController.Update()`: 릴링 중 물고기와 장력 계산
- `GameFlowController.Update()`: 보정 HUD 또는 세션 타이머 갱신

## 6. 이벤트 연결

| 이벤트 | 발행자 | 구독자 | 목적 |
|---|---|---|---|
| `CastPerformed` | `MotionInputService` | `FishingController` | 찌 투척 시작 |
| `CastPerformed` | `MotionInputService` | `GameFlowController` | 첫 캐스트에서 세션 시작 |
| `StateChanged` | `FishingController` | `GameFlowController` | HUD 상태 문구 변경 |
| `RoundResolved` | `FishingController` | `GameFlowController` | 점수, 콤보, 저장 대상 갱신 |
| `ReelingUpdated` | `FishingController` | `FishingHUD.SetReeling` | 장력·진행도·방향 표시 |
| `RecalibrateRequested` | `FishingHUD` | `MotionInputService.Recalibrate` | 현재 자세 재보정 |
| `RestartRequested` | `FishingHUD` | `GameFlowController.Restart` | 현재 씬 다시 로드 |

`FishingController`와 `GameFlowController`는 `OnDestroy()`에서 주요 이벤트를 해제합니다.
HUD 버튼 리스너는 `FishingHUD` 내부에서 한 번만 바인딩되며 씬 재로드로 함께 폐기됩니다.

## 7. 입력 계약

`IPlayerInput`은 게임플레이가 입력 장치를 직접 알지 않게 하는 경계입니다.

| 멤버 | 의미 |
|---|---|
| `CastPerformed` | 감지된 캐스팅과 강도 `Short/Medium/Long` |
| `Direction` | 플레이어 좌우 방향, 정규화 범위 `[-1, 1]` |
| `IsReeling` | 화면/마우스/Space를 누르는 동안 `true` |
| `IsCalibrated` | 센서 또는 대체 입력 사용 준비 완료 |
| `HasMotionSensors` | `AttitudeSensor`와 `Accelerometer`가 모두 존재 |
| `CalibrationProgress` | 정지 보정 진행도 `[0, 1]` |
| `BeginCalibration()` | 안정 상태를 측정하는 보정 시작 |
| `Recalibrate()` | 현재 자세를 즉시 중립으로 재지정하거나 보정 재시작 |

`Direction`은 단순한 낚싯대 애니메이션 값이 아닙니다. 줄 장력 계산과 HUD 방향 표시의
공용 도메인 값이므로 센서 구현을 바꿔도 범위와 좌우 부호 계약을 유지해야 합니다.

## 8. 센서와 대체 입력의 실제 처리

### 8.1 장치 활성화

`TryEnableSensors()`는 `AttitudeSensor`와 `Accelerometer`가 모두 있을 때만
`HasMotionSensors`를 `true`로 설정하고 두 장치를 활성화합니다. `Gyroscope`가 있으면
함께 활성화하지만 현재 코드는 `Gyroscope.angularVelocity`를 읽지 않습니다.

즉, 현재 흔히 말하는 “자이로 좌우 입력”의 실제 데이터 출처는 `Gyroscope`가 아니라
`AttitudeSensor.attitude`입니다.

### 8.2 보정

1. `BeginCalibration()`이 안정 시간과 상태를 초기화합니다.
2. 가속도 값을 다음 식으로 완화합니다.

   ```text
   filteredAcceleration =
       Lerp(previousFiltered, currentAcceleration, sensorSmoothing)
   ```

3. 가속도 크기가 1g에서 벗어난 정도가 `stableAccelerationTolerance` 이하면
   `stableTime`을 누적하고, 움직이면 다시 0으로 만듭니다.
4. `stableDuration` 이상 정지하면 현재 `attitude`를 `baselineAttitude`로 저장합니다.

`Recalibrate()`는 센서가 있으면 안정 시간 검사를 기다리지 않고 현재 attitude를 즉시
기준으로 지정합니다. 따라서 버튼을 누르는 순간의 자세가 중립 자세가 됩니다.

### 8.3 상대 자세와 현재 좌우 방향

보정 후 현재 자세는 다음과 같이 기준 자세에 대한 상대 회전으로 바뀝니다.

```csharp
relative = Quaternion.Inverse(baselineAttitude) * attitude;
relativeEuler = relative.eulerAngles;
pitch = NormalizeAngle(relativeEuler.x);
roll = NormalizeAngle(relativeEuler.z);
Direction = Clamp(roll / maximumTiltAngle, -1, 1);
```

현재 코드가 좌우 입력에 선택한 값은 상대 Euler 회전의 Z 성분입니다. 변수 이름은
`roll`이지만, 사용자가 체감하는 회전은 기기 기본 좌표, 운영체제의 센서 좌표,
Portrait 화면 방향과 기준 자세에 따라 예상한 좌우 기울임과 다를 수 있습니다.
현재 실기기에서 수평면 좌우 방향 전환처럼 느껴지는 현상도 이 좌표계 해석을 먼저
측정해야 합니다.

`maximumTiltAngle`은 선택한 회전량이 몇 도일 때 최대 입력이 되는지를 결정합니다.
기본값 32도이면 Z 상대각이 -32도에서 `-1`, +32도에서 `+1`에 도달합니다.

### 8.4 캐스팅

- 뒤로 휘두르기: 상대 Euler X인 `pitch <= -backswingAngle`
- 앞으로 휘두르기: `-filteredAcceleration.z >= forwardSwingAcceleration`
- 재감지 제한: `castCooldown`
- 강도: 전방 충격 값이 1.55 미만이면 Short, 1.55 이상이면 Medium,
  2.2 이상이면 Long

가속도계 Z는 캐스팅 충격에 사용되며 좌우 `Direction` 계산에는 사용되지 않습니다.

### 8.5 터치, 마우스와 키보드

- 터치 또는 마우스 버튼을 누르면 `IsReeling = true`입니다.
- 센서가 없을 때 포인터 X 위치를 화면 중앙 기준 `[-1, 1]`로 변환합니다.
- 버튼을 놓을 때 위쪽 이동량이 화면 높이의 12%보다 크면 캐스팅합니다.
- 에디터/Standalone에서 `A`, `D`, 방향키는 프레임마다 `Direction`을 0.05씩
  변경합니다.
- `Space`는 릴 감기, `Enter`는 Medium 캐스팅입니다.
- 센서가 없는 환경에서 보정 요청을 받으면 즉시 보정 완료 상태로 전환됩니다.

## 9. 향후 좌우 기울이기 입력으로 변경하기

목표는 Portrait 상태에서 휴대폰 상단이 왼쪽 또는 오른쪽으로 기울어질 때
`Direction`이 변하도록 만드는 것입니다. 변경의 주 진입점은
`MotionInputService.UpdateMotion()` 안의 `relative` 자세에서 `Direction`을 만드는
부분입니다.

### 유지해야 할 외부 계약

- 중립 자세: `Direction == 0`
- 왼쪽 동작: 프로젝트가 정한 일관된 음수 또는 양수
- 오른쪽 동작: 왼쪽의 반대 부호
- 출력 범위: 항상 `[-1, 1]`
- `maximumTiltAngle` 또는 대체 민감도 설정으로 최대 입력 조절
- 보정, 터치 fallback, 키보드 fallback과 캐스팅 감지는 계속 동작

이 계약을 유지하면 `FishingController`, `LineTensionModel`, `FishingHUD`는 센서 축
변경을 알 필요가 없습니다.

### 변경 전에 실기기에서 측정할 값

개발용 로그 또는 임시 디버그 UI로 다음 자세를 각각 1~2초 기록하십시오.

1. 평소 플레이 중립 자세
2. 휴대폰 상단을 왼쪽으로 기울인 자세
3. 휴대폰 상단을 오른쪽으로 기울인 자세
4. 현재 체감되는 수평면 왼쪽·오른쪽 방향 전환

각 자세에서 최소한 아래 값을 함께 기록해야 합니다.

- 원본 `attitude`
- `baselineAttitude`
- 상대 `relative` quaternion
- 정규화한 relative Euler X/Y/Z
- `Screen.orientation`
- 최종 `Direction`

이 측정으로 어떤 축이 의도한 기울임에 가장 일관되게 반응하는지와 부호 반전 필요성을
결정해야 합니다. Android 제조사와 기기 방향에 따라 한 기기 결과만 일반화하지 않는
것이 좋습니다.

### 구현 선택지

1. **Euler 축 교체**  
   현재 Z 대신 측정된 X/Y/Z 중 하나를 선택합니다. 변경이 작고 빠르지만 180도 경계,
   축 결합과 Euler 특이점에 취약합니다.
2. **상대 자세로 기준 벡터 회전**  
   `relative`로 기기의 up/right/forward 벡터를 회전한 뒤 원하는 기준 평면에 투영해
   signed angle을 계산합니다. 동작 의미를 코드에 명확히 표현할 수 있습니다.
3. **Swing-twist 또는 quaternion 성분 분해**  
   특정 물리 축의 회전만 분리합니다. 가장 정교하지만 구현과 테스트 비용이 큽니다.

현재 문서화 단계에서는 축을 확정하지 않습니다. 실기기 로그를 기준으로 가장 단순하면서
Portrait 자세에서 안정적인 방식을 선택해야 합니다.

### 센서 축과 모델 축을 구분할 것

센서 축은 `MotionInputService`가 사용자의 움직임을 `Direction`으로 해석하는 기준입니다.
낚싯대 모델 축은 `FishingController`가 그 값을 화면에 표현하는 기준입니다.

```csharp
tiltRotation = Quaternion.AngleAxis(-Direction * 18f, Vector3.forward);
rodRotation = tiltRotation * rodRestRotation;
```

여기서 `Vector3.forward`를 바꾸는 것은 낚싯대 모델의 시각적 회전축을 바꾸는 것이며,
휴대폰에서 읽는 센서 축을 바꾸지 않습니다. 반대로 입력 축만 바꾸면 장력과 HUD는
바뀌지만 모델 계층 또는 초기 회전에 따라 낚싯대 시각이 기대와 다를 수 있습니다.

## 10. 낚시 규칙

### 물고기 방향

`FishController`는 `[-1, 1]`에서 임의 목표 방향을 고르고 `MoveSpeed`로 접근합니다.
목표는 `DirectionChangeInterval`에 0.75~1.25의 랜덤 배율을 곱한 간격으로 바뀝니다.
화면상 위치는 중심에서 `Direction * 2.4m`만큼 X축으로 이동합니다.

### 방향 오차

```text
directionError = Abs(playerDirection - fishDirection) * 0.5
```

두 방향이 같으면 0이고 정반대 끝이면 최대 1입니다.

### 릴을 감는 동안

```text
Progress += reelProgressPerSecond * dt / Max(0.25, resistance)
strain = Max(0, directionError - directionDeadZone)
pullLoad = 0.16 + strain * 1.35
Tension += tensionGainPerSecond * pullLoad * resistance * dt
```

완전히 정렬해도 `pullLoad`의 기본값 0.16 때문에 장력은 조금씩 증가합니다. 저항이
큰 물고기는 진행이 느리고 장력은 더 빨리 증가합니다.

### 릴을 놓는 동안

```text
Progress -= escapeProgressPerSecond * resistance * dt
Tension -= tensionRecoveryPerSecond * dt
```

장력을 낮추는 대신 진행도를 잃습니다.

### 성공과 실패

- `Progress >= 1`: 포획 성공
- `Tension >= dangerTension`: 위험 시간 증가
- 위험 상태가 `breakGraceDuration` 이상 지속: 줄 끊김
- 위험 기준 아래에서는 위험 시간이 초당 2배 속도로 감소
- `Progress`와 `Tension`은 항상 `[0, 1]`로 제한

따라서 센서 축이나 부호가 잘못되면 낚싯대 모양만 어색해지는 것이 아니라
`directionError`가 계속 커져 난이도와 줄 끊김 빈도가 직접 달라집니다.

## 11. 데이터와 설정

### `GameBalanceConfig`

| 그룹 | 필드 | 의미 |
|---|---|---|
| Session | `sessionDuration` | 세션 제한 시간 |
| Session | `biteDelayRange` | 캐스팅 후 입질까지 랜덤 지연 |
| Session | `catchResultDuration` | 라운드 결과 표시 시간 |
| Motion | `stableDuration` | 정지 보정에 필요한 시간 |
| Motion | `stableAccelerationTolerance` | 1g 기준 허용 편차 |
| Motion | `backswingAngle` | 백스윙 판정 각도 |
| Motion | `forwardSwingAcceleration` | 전방 캐스팅 가속 임계값 |
| Motion | `castCooldown` | 캐스팅 재감지 제한 |
| Motion | `sensorSmoothing` | 가속도 Lerp 비율 |
| Motion | `maximumTiltAngle` | `Direction` 최대값에 대응하는 각도 |
| Reeling | `startingTension` | 라운드 시작 장력 |
| Reeling | `reelProgressPerSecond` | 릴 진행 속도 |
| Reeling | `escapeProgressPerSecond` | 릴을 놓았을 때 진행 손실 |
| Reeling | `tensionGainPerSecond` | 장력 증가 계수 |
| Reeling | `tensionRecoveryPerSecond` | 장력 회복 속도 |
| Reeling | `directionDeadZone` | 방향 오차 무시 범위 |
| Reeling | `dangerTension` | 줄 끊김 위험 장력 |
| Reeling | `breakGraceDuration` | 위험 허용 시간 |
| Score | `quickCatchBonus` | 4초 이내 포획 보너스 |
| Score | `comboStepBonus` | 연속 포획 단계 보너스 |

현재 씬은 `Scenes/GameSetting.asset`을 `DemoBootstrap`에 할당합니다.
`CreateRuntime()`은 주로 테스트나 런타임 fallback 설정 생성에 사용할 수 있습니다.

### `FishDefinition`

어종 ID, 표시 이름, 희귀도, 기본 점수, 저항, 이동 속도, 방향 변경 간격, 색상과
선택적 프리팹을 보유합니다. 프리팹이 없으면 `FishingController`가 Sphere와 Cube로
절차형 물고기를 생성합니다. `ConfigureRuntime()`은 런타임 생성 데이터를 설정하는
API이며 현재 씬에서는 `FishDefinitions`의 에셋 카탈로그를 사용합니다.

### `TimeAttackRule`

시간, 점수, 포획 수, 현재·최대 콤보를 순수 규칙 객체로 관리합니다.

```text
rarityMultiplier = 1 + rarity * 0.5
awarded =
    Round(baseScore * rarityMultiplier)
    + (catchDuration <= 4 ? quickCatchBonus : 0)
    + Max(0, combo - 1) * comboStepBonus
```

실패하면 현재 콤보만 0이 되고 누적 점수와 최대 콤보는 유지됩니다.

### `SaveService`

`PlayerPrefs` 키 `nan_fishing_save_v1`에 JSON으로 최고 점수와 발견한 어종 ID 목록을
저장합니다. 저장 스키마는 `SaveData`이며 버전 마이그레이션 로직은 아직 없습니다.

## 12. UI 데이터 흐름

| UI | 데이터 출처 |
|---|---|
| 시작 안내/보정률 | `MotionInputService` → `GameFlowController.Update()` |
| 상태 안내 문구 | `FishingController.StateChanged` |
| 시간/점수/포획/콤보 | `TimeAttackRule` |
| 장력/진행도 | `LineTensionModel` → `ReelingUpdated` |
| 물고기 마커 | `FishController.Direction` |
| 플레이어 마커 | `IPlayerInput.Direction` |
| 휴대폰 그림 회전 | `-playerDirection * 25°` |
| 낚싯대 회전 | `-playerDirection * 18°` |
| 결과/수집률 | `TimeAttackRule`, `SaveService`, 어종 카탈로그 |

`FishingHUD.SetPanel()`은 `StartPanel`, `GameplayPanel`, `ResultPanel` 중 하나만
활성화합니다. HUD 텍스트는 현재 영어 문자열이 코드에 직접 들어 있으며 현지화 계층은
없습니다.

## 13. 에디터 빌더

`EditableSceneBuilder`의 메뉴는 `Tools/NAN Fishing/Build Editable Scene`입니다.
기존 `Environment` 전체를 `DestroyImmediate`로 제거한 뒤 환경, 낚싯대, 찌,
FishPool, 카메라와 조명을 다시 만듭니다. 환경을 직접 편집한 상태에서는 파괴적입니다.

`EditableUIBuilder`의 메뉴는 `Tools/NAN Fishing/Build Scene UI`입니다. 기존
`FishingHUD` GameObject를 제거하고 Canvas, 패널과 모든 참조를 다시 구성합니다.
직접 수정한 레이아웃을 유지하려면 실행 전에 변경사항을 커밋하거나 백업해야 합니다.

두 빌더는 초기 데모 복구용 도구이지 일반 실행 과정이 아닙니다.

## 14. 테스트 현황

`NanFishingTests`는 다음을 Edit Mode에서 검증합니다.

- 시간이 0에서 종료되고 음수가 되지 않음
- 성공 시 점수·콤보가 증가하고 실패 시 콤보가 초기화됨
- 방향이 맞으면 최종적으로 포획할 수 있음
- 릴을 감으면 정렬 상태에서도 가시적인 장력이 생김
- 릴을 놓으면 장력과 진행도가 감소함

현재 자동화되지 않은 영역은 다음과 같습니다.

- `AttitudeSensor` 기준 자세와 축 변환
- 정지 보정과 재보정
- 가속도 기반 캐스팅
- 터치/마우스 스와이프
- 전체 상태 전이와 코루틴
- 씬 이름 참조와 HUD 직렬화 참조
- Android 기기 및 화면 방향별 입력

센서 축 변경 시 최소 실기기 시나리오는 중립, 좌우 기울임, 수평 좌우 방향 전환,
재보정 후 반복, 캐스팅과 좌우 입력의 간섭 여부, 최소 두 종류 Android 기기입니다.

## 15. 작업 유형별 유지보수 가이드

| 작업 | 주 수정 위치 | 함께 확인할 곳 |
|---|---|---|
| 센서 축/민감도 | `MotionInputService` | `GameBalanceConfig`, 장력, HUD 마커, 실기기 |
| 키보드/터치 입력 | `MotionInputService` | `IPlayerInput`, README 조작법 |
| 낚싯대 모델 회전 | `FishingController` | 모델 피벗, `rodRestRotation` |
| 장력 난이도 | `GameSetting.asset` | `LineTensionModel`, 테스트 |
| 어종 추가 | `FishDefinition` 에셋과 카탈로그 | prefab, 수집률, 저항 밸런스 |
| 점수/게임 모드 | `TimeAttackRule` | `IGameModeRule`, HUD 결과 |
| HUD 변경 | 씬의 `FishingHUD`와 UI | `FishingHUD.cs`, UI 빌더 |
| 환경/소품 교체 | `SampleScene` 또는 prefab | 런타임 필수 오브젝트 이름 |
| 저장 데이터 변경 | `SaveData`, `SaveService` | 키 버전과 마이그레이션 |

## 16. 알려진 유지보수 포인트

- `GameObject.Find`와 오브젝트 이름이 런타임 계약이라 리팩터링에 취약합니다.
- 핵심 서비스가 런타임 `AddComponent`로 생성되어 Inspector에서 전체 구성을 보기
  어렵습니다.
- `MotionInputService`가 센서, 터치, 마우스, 키보드와 캐스팅 판정을 모두 담당합니다.
- 자세 계산이 Euler 단일 축에 의존하며 화면 방향을 명시적으로 보정하지 않습니다.
- `Gyroscope`를 활성화하지만 읽지 않아 이름과 실제 구현을 혼동하기 쉽습니다.
- `Direction`이 입력, 난이도와 시각화를 동시에 연결하므로 의미 변경의 영향 범위가
  큽니다.
- HUD 문자열과 임계값 일부가 코드에 직접 들어 있습니다.
- 저장 스키마 마이그레이션과 손상 데이터 복구가 없습니다.
- 빈 `CastFishingrod.cs`가 실제 기능 위치를 오해하게 할 수 있습니다.
- 센서 처리에 단위 테스트나 주입 가능한 센서 추상화가 없습니다.

## 17. 변경 안전 수칙

1. 입력 계층은 `Direction [-1, 1]` 계약을 유지합니다.
2. 센서 축 변경 전후에 같은 물고기·밸런스로 장력 체감을 비교합니다.
3. 씬 필수 이름을 바꿀 때는 검색 코드를 동시에 수정하거나 직렬화 참조로 전환합니다.
4. 에디터 빌더 실행 전 씬과 UI 변경사항을 백업합니다.
5. 어종 데이터와 공통 밸런스를 코드 상수로 중복하지 않습니다.
6. 센서 변경은 Android 실기기에서 보정, 좌우, 캐스팅과 재보정을 모두 검증합니다.
7. 사용자 작업 중인 씬, 설정, 데이터 에셋은 문서 작업이나 무관한 리팩터링에서
   변경하지 않습니다.

