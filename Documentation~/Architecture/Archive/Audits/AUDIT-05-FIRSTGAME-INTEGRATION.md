# AUDIT-05 — FIRSTGAME: composição real do problema

**Escopo:** `planet-devourer / FIRSTGAME` — composição real de Activity, Player provisioning, Join, Actor lifecycle, Loading/Transition e capability gate.  
**Contratos de referência:** `AUDIT-01-ACTIVITY-READINESS.md`, `AUDIT-02-PLAYER-LATE-JOIN.md`, `AUDIT-03-WAITCOVERED-LOADING-GATES.md` e, como evidência complementar de cobertura, `AUDIT-04-QA-COVERAGE.md`.  
**Método:** análise somente dos arquivos locais disponíveis. O projeto Unity completo/serialized assets atuais não está montado neste ambiente; por isso, detalhes não presentes nos logs ou auditorias locais são marcados como não comprovados.

---

## 1. Resumo

- **IF-READY-05-001:** O cenário real relevante é o **Demo02 Manager-Provisioned Player**, composto por `Demo 02 Game Application`, `Manager Player Route`, `Manager Player Activity`, `SceneManagerPlayer`, `SceneManagerPlayerMenu`, `SceneManagerPlayerActivity` e `Demo02_PersistentContent`.

- **IF-READY-05-002:** A evidência local histórica identifica `ActivityManagerPlayer.asset` com requirement `30`, correspondente a `LogicalActorsPrepared`. A configuração serializada atual desse asset não está disponível localmente para reinspeção direta; entretanto, os logs atuais continuam mostrando uma única participação Player que mantém a Activity `NotReady` até o Join.

- **IF-READY-05-003:** A evidência runtime mais recente disponível **não mostra `WaitCovered` no M07**. O log de 6 de agosto de 2026 registra explicitamente `policy='WaitVisible'` para `Manager Player Activity`. Portanto, esta auditoria não pode afirmar que o asset atualmente salvo esteja configurado como `WaitCovered`.

- **IF-READY-05-004:** O envelope visual atual usa `FadeWithLoading` e o `Demo02_PersistentContent` contém exatamente um Transition adapter e um Loading adapter. Loading e Transition são infraestrutura persistente do demo; não foi encontrada evidência de uma segunda autoridade local de Loading.

- **IF-READY-05-005:** O Join é disparado por um fluxo de consumidor próprio, mas fino e tipado:

  ```text
  ManagerProvisionedPlayerCommandEmitter
  → ManagerProvisionedPlayerCommandChannel
  → ManagerProvisionedPlayerCommandReceiver
  → LocalPlayerProvisioningAuthoring.OpenJoining / RequestJoin
  ```

  A seleção default também pode ser solicitada via `LocalPlayerActorSelectionRequestAuthoring`. Não foi encontrada chamada local a preparação, materialização, reconcile interno, `RuntimeScopeContext`, reflection, mutação de Slot ou `Destroy` corretivo.

- **IF-READY-05-006:** Uma conclusão antiga do FIRSTGAME está superada. Em logs de 2 de agosto o Join terminava em Slot Joined/Actor selecionado sem reavaliar a Activity. Nos logs de 6 de agosto, `RequestJoin` ainda retorna `logicalActorPrepared='False'` **como resultado imediato da transação de Join**, porém logo depois a mesma Route request conclui com `Manager Player Activity` em `activityReadiness='Ready'`. Isso é evidência real de que o runtime atual já reconcilia a Activity após o late join.

- **IF-READY-05-007:** Portanto, no estado atual observado, o Fluxo 2 não fica mais bloqueado em:

  ```text
  RequestJoin
  → reconciliation
  → Actor preparation
  → readiness
  ```

  Esse trecho progride até `Ready` no consumidor real quando a policy usada é `WaitVisible`.

- **IF-READY-05-008:** O `OpenJoining` é executado com sucesso enquanto a Route/Startup Activity ainda está em operação e aguardando readiness. Isso confirma que o comando de Join não depende do Player gameplay já estar liberado. O próprio `AUDIT-03` também mostrou que a API canônica de Join não consulta o Gameplay Gate.

- **IF-READY-05-009:** A composição atual separa parcialmente **control plane** de **gameplay plane**: o receiver de comandos permanece disponível e o Join pode ser executado enquanto gameplay está gated. Porém, o `Emitter` é diagnosticado como `FIRSTGAME.Demo02.ManagerProvisionedPlayerMenu`, e `SceneManagerPlayerMenu` é conteúdo Route-owned do destino. Isso significa que a ação humana de Join depende da apresentação desse menu.

