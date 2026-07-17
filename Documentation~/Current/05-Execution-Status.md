# 05 — Execution Status

Status: Canonical operational source  
Last updated: 2026-07-16

## Read-only baseline

| Repository | Commit | Meaning |
|---|---|---|
| `ImmersiveGames/com.immersive.framework` | `385c957a8fefb53f0daf395c662ffa7d5fedc996` | selected safe package source baseline |
| `rinnocenti/QAFramework` | `993f8e698edb8c826054c9f8faa8bd344fbc8013` | selected safe QA source baseline |

Package version:

```text
1.0.0-preview.15
```

This records source identity only. No Unity import, compile or runtime PASS is claimed by this inspection.

## Canonical P3 hygiene gate

| Cut | Source status | Validation status |
|---|---|---|
| H0 Foundation alignment | Implemented | QA import/compile pending |
| H1 canonical decision/docs | Implemented | Static review complete |
| H2A F49/F51/F52 removal | Implemented | Framework/QA compile pending |
| H2B Slot declaration removal | Implemented | Host/Slot QA pending |
| H2C provisioning/InputMode migration | Implemented | Focused QA pending |
| H3 QA cleanup | Implemented in source | Full QA import pending |
| H4 factories/validation/diagnostics | Implemented in source | Runtime confirmation pending |
| H5 clean P3 regression | Pending | P3K.7I 16/16 required |
| H6 FIRSTGAME migration | Superseded by ordered P3M6 integration proof | Not started |

## Scene Local Player sequence

| Cut | Status | Evidence / next action |
|---|---|---|
| P3M0 source baseline freeze | Complete by documentation patch | exact package/QA commits recorded; Unity PASS not inferred |
| P3M1 architecture and docs | Complete by documentation patch | ADR-PROD-0013 accepted and related ADRs reconciled |
| P3M2 PreAuthored consumer decoupling | Active | prepare package + QA code zip |
| P3M3 destructive PreAuthored removal | Ordered | starts only after zero external consumers |
| P3M4 Scene Local Player Admission promotion | Ordered | requires complete runtime/authoring/release unit |
| P3M5 QA proof | Ordered | requires package implementation |
| P3M6 FIRSTGAME proof | Blocked | requires P3M5 PASS |
| P3M7 sample/docs | Blocked | requires P3M6 product proof |

## Next technical cut

```text
P3M2 — Decouple Camera, shared Editors and QA from PreAuthoredPlayerComposer
```

Required output:

```text
one package zip
one QA zip when QA files change
relative paths preserved
created / altered / removed manifest
no Git write
```

## H5 manual validation remains required

1. Import and compile Framework and QAFramework.
2. Run the canonical P3 aggregate command.
3. Run the alternative PreAuthored smoke separately while it still exists.
4. Confirm `[P3K7I_PUBLIC_DEFAULT_ACTOR_SELECTION_SMOKE] status='Passed' cases='16'` inside the canonical result.
5. Do not use the PreAuthored result as canonical join evidence.

## Rollback policy

Fix defects inside the owning cut. Never restore removed APIs, shims, aliases, fallback discovery or pre-authored Slot identity.
