# Game Flow: Player-independent navigation

Route navigation and Activity authority are independent from Player existence.

- A Route or Activity may become authoritative before any Player exists.
- `GameplayReady` is an Activity readiness requirement, not a navigation admission requirement.
- An Activity that cannot satisfy `GameplayReady` remains `NotReady`; Player-dependent gameplay gates stay closed.
- A Player handoff runs only when the previous Activity owns complete, compatible `GameplayReady` evidence. Otherwise the target runs its normal lifecycle and evaluates its own readiness.
- A contradictory or partially started handoff remains blocking before authority commit. Normal absence of Player evidence is not a handoff inconsistency.
- `CommittedNotReady` and `CommittedFinalizationFailed` keep destination authority. Only `FailedBeforeCommit` keeps the origin authoritative.
- `FrameworkValidationMode` changes diagnostic severity only; it does not select Route or Activity authority.

Diagnostics distinguish three cases:

1. Invalid authoring: required IDs, Route, Activity operation plan or other structural contracts are invalid.
2. Runtime readiness mismatch: the target has no matching Player evidence and completes `NotReady`.
3. Technical handoff inconsistency: a handoff was started but its exact token, ownership or evidence is contradictory; it blocks before commit.

When a request is rejected before Loading presentation, diagnostics report `NotExecutedRequestRejected` and `SkippedBeforePresentation`; unknown/default Loading results never mean success.
