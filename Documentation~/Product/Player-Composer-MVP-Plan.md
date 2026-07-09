# Player Composer MVP Plan

Status: Technical MVP plan, no implementation
Last updated: 2026-07-09
Package: `com.immersive.framework`

## 1. Objetivo

Definir o plano tecnico minimo para implementar futuramente o `PlayerComposer` MVP como primeira vertical slice de produto.

O MVP deve transformar a superficie de Player de:

```text
componentes soltos + validators + smokes
```

para:

```text
PlayerComposer + Apply/Rebuild + materializacao tecnica + diagnostics
```

Este corte nao implementa codigo. Ele define o comportamento esperado, limites, componentes envolvidos, campos minimos, validacoes bloqueantes e criterios de aceite.

## 2. Escopo do MVP

O MVP deve cobrir um Player local scene-authored ou prefab-authored com:

- actor identity;
- player slot identity;
- `PlayerInput` tipado;
- gameplay action map esperado;
- input gate/bridge/activation bindings;
- control binding target;
- camera target/look-at target references;
- reset subject/transform participant quando `resetEnabled`;
- materializacao tecnica idempotente;
- diagnostics de Apply/Rebuild.

O MVP e authoring/apply/diagnostics. Ele nao e runtime de gameplay.

## 3. Fora de escopo

Fora deste MVP:

- codigo C# neste corte documental;
- `PlayerComposer` real neste corte;
- `PlayerRecipe` real neste corte;
- `PlayerRuntimeContext` real neste corte;
- novo validator;
- novo smoke;
- alteracao de runtime;
- alteracao de editor tooling;
- alteracao de asmdefs;
- FIRSTGAME;
- QAFramework;
- scripts FIRSTGAME;
- IDs, paths ou nomes `FG_*` / `firstgame.*`;
- movement framework-owned;
- gameplay command execution;
- spawn;
- multiplayer join;
- save/progression;
- `PlayerManager` global;
- Session generica;
- migracao automatica de componentes para child sem validar dependencias.

## 4. Componentes tecnicos atuais envolvidos

