# Immersive Framework — Roteiro Operacional de Modelos de Demonstração

Status: guia de montagem e tracking atualizado  
Data: 2026-07-30  
Destino: `ImmersiveGames/planet-devourer` — FIRSTGAME  
Framework oficial: `ImmersiveGames/com.immersive.framework`  
Validação técnica e casos negativos: `rinnocenti/QAFramework`

Substitui os roteiros anteriores que tratavam o FIRSTGAME como um jogo integrado amplo ou como um
QA manual.

---

# 1. Objetivo

O FIRSTGAME deve ser uma coleção de pequenos modelos práticos de uso do Immersive Framework.

Cada modelo deve ajudar designers e desenvolvedores consumidores a entender:

```text
o que a feature faz;
quais assets precisam ser criados;
quais componentes devem ser adicionados;
como as peças são configuradas;
o que acontece em Play Mode;
qual parte pode ser reutilizada em outro jogo;
quais problemas de UX aparecem durante a montagem.
```

A coleção também deve provar:

```text
modularidade;
custo de composição;
dependências explícitas;
isolamento entre features;
eficiência de runtime;
clareza de Inspector;
reutilização de prefabs;
qualidade do diagnóstico.
```

O FIRSTGAME não deve provar exaustivamente contratos técnicos.

---

# 2. Regra congelada de caminhos felizes

Todas as demonstrações do FIRSTGAME exibem somente caminhos felizes.

Cada modelo deve apresentar:

```text
uma configuração válida;
um fluxo compreensível;
um resultado visual;
cleanup normal;
reentrada normal, quando aplicável.
```

Não criar no FIRSTGAME:

```text
prefabs propositalmente inválidos;
cenas de falha;
botões para injetar erro;
duplicação deliberada de IDs;
mismatch proposital;
rollback forçado;
falha de participant;
binding ausente intencional;
stress test;
matriz de combinações;
painel de asserts.
```

Quando a montagem revelar que um caso técnico precisa ser provado, registrar:

```text
QA Follow-up
  comportamento a validar;
  contrato envolvido;
  resultado esperado;
  risco técnico.
```

A implementação e a regressão desse caso pertencem ao `QAFramework`.

---

# 3. Unidade de trabalho

A unidade principal é o **Modelo de Demonstração**.

```text
Demonstration Model
├── README curto
├── assets de intenção
├── uma ou poucas cenas
├── prefabs de habilidade
├── prefab de composição, quando útil
├── controles mínimos
├── comportamento visual
├── diagnóstico resumido
└── checklist de aceite
```

Um modelo não precisa ser uma única cena.

Quando a feature depende de Route, Activity ou cena aditiva, o modelo pode conter um pequeno conjunto de
cenas. O conjunto deve continuar reduzido e independente.

---

# 4. Critérios de independência

Um modelo está isolado quando:

- não depende dos assets de outro modelo;
- pode reutilizar apenas peças realmente genéricas em `Shared`;
- possui `GameApplicationAsset`, Routes e Activities próprios quando necessário;
- carrega somente os sistemas necessários para a feature;
- não exige modificar uma cena de demonstração anterior;
- não depende de uma sequência global de testes;
- pode ser aberto e executado diretamente;
- pode ser copiado para outro projeto com dependências identificáveis;
- não exige que o usuário entenda toda a arquitetura do FIRSTGAME.

Dependência conceitual não implica dependência de assets.

Exemplo:

```text
Activity Restart depende conceitualmente de Reset.

M13_ActivityRestart não precisa referenciar assets de M12_ObjectReset.
Ele deve possuir sua própria composição mínima de Reset.
```

---

# 5. Estrutura de pastas

```text
Assets/
└── _Project/
    └── FrameworkModels/
        ├── Shared/
        │   ├── Materials/
        │   ├── UI/
        │   ├── Prefabs/
        │   ├── Scripts/
        │   └── Documentation/
        │
        ├── M01_RouteActivity/
        ├── M02_LifecycleEvents/
        ├── M03_ActivityReadiness/
        ├── M04_ContentAnchors/
        ├── M05_AnchorMaterialization/
        ├── M06_SceneProvidedPlayer/
        ├── M07_ManagerProvisionedPlayer/
        ├── M08_ParticipationPolicies/
        ├── M09_InputGate/
        ├── M10_PlayerCamera/
        ├── M11_ObjectReset/
        ├── M12_ActivityRestart/
        ├── M13_Pause/
        ├── M14_TransitionLoading/
        ├── M15_CameraOverrides/
        └── M16_Bgm/
```

Estrutura interna padrão:

```text
M##_FeatureName/
├── Application/
├── Routes/
├── Activities/
├── Profiles/
├── Recipes/
├── Scenes/
├── Prefabs/
├── Materials/
├── Scripts/
└── README.md
```

Criar somente as pastas que o modelo realmente usa.

---

# 6. Convenções de nomenclatura

## Assets

```text
GA_M01_RouteActivity
Route_M01_Menu
Route_M01_Gameplay
Activity_M01_ActivityA
PlayerSlot_M06_Player1
Actor_M06_Default
CameraRig_M10_Player
```

## Cenas

```text
M01_Boot
M01_Menu
M01_Gameplay
M01_ActivityA_Add
M01_ActivityB_Add
```

## Prefabs

```text
PF_M02_RouteLifecycleObject
PF_M04_ActivityContentAnchor
PF_M06_SceneProvidedPlayer
PF_M11_ResettableObject
```

## Scripts do consumidor

```text
namespace FirstGame.FrameworkModels.*
```

Não usar `Immersive.Framework.*` em scripts próprios do FIRSTGAME.

---

# 7. Infraestrutura Shared

`Shared` deve permanecer pequeno.

## Permitido

```text
material visual neutro;
painel de instruções;
label de estado;
botão visual genérico;
marcador de mundo;
controle simples de movimento do consumidor;
ícones e fontes;
scripts de apresentação sem autoridade runtime.
```

## Evitar

