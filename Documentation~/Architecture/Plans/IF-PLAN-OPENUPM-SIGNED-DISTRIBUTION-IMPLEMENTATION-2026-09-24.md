# Signed OpenUPM Distribution Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use
> `superpowers:executing-plans` to implement this plan task-by-task. Steps use
> checkbox (`- [ ]`) syntax for tracking.

**Goal:** Publish the next five `com.immersive.*` package versions as
Unity-signed GitHub Release archives and make OpenUPM deliver those exact
archives so Unity Package Manager no longer reports a missing signature.

**Architecture:** A single Immersive Games Unity Service Account signs every
package through the Unity UPM CLI. GitHub Actions owns packaging, attestation
verification and release-asset creation. OpenUPM uses GitHub Release asset
tracking and publishes the signed archive unchanged. Dependency-free packages
release first; Audio and Framework release only after their Immersive
dependencies are available from the registry.

**Tech Stack:** Unity UPM CLI, Unity Service Accounts, GitHub Actions and
Organization secrets, GitHub Releases, OpenUPM GitHub Action, OpenUPM package
metadata, Unity Package Manager manifests.

**Spec:** `Documentation~/Architecture/Plans/IF-PLAN-OPENUPM-SIGNED-DISTRIBUTION.v2.md`

## Global Constraints

- No runtime, Editor, assembly, serialized field or public API change is
  authorized.
- Do not run Unity Editor, builds, imports, tests, Play Mode, smoke or
  batchmode.
- Signing credentials must never be placed in a repository, terminal command,
  documentation, log, screenshot or final response.
- Use GitHub Organization secrets restricted to exactly the five package
  repositories.
- Existing published tags and unsigned registry versions are immutable.
- OpenUPM metadata must be merged before any new version tag is pushed.
- A new tag is allowed only when it equals the committed `package.json`
  version.
- A release is allowed only when the archive contains both
  `package/package.json` and `package/.attestation.p7m`.
- Foundation, Logging and Pooling release before Audio and Framework.
- A failed published cut receives a new patch version; never move or replace a
  published tag.
- Framework remains on `master`; the other four package repositories remain on
  `main`.
- Audio remains optional and must not become a Framework dependency.
- Final status remains `PENDING UNITY COMPILE/IMPORT VALIDATION` until the user
  confirms a clean Unity `6000.5.0f1` consumer import.

## Accepted Release Graph

```text
com.immersive.foundation 0.2.2
com.immersive.logging    0.2.3
com.immersive.pooling    0.2.2

com.immersive.audio 0.2.3
└── com.immersive.pooling 0.2.2

com.immersive.framework 1.0.3
├── com.immersive.foundation 0.2.2
├── com.immersive.logging 0.2.3
├── com.unity.cinemachine 3.1.7
└── com.unity.inputsystem 1.19.0
```

## Review Focus

- Credential exposure: create and store secrets only through authenticated web
  forms; confirm that workflow files reference secret names, never values.
- Source-mode race: require all five OpenUPM `githubRelease` metadata PRs to be
  merged before pushing any new package tag.
- Invalid tag: stop the workflow before packing when the tag version differs
  from `package.json.version`.
- Unsigned artifact: stop before GitHub Release creation when the archive lacks
  `.attestation.p7m` or its embedded package identity differs from the manifest.
- Dependency ordering: prove exact base versions are signed and installable
  from OpenUPM before releasing Audio or Framework.
- Unity compatibility: Framework must use Cinemachine `3.1.7`; do not change
  Input System `1.19.0`.

---

### Task 1: Create the restricted Unity signing identity

**External state:**
- Unity Dashboard organization: `Immersive Games`
- Service account: `Immersive Package Signing`
- Required role: `Package Manager Package Signer`
- Required outputs: organization ID, key ID and key secret

**Interfaces:**
- Produces: the three values later stored as GitHub secrets.
- Does not produce: local files, committed credentials or a downloadable key
  file retained on disk.

