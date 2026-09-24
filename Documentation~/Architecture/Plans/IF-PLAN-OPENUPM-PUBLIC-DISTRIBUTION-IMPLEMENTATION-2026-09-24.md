# OpenUPM Public Distribution Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Publish the five `com.immersive.*` Unity packages through OpenUPM so a consumer can install `com.immersive.framework@1.0.2` and have Foundation and Logging resolved automatically.

**Architecture:** Keep every repository as an independent UPM distribution unit. Add MIT licensing and registry-ready metadata, release dependency-free packages first, wait for their OpenUPM availability, then release and register Audio and Framework against exact published versions.

**Tech Stack:** Unity Package Manager manifests, Git/GitHub tags and Releases, npm-compatible package packing, OpenUPM public scoped registry, GitHub pull requests.

**Spec:** `Documentation~/Architecture/Plans/IF-PLAN-OPENUPM-PUBLIC-DISTRIBUTION.v1.md`

## Global Constraints

- Unity floor remains exactly `6000.5.0f1`.
- Package names and assembly boundaries must not change.
- No runtime, Editor, test, asmdef, scene, prefab or serialized API changes are authorized.
- License is MIT with `Copyright (c) 2026 Immersive Games`.
- Published Git tags are immutable and must match `package.json.version` exactly.
- OpenUPM registry URL is `https://package.openupm.com` and consumer scope is `com.immersive`.
- Audio remains optional and must not become a Framework dependency.
- Historical tags are not rewritten or deleted.
- Codex must not run Unity build, import, tests, Play Mode, smoke or batchmode.
- Every repository must be clean before its release commit; unrelated user changes block only that repository's cut.

Use this exact MIT text in every `LICENSE.md`:

```text
MIT License

Copyright (c) 2026 Immersive Games

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```

## Review Focus

- Historical tags whose names do not match their embedded manifest version must be excluded with each OpenUPM `minVersion`, never rewritten.
- A repository with unrelated local changes must not receive a release commit until those changes are isolated or the user resolves them.
- `main` is the documentation branch for Foundation, Logging, Pooling and Audio; `master` is the Framework documentation branch.
- Audio registration must remain blocked until Pooling is queryable from OpenUPM; Framework registration must remain blocked until Foundation and Logging are queryable.
- Registry verification must prove exact dependency versions and must not mistake GitHub tag availability for OpenUPM package availability.

---

### Task 1: Prepare Foundation 0.2.1

**Files:**
- Create: `C:/Projetos/ImmersivePackages/com.immersive.foundation/LICENSE.md`
- Create: `C:/Projetos/ImmersivePackages/com.immersive.foundation/LICENSE.md.meta`
- Create: `C:/Projetos/ImmersivePackages/com.immersive.foundation/CHANGELOG.md`
- Create: `C:/Projetos/ImmersivePackages/com.immersive.foundation/CHANGELOG.md.meta`
- Modify: `C:/Projetos/ImmersivePackages/com.immersive.foundation/package.json`
- Modify: `C:/Projetos/ImmersivePackages/com.immersive.foundation/README.md`

**Interfaces:**
- Consumes: no Immersive package dependency.
- Produces: `com.immersive.foundation@0.2.1`, required by Framework 1.0.2.

- [ ] **Step 1: Verify the repository baseline**

Run:

```powershell
$repo='C:/Projetos/ImmersivePackages/com.immersive.foundation'
git -c "safe.directory=$repo" -C $repo status --short --branch
git -c "safe.directory=$repo" -C $repo tag --list v0.2.1
```

Expected: clean `main`, tracking `origin/main`, and no `v0.2.1` tag.

- [ ] **Step 2: Add the license and release documentation**

Create `LICENSE.md` with the exact MIT text from Global Constraints. Create
Unity `TextScriptImporter` `.meta` files for `LICENSE.md` and `CHANGELOG.md` with
fresh 32-character lowercase GUIDs. Create a `0.2.1` changelog entry dated
`2026-09-24` stating that the cut adds public MIT licensing and OpenUPM
distribution metadata without runtime/API changes.

- [ ] **Step 3: Update the package manifest**

