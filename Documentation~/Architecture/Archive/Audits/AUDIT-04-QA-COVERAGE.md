# AUDIT-04 — Cobertura real do QAFramework

**Escopo:** Activity Readiness, Player Participation, Actor Preparation/Materialization, Late Join/Reconciliation, WaitCovered, Loading Progress e occurrence isolation.  
**Base de contexto:** `AUDIT-01-ACTIVITY-READINESS.md`, `AUDIT-02-PLAYER-LATE-JOIN.md`, `AUDIT-03-WAITCOVERED-LOADING-GATES.md`.  
**Método:** revisão das evidências locais disponíveis de QA — relatórios de auditoria, trechos de fonte e logs de execução. O diretório-fonte completo atual do `QAFramework` não está montado neste ambiente; portanto, nenhuma conclusão depende de assumir conteúdo de `.cs` que não apareça nas evidências locais.

## 1. Resumo

- **IF-READY-04-001:** O QA prova de forma forte o comportamento genérico de `WaitVisible` e `WaitCovered` em Activity entry. A execução observada de `QaDirectActivityReadinessPoliciesRegression` terminou `Passed` com 42 casos e demonstra request pendente enquanto readiness está `Preparing`, retenção de presentation/gate, conclusão de readiness e liberação posterior.
- **IF-READY-04-002:** Failure terminal de readiness está bem coberto. `QaParticipantAwareReadinessLoadingTerminalRegression` terminou `Passed` com 34 casos e prova `RequiredFailed`, `RequiredReleased`, cancellation, replacement, late old occurrence e terminal duplicado, incluindo ausência de progresso terminal de sucesso.
- **IF-READY-04-003:** A capacidade técnica de Player Join, Actor preparation, Actor materialization, gameplay admission e release possui evidência extensa em `QaPlayerGameplayAdmissionRegression`, que terminou `Passed` com 114 casos.
- **IF-READY-04-004:** Essa prova antiga de Player não é equivalente à jornada pública atual. A fixture usa acesso interno/reflection para alcançar preparação e gameplay; portanto, ela prova a autoridade técnica, mas não prova que um consumidor normal chega ao mesmo resultado apenas por `RequestJoin` + lifecycle oficial.
- **IF-READY-04-005:** Existe um QA específico mais recente, `QaM07InternalReconcileRegression`, que exercita `WaitingForJoin`, revision coalescing, reconcile, Actor replacement, múltiplos Slots, idempotência, exit e reentry. A execução local mais avançada encontrada chegou a **53/54 casos**, tendo concluído as assertions centrais de late join/reconcile, mas terminou `Failed` durante a verificação de cleanup/evidência de readiness. Por isso esse fluxo é **PARTIALLY PROVEN**, não `PROVEN`.
- **IF-READY-04-006:** Não foi encontrada evidência local de uma execução verde de QA **public-only** para `Activity ativa → RequestJoin → automatic reconcile → Actor materialized → same occurrence Ready`. Esse contrato permanece **NOT PROVEN** no material disponível.
- **IF-READY-04-007:** A prova positiva específica de Loading participant-aware com vários Required e Optional é esperada por `QaParticipantAwareReadinessLoadingProgressRegression`, mas não foi localizada uma execução `Passed` correspondente. Failure/no-100% está provado; progressão positiva Required/Optional fica **PARTIALLY PROVEN**.
- **IF-READY-04-008:** Occurrence replacement e stale evidence possuem cobertura forte no nível genérico: o terminal regression rejeita replacement/late old occurrence e a regressão IF-ID posterior prova `readiness-collision-isolation` e `legitimate-supersession-preservation`.
- **IF-READY-04-009:** O cenário “Player nunca entra” possui evidência de `Preparing / WaitingForJoin` e exit seguro enquanto ainda waiting, mas não foi localizado um caso dedicado que prove, pela superfície pública, que o estado permanece legitimamente pendente sem falso `Ready` durante um `WaitCovered`.
- **IF-READY-04-010:** A cobertura atual é tecnicamente significativa, mas ainda fragmentada entre regressões genéricas, regressões Player internas e smokes manuais. Para o problema investigado, o principal gap não é falta de teste de `WaitCovered`; é falta de uma prova verde, consumer-equivalent, que combine **Player late join + same occurrence reconcile + readiness + gate/reveal**.

### Classificação geral

