# IF-ADR-014 — Authored Definition and Stable Identity Authority

Status: Proposed  
Last updated: 2026-08-06  
Supersedes: none  
Superseded by: none  
Related decisions: IF-ADR-001, IF-ADR-002  

> Formerly numbered IF-ADR-009. Renumbered to 014 to resolve a duplicate
> ADR number collision with Activity Local Visibility Rules (IF-ADR-009).

## Context

`RouteAsset` and `ActivityAsset` currently expose typed stable identities through
`RouteId` and `ActivityId`. Parts of the framework use those IDs to compare
definitions, create runtime ownership keys and report diagnostics.

Unity authoring, however, already represents Route and Activity definitions as
typed `ScriptableObject` references:

```text
GameApplication -> RouteAsset
RouteAsset -> ActivityAsset
ActivityAsset -> content and participation configuration
```

These references answer which authored definition is selected. A stable textual
ID answers a different question: how that definition is represented across a
boundary where a Unity object reference is unavailable or inappropriate.

Treating the stable ID as the authority for both questions creates avoidable
ambiguity:

```text
Are these references the same authored definition?
Do these definitions expose the same stable external identity?
```

The current product UX also exposes the problem when an asset is duplicated.
Unity correctly copies all serialized configuration, including the stable ID.
The duplicated definition is a different Unity asset but temporarily carries the
same external identity until the user explicitly regenerates it.

Removing ID uniqueness would make persistence, ownership, catalogs and external
resolution ambiguous. Keeping ID equality as the operational authority would
continue to make normal Unity reference-based authoring indirect.

## Decision

The future authority model is:

```text
Typed authored asset reference
  Authority for the selected definition inside Unity authoring and in-process
  Route/Activity runtime operation.

Typed stable ID
  Stable external identity projection used for persistence, explicit catalogs,
  runtime ownership keys, structured diagnostics and integrations that cannot
  carry a Unity asset reference.

Display name
  Human-facing presentation only.
```

`RouteAsset` and `ActivityAsset` remain immutable authoring definitions at
runtime. A scoped runtime context owns the active operational state and retains
the exact authored definition reference.

Conceptually:

```csharp
public sealed class RouteRuntimeContext
{
    public RouteAsset Definition { get; }

    public RouteId StableId => Definition.RouteId;
}
```

The exact public/internal shape may differ, but the authority relationship must
remain equivalent.

This ADR refines IF-ADR-001 without superseding it. Route and Activity continue
to own their domain identity and lifecycle. The refinement is that the selected
authored definition is identified in-process by its typed asset reference, while
its stable ID is the boundary-safe projection of that definition.

## Definition equality

When both sides are available as typed Unity references, definition equality is
reference equality:

```text
currentRoute == requestedRoute
currentActivity == requestedActivity
```

Stable-ID equality must not be used as a substitute for definition equality when
the references are already available.

APIs named like `HasSameIdentity` that compare IDs are ambiguous. They must be
removed, deprecated or renamed to communicate their exact meaning, such as:

```text
HasSameStableId
```

Two different authored assets are different definitions even if a temporary
authoring error gives them the same stable ID.

## Stable identity invariants

Stable IDs remain:

- typed by domain;
- required for definitions that cross an identity boundary;
- independent from asset filename and display name;
- unchanged by rename, move, import or Inspector repaint;
- explicitly regenerated when a duplicated asset represents a new concept;
- non-authoritative for choosing between already available typed references.

Within one runtime-resolvable application catalog, each stable ID must resolve to
exactly one authored definition.

Project-wide duplicate detection may remain as an audit, but a local Route or
Activity Inspector must not present an unrelated collision elsewhere in the
project as if the selected asset itself were invalid.

A collision is blocking when it:

- involves the selected definition;
- exists inside the active Game Application product graph;
- exists inside an explicit catalog used for stable-ID resolution;
- can make runtime ownership or persistence resolution ambiguous.

