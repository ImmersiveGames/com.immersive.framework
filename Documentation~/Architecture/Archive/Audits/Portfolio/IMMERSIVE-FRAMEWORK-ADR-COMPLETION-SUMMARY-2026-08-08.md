# Immersive Framework — ADR Completion Summary

> Current operational reconciliation after Player serialization closure,
> IF-ADR-010 acceptance, the 010B package product-surface audit and the
> 2026-08-09 ADR-002/005/008/009 package reclassification and the
> 2026-08-10 IF-ADR-009 QA-certified closure.
>
> Historical percentages remain useful only as historical planning evidence.
> They are no longer the current completion model.

**Date:** 2026-08-10  
**Status:** current operational documentation baseline  
**Mode:** documentation reconciliation from recorded package and Unity QA evidence; no runtime implementation contained in this document

## 1. Current Git baselines

```text
com.immersive.framework
  43b96a4b100b8273da1190520536007ba82dc081
  ADR-010B

QAFramework
  b6a45728285ddb2ce08269fc1f88ae3f1a4235e4
  P0 — Serialized Player Migration Integrity

FIRSTGAME / planet-devourer
  796618243c3ca76f70d582f38475320c6461420b
  Demo02 Reajuste
```

The repositories are treated as read-only evidence sources.

Package changes from this review are documentation-only and are delivered as an
external ZIP.

## 2. Completion model correction

The previous portfolio rebaseline used:

```text
architecture
runtime
product/authoring
QA
FIRSTGAME
```

inside one percentage.

That model is retired for current progression because FIRSTGAME is not a
technical dependency of framework correctness.

The current status model is:

```text
Technical Status
  architecture/contracts
  runtime
  relevant technical QA

Product Surface Status
  official package authoring surface
  configuration/validation/diagnostics
  technical inspectability

Consumer UX Evidence
  separate real-game observation in FIRSTGAME
```

Rules:

```text
FIRSTGAME does not reduce Technical Status.
FIRSTGAME is not a technical acceptance blocker.
Missing FIRSTGAME evidence does not make framework functionality incomplete.

QA proves deterministic technical contracts.
QA does not synthetically certify Inspector UX.

Product friction found in FIRSTGAME may justify future package improvement,
but that improvement is a new product finding, not retroactive proof that the
framework was technically non-functional.
```

## 3. Historical percentages

Historical planning values are preserved for traceability only.

```text
Historical planning average
  84.6%

Former five-dimension evidence-backed maturity
  72.1%
```

The 72.1% value included FIRSTGAME as 15% of completion and therefore must not be
used as the current framework completion percentage.

Do not compare new technical/package progress directly against that number.

No replacement global percentage is introduced by this document.

Use explicit ADR status/disposition instead.

## 4. Player serialization integrity

P0 status:

```text
TECHNICALLY CLOSED
```

Accepted serialized command identities:

```text
OpenJoining                   = 10
CloseJoining                  = 20
30                            = retired / unsupported
RequestJoin                   = 40
RequestDefaultActorSelection  = 50
```

Do not:

```text
restore SetCapacity
map retired 30 to a new command
restore separate PlayerProvisioningProfile
restore per-Slot Host Provisioning override
```

The current Player Session model remains:

```text
PlayerSessionProfile
├── Supported Slots
├── Initial Joining
├── Host Provisioning
│   ├── Scene Provided
│   └── Manager Provisioned
└── Actor Resolution
```

## 5. Player technical status vs consumer UX

Current interpretation:

```text
Player technical contracts
  STRONG / QA-CERTIFIED FOR THE RECORDED PHASE EVIDENCE

serialized migration integrity
  CLOSED

package public surface
  IMPLEMENTED

FIRSTGAME current Player authoring
  separate redesign / consumer UX evaluation

FIRSTGAME technical gate
  NO
```

The Player QA certification record remains careful not to invent an unexecuted
post-patch one-button Unity result.

Existing phase evidence and focused serialization evidence remain the documented
technical basis.

## 6. ADR-010 disposition

```text
IF-ADR-010 — Editor and Inspector Product Surface Authority
  ACCEPTED

IF-ADR-010A — Product Surface Standard
  CLOSED

IF-ADR-010B — Current Package Surface Audit
  CLOSED

IF-ADR-010C — Canonical Editor Product-Surface QA
  CANCELLED / NOT REQUIRED
```

Current rule:

```text
Manual explicit authoring is the default.

The framework should:
  present
  organize
  explain
  validate
  diagnose

Additional authoring layers are conditional.

Synthetic UX QA is not required.
```

