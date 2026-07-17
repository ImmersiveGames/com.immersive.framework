# Auditoria: Saídas de Route e Activity — Ordem e Reentrada

**Produto:** Immersive Framework (`com.immersive.framework`)  
**Alvo:** Unity 6 — framework de ciclo de vida para jogos de médio porte  
**Versão auditada:** `1.0.0-preview.15` (branch `master`)  
**Data:** 2026-07-17  
**Escopo:** caminhos de saída (exit/teardown) de Route e Activity no Runtime  
**Foco principal:** ordem de teardown, reentrada durante callbacks e consistência de identidade publicada  

**Documentos relacionados:**

- `ARCHITECTURE-WEAKNESS-AUDIT.md` — fragilidades de identidade/discovery (escopo distinto)
- `Documentation~/Current/Camera-Delivery-Reconciliation.md` — C9O (Activity teardown antes de Route unload)
- ADRs de Route/Activity lifecycle (`F03`, `F25R`, etc.)

---

## 1. Resumo executivo

As saídas de Route e Activity **já codificam intenções corretas em pontos-chave**:

- Activity lifecycle exit ocorre com as cenas da Activity ainda carregadas.
- Em troca de Route, Activity é limpa antes do unload de conteúdo de Route.
- `GameFlowRuntime` bloqueia requests concorrentes via flags in-flight e `GateRequestAdmission`.
- Dispatch local de receivers de cena usa Enter parent-first e Exit child-first **dentro de um binding**.
- Scope tail limpa content-anchor bindings antes de remover o scope root.

Apesar disso, a auditoria encontrou **fragilidades reais de ordem e reentrada**, concentradas no caminho de **Activity** (Route é mais conservador e consistente).

| Severidade | Qtd. | Tema dominante |
|---|---:|---|
| **Crítica** | 2 | Commit de `CurrentActivity` antes do teardown; Exit/Enter de content intercalados por discovery |
| **Alta** | 3 | Dual pipeline de saída com timings distintos; ordem de participants sem reverse-on-exit; clear+finalize no Route startup |
| **Média** | 3 | Assimetria Route vs Activity; reentrada síncrona via callbacks; falha parcial sem compensação |
| **Baixa / higiene** | 1 | Dual scope window durante switch de Activity sem contrato explícito |

**Conclusão:** o produto **não está sem disciplina de lifecycle**, mas a **janela de transição de Activity é semi-visível e semi-ordenada**. Handlers de exit que leem `CurrentActivity`, assinam eventos, ou dependem de exclusividade entre activities, podem observar estado adiantado ou entram/saem em ordem não determinística entre bindings.

---

## 2. Método

Análise estática de Runtime com foco em:

1. Ordem de passos em `RouteLifecycleRuntime.StartRouteAsync`
2. Ordem de passos em `ActivityFlowRuntime.StartActivityCoreAsync` e `ClearActivityAsync`
3. Dispatch de content lifecycle (`RouteContentRuntime`, `ActivityContentRuntime`)
4. Execution participants (`ActivityContentExecutionRuntime` / request factory / collection order)
5. Reentrada e admission (`GameFlowRuntime`, `GateRequestAdmission`, triggers)
6. Scope tail (`FrameworkScopeTailOperationExecutor`)
7. Uso (ou ausência) de `ActivityRuntimeStatus.Transitioning` e `RuntimeScopeTransitionGuard`

**Não é** auditoria de performance, networking ou cobertura completa de testes automatizados.

### Superfícies principais lidas

| Área | Arquivos-chave |
|------|----------------|
| Route owner | `Runtime/RouteLifecycle/RouteLifecycleRuntime.cs` |
| Route content | `Runtime/RouteLifecycle/RouteContentRuntime.cs` |
| Activity owner | `Runtime/ActivityFlow/ActivityFlowRuntime.cs` |
| Activity startup gate | `Runtime/ActivityFlow/ActivityFlowRuntime.RouteStartupActivation.cs` |
| Activity content | `Runtime/ActivityFlow/ActivityContentRuntime.cs` |
| Activity execution | `Runtime/ActivityFlow/ActivityContentExecution*.cs` |
| GameFlow / reentrada | `Runtime/GameFlow/GameFlowRuntime.cs`, `Runtime/Gate/GateRequestAdmission.cs` |
| Scope tail | `Runtime/Common/Lifecycle/FrameworkScopeTailOperationExecutor.cs` |
| State | `Runtime/ActivityFlow/ActivityRuntimeState.cs`, `ActivityRuntimeStatus.cs` |

