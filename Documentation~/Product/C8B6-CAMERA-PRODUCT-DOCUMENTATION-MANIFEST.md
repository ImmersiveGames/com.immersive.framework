> Superseded by ADR-PROD-0006. This document is historical and is not current implementation guidance.

# C8B6 — Camera Product Documentation

Status: documentation cut.  
Package: `com.immersive.framework`

## Objetivo

Consolidar a superfície oficial de câmera após C8B4C e C8B5, distinguindo claramente o fluxo designer-first `CameraComposer` da integração técnica Route/Activity.

## Tipo

```text
Documentação / consolidação de produto
```

## Escopo

- Guia rápido de decisão entre `CameraComposer` e bindings Route/Activity.
- Início rápido de `CameraComposer` com `PlayerComposer`.
- Uso de targets explícitos.
- Validate, Apply/Rebuild, idempotência e debug.
- Shape técnico dos outputs Route/Activity.
- Lista explícita de APIs legacy removidas.
- QA técnico e proof FIRSTGAME.
- Correção de referências legacy em documentação corrente.

## Fora de escopo

- Alteração de runtime/editor code.
- Novo contrato de release/rebaixamento de priority no exit.
- Blends, multiplayer, split-screen ou spectator.
- Novo sample Unity.
- Alterações no QAFramework ou FIRSTGAME.

## Arquivos criados

```text
Documentation~/Guides/Camera-Product-Usage.md
Documentation~/Product/C8B6-CAMERA-PRODUCT-DOCUMENTATION-MANIFEST.md
```

## Arquivos alterados

```text
Documentation~/README.md
Documentation~/Current/00-Current-State.md
Documentation~/Current/02-Usage-Map.md
Documentation~/Guides/Camera-Architecture-Flow.md
```

## Arquivos removidos

```text
nenhum
```

## Superfície de produto afetada

```text
CameraComposer
CameraRecipe
PlayerComposer camera targets
FrameworkRouteCameraBinding
FrameworkActivityCameraBinding
FrameworkCinemachineCameraOutputSource
FrameworkCinemachineOutputApplier
```

## Fluxo de uso esperado

```text
Gameplay camera comum:
PlayerComposer -> CameraComposer -> Validate -> Apply/Rebuild

Lifecycle camera específico:
Route/Activity binding -> explicit Cinemachine output -> output applier
```

## Smoke técnico esperado

Nenhum smoke novo. Manter as evidências existentes:

```text
C8B4C PASS
C8B2 PASS
C7 PASS
FIRSTGAME CameraComposerProof PASS
```

## Critérios de aceite técnico

- Documentação não referencia `FrameworkCameraDirector` ou `FrameworkCameraAnchorHost` como APIs ativas.
- Usage Map aponta para APIs atuais.
- Guia não promete release automático inexistente.
- Nomes de componentes e menus correspondem ao código atual.
- Nenhum runtime/editor code alterado.

## Critérios de aceite de produto

- Usuário consegue decidir qual superfície usar.
- Usuário consegue configurar `CameraComposer` sem conhecer contratos internos.
- Validate/Apply/Rebuild e debug estão explicados.
- Route/Activity aparece como integração técnica, não fluxo principal.
- Legacy removido está claramente marcado como proibido.

## Ganho arquitetural

A documentação passa a refletir uma única autoridade canônica de câmera e evita reintrodução acidental do director/rig legacy.

## Ganho de usabilidade

O fluxo principal fica executável como sequência curta de Inspector, com critérios de diagnóstico e idempotência explícitos.

## Commit message sugerida

```text
Docs: add canonical camera product usage guide
```
