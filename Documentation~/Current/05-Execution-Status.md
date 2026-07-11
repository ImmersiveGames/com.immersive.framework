# 05 — Execution Status

Status: **canonical operational source**

Last reconciled decision: **ADR-PROD-0006 Camera Requests and Output Contexts accepted**

This document answers only three questions:

```text
What is closed?
What is active?
What comes next?
```

Architectural constraints are defined by ADRs.

## Current position

```text
P2 — Player Control Product
  Closed at the accepted current shape.

C9A — Camera Architecture ADR and Documentation Reset
  Closed by this documentation delta.

C9B — Destructive Superseded Camera Removal
  Active next.

C9C–C9H — Request/output implementation, QA and FIRSTGAME
  Ordered after C9B.

P3 — Player Spawn / Runtime Materialization
  Ordered after C9.

S1 — Progression Save Runtime
  Ordered after P3 and meaningful FIRSTGAME state.
```

## Frozen camera architecture

```text
CameraTargetSource
  explicit Follow/LookAt provider

CameraRigRecipe
  reusable Cinemachine behavior intent

CameraRigComposer
  idempotent Cinemachine materialization
  no winner-selection authority

CameraRequest
  owner + lifetime + output + targets + policy

CameraOutputContext
  one scoped authority per output/viewport
  arbitrates requests
  applies the winner through Cinemachine
```

Cinemachine executes camera presentation. The framework executes request policy.

## Superseded camera shape

The following must not be used as current guidance or extended:

```text
FrameworkCameraDirector
FrameworkRouteCameraBinding
FrameworkActivityCameraBinding
PlayerViewCameraTargetBindingAdapter
PlayerViewCameraActivationAdapter
Camera.enabled as camera-selection policy
direct independent priority competition
one local CameraComposer treated as complete runtime authority
```

No compatibility wrappers, aliases, obsolete facades or retained QA smokes are authorized.

## Active cut — C9B

### Goal

Physically remove the superseded camera architecture from the package so it cannot return as a parallel path.

### Required removal audit

```text
runtime types
editor tooling
asmdef references made obsolete
validators
QA menus/scenes/smokes
FIRSTGAME guidance
current docs and HTML guidance
recipes/templates that instantiate the old shape
```

### Preserve only where compatible

```text
Cinemachine dependency
neutral Cinemachine materialization helpers
explicit PlayerComposer CameraTarget / LookAtTarget
typed target-resolution primitives
idempotent Apply/Rebuild patterns
diagnostic result patterns
```

A helper is preserved only if it has no dependency on the old Director/binding/activation model and no request-winner authority.

### Acceptance

```text
package compiles
no old camera Director exists
no Route/Activity old binding exists
no PlayerView camera activation exists
no raw Camera.enabled camera policy exists
no current guide teaches the old path
no compatibility surface remains
removal manifest lists all deleted and changed files
```

## Ordered continuation

```text
C9C — Camera request and output contracts
C9D — Single-output CameraOutputContext runtime
C9E — Cinemachine winning-request application
C9F — Route, Activity and Player request publishers
C9G — QA arbitration and restoration
C9H — FIRSTGAME manual integration proof
P3 — Player Spawn / Runtime Materialization
S1 — Progression Save Runtime
```

## Repository roles

```text
com.immersive.framework
  Official product, contracts, runtime, tooling and docs.

QAFramework
  Technical contracts, regressions and negative cases.

FIRSTGAME / planet-devourer
  Real usability and playable integration.
```

Git repositories are read-only inputs. All changes are delivered as ZIP deltas.
