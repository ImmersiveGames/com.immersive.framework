# C9M — Stale Camera Request Correction

Status: implementation delivered; Unity QA and FIRSTGAME rerun pending.

## Incident

FIRSTGAME Route exit exposed an admitted Camera request whose Unity
`CameraRigComposer` had already been destroyed. Releasing another owner then
selected that stale request, output application rejected the invalid winner,
and rollback could not re-admit the already-invalid state.

## Correction

`CameraOutputContext.Release` now removes stale invalid admitted requests
before selecting the next winner. Every removed request emits the explicit
non-blocking issue:

```text
camera.output-context.stale-request-pruned
```

The issue and diagnostic summary propagate through `CameraOutputSession` and
the scoped publisher result. This is explicit stale-state cleanup, not a
silent Camera fallback. No output, rig or winner is discovered automatically.

## QA

The C9D runtime fixture now admits a valid winner and a lower request, destroys
the lower request's composer, and releases the winner. Acceptance requires:

```text
release succeeds
stale request is pruned
warning is present
no winner remains
admitted request count is zero
no rollback failure occurs
```

## FIRSTGAME

The C9M controller now renders visible Game View controls in addition to its
keyboard shortcuts. Runtime PASS remains pending user-provided Unity logs and
visual evidence.