| Componente | Funcao | Necessario no MVP? | Root ou `_Framework/_Bindings`? | Criado por Apply/Rebuild? | Reparado por Apply/Rebuild? | Inspector Designer? | Advanced/Debug? | Risco de mover para child | Dependencias conhecidas | Decisao pendente |
|---|---|---|---|---|---|---|---|---|---|---|
| `PlayerActorDeclaration` | Declara ActorId de player e evidencia `PlayerInput`; implementa `IActor`. | Sim | Root no MVP | Sim, se ausente | Sim | Nao como componente direto | Sim | Alto | `[RequireComponent(typeof(PlayerInput))]`; `PlayerInput` no mesmo GameObject por fallback; usado por reset identity | Manter root no MVP; avaliar child apenas se contrato deixar de exigir same-object `PlayerInput` |
| `PlayerSlotDeclaration` | Declara `PlayerSlotId` e evidencia opcional de `PlayerInput`. | Sim | Root no MVP | Sim, se ausente | Sim | Nao como componente direto | Sim | Medio | Outros componentes fazem `GetComponent<PlayerSlotDeclaration>()` no root; sourceSlot do gate | Manter root no MVP para compatibilidade de discovery local |
| `PlayerSlotOccupancy` | Relacao passiva slot -> actor. | Sim | Preferencialmente `_Framework/_Bindings`, mas root tolerado no MVP | Sim, se policy incluir occupancy | Sim | Nao | Sim | Medio | `Reset()` usa same-object `PlayerSlotDeclaration`, `ActorDeclaration`, `PlayerActorDeclaration`; valida conflitos de actor source | Decidir se occupancy fica root no MVP para reduzir risco ou se Composer injeta refs tipadas no child |
| `ActorReadinessBehaviour` | Evidence/runtime-local readiness passivo para `PlayerEntry`. | Opcional no MVP | Root se usado por gameplay/entry; `_Framework/_Bindings` se gerado como evidence tecnico | Opcional | Sim | Nao | Sim | Medio | `PlayerEntryBehaviour` pode amostrar `ActorReadinessBehaviour`; possui estado no `Awake` | Decidir se MVP gera readiness ou apenas referencia existente |
| `PlayerEntryBehaviour` | Evidence passivo slot + actor + readiness; `RebuildEntry()` no `Awake`. | Opcional/P1 no MVP | `_Framework/_Bindings` se gerado; root tolerado | Opcional | Sim | Nao | Sim | Medio-Alto | `Reset()` usa same-object declarations/readiness; falha se IDs invalidos; `rebuildOnAwake` | MVP pode adiar Entry/View/Control passive chain se nao for exigida pelo FIRSTGAME slice |
| `PlayerViewBehaviour` | Evidence passivo de camera/target/view; nao ativa camera. | Opcional/P1 no MVP | `_Framework/_Bindings` se gerado; root tolerado | Opcional | Sim | Nao | Sim | Medio | Pode buscar `Camera` em children e `PlayerEntryBehaviour` no mesmo GameObject; valida slot contra entry | Decidir se MVP usa `FrameworkCameraAnchorHost` simples antes da cadeia F51 completa |
| `PlayerControlBehaviour` | Evidence passivo de control target/input source; nao controla gameplay. | Opcional/P1 no MVP | `_Framework/_Bindings` se gerado; root tolerado | Opcional | Sim | Nao | Sim | Medio | Pode buscar `PlayerEntryBehaviour`; `controlTarget` default = transform | Decidir se MVP limita a F52 binding targets sem PlayerControl passive chain |
| `UnityPlayerInputGateAdapter` | Bloqueia/restaura `PlayerInput` por Gate; usa action map e source slot. | Sim quando input binding requerido | Root no MVP | Sim, se ausente | Sim | Nao como componente direto | Sim | Alto | Resolve `PlayerInput` por field ou same-object fallback; observa runtime host no `Update`; `sourceSlot` tipado | Manter root no MVP para evitar quebrar fallback e lifecycle |
| `UnityPlayerInputBridgeTargetBehaviour` | Evidence de bridge PlayerControl -> `PlayerInput`; guarda slot esperado. | Sim | `_Framework/_Bindings` preferencial | Sim | Sim | Nao | Sim | Baixo-Medio | Exige `PlayerInput` explicito; `expectedPlayerSlotId` string pode driftar | Composer deve derivar slot do `PlayerSlotDeclaration` e reparar string duplicada |
| `UnityPlayerInputActivationTargetBehaviour` | Aplica action map no `PlayerInput` a partir de bridge evidence. | Sim quando activation binding requerido | `_Framework/_Bindings` preferencial | Sim | Sim | Nao | Sim | Baixo-Medio | Exige `PlayerInput`, `expectedPlayerSlotId`, `actionMapName` e action asset valido | Composer deve validar action map antes de Apply/Rebuild |
| `PlayerControlBindingTargetBehaviour` | Target de binding de control evidence. | Sim | `_Framework/_Bindings` preferencial | Sim | Sim | Nao | Sim | Baixo | Apenas nome diagnostico e binding snapshot | Pode ser materializado em child tecnico no MVP |
| `UnityResetSubjectAdapter` | Registra ResetSubject e participantes no runtime. | Sim quando `resetEnabled` | Root no MVP ou `_Framework/_Bindings` com validacao forte | Sim, se resetEnabled | Sim | Nao como componente direto | Sim | Alto | Lifecycle `OnEnable/Start/Update`; `sourcePlayerActor`; participant discovery SameGameObject/Children; owner runtime por scope | Manter root no MVP se participant discovery e resettable components do consumidor dependem do player subtree |
| `UnityTransformResetParticipant` | Participant que reseta transform alvo. | Sim quando `resetEnabled` e transform reset policy ativa | `_Framework/_Bindings` se `target` aponta para Player transform; root tolerado | Sim | Sim | Nao | Sim | Medio | `ResolveTarget()` usa `target` ou `transform`; `OnEnable` captura baseline | Se mover para child, `target` deve apontar explicitamente para Player root ou anchor correto |
| `FrameworkCameraAnchorHost` | Provider de tracking/lookAt target para camera bindings. | Sim quando camera binding requerido | `_Framework/_Bindings` ou `Anchors` conforme policy | Sim | Sim | Nao como componente direto | Sim | Baixo-Medio | Campos privados `trackingTarget`, `lookAtTarget`; sem setters publicos atuais; requer editor serialization para configurar | MVP precisa usar SerializedObject/editor-only future code ou API publica futura para configurar |

## 5. Modelo de hierarchy esperado

Alvo conceitual:

```text
Player
  PlayerComposer
  PlayerInput
  gameplay components owned by consumer

  Anchors
    CameraTarget
    LookAtTarget

  _Framework
    _Bindings
      technical framework components
```

Plano honesto para o MVP:

- `PlayerComposer` e `PlayerInput` ficam no root.
- Gameplay components do consumidor ficam no root ou children conforme o jogo decidir.
- `Anchors/CameraTarget` e `Anchors/LookAtTarget` podem ser criados quando a policy permitir.
- `_Framework/_Bindings` deve receber componentes puramente tecnicos quando a movimentacao for segura.
- Nem todo componente deve ser movido automaticamente para child. O MVP deve validar dependencias locais antes de materializar em child.

Componentes que tendem a ficar no root no MVP:

- `PlayerComposer`
- `PlayerInput`
- `PlayerActorDeclaration`
- `PlayerSlotDeclaration`
- `UnityPlayerInputGateAdapter`

Componentes candidatos a `_Framework/_Bindings` no MVP:

- `PlayerControlBindingTargetBehaviour`
- `UnityPlayerInputBridgeTargetBehaviour`
- `UnityPlayerInputActivationTargetBehaviour`
- `FrameworkCameraAnchorHost`
- `UnityTransformResetParticipant`, desde que `target` seja explicito
- `PlayerSlotOccupancy`, se referencias tipadas forem configuradas explicitamente

Componentes com decisao pendente:

- `UnityResetSubjectAdapter`
- `ActorReadinessBehaviour`
- `PlayerEntryBehaviour`
- `PlayerViewBehaviour`
- `PlayerControlBehaviour`

## 6. Campos do PlayerComposer MVP

### Designer

Campos minimos:

- `recipe` optional;
- `actorId`;
- `playerSlotId`;
- `playerInput`;
- `gameplayActionMap`;
- `cameraTarget`;
- `lookAtTarget`;
- `resetEnabled`;
- `validationMode`;

### Advanced

Campos tecnicos controlados:

- `frameworkBindingsRoot`;
- `createBindingsRootIfMissing`;
- `createAnchorsIfMissing`;
- `inputBindingRequired`;
- `cameraBindingRequired`;
- `resetScope`;
- `resetParticipantPolicy`;
- `materializeSlotOccupancy`;
- `materializePassiveEntryViewControl`;
- `logApplyRebuildDiagnostics`.

### Debug

Campos/evidencias somente leitura futuras:

- resolved actor id;
- resolved player slot id;
- resolved `PlayerInput`;
- action map found;
- bindings root status;
- materialized component list;
- conflicting component list;
- last Apply/Rebuild status;
- last blocking issue;
- generated references summary.

## 7. Apply/Rebuild MVP

Comportamento esperado:

1. Validar campos Designer obrigatorios.
2. Criar `_Framework/_Bindings` quando ausente e creation policy permitir.
3. Criar `Anchors/CameraTarget` e `Anchors/LookAtTarget` quando ausentes e policy permitir.
4. Criar ou reparar `PlayerActorDeclaration`.
5. Criar ou reparar `PlayerSlotDeclaration`.
6. Criar ou reparar `PlayerSlotOccupancy`, se a policy do MVP incluir occupancy.
7. Criar ou reparar `UnityPlayerInputGateAdapter`.
8. Criar ou reparar `PlayerControlBindingTargetBehaviour`.
9. Criar ou reparar `UnityPlayerInputBridgeTargetBehaviour`.
10. Criar ou reparar `UnityPlayerInputActivationTargetBehaviour`.
11. Criar ou reparar `FrameworkCameraAnchorHost`, se camera binding requerido.
12. Criar ou reparar `UnityResetSubjectAdapter` e `UnityTransformResetParticipant`, se `resetEnabled`.
13. Conectar camera target/look-at target a bindings compativeis.
14. Nao duplicar componentes.
15. Nao apagar componentes gameplay do consumidor.
16. Nao usar lookup por nome como identidade funcional.
17. Falhar com diagnostics claros quando `PlayerInput` ou action map obrigatorio estiver ausente.

Apply/Rebuild nao deve:

- executar gameplay;
- mover personagem;
- trocar action map em edit mode como prova de uso;
- spawnar actor;
- fazer join;
- salvar progresso;
- criar validator/smoke como pre-requisito.

## 8. Materializacao tecnica MVP

Materializacao tecnica esperada:

```text
Player/_Framework/_Bindings
  PlayerControlBindingTargetBehaviour
  UnityPlayerInputBridgeTargetBehaviour
  UnityPlayerInputActivationTargetBehaviour
  FrameworkCameraAnchorHost
  UnityTransformResetParticipant (optional, explicit target)
  PlayerSlotOccupancy (optional, explicit refs)
```

