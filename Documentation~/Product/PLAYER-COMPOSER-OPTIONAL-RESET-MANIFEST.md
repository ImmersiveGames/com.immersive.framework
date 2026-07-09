# Player Composer Optional Reset Manifest

Status: Small product alignment patch
Date: 2026-07-09
Package: `com.immersive.framework`

## Corte

Ajuste pequeno do `PlayerComposer` MVP para impedir que reset transform participant seja materializado por padrao.

## Objetivo

Tornar reset materialization opcional por default. `UnityTransformResetParticipant` deve ser uma policy explicita de teste/uso, nao uma obrigatoriedade do Player Product Surface.

## Arquivos alterados

```text
Runtime/PlayerAuthoring/PlayerComposer.cs
```

## Arquivos criados

```text
Documentation~/Product/PLAYER-COMPOSER-OPTIONAL-RESET-MANIFEST.md
```

## Arquivos removidos

```text
none
```

## Decisoes registradas

- `resetEnabled` passa a iniciar desativado.
- `resetParticipantPolicy` passa a iniciar como `None`.
- `UnityTransformResetParticipant` continua suportado, mas apenas quando reset materialization estiver habilitado e a policy for `Transform`.
- Reset participant e uma materializacao tecnica opcional, nao parte obrigatoria de todo Player.
- O Composer continua nao destrutivo: este patch nao remove automaticamente participants ja criados em instancias existentes.

## Fora de escopo

```text
PlayerRecipe
PlayerRuntimeContext
Reset Group oficial
cleanup/destructive rebuild
FIRSTGAME
QAFramework
new smoke
new validator
asmdef changes
```

## Criterios de aceite

Este corte e PASS se:

- novo `PlayerComposer` nao criar `UnityTransformResetParticipant` por padrao;
- usuario puder habilitar reset explicitamente e escolher policy `Transform` quando quiser o participant;
- `PlayerComposer` continuar compilando;
- nenhum asmdef for alterado;
- nenhum arquivo FIRSTGAME/QAFramework for alterado;
- Apply/Rebuild continuar idempotente e nao destrutivo.

## Commit message sugerida

```text
Product: make player reset participant optional by default
```
