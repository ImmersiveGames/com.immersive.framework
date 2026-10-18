# IF-PLAN — Signed OpenUPM Distribution

Status: **Accepted / Immutable**
Version: **v3**
Last updated: **2026-10-18**
Supersedes: **[IF-PLAN-OPENUPM-SIGNED-DISTRIBUTION.v2.md](IF-PLAN-OPENUPM-SIGNED-DISTRIBUTION.v2.md)**
Superseded by: **[IF-PLAN-OPENUPM-SIGNED-DISTRIBUTION.v4.md](IF-PLAN-OPENUPM-SIGNED-DISTRIBUTION.v4.md)**

## Purpose

Deliver Immersive packages as Unity-signed UPM archives through GitHub Releases
and OpenUPM. Framework dependencies remain semantic versions in
`package.json`, so installing Framework through OpenUPM installs Composition
transitively. This version extends the accepted v2 release graph and signing
scope to `com.immersive.composition`.

The Project Owner selected this route on 2026-10-18 to preserve transitive
auto-install. That selection does not authorize a batch release; each package
release follows the order and gates below.

## Version and branch authority

- A package release is identified by the matching root `package.json` version,
  immutable `vX.Y.Z` Git tag, signed GitHub Release archive and OpenUPM version.
- The tagged commit must be reachable from that repository's existing default
  branch. Framework remains on `master`; Composition and the other package
  repositories remain on their existing `main` branches. This plan does not
  rename branches or make unpinned branch URLs the installation mechanism.
- A new release is published only after its version and source commit are
  reviewed. Published tags and registry versions are never moved or replaced.

## Accepted dependency path

```text
QA / consumer project manifest
└── com.immersive.framework <version>
    ├── com.immersive.composition <semver from Framework package.json>
    ├── com.immersive.foundation <semver from Framework package.json>
    ├── com.immersive.logging <semver from Framework package.json>
    ├── com.unity.cinemachine <semver from Framework package.json>
    └── com.unity.inputsystem <semver from Framework package.json>
```

Audio remains optional. Framework declares Composition as a normal UPM
dependency. Consumers install Framework from the `com.immersive` OpenUPM
scope; they do not add a parallel Composition Git/local dependency for this
distribution path.

### Current and next compatible preview state

| Package | Current source state | Next release candidate |
|---|---|---|
| `com.immersive.composition` | `0.1.0-preview.2`, tag exists; no GitHub Release is present | `0.1.0-preview.3`, signed and available from OpenUPM before Framework depends on it |
| `com.immersive.framework` | `1.2.0-preview.3`, signed release exists; its dependency is Composition `0.1.0-preview.2`. The current local ActorProfile part-asset changes are not in that tag. | `1.2.0-preview.4`, containing the reviewed current Framework changes and depending on Composition `0.1.0-preview.3` |

These are the next candidate versions, not published artifacts. Do not alter
existing tags or releases. The currently released Framework preview remains
historical and is not repointed. Composition `.2` is an existing unpublished
tag; because v2 preserves unsigned versions, the first signed publication is
the new `.3` candidate rather than retrofitting `.2`.

## Signing authority and repository scope

One Unity Service Account owned by the Immersive Games Unity Organization signs
all packages in this graph. GitHub Organization secrets must be restricted to
exactly these six package repositories:

- `com.immersive.foundation`;
- `com.immersive.logging`;
- `com.immersive.pooling`;
- `com.immersive.audio`;
- `com.immersive.framework`;
- `com.immersive.composition`.

Required secret names:

```text
UPM_SERVICE_ACCOUNT_KEY_ID
UPM_SERVICE_ACCOUNT_KEY_SECRET
UPM_ORG_ID
```

Secret values must never be committed, logged, copied into documentation or
returned in release output. Composition must be added to the existing secret
visibility before its signed workflow is run.

## Release workflow and OpenUPM source

Each package owns a tag-triggered GitHub Actions workflow that checks out the
tag, requires tag/version/source SHA agreement, installs the Unity UPM CLI,
signs the package, verifies its identity and attestation, and publishes the
signed archive as the matching GitHub Release asset. Composition requires its
own workflow; the Framework workflow's package-specific archive file list
must not be copied unchanged.

Before a new Composition tag is pushed, its package entry in `openupm/openupm`
must be configured with:

```yaml
trackingMode: githubRelease
githubReleaseAssetName: 'com.immersive.composition-'
```

OpenUPM must republish the signed GitHub Release archive unchanged. The same
`githubRelease` source mode remains required for Framework and all other signed
packages. There is no fallback to unsigned Git-based packing.

## Ordered execution

1. Verify the current six package repositories, default branches, tags,
   package versions, manifests and working-tree ownership. Preserve unrelated
   work; do not rewrite history or move published tags.
2. Add `com.immersive.composition` to the GitHub Organization signing-secret
   repository allowlist. This is an external configuration gate.
3. Add the Composition signed-release workflow and submit/merge its OpenUPM
   `githubRelease` metadata before publishing any new Composition version.
4. Prepare Composition `0.1.0-preview.3` on its existing `main` branch. Verify
   metadata/tag equality and the signed archive content, then create tag
   `v0.1.0-preview.3` only after the signing and OpenUPM gates are ready.
5. Wait for OpenUPM to report Composition `0.1.0-preview.3` as signed and
   installable.
6. Prepare Framework `1.2.0-preview.4` on `master`, update its Composition
   dependency to `0.1.0-preview.3`, and include the current reviewed
   ActorProfile part-asset changes. Bind Unity/QA evidence to this exact source
   before creating its release tag.
7. Publish Framework only after step 5 succeeds. Verify OpenUPM reports the
   signed version and that a consumer manifest containing Framework alone
   resolves Composition transitively at the declared version.
8. Record exact source, tags, workflow runs, archive hashes, registry status,
   dependency lock results and owner-reported Unity import/compile evidence in
   the mutable Framework tracker.

Other packages with no source or dependency change remain on their current
versions and tags; this plan does not manufacture no-op releases for them.

## Release gates

For each package release, prove:

```text
tag version equals package.json version
tag resolves to the reviewed source commit reachable from the default branch
archive identity and version match package.json
archive contains package/.attestation.p7m
GitHub Release contains the signed archive for the same tag
no signing secret is present in logs or repository content
OpenUPM reports the exact version as signed and published
```

For the Framework integration, additionally prove that a project manifest
declaring only Framework resolves the matching Composition package through
UPM and that `packages-lock.json` records the intended versions and sources.
Owner-run Unity import/compile evidence must use that resolved package graph.

## Failure handling and non-goals

- Missing Unity permissions, repository-scoped secrets or OpenUPM metadata
  block release; no unsigned fallback is allowed.
- A failed unpublished candidate may be corrected on its unreleased source
  branch. Once a tag, GitHub Release or registry version is published, create a
  new version rather than replacing it.
- No branch rename, history rewrite, runtime redesign, dependency on Audio,
  Unity Editor run by Codex, or release of unrelated packages is authorized by
  this plan.
- This plan changes distribution scope only; package runtime/API contracts
  remain governed by their accepted ADRs and API maturity classifications.

## Change policy

This plan is immutable after acceptance. Execution progress and evidence
belong in [IF-TRACK-Framework.md](../Tracking/IF-TRACK-Framework.md). A material
change to signing authority, credential scope, registry, artifact source,
dependency graph or release order requires a superseding plan version.
