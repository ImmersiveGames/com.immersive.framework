# P3A — Player Materialization Conformance Audit

**Package:** `com.immersive.framework`  
**Unity:** 6.5  
**Date:** 2026-07-12  
**Type:** Conformance audit only (no implementation, no ZIP, no repository mutation)  
**Scope:** Verify that current Player / Actor / Slot / Composer / Camera binding implementation respects accepted ADRs and contracts for a future runtime materialization path.

**Related operational context:** `Documentation~/Current/05-Execution-Status.md` places **P3 — Player Spawn / Runtime Materialization** after **G1 — Consumer Route Loop**. This audit prepares that lane. It does not reopen closed Camera C9 ownership decisions and does not redesign Actor / PlayerSlot concepts.

**Naming note:** Package history already contains a product cut named `P3A — Player Product Simplification` (`Documentation~/Product/P3A-PLAYER-PRODUCT-SIMPLIFICATION-MANIFEST.md`). This document is a **different** P3A: a **materialization conformance audit** against accepted ADRs. Where the product simplification cut is relevant, it is cited as evidence of current product intent.

---

## 1. Resumo executivo

### 1.1 Modelo já definido (congelado)

Os ADRs e documentos canônicos já separam e nomeiam:

| Conceito | Significado canônico | Primária no package |
| -------- | -------------------- | ------------------- |
| **Actor** | Entidade de gameplay no mundo | `IActor`, `ActorId`, `ActorDeclaration`, `PlayerActorDeclaration` |
| **PlayerSlot** | Assento estável de participação | `PlayerSlotId`, `PlayerSlotDeclaration` |
| **PlayerSlotOccupancy** | Relação Slot → Actor | `PlayerSlotOccupancy`, `PlayerSlotOccupancyDescriptor` |
| **Player participation / Entry** | Vocabulário de entrada em jogo (passivo hoje) | `PlayerEntry`, `PlayerEntryBehaviour`, estados em `PlayerEntryState` |
| **PlayerComposer / PlayerRecipe** | Superfície de produto de authoring de um player concreto | `PlayerComposer`, `PlayerRecipe` |
| **Camera target source** | Follow/LookAt tipados, sem autoridade de winner | `PlayerComposer.CameraTarget` / `LookAtTarget` |
| **Camera rig materialization** | Materialização editor-only do rig Cinemachine | `CameraRigComposer` + editor Apply/Rebuild |
| **Camera request after eligibility** | Publicar/liberar request do local player | `LocalPlayerCameraRequestBinding` |
| **Runtime materialization** | Conteúdo runtime com request/result/handle/release | `RuntimeContent*` + adapters Unity prefab/release |
| **PlayerInput** | Evidência operacional Unity, **não** identidade | `PlayerInput` + adapters de Gate/InputMode |

Decisões-chave já aceitas:

1. **Actor ≠ PlayerSlot** (`F45-ADR-ACTOR-001`).
2. **Occupancy não implica spawn** (`F45` guardrail).
3. **Materialização física não pertence a Actor** (`F08-ADR-RUNTIME-001`).
4. **Player Recipe / Composer é a first reference product surface** (`ADR-PROD-0004`).
5. **PlayerComposer expõe targets; CameraRigComposer + request path consomem explicitamente** (`ADR-PROD-0006`, C9).
6. **Sem lookup por nome/tag/hierarquia, sem singleton/service locator** (guardrails de identidade e camera).
7. **Movement e gameplay permanecem game-owned** (P2 / product cuts).

### 1.2 Partes corretas

- Identidades tipadas `ActorId` e `PlayerSlotId` com domínios distintos.
- Declarações passivas `ActorDeclaration`, `PlayerActorDeclaration`, `PlayerSlotDeclaration`.
- Validadores e conjuntos: `ActorSet` / `PlayerActorSet` / `PlayerSlotSet` + topology validators.
- `PlayerComposer` materializa (editor-only) identidade, Gate, anchors de câmera e Reset opcional de forma explícita e tipada.
- `CameraRigComposer` resolve targets só por referência explícita a `PlayerComposer` ou `Transform` (sem descoberta por cena).
- `LocalPlayerCameraRequestBinding` publica/libera request com owner = `PlayerSlotId` e targets do `PlayerComposer`.
- RuntimeContent já existe para materialize/release de prefabs com handle e escopo — independente de Actor spawn.
- Contratos passivos de Entry/View/Control e adapters F51/F52 existem como evidência técnica, sem autoridade de lifecycle.

### 1.3 Divergências reais

1. **`PlayerComposer` Apply/Rebuild remove `PlayerSlotOccupancy`** (e Entry/View/Control passivos). Isso **colide** com o modelo F45/F49 em que Slot → Actor é uma relação explícita e com a topologia que exige occupancy coerente com entry.
2. **Identidade duplicada em strings** em `PlayerComposer` / `PlayerRecipe` (`actorId`, `playerSlotId`) em paralelo às declarações materializadas; a superfície de produto é autoridade de authoring, mas o runtime tipado vive nas declarations — risco de dessincronia se algo editar declarations fora do Composer.
3. **`ActorDefinition` não existe** como tipo. Não há contrato package-level que ligue `ActorId` → prefab/conteúdo resolvível. O conceito pedido no fluxo de materialização não tem correspondente nomeado; RuntimeContent resolve **conteúdo**, não **Actor identity catalog**.
4. **Caminho de câmera e input assume Player já instanciado na cena/prefab**: `CameraRigComposer.playerComposer` e `LocalPlayerCameraRequestBinding.playerComposer` são referências de cena a um Composer existente. Não há binding pós-spawn a partir de seleção de Slot/Actor.
5. **`eligibilityScopeId` / `requestId` / `tieBreakerId` fixos no binding** de câmera local — autoráveis e estáveis, mas não gerados nem amarrados a um runtime scope de materialização de player.
6. **Product cut P3A (simplification) desliga materialização de occupancy e passive entry/view/control** enquanto os validators de topologia ainda os tratam como evidência canônica. Há **dois modelos de “player ready”** coexistindo: product surface mínima vs topologia passiva F49.

