# Immersive Framework — Player / Session / Activity ADR-Reconciled Matrix

**Date:** 2026-08-08  
**Status:** ADR reconciliation baseline complete; runtime/source confrontation not started  
**Project:** Immersive Framework 1.1 / Unity 6.5

> This document replaces the *conceptual-only* interpretation of the 2026-08-08 Player / Session / Activity Decision Matrix for the next analysis phase. It preserves all 113 original decision IDs, applies the prior mechanical normalization, and reconciles the decisions against the ADR set available in this chat. It does **not** claim that the current package/runtime implements the reconciled shape. The next phase is explicitly source/runtime confrontation.

## 1. Source baseline

Conceptual sources:

- `IMMERSIVE-FRAMEWORK-PLAYER-SESSION-ACTIVITY-DECISION-MATRIX-2026-08-08.md`
- `IMMERSIVE-FRAMEWORK-NORMALIZED-PLAYER-DECISION-CLASSIFICATION-2026-08-08.md`
- `IMMERSIVE-FRAMEWORK-TRANSVERSAL-ARCHITECTURE-INVARIANTS-AND-DECISION-CLASSIFICATION-2026-08-08.md`

ADR reconciliation baseline:

- IF-ADR-001 — Core Lifecycle and Runtime Authority — **Accepted**
- IF-ADR-002 — Product Authoring Model — **Accepted**
- IF-ADR-003 — Player Participation and Actor Lifecycle — **Accepted**
- IF-ADR-005 — Input, Pause, Gate and Reset — **Accepted**
- IF-ADR-006 — Loading, Transition, Persistence and Diagnostics — **Accepted**
- IF-ADR-007 — Activity Entry Readiness and Reveal Gating — **Accepted**
- IF-ADR-010 — Editor and Inspector Product Surface Authority — **Proposed**
- IF-ADR-011 — Participant-Aware Activity Readiness Loading Progress — **Accepted**
- IF-ADR-012 — Activity Player Participation Profile and Readiness Compatibility — **Accepted**
- IF-ADR-014 — Authored Definition and Stable Identity Authority — **Accepted**
- IF-ADR-015 — Player Provisioning Commands and Consumer Observation Surface — **Proposed**

The ADR completion summary/tracker is used only to distinguish accepted/proposed/closed boundaries; implementation percentages are **not** used as proof of runtime conformance in this document.

## 2. Reconciliation classifications

| Classification | Meaning for this baseline |
|---|---|
| **ALREADY DEFINED — ACCEPTED ADR** | Keep as canonical/clarifying wording; runtime audit must test conformance, not redesign it. |
| **DERIVED / ADR-ALIGNED** | Keep as derived validation/runtime rule; do not create a separate ADR decision unless runtime evidence exposes a contradiction. |
| **COMPATIBLE REFINEMENT — ADR UPDATE** | Keep in reconciled baseline; update the indicated accepted ADR before treating the exact wording as fully normative. |
| **COMPATIBLE NEW DECISION — ADR UPDATE** | Keep as a candidate domain decision; formalize through the indicated ADR amendment before implementation cuts. |
| **ALIGNED WITH IF-ADR-015 — PROPOSED** | Keep as aligned with ADR-015, but do not treat as accepted normative contract until ADR-015 is accepted/updated. |
| **CONFLICT RESOLVED — REWRITTEN** | Use the reconciled wording. The original matrix wording is superseded by accepted transition/readiness ADR semantics. |
| **DEFERRED / DO NOT GENERALIZE** | Keep out of the active Player implementation baseline except as a future cross-system review note. |
| **GLOBAL / REMOVE FROM PLAYER-SPECIFIC LAYER** | Preserve as transversal guidance, not as an independent Player-domain decision. |
| **HISTORICAL / REJECTED** | Preserve only as a regression guardrail; do not implement or reopen without new evidence. |

## 3. Critical reconciliation corrections

### 3.1 Activity authority commit is not gated by Player readiness completion

The original matrix model `Preparing → Ready → Commit` is superseded. Accepted ADR-001/006/007 semantics allow the target Activity to become current authority while its occurrence-scoped readiness remains `Preparing`. `WaitCovered`/`WaitVisible` govern presentation/capability release, not whether the target Activity has already committed as authority.

Canonical conceptual ordering for the reconciled matrix:

```text
Activity request
  → Transition Before / pre-commit checks
  → target lifecycle/materialization mutation
  → target Activity authority commits
  → target Activity is Current
  → occurrence-scoped Activity Entry Readiness
       Preparing
         → Ready
         → or typed terminal failure / invalidation / cancellation / supersession
  → presentation/capability release according to ActivityEntryReadinessPolicy
```

Consequences:

- `T02` is rewritten.
- `E03` no longer uses Activity Commit as the boundary after which late join cannot affect the initial readiness occurrence.
- `T05` no longer treats Activity Commit as proof that Player Entry Readiness was accepted.
- No second Player-specific transaction/commit authority is introduced.

### 3.2 Player Activity Profile does not own the generic Activity wait/reveal policy

The proposed Player Activity Profile may contain Player participation and Player Physical Presence intent, but `ActivityEntryReadinessPolicy` (`ObserveOnly`, `WaitVisible`, `WaitCovered`) remains directly Activity-owned under IF-ADR-007. Route defaulting of the Player Activity Profile must not duplicate that policy.

### 3.3 Join Inhibits, if adopted, are Player-admission concepts

The GameFlow Transition Gate remains internal operation state with no external lease/release ownership protocol. If the Player model adopts `Join Inhibits`, transition-related joining inhibition must be a typed contextual input to Player admission, not an alias for or lease on the Transition Gate.

### 3.4 ADR-015 is Proposed, not Accepted

Commands/observation/scoped reachability already have a substantially defined architectural shape in IF-ADR-015, but this document does not treat those items as accepted normative contract until ADR-015 is formally accepted/updated.

## 4. Complete reconciled matrix

