# Immersive Framework — ADR Completion Summary

Date: 2026-08-06  
Package baseline: `9ed698e55b48077c54be5056c6951b7e52dac51b`  
QA baseline: `0521d1f1804dff2806e06b1e095d47023a062b9e`  
FIRSTGAME baseline: `e551643ce1b154fdb2744f97b039b4ce73bc6bf5`  
Portfolio average: **75.0%**

## Important baseline changes

1. The package HEAD changed from `a7b915c` to `9ed698e`.
2. IF-ADR-015 was added and is currently `Proposed`; its official package product surface is not shipped.
3. The latest package cut adds typed Activity readiness supersession for Route-authority replacement. This improves IF-ADR-006 and IF-ADR-007 and addresses the recent replacement/wait failure classification.
4. QAFramework was cleaned/reorganized at `0521d1f`. Historical smoke results are not counted as current certification unless the corresponding canonical test remains registered and is rerun.
5. FIRSTGAME Demo03 advanced to `e551643` and now provides active consumer evidence for local multiplayer controls/status UX. It remains consumer evidence, not the permanent framework implementation.

## Method

Each percentage considers:

```text
Normative contract and architectural clarity   20%
Runtime implementation                         30%
Authoring, diagnostics and documentation        20%
Current QA evidence                             15%
Current FIRSTGAME consumer evidence             15%
```

The percentages are planning estimates. They are deliberately reduced when code is
present but the current QA harness, product surface, or consumer proof is incomplete.

## ADR matrix

| ADR | Decision | Normative status | Completion | Current classification |
|---|---|---:|---:|---|
| IF-ADR-001 | Core Lifecycle and Runtime Authority | Accepted | **88%** | Substantially implemented; architectural residuals remain |
| IF-ADR-002 | Product Authoring Model | Accepted | **65%** | Partially implemented across the product portfolio |
| IF-ADR-003 | Player Participation and Actor Lifecycle | Accepted | **84%** | Runtime substantially implemented; product and hardening gaps remain |
| IF-ADR-004 | Camera Requests and Output Authority | Accepted | **78%** | Core runtime implemented; isolated product proof incomplete |
| IF-ADR-005 | Input, Pause, Gate and Reset | Accepted | **76%** | Integrated runtime exists; product extraction and negative coverage incomplete |
| IF-ADR-006 | Loading, Transition, Persistence and Diagnostics | Accepted | **88%** | Core orchestration implemented; recovery and product gaps remain |
| IF-ADR-007 | Activity Entry Readiness and Reveal Gating | Accepted | **96%** | Runtime contract complete; current QA recertification remains |
| IF-ADR-008 | Persistent Application Content Composition | Accepted | **90%** | Product model implemented; portfolio expansion and QA remain |
| IF-ADR-009 | Activity Local Visibility Rules | Accepted | **88%** | Runtime integrated; authoring and regression polish remain |
| IF-ADR-010 | Editor and Inspector Product Surface Authority | Proposed | **70%** | Broad foundation exists; decision not fully accepted or consistently applied |
| IF-ADR-011 | Participant-Aware Activity Readiness Loading Progress | Accepted | **92%** | Runtime complete; current QA and product presentation recertification remain |
| IF-ADR-012 | Activity Player Participation Profile and Readiness Compatibility | Accepted | **90%** | Contract and runtime implemented; product/QA consolidation remains |
| IF-ADR-013 | Optional Audio BGM Adapter | Accepted / Experimental | **65%** | Technical adapter exists; product promotion incomplete |
| IF-ADR-014 | Authored Definition and Stable Identity Authority | Proposed | **25%** | Decision documented; systemic migration not implemented |
| IF-ADR-015 | Player Provisioning Commands and Consumer Observation Surface | Proposed | **30%** | ADR and consumer prototype exist; official package surface not shipped |

## Priority order

### P0 — Cross-cutting architectural authority

- **IF-ADR-014 — 25%:** complete authored-reference versus stable-ID migration as one coordinated cut.

### P1 — Missing package product surface

- **IF-ADR-015 — 30%:** convert Demo03 findings into canonical typed commands, immutable observation, authoring, QA, and migration.
- **IF-ADR-002 — 65%:** apply the product model consistently beyond the currently mature composers.
- **IF-ADR-010 — 70%:** standardize guided creation, remediation, receipts, and Advanced/Debug.

### P2 — Runtime hardening and current QA recertification

- IF-ADR-003 Player provisioning hardening, Leave/disconnect boundaries, and current public-only QA.
- IF-ADR-004 Camera priority/release/override negative matrix.
- IF-ADR-005 Gate/Pause/Reset/Restart terminal cleanup matrix.
- IF-ADR-006/007 replacement, cancellation, supersession, reveal, and cleanup matrix.

### P3 — Product demonstrations and promotion

- Dedicated Player Camera, Camera Override, Reset/Restart, Pause, and Transition/Loading demonstrations.
- IF-ADR-013 BGM FIRSTGAME demonstration and promotion decision.

## Portfolio interpretation

```text
Core runtime architecture
  strong and mostly implemented

Readiness/loading
  contractually mature; latest Route replacement fix incorporated

Player lifecycle
  technically strong; canonical consumer command/observation surface missing

Product authoring
  proven in selected systems but inconsistent across the portfolio

QA
  undergoing canonical reorganization; requires recertification

Stable identity
  largest unresolved cross-cutting architectural migration
```
