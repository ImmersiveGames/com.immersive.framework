# IF-ADR-009 — Activity Local Visibility Rules

Status: **Accepted / Implemented / Technical QA Certified**
Last updated: **2026-08-30**
Current certification: [IF-ADR-009 — Contribution / Visibility Technical Certification — 2026-08-30](../Reconciliation/IF-ADR-009-CONTRIBUTION-VISIBILITY-TECHNICAL-CERTIFICATION-2026-08-30.md)
Historical reconciliation: [ADR-002 / ADR-009 reconciliation — 2026-08-10](../Reconciliation/IMMERSIVE-FRAMEWORK-ADR-002-009-RECONCILIATION-2026-08-10.md)
Historical QA evidence: [ADR-009 QA certification — 2026-08-10](../Archive/IMMERSIVE-FRAMEWORK-ADR-009-QA-CERTIFICATION-2026-08-10.md)
Related decisions: IF-ADR-001, IF-ADR-002, IF-ADR-006, IF-ADR-007, IF-ADR-010, IF-ADR-014

## Context

The earlier combined local-visibility component mixed two independent concerns:
Activity-owned content and presentation visibility. The current framework separates
them so a GameObject may be shown or hidden for an Activity without becoming
Activity-owned content.

Visibility remains explicit, scoped and lifecycle-aware. It must never be inferred
from scene load, object names or hierarchy paths.

## Decision

```text
ActivityContentContribution
  = explicit Activity-owned content contract
  = Activity identity + Local Content Id + Required / Optional
  = content lifecycle participation
  = invalid Required contribution may block an Activity transition

ActivityVisibilityRule
  = presentation-only Activity-conditioned show / hide rule
  = no Activity ownership or Local Content Id
  = no Required / Optional semantics
  = invalid rule is diagnostic, ignored and non-mutating
  = never transition-blocking authority
```

`ActivityContentRuntime` discovers both component types only from framework-supplied
scope roots. It evaluates an `ActivityVisibilityRule` against the canonical active
Activity and applies only the rule GameObject's active state.

An `ActivityVisibilityRule` may live on Route-owned or otherwise externally owned
content. Its scene location does not transfer content ownership to an Activity.

## Accepted scope

- `ActivityContentContribution` owns explicit Activity content participation,
  requiredness and lifecycle callbacks.
- Invalid Required contributions are diagnostic and may reject the incoming Activity
  before commit; Optional invalid contributions remain non-mutating.
- `ActivityVisibilityRule` owns only presentation visibility through its explicit
  Activity list, match mode and no-active policy.
- An invalid visibility rule produces diagnostics and leaves its GameObject unchanged.
- Visibility consumes lifecycle facts; it does not own Activity identity, readiness,
  transition outcome, RuntimeContent lifetime or restoration policy.
- Stable authored identity remains definition identity, never runtime occurrence or
  ownership authority.

## Rejected scope

- Restoring `ActivityLocalVisibilityAdapter` or a combined content/visibility model.
- Adding Required / Optional or transition-blocking semantics to
  `ActivityVisibilityRule`.
- Treating visibility as Activity-owned content, readiness authority or lifecycle
  callback ownership.
- Global scene lookup, hierarchy/name-derived identity or silent fallback.
- A visibility-specific manager, Profile, Composer, Wizard or Apply/Rebuild layer
  without separately demonstrated product need.

## Consequences

Authoring and diagnostics must present the two components as separate contracts.
`ActivityContentContribution` validation explains ownership, local identity and
requiredness. `ActivityVisibilityRule` validation explains only presentation-rule
configuration and never changes transition authority.

IF-ADR-001 continues to own scoped runtime/lifecycle authority; IF-ADR-006 and
IF-ADR-007 own transition and readiness authority; IF-ADR-010 owns the Inspector
product-surface rules. This ADR does not create another owner.

## Current implementation coverage

Current implementation matches this split:

- `Runtime/ActivityFlow/ActivityContentContribution.cs` supplies the Activity-owned
  Required / Optional content contract.
- `Runtime/ActivityFlow/ActivityVisibilityRule.cs` supplies presentation-only
  visibility evaluation.
- `Runtime/ActivityFlow/ActivityContentRuntime.Transaction.cs` validates required
  contributions separately, while invalid visibility rules are diagnostic and skipped.
- The corresponding Editor validators and Inspectors are distinct:
  `ActivityContentContributionEditor` and `ActivityVisibilityRuleEditor`.

The current lifecycle/presentation order is:

```text
validate / collect
-> Exit previous Contribution
-> lifecycle participants
-> apply target Visibility Rules
-> Enter target Contribution
```

This ordering does not couple the authorities. A visibility change may occur with no
Contribution callback, and a Contribution callback may observe presentation state that
was already changed by the target Visibility Rule.

## Current technical certification

The post-split contract is technically certified by three focused Unity QA runners:

```text
Contribution Authority     3/3  PASS
Visibility Isolation       2/2  PASS
Lifecycle regression      16/16 PASS
------------------------------------
Current post-split evidence 21/21 PASS
```

The Contribution runner proves Required-invalid pre-commit rejection, Optional-invalid
non-blocking behavior and valid Activity-owned lifecycle independently of visibility.
The Visibility runner proves invalid-rule diagnostic/non-mutating/non-blocking behavior
and valid presentation-only behavior without Contribution ownership. The 16-case
lifecycle regression proves that Visibility membership does not broaden Contribution
membership and that presentation/lifecycle changes remain independent across positive,
negative, no-active, clear and idempotence cases.

See the dated certification record for the exact scope and evidence matrix.

## Historical evidence

The 2026-08-10 ADR-009 reconciliation and its `46` focused QA cases certify the
previous combined boundary. They remain historical evidence and do not certify the
later Contribution versus Visibility split.

They are not rewritten or reclassified as current post-split evidence.

## Certification state

The architecture and current technical QA boundary are closed for the implemented
Contribution / Visibility split.

Future work, if any, must be justified as a new product, runtime, Editor or consumer
requirement rather than as missing certification of this contract.