---

## 3. Ordem canônica observada

### 3.1 Route switch — `RouteLifecycleRuntime.StartRouteAsync`

| # | Passo | Notas |
|---|--------|-------|
| 1 | Preview / progress plan | Inclui release de cenas de Activity e Route |
| 2 | Clear Activity **ou** release de cenas Activity (se não havia Activity) | Teardown de Activity **antes** de Route content exit |
| 3 | `ExitRouteContent` (receivers de cena da Route anterior) | Primary scene antiga ainda loaded — intencional (C9O) |
| 4 | Execute release plan de Route content | Unload / release |
| 5 | Compose / load cenas da nova Route | |
| 6 | Create Route scope root | |
| 7 | Discover Route content anchors | |
| 8 | `EnterRouteContent` | |
| 9 | Startup Activity (com ou sem activation gate) | |
| 10 | Opcional: `FinalizeRouteStartupPreviousActivityScope` | Só no path com gate de player |
| 11 | Scope tail de Route (binding cleanup → remove previous Route root) | Tarde no fluxo |
| 12 | Commit `_currentRouteState` | Identidade de Route só muda no fim **em sucesso** |
| 13 | Publish `RouteExited` / `RouteEntered` | **Depois** do commit |

### 3.2 Activity switch — `ActivityFlowRuntime.StartActivityCoreAsync`

| # | Passo | Notas |
|---|--------|-------|
| 1 | Create **next** Activity scope root | Scope novo antes do teardown do antigo |
| 2 | Load / compose cenas do target | |
| 3 | Activation gate (ex.: player admission commit) | |
| 4 | **Commit** `_currentActivityState = Active(next)` | Identidade já é a próxima |
| 5 | Publish lifecycle events + apply visibility / scene receivers | Exit event no switch **não** aplica content (ver §5.3) |
| 6 | Discover Activity content anchors (next) | |
| 7 | Execution participants: Exit previous → Enter next | Depois do content de cena |
| 8 | Scope tail (bindings previous → remove previous root) | |
| 9 | Release cenas da previous (policy de Activity) | Último passo — bom para receivers de cena |

### 3.3 Activity clear — `ActivityFlowRuntime.ClearActivityAsync`

| # | Passo | Notas |
|---|--------|-------|
| 1 | **Commit** `_currentActivityState = None` | Antes de qualquer callback de exit |
| 2 | Apply content via events (Exit com `next == null`) | Content apply **ocorre** no Exit handler |
| 3 | Execution participants Exit | |
| 4 | Scope tail | |
| 5 | Release cenas (respeitando policy de clear) | |

### 3.4 Dispatch local dentro de um binding

`RouteContentRuntime` e `ActivityContentRuntime`:

- **Enter:** `parentFirst = true` (raiz → filhos)
- **Exit:** `parentFirst = false` (filhos → raiz)

Isso é correto **dentro de um único** `RouteContentBinding` / `ActivityLocalVisibilityAdapter`.

---

## 4. O que o framework já faz bem

1. **Gate de reentrada no GameFlow**  
   `_routeRequestInFlight`, `_activityRequestInFlight`, `_cycleResetRequestInFlight` + `GateRequestAdmission.EvaluateLifecycleRequest`.  
   Host público (`FrameworkRuntimeHost`) encaminha requests pelo `GameFlowRuntime`.

2. **Activity exit com cenas ainda loaded**  
   Release de cena é o último passo do clear/switch. Receivers de cena podem liberar camera, áudio e input antes do unload.

3. **Route exit com Activity já limpa**  
   Evita callbacks de Activity em hierarquia Route prestes a ser destruída. Comentário explícito em `StartRouteAsync` reforça a regra.

4. **Scope tail ordenado**  
   Binding cleanup **antes** de `RemoveScopeRoot` (`FrameworkScopeTailOperationExecutor`).

5. **Isolamento de falha em receivers de cena**  
   Exception em um receiver é logada e contada; o dispatch continua nos demais.

6. **Participant collection com `Order` explícito**  
   Há contrato de ordenação por descriptor (embora o Exit não reverta — ver §5.4).

