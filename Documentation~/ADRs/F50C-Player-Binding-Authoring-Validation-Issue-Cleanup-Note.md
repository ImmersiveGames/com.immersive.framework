# F50C — Player Binding Authoring Validation Issue Cleanup

Status: Accepted / package-first / QA-first.

## Objective

Reduce authoring validation noise without hiding technical evidence.

F50A proved the validator can build the passive chain and propagate topology, readiness and diagnostic issues. That was correct, but negative authoring cases produced too many repeated derived messages in the default report string.

## Scope

F50C keeps the complete issue list and adds root-cause classification:

```text
PlayerBindingAuthoringValidationReport.Issues             complete technical issue list
PlayerBindingAuthoringValidationReport.RootCauseIssues    prioritized authoring/root causes
PlayerBindingAuthoringValidationReport.DerivedIssues      propagated topology/readiness/diagnostic evidence
```

The default `ToDiagnosticString()` now prioritizes root causes and reports the number of suppressed derived issues.

`ToDetailedDiagnosticString()` keeps the full expanded output for deep technical inspection.

## Out of scope

F50C does not:

```text
bind PlayerView
bind PlayerControl
activate cameras
activate Unity Input
switch action maps
enable movement
spawn actors
create runtime lifecycle
integrate FIRSTGAME
```

## Classification rule

Primary authoring evidence problems are root causes:

```text
missing required component
slot declaration/occupancy issue
PlayerSlotSet issue
snapshot creation failure
```

Topology issues become root causes only when no more direct authoring issue is present.

Readiness and diagnostic errors are derived when an upstream authoring/topology issue exists.

## Acceptance

QA must prove:

```text
valid authoring has no root or derived issues
missing PlayerSlotDeclaration exposes a single root cause and keeps derived details available
missing PlayerSlotOccupancy exposes a single root cause and keeps derived details available
topology-only failures remain visible as root causes
default diagnostics suppress derived issue spam
detailed diagnostics preserve derived issues
boundary remains passive
```

## Architectural gain

Authoring validation becomes usable by humans before binding real behavior. The framework keeps precise technical evidence while presenting a clean cause-first summary.
