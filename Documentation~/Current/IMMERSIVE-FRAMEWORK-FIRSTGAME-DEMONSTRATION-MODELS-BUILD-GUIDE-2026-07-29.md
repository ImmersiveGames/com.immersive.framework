# Immersive Framework — Roteiro Operacional de Modelos de Demonstração

Status: guia de montagem — arquitetura base materializada  
Data: 2026-07-29  
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

## 5.1 Regra de materialização da arquitetura inicial

O corte inicial de arquitetura cria somente:

```text
FrameworkModels/
Shared/ e suas categorias permitidas
raízes M01 a M16
```

As pastas internas padrão de cada modelo não são criadas antecipadamente. Elas devem surgir apenas
quando o modelo entrar em `Authoring` e somente quando houver conteúdo real para a categoria.

Para manter as raízes vazias rastreáveis no Git sem produzir assets Unity prematuros:

```text
cada raiz de modelo contém somente .gitkeep;
folder .meta files são entregues junto com a estrutura;
.gitkeep deve ser removido quando o primeiro arquivo real entrar na pasta;
nenhuma cena, prefab, ScriptableObject ou script é criado neste corte.
```

Estado do corte de arquitetura em 2026-07-29: `Closed`.

---

# 6. Convenções de nomenclatura

## Assets

```text
GA_M01_RouteActivity
Route_M01_Menu
Route_M01_Gameplay
Activity_M01_A
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

## Fundação da coleção

| Corte | Entrega | Estado |
|---|---|---|
| F0 | Arquitetura rastreável de `FrameworkModels`, `Shared` e raízes `M01`–`M16` | Closed |

A conclusão de F0 não altera o estado de nenhum modelo. Um modelo muda de `Pending` para `Authoring`
somente quando sua montagem específica começar.

| Ordem | Modelo | Tipo | Estado |
|---:|---|---|---|
| 1 | M01 Route and Activity | Fundação | Closed |
| 2 | M02 Lifecycle Events | Fundação | Authoring |
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

Status: **Closed**  
Closed: 2026-07-30

## Objetivo

Demonstrar a estrutura mínima de Application, Route e Activity sem Player, Camera de gameplay, Reset ou
Pause.

## Resultado comprovado

```text
Boot
→ Menu Route sem Activity
→ Gameplay Route + startup Activity A
→ Activity B
→ Activity A
→ Menu
→ Gameplay + startup Activity A novamente
```

A execução real confirmou:

```text
zero Local Player Slots;
Player runtime = NotConfigured;
Scene Local Player admission = NotConfigured;
startup Menu sem Activity;
Gameplay inicia Activity A;
troca A/B libera a cena anterior e carrega uma cena nova;
retorno ao Menu limpa Gameplay e Activity;
reentrada inicia Activity A novamente;
activitySceneLedgerStale = 0;
blockingIssues = 0.
```

## Assets finais

```text
Application/GA_M01_RouteActivity.asset
Routes/Route_M01_Menu.asset
Routes/Route_M01_Gameplay.asset
Activities/Activity_M01_A.asset
Activities/Activity_M01_B.asset
Profiles/ActivityContent_M01_A.asset
Profiles/ActivityContent_M01_B.asset
```

## Cenas finais

```text
Scenes/M01_PersistentContent.unity
Scenes/M01_Boot.unity
Scenes/M01_Menu.unity
Scenes/M01_Gameplay.unity
Scenes/M01_ActivityA_Add.unity
Scenes/M01_ActivityB_Add.unity
```

## Prefabs finais

```text
Prefabs/PF_M01_RouteNavigation.prefab
Prefabs/PF_M01_ActivityNavigation.prefab
Prefabs/PF_M01_CurrentContextDisplay.prefab
```

`PF_M01_CurrentContextDisplay` permanece como placeholder e não participa do aceite. O package ainda não
expõe uma superfície pública tipada para apresentar Current Route e Current Activity sem acesso a internals,
reflection ou lookup global.

## Correções de produto originadas no M01

```text
Game Application local validation
  não agrega Project Profile Audit;
  zero Local Player Slots é válido quando Player não está configurado.

