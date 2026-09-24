# IF-PLAN — Signed OpenUPM Distribution

Status: **Accepted / Immutable**
Version: **v2**
Last updated: **2026-09-24**
Supersedes: **[IF-PLAN-OPENUPM-PUBLIC-DISTRIBUTION.v1.md](IF-PLAN-OPENUPM-PUBLIC-DISTRIBUTION.v1.md)**
Superseded by: **none**

## Purpose

Publish future Immersive package versions as Unity-signed UPM archives so
Unity 6.3 and newer can verify package provenance without changing runtime,
assembly or public API boundaries.

The signed artifact attached to each GitHub Release becomes the distribution
source consumed unchanged by OpenUPM.

## Accepted release graph

```text
com.immersive.framework 1.0.3
├── com.immersive.foundation 0.2.2
├── com.immersive.logging 0.2.3
├── com.unity.cinemachine 3.1.7
└── com.unity.inputsystem 1.19.0

com.immersive.audio 0.2.3
└── com.immersive.pooling 0.2.2
```

Audio remains optional. The Cinemachine update corrects Unity 6000.5
compatibility; no Framework runtime API change is authorized.

## Signing authority

One Unity Service Account owned by the Immersive Games Unity Organization is
the signing authority for all five packages.

GitHub Organization secrets are restricted to these repositories:

- `com.immersive.foundation`;
- `com.immersive.logging`;
- `com.immersive.pooling`;
- `com.immersive.audio`;
- `com.immersive.framework`.

Required secret names:

```text
UPM_SERVICE_ACCOUNT_KEY_ID
UPM_SERVICE_ACCOUNT_KEY_SECRET
UPM_ORG_ID
```

Secret values must never be committed, logged, copied into documentation or
returned in release output.

## Release workflow

Each repository owns a tag-triggered GitHub Actions workflow derived from the
official OpenUPM signed-package example.

For a `vX.Y.Z` tag, the workflow:

1. checks out the tagged commit;
2. reads the root `package.json`;
3. requires the tag version to equal `package.json.version`;
4. installs the Unity UPM CLI;
5. executes `upm pack . --organization-id "$UPM_ORG_ID"`;
6. requires exactly one `.tgz` or `.tar.gz` archive;
7. requires `package/package.json` and `package/.attestation.p7m` inside it;
8. creates the matching GitHub Release and uploads the signed archive;
9. triggers the OpenUPM scan only after the release asset exists.

The workflows receive only the three approved secrets and GitHub's short-lived
OIDC/repository tokens. They do not run Unity Editor, builds, tests, Play Mode
or batchmode.

## OpenUPM source mode

Before any signed version tag is pushed, each package metadata file must be
merged in `openupm/openupm` with:

```yaml
trackingMode: githubRelease
githubReleaseAssetName: '<package-name>-'
```

OpenUPM must download and republish the signed GitHub Release archive rather
than packing the Git checkout. Existing unsigned versions remain immutable.

## Planned order

1. Create the Unity Service Account and restrict its signing permission.
2. Add the three GitHub Organization secrets for only the five package repos.
3. Add and publish the signed-release workflow in every repository.
4. Merge five separate OpenUPM metadata PRs switching to `githubRelease`.
5. Publish signed Foundation `0.2.2`, Logging `0.2.3` and Pooling `0.2.2`.
6. Wait until those exact signed versions are available from OpenUPM.
7. Publish signed Audio `0.2.3` and Framework `1.0.3`.
8. Verify registry manifests, signed status and dependency graph.
9. Update the mutable Framework tracker with real release evidence.
10. Request Unity consumer import/compile confirmation.

## Gates

Every signing workflow must prove:

```text
tag version equals package.json version
archive contains package/package.json
archive contains package/.attestation.p7m
GitHub Release tag equals the package tag
signed archive is attached as a persistent release asset
no secret value appears in logs or repository content
```

Every registry publication must prove:

```text
OpenUPM metadata uses trackingMode: githubRelease
OpenUPM reports the intended version as published
OpenUPM reports the version as signed
published manifest contains the accepted dependency versions
dependent versions publish only after prerequisites are available
```

## Failure handling

- Missing Unity organization permission blocks credential creation.
- Missing or invalid signing credentials block tagging and release.
- A workflow without `.attestation.p7m` must fail before release creation.
- OpenUPM metadata must be merged before new tags to avoid unsigned Git-based
  ingestion.
- Published tags and registry versions remain immutable.
- A failed signed cut receives a new patch version; released tags are not moved.
- No signing failure may fall back silently to `trackingMode: git`.

## Non-goals

- changing runtime behavior, assemblies, serialized fields or public APIs;
- signing or replacing historical package versions;
- committing Unity or GitHub credentials;
- storing signing keys in package files;
- making Audio or Pooling mandatory Framework dependencies;
- running Unity Editor validation in CI;
- introducing a private registry.

## Validation policy

Repository-side validation is limited to release metadata, tag/version
equality and signed archive inspection. OpenUPM publication and signature state
must be verified from the registry.

Unity build, import, tests, Play Mode, smoke and batchmode are not run by Codex.
The final status remains `PENDING UNITY COMPILE/IMPORT VALIDATION` until a clean
Unity `6000.5.0f1` project installs Framework `1.0.3` and confirms that package
signature warnings are absent.

## Change policy

This plan is immutable after acceptance. Execution progress belongs in
`../Tracking/IF-TRACK-Framework.md`.

A material change to signing authority, credential storage, registry provider,
release graph or artifact source requires a superseding plan version.
