# Product Surface Manifest

Status: Documentation cut
Date: 2026-07-09
Package: `com.immersive.framework`

## Corte

Definicao documental inicial da nova fase de produto do Immersive Framework 1.0.

Primeira vertical slice preparada:

```text
Player Recipe / Player Composer
```

## Objetivo

Criar documentacao de produto para orientar implementacao futura, sem alterar runtime, editor tooling, asmdefs, FIRSTGAME ou QAFramework.

## Arquivos criados

```text
Documentation~/Product/Product-Surface-Index.md
Documentation~/Product/Player-Product-Surface-Spec.md
Documentation~/Product/PRODUCT-SURFACE-MANIFEST.md
```

## Arquivos alterados

```text
none
```

## Arquivos removidos

```text
none
```

## Fora de escopo

```text
codigo C#
runtime
editor tooling
asmdefs
PlayerComposer implementation
PlayerRecipe implementation
PlayerRuntimeContext implementation
FIRSTGAME
QAFramework
smokes
validators
scripts FIRSTGAME
paths, IDs ou nomes FG_* / firstgame.*
```

## Criterios de aceite

Este corte e PASS se:

- os 3 arquivos forem criados;
- nenhum codigo C# for alterado;
- nenhum asmdef for alterado;
- nenhum arquivo FIRSTGAME/QAFramework for alterado;
- os documentos nao tratarem validators/smokes como UX principal;
- `Player Recipe / Player Composer` estiverem definidos como direcao, nao implementacao;
- o texto deixar claro que `_Framework/_Bindings` e materializacao tecnica;
- o texto deixar claro que `PlayerComposer` nao executa gameplay e nao e `PlayerManager`;
- Unity, build, playmode, smoke e batchmode nao forem executados.

## Commit message sugerida

```text
Docs: define player product surface
```