```text
uma Persistent Content Scene obrigatória para todos;
um painel global com todos os sistemas;
um manager de demos;
service locator;
bootstrap mágico;
prefab que já contém Player, Camera, Pause e Loading;
dependência oculta entre modelos.
```

## Prefabs compartilhados sugeridos

```text
PF_ModelInstructions
PF_ModelStatusLabel
PF_ModelActionButton
PF_ModelWorldMarker
PF_ModelSimpleCanvas
```

Cada modelo pode compor sua própria infraestrutura mínima.

---

# 8. Formato do README de cada modelo

Cada pasta de modelo deve conter um `README.md` curto com:

```text
Purpose
What This Model Demonstrates
Required Package Features
Assets
Scenes
Prefabs
Setup
Play Mode Flow
Expected Result
Reusable Pieces
UX Findings
QA Follow-ups
```

Código e nomes de tipos permanecem em inglês. A explicação pode ficar em português.

---

# 9. Controle geral de progresso

| Ordem | Modelo | Tipo | Estado |
|---:|---|---|---|
| 1 | M01 Route and Activity | Fundação | Closed |
| 2 | M02 Lifecycle Events | Fundação | Closed |
| 3 | M03 Activity Readiness | Fundação | Pending |
| 4 | M04 Content Anchors | Ownership | Pending |
| 5 | M05 Anchor Materialization | Ownership opcional | Pending |
| 6 | M06 Scene-Provided Player | Player | Pending |
| 7 | M07 Manager-Provisioned Player | Player | Pending |
| 8 | M08 Participation Policies | Player | Pending |
| 9 | M09 Input Gate | Controle | Pending |
| 10 | M10 Player Camera | Câmera | Pending |
| 11 | M11 Object Reset | Estado | Pending |
| 12 | M12 Activity Restart | Estado | Pending |
| 13 | M13 Pause | Estado | Pending |
| 14 | M14 Transition and Loading | Apresentação | Pending |
| 15 | M15 Camera Overrides | Extensão | Pending |
| 16 | M16 BGM | Experimental | Pending |

Estados permitidos:

```text
Pending
Authoring
Play Mode Review
UX Review
Closed
Deferred
```

---

# BLOCO A — ESTRUTURA, LIFECYCLE E OWNERSHIP

# 10. M01 — Route and Activity

## Objetivo

Demonstrar a estrutura mínima de Application, Route e Activity sem Player, Camera de gameplay, Reset ou
Pause.

## Resultado esperado para o usuário

O usuário entende como:

```text
criar uma Game Application;
adicionar Routes;
definir startup Route;
adicionar Activities;
definir startup Activity;
solicitar Route;
solicitar Activity;
compor cenas;
retornar ao Menu.
```

## Assets

```text
Application/GA_M01_RouteActivity.asset
Routes/Route_M01_Menu.asset
Routes/Route_M01_Gameplay.asset
Activities/Activity_M01_A.asset
Activities/Activity_M01_B.asset
```

## Cenas

```text
Scenes/M01_Boot.unity
Scenes/M01_Menu.unity
Scenes/M01_Gameplay.unity
Scenes/M01_ActivityA_Add.unity
Scenes/M01_ActivityB_Add.unity
```

## Prefabs

```text
Prefabs/PF_M01_RouteNavigation.prefab
Prefabs/PF_M01_ActivityNavigation.prefab
Prefabs/PF_M01_CurrentContextDisplay.prefab
```

## Montagem

### Etapa 1 — Game Application

- [ ] Criar `GA_M01_RouteActivity`.
- [ ] Selecionar `Route_M01_Menu` como startup Route.
- [ ] Configurar somente os campos necessários ao modelo.
- [ ] Salvar e executar a validação disponível no Inspector.

### Etapa 2 — Routes

- [ ] Criar `Route_M01_Menu`.
- [ ] Associar `M01_Menu` como cena principal.
- [ ] Não definir startup Activity no Menu.
- [ ] Criar `Route_M01_Gameplay`.
- [ ] Associar `M01_Gameplay` como cena principal.
- [ ] Definir `Activity_M01_A` como startup Activity.

### Etapa 3 — Activities

- [ ] Criar `Activity_M01_A`.
- [ ] Associar `M01_ActivityA_Add`.
- [ ] Criar `Activity_M01_B`.
- [ ] Associar `M01_ActivityB_Add`.
- [ ] Manter participation requirements no mínimo permitido.

### Etapa 4 — Controles

- [ ] Adicionar `RouteRequestTrigger` ao botão `Open Gameplay`.
- [ ] Adicionar `RouteRequestTrigger` ao botão `Back to Menu`.
- [ ] Adicionar `ActivityRequestTrigger` aos botões `Activity A` e `Activity B`.
- [ ] Exibir Route e Activity atuais em um painel pequeno.

## Fluxo em Play Mode

```text
Boot
→ Menu
→ Gameplay Route
→ startup Activity A
→ Activity B
→ Activity A
→ Menu
```

## Evidência visual

```text
Menu visível somente no Menu;
ambiente da Gameplay Route permanece entre Activities;
conteúdo A aparece somente na Activity A;
conteúdo B aparece somente na Activity B;
retorno ao Menu remove Gameplay e Activity.
```

## Critério de aceite

- [ ] O fluxo completo funciona sem Console como guia.
- [ ] As cenas carregadas são compreensíveis no Hierarchy.
- [ ] O Inspector dos triggers deixa claro o destino.
- [ ] O usuário consegue trocar a Activity sem editar código.
- [ ] Nenhuma feature não relacionada foi adicionada.
- [ ] As peças reutilizáveis estão em prefabs.

## Pontos de UX a registrar

```text
Foi fácil criar Route e Activity?
Os menus Create estão claros?
O Inspector explica startup Activity?
O trigger deixa explícito o destino?
Existe informação técnica demais no modo normal?
O usuário entende quais cenas pertencem à Route e à Activity?
```

## QA Follow-ups

Registrar somente os casos técnicos percebidos durante a montagem, por exemplo:

```text
request repetido;
ID duplicado;
Activity fora da Route;
falha de scene composition;
cleanup parcial.
```

Não executar esses casos no FIRSTGAME.

