# Proposed ADR-014 Addition — Current Audit and What Remains

The following section is intended for review before being appended to IF-ADR-014. It is not applied to Git.

---

## Current authority audit — 2026-08-06

The IF-ID-01 static source audit confirms that the target authority model is not yet operationally consistent.

Positive foundations already exist:

- Route and Activity authoring/request surfaces carry typed asset references;
- Route and Activity runtime state retain exact authored references;
- `RouteId` and `ActivityId` are typed stable identity primitives;
- Activity readiness occurrences combine exact Activity reference and occurrence sequence;
- project-wide duplicate-ID detection exists.

The principal remaining inconsistency is:

```text
typed reference enters runtime
  -> lifecycle or ownership reduces it to stable-ID equality
```

Critical affected areas include:

- Route active-target and idempotence decisions;
- Route event publication and previous-scope cleanup;
- Activity active-target and previous-Activity finalization;
- readiness wait ownership and supersession;
- Route and Activity runtime-content owner construction;
- host and cross-module Route-context synchronization.

Editor validation also currently mixes definition-local findings with project-wide identity audit findings. An explicit regenerate-existing-ID remediation action was not identified in the inspected Route and Activity editors.

## What remains

### IF-ID-02 — Identity vocabulary and QA baseline

- Replace ambiguous `HasSameIdentity` vocabulary with explicit stable-ID terminology.
- Establish QA fixtures for different authored references sharing one stable ID.
- Prove rename/move preservation and explicit regeneration.
- Preserve production behavior until the baseline is reviewable.

### IF-ID-03 — Route reference authority

- Use exact `RouteAsset` reference for active-target and idempotence decisions.
- Align Route events, RuntimeHost synchronization and ActivityFlow Route context.
- Preserve deterministic cleanup through an explicit temporary ownership compatibility boundary.

### IF-ID-04 — Activity reference and occurrence authority

- Use exact `ActivityAsset` reference for authored-definition equality.
- Use occurrence/revision/transaction evidence for one concrete execution.
- Align active-target, finalization, restart, reentry and readiness supersession.

### IF-ID-05 — Runtime ownership boundary

- Decide which lifetime dimension belongs in Route and Activity owner identity.
- Prevent different references with the same stable ID from sharing release authority.
- Preserve stable IDs as boundary evidence without making them the sole operational owner.
- Prove acquisition, out-of-order release, restart, reentry and stale-handle behavior.

### IF-ID-06 — Validation scopes and product UX

- Separate definition-local, Game Application and project audit findings.
- Add deep links and explicit conflict scope.
- Add an explicit `Regenerate Stable ID...` remediation flow.
- Keep validators non-mutating and forbid automatic identity changes.

### IF-ID-07 — Scoped external resolution

- Add a Game Application-scoped catalog only when a real save or integration boundary requires it.
- Fail explicitly on missing or ambiguous resolution.
- Do not introduce global lookup, name fallback or service locator behavior.

### IF-ID-08 — QA and FIRSTGAME completion proof

- QA must prove Route/Activity collisions, lifecycle, readiness and ownership semantics.
- FIRSTGAME must prove duplicate, diagnose, regenerate, validate, run, rename and move as a real user workflow.
- Consumer prototypes must not become permanent identity authority.

## Completion estimate after IF-ID-01

The ADR remains approximately **25% complete**:

```text
Decision and target authority model        substantially documented
Typed authoring/reference foundations      present
Runtime reference authority migration      not complete
Ownership migration                        not complete
Validation scope/product UX                not complete
Focused current QA proof                   not identified
FIRSTGAME product proof                    not complete
```

The audit improves implementation readiness but does not increase runtime completion by itself.
