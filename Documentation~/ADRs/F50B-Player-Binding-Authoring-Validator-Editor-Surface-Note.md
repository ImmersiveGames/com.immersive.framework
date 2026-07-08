# F50B — Player Binding Authoring Validator Editor Surface

Status: Accepted after package implementation; QA smoke pending.

## Objective

Expose the F50A passive Player binding authoring validator through an Editor-only surface that can validate the active scene, the selected root, or an explicit root field.

## Scope

- Add an Editor utility for active-scene/root validation.
- Add an EditorWindow under `Immersive Framework/Player Binding/Authoring Validation`.
- Display authoring counts, readiness flags, blocking issues and issue messages.
- Keep the surface read-only and diagnostic-only.

## Out of scope

- View binding.
- Control binding.
- Camera activation.
- Input activation.
- Movement enable/disable.
- Actor spawning.
- Runtime lifecycle.
- FIRSTGAME integration.

## Acceptance criteria

- Editor utility can validate the active scene.
- Editor utility can validate the selected root.
- Editor utility returns explicit diagnostics for missing root.
- Editor window can open without mutating the scene.
- Passive boundary flags remain false.

## Architectural gain

F50B turns the F50A validator into a practical authoring surface without duplicating validation rules or starting binding behavior.
