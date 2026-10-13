# Plans

Current work and next actions live on the mutable tracker:

- [IF-TRACK-Framework](../Tracking/IF-TRACK-Framework.md)

Active immutable distribution plan:

- [OpenUPM Signed Distribution v2](IF-PLAN-OPENUPM-SIGNED-DISTRIBUTION.v2.md)
- [OpenUPM Signed Distribution — implementation plan](IF-PLAN-OPENUPM-SIGNED-DISTRIBUTION-IMPLEMENTATION-2026-09-24.md)

Proposed master stabilization route — analysis/review only; implementation is
blocked until explicit integrated-plan approval:

- [IF-ADR-048 — 1.1.0 Stable Readiness Master Plan v1](IF-PLAN-ADR-048-STABLE-READINESS.v1.md)

Accepted architecture implementation route (implementation pending):

- [IF-ADR-045 — Runtime-Safe Scene References v1](IF-PLAN-ADR-045-RUNTIME-SAFE-SCENE-REFERENCES.v1.md)

Approved convergence route; execution remains task-gated:

- [IF-ADR-046 — Stable Manager-Provisioned Player Join Boundary v3](IF-PLAN-ADR-046-STABLE-MANAGER-PROVISIONED-PLAYER-JOIN.v3.md)

Superseded historical routes retained for traceability:

- [IF-ADR-046 — Stable Manager-Provisioned Player Join Boundary v2](IF-PLAN-ADR-046-STABLE-MANAGER-PROVISIONED-PLAYER-JOIN.v2.md)
- [IF-ADR-046 — Stable Manager-Provisioned Player Join Boundary v1](IF-PLAN-ADR-046-STABLE-MANAGER-PROVISIONED-PLAYER-JOIN.v1.md)

Superseded distribution plan and execution record:

- [OpenUPM Public Package Distribution v1](IF-PLAN-OPENUPM-PUBLIC-DISTRIBUTION.v1.md)
- [OpenUPM Public Package Distribution — implementation plan](IF-PLAN-OPENUPM-PUBLIC-DISTRIBUTION-IMPLEMENTATION-2026-09-24.md)

This plan defines the approved cross-repository distribution route. Execution
progress remains owned by the Tracker and does not mutate the plan body.

Files in this directory that explicitly identify themselves as
`Historical / Superseded` are retained only as execution history and must not
guide current implementation. Closed/superseded planning records should migrate
to `Architecture/Archive/Plans` when touched for repository topology cleanup.

Canonical current authority remains:

```text
ADRs      -> decisions
Tracker   -> current status and next work
Guides    -> current usage
Archive   -> historical records
```

The proposed IF-ADR-048 master plan organizes operational investigations and
later certification. It does not replace the ADR or tracker, close a readiness
phase, or authorize implementation.
