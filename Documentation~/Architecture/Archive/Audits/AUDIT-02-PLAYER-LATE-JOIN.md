# AUDIT-02 — Player Participation, Actor Preparation e Late Join

## 1. Resumo

- **IF-READY-02-001:** O fluxo canônico começa em `PlayerParticipationRuntimeContext`, autoridade session-scoped de Slots. Slots nascem configurados como `Available`, podem ser `Reserved` durante uma operação de join e viram `Joined` apenas por `TryMarkJoined` com reservation token válido.
- **IF-READY-02-002:** Uma Activity projeta participantes Player via `ActivityPlayerParticipationProjectionResolver`: pode projetar nenhum Slot, todos os Slots já Joined, ou Slots explícitos configurados na Activity. Essa projeção é capturada no enter da Activity e alimenta o lifecycle `ActivityPlayerActorLifecycleParticipant`.
- **IF-READY-02-003:** A diferença principal é: nenhum participante gera enter no-op sem readiness participant; Slot explícito existente mas não Joined gera readiness deferred/preparing se a requirement level exige Joined; Player Joined fornece host evidence e pode avançar para seleção/preparação/materialização.
- **IF-READY-02-004:** A seleção de Actor ocorre depois do Slot Joined: `ApplyActorSelection` rejeita Slots não Joined, e o lifecycle seleciona o default Actor quando a requirement level exige `SelectedActors` e o Slot Joined ainda não tem seleção.
- **IF-READY-02-005:** O Actor passa a estar logicamente preparado quando `TryPrepareSelectedActor` resolve Slot Joined + Actor selecionado + host evidence, materializa fisicamente o Actor, ativa o handle e grava um `PreparationRecord` com `PlayerActorPreparationState.Prepared`.
- **IF-READY-02-006:** Materialização física é executada por `AttachedPlayerActorMaterializationAdapter.TryMaterialize` a partir do scope da Activity, Slot, ActorProfile e `LocalPlayerHostAuthoring`; falha de materialização/ativação é caminho explícito de failure.
- **IF-READY-02-007:** A contribuição para Activity Readiness é um único `ActivityReadinessParticipant` runtime Required (`framework.player-actor.activity-readiness`) fornecido pelo mesmo `ActivityPlayerActorLifecycleParticipant` como `IActivityReadinessParticipantSource`.
- **IF-READY-02-008:** Player que entra antes da Activity: a Activity enter vê o Slot já Joined na Session snapshot, registra/admite host evidence, seleciona default Actor se necessário, prepara/materializa no próprio enter e completa a readiness contribution imediatamente.
- **IF-READY-02-009:** Player que entra depois da Activity ativa: se a Activity projetou explicitamente um Slot ainda não Joined, o enter cria um readiness record Preparing; o `LateUpdate` do `PlayerActorPreparationRuntimeHostModule` observa mudança de revision da Session e chama reconcile para a mesma Activity/owner/occurrence.
- **IF-READY-02-010:** O runtime atual suporta canonicamente o cenário principal somente para Slots já projetados pela occurrence atual. Ele não adiciona novos participantes a uma occurrence já congelada para uma Activity em modo “all joined slots” que tinha zero participantes ou não incluía o late join no conjunto capturado.

## 2. Arquivos/classes relevantes