---

# 11. M02 — Lifecycle Events

## Estado

```text
Package authoring surface       Passed
FIRSTGAME manual authoring      Passed
FIRSTGAME Play Mode smoke       Passed
QAFramework deterministic QA    Deferred
Roadmap status                  Closed
```

O M02 está fechado como prova de produto no FIRSTGAME. A regressão sintética no QAFramework foi adiada por
decisão de roadmap e não bloqueia o próximo modelo.

## Objetivo

Demonstrar que objetos scene-authored recebem callbacks reais de Scene, Route e Activity por uma superfície
authorável no Inspector, sem implementar lifecycle paralelo.

## Diferença em relação ao M01

```text
M01
  prova transições de Route e Activity.

M02
  prova consumo authorável dos callbacks por objetos do jogo.
```

## Superfícies oficiais utilizadas

```text
SceneLifecycleEvents
  Available / Releasing

RouteContentBinding
+ RouteContentLifecycleEvents
  Entered / Exited

ActivityLocalVisibilityAdapter
+ ActivityContentLifecycleEvents
  Entered / Exited
```

## Prefabs

```text
PF_M02_SceneLifecycleObject
PF_M02_RouteLifecycleObject
PF_M02_ActivityLifecycleObject
```

Organização validada:

```text
Prefab Root
├── Visual Placeholder
├── Label
├── Framework Components (Configure Manually)
│   └── componentes oficiais
└── Bindings (Configure Manually)
    └── M02LifecycleVisualPresenter
```

A descoberta começa nos roots explícitos da cena e atravessa descendentes, inclusive inativos. A posição na
hierarquia é organização. Route/Activity assets e `localContentId` continuam sendo identidade explícita.

## Montagem concluída

### Base

- [x] Game Application própria.
- [x] Routes A/B próprias.
- [x] Activities A/B próprias.
- [x] Profiles e cenas aditivas próprias.
- [x] Persistent Content próprio.
- [x] Zero Local Player Slots.
- [x] Nenhum asset operacional do M01 reutilizado, exceto navegação visual temporária.

### Scene lifecycle

- [x] `PF_M02_SceneLifecycleObject` criado.
- [x] `SceneLifecycleEvents` configurado.
- [x] `Available` ligado a `OnAvailable`.
- [x] `Releasing` ligado a `OnReleasing`.
- [x] Reação visual e log estruturado comprovados.

### Route lifecycle

- [x] `PF_M02_RouteLifecycleObject` criado.
- [x] `RouteContentBinding` configurado em descendente organizacional.
- [x] `RouteContentLifecycleEvents` ligado ao presenter.
- [x] Route A/B atribuídas por override de instância.
- [x] Enter/Exit comprovados nas duas direções.

### Activity lifecycle

- [x] `PF_M02_ActivityLifecycleObject` criado.
- [x] `ActivityLocalVisibilityAdapter` configurado.
- [x] `ActivityContentLifecycleEvents` ligado ao presenter.
- [x] Activity A/B atribuídas por override de instância.
- [x] Enter/Exit e reentrada comprovados.

## Fluxo validado em Play Mode

```text
M02_Boot
→ Route A + Activity A
→ Activity A novamente: IgnoredAlreadyActive
→ Activity B
→ Route B + Activity B
→ Activity B novamente: IgnoredAlreadyActive
→ Route A + Activity A
```

Não existe etapa de Menu neste modelo isolado.

## Evidência observada

```text
Scene
  Route A Available/Releasing
  Route B Available/Releasing

Route
  A Enter/Exit
  B Enter/Exit

Activity
  A Enter/Exit
  B Enter/Exit

Diagnostics
  routeContentEnterBindings = 1
  routeContentEnterReceivers = 1
  routeContentExitBindings = 1
  routeContentExitReceivers = 1
  activityContentEnterBindings = 1
  activityContentEnterReceivers = 1
  activityContentExitBindings = 1
  activityContentExitReceivers = 1
  failed = 0
  blockingIssues = 0
  activitySceneLedgerStale = 0
```

`IgnoredAlreadyActive` produz zero side effects e nenhum novo callback do presenter.

## Critério de aceite

- [x] Cada lifecycle produz uma reação visível e logável no consumidor.
- [x] Scene, Route e Activity têm responsabilidades distinguíveis.
- [x] Os prefabs podem ser abertos e compreendidos isoladamente.
- [x] Nenhum script do demo assume autoridade de lifecycle.
- [x] Nenhum callback é disparado manualmente pelo consumidor.
- [x] Hierarquia de organização não altera descoberta.
- [x] Reentrada Route B → Route A restaura a composição correta.
- [x] Não há falhas, blocking issues ou ledger stale no smoke fornecido.

## Findings

```text
UX-M02-001
Navigation prefabs reused from M01 are still named for M01.
Candidate: FIRSTGAME Shared/Navigation or future package template.

UX-M02-002
Scene lifecycle lacked a public Inspector bridge.
Resolved in package with SceneLifecycleEvents.

UX-M02-003
Moving a component inside a prefab changes its fileID and can leave stale
scene overrides. Reassign or revert orphaned overrides after restructuring.

UX-M02-004
The local presenter counts/logs its Initial visual state. Cleanup is optional
and does not affect framework lifecycle correctness.
```

## Fora de escopo confirmado

```text
receiver failure;
required/optional readiness;
negative mismatch cases;
assert panel;
fault injection;
stress;
Reset;
Player;
Pause;
gameplay camera.
```

Required/optional readiness moves to M03. Synthetic negative coverage remains a deferred QA follow-up.

## QA follow-up deferred

```text
QA-M02-001
Activity already active must not duplicate Enter/Exit.
Destination: QAFramework
Status: Deferred
```

---

# 12. M03 — Activity Readiness

## Objetivo

Demonstrar uma Activity aguardando uma condição válida antes de ficar pronta.

## Caminho feliz

```text
Activity entra
→ participant inicia preparação
→ painel mostra Waiting
→ preparação conclui
→ Activity fica Ready
```

## Assets

```text
GA_M03_Readiness
Route_M03_Readiness
Activity_M03_Preparation
```