| ID | Area | Normalized class | Reconciled decision | ADR reconciliation | ADR basis | Runtime-confrontation treatment |
|---|---|---|---|---|---|---|
| P01 | Identity | DOMAIN DECISION | `PlayerSlotId` is the stable logical identity/seat of a Player in the Session. Do not create another `LogicalPlayerId`. | **ALREADY DEFINED — ACCEPTED ADR** | IF-ADR-001 + IF-ADR-003 | Keep as canonical/clarifying wording; runtime audit must test conformance, not redesign it. |
| P02 | Ownership | DOMAIN DECISION | Player/Slot belongs to the **Session**, including when the Player joins while an Activity is running. | **ALREADY DEFINED — ACCEPTED ADR** | IF-ADR-001 + IF-ADR-003 | Keep as canonical/clarifying wording; runtime audit must test conformance, not redesign it. |
| P03 | Actor | DOMAIN DECISION | A Slot may exist with `Current Actor = none`. This does not remove the Player. | **ALREADY DEFINED — ACCEPTED ADR** | IF-ADR-001 + IF-ADR-003 | Keep as canonical/clarifying wording; runtime audit must test conformance, not redesign it. |
| P04 | Actor | DERIVED RULE | Gameplay decides when/why to select or change Actor. Framework provides state, evidence and capability. | **DERIVED / ADR-ALIGNED** | IF-ADR-003 + IF-ADR-001 | Keep as derived validation/runtime rule; do not create a separate ADR decision unless runtime evidence exposes a contradiction. |
| P05 | Host | DOMAIN DECISION | An accepted Join establishes `Slot + Player Host` at Session level, even when no Activity exists. | **COMPATIBLE REFINEMENT — ADR UPDATE** | IF-ADR-001 + IF-ADR-003 | Keep in reconciled baseline; update the indicated accepted ADR before treating the exact wording as fully normative. |
| P06 | Physical Actor | DOMAIN DECISION | Physical representation is not the identity of the Player nor of the logical Actor. | **ALREADY DEFINED — ACCEPTED ADR** | IF-ADR-001 + IF-ADR-003 | Keep as canonical/clarifying wording; runtime audit must test conformance, not redesign it. |
| S01 | GameApplication | TECHNICAL REVIEW | `GameApplication` may provide the **Default Player Session Profile**. | **COMPATIBLE NEW DECISION — ADR UPDATE** | IF-ADR-001; IF-ADR-002 for authoring intent | Keep as a candidate domain decision; formalize through the indicated ADR amendment before implementation cuts. GameApplication/Session is the accepted composition root, so this owner is compatible; the specific `Default Player Session Profile` field/asset is not yet frozen by an accepted ADR. |
| S02 | Session Profile | DOMAIN DECISION | Session creation may use the GameApplication default or an explicit override. | **COMPATIBLE NEW DECISION — ADR UPDATE** | IF-ADR-001; IF-ADR-002 for authoring intent | Keep as a candidate domain decision; formalize through the indicated ADR amendment before implementation cuts. Depends on adopting S01 and a canonical Session-profile shape. |
| S03 | Lifetime | DERIVED RULE | The Profile initializes the Session once; it does not continuously control runtime state. | **DERIVED / ADR-ALIGNED** | IF-ADR-001; IF-ADR-002 for authoring intent | Keep as derived validation/runtime rule; do not create a separate ADR decision unless runtime evidence exposes a contradiction. |
| S04 | Runtime | DERIVED RULE | After creation, Session/runtime becomes the authority for mutable state. | **DERIVED / ADR-ALIGNED** | IF-ADR-001; IF-ADR-002 for authoring intent | Keep as derived validation/runtime rule; do not create a separate ADR decision unless runtime evidence exposes a contradiction. |
| S05 | Mutation | DERIVED RULE | Capacity, Joining and equivalent mutable Session state change only through explicit requests/capabilities. | **DERIVED / ADR-ALIGNED** | IF-ADR-001; IF-ADR-002 for authoring intent | Keep as derived validation/runtime rule; do not create a separate ADR decision unless runtime evidence exposes a contradiction. |
| S06 | Route/Activity | DERIVED RULE | Route or Activity do not automatically reapply the Player Session Profile. | **DERIVED / ADR-ALIGNED** | IF-ADR-001; IF-ADR-002 for authoring intent | Keep as derived validation/runtime rule; do not create a separate ADR decision unless runtime evidence exposes a contradiction. |
| S07 | Supported Slots | DOMAIN DECISION | Session has a structural universe of supported Slot identities. | **COMPATIBLE REFINEMENT — ADR UPDATE** | IF-ADR-003; IF-ADR-012/015 provide adjacent Slot/capacity evidence | Keep in reconciled baseline; update the indicated accepted ADR before treating the exact wording as fully normative. |
| S08 | Capacity | DOMAIN DECISION | `Current Capacity` is runtime-variable and bounded by Supported Slots. | **COMPATIBLE REFINEMENT — ADR UPDATE** | IF-ADR-003; IF-ADR-012/015 provide adjacent Slot/capacity evidence | Keep in reconciled baseline; update the indicated accepted ADR before treating the exact wording as fully normative. |
| S09 | Capacity | DERIVED RULE | An Activity accepting Slots 1–4 does not automatically raise Session Capacity to 4. | **DERIVED / ADR-ALIGNED** | IF-ADR-001; IF-ADR-002 for authoring intent | Keep as derived validation/runtime rule; do not create a separate ADR decision unless runtime evidence exposes a contradiction. |
| S10 | Capacity | DERIVED RULE | Increasing or reducing Capacity must be explicitly requested. | **DERIVED / ADR-ALIGNED** | IF-ADR-001; IF-ADR-002 for authoring intent | Keep as derived validation/runtime rule; do not create a separate ADR decision unless runtime evidence exposes a contradiction. |
| S11 | Allocation | DOMAIN DECISION | Join assigns the first available Slot according to a defined allocation order/policy; the Player does not choose a Slot. | **COMPATIBLE NEW DECISION — ADR UPDATE** | IF-ADR-003; IF-ADR-012/015 provide adjacent Slot/capacity evidence | Keep as a candidate domain decision; formalize through the indicated ADR amendment before implementation cuts. First-available allocation is a Player-domain policy not explicitly frozen by the current accepted ADR set. |
| S12 | Slot stability | DOMAIN DECISION | Once assigned, Slot identity does not renumber because another Player leaves. | **COMPATIBLE NEW DECISION — ADR UPDATE** | IF-ADR-003; IF-ADR-012/015 provide adjacent Slot/capacity evidence | Keep as a candidate domain decision; formalize through the indicated ADR amendment before implementation cuts. Stable Slot identity can be adopted now, but Session Leave/disconnect lifecycle remains separately unresolved/out of ADR-015 scope. |
| PR01 | Profile | DOMAIN DECISION | Initial Player provisioning deserves a dedicated **Player Provisioning Profile**. | **COMPATIBLE NEW DECISION — ADR UPDATE** | IF-ADR-002 + IF-ADR-003 | Keep as a candidate domain decision; formalize through the indicated ADR amendment before implementation cuts. Supported by the IF-ADR-002 product model, but the exact dedicated asset is new. |
| PR02 | Composition | DOMAIN DECISION | Player Session Profile references the Player Provisioning Profile. | **COMPATIBLE NEW DECISION — ADR UPDATE** | IF-ADR-002 + IF-ADR-003 | Keep as a candidate domain decision; formalize through the indicated ADR amendment before implementation cuts. Composition shape is new and depends on the final Session-profile design. |
| PR03 | Host | DOMAIN DECISION | `Scene Provided` and `Manager Provisioned` are provisioning decisions. | **ALREADY DEFINED — ACCEPTED ADR** | IF-ADR-001 + IF-ADR-003 | Keep as canonical/clarifying wording; runtime audit must test conformance, not redesign it. |
| PR04 | Actor | DOMAIN DECISION | How the Host is obtained is separate from how the Actor is resolved. | **ALREADY DEFINED — ACCEPTED ADR** | IF-ADR-001 + IF-ADR-003 | Keep as canonical/clarifying wording; runtime audit must test conformance, not redesign it. |
| PR05 | Actor | DOMAIN DECISION | Provisioning may resolve a default Actor or leave Actor unresolved for external/gameplay resolution. | **COMPATIBLE REFINEMENT — ADR UPDATE** | IF-ADR-001 + IF-ADR-003 | Keep in reconciled baseline; update the indicated accepted ADR before treating the exact wording as fully normative. Existing lifecycle separates Actor selection; default-vs-external resolution needs explicit normative vocabulary. |
| PR06 | Lifetime | DOMAIN DECISION | The effective Player Provisioning Profile remains stable for the whole Session, including late joins. | **COMPATIBLE NEW DECISION — ADR UPDATE** | IF-ADR-001 + IF-ADR-003 | Keep as a candidate domain decision; formalize through the indicated ADR amendment before implementation cuts. Session-stable provisioning intent is compatible with Session-scoped Player authority and late joins but needs explicit amendment. |
| PR07 | Route/Activity | DERIVED RULE | Route/Activity do not automatically replace the effective Provisioning Profile. | **DERIVED / ADR-ALIGNED** | IF-ADR-001 + IF-ADR-002 + IF-ADR-003 | Keep as derived validation/runtime rule; do not create a separate ADR decision unless runtime evidence exposes a contradiction. |
| A01 | Profile | DOMAIN DECISION | The main design surface is conceptually a complete **Player Activity Profile**. | **COMPATIBLE NEW DECISION — ADR UPDATE** | IF-ADR-002 + IF-ADR-012; constrained by IF-ADR-007 | Keep as a candidate domain decision; formalize through the indicated ADR amendment before implementation cuts. Expands IF-ADR-012 from participation Profile to a complete Player Activity intent surface. |
| A02 | Contents | DOMAIN DECISION | The Player Activity Profile composes Player participation intent and Player Physical Presence intent. It does not absorb the Activity-owned reveal/wait policy from IF-ADR-007. | **COMPATIBLE NEW DECISION — ADR UPDATE** | IF-ADR-002 + IF-ADR-012; constrained by IF-ADR-007 | Keep as a candidate domain decision; formalize through the indicated ADR amendment before implementation cuts. |
| A03 | Route | DOMAIN DECISION | Route may provide a `Default Player Activity Profile` as the Player-specific authoring default. This default does **not** duplicate or own `ActivityEntryReadinessPolicy` (`ObserveOnly / WaitVisible / WaitCovered`), which remains Activity-owned under IF-ADR-007. | **COMPATIBLE NEW DECISION — ADR UPDATE** | IF-ADR-002 + IF-ADR-012; constrained by IF-ADR-007 | Keep as a candidate domain decision; formalize through the indicated ADR amendment before implementation cuts. |
| A04 | Activity | DOMAIN DECISION | Activity chooses `Inherit Route Default` or `Override`. | **COMPATIBLE NEW DECISION — ADR UPDATE** | IF-ADR-002 + IF-ADR-012; constrained by IF-ADR-007 | Keep as a candidate domain decision; formalize through the indicated ADR amendment before implementation cuts. New authoring-resolution policy; runtime must still receive one normalized effective policy per IF-ADR-012. |
| A05 | Default UX | DOMAIN DECISION | `Inherit` is the normal/default authoring behavior to avoid accidental duplicated decisions. | **COMPATIBLE NEW DECISION — ADR UPDATE** | IF-ADR-002 + IF-ADR-012; constrained by IF-ADR-007 | Keep as a candidate domain decision; formalize through the indicated ADR amendment before implementation cuts. Designer-first default, not runtime authority. |
| A06 | Override | DOMAIN DECISION | Override replaces the **whole Profile**, not individual fields. | **COMPATIBLE NEW DECISION — ADR UPDATE** | IF-ADR-002 + IF-ADR-012; constrained by IF-ADR-007 | Keep as a candidate domain decision; formalize through the indicated ADR amendment before implementation cuts. Complete override avoids field-level inheritance ambiguity; requires ADR-012 amendment. |
| A07 | Override lifetime | DOMAIN DECISION | Override applies only to that Activity. A later Activity using `Inherit` resolves the Route default again. | **COMPATIBLE NEW DECISION — ADR UPDATE** | IF-ADR-002 + IF-ADR-012; constrained by IF-ADR-007 | Keep as a candidate domain decision; formalize through the indicated ADR amendment before implementation cuts. No inheritance chain from the previously executed Activity. |
| A08 | Missing inheritance | DERIVED RULE | `Inherit` without a resolvable Route default is an authoring error. No silent fallback. | **DERIVED / ADR-ALIGNED** | IF-ADR-002 + IF-ADR-012; constrained by IF-ADR-007 | Keep as derived validation/runtime rule; do not create a separate ADR decision unless runtime evidence exposes a contradiction. |
| A09 | Occurrence | DOMAIN DECISION | Effective Player Activity Profile is resolved for the Activity occurrence and stays immutable until that occurrence ends. | **COMPATIBLE REFINEMENT — ADR UPDATE** | IF-ADR-002 + IF-ADR-012; constrained by IF-ADR-007 | Keep in reconciled baseline; update the indicated accepted ADR before treating the exact wording as fully normative. Occurrence-scoped effective intent aligns with occurrence-aware readiness/reconcile; exact mutability rules must be frozen in ADR-012. |
| A10 | Generalization | TECHNICAL REVIEW | `Inherit + deliberate Override` remains a Player Activity design pattern only. Do not promote it to a universal framework invariant without cross-system evidence. | **DEFERRED / DO NOT GENERALIZE** | IF-ADR-012 + IF-ADR-007 provide no universal inheritance rule | Keep out of the active Player implementation baseline except as a future cross-system review note. |
| W01 | Unit | DOMAIN DECISION | Participation is defined in **Slots**, not GameObjects and not only currently existing Players. | **ALREADY DEFINED — ACCEPTED ADR** | IF-ADR-001 + IF-ADR-003 | Keep as canonical/clarifying wording; runtime audit must test conformance, not redesign it. |
| W02 | Vacancy | DOMAIN DECISION | An eligible Slot remains a valid Activity participation possibility while vacant. | **ALREADY DEFINED — ACCEPTED ADR** | IF-ADR-001 + IF-ADR-003 | Keep as canonical/clarifying wording; runtime audit must test conformance, not redesign it. |
| W03 | Lifetime | DOMAIN DECISION | Who Participates remains stable during the Activity occurrence. | **COMPATIBLE REFINEMENT — ADR UPDATE** | IF-ADR-001 + IF-ADR-003 + IF-ADR-012 | Keep in reconciled baseline; update the indicated accepted ADR before treating the exact wording as fully normative. |
| W04 | Semantics | DOMAIN DECISION | Who Participates is scope/permission, not a Player lifecycle driver. | **ALREADY DEFINED — ACCEPTED ADR** | IF-ADR-001 + IF-ADR-003 | Keep as canonical/clarifying wording; runtime audit must test conformance, not redesign it. |
| W05 | Modes | DOMAIN DECISION | `All Supported Slots / Explicit Slots / No Slots`. | **COMPATIBLE NEW DECISION — ADR UPDATE** | IF-ADR-001 + IF-ADR-003 + IF-ADR-012 | Keep as a candidate domain decision; formalize through the indicated ADR amendment before implementation cuts. Exact participation mode vocabulary still needs formal ADR wording even though Slot projection exists today. |
| W06 | Default | DOMAIN DECISION | `All Supported Slots` is the normal/default choice. | **COMPATIBLE NEW DECISION — ADR UPDATE** | IF-ADR-001 + IF-ADR-003 + IF-ADR-012 | Keep as a candidate domain decision; formalize through the indicated ADR amendment before implementation cuts. UX/default choice, not an implication of current ADRs. |
| W07 | Supported universe | DERIVED RULE | Referencing an unsupported Slot is a structural authoring error. | **DERIVED / ADR-ALIGNED** | IF-ADR-001 + IF-ADR-003 + IF-ADR-012 | Keep as derived validation/runtime rule; do not create a separate ADR decision unless runtime evidence exposes a contradiction. |
| W08 | No Slots | DERIVED RULE | `No Slots` implies `Ready When=None` and `Physical Presence=No Requirement`. | **DERIVED / ADR-ALIGNED** | IF-ADR-001 + IF-ADR-003 + IF-ADR-012 | Keep as derived validation/runtime rule; do not create a separate ADR decision unless runtime evidence exposes a contradiction. |
| W09 | Join | DOMAIN DECISION | A Player may join the Session into a Slot outside the current Activity's Who Participates. | **COMPATIBLE REFINEMENT — ADR UPDATE** | IF-ADR-001 + IF-ADR-003 | Keep in reconciled baseline; update the indicated accepted ADR before treating the exact wording as fully normative. |
| W10 | Outside scope | DERIVED RULE | A Slot outside Activity scope remains valid in Session but does not participate in that Activity's readiness or physical-presence requirements. | **DERIVED / ADR-ALIGNED** | IF-ADR-001 + IF-ADR-003 + IF-ADR-012 | Keep as derived validation/runtime rule; do not create a separate ADR decision unless runtime evidence exposes a contradiction. |
| E01 | Separation | DOMAIN DECISION | `Who Participates`, `Ready When`, and `Required Coverage` are independent dimensions. | **COMPATIBLE REFINEMENT — ADR UPDATE** | IF-ADR-003 + IF-ADR-007 + IF-ADR-011 + IF-ADR-012 | Keep in reconciled baseline; update the indicated accepted ADR before treating the exact wording as fully normative. |
| E02 | Lifetime | DOMAIN DECISION | `Ready When` is exclusively an **Entry Attempt** rule. | **ALREADY DEFINED — ACCEPTED ADR** | IF-ADR-003 + IF-ADR-007 + IF-ADR-011 + IF-ADR-012 | Keep as canonical/clarifying wording; runtime audit must test conformance, not redesign it. |
| E03 | Non-regression | DERIVED RULE | After the **captured Activity Entry Readiness occurrence reaches a terminal/finalized state**, a late join does not reopen or rewrite that historical readiness occurrence. A late join may still affect the current live Activity participation projection and any current contextual Player requirements. | **CONFLICT RESOLVED — REWRITTEN** | IF-ADR-003 + IF-ADR-007 + IF-ADR-011 + IF-ADR-012 | Use the reconciled wording. The original matrix wording is superseded by accepted transition/readiness ADR semantics. |
| E04 | Evidence | DOMAIN DECISION | Activity observation includes all relevant Slots, not only ready Slots. | **COMPATIBLE REFINEMENT — ADR UPDATE** | IF-ADR-003 + IF-ADR-007 + IF-ADR-011 + IF-ADR-012 | Keep in reconciled baseline; update the indicated accepted ADR before treating the exact wording as fully normative. |
| E05 | Stages | DOMAIN DECISION | Conceptual chain: Joined → Actor Resolved → Logical Prepared → Physical Available → Gameplay Ready. | **ALREADY DEFINED — ACCEPTED ADR** | IF-ADR-001 + IF-ADR-003 | Keep as canonical/clarifying wording; runtime audit must test conformance, not redesign it. |
| E06 | Physical stage | TECHNICAL REVIEW | Player Entry Readiness may require explicit evidence that the selected Actor has a valid physical representation available. This must reuse existing Player/Actor materialization authority and evidence rather than create a second materialization or readiness authority. | **COMPATIBLE NEW DECISION — ADR UPDATE** | IF-ADR-003 + IF-ADR-007 + IF-ADR-011 + IF-ADR-012 | Keep as a candidate domain decision; formalize through the indicated ADR amendment before implementation cuts. |
| E07 | Coverage | DOMAIN DECISION | `At Least N`, `All Occupied`, `All Eligible`; `Any` may only be a UX alias for `At Least 1`. | **COMPATIBLE NEW DECISION — ADR UPDATE** | IF-ADR-003 + IF-ADR-007 + IF-ADR-011 + IF-ADR-012 | Keep as a candidate domain decision; formalize through the indicated ADR amendment before implementation cuts. Coverage vocabulary is new Player-specific policy over existing readiness authority. |
| E08 | All Occupied | DOMAIN DECISION | Captures the occupied cohort at Entry Attempt start; it does not silently shrink. | **COMPATIBLE NEW DECISION — ADR UPDATE** | IF-ADR-003 + IF-ADR-007 + IF-ADR-011 + IF-ADR-012 | Keep as a candidate domain decision; formalize through the indicated ADR amendment before implementation cuts. Cohort capture is compatible with occurrence-scoped readiness snapshots but the exact All Occupied semantics are new. |
| E09 | All Eligible | DOMAIN DECISION | Requires all Slots allowed by the Profile, even if some are vacant. | **COMPATIBLE NEW DECISION — ADR UPDATE** | IF-ADR-003 + IF-ADR-007 + IF-ADR-011 + IF-ADR-012 | Keep as a candidate domain decision; formalize through the indicated ADR amendment before implementation cuts. All Eligible including vacancies is a new Player policy. |
| E10 | At Least | DERIVED RULE | `N > eligible Slot count` is a structural authoring error. | **DERIVED / ADR-ALIGNED** | IF-ADR-003 + IF-ADR-007 + IF-ADR-011 + IF-ADR-012 | Keep as derived validation/runtime rule; do not create a separate ADR decision unless runtime evidence exposes a contradiction. |
| E11 | Runtime capacity | DERIVED RULE | `N <= eligible Slot count` with insufficient current Capacity does not make the Profile structurally invalid by itself. | **DERIVED / ADR-ALIGNED** | IF-ADR-003 + IF-ADR-007 + IF-ADR-011 + IF-ADR-012 | Keep as derived validation/runtime rule; do not create a separate ADR decision unless runtime evidence exposes a contradiction. |
| E12 | Satisfiability | DERIVED RULE | While still satisfiable → Preparing. Once framework can prove the occurrence is unsatisfiable → Failed. | **DERIVED / ADR-ALIGNED** | IF-ADR-003 + IF-ADR-007 + IF-ADR-011 + IF-ADR-012 | Keep as derived validation/runtime rule; do not create a separate ADR decision unless runtime evidence exposes a contradiction. |
| E13 | No Player | DOMAIN DECISION | Zero occupied Players is controlled by explicit `If No Players Are Available`. | **COMPATIBLE NEW DECISION — ADR UPDATE** | IF-ADR-003 + IF-ADR-007 + IF-ADR-011 + IF-ADR-012 | Keep as a candidate domain decision; formalize through the indicated ADR amendment before implementation cuts. Zero-player behavior should be explicit rather than inferred. |
| E14 | Empty | DOMAIN DECISION | `Allow Empty Entry` permits the empty case; `Require Player` does not. | **COMPATIBLE NEW DECISION — ADR UPDATE** | IF-ADR-003 + IF-ADR-007 + IF-ADR-011 + IF-ADR-012 | Keep as a candidate domain decision; formalize through the indicated ADR amendment before implementation cuts. New explicit zero-player policy vocabulary. |
| E15 | None | DOMAIN DECISION | `Ready When=None` means Player participation does not contribute to Entry Readiness; it does not remove Slot scope/observation. | **COMPATIBLE REFINEMENT — ADR UPDATE** | IF-ADR-003 + IF-ADR-007 + IF-ADR-011 + IF-ADR-012 | Keep in reconciled baseline; update the indicated accepted ADR before treating the exact wording as fully normative. |
| T01 | Current Activity | TECHNICAL REVIEW | `Current Activity` is the Activity authority established by the lifecycle mutation that actually committed. It may already be current while its Activity Entry Readiness occurrence is still `Preparing`; after Clear it may be `None`. | **ALREADY DEFINED — ACCEPTED ADR** | IF-ADR-001 + IF-ADR-006 + IF-ADR-007 + IF-ADR-011 | Keep as canonical/clarifying wording; runtime audit must test conformance, not redesign it. |
| T02 | Next Activity | TECHNICAL REVIEW | An Activity request uses the existing GameFlow transition/lifecycle authority. A non-accepted pre-commit transition leaves the previous authority unchanged. Once the target lifecycle mutation commits, the target Activity may become current **before** its Entry Readiness reaches `Ready`. Entry Readiness then progresses `Preparing → Ready` or to a typed terminal failure/recovery state without creating a second commit/transaction concept. | **CONFLICT RESOLVED — REWRITTEN** | IF-ADR-001 + IF-ADR-006 + IF-ADR-007 + IF-ADR-011 | Use the reconciled wording. The original matrix wording is superseded by accepted transition/readiness ADR semantics. |
| T03 | Snapshot | DOMAIN DECISION | Entry Attempt captures Eligible Slots, Occupied Slots, Coverage, Ready When and Activity/occurrence identity. | **COMPATIBLE REFINEMENT — ADR UPDATE** | IF-ADR-001 + IF-ADR-006 + IF-ADR-007 + IF-ADR-011 | Keep in reconciled baseline; update the indicated accepted ADR before treating the exact wording as fully normative. Must fit existing Activity definition/occurrence/revision identity; no second transaction identity. |
| T04 | Live evidence | DERIVED RULE | Slot lifecycle evidence stays live during Preparing. | **DERIVED / ADR-ALIGNED** | IF-ADR-001 + IF-ADR-006 + IF-ADR-007 + IF-ADR-011 | Keep as derived validation/runtime rule; do not create a separate ADR decision unless runtime evidence exposes a contradiction. |
| T05 | Historical evidence | DERIVED RULE | The captured Player Entry Participation question/cohort/policy is occurrence-scoped and immutable once captured. Terminal historical evidence records why that readiness occurrence became `Ready`, failed, was invalidated, cancelled, or superseded. **Activity commit is not evidence that readiness was already accepted.** | **CONFLICT RESOLVED — REWRITTEN** | IF-ADR-001 + IF-ADR-006 + IF-ADR-007 + IF-ADR-011 | Use the reconciled wording. The original matrix wording is superseded by accepted transition/readiness ADR semantics. |
| T06 | Current participation | DOMAIN DECISION | Live participation after Commit is a separate projection and may include late joins. | **COMPATIBLE REFINEMENT — ADR UPDATE** | IF-ADR-001 + IF-ADR-006 + IF-ADR-007 + IF-ADR-011 | Keep in reconciled baseline; update the indicated accepted ADR before treating the exact wording as fully normative. Live projection is distinct from the captured entry question and may include late joins after the initial occurrence is finalized. |
| T07 | Conflict risk | TECHNICAL REVIEW | Exact transition shape must be reconciled with existing transition ADRs/runtime to avoid creating a duplicate transaction concept. | **ALREADY DEFINED — ACCEPTED ADR** | IF-ADR-001 + IF-ADR-006 + IF-ADR-007 + IF-ADR-011 | Keep as canonical/clarifying wording; runtime audit must test conformance, not redesign it. |
| J01 | Separation | DOMAIN DECISION | `Joining Intent` and temporary blockers are separate concepts. | **COMPATIBLE NEW DECISION — ADR UPDATE** | IF-ADR-001 + IF-ADR-003 + IF-ADR-005; public extension would amend IF-ADR-015 | Keep as a candidate domain decision; formalize through the indicated ADR amendment before implementation cuts. Current ADRs expose joining state/commands but do not yet formalize Intent-vs-temporary-blocker as a Player-domain split. |
| J02 | Intent | DOMAIN DECISION | Open/Closed is game-controlled joining intent. | **ALIGNED WITH IF-ADR-015 — PROPOSED** | IF-ADR-015 (Proposed); underlying Player authority remains IF-ADR-003 | Keep as aligned with ADR-015, but do not treat as accepted normative contract until ADR-015 is accepted/updated. Open/Close joining is already in ADR-015 command vocabulary, but ADR-015 remains Proposed. |
| J03 | Inhibit | DOMAIN DECISION | Transition/Recovery/etc. add temporary **Join Inhibits** without mutating Joining Intent. | **COMPATIBLE NEW DECISION — ADR UPDATE** | IF-ADR-001 + IF-ADR-003 + IF-ADR-005; public extension would amend IF-ADR-015 | Keep as a candidate domain decision; formalize through the indicated ADR amendment before implementation cuts. New domain abstraction. Do not conflate with GameFlow Transition Gate ownership semantics. |
| J04 | Composition | DOMAIN DECISION | Effective Joining is composed from Intent + Inhibits + Capacity + Slot availability. | **DERIVED / ADR-ALIGNED** | IF-ADR-001 + IF-ADR-003 + IF-ADR-005; public extension would amend IF-ADR-015 | Keep as derived validation/runtime rule; do not create a separate ADR decision unless runtime evidence exposes a contradiction. |
| J05 | Multiple blockers | DERIVED RULE | Multiple independent inhibits may coexist and need identity. | **DERIVED / ADR-ALIGNED** | IF-ADR-001 + IF-ADR-003 + IF-ADR-005; public extension would amend IF-ADR-015 | Keep as derived validation/runtime rule; do not create a separate ADR decision unless runtime evidence exposes a contradiction. |
| J06 | Ownership | DERIVED RULE | The creator/owner of an inhibit may release only its own inhibit. | **DERIVED / ADR-ALIGNED** | IF-ADR-001 + IF-ADR-003 + IF-ADR-005; public extension would amend IF-ADR-015 | Keep as derived validation/runtime rule; do not create a separate ADR decision unless runtime evidence exposes a contradiction. |
| J07 | Lifetime | DERIVED RULE | Inhibit has Owner + Scope/Lifetime + Reason/Evidence and cannot survive scope end. | **DERIVED / ADR-ALIGNED** | IF-ADR-001 + IF-ADR-003 + IF-ADR-005; public extension would amend IF-ADR-015 | Keep as derived validation/runtime rule; do not create a separate ADR decision unless runtime evidence exposes a contradiction. |
| J08 | Consumer | DOMAIN DECISION | A game-owned temporary Join Inhibit is a **new public capability proposal**, not part of the currently proposed ADR-015 command vocabulary. It requires an explicit ADR amendment before implementation. | **COMPATIBLE NEW DECISION — ADR UPDATE** | IF-ADR-001 + IF-ADR-003 + IF-ADR-005; public extension would amend IF-ADR-015 | Keep as a candidate domain decision; formalize through the indicated ADR amendment before implementation cuts. |
| J09 | Transition | DOMAIN DECISION | Joining may be considered temporarily inhibited while a transition context requires it, **without mutating Joining Intent**. This must be evaluated by Player admission as contextual typed state; it must not reinterpret the GameFlow Transition Gate as a public lease/handle or create a second gate authority. | **COMPATIBLE NEW DECISION — ADR UPDATE** | IF-ADR-001 + IF-ADR-003 + IF-ADR-005; public extension would amend IF-ADR-015 | Keep as a candidate domain decision; formalize through the indicated ADR amendment before implementation cuts. |
| J10 | Result | DOMAIN DECISION | RequestJoin should produce typed acceptance/rejection reasons such as closed, inhibited, capacity reached or no Slot. | **ALIGNED WITH IF-ADR-015 — PROPOSED** | IF-ADR-015 (Proposed); underlying Player authority remains IF-ADR-003 | Keep as aligned with ADR-015, but do not treat as accepted normative contract until ADR-015 is accepted/updated. Typed command-result direction is already in ADR-015, but exact rejection vocabulary remains subject to final contract. |
| PH01 | Distinction | DOMAIN DECISION | Physical existence is not equivalent to alive, visible, enabled, targetable or controllable. | **ALREADY DEFINED — ACCEPTED ADR** | IF-ADR-001 + IF-ADR-003 | Keep as canonical/clarifying wording; runtime audit must test conformance, not redesign it. |
| PH02 | Activity | DOMAIN DECISION | Activity uses `Physical Presence = No Requirement / Require`. | **COMPATIBLE NEW DECISION — ADR UPDATE** | IF-ADR-001 + IF-ADR-003 | Keep as a candidate domain decision; formalize through the indicated ADR amendment before implementation cuts. New Activity Player policy over existing contextual materialization authority. |
| PH03 | Route default | DOMAIN DECISION | Physical Presence is part of the Player Activity Profile defaulted by Route and locally overridable by Activity. | **COMPATIBLE NEW DECISION — ADR UPDATE** | IF-ADR-003 + IF-ADR-012; validation constrained by IF-ADR-007 | Keep as a candidate domain decision; formalize through the indicated ADR amendment before implementation cuts. Requires the Player Activity Profile amendment. |
| PH04 | Require lifetime | DOMAIN DECISION | `Require` applies while that Activity occurrence is current and includes late joins. | **COMPATIBLE NEW DECISION — ADR UPDATE** | IF-ADR-001 + IF-ADR-003 | Keep as a candidate domain decision; formalize through the indicated ADR amendment before implementation cuts. Late-join/current-occurrence lifetime is new normative detail. |
| PH05 | Structural points | DOMAIN DECISION | `Require` causes ensure/reconcile at structural lifecycle points: Entry, late join, Actor resolution/change and explicit reconciliation request. | **COMPATIBLE NEW DECISION — ADR UPDATE** | IF-ADR-001 + IF-ADR-003 | Keep as a candidate domain decision; formalize through the indicated ADR amendment before implementation cuts. Existing runtime already has reconcile/materialization concepts, but this exact public policy/trigger list is a new decision. |
| PH06 | Respawn | DERIVED RULE | Loss of physical representation during gameplay does **not** automatically respawn/recreate it. | **DERIVED / ADR-ALIGNED** | IF-ADR-001 + IF-ADR-003 | Keep as derived validation/runtime rule; do not create a separate ADR decision unless runtime evidence exposes a contradiction. |
| PH07 | Runtime ownership | DERIVED RULE | Activity declares need; Player runtime owns how physical presence is ensured/reconciled. | **DERIVED / ADR-ALIGNED** | IF-ADR-001 + IF-ADR-003 | Keep as derived validation/runtime rule; do not create a separate ADR decision unless runtime evidence exposes a contradiction. |
| PH08 | Existing physical | DERIVED RULE | A valid existing physical representation may be reused; Activity does not rematerialize it on every entry. | **DERIVED / ADR-ALIGNED** | IF-ADR-001 + IF-ADR-003 | Keep as derived validation/runtime rule; do not create a separate ADR decision unless runtime evidence exposes a contradiction. |
| PH09 | Route | DOMAIN DECISION | Route may have `Preserve Existing / Suppress` physical-presence intent. | **COMPATIBLE NEW DECISION — ADR UPDATE** | IF-ADR-003 + IF-ADR-012; validation constrained by IF-ADR-007 | Keep as a candidate domain decision; formalize through the indicated ADR amendment before implementation cuts. Route `Preserve Existing / Suppress` is not defined by current ADRs and needs explicit reconciliation with Route/player materialization semantics. |
| PH10 | Suppress | DOMAIN DECISION | Route change alone does not imply dematerialization; `Suppress` is explicit desired absence. | **COMPATIBLE NEW DECISION — ADR UPDATE** | IF-ADR-001 + IF-ADR-003 | Keep as a candidate domain decision; formalize through the indicated ADR amendment before implementation cuts. Clarifies that Route change itself is not dematerialization authority. |
| PH11 | Contradiction | DERIVED RULE | Route `Suppress` + Activity `Require` is a structural authoring error. | **DERIVED / ADR-ALIGNED** | IF-ADR-003 + IF-ADR-012; validation constrained by IF-ADR-007 | Keep as derived validation/runtime rule; do not create a separate ADR decision unless runtime evidence exposes a contradiction. |
| PH12 | Readiness conflict | DERIVED RULE | Route `Suppress` + Entry stage necessarily requiring Physical Available / Gameplay Ready is a structural error. | **DERIVED / ADR-ALIGNED** | IF-ADR-003 + IF-ADR-012; validation constrained by IF-ADR-007 | Keep as derived validation/runtime rule; do not create a separate ADR decision unless runtime evidence exposes a contradiction. |
| PH13 | Implementation | DERIVED RULE | `Suppress` does not prescribe Destroy vs pooling/hiding/etc.; implementation details are separate from public intent/evidence. | **DERIVED / ADR-ALIGNED** | IF-ADR-003 + IF-ADR-012; validation constrained by IF-ADR-007 | Keep as derived validation/runtime rule; do not create a separate ADR decision unless runtime evidence exposes a contradiction. |
| G01 | Framework | GLOBAL | Framework owns state/truth, contracts, evidence, capabilities and progression/lifecycle it genuinely owns. | **ALREADY DEFINED — ACCEPTED ADR** | Cross-ADR constraints, principally IF-ADR-001/003/007/010 | Keep as canonical/clarifying wording; runtime audit must test conformance, not redesign it. |
| G02 | Game | GLOBAL | Gameplay owns when/why capabilities are used, UI/interaction flow and game-specific rules/orchestration. | **ALREADY DEFINED — ACCEPTED ADR** | Cross-ADR constraints, principally IF-ADR-001/003/007/010 | Keep as canonical/clarifying wording; runtime audit must test conformance, not redesign it. |
| G03 | Actor selection | DERIVED RULE | Framework does not own a generic character-selection flow. | **DERIVED / ADR-ALIGNED** | Cross-ADR constraints, principally IF-ADR-001/003/007/010 | Keep as derived validation/runtime rule; do not create a separate ADR decision unless runtime evidence exposes a contradiction. |
| G04 | Pending | DERIVED RULE | Do not create global `Selection Pending` merely because Actor is unresolved. | **DERIVED / ADR-ALIGNED** | Cross-ADR constraints, principally IF-ADR-001/003/007/010 | Keep as derived validation/runtime rule; do not create a separate ADR decision unless runtime evidence exposes a contradiction. |
| G05 | Reselection | HISTORICAL / REJECTED | Do not add `Allow Reselect`, `Require Reselect` or Activity re-entry selection behavior. | **HISTORICAL / REJECTED** | Original matrix rejected-history guardrails; compatible with current ADR boundaries | Preserve only as a regression guardrail; do not implement or reopen without new evidence. |
| G06 | Runtime requirement | HISTORICAL / REJECTED | Do not create a generic Runtime Participation Requirement derived from Activity Ready When. | **HISTORICAL / REJECTED** | Original matrix rejected-history guardrails; compatible with current ADR boundaries | Preserve only as a regression guardrail; do not implement or reopen without new evidence. |
| G07 | UI dependency | GLOBAL | Framework does not prove that game UI has an interaction path to solve an external dependency. | **ALREADY DEFINED — ACCEPTED ADR** | Cross-ADR constraints, principally IF-ADR-001/003/007/010 | Keep as canonical/clarifying wording; runtime audit must test conformance, not redesign it. |
| G08 | Diagnostics | GLOBAL | Framework must clearly expose unresolved external dependency/evidence. | **ALREADY DEFINED — ACCEPTED ADR** | Cross-ADR constraints, principally IF-ADR-001/003/007/010 | Keep as canonical/clarifying wording; runtime audit must test conformance, not redesign it. |
| G09 | Validation | GLOBAL | Hard errors are for contradictions intrinsically provable by the framework. | **ALREADY DEFINED — ACCEPTED ADR** | Cross-ADR constraints, principally IF-ADR-001/003/007/010 | Keep as canonical/clarifying wording; runtime audit must test conformance, not redesign it. |
| R01 | Failure | DERIVED RULE | Readiness/runtime detects and reports typed failure; it does not know Lobby/Menu/etc. | **DERIVED / ADR-ALIGNED** | IF-ADR-002 + IF-ADR-006 + IF-ADR-007 + IF-ADR-010 | Keep as derived validation/runtime rule; do not create a separate ADR decision unless runtime evidence exposes a contradiction. |
| R02 | Recovery | DERIVED RULE | Game chooses recovery. | **DERIVED / ADR-ALIGNED** | IF-ADR-002 + IF-ADR-006 + IF-ADR-007 + IF-ADR-010 | Keep as derived validation/runtime rule; do not create a separate ADR decision unless runtime evidence exposes a contradiction. |
| R03 | Product UX | DOMAIN DECISION | Provide an official authoring surface for reacting to typed Activity Entry failure evidence through legitimate public capabilities. This is a product-surface decision; it must not make Player/readiness recovery a game-local authority or a generic global event bus. | **COMPATIBLE NEW DECISION — ADR UPDATE** | IF-ADR-002 + IF-ADR-006 + IF-ADR-007 + IF-ADR-010 | Keep as a candidate domain decision; formalize through the indicated ADR amendment before implementation cuts. Likely belongs to a broader product/reaction decision rather than only ADR-003 Player lifecycle. |
| R04 | Reaction model | GLOBAL | Reactions use typed facts/conditions/actions/results. | **GLOBAL / REMOVE FROM PLAYER-SPECIFIC LAYER** | Transversal extension/reaction principles; not Player-specific ADR decisions | Preserve as transversal guidance, not as an independent Player-domain decision. |
| R05 | Scope | GLOBAL | Prefer context-specific reaction authoring components, not one universal visual-scripting system. | **GLOBAL / REMOVE FROM PLAYER-SPECIFIC LAYER** | Transversal extension/reaction principles; not Player-specific ADR decisions | Preserve as transversal guidance, not as an independent Player-domain decision. |
| R06 | Extension | GLOBAL | Game may extend conditions/reactions and invoke legitimate public capabilities without becoming another authority. | **GLOBAL / REMOVE FROM PLAYER-SPECIFIC LAYER** | Transversal extension/reaction principles; not Player-specific ADR decisions | Preserve as transversal guidance, not as an independent Player-domain decision. |
| R07 | Forbidden | GLOBAL | Reactions may not force readiness, mutate private authority, bypass occurrence/scope, or use implicit global lookup. | **GLOBAL / REMOVE FROM PLAYER-SPECIFIC LAYER** | Transversal extension/reaction principles; not Player-specific ADR decisions | Preserve as transversal guidance, not as an independent Player-domain decision. |
| C01 | Observation | GLOBAL | Gameplay may observe Player/Session/Activity state without gaining mutation authority. | **GLOBAL / REMOVE FROM PLAYER-SPECIFIC LAYER** | IF-ADR-001 + IF-ADR-015 command/observation separation | Preserve as transversal guidance, not as an independent Player-domain decision. |
| C02 | Activity view | DOMAIN DECISION | Activity observation is Slot-centered and combines Activity scope with live Session truth. | **COMPATIBLE REFINEMENT — ADR UPDATE** | IF-ADR-001 + IF-ADR-003 + IF-ADR-012 + IF-ADR-015 (Proposed) | Keep in reconciled baseline; update the indicated accepted ADR before treating the exact wording as fully normative. ADR-015 requires coherent immutable observation but intentionally leaves DTO/split open; this proposes the Activity projection split. |
| C03 | Session view | DOMAIN DECISION | Session has its own observation surface independent of the current Activity. | **COMPATIBLE REFINEMENT — ADR UPDATE** | IF-ADR-001 + IF-ADR-003 + IF-ADR-012 + IF-ADR-015 (Proposed) | Keep in reconciled baseline; update the indicated accepted ADR before treating the exact wording as fully normative. Same: proposes a distinct Session view without creating a second authority. |
| C04 | Commands | GLOBAL | Commands are requests to existing authorities, not setters for internal mutable state. | **ALIGNED WITH IF-ADR-015 — PROPOSED** | IF-ADR-015 (Proposed), constrained by IF-ADR-001 | Keep as aligned with ADR-015, but do not treat as accepted normative contract until ADR-015 is accepted/updated. Command-not-setter boundary is strongly architecture-aligned; its Player consumer surface is currently specified in Proposed ADR-015. |
| C05 | Activity capabilities | DOMAIN DECISION | Activity-contextual capabilities validate Who Participates. | **COMPATIBLE REFINEMENT — ADR UPDATE** | IF-ADR-001 + IF-ADR-003 + IF-ADR-012 + IF-ADR-015 (Proposed) | Keep in reconciled baseline; update the indicated accepted ADR before treating the exact wording as fully normative. Activity-contextual capability scope remains domain-specific; the current Player provisioning consumer API is now shipped and scoped explicitly without global lookup. |
| C06 | Session capabilities | DOMAIN DECISION | RequestJoin, Capacity and general Session observation do not depend on current Activity Who Participates. | **COMPATIBLE REFINEMENT — ADR UPDATE** | IF-ADR-001 + IF-ADR-003 + IF-ADR-012 + IF-ADR-015 (Proposed) | Keep in reconciled baseline; update the indicated accepted ADR before treating the exact wording as fully normative. Session capabilities remain Session-scoped even when an Activity projection excludes a Slot. |
| C07 | Consumer boundary | TECHNICAL REVIEW | Scoped command/observation reachability follows the boundary proposed by IF-ADR-015 and is now concretely realized by P1/P2: typed, explicitly scoped, lifetime-explicit and cross-scene-capable, with no service locator, scene search, reflection or second Player authority. P3/P4 provide command authoring and status/diagnostics surfaces. | **ALIGNED WITH IF-ADR-015 — PROPOSED** | IF-ADR-015 (Proposed), constrained by IF-ADR-001 | Keep as aligned with ADR-015, but do not treat as accepted normative contract until ADR-015 is accepted/updated. Architecture boundary is substantially specified and implementation reachability is QA-certified (Q1 29/29; Q2 36/36). Remaining ADR-015 work is product proof/disposition, not consumer reachability. |

