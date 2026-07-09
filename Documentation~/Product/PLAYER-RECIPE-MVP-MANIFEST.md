# PlayerRecipe MVP Manifest

Status: Product surface cut
Package: `com.immersive.framework`

## Corte

Cria o primeiro `PlayerRecipe` MVP como asset reutilizavel de intencao para `PlayerComposer`.

## Objetivo

Reduzir repeticao de configuracao no `PlayerComposer` sem introduzir runtime authority, gameplay ownership, QA ou dependencia de FIRSTGAME.

## Arquivos criados

```text
Runtime/PlayerAuthoring/PlayerRecipe.cs
Runtime/PlayerAuthoring/PlayerRecipe.cs.meta
Documentation~/Product/PlayerRecipe-MVP.md
Documentation~/Product/PlayerRecipe-MVP.md.meta
Documentation~/Product/PLAYER-RECIPE-MVP-MANIFEST.md
Documentation~/Product/PLAYER-RECIPE-MVP-MANIFEST.md.meta
```

## Arquivos alterados

```text
Runtime/PlayerAuthoring/PlayerComposer.cs
Editor/PlayerAuthoring/PlayerComposerEditor.cs
```

## Arquivos removidos

```text
none
```

## Decisoes registradas

- `PlayerRecipe` e intencao reutilizavel.
- `PlayerComposer` continua sendo a superficie principal da instancia.
- Recipe nao materializa bindings diretamente.
- Recipe nao executa gameplay.
- Recipe nao e `PlayerManager`.
- Recipe e opcional.
- `Apply Recipe Defaults` copia defaults para campos locais do Composer.
- Reset continua desativado por default.

## Fora de escopo

```text
PlayerRuntimeContext
spawn
multiplayer join
movement framework-owned
save/progression
QAFramework
FIRSTGAME
smoke
validator
asmdef changes
runtime behavior changes
```

## Criterios de aceite

Este corte e PASS se:

- Unity compilar;
- `Create > Immersive Framework > Player > Player Recipe` criar o asset;
- `PlayerComposer` aceitar `PlayerRecipe` tipado;
- `Apply Recipe Defaults` copiar defaults;
- `Validate` continuar passando com configuracao valida;
- `Apply/Rebuild` continuar idempotente;
- nenhum asmdef for alterado;
- FIRSTGAME e QAFramework nao forem alterados.

## Commit message sugerida

```text
Product: add PlayerRecipe MVP
```
