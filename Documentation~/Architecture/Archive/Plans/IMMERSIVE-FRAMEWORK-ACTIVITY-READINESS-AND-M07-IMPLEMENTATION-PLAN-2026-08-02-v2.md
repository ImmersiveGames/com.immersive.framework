# Immersive Framework — Activity Entry Readiness + M07 Manager-Provisioned Player
## Plano integrado de implementação v2

**Data:** 2026-08-02  
**Status:** Proposed implementation plan  
**Substitui:** `IMMERSIVE-FRAMEWORK-M07-IMPLEMENTATION-PLAN-2026-08-02.md`  
**Decisão normativa:** `IF-ADR-007 — Activity Entry Readiness and Reveal Gating` — Accepted  
**Escopo:** `com.immersive.framework`, `QAFramework`, `planet-devourer`

### Baseline auditado

| Repositório | HEAD de referência |
|---|---|
| `ImmersiveGames/com.immersive.framework` | `5ade3bf9d0c50a7c960975f0f26c6ea42b456be8` |
| `rinnocenti/QAFramework` | `d7e21026ab4564de82c75f31c11cbf6f4f9e3837` |
| `ImmersiveGames/planet-devourer` | `38f37da16daa310fc41585d20d4590e5a46801f1` |

**Regra operacional:** os repositórios permanecem somente leitura. Cada implementação deve ser entregue como `.zip`, contendo arquivos criados, editados e removidos, mais `CHANGESET.md`, SHAs de origem, validações executadas e limitações.

---

# 1. Decisão executiva

O problema genérico de Activity Readiness deve entrar no plano antes do M07.

O M07 não deve criar:

```text
um estado global Waiting exclusivo de Player
um ActivityPlayerParticipationEntryMode paralelo
um PendingReadiness em ActivityContentExecutionResult
um botão obrigatório de Prepare/Reconcile no jogo
```

O M07 deve consumir o contrato já aceito pelo `IF-ADR-007`:

```text
ActivityEntryReadinessPolicy
  ObserveOnly
  WaitCovered
  WaitVisible

Activity authority
  separada de readiness

Activity readiness
  agregada por occurrence

Presentation
  controlada pela política de entrada

Capability gate
  retido até Ready nas políticas de espera
```

“Waiting for Join” será uma **razão de preparação de uma contribuição Required**, não um novo estado global de Activity.

A representação agregada será:

```text
ActivityReadinessStatus.NotReady
+ RequiredPendingCount > 0
+ RequiredFailedCount = 0
+ BlockingIssueCount = 0
+ DiagnosticReason = WaitingForJoin
```

Falha real será:

```text
ActivityReadinessStatus.NotReady
+ RequiredFailedCount > 0
+ BlockingIssueCount > 0
```

A Activity permanece a autoridade atual enquanto está preparando. `WaitVisible` ou `WaitCovered` decide quando revelar conteúdo e quando liberar input, interaction e gameplay.

---

# 2. Objetivos do programa

## 2.1 Objetivo genérico de readiness

Implementar integralmente o `IF-ADR-007`:

```text
Activity scenes materializadas
→ readiness occurrence iniciado
→ Required contributions preparam
→ política de entrada controla reveal e gate
→ Ready libera exatamente uma vez
→ Failed termina explicitamente sem fallback
→ replacement/reentry invalida occurrence antigo
```

O programa precisa fechar:

- policy authoring em `ActivityAsset`;
- agregação que distingue Preparing de Failed;
- espera event-driven por occurrence;
- `ObserveOnly`, `WaitVisible` e `WaitCovered`;
- integração com Loading, Transition e capability gate;
- Route Startup Activity;
- failure/recovery diagnostics;
- validators;
- QA técnico e prova real no FIRSTGAME.

## 2.2 Objetivo específico do M07

Usar a infraestrutura genérica para fechar a jornada pública:

```text
M07 Activity entra com WaitVisible
→ conteúdo é revelado
→ gameplay permanece bloqueado
→ Player readiness contribution = Preparing / WaitingForJoin
→ usuário solicita Join por controle persistente permitido
→ Host técnico é criado e commitado
→ Slot fica Joined
→ framework reconcilia o occurrence ativo
→ Actor default é selecionado quando requerido
→ Actor lógico é preparado
→ Actor físico é materializado sob ActorMount
→ input/camera/gameplay evidence é publicada quando requerida
→ Player readiness contribution = Completed
→ Activity aggregate = Ready
→ capability gate é liberado
```

Na saída:

```text
Activity exit
→ libera gameplay/input/camera contextual
→ libera Actor lógico e físico
→ preserva PlayerInput, LocalPlayerHost, Slot, assignment e seleção da Session
```

`Session Leave` permanece uma capacidade separada.

---

# 3. Correções obrigatórias ao plano M07 anterior

## 3.1 Remover `ActivityPlayerParticipationEntryMode`

Não criar:

```csharp
ActivityPlayerParticipationEntryMode
RequireSatisfiedOnEnter
AllowActiveWaiting
```

A intenção de entrada já pertence à Activity por meio de:

```csharp
ActivityEntryReadinessPolicy
```

Player Participation continua declarando:

```text
quem participa
qual nível é requerido
```

Entry Readiness declara:

```text
como a Activity é apresentada e gated enquanto a preparação ocorre
```

## 3.2 Não criar `ActivityReadinessStatus.Waiting`

Manter o vocabulário agregado atual:

```text
None
Ready
NotReady
```

Adicionar evidência explícita para distinguir:

```text
NotReady / Preparing
NotReady / Failed
```

A UI pode mostrar “Waiting for Join”, “Preparing Actors” ou “Waiting for Camera” a partir da contribuição e de sua razão.

## 3.3 Não criar `ActivityContentExecutionStatus.PendingReadiness`

`ActivityContentExecutionResult` representa execução síncrona de lifecycle/content.

Readiness assíncrona deve ser representada por uma contribuição occurrence-scoped, não por um resultado de execução que permanece eternamente “pending”.

Em particular, `ActivityReadinessParticipant` não deve produzir `BlockingFailure` apenas porque começou em `Preparing`. O Enter deve iniciar a contribuição e completar a fase síncrona sem fabricar falha. A agregação de readiness passa a ser a única autoridade para o estado de preparação.

## 3.4 Não exigir endpoint manual de reconcile no primeiro fechamento

A jornada pública precisa funcionar por:

```text
Activity policy authoring
+ public RequestJoin
+ stable package-owned notification
+ automatic reconcile
+ public readiness diagnostics/events
```

Um comando público de retry/recovery deve ser criado somente após a decisão pendente do ADR sobre recovery authoring.

QA public-only deve provar o fluxo usando a mesma superfície disponível ao FIRSTGAME, sem chamar internals e sem um botão técnico de preparação.

## 3.5 Não reinterpretar Zero Participants

`Zero Participants = Rejected` continua significando rejeição quando a projeção realmente resolve zero participantes.

No M07 com `ExplicitSlots`, os Slots configurados existem na projeção, embora ainda não estejam Joined. A condição é:

```text
Required Player contribution Preparing / WaitingForJoin
```

Não:

```text
zero participants accepted
zero participants silently converted to waiting
```

---

# 4. Autoridades e responsabilidades

```text
ActivityAsset
  Autoridade authoring da ActivityEntryReadinessPolicy.

ActivityFlowRuntime
  Autoridade do Activity occurrence, participant/contribution lifecycle,
  readiness aggregation, updates e invalidation.

GameFlowRuntime
  Autoridade da ordem da operação:
  Loading, Transition Before/After, capability gate e release.

LoadingSurface
  Apresentação de LoadingScenes, MaterializingActivity,
  PreparingActivity e Ready.
  Não decide readiness.

TransitionSurface
  Envelope visual.
  Não decide readiness.

PlayerParticipationRuntimeContext
  Autoridade Session-scoped de Slot, allocation, assignment e selection.

ActivityPlayerActorLifecycleParticipant
  Autoridade contextual de Player/Actor para o Activity occurrence.

FrameworkRuntimeHost
  Composição explícita host-scoped entre GameFlow e módulos.
  Não é service locator global.

FIRSTGAME
  Solicita operações públicas e apresenta snapshots.
  Não cria RuntimeScopeContext, não prepara Actor e não repara lifecycle.
```

---

# 5. Modelo técnico alvo

## 5.1 Policy pública

```csharp
public enum ActivityEntryReadinessPolicy
{
    ObserveOnly = 0,
    WaitCovered = 10,
    WaitVisible = 20
}
```

Campo em `ActivityAsset`:

```text
Activity Entry Readiness
  Policy
    Observe Only
    Wait Covered
    Wait Visible
```

Compatibilidade:

- default serializado e runtime: `ObserveOnly`;
- assets existentes mantêm o comportamento atual;
- enum inválido é erro explícito;
- Route não duplica o campo;
- Route Startup consome a policy da Startup Activity.

## 5.2 Estado agregado

`ActivityReadinessState` deve expor evidência suficiente para não inferir preparação a partir de uma mensagem:

```text
Status
RequiredCount
OptionalCount
RequiredPendingCount
RequiredFailedCount
OptionalPendingCount
OptionalFailedCount
BlockingIssueCount
IsPreparing
HasTerminalFailure
DiagnosticReason
Occurrence identity ou snapshot correlacionável
```

Regras:

```text
Required Preparing
  Status = NotReady
  IsPreparing = true
  HasTerminalFailure = false
  BlockingIssueCount não aumenta por preparation normal

Required Failed
  Status = NotReady
  HasTerminalFailure = true
  BlockingIssueCount > 0

Optional Preparing/Failed
  diagnóstico apenas
  não impede Ready

Nenhum Required pendente ou falho
+ technical baseline válido
  Status = Ready
```

## 5.3 Contribuição genérica

A implementação deve permitir contribuições scene-authored e package-owned sem acoplar `ActivityFlowRuntime` a Player.

Shape interno recomendado:

```text
IActivityReadinessContribution
  Descriptor
  Occurrence
  Snapshot
  StateChanged
  Release

ActivityReadinessContributionDescriptor
  Id
  Requiredness
  Source
  DisplayName

ActivityReadinessContributionSnapshot
  State: Preparing / Completed / Failed / Released
  Reason
  Revision
```

Adaptações:

```text
ActivityReadinessParticipant
  contribuição authorable de cena

Player Activity lifecycle
  contribuição técnica package-owned

futuros save/camera/network modules
  podem usar o mesmo modelo sem criar outro agregador
```

Não tornar a interface pública no primeiro corte sem necessidade de consumidor externo. O authoring público continua sendo `ActivityReadinessParticipant`.

## 5.4 Espera por occurrence

Contratos sugeridos:

```csharp
internal enum ActivityEntryReadinessWaitStatus
{
    Ready = 10,
    Failed = 20,
    Invalidated = 30,
    Cancelled = 40
}
```

```text
ActivityEntryReadinessWaitResult
  Occurrence
  Policy
  Status
  AggregateReadiness
  Required pending/failed identities
  Source
  Reason
```

Requisitos:

- event-driven;
- keyed por referência exata da Activity + transition sequence;
- exatamente um terminal;
- completion antigo não libera occurrence novo;
- clear/replacement/restart invalida o wait;
- late completion permanece rejeitada e diagnosticada;
- nenhum polling de cena ou `Update()` para verificar Ready.

## 5.5 Estado da operação de entrada

GameFlow deve manter um estado operation-scoped:

```text
None
Composing
PreparingCovered
PreparingVisible
ReadyReleased
FailedCovered
FailedVisible
Invalidated
Cancelled
```

Esse estado não substitui Activity authority ou readiness. Ele registra apenas o envelope da operação e o que permanece retido.

## 5.6 Loading

Adicionar ou materializar semanticamente:

```text
LoadingScenes
MaterializingActivity
PreparingActivity
Ready
```

`PreparingActivity` é indeterminado por padrão.

Não calcular percentual por:

```text
participants completed / participants total
tempo decorrido
timer artificial
```

## 5.7 Gate e recuperação

Nas políticas de espera:

```text
input normal
interaction normal
gameplay
```

permanecem bloqueados até Ready.

O controle de Join do M07 precisa permanecer operável. Ele pertence ao control plane/persistent UI, não ao gameplay input do Player que ainda não existe.

Obrigatório provar:

```text
WaitVisible ativo
+ gameplay gate retido
+ persistent Join control continua funcional
```

Falha Required:

- termina o wait;
- não libera capabilities;
- não fica como request in-flight eterno;
- cria blocker scoped de recuperação;
- permite uma nova Route/Activity request de recuperação;
- não executa rollback automático do destino já commitado.

---

# 6. Player como consumidor do readiness genérico

