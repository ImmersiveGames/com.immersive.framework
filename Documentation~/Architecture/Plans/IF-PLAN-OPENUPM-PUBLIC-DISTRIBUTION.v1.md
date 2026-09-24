# IF-PLAN — OpenUPM Public Package Distribution

Status: **Accepted / Immutable**
Version: **v1**
Last updated: **2026-09-24**
Supersedes: **none**
Superseded by: **none**

## Purpose

Publish the Immersive package family through the public OpenUPM scoped registry
so a Unity project can declare only `com.immersive.framework` and let the Unity
Package Manager resolve its Immersive technical dependencies by semantic version.

The distribution change must preserve the existing package and assembly
boundaries. It must not copy technical package source into the Framework or add
runtime installation behavior.

## Accepted package graph

```text
com.immersive.framework 1.0.2
├── com.immersive.foundation 0.2.1
├── com.immersive.logging 0.2.2
├── com.unity.cinemachine 3.1.0
└── com.unity.inputsystem 1.19.0

com.immersive.audio 0.2.2
└── com.immersive.pooling 0.2.1
```

Audio remains optional. Framework installation must not implicitly install Audio
or Pooling while no active Framework dependency requires them.

## Package release cuts

Each package receives one metadata-only release cut:

| Package | Target version | Package dependencies |
|---|---:|---|
| `com.immersive.foundation` | `0.2.1` | none |
| `com.immersive.logging` | `0.2.2` | none |
| `com.immersive.pooling` | `0.2.1` | none |
| `com.immersive.audio` | `0.2.2` | `com.immersive.pooling@0.2.1` |
| `com.immersive.framework` | `1.0.2` | `com.immersive.foundation@0.2.1`, `com.immersive.logging@0.2.2`, Cinemachine `3.1.0`, Input System `1.19.0` |

Each repository must include:

- an MIT `LICENSE.md` with copyright owned by Immersive Games;
- Unity metadata for the license file when applicable;
- a `package.json` version matching the release tag exactly;
- `license`, `licensesUrl`, `documentationUrl` and `changelogUrl` package
  metadata that points to the official repository;
- a current `CHANGELOG.md` and package README installation section;
- no `private: true` publication opt-out.

Existing package namespaces, assemblies and public runtime contracts remain
unchanged.

## OpenUPM registration

The five repositories are registered as separate OpenUPM packages with:

- exact package name matching `package.json`;
- official public GitHub repository URL;
- `licenseSpdxId: MIT`;
- `trackingMode: git`;
- `minVersion` set to the new metadata-only release version;
- the repository's actual default branch as the README source;
- no historical tag rewrite.

Using the new version as `minVersion` prevents obsolete or inconsistent
historical tags from becoming the distribution baseline.

Registration and publication order is dependency-first:

1. Foundation;
2. Logging;
3. Pooling;
4. Audio;
5. Framework.

Audio is submitted only after Pooling is available from the registry. Framework
is submitted only after Foundation and Logging are available.

## Consumer configuration

A consuming Unity project configures OpenUPM once:

```json
{
  "scopedRegistries": [
    {
      "name": "OpenUPM",
      "url": "https://package.openupm.com",
      "scopes": [
        "com.immersive"
      ]
    }
  ],
  "dependencies": {
    "com.immersive.framework": "1.0.2"
  }
}
```

Unity must then resolve Foundation and Logging from OpenUPM and Cinemachine/Input
System from the Unity Registry without Git URLs for transitive dependencies.

## Planned order

1. Prepare and statically validate Foundation, Logging and Pooling.
2. Commit, tag, push and publish their GitHub Releases.
3. Register those three packages with OpenUPM and wait for registry availability.
4. Prepare Audio against the published Pooling version.
5. Prepare Framework against the published Foundation and Logging versions.
6. Commit, tag, push and publish Audio and Framework releases.
7. Register Audio and Framework with OpenUPM and wait for availability.
8. Validate registry metadata and the resolved dependency graph without invoking Unity.
9. Update the mutable Framework tracker with real publication state.
10. Request manual Unity import/compile validation from a clean consumer project.

## Gates

Every repository release must satisfy:

```text
package.json parses
package name is unchanged
package version equals the intended Git tag
MIT license exists and is referenced
README and changelog links resolve
Unity .meta GUIDs are unique
npm pack --dry-run contains the expected package files
working tree contains no unrelated changes
```

Every OpenUPM registration must satisfy:

```text
metadata pull request accepted
tag discovered
version published by https://package.openupm.com
registry manifest reports the expected dependencies
dependent package submitted only after prerequisites are available
```

An external moderation or registry delay is reported as pending. It is never
reported as successful publication.

## Failure handling

- A tag/version mismatch blocks tagging and publication.
- A failed `npm pack --dry-run` blocks the repository release.
- A rejected OpenUPM build is diagnosed from its build result before another
  release or metadata change is attempted.
- Published tags remain immutable.
- No dependent package is weakened by removing a required dependency to bypass
  registry ordering.
- No package falls back to bundled or copied source when registry resolution
  fails.

## Non-goals

- changing runtime behavior, assemblies, public APIs or serialized data;
- merging the five repositories or packages;
- making Audio a mandatory Framework dependency;
- creating a bootstrap installer that mutates consumer manifests;
- hosting a private registry or maintaining registry infrastructure;
- signing packages with Unity UPM CLI in this initial public-distribution cut;
- rewriting or deleting historical tags.

## Validation policy

Static packaging and registry validation are required in this execution. Unity
build, import, tests, Play Mode, smoke and batchmode are not run by Codex under
the workspace policy.

The final result remains `PENDING UNITY COMPILE/IMPORT VALIDATION` until a clean
Unity `6000.5.0f1` consumer project confirms:

1. OpenUPM scoped registry configuration;
2. installation of only `com.immersive.framework@1.0.2`;
3. automatic Foundation and Logging resolution;
4. package import and compilation;
5. optional Audio installation resolving Pooling independently.

## Change policy

This plan is immutable after acceptance. Execution progress belongs in
`../Tracking/IF-TRACK-Framework.md`.

A material change to registry provider, licensing, package graph, target
versions or publication authority requires a superseding plan version rather
than rewriting this file.
