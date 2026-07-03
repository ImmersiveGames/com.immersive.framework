# F11-ADR-RESET-001 — Cycle Reset Foundation

Status: Applied through F11G  
Fase: F11 — Cycle Reset Foundation  
Tipo: Core / Lifecycle / Reset  
Última atualização: 2026-06-25

---

## 1. Contexto

O Immersive Framework nasceu para resolver duas dores principais:

1. organizar a execução das etapas do jogo em ordem previsível e controlada;
2. oferecer um caminho único e canônico para resetar estado ativo do jogo.

Até a F10, o framework consolidou owners, identidade, Route, Activity, content, contribution, release, runtime materialization, Content Anchor binding e Activity Content Execution. Isso resolve bem a primeira dor: o ciclo de entrada/saída de Route e Activity agora possui owners e ordem previsíveis.

A próxima frente precisa atacar a segunda dor sem antecipar gameplay. Portanto, F11 não deve introduzir Player, Actor, Camera, Audio, Pooling, Projectile, Damage, Powerups ou qualquer sistema concreto de jogo. F11 deve criar o formato canônico de reset de ciclo.

---

## 2. Dor original

O usuário precisa poder reiniciar uma fase ou etapa jogável sem depender de caminhos paralelos, reload de cena, métodos ad hoc por componente ou lógica espalhada em gameplay scripts.

Exemplos de intenção de design:

```text
Resetar a Activity atual.
Resetar a Route/fase atual.
Garantir que todos os participantes ativos recebam uma solicitação de reset pelo mesmo caminho.
Ter diagnostics claros sobre quem participou, quem falhou e quem foi ignorado.
```

Ainda não há objeto real completo, como Player/Actor, que justifique reset local profundo. Por isso, o primeiro momento deve criar apenas o reset de ciclo com mocks/probes para validar o core.

---

## 3. Decisão

F11 define **Cycle Reset** como capacidade de core lifecycle.

Cycle Reset cobre somente:

```text
Route Cycle Reset
Activity Cycle Reset
```

Cycle Reset não cobre:

```text
Object Reset
Component Reset
Player Reset
Actor Reset
Gameplay Reset
Pool Return
Scene Reload
Save Restore
Snapshot Restore
```

O core deve orquestrar:

```text
request
scope
policy
plan
ordering
participant dispatch
result aggregation
diagnostics
```

Cada participante decide como resetar seu próprio estado. O core não conhece aceleração, vida, powerups, Animator, Rigidbody, Player, Actor ou gameplay state.

---

## 4. Escopo incluído

F11 inclui:

```text
CycleResetRequest
CycleResetScope
CycleResetPolicy
CycleResetReason / source / reason
CycleResetPlan
CycleResetResult
CycleResetIssue
CycleResetParticipantDescriptor
ICycleResetParticipant
CycleResetRuntime / executor mínimo
Route cycle reset path
Activity cycle reset path
QA mocks/probes para smoke
Diagnostics estruturados de reset
```

F11 deve permitir que o framework responda:

```text
Qual reset foi solicitado?
Qual escopo foi alvo?
Qual Route estava ativa?
Qual Activity estava ativa?
Qual policy foi usada?
Quais participantes foram chamados?
Quais participantes foram skipped?
Quais participantes falharam?
O reset geral passou, passou com warnings ou falhou?
```

---

## 5. Escopo excluído

F11 exclui explícitamente:

```text
Reset real de Transform
Reset real de Rigidbody
Reset real de Animator
Reset real de Player stats
Reset real de movement component
Reset real de powerups
Reset real de Actor
Reset real de NPC
Reset real de Projectile
Reset por pool
Reset por save/checkpoint
Scene reload como reset
Addressables reload como reset
Object Reset UX
Component Reset UX
```

Se um corte F11 tentar implementar qualquer item acima, ele deve ser rejeitado como antecipação de consumer/gameplay.

---

## 6. Modelo conceitual

### 6.1. Cycle Reset

