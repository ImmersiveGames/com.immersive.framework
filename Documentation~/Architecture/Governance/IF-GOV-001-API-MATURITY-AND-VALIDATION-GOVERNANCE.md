# IF-GOV-001 — API Maturity and Validation Governance

Status: **Active**  
Date: 2026-08-11  
Last updated: 2026-10-12
Classification: **Cross-cutting architecture governance**  
Feature authority created: **None**  
Primary related decision: **IF-ADR-010 — Editor and Inspector Product Surface Authority**

## 1. Purpose

This record makes existing framework-wide governance policies explicit:

1. API maturity metadata expressed by `FrameworkApiStatus` and
   `FrameworkApiStatusAttribute`.
2. Product authoring diagnostic strictness expressed by
   `FrameworkValidationMode` and `FrameworkValidationModePolicy`.
3. Risk-proportionate evidence and certification decisions used to assess
   implementation increments and API maturity.

These concerns are governance primitives. They do not justify a new feature ADR,
runtime service, manager, singleton, Composer or authoring authority. For API
maturity and compatibility, the consumer contract is the primary normative
unit; a feature is the functional grouping that may contain multiple contracts.

## 2. API maturity metadata

`FrameworkApiStatusAttribute` is source-level documentation/validation metadata.
It does not alter runtime behavior.

### Contract and feature units

The **consumer contract** is the primary unit for maturity and compatibility. A
contract consists of the consumer-facing API surface and its behavioral,
configuration, serialization, extension, or lifecycle guarantees. One contract
may comprise one or more related C# types, and one C# type may participate in
more than one contract.

A **feature** is a functional grouping and may contain contracts with different
maturities. A contract's maturity does not determine the maturity of other
contracts in the feature; a Stable contract does not promote the feature or its
other contracts, and an Experimental contract does not downgrade Stable
contracts in the same feature.

`FrameworkApiStatusAttribute` remains type-level metadata and is the reference
maturity declaration for each attributed C# type. It does not, by itself,
define a feature or establish the complete scope of every contract in which the
type participates.

Where distinct guarantees exist within one type, the contract record or
relevant architecture decision must identify the included members, operations,
and behaviors, along with compatibility and serialization boundaries. It must
preserve existing Stable guarantees, identify relevant Experimental
dependencies, and require explicit ADR and migration handling for changes that
break Stable commitments. A contract does not become Stable merely because it
uses a Stable type. Conversely, documenting a narrower boundary must not
retrospectively reduce any existing Stable guarantee.

For this decision, the Stable authoring contracts of `ActivityAsset` and
`GameApplicationAsset` cover preservation and interpretation of their existing
serialized consumer data, including fields whose configured behavior belongs to
an Experimental contract. This does not promote those field behaviors. A
change that makes existing authored data unreadable or changes its stored
intent is a breaking change to the affected Stable authoring contract and
requires an explicit ADR and migration. This guarantee concerns the package's
serialized data schema, not byte-for-byte Unity YAML formatting. Field-specific
behavior and its maturity remain governed by the relevant feature ADR.

An inventory of contracts is an index for traceability, not a second authority
for maturity. It should reference the applicable type attributes and
architecture decisions. A divergence among attribute, documentation, and
behavior must be recorded for formal reconciliation; it must not be resolved
silently.

The canonical categories are:

| Status | Contract |
|---|---|
| `Stable` | May be consumed by games and external modules. Breaking changes require an ADR/migration decision. |
| `Experimental` | Available for controlled development use. Shape may change without compatibility guarantees. |
| `Internal` | Framework implementation detail. Game code must not depend on it. |
| `Deferred` | Retained for planning/frozen source; not part of the active baseline. |
| `DevelopmentTooling` | QA, Editor or development tooling; not product API. |
| `Removed` | Removed or scheduled for removal. It is not an active consumer contract. |

These six enum categories and their meanings remain canonical. `Legacy` and
`Deprecated` are lifecycle descriptions, not additional `FrameworkApiStatus`
categories:

- `Legacy` describes historical or compatibility context and does not determine
  maturity.
- `Deprecated` records a formal intent to replace or discontinue a surface and
  requires a traceable decision. A Deprecated surface may retain its current
  maturity throughout a migration period.
- Assigning `Removed` requires an explicit decision, including when removal is
  scheduled. Its existing meaning—removed or scheduled for removal—does not
  change.
- `[Obsolete]` is an implementation choice for communicating deprecation; it is
  not required for every Deprecated surface and must follow the relevant
  implementation decision.
- Deprecation does not by itself remove or weaken any Stable compatibility
  guarantee.

### Governance rule

API status describes **maturity and compatibility expectations**. It does not
create runtime authority, instantiate services, select implementations or change
execution semantics by itself.

A surface marked `Stable` must not be broken merely by changing its attribute.
The architectural/API change and its migration consequence must be handled
explicitly.

## 3. Validation mode ownership

`FrameworkValidationMode` is a Stable product-authoring policy used by the
application/route/activity validation path. It belongs to ADR-010's product
surface and diagnostics boundary.

It is not a separate validation architecture. The existing authoring validation
infrastructure remains canonical.

The exact policy is:

| Mode | Required configuration | Warnings | Info diagnostics |
|---|---|---|---|
| `Strict` | Fail | Promote to errors | Include |
| `Standard` | Fail | Keep as warnings | Include |
| `Release` | Fail | Keep as warnings | Suppress |

`Standard` remains the authored default where the product surface does not
explicitly select another valid mode.

## 4. Unknown validation values

An unknown serialized enum value is invalid configuration. It must not silently
weaken diagnostics.

Until the authored asset is corrected, an unknown value uses conservative
`Strict` diagnostic semantics:

```text
required configuration  -> fail
warnings                -> errors
info diagnostics         -> included
known mode               -> false
```

This is not a fallback that repairs or rewrites user intent. The invalid authored
value remains invalid and diagnosable; the policy only prevents a corrupted or
obsolete enum value from reducing validation severity.

## 5. Runtime and product boundaries

This governance record creates no runtime behavior beyond the existing
validation policy.

It explicitly does **not** introduce:

```text
global validation manager
service locator
runtime validation session
automatic asset mutation
silent enum normalization
new authoring authority
new Composer/Profile/Recipe
```

## 6. QA expectations

Technical QA is appropriate for the deterministic policy matrix because a change
can alter validation severity without a visible Inspector-layout change.

The focused regression must prove:

```text
Strict    known, required fails, warnings -> errors, info included
Standard  known, required fails, warnings stay warnings, info included
Release   known, required fails, warnings stay warnings, info suppressed
Unknown   not known, required fails, warnings -> errors, info included
```

No synthetic Inspector-understandability test is required.

## 7. Change discipline

- Changes to `FrameworkApiStatus` meanings are architecture-governance changes.
- The consumer contract is the scope for evaluating Stable compatibility;
  breaking changes to Stable guarantees require explicit ADR/migration handling.
- Do not promote or downgrade other contracts based only on their membership in
  the same feature or their use of a type with another maturity.
- Reconcile any divergence among type attributes, contract documentation, and
  observed behavior through an explicit decision; do not change classifications
  silently or retrospectively.
- `Legacy` and `Deprecated` do not add enum categories or alter a surface's
  maturity. Deprecation and assignment of `Removed` require the explicit
  lifecycle decisions described in §2.
- Changes to validation-mode semantics affect ADR-010 product diagnostics and require focused technical QA.
- Adding a new validation mode requires defining its severity/noise semantics explicitly before adoption.
- Unknown values must continue to fail conservatively; no silent downgrade is allowed.

## 8. Evidence proportionality and certification

Evidence demonstrates a defined property at a defined scope. Evidence format,
provenance detail, and repeat execution are selected for the risk and purpose of
the decision; none is a universal requirement or universally sufficient.

Keep these concepts distinct:

| Concept | Meaning |
|---|---|
| Functional evidence | Test result or observation that demonstrates a behavior within its actual scope. |
| Provenance | Available information about source/package, version, environment, and execution time. It describes evidence; it is not itself functional proof. |
| Composite evidence | Verifiable combination of independent or complementary tests, runtime observations, and static inspection used to assess related properties. |
| Residual risk | A known property or limitation not fully demonstrated, explicitly accepted by the authority responsible for the decision. |
| Functional gap | A required behavior not demonstrated by the available evidence, or a valid execution that showed a real failure. |

### 8.1 Reconcile before requesting more execution

Before requesting a new test or fixture, the reviewer or certifier must:

1. State the exact property and decision scope.
2. Locate accessible existing tests, QA results, consumer observations,
   certification records, and relevant static reviews.
3. Record what each source actually demonstrates, including its scope and
   limitations; do not infer coverage from its title or verdict alone.
4. Consider whether complementary evidence covers the claim when combined.
5. Identify uncovered properties and residual risks; obtain explicit acceptance
   from the responsible authority when a bounded decision depends on that risk.
6. Propose a new test only for a material technical gap that cannot be accepted
   under the applicable governance and gate.

Do not create a Play Mode fixture solely because no single fixture covers the
entire property. The proposed test must target a named uncovered risk, and its
owner must be the package, QAFramework, or consumer that owns that behavior.

### 8.2 Accepted evidence forms and limits

Depending on the property, evidence may include NUnit/Test Runner output,
execution screenshots, structured QA terminal lines, raw logs, a manual run
record with verifiable criteria, static inspection, a composition of these
sources, or a historical certification whose applicability to the current
contract is justified. No form is automatically sufficient. Preserve direct
results as reported: static inspection is not an executed test, and a composite
acceptance is a gate decision rather than a new `PASS`.

Missing provenance is not a functional failure. Never attach a historical
result to a SHA, package revision, or exact execution time that was not captured.
A Git SHA identifies repository content; it does not alone prove which package
bytes the consumer resolved, imported, compiled, or exercised. Record the
effective package source/version or consumer resolution when the claim depends
on it.

### 8.3 Risk-based traceability

Record the provenance available for each execution and state unknown fields
plainly. For routine manual certification, scenario, result, known environment,
and available revision may be enough. For a critical release, security/legal
claim, regression investigation, or explicitly governed gate, exact candidate
identity, raw output, timestamps, artifact hashes, and replay may be warranted.
Require each additional field only when its technical, legal, security, or
governance purpose is stated.

An explicit contract-specific gate remains binding. A general policy does not
waive its required proof, candidate identity, compatibility check, or approval.
If a gate demands traceability disproportionate to its risk, document the
conflict and use that decision's amendment mechanism; do not silently ignore the
gate or repeat tests only to reconstruct historical metadata. Historical
certifications remain usable when their scope and applicability to the current
contract are justified; retain their original result and provenance limits.

### 8.4 Cross-agent evidence access

An agent may assess only evidence it can read. If evidence is in another
conversation, tool session, repository, or host, identify the files or results
needed. When evidence is in a ChatGPT conversation, prepare a compact evidence
package when appropriate and tell the user which package to attach to the Codex
task. Confirm the transferred files are readable and distinguish original
artifacts from transcripts or summaries. If access is unavailable, report that
limitation; do not claim the evidence does not exist or was never executed. Do
not presume automatic transfer between conversation histories, tools,
repositories, or agent sessions.

The implementation agent owns required contract and package tests and must not
expand QA automatically. The reviewer assesses properties, existing coverage,
and regressions. The certifier reconciles evidence, identifies real gaps, and
states a scoped verdict. The governance authority decides explicit residual
risk and normative exceptions. Existing agents may hold more than one of these
responsibilities; do not create duplicate agents solely to mirror these roles.

### 8.5 Application example: IF-ADR-046

IF-ADR-046 keeps the individual NUnit, QA, consumer, and static-review claims
separate, then uses Amendments 002 and 003 to record bounded composite evidence
and explicit acceptance of partial provenance for the identified Stable
decision. The accepted physical-compensation risk is not reported as an
integrated Play Mode `PASS`; missing execution SHAs are not filled from current
checkout state. This is an example of a scoped decision, not a new universal
manifest or waiver for other gates. For a future candidate-specific Stable
claim, verify the package source/version actually resolved by the consumer as
well as the Framework revision; a Git SHA alone does not establish loaded or
executed package bytes.

## 9. Disposition

Sections 2–7 govern the existing API maturity and authoring validation
primitives. Contract maturity does not imply functional certification; Section
8 establishes risk-proportionate evidence policy for the specific contract
properties and certification decisions under review. These policies create no
runtime behavior, new feature authority, or product subsystem.
