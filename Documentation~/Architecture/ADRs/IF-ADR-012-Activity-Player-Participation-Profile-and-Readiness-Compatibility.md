# IF-ADR-012 — Activity Player Participation Profile and Readiness Compatibility

Status: Accepted  
Last updated: 2026-08-09  
Implementation classification: **Contract/runtime substantially implemented; canonical Player participation integration QA certified; product consolidation remains**  
Related decisions: IF-ADR-003, IF-ADR-007, IF-ADR-010, IF-ADR-015, IF-ADR-016

## Context

Activities need reusable Player participation intent that can express projected Slots, readiness requirements and compatibility without duplicating Session/runtime rules in each scene.

## Decision

Activity Player participation is authored through the approved Activity/Route policy surface and resolves into one normalized effective policy with provenance. Runtime uses explicit Slot/Player/Actor evidence and publishes requested/effective state and diagnostic reasons. Invalid or incompatible states fail explicitly.

Activity participation does not own or silently mutate Player Session configuration. It consumes the Session established through IF-ADR-016.

## Session boundary

```text
PlayerSessionProfile
  owns Supported Slots
  owns Initial Joining
  owns Session Host Provisioning
  owns Actor Resolution

Activity Player policy
  projects/qualifies current Session Slots
  defines participation/readiness intent
  does not replace Session provisioning
  does not create Capacity
```

## Architectural constraints

- Runtime authority is scoped, typed and lifetime-explicit.
- Required invalid configuration fails explicitly and diagnostically.
- Consumer code does not depend on internal runtime modules, reflection, object-name inference or implicit global lookup.
- Editor tooling is idempotent and non-destructive.
- QA proves technical contracts; FIRSTGAME proves real consumer usability.
- Activity/GameFlow tests may consume a stable Player fixture but must not become the owner of Player Session setup.

## Current implementation coverage

The package provides normalized Player participation/readiness integration, explicit Slot projection, requested/effective evidence, compatibility diagnostics and occurrence-aware runtime behavior. Manager-Provisioned lifecycle reconciliation respects active Activity occurrences, and Scene-Provided/Manager-Provisioned both feed the same participation/readiness contract.

## Current QA evidence — certified 2026-08-09

The canonical Player QA run includes an explicit Participation phase and completed:

```text
Activity Session Projection
  PASS — 30 cases

Master Player QA
  participation='PASS'
  verdict='PLAYER QA CERTIFIED'
```

The surrounding Player lifecycle used by participation is also green:

```text
Scene-Provided                 PASS
Manager-Provisioned            PASS
Actor lifecycle                PASS
Public Player Surface          PASS
```

This replaces the previous statement that the cleaned harness still required canonical Player participation revalidation.

See `../IMMERSIVE-FRAMEWORK-PLAYER-QA-CERTIFICATION-2026-08-09.md`.

## Current FIRSTGAME evidence

FIRSTGAME remains the product/usability proof. It should demonstrate participation only after the underlying Scene-Provided or Manager-Provisioned Player composition is understandable in isolation.

Participation policies are not a Host provisioning mode and should not be mixed into the first manual proof of either provisioning model.

## What remains

- Product consolidation and designer-facing clarity for participation policy authoring.
- Advanced/Debug provenance/effective-policy visibility where still incomplete.
- FIRSTGAME proof that real Activity participation can be configured without framework-internal knowledge.
- Additional focused QA only for uncovered policy variants or new regressions.
- Clarification of any future policy changes that may apply to an active Activity versus requiring reentry/restart.

## Completion criteria

- One normalized effective participation policy is the runtime input.
- Provenance and requested/effective differences are visible where needed.
- Invalid compatibility never falls back silently.
- Current QA and FIRSTGAME prove the same official package surface.