## Cenas

```text
M03_Boot
M03_Route
M03_Activity_Add
```

## Prefabs

```text
PF_M03_PreparationParticipant
PF_M03_ReadinessDisplay
PF_M03_PreparedContent
```

## Comportamento sugerido

Usar uma preparação visual curta e determinística:

```text
montar uma pequena plataforma;
abrir uma porta;
ativar um terminal;
concluir uma animação.
```

Não usar falha artificial.

## Montagem

- [ ] Criar Activity com requirement compatível com o participant escolhido.
- [ ] Criar participant de preparação.
- [ ] Expor no mundo os estados `Preparing` e `Ready`.
- [ ] Manter o conteúdo interativo desabilitado antes de Ready.
- [ ] Habilitar o conteúdo quando a preparação concluir.
- [ ] Exibir blocking reason somente quando houver um problema real de authoring.

## Fluxo em Play Mode

```text
entrar na Activity
→ observar preparação
→ observar Ready
→ usar o conteúdo preparado
→ sair
```

## Critério de aceite

- [ ] Waiting e Ready são compreensíveis visualmente.
- [ ] O designer identifica qual participant participa da readiness.
- [ ] A Activity não depende de timer mágico do controlador de demo.
- [ ] O fluxo usa o contrato oficial.
- [ ] A reentrada repete a preparação normalmente.

## Pontos de UX a registrar

```text
A relação participant → readiness é clara?
O Inspector mostra requiredness?
A Activity explica por que está Waiting?
O designer precisa abrir logs para entender o estado?
Falta uma superfície authoring mais direta?
```

## QA Follow-ups

```text
required participant failure;
optional failure;
timeout;
participant ausente;
readiness duplicada;
late completion.
```

---

# 13. M04 — Content Anchors

## Objetivo

Demonstrar objetos já existentes em cena declarando ownership de Route, Activity ou Local.

## Caminho feliz

```text
Route entra
→ Route Anchor é descoberto

Activity entra
→ Activity Anchors são descobertos

Activity sai
→ bindings da Activity são liberados

Route sai
→ binding da Route é liberado
```

## Assets

```text
GA_M04_ContentAnchors
Route_M04_ContentAnchors
Activity_M04_A
Activity_M04_B
```

## Cenas

```text
M04_Boot
M04_Route
M04_ActivityA_Add
M04_ActivityB_Add
```

## Prefabs

```text
PF_M04_RouteRootAnchor
PF_M04_ActivityRootAnchor
PF_M04_ActivitySlotAnchor
PF_M04_LocalPointAnchor
PF_M04_AnchorStatusDisplay
```

## Montagem

### Route Anchor

- [ ] Criar um objeto de ambiente na cena da Route.
- [ ] Adicionar a declaração oficial de Content Anchor.
- [ ] Configurar scope `Route`.
- [ ] Configurar kind `Root`.
- [ ] Dar identidade e nome compreensíveis.

### Activity Anchors

- [ ] Criar um root de conteúdo em `M04_ActivityA_Add`.
- [ ] Configurar scope `Activity` e kind `Root`.
- [ ] Criar um ponto de interação com kind `Slot`.
- [ ] Criar um marcador espacial com kind `Point`.
- [ ] Repetir uma composição pequena para Activity B.

### Local Anchor

- [ ] Criar um objeto local dentro do conteúdo da Activity.
- [ ] Configurar scope `Local`.
- [ ] Explicar no README a diferença entre Local e Activity.

### Evidência

- [ ] Exibir em cada objeto um label de scope/kind.
- [ ] Em Advanced/Debug, mostrar owner e binding status.
- [ ] Não serializar cada objeto individualmente no `ActivityAsset`.

## Fluxo em Play Mode

```text
Route / Activity A
→ observar anchors A

Activity B
→ anchors A saem
→ anchors B aparecem

Menu
→ nenhum conteúdo da Route permanece
```

## Critério de aceite

- [ ] Ownership é compreensível no Inspector.
- [ ] Root, Slot e Point não recebem comportamento mágico.
- [ ] O designer entende que kind expressa intenção.
- [ ] O cleanup normal é visível.
- [ ] O modelo não inclui mismatch proposital.

## Pontos de UX a registrar

```text
A diferença entre scope e kind é clara?
O owner precisa ser preenchido manualmente?
Há IDs demais para o usuário normal?
A Activity poderia materializar isso via Composer?
O objeto mostra quando está bound?
Falta um gizmo ou ícone de cena?
```

## QA Follow-ups

```text
Route mismatch;
Activity mismatch;
duplicate anchor identity;
invalid scope;
invalid kind;
cleanup failure;
binding duplicado.
```

---

# 14. M05 — Anchor Materialization

## Prioridade

Opcional. Abrir somente quando o uso de materialização for necessário para uma demonstração real.

## Objetivo

Demonstrar um prefab sendo materializado em um anchor explícito e liberado com seu scope.

## Assets e cenas

```text
GA_M05_Materialization
Route_M05_Materialization
Activity_M05_Materialization
M05_Boot
M05_Route
M05_Activity_Add
```

## Prefabs

```text
PF_M05_Anchor
PF_M05_MaterializedContent
PF_M05_MaterializationBridge
```

## Montagem

- [ ] Criar um anchor Transform visível.
- [ ] Adicionar o bridge oficial.
- [ ] Selecionar o prefab explicitamente.
- [ ] Selecionar o anchor explicitamente.
- [ ] Configurar scope e owner.
- [ ] Configurar release policy.
- [ ] Materializar pelo fluxo oficial previsto pelo componente.
- [ ] Mostrar a instância usando um comportamento simples.

## Fluxo em Play Mode

```text
Activity entra
→ conteúdo é materializado no anchor
→ conteúdo é usado
→ Activity sai
→ conteúdo é liberado
```

## Critério de aceite

- [ ] Uma única instância é criada no caminho feliz.
- [ ] Parent e transform estão corretos.
- [ ] Ownership é visível em Advanced/Debug.
- [ ] Release ocorre na saída normal.
- [ ] O Inspector não exige conhecimento de contratos internos.

