# IF-ADR-047 — Initial Stable Baseline and Versioning Policy

Status: **Accepted**
Accepted: **2026-10-12**
Type: release governance / API compatibility
Related: [IF-GOV-001](../Governance/IF-GOV-001-API-MATURITY-AND-VALIDATION-GOVERNANCE.md), [Public-API](../../API/Public-API.md), [Framework tracker](../Tracking/IF-TRACK-Framework.md#110-first-stable-release-readiness)

## Context

The package has published versions labeled Stable and preview, including
`1.0.2` and `1.1.0-preview.*`. The product decision is to establish a new
official compatibility baseline without requiring the new baseline to preserve
or migrate every earlier experimental and prototyping surface. IF-GOV-001
defines compatibility per consumer contract and per API maturity, not by
feature or package version alone.

## Decision

1. **`1.1.0` is the first official Stable baseline of Immersive Framework.**
   Publication is a separate release operation; this decision does not claim
   that 1.1.0 is already packaged or published.
2. All versions before 1.1.0, including 1.0.2 and every `1.1.0-preview.*`, are
   retained as historical prototyping, experimentation and validation records.
   They impose no compatibility or generic migration obligation on the 1.1.0
   baseline. This does not delete tags/releases, deny their historical labels,
   or change what those artifacts contained.
3. From 1.1.0 forward, SemVer applies to the Stable consumer contracts:
   - **PATCH** preserves Stable API shape, serialized intent and documented
     behavior while correcting defects or making nonbreaking documentation or
     implementation changes.
   - **MINOR** may add Stable contracts or capabilities without breaking
     existing Stable guarantees.
   - **MAJOR** is required for a breaking change to an existing Stable
     guarantee. Such a change still requires an explicit architecture and
     migration decision under IF-GOV-001; SemVer alone does not approve it.
   - Prerelease identifiers such as `-preview.N` and `-rc.N` identify
     non-Stable candidates and do not establish a compatibility baseline.
4. The API classification remains per type/contract as declared by
   `FrameworkApiStatusAttribute`, Public-API and the owning ADR. A Stable package
   release does not promote all public types or features. Experimental and
   Internal boundaries retain their present meanings and classifications.

## Consequences and release gate

- Compatibility and support claims begin at the published 1.1.0 artifact and
  are evaluated against that exact artifact and its Stable contracts.
- No generic 1.0.2-to-1.1.0 migration is a release prerequisite. A specific
  Stable contract, serialized-data guarantee or dependency may still have its
  own explicit gate; it must be named and evidenced rather than inferred from
  historical version labels.
- 1.1.0 is **NO-GO** until the release candidate passes the objective Runtime,
  contract, QA and reproducible-distribution gates in the tracker. Any
  unresolved failure affecting a Stable guarantee, package import/compile,
  dependency resolution or artifact provenance is a blocker. A bounded gap in
  an Experimental contract is not a blocker unless it affects a Stable
  guarantee or an explicitly accepted release gate.
- The current package metadata and published registry state are not changed by
  this decision. The release operation must align tag, package metadata, source
  commit and distributed artifact before publication.

## Rejected

- Treating 1.0.2 as the compatibility starting point for the new Stable line.
- Requiring a generic migration of all earlier consumer data or Experimental
  APIs before the first Stable baseline.
- Promoting every public type or feature merely because package version 1.1.0
  is Stable.
- Changing Runtime, adding legacy compatibility, or creating replacement APIs
  as part of this release-governance decision.

## Pending

No architecture decision remains pending for the baseline. Release-candidate
execution and its GO/NO-GO evidence remain pending in the tracker.