## 5. Reconciliation summary by status

| Status | Count |
|---|---:|
| COMPATIBLE NEW DECISION — ADR UPDATE | 33 |
| DERIVED / ADR-ALIGNED | 30 |
| ALREADY DEFINED — ACCEPTED ADR | 19 |
| COMPATIBLE REFINEMENT — ADR UPDATE | 16 |
| GLOBAL / REMOVE FROM PLAYER-SPECIFIC LAYER | 5 |
| ALIGNED WITH IF-ADR-015 — PROPOSED | 4 |
| CONFLICT RESOLVED — REWRITTEN | 3 |
| HISTORICAL / REJECTED | 2 |
| DEFERRED / DO NOT GENERALIZE | 1 |

Total decision IDs: **113**.

## 6. Decisions that now require ADR amendments before implementation

### 6.1 IF-ADR-003 — Player Participation and Actor Lifecycle

Primary additions/refinements to formalize:

```text
Session Player model
  Supported Slots
  Current Capacity
  allocation policy
  stable Slot identity

Provisioning model
  Player Provisioning Profile
  Session-scoped effective provisioning intent
  default vs external/unresolved Actor resolution vocabulary

Join model
  Joining Intent vs temporary Player admission blockers
  Join Inhibits, only if deliberately adopted

Physical Presence
  No Requirement / Require
  structural reconcile points
  Route Preserve Existing / Suppress, if adopted
```

