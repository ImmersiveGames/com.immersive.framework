# Git Package Install

Use this page when installing `com.immersive.framework` from Git in a Unity consumer project.

## Version and tag policy

- Package version: `1.0.0-preview.1`
- Source repository: `https://github.com/ImmersiveGames/com.immersive.framework`
- Recommended Git tag: `v1.0.0-preview.1`
- Create the tag only after Unity import/compile, Model Readiness and clean consumer install validation pass.

F60 synchronizes this package into the dedicated package repository. It does not create the tag, push or publish a release.

## Dependency policy

`com.immersive.framework` has real package dependencies on:

- `com.immersive.foundation`
- `com.immersive.logging`
- `com.unity.inputsystem`

`com.immersive.pooling` is not a framework dependency in this package version.

Unity does not support Git URLs as transitive package dependencies inside a package `package.json`. The framework package manifest uses version strings only. If the Immersive sibling packages are private Git packages and not available from a scoped registry, install them directly in the consumer project's `Packages/manifest.json`.

## Consumer manifest example

```json
{
  "dependencies": {
    "com.immersive.foundation": "https://github.com/ImmersiveGames/com.immersive.foundation.git#v1.0.0-preview.1",
    "com.immersive.logging": "https://github.com/ImmersiveGames/com.immersive.logging.git#v1.0.0-preview.1",
    "com.immersive.framework": "https://github.com/ImmersiveGames/com.immersive.framework.git#v1.0.0-preview.1"
  }
}
```

Adjust URLs only if the real repositories differ.

## Package Manager install

1. Confirm Git is installed and visible to Unity.
2. Install `com.immersive.foundation` from its pinned Git tag, unless it is available from a scoped registry.
3. Install `com.immersive.logging` from its pinned Git tag, unless it is available from a scoped registry.
4. Install `com.immersive.framework` from its pinned Git tag.
5. Let Unity resolve packages and regenerate `Packages/packages-lock.json`.
6. Run Unity import/compile.
7. Configure the minimum Model 1.0 authoring setup.
8. Run Project Settings > Immersive Framework > Model Readiness > `Run Model Readiness Check`.

Do not copy QA scenes, prefabs, `ProjectSettings` or old project assets into the package.

## Common install failures

- Git is not installed or not available to Unity.
- A sibling private package was not installed before the framework package.
- The requested Git tag does not exist yet.
- `Packages/packages-lock.json` pins an older package revision.
- The project uses a branch URL instead of a reproducible tag.
- A scoped registry is missing or does not host the sibling package version declared by the framework package manifest.

Fix the consumer project manifest or registry configuration. Do not add Git URLs to `com.immersive.framework/package.json`.
