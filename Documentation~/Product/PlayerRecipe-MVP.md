# PlayerRecipe MVP

Status: Product surface MVP
Package: `com.immersive.framework`

## Objetivo

`PlayerRecipe` e o primeiro asset reutilizavel de intencao para `PlayerComposer`.

Ele existe para evitar repeticao manual de defaults em players authored por cena ou prefab. Ele nao cria objetos, nao materializa bindings, nao executa gameplay e nao substitui o `PlayerComposer`.

## Papel no Product Surface

```text
PlayerRecipe
  intencao reutilizavel

PlayerComposer
  instancia concreta no Player

Apply/Rebuild
  materializacao tecnica oficial
```

`PlayerRecipe` define defaults. `PlayerComposer` continua sendo a superficie principal de authoring da instancia.

## Campos cobertos

O MVP cobre defaults reutilizaveis para:

- `actorId`;
- `playerSlotId`;
- `gameplayActionMap`;
- `resetEnabled`;
- `validationMode`;
- `createBindingsRootIfMissing`;
- `createAnchorsIfMissing`;
- `inputBindingRequired`;
- `cameraBindingRequired`;
- `resetScope`;
- `resetParticipantPolicy`;
- `materializeSlotOccupancy`;
- `materializePassiveEntryViewControl`;
- `logApplyRebuildDiagnostics`.

O MVP nao guarda referencias de cena como `PlayerInput`, `cameraTarget` ou `lookAtTarget`, porque essas referencias pertencem a instancia concreta do `PlayerComposer`.

## Fluxo de uso

1. Criar um asset pelo menu:

```text
Create > Immersive Framework > Player > Player Recipe
```

2. Configurar os defaults reutilizaveis.
3. Selecionar um GameObject com `PlayerComposer`.
4. Atribuir o `PlayerRecipe` no campo `Recipe`.
5. Clicar `Apply Recipe Defaults`.
6. Revisar campos locais do Composer.
7. Rodar `Validate`.
8. Rodar `Apply / Rebuild`.

## Regras

- Recipe e opcional.
- Composer continua funcionando sem Recipe.
- `Apply Recipe Defaults` copia defaults para campos locais do Composer.
- Overrides locais continuam visiveis e editaveis.
- Recipe nao materializa bindings diretamente.
- Recipe nao executa gameplay.
- Recipe nao e `PlayerManager`.
- Reset continua desativado por default.
- `resetParticipantPolicy = None` continua sendo o default seguro.

## Fora de escopo

- `PlayerRuntimeContext`;
- spawn;
- multiplayer join;
- movement framework-owned;
- save/progression;
- QA smoke;
- FIRSTGAME changes;
- migration de prefab/cena.

## Criterio de aceite

Este MVP esta correto se:

- o asset `PlayerRecipe` puder ser criado pelo Create menu;
- `PlayerComposer` aceitar uma Recipe tipada;
- `Apply Recipe Defaults` copiar defaults para o Composer;
- `Validate` continuar funcionando;
- `Apply/Rebuild` continuar idempotente;
- Recipe nao alterar runtime behavior;
- Recipe nao reintroduzir reset participant obrigatorio.