### 6.2 IF-ADR-012 — Activity Player Participation Profile

Primary additions/refinements:

```text
complete Player Activity Profile authoring surface
Route Player default + Activity Inherit / complete Override
occurrence-scoped effective Player intent
Who Participates modes
Ready When Player stages
Required Coverage
zero-player policy
Player Physical Presence composition
```

Constraint: the normalized effective policy remains the sole runtime Player-participation input. Do not create a parallel runtime policy pipeline.

### 6.3 IF-ADR-015 — Consumer commands and observation

Before acceptance, reconcile:

```text
Session observation vs Activity Slot-centered projection
Activity-contextual vs Session-scoped capability validation
whether game-owned Join Inhibit is part of the public contract
scoped cross-scene reachability against the actual package implementation
```

Do not add public prepare/materialize/reconcile commands merely to satisfy the matrix.

### 6.4 Session default owner / authoring

`S01/S02` require a deliberate ADR amendment that says whether `GameApplication` owns the default Player Session Profile authoring source. Current authority hierarchy makes this compatible, but the exact product field/asset is not yet an accepted decision.

### 6.5 Failure recovery authoring

`R03` is a valid product gap but should not be silently absorbed into Player lifecycle. It should be reconciled as a context-specific typed reaction surface under the broader product/Editor model.

