# Immersive Framework — Auditoria de Habilidades, Evidência e Ordem de Validação v2

Status: Working audit / revised planning baseline  
Data: 2026-07-29  
Escopo: `com.immersive.framework`, `QAFramework` e `planet-devourer`  
Objetivo: organizar as features do framework como habilidades authoráveis, distinguir prova integrada de demonstração isolada e ordenar os próximos testes por dependência real.

---

## 1. Correção principal desta revisão

A versão anterior usava um único campo de estado (`Closed`, `Partial`, `Not Proven`, etc.). Isso misturava perguntas diferentes:

```text
A feature existe no package?
A feature já rodou em um fluxo integrado?
A feature possui demo isolada e compreensível?
A feature possui casos negativos?
A superfície de authoring está pronta para produto?
```

Essa simplificação produziu uma leitura incorreta: alguns grupos pareciam sem prova real, embora já tivessem passado em `SceneProvidedGameplay`.

A partir desta versão, cada habilidade é avaliada em cinco eixos.

| Eixo | Pergunta |
|---|---|
| Contrato | Existe contrato oficial e superfície authorável no package? |
| Integrado | A habilidade já executou com sucesso em um fluxo real do FIRSTGAME? |
| Isolado | Existe uma demonstração pequena que ensina a habilidade sem depender de um cenário grande? |
| Negativo | Falhas, mismatch, required/optional e cleanup inválido foram provados? |
| Produto | Inspector, criação, documentação curta e diagnóstico estão adequados? |

### Estados usados por eixo

| Estado | Significado |
|---|---|
| `Passed` | Prova correspondente concluída para o corte atual. |
| `Partial` | Há evidência, mas falta parte relevante. |
| `Pending` | Existe ou é planejado, porém ainda não foi demonstrado nesse eixo. |
| `Blocked` | Depende de contrato ou authority ainda ausente. |
| `N/A` | O eixo não é aplicável à habilidade. |

### Regra de leitura

```text
Integrated Passed / Isolated Pending
```

significa:

```text
O mecanismo funciona em um fluxo real.
Ainda falta uma demo que ensine e diagnostique a habilidade isoladamente.
```

Isso é diferente de `Not Proven`.

---

## 2. Definição de habilidade do framework

Uma **habilidade do framework** é uma capacidade que um usuário obtém ao:

- configurar um asset;
- adicionar um componente a um GameObject;
- aplicar uma Recipe/Composer;
- registrar um prefab ou objeto de cena;
- conectar uma superfície persistente;
- publicar uma request tipada;
- ou participar de um lifecycle explícito.

Exemplos:

```text
Adicionar RouteRequestTrigger
  -> o objeto pode solicitar uma Route.

Adicionar um Activity lifecycle participant
  -> o objeto pode reagir a Enter/Exit da Activity.

Adicionar um Content Anchor de escopo Activity
  -> um objeto carregado em cena pode ser associado à Activity ativa.

Adicionar PlayerGameplayCameraAuthoring
  -> o Player/Actor admitido pode publicar uma câmera elegível.

Adicionar UnityResetSubjectAdapter
  -> o objeto pode participar do ResetRegistry.
```

Uma classe interna sem superfície authorável, documentação ou caminho real de produto não é promovida automaticamente a feature.

---

## 3. Autoridades de prova

```text
com.immersive.framework
  Fonte oficial de contratos, runtime, authoring, tooling e documentação.

QAFramework
  Prova técnica, regressões e casos negativos.

planet-devourer / FIRSTGAME
  Prova integração, usabilidade e fluxo real.
```

### Regra de promoção

Uma habilidade é considerada **capacidade de produto concluída** quando houver:

```text
contrato oficial
+ authoring compreensível
+ runtime real quando aplicável
+ prova integrada no FIRSTGAME
+ demo isolada quando a habilidade precisa ser ensinada
+ QA negativo apropriado
+ diagnóstico suficiente
```

Nem toda habilidade precisa de uma cena exclusiva. Uma fixture pode demonstrar várias habilidades, desde que a evidência e os passos permaneçam separáveis.

---

## 4. Baseline de evidência já existente

A fixture `SceneProvidedGameplay` não deve ser tratada como ausência de prova. Ela já cobre um fluxo integrado substancial.

### Evidência registrada como passada