## QA Follow-ups

```text
missing prefab;
missing anchor;
duplicate materialization;
invalid owner;
failed release;
binding runtime ausente.
```

---

# BLOCO B — PLAYER, INPUT E CÂMERA

# 15. M06 — Scene-Provided Player

## Objetivo

Demonstrar a adoção de um Player já authorado na cena.

## Caminho feliz

```text
Activity entra
→ Host de cena é encontrado
→ Slot é reservado
→ Actor é associado
→ Player é admitido
→ Activity fica gameplay-ready
→ saída libera participação
```

## Assets

```text
GA_M06_ScenePlayer
Route_M06_ScenePlayer
Activity_M06_ScenePlayer
PlayerSlot_M06_Player1
Actor_M06_Default
```

## Cenas

```text
M06_Boot
M06_Route
M06_Activity_Add
```

## Prefabs

```text
PF_M06_SceneProvidedPlayer
PF_M06_PlayerActor
PF_M06_PlayerStatusDisplay
```

## Componentes principais esperados

```text
PlayerInput
LocalPlayerHostAuthoring
SceneLocalPlayerAdmissionAuthoring
PlayerActorDeclaration
game-specific movement
```

Adicionar Camera somente se o contrato de admissão exigir evidência de gameplay-ready. Caso contrário,
deixar Camera para M10.

## Montagem

- [ ] Criar `PlayerSlotProfile`.
- [ ] Criar `ActorProfile`.
- [ ] Definir Actor padrão do Slot quando aplicável.
- [ ] Preparar o Player scene-authored.
- [ ] Adicionar `LocalPlayerHostAuthoring`.
- [ ] Adicionar `SceneLocalPlayerAdmissionAuthoring`.
- [ ] Adicionar o Actor ou mount conforme o contrato atual.
- [ ] Configurar `PlayerActorDeclaration`.
- [ ] Configurar a Activity para projetar o Slot.
- [ ] Selecionar o requirement mínimo que o modelo deseja demonstrar.
- [ ] Exibir Slot, Host, Actor e participation state.

## Fluxo em Play Mode

```text
entrar na Activity
→ Player é admitido
→ mover o Player
→ sair para Menu
→ reentrar
→ Player é admitido novamente sem duplicação visível
```

## Critério de aceite

- [ ] A composição do prefab é compreensível.
- [ ] Slot, Host e Actor são distinguíveis.
- [ ] O movimento continua sendo código do jogo.
- [ ] O framework controla participação, não movimento.
- [ ] Release normal ocorre na saída.
- [ ] Reentrada funciona.

## Pontos de UX a registrar

```text
É claro onde colocar admission authoring?
Host e Actor parecem conceitos duplicados?
O Slot é selecionado de forma direta?
A configuração da Activity está legível?
O prefab exige componentes em roots específicos?
Falta um Composer de Scene-Provided Player?
```

## QA Follow-ups

```text
Slot ocupado;
Host inválido;
Actor ausente;
duplicate Actor;
admission failure;
cleanup failure;
re-entry race.
```

---

# 16. M07 — Manager-Provisioned Player

## Objetivo

Demonstrar criação autorizada de Player via `PlayerInputManager`.

## Assets e cenas

```text
GA_M07_ProvisionedPlayer
Route_M07_ProvisionedPlayer
Activity_M07_ProvisionedPlayer
PlayerSlot_M07_Player1
Actor_M07_Default
M07_Boot
M07_Route
M07_Activity_Add
```

## Prefabs

```text
PF_M07_PlayerInputManagerHost
PF_M07_RuntimePlayer
PF_M07_PlayerActor
PF_M07_JoinControl
PF_M07_PlayerStatusDisplay
```

## Montagem

- [ ] Criar Slot e Actor.
- [ ] Criar Player prefab.
- [ ] Adicionar `PlayerInput`.
- [ ] Adicionar `LocalPlayerHostAuthoring`.
- [ ] Preparar o Actor mount exigido pelo contrato atual.
- [ ] Criar `PlayerInputManager`.
- [ ] Adicionar `LocalPlayerProvisioningAuthoring`.
- [ ] Adicionar `LocalPlayerProvisioningHostRegistration`.
- [ ] Criar controle de authorized join.
- [ ] Configurar Activity participation.
- [ ] Exibir o estado `Waiting for Join`.
- [ ] Exibir Player admitido após join.

## Fluxo em Play Mode

```text
Activity entra
→ Waiting for Join
→ usuário solicita Join
→ Player é criado
→ Actor é preparado
→ Activity fica gameplay-ready
→ saída libera Player
```

## Critério de aceite

- [ ] O designer entende qual prefab será instanciado.
- [ ] O join não depende de `playerIndex` como autoridade.
- [ ] Slot e Actor são visíveis.
- [ ] O fluxo de authoring é diferente e comparável ao M06.
- [ ] A saída normal libera o Player.

## QA Follow-ups

```text
join duplicado;
Slot ocupado;
Host validation failure;
rollback;
timeout;
commit failure;
release failure.
```

---

# 17. M08 — Participation Policies

## Objetivo

Demonstrar níveis de participação por Activities pequenas.

## Estrutura

Usar uma Route com Activities separadas:

```text
Activity No Slots
Activity Joined Slots
Activity Selected Actors
Activity Logical Actors Prepared
Activity Gameplay Ready
```

Cada Activity usa a mesma composição base de Player, mas muda somente a policy que está sendo
demonstrada.

## Assets

```text
GA_M08_Participation
Route_M08_Participation
Activity_M08_NoSlots
Activity_M08_JoinedSlots
Activity_M08_SelectedActors
Activity_M08_LogicalPrepared
Activity_M08_GameplayReady
```

## Prefabs

```text
PF_M08_ParticipationPlayer
PF_M08_ParticipationStatus
PF_M08_ActivitySelector
```

## Montagem