## 7. 2026-08-09 package reclassification

The latest package audit revisited ADR-002, ADR-005, ADR-008 and ADR-009.

Camera/ADR-004 is intentionally outside this cut.

| ADR | Current package assessment | Current package interpretation | Disposition |
|---|---:|---|---|
| IF-ADR-002 | **29/30** | Product authoring model is already represented by several valid lifecycle shapes; no generic tooling gap | No cross-cutting implementation |
| IF-ADR-005 | **29/30** | Pause/Input Gate/Reset/Restart package solution and primary product surfaces exist | Package complete for current accepted scope; technical hardening only if justified |
| IF-ADR-008 | **30/30** | Official product is Source Scene + Scene Template + non-mutating verification, not Composer/Apply | Package complete for current accepted model |
| IF-ADR-009 | **26/30 at 2026-08-09 audit** | Runtime existed; authoring/target/occurrence/release diagnostics required a narrow audit | Superseded by 2026-08-10 closure evidence |
| IF-ADR-004 | — | Camera redesign is a separate future program | DEFERRED |

The local `/30` values are audit planning assessments.

They are not a new portfolio completion formula.

For IF-ADR-009 specifically, the 26/30 entry above is historical evidence from
the 2026-08-09 audit. The focused audit subsequently found two concrete gaps,
corrected them, and the resulting boundary was certified in Unity on 2026-08-10.

## 8. ADR-002 correction

Former interpretation:

```text
recurrent feature
  -> Recipe/Profile
  -> Composer
  -> Apply/Rebuild
  -> Wizard/creation flow
```

Current accepted interpretation:

```text
manual explicit authoring
  = valid default

Recipe/Profile/Template
  = conditional

Composer
  = conditional

Apply/Rebuild
  = only for real deterministic technical materialization

Wizard
  = exceptional
```

ADR-002 is not waiting for a generic authoring implementation.

## 9. ADR-005 correction

Package audit conclusion:

```text
Pause primary surface           COMPLIANT
Activity Restart                COMPLIANT
Object Reset Group Trigger      COMPLIANT
Unity Input Gate                COMPLIANT SEMANTICALLY
```

No generic product extraction is required.

Future QA may strengthen real technical invariants, but that is system-specific
hardening rather than missing product implementation.

## 10. ADR-008 correction

The former ADR described a Recipe/Composer/Apply model.

The current official lifecycle is:

```text
Physical Source Scene
        ↓
Scene Template
        ↓
consumer-created scene
        ↓
Game Application reference
        ↓
non-mutating verification
```

Therefore the old remaining items around Apply/Rebuild idempotency and managed
materialization are retired.

Persistent Content is package-complete for the current accepted composition
model.

## 11. ADR-009 closure

The focused ADR-009 audit is complete.

It found two concrete gaps:

```text
invalid Required visibility binding
  -> could continue toward commit

distinct Activity definitions sharing ActivityId
  -> collision was not rejected
```

The package correction now establishes:

```text
invalid Required binding
  -> explicit diagnostic
  -> transition rejected before commit
  -> previous Activity keeps authority

invalid Optional binding
  -> diagnostic
  -> non-mutating

stable-ID collision between distinct definitions
  -> rejected
  -> stable ID does not become runtime ownership authority
```

The audit also confirmed that occurrence/revision, replacement and disposal are
already governed by the existing serialized transaction model and
`RuntimeDefinitionToken`; no new ownership system or authoring layer was needed.

Unity QA evidence:

```text
QA_ACTIVITY_LOCAL_VISIBILITY_RULE
  Passed — 28 cases
  positive, negative, no-active, invalid, idempotent, single-owner

QA_ACTIVITY_LOCAL_VISIBILITY_LIFECYCLE
  Passed — 18 cases
  positive-single, positive-multiple, negative-single, negative-multiple,
  no-active-visible, required-invalid-blocks, optional-invalid-diagnostic,
  clear, idempotence
```

Current disposition:

```text
Architecture  Accepted
Package       Implemented
QA            Certified
FIRSTGAME     Not Applicable for current accepted boundary
Status        CLOSED — current accepted boundary
```

Detailed evidence is recorded in
`IMMERSIVE-FRAMEWORK-ADR-009-QA-CERTIFICATION-2026-08-10.md`.

## 12. Current operational ADR matrix

This matrix is intentionally qualitative.

Consumer UX Evidence is tracked separately and does not demote technical/package
status.

