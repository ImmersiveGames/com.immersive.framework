# IF-ADR-014 — Authored Definition and Stable Identity Authority

Status: **Accepted**  
Last updated: 2026-10-01
Related decisions: IF-ADR-001, IF-ADR-002, IF-ADR-003, IF-ADR-005, IF-ADR-006, IF-ADR-009, IF-ADR-010, IF-ADR-013, IF-ADR-015, IF-ADR-035
Closed execution record: [IF-ID Identity Authority](../Archive/Plans/IF-ID-IDENTITY-AUTHORITY-EXECUTION-PLAN-2026-08-06.md)

> Current implementation, QA and FIRSTGAME integration status is tracked in
> `../Tracking/IF-TRACK-Framework.md`. This ADR is normative and intentionally
> does not carry a mutable completion percentage. UX observations are qualitative
> product feedback and are not part of functional completion arithmetic.

## Context

Unity asset references and stable external identifiers solve different problems.
Treating equal stable IDs as authored-definition equality can merge distinct
assets, while relying only on references breaks persistence/external boundaries.

## Decision

Authored/runtime definition equality uses the exact `RouteAsset` or
`ActivityAsset` reference.

`RouteId` and `ActivityId` are stable projections for persistence,
serialization, diagnostics and external references. Two distinct assets with the
same stable ID are a collision and must not silently become one definition.

Operational Route/Activity ownership requires a process-local
`RuntimeDefinitionToken` for the exact definition instance in addition to stable
ID.

Runtime occurrence identity remains definition-aware and occurrence-scoped.
Stable ID is not lifecycle, readiness, release, supersession or cleanup authority.

## Authority model

| Dimension | Authority |
|---|---|
| Authored definition | exact typed asset reference |
| Stable boundary identity | `RouteId` / `ActivityId` |
| Runtime occurrence | definition reference + occurrence/sequence/revision |
| Operational ownership | scoped owner + `RuntimeDefinitionToken` |
| Physical occurrence binding | owner-scoped runtime projection from stable logical identity to a live physical object |
| Presentation | display name only |

## Stable-ID rules

- Stable IDs never regenerate automatically on rename, move, import or
  `OnValidate`.
- Collision repair is explicit and targeted to the selected definition.
- Definition-local cleanup/release uses exact definition/token authority, never
  stable ID alone.
- Project-wide collision diagnostics do not turn stable ID into runtime equality.

## Object Entry identity and metadata ownership

`ObjectEntryId` is the stable semantic identity of a logical Object Entry within
the Object Entry identity domain.

Object Entry is a passive metadata/addressing layer, not a new authored-definition
or lifecycle authority.

For scene-authored content, `ObjectEntryDeclaration` serializes one stable
`ObjectEntryId` value and independent requiredness metadata. Its ID follows the
same explicit generation/copy/regeneration model as Route and Activity IDs and
does not change on rename or move. The owner-aware admission transaction
supplies effective scope and exact `RuntimeContentOwner`; declarations do not
repeat a Route/Activity asset or owner identity.

The accepted relationship is:

```text
ObjectEntryDeclaration
  authored passive metadata
        ↓
ObjectEntryDescriptor / ObjectEntrySet
  stable object identity + scope/owner/requiredness/source metadata
        ↓
ObjectEntryRuntimeContextSnapshot
  scoped read-only projection
```

Rules:

```text
ObjectEntryId
  = stable logical object identity

Display name / GameObject name / hierarchy / scene path
  = diagnostics only
  != functional object identity

Route/Activity owner on new declarations
  = supplied by the explicit admission transaction
  != repeated authored declaration data

ObjectEntryRuntimeContextSnapshot
  = projection of current lifecycle context
  != runtime occurrence authority
```

Duplicate `ObjectEntryId` values in one accepted set are a collision and must be
rejected explicitly rather than merged silently.

An `ObjectEntryDeclaration` alone does not bind a Unity object. Physical binding
is a separate runtime projection established by an owner-aware content
registration transaction; it does not change the meaning or authority of
`ObjectEntryId`.

### Stable Object Binding — corrective decision (2026-10-01)

Framework Core may maintain an ephemeral `StableObjectBinding` to connect the
stable logical Object Entry identity to one physical object occurrence while
that content is admitted. Object Entry owns the logical identity and
declaration; RuntimeContent owns the physical binding table and its lifecycle.
The active Framework host composes this authority per application instance; no
singleton or process-global lookup is introduced. This is a generic boundary,
not Reset infrastructure.

The binding records:

```text
ObjectEntryId          stable logical identity
RuntimeContentOwner    exact lifetime/release authority
physical object        current Unity occurrence (runtime-only)
```

The effective lookup key is `(ObjectEntryId, RuntimeContentOwner)`. The owner
includes its typed scope, stable owner identity and process-local definition
token. The binding itself is only a projection: it cannot create identity,
ownership or lifecycle authority. The physical object reference and any
occurrence token remain runtime-only and are never serialized as the stable
reference.

The owner-aware Route/Activity content admission transaction creates bindings
from `ObjectEntryDeclaration` components found under its explicit materialized
roots and removes them on rollback/release. The transaction supplies effective
Route/Activity scope and exact owner; declarations do not author duplicate owner
or scope metadata.
Association is component-local: the declaration binds only the physical object
on its own GameObject; it does not search descendants for a Resettable or
another component. No `OnEnable` lookup, global search, name, hierarchy path or
current Activity/Route inference is permitted. A declaration without a valid
identity remains metadata only and cannot create a binding.

Bindings are occurrence records, not a one-value dictionary. Zero matching
records means unavailable; exactly one live record resolves; more than one is
ambiguous and rejected. Reuse of one `ObjectEntryId` by different owners keeps
distinct records under distinct composite keys and is not a collision. If an
authored reference omits an owner selector, it resolves only when the ID has
exactly one active binding across all owners. Normal owner selection references
the exact `RouteAsset` or `ActivityAsset`; its stable ID is derived for boundary
matching, while its definition token preserves exact-definition distinction.
`StableReference` stores `ObjectEntryId` and consumes the declaration's same
identity; it does not create an identity container.
A typed selector is not operational ownership authority.
If multiple active occurrences still match that selector, resolution is
ambiguous.

Resolution verifies Unity object liveness. A destroyed physical reference is
unavailable and cannot be returned as a stale occurrence; owner rollback/release
still removes its binding records deterministically.

The transaction rolls back every binding it created if preparation fails before
commit. After commit, the binding remains only for that `RuntimeContentOwner`
and is removed as part of that owner's release, including additive content
release. Duplicate active bindings for the same composite key are retained as
ambiguity evidence and never overwrite one another. After unload there is no
resolvable binding; reload creates a new physical occurrence and a new binding
for the same logical identity.

Future runtime materialization may use the same binding boundary by passing its
explicit request owner and materialized roots. This does not introduce a second
identity model or alter this decision's scene-authored transaction contract.

Reset consumes this generic boundary only: `StableReference` carries
`ObjectEntryId` and, when needed, a typed stable owner selector; resolution
returns the unique current physical occurrence, validates its registered
`Resettable`, and then uses that occurrence's current runtime Reset subject.
Reset never stores `ResetSubjectId` in the stable reference and never owns,
creates or releases the binding.

Physical binding, Reset execution, spawn/materialization behavior, Player/Actor
lifecycle and service registration remain outside the identity semantics of
`ObjectEntryId`; the separate binding lifecycle is governed by its content
transaction owner.

Lifecycle ownership and occurrence authority remain governed by IF-ADR-001.
Reset may consume Object Entry scope/owner metadata under IF-ADR-005 without
transferring Reset or lifecycle authority to Object Entry.

The historical F13 Object Entry Foundation is therefore reconciled into the
current architecture through IF-ADR-014 identity semantics plus IF-ADR-001
runtime authority, rather than being revived as a separate current feature ADR.

## Conformance evidence — IF-ADR-013 Optional Audio BGM Adapter

IF-ADR-013 is a concrete consumer of this identity authority and does not change
its accepted boundary. The optional BGM integration preserves exact
Route/Activity authored-definition authority and does not use audio cue identity,
desired BGM state or confirmed BGM state as Route/Activity equality, lifecycle
ownership, release authority or occurrence identity.

The 2026-08-10 IF-ADR-013A technical certification provides supporting
conformance evidence: Route/Activity BGM precedence, Route-scoped retained-state
cleanup and optional-authority failure behavior passed inside the existing scoped
Route/Activity lifecycle. This evidence does not reopen IF-ADR-014 and does not
change its completion/status; it demonstrates that the newer optional audio
feature respects the accepted identity model.

### IF-ADR-009 conformance evidence — 2026-08-10

The ADR-009 closure adds direct evidence for this identity boundary. Activity
Local Visibility now rejects distinct authored Activity definitions that collide
on the same stable `ActivityId`, while occurrence/release/restoration ownership
continues to use definition-aware runtime authority.

This is conformance evidence only. It does not change ADR-014 status or reopen
its accepted boundary.

## Deferred boundary

An application-wide resolver that treats stable ID alone as global runtime
authority remains deferred. The accepted `StableObjectBinding` boundary is
scoped to explicit owner transactions and must preserve typed owner selection,
ambiguity diagnostics and the distinction between stable logical identity and
runtime occurrence/ownership.

Object Entry request/result envelopes that remain Experimental are not promoted
by this reconciliation. Their public API necessity should be evaluated separately
as API/governance hygiene; stable identity ownership does not require a generic
Object Entry dispatcher.

## Reopen criteria

Reopen only if evidence shows distinct definitions collapsing through stable ID,
Object Entry identities being silently merged, release authority crossing
definition tokens, wrong-occurrence correlation, Object Entry metadata acquiring
lifecycle authority, implicit stable-ID mutation or a concrete external-resolution
requirement.
