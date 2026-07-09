# Player Composer Diagnostic Breakdown Manifest

Status: Implementation diagnostic patch
Date: 2026-07-09
Package: `com.immersive.framework`

## Corte

`P1C-B2` — PlayerComposer Apply/Rebuild result breakdown.

## Objetivo

Melhorar o diagnostico do `PlayerComposer` sem alterar o comportamento de materializacao.

O log anterior mostrava apenas:

```text
materialized='N'
```

Este corte passa a separar:

```text
created
repaired
alreadyValid
skippedByPolicy
blocked
resetEnabled
resetParticipantPolicy
```

## Arquivos criados

```text
Documentation~/Product/PLAYER-COMPOSER-DIAGNOSTIC-BREAKDOWN-MANIFEST.md
```

## Arquivos alterados

```text
Editor/PlayerAuthoring/PlayerComposerEditor.cs
```

## Arquivos removidos

```text
none
```

## Fora de escopo

```text
runtime behavior
PlayerComposer runtime fields
PlayerRecipe
PlayerRuntimeContext
FIRSTGAME
QAFramework
asmdefs
validators
smokes
reset policy changes
materialization topology changes
```

## Decisoes registradas

- `Apply/Rebuild` deve mostrar breakdown diagnostico, nao apenas um contador agregado.
- `created` indica objetos/componentes criados pelo Apply/Rebuild.
- `repaired` indica componentes existentes que tiveram serialized fields ajustados.
- `alreadyValid` indica materializacao existente sem mudanca necessaria.
- `skippedByPolicy` indica materializacao ignorada por policy ou tipo opcional ausente.
- `blocked` indica problema tecnico encontrado durante materializacao.
- Reset policy deve aparecer no log para confirmar que participants opcionais nao foram tratados como obrigatorios.

## Criterios de aceite

Este corte e PASS se:

- o package compilar;
- `Validate` continuar funcionando;
- `Apply/Rebuild` continuar funcionando;
- o log de Apply/Rebuild exibir `created`, `repaired`, `alreadyValid`, `skippedByPolicy`, `blocked`, `resetEnabled` e `resetParticipantPolicy`;
- rodar Apply/Rebuild duas vezes no mesmo Player mostrar aumento de `alreadyValid` e reduzir `created` quando nada novo precisar ser criado;
- nenhum asmdef for alterado;
- nenhum arquivo FIRSTGAME/QAFramework for alterado.

## Commit message sugerida

```text
Product: improve PlayerComposer apply diagnostics
```
