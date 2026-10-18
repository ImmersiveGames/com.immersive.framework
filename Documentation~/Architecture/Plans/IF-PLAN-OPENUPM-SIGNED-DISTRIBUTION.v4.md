# IF-PLAN — Signed OpenUPM Distribution

Status: **Approved — implementation task-gated; immutable**
Version: **v4**
Last updated: **2026-10-18**
Supersedes: **[IF-PLAN-OPENUPM-SIGNED-DISTRIBUTION.v3.md](IF-PLAN-OPENUPM-SIGNED-DISTRIBUTION.v3.md)**

## Purpose and authorization

Normalize the Immersive package repositories and the PEGA / planet-devourer
consumer manifests around signed GitHub Release archives indexed by OpenUPM.
The Project Owner explicitly selected this distribution route and clarified
that project manifests state the minimum SemVer, the generated lock records the
exact resolved graph, and Package Manager is used to update packages. This is
not an unpinned Git installation policy and does not authorize a batch release.

This version extends v3 to include the existing `com.immersivegames.face-system`
package and both consumer manifests. Existing user changes in those projects
and the Framework repository must be preserved.

## Package and consumer baseline

| Repository | Current default branch / source | Current distribution | Required action |
|---|---|---|---|
| `com.immersive.audio` | `main`, `481f5e01a26f158118ff475230910944aba676ef`, `0.2.4` | Signed GitHub Release and OpenUPM entry | Keep current release; no no-op version bump. |
| `com.immersive.foundation` | `main`, `86b5e17d69ffa72b302f323628ec494362702f42`, `0.2.3` | Signed GitHub Release and OpenUPM entry | Keep current release. |
| `com.immersive.logging` | `main`, `fe19eff32faea98a8f1005ba060ffd1babdbcadb`, `0.2.4` | Signed GitHub Release and OpenUPM entry | Keep current release. |
| `com.immersive.pooling` | `main`, `8befe28927408a2fb9883ef56dd7d0d294275305`, `0.2.3` | Signed GitHub Release and OpenUPM entry | Keep current release. |
| `com.immersive.composition` | `main`, `ce2ffe30c392f375a27e74c964cfe0e9b4b36c4a`, `0.1.0-preview.2` | Existing tag is unpublished; no signing workflow or OpenUPM entry | Prepare signed `0.1.0-preview.3`. |
| `com.immersive.framework` | `master`, `c6636561581cd4eb49e918d163f73712e64c1bcf`, `1.2.0-preview.3` | Signed release and OpenUPM entry; tag predates ActorProfile part assets | Prepare `1.2.0-preview.4` with Composition `0.1.0-preview.3`, Foundation `0.2.3`, and Logging `0.2.4`. Preserve the existing ActorProfile part-asset source changes. |
| `com.immersivegames.face-system` | `main`, `fcfeabdd52ce7300857b322fa248510e91610e6a`, `1.1.0` | No tag, signing workflow, or OpenUPM entry | Sign and publish the existing stable source as `v1.1.0`; do not change package code or version. |

The Framework checkout contains the previously authorized ActorProfile
part-asset implementation and documentation changes. PEGA and planet-devourer
both contain user-owned migration and package-file changes; none may be
discarded or overwritten.

## Accepted dependency and installation model

```text
Consumer manifest — minimum SemVer from OpenUPM
└── com.immersive.framework
    ├── com.immersive.composition
    ├── com.immersive.foundation
    └── com.immersive.logging

com.immersive.audio ──> com.immersive.pooling
```

Framework is the only direct declaration required for its dependency graph;
Composition must be installed transitively from Framework's package manifest.
The consumer may declare Audio and other packages it uses directly. PEGA also
declares Face System directly under the `com.immersivegames` registry scope.
Do not add Composition directly beside Framework or use local absolute paths or
Git tag URLs in the normalized consumer manifests.

Project manifests use the package's minimum compatible SemVer. Unity generates
`Packages/packages-lock.json` with the exact source and resolved version; do not
hand-edit that file. Package Manager is the update surface. The minimum version
allows later compatible package versions to be selected by a Package Manager
update while the lock preserves the current resolution until it is updated.

Expected direct Immersive declarations after all release gates:

| Consumer | Direct declarations | OpenUPM scopes |
|---|---|---|
| PEGA | Audio `0.2.4`, Framework `1.2.0-preview.4`, Foundation `0.2.3`, Logging `0.2.4`, Pooling `0.2.3`, Face System `1.1.0`; no direct Composition | `com.immersive`, `com.immersivegames` |
| planet-devourer | Audio `0.2.4`, Framework `1.2.0-preview.4`, Foundation `0.2.3`, Logging `0.2.4`, Pooling `0.2.3`; no direct Composition or Face System | `com.immersive` |

## Release sequence and gates

1. Preserve the existing default branches (`master` for Framework, `main` for
   all other package repositories); do not create worktrees, rename branches,
   move tags, or rewrite history.
2. Add package-scoped signed-release workflows for Composition and Face System,
   and prepare the two OpenUPM `githubRelease` package-index entries. Keep the
   existing signed workflow and index metadata for Audio, Foundation, Logging,
   Pooling, and Framework.
3. Confirm the GitHub Organization signing secrets are visible to the
   Composition and Face System repositories. Required names are
   `UPM_SERVICE_ACCOUNT_KEY_ID`, `UPM_SERVICE_ACCOUNT_KEY_SECRET`, and
   `UPM_ORG_ID`. Secret values must not be written to source, logs, or records.
4. Publish Composition `v0.1.0-preview.3` and Face System `v1.1.0` separately.
   Each release must use the matching `package.json` version and a source
   commit reachable from that repository's existing default branch. OpenUPM
   must show the exact signed release before a consumer is switched to it.
5. Prepare and validate Framework `1.2.0-preview.4` on `master`, including the
   current ActorProfile part-asset implementation. Bind owner-run Unity
   compile/import evidence to the exact source and resolved Composition
   dependency before tagging `v1.2.0-preview.4`.
6. After Framework `.4` is signed and available in OpenUPM, update only
   `Packages/manifest.json` in PEGA and planet-devourer to the table above.
   Remove the direct Composition entry. Add `com.immersivegames` to PEGA's
   OpenUPM scopes. Preserve other manifest entries and all profile migrations.
   Let Unity regenerate both package lock files in those existing projects.
7. In the existing consumer projects, reload Package Manager and confirm
   package signatures, resolved versions, Framework-to-Composition
   transitive resolution, and clean Unity import/compile. Record owner-run
   evidence in the mutable Framework Tracker. Do not create a clean project.

No release is created while signing-secret scope, OpenUPM package metadata,
reviewed source, or a required validation gate is missing. A failed or already
published version is never replaced; corrections use a new version.

## Release verification

For each new release, verify:

- tag version equals the root package manifest version;
- tag source commit is reachable from the package's default branch;
- the signed archive contains matching package identity/version and a non-empty
  `.attestation.p7m`;
- the GitHub Release has the expected archive and exact source SHA;
- OpenUPM reports the exact version as signed and installable;
- no signing secret appears in repository content or workflow output.

For consumers, verify the manifest uses registry SemVer minima (no local paths
or Git refs), the generated lock has the expected registry resolution, and
Composition is transitive through Framework. Preserve Stable, Experimental,
Internal, and serialized contracts according to their existing ADRs and
IF-GOV-001; package version does not promote API maturity.

## Non-goals and validation boundary

- No runtime/API, Unity asset, prefab, QAFramework, PEGA, or planet-devourer
  asset changes beyond the two explicitly authorized `Packages/manifest.json`
  edits after release gates succeed.
- No Unity execution by Codex, automated tests, Play Mode, smoke, package
  publication, or release tags before their gates.
- No new package, dependency redesign, Framework-to-Audio dependency, or
  no-op version bump for packages that are already signed and indexed.
- Existing consumer migration files and all unrelated working-tree changes are
  preserved.

## Change policy

This plan is immutable after approval. Record execution progress, exact source
revisions, OpenUPM/GitHub workflow evidence, consumer locks and residual gates
in [IF-TRACK-Framework.md](../Tracking/IF-TRACK-Framework.md). A material
change to the registry, signing authority, dependency graph, candidate versions
or release order requires a superseding version.