| Cenário | Evidência integrada |
|---|---|
| `TS-01` | Boot, Route/Activity e navegação Menu ↔ Gameplay. |
| `TS-02` | Camera Output único e request/release de câmera do Player. |
| `TS-04` | Pause/Resume e Input Gate ligados ao Player. |
| `TS-05` / `TS-05R` | Player Scene-Provided, Object Reset, Group Reset, Activity Restart e reentrada sem resíduo. |
| `TS-06` | Player Camera seguindo o Player pelo output persistente. |
| `TS-07` | Manager-Provisioned: ainda não executado. |

### Consequência para a auditoria

Os grupos abaixo não começam do zero:

```text
G4 Player/Actor
G5 Input/Gate
G6 Camera
G8 Reset
G9 Pause
parte de G10 Activity Restart
```

O trabalho principal nesses grupos passa a ser:

```text
extrair a habilidade da fixture integrada
+ tornar a montagem legível
+ criar evidência isolada
+ completar casos negativos
```

Não reconstruir sistemas que já passaram integrados.

---

## 5. ADR-009 e impacto na auditoria

O `IF-ADR-009 — Authored Definition and Stable Identity Authority` está `Proposed`.

Direção proposta:

```text
Typed RouteAsset / ActivityAsset reference
  autoridade para definição selecionada em authoring e runtime in-process.

RouteId / ActivityId
  projeção estável para persistence, ownership keys, diagnostics e fronteiras externas.
```

### Impacto em G1

Idempotência de Route/Activity deve, no modelo futuro, comparar a referência exata da definição quando ela estiver disponível.

A demo não deve ensinar que:

```text
mesmo stable ID == mesma definição authored
```

Dois assets diferentes continuam sendo definições diferentes, mesmo que temporariamente carreguem o mesmo ID por duplicação.

### Impacto em G3

Content ownership e cleanup podem continuar usando stable IDs como boundary keys, mas a seleção da definição não deve ser resolvida implicitamente por ID quando a referência tipada já existe.

### Regra de compatibilidade

Enquanto a migração do ADR-009 não estiver concluída:

```text
o contrato atual baseado em stable IDs permanece operacional;
não alterar isoladamente igualdade, validators ou ownership;
adicionar guards de teste sem antecipar a migração.
```

### Provas a adicionar quando o ADR avançar

```text
mesma referência -> request idempotente
asset diferente com mesmo ID -> definições diferentes + collision explícita
rename/move -> identidade preservada
regeneração explícita -> novo stable ID
ownership/release permanece determinístico
```

---

# 6. Grafo revisado

É necessário separar dois grafos.

## 6.1 Dependência runtime real

```text
G0 Persistent Content / Diagnostics
 |
 v
G1 Application / Route / Activity
 |
 v
G2 Scene Lifecycle composition
 |\
 | \
 v  v
G3 Content Anchors      G4 Player / Actor
 |                       |\
 |                       | \
 v                       v  v
G8 Reset                G5 Input   G6 Camera
 \                       |          /
  \                      v         /
   \------------------- G7 Gameplay
                         |
                         v
                       G9 Pause
                         |
                         v
                 G10 Integrated Loop
```

Esse grafo descreve dependências de implementação. `SceneProvidedGameplay` já percorre várias dessas ligações.

## 6.2 Ordem de validação e extração

```text
V0 Congelar baseline integrada
 |
 v
V1 Lifecycle Events isolados
 |
 v
V2 Content Anchors passivos
 |
 v
V3 Anchor Materialization
 |
 v
V4 Extrair Player Scene-Provided
 |
 +--> V5 Extrair Input/Gate
 +--> V6 Extrair Player Camera
 +--> V7 Extrair Pause
 +--> V8 Extrair Reset/Restart
 |
 v
V9 Manager-Provisioned
 |
 v
V10 Gameplay/Showcase integrado
```

Aqui `V1` não significa que Player, Reset ou Pause nunca funcionaram. Significa que Lifecycle é a melhor **próxima demo isolada**, pois explica a infraestrutura reutilizada pelos outros grupos.

---

# 7. Inventário revisado por grupo

## G0 — Authoring, Diagnostics e Persistent Content

### Estado do grupo

```text
Contrato: Passed
Integrado: Passed para o shell atual
Isolado: Partial
Negativo: Partial
Produto: Partial
```

### Habilidades

