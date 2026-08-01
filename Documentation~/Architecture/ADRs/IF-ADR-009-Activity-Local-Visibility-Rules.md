# IF-ADR-009 — Activity Local Visibility Rules

Status: Accepted
Last updated: 2026-08-01
Supersedes: none
Superseded by: none

## Context

`ActivityLocalVisibilityAdapter` is a scene-authored reaction surface. It
evaluates one local `GameObject` against the canonical current Activity; it is
not Activity authority, scene ownership, or Activity materialization.

Shared local UI and scene content need to express positive and negative
visibility for one or more Activities.

Route-scoped visibility is a separate concern.

Route-owned content normally follows scene composition:

```text
Route enters
  -> Primary Scene and Route Content scenes load

Route exits
  -> owned scenes release
```

`RouteContentBinding` identifies and notifies Route-local content; it does not
own visibility. Route-scoped visibility is not part of this decision.

## Decision

`activities` is the only Activity visibility contract. It contains one or more
explicit, valid `ActivityAsset` references and preserves authored order:

```text
Activity Local Visibility Adapter
  Activities
    one or more valid ActivityAsset references

  Match Mode
    Visible When Any Listed Activity Is Active
    Hidden When Any Listed Activity Is Active

  No Active Activity
    Hidden
    Visible
```

The rule evaluates only the canonical current Activity. Its result is
deterministic:

```text
current Activity
+ explicit Activities
+ Match Mode
+ No Active Activity policy
= desired local visibility
```

### Positive mode

```text
Visible When Any Listed Activity Is Active
```

The GameObject is visible when the current Activity matches at least one listed
Activity.

Example:

```text
Activities
  Cows
  Chickens

Result
  visible during Cows
  visible during Chickens
  hidden during any other Activity
```

### Negative mode

```text
Hidden When Any Listed Activity Is Active
```

The GameObject is hidden when the current Activity matches at least one listed
Activity.

Example:

```text
Activities
  Cows

Result
  hidden during Cows
  visible during other Activities
```

`No Active Activity` remains explicit because negative matching alone does not
define visibility before an Activity is active or after Activity clear.

## Authoring rules

The authoring surface remains explicit and designer-readable.

```text
Activities list must contain at least one entry
every entry must be non-null and have a valid Activity identity
duplicate Activity identities are invalid
Match Mode must be supported
No Active Activity behavior must be supported
Local Content Id remains explicit
Requiredness remains explicit
```

The Inspector should present:

```text
Activity Rule
  Match Mode
  Activities
  No Active Activity

Local Content
  Local Content Id
  Requiredness

Validation
  Validate
  compact result

Advanced / Debug
  current Activity
  match result
  resulting visibility
  last diagnostic
```

Normal mode does not expose runtime implementation details or permanent
instructional HelpBoxes. Invalid configuration receives neither fallback nor
mutation: evaluation and validation report the reason, do not repair data or
infer identity, and do not change `GameObject.activeSelf`.

## Runtime rules

The adapter remains a reaction surface, not Activity authority.

It may:

```text
observe the current Activity
evaluate the authored rule
apply GameObject active state idempotently
publish diagnostic evidence
```

It must not:

```text
request or change Activity
load or unload scenes
create fallback Activity identity
infer identity from names or hierarchy
silently repair invalid rules
become canonical Activity materialization
```

The runtime rule must be deterministic:

```text
current Activity
+ explicit Activity list
+ Match Mode
+ No Active Activity policy
= desired local visibility
```

No project search, scene-wide fallback discovery or reflection is required.

## Singular owner contract

`TryGetSingleActivityOwner` returns an owner only when the rule is valid, Match
Mode is `Visible When Any Listed Activity Is Active`, Activities contains exactly
one Activity, and No Active Activity is `Hidden`.

Rules with multiple Activities, negative matching, or No Active Activity set to
`Visible` do not have a singular owner. Consumers that require one use this
method explicitly; existing consumers have been adapted accordingly.

## Serialized instances

Serialized instances created under the previous contract must be reauthored as
an explicit Activities rule. There is no automatic conversion, compatibility
window, or mutation of consumer assets.

## Rejected scope

- `RouteLocalVisibilityAdapter`.
- Generic boolean-expression graphs over Activities.
- Tags, names or hierarchy paths as Activity selectors.
- Wildcard Activity discovery.
- Automatic Activity-list population or serialized-data conversion.
- Adapter-owned Activity requests.
- Adapter-owned scene loading or unloading.
- Hidden fallback when the authored rule is invalid.
- A global visibility manager or service locator.

## Consequences

Activity-local content gains a scalable, explicit rule for shared UI and scene
objects without requiring mirrored single-Activity adapters.

Negative visibility becomes a first-class authoring intent rather than an
implicit workaround.

The Route model remains simpler: Route-owned content continues to rely on scene
composition and release.

A future need for shared cross-Route UI must be evaluated from its actual
ownership and lifetime before introducing a Route-specific visibility adapter.

## Current implementation coverage

Implemented:

```text
Activities-only rule evaluation
positive and negative Match Mode
explicit No Active Activity policy
explicit Local Content Id and Requiredness
non-mutating invalid evaluation and validation
singular-owner query with explicit constraints
Inspector and adapted consumers
```

## Verification evidence

The current-only rule contract is technically verified by the following QA
regressions:

```text
Rule regression: 28 cases
Lifecycle regression: 17 cases, executed twice
Boot/Game Flow baseline: 7 cases, executed twice
```

`CurrentActivitiesEmpty` is the only expected Activity Local Visibility Adapter
warning. It is emitted by the intentional `activities=[]` invalid-rule fixture;
the request result verifies that the rule remains non-mutating and does not
produce lifecycle failures. Any other Activity Local Visibility Adapter warning
is a regression.
