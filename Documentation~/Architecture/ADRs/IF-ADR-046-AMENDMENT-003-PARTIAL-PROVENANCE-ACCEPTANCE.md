# IF-ADR-046 Amendment 003 — Aceitação explícita de proveniência parcial

Status: **Accepted**
Decision authority: **User acting as the responsible project authority; explicit acceptance in the task instruction dated 2026-10-12**
Date: **2026-10-12**
Amends: [IF-ADR-046 — Stable Manager-Provisioned Player Join Boundary](IF-ADR-046-Stable-Manager-Provisioned-Player-Join-Boundary.md), §7 gates 4 and 7
Related route: [IF-PLAN-ADR-046 v3](../Plans/IF-PLAN-ADR-046-STABLE-MANAGER-PROVISIONED-PLAYER-JOIN.v3.md), Stage E
Evidence status: [Player recertification matrix](../Reconciliation/IF-PLAYER-FULL-RECERTIFICATION-2026-10-11.md)

## Context

The functional certification work for the bounded `PlayerSessionJoinTrigger` closure is reported complete in Unity 6000.5.0f1. The reports cover Framework EditMode 292/292 (PlayerParticipation 101/101), post-removal QA IF-ADR-042/043/044 22/22, corrected Slice 1 8/8 with `BaselineRestored`, Leave/Rejoin occurrence, and FirstGame Local Multiplayer Join/Leave/Rejoin. Execution-time Framework/QA SHAs and full provenance were not captured for those runs.

The supplied ZIP was available at `F:\Download\IF-ADR-046-Player-Stable-Evidencias.zip` and is archived with its unchanged members under `QAFramework/Documentation~/Certification/PlayerJoin/Evidence/PlayerStable-2026-10-12/originals/`. The FirstGame raw log is 339106 bytes / 886 lines; its SHA-256 `b57c06ba57f9bc0df6312306faa76abc4e8728dc978d2aa470e62b3487f9919e` was recomputed from the extracted member and matches the supplied `SHA256SUMS.txt`. The Test Runner screenshot and QA transcript hashes also match that list. These checks verify artifact bytes only, not execution time or source SHA.

IF-GOV-001 §2 defines the compatibility expectation of `Stable`; §7 requires architecture-governance handling for changes to `FrameworkApiStatus` meanings and explicit ADR/migration for breaking Stable changes. This amendment does not change those meanings, weaken compatibility, or change Runtime behavior. It adopts a narrowly scoped exception to IF-ADR-046's evidence-provenance gates. IF-GOV-001 does not prohibit this ADR-specific evidence disposition; Stable's compatibility meaning and its guarantees remain unchanged.

## Accepted amendment

For the current certification decision of the bounded `PlayerSessionJoinTrigger` closure only:

1. IF-ADR-046 §7.4's immutable-SHA/raw-output/timestamp/execution-provenance condition may be considered satisfied for a maturity decision by **explicit acceptance of partial provenance**, without claiming execution against a known Framework or QA SHA. Record each reported result, its source, known artifact metadata, and each unknown field. A present-day checkout SHA must never be substituted for an execution SHA.
2. IF-ADR-046 §7.7 and IF-PLAN-ADR-046 v3 Stage E's same-candidate association are waived for these already completed runs by this explicit decision. The authority has accepted the residual risk that the exact source bytes imported, compiled and exercised are unverified. This does not state that they were the current or any named candidate.
3. The unchanged FirstGame raw log is archived under `QAFramework/Documentation~/Certification/PlayerJoin/Evidence/PlayerStable-2026-10-12/originals/`; its SHA-256, byte length and line count were recomputed and checked against the supplied `SHA256SUMS.txt`. This verifies the artifact bytes, not the execution timestamp or source SHA. The functional interpretation remains an observation of user-supplied execution output. The EditMode screenshot and QA terminal transcription are archived with matching member hashes; the QA transcription is not represented as a full raw runner log.
4. All other IF-ADR-046 gates remain unchanged. I4 remains accepted by composed evidence with its existing P1/P2 and LEASE qualifications. Physical compensation remains accepted by composed evidence under Amendment 002. Session-commit reachability remains closed as documented. The one-public-Join migration, package-owned coverage, mapping/fail-closed checks, dependency/version review, and remaining accepted evidence are not reopened or waived by this amendment.
5. This exception applies only to the above bounded Player Join maturity decision and the identified existing evidence packet. It does not establish a general provenance policy, validate future runs, or promote adjacent APIs or the Framework.

## Evidence packet and residual risk accepted

| Evidence | Reported result | Provenance qualification |
|---|---|---|
| Framework EditMode / PlayerParticipation | 292/292 PASS; 101/101 PASS | Test Runner screenshot archived with member hash verified; no execution SHA or timestamp. |
| QA IF-ADR-042/043/044 | 22/22 PASS; all `BaselineRestored` | Supplied terminal transcript archived with hash verified; it is a transcription, not full raw logs. No execution SHA or timestamp. |
| QA-PLAYER-SESSION-JOIN Slice 1 after bridge correction | 8/8 PASS; `BaselineRestored` | Supplied terminal transcript archived with hash verified; no contemporaneous immutable Framework SHA, timestamp or full raw QA log. |
| QA-PLAYER-LEAVE-REJOIN-OCCURRENCE | PASS; `BaselineRestored` | User-supplied terminal transcript; raw scenario log and execution provenance partial. |
| FirstGame Local Multiplayer | P1/P2 Join, Host/PlayerInput correlation, Actor readiness/materialization, GameplayReady, Leave and Rejoin reported | Raw log preserved unchanged at `QAFramework/Documentation~/Certification/PlayerJoin/Evidence/PlayerStable-2026-10-12/originals/FirstGame-LocalMultiplayer-console.txt`; SHA-256 and size verified against the supplied manifest. Execution-time Framework SHA and timestamp remain unknown. |
| I4 | Accepted by composite evidence | Preserve P1/P2 static-review residuals and historical I4.2 LEASE `BLOCKED 3/3`; do not relabel as direct test PASS. |
| Physical compensation | Accepted by composite evidence under Amendment 002 | Not an integrated callback-then-throw Play Mode PASS; retain Amendment 002's residual risks. |
| Session commit failure | Unreachable through valid public boundary under documented synchronous Unity assumptions | Static reachability decision remains bounded by those assumptions. |

The user, acting as the responsible project authority, explicitly accepts the risk that reported test outcomes cannot be cryptographically tied to Framework/QA source bytes executed at the time. The evidence bundle preserves screenshots, terminal transcriptions, the raw FirstGame log, README and hashes. Acceptance does not backfill missing execution SHAs, timestamps or dirty-state records.

## Accepted disposition

This amendment is explicitly accepted by the responsible project authority. The authority also records **APPROVED — Player Join Stable** for only the closure enumerated in the IF-ADR-046 Stable-promotion decision record, based on the evidence packet above and the stated residual risk. The source maturity attributes are updated by the accompanying decision implementation.

This amendment changes no Runtime behavior, QA fixture, consumer project, package version, release or tag.