| Habilidade | Integrado | Isolado | Principal pendência |
|---|---:|---:|---|
| Startup Game Application | Passed | Passed | Refinar UX do asset. |
| Persistent Content único | Passed | Partial | Contador/evidência de instância única. |
| Camera Output persistente | Passed | Partial | Separar output de Player request e overrides. |
| Transition surface | Passed | Partial | Prova visual e falha/release do Gate. |
| Loading surface | Passed | Partial | Progresso e policy em demo curta. |
| Pause surface | Passed | Partial | Extração do TS-04. |
| Diagnostics estruturados | Passed | Partial | Padronização Info/Debug. |
| Validate / Advanced Debug | Partial | Partial | Padronização entre assets/components. |

### Decisão

Não criar um novo sistema G0. Apenas consolidar a composição existente e usar o Persistent Content atual como base das demos.

---

## G1 — Application / Route / Activity Core

### Estado do grupo

```text
Contrato: Passed
Integrado: Passed
Isolado: Passed no demo atual de Route/Activity
Negativo: Partial
Produto: Partial
```

### Já provado

```text
boot em Menu
Route A / Activity 1
request repetida ignorada
Route B / Activity 1
Activity 2
release de cenas adicionais
reentrada na Route restaura startup Activity
retorno ao Menu limpa Activity
ledger sem stale
```

### Pendências reais

- required participant bloqueando readiness;
- optional participant falhando sem bloquear;
- ADR-009: futura prova de reference equality versus stable ID;
- UX final dos assets de Route/Activity e criação guiada.

### Decisão

Não reabrir composição de cenas. Completar os gaps de readiness dentro de G2.

---

## G2 — Scene Lifecycle e Lifecycle Participants

### Estado do grupo

```text
Contrato: Passed
Integrado: Partial/indireto
Isolado: Pending
Negativo: Pending
Produto: Pending
```

### Correção de leitura

Scene Lifecycle já está sendo usado por Pause e Reset na fixture integrada. O que falta não é provar que `SceneAvailable` existe internamente; falta provar a **habilidade authorável** de um objeto reagir ao lifecycle de forma visível, previsível e diagnosticável.

### Habilidades a demonstrar

```text
SceneAvailable
SceneReleasing
Route Enter
Route Exit
Activity Enter
Activity Exit
binding idempotente
request ignorada não repete callback
required participant bloqueia readiness
optional participant não bloqueia
```

### Demo isolada recomendada

Uma única cena/família de prefabs:

```text
LifecycleProbe
LifecycleSequencePanel
RequiredReadinessProbe
OptionalReadinessProbe
```

Cada probe deve mostrar:

```text
último evento
contador
owner/scope atual
resultado da última execução
```

### Critério de fechamento

```text
Integrated Passed
+ Isolated Passed
+ Negative Passed para required/optional
+ montagem documentada
```

### Prioridade

**Próximo corte.**

---

## G3 — Content Ownership, Content Anchors e Materialization

### Estado do grupo

```text
Contrato: Passed/Experimental em partes
Integrado: Pending
Isolado: Pending
Negativo: Pending
Produto: Pending
```

### Ausência confirmada

Os logs atuais mostram zero anchors e zero candidates. Portanto, esse grupo realmente não foi exercitado no FIRSTGAME.

### Ordem interna

#### G3.1 Anchors passivos

```text
Route anchor aceito
Activity anchor aceito
Route mismatch
Activity mismatch
cleanup ao sair
reentrada sem duplicação
```

#### G3.2 Kinds

```text
Root
Slot
Point
```

Os kinds devem ser demonstrados como intenção de authoring, sem comportamento mágico implícito.

#### G3.3 Materialization Bridge

```text
prefab explícito
anchor Transform explícito
materialização
ownership registrado
release
reexecução idempotente ou rejeição tipada
```

### Impacto do ADR-009

A demo deve exibir separadamente:

```text
definition reference
stable external identity
runtime owner/handle
```

### Prioridade

Logo após G2.

---

## G4 — Player Slot, Logical Player e Actor

### Estado do grupo

```text
Contrato: Passed para Scene-Provided e Actors atuais
Integrado Scene-Provided: Passed
Isolado: Pending
Negativo: Partial
Produto: Partial
Manager-Provisioned: Pending
Session-Persistent: Blocked
```

### Correção de leitura

`Runtime/Actors` já participa do fluxo real por `PlayerActorDeclaration` no prefab `Actor_PlayerSceneProvided`. O grupo não deve ser descrito como não utilizado.

### O que já foi provado integrado

