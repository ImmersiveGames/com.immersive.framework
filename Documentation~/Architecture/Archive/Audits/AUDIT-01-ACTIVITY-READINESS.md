# AUDIT-01 — Activity Readiness

## 1. Resumo

- **IF-READY-01-001:** A occurrence de readiness nasce como `ActivityReadinessOccurrence(activity, transition.Sequence)` durante a transação de entrada de Activity. Ela não é derivada apenas do asset: a identidade combina referência da `ActivityAsset` com a sequência da transição.
- **IF-READY-01-002:** A autoridade atual de readiness fica em `ActivityFlowRuntime`, que mantém `_currentReadinessOccurrence`, `_pendingAuthorableReadinessState`, `_currentAuthorableReadinessState` e publica `ActivityReadinessUpdate` somente quando a occurrence bate com a Activity e sequência atuais.
- **IF-READY-01-003:** O estado agregado exposto é `ActivityReadinessState`; ele carrega status compacto (`None`, `Ready`, `NotReady`), baseline técnico, contadores Required/Optional e diagnósticos.
- **IF-READY-01-004:** Participantes authorable são componentes `ActivityReadinessParticipant` descobertos em cenas de Activity content e participantes explícitos host-scoped fornecidos por `IActivityReadinessParticipantSource`.
- **IF-READY-01-005:** O conjunto de participantes fica congelado quando `BeginPendingAuthorableReadiness` cria o `ActivityReadinessOccurrenceState`: ele copia os participantes válidos para entradas internas e começa o tracking da occurrence.
- **IF-READY-01-006:** `Required` bloqueia `Ready` enquanto estiver `Idle`/`Preparing`, `Failed` ou `Released`; `Optional` aparece nos contadores, mas não entra em `IsSatisfied` nem em `TerminalBlockingIssueCount`.
- **IF-READY-01-007:** A agregação exige duas condições para `Ready`: baseline técnico já `Ready` e contribuição authorable satisfeita. Caso contrário, o agregado fica `NotReady`.
- **IF-READY-01-008:** Estados equivalentes de preparação/falha/cancelamento/invalidação aparecem em camadas diferentes: participantes têm `Idle`, `Preparing`, `Completed`, `Failed`, `Released`; occurrence state tem `Pending`, `Current`, `Invalidated`; waits retornam `Ready`, `Failed`, `Invalidated`, `Cancelled` e `Superseded`.
- **IF-READY-01-009:** Occurrences antigas são invalidadas/released em substituição, clear, reentry ou falha antes de commit. Mesmo que um participante antigo mude depois, a assinatura foi removida e/ou a occurrence não bate mais com `_currentReadinessOccurrence`, então ele não deve continuar contribuindo para o agregado atual.
- **IF-READY-01-010:** `LogicalActorsPrepared` pertence ao domínio Player Participation. Ele entra nesta camada apenas se essa projeção materializa participantes explícitos de Activity readiness; semanticamente não é um estado nativo de `ActivityReadinessStatus`.

## 2. Arquivos/classes relevantes

| ID | Arquivo/classe | Papel |
| --- | --- | --- |
| IF-READY-01-011 | `ActivityFlowRuntime` | Autoridade runtime da Activity atual, occurrence atual, estados authorable pending/current e publicação de updates. |
| IF-READY-01-012 | `ActivityFlowRuntime.Transaction` | Cria a occurrence por transição, constrói baseline técnico, promove pending para current e invalida occurrences antigas. |
| IF-READY-01-013 | `ActivityReadinessOccurrence` | Identidade de occurrence: Activity asset + sequência de transição. |
| IF-READY-01-014 | `ActivityReadinessOccurrenceState` | Snapshot mutable controlado pela runtime para uma occurrence, com lifecycle pending/current/invalidated e entradas dos participantes. |
| IF-READY-01-015 | `ActivityReadinessState` | Snapshot agregado imutável publicado/guardado no resultado da Activity. |
| IF-READY-01-016 | `ActivityReadinessRecomposer` | Combina baseline técnico e contribuição authorable. |
| IF-READY-01-017 | `ActivityReadinessParticipantSource` | Descobre participantes em Activity content e mescla fonte explícita host-scoped. |
| IF-READY-01-018 | `ActivityReadinessParticipant` | Componente authorable/runtime que contribui com `Preparing`, `Completed`, `Failed` ou `Released`. |
| IF-READY-01-019 | `ActivityFlowRuntime.EntryReadinessWait` / `ActivityEntryReadinessWaiter` | Espera por terminalidade da occurrence resolvendo pending/current pelo par Activity + sequência. |
| IF-READY-01-020 | `ActivityFlowRuntime.PlayerReadinessSource` | Ponte estreita para instalar fonte explícita de participantes de readiness. |
| IF-READY-01-021 | `PlayerParticipationRequirementLevel` | Define `LogicalActorsPrepared`, fora do enum nativo de Activity readiness. |

