# Player Composer MVP Manifest

Status: Documentation cut
Date: 2026-07-09
Package: `com.immersive.framework`

## Corte

Plano tecnico minimo para futura implementacao do `PlayerComposer` MVP.

## Objetivo

Transformar a spec de produto `Player Recipe / Player Composer` em uma decisao tecnica inicial, sem implementar codigo.

## Arquivos criados

```text
Documentation~/Product/Player-Composer-MVP-Plan.md
Documentation~/Product/PLAYER-COMPOSER-MVP-MANIFEST.md
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
PlayerComposer
PlayerRecipe
PlayerRuntimeContext
validator
smoke
FIRSTGAME
QAFramework
scripts FIRSTGAME
IDs, paths ou nomes FG_* / firstgame.*
Unity build
Play Mode
batchmode
```

## Criterios de aceite

Este corte e PASS se:

- os 2 arquivos forem criados;
- nenhum codigo C# for alterado;
- nenhum asmdef for alterado;
- nenhum arquivo FIRSTGAME/QAFramework for alterado;
- o plano classificar componentes tecnicos atuais;
- o plano definir campos minimos do `PlayerComposer`;
- o plano definir Apply/Rebuild MVP;
- o plano separar Designer / Advanced / Debug;
- o plano deixar claro que `PlayerComposer` nao executa gameplay;
- o plano deixar claro que `PlayerComposer` nao e `PlayerManager`;
- o plano deixar claro que `PlayerRecipe` e recomendado, mas nao obrigatorio no MVP;
- o plano deixar claro que `PlayerRuntimeContext` e futuro e nao parte deste MVP;
- Unity, build, playmode, smoke e batchmode nao forem executados.

## Decisoes registradas

- `PlayerComposer` e authoring/apply/diagnostics.
- `PlayerComposer` nao executa gameplay.
- `PlayerComposer` nao e `PlayerManager`.
- `PlayerRecipe` e recomendado, mas nao obrigatorio no primeiro MVP.
- `_Framework/_Bindings` e materializacao tecnica, nao autoridade principal.
- Diagnostics confirmam Apply/Rebuild, mas nao sao fluxo principal.
- Componentes com same-object fallback ou `RequireComponent` nao devem ser movidos automaticamente para child no MVP.

## Decisoes pendentes

- Se `ActorReadinessBehaviour`, `PlayerEntryBehaviour`, `PlayerViewBehaviour` e `PlayerControlBehaviour` entram no MVP ou ficam para P1.
- Se `UnityResetSubjectAdapter` pode migrar para `_Framework/_Bindings` depois de validar participant discovery e lifecycle.
- Se `PlayerSlotOccupancy` sera sempre materializado ou policy-driven.
- Se camera MVP usa apenas `FrameworkCameraAnchorHost` ou integra a cadeia F51 completa.
- Se serao criadas APIs publicas de configuracao para evitar acesso editor-only por serialized property names.

## Commit message sugerida

```text
Docs: plan player composer MVP
```