Set these exact fields while preserving the package name and Unity floor:

```json
{
  "name": "com.immersive.foundation",
  "displayName": "Immersive Foundation",
  "version": "0.2.1",
  "description": "Reusable validation, event and finite-state-machine primitives for Immersive Unity packages.",
  "unity": "6000.5",
  "unityRelease": "0f1",
  "license": "MIT",
  "licensesUrl": "https://github.com/ImmersiveGames/com.immersive.foundation/blob/main/LICENSE.md",
  "documentationUrl": "https://github.com/ImmersiveGames/com.immersive.foundation#readme",
  "changelogUrl": "https://github.com/ImmersiveGames/com.immersive.foundation/blob/main/CHANGELOG.md",
  "author": {
    "name": "Immersive Games",
    "url": "http://www.immersivegame.com.br",
    "email": "contato@immersivegames.com.br"
  }
}
```

- [ ] **Step 4: Update README installation guidance**

Add an MIT license section and two installation modes: OpenUPM
`com.immersive.foundation@0.2.1` as canonical, and the tagged Git URL
`https://github.com/ImmersiveGames/com.immersive.foundation.git#v0.2.1` as the
fallback.

- [ ] **Step 5: Run static packaging validation**

Run:

```powershell
$repo='C:/Projetos/ImmersivePackages/com.immersive.foundation'
$manifest=Get-Content -Raw "$repo/package.json" | ConvertFrom-Json
if($manifest.name -ne 'com.immersive.foundation' -or $manifest.version -ne '0.2.1' -or $manifest.license -ne 'MIT'){throw 'Foundation manifest mismatch'}
git -c "safe.directory=$repo" -C $repo diff --check
npm pack $repo --dry-run --json
```

Expected: parsed manifest, clean diff check, and pack inventory containing
`package/package.json`, `package/LICENSE.md`, `package/CHANGELOG.md`, Runtime and
Editor content.

- [ ] **Step 6: Commit without tagging**

```powershell
git -c "safe.directory=$repo" -C $repo add -- LICENSE.md LICENSE.md.meta CHANGELOG.md CHANGELOG.md.meta package.json README.md
git -c "safe.directory=$repo" -C $repo diff --cached --check
git -c "safe.directory=$repo" -C $repo commit -m "chore: prepare OpenUPM release 0.2.1"
```

Expected: one metadata/documentation-only commit; tag creation waits for Task 4.

### Task 2: Prepare Logging 0.2.2

**Files:**
- Create: `C:/Projetos/ImmersivePackages/com.immersive.logging/LICENSE.md`
- Create: `C:/Projetos/ImmersivePackages/com.immersive.logging/LICENSE.md.meta`
- Create: `C:/Projetos/ImmersivePackages/com.immersive.logging/CHANGELOG.md`
- Create: `C:/Projetos/ImmersivePackages/com.immersive.logging/CHANGELOG.md.meta`
- Modify: `C:/Projetos/ImmersivePackages/com.immersive.logging/package.json`
- Modify: `C:/Projetos/ImmersivePackages/com.immersive.logging/README.md`

**Interfaces:**
- Consumes: no Immersive package dependency.
- Produces: `com.immersive.logging@0.2.2`, required by Framework 1.0.2.

- [ ] **Step 1: Verify the repository baseline**

Run:

```powershell
$repo='C:/Projetos/ImmersivePackages/com.immersive.logging'
git -c "safe.directory=$repo" -C $repo status --short --branch
git -c "safe.directory=$repo" -C $repo tag --list v0.2.2
```

Expected: clean `main`, tracking `origin/main`, and no `v0.2.2` tag.

- [ ] **Step 2: Add the license and release documentation**

Create `LICENSE.md` with the exact MIT text from Global Constraints, two unique
Unity `.meta` files, and a `0.2.2` changelog entry dated `2026-09-24`. State that
runtime logging contracts remain unchanged.

- [ ] **Step 3: Update the package manifest**

Preserve existing identity and description; set `version` to `0.2.2`, add
`license: MIT`, and use these URLs:

```text
licensesUrl      https://github.com/ImmersiveGames/com.immersive.logging/blob/main/LICENSE.md
documentationUrl https://github.com/ImmersiveGames/com.immersive.logging#readme
changelogUrl     https://github.com/ImmersiveGames/com.immersive.logging/blob/main/CHANGELOG.md
author.url       http://www.immersivegame.com.br
author.email     contato@immersivegames.com.br
```

- [ ] **Step 4: Update README installation guidance**

Document OpenUPM `com.immersive.logging@0.2.2`, tagged Git fallback
`https://github.com/ImmersiveGames/com.immersive.logging.git#v0.2.2`, and MIT.

- [ ] **Step 5: Validate and commit**

Run:

```powershell
$repo='C:/Projetos/ImmersivePackages/com.immersive.logging'
$manifest=Get-Content -Raw "$repo/package.json" | ConvertFrom-Json
if($manifest.name -ne 'com.immersive.logging' -or $manifest.version -ne '0.2.2' -or $manifest.license -ne 'MIT'){throw 'Logging manifest mismatch'}
git -c "safe.directory=$repo" -C $repo diff --check
npm pack $repo --dry-run --json
git -c "safe.directory=$repo" -C $repo add -- LICENSE.md LICENSE.md.meta CHANGELOG.md CHANGELOG.md.meta package.json README.md
git -c "safe.directory=$repo" -C $repo diff --cached --check
git -c "safe.directory=$repo" -C $repo commit -m "chore: prepare OpenUPM release 0.2.2"
```

Verify the pack inventory includes license, changelog, runtime and editor entries.
The commit message is:

```text
chore: prepare OpenUPM release 0.2.2
```

Expected: no runtime or asmdef changes and no `v0.2.2` tag yet.

### Task 3: Prepare Pooling 0.2.1

**Files:**
- Create: `C:/Projetos/ImmersivePackages/com.immersive.pooling/LICENSE.md`
- Create: `C:/Projetos/ImmersivePackages/com.immersive.pooling/LICENSE.md.meta`
- Create: `C:/Projetos/ImmersivePackages/com.immersive.pooling/CHANGELOG.md`
- Create: `C:/Projetos/ImmersivePackages/com.immersive.pooling/CHANGELOG.md.meta`
- Modify: `C:/Projetos/ImmersivePackages/com.immersive.pooling/package.json`
- Modify: `C:/Projetos/ImmersivePackages/com.immersive.pooling/README.md`

**Interfaces:**
- Consumes: no Immersive package dependency.
- Produces: `com.immersive.pooling@0.2.1`, required by Audio 0.2.2.

- [ ] **Step 1: Verify the repository baseline**

Run:

```powershell
$repo='C:/Projetos/ImmersivePackages/com.immersive.pooling'
git -c "safe.directory=$repo" -C $repo status --short --branch
git -c "safe.directory=$repo" -C $repo tag --list v0.2.1
```

Expected: clean `main`, tracking `origin/main`, with no `v0.2.1` tag.

- [ ] **Step 2: Add MIT and release files**

Create the exact MIT license, unique `.meta` files and a `0.2.1` changelog entry
dated `2026-09-24`, explicitly preserving the frozen pooling runtime baseline.

- [ ] **Step 3: Update package metadata and README**

Set version `0.2.1`, license `MIT`, official author URL/email and repository URLs
under `https://github.com/ImmersiveGames/com.immersive.pooling`. Document
OpenUPM `com.immersive.pooling@0.2.1`, Git fallback ending `#v0.2.1`, and MIT.

- [ ] **Step 4: Validate and commit**

Run:

```powershell
$repo='C:/Projetos/ImmersivePackages/com.immersive.pooling'
$manifest=Get-Content -Raw "$repo/package.json" | ConvertFrom-Json
if($manifest.name -ne 'com.immersive.pooling' -or $manifest.version -ne '0.2.1' -or $manifest.license -ne 'MIT'){throw 'Pooling manifest mismatch'}
git -c "safe.directory=$repo" -C $repo diff --check
npm pack $repo --dry-run --json
git -c "safe.directory=$repo" -C $repo add -- LICENSE.md LICENSE.md.meta CHANGELOG.md CHANGELOG.md.meta package.json README.md
git -c "safe.directory=$repo" -C $repo diff --cached --check
git -c "safe.directory=$repo" -C $repo commit -m "chore: prepare OpenUPM release 0.2.1"
```

