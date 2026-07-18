# Auditoria de Arquitetura — Redundância e Legado para Remoção

**Produto:** Immersive Framework (`com.immersive.framework`)  
**Alvo:** Unity 6 (`6000.0`) — framework de ciclo de vida para jogos de médio porte  
**Versão auditada:** `1.0.0-preview.15` (branch `master`)  
**Data:** 2026-07-17  
**Escopo:** Runtime (~969 `.cs`, ~132k LOC) + Editor + superfícies de authoring/menus  
**Foco principal:** **redundância**, **trilhos paralelos**, **legado** e **candidatos a remoção/consolidação**

**Documentos relacionados (não substituídos):**

| Documento | Foco |
|-----------|------|
| `ARCHITECTURE-WEAKNESS-AUDIT.md` | Fragilidades (discovery, identidade, Find*) |
| `ARCHITECTURE-FRAGILITY-NEXT-STEPS.md` | Backlog pós-mitigação C1/C3/C4/A2 |
| `INPUT-CAMERA-DUAL-RAILS-AUDIT.md` | Trilhos duplos Input/Camera |
| `ROUTE-ACTIVITY-EXIT-ORDER-REENTRANCY-AUDIT.md` | Ordem/reentrada Route/Activity |
| `Documentation~/Current/00-Current-State.md` / `01-Roadmap.md` | Estado P3M3 e remoções planejadas |

---

## 1. Resumo executivo

O framework **não é um monólito acidental**: a maior parte das pastas espelha fases (F*) e ADRs com vocabulário explícito, IDs tipados e superfícies de resultado diagnosticáveis. Isso é uma força — e também a origem da principal dívida:

> **Custo de superfície >> custo de capacidade.**  
> Muitos domínios têm 2–4 stacks paralelos (conceitual / Unity / trigger de cena / smoke), Result/Status/Issue por estágio, e pares Route/Activity quase gêmeos.

| Categoria | Estimativa | Tema dominante |
|-----------|----------:|----------------|
| **Remoção imediata / cut ativo** | 4–6 superfícies | P3M3 residual, stub Removed, pastas vazias, menu legado |
| **Consolidação de trilho (produto)** | 5–7 trilhos | Pause input dual, LocalPlayer camera dual, Reset vs CycleReset |
| **Redundância estrutural (médio prazo)** | ~28 pares Route/Activity + camadas Content | Duplicação intencional sem genérico compartilhado |
| **Explosão de tipos (higiene)** | ~120 Result + ~101 Status + ~41 Issue* | ~27% dos arquivos Runtime são Result/Status/Issue |
| **QA no assembly de produto** | 36 smokes (~9% LOC) | `DevelopmentTooling` dentro de `Immersive.Framework.Runtime` |

**Conclusão:** o próximo salto de manutenibilidade **não** é “mais abstração”, e sim **fechar trilhos legados**, **eleger um canônico por domínio**, e **reduzir a superfície morta/duplicada** sem quebrar o contrato de lifecycle. O cut **P3M3** (remoção do Player Composer alternativo) já aponta na direção certa; esta auditoria amplia o inventário para o framework inteiro.

---

## 2. Método

Análise estática sobre `Runtime/`, `Editor/` e documentação Current/History:

1. Inventário de módulos, LOC e densidade de tipos Result/Status/Issue/Id  
2. Pares Route/Activity e similaridade lexical (proxy de cópia)  
3. Superfícies `FrameworkApiStatus.Removed` / `Deferred` / `DevelopmentTooling`  
4. Trilhos paralelos que mutam o mesmo efeito físico (input map, camera winner, reset)  
5. Stacks de content (ContentFlow / RuntimeContent / ContentAnchor / LocalContribution)  
6. Pastas vazias, stubs de migração e menus `Removed/`  
7. Cruzamento com roadmap P3M3 e auditorias já existentes  

**Não é** auditoria de performance, cobertura de testes unitários, ou quality de gameplay.

### Classificação de ação

| Tag | Significado |
|-----|-------------|
| **DELETE** | Remover do package (código/menu/pasta); sem shim se o roadmap proíbe |
| **DEPRECATE→DELETE** | Marcar Removed, migrar consumers, apagar em cut seguinte |
| **CONSOLIDATE** | Manter um canônico; fundir ou rebaixar o segundo trilho |
| **EXTRACT** | Mover para assembly/pasta de QA ou genérico compartilhado |
| **KEEP** | Duplicação consciente de domínio (não unificar prematuramente) |

---

## 3. Mapa de módulos (Runtime)

| Módulo | `.cs` | Papel (1 linha) | Sinal de redundância |
|--------|------:|-----------------|----------------------|
| **PlayerParticipation** | 189 | Lane canônica Player (join, admission, gameplay, camera eligibility) | Contratos (~118) com ~45 Result/Status/Issue; maior superfície do package |
| **ActivityFlow** | 84 | Lifecycle de Activity + scene composition + content execution | Espelho forte com RouteLifecycle |
| **InputMode** | 57 | Posture de input + apply Unity PlayerInput | Pipeline Preview→Plan→Apply **+** Pause* paralelo (~33 Result/Status/Issue) |
| **Camera** | 54 | Output session, requests, publishers, bindings | Dual publish LocalPlayer; Cinemachine/Unity vazios |
| **ContentAnchor** | 51 | Anchors de cena + materialização Unity | Stack materialize/release paralelo a RuntimeContent |
| **Reset** | 45 | Registry + executor de subjects/participants | **3 superfícies de produto** ao redor (ObjectReset, ActivityRestart, CycleReset) |
| **RuntimeContent** | 44 | Handles runtime, materialização/release genéricos | Overlap com ContentFlow + ContentAnchor |
| **Pause** | 40 | Pause lógico + surface visual + input triggers | Trilho minimal **e** bridge canônico + stub Removed |
| **Diagnostics** | 33 | Logger + QA canvas + **27 smokes** | QA embutido no Runtime |
| **Loading** | 31 | Loading steps/readiness | Baixa redundância aparente |
| **RouteLifecycle** | 30 | Lifecycle de Route + composition | Espelho Activity (mais enxuto) |
| **CycleReset** | 27 | Plan/execute de cycle participants | Stack paralelo a Reset (não reusa `IResetParticipant`) |
| **ProgressionSave** | 27 | Save/load progression | Baixa redundância com Snapshot (domínios distintos) |
| **GameFlow** | 24 | Orquestração + triggers Route/Activity | Triggers quase gêmeos (Jaccard ~90%) |
| **Actors** | 23 | Actor genérico + PlayerActor* | Espelho Validator/Set/Issue Player vs genérico |
| **ObjectEntry** | 18 | Declaração/coleção de object entries | Vizinho conceitual de LocalContribution / Reset subject |
| **Transition** | 16 | Orquestração de transição | Separado de TransitionEffects (ok) |
| **Snapshot** | 16 | Snapshot participants/envelope | Não confundir com ProgressionSave |
| **ContentFlow** | 16 | Content identity + release de cenas | Camada “F1” ainda usada por Route/Activity composition |
| **LocalContribution** | 15 | Discovery/validação de contribuições locais | Quarto vocabulário de “content” local |
| **Authoring** | 15 | Assets de produto (Game/Route/Activity/content) | Profiles Route/Activity espelhados |
| **TransitionEffects** | 14 | Fade/curtain adapters | Ok como borda de Transition |
| **UnityInput** | 12 | Declarações/targets Unity Input + Gate adapter | Gate adapter **e** InputMode mutam PlayerInput |
| **ObjectReset** | 6 | Triggers de Object Reset | Thin wrapper sobre ResetExecutor |
| **ActivityRestart** | 5 | Trigger de restart via Reset + flow | Thin wrapper; não é CycleReset |
| **SessionLifecycle** | 4 | Session content set/state | Mínimo; pouco peso |
| **SceneLifecycle** | 4 | Load/unload de cenas | Núcleo único (bom) |
| **CameraAuthoring** | 4 | Rig composer/recipe | **Canônico** (não confundir com Player Composer removido) |
| **Audio** | 4 + asmdef | BGM por route/activity | Assembly separado; superfície pequena |
| **PlayerAuthoring** | **0** | Pasta residual pós-P3M2/M3 | **DELETE pasta/meta** |
| **Camera/Cinemachine**, **Camera/Unity** | **0** | Placeholders vazios | **DELETE pastas/meta** |
| **PlayerSlots** | 1 | `PlayerSlotId` | Poderia viver em PlayerParticipation (higiene) |
| **GlobalUi** | 1 | Scene runtime UI global | Ok se for o único owner |

---

## 4. Achados prioritários — remoção e consolidação

### R1 — Cut P3M3 ainda incompleto (Player Composer alternativo)

**Tag:** DELETE (já no roadmap; validação Unity pendente)  
**Severidade:** Alta (bloqueia P3M4)

**Estado observado no source**

| Superfície | Estado no tree |
|------------|----------------|
| `Runtime/PlayerAuthoring/` | **Vazia** (0 `.cs`) — residual de pasta |
| `Editor/PlayerAuthoring/` | **Vazia** |
| `CameraRigComposer` | **Mantido** (canônico de camera, não é o Player Composer) |
| Docs Current | “source prepared; Unity validation pending” |

**Ação**

1. Fechar validação Unity do cut P3M3 (import, C9M/C9R, P3 aggregate, sem menu P3B, sem Missing Script).  
2. Remover pastas/meta vazias `PlayerAuthoring` (Runtime + Editor) se ainda existem no package publicado.  
3. Grep de consumidores serializados / QA scenes por tipos apagados.  
4. **Não** reintroduzir shim, alias ou null bridge (regra do Current State).

---

### R2 — Stub `UnityPauseInputActionAdapter` (Removed, ainda no menu)

**Tag:** DEPRECATE→DELETE  
**Severidade:** Média (confunde authoring; já é inerte)

**Onde**

- `Runtime/Pause/UnityPauseInputActionAdapter.cs`  
- `FrameworkApiStatus.Removed`  
- Menu: `Immersive Framework/Removed/Legacy Unity Pause Input Action Adapter`

**Por que remover**

- Única superfície `Removed` no Runtime; já não executa Pause.  
- Mantém serialização/campos e ruído no Add Component.  
- Canônico documentado: `PauseInputActionRuntimeBridgeTrigger` → bridge → InputMode apply.

**Ação**

| Fase | Ação |
|------|------|
| 1 | Remover do menu **ou** ocultar com `[AddComponentMenu("")]` + warning OnValidate |
| 2 | Script de migração Editor: substituir por bridge trigger se ainda presente em cenas |
| 3 | Apagar classe após janela de migração (ex.: próximo preview) |

---

### R3 — Trilho dual de Pause input (minimal vs canônico)

**Tag:** CONSOLIDATE  
**Severidade:** Crítica (produto)

| Trilho | Tipo | Comportamento |
|--------|------|----------------|
| **A — Minimal** | `PauseInputActionTrigger` | Escuta action → `PauseRequest` direto; **opcionalmente** troca action map |
| **B — Canônico** | `PauseInputActionRuntimeBridgeTrigger` + `PauseInputMode*` | Pause + InputMode + PlayerInput sincronizados |
| **C — Morto** | `UnityPauseInputActionAdapter` | Stub (R2) |

**Problema**

- Dois caminhos de produto ativos escrevem o mesmo efeito (pause + maps).  
- O minimal **pode** mutar action map sem passar por InputMode → desync com o pipeline Preview/Plan/Apply.  
- Documentação de Player aponta o path B; FIRSTGAME-2D ainda anuncia o path A como Experimental.

**Recomendação**

1. **Produto:** path B único.  
2. Path A:  
   - remover `switchPlayerInputActionMap` **ou**  
   - rebaixar a “QA / minimal keyboard only” com defaults que **nunca** tocam PlayerInput.  
3. Validator Editor: erro se A e B coexistem na mesma cena com switching ligado.  
4. Após migração: DEPRECATE→DELETE do switching em A (ou do componente inteiro se FIRSTGAME não depender).

---

### R4 — Dual publish LocalPlayer camera

**Tag:** CONSOLIDATE  
**Severidade:** Crítica (já detalhado em `INPUT-CAMERA-DUAL-RAILS-AUDIT` como C-C1)

| Trilho | Superfície |
|--------|------------|
| **Scene binding** | `LocalPlayerCameraRequestBinding` (`eligibleOnEnable` default true) |
| **Gameplay admission** | `PlayerGameplayAdmissionRuntimeContext.TryPublishCamera` |

Ambos → `LocalPlayerCameraRequestPublisher` → `CameraOutputSession.Admit` com o mesmo `OwnerKind` / lifetime.

**Recomendação (alinhar com P3)**

- **Canônico P3:** admission publica; binding de cena vira evidence/authoring **ou** opt-in com `eligibleOnEnable = false` default.  
- Validator: erro se ambos ativos no mesmo slot/host.  
- Guide de Camera: incluir o path de admission no diagrama.

---

### R5 — Três famílias de Reset + wrappers

**Tag:** CONSOLIDATE (documentar canônico; eventualmente unificar contratos)  
**Severidade:** Alta (superfície de produto confusa)

```text
                    ┌─────────────────────┐
                    │   Reset (núcleo)    │
                    │ Registry+Executor   │
                    │ IResetParticipant   │
                    └──────────┬──────────┘
           ┌───────────────────┼───────────────────┐
           ▼                   ▼                   ▼
   ObjectResetTrigger   ActivityRestartTrigger   (seleção via
   (1 subject)          (ResetSelectionConfig    ResetSelectionConfig)
                         + flow restart)
                    
   ── stack paralelo ──────────────────────────────
                    ┌─────────────────────┐
                    │ CycleResetRuntime   │
                    │ ICycleResetParticipant │
                    │ Route/Activity triggers │
                    └─────────────────────┘
```

| Superfície | Relação com Reset |
|------------|-------------------|
| **Reset** | Núcleo registry/executor (manter) |
| **ObjectReset** | Thin MonoBehaviour sobre ResetExecutor — **KEEP** como authoring boundary |
| **ActivityRestart** | Compose Reset + flow de Activity — **KEEP**, documentar “não é CycleReset” |
| **CycleReset** | **Stack completo paralelo** (Request/Plan/Result/Issue/Participant Id) **sem** reutilizar `IResetParticipant` |

**Problema**

- Dois contratos de participante (`IResetParticipant` vs `ICycleResetParticipant`) com o mesmo shape mental (id, requiredness, execute, result).  
- Authoring e QA precisam escolher entre 4 menus/conceitos.  
- ~45 + 27 + 6 + 5 arquivos só no “ecossistema reset”.

**Recomendação**

| Prazo | Ação |
|-------|------|
| Curto | Tabela canônica no guide `Reset-Architecture-Flow.md`: quando usar cada um |
| Médio | Adapter `ICycleResetParticipant` → `IResetParticipant` **ou** CycleReset como *policy/orquestração* sobre ResetExecutor |
| Longo | Evitar novo quarto stack (ex.: “SceneReset”) |

**Não é DELETE cego de CycleReset** — RouteLifecycle o instancia e executa. É consolidação de **contrato**, não remoção de feature.

---

### R6 — InputMode: explosão de estágios + Pause* espelhado

**Tag:** CONSOLIDATE / EXTRACT  
**Severidade:** Média–Alta (manutenibilidade)

**Fatos**

- 57 arquivos em `InputMode/`  
- ~33 são Result/Status/Issue  
- Estágios: Request → ApplicationPreview → ActionMapPreview → Plan → Adapter → Application → RequestApplication  
- Camada **Pause*** repete Result/Status para bridge/apply/application

**O que é bom**

- Pipeline diagnosticável, posture ≠ map name.  
- Separação Pause lógico vs apply de input.

**O que é ruído**

- Quase cada estágio tem **Issue + IssueKind + Result + Status** como tipos de arquivo separados.  
- Pause wrappeia o mesmo pipeline com mais 3 triplas Result/Status.

**Recomendação**

1. Genéricos internos: `StageResult<TStatus, TIssue>` / nested types — **sem** mudar API pública de uma vez.  
2. Colapsar Preview de Application e Preview de ActionMap se um for sempre subconjunto do outro (medir callers).  
3. `PauseInputMode*` deve ser **orquestrador fino**, não segunda cópia do vocabulário de status.  
4. Host: um `InputModeState` canônico (já citado na dual-rails audit) — evita mutadores concorrentes (`UnityPlayerInputGateAdapter` + InputMode + Pause minimal).

---

### R7 — Stacks de Content (4 vocabulários)

**Tag:** CONSOLIDATE (mapa + fronteiras)  
**Severidade:** Alta (cognição / APIs sobrepostas)

| Stack | Identidade / handle | Papel real no código |
|-------|---------------------|----------------------|
| **ContentFlow** | `FrameworkContentId/Identity/Handle/Set` + `ContentReleaseRuntime` | Planejamento/release de cenas de Route/Activity |
| **RuntimeContent** | `RuntimeContentId/Handle` + materialização/release | Instâncias criadas em runtime (prefab etc.) |
| **ContentAnchor** | `ContentAnchorId` + bridges Unity de materialização | Anchors de cena + pipeline Unity dedicado |
| **LocalContribution** | `LocalContentId` + discovery/validator | Contribuições locais declarativas |

**Sintoma**

- 3+ tipos `*ContentId`, 3+ `*Handle`, **33 arquivos** com “Release”, **47** com “Materialization”.  
- ContentAnchor tem **seu próprio** pipeline de materialize/release Unity **além** de RuntimeContent.

**Recomendação**

1. **Documento de fronteira** (1 página): qual stack um game dev toca em cada caso.  
2. Proibir novo `*ContentId` sem ADR.  
3. Avaliar se `ContentAnchor` materialization deve **delegar** a `RuntimeContent` (um registry de owns/release).  
4. `LocalContribution`: confirmar se ainda tem consumer de produto pós-P3; se só discovery residual → DEPRECATE ou fundir em ObjectEntry/PlayerParticipation.  
5. ContentFlow: **KEEP** enquanto Route/Activity composition usarem `FrameworkContentSet` / `ContentReleaseRuntime` — não é morto.

---

### R8 — Pares Route / Activity (duplicação estrutural)

**Tag:** CONSOLIDATE seletivo / KEEP consciente  
**Severidade:** Média (custo de evolução simétrica)

**~28 pares** com o mesmo sufixo (`*ContentRuntime`, `*SceneComposition*`, `*CameraOverrideBinding`, `*RequestTrigger`, `*CycleResetTrigger`, assets de authoring, etc.).

| Par (amostra) | Similaridade lexical (proxy) | Leitura |
|---------------|-----------------------------:|---------|
| `*CameraOverrideBinding` | **~97%** | Já herdam `ScopedCameraOverrideBinding` — bom |
| `*RequestTrigger` | **~90%** | Candidato a base genérica `ScopeRequestTrigger<TAsset,TKind>` |
| `*ContentBehaviour` | **~59%** | Parecidos, divergência ok |
| `*SceneCompositionRuntime` | **~32%** | Activity bem maior — não unificar à força |

**Onde unificar paga**

- Triggers GameFlow (`ActivityRequestTrigger` / `RouteRequestTrigger` + UnityEvent bridges).  
- CycleReset triggers Route/Activity (já quase simétricos).  
- Content profile / scene entry authoring (já há padrões espelhados em `Authoring/`).

**Onde NÃO unificar agora**

- `ActivityFlowRuntime` vs `RouteLifecycleRuntime` (semântica e tamanho diferentes).  
- Content execution só de Activity (não existe espelho Route completo).

**Regra proposta**

> Duplicar **boundário de domínio** (ok). Duplicar **boilerplate de Result/Trigger/Issue** (ruim).  
> Extrair só quando Jaccard ≳ 85% **e** os divergentes cabem em 1–2 virtuals.

---

### R9 — Actors genéricos vs PlayerActor*

**Tag:** KEEP com higiene  
**Severidade:** Baixa–Média

- `Actor*` (genérico) + `PlayerActor*` (set/validator/declaration/descriptor/issue).  
- Player é especialização legítima; Validator Player é bem menor (arquivo ~2.3k vs Actor ~7.6k).

**Ação**

- Não fundir.  
- Evitar terceira árvore (ex.: NpcActorSet completo) sem herdar padrões de `ActorSet`/`ActorValidator`.  
- Longo prazo: `PlayerActorValidator` como composição de regras, não cópia.

---

### R10 — QA / Smoke no assembly Runtime (~9% LOC)

**Tag:** EXTRACT  
**Severidade:** Média (peso de package, risco de shipping tooling)

| Métrica | Valor |
|---------|------:|
| Arquivos `*Smoke*` | 36 |
| LOC smoke (aprox.) | ~11.5k |
| % do Runtime | ~8.7% |
| Maioria em | `Diagnostics/` (27) |

**Problema**

- `DevelopmentTooling` vive no mesmo `Immersive.Framework.Runtime.asmdef` do produto.  
- Aumenta compile surface e tenta consumers a chamar smokes.  
- Canvas QA já diz que botões supersedidos foram removidos, mas runners permanecem.

**Recomendação**

1. Assembly opcional `Immersive.Framework.Runtime.Qa` / package sample `QAFramework` only.  
2. Ou `#if IMMERSIVE_FRAMEWORK_QA` em smokes.  
3. Manter no Runtime só o mínimo de probes usados por testes de integração oficiais.  
4. Inventário: smokes de fases antigas (F18*, F19*, F21*, F29*) — candidatos a archive se não há menu/botão.

---

### R11 — Pastas e meta vazias

**Tag:** DELETE  
**Severidade:** Baixa (higiene de package)

| Path | Conteúdo |
|------|----------|
| `Runtime/PlayerAuthoring/` | 0 cs |
| `Editor/PlayerAuthoring/` | 0 cs |
| `Runtime/Camera/Cinemachine/` | 0 cs |
| `Runtime/Camera/Unity/` | 0 cs |

Remover pastas + `.meta` órfãos evita reaparecimento de “módulos fantasmas” e Missing Folder noise.

---

### R12 — Naming legado / drift documental

**Tag:** CONSOLIDATE (docs + aliases mentais)  
**Severidade:** Baixa–Média

| Doc / expectativa | Código real |
|-------------------|-------------|
| C9I `RouteCameraRequestBinding` / `ActivityCameraRequestBinding` | `*CameraOverrideBinding` |
| “Eligible” em 3 camadas | binding scene / eligibility context / admission |
| Discovery “legacy migration” | `LocalPlayerProvisioningAuthoringDiscovery.TryResolveLoadedForLegacyMigration` |

**Ação**

- Atualizar Product docs C9I e guides.  
- Discovery legada: se Host Registration é o path de produto (Current State), **remover** fallback legacy do boot após consumers migrarem (alinha com C2 da weakness audit).

---

### R13 — Gate adapter vs InputMode vs Pause

**Tag:** CONSOLIDATE (ownership)  
**Severidade:** Alta (comportamento)

Mutadores de `PlayerInput` / action maps observados:

1. InputMode application pipeline  
2. `PauseInputActionTrigger` (opcional)  
3. Gameplay input binding (admission)  
4. `UnityPlayerInputGateAdapter` (block mode para Pause/Transition)

**Recomendação**

- Um **único writer** de action map em runtime de produto: InputMode apply (ou um `PlayerInputMutationGateway` interno).  
- Gate adapter: só **disable/enable** ou blocker registration — não “outro SwitchCurrentActionMap” paralelo sem contrato.  
- Detalhe operacional: ver dual-rails audit.

---

### R14 — Deferred Route content planning types

**Tag:** KEEP ou DELETE com ADR  
**Severidade:** Baixa

- `RouteContentScenePlanEntry`, `RouteContentMaterializationPlan` → `FrameworkApiStatus.Deferred` (“F6”).  
- Se F6 já foi absorvido por `RouteSceneComposition*`, estes tipos podem ser **mortos documentais**.  
- Ação: confirmar callers; se zero runtime path → DELETE; se só authoring residual → manter com nota “planning only”.

---

## 5. Explosão de tipos (higiene transversal)

| Família de arquivo | Qtd. no Runtime |
|--------------------|----------------:|
| `*Result.cs` | ~120 |
| `*Status.cs` | ~101 |
| `*Issue*.cs` | ~41 |
| `*Id.cs` | ~34 |
| `*Snapshot.cs` | ~31 |
| `*Handle*.cs` | ~10 |
| `*Participant*.cs` | ~52 |
| `*Materialization*.cs` | ~47 |

**Interpretação**

- Não é “código morto” em massa — é **padrão de design** (resultado explícito em vez de exceção).  
- O problema é a **granularidade de arquivo** e a **repetição por estágio/escopo**, não o padrão em si.

**Direções de redução (sem big-bang)**

1. Nested types / `readonly record struct` no mesmo arquivo do owner.  
2. Genéricos internos compartilhados em `Common/` para Issue severity + aggregate.  
3. Congelar novos `*IssueKind` enums até reutilizar vocabulário existente.  
4. PlayerParticipation Contracts (118 arquivos): maior ROI de nested/partial se o time for tocar P3 de novo.

---

## 6. O que NÃO deve ser removido (falsos positivos)

| Superfície | Por que KEEP |
|------------|--------------|
| **CameraRigComposer / Recipe** | Canônico de camera targets; **não** é o PreAuthored Player Composer do P3M3 |
| **ContentFlow** | Ainda amarra composition/release de Route/Activity |
| **ObjectReset / ActivityRestart** | Boundaries de authoring finos sobre Reset — não stacks fantasmas |
| **Transition vs TransitionEffects** | Separação orquestração × visual correta |
| **Snapshot vs ProgressionSave** | Domínios diferentes (restore técnico × save de progresso) |
| **SessionLifecycle fino** | Poucos tipos; owner de session content é válido |
| **Audio asmdef separado** | Isolation ok; não é legado |
| **Actors genéricos** | Base para não-Player; PlayerActor é especialização |

---

## 7. Matriz de ação recomendada

### 7.1 Remoção / cut (ordem sugerida)

| # | Item | Tag | Esforço | Risco | Depende de |
|---|------|-----|---------|-------|------------|
| 1 | Fechar P3M3 Unity + limpar `PlayerAuthoring` vazio | DELETE | S | Médio (Missing Script em cenas antigas) | Validação Unity |
| 2 | Pastas vazias Camera Cinemachine/Unity | DELETE | S | Baixo | — |
| 3 | `UnityPauseInputActionAdapter` stub | DEPRECATE→DELETE | S | Baixo | Scan de cenas |
| 4 | Fallback legacy provisioning discovery no boot | DELETE path | M | Médio | Host Registration em todos consumers |
| 5 | Smokes órfãos sem menu/botão | EXTRACT/DELETE | M | Baixo | Inventário QA |
| 6 | Tipos Deferred Route se sem caller | DELETE | S | Baixo | Grep callers |

### 7.2 Consolidação de trilho (produto)

| # | Item | Tag | Esforço | Risco |
|---|------|-----|---------|-------|
| 1 | Pause input: um canônico (bridge) | CONSOLIDATE | M | Médio |
| 2 | LocalPlayer camera: um publisher path | CONSOLIDATE | M | Médio |
| 3 | Writer único de action map | CONSOLIDATE | M–L | Alto |
| 4 | CycleReset vs Reset contrato | CONSOLIDATE | L | Médio |
| 5 | Fronteiras Content* (doc + eventual merge Anchor→RuntimeContent) | CONSOLIDATE | L | Médio |

### 7.3 Higiene estrutural

| # | Item | Tag | Esforço |
|---|------|-----|---------|
| 1 | Base genérica RequestTriggers Route/Activity | CONSOLIDATE | M |
| 2 | Nested Result/Status em InputMode e Contracts | EXTRACT | L |
| 3 | QA assembly separado | EXTRACT | M |
| 4 | Docs C9I / guides alinhados aos nomes reais | CONSOLIDATE | S |
| 5 | `PlayerSlotId` morar em PlayerParticipation | EXTRACT | S |

---

## 8. Inventário rápido de legado explícito no código

| Marcador | Ocorrências / exemplos |
|----------|------------------------|
| `FrameworkApiStatus.Removed` | 1 — `UnityPauseInputActionAdapter` |
| `FrameworkApiStatus.Deferred` | 2 — Route content planning F6 |
| Menu `Removed/` | 1 componente |
| Comentários/API “legacy” | Scene name match residual; provisioning discovery; tooltips ContentId (já mitigados em paths de execução) |
| `DevelopmentTooling` | Dezenas de smokes + ActivityRestart QA |
| Pastas vazias pós-remoção | PlayerAuthoring, Camera/Cinemachine, Camera/Unity |

---

## 9. Métricas de baseline (para medir limpeza)

| Métrica | Baseline 2026-07-17 |
|---------|--------------------:|
| Runtime `.cs` | 969 |
| Runtime LOC (aprox.) | ~132k |
| Módulos top-level | 40 pastas |
| Smoke files | 36 |
| Smoke LOC % | ~8.7% |
| Result+Status+Issue files | ~262 (~27% dos .cs) |
| Pares Route/Activity nomeados | ~28 |
| Superfícies Removed | 1 |
| Pastas Runtime sem `.cs` | 3 (+ Editor PlayerAuthoring) |

**Metas sugeridas (2–3 previews)**

| Meta | Alvo |
|------|------|
| Superfícies Removed no menu | 0 |
| Pastas vazias | 0 |
| Smoke no Runtime product asmdef | 0 (ou &lt;2% LOC sob `#if`) |
| Trilhos Pause input de produto | 1 |
| Trilhos publish LocalPlayer | 1 |
| Docs com nomes de tipo desatualizados (C9I) | 0 |

---

## 10. Riscos de remoção agressiva

1. **Serialized MonoBehaviours** — Unity vira Missing Script; exige repair em QA/FIRSTGAME (já previsto no P3M3).  
2. **Ciclo Route/Activity** — unificar composition runtimes cedo demais quebra assimetrias reais (Activity tem execution participants; Route não).  
3. **CycleReset** — não apagar sem migrar `RouteLifecycleRuntime` / triggers.  
4. **ContentFlow** — apagar por “parece F1 antigo” quebra release/composition.  
5. **Smokes** — se a prova de regressão oficial ainda os invoca via reflection/menu interno, EXTRACT deve manter API de teste.

---

## 11. Plano de execução sugerido (levas)

### Leva A — Higiene e legado morto (baixo risco)

1. Remover pastas vazias + meta.  
2. Ocultar/apagar `UnityPauseInputActionAdapter` após scan.  
3. Alinhar docs C9I / guides aos nomes `*OverrideBinding`.  
4. Inventário smokes sem referência de menu → archive.

### Leva B — Um trilho por efeito físico (médio risco)

1. Pause: defaults e validators para path bridge-only.  
2. Camera LocalPlayer: default `eligibleOnEnable = false` + validator dual-path.  
3. Boot: desligar legacy provisioning discovery quando registration existe.  
4. Fechar P3M3 validation gate.

### Leva C — Consolidação estrutural (alto esforço)

1. RequestTrigger genérico Route/Activity.  
2. Contrato unificado Reset/CycleReset (adapter).  
3. Fronteira ContentAnchor → RuntimeContent.  
4. QA assembly + nested results em InputMode/Contracts.

---

## 12. Conclusão

O Immersive Framework está em um ponto clássico de **preview maduro**:

- a **arquitetura de domínio** (lifecycle, IDs, results, ownership) é intencional e majoritariamente coerente;  
- a **dívida** está na **multiplicidade de superfícies** que implementam a mesma intenção (especialmente Pause/Input, Camera LocalPlayer, Reset family, Content stacks) e no **peso de scaffolding** (Result/Status/Issue + smokes no Runtime).

Prioridade objetiva para remoção/consolidação:

1. **Fechar P3M3** e limpar residual vazio.  
2. **Matar trilhos legados de input/pause e dual camera publish.**  
3. **Extrair QA** e **reduzir wrappers mortos** (Removed stub).  
4. **Só então** unificar genéricos Route/Activity e Reset/CycleReset — com fronteiras documentadas, sem “framework genérico demais”.

Este documento deve ser revisitado após o fechamento de P3M3 e da Leva B; as métricas da §9 servem de scorecard.

---

## Apêndice A — Checklist de grep para próxima limpeza

```text
FrameworkApiStatus.Removed
FrameworkApiStatus.Deferred
AddComponentMenu("Immersive Framework/Removed
TryResolveLoadedForLegacyMigration
eligibleOnEnable
switchPlayerInputActionMap
autoDiscover
QaSmokeRunner
PreAuthored|P3B|PlayerComposer
RouteCameraRequestBinding|ActivityCameraRequestBinding
```

## Apêndice B — Pares Route/Activity catalogados (sufixo)

```text
ContentBehaviour, ContentEntry, ContentLifecycleContext/Events/Phase,
ContentRuntime, ContentSet, EnteredEvent, ExitedEvent, RuntimeState,
SceneComposition*, Asset, ContentProfileAsset, ContentSceneEntry,
CameraOverrideBinding, CameraRequestPublisher, ContentAnchor,
CycleResetTrigger(+UnityEventBridge), RequestTrigger(+Event+UnityEventBridge)
```

*(Lista gerada por matching de nome; não implica que todos sejam cópia 1:1.)*