- [ ] Criar uma base mínima de Player.
- [ ] Criar as cinco Activities.
- [ ] Alterar apenas projection/requirement de cada Activity.
- [ ] Exibir `Required Level`.
- [ ] Exibir `Observed Level`.
- [ ] Exibir `Ready`.
- [ ] Documentar a intenção de cada policy.

## Fluxo em Play Mode

```text
selecionar Activity
→ observar requirement
→ observar estado alcançado
→ trocar para próxima Activity
```

## Critério de aceite

- [ ] A diferença entre níveis é compreensível.
- [ ] Não há scripts distintos por Activity.
- [ ] A policy é authorada no asset correto.
- [ ] O modelo não injeta estados inválidos.

## QA Follow-ups

```text
missing evidence;
forced premature ready;
inconsistent projection;
late Actor preparation;
release between levels.
```

---

# 18. M09 — Input Gate

## Objetivo

Demonstrar elegibilidade de input e bloqueio temporário.

## Assets e cenas

```text
GA_M09_InputGate
Route_M09_InputGate
Activity_M09_InputGate
M09_Boot
M09_Route
M09_Activity_Add
```

## Prefabs

```text
PF_M09_Player
PF_M09_InteractionTarget
PF_M09_GateControl
PF_M09_InputStatus
```

## Montagem

- [ ] Reutilizar a composição conceitual de Player, mas criar assets próprios.
- [ ] Adicionar o adapter oficial de Input Gate.
- [ ] Criar uma ação de movimento observável.
- [ ] Criar uma interação simples observável.
- [ ] Criar botão `Acquire Gate`.
- [ ] Criar botão `Release Gate`.
- [ ] Exibir `Input Eligible`, `Interaction Eligible` e `Gameplay Eligible`.

## Fluxo em Play Mode

```text
mover e interagir
→ Acquire Gate
→ movimento e interação param
→ Release Gate
→ movimento e interação retornam
```

## Critério de aceite

- [ ] O framework não implementa movimento.
- [ ] O bloqueio é evidente.
- [ ] A restauração ocorre no caminho feliz.
- [ ] A UI explica o estado sem relatório técnico.

## QA Follow-ups

```text
double acquire;
release sem acquire;
Gate stale;
Activity exit com Gate;
binding ausente;
action map inválido.
```

---

# 19. M10 — Player Camera

## Objetivo

Demonstrar uma câmera do Player publicando request para um único output físico.

## Assets e cenas

```text
GA_M10_PlayerCamera
Route_M10_PlayerCamera
Activity_M10_PlayerCamera
CameraRig_M10_Player
M10_Boot
M10_Route
M10_Activity_Add
```

## Prefabs

```text
PF_M10_PersistentCameraOutput
PF_M10_Player
PF_M10_PlayerCameraRig
PF_M10_CameraStatus
```

## Montagem

- [ ] Criar output físico com Camera e CinemachineBrain.
- [ ] Adicionar `CameraOutputSessionBinding`.
- [ ] Criar `CameraRigRecipe`.
- [ ] Criar ou aplicar `CameraRigComposer`.
- [ ] Configurar follow/look targets.
- [ ] Adicionar `PlayerGameplayCameraAuthoring`.
- [ ] Adicionar o binding de request atual do Player.
- [ ] Exibir `Active Camera Request` e `Output Winner`.

## Fluxo em Play Mode

```text
Activity entra
→ Player é admitido
→ camera request é publicado
→ output segue o Player
→ Activity sai
→ request é liberado
```

## Critério de aceite

- [ ] Existe um único output físico.
- [ ] O rig é authorável e reutilizável.
- [ ] O request não depende de busca global.
- [ ] O release normal restaura o estado esperado.
- [ ] Overrides não fazem parte deste modelo.

## QA Follow-ups

```text
dois requests;
prioridade empatada;
output ausente;
release failure;
request stale;
Player release antes da Camera.
```

---

# BLOCO C — ESTADO, RESTART E APRESENTAÇÃO

# 20. M11 — Object Reset

## Objetivo

Demonstrar restauração de objetos de cena e estado de script.

## Assets e cenas

```text
GA_M11_Reset
Route_M11_Reset
Activity_M11_Reset
M11_Boot
M11_Route
M11_Activity_Add
```

## Prefabs

```text
PF_M11_TransformResettable
PF_M11_StateResettable
PF_M11_RuntimeSpawnedObject
PF_M11_ResetControls
PF_M11_ResetStatus
```

## Montagem

- [ ] Criar objeto que pode ser movido.
- [ ] Adicionar `UnityResetSubjectAdapter`.
- [ ] Adicionar participant de Transform Reset.
- [ ] Criar objeto com estado de script.
- [ ] Implementar o contrato resettable oficial aplicável.
- [ ] Criar `ObjectResetTrigger`.
- [ ] Criar `ObjectResetGroupTrigger`.
- [ ] Opcionalmente criar um runtime spawner válido.
- [ ] Exibir `Last Reset` e contagem resumida.

## Fluxo em Play Mode

```text
mover objeto
→ alterar estado
→ gerar objeto runtime
→ Reset Object
→ alterar novamente
→ Reset Group
```

## Critério de aceite

- [ ] O resultado é visual.
- [ ] A identidade dos Subjects é clara.
- [ ] Object Reset e Group Reset são distinguíveis.
- [ ] O modelo não usa Activity Restart.
- [ ] Runtime object só entra se o fluxo oficial estiver suficientemente authorável.

## QA Follow-ups

```text
duplicate Subject identity;
participant failure;
group partial failure;
runtime registration failure;
unregister failure.
```

---

# 21. M12 — Activity Restart

## Objetivo

Demonstrar a diferença entre Reset de objetos e Restart completo da Activity.

## Assets e cenas

```text
GA_M12_ActivityRestart
Route_M12_ActivityRestart
Activity_M12_Gameplay
M12_Boot
M12_Route
M12_Activity_Add
```

## Prefabs

```text
PF_M12_RestartableObjective
PF_M12_RestartableWorld
PF_M12_ActivityRestartControl
PF_M12_RestartStatus
```

## Montagem