---

## 5. Achados

### C1 — Identidade de Activity publicada antes do teardown

**Severidade:** Crítica  

**Onde**

- `ActivityFlowRuntime.StartActivityCoreAsync` — `_currentActivityState = ActiveWith(next, …)` **antes** de `ApplyActivityContentThroughLifecycleEvents` e `ExecuteActivityContentLifecycle`
- `ActivityFlowRuntime.ClearActivityAsync` — `_currentActivityState = None(…)` **antes** dos mesmos passos

**Por que é frágil**

1. Durante `OnActivityContentExited`, `ActivityExitedEvent` e participant `Exit`, `CurrentActivity` já aponta para o **próximo** (ou `null`).
2. Handlers que consultam o host para “qual Activity está saindo?” vs “qual está ativa?” obtêm respostas **adiantadas**.
3. `ActivityRuntimeStatus.Transitioning` existe e está documentado como reservado, mas **nunca é atribuído** — a janela de transição não é observável.

**Contraste com Route**

- `_currentRouteState` só é atualizado no fim de `StartRouteAsync` (em sucesso).
- `RouteExited` / `RouteEntered` são publicados **depois** do commit.

**Impacto**

- Side effects em exit que dependem de “ainda sou a Activity ativa” falham silenciosamente ou atuam na Activity errada.
- Diagnósticos e probes de smoke que leem `CurrentActivity` no meio do exit mentem.

**Recomendação**

- Entrar em `Transitioning` **antes** de qualquer exit.
- Manter `Activity` / `PreviousActivity` estáveis durante o teardown.
- Commit de `Active(next)` ou `None` **somente após** exit de content + exit de participants (e, se possível, após scope tail da previous).
- Alinhar o contrato de observação com Route.

---

### C2 — Exit e Enter de content de Activity intercalados por discovery

**Severidade:** Crítica  

**Onde**

- `ActivityContentRuntime.ApplyActivityTransition` — loop único por `ActivityLocalVisibilityAdapter`

**Comportamento atual (por binding)**

1. Se o adapter é da previous → `OnActivityContentExited`
2. `SetContentActive(shouldBeActive)`
3. Se o adapter é da next → `OnActivityContentEntered`

Não há fase global “**todos os exits → todos os enters**”. A ordem depende da ordem de discovery dos adapters nas cenas.

**Por que é frágil**

1. Se o adapter da Activity B aparece **antes** do da Activity A na lista, B entra com A ainda sem exit.
2. Contratos de exclusividade (camera override, BGM, input mode, pause surfaces) podem sobrepor-se.
3. Route content **não** tem esse problema: pass completo de Exit, e Enter só depois da nova composição.

**Impacto**

- Bugs intermitentes dependentes de ordem de cena / ordem de componentes.
- Difícil de reproduzir e de diagnosticar sem logs de discovery order.

**Recomendação**

- Pass 1: todos os exits da previous + deactivate.
- Pass 2: activate da next + todos os enters.
- Opcional: ordenar bindings de forma estável e documentada (não depender de scan bruto).

---

### A1 — Dual pipeline de saída com timings distintos

**Severidade:** Alta  

**Onde**

| Pipeline | Superfície | Momento no switch A→B |
|----------|------------|------------------------|
| Scene-authored | `IActivityContentLifecycleReceiver` via `ActivityExited`/`ActivityEntered` | Passo de content apply (eventos) |
| Execution participants | `IActivityContentExecutionParticipant` | **Depois** do content apply completo |

**Ordem real no switch**

```text
Commit Active(B)
→ Publish Exit(A, next=B)     [content runtime IGNORA se next != null]
→ Publish Enter(B, prev=A)    [content runtime aplica exit A + enter B]
→ Discover anchors (B)
→ Participants Exit(A)
→ Participants Enter(B)
→ Scope tail (A)
→ Release scenes (A)
```

**Detalhe adicional — Exit event no switch**

```csharp
// ActivityContentRuntime.HandleActivityExited
if (activityExitedEvent == null || activityExitedEvent.NextActivity != null)
{
    return; // switch: no-op para content apply
}
```

No switch A→B, o content apply **não** roda no Exit event; roda no Enter handler. Subscribers externos de `ActivityExited` podem assumir que o content já foi tear-down — **não foi**.

**Impacto**

