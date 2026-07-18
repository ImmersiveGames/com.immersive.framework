# F34-ADR-PAUSE-004 - Direct Pause Input Trigger (Historical)

Status: Superseded / Historical
Phase: F34 / FIRSTGAME-2B
Type: Historical runtime and authoring record
Last updated: 2026-07-18

> This ADR is an archive, not current setup guidance. H1 physically removed the
> direct Pause input component it records. Do not add, configure, restore, or
> reference that component.

## Historical record

F34 recorded an early direct keyboard-to-Pause path for FIRSTGAME. It predated
the resident InputMode authority and canonical Pause submitter architecture.
That implementation no longer exists in the package or in FIRSTGAME.

## Current guidance

Use the canonical architecture only:

- `ADR-INPUT-0001` - PlayerInput single physical writer.
- `ADR-INPUT-0002` - resident InputMode authority and canonical Pause submitter.
- `ADR-INPUT-0003` - current Unity Input authoring boundary.

No compatibility surface, menu entry, migration shim, or alternate FIRSTGAME
Pause integration is provided by this historical ADR.
