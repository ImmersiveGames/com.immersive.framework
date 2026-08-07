# AUDIT-06 — Root Cause: LogicalActorsPrepared + WaitCovered + Late Join

**Escopo:** diagnóstico causal consolidado a partir de:

```text
AUDIT-01-ACTIVITY-READINESS.md
AUDIT-02-PLAYER-LATE-JOIN.md
AUDIT-03-WAITCOVERED-LOADING-GATES.md
AUDIT-04-QA-COVERAGE.md
AUDIT-05-FIRSTGAME-INTEGRATION.md
```

Nenhuma nova auditoria ampla de código foi realizada.

---

# 1. Root Cause Summary

## Classificação principal

```text
K. composição incorreta no FIRSTGAME
```

com subtipo:

```text
dependência circular de apresentação / control plane
```

e relação parcial com:

```text
J. dependência circular entre Join e Gate
```

Porém, a classificação `J` precisa de uma precisão importante:

> O `RequestJoin` do package não é bloqueado diretamente pelo Gameplay Gate.

A circularidade encontrada é mais específica:

```text
Join é necessário para Activity Ready
+
o controle humano que dispara Join depende da apresentação
que WaitCovered mantém coberta até Activity Ready
```

Portanto, a causa não é:

```text
Gameplay Gate
→ bloqueia internamente RequestJoin
```

A causa é:

```text
WaitCovered
→ mantém a apresentação do destino coberta

Join control
→ pertence ao conteúdo/apresentação desse destino

Activity Ready
→ depende do Join

logo:

Join control só fica utilizável depois de Ready
mas Ready só acontece depois de Join
```

## Diagnóstico em uma frase

**A Activity está corretamente esperando `LogicalActorsPrepared`; `WaitCovered` está corretamente esperando a Activity ficar Ready; o problema aparece porque a operação necessária para satisfazer essa readiness — o Join do Player — está funcionalmente colocada dentro da apresentação que `WaitCovered` não revela antes dessa mesma readiness completar.**

## O que a evidência descarta

Não há evidência suficiente para classificar a causa como:

```text
B. problema de Player participation
C. problema de late join reconciliation
D. problema de Actor preparation/materialization
E. problema de readiness contribution
F. problema de aggregate readiness
G. problema de WaitCovered
H. problema de Loading projection
L. falta de contrato fundamental no package
```

Os relatórios anteriores fornecem evidência em sentido contrário:

```text
RequestJoin ocorre
→ late join é reconciliado
→ Actor lifecycle progride
→ Player readiness contribution completa
→ mesma Activity occurrence chega a Ready
```

quando o Join consegue ser emitido.

---

# 2. Cadeia causal

## 2.1 Configuração relevante

A condição analisada é:

```text
Activity Player Projection
  Explicit Slot já projetado

Player Slot
  ainda não Joined

Requirement Level
  LogicalActorsPrepared

Entry Readiness Policy
  WaitCovered

Transition Gate
  InputInteractionAndGameplay
```

`LogicalActorsPrepared` não é um status de Activity.

Ele é um requisito de Player Participation que resulta em uma contribuição `Required` dentro da Activity Readiness occurrence.

Fluxo:

```text
Player Participation
→ ActivityPlayerActorLifecycleParticipant
→ Required ActivityReadinessParticipant
→ aggregate ActivityReadinessState
```

## 2.2 Estado inicial correto

Como existe um Slot explícito projetado, mas ainda não Joined:

```text
Player readiness contribution
  Preparing
  reason = WaitingForJoin
```

Não é:

```text
Failed
```

e não é:

```text
NoParticipants
```

A Activity aggregate fica:

```text
NotReady
RequiredPendingCount > 0
Blocking failure = none
```

Esse comportamento é correto.

## 2.3 Efeito de `WaitCovered`

`GameFlowRuntime` captura a Activity readiness occurrence e espera seu terminal.

Enquanto:

```text
ActivityReadinessState.IsReady == false
```

