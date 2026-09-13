# Architecture

## Tech Stack

Engine: Unity
Language: C#
Rendering: URP

## Principles

- 게임 로직과 Unity 표현을 가능한 분리한다.
- Runtime State와 Static Data를 분리한다.
- System 간 강한 coupling을 피한다.

## Major Systems

- Game Flow
- Input
- Fishing
- Game Mode
    - 각 게임 모드 별 달라지는 점을 처리하는 시스템
- Save
- UI

## Dependency Rules

UI
↓
Application
↓
Domain

## Data

Static Game Data
→ ScriptableObject

Runtime State
→ Runtime Objects

Persistent Data
→ Save System

## Communication

System 간 통신:
- Interface
- Event
    - Scriptable Object를 통한 Event, EventListener
    - Unity Event

사용 기준:
- 이벤트
    - 게임오브젝트 내부의 여러 컴포넌트의 상호작용은 Unity Event로 처리한다.
    - 게임 진행에 관련되거나, 디버깅을 위해 수동 Rise가 필요하거나, 여러 오브젝트가 Listening 해야하는 이벤트는 SO를 활용한 Event, EventListener을 활용해 처리한다.

## Folder Structure

Assets/
  Scripts/
    Core/
    Input/
    Fishing/
    UI/