- [ ] **Step 1: Open the Immersive Games organization administration**

Use the authenticated Unity Dashboard. If Unity asks for login, password,
passkey or MFA, stop at that form and ask the user to complete it. Confirm the
organization name before creating anything.

- [ ] **Step 2: Create the signing-only service account**

Create `Immersive Package Signing`. Assign only the organization role
`Package Manager Package Signer`; do not grant Owner, Manager or unrelated
Cloud roles.

- [ ] **Step 3: Generate one service-account key**

Generate one active key. Capture its key ID and one-time secret only for the
immediate GitHub secret entry in Task 2. Do not paste either value into chat,
shell, a text editor or repository file.

- [ ] **Step 4: Capture the Unity organization ID**

Read the stable Immersive Games organization ID from the organization settings.
Keep it with the two key values only until Task 2 is complete.

- [ ] **Step 5: Verify least privilege**

Before leaving Unity Dashboard, confirm:

```text
service account = Immersive Package Signing
organization    = Immersive Games
role            = Package Manager Package Signer
active keys     = 1
```

If account creation or key management is unavailable, report which Unity
organization permission is missing; do not substitute a personal credential.

### Task 2: Store signing values as restricted GitHub Organization secrets

**External state:**
- GitHub organization: `ImmersiveGames`
- Repositories: Foundation, Logging, Pooling, Audio and Framework package repos
- Secrets: `UPM_SERVICE_ACCOUNT_KEY_ID`,
  `UPM_SERVICE_ACCOUNT_KEY_SECRET`, `UPM_ORG_ID`

**Interfaces:**
- Consumes: the three values from Task 1.
- Produces: GitHub Actions access for exactly five repositories.

- [ ] **Step 1: Open GitHub Organization Actions secrets**

Use `ImmersiveGames` organization settings under Secrets and variables →
Actions. If GitHub asks for login, password, passkey, sudo mode or MFA, stop at
that form and ask the user to complete it.

- [ ] **Step 2: Create the three secrets**

Create these names exactly:

```text
UPM_SERVICE_ACCOUNT_KEY_ID
UPM_SERVICE_ACCOUNT_KEY_SECRET
UPM_ORG_ID
```

For every secret choose selected-repository visibility and select exactly:

```text
com.immersive.foundation
com.immersive.logging
com.immersive.pooling
com.immersive.audio
com.immersive.framework
```

- [ ] **Step 3: Verify configuration without revealing values**

Confirm that all three secret names exist, show access to five selected
repositories and do not display a value. Clear any clipboard entry that held a
key value. Do not retain the one-time secret elsewhere.

### Task 3: Add the signed-release workflow to all five repositories

**Files:**
- Create: `C:/Projetos/ImmersivePackages/com.immersive.foundation/.github/workflows/signed-release.yml`
- Create: `C:/Projetos/ImmersivePackages/com.immersive.logging/.github/workflows/signed-release.yml`
- Create: `C:/Projetos/ImmersivePackages/com.immersive.pooling/.github/workflows/signed-release.yml`
- Create: `C:/Projetos/ImmersivePackages/com.immersive.audio/.github/workflows/signed-release.yml`
- Create: `C:/Projetos/ImmersivePackages/com.immersive.framework/.github/workflows/signed-release.yml`

**Interfaces:**
- Consumes: three GitHub Organization secrets.
- Produces: signed release asset and an OpenUPM publish result for every future
  `vX.Y.Z` tag.

- [ ] **Step 1: Verify repository baselines**

For each repository, require no unrelated local change and record its current
branch/upstream. In Framework, the accepted v2 spec and this implementation
plan are intended local documentation changes, not unrelated changes.

- [ ] **Step 2: Create the same root-package workflow in every repository**

Use this exact workflow in all five files:

```yaml
name: Sign and release UPM package

on:
  push:
    tags:
      - "v*.*.*"

permissions:
  contents: write
  id-token: write

concurrency:
  group: signed-release-${{ github.ref }}
  cancel-in-progress: false

jobs:
  release:
    name: Sign, release and publish
    runs-on: ubuntu-latest
    env:
      UPM_SERVICE_ACCOUNT_KEY_ID: ${{ secrets.UPM_SERVICE_ACCOUNT_KEY_ID }}
      UPM_SERVICE_ACCOUNT_KEY_SECRET: ${{ secrets.UPM_SERVICE_ACCOUNT_KEY_SECRET }}
      UPM_ORG_ID: ${{ secrets.UPM_ORG_ID }}
      DIST_DIR: /tmp/signed-upm-dist

    steps:
      - name: Check out tagged source
        uses: actions/checkout@v4

      - name: Read and verify package metadata
        id: package
        shell: bash
        run: |
          package_name="$(jq -r '.name' package.json)"
          package_version="$(jq -r '.version' package.json)"
          tag_version="${GITHUB_REF_NAME#v}"

          test -n "$package_name"
          test -n "$package_version"
          if [ "$tag_version" != "$package_version" ]; then
            printf 'Tag version %s does not match package version %s\n' "$tag_version" "$package_version" >&2
            exit 1
          fi

          echo "PACKAGE_NAME=$package_name" >> "$GITHUB_ENV"
          echo "PACKAGE_VERSION=$package_version" >> "$GITHUB_ENV"
          echo "name=$package_name" >> "$GITHUB_OUTPUT"
          echo "version=$package_version" >> "$GITHUB_OUTPUT"
          printf 'Package: %s@%s\n' "$package_name" "$package_version"

      - name: Install Unity UPM CLI
        shell: bash
        run: |
          curl -fsSL https://cdn.packages.unity.com/upm-cli/install.sh -o install.sh
          bash install.sh
          echo "$HOME/.upm/bin" >> "$GITHUB_PATH"

      - name: Verify Unity UPM CLI
        run: upm --help

      - name: Sign package
        shell: bash
        run: |
          mkdir -p "$DIST_DIR"
          upm pack . --organization-id "$UPM_ORG_ID" --destination "$DIST_DIR"

      - name: Verify signed archive
        shell: bash
        run: |
          shopt -s nullglob
          archives=("$DIST_DIR"/*.tgz "$DIST_DIR"/*.tar.gz)
          if [ "${#archives[@]}" -ne 1 ]; then
            printf 'Expected one signed archive, found %s\n' "${#archives[@]}" >&2
            exit 1
          fi

          archive="${archives[0]}"
          tar -tzf "$archive" | grep -qx 'package/package.json'
          tar -tzf "$archive" | grep -qx 'package/.attestation.p7m'

          archive_name="$(tar -xOzf "$archive" package/package.json | jq -r '.name')"
          archive_version="$(tar -xOzf "$archive" package/package.json | jq -r '.version')"
          test "$archive_name" = "$PACKAGE_NAME"
          test "$archive_version" = "$PACKAGE_VERSION"

          printf 'Signed archive: %s\n' "$(basename "$archive")"

      - name: Create GitHub Release and upload signed archive
        uses: softprops/action-gh-release@v2
        with:
          name: ${{ steps.package.outputs.name }} ${{ steps.package.outputs.version }}
          generate_release_notes: true
          fail_on_unmatched_files: true
          files: |
            /tmp/signed-upm-dist/*.tgz
            /tmp/signed-upm-dist/*.tar.gz

      - name: Publish signed archive through OpenUPM
        uses: openupm/openupm-action@v1
        with:
          package: ${{ steps.package.outputs.name }}
          tag: ${{ github.ref_name }}
          timeout-minutes: 30
```

- [ ] **Step 3: Perform static workflow checks**

For all five files, verify exact secret references, `contents: write`,
`id-token: write`, the root `package.json`, `upm pack .`, both archive members,
release upload before OpenUPM action, and tag/version equality. Run
`git diff --check`. Do not execute the workflow or Unity locally.

- [ ] **Step 4: Commit and push workflow changes without tagging**

Use one repository-local commit per package:

```text
ci: add signed UPM release workflow
```

Push Foundation, Logging, Pooling and Audio to `main`; push Framework to
`master`. Do not create any release tags yet.

### Task 4: Switch all OpenUPM package metadata to GitHub Release assets

**External files in `openupm/openupm`:**
- Modify: `data/packages/com.immersive.foundation.yml`
- Modify: `data/packages/com.immersive.logging.yml`
- Modify: `data/packages/com.immersive.pooling.yml`
- Modify: `data/packages/com.immersive.audio.yml`
- Modify: `data/packages/com.immersive.framework.yml`

**Interfaces:**
- Consumes: public repositories with the signed-release workflow already on the
  default branch.
- Produces: OpenUPM source mode that preserves the Unity signature.

- [ ] **Step 1: Create one OpenUPM change per package**

In each metadata file replace:

```yaml
trackingMode: git
```

with the package-specific pair:

```yaml
trackingMode: githubRelease
githubReleaseAssetName: 'com.immersive.foundation-'
```

Use the matching package name as the prefix in each of the other four files.
Preserve all unrelated metadata and current `minVersion` values.

- [ ] **Step 2: Submit five separate pull requests**

Use titles in this shape:

```text
Use GitHub Release assets for com.immersive.foundation
```

Attach every created pull request to the current task. Do not combine the five
package metadata files into one submission.

- [ ] **Step 3: Wait for all five merges**

Treat an open, approved or CI-green PR as incomplete. Confirm the merged
default-branch YAML for each package contains `trackingMode: githubRelease` and
the correct stable asset prefix.

- [ ] **Step 4: Enforce the source-mode hard gate**

Do not start Task 5 until all five merged files are visible on
`openupm/openupm` default branch. If any PR remains pending, report the PR URL
and wait; do not push a new package tag.

### Task 5: Prepare the dependency-free signed versions

**Files:**
- Modify: `C:/Projetos/ImmersivePackages/com.immersive.foundation/package.json`
- Modify: `C:/Projetos/ImmersivePackages/com.immersive.foundation/CHANGELOG.md`
- Modify: `C:/Projetos/ImmersivePackages/com.immersive.foundation/README.md`
- Modify: `C:/Projetos/ImmersivePackages/com.immersive.logging/package.json`
- Modify: `C:/Projetos/ImmersivePackages/com.immersive.logging/CHANGELOG.md`
- Modify: `C:/Projetos/ImmersivePackages/com.immersive.logging/README.md`
- Modify: `C:/Projetos/ImmersivePackages/com.immersive.pooling/package.json`
- Modify: `C:/Projetos/ImmersivePackages/com.immersive.pooling/CHANGELOG.md`
- Modify: `C:/Projetos/ImmersivePackages/com.immersive.pooling/README.md`

**Interfaces:**
- Produces: Foundation `0.2.2`, Logging `0.2.3`, Pooling `0.2.2`.
- Preserves: every runtime and API contract.

- [ ] **Step 1: Set exact patch versions**

Change only the manifest version field:

```text
Foundation 0.2.1 -> 0.2.2
Logging    0.2.2 -> 0.2.3
Pooling    0.2.1 -> 0.2.2
```

- [ ] **Step 2: Update release-facing documentation**

Add dated changelog entries stating that each patch introduces signed Unity UPM
distribution without runtime/API changes. Update README version examples and
state that OpenUPM is the canonical signed installation path.

- [ ] **Step 3: Validate only release metadata**

Parse the three JSON manifests, verify exact names/versions, check that no
Runtime, Editor, asmdef or serialized asset changed, and run `git diff --check`.
Do not run Unity or package behavior tests.

- [ ] **Step 4: Commit and push without tags**

Use these commits:

```text
chore: prepare signed release 0.2.2   # Foundation
chore: prepare signed release 0.2.3   # Logging
chore: prepare signed release 0.2.2   # Pooling
```

