# ADR — Game Flow Diagnostic Fault Leases

## Decisão

Os faults de QA de Game Flow são planos internos, one-shot e instaláveis somente pela ponte `Immersive.Framework.Editor`. O QA consome apenas `FrameworkGameFlowDiagnosticFaultUtility`, uma scenario fechada e uma lease de leitura; ele não recebe setters de snapshots, delegates ou substituição de authority.

## Authority e isolamento

`FrameworkRuntimeHost` continua dono da composição e `ActivityPlayerLifecycleAdmissionRuntimeContext` continua a authority de lifecycle. A instalação aplica o mesmo port interno ao `GameFlowRuntime` e à authority existente; ela não cria nem troca qualquer runtime de lifecycle. A instalação rejeita request/transaction ativos e uma segunda lease no mesmo Host.

## Checkpoints

Os checkpoints fechados são `CurrentPreparationTokenValidation`, `CurrentOwnershipValidation`, `BeforeCandidateStaging`, `LifecycleRuntimeAvailability`, `BeforeLoadingPresentation`, `AfterCommitBeforeTargetReadiness` e `AfterCandidateOwnershipBeforePreviousCleanup`. Cada scenario pública tem um único checkpoint fixo. A decisão padrão é `NoOpGameFlowDiagnosticFaultPlan.Instance`.

## Semântica

Falhas antes do commit seguem a rejeição/rollback normal da authority e preservam a origem. Falhas posteriores ao commit devem manter o destino autoritativo, com readiness ou cleanup pendente explícitos; rollback não é uma recuperação permitida nesse lado da fronteira.

## Builds de produto

| Scenario | Owner canônico | Fronteira |
| --- | --- | --- |
| `CommittedTargetNotReady` | `GameFlowRuntime` após `StartActivityWithActivationGateAsync` | Target publicado e lifecycle Player committed, antes de `Transition.After` e do sucesso terminal |
| `CommittedFinalizationFailure` | `PlayerActorPreparationRuntimeContext` | Candidate ownership concluída, antes da liberação física do Actor anterior |

`CommittedTargetNotReady` retorna a falha da request sem rollback, republicação da origem ou remoção do target. `CommittedFinalizationFailure` segue o caminho `FailedPreviousActorRelease` / `FailedCommitCleanup`, mantém cleanup pending e permite o retry oficial; como a lease é one-shot, o retry executa a liberação real.

O plano runtime é NoOp. A única instalação vive na assembly Editor e não há asset, menu runtime ou configuração serializada de fault em player builds. A saída do Play Mode libera leases remanescentes e restaura o plano NoOp.

## Post-commit retry and destination projection

`CommittedFinalizationFailure` is recovered through
`IActivityPlayerLifecycleAdmissionRuntime.TryRetryCommitCleanup`.
The lifecycle authority owns the retry, delegates lower-layer cleanup to the
active handoff group, clears `CommitCleanupPending`, and only then finalizes the
canonical lifecycle snapshot. QA does not mutate group, Slot, Actor, or
lifecycle state.

`CommittedTargetNotReady` returns
`FrameworkActivityRequestKind.FailedCommittedTargetNotReady`. The result carries
the real `ActivityFlowStartResult`, marks the commit boundary as reached, and
keeps the destination authoritative. `FrameworkRuntimeHost` projects that
destination into `FrameworkRuntimeState` even though the request did not return
terminal readiness success. The failure is not represented as a rejected
preflight request and does not execute rollback.
