# F36-ADR-PAUSE-006 - Global Pause Input Action (Historical)

Status: Superseded / Historical
Phase: F36 / FIRSTGAME-2D
Type: Historical runtime and authoring record
Last updated: 2026-07-18

> This ADR is an archive, not current setup guidance. H1 physically removed the
> direct Pause action component and its configuration path. Do not configure or
> restore the historical Global Pause route described by the former revision.

## Historical record

F36 recorded an early direct Pause action route and its action-map policy. That
route is no longer part of the package and this ADR does not prescribe a valid
authoring flow.

## Current guidance

Use the canonical architecture only:

- `ADR-INPUT-0001` - PlayerInput single physical writer.
- `ADR-INPUT-0002` - resident InputMode authority and canonical Pause submitter.
- `ADR-INPUT-0003` - current Unity Input authoring boundary.

H1 does not provide a substitute Pause integration in FIRSTGAME, compatibility
code, a menu surface, or a migration path for the removed direct component.
