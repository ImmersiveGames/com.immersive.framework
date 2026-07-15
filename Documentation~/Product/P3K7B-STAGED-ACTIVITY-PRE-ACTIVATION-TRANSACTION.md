# P3K.7B — Staged Activity Pre-Activation Transaction

Status: **closed by QA; ordering constraint refined by P3K.7C**  
Type: **runtime transaction + real P3J/P3K resolver + rollback**

## Objective

```text
current Activity remains untouched
-> create target Activity staging scope through explicit lifecycle adapter
-> resolve projected Player preparation and P3K.2-P3K.5 chain
-> evaluate through P3K.6/P3K.7A
-> Proceed: hand off a commit handle
-> Await/Reject/Failure: rollback chain and staging scope
```

## Transaction boundary

`ActivityPlayerAdmissionStageRuntimeContext` owns at most one active stage. It
never invokes `GameFlowRuntime` or `ActivityFlowRuntime` and performs no scene or
content lifecycle publication.

A successful stage ends at `ReadyToCommit`. `TryCommit` transfers ownership into
an internal `ActivityPlayerAdmissionStageCommit` handle. The future lifecycle
integration must either complete that handle after Activity entry owns the staged
scope or roll it back before any previous Activity teardown.

A completed handle remains the Activity-owned release lease. On Activity exit,
`TryRelease` reverses the retained Player chain and then releases the staged
scope. Completed release is idempotent; pre-completion rollback and post-completion
release are distinct operations.

## Real resolver

`ActivityPlayerGameplayChainStageResolver` coordinates the existing authorities:

```text
P3J selection and preparation endpoint source
P3K.2 occupancy
P3K.3 gameplay input binding
P3K.4 camera eligibility / explicit optional skip
P3K.5 admission and camera publication
P3K.6 evaluation
P3K.7A flow decision
```

Physical endpoint resolution remains explicit through
`IActivityPlayerGameplayStageEndpointSource`. The source supplies exact
selection/preparation operations, the registered stable Local Player Host, exact
Actor declaration, Gate adapter, camera authoring and output session. Default
Actor selection is permitted only through the already-authored Slot policy and
is recorded as stage-created work for exact rollback. No lookup by name, tag or
hierarchy path is introduced.

## Rollback

Only work created by the current stage is released.

```text
P3K.5 admission release
  -> camera request
  -> camera eligibility
  -> input binding
  -> occupancy
then
P3J preparation release
then
stage-created Actor selection release
then
staging scope release
```

When admission was not reached, partial P3K steps are released individually in
the same reverse order. Pre-existing preparation or admission is preserved.

Rollback stops at the first failed dependency for each Slot. Every successful
release clears its internal stage-created flag, so a later retry resumes from the
remaining dependency instead of resubmitting stale functional tokens.

Rollback failure retains an exact stage token and explicit progress evidence for
retry. No failure is converted to success.

## Scope runtime

`IActivityPlayerAdmissionStageScopeRuntime` is intentionally injected. The
canonical Activity lifecycle owns `RuntimeContentRuntime`; P3K.7B does not create
a parallel root registry. P3K.7C must provide the concrete ActivityFlow adapter.

## Scope

```text
stage token/state/result contracts
explicit scope lifecycle dependency
real P3J/P3K chain resolver
single active-stage authority
ReadyToCommit handoff
pre-completion rollback
post-completion Activity exit release
exact-token commit/rollback
reverse rollback and retry evidence
QA transaction regression
```

## Out of scope

```text
GameFlowRuntime mutation
ActivityFlowRuntime mutation
scene composition
previous Activity teardown
automatic polling/retry
new designer-facing authoring
FIRSTGAME integration
```

## P3K.7C ordering correction

The lifecycle integration audit found one earlier missing boundary: P3J and the
P3K gameplay authorities currently retain one current Actor/chain per Slot. A
normal Activity-to-Activity transition therefore cannot run the target P3K.7B
resolver while the previous Activity Player remains current.

P3K.7C adds concurrent inactive target Actor staging first. It does not weaken
P3K.7B; it supplies the physical coexistence required before the full resolver
can be promoted into lifecycle integration.

## Next

```text
P3K.7D — Player Gameplay Chain Promotion and Handoff
```

Promotion must define the reversible cutover from current Actor/chain to the
staged target candidate before P3K.7B becomes the canonical GameFlow/ActivityFlow
entry transaction.