- **IF-READY-05-010:** Essa distinção explica o risco de `WaitCovered`: o runtime de Player/Readiness pode estar correto e mesmo assim o usuário não conseguir produzir o evento de Join se o botão/emitter estiver atrás do cover que só será removido quando a Activity ficar `Ready`.

- **IF-READY-05-011:** Assim, para a variante `WaitCovered`, a possível circularidade é de **composição de produto/UI**, não de autoridade de readiness:

  ```text
  LogicalActorsPrepared Required
  → Activity espera Player
  → WaitCovered mantém o destino coberto
  → controle de Join pertence ao destino coberto
  → usuário não consegue emitir RequestJoin
  → Player continua WaitingForJoin
  → Activity continua NotReady
  → WaitCovered nunca revela
  ```

  O último elo — o cover realmente impedir clique/raycast no botão atual — **não está comprovado pelos arquivos locais disponíveis**, porque a hierarquia do Canvas e a configuração de raycast do Transition surface não estão disponíveis.

- **IF-READY-05-012:** Não há evidência de workaround local que falsifique readiness ou faça o jogo chamar internals. O `ScriptableObject` command channel é transporte game-owned entre uma superfície de menu e o receiver persistente; não substitui Player authority, Activity Readiness, Actor preparation ou GameFlow.

### Veredito

A composição atual prova que:

```text
WaitVisible
→ WaitingForJoin
→ public OpenJoining / RequestJoin
→ automatic reconcile
→ Activity Ready
```

funciona no FIRSTGAME.

A variante:

```text
WaitCovered
→ WaitingForJoin
→ Join pelo mesmo menu Route-owned
```

não está comprovada nos arquivos locais. Se o botão de Join estiver efetivamente coberto/inacessível, o problema é um **deadlock de apresentação/control plane no FIRSTGAME**: uma operação necessária para satisfazer readiness foi colocada dentro da apresentação que `WaitCovered` retém até essa mesma readiness completar.

Não há evidência atual suficiente para classificar esse comportamento como defeito do agregado de readiness, do late-join reconcile ou do Loading runtime.

---

## 2. Cena/assets/configuração envolvidos

### 2.1 Topologia observada

| Elemento | Evidência runtime |
|---|---|
| Game Application | `Demo 02 Game Application` |
| Startup Route | `Demo 02 Startup Menu` |
| Startup Scene | `Demo02StartMenu` |
| Route de Player | `Manager Player Route` |
| Primary Scene da Route | `SceneManagerPlayer` |
| Route additional scene | `SceneManagerPlayerMenu` |
| Activity | `Manager Player Activity` |
| Activity scene | `SceneManagerPlayerActivity` |
| Activity content profile | `activity-content-profile.activitymanagerplayercontentprofile` |
| Persistent Content | `Demo02_PersistentContent` |
| Player manager/authoring | `LocalPlayerProvisioning` |
| Runtime Host prefab | `Player_ManagerProvisioned` |
| Slot observado | `PlayerSlot:player.1` |
| Actor mount | `ActorMount` |

### 2.2 Persistent presentation

Os logs mostram:

```text
Persistent Content loaded: Demo02_PersistentContent
rootCount = 4
transitionAdapterCount = 1
loadingAdapterCount = 1
pauseAdapterCount = 1
```

Também mostram resolução explícita de:

```text
Loading surface
Transition surface
Pause surface
Camera Output Session
Session Camera Override
```

**IF-READY-05-013:** Não há evidência de duplicação de Loading ou Transition authority no FIRSTGAME. A composição usa as surfaces persistentes esperadas pelo package.

### 2.3 Route composition

`Manager Player Route` carrega:

```text
SceneManagerPlayer
SceneManagerPlayerMenu
```

O segundo aparece como additional Route content:

```text
route-content.scenemenumanageradd
```

**IF-READY-05-014:** O menu de Manager-Provisioned Player é Route-owned, não Activity-owned. Ele pode existir enquanto a Startup Activity prepara.

### 2.4 Activity composition

`Manager Player Activity` materializa:

```text
SceneManagerPlayerActivity
```

e o log atual registra:

```text
activitySceneComposition = Succeeded
activitySceneCompositionScenes = 1
activitySceneCompositionRequired = 1
activitySceneCompositionOptional = 0
```

A evidência histórica de asset registra:

```text
Requirement = 30
LogicalActorsPrepared
```

e uma projeção Player explícita.

**Limitação:** não há cópia local atual do `.asset` serializado para confirmar campo por campo a configuração salva em 6/7 de agosto.

### 2.5 Entry policy observada

O log mais recente que expõe a policy nominal registra:

```text
policy='WaitVisible'
```

com:

```text
reveal='True'
loadingReleased='True'
readiness='NotReady'
```

durante uma interrupção/supersession.

Isso é coerente com o contrato de `WaitVisible`: o destino pode ser revelado enquanto readiness ainda está preparando, com capabilities retidas.

**IF-READY-05-015:** `WaitCovered` não foi localizado como policy efetivamente executada pelo M07 nos arquivos locais atuais. Qualquer conclusão específica sobre o bug em `WaitCovered` precisa ser classificada como análise da variante de composição, não como reprodução observada neste material.

---

## 3. Composição da Activity

### 3.1 Estado inicial sem Player

O runtime inicializa a Session com:

```text
configuredSlots = 1
dynamicCapacity = 1
joiningOpen = False
```

Ao entrar na Route de Manager Player sem Player previamente Joined, a Startup Activity pode ficar:

```text
Activity = Active
Readiness = NotReady
BlockingIssues = 0
```

A ausência de blocking issue é importante: no contrato atual, um Slot explícito aguardando Join é preparação esperada, não failure.

**IF-READY-05-016:** O estado real atual já representa a espera do Player como readiness pendente, em vez do antigo `ActivityContentExecutionBlockingFailure` observado nos logs de 2 de agosto.

### 3.2 Relação com `LogicalActorsPrepared`

No modelo do package descrito pelo `AUDIT-02`:

```text
Explicit Slot ainda não Joined
→ Player readiness contribution = Preparing / WaitingForJoin

Joined
→ default Actor selection quando necessária
→ logical Actor preparation
→ physical materialization
→ Player readiness contribution = Completed
→ aggregate Activity readiness pode virar Ready
```

O FIRSTGAME atual é consistente com esse modelo:

1. `RequestJoin` retorna sucesso.
2. O resultado imediato ainda diz `logicalActorPrepared='False'`.
3. A Route request não termina nesse instante.
4. Depois, `Manager Player Activity` termina `Ready`.

**IF-READY-05-017:** O campo `logicalActorPrepared=False` no resultado de Join **não deve ser interpretado como falha do late-join flow**. Ele descreve o limite transacional de `RequestJoin`: Host/Slot/assignment foram commitados; Actor preparation ocorre no reconcile subsequente.

### 3.3 Requirement atual: nível de confiança

- `LogicalActorsPrepared` é confirmado pela auditoria do asset anterior.
- O comportamento atual de aguardar Join e depois atingir `Ready` é compatível com o mesmo requirement.
- O `.asset` atual não está disponível localmente.

Classificação:

```text
LogicalActorsPrepared no cenário atual:
  fortemente suportado, mas não reinspecionado diretamente no asset atual.
```

---

## 4. Player provisioning

### 4.1 Inicialização

O boot registra:

```text
Local Player provisioning Session runtime initialized
status = Ready
authoring = LocalPlayerProvisioning
manager = LocalPlayerProvisioning
slots = 1
capacity = 1
joiningOpen = False
localPlayerHostPrefab = Player_ManagerProvisioned
```

O prefab técnico usado pelo `PlayerInputManager` é:

```text
Player_ManagerProvisioned
```

e o resultado de Join mostra:

```text
playerInput = Player_ManagerProvisioned(Clone)
localPlayerHost = Player_ManagerProvisioned(Clone)
actorMount = ActorMount
```

### 4.2 Join

O fluxo observado é:

```text
OpenJoining
→ Succeeded

RequestJoin
→ SucceededJoined
→ PlayerSlot:player.1
→ callback ConfirmedSamePlayerInput
→ assignment committed
→ hostBinding committed
→ PlayerInput / LocalPlayerHost available
```

**IF-READY-05-018:** O FIRSTGAME usa a superfície pública de provisioning, sem mutar Slot diretamente.

### 4.3 Requisições repetidas

Depois que existe um Player, uma nova `RequestJoin` pode retornar:

```text
RejectedCapacityReached
```

e, quando joining está fechado:

```text
RejectedJoiningClosed
```

Essas são rejeições normais do provisioning e não evidência de Activity deadlock.

### 4.4 Actor selection

O demo também possui comando para:

```text
RequestDefaultActorSelection
```

Uma tentativa antes de existir Slot Joined produz:

```text
No joined Player Slot is available.
```

O comportamento é explícito e não inventa seleção.

### 4.5 Actor preparation

Não foi encontrada no código/log de consumidor uma chamada a:

```text
TryPrepareSelectedActor
TryEnsureCurrentGameplay
TryReconcileActiveActivityPlayerLifecycle
RuntimeScopeContext
```

**IF-READY-05-019:** Actor preparation é package-owned. O FIRSTGAME não implementa uma segunda autoridade para “consertar” a Activity.

---

## 5. Join control

### 5.1 Cadeia do consumidor

A cadeia observada é:

```text
ManagerProvisionedPlayerCommandEmitter
  → ManagerProvisionedPlayerCommandChannel
  → ManagerProvisionedPlayerCommandReceiver
  → LocalPlayerProvisioningAuthoring
```

Os arquivos envolvidos aparecem nos traces como:

```text
Assets/_Project/Demo02/Scripts/ManagerProvisionedPlayer/Commands/
  ManagerProvisionedPlayerCommandEmitter.cs
  ManagerProvisionedPlayerCommandChannel.cs
  ManagerProvisionedPlayerCommandReceiver.cs
```

Em evidência anterior do mesmo demo, a pasta aparece como:

```text
Assets/_Project/Demo 02 - Provisioned Players/...
```

A diferença de path reflete reorganização do FIRSTGAME; a cadeia conceitual permanece a mesma.

### 5.2 Receiver

O receiver registra:

```text
[FIRSTGAME_M07_COMMAND_RECEIVER]
status='Bound'
channel='Manager Provisioned Player Command Channel'
receiver='LocalPlayerProvisioning'
```

Ele é bound antes da operação da Route de Manager Player.

### 5.3 Emitter

Os logs do emitter registram:

```text
source='FIRSTGAME.Demo02.ManagerProvisionedPlayerMenu'
reason='open-joining-from-route-menu'
```

e a Route carrega uma cena adicional:

```text
SceneManagerPlayerMenu
```

**IF-READY-05-020:** A evidência aponta fortemente para o botão/emitter vivendo no menu Route-owned do destino. A hierarquia exata do GameObject/Canvas não está disponível para inspeção direta.

### 5.4 O comando funciona durante readiness pendente?

Sim, para a configuração `WaitVisible` observada.

A sequência local inclui:

```text
target Route/Activity já commitados
Activity readiness ainda NotReady
→ OpenJoining Succeeded
→ RequestJoin SucceededJoined
→ Route Request posteriormente Succeeded
→ Activity readiness Ready
```

**IF-READY-05-021:** O Join control não depende do Player gameplay ter sido liberado. Isso é uma prova real de separação funcional entre o comando de provisioning e o gameplay do Player.

### 5.5 Limite dessa prova para `WaitCovered`

`WaitVisible` torna o conteúdo visível antes de Ready. Portanto, um botão localizado no destino pode ser clicado.

`WaitCovered` faz exatamente o contrário: retém o cover até Ready.

Logo, a pergunta crítica deixa de ser:

```text
RequestJoin é bloqueado pelo Gameplay Gate?
```

e passa a ser:

```text
o usuário ainda consegue alcançar o emitter de RequestJoin
enquanto o Transition cover está ativo?
```

Os arquivos locais não contêm a hierarquia do Canvas nem a configuração de `CanvasGroup`, `GraphicRaycaster`, bloqueio de raycast ou sorting do cover.

Resultado:

```text
Acessibilidade física do botão sob WaitCovered:
  Não comprovado com a evidência disponível.
```

---

## 6. Gates

### 6.1 Gate observado

Os diagnostics atuais mostram:

```text
TransitionGateMode = InputInteractionAndGameplay
applied = True
blockers = 4

LifecycleRequest
InputAcceptance
InteractionAcceptance
GameplayAction
```

e, no sucesso:

```text
released = True
```

### 6.2 O que o gate bloqueia

Pelo contrato já auditado no package:

```text
Player gameplay input
normal interaction
normal gameplay actions
```

ficam retidos enquanto uma policy de waiting aguarda.

### 6.3 O que ele não bloqueia diretamente

O caminho público:

```text
LocalPlayerProvisioningAuthoring.RequestJoin
```

não consulta diretamente esse gate.