Materializacao root esperada:

```text
Player
  PlayerComposer
  PlayerInput
  PlayerActorDeclaration
  PlayerSlotDeclaration
  UnityPlayerInputGateAdapter
  UnityResetSubjectAdapter (initial MVP default, if resetEnabled)
```

Racional:

- Root preserva components que possuem same-object fallback ou `RequireComponent`.
- `_Framework/_Bindings` recebe target/evidence components que sao claramente tecnicos.
- Reset subject fica root no primeiro MVP por risco de participant discovery e lifecycle; pode migrar depois se o Composer configurar discovery/targets explicitamente.

## 9. Inspector Designer / Advanced / Debug

### Designer

Deve mostrar intencao:

- Player identity;
- Player slot;
- `PlayerInput`;
- gameplay action map;
- camera target;
- look-at target;
- reset enabled;
- recipe optional;
- button/action de Apply/Rebuild.

Nao deve mostrar como primeira camada:

- bridge target;
- activation target;
- gate adapter internals;
- reset participant internals;
- generated binding names;
- diagnostics-only reasons.

### Advanced

Deve mostrar configuracao tecnica controlada:

- binding root policy;
- anchor creation policy;
- input binding policy;
- camera binding policy;
- reset scope/policy;
- passive topology materialization toggles;
- validation mode.

### Debug

Deve mostrar evidencias:

- components criados/reparados;
- references resolvidas;
- action map validation;
- blocking issues;
- last Apply/Rebuild result;
- drift detected;
- diagnostics source.

## 10. Regras de validacao bloqueante

Apply/Rebuild deve bloquear quando:

- `PlayerInput` estiver ausente e input binding for requerido;
- `PlayerInput.actions` estiver ausente e action map for requerido;
- `gameplayActionMap` nao existir no `InputActionAsset`;
- `actorId` estiver vazio ou invalido;
- `playerSlotId` estiver vazio ou invalido;
- houver multiplos bindings conflitantes gerados pelo Composer;
- `frameworkBindingsRoot` estiver ausente e creation policy estiver desativada;
- camera target estiver ausente quando camera binding for requerido;
- look-at target estiver ausente quando a policy exigir alvo distinto;
- reset policy estiver ativa sem subject/participant materializavel;
- `PlayerActorDeclaration` e outro actor source conflitarem;
- `PlayerSlotOccupancy` resolver actor/slot diferentes dos campos do Composer;
- componentes existentes estiverem em local nao suportado e a policy nao permitir reparo seguro.

## 11. Regras de idempotencia

Apply/Rebuild deve:

- reutilizar componentes gerados anteriormente;
- encontrar materializacao por referencias registradas ou ownership do Composer, nao por nome;
- nao duplicar `_Framework`, `_Bindings`, `Anchors`, `CameraTarget` ou `LookAtTarget`;
- nao duplicar declarations, bridge targets, activation targets, reset adapters ou participants;
- reparar campos divergentes derivados do Composer;
- preservar campos de gameplay do consumidor;
- reportar drift em Debug/Diagnostics;
- produzir o mesmo resultado em execucoes repetidas.

## 12. Regras de seguranca

O MVP deve:

- falhar rapido em config obrigatoria ausente;
- nao usar fallback silencioso;
- nao usar service locator;
- nao criar singleton;
- nao comparar identidades de dominios diferentes;
- nao usar `GameObject.name` ou path como identidade funcional;
- nao apagar componentes do consumidor;
- nao mover componentes existentes sem policy explicita;
- nao alterar scenes/prefabs fora do Player selecionado ou alvo explicito;
- nao alterar FIRSTGAME ou QAFramework automaticamente;
- nao depender de validators/smokes para criar a feature.

## 13. Relacao com PlayerRecipe

`PlayerRecipe` e recomendado, mas nao obrigatorio no primeiro MVP.

No MVP:

- `recipe` pode ser vazio;
- campos locais do Composer devem ser suficientes para Apply/Rebuild;
- se `recipe` existir, o Composer deve usar Recipe como default de intencao;
- overrides locais devem ser explicitos;
- templates oficiais devem caminhar para Recipe.

Responsabilidades futuras do Recipe:

- actor identity padrao;
- player slot padrao;
- gameplay action map esperado;
- reset policy;
- camera target policy;
- required bindings;
- authoring validation policy.

## 14. Relacao com PlayerRuntimeContext

