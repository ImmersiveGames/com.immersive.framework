# IF-ADR-013 — Optional Audio BGM Adapter

Status: **Accepted / Experimental implementation**  
Last updated: 2026-08-10  
Package implementation: **Partial — typed execution evidence gap confirmed**  
Technical QA: **Partial — negative boundary certification required**  
FIRSTGAME: **Not Proven**  
Related decisions: IF-ADR-001, IF-ADR-002, IF-ADR-006, IF-ADR-010, IF-ADR-014  
External provider currently audited: `com.immersive.audio`

> Current mutable implementation, QA and FIRSTGAME status is tracked in
> `../Tracking/IF-TRACK-Framework.md`. This ADR is normative and intentionally
> does not carry a mutable completion percentage. UX observations are qualitative
> product feedback and are not part of functional completion arithmetic.

## Context

BGM integration is optional and may depend on an external audio package. The
framework needs a narrow adapter boundary without making audio-specific authority
part of Route/Activity identity or core lifecycle.

The current external provider is `com.immersive.audio`. Its concrete runtime API
must remain behind the optional framework audio integration boundary. Core
framework runtime must continue to work without that package being installed.

## Decision

The framework exposes a narrow optional BGM integration with Route/Activity
bindings and explicit policies such as own cue, Route cue, retained Activity cue
or silence.

Accepted requests and releases return typed framework-side evidence. `Applied`
or `Released` means that the concrete provider confirmed the corresponding audio
transition; it must not mean only that framework configuration was valid or that
a request was dispatched.

The framework distinguishes desired BGM state from provider-confirmed BGM state.
Only provider-confirmed success may update the confirmed state used by later
no-change decisions, retry behavior, release or restoration.

Absence of the optional audio integration must not corrupt core framework
lifecycle.

## Architectural constraints

- Core lifecycle works when the audio package/adapter is absent.
- Framework Core/Runtime does not reference a concrete audio package.
- Concrete provider types remain isolated inside an optional integration assembly.
- Every accepted request has deterministic release/restoration semantics.
- `Applied` and `Released` require provider-confirmed execution evidence.
- Failed or rejected provider operations do not mutate confirmed framework BGM state.
- Restoration derives from retained confirmed evidence, not from attempted intent.
- Audio-specific identity does not become Route/Activity identity.
- Stable authored identity is not runtime BGM ownership or execution authority.
- Missing required configured audio authority fails explicitly.
- Optional authority absence remains non-corrupting and diagnostic for the core lifecycle.
- No scene search, singleton, service locator or global audio manager is introduced.
- Route/Activity authoring does not become audio runtime authority.
- Runtime evidence remains scoped to the existing Route/Activity BGM integration lifetime.

## Accepted integration model

The accepted boundary is:

```text
Route / Activity authored BGM intent
        ↓
Framework Route / Activity BGM binding
        ↓
Framework BGM director / optional bridge
        ↓
concrete provider request
        ↓
provider execution result
        ↓
framework-side typed execution evidence
        ↓
confirmed BGM state / release / restoration
```

The concrete provider may expose its own result type. The framework bridge
translates that provider-specific result into framework-owned evidence rather
than leaking provider contracts into Framework Core or consumers.

## Second implementation audit — 2026-08-10

A focused audit of the framework package and `com.immersive.audio` confirmed that
the overall architecture is viable and that a narrow technical gap remains.

### Already correct

The following parts of the accepted direction are already present:

- `com.immersive.audio` is not a mandatory dependency of Framework Core/Runtime.
- The concrete integration is isolated in the optional `Immersive.Framework.Audio` boundary.
- The main Framework Runtime assembly does not require audio assemblies.
- Current BGM integration does not require global scene search as runtime authority.
- Route/Activity BGM policies already model explicit intent, including own cue,
  Route fallback/retention, silence and keep-current behavior.
- `com.immersive.audio` already returns typed execution information from its BGM service.
- No new BGM Recipe, Profile, Composer, Wizard or global runtime service is required
  to close the current technical boundary.

These findings mean ADR-013 does not require an audio-system redesign.

### Gap 1 — provider execution evidence is not preserved strongly enough

The concrete audio provider already returns typed operation evidence, but the
framework bridge does not yet preserve an equivalent framework-owned result
through the complete Route/Activity BGM flow.

The framework must be able to distinguish at least the semantic outcomes:

```text
Applied
Released
NoChange
OptionalAuthorityUnavailable
Rejected / Failed
```

Exact type and enum names are implementation details. The invariant is that
framework consumers and diagnostics can distinguish successful provider
execution from accepted configuration or dispatched intent.

### Gap 2 — desired state and confirmed state must be distinct

A requested cue must not become the framework's confirmed current cue until the
provider confirms successful execution.

Required behavior:

```text
compute desired state
        ↓
compare with confirmed state
        ↓
request provider operation when required
        ↓
inspect provider result
        ↓
success  -> commit confirmed state + typed success evidence
failure  -> preserve previous confirmed state + typed failure evidence
```

This distinction is required so a rejected request can be retried and so a
failed request cannot suppress future work through a false `NoChange` decision.

### Gap 3 — release must also be provider-confirmed

Release/stop follows the same rule as apply.

A release request that the provider rejects must not clear confirmed framework
state or report `Released`. Repeated release after an already confirmed empty
state may report `NoChange` without issuing unnecessary provider mutation.

### Gap 4 — restoration must use confirmed evidence

Retention/restoration policies may remember BGM state across Route/Activity
transitions, but the retained state must represent something that was actually
confirmed by the provider.

A cue that was only attempted and rejected must never later become a restoration
target merely because it was the desired authored intent.

Restoration therefore operates from retained confirmed state/evidence and must
not manufacture historical audio state.

## Provider disposition — `com.immersive.audio`

The second audit did not establish a need for a major change in the external
audio package for this cut.

`com.immersive.audio` already exposes BGM operations with typed provider-side
execution results. The first ADR-013 completion cut should therefore adapt and
preserve that evidence inside `Immersive.Framework.Audio` rather than redesign
the provider or expose provider-specific types to Framework Core.

A provider change is justified only if implementation proves that a required
execution fact cannot be obtained through the current provider API.

## Product surface

No new Recipe, Profile, Composer, Wizard or Apply/Rebuild workflow is required
for the current accepted ADR-013 boundary.

Direct Route/Activity BGM authoring remains acceptable while consumers can
express intent without reconstructing hidden runtime contracts.

The expected product model remains:

```text
Route / Activity BGM intent
        ↓
clear policy and cue configuration
        ↓
optional framework audio bridge
        ↓
runtime execution
        ↓
actionable execution diagnostics / Advanced evidence
```

Advanced/debug surfaces may expose provider outcome, requested cue, confirmed
cue, previous confirmed state and last operation evidence where useful. These
technical details must not become the primary designer authoring surface.

## First technical cut — IF-ADR-013A Typed BGM Execution Evidence

### Objective

Make framework BGM state and diagnostics represent provider-confirmed execution
rather than attempted intent.

### Type

Technical / runtime contract completion.

### Scope

The first cut is limited to the existing optional framework audio bridge and the
QA necessary to certify its runtime contract.

Expected implementation areas include:

- framework-owned typed BGM operation evidence;
- translation from the provider result to framework-side outcomes;
- explicit desired-versus-confirmed state handling in the BGM director;
- apply semantics that commit confirmed state only after provider success;
- release semantics that clear confirmed state only after provider success;
- restoration/retention based only on confirmed evidence;
- Route/Activity bindings propagating or exposing the meaningful operation result;
- diagnostics sufficient to explain request, outcome and resulting confirmed state;
- focused negative QA for rejection, retry, release and restoration behavior.

### Out of scope

Do not use this cut to create:

```text
new audio system
AudioManager or global BGM authority
singleton or service locator
BgmRequestHandle ownership architecture unless proven strictly necessary
BgmRuntimeContext or BgmSession unless proven strictly necessary
BgmRecipe
BgmProfile
BgmComposer
Wizard
new generic Apply/Rebuild workflow
major com.immersive.audio redesign
FIRSTGAME authoring redesign
broad Inspector UX rewrite
```

Do not add a second BGM state machine beside the existing director merely to
satisfy terminology in this ADR. Correct the smallest existing authority that
owns the optional integration boundary.

### Required runtime scenarios

The first cut is not complete until the following semantics hold:

```text
A. provider accepts Play
   -> Applied
   -> confirmed state becomes requested cue

B. provider rejects Play
   -> explicit failure
   -> previous confirmed state remains unchanged
   -> a later equivalent request can retry

C. provider accepts Stop/release
   -> Released
   -> confirmed state becomes empty/released

D. provider rejects Stop/release
   -> explicit failure
   -> confirmed state remains unchanged
   -> framework does not claim Released

E. repeated release from already confirmed released state
   -> NoChange or equivalent
   -> no unnecessary provider mutation

F. Activity overrides Route and succeeds
   -> Activity cue becomes confirmed only after provider success

G. Activity override is rejected
   -> previous confirmed Route state remains the framework truth

H. Activity/Route restoration
   -> restoration target derives only from retained confirmed evidence
   -> rejected historical intent is never restored

I. optional integration/authority absent
   -> core lifecycle remains valid
   -> result is explicit and non-corrupting for the supported optional boundary

J. KeepCurrent / equivalent non-mutating policy
   -> no unintended provider operation
```

### Technical QA expected

QAFramework should prove the actual optional bridge contract, not implement a
parallel audio system.

Minimum negative/transition coverage should include:

- provider Play success;
- provider Play rejection;
- retry after rejected Play;
- provider Stop success;
- provider Stop rejection;
- repeated release/idempotent no-change;
- Route-to-Activity override success;
- failed Activity override preserving previous confirmed state;
- Activity exit/restoration from confirmed evidence;
- rejected historical intent excluded from restoration;
- optional provider/authority absence;
- keep-current/no-mutation behavior;
- replacement/teardown behavior where the current director lifetime already
  defines such transitions.

Tests should assert externally meaningful framework evidence and provider calls,
not private implementation trivia.

### Technical acceptance criteria

The first cut is accepted when:

```text
Framework Core/Runtime remains independent of com.immersive.audio
provider-specific result types remain inside the optional bridge
framework exposes typed operation evidence for the supported BGM boundary
Applied means provider-confirmed apply
Released means provider-confirmed release
failed apply does not mutate confirmed state
failed release does not mutate confirmed state
failed equivalent apply can retry
restoration uses confirmed retained evidence only
NoChange does not issue unnecessary provider mutation
optional integration absence does not corrupt core lifecycle
failures are explicit and diagnostic
focused QA covers the negative execution matrix
```

### Product acceptance criteria

The cut must not make normal authoring more complex.

```text
Route/Activity BGM intent remains understandable
no provider-specific runtime type leaks into normal authoring
no hidden global authority is introduced
runtime/debug evidence explains what was requested and what actually happened
Advanced diagnostics do not become the primary authoring workflow
```

### Architectural gain

The framework stops conflating authored/requested intent with audio state that
actually exists in the provider.

This establishes a truthful adapter boundary without creating a new audio
authority inside the framework.

### Usability gain

Failures become explainable: a developer can tell whether a cue was requested,
applied, rejected, released or left unchanged, and can see the confirmed state
used by later restoration decisions.

### Suggested commit message

```text
feat(audio): add typed BGM execution evidence and confirmed-state semantics
```

## Experimental promotion

ADR-013 remains Experimental after the audit because the accepted runtime
boundary still requires implementation and technical certification of the
confirmed-evidence semantics above.

Promotion from Experimental requires, in order:

```text
1. IF-ADR-013A implemented in the package
2. focused QAFramework certification of the supported execution matrix
3. real-game integration in FIRSTGAME proving the supported optional boundary
```

FIRSTGAME is evidence of real integration and usability; it must not be used to
invent permanent framework contracts that belong in the package.

UX polish is not a promotion score. Product improvements may be made from real
use, but Experimental status is about supported integration/contract confidence,
not aesthetic Inspector maturity.

## Current disposition

```text
Architecture: Accepted
Package: Partial — IF-ADR-013A required
QA: Partial — negative execution certification required
FIRSTGAME: Not Proven
Status: Accepted / Experimental implementation
Next: implement IF-ADR-013A, certify technically, then prove real-game integration
```

## Normative summary

```text
Keep audio optional and outside Framework Core authority.
Keep the concrete provider behind the optional bridge.
Treat authored BGM as desired intent, not proof of audio state.
Applied and Released require provider-confirmed execution.
Commit confirmed state only after successful provider operations.
Preserve confirmed state after rejected apply/release so retry remains possible.
Restore only state that was actually confirmed.
Do not add a global audio authority or new authoring architecture to close this gap.
Close the technical boundary in the package and QA before using FIRSTGAME as real integration proof.
```