- Scene content da B pode “entrar” com players/materialização da A ainda vivos (participants Exit ainda não rodaram), ou o inverso conforme o que o consumer assume.
- Dois “mundos” de lifecycle sem um contrato único de ordenação cross-pipeline.

**Recomendação**

Documentar e implementar **uma** ordem canônica, por exemplo:

```text
Exit scene receivers (A)
→ Exit participants (A)
→ Enter participants (B)
→ Enter scene receivers (B)
```

Ou o inverso **explícito** — o problema é o híbrido atual sem contrato.

---

### A2 — Ordem de participants no Exit não espelha o Enter

**Severidade:** Alta  

**Onde**

- `ActivityContentExecutionParticipantCollection` — sort por `Descriptor.Order` ascendente
- `ActivityContentExecutionRequestFactory` / `ExecutePhasePlan` — mesma sequência para Enter e Exit

**Por que é frágil**

Padrão clássico de lifecycle:

- Enter: bases primeiro (`Order` baixo → alto)
- Exit: dependentes primeiro (alto → baixo)

Hoje Exit usa a **mesma** ordem ascendente. Se camera depende de actor, ou input de player, o teardown pode destruir a base antes do dependente.

**Recomendação**

- Exit: `Order` descendente (ou campo `ExitOrder` explícito).
- Smoke/QA: provar ordem com participants sintéticos instrumentados.

---

### A3 — Route change: clear total + finalize previous scope no startup path

**Severidade:** Alta  

**Onde**

- `RouteLifecycleRuntime.StartRouteAsync`
  1. Se havia Activity: `ClearActivityAsync` (exit completo + scope tail + release)
  2. … troca de Route …
  3. Startup Activity com gate (player)
  4. `FinalizeRouteStartupPreviousActivityScope(previousActivity, startupActivity, …)` — cleanup **de novo** da previous capturada no início

**Por que é frágil**

1. Após o clear, `CurrentActivity` é `null`; o “previous” do startup core **não** é a Activity antiga real.
2. Handoff “previous ainda viva até commit de startup” **não existe** neste path — a previous já morreu no passo 1.
3. Double cleanup (bindings/root) depende de idempotência; mascara o modelo de ownership.
4. Expectativas de player retention cross-route conflitar com clear duro precoce.

**Recomendação**

Escolher **um** modelo e documentar:

- **Modelo A:** clear total no início da troca de Route; remover finalize previous como path de ownership (no máximo no-op/diagnóstico).
- **Modelo B:** defer clear/teardown da previous até depois do handoff de Route Startup (um único dono do lifecycle da previous).

Evitar A+B parcialmente implementados.

---

### M1 — Assimetria Route vs Activity (estado, eventos, guards)

**Severidade:** Média  

| Aspecto | Route | Activity |
|---------|-------|----------|
| Commit de identity | Fim do fluxo (sucesso) | Meio do fluxo (antes dos exits) |
| Eventos Exited/Entered | Após commit total | No meio (antes de scope tail e scene release) |
| Status Transitioning | N/A | Enum reservado, **nunca setado** |
| Guard reentrada no owner | Não (só GameFlow) | Não (só GameFlow) |

`RouteLifecycleRuntime` e `ActivityFlowRuntime` **não** possuem flag in-flight própria. Proteção existe apenas no `GameFlowRuntime`.

`RuntimeScopeTransitionGuard` protege materialização de scope content, **não** a transição Route/Activity como operação atômica.

**Recomendação**

- Guard `isTransitioning` nos owners internos, não só no GameFlow.
- Documentar contrato: durante Transitioning, `Request*` deve falhar e `Current*` deve expor estado de transição explícito.

---

### M2 — Reentrada síncrona via callbacks (apesar do in-flight)

**Severidade:** Média  

O in-flight do GameFlow impede um **segundo request** assíncrono aceito. **Não** impede:

1. Handler de Exit/Enter ler `CurrentActivity` / `CurrentRoute` com valor adiantado (C1).
2. Handler disparar side effects síncronos em outros subsistemas (não-request).
3. UnityEvents em `RouteContentLifecycleEvents` / `ActivityContentLifecycleEvents` com wiring de cena reentrante.
4. Triggers `async void` (`RequestRoute`, `ClearActivity`, etc.): o `finally` limpa in-flight; a modelagem é sensível a exceções e ordenação de continuations.