`WaitCovered` mantém:

```text
Loading
Transition cover
Input/Interaction/Gameplay gate
```

retidos.

Não existe timeout automático.

Portanto:

```text
Required continua Preparing
→ wait continua aberto
→ Loading continua
→ cover continua
→ gate continua
```

Esse comportamento também é correto.

## 2.4 Evento necessário para progredir

Para sair de `WaitingForJoin`, é necessário:

```text
RequestJoin
```

O Join produz:

```text
Slot Joined
assignment committed
Host admission committed
Host evidence
Session revision
```

Depois:

```text
Player reconciliation
→ same Activity occurrence
→ default Actor selection quando necessária
→ logical Actor preparation
→ physical Actor materialization
→ Player readiness contribution Completed
→ aggregate Activity Ready
```

Essa cadeia já foi observada no FIRSTGAME com `WaitVisible`.

## 2.5 Onde nasce a circularidade

No FIRSTGAME, o comando humano é produzido por:

```text
ManagerProvisionedPlayerCommandEmitter
→ ManagerProvisionedPlayerCommandChannel
→ ManagerProvisionedPlayerCommandReceiver
→ LocalPlayerProvisioningAuthoring.RequestJoin
```

A evidência do `AUDIT-05` associa esse emitter ao:

```text
ManagerProvisionedPlayerMenu
```

e o menu é conteúdo Route-owned do destino.

Com `WaitVisible`:

```text
destino é revelado antes de Ready
→ usuário vê o menu
→ usuário emite Join
→ reconcile
→ Ready
→ gate libera
```

Com `WaitCovered`, assumindo que o mesmo Join control esteja atrás do cover:

```text
destino não é revelado
→ usuário não alcança o Join control
→ RequestJoin não acontece
→ Session revision não muda
→ reconcile não recebe delta
→ Player contribution continua WaitingForJoin
→ Activity continua NotReady
→ WaitCovered continua cobrindo
```

Esse é o ciclo causal.

---

# 3. Timeline T0...Tn

## T0 — Request da Route/Activity

```text
GameFlow inicia entrada
Transition Before aplica cover
Transition Gate é aplicado
```

Estado:

```text
Cover = Held
Gameplay = Gated
Request = InFlight
```

## T1 — Activity é materializada

A Activity occurrence é criada.

O Player Slot explícito faz parte da projeção da occurrence.

Estado:

```text
Slot = Available / not Joined
Activity occurrence = N
```

## T2 — Player readiness contribution começa

Como o requirement é:

```text
LogicalActorsPrepared
```

e o Slot ainda não está Joined:

```text
Player contribution = Preparing
Reason = WaitingForJoin
Required = true
```

## T3 — Aggregate readiness é recomposta

Como existe Required pending:

```text
ActivityReadiness = NotReady
```

Sem failure:

```text
RequiredFailedCount = 0
Blocking failure = none
```

## T4 — `WaitCovered` entra em espera

O waiter observa occurrence `N`.

Como ainda não está Ready:

```text
wait = pending
Loading = retained
Cover = retained
Gate = retained
```

## T5 — O sistema precisa de um Join

A única mudança normal capaz de destravar esse Required é:

```text
Player Join
```

Idealmente:

```text
RequestJoin
→ stable Session commit
```

## T6 — Ponto de bloqueio real

Se o Join control está dentro da apresentação coberta:

```text
usuário não consegue acionar Join
```

Logo:

```text
RequestJoin = não emitido
```

## T7 — Nenhum delta de Player ocorre

Sem Join:

```text
Slot continua not Joined
Session não adquire o estado esperado
Player reconcile não tem novo delta útil
```

## T8 — Readiness permanece pendente

```text
Player contribution = Preparing / WaitingForJoin
ActivityReadiness = NotReady
```

## T9 — `WaitCovered` permanece correto, mas preso

Como a occurrence ainda não está Ready:

```text
Loading não chega ao terminal de sucesso
Transition After não executa
Cover não revela
Gameplay gate não libera
```

## T10 — Ciclo fechado

```text
Join necessário para Ready
        ↑
        |
controle de Join coberto
        ↑
        |
WaitCovered espera Ready
```

Sem uma ação externa:

```text
estado pode permanecer indefinidamente
```

pois o contrato não possui timeout/fallback silencioso.

---

# 4. Onde ocorre o bloqueio

## Ponto exato

O bloqueio ocorre **antes do Player Join**, e não durante readiness aggregation ou reconciliation.

O ponto é:

```text
T5 → T6
```

Mais precisamente:

```text
necessidade de RequestJoin
→ disponibilidade do controle que emite RequestJoin
```

## Não ocorre em

```text
RequestJoin
→ Slot Joined
```

Não há evidência de que esse trecho seja a causa.

Também não ocorre em:

```text
Slot Joined
→ reconcile
```

O FIRSTGAME atual já mostrou progresso automático depois do Join.

Também não ocorre em:

```text
reconcile
→ Activity Ready
```

porque a mesma cadeia já chegou a `Ready` quando o evento de Join existiu.

Também não ocorre em:

```text
Activity Ready
→ WaitCovered release
```

O QA genérico prova que `WaitCovered` libera presentation/gate depois de Ready.

## Resumo do ponto de falha

```text
Runtime pode processar Join.
Runtime pode reconciliar Join.
Runtime pode atingir Ready.
WaitCovered pode liberar após Ready.

Mas nenhum desses passos começa
se a única superfície humana capaz de emitir Join
estiver inacessível sob o cover.
```

---

# 5. O que está funcionando corretamente

## 5.1 Activity Readiness

**Correto.**

Um Required pendente deve impedir `Ready`.

```text
LogicalActorsPrepared ainda não satisfeito
→ Activity NotReady
```

Fazer a Activity ficar Ready sem Actor preparado quebraria o contrato.

## 5.2 Zero participants vs Explicit Slot

**Correto.**

O cenário relevante não é:

```text
zero participants
```

É:

```text
Explicit Slot projetado
+
Slot ainda não Joined
```

Isso deve resultar em:

```text
Preparing / WaitingForJoin
```

Não deve ser convertido em:

```text
NoParticipants
Ready
Failed
```

## 5.3 Player participation

**Correto na evidência disponível.**

O Slot Joined é uma mutação Session-scoped separada do Actor lifecycle.

## 5.4 Late join reconciliation

**Funciona na evidência atual quando o Join ocorre.**

O `AUDIT-05` encontrou:

```text
RequestJoin SucceededJoined
logicalActorPrepared=False
```

seguido posteriormente por:

```text
ActivityReadiness=Ready
Route Request=Succeeded
```

Isso demonstra que `logicalActorPrepared=False` no resultado imediato do Join não é terminal.

## 5.5 Actor preparation/materialization

**Não há evidência de que seja a causa.**

O pipeline técnico existe e é alcançado pelo reconcile.

## 5.6 WaitCovered

**Correto.**

Sua obrigação é:

```text
não revelar antes de Ready
```

Enfraquecê-lo para revelar durante `Preparing` transformaria `WaitCovered` em comportamento equivalente a `WaitVisible`.

## 5.7 Loading

**Correto.**

Loading não deve produzir sucesso/100% enquanto a Activity ainda não está Ready.

## 5.8 Gate

**Correto como gameplay/capability gate.**

O package não bloqueia diretamente `RequestJoin` através desse gate.

O erro é confundir:

```text
control plane necessário à preparação
```

com:

```text
gameplay que deve permanecer bloqueado durante a preparação
```

---

# 6. O que está incorreto

## 6.1 Defeito de composição

O defeito conceitual é combinar:

```text
WaitCovered
+
Required readiness que depende de uma ação humana
+
único controle dessa ação dentro da apresentação coberta
```

Essas três decisões isoladamente podem ser válidas.