Bootstrap runtime
  não compõe Player Participation, Actor Preparation ou Player Gameplay para zero Slots.

FrameworkRuntimeHost
  não compõe Scene Local Player admission quando Player Participation está NotConfigured.
```

## Evidência técnica

```text
GAME_APPLICATION_VALIDATION_SCOPE_SMOKE
  status = Passed
  cases = 3

ZERO_SLOT_BOOTSTRAP_COMPOSITION_POLICY_SMOKE
  status = Passed
  cases = 5

P3F_SESSION_SLOT_RUNTIME_SMOKE
  status = Passed
  cases = 17

M01_ZERO_PLAYER_BOOT_SMOKE
  status = Passed
  cases = 5
```

## Critério de aceite

- [x] A Game Application valida sem Player configurado.
- [x] O boot inicia a Menu Route sem Activity.
- [x] O trigger de Route abre Gameplay.
- [x] Gameplay inicia Activity A.
- [x] Activity A e B alternam sem duplicação.
- [x] Gameplay permanece durante a troca de Activity.
- [x] Retornar ao Menu libera Gameplay e Activity.
- [x] Reentrar em Gameplay inicia Activity A novamente.
- [x] Nenhuma operação principal reportou blocking issue.
- [x] Nenhuma feature de Player foi criada como fallback.

## Findings de UX/produto

```text
UX-M01-001 — Player era tratado como obrigatório na validação e no boot.
Destino: corrigido no package e coberto no QA.

UX-M01-002 — Persistent Content mínimo inclui Camera, Loading, Transition e Pause.
Destino: revisão futura de Recipes/Templates de aplicação mínima.

UX-M01-003 — não existe binding público tipado para Current Route/Activity.
Destino: nova superfície de apresentação read-only no package.

UX-M01-004 — logs Debug de requests são excessivamente volumosos para uso cotidiano.
Destino: separar resumo, debug operacional e trace avançado.
```

## Peças reutilizáveis

Os prefabs de navegação podem ser testados em outro modelo, mas ainda não são templates oficiais. O painel de
contexto não deve ser promovido enquanto o package não oferecer um binding público tipado.

---

# 11. M02 — Lifecycle Events

Status: **Authoring**  
Started: 2026-07-30

## Objetivo

Demonstrar objetos reagindo ao lifecycle oficial de Scene, Route e Activity sem criar uma autoridade paralela
de lifecycle no jogo consumidor.

## Resultado esperado para o usuário

O usuário entende:

```text
qual evento pertence à Scene;
qual evento pertence à Route;
qual evento pertence à Activity;
como adicionar um participant oficial;
como ligar callbacks a apresentação local;
como observar Enter/Exit sem usar Console como fluxo principal.
```

## Assets planejados

```text
Application/GA_M02_Lifecycle.asset
Routes/Route_M02_A.asset
Routes/Route_M02_B.asset
Activities/Activity_M02_A.asset
Activities/Activity_M02_B.asset
Profiles/ActivityContent_M02_A.asset
Profiles/ActivityContent_M02_B.asset
```

## Cenas planejadas

```text
Scenes/M02_PersistentContent.unity
Scenes/M02_Boot.unity
Scenes/M02_RouteA.unity
Scenes/M02_RouteB.unity
Scenes/M02_ActivityA_Add.unity
Scenes/M02_ActivityB_Add.unity
```

## Prefabs de habilidade

```text
Prefabs/PF_M02_SceneLifecycleObject.prefab
Prefabs/PF_M02_RouteLifecycleObject.prefab
Prefabs/PF_M02_ActivityLifecycleObject.prefab
```

## Regra de independência

```text
não reutilizar Game Application, Routes, Activities ou cenas do M01;
não depender do Menu do M01;
não copiar Host, bootstrap ou componentes de cenas antigas;
zero Player Slots permanece válido;
Persistent Content é próprio do M02;
scripts do consumidor apresentam estado, mas não disparam lifecycle manualmente.
```

## Grafo autoral inicial

```text
GA_M02_Lifecycle
  Content Scene: M02_PersistentContent
  Startup Route: Route_M02_A
  Local Player Slots: empty