```text
Player existente na cena
Slot configurado
Local Player admitido
Actor declarado/correlacionado
Activity Ready
release/reentrada
```

### Próximos cortes

#### G4.1 Extrair Scene-Provided

Transformar a fixture existente em uma montagem legível:

```text
PlayerSlotProfile
ActorProfile / Actor declaration
LocalPlayerHostAuthoring
SceneLocalPlayerAdmissionAuthoring
Activity participation configuration
```

Não reescrever o fluxo.

#### G4.2 Manager-Provisioned

Esse é o caminho realmente não executado:

```text
join autorizado
Slot reservation
PlayerInputManager
Host validation
Logical Player admission
Actor selection/preparation
```

#### G4.3 Policies

```text
NoSlots
JoinedSlots
SelectedActors
LogicalActorsPrepared
GameplayReady
zero-participant policy
Actor duplicate policy
```

### Prioridade

Depois de G3 ou em paralelo após o harness de G2, começando pela extração Scene-Provided. Manager-Provisioned vem depois.

---

## G5 — Input Eligibility e Gate

### Estado do grupo

```text
Contrato: Passed
Integrado: Passed no fluxo Scene-Provided/Pause
Isolado: Pending
Negativo: Partial
Produto: Partial
```

### Já provado integrado

- Player produz input;
- Pause/Gate bloqueia o fluxo;
- Resume restaura o input;
- Gate de transição publica blockers de Input, Interaction e Gameplay.

### Falta demonstrar isoladamente

```text
eligibility antes/depois da admissão
Input Acceptance
Interaction Acceptance
Gameplay Action
Input Mode
cleanup ao sair da Activity
sem action map stale
```

### Decisão

Extrair da fixture existente. Não construir controller ou movimento genérico no framework.

---

## G6 — Camera Requests e Output Authority

### Estado do grupo

```text
Contrato: Passed para output e Player camera atuais
Integrado: Passed
Isolado: Partial
Negativo: Partial
Produto: Partial
```

### Já provado integrado

```text
um Camera Output persistente
Player camera request
Player camera seguindo o Player
release/restauração ao sair
```

### Ainda não concluído

```text
CameraRigComposer no novo fluxo
Apply/Rebuild em demo
Session override
Route override
Activity override
prioridade entre requests
negative release/stale request
```

### Ordem

```text
extrair Player camera existente
-> provar prioridade/release
-> adicionar Activity override
-> adicionar Route/Session override somente depois
```

---

## G7 — Gameplay State Real

### Estado do grupo

```text
Integrado mínimo: Passed para movimento/reset/pause
Showcase real: Partial
Isolado: N/A
Produto: Partial
```

A fixture Scene-Provided já oferece estado suficiente para provar movimento, câmera, pause e reset. Não é necessário esperar um jogo completo para extrair as capacidades.

Para o Showcase, ainda falta um loop mais representativo:

```text
objetivo
coleta/contador
resultado
replay
```

---

## G8 — Reset Foundation

### Estado do grupo

```text
Contrato: Passed para Object/Group Reset atuais
Integrado: Passed
Isolado: Pending
Negativo: Partial
Produto: Partial
```

### Correção de leitura

Object Reset, Group Reset e registro/unregister dos Subjects já passaram no fluxo real. Activity Restart também executou Reset → clear → reentry com sucesso.

### Evidência integrada

```text
UnityResetSubjectAdapter registrado
Player Transform restaurado
box de Group Reset restaurada
Object Reset executado
Group Reset executado
Subjects liberados em SceneReleasing
Activity Restart sucedido
```

### Gaps reais

```text
demo isolada de Subject + participant
IUnityResettable customizado
runtime instance criada após SceneAvailable
required participant failure
optional participant failure
seleção Route vs Activity
repeated reset sem handle stale
```

### Decisão

Não reconstruir binding vertical. Extrair a configuração aprovada e adicionar casos negativos.

---

## G9 — Pause e InputMode

### Estado do grupo

```text
Contrato: Passed
Integrado Pause/Resume: Passed
Isolado: Pending
Negativo: Partial
Produto: Partial
```

### Já provado integrado

```text
Pause Request
Resume Request
Pause surface persistente
Input Gate ligado ao Player
retomada do gameplay
```

### Gaps reais

```text
sair da Activity pausado
sair da Route pausado
restart enquanto pausado
reentrada sem timeScale/input mode stale
falha/rejeição de binding
```

### Decisão

Extrair TS-04 e completar os casos de cleanup.

---

## G10 — Activity Restart e loop integrado

A versão anterior agrupava duas coisas em um único estado. Esta revisão separa:

### G10A — Activity Restart

```text
Contrato: Passed
Integrado: Passed
Isolado: Partial/Pending
Negativo: Partial
Produto: Partial
```

Já existe prova real de:

```text
Reset Subjects
Activity clear
Activity reentry
Player/Actor reentrada
sem resíduo terminal aparente
```

### G10B — Loop completo com Transition/Loading/Pause/Camera

```text
Integrado: Partial
Isolado: N/A
Negativo: Pending
Produto: Pending
```

Fluxo ainda necessário:

```text
Play
-> modificar estado
-> Pause/Resume
-> Restart
-> completar objetivo
-> Result
-> Replay
-> repetir sem acumular bindings, requests ou stale content
```

### Decisão

Activity Restart não bloqueia mais a extração dos grupos anteriores. O que permanece para o final é a **composição completa do loop**.

---

## G11 — Features avançadas e opcionais

Continuam posteriores ao loop principal:

```text
Camera storytelling e overrides complexos
Actor choice
segundo Player/co-op
BGM experimental
persistence/progression
Session-Persistent Logical Player
multi-output/split-screen
```

Não usar essas features para atrasar a conclusão dos grupos básicos.

---

# 8. Ordem revisada de execução

## Fase 0 — Congelar baseline existente

### Objetivo

Evitar que novas demos destruam ou substituam evidência já aprovada.

### Ações

```text
registrar TS-01, TS-02, TS-04, TS-05/05R e TS-06 como Integrated Passed
manter SceneProvidedGameplay como fixture de regressão
não duplicar sua lógica em novas cenas
registrar TS-07 como Pending
```

### Saída

Matriz de evidência atualizada no documento de tracking.

---

## Fase 1 — Fechar o que está realmente ausente

### 1. G2 Lifecycle Events isolados

Motivo:

- é a infraestrutura comum mais reutilizada;
- ensina a ordem de callbacks;
- permite casos required/optional;
- reduz ambiguidade ao extrair Player, Reset e Pause.

Entrega mínima:

```text
LifecycleProbe prefab
Scene/Route/Activity event panel
required readiness failure
optional failure
smoke de ordem e idempotência
```

### 2. G3.1 Content Anchors passivos

Motivo:

- zero uso atual no FIRSTGAME;
- prova ownership scene-authored;
- prepara conteúdo modular, Reset owner-scoped e materialização.

### 3. G3.3 Materialization Bridge

Somente após anchors passivos.

---

## Fase 2 — Extrair capacidades já aprovadas

### 4. G4.1 Player Scene-Provided

Usar a fixture existente como fonte. Criar uma versão didática ou uma documentação guiada, não uma implementação paralela.

### 5. G5 Input/Gate

Extrair blockers e eligibility do mesmo Player.

### 6. G6 Player Camera

Extrair output, request e release.

### 7. G9 Pause

Extrair Pause/Resume e adicionar exit while paused.

### 8. G8 Reset/Restart

Extrair Object, Group e Activity Restart. Adicionar required/optional, runtime instances e repeated execution.

#### Observação

Esses itens podem compartilhar a mesma fixture desde que existam:

```text
seções de hierarchy claras
prefabs reutilizáveis
painel de evidência por habilidade
passos independentes
```

Não é necessário criar cinco cenas quase idênticas.

---

## Fase 3 — Fechar o caminho realmente novo de Player

### 9. G4.2 Manager-Provisioned

Esse é o próximo grande caminho funcional ainda não executado.

Critérios:

```text
join autorizado
Slot reservado
PlayerInputManager cria Host
Host validado
Logical Player admitido
Actor selecionado/preparado
Input/Camera publicados
release correto
```

### 10. G4.3 Policies

Depois do caminho canônico funcionar.

---

## Fase 4 — Completar produto e negativos

### 11. Camera overrides

```text
Player camera
-> Activity override
-> release
-> Player camera restaurada
```

### 12. Pause/Reset/Restart negatives

```text
exit pausado
restart pausado
required failure
optional failure
stale release
repeated replay
```

### 13. ADR-009 migration guards

Somente conforme a decisão avançar. Não antecipar mudança de authority no demo.

---

