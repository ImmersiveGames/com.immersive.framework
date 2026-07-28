# Editor Authoring Standard

## Purpose

Immersive Framework Inspectors are product surfaces. They let a designer configure intent and understand readiness without reading logs, runtime port names, generated identities or registry handles.

This guide implements the product-authoring direction in `IF-ADR-002`, the Pause/Reset boundary in `IF-ADR-005`, and persistent composition rules in `IF-ADR-008`.

## Standard order

Use the applicable sections in this order:

1. Product Header — public responsibility, in product language.
2. Intent Summary — a read-only sentence derived from current authoring.
3. Primary Authoring — only fields required for the active mode.
4. Identity — explicit authored identity and explicit generation action.
5. Request Metadata — Source and Reason when that component owns serialized metadata.
6. Configuration Status — concrete issue, impact and corrective action.
7. Runtime Binding and Runtime Evidence — read-only and Play Mode only.
8. Play Mode Action — invokes the same public UnityEvent method.
9. Advanced / Debug — typed identities, raw results and technical diagnostics.

## Identity, Source and Reason

Identity names a stable authored concept such as a Reset Group, Subject, Player Slot or Actor. Source identifies the public request surface. Reason describes this particular request. They are not interchangeable and no Editor copies one into another.

Generation is always explicit. A suggestion is deterministic, readable and derived from the authored object and its domain. It never runs during repaint, does not replace non-empty values, records Undo, marks the object dirty and records prefab overrides. Regeneration of a populated identity requires a separate confirmed workflow; normal Editors only fill missing values.

## Validation and runtime evidence

Validation is non-mutating and is initiated explicitly. Repaint only displays the last evidence. Inspectors do not find a runtime host, bind a port, admit a Player, register a Reset Subject or interpret Console output.

Runtime diagnostics are read-only. `Unbound` must explain which official Scene Lifecycle composition owns the binding. Player defaults show the authored Slot, Actor and Host; typed IDs, tokens and raw evidence remain under Advanced / Debug.

## Play Mode actions

An Inspector action is disabled while its trigger is unbound or already in flight. It calls the public method used by UnityEvent, never a runtime port directly. Edit Mode has no functional runtime action.

## Authoring safety checklist

- Use exact serialized property names from the current component contract.
- Do not use reflection, scene search, fallback binding or automatic repair.
- Do not mutate data during repaint or validation.
- Use `Undo`, dirty marking and prefab override recording for every explicit mutation.
- Keep Editor-only code in the Editor assembly and keep runtime independent of Editor APIs.
- Put operational detail in Advanced / Debug, not the default flow.
