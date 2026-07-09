# Camera Architecture Audit

Status: Audit / product correction
Package: `com.immersive.framework`
Surface: `Camera Recipe / Camera Composer`

## Objetivo

Auditar o estado atual da arquitetura de camera antes de avançar a Product Surface `Camera Recipe / Camera Composer`.

A auditoria existe porque o eixo Camera ainda nao esta alinhado com o plano de produto. A implementacao atual possui contratos tecnicos uteis, mas ainda nao forma uma superficie authoravel coerente para:

```text
single-player camera
player-follow camera
route/activity camera
multiplayer camera
shared target camera
split-screen/local-player camera
```

## Escopo

Este corte documenta:

```text
estado atual da camera no package
lacunas contra os docs de produto
separacao entre camera tecnica e camera authoravel
riscos de avancar CameraComposer cedo demais
recomendacao de proxima ordem de cortes
```

## Fora de escopo

Este corte nao implementa:

```text
CameraComposer
CameraRecipe
CameraRuntimeContext
CameraManager
multiplayer camera
split-screen
Cinemachine integration
QA smoke
FIRSTGAME integration
```

## Evidencia auditada

A arquitetura atual tem pelo menos tres linhas separadas.

### 1. Route/Activity camera skeleton

Arquivos principais:

```text
Runtime/Camera/Unity/FrameworkCameraDirector.cs
Runtime/Camera/Unity/FrameworkRouteCameraBinding.cs
Runtime/Camera/Unity/FrameworkActivityCameraBinding.cs
Runtime/Camera/FrameworkCameraActivityPolicy.cs
Runtime/Camera/FrameworkCameraRigRole.cs
Runtime/Camera/FrameworkCameraScope.cs
Runtime/Camera/IFrameworkCameraRigApplier.cs
```

Essa linha seleciona um rig efetivo entre default/route/activity e permite um applier opcional.

Limite atual:

```text
nao possui Player scope
nao possui PlayerSlot ownership
nao possui local-player camera
nao possui split-screen
nao possui shared multi-target camera
nao possui spectator/owner policy
```

### 2. PlayerView camera binding chain

Arquivos principais:

```text
Runtime/PlayerBinding/PlayerViewBindingTargetBehaviour.cs
Runtime/PlayerBinding/PlayerViewCameraTargetBindingTargetBehaviour.cs
Runtime/PlayerBinding/PlayerViewCameraActivationTargetBehaviour.cs
Runtime/PlayerBinding/PlayerViewCameraTargetBindingAdapter.cs
Runtime/PlayerBinding/PlayerViewCameraActivationAdapter.cs
Runtime/PlayerViews/PlayerViewBehaviour.cs
Documentation~/Current/06-PlayerView-Camera-Binding-Chain.md
```

Essa linha e tecnica e incremental:

```text
PlayerViewBindingAdapter
  -> PlayerViewCameraTargetBindingAdapter
  -> PlayerViewCameraActivationAdapter
```

Ela nao e uma Product Surface. O proprio documento corrente declara que esta cadeia nao cria lifecycle, director, arbitration, Cinemachine ou FIRSTGAME setup.

### 3. PlayerComposer anchors

O `PlayerComposer` ja tem campos de camera target e look-at target e pode materializar `FrameworkCameraAnchorHost` em `_Framework/_Bindings`.

Limite atual:

```text
PlayerComposer cria ou referencia anchors.
Camera ainda nao consome esses anchors como produto.
Nao existe relacao authoravel oficial CameraComposer -> PlayerComposer.
```

## Conclusao tecnica

O estado atual e uma combinacao de:

```text
Route/Activity camera skeleton
PlayerView camera technical binding chain
PlayerComposer camera anchors
```

Isso e util, mas ainda nao e uma Product Surface de camera.

A arquitetura esta fora do plano em tres pontos principais:

1. **Player consumption incompleto**

   O plano exige que CameraComposer consuma anchors gerados pelo PlayerComposer. Isso ainda nao esta formalizado como API/produto.