Verify the package inventory, stage only the six release files, and use commit:

```text
chore: prepare OpenUPM release 0.2.1
```

Expected: no runtime or asmdef changes and no `v0.2.1` tag yet.

### Task 4: Publish dependency-free GitHub releases

**Files:**
- No new source files.
- Read: each package's `package.json` and `CHANGELOG.md`.

**Interfaces:**
- Consumes: verified commits from Tasks 1–3.
- Produces: immutable tags and public GitHub Releases used by OpenUPM discovery.

- [ ] **Step 1: Reverify all three committed trees**

For each repository, require a clean tree, ensure `HEAD` is exactly one intended
release commit ahead of its upstream, parse the committed manifest from `HEAD`,
and compare it to the intended tag.

- [ ] **Step 2: Create annotated tags**

```text
Foundation v0.2.1 -> Immersive Foundation 0.2.1
Logging    v0.2.2 -> Immersive Logging 0.2.2
Pooling    v0.2.1 -> Immersive Pooling 0.2.1
```

- [ ] **Step 3: Push each branch and tag atomically**

Use `git push --atomic origin main <tag>` per repository. If a push fails, do
not publish its release or any dependent package.

- [ ] **Step 4: Create GitHub Releases**

Create non-draft, non-prerelease releases whose names match the package display
name and version. Release bodies must state MIT/OpenUPM readiness and explicitly
state that runtime/API behavior is unchanged.

- [ ] **Step 5: Verify remote state**

Use `git ls-remote` to prove branch/tag commit identity and the GitHub Releases
API to prove `draft=false`, `prerelease=false` and correct tag names.

### Task 5: Register Foundation, Logging and Pooling with OpenUPM

**Files:**
- External create: `openupm/openupm:data/packages/com.immersive.foundation.yml`
- External create: `openupm/openupm:data/packages/com.immersive.logging.yml`
- External create: `openupm/openupm:data/packages/com.immersive.pooling.yml`

**Interfaces:**
- Consumes: public tags/releases from Task 4 and GitHub hunter `rinnocenti`.
- Produces: registry-resolvable dependency-free Immersive packages.

- [ ] **Step 1: Create exact OpenUPM metadata**

Use one separate PR per package. Each YAML uses `parentRepoUrl: null`,
`licenseSpdxId: MIT`, `licenseName: MIT License`, `gitTagPrefix: ''`,
`trackingMode: git`, and `hunter: rinnocenti`.

Foundation:

```yaml
name: com.immersive.foundation
displayName: Immersive Foundation
description: Reusable validation, event and finite-state-machine primitives for Immersive Unity packages.
repoUrl: 'https://github.com/ImmersiveGames/com.immersive.foundation'
parentRepoUrl: null
licenseSpdxId: MIT
licenseName: MIT License
topics:
  - utilities
gitTagPrefix: ''
gitTagIgnore: ''
minVersion: '0.2.1'
trackingMode: git
image: ''
readme: 'main:README.md'
hunter: rinnocenti
```

Logging uses name/display/description/repository for Logging, topic
`debugging-and-logging`, `minVersion: '0.2.2'`, and `readme: 'main:README.md'`.

Pooling uses name/display/description/repository for Pooling, topic `utilities`,
`minVersion: '0.2.1'`, and `readme: 'main:README.md'`.

- [ ] **Step 2: Submit three separate pull requests**

Use titles:

```text
Create com.immersive.foundation.yml
Create com.immersive.logging.yml
Create com.immersive.pooling.yml
```

Attach every created PR to the current Codex task. Do not combine metadata files
into one PR because OpenUPM's automatic path expects separate submissions.

- [ ] **Step 3: Wait for merge and package builds**

Treat moderator approval/build processing as an external gate. Poll PR state
with bounded waits; once merged, query `https://package.openupm.com` rather than
assuming merge equals package availability.

- [ ] **Step 4: Prove registry availability**

Run:

```powershell
npm view com.immersive.foundation@0.2.1 version --registry https://package.openupm.com
npm view com.immersive.logging@0.2.2 version --registry https://package.openupm.com
npm view com.immersive.pooling@0.2.1 version --registry https://package.openupm.com
```

Expected exact outputs: `0.2.1`, `0.2.2`, `0.2.1`.

### Task 6: Prepare Audio 0.2.2

**Files:**
- Create: `C:/Projetos/ImmersivePackages/com.immersive.audio/LICENSE.md`
- Create: `C:/Projetos/ImmersivePackages/com.immersive.audio/LICENSE.md.meta`
- Create: `C:/Projetos/ImmersivePackages/com.immersive.audio/CHANGELOG.md`
- Create: `C:/Projetos/ImmersivePackages/com.immersive.audio/CHANGELOG.md.meta`
- Modify: `C:/Projetos/ImmersivePackages/com.immersive.audio/package.json`
- Modify: `C:/Projetos/ImmersivePackages/com.immersive.audio/README.md`

**Interfaces:**
- Consumes: registry-proven `com.immersive.pooling@0.2.1`.
- Produces: optional `com.immersive.audio@0.2.2`.

- [ ] **Step 1: Verify the repository and registry prerequisite**

Require clean `main`, no `v0.2.2` tag, and a successful exact-version registry
query for Pooling 0.2.1.

- [ ] **Step 2: Add license and release documentation**

Create the canonical MIT file, two unique Unity `.meta` files and a `0.2.2`
changelog entry. State that the dependency moves from Pooling 0.2.0 to 0.2.1 for
registry distribution without an Audio runtime/API change.

- [ ] **Step 3: Update package metadata**

Set version `0.2.2`, dependency `com.immersive.pooling: 0.2.1`, MIT and official
repository URLs under `com.immersive.audio`. Preserve all other dependency and
Unity boundary fields.

- [ ] **Step 4: Update README installation guidance**

Document canonical OpenUPM installation of `com.immersive.audio@0.2.2`, automatic
Pooling resolution, tagged Git fallback and MIT.

- [ ] **Step 5: Validate and commit**

Run:

```powershell
$repo='C:/Projetos/ImmersivePackages/com.immersive.audio'
$manifest=Get-Content -Raw "$repo/package.json" | ConvertFrom-Json
if($manifest.name -ne 'com.immersive.audio' -or $manifest.version -ne '0.2.2' -or $manifest.license -ne 'MIT'){throw 'Audio manifest mismatch'}
if($manifest.dependencies.'com.immersive.pooling' -ne '0.2.1'){throw 'Audio Pooling dependency mismatch'}
git -c "safe.directory=$repo" -C $repo diff --check
npm pack $repo --dry-run --json
git -c "safe.directory=$repo" -C $repo add -- LICENSE.md LICENSE.md.meta CHANGELOG.md CHANGELOG.md.meta package.json README.md
git -c "safe.directory=$repo" -C $repo diff --cached --check
git -c "safe.directory=$repo" -C $repo commit -m "chore: prepare OpenUPM release 0.2.2"
```

Stage only release metadata/docs and use commit:

```text
chore: prepare OpenUPM release 0.2.2
```

### Task 7: Prepare Framework 1.0.2

**Files:**
- Create: `C:/Projetos/ImmersivePackages/com.immersive.framework/LICENSE.md`
- Create: `C:/Projetos/ImmersivePackages/com.immersive.framework/LICENSE.md.meta`
- Modify: `C:/Projetos/ImmersivePackages/com.immersive.framework/CHANGELOG.md`
- Modify: `C:/Projetos/ImmersivePackages/com.immersive.framework/README.md`
- Modify: `C:/Projetos/ImmersivePackages/com.immersive.framework/package.json`
- Preserve committed spec: `C:/Projetos/ImmersivePackages/com.immersive.framework/Documentation~/Architecture/Plans/IF-PLAN-OPENUPM-PUBLIC-DISTRIBUTION.v1.md`

**Interfaces:**
- Consumes: registry-proven Foundation 0.2.1 and Logging 0.2.2.
- Produces: `com.immersive.framework@1.0.2` with automatic technical dependency resolution.

