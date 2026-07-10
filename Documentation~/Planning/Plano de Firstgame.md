# Plano Consolidado — Retomada do Immersive Framework após CameraComposer

## 1. Contexto

O estado real do projeto avançou além do roadmap canônico atual.

Já existem e foram validados:

```text
PlayerRecipe
PlayerComposer
CameraRecipe
CameraComposer
Integração explícita PlayerComposer -> CameraComposer
Materialização Cinemachine
QA técnico de câmera
Prova real no FIRSTGAME
Remoção do FrameworkCameraDirector e componentes legacy
Bindings Route/Activity com outputs Cinemachine explícitos
```

Porém, a documentação ainda mistura:

```text
estado antigo da fundação passiva F49
roadmap anterior à implementação do PlayerComposer
sequência antiga de PlayerView Binding Adapter
estado novo do CameraComposer
arquitetura Route/Activity parcialmente incompleta
```

Antes de iniciar outro sistema, o package deve voltar a possuir uma única fonte documental coerente.

---

# 2. Objetivo geral

Retomar o desenvolvimento a partir do estado real dos repositórios e evoluir o eixo principal do framework na seguinte ordem:

```text
1. Sincronizar documentação e roadmap.
2. Consolidar o produto Player atual.
3. Implementar controle e input runtime reais.
4. Validar tecnicamente no QAFramework.
5. Provar um loop jogável mínimo no FIRSTGAME.
6. Implementar materialização/spawn escopado do Player.
7. Completar o lifecycle avançado de outputs de câmera.
8. Avançar para progressão/save quando houver estado real para persistir.
```

A direção deve permanecer:

```text
produto authorável
+ contratos técnicos
+ runtime real
+ diagnóstico
```

---

# 3. Regras de execução

## Repositórios

```text
com.immersive.framework
  Fonte oficial de contracts, runtime, editor tooling, validators,
  recipes, composers, diagnostics e documentação canônica.

QAFramework
  Validação técnica, smokes sintéticos, casos negativos e regressões.

planet-devourer / FIRSTGAME
  Prova de integração real, usabilidade e fluxo mínimo de jogo.
```

## Ordem padrão

Para cortes técnicos:

```text
1. Implementar no package.
2. Validar no QAFramework.
3. Validar no FIRSTGAME quando houver uso real.
```

Para cortes de UX/produto:

```text
1. Definir a superfície de uso.
2. Provar no FIRSTGAME quando necessário.
3. Formalizar no package.
4. Criar QA do contrato oficial.
```

## Restrições

```text
Git somente leitura.
Mudanças entregues em .zip.
Sem fallback silencioso.
Sem Camera.main como autoridade.
Sem busca funcional por nome.
Sem singleton ou service locator implícito.
Sem runtime dependendo de Editor.
Sem manter arquitetura legacy apenas por compatibilidade.
```

---

# 4. Bloco R0 — Sincronização documental e seleção da lane ativa

## Tipo

```text
Documentação / governança / consolidação arquitetural
```

## Objetivo

Fazer a documentação canônica refletir o estado real do package, QAFramework e FIRSTGAME.

## Problema atual

`Current/00-Current-State.md` já reconhece o CameraComposer, mas `Current/01-Roadmap.md` ainda trata F49M como último estado e recomenda uma sequência parcialmente superada.

O roadmap atual também não fecha formalmente:

```text
PlayerRecipe MVP
PlayerComposer MVP
CameraRecipe MVP
CameraComposer MVP
QA de câmera
FIRSTGAME CameraComposer proof
remoção da arquitetura legacy de câmera
```

## Escopo

### Atualizar

```text
Documentation~/Current/00-Current-State.md
Documentation~/Current/01-Roadmap.md
Documentation~/Current/02-Usage-Map.md
Documentation~/Current/04-Player-Passive-Binding-Foundation.md
Documentation~/Current/10-Player-Identity-Typed-Binding-Audit.md
Documentation~/README.md
Documentation~/Guides/Camera-Product-Usage.md
Documentation~/Guides/Usage/index.html
Documentation~/ADRs/ADR-INDEX.md
Documentation~/History/000-INDEX.md
```

Os nomes exatos devem ser confirmados no repositório antes da alteração.

### Criar, caso ainda não exista

```text
Documentation~/Current/11-Player-Product-Current-State.md
Documentation~/Current/12-Camera-Product-Current-State.md
Documentation~/History/<novo-id>-Player-Camera-Product-Consolidation.md
```

Não criar documentos redundantes se o conteúdo couber de forma clara nos documentos atuais.

## Decisões que devem ficar congeladas

### Player

```text
PlayerComposer é a superfície principal de authoring do Player.

PlayerRecipe guarda intenção reutilizável.

PlayerSlot, ActorId, PlayerEntry, PlayerView e PlayerControl
continuam sendo contratos/evidências técnicas.

PlayerComposer materializa ou referencia a composição concreta.

A fundação passiva não executa runtime por conta própria.
```

### Câmera

```text
CameraComposer é a superfície principal para câmera de gameplay.

CameraRecipe guarda defaults reutilizáveis.

PlayerComposer fornece targets explícitos.

PlayerViewBehaviour não é autoridade de câmera.

Route/Activity bindings são integração técnica de lifecycle,
não a superfície principal de criação da câmera.

FrameworkCameraDirector e componentes legacy permanecem removidos.
```

### Limite de câmera atual

```text
CameraComposer single-player MVP: fechado.

Route/Activity output apply-on-enter: disponível.

Release/restauração automática no lifecycle exit: pendente.

FIRSTGAME não deve carregar bindings Route/Activity
quando não há troca real de output.
```

## Fora de escopo

```text
Novo runtime.
Novo adapter.
Mudança de comportamento.
Refatoração de cenas do FIRSTGAME.
Criação de novo smoke.
```

## Superfície de produto afetada

```text
Documentação canônica
Roadmap
Usage map
Guia HTML
Índice de ADRs e histórico
```

## Fluxo esperado

```text
Usuário abre a documentação.
Entende o que já existe.
Identifica CameraComposer e PlayerComposer como superfícies principais.
Entende o que ainda é passivo.
Vê apenas uma lane ativa.
Não encontra instruções legacy contraditórias.
```

## Critérios de aceite técnico

```text
[ ] Todos os links internos resolvem.
[ ] Não há referência funcional ao FrameworkCameraDirector.
[ ] Não há recomendação de Camera.main.
[ ] Não há roadmap marcando F49M como estado atual.
[ ] O estado de CameraComposer está registrado como fechado.
[ ] O estado de PlayerComposer está registrado como fechado no nível atual.
[ ] A limitação de camera output release está explícita.
[ ] Não existem duas lanes ativas.
```

## Critérios de aceite de produto

```text
[ ] Um designer identifica como criar Player e câmera.
[ ] Um técnico identifica os contracts passivos subjacentes.
[ ] O próximo bloco de implementação está explícito.
[ ] FIRSTGAME e QA aparecem com papéis corretos.
[ ] A documentação não exige leitura de ADRs históricos para uso básico.
```

## Ganho arquitetural

```text
Remove ambiguidade entre arquitetura histórica e produto atual.
```

## Ganho de usabilidade

```text
O usuário encontra um caminho único de criação e configuração.
```

## Commit sugerido

```text
Docs: consolidate Player and Camera product state and select next active lane
```

---

# 5. Bloco P2 — Player Control Product

Este será o próximo bloco ativo após R0.

## Objetivo

Transformar a composição passiva de Player em um fluxo real de controle, sem acoplar o framework a um controlador de personagem específico.

## Resultado desejado

```text
PlayerRecipe
  intenção reutilizável

PlayerComposer
  composição concreta e referências

PlayerControl authoring
  seleção explícita de fonte e adapter

PlayerControlRuntimeContext
  autoridade runtime escopada

UnityPlayerInput adapter
  integração opcional com Input System

Gameplay movement component
  consumidor do controle, não autoridade do framework

Gate/Pause/Transition
  bloqueiam controle pelo contrato existente
```

---

## P2A — Auditoria e fechamento de arquitetura

### Tipo

```text
Técnico / arquitetura / documentação
```

### Objetivo

Auditar o estado atual de:

```text
PlayerRecipe
PlayerComposer
PlayerControlBehaviour
PlayerControlTopologyValidator
PlayerBindingReadinessSummarizer
PlayerBindingDiagnosticReporter
UnityPlayerInputGateAdapter
PlayerInput usado no FIRSTGAME
scripts de movimento do FIRSTGAME
```

### Decisões obrigatórias

```text
Quem possui a autoridade de controle.
Qual é o lifetime.
Como o PlayerComposer referencia a fonte de controle.
Como o adapter recebe PlayerInput.
Como Gate/Pause/Transition bloqueiam a execução.
Como falha required é reportada.
Como optional é ignorado explicitamente.
```

### Fora de escopo

```text
Movimento genérico completo.
Sistema de habilidades.
Multiplayer.
Auto join.
Spawn de Player.
Rebinding de controles.
```

### Entrega

```text
Documento de auditoria.
ADR ou atualização de ADR existente.
Plano fechado dos cortes P2B-P2G.
```

### Commit sugerido

```text
P2A: audit Player control authority and runtime binding architecture
```

---

## P2B — PlayerControl Recipe e superfície de authoring

### Tipo

```text
UX/produto + editor
```

### Objetivo

Adicionar intenção de controle reutilizável e uma superfície clara no PlayerComposer.

### Shape esperado

```text
PlayerControlRecipe
  control mode
  requiredness
  input integration policy
  action map name ou asset explícito
  gate participation policy
  diagnostics policy

PlayerComposer
  Control section
  PlayerControlRecipe opcional
  PlayerInput explícito
  control target explícito
  Validate
  Apply/Rebuild
  Advanced/Debug
```

### Regras

```text
Recipe não guarda referência concreta de cena.

PlayerInput deve ser explícito.

Não buscar PlayerInput em filhos silenciosamente,
a menos que isso seja uma política declarada e diagnosticável.

Apply/Rebuild deve ser idempotente.

Não executar gameplay no Editor.

Composer não deve virar runtime manager.
```

### Arquivos prováveis

```text
Runtime/Player/Authoring/PlayerControlRecipe.cs
Runtime/Player/Authoring/PlayerControlAuthoringConfig.cs
Editor/Player/PlayerComposerEditor.cs
Editor/Player/PlayerControlRecipeEditor.cs
```

Confirmar estrutura existente antes de criar novos diretórios ou tipos.

### Smoke esperado

```text
Recipe defaults são aplicados.
Referências concretas não são sobrescritas.
Validate bloqueia required ausente.
Apply/Rebuild não duplica componentes.
Segunda execução retorna alreadyValid.
```

### Commit sugerido

```text
P2B: add Player control recipe and designer-first authoring surface
```

---

## P2C — PlayerControl Binding Adapter

### Tipo

```text
Técnico / runtime
```

### Objetivo

Criar o primeiro binding real de controle do Player.

### Contratos sugeridos

```text
IPlayerControlSource
IPlayerControlTarget
IPlayerControlBinding
PlayerControlBindingRequest
PlayerControlBindingResult
PlayerControlBindingFailure
PlayerControlBindingDiagnostic
```

Os nomes devem respeitar tipos existentes e evitar duplicação.

### Comportamento

```text
Bind explícito.
Unbind explícito.
Identidade validada.
Lifetime escopado.
Sem lookup global.
Sem fallback.
Falha required bloqueia.
Optional inválido retorna skipped.
```

### Fora de escopo

```text
Interpretação de movimento.
Input remapping.
Multiplayer.
Possession de NPC.
```

### Critérios técnicos

```text
[ ] Bind idempotente.
[ ] Unbind idempotente.
[ ] Double bind incompatível é rejeitado.
[ ] Foreign/stale binding é rejeitado.
[ ] Diagnóstico inclui PlayerSlot/ActorId quando disponível.
[ ] Nenhuma dependência de Editor.
```

### Commit sugerido

```text
P2C: implement scoped Player control binding contracts and runtime adapter
```

---

## P2D — Unity PlayerInput Bridge

### Tipo

```text
Integração Unity / runtime
```

### Objetivo

Integrar o contrato de controle com `UnityEngine.InputSystem.PlayerInput`.

### Shape esperado

```text
UnityPlayerInputControlSource
ou
UnityPlayerInputBindingAdapter
```

Responsabilidades:

```text
Referenciar PlayerInput explicitamente.
Resolver action map configurado.
Ativar/desativar conforme binding.
Cooperar com UnityPlayerInputGateAdapter.
Reportar action map ausente.
Reportar PlayerInput inválido.
Não alterar identidade funcional por playerIndex.
```

### Regras

```text
PlayerInput.playerIndex não é ActorId.
Não criar PlayerInputManager global.
Não fazer FindObjectOfType.
Não escolher action map silenciosamente.
```

### Casos negativos

```text
PlayerInput ausente.
Actions asset ausente.
Action map inexistente.
Action map duplicado ou incompatível.
Binding de outro Player.
Gate blocker ativo.
```

### Commit sugerido

```text
P2D: add explicit Unity PlayerInput bridge for Player control binding
```

---

## P2E — Player Control Runtime Context

### Tipo

```text
Runtime real
```

### Objetivo

Criar a autoridade escopada que opera o estado de controle.

### Lifetime inicial recomendado

```text
Route-scoped ou Player-instance-scoped,
com vínculo explícito ao runtime route context.
```

A decisão final deve sair da auditoria P2A.

### Estado mínimo

```text
Unbound
Binding
Bound
Blocked
Releasing
Released
Failed
```

Não criar FSM complexa se um estado menor for suficiente.

### Integração

```text
Route entry
Activity readiness
Pause
Transition
Gate blockers
Route exit
Player teardown
```

### Diagnóstico

```text
player id
slot id
actor id
source
target
binding state
gate state
reason
lifetime
```

### Commit sugerido

```text
P2E: add scoped Player control runtime context and lifecycle integration
```

---

## P2F — QA técnico

### Tipo

```text
QAFramework
```

### Objetivo

Provar o contrato sem depender do FIRSTGAME.

### Smokes

```text
Happy path bind/unbind.
Idempotent rebind.
Required source missing.
Optional source missing.
Invalid action map.
Foreign player binding.
Stale binding.
Pause blocker.
Transition blocker.
Route exit release.
Activity switch preservation ou release conforme política.
```

### Aceite

```text
[ ] Todos os casos positivos passam.
[ ] Casos negativos falham explicitamente.
[ ] Não há fallback.
[ ] Logs são estruturados e diagnosticáveis.
[ ] O smoke não depende de assets do FIRSTGAME.
```

### Commit sugerido

```text
P2F: add QA coverage for Player control binding and Unity PlayerInput bridge
```

---

## P2G — FIRSTGAME controle e movimento mínimo

### Tipo

```text
Integração real / produto
```

### Objetivo

Provar que o framework permite controlar um Player real em um jogo.

### Fluxo esperado

```text
Boot
-> Route
-> Activity
-> PlayerComposer disponível
-> Player control binding executado
-> PlayerInput vinculado
-> movimento mínimo responde
-> CameraComposer acompanha
-> Pause bloqueia
-> Transition bloqueia
-> retorno restaura controle
```

