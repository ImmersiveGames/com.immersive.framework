# AUDIT-03 — WaitCovered, Loading, Cover e Gates

## 1. Resumo

- **IF-READY-03-001:** `WaitCovered` é iniciado por `GameFlowRuntime` depois que a Activity/Startup Activity já foi materializada e `TryPrepareActivityEntryReadinessExecution` capturou a `ActivityReadinessOccurrence` atual.
- **IF-READY-03-002:** A occurrence observada é sempre a occurrence corrente de `RouteLifecycleRuntime.CurrentOccurrence`; a espera é recusada/invalida se o resultado de Activity não corresponde ao target ou se a occurrence não casa com Activity + sequence.
- **IF-READY-03-003:** A espera encerra somente por `Ready`, terminal failure da readiness, invalidation da occurrence, cancellation/supersession por troca de Route/Activity/clear/dispose, ou falha de validação/configuração antes da espera.
- **IF-READY-03-004:** `WaitCovered` segura o cover visual porque `GameFlowRuntime` executa `TransitionRequest.Before` antes da Activity e adia `TransitionRequest.After` até a readiness ficar Ready. Em `WaitVisible`, o `After` roda antes da espera.
- **IF-READY-03-005:** Loading progress participant-aware só é projetado para Activity `WaitCovered` com reporter real. O envelope reserva uma faixa técnica e uma faixa final de readiness.
- **IF-READY-03-006:** Loading só pode chegar a 100% quando a readiness snapshot da occurrence capturada está `IsReady`; o envelope rejeita 1.0 técnico se há faixa de readiness reservada.
- **IF-READY-03-007:** O Transition Gate é aplicado por `ApplyTransitionGate` e mantido durante a operação; para políticas waiting, a configuração exige `TransitionGateMode.InputInteractionAndGameplay`, logo bloqueia lifecycle request, input, interaction e gameplay action até release.
- **IF-READY-03-008:** Em failure/cancelled/invalidated committed, `GameFlowRuntime` libera o transition gate normal, mas aplica um recovery gate por Activity owner que continua bloqueando input, interaction e gameplay.
- **IF-READY-03-009:** Não há timeout na espera de readiness. Se readiness permanecer Preparing indefinidamente, a task de wait e, no caminho WaitCovered, o cover/loading/gate permanecem ativos indefinidamente salvo interrupção externa por substituição/cancel/dispose.
- **IF-READY-03-010:** `WaitCovered` não tem autoridade indevida sobre readiness: o waiter só observa `ActivityReadinessOccurrenceState.Changed`; Loading só projeta snapshots; Gates bloqueiam capabilities, não alteram `ActivityReadinessState`.
- **IF-READY-03-011:** A dependência circular crítica não aparece criada no package para Player Join manager-provisioned: os caminhos `RequestJoin`/`TryJoin`/`TryOpenJoining` não avaliam `GateDomain.InputAcceptance` ou `GameplayAction`. Se um consumidor dispara Join por input/UI bloqueado pelo Transition Gate, a investigação deve continuar na composição do consumidor.

## 2. Arquivos/classes relevantes

| ID | Arquivo/classe | Papel |
| --- | --- | --- |
| IF-READY-03-012 | `ActivityEntryReadinessPolicy` | Define `ObserveOnly`, `WaitCovered` e `WaitVisible`; documenta cover/gate semantics. |
| IF-READY-03-013 | `GameFlowRuntime.ActivityEntryReadinessOrchestration` | Valida configuração, captura occurrence, inicia/cancela wait, mapeia terminal states e aplica recovery gate. |
| IF-READY-03-014 | `ActivityEntryReadinessWaiter` | Waiter one-shot occurrence-scoped; observa Ready/Failure/Invalidated/Cancelled. |
| IF-READY-03-015 | `ActivityEntryReadinessWaitResult` / `ActivityEntryReadinessExecutionResult` | Evidência terminal do wait e apresentação: reveal/loading/gate/recovery. |
| IF-READY-03-016 | `GameFlowRuntime` | Fluxos Start/Route/Activity: aplica transition gate, executa Before/After, segura/libera cover e gates. |
| IF-READY-03-017 | `GameFlowRuntime.ActivityEntryLoadingProgress` | Variante participant-aware de Loading progress para `WaitCovered`. |
| IF-READY-03-018 | `ActivityEntryLoadingProgressEnvelope` | Autoridade operation-scoped de progress, monotonicidade e terminal 100% somente com readiness Ready. |
| IF-READY-03-019 | `ActivityEntryLoadingProgressPlan` | Divide a barra em range técnico e range readiness. |
| IF-READY-03-020 | `ActivityReadinessProgressSnapshot` | Snapshot de contadores de readiness convertido em ratio para Loading. |
| IF-READY-03-021 | `TransitionGateBlockerPolicy` | Cria blockers de lifecycle, input, interaction e gameplay durante transição. |
| IF-READY-03-022 | `ActivityEntryReadinessRecoveryGatePolicy` | Cria blockers de recovery após readiness failure/cancel/invalidation committed. |
| IF-READY-03-023 | `LocalPlayerProvisioningAuthoring` / `LocalPlayerProvisioningRuntimeHostModule` / `LocalPlayerProvisioningBridge` | Caminho de Player Join usado para checar se Join é bloqueado pelo mesmo gate. |

## 3. WaitCovered

- **IF-READY-03-024:** Quem inicia `WaitCovered` é `GameFlowRuntime`: após `StartRouteCoreAsync`/`StartActivityCoreAsync` retornar um destination committed, ele chama `TryPrepareActivityEntryReadinessExecution` e, se a policy exige wait, chama `WaitForPreparedActivityEntryReadinessAsync`.
- **IF-READY-03-025:** `TryValidateActivityEntryReadinessConfiguration` exige policy definida, `TransitionGateMode.InputInteractionAndGameplay` para qualquer policy waiting e cover visual explícito para `WaitCovered`.
- **IF-READY-03-026:** A occurrence é capturada de `_routeLifecycleRuntime.CurrentOccurrence` e precisa casar com a Activity do resultado committed. A occurrence tem Activity reference + transition sequence, conforme AUDIT-01.
- **IF-READY-03-027:** `WaitForPreparedActivityEntryReadinessAsync` cria um `ActivityEntryReadinessActiveOperation`, publica progress inicial se houver forwarder, e chama `WaitForActivityEntryReadinessAsync(occurrence, activeOperation.WaitScope.Token)`.
- **IF-READY-03-028:** `ActivityEntryReadinessWaiter` não descobre estado nem altera readiness; ele recebe `ActivityReadinessOccurrenceState`, assina `Changed`, observa aggregate readiness e completa a task.
- **IF-READY-03-029:** Condição de sucesso: `readinessState.IsReady`. Condição de failure: `readinessState.HasTerminalFailure`. Condição de invalidation: occurrence state invalidated. Cancelled vem do token; superseded é mapeado quando cancellation reason é route replacement.
- **IF-READY-03-030:** Se readiness fica Preparing sem terminal failure, nada no waiter completa a task. Não foi encontrado timeout ou fallback silencioso nessa camada.

## 4. Loading Progress

- **IF-READY-03-031:** Loading participant-aware só é usado quando `ShouldProjectActivityEntryReadinessProgress` retorna true: Activity não nula, `EntryReadinessPolicy == WaitCovered`, reporter não nulo e reporter não é `NoOpFrameworkLoadingProgressReporter`.
- **IF-READY-03-032:** O plano reserva uma unidade final para readiness: `ActivityEntryLoadingProgressPlan.Create(technicalStepCount, reserveReadinessPhase: true)` cria `TechnicalRange` e `ReadinessRange` proporcionais a `technicalStepCount + 1`.
- **IF-READY-03-033:** O `TechnicalReporter` mapeia progresso técnico para `TechnicalRange`; `CompleteTechnicalRangeAsync` obriga a faixa técnica a alcançar o boundary reservado antes de readiness progress começar.
- **IF-READY-03-034:** `ActivityReadinessProgressSnapshot` converte readiness em ratio: se há Required, `RequiredCompletedCount / RequiredCount`; se não há Required, usa 1.0 somente quando `readiness.IsReady`, senão 0.
- **IF-READY-03-035:** `ActivityEntryLoadingProgressForwarder` assina updates de readiness, captura uma occurrence esperada e só aceita updates da mesma Activity + occurrence. Updates Ready intermediários são ignorados pelo handler; o Ready terminal é reportado explicitamente no release boundary.
- **IF-READY-03-036:** O envelope rejeita snapshots de outra occurrence incrementando `RejectedReadinessSnapshotCount`; isso impede que uma occurrence substituta avance o Loading da operação antiga.
- **IF-READY-03-037:** Loading não pode declarar sucesso antes de Activity Ready: `ReportEnvelopeProgressAsync` ignora progresso determinate >= 1 quando há readiness phase reservada e `_reportingReadyTerminal` é false.
- **IF-READY-03-038:** O terminal 100% só é emitido quando `ReportReadinessAsync` recebe snapshot `IsReady`, marca `_reportingReadyTerminal` e reporta `Determinate(1f, ActivityReadiness, message)`.
- **IF-READY-03-039:** Se há terminal failure, o envelope marca terminal failure e não completa a barra; `EnsureTerminalLoadingCompletion` lança se algum fluxo tentar liberar Loading sem terminal progress Ready.

## 5. Cover

