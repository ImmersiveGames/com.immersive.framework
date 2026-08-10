# IF-ADR-009 — Activity Local Visibility Rules

Status: **Accepted**  
Last updated: 2026-08-09  
Package implementation: **MATURE — focused package audit remains before final package closure**  
Current package assessment: **26/30** — local planning assessment; not release certification  
Related decisions: IF-ADR-006, IF-ADR-007, IF-ADR-010  
Current package baseline: `43b96a4b100b8273da1190520536007ba82dc081` (`ADR-010B`)

> No implementation is authorized by this revision.
>
> The current runtime exists. The next step is a narrow inspection of the real
> authoring, target validity, occurrence evidence and release/restoration
> diagnostics before deciding whether any package change is necessary.

## Context

Activity-owned content may need to remain hidden, disabled or
presentation-gated until lifecycle/readiness conditions are satisfied.

Visibility must be explicit and scoped rather than inferred from scene load,
hierarchy position or object naming.

## Decision

Activity local visibility is expressed through explicit authored/adapted
configuration bound to Activity lifecycle/readiness.

Visibility authority is contextual and occurrence-aware.

Required visibility failures are blocking and diagnostic.

Optional presentation behavior must not silently weaken required readiness.

## Architectural constraints

- Runtime authority is scoped, typed and lifetime-explicit.
- Required invalid configuration fails explicitly.
- Visibility is not inferred from scene load.
- Object names and hierarchy paths are not fallback identity.
- Lifecycle occurrence/replacement semantics remain explicit.
- Editor authoring does not become gameplay authority.

## Current package coverage

The current package contains `ActivityLocalVisibilityAdapter` integration with
Activity lifecycle and framework-owned discovery scoped to the supplied framework
roots.

The existing implementation already establishes the core runtime model:

```text
authored/local visibility intent
        ↓
Activity occurrence/lifecycle
        ↓
scoped visibility application
        ↓
release/restoration/disposal evidence
```

No global scene search is part of the intended authority model.

## Why this ADR remains the next focused audit

The current evidence is strong enough that a large implementation cut would be
premature.

The remaining package question is narrower:

```text
does the current authoring/diagnostic surface completely expose
the states that the runtime already models?
```

The focused audit must inspect:

```text
authoring validation
target validity
required vs optional target semantics
current Activity binding
occurrence/revision evidence
last application result
release/restoration evidence
stale occurrence behavior
replacement/disposal behavior
```

## Product surface

Do not assume a new Profile, Composer or Apply/Rebuild flow is needed.

The current lifecycle appears compatible with direct authoring.

Only introduce another layer if the focused audit proves that normal consumers
must manually reconstruct internal contracts or repeatedly perform deterministic
technical setup.

## QA

Technical QA is justified for real lifecycle invariants such as:

```text
missing required target
stale occurrence
repeated enter/exit
Route replacement
owner/context disposal
release/restoration ownership
```

Only add tests that prove actual runtime contracts.

Do not create synthetic Inspector UX tests.

## FIRSTGAME

FIRSTGAME can later reveal whether:

```text
the visibility intent is understandable
the target configuration is discoverable
the relation to loading cover is clear
the runtime evidence is useful during debugging
```

Those observations are separate Consumer UX Evidence.

They are not required to establish technical package correctness.

## Current assessment

Current local package assessment:

```text
26 / 30
```

Disposition:

```text
runtime model          EXISTS
large package gap      NOT CONFIRMED
new authoring layer    NOT JUSTIFIED
focused package audit  REQUIRED
```

If the narrow audit confirms that current validation and diagnostic evidence are
already sufficient, the expected package reclassification is approximately:

```text
29 / 30
```

without implementation.

If a real gap is found, fix only that gap.

## What remains

```text
1. inspect the current ActivityLocalVisibilityAdapter authoring surface
2. inspect target validity and occurrence/runtime evidence
3. inspect release/restoration diagnostics
4. classify concrete gaps, if any
5. implement only a proven gap
6. add technical QA only when the corrected/declared contract warrants it
```

## Completion criteria

Package closure requires evidence that:

```text
visibility never becomes implicit scene-load authority
required invalid targets fail explicitly
occurrence ownership is diagnosable
release/restoration affects only context-owned state
replacement/disposal does not leak visibility authority
normal authoring does not require hidden internal contracts
```

Consumer UX evidence remains separate.

## Normative summary

```text
Keep visibility explicit, scoped and occurrence-aware.
Audit the existing product before adding tooling.
Do not infer package incompleteness from missing Composer/Wizard/Apply.
Do not use FIRSTGAME or UX smokes as technical closure gates.
```