## Fase 5 — Showcase 0.1

```text
Menu
-> Gameplay
-> Player admitido/provisionado
-> Input e Camera ativos
-> objetivo altera estado
-> Pause/Resume
-> Restart
-> Result
-> Replay
-> retorno ao Menu
```

Critérios finais:

```text
sem duplicação de Persistent Content
sem request de Camera stale
sem Input Mode preso
sem Subject de Reset stale
sem conteúdo de Activity stale
sem fallback silencioso
sem dependência de menu de smoke
```

---

# 9. Matriz resumida revisada

| Grupo | Integrado | Isolado | Negativo | Próxima ação |
|---|---|---|---|---|
| G0 Persistent/Diagnostics | Passed | Partial | Partial | Consolidar evidência e UX. |
| G1 Route/Activity | Passed | Passed | Partial | Readiness + ADR-009 guards futuros. |
| G2 Lifecycle Events | Partial/indireto | Pending | Pending | Próximo corte. |
| G3 Content Anchors | Pending | Pending | Pending | Depois de G2. |
| G4 Player Scene-Provided | Passed | Pending | Partial | Extrair fixture. |
| G4 Manager-Provisioned | Pending | Pending | Pending | Após extração básica. |
| G5 Input/Gate | Passed | Pending | Partial | Extrair TS-04/fixture. |
| G6 Player Camera | Passed | Partial | Partial | Extrair TS-02/TS-06. |
| G7 Gameplay mínimo | Passed | N/A | N/A | Expandir somente para Showcase. |
| G8 Reset | Passed | Pending | Partial | Extrair e completar negativos. |
| G9 Pause | Passed | Pending | Partial | Extrair e testar exit pausado. |
| G10A Activity Restart | Passed | Pending | Partial | Extrair e testar falhas. |
| G10B Loop integrado | Partial | N/A | Pending | Após grupos básicos. |
| G11 Avançado | Partial/Experimental | Pending | Pending | Posterior. |

---

# 10. Próximo corte recomendado

```text
IF-DEMO-LIFECYCLE-CAPABILITIES-01
```

## Objetivo

Demonstrar como objetos scene-authored participam de Scene, Route e Activity lifecycle.

## Escopo

```text
SceneAvailable
SceneReleasing
Route Enter/Exit
Activity Enter/Exit
required readiness participant
optional readiness participant
ordem e contadores visíveis
```

## Fora de escopo

```text
Content Anchors
Player provisioning
Camera override
Reset
Pause
Restart
```

## Ganho

- fecha o único grupo de capacidade básica ainda sem demo própria;
- cria o padrão de probe/evidence panel para G3 e para extração dos grupos já aprovados;
- reduz risco de atribuir a Player, Reset ou Pause um comportamento que pertence ao Scene Lifecycle.

---

# 11. Fontes e evidências consideradas

```text
IF-ADR-001 Core Lifecycle and Runtime Authority
IF-ADR-002 Product Authoring Model
IF-ADR-003 Player Participation and Actor Lifecycle
IF-ADR-004 Camera Requests and Output Authority
IF-ADR-005 Input Pause Gate and Reset
IF-ADR-006 Loading Transition Persistence and Diagnostics
IF-ADR-008 Persistent Application Content Composition
IF-ADR-009 Authored Definition and Stable Identity Authority (Proposed)
IF-TRACK-Framework.md
FIRSTGAME current-state/test-scenario review
SceneProvidedGameplay Play Mode logs
Route/Activity additive-scene smoke logs
Reset/Restart Play Mode logs
```

---

# 12. Conclusão

A ordem arquitetural original permanece válida, mas o estado dos grupos precisava ser corrigido.

A leitura correta é:

```text
G2 e G3 são os gaps de demonstração mais fundamentais.
G4/G5/G6/G8/G9 já têm prova integrada relevante.
Esses grupos precisam ser extraídos e ensinados, não reconstruídos.
Manager-Provisioned continua realmente pendente.
Session-Persistent continua bloqueado.
Activity Restart já passou integrado; o loop completo ainda não.
ADR-009 deve entrar como guardrail de identidade antes de futuras mudanças de authority.
```

Próxima sequência:

```text
Lifecycle Events
-> Content Anchors
-> Materialization
-> extrair Scene-Provided Player/Input/Camera/Pause/Reset
-> Manager-Provisioned
-> negativos e overrides
-> Showcase completo
```
