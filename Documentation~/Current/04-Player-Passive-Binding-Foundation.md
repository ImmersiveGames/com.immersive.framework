# 04 — Player Passive Binding Foundation

Status: **valid technical foundation; not the primary authoring surface**.

F49 established passive contracts, Unity evidence adapters, topology validation, readiness and diagnostics:

```text
PlayerSlot -> PlayerEntry -> PlayerTopology
PlayerView -> PlayerViewTopology
PlayerControl -> PlayerControlTopology
PlayerBindingReadiness -> PlayerBindingDiagnostics
```

This foundation remains valid. `PlayerComposer` was built over and alongside it and is now the main designer-facing Player authoring surface. `CameraComposer` consumes explicit PlayerComposer targets and does not turn `PlayerViewBehaviour` into camera authority.

## Passive boundary

```text
view binding = false
control runtime = false
automatic input activation = false
movement = false
actor spawning = false
```

Historical F49/F50/F51/F52/F53 details are retained in [History/070](../History/070-Player-Binding-and-Composer-History.md) and the ADR archive.

## Next implementation

```text
P2 — Player Control Product
P2A authority audit
P2B recipe and authoring surface
P2C PlayerControl binding adapter
P2D Unity PlayerInput bridge
P2E scoped runtime context
P2F QA
P2G FIRSTGAME proof
```

Do not restore the obsolete Authoring Validator -> PlayerView Binding Adapter -> PlayerControl Binding Adapter queue as the active roadmap.
