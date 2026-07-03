# F13-ADR-OBJECT-001 — Object Entry Foundation

Status: Closed / Applied through F13L  
Fase: F13 — Object Entry Foundation  
Tipo: Core / Object Lifecycle / Diagnostics  
Última atualização: 2026-06-25

---

## 1. Contexto

F11 criou o caminho canônico de Cycle Reset e F12 tornou esse caminho utilizável por QA, triggers e bridges opcionais. Antes de criar Local/Object Reset, o framework precisa de uma representação estável dos objetos lógicos conhecidos pelo lifecycle.

F13 não cria Player, Actor, spawn ou reset físico. Ela cria a fundação passiva de Object Entry: identidade, escopo, requiredness, declaração authored, conjunto imutável, diagnóstico e snapshot runtime controlado.

---

## 2. Dor original

Sem Object Entry, fases posteriores tenderiam a identificar objetos por:

```text
GameObject.name
hierarchy path
scene path
component reference solta
scan global sem owner
```

Esses dados podem aparecer em diagnóstico, mas não podem ser a identidade funcional do objeto.

---

## 3. Decisão consolidada

Object Entry é um catálogo lógico passivo, scoped e owned pelo lifecycle. O formato aceito é:

```text
ObjectEntryId
ObjectEntryScope
ObjectEntrySourceKind
ObjectEntryRequiredness
ObjectEntryDescriptor
ObjectEntrySet
ObjectEntryIssue
ObjectEntryRequest / ObjectEntryResult
ObjectEntryDeclaration
ObjectEntryDeclarationSourceResult
ObjectEntryRuntimeContextSnapshot
ObjectEntryScopedCollectionContext
```

O `FrameworkRuntimeHost` mantém o snapshot canônico atual. Ele invalida o snapshot antes de mudanças de Route/Activity e o reconstrói após transições bem-sucedidas. O snapshot não é registry vivo, binding físico ou inventário de reset.

---

## 4. Implementação real

### F13A — Primitivas de Object Entry

Criou identidade tipada, escopo `Session/Route/Activity`, source kind, requiredness, descriptor, request/result, issues e `ObjectEntrySet` imutável.

Decisões:

```text
ObjectEntryId usa FrameworkIdentityDomain.ObjectEntry.
DisplayName é diagnóstico, não identidade.
ObjectEntrySet rejeita ObjectEntryId duplicado.
Descriptors isolados podem existir sem owner durante testes de primitivas.
Todo descriptor aceito no snapshot autoritativo possui owner tipado coerente com o scope.
```

### F13B — Synthetic Set Smoke

Validou conjunto passivo, required/optional, scopes e rejeição de identidade duplicada sem usar GameObject real.

### F13C — Scene-authored Declaration

Criou `ObjectEntryDeclaration`, componente authored passivo com:

```text
Object Entry Id
Scope
Route Owner ou Activity Owner, conforme o scope
Requiredness
Display Name
```

O componente não faz binding físico e não transforma o próprio GameObject em alvo runtime.

O Fix01 removeu `GetInstanceID()` do caminho de QA para compatibilidade com Unity 6.5.

### F13D/F13E — Declaration Source e diagnostics

`ObjectEntryDeclarationSource` converte declarações em descriptors e produz resultado agregado.

F13E separou corretamente:

```text
declarations
candidatéDescriptors
acceptedDeclarations
rejectedDeclarations
resultStatus
```

Quando há identidade duplicada, o conjunto inteiro é rejeitado e nenhuma declaração entra no `ObjectEntrySet` final.

### F13F — Loaded-scene Integration Smoke

Validou coleta de declarações em cenas carregadas, incluindo declaração em GameObject inativo.

Esse caminho é evidência de integração/QA. Ele ainda não é aceito como fonte autoritativa final do lifecycle porque coleta todas as cenas carregadas sem owner ativo explícito.

### F13G — Runtime Context Snapshot

Criou `ObjectEntryRuntimeContextSnapshot` como fotografia passiva do resultado coletado.

O snapshot permite consulta por id e scope e expõe contagens/diagnósticos. Ele não contém binding para GameObject, Transform ou Component.

### F13H — Runtime Host Snapshot Exposure

`FrameworkRuntimeHost` passou a guardar o último snapshot e expor leitura interna controlada:

```text
RefreshObjectEntryRuntimeContextSnapshot(source)
TryGetObjectEntryRuntimeContextSnapshot(out snapshot)
```

Esse foi o primeiro acesso controlado ao snapshot; F13K completou sua integração automática ao lifecycle.

### F13I — Reconciliation Audit

Reconciliou o ADR original com a implementação real e identificou três lacunas antes do fechamento: owner authored, coleta autoritativa scoped e policy contra snapshot stale.

### F13J — Scoped Ownership

Adicionou owner authored explícito para Route/Activity, validação de domínio por scope e `ObjectEntryScopedCollectionContext`.

O Runtime Host deixou de usar all-loaded scan como fonte autoritativa. A coleta canônica usa somente cenas carregadas pela composição da Route ativa. Declarações pertencentes a owners não ativos são `filtered`, não rejeitadas.

### F13K — Snapshot Lifecycle

O Runtime Host passou a:

```text
invalidar antes de startup/Route/Activity boundaries;
reconstruir após transições bem-sucedidas;
manter snapshot indisponível quando uma falha pode torná-lo stale;
registrar revision e invalidation count para diagnóstico.
```

### F13L — Closure Smoke e QA Hygiene

O smoke final somente leitura validou lifecycle source, owners, domínios, invariantes de contagem e ausência de entries foreign/stale.

No QA Canvas, os sete botões intermediários de Object Entry foram removidos. O painel padrão mantém apenas:

```text
Run Object Entry Foundation Closure Smoke
```

Os runners intermediários permanecem internos para regressão/evidência, sem poluir a superfície normal do painel.

---

## 5. Evidência aceita da F13

### Scene-authored Declaration

```text
objectEntryDeclarations='1'
objectEntries='1'
objectEntryRequired='1'
objectEntryActivity='1'
issues='0'
```

### Declaration Source

```text
resultStatus='Accepted'
declarations='2'
candidatéDescriptors='2'
acceptedDeclarations='2'
rejectedDeclarations='0'
objectEntries='2'
blockingIssues='0'
```

Duplicidade:

```text
resultStatus='Rejected'
acceptedDeclarations='0'
rejectedDeclarations='2'
objectEntries='0'
blockingIssues='1'
```

### Runtime Host Snapshot Exposure

```text
hostSnapshotAvailable='True'
snapshotAvailable='True'
resultStatus='Accepted'
declarations='3'
acceptedDeclarations='3'
rejectedDeclarations='0'
qaRouteFound='True'
qaActivityFound='True'
blockingIssues='0'
```

O total `3` é esperado nessa evidência: duas declarações temporárias do smoke e uma declaração authored real já carregada.

### Scoped Ownership

```text
resultStatus='Accepted'
routeOwner='Route:Assets/Scenes/StartupScene.unity'
activityOwner='Activity:QA Primary Content Activity'
acceptedDeclarations='2'
rejectedDeclarations='0'
filteredDeclarations='2'
foreignActivityFiltered='True'
blockingIssues='0'
```

### Snapshot Lifecycle

```text
targetSnapshotAvailable='True'
restoredSnapshotAvailable='True'
initialRevision='2'
targetRevision='3'
restoredRevision='4'
invalidationDelta='2'
refreshDelta='2'
targetForeignFiltered='True'
restoredForeignFiltered='True'
blockingIssues='0'
```

### Foundation Closure

