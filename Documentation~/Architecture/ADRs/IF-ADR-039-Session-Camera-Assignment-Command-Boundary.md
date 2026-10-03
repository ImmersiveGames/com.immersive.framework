# IF-ADR-039 — Session Camera Assignment Command Boundary

Status: **Accepted — Unity validation pending**
Proposed: **2026-10-03**
Type: architecture / Session Camera / public command boundary
Depends on: **IF-ADR-038**

## 1. Context

IF-ADR-038 establishes Session Camera Assignment as the authority for normal Camera selection and already requires explicit transactional Assignment replacement. The runtime implements replacement internally, but consumers currently have no supported command boundary to activate, replace or intentionally remove an active Assignment.

This blocks legitimate game composition such as:

```text
Hub       -> no normal Assignment -> Output Fallback
Gameplay A -> Assignment A
Gameplay B -> Assignment B
Gameplay C -> no normal Assignment -> Output Fallback
```

Route and Activity must not regain Camera ownership. A game may react to its own flow state and issue a Camera command, but the Camera Session remains the only authority that validates and commits the resulting Assignment state.

## 2. Decision

Expose one narrow Session Camera command boundary owned by Camera Session.

Supported intents:

1. **Activate** — make an authored Session Camera Assignment active on its explicitly mapped Output(s) when those Outputs currently have no active normal Assignment.
2. **Replace** — transactionally replace the currently active Assignment on the candidate's explicitly mapped Output(s).
3. **Clear** — explicitly remove one named active Assignment by `SessionCameraAssignmentId`, leaving all Outputs owned by that Assignment on their own Fallback Camera.

Activation/replacement uses the existing IF-ADR-038 transaction contract:

```text
validate candidate
-> prepare candidate occurrence(s)
-> preserve current state on failure
-> commit Output routing
-> release previous occurrence(s) after commit
```

Clear is an explicit Session Camera mutation, not temporary fallback coverage. After successful Clear the affected Output has no configured active normal Assignment and presents its Fallback.

## 3. Consumer boundary

The Framework exposes a game-facing command port plus an optional Unity trigger/authoring adapter.

The consumer supplies explicit intent and authored Camera identities. It does not:

- access `FrameworkRuntimeHost`;
- discover Camera state globally;
- select by hierarchy/name;
- manipulate Cinemachine directly;
- write `Camera.rect` / `pixelRect`;
- own Assignment or Occurrence lifetime.

A trigger may serialize an Assignment candidate for Activate/Replace or an explicit Assignment identity for Clear. Binding follows the existing explicit composition/binder pattern used by Route, Activity, Pause and Reset triggers.

The command result must expose success/failure and a diagnostic suitable for samples and QA.

## 4. Non-goals

This ADR does not introduce:

- CameraRequest;
- precedence, winner arbitration or a pending selection queue;
- Route/Activity Camera fields;
- automatic Camera selection from Route/Activity identity;
- a global Camera service or service locator;
- a catalog of inactive Assignments in `GameApplicationAsset`;
- fallback as a competing Assignment.

`GameApplicationAsset.SessionCameraAssignments` remains the set of Assignments active at Session startup. Runtime command candidates are explicit consumer intent, not additional startup entries.

## 5. Ownership and semantics

- Camera Session is the single writer of active Assignment state.
- An Output has at most one active normal Assignment.
- Activate fails if a targeted Output already has an active Assignment; callers use Replace for that transition.
- Replace fails transactionally if the candidate cannot be prepared and preserves the previous Assignment.
- Clear targets one explicit active Assignment identity and clears all Outputs owned by that Assignment through Session Camera authority.
- Temporary transition fallback coverage never implies Clear.
- Route/Activity changes alone never issue Camera commands.
- SessionScoped, SharedGroup and IndividualPerPlayer retain the semantics defined by IF-ADR-038.

## 6. Initial consumer proof

The first consumer proof is the GameFlow sample:

```text
Hub / Basic C -> Clear the sample-owned active Assignment -> Fallback
Basic A       -> Activate/Replace Assignment A
Basic B       -> Replace Assignment B
```

A and B use distinct Fixed Camera Definitions/Rigs. The sample owns the decision to issue these commands; Activity assets themselves remain Camera-free.

## 7. Validation

Required evidence:

- Activate from no Assignment to a valid SessionScoped Assignment;
- Replace A -> B without winner gap;
- failed Replace preserves A;
- Clear B -> Fallback and clears normal Assignment state;
- Activate again after Clear;
- temporary transition fallback does not clear the Assignment;
- no Route/Activity Camera authoring is introduced;
- Session shutdown remains clean.

Report Implemented / Tested / Integrated / Validated separately.