### 1.4 Lacunas reais para materialização runtime

Para o fluxo desejado:

```text
escolher PlayerSlot
→ escolher Actor
→ resolver prefab/conteúdo
→ materializar instância
→ aplicar Slot → Actor
→ conectar input e câmera
→ liberar no fim do escopo
```

| Etapa | Estado atual |
| ----- | ------------ |
| Seleção explícita de Slot | **Ausente** como API runtime; só authoring de `PlayerSlotId` em Composer/Declaration |
| Seleção explícita de Actor | **Ausente** como API runtime; só authoring de `ActorId` string no Composer |
| Resolução de conteúdo por Actor | **Ausente** (sem `ActorDefinition` / sem mapa ActorId→resource) |
| Materialização de instância de player | **Ausente** como operação de player; RuntimeContent materializa conteúdo genérico, não “player” |
| Aplicar Slot → Actor | **Contrato existe** (`PlayerSlotOccupancy`), mas product Apply **remove** o componente |
| Binding de input pós-spawn | **Parcial**: Gate + `PlayerInput` no objeto; bridge/control adapters existem mas não são orquestrados no spawn |
| Binding de câmera pós-spawn | **Parcial**: path C9 completo exige Composer + Rig + Binding já wired; targets só funcionam se instância já existe |
| Release de lifetime de player | **Ausente** como operação de player; release existe em RuntimeContent/ContentAnchor, não amarrado a Slot/Actor |

**Conclusão:** o modelo conceitual está majoritariamente correto e implementado como **authoring passivo + product surface de instância em cena**. Falta a **integração runtime de materialização** e a **reconciliação occupancy/product**. Não é necessário inventar Session, managers ou novas identidades antes de provar que Slot, Actor, Occupancy, RuntimeContent e o path de câmera/input existentes não bastam.

---

## 2. Matriz de conformidade

| Decisão existente | ADR/documento | Implementação atual | Estado | Problema |
| ----------------- | ------------- | ------------------- | ------ | -------- |
| Actor ≠ PlayerSlot | F45-ADR-ACTOR-001 | `ActorId` / `PlayerSlotId` domínios separados; declarations distintas | **correto** | — |
| ActorId estável, não GO name/path | F45; F01-ID | `ActorId` + declarations; tooltips proíbem name/path | **correto** | — |
| PlayerSlotId estável, não playerIndex | F45; F49-PLAYER-003 | `PlayerSlotId`; `Player1`…`Player4` helpers | **correto** | — |
| PlayerInput ≠ identidade | F45 §9; F49-PLAYER-003 | Evidence em Slot/PlayerActor; Gate usa Slot tipado | **correto** | — |
| PlayerSlotOccupancy = Slot → Actor | F45 §5.3 | `PlayerSlotOccupancy` + descriptors + `PlayerSlotSet` | **divergente** (produto) | Apply/Rebuild do `PlayerComposer` **remove** occupancy (`LegacyMaterializationTypeNames`) |
| Occupancy não implica spawn | F45 guardrail | Occupancy é passivo; sem spawn | **correto** | — |
| PlayerActorDeclaration specialization | F45; F31 | `PlayerActorDeclaration` + `RequireComponent(PlayerInput)` | **correto** | Constraint same-GO PlayerInput é rule atual, não eterna (já documentado) |
| ActorDeclaration genérico | F45 | `ActorDeclaration` Kind/Role | **correto** | — |
| PlayerActorSet / PlayerSlotSet validação passiva | F45; F49 | Validators + Sets + issues | **correto** | Product players sem occupancy/entry geram topologia incompleta se validados no modelo F49 |
| PlayerEntry conecta Slot+Actor+readiness | F49-PLAYER-001 | `PlayerEntryBehaviour` passivo | **parcial** | Removido do materialize do Composer; estados Joined→Active são vocabulário, não pipeline |
| Topology 1 slot ↔ 1 occupancy ↔ 1 entry | F49; `PlayerTopologyValidator` | Validator implementado | **parcial** | Incompatível com shape product mínima (sem occupancy/entry) |
| PlayerRecipe intent reutilizável | ADR-PROD-0004 | `PlayerRecipe` SO com defaults | **parcial** | Carrega `actorId`+`playerSlotId` defaults (identidade de instância no recipe); não resolve conteúdo/prefab |
| PlayerComposer authoring + Apply/Rebuild | ADR-PROD-0004; product P3A simplification | Materializa declarations, Gate, anchors, Reset opcional | **parcial** | Correto como authoring de instância em cena; **não** é runtime materialization |
| Movement game-owned | ADR-PROD-0004; P2 | Composer não materializa motor | **correto** | — |
| Camera targets em PlayerComposer | ADR-PROD-0006; C9 | `CameraTarget` / `LookAtTarget` + anchors | **correto** | — |
| CameraRigComposer consome PlayerComposer explícito | ADR-PROD-0006; C9N | `targetSourceKind=PlayerComposer` + ref serializada | **correto** | Exige Player já existente; não resolve Actor materializado |
| Local player camera request path | C9K; ADR-PROD-0006 | `LocalPlayerCameraRequestBinding` | **parcial** | IDs de scope/request fixos; depende de Composer pré-wired; eligibility ≠ spawn |
| Materialização física via RuntimeContent | F08; F8R; F45 step 8 | `RuntimeMaterializationRequest`, prefab adapter, release, lifecycle registry | **correto** (genérico) | **Não** integrado a Actor/PlayerSlot/Composer |
| ActorDefinition / catálogo Actor→conteúdo | Pedido no fluxo P3; F45 step 8 (futuro) | **Tipo inexistente** | **ausente** | Não inventar ainda; gap real para “resolver prefab” |
| Seleção runtime de Slot e Actor | F45 occupancy replacement; F49 entry | Sem API de seleção | **ausente** | — |
| Binding input pós-materialização | F49-ACTOR-002; F52 chain | Gate + optional bridge/activation adapters (não orquestrados) | **parcial** | Sem coordinator de spawn→bind |
| Release de player no fim do escopo | F08 release; C9 release camera | Camera release existe; player instance release não | **ausente** (player) | RuntimeContent release genérico existe |
| Sem name/tag/hierarchy lookup funcional | Guardrails F45 / camera | Product path usa refs tipadas | **correto** | Validators usam `FindObjectsByType` só para diagnóstico de cena |
| Sem singleton/service locator | Guardrails | Não introduzidos no player path | **correto** | — |
| Actor materialization ≠ PlayerSlotOccupancy | F45 | Separados conceitualmente | **correto** | Falta orquestração que use ambos na ordem certa |

Legenda de estado: **correto** | **parcial** | **ausente** | **divergente** | **duplicado**.

---

## 3. Divergências por arquivo

### 3.1 `Runtime/PlayerAuthoring/PlayerComposer.cs`

| Campo | Conteúdo |
| ----- | -------- |
| **arquivo** | `Runtime/PlayerAuthoring/PlayerComposer.cs` |
| **responsabilidade correta** | Superfície de produto de **um** player concreto em cena/prefab: intent de identidade Slot+Actor, input tipado, anchors de câmera, Gate/Reset opcionais. Não spawn, join, movement ou camera winner. |
| **comportamento atual** | Serializa `actorId` e `playerSlotId` como `string`; exige `PlayerInput`; expõe `CameraTarget`/`LookAtTarget`; `MaterializeSlotOccupancy` e `MaterializePassiveEntryViewControl` forçados a `false`; campos legacy ocultos. |
| **decisão violada** | Nenhuma violação grave de ADR-PROD-0004. Tensão com F45: identidade funcional vive em strings de authoring até Apply, não em `ActorId`/`PlayerSlotId` tipados na API pública do Composer (`public string ActorId` / `PlayerSlotId`). |
| **correção necessária** | Manter Composer como authority de authoring. Em corte de materialização: (1) expor resolução tipada (`ActorId`/`PlayerSlotId`) a partir das strings normalizadas sem mudar semântica; (2) **não** usar o Composer como spawner; (3) decidir explicitamente se occupancy passa a ser materializada de novo no Apply **ou** se um runtime occupancy descriptor substitui o MonoBehaviour na topologia product. |
| **impacto em QA e FIRSTGAME** | QA/FIRSTGAME hoje montam player em cena com Composer. Runtime spawn exigirá fixtures novas sem quebrar o shape de cena. |

### 3.2 `Runtime/PlayerAuthoring/PlayerRecipe.cs`

| Campo | Conteúdo |
| ----- | -------- |
| **arquivo** | `Runtime/PlayerAuthoring/PlayerRecipe.cs` |
| **responsabilidade correta** | Defaults reutilizáveis de **intent** (flags, policies, mapas, validation mode). Não deve ser catálogo de conteúdo nem identidade canônica de runtime. |
| **comportamento atual** | Inclui defaults `actorId` e `playerSlotId`; `EditorApplyRecipeDefaults` copia para Composer. |
| **decisão violada** | **Duplicado** (identidade de instância no recipe + no composer + nas declarations). Não viola F45 se tratado só como default de authoring. |
| **correção necessária** | Documentar que Recipe defaults **não** são seleção runtime. Opcional: recipe sem Slot/Actor de instância se o fluxo de seleção for sempre externo (game/model). **Não** criar ActorDefinition dentro do Recipe sem prova. |
| **impacto em QA e FIRSTGAME** | Baixo se defaults forem só conveniência de Inspector. |

### 3.3 `Editor/PlayerAuthoring/PlayerComposerApplyRebuildUtility.cs`

| Campo | Conteúdo |
| ----- | -------- |
| **arquivo** | `Editor/PlayerAuthoring/PlayerComposerApplyRebuildUtility.cs` |
| **responsabilidade correta** | Apply/Rebuild editor-only: materializar bindings técnicos derivados do Composer; idempotente; remover legado inseguro. |
| **comportamento atual** | Cria/repara `PlayerActorDeclaration`, `PlayerSlotDeclaration`, Gate, anchors, Reset; **remove** `PlayerSlotOccupancy`, `PlayerEntryBehaviour`, `PlayerViewBehaviour`, `PlayerControlBehaviour` e targets de bridge/activation. |
| **decisão violada** | **Divergente** de F45 (occupancy explícita) e F49 (entry/view/control evidence + topology). Alinhado ao product cut “P3A simplification”. |
| **correção necessária** | Tratar como **divergência product vs topology**, não reintroduzir tudo às cegas. Correção mínima comprovada para materialização: se o runtime for validar Slot→Actor, precisa de **uma** representação canônica de occupancy (componente re-materializado **ou** descriptor runtime construído no apply de ocupação). Entry/View/Control passivos não são pré-requisito de spawn físico. |
| **impacto em QA e FIRSTGAME** | Smokes de topology F49 e authoring validation que contam occupancy/entry ficam desalinhados do shape Composer. FIRSTGAME product path não usa occupancy no player Composer. |

