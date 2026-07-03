# F8R-D — Physical Release Adapter

Status: Accepted

Scope: RuntimeContent / Physical Release Adapter Boundary

## Context

`F8R-A — RuntimeContent / ContentAnchor Materialization Audit` found that RuntimeContent and ContentAnchor currently provide logical/experimental runtime contracts, not a complete physical materialization and release lane.

`F8R-B — Runtime Root / Handle / Release Policy` accepts root, handle and release policy as logical framework language. `RuntimeReleasePolicy` controls handle state and registry cleanup only; it does not destroy objects, unload scenes, return pools or release Addressables handles.

`F8R-C — Runtime Materialization Adapter Boundary` accepts that physical materialization belongs to future adapters. Pure RuntimeContent core keeps request/result/identity/diagnostics and must not reference `UnityEngine.GameObject`, `Transform`, `Object.Instantiate`, `Object.Destroy`, Addressables or Pooling.

Current package evidence:

- `Packages/com.immersive.framework/Runtime/RuntimeContent/RuntimeReleaseRequest.cs`
- `Packages/com.immersive.framework/Runtime/RuntimeContent/RuntimeReleasePolicy.cs`
- `Packages/com.immersive.framework/Runtime/RuntimeContent/RuntimeReleaseResult.cs`
- `Packages/com.immersive.framework/Runtime/RuntimeContent/RuntimeReleaseStatus.cs`
- `Packages/com.immersive.framework/Runtime/RuntimeContent/IRuntimeReleaseAdapter.cs`
- `Packages/com.immersive.framework/Runtime/RuntimeContent/RuntimeContentRuntime.cs`

## Decision

RuntimeContent core keeps release as logical request/result/policy language.

The core owns:

- `RuntimeReleaseRequest`
- `RuntimeReleasePolicy`
- `RuntimeReleaseResult`
- logical owner, scope and identity
- logical handle state and registry diagnostics

Physical release belongs to future adapters. `Object.Destroy`, pool return, Addressables release and scene unload stay outside RuntimeContent core.

A future Unity physical release adapter may destroy only objects that were physically created and owned by an accepted Unity materialization adapter. A future Pooling adapter may return only objects leased by the accepted pooling boundary. A future Addressables adapter may release only resources loaded by the accepted Addressables boundary. Scene unload or scene-owned content release must use a separate scene lifecycle boundary if approved later.

No consumer may depend on physical cleanup until this accepted adapter boundary is implemented in a later explicit cut.

Unity names, hierarchy paths, scene paths and asset paths are diagnostics only. Functional release identity must come from explicit ids, owners, scopes and adapter-owned physical evidence.

## Consequences

- RuntimeContent core remains pure/logical and does not absorb Unity cleanup behavior.
- Future release adapters can map logical release requests to physical cleanup without changing `RuntimeReleasePolicy`.
- Destroy, Pooling, Addressables and scene unload semantics stay independently reviewable.
- Consumers remain blocked from assuming physical cleanup for camera, audio, save/progression, gameplay, pooling/runtime-spawned or actor materialization.
- Physical release diagnostics must distinguish logical release, adapter execution and physical evidence.
- Missing physical evidence, stale scope, owner mismatch and adapter failures must be explicit outcomes, not silent fallbacks.

## Non-Goals

- No runtime implementation.
- No materializer.
- No `Destroy` call.
- No pool return implementation.
- No Addressables release implementation.
- No scene unload implementation.
- No actor spawn.
- No camera, audio, save, progression or gameplay consumer.
- No ContentAnchor physical placement.
- No F34.

## Rejected Alternatives

| Alternative | Rejection reason |
|---|---|
| Let `RuntimeReleasePolicy` execute `Object.Destroy`. | It would move Unity physical cleanup into pure core and contradict F8R-B/F8R-C. |
| Store physical object references on `RuntimeContentHandle`. | It would make logical handles represent Unity objects, pooled instances or Addressables handles. |
| Treat `MarkReleasedAndUnregister` as scene unload. | Registry cleanup is not scene lifecycle ownership. |
| Destroy pooled objects by default. | Pooling ownership and active lease semantics require a separate adapter decision. |
| Release Addressables from core based on resource strings. | Addressables handles and load/release ownership belong to an optional adapter boundary. |
| Search by Unity name, hierarchy path or scene path when physical evidence is missing. | Names and paths are diagnostics only, not functional identity. |
| Let consumers assume physical cleanup before adapter implementation. | That would unblock camera/audio/save/gameplay/pooling/actor materialization without accepted ownership. |

## References

- `Assets/_Documentation/Audits/F8R-A-RuntimeContent-ContentAnchor-Materialization-Audit.md`
- `Assets/_Documentation/Plans/F8R-B-PLAN-Runtime-Root-Handle-Release-Policy.md`
- `Assets/_Documentation/Notes/F8R-B1-Runtime-Root-Handle-Release-Policy-Acceptance.md`
- `Packages/com.immersive.framework/Documentation~/ADRs/F8R-B-ADR-Runtime-Root-Handle-Release-Policy.md`
- `Assets/_Documentation/Plans/F8R-C-PLAN-Runtime-Materialization-Adapter-Boundary.md`
- `Assets/_Documentation/Notes/F8R-C1-Runtime-Materialization-Adapter-Boundary-Acceptance.md`
- `Packages/com.immersive.framework/Documentation~/ADRs/F8R-C-ADR-Runtime-Materialization-Adapter-Boundary.md`
- `Assets/_Documentation/Plans/F8R-D-PLAN-Physical-Release-Adapter.md`
- `Assets/_Documentation/Notes/F8R-D1-Physical-Release-Adapter-Acceptance.md`