Cycle Reset é uma solicitação para reconfigurar o estado ativo de um ciclo conhecido, sem trocar, descarregar ou destruir o ciclo.

```text
Cycle Reset = reset de estado ativo dentro de Route/Activity existente.
```

### 6.2. Route Cycle Reset

Route Cycle Reset mira a Route ativa.

Ele pode incluir a Activity ativa quando a policy declarar isso. A policy inicial recomendada é:

```text
Route reset inclui Activity ativa por padrão.
```

Motivo: para design de jogo, resetar a fase normalmente significa resetar o estado jogável visível inteiro.

### 6.3. Activity Cycle Reset

Activity Cycle Reset mira somente a Activity ativa.

Ele não deve resetar estado global de Route, Session, Save, Camera, Audio ou consumers técnicos.

### 6.4. Participante

Um participante de Cycle Reset é qualquer componente/objeto conhecido pelo escopo ativo que implementa o contrato de reset de ciclo.

No F11, participantes podem ser mocks/probes usados apenas para QA.

---

## 7. Diferença entre Reset, Release, Reload, Snapshot e Pool

| Conceito | Definição | Entra na F11? |
|---|---|---:|
| Reset | Reconfigura estado ativo para um baseline/contexto do ciclo. | Sim |
| Release | Libera conteúdo owned e encerra lifetime. | Não |
| Reload | Recarrega cena/conteúdo. | Não |
| Snapshot Restore | Restaura payload capturado/versionado. | Não |
| Pool Return | Devolve instância a pool. | Não |
| Scene Reload | Recarrega cena Unity. | Não |

Regra:

```text
Reset não deve ser implementado como release/reload disfarçado.
```

---

## 8. Contratos esperados

Os nomes finais podem mudar no corte técnico, mas o shape conceitual deve preservar estes papéis.

```csharp
public enum CycleResetScope
{
    Route = 1,
    Activity = 2
}
```

```csharp
public readonly struct CycleResetRequest
{
    public CycleResetScope Scope { get; }
    public string Source { get; }
    public string Reason { get; }
    public bool IncludeActiveActivity { get; }
}
```

```csharp
public interface ICycleResetParticipant
{
    CycleResetParticipantDescriptor Descriptor { get; }
    CycleResetParticipantResult Reset(CycleResetContext context);
}
```

```csharp
public readonly struct CycleResetResult
{
    public CycleResetStatus Status { get; }
    public int Participants { get; }
    public int Succeeded { get; }
    public int Skipped { get; }
    public int Failed { get; }
}
```

Contrato essencial:

```text
Request antes do side effect.
Plan antes da execução.
Result depois da execução.
Diagnostics sempre explícitos.
```

---

## 9. Ordem de execução

F11 deve definir ordem determinística, mesmo que a execução inicial use mocks.

Ordem conceitual recomendada:

```text
1. Build Plan
2. Validaté Plan
3. Dispatch Participants
4. Aggregate Results
5. Emit Diagnostics
```

A execução interna pode evoluir futuramente para fases:

```text
PrepareReset
ApplyReset
PostReset
```

Mas F11 não precisa implementar as três fases se isso aumentar escopo. O importante é não criar uma API que impeça essa evolução.

---

## 10. Required / Optional

Cycle Reset deve preservar a semântica required/optional já usada pelo framework.

Regras:

```text
Required participant falhou -> CycleResetResult Failed.
Optional participant falhou -> CompletedWithWarnings.
Participant não aplicável -> Skipped.
Nenhum participant em cenário sem expected required -> pode ser SucceededNoParticipants.
Nenhum participant quando expected required existir -> Failed.
```

F11 pode adiar expected declarations reais, mas não deve impedir sua entrada futura.

---

## 11. Diagnostics e validação

F11 deve validar o core por QA smoke com participantes sintéticos.

Smokes esperados:

```text
Activity Cycle Reset Smoke
Route Cycle Reset Smoke
Route Cycle Reset including active Activity Smoke
Cycle Reset no participants Smoke
Cycle Reset participant failure Smoke, se couber no corte
```

