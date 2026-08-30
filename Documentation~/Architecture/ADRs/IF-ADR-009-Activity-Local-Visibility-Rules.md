# IF-ADR-009 — Activity Local Visibility Rules

Status: **Accepted / Implemented — current contract certification pending**
Last updated: **2026-08-30**
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

## Historical evidence

The 2026-08-10 ADR-009 reconciliation and its `46` focused QA cases certify the
previous combined boundary. They remain historical evidence and do not certify this
later Contribution versus Visibility split.

No post-split focused QA or certification record was found in the package
documentation or package-local tests at this cut.

## Pending decisions

The architecture is decided. The remaining work is evidence only: add and record
focused validation for presentation-only invalid-rule behavior and for the separate
Required / Optional contribution path before claiming certification of this current
contract.