- [ ] **Step 1: Verify repository and registry prerequisites**

Require no unrelated changes, no `v1.0.2` tag, and exact successful OpenUPM
queries for Foundation 0.2.1 and Logging 0.2.2. The already committed design and
implementation plans are authorized documentation, not unrelated changes.

- [ ] **Step 2: Add MIT license**

Create canonical `LICENSE.md` and a unique Unity `.meta` file.

- [ ] **Step 3: Update package manifest**

Set version `1.0.2`, Foundation `0.2.1`, Logging `0.2.2`, license `MIT`, official
author data, and Framework URLs on the `master` branch:

```text
licensesUrl      https://github.com/ImmersiveGames/com.immersive.framework/blob/master/LICENSE.md
documentationUrl https://github.com/ImmersiveGames/com.immersive.framework#readme
changelogUrl     https://github.com/ImmersiveGames/com.immersive.framework/blob/master/CHANGELOG.md
```

Preserve Cinemachine `3.1.0` and Input System `1.19.0` exactly.

- [ ] **Step 4: Update README and changelog**

Make OpenUPM installation canonical:

```json
{
  "scopedRegistries": [
    {
      "name": "OpenUPM",
      "url": "https://package.openupm.com",
      "scopes": ["com.immersive"]
    }
  ],
  "dependencies": {
    "com.immersive.framework": "1.0.2"
  }
}
```

Retain the tagged Git fallback but state that Git consumers must declare custom
Git dependencies themselves. Add a `1.0.2` changelog entry for MIT/OpenUPM
distribution and exact dependency updates; do not claim runtime changes.

- [ ] **Step 5: Validate and commit**

Run:

```powershell
$repo='C:/Projetos/ImmersivePackages/com.immersive.framework'
$manifest=Get-Content -Raw "$repo/package.json" | ConvertFrom-Json
if($manifest.name -ne 'com.immersive.framework' -or $manifest.version -ne '1.0.2' -or $manifest.license -ne 'MIT'){throw 'Framework manifest mismatch'}
if($manifest.dependencies.'com.immersive.foundation' -ne '0.2.1'){throw 'Framework Foundation dependency mismatch'}
if($manifest.dependencies.'com.immersive.logging' -ne '0.2.2'){throw 'Framework Logging dependency mismatch'}
if($manifest.dependencies.'com.unity.cinemachine' -ne '3.1.0'){throw 'Framework Cinemachine dependency mismatch'}
if($manifest.dependencies.'com.unity.inputsystem' -ne '1.19.0'){throw 'Framework Input System dependency mismatch'}
git -C $repo diff --check
npm pack $repo --dry-run --json
git -C $repo add -- LICENSE.md LICENSE.md.meta CHANGELOG.md README.md package.json Documentation~/Architecture/Plans
git -C $repo diff --cached --check
git -C $repo commit -m "chore: prepare OpenUPM release 1.0.2"
```

Also parse every release-facing link and run a repository-wide Unity `.meta` GUID
uniqueness scan before staging. Stage only license, manifest, README, changelog
and the approved plan documents. The commit message is:

```text
chore: prepare OpenUPM release 1.0.2
```

### Task 8: Publish Audio and Framework GitHub releases

**Files:**
- No new source files.
- Read: committed manifests and changelogs.

**Interfaces:**
- Consumes: Tasks 6–7 plus registry prerequisites from Task 5.
- Produces: `v0.2.2` Audio and `v1.0.2` Framework tags/releases.

- [ ] **Step 1: Reverify committed trees and dependency versions**

Require clean trees, tag/version equality and dependency versions already proven
available from OpenUPM.

- [ ] **Step 2: Tag and push atomically**

Create annotated tags `v0.2.2` and `v1.0.2`, then push each repository branch
and tag atomically. Framework pushes `master`; Audio pushes `main`.

- [ ] **Step 3: Publish and verify GitHub Releases**

Create stable, non-draft Releases named `Immersive Audio 0.2.2` and
`Immersive Framework 1.0.2`; verify their public API metadata and remote refs.

### Task 9: Register Audio and Framework with OpenUPM

