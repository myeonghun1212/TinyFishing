# Project Instructions

## Required Reading

Before working on this project, read:

- docs/GAME.md
- docs/ARCHITECTURE.md
- docs/CONVENTIONS.md

## Context Discovery

Before implementing a feature:

1. Read the relevant document under docs/design/.
2. Read the relevant system specifications under docs/systems/.
3. Read the feature specification under docs/features/, if one exists.
4. Inspect related existing code and tests.

Do not read unrelated documentation unless necessary.

## Implementation

- Follow docs/ARCHITECTURE.md.
- Follow docs/CONVENTIONS.md.
- Do not change architecture without explicit approval.
- Do not implement functionality outside the requested scope.

## Verification

After implementation:

1. Compile.
2. Run relevant tests.
3. Check the feature's acceptance criteria.
4. Review the final diff.

## Subagents

Use subagents for independent investigation when useful.

Good candidates:
- design/spec investigation
- codebase/architecture investigation
- test and edge-case investigation

Prefer parallel delegation when tasks are independent.
Avoid multiple agents modifying the same files concurrently.

## NVIDIA Kimi worker

For tasks suitable for independent review, analysis, or second opinions,
you may delegate work to NVIDIA Kimi K3.

Invoke:

powershell -ExecutionPolicy Bypass -File tools/nim-agent.ps1 -Prompt "<task>"

Use Kimi primarily for:

- code review
- architecture review
- bug hypothesis generation
- alternative implementation proposals
- large-scale code inspection
- independent second opinions

The primary Astra agent remains responsible for:
- deciding whether delegation is useful
- validating Kimi's output
- modifying the repository
- running tests
- making final decisions

Do not blindly apply Kimi's suggestions.

## Unity CLI

If Unity Editor work is required, the `unity-cli` skill is used first.

Use unity-cli for the following tasks whenever possible:

- Verify Unity project compilation
- Run the EditMode / PlayMode test
- Scene verification
- Check the state of prefabs and GameObjects
- Tasks that can only be viewed in the Unity Editor

Do not guess and directly modify the Unity project file,
First, check the information that can be viewed with unity-cli.