## 6.1 Contribuição de Player

Para uma Activity com Player participation:

```text
Contribution ID
  framework.player-participation

Requiredness
  Required quando RequirementLevel > None e há projeção requerida

State/reason examples
  Preparing / WaitingForJoin
  Preparing / WaitingForActorSelection
  Preparing / PreparingLogicalActor
  Preparing / PreparingGameplayAdmission
  Completed / RequirementSatisfied
  Failed / HostEvidenceInvalid
  Failed / ActorPreparationFailed
  Failed / GameplayAdmissionFailed
  Released / ActivityExit
```

A contribuição é occurrence-scoped e não é um snapshot global da Session.

## 6.2 Enter

No Enter:

1. Resolver projeção.
2. Validar configuração e owner.
3. Criar a contribuição para o occurrence.
4. Aplicar imediatamente o delta possível.
5. Se faltar um estado runtime esperado, permanecer `Preparing`.
6. Se ocorrer erro real, marcar `Failed`.
7. Se o requirement já estiver satisfeito, marcar `Completed`.

Ausência de um Slot `Joined` em `ExplicitSlots` é preparação esperada:

```text
Preparing / WaitingForJoin
```

Host inválido para um Slot que já foi admitido é falha:

```text
Failed / HostEvidenceInvalid
```

## 6.3 Reconcile por delta

O reconcile deve comparar:

```text
Activity reference
occurrence sequence
RuntimeContentOwner
Session context ID
participation revision
per-Slot revision
selection revision
preparation token/revision
gameplay admission state
```

Aplicar somente o delta necessário:

```text
JoinedSlots
  confirmar join e Host evidence

SelectedActors
  selecionar default quando a policy atual autoriza esse comportamento

LogicalActorsPrepared
  preparar/materializar Actor

GameplayReady
  assegurar input/camera/gameplay evidence
```

Reconcile repetido sem mudança:

```text
SucceededNoChange
```

Não cria outro Actor, token, binding ou camera request.

## 6.4 Trigger estável

Não publicar change durante `TryMarkJoined`.

Publicar somente depois de:

```text
Slot Joined
+ assignment committed
+ Host admission committed
+ Host evidence registered
```

A seleção publica mudança somente depois do commit da selection revision.

O coordinator host-scoped:

- serializa reconcile;
- coalesce revisions;
- ignora occurrence stale;
- não executa durante transition mutation incompatível;
- agenda uma nova passagem se a revision mudar durante o reconcile;
- publica diagnóstico imutável.

## 6.5 Superfície pública

Fluxo normal do M07:

```text
LocalPlayerProvisioningAuthoring.RequestJoin
→ package internal auto-reconcile
→ ActivityReadinessEvents / public snapshot
```

O consumidor não recebe:

```text
RuntimeScopeContext
PlayerActorPreparationRuntimeHostModule
TryPrepareSelectedActor
TryEnsureCurrentGameplay
```

A superfície pública necessária é observacional:

```text
ActivityEntryReadinessPolicy no ActivityAsset
ActivityReadinessEvents
read-only current readiness/entry snapshot
structured join result
structured Player participation diagnostics
```

Retry técnico de Player não entra no primeiro fechamento. Recovery authoring deve ser genérico e decidido separadamente.

---

# 7. Cortes de implementação

# IF-READY-01 — Corrigir a semântica de agregação

## Tipo

```text
Técnico / ActivityFlow contracts
```

## Objetivo

Separar preparação normal de falha e preparar o runtime para espera occurrence-scoped.

## Escopo

- manter `ActivityReadinessStatus.None/Ready/NotReady`;
- adicionar contadores Required/Optional pending/failed;
- adicionar `IsPreparing` e `HasTerminalFailure`;
- corrigir `ActivityReadinessRecomposer`;
- impedir que Required Preparing aumente `BlockingIssueCount`;
- adaptar `ActivityReadinessParticipant` para não retornar blocking failure apenas por iniciar preparação;
- preservar late completion rejection;
- preservar `ObserveOnly` behavior neste corte.

## Fora de escopo

- policy authoring;
- Transition/Loading retention;
- Player reconcile;
- recovery UI;
- timeout.

## Arquivos prováveis

```text
com.immersive.framework/
  Runtime/ActivityFlow/ActivityReadinessState.cs
  Runtime/ActivityFlow/ActivityReadinessOccurrenceState.cs
  Runtime/ActivityFlow/ActivityReadinessRecomposer.cs
  Runtime/ActivityFlow/ActivityReadinessParticipant.cs
  Runtime/ActivityFlow/ActivityReadinessUpdate.cs
  Runtime/ActivityFlow/ActivityFlowStartResult.cs ou snapshot equivalente
```

Verificar helpers existentes em `Runtime/Common` antes de criar normalização ou equality helpers.

## Superfície de produto afetada

Diagnostics de Activity Readiness; nenhuma policy nova ainda.

## Fluxo esperado

```text
Required participant begins
→ Activity NotReady
→ IsPreparing = true
→ BlockingIssueCount = 0

Complete
→ Ready

Fail
→ NotReady
→ HasTerminalFailure = true
→ BlockingIssueCount > 0
```

## Smoke técnico

- Required Preparing;
- Required Completed;
- Required Failed;
- Optional Preparing;
- Optional Failed;
- nenhum participant;
- late completion;
- duplicate completion;
- release.

## Aceite técnico

- preparação normal não aparece como failure;
- Optional nunca bloqueia;
- aggregate fica Ready após Required completar;
- immutable snapshot;
- no polling;
- sem breaking change público desnecessário.

## Aceite de produto

- diagnostics distinguem “preparando” de “falhou”;
- mensagem não é a única fonte dessa distinção.

## Ganho arquitetural

Estabelece readiness como autoridade própria em vez de reutilizar failure de content execution.

## Ganho de usabilidade

Painéis podem mostrar Waiting/Preparing sem alarmes falsos.

## Commit sugerido

```text
Separate Activity readiness preparation from failure
```

## Gate de saída

Todos os testes de readiness existentes continuam verdes e a transição Preparing → Ready funciona pelo mesmo occurrence.

---

# IF-READY-02 — Policy authoring e validação

## Tipo

```text
Produto + authoring + validation
```

## Objetivo

Adicionar `ActivityEntryReadinessPolicy` ao `ActivityAsset` com default compatível.

## Escopo

- enum `ObserveOnly`, `WaitCovered`, `WaitVisible`;
- campo no `ActivityAsset`;
- Inspector designer-first;
- tooltips;
- validators de policy, transition mode e gate mode;
- Route Startup cross-asset validation;
- migration/default `ObserveOnly`;
- diagnostics de configuração.

## Fora de escopo

- runtime wait;
- Player;
- timeout/retry;
- Route-owned duplicate policy.

## Arquivos prováveis

```text
com.immersive.framework/
  Runtime/ActivityFlow/ActivityEntryReadinessPolicy.cs
  Runtime/Authoring/ActivityAsset.cs
  Editor/.../ActivityAssetEditor*.cs
  Editor/.../Activity authoring validator files
  Editor/.../Route validator files
  Documentation~/Guides/Activity-Readiness-Usage.md
```

Os paths Editor exatos devem ser confirmados no HEAD antes do patch.

## Regras de validação

```text
ObserveOnly + Seamless/Fade/FadeWithLoading
  valid

WaitVisible + Seamless/Fade/FadeWithLoading
  valid

WaitCovered + Fade/FadeWithLoading
  valid

WaitCovered + Seamless
  blocking error

WaitVisible/WaitCovered + gate sem Input/Interaction/Gameplay
  blocking error
```

Para Route Startup:

```text
Route gate
  deve satisfazer Startup Activity policy
```

## Superfície de produto

```text
Activity Entry Readiness
  Policy
```

Advanced/Debug exibe valores serializados e compatibilidade.

## Smoke técnico

- assets antigos desserializam como ObserveOnly;
- enum inválido bloqueia;
- combinações válidas passam;
- combinações inválidas não recebem fallback silencioso;
- Route Startup reporta cross-asset mismatch.

## Aceite técnico

- runtime não normaliza policy inválida silenciosamente;
- default explícito;
- validator e runtime usam a mesma regra central quando possível;
- Runtime não depende de Editor.

## Aceite de produto

- designer entende ObserveOnly, WaitCovered e WaitVisible sem abrir código;
- correção proposta pelo Inspector aponta o asset/campo responsável.

## Ganho arquitetural

Coloca intenção de entry readiness na Activity, como definido no ADR.

## Ganho de usabilidade

Elimina configuração implícita em Loading/Transition.

## Commit sugerido

```text
Add Activity entry readiness policy authoring
```

## Gate de saída

Policy serializa, valida e preserva compatibilidade; ainda não declarar Wait modes operacionais até o próximo corte.

---

# IF-READY-03 — Awaiter occurrence-scoped

## Tipo

```text
Técnico / ActivityFlow runtime
```

## Objetivo

Criar uma espera tipada, event-driven e invalidável para o initial readiness occurrence.

## Escopo

- `ActivityEntryReadinessWaitStatus`;
- `ActivityEntryReadinessWaitResult`;
- waiter interno;
- terminal Ready/Failed/Invalidated/Cancelled;
- subscriptions e cleanup;
- revision monotônica;
- current/pending occurrence;
- clear/replacement/restart invalidation;
- exposed internal port para GameFlow.

## Fora de escopo

- visual transition;
- loading phase;
- capability gate;
- Player;
- timeout.

## Arquivos prováveis

```text
com.immersive.framework/
  Runtime/ActivityFlow/ActivityEntryReadinessWaitStatus.cs
  Runtime/ActivityFlow/ActivityEntryReadinessWaitResult.cs
  Runtime/ActivityFlow/ActivityEntryReadinessWaiter.cs
  Runtime/ActivityFlow/ActivityFlowRuntime.cs ou partial dedicado
  Runtime/ActivityFlow/ActivityReadinessOccurrence.cs
  Runtime/ActivityFlow/ActivityReadinessOccurrenceState.cs
```

## Fluxo esperado

```text
Begin wait for occurrence N
→ Required Preparing
→ update occurrence N
→ Ready terminal

replacement creates occurrence N+1
→ N Invalidated
→ completion from N rejected
```

## Smoke técnico

- immediate Ready;
- delayed Ready;
- Required Failed;
- invalidated by clear;
- invalidated by replacement;
- cancelled operation;
- double terminal;
- stale completion;
- no participants baseline.

## Aceite técnico

- nenhum polling;
- exatamente um terminal;
- subscriptions liberadas;
- result preserva occurrence;
- late completion não afeta current Activity.

## Aceite de produto

Ainda não há UI nova; snapshots já podem explicar o terminal.

## Ganho arquitetural

Fornece primitive genérica para GameFlow sem inverter autoridade.

## Ganho de usabilidade

Permite políticas de espera previsíveis em qualquer sistema de preparação.

## Commit sugerido

```text
Add occurrence-scoped Activity readiness waiting
```

## Gate de saída

Awaiter prova todos os terminais e invalidation antes de integrar Transition/Gate.

---

# IF-READY-04 — GameFlow reveal e capability gating

## Tipo

```text
Técnico / lifecycle orchestration
```

## Objetivo

Implementar `ObserveOnly`, `WaitVisible` e `WaitCovered` na ordem da operação.

## Escopo

- consumo da policy por direct Activity request;
- consumo por Route Startup Activity;
- WaitVisible reveal-before-ready;
- WaitCovered reveal-after-ready;
- retenção/release exata do capability gate;
- lifecycle request admission durante wait;
- terminal failure com destination authoritative;
- recovery blocker;
- cancellation/invalidation;
- no re-gating em post-release readiness changes.

## Fora de escopo

- timeout/retry authoring;
- automatic rollback;
- Player;
- custom percentage progress.

## Arquivos prováveis

```text
com.immersive.framework/
  Runtime/GameFlow/GameFlowRuntime.cs ou partial dedicado
  Runtime/ActivityFlow/ActivityFlowRuntime.cs ou port dedicado
  Runtime/Transition/... operation result/diagnostic files
  Runtime/Gate/... retained gate/recovery blocker files
  Runtime/RouteLifecycle/... startup integration files
  Runtime/ApplicationLifecycle/FrameworkRuntimeHost*.cs somente para adaptação explícita
```

## Fluxos obrigatórios

### ObserveOnly

```text
compose
→ authority commit
→ transition/loading release
→ gate release
→ readiness may complete later
```

### WaitVisible

```text
cover/load
→ compose
→ authority commit
→ begin readiness
→ hide loading
→ Transition After/reveal
→ retain capability gate
→ Ready
→ release gate once
```

### WaitCovered

```text
cover/load
→ compose
→ authority commit
→ begin readiness
→ retain loading/cover/gate
→ Ready
→ hide loading
→ Transition After/reveal
→ release gate once
```

### Failure

```text
Required Failed
→ typed committed-destination failure
→ unsafe capabilities remain blocked
→ operation no longer in-flight
→ recovery request remains possible
```

## Smoke técnico

- three policies;
- direct Activity;
- Route Startup;
- required completion;
- required failure;
- invalidation;
- cancellation;
- same Activity request while waiting;
- recovery request after failure;
- post-release NotReady does not re-cover or re-gate.

## Aceite técnico

- no gate leak;
- no double Transition After;
- no Loading hide before policy allows;
- failure does not silently release;
- operation in-flight flag clears at terminal;
- recovery blocker has explicit owner/lifetime;
- no global lookup.

## Aceite de produto

- policy behavior matches Inspector explanation;
- destination failure is diagnosable without interpreting raw logs.

## Ganho arquitetural

Separa authority, readiness, presentation e capabilities.

## Ganho de usabilidade

Permite produção coberta e preparação didática visível usando a mesma feature.

## Commit sugerido

```text
Implement Activity readiness reveal and gate policies
```

## Gate de saída

Direct Activity e Route Startup passam para todas as policies sem stale gate/transition.

---

# IF-READY-05 — Loading, diagnostics e produto M03

## Tipo

```text
Produto + diagnostics + QA + integração real
```

## Objetivo

Fechar o ADR-007 como capacidade de produto antes de acoplar M07.

## Escopo package

- semantic loading phase `PreparingActivity`;
- indeterminate progress;
- entry-readiness snapshot;
- pending/failed participant identities;
- cover/reveal state;
- gate state;
- last terminal;
- Activity Inspector Advanced/Debug;
- `ActivityReadinessEvents` permanece observer.

## Escopo QAFramework

Provar toda a matriz do ADR:

- ObserveOnly parity;
- WaitCovered;
- WaitVisible;
- Required/Optional;
- Ready exactly once;
- failure;
- invalidation;
- restart/reentry;
- Route Startup;
- invalid combinations;
- insufficient gate;
- no fabricated progress;
- no post-release re-gating.

## Escopo FIRSTGAME M03

Duas formas de produto:

```text
WaitVisible
  chicken preparation visível
  gameplay gated

WaitCovered
  production-like preparation escondida
  reveal somente após Ready
```

FIRSTGAME permanece happy-path. Falhas ficam no QA.

## Fora de escopo

- Player provisioning;
- recovery button;
- timeout;
- Content Anchors.

## Arquivos prováveis

```text
com.immersive.framework/
  Runtime/Loading/... progress phase/result files
  Runtime/ActivityFlow/... diagnostics snapshot files
  Editor/... Activity inspector/debug files
  Documentation~/Guides/Activity-Readiness-Usage.md

QAFramework/
  Activity readiness entry-policy regression files
  Route Startup readiness regression files
  docs/tracking

planet-devourer/
  Assets/_Project/FrameworkModels/M03_ActivityReadiness/...
```

## Aceite técnico

- compile/import limpos;
- QA generic green;
- no polling;
- no fallback;
- no gate/transition/loading residue;
- restart gera novo occurrence.

## Aceite de produto

- designer configura policy;
- usuário distingue visible preparation de covered preparation;
- painel mostra Preparing e Ready;
- README explica reutilização;
- M03 não usa Player para provar readiness genérico.

## Ganho arquitetural

Fecha a infraestrutura antes de um consumidor complexo.

## Ganho de usabilidade

M03 passa a ensinar a feature oficial descrita pelo ADR.

## Commit sugerido

Package:

```text
Complete Activity entry readiness product diagnostics
```

FIRSTGAME:

```text
Demonstrate visible and covered Activity readiness
```

## Gate de saída

ADR-007 pode ser marcado como Implemented/Validated para o corte atual.

---

# IF-M07-10 — Player readiness contribution e delta reconcile

## Tipo

```text
Técnico / PlayerParticipation runtime
```

## Objetivo

Fazer o lifecycle de Player contribuir com readiness e progredir depois de late join.

## Escopo

- contribuição package-owned;
- ExplicitSlots primeiro;
- Player requirement levels;
- Enter com WaitingForJoin;
- delta reconcile;
- default selection;
- preparation/materialization;
- gameplay admission;
- completion/failure;
- exit waiting/ready;
- reentry;
- idempotência.

## Fora de escopo

- AllJoinedSlots membership dinâmico;
- Session Leave;
- disconnect;
- callback timeout;
- Composer.

## Arquivos prováveis

```text
com.immersive.framework/
  Runtime/PlayerParticipation/Runtime/ActivityPlayerActorLifecycleParticipant.cs
  Runtime/PlayerParticipation/Runtime/ActivityPlayerActorLifecycleParticipant.*.cs
  Runtime/PlayerParticipation/Runtime/PlayerActorPreparationRuntimeHostModule.cs
  Runtime/PlayerParticipation/Runtime/PlayerGameplayRuntimeHostModule.cs
  Runtime/PlayerParticipation/Contracts/... lifecycle/readiness snapshot files
  Runtime/ActivityFlow/... internal contribution contracts/adapters
```

## Fluxo esperado

```text
Explicit Slot unjoined
→ contribution Preparing/WaitingForJoin

Slot joined with stable Host evidence
→ reconcile
→ select/prepare/admit
→ contribution Completed
```

## Smoke técnico

- JoinedSlots;
- SelectedActors;
- LogicalActorsPrepared;
- GameplayReady;
- unjoined explicit slot;
- invalid Host;
- materialization failure;
- no-op;
- revision changes during reconcile;
- exit while waiting;
- exit after ready;
- reentry.

## Aceite técnico

- expected waiting is not blocking failure;
- actual failures are terminal and explicit;
- one Actor per Slot/occurrence;
- exact owner/token release;
- rollback only for newly applied delta;
- no runtime reflection.

## Aceite de produto

Nenhuma nova configuração além de Activity participation + entry readiness policy.

## Ganho arquitetural

Player usa readiness genérico em vez de lifecycle paralelo.

## Ganho de usabilidade

Join tardio passa a progredir a Activity sem botão técnico.

## Commit sugerido

```text
Integrate Player lifecycle with Activity readiness
```

## Gate de saída

Internal QA alcança Ready por late join e libera corretamente no exit.

---

# IF-M07-11 — Stable notifications e reconcile automático

## Tipo

```text
Técnico / host-scoped orchestration
```

## Objetivo

Disparar reconcile somente depois de mudanças estáveis da Session.

## Escopo

- post-commit join notification;
- post-commit selection notification;
- host-scoped coordinator;
- serialized reconcile;
- revision coalescing;
- stale occurrence rejection;
- immutable snapshot;
- no active Activity no-op diagnostic;
- control-plane Join permitido durante readiness gate.

## Fora de escopo

- manual public retry;
- Session Leave;
- global event bus;
- cross-scene channel genérico.

## Arquivos prováveis

```text
com.immersive.framework/
  Runtime/PlayerParticipation/Runtime/LocalPlayerProvisioningRuntimeHostModule.cs
  Runtime/PlayerParticipation/Runtime/LocalPlayerProvisioningBridge.cs
  Runtime/PlayerParticipation/Runtime/PlayerActorPreparationRuntimeHostModule.cs
  Runtime/PlayerParticipation/Runtime/PlayerActivityReconciliationRuntimeHostModule.cs
  Runtime/ApplicationLifecycle/FrameworkRuntimeHost*.cs
  Runtime/GameFlow/... scoped Player integration files
```

## Limite transacional

Notificação de join somente após:

```text
Slot Joined
assignment committed
Host admission committed
Host evidence registered
```

## Smoke técnico

- change before commit não é publicado;
- one stable join = one effective reconcile;
- multiple revisions coalesced;
- occurrence replaced before execution;
- transition conflict;
- repeated no-change;
- join UI/control request enquanto gate está retido.

## Aceite técnico

- sem race entre Slot Joined e Host evidence;
- sem duplicate Actor;
- coordinator não é singleton;
- owner/lifetime explícitos;
- diagnostics mostram revision requested/applied.

## Aceite de produto

Public `RequestJoin` é suficiente para iniciar progressão.

## Ganho arquitetural

Estabelece limite transacional correto entre Session e Activity.

## Ganho de usabilidade

O usuário não precisa conhecer preparation/reconcile internals.

## Commit sugerido

```text
Reconcile active Activity after stable Player changes
```

## Gate de saída

Public join produz auto-reconcile determinístico no package.

---

# IF-M07-12 — QA public-only e diagnostics públicos

## Tipo

```text
QA técnico + prova de alcance de produto
```

## Objetivo

Provar que um consumidor normal alcança o happy path sem reflection/internal access.

## Superfície permitida

```text
GameApplicationAsset
RouteAsset
ActivityAsset
ActivityEntryReadinessPolicy
PlayerSlotProfile
ActorProfile
LocalPlayerProvisioningAuthoring.RequestJoin
public readiness events/snapshots
public join/participation diagnostics
```

## Proibido

```text
reflection
InternalsVisibleTo como caminho principal
manual RuntimeScopeContext
direct TryPrepareSelectedActor
direct TryEnsureCurrentGameplay
external Slot mutation
consumer Destroy
```

## Happy path

```text
WaitVisible Activity active
→ public status = Preparing / WaitingForJoin
→ public RequestJoin
→ Host
→ Slot Joined
→ Actor under ActorMount
→ LogicalActorsPrepared
→ GameplayReady when configured
→ readiness Ready
→ gate released
→ exit releases Actor/context
→ Host/Slot Session state preserved
→ reentry creates one Actor
```

## Negativos QA

- joining closed;
- capacity;
- Host invalid;
- stale occurrence;
- required preparation failure;
- repeated reconcile notification;
- exit during waiting;
- replace during reconcile;
- gate release failure;
- join control unavailable under retained gate;
- duplicate Actor prevention.

## Arquivos prováveis

```text
QAFramework/
  Assets/ImmersiveFrameworkQA/Player/Editor/
    QaManagerProvisionedReadinessPublicSurfaceRegression.cs
    QaManagerProvisionedReadinessNegativeRegression.cs
  documentation/tracking files
```

## Aceite técnico

- suite public-only não resolve tipos internal;
- package internal suite continua cobrindo detalhes;
- logs/snapshots correlacionam occurrence e Slot;
- cleanup deixa zero Actor contextual residual.

## Aceite de produto

A mesma sequência poderá ser reproduzida no FIRSTGAME.

## Commit sugerido

```text
Prove Manager-Provisioned readiness through public APIs
```

## Gate de saída

QA público alcança Ready e release sem bypass.

---

# IF-M07-13 — FIRSTGAME M07 real

## Tipo

```text
UX/produto + integração real
```

## Objetivo

Demonstrar Manager-Provisioned Player como consumidor de Activity Entry Readiness.

## Configuração recomendada

```text
Activity
  Participation Projection = ExplicitSlots
  Requirement = LogicalActorsPrepared
    ou GameplayReady quando input/camera authoring estiver claro
  Entry Readiness Policy = WaitVisible

Persistent composition
  PlayerInputManager
  LocalPlayerProvisioningAuthoring
  LocalPlayerProvisioningHostRegistration

Runtime Player prefab
  PlayerInput
  LocalPlayerHostAuthoring
  empty ActorMount

ActorProfile
  explicit Actor prefab
```

Não adicionar `ActivityReadinessParticipant` manual apenas para representar Player. A contribuição vem do package.

## Fluxo

```text
Boot
→ Activity authority active
→ target visible
→ status Waiting for Join
→ normal gameplay blocked
→ persistent Join control disponível
→ RequestJoin
→ Host appears
→ Slot Joined
→ Actor materializes
→ Ready
→ gameplay released
→ exit
→ Actor/context released
→ Session Host preserved
→ reenter
→ one Actor rematerialized
```

## Status mínimos

```text
Entry policy
Activity authority
Readiness: Preparing/Ready
Readiness reason
Gate held/released
Joining open
Host count
Slot state
Selected Actor
Actor preparation/materialization
Gameplay admission
```

## Fora de escopo

- fault injection;
- invalid prefabs;
- Session Leave;
- M08 policies;
- generic command channel promotion;
- manual prepare button.

## Arquivos prováveis

```text
planet-devourer/
  Assets/_Project/FrameworkModels/M07_ManagerProvisionedPlayer/
    Application/
    Routes/
    Activities/
    Profiles/
    Scenes/
    Prefabs/
    Scripts/
    README.md
```

Migrar o M07 já existente; não duplicar assets.

## Aceite técnico

- somente APIs públicas;
- no reflection;
- no FindObjectOfType authority;
- no manual Slot mutation;
- no Destroy repair;
- exit/reentry sem duplicação;
- Console sem error.

## Aceite de produto

- designer entende Host, Slot e Actor;
- Waiting for Join é visível e não parece falha;
- Join funciona com gameplay gated;
- Inspector/README explicam por que WaitVisible foi escolhido;
- M07 não ensina M08.

## Ganho arquitetural

FIRSTGAME prova a integração real entre duas features oficiais.

## Ganho de usabilidade

O fluxo descrito pelo modelo finalmente corresponde ao comportamento do package.

## Commit sugerido

```text
Complete M07 with Activity readiness integration
```

## Gate de saída

M07 pode ser marcado `Closed` no FIRSTGAME.

---

# IF-M07-14 — Manager-Provisioned authoring e documentação

## Tipo

```text
Produto + editor tooling + docs
```

## Objetivo

Reduzir montagem técnica repetitiva sem misturar Activity policy com provisioning.

## Shape recomendado

```text
ManagerProvisionedPlayerRecipe
  Local Player Host Prefab
  Initial Capacity
  Initial Joining State

ManagerProvisionedPlayerComposer
  Recipe
  PlayerInputManager
  Apply / Rebuild
  Validate

Advanced / Debug
  LocalPlayerProvisioningAuthoring
  LocalPlayerProvisioningHostRegistration
  LocalPlayerActorSelectionRequestAuthoring
  runtime snapshots
```

A Activity continua authorando sua própria:

```text
Participation
Entry Readiness Policy
Transition
Gate
```

O Composer pode validar compatibilidade quando referências explícitas forem fornecidas, mas não deve tomar autoridade sobre a Activity.

## Escopo

- Recipe;
- Composer;
- idempotent Apply/Rebuild;
- create menu/template;
- embedded validation;
- Advanced/Debug;
- docs separando join, selection, preparation, readiness, Activity release e Session Leave.

## Fora de escopo

- gameplay;
- movement;
- Session Leave;
- global demo manager;
- automatic Route/Activity generation.

## Arquivos prováveis

```text
com.immersive.framework/
  Runtime/PlayerParticipation/Authoring/ManagerProvisionedPlayerRecipe.cs
  Runtime/PlayerParticipation/Authoring/ManagerProvisionedPlayerComposer.cs
  Editor/PlayerParticipation/ManagerProvisionedPlayerComposerEditor.cs
  Editor/PlayerParticipation/ManagerProvisionedPlayerComposerUtility.cs
  Editor/PlayerParticipation/ManagerProvisionedPlayerCreationMenus.cs
  Documentation~/Guides/Manager-Provisioned-Player-Usage.md
  Documentation~/Guides/Player-Usage.md

planet-devourer/
  M07 README and composition migration
```

## Aceite técnico

- Apply/Rebuild idempotente;
- sem scene mutation em Play Mode;
- não destrutivo;
- undo/dirty/prefab handling corretos;
- components técnicos visíveis em Advanced;
- validator não cria fallback.

## Aceite de produto

- usuário cria e configura o host sem conhecer módulos internos;
- Activity policy permanece claramente separada;
- documentação apresenta o fluxo completo.

## Commit sugerido

```text
Add Manager-Provisioned Player authoring workflow
```

## Gate de saída

Capacidade de produto authorável fechada.

---

# IF-M07-15 — Provisioning hardening

## Tipo

```text
Técnico + QA negativo
```

## Objetivo

Fechar callback pending e semântica de requests repetidos após o happy path estar estável.

## Escopo

- callback confirmation policy;
- expiry/watchdog determinístico ou decisão formal de callback opcional;
- late callback;
- divergent callback;
- unexpected callback;
- in-flight request;
- second Player request;
- capacity/no Slot;
- cleanup.

## Fora de escopo

- Session Leave;
- disconnect/reconnect;
- mudanças no readiness contract.

## Aceite técnico

- awaiting confirmation não fica retido sem diagnóstico;
- stale callback não confirma operação nova;
- timeout não destrói Player admitido sem política explícita;
- repeated RequestJoin continua significando tentativa de novo Player;
- diagnostics distinguem duplicate operation de additional Player.

## Commit sugerido

```text
Harden Local Player join confirmation semantics
```

---

# IF-M07-16 — Session Player Leave

## Tipo

```text
Novo capability separado
```

## Estado

```text
Deferred
```

## Abrir somente quando um fluxo real exigir:

```text
Player leaves the Session
```

## Transação futura

```text
release contextual gameplay/Actor
→ release Host evidence
→ release assignment
→ Slot Leaving → Available
→ remove physical PlayerInput Host
→ allow rejoin
```

Não usar join rollback como Leave e não executar Leave implicitamente em Activity exit.

---

# 8. Ordem de implementação

```text
IF-READY-01  aggregate semantics
    ↓
IF-READY-02  policy authoring/validation
    ↓
IF-READY-03  occurrence waiter
    ↓
IF-READY-04  GameFlow reveal/gate orchestration
    ↓
IF-READY-05  loading/diagnostics/QA/M03
    ↓
IF-M07-10    Player contribution and delta reconcile
    ↓
IF-M07-11    stable notifications and automatic reconcile
    ↓
IF-M07-12    public-only QA
    ↓
IF-M07-13    FIRSTGAME M07
    ↓
IF-M07-14    Recipe/Composer/docs
    ↓
IF-M07-15    provisioning hardening

IF-M07-16 Session Leave
    separate/deferred
```

---

# 9. Merge gates

## Gate A — readiness semantics

Após `IF-READY-01`:

- Preparing não é failure;
- aggregate pode transicionar para Ready;
- Optional não bloqueia;
- existing readiness tests verdes.

## Gate B — policy contract

Após `IF-READY-02`:

- policy authoring existe;
- default ObserveOnly;
- invalid combinations bloqueiam;
- nenhum fallback.

## Gate C — generic runtime

Após `IF-READY-04`:

- all policies work;
- gate/transition/loading order correct;
- occurrence terminal correct;
- failure recovery path available.

## Gate D — readiness product

Após `IF-READY-05`:

- QA generic green;
- M03 proves WaitVisible and WaitCovered;
- ADR-007 current scope validated.

## Gate E — Player runtime

Após `IF-M07-11`:

- late join automatically reconciles;
- Player contribution drives generic readiness;
- no duplicate Actor;
- public RequestJoin sufficient.

## Gate F — consumer reachability

Após `IF-M07-12`:

- public-only QA reaches Ready/release;
- no reflection/internal bypass.

## Gate G — FIRSTGAME closure

Após `IF-M07-13`:

- M07 happy path and reentry proven;
- WaitVisible and persistent Join control are comprehensible.

## Gate H — product release

Após `IF-M07-14` e `IF-M07-15`:

- authoring flow complete;
- docs corrected;
- callback/repeat semantics hardened.

---

# 10. Responsabilidade por repositório

| Repositório | Responsabilidade |
|---|---|
| `com.immersive.framework` | Policy, readiness aggregation, occurrence wait, GameFlow gating, Player contribution/reconcile, public diagnostics, authoring tooling e docs. |
| `QAFramework` | Contratos, terminais, ordering, negativos, public-only reachability e regressões. |
| `planet-devourer` | M03 e M07 como experiências reais, happy path, UX findings e reutilização. |

FIRSTGAME não implementa:

```text
readiness authority
reconcile
gate retention
Actor preparation
failure injection
```

QA não define a solução oficial.

---

# 11. Ordem de testes

| Ordem | Área | Prova |
|---:|---|---|
| 1 | Package pure/runtime | Readiness aggregate, occurrence wait e policy state machine. |
| 2 | QA generic | ObserveOnly, WaitVisible, WaitCovered, Required/Optional, failure/invalidation. |
| 3 | FIRSTGAME M03 | UX real das duas waiting policies. |
| 4 | Package Player | Contribution, delta reconcile, stable notifications. |
| 5 | QA Player internal | Revisions, owner, rollback, exit/reentry. |
| 6 | QA Player public-only | Public join → Ready → release. |
| 7 | FIRSTGAME M07 | Jornada manual real e UX. |
| 8 | QA hardening | Callback, repeated join, stale/capacity. |

---

# 12. Critério de fechamento do readiness genérico

## Técnico

- `ObserveOnly`, `WaitVisible`, `WaitCovered`;
- occurrence-scoped wait;
- Required/Optional correct;
- terminal failure explicit;
- invalidation/cancellation;
- Route Startup;
- Loading `PreparingActivity`;
- no fabricated progress;
- no stale gate/cover;
- no post-release re-gating;
- recovery request possible.

## Produto

- policy designer-first;
- inline validation;
- Advanced/Debug;
- M03 demonstrates both shapes;
- short usage documentation;
- no logs required to understand Preparing versus Failed.

---

# 13. Critério de fechamento do M07

## Técnico

```text
public RequestJoin
→ stable Host/Slot commit
→ automatic active occurrence reconcile
→ Actor selected/prepared/materialized
→ required gameplay evidence
→ readiness Ready
→ gate release
→ contextual exit release
→ reentry without duplication
```

Também:

- no reflection in consumer path;
- no internal API in public-only QA;
- no RuntimeScopeContext exposed;
- exact tokens/owners;
- failure explicit;
- Session Host preservation documented.

## Produto

- Manager-Provisioned composition authorable;
- WaitVisible shows Waiting for Join;
- Join control remains available while gameplay is gated;
- status panel explains stages;
- Composer/Apply/Rebuild;
- Advanced/Debug;
- README and reusable prefab;
- M08 remains separate.

---

# 14. O que não fazer

- Não criar `ActivityReadinessStatus.Waiting`.
- Não criar `ActivityContentExecutionStatus.PendingReadiness`.
- Não criar `ActivityPlayerParticipationEntryMode`.
- Não tratar Required Preparing como blocking failure.
- Não usar readiness events como command path.
- Não fazer polling.
- Não permitir Loading/Transition decidir readiness.
- Não ocultar gate strengthening silencioso.
- Não bloquear o control plane necessário ao Join.
- Não expor `TryPrepareSelectedActor`.
- Não re-requestar a mesma Activity como pseudo-reconcile.
- Não adicionar botão “Prepare Actor” ao FIRSTGAME.
- Não promover o channel M07 ao package sem segundo caso transversal.
- Não misturar Session Leave com Activity exit.
- Não avançar para M08 antes do M07 público funcionar.

---

# 15. Primeiro ZIP recomendado

```text
IF-READY-01-readiness-semantics.zip
```

## Conteúdo

```text
Runtime/ActivityFlow/
  ActivityReadinessState.cs
  ActivityReadinessOccurrenceState.cs
  ActivityReadinessRecomposer.cs
  ActivityReadinessParticipant.cs
  ActivityReadinessUpdate.cs
  arquivos de testes/contracts diretamente dependentes

CHANGESET.md
```

## Objetivo do ZIP

```text
Required Preparing
  deixa de ser tratado como failure

Activity aggregate
  passa de NotReady/Preparing para Ready

Required Failed
  permanece terminal e blocking

Optional
  permanece diagnóstico
```

## Não incluir ainda

```text
ActivityEntryReadinessPolicy
GameFlow wait
Transition/Loading changes
Player changes
FIRSTGAME changes
```

Isso mantém o primeiro patch pequeno, verificável e reutilizável, sem expor uma policy de produto antes de o runtime estar pronto para consumi-la.

---

# 16. Fontes

- `IF-ADR-007 — Activity Entry Readiness and Reveal Gating`
- `IMMERSIVE-FRAMEWORK-M07-CANONICAL-CAPABILITY-MATRIX-2026-08-02.md`
- `IMMERSIVE-FRAMEWORK-FEATURE-CAPABILITY-AUDIT-AND-TEST-ORDER-2026-07-30-v3.md`
- `IMMERSIVE-FRAMEWORK-FIRSTGAME-DEMONSTRATION-MODELS-BUILD-GUIDE-2026-07-30.md`
- verificação dirigida dos contracts atuais de ActivityFlow, GameFlow e PlayerParticipation no baseline auditado

---

# 17. Conclusão

A direção consolidada é:

```text
implementar ADR-007 como feature genérica
→ provar WaitVisible e WaitCovered no M03
→ fazer Player publicar contribuição de readiness
→ reconciliar late join automaticamente
→ provar o caminho público no QA
→ fechar o M07 no FIRSTGAME
```

O M07 deixa de ser o lugar onde o framework inventa “waiting”. Ele passa a ser o primeiro consumidor complexo de uma capacidade oficial de Activity Entry Readiness.