| Área | Cobertura |
|---|---|
| Activity Readiness genérico | **PROVEN** |
| Required failure/release | **PROVEN** |
| Optional non-blocking / denominator | **PARTIALLY PROVEN** |
| WaitVisible | **PROVEN** |
| WaitCovered | **PROVEN** |
| Loading sem 100% em failure | **PROVEN** |
| Loading participant-aware positivo | **PARTIALLY PROVEN** |
| Player Join técnico | **PROVEN** |
| Actor preparation/materialization técnico | **PROVEN** |
| Late Join + reconcile interno | **PARTIALLY PROVEN** |
| Late Join + reconcile public-only | **NOT PROVEN** |
| No duplicate Actor durante reconcile | **PARTIALLY PROVEN** |
| Exit/reentry após reconcile | **PARTIALLY PROVEN** |
| Player nunca entra | **PARTIALLY PROVEN** |
| Generic occurrence replacement/stale rejection | **PROVEN** |
| Player reconcile stale occurrence | **PARTIALLY PROVEN** |

---

## 2. Testes relevantes encontrados

### 2.1 `QaDirectActivityReadinessPoliciesRegression`

**Área:** GameFlow / Activity Entry Readiness.

Evidência de execução observada:

```text
[IF_READY_04_QA_DIRECT_POLICIES]
status='Passed'
cases='42'
waitVisible='Passed'
waitCovered='Passed'
```

Casos concluídos relevantes incluem:

```text
wait-visible-participant-preparing
wait-visible-request-pending-after-release
wait-visible-gate-retained-after-reveal
wait-visible-readiness-completed-through-public-api
wait-visible-request-succeeded
wait-visible-ready-authority-confirmed
wait-visible-gate-released

wait-covered-participant-preparing
wait-covered-presentation-retained-before-ready
wait-covered-request-pending-before-ready
wait-covered-gate-retained-while-covered
wait-covered-readiness-completed-through-public-api
wait-covered-request-succeeded
wait-covered-ready-authority-confirmed
wait-covered-presentation-released-after-ready
wait-covered-presentation-order-confirmed
wait-covered-gate-released
```

#### O que configura

Uma Activity temporária, participant de readiness, policy `WaitVisible` ou `WaitCovered`, surfaces host-owned e gate real.

#### O que executa

Inicia uma Activity request enquanto o participant está `Preparing`, observa o comportamento antes de `Ready`, completa o participant pela API de readiness e aguarda o request terminal.

#### O que realmente prova

- `WaitVisible` revela antes de Ready e mantém gate.
- `WaitCovered` mantém presentation coberta antes de Ready.
- O request permanece pendente durante readiness.
- O gate permanece retido.
- Completar readiness permite o request terminar.
- `WaitCovered` só libera presentation após Ready.
- Gate é liberado depois da conclusão.
- Cleanup/restauração do fixture é executado.

#### O que não prova

- Player Join.
- `LogicalActorsPrepared`.
- Actor preparation/materialization.
- Late join.
- Reconciliation.
- Slot explícito não Joined.
- Join command durante o gate.
- Vários Required/Optional.
- Failure de Player preparation.

**Cobertura atribuída:** `PROVEN` para policies genéricas; não é prova de Player late join.

---

### 2.2 `QaParticipantAwareReadinessLoadingTerminalRegression`

**Área:** Activity Readiness / Loading / terminais.

Evidência observada:

```text
[QA_READY_PROGRESS_02A]
status='Passed'
cases='34'
runtimePath='DirectActivityRequiredFailure'
contractPaths='DirectActivity,RouteStartupActivity,GameApplicationStartupActivity'
terminals='RequiredFailed,RequiredReleased,ReplacementRejected,
LateOldOccurrenceRejected,DuplicateTerminal,OwnedCancellation'
```

Casos concluídos relevantes:

```text
direct-envelope-required-failure
route-envelope-required-failure
game-application-envelope-required-failure
required-release-terminal-confirmed
replacement-occurrence-rejected
late-old-occurrence-rejected
duplicate-terminal-idempotent
owned-cancellation-terminal
direct-required-failed
direct-terminal-result-typed
direct-destination-authoritative
direct-terminal-snapshot-confirmed
direct-last-progress-below-one
direct-no-terminal-progress-update
direct-loading-retained
direct-transition-retained
direct-recovery-gate-retained
```

#### O que configura

Readiness occurrence com participante Required e envelope de Loading/entry readiness.

#### O que executa

Failure, release prematuro, cancellation, replacement e stale completion.

#### O que realmente prova

- Required failure é terminal.
- Required release não vira sucesso.
- Failure não publica terminal 100%.
- Loading permanece abaixo de 1.0.
- Destination authority é preservada.
- Recovery gate é retido.
- Occurrence substituída não é aceita.
- Late completion de occurrence antiga é rejeitada.
- Terminal duplicado é idempotente.
- Cancellation tem resultado tipado.
- Há paridade contratual para Direct Activity, Route Startup e Game Application Startup nas paths verificadas.

#### O que não prova

- Player-specific readiness failure end-to-end.
- Late Join.
- Actor materialization.
- Optional failure.
- Progressão positiva de múltiplos Required até 100%.

**Cobertura atribuída:** `PROVEN` para failure/release/cancellation/stale occurrence no contrato genérico.

---

### 2.3 `QaParticipantAwareReadinessLoadingProgressRegression`

**Área:** Loading participant-aware positivo.

A documentação local descreve esse runner com:

```text
4 Required
1 Optional
0/4 → 1/4 → 2/4 → 3/4 → 4/4 + Ready
Optional pending sem alterar denominator
Optional failed sem alterar denominator
100% antes de Hide
Hide antes de reveal
```

#### Evidência observada

Não foi localizada, entre os arquivos locais consultados, uma saída equivalente a:

```text
status='Passed'
```

para esse runner.

#### Consequência

A existência/planejamento do teste não é suficiente para classificar o contrato como provado.

**Cobertura atribuída:** `PARTIALLY PROVEN`.

---

### 2.4 `QaActivityReadinessPostTransitionSmoke`

**Área:** Activity Readiness pós-entry.

O trecho de fonte local mostra três assertions centrais:

```text
Ready → NotReady
NotReady → Ready
IdenticalValueIgnored
```

Também compara a mesma occurrence em:

```text
ActivityFlow
RouteLifecycle
GameFlow
FrameworkRuntimeHost.State
FrameworkRuntimeHost.SessionState
```

e exige que a mudança de readiness não faça nova Route/Activity request.

#### O que realmente prova

- readiness da occurrence atual pode mudar após a transição;
- o mesmo estado é refletido pelas camadas runtime;
- update idêntico não altera estado;
- readiness update não é tratado como navigation request.

#### O que não prova

- participant capture;
- Required/Optional;
- Player contribution;
- WaitingForJoin;
- reconciliation;
- WaitCovered;
- occurrence replacement.

**Cobertura atribuída:** `PROVEN` para propagação do estado da occurrence atual; complementar, não suficiente sozinho.

---

### 2.5 `QaPlayerGameplayAdmissionRegression`

**Área:** Player / Actor / Gameplay admission.

Evidência observada:

```text
[PLAYER_GAMEPLAY_ADMISSION_REGRESSION]
status='Passed'
cases='114'
```

Casos relevantes incluem:

```text
official-requestjoin-created-materialized-host
real-local-player-joined
stable-player-host-preserved
technical-host-parented
public-default-actor-selection
current-activity-scope-authoritative
current-actor-prepared
current-gameplayready-authoritative
current-input-gate-runtime-bound
manager-input-correlated-to-current-actor
...
adopted-activity-clear-succeeded
activity-clear-releases-gameplay-chain
activity-clear-releases-input-binding
target-actor-released-after-gameplay
target-physical-actor-destroyed
stable-session-player-survives-activity-exit
activity-clear-preserves-assignment
activity-clear-preserves-host
```

#### O que configura

Player provisioning, Slot, Host, Activity scope, Actor selection/preparation e gameplay capabilities.

#### O que executa

Join, preparação, gameplay admission, input/camera checks, route/activity lifecycle e release.

#### O que realmente prova

- `RequestJoin` técnico funciona.
- Host físico é criado/admitido.
- Slot fica Joined.
- Actor pode ser preparado.
- GameplayReady pode ser alcançado.
- Input/camera têm contratos de correlação/release.
- Actor físico é destruído no Activity clear.
- Host/assignment Session-scoped sobrevivem ao Activity exit.

#### Limitação crítica

As evidências locais da auditoria anterior mostram que a fixture usa reflection/internal access para alcançar operações como:

```text
CreateScopeRoot
TryCreateScopeContext
TryPrepareSelectedActor
TryEnsureCurrentGameplay
```

Portanto:

> Este runner prova que as autoridades internas conseguem executar o pipeline.  
> Ele não prova que o runtime atual realiza automaticamente esse pipeline após um late join usando somente a superfície pública disponível a um consumidor.

**Cobertura atribuída:** `PROVEN` para capacidade técnica interna; `NOT PROVEN` como consumer-equivalent late-join flow.

---

### 2.6 `QaM07InternalReconcileRegression`

**Área:** Player late join / delta reconcile / lifecycle.

Este é o runner mais próximo do contrato de `AUDIT-02`.

A execução local mais avançada encontrada terminou:

```text
[QA_M07_INTERNAL]
status='Failed'
cases='53/54'
next='joining-closed'
```

Entretanto, antes do failure final, foram concluídos:

```text
waiting-entry-succeeded
waiting-lifecycle-preparing
waiting-player-contribution-preparing
waiting-exit-succeeded
waiting-exit-released-contribution
waiting-exit-preserved-session

rollback-reconcile-failed-preparation
rollback-delta-reverted
rollback-request-terminal-not-ready

main-request-started
main-participants-preparing
exact-owner-rejections-proved
pre-delta-no-change-proved

first-slot-progressed
revision-coalescing-proved
replacement-selection-applied
replacement-reconcile-proved
second-player-joined
main-reconcile-completed
main-request-succeeded
one-actor-per-slot-proved
completed-no-change-proved

ready-exit-succeeded
ready-exit-released-context
session-authority-preserved

reentry-first-request-succeeded
reentry-first-exit-succeeded
reentry-second-request-succeeded
reentry-occurrence-advanced
reentry-actors-renewed
reentry-cleared
```

O failure final foi associado à evidência/cleanup de readiness participant:

```text
Invalid readiness participant evidence before surface destruction.
started='2'
released='2'
occurrence='13'
```

#### O que realmente prova

A execução chegou muito além de um smoke nominal. Os casos concluídos fornecem evidência de que o runner conseguiu observar:

- Activity ativa em `Preparing`;
- Player readiness contribution em `Preparing`;
- exit durante waiting;
- release da contribuição;
- preservation de Session;
- preparation failure + rollback;
- delta/no-change;
- revision coalescing;
- Player progress;
- reconcile;
- segundo Join;
- request concluído;
- um Actor por Slot;
- idempotência após completion;
- release after Ready;
- reentry em nova occurrence;
- Actor renovado em reentry.

#### Por que não recebe `PROVEN`

O runner, como unidade de regressão, não terminou verde. A regra desta auditoria é não transformar “quase passou” em prova fechada.

Além disso, trata-se de QA interno. Ele não substitui o smoke public-only.

**Cobertura atribuída:** `PARTIALLY PROVEN`.

---

### 2.7 `QaRouteActivityIdentityRegression`

**Área:** identity/ownership/readiness isolation.

Execução posterior encontrada:

```text
[IF_ID_QA]
status='Passed'
executed='6'
completed='6'
failed='0'
```

Inclui:

```text
activity-collision-transition
ownership-release-isolation
readiness-collision-isolation
legitimate-supersession-preservation
```

#### O que realmente prova

- definitions distintas não colapsam por stable ID;
- ownership release permanece isolado;
- readiness de Activities colididas permanece isolada;
- supersession legítima continua funcionando.

#### O que não prova

- Player late join;
- Actor lifecycle;
- WaitCovered;
- Loading progress.

**Cobertura atribuída:** `PROVEN` como evidência complementar de occurrence/identity isolation.

---

## 3. Matriz contrato × teste

| Contrato | Evidência QA principal | Status | O que está realmente provado | Principal ausência |
|---|---|---|---|---|
| Activity Readiness current occurrence | `QaActivityReadinessPostTransitionSmoke`, QA-03 | **PROVEN** | atualização/propagação da occurrence atual | participant semantics completos |
| Required blocks while Preparing | QA-03, Q3 M07 | **PROVEN** | request permanece pending e gate retido | public Player combination |
| Required failure terminal | `QaParticipantAwareReadinessLoadingTerminalRegression` | **PROVEN** | failure typed, no 100%, recovery retained | Player-specific public failure |
| Required release terminal | terminal regression | **PROVEN** | release impede sucesso | — |
| Optional não bloqueia | positive progress runner especificado | **PARTIALLY PROVEN** | contrato esperado está definido | execução verde local não localizada |
| Frozen participant set | terminal/stale tests + audits | **PARTIALLY PROVEN** | stale occurrence rejeitada | positive denominator run não confirmado |
| `LogicalActorsPrepared` | Player regression + Q3 internal | **PARTIALLY PROVEN** | estado é alcançável internamente | public-only late join |
| Player Join | Player regression | **PROVEN** | Host + Slot Joined | não implica Activity progression |
| Actor preparation | Player regression | **PROVEN** técnico | Actor preparado sob scope válido | consumer-equivalent orchestration |
| Actor materialization | Player regression | **PROVEN** técnico | physical Actor criado/released | consumer-equivalent orchestration |
| Late Join | Q3 internal | **PARTIALLY PROVEN** | core cases executados | runner não terminou verde |
| Reconcile same occurrence | Q3 internal | **PARTIALLY PROVEN** | reconcile e main request succeeded nos casos concluídos | full green + public-only |
| Revision coalescing | Q3 internal | **PARTIALLY PROVEN** | assertion concluída | runner global falhou |
| One Actor per Slot | Q3 internal | **PARTIALLY PROVEN** | assertion concluída | runner global falhou |
| Reentry new occurrence | Q3 internal | **PARTIALLY PROVEN** | occurrence avançou e Actors renovaram | runner global falhou |
| WaitVisible | QA-03 | **PROVEN** | reveal antes de Ready + gate retained | Player Join sob esse gate |
| WaitCovered | QA-03 | **PROVEN** | cover/request/gate retidos até Ready | Player Join sob esse gate |
| Loading terminal 100% requires Ready | terminal regression + QA-03 ordering | **PROVEN** negativo | no success progress em failure | positive multi-participant run não confirmado |
| Multi-Required progress | positive progress runner | **PARTIALLY PROVEN** | shape do teste existe | execução green não localizada |
| Cancellation | terminal regression | **PROVEN** | owned cancellation terminal | — |
| Invalidation/replacement | terminal regression | **PROVEN** | replacement/late old occurrence rejected | Player reconcile mid-flight |
| Duplicate terminal | terminal regression | **PROVEN** | idempotência | — |
| Generic stale occurrence isolation | terminal + IF-ID | **PROVEN** | stale/collision/supersession isolados | — |
| Player stale reconcile | Q3 internal | **PARTIALLY PROVEN** | exact-owner checks existem | full successful regression |
| Gate release on Ready | QA-03 | **PROVEN** | release depois de Ready | join control availability |
| Gate remains safe on failure | terminal regression | **PROVEN** | recovery gate retained | remediation flow |
| Join control available while waiting gate held | nenhum resultado green localizado | **NOT PROVEN** | — | caso crítico para o problema real |
| Public-only late join → Ready | nenhum resultado green localizado | **NOT PROVEN** | — | principal gap |
| Player never joins, no fake Ready | Q3 waiting/exit | **PARTIALLY PROVEN** | Preparing/WaitingForJoin + exit seguro | dedicated never-join public case |

---

## 4. Caso A — Player já disponível antes da Activity

### Estado

**PARTIALLY PROVEN**

### Evidência existente

`QaPlayerGameplayAdmissionRegression` prova tecnicamente:

```text
Player Join
→ Host estável
→ Actor selecionado
→ Activity scope resolvido
→ Actor preparado
→ GameplayReady
→ release contextual
```

Também existe evidência de lifecycle/reentry em Player regressions.

### O que isso permite afirmar

O framework possui capacidade técnica para entrar em uma Activity com Player/Actor já disponível e atingir estado preparado/gameplay-ready.

### Por que não é `PROVEN` para esta auditoria

O runner histórico atravessa internals/reflection em parte do pipeline. Não foi localizada uma regressão atual public-only que configure:

```text
Player já Joined
→ Activity com Explicit Slot
→ LogicalActorsPrepared
→ Actor preparado pelo lifecycle oficial
→ same entry occurrence Ready
```

sem bypass técnico.

### Gap

Adicionar esse caso ao mesmo runner public-only que provará o Caso B. Não é necessário criar uma suíte separada.

---

## 5. Caso B — Player entra depois da Activity ativa

### Estado

**PARTIALLY PROVEN**

Este é o caso central da auditoria.

### Evidência positiva

