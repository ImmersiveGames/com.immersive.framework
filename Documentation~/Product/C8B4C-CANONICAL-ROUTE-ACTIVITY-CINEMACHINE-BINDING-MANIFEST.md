# C8B4C — Canonical Route/Activity Cinemachine Binding

Status: implementation cut; Unity compile/import validation pending.  
Package: `com.immersive.framework`

## Objetivo

Estabelecer `FrameworkCinemachineCameraOutputSource` como a única autoridade ativa dos bindings Route/Activity.

## Escopo

- `FrameworkRouteCameraBinding` aplica somente output Cinemachine explícito.
- `FrameworkActivityCameraBinding` aplica somente output Cinemachine explícito.
- `UseRoute` não aplica override Activity.
- Required inválido bloqueia; optional inválido gera `Skipped`.
- Não há fallback para câmera legacy.

## Arquivos alterados

```text
Runtime/Camera/Cinemachine/FrameworkRouteCameraBinding.cs
Runtime/Camera/Cinemachine/FrameworkActivityCameraBinding.cs
```

## Arquivos removidos

```text
Runtime/Camera/Unity/FrameworkCameraDirector.cs
Runtime/Camera/Unity/FrameworkCameraAnchorHost.cs
Runtime/Camera/FrameworkCameraAnchorDescriptor.cs
Runtime/Camera/FrameworkCameraPriorityState.cs
Runtime/Camera/FrameworkCameraRigDescriptor.cs
Runtime/Camera/FrameworkCameraRigRole.cs
Runtime/Camera/FrameworkCameraScope.cs
Runtime/Camera/IFrameworkCameraRigApplier.cs
Runtime/Camera/Cinemachine/FrameworkCinemachineRigApplier.cs
```

## Superfície afetada

Route/Activity continuam bindings técnicos. `CameraComposer` permanece a superfície oficial para authoring de câmera.

## Caminho canônico

```text
FrameworkRouteCameraBinding
  -> FrameworkCinemachineCameraOutputSource
  -> FrameworkCinemachineOutputApplier

FrameworkActivityCameraBinding
  -> FrameworkCinemachineCameraOutputSource
  -> FrameworkCinemachineOutputApplier
```

Os campos de rig, anchors, director e startup binding foram removidos. Não há `FrameworkCameraDirector`, `SetActive`, `Camera.enabled`, lookup global ou fallback silencioso.

## Fora de escopo

- FIRSTGAME migration.
- Blends, multiplayer, split-screen e spectator.
- Clear/release de prioridade no exit; fica para C8B5/C8B6 com contrato explícito.

## Ganho arquitetural

Elimina duas autoridades concorrentes no mesmo binding e reduz a integração Route/Activity a um contrato explícito, validável e local.

## Status de migração

C8B4A e C8B4B foram superseded. `FrameworkCameraDirector` não é mais parte do package ativo. FIRSTGAME ainda não foi migrado neste corte.

## Commit message sugerida

```text
Framework: make Route Activity camera bindings Cinemachine canonical
```
