# F8R-C — Runtime Materialization Adapter Boundary

Status: Accepted

Scope: RuntimeContent / Materialization Adapter Boundary

## Context

`F8R-A — RuntimeContent / ContentAnchor Materialization Audit` found that the package has logical RuntimeContent and ContentAnchor contracts, but no approved physical materializer, no prefab instantiation adapter, no Addressables adapter, no pooling adapter, no physical release adapter and no ContentAnchor physical placement.

`F8R-B — Runtime Root / Handle / Release Policy Plan` and `F8R-B1 — Runtime Root / Handle / Release Policy Acceptance` accepted that RuntimeContent core owns root, handle and release policy as logical framework language only. Physical root, placement, instantiate, destroy, pool return, scene unload and Addressables release stay outside core.

Current package evidence:

- `Packages/com.immersive.framework/Runtime/RuntimeContent/RuntimeMaterializationRequest.cs`
- `Packages/com.immersive.framework/Runtime/RuntimeContent/RuntimeMaterializationResource.cs`
- `Packages/com.immersive.framework/Runtime/RuntimeContent/RuntimeMaterializationResult.cs`
- `Packages/com.immersive.framework/Runtime/RuntimeContent/IRuntimeMaterializationAdapter.cs`
- `Packages/com.immersive.framework/Runtime/RuntimeContent/RuntimeContentRuntime.cs`
- `Packages/com.immersive.framework/Runtime/ContentAnchor/ContentAnchorBindingRequest.cs`
- `Packages/com.immersive.framework/Runtime/ContentAnchor/RuntimeContentAnchorBinding.cs`

## Decision

The pure RuntimeContent core keeps request, result, identity, owner, scope, handle state, guard state and diagnostics.

The future Unity materialization adapter is the first authorized boundary for physical materialization, but only after a later accepted implementation cut. That adapter may interpret a `RuntimeMaterializationRequest` and `RuntimeMaterializationResource`, perform Unity-side physical work inside its own layer and return a canonical `RuntimeMaterializationResult`.

RuntimeContent core must not reference or depend on:

- `UnityEngine.GameObject`
- `UnityEngine.Transform`
- `Object.Instantiate`
- `Object.Destroy`
- Addressables handles
- Pooling-owned instance references

No consumer can depend on physical materialization until this accepted adapter boundary is implemented in a later explicit cut. `RuntimeContentHandle` remains logical and must not be treated as a `GameObject`, `Transform`, prefab instance, pooled instance, Addressables handle or placement token.

Physical object evidence may exist only inside the future adapter boundary. It must not leak into pure core contracts.

## Consequences

- RuntimeContent core stays pure and remains safe for `noEngineReferences` assembly boundaries.
- Materialization can later be implemented by one or more adapters without changing logical ownership.
- Addressables and Pooling remain optional adapter choices, not implicit core dependencies.
- Consumers remain blocked from camera, audio, save/progression, gameplay, pooling/runtime-spawned and actor materialization work until an explicit implementation cut is accepted.
- Diagnostics must separate request creation, adapter execution, handle registration and release.
- Unity names, hierarchy paths, scene paths and asset paths remain diagnostic text only, not functional identity.

## Non-Goals

- No runtime implementation.
- No prefab materializer.
- No Addressables adapter.
- No pooling adapter.
- No physical release adapter.
- No actor spawn.
- No camera, audio, save, progression or gameplay consumer.
- No ContentAnchor physical placement.
- No F34.

## Rejected Alternatives

| Alternative | Rejection reason |
|---|---|
| Put `GameObject` or `Transform` on `RuntimeContentHandle`. | It would make logical core depend on physical Unity objects and contradict F8R-B/B1. |
| Implement a prefab materializer in this cut. | This cut is docs-only and only proposes the adapter boundary. |
| Make Addressables the first canonical materialization path. | Addressables is optional and would introduce package-specific lifecycle ownership before the core boundary is accepted. |
| Make Pooling the first canonical materialization path. | Pooling is optional and release semantics need a separate adapter plan. |
| Let ContentAnchor binding perform physical placement now. | F9R-A has not re-entered binding semantics after F8R-B/B1 and F8R-C. |
| Use Unity names, hierarchy paths or asset paths as fallback identity. | Current identity policy requires explicit ids, owners, scopes and resource keys. |
| Let consumers call or assume physical materialization before acceptance. | That would unblock gameplay/camera/audio/save/pooling/actor materialization without the required adapter decision. |

## References

- `Assets/_Documentation/Audits/F8R-A-RuntimeContent-ContentAnchor-Materialization-Audit.md`
- `Assets/_Documentation/Plans/F8R-B-PLAN-Runtime-Root-Handle-Release-Policy.md`
- `Assets/_Documentation/Notes/F8R-B1-Runtime-Root-Handle-Release-Policy-Acceptance.md`
- `Assets/_Documentation/Plans/F8R-C-PLAN-Runtime-Materialization-Adapter-Boundary.md`
- `Assets/_Documentation/Notes/F8R-C1-Runtime-Materialization-Adapter-Boundary-Acceptance.md`
- `Packages/com.immersive.framework/Documentation~/ADRs/F8R-B-ADR-Runtime-Root-Handle-Release-Policy.md`