- **IF-READY-03-040:** O cover é operado pelo transition orchestrator via `TransitionRequest.Before` e `TransitionRequest.After`; o package não implementa o visual em `WaitCovered`, mas exige que exista cover visual para essa policy.
- **IF-READY-03-041:** Em `WaitCovered`, `GameFlowRuntime` executa `Before`, materializa a destination Activity/Route, espera readiness, e só executa `After` quando `readinessExecution.IsReady`.
- **IF-READY-03-042:** Portanto, quem segura o cover é a própria sequência do `GameFlowRuntime`: a ausência de `TransitionRequest.After` mantém a apresentação coberta até Ready.
- **IF-READY-03-043:** Quem libera o cover é `GameFlowRuntime` ao chamar `ExecuteTransitionAsync(TransitionRequest.After(...))` depois de Ready. Em failure, cancelled ou invalidated, `transitionAfter` fica default no caminho `WaitCovered`, logo não há reveal normal.
- **IF-READY-03-044:** Em `WaitVisible`, o `After` roda antes do wait; a Activity fica visível, mas gate/capabilities continuam bloqueados até a espera terminar e o gate ser liberado.
- **IF-READY-03-045:** Em supersession no fluxo normal com `WaitCovered`, há tratamento para chamar `afterRouteLifecycle`/`afterActivityLifecycle` e marcar loading released, mas isso não equivale a Ready; o resultado final é superseded/failed committed target.

## 6. Gates

- **IF-READY-03-046:** `ApplyTransitionGate` cria `_transitionGateSnapshot` por `TransitionGateBlockerPolicy.CreateRunningSnapshot`. Para `InputInteractionAndGameplay`, os blockers cobrem lifecycle request, input acceptance, interaction acceptance e gameplay action.
- **IF-READY-03-047:** Waiting policies exigem `InputInteractionAndGameplay`, então `WaitCovered` e `WaitVisible` mantêm input/interactions/gameplay bloqueados durante a espera.
- **IF-READY-03-048:** O gate normal é liberado por `ReleaseTransitionGate` após a seção de wait/reveal, ou por `ReleaseTransitionGateIfStillActive` no `finally`.
- **IF-READY-03-049:** Em failure/cancelled/invalidated committed, `ApplyActivityEntryReadinessRecoveryGate` cria um recovery gate owner-scoped para a Activity occurrence. Ele bloqueia input acceptance, interaction acceptance e gameplay action, mas não lifecycle request.
- **IF-READY-03-050:** `CurrentActivityEntryReadinessGateSnapshot` combina o transition gate com o recovery gate; `EvaluateTransitionGateAdmission` avalia esse snapshot combinado.
- **IF-READY-03-051:** O recovery gate é liberado por `ReleaseActivityEntryReadinessRecoveryGate`, normalmente em sucesso, supersession cleanup ou dispose. Se failure é retornado ao consumidor, o recovery gate fica aplicado para impedir capabilities na destination não pronta.
- **IF-READY-03-052:** Não foi encontrado caminho em Player Join manager-provisioned que consulte `EvaluateTransitionGateAdmission` para `InputAcceptance` ou `GameplayAction`. O join lógico/manual usa `RequestJoin` → `TryJoin` → bridge diretamente.

## 7. Terminal states

- **IF-READY-03-053:** `Ready`: wait result Ready, execution status Ready, `WaitCovered` executa `After`, Loading reporta 100%, transition gate é liberado e recovery gate é limpo.
- **IF-READY-03-054:** `Preparing` indefinido: não há terminal state; a operação permanece aguardando. Em `WaitCovered`, cover/loading/gate ficam retidos; em `WaitVisible`, visual já foi revelado, mas gate segue retido.
- **IF-READY-03-055:** `Failed`: `ActivityReadinessState.HasTerminalFailure` completa o waiter como Failed; GameFlow retorna committed target not ready e aplica recovery gate se destination é authoritative.
- **IF-READY-03-056:** `Invalidated`: occurrence state invalidated ou ocorrência indisponível produz Invalidated; GameFlow trata como failure committed readiness invalidated quando já há destination authoritative.
- **IF-READY-03-057:** `Cancelled`: token cancelado por dispose/activity clear/activity replacement vira Cancelled, exceto route replacement que pode ser remapeado para Superseded.
- **IF-READY-03-058:** `Superseded`: route replacement cancela a espera antiga com status Superseded; o resultado preserva a classificação separada de cancelamento genérico.
- **IF-READY-03-059:** Não há fallback silencioso para Ready: unknown wait status vira Invalidated, failure marca terminal failure em Loading, e `EnsureTerminalLoadingCompletion` falha se tentarem liberar Loading sem progress terminal Ready.

## 8. Occurrence replacement

- **IF-READY-03-060:** Substituição de Route/Activity ou clear chama os métodos de interrupção da active readiness operation. Eles capturam a operação ativa, solicitam cancellation com reason específico e aguardam unwind.
- **IF-READY-03-061:** Se a occurrence observada for substituída antes da projeção final, `WaitForPreparedActivityEntryReadinessAsync` invalida um resultado Ready antigo com reason `OccurrenceReplacedBeforeFinalProjection`.
- **IF-READY-03-062:** Loading forwarder e envelope são occurrence-bound: o forwarder compara updates com `_capturedOccurrence`; o envelope aceita a primeira occurrence e rejeita snapshots posteriores que não casem.
- **IF-READY-03-063:** Isso evita que readiness de uma occurrence nova libere cover/loading da operação antiga.

## 9. Análise de possível dependência circular

- **IF-READY-03-064:** A dependência circular procurada seria: Player Join é necessário para Activity Ready; ao mesmo tempo, Player Join é bloqueado pelo gate que só libera quando Activity Ready.
- **IF-READY-03-065:** A primeira metade pode existir por configuração de Activity: AUDIT-02 mostrou que uma Activity com Slot explícito não Joined e requirement >= `JoinedSlots` fica Preparing até o Player entrar; para `LogicalActorsPrepared`, o join também é pré-condição de seleção/preparação/materialização.
- **IF-READY-03-066:** A segunda metade não aparece no package para o caminho canônico manager-provisioned: `LocalPlayerProvisioningAuthoring.RequestJoin`, `LocalPlayerProvisioningRuntimeHostModule.TryJoin` e `LocalPlayerProvisioningBridge.TryJoin` não consultam o Transition/Readiness gate; eles validam joiningOpen/capacity/backend/configuração e mutam a Session.
- **IF-READY-03-067:** O Transition Gate bloqueia `GateScope.Input/InputAcceptance`, `Interaction/InteractionAcceptance`, `Gameplay/GameplayAction` e lifecycle requests. Ele não é chamado diretamente pelo join API auditado.
- **IF-READY-03-068:** Portanto, no package, não foi encontrada dependência circular canônica entre Player Join e Activity Ready. Se o consumidor só dispara `RequestJoin` por um input/UI path que respeita `InputAcceptance`/`InteractionAcceptance`, a circularidade pode existir na composição do consumidor, não neste núcleo.
- **IF-READY-03-069:** Local exato onde a primeira metade nasce: `ActivityPlayerActorLifecycleParticipant.BeginDeferredActivityPlayerReadiness` com Slot explícito não Joined cria Player readiness Required em Preparing/WaitingForJoin. Local do gate que poderia bloquear um trigger de consumidor: `TransitionGateBlockerPolicy.CreateInputAcceptanceBlocker`/`CreateInteractionAcceptanceBlocker` aplicados por `ApplyTransitionGate` enquanto `WaitCovered` aguarda.

## 10. Findings

- **IF-READY-03-070:** `WaitCovered` é um orquestrador de apresentação/capability, não uma autoridade de readiness.
- **IF-READY-03-071:** O package é estrito contra sucesso prematuro de Loading: 100% depende de snapshot Ready da occurrence capturada.
- **IF-READY-03-072:** Não há timeout; Preparing infinito é um estado operacional possível e segura cover/gates indefinidamente.
- **IF-READY-03-073:** Failure não revela normalmente em `WaitCovered`; o transition gate é liberado, mas um recovery gate owner-scoped mantém capabilities bloqueadas.
- **IF-READY-03-074:** Occurrence replacement é tratada defensivamente por cancellation/supersession/invalidation e por filtros de occurrence no Loading forwarder/envelope.
- **IF-READY-03-075:** A potencial circularidade Player Join ↔ Activity Ready depende de como o consumidor expõe o comando de join sob input/UI/gate, não de uma checagem de Gate encontrada no caminho canônico de join do package.

## 11. Questões para integração

- **IF-READY-03-076:** O consumidor chama `LocalPlayerProvisioningAuthoring.RequestJoin` por código independente do Input Gate, ou apenas por ação/input/UI bloqueada por `InputAcceptance`/`InteractionAcceptance`?
- **IF-READY-03-077:** Para Activities WaitCovered que exigem late join, deve existir uma exceção de UI/input para join enquanto o cover está ativo?
- **IF-READY-03-078:** Qual componente visual implementa o cover concreto no consumidor e ele tem fallback/manual dismiss em failure?
- **IF-READY-03-079:** Deve haver timeout de readiness no produto, ou o comportamento correto é aguardar indefinidamente?
- **IF-READY-03-080:** O recovery gate tem fluxo operacional documentado de liberação após remediation, retry, clear ou replacement?
- **IF-READY-03-081:** Testes de integração devem cobrir WaitCovered + Slot explícito não Joined + join programático durante gate + same occurrence Ready + Loading 100% + reveal.