Route_M02_A
  Primary Scene: M02_RouteA
  First Activity: Activity_M02_A

Route_M02_B
  Primary Scene: M02_RouteB
  First Activity: Activity_M02_B

Activity_M02_A
  Activity Content Profile: ActivityContent_M02_A
  Projection: No Slots
  Zero Participants: Allowed
  Requirement Level: None

Activity_M02_B
  Activity Content Profile: ActivityContent_M02_B
  Projection: No Slots
  Zero Participants: Allowed
  Requirement Level: None
```

## Corte atual — fundação autoral completa

A primeira versão do corte dependia implicitamente do scaffold unificado M02-M16. Essa dependência deixou o
início do M02 incompleto quando o scaffold não havia sido materializado. O comando do M02 agora é autocontido:

```text
Tools
→ Immersive Framework
→ FIRSTGAME
→ M02
→ Resolve Application Foundation
```

O comando:

```text
cria todos os assets autorais ausentes do M02;
cria as cinco cenas não persistentes ausentes;
cria os três prefabs placeholders de lifecycle;
cria M02_PersistentContent a partir da fonte oficial do package;
remove somente Main Camera/EventSystem gerados em hierarquias M02 reconhecidas;
preserva arquivos existentes;
não atribui referências entre assets;
não adiciona participants;
não instala bootstrap;
não altera Build Profiles ou ProjectSettings.
```

Semântica de startup:

```text
Build Profile entry scene: M02_Boot
Game Application Startup Route: Route_M02_A
Route_M02_A Primary Scene: M02_RouteA
Game Application Content Scene: M02_PersistentContent
```

Não reutilizar `M01_PersistentContent`. A cena persistente própria do M02 preserva isolamento e ownership do
modelo, mesmo que ambas sejam originadas da mesma template oficial.

## Primeiro bloco de montagem

1. Executar `M02 > Resolve Application Foundation`.
2. Confirmar o inventário completo de assets, seis cenas e três prefabs.
3. Configurar os dois Activity Content Profiles.
4. Configurar Activities A/B como `No Slots`.
5. Configurar Route A/B e suas startup Activities.
6. Configurar `GA_M02_Lifecycle` com Content Scene `M02_PersistentContent` e Startup Route `Route_M02_A`.
7. Colocar `M02_Boot` como cena de entrada do Build Profile.
8. Validar o grafo antes de adicionar qualquer lifecycle participant.

## Fluxo funcional planejado

```text
Boot
→ Route A + Activity A
→ Activity B
→ Route B + Activity B
→ Route A + Activity A
```

Não existe etapa `Menu` neste modelo. O M02 é isolado e possui apenas Route A e Route B.

## Comportamentos visuais planejados

```text
Scene Available
  apresenta o objeto como disponível.

Scene Releasing
  apresenta o estado de saída antes do unload.

Route Enter
  ativa a apresentação persistente da Route.

Route Exit
  encerra a apresentação da Route.

Activity Enter
  ativa o objeto contextual da Activity.

Activity Exit
  encerra o estado contextual da Activity.
```

## Limite do código consumidor

```text
permitido:
  atualizar label, material, luz, animação ou contador visual;
  armazenar o último evento apenas para apresentação local.

proibido:
  chamar Enter/Exit manualmente;
  decidir qual Route/Activity está ativa;
  substituir a authority do framework;
  usar singleton, service locator ou lookup global.
