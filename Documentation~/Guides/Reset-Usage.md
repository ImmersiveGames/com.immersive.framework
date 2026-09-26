# Reset Usage

Status: Transitional — legacy Reset authoring remains implemented; IF-ADR-035 defines the replacement model
Last updated: 2026-09-26

> **Architecture transition (IF-ADR-035):** The instructions below describe the currently implemented legacy Reset authoring path. They remain valid for existing content during migration, but they are not the target product model. New architecture work must converge on `Resettable -> ResetComposition -> ResetTarget`, with content ownership derived from composition/materialization origin and Reset membership modeled separately. Do not expand the legacy textual-ID/scope/list authoring model.

## Target authoring model (not yet implemented)

```text
Reset capability
  -> Resettable
      -> optional ResetComposition
          -> ResetTarget
              -> existing ResetRegistry / ResetExecutor during migration
```

Normal target authoring does not require textual Reset IDs, per-object ownership scope or explicit subject-ID groups. Stable identity remains opt-in for real cross-boundary specific targeting.

## Legacy operation mapping during migration



| Need | Surface |
|---|---|
| Reset one gameplay object | `ObjectResetTrigger` |
| Reset selected/scoped objects | `ObjectResetGroupTrigger` + `ResetSelectionConfig` |
| Reset objects and restart the current Activity | `ActivityRestartTrigger` |
| Run Route/Activity lifecycle reset participants | `RouteCycleResetTrigger` / `ActivityCycleResetTrigger` |

Object Reset, Cycle Reset, Release, Snapshot and Save are separate operations.

## Author an Object Reset subject

1. Add `UnityResetSubjectAdapter` to the object.
2. Choose an authored stable id or runtime-instance id generation.
3. Choose Route, Activity or Runtime scope.
4. Add built-in Transform/active-state participants as needed.
5. Implement `IUnityResettable` for custom synchronous gameplay state.
6. Configure participant order and requiredness.
7. Point a trigger or selection configuration at the subject/scope.

Bootstrap/lifecycle binds the adapter's explicit Reset registration port.
Although the public registration method is still named
`RegisterWithCurrentHost`, it uses that bound port and performs no static host
lookup.

## Runtime flow

```text
UnityResetSubjectAdapter
-> IResetRegistrationRuntimePort
-> ResetRegistry
-> ResetSelectionConfig
-> ResetExecutor
-> ordered IResetParticipant.Reset
-> typed aggregate result
```

Required participant failure blocks the subject according to execution policy.
Optional failure remains diagnostic. Empty selection succeeds only when
explicitly allowed.

## Cycle Reset and Activity Restart

Cycle Reset executes explicitly registered lifecycle participants; it does not
reload scenes or reset gameplay objects automatically.

Activity Restart composes Object Reset with Activity clear/re-enter under the
canonical lifecycle/transition path. Do not implement it as two unrelated
requests or a scene reload shortcut.

## Diagnose

Inspect subject id, origin, scope, owner, participant order/requiredness,
selection resolution and typed issues. Fix missing explicit port binding or
invalid ownership at its composition owner.

## Manual validation

1. Compile Framework and QAFramework.
2. Run focused registry, executor, Unity adapter, runtime-prefab, Object Reset
   and Activity Restart suites.
3. Confirm runtime subjects receive unique ids and stale owners are cleaned.
4. Confirm required failure blocks and optional failure remains diagnostic.
5. Confirm Activity Restart preserves transition and lifecycle ordering.
