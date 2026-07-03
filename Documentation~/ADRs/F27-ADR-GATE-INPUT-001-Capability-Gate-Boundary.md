# F27-ADR-GATE-INPUT-001 — Gate as Capability Admission Boundary

Status: Accepted / F27D runtime reframe applied  
Phase: F27 — Pause UIGlobal / Input / Gate Reframe  
Type: Framework Core / Gate / Input Boundary  
Date: 2026-06-28

---

## 1. Context

F17 introduced Gate as passive admission primitives. F20 connected Pause to Gate through a Pause-derived blocker snapshot. F27A-F27B then connected Pause to a concrete UIGlobal surface and to Unity Input System `PauseToggle`.

After F27B, the next question was whether Pause should freeze gameplay through component-specific blockers or whether Gate should be closer to the input/command boundary.

The audit conclusion is that Gate must not become a component blocker.

---

## 2. Decision

Gate is a capability/admission boundary.

It answers whether a scoped request, input acceptance, interaction acceptance or command should be admitted.

Gate is not responsible for finding components or telling them to pause.

Canonical shape:

```text
producer creates blockers
consumer evaluates admission
consumer decides local reaction
```

Examples:

```text
Pause produces blockers.
Transition produces blockers.
Unity input adapters evaluate blockers before emitting player commands.
Interaction adapters evaluate blockers before accepting interaction.
Gameplay command producers evaluate blockers before mutating gameplay.
```

---

## 3. Pause relationship

Pause is one producer of blockers.

Pause does not own Gate and must not define Gate as a Pause-only concept.

Pause should initially block player-facing capabilities, not components:

```text
Input / InputAcceptance
Interaction / InteractionAcceptance
```

Pause should keep these admissible unless a future policy explicitly says otherwise:

```text
Pause / PauseRequest
UI / UiNavigation
```

This lets `PauseToggle`, menu navigation and resume remain available while gameplay-facing input is blocked.

---

## 4. Input relationship

The Unity Input System remains an adapter detail.

Correct flow:

```text
InputAction.performed
  -> Unity input adapter
  -> Gate admission evaluation
  -> framework/gameplay command if allowed
```

Incorrect flow:

```text
Gate owns InputActionAsset
Gate switches action maps
Gate pauses PlayerInput components
```

Action-map switching and input modes remain a separate topic.

---

## 5. Component relationship

A gameplay component may react locally to a command not being emitted, or to a separate local receiver, but Gate does not call component APIs directly.

Do not implement a model like:

```text
Pause -> find every player/enemy/door/camera -> call Pause() on each component
```

The accepted model is:

```text
Pause -> blockers
Input/command adapter -> admission check
Component -> receives no gameplay command while blocked
```

---

## 6. TimeScale relationship

`Time.timeScale` is a freeze policy, not the Gate itself.

Future Pause freeze options may be:

```text
GateOnly
TimeScaleOnly
Hybrid
```

But this policy must stay separate from Gate admission. Gate expresses whether a capability is allowed; `Time.timeScale` changes global Unity time behavior.

---

## 7. Runtime follow-up applied in F27D

F27D updates runtime vocabulary and diagnostics so the framework stops implying that Gate is a broad component/gameplay freezer.

Applied direction:

```text
blocksGameplay -> blocksInputAcceptance
blocksInteraction -> blocksInteractionAcceptance
blocksPauseRequest -> allowsPauseRequest / blocksPauseRequest diagnostic remains only as a negative proof
```

`Gameplay / GameplayAction` remains available as a future domain, but F27 Pause now uses input/interaction admission first.

---

## 8. Consequences

Positive:

```text
Pause remains small.
Input adapters become the first real Gate consumers.
Component-specific pause is deferred until there is a mature gameplay object model.
TimeScale policy remains configurable and separate.
```

Cost:

```text
Gameplay systems that bypass input/command adapters will not be automatically blocked by GateOnly pause.
Those systems must either use scaled time, consult Gate, or implement a local receiver in a future adapter cut.
```


---

## F27E cancellation addendum

After F27D, the proposed F27E direction was rejected.

Gate remains a capability/admission boundary, but ordinary input consumers should not each query Gate as the primary way to implement Pause. That direction would spread Pause behavior across consumers and bypass the missing InputMode decision.

Corrected handoff:

```text
Pause -> InputMode -> Unity Input adapter/action-map policy
```

Gate remains available for lifecycle admission, hard locks, stale/foreign/in-flight safety and diagnostics. It is not the normal action-map switch mechanism.

See `F28-ADR-INPUT-001-InputMode-Adapter-Boundary.md`.
