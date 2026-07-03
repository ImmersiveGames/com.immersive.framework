# F14-ADR-RESET-003 — Local/Object Reset Foundation

Status: Closed / Applied through F14H  
Fase: F14 — Local/Object Reset Foundation  
Tipo: Core / Object Lifecycle / Reset  
Última atualização: 2026-06-25 — F14H closure

---

## 1. Contexto

F11/F12 fecharam Cycle Reset como reset lógico de Route/Activity. F13 fechou Object Entry como catálogo lógico owned/scoped, com identidade tipada, coleta limitada às cenas da Route ativa e snapshot lifecycle.

F14 usa essas duas bases para criar reset direcionado a um Object Entry específico. Ainda não há Transform, Rigidbody, Animator, Player, Actor, pooling ou gameplay reset concreto.

---

## 2. Problema

Cycle Reset responde:

```text
qual ciclo de Route/Activity deve receber uma solicitação de reset?
```

Object Reset responde:

```text
qual Object Entry atual deve receber participantes de reset?
```

Sem essa separação, o framework tenderia a:

```text
procurar GameObjects durante o request;
usar nome/path como target;
misturar reset de objeto com reload de cena;
acoplar Player/Actor ao core;
transformar Cycle Reset em dispatcher físico de componentes.
```

---

## 3. Decisão central

F14 define **Object Reset** como orquestração lógica direcionada a um `ObjectResetTarget` derivado de um `ObjectEntryDescriptor` atualmente aceito no snapshot do Runtime Host.

```text
ObjectEntryRuntimeContextSnapshot
  -> resolve ObjectResetTarget
  -> resolve participants por source explícito
  -> build ObjectResetPlan determinístico
  -> execute participants fornecidos
  -> aggregate ObjectResetResult
```

Object Reset não descobre GameObjects, Components ou participants por scan global.

---

## 4. Target canônico

O target de F14 contém somente identidade lógica já fechada na F13:

```text
ObjectEntryId
ObjectEntryScope
OwnerIdentity
```

Regras:

```text
ObjectEntryId deve existir no snapshot atual.
Scope deve ser Session, Route ou Activity.
OwnerIdentity deve existir e usar o domain correspondente ao scope.
O descriptor resolvido deve continuar pertencendo ao owner ativo.
Target foreign, filtered, ausente ou stale é rejeitado.
```

O target não pode ser:

```text
GameObject
Transform
Component
GameObject.name
scene path
hierarchy path
string livre sem domínio
LocalContentIdentity ou RuntimeContentHandle usados como union improvisada
```

`LocalContentIdentity` e `RuntimeContentHandle` podem ganhar adapters explícitos no futuro, mas não fazem parte do target canônico inicial.

---

## 5. Contratos aprovados

F14 pode introduzir:

```text
ObjectResetTarget
ObjectResetRequest
ObjectResetPolicy
ObjectResetStatus
ObjectResetIssue / ObjectResetIssueKind
ObjectResetParticipantId
ObjectResetParticipantRequiredness
ObjectResetParticipantDescriptor
IObjectResetParticipant
IObjectResetParticipantSource
ObjectResetContext
ObjectResetParticipantResult
ObjectResetParticipantEntry
ObjectResetPlan / ObjectResetPlanStatus
ObjectResetResult
ObjectResetRuntime
EmptyObjectResetParticipantSource
```

Não será criada `ILocalResetParticipant` em paralelo.

Decisão:

```text
IObjectResetParticipant é o único contrato canônico.
"Local" é uma leitura de scope/owner/target, não outro tipo de participant.
```

---

## 6. Request e resolução

`ObjectResetRequest` deve carregar:

```text
ObjectResetTarget
ObjectResetPolicy
Source
Reason
```

O Runtime Host deve resolver o target contra o snapshot de Object Entry atual antes de solicitar participants.

Rejeições mínimas:

```text
snapshot indisponível;
ObjectEntryId ausente;
scope diferente;
owner diferente;
owner domain inválido;
request default/inválida;
```

Não existe fallback para procurar o objeto em cena.

---

## 7. Participant e source

`IObjectResetParticipant` segue o padrão de Cycle Reset:

```text
GetObjectResetDescriptor()
ResetObject(ObjectResetContext context)
```

O descriptor carrega:

```text
ParticipantId
ObjectResetTarget
Requiredness
Order
DisplayName diagnóstico
```

`IObjectResetParticipantSource` recebe o request já resolvido e retorna somente participants conhecidos. Ele não pode:

```text
usar FindObjectsByType;
consultar service locator;
instanciar ou destruir objetos;
capturar baseline;
executar reset durante a resolução;
recarregar cena;
restaurar save/snapshot;
fazer pool return.
```

O core ainda deve rejeitar participant nulo, descriptor inválido, target foreign e ParticipantId duplicado.

---

## 8. Requiredness, ordering e policy

Participants são `Required` ou `Optional`.

Regras iniciais:

```text
ordenação: Order crescente, depois ParticipantId estável;
falha Required: blocking issue;
falha Optional: warning quando policy permitir;
nenhum participant: SucceededNoParticipants quando policy permitir;
participant foreign: não executa;
exception: convertida em result/issue estruturado.
```

`ObjectResetPolicy` inicial controla somente orquestração:

```text
AllowNoParticipants
TreatOptionalFailuresAsWarnings
```

Não entram flags de Transform, physics, animator, player, save ou pooling.

---

## 9. Plan e execução

`ObjectResetPlan` é imutável e separa planejamento de execução.

Status conceituais:

```text
Unknown
Planned
SkippedNoParticipants
RejectedInvalidRequest
RejectedInvalidTarget
RejectedInvalidParticipants
```

`ObjectResetRuntime` executa somente entries já aceitas no plan e agrega resultados.

F14 usa uma única chamada lógica de execução por participant. Não cria prematuramente fases públicas `PrepareReset/ApplyReset/PostReset`.

O modelo preserva evolução futura por composição de participants e ordering. Fases adicionais só entram quando um adapter real provar a necessidade.

---

## 10. Reset Baseline

Reset Baseline continua sendo conceito diferente de Save Snapshot:

```text
Reset Baseline = referência local ao estado base usado por um adapter de reset.
Save Snapshot = payload versionado e persistível.
```

F14 não cria payload genérico, dictionary, object blob ou baseline registry.

Decisão:

```text
O core F14 orquestra target e participants.
O adapter concreto da F15 define/captura o baseline que sabe aplicar.
```

Fontes futuras possíveis permanecem:

```text
authored baseline
captured-on-entry baseline
runtime-materialization baseline
```

---

## 11. Relação com Cycle Reset

Cycle Reset e Object Reset continuam contratos diferentes:

| Aspecto | Cycle Reset | Object Reset |
|---|---|---|
| Target | Route/Activity cycle | Object Entry específico |
| Source atual | `ICycleResetParticipantSource` | `IObjectResetParticipantSource` |
| Identidade | Cycle scope + Route/Activity | ObjectEntryId + owner + scope |
| Efeito físico no core | Nenhum | Nenhum |

F14 não altera automaticamente o executor de Cycle Reset para chamar Object Reset.

Integração entre ambos só pode ocorrer por adapter/source explícito depois que participants reais existirem, sem recursão e sem transformar Object Reset em scene reload.

---

## 12. Diagnostics e QA

Diagnostics mínimos:

```text
targetObjectEntryId
targetScope
targetOwner
source
reason
policy
participants
required
optional
succeeded
skipped
failed
status
blockingIssues
nonBlockingIssues
```

Smokes previstos:

```text
target válido + participants sintéticos ordenados;
target ausente/foreign rejeitado;
ParticipantId duplicado rejeitado;
optional skipped/failed gera warning conforme policy;
required failed bloqueia;
no participants produz SucceededNoParticipants;
Runtime Host resolve target somente pelo snapshot atual.
```