2. **Single vs multiplayer nao modelado**

   A camera atual nao distingue os modelos:

   ```text
   single-player follow camera
   local multiplayer split-screen
   shared multiplayer group camera
   online/remote/spectator camera
   route/activity cinematic camera
   ```

3. **Route/Activity e PlayerView sao trilhos separados**

   `FrameworkCameraDirector` fala em default/route/activity.
   `PlayerViewCameraActivationAdapter` fala em camera explicita de PlayerView.

   Nao ha ainda uma decisao clara sobre como esses trilhos se relacionam quando uma camera e do Player, da Activity, da Route ou de um player local especifico.

## Risco do CameraComposer MVP atual

Um `CameraComposer` que apenas materializa rig/camera/bindings e util como foundation, mas nao deve ser tratado como fechamento de MVP.

Ele deve ser classificado como:

```text
CameraComposer MVP-A — Rig/Bindings Foundation
```

Nao como:

```text
Camera Product Surface PASS
```

Sem a ligacao tipada com PlayerComposer e sem a taxonomia single/multi, o Composer pode cristalizar uma API errada.

## Taxonomia minima recomendada

Antes de implementar mais camera, formalizar pelo menos estes modos:

```text
RouteCamera
  camera pertence a Route; fallback geral.

ActivityCamera
  camera pertence a Activity; pode substituir ou reter Activity ate Route exit.

SinglePlayerFollowCamera
  uma camera consome anchors de um PlayerComposer.

LocalPlayerCamera
  camera pertence a um PlayerSlot local especifico; base para split-screen.

SharedPlayerGroupCamera
  uma camera segue multiplos players/targets; exige target aggregation posterior.

SpectatorOrDebugCamera
  camera explicitamente fora do ownership normal de Player/Route/Activity.
```

## Recomendacao de ordem

### C0 — Camera Architecture Audit

Este documento.

### C1 — Camera Ownership / Target Source ADR

Definir oficialmente:

```text
camera ownership model
camera target source policy
single-player vs multiplayer boundary
relacao entre Route/Activity/Player camera
como PlayerComposer anchors sao consumidos
```

### C2 — Camera contracts stabilization

Ajustar contratos pequenos, se necessario:

```text
CameraTargetSourcePolicy
CameraOwnershipScope
PlayerCameraTargetSource
CameraComposer -> PlayerComposer reference model
```

Evitar criar runtime context aqui, salvo se a autoridade runtime estiver clara.

### C3 — CameraComposer Single Player MVP

Implementar apenas o caso seguro:

```text
SinglePlayerFollowCamera
CameraComposer references PlayerComposer explicitly
uses PlayerComposer.CameraTarget / LookAtTarget
materializes anchor host and target/activation evidence
idempotent Apply/Rebuild
```

### C4 — FIRSTGAME proof

Provar em jogo real:

```text
camera real consome PlayerPrototype/Anchors/CameraTarget
camera real consome PlayerPrototype/Anchors/LookAtTarget
setup local antigo deixa de ser fluxo principal
```

### C5 — Multiplayer camera plan

So depois disso decidir:

```text
split-screen
shared group camera
target aggregation
per-local-player ownership
viewport/display ownership
```

## Criterios de aceite tecnico para proximo corte

```text
nao criar singleton
nao criar CameraManager global
nao usar Camera.main lookup
nao usar nome/path como identidade funcional
nao criar Player automaticamente
nao misturar single-player e multiplayer no mesmo MVP sem contrato
falhas obrigatorias explicitas e diagnosticaveis
```

## Criterios de aceite de produto para proximo corte

```text
designer entende se camera e Route, Activity ou Player
CameraComposer deixa claro de onde vem o target
CameraComposer mostra quando usa PlayerComposer anchors
Apply/Rebuild materializa/repara sem duplicar
Advanced/Debug mostra evidencias tecnicas
FIRSTGAME consegue usar sem helper local
```

## Decisao recomendada

Pausar novos codigos de CameraComposer ate criar o ADR de ownership/target source.

O delta `CameraComposer MVP-A`, se aplicado, deve ser tratado como spike/foundation tecnica, nao como Product Surface validada.

## Commit message sugerida

```text
Docs: audit camera product surface alignment
```
