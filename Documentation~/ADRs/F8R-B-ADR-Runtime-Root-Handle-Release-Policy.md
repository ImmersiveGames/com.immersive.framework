# F8R-B — Runtime Root / Handle / Release Policy

Status: Accepted

Scope: RuntimeContent / Materialization Governance.

## Context

`POST-F33-A — Matrix Reconciliation Closeout` and `POST-F33-B — Officialize/Reclassify F28-F33` keep F34/gameplay unauthorized and keep camera, audio, save/progression, pooling/runtime-spawned and actor materialization unselected.

`F8R-A — RuntimeContent / ContentAnchor Materialization Audit` found that `RuntimeContent` and `ContentAnchor` exist as logical/experimental runtime layers, but physical materialization is not implemented. The package has logical roots, typed runtime identities, runtime content handles, materialization request/result contracts, release request/result/policy contracts and logical Content Anchor binding. It does not have a concrete prefab materializer, Unity hierarchy placement, `Instantiate`, `Destroy`, pooling, Addressables, actor spawn or consumer-safe physical ownership.

Relevant code:

- `Packages/com.immersive.framework/Runtime/RuntimeContent/RuntimeScopeRoot.cs`
- `Packages/com.immersive.framework/Runtime/RuntimeContent/RuntimeRootRegistry.cs`
- `Packages/com.immersive.framework/Runtime/RuntimeContent/RuntimeContentHandle.cs`
- `Packages/com.immersive.framework/Runtime/RuntimeContent/RuntimeContentOwner.cs`
- `Packages/com.immersive.framework/Runtime/RuntimeContent/RuntimeReleasePolicy.cs`
- `Packages/com.immersive.framework/Runtime/RuntimeContent/RuntimeReleaseRequest.cs`
- `Packages/com.immersive.framework/Runtime/RuntimeContent/RuntimeReleaseResult.cs`
- `Packages/com.immersive.framework/Runtime/RuntimeContent/IRuntimeMaterializationAdapter.cs`
- `Packages/com.immersive.framework/Runtime/RuntimeContent/IRuntimeReleaseAdapter.cs`
- `Packages/com.immersive.framework/Runtime/ContentAnchor/RuntimeContentAnchorBinding.cs`

## Decision

RuntimeContent core keeps root, handle and release as logical framework language.

The core owns:

- typed `RuntimeContentOwner` and `RuntimeContentIdentity`;
- logical `RuntimeScopeRoot` and `RuntimeRootRegistry`;
- `RuntimeContentHandle` state transitions;
- `RuntimeMaterializationRequest` / `RuntimeMaterializationResult` contracts;
- `RuntimeReleaseRequest` / `RuntimeReleaseResult` contracts;
- logical release policy names and state/registry effects.

Future Unity adapters may materialize physical objects, but only after a later accepted implementation cut. Physical root creation/resolution, transform placement, prefab instantiation, destroy, pool return, scene unload and Addressables release remain outside RuntimeContent core.

No consumer may assume `GameObject`, `Transform`, spawned prefab, pooled instance, Addressables handle or physical Content Anchor placement from a `RuntimeContentHandle` until the relevant physical adapter plan is accepted and implemented.

`RuntimeReleasePolicy` remains logical:

- `MarkReleasedOnly`: mark the handle released and keep it registered for diagnostics or later owner cleanup.
- `MarkReleasedAndUnregister`: mark the handle released and unregister it from the logical root.

Neither policy performs physical cleanup by itself.

## Consequences

Positive:

- RuntimeContent core stays pure/logical and does not absorb Unity adapter behavior.
- Route, Activity, Session and Transient ownership remain typed and explicit.
- Future physical materialization can be added without changing the meaning of existing handles.
- Consumers remain blocked until physical ownership is accepted instead of inferring runtime object lifetime from logical contracts.

Tradeoffs:

- `RuntimeContentHandle` cannot be used as a Unity object reference.
- A later adapter plan is required before prefab materialization or physical release.
- QA smokes that synthesize materialization results remain evidence for logical contracts only.
- Content Anchor binding remains logical until a later F9 plan defines physical placement.

## Non-goals

- No runtime implementation.
- No concrete materializer.
- No `Instantiate`.
- No `Destroy`.
- No Addressables load/release.
- No pooling rent/return.
- No actor spawn.
- No `PlayerInputManager.JoinPlayer`.
- No camera, audio, save/progression or gameplay consumer.
- No automatic Content Anchor physical placement.
- No scene, prefab, asmdef or code changes.
- No F34 selection.

## Rejected Alternatives

| Alternative | Rejection reason |
|---|---|
| Make `RuntimeScopeRoot` a Unity `GameObject` / `Transform` owner now. | This would move physical hierarchy policy into core before materializer and placement semantics are accepted. |
| Store Unity object references in `RuntimeContentHandle`. | This would make core Unity-bound and blur logical identity with physical object lifetime. |
| Let `RuntimeReleasePolicy` call `Destroy`, pool return or Addressables release directly. | Physical cleanup belongs to adapters and requires separate ownership policy. |
| Allow consumers to bind directly to Content Anchor transforms before runtime materialization is accepted. | This would bypass runtime ownership and recreate ad hoc materialization. |
| Treat QA smokes as proof of physical materialization. | Current diagnostics validate logical request/result/release/binding paths only. |
| Allow fallback root creation from adapters. | Silent fallback roots would hide ownership errors and conflict with explicit owner identity. |

## References

- `Assets/_Documentation/Audits/F8R-A-RuntimeContent-ContentAnchor-Materialization-Audit.md`
- `Assets/_Documentation/Plans/F8R-B-PLAN-Runtime-Root-Handle-Release-Policy.md`
- `Assets/_Documentation/Plans/POST-F33-PLAN-Matrix-Reconciliation.md`
- `Assets/_Documentation/Notes/POST-F33-A-Matrix-Reconciliation-Closeout.md`
- `Assets/_Documentation/Notes/POST-F33-B-Officialize-Reclassify-F28-F33.md`
- `Assets/_Documentation/Notes/Capability-Traceability-Matrix.md`
- `Assets/_Documentation/Notes/Package-System-XRay-Consolidated.md`
