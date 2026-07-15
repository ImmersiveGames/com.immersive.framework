# P3K.7A — Pre-Activation Activity Player Admission Boundary

Status: **implementation delta ready for Unity compile and QA**  
Type: **runtime contract + architecture gate**

## Objective

Convert the truthful P3K.6 evaluation into an operational decision without
prematurely wiring it into an Activity transition that cannot yet roll back all
side effects.

```text
P3K.6 evaluation
-> Proceed | AwaitResolution | RejectBlocked | RejectFailed
```

## Why direct GameFlow integration is not safe yet

Current Activity entry order is effectively:

```text
create Activity RuntimeContent scope
set current Activity active
compose Activity scenes
publish Activity content lifecycle
execute Activity content participants
release previous Activity scope/scenes
```

P3J.6 prepares Logical Player Actors inside Activity Content Execution.
Therefore:

```text
preflight LogicalActorsPrepared/GameplayReady
-> blocks the participant that creates preparation evidence
-> permanent PendingResolution cycle
```

Evaluating only after participant execution is also unsafe today:

```text
postflight rejection
-> next Activity scope/scenes/content already entered
-> previous Activity may already have exited
-> no complete Activity transaction rollback exists
```

The framework must not hide this ordering problem with a permissive fallback or
report success after partial entry.

## Runtime contract

`ActivityPlayerAdmissionFlowGate`:

```text
calls only ActivityPlayerAdmissionEvaluator
numbers explicit attempts
maps aggregate status to one flow disposition
retains exact immutable evaluation evidence
normalizes source/reason diagnostics
never mutates Player or Activity state
```

`ActivityPlayerAdmissionFlowDecision` exposes:

```text
attempt sequence
flow disposition
exact P3K.6 result
Activity/Session/requirement identity
Slot aggregate counts
CanProceed / RequiresResolution / IsRejected / CanRetry
source / reason / diagnostic message
```

Public decision contracts contain no Unity object references.

## Retry rule

`AwaitResolution` does not create background work or polling.

```text
caller resolves evidence explicitly
-> caller submits a new attempt
-> gate evaluates fresh snapshots
-> new attempt sequence and decision
```

Blocked or failed results are not marked retryable. Their policy, authoring or
runtime evidence must first be corrected.

## Scope

```text
flow disposition contract
exact P3K.6 mapping
attempt sequencing
explicit retry semantics
diagnostics
architecture ordering record
QA mapping regression
```

## Out of scope

```text
ActivityFlowRuntime mutation
Route/GameFlow request mutation
Activity scene/scope staging
automatic join or Actor selection
moving P3J.6 preparation lifecycle
P3K.2-P3K.5 host composition
camera publication
transition rollback
FIRSTGAME integration
```

## Next cut — P3K.7B

Introduce a real pre-activation Activity transaction phase:

```text
1. create a staged target Activity scope/context
2. run explicit admission resolvers against that staged context
3. re-evaluate P3K.6
4. commit Activity scene/content entry only when Proceed
5. release staged work on Await/Reject without touching current Activity
```

P3J.6 preparation must migrate into or be adapted to that phase before the gate
can become the canonical GameFlow continuation authority.

## Technical acceptance

```text
all P3K.6 states map deterministically
attempt sequence increments once per decision
pending is explicitly retryable
blocked/failed are explicit rejection
no Player/Activity mutation
no Runtime -> Editor dependency
no reflection in Runtime
no Unity references in public decision
```

## Suggested commits

```text
Framework:
P3K.7A — add pre-activation Player admission boundary

QAFramework:
P3K.7A — validate Player admission flow decisions
```


## P3K.7B implementation closure

P3K.7B now provides the staged transaction required by this boundary. It creates
a target scope through an explicit lifecycle adapter, coordinates the real
P3J/P3K chain, reuses this flow gate for the final decision, and either hands off
an exact commit handle or rolls back every stage-created dependency.

GameFlow integration remains deferred until P3K.7C so the current Activity is not
mutated before the staged transaction reaches `ReadyToCommit`.