## Authoring behavior

Normal authoring uses typed references. Designers select Route, Activity, Profile,
Recipe and other definitions through Unity object fields rather than manually
entering stable IDs.

Asset duplication remains a native and explicit Unity operation:

```text
Duplicate asset
  -> copy configuration
  -> copy stable ID
  -> validator reports the temporary collision
  -> user regenerates the ID when the copy represents a new definition
```

The framework must not silently regenerate IDs through:

- `AssetPostprocessor`;
- `OnValidate`;
- import or reimport;
- asset rename or move;
- Inspector repaint;
- Play Mode entry.

An explicit `Regenerate Stable ID...` action is valid. A future
`Duplicate as New Route/Activity` action is also valid when it is an explicit
product operation that duplicates the asset and assigns a new identity in one
user-requested transaction.

## Runtime behavior

Route and Activity lifecycle requests should carry or resolve to typed authored
definitions.

The runtime uses the exact definition reference to decide:

- whether the requested target is already active;
- whether a transition targets a different definition;
- which configuration, scenes, profiles and policies apply;
- which scoped runtime context owns the operation.

The stable ID is derived from the selected definition for:

- structured facts and diagnostics;
- content ownership keys;
- persistence records;
- external integrations;
- explicit application-scoped catalog lookup.

A stable ID must not introduce an implicit global lookup or service locator.
Resolution from ID to asset requires an explicit, scoped catalog owned by the
Game Application or another documented product boundary.

## Persistence and external boundaries

Unity asset references are not the serialized interchange contract for save data
or external systems.

A persistence record may store:

```text
RouteId
ActivityId
versioned payload
```

Loading such a record requires an explicit scoped resolver:

```text
saved stable ID
  -> active Game Application catalog
  -> exactly one authored definition
  -> typed runtime request
```

Missing and ambiguous resolutions fail explicitly. The framework must not select
an arbitrary asset, use a filename fallback or search globally by name.

This ADR does not require save/progression implementation. It defines the
authority boundary that future persistence must respect.

## Validation model

Validation must be separated by scope.

### Definition-local validation

`Validate Route` and `Validate Activity` validate:

- the selected definition;
- its required stable ID;
- collisions involving that definition;
- its directly owned configuration;
- reachable dependencies appropriate to that product surface.

### Application validation

`Validate Game Application` validates:

- the active product graph;
- stable-ID uniqueness inside its resolvable scope;
- Route and Activity references reachable by the application;
- build/runtime requirements.

### Project audit

A separate project identity audit may report all collisions, including unused,
experimental or archived assets.

Project-audit findings that do not involve the selected definition must not be
merged into its local blocking status without being clearly labeled as unrelated
project-level issues.

Validators remain non-mutating.

## Migration strategy

The migration should be performed in four reversible cuts.

### Cut 1 — Contract and equality vocabulary

- Record and accept this ADR.
- Audit all Route/Activity stable-ID comparisons.
- Identify every use that means definition equality, boundary identity,
  ownership or diagnostics.
- Deprecate or rename ambiguous equality APIs.
- Preserve current runtime behavior until the audit is complete.
- Add focused QA for the current and target semantics.

### Cut 2 — Route and Activity runtime authority

- Make typed `RouteAsset` and `ActivityAsset` references the authority in
  lifecycle requests and active runtime contexts.
- Use reference equality for active-target and idempotence decisions.
- Keep stable IDs derived from the selected definitions.
- Prove enter, exit, switch, restart and re-entry behavior.
- Do not change content ownership semantics in the same cut unless required for
  compilation and explicitly covered.

### Cut 3 — Ownership and identity boundaries

- Audit Route/Activity content ownership, runtime identity keys, reset,
  diagnostics and any snapshot contracts.
- Keep IDs where a stable boundary key is genuinely required.
- Remove ID-based definition selection where a typed reference is available.
- Introduce an explicit application-scoped ID resolver only when a real
  persistence or external-resolution use case requires it.
