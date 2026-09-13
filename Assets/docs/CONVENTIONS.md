# Development Conventions

## C#

Class / Method:
PascalCase

private field:
_camelCase

## Unity

public field 사용을 피한다.

[SerializeField]
private GameObject _target;

형태를 사용한다.

## MonoBehaviour

가능한 얇게 유지한다.

복잡한 게임 로직을 MonoBehaviour 내부에 직접 구현하지 않는다.

## Performance

Update에서:
- Find
- GetComponent
- LINQ

사용을 피한다.

## File Rules

하나의 파일에는 하나의 public class.

파일명은 class명과 동일.

## Git / Version Control
하나의 기능을 개발, 수정 했을 경우마다 Commit을 수행한다.
커밋 메세지는 다음과 같이 한다.
Feature : Detail
Feature
- feat : 새로운 기능 추가
- fix : 버그 수정
- docs : 문서 수정
- style : 코드 스타일 변경
- design : 사용자 UI 변경
- refactor : 리팩토링

위의 분류에 해당하지 않는 경우 사용자 혹은 상위 에이전트에게 확인한다.