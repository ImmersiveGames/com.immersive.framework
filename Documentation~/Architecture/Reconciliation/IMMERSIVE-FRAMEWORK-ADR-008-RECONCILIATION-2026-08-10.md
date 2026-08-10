# Immersive Framework - ADR-008 Reconciliation

Date: 2026-08-10  
ADR: `IF-ADR-008 - Persistent Application Content Composition`  
Repository: `com.immersive.framework`  
Branch inspected: `master`  
Package HEAD inspected: `baa5b00a004e81aec6f0080395cc2b8621d3d22c`

---

## 1. Executive result

ADR-008 is already aligned with the current package product model.

The inconsistency was in `IF-TRACK-Framework.md`, which still described
Persistent Content as partial work with approximately 15% technical remainder
and treated Scene Template integration/hardening as the highest active package
priority.

That tracking state does not match the accepted ADR or the current package
surface.

### Reconciled statuses

| Surface | Status | Interpretation |
|---|---|---|
| ADR documentation | ACCEPTED / RECONCILED | The current normative decision matches the package boundary. |
| Package implementation | IMPLEMENTED | The accepted source Scene + Scene Template + consumer-owned Scene model exists. |
| Product surface | IMPLEMENTED | The official Scene Template product surface is present. |
| Technical QA | Not applicable by default | Add focused QA only for a concrete deterministic pipeline contract or reproduced regression. |
| FIRSTGAME / Stage B | Not applicable as a technical closure gate | Consumer UX evidence may be gathered separately and may identify a product issue without reopening Stage A by default. |
| Disposition | Stage A closed | No active package implementation is justified by current evidence. |

Reconciled planning state:

```text
Stage A estimate       100%
Technical remaining      0%
Portfolio estimate     100%
Attention now          None
```

---

## 2. Why reconciliation was required

The accepted ADR states that Persistent Content is a Class B reusable Scene
Template flow whose physical source Scene owns the authored composition.

The tracker still described ADR-008 as:

```text
Package / product surface: Partial current tracking
Technical QA:              Partial
FIRSTGAME / Stage B:       Partial
Disposition:               Scene Template integration/hardening remains
Stage A estimate:          85%
Technical remaining:       15%
Attention now:             Highest
```

Keeping that state would create false technical debt and could drive work that
the accepted ADR explicitly rejects, such as speculative Composer,
Apply/Rebuild, hidden regeneration or hardening without a demonstrated failure.

The tracker is subordinate to accepted ADRs and reconciliation records, so it
must reflect the current accepted package boundary rather than preserve an older
planning assumption.

---

## 3. Package evidence inspected

The current package exposes the official Persistent Content authoring surface:

```text
Editor/SceneTemplates/PersistentContent/
  PersistentContentTemplateSource.unity
  ImmersivePersistentContent.scenetemplate
  PersistentContentSceneTemplatePipeline.cs
```

The evidence supports the ADR boundary:

```text
package-owned physical source Scene
        ↓
Unity Scene Template
        ↓
consumer-owned generated Scene
        ↓
direct user editing
        ↓
non-mutating package verification
```

The Scene Template references its source Scene and the dedicated template
pipeline.

`PersistentContentSceneTemplatePipeline` explicitly treats pre-instantiation as
non-mutating. After instantiation it validates the resulting Scene and reports
the result instead of silently rebuilding, repairing, assigning or saving
consumer content.

This is the product contract ADR-008 currently describes.

No package code change is required by this reconciliation.

---

## 4. QA evidence and disposition

This reconciliation does not open a broad QA campaign and does not treat the
absence of a dedicated Scene Template UI smoke as missing architecture.

ADR-008 already limits future QA to deterministic technical contracts where a
real regression risk or failure exists.

Legitimate future QA targets include a reproduced risk in:

```text
template verification
required reference validation
non-mutating behavior
explicit failure reporting
```

A focused QA cut becomes required when one of those contracts is opened by a
concrete defect, regression or newly accepted invariant.

QA must not be created merely to improve a maturity percentage or to simulate
Unity-internal Template UI workflow.

---

## 5. FIRSTGAME / Stage B disposition

FIRSTGAME may evaluate real consumer usability, including:

```text
template discoverability
source-versus-consumer ownership clarity
scene-reference workflow clarity
validation-message usefulness
```

Those observations are valuable product evidence, but they are not a technical
completion gate for the current ADR-008 boundary.

A future real-consumer issue may justify documentation, UX or product-surface
work. It does not automatically imply that the package needs a Composer,
Apply/Rebuild lifecycle or a different runtime authority model.

---

## 6. Reconciled tracker state

The ADR status row becomes:

```text
Architecture:            ACCEPTED / RECONCILED
Package / product:       IMPLEMENTED for current accepted product model
Technical QA:            Not applicable by default
FIRSTGAME / Stage B:     Not applicable as a technical closure gate
Current disposition:     Stage A closed; reopen only on concrete contract failure
```

The planning row becomes:

```text
ADR                      008
Stage A estimate         100%
Technical remaining      0%
Portfolio estimate       100%
Attention now            None
Concrete next work       No active package work; reopen only on concrete contract failure.
```