- Prove deterministic release and cleanup.

### Cut 4 — Product validation and integration

- Separate definition-local, application and project identity validation.
- Keep explicit stable-ID regeneration for duplicated definitions.
- Update authoring documentation and Inspector diagnostics.
- Add QA for duplicated assets, regenerated IDs, reference-based lifecycle and
  collision failures.
- Prove the final workflow in FIRSTGAME.

No cut may silently allow ambiguous stable IDs in a runtime-resolvable scope.

## Compatibility rule

Until the runtime-authority migration is complete, the existing stable-ID
contract remains operational.

This ADR does not authorize an isolated change that:

- permits duplicate Route or Activity IDs;
- removes current validators;
- changes equality semantics in only one subsystem;
- leaves ownership and lifecycle using different undocumented authorities;
- introduces fallback from reference to name, path or arbitrary project search.

During migration, compatibility shims must be explicit, temporary and covered by
QA. They must not become a second permanent authority.

## Accepted scope

- Route and Activity authored-definition authority.
- Typed reference equality for in-process definition selection.
- Stable IDs as boundary identities.
- Scoped runtime contexts retaining authored definitions.
- Explicit stable-ID catalogs for future persistence or integrations.
- Separation of local, application and project validation.
- Explicit duplication and ID-regeneration UX.

## Rejected scope

- Removing RouteId or ActivityId.
- Allowing ambiguous IDs inside one runtime-resolvable application scope.
- Using display names, asset filenames, paths or hierarchy names as identity.
- Global static asset registries or service locators.
- Implicit project-wide lookup as runtime authority.
- Automatic identity mutation during import, validation or rename.
- Runtime mutation of authored ScriptableObject definitions.
- Treating a stable ID as sufficient proof that two loaded asset references are
  the same definition.

## Consequences

Normal Unity authoring becomes direct: references select definitions without
requiring an unnecessary ID lookup.

Runtime lifecycle becomes easier to reason about because the active context
retains the exact definition that supplied its configuration.

Stable IDs remain available for the cases that genuinely require persistence and
cross-boundary identity.

The framework must maintain a clear distinction between:

```text
definition reference
stable external identity
human-readable name
runtime occurrence or handle
```

Some existing APIs and tests will require migration. The highest-risk area is
Route/Activity lifecycle idempotence and transition comparison. Content release
and persistence resolution are the next most sensitive boundaries.

## Current implementation coverage

The target authority model is not yet implemented.

The current package already has:

- typed `RouteAsset` and `ActivityAsset` references in authoring;
- typed `RouteId` and `ActivityId` values;
- stable-ID validation;
- runtime use of stable IDs in identity and ownership paths;
- explicit Editor tooling to regenerate stable IDs for duplicated Route and
  Activity assets.

The current implementation still includes ID-based identity semantics that must
be audited before authority changes.

## Acceptance criteria

This ADR may move to `Accepted` when the architectural direction is approved.

The migration is complete only when:

- lifecycle requests and active contexts retain typed definitions;
- reference equality controls in-process target equality;
- stable IDs remain unique in each resolvable application scope;
- ID-based external resolution uses an explicit scoped catalog;
- ownership and release remain deterministic;
- local validation no longer absorbs unrelated project collisions as local
  blocking errors;
- QA proves Route and Activity switch, re-entry, restart, duplication,
  regeneration and collision failure;
- FIRSTGAME proves the workflow in a real consumer;
- no silent fallback or global lookup was introduced.

## Pending decisions

- Exact internal shape of Route and Activity runtime contexts.
- Whether the application catalog is authored explicitly or derived
  deterministically from the Game Application graph.
- Whether project-wide duplicate IDs are warnings or errors when the definitions
  cannot coexist in one application catalog.
- Compatibility and migration policy for future save data that already stores
  stable IDs.
- Whether explicit `Duplicate as New Route/Activity` product actions are worth
  adding after the authority migration.