Push the three `main` branches. Reconfirm Task 4's merged source mode before
creating tags.

### Task 6: Publish Foundation, Logging and Pooling

**Interfaces:**
- Consumes: Task 5 commits and the three configured GitHub secrets.
- Produces: signed GitHub Releases and signed OpenUPM versions.

- [ ] **Step 1: Create and push annotated tags**

Create these immutable tags on the exact release commits:

```text
com.immersive.foundation -> v0.2.2
com.immersive.logging    -> v0.2.3
com.immersive.pooling    -> v0.2.2
```

Push one tag at a time so a failed signing workflow cannot be hidden among
parallel releases.

- [ ] **Step 2: Wait for each GitHub Actions workflow**

Require a successful `Sign, release and publish` job for each package. If login
credentials, signing permission or organization identity fail, fix the external
configuration and create a new patch version only if a release/tag was already
published.

- [ ] **Step 3: Inspect public release evidence**

For every version require:

```text
GitHub Release is stable and uses the exact tag
exactly one .tgz or .tar.gz package asset exists
archive contains package/package.json
archive contains package/.attestation.p7m
embedded name/version match the release
OpenUPM action reports succeeded
OpenUPM action reports signed=true
```

- [ ] **Step 4: Prove registry availability**

Run exact registry queries:

```powershell
npm view com.immersive.foundation@0.2.2 version --registry https://package.openupm.com
npm view com.immersive.logging@0.2.3 version --registry https://package.openupm.com
npm view com.immersive.pooling@0.2.2 version --registry https://package.openupm.com
```

Expected outputs are `0.2.2`, `0.2.3` and `0.2.2`. Do not release dependents
until these exact versions and signed states are confirmed.

### Task 7: Prepare Audio 0.2.3

**Files:**
- Modify: `C:/Projetos/ImmersivePackages/com.immersive.audio/package.json`
- Modify: `C:/Projetos/ImmersivePackages/com.immersive.audio/CHANGELOG.md`
- Modify: `C:/Projetos/ImmersivePackages/com.immersive.audio/README.md`

**Interfaces:**
- Consumes: signed `com.immersive.pooling@0.2.2`.
- Produces: optional signed `com.immersive.audio@0.2.3`.

- [ ] **Step 1: Update the exact manifest graph**

Set:

```json
{
  "version": "0.2.3",
  "dependencies": {
    "com.immersive.pooling": "0.2.2"
  }
}
```

Preserve every other existing manifest field.

- [ ] **Step 2: Update changelog and installation examples**

Document signed OpenUPM distribution and the Pooling `0.2.2` dependency. State
that no Audio runtime/API behavior changed.

- [ ] **Step 3: Verify and commit**

Parse the manifest, prove the exact name/version/dependency, confirm no runtime
or assembly change, run `git diff --check`, then commit:

```text
chore: prepare signed release 0.2.3
```

Push `main` without a tag, then reconfirm signed Pooling registry availability.

### Task 8: Prepare Framework 1.0.3 and Unity 6000.5 compatibility

**Files:**
- Modify: `C:/Projetos/ImmersivePackages/com.immersive.framework/package.json`
- Modify: `C:/Projetos/ImmersivePackages/com.immersive.framework/CHANGELOG.md`
- Modify: `C:/Projetos/ImmersivePackages/com.immersive.framework/README.md`
- Preserve: accepted v2 spec and this implementation plan

**Interfaces:**
- Consumes: signed Foundation `0.2.2`, Logging `0.2.3`, official Cinemachine
  `3.1.7` and Input System `1.19.0`.
- Produces: signed Framework `1.0.3`.

- [ ] **Step 1: Update the exact manifest graph**

Set:

```json
{
  "version": "1.0.3",
  "dependencies": {
    "com.immersive.foundation": "0.2.2",
    "com.immersive.logging": "0.2.3",
    "com.unity.cinemachine": "3.1.7",
    "com.unity.inputsystem": "1.19.0"
  }
}
```