A evidência do FIRSTGAME confirma que o comando pode chegar ao receiver e executar durante o estado de waiting usado por `WaitVisible`.

### 6.4 Gate lógico versus cover visual

Há duas barreiras diferentes:

```text
Capability Gate
  decisão runtime sobre Input/Interaction/Gameplay.

Transition Cover
  apresentação visual que pode também interceptar UI/raycast,
  dependendo da composição concreta.
```

**IF-READY-05-022:** É incorreto concluir que “o Gameplay Gate bloqueia RequestJoin” apenas porque o usuário não consegue clicar no Join durante `WaitCovered`. O package pode aceitar o comando perfeitamente enquanto a UI necessária para produzi-lo está escondida ou coberta.

### 6.5 Circularidade possível

Se o Join emitter atual estiver atrás do cover:

```text
Activity needs Player
        ↓
Player contribution Preparing
        ↓
WaitCovered retains cover
        ↓
Join emitter is behind cover
        ↓
no RequestJoin
        ↓
no Session revision
        ↓
no reconcile
        ↓
Activity remains Preparing
        └───────────────↺
```

Essa é uma dependência circular **de composição**, não de ownership entre `ActivityReadinessState` e Loading.

---

## 7. Fluxo Player já Joined

### 7.1 Fluxo contratual esperado

Segundo `AUDIT-02`:

```text
Player já Joined
→ Activity enter captura o Slot projetado
→ Host evidence já existe
→ default Actor é selecionado se necessário
→ Actor é preparado/materializado no próprio enter
→ Player readiness contribution completa
→ Activity Ready
```

### 7.2 Evidência no FIRSTGAME

Depois de um Join bem-sucedido:

- nova tentativa de Join retorna `RejectedCapacityReached`, indicando que o Session Player permanece;
- o demo executa `ActivityRestartTrigger`;
- o restart faz:
  ```text
  clearStatus='Succeeded'
  reentryStatus='Succeeded'
  blockingIssues='0'
  ```
- a cena `SceneManagerPlayerActivity` é descarregada e novamente disponibilizada.

**IF-READY-05-023:** O FIRSTGAME prova reentry da `Manager Player Activity` com o Session Player já existente no nível de lifecycle.

### 7.3 O que não está diretamente evidenciado

O trecho de log disponível do restart não expõe um snapshot explícito contendo:

```text
ActorPreparationState = Prepared
PhysicalActor = ...
ActivityReadiness = Ready
```

logo após a reentry.

Portanto:

```text
Reentry com Player já Joined:
  comprovada no lifecycle.

Rematerialização física do Actor nessa reentry:
  não comprovada diretamente pelos logs locais consultados.
```

### 7.4 Resultado do Fluxo 1

Classificação:

```text
Player já Joined
→ entrar/reentrar na Activity
→ reentry succeeds

Readiness/Actor rematerialization detalhada:
  fortemente consistente com o contrato,
  mas sem evidência FIRSTGAME específica suficiente para declarar
  toda a cadeia como observada.
```

---

## 8. Fluxo Late Join

### 8.1 Fluxo real observado com `WaitVisible`

A sequência reconstruída dos logs atuais é:

```text
T0  Session inicializada
    Slot player.1 existe
    joiningOpen = False

T1  Request de Manager Player Route começa

T2  SceneManagerPlayer
    + SceneManagerPlayerMenu
    + SceneManagerPlayerActivity
    são compostas

T3  Manager Player Activity fica Active / NotReady
    Player readiness aguarda Join

T4  OpenJoining é emitido pelo ManagerProvisionedPlayerMenu
    → Succeeded

T5  RequestJoin é emitido
    → SucceededJoined
    → Player_ManagerProvisioned(Clone)
    → PlayerSlot:player.1 Joined
    → assignment/host evidence committed
    → join result ainda informa logicalActorPrepared=False

T6  runtime observa a alteração estável de participação
    e reconcilia a Activity atual

T7  lifecycle Player satisfaz o requisito configurado
    por seleção/preparação/materialização conforme necessário

T8  Activity aggregate muda para Ready

T9  Route Request termina
    kind = Succeeded
    currentActivity = Manager Player Activity
    activityReadiness = Ready

T10 capability gate é liberado no terminal correto
```

### 8.2 Evidência crítica de T5 → T9

O mesmo log contém primeiro:

```text
RequestJoin
status='SucceededJoined'
logicalActorPrepared='False'
```

e depois:

```text
Route Request completed
kind='Succeeded'
activity='Manager Player Activity'
activityState='Active'
activityReadiness='Ready'
blockingIssues='0'
```

**IF-READY-05-024:** Essa sequência invalida a conclusão histórica de que o FIRSTGAME necessariamente para após Join/seleção. No runtime atual observado, a Activity progride depois do Join.

### 8.3 Onde o Fluxo 2 continua

Para `WaitVisible`, continua em:

```text
RequestJoin
→ Session change
→ reconcile
→ readiness Ready
→ request success
```

Não há necessidade de:

```text
Prepare Actor button
Reconcile button
Activity re-request
ActivityRestart
```

para fechar o happy path observado.

### 8.4 Onde o Fluxo 2 pode ficar bloqueado com `WaitCovered`

Não no reconcile.

O ponto de risco é anterior a T5:

```text
T3  Activity Preparing
T4  usuário precisa acionar Join
```

Com `WaitVisible`, T4 é possível porque o destino já está visível.

Com `WaitCovered`, se `ManagerProvisionedPlayerMenu` estiver no conteúdo atrás do cover:

```text
T4 nunca acontece.
```

Sem `RequestJoin`:

```text
Session revision não muda
reconcile não tem novo delta
Player contribution continua WaitingForJoin
Activity não chega a Ready
WaitCovered mantém cover
```

### 8.5 Diagnóstico do Fluxo 2

| Hipótese | Evidência |
|---|---|
| Player readiness não atualiza após Join | **Contradita** pelos logs atuais |
| Reconcile não existe | **Contradita** pelo resultado Ready após Join |
| Join API é bloqueada pelo Gameplay Gate | **Contradita** pelo contrato e pelo uso em WaitVisible |
| Loading decide readiness | **Contradita** pelos contratos anteriores |
| WaitCovered deveria ignorar Player | **Contradita** pelo contrato Required |
| Usuário não consegue emitir Join porque o controle está coberto | **Compatível e provável**, porém exige inspeção da UI/cover para prova final |
| `WaitCovered` é inadequado para um Join que só existe dentro do destino coberto | **Confirmado como risco de composição conceitual** |

---

## 9. Workarounds locais

### 9.1 Command channel

O FIRSTGAME possui:

```text
ManagerProvisionedPlayerCommandChannel
ManagerProvisionedPlayerCommandReceiver
ManagerProvisionedPlayerCommandEmitter
```

Esse mecanismo:

- transporta comandos tipados;
- possui receiver explícito;
- registra bind;
- propaga resultado/diagnóstico;
- chama superfícies públicas do framework.

**IF-READY-05-025:** O command channel não é uma reimplementação de Player runtime nem de Activity Readiness. Ele é glue game-owned para comunicação entre a UI Route-owned e uma composição persistente.

Não há evidência atual de necessidade de promovê-lo ao package.

### 9.2 Sem manual prepare

Não foi encontrado:

```text
Prepare Actor button
direct TryPrepareSelectedActor
direct Actor materialization
manual RuntimeScopeContext
```

### 9.3 Sem manual reconcile

Não foi encontrado:

```text
Reconcile Activity button
same-Activity re-request usado como repair
external lifecycle replay usado para completar late join
```

### 9.4 Activity Restart

Existe:

```text
[Demo02] Activity Restart Control
```

e ele executa o contrato oficial de restart.

**IF-READY-05-026:** O restart é uma feature separada e não aparece como mecanismo necessário para o late join atual. Usá-lo para destravar `WaitCovered` seria um workaround incorreto, mas não há evidência de que o FIRSTGAME esteja fazendo isso.

### 9.5 Sem Loading local paralelo

Não há evidência de script do demo calculando readiness progress ou escrevendo diretamente na Loading surface.

### 9.6 Sem fallback silencioso

Os caminhos inválidos vistos no demo retornam failures explícitos:

```text
RejectedJoiningClosed
RejectedCapacityReached
No joined Player Slot is available
```

Nenhum deles é convertido silenciosamente em sucesso.

---

## 10. Problema de integração encontrado

### 10.1 O que não é o problema

Com as evidências atuais, o problema não deve ser formulado como:

```text
"Player é readiness, então Loading nunca consegue terminar."
```

Isso é incompleto.

Se `LogicalActorsPrepared` é Required, é correto que:

```text
sem Player preparado
→ Activity ainda não esteja Ready
```

Também é correto que:

```text
WaitCovered
→ não revele enquanto Activity ainda não está Ready
```

E os logs atuais demonstram que:

```text
RequestJoin
→ pode provocar reconcile
→ Activity pode chegar a Ready
```

### 10.2 O problema de integração

O problema surge quando uma operação necessária para produzir readiness — `RequestJoin` — é oferecida apenas por uma superfície que pertence à própria apresentação retida por `WaitCovered`.

Shape:

```text
Manager Player Activity
  Required: LogicalActorsPrepared

Manager Player menu
  contém comando humano de Join

WaitCovered
  cobre o destino inteiro enquanto Activity != Ready
```

Se o menu de Join estiver sob esse cover:

```text
Join é necessário para Ready
mas a UI de Join só fica acessível após Ready
```

Isso é uma circularidade de produto.

### 10.3 Separação correta das responsabilidades

```text
Activity Readiness
  está correta ao aguardar a contribuição Required.

Player runtime
  está correto ao aguardar RequestJoin e depois reconciliar.

WaitCovered
  está correto ao manter cover até Ready.

Loading
  está correto ao não declarar sucesso enquanto Ready não chegou.

FIRSTGAME composition
  precisa garantir que a operação que desbloqueia readiness
  continue acessível fora da apresentação que depende dessa readiness.
```

### 10.4 `WaitVisible` versus `WaitCovered`

A configuração atualmente comprovada (`WaitVisible`) resolve essa dependência de UX naturalmente:

```text
Activity fica visível
→ menu de Join aparece
→ gameplay continua gated
→ Join acontece
→ Ready
→ gate libera
```

`WaitCovered` tem uma intenção de produto diferente:

```text
não mostrar o destino enquanto ele prepara
```

Se o usuário precisa interagir para completar essa preparação, essa interação não pode depender do conteúdo coberto.

### 10.5 Alternativas conceituais compatíveis

Sem prescrever implementação nesta auditoria, existem três composições coerentes:

```text
A. WaitVisible
   O Join acontece no próprio destino visível.
   Gameplay segue bloqueado até Ready.

B. WaitCovered + persistent control plane
   O destino permanece coberto.
   Um Join control persistente, fora do cover do destino,
   continua visível/interativo.

C. Pre-entry/lobby
   O Join acontece antes de solicitar a Activity coberta.
   Quando a Activity entra, Player já está disponível.
```

O que não é coerente é:

```text
WaitCovered
+ Required Player readiness
+ único Join control dentro do conteúdo coberto.
```

### 10.6 Nível de comprovação

**Confirmado:**

- M07 atual executa `WaitVisible` em evidência recente.
- `OpenJoining`/`RequestJoin` funcionam durante waiting.
- `RequestJoin` é seguido por Activity `Ready`.
- command emitter é associado a `ManagerProvisionedPlayerMenu`.
- `SceneManagerPlayerMenu` é Route-owned.
- Loading/Transition são persistentes e únicos.
- não há workaround técnico local encontrado.

**Não comprovado com a evidência disponível:**

- o asset atual estar configurado como `WaitCovered`;
- o Canvas exato onde o botão de Join vive;
- se o cover atual intercepta raycast sobre esse botão;
- sorting/CanvasGroup do Transition cover;
- rematerialização física do Actor no restart já Joined.

### 10.7 Classificação

Para a variante do problema descrita no prompt:

```text
Classe principal:
  composição incorreta/insuficiente do control plane no FIRSTGAME
  quando WaitCovered é combinado com Join necessário durante readiness.

Não classificado como:
  bug de ActivityReadiness aggregate;
  bug de late-join reconciliation;
  bug de Loading completion;
  fallback silencioso;
  ausência de Actor preparation runtime.
```

---

## 11. Findings

### IF-READY-05-027 — A evidência atual do FIRSTGAME já prova late-join progression

**Status:** Confirmed.

A sequência:

```text
RequestJoin SucceededJoined
logicalActorPrepared=False
→ posteriormente
ActivityReadiness=Ready
Route Request=Succeeded
```

mostra que a transação de Join e a preparação do Actor são fases separadas, e que a segunda fase ocorre automaticamente depois.

---

### IF-READY-05-028 — `logicalActorPrepared=False` no Join result é intermediário, não terminal

**Status:** Confirmed.

Usar esse campo isoladamente para diagnosticar “Actor não foi preparado” produz falso positivo. O terminal relevante para a Activity é a readiness após reconcile.

---

### IF-READY-05-029 — O M07 atual observado usa `WaitVisible`

**Status:** Confirmed.

