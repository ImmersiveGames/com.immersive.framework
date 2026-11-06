# IF-AUDIT-014-C — Dynamic Object Entry Composition

Status: **FINDINGS** — static audit; independent review findings recorded below
Scope: Framework master a9e379362dd80dd8dc581215a62d331dd62baf14; QAFramework-resolved Composition; Object Entry and Actor Composition boundaries
Related proposal: [IF-ADR-014 Amendment 003](../ADRs/IF-ADR-014-AMENDMENT-003-DYNAMIC-OBJECT-ENTRY-COMPOSITION.md)

## 1. Summary

The smallest coherent evolution is a Framework integration change over the existing generic Composer. The kernel already supplies independent keyed contributions, priorities, dependencies, guarded mutations, batch updates, revisions, deltas and operation outcomes. The missing capability is Framework-controlled access bound to the exact admitted Object Entry occurrence, with safe asynchronous release.

B2 is profile-driven and private: it creates one Composer and Base contribution for an opted-in profile, then delegates materialization and cleanup through StableObjectBindingRegistry. It issues no consumer contribution handles. A profile-less dynamic opt-in cannot use the current authoring validation or runtime-binding constructor unchanged.

No dynamic Object Entry composition was found in the inspected planet-devourer Assets/Packages search. PEGA selection feedback remains a future consumer, not evidence for a gameplay-specific API.

## 2. Baseline and evidence