| ID | Arquivo/classe | Papel |
| --- | --- | --- |
| IF-READY-02-011 | `PlayerParticipationRuntimeContext` | Autoridade session-scoped de Slots, join state, selection state, revisions e snapshots. |
| IF-READY-02-012 | `LocalPlayerProvisioningBridge` | Orquestra join manual: reserva Slot, provisiona PlayerInput, marca Joined, cria assignment e admite host. |
| IF-READY-02-013 | `LocalPlayerProvisioningRuntimeHostModule` / `LocalPlayerProvisioningAuthoring` | Superfície host/authoring para RequestJoin e registro do join na preparação de Actor. |
| IF-READY-02-014 | `PlayerActorPreparationRuntimeHostModule` | Autoridade host-scoped de preparação; registra host evidence, expõe seleção/preparação/release e executa reconciliation em `LateUpdate`. |
| IF-READY-02-015 | `PlayerActorPreparationRuntimeContext` | Estado de preparação por Slot, materialização física, idempotência e proteção contra duplicação. |
| IF-READY-02-016 | `ActivityPlayerActorLifecycleParticipant` | Participante Required de Activity content que projeta Players, executa enter/exit e guarda readiness record/reconcile state. |
| IF-READY-02-017 | `ActivityPlayerParticipationProjectionResolver` | Resolve projeção da Activity para Slots de sessão: none, all joined, explicit. |
| IF-READY-02-018 | `PlayerActivityReconciliationRuntimeHostModule` | Observador de revision/occurrence que chama `TryReconcileActiveActivityPlayerLifecycle`. |
| IF-READY-02-019 | `ActivityPlayerActorLifecycleParticipant.Readiness` | Fonte de `ActivityReadinessParticipant` e ponte entre readiness record e terminal state Completed/Failed/Released. |
| IF-READY-02-020 | `ActivityPlayerActorLifecycleParticipant.Reconcile` | Delta reconcile: join, default selection, logical preparation, materialization, completion/failure/rollback. |
| IF-READY-02-021 | `FrameworkRuntimeHost.PlayerReadinessSource` | Registra o lifecycle participant tanto como content execution source quanto como readiness participant source. |

## 3. Player participation

- **IF-READY-02-022:** `PlayerParticipationRuntimeContext` contém `SlotRecord` por perfil configurado. Cada record guarda `AllocationState`, `ReservationToken`, `Revision`, `SelectedActorProfile` e `SelectionRevision`.
- **IF-READY-02-023:** O contexto cria snapshots com `ContextId`, revision global, dynamic capacity, joining gate e todos os Slots. Reconciliation observa esses snapshots, não eventos diretos de join.
- **IF-READY-02-024:** O join técnico é deliberadamente separado do Actor: o join bem-sucedido termina com a mensagem “Logical Actor remains unprepared”, e o registro em Actor preparation acontece depois, por extensão de provisioning.
- **IF-READY-02-025:** `LocalPlayerProvisioningAuthoring.RequestJoin` chama `runtimeModule.TryJoin(request)` e, se o resultado é sucesso, chama `RegisterJoinWithActorPreparation` antes de devolver o resultado ao consumidor.

## 4. Slot/Join lifecycle

- **IF-READY-02-026:** Nenhum participante: a Activity resolve projeção com `projectedSlots.Count == 0`, cria `ActiveActivityRecord` vazio, snapshot `SucceededEnteredNoParticipants` e retorna `SucceededNoOp`; não há `playerReadinessRecord` nem readiness participant Player.
- **IF-READY-02-027:** Slot existente mas ainda não Joined: isso só entra na Activity projection quando a Activity usa projeção explícita de Slots. Se a requirement level exige `JoinedSlots` ou superior, `ShouldDeferActivityPlayerReadiness` retorna true e cria record Preparing com `WaitingForJoin`.
- **IF-READY-02-028:** Player Joined: `LocalPlayerProvisioningBridge.TryJoin` reserva o próximo Slot disponível, chama o backend/PlayerInputManager, valida `LocalPlayerHostAuthoring`, marca o reservation token como Joined, cria assignment session-scoped e comita a admissão no host.
- **IF-READY-02-029:** Depois do join, `TryRegisterJoinedHost` registra host evidence no `PlayerActorPreparationRuntimeHostModule` usando origem `ManagerProvisioned`, assignment token, host binding identity e LocalPlayerHost. Esse host evidence é obrigatório para a preparação posterior.
- **IF-READY-02-030:** A revision global da Session aumenta nas mutações de Slot/selection; a reconciliation compara `session.Revision` com a revision observada/aplicada para disparar uma nova passada.

