# IF-ADR-009 — Activity Local Visibility Rules

Status: Proposed
Last updated: 2026-07-31
Supersedes: none
Superseded by: none

## Context

`ActivityLocalVisibilityAdapter` is a scene-authored adapter that toggles one
local `GameObject` according to the current Activity.

The current authoring contract is intentionally narrow:

```text
one assigned Activity
positive visibility rule
visible only while that Activity is active
```

This works for content that belongs to exactly one Activity, including:

```text
local environment objects
Activity-specific UI
one Activity-specific menu button
local presentation roots
```

FIRSTGAME exposed two recurring cases that the current single positive rule does
not express clearly:

```text
one local object should be visible for several Activities
one local object should be hidden for one or several Activities
```

The menu example makes the limitation visible:

```text
Button Cows
  hidden while Cows is active
  visible while another supported Activity is active

Button Chickens
  hidden while Chickens is active
  visible while another supported Activity is active
```

With only a positive single-Activity binding, two Activities can be handled by
cross-binding each button to the other Activity. That workaround does not scale
and obscures the authored intent.

Route-scoped visibility is a separate concern.

Route-owned content normally follows scene composition:

```text
Route enters
  -> Primary Scene and Route Content scenes load

Route exits
  -> owned scenes release
```

Therefore, a `RouteLocalVisibilityAdapter` is not currently required for normal
Route-owned content. `RouteContentBinding` identifies and notifies Route-local
content; it does not own visibility.

Shared UI that survives or is reused across Routes may need a separate product
policy, but the current FIRSTGAME evidence is not sufficient to introduce a
Route visibility adapter into the official package.

## Decision

The current `ActivityLocalVisibilityAdapter` remains the implemented baseline.

A future explicit product cut may evolve its authoring intent from one positive
Activity reference into one visibility rule:

```text
Activity Local Visibility Adapter
  Activities
    one or more explicit ActivityAsset references

  Match Mode
    Visible When Any Listed Activity Is Active
    Hidden When Any Listed Activity Is Active

  No Active Activity
    Hidden
    Visible
```

The future rule evaluates only the current Activity authority.

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
define whether local content should be visible before an Activity is active or
after Activity clear.

## Authoring rules

The future authoring surface must remain explicit and designer-readable.

```text
Activities list must contain at least one entry
null Activity references are invalid
duplicate Activity references are invalid
Match Mode must be explicit
No Active Activity behavior must be explicit
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

Normal mode must not expose runtime implementation details or permanent
instructional HelpBoxes.

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

## Compatibility and migration

The existing serialized single-Activity contract maps conceptually to:

```text
Activities
  existing Activity

Match Mode
  Visible When Any Listed Activity Is Active

No Active Activity
  Hidden
```

Any serialized migration must occur in a dedicated implementation cut with:

```text
explicit migration plan
prefab and scene compatibility proof
Undo-safe Editor handling where applicable
QA coverage for existing serialized assets
FIRSTGAME verification
```

This ADR does not authorize silent field replacement or automatic mutation of
consumer assets.

## Validation

The future validator must report:

```text
empty Activities list
null Activity entry
duplicate Activity entry
unsupported Match Mode
unsupported No Active Activity policy
missing Local Content Id
nested visibility adapter ownership conflicts
```

Validation remains explicit and non-mutating.

## QA expectations

QAFramework should prove at least:

```text
single positive Activity preserves current behavior
multiple positive Activities use any-match semantics
single negative Activity hides on match
multiple negative Activities hide on any match
No Active Activity Hidden
No Active Activity Visible
duplicate entries fail validation
null entries fail validation
Activity changes update visibility idempotently
Activity clear follows explicit policy
invalid configuration has no silent fallback
```

## FIRSTGAME expectations

FIRSTGAME should prove the feature with real shared local UI:

```text
one button visible in multiple Activities
one button hidden while its target Activity is current
Activity changes update the menu without reloading the Route
Advanced / Debug explains the selected rule and current result
```

The sample must not use the adapter to replace Route scene ownership.

## Rejected scope

- `RouteLocalVisibilityAdapter` in the current decision.
- Generic boolean-expression graphs over Activities.
- Tags, names or hierarchy paths as Activity selectors.
- Wildcard Activity discovery.
- Automatic Activity list population.
- Silent migration of existing serialized assets.
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

Implemented today:

```text
one Activity reference
positive visibility rule
explicit Local Content Id
explicit Requiredness
runtime GameObject activation
authoring validation
```

Not implemented by this ADR:

```text
Activity list
negative match mode
explicit No Active Activity policy
serialized migration
new QA cases
updated FIRSTGAME sample
```

## Pending decisions

- Final enum and field names.
- Whether the current component evolves in place or a new versioned component is
  introduced.
- Serialized migration mechanism and compatibility window.
- Whether `No Active Activity` defaults are allowed or always require explicit
  authored selection.
- Exact debug evidence exposed by the Inspector.