**Recomendação**

- Contrato de API: callbacks de lifecycle **não** devem chamar `RequestRoute` / `RequestActivity` / `ClearActivity`.
- Preferir enfileirar intenção para o próximo frame / request queue se o produto precisar disso.
- Em diagnostics/QA, detectar reentrada síncrona (stack guard).

---

### M3 — Falha no meio de Route switch após clear de Activity

**Severidade:** Média  

Se `StartRouteAsync` falha **depois** do clear de Activity e **antes** do commit da nova Route (ex.: release/composition fail):

- Previous Activity já saiu (content, participants, cenas conforme clear).
- `_currentRouteState` **não** avança (bom para identity de Route em falha).
- Não há restauração transacional da Activity anterior.

Não é reentrada, mas é o reverso: **saída sem entrada compensada**.

**Recomendação**

- Resultado de falha com evidência explícita: `partialRouteTransition`, `activityAlreadyCleared`.
- Policy de produto: abortar com diagnóstico forte; evitar “Route antiga + sem Activity” silencioso em consumers.
- Futuro: compensation/rollback só se o produto exigir atomicidade real (custo alto).

---

### B1 — Dual Activity scope durante switch

**Severidade:** Baixa / higiene  

Em `StartActivityCoreAsync`, o scope root da **next** é criado **antes** do exit da previous. Durante load, gate e lifecycle, dois scopes de Activity coexistem.

**Por que importa**

- Útil para handoff e materialização do target.
- Queries de “qual é o scope ativo?” podem ser ambíguas sem contrato.
- Clear puro usa modelo diferente (só remove previous).

**Recomendação**

- Documentar a “dual scope window” como contrato intencional.
- APIs de query devem distinguir `Entering` / `Exiting` / `Active` se consumers consultarem scopes em runtime.

---

## 6. Mapa de reentrada

```text
[GameFlow] RequestRoute / RequestActivity / ClearActivity / Restart
    → EvaluateLifecycleRequestAdmission (inFlight flags)
    → set *_RequestInFlight = true
    → Transition Before (visual gate)
    → RouteLifecycle / ActivityFlow (sem guard próprio de owner)
        → Publish Exit / Enter  ──► handlers síncronos (EventBus, UnityEvent, receivers)
        → Participants Execute  ──► handlers síncronos
        → leitura de Current* pode estar adiantada (Activity)
    → Transition After
    → finally: *_RequestInFlight = false
```

| Tipo de reentrada | Protegido? | Notas |
|-------------------|------------|-------|
| Segundo Request* enquanto outro roda | Sim (GameFlow) | `IgnoredAlreadyInFlight` / gate blocked |
| Leitura de Current* durante exit | Não | Activity adiantada (C1) |
| Side effect síncrono em callback | Não | Contrato social apenas |
| Chamada direta aos owners internos fora do GameFlow | Não | Owners são `internal`; risco se surgirem novos callers |
| Materialização em scope cancelado | Parcial | `RuntimeScopeTransitionGuard` (não cobre transition inteira) |

---

## 7. Tabela consolidada de achados

| ID | Severidade | Tema | Status |
|----|------------|------|--------|
| **C1** | Crítica | Commit de `CurrentActivity` antes do teardown; `Transitioning` morto | Aberto |
| **C2** | Crítica | Exit/Enter de content intercalados por discovery order | Aberto |
| **A1** | Alta | Dual pipeline scene vs participants com timing desalinhado; Exit event no-op no switch | Aberto |
| **A2** | Alta | Exit de participants sem ordem reversa | Aberto |
| **A3** | Alta | Route clear total + finalize previous scope redundante no startup | Aberto |
| **M1** | Média | Assimetria Route vs Activity; guard só no GameFlow | Aberto |
| **M2** | Média | Reentrada síncrona via callbacks / UnityEvents | Aberto |
| **M3** | Média | Falha parcial de Route após Activity clear | Aberto |
| **B1** | Baixa | Dual scope window sem contrato documentado | Aberto |

---

## 8. Plano de mitigação sugerido (cortes)

Ordem recomendada para minimizar risco e maximizar clareza de contrato:

