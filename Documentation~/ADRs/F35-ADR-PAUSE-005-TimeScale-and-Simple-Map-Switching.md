# F35-ADR-PAUSE-005 - TimeScale and Simple Map Switching (Historical)

Status: Superseded / Historical
Phase: F35 / FIRSTGAME-2C
Type: Historical runtime and authoring record
Last updated: 2026-07-18

> This ADR is an archive, not current setup guidance. Its former direct Pause
> input and action-map switching path was physically removed in H1. Do not
> recreate that path or use this document as a configuration guide.

## Historical record

F35 recorded an early FIRSTGAME Pause experiment that combined basic simulation
pause with direct PlayerInput map switching. The direct input authoring route is
no longer available and this ADR does not define current Pause integration.

## Current guidance

Use the canonical architecture only:

- `ADR-INPUT-0001` - PlayerInput single physical writer.
- `ADR-INPUT-0002` - resident InputMode authority and canonical Pause submitter.
- `ADR-INPUT-0003` - current Unity Input authoring boundary.

No direct map-switching setup, compatibility bridge, menu entry, or FIRSTGAME
replacement is introduced by H1.
