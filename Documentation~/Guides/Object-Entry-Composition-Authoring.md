# Object Entry composition authoring — B1 (not runtime-enabled)

Status: **Experimental / authoring-only**. Decision: [IF-ADR-014 Amendment 002](../Architecture/ADRs/IF-ADR-014-AMENDMENT-002-OBJECT-ENTRY-PHYSICAL-COMPOSITION.md). No runtime composition, contribution API or lifecycle integration is implemented by this cut.

## Purpose

An Object Entry remains a passive, owner-admitted identity. Composition is optional. Ordinary scene-authored Object Entries with only `ObjectEntryDeclaration` continue without a Composer, while an opted-in declaration can reference a reusable `ObjectEntryCompositionProfile`.

**Do not rely on this authoring to instantiate or remove parts in a running game yet.** It only serializes and validates an intended configuration. Runtime behavior belongs to the subsequent integration cut.

## Authoring

1. Add/configure `ObjectEntryDeclaration` on the exact object that owns the logical identity; generate a valid Object Entry ID using the existing inspector.
2. Optionally add `ObjectEntryCompositionAuthoring` to that **same GameObject**. It does not assign identity or lifecycle ownership.
3. Create an asset through **Assets > Create > Immersive Framework > Object Entry > Composition Profile** and reference it in the component.
4. Configure one or more keyed prefab-backed parts in the profile. Each part has a unique `key`, a prefab and optional dependencies referencing other part keys in that profile.
5. Review the validation result in both the profile and component Inspectors. Missing profile, invalid identity, invalid/duplicate keys, missing prefabs, missing/duplicate dependencies, dependency cycles and forbidden nested authority components are reported as errors.

The first authored part descriptor is **prefab-backed**. It represents a subordinate object that a later materializer may install under the existing declaration root; it does not replace the root or its identity. Parts are not general-purpose scene managers or new Object Entries. Directly authored scene components do not require composition.

## Boundaries

- `ObjectEntryDeclaration` owns identity metadata only. `ObjectEntryCompositionAuthoring` and the profile are optional and separate.
- The profile cannot produce a Framework owner, a committed physical binding or permission to mutate an occurrence.
- Nested prefabs with `ObjectEntryDeclaration`, `ObjectEntryCompositionAuthoring`, `ActorDeclaration`, `PlayerActorRuntimeHost` or `PlayerInput` are deliberately rejected for this narrow first authoring surface.
- Effective physical coverage is Route/Activity only when the runtime integration is implemented. Session and dynamic Object Entry admission are excluded.
- The composition kernel remains `com.immersive.composition`, engine-free; no Actor composition assets were changed.

## Validation status

**B1:** authoring and Editor validation implemented; 10 Edit Mode regression tests authored in `Tests/Editor/CameraAuthoring/ObjectEntryCompositionAuthoringTests.cs` for explicit opt-in, valid/invalid parts, dependency validation and forbidden identity components. **Tests have not been executed**; Unity Editor compilation and Inspector checks **not executed**.  
**B2:** runtime materialization/commit/rollback/release **not implemented**.  
**C:** live contribution access and full lifecycle guards **not implemented**.  
**D:** QAFramework tests, integration and technical certification **not executed**.

Minimum QAFramework follow-up: validate absence of opt-in, valid profile, malformed keys, duplicate/missing dependencies, cycle, invalid declaration, missing profile and forbidden prefab components. Runtime tests must not be considered green until B2 and C are integrated and verified.