Juntas, formam uma composição sem caminho de progresso.

## 6.2 Autoridade incorreta?

Não.

Nenhuma autoridade central está invertida.

```text
ActivityFlowRuntime
  continua autoridade de readiness.

PlayerParticipation
  continua autoridade de Player/Actor.

GameFlowRuntime
  continua autoridade de entry ordering.

Loading
  continua presentation.

FIRSTGAME
  continua consumidor.
```

O problema é **acessibilidade da operação**, não ownership runtime.

## 6.3 Defeito do package?

Não comprovado como defeito do package.

O package permite:

```text
RequestJoin enquanto gameplay está gated
```

e `WaitCovered` não possui obrigação genérica de inventar um Join control.

O package também não pode assumir que toda Activity WaitingForJoin deve ser automaticamente revelada, pois isso destruiria a semântica authorada de `WaitCovered`.

## 6.4 Defeito do FIRSTGAME

**Sim, se a configuração real for exatamente a descrita:**

```text
WaitCovered
+
Join control somente no conteúdo coberto
```

Nesse caso o FIRSTGAME monta uma feature que exige uma operação do usuário, mas remove a superfície pela qual o usuário consegue executá-la.

---

# 7. Package vs FIRSTGAME

## Package

### Responsabilidades corretas observadas

```text
Player Slot projetado
→ WaitingForJoin

RequestJoin
→ Session mutation

Session revision
→ reconcile

LogicalActorsPrepared
→ readiness contribution

WaitCovered
→ cover até Ready

Loading
→ não termina antes de Ready

Gate
→ gameplay/capabilities retidos
```

### O que o package não deve fazer para esconder a composição

Não deve:

```text
ignorar Player readiness
forçar Ready
revelar WaitCovered antes de Ready
executar Join automaticamente
criar Player sem request
aplicar timeout que resulte em Ready
re-requestar a mesma Activity
executar reconcile sem mudança de participação
```

## FIRSTGAME

### Responsabilidade

O consumidor decide:

```text
onde o usuário solicita Join
```

Se a Activity exige Join durante a preparação, esse controle precisa pertencer a um plano que permaneça operável nessa fase.

### Composição atual conhecida

Com `WaitVisible`:

```text
ManagerProvisionedPlayerMenu
→ visível enquanto waiting
→ Join funciona
```

Isso já foi observado.

### Variante problemática

Com `WaitCovered`:

```text
ManagerProvisionedPlayerMenu
→ potencialmente coberto
```

Se não houver outro control plane persistente:

```text
composição não possui caminho de progresso humano
```

---

# 8. QA que deveria detectar

## Gap principal

O `AUDIT-04` concluiu que o QA genérico já prova `WaitCovered`.

O que falta não é:

```text
mais um teste de WaitCovered
```

É a combinação real:

```text
WaitCovered
+
Explicit Slot not Joined
+
LogicalActorsPrepared
+
Join executado enquanto o entry gate está retido
+
same occurrence reconcile
+
Ready
+
Loading/reveal
```

## QA mínimo necessário

### QA-1 — Programmatic Join under WaitCovered

Configurar:

```text
Activity
  Explicit Slot
  LogicalActorsPrepared
  WaitCovered

Slot
  not Joined
```

Executar:

```text
Activity request
→ confirmar WaitingForJoin
→ confirmar cover/gate retidos
→ emitir public RequestJoin programaticamente
→ confirmar reconcile
→ confirmar Actor Prepared
→ confirmar same occurrence Ready
→ confirmar Loading terminal
→ confirmar reveal
→ confirmar gate release
```

Esse teste separa package runtime de UI.

### Resultado esperado

Se passar:

```text
package/runtime não possui deadlock Join ↔ WaitCovered
```

e o problema real é composição/UI.

Se falhar após `RequestJoin` ter sido realmente aceito:

```text
há nova evidência contra reconcile/runtime
```

e a classificação deve ser reaberta.

## QA-2 — Never Join

Configurar o mesmo cenário e não emitir Join.

Provar:

```text
Preparing permanece legítimo
Ready não aparece
100% não aparece
cover não revela
```

Depois:

```text
clear/replacement
→ unwind explícito
```

Sem timeout artificial.

## QA-3 — Consumer/control-plane integration

Esse caso pode ser FIRSTGAME smoke ou QA de integração visual, dependendo da superfície usada.

Provar:

```text
WaitCovered ativo
→ controle de Join escolhido para esse fluxo permanece acessível
→ comando chega ao receiver
```

Este é o teste que captura a composição que causou o problema.

---

# 9. Findings consolidados

## IF-READY-06-001 — Root cause não está no aggregate readiness

**Status:** Confirmed.

`LogicalActorsPrepared` é Required e deve manter a Activity `NotReady` enquanto o Player está `WaitingForJoin`.

---

## IF-READY-06-002 — `WaitCovered` está retendo exatamente o que seu contrato exige

**Status:** Confirmed.

Enquanto a Activity não está Ready:

```text
Loading
Cover
Gate
```

permanecem retidos.

Isso é comportamento esperado.

---

## IF-READY-06-003 — Loading preso é sintoma, não causa

**Status:** Confirmed.

Loading permanece aberto porque a readiness occurrence não chega a Ready.

Ele não está impedindo a readiness de completar por autoridade própria.

---

## IF-READY-06-004 — Late join reconcile não é a causa principal encontrada

**Status:** Confirmed pela evidência atual.

O FIRSTGAME já apresentou:

```text
RequestJoin
→ posteriormente Activity Ready
```

quando o Join conseguiu ser emitido.

---

## IF-READY-06-005 — O bloqueio causal acontece antes do Join

**Status:** Confirmed como cadeia conceitual; acessibilidade visual concreta ainda depende da composição real do cover.

O elo é:

```text
Player precisa Join
→ usuário precisa Join control
→ Join control está sob presentation retida
```

---

## IF-READY-06-006 — A circularidade é de control plane, não de Player API

**Status:** Confirmed.

Não foi encontrada checagem de Gameplay Gate dentro do caminho canônico de `RequestJoin`.

A descrição mais precisa é:

```text
presentation dependency cycle
```

e não:

```text
Player API gate deadlock
```

---

## IF-READY-06-007 — `WaitVisible` funciona porque rompe o ciclo visual

**Status:** Confirmed.

`WaitVisible` revela a superfície necessária ao Join enquanto mantém gameplay bloqueado.

Fluxo observado:

```text
WaitingForJoin
→ Join control disponível
→ RequestJoin
→ reconcile
→ Ready
→ gate release
```

---

## IF-READY-06-008 — `WaitCovered` continua sendo válido para `LogicalActorsPrepared`

**Status:** Confirmed conceitualmente.

A combinação não é inválida por si só.

É válida quando:

```text
Player já está Joined antes da entrada
```

ou quando:

```text
o Join control pertence a um control plane
que permanece operável durante o cover
```

---

## IF-READY-06-009 — A configuração problemática é uma composição sem caminho de progresso

**Status:** Root cause.

```text
WaitCovered
+
Player Required not Joined
+
Join somente dentro do conteúdo coberto
```

deve ser tratada como composição inválida/inadequada para esse fluxo de produto.

---

## IF-READY-06-010 — O QA atual tem uma lacuna exatamente na interseção do problema

**Status:** Confirmed.

Há prova genérica de WaitCovered e prova parcial/interna de late join, mas não uma prova verde public-only de:

```text
WaitCovered
→ public Join under retained gate
→ same occurrence Ready
```

---

## IF-READY-06-011 — Não corrigir o sintoma enfraquecendo readiness

**Status:** Architectural constraint.

Não usar como correção:

```text
Ready com Player ausente
Optionalizar silenciosamente Player
Loading liberar com NotReady
timeout para sucesso
WaitCovered revelar cedo
```