ADR-008 is also removed from the focused active QA-gap list and from the highest
technical-priority position.

---

## 7. What remains closed

The following are closed for the current accepted model:

```text
Scene Template as the official reusable authoring product
physical source Scene as composition owner
consumer ownership of instantiated Scenes
direct manual editing after instantiation
non-mutating verification
no mandatory Composer
no mandatory Apply/Rebuild
no managed technical-slot materializer
no hidden Scene repair or save
no speculative migration layer
```

These must not be reopened merely to increase apparent framework sophistication
or to satisfy a historical planning percentage.

---

## 8. What remains intentionally open

ADR-008 may be reopened only when evidence justifies it, including:

- the template cannot instantiate the advertised valid composition;
- validation accepts a concrete invalid required topology;
- verification mutates or overwrites consumer-owned content;
- a repeated migration problem becomes measured rather than hypothetical;
- direct Scene authoring proves materially error-prone despite the existing
  template and diagnostics;
- a newly accepted technical contract requires stronger tooling.

Until then there is no active ADR-008 implementation backlog.

---

## 9. Required correction

This reconciliation requires documentation/tracking corrections only.

### Edited

```text
Documentation~/Architecture/ADRs/
  IF-ADR-008-Persistent-Application-Content-Composition.md

Documentation~/Architecture/Tracking/
  IF-TRACK-Framework.md
```

### Created

```text
Documentation~/Architecture/Reconciliation/
  IMMERSIVE-FRAMEWORK-ADR-008-RECONCILIATION-2026-08-10.md
```

### Removed

```text
none
```

The ADR edit updates provenance and package baseline only. Its accepted technical
decision is unchanged.

The tracker edit removes stale active work and points to this reconciliation
record.

---

## 10. Scope of this reconciliation

### Objective

Reconcile ADR-008 tracking with the accepted Persistent Content package model
and prevent stale planning data from reopening speculative implementation work.

### In scope

- verify the accepted ADR against current package product surfaces;
- update ADR provenance to the inspected package baseline;
- reconcile the current tracker row and planning estimate;
- remove ADR-008 from active focused QA-gap tracking;
- record explicit reopen conditions;
- create a durable reconciliation record.

### Out of scope

- runtime changes;
- new Editor tooling;
- Composer or Authoring Component creation;
- Apply/Rebuild materialization;
- migration automation;
- new validators without a demonstrated contract gap;
- a new QA campaign;
- FIRSTGAME implementation work.

### Type

```text
documentation / technical reconciliation
```

### Product surface affected

No runtime or authoring behavior changes.

The official product surface remains:

```text
Create Scene from Persistent Content template
        ↓
Save consumer Scene
        ↓
Edit normally
        ↓
reference Scene from Game Application
        ↓
package validation reports contract violations
```

---

## 11. Validation and smoke expectation

No Unity runtime smoke is required for this documentation-only correction.

Static reconciliation checks must prove:

- ADR-008 points to this reconciliation record;
- ADR-008 package baseline matches the inspected package baseline;
- tracker marks ADR-008 `ACCEPTED / RECONCILED`;
- tracker marks its package/product surface as implemented;
- Stage A is `100%` with `0%` technical remainder;
- attention is `None`;
- ADR-008 is not presented as an active focused QA gap;
- no runtime, Editor or ProjectSettings file is changed by this cut.

A future Unity/QA smoke is justified only when a concrete deterministic contract
or regression is opened.

---

## 12. Acceptance criteria

### Technical

- package implementation remains unchanged;
- accepted ADR-008 authority remains unchanged;
- tracker no longer reports ADR-008 as partial or highest priority;
- tracker contains no synthetic 15% technical remainder for ADR-008;
- no silent repair/materialization requirement is introduced;
- reopen conditions are explicit and evidence-driven.

### Product

- the Scene Template remains the official reusable product surface;
- consumer ownership and direct Scene editing remain explicit;
- no extra Composer/Apply/Rebuild workflow is introduced;
- FIRSTGAME consumer evidence remains separate from Stage A technical closure.

---

## 13. Architectural and usability gain

### Architectural gain

The reconciliation removes false technical debt and restores the intended
authority chain:

```text
accepted ADR
  ↓
current package evidence
  ↓
reconciliation record
  ↓
mutable tracker
```

It also prevents historical materialization assumptions from driving new package
architecture without evidence.

### Usability gain

The official workflow stays Unity-native and direct:

```text
Template -> consumer Scene -> edit -> reference -> validate
```

The designer does not acquire an unnecessary second authoring lifecycle solely
because an older track entry was stale.

---

## 14. Final architecture rule

```text
Persistent Content remains Stage A closed under the accepted boundary:

package-owned source Scene
+ Unity Scene Template
+ consumer-owned generated Scene
+ direct editing
+ non-mutating verification

Reopen ADR-008 only when concrete evidence demonstrates that this boundary fails
or a newly accepted contract requires a different product surface.
```

---

## 15. Suggested commit

```text
docs: reconcile ADR-008 tracking with current persistent content model
```