```text
snapshotAvailable='True'
lifecycleSource='True'
ownerDomainsValid='True'
activeOwnersValid='True'
countInvariant='True'
revision='1'
invalidations='1'
resultStatus='Accepted'
declarations='1'
acceptedDeclarations='0'
rejectedDeclarations='0'
filteredDeclarations='1'
blockingIssues='0'
nonBlockingIssues='0'
```

`acceptedDeclarations='0'` é válido nessa evidência: a única declaração authored carregada pertencia a outro owner e foi corretamente filtrada. O fechamento exige ausência de foreign/stale entries aceitas, não um número mínimo artificial de objetos.

---

## 6. Escopo excluído

F13 continua excluindo:

```text
GameObject/Transform/Component binding funcional
entry execution de objeto
Object Reset ou Component Reset
Player, Actor, NPC ou Projectile
prefab materialization ou spawn
pool rent/return
save/checkpoint restore
registry público mutável
service locator
```

---

## 7. Resoluções de fechamento

### 7.1. Ownership resolvido

Route/Activity declarations possuem owner authored explícito. Session owner é resolvido pelo Application Runtime. O descriptor valida domínio coerente:

```text
Route entry -> FrameworkIdentityDomain.Route
Activity entry -> FrameworkIdentityDomain.Activity
Session entry -> FrameworkIdentityDomain.Session
```

Não existe fallback por nome de GameObject, hierarchy path ou scene name.

### 7.2. Coleta autoritativa scoped

O caminho all-loaded foi mantido apenas como diagnóstico interno dos smokes anteriores. O Runtime Host usa `ObjectEntryScopedCollectionContext`, Route scene composition e `SceneScopedComponentQuery`.

### 7.3. Snapshot lifecycle resolvido

Startup, Route request, Activity request e Activity clear invalidam/reconstroem o snapshot conforme o resultado do lifecycle. Falhas não preservam silenciosamente um snapshot possivelmente stale.

### 7.4. Readiness deliberadamente adiada

O ADR original previa readiness por objeto. Como F13 não executa entrada física nem possui participant runtime, um estado `Ready` seria apenas um espelho artificial de “descriptor aceito”.

Decisão aceita:

```text
F13 fecha catálogo lógico + ownership + snapshot scoped.
Readiness de execução de gameplay fica deferred para F22+ / Future, depois de Gate/Transition/Pause e quando existir Participant Entry real com modelo de gameplay object maduro.
```

---

## 8. Sequência aplicada

| Corte | Status | Objetivo |
|---|---|---|
| F13I | `CLOSED / DOC-AUDIT` | Reconciliou ADR, roadmap e implementação real após F13H. |
| F13J | `CLOSED / PASS` | Owner tipado por scope e coleta autoritativa scoped. |
| F13K | `CLOSED / PASS` | Refresh/invalidation do snapshot nos boundaries de Route/Activity. |
| F13L | `CLOSED / PASS + DOCS` | Smoke final, QA panel hygiene e fechamento documental. |

---

## 9. Fechamento

Critérios satisfeitos:

```text
identidade duplicada continuar bloqueando o set;
owner estiver presente e coerente com Route/Activity/Session scope;
coleta runtime autoritativa não usar all-loaded scan sem filtro;
snapshot tiver policy explícita de refresh/invalidation;
QA provar Route e Activity scope sem foreign/stale entries;
nenhum binding físico, reset, Player/Actor ou spawn entrar na fase.
```

Conclusão:

```text
F13 — CLOSED / APPLIED THROUGH F13L
```

---

## 10. Relação com fases futuras

F13 desbloqueia:

```text
F14 — Local/Object Reset Foundation
F15 — Unity Reset Adapters mínimos
F16 — GameObject Active State Reset Adapter
F22+ / Future - Contextual Reset / Participant readiness after Gate/Transition/Pause
```

F13 não desbloqueia diretamente:

```text
Camera
Audio
Actor gameplay
Pooling consumer
Projectile
Damage
Attributes
Powerups
```
