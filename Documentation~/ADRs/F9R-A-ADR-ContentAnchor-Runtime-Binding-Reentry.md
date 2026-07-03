# F9R-A — ContentAnchor Runtime Binding Re-entry

Status: Proposed

Scope: ContentAnchor / Runtime Binding / Materialization Governance.

## Context

`POST-F33-A - Matrix Reconciliation Closeout` and `POST-F33-B - Officialize/Reclassify F28-F33` keep F34/gameplay unauthorized and keep camera, audio, save/progression, pooling/runtime-spawned and actor materialization unselected.

`F8R-A - RuntimeContent / ContentAnchor Materialization Audit` found that ContentAnchor already has identity/vocabulary, Route and Activity authoring declarations, discovery, `ContentAnchorSet`, logical `RuntimeContentAnchorBinding` and `ContentAnchorContentHandle`. It also found that physical placement, transform parenting, prefab attach, overlay root placement and physical lifecycle cleanup are absent.

`F8R-B - Runtime Root / Handle / Release Policy` accepts `RuntimeContentHandle` as logical state, not a Unity object or placement token. `F8R-C - Runtime Materialization Adapter Boundary` accepts physical materialization as a future adapter boundary. `F8R-D - Physical Release Adapter` accepts physical cleanup as a future adapter boundary.

Current package evidence:

- `Packages/com.immersive.framework/Runtime/ContentAnchor/ContentAnchorDeclaration.cs`
- `Packages/com.immersive.framework/Runtime/ContentAnchor/RouteContentAnchor.cs`
- `Packages/com.immersive.framework/Runtime/ContentAnchor/ActivityContentAnchor.cs`
- `Packages/com.immersive.framework/Runtime/ContentAnchor/ContentAnchorDiscoveryRuntime.cs`
- `Packages/com.immersive.framework/Runtime/ContentAnchor/ContentAnchorSet.cs`
- `Packages/com.immersive.framework/Runtime/ContentAnchor/ContentAnchorBindingRequest.cs`
- `Packages/com.immersive.framework/Runtime/ContentAnchor/ContentAnchorBindingResult.cs`
- `Packages/com.immersive.framework/Runtime/ContentAnchor/RuntimeContentAnchorBinding.cs`
- `Packages/com.immersive.framework/Runtime/ContentAnchor/ContentAnchorContentHandle.cs`

## Decision

ContentAnchor runtime binding remains logical in the core.

ContentAnchor binding may associate an explicit logical `RuntimeContentIdentity` / `RuntimeContentHandle` with an explicit logical ContentAnchor declaration. It may validate anchor id, anchor scope, anchor kind, owner identity, runtime scope, requiredness, duplicate declaration diagnostics, stale handles and already-bound content. It may report diagnostics and clean logical bindings for release or scope exit.

Physical placement belongs to a future adapter. ContentAnchor binding must not move, parent, create or destroy Unity objects.

ContentAnchor core must not:

- create `GameObject`;
- move `Transform`;
- parent hierarchy objects;
- call `Object.Instantiate`;
- call `Object.Destroy`;
- load Addressables;
- return objects to pools;
- resolve camera/audio/gameplay placement;
- fabricate functional identity from Unity object name, hierarchy path, scene path or asset path.

No consumer can depend on physical placement until a specific adapter is accepted and implemented in a later explicit cut.

## Consequences

Positive:

- ContentAnchor core stays aligned with the accepted RuntimeContent logical ownership model.
- Route and Activity anchor authoring remain passive declarations until adapter boundaries are accepted.
- Future physical placement can be planned without changing logical binding identity.
- Consumers remain blocked from inferring physical Unity behavior from logical handles.

Tradeoffs:

- Logical binding success does not mean content is visible, parented, placed, spawned or cleaned physically.
- Required anchors remain diagnostic policy unless a later lifecycle gate accepts stronger behavior.
- Camera, audio, gameplay, actor materialization, pooling/runtime-spawned and save/progression cannot consume physical placement yet.
- A later placement adapter plan is required before implementation.

## Non-Goals

- No runtime implementation.
- No physical placement.
- No concrete materializer.
- No concrete release adapter.
- No `Instantiate`.
- No `Destroy`.
- No Addressables.
- No pooling.
- No actor spawn.
- No camera, audio, save/progression or gameplay consumer.
- No F34.
- No scene, prefab, asmdef or code changes.

## Rejected Alternatives

| Alternative | Rejection reason |
|---|---|
| Let `RuntimeContentAnchorBinding` parent or move transforms. | This would put physical placement in logical ContentAnchor core and bypass the future placement adapter boundary. |
| Treat `ContentAnchorContentHandle` as a placement token or Unity object reference. | It would contradict F8R-B/C, where handles remain logical and physical evidence stays in adapters. |
| Let missing anchors create fallback scene objects. | Fallback anchors hide authoring errors and fabricate identity from runtime scene state. |
| Use Unity object names, hierarchy paths or scene paths as functional binding identity. | They are diagnostic text only; functional identity must come from explicit ids, owners and scopes. |
| Let required anchors block lifecycle in this cut. | Current requiredness is authoring/diagnostic policy; lifecycle blocking requires a separate accepted gate decision. |
| Use logical binding cleanup as physical release. | F8R-D keeps `Destroy`, pool return, Addressables release and scene unload outside core. |
| Allow consumers to depend on placement before adapter acceptance. | This would unblock camera/audio/gameplay/pooling/actor materialization without accepted physical ownership. |

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
- `Packages/com.immersive.framework/Documentation~/ADRs/F8R-D-ADR-Physical-Release-Adapter.md`
- `Assets/_Documentation/Plans/F9R-A-PLAN-ContentAnchor-Runtime-Binding-Reentry.md`