`PlayerRuntimeContext` e futuro e nao faz parte deste MVP.

O MVP pode preparar materializacao e diagnostics para um futuro context, mas nao deve criar runtime authority nova.

Quando existir, `PlayerRuntimeContext` deve ser:

- tipado;
- escopado;
- sem singleton;
- sem service locator;
- sem lookup global implicito;
- sem fallback silencioso.

Responsabilidades candidatas futuras:

- current actor identity;
- current player slot;
- current `PlayerInput`;
- current input activation state;
- current camera targets;
- current reset subject registration.

## 15. Relacao com FIRSTGAME

FIRSTGAME deve provar a futura implementacao como consumidor real.

Prova esperada depois da implementacao:

- `PlayerPrototype` usa `PlayerComposer` oficial;
- menus/facades FIRSTGAME deixam de ser UX principal;
- Apply/Rebuild oficial substitui repair local;
- `PlayerInput`, camera target, reset e identity continuam funcionando;
- componentes tecnicos ficam fora da superficie principal do designer;
- validators confirmam, mas nao criam a feature.

Nao migrar para o package:

- scripts FIRSTGAME;
- facades FIRSTGAME;
- paths `Assets/_Project/...`;
- IDs `firstgame.*`;
- nomes `FG_*`;
- estrutura exata de cena.

## 16. Relacao com QAFramework

QAFramework deve validar depois que a implementacao oficial existir.

Casos futuros:

- Apply cria bindings;
- Rebuild e idempotente;
- Apply falha sem `PlayerInput` requerido;
- Apply falha com action map invalido;
- Apply nao usa name lookup;
- Apply nao duplica componentes;
- Apply mantem `actorId` / `playerSlotId` tipados;
- drift repair funciona;
- resetEnabled materializa reset subject/participant;
- camera binding requerido falha sem target;
- components em locais nao suportados geram diagnostics claros.

Nao criar QA neste corte.

## 17. Criterios de aceite tecnico

Para a implementacao futura:

- compila;
- nao altera asmdefs sem necessidade documentada;
- nao altera packages tecnicos congelados;
- Apply/Rebuild e idempotente;
- campos obrigatorios bloqueiam corretamente;
- diagnostics sao claros;
- nao ha fallback silencioso;
- nao ha name/path lookup funcional;
- referencias tipadas sao preservadas;
- componentes gameplay do consumidor nao sao removidos;
- root vs `_Framework/_Bindings` segue policy documentada.

## 18. Criterios de aceite de produto

Para a implementacao futura:

- usuario cria/configura Player por `PlayerComposer`;
- Inspector Designer e compreensivel;
- Advanced/Debug contem detalhes tecnicos;
- `_Framework/_Bindings` nao e UX principal;
- diagnostics confirmam Apply/Rebuild, mas nao substituem criacao;
- FIRSTGAME prova uso real sem facade local como caminho principal;
- QAFramework valida contrato oficial depois.

## 19. Riscos e decisoes pendentes

Riscos:

- `PlayerActorDeclaration` exige `PlayerInput` no mesmo GameObject; mover para child pode quebrar contrato atual.
- `UnityPlayerInputGateAdapter` possui same-object fallback de `PlayerInput` e source slot.
- `UnityResetSubjectAdapter` registra em runtime e descobre participants em children; mover para child pode alterar registro/reset.
- `UnityTransformResetParticipant` usa `transform` como fallback; em child, precisa de `target` explicito.
- `FrameworkCameraAnchorHost` nao possui setters publicos; configuracao futura pode precisar de API authoring ou editor serialization.
- Passive Entry/View/Control chain pode ampliar o MVP alem do necessario para FIRSTGAME.

Decisoes pendentes:

- Gerar `ActorReadinessBehaviour`, `PlayerEntryBehaviour`, `PlayerViewBehaviour` e `PlayerControlBehaviour` no MVP ou adiar para P1?
- `UnityResetSubjectAdapter` fica root no MVP ou pode ir para `_Framework/_Bindings` com target/discovery explicitos?
- `PlayerSlotOccupancy` deve ser sempre materializado no MVP ou apenas quando diagnostics/topology exigirem?
- Camera MVP deve usar `FrameworkCameraAnchorHost` simples ou integrar F51 PlayerView camera chain?
- Necessidade de API publica de configuracao para `FrameworkCameraAnchorHost` e target behaviours, evitando reflection/string serialized property access.

## 20. Commit message sugerida

```text
Docs: plan player composer MVP
```