### 3.4 `Runtime/PlayerSlots/PlayerSlotOccupancy.cs`

| Campo | Conteúdo |
| ----- | -------- |
| **arquivo** | `Runtime/PlayerSlots/PlayerSlotOccupancy.cs` |
| **responsabilidade correta** | Relação passiva Slot → Actor; não spawn, clear automático, ou resolução de capability. |
| **comportamento atual** | Resolve Slot e Actor por declaration tipada ou string fallback; valida conflitos de fontes. |
| **decisão violada** | Implementação **correta** face a F45. Problema é **não ser materializada / ser removida** pelo product path. |
| **correção necessária** | Reintroduzir no fluxo de **ocupação** (não necessariamente no Apply de authoring de todos os players) quando houver seleção runtime ou quando topologia product precisar de prova Slot→Actor. |
| **impacto em QA e FIRSTGAME** | QA ainda pode montar occupancy manualmente; FIRSTGAME product shape pode não tê-la. |

### 3.5 `Runtime/Actors/PlayerActorDeclaration.cs` / `ActorDeclaration.cs`

| Campo | Conteúdo |
| ----- | -------- |
| **arquivo** | `Runtime/Actors/PlayerActorDeclaration.cs`, `Runtime/Actors/ActorDeclaration.cs` |
| **responsabilidade correta** | Declaração de identidade; sem lifetime/spawn/input routing. |
| **comportamento atual** | Conforme. PlayerActor exige evidência PlayerInput. |
| **decisão violada** | Nenhuma. |
| **correção necessária** | Nenhuma para conformidade. Materialização deve **escrever** identity na instância, não inventar outro id store. |
| **impacto em QA e FIRSTGAME** | Continua sendo a prova de identidade em cena. |

### 3.6 `Runtime/CameraAuthoring/CameraRigComposer.cs`

| Campo | Conteúdo |
| ----- | -------- |
| **arquivo** | `Runtime/CameraAuthoring/CameraRigComposer.cs` |
| **responsabilidade correta** | Authoring + materialização de rig; resolve Follow/LookAt de fonte tipada; sem winner selection. |
| **comportamento atual** | `PlayerComposer` explícito; lê `CameraTarget`/`LookAtTarget` no momento do resolve/Apply. |
| **decisão violada** | Conformidade com ADR-PROD-0006. **Lacuna de integração**: se o Player for spawnado depois, o rig materializado no editor pode ter targets nulos ou stale até re-resolve/runtime applicator. |
| **correção necessária** | Não redesenhar Composer. Garantir que, **após** materialização do player, targets sejam re-resolvidos via request path (`CameraRequest` + applicator) usando transforms da instância viva — já é o modelo C9 em runtime via request, não via re-Apply editor. |
| **impacto em QA e FIRSTGAME** | Cenas com player pre-placed PASS. Spawn deferred precisa wiring de request após spawn. |

### 3.7 `Runtime/Camera/Bindings/LocalPlayerCameraRequestBinding.cs`

| Campo | Conteúdo |
| ----- | -------- |
| **arquivo** | `Runtime/Camera/Bindings/LocalPlayerCameraRequestBinding.cs` |
| **responsabilidade correta** | Traduzir eligibility local em publish/release de `CameraRequest`; sem descobrir player; sem escolher winner. |
| **comportamento atual** | Refs explícitas: `PlayerComposer`, `CameraRigComposer`, `CameraOutputSessionBinding`; `eligibilityScopeId`, `requestId`, `tieBreakerId` serializados; owner id = `PlayerComposer.PlayerSlotId`. |
| **decisão violada** | Correto face a C9K. **Lacunas**: (1) exige Composer já existente; (2) IDs fixos em prefab/cena; (3) `eligibleOnEnable` amarra publish ao lifecycle do MonoBehaviour, não ao “ocupação confirmada”. |
| **correção necessária** | Para spawn: materializar ou reconfigurar binding **depois** da instância; opcionalmente derivar scope/request de lifetime tipado (RuntimeContent scope / Route/Activity) sem lookup por nome. Não usar GO name. |
| **impacto em QA e FIRSTGAME** | C9L/C9R e FIRSTGAME usam player pre-wired. Spawn exigirá casos de publish pós-spawn e release no unload. |

### 3.8 `Runtime/PlayerBinding/*` (bridge / control / view adapters)

| Campo | Conteúdo |
| ----- | -------- |
| **arquivo** | `Runtime/PlayerBinding/*` (ex.: `UnityPlayerInputBridgeAdapter`, `PlayerControlBindingAdapter`) |
| **responsabilidade correta** | Adapters explícitos de evidência/binding; sem spawn; sem movement. |
| **comportamento atual** | Contratos e results existem; Composer **não** materializa targets; Apply remove behaviours legados. |
| **decisão violada** | Product path divergiu de F51/F52 como materialização automática. Contratos em si **corretos**. |
| **correção necessária** | Reusar adapters **somente se** o consumer provar necessidade de control binding pós-spawn. Não reativar materialização em massa no Composer. |
| **impacto em QA e FIRSTGAME** | QA histórica de binding passivo ≠ product surface atual. |