---

## 13. Escopo excluído

F14 exclui:

```text
GameObject/Transform/Component target público
TransformResetParticipant real
RigidbodyResetParticipant real
AnimatorResetParticipant real
Player/Actor/NPC reset
health/attributes/powerups/projectile reset
pool return
scene reload
save/checkpoint restore
baseline payload genérico
authoring de baseline concreto
public mutable registry
service locator
```

---

## 14. Cortes aprovados

| Corte | Objetivo |
|---|---|
| F14A | Reconciliar e aceitar este ADR após o fechamento real da F13. |
| F14B | Primitivas puras: target, request, policy, status e issues; synthetic target smoke. |
| F14C | Participant descriptor/interface/source, collection validation e ordering. |
| F14D | Plan, context, participant result, runtime executor e aggregate result com probes sintéticos. |
| F14E | Runtime Host resolve target contra snapshot atual e executa source injetada; smoke de host. |
| F14F | Trigger público com result UX para `ObjectEntryDeclaration`/`ObjectEntryId`. |
| F14G | Bridge opcional de UnityEvent para resultado do trigger. |
| F14H | Closure smoke canônico, QA panel hygiene, documentação e fechamento da F14. |

Cada corte deve preservar compile/smoke antes do próximo.

---

## 15. Critério de fechamento

F14 só fecha quando:

```text
target vier exclusivamente de Object Entry atual;
foreign/missing/stale target for rejeitado;
participant source for explícita e sem scan global;
ordering/requiredness/duplicaté validation estiverem provados;
runtime agregar results/issues sem executar Unity diretamente;
Cycle Reset permanecer separado;
nenhum adapter Unity ou gameplay reset entrar na fase;
QA Canvas manter apenas o smoke canônico de fechamento ao final.
```

---

## 16. Relação com fases futuras

F14 desbloqueia:

```text
F15 — Unity Reset Adapters mínimos
F16 — GameObject Active State Reset Adapter
F22+ / Future - Contextual Reset / Participant readiness after Gate/Transition/Pause
```

F14 mantém bloqueado:

```text
Rigidbody/Animator reset concreto
Player/Actor reset concreto
pool return
Projectile/Damage/Attributes/Powerups
Save/checkpoint restore
```


---

## 17. Fechamento aplicado em F14H

F14 fechou como foundation lógica de Object Reset.

Implementado:

```text
ObjectResetTarget / ObjectResetRequest / ObjectResetPolicy / ObjectResetResult;
ObjectResetTargetResolver contra ObjectEntryRuntimeContextSnapshot atual;
IObjectResetParticipant e IObjectResetParticipantSource;
ObjectResetPlan e ObjectResetRuntime determinísticos;
FrameworkRuntimeHost.RequestObjectResetAsync(...);
ObjectResetTrigger público;
ObjectResetTriggerUnityEventBridge opcional;
QA Object Reset Foundation Closure Smoke.
```

Smoke canônico final:

```text
Run Object Reset Foundation Closure Smoke
```

O smoke final valida, sem reset físico Unity:

```text
snapshot atual disponível;
target coletado e resolvido por Object Entry;
Runtime Host executa participants sintéticos required/optional;
trigger público completa via snapshot atual;
bridge opcional recebe eventos corretos;
blockingIssues = 0;
nonBlockingIssues = 0.
```

Botões intermediários de Object Reset foram removidos do painel QA. Os métodos/runners intermediários permanecem internos para regressão controlada.

F14 continua excluindo explícitamente:

```text
Transform reset real;
Rigidbody reset real;
Animator reset real;
GameObject active reset real;
Player/Actor reset real;
pool return;
gameplay state reset;
save/checkpoint restore.
```

Esses itens pertencem a F15+ mediante adapters explícitos.