## 5. Actor preparation

- **IF-READY-02-031:** Seleção de Actor só pode mudar em Slot Joined. `ApplyActorSelection` rejeita `record.AllocationState != Joined` com `RejectedSlotNotJoined`.
- **IF-READY-02-032:** No enter ou reconcile, se a requirement level é pelo menos `SelectedActors` e o Slot Joined não tem Actor selecionado, o lifecycle chama `TrySelectDefaultActor(slotId, expectedSelectionRevision, ...)`.
- **IF-READY-02-033:** `TrySelectDefaultActor` usa o default Actor do `PlayerSlotProfile`; se não houver default ou a selection policy/revision rejeitar, o lifecycle falha selection e faz rollback das mutações já aplicadas naquele enter/reconcile.
- **IF-READY-02-034:** Para `LogicalActorsPrepared` ou superior, o lifecycle chama `TryPrepareSelectedActor(scopeContext, slotId, ...)`.
- **IF-READY-02-035:** `TryPrepareSelectedActor` exige scope válido, Slot válido, Slot Joined, ActorProfile selecionado e host/assignment correlation válida. Sem isso retorna rejected/failure explícito.
- **IF-READY-02-036:** Se já existe `PreparationRecord` para o Slot e ele corresponde ao mesmo owner/profile/host/identidades funcionais, a preparação é idempotente e retorna `SucceededAlreadyPrepared`; se não corresponde, rejeita conflito.

## 6. Actor materialization

- **IF-READY-02-037:** A materialização física ocorre dentro de `TryPrepareSelectedActor`, por `materializationAdapter.TryMaterialize(scopeContext, slot, selectedActorProfile, localPlayerHost, source, reason)`.
- **IF-READY-02-038:** Depois da materialização, o handle precisa ativar com `handle.TryActivate`; falha de ativação tenta rollback físico via `TryReleaseMaterialization` e pode virar `FailedActivation` ou `FailedRollback`.
- **IF-READY-02-039:** Em sucesso, o contexto cria um `PlayerActorPreparationSummary` Prepared com materialization/actor evidence, adiciona `PreparationRecord` no dicionário por `PlayerSlotId` e incrementa revision.
- **IF-READY-02-040:** O dicionário `records` por Slot e a checagem de idempotência/conflito são a principal barreira contra Actor duplicado para o mesmo Slot na mesma preparação corrente.

## 7. Late join

- **IF-READY-02-041:** Late join só pode alterar a readiness da mesma Activity occurrence se essa occurrence já criou `playerReadinessRecord` e `ActivityReadinessParticipant` para um Slot projetado que ainda estava pendente.
- **IF-READY-02-042:** Esse caso acontece de forma canônica para projeção explícita de Slot não Joined com requirement >= `JoinedSlots`: o enter fica `SucceededEnteredPreparing`, o readiness participant fica `Preparing`, e `CaptureActiveReconcileTarget` depois expõe Activity/owner/occurrence exatos.
- **IF-READY-02-043:** Para projeção “all joined slots”, o resolver só adiciona Slots que já estão Joined no snapshot de enter. Um Player que entra depois não fazia parte do conjunto congelado da occurrence; se a Activity entrou com zero projected slots, não há readiness participant Player para atualizar.
- **IF-READY-02-044:** Portanto, “late join” suportado aqui é late completion de um Slot explicitamente projetado, não expansão dinâmica do conjunto de participantes de readiness.

## 8. Reconciliation

- **IF-READY-02-045:** Quem executa reconciliation é `PlayerActorPreparationRuntimeHostModule.LateUpdate`. Ele cria/usa `PlayerActivityReconciliationRuntimeHostModule` e passa `participationContext.CreateSnapshot()` mais `activityLifecycleParticipant`.
- **IF-READY-02-046:** A notificação efetiva é polling de revision/target em `LateUpdate`: `ObserveAndReconcile` compara `session.Revision` com `observedSessionRevision` e compara Activity/owner/occurrence/status do reconcile target.
- **IF-READY-02-047:** O target é capturado do `playerReadinessRecord`. Ele só fica `Ready` se existe Activity, owner/scope válido, occurrence positiva, readiness participant existente e `playerReadinessParticipant.Occurrence == occurrence`.
- **IF-READY-02-048:** Reconciliation opera sobre a mesma Activity occurrence porque `TryReconcileActiveActivityPlayerLifecycle` valida Activity por referência, owner/scope por igualdade e occurrence contra o readiness record e o participante.
- **IF-READY-02-049:** É idempotente em vários pontos: sem revision delta retorna `SucceededNoChange`; record já completed sem delta projetado apenas reconhece revision externa; preparação já igual retorna `SucceededAlreadyPrepared`; enter para mesmo owner retorna no-op.
- **IF-READY-02-050:** Em uma passada com delta, reconcile atualiza cada Slot projetado: espera join se ainda não Joined; valida host evidence; seleciona default Actor se necessário; prepara/materializa; opcionalmente garante gameplay se requirement for `GameplayReady`; depois avalia admission.
- **IF-READY-02-051:** Se a avaliação final permite ativar ou está bloqueada somente pelo current entry gate, o lifecycle marca todos os Slots satisfeitos e completa a Player readiness contribution.
- **IF-READY-02-052:** Há caminho explícito de failure: projection missing/regressed, host evidence missing, selection failure, preparation/materialization failure, gameplay admission failure, evaluator failure/block e rollback failure chamam `FailPlayerReadinessContribution`.

## 9. Relação com Activity Readiness

- **IF-READY-02-053:** A relação com `AUDIT-01` é direta: Activity readiness captura participantes uma vez por occurrence. O Player lifecycle entra como fonte explícita e fornece exatamente um participante runtime Required quando a Activity tem requirement level != None e projectedSlots.Count > 0.
- **IF-READY-02-054:** O participante runtime é criado em objeto filho do `FrameworkRuntimeHost` chamado `Player Activity Readiness`, configurado como `framework.player-actor.activity-readiness`, Required, ordem -190.
- **IF-READY-02-055:** Quando Activity readiness chama `BeginPreparation`, o listener `OnPlayerReadinessPreparationStarted` sincroniza a occurrence para dentro do `playerReadinessRecord` e aplica terminal state se o record já estava Completed/Failed.
- **IF-READY-02-056:** Se o Player lifecycle completa, chama `CompletePlayerReadinessContribution`, que põe record Completed e então `ActivityReadinessParticipant.CompletePreparation`. Isso aciona o recomposer de `AUDIT-01` para transformar Required completed em aggregate Ready se o baseline técnico também estava Ready.
- **IF-READY-02-057:** Se falha, chama `FailPlayerReadinessContribution`, que aciona `FailPreparation` no participante Required; pelo contrato de `AUDIT-01`, Required failed incrementa blocking issues e impede Ready.
- **IF-READY-02-058:** Se a Activity é encerrada/substituída, o release do participante marca o record Released quando a occurrence bate; exit também chama `ReleasePlayerReadinessRecord("ActivityExit")`.

## 10. Cenário Player já Joined

- **IF-READY-02-059:** Pré-condição: o Player já passou por join completo, Slot está Joined, host evidence foi registrado pela preparação e a Session snapshot já carrega selection revision/Slot revision atuais.
- **IF-READY-02-060:** Activity enter resolve projeção. Se projection mode for all joined, inclui esse Slot; se explicit, inclui o Slot configurado independentemente de all joined.
- **IF-READY-02-061:** Como o Slot já está Joined, o lifecycle não defere por `WaitingForJoin`. Para requirement >= `SelectedActors`, seleciona default Actor se ainda não houver seleção.
- **IF-READY-02-062:** Para requirement >= `LogicalActorsPrepared`, chama preparação e materialização no próprio enter. Em sucesso, o readiness record é `Completed` e o readiness participant Required completa assim que a occurrence começa.
- **IF-READY-02-063:** Resultado esperado: Activity readiness da occurrence atual pode ficar Ready no próprio enter/publish inicial se baseline técnico também estiver Ready.

## 11. Cenário Late Join

- **IF-READY-02-064:** Pré-condição canônica: Activity já ativa projetou explicitamente um Slot ainda não Joined, requirement >= `JoinedSlots`, e criou readiness participant Required em `Preparing` para a occurrence atual.
- **IF-READY-02-065:** O Player entra depois via `RequestJoin`: Slot passa a Joined, assignment e host evidence são registrados, e a Session revision muda.
- **IF-READY-02-066:** No próximo `LateUpdate`, reconciliation observa mudança de session revision. Como o target da Activity atual tem Activity/owner/occurrence válidos, chama reconcile sobre a mesma occurrence.
- **IF-READY-02-067:** Reconcile vê o Slot projetado agora Joined, seleciona default Actor se exigido, prepara/materializa se exigido, atualiza o readiness record e completa/falha o participante Required.
- **IF-READY-02-068:** Resposta à pergunta principal: sim, o runtime suporta canonicamente `Activity ativa → Player entra depois → participação muda → Activity atual é reconciliada → Actor é preparado/materializado → readiness da mesma occurrence é atualizada`, **desde que o Slot late-joined já pertença à projeção congelada da occurrence**. Não há evidência de suporte canônico para adicionar um novo Slot não projetado à mesma occurrence.

## 12. Findings

- **IF-READY-02-069:** O desenho evita recursão: seleção default durante reconcile pode mudar a Session revision, mas o comentário e a arquitetura indicam que essa revision é tratada no próximo `LateUpdate`, não recursivamente.
- **IF-READY-02-070:** A occurrence é protegida contra stale reconcile por validação tripla: Activity reference, RuntimeContentOwner/scope e occurrence sequence igual ao participante de readiness.
- **IF-READY-02-071:** O fluxo é idempotente o suficiente para repeated polling: no delta/no-op, already completed, already prepared e same-owner enter não duplicam materialização.
- **IF-READY-02-072:** A barreira contra Actor duplicado é explícita: `records` por `PlayerSlotId` rejeita preparação divergente enquanto aceita idempotência da mesma preparação.
- **IF-READY-02-073:** Há failures explícitos e rollback para enter/reconcile, incluindo rollback de preparação, gameplay e seleção default criada pelo lifecycle.
- **IF-READY-02-074:** A maior limitação para late join é semântica da projeção congelada: “all joined slots” significa “all joined no snapshot de enter”, não “todos que venham a joined no futuro da mesma occurrence”.
- **IF-READY-02-075:** `LogicalActorsPrepared` se materializa como requirement level que exige preparação/materialização antes de completar o participante Required de Activity readiness.

## 13. Questões restantes

- **IF-READY-02-076:** O produto espera que Activity com projection “all joined slots” aceite late join dinâmico na mesma occurrence, ou late join só deve ser suportado para explicit Slots projetados?
- **IF-READY-02-077:** Deve existir uma operação canônica para recriar/substituir a occurrence quando o conjunto de participantes precisa expandir após Activity ativa?
- **IF-READY-02-078:** Quais testes cobrem exatamente o caminho Activity ativa + explicit Slot não Joined + late join + same occurrence Ready?
- **IF-READY-02-079:** Como Loading/WaitCovered devem reagir quando a Activity já foi revelada e a Player readiness contribution completa depois?
- **IF-READY-02-080:** O próximo audit deve separar o que é GameplayReady de `LogicalActorsPrepared`, sem confundir Actor materialization com input/camera/action gates.