### 3.9 `Runtime/RuntimeContent/*`

| Campo | Conteúdo |
| ----- | -------- |
| **arquivo** | `Runtime/RuntimeContent/*` (`UnityPrefabRuntimeMaterializationAdapter`, `RuntimeContentRuntime`, release adapters) |
| **responsabilidade correta** | Materialize/release de conteúdo com identity/owner/scope/handle. |
| **comportamento atual** | Genérico; smokes de ContentAnchor; flags `actorSpawn=False`, `playerJoin=False` em diagnósticos de bridge. |
| **decisão violada** | Conforme F08. **Lacuna**: sem etapa “Actor materialization boundary” (F45G) ligando resultado a `ActorId`/`PlayerSlotOccupancy`. |
| **correção necessária** | Orquestrar **depois** de Slot+Actor estáveis: materialize prefab → declarations na instância → occupancy → camera/input. Não mover spawn para Actor. |
| **impacto em QA e FIRSTGAME** | Reutilizar smokes de materialize/release; novos casos de player-owned content. |

### 3.10 `ActorDefinition` (tipo inexistente)

| Campo | Conteúdo |
| ----- | -------- |
| **arquivo** | *(nenhum)* |
| **responsabilidade correta** | Se existir no futuro: mapear `ActorId` → recurso de conteúdo (prefab/addressable) **sem** ser identidade e **sem** ser spawner. |
| **comportamento atual** | Ausente. Conteúdo é authorado em ContentAnchor bridges / RuntimeMaterializationResource. |
| **decisão violada** | Não há ADR que exija o nome `ActorDefinition` hoje. F45 step 8 prevê integração com RuntimeContent **após** estabilidade de declarations/occupancy. |
| **correção necessária** | **Não criar** `ActorDefinition` neste audit. Provar primeiro se ContentAnchor + resource tipado + tabela consumer-side bastam. Só introduzir se a resolução ActorId→conteúdo for framework-owned de fato. |
| **impacto em QA e FIRSTGAME** | FIRSTGAME pode continuar com prefab de player único até multi-actor selection. |

---

## 4. Fluxo esperado usando contratos existentes

Nomes reais do package. Nenhum tipo novo obrigatório.

```text
1. seleção de PlayerSlot
   → PlayerSlotId (ex.: PlayerSlotId.Player1 / "player.1")
   → evidência opcional: PlayerSlotDeclaration (pós-materialização ou seat pré-autorado)

2. seleção de Actor
   → ActorId (ex.: "firstgame.player" / "firstgame.ship")
   → evidência: ActorDeclaration ou PlayerActorDeclaration na instância (após spawn)

3. resolução do conteúdo
   → RuntimeMaterializationResource / prefab explícito (consumer ou ContentAnchor bridge)
   → NÃO: GameObject.Find, tag, hierarchy
   → NÃO: ActorId como path de prefab

4. materialização
   → RuntimeMaterializationRequest
   → IRuntimeMaterializationAdapter.Materialize (ex.: UnityPrefabRuntimeMaterializationAdapter)
   → RuntimeContentHandle em RuntimeContentState.Materialized
   → evidência física: UnityRuntimeMaterializedObjectRegistry

5. configuração da instância
   → escrever/garantir PlayerActorDeclaration.ActorId e PlayerSlotDeclaration.PlayerSlotId
   → PlayerInput evidence (Unity) no local player
   → anchors CameraTarget / LookAtTarget (de prefab ou equivalentes ao que PlayerComposer gera em editor)
   → opcional: UnityPlayerInputGateAdapter + Reset subject já modelados pelo product Apply (como template de prefab)

6. binding de input
   → PlayerInput configurado (actions / defaultActionMap)
   → UnityPlayerInputGateAdapter com sourceSlot tipado (PlayerSlotDeclaration)
   → (opcional, se motor exigir) PlayerControlBindingAdapter + UnityPlayerInputBridge/Activation — só se o consumer chamar explicitamente
   → NÃO tratar PlayerInput.playerIndex como PlayerSlotId

7. binding de câmera
   → CameraRigComposer (rig já materializado em editor ou re-resolvido)
   → LocalPlayerCameraRequestBinding.playerComposer = instância viva (ou targets equivalentes via CameraRequest)
   → SetLocalPlayerEligible(true) / TryPublish
   → CameraOutputSession admite request; CameraOutputContext escolhe winner; CameraOutputRigApplicator aplica

8. ocupação confirmada
   → PlayerSlotOccupancyDescriptor / componente PlayerSlotOccupancy: PlayerSlotId → ActorId
   → (opcional topologia) PlayerEntry com Slot+Actor+ActorReadiness
   → validação: PlayerSlotValidator / PlayerTopologyValidator se o escopo exigir prova

9. release
   → LocalPlayerCameraRequestBinding.SetLocalPlayerEligible(false) / TryRelease
   → limpar ou invalidar occupancy (relação, não destroy implícito)
   → RuntimeReleaseRequest / UnityObjectRuntimeReleaseAdapter + lifecycle registry
   → handle → Released; sem Destroy ad-hoc fora do adapter
```

### 4.1 Onde o fluxo quebra hoje