A policy nominal aparece no runtime como:

```text
policy='WaitVisible'
```

Não foi encontrada evidência runtime recente de `WaitCovered` para a mesma Activity.

---

### IF-READY-05-030 — O control path de Join funciona enquanto gameplay ainda está gated

**Status:** Confirmed para `WaitVisible`.

`OpenJoining` e `RequestJoin` são executados antes do terminal `Ready` da Route request.

Isso demonstra que o provisioning command não precisa de Player gameplay liberado.

---

### IF-READY-05-031 — O ponto frágil para `WaitCovered` é a disponibilidade do emitter

**Status:** High-confidence composition finding.

O receiver e as APIs podem continuar operáveis, mas o emitter é associado ao `ManagerProvisionedPlayerMenu`, que pertence ao destino Route-owned.

Se a presentation de `WaitCovered` cobre esse menu, o runtime continua saudável mas nenhum evento de Join é produzido.

---

### IF-READY-05-032 — Capability gate e visual cover não devem ser confundidos

**Status:** Confirmed.

A incapacidade do usuário de clicar em Join pode vir de:

```text
cover/sorting/raycast
```

mesmo que:

```text
RequestJoin API
```

não seja bloqueada pelo `GameplayAction` gate.

O diagnóstico deve separar essas duas camadas.

---

### IF-READY-05-033 — `WaitCovered` não deve ser enfraquecido para corrigir a composição

**Status:** Architectural conclusion from audited contracts.

Não seria correto corrigir o problema fazendo:

```text
Loading ignorar Player readiness
Activity fingir Ready
WaitCovered revelar antes de Ready
timeout converter WaitingForJoin em sucesso
```

Isso quebraria exatamente o contrato que `WaitCovered` deve proteger.

---

### IF-READY-05-034 — O local command channel não é uma autoridade duplicada

**Status:** Confirmed.

Não há evidência de que ele:

```text
mude readiness
prepare Actor
materialize Actor
libere gate
controle Loading
```

Ele apenas entrega comandos do jogo às superfícies públicas.

---

### IF-READY-05-035 — O Fluxo 2 atual observado chega a Ready sem repair

**Status:** Confirmed para `WaitVisible`.

Não há necessidade observada de:

```text
ActivityRestart
same-Activity request
manual selection obrigatória após Join
manual prepare
```

para fazer a Route request original terminar.

---

### IF-READY-05-036 — O Fluxo 1 possui prova parcial no FIRSTGAME

**Status:** Partially proven.

Após Join, `ActivityRestart` faz clear/reentry com sucesso e o Session Player continua presente, mas os logs locais consultados não exibem a evidência completa de Actor rematerialization dessa reentry.

---

### IF-READY-05-037 — A variante `WaitCovered` precisa de prova de UI, não de nova auditoria do Player runtime

**Status:** Recommended verification target.

A evidência que falta é pequena e específica:

```text
WaitCovered ativo
→ cover visível
→ onde está o Join control?
→ ele permanece visível?
→ recebe raycast?
→ consegue emitir OpenJoining/RequestJoin?
```

Se a resposta for “não”, o bloqueio está localizado no FIRSTGAME/control-plane composition.

Se a resposta for “sim” e `RequestJoin` realmente executar, mas a Activity não chegar a `Ready`, então a investigação deve retornar ao package/reconcile com uma evidência nova que contradiga os runs atuais.

---

## Conclusão final

O FIRSTGAME atual não sustenta mais a tese histórica de que:

```text
Join acontece
→ Actor nunca prepara
→ Activity nunca reavalia
```

Os logs mais recentes demonstram:

```text
Join acontece
→ Activity atual progride
→ Readiness chega a Ready
→ Route request conclui
```

O cenário comprovado usa `WaitVisible`, justamente a policy que permite ao usuário enxergar e operar um Join control localizado no destino enquanto gameplay permanece gated.

Por isso, quando essa mesma composição é alterada para `WaitCovered`, o primeiro suspeito não deve ser o readiness aggregate nem o Loading. O primeiro suspeito é a **posição funcional da ação de Join**:

```text
uma Activity não pode depender de uma ação do usuário
que só existe dentro da apresentação que essa própria Activity
mantém coberta até a ação ter sido concluída.
```

Com os arquivos locais disponíveis, essa é a explicação mais coerente para o bloqueio descrito. A prova final exige somente verificar a hierarquia/raycast do Join control sob `WaitCovered`; não exige reauditar todo o package.