### Regra

O framework não deve absorver o script de movimento singular do FIRSTGAME.

O package possui:

```text
autoridade
binding
lifetime
gate
diagnóstico
```

O FIRSTGAME possui:

```text
implementação concreta de movimento
tuning
velocidade
rotação
regras específicas do jogo
```

### Critérios de produto

```text
[ ] Designer configura pelo PlayerComposer.
[ ] Não há montagem manual de contracts internos.
[ ] Controle funciona em Play Mode.
[ ] Pause bloqueia corretamente.
[ ] CameraComposer continua funcional.
[ ] Diagnóstico permite identificar falha de binding.
```

### Commit sugerido

```text
FIRSTGAME: prove Player control binding with minimal playable movement
```

---

# 6. Bloco G1 — Loop jogável mínimo

## Tipo

```text
Integração real
```

## Objetivo

Validar o eixo principal já existente como uma experiência mínima coerente.

## Fluxo

```text
Bootstrap
-> Startup Route
-> Startup Activity
-> Player disponível
-> controle ativo
-> câmera ativa
-> objetivo simples
-> Pause
-> Transition
-> Activity Restart
-> Reset
-> retorno ao estado inicial
```

## Escopo

```text
Um objetivo simples.
Uma interação ou trigger.
Um estado resetável.
Um restart de Activity.
Uma troca ou reentrada controlada.
```

## Fora de escopo

```text
Combate completo.
Inventário.
Save.
UI final.
Multiplayer.
Sistema de missão completo.
```

## Ganho

Esse corte prova que o framework já é utilizável para iniciar um jogo real, e não apenas que os subsistemas passam em isolamento.

## Commit sugerido

```text
FIRSTGAME: add minimal playable loop across Player, Camera, Pause and Restart
```

---

# 7. Bloco P3 — Player Spawn e materialização runtime

## Tipo

```text
Produto + runtime
```

## Objetivo

Definir como o Player é criado ou vinculado durante o lifecycle.

## Modelo desejado

```text
PlayerRecipe
  prefab e intenção reutilizável

PlayerComposer
  configuração authoring

PlayerSpawnPoint / PlayerEntryPoint
  referência concreta

PlayerMaterializationPolicy
  ExistingSceneInstance
  InstantiatePrefab

PlayerRuntimeSession
  autoridade escopada

PlayerMaterializationResult
  Created
  Reused
  Blocked
  Released
```

## Questões obrigatórias

```text
Quem cria o Player?
Quando?
Em qual cena?
Qual é o parent?
Qual é o lifetime?
Persiste entre Activities?
É destruído em Route exit?
Como resolve spawn point?
Como evita duplicidade?
Como registra identidade?
```

## Fora de escopo inicial

```text
Multiplayer.
Respawn completo.
Checkpoint.
Seleção de personagem.
Network spawn.
Pooling de Player.
```

## QA

```text
Existing instance.
Instantiate prefab.
Duplicate player.
Missing spawn point.
Missing prefab.
Route exit release.
Activity switch retention.
Foreign identity.
```

## FIRSTGAME

Substituir setup manual apenas depois que o contrato estiver estável no package e QA.

## Commit sugerido

```text
P3: add scoped Player runtime materialization and spawn authoring
```

---

# 8. Bloco C9 — Camera Output Lifetime e release

## Tipo

```text
Runtime técnico
```

## Objetivo

Completar o lifecycle de outputs Route/Activity.

## Estado atual

```text
Apply on enter: disponível.
Automatic release on exit: ausente.
Restore previous effective output: ausente.
```

## Modelo proposto

```text
CameraOutputHandle
CameraOutputApplication
CameraOutputLifetime
CameraOutputReleaseResult
EffectiveCameraOutputResolver
```

## Comportamento esperado

### Route