| Corte | Objetivo | Achados |
|-------|----------|---------|
| **E1** | Introduzir `Transitioning` e **commit-after-exit** em Activity switch/clear | C1, M1 |
| **E2** | Two-phase content apply: all exits → all enters | C2 |
| **E3** | Reverse (ou ExitOrder) na execução de participants Exit | A2 |
| **E4** | Contrato único de ordenação scene receivers ↔ participants | A1 |
| **E5** | Unificar modelo Route startup vs previous Activity (clear **ou** defer, não ambos) | A3 |
| **E6** | Guard `isTransitioning` nos owners + política de callbacks | M1, M2 |
| **E7** | Diagnóstico de partial Route transition + policy de falha | M3 |
| **E8** | Documentar dual scope window | B1 |

### Critérios de aceite sugeridos (por corte)

- **E1:** Durante qualquer `OnActivityContentExited` / participant Exit, `CurrentActivity` ainda identifica a Activity que está saindo **ou** status é `Transitioning` com `PreviousActivity` estável; nunca `Active(next)` no meio do exit.
- **E2:** Em switch A→B com N adapters, todos os exits de A completam antes do primeiro enter de B (prova por probe/smoke).
- **E3:** Participant com `Order=20` exit **antes** de `Order=0` (se Enter foi 0→20).
- **E4:** Sequência cross-pipeline documentada no guide e coberta por smoke sintético.
- **E5:** Um único owner documentado do teardown da previous em Route switch; sem double cleanup obrigatório.
- **E6:** Request* durante transition → reject previsível; stack guard opcional em diagnostics.
- **E7:** Mensagem/resultado de falha de Route inclui se Activity já foi cleared.
- **E8:** Guide de lifecycle menciona dual scope e o que consumers podem consultar.

---

## 9. Conclusão

A arquitetura de saída **já acerta o grosso do problema de cena** (não destruir hierarquia antes dos exits; Activity fora antes de Route unload; gate de request no GameFlow).  

Os riscos principais **não** são “falta total de ordem”, e sim:

1. **Identidade de Activity adiantada** durante o teardown (C1).  
2. **Falta de fases globais** no apply de content de Activity (C2).  
3. **Dois pipelines** (scene + participants) sem contrato cross-order (A1/A2).  
4. **Modelo duplo** no Route startup com previous Activity (A3).  
5. **Assimetria** com Route, que é o modelo mais seguro e deveria ser o template de Activity.

Tratar E1+E2 primeiro reduz a classe de bugs mais caros (side effects de exit no mundo errado e overposições de content). E3–E5 consolidam o contrato de produto. E6–E8 endurecem observabilidade e falhas parciais.

---

## 10. Apêndice — trechos de referência (comportamento auditado)

### Activity: commit antes do lifecycle (switch)

`ActivityFlowRuntime.StartActivityCoreAsync` — estado `Active(next)` seguido imediatamente de apply/events e execution lifecycle.

### Activity: commit antes do lifecycle (clear)

`ActivityFlowRuntime.ClearActivityAsync` — estado `None` seguido de apply/events e execution lifecycle.

### Activity content: Exit handler ignora switch

`ActivityContentRuntime.HandleActivityExited` — early return quando `NextActivity != null`.

### Activity content: loop por binding (exit + enter no mesmo índice)

`ActivityContentRuntime.ApplyActivityTransition` — `exitsPreviousActivity` → `SetContentActive` → `entersActiveActivity` dentro do mesmo `for`.

### Route: commit e eventos no fim

`RouteLifecycleRuntime.StartRouteAsync` — `_currentRouteState = result.RouteState` depois de scope tail; em seguida `PublishRouteTransition`.

### Route: Activity exit antes de Route content exit

`RouteLifecycleRuntime.StartRouteAsync` — `ClearActivityAsync` / release de cenas de Activity, **depois** `ExitRouteContent`.

### Reentrada no GameFlow

`GameFlowRuntime` — flags `_routeRequestInFlight` / `_activityRequestInFlight` / `_cycleResetRequestInFlight` + `EvaluateLifecycleRequestAdmission`.

### Transitioning reservado e não usado

`ActivityRuntimeStatus` — `Transitioning = 20` com comentário de “later cuts”; factories de state só produzem `None` e `Active`.

---

*Fim da auditoria. Próximo passo natural: implementar cortes E1–E2 com smoke probes de ordem, ou abrir ADR de “Activity transition observability” se o time quiser congelar o contrato antes do código.*