## 7. Canonical reconciled model for runtime confrontation

```text
GAME APPLICATION / SESSION
  may provide initial Player Session intent [ADR amendment required]
  Session owns Player/Slot identity and mutable Session state

PLAYER SESSION
  stable PlayerSlotId
  Supported Slots
  Current Capacity
  Joining state / admission
  Session-scoped Host/Player truth

PLAYER PROVISIONING
  Scene Provided / Manager Provisioned
  Host provisioning != Actor resolution
  effective provisioning intent stable for Session [ADR amendment]

ROUTE / ACTIVITY PLAYER INTENT
  Route may default Player Activity Profile [ADR amendment]
  Activity Inherit / complete Override [ADR amendment]
  Effective Player Activity Profile occurrence-scoped
  Who Participates / Ready When / Coverage / zero-player / Physical Presence

ACTIVITY ENTRY LIFECYCLE
  existing GameFlow transition owns pre/post-commit authority
  target Activity may become Current while Player readiness is Preparing
  ActivityEntryReadinessPolicy remains Activity-owned

PLAYER ENTRY READINESS
  occurrence-scoped captured Player question/cohort/policy
  live Player evidence may evolve during the occurrence
  Ready / typed terminal failure evidence
  finalized historical evidence is not reopened by later joins

CURRENT ACTIVITY PLAYER PARTICIPATION
  separate live Slot-centered projection
  may include late joins after initial readiness finalization

JOIN
  Session operation
  Activity scope does not own Session Join
  optional Join Inhibits require an explicit new Player contract
  transition inhibition must not become a Transition Gate lease

PHYSICAL PRESENCE
  contextual Player requirement
  Player runtime remains materialization/reconcile authority
  Require is not gameplay respawn

CONSUMER
  typed scoped commands
  immutable observations
  no second Player/readiness/transition authority
```