```text
seleção Slot/Actor ........ sem API (só fields de authoring)
resolver conteúdo ......... sem vínculo ActorId→resource no framework
materializar player ....... RuntimeContent existe, player path não
configurar instância ...... Composer Apply é editor-only
input/câmera .............. dependem de refs a PlayerComposer pré-existente
ocupação .................. removida pelo product Apply
release player ............ camera release ok; instance release não orquestrado
```

### 4.2 Fluxo product atual (cena pre-placed) — válido e distinto

```text
PlayerRecipe defaults
→ PlayerComposer na cena/prefab
→ Validate / Apply/Rebuild (editor)
→ PlayerActorDeclaration + PlayerSlotDeclaration + Gate + anchors
→ CameraRigComposer.PlayerComposer = esse Composer
→ LocalPlayerCameraRequestBinding wired
→ Play Mode: eligibility publica request; C9 arbitra
```

Esse fluxo **não** é o de materialização runtime. Ambos devem coexistir: scene-authored player vs deferred materialization.

---

## 5. Lacunas reais

### 5.1 Contrato

| Lacuna | Notas |
| ------ | ----- |
| API de seleção Slot + Actor | Intent de game/model; framework valida/admite (F49-PLAYER-003). Não existe operation type. |
| Representação canônica de ocupação no product path | Occupancy existe, product remove. Precisa de decisão de integração, não novo id. |
| ActorId → conteúdo | Sem `ActorDefinition`; sem contrato framework de catálogo. |
| Operação “Apply occupancy” runtime | Distinct de spawn (F45). Ausente. |
| Typed public API em Composer | `string` em vez de `ActorId`/`PlayerSlotId` structs (menor; conversão trivial). |

### 5.2 Runtime

| Lacuna | Notas |
| ------ | ----- |
| Orquestração materialize player | Usar RuntimeContent; não Actor-owned spawner. |
| Wire pós-spawn de camera request | Rebind `PlayerComposer`/targets + publish eligibility. |
| Wire pós-spawn de Gate/input | Template no prefab vs configure step. |
| Release de instância amarrado a Route/Activity/scope | LifecycleMaterializationRegistry existe; não player-specific. |
| Coordinator Entry→View→Control | Explicitamente fora do product mínimo; só se topologia F49 for reativada. |

### 5.3 Authoring

| Lacuna | Notas |
| ------ | ----- |
| Prefab de player como template de materialização | Composer Apply define shape editor; runtime precisa do mesmo shape no prefab. |
| Recipe sem pretensão de catálogo multi-actor | Defaults de identidade no Recipe confundem “template” com “personagem escolhido”. |
| Authoring de binding de câmera para player deferred | Hoje authoring assume Composer na cena. |

### 5.4 Integração

| Lacuna | Notas |
| ------ | ----- |
| Product Composer vs Topology F49 | Dois “player ready” shapes. |
| Camera C9 vs spawn | C9 fechado para pre-placed; spawn é P3. |
| ContentAnchor auto-materialization | Bridges existem; `actorSpawn`/`playerJoin` explicitamente false. |
| FIRSTGAME character select / respawn | Não coberto pelo package; consumer-owned até P3. |

### 5.5 Diagnóstico

| Lacuna | Notas |
| ------ | ----- |
| Validação product-aware | `PlayerBindingAuthoringValidator` ainda conta occupancy/entry. |
| Diagnóstico de “occupancy missing because product removed it” | Pode ser falso positivo. |
| Smokes de spawn player | Inexistentes; ContentAnchor smokes não cobrem Slot/Actor. |

### 5.6 QA

| Lacuna | Notas |
| ------ | ----- |
| Fixture: select Slot+Actor → materialize → occupancy → camera publish → release | Não existe. |
| Idempotência de double-materialize player | Não existe. |
| Failure cases: missing prefab, conflicting occupancy, missing CameraTarget | Parcialmente cobertos em sistemas isolados. |
| Alinhar validators ao product shape | Necessário antes de falhar builds por occupancy ausente. |

### 5.7 FIRSTGAME

| Lacuna | Notas |
| ------ | ----- |
| Player pre-placed com Composer + C9 | Provado (C9M/C9R). |
| Seleção de personagem / body swap | Não framework; precisa occupancy change sem novo Slot. |
| Spawn no entry de Gameplay Route | Provável P3 + G1 interaction; G1 não deve inventar spawn. |
| Release ao sair da Route de gameplay | Camera restore existe; destroy/release de player instance consumer-owned hoje. |

---

## 6. Ordem recomendada dos próximos cortes

Princípios: (1) não inventar identidade; (2) reconciliar product vs occupancy antes de spawn; (3) reutilizar RuntimeContent e C9; (4) G1 permanece active lifecycle; P3 ordenado depois conforme roadmap — estes cortes são a decomposição de **P3** quando iniciar.

### Corte P3A — Conformance lock (este documento)

| Campo | Conteúdo |
| ----- | -------- |
| **objetivo** | Congelar o mapa de conformidade e gaps reais sem código. |
| **tipo** | Documentação / audit |
| **escopo** | Este arquivo; leitura de ADRs e runtime/editor atuais |
| **fora de escopo** | Implementação, ZIP, novos ADRs, mudanças em QA/FIRSTGAME |
| **arquivos afetados** | `P3A-PLAYER-MATERIALIZATION-CONFORMANCE-AUDIT.md` |
| **QA esperado** | N/A |
| **FIRSTGAME esperado** | N/A |
| **critérios de aceite** | Matriz + divergências + fluxo + lacunas + ordem de cortes entregues; sem redesign conceitual |
| **commit sugerido** | `P3A — Player Materialization Conformance Audit` |

