# Player Product Surface Closure

Status: Product surface closure
Date: 2026-07-09
Package: `com.immersive.framework`

## 1. Objetivo

Fechar o primeiro eixo de Product Surface do Immersive Framework 1.0:

```text
Player Recipe / Player Composer
```

Este fechamento registra que o Player deixou de depender de uma pilha tecnica exposta como UX principal e passou a ter uma superficie authorable minima validada em package e consumidor real.

## 2. Decisao

`PlayerRecipe / PlayerComposer` e a primeira Product Surface validada do framework.

O modelo validado e:

```text
PlayerRecipe
  intencao reutilizavel

PlayerComposer
  authoring da instancia

Apply/Rebuild
  materializacao tecnica idempotente

_Framework/_Bindings
  evidencia tecnica e bindings derivados

Diagnostics
  Validate + Apply/Rebuild breakdown

FIRSTGAME
  prova de uso real
```

## 3. O que foi entregue

### Package

O package agora possui:

- `PlayerComposer` como componente oficial de authoring da instancia de Player;
- `PlayerRecipe` como asset de defaults reutilizaveis;
- `PlayerComposerApplyRebuildUtility` como API editor-only oficial para Validate e Apply/Rebuild;
- Inspector com fluxo designer-first, Advanced/Debug e actions explicitas;
- Apply/Rebuild idempotente;
- diagnostics com breakdown: `created`, `repaired`, `alreadyValid`, `skippedByPolicy`, `blocked`;
- reset opcional por default;
- documentacao de uso rapido;
- documentacao de evidencia FIRSTGAME;
- plano de readiness QA futuro.

### FIRSTGAME

O consumidor real validou:

- configuracao de `PlayerPrototype` com `PlayerComposer` oficial;
- aplicacao de defaults via `PlayerRecipe`;
- Validate bem-sucedido;
- Apply/Rebuild bem-sucedido;
- segundo Apply/Rebuild idempotente;
- reset opcional por default;
- remocao da facade local antiga de player binding.

### Product direction

Este eixo prova a regra nova:

```text
Product Surface primeiro.
Diagnostics depois.
QA depois do contrato oficial.
```

## 4. Evidencias de validacao

### PlayerComposer no FIRSTGAME

```text
[FIRSTGAME][PlayerComposerPilot] Configured official PlayerComposer intent. player='PlayerPrototype' actorId='player.actor' playerSlotId='player.1' actionMap='Player' hasPlayerInput='True'. Use the PlayerComposer Inspector Apply/Rebuild button to materialize framework bindings.

[Immersive.Framework][PlayerComposer] Apply/Rebuild completed. player='PlayerPrototype' actorId='player.actor' playerSlotId='player.1' created='0' repaired='0' alreadyValid='13' skippedByPolicy='2' blocked='0' resetEnabled='False' resetParticipantPolicy='None'

[Immersive.Framework][PlayerComposer] Validation succeeded. player='PlayerPrototype' actorId='player.actor' playerSlotId='player.1'
```

### PlayerRecipe no FIRSTGAME

```text
[Immersive.Framework][PlayerComposer] Recipe defaults applied. player='PlayerPrototype' recipe='PlayerRecipe' actorId='player.actor' playerSlotId='player.1' actionMap='Player' resetEnabled='False' resetParticipantPolicy='None'.

[Immersive.Framework][PlayerComposer] Validation succeeded. player='PlayerPrototype' actorId='player.actor' playerSlotId='player.1'

[Immersive.Framework][PlayerComposer] Apply/Rebuild completed. player='PlayerPrototype' actorId='player.actor' playerSlotId='player.1' created='0' repaired='0' alreadyValid='13' skippedByPolicy='2' blocked='0' resetEnabled='False' resetParticipantPolicy='None'
```

## 5. O que foi removido do caminho principal

O FIRSTGAME removeu a antiga facade local de canonical player binding.

Removido do fluxo principal:

- `FirstGameCanonicalPlayerBindingAuthoringFacade`;
- `FirstGameCanonicalPlayerBindingFacadeRepairProof`;
- menus antigos de validate/apply/repair proof de player binding facade.

O fluxo principal agora e:

```text
PlayerRecipe opcional
+ PlayerComposer oficial
+ Apply Recipe Defaults quando aplicavel
+ Validate
+ Apply/Rebuild
```

## 6. O que fica fora deste fechamento

Este fechamento nao declara como pronto:

- spawn;
- multiplayer join;
- movement framework-owned;
- gameplay command execution;
- save/progression;
- PlayerRuntimeContext;
- PlayerSession;
- Camera Composer;
- Route/Activity Composer;
- QAFramework automated smoke.

Esses itens continuam futuros e devem seguir o mesmo modelo de Product Surface quando forem formalizados.

## 7. Criterios de aceite do eixo Player

O eixo Player e considerado fechado para MVP porque:

- usuario consegue configurar Player por uma superficie oficial;
- intencao reutilizavel existe por `PlayerRecipe`;
- `PlayerComposer` materializa bindings tecnicos;
- `_Framework/_Bindings` e materializacao tecnica, nao UX principal;
- Apply/Rebuild e idempotente;
- reset nao e obrigatorio por default;
- diagnostics explicam o resultado;
- FIRSTGAME validou uso real;
- facade local antiga foi removida;
- package nao depende de FIRSTGAME.

## 8. QAFramework

QAFramework ainda nao precisa bloquear este fechamento.

A razao e simples: a Product Surface ja foi validada manualmente no package e no consumidor real. QA tecnico agora deve entrar como regressao futura, nao como substituto de UX.

O plano de QA readiness ja existe e deve ser usado quando for hora de automatizar regressao do contrato oficial.

## 9. Proxima superficie candidata

A proxima candidata recomendada e:

```text
Camera Recipe / Camera Composer
```

Motivo:

- Player ja expoe camera targets e look-at targets;
- FIRSTGAME ainda depende de setup local para camera route/activity;
- camera cruza Player anchors, Route, Activity e transition presentation;
- Camera Composer e o proximo vazamento tecnico visivel depois do Player.

A proxima fase nao deve comecar por smoke. Deve comecar por Product Surface:

```text
Camera Recipe/Profile
+ Camera Composer
+ Apply/Rebuild
+ materializacao tecnica
+ diagnostics
+ FIRSTGAME proof
```

## 10. Commit message sugerida

```text
Docs: close Player product surface MVP
```