**Files:**
- External create: `openupm/openupm:data/packages/com.immersive.audio.yml`
- External create: `openupm/openupm:data/packages/com.immersive.framework.yml`

**Interfaces:**
- Consumes: published prerequisite packages and Task 8 releases.
- Produces: complete public `com.immersive` registry graph.

- [ ] **Step 1: Create exact metadata**

Audio metadata:

```yaml
name: com.immersive.audio
displayName: Immersive Audio
description: Unity audio services and authoring primitives for Immersive packages.
repoUrl: 'https://github.com/ImmersiveGames/com.immersive.audio'
parentRepoUrl: null
licenseSpdxId: MIT
licenseName: MIT License
topics:
  - audio
gitTagPrefix: ''
gitTagIgnore: ''
minVersion: '0.2.2'
trackingMode: git
image: ''
readme: 'main:README.md'
hunter: rinnocenti
```

Framework metadata:

```yaml
name: com.immersive.framework
displayName: Immersive Framework
description: Runtime, authoring, diagnostics and validation surfaces for Unity projects.
repoUrl: 'https://github.com/ImmersiveGames/com.immersive.framework'
parentRepoUrl: null
licenseSpdxId: MIT
licenseName: MIT License
topics:
  - frameworks
  - camera
  - input-management
gitTagPrefix: ''
gitTagIgnore: ''
minVersion: '1.0.2'
trackingMode: git
image: ''
readme: 'master:README.md'
hunter: rinnocenti
```

- [ ] **Step 2: Submit separate pull requests**

Use titles `Create com.immersive.audio.yml` and
`Create com.immersive.framework.yml`; attach both PRs to the current Codex task.

- [ ] **Step 3: Wait for merge/build and verify versions**

Do not treat pending moderation as completion. After merge/build, require:

```powershell
npm view com.immersive.audio@0.2.2 version --registry https://package.openupm.com
npm view com.immersive.framework@1.0.2 version --registry https://package.openupm.com
```

- [ ] **Step 4: Verify published dependency manifests**

Query OpenUPM and assert:

```text
Audio 0.2.2     -> com.immersive.pooling 0.2.1
Framework 1.0.2 -> com.immersive.foundation 0.2.1
Framework 1.0.2 -> com.immersive.logging 0.2.2
Framework 1.0.2 -> com.unity.cinemachine 3.1.0
Framework 1.0.2 -> com.unity.inputsystem 1.19.0
```

### Task 10: Record real completion and hand off Unity validation

**Files:**
- Modify: `C:/Projetos/ImmersivePackages/com.immersive.framework/Documentation~/Architecture/Tracking/IF-TRACK-Framework.md`
- Modify if registry instructions changed during publication: `C:/Projetos/ImmersivePackages/com.immersive.framework/README.md`

**Interfaces:**
- Consumes: verified OpenUPM versions and dependency manifests.
- Produces: honest distribution status and consumer validation checklist.

- [ ] **Step 1: Update only the mutable tracker**

Record exact GitHub Release URLs, OpenUPM package URLs, published versions and
the remaining `PENDING UNITY COMPILE/IMPORT VALIDATION` gate. Do not mutate the
approved plan bodies.

- [ ] **Step 2: Run final static verification**

Require clean/static-valid states in all five package repositories, exact remote
tag commits, public stable Releases, registry versions and dependency graph.
Run Markdown link checks and `git diff --check` for final Framework docs.

- [ ] **Step 3: Commit and push tracker evidence**

Commit only the tracker and any strictly necessary corrected installation text:

```text
docs: record OpenUPM package publication
```

Push `master` without moving `v1.0.2`.

- [ ] **Step 4: Provide manual Unity validation checklist**

Ask the user to validate in a clean Unity `6000.5.0f1` project:

```text
configure OpenUPM scope com.immersive
add com.immersive.framework 1.0.2 only
confirm automatic Foundation 0.2.1 and Logging 0.2.2 resolution
confirm Cinemachine 3.1.0 and Input System 1.19.0 resolution
confirm package import and compile
optionally add Audio 0.2.2 and confirm Pooling 0.2.1 resolution
```

Expected final status: registry publication verified; Unity compile/import remains
pending until the user supplies evidence.