Isso esconderia a circularidade e enfraqueceria os contratos.

---

## IF-READY-06-012 — A decisão de produto deve separar control plane e gameplay plane

**Status:** Recommended conceptual direction.

Operações necessárias para construir a condição de readiness precisam permanecer disponíveis enquanto gameplay está bloqueado.

Exemplos:

```text
Join
login/connection
device assignment
seat selection
recovery action
```

quando essas operações forem pré-condições da própria Activity readiness.

---

# 10. Correção conceitual recomendada

Não é necessário redesenhar Activity Readiness, Loading ou Player reconciliation.

A correção conceitual é:

> **Uma Activity `WaitCovered` não deve depender de uma ação humana cuja única superfície de execução esteja dentro do conteúdo que ela mantém coberto.**

Existem três composições coerentes.

## Opção 1 — Usar `WaitVisible` para Activity interativa de Join

Fluxo:

```text
Activity entra
→ conteúdo/menu de Join é revelado
→ gameplay permanece gated
→ usuário faz Join
→ Actor prepara
→ Activity Ready
→ gate libera
```

É o shape já demonstrado no FIRSTGAME.

Use quando:

```text
WaitingForJoin faz parte da experiência visível da Activity.
```

## Opção 2 — Manter `WaitCovered`, mover Join para control plane persistente

Fluxo:

```text
Activity começa coberta
→ persistent Join control continua disponível
→ usuário faz Join
→ Actor prepara por trás do cover
→ Activity Ready
→ Loading termina
→ cover revela
```

Use quando:

```text
o destino só deve aparecer completamente pronto.
```

O Join control não pode pertencer ao gameplay input bloqueado nem ao conteúdo que o cover torna inacessível.

## Opção 3 — Join antes da Activity coberta

Fluxo:

```text
Lobby/Menu
→ Player Join
→ solicitar Activity
→ Actor prepara
→ WaitCovered
→ Ready
→ reveal
```

Use quando:

```text
Join é condição de entrada no gameplay,
não uma etapa da própria Activity.
```

---

## Recomendação causal

Para o bug descrito, a primeira correção não deve ser no aggregate readiness.

Também não deve ser no Loading.

O primeiro ajuste deve ser na **composição do fluxo de Join**:

```text
decidir se Join é:
  parte visível da Activity → WaitVisible

ou

  pré-condição/background preparation → WaitCovered
  com Join fora do cover
```

Depois disso, o QA deve provar:

```text
WaitCovered
→ public RequestJoin realmente executado durante retained gate
→ same occurrence reconcile
→ LogicalActorsPrepared
→ Ready
→ Loading 100%
→ reveal
```

Só se esse teste falhar **depois que `RequestJoin` foi efetivamente emitido e aceito** haverá evidência para reabrir Player reconciliation ou Activity Readiness como root cause.

---

# Diagnóstico final

```text
CAUSA
  Join necessário para satisfazer a contribuição Required
  está funcionalmente dependente de uma superfície de UI
  retida pelo próprio WaitCovered.

SINTOMA
  Activity permanece NotReady / WaitingForJoin.
  Loading, cover e gate permanecem ativos indefinidamente.

COMPORTAMENTO CORRETO
  LogicalActorsPrepared bloqueia Ready.
  WaitCovered espera Ready.
  Loading não conclui antes de Ready.
  RequestJoin pode ocorrer com gameplay gated.
  Late join reconcile atualiza a mesma occurrence quando Join ocorre.

DEFEITO
  Composição do FIRSTGAME sem um control plane acessível para produzir Join
  enquanto o destino está coberto.

GAP DE QA
  Falta prova verde public-only:
  WaitCovered + WaitingForJoin + RequestJoin sob retained gate
  + reconcile + same occurrence Ready + reveal.

GAP DE PRODUTO/UX
  Falta tornar explícito onde operações necessárias à readiness
  devem viver quando a Activity usa WaitCovered.
```