Diagnostics mínimos:

```text
cycleResetScope
source
reason
activeRoute
activeActivity
includeActiveActivity
planParticipants
participantsSucceeded
participantsSkipped
participantsFailed
status
```

---

## 12. Consequências

### Positivas

- O framework ganha o caminho canônico de reset antes de Player/Actor/gameplay.
- Reset deixa de ser uma coleção de métodos ad hoc.
- Route/Activity passam a ter reset planejável e observável.
- Fases futuras podem conectar objetos reais sem redesenhar o core.

### Custos

- O primeiro reset funcional será validado por mocks/probes.
- Ainda não haverá reset real de Transform/Player/Actor.
- Será necessário manter a separação entre Cycle Reset e Local/Object Reset.

---

## 13. Guardrails

- Não implementar gameplay reset em F11.
- Não usar reload de cena como reset.
- Não usar release como reset.
- Não usar pool return como reset.
- Não criar fallback por `GameObject.name`, scene path ou hierarchy path como chave funcional.
- Não criar singleton/service locator público para reset.
- Não permitir que Player/Actor defina o contrato de Cycle Reset.
- Não transformar Cycle Reset em Snapshot Restore.

---

## 14. Relação com fases futuras

F11 desbloqueia:

```text
F12 — Cycle Reset Integration & Authoring UX
F13 — Object Entry Foundation
F14 — Local/Object Reset Foundation
F15 — Unity Reset Adapters mínimos
F16 — GameObject Active State Reset Adapter
F22+ / Future - Contextual Reset / Player Participant planning after Gate/Transition/Pause
```

F11 mantém bloqueado:

```text
Object Reset
Component Reset
Player Reset
Actor Reset
Gameplay reset policies
Pooling reset behavior
Projectile reset behavior
```


---

## 15. Fechamento aplicado em F11G

F11 foi aplicada como `Cycle Reset Foundation`.

Cortes aceitos:

| Corte | Status | Evidência |
|---|---|---|
| F11A | `CLOSED / COMPILE PASS` | Contratos e executor isolado compilaram. |
| F11B | `CLOSED / SYNTHETIC SMOKE EVOLVED` | Probe sintético consolidado no runner de QA. |
| F11C | `CLOSED / RUNTIME PATH PASS` | Caminho canônico interno criado. |
| F11D/F11E | `CLOSED / QA CANVAS SMOKE PASS` | `Run Cycle Reset Runtime Host Smoke` validou Route e Activity reset com participantes sintéticos. |
| F11F | `CLOSED / TRIGGER PASS` | `RouteCycleResetTrigger` e `ActivityCycleResetTrigger` solicitaram reset via runtime host. |
| F11G | `CLOSED / DOCS` | Plano e ADR atualizados para fronteira F12. |

Caminho runtime validado:

```text
FrameworkRuntimeHost
  -> GameFlowRuntime
      -> RouteLifecycleRuntime
          -> CycleResetRuntime
```

Smoke de QA aceito:

```text
QA Smoke completed. name='Cycle Reset Runtime Host Smoke'.
Route Cycle Reset: status='Succeeded', participants='3', blockingIssues='0'.
Activity Cycle Reset: status='Succeeded', participants='2', blockingIssues='0'.
```

Trigger smoke aceito:

```text
RouteCycleResetTrigger -> status='SucceededNoParticipants', blockingIssues='0'.
ActivityCycleResetTrigger -> status='SucceededNoParticipants', blockingIssues='0'.
```

`SucceededNoParticipants` e permitido em trigger real na F11 porque discovery real e participantes físicos ainda não existem. Required participants reais pertencem a fases posteriores.

F11 permanece limitada a reset de ciclo. Object Reset, Component Reset, Player Reset, Actor Reset, adapters físicos e gameplay mutation seguem bloqueados até suas fases próprias. Contextual Player/Actor reset permanece deferred para F22+ depois de Gate/Transition/Pause e de um modelo maduro de gameplay object.
