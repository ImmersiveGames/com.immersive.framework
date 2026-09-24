# Logging Usage

Status: Current
Last updated: 2026-07-23

## Normal development

Create or assign a `LoggingConfigAsset` in:

```text
Project Settings > Immersive Framework
```

Use `Info` as the default minimum level. This keeps lifecycle milestones,
explicit authoring operations, actionable warnings and errors visible while
filtering technical state transitions.

Recommended normal profile:

```text
Default Minimum Level = Info
Suppress Standard Log Stack Trace = true
Suppress Warning Stack Trace = true
```

## Levels

| Level | Intended use |
| --- | --- |
| `Trace` | Repeated, fine-grained execution tracking. |
| `Debug` | Technical evidence, resolved optional surfaces and detailed state. |
| `Info` | Operational milestones and explicit user-triggered operations. |
| `Warning` | Recoverable but suspicious conditions that may require action. |
| `Error` | Failed operations or invalid required configuration. |

The Framework does not use a separate `Verbose` level. Use `Trace` for
high-volume tracking and `Debug` for ordinary technical investigation.

## Focused diagnosis

Change the default minimum to `Debug`, or add a namespace/type rule for the
owner under investigation. Prefer a narrow rule such as:

```text
Immersive.Framework.Camera
Immersive.Framework.Audio
Immersive.Framework.PlayerParticipation
```

Use `Trace` only when the repeated sequence itself is needed. Camera and BGM
components retain their latest diagnostic state in Advanced/Debug even when
their Console entries are filtered.

## Release validation

Use `Warning` as the minimum only for a problem-focused release pass. It hides
normal lifecycle milestones and is therefore not the recommended development
default.

## Expected Console shape

With the normal profile, a successful Menu to Gameplay to Menu sequence should
show concise boot and Route/Activity completion milestones. Camera arbitration,
BGM set/clear operations, optional surface resolution and complete validation
issue payloads should appear only when `Debug` or `Trace` is enabled.

All Framework emitters use `com.immersive.logging`; namespace and type rules
therefore apply consistently across runtime and Editor tooling.