- The original Framework checkout was clean on master at a9e379362dd80dd8dc581215a62d331dd62baf14.
- Framework PRs [#21](https://github.com/ImmersiveGames/com.immersive.framework/pull/21) and [#22](https://github.com/ImmersiveGames/com.immersive.framework/pull/22) are merged into that baseline. PR #22 reports owner-run 518/518 Edit Mode and 10/10 IF-ADR-014 Play Mode evidence. This is B2 evidence only.
- QAFramework package lock resolves Composition 0.1.0-preview.3 at ef577765d0b565a020f3e98f6e5eaae681333c29. The checked local Composition repository head is newer and is not the QA-resolved source.
- The prior IF-ADR-014-C-proposta.zip and draft Markdown files were unavailable in accessible attachments. These documents are reconstructed from checked-in evidence; no unavailable draft is represented as reviewed.
- This task ran no Unity compile, Edit Mode or Play Mode tests.

### Source contracts

| Source | Verified behavior |
|---|---|
| Runtime/ObjectEntry/ObjectEntryDeclaration.cs; Runtime/RuntimeContent/StableObjectBindingRegistry.cs | Declaration supplies logical identity. The registry owns distinct physical records per owner registration, commit/rollback/release, and an exact-current predicate covering the record and references. |
| Runtime/ObjectEntry/ObjectEntryCompositionAuthoring.cs; ObjectEntryCompositionProfile.cs | B2 authoring requires a Profile. Profile validation requires prefab-backed parts and validates keys/dependencies plus selected nested identity, Actor and Player authority components. |
| Runtime/RuntimeContent/ObjectEntryCompositionRuntimeBinding.cs | Private Composer and Base contribution at int.MinValue. Constructor requires Profile and eagerly creates Composer when the binding runtime is created. No public consumer handle exists. |
| Runtime/RuntimeContent/ObjectEntryCompositionPartMaterializer.cs | Exact-root, prefab-backed Acquired materialization and cleanup. An internal Adopted path is not public authoring authorization. |
| Runtime/RuntimeContent/StableObjectBindingRegistry.cs | Commit binds read-only query anchors. Composition activation follows commit. Owner release invalidates writes, awaits close/reconciliation and retains the owner when cleanup is unresolved. |
| RouteLifecycle/ActivityFlow registration and transaction partials | Existing flows register, commit, activate and release bindings through the route/activity lifecycle barriers before owned scene release. |
| Runtime/ApplicationLifecycle/FrameworkRuntimeHost.cs; FrameworkRuntimeHostAdmission.cs; FrameworkRuntimeHost.ResettableRegistration.cs | Controlled Stop drains admitted operations before Activity clear/Route exit. OnDestroy/OnApplicationQuit invalidate access synchronously but cannot await those async owner-release barriers. |
| Runtime/PlayerParticipation/Runtime/ActorCompositionRuntimeBinding.cs; PlayerActorCompositionContribution.cs | Actor path shows a Framework-owned wrapper, exact-target guard, set/batch/remove/close, result mapping and deltas. Its Actor/Player identity and host are not reusable for Object Entries. |
| QA-resolved CompositionComposer.cs, CompositionContribution.cs, CompositionResult.cs, Composition-Boundary.md | Unique top priority wins; Set/Remove with an equal top-priority conflict reject before effects. Dependencies are graph-validated; guarded Set/Remove re-check external targets. Results expose status, revision, changed keys, issues and EffectsMayHaveApplied. |

## 3. Reusable capabilities and current limitations

### Reusable directly

- One CompositionComposer per physical binding context.
- Framework-generated CompositionOwnerId values for separate contributor intents.
- Guarded SetPart/SetParts/RemovePart operations; per-contribution Close closes intent and requires an exact-target Framework wrapper guard.
- Kernel dependency ordering, priority conflict policy, revisions, results and deltas.
- B2 exact-root guard and prefab-backed Acquired materializer.
- Actor's general wrapper/result-projection pattern, without its identity, Player host, mounts or gameplay assumptions.

### Limitations preventing Object Entry use

1. No public Object Entry write grant exists. B2 keeps the Composer private.
2. ObjectEntryCompositionAuthoring requires a Profile; the B2 runtime binding also requires a Profile and eagerly constructs the Composer/Base.
3. Owner commit precedes async Base activation. IsCommitted alone cannot authorize a consumer grant.
4. A lease must capture the exact binding-record instance. Resolving by ObjectEntryId, owner and GameObject can cross a readmission that reuses all three.
5. CompositionContribution.CloseAsync closes intent, not physical resources. It has no external-target guard and may remove intent before effects. Repeated close can return NoChange after a detach failure. RuntimeContent must own final context close, retry and physical reconciliation.
6. Cancellation and mutation outcomes do not imply generic rollback. EffectsMayHaveApplied, diagnostics and cleanup-pending state must remain observable.
7. A higher-priority winner may hide a tie below it. Closing that winner can expose an equal-priority conflict; Framework must not add a second resolver.
8. Profile validation requires dependencies to be inside that Profile. Dynamic contributions need reusable per-part/prefab checks and Composer graph validation, not the Profile's closed-graph rule on each contribution.
9. Current public authoring is prefab-backed. Public Adopted instances need exact provenance and ownership semantics; an internal materializer branch does not provide that contract.
10. Host Stop currently waits for operations before the releases that may cancel composition work. C3 must cancel/invalidate earlier or prove equivalent progress. Abrupt OnDestroy cannot await current async release.

### Alternatives

| Option | Assessment |
|---|---|
| New ObjectEntryCompositionAccessBinding component beside existing authoring | Avoid initially; it duplicates the per-declaration opt-in and splits authoring/handoff. |
| Extend ObjectEntryCompositionAuthoring with default-off dynamic opt-in/profile-less mode | Smallest coherent Framework path; preserves B2 when false. Requires snapshot, API and Inspector review. |
| Composer creation by consumers or grant from read-only queries | Reject; bypasses occurrence and cleanup authority. |
| Actor conversion or reuse of Actor host/contracts | Reject; couples identity, Player and lifecycle domains. |
| New composition mechanism or Composition-kernel change | Reject; required generic operations already exist; missing capability is Framework authority/integration. |
| Profile-only static composition | Insufficient for independent runtime contributors. |

## 4. Independent review

**Independent review: CLEAR for the revised proposal/documentary reconciliation.** The first pass returned FINDINGS/NEEDS_FIX and its findings were corrected. **Architecture audit status: FINDINGS** because operation-drain and abrupt-teardown gates remain open for future C implementation. Review was read-only against the original Framework checkout and QA-resolved Composition commit ef577765d0b5. No Unity execution was performed.

| Finding and contract | Consequence | Correction / remaining gate |
|---|---|---|
| ObjectEntryCompositionAuthoring validation and registry current checks | Profile is currently mandatory; commit precedes Base readiness; current-root lookup can cross readmission. | Reuse existing authoring with default-off dynamic opt-in. Capture exact binding and grant only after commit and successful Base activation. |
| CompositionComposer contribution/context Close and CompositionContribution.CloseAsync | Close lacks the mutation target guard and can close intent before effects; retry may return NoChange after failed detach. | Guard consumer close against exact live record. Framework invalidates grants and owns context close/retry/reconciliation; preserve original result and diagnostics. |
| Composer precedence, graph validation and CompositionResult | Set/Remove equal top-priority conflicts reject before effects. Close of a higher winner can reveal a hidden tie and degrade; NoChange may represent shadowed intent, not physical effect. | Keep kernel-only resolution, expose status/revision/keys/diagnostic/effect uncertainty, test hidden/revealed ties and rematerialization. |
| Public batch/events | The kernel supports a batch API, but no dynamic Object Entry consumer demonstrates a need for one atomic multi-key update or cross-contributor notifications. | Defer both public surfaces; retain the kernel API internally and reopen this decision when a concrete consumer requires them. |
| Composer asynchronous release and RuntimeContent close barrier | Overlapping close/reconcile can race cleanup. | C3 requires per-binding operation coordination, cancellation/drain and a single close/reconcile barrier. Do not expose mutation before this gate. |
| FrameworkRuntimeHost Stop/admission drain and OnDestroy/OnApplicationQuit | A composition task may prevent Stop reaching owner release; abrupt teardown does not await physical cleanup. | Cancel/invalidate before stop drain and prove progress. Prove abrupt-teardown cleanup under the existing authority, or obtain separate explicit Project Owner approval for any limitation before C certification. |
| Profile validation and Acquired/Adopted materializer paths | Profile-only graph validation does not fit multiple contributions; public adoption would weaken ownership. Contribution intent can be confused with physical ownership. | Share per-part/prefab validation, use kernel graph validation, start with prefab Acquired only, and keep acquired-resource ownership/provenance in the Framework materializer. |
| Actor Composition wrapper | Copying Actor/Player authority or events couples domains. | Reuse only the wrapper/result pattern and exact-target guard; keep grant and owner inside RuntimeContent. |

The reviewer found no need for a new public access component or Composition-kernel change. The revised proposal has the authoring anchor hand out an immutable per-occurrence grant; TryOpenContribution operates on that grant. Profile-only B2 keeps its Composer/Base without dynamic grant. Public batch operations are deferred. Host teardown and operation-drain gates remain unresolved, so this audit is **not CLEAR**.

## 5. Recommendation

Keep the Composition kernel unchanged. Put the evolution in com.immersive.framework: extend the existing per-declaration opt-in safely; have the authoring handoff yield an immutable grant tied to the exact committed binding after Base activation; lazily create a context for profile-less opt-ins; wrap single-key Set/Remove with typed results; defer public batch/events; reuse the B2 Acquired materializer; and let the existing owner-release authority close/reconcile. Keep Player selection and gameplay authorization outside composition.

Before C3 acceptance, prove teardown cleanup under the existing owner authority or obtain separate explicit Project Owner approval for a limitation, and prove cancellation precedes stop's operation drain. C4 must cover stale handles with the same ID/owner/root, hidden/revealed ties, asynchronous failure and pending physical destruction. No C implementation or validation is claimed.
