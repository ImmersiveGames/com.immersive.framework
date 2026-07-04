# 030 — Guide History

The current public guide is:

```text
Documentation~/Guides/Usage/index.html
```

Old per-phase guides were consolidated to reduce documentation noise. Use this file to map historical guide names to their current location.

## Consolidated guide map

| Historical guide | Current replacement |
|---|---|
| F10C/F10D pause content-anchor binding guides | Current HTML guide Pause sections + current usage map. |
| F10E/F10F/F10G/F10H pause visual/presentation guides | Current HTML guide Pause/UIGlobal sections. |
| F15/F16 reset adapter guides | Current HTML guide Reset sections + `Current/02-Usage-Map.md`. |
| F17 gate foundation guide | Current usage map Gate/PlayerInput gate entries. |
| F18/F19 transition guides | Current HTML guide Transition sections. |
| F20 pause state/gate guide | Current HTML guide Pause + Gate sections. |
| F21 save guide | Current state Save boundary summary. |
| F22 loading guide | Current HTML guide Loading sections. |
| F23 pause content overlay/input guide | Current HTML guide Pause overlay/input sections. |
| FIRSTGAME-2B pause input guide | Current HTML FIRSTGAME flow sections. |
| First Practical Flow Transition | Current HTML FIRSTGAME transition/gate flow. |
| immersive-framework-manual-gamedesigner.html | Replaced by `Guides/Usage/index.html`. |

## Rule for future guides

Prefer updating the current HTML guide and `Current/02-Usage-Map.md` instead of adding another phase-specific guide.

Create a new guide file only when:

```text
1. The subject is too large for the HTML guide.
2. The file is expected to remain current beyond one cut.
3. README.md links it as an active document.
```