### Corte P3B — Product ↔ Occupancy reconciliation

| Campo | Conteúdo |
| ----- | -------- |
| **objetivo** | Eliminar a divergência “Composer remove occupancy” vs F45 Slot→Actor, sem reintroduzir F51/F52 em massa. |
| **tipo** | Contrato + authoring mínimo |
| **escopo** | Decidir e implementar **uma** via canônica: (A) Apply re-materializa `PlayerSlotOccupancy` a partir de Composer Slot+Actor, **ou** (B) product topology usa descriptor derivado do Composer sem MonoBehaviour, e validators product-aware param de exigir occupancy de cena. Preferir (A) se FIRSTGAME/QA ainda usam `PlayerSlotOccupancy` visual. |
| **fora de escopo** | Spawn, character select UI, movement, camera rewrite, Entry/View/Control passivos completos |
| **arquivos afetados** | `PlayerComposerApplyRebuildUtility.cs`, possivelmente `PlayerComposer.cs`, validators editor, docs product |
| **QA esperado** | Apply cria/repara occupancy coerente; segundo Apply idempotente; removal list não apaga occupancy se (A) |
| **FIRSTGAME esperado** | Player scene-authored mantém Slot→Actor explícito alinhado ao Composer |
| **critérios de aceite** | Uma relação Slot→Actor canônica no shape product; topology/product não se contradizem no path oficial |
| **commit sugerido** | `P3B — reconcile PlayerComposer occupancy with Slot→Actor contract` |

### Corte P3C — Runtime occupancy apply (sem spawn)

| Campo | Conteúdo |
| ----- | -------- |
| **objetivo** | API explícita para aplicar/limpar ocupação `PlayerSlotId → ActorId` com result/issues, sem Instantiate. |
| **tipo** | Runtime contract |
| **escopo** | Operation/result sobre occupancy; validação de conflito (1 actor / 1 slot); integração opcional com declarations existentes |
| **fora de escopo** | Prefab spawn, camera, movement, Session manager |
| **arquivos afetados** | `Runtime/PlayerSlots/*` (novos types de operation se necessário), smokes QA |
| **QA esperado** | Apply occupancy, replace actor, clear, conflict blocked |
| **FIRSTGAME esperado** | Base para future ship/drone swap sem novo Slot |
| **critérios de aceite** | Occupancy muda sem spawn; PlayerSlot estável; ActorId muda; failures explícitas |
| **commit sugerido** | `P3C — runtime PlayerSlot occupancy apply without spawn` |

### Corte P3D — Content resolution proof (ActorId → resource)

| Campo | Conteúdo |
| ----- | -------- |
| **objetivo** | Provar resolução de conteúdo para um Actor **sem** criar `ActorDefinition` se ContentAnchor/RuntimeContent bastarem. |
| **tipo** | Integração / boundary |
| **escopo** | Um mapa explícito consumer ou resource tipado referenciado por ActorId estável; materialize via `UnityPrefabRuntimeMaterializationAdapter`; handle + release |
| **fora de escopo** | Addressables complexos, pooling, registry global de todos os actors, UI de select |
| **arquivos afetados** | Possivelmente thin bridge; preferir consumer table + RuntimeContent existente; docs boundary |
| **QA esperado** | Materialize by explicit resource; release idempotent; missing resource blocks |
| **FIRSTGAME esperado** | Um prefab de player materializável sob controle de teste |
| **critérios de aceite** | Instância física com `PlayerActorDeclaration` configurável; sem name lookup; sem Actor-owned materializer |
| **commit sugerido** | `P3D — prove Actor content materialization via RuntimeContent` |

### Corte P3E — Instance configure + input attach

| Campo | Conteúdo |
| ----- | -------- |
| **objetivo** | Após materialize, configurar identity + Gate/input evidence na instância usando os mesmos campos que o Composer Apply escreve. |
| **tipo** | Runtime integration |
| **escopo** | Configure step: ActorId, PlayerSlotId, PlayerInput ref, defaultActionMap, Gate `sourceSlot`; reutilizar shape do product materializer (lógica compartilhada, não editor-only undo) |
| **fora de escopo** | Gameplay motors; full F52 chain salvo se FIRSTGAME exigir |
| **arquivos afetados** | Extração de configure logic (runtime-safe) a partir do Apply editor; prefab template |
| **QA esperado** | Instance post-materialize tem declarations + Gate válidos; invalid input blocks |
| **FIRSTGAME esperado** | Prefab player template alinhado ao Composer |
| **critérios de aceite** | Mesmas invariantes do product Apply sem depender de `UnityEditor` |
| **commit sugerido** | `P3E — runtime player instance configure and input gate attach` |

### Corte P3F — Camera bind after spawn

| Campo | Conteúdo |
| ----- | -------- |
| **objetivo** | Conectar câmera **depois** da instância viva usando C9 existente. |
| **tipo** | Integração camera/player |
| **escopo** | Garantir targets (`CameraTarget`/`LookAtTarget`) na instância; wire `LocalPlayerCameraRequestBinding` + `CameraRigComposer` por refs tipadas; `SetLocalLocalPlayerEligible(true)` após ocupação; release no exit |
| **fora de escopo** | Reabrir arbitration C9; split-screen; Session output redesign |
| **arquivos afetados** | Binding wiring helpers se necessário; QA C9-style cases pós-spawn |
| **QA esperado** | Publish após spawn; win/restore precedence inalterada; release no destroy/unload |
| **FIRSTGAME esperado** | Gameplay route pode spawnar player e assumir camera 50 |
| **critérios de aceite** | Nenhuma ref nula a Composer pre-placed obrigatória se o pipeline de spawn configurar refs; request ids estáveis e explícitos |
| **commit sugerido** | `P3F — bind local player camera after runtime materialization` |

