# Content Anchor Authoring Inspectors

## Purpose

`RouteContentAnchor` and `ActivityContentAnchor` are passive declarations, but their Inspectors are product authoring surfaces.

This cut aligns both Inspectors with the framework Editor Authoring Standard.

## Default Inspector Order

```text
Product Header
Intent Summary
Primary Authoring
Anchor Intent
Identity
Presentation
Configuration Status
Authoring Validation
Advanced / Debug
```

## Identity

`Anchor ID` remains explicit authoring.

The Inspector provides:

```text
Suggested ID
Use Suggested ID
```

The suggestion:

- is deterministic;
- is derived from scope, owner, Kind and authored component context;
- never writes during repaint;
- never overwrites a populated ID;
- uses Undo, dirty marking and prefab override recording through the shared Inspector helper.

A suggested ID is a starting point. It remains visible and editable before commit.

## Configuration Status

The live status is non-mutating and reports:

```text
Valid
Missing Owner
Invalid Owner Identity
Missing Anchor ID
Invalid Kind
Invalid Requiredness
Invalid Declaration
```

Invalid states show:

```text
issue
impact
corrective action
```

## Explicit Validation

The Inspector no longer runs the full framework authoring validator on every repaint.

The user invokes:

```text
Validate Route Content Anchor
Validate Activity Content Anchor
```

The result becomes outdated when authoring changes.

Validation does not repair data and does not query runtime discovery.

## Advanced / Debug

The default flow hides technical evidence such as:

- fixed Scope;
- owner stable identity;
- normalized Anchor ID;
- GameObject and scene diagnostics;
- resource path;
- canonical declaration diagnostic.

Advanced evidence does not claim runtime discovery, acceptance, mismatch or binding.

## Runtime Boundary

This cut does not change:

- `RouteContentAnchor`;
- `ActivityContentAnchor`;
- discovery scope;
- `ContentAnchorSet`;
- duplicate detection;
- logical binding;
- materialization;
- lifecycle behavior.

`Requiredness` remains authoring-validation intent. It is not Activity Readiness and does not block Route or Activity lifecycle.

## FIRSTGAME Revalidation

After package and QA validation:

1. update the package reference used by `planet-devourer`;
2. remove and re-add the first M04 Route Content Anchor;
3. verify the new product-first Inspector;
4. use the explicit ID suggestion or enter `m04.route.root`;
5. run explicit validation;
6. continue with Activity A anchors only after the Route authoring experience passes.