`QaM07InternalReconcileRegression` chegou a concluir:

```text
main-request-started
main-participants-preparing
first-slot-progressed
revision-coalescing-proved
second-player-joined
main-reconcile-completed
main-request-succeeded
one-actor-per-slot-proved
completed-no-change-proved
```

Isso é compatível com o fluxo descoberto em `AUDIT-02`:

```text
Activity ativa
→ explicit Slot ainda não Joined
→ readiness Preparing / WaitingForJoin
→ Session revision muda
→ reconcile
→ Actor preparation/materialization
→ contribution completion
→ same occurrence Ready
```

### Limitação

A execução local terminou:

```text
53/54
status='Failed'
```

por uma assertion de cleanup/evidência de participant.

Também não foi localizada execução verde de um equivalente:

```text
QaManagerProvisionedReadinessPublicSurfaceRegression
```

ou outro runner que faça o mesmo caminho apenas por superfícies públicas.

### Conclusão

O QA atual fornece **forte evidência interna do mecanismo**, mas ainda não fornece uma prova fechada e consumer-equivalent do contrato.

---

## 6. Caso C — Player nunca entra

### Estado

**PARTIALLY PROVEN**

### Evidência existente

Q3 M07 concluiu:

```text
waiting-entry-succeeded
waiting-lifecycle-preparing
waiting-player-contribution-preparing
waiting-exit-succeeded
waiting-exit-released-contribution
waiting-exit-preserved-session
```

Isso prova que um Explicit Slot ainda não Joined pode ser representado como preparação esperada e que sair enquanto espera possui cleanup/release.

### O que falta

Não foi localizada uma regressão dedicada que mantenha o cenário:

```text
Activity ativa
+ Player Required
+ Player nunca Joined
```

e prove de forma determinística:

```text
continua Preparing
não vira Failed automaticamente
não vira Ready
não publica Loading 100%
não libera WaitCovered
continua cancelável/substituível
```

Não é necessário usar timeout para provar isso. O runner pode observar o estado causal antes de uma interrupção explicitamente owned e então executar clear/replacement para unwind.

### Gap

Esse caso deveria fazer parte do runner public-only de Manager-Provisioned readiness.

---

## 7. Caso D — Required readiness falha

### Estado

**PROVEN** para readiness genérico.  
**PARTIALLY PROVEN** para failure originado especificamente de Player preparation.

### Evidência genérica

`QaParticipantAwareReadinessLoadingTerminalRegression` prova:

```text
RequiredFailed
RequiredReleased
typed terminal result
destination authority preserved
last progress < 1
no terminal 100%
Loading retained
Transition retained
recovery gate retained
cleanup/restoration
```

### Evidência Player

Q3 M07 concluiu:

```text
rollback-reconcile-failed-preparation
rollback-delta-reverted
rollback-request-terminal-not-ready
```

mas o runner completo não terminou verde.

### Conclusão

O contrato genérico que o Player deveria consumir está bem protegido. A derivação Player → Required failure ainda precisa ser fechada no mesmo regression set de M07.

---

## 8. Caso E — Activity occurrence antiga é substituída

### Estado

**PROVEN** no nível genérico de Activity Readiness/Loading.  
**PARTIALLY PROVEN** para reconcile Player em andamento.

### Evidência genérica

O terminal regression passou com:

```text
replacement-occurrence-rejected
late-old-occurrence-rejected
duplicate-terminal-idempotent
```

A regressão IF-ID posterior também passou:

```text
readiness-collision-isolation
legitimate-supersession-preservation
```

### O que isso prova

- occurrence antiga não deve finalizar a nova;
- stale terminal não deve produzir sucesso;
- definitions/owners distintos não colapsam;
- supersession legítima permanece funcional.

### Subcaso ainda incompleto

Não há uma execução verde específica mostrando:

```text
Player reconcile da occurrence N em andamento
→ Activity substituída por occurrence N+1
→ completion/reconcile tardio de N rejeitado
→ nenhum Actor/gate/readiness de N afeta N+1
```

Esse é um negativo necessário para o QA Player, mas não impede classificar o contrato genérico de occurrence isolation como `PROVEN`.

---

## 9. Gaps de QA

### Gap 1 — Public-only Manager-Provisioned readiness

**Severidade:** Critical para o problema auditado.

Falta uma regressão verde que use somente a superfície que um consumidor real pode usar e prove:

```text
Activity WaitVisible ou WaitCovered
→ Explicit Slot projetado, ainda não Joined
→ readiness = Preparing / WaitingForJoin
→ RequestJoin
→ Host/Slot commit estável
→ automatic reconcile
→ Actor selection
→ logical preparation
→ physical materialization
→ contribution Completed
→ same occurrence Ready
→ gate/reveal segue a policy
```

Sem:

```text
reflection
manual RuntimeScopeContext
direct TryPrepareSelectedActor
direct internal reconcile
external Slot mutation
manual Actor creation
```

### Gap 2 — Q3 interno precisa terminar verde

`QaM07InternalReconcileRegression` já possui quase toda a matriz relevante, mas a prova não pode ser promovida enquanto a execução observada terminar `Failed`.

O primeiro objetivo não é adicionar mais casos. É estabilizar:

```text
fixture ownership
participant evidence
cleanup invariant
joining close/final restore
```

sem enfraquecer as assertions existentes.

### Gap 3 — Player never joins

Adicionar um caso explícito:

```text
WaitingForJoin
→ nenhuma Session revision de Join
→ request continua não terminal
→ readiness continua Preparing
→ nenhum Actor existe
→ interrupção owned por clear/replacement
→ cleanup correto
```

Esse caso distingue **legítima espera** de deadlock/false success.

### Gap 4 — Join disponível durante retained gate

`AUDIT-03` concluiu que o package Join API não consulta diretamente gameplay/input gate, mas isso ainda precisa de prova QA integrada.

O caso deve demonstrar:

```text
entry gate held
→ public Join command ainda executável
→ Session commit
→ reconcile
```

Esse é o teste que detecta uma dependência circular real entre control plane e gameplay gate.

### Gap 5 — Positive participant-aware Loading

Confirmar por execução verde:

```text
4 Required
1 Optional
0/4 → 1/4 → 2/4 → 3/4 → 4/4
Optional pending/failed não altera denominator
aggregate Ready → 100%
100% → Hide → Reveal
```

O negative terminal path já está bem coberto.

### Gap 6 — Optional semantics

A evidência de Required é forte; Optional ainda depende principalmente do contrato planejado/esperado nas evidências locais consultadas.

Deve existir assertion executada que prove:

```text
Optional Preparing não bloqueia Ready
Optional Failed não bloqueia Ready
Optional não altera readiness ratio
```

### Gap 7 — Player stale occurrence during reconcile

Adicionar um negativo owner/occurrence-scoped:

```text
occurrence N waiting
→ Join/reconcile começa
→ Activity replacement cria N+1
→ N tenta completar
→ N rejeitado
→ Actor/Readiness/Gate de N+1 intactos
```

### Gap 8 — Regressões manuais não equivalem a regressão contínua

A infraestrutura QA observada é predominantemente baseada em `MenuItem`/Play Mode manual. Isso não invalida a evidência dos runs, mas significa que “existe um runner” e “o contrato está protegido continuamente” são afirmações diferentes.

Para esta auditoria, `PROVEN` significa **evidência local de execução bem-sucedida**, não apenas presença de classe ou plano.

---

## 10. Findings

### IF-READY-04-011 — WaitCovered não é o principal gap de QA

**Status:** Confirmed.

O QA genérico já demonstra:

```text
Preparing
→ request pending
→ presentation retained
→ gate retained
→ Ready
→ presentation release
→ gate release
```

Portanto, criar outro smoke genérico de WaitCovered não ataca o maior risco atual.

---

### IF-READY-04-012 — Failure e stale occurrence estão melhor cobertos que late join

**Status:** Confirmed.

`QaParticipantAwareReadinessLoadingTerminalRegression` fornece uma prova fechada de Required failure/release, cancellation, replacement e late old occurrence.

O contraste é importante: a infraestrutura genérica de readiness possui cobertura terminal mais forte que a integração Player.

---

### IF-READY-04-013 — Player technical capability é provada, product reachability não

**Status:** Confirmed.

`QaPlayerGameplayAdmissionRegression` prova Join, Actor preparation/materialization, gameplay admission e release, mas alcança parte desse pipeline através de internals/reflection.

Não se deve reutilizar esse resultado como prova de:

```text
public RequestJoin
→ automatic active-Activity reconcile
```

---

### IF-READY-04-014 — O Q3 interno é evidência útil, mas ainda não baseline verde