- [ ] Criar Activity com estado inicial visível.
- [ ] Adicionar Subjects necessários.
- [ ] Criar objetivo simples.
- [ ] Alterar mundo ao completar o objetivo.
- [ ] Adicionar `ActivityRestartTrigger`.
- [ ] Exibir resumidamente Reset, Exit, Enter e Ready.
- [ ] Não chamar SceneManager diretamente para simular restart.

## Fluxo em Play Mode

```text
entrar
→ alterar estado
→ completar objetivo
→ Restart Activity
→ observar Reset
→ observar reentrada
→ repetir o fluxo
```

## Critério de aceite

- [ ] O mundo retorna ao estado inicial.
- [ ] A Activity executa lifecycle normal.
- [ ] Não há objeto residual visível.
- [ ] O usuário entende a diferença para M11.
- [ ] O trigger oficial é a entrada de produto.

## QA Follow-ups

```text
Reset failure;
clear failure;
re-entry failure;
restart repetido;
restart durante transition;
stale Subject.
```

---

# 22. M13 — Pause

## Objetivo

Demonstrar Pause como feature independente.

## Variantes internas

```text
Variant A
  Pause sem Player.

Variant B
  Pause com PlayerInput.
```

Podem estar em duas Activities ou duas cenas pequenas dentro do mesmo modelo.

## Assets e cenas

```text
GA_M13_Pause
Route_M13_Pause
Activity_M13_ApplicationPause
Activity_M13_PlayerPause
M13_Boot
M13_Route
M13_ApplicationPause_Add
M13_PlayerPause_Add
```

## Prefabs

```text
PF_M13_PauseSurface
PF_M13_PauseControls
PF_M13_Player
PF_M13_PausePlayerBinding
PF_M13_PauseStatus
```

## Montagem

- [ ] Criar Pause Surface.
- [ ] Adicionar `PauseRequestTrigger`.
- [ ] Configurar Pause e Resume.
- [ ] Exibir `Paused`, `Time Scale`, `Input Mode`.
- [ ] Na variante com Player, adicionar `PausePlayerInputBinding`.
- [ ] Garantir que a UI de Pause usa o EventSystem correto.

## Fluxo em Play Mode

```text
Variant A
  Pause
  Resume

Variant B
  mover Player
  Pause
  confirmar movimento bloqueado
  Resume
  confirmar movimento restaurado
  voltar ao Menu
```

## Critério de aceite

- [ ] A diferença entre aplicação e Player está clara.
- [ ] A Pause Surface é reutilizável.
- [ ] O estado visual é suficiente.
- [ ] Saída normal ao Menu restaura a aplicação.
- [ ] O modelo não inclui fault injection.

## QA Follow-ups

```text
Pause duplicado;
Resume sem Pause;
stale binding;
exit cleanup failure;
restart while paused;
Gate imbalance.
```

---

# 23. M14 — Transition and Loading

## Objetivo

Demonstrar superfícies e políticas de transição sem combinar todas as features.

## Assets e cenas

```text
GA_M14_TransitionLoading
Route_M14_Menu
Route_M14_Destination
Activity_M14_Light
Activity_M14_Loaded
M14_Boot
M14_Menu
M14_Destination
M14_Light_Add
M14_Loaded_Add
```

## Prefabs

```text
PF_M14_TransitionSurface
PF_M14_LoadingSurface
PF_M14_Navigation
PF_M14_TransitionStatus
```

## Montagem

- [ ] Criar Transition Surface.
- [ ] Criar Loading Surface.
- [ ] Associar os adapters oficiais.
- [ ] Configurar uma Route transition com loading.
- [ ] Configurar uma Activity transition sem apresentação de loading.
- [ ] Exibir estado simples: Covering, Loading, Revealing, Ready.
- [ ] Não adicionar Player salvo quando necessário para mostrar Gate.

## Fluxo em Play Mode

```text
Menu
→ Destination Route com loading
→ Light Activity sem loading visual
→ Loaded Activity com política explícita
→ Menu
```

## Critério de aceite

- [ ] O usuário entende que scene loading e loading presentation são conceitos diferentes.
- [ ] Nenhum estado intermediário inválido é visível.
- [ ] As superfícies são prefabs reutilizáveis.
- [ ] As policies são authoradas nos assets esperados.

## QA Follow-ups

```text
load failure;
transition adapter failure;
Gate release failure;
operation cancellation;
progress invalid;
surface ausente.
```

---

# BLOCO D — EXTENSÕES

# 24. M15 — Camera Overrides

## Objetivo

Demonstrar override de Activity sobre Player Camera e restauração normal.

## Dependência conceitual

M10 Player Camera.

## Assets e cenas

```text
GA_M15_CameraOverrides
Route_M15_CameraOverrides
Activity_M15_PlayerCamera
Activity_M15_Cinematic
M15_Boot
M15_Route
M15_Player_Add
M15_Cinematic_Add
```

## Prefabs

```text
PF_M15_PlayerCamera
PF_M15_ActivityCameraOverride
PF_M15_CameraStatus
```

## Fluxo em Play Mode

```text
Player Camera
→ Cinematic Activity
→ Activity override vence
→ voltar
→ Player Camera restaurada
```

## Critério de aceite

- [ ] O vencedor é compreensível.
- [ ] A prioridade é authorada explicitamente.
- [ ] O release restaura a câmera anterior.
- [ ] Session e Route overrides permanecem fora de escopo deste primeiro corte.

## QA Follow-ups

```text
priority tie;
multiple overrides;
stale override;
release order;
missing output.
```

---

# 25. M16 — BGM

## Status

Experimental.

## Objetivo

Demonstrar o adapter opcional de BGM sem torná-lo dependência dos demais modelos.

## Assets e cenas

```text
GA_M16_Bgm
Route_M16_Bgm
Activity_M16_OwnMusic
Activity_M16_UseRoute
Activity_M16_Silence
M16_Boot
M16_Route
M16_OwnMusic_Add
M16_UseRoute_Add
M16_Silence_Add
```

## Prefabs

```text
PF_M16_BgmDirector
PF_M16_RouteBgmBinding
PF_M16_ActivityBgmBinding
PF_M16_BgmStatus
```