```

## Critério de aceite do M02

- [ ] O grafo autoral independente valida.
- [ ] Scene, Route e Activity lifecycle são distinguíveis visualmente.
- [ ] Os participants oficiais são configuráveis pelo Inspector.
- [ ] Nenhum callback é disparado manualmente pelo consumidor.
- [ ] Os prefabs são compreensíveis isoladamente.
- [ ] A navegação repete o fluxo sem callbacks duplicados aparentes.
- [ ] Findings técnicos são transferidos ao QA, não transformados em cenas inválidas.

## Pontos de UX a registrar

```text
Como o designer encontra o participant correto?
O Inspector diferencia Enter e Exit?
O callback aceita UnityEvent ou requer adapter?
A requiredness é clara?
Há configuração repetitiva entre Scene, Route e Activity?
Um Composer/Template oficial reduziria montagem sem esconder contratos?
```

## QA Follow-ups previstos

```text
ordem exata de callbacks;
idempotência;
participant obrigatório ou opcional com falha;
exceção durante Enter ou Exit;
reentrada repetida.
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

# 30. Checkpoint atual

```text
F0 Folder Architecture: Closed
M01 Route and Activity: Closed
M02 Lifecycle Events: Authoring
Current roadmap step: M02 — configurar o grafo independente e materializar lifecycle participants oficiais.
```

## M01 — fechamento

O M01 provou o caminho feliz completo em FIRSTGAME:

```text
Boot → Menu → Gameplay/A → B → A → Menu → Gameplay/A
```

Também originou e validou correções oficiais para:

```text
escopo da validação local da Game Application;
zero Local Player Slots como feature NotConfigured;
composição condicional da pilha runtime de Player;
Scene Local Player admission opcional quando Player não está configurado.
```

O modelo foi fechado com `blockingIssues = 0`, cleanup de cenas, reentrada e `activitySceneLedgerStale = 0`.

## M02 — corte inicial

Tipo: UX/produto + integração real.

```text
Objetivo:
  provar Scene, Route e Activity lifecycle como superfícies authoráveis e visíveis.

Escopo atual:
  fundação própria;
  grafo Application/Routes/Activities;
  zero Player Slots;
  identificação dos participants oficiais atuais;
  apresentação local sem authority paralela.

Fora de escopo:
  readiness;
  Player;
  Camera de gameplay;
  Reset;
  Pause;
  casos negativos e matriz de regressão.
```

## Arquivos afetados por este corte documental

```text
com.immersive.framework
  Documentation~/Current/IMMERSIVE-FRAMEWORK-FIRSTGAME-DEMONSTRATION-MODELS-BUILD-GUIDE-2026-07-29.md

planet-devourer
  Assets/_Project/FrameworkModels/M01_RouteActivity/README.md
  Assets/_Project/FrameworkModels/M02_LifecycleEvents/README.md
  Assets/_Project/FrameworkModels/M02_LifecycleEvents/Editor/M02ApplicationFoundationResolver.cs
```

## Critério para avançar ao bloco de participants

```text
M02_PersistentContent existe;
GA_M02_Lifecycle valida;
Route A inicia Activity A;
Route B inicia Activity B;
Profiles A/B apontam para as cenas aditivas corretas;
zero Player Slots permanece válido;
nenhum componente lifecycle foi adicionado por suposição.
```

## Commit sugerido

```text
docs(firstgame): close M01 and start M02 lifecycle authoring
```



---

# 32. Correção M02 — fundação autoral autocontida

## Problema observado

O usuário chegou à configuração da Game Application sem possuir todo o inventário M02 e sem
`M02_PersistentContent`. A orientação anterior dependia implicitamente do scaffold unificado M02-M16.

## Regra corrigida

```text
M02 > Resolve Application Foundation
  → materializa assets, cenas e prefabs ausentes;
  → cria Persistent Content própria a partir da template oficial;
  → preserva conteúdo existente;
  → não configura referências ou runtime automaticamente.
```

## Startup oficial do modelo

```text
Unity entry scene: M02_Boot
Startup Route: Route_M02_A
Startup Route Primary Scene: M02_RouteA
Content Scene: M02_PersistentContent
```

`M01_PersistentContent` não deve ser reutilizada, pois M02 precisa provar isolamento de assets e ownership.