Preserve all other existing fields. The Cinemachine patch replaces `3.1.0`
because `3.1.6+` migrated the obsolete instance-identity API used by Unity
`6000.5`.

- [ ] **Step 2: Update changelog and installation examples**

Document signed distribution, exact Immersive dependency updates and the
Cinemachine compatibility correction. Update canonical OpenUPM examples to
Framework `1.0.3`. Do not claim a Framework runtime/API change.

- [ ] **Step 3: Verify and commit the complete release cut**

Parse the manifest and prove all four dependency versions. Confirm no Runtime,
Editor, asmdef or serialized asset changed. Run `git diff --check`. Include the
accepted spec and implementation-plan commits in the branch history, then use a
release commit:

```text
chore: prepare signed release 1.0.3
```

Push `master` without a tag. Reconfirm signed Foundation and Logging registry
availability before tagging.

### Task 9: Publish Audio and Framework

**Interfaces:**
- Consumes: Tasks 7–8 and signed prerequisite versions.
- Produces: Audio `0.2.3` and Framework `1.0.3` signed releases.

- [ ] **Step 1: Tag and publish Audio**

Create annotated `v0.2.3`, push it, wait for the workflow, and require the same
release-asset, attestation, identity, OpenUPM success and `signed=true` evidence
used in Task 6.

- [ ] **Step 2: Verify the published Audio manifest**

Require OpenUPM to report:

```text
com.immersive.audio@0.2.3
└── com.immersive.pooling@0.2.2
```

- [ ] **Step 3: Tag and publish Framework**

Create annotated `v1.0.3`, push it, wait for the workflow, and require the same
signed evidence. Do not reuse or move `v1.0.2`.

- [ ] **Step 4: Verify the published Framework manifest**

Require OpenUPM to report:

```text
com.immersive.framework@1.0.3
├── com.immersive.foundation@0.2.2
├── com.immersive.logging@0.2.3
├── com.unity.cinemachine@3.1.7
└── com.unity.inputsystem@1.19.0
```

### Task 10: Record release evidence and hand off consumer validation

**Files:**
- Modify: `C:/Projetos/ImmersivePackages/com.immersive.framework/Documentation~/Architecture/Tracking/IF-TRACK-Framework.md`
- Modify only if real publication differs from documented installation:
  `C:/Projetos/ImmersivePackages/com.immersive.framework/README.md`

**Interfaces:**
- Consumes: public GitHub/OpenUPM evidence from all five releases.
- Produces: honest current status and one short manual Unity checklist.

- [ ] **Step 1: Update only the mutable tracker**

Record exact release URLs, OpenUPM package URLs, versions, signed status,
dependency graph and remaining Unity validation. Do not mutate the accepted v2
spec or this implementation plan.

- [ ] **Step 2: Run final non-Unity checks**

Require clean repository trees after intended commits, exact remote branch/tag
identity, stable GitHub Releases, signed registry states and exact dependencies.
Run `git diff --check` for the tracker update.

- [ ] **Step 3: Commit and push evidence**

Commit the tracker and any strictly necessary README correction:

```text
docs: record signed OpenUPM releases
```

Push `master` without moving `v1.0.3`.

- [ ] **Step 4: Ask for one manual Unity confirmation**

In a clean Unity `6000.5.0f1` project, the user should:

```text
configure the OpenUPM scoped registry for com.immersive
install only com.immersive.framework 1.0.3
confirm Foundation 0.2.2 and Logging 0.2.3 resolve automatically
confirm Cinemachine 3.1.7 and Input System 1.19.0 resolve automatically
confirm Package Manager shows Signature: Valid for the three Immersive packages
confirm the project imports and compiles
optionally install Audio 0.2.3 and confirm Pooling 0.2.2 and valid signatures
```

Expected final status before that report: signed registry publication verified;
Unity consumer import/compile remains pending.
