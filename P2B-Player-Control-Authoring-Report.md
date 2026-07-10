# P2B — Player Control Authoring Report

## Arquivos alterados

- `Runtime/PlayerAuthoring/PlayerRecipe.cs`
- `Runtime/PlayerAuthoring/PlayerComposer.cs`
- `Editor/PlayerAuthoring/PlayerComposerEditor.cs`
- `Editor/PlayerAuthoring/PlayerComposerApplyRebuildUtility.cs`
- `Documentation~/Current/01-Roadmap.md`
- `Documentation~/Current/02-Usage-Map.md`
- `Documentation~/Current/11-Player-Control-Authority-Audit.md`
- este relatório e seu `.meta`.

## Gaps corrigidos

- `UnityPlayerInputGateAdapter.sourceSlot` recebe a referência tipada do `PlayerSlotDeclaration` materializado/reutilizado.
- owners e targets externos ao Player root / `_Framework/_Bindings` bloqueiam Validate e Apply com diagnóstico determinístico; nada é escolhido ou removido automaticamente.
- controle required bloqueia `PlayerInput.actions == null`; action map e control target required ausentes também bloqueiam.

## Materialização final

Com Control habilitado e configuração completa, Apply/Rebuild materializa ou repara um único conjunto canônico:

```text
Player
  PlayerComposer
  PlayerSlotDeclaration
  PlayerActorDeclaration
  UnityPlayerInputGateAdapter (quando Gate Participation está habilitado)

  _Framework
    _Bindings
      PlayerControlBindingTargetBehaviour
      UnityPlayerInputBridgeTargetBehaviour
      UnityPlayerInputActivationTargetBehaviour
```

Control disabled ou optional incompleto não cria targets de controle. Componentes preexistentes não são apagados.

## Validações

- inspeção estática da API pública, campos serializados e boundaries;
- checagem textual de referências proibidas no Recipe;
- checagem de escopo Git para package, QAFramework e FIRSTGAME;
- Unity compile/import e P2A-QA0 não executados nesta sessão.

Resultado operacional: `PENDING UNITY COMPILE/IMPORT AND P2A-QA0 VALIDATION`.

## Pendências

- P2C: contratos e binding runtime transacional.
- P2D: lifecycle runtime do Unity PlayerInput.
- P2E: autoridade runtime escopada por Player.
- migração manual/explícita de duplicidades existentes; FIRSTGAME permanece para P2G.

