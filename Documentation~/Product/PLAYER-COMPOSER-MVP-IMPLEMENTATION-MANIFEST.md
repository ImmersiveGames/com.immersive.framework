# Player Composer MVP Implementation Manifest

Status: Implementation delta
Date: 2026-07-09
Package: `com.immersive.framework`

## Corte

Primeira implementação MVP do `PlayerComposer` como Product Surface authoring/apply/diagnostics.

## Objetivo

Criar uma superfície oficial mínima para configurar um player framework-ready sem usar validators/smokes como fluxo principal.

## Arquivos criados

```text
Runtime/PlayerAuthoring/PlayerComposer.cs
Runtime/PlayerAuthoring.meta
Runtime/PlayerAuthoring/PlayerComposer.cs.meta
Editor/PlayerAuthoring/PlayerComposerEditor.cs
Editor/PlayerAuthoring.meta
Editor/PlayerAuthoring/PlayerComposerEditor.cs.meta
Documentation~/Product/PLAYER-COMPOSER-MVP-IMPLEMENTATION-MANIFEST.md
```

## Arquivos alterados

```text
none
```

## Arquivos removidos

```text
none
```

## Superfície de produto

`PlayerComposer` é a superfície principal de authoring da instância do player.

Ele oferece:

- campos designer-first para actor id, player slot, PlayerInput, action map, camera target, look-at target e reset;
- ações editor-only `Apply / Rebuild`, `Validate` e `Select Technical Bindings`;
- materialização técnica em `_Framework/_Bindings` quando seguro;
- Advanced/Debug para políticas e evidências técnicas.

## Materialização MVP

Root preservado para componentes com same-object fallback ou `RequireComponent`:

```text
PlayerComposer
PlayerInput
PlayerActorDeclaration
PlayerSlotDeclaration
UnityPlayerInputGateAdapter, se existir no package
UnityResetSubjectAdapter, se resetEnabled
```

`_Framework/_Bindings` para evidências técnicas:

```text
PlayerControlBindingTargetBehaviour
UnityPlayerInputBridgeTargetBehaviour
UnityPlayerInputActivationTargetBehaviour
FrameworkCameraAnchorHost, se existir no package
UnityTransformResetParticipant, se resetEnabled
PlayerSlotOccupancy, quando policy habilitada
```

## Fora de escopo

```text
PlayerRecipe real
PlayerRuntimeContext real
movement framework-owned
spawn
multiplayer join
save/progression
PlayerManager
Session generica
FIRSTGAME
QAFramework
novo smoke
novo validator
alteracao de asmdefs
```

## Observações técnicas

- O editor usa `TypeCache` e `SerializedObject` para materializar componentes existentes sem exigir APIs públicas novas neste MVP.
- Não usa reflection em runtime.
- Não usa `GameObject.name` ou path como identidade funcional.
- `_Framework/_Bindings` é materialização técnica, não autoridade principal.
- Diagnostics confirmam Apply/Rebuild, mas não são fluxo principal.

## Critérios de aceite técnico

- Compila no package.
- Nenhum asmdef alterado.
- `PlayerComposer` aparece no menu `Immersive Framework/Player/Player Composer`.
- Inspector mostra Designer, Advanced e Debug.
- `Validate` bloqueia ausência de `PlayerInput` quando input binding é requerido.
- `Validate` bloqueia action map inexistente quando input binding é requerido.
- `Apply/Rebuild` é idempotente e não duplica `_Framework`, `_Bindings`, `Anchors`, bindings ou declarations.
- `Apply/Rebuild` não remove componentes gameplay do consumidor.
- Falhas são registradas em `lastBlockingIssue`.

## Critérios de aceite de produto

- Um usuário consegue configurar um Player pelo `PlayerComposer` sem usar menus FIRSTGAME.
- Componentes técnicos deixam de ser a superfície principal do designer.
- FIRSTGAME pode usar o Composer oficial em etapa posterior.
- QAFramework pode validar o contrato oficial em etapa posterior.

## Commit message sugerida

```text
Product: add PlayerComposer MVP
```