### Corte P3G — Scoped release of player instance

| Campo | Conteúdo |
| ----- | -------- |
| **objetivo** | Liberar câmera + occupancy + RuntimeContent handle de forma ordenada ao sair do escopo. |
| **tipo** | Runtime lifecycle |
| **escopo** | Ordem: camera release → clear occupancy → content release; idempotência; diagnostics |
| **fora de escopo** | Save, multiplayer leave policy, join |
| **arquivos afetados** | Orchestration fina sobre APIs existentes; smokes |
| **QA esperado** | Leave scope: no leaked handles, no stale camera request, occupancy cleared |
| **FIRSTGAME esperado** | Exit gameplay route limpa player materializado se esse for o modelo escolhido |
| **critérios de aceite** | Release idempotente; sem Destroy fora do release adapter; sem service locator |
| **commit sugerido** | `P3G — scoped player materialization release` |

### Corte P3H — FIRSTGAME / QA proof of full path

| Campo | Conteúdo |
| ----- | -------- |
| **objetivo** | Prova end-to-end do fluxo §4 em QA e, se no escopo, FIRSTGAME. |
| **tipo** | QA + consumer integration |
| **escopo** | Smoke/fixture: select slot → select actor → materialize → occupy → input ready → camera publish → release |
| **fora de escopo** | Character creator UI, multiplayer, combat |
| **arquivos afetados** | QAFramework fixtures; opcional FIRSTGAME scene path |
| **QA esperado** | Suite dedicada PASS |
| **FIRSTGAME esperado** | Se incluído: um path real de gameplay entry sem player pre-placed **ou** proof dual (pre-placed + materializado) |
| **critérios de aceite** | Fluxo §4 executável sem name/tag lookup; diagnostics sem blockers |
| **commit sugerido** | `P3H — QA/FIRSTGAME player materialization path proof` |

### O que **não** fazer agora

```text
não criar novo ADR de identidade (F45 já cobre)
não criar Session manager de player
não criar ActorDefinition só pelo nome no audit
não tratar PlayerComposer como spawner
não mover movement/gameplay para o framework
não reintroduzir F51/F52 como materialização obrigatória no Composer
não usar PlayerInput como PlayerSlotId/ActorId
não usar PlayerSlot como Actor
não usar lookup por nome/tag/hierarquia
não usar singleton/service locator
não reabrir Camera C9 ownership
```

---

## 7. Mapa rápido: conceito pedido → tipo real

| Conceito na tarefa | Existe? | Tipo / local real |
| ------------------ | ------- | ----------------- |
| Actor | sim | `IActor`, `ActorDeclaration`, `PlayerActorDeclaration` |
| ActorDefinition | **não** | — (conteúdo via RuntimeContent / consumer) |
| ActorId | sim | `Runtime/Actors/ActorId.cs` |
| PlayerActor | sim (declaration) | `PlayerActorDeclaration` |
| PlayerSlot | sim | `PlayerSlotId`, `PlayerSlotDeclaration` |
| PlayerSlotId | sim | `Runtime/PlayerSlots/PlayerSlotId.cs` |
| PlayerSlotOccupancy | sim | `PlayerSlotOccupancy` (+ product remove no Apply) |
| Player participation | parcial | `PlayerEntry*` vocabulary; sem join runtime |
| PlayerComposer | sim | `Runtime/PlayerAuthoring/PlayerComposer.cs` |
| PlayerRecipe | sim | `Runtime/PlayerAuthoring/PlayerRecipe.cs` |
| runtime materialization | sim (genérico) | `Runtime/RuntimeContent/*` |
| camera target | sim | `PlayerComposer.CameraTarget` / `LookAtTarget` |
| input binding | parcial | `PlayerInput` + Gate; F52 adapters opcionais |
| lifetime e release | parcial | RuntimeContent release + camera request release; não player orchestrator |
| PlayerActorSet / PlayerSlotSet | sim | validators passivos |
| LocalPlayerCameraRequestBinding | sim | `Runtime/Camera/Bindings/` |
| CameraRigComposer | sim | `Runtime/CameraAuthoring/CameraRigComposer.cs` |

---

## 8. Veredito

| Área | Veredito |
| ---- | -------- |
| Identidades Actor / Slot | **Conformes** |
| Product surface Composer/Recipe | **Conforme como authoring de instância em cena** |
| Occupancy no product path | **Divergente** de F45; corrigir antes de spawn sério |
| Camera C9 path | **Conforme** para player pre-placed; **lacuna** pós-spawn |
| Runtime materialization genérica | **Presente e reutilizável** |
| Player materialization end-to-end | **Ausente** (integração, não redesenho conceitual) |
| Necessidade de novo ADR de identidade | **Não comprovada** |
| Necessidade de ActorDefinition agora | **Não comprovada**; provar RuntimeContent primeiro |

O próximo trabalho de implementação deve ser **integração ordenada (P3B→P3H)** sobre contratos existentes, não um redesign de Player.

---

*Fim da auditoria P3A — Player Materialization Conformance Audit.*
