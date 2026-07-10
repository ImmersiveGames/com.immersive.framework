# 03 — Consumer Project Roles

Status: **canonical separation rule**.

```text
QA proves technical behavior.
FIRSTGAME proves real-game usability.
The package owns framework contracts, product surfaces and canonical documentation.
```

## Framework package

Owner: `com.immersive.framework`.

Owns runtime, authoring, Editor tooling, validators, diagnostics and `Documentation~/`. It must not contain consumer scenes, QA fixtures or FIRSTGAME gameplay.

## QAFramework

Owner: `C:\Projetos\QAFramework`.

Canonical root: `Assets/ImmersiveFrameworkQA/`.

Owns synthetic scenes, regression smokes, negative cases and technical probes. Legacy serialized camera components that remain in old QA scenes are historical fixtures, not current package authority.

## FIRSTGAME

Owner: `C:\Projetos\planet-devourer`.

Canonical root: `Assets/_Project/`.

Owns the minimal playable consumer flow and game-specific input/movement. Its current main camera proof is `PlayerComposer -> CameraComposer`; Route/Activity camera bindings are not used in that gameplay flow.

## Documentation and serialization policy

Canonical framework docs live only in the package. Consumer READMEs may contain local operating instructions and links.

Do not rename or move serialized scenes, prefabs, assets or MonoBehaviour scripts without a Unity Editor migration that preserves metadata and verifies references.