## 8. Rules for the next source/runtime confrontation

The next pass should inspect the package operations **without changing this baseline merely because code currently behaves differently**. Each reconciled decision should be classified against the real implementation as:

```text
IMPLEMENTED AS RECONCILED
IMPLEMENTED UNDER DIFFERENT NAME/SHAPE BUT SEMANTICALLY EQUIVALENT
PARTIALLY IMPLEMENTED
NOT IMPLEMENTED
RUNTIME CONFLICT — CODE CONTRADICTS ACCEPTED ADR
RUNTIME GAP — RECONCILED ADR UPDATE NOT YET IMPLEMENTED
OBSOLETE / DUPLICATE IMPLEMENTATION
```

Priority in a disagreement:

```text
Accepted ADR
  > reconciled compatible amendment candidate
  > Proposed ADR
  > current implementation detail
```

If runtime evidence exposes a real impossibility or contradiction, reopen the specific decision with that evidence rather than silently changing the matrix.

## 9. Explicitly out of scope for this document

- No package/source audit beyond the architectural facts already recorded in the ADR documents.
- No decision about exact C# type/class/asset names.
- No implementation cuts or ZIP plan.
- Player Surface technical QA is complete: Q1 29/29 and Q2 36/36, joint verdict `PLAYER SURFACE QA CERTIFIED`.
- FIRSTGAME manual real-consumer proof remains the next product evidence; P5 tooling disposition follows that proof.
- No Session Leave/disconnect/reconnect design.
- No Session-Persistent Player design.

The next document/pass should therefore be a **runtime operations confrontation matrix** using this file as the normative comparison baseline.
