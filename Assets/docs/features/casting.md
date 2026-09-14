# Casting

Status: Planned

## Goal

플레이어가 낚시대를 Casting 할 수 있다.

## Related Docs

- ../design/core-loop.md
- ../systems/fishing_basis.md
- ../systems/fishing_input.md

## Behavior

Casting 이벤트 발생 시 낚시대를 던진다.

## Rules

input에서 Casting 이벤트가 수신되면, 찌를 던지는 애니메이션을 실행하고, 게임 상태를 Waiting으로 변경한다.

## Acceptance Criteria

- [ ] 낚시대 던지기 애니메이션
- [ ] Casting 후 게임 상태 변경