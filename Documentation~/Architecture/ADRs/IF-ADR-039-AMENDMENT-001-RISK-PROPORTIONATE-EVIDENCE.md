# IF-ADR-039 Amendment 001 — Risk-proportionate certification evidence

Status: **Accepted**
Date: **2026-10-12**
Amends: [IF-ADR-039 — Session Camera Assignment Command Boundary](IF-ADR-039-Session-Camera-Assignment-Command-Boundary.md), evidence note in API maturity disposition
Related governance: [IF-GOV-001 §8](../Governance/IF-GOV-001-API-MATURITY-AND-VALIDATION-GOVERNANCE.md)

## Context

The original disposition states that future certification records must include
the exact resolved Framework revision. IF-GOV-001 now defines evidence and
provenance requirements proportionate to the claim and risk. Requiring a
revision for every routine execution records metadata even when the decision
does not depend on candidate-specific identity.

## Amendment

1. For routine certification, record the scenario/property, result, known Unity
   and package environment, cleanup/restoration, and revision when available.
2. Exact execution revision, raw logs, detailed timestamps, artifact hashes,
   and replay are required only when an applicable gate says so or the decision
   needs candidate identity, release/regression investigation, or equivalent
   traceability. For a consumer candidate claim, record the effective package
   source/version and revision when available; a Git SHA alone does not prove
   which package bytes were loaded.
3. Unknown execution provenance remains unknown. Do not infer it from the
   current checkout. Preserve the original outcome and explain what the
   available evidence actually supports.

This amendment changes only the metadata expected in future certification
records. It does not alter IF-ADR-039's camera command contract, API maturity,
historical evidence or any runtime/consumer validation gate.
