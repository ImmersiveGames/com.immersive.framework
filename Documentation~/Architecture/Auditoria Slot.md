Auditoria concluída em modo somente leitura. Nenhum arquivo foi alterado e nenhum teste/Unity foi executado.

Baseline confirmado nas branches oficiais:

- Framework: [`b250a85`](https://github.com/ImmersiveGames/com.immersive.framework/commit/b250a85abc47fed22f4ed496cd1d6159999d4b28)
- QA: [`f96e36b`](https://github.com/rinnocenti/QAFramework/commit/f96e36b25fea98bf0a64c1a712cbe959d252817c)
- FIRSTGAME: [`fff53de`](https://github.com/ImmersiveGames/planet-devourer/commit/fff53de7318db541057f843138e95b6115389360)

Legenda: **Fato** = confirmado no código; **Inferência** = consequência arquitetural; **Hipótese** = requer prova runtime adicional.

## 1. Resumo executivo

**Fato:** não existe hoje uma autoridade canônica única para responder:

> Qual Player lógico, Host e Actor ocupam atualmente um `PlayerSlotId`, sob qual owner, scope e lifetime?

A verdade está dividida assim:

- `PlayerParticipationRuntimeContext`: Slot, join e `ActorProfile` selecionado.
- `PlayerActorPreparationRuntimeHostModule.joinedHosts`: Slot → Host.
- `PlayerActorPreparationRuntimeContext.records`: Slot → preparação → handle físico contendo Host e Actor.
- `SceneLocalPlayerAdmissionRuntime`: associação Slot–Host exclusiva do fluxo Scene-Provided.
- `PlayerGameplayOccupancyRuntimeContext`: Slot → Actor preparado/owner, mas sem Host.
- Input, Camera e Gameplay Admission: cópias tokenizadas da identidade do Actor, próprias das capacidades.

A única estrutura que conhece simultaneamente Slot, Host e Actor é o `PreparationRecord` interno, por meio de `PlayerActorMaterializationHandle`. Ela só existe quando o Actor está preparado, não oferece um snapshot agregado de assignment e tem lifetime vinculado à materialização — normalmente Activity.

**Conclusão:** nenhum conceito existente é suficiente, isoladamente, como `Current Player Slot Assignment`.

O problema da Camera depender de Input está confirmado: `PlayerGameplayCameraEligibilityToken` incorpora `PlayerGameplayInputBindingToken`, e `TryConfirmEligibility` rejeita Camera sem Input corrente. Isso não é necessário para identificar Slot, Host ou Actor; é uma dependência da cadeia atual de gameplay.

## 2. Modelo atual real

### Manager-Provisioned

```text
GameApplicationAsset.LocalPlayerSlots
  → PlayerParticipationRuntimeContext
      SlotRecord
      PlayerSlotId + allocation + selectionRevision
  → PlayerSlotReservationToken
  → LocalPlayerProvisioningBridge.TryJoin
      PlayerInput + LocalPlayerHostAuthoring
  → PlayerActorPreparationRuntimeHostModule.joinedHosts
      PlayerSlotId → Host (Unity reference)
  → PlayerActorPreparationRuntimeContext.TryPrepareSelectedActor
      PreparationRecord
      → PlayerActorMaterializationHandle
          Slot + Host + PlayerInput + PlayerActorDeclaration + GameObject
  → PlayerActorPreparationToken
  → PlayerGameplayOccupancyToken
  → PlayerGameplayInputBindingToken
  → PlayerGameplayCameraEligibilityToken
  → PlayerGameplayAdmissionToken
```

Evidência: `PlayerParticipationRuntimeContext.SlotRecord`, `LocalPlayerProvisioningBridge.TryJoin`, `PlayerActorPreparationRuntimeHostModule.TryRegisterJoinedHost`, `PlayerActorPreparationRuntimeContext.TryPrepareSelectedActor`.

### Scene-Provided

```text
SceneLocalPlayerAdmissionAuthoring
  → SceneLocalPlayerAdmissionRuntime.TryAdmit
      PlayerSlotId + Host + Scene admission token
  → PlayerParticipationRuntimeContext
      Slot = Joined
  → PlayerActorSelection
  → PlayerActorPreparationRuntimeHostModule.TryAdoptSceneLocalPlayerActor
      registra o mesmo Host em joinedHosts
  → PlayerActorPreparationRuntimeContext.TryAdoptScenePlayerActor
      sobrescreve temporariamente ActorId
      cria PreparationRecord no mesmo records do fluxo provisionado
  → ScenePlayerActorAdoptionToken
      inclui PlayerActorPreparationToken
  → [se GameplayReady for exigido]
      Occupancy → Input → Camera → Gameplay Admission
```

O ponto real de convergência é `PlayerActorPreparationRuntimeContext.records`. Antes dele, Manager-Provisioned e Scene-Provided possuem contratos de Host distintos.

### Session-Persistent

```text
Arquitetura aceita em ADR
  → sem authoring concreto
  → sem admission/runtime authority
  → sem projeção implementada
  → sem token ou release contract
```

**Fato:** o Host Manager-Provisioned sobrevive a Activity, mas isso não implementa o conceito documentado de `Session-Persistent Logical Player source`.

## 3. Identidades

| Identidade | Criação/manutenção | Lifetime | Estabilidade e uso |
|---|---|---|---|
| `PlayerSlotId` | `PlayerSlotProfile.GetRequiredPlayerSlotId`; mantida por `PlayerParticipationRuntimeContext` | Session | Estável e tipada. Participa de todos os tokens. |
| `PlayerSlotProfile` | Asset do produto | Asset/application | Referência Unity estável como configuração; não é estado runtime. |
| Player lógico | Não existe tipo próprio | — | O modelo usa o Slot como seat lógico, mas não há `PlayerId`/`LocalPlayerId`. |
| Local Player identity | Não existe tipo de domínio | Session física | Host é identificado por referência `LocalPlayerHostAuthoring`; `UnityPlayerIndex` é diagnóstico. |
| Host identity | Não existe `HostId` | Host GameObject | Referência Unity efêmera; não participa dos tokens principais. |
| `ActorProfileId` | `ActorProfile` | Asset/application | Estável, selecionável e presente em Occupancy/Input/Camera/Admission. |
| `ActorId` | Gerado na materialização ou adoção | Materialização/adoption | Estável apenas durante aquela materialização. Muda entre Activity/adoption. |
| Runtime content | `RuntimeContentIdentity(owner, RuntimeContentId)` | Owner scope | Tipada; participa de Preparation e cadeia gameplay. |
| Scene object | Unity reference/instance | Scene | Sem identidade de domínio própria. |
| `RouteId` | `RouteAsset` | Asset/Route | Estável; participa dos tokens de lifecycle/handoff. |
| `ActivityId` | `ActivityAsset` | Asset/Activity | Estável; usado como `RuntimeContentOwner.Activity`. |
| Session context | `Guid.NewGuid().ToString("N")` em `PlayerParticipationRuntimeContext` | Instância de Session | Efêmera por boot; participa dos tokens Player. |
| Session owner | `RuntimeContentOwner.Session(application.ApplicationName, ...)` | Session scope | Outra representação da Session, diferente do GUID do participation context. |

Evidências principais:

- `Runtime/PlayerSlots/PlayerSlotId.cs`, `PlayerSlotId`.
- `Runtime/PlayerParticipation/Runtime/PlayerParticipationRuntimeContext.cs:58-69`, construtor.
- `Runtime/Actors/ActorProfile.cs`, `ActorProfile.TryGetActorProfileId`.
- `Runtime/PlayerParticipation/Runtime/AttachedPlayerActorMaterializationAdapter.cs`, `TryCreateRuntimeIdentities`.
- `PlayerActorPreparationRuntimeContext.ScenePlayerActorAdoption.cs:263-273`, geração do `ActorId` Scene-Provided.
- `FrameworkRuntimeHost.cs:1603-1610`, `CreateSessionScopeRoot`.

**Achado:** não há fallback funcional por nome, tag ou `Camera.main` nessa cadeia. Nomes como `PlayerInputName` e `CameraRigName` são diagnósticos; a coerência é feita por tokens, IDs e `ReferenceEquals`.

## 4. Autoridades e lifetimes

| Autoridade | Verdade afirmada | Não afirma | Scope/lifetime | Token/release |
|---|---|---|---|---|
| `PlayerParticipationRuntimeContext` | Roster, capacity, join, Slot state e seleção | Host, Actor materializado, owner | Session | Reservation token; slot/selection revisions; release explícito |
| `LocalPlayerHostAuthoring` | Este Host está joined a um Slot | ActorId, owner, assignment revision | GameObject/Session ou Scene | Sem token próprio; estado interno |
| `PlayerActorPreparationRuntimeHostModule` | Registry Slot → Host e facade de preparation | Snapshot atômico Slot+Host+Actor | Runtime host/Session | Sem token de Host |
| `SceneLocalPlayerAdmissionRuntime` | Associação Scene authoring + Host + Slot joined | Actor preparado | Activity/scene admission | `SceneLocalPlayerAdmissionToken`; release transacional |
| `PlayerActorPreparationRuntimeContext` | Actor selecionado foi materializado/adotado para o Slot | Gameplay/input/camera | Materialization owner, usualmente Activity | `PlayerActorPreparationToken`; release |
| `ScenePlayerActorAdoption` | Actor externo foi adotado sem transferência física | Assignment independente de Activity | Activity owner | Adoption token; restaura declaração original |
| `PlayerGameplayOccupancyRuntimeContext` | Slot está efetivamente ocupado pelo Actor preparado | Host, Input, Camera | Contexto Session; registro com owner da materialização | Occupancy token; release |
| `PlayerGameplayInputBindingRuntimeContext` | Actor/occupancy estão ligados ao `PlayerInput` e Gate | Camera, assignment principal | Lifetime da cadeia gameplay | Input token; restaura Gate/action map |
| `PlayerGameplayCameraEligibilityRuntimeContext` | Actor/input corrente têm camera authoring elegível | Publicação final | Lifetime da cadeia gameplay | Camera token; depende de Input |
| `PlayerGameplayAdmissionRuntimeContext` | Cadeia está admitida; publica Camera; calcula `GameplayReady` | Fonte independente de assignment | Activity/gameplay admission | Admission token; release em ordem |
| `ActivityPlayerActorLifecycleParticipant` | Projeção da Session na Activity e preparação/release | Player permanente | Activity | Snapshot de lifecycle |
| Lifecycle/handoff contexts | Transição reversível entre owners/Activities | Assignment Session neutro | Route/Activity transition | Stage, group e lifecycle tokens |

### Respostas diretas: Slot, Host e Actor

- **O Slot conhece o Host atual?** Não. `PlayerSlotRuntimeSnapshot` não contém Host.
- **O Slot conhece o Actor atual?** Conhece somente o `ActorProfile` selecionado, não `ActorId` ou materialização.
- **O Host conhece o Slot?** Sim, por `JoinedPlayerSlotId` e `JoinedConfiguredIndex`.
- **O Actor conhece o Slot?** Não por propriedade tipada. A relação está no materialization/adoption record; no Scene-Provided ela aparece também como texto diagnóstico em `Reason`.
- **Existe autoridade com Slot, Host e Actor?** Apenas internamente durante `Prepared`: `PreparationRecord.Handle`.
- **Occupancy conhece Host?** Não; conhece Slot, Actor, owner, runtime content e preparation token.
- **Preparation conhece Host e Actor simultaneamente?** Sim internamente; `PlayerActorPreparationSummary` omite Host.
- **Scene admission cria associação oficial?** Sim, uma associação Slot–Host transacional e origin-specific. Não cria Slot–Host–Actor.
- **Scene adoption usa a mesma preparation authority do Manager-Provisioned?** Sim. Escreve em `PlayerActorPreparationRuntimeContext.records`, além do registry específico `sceneAdoptions`.
- **Trocar Actor mantendo Host:** altera selection revision, preparation/materialization token e toda a cadeia downstream.
- **Trocar Host mantendo Slot:** não há operação canônica de swap. O registry rejeita outro Host; no Scene-Provided é necessário liberar e readmitir.
- **Trocar apenas Input:** release/rebind renova Input, Camera e Admission; não altera Preparation/Occupancy.
- **Trocar apenas Camera:** renova Camera Eligibility e Admission; não altera Preparation/Occupancy/Input.
- **Isso altera indevidamente a associação principal?** Não existe token da associação principal para permanecer estável ou provar que permaneceu igual.

## 5. Fluxos por origem

| Etapa | Manager-Provisioned | Scene-Provided | Session-Persistent |
|---|---|---|---|
| Entrada | `LocalPlayerProvisioningBridge.TryJoin` | `SceneLocalPlayerAdmissionRuntime.TryAdmit` | Não implementada |
| Host | Instanciado e transferido ao `FrameworkRuntimeHost` | GameObject da cena | Contrato ausente |
| Associação Slot–Host | `joinedHosts` + Host state + join result | Admission record + Host state + `joinedHosts` | Ausente |
| Seleção | Session-persistent em `SlotRecord` | Aplicada no Activity enter | Sem fluxo |
| Preparação | Instancia prefab sob `ActorMount` | Adota Actor físico da cena | Sem fluxo |
| Ownership físico | Framework/runtime content | `ExternalSceneOwned` | Sem contrato |
| Convergência | `PreparationRecord` | Mesmo `PreparationRecord` | Nenhuma |
| Occupancy/Input/Camera | Cadeia P3K | Só se chegar a `GameplayReady` | Nenhuma |
| Activity exit | Gameplay e Actor liberados; Slot, Host e seleção sobrevivem | Gameplay, adoption, seleção e admission liberados; objetos da cena sobrevivem | Não aplicável |
| Route handoff | Candidate/preparation/gameplay chain promovidos | Route scene unload + nova admission/adoption | Não aplicável |

**Fato:** no Manager-Provisioned, o Host sobrevive a Activity. O smoke oficial verifica explicitamente `stable-session-player-survives-activity-exit`.

**Fato:** no Scene-Provided, o mesmo objeto físico pode sobreviver, mas é readmitido e recebe novos `ActorId`, runtime content identity, preparation token e adoption token.

## 6. Linhagem de tokens

```text
Session context GUID
  └─ PlayerSlotReservationToken
       context + Slot + slotRevision

Scene flow:
  └─ SceneLocalPlayerAdmissionToken
       context + operationSequence + Slot + joinedSlotRevision
       └─ ScenePlayerActorAdoptionToken
            context + Slot + ActorId + RuntimeContentIdentity
            + PlayerActorPreparationToken + adoptionRevision

Common prepared/gameplay chain:
PlayerActorPreparationToken
  context + Slot + ActorId + RuntimeContentIdentity + materializationRevision
  └─ PlayerGameplayOccupancyToken
       + Owner + ActorProfileId + preparationToken + occupancyRevision
       └─ PlayerGameplayInputBindingToken
            + occupancyToken + bindingRevision
            └─ PlayerGameplayCameraEligibilityToken
                 + inputBindingToken + eligibilityRevision
                 └─ PlayerGameplayAdmissionToken
                      materialization + occupancy + input + camera + admission revisions

Transition:
PlayerActorCandidateStageToken
  + previous preparation/admission
  └─ PlayerGameplayChainHandoffToken
       └─ ActivityPlayerHandoffGroupToken
            └─ ActivityPlayerLifecycleAdmissionToken
                 previous/target owners + flow + RouteIds
```

### Staleness e release

| Token | Torna-se stale quando | Release |
|---|---|---|
| Reservation | Slot revision/context muda | `TryReleaseReservation` ou commit |
| Scene admission | Slot/record muda ou authoring/Host não coincide | `SceneLocalPlayerAdmissionRuntime.TryRelease` |
| Preparation | Actor/materialization muda | `TryReleasePreparedActor` |
| Adoption | preparation, Actor, owner ou scene record muda | `TryReleaseScenePlayerActorAdoption` |
| Occupancy | preparation ou occupancy revision muda | `TryReleaseOccupancy` |
| Input | occupancy ou binding muda | `TryRelease`, restaurando Gate/action map |
| Camera | Input, occupancy ou eligibility muda | `TryRelease` |
| Admission | qualquer capability revision muda | `TryReleaseDependencies` |
| Handoff | candidate/current evidence muda | commit/rollback/retry |
| Activity lifecycle | owner/Route/flow muda | finalização/rollback do lifecycle |

### Dependências suspeitas

**Alta — dependência indevida:** Camera incorpora e exige Input.

- `PlayerGameplayCameraEligibilityToken.InputBindingToken`.
- `TryValidateIdentityChain` exige `inputBinding.IsBound`.
- `CreateLifetimeScopeId` inclui `InputBindingRevision`.

Isso significa que refazer Input torna Camera stale mesmo com Slot, Host, Actor, rig e owner inalterados.

**Média — agregação rígida:** `PlayerGameplayAdmissionToken` inclui revisions de Occupancy, Input e Camera. É válido como identidade da transação de gameplay, mas inadequado como identidade do Player/assignment.

## 7. Verdades duplicadas

| Verdade | Locais | Classificação |
|---|---|---|
| Slot joined | Participation snapshot, Host state, joinedHosts, scene admission record | Autoridades paralelas com validação cruzada; inconsistência potencial |
| Host atual | Host state, joinedHosts, preparation handle, input binding record, scene admission/adoption | Referências físicas derivadas, mas sem token/revision comum |
| Actor selecionado | Participation SlotRecord e Preparation summary | Intenção canônica + snapshot coerente |
| Actor atual | Preparation, Occupancy, Input, Camera, Admission | Evidência derivada legítima, tokenizada |
| Owner atual | Materialization, Occupancy, Input, Camera, Admission | Evidência derivada legítima |
| Gameplay readiness | Input availability, Admission summary, Activity evaluator/lifecycle | Agregação + autoridade transacional |
| Camera atual | Eligibility record, Admission record, Camera output context | Eligibility, publisher e arbitration são autoridades diferentes |
| Input atual | Input context record e estado físico do Gate/PlayerInput | Autoridade lógica + materialização técnica |

O maior risco não está nas cópias downstream do Actor, que têm coerência forte por token. Está na associação Slot–Host, duplicada por referências Unity sem uma identidade ou revision compartilhada.

## 8. Readiness

A progressão oficial é:

```text
JoinedSlots
  → SelectedActors
  → LogicalActorsPrepared
  → GameplayReady
```

`ActivityPlayerAdmissionEvaluator.Evaluate` é puro: lê snapshots e não cria efeitos.

Porém, produzir `GameplayReady` é uma transação com efeitos:

1. confirma Occupancy;
2. seleciona action map e aplica Gate;
3. calcula Camera eligibility;
4. publica Camera request;
5. cria Gameplay Admission;
6. define `Ready` ou `BlockedByInputGate`.

Evidências:

- `ActivityPlayerAdmissionEvaluator.Evaluate`: agregação pura.
- `ActivityPlayerGameplayChainStageResolver.Resolve`: cria/resolve a cadeia.
- `PlayerGameplayAdmissionRuntimeContext.TryAdmit`: publica Camera.
- `TryAdmit`, linhas 324-339: `GameplayReady` depende de `inputBinding.IsAllowed`.
- `TryRefreshReadiness`: atualiza readiness exclusivamente a partir do Gate/Input.

**Conclusão:** `GameplayReady` mistura:

- agregação de prerequisites;
- transação de criação/release de capacidades;
- publicação de Camera;
- estado momentâneo do Input Gate.

Não é autoridade de ownership do Player, mas carrega cópias completas da identidade/owner.

## 9. QA existente

| Classe/menu | Prova real | Não prova |
|---|---|---|
| `QaP3FSessionSlotRuntimeSmoke` — `Run Session Player Slots Regression` | roster, capacity, reserve/join/release, stale reservation | Host/Actor/assignment |
| `QaP3G3ProvisioningBridgeSyntheticSmoke` — `Run Local Player Provisioning Regression` | Host–Slot manager join, parent persistente, rollback | Actor/gameplay |
| `QaP3M4B1SceneLocalPlayerAdmissionTransactionSmoke` | Scene Slot–Host admission/release | Actor preparation |
| `QaP3M4B2BScenePlayerActorAdoptionSmoke` | adoption, external ownership, release | Sucesso completo de Scene-Provided GameplayReady |
| `QaP3M4DSceneProvidedExitReentryRegression` | exit/reentry preservando objetos físicos | estabilidade de identidade lógica |
| `QaP3M4ESceneProvidedActivitySwitchRegression` | A→B→A com Actor físico scene-owned | Camera/Input independence |
| `QaP3M5BRouteTransitionAndNegativeMatrixSmoke` — `Run Scene Player Route Lifecycle Regression` | Route switch, fresh identities, cleanup e negativos | assignment contínuo entre origins |
| `QaPlayerGameplayAdmissionRegression` | Manager-Provisioned P3J/P3K, handoff e release order | Scene-Provided Camera |
| `QaCameraRuntimeHostIntegrationRegression` | publicação única e release da Camera pelo gameplay admission | Camera sem Input ou Scene-Provided prefab oficial |
| `QaCameraPlayerAuthoringUxSmoke` | authoring/Inspector | Runtime assignment |

Lacunas principais:

- nenhum teste de Host swap no mesmo Slot;
- nenhum teste de Actor swap preservando uma identidade de assignment;
- nenhum teste de Camera elegível sem Input binding;
- nenhum teste de release/rebind de Input preservando Camera;
- nenhum teste de Scene-Provided + `GameplayReady` + Camera no prefab oficial;
- nenhuma asserção de uma autoridade Slot+Host+Actor;
- nenhum teste do ainda inexistente Session-Persistent source.

O caso `gameplay-ready-reaches-canonical-pipeline` em `QaP3M4B2BScenePlayerActorAdoptionSmoke` somente prova que Adoption deixou de ser o blocker; ele permite falha posterior e não prova Camera/Input/admission bem-sucedidos.

## 10. FIRSTGAME

### Composição oficial em `HEAD`

`Player_SceneProvided.prefab` contém:

- `PlayerInput`;
- `LocalPlayerHostAuthoring`;
- `SceneLocalPlayerAdmissionAuthoring`;
- Actor filho com `PlayerActorDeclaration`.

`Player_SceneProvided_With_Camera.prefab` adiciona no Actor:

- `CameraRigComposer`;
- `CinemachineCamera`;
- `PlayerGameplayCameraAuthoring`.

Mas o prefab oficial não contém `UnityPlayerInputGateAdapter`.

`Player_SceneProvided_With_Pause.prefab` contém:

- `PausePlayerInputBinding`;
- `UnityPlayerInputGateAdapter` no Host.

`Activity_PlayerLocalProvider.asset` usa `playerParticipationRequirementLevel: 30`, equivalente a `LogicalActorsPrepared`, não `GameplayReady` (`40`).

### Contradição de produto

O designer precisa combinar conhecimento de objetos diferentes:

- Slot/Profile no application e Activity;
- Host/Admission no prefab raiz;
- Actor/Profile no Actor;
- Gate no Host;
- Camera authoring/rig/targets no Actor;
- Camera output em cena persistente;
- requirement level no `ActivityAsset`.

**Fato:** a Camera prefab oficial não satisfaz o endpoint source, que exige exatamente um `UnityPlayerInputGateAdapter` no Host.

**Fato:** a Activity real para Scene-Provided para em `LogicalActorsPrepared`; portanto Camera/Input/Gameplay Admission não fazem parte de seu readiness oficial.

Há uma modificação local não versionada adicionando Gate ao prefab Camera. Ela foi excluída como evidência oficial e não foi alterada. Essa mudança local confirma empiricamente a fricção de authoring, mas não resolve o problema arquitetural: Camera continua dependente do lifetime/token de Input.

## 11. Inconsistências

| Severidade | Categoria | Achado e evidência |
|---|---|---|
| Alta | Contrato ausente | Nenhum snapshot/token representa Slot+Host+Actor+owner+lifetime. |
| Alta | Dependência indevida | `PlayerGameplayCameraEligibilityToken` incorpora Input; `TryValidateIdentityChain` exige binding corrente. |
| Alta | UX fragmentada | FIRSTGAME separa Gate, Camera, admission, output e requirement; Camera prefab oficial não possui Gate. |
| Média/alta | Autoridade duplicada | Slot–Host existe em Host state, `joinedHosts` e admission records sem revision comum. |
| Média | Lifetime misturado | Contextos P3K são Session-scoped, mas os registros atuais carregam owner Activity e são destruídos no Activity exit. |
| Média | Identidade instável | Scene Actor tem seu `ActorId` reescrito por adoption e restaurado no release. |
| Média | Scope divergente | Manager Host/selection sobrevivem à Activity; Scene Host association/selection são removidos no Activity exit. |
| Média | QA ausente | Não há prova de Scene-Provided Camera GameplayReady no consumidor real. |
| Média | Diagnóstico insuficiente | Não há diagnóstico agregado de “assignment atual”; é necessário correlacionar vários snapshots e registros internos. |
| Conhecida/deferida | Contrato ausente | `Session-Persistent Logical Player source` está documentado, mas não implementado. |

Não encontrei fallback implícito por nome/tag, singleton ou service locator nessa cadeia.

## 12. Causas raiz

1. **A cadeia de readiness tornou-se também cadeia de identidade.**  
   Como não existe evidência capability-neutral de Slot+Host+Actor, cada capability incorpora integralmente a anterior. Input acaba fornecendo a ponte física de Host para Camera.

2. **A convergência ocorre tarde demais.**  
   Participation conhece Slot/selection; registries conhecem Host; Preparation só une Slot+Host+Actor depois da materialização. Não existe assignment coerente entre Join e Prepared nem durante rebuild independente de capabilities.

3. **Origin e lifetime estão embutidos em authorities diferentes.**  
   Manager-Provisioned preserva Host/selection na Session; Scene-Provided libera admission/selection por Activity; Session-Persistent não existe. A mesma pergunta de negócio tem respostas diferentes conforme a origem.

## 13. Avaliação de `Current Player Slot Assignment`

| Candidato | Dados disponíveis | Ausências/incompatibilidades | Decisão |
|---|---|---|---|
| `PlayerParticipationRuntimeContext` | Slot, Profile, join, selection, revisions | Sem Host, ActorId, materialização ou owner | Insuficiente |
| `PlayerActorPreparationRuntimeContext` | Slot, Actor, owner, runtime content; handle contém Host | Só existe quando Prepared; lifetime da materialização; Host não está no snapshot/token | Mais próximo tecnicamente, mas insuficiente |
| `PlayerGameplayOccupancyRuntimeContext` | Slot, Actor, owner, preparation token | Sem Host; começa depois da preparação; é gameplay-specific | Não deve virar assignment |
| `SceneLocalPlayerAdmissionRuntime` | Slot + Host + admission token | Apenas Scene-Provided; sem Actor | Insuficiente/origin-specific |
| `PlayerActorPreparationRuntimeHostModule` + preparation context | Pode reconstruir Slot+Host+Actor | Duas estruturas, não atômicas, sem assignment revision | Consulta composta, não autoridade |
| Outra autoridade atual | Nenhuma contém todos os campos | — | Nenhuma suficiente |

### Dados existentes para um futuro conceito, sem prescrever classe

- Slot: `PlayerSlotId`;
- Host atual: `joinedHosts`/admission record;
- Profile selecionado: `PlayerSlotRuntimeSnapshot`;
- Actor/materialização: `PlayerActorPreparationSummary`;
- owner/scope: `PlayerActorMaterializationSnapshot`;
- physical evidence: `PlayerActorMaterializationHandle`;
- origin/physical ownership: join/adoption contracts.

### Dados ausentes

- identidade tipada do Host ou correlação equivalente;
- revision/token próprio da associação Slot–Host–Actor;
- snapshot atômico comum aos três origins;
- estado válido antes de Preparation;
- semântica explícita para Host sem Actor e Actor swap;
- contrato do Session-Persistent source.

**Decisão:** `Current Player Slot Assignment` é um contrato ausente. Não deve ser representado por Input Binding, Camera Eligibility ou Gameplay Admission. Também não há evidência suficiente para criar automaticamente uma nova classe sem antes fechar lifetime e mutation semantics.

## 14. Ordem recomendada para as próximas auditorias

1. **Mutation e lifetime da associação Slot–Host–Actor**  
   Definir semanticamente join, host replacement, actor replacement, empty assignment e Activity/Route survival.

2. **Input capability**  
   Separar prova de Host/Actor da disponibilidade momentânea do Gate e do action map.

3. **Camera capability**  
   Determinar quais evidências realmente precisa e remover, se injustificada, a dependência de Input.

4. **Gameplay Admission e `GameplayReady`**  
   Separar ownership, capability aggregation, transação e readiness momentâneo.

5. **Activity/Route handoff**  
   Verificar qual identidade deve sobreviver enquanto materializações são substituídas.

6. **Publication/arbitration de Camera**  
   Avaliar request lifetime depois de estabilizar assignment e capability tokens.

7. **Authoring/Inspector UX**  
   Consolidar Slot, Host, Actor, Gate, Camera e requirement sem esconder contracts.

### Allowed now

- Continuar auditorias somente leitura.
- Especificar invariantes do assignment e uma matriz de transições.
- Adicionar posteriormente QA que observe estabilidade entre capabilities, quando implementação for autorizada.

### Deferred/rejected

- Criar singleton/service locator.
- Transformar Occupancy ou Input Binding em Player authority.
- Criar fallback por nome, tag, hierarchy search ou `Camera.main`.
- Portar Session/NewScripts.
- Alterar Camera ou Input antes de fechar o contrato de assignment.

### Checklist manual futuro

- Executar em Unity, separadamente, os regressions de Session Slot, provisioning, Scene lifecycle, Player Gameplay Admission e Camera Runtime Host.
- Provar Scene-Provided com `GameplayReady` e Camera.
- Provar Actor swap com Host constante.
- Provar rebuild de Input e Camera sem mudança da associação principal.
- Provar Activity/Route switch para Manager e Scene origins.
- Verificar release order e ausência de tokens stale retidos.

Nenhum desses itens foi executado nesta auditoria; portanto não há declaração de PASS de Unity/import/Play Mode.

## 15. Arquivos e símbolos examinados

Principais fontes oficiais examinadas:

- `package.json`; asmdefs Runtime e Editor.
- `PlayerSlotId`, `PlayerSlotProfile`.
- `PlayerParticipationRuntimeContext` e partial Scene admission.
- `PlayerSlotRuntimeSnapshot`, `PlayerParticipationSnapshot`, `PlayerSlotReservationToken`.
- `LocalPlayerProvisioningBridge`, `LocalPlayerJoinResult`.
- `LocalPlayerHostAuthoring`, `SceneLocalPlayerAdmissionAuthoring`.
- `SceneLocalPlayerAdmissionRuntime` e Activity lifecycle runtime.
- `PlayerActorPreparationRuntimeHostModule` e partials.
- `PlayerActorPreparationRuntimeContext`, Scene adoption e Promotion.
- `PlayerActorMaterializationHandle`, `AttachedPlayerActorMaterializationAdapter`.
- `ActorProfile`, `ActorProfileId`, `ActorId`, `PlayerActorDeclaration`.
- `RuntimeContentOwner`, `RuntimeContentIdentity`, `RuntimeScopeContext`.
- Occupancy, Input Binding, Camera Eligibility e Gameplay Admission contexts, summaries e tokens.
- Gameplay chain handoff, Activity stage/group/lifecycle contracts.
- `ActivityPlayerActorLifecycleParticipant`.
- `ActivityPlayerAdmissionEvaluator`.
- QA classes listadas na seção 9.
- FIRSTGAME: quatro prefabs Player/Actor, `SceneProvidedGameplay`, `Activity_PlayerLocalProvider`, `FG_PlayerSceneProvider`, `FG_GameApplication` e configuração persistente de Camera.

Estado final confirmado: Framework e QA limpos; FIRSTGAME mantém exclusivamente a alteração local preexistente no prefab Camera.