```text
Route enter
-> aplica Route output

Activity UseOwn
-> aplica Activity override

Activity exit
-> libera Activity output
-> restaura Route output

Route exit
-> libera Route output
-> nenhuma câmera legacy/fallback é inventada
```

### Políticas

```text
UseOwn
UseRoute
```

Adicionar novas políticas apenas com necessidade real.

## Regras

```text
Não usar SetActive.
Não usar Camera.enabled.
Não usar busca por prioridade global não rastreada.
Não recriar FrameworkCameraDirector.
Não criar singleton.
```

## QA

```text
Route apply.
Activity override.
Activity release.
Route restore.
Route exit release.
Invalid required output.
Optional output skipped.
Stale handle.
Double release.
```

## FIRSTGAME

Executar apenas quando existir uma Route ou Activity que realmente use câmera diferente.

## Commit sugerido

```text
C9: implement scoped Camera output lifetime, release and restoration
```

---

# 9. Bloco S1 — Progression Save Runtime

## Tipo

```text
Runtime / adapter
```

## Pré-condição

O FIRSTGAME deve possuir estado real que valha a pena persistir.

## Objetivo

Implementar um fluxo de save de progressão com backend inicial simples e engine substituível.

## Modelo

```text
ProgressionSaveAdapter
SaveSlotId
SaveSnapshotId
SaveManifest
SaveRequest
LoadRequest
DeleteRequest
ISaveBackend
JsonSaveBackend inicial
```

## Regra arquitetural

```text
Framework possui momentos, contratos e adapters.
Backend executa serialização e armazenamento.
Backend pode ser substituído sem alterar o gameplay.
```

## Fora de escopo inicial

```text
Cloud save.
Criptografia avançada.
Version migration completa.
Save premium específico.
```

## Commit sugerido

```text
S1: implement progression save runtime with interchangeable JSON backend
```

---

# 10. Hardening posterior

Os itens abaixo permanecem candidatos, não lanes ativas:

```text
Transition progress real
Loading progress e failure presentation
Pause UX avançada
Camera modes adicionais
Player multiplayer
Input rebinding
Actor spawning genérico
NPC authoring product
Gameplay interaction product
Save migrations
Templates e samples ampliados
```

Eles só devem ser selecionados após fechamento explícito do bloco ativo.

---

# 11. Sequência consolidada

```text
R0
  Sincronização documental e roadmap

P2A
  Auditoria de controle

P2B
  PlayerControl Recipe / authoring

P2C
  Binding adapter

P2D
  Unity PlayerInput bridge

P2E
  Runtime context escopado

P2F
  QA técnico

P2G
  FIRSTGAME controle real

G1
  Loop jogável mínimo

P3
  Player spawn/materialização

C9
  Camera output lifetime/release

S1
  Progression save runtime
```

---

# 12. Próxima lane ativa

Após este plano ser aceito, a única lane ativa deve ser:

```text
R0 — Documentation and Roadmap Reconciliation
```

Após R0 ser fechado:

```text
P2 — Player Control Product
```

Nenhum trabalho de spawn, save ou camera output release deve ocorrer em paralelo, salvo correção crítica diretamente relacionada.

---

# 13. Critério de conclusão do ciclo

O ciclo Player + Camera + Control estará consolidado quando:

```text
[ ] Docs refletem o estado real.
[ ] PlayerComposer é a superfície principal.
[ ] CameraComposer é a superfície principal.
[ ] Controle possui binding runtime real.
[ ] Unity PlayerInput é integrado explicitamente.
[ ] Pause e Transition bloqueiam controle.
[ ] QA cobre casos positivos e negativos.
[ ] FIRSTGAME possui movimento mínimo funcional.
[ ] CameraComposer acompanha o Player.
[ ] Activity Restart e Reset continuam funcionando.
[ ] Não há fallback silencioso.
[ ] Não há autoridade global implícita.
[ ] Logs e debug mostram evidências suficientes.
```