**Status:** Confirmed.

O runner atingiu as assertions centrais de:

```text
WaitingForJoin
revision coalescing
reconcile
second Player
request success
one Actor per Slot
exit
reentry
```

Porém a execução observada terminou `53/54 Failed`.

A classificação correta é `PARTIALLY PROVEN`.

---

### IF-READY-04-015 — O principal teste faltante é uma vertical pública, não mais um smoke interno

**Status:** Confirmed.

A lacuna com maior valor causal é:

```text
public-only Manager-Provisioned late join
```

com a mesma Activity occurrence e sem bypass técnico.

Esse teste conecta os contratos de `AUDIT-01`, `AUDIT-02` e `AUDIT-03` numa única prova.

---

### IF-READY-04-016 — “Player nunca entra” precisa ser um estado testado, não um timeout

**Status:** Confirmed.

O QA deve provar que `WaitingForJoin` permanece um estado de preparação legítimo até uma interrupção explícita. Não deve adicionar timer artificial para transformar ausência de Join em failure ou Ready.

---

### IF-READY-04-017 — A circularidade Join ↔ Gate ainda não está provada nem descartada por QA integrado

**Status:** Confirmed.

O package Join API, segundo `AUDIT-03`, não é diretamente bloqueado pelo gameplay gate. Porém falta um QA que execute o public Join enquanto a entry policy ainda mantém o gate.

Isso é necessário para diferenciar:

```text
runtime deadlock
```

de:

```text
consumer/control-plane composition problem
```

---

### IF-READY-04-018 — Occurrence isolation genérico possui boa evidência

**Status:** Confirmed.

Entre terminal regression e IF-ID regression, existe evidência executada de:

```text
replacement rejection
late old occurrence rejection
readiness isolation
ownership isolation
legitimate supersession
```

Não há razão para criar outra suíte genérica apenas para repetir essa prova.

---

### IF-READY-04-019 — Positive Required/Optional Loading ainda precisa de run confirmado

**Status:** Not fully proven with available evidence.

O shape do regression está documentado e coerente com o contrato, mas esta auditoria não encontrou uma execução verde local do positive participant-aware Loading runner.

Até essa evidência existir, não promover:

```text
Optional denominator semantics
multi-Required determinate progression
```

para `PROVEN`.

---

### IF-READY-04-020 — Cobertura mínima recomendada antes da auditoria causal final

A próxima auditoria pode considerar o QA suficiente para a análise causal quando houver, no mínimo:

```text
1. Q3 internal green
2. Q4 public-only green
3. public never-join case
4. Join-under-retained-gate case
5. Player stale-occurrence reconcile case
6. positive Required/Optional Loading run confirmado
```

Os testes genéricos de WaitCovered, terminal failure e occurrence rejection já fornecem a base necessária e não precisam ser recriados.

---

## Conclusão

O QAFramework **já prova bastante do sistema que cerca o problema**, especialmente:

```text
Activity Readiness
WaitVisible
WaitCovered
failure terminals
Loading safety
recovery gate
occurrence replacement
technical Player/Actor capabilities
```

O ponto não coberto com a mesma força é exatamente a interseção que interessa:

```text
Activity occurrence já ativa
+ Explicit Player Slot WaitingForJoin
+ public RequestJoin
+ automatic reconcile
+ Actor preparation/materialization
+ same occurrence Ready
+ gate/reveal completion
```

A cobertura interna mais recente mostra que esse caminho está muito próximo de uma prova técnica fechada, mas o runner observado ainda termina em failure de cleanup/evidência. E, com os arquivos locais disponíveis, **não há prova verde do caminho public-only**.

Portanto, para os cinco casos mínimos desta auditoria:

| Caso | Resultado |
|---|---|
| A — Player já disponível | **PARTIALLY PROVEN** |
| B — Player entra depois | **PARTIALLY PROVEN** |
| C — Player nunca entra | **PARTIALLY PROVEN** |
| D — Required readiness falha | **PROVEN** genérico / **PARTIAL** Player-specific |
| E — occurrence antiga substituída | **PROVEN** genérico / **PARTIAL** Player-reconcile |

O próximo ganho de QA não vem de ampliar horizontalmente a suíte. Vem de fechar verticalmente **um runner public-only de Manager-Provisioned readiness**, usando os contratos oficiais e a mesma sequência que um consumidor real precisa executar.
