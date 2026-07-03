# F12-ADR-RESET-002 — Cycle Reset Integration & Authoring UX

Status: Closed / Applied through F12E  
Fase: F12 — Cycle Reset Integration & Authoring UX  
Tipo: Core / Tooling / Authoring / QA  
Ultima atualizacao: 2026-06-25

---

## 1. Contexto

F11 criou o contrato e o executor mínimo de Cycle Reset. F12 torna esse caminho usavel dentro do fluxo real do framework, sem ainda introduzir reset local/object ou gameplay.

A F12 existe para impedir que Cycle Reset fique como contrato técnico sem superfície de uso, validação ou authoring claro.

---

## 2. Dor original

O usuario precisa acionar reset de ciclo a partir de UI, QA, triggers de cena ou ferramentas de desenvolvimento, com logs consistentes e sem criar caminhos paralelos.

Exemplos:

```text
Botao QA: Reset Activity atual.
Botao QA: Reset Route atual.
Componente authored: solicitar reset da Activity.
Componente authored: solicitar reset da Route.
```

---

## 3. Decisão

F12 integra Cycle Reset ao runtime público do framework por superficies authoring/dev controladas.

F12 não cria reset real de objetos. Ela apenas expoe o caminho canônico criado na F11 para uso e validação.

A decisão central de UX e:

```text
Trigger e o componente principal.
Unity Event Bridge e opcional.
```

Botoes/UI podem chamar diretamente os metodos do trigger. A bridge so existe quando o objeto precisa de callbacks UnityEvent de resultado no Inspector.

---

## 4. Escopo incluido

F12 inclui:

```text
ActivityCycleResetTrigger
RouteCycleResetTrigger
FrameworkQaCanvas reset buttons/smokes
Trigger result UX
Unity Event Bridges opcionais
Authoring validation mínima de triggers
Cycle Reset smoke criteria
Diagnostics de request concluido
```

F12 tambem garante que os triggers chamem o owner correto do runtime, e não executem reset localmente.

---

## 5. Escopo excluido

F12 exclui:

```text
Object Reset
Component Reset
Player Reset
Actor Reset
TransformResetParticipant
RigidbodyResetParticipant
AnimatorResetParticipant
ResetBaseline authoring real
Save/checkpoint restore
Pool return
Scene reload
```

---

## 6. Modelo conceitual

F12 adiciona superfície de uso para o contrato F11:

```text
Scene/UI/QA Trigger
  -> FrameworkRuntimeHost
    -> GameFlowRuntime
      -> RouteLifecycleRuntime
        -> CycleResetRuntime
          -> participants/probes
          -> result
```

Os triggers sao request sources, não owners de reset.

A bridge e apenas um adapter de evento:

```text
CycleResetTriggerEvent typed
  -> UnityEvent callbacks opcionais
```

A bridge não cria request novo, não executa reset e não substitui o trigger.

---

## 7. Contratos esperados

### ActivityCycleResetTrigger

Componente público para scene/UI solicitar reset da Activity ativa.

Requisitos:

```text
Não precisa conhecer Activity asset alvo.
Usa Activity ativa do runtime.
Falha explicitamente se não houver runtime ou Activity ativa.
Possui Source/Reason para diagnostics.
Expor último resultado para Inspector/UX.
```

### RouteCycleResetTrigger

Componente público para scene/UI solicitar reset da Route ativa.

Requisitos:

```text
Não precisa conhecer Route asset alvo.
Usa Route ativa do runtime.
Pode expor Include Active Activity quando a policy permitir.
Falha explicitamente se não houver runtime ou Route ativa.
Expor último resultado para Inspector/UX.
```

### Unity Event Bridges

As bridges sao opcionais.

Modelo correto:

```text
1 RouteCycleResetTrigger pode ter 0 ou 1 RouteCycleResetTriggerUnityEventBridge.
1 ActivityCycleResetTrigger pode ter 0 ou 1 ActivityCycleResetTriggerUnityEventBridge.
A bridge fica no mesmo GameObject do trigger.
```

Usar bridge apenas quando o GameObject precisa expor callbacks de resultado via Inspector/UnityEvent.

Exemplos de callbacks:

```text
Request Submitted
Request Succeeded
Request Succeeded With Participants
Request Succeeded No Participants
Request Completed With Warnings
Request Ignored
Request Failed
Request Completed
```

---

## 8. Authoring UX

Inspector deve explicar de forma direta:

```text
Este componente solicita reset do ciclo ativo.
Ele não recarrega cena.
Ele não troca Route/Activity.
Ele não reseta objeto específico.
Ele não restaura save/checkpoint.
```

Campos recomendados:

```text
Reason
Include Active Activity, somente para Route reset se aplicável
```

O Inspector/guia deve deixar explícito:

```text
Unity Event Bridge is optional.
Add it only when this trigger needs Inspector callbacks for request results.
Buttons can call the trigger method directly without a bridge.
```

O caminho simples para game designers deve aparecer primeiro:

```text
Button.onClick -> RouteCycleResetTrigger.RequestRouteCycleReset()
Button.onClick -> ActivityCycleResetTrigger.RequestActivityCycleReset()
```

---

## 9. Diagnostics e validação

F12 expande QA para validar o caminho real de request e a UX dos triggers.

Smokes aceitos:

```text
Run Cycle Reset Trigger Smoke
Run Cycle Reset Bridge Smoke
```

Logs/resultados devem deixar explícito:

```text
requestSource
requestReason
scope
activeRoute
activeActivity
resultStatus
participantCounts
blockingIssues
nonBlockingIssues
```

Authoring validation deve reportar contadores de:

```text
routeCycleResetTriggers
activityCycleResetTriggers
```

O validator não deve exigir bridges.

Warnings aceitaveis:

```text
Reason usa vocabulario futuro de object/player/component/etc.
Route e Activity trigger no mesmo GameObject, se isso gerar ambiguidade de authoring.
Trigger inativo na hierarquia, se o smoke/QA depender dele ativo.
```

---

## 10. Evidência aceita da F12

### F12A — Authoring Guardrails

```text
QA Authoring Validation completed.
routeCycleResetTriggers='1'
activityCycleResetTriggers='1'
issues='0'
```

### F12C — Trigger Smoke

```text
QA Smoke completed. name='Cycle Reset Trigger Smoke'.
Route trigger: status='SucceededNoParticipants', participants='0', blockingIssues='0'.
Activity trigger: status='SucceededNoParticipants', participants='0', blockingIssues='0'.
```

### F12D — Bridge Smoke

```text
QA Smoke completed. name='Cycle Reset Bridge Smoke'.
Route bridge: submittedEvents='1', succeededEvents='1', succeededNoParticipantsEvents='1', completedEvents='1', failedEvents='0', ignoredEvents='0'.
Activity bridge: submittedEvents='1', succeededEvents='1', succeededNoParticipantsEvents='1', completedEvents='1', failedEvents='0', ignoredEvents='0'.
```

`SucceededNoParticipants` e resultado esperado nesta fase para triggers reais sem participantes físicos.

---

## 11. Consequencias

### Positivas

- Cycle Reset passa a ser utilizável no Unity.
- QA consegue validar a mecanica sem objetos reais.
- Triggers tem feedback de resultado suficiente para Inspector/UI.
- Bridges opcionais permitem callbacks de resultado sem obrigar toda UI a usar bridge.
- O core não fica dependente de implementação futura de Player.

### Custos

- Ainda não há reset local útil para gameplay.
- A UX precisa ser explícita para não prometer reset de objeto.
- Bridges aumentam a superfície authoring, entao precisam permanecer claramente opcionais.

---

## 12. Guardrails

- Triggers não podem executar reset diretamente.
- QA Canvas não pode virar owner de reset.
- Inspector não pode sugerir que reset recarrega cena ou restaura save.
- Não adicionar campos de Player/Actor/Component nos triggers de cycle reset.
- Não implementar fallback silencioso se não houver runtime ativo.
- Validator não deve exigir bridge.
- Bridge não deve existir como segundo caminho de request.
- Bridge não deve ser documentada como obrigatória.
- Guia deve mostrar primeiro o caminho simples sem bridge.

---

## 13. Relacao com fases futuras

F12 desbloqueia uso pratico do Cycle Reset enquanto F13/F14 ainda não existem.

F12 não desbloqueia gameplay reset diretamente. Object Reset e reset de objetos reais continuam para F14/F15/F16.

F13 deve iniciar Object Entry Foundation sem reaproveitar Cycle Reset como atalho para reset local.

---

## 14. Fechamento auditado

F12 foi revalidada contra o package atualizado antes da reconciliação da F13.

Confirmado no codigo:

```text
RouteCycleResetTrigger e ActivityCycleResetTrigger chamam o Runtime Host.
Triggers expoem último status, resultado, participants e issues.
Bridges apenas observam CycleResetTriggerEvent; não criam segundo request path.
QA Canvas expoe Trigger Smoke e Bridge Smoke.
Authoring Validation conta triggers de Route e Activity sem exigir bridge.
Inspectors explicam que Cycle Reset não e reset de objeto, save ou scene reload.
```

Confirmado na documentacao de uso:

```text
o caminho simples Button -> Trigger aparece antes da bridge;
bridge e apresentada como opcional;
SucceededNoParticipants e explicado como resultado valido nesta fase;
Object Reset permanece explicitamente fora de F12.
```

Conclusao:

```text
F12 — CLOSED / APPLIED
```