| ADR | Normative status | Technical / package interpretation | Current action |
|---|---|---|---|
| IF-ADR-001 | Accepted | Mature core lifecycle/runtime authority | Focused hardening only if a concrete gap exists |
| IF-ADR-002 | Accepted | Mature product authoring model; 29/30 package audit | No cross-cutting implementation |
| IF-ADR-003 | Accepted | Strong Player participation/Actor technical state | Consumer UX separate |
| IF-ADR-004 | Accepted | Camera outside current cut | DEFERRED — larger Camera redesign |
| IF-ADR-005 | Accepted | Package complete for current accepted scope; 29/30 | Only justified technical hardening |
| IF-ADR-006 | Accepted | Mature Transition/Loading runtime | Focused hardening only |
| IF-ADR-007 | Accepted | Mature readiness/reveal contract | Focused hardening only |
| IF-ADR-008 | Accepted | Package complete for current Scene Template model; 30/30 | No package implementation |
| IF-ADR-009 | Accepted | Package implemented; QA certified for current accepted boundary | CLOSED — preserve current occurrence-scoped contract |
| IF-ADR-010 | Accepted | Standard accepted; package audit closed | 010C cancelled |
| IF-ADR-011 | Accepted | Strong technical readiness/loading progress | Consumer presentation can evolve separately |
| IF-ADR-012 | Accepted | Player participation compatibility technically strong | Consumer UX separate |
| IF-ADR-013 | Accepted / Experimental | Optional narrow adapter | Demand-driven only |
| IF-ADR-014 | Accepted | Complete for current accepted identity scope | No active work |
| IF-ADR-015 | Proposed | Public Player command/observation surface technically implemented/certified for recorded evidence | Normative disposition may evolve separately; not blocked by FIRSTGAME technical proof |
| IF-ADR-016 | Accepted | Current no-Capacity Player Session model implemented | Consumer UX separate |

## 13. Current priority order

```text
1. keep documentation aligned with accepted and certified contracts
2. independent non-Player technical hardening only where a real contract gap exists
3. keep IF-ADR-004 / Camera deferred until its larger redesign begins
4. rebuild/prove current Player integration in FIRSTGAME where still pending
5. use FIRSTGAME as separate consumer UX evidence, not a technical closure gate
6. turn confirmed FIRSTGAME friction into the smallest justified package improvement
7. promote IF-ADR-013 only from real product demand
```

There is no active ADR-010C program.

There is no generic Composer/Wizard program.

There is no Persistent Content Apply/Rebuild program.

## 14. FIRSTGAME governance

FIRSTGAME is not a framework progression gate.

Use it to answer:

```text
Can a real consumer find the feature?
Can they understand the fields?
Can they configure it without hidden internal knowledge?
Is the manual sequence reasonable?
Are errors and diagnostics understandable?
Is there repetitive friction worth productizing?
```

Possible Consumer UX Evidence states:

```text
NOT EVALUATED
EVALUATED
FRICTION FOUND
```

These states do not replace technical QA.

A feature can be technically certified while Consumer UX Evidence is
`NOT EVALUATED`.

## 15. QA governance

QAFramework remains responsible for:

```text
technical contracts
negative cases
regressions
deterministic Editor-write behavior
runtime lifecycle invariants
serialization integrity
```

Do not add tests solely to prove:

```text
Inspector looks consistent
ProductHeader renders
IntentSummary renders
OnInspectorGUI executes in a synthetic GUILayout context
a direct authoring component has a Wizard/Composer
```

If Camera, Input Gate, Reset or another system later needs Editor QA, the test
must be justified by that system's real technical contract.

## 16. What should NOT be done to improve status

Do not:

```text
reintroduce Capacity
restore separate PlayerProvisioningProfile
create per-Slot Host Provisioning overrides
silently map retired serialized identities
create global managers/service locators
add runtime reflection as migration glue
create Wizard/Composer before real need is proven
add synthetic UX smokes
add validators merely to raise a score
promote experimental systems only to improve a matrix
treat FIRSTGAME absence as technical incompleteness
```

## 17. Current portfolio interpretation

```text
ARCHITECTURE
  strong

RUNTIME
  strong

PLAYER TECHNICAL QA
  strong; serialization integrity closed

PRODUCT / AUTHORING
  substantially stronger than older documents described
  ADR-010A/010B closed
  valid authoring shapes are intentionally heterogeneous

QA
  remains system-specific
  no UX certification program

FIRSTGAME
  consumer UX laboratory
  separate from technical completion

CAMERA
  deliberately deferred from the current package progression cut
```

## 18. Suggested next commit message

```text
Rebaseline ADR documentation after product-surface audit
```
