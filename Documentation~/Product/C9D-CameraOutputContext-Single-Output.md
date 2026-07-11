# C9D — CameraOutputContext single-output runtime

Status: implementation ready for Unity compile and QA validation  
Type: technical runtime authority  
Package: `com.immersive.framework`

## Objective

Introduce the first real runtime authority for one camera output.

```text
CameraRequest
-> CameraOutputContext
-> deterministic winner
```

## Scope

```text
one CameraOutputId per context
request admission
request release
deterministic winner selection
winner restoration after release
explicit ambiguity rejection
diagnostic snapshot
```

## Out of scope

```text
Route/Activity/Player request publishers
Cinemachine priority or channel application
Unity Camera mutation
blending
multiple output registry
global manager or service locator
automatic lifetime observation
```

## Arbitration

Higher `CameraRequestPolicy.Precedence` wins.

Equal precedence is accepted only when both requests declare distinct
`DeterministicTieBreakerId` values.

Tie-breaker ordering uses ordinal ascending comparison.

```text
precedence 200 > precedence 100
tieBreaker "activity" < tieBreaker "player"
```

Missing or duplicate tie-breakers at equal precedence block admission.

## Release

Releasing the current winner recalculates the winner from remaining admitted requests.

```text
Route admitted
Player admitted -> Player wins
Activity admitted -> Activity wins
Activity released -> Player restored
Player released -> Route restored
Route released -> output has no winner
```

## Explicit failures

```text
invalid context output id -> constructor exception
invalid request -> Blocked
output mismatch -> Blocked
duplicate request id -> Blocked
ambiguous equal precedence -> Blocked
invalid release id -> Blocked
unknown release id -> NotFound + warning
```

## Product surface

None. C9D is runtime authority only.

## Expected QA

A dedicated QA Route and scene should prove:

```text
first winner establishment
higher-precedence override
lower-precedence preservation
deterministic equal-precedence ordering
missing tie-breaker blocked
duplicate tie-breaker blocked
duplicate request id blocked
foreign output blocked
winner restoration chain
unknown release explicit
snapshot ordering
no Unity Camera or Cinemachine mutation
```

## Suggested commit

```text
Camera: add single-output CameraOutputContext runtime
```