## Fluxo em Play Mode

```text
Route BGM
→ Activity own BGM
→ Activity use Route
→ Activity silence
→ Route BGM restaurada
```

## Critério de aceite

- [ ] A policy efetiva é compreensível.
- [ ] O status Experimental está visível.
- [ ] O modelo não bloqueia o roadmap principal.
- [ ] A restauração normal funciona.

## QA Follow-ups

```text
clip ausente;
binding duplicado;
release failure;
policy inválida;
director ausente.
```

---

# 26. Ordem recomendada de montagem

## Fase 1 — Base authorável

```text
M01 Route and Activity
M02 Lifecycle Events
M03 Activity Readiness
M04 Content Anchors
```

Objetivo da fase:

```text
fechar criação, composição, lifecycle e ownership;
identificar gaps de UX fundamentais;
estabelecer padrão visual e documental.
```

## Fase 2 — Player

```text
M06 Scene-Provided Player
M07 Manager-Provisioned Player
M08 Participation Policies
M09 Input Gate
M10 Player Camera
```

Objetivo da fase:

```text
provar dois caminhos de Player;
comparar authoring;
separar participação, input e câmera;
extrair prefabs reutilizáveis.
```

## Fase 3 — Estado e experiência

```text
M11 Object Reset
M12 Activity Restart
M13 Pause
M14 Transition and Loading
```

Objetivo da fase:

```text
provar restauração, reentrada, bloqueio e apresentação;
manter cada feature compreensível isoladamente.
```

## Fase 4 — Extensões

```text
M05 Anchor Materialization, quando necessário
M15 Camera Overrides
M16 BGM
```

---

# 27. Checklist de fechamento de um modelo

## Authoring

- [ ] Assets criados pela superfície oficial.
- [ ] Componentes configurados pelo Inspector.
- [ ] Nenhuma edição manual de YAML.
- [ ] Nenhum script de bootstrap mágico.
- [ ] Nenhum singleton ou lookup global.
- [ ] Dependências documentadas.
- [ ] Prefabs reutilizáveis identificados.

## Runtime

- [ ] Caminho feliz executado.
- [ ] Resultado visual compreensível.
- [ ] Cleanup normal executado.
- [ ] Reentrada normal executada quando aplicável.
- [ ] Nenhum erro de compilação.
- [ ] Nenhum erro runtime.
- [ ] Logs principais diagnosticáveis.

## Produto

- [ ] Um designer entende o modelo sem abrir código.
- [ ] O Inspector apresenta primeiro a intenção.
- [ ] Advanced/Debug contém evidência técnica.
- [ ] A cena não carrega sistemas desnecessários.
- [ ] O custo da feature é observável.
- [ ] O README explica como reutilizar.
- [ ] Gaps de UX foram registrados.

## QA handoff

- [ ] Casos negativos percebidos foram listados.
- [ ] Nenhum caso negativo foi implementado no FIRSTGAME.
- [ ] O contrato técnico a provar foi identificado.
- [ ] O resultado esperado foi descrito para QA.

---

# 28. Registro de UX por modelo

Usar uma tabela curta no README:

| Área | Observação | Impacto | Destino |
|---|---|---|---|
| Creation |  | Low/Medium/High | Package/Docs/FIRSTGAME |
| Inspector |  | Low/Medium/High | Package |
| Composition |  | Low/Medium/High | Package/Template |
| Runtime |  | Low/Medium/High | Package/QA |
| Diagnostics |  | Low/Medium/High | Package |
| Reuse |  | Low/Medium/High | Sample/Template |
| Performance |  | Low/Medium/High | Package/QA |

Perguntas obrigatórias:

```text
Quantos passos foram necessários?
Quais passos eram técnicos demais?
Havia configuração duplicada?
Alguma dependência ficou escondida?
Um Composer reduziria erro sem esconder a materialização?
Um Recipe/Profile ajudaria?
O prefab é realmente reutilizável?
O modelo carregou algo que não usou?
O diagnóstico normal foi suficiente?
```

---

# 29. Registro de QA Follow-up

Formato:

```text
QA Follow-up ID:
Source Model:
Feature:
Contract:
Scenario:
Expected Result:
Risk:
Suggested QA Fixture:
Priority:
```

Exemplo:

```text
QA Follow-up ID: QA-M02-001
Source Model: M02 Lifecycle Events
Feature: Activity lifecycle participant
Contract: Activity Enter is idempotent per admitted participant occurrence
Scenario: request the already active Activity repeatedly
Expected Result: no duplicate Enter callback
Risk: duplicate gameplay initialization
Suggested QA Fixture: synthetic Activity participant with invocation count
Priority: High
```

Esse registro não vira botão ou cena no FIRSTGAME.

---

# 30. Próximo modelo a montar

## M03 — Activity Readiness

M01 e M02 estão fechados no FIRSTGAME. O próximo corte deve provar a experiência de authoring e diagnóstico
de readiness, sem ampliar o M02 e sem antecipar Content Anchors.

Objetivo:

```text
configurar readiness required e optional;
mostrar quando uma Activity fica Ready;
mostrar quando uma dependência bloqueia readiness;
distinguir falha obrigatória de ausência opcional;
manter diagnóstico explícito e sem fallback silencioso.
```

Entregas esperadas:

```text
M03_Boot
M03_Route
M03_ActivityReady
M03_ActivityBlocked
M03_ActivityOptional

GA_M03_Readiness
Route_M03
Activity_M03_Ready
Activity_M03_Blocked
Activity_M03_Optional

recipe/profile ou superfície oficial existente;
presenter compacto de readiness;
README.md;
UX findings;
QA follow-ups registrados, sem implementar QA neste corte.
```

Fora de escopo:

```text
Content Anchors;
materialization;
Player;
Reset;
Pause;
fault injection;
smoke menu como fluxo principal.
```

O M03 deve responder:

```text
como o usuário declara a intenção de readiness?
onde ele vê o bloqueio?
qual referência é obrigatória?
como um estado opcional é explicado?
como corrigir a configuração sem inspecionar logs extensos?
```