## 3. Autoridade de readiness

- **IF-READY-01-022:** A autoridade é `ActivityFlowRuntime`. Ela instancia um `ActivityReadinessParticipantSource`, mantém estados authorable pending/current e guarda o resultado atual da Activity.
- **IF-READY-01-023:** A occurrence current exposta por `CurrentOccurrence` é `_currentReadinessOccurrence`; o contexto atual só é válido quando há Activity ativa, occurrence válida e a Activity do resultado é a Activity ativa.
- **IF-READY-01-024:** `TryPublishPostTransitionReadiness` rejeita updates se a occurrence fornecida não bate com a occurrence current, se a Activity não está ativa ou se o `readinessState.Activity` não é a Activity ativa. Portanto, publishers externos não decidem readiness diretamente; eles apenas disparam mudanças em participantes que a autoridade aceita se a occurrence ainda for válida.

## 4. Modelo de participantes

- **IF-READY-01-025:** `ActivityReadinessParticipant` tem identidade (`participantId`), requiredness (`Required`/`Optional`), ordem, eventos Unity de início/release e diagnósticos de `state`, `lastReason` e `occurrence`.
- **IF-READY-01-026:** Como `IActivityContentExecutionParticipant`, ele retorna descritores Required ou Optional, mas sua execução de content é no-op: preparação/release são propriedade do tracker occurrence-scoped, evitando começar preparação duas vezes.
- **IF-READY-01-027:** Descoberta combina duas origens: componentes em Activity content scope e uma fonte explícita configurável. Cada participante precisa de ID não vazio, requiredness válido e ID único no conjunto combinado.
- **IF-READY-01-028:** Quando o tracking começa, a fonte cancela/release o tracking anterior, registra `StateChanged` nos participantes válidos e chama `BeginPreparation(occurrence)` em cada um.
- **IF-READY-01-029:** `CompletePreparation` e `FailPreparation` só mudam para terminal se o participante ainda estiver `Preparing`; uma tentativa tardia em outro estado vira diagnóstico `LateCompletionRejected` e dispara `StateChanged`, mas não altera para Completed/Failed.

## 5. Agregação

- **IF-READY-01-030:** `ActivityReadinessOccurrenceState` copia cada participante para um `ParticipantEntry` contendo referência, ID, requiredness, state e reason. A contribuição é recalculada a partir dessas entradas.
- **IF-READY-01-031:** `Idle` e `Preparing` contam como pending; `Completed` conta como completed; `Failed` como failed; `Released` como released, separados em Required e Optional.
- **IF-READY-01-032:** Com zero participantes, a contribuição authorable é satisfeita com reason `NoParticipants`.
- **IF-READY-01-033:** Com participantes, `IsSatisfied` exige `requiredFailedCount == 0`, `requiredReleasedCount == 0` e `requiredPendingCount == 0`. Optional pending/failed/released não impede satisfação.
- **IF-READY-01-034:** `ActivityReadinessRecomposer` soma os blocking issues técnicos com failures/releases Required e define `Ready` apenas quando `technicalBaseline.IsReady && authorableContribution.IsSatisfied`.
- **IF-READY-01-035:** O diagnóstico prioriza baseline técnico bloqueado, depois falha/release Required, depois Required pending, depois `Ready` ou `BaselineReady`.

## 6. Lifecycle da occurrence

- **IF-READY-01-036:** A occurrence é criada no início da transação de entrada como pending, antes do commit final, usando a `Sequence` da transação.
- **IF-READY-01-037:** Depois que cenas, scope, content execution e releases anteriores terminam, a runtime constrói o baseline técnico, cria o pending authorable state, recompõe o agregado e só então grava `_currentReadinessOccurrence`, `SetCurrentActivityContext` e promove pending para current.
- **IF-READY-01-038:** Durante substituição de Activity, a runtime invalida a current authorable readiness antes de executar exit do content/participantes anteriores.
- **IF-READY-01-039:** Durante clear explícito, a runtime põe a Activity como `None`, limpa `_currentReadinessOccurrence` e contexto atual, e invalida readiness current com motivo `ActivityClear`.
- **IF-READY-01-040:** Falhas antes de commit invalidam pending readiness e fazem rollback/compensação; exceções pós-commit também invalidam pending readiness.
- **IF-READY-01-041:** Waiters resolvem somente pending/current não invalidated que casem Activity + sequência; se a occurrence não está disponível, retornam invalidation `OccurrenceUnavailable`.

## 7. `LogicalActorsPrepared`, se aplicável

- **IF-READY-01-042:** `LogicalActorsPrepared` é um nível de requisito em `PlayerParticipationRequirementLevel`, entre `SelectedActors` e `GameplayReady`.
- **IF-READY-01-043:** Activity readiness não tem status chamado `LogicalActorsPrepared`: seus status nativos são `None`, `Ready`, `NotReady`, enquanto os participantes expõem `Idle`, `Preparing`, `Completed`, `Failed`, `Released`.
- **IF-READY-01-044:** A integração direta visível nesta camada é a fonte explícita configurada por `SetActivityReadinessParticipantSource`. Assim, qualquer contribuição baseada em Player Participation entra como participante authorable/runtime, não como regra hardcoded no recomposer.
- **IF-READY-01-045:** Sem aprofundar Player provisioning/join, a leitura segura é: `LogicalActorsPrepared` pertence ao modelo de Player Participation e pode ser projetado para Activity readiness por participantes explícitos; a agregação de Activity continua genérica por Required/Optional e estados de participante.

## 8. Failure/Invalidation

- **IF-READY-01-046:** Failure técnico: content ausente, falha de lifecycle enter, falha/rejeição de participant execution ou target enter incompleto adicionam blocking issues ao baseline e produzem `NotReady`.
- **IF-READY-01-047:** Failure authorable: `Required` em `Failed` ou `Released` adiciona terminal blocking issue; `Optional` em `Failed`/`Released` é diagnóstico/contagem, mas não bloqueia Ready.
- **IF-READY-01-048:** Invalidation de occurrence: `ActivityReadinessOccurrenceState.Invalidate` muda lifecycle para `Invalidated`, remove referências dos participantes nas entradas internas e publica `Changed`.
- **IF-READY-01-049:** Release de tracking: `ActivityReadinessParticipantSource.ReleaseTracked` desassina `StateChanged`, limpa occurrence/sink/lista e chama `Release(reason)` nos participantes previamente tracked.
- **IF-READY-01-050:** Cancelled/Superseded vivem na camada de espera/orquestração de Entry Readiness. O wait status é mapeado para execution status, preservando `Cancelled` e tratando supersession separadamente de cancelamento genérico.

## 9. Findings

- **IF-READY-01-051:** O modelo tem boa defesa contra contribuição stale: identidade por asset reference + sequência, invalidation de state, release/desassinatura da fonte e guarda em `TryPublishPostTransitionReadiness`.
- **IF-READY-01-052:** O termo `Ready` é estritamente agregado: baseline técnico deve estar pronto e todos os Required devem estar completed; Optional não bloqueia.
- **IF-READY-01-053:** `Released` de Required é tratado como terminal failure para readiness. Isso é importante em substituição/clear porque release é um estado de teardown, não sucesso.
- **IF-READY-01-054:** O conjunto de participantes não parece aceitar late registration dentro da mesma occurrence; mudanças pós-captura só contam se vierem de objetos já copiados para `ParticipantEntry`.
- **IF-READY-01-055:** Há separação clara entre execução de Activity content e readiness authorable: o participante implementa o contrato de execution, mas a execução é no-op para readiness.
- **IF-READY-01-056:** `LogicalActorsPrepared` não deve ser tratado como status de Activity readiness na próxima auditoria; deve ser investigado como uma projeção de Player Participation para participante explícito/host-scoped.

## 10. Perguntas que precisam da próxima auditoria

- **IF-READY-01-057:** Qual runtime concreta instala `IActivityReadinessParticipantSource` e com quais participantes para Player Participation?
- **IF-READY-01-058:** Como a contribuição de `LogicalActorsPrepared` é atualizada para `Completed`, `Failed` ou `Released` sem investigar ainda Player Join/provisioning em profundidade?
- **IF-READY-01-059:** Existem cenários de late join/reconciliation que tentam alterar readiness depois do congelamento do conjunto? Se sim, isso cria uma nova occurrence ou atualiza apenas participantes já capturados?
- **IF-READY-01-060:** Como Loading/WaitCovered consomem `ActivityEntryReadinessWaitResult` e distinguem `Superseded`, `Cancelled`, `Failed` e `Invalidated`?
- **IF-READY-01-061:** Há testes cobrindo stale participant changes após `ActivityReplacement` e `ActivityClear` para provar que não publicam `ActivityReadinessUpdate`?
