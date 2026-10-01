# RESET-035-B — Auditoria de owner-aware registration

Data: 2026-09-30  
Escopo: auditoria pré-implementação de `Resettable` e registro por owner, sem avançar RESET-035-C.

## Resultado da auditoria

O corte já está parcialmente integrado. `ResettableOwnerRegistrationRuntime` cria subject IDs pela `ResetRegistry`, coleta capabilities sem atravessar outro `Resettable`, rejeita adapter legado na boundary e faz rollback atômico da chamada. A transação Activity já passa o owner de destino e as raízes das cenas materializadas; a transação Route também dispõe desses dados e tem hooks de rollback/release.

### Evidências por fluxo

| Fluxo | Owner e raízes | Prepare/commit | Rollback/release | Avaliação |
|---|---|---|---|---|
| Activity | `ActivityFlowRuntime.Transaction.ExecuteActivityTransitionCoreAsync` usa `runtimeEnterResult.Owner`; `ResolveMaterializedActivitySceneRoots` deriva roots das entries carregadas/already-loaded da composição | `PrepareResettableRegistration` ocorre antes de `transaction.Commit` | `FailBeforeCommitAsync` chama `RollbackTargetResettableRegistration` para o alvo; após commit, transição/clear liberam via `ReleaseResettableRegistrationForPreviousActivity` usando `previousActivity` | owner correto, sem current-owner lookup |
| Route | `RouteLifecycleRuntime` deriva o owner pelo `CreateRouteOwner(route)` do alvo; roots vêm das entries materializadas de `RouteSceneCompositionResult` | prepare ocorre depois dos binders e antes do conteúdo/player entry e da transição da Activity de startup | há rollback explícito nos retornos de falha de player spatial entry e startup Activity; release ocorre para `previousRoute` no exit | owner correto; sem lookup de current Route |
| Host | `FrameworkRuntimeHost.ResettableOwnerRegistration` injeta uma instância no `GameFlowRuntime`, que repassa para Route e Activity | compartilha a `ResetRegistry` existente | sem autoridade global/estática | boundary adequada |

### Componentes de RESET-035-B já integrados

- `Resettable` não expõe authored subject ID nem registra por `OnEnable`.
- `ResettableOwnerRegistrationRuntime` registra com owner explícito Route/Activity e mapeia o scope desse mesmo owner.
- `ResettableBoundary` interrompe a coleta no Resettable aninhado e detecta adapters dentro da boundary ou ancestor adapter com descoberta `Children`.
- Registro de subjects/capabilities acontece antes de marcar `Resettable.IsRegistered`; em falha, remove os subjects desta chamada.
- `TryRollbackOwner` e `TryReleaseOwner` removem evidências locais e registrations por owner.
- Aditivo está coberto: os resolvers coletam as roots de todas as entries `Loaded` e `AlreadyLoaded` da composição, deduplicando scene handles.
- `Tests/Editor/Reset/ResettableOwnerRegistrationTests.cs` cobre contratos locais de identidade, nested boundary, owners, rollback/release e mixed legacy/new. Não é evidência de integração/lifecycle executada no QAFramework.

### Gaps e decisão

Não foi encontrada lacuna de integração de owner-aware registration nas transações existentes. Route prepara antes da entrada de conteúdo e tem rollback nos dois retornos de falha posteriores ao prepare; Activity registra antes do commit e converge suas falhas pré-commit em `FailBeforeCommitAsync`. A mudança de runtime não é necessária para este corte. Permanece pendente a prova de integração/lifecycle no QAFramework; o teste Editor local não a substitui.

### Fora do corte

Não alterar `ResetRegistry`/`ResetExecutor`, Reset membership, `ResetComposition`, requests, StableReference ou runtime materialization. Não remover adapters legados nem afirmar validação integrada sem execução do QAFramework e confirmação Unity.
