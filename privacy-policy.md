# Storylines Privacy

Storylines uses a telemetry provider to collect anonymous usage and crash information. The underlying provider may change over time. Telemetry helps improve stability, prioritize fixes, and understand which workflows are used most often. Story content is not intentionally collected.

## Application And Session Data

- App version
- Anonymous session identifier
- Activation type, such as normal launch or file activation
- Whether this is the first run and total launch count
- App uptime
- Current editor mode
- Settings state, including:
  - theme
  - accent mode
  - autosave enabled or disabled
  - autosave interval
  - exit dialog enabled or disabled
  - white text background enabled or disabled
  - dialogue mode enabled or disabled
  - experimental features enabled or disabled
  - review prompt state

## Project Summary Data

- Whether a project is currently open
- Chapter count
- Character count
- Plot thread count
- Branching dialogue graph count
- Branching dialogue node count
- Branching dialogue choice count
- Whether unsaved progress exists

## Feature Interaction Data

- Review prompt display and interaction events
- Microsoft Store update availability notifications
- Focus Mode configuration, including fullscreen, autosave, selected metric, target value, and whether the session was completed
- Project statistics dialog opens
- In-app banner clicks

## Crash Diagnostics

- Exception type and message
- Whether an inner exception exists
- Available memory
- Project summary counts at the time of the error
- Unsaved progress state at the time of the error

## Operating System-Level Data

- Device architecture
- Device family
- Windows build
